using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Starfall {
 public sealed class GuidanceVerification:MonoBehaviour {
  CampaignRuntime c;SceneDirector d;int failures;string folder;readonly List<string> results=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-guidanceQA")&&CampaignRuntime.Current)CampaignRuntime.Current.gameObject.AddComponent<GuidanceVerification>();}
  void Check(string name,bool ok){var line=(ok?"PASS ":"FAIL ")+name;results.Add(line);if(!ok)failures++;Debug.Log("GUIDANCE_QA "+line);}
  IEnumerator Start(){
   yield return new WaitForSeconds(2);c=CampaignRuntime.Current;d=c?c.director:null;folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/Reports/Guidance"));Directory.CreateDirectory(folder);
   Check("Campaign loaded in independent verification save",c&&c.IsQA&&c.Store.directory.Contains("GuidanceVerification"));if(!c){Finish();yield break;}
   var run=Run();while(true){object next=null;bool more=false;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check("Unhandled "+e,false);break;}if(!more)break;yield return next;}Finish();
  }
  void Finish(){File.WriteAllLines(Path.Combine(folder,"guidance-report.txt"),results);Debug.Log(failures==0?"GUIDANCE_QA_PASS":"GUIDANCE_QA_FAIL "+failures);Application.Quit(failures==0?0:2);}
  IEnumerator Wait(Func<bool> test){float until=Time.realtimeSinceStartup+25;while(!test()&&Time.realtimeSinceStartup<until)yield return null;Check("Scene transition completed",test());}
  void Warp(Vector3 p){var fps=c.Explorer;fps.Motor.enabled=false;fps.transform.position=p+Vector3.up*.08f;fps.Motor.enabled=true;}
  IEnumerator Run(){
   GuidanceRuleChecks.Run(Check);Check("Initial task navigation exists",c.GetGuidanceNavigation().available);Check("Initial navigation uses actual mine object",Vector3.Distance(c.GetGuidanceNavigation().destination,d.mine.transform.position)<.1f);
   for(int category=0;category<6;category++)foreach(var key in CampaignGuidance.TaskKeys(c.State,c.GetGuidanceContext(),category))Check("Actual catalog task resolves "+key,CampaignGuidance.Describe(c.State,c.GetGuidanceContext(),key)!=null);
   c.State.coordinates.Add(1);c.EnterRegion(1);yield return Wait(()=>c.Explorer&&c.Explorer.Ready&&!d.battle.IsTransitioning);c.InvalidateGuidance();
   var nav=c.GetGuidanceNavigation();Check("Wreck cargo destination is runtime site",nav.available&&Vector3.Distance(nav.destination,c.RegionView.FrontierSitePosition("chest"))<.1f);
   Check("Navigation available before drone scan unlock",!c.ScanUnlocked&&nav.available);
   c.State.device=true;c.State.calibrated=true;c.State.Flag("repair-plan");c.State.coordinates.Add(2);d.battle.ReturnToExpedition();yield return Wait(()=>!d.battle.IsEngaged&&!d.battle.IsTransitioning);c.EnterRegion(2);yield return Wait(()=>c.Explorer&&c.Explorer.Ready&&!d.battle.IsTransitioning);
   c.TrackObjective("quest:2");c.InvalidateGuidance();var winch=c.RegionView.FrontierSitePosition("winch");nav=c.GetGuidanceNavigation();Check("Rescue uses physical winch destination",nav.available&&Vector3.Distance(nav.destination,winch)<.1f);
   Warp(winch+Vector3.left*2);yield return null;Check("Actual rescue interaction succeeds",c.RegionView.Interact()&&c.Exp("winch"));c.InvalidateGuidance();Check("Real winch interaction advances rescue step",c.CurrentGuidance.targetKey=="shelter");
   c.TrackObjective("surveys");nav=c.GetGuidanceNavigation();Check("Survey navigation follows relocated runtime anchor",nav.available&&Vector3.Distance(nav.destination,c.RegionView.FrontierSitePosition("survey"))<.1f);
   c.State.coordinates.Add(3);c.State.expansionProgress.Add("doctor-rescued");c.TrackObjective("quest:5");nav=c.GetGuidanceNavigation();Check("Cross region task first navigates to current beacon",nav.available&&Vector3.Distance(nav.destination,c.Explorer.returnBeacon.position)<.1f);
   c.Explorer.SetPaused(true);float relay=c.RegionView.RelayRemaining;yield return new WaitForSecondsRealtime(.15f);Check("Guidance does not advance paused challenge",c.RegionView.RelayRemaining==relay);c.Explorer.SetPaused(false);
   var store=new CampaignStore(Path.Combine(c.Store.directory,"guidance-compatibility"));var legacy=CampaignState.New();legacy.guidanceSeen=null;legacy.guidanceRoute="unknown";store.Save(legacy);var restored=store.Load();Check("Missing guide fields and unknown route normalize",restored!=null&&restored.guidanceSeen!=null&&restored.guidanceRoute=="escape");
   c.State.guidanceRoute="investigate";c.State.trackedObjective="quest:0";c.Save("guidance-roundtrip");restored=c.Store.Load("guidance-roundtrip");Check("Route and legacy tracked key survive save",restored!=null&&restored.guidanceRoute=="investigate"&&restored.trackedObjective=="quest:0");
   c.TrackObjective("quest:2");yield return Capture("battlefield-guidance-900p",1600,900);
   c.Open("journal");yield return Capture("journal-guidance-720p",1280,720);c.Close();yield return Capture("battlefield-guidance-1080p",1920,1080);
   d.battle.ReturnToExpedition();yield return Wait(()=>!d.battle.IsEngaged&&!d.battle.IsTransitioning);c.State.coordinates.Add(4);c.EnterRegion(4);yield return Wait(()=>c.Explorer&&c.Explorer.Ready&&!d.battle.IsTransitioning);var upper=c.RegionView.FrontierSitePosition("relic-4");Check("Physical shipyard ramp fallback locates entrance",c.RegionView.TryGuidanceRamp(upper,out var ramp));c.TrackObjective("relics");c.State.expansionProgress.Add("relic-1");c.State.expansionProgress.Add("relic-2");c.State.expansionProgress.Add("relic-3");c.InvalidateGuidance();nav=c.GetGuidanceNavigation();Check("Upper relic navigation uses actual deck destination",nav.available&&Vector3.Distance(nav.destination,upper)<.1f);yield return Capture("shipyard-upper-guidance-900p",1600,900);
   Warp(new Vector3(17,FieldSurface.Height(17,-5,AlienSurfaceLandmarks.Height(17,-5)),-5));yield return null;Check("Ramp fallback advances ahead after entering slope",c.RegionView.TryGuidanceRamp(upper,out var forwardRamp)&&forwardRamp.z>c.Explorer.transform.position.z);
  }
  IEnumerator Capture(string name,int w,int h){if(Screen.width==w&&Screen.height==h){Screen.SetResolution(w-16,h-9,false);yield return new WaitForSecondsRealtime(.2f);}Screen.SetResolution(w,h,false);bool captured=false;for(int retry=0;retry<8&&!captured;retry++){yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();if(!shot)continue;var pixels=shot.GetPixels32();int bright=0;for(int i=0;i<pixels.Length;i+=Mathf.Max(1,pixels.Length/256))if(pixels[i].r+pixels[i].g+pixels[i].b>30)bright++;if(bright>12){File.WriteAllBytes(Path.Combine(folder,name+".png"),shot.EncodeToPNG());captured=true;}Destroy(shot);}Check("Nonblack full UI capture "+name,captured);}
 }
}
