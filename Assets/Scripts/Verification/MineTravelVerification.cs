using System;
using System.Collections;
using UnityEngine;

namespace Starfall {
 public static class MineTravelVerification {
  public static IEnumerator Run(CampaignRuntime c,SceneDirector d,Action<string,bool> check){
   float duration=d.transitionSeconds,maxLength=d.maxCableLength;int fps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
   var start=d.player.position;
   QualitySettings.vSyncCount=0;
   foreach(int rate in new[]{30,60,144}){
    Application.targetFrameRate=rate;yield return null;
    float began=Time.realtimeSinceStartup;yield return d.Travel(true);float elapsed=Time.realtimeSinceStartup-began;
    check("Mine travel finishes in 0.35-0.65 seconds at "+rate+" FPS",elapsed>=.35f&&elapsed<=.65f&&d.underground&&!d.Transitioning&&Mathf.Abs(d.worldCamera.transform.position.y+8)<.01f);
    yield return d.Travel(false);
   }
   Application.targetFrameRate=fps;QualitySettings.vSyncCount=vsync;

   d.StartCoroutine(d.Travel(true));yield return null;d.Interact();
   check("Mine E skip completes descent immediately",!d.Transitioning&&d.underground);
   d.Interact();check("Same-frame E does not start a second trip",!d.Transitioning&&d.underground);
   yield return new WaitForSeconds(duration+.1f);
   check("Skipped coroutine cannot move camera again",d.underground&&Mathf.Abs(d.worldCamera.transform.position.y+8)<.01f);
   yield return d.Travel(false);

   d.StartCoroutine(d.Travel(true));yield return null;c.Open("bag");
   check("Opening bag finishes travel before pausing",c.Modal&&!d.Transitioning&&d.underground&&Time.timeScale==0);c.Close();
   while(d.Transitioning)yield return null;
   d.StartCoroutine(d.Travel(false));yield return null;c.Open("journal");
   check("Opening journal finishes return before pausing",c.Modal&&!d.Transitioning&&!d.underground&&Time.timeScale==0);c.Close();
   while(d.Transitioning)yield return null;

   d.StartCoroutine(d.Travel(true));yield return null;d.SetPaused(true);var camera=d.worldCamera.transform.position;var player=d.player.position;
   yield return new WaitForSecondsRealtime(.15f);d.Interact();bool pausedCast=d.Cast();d.Move(1,.1f);
   check("Paused mine travel freezes and rejects gameplay",d.Transitioning&&d.worldCamera.transform.position==camera&&d.player.position==player&&!pausedCast);
   check("Paused direct completion keeps the camera frozen",!d.CompleteTravel()&&d.Transitioning&&d.worldCamera.transform.position==camera);
   c.Open("bag");check("Bag during Esc pause preserves frozen travel",c.Modal&&d.Transitioning&&d.worldCamera.transform.position==camera);c.Close();
   c.Open("journal");check("Journal during Esc pause preserves frozen travel",c.Modal&&d.Transitioning&&d.worldCamera.transform.position==camera);c.Close();
   c.Save("qa-paused-mine");check("Saving while paused keeps committed surface state",!c.Store.Load("qa-paused-mine").underground&&d.Transitioning);
   d.SetPaused(false);while(d.Transitioning)yield return null;yield return d.Travel(false);

   d.StartCoroutine(d.Travel(true));d.transitionSeconds=0;d.SetPaused(true);yield return new WaitForSecondsRealtime(.05f);
   check("Pause on final travel frame defers completion",d.Transitioning&&!d.underground);d.SetPaused(false);
   while(d.Transitioning)yield return null;check("Final-frame pause resumes to completion",d.underground);d.transitionSeconds=duration;yield return d.Travel(false);

   d.maxCableLength=1.1f;d.StartCoroutine(d.Travel(true));yield return null;
   bool cast=d.Cast();check("Space finishes descent and casts immediately",cast&&!d.Transitioning&&d.underground&&d.hookState==SceneDirector.HookState.Extending);
   check("Repeated space still rejects a second cast",!d.Cast());d.Interact();check("Busy hook cannot leave the mine",d.underground&&!d.Transitioning);
   while(d.Transitioning||d.hookState!=SceneDirector.HookState.Swinging)yield return null;d.maxCableLength=maxLength;
   if(!d.underground)yield return d.Travel(true);
   d.StartCoroutine(d.Travel(false));yield return null;float x=d.player.position.x;d.Move(1,.1f);
   check("Return movement finishes travel and moves immediately",!d.Transitioning&&!d.underground&&Mathf.Abs(d.player.position.x-x-.31f)<.001f);
   while(d.Transitioning)yield return null;

   yield return d.Travel(true);
   var collected=new bool[d.ores.Count];for(int i=0;i<d.ores.Count;i++){collected[i]=d.ores[i].collected;d.ores[i].collected=true;}
   check("Empty-hook speed check casts",d.Cast());float end=Time.realtimeSinceStartup+6;
   while(d.hookState==SceneDirector.HookState.Extending&&Time.realtimeSinceStartup<end)yield return null;
   float length=d.cableLength,beganReturn=Time.time;yield return null;float emptySpeed=(length-d.cableLength)/(Time.time-beganReturn);
   check("Empty hook retracts at 9.2 units per second",d.hookState==SceneDirector.HookState.Returning&&Mathf.Abs(emptySpeed-9.2f)<.05f);
   while(d.hookState!=SceneDirector.HookState.Swinging&&Time.realtimeSinceStartup<end)yield return null;
   var sample=d.ores[0];sample.collected=false;sample.transform.position=d.winchAnchor.position+(d.hook.position-d.winchAnchor.position).normalized*3;
   check("Loaded-hook speed check casts",d.Cast());end=Time.realtimeSinceStartup+6;
   while(d.hookState==SceneDirector.HookState.Extending&&Time.realtimeSinceStartup<end)yield return null;
   length=d.cableLength;beganReturn=Time.time;yield return null;float loadedSpeed=(length-d.cableLength)/(Time.time-beganReturn);
   check("Loaded hook preserves 3.312 units per second",d.hookState==SceneDirector.HookState.Returning&&sample.collected&&Mathf.Abs(loadedSpeed-3.312f)<.05f);
   while(d.hookState!=SceneDirector.HookState.Swinging&&Time.realtimeSinceStartup<end)yield return null;
   for(int i=0;i<d.ores.Count;i++)d.ores[i].collected=collected[i];yield return d.Travel(false);

   c.Save("qa-mine-travel");d.StartCoroutine(d.Travel(true));yield return null;yield return c.Restore("qa-mine-travel");
   check("Restore cancels mine travel immediately",!d.Transitioning&&!d.underground&&Mathf.Abs(d.worldCamera.transform.position.y)<.01f);
   yield return new WaitForSeconds(duration+.1f);check("Restore prevents old camera coroutine resuming",!d.underground&&Mathf.Abs(d.worldCamera.transform.position.y)<.01f);
   d.StartCoroutine(d.Travel(true));yield return null;yield return c.NewGame();
   check("New story cancels mine travel immediately",!d.Transitioning&&!d.underground);
   yield return new WaitForSeconds(duration+.1f);check("New story prevents old camera coroutine resuming",!d.underground&&Mathf.Abs(d.worldCamera.transform.position.y)<.01f);

   d.transitionSeconds=0;yield return d.Travel(true);check("Zero-duration travel reaches a finite endpoint",d.underground&&!d.Transitioning&&d.worldCamera.transform.position==new Vector3(0,-8,-20));yield return d.Travel(false);
   d.transitionSeconds=duration;d.player.position=start;
  }
 }
}
