using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Starfall {
 // Called only by the independent project audit runner. Scene corruption is
 // injected into the newly loaded QA instance; no scene asset is modified.
 public static class CombatAuditChecks {
  public static IEnumerator Run(CampaignRuntime campaign,Action<string,bool> check){
   check("Combat audit uses an independent QA campaign",campaign&&campaign.IsQA);
   if(!campaign||!campaign.IsQA)yield break;
   var director=campaign.director;var travel=director.battle;
   if(!campaign.State.coordinates.Contains(1))campaign.State.coordinates.Add(1);
   campaign.Close();director.SetPaused(false);
   var fog=RenderSettings.fogColor;int aa=QualitySettings.antiAliasing;
   Time.timeScale=.75f;bool injected=false;
   UnityEngine.Events.UnityAction<Scene,LoadSceneMode> corrupt=(scene,mode)=>{
    if(scene.name!="Scene03_WreckRift")return;
    foreach(var root in scene.GetRootGameObjects()){
     var explorer=root.GetComponentInChildren<FirstPersonExplorer>(true);if(!explorer)continue;
     explorer.gameObject.SetActive(false);explorer.viewCamera=null;injected=true;break;
    }
   };
   SceneManager.sceneLoaded+=corrupt;
   try{
    check("Broken-scene transition was requested",campaign.EnterRegion(1));
    yield return Until(()=>!travel.IsEngaged&&!travel.IsTransitioning,18);
   }finally{SceneManager.sceneLoaded-=corrupt;}
   check("Broken explorer was injected into runtime scene only",injected);
   check("Invalid encounter rolls back transition and reports its failure",!travel.IsEngaged&&!travel.IsTransitioning&&!travel.Explorer&&!string.IsNullOrEmpty(travel.TransitionError));
   check("Failed transition restores source view and campaign region",director.enabled&&director.worldCamera.enabled&&campaign.State.region==0&&!SceneManager.GetSceneByName("Scene03_WreckRift").isLoaded);
   check("Failed transition restores time, graphics and environment",Mathf.Abs(Time.timeScale-.75f)<.001f&&QualitySettings.antiAliasing==aa&&RenderSettings.fogColor==fog);
   Time.timeScale=1;
   if(travel.IsEngaged)yield break;
   check("Healthy encounter can load after rollback",campaign.EnterRegion(1));
   yield return Until(()=>travel.Explorer&&!travel.IsTransitioning,18);
   var player=travel.Explorer;
   check("Retry clears the previous transition error",player&&!travel.IsTransitioning&&travel.TransitionError=="");
   if(!player)yield break;
   yield return Until(()=>player.expedition.Initialized,3);
   player.Automated=true;
   foreach(var creature in player.arena.creatures){if(!creature)continue;creature.enabled=false;if(creature.Agent&&creature.Agent.isOnNavMesh)creature.Agent.isStopped=true;}
   var recovered=player.expedition.AddDrop(BasinLootKind.Fuel,new Vector3(10000,-10000,10000));yield return null;
   // The beacon origin is inside its solid column. Approach the actual relocated
   // item from a navigable point, as a walking player would, instead of placing
   // the camera and controller inside the beacon geometry.
   var approach=recovered.data.position-Vector3.up*.6f+Vector3.back*1.25f;
   bool navigable=NavMesh.SamplePosition(approach,out var standing,.75f,NavMesh.AllAreas);
   check("Relocated beacon loot has a navigable approach",navigable);
   Warp(player,navigable?standing.position+Vector3.up*.08f:approach);Face(player,recovered.transform.position);Physics.SyncTransforms();
   check("Unreachable loot is relocated inside the beacon's safe radius",CreatureArena.FlatDistance(recovered.transform.position,player.returnBeacon.position)<6&&player.arena.Safe);
   bool aimed=Physics.Raycast(player.viewCamera.transform.position,player.viewCamera.transform.forward,out var aimedHit,3.3f,~(1<<30),QueryTriggerInteraction.Ignore)&&aimedHit.collider.GetComponentInParent<BasinPickup>()==recovered;
   check("Beacon pickup fixture aims at the real supply collider",aimed);
   player.SetPaused(true);check("Paused beacon pickup cannot change inventory",!player.expedition.Collect(recovered)&&!recovered.data.collected);player.SetPaused(false);
   int beforeFuel=campaign.State.bag.Count(ItemId.Fuel);
   bool attempted=player.expedition.TryCollectAimedPickup();int afterFuel=campaign.State.bag.Count(ItemId.Fuel);
   Debug.Log("COMBAT_AUDIT_BEACON_PICKUP aimed="+aimed+" hit="+(aimedHit.collider?aimedHit.collider.name:"none")+" distance="+Vector3.Distance(player.viewCamera.transform.position,recovered.transform.position)+" visible="+player.expedition.Visible(recovered.transform.position,recovered.transform)+" attempted="+attempted+" collected="+recovered.data.collected+" fuel="+beforeFuel+"->"+afterFuel);
   check("Aimed beacon pickup collects real relocated loot",attempted&&recovered.data.collected&&afterFuel==beforeFuel+2);
   check("Aimed pickup takes priority over beacon departure",travel.IsEngaged&&!travel.IsTransitioning&&player.arena.Safe);
   Warp(player,player.returnBeacon.position+Vector3.forward*10);
   player.Look(0,player.Pitch+60);
   var gun=player.arsenal;
   check("Regression setup spends a real pistol round",gun.Attack());
   yield return new WaitForSecondsRealtime(.4f);
   Warp(player,player.returnBeacon.position);
   int magazine=gun.Magazine,reserve=gun.Reserve;
   check("Beacon still blocks outgoing attacks",player.arena.Safe&&!gun.Attack()&&!gun.Attack(true));
   check("An incomplete magazine can reload at the safe beacon",gun.Reload());
   player.SetPaused(true);float remaining=gun.ReloadRemaining;
   yield return new WaitForSecondsRealtime(.35f);
   check("Pause freezes a beacon reload without consuming rounds",Mathf.Abs(gun.ReloadRemaining-remaining)<.001f&&gun.Magazine==magazine&&gun.Reserve==reserve);
   player.SetPaused(false);
   yield return Until(()=>gun.ReloadRemaining<=0,5);
   check("Beacon reload transfers the exact reserve quantity",gun.Magazine==gun.ActiveCapacity&&gun.Reserve==reserve-(gun.ActiveCapacity-magazine));
   Warp(player,player.returnBeacon.position+Vector3.forward*10);
   check("Second regression setup spends a real round",gun.Attack());yield return new WaitForSecondsRealtime(.4f);
   Warp(player,player.returnBeacon.position);player.SetPaused(true);
   check("Paused reload remains rejected",!gun.Reload()&&gun.ReloadRemaining==0);
   player.SetPaused(false);
   var originalBag=campaign.State.bag;
   try{
    campaign.State.bag=new ItemContainer(16);campaign.State.bag.Add(ItemId.Ammo,198);
    gun.AddAmmo(1);check("A story ammo grant preserves reserves above the old preview cap",gun.Reserve==199);
    gun.AddAmmo(0);gun.AddAmmo(-5);check("Zero and negative ammo grants cannot discard existing ammunition",gun.Reserve==199);
    campaign.State.bag=new ItemContainer(16);for(int i=0;i<16;i++)campaign.State.bag.stacks.Add(new ItemStack(ItemId.StarOre,99));
    gun.AddAmmo(18);check("An ammo grant cannot corrupt or exceed a full inventory",campaign.State.bag.stacks.Count==16&&campaign.State.bag.Count(ItemId.StarOre)==16*99&&gun.Reserve==0);
   }finally{campaign.State.bag=originalBag;gun.Sync();}
   travel.ReturnToExpedition();yield return Until(()=>!travel.IsEngaged,12);
   check("Combat audit returns to the original scene",!travel.IsEngaged&&!travel.IsTransitioning&&director.enabled&&campaign.State.region==0);
  }
  static IEnumerator Until(Func<bool> condition,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
  static void Warp(FirstPersonExplorer player,Vector3 point){
   player.Motor.enabled=false;player.transform.position=point;player.Motor.enabled=true;player.ResetMotion();Physics.SyncTransforms();
  }
  static void Face(FirstPersonExplorer player,Vector3 point){
   var delta=point-player.viewCamera.transform.position;player.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);
   player.Look(0,player.Pitch);player.Look(0,Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);
  }
 }
}
