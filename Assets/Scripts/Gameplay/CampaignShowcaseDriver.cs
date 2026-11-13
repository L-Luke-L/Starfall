using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 // Runs ordinary gameplay commands for the isolated showcase. No quest flags or
 // enemy health records are fabricated here; failures remain visible to the host.
 public sealed class CampaignShowcaseDriver {
  readonly CampaignRuntime owner;
  public bool LastSucceeded {get;private set;}
  public int GuardianDodges {get;private set;}
  public string Failure {get;private set;}="";
  public CampaignShowcaseDriver(CampaignRuntime campaign){owner=campaign;}
  bool Active=>owner&&owner.ShowcaseActive;
  float Delta=>Active?owner.ShowcaseDelta:0;
  void Begin(){LastSucceeded=false;Failure="";}
  void Fail(string message){LastSucceeded=false;Failure=message;if(owner)owner.Notice(message);}
  public static void Warp(FirstPersonExplorer player,Vector3 point){
   if(!player)return;
   if(NavMesh.SamplePosition(point,out var sampled,3,NavMesh.AllAreas))point=sampled.position;
   bool motorEnabled=player.Motor&&player.Motor.enabled;
   if(player.Motor)player.Motor.enabled=false;
   player.transform.position=point;player.ResetMotion();
   if(player.Motor)player.Motor.enabled=motorEnabled;
   Physics.SyncTransforms();
  }
  public void AimAt(Vector3 point,float dt){
   var player=owner.Explorer;if(!player||dt<=0)return;
   var direction=point-player.viewCamera.transform.position;if(direction.sqrMagnitude<.001f)return;
   float desiredYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
   float desiredPitch=-Mathf.Asin(Mathf.Clamp(direction.normalized.y,-1,1))*Mathf.Rad2Deg;
   player.Look(Mathf.Clamp(Mathf.DeltaAngle(player.transform.eulerAngles.y,desiredYaw),-148*dt,148*dt),
    Mathf.Clamp(player.Pitch-desiredPitch,-110*dt,110*dt));
  }
  void DriveToward(FirstPersonExplorer player,Vector3 point,float dt,bool running=true){
   var delta=point-player.transform.position;delta.y=0;
   var local=player.transform.InverseTransformDirection(delta.normalized);
   player.Drive(new Vector2(local.x,local.z),running,false,Mathf.Min(dt,.05f));
  }
  static float FlatDistance(Vector3 a,Vector3 b)=>CreatureArena.FlatDistance(a,b);
  internal static bool FindRoute(FirstPersonExplorer player,Vector3 goal,out Vector3 target,out NavMeshPath path,float sampleRadius=2f){
   path=new NavMeshPath();
   target=goal;
   if(!NavMesh.SamplePosition(player.transform.position,out var start,2f,NavMesh.AllAreas)||
    !NavMesh.SamplePosition(goal,out var end,sampleRadius,NavMesh.AllAreas))return false;
   target=end.position;
   return NavMesh.CalculatePath(start.position,target,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
  }
  static float RouteLength(Vector3[] corners){
   float length=0;
   for(int i=1;i<corners.Length;i++)length+=Vector3.Distance(corners[i-1],corners[i]);
   return length;
  }
  public IEnumerator Face(Vector3 point,float hold=.55f){
   var player=owner.Explorer;
   float time=0,settled=0;
   while(Active&&player&&time<3f&&settled<hold){
    float dt=Delta;
    if(dt>0){
     time+=dt;
     AimAt(point,dt);
     var camera=player.viewCamera.transform;
     if(Vector3.Angle(camera.forward,point-camera.position)<5)settled+=dt;
     else settled=0;
    }
    yield return null;
   }
  }
  public IEnumerator MoveTo(Vector3 goal,float seconds=22,float arrival=1.05f){
   Begin();
   var player=owner.Explorer;
   if(!player){Fail("演示行走失败：探索者尚未就绪");yield break;}
   player.Automated=true;
   Vector3 start=player.transform.position,previous=start,target=goal;
   Vector3[] corners=Array.Empty<Vector3>();
   int next=0;
   float elapsed=0,stuck=0,replan=0,moved=0;
   while(Active&&elapsed<seconds&&!player.arena.Downed){
    float dt=Delta;
    if(dt<=0){yield return null;continue;}
    elapsed+=dt;replan-=dt;
    if(FlatDistance(player.transform.position,target)<arrival&&Mathf.Abs(player.transform.position.y-target.y)<1.3f){
     LastSucceeded=true;break;
    }
    if(replan<=0&&(corners.Length==0||stuck>.55f)){
     if(FindRoute(player,goal,out target,out var path)){
      corners=path.corners;next=corners.Length>1?1:0;
     }else corners=Array.Empty<Vector3>();
     replan=.65f;
    }
    if(corners.Length==0){yield return null;continue;}
    while(next<corners.Length-1&&FlatDistance(player.transform.position,corners[next])<.65f&&
     Mathf.Abs(player.transform.position.y-corners[next].y)<1.4f)next++;
    Vector3 step=corners[next];
    var look=step-player.transform.position;look.y=0;
    if(look.sqrMagnitude>.10f)AimAt(player.transform.position+look.normalized*7+Vector3.up*1.2f,dt);
    DriveToward(player,step,dt,FlatDistance(player.transform.position,step)>2.1f);
    float stepDistance=FlatDistance(player.transform.position,previous);
    moved+=stepDistance;
    stuck=stepDistance<dt*.35f?stuck+dt:Mathf.Max(0,stuck-dt*2);
    previous=player.transform.position;
    if(owner.BombSeconds<0&&player.arena.Health<=45)owner.UseMedkit();
    if(stuck>3.25f)break;
    yield return null;
   }
   if(!Active)yield break;
   player.ResetMotion();
   owner.TraceShowcase("walk",$"arrived={LastSucceeded} distance={moved:F1} from={start} to={player.transform.position} target={goal}");
   if(!LastSucceeded)Fail($"演示行走失败：路线未到达目标 {goal}，当前位置 {player.transform.position}");
  }
  public IEnumerator MineLayer(){
   Begin();var director=owner.director;int layer=owner.State.layer;
   if(!director.underground){Fail("演示采矿失败：尚未进入矿井");yield break;}
   float oldCast=director.castSpeed,oldRetrieve=director.retrieveSpeed;
   float elapsed=0;int initial=owner.Collected(layer);bool firstCast=true;
   try{
    while(Active&&elapsed<45&&!owner.LayerComplete(layer)){
     float dt=Delta;if(dt<=0){yield return null;continue;}elapsed+=dt;
     if(director.hookState==SceneDirector.HookState.Swinging){
      Interactable target=null;
      foreach(var ore in director.ores){
       if(!ore||ore.collected||!ore.gameObject.activeInHierarchy)continue;
       var node=ore.GetComponent<CampaignNode>();
       if(node&&!node.bonus&&node.layer==layer&&!owner.State.minedNodes.Contains(CampaignRules.NodeId(layer,node.index))){target=ore;break;}
      }
      if(!target){Fail("演示采矿失败：主线矿点缺失");yield break;}
      // The first recovery reads clearly; subsequent recoveries use the same
      // collision and inventory settlement with shorter cable travel time.
      director.castSpeed=firstCast&&layer==1?oldCast*2.5f:Mathf.Max(oldCast,70);
      director.retrieveSpeed=firstCast&&layer==1?oldRetrieve*2.5f:Mathf.Max(oldRetrieve,65);
      if(!director.ShowcaseCastAt(target.transform.position)){Fail("演示采矿失败：抓钩无法下放");yield break;}
      firstCast=false;
     }
     yield return null;
    }
    if(!Active)yield break;
    LastSucceeded=owner.LayerComplete(layer)&&director.hookState==SceneDirector.HookState.Swinging;
    if(!LastSucceeded)Fail("演示采矿超时：矿层 "+layer+" 已回收 "+(owner.Collected(layer)-initial)+" 个矿点");
   }finally{director.castSpeed=oldCast;director.retrieveSpeed=oldRetrieve;}
  }
  public IEnumerator ForgeOne(RepairResource metal){
   Begin();var director=owner.director;var forge=director.forge;
   director.inventory.SelectedMetal=metal;
   if(!forge.IsOpen&&(!forge.OpenCamp()||!forge.OpenWorkshop())){Fail("演示锻造失败：无法打开营地工坊");yield break;}
   if(forge.State==ForgeState.CampMenu)forge.OpenWorkshop();
   int before=owner.State.bag.Count(CampaignRules.Ingot(metal));float elapsed=0;int attempts=0;
   while(Active&&elapsed<30){
    float dt=Delta;if(dt<=0){yield return null;continue;}elapsed+=dt;
    if((forge.State==ForgeState.Ready||forge.State==ForgeState.Failure)&&!forge.FailurePresentationActive){
     if(attempts++>=3||!forge.BeginForge()){Fail("演示锻造失败：原矿不足或锻造无法开始");break;}
    }
    if(forge.StrikeReady&&Mathf.Abs(forge.Pointer-forge.TargetCenter)<forge.TargetWidth*.34f)forge.SubmitStrike();
    if(forge.State==ForgeState.Success&&!forge.RewardPresentationActive){
     LastSucceeded=owner.State.bag.Count(CampaignRules.Ingot(metal))>before;
     forge.TryClose();break;
    }
    yield return null;
   }
   if(Active&&!LastSucceeded&&Failure=="")Fail("演示锻造超时：三次落锤未完成");
  }
  static Vector3 CreatureAim(AlienCreature creature){
   var collider=creature.GetComponent<Collider>();
   return collider?collider.bounds.center:creature.transform.position+Vector3.up*1.35f;
  }
  static bool Alive(AlienCreature actor){
   if(!actor||!actor.gameObject.activeInHierarchy)return false;
   var life=actor.GetComponent<CreatureVitality>();
   return life&&!life.Dead;
  }
  static bool VisibleTarget(FirstPersonExplorer player,Vector3 target,out RaycastHit hit){
   var camera=player.viewCamera;
   var delta=target-camera.transform.position;
   if(delta.sqrMagnitude>player.arsenal.ActiveRange*player.arsenal.ActiveRange){hit=default;return false;}
   return player.arsenal.Aim(camera.transform.position,delta.normalized,delta.magnitude+.1f,out hit);
  }
  static bool VisibleInFrame(FirstPersonExplorer player,Vector3 target){
   var point=player.viewCamera.WorldToViewportPoint(target);
   return point.z>0&&point.x>.07f&&point.x<.93f&&point.y>.10f&&point.y<.90f;
  }
  static bool ShotLine(FirstPersonExplorer player,out RaycastHit hit){
   var camera=player.viewCamera.transform;
   return player.arsenal.Aim(camera.position,camera.forward,player.arsenal.ActiveRange,out hit);
  }
  AlienCreature SelectThreat(FirstPersonExplorer player,out AlienCreature nearest){
   nearest=null;
   AlienCreature visible=null;
   float nearestDistance=float.PositiveInfinity,visibleDistance=float.PositiveInfinity;
   foreach(var actor in player.arena.creatures){
    if(!Alive(actor))continue;
    float distance=FlatDistance(player.transform.position,actor.transform.position);
    if(distance<nearestDistance){nearest=actor;nearestDistance=distance;}
    if(distance<visibleDistance&&distance<28&&VisibleTarget(player,CreatureAim(actor),out var hit)
     &&hit.collider.GetComponentInParent<AlienCreature>()==actor){
     visible=actor;visibleDistance=distance;
    }
   }
   return visible;
  }
  public IEnumerator ShootEncounter(float seconds=22){
   Begin();
   var player=owner.Explorer;
   if(!player){Fail("演示战斗失败：探索者尚未就绪");yield break;}
   player.Automated=true;
   var gun=player.arsenal;
   int startShots=gun.ShotsFired,startHits=gun.ConfirmedHits,burst=0;
   float elapsed=0,tracked=0,rest=0,scouted=0;
   AlienCreature target=null;
   Vector3 start=player.transform.position;
   owner.TraceShowcase("wreck-contact");
   while(Active&&elapsed<seconds){
    float dt=Delta;
    if(dt<=0){yield return null;continue;}
    elapsed+=dt;rest=Mathf.Max(0,rest-dt);
    var visible=SelectThreat(player,out var nearest);
    if(visible!=target){target=visible;tracked=0;burst=0;}
    if(!nearest)break;

    var focus=CreatureAim(target?target:nearest);
    AimAt(focus,dt);
    var contact=target?target:nearest;
    float distance=FlatDistance(player.transform.position,contact.transform.position);
    if(!target||player.arena.Safe||distance>16){
     Vector3 toward=contact.transform.position;
     // Keep the patrol in view while using the walkable route around the wreck.
     if(scouted<=0){
      encounterRoute=FindRoute(player,toward,out _,out var path)?path.corners:Array.Empty<Vector3>();
      encounterCorner=1;scouted=.45f;
     }
     scouted-=dt;
     while(encounterCorner<encounterRoute.Length-1&&FlatDistance(player.transform.position,encounterRoute[encounterCorner])<.65f)encounterCorner++;
     if(encounterCorner<encounterRoute.Length)DriveToward(player,encounterRoute[encounterCorner],dt,false);
     else player.Drive(Vector2.zero,false,false,dt);
    }else if(distance<4){
     Vector3 away=player.transform.position+(player.transform.position-contact.transform.position).normalized*3;
     if(!NavMesh.Raycast(player.transform.position,away,out _,NavMesh.AllAreas))DriveToward(player,away,dt,false);
     else player.Drive(Vector2.zero,false,false,dt);
    }else player.Drive(Vector2.zero,false,false,dt);

    if(target&&VisibleInFrame(player,focus)&&!player.arena.Safe){
     tracked+=dt;
     if(gun.ActiveMagazine<gun.ActiveCost)gun.Reload();
     if(elapsed>.8f&&tracked>.24f&&rest<=0&&ShotLine(player,out var line)
      &&line.collider.GetComponentInParent<AlienCreature>()==target&&Alive(target)){
      int before=gun.ConfirmedHits;
      if(gun.Attack()){
       owner.TraceShowcase("wreck-shot",$"target={target.kind} distance={line.distance:F1} hit={gun.ConfirmedHits>before}");
       if(++burst==3){burst=0;rest=.52f;}
      }
     }
    }else{tracked=0;burst=0;}
    if(gun.ConfirmedHits-startHits>=4&&elapsed>6&&(target==null||!Alive(target)||elapsed>11))break;
    if(player.arena.Health<=50&&gun.ReloadRemaining>0)owner.UseMedkit();
    yield return null;
   }
   if(!Active)yield break;
   player.ResetMotion();
   LastSucceeded=gun.ConfirmedHits>startHits;
   owner.TraceShowcase("wreck-contact-end",$"shots={gun.ShotsFired-startShots} hits={gun.ConfirmedHits-startHits} moved={FlatDistance(start,player.transform.position):F1}");
   if(!LastSucceeded)Fail("演示交火未命中：请检查怪物路线与遮挡");
  }
  Vector3[] encounterRoute=Array.Empty<Vector3>();
  int encounterCorner;

  bool FindDodge(FirstPersonExplorer player,CampaignBoss boss,out Vector3[] route){
   route=null;
   float shortest=float.PositiveInfinity;
   var p=player.transform.position;
   foreach(float offset in new[]{-6f,6f,-18f,18f}){
    foreach(float z in new[]{0f,-2.5f,2.5f}){
     var point=new Vector3(boss.WarningCenter.x+offset,p.y,p.z+z);
     if(!FindRoute(player,point,out var stand,out var path,1.3f)||boss.InWarning(stand,.75f))continue;
     var corners=path.corners;
     float distance=RouteLength(corners);
     if(distance<shortest){shortest=distance;route=corners;}
    }
   }
   return route!=null;
  }
  public IEnumerator FightGuardian(float seconds=100){
   Begin();
   var player=owner.Explorer;
   var boss=owner.RegionView?owner.RegionView.boss:null;
   if(!player||!boss){Fail("演示决战失败：守核机甲尚未就绪");yield break;}
   var center=boss.transform.position;
   if(FlatDistance(player.transform.position,center)>14){
    yield return MoveTo(center+new Vector3(-3.8f,0,-8),20,1.2f);
    if(!Active||!LastSucceeded)yield break;
    Begin();
   }
   player.Automated=true;
   var gun=player.arsenal;
   var parts=boss.GetComponentsInChildren<CampaignBossPart>(true);
   var centerView=center+Vector3.up*1.6f;
   float elapsed=0,resumeFire=0,nextPlan=0;
   int attacks=boss.Attacks,warningHealth=player.arena.Health,dodges=0;
   bool warningStarted=false,startedInLane=false;
   Vector3[] dodgeRoute=null;
   int dodgeCorner=0;
   owner.TraceShowcase("guardian-contact");
   while(Active&&elapsed<seconds&&!boss.Dead){
    float dt=Delta;
    if(dt<=0){yield return null;continue;}
    elapsed+=dt;resumeFire=Mathf.Max(0,resumeFire-dt);nextPlan-=dt;
    if(boss.Attacks!=attacks){
     bool evaded=warningStarted&&startedInLane&&!boss.LastAttackInWarning&&player.arena.Health==warningHealth;
     if(evaded)dodges++;
     owner.TraceShowcase("guardian-impact",$"attack={boss.Attacks} in-lane={boss.LastAttackInWarning} damage={warningHealth-player.arena.Health} evaded={evaded} position={player.transform.position}");
     if(evaded&&dodges==1)owner.ShowcaseDirection("炮击落空。转回机甲，重新寻找射击窗口。");
     attacks=boss.Attacks;resumeFire=.48f;
    }
    if(boss.Telegraphing){
     if(!warningStarted){
      startedInLane=boss.InWarning(player.transform.position);
      warningHealth=player.arena.Health;
      FindDodge(player,boss,out dodgeRoute);dodgeCorner=1;nextPlan=.5f;
      owner.TraceShowcase("guardian-warning",$"attack={attacks+1} center={boss.WarningCenter} position={player.transform.position} path={dodgeRoute!=null}");
      if(dodges==0)owner.ShowcaseDirection("地面标出炮击范围。停止开火，跑向两条预警之间的空地。");
     }else if(nextPlan<=0&&boss.InWarning(player.transform.position,.3f)){
      FindDodge(player,boss,out dodgeRoute);dodgeCorner=1;nextPlan=.5f;
     }
     warningStarted=true;
     AimAt(centerView,dt);
     if(dodgeRoute!=null&&boss.InWarning(player.transform.position,.45f)){
      while(dodgeCorner<dodgeRoute.Length-1&&FlatDistance(player.transform.position,dodgeRoute[dodgeCorner])<.65f)dodgeCorner++;
      if(dodgeCorner<dodgeRoute.Length)DriveToward(player,dodgeRoute[dodgeCorner],dt);
     }else player.Drive(Vector2.zero,false,false,dt);
     yield return null;continue;
    }
    warningStarted=false;
    if(resumeFire>0){AimAt(centerView,dt);player.Drive(Vector2.zero,false,false,dt);yield return null;continue;}

    CampaignBossPart target=null;
    foreach(var part in parts){
     if(!part.gameObject.activeInHierarchy)continue;
     if(boss.NodesRemaining>0&&part.node>=0||boss.NodesRemaining==0&&part.node==-1){target=part;break;}
    }
    if(target){
     var collider=target.GetComponent<Collider>();
     Vector3 aim=collider?collider.bounds.center:target.transform.position;
     bool canDamage=target.node>=0||boss.WeakOpen;
     AimAt(canDamage?aim:centerView,dt);
     Vector3 firingPoint=new Vector3(target.transform.position.x,player.transform.position.y,center.z-6.8f);
     bool clear=canDamage&&VisibleTarget(player,aim,out var direct)&&direct.collider.GetComponent<CampaignBossPart>()==target;
     if(!clear&&FlatDistance(player.transform.position,firingPoint)>1.0f)DriveToward(player,firingPoint,dt,false);
     else player.Drive(Vector2.zero,false,false,dt);
     if(gun.ActiveMagazine<gun.ActiveCost||!canDamage&&gun.ActiveMagazine<=gun.ActiveCapacity/2)gun.Reload();
     if(clear&&VisibleInFrame(player,aim)&&ShotLine(player,out var shot)&&shot.collider.GetComponent<CampaignBossPart>()==target){
      int before=gun.ConfirmedHits,nodes=boss.NodesRemaining;
      if(gun.Attack()){
       owner.TraceShowcase("guardian-shot",$"part={target.node} hit={gun.ConfirmedHits>before} nodes={boss.NodesRemaining} health={boss.Health}");
       if(nodes!=boss.NodesRemaining)
        owner.ShowcaseDirection(boss.NodesRemaining>0?$"供能节点已破坏。还剩 {boss.NodesRemaining} 个。":"供能已切断。等胸口核心露出，再集中射击。");
      }
     }
    }
    if(player.arena.Health<=50)owner.UseMedkit();
    yield return null;
   }
   if(!Active)yield break;
   player.ResetMotion();
   GuardianDodges=dodges;
   LastSucceeded=boss.Dead&&owner.State.Has("boss-defeated")&&dodges>0;
   owner.TraceShowcase("guardian-end",$"health={boss.Health} nodes={boss.NodesRemaining} dodges={dodges}");
   if(!LastSucceeded)Fail($"演示决战未完成：机甲生命 {boss.Health}，供能节点 {boss.NodesRemaining}，有效躲避 {dodges}");
  }
 }
}
