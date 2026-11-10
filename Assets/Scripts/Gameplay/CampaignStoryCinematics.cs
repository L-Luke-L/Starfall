using System.Collections;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  bool storyCinematicPending;

  void TickStoryCinematics() {
   if (IsQA || ShowcaseActive || ShowcaseRestoring || !StoryCinematicsEnabled || storyCinematicPending || CinematicActive ||
       !presentationReady || Busy || loading || saveBlocked || !director || director.IsPaused || director.Transitioning ||
       director.forge.IsOpen || director.battle.IsTransitioning) return;
   bool ending = HomecomingCinematicReady(State);
   // Completion opens the original ending panel; its modal may be temporarily closed for the epilogue.
   if (Modal && !(ending && panel == "saved")) return;
   if (Explorer && (Explorer.Paused || !Explorer.Ready || Explorer.arena.Downed) && !(ending && panel == "saved")) return;

   if (ending && !director.battle.IsEngaged) {
    QueueStoryCinematic(CampaignCinematicBeat.Homecoming, "cg-seen-homecoming", "saved");
   } else if (State.Has("detonation-active") && BombSeconds > 0 && Explorer && State.region == 5 && !State.Has("cg-seen-collapse")) {
    QueueStoryCinematic(CampaignCinematicBeat.Collapse, "cg-seen-collapse");
   } else if (State.region == 5 && RegionView && RegionView.boss && !RegionView.boss.Dead && !State.Has("cg-seen-guardian")) {
    QueueStoryCinematic(CampaignCinematicBeat.Guardian, "cg-seen-guardian");
   } else if (State.region == 1 && Explorer && !State.Has("cg-seen-rift")) {
    QueueStoryCinematic(CampaignCinematicBeat.RiftArrival, "cg-seen-rift");
   } else if (State.region == 0 && !director.underground && State.minedNodes.Count == 0 && !State.escapeComplete && !State.saviorComplete && !State.Has("cg-seen-crash")) {
    QueueStoryCinematic(CampaignCinematicBeat.Crash, "cg-seen-crash");
   }
  }

  internal static bool HomecomingCinematicReady(CampaignState story) {
   // The completion marker survives restoring an earlier checkpoint as a trophy.
   // Only the current world's completed ecology may launch the epilogue again.
   return story.saviorComplete && story.Has("rift-destroyed") && story.Has("ecology-chosen") &&
    story.Has("pollution-off") && story.Has("water-restored") && story.Has("seed-planted") && !story.Has("cg-seen-homecoming");
  }

  void QueueStoryCinematic(CampaignCinematicBeat beat, string seenFlag, string reopen = "") {
   if (storyCinematicPending || CinematicActive) return;
   storyCinematicPending = true;
   StartCoroutine(StoryCinematic(beat, seenFlag, reopen));
  }

  IEnumerator StoryCinematic(CampaignCinematicBeat beat, string seenFlag, string reopen = "") {
   var story = State;
   try {
    cinematics = CampaignCinematics.Ensure(this);
    State.Flag(seenFlag);
    Save();
    if (Modal) Close();
    yield return cinematics.Play(beat);
   } finally {
    storyCinematicPending = false;
    if (reopen != "" && State == story && !ShowcaseActive && !ShowcaseRestoring) Open(reopen);
   }
  }
 }
}
