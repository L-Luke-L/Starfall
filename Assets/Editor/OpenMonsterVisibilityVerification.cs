using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Unity 6000.3 BakeMesh(false) includes renderer scale in its vertices. Transform
// those vertices with position and rotation only; never multiply that scale twice.
public static class OpenMonsterVisibilityVerification {
    const string Sources = "Assets/ThirdParty/Quaternius/Monsters/";
    const string ReportDirectory = "QA/Reports/MonsterVisibility";
    static readonly string[] Ground = { "Dino", "Alien", "BlueDemon", "Yeti" };
    static readonly string[] Flying = { "Dragon", "Dragon_Evolved", "Glub_Evolved" };
    static readonly string[] States = { "Idle", "Move", "Attack" };
    static readonly float[] Times = { 0f, .25f, .5f, .75f };

    [MenuItem("Starfall/Monsters/Diagnose Visibility")]
    public static void Diagnose() {
        var report = new StringBuilder("Monster visibility diagnostics\n");
        int failures = 0;
        var scene = EditorSceneManager.NewPreviewScene();
        try {
            foreach (string name in Ground.Concat(Flying)) {
                bool flying = Flying.Contains(name);
                foreach (string path in new[] { Sources + name + ".fbx", ResourcePath(name, flying) }) {
                    GameObject instance = null;
                    try {
                        instance = Instantiate(path, scene);
                        var clips = AssetDatabase.LoadAllAssetsAtPath(Sources + name + ".fbx")
                            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                        var idle = clips.FirstOrDefault(c => SimpleName(c.name) == (flying ? "Flying_Idle" : "Idle"))
                            ?? clips.FirstOrDefault(c => SimpleName(c.name) == "Idle");
                        var animator = instance.GetComponentInChildren<Animator>(true);
                        if (idle) idle.SampleAnimation(animator ? animator.gameObject : instance, 0f);
                        report.AppendLine("ASSET " + path + " idle=" + (idle ? idle.name : "MISSING"));
                        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
                            report.AppendLine("  RENDERER " + renderer.name + " renderer.bounds=" + Format(renderer.bounds));
                            for (var current = renderer.transform; current; current = current.parent)
                                report.AppendLine("    TRANSFORM " + current.name + " localScale=" + V(current.localScale)
                                    + " lossyScale=" + V(current.lossyScale) + " localPosition=" + V(current.localPosition));
                            if (!(renderer is SkinnedMeshRenderer skinned)) continue;
                            foreach (bool useScale in new[] { false, true }) {
                                var mesh = new Mesh();
                                try {
                                    skinned.BakeMesh(mesh, useScale);
                                    var world = useScale ? skinned.transform.localToWorldMatrix
                                        : Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                                    report.AppendLine("    BakeMesh(" + useScale + ") vertices=" + mesh.vertexCount
                                        + " local=" + Format(mesh.bounds) + " world="
                                        + Format(GeometryUtility.CalculateBounds(mesh.vertices, world))
                                        + " rawLocalToWorld=" + Format(GeometryUtility.CalculateBounds(mesh.vertices, skinned.transform.localToWorldMatrix)));
                                } finally { Object.DestroyImmediate(mesh); }
                            }
                        }
                    } catch (Exception exception) {
                        failures++;
                        report.AppendLine("ERROR " + path + " " + exception);
                    } finally { if (instance) Object.DestroyImmediate(instance); }
                }
            }
        } finally { EditorSceneManager.ClosePreviewScene(scene); }
        Finish(report, "diagnostics.txt", failures, "STARFALL_MONSTER_DIAGNOSE");
    }

    [MenuItem("Starfall/Monsters/Verify Visibility")]
    public static void Run() {
        var report = new StringBuilder("Monster visibility regression; no gameplay or saves loaded.\n");
        int failures = 0, samples = 0;
        Directory.CreateDirectory(ReportDirectory);
        var scene = EditorSceneManager.NewPreviewScene();
        var cameraObject = new GameObject("Monster QA camera");
        var lightObject = new GameObject("Monster QA key light");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene;
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .035f, .06f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 40f;
        camera.cullingMask = 1 << 31;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        light.cullingMask = camera.cullingMask;
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        try {
            foreach (string name in Ground.Concat(Flying)) {
                bool flying = Flying.Contains(name);
                GameObject instance = null;
                try {
                    instance = Instantiate(ResourcePath(name, flying), scene);
                    foreach (var transform in instance.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
                    var animator = instance.GetComponentInChildren<Animator>(true);
                    Require(animator && animator.runtimeAnimatorController, name + " has an Animator controller");
                    var controller = animator.runtimeAnimatorController as AnimatorController;
                    Require(controller && controller.layers.Length > 0, name + " has an editable state controller");
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    Require(renderers.Length > 0, name + " has renderers");
                    foreach (var renderer in renderers) {
                        Require(renderer.enabled && renderer.gameObject.activeInHierarchy, name + " renderer is active");
                        Require(renderer.sharedMaterials.Length > 0, name + " has skin materials");
                        foreach (var material in renderer.sharedMaterials) {
                            Require(material && material.shader && material.shader.isSupported, name + " skin shader is supported");
                            Require(material.shader.name != "Hidden/InternalErrorShader", name + " skin shader is valid");
                            Require(material.mainTexture && material.mainTexture.dimension == TextureDimension.Tex2D,
                                name + " skin atlas is a 2D texture (not a cube texture)");
                        }
                    }
                    Vector3 target = new Vector3(0f, flying ? 0f : 1.4f, 0f);
                    camera.transform.position = target + new Vector3(6f, 3f, -9f);
                    camera.transform.LookAt(target);
                    foreach (string stateName in States) {
                        var state = controller.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == stateName).state;
                        Require(state, name + " has state " + stateName);
                        var clip = state.motion as AnimationClip;
                        Require(clip && clip.length > 0f, name + " " + stateName + " has an animation clip");
                        foreach (float normalizedTime in Times) {
                            clip.SampleAnimation(animator.gameObject, normalizedTime * clip.length);
                            Bounds bounds = GeometryBounds(instance, false);
                            Bounds compensated = GeometryBounds(instance, true);
                            string sample = name + " " + stateName + "@" + normalizedTime.ToString("F2", CultureInfo.InvariantCulture);
                            report.AppendLine(sample + " clip=" + clip.name + " " + Format(bounds));
                            Require(Finite(bounds.center) && Finite(bounds.size), sample + " has finite geometry");
                            Require((bounds.center - compensated.center).magnitude < .025f
                                && (bounds.size - compensated.size).magnitude < .025f,
                                sample + " BakeMesh(false) and compensated BakeMesh(true) world bounds agree");
                            Require(bounds.size.y > (flying ? .3f : 1f) && bounds.size.y < (flying ? 3.5f : 6.2f),
                                sample + " has gameplay-scale height");
                            Require(bounds.size.x > .3f && bounds.size.x < (flying ? 10f : 8f), sample + " has gameplay-scale width");
                            Require(bounds.center.magnitude < 6f, sample + " remains near its actor origin");
                            if (stateName == "Idle" && normalizedTime == 0f) {
                                if (flying) {
                                    Require(bounds.size.y <= 1.73f && bounds.size.x <= 6.48f, sample + " fits 1.7m height / 6.4m width");
                                    Require(Mathf.Abs(bounds.center.y) < .12f, sample + " is vertically centered");
                                } else {
                                    Require(Mathf.Abs(bounds.size.y - 3.05f) < .08f, sample + " is 3.05m tall");
                                    Require(Mathf.Abs(bounds.min.y - .03f) < .08f, sample + " feet are at actor ground level");
                                }
                                Require(Mathf.Abs(bounds.center.x) < .12f && Mathf.Abs(bounds.center.z - (flying ? .05f : .32f)) < .12f,
                                    sample + " is centered at its intended actor offset");
                            }
                            var image = Render(camera);
                            try {
                                int pixels = CountVisiblePixels(image);
                                report.AppendLine("  visiblePixels=" + pixels + "/" + (image.width * image.height));
                                Require(pixels > 150, sample + " visibly renders in a fixed 9m camera frame");
                                if (stateName == "Idle" && normalizedTime == 0f)
                                    File.WriteAllBytes(Path.Combine(ReportDirectory, name + ".png"), image.EncodeToPNG());
                            } finally { Object.DestroyImmediate(image); }
                            samples++;
                        }
                    }
                } catch (Exception exception) {
                    failures++;
                    report.AppendLine("FAIL " + name + " " + exception);
                } finally { if (instance) Object.DestroyImmediate(instance); }
            }
        } catch (Exception exception) {
            failures++;
            report.AppendLine("ERROR " + exception);
        } finally {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        report.AppendLine("Completed samples=" + samples + "/84");
        Finish(report, "visibility-report.txt", failures, "STARFALL_MONSTER_VISIBILITY");
    }

    static Bounds GeometryBounds(GameObject instance, bool compensated) {
        bool found = false;
        var result = new Bounds();
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
            Mesh mesh = null;
            bool temporary = false;
            try {
                Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
                if (renderer is SkinnedMeshRenderer skinned) {
                    mesh = new Mesh();
                    temporary = true;
                    skinned.BakeMesh(mesh, compensated);
                    if (!compensated) matrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
                } else {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter) mesh = filter.sharedMesh;
                }
                if (!mesh || mesh.vertexCount == 0) continue;
                var current = GeometryUtility.CalculateBounds(mesh.vertices, matrix);
                if (!found) { result = current; found = true; }
                else result.Encapsulate(current);
            } finally { if (temporary && mesh) Object.DestroyImmediate(mesh); }
        }
        Require(found, instance.name + " has baked geometry vertices");
        return result;
    }

    static Texture2D Render(Camera camera) {
        var target = RenderTexture.GetTemporary(384, 384, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        try {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(384, 384, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 384, 384), 0, 0);
            texture.Apply();
            return texture;
        } finally {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    static int CountVisiblePixels(Texture2D image) {
        Color32[] pixels = image.GetPixels32();
        Color32 background = pixels[0];
        int count = 0;
        foreach (Color32 pixel in pixels)
            if (Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) + Mathf.Abs(pixel.b - background.b) > 24) count++;
        return count;
    }

    static bool Finite(Vector3 value) {
        return !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y)
            || float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));
    }

    static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    static string ResourcePath(string name, bool flying) {
        return "Assets/Resources/OpenMonsters/" + (flying ? "Flying/" : "Ground/") + name + ".prefab";
    }

    static GameObject Instantiate(string path, Scene scene) {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!asset) throw new InvalidOperationException("Missing model: " + path);
        var instance = Object.Instantiate(asset);
        instance.name = asset.name;
        SceneManager.MoveGameObjectToScene(instance, scene);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return instance;
    }

    static string SimpleName(string name) { return name.Substring(name.LastIndexOf('|') + 1); }
    static string V(Vector3 value) { return value.ToString("F6", CultureInfo.InvariantCulture); }
    static string Format(Bounds value) { return "center=" + V(value.center) + " size=" + V(value.size); }

    static void Finish(StringBuilder report, string filename, int failures, string marker) {
        Directory.CreateDirectory(ReportDirectory);
        report.AppendLine(marker + (failures == 0 ? "_PASS" : "_FAIL") + " failures=" + failures);
        File.WriteAllText(Path.Combine(ReportDirectory, filename), report.ToString());
        Debug.Log(report.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
