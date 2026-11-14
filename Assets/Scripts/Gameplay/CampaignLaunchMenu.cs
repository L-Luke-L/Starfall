using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  public bool LaunchMenuActive { get; private set; }
  internal bool LaunchHasStory => launchHasStory;
  bool launchHasStory, initialWorldPending, launchWorking, showcaseFromLaunch;
  string launchPage="", launchMessage="";
  int launchSelection;

  void InitializeLaunchMenu() {
   launchHasStory=!saveBlocked&&(File.Exists(Store.PathFor("campaign"))||File.Exists(Store.PathFor("campaign")+".bak"));
   LaunchMenuActive=(!IsQA||Array.IndexOf(Environment.GetCommandLineArgs(),"-launchQA")>=0)&&Array.IndexOf(Environment.GetCommandLineArgs(),"-guardianDemo")<0;
   initialWorldPending=LaunchMenuActive;
   if(!IsQA){InteractionFeel.Muted=PlayerPrefs.GetInt("Starfall.Muted",0)!=0;InteractionFeel.ReducedMotion=PlayerPrefs.GetInt("Starfall.ReducedMotion",0)!=0;}
  }
  internal void ShowLaunchMenu() {
   Close();LaunchMenuActive=true;launchPage="";launchSelection=0;launchWorking=false;
   director.SetPaused(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   if(saveBlocked)launchMessage="存档无法读取，原文件已保留。可重新开始或观看演示。";
  }
  internal void LaunchContinue() {
   if(launchWorking||saveBlocked||!launchHasStory)return;
   launchWorking=true;LaunchMenuActive=false;director.SetPaused(false);consumedFrame=Time.frameCount;
   StartCoroutine(ContinueLaunchStory());
  }
  IEnumerator ContinueLaunchStory() {
   initialWorldPending=false;yield return ResumeInitialWorld();launchWorking=false;
  }
  internal void LaunchNewStory() {if(!launchWorking){launchWorking=true;StartCoroutine(CreateLaunchStory());}}
  IEnumerator CreateLaunchStory() {
   // Save the loaded state directly: the world has not been restored behind the title screen.
   if(launchHasStory&&!saveBlocked){
    try{Store.Save(State,"previous-story");}catch(Exception ex){launchMessage="备份失败，未重新开始："+ex.Message;launchWorking=false;yield break;}
   }
   if(saveBlocked){
    try{foreach(var path in new[]{Store.PathFor("campaign"),Store.PathFor("campaign")+".bak"})if(File.Exists(path))File.Copy(path,path+".corrupt-"+DateTime.UtcNow.Ticks);}
    catch(Exception ex){launchMessage="原存档保留失败，未重新开始："+ex.Message;launchWorking=false;yield break;}
   }
   yield return NewGame();
   initialWorldPending=false;LaunchMenuActive=false;launchWorking=false;launchHasStory=true;
   director.SetPaused(false);consumedFrame=Time.frameCount;Save();
  }
  internal bool LaunchDemo(bool guardian=false) {
   if(launchWorking)return false;
   // Keep the menu active until StartShowcase has skipped saving the unopened story.
   showcaseFromLaunch=true;
   if(!(guardian?StartGuardianShowcase():StartShowcase())){showcaseFromLaunch=false;launchMessage="场景正在准备，请稍后再试。";return false;}
   LaunchMenuActive=false;return true;
  }
  void TickLaunchMenu() {
   if(Input.GetKeyDown(KeyCode.Escape)&&!launchWorking){launchPage="";launchSelection=0;}
   Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  }
  void LaunchPage(string page){launchPage=page;launchSelection=0;launchMessage="";}
  void DrawLaunchMenu() {
   var oldMatrix=GUI.matrix;int oldDepth=GUI.depth;bool oldEnabled=GUI.enabled;
   GUI.depth=-800;GUI.matrix=Matrix4x4.identity;
   CampaignGameUI.Solid(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.032f,.047f));
   float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
   GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
   DrawLaunchSky();DrawPixelLogo();
   Caption(new Rect(400,316,800,34),"星 陨 边 境   /   深 空 探 索 终 端",18,new Color(.56f,.63f,.69f),TextAnchor.MiddleCenter);
   string[] labels=launchPage=="settings"?new[]{InteractionFeel.Muted?"声音  ·  关闭":"声音  ·  开启",InteractionFeel.ReducedMotion?"镜头反馈  ·  减弱":"镜头反馈  ·  标准",StoryCinematicsEnabled?"剧情演出  ·  开启":"剧情演出  ·  关闭",Screen.fullScreen?"显示  ·  全屏":"显示  ·  窗口","返回"}:
    launchPage=="restart"?new[]{"确认重新开始","返回"}:
    launchPage=="demo"?new[]{"主线与界面演示","直看机甲战","返回"}:
    launchHasStory?new[]{"重新开始","继续游戏","演示模式","设置"}:new[]{"开始","演示模式","设置"};
   bool activate=false;var evt=Event.current;
   if(!launchWorking&&evt.type==EventType.KeyDown){
    if(evt.keyCode==KeyCode.UpArrow||evt.keyCode==KeyCode.DownArrow||evt.keyCode==KeyCode.Tab){int step=evt.keyCode==KeyCode.UpArrow||(evt.keyCode==KeyCode.Tab&&evt.shift)?-1:1;launchSelection=(launchSelection+step+labels.Length)%labels.Length;evt.Use();}
    else if(evt.keyCode==KeyCode.Return||evt.keyCode==KeyCode.KeypadEnter||evt.keyCode==KeyCode.Space){activate=true;evt.Use();}
   }
   GUI.enabled=!launchWorking;
   for(int i=0;i<labels.Length;i++){
    var r=new Rect(550,405+i*70,500,56);
    if(evt.type==EventType.MouseMove&&r.Contains(evt.mousePosition))launchSelection=i;
    if(MetalMenuButton(r,labels[i],i==launchSelection)||(activate&&i==launchSelection)){HandleLaunchAction(i);break;}
   }
   string help=launchPage=="restart"?"当前故事将备份到 previous-story，再从迫降开始。":launchPage=="demo"?"独立临时进度 · 包含背包、仓库、任务与战斗 · Esc 暂停 / 退出":launchHasStory?"探索记录已就绪 · 继续上一次旅程":"一颗陌生星球，一段尚未完成的归途。";
   Caption(new Rect(240,774,1120,32),launchWorking?"正在准备旅程…":launchMessage!=""?launchMessage:help,17,new Color(.65f,.70f,.74f),TextAnchor.MiddleCenter);
   Caption(new Rect(240,842,1120,26),"↑ ↓  选择     ENTER  确认     F11  全屏",14,new Color(.39f,.45f,.51f),TextAnchor.MiddleCenter);
   GUI.matrix=oldMatrix;GUI.depth=oldDepth;GUI.enabled=oldEnabled;
  }
  void HandleLaunchAction(int i) {
   InteractionFeel.Play(FeelCue.Click,.6f);
   if(launchPage=="settings"){
    if(i==0){InteractionFeel.Muted=!InteractionFeel.Muted;PlayerPrefs.SetInt("Starfall.Muted",InteractionFeel.Muted?1:0);}
    else if(i==1){InteractionFeel.ReducedMotion=!InteractionFeel.ReducedMotion;PlayerPrefs.SetInt("Starfall.ReducedMotion",InteractionFeel.ReducedMotion?1:0);}
    else if(i==2)StoryCinematicsEnabled=!StoryCinematicsEnabled;
    else if(i==3)Screen.fullScreen=!Screen.fullScreen;else LaunchPage("");
    PlayerPrefs.Save();return;
   }
   if(launchPage=="restart"){if(i==0)LaunchNewStory();else LaunchPage("");return;}
   if(launchPage=="demo"){if(i<2)LaunchDemo(i==1);else LaunchPage("");return;}
   if(i==0){if(launchHasStory)LaunchPage("restart");else LaunchNewStory();}
   else if(launchHasStory&&i==1)LaunchContinue();
   else if(i==(launchHasStory?2:1))LaunchPage("demo");else LaunchPage("settings");
  }
  bool MetalMenuButton(Rect r,string text,bool focused) {
   bool hover=r.Contains(Event.current.mousePosition);bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);
   CampaignGameUI.Solid(new Rect(r.x+6,r.y+7,r.width,r.height),new Color(0,0,0,.5f));
   CampaignGameUI.Solid(r,new Color(.12f,.14f,.16f));
   CampaignGameUI.Solid(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),hover||focused?new Color(.42f,.45f,.48f):new Color(.29f,.32f,.35f));
   CampaignGameUI.Solid(new Rect(r.x+4,r.y+4,r.width-8,4),new Color(.62f,.65f,.67f));
   CampaignGameUI.Solid(new Rect(r.x+4,r.yMax-8,r.width-8,4),new Color(.17f,.19f,.21f));
   for(int y=12;y<r.height-9;y+=6)CampaignGameUI.Solid(new Rect(r.x+9,r.y+y,r.width-18,1),new Color(0,0,0,.075f));
   foreach(float x in new[]{r.x+16,r.xMax-22})foreach(float y in new[]{r.y+13,r.yMax-19}){
    CampaignGameUI.Solid(new Rect(x,y,6,6),new Color(.13f,.15f,.17f));CampaignGameUI.Solid(new Rect(x,y,4,2),new Color(.67f,.69f,.70f));
   }
   if(focused){CampaignGameUI.Solid(new Rect(r.x-18,r.y+22,8,12),new Color(.78f,.84f,.85f));CampaignGameUI.Outline(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),new Color(.65f,.70f,.73f),2);}
   Caption(new Rect(r.x+1,r.y+2,r.width,r.height),text,23,new Color(.09f,.11f,.13f),TextAnchor.MiddleCenter);
   Caption(r,text,23,new Color(.94f,.95f,.96f),TextAnchor.MiddleCenter);return hit;
  }
  void DrawLaunchSky() {
   for(int i=0;i<95;i++){float x=(i*317+29)%1600,y=(i*137+43)%710;float a=.18f+(i%5)*.08f;CampaignGameUI.Solid(new Rect(x,y,i%7==0?4:2,i%7==0?4:2),new Color(.64f,.73f,.83f,a));}
   // Stepped horizon and meteor are drawn as hard-edged pixels, without imported art.
   for(int i=0;i<40;i++){float h=38+(i*43%85);CampaignGameUI.Solid(new Rect(i*40,900-h,40,h),new Color(.065f,.08f,.10f));}
   for(int i=0;i<12;i++)CampaignGameUI.Solid(new Rect(1130+i*12,154-i*8,16,4),new Color(.6f,.71f,.80f,(12-i)/20f));
   CampaignGameUI.Solid(new Rect(1130,154,8,8),new Color(.8f,.89f,.95f));
   Caption(new Rect(52,38,500,25),"S F   /   DEEP SPACE EXPEDITION",14,new Color(.43f,.50f,.57f));
   Caption(new Rect(1100,38,448,25),"SIGNAL  :  ACTIVE   ▪",14,new Color(.43f,.50f,.57f),TextAnchor.MiddleRight);
  }
  void DrawPixelLogo() {
   string[] glyphs={"01111100001000001110000010000111110","11111001000010000100001000010000100","01110100011000111111100011000110001","11110100011000111110101001001010001","11111100001000011110100001000010000","01110100011000111111100011000110001","10000100001000010000100001000011111","10000100001000010000100001000011111"};
   const int cell=14;float left=800-(8*6-1)*cell*.5f;
   for(int pass=0;pass<2;pass++)for(int letter=0;letter<glyphs.Length;letter++)for(int y=0;y<7;y++)for(int x=0;x<5;x++){
    if(glyphs[letter][y*5+x]!='1')continue;
    var r=new Rect(left+(letter*6+x)*cell+(pass==0?7:0),191+y*cell+(pass==0?8:0),cell,cell);
    CampaignGameUI.Solid(r,pass==0?new Color(.12f,.16f,.20f):Color.Lerp(new Color(.92f,.95f,.96f),new Color(.45f,.53f,.60f),y/6f));
    if(pass==1)CampaignGameUI.Solid(new Rect(r.x,r.y,cell,2),new Color(1,1,1,.17f));
   }
  }
 }
}
