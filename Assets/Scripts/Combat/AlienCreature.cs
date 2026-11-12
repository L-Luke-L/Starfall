using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public enum CreatureKind {RiftMauler,SpineFloater}
 public enum CreatureState {Roam,Pursue,ChargeTell,Charge,BiteTell,Recover,Orbit,VolleyTell,Volley}
 [RequireComponent(typeof(NavMeshAgent))]
 public sealed class AlienCreature:MonoBehaviour {
  public CreatureKind kind;
  public CreatureArena arena;
  public Transform visual,mouth,core;
  public Transform[] limbs,fins;
  public LineRenderer warning;
  public Light glow;
  public float patrolRadius=11,detectionRange=22,leashRadius=40;
  public NavMeshAgent Agent {get;private set;}
  public CreatureState State {get;private set;}
  public int Attacks {get;private set;}
  public int Hits {get;private set;}
  public int Shots {get;private set;}
  public int Charges {get;private set;}
  public int Bites {get;private set;}
  public Vector3 VolleyAim=>lockedAim;
  public Vector3 Home {get;private set;}
  public bool IsWindingUp=>State==CreatureState.ChargeTell||State==CreatureState.BiteTell||State==CreatureState.VolleyTell;
  public bool CanBeLured=>!GetComponent<EliteCreature>()&&!Staggered&&(State==CreatureState.Roam||State==CreatureState.Pursue||State==CreatureState.Orbit);
  public bool Staggered=>staggerTime>0;float staggerTime;
  float age,timer,repath,cooldown,roamTimer,animationClock,turnSign=1,shotTimer;
  Vector3 destination,chargeDirection,lockedAim,lastPosition;int volley;bool struck;
  TrailRenderer[] chargeWake;
  MeshRenderer throatGlow;MaterialPropertyBlock attackTint;
  Quaternion[] limbRest,finRest;Vector3 visualRest,coreRest;
  void Awake(){Agent=GetComponent<NavMeshAgent>();Home=transform.position;lastPosition=Home;visualRest=visual.localPosition;coreRest=core.localScale;limbRest=new Quaternion[limbs.Length];for(int i=0;i<limbs.Length;i++)limbRest[i]=limbs[i].localRotation;finRest=new Quaternion[fins.Length];for(int i=0;i<fins.Length;i++)finRest[i]=fins[i].localRotation;}
  void Start(){
   ResetToHome();
   if(warning)arena.MonsterFx.Style(warning,MonsterAttackFx.Heat,.34f,.57f);
  }
  public void SetHome(Vector3 position){Home=position;lastPosition=position;}
  public void ResumeFromElite(){SetState(kind==CreatureKind.RiftMauler?CreatureState.Pursue:CreatureState.Orbit);cooldown=2;}
  public bool PulseStagger(float seconds){var life=GetComponent<CreatureVitality>();if(GetComponent<EliteCreature>()||!life||life.Dead||!Agent||!Agent.isOnNavMesh||!arena.CanTarget)return false;SetState(CreatureState.Recover);staggerTime=Mathf.Max(staggerTime,seconds);cooldown=Mathf.Max(cooldown,seconds+1);return true;}
  void OnDisable(){SetChargeWake(false);if(throatGlow)throatGlow.enabled=false;if(arena)arena.Release(this);}
  public void ResetToHome(){
   if(!Agent)Agent=GetComponent<NavMeshAgent>();
   if(arena)arena.Release(this);
   if(!gameObject.activeInHierarchy||!Agent.enabled)return;
   if(NavMesh.SamplePosition(Home,out var hit,8,NavMesh.AllAreas))Agent.Warp(hit.position);
   if(!Agent.isOnNavMesh)return;
   State=CreatureState.Roam;timer=0;cooldown=1;roamTimer=0;repath=0;warning.enabled=false;Agent.isStopped=false;Agent.ResetPath();
  }
  public void RegisterHit(){Hits++;}
  public void AlertToPlayer(){if(!Agent||!Agent.isOnNavMesh||!arena.CanTarget)return;if(State==CreatureState.Roam){SetState(kind==CreatureKind.RiftMauler?CreatureState.Pursue:CreatureState.Orbit);cooldown=.25f;}}
  void SetChargeWake(bool visible){
   if(chargeWake==null){
    if(!visible)return;
    chargeWake=new[]{
     arena.MonsterFx.MotionWake(transform,new Vector3(-.72f,.20f,.13f),MonsterAttackFx.Heat,.38f),
     arena.MonsterFx.MotionWake(transform,new Vector3(.72f,.20f,.13f),MonsterAttackFx.Heat,.38f)
    };
   }
   foreach(var ribbon in chargeWake)ribbon.emitting=visible;
  }
  void LateUpdate(){
   SetChargeWake(arena&&arena.Active&&!arena.Downed&&!Staggered&&State==CreatureState.Charge);
   bool charging=arena&&arena.Active&&State==CreatureState.VolleyTell;
   if(!charging){if(throatGlow)throatGlow.enabled=false;return;}
   Color color=MonsterAttackFx.BoltColor(this);
   if(!throatGlow){
    throatGlow=arena.MonsterFx.Glow(transform,"Breath charge",mouth.position,.70f,color);
    attackTint=new MaterialPropertyBlock();
   }
   throatGlow.enabled=true;throatGlow.transform.position=mouth.position;
   float amount=Mathf.Clamp01(timer/.62f);
   throatGlow.transform.localScale=Vector3.one*Mathf.Lerp(.30f,.91f,amount);
   color.a=Mathf.Lerp(.40f,1,amount);
   MonsterAttackFx.Tint(throatGlow,attackTint,color);
  }
  void Update(){
   if(!Agent||!Agent.isOnNavMesh)return;
   if(!arena||!arena.Active||arena.Downed){Agent.isStopped=true;warning.enabled=false;return;}
   if(staggerTime>0){staggerTime=Mathf.Max(0,staggerTime-Time.deltaTime);Agent.isStopped=true;warning.enabled=false;Animate(Time.deltaTime);return;}
   Agent.isStopped=false;float dt=Time.deltaTime;age+=dt;timer+=dt;cooldown-=dt;repath-=dt;animationClock+=dt;
   Vector3 target=arena.player.transform.position;float distance=CreatureArena.FlatDistance(transform.position,target);
   bool withinTerritory=CreatureArena.FlatDistance(target,Home)<leashRadius;
   bool see=arena.CanTarget&&withinTerritory&&distance<detectionRange&&arena.ClearSight(mouth.position,target+Vector3.up, this);
   // Entering the beacon's safe radius cancels committed attacks as well as pursuit.
   if(!arena.CanTarget||!withinTerritory||distance>detectionRange+10){arena.Release(this);SetRoam(dt);Animate(dt);return;}
   if((State==CreatureState.Roam||State==CreatureState.Pursue||State==CreatureState.Orbit)&&FrontierDecoy.Find(this,out var lure)&&arena.TryEngage(this)){Destination(lure,kind==CreatureKind.RiftMauler?5.2f:5.5f);Animate(dt);return;}
   if((State==CreatureState.Roam&&!see)||!arena.TryEngage(this)){arena.Release(this);SetRoam(dt);Animate(dt);return;}
   if(kind==CreatureKind.RiftMauler)GroundBrain(target,distance,see,dt);else FlyingBrain(target,distance,see,dt);
   Animate(dt);lastPosition=transform.position;
  }
  void SetState(CreatureState value){State=value;if(IsWindingUp)arena.RegisterCreatureTell();timer=0;repath=0;warning.enabled=false;Agent.ResetPath();Agent.isStopped=false;struck=false;}
  void Destination(Vector3 point,float speed){
   Agent.speed=CampaignRuntime.Current?CampaignRules.EnemySpeed(speed):speed;Agent.angularSpeed=kind==CreatureKind.RiftMauler?240:180;
   if(repath>0)return;repath=.3f;
   if(NavMesh.SamplePosition(point,out var hit,5,NavMesh.AllAreas)){destination=hit.position;Agent.SetDestination(hit.position);}
  }
  void Roam(float dt){
   warning.enabled=false;roamTimer-=dt;
   if(roamTimer<=0||(!Agent.pathPending&&Agent.remainingDistance<1.2f)){
    bool found=false;var path=new NavMeshPath();
    for(int i=0;i<16;i++){
     Vector2 offset=Random.insideUnitCircle*patrolRadius;var p=Home+new Vector3(offset.x,0,offset.y);
     if(CreatureArena.FlatDistance(p,arena.beacon.position)<11||CreatureArena.FlatDistance(p,transform.position)<8)continue;
     if(NavMesh.SamplePosition(p,out var hit,4,NavMesh.AllAreas)&&CreatureArena.FlatDistance(hit.position,Home)<=patrolRadius+1&&Agent.CalculatePath(hit.position,path)&&path.status==NavMeshPathStatus.PathComplete){destination=hit.position;found=true;break;}
    }
    roamTimer=found?Random.Range(5f,8):.5f;repath=0;
   }
   Destination(destination,kind==CreatureKind.RiftMauler?2.5f:3.4f);
  }
  void Face(Vector3 point,float dt){var dir=point-transform.position;dir.y=0;if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(dir),dt*220);}
  void GroundBrain(Vector3 target,float distance,bool see,float dt){
   switch(State){
    case CreatureState.ChargeTell:
     Agent.isStopped=true;Face(transform.position+chargeDirection,dt);warning.enabled=true;
     for(int i=0;i<warning.positionCount;i++){var point=transform.position+chargeDirection*(14*i/(warning.positionCount-1f));point.y=FieldSurface.Height(point.x,point.z,transform.position.y)+.13f;warning.SetPosition(i,point);}
     if(timer>=.65f){SetState(CreatureState.Charge);Attacks++;Charges++;arena.MonsterFx.Burst(transform.position+Vector3.up*.12f,Vector3.up,MonsterAttackFx.Heat,.85f);}break;
    case CreatureState.Charge:
     Agent.isStopped=true;Vector3 from=transform.position;Vector3 step=chargeDirection*(CampaignRuntime.Current?CampaignRules.EnemySpeed(17.5f):17.5f)*dt;
     if(Agent.Raycast(from+step,out var edge)){var elite=GetComponent<EliteCreature>();if(elite)elite.Expose(2.8f);SetState(CreatureState.Recover);cooldown=2.1f;break;}
     Agent.Move(step);var closest=Closest(from,from+step,target);
     if(!struck&&CreatureArena.FlatDistance(closest,target)<1.65f&&Mathf.Abs(target.y-transform.position.y)<2&&arena.ClearSight(mouth.position,target+Vector3.up,this)){struck=true;if(arena.Damage(CampaignRuntime.Current?CampaignRules.EnemyDamage(44):44,this))Hits++;}
     if(timer>1.05f){SetState(CreatureState.Recover);cooldown=2.1f;}break;
    case CreatureState.BiteTell:
     Agent.isStopped=true;if(timer<.25f)Face(target,dt);
     if(timer>=.42f){Attacks++;Bites++;arena.MonsterFx.Sweep(mouth.position,transform.forward,-49,49,.61f,MonsterAttackFx.Heat);var biteDirection=target-transform.position;biteDirection.y=0;if(distance<3.8f&&Vector3.Dot(transform.forward,biteDirection.normalized)>.15f&&arena.ClearSight(mouth.position,target+Vector3.up,this)){if(arena.Damage(CampaignRuntime.Current?CampaignRules.EnemyDamage(32):32,this))Hits++;arena.MonsterFx.Burst(mouth.position,transform.forward,MonsterAttackFx.Heat,.69f);}SetState(CreatureState.Recover);cooldown=1.15f;}break;
    case CreatureState.Recover:Agent.isStopped=true;if(timer>.5f)SetState(CreatureState.Pursue);break;
    default:
     if(!see&&distance>36){SetRoam(dt);break;}
     if(!see&&State==CreatureState.Roam){Roam(dt);break;}
     if(State!=CreatureState.Pursue)SetState(CreatureState.Pursue);
     Destination(target,5.8f);
     if(see&&cooldown<=0&&distance<3.5f&&arena.CanStartCreatureTell){SetState(CreatureState.BiteTell);}
     else if(see&&cooldown<=0&&distance>5&&distance<17&&arena.CanStartCreatureTell){chargeDirection=(target-transform.position);chargeDirection.y=0;chargeDirection.Normalize();if(!Agent.Raycast(transform.position+chargeDirection*Mathf.Min(14,distance+1),out var block))SetState(CreatureState.ChargeTell);}
     break;
   }
  }
  void SetRoam(float dt){if(State!=CreatureState.Roam)SetState(CreatureState.Roam);Roam(dt);}
  void FlyingBrain(Vector3 target,float distance,bool see,float dt){
   switch(State){
    case CreatureState.VolleyTell:
     Agent.isStopped=true;Face(target,dt);
     if(timer>=.62f){lockedAim=target+Vector3.up*1.05f+arena.player.Motor.velocity*.22f;SetState(CreatureState.Volley);volley=0;shotTimer=0;Attacks++;}break;
    case CreatureState.Volley:
     Agent.isStopped=true;shotTimer-=dt;
     if(shotTimer<=0&&volley<5){arena.Fire(this,mouth.position,lockedAim);Shots++;volley++;shotTimer=.18f;}
     if(volley>=5&&timer>.85f){SetState(CreatureState.Orbit);cooldown=1.65f;turnSign=-turnSign;}break;
    default:
     if(!see&&distance>36){SetRoam(dt);break;}
     if(!see&&State==CreatureState.Roam){Roam(dt);break;}
     if(State!=CreatureState.Orbit)SetState(CreatureState.Orbit);
     Vector3 away=transform.position-target;away.y=0;if(away.sqrMagnitude<.1f)away=Vector3.right;away.Normalize();var side=Vector3.Cross(Vector3.up,away)*turnSign;
     Vector3 goal=distance<9?transform.position+away*7+side*3:distance>17?target+away*13:target+away*13+side*7;
     if(distance>=9&&repath<=0&&arena.TryFlank(this,target,turnSign,out var flank))goal=flank;
     Destination(goal,5.1f);Face(target,dt);Agent.updateRotation=false;
     if(see&&distance>=8&&distance<24&&cooldown<=0&&arena.CanStartCreatureTell){SetState(CreatureState.VolleyTell);}break;
   }
  }
  void Animate(float dt){
   bool wind=IsWindingUp;float pulse=wind?1+Mathf.Sin(timer*24)*.13f:1;
   core.localScale=coreRest*pulse;glow.intensity=wind?3.2f:1.0f;
   float moving=Mathf.Clamp01(Agent.velocity.magnitude/2);
   if(kind==CreatureKind.RiftMauler){
    visual.localPosition=visualRest+Vector3.up*(Mathf.Sin(animationClock*10)*.05f*moving-(State==CreatureState.ChargeTell?.16f:0));
    visual.localRotation=Quaternion.Euler(State==CreatureState.BiteTell?-12:State==CreatureState.Charge?8:0,0,0);
    for(int i=0;i<limbs.Length;i++)limbs[i].localRotation=limbRest[i]*Quaternion.Euler(Mathf.Sin(animationClock*9+i*Mathf.PI)*27*moving,0,0);
   }else{
    Agent.updateRotation=State==CreatureState.Roam;var elite=GetComponent<EliteCreature>();Agent.baseOffset=elite&&elite.Landed ? .5f : 3.0f+Mathf.Sin(animationClock*1.9f)*.3f;
    visual.localRotation=Quaternion.Euler(Mathf.Sin(animationClock*1.4f)*4,0,Mathf.Sin(animationClock*2)*6);
    for(int i=0;i<limbs.Length;i++)limbs[i].localRotation=limbRest[i]*Quaternion.Euler(Mathf.Sin(animationClock*2.8f+i)*12,0,Mathf.Cos(animationClock*2+i)*9);
   }
   for(int i=0;i<fins.Length;i++)fins[i].localRotation=finRest[i]*Quaternion.Euler(0,0,(i==0?-1:1)*(Mathf.Sin(animationClock*4.5f)*24+8));
  }
  static Vector3 Closest(Vector3 a,Vector3 b,Vector3 p){Vector3 d=b-a;float f=d.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;return a+d*f;}
 }
}
