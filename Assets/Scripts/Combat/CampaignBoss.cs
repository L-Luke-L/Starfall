using UnityEngine;
using System.Collections.Generic;
namespace Starfall {
 public sealed class CampaignBoss:MonoBehaviour {
  public CampaignRegion region; public int Health {get;private set;} public int NodesRemaining {get;private set;}
  public bool Dead=>Health<=0; public bool WeakOpen=>NodesRemaining==0&&clock%8<3.3f;
  public int Attacks {get;private set;} public bool Telegraphing=>tell>0;
  readonly int[] nodes=new int[3]; readonly Transform[] nodesVisual=new Transform[3];
  readonly List<Mesh> warningMeshes=new List<Mesh>();Material warning;GuardianMechVisual mech;
  AudioSource music;bool musicPaused,engaged;
  internal bool ScoreActive=>music&&music.volume>.01f;
  public bool ShutdownComplete=>Dead&&mech&&mech.ShutdownComplete;
  Transform chest;Transform[] arms=new Transform[2];Transform[] markers=new Transform[3];float clock,attackClock=5,tell=-1;Vector3 aim;BasinProgress progress;
  const string Key="af7187df-3a32-46d6-8595-0f13bf346301";
  public void Configure(CampaignRegion owner){
   region=owner;progress=owner.campaign.ProgressFor(5);Health=progress.enemyHealth.TryGetValue(Key,out int hp)?hp:880;
   warning=new Material(Shader.Find("Starfall/TitanEnergy"));warning.SetColor("_Color",new Color(1,.37f,.14f,.82f));
   transform.position=new Vector3(0,AlienSurfaceLandmarks.Height(0,11),11);
   Part("Reactor frame",new Vector3(0,3.7f,0),new Vector3(3.2f,2.5f,1.8f),owner.dark,-1);
   chest=Part("Exposed core",new Vector3(0,3.6f,-1.02f),new Vector3(.9f,1,.22f),owner.accent,-1);
   Part("Command cockpit",new Vector3(0,5.5f,0),new Vector3(1.55f,.85f,1.25f),owner.steel,-1);
   Part("Cockpit visor",new Vector3(0,5.52f,-.66f),new Vector3(1.1f,.42f,.09f),owner.dark,-1);
   Part("Commander helmet",new Vector3(0,5.57f,-.73f),new Vector3(.31f,.30f,.14f),owner.steel,-1);
   Part("Commander amber visor",new Vector3(0,5.56f,-.82f),new Vector3(.22f,.10f,.04f),owner.warningMaterial,-1);
   for(int i=0;i<5;i++){Part("Reactor vent left "+i,new Vector3(-1.05f,3.05f+i*.27f,-.96f),new Vector3(.48f,.09f,.13f),owner.steel,-1);Part("Reactor vent right "+i,new Vector3(1.05f,3.05f+i*.27f,-.96f),new Vector3(.48f,.09f,.13f),owner.steel,-1);}
   Part("Chest crest",new Vector3(0,4.72f,-.65f),new Vector3(2.3f,.38f,.9f),owner.steel,-1).localRotation=Quaternion.Euler(13,0,0);
   for(int side=-1;side<=1;side+=2){
    Part("Shoulder armor",new Vector3(side*2.1f,4.35f,0),new Vector3(1.45f,1.35f,1.6f),owner.steel,-1).localRotation=Quaternion.Euler(0,0,-side*17);
    arms[side<0?0:1]=Part("Forearm emitter",new Vector3(side*2.25f,2.9f,-.25f),new Vector3(.9f,1.7f,1.25f),owner.dark,-1);
    Part("Elbow piston",new Vector3(side*2.23f,3.5f,-.75f),new Vector3(.4f,.34f,.36f),owner.steel,-1);
    Part("Cannon aperture",new Vector3(side*2.25f,2.7f,-.94f),new Vector3(.52f,.33f,.15f),owner.warningMaterial,-1);
    Part("Thigh actuator",new Vector3(side*.95f,1.9f,0),new Vector3(.85f,1.7f,1),owner.steel,-1);
    Part("Shin armor",new Vector3(side*1.13f,.85f,-.15f),new Vector3(1.1f,1.4f,1.2f),owner.dark,-1);
    Part("Ground brace",new Vector3(side*1.13f,.22f,-.55f),new Vector3(1.45f,.44f,2.05f),owner.steel,-1);
    Part("Knee cap",new Vector3(side*1.13f,1.55f,-.79f),new Vector3(.88f,.64f,.39f),owner.steel,-1);
    Part("Shin actuator",new Vector3(side*1.13f,.8f,-.79f),new Vector3(.15f,.58f,.12f),owner.accent,-1);
   }
   for(int i=0;i<3;i++){
    string key=Key+"/"+i;nodes[i]=progress.enemyHealth.TryGetValue(key,out hp)?hp:88;if(nodes[i]>0)NodesRemaining++;
    // All nodes remain reachable with the pick: no ammunition requirement.
    nodesVisual[i]=Part("Power node "+(i+1),new Vector3((i-1)*3.8f,1.05f,-2.2f),new Vector3(.9f,1.4f,.9f),owner.accent,i);
    nodesVisual[i].gameObject.SetActive(nodes[i]>0);
    Part("Node plinth",new Vector3((i-1)*3.8f,.23f,-2.2f),new Vector3(1.4f,.46f,1.4f),owner.dark,-1);
    var warning=new GameObject("D4 sweep warning "+i).transform;warning.SetParent(region.player.arena.transform);markers[i]=warning;
    var mesh=new Mesh();warningMeshes.Add(mesh);warning.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=warning.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=this.warning;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;warning.gameObject.SetActive(false);
   }
   mech=gameObject.AddComponent<GuardianMechVisual>();mech.Build(chest,arms,nodesVisual,region.player.arena);
   // Cover interrupts the ground cannon's line of sight.
   for(int i=0;i<4;i++){float x=i%2==0?-7:7,z=-8+i/2*8;var cover=region.Box("Core blast shelter "+i,region.player.arena.transform,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)+1.35f,z),new Vector3(3.2f,2.7f,1.6f),owner.steel,true);}
   if(Dead)StopMech(false);
   else{
    var clip=Resources.Load<AudioClip>("GuardianBattle");
    if(clip){
     music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;
     music.clip=clip;music.spatialBlend=0;music.volume=0;
    }
   }
  }
  Transform Part(string name,Vector3 pos,Vector3 size,Material mat,int node){var t=region.Box(name,transform,pos,size,mat,true);var part=t.gameObject.AddComponent<CampaignBossPart>();t.GetComponent<Renderer>().enabled=false;part.boss=this;part.node=name=="Exposed core"?-1:node<0?-2:node;return t;}
  public bool Hit(int node,int damage){
   if(Dead||damage<=0||node<-1||!region.player.arena.CanTarget)return false;
   if(node>=0&&node<3){if(nodes[node]<=0)return false;nodes[node]=Mathf.Max(0,nodes[node]-damage);progress.enemyHealth[Key+"/"+node]=nodes[node];if(nodes[node]==0){NodesRemaining--;nodesVisual[node].gameObject.SetActive(false);region.campaign.Notice("供能节点已破坏 · 剩余 "+NodesRemaining);}}
   else{if(!WeakOpen)return false;Health=Mathf.Max(0,Health-damage);progress.enemyHealth[Key]=Health;if(Dead){region.campaign.State.Flag("boss-defeated");StopMech(true);region.campaign.Notice("守核机甲已停机 · 前往核心控制台");}}
   InteractionFeel.Play(FeelCue.Hit,.8f);region.campaign.Changed();return true;
  }
  void StopMech(bool animate){
   foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
   foreach(var m in markers)if(m)m.gameObject.SetActive(false);
   tell=-1;mech.ShutDown(region.player.arena,animate);
  }
  void UpdateMusic(CreatureArena arena,float dt){
   if(!music)return;
   if(!arena.Active||region.campaign.CinematicActive){
    if(music.isPlaying){music.Pause();musicPaused=true;}
    return;
   }
   if(!Dead&&arena.CanTarget&&CreatureArena.FlatDistance(region.player.transform.position,transform.position)<24)engaged=true;
   float target=engaged&&!Dead?.37f:0;
   if(musicPaused){music.UnPause();musicPaused=false;}
   if(target>0&&!music.isPlaying)music.Play();
   music.volume=Mathf.MoveTowards(music.volume,target,dt*(Dead?.47f:.31f));
   if(target==0&&music.volume==0&&music.isPlaying)music.Stop();
  }
  void PaintWarning(int i,float x,float z){
   var marker=markers[i];marker.position=new Vector3(x,0,z);var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int rows=24,cols=8;
   for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){float px=x-4+col,pz=z-18+row*1.5f;vertices.Add(new Vector3(px,FieldSurface.Height(px,pz,aim.y)+.1f,pz)-marker.position);uv.Add(new Vector2(col/(float)cols,row/(float)rows));if(row>0&&col>0){int a=row*(cols+1)+col;triangles.AddRange(new[]{a-cols-2,a-1,a-cols-1,a-cols-1,a-1,a});}}
   var mesh=warningMeshes[i];mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();marker.gameObject.SetActive(true);
  }
  void Blast(){
   var arena=region.player.arena;
   for(int i=0;i<3;i++){
    float x=markers[i].position.x;
    var target=new Vector3(x,FieldSurface.Height(x,aim.z,aim.y)+.1f,aim.z);
    Vector3 origin=transform.position+new Vector3(i==0?-2.25f:2.25f,2.7f,-1.15f);
    var delta=target-origin;
    var hits=Physics.RaycastAll(origin,delta.normalized,delta.magnitude);
    System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
    Vector3 normal=Vector3.up;
    foreach(var hit in hits){
     if(hit.collider.GetComponent<CampaignBossPart>()||hit.collider.GetComponentInParent<FirstPersonExplorer>())continue;
     target=hit.point;normal=hit.normal;break;
    }
    arena.MonsterFx.Beam(mech.Muzzle(i==0?0:1),target,MonsterAttackFx.Heat,.70f);
    arena.MonsterFx.Burst(target+normal*.06f,normal,MonsterAttackFx.Heat,1.45f);
   }
  }
  void Update(){
   if(!region||!mech)return;
   var arena=region.player.arena;float dt=Time.deltaTime;
   UpdateMusic(arena,dt);
   if(!arena.Active)return;
   if(Dead){mech.StepShutdown(arena,dt);return;}
   clock+=dt;
   // Opening lowers the exposed core to pick height.
   chest.localPosition=new Vector3(0,WeakOpen?1.65f:3.6f,-1.22f);mech.SetCoreState(WeakOpen,NodesRemaining>0);
   for(int i=0;i<2;i++)arms[i].localRotation=Quaternion.Euler(tell>0?-18:Mathf.Sin(clock*.7f+i)*2,0,(i==0?-1:1)*5);
   if(!region.player.arena.CanTarget){mech.SetCharge(0);tell=-1;attackClock=Mathf.Max(attackClock,2);foreach(var m in markers)m.gameObject.SetActive(false);return;}
   mech.SetCharge(tell>0?1-tell/3.2f:0);
   warning.SetFloat("_Charge",tell>0?1-tell/3.2f:0);warning.SetFloat("_Flow",clock);if(tell>0){tell-=dt;if(tell<=0){
     var p=region.player.transform.position;bool lane=false;foreach(var m in markers){if(Mathf.Abs(p.x-m.position.x)<4&&Mathf.Abs(p.z-m.position.z)<18)lane=true;m.gameObject.SetActive(false);}
     Vector3 origin=transform.position+new Vector3(0,2.3f,-2);if(lane&&region.player.arena.ClearSight(origin,p+Vector3.up,null))region.player.arena.Damage(84,null);
     Blast();InteractionFeel.Play(FeelCue.Shot,1);Attacks++;attackClock=4.5f;tell=-1;
    }}else{attackClock-=dt;if(attackClock<=0){aim=region.player.transform.position;tell=3.2f;arena.Soundscape?.Action(0,.26f);for(int i=0;i<3;i++)PaintWarning(i,aim.x+(i-1)*12,aim.z);InteractionFeel.Play(FeelCue.Alert,.9f);}}
  }
  void OnGUI(){if(!region||Dead||!region.player.Ready||region.player.Paused)return;float w=Screen.width;
   InteractionFeel.Text(new Rect(w/2-280,28,560,32),"D4 守核机甲 · "+(NodesRemaining>0?"供能节点 "+NodesRemaining:WeakOpen?"核心暴露":"装甲闭合"),region.player.uiFont,20,new Color(.95f,.75f,.48f),TextAnchor.MiddleCenter);
   GUI.color=new Color(.15f,.2f,.23f,.85f);GUI.DrawTexture(new Rect(w/2-220,65,440,4),Texture2D.whiteTexture);GUI.color=new Color(.92f,.4f,.18f);GUI.DrawTexture(new Rect(w/2-220,65,440*Health/880f,4),Texture2D.whiteTexture);GUI.color=Color.white;
   if(tell>0)InteractionFeel.Text(new Rect(w/2-300,105,600,30),"离开亮区 / 进入掩体",region.player.uiFont,18,new Color(1,.55f,.25f),TextAnchor.MiddleCenter);
  }
  void OnDestroy(){foreach(var mesh in warningMeshes)if(mesh)Destroy(mesh);if(warning)Destroy(warning);foreach(var marker in markers)if(marker)Destroy(marker.gameObject);}
 }
 public sealed class CampaignBossPart:MonoBehaviour {public CampaignBoss boss;public int node;}
}
