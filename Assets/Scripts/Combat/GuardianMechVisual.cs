using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
    internal sealed class GuardianMechVisual : MonoBehaviour {
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Material armor, trim, dark, steel, brass, glass, amber, cyan;
        readonly Transform[] muzzles = new Transform[2];
        readonly MeshRenderer[] chargeGlows = new MeshRenderer[2];
        MaterialPropertyBlock tint;
        Material reactorLight, barrelLight;
        Transform reactorAssembly, chassis;
        Transform[] cannons;
        readonly Quaternion[] shutdownArmStart = new Quaternion[2];
        float shutdownTime;
        public const float ShutdownSeconds = 2.25f;
        public bool ShutdownComplete => shutdownTime >= ShutdownSeconds;
        bool coreOpen, corePowered = true;

        void Awake() {
            tint = new MaterialPropertyBlock();
        }

        public void Build(Transform core, Transform[] arms, Transform[] nodes, CreatureArena arena) {
            armor = Surface("Weathered ceramic armor", new Color(.57f, .60f, .55f), .32f, .34f);
            trim = Surface("Patinated copper panels", new Color(.54f, .29f, .16f), .52f, .37f);
            dark = Surface("Graphite chassis", new Color(.19f, .22f, .23f), .34f, .27f);
            steel = Surface("Machined alloy", new Color(.64f, .71f, .73f), .66f, .57f);
            brass = Surface("Hazard inlays", new Color(.79f, .50f, .20f), .27f, .36f);
            glass = Surface("Smoked cockpit glass", new Color(.065f, .16f, .18f), .32f, .86f, false);
            amber = Glow("Optical indicator", new Color(1, .32f, .085f));
            barrelLight = Glow("Pulse ignition chamber", new Color(.64f, .22f, .074f));
            barrelLight.SetColor("_EmissionColor", new Color(.31f, .082f, .014f));
            cyan = Glow("Coolant power cells", new Color(.11f, .72f, .86f));

            chassis = Pivot(transform, "D4 articulated chassis", Vector3.zero);
            var body = Pivot(chassis, "D4 armor assembly", Vector3.zero);
            var legs = Pivot(transform, "Grounded legs", Vector3.zero);
            var bases = Pivot(transform, "Stationary power pedestals", Vector3.zero);
            cannons = arms;
            core.SetParent(chassis, false);
            BuildBody(body);
            for (int i = 0; i < arms.Length; i++) {
                int side = i == 0 ? -1 : 1;
                BuildLimb(body, legs, side);
                arms[i].SetParent(chassis, false);
                // Original pivots and their scaled box colliders still drive the cannons.
                var gun = Unscaled(arms[i], "Articulated pulse cannon");
                BuildCannon(gun, side);
                muzzles[i] = Pivot(gun, "Pulse origin", new Vector3(0, -.18f, -1.32f));
                Batch(gun);
                chargeGlows[i] = arena.MonsterFx.Glow(muzzles[i], "Cannon charge", muzzles[i].position, 1, MonsterAttackFx.Heat);
                chargeGlows[i].enabled = false;
            }
            reactorAssembly = Unscaled(core, "Moving weak-point reactor");
            reactorLight = Glow("Reactor lens", MonsterAttackFx.Plasma);
            Lens(reactorAssembly, Vector3.zero, .43f, .22f, reactorLight);
            for (int i = 0; i < nodes.Length; i++) {
                var plinth = new Vector3((i - 1) * 3.8f, 0, -2.2f);
                Tube(bases, "Anchored power pedestal", plinth + Vector3.up * .055f, plinth + Vector3.up * .43f, .72f, dark, 8);
                Tube(bases, "Power socket", plinth + Vector3.up * .37f, plinth + Vector3.up * .49f, .51f, steel, 12);
                var cell = Unscaled(nodes[i], "Power cell assembly");
                Tube(cell, "Armored cap", new Vector3(0, .46f, 0), new Vector3(0, .68f, 0), .45f, dark);
                Tube(cell, "Cell housing", new Vector3(0, -.66f, 0), new Vector3(0, -.35f, 0), .46f, trim);
                Tube(cell, "Plasma column", new Vector3(0, -.44f, 0), new Vector3(0, .53f, 0), .25f, cyan, 12);
                for (int j = 0; j < 4; j++) {
                    float angle = (j + .5f) * Mathf.PI / 2;
                    var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .38f;
                    Tube(cell, "Retaining strut", offset + Vector3.down * .45f, offset + Vector3.up * .52f, .044f, steel, 8);
                }
                Batch(cell);
            }
            Batch(body);
            Batch(legs);
            Batch(bases);
            Batch(reactorAssembly);
            SetCoreState(false, nodes[0].gameObject.activeSelf || nodes[1].gameObject.activeSelf || nodes[2].gameObject.activeSelf);
        }

        public Vector3 Muzzle(int index) => muzzles[index].position;

        public void SetCharge(float amount) {
            var chamber = new Color(.64f, .22f, .074f);
            barrelLight.SetColor("_EmissionColor", chamber * Mathf.Lerp(.36f, 3.2f, amount));
            foreach (var flare in chargeGlows) {
                flare.enabled = amount > 0;
                if (!flare.enabled) continue;
                flare.transform.localScale = Vector3.one * Mathf.Lerp(.45f, 1.55f, amount);
                var color = MonsterAttackFx.Heat;
                color.a = Mathf.Lerp(.36f, 1, amount);
                MonsterAttackFx.Tint(flare, tint, color);
            }
        }

        public void SetCoreState(bool open, bool powered) {
            if (open == coreOpen && powered == corePowered) return;
            coreOpen = open;
            corePowered = powered;
            reactorAssembly.gameObject.SetActive(open || powered);
            var color = open ? MonsterAttackFx.Heat : MonsterAttackFx.Plasma;
            reactorLight.color = color;
            reactorLight.SetColor("_EmissionColor", color * (open ? 2.1f : 1.1f));
        }

        public void ShutDown(CreatureArena arena, bool animate) {
            SetCharge(0);
            for (int i = 0; i < cannons.Length; i++) shutdownArmStart[i] = cannons[i].localRotation;
            shutdownTime = animate ? 0 : ShutdownSeconds;
            ApplyShutdownPose();
            if (animate) {
                arena.MonsterFx.Burst(reactorAssembly.position + transform.forward * -.32f, -transform.forward, MonsterAttackFx.Heat, 1.7f);
                InteractionFeel.Play(FeelCue.Kill, .66f);
            }
        }

        public void StepShutdown(CreatureArena arena, float dt) {
            if (ShutdownComplete) return;
            float before = shutdownTime;
            shutdownTime = Mathf.Min(ShutdownSeconds, shutdownTime + dt);
            ApplyShutdownPose();
            if (before < .43f && shutdownTime >= .43f)
                arena.MonsterFx.Burst(chassis.TransformPoint(new Vector3(1.63f, 3.93f, -.70f)), -transform.forward, MonsterAttackFx.Heat, 1.16f);
            if (before < 1.30f && shutdownTime >= 1.30f) {
                arena.MonsterFx.Burst(chassis.TransformPoint(new Vector3(-.97f, 2.4f, -.46f)), Vector3.up, MonsterAttackFx.Heat, .95f);
                InteractionFeel.Play(FeelCue.Step, .82f);
            }
        }

        void ApplyShutdownPose() {
            float collapse = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, 1.44f, shutdownTime));
            float settle = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(1.24f, 1.83f, shutdownTime));
            var rotation = Quaternion.Euler(12 * collapse - 2.6f * settle, -5 * collapse, 13 * collapse);
            var hips = new Vector3(0, 2.36f, .11f);
            chassis.localRotation = rotation;
            chassis.localPosition = hips - rotation * hips + Vector3.down * (.39f * collapse - .032f * settle);
            for (int i = 0; i < cannons.Length; i++)
                cannons[i].localRotation = Quaternion.Slerp(shutdownArmStart[i], Quaternion.Euler(-25 - i * 8, 0, i == 0 ? -9 : 8), collapse);
            float power = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.09f, .86f, shutdownTime));
            power *= .71f + .29f * Mathf.Cos(shutdownTime * 54);
            amber.SetColor("_EmissionColor", MonsterAttackFx.Heat * power);
            cyan.SetColor("_EmissionColor", MonsterAttackFx.Plasma * power);
            barrelLight.SetColor("_EmissionColor", MonsterAttackFx.Heat * power * .34f);
            reactorLight.SetColor("_EmissionColor", MonsterAttackFx.Heat * power * 1.65f);
            if (shutdownTime >= .92f) SetCoreState(false, false);
        }

        void BuildBody(Transform p) {
            Panel(p, "Load-bearing frame", new Vector3(0, 3.42f, .10f), new Vector3(2.88f, 2.08f, 1.60f), dark, .22f);
            Panel(p, "Waist assembly", new Vector3(0, 2.42f, .13f), new Vector3(2.28f, .60f, 1.30f), trim, .20f);
            for (int i = 0; i < 4; i++)
                Panel(p, "Reactor lift rail", new Vector3(0, 2.02f + .33f * i, -.77f), new Vector3(.49f, .12f, .24f), steel, .18f);
            Panel(p, "Cockpit throat bearing", new Vector3(0, 4.76f, .07f), new Vector3(.87f, .36f, .92f), steel, .28f);
            for (int side = -1; side <= 1; side += 2) {
                Panel(p, "Reactor guide channel", new Vector3(side * .27f, 2.67f, -.845f), new Vector3(.082f, 1.56f, .17f), dark, .15f);
                Panel(p, "Linear guide trim", new Vector3(side * .28f, 2.68f, -.94f), new Vector3(.039f, 1.44f, .035f), trim, .12f);
            }
            Panel(p, "Sloped cockpit shell", new Vector3(0, 5.19f, .02f), new Vector3(1.49f, 1.01f, 1.24f), armor, .23f, -9);
            Panel(p, "Recessed optical bay", new Vector3(0, 5.20f, -.60f), new Vector3(1.24f, .38f, .23f), dark, .25f);
            Panel(p, "Panoramic optical glass", new Vector3(0, 5.21f, -.725f), new Vector3(1.11f, .27f, .10f), glass, .24f);
            Panel(p, "Optical slit", new Vector3(0, 5.19f, -.778f), new Vector3(.88f, .045f, .045f), amber, .20f);
            Panel(p, "Cockpit brow", new Vector3(0, 5.42f, -.58f), new Vector3(1.38f, .18f, .42f), trim, .25f, -10);
            for (int side = -1; side <= 1; side += 2) {
                var plate = Pivot(p, "Chest plate hardware", new Vector3(side * .97f, 3.85f, -.70f));
                plate.localRotation = Quaternion.Euler(-9, 0, side * 15);
                Panel(plate, "Floating chest armor", Vector3.zero, new Vector3(1.22f, 1.43f, .58f), armor, .24f);
                Panel(plate, "Reinforced outer chest edge", new Vector3(side * .446f, .055f, -.299f), new Vector3(.099f, .72f, .054f), dark, .24f);
                for (int i = 0; i < 3; i++) {
                    var screw = new Vector3(side * .352f, -.39f + i * .33f, -.29f);
                    Tube(plate, "Chest fastener", screw, screw + Vector3.back * .033f, .038f, steel, 8);
                }
                if (side > 0) Stencil(plate);
                Panel(p, "Chest intake", new Vector3(side * 1.02f, 3.58f, -1.015f), new Vector3(.62f, .63f, .11f), dark, .18f, 0, side * 12);
                for (int i = 0; i < 4; i++)
                    Panel(p, "Cooling louver", new Vector3(side * 1.02f, 3.39f + i * .118f, -1.08f), new Vector3(.53f, .051f, .11f), steel, .20f, -23, side * 12);
                Panel(p, "Dorsal radiator case", new Vector3(side * .80f, 4.13f, .85f), new Vector3(.69f, 1.56f, .78f), trim, .18f, 9);
                for (int i = 0; i < 6; i++)
                    Panel(p, "Radiator heat sink", new Vector3(side * .80f, 3.69f + .15f * i, 1.24f), new Vector3(.57f, .068f, .24f), dark, .18f);
                Conduit(p, new Vector3(side * .86f, 4.42f, 1.14f), new Vector3(side * 2.2f, 4.05f, 1.31f),
                    new Vector3(side * 2.58f, 3.62f, .75f), new Vector3(side * 2.34f, 3.61f, .42f));
                Conduit(p, new Vector3(side * .66f, 3.03f, .80f), new Vector3(side * 1.28f, 2.95f, .95f),
                    new Vector3(side * 1.50f, 2.28f, .92f), new Vector3(side * 1.21f, 2.12f, .54f));
                Tube(p, "Cockpit telemetry aerial", new Vector3(side * .60f, 5.50f, .38f), new Vector3(side * .81f, side == 1 ? 6.20f : 5.93f, .53f), .043f, dark, 8);
                Tube(p, "Antenna foot", new Vector3(side * .60f, 5.45f, .38f), new Vector3(side * .65f, 5.65f, .42f), .09f, steel);
            }
        }

        void Stencil(Transform p) {
            // A small painted unit marking laid onto the armor face.
            var marks = new[] {
                new Vector4(-.20f,.24f,-.20f,.47f), new Vector4(-.20f,.47f,-.10f,.47f),
                new Vector4(-.10f,.47f,-.052f,.415f), new Vector4(-.052f,.415f,-.052f,.295f),
                new Vector4(-.052f,.295f,-.10f,.24f), new Vector4(-.10f,.24f,-.20f,.24f),
                new Vector4(.024f,.32f,.15f,.47f), new Vector4(.024f,.32f,.184f,.32f),
                new Vector4(.15f,.24f,.15f,.47f)
            };
            foreach (var mark in marks) {
                var a = new Vector2(mark.x, mark.y);
                var b = new Vector2(mark.z, mark.w);
                var delta = b - a;
                var center = (a + b) * .5f;
                Panel(p, "D4 stencil", new Vector3(center.x, center.y, -.293f),
                    new Vector3(delta.magnitude + .016f, .032f, .012f), brass, .13f, 0,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        void Conduit(Transform p, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            var previous = a;
            for (int i = 1; i <= 12; i++) {
                float t = i / 12f, u = 1 - t;
                var point = a * (u*u*u) + b * (3*u*u*t) + c * (3*u*t*t) + d * (t*t*t);
                var along = (point - previous).normalized;
                Tube(p, "Insulated conduit", previous - along * .026f, point + along * .026f, .067f, dark, 10);
                Tube(p, "Conduit rib", point - along * .03f, point + along * .016f, .085f, i % 4 == 0 ? trim : dark, 10);
                previous = point;
            }
        }

        void BuildLimb(Transform p, Transform legs, int side) {
            float x = side * 1.13f;
            Tube(p, "Shoulder bearing", new Vector3(side * 1.25f, 4.19f, .03f), new Vector3(side * 2.54f, 4.19f, .03f), .50f, dark);
            Panel(p, "Shoulder armor", new Vector3(side * 2.16f, 4.49f, .05f), new Vector3(1.62f, 1.17f, 1.72f), armor, .27f, -7, -side * 18);
            Panel(p, "Shoulder insert", new Vector3(side * 2.18f, 4.48f, -.82f), new Vector3(1.07f, .45f, .22f), trim, .26f, 0, -side * 18);
            for (int i = 0; i < 3; i++)
                Panel(p, "Shoulder hazard stripe", new Vector3(side * 2.18f + (i - 1) * .19f, 4.49f, -.951f), new Vector3(.09f, .23f, .025f), brass, .16f, 0, -23);
            Tube(p, "Upper arm", new Vector3(side * 2.15f, 4.02f, .20f), new Vector3(side * 2.27f, 3.35f, -.15f), .32f, dark);
            p = legs;
            Tube(p, "Hip joint", new Vector3(side * .77f, 2.38f, .11f), new Vector3(side * 1.24f, 2.38f, .11f), .40f, steel);
            Panel(p, "Thigh armor", new Vector3(side * .96f, 1.91f, -.02f), new Vector3(.85f, 1.24f, .98f), armor, .19f, -5, -side * 10);
            Tube(p, "Hydraulic barrel", new Vector3(x + side * .45f, .46f, .26f), new Vector3(x + side * .40f, 1.23f, .35f), .13f, dark);
            Tube(p, "Hydraulic ram", new Vector3(x + side * .40f, 1.15f, .35f), new Vector3(x + side * .34f, 1.76f, .44f), .073f, steel);
            Panel(p, "Shin underframe", new Vector3(x, .92f, -.10f), new Vector3(1.04f, 1.49f, 1.11f), dark, .18f, 5);
            Panel(p, "Overlapping greave", new Vector3(x, .87f, -.58f), new Vector3(.98f, 1.08f, .51f), armor, .24f, 9);
            Panel(p, "Knee shield", new Vector3(x, 1.55f, -.74f), new Vector3(.83f, .54f, .44f), trim, .26f, -16);
            Panel(p, "Greave power conduit", new Vector3(x, .80f, -.855f), new Vector3(.061f, .39f, .026f), cyan, .20f, 9);
            Panel(p, "Heel block", new Vector3(x, .24f, .10f), new Vector3(1.18f, .49f, 1.30f), dark, .18f, -3);
            for (int toe = -1; toe <= 1; toe += 2)
                Panel(p, "Split load bearing toe", new Vector3(x + toe * .30f, .22f, -.91f), new Vector3(.55f, .43f, 1.30f), armor, .20f, -8);
            for (int i = 0; i < 3; i++) {
                var pos = new Vector3(x + side * .37f, .59f + .22f * i, -.78f);
                Tube(p, "Countersunk greave fastener", pos, pos + Vector3.back * .075f, .043f, steel, 6);
            }
        }

        void BuildCannon(Transform p, int side) {
            Panel(p, "Cannon breech", new Vector3(0, .02f, .10f), new Vector3(.99f, 1.36f, 1.24f), dark, .18f);
            Panel(p, "Recoil shield", new Vector3(side * .32f, .01f, -.19f), new Vector3(.54f, 1.15f, 1.42f), trim, .26f, 5, side * 8);
            Tube(p, "Elbow pivot pin", new Vector3(side * .18f, .44f, .19f), new Vector3(side * .545f, .44f, .19f), .216f, dark, 16);
            Tube(p, "Elbow retention plate", new Vector3(side * .52f, .44f, .19f), new Vector3(side * .59f, .44f, .19f), .145f, steel, 12);
            Tube(p, "Pulse barrel", new Vector3(0, -.18f, -.14f), new Vector3(0, -.18f, -1.10f), .31f, steel);
            Lens(p, new Vector3(0, -.18f, -1.04f), .35f, .22f, barrelLight);
            for (int i = 0; i < 4; i++)
                Panel(p, "Cannon cooling fin", new Vector3(-side * .32f, .32f - i * .17f, -.51f), new Vector3(.20f, .064f, .65f), steel, .16f);
        }

        void Lens(Transform p, Vector3 at, float radius, float depth, Material light) {
            Tube(p, "Aperture shroud", at + Vector3.forward * .05f, at + Vector3.back * (depth - .10f), radius, dark);
            RecessedBezel(p, at + Vector3.back * depth, radius);
            Tube(p, "Inset ignition disc", at + Vector3.back * (depth - .11f),
                at + Vector3.back * (depth - .085f), radius * .49f, light, 24);
            for (int i = 0; i < 8; i++) {
                float a = Mathf.PI * i / 4;
                var pin = at + new Vector3(Mathf.Cos(a) * radius * .89f, Mathf.Sin(a) * radius * .89f, -depth);
                Tube(p, "Bezel bolt", pin, pin + Vector3.back * .035f, radius * .064f, trim, 6);
            }
        }

        void RecessedBezel(Transform parent, Vector3 face, float radius) {
            const int segments = 24;
            var profile = new[] {
                new Vector2(.96f, .13f), new Vector2(1, .044f), new Vector2(.92f, 0),
                new Vector2(.71f, 0), new Vector2(.62f, .055f), new Vector2(.60f, .112f)
            };
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int ring = 0; ring < profile.Length - 1; ring++) {
                int first = vertices.Count;
                for (int i = 0; i <= segments; i++) {
                    float fraction = i / (float)segments;
                    float angle = fraction * Mathf.PI * 2;
                    var radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    for (int side = 0; side < 2; side++) {
                        var edge = profile[ring + side];
                        vertices.Add(new Vector3(radial.x * edge.x * radius, radial.y * edge.x * radius, edge.y));
                        uv.Add(new Vector2(fraction * 1.35f, (ring + side) * .11f));
                    }
                    if (i == 0) continue;
                    int a = first + (i - 1) * 2;
                    triangles.AddRange(new[] { a, a+1, a+2, a+2, a+1, a+3 });
                }
            }
            var mesh = new Mesh { name = "Recessed machined bezel" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            var pivot = Pivot(parent, mesh.name, face);
            pivot.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            pivot.gameObject.AddComponent<MeshRenderer>().sharedMaterial = steel;
        }

        static Transform Pivot(Transform parent, string label, Vector3 at) {
            var pivot = new GameObject(label).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = at;
            return pivot;
        }

        static Transform Unscaled(Transform parent, string label) {
            var pivot = Pivot(parent, label, Vector3.zero);
            Vector3 scale = parent.localScale;
            pivot.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            return pivot;
        }

        Material Surface(string label, Color tint, float metal, float gloss, bool worn = true) {
            var m = new Material(Shader.Find("Standard")) { name = label, color = tint };
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Glossiness", gloss);
            if (worn) {
                m.mainTexture = RealismAssets.Texture("Textures/MetalColor");
                m.SetTexture("_BumpMap", RealismAssets.Texture("Textures/MetalNormal"));
                m.SetFloat("_BumpScale", .64f);
                m.EnableKeyword("_NORMALMAP");
            }
            materials.Add(m);
            return m;
        }

        Material Glow(string label, Color color) {
            var m = Surface(label, color, .08f, .57f, false);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.42f);
            return m;
        }

        void Panel(Transform p, string label, Vector3 pos, Vector3 size, Material material, float cut, float pitch = 0, float roll = 0) {
            float x = size.x / 2, y = size.y / 2, dx = size.x * cut, dy = size.y * cut;
            var profile = new[] {
                new Vector2(-x+dx,-y), new Vector2(x-dx,-y), new Vector2(x,-y+dy), new Vector2(x,y-dy),
                new Vector2(x-dx,y), new Vector2(-x+dx,y), new Vector2(-x,y-dy), new Vector2(-x,-y+dy)
            };
            Extrude(p, label, pos, Quaternion.Euler(pitch, 0, roll), profile, size.z, .81f, material);
        }

        void Tube(Transform p, string label, Vector3 a, Vector3 b, float radius, Material material, int sides = 16) {
            var profile = new Vector2[sides];
            for (int i = 0; i < sides; i++) {
                float angle = i * 2 * Mathf.PI / sides;
                profile[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            var axis = b - a;
            Extrude(p, label, (a+b)*.5f, Quaternion.FromToRotation(Vector3.forward, axis), profile, axis.magnitude, .90f, material);
        }

        void Extrude(Transform parent, string label, Vector3 pos, Quaternion rotation, Vector2[] profile, float depth, float inset, Material material) {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            float half = depth / 2, bevel = Mathf.Min(depth * .21f, .082f);
            float[] z = { -half, -half+bevel, half-bevel, half };
            float[] scale = { inset, 1, 1, inset };
            var textureOffset = new Vector2(pos.x + pos.z * .59f, pos.y - pos.z * .43f) * .38f;
            int n = profile.Length;
            for (int end = 0; end <= 3; end += 3) {
                int start = vertices.Count;
                foreach (var pt in profile) {
                    vertices.Add(new Vector3(pt.x * inset, pt.y * inset, z[end]));
                    uv.Add(pt * .72f + textureOffset);
                }
                for (int i = 1; i < n - 1; i++) {
                    indices.Add(start);
                    indices.Add(start + (end == 0 ? i+1 : i));
                    indices.Add(start + (end == 0 ? i : i+1));
                }
            }
            for (int ring = 0; ring < 3; ring++) {
                for (int i = 0; i < n; i++) {
                    Vector2 a = profile[i], b = profile[(i + 1) % n];
                    int start = vertices.Count;
                    vertices.Add(new Vector3(a.x * scale[ring], a.y * scale[ring], z[ring]));
                    vertices.Add(new Vector3(b.x * scale[ring], b.y * scale[ring], z[ring]));
                    vertices.Add(new Vector3(b.x * scale[ring+1], b.y * scale[ring+1], z[ring+1]));
                    vertices.Add(new Vector3(a.x * scale[ring+1], a.y * scale[ring+1], z[ring+1]));
                    float edge = Vector2.Distance(a, b) * .72f;
                    float strip = Mathf.Max(.005f, (z[ring+1] - z[ring]) * .72f);
                    var edgeOffset = textureOffset + a * .72f + Vector2.up * z[ring] * .72f;
                    uv.Add(edgeOffset);
                    uv.Add(edgeOffset + new Vector2(edge, 0));
                    uv.Add(edgeOffset + new Vector2(edge, strip));
                    uv.Add(edgeOffset + new Vector2(0, strip));
                    indices.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
                }
            }
            var mesh = new Mesh { name = label };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            var part = Pivot(parent, label, pos);
            part.localRotation = rotation;
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        void Batch(Transform assembly) {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in assembly.GetComponentsInChildren<MeshFilter>()) {
                var renderer = filter.GetComponent<MeshRenderer>();
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var parts)) groups[material] = parts = new List<CombineInstance>();
                parts.Add(new CombineInstance { mesh = filter.sharedMesh, transform = assembly.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                filter.gameObject.SetActive(false);
                Destroy(filter.gameObject);
            }
            foreach (var group in groups) {
                var mesh = new Mesh { name = group.Key.name };
                mesh.CombineMeshes(group.Value.ToArray());
                foreach (var part in group.Value) Destroy(part.mesh);
                meshes.Add(mesh);
                var pivot = Pivot(assembly, group.Key.name, Vector3.zero);
                pivot.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                pivot.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
        }

        void OnDestroy() {
            foreach (var mesh in meshes) Destroy(mesh);
            foreach (var material in materials) Destroy(material);
        }
    }
}
