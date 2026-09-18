using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public sealed class WormholeVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<string> frameTimes=new List<string>();string folder;bool recording,reload;int frames;SceneDirector d;ExpeditionWormhole portal;BattleTravel travel;
  IEnumerator Start(){
   reload=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-portalReloadQA");
   if(!reload&&!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-portalQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/wormhole-v12"));Directory.CreateDirectory(folder+"/frames");
   yield return new WaitForSeconds(1);d=GetComponent<SceneDirector>();portal=d.wormhole;travel=d.battle;
   if(reload){yield return ReloadCheck();yield break;}
   int originalProgress=PlayerPrefs.GetInt(ExpeditionWormhole.ProgressKey,-1);
   PlayerPrefs.DeleteKey(portal.StorageKey);PlayerPrefs.Save();portal.ReloadSavedState();
   checks["lockedBeforeFirstVisit"]=!portal.Unlocked&&!portal.portalVisual.activeSelf&&!portal.TryEnter();
   yield return Capture("01-before-discovery");
   var ore=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.RepairMaterial);
   d.AwardRecoveredOre(ore.GetComponent<BuriedSample>().Definition,99201);int inventory=d.inventory.UnknownCount;
   var enemy=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.DormantEncounter);
   d.AwardRecoveredOre(enemy.GetComponent<BuriedSample>().Definition,99202);
   checks["discoveryStillPlaysEncounterAlert"]=d.encounter.IsPresenting&&!portal.Unlocked;
   yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,18);
   if(!travel.Explorer){checks["firstVisitLoadsWorld"]=false;Finish();yield break;}
   var fps=travel.Explorer;var arena=fps.arena;
   checks["unlockOnlyAfterSuccessfulArrival"]=portal.Unlocked&&PlayerPrefs.GetInt(portal.StorageKey,0)==1;
   checks["portalHiddenInOtherWorld"]=!portal.portalVisual.activeSelf&&!portal.TryEnter();
   int ground=0,flyers=0;float minGap=float.MaxValue;bool clear=true,connected=true;var path=new NavMeshPath();
   NavMesh.SamplePosition(fps.transform.position,out var entry,8,NavMesh.AllAreas);
   for(int i=0;i<arena.creatures.Length;i++){
    var c=arena.creatures[i];if(c.kind==CreatureKind.RiftMauler)ground++;else flyers++;
    clear&=CreatureArena.FlatDistance(c.Home,arena.beacon.position)>=28;
    connected&=c.Agent.isOnNavMesh&&NavMesh.SamplePosition(c.Home,out var hit,8,NavMesh.AllAreas)&&NavMesh.CalculatePath(entry.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
    for(int j=0;j<i;j++)minGap=Mathf.Min(minGap,CreatureArena.FlatDistance(c.Home,arena.creatures[j].Home));
   }
   checks["threeOfEachSpecies"]=ground==3&&flyers==3&&arena.creatures.Length==6;
   checks["spawnSpacingAtLeast32Meters"]=minGap>=31.9f;
   checks["entryHas28MeterSpawnClearance"]=clear;
   checks["allSixHaveConnectedNavigation"]=connected;
   checks["giantStillPresent"]=UnityEngine.Object.FindFirstObjectByType<SkyTitan>()!=null;
   Debug.Log("Population: ground="+ground+", airborne="+flyers+", minimum home spacing="+minGap);
   var prior=new Vector3[6];var distance=new float[6];for(int i=0;i<6;i++)prior[i]=arena.creatures[i].transform.position;
   float end=Time.realtimeSinceStartup+8;bool limited=true,territories=true;
   while(Time.realtimeSinceStartup<end){yield return null;limited&=arena.PursuerCount<=3;for(int i=0;i<6;i++){var c=arena.creatures[i];distance[i]+=CreatureArena.FlatDistance(prior[i],c.transform.position);prior[i]=c.transform.position;territories&=CreatureArena.FlatDistance(c.Home,c.transform.position)<18;}}
   bool moving=true;for(int i=0;i<6;i++){moving&=distance[i]>2;Debug.Log("Patrol "+i+": "+distance[i]);}
   checks["allSixPatrolWithoutCrowdingArrival"]=moving&&territories&&arena.Safe&&arena.Health==100;
   var position=fps.transform.position;fps.Motor.enabled=false;fps.transform.position=new Vector3(0,AlienSurfaceLandmarks.Height(0,9)+.1f,9);fps.Motor.enabled=true;Physics.SyncTransforms();
   int reserved=0;foreach(var c in arena.creatures)if(arena.TryEngage(c))reserved++;
   checks["atMostThreeCanReservePursuit"]=limited&&reserved==3&&arena.PursuerCount==3;
   arena.Release(arena.creatures[0]);checks["releasedPursuitSlotCanBeReused"]=arena.TryEngage(arena.creatures[3])&&arena.PursuerCount==3;
   foreach(var c in arena.creatures)arena.Release(c);fps.Motor.enabled=false;fps.transform.position=position;fps.Motor.enabled=true;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,12);yield return null;
   checks["wormholeAppearsOnMainScene"]=portal.portalVisual.activeInHierarchy&&d.wormholeSite.kind==SiteKind.Wormhole&&d.wormholeSite.transform.position.x>0;
   checks["initialReturnPreservesInventory"]=d.inventory.UnknownCount==inventory;
   yield return new WaitForSeconds(.5f);yield return Capture("02-unlocked-wormhole");
   portal.ReloadSavedState();checks["unlockCanBeLoadedFromSavedProgress"]=portal.Unlocked&&portal.CanEnter;
   Time.timeScale=0;checks["pausedEntryBlocked"]=!portal.TryEnter();Time.timeScale=1;
   d.player.position=new Vector3(d.camp.transform.position.x,d.surfaceY,0);d.forge.OpenCamp();checks["forgeEntryBlocked"]=d.forge.IsOpen&&!portal.TryEnter();d.forge.TryClose();
   recording=true;StartCoroutine(Record());d.player.position=new Vector3(d.wormholeSite.transform.position.x,d.surfaceY,0);yield return new WaitForSeconds(2.5f);yield return Capture("03-nearby-portal-interaction");
   d.Interact();checks["nearbyEInteractionEntersPortal"]=travel.IsEngaged&&!portal.TryEnter();
   yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);if(travel.Explorer)travel.Explorer.Look(0,16);yield return new WaitForSeconds(2);yield return Capture("04-world-from-wormhole");
   checks["reentryHasSixMonstersAndOneGiant"]=travel.Explorer&&travel.Explorer.arena.creatures.Length==6&&UnityEngine.Object.FindObjectsByType<SkyTitan>(FindObjectsSortMode.None).Length==1;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,12);yield return new WaitForSeconds(1);recording=false;
   var transition=d.StartCoroutine(d.Travel(true));checks["cameraTransitionBlocksPortal"]=d.Transitioning&&!portal.TryEnter();yield return transition;
   float oldMax=d.maxCableLength,oldCast=d.castSpeed,oldRetrieve=d.retrieveSpeed;d.maxCableLength=1.2f;d.castSpeed=100;d.retrieveSpeed=100;d.Cast();checks["busyHookBlocksPortal"]=d.hookState!=SceneDirector.HookState.Swinging&&!portal.TryEnter();yield return Wait(()=>d.hookState==SceneDirector.HookState.Swinging,5);d.maxCableLength=oldMax;d.castSpeed=oldCast;d.retrieveSpeed=oldRetrieve;
   checks["quickEntryWorksFromUnderground"]=d.underground&&portal.TryEnter();yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,12);
   checks["returnRestoresUndergroundAndInventory"]=d.underground&&d.enabled&&d.inventory.UnknownCount==inventory;
   checks["repeatedTravelDoesNotDuplicatePortal"]=UnityEngine.Object.FindObjectsByType<ExpeditionWormhole>(FindObjectsSortMode.None).Length==1&&portal.portalVisual.activeSelf;
   checks["verificationDoesNotAlterRealProgress"]=PlayerPrefs.GetInt(ExpeditionWormhole.ProgressKey,-1)==originalProgress;
   File.WriteAllLines(folder+"/frame-times.txt",frameTimes);Finish();
  }
  IEnumerator ReloadCheck(){
   checks["separateProcessRetainsUnlock"]=portal.Unlocked&&PlayerPrefs.GetInt(portal.StorageKey,0)==1;
   checks["portalVisibleAtFreshLaunch"]=portal.portalVisual.activeInHierarchy;
   checks["freshLaunchCanEnterWithoutMining"]=portal.TryEnter();yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);checks["savedPortalLoadsWorld"]=travel.Explorer&&travel.Explorer.arena.creatures.Length==6;
   if(travel.IsEngaged){travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,12);}
   PlayerPrefs.DeleteKey(portal.StorageKey);PlayerPrefs.Save();Finish();
  }
  IEnumerator Wait(Func<bool> condition,float duration){float end=Time.realtimeSinceStartup+duration;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+name+".png",t.EncodeToPNG());Destroy(t);}}
  IEnumerator Record(){while(recording&&frames<160){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){frameTimes.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frames++).ToString("D4")+".png",t.EncodeToPNG());Destroy(t);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frames+"\n}";File.WriteAllText(folder+"/"+(reload?"reload-report":"wormhole-report")+".json",json);Debug.Log(pass?"STARFALL_WORMHOLE_QA_PASS":"STARFALL_WORMHOLE_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
