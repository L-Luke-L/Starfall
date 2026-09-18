using UnityEngine;

namespace Starfall {
 [RequireComponent(typeof(CharacterController))]
 public sealed class FirstPersonExplorer:MonoBehaviour {
  public Camera viewCamera;
  public Transform returnBeacon;
  public Font uiFont;
  public BattleTravel session;
  public CreatureArena arena;
  public PlayerArsenal arsenal;public BasinExpedition expedition;
  public float walkSpeed=4.6f,runSpeed=7.2f,mouseSensitivity=2f,jumpHeight=1.1f;
  public bool Paused {get;private set;}
  public bool Ready {get;set;}
  public bool Automated {get;set;}
  public bool IsGrounded=>motor&&motor.isGrounded;
  public float Pitch {get;private set;}
  public CharacterController Motor=>motor;
  CharacterController motor;Vector3 spawn;float vertical,bob,tipUntil,steps,impact,baseFov;Vector2 smoothInput;
  public void ImpactPulse(float strength){impact=Mathf.Max(impact,strength);}
  void Awake(){motor=GetComponent<CharacterController>();spawn=transform.position;baseFov=viewCamera.fieldOfView;if(!uiFont)uiFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);}
  void Start(){tipUntil=Time.unscaledTime+4;if(session==null)Ready=true;}
  public void Begin(BattleTravel owner){session=owner;Ready=true;Paused=false;tipUntil=Time.unscaledTime+4;if(!Automated)CaptureMouse();}
  void CaptureMouse(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Input.ResetInputAxes();}
  public void SetPaused(bool value){InteractionFeel.Play(FeelCue.Click,.5f);Paused=value;if(arena)arena.SetPaused(value);Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;if(!value)Input.ResetInputAxes();}
  void OnApplicationFocus(bool focused){if(!focused&&Ready&&!Automated)SetPaused(true);}
  void Update(){
   if(!Ready||Automated)return;
   if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
   if(Input.GetKeyDown(KeyCode.Escape))SetPaused(!Paused);
   if(Paused||(arena&&arena.Downed))return;
   if(Cursor.lockState!=CursorLockMode.Locked){if(Input.GetMouseButtonDown(0))CaptureMouse();return;}
   Look(Input.GetAxisRaw("Mouse X")*mouseSensitivity,Input.GetAxisRaw("Mouse Y")*mouseSensitivity);
   var move=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
   Drive(move,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),Input.GetKeyDown(KeyCode.Space),Time.deltaTime);
   if(Input.GetKeyDown(KeyCode.E)&&returnBeacon&&Vector3.Distance(transform.position,returnBeacon.position)<4)Return();
  }
  public void Look(float yaw,float pitch){if(Paused)return;transform.Rotate(0,yaw,0);Pitch=Mathf.Clamp(Pitch-pitch,-85,85);viewCamera.transform.localRotation=Quaternion.Euler(Pitch,0,0);}
  public void Drive(Vector2 input,bool running,bool jump,float dt){
   if(!Ready||Paused||(arena&&arena.Downed)||dt<=0)return;input=Vector2.ClampMagnitude(input,1);
   bool grounded=motor.isGrounded;if(grounded&&vertical<0)vertical=-2;
   running=running&&input.sqrMagnitude>.01f&&(!arsenal||arsenal.Sprint(dt));
   if(grounded&&jump&&(!arsenal||arsenal.SpendStamina(14))){InteractionFeel.Play(FeelCue.Step,.55f);vertical=Mathf.Sqrt(2*18*jumpHeight);}vertical-=18*dt;
   smoothInput=Vector2.MoveTowards(smoothInput,input,dt*(input.sqrMagnitude<.01f?20:14));var before=transform.position;
   Vector3 direction=transform.right*smoothInput.x+transform.forward*smoothInput.y;motor.Move((direction*(running?runSpeed:walkSpeed)+Vector3.up*vertical)*dt);
   float moved=CreatureArena.FlatDistance(before,transform.position);steps+=moved;if(motor.isGrounded&&steps>(running?1.9f:1.5f)){steps=0;InteractionFeel.Play(FeelCue.Step,running?.65f:.38f);}
   if(!grounded&&motor.isGrounded&&vertical<-5){ImpactPulse(.3f);InteractionFeel.Play(FeelCue.Step,.8f);}
   if(transform.position.y<-25)Respawn();
   float walking=grounded?input.magnitude:0;bob+=dt*(running?11:8)*walking;float offset=Mathf.Sin(bob)*.018f*walking;
   viewCamera.transform.localPosition=new Vector3(0,1.64f+(InteractionFeel.ReducedMotion?0:offset),0);
  }
  void LateUpdate(){if(!Ready||Paused)return;impact=Mathf.MoveTowards(impact,0,Time.deltaTime*4);float movement=InteractionFeel.ReducedMotion?0:Mathf.Clamp01(motor.velocity.magnitude/runSpeed)*1.4f+impact*.6f;viewCamera.fieldOfView=Mathf.Lerp(viewCamera.fieldOfView,baseFov+movement,1-Mathf.Exp(-Time.deltaTime*12));}
  public void ResetMotion(){smoothInput=Vector2.zero;vertical=0;steps=0;}
  public void Respawn(){ResetMotion();motor.enabled=false;transform.position=spawn;vertical=0;motor.enabled=true;}
  public void Return(){if(session)session.ReturnToExpedition();}
  void Label(Rect r,string s,int size,Color c,TextAnchor align=TextAnchor.MiddleLeft){GUI.Label(r,s,new GUIStyle(GUI.skin.label){font=uiFont,fontSize=size,normal={textColor=c},alignment=align});}
  void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
  bool Button(Rect r,string text){return InteractionFeel.Button(r,text,uiFont,20,new Color(.87f,.75f,.52f));}
  void OnGUI(){
   if(!Ready)return;var prior=GUI.matrix;GUI.depth=-20;float s=Mathf.Min(Screen.width/1600f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*s)/2,(Screen.height-900*s)/2,0),Quaternion.identity,new Vector3(s,s,1));
   float intro=Mathf.Clamp01(tipUntil-Time.unscaledTime);if(intro>0)Label(new Rect(43,32,350,40),"断崖盆地",23,new Color(.86f,.9f,.9f,intro));
   if(!Paused){Fill(new Rect(798,448,4,4),new Color(.8f,.9f,.92f,.72f));
    if(Time.unscaledTime<tipUntil)Label(new Rect(390,847,820,28),"Esc  操作与暂停",13,new Color(.81f,.86f,.86f,intro),TextAnchor.MiddleCenter);
    if(returnBeacon&&Vector3.Distance(transform.position,returnBeacon.position)<4)Label(new Rect(590,757,420,38),"E  返回",16,new Color(.93f,.77f,.49f),TextAnchor.MiddleCenter);
   }else{
    Fill(new Rect(0,0,1600,900),new Color(.012f,.025f,.035f,.60f));Label(new Rect(560,220,480,55),"暂停",30,Color.white,TextAnchor.MiddleCenter);
    if(Button(new Rect(630,330,340,50),"继续"))SetPaused(false);
    if(session&&Button(new Rect(630,397,340,50),"返回矿井"))Return();
    if(Button(new Rect(630,464,340,44),InteractionFeel.Muted?"声音 · 关闭":"声音 · 开启"))InteractionFeel.Muted=!InteractionFeel.Muted;
    if(Button(new Rect(630,525,340,44),InteractionFeel.ReducedMotion?"镜头反馈 · 减弱":"镜头反馈 · 标准"))InteractionFeel.ReducedMotion=!InteractionFeel.ReducedMotion;
    Label(new Rect(330,660,940,35),"WASD 移动   ·   Shift 奔跑   ·   空格 跳跃",14,new Color(.65f,.76f,.78f),TextAnchor.MiddleCenter);
    Label(new Rect(260,700,1080,35),"左键 射击   ·   右键 近战   ·   R 换弹   ·   E 拾取 / 长按采集",14,new Color(.65f,.76f,.78f),TextAnchor.MiddleCenter);
   }
   GUI.matrix=prior;
  }
 }
}
