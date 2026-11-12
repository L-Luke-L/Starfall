using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Starfall {
    internal sealed class ExpeditionWayfinding : MonoBehaviour {
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();

        public void Build(Transform beacon, int region) {
            if (!beacon) return;
            var plate = CampaignGeometry.Armor();
            var steel = Surface("Path fixture steel", new Color(.23f, .28f, .31f), false);
            var cyan = Surface("Cold route lighting", new Color(.19f, .78f, .94f), true);
            var amber = Surface("Warm route lighting", new Color(.95f, .44f, .14f), true);
            if (region == 4) {
                Route(beacon.position, new Vector3(17, 0, -9), plate, steel, cyan, 7, true);
                Route(beacon.position, new Vector3(-25, 0, 1), plate, steel, amber, 7, true);
            } else {
                Vector3 destination;
                switch (region) {
                    case 1: destination = new Vector3(0, 0, -8); break;
                    case 2: destination = new Vector3(0, 0, 13); break;
                    case 3: destination = new Vector3(-3, 0, 6); break;
                    case 5: destination = new Vector3(0, 0, -5); break;
                    default: destination = new Vector3(0, 0, -8); break;
                }
                Route(beacon.position, destination, plate, steel, region == 2 ? amber : cyan, 8, true);
            }
            Destroy(plate);
        }

        void Route(Vector3 origin, Vector3 destination, Mesh plate, Material metal, Material emitter, int limit, bool practicalLight) {
            destination.y = AlienSurfaceLandmarks.Height(destination.x, destination.z);
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(origin, out var start, 4, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destination, out var target, 4, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete) return;

            var bases = new List<CombineInstance>();
            var lamps = new List<CombineInstance>();
            float carry = 0;
            Vector3 last = start.position;
            var corners = path.corners;
            for (int i = 1; i < corners.Length && bases.Count < limit * 2; i++) {
                Vector3 from = corners[i - 1], to = corners[i], heading = to - from;
                float length = heading.magnitude;
                if (length < .05f) continue;
                heading /= length;
                var across = Vector3.Cross(Vector3.up, heading).normalized;
                var yaw = Quaternion.LookRotation(Vector3.Cross(across, Vector3.up));
                for (float along = 4.8f - carry; along < length && bases.Count < limit * 2; along += 4.8f) {
                    Vector3 center = from + heading * along;
                    if (CreatureArena.FlatDistance(center, origin) < 5) continue;
                    last = center;
                    for (int side = -1; side <= 1; side += 2) {
                        Vector3 p = center + across * (side * .78f);
                        p.y = FieldSurface.Height(p.x, p.z, center.y);
                        bases.Add(Plate(plate, p + Vector3.up * .043f, yaw, new Vector3(.30f, .095f, .19f)));
                        lamps.Add(Plate(plate, p + Vector3.up * .097f, yaw, new Vector3(.18f, .035f, .112f)));
                    }
                }
                carry = Mathf.Repeat(carry + length, 4.8f);
            }
            if (bases.Count == 0) return;
            Combine("Route fixtures", bases, metal);
            Combine("Route marker lenses", lamps, emitter);
            if (practicalLight) {
                var lamp = new GameObject("Route lighting spill").AddComponent<Light>();
                lamp.transform.SetParent(transform, false);
                lamp.transform.position = last + Vector3.up * .82f;
                lamp.type = LightType.Point;
                lamp.range = 5.6f;
                lamp.intensity = 2.15f;
                lamp.color = emitter.color;
                lamp.shadows = LightShadows.None;
            }
        }

        CombineInstance Plate(Mesh mesh, Vector3 point, Quaternion rotation, Vector3 scale) =>
            new CombineInstance { mesh = mesh, transform = transform.worldToLocalMatrix * Matrix4x4.TRS(point, rotation, scale) };

        void Combine(string label, List<CombineInstance> parts, Material material) {
            var mesh = new Mesh { name = label };
            mesh.CombineMeshes(parts.ToArray());
            meshes.Add(mesh);
            var g = new GameObject(label);
            g.transform.SetParent(transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        Material Surface(string label, Color color, bool lit) {
            var material = new Material(Shader.Find("Standard")) { name = label, color = color };
            material.SetFloat("_Metallic", .42f);
            material.SetFloat("_Glossiness", .42f);
            if (lit) {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .87f);
            }
            materials.Add(material);
            return material;
        }

        void OnDestroy() {
            foreach (var mesh in meshes) Destroy(mesh);
            foreach (var material in materials) Destroy(material);
        }
    }
}
