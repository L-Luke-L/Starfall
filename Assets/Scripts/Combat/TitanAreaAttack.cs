using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 // A fixed ground footprint: each 14m band has a 10m escape corridor beside it.
 public sealed class TitanAreaAttack:MonoBehaviour {
  public SkyTitan titan;
  public Transform muzzle;
  public Material energyMaterial;
  public bool AttackEnabled=true;
  public bool IsAttacking=>Phase==1||Phase==2;
  public bool IsFiring=>Phase==2;
  public int Phase {get;private set;}
  public int AttackCount {get;private set;}
  public int HitCount {get;private set;}
  public float PhaseTime {get;private set;}
  public Vector3 LockedCenter {get;private set;}
  public int ActiveBand {get;private set;}=-1;
  public const float WarningSeconds=3.2f,BandSeconds=1.3f,HalfWidth=8.5f,HalfLength=44;
  readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
  const int EruptionCount=24;
  readonly Vector3[,] eruptionBases=new Vector3[3,EruptionCount];
  MeshRenderer[] bands;LineRenderer beam,beamCore;LineRenderer[] breathWisps;LineRenderer[] eruptions;
  GameObject effects;Light mouthLight;MeshRenderer throatGlow;float contactFxClock;bool hitThisAttack;float cooldown=10;MaterialPropertyBlock tint;
  public float BandX(int band)=>LockedCenter.x+(band-1)*24;
  public bool InBand(Vector3 point,int band)=>Mathf.Abs(point.x-BandX(band))<=HalfWidth&&Mathf.Abs(point.z-LockedCenter.z)<=HalfLength;
  void Start(){BuildEffects();}
  void BuildEffects(){
   effects=new GameObject("Dragon area attack · independent ground and mouth effects");effects.transform.SetParent(titan.arena.transform,false);
   tint=new MaterialPropertyBlock();bands=new MeshRenderer[3];for(int i=0;i<3;i++){
    var g=new GameObject("Terrain warning band "+i);g.transform.SetParent(effects.transform,false);g.AddComponent<MeshFilter>().sharedMesh=new Mesh();meshes.Add(g.GetComponent<MeshFilter>().sharedMesh);bands[i]=g.AddComponent<MeshRenderer>();bands[i].sharedMaterial=energyMaterial;bands[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   beam=Line("Mouth anchored breath halo",4.2f);beamCore=Line("White hot breath core",.96f);beam.positionCount=beamCore.positionCount=12;beamCore.sharedMaterial.SetColor("_Color",new Color(.75f,1,1,1));
   breathWisps=new LineRenderer[2];
   for(int i=0;i<breathWisps.Length;i++){
    var wisp=Line("Rolling plasma filament "+i,.52f);wisp.endWidth=.12f;wisp.positionCount=24;
    wisp.sharedMaterial.SetColor("_Color",new Color(.16f,.71f,1,.61f));breathWisps[i]=wisp;
   }
   eruptions=new LineRenderer[EruptionCount];for(int i=0;i<eruptions.Length;i++){eruptions[i]=Line("Ground eruption "+i,.77f);eruptions[i].endWidth=.035f;eruptions[i].positionCount=6;}
   mouthLight=new GameObject("Charging throat light").AddComponent<Light>();mouthLight.transform.SetParent(muzzle,false);mouthLight.color=new Color(.13f,.64f,1);mouthLight.range=28;mouthLight.intensity=0;
   throatGlow=titan.arena.MonsterFx.Glow(effects.transform,"Breath charge",muzzle.position,5,MonsterAttackFx.Plasma);
   effects.SetActive(false);
  }
  LineRenderer Line(string name,float width){var g=new GameObject(name);g.transform.SetParent(effects.transform,false);var l=g.AddComponent<LineRenderer>();var m=new Material(energyMaterial);materials.Add(m);m.SetColor("_Color",new Color(.10f,.48f,1,1));m.SetFloat("_Beam",1);l.sharedMaterial=m;l.positionCount=2;l.useWorldSpace=true;l.startWidth=width;l.endWidth=width*1.6f;l.numCapVertices=0;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return l;}
  void Footprint(){
   for(int b=0;b<3;b++){var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
    const int n=44,cols=8;for(int j=0;j<=n;j++)for(int k=0;k<=cols;k++){float x=BandX(b)+Mathf.Lerp(-HalfWidth,HalfWidth,k/(float)cols),z=LockedCenter.z-HalfLength+j*2;vertices.Add(new Vector3(x,FieldSurface.Height(x,z,LockedCenter.y)+.13f,z));uv.Add(new Vector2(k/(float)cols,j/(float)n));if(j>0&&k>0){int a=j*(cols+1)+k;indices.AddRange(new[]{a-cols-2,a-1,a-cols-1,a-cols-1,a-1,a});}}
    var mesh=meshes[b];mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
    for(int i=0;i<EruptionCount;i++){float ex=BandX(b)+(i%3-1)*5.2f+Mathf.Sin(i*8.3f)*1.1f,ez=LockedCenter.z-HalfLength+5+(i/3)*11;eruptionBases[b,i]=new Vector3(ex,FieldSurface.Height(ex,ez,LockedCenter.y)+.16f,ez);}
   }
  }
  public bool BeginAttack(){
   if(!AttackEnabled||!titan.Active||!titan.arena.CanTarget||IsAttacking||!effects)return false;
   titan.arena.Soundscape?.Action(8,.33f);
   LockedCenter=titan.player.transform.position;Phase=1;PhaseTime=0;contactFxClock=0;ActiveBand=-1;hitThisAttack=false;AttackCount++;Footprint();effects.SetActive(true);beam.enabled=beamCore.enabled=false;foreach(var wisp in breathWisps)wisp.enabled=false;foreach(var e in eruptions)e.enabled=false;return true;
  }
  public void Cancel(){Phase=0;PhaseTime=0;ActiveBand=-1;cooldown=8;mouthLight.intensity=0;if(effects)effects.SetActive(false);}
  void Update(){
   if(!effects)return;
   if(!AttackEnabled||titan.arena.Downed){if(IsAttacking)Cancel();return;}
   if(!titan.Active)return;
   float dt=Time.deltaTime;if(!IsAttacking){cooldown-=dt;if(cooldown<=0&&titan.arena.CanTarget)BeginAttack();return;}
   PhaseTime+=dt;
   if(Phase==1&&PhaseTime>=WarningSeconds){Phase=2;PhaseTime=0;titan.voice.Play();}
   if(Phase==2&&PhaseTime>=BandSeconds*3){Cancel();return;}
   // Ignite the locked central band first, then the flanks. No retargeting after warning.
   int sequence=Mathf.Min(2,(int)(PhaseTime/BandSeconds));ActiveBand=Phase==2?(sequence==0?1:sequence==1?0:2):-1;
   for(int i=0;i<3;i++){bool active=i==ActiveBand;float pulse=.32f+Mathf.Sin(PhaseTime*7)*.1f;tint.SetColor("_Color",active?new Color(.15f,.7f,1,.85f):new Color(1,.28f,.055f,pulse));tint.SetFloat("_Flow",PhaseTime);tint.SetFloat("_Charge",Phase==1?PhaseTime/WarningSeconds:active?1:0);bands[i].SetPropertyBlock(tint);}foreach(var material in materials)material.SetFloat("_Flow",PhaseTime);
   mouthLight.intensity=Phase==1?Mathf.Lerp(1,7,PhaseTime/WarningSeconds):10;
   throatGlow.transform.position=muzzle.position;
   throatGlow.transform.localScale=Vector3.one*(Phase==2?6.5f:Mathf.Lerp(2.2f,5.4f,PhaseTime/WarningSeconds));
   beam.enabled=beamCore.enabled=Phase==2;
   foreach(var wisp in breathWisps)wisp.enabled=Phase==2;
   if(Phase==2){
    float sweep=(PhaseTime%BandSeconds)/BandSeconds;float z=LockedCenter.z+Mathf.Lerp(-HalfLength,HalfLength,sweep);float x=BandX(ActiveBand);var contact=new Vector3(x,FieldSurface.Height(x,z,LockedCenter.y)+.2f,z);
    for(int i=0;i<beam.positionCount;i++){
     float t=i/(beam.positionCount-1f), amplitude=Mathf.Sin(t*Mathf.PI);
     var point=Vector3.Lerp(muzzle.position,contact,t);
     point+=new Vector3(Mathf.Sin(t*17-PhaseTime*19),Mathf.Cos(t*21-PhaseTime*12),0)*(.42f*amplitude);
     beam.SetPosition(i,point);beamCore.SetPosition(i,point);
    }
    var axis=(contact-muzzle.position).normalized;
    var right=Vector3.Cross(axis,Vector3.up).normalized;
    var up=Vector3.Cross(right,axis);
    for(int j=0;j<breathWisps.Length;j++){
     var wisp=breathWisps[j];
     for(int i=0;i<wisp.positionCount;i++){
      float t=i/(wisp.positionCount-1f),angle=t*14-PhaseTime*21+j*Mathf.PI;
      float radius=Mathf.Sin(t*Mathf.PI)*1.42f;
      wisp.SetPosition(i,Vector3.Lerp(muzzle.position,contact,t)+(Mathf.Cos(angle)*right+Mathf.Sin(angle)*up)*radius);
     }
    }
    contactFxClock-=dt;
    if(contactFxClock<=0){contactFxClock=.16f;titan.arena.MonsterFx.Burst(contact,Vector3.up,MonsterAttackFx.Plasma,3.25f);}
    for(int i=0;i<eruptions.Length;i++){var p=eruptionBases[ActiveBand,i];eruptions[i].enabled=Mathf.Abs(p.z-z)<19;float height=1.1f+Mathf.Sin(PhaseTime*13+i*4.3f)*.85f;for(int j=0;j<6;j++){float u=j/5f;eruptions[i].SetPosition(j,p+new Vector3(Mathf.Sin(i+j*2+PhaseTime*12)*.32f*u,height*u,Mathf.Cos(i+j+PhaseTime*10)*.22f*u));}}
    if(!hitThisAttack&&InBand(titan.player.transform.position,ActiveBand)&&titan.arena.Damage(CampaignRuntime.Current?CampaignRules.EnemyDamage(65):65,null)){hitThisAttack=true;HitCount++;}
   }
  }
  void OnGUI(){
   if(!IsAttacking||!titan.Active)return;var previous=GUI.color;GUI.depth=-23;float w=260,h=4,x=(Screen.width-w)*.5f,y=Screen.height*.19f;
   var label=new GUIStyle(GUI.skin.label){font=titan.player.uiFont,fontSize=17,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.83f,.9f,1)}};
   GUI.Label(new Rect(x-40,y-30,w+80,25),Phase==1?"焚界吐息":"灼烧扫掠",label);
   GUI.color=new Color(.03f,.055f,.07f,.8f);GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Phase==1?new Color(1,.44f,.15f):new Color(.15f,.7f,1);GUI.DrawTexture(new Rect(x,y,w*(Phase==1?PhaseTime/WarningSeconds:1-PhaseTime/(BandSeconds*3)),h),Texture2D.whiteTexture);GUI.color=previous;
  }
  void OnDestroy(){if(effects)Destroy(effects);foreach(var m in meshes)if(m)Destroy(m);foreach(var m in materials)if(m)Destroy(m);}
 }
}
