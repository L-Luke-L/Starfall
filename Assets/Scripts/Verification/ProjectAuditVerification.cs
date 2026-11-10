using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 // Opt-in integration checks use CampaignVerification, never the player's CampaignV1.
 public sealed class ProjectAuditVerification:MonoBehaviour {
  CampaignRuntime campaign;SceneDirector director;string folder;float deadline;int failures;bool finished;
  readonly List<string> report=new List<string>();readonly HashSet<string> errors=new HashSet<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-projectAuditQA")&&CampaignRuntime.Current)CampaignRuntime.Current.gameObject.AddComponent<ProjectAuditVerification>();}
  void Check(string name,bool passed){report.Add((passed?"PASS ":"FAIL ")+name);if(!passed)failures++;Debug.Log("PROJECT_AUDIT "+report[report.Count-1]);}
  void Log(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
  IEnumerator Start(){
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/Reports/ProjectAudit/Targeted"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;deadline=Time.realtimeSinceStartup+360;
   yield return new WaitForSecondsRealtime(2);campaign=CampaignRuntime.Current;director=campaign?campaign.director:null;
   Check("Audit has an isolated campaign",campaign&&campaign.IsQA&&!campaign.Store.directory.EndsWith("CampaignV1",StringComparison.OrdinalIgnoreCase));if(!campaign){Finish();yield break;}
   var stack=new Stack<IEnumerator>();stack.Push(Run());
   while(stack.Count>0&&!finished){bool more=false;object next=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception ex){Check("Unhandled audit exception: "+ex,false);break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;}
   while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception ex){Check("Audit cleanup exception: "+ex,false);}}
   Finish();
  }
  void Update(){if(!finished&&deadline>0&&Time.realtimeSinceStartup>deadline){Check("Audit completes before watchdog",false);Finish();}}
  void Finish(){if(finished)return;finished=true;Application.logMessageReceived-=Log;Check("No unhandled runtime exceptions during audit",errors.Count==0);foreach(var error in errors)report.Add("EXCEPTION "+error);File.WriteAllLines(Path.Combine(folder,"audit-report.txt"),report);Debug.Log(failures==0?"PROJECT_AUDIT_PASS":"PROJECT_AUDIT_FAIL "+failures);Application.Quit(failures==0?0:2);}
  IEnumerator Run(){
   campaign.Close();director.SetPaused(false);
   CampaignStateAuditChecks.RunStoreChecks(Check);
   yield return CampaignStateAuditChecks.RunTreatmentChecks(campaign,Check);
   yield return CampaignStateAuditChecks.RunEmergencyChecks(campaign,Check);
   yield return campaign.NewGame();
   yield return HookResetChecks();
   yield return TransitionChecks();
   yield return SkyOwnershipChecks();
   yield return campaign.NewGame();
   yield return AuditPresentationVerification.Run(campaign,Check);
   yield return campaign.NewGame();
   yield return CombatAuditChecks.Run(campaign,Check);
  }
  IEnumerator Until(Func<bool> ready,float seconds,string label){float until=Time.realtimeSinceStartup+seconds;while(!ready()&&Time.realtimeSinceStartup<until)yield return null;Check(label,ready());}
  IEnumerator CarryOre(){
   campaign.Close();director.SetPaused(false);if(!director.underground)yield return director.Travel(true);yield return null;
   Interactable target=null;foreach(var ore in director.ores){var node=ore?ore.GetComponent<CampaignNode>():null;if(ore&&ore.gameObject.activeInHierarchy&&!ore.collected&&node&&!node.bonus){target=ore;break;}}
   Check("Hook fixture has a real fixed mineral",target);if(!target)yield break;
   var delta=target.transform.position-director.winchAnchor.position;
   typeof(SceneDirector).GetField("angle",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,Mathf.Atan2(delta.x,-delta.y)*Mathf.Rad2Deg);
   director.castSpeed=60;director.retrieveSpeed=.01f;Check("Hook fixture casts through normal gameplay",director.Cast());
   var cargo=typeof(SceneDirector).GetField("cargo",BindingFlags.Instance|BindingFlags.NonPublic);
   yield return Until(()=>cargo.GetValue(director)!=null&&director.hookState==SceneDirector.HookState.Returning,4,"Hook fixture physically catches and carries a mineral");
   campaign.Open("system");
  }
  IEnumerator HookResetChecks(){
   float cast=director.castSpeed,retrieve=director.retrieveSpeed;
   try{
    yield return CarryOre();yield return campaign.NewGame();director.retrieveSpeed=1000;
    Check("New story clears an in-flight hook and its cargo",director.hookState==SceneDirector.HookState.Swinging&&Mathf.Abs(director.cableLength-1.1f)<.001f);
    yield return new WaitForSecondsRealtime(.2f);
    Check("Old cargo cannot award minerals or coordinates to a new story",campaign.State.minedNodes.Count==0&&campaign.State.coordinates.Count==0&&campaign.State.bag.Count(ItemId.PurpleOre)==0&&!director.underground);
    Check("Hook regression checkpoint saves",campaign.Save("audit-empty-hook"));
    yield return CarryOre();yield return campaign.Restore("audit-empty-hook");director.retrieveSpeed=1000;
    Check("Loading a checkpoint clears an in-flight hook",director.hookState==SceneDirector.HookState.Swinging);
    yield return new WaitForSecondsRealtime(.2f);
    Check("Old cargo cannot contaminate restored inventory or mined records",campaign.State.minedNodes.Count==0&&campaign.State.coordinates.Count==0&&campaign.State.bag.Count(ItemId.PurpleOre)==0);
   }finally{director.castSpeed=cast;director.retrieveSpeed=retrieve;}
  }
  IEnumerator TransitionChecks(){
   campaign.State.coordinates.Add(1);Check("Transition regression saves a surface checkpoint",campaign.Save("audit-transition"));
   Check("Restore race fixture begins a real additive scene load",campaign.EnterRegion(1)&&director.battle.IsTransitioning);
   bool restored=false;var restore=campaign.StartCoroutine(Complete(campaign.Restore("audit-transition"),()=>restored=true));
   yield return Until(()=>restored,18,"Reading during an entry transition completes instead of waiting forever");
   if(!restored){campaign.StopCoroutine(restore);yield break;}
   Check("Transition restore returns to the correct surface view",!director.battle.IsEngaged&&!director.battle.IsTransitioning&&campaign.State.region==0&&director.enabled);
   Check("New-story race fixture begins a real additive scene load",campaign.EnterRegion(1)&&director.battle.IsTransitioning);
   bool reset=false;var newGame=campaign.StartCoroutine(Complete(campaign.NewGame(),()=>reset=true));
   yield return Until(()=>reset,18,"Starting a new story during entry completes instead of waiting forever");
   if(!reset){campaign.StopCoroutine(newGame);yield break;}
   Check("Transition new story leaves no stale encounter or progress",!director.battle.IsEngaged&&campaign.State.region==0&&campaign.State.coordinates.Count==0);
  }
  static IEnumerator Complete(IEnumerator routine,Action completed){yield return routine;completed();}
  IEnumerator SkyOwnershipChecks(){
   campaign.State.coordinates.Add(1);Check("Sky resource fixture enters a real region",campaign.EnterRegion(1));
   yield return Until(()=>campaign.Explorer&&!director.battle.IsTransitioning,18,"Sky resource fixture scene loads");if(!campaign.Explorer)yield break;
   yield return null;yield return null;yield return null;
   var planet=FindInRegion("Distant gas giant");var ring=FindInRegion("Planet orbital ring");
   var sky=RenderSettings.skybox;var planetMaterial=planet?planet.GetComponent<Renderer>().sharedMaterial:null;var ringMaterial=ring?ring.GetComponent<Renderer>().sharedMaterial:null;var ringMesh=ring?ring.GetComponent<MeshFilter>().sharedMesh:null;
   Check("Region owns its generated planet, ring and sky resources",sky&&planetMaterial&&ringMaterial&&ringMesh);
   director.battle.ReturnToExpedition();yield return Until(()=>!director.battle.IsEngaged,18,"Sky resource fixture unloads the region");yield return null;yield return null;
   Check("Unloading a region releases generated planet material",!planetMaterial);
   Check("Unloading a region releases generated ring mesh and material",!ringMaterial&&!ringMesh);
   Check("Unloading a region releases generated sky material",!sky);
  }
  Transform FindInRegion(string name){foreach(var root in campaign.Explorer.gameObject.scene.GetRootGameObjects())foreach(var target in root.GetComponentsInChildren<Transform>(true))if(target.name==name)return target;return null;}
 }
}
