using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 [DefaultExecutionOrder(-200)]
 public sealed class CreatureArena : MonoBehaviour {
  public NavMeshData navigation;
  public AlienCreature[] creatures;
  public FirstPersonExplorer player;
  public Transform beacon;
  public Material projectileMaterial,impactMaterial;
  public int Health {get;private set;}=100;
  public int DamageCount {get;private set;}
  public int RespawnCount {get;private set;}
  public int BlockedProjectiles {get;set;}
  public bool SimulationEnabled=true;
  public int maxSimultaneousPursuers=2;
  readonly HashSet<AlienCreature> pursuers=new HashSet<AlienCreature>();
  public int PursuerCount=>pursuers.Count;
  public bool TryEngage(AlienCreature creature){if(!CanTarget||!creature)return false;if(pursuers.Contains(creature))return true;if(pursuers.Count>=maxSimultaneousPursuers)return false;pursuers.Add(creature);return true;}
  public void Release(AlienCreature creature){pursuers.Remove(creature);}
  public bool Active=>SimulationEnabled&&player&&player.Ready&&!player.Paused&&(!player.session||!player.session.IsTransitioning);
  public bool Safe=>player&&beacon&&FlatDistance(player.transform.position,beacon.position)<6;
  public bool Downed=>Health<=0;
  public bool CanTarget=>Active&&!Safe&&!Downed;
  public event Action<int,AlienCreature> PlayerDamaged;
  NavMeshDataInstance nav;float hitFlash,immune,downTimer,quiet,regeneration;
  void Awake(){if(navigation)nav=NavMesh.AddNavMeshData(navigation);foreach(var c in creatures)if(c)c.GetComponent<NavMeshAgent>().enabled=true;if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-battleQA"))SimulationEnabled=false;}
  public void RestoreHealth(int health){Health=Mathf.Clamp(health,1,100);}
  public bool Heal(int amount){if(Downed||Health>=100)return false;Health=Mathf.Min(100,Health+amount);return true;}
  public void SetPaused(bool paused){foreach(var c in creatures)if(c&&c.Agent&&c.Agent.isActiveAndEnabled&&c.Agent.isOnNavMesh){c.Agent.isStopped=paused;c.Agent.updatePosition=!paused;if(paused)c.Agent.nextPosition=c.transform.position;}}
  void OnDestroy(){if(nav.valid)nav.Remove();}
  public static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
  void Update(){
   if(!Active)return;float dt=Time.deltaTime;hitFlash=Mathf.Max(0,hitFlash-dt);immune=Mathf.Max(0,immune-dt);quiet+=dt;
   if(Downed){downTimer-=dt;if(downTimer<=0){player.Respawn();Health=100;immune=2;RespawnCount++;ClearBolts();foreach(var c in creatures)if(!c.GetComponent<CreatureVitality>()||!c.GetComponent<CreatureVitality>().Dead)c.ResetToHome();}}
   else if(Safe&&quiet>5){regeneration+=dt*4;int heal=Mathf.FloorToInt(regeneration);regeneration-=heal;Health=Mathf.Min(100,Health+heal);}
  }
  public bool Damage(int amount,AlienCreature source){
   if(!CanTarget||immune>0||amount<=0)return false;
   InteractionFeel.Play(FeelCue.Hurt,.8f);player.ImpactPulse(.65f);Health=Mathf.Max(0,Health-amount);DamageCount++;immune=.32f;hitFlash=.45f;quiet=0;PlayerDamaged?.Invoke(amount,source);
   if(Downed){downTimer=1.5f;ClearBolts();if(CampaignRuntime.Current){if(CampaignRuntime.Current.BombSeconds>=0)CampaignRuntime.Current.StartCoroutine(CampaignRuntime.Current.Restore("before-core"));else CampaignRuntime.Current.DropOnDeath(player.transform.position);}}return true;
  }
  public void ClearBolts(){foreach(var b in GetComponentsInChildren<AlienBolt>())Destroy(b.gameObject);}
  public AlienBolt Fire(AlienCreature owner,Vector3 origin,Vector3 aim){
   var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Dragon plasma · swept collision";g.transform.SetParent(transform);g.transform.position=origin;g.transform.localScale=Vector3.one*.28f;Destroy(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=projectileMaterial;
   var bolt=g.AddComponent<AlienBolt>();bolt.arena=this;bolt.owner=owner;bolt.velocity=(aim-origin).normalized*19;
   var trail=g.AddComponent<TrailRenderer>();trail.sharedMaterial=projectileMaterial;trail.time=.23f;trail.startWidth=.21f;trail.endWidth=.015f;trail.minVertexDistance=.08f;
   return bolt;
  }
  public void Impact(Vector3 position,Color color){
   for(int i=0;i<5;i++){
    var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Impact chip";g.transform.SetParent(transform);g.transform.position=position;g.transform.localScale=new Vector3(.018f,.038f,.018f);Destroy(g.GetComponent<Collider>());
    var renderer=g.GetComponent<Renderer>();renderer.sharedMaterial=impactMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    var block=new MaterialPropertyBlock();block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*.7f);renderer.SetPropertyBlock(block);
    var chip=g.AddComponent<ImpactChip>();chip.arena=this;chip.velocity=new Vector3(Mathf.Cos(i*2.4f)*.9f,1.1f+i*.12f,Mathf.Sin(i*2.4f)*.9f);
   }
  }
  public bool ClearSight(Vector3 from,Vector3 to,AlienCreature owner){
   var delta=to-from;foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)){
    if((owner&&hit.collider.GetComponentInParent<AlienCreature>()==owner)||hit.collider.GetComponentInParent<FirstPersonExplorer>()==player)continue;
    return false;
   }return true;
  }
  void OnGUI(){
   if(!player||!player.Ready||player.Paused)return;
   GUI.depth=-22;var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
   if(hitFlash>0){GUI.color=new Color(.75f,.11f,.065f,hitFlash*.34f);float edge=Screen.height*.065f;GUI.DrawTexture(new Rect(0,0,Screen.width,edge),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-edge,Screen.width,edge),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,0,edge,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width-edge,0,edge,Screen.height),Texture2D.whiteTexture);}
   float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale;
   GUI.color=new Color(.025f,.05f,.06f,Health<100?.7f:0);GUI.DrawTexture(new Rect(44,812,132,3),Texture2D.whiteTexture);GUI.color=Health==100?Color.clear:Safe?new Color(.24f,.75f,.71f):new Color(.92f,.64f,.33f);GUI.DrawTexture(new Rect(44,812,132*Health/100f,3),Texture2D.whiteTexture);GUI.color=Color.white;
   var style=new GUIStyle(GUI.skin.label){font=player.uiFont,fontSize=16,normal={textColor=new Color(.84f,.9f,.9f)}};
   if(Health<100)GUI.Label(new Rect(44,779,250,26),Health.ToString(),style);
   if(Downed){GUI.color=new Color(.02f,.025f,.035f,.55f);GUI.DrawTexture(new Rect(0,0,w,900),Texture2D.whiteTexture);GUI.color=Color.white;style.alignment=TextAnchor.MiddleCenter;style.fontSize=26;GUI.Label(new Rect(w/2-220,390,440,100),"信标回收中…",style);}
   GUI.matrix=matrix;GUI.color=Color.white;
  }
 }
}
