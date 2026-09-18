using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed class EncounterVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();string folder;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-encounterQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/encounter-v7"));Directory.CreateDirectory(folder);
   yield return new WaitForSeconds(1);
   var d=GetComponent<SceneDirector>();var e=d.encounter;var inventory=d.inventory;
   // Verify the unbound encounter contract in isolation; -battleQA covers the connected scene flow.
   if(d.battle)d.battle.enabled=false;
   var target=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.DormantEncounter);
   var material=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.RepairMaterial);
   checks["ordinaryOreStillAddsInventory"]=d.AwardRecoveredOre(material.GetComponent<BuriedSample>().Definition,99001)&&inventory.UnknownCount==1;
   yield return d.Travel(true);
   foreach(var o in d.ores)o.collected=true;target.collected=false;
   var direction=(d.hook.position-d.winchAnchor.position).normalized;target.transform.position=d.winchAnchor.position+direction*5;
   int notices=0;EncounterRequest request=null;float notifiedAt=-1;
   e.BattleRequested+=r=>{notices++;request=r;notifiedAt=e.AlertAge;};
   checks["realHookCast"]=d.Cast();float deadline=Time.time+14;
   while(!e.IsPresenting&&Time.time<deadline)yield return null;
   checks["alertOnlyAfterHookReturns"]=e.IsPresenting&&!target.gameObject.activeSelf&&d.hookState==SceneDirector.HookState.Swinging;
   checks["encounterDoesNotGiveOre"]=inventory.UnknownCount==1&&!d.PickupPopupVisible;
   checks["noEarlyBattleDispatch"]=notices==0;
   var oldPosition=d.player.position;d.Move(1,1);
   checks["alertBlocksMovementHookAndCamp"]=d.player.position==oldPosition&&!d.Cast()&&!d.forge.OpenCamp();
   int flashes=0;bool previous=false,sawGap=false;
   deadline=Time.time+3;
   while(e.IsPresenting&&Time.time<deadline){bool visible=e.ExclamationVisible;if(visible&&!previous)flashes++;if(!visible&&flashes==1)sawGap=true;previous=visible;yield return null;}
   checks["exactlyTwoFlashes"]=flashes==2&&sawGap;
   checks["battleInterfaceAfterTwoFlashes"]=notices==1&&notifiedAt>=.94f;
   var definition=target.GetComponent<BuriedSample>().Definition;
   checks["battleRequestCarriesContext"]=request!=null&&request.source==definition&&request.sourceId==target.GetInstanceID()&&request.encounterId==definition.encounterId&&request.fuelRewardId==definition.fuelRewardId;
   checks["recoveryCannotDispatchTwice"]=!d.AwardRecoveredOre(definition,target.GetInstanceID())&&notices==1;
   checks["encountersCannotEnterForgeInventory"]=!inventory.AddRecovered(definition,99002)&&inventory.UnknownCount==1;
   checks["unboundBattleInterfaceDoesNotLockExploration"]=!d.RecoveryLocked&&!e.IsPresenting;
   // A second distinct sample captures the visible presentation after behavior is verified.
   d.AwardRecoveredOre(definition,99003);yield return Capture("01-exclamation-first");
   while(e.IsPresenting&&e.AlertAge<.31f)yield return null;yield return Capture("02-exclamation-gap");
   while(e.IsPresenting&&e.AlertAge<.48f)yield return null;yield return Capture("03-exclamation-second");
   while(e.IsPresenting)yield return null;
   bool pass=true;string json="{\n";foreach(var check in checks){pass&=check.Value;json+="  \""+check.Key+"\":"+(check.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+"\n}";
   File.WriteAllText(folder+"/encounter-report.json",json);Debug.Log(pass?"STARFALL_ENCOUNTER_QA_PASS":"STARFALL_ENCOUNTER_QA_FAIL");Application.Quit(pass?0:2);
  }
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+name+".png",t.EncodeToPNG());Destroy(t);}}
 }
}
