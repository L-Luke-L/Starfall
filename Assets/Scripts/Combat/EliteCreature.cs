using UnityEngine;

namespace Starfall {
 public sealed class EliteCreature:MonoBehaviour {
  public string Id=>ground?"ground":"wing";
  public string DisplayName=>ground?"玄甲地龙":"雷冠翼龙";
  public bool Exposed=>exposed>0;
  public bool Landed=>landed>0;
  public int SpecialPhase=>phase;
  public int LightningStrikes {get;private set;}
  public int Dives {get;private set;}
  public int TailSwings {get;private set;}
  AlienCreature actor;CreatureArena arena;CreatureVitality vitality;bool ground,lightning,diveNext=true,ownsActor,struck;
  float exposed,cycle=5,timer,landed;int phase,strikeIndex;Vector3 locked,diveDirection;Quaternion visualRest;
  LineRenderer[] marks;TrailRenderer[] diveWake;readonly Vector3[] strikePoints=new Vector3[3];
  public void Configure(AlienCreature creature,bool isGround,CreatureArena owner,CampaignRuntime state){actor=creature;ground=isGround;arena=owner;vitality=GetComponent<CreatureVitality>();marks=new LineRenderer[3];for(int i=0;i<3;i++){var g=new GameObject("Elite attack warning "+i);g.transform.SetParent(transform);var line=g.AddComponent<LineRenderer>();arena.MonsterFx.Style(line,MonsterAttackFx.Heat,.21f,.21f);line.positionCount=49;line.enabled=false;marks[i]=line;}}
  public void Expose(float seconds){exposed=Mathf.Max(exposed,seconds);}
  public int ModifyDamage(int damage){if(!actor||!arena||!arena.player)return damage;if(!ground)return Landed?Mathf.RoundToInt(damage*1.35f):damage;var toPlayer=arena.player.transform.position-transform.position;toPlayer.y=0;bool front=Vector3.Dot(transform.forward,toPlayer.normalized)>.15f;return exposed>0?Mathf.RoundToInt(damage*1.35f):front?Mathf.Max(1,Mathf.RoundToInt(damage*.30f)):damage;}
  void Suspend(){ownsActor=true;visualRest=actor.visual.localRotation;actor.enabled=false;if(actor.Agent&&actor.Agent.isOnNavMesh){actor.Agent.ResetPath();actor.Agent.isStopped=true;}actor.warning.enabled=false;}
  void Resume(){if(!ownsActor)return;ownsActor=false;actor.visual.localRotation=visualRest;if(!vitality||vitality.Dead)return;actor.enabled=true;if(actor.Agent&&actor.Agent.isOnNavMesh){actor.Agent.baseOffset=ground?0:3;actor.ResumeFromElite();}}
  void Cancel(){SetDiveWake(false);phase=0;landed=0;cycle=3;Warn(false);Resume();}
  void Begin(){arena.Soundscape?.Action(6,.28f);Suspend();phase=1;timer=ground?.95f:1.3f;locked=arena.player.transform.position;lightning=!ground&&!diveNext;if(!ground)diveNext=!diveNext;strikeIndex=0;struck=false;diveDirection=locked-transform.position;diveDirection.y=0;diveDirection=diveDirection.normalized;for(int i=0;i<3;i++){var p=locked+new Vector3((i-1)*3,0,i*2);p.y=FieldSurface.Height(p.x,p.z,locked.y);strikePoints[i]=p;}PaintWarning();Warn(true);InteractionFeel.Play(FeelCue.Alert,.75f);}
  void Update(){
   if(!actor||!arena||!vitality)return;if(vitality.Dead){SetDiveWake(false);Warn(false);return;}if(!arena.Active){SetDiveWake(false);return;}float dt=Time.deltaTime;exposed=Mathf.Max(0,exposed-dt);
   if(!arena.CanTarget){if(phase!=0)Cancel();return;}
   float distance=CreatureArena.FlatDistance(transform.position,arena.player.transform.position);
   if(phase==0){if(distance>22)return;cycle-=dt;if(cycle<=0&&!actor.IsWindingUp)Begin();return;}
   timer-=dt;
   if(phase==1){if(ground)actor.visual.localRotation=visualRest*Quaternion.Euler(0,-18*Mathf.Clamp01(1-timer/.95f),0);if(timer<=0){
     if(ground)arena.MonsterFx.Sweep(transform.position+Vector3.up*.95f,transform.forward,76,280,3.1f,MonsterAttackFx.Heat);
     phase=lightning?3:2;timer=ground?.30f:lightning?0:.65f;Warn(lightning);}return;}
   if(phase==2){
    if(ground){actor.visual.localRotation=visualRest*Quaternion.Euler(0,Mathf.Lerp(-18,35,Mathf.Clamp01(1-timer/.3f)),0);if(timer<=0){var delta=arena.player.transform.position-transform.position;delta.y=0;bool rear=Vector3.Dot(transform.forward,delta.normalized)<.5f;if(distance<4&&rear&&arena.ClearSight(actor.mouth.position,arena.player.transform.position+Vector3.up,actor))arena.Damage(CampaignRules.EnemyDamage(29),actor);TailSwings++;arena.MonsterFx.Burst(transform.position-transform.forward*2+Vector3.up*.2f,Vector3.up,MonsterAttackFx.Heat,1.04f);Finish();}}
    else {SetDiveWake(true);var from=transform.position;var step=diveDirection*14*dt;actor.Agent.isStopped=false;if(actor.Agent.Raycast(from+step,out var wall))timer=0;else actor.Agent.Move(step);actor.Agent.baseOffset=Mathf.MoveTowards(actor.Agent.baseOffset,.5f,dt*7);var segment=transform.position-from;float t=segment.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(arena.player.transform.position-from,segment)/segment.sqrMagnitude):0;var nearest=from+segment*t;if(!struck&&CreatureArena.FlatDistance(nearest,arena.player.transform.position)<1.8f&&Mathf.Abs(nearest.y-arena.player.transform.position.y)<3&&arena.ClearSight(actor.mouth.position,arena.player.transform.position+Vector3.up,actor)){struck=true;arena.Damage(CampaignRules.EnemyDamage(32),actor);}if(timer<=0){Dives++;Land();}}
   }
   else if(phase==3&&timer<=0){Strike(strikeIndex++);if(strikeIndex>=3)Land();else{timer=.6f;Warn(true);}}
   else if(phase==4){landed=Mathf.Max(0,timer);if(timer<=0)Finish();}
  }
  void Strike(int index){var point=strikePoints[index];arena.MonsterFx.Beam(point+Vector3.up*14,point,MonsterAttackFx.Plasma,.62f,true);arena.MonsterFx.Burst(point+Vector3.up*.13f,Vector3.up,MonsterAttackFx.Plasma,1.85f);LightningStrikes++;InteractionFeel.Play(FeelCue.Shot,.7f);var playerPoint=arena.player.transform.position;if(CreatureArena.FlatDistance(playerPoint,point)<2.2f&&Mathf.Abs(playerPoint.y-point.y)<2.5f&&arena.ClearSight(point+Vector3.up*14,playerPoint+Vector3.up,actor))arena.Damage(CampaignRules.EnemyDamage(24),actor);}
  void Land(){SetDiveWake(false);var at=OnSurface(transform.position);arena.MonsterFx.Burst(at,Vector3.up,MonsterAttackFx.Plasma,1.35f);Warn(false);phase=4;timer=landed=2.1f;if(actor.Agent&&actor.Agent.isOnNavMesh){actor.Agent.isStopped=true;actor.Agent.baseOffset=.5f;}InteractionFeel.Play(FeelCue.Step,.65f);}
  void Finish(){phase=0;landed=0;cycle=ground?7:8;Warn(false);Resume();}
  Vector3 OnSurface(Vector3 p){p.y=FieldSurface.Height(p.x,p.z,locked.y)+.14f;return p;}
  void PaintWarning(){
   var color=lightning?MonsterAttackFx.Plasma:MonsterAttackFx.Heat;
   foreach(var line in marks)line.startColor=line.endColor=color;
   if(ground){marks[0].positionCount=49;for(int i=0;i<49;i++){float angle=Mathf.Lerp(60,300,i/48f);marks[0].SetPosition(i,OnSurface(transform.position+Quaternion.Euler(0,angle,0)*transform.forward*4));}}
   else if(lightning){for(int k=0;k<3;k++){marks[k].positionCount=49;for(int j=0;j<49;j++){float a=j*Mathf.PI*2/48;marks[k].SetPosition(j,OnSurface(strikePoints[k]+new Vector3(Mathf.Sin(a)*2.2f,0,Mathf.Cos(a)*2.2f)));}}}
   else {var right=Vector3.Cross(Vector3.up,diveDirection)*1.8f;marks[0].positionCount=5;marks[0].SetPosition(0,OnSurface(transform.position-right));marks[0].SetPosition(1,OnSurface(locked-right));marks[0].SetPosition(2,OnSurface(locked+right));marks[0].SetPosition(3,OnSurface(transform.position+right));marks[0].SetPosition(4,OnSurface(transform.position-right));}
  }
  void Warn(bool active){if(marks==null)return;for(int i=0;i<marks.Length;i++)if(marks[i])marks[i].enabled=active&&(ground||!lightning?i==0:i>=strikeIndex);}
  void SetDiveWake(bool visible){
   if(diveWake==null){
    if(!visible)return;
    diveWake=new[]{
     arena.MonsterFx.MotionWake(transform,new Vector3(-1.72f,.78f,-.12f),MonsterAttackFx.Frost,.44f),
     arena.MonsterFx.MotionWake(transform,new Vector3(1.72f,.78f,-.12f),MonsterAttackFx.Frost,.44f)
    };
   }
   foreach(var ribbon in diveWake)ribbon.emitting=visible;
  }
  void OnDisable(){SetDiveWake(false);Warn(false);}
 }
}
