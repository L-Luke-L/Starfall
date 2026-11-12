using UnityEngine;

namespace Starfall {
    // Every exploration loop shares the same 16-second clock as the two rhythm layers.
    internal sealed class ExpeditionSoundscape : MonoBehaviour {
        const string AudioRoot = "ExpeditionAudio/";
        CreatureArena arena;
        AudioSource exploration, pulse, drums, footsteps;
        readonly AudioClip[] dustSteps = new AudioClip[3], metalSteps = new AudioClip[3];
        float combatTime, duck, intensity;
        int region, stepIndex;
        bool started, paused;
        public float Intensity => intensity;

        public void Configure(CreatureArena owner) {
            arena = owner;
        }

        public void SetRegion(int id) {
            region = Mathf.Clamp(id, 0, 5);
        }

        public void Action(float seconds, float emphasis = .20f) {
            combatTime = Mathf.Max(combatTime, seconds);
            duck = Mathf.Max(duck, emphasis);
        }

        AudioSource Source(string name, AudioClip clip, bool loop) {
            var g = new GameObject(name);
            g.transform.SetParent(transform, false);
            var source = g.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            return source;
        }

        void StartScore() {
            exploration = Source("Exploration and regional ambience", Resources.Load<AudioClip>(AudioRoot + "Zone" + region), true);
            pulse = Source("Threat rhythm", Resources.Load<AudioClip>(AudioRoot + "Threat"), true);
            drums = Source("Combat percussion", Resources.Load<AudioClip>(AudioRoot + "Combat"), true);
            exploration.volume = pulse.volume = drums.volume = 0;
            double start = AudioSettings.dspTime + .08;
            if (exploration.clip) exploration.PlayScheduled(start);
            if (pulse.clip) pulse.PlayScheduled(start);
            if (drums.clip) drums.PlayScheduled(start);
            started = true;
        }

        void Update() {
            if (!arena) return;
            var campaign = CampaignRuntime.Current;
            if (!arena.Active || (campaign && campaign.CinematicActive)) {
                if (started && !paused) {
                    exploration.Pause();
                    pulse.Pause();
                    drums.Pause();
                    paused = true;
                }
                return;
            }
            if (!started) StartScore();
            if (paused) {
                exploration.UnPause();
                pulse.UnPause();
                drums.UnPause();
                paused = false;
            }

            float dt = Time.deltaTime;
            combatTime = Mathf.Max(0, combatTime - dt);
            duck = Mathf.MoveTowards(duck, 0, dt * 2.5f);
            var boss = campaign && campaign.RegionView ? campaign.RegionView.boss : null;
            bool guardianScore = boss && boss.ScoreActive;
            float goal = !arena.CanTarget || region == 5 ? 0 : combatTime > 0 ? 1 : arena.PursuerCount > 0 ? .48f : 0;
            intensity = Mathf.MoveTowards(intensity, goal, dt * (goal > intensity ? 1.7f : .43f));

            float spaceForEffects = 1 - duck;
            float air = (guardianScore ? .08f : Mathf.Lerp(.47f, .34f, intensity)) * spaceForEffects;
            exploration.volume = Mathf.MoveTowards(exploration.volume, air, dt * .62f);
            pulse.volume = Mathf.SmoothStep(0, .58f, intensity) * spaceForEffects;
            drums.volume = Mathf.SmoothStep(0, .52f, Mathf.InverseLerp(.43f, 1, intensity)) * spaceForEffects;
        }

        public void Step(Vector3 feet, float weight) {
            if (!arena || !arena.Active || InteractionFeel.Muted) return;
            if (!footsteps) {
                footsteps = Source("Boot foley", null, false);
                for (int i = 0; i < dustSteps.Length; i++) {
                    dustSteps[i] = Resources.Load<AudioClip>(AudioRoot + "Dust" + i);
                    metalSteps[i] = Resources.Load<AudioClip>(AudioRoot + "Metal" + i);
                }
            }
            bool metal = false;
            if (Physics.Raycast(feet + Vector3.up * .16f, Vector3.down, out var hit, .80f, ~(1 << 30), QueryTriggerInteraction.Ignore)) {
                string surface = hit.collider.name;
                metal = surface.StartsWith("Vessel ") || surface.StartsWith("Accessible ") ||
                    surface.StartsWith("Habitat floor") || surface.StartsWith("Broken foredeck") ||
                    surface.StartsWith("Shipyard platform") || surface.StartsWith("Dock deck");
            }
            int iStep = stepIndex++ % dustSteps.Length;
            var clip = metal ? metalSteps[iStep] : dustSteps[iStep];
            if (clip) footsteps.PlayOneShot(clip, weight * .77f);
            else InteractionFeel.Play(FeelCue.Step, weight);
        }
    }
}
