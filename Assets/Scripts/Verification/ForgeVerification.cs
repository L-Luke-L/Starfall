using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Starfall {
 public sealed class ForgeVerification : MonoBehaviour {
  string folder;bool recording;int frames;readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();ForgeController forge;
  readonly List<string> captureTimes=new List<string>();
  readonly List<Task> frameWrites=new List<Task>();
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-forgeQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/forge-v8"));Directory.CreateDirectory(folder+"/frames");
   var d=GetComponent<SceneDirector>();forge=d.forge;var inv=d.inventory;
   yield return new WaitForSeconds(1);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
   checks["campRequiresProximity"]=!forge.OpenCamp();d.player.position=new Vector3(d.camp.transform.position.x,d.surfaceY,0);d.Interact();
   checks["campMenuOpensByInteraction"]=forge.State==ForgeState.CampMenu;
   forge.OpenWorkshop();checks["zeroInventoryCannotStart"]=!forge.BeginForge()&&inv.UnknownCount==0;forge.TryClose();
   d.player.position=new Vector3(-3.25f,d.surfaceY,0);
   var def=d.ores[0].GetComponent<BuriedSample>().Definition;
   d.AwardRecoveredOre(def,91001);checks["duplicateRecoveryBlocked"]=!d.AwardRecoveredOre(def,91001)&&inv.UnknownCount==1;
   yield return new WaitForSeconds(.12f);checks["popupBeforeHudIncrement"]=d.PickupPopupVisible&&d.DisplayedOreCount==0;yield return Capture("01-pickup-popup");
   yield return new WaitForSeconds(.65f);checks["unknownHudIncremented"]=d.DisplayedOreCount==1;yield return Capture("02-inventory-count");
   var materials=d.ores.FindAll(o=>o.GetComponent<BuriedSample>().Definition.payloadKind!=BuriedPayloadKind.DormantEncounter);for(int i=1;i<5;i++)d.AwardRecoveredOre(materials[i].GetComponent<BuriedSample>().Definition,91001+i);
   yield return new WaitForSeconds(.7f);d.player.position=new Vector3(d.camp.transform.position.x,d.surfaceY,0);d.Interact();yield return Capture("03-camp-menu");forge.OpenWorkshop();yield return Capture("04-workshop-ready");
   recording=true;StartCoroutine(RecordFrames());yield return new WaitForSeconds(.7f);
   checks["moderatedQteSerialized"]=forge.targetWidths[2]>=.085f&&forge.pointerSpeeds[2]<=1.48f&&forge.roundDurations[2]>=2.2f;VerifyRhythms();checks["reserveExactlyOne"]=forge.BeginForge()&&inv.UnknownCount==4;
   checks["doubleStartBlocked"]=!forge.BeginForge()&&inv.UnknownCount==4;
   checks["activeCloseBlocked"]=!forge.TryClose();checks["worldHookBlockedByModal"]=!d.Cast();
   checks["warmupIgnoresPrematureInput"]=!forge.SubmitStrike()&&forge.State==ForgeState.Timing&&forge.Hits==0;var firstOpening=forge.Rhythm;var usedRhythms=new HashSet<ForgeRhythmKind>();yield return Capture("05-qte");
   for(int i=0;i<3;i++){
    usedRhythms.Add(forge.Rhythm);float widthBeforeStrike=forge.TargetWidth;
    yield return HitWindow();
    checks["round"+(i+1)+"MeterStableDuringStrike"]=forge.State==ForgeState.Striking&&forge.RoundIndex==i&&Mathf.Abs(forge.TargetWidth-widthBeforeStrike)<.0001f&&forge.RoundTimeFraction>=0&&forge.RoundTimeFraction<=1;
    if(i==0){yield return new WaitForSeconds(.32f);yield return Capture("06-impact");}
    yield return WaitForState(i==2?ForgeState.Success:ForgeState.Timing,3);
   }
   checks["threeDifferentRhythmsPerOre"]=usedRhythms.Count==3;checks["threeHitsOneMetal"]=forge.State==ForgeState.Success&&forge.Hits==3&&forge.TotalStrikes==3&&inv.MetalTotal==1&&inv.UnknownCount==4&&!inv.HasReservation;
   checks["rewardPresentationProtected"]=forge.RewardPresentationActive&&!forge.BeginForge()&&!forge.TryClose();yield return new WaitForSeconds(.72f);checks["namedIngotReward"]=forge.RewardArtAvailable&&OreInventory.MetalName(forge.ResultMetal)=="陨铁锭";yield return Capture("07-success");yield return new WaitForSeconds(.65f);
   forge.BeginForge();checks["nextOreChangesOpeningRhythm"]=forge.Rhythm!=firstOpening;yield return MissWindow();
   checks["firstRoundMissLosesOnlyOne"]=forge.State==ForgeState.Failure&&inv.UnknownCount==3&&inv.MetalTotal==1&&!inv.HasReservation;
   checks["failureAnimationBlocksRestart"]=!forge.BeginForge()&&!forge.TryClose();yield return new WaitForSeconds(.12f);float earlyBlur=forge.FailureBlur;yield return Capture("08a-failure-enter");yield return new WaitForSeconds(.46f);checks["progressiveWorkshopBlur"]=forge.FailureBlur>earlyBlur;checks["portraitCrossesCenter"]=forge.FailurePortraitX>300&&forge.FailurePortraitX<500;yield return Capture("08-failure-cg");yield return new WaitForSeconds(1.25f);checks["portraitLeavesScreen"]=forge.FailurePortraitX>1600&&!forge.FailurePresentationActive;yield return Capture("08b-failure-exit");recording=false;
   forge.BeginForge();while(!forge.StrikeReady)yield return null;yield return new WaitForSeconds(.2f);forge.TogglePause();float remaining=forge.TimeRemaining,pointerBeforePause=forge.Pointer,targetBeforePause=forge.TargetCenter;yield return new WaitForSeconds(.5f);
   checks["pauseFreezesQte"]=forge.Paused&&Mathf.Abs(forge.TimeRemaining-remaining)<.001f&&forge.Pointer==pointerBeforePause&&forge.TargetCenter==targetBeforePause;forge.TogglePause();
   yield return WaitForState(ForgeState.Failure,7);checks["timeoutLosesOnlyOne"]=forge.State==ForgeState.Failure&&inv.UnknownCount==2&&inv.MetalTotal==1;
   yield return new WaitForSeconds(1.85f);forge.BeginForge();yield return HitWindow();yield return WaitForState(ForgeState.Timing,3);yield return MissWindow();checks["secondRoundMissNoMetal"]=forge.State==ForgeState.Failure&&inv.UnknownCount==1&&inv.MetalTotal==1;
   yield return new WaitForSeconds(1.85f);forge.BeginForge();for(int i=0;i<2;i++){yield return HitWindow();yield return WaitForState(ForgeState.Timing,3);}yield return MissWindow();checks["thirdRoundMissNoMetal"]=forge.State==ForgeState.Failure&&inv.UnknownCount==0&&inv.MetalTotal==1&&!inv.HasReservation;
   yield return new WaitForSeconds(1.85f);checks["cannotForgeWhenExhausted"]=!forge.BeginForge();forge.TryClose();yield return null;checks["returnKeepsResourceCounts"]=!forge.IsOpen&&d.DisplayedOreCount==0&&inv.MetalTotal==1;
   forge.OpenCamp();forge.OpenWorkshop();
   for(int resource=1;resource<4;resource++){
    var source=ScriptableObject.CreateInstance<SubsurfaceDefinition>();source.payloadKind=BuriedPayloadKind.RepairMaterial;source.repairResource=(RepairResource)resource;
    d.AwardRecoveredOre(source,92000+resource);forge.BeginForge();
    for(int hit=0;hit<3;hit++){yield return HitWindow();yield return WaitForState(hit==2?ForgeState.Success:ForgeState.Timing,3);}
    checks["namedResource"+resource+"Awarded"]=forge.State==ForgeState.Success&&forge.ResultMetal==(RepairResource)resource&&inv.MetalCount((RepairResource)resource)==1&&inv.MetalTotal==resource+1;
    yield return new WaitForSeconds(.72f);yield return Capture("reward-"+resource);yield return new WaitForSeconds(.55f);Destroy(source);
   }
   forge.TryClose();
   while(frameWrites.Exists(t=>!t.IsCompleted))yield return null;
   checks["captureWritesCompleted"]=!frameWrites.Exists(t=>t.IsFaulted);
   bool pass=true;var json="{\n";foreach(var kv in checks){pass&=kv.Value;json+="  \""+kv.Key+"\":"+(kv.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frames+"\n}";File.WriteAllText(folder+"/forge-report.json",json);Debug.Log(pass?"STARFALL_FORGE_QA_PASS":"STARFALL_FORGE_QA_FAIL");Application.Quit(pass?0:2);
  }
  void VerifyRhythms(){
   bool bounded=true,opportunities=true,smooth=true,slowerWindows=true;
   foreach(ForgeRhythmKind kind in Enum.GetValues(typeof(ForgeRhythmKind)))for(int round=0;round<3;round++)for(int direction=0;direction<2;direction++)for(int home=0;home<3;home++)for(int phase=0;phase<4;phase++){
    float prior=direction==1?1:0;int entries=0;bool insideBefore=false;
    for(float t=0;t<forge.roundDurations[round];t+=1/60f){
     ForgeRhythm.Evaluate(kind,t,forge.pointerSpeeds[round],direction==1,.32f+home*.18f,phase*Mathf.PI*.5f,out float p,out float target);
     bounded&=p>=0&&p<=1&&target-forge.targetWidths[round]/2>0&&target+forge.targetWidths[round]/2<1;
     smooth&=Mathf.Abs(p-prior)<=forge.pointerSpeeds[round]*1.17f/60+.0001f;prior=p;
     bool inside=Mathf.Abs(p-target)<=forge.targetWidths[round]/2;if(inside&&!insideBefore)entries++;insideBefore=inside;
    }
    opportunities&=entries>=2;
    float maximumRelativeSpeed=forge.pointerSpeeds[round]*(kind==ForgeRhythmKind.Surge?1.17f:1)+(kind==ForgeRhythmKind.Drift?.105f:0);
    float[] previousWindows={.10f/1.2f,.08f/1.45f,.065f/1.65f};slowerWindows&=forge.targetWidths[round]/maximumRelativeSpeed>previousWindows[round];
   }
   checks["allRhythmsStayInsideTrack"]=bounded;checks["allRhythmsHaveMultiple60FpsOpportunities"]=opportunities;checks["speedChangesRemainSmooth"]=smooth;checks["everyVariantHasWiderTimingThanV7"]=slowerWindows;
  }
  IEnumerator MissWindow(){float deadline=Time.realtimeSinceStartup+5;while(forge.State==ForgeState.Timing&&(!forge.StrikeReady||Mathf.Abs(forge.Pointer-forge.TargetCenter)<=forge.TargetWidth*.6f)&&Time.realtimeSinceStartup<deadline)yield return null;forge.SubmitStrike();}
  IEnumerator HitWindow(){float deadline=Time.realtimeSinceStartup+7;while(forge.State==ForgeState.Timing&&(!forge.StrikeReady||Mathf.Abs(forge.Pointer-forge.TargetCenter)>forge.TargetWidth*.499f)&&Time.realtimeSinceStartup<deadline)yield return null;forge.SubmitStrike();}
  IEnumerator WaitForState(ForgeState state,float seconds){float deadline=Time.realtimeSinceStartup+seconds;while(forge.State!=state&&Time.realtimeSinceStartup<deadline)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture){File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);}}
  IEnumerator RecordFrames(){while(recording&&frames<400){yield return new WaitForSecondsRealtime(.05f);yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture){captureTimes.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));var pixels=texture.GetPixels32();int w=texture.width,h=texture.height;string path=folder+"/frames/"+frames.ToString("D4")+".bmp";Destroy(texture);frameWrites.Add(Task.Run(()=>WriteBitmap(path,pixels,w,h)));frames++;}}File.WriteAllLines(folder+"/frame-times.txt",captureTimes);}
  // Keep compression and file IO off the gameplay thread so capture cannot consume a QTE window.
  static void WriteBitmap(string path,Color32[] pixels,int width,int height){
   using(var stream=new MemoryStream(54+pixels.Length*4))using(var writer=new BinaryWriter(stream)){
    writer.Write((ushort)0x4d42);writer.Write(54+pixels.Length*4);writer.Write(0);writer.Write(54);writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);writer.Write(pixels.Length*4);writer.Write(2835);writer.Write(2835);writer.Write(0);writer.Write(0);
    foreach(var p in pixels){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);writer.Write(p.a);}File.WriteAllBytes(path,stream.ToArray());
   }
  }
 }
}



