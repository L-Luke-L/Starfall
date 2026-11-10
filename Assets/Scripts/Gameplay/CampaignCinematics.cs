using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public enum CampaignCinematicBeat { Crash, RiftArrival, Guardian, Collapse, Homecoming, Departure }

 // In-engine CG: every shot renders the current world, never substitutes a prerecorded ending.
 // All temporary render/input changes belong to one snapshot and are restored on every exit path.
 [DefaultExecutionOrder(1500)]
 public sealed class CampaignCinematics : MonoBehaviour {
  public bool IsPlaying { get; private set; }
  public bool IsPaused { get; private set; }
  public CampaignCinematicBeat CurrentBeat { get; private set; }
  public float Elapsed { get; private set; }
  public int CompletedCount { get; private set; }
  public int SkippedCount { get; private set; }
  public Camera ShotCamera => shotCamera;
  public static readonly float[] Durations = { 11, 8, 10, 10, 11, 10 };

  CampaignRuntime owner;
  Camera source, shotCamera;
  SceneDirector director;
  FirstPersonExplorer explorer;
  bool oldDirectorEnabled, oldRuntimeEnabled, oldReady, oldSourceEnabled, snapshot, skip;
  CursorLockMode oldCursorLock;
  bool oldCursorVisible;
  float oldScale, length, black, flare;
  int version, shot;
  Font font;
  GameObject temporary;
  Transform flyingShip;
  SpriteRenderer replacedShip;
  Vector3 focus, from, to, lookFrom, lookTo, shipLanding;
  float orthoFrom, orthoTo;
  readonly List<Renderer> hiddenRenderers = new List<Renderer>();
  readonly List<Material> materials = new List<Material>();
  readonly List<LineRenderer> rings = new List<LineRenderer>();
  readonly List<Transform> fragments = new List<Transform>();
  readonly List<Vector3> fragmentStarts = new List<Vector3>();
  AudioSource score;
  AudioClip scoreClip;
  Color accent;

  public static CampaignCinematics Ensure(CampaignRuntime runtime) {
   if (!runtime) return null;
   var cine = runtime.GetComponent<CampaignCinematics>();
   if (!cine) cine = runtime.gameObject.AddComponent<CampaignCinematics>();
   cine.owner = runtime;
   return cine;
  }

  public static string Title(CampaignCinematicBeat beat) {
   switch (beat) {
    case CampaignCinematicBeat.Crash: return "星陨 · 最后的信号";
    case CampaignCinematicBeat.RiftArrival: return "裂隙另一端";
    case CampaignCinematicBeat.Guardian: return "D4 · 守核者";
    case CampaignCinematicBeat.Collapse: return "让远征在此终结";
    case CampaignCinematicBeat.Homecoming: return "余烬之后，仍有新生";
    default: return "穿过星海，回家";
   }
  }

  public IEnumerator Play(CampaignCinematicBeat beat, float duration = -1) {
   if (IsPlaying || !owner || !isActiveAndEnabled) yield break;
   director = owner.director;
   explorer = owner.Explorer;
   source = explorer ? explorer.viewCamera : director ? director.worldCamera : null;
   if (!director || !source || (director.battle && director.battle.IsTransitioning)) yield break;
   // Closing a modal before taking the snapshot returns its time/cursor state to gameplay.
   owner.Close();
   CurrentBeat = beat;
   length = duration > 0 ? Mathf.Max(1, duration) : Durations[(int)beat];
   Elapsed = 0;
   IsPaused = false;
   skip = false;
   int ticket = ++version;
   IsPlaying = true;
   try {
    TakeSnapshot();
    BuildStage();
    BeginScore();
    for (shot = 0; shot < 3 && ticket == version && !skip; shot++) {
     SetupShot(shot);
     float t = 0, shotLength = length / 3;
     while (t < shotLength && ticket == version && !skip) {
      if (!IsPaused) {
       float dt = Time.unscaledDeltaTime;
       t += dt;
       Elapsed += dt;
       float n = Mathf.Clamp01(t / shotLength);
       black = Mathf.Max(1 - Mathf.Clamp01(t / .38f), 1 - Mathf.Clamp01((shotLength - t) / .28f));
       AnimateShot(n);
       AnimateStage(Mathf.Clamp01(Elapsed / length));
      }
      yield return null;
     }
    }
    if (ticket == version) {
     if (skip) SkippedCount++;
     CompletedCount++;
    }
   } finally {
    if (ticket == version) RestoreSnapshot();
   }
  }

  void TakeSnapshot() {
   snapshot = true;
   oldRuntimeEnabled = owner.enabled;
   oldDirectorEnabled = director.enabled;
   oldReady = explorer && explorer.Ready;
   oldSourceEnabled = source.enabled;
   oldCursorLock = Cursor.lockState;
   oldCursorVisible = Cursor.visible;
   oldScale = Time.timeScale;
   font = director.uiFont;
   // Disabling a MonoBehaviour leaves its running coroutines alive.
   owner.enabled = false;
   director.enabled = false;
   if (explorer) {
    explorer.Ready = false;
    if (explorer.arena) explorer.arena.SetPaused(true);
    foreach (var renderer in source.GetComponentsInChildren<Renderer>()) {
     if (!renderer.enabled) continue;
     hiddenRenderers.Add(renderer);
     renderer.enabled = false;
    }
   }
   Cursor.lockState = CursorLockMode.None;
   Cursor.visible = true;
   temporary = new GameObject("Starfall cinematic · temporary stage");
   var cameraObject = new GameObject("Cinematic camera · current scene");
   cameraObject.transform.SetParent(temporary.transform, false);
   shotCamera = cameraObject.AddComponent<Camera>();
   shotCamera.CopyFrom(source);
   shotCamera.depth = source.depth + 20;
   shotCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
   source.enabled = false;
   if (shotCamera.orthographic) cameraObject.AddComponent<SurfaceCameraFinish>();
  }

  public void SetPaused(bool paused) {
   if (!IsPlaying || IsPaused == paused) return;
   IsPaused = paused;
   if (score) { if (paused) score.Pause(); else score.UnPause(); }
  }

  public void Cancel() { version++; RestoreSnapshot(); }

  void RestoreSnapshot() {
   IsPlaying = false;
   IsPaused = false;
   if (temporary) { temporary.SetActive(false); Destroy(temporary); }
   temporary = null;
   shotCamera = null;
   flyingShip = null;
   foreach (var renderer in hiddenRenderers) if (renderer) renderer.enabled = true;
   hiddenRenderers.Clear();
   replacedShip = null;
   foreach (var material in materials) if (material) Destroy(material);
   materials.Clear();
   rings.Clear(); fragments.Clear(); fragmentStarts.Clear();
   if (scoreClip) Destroy(scoreClip);
   scoreClip = null; score = null;
   if (!snapshot) return;
   snapshot = false;
   if (source) source.enabled = oldSourceEnabled;
   if (explorer) {
    explorer.Ready = oldReady;
    if (explorer.arena) explorer.arena.SetPaused(explorer.Paused || !oldReady);
   }
   if (director) director.enabled = oldDirectorEnabled;
   if (owner) owner.enabled = oldRuntimeEnabled;
   Cursor.lockState = oldCursorLock;
   Cursor.visible = oldCursorVisible;
   Time.timeScale = oldScale;
  }

  void Update() {
   if (!IsPlaying) return;
   if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!IsPaused);
   if (!IsPaused && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) skip = true;
   if (Input.GetKeyDown(KeyCode.F11)) Screen.fullScreen = !Screen.fullScreen;
  }

  void OnDisable() { Cancel(); }
  void OnDestroy() { Cancel(); }

  void BuildStage() {
   bool destruction = CurrentBeat == CampaignCinematicBeat.Crash || CurrentBeat == CampaignCinematicBeat.Collapse;
   accent = destruction ? new Color(1, .48f, .18f) : new Color(.32f, .83f, 1);
   if (CurrentBeat == CampaignCinematicBeat.Homecoming) accent = new Color(.72f, .92f, .56f);
   focus = explorer ? new Vector3(-2, AlienSurfaceLandmarks.Height(-2, -5) + 2, -5) : new Vector3(0, director.surfaceY + 2.8f, 0);
   if (CurrentBeat == CampaignCinematicBeat.Guardian && owner.RegionView && owner.RegionView.boss)
    focus = owner.RegionView.boss.transform.position + Vector3.up * 3;
   if (CurrentBeat == CampaignCinematicBeat.Collapse && explorer)
    focus = new Vector3(0, AlienSurfaceLandmarks.Height(0, 21) + 3, 21);
   if ((CurrentBeat == CampaignCinematicBeat.Crash || CurrentBeat == CampaignCinematicBeat.Departure) && !explorer)
    focus = director.wreck.transform.position + Vector3.up * 1.5f;

   var shader = Shader.Find("Sprites/Default");
   if (!shader) shader = Shader.Find("Unlit/Color");
   if (!shader) return;
   var glow = new Material(shader) { name = "Cinematic luminous geometry", color = accent };
   materials.Add(glow);
   // Three fine, independently expanding energy rings read at both world scales.
   if (CurrentBeat != CampaignCinematicBeat.Crash && CurrentBeat != CampaignCinematicBeat.Departure)
    for (int i = 0; i < 3; i++) {
     var ring = new GameObject("Cinematic energy front " + i).AddComponent<LineRenderer>();
     ring.transform.SetParent(temporary.transform, false);
     ring.sharedMaterial = glow;
     ring.positionCount = 97;
     ring.useWorldSpace = true;
     ring.sortingOrder = 95;
     ring.startWidth = ring.endWidth = explorer ? .065f : .025f;
     ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
     rings.Add(ring);
    }
   // Deterministic authored particles have no collisions or gameplay components.
   for (int i = 0; i < 42; i++) {
    var spark = new GameObject("Cinematic ember " + i).AddComponent<LineRenderer>();
    spark.transform.SetParent(temporary.transform, false);
    spark.sharedMaterial = glow; spark.positionCount = 2;
    spark.useWorldSpace = false;
    spark.sortingOrder = 96;
    spark.startWidth = .018f; spark.endWidth = .003f;
    spark.SetPosition(0, Vector3.zero);
    spark.SetPosition(1, explorer ? new Vector3(.06f, .16f, 0) : new Vector3(.06f, .09f, 0));
    spark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    float a = i * 2.399963f, radius = 1.2f + (i % 9) * .45f;
    var start = focus + new Vector3(Mathf.Cos(a) * radius, -1.6f + (i % 8) * .48f, explorer ? Mathf.Sin(a) * radius : -1);
    spark.transform.position = start;
    fragments.Add(spark.transform); fragmentStarts.Add(start);
   }
   if (!explorer && (CurrentBeat == CampaignCinematicBeat.Crash || CurrentBeat == CampaignCinematicBeat.Departure)) {
    var art = director.wreck.transform.Find("Artwork");
    var original = art ? art.GetComponent<SpriteRenderer>() : null;
    if (original && original.sprite) {
     var vessel = new GameObject("Cinematic vessel · visual duplicate").AddComponent<SpriteRenderer>();
     vessel.transform.SetParent(temporary.transform, false);
     vessel.sprite = original.sprite; vessel.color = new Color(.84f, .91f, .95f);
     vessel.sortingOrder = 90;
     vessel.transform.localScale = original.transform.lossyScale;
     shipLanding = original.transform.position;
     flyingShip = vessel.transform;
     if (original.enabled) {
      replacedShip = original;
      hiddenRenderers.Add(original); original.enabled = false;
     }
    }
   }
  }

  void SetupShot(int index) {
   if (!shotCamera) return;
   if (shotCamera.orthographic) {
    var center = focus;
    float z = source.transform.position.z;
    if (CurrentBeat == CampaignCinematicBeat.Homecoming) center = new Vector3(0, director.surfaceY + 2, 0);
    from = new Vector3(center.x + (index == 0 ? -2 : index == 1 ? 1.2f : -1), center.y + (index == 0 ? 2 : .4f), z);
    to = from + new Vector3(index == 1 ? -1.6f : .9f, index == 2 ? .7f : -.3f, 0);
    orthoFrom = index == 0 ? 7.8f : index == 1 ? 5.3f : 8.6f;
    orthoTo = orthoFrom - .65f;
    if (CurrentBeat == CampaignCinematicBeat.Departure && index == 2) { from = new Vector3(0, 1, z); to = new Vector3(2, 4, z); }
    shotCamera.transform.rotation = source.transform.rotation;
   } else {
    Vector3 offset = index == 0 ? new Vector3(-13, 6.5f, -17) : index == 1 ? new Vector3(7, 2, -10) : new Vector3(-5, 2.8f, -12);
    if (CurrentBeat == CampaignCinematicBeat.Guardian && index == 1) offset = new Vector3(4, -.1f, -9);
    from = focus + offset;
    to = from + new Vector3(index == 1 ? -2.4f : 2.8f, index == 0 ? -1.4f : .35f, 1.8f);
    // A safe high overview is the first shot; later close-ups reveal the actual objective.
    lookFrom = focus + Vector3.up * (index == 0 ? 1 : .2f);
    lookTo = focus + Vector3.up * (CurrentBeat == CampaignCinematicBeat.Guardian && index == 2 ? 1.5f : .4f);
    shotCamera.fieldOfView = index == 0 ? 58 : 49;
   }
   AnimateShot(0);
  }

  void AnimateShot(float n) {
   if (!shotCamera) return;
   const float desired = 2.35f;
   float motion = InteractionFeel.ReducedMotion ? .5f : Mathf.SmoothStep(0, 1, n);
   shotCamera.transform.position = Vector3.Lerp(from, to, motion);
   shotCamera.aspect = desired;
   if (shotCamera.orthographic) {
    // The surface is an authored 32 x 18 stage. A wide CG lens must crop inside it,
    // including while centering on the wreck near the right edge of the stage.
    float size = Mathf.Min(Mathf.Lerp(orthoFrom, orthoTo, motion), 16f / desired);
    shotCamera.orthographicSize = size;
    float halfWidth = size * desired;
    var center = source.transform.position;
    var position = shotCamera.transform.position;
    position.x = Mathf.Clamp(position.x, center.x - 16 + halfWidth, center.x + 16 - halfWidth);
    position.y = Mathf.Clamp(position.y, center.y - 9 + size, center.y + 9 - size);
    shotCamera.transform.position = position;
   }
   else {
    Vector3 direction = Vector3.Lerp(lookFrom, lookTo, motion) - shotCamera.transform.position;
    if (direction.sqrMagnitude > .01f) shotCamera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
   }
   float ratio = Screen.width / (float)Mathf.Max(1, Screen.height);
   if (ratio > desired) { float width = desired / ratio; shotCamera.rect = new Rect((1 - width) * .5f, 0, width, 1); }
   else { float height = ratio / desired; shotCamera.rect = new Rect(0, (1 - height) * .5f, 1, height); }
  }

  void AnimateStage(float n) {
   bool collapse = CurrentBeat == CampaignCinematicBeat.Collapse;
   bool crash = CurrentBeat == CampaignCinematicBeat.Crash;
   // Brightness is deliberately limited, with no repeated flash or camera shake.
   flare = InteractionFeel.ReducedMotion ? 0 : collapse ? Mathf.Max(0, 1 - Mathf.Abs(n - .69f) * 9) * .19f : crash ? Mathf.Max(0, 1 - Mathf.Abs(n - .54f) * 16) * .12f : 0;
   for (int i = 0; i < rings.Count; i++) {
    float phase = Mathf.Repeat(n * 1.2f + i / 3f, 1), radius = collapse ? 2 + phase * 14 : 1.5f + phase * 7;
    var ring = rings[i];
    var color = accent; color.a = (1 - phase) * .55f;
    ring.startColor = ring.endColor = color;
    for (int j = 0; j <= 96; j++) {
     float a = j * Mathf.PI * 2 / 96;
     Vector3 offset = explorer ? new Vector3(Mathf.Cos(a) * radius, -.8f, Mathf.Sin(a) * radius) : new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -1);
     ring.SetPosition(j, focus + offset);
    }
   }
   for (int i = 0; i < fragments.Count; i++) {
    float phase = Mathf.Repeat(n * 1.4f + (i % 11) / 11f, 1);
    Vector3 drift = new Vector3(Mathf.Sin(i * 2.4f) * .8f, 1.2f + (i % 4) * .7f, 0) * phase;
    if (collapse) drift += (fragmentStarts[i] - focus).normalized * phase * 5;
    fragments[i].position = fragmentStarts[i] + drift;
    fragments[i].localScale = Vector3.one * (Mathf.Sin(phase * Mathf.PI) * (explorer ? 1.4f : .85f));
   }
   if (flyingShip) {
    if (crash) {
     float entry = Mathf.SmoothStep(0, 1, Mathf.Clamp01(n / .55f));
     flyingShip.position = shipLanding + Vector3.Lerp(new Vector3(-18, 12, 0), Vector3.zero, entry);
     flyingShip.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(-18, -4, entry));
     flyingShip.gameObject.SetActive(n < .57f);
     if (replacedShip) replacedShip.enabled = n >= .57f;
    } else {
     float launch = Mathf.Clamp01((n - .18f) / .82f);
     flyingShip.position = shipLanding + new Vector3(launch * launch * 15, launch * launch * 16, 0);
     flyingShip.rotation = Quaternion.Euler(0, 0, launch * 28);
    }
   }
   if (score) score.volume = .16f * Mathf.Min(Mathf.Clamp01(n * 8), Mathf.Clamp01((1 - n) * 7));
  }

  void BeginScore() {
   // A small original synthesized pad: quiet, deterministic, and controlled by the sound setting.
   const int rate = 16000, seconds = 12;
   var samples = new float[rate * seconds];
   float[] tones = CurrentBeat == CampaignCinematicBeat.Homecoming || CurrentBeat == CampaignCinematicBeat.Departure
    ? new[] { 130.81f, 164.81f, 196f, 261.63f } : new[] { 110f, 130.81f, 164.81f, 220f };
   for (int i = 0; i < samples.Length; i++) {
    float t = i / (float)rate, sum = 0;
    for (int j = 0; j < tones.Length; j++) sum += Mathf.Sin(2 * Mathf.PI * tones[j] * t + Mathf.Sin(t * .37f) * .12f) * (.07f - j * .009f);
    float pulse = .75f + .25f * Mathf.Sin(t * 2.1f);
    samples[i] = sum * pulse * Mathf.Min(1, t * 2) * Mathf.Min(1, (seconds - t) * 2);
   }
   scoreClip = AudioClip.Create("Starfall cinematic original ambient pad", samples.Length, 1, rate, false);
   scoreClip.SetData(samples, 0);
   score = temporary.AddComponent<AudioSource>(); score.clip = scoreClip; score.loop = true;
   score.spatialBlend = 0; score.volume = .12f; score.Play();
   InteractionFeel.Play(CurrentBeat == CampaignCinematicBeat.Guardian ? FeelCue.Alert : FeelCue.Portal, .45f);
  }

  string Subtitle() {
   switch (CurrentBeat) {
    case CampaignCinematicBeat.Crash:
     return shot == 0 ? "航行日志  07:16  /  引擎失效，正在接近未知星体。" : shot == 1 ? "求救频道只剩回声。地下，却传来了另一种脉搏。" : "回收矿物，锻造金属。修好飞船之前，先找出信号的源头。";
    case CampaignCinematicBeat.RiftArrival:
     return shot == 0 ? "空间坐标锁定。这里保存着同盟最后的足迹。" : shot == 1 ? "残骸、战场、医疗站与船坞，指向同一个名字：D4。" : "回家的路，和再次远征的航道，竟共用同一颗核心。";
    case CampaignCinematicBeat.Guardian:
     return shot == 0 ? "指挥官：你以为，你修好的是回家的路？" : shot == 1 ? "指挥官：那是我们重新出征的航道。" : "击破三处供能节点。在炮击预警之间，抓住核心暴露的窗口。";
    case CampaignCinematicBeat.Collapse:
     return shot == 0 ? "生命维持核查完成。现实耦合切断。安全锚点锁定。" : shot == 1 ? "这一次，裂隙不会再通向战场。" : "引爆核心，返回信标。把最后的光，留给活着的人。";
    case CampaignCinematicBeat.Homecoming:
     return shot == 0 ? "裂隙沉寂了。远征的命令，也终于不再回响。" : shot == 1 ? "关闭污染，恢复净水，把种子交还给土地。" : "你留下的，不是最后一座营地。是下一片家园。";
    default:
     return shot == 0 ? "船体、配电、冷却与导航，全部就绪。" : shot == 1 ? "引擎重新点燃。未知星体，渐渐退到身后。" : "群星仍在前方。这一次，航向是家。";
   }
  }

  void OnGUI() {
   if (!IsPlaying) return;
   var matrix = GUI.matrix; var color = GUI.color; int depth = GUI.depth;
   GUI.matrix = Matrix4x4.identity; GUI.depth = -900;
   float ratio = Screen.width / (float)Mathf.Max(1, Screen.height), aspect = 2.35f;
   float safeWidth = Mathf.Min(Screen.width, Screen.height * aspect), safeHeight = safeWidth / aspect;
   float x = (Screen.width - safeWidth) * .5f, y = (Screen.height - safeHeight) * .5f;
   Fill(new Rect(0, 0, Screen.width, y), Color.black);
   Fill(new Rect(0, Screen.height - y, Screen.width, y), Color.black);
   Fill(new Rect(0, 0, x, Screen.height), Color.black);
   Fill(new Rect(Screen.width - x, 0, x, Screen.height), Color.black);
   if (flare > 0) Fill(new Rect(x, y, safeWidth, safeHeight), new Color(accent.r, accent.g, accent.b, flare));
   if (black > 0) Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, black));
   float s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
   GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * s) * .5f, (Screen.height - 900 * s) * .5f, 0), Quaternion.identity, Vector3.one * s);
   float textAlpha = Mathf.Clamp01(1 - black);
   Text(new Rect(80, 52, 1000, 28), "STARFALL  /  星陨边境", 15, new Color(.64f, .75f, .78f));
   Text(new Rect(80, 91, 1150, 45), Title(CurrentBeat), 30, new Color(.91f, .87f, .75f));
   Fill(new Rect(82, 144, 82, 2), accent);
   Fill(new Rect(100, 724, 1400, 60), new Color(.008f, .018f, .028f, .78f * textAlpha));
   Text(new Rect(130, 731, 1340, 46), Subtitle(), 21, new Color(.94f, .96f, .96f, textAlpha), TextAnchor.MiddleCenter);
   Text(new Rect(80, 826, 1000, 30), IsPaused ? "演出已暂停" : "空格 / Enter  跳过本段    ·    Esc  暂停", 14, new Color(.65f, .74f, .78f));
   Text(new Rect(1190, 826, 330, 30), "实时演出  /  " + (shot + 1).ToString("00"), 13, new Color(.65f, .74f, .78f), TextAnchor.MiddleRight);
   if (IsPaused) {
    Fill(new Rect(0, 0, 1600, 900), new Color(.006f, .016f, .025f, .72f));
    Text(new Rect(450, 330, 700, 60), "演出暂停", 32, Color.white, TextAnchor.MiddleCenter);
    if (InteractionFeel.Button(new Rect(600, 434, 400, 50), "继续演出 · Esc", font, 22, accent)) SetPaused(false);
    if (InteractionFeel.Button(new Rect(600, 509, 400, 50), "跳过本段", font, 19, new Color(.82f, .84f, .84f))) { skip = true; SetPaused(false); }
    if (owner && owner.ShowcaseActive && InteractionFeel.Button(new Rect(550, 584, 500, 50), "退出演示 · 恢复原故事", font, 19, new Color(.92f, .73f, .51f))) owner.StopShowcase();
   }
   GUI.matrix = matrix; GUI.color = color; GUI.depth = depth;
  }

  static void Fill(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
  void Text(Rect rect, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft) {
   InteractionFeel.Text(rect, text, font, size, color, alignment);
  }
 }
}
