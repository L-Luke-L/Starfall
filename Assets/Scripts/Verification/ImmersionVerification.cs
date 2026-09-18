using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 public sealed class ImmersionVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<string> times=new List<string>();
  string folder;int frames;bool recording;SceneDirector d;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-immersionQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/immersion-v15"));Directory.CreateDirectory(folder+"/frames");d=GetComponent<SceneDirector>();yield return new WaitForSeconds(4.3f);
   int sound=InteractionFeel.Played;d.Move(1,.5f);checks["footstepFeedbackOnMovement"]=InteractionFeel.Played>sound;yield return Capture("01-quiet-surface");
   d.SetPaused(true);float cable=d.cableLength;checks["pauseBlocksHookInput"]=!d.Cast();yield return new WaitForSecondsRealtime(.3f);checks["pauseFreezesWorldHook"]=d.cableLength==cable;yield return Capture("02-surface-pause");d.SetPaused(false);
   InteractionFeel.Muted=true;sound=InteractionFeel.Played;InteractionFeel.Play(FeelCue.Shot);checks["muteCoversAllAudio"]=AudioListener.volume==0&&InteractionFeel.Played==sound;InteractionFeel.Muted=false;checks["unmuteRestoresAudio"]=AudioListener.volume==1;
   d.inventory.AddGathered(RepairResource.CopperCompound);d.inventory.AddGathered(RepairResource.CobaltSilicate);d.player.position=new Vector3(d.camp.transform.position.x,d.surfaceY,0);d.Interact();yield return null;checks["campStillUsesProximityInteraction"]=d.forge.State==ForgeState.CampMenu;yield return Capture("03-camp-in-world");
   d.forge.OpenWorkshop();yield return new WaitForSeconds(.3f);yield return Capture("04-workshop-minimal");int metal=d.inventory.MetalTotal;d.forge.BeginForge();recording=true;StartCoroutine(Record());yield return new WaitForSeconds(.1f);yield return Capture("05-qte-minimal");
   d.forge.TogglePause();float remaining=d.forge.TimeRemaining;yield return new WaitForSeconds(.3f);checks["forgePauseKeepsTiming"]=d.forge.TimeRemaining==remaining&&!d.forge.SubmitStrike();yield return Capture("06-workshop-pause");d.forge.TogglePause();
   for(int i=0;i<3;i++){yield return Wait(()=>d.forge.StrikeReady&&Mathf.Abs(d.forge.Pointer-d.forge.TargetCenter)<=d.forge.TargetWidth*.38f,7);d.forge.SubmitStrike();if(i==0){yield return new WaitForSeconds(.32f);yield return Capture("07-strike-feedback");}yield return Wait(()=>d.forge.State!=ForgeState.Striking,3);}
   checks["threeQteStrikesStillYieldCorrectMetal"]=d.forge.State==ForgeState.Success&&d.inventory.MetalTotal==metal+1&&d.inventory.MetalCount(RepairResource.CopperCompound)==1;yield return new WaitForSeconds(.7f);yield return Capture("08-ingot-in-workshop");yield return new WaitForSeconds(.6f);
   d.forge.BeginForge();yield return Wait(()=>d.forge.StrikeReady&&Mathf.Abs(d.forge.Pointer-d.forge.TargetCenter)>d.forge.TargetWidth,4);d.forge.SubmitStrike();yield return new WaitForSeconds(.55f);checks["failureRetainsPortraitAndBlur"]=d.forge.State==ForgeState.Failure&&d.forge.FailureBlur>.2f;yield return Capture("09-failure-in-workshop");yield return new WaitForSeconds(1.35f);d.forge.TryClose();recording=false;
   d.player.position=new Vector3(d.mine.transform.position.x,d.surfaceY,0);d.Interact();yield return Wait(()=>!d.Transitioning,4);checks["verticalTravelRetained"]=d.underground;yield return Capture("10-underground-minimal");
   var ore=d.ores.Find(x=>x.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.RepairMaterial);ore.transform.position=d.winchAnchor.position+Vector3.down*4.5f;typeof(SceneDirector).GetField("angle",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(d,0f);int count=d.inventory.UnknownCount;sound=InteractionFeel.Played;
   checks["hookCastHasFeedback"]=d.Cast()&&InteractionFeel.Played>sound;checks["repeatedHookCastStillBlocked"]=!d.Cast();yield return Wait(()=>d.inventory.UnknownCount>count,10);checks["hookPickupAndPopupStillWork"]=d.inventory.UnknownCount==count+1&&d.PickupPopupVisible;yield return Capture("11-hook-recovery");yield return Wait(()=>d.hookState==SceneDirector.HookState.Swinging,3);d.Interact();yield return Wait(()=>!d.Transitioning,4);
   d.battle.Enter(null);yield return Wait(()=>d.battle.Explorer&&!d.battle.IsTransitioning,18);var fps=d.battle.Explorer;yield return new WaitForSeconds(.4f);foreach(var c in fps.arena.creatures){c.enabled=false;c.Agent.isStopped=true;}FindFirstObjectByType<TitanAreaAttack>().AttackEnabled=false;
   yield return new WaitForSeconds(4.2f);yield return Capture("12-first-person-quiet");fps.SetPaused(true);yield return Capture("13-first-person-pause");checks["pausedCombatRejectsInput"]=!fps.arsenal.Attack()&&!fps.arsenal.Reload();fps.SetPaused(false);
   var node=fps.expedition.resources[0];fps.Motor.enabled=false;var pos=node.transform.position+Vector3.back*2.2f;fps.transform.position=new Vector3(pos.x,AlienSurfaceLandmarks.Height(pos.x,pos.z)+.08f,pos.z);fps.Motor.enabled=true;fps.ResetMotion();Face(fps,node.transform.position+Vector3.up*.7f);Physics.SyncTransforms();fps.expedition.Interact(false,.016f);checks["lookFocusIdentifiesRealMineral"]=fps.expedition.Focused==node.transform;
   fps.expedition.Interact(true,.8f);checks["reticleMiningProgressStarts"]=fps.expedition.MiningProgress>0;yield return Capture("14-reticle-mining");fps.expedition.Interact(false,.016f);checks["releaseCancelsMining"]=fps.expedition.MiningProgress==0;
   var origin=node.transform.position;fps.transform.rotation=Quaternion.Euler(0,180,0);fps.expedition.Interact(false,.016f);checks["lookAwayClearsFocus"]=fps.expedition.Focused==null;
   var camera=fps.viewCamera;InteractionFeel.ReducedMotion=true;fps.ImpactPulse(.8f);float before=camera.fieldOfView;yield return new WaitForSeconds(.4f);checks["reducedMotionDoesNotAmplifyFov"]=camera.fieldOfView<=before+.01f;InteractionFeel.ReducedMotion=false;
   count=d.inventory.UnknownCount;d.battle.ReturnToExpedition();yield return Wait(()=>!d.battle.IsEngaged,12);checks["returnPreservesInventoryAndWormhole"]=d.inventory.UnknownCount==count&&d.wormhole.Unlocked;checks["oneFeedbackServiceAcrossScenes"]=FindObjectsByType<InteractionFeel>(FindObjectsSortMode.None).Length==1;yield return Capture("15-return-to-world");
   File.WriteAllLines(folder+"/frame-times.txt",times);Finish();
  }
  void Face(FirstPersonExplorer fps,Vector3 point){var v=point-fps.viewCamera.transform.position;fps.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg,0);fps.Look(0,fps.Pitch+Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg);}
  IEnumerator Wait(Func<bool> c,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!c()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture){File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);}}
  IEnumerator Record(){while(recording&&frames<120){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frames++).ToString("D4")+".png",t.EncodeToPNG());Destroy(t);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frames+"\n}";File.WriteAllText(folder+"/immersion-report.json",json);Debug.Log(pass?"STARFALL_IMMERSION_QA_PASS":"STARFALL_IMMERSION_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
