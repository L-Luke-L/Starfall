using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed class SceneDirector : MonoBehaviour {
  [Header("Scene objects / editable references")]
  public Camera worldCamera;
  public Transform player, playerVisual, hook, winchAnchor, atmosphere;
  public SpriteRenderer playerSprite;
  public LineRenderer cable;
  public Interactable camp, mine, wreck;
  public List<Interactable> ores=new List<Interactable>();
  public Sprite pixel, glow, flame;
  public Font uiFont;
  public OreInventory inventory;
  public ForgeController forge;
  public RecoveryEncounter encounter;
  public BattleTravel battle;
  public ExpeditionWormhole wormhole;
  public Interactable wormholeSite;
  public bool IsPaused=>paused;
  public void SetPaused(bool value){paused=value;Time.timeScale=value?0:1;InteractionFeel.Play(FeelCue.Click,.5f);}
  public bool RecoveryLocked=>(encounter&&encounter.IsPresenting)||(battle&&battle.IsEngaged);
  [Header("Composition")]
  public float surfaceY=-3.8f, undergroundCameraY=-8.0f, transitionSeconds=2.4f;
  public float moveSpeed=3.1f, leftBound=-14.3f, rightBound=14.3f;
  [Header("Grapple")]
  public float swingDegrees=34, castSpeed=5.8f, retrieveSpeed=4.6f, maxCableLength=13.2f;
  public bool underground;
  public int oreCount=>inventory?inventory.UnknownCount:0;
  public int DisplayedOreCount {get;private set;}
  public bool PickupPopupVisible=>Time.time<orePopupUntil;
  public bool Transitioning {get;private set;}
  public enum HookState { Swinging, Extending, Returning }
  public HookState hookState {get;private set;}
  public float cableLength {get;private set;}=1.1f;
  float angle, targetAngle, toastUntil, startTime,moveIntent,stepClock,focusAge;
  public bool MinimalHud=>true;
  int pickupQuantity=1;
  float orePopupUntil=-1,orePopupStarted=-1,counterDelayUntil=-1,counterFlashUntil=-1;
  string toast="信号微弱。前往中央矿井，调查地下回声。";
  Interactable cargo, nearest;
  bool photoMode, paused, qaMode;
  Vector3 visualScale;
  Color cyan=new Color(.44f,.86f,.86f), gold=new Color(.87f,.73f,.47f), muted=new Color(.51f,.61f,.66f);
  void Start(){
   Application.targetFrameRate=60;QualitySettings.vSyncCount=1;
   visualScale=playerVisual.localScale; startTime=Time.time;toastUntil=0;
   if(!uiFont)uiFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);

   qaMode=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-starfallQA");
   if(qaMode)StartCoroutine(RunQA());
   else if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-undergroundPreview"))StartCoroutine(OpenUndergroundPreview());
   else if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-capturePreview"))StartCoroutine(CapturePreview());
  }
  void Update(){
   FitCamera();
   if(Time.time>=counterDelayUntil&&DisplayedOreCount!=oreCount){DisplayedOreCount=oreCount;counterFlashUntil=Time.time+.65f;}
   if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
   if(forge&&forge.IsOpen)return;
   if(CampaignRuntime.Current&&(CampaignRuntime.Current.Modal||CampaignRuntime.Current.Busy||CampaignRuntime.Current.ConsumedInput))return;
   if(Input.GetKeyDown(KeyCode.Tab))photoMode=!photoMode;
   if(Input.GetKeyDown(KeyCode.Escape))SetPaused(!paused);
   if(paused||RecoveryLocked)return;

   if(!Transitioning){
    float move=0;
    if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))move-=1;
    if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))move+=1;
    moveIntent=Mathf.MoveTowards(moveIntent,move,Time.deltaTime*12);if(!underground)Move(moveIntent,Time.deltaTime);
    FindNearest();
    if(Input.GetKeyDown(KeyCode.E))Interact();
    if(Input.GetKeyDown(KeyCode.Space))Cast();
   }
   TickHook(Time.deltaTime);
  }
  void FitCamera(){
   // Keep exactly 32 x 18 composition; portrait and ultrawide use safe letterboxing.
   float a=Screen.width/(float)Screen.height, ideal=16f/9;
   if(a>ideal){float w=ideal/a;worldCamera.rect=new Rect((1-w)/2,0,w,1);}
   else {float h=a/ideal;worldCamera.rect=new Rect(0,(1-h)/2,1,h);}
  }
  public void Move(float direction,float dt){
   if(RecoveryLocked)return;
   var p=player.position;p.x=Mathf.Clamp(p.x+direction*moveSpeed*dt,leftBound,rightBound);p.y=surfaceY;player.position=p;
   bool walking=Mathf.Abs(direction)>.01f;
   if(walking){playerSprite.flipX=direction<0;stepClock+=dt*Mathf.Abs(direction);if(stepClock>.42f){stepClock=0;InteractionFeel.Play(FeelCue.Step,.3f);}}
   playerVisual.localPosition=new Vector3(0,walking?Mathf.Abs(Mathf.Sin(Time.time*12))*.045f:0,0);
   playerVisual.localRotation=Quaternion.Euler(0,0,walking?Mathf.Sin(Time.time*12)*2:0);
   playerVisual.localScale=visualScale;
  }
  void FindNearest(){
   var previous=nearest;nearest=null;if(underground){nearest=mine;return;}
   foreach(var site in new[]{camp,mine,wreck,wormholeSite})if(site&&site.gameObject.activeInHierarchy&&Mathf.Abs(player.position.x-site.transform.position.x)<=site.radius)nearest=site;
   if(previous!=nearest){focusAge=Time.time;if(nearest)InteractionFeel.Play(FeelCue.Hover,.35f);}
  }
  public void Interact(){
   if(paused||RecoveryLocked||Transitioning||(forge&&forge.IsOpen))return;
   if(CampaignRuntime.Current&&CampaignRuntime.Current.SurfaceInteract())return;
   FindNearest();if(nearest==null){InteractionFeel.Play(FeelCue.Deny,.4f);return;}
   InteractionFeel.Play(FeelCue.Click,.5f);nearest.Invoke();
   if(nearest.kind==SiteKind.Mine){
    if(hookState!=HookState.Swinging){Say("等待回收");InteractionFeel.Play(FeelCue.Deny,.5f);return;}
    StartCoroutine(Travel(!underground));
   } else if(nearest.kind==SiteKind.Camp){if(forge)forge.OpenCamp();}
   else if(nearest.kind==SiteKind.Wormhole){if(wormhole)wormhole.TryEnter();}
   else Say("黑匣子信号 · 地下");
  }
  public IEnumerator Travel(bool down){
   if(RecoveryLocked||Transitioning)yield break;
   InteractionFeel.Play(FeelCue.Portal,.45f);Transitioning=true;float from=worldCamera.transform.position.y,to=down?undergroundCameraY:0;
   if(down){player.position=new Vector3(-1.35f,surfaceY,0);playerVisual.localRotation=Quaternion.identity;}
   float elapsed=0;
   while(elapsed<transitionSeconds){elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/transitionSeconds);t=t*t*t*(t*(t*6-15)+10);worldCamera.transform.position=new Vector3(0,Mathf.Lerp(from,to,t),-20);yield return null;}
   worldCamera.transform.position=new Vector3(0,to,-20);underground=down;Transitioning=false;
   toastUntil=0;InteractionFeel.Play(FeelCue.Click,.4f);
  }
  public bool Cast(){
   if((CampaignRuntime.Current&&CampaignRuntime.Current.Busy)||paused||Time.timeScale<=0||RecoveryLocked||Transitioning||hookState!=HookState.Swinging||(forge&&forge.IsOpen)){InteractionFeel.Play(FeelCue.Deny,.4f);return false;}
   if(!underground){InteractionFeel.Play(FeelCue.Deny,.4f);return false;}
   InteractionFeel.Play(FeelCue.Cast,.7f);targetAngle=angle;hookState=HookState.Extending;return true;
  }
  void TickHook(float dt){
   if(hookState==HookState.Swinging)angle=Mathf.Sin(Time.time*.86f)*swingDegrees;
   else angle=targetAngle;
   float oldLength=cableLength;
   if(hookState==HookState.Extending)cableLength=Mathf.Min(maxCableLength,cableLength+dt*castSpeed);
   if(hookState==HookState.Returning)cableLength=Mathf.Max(1.1f,cableLength-dt*retrieveSpeed*(cargo != null ? .72f : 1));
   Vector3 direction=new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad),-Mathf.Cos(angle*Mathf.Deg2Rad),0);
   Vector3 previous=winchAnchor.position+direction*oldLength;
   Vector3 end=winchAnchor.position+direction*cableLength;
   hook.position=end;hook.rotation=Quaternion.Euler(0,0,angle);
   cable.SetPosition(0,winchAnchor.position);cable.SetPosition(1,end);
   if(hookState==HookState.Extending){
    foreach(var ore in ores){if(ore.collected)continue;
     Vector3 seg=end-previous;float f=seg.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(ore.transform.position-previous,seg)/seg.sqrMagnitude);
     if(Vector3.Distance(previous+seg*f,ore.transform.position)<ore.radius){var node=ore.GetComponent<CampaignNode>();if(CampaignRuntime.Current&&node&&!CampaignRuntime.Current.State.bag.CanAdd(node.item,node.quantity)){Say("背包已满");hookState=HookState.Returning;break;}cargo=ore;var sample=ore.GetComponent<BuriedSample>();if(sample)sample.LiftFromRock();ore.collected=true;hookState=HookState.Returning;InteractionFeel.Play(FeelCue.Catch,.8f);break;}
    }
    if(cableLength>=maxCableLength)hookState=HookState.Returning;
   }
   if(cargo)cargo.transform.position=end+Vector3.down*.3f;
   if(hookState==HookState.Returning&&cableLength<=1.101f){
    if(cargo){var sample=cargo.GetComponent<BuriedSample>();var node=cargo.GetComponent<CampaignNode>();bool ok=CampaignRuntime.Current&&node?CampaignRuntime.Current.CollectNode(node):AwardRecoveredOre(sample?sample.Definition:null,cargo.GetInstanceID());if(ok){if(sample)sample.NotifyRecovered();cargo.Invoke();cargo.gameObject.SetActive(false);if(CampaignRuntime.Current){pickupQuantity=node?node.quantity:1;InteractionFeel.Play(FeelCue.Pickup,.75f);orePopupStarted=Time.time;orePopupUntil=Time.time+1.3f;counterDelayUntil=Time.time+.52f;}}else{cargo.collected=false;if(node)cargo.transform.position=node.origin;Say("背包已满，矿物已放回");}cargo=null;}
    hookState=HookState.Swinging;
   }
  }
  public bool AwardRecoveredOre(SubsurfaceDefinition source,int sourceId){
   if(source&&source.payloadKind==BuriedPayloadKind.DormantEncounter){
    if(!encounter||!encounter.TryBegin(source,sourceId))return false;
    orePopupUntil=-1;toastUntil=0;return true;
   }
   if(!inventory||!inventory.AddRecovered(source,sourceId))return false;
   InteractionFeel.Play(FeelCue.Pickup,.75f);orePopupStarted=Time.time;orePopupUntil=Time.time+1.3f;counterDelayUntil=Time.time+.52f;toastUntil=0;return true;
  }
  public void Say(string message){toast=message;toastUntil=Time.time+2.6f;}
  void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
  void Text(Rect r,string s,int size,Color c,TextAnchor align=TextAnchor.MiddleLeft){GUI.Label(r,s,new GUIStyle(GUI.skin.label){font=uiFont,fontSize=size,normal={textColor=c},alignment=align});}
  bool Button(Rect r,string s){return InteractionFeel.Button(r,s,uiFont,15,gold);}
  Vector2 ScreenPoint(Vector3 p){var v=worldCamera.WorldToViewportPoint(p);return new Vector2(v.x*1600,(1-v.y)*900);}
  void SiteLabel(Interactable site,string num,string subtitle){
   var p=ScreenPoint(new Vector3(site.transform.position.x,surfaceY-.8f,0));
   Text(new Rect(p.x-95,p.y,190,22),num+"  /  "+site.displayName,14,gold,TextAnchor.MiddleCenter);
   Text(new Rect(p.x-110,p.y+24,220,22),subtitle,10,muted,TextAnchor.MiddleCenter);
  }
  void OnGUI(){
   if(!worldCamera||(forge&&forge.IsOpen)||(CampaignRuntime.Current&&CampaignRuntime.Current.Modal))return;
   float w=Mathf.Min(Screen.width,Screen.height*16f/9),h=w*9/16;
   float ox=(Screen.width-w)/2,oy=(Screen.height-h)/2;
   // Black areas outside fixed cinematic aspect ratio.
   Box(new Rect(0,0,Screen.width,oy),Color.black);Box(new Rect(0,Screen.height-oy,Screen.width,oy),Color.black);
   Box(new Rect(0,0,ox,Screen.height),Color.black);Box(new Rect(Screen.width-ox,0,ox,Screen.height),Color.black);
   GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,Vector3.one*(w/1600));
   if(!photoMode){
    float intro=Mathf.Clamp01(1-(Time.time-startTime-2.5f)/1.5f);
    if(intro>0){if(!wormhole||!wormhole.Unlocked)Text(new Rect(48,43,370,42),"星陨边境",25,new Color(.86f,.91f,.92f,intro));Text(new Rect(590,850,420,26),"A / D  移动     E  交互",13,new Color(.7f,.8f,.82f,intro),TextAnchor.MiddleCenter);}
    Text(new Rect(1240,35,300,28),(CampaignRuntime.Current?"原矿  ":"未知矿石  ")+DisplayedOreCount,17,Time.time<counterFlashUntil?gold:cyan,TextAnchor.MiddleRight);
    if(inventory&&(inventory.MetalTotal>0||inventory.FuelCount>0))Text(new Rect(1140,65,400,23),"金属 "+inventory.MetalTotal+"   燃料 "+inventory.FuelCount,12,muted,TextAnchor.MiddleRight);
    if(wormhole&&wormhole.Unlocked){bool before=GUI.enabled;GUI.enabled=wormhole.CanEnter;if(Button(new Rect(42,35,170,35),CampaignRuntime.Current?"星界坐标  ↗":"断崖盆地  ↗"))wormhole.TryEnter();GUI.enabled=before;}
    if(!Transitioning&&!RecoveryLocked){
     if(underground){
      if(hookState==HookState.Swinging){if(Button(new Rect(660,839,280,38),"空格  下放"))Cast();}
      else Text(new Rect(660,839,280,38),hookState==HookState.Extending?"↓":"↑",22,gold,TextAnchor.MiddleCenter);
      if(Button(new Rect(1380,839,170,38),CampaignRuntime.Current?"返回地表":"E  返回")){if(CampaignRuntime.Current)StartCoroutine(Travel(false));else Interact();}
      var tip=ScreenPoint(hook.position);InteractionFeel.Ring(tip,14,hookState==HookState.Swinging?1:cableLength/maxCableLength,new Color(.6f,.83f,.83f,.45f),1);
     }else if(nearest){
      var p=ScreenPoint(player.position+Vector3.up*1.72f);float fade=Mathf.Clamp01((Time.time-focusAge)*7);Text(new Rect(p.x-135,p.y-35,270,34),"E  "+nearest.displayName,15,new Color(gold.r,gold.g,gold.b,fade),TextAnchor.MiddleCenter);
      var ground=ScreenPoint(new Vector3(nearest.transform.position.x,surfaceY+.05f,0));InteractionFeel.Ring(ground,16,1,new Color(cyan.r,cyan.g,cyan.b,.28f*fade),1);
     }
    }
    if(Time.time<toastUntil)Text(new Rect(455,764,690,34),toast,16,new Color(.85f,.92f,.92f,Mathf.Clamp01(toastUntil-Time.time)),TextAnchor.MiddleCenter);
   }
   if(PickupPopupVisible){float age=Time.time-orePopupStarted,alpha=Mathf.Clamp01((orePopupUntil-Time.time)*2.8f);var p=ScreenPoint(player.position+Vector3.up*(1.8f+age*.33f));var r=new Rect(p.x-76,p.y-19,152,38);Text(new Rect(r.x+1,r.y+2,r.width,r.height),"矿石+"+pickupQuantity,21,new Color(.03f,.06f,.08f,alpha),TextAnchor.MiddleCenter);Text(r,"矿石+"+pickupQuantity,21,new Color(1,.89f,.51f,alpha),TextAnchor.MiddleCenter);}
   if(paused){
    Box(new Rect(0,0,1600,900),new Color(.008f,.02f,.03f,.58f));Text(new Rect(600,260,400,65),"暂停",30,gold,TextAnchor.MiddleCenter);
    if(Button(new Rect(650,371,300,48),"继续"))SetPaused(false);
    if(Button(new Rect(650,433,300,42),InteractionFeel.Muted?"声音 · 关闭":"声音 · 开启"))InteractionFeel.Muted=!InteractionFeel.Muted;
    if(Button(new Rect(650,487,300,42),InteractionFeel.ReducedMotion?"镜头反馈 · 减弱":"镜头反馈 · 标准"))InteractionFeel.ReducedMotion=!InteractionFeel.ReducedMotion;
    if(Button(new Rect(650,554,300,42),"退出游戏"))Application.Quit();
    Text(new Rect(360,694,880,30),"A / D  移动    E  交互    空格  抓钩    TAB  隐藏界面    F11  全屏",13,muted,TextAnchor.MiddleCenter);
   }
   GUI.matrix=Matrix4x4.identity;
  }
  IEnumerator OpenUndergroundPreview(){yield return new WaitForSeconds(.5f);yield return Travel(true);yield return CapturePreview();}
  IEnumerator CapturePreview(){yield return new WaitForSeconds(4);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/03-interface.png")),t.EncodeToPNG());Destroy(t);}}
  IEnumerator Capture(string name){
   yield return new WaitForEndOfFrame();
   // Explicit camera rendering also works while the QA player is hidden.
   var target=RenderTexture.GetTemporary(1600,900,24);var oldTarget=worldCamera.targetTexture;var oldRect=worldCamera.rect;var oldActive=RenderTexture.active;
   worldCamera.rect=new Rect(0,0,1,1);worldCamera.targetTexture=target;worldCamera.Render();RenderTexture.active=target;
   var capture=new Texture2D(1600,900,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1600,900),0,0);capture.Apply();
   File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/"+name+".png")),capture.EncodeToPNG());Destroy(capture);
   worldCamera.targetTexture=oldTarget;worldCamera.rect=oldRect;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(target);
   yield return new WaitForSeconds(.3f);
  }
  IEnumerator RunQA(){
   string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA"));Directory.CreateDirectory(dir);
   yield return new WaitForSeconds(3);
   yield return Capture("01-surface");
   var start=player.position;Move(-1,99);bool left=player.position.x==leftBound&&player.position.y==surfaceY;
   Move(1,99);bool right=player.position.x==rightBound&&player.position.y==surfaceY;
   player.position=new Vector3(0,surfaceY,0);Interact();yield return new WaitForSeconds(transitionSeconds+.3f);
   bool cameraDown=underground&&Mathf.Abs(worldCamera.transform.position.y-undergroundCameraY)<.01f;
   var bounds=mine.GetComponentInChildren<SpriteRenderer>().bounds;
   bool mineVisible=worldCamera.WorldToViewportPoint(bounds.max).y<.91f&&worldCamera.WorldToViewportPoint(bounds.min).y>0;
   yield return Capture("02-underground");
   // Force a deterministic angle but use the same cast, travel, hit and recovery path as gameplay.
   angle=0;targetAngle=0;var testOre=ores[0];testOre.transform.position=winchAnchor.position+Vector3.down*5;
   bool cast=Cast();float deadline=Time.time+12;
   while(oreCount==0&&Time.time<deadline)yield return null;
   bool mining=oreCount==1&&!testOre.gameObject.activeSelf&&hookState==HookState.Swinging;
   bool repeatBlocked;angle=0;Cast();repeatBlocked=!Cast();
   deadline=Time.time+12;while(hookState!=HookState.Swinging&&Time.time<deadline)yield return null;
   Interact();yield return new WaitForSeconds(transitionSeconds+.2f);
   bool returned=!underground&&Mathf.Abs(worldCamera.transform.position.y)<.01f&&oreCount>=1;
   player.position=start;
   bool pass=left&&right&&cameraDown&&mineVisible&&cast&&mining&&repeatBlocked&&returned;
   File.WriteAllText(Path.Combine(dir,"runtime-report.json"),"{\n  \"passed\": "+pass.ToString().ToLower()+",\n  \"leftBoundAndFixedY\": "+left.ToString().ToLower()+",\n  \"rightBoundAndFixedY\": "+right.ToString().ToLower()+",\n  \"cameraDown\": "+cameraDown.ToString().ToLower()+",\n  \"mineFullyVisible\": "+mineVisible.ToString().ToLower()+",\n  \"captureAndRetrieve\": "+mining.ToString().ToLower()+",\n  \"repeatedCastBlocked\": "+repeatBlocked.ToString().ToLower()+",\n  \"returnPreservesInventory\": "+returned.ToString().ToLower()+"\n}");
   Debug.Log(pass?"STARFALL_QA_PASS":"STARFALL_QA_FAIL");Application.Quit(pass?0:2);
  }
 }
}
