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
  int wreckShots,wreckHits;bool reachedEvacuation;
  readonly List<string> results=new List<string>();readonly HashSet<string> runtimeErrors=new HashSet<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseQA")&&CampaignRuntime.Current)CampaignRuntime.Current.gameObject.AddComponent<ShowcaseVerification>();}
  void Check(string name,bool ok){string line=(ok?"PASS ":"FAIL ")+name;results.Add(line);if(!ok)failures++;Debug.Log("SHOWCASE_QA "+line);}
  void Log(string condition,string stack,LogType type){
   if(type==LogType.Exception||type==LogType.Assert)runtimeErrors.Add(condition);
   if(condition.StartsWith("SHOWCASE_ACTION wreck-shot ",StringComparison.Ordinal)){
    wreckShots++;
    if(condition.EndsWith(" hit=True",StringComparison.Ordinal))wreckHits++;
   }
   if(condition.StartsWith("SHOWCASE_ACTION evacuation-beacon ",StringComparison.Ordinal))reachedEvacuation=true;
  }
  IEnumerator Start(){
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseLifecycleOnly")?"../../../QA/Reports/ShowcaseLifecycle":"../../../QA/Reports/Showcase"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;watchdog=Time.realtimeSinceStartup+720;
   yield return new WaitForSecondsRealtime(2);campaign=CampaignRuntime.Current;director=campaign?campaign.director:null;
   Check("Verification uses independent QA campaign",campaign&&campaign.IsQA&&!campaign.Store.directory.EndsWith("CampaignV1",StringComparison.OrdinalIgnoreCase));
   if(!campaign){Finish();yield break;}
   // A hidden swap chain may ignore v-sync. Bound QA simulation and warm rendering.
   QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
   Screen.SetResolution(960,720,false);yield return new WaitForSecondsRealtime(.3f);
   Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.3f);
   var args=Environment.GetCommandLineArgs();
   var run=Array.IndexOf(args,"-expeditionPolishOnly")>=0?RunExpeditionPolish():Array.IndexOf(args,"-guardianShowcaseOnly")>=0?RunGuardianPreview():Array.IndexOf(args,"-showcaseEncounterOnly")>=0?RunWreckEncounter():Run();while(!finished){object next=null;bool more=false;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check("Unhandled verification exception: "+e,false);break;}if(!more)break;yield return next;}
   Finish();
  }
  void Update(){if(!finished&&watchdog>0&&Time.realtimeSinceStartup>watchdog){Check("Verification completed before watchdog",false);Finish();}}
  void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;Check("No unhandled runtime exceptions",runtimeErrors.Count==0);foreach(var error in runtimeErrors)results.Add("RUNTIME "+error);File.WriteAllLines(Path.Combine(folder,"showcase-report.txt"),results);Debug.Log(failures==0?"SHOWCASE_QA_PASS":"SHOWCASE_QA_FAIL "+failures);Application.Quit(failures==0?0:2);}
  static Dictionary<string,string> FilesAt(string directory){var files=new Dictionary<string,string>();foreach(var path in Directory.GetFiles(directory))files[path]=Convert.ToBase64String(File.ReadAllBytes(path));return files;}
  static bool SameFiles(string directory,Dictionary<string,string> expected){var current=FilesAt(directory);if(current.Count!=expected.Count)return false;foreach(var pair in expected)if(!current.TryGetValue(pair.Key,out var data)||data!=pair.Value)return false;return true;}
  IEnumerator RunExpeditionPolish(){
   for(int region=0;region<=5;region++){
    campaign.Close();director.SetPaused(false);
    if(region==0)director.battle.Enter(null);
    else{
     if(!campaign.State.coordinates.Contains(region))campaign.State.coordinates.Add(region);
     Check("Enter updated 3D region "+region,campaign.EnterRegion(region));
    }
    float until=Time.realtimeSinceStartup+25;
    while((!campaign.Explorer||director.battle.IsTransitioning||!campaign.Explorer.expedition.Initialized)&&Time.realtimeSinceStartup<until)yield return null;
    var fps=campaign.Explorer;
    if(!fps){Check("3D region loaded "+region,false);yield break;}
    fps.Automated=true;fps.SetPaused(false);
    yield return new WaitForSecondsRealtime(.55f);
    var arena=fps.arena;
    var score=arena.Soundscape;
    var sources=score.GetComponentsInChildren<AudioSource>();
    AudioSource exploration=Array.Find(sources,source=>source.clip&&source.clip.name=="Zone"+region);
    AudioSource threat=Array.Find(sources,source=>source.clip&&source.clip.name=="Threat");
    AudioSource drums=Array.Find(sources,source=>source.clip&&source.clip.name=="Combat");
    Check("Regional loop and synced layers start "+region,exploration&&threat&&drums&&exploration.isPlaying&&threat.isPlaying&&drums.isPlaying&&
     exploration.clip.samples==threat.clip.samples&&threat.clip.samples==drums.clip.samples);
    var wayfinding=arena.GetComponentInChildren<ExpeditionWayfinding>();
    Check("Non-colliding route lamps exist "+region,wayfinding&&wayfinding.GetComponentsInChildren<MeshRenderer>().Length>=2&&wayfinding.GetComponentsInChildren<Collider>().Length==0);
    if(region==4){
     Check("Two new solid cover positions",Array.FindAll(arena.GetComponentsInChildren<BoxCollider>(),box=>box.name=="Shipyard approach cover").Length==2);
     Check("Two illuminated routes through shipyard",wayfinding.GetComponentsInChildren<Light>().Length==2);
     CampaignShowcaseDriver.Warp(fps,new Vector3(-7,AlienSurfaceLandmarks.Height(-7,-8),-8));
     var wing=Array.Find(arena.creatures,creature=>creature&&creature.kind==CreatureKind.SpineFloater);
     foreach(var creature in arena.creatures)if(creature){
      creature.enabled=false;if(creature.Agent.isOnNavMesh)creature.Agent.isStopped=true;
      var elite=creature.GetComponent<EliteCreature>();if(elite)elite.enabled=false;
     }
     var titan=FindFirstObjectByType<TitanAreaAttack>();if(titan)titan.AttackEnabled=false;
     yield return null;
     var ground=Array.Find(arena.creatures,creature=>creature&&creature.kind==CreatureKind.RiftMauler);
     if(ground&&wing){
      ground.enabled=true;arena.TryEngage(ground);
      bool found=false;
      foreach(var spot in new[]{fps.transform.position,new Vector3(-14,0,-13),new Vector3(2,0,-13),new Vector3(-18,0,1)}){
       var target=new Vector3(spot.x,AlienSurfaceLandmarks.Height(spot.x,spot.z),spot.z);
       if(arena.TryFlank(wing,target,1,out var flank)&&CreatureArena.FlatDistance(target,flank)>8){found=true;break;}
      }
      Check("Flying dragon can take a reachable lateral firing position",found);
      ground.enabled=false;
     }
     fps.Look(0,22);
     int rounds=fps.arsenal.ActiveMagazine;
     Check("Firing still uses one pistol round",fps.arsenal.Attack()&&fps.arsenal.ActiveMagazine==rounds-1);
     yield return new WaitForSecondsRealtime(.8f);
     Check("A real shot raises combat percussion",score.Intensity>.65f&&drums.volume>.1f);
     Check("Reload starts after shot",fps.arsenal.Reload());
     yield return new WaitForSecondsRealtime(2.65f);
     Check("Reload still seats the full magazine",fps.arsenal.ActiveMagazine==rounds);
     fps.SetPaused(true);yield return new WaitForSecondsRealtime(.12f);
     Check("Pausing pauses all music layers",!exploration.isPlaying&&!threat.isPlaying&&!drums.isPlaying);
     fps.SetPaused(false);yield return new WaitForSecondsRealtime(.16f);
     Check("All music layers resume",exploration.isPlaying&&threat.isPlaying&&drums.isPlaying);
     var visual=wing?wing.GetComponentInChildren<RealismCreatureVisual>():null;
     var l=visual?RealismAssets.Bone(visual.transform,"l_wingFlapA_04"):null;
     var r=visual?RealismAssets.Bone(visual.transform,"r_wingFlapA_04"):null;
     float span=float.PositiveInfinity;
     if(l&&r){
      for(int i=0;i<8;i++){
       yield return new WaitForSecondsRealtime(.15f);
       span=Mathf.Min(span,Vector3.Dot(r.position-l.position,wing.transform.right));
      }
     }
     Check("Original wyvern stays spread through its flight cycle",l&&r&&span>3.45f);
     fps.arsenal.rifle.gameObject.SetActive(false);
     yield return CaptureScene("shipyard-lit-approach",new Vector3(-18,AlienSurfaceLandmarks.Height(-18,-27)+11,-27),new Vector3(6,3,3));
     fps.arsenal.rifle.gameObject.SetActive(true);
    }
    fps.Return();until=Time.realtimeSinceStartup+20;
    while(director.battle.IsEngaged&&Time.realtimeSinceStartup<until)yield return null;
    Check("Audio and route objects unload with region "+region,!score&&!wayfinding);
   }
  }
  IEnumerator RunWreckEncounter(){
   var original=campaign.State;var store=campaign.Store;
   Check("Wreck encounter uses the shipping mainline demo",campaign.StartShowcase());
   float deadline=Time.realtimeSinceStartup+110;
   while(campaign.ShowcaseActive&&!campaign.ShowcaseFinished&&campaign.ShowcaseStage<2&&Time.realtimeSinceStartup<deadline)yield return null;
   Check("Wreck encounter completes with live shots and hits",campaign.ShowcaseStage==2&&campaign.ShowcaseError==""&&wreckShots>=3&&wreckHits>0);
   Check("Wreck interaction records cargo and calibrates the return beacon",campaign.State.device&&campaign.State.calibrated&&campaign.State.Knows("J01")&&campaign.State.Knows("J02"));
   campaign.StopShowcase();deadline=Time.realtimeSinceStartup+25;
   while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<deadline)yield return null;
   Check("Wreck encounter exit restores the original state and store",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,store));
  }
  IEnumerator RunGuardianPreview(){
   var original=campaign.State;var store=campaign.Store;
   director.SetPaused(true);
   Check("Direct guardian preview starts from pause menu",campaign.StartGuardianShowcase());
   float deadline=Time.realtimeSinceStartup+18;
   while((!campaign.RegionView||!campaign.RegionView.boss||director.battle.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
   var boss=campaign.RegionView?campaign.RegionView.boss:null;
   Check("Preview opens the core region directly",boss&&campaign.ShowcaseStage==8&&campaign.ShowcaseElapsed<18);
   Check("Preview uses a separate store",!ReferenceEquals(store,campaign.Store)&&store.directory!=campaign.Store.directory);
   if(boss){
    float attackDeadline=Time.realtimeSinceStartup+15;
    while(boss.NodesRemaining==3&&Time.realtimeSinceStartup<attackDeadline)yield return null;
    yield return Capture("guardian-direct-combat");
    var score=boss.GetComponent<AudioSource>();
    Check("Guardian score is playing under combat",score&&score.clip&&score.isPlaying&&score.volume>0);
    Check("Guardian actually changes position in combat",boss.DistanceTravelled>1);
    campaign.SetShowcasePaused(true);var pausedPosition=boss.transform.position;
    yield return new WaitForSecondsRealtime(.4f);
    Check("Guardian movement freezes during pause",Vector3.Distance(pausedPosition,boss.transform.position)<.001f);
    campaign.SetShowcasePaused(false);
    deadline=Time.realtimeSinceStartup+125;
    while(!campaign.ShowcaseFinished&&Time.realtimeSinceStartup<deadline)yield return null;
    Check("Preview defeats the real guardian with live combat",boss.Dead&&campaign.ShowcaseFinished&&campaign.ShowcaseError=="");
    Check("Guardian completes its shutdown before the result screen",boss.ShutdownComplete);
    var deadPosition=boss.transform.position;yield return new WaitForSecondsRealtime(.3f);
    Check("Defeated guardian stays in place",Vector3.Distance(deadPosition,boss.transform.position)<.001f);
    Check("Guardian cannon fires and the player demonstrably leaves its active lane",boss.Attacks>0&&campaign.ShowcaseGuardianDodges>0);
   }
   campaign.StopShowcase();deadline=Time.realtimeSinceStartup+25;
   while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<deadline)yield return null;
   Check("Preview exits to the original campaign",!campaign.ShowcaseActive&&ReferenceEquals(original,campaign.State)&&ReferenceEquals(store,campaign.Store)&&!original.Has("boss-defeated"));
   director.SetPaused(false);
   campaign.State.coordinates.Add(1);
   Check("Ground dragon region opens",campaign.EnterRegion(1));
   deadline=Time.realtimeSinceStartup+22;
   RealismCreatureVisual dragon=null;AlienCreature actor=null;
   while(Time.realtimeSinceStartup<deadline){
    var explorer=campaign.Explorer;
    if(explorer&&!director.battle.IsTransitioning){
     actor=Array.Find(explorer.arena.creatures,creature=>creature&&creature.kind==CreatureKind.RiftMauler);
     if(actor)dragon=actor.GetComponentInChildren<RealismCreatureVisual>();
     if(dragon)break;
    }
    yield return null;
   }
   Check("Ground dragon with imported rig loads",dragon);
   if(!dragon)yield break;
   var left=RealismAssets.Bone(dragon.transform,"bn_LeftEye");
   var right=RealismAssets.Bone(dragon.transform,"bn_RightEye");
   Check("Both skull calibration landmarks exist",left&&right);
   if(!left||!right)yield break;
   var fps=campaign.Explorer;
   var frame=actor.transform;
   var ahead=frame.position+frame.forward*8;
   CampaignShowcaseDriver.Warp(fps,ahead);
   actor.AlertToPlayer();actor.enabled=false;if(actor.Agent.isOnNavMesh)actor.Agent.isStopped=true;
   yield return new WaitForSecondsRealtime(.9f);
   Check("Ground dragon eyes level and muzzle centered",Mathf.Abs(SkullYaw(frame,left,right))<7&&Mathf.Abs(Vector3.Dot((right.position-left.position).normalized,frame.up))<.12f);
   var head=RealismAssets.Bone(dragon.transform,"bn_Head");
   yield return CaptureScene("ground-dragon-front",head.position+frame.forward*4.6f+frame.up*.2f,head.position-frame.forward*.5f);
   CampaignShowcaseDriver.Warp(fps,ahead+frame.right*7);
   yield return new WaitForSecondsRealtime(.06f);
   Check("Head changes direction without snapping",Mathf.Abs(SkullYaw(frame,left,right))<20);
   yield return new WaitForSecondsRealtime(.85f);
   Check("Head turns right toward the player",SkullYaw(frame,left,right)>14);
   CampaignShowcaseDriver.Warp(fps,ahead-frame.right*7);
   yield return new WaitForSecondsRealtime(.95f);
   Check("Head turns left toward the player",SkullYaw(frame,left,right)<-14);
   yield return CaptureScene("ground-dragon-tracking",head.position+frame.forward*4.7f-frame.right*2.0f+frame.up*.25f,head.position-frame.forward*.55f);
  }
  static float SkullYaw(Transform frame,Transform left,Transform right){
   var forward=Vector3.Cross(right.position-left.position,frame.up);
   return Vector3.SignedAngle(frame.forward,forward,frame.up);
  }
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
   float mineWait=Time.realtimeSinceStartup+35;
   while(campaign.ShowcaseActive&&(campaign.CinematicActive||!director.underground||director.Transitioning)&&Time.realtimeSinceStartup<mineWait)yield return null;
   Check("Demo reaches active mine gameplay",campaign.ShowcaseActive&&!campaign.CinematicActive&&director.underground&&!director.Transitioning);
   yield return Capture("mine-hud");
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseLifecycleOnly")){
    campaign.Open("craft");
    yield return Capture("craft-panel-fixture",true);
    campaign.SetShowcasePaused(true);
    yield return Capture("craft-panel-paused");
    campaign.SetShowcasePaused(false);
    campaign.Close();
   }
   Check("Demo save succeeds in separate store",campaign.Save());Check("Demo writes cannot alter original campaign files",SameFiles(originalStore.directory,originalFiles));
   campaign.StopShowcase();float until=Time.realtimeSinceStartup+30;while(campaign.ShowcaseActive&&Time.realtimeSinceStartup<until)yield return null;
   Check("Early exit restores original state and store",!campaign.ShowcaseActive&&ReferenceEquals(campaign.State,original)&&ReferenceEquals(campaign.Store,originalStore));
   Check("Early exit restores gameplay camera and enabled components",!cine.IsPlaying&&!cine.ShotCamera&&director.worldCamera.enabled&&campaign.enabled&&director.enabled);
   Check("Early exit restores original progression and surface position",JsonUtility.ToJson(original)==originalJson&&Vector3.Distance(director.player.position,originalPosition)<.05f);
   Check("Early exit keeps original save files unchanged",SameFiles(originalStore.directory,originalFiles));
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-showcaseLifecycleOnly")){yield return LifecycleEdges();yield break;}
   wreckShots=0;wreckHits=0;reachedEvacuation=false;
   Check("A fresh complete showcase can restart after cancellation",campaign.StartShowcase());yield return null;originalJson=JsonUtility.ToJson(original);originalFiles=FilesAt(originalStore.directory);
   int lastStage=-1,ammoBeforeCraft=-1;var visited=new HashSet<int>();var milestones=new HashSet<string>();var deaths=new Dictionary<int,int>();bool testedForgePause=false;float runStart=Time.realtimeSinceStartup;
   while(campaign.ShowcaseActive&&!campaign.ShowcaseFinished&&Time.realtimeSinceStartup-runStart<650){
    int stage=campaign.ShowcaseStage;if(stage!=lastStage){lastStage=stage;visited.Add(stage);Debug.Log("SHOWCASE_QA_STAGE "+stage+" elapsed="+campaign.ShowcaseElapsed.ToString("F2"));yield return Capture("stage-"+stage.ToString("D2"));}
    string milestone=Milestone();if(milestone!=null&&milestones.Add(milestone))yield return Capture(milestone);
    string panelMilestone=PanelMilestone(ref ammoBeforeCraft);
    if(panelMilestone!=null&&milestones.Add(panelMilestone)){
     yield return Capture(panelMilestone,true);
     if(panelMilestone=="ammo-recipe"){
      campaign.SetShowcasePaused(true);float pausedAt=campaign.ShowcaseElapsed;
      yield return new WaitForSecondsRealtime(.35f);
      Check("Pausing on the crafting panel holds the recipe and the showcase clock",campaign.ShowcasePaused&&campaign.CurrentPanel=="craft"&&Mathf.Abs(campaign.ShowcaseElapsed-pausedAt)<.01f);
      yield return Capture("craft-panel-paused");
      campaign.SetShowcasePaused(false);
     }
    }
    if(campaign.Explorer)deaths[campaign.Explorer.GetInstanceID()]=campaign.Explorer.arena.RespawnCount;
    if(stage==2&&!testedForgePause&&director.forge.IsWorking&&!campaign.CinematicActive){testedForgePause=true;campaign.SetShowcasePaused(true);float pausedAt=campaign.ShowcaseElapsed,pointer=director.forge.Pointer;yield return new WaitForSecondsRealtime(.4f);Check("Gameplay pause freezes clock and unscaled forge QTE",campaign.ShowcasePaused&&director.forge.Paused&&Mathf.Abs(campaign.ShowcaseElapsed-pausedAt)<.01f&&Mathf.Abs(director.forge.Pointer-pointer)<.001f);campaign.SetShowcasePaused(false);}
    if(!string.IsNullOrEmpty(campaign.ShowcaseError))break;yield return null;
   }
   Check("Showcase reaches its result page",campaign.ShowcaseActive&&campaign.ShowcaseFinished&&string.IsNullOrEmpty(campaign.ShowcaseError));
   if(!string.IsNullOrEmpty(campaign.ShowcaseError))results.Add("SHOWCASE_ERROR "+campaign.ShowcaseError);
   Debug.Log("SHOWCASE_QA_DURATION "+campaign.ShowcaseElapsed.ToString("F2"));
   Check("Full unskipped presentation finishes below seven minutes",campaign.ShowcaseElapsed>0&&campaign.ShowcaseElapsed<420);
   Check("All eleven mainline chapters were shown",visited.Count==11);
   foreach(string panelShot in new[]{"coordinates-panel","chest-panel","forge-select-panel","ammo-recipe","ammo-crafted","archive-panel-2","archive-panel-3","archive-panel-4","archive-panel-5","bomb-recipe","bomb-crafted","ship-repair","ship-ready","core-controls","core-bomb-installed","detonation-panel","ecology-choice","guardian-ending"})
    Check("Full flow reaches "+panelShot,milestones.Contains(panelShot));
   Check("Wreck combat executes real visible pistol shots and hits",wreckShots>=3&&wreckHits>0);
   Check("Guardian cannon has a completed, damage-free dodge",campaign.ShowcaseGuardianDodges>0);
   Check("Timed evacuation reaches the beacon using the normal movement route",reachedEvacuation);
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
  string PanelMilestone(ref int ammoBeforeCraft){
   switch(campaign.CurrentPanel){
    case "coordinates":return "coordinates-panel";
    case "chest":return "chest-panel";
    case "forge":return "forge-select-panel";
    case "craft":
     if(campaign.ShowcaseStage==2){
      int ammo=campaign.State.bag.Count(ItemId.Ammo);
      if(ammoBeforeCraft<0)ammoBeforeCraft=ammo;
      return ammo>=ammoBeforeCraft+24?"ammo-crafted":"ammo-recipe";
     }
     return campaign.State.Has("bomb-crafted")?"bomb-crafted":"bomb-recipe";
    case "terminal":return "archive-panel-"+campaign.State.region;
    case "ship":
     if(campaign.State.Has("ship-fueled"))return "ship-ready";
     return campaign.State.repairs.Count==0?"ship-repair":null;
    case "core":return campaign.State.Has("bomb-installed")?"core-bomb-installed":"core-controls";
    case "detonate":return "detonation-panel";
    case "remain":return "ecology-choice";
    case "saved":return "guardian-ending";
    default:return null;
   }
  }
  IEnumerator Capture(string name,bool requirePanel=false){
   for(int attempt=0;attempt<6;attempt++){
    if(attempt>0)yield return new WaitForSecondsRealtime(.12f);
    yield return new WaitForEndOfFrame();
    Texture2D image=null;
    try{
     image=ScreenCapture.CaptureScreenshotAsTexture();
     if(!image)continue;
     var pixels=image.GetPixels32();
     int visible=0;for(int i=0;i<pixels.Length;i+=3600)if(pixels[i].r+pixels[i].g+pixels[i].b>35)visible++;
     if(visible<=12)continue;
     if(name=="opening-cg")Check("Opening capture includes the cinematic title overlay",HasHudInk(image,pixels,new Rect(80,52,300,28),false));
     if(name=="mine-hud")Check("Mine capture includes the live mining readout",HasHudInk(image,pixels,new Rect(1150,49,415,25),true));
     if(requirePanel)Check("Original gameplay panel is visible in "+name,HasPanelHeader(image,pixels));
     if(name=="ammo-crafted")Check("Ammo craft feedback is visible in the panel footer",HasInk(image,pixels,new Rect(160,799,390,27),pixel=>pixel.r>155&&pixel.r-pixel.b>55,16));
     if(name=="craft-panel-paused")Check("Showcase pause title is drawn above the crafting panel",HasInk(image,pixels,new Rect(690,239,220,42),pixel=>pixel.r>155&&pixel.r-pixel.b>55,24));
     File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
     Check("Nonblack game capture "+name,true);
    }
    catch(Exception e){Check("Capture "+name+": "+e.Message,false);}
    finally{if(image)Destroy(image);}
    yield break;
   }
   Check("Nonblack game capture "+name,false);
  }
  IEnumerator CaptureScene(string name,Vector3 position,Vector3 focus){
   yield return new WaitForEndOfFrame();var cameras=Camera.allCameras;Array.Sort(cameras,(a,b)=>a.depth.CompareTo(b.depth));bool hasWorld=false;foreach(var camera in cameras)if(camera&&camera.isActiveAndEnabled&&camera.targetTexture==null&&camera.cullingMask!=0)hasWorld=true;
   if(!hasWorld){Check("Active rendered camera exists for "+name,false);yield break;}
   var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;Texture2D image=null;var priorTargets=new Dictionary<Camera,RenderTexture>();
   var view=campaign.Explorer?campaign.Explorer.viewCamera:null;
   Vector3 oldPosition=view?view.transform.position:Vector3.zero;
   Quaternion oldRotation=view?view.transform.rotation:Quaternion.identity;
   try{if(view)view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(focus-position));RenderTexture.active=target;GL.Clear(true,true,Color.black);foreach(var camera in cameras){if(!camera||!camera.isActiveAndEnabled||camera.targetTexture!=null)continue;priorTargets[camera]=camera.targetTexture;camera.targetTexture=target;camera.Render();camera.targetTexture=priorTargets[camera];}RenderTexture.active=target;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var pixels=image.GetPixels32();int visible=0;for(int i=0;i<pixels.Length;i+=3600)if(pixels[i].r+pixels[i].g+pixels[i].b>35)visible++;Check("Nonblack game camera capture "+name,visible>12);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());}
   catch(Exception e){Check("Capture "+name+": "+e.Message,false);}
   finally{if(view)view.transform.SetPositionAndRotation(oldPosition,oldRotation);foreach(var pair in priorTargets)if(pair.Key)pair.Key.targetTexture=pair.Value;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(image)Destroy(image);}
  }
  static bool HasPanelHeader(Texture2D image,Color32[] pixels){
   return HasInk(image,pixels,new Rect(155,84,4,12),pixel=>pixel.r>155&&pixel.r-pixel.b>55,4)
    &&HasInk(image,pixels,new Rect(154,121,580,23),pixel=>pixel.r>170&&pixel.g>175&&pixel.b>170,24);
  }
  static bool HasHudInk(Texture2D image,Color32[] pixels,Rect area,bool cyan){
   return HasInk(image,pixels,area,pixel=>pixel.g>132&&pixel.b>140&&pixel.g-pixel.r>(cyan?30:12),16);
  }
  static bool HasInk(Texture2D image,Color32[] pixels,Rect area,Func<Color32,bool> matches,int minimum){
   float scale=Mathf.Min(image.width/1600f,image.height/900f);
   float xOffset=(image.width-1600*scale)/2,yOffset=(image.height-900*scale)/2;
   int left=Mathf.CeilToInt(xOffset+area.xMin*scale),right=Mathf.FloorToInt(xOffset+area.xMax*scale);
   int top=Mathf.CeilToInt(yOffset+area.yMin*scale),bottom=Mathf.FloorToInt(yOffset+area.yMax*scale),count=0;
   for(int y=top;y<bottom;y++)for(int x=left;x<right;x++){
    var pixel=pixels[(image.height-1-y)*image.width+x];
    if(matches(pixel)&&++count>=minimum)return true;
   }
   return false;
  }
 }
}
