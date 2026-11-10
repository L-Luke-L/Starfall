using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 public static class AuditPresentationVerification {
  public static IEnumerator Run(CampaignRuntime campaign, Action<string,bool> check) {
   var marker=CampaignRuntime.GuidanceViewportToCanvas(new Vector3(.75f,.5f,1),new Rect(0,0,1,1),new Vector2(1920,800));
   check("Ultrawide world guidance projects to its actual visible target",Vector2.Distance(marker,new Vector2(1340,450))<.01f);
   marker=CampaignRuntime.GuidanceViewportToCanvas(new Vector3(.5f,.75f,1),new Rect(0,0,1,1),new Vector2(1440,1080));
   check("Four-by-three guidance compensates for the centered GUI safe frame",Vector2.Distance(marker,new Vector2(800,150))<.01f);
   marker=CampaignRuntime.GuidanceViewportToCanvas(new Vector3(.75f,.75f,1),new Rect(0,.125f,1,.75f),new Vector2(1440,1080));
   check("Surface letterbox guidance retains its original alignment",Vector2.Distance(marker,new Vector2(1200,225))<.01f);
   var director = campaign.director;
   campaign.Close(); director.SetPaused(false);
   var cine = CampaignCinematics.Ensure(campaign); cine.Cancel(); cine.enabled = true;
   if (director.battle.IsEngaged) {
    while (director.battle.IsTransitioning) yield return null;
    director.battle.ReturnToExpedition();
    while (director.battle.IsEngaged) yield return null;
   }
   if (director.underground) yield return director.Travel(false);

   bool originalSavior = campaign.State.saviorComplete, originalEscape = campaign.State.escapeComplete;
   check("Presentation audit saves an isolated baseline",campaign.Save("audit-presentation-baseline"));
   campaign.State.saviorComplete = false;
   foreach (string flag in new[]{"rift-destroyed","ecology-chosen","pollution-off","water-restored","seed-planted","cg-seen-homecoming"}) campaign.State.flags.Remove(flag);
   check("Pre-epilogue checkpoint saves",campaign.Save("audit-before-homecoming"));
   campaign.State.saviorComplete = true;
   yield return campaign.Restore("audit-before-homecoming");
   check("Real checkpoint restore preserves the earned ending trophy",campaign.State.saviorComplete);
   check("Restored pre-core world cannot relaunch the ending from its trophy",!CampaignRuntime.HomecomingCinematicReady(campaign.State));
   foreach (string flag in new[]{"rift-destroyed","ecology-chosen","pollution-off","water-restored","seed-planted"}) campaign.State.Flag(flag);
   check("Actual restored-world completion still permits its first epilogue",CampaignRuntime.HomecomingCinematicReady(campaign.State));
   campaign.State.Flag("cg-seen-homecoming");
   check("Completed epilogue does not repeat every update",!CampaignRuntime.HomecomingCinematicReady(campaign.State));
   // Restore merges trophy markers by design, so remove only our fixture's marker first.
   campaign.State.saviorComplete = originalSavior; campaign.State.escapeComplete = originalEscape;
   yield return campaign.Restore("audit-presentation-baseline");
   campaign.Close(); director.SetPaused(false);

   var source = director.worldCamera;
   bool sourceEnabled = source.enabled, runtimeEnabled = campaign.enabled, directorEnabled = director.enabled;
   int cameras = Camera.allCamerasCount;
   var artwork = director.wreck.transform.Find("Artwork");
   var wreck = artwork ? artwork.GetComponent<SpriteRenderer>() : null;
   bool wreckEnabled = wreck && wreck.enabled;
   var playback = campaign.StartCoroutine(cine.Play(CampaignCinematicBeat.Crash,3));
   yield return null;
   check("CG snapshot takes exclusive control of the active camera",cine.IsPlaying && cine.ShotCamera && !source.enabled && !campaign.enabled && !director.enabled);
   cine.SetPaused(true); float pausedAt = cine.Elapsed;
   yield return new WaitForSecondsRealtime(.2f);
   check("Cinematic pause freezes its independent timeline",cine.IsPlaying && cine.IsPaused && Mathf.Abs(cine.Elapsed-pausedAt)<.001f);
   cine.enabled = false;
   yield return null;
   check("Disabling a paused CG restores camera, runtime and director",!cine.IsPlaying && !cine.ShotCamera && source.enabled==sourceEnabled && campaign.enabled==runtimeEnabled && director.enabled==directorEnabled);
   check("Cancelling an airborne crash restores the hidden wreck artwork",!wreck || wreck.enabled==wreckEnabled);
   check("Cancelling CG releases its temporary camera",Camera.allCamerasCount==cameras);
   cine.enabled = true;
   playback = campaign.StartCoroutine(cine.Play(CampaignCinematicBeat.Crash,3));
   yield return null;
   check("A cancelled CG component can play again",cine.IsPlaying && cine.ShotCamera);
   cine.Cancel();
   playback = campaign.StartCoroutine(cine.Play(CampaignCinematicBeat.Crash,3));
   yield return null;
   check("A previous cancelled iterator cannot disable a later camera snapshot",cine.IsPlaying && cine.ShotCamera && !source.enabled && !campaign.enabled);
   cine.Cancel(); yield return null;

   check("Showcase starts for a paused chapter-boundary fixture",campaign.StartShowcase());
   yield return null;
   cine.Cancel(); campaign.SetShowcasePaused(true);
   var shotMethod = typeof(CampaignRuntime).GetMethod("ShowcaseShot",BindingFlags.Instance|BindingFlags.NonPublic);
   var nextShot = campaign.StartCoroutine((IEnumerator)shotMethod.Invoke(campaign,new object[]{CampaignCinematicBeat.Crash}));
   float elapsed = campaign.ShowcaseElapsed;
   yield return new WaitForSecondsRealtime(.2f);
   check("A chapter-boundary pause prevents the next unscaled CG from starting",campaign.ShowcasePaused && !cine.IsPlaying && Mathf.Abs(campaign.ShowcaseElapsed-elapsed)<.001f);
   campaign.StopCoroutine(nextShot);
   campaign.StopShowcase(); float deadline = Time.realtimeSinceStartup+30;
   while (campaign.ShowcaseActive && Time.realtimeSinceStartup<deadline) yield return null;
   check("Stopping at a paused CG boundary restores the story",!campaign.ShowcaseActive && !campaign.ShowcaseRestoring && !cine.IsPlaying && campaign.enabled && source.enabled);
   campaign.Close(); director.SetPaused(false);
  }
 }
}
