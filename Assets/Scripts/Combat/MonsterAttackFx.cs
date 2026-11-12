using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Starfall {
    internal sealed class MonsterAttackFx {
        public static readonly Color Plasma = new Color(.19f, .78f, 1);
        public static readonly Color Heat = new Color(1, .49f, .20f);
        public static readonly Color Frost = new Color(.46f, .86f, 1);
        public static readonly Color Magnetic = new Color(.71f, .38f, .98f);
        readonly CreatureArena arena;
        readonly Material line, glow, particle, ground;
        readonly Mesh quad;

        public MonsterAttackFx(CreatureArena owner) {
            arena = owner;
            var shader = Resources.Load<Shader>("MonsterAttack");
            line = Material(shader, 0);
            particle = Material(shader, 1);
            glow = Material(shader, 1);
            glow.SetFloat("_Billboard", 1);
            ground = Material(shader, 2);
            quad = new Mesh { name = "Combat glow quad" };
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.bounds = new Bounds(Vector3.zero, Vector3.one * 1.5f);
        }

        static Material Material(Shader shader, int profile) {
            var material = new Material(shader);
            material.SetFloat("_Profile", profile);
            return material;
        }

        public void Dispose() {
            Object.Destroy(line);
            Object.Destroy(glow);
            Object.Destroy(particle);
            Object.Destroy(ground);
            Object.Destroy(quad);
        }

        public static Color BoltColor(AlienCreature owner) {
            var trait = owner ? owner.GetComponent<FrontierCreatureTrait>() : null;
            return trait && trait.frost ? Frost : Plasma;
        }

        public void Style(LineRenderer ribbon, Color color, float width, float endWidth) {
            ribbon.sharedMaterial = line;
            ribbon.shadowCastingMode = ShadowCastingMode.Off;
            ribbon.receiveShadows = false;
            ribbon.numCornerVertices = 3;
            ribbon.numCapVertices = 4;
            ribbon.startWidth = width;
            ribbon.endWidth = endWidth;
            ribbon.startColor = ribbon.endColor = color;
        }

        static GameObject ObjectAt(Transform parent, string label, Vector3 position) {
            var g = new GameObject(label);
            g.transform.SetParent(parent, false);
            g.transform.position = position;
            return g;
        }

        public MeshRenderer Glow(Transform parent, string label, Vector3 position, float size, Color color) {
            var g = ObjectAt(parent, label, position);
            g.transform.localScale = Vector3.one * size;
            g.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var tint = new MaterialPropertyBlock();
            Tint(renderer, tint, color);
            return renderer;
        }

        public static void Tint(Renderer renderer, MaterialPropertyBlock tint, Color color) {
            tint.SetColor("_Tint", color);
            renderer.SetPropertyBlock(tint);
        }

        public void Projectile(GameObject g, Color color) {
            Glow(g.transform, "Plasma core", g.transform.position, .59f, color);
            var trail = g.AddComponent<TrailRenderer>();
            trail.sharedMaterial = line;
            trail.time = .25f;
            trail.minVertexDistance = .085f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 3;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, .29f), new Keyframe(.22f, .19f), new Keyframe(1, 0));
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color * .69f, 1) },
                             new[] { new GradientAlphaKey(.94f, 0), new GradientAlphaKey(.52f, .43f), new GradientAlphaKey(0, 1) });
            trail.colorGradient = gradient;
        }

        public TrailRenderer MotionWake(Transform parent, Vector3 offset, Color color, float width) {
            var g = new GameObject("Attack motion wake");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = offset;
            var trail = g.AddComponent<TrailRenderer>();
            trail.sharedMaterial = line;
            trail.emitting = false;
            trail.time = .26f;
            trail.minVertexDistance = .10f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.numCapVertices = 3;
            trail.widthCurve = AnimationCurve.EaseInOut(0, width, 1, 0);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color * .62f, 1) },
                             new[] { new GradientAlphaKey(.45f, 0), new GradientAlphaKey(.22f, .45f), new GradientAlphaKey(0, 1) });
            trail.colorGradient = gradient;
            return trail;
        }

        public void Burst(Vector3 at, Vector3 normal, Color color, float size) {
            var g = ObjectAt(arena.transform, "Impact flash and scattering embers", at);
            var flash = Glow(g.transform, "Flash", at, size, color);
            var particles = g.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 18;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .63f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var overLife = particles.colorOverLifetime;
            overLife.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(color, 1) },
                         new[] { new GradientAlphaKey(.95f, 0), new GradientAlphaKey(.5f, .3f), new GradientAlphaKey(0, 1) });
            overLife.color = fade;
            var renderer = g.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particle;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.35f;
            renderer.velocityScale = .09f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            var basis = Quaternion.FromToRotation(Vector3.up, normal);
            for (int i = 0; i < 13; i++) {
                float angle = i * 2.39996f;
                float speed = .82f + (i % 5) * .23f;
                particles.Emit(new ParticleSystem.EmitParams {
                    position = at,
                    velocity = basis * new Vector3(Mathf.Cos(angle) * speed, .44f + .12f * (i % 7), Mathf.Sin(angle) * speed) * (1.2f + size),
                    startSize = Mathf.Lerp(.052f, .14f, (i % 7) / 6f) * Mathf.Sqrt(size),
                    startLifetime = .23f + .046f * (i % 8),
                    startColor = color
                }, 1);
            }
            g.AddComponent<MonsterFxLifetime>().Configure(arena, .61f, color, flash, particles);
        }

        public void Beam(Vector3 from, Vector3 to, Color color, float width, bool lightning = false) {
            var g = ObjectAt(arena.transform, lightning ? "Branched lightning" : "Cannon discharge", from);
            var main = g.AddComponent<LineRenderer>();
            Style(main, color, width, width * .56f);
            if (!lightning) {
                main.positionCount = 2;
                main.SetPosition(0, from);
                main.SetPosition(1, to);
                Burst(from, (to - from).normalized, color, .94f);
            } else {
                var path = new Vector3[14];
                for (int i = 0; i < path.Length; i++) {
                    float t = i / (path.Length - 1f);
                    path[i] = Vector3.Lerp(from, to, t) + Mathf.Sin(t * Mathf.PI) *
                        new Vector3(Mathf.Sin(i * 4.83f) * .72f, 0, Mathf.Cos(i * 2.72f) * .51f);
                }
                main.positionCount = path.Length;
                main.SetPositions(path);
                for (int i = 3; i <= 9; i += 3) {
                    var branch = ObjectAt(g.transform, "Fork", path[i]).AddComponent<LineRenderer>();
                    Style(branch, color, width * .55f, .01f);
                    float sign = i == 6 ? -1 : 1;
                    branch.positionCount = 3;
                    branch.SetPosition(0, path[i]);
                    branch.SetPosition(1, path[i + 1] + new Vector3(sign * .75f, 0, .25f));
                    branch.SetPosition(2, path[i + 2] + new Vector3(sign * 1.38f, -.36f, -.25f));
                }
            }
            g.AddComponent<MonsterFxLifetime>().Configure(arena, .28f, Color.white);
        }

        public void Sweep(Vector3 center, Vector3 forward, float from, float to, float radius, Color color) {
            var g = ObjectAt(arena.transform, "Melee wake", center);
            var ribbon = g.AddComponent<LineRenderer>();
            Style(ribbon, color, .33f, .12f);
            ribbon.positionCount = 24;
            for (int i = 0; i < ribbon.positionCount; i++) {
                float t = i / (ribbon.positionCount - 1f);
                var offset = Quaternion.AngleAxis(Mathf.Lerp(from, to, t), Vector3.up) * forward;
                ribbon.SetPosition(i, center + offset * radius + Vector3.up * (.26f * t));
            }
            g.AddComponent<MonsterFxLifetime>().Configure(arena, .23f, Color.white);
        }

        public MeshRenderer GroundPatch(GameObject g, Vector3 center, float radius, Color color, out Mesh mesh) {
            g = ObjectAt(g.transform, "Ground shimmer", center);
            const int segments = 48, rings = 5;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var indices = new List<int>();
            for (int r = 0; r <= rings; r++) {
                for (int i = 0; i <= segments; i++) {
                    float theta = i * 2 * Mathf.PI / segments;
                    var radial = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * (r / (float)rings);
                    var p = center + new Vector3(radial.x, 0, radial.y) * radius;
                    p.y = FieldSurface.Height(p.x, p.z, center.y) + .062f;
                    vertices.Add(g.transform.InverseTransformPoint(p));
                    uvs.Add(Vector2.one * .5f + radial * .5f);
                    colors.Add(Color.white);
                    if (r == 0 || i == 0) continue;
                    int b = vertices.Count - 1, a = b - segments - 1;
                    indices.AddRange(new[] { a - 1, a, b, a - 1, b, b - 1 });
                }
            }
            mesh = new Mesh { name = "Terrain-following attack footprint" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ground;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Tint(renderer, new MaterialPropertyBlock(), color);
            return renderer;
        }
    }

    internal sealed class MonsterFxLifetime : MonoBehaviour {
        CreatureArena arena;
        float age, duration;
        Color color;
        Renderer[] renderers;
        MeshRenderer flash;
        ParticleSystem particles;
        MaterialPropertyBlock tint;
        float flashScale;

        public void Configure(CreatureArena owner, float seconds, Color baseColor, MeshRenderer flare = null, ParticleSystem sparks = null) {
            arena = owner;
            duration = seconds;
            color = baseColor;
            flash = flare;
            particles = sparks;
            if (flash) flashScale = flash.transform.localScale.x;
            renderers = flare ? new Renderer[] { flare } : GetComponentsInChildren<LineRenderer>();
            tint = new MaterialPropertyBlock();
        }

        void Update() {
            if (!arena || !arena.Active) {
                if (particles && particles.isPlaying) particles.Pause();
                return;
            }
            if (particles && particles.isPaused) particles.Play();
            age += Time.deltaTime;
            if (age >= duration) { Destroy(gameObject); return; }
            var fading = color;
            float fraction = age / (flash ? .21f : duration);
            fading.a *= Mathf.Pow(Mathf.Clamp01(1 - fraction), 1.55f);
            if (flash) flash.transform.localScale = Vector3.one * flashScale * (1 + Mathf.Clamp01(fraction) * 1.1f);
            foreach (var renderer in renderers) MonsterAttackFx.Tint(renderer, tint, fading);
        }
    }
}
