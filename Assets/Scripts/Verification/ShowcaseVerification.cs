using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 // A real-duration run against the shipping showcase. It never loads the normal CampaignV1 save.
 public sealed class ShowcaseVerification:MonoBehaviour {
  CampaignRuntime campaign;SceneDirector director;string folder;int failures;float watchdog;bool finished;
  readonly List<string> results=new List<string>();readonly HashSet<string> runtimeErrors=new HashSet<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseQA")&&CampaignRuntime.Current)CampaignRuntime.Current.gameObject.AddComponent<ShowcaseVerification>();}
  void Check(string name,bool ok){string line=(ok?"PASS ":"FAIL ")+name;results.Add(line);if(!ok)failures++;Debug.Log("SHOWCASE_QA "+line);}
  void Log(string condition,string stack,LogType type){if(type==LogType.Exception||type==LogType.Assert)runtimeErrors.Add(condition);}
  IEnumerator Start(){
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseLifecycleOnly")?"../../../QA/Reports/ShowcaseLifecycle":"../../../QA/Reports/Showcase"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;watchdog=Time.realtimeSinceStartup+720;
   yield return new WaitForSecondsRealtime(2);campaign=CampaignRuntime.Current;director=campaign?campaign.director:null;
   Check("Verification uses independent QA campaign",campaign&&campaign.IsQA&&!campaign.Store.directory.EndsWith("CampaignV1",StringComparison.OrdinalIgnoreCase));
   if(!campaign){Finish();yield break;}
   var run=Run();while(!finished){object next=null;bool more=false;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check("Unhandled verification exception: "+e,false);break;}if(!more)break;yield return next;}
   Finish();
  }
  void Update(){if(!finished&&watchdog>0&&Time.realtimeSinceStartup>watchdog){Check("Verification completed before watchdog",false);Finish();}}
  void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;Check("No unhandled runtime exceptions",runtimeErrors.Count==0);foreach(var error in runtimeErrors)results.Add("RUNTIME "+error);File.WriteAllLines(Path.Combine(folder,"showcase-report.txt"),results);Debug.Log(failures==0?"SHOWCASE_QA_PASS":"SHOWCASE_QA_FAIL "+failures);Application.Quit(failures==0?0:2);}
  static Dictionary<string,string> FilesAt(string directory){var files=new Dictionary<string,string>();foreach(var path in Directory.GetFiles(directory))files[path]=Convert.ToBase64String(File.ReadAllBytes(path));return files;}
  static bool SameFiles(string directory,Dictionary<string,string> expected){var current=FilesAt(directory);if(current.Count!=expected.Count)return false;foreach(var pair in expected)if(!current.TryGetValue(pair.Key,out var data)||data!=pair.Value)return false;return true;}
  IEnumerator Run(){
   campaign.Close();director.SetPaused(false);var original=campaign.State;var originalStore=campaign.Store;Vector3 originalPosition=director.player.position;
   Check("Showcase starts from normal surface",campaign.StartShowcase());yield return null;
   // Entry legitimately flushes pending normal progress once, before switching stores.
   string originalJson=JsonUtility.ToJson(original);var originalFiles=FilesAt(originalStore.directory);
   Check("Demo owns a fresh state and a separate save directory",campaign.ShowcaseActive&&!ReferenceEquals(campaign.State,original)&&!ReferenceEquals(campaign.Store,originalStore)&&campaign.Store.directory!=originalStore.directory);
   Check("Duplicate showcase launch is rejected",!campaign.StartShowcase());
   var cine=CampaignCinematics.Ensure(campaign);float cineWait=Time.realtimeSinceStartup+10;while(!cine.IsPlaying&&Time.realtimeSinceStartup<cineWait)yield return null;
   Check("Opening CG uses a live cinematic camera",cine.IsPlaying&&cine.ShotCamera&&cine.ShotCamera.enabled&&!director.worldCamera.enabled);
   cine.SetPaused(true);float elapsed=campaign.ShowcaseElapsed,cineElapsed=cine.Elapsed;yield return new WaitForSecondsRealtime(.6f);
   Check("Pause freezes showcase clock",Mathf.Abs(campaign.ShowcaseElapsed-elapsed)<.01f);
   Check("Pause also freezes the real cinematic",cine.IsPlaying&&cine.IsPaused&&Mathf.Abs(cine.Elapsed-cineElapsed)<.01f);
   cine.SetPaused(false);yield return new WaitForSecondsRealtime(.35f);Check("Resume advances showcase clock",!campaign.ShowcasePaused&&campaign.ShowcaseElapsed>elapsed);
   yield return Capture("opening-cg");
   Check("Demo save succeeds in separate store",campaign.Save());Check("Demo writes cannot alter original campaign files",SameFiles(originalStore.directory,originalFiles));
   campaign.StopShowcase();float until=Time.realtimeSinceStartup+30;while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<until)yield return null;
   Check("Early exit restores original state and store",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,originalStore));
   Check("Cancelling CG restores gameplay camera and enabled components",!cine.IsPlaying&&!cine.ShotCamera&&director.worldCamera.enabled&&campaign.enabled&&director.enabled);
   Check("Early exit restores original progression and surface position",JsonUtility.ToJson(original)==originalJson&&Vector3.Distance(director.player.position,originalPosition)<.05f);
   Check("Early exit keeps original save files unchanged",SameFiles(originalStore.directory,originalFiles));
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseLifecycleOnly")){yield return LifecycleEdges();yield break;}
   Check("A fresh complete showcase can restart after cancellation",campaign.StartShowcase());yield return null;originalJson=JsonUtility.ToJson(original);originalFiles=FilesAt(originalStore.directory);
   int lastStage=-1;var visited=new HashSet<int>();var milestones=new HashSet<string>();var deaths=new Dictionary<int,int>();bool testedForgePause=false;float runStart=Time.realtimeSinceStartup;
   while(campaign.ShowcaseActive&&!campaign.ShowcaseFinished&&Time.realtimeSinceStartup-runStart<650){
    int stage=campaign.ShowcaseStage;if(stage!=lastStage){lastStage=stage;visited.Add(stage);Debug.Log("SHOWCASE_QA_STAGE "+stage+" elapsed="+campaign.ShowcaseElapsed.ToString("F2"));yield return Capture("stage-"+stage.ToString("D2"));}
    string milestone=Milestone();if(milestone!=null&&milestones.Add(milestone))yield return Capture(milestone);
    if(campaign.Explorer)deaths[campaign.Explorer.GetInstanceID()]=campaign.Explorer.arena.RespawnCount;
    if(stage==2&&!testedForgePause&&director.forge.IsWorking&&!campaign.CinematicActive){testedForgePause=true;campaign.SetShowcasePaused(true);float pausedAt=campaign.ShowcaseElapsed,pointer=director.forge.Pointer;yield return new WaitForSecondsRealtime(.4f);Check("Gameplay pause freezes clock and unscaled forge QTE",campaign.ShowcasePaused&&director.forge.Paused&&Mathf.Abs(campaign.ShowcaseElapsed-pausedAt)<.01f&&Mathf.Abs(director.forge.Pointer-pointer)<.001f);campaign.SetShowcasePaused(false);}
    if(!string.IsNullOrEmpty(campaign.ShowcaseError))break;yield return null;
   }
   Check("Showcase reaches its result page",campaign.ShowcaseActive&&campaign.ShowcaseFinished&&string.IsNullOrEmpty(campaign.ShowcaseError));
   if(!string.IsNullOrEmpty(campaign.ShowcaseError))results.Add("SHOWCASE_ERROR "+campaign.ShowcaseError);
   Check("Full unskipped presentation lasts between five and ten minutes",campaign.ShowcaseElapsed>=300&&campaign.ShowcaseElapsed<=600);
   Check("All eleven mainline chapters were shown",visited.Count==11);
   int deathCount=0;foreach(int count in deaths.Values)deathCount+=count;Check("Directed presentation avoids death and recovery interruptions",deathCount==0);
   Check("Forge pause was exercised during the actual mainline",testedForgePause);
   var state=campaign.State;Check("All five mine layers and all five coordinates were earned",state.unlockedLayer==5&&state.coordinates.Count==5&&campaign.LayerComplete(1)&&campaign.LayerComplete(2)&&campaign.LayerComplete(3)&&campaign.LayerComplete(4)&&campaign.LayerComplete(5));
   bool knowledge=true;foreach(string id in new[]{"J01","J02","J03","J04","J05","J06","J07","J08"})knowledge&=state.Knows(id);Check("Mainline records from all five realms were acquired",knowledge);
   Check("Ship repair route remains complete",state.repairs.Count==4&&state.Has("repair-plan")&&state.Has("ship-navigation")&&state.Has("ship-fueled"));
   Check("Boss, bomb and all three safety checks precede evacuation",state.Has("boss-defeated")&&state.Has("bomb-crafted")&&state.Has("bomb-installed")&&state.Has("life-support-checked")&&state.Has("coupling-cut")&&state.Has("evac-anchor")&&state.Has("rift-destroyed")&&!state.Has("detonation-active"));
   Check("Mainline epilogue really completes the guardian ending",state.saviorComplete&&state.Has("ecology-chosen")&&state.Has("pollution-off")&&state.Has("water-restored")&&state.Has("seed-planted"));
   Check("No optional quests, modules or ruins were completed",state.expansionProgress.TrueForAll(x=>x=="frontier-initialized")&&state.craftedModules.Count==0&&state.equippedModules.Count==0&&state.ruins.TrueForAll(x=>!x.solved));
   Check("Original story remains unchanged throughout full demo",JsonUtility.ToJson(original)==originalJson&&SameFiles(originalStore.directory,originalFiles));yield return Capture("complete");
   campaign.StopShowcase();until=Time.realtimeSinceStartup+30;while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<until)yield return null;
   Check("Result exit restores original story and original save store",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,originalStore)&&JsonUtility.ToJson(original)==originalJson);
   yield return LifecycleEdges();
  }
  IEnumerator LifecycleEdges(){
   director.SetPaused(false);yield return director.Travel(true);var original=campaign.State;var store=campaign.Store;Vector3 position=director.player.position;
   Check("Underground story can start a showcase",director.underground&&campaign.StartShowcase());
   float end=Time.realtimeSinceStartup+20;while(!campaign.CinematicActive&&campaign.ShowcaseActive&&Time.realtimeSinceStartup<end)yield return null;
   var files=FilesAt(store.directory);campaign.StopShowcase();end=Time.realtimeSinceStartup+30;while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<end)yield return null;
   Check("Underground cancellation restores story, mine camera and player",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,store)&&director.underground&&Mathf.Abs(director.worldCamera.transform.position.y-director.undergroundCameraY)<.01f&&Vector3.Distance(position,director.player.position)<.05f);
   Check("Underground cancellation preserves original save files",SameFiles(store.directory,files));
   yield return Capture("restored-underground");
   director.SetPaused(false);yield return director.Travel(false);bool seeded=!campaign.State.coordinates.Contains(1);if(seeded)campaign.State.coordinates.Add(1);
   Check("Original first-person scene loads for restoration test",campaign.EnterRegion(1));end=Time.realtimeSinceStartup+30;while((!campaign.Explorer||director.battle.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;
   if(!campaign.Explorer){Check("First-person explorer exists",false);yield break;}
   var player=campaign.Explorer;CampaignShowcaseDriver.Warp(player,player.returnBeacon.position+Vector3.right*2);player.Look(Mathf.DeltaAngle(player.transform.eulerAngles.y,137),player.Pitch-17);player.SetPaused(true);
   var bomb=typeof(CampaignRuntime).GetField("<BombSeconds>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);bomb.SetValue(campaign,27f);
   original=campaign.State;store=campaign.Store;position=player.transform.position;float yaw=player.transform.eulerAngles.y,pitch=player.Pitch;
   Check("First-person story can start a showcase",campaign.StartShowcase());end=Time.realtimeSinceStartup+25;while(!campaign.CinematicActive&&campaign.ShowcaseActive&&Time.realtimeSinceStartup<end)yield return null;
   files=FilesAt(store.directory);string json=JsonUtility.ToJson(original);campaign.StopShowcase();end=Time.realtimeSinceStartup+40;while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<end)yield return null;
   player=campaign.Explorer;Check("First-person cancellation restores original state, store and region",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,store)&&campaign.State.region==1&&player);
   Check("First-person cancellation restores position, yaw, pitch and timer",player&&Vector3.Distance(player.transform.position,position)<.15f&&Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,yaw))<.1f&&Mathf.Abs(player.Pitch-pitch)<.1f&&Mathf.Abs(campaign.BombSeconds-27)<.01f&&player.Paused);
   Check("First-person cancellation preserves original progression and save files",JsonUtility.ToJson(original)==json&&SameFiles(store.directory,files));
   yield return Capture("restored-first-person");
   var cine=CampaignCinematics.Ensure(campaign);var playback=campaign.StartCoroutine(cine.Play(CampaignCinematicBeat.RiftArrival,3));yield return new WaitForSecondsRealtime(1);yield return Capture("fixture-rift-cg");yield return playback;
   bomb.SetValue(campaign,-1f);if(player)player.Return();end=Time.realtimeSinceStartup+25;while(director.battle.IsEngaged&&Time.realtimeSinceStartup<end)yield return null;
   if(seeded)campaign.State.coordinates.Remove(1);
   bool coreSeeded=!campaign.State.coordinates.Contains(5);if(coreSeeded)campaign.State.coordinates.Add(5);Check("Core scene loads for visual fixture",campaign.EnterRegion(5));end=Time.realtimeSinceStartup+30;while((!campaign.Explorer||director.battle.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;
   if(campaign.Explorer){campaign.Explorer.SetPaused(true);playback=campaign.StartCoroutine(cine.Play(CampaignCinematicBeat.Guardian,3));yield return new WaitForSecondsRealtime(1);yield return Capture("fixture-guardian-cg");yield return playback;Check("CG fixture leaves the real guardian undefeated",campaign.RegionView&&campaign.RegionView.boss&&!campaign.RegionView.boss.Dead&&!campaign.State.Has("boss-defeated"));campaign.Explorer.Return();end=Time.realtimeSinceStartup+25;while(director.battle.IsEngaged&&Time.realtimeSinceStartup<end)yield return null;}
   if(coreSeeded)campaign.State.coordinates.Remove(5);
  }
  string Milestone(){
   if(campaign.CinematicActive&&campaign.ShowcaseStage==1)return "rift-cg";
   if(campaign.Explorer&&!director.battle.IsTransitioning){
    if(campaign.ShowcaseStage==1&&!campaign.CinematicActive&&campaign.Explorer.arsenal.ShotsFired>3)return "pistol-combat";
    if(campaign.ShowcaseStage>=3&&campaign.ShowcaseStage<=6&&campaign.RegionView&&campaign.RegionView.Near("terminal"))return "realm-"+campaign.State.region+"-archive";
    var boss=campaign.RegionView?campaign.RegionView.boss:null;
    if(campaign.ShowcaseStage==8&&boss&&!campaign.CinematicActive){if(boss.Telegraphing)return "guardian-warning";if(boss.NodesRemaining<3&&!boss.Dead)return "guardian-combat";}
    if(campaign.BombSeconds>0&&!campaign.CinematicActive)return "evacuation-countdown";
   }
   if(campaign.ShowcaseStage==2&&director.forge.IsWorking&&director.forge.Hits>0)return "forge-qte";
   if(campaign.ShowcaseStage==10&&campaign.State.Has("seed-planted"))return "restored-planet";
   return null;
  }
  IEnumerator Capture(string name){
   yield return new WaitForEndOfFrame();var cameras=Camera.allCameras;Array.Sort(cameras,(a,b)=>a.depth.CompareTo(b.depth));bool hasWorld=false;foreach(var camera in cameras)if(camera&&camera.isActiveAndEnabled&&camera.targetTexture==null&&camera.cullingMask!=0)hasWorld=true;
   if(!hasWorld){Check("Active rendered camera exists for "+name,false);yield break;}
   var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;Texture2D image=null;var priorTargets=new Dictionary<Camera,RenderTexture>();
   try{RenderTexture.active=target;GL.Clear(true,true,Color.black);foreach(var camera in cameras){if(!camera||!camera.isActiveAndEnabled||camera.targetTexture!=null)continue;priorTargets[camera]=camera.targetTexture;camera.targetTexture=target;camera.Render();camera.targetTexture=priorTargets[camera];}RenderTexture.active=target;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var pixels=image.GetPixels32();int visible=0;for(int i=0;i<pixels.Length;i+=3600)if(pixels[i].r+pixels[i].g+pixels[i].b>35)visible++;Check("Nonblack game camera capture "+name,visible>12);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
   catch(Exception e){Check("Capture "+name+": "+e.Message,false);}
   finally{foreach(var pair in priorTargets)if(pair.Key)pair.Key.targetTexture=pair.Value;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(image)Destroy(image);}
  }
 }
}
