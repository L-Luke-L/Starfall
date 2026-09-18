using UnityEngine;

namespace Starfall {
 public sealed partial class ForgeController {
  static readonly Color ink=new Color(.035f,.065f,.08f),light=new Color(.92f,.95f,.92f),amber=new Color(1,.65f,.22f),muted=new Color(.51f,.64f,.65f);
  RenderTexture workshopFrame,blurA,blurB;
  Material blurMaterial;
  Texture2D contactShadow,edgeShade,rewardGlow;
  static readonly Rect heroRect=new Rect(535,95,480,650),anvilRect=new Rect(835,450,300,300);
  static readonly Vector2 contact=new Vector2(948,487);
  void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
  void Label(Rect r,string text,int size,Color color,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft){GUI.Label(r,text,new GUIStyle(GUI.skin.label){font=director.uiFont,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,normal={textColor=color},alignment=anchor,wordWrap=false});}
  bool Action(Rect r,string text,bool enabled=true){return InteractionFeel.Button(r,text,director.uiFont,18,amber,enabled);}
  void Art(Rect r,Texture texture,float opacity=1,ScaleMode mode=ScaleMode.ScaleToFit){if(!texture)return;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(r,texture,mode,true);GUI.color=Color.white;}
  void Stroke(Vector2 a,Vector2 b,float width,Color color){var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width*.5f,Vector2.Distance(a,b),width),color);GUI.matrix=old;}
  Texture2D Pose(){return State==ForgeState.Striking?(StrikeAge<.12f?heroReady:StrikeAge<.25f?heroLift:StrikeAge<.74f?heroStrike:heroReady):heroReady;}
  int PoseIndex=>State==ForgeState.Striking?(StrikeAge<.12f?0:StrikeAge<.27f?1:StrikeAge<.74f?2:0):0;
  static Rect Fit(Rect r,Texture t){float s=Mathf.Min(r.width/t.width,r.height/t.height);return new Rect(r.center.x-t.width*s*.5f,r.center.y-t.height*s*.5f,t.width*s,t.height*s);}
  void Layer(Rect r,Texture t){if(t)Graphics.DrawTexture(Fit(r,t),t);}
  void LateUpdate(){
   if(!IsOpen||State==ForgeState.CampMenu||!workshop)return;
   if(!workshopFrame){
    workshopFrame=new RenderTexture(1600,900,0,RenderTextureFormat.ARGB32);workshopFrame.Create();
    blurA=new RenderTexture(400,225,0);blurA.Create();blurB=new RenderTexture(400,225,0);blurB.Create();
    foreach(var rt in new[]{workshopFrame,blurA,blurB}){rt.filterMode=FilterMode.Bilinear;rt.wrapMode=TextureWrapMode.Clamp;}
    edgeShade=new Texture2D(1,128,TextureFormat.RGBA32,false);var shade=new Color[128];for(int y=0;y<128;y++)shade[y]=new Color(0,.015f,.025f,Mathf.Pow(y/127f,1.6f)*.7f);edgeShade.SetPixels(shade);edgeShade.Apply();edgeShade.wrapMode=TextureWrapMode.Clamp;
    if(workshopBlurShader)blurMaterial=new Material(workshopBlurShader);
    contactShadow=new Texture2D(128,32,TextureFormat.RGBA32,false);var px=new Color[128*32];for(int y=0;y<32;y++)for(int x=0;x<128;x++){float d=new Vector2((x-63.5f)/64,(y-15.5f)/16).magnitude;px[y*128+x]=new Color(.015f,.025f,.025f,Mathf.Pow(Mathf.Clamp01(1-d),1.3f)*.78f);}contactShadow.SetPixels(px);contactShadow.Apply();
   }
   var prior=RenderTexture.active;RenderTexture.active=workshopFrame;GL.Clear(true,true,ink);GL.PushMatrix();GL.LoadPixelMatrix(0,1600,900,0);
   Graphics.DrawTexture(new Rect(0,0,1600,900),workshop);
   Graphics.DrawTexture(new Rect(596,715,389,43),contactShadow);Graphics.DrawTexture(new Rect(856,712,270,45),contactShadow);
   if(smithPoses){float width=heroRect.height*(smithPoses.width/3f)/smithPoses.height;var r=new Rect(heroRect.center.x-width/2,heroRect.y,width,heroRect.height);Graphics.DrawTexture(r,smithPoses,new Rect(PoseIndex/3f,0,1/3f,1),0,0,0,0);}else Layer(heroRect,Pose());
   Layer(anvilRect,anvil);
   if(State!=ForgeState.Success)Layer(new Rect(contact.x-23,contact.y-23,46,35),oreArt);
   GL.PopMatrix();RenderTexture.active=prior;
   if(FailureBlur>0&&blurMaterial){Graphics.Blit(workshopFrame,blurA);for(int i=0;i<3;i++){blurMaterial.SetVector("_Direction",new Vector4(FailureBlur*1.35f,0,0,0));Graphics.Blit(blurA,blurB,blurMaterial);blurMaterial.SetVector("_Direction",new Vector4(0,FailureBlur*1.35f,0,0));Graphics.Blit(blurB,blurA,blurMaterial);}}
  }
  void ReleaseWorkshop(){foreach(var rt in new[]{workshopFrame,blurA,blurB})if(rt){rt.Release();Destroy(rt);}if(blurMaterial)Destroy(blurMaterial);if(contactShadow)Destroy(contactShadow);if(edgeShade)Destroy(edgeShade);if(rewardGlow)Destroy(rewardGlow);}
  void OnGUI(){
   if(!IsOpen||!director.uiFont)return;
   GUI.depth=-100;var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
   Fill(new Rect(0,0,Screen.width,Screen.height),State==ForgeState.CampMenu?new Color(.01f,.02f,.03f,.18f):ink);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
   if(State==ForgeState.CampMenu){DrawCamp();GUI.matrix=old;return;}
   float impact=State==ForgeState.Striking?Mathf.Clamp01(1-Mathf.Abs(StrikeAge-.31f)*7):0;
   var stageMatrix=GUI.matrix;GUI.matrix*=Matrix4x4.Translate(new Vector3(Mathf.Sin(StrikeAge*124)*impact*(InteractionFeel.ReducedMotion?0:3),Mathf.Sin(StrikeAge*147)*impact*(InteractionFeel.ReducedMotion?0:2),0));
   Art(new Rect(0,0,1600,900),FailureBlur>0&&blurMaterial?blurA:workshopFrame,1,ScaleMode.StretchToFill);
   DrawSparks();GUI.matrix=stageMatrix;
   if(edgeShade){GUI.DrawTexture(new Rect(0,0,1600,132),edgeShade);GUI.DrawTextureWithTexCoords(new Rect(0,746,1600,154),edgeShade,new Rect(0,1,1,-1));}
   if(State==ForgeState.Failure){Fill(new Rect(0,0,1600,900),new Color(.015f,.025f,.04f,FailureBlur*.28f));if(ResultAge<1.7f)Art(new Rect(FailurePortraitX,-8,920,920),failureCG);}
   if(State==ForgeState.Success)DrawReward();
   if(!FailurePresentationActive&&!RewardPresentationActive){

    Label(new Rect(1090,29,270,39),"矿石  "+inventory.UnknownCount,17,amber,true,TextAnchor.MiddleRight);
    if(Action(new Rect(1400,30,154,40),"Esc")){if(IsWorking)TogglePause();else TryClose();}
    DrawControls();
   }
   if(Paused){Fill(new Rect(0,0,1600,900),new Color(.015f,.03f,.04f,.55f));Label(new Rect(600,338,400,70),"暂停",30,light,true,TextAnchor.MiddleCenter);if(Action(new Rect(640,455,320,60),"继续"))TogglePause();}
   GUI.matrix=old;
  }
  void DrawCamp(){
   var anchor=director.worldCamera.WorldToViewportPoint(director.camp.transform.position+Vector3.up*2.4f);float x=Mathf.Clamp(anchor.x*1600,210,1370),y=Mathf.Clamp((1-anchor.y)*900-155,180,465);
   Label(new Rect(x-160,y-67,320,40),"营地",25,light,true,TextAnchor.MiddleCenter);
   Label(new Rect(x-160,y-28,320,30),"矿石  "+inventory.UnknownCount,15,amber,false,TextAnchor.MiddleCenter);
   if(Action(new Rect(x-150,y+24,300,52),"锻造"))OpenWorkshop();
   if(Action(new Rect(x-150,y+85,300,42),"离开"))TryClose();
  }
  void DrawSparks(){
   if(State!=ForgeState.Striking||StrikeAge<.28f||StrikeAge>.79f)return;
   float t=StrikeAge-.28f,fade=Mathf.Clamp01(1-t/.51f);
   for(int i=0;i<48;i++){float a=-Mathf.PI+(i*.618034f%1)*Mathf.PI,s=190+(i*47%420);Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*s;Vector2 p=contact+v*t+Vector2.up*(380*t*t);Stroke(p,p-v.normalized*(8+i%15)*fade,1.4f+i%3,new Color(1,.6f+(i%3)*.13f,.15f,fade));}
   if(t<.1f)for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Stroke(contact,contact+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*35,3,new Color(1,.94f,.66f,1-t/.1f));}
  }
  void DrawControls(){
   bool active=State==ForgeState.Timing||State==ForgeState.Striking;
   if(active){
    for(int i=0;i<3;i++)Fill(new Rect(757+i*32,743,20,6),i<Hits?amber:new Color(.3f,.42f,.44f));
    var bar=new Rect(510,790,580,9);Fill(bar,new Color(.03f,.07f,.08f,.6f));

    if(Rhythm==ForgeRhythmKind.Drift)Fill(new Rect(bar.x+(TargetHome-ForgeRhythm.DriftAmplitude)*bar.width,bar.y-8,ForgeRhythm.DriftAmplitude*2*bar.width,2),new Color(1,.65f,.22f,.32f));
    Fill(new Rect(bar.x+(TargetCenter-TargetWidth*.5f)*bar.width,bar.y,TargetWidth*bar.width,bar.height),amber);
    if(State==ForgeState.Timing&&WarmupRemaining<=0)Fill(new Rect(bar.x+PreviousPointer*bar.width-2,bar.y-4,4,bar.height+8),new Color(.92f,.95f,.92f,.24f));
    Fill(new Rect(bar.x+Pointer*bar.width-2,bar.y-7,4,bar.height+14),light);
    Fill(new Rect(bar.x,bar.y+28,bar.width*RoundTimeFraction,2),new Color(1,.65f,.22f,.65f));
    if(Action(new Rect(690,839,220,41),State==ForgeState.Striking?"·":WarmupRemaining>0?"· · ·":"空格",StrikeReady))SubmitStrike();
   }else{
    if(State==ForgeState.Failure)Label(new Rect(550,724,500,53),"矿石 −1",24,light,true,TextAnchor.MiddleCenter);
    if(Action(new Rect(650,811,300,58),inventory.UnknownCount>0?(State==ForgeState.Ready?"空格  开始":"空格  继续"):"暂无矿石",inventory.UnknownCount>0))BeginForge();
   }
  }
 }
}
