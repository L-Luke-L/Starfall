using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 public sealed class LaunchMenuVerification:MonoBehaviour {
  readonly List<string> results=new List<string>();int failures;bool finished;float deadline;
  CampaignRuntime campaign;string folder;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-launchQA")>=0&&CampaignRuntime.Current)CampaignRuntime.Current.gameObject.AddComponent<LaunchMenuVerification>();}
  void Check(string label,bool ok){results.Add((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;Debug.Log(results[results.Count-1]);}
  void Log(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Assert){Check("Runtime exception: "+message,false);}}
  void Update(){if(!finished&&deadline>0&&Time.realtimeSinceStartup>deadline){Check("Launch QA timed out",false);Finish();}}
  void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;File.WriteAllLines(Path.Combine(folder,"launch-report.txt"),results);Debug.Log(failures==0?"LAUNCH_QA_PASS":"LAUNCH_QA_FAIL");Application.Quit(failures==0?0:2);}
  IEnumerator Capture(string name,int width=1600,int height=900){
   Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
  }
  IEnumerator Start(){
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/Reports/Launch"));Directory.CreateDirectory(folder);
   deadline=Time.realtimeSinceStartup+180;Application.logMessageReceived+=Log;
   yield return new WaitForSecondsRealtime(2);campaign=CampaignRuntime.Current;
   Check("Fresh startup opens title with no story",campaign&&campaign.LaunchMenuActive&&!campaign.LaunchHasStory);
   Check("Title freezes world and blocks saving",Time.timeScale==0&&!campaign.Save()&&!File.Exists(campaign.Store.PathFor("campaign")));
   Screen.SetResolution(960,720,false);yield return new WaitForSecondsRealtime(.3f);
   yield return Capture("first-launch");
   typeof(CampaignRuntime).GetField("launchPage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(campaign,"settings");
   yield return Capture("settings-4x3",960,720);
   campaign.ShowLaunchMenu();yield return Capture("first-launch-ultrawide",1920,800);
   var original=campaign.State;var store=campaign.Store;
   Check("Demo can start from untouched title",campaign.LaunchDemo());
   float until=Time.realtimeSinceStartup+75;bool warehouse=false,journal=false;
   while(campaign.ShowcaseActive&&!campaign.ShowcaseFinished&&Time.realtimeSinceStartup<until){
    if(campaign.CurrentPanel=="warehouse"&&!warehouse){warehouse=true;yield return Capture("demo-warehouse");}
    if(campaign.CurrentPanel=="journal"){journal=true;yield return Capture("demo-journal");break;}
    yield return null;
   }
   Check("Demo displays actual warehouse and journal",warehouse&&journal);
   campaign.SetShowcasePaused(true);float elapsed=campaign.ShowcaseElapsed;
   yield return new WaitForSecondsRealtime(.5f);Check("Demo pause stops clock",Mathf.Abs(campaign.ShowcaseElapsed-elapsed)<.03f);
   campaign.StopShowcase();while(campaign.ShowcaseActive)yield return null;
   Check("Demo returns to title and original state",campaign.LaunchMenuActive&&campaign.State==original&&campaign.Store==store);
   Check("Demo did not create a normal save",!File.Exists(store.PathFor("campaign"))&&!campaign.LaunchHasStory);
   campaign.LaunchNewStory();while(campaign.LaunchMenuActive)yield return null;
   Check("Start creates playable story and save",File.Exists(store.PathFor("campaign"))&&Time.timeScale==1);
   campaign.State.bag.Add(ItemId.Medkit,3);campaign.Save();
   var bytes=File.ReadAllText(store.PathFor("campaign"));
   campaign.SendMessage("InitializeLaunchMenu");campaign.ShowLaunchMenu();
   Check("Returning startup exposes continue",campaign.LaunchHasStory);
   yield return Capture("returning-launch",1280,720);
   Check("Returning menu does not mutate save",!campaign.Save()&&File.ReadAllText(store.PathFor("campaign"))==bytes);
   campaign.LaunchContinue();yield return null;
   Check("Continue preserves inventory and unpauses",!campaign.LaunchMenuActive&&campaign.State.bag.Count(ItemId.Medkit)==3&&Time.timeScale==1);
   campaign.SendMessage("InitializeLaunchMenu");campaign.ShowLaunchMenu();campaign.LaunchNewStory();while(campaign.LaunchMenuActive)yield return null;
   Check("Restart resets inventory and keeps previous story",campaign.State.bag.Count(ItemId.Medkit)==0&&store.Load("previous-story").bag.Count(ItemId.Medkit)==3);
   Finish();
  }
 }
}
