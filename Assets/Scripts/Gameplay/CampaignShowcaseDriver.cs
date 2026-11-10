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
   player.Look(Mathf.Clamp(Mathf.DeltaAngle(player.transform.eulerAngles.y,desiredYaw),-220*dt,220*dt),
    Mathf.Clamp(player.Pitch-desiredPitch,-150*dt,150*dt));
  }
  void DriveToward(FirstPersonExplorer player,Vector3 point,float dt,bool running=true){
   var delta=point-player.transform.position;delta.y=0;
   var local=player.transform.InverseTransformDirection(delta.normalized);
   player.Drive(new Vector2(local.x,local.z),running,false,Mathf.Min(dt,.05f));
  }
  static float FlatDistance(Vector3 a,Vector3 b)=>CreatureArena.FlatDistance(a,b);
  public IEnumerator MoveTo(Vector3 goal,float seconds=12){
   Begin();var player=owner.Explorer;if(!player){Fail("演示行走失败：探索者尚未就绪");yield break;}
   player.Automated=true;
   var path=new NavMeshPath();Vector3 target=goal;
   if(NavMesh.SamplePosition(goal,out var sampled,3,NavMesh.AllAreas))target=sampled.position;
   bool hasPath=NavMesh.CalculatePath(player.transform.position,target,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
   var corners=hasPath?path.corners:new[]{target};int next=0;float elapsed=0,stuck=0;var previous=player.transform.position;
   while(Active&&elapsed<seconds){
    float dt=Delta;if(dt<=0){yield return null;continue;}elapsed+=dt;
    if(FlatDistance(player.transform.position,target)<1.1f&&Mathf.Abs(player.transform.position.y-target.y)<1.8f){LastSucceeded=true;break;}
    while(next<corners.Length-1&&FlatDistance(player.transform.position,corners[next])<1)next++;
    Vector3 step=corners.Length==0?target:corners[Mathf.Min(next,corners.Length-1)];
    AimAt(step+Vector3.up*1.4f,dt);DriveToward(player,step,dt);
    if(FlatDistance(player.transform.position,previous)<.003f)stuck+=dt;else stuck=Mathf.Max(0,stuck-dt*.6f);
    previous=player.transform.position;
    if(player.arena.Health<45)owner.UseMedkit();
    if(stuck>2.5f)break;
    yield return null;
   }
   if(!Active)yield break;
   if(!LastSucceeded){
    // Navigation is allowed to compress uneventful travel, but never resolve an
    // interaction. The caller still has to pass the real Near/visibility check.
    owner.Notice("演示转场 · 压缩重复赶路");Warp(player,target);
    yield return null;
    LastSucceeded=FlatDistance(player.transform.position,target)<1.8f;
    if(!LastSucceeded)Fail("演示行走失败：无法抵达主线交互点");
   }
   player.ResetMotion();
  }
  public IEnumerator MineLayer(float seconds=18){
   Begin();var director=owner.director;int layer=owner.State.layer;
   if(!director.underground){Fail("演示采矿失败：尚未进入矿井");yield break;}
   float oldCast=director.castSpeed,oldRetrieve=director.retrieveSpeed;
   float elapsed=0;int initial=owner.Collected(layer);bool firstCast=true;
   try{
    while(Active&&elapsed<Mathf.Max(45,seconds)&&!owner.LayerComplete(layer)){
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
    while(Active&&LastSucceeded&&elapsed<seconds){elapsed+=Delta;yield return null;}
   }finally{director.castSpeed=oldCast;director.retrieveSpeed=oldRetrieve;}
  }
  public IEnumerator ForgeOne(RepairResource metal,float seconds=12){
   Begin();var director=owner.director;var forge=director.forge;
   director.inventory.SelectedMetal=metal;
   if(!forge.IsOpen&&(!forge.OpenCamp()||!forge.OpenWorkshop())){Fail("演示锻造失败：无法打开营地工坊");yield break;}
   if(forge.State==ForgeState.CampMenu)forge.OpenWorkshop();
   int before=owner.State.bag.Count(CampaignRules.Ingot(metal));float elapsed=0;int attempts=0;
   while(Active&&elapsed<Mathf.Max(30,seconds)){
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
   while(Active&&LastSucceeded&&elapsed<seconds){elapsed+=Delta;yield return null;}
  }
  static Vector3 CreatureAim(AlienCreature creature){
   var collider=creature.GetComponent<Collider>();
   return collider?collider.bounds.center:creature.transform.position+Vector3.up*1.35f;
  }
  public IEnumerator ShootEncounter(float seconds=18){
   Begin();var player=owner.Explorer;if(!player){Fail("演示战斗失败：探索者尚未就绪");yield break;}
   player.Automated=true;int hits=player.arsenal.ConfirmedHits;float elapsed=0;
   while(Active&&elapsed<seconds){
    float dt=Delta;if(dt<=0){yield return null;continue;}elapsed+=dt;
    AlienCreature target=null;float nearest=float.PositiveInfinity;
    foreach(var actor in player.arena.creatures){
     if(!actor||!actor.gameObject.activeInHierarchy)continue;var life=actor.GetComponent<CreatureVitality>();
     if(!life||life.Dead)continue;float distance=FlatDistance(player.transform.position,actor.transform.position);
     if(distance<nearest){target=actor;nearest=distance;}
    }
    if(!target){player.Drive(Vector2.zero,false,false,dt);yield return null;continue;}
    Vector3 aim=CreatureAim(target);AimAt(aim,dt);
    if(player.arena.Safe||nearest>15)DriveToward(player,target.transform.position,dt);
    else{
     Vector2 strafe=new Vector2(Mathf.Sin(elapsed*1.2f)>.1f?.65f:-.65f,nearest<6?-.6f:0);
     player.Drive(strafe,false,false,dt);
    }
    if(player.arsenal.ActiveMagazine==0)player.arsenal.Reload();
    if(Vector3.Dot(player.viewCamera.transform.forward,(aim-player.viewCamera.transform.position).normalized)>.992f)player.arsenal.Attack();
    if(player.arena.Health<55)owner.UseMedkit();
    yield return null;
   }
   if(!Active)yield break;
   player.ResetMotion();LastSucceeded=player.arsenal.ConfirmedHits>hits;
   if(!LastSucceeded)Fail("演示交火未命中：请检查怪物路径与遮挡");
  }
  public IEnumerator FightGuardian(float seconds=60){
   Begin();var player=owner.Explorer;var boss=owner.RegionView?owner.RegionView.boss:null;
   if(!player||!boss){Fail("演示决战失败：守核机甲尚未就绪");yield break;}
   // Follow the baked route into the arena before engaging. Walking straight
   // from the arrival beacon can otherwise catch on a terrain ledge or shelter.
   if(FlatDistance(player.transform.position,boss.transform.position)>14){
    var approach=boss.transform.position+new Vector3(-3.8f,0,-7);
    if(NavMesh.SamplePosition(approach,out var stand,4,NavMesh.AllAreas))approach=stand.position;
    yield return MoveTo(approach,8);
    if(!Active||!LastSucceeded)yield break;
    Begin();
   }
   player.Automated=true;float elapsed=0;bool wasTelling=false;Vector3 dodge=player.transform.position;
   while(Active&&elapsed<Mathf.Max(60,seconds)&&!boss.Dead){
    float dt=Delta;if(dt<=0){yield return null;continue;}elapsed+=dt;
    CampaignBossPart target=null;
    foreach(var part in boss.GetComponentsInChildren<CampaignBossPart>()){
     if(!part.gameObject.activeInHierarchy)continue;
     if(boss.NodesRemaining>0&&part.node>=0){target=part;break;}
     if(boss.NodesRemaining==0&&part.node==-1){target=part;break;}
    }
    if(target){
     var collider=target.GetComponent<Collider>();Vector3 aim=collider?collider.bounds.center:target.transform.position;
     AimAt(aim,dt);
     if(boss.Telegraphing&&!wasTelling){dodge=player.transform.position;dodge.x+=player.transform.position.x<0?6:-6;}
     if(boss.Telegraphing){if(FlatDistance(player.transform.position,dodge)>.6f)DriveToward(player,dodge,dt);else player.Drive(Vector2.zero,false,false,dt);}
     else{
      Vector3 firingPoint=new Vector3(target.transform.position.x,player.transform.position.y,boss.transform.position.z-6);
      if(FlatDistance(player.transform.position,firingPoint)>1.1f)DriveToward(player,firingPoint,dt);else player.Drive(Vector2.zero,false,false,dt);
     }
     if(player.arsenal.ActiveMagazine==0)player.arsenal.Reload();
     if((target.node>=0||boss.WeakOpen)&&Vector3.Dot(player.viewCamera.transform.forward,(aim-player.viewCamera.transform.position).normalized)>.997f)player.arsenal.Attack();
    }
    wasTelling=boss.Telegraphing;
    if(player.arena.Health<70)owner.UseMedkit();
    yield return null;
   }
   if(!Active)yield break;
   player.ResetMotion();LastSucceeded=boss.Dead&&owner.State.Has("boss-defeated");
   if(!LastSucceeded)Fail("演示决战超时：机甲生命 "+boss.Health+"，供能节点 "+boss.NodesRemaining);
  }
 }
}
