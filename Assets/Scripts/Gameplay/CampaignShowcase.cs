using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall {
 // A temporary, directed playthrough. CampaignRules and the player's store are never replaced.
 public sealed partial class CampaignRuntime {
  public bool ShowcaseActive {get;private set;}
  public bool ShowcasePaused {get;private set;}
  public bool ShowcaseFinished {get;private set;}
  public bool ShowcaseRestoring {get;private set;}
  public float ShowcaseElapsed {get;private set;}
  public int ShowcaseStage {get;private set;}
  public string ShowcaseError {get;private set;}="";
  internal int ShowcaseGuardianDodges=>showcaseDriver?.GuardianDodges??0;
  internal void ShowcaseDirection(string detail){showcaseDetail=detail;}
  internal void TraceShowcase(string action,string detail=""){
   Debug.Log(FormattableString.Invariant($"SHOWCASE_ACTION {action} frame={Time.frameCount} elapsed={ShowcaseElapsed:F3} {detail}"));
  }
  public float ShowcaseDelta=>ShowcaseActive&&!ShowcasePaused&&!(cinematics&&cinematics.IsPaused)?Time.unscaledDeltaTime:0;
  public bool CinematicActive=>cinematics&&cinematics.IsPlaying;
  public static bool StoryCinematicsEnabled {get=>PlayerPrefs.GetInt("Starfall.StoryCinematics",1)!=0;set=>PlayerPrefs.SetInt("Starfall.StoryCinematics",value?1:0);}
  CampaignCinematics cinematics;
  CampaignState showcaseOriginal;
  CampaignStore showcaseOriginalStore;
  bool showcaseOriginalSaveBlocked,showcaseStop,guardianOnly;
  bool showcaseOriginalUnderground;
  float showcaseOriginalX,showcaseOriginalScale,showcaseCastSpeed,showcaseRetrieveSpeed,showcaseOriginalBomb,showcaseOriginalPitch,showcaseOriginalAutosave;
  int showcaseOriginalRegion;
  Vector3 showcaseOriginalPosition;
  Quaternion showcaseOriginalRotation;
  RepairResource showcaseOriginalMetal;
  string showcaseSubtitle="",showcaseDetail="",showcaseReadoutTitle="",showcaseReadout="";
  CampaignShowcaseDriver showcaseDriver;
  readonly string[] showcaseTitles={"迫降之后 · 地下的另一片天空","穿过裂隙 · 取回回家的线索","营地整备 · 三次落锤","撤离战场 · 救援的呼声","医疗档案 · 裂隙的真相","军用船坞 · 门后的舰队","第五矿层 · 最后的隔离方案","回家或留下 · 准备最后一战","守核机甲 · 击破供能与弱点","核心崩解 · 最后四十五秒","星球的守望者 · 让荒原重新生长"};

  public bool StartShowcase()=>StartShowcase(false);
  public bool StartGuardianShowcase()=>StartShowcase(true);
  bool StartShowcase(bool guardian){
   if(!presentationReady||ShowcaseActive||ShowcaseRestoring||Busy||CinematicActive||director.Transitioning||director.battle.IsTransitioning||director.forge.IsOpen||director.hookState!=SceneDirector.HookState.Swinging)return false;
   Close();if(!saveBlocked)Save();
   showcaseOriginal=State;showcaseOriginalStore=Store;showcaseOriginalSaveBlocked=saveBlocked;
   showcaseOriginalRegion=State.region;showcaseOriginalUnderground=director.underground;showcaseOriginalX=director.player.position.x;
   if(Explorer){showcaseOriginalPosition=Explorer.transform.position;showcaseOriginalRotation=Explorer.transform.rotation;showcaseOriginalPitch=Explorer.Pitch;}
   showcaseOriginalScale=Time.timeScale;showcaseOriginalBomb=BombSeconds;showcaseOriginalAutosave=autosave;autosave=0;showcaseOriginalMetal=director.inventory.SelectedMetal;
   showcaseCastSpeed=director.castSpeed;showcaseRetrieveSpeed=director.retrieveSpeed;
   guardianOnly=guardian;
   string saveRoot=Path.GetDirectoryName(Store.directory);
   Store=new CampaignStore(guardian
    ?Path.Combine(saveRoot,"GuardianShowcase")
    :Path.Combine(saveRoot,"CampaignShowcase","active"));
   State=CampaignState.New();State.guidanceRoute="investigate";saveBlocked=false;progress.Clear();
   ShowcaseActive=true;ShowcaseFinished=false;ShowcasePaused=false;ShowcaseRestoring=false;ShowcaseError="";ShowcaseElapsed=0;ShowcaseStage=0;showcaseStop=false;
   BombSeconds=-1;ecologyTask=-1;emergencyItem=-1;treatmentTime=0;
   director.SetPaused(false);if(Explorer)Explorer.SetPaused(false);Time.timeScale=1;
   cinematics=CampaignCinematics.Ensure(this);
   var clock=GetComponent<CampaignShowcaseClock>();if(!clock)clock=gameObject.AddComponent<CampaignShowcaseClock>();clock.owner=this;
   showcaseDriver=new CampaignShowcaseDriver(this);
   StartCoroutine(DriveShowcase());return true;
  }
  public void SetShowcasePaused(bool paused){
   if(!ShowcaseActive||ShowcaseFinished||ShowcaseRestoring||CinematicActive)return;
   ShowcasePaused=paused;director.SetPaused(paused);if(Explorer)Explorer.SetPaused(paused||Modal);if(Modal&&!paused)Time.timeScale=0;
   if(director.forge.IsWorking&&director.forge.Paused!=paused)director.forge.TogglePause();
   Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  }
  public void StopShowcase(){
   if(!ShowcaseActive||ShowcaseRestoring)return;
   showcaseStop=true;SetShowcasePaused(false);if(cinematics)cinematics.Cancel();
  }
  internal void TickShowcase(){
   if(!ShowcaseActive||ShowcaseRestoring)return;
   if(!ShowcaseFinished)ShowcaseElapsed+=ShowcaseDelta;
   if(!CinematicActive&&Explorer&&Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
   if(!CinematicActive&&Input.GetKeyDown(KeyCode.Escape)&&!ShowcaseFinished)SetShowcasePaused(!ShowcasePaused);
   if(!ShowcaseFinished&&ShowcaseElapsed>590)ShowcaseError="演示流程超时，请退出后重新开始。";
   if(Explorer){Explorer.Automated=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=ShowcasePaused||ShowcaseFinished;}
  }
  IEnumerator DriveShowcase(){
   // Pump nested iterators so an exception in a helper also reaches the result page.
   var stack=new Stack<IEnumerator>();stack.Push(guardianOnly?ShowcaseGuardian():ShowcaseStory());
   while(stack.Count>0&&!showcaseStop&&ShowcaseError==""){
    bool moved=false;object next=null;
    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
    catch(Exception ex){ShowcaseError=ex.Message;Debug.LogException(ex);break;}
    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
    if(next is IEnumerator child){stack.Push(child);continue;}
    yield return next;
   }
   while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception ex){Debug.LogException(ex);}}
   if(!showcaseStop){
    ShowcaseFinished=true;ShowcasePaused=false;director.SetPaused(false);if(Explorer)Explorer.SetPaused(true);
    Close();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    while(!showcaseStop)yield return null;
   }
   yield return RestoreShowcaseStory();
  }
  void ShowcaseRequire(bool ok,string error){if(!ok)throw new InvalidOperationException(error);}
  void ShowcaseDriverCheck(){ShowcaseRequire(showcaseDriver.LastSucceeded,showcaseDriver.Failure);}
  void ShowcaseChapter(int stage,string subtitle,string detail){ShowcaseStage=stage;showcaseSubtitle=subtitle;showcaseDetail=detail;showcaseReadout="";InvalidateGuidance();}
  IEnumerator ShowcaseWait(float seconds){for(float t=0;t<seconds;t+=ShowcaseDelta)yield return null;}
  IEnumerator ShowcaseShot(CampaignCinematicBeat beat){while(ShowcaseActive&&ShowcasePaused&&!showcaseStop)yield return null;if(!ShowcaseActive||showcaseStop)yield break;yield return cinematics.Play(beat);}
  IEnumerator ShowcaseSurface(float x){
   Close();if(director.underground)yield return director.Travel(false);
   for(float t=0;t<12&&Mathf.Abs(director.player.position.x-x)>.15f;t+=ShowcaseDelta){
    if(ShowcaseDelta>0)director.Move(Mathf.Sign(x-director.player.position.x),ShowcaseDelta);
    yield return null;
   }
   ShowcaseRequire(Mathf.Abs(director.player.position.x-x)<.35f,"演示地表路线未抵达目标");
  }
  IEnumerator ShowcaseMine(int layer){
   if(!director.underground){yield return ShowcaseSurface(director.mine.transform.position.x);yield return director.Travel(true);}
   if(State.layer<layer){ShowcaseRequire(StartDrill(),"钻机前置条件尚未完成");while(Busy)yield return null;}
   ShowcaseRequire(State.layer==layer,"矿层顺序异常");
   yield return showcaseDriver.MineLayer();ShowcaseDriverCheck();
  }
  IEnumerator ShowcaseEnter(int id){
   if(!guardianOnly){Open("coordinates");yield return ShowcaseWait(2);}
   Close();
   ShowcaseRequire(EnterRegion(id),"尚未取得星界坐标 "+id);
   float timeout=0;while((!Explorer||director.battle.IsTransitioning)&&timeout<25){timeout+=ShowcaseDelta;yield return null;}
   ShowcaseRequire(Explorer&&!director.battle.IsTransitioning,"星界场景加载未完成");Explorer.Automated=true;
  }
  IEnumerator ShowcaseReturn(){
   Close();
   if(Explorer){
    var beacon=Explorer.returnBeacon.position;
    yield return showcaseDriver.MoveTo(beacon,24,2.35f);ShowcaseDriverCheck();
    yield return showcaseDriver.Face(beacon+Vector3.up*.85f);
    if(State.device&&!State.calibrated){
     ShowcaseRequire(CalibrateDevice(),"信标校准失败");Notice("入口信标已校准 · 跃迁坐标已记录");TraceShowcase("beacon-calibrated");
     yield return ShowcaseWait(1.15f);
    }
    Explorer.Return();
   }
   while(director.battle.IsEngaged)yield return null;
  }
  IEnumerator ShowcaseApproach(string key){
   ShowcaseRequire(RegionView&&RegionView.TryGuidancePoint(key,out _),"缺少主线交互点："+key);
   RegionView.TryGuidancePoint(key,out var point);
   yield return showcaseDriver.MoveTo(point+Vector3.back*2,25,.9f);ShowcaseDriverCheck();
   yield return showcaseDriver.Face(point+Vector3.up*.7f);
   ShowcaseRequire(RegionView.Near(key),"尚未到达主线交互点："+key);
  }
  IEnumerator ShowcaseArchive(int layer){
   yield return ShowcaseMine(layer);yield return ShowcaseEnter(layer);
   yield return ShowcaseApproach("terminal");Open("terminal");yield return ShowcaseWait(.65f);ShowcaseRequire(RegionView.ReadArchive(),"档案读取失败");
   if(layer==3){RegionView.SendPartialDiagnosis();ShowcaseRequire(SetExitPermission(false),"出口权限锁定失败");}
   if(layer==4)RegionView.RecordCommanderRoute();
   showcaseReadoutTitle=layer==2?"J03  /  撤离战场":layer==3?"J04 · J05  /  医疗报告":"J06  /  门后的舰队";
   showcaseReadout=layer==2?"撤离队列被空间异常截断。\n频道里仍有人请求你修复中继。\n\n那个自称救援站的人，身份尚未证实。":layer==3?"D4 武器撕裂空间，使本土生物发生变异。\n中继请求的质量，远超平民救援需求。\n\n仅发送局部诊断；现实出口继续锁定。":"军令、舰队清单与 D4 档案相互印证：\n所谓救援站，是远征议会的军需系统。\n他们想借你的坐标，把战争带入现实。";
   Open("terminal");yield return ShowcaseWait(4.5f);Close();showcaseReadout="";yield return ShowcaseReturn();
  }
  IEnumerator ShowcasePanelAction(string page,Func<bool> action,string error){
   Open(page);yield return ShowcaseWait(2);
   ShowcaseRequire(action(),error);yield return ShowcaseWait(3);
   Close();
  }
  IEnumerator ShowcaseGuardian(){
   if(director.battle.IsEngaged){director.battle.ReturnToExpedition();while(director.battle.IsEngaged)yield return null;}
   State.coordinates.Add(5);
   State.bag.Add(ItemId.Ammo,150);
   State.bag.Add(ItemId.Medkit,8);
   ShowcaseChapter(8,"守核机甲 · 供能节点与暴露核心","自动战斗 · Esc 暂停或退出");
   yield return ShowcaseEnter(5);
   var boss=RegionView.boss;
   ShowcaseRequire(boss,"守核机甲未加载");
   yield return showcaseDriver.FightGuardian();
   ShowcaseDriverCheck();
   ShowcaseRequire(boss.Dead,"守核机甲尚未击败");
   yield return ShowcaseWait(GuardianMechVisual.ShutdownSeconds+.50f);
  }
  IEnumerator ShowcaseStory(){
   if(director.battle.IsEngaged){director.battle.ReturnToExpedition();while(director.battle.IsEngaged)yield return null;}
   State=CampaignState.New();State.guidanceRoute="investigate";progress.Clear();director.CancelTravel();director.ResetShowcaseHook();director.underground=false;director.worldCamera.transform.position=new Vector3(0,0,-20);director.player.position=new Vector3(-10,director.surfaceY,0);ApplyLayer();
   // Only logistics are condensed: no knowledge, layer, boss, safety or ending flags are granted.
   State.bag.Add(ItemId.Ammo,150);State.bag.Add(ItemId.Medkit,8);Changed();
   ShowcaseChapter(0,"求生，从一钩矿石开始。","自动操作演示 · 抓钩保留真实碰撞与回收；后续重复开采加快。Esc 暂停 / 退出。");
   yield return ShowcaseShot(CampaignCinematicBeat.Crash);yield return ShowcaseMine(1);
   ShowcaseChapter(1,"裂纹中藏着另一片天空；遗骸里留着回家的图纸。","第一人称探索与射击 · 取回货舱档案，再校准返程信标。");
   yield return ShowcaseEnter(1);yield return ShowcaseShot(CampaignCinematicBeat.RiftArrival);
   if(RegionView.TryGuidancePoint("chest",out var cargo))yield return showcaseDriver.Face(cargo+Vector3.up*1.2f,.7f);
   ShowcaseDirection("离开入口信标，确认前方巡逻目标。保持准星视线，再短促开火。");
   yield return showcaseDriver.ShootEncounter();ShowcaseDriverCheck();
   ShowcaseDirection("交火结束。沿可通行路线前往遗骸货舱，随后返回入口信标。");
   yield return ShowcaseApproach("chest");Open("chest");yield return ShowcaseWait(.75f);ShowcaseRequire(OpenWreckChest(),"货舱开启失败");TraceShowcase("wreck-cargo");showcaseReadoutTitle="J01  /  第七次回收";showcaseReadout="裂纹矿石里，有一片不属于这里的天空。\n\n已取回：维修图纸、弹药配方和绑定跃迁器。\n先校准入口信标，给自己留一条回去的路。";Open("chest");yield return ShowcaseWait(5);Close();showcaseReadout="";yield return ShowcaseReturn();
   ShowcaseChapter(2,"看准时机，三次落锤，把原矿变成回家的零件。","真实三段锻造 QTE → 制作弹药。重复冶炼将在整备章节压缩结算。");
   yield return ShowcaseSurface(director.camp.transform.position.x);
   Open("forge");yield return ShowcaseWait(2);Close();
   yield return showcaseDriver.ForgeOne(RepairResource.PurpleCopper);ShowcaseDriverCheck();
   yield return ShowcasePanelAction("craft",CraftAmmo,"弹药制作失败");
   ShowcaseChapter(3,"撤离编号还在，等待救援的人却已消失。","贯通第二矿层 → 撤离战场 → J03 撤离档案。只沿主线调查。");
   yield return ShowcaseArchive(2);
   ShowcaseChapter(4,"所谓变异怪物，曾是这颗星球自己的生命。","第三矿层 → 医疗报告 J04 / J05。只发送局部诊断，保留现实出口锁定。");
   yield return ShowcaseArchive(3);
   ShowcaseChapter(5,"他们索要的并非救援通道，而是整支舰队的出口。","第四矿层 → 军用船坞 J06。拒绝交出跃迁装置，保留隔离选择。");
   yield return ShowcaseArchive(4);
   ShowcaseChapter(6,"关闭共用核心，是阻止 D4 进入现实的最后方案。","第五矿层 → 稳定异晶 → J07 隔离方案与种源库。保留全部通关前置。");
   yield return ShowcaseMine(5);yield return ShowcaseEnter(5);yield return ShowcaseApproach("terminal");Open("terminal");yield return ShowcaseWait(.65f);ShowcaseRequire(RegionView.ReadArchive(),"隔离方案读取失败");showcaseReadoutTitle="J07  /  最后的隔离方案";showcaseReadout="共用核心连接着全部星界。\n切断现实耦合、锁定安全锚点，再引爆核心。\n\n崩解后，便携跃迁装置也将永久失效。";Open("terminal");yield return ShowcaseWait(5);Close();showcaseReadout="";yield return ShowcaseReturn();
   ShowcaseChapter(7,"飞船可以带你离开；你也可以选择阻止灾难。","演示补给压缩重复冶炼：先在营地制作异晶炸弹，再按原配方修船、校准并装填燃料。此轮继续守望者主线。");
   yield return ShowcaseSurface(director.camp.transform.position.x);
   if(State.crates.Count>0){TransportCrates();foreach(var crate in new List<SavedCrate>(State.crates))ShowcaseRequire(RecoverCrate(crate.id),"演示回收箱物资暂未取回");}
   // Pay the original one-raw-per-ingot recipes; show one full QTE above, condense the other 20.
   foreach(var item in new[]{ItemId.PurpleIngot,ItemId.IronIngot,ItemId.CopperIngot,ItemId.CobaltIngot,ItemId.StarIngot}){
    int need=item==ItemId.PurpleIngot?2:item==ItemId.IronIngot?6:item==ItemId.CopperIngot||item==ItemId.CobaltIngot?4:5;
    int missing=Mathf.Max(0,need-State.bag.Count(item));if(missing>0)ShowcaseRequire(Transaction(State,new[]{new ItemStack((ItemId)((int)item-5),missing)},new ItemStack(item,missing)),"重复冶炼物资不足");
   }
   yield return ShowcasePanelAction("craft",CraftBomb,"异晶炸弹制作失败");
   yield return ShowcaseSurface(director.wreck.transform.position.x);Open("ship");yield return ShowcaseWait(.65f);showcaseReadoutTitle="飞船修复  /  另一条回家的路";
   for(int i=0;i<4;i++){ShowcaseRequire(Repair(i),"飞船维修失败 "+i);showcaseReadout="正在安装："+CampaignRules.RepairNames[i]+"\n\n已修复 "+State.repairs.Count+" / 4 项 · 使用原配方材料";yield return ShowcaseWait(1);}
   ShowcaseRequire(CalibrateNavigation(),"导航校准失败");yield return ShowcaseWait(1.5f);
   ShowcaseRequire(FuelShip()&&EscapeReady,"飞船起飞条件未完成");showcaseReadout="四项修复、导航校准与燃料装填全部完成。\n逃离结局已经具备条件。\n\n这一次，你选择留下，关闭 D4。";yield return ShowcaseWait(3.5f);Close();showcaseReadout="";
   ShowcaseChapter(8,"先摧毁供能节点，再抓住胸部弱点暴露的时机。","守核机甲战 · 三处供能节点 / 范围炮预警 / 换弹与弱点射击。预警出现时优先躲避。");
   yield return ShowcaseEnter(5);yield return ShowcaseShot(CampaignCinematicBeat.Guardian);yield return showcaseDriver.FightGuardian();ShowcaseDriverCheck();ShowcaseRequire(State.Has("boss-defeated"),"守核机甲尚未击败");yield return ShowcaseWait(GuardianMechVisual.ShutdownSeconds+.25f);
   ShowcaseChapter(9,"广播、隔离、锚定。最后一次跃迁，必须回到现实。","核查生命维持 → 切断现实耦合 → 锁定锚点 → 安装炸弹 → 原版 45 秒撤离。");
   yield return ShowcaseApproach("core");Open("core");yield return ShowcaseWait(.7f);for(int i=0;i<3;i++){ShowcaseRequire(RegionView.CoreStep(i),"隔离安全步骤失败 "+i);yield return ShowcaseWait(1.2f);}
   ShowcaseRequire(InstallBomb(),"炸弹安装失败");yield return ShowcaseWait(2);
   ShowDetonate();yield return ShowcaseWait(1.7f);
   ShowcaseRequire(BeginDetonation(),"引爆检查点保存失败");yield return ShowcaseShot(CampaignCinematicBeat.Collapse);
   ShowcaseDirection("45 秒倒计时已经启动。沿通道跑回入口信标，完成跃迁。");
   TraceShowcase("evacuation-start",$"seconds={BombSeconds:F2} position={Explorer.transform.position}");
   yield return showcaseDriver.MoveTo(Explorer.returnBeacon.position,36,2.35f);ShowcaseDriverCheck();
   TraceShowcase("evacuation-beacon",$"seconds={BombSeconds:F2} distance={Vector3.Distance(Explorer.transform.position,Explorer.returnBeacon.position):F2}");
   ShowcaseRequire(Evacuate(),"未能在倒计时内撤离");while(director.battle.IsEngaged)yield return null;
   ShowcaseChapter(10,"留下来，让荒原再长出第一簇幼苗。","结局必需的三项重建：关闭污染管道、恢复净水、播下种子。支线委托全部略过。");
   yield return ShowcaseWait(2.5f);ChooseEcology();
   for(int i=0;i<3;i++){yield return ShowcaseSurface(ecologyX[i]);ShowcaseRequire(TryEcology(),"生态设施交互失败");while(ecologyTask>=0)yield return null;}
   ShowcaseRequire(State.saviorComplete,"守望者结局尚未完成");
   yield return ShowcaseWait(3);Close();yield return ShowcaseShot(CampaignCinematicBeat.Homecoming);
  }
  IEnumerator RestoreShowcaseStory(){
   ShowcaseRestoring=true;if(cinematics)cinematics.Cancel();enabled=true;Close();Time.timeScale=1;director.SetPaused(false);if(Explorer)Explorer.SetPaused(false);
   director.forge.CancelShowcaseForge();director.ResetShowcaseHook();
   // Drill is a short existing coroutine: let it release Busy before swapping the state.
   while(Busy)yield return null;
   while(director.battle.IsTransitioning)yield return null;
   if(director.battle.IsEngaged){director.battle.ReturnToExpedition();while(director.battle.IsEngaged)yield return null;}
   director.CancelTravel();BombSeconds=-1;ecologyTask=-1;emergencyItem=-1;treatmentTime=0;returnFlash=0;greenAge=0;
   State=showcaseOriginal;Store=showcaseOriginalStore;saveBlocked=showcaseOriginalSaveBlocked;autosave=showcaseOriginalAutosave;progress.Clear();
   director.castSpeed=showcaseCastSpeed;director.retrieveSpeed=showcaseRetrieveSpeed;director.inventory.SelectedMetal=showcaseOriginalMetal;
   director.underground=showcaseOriginalUnderground;director.player.position=new Vector3(showcaseOriginalX,director.surfaceY,0);
   director.worldCamera.transform.position=new Vector3(0,director.underground?director.undergroundCameraY:0,-20);ApplyLayer();InvalidateGuidance();director.inventory.NotifyChanged();
   // EnterRegion saves normally, so rebuild the previous view directly while saves are blocked.
   if(showcaseOriginalRegion>0&&!State.Has("rift-destroyed")){
    loading=true;director.battle.Enter(null);while(director.battle.IsTransitioning)yield return null;
    if(Explorer){CampaignShowcaseDriver.Warp(Explorer,showcaseOriginalPosition);Explorer.transform.rotation=showcaseOriginalRotation;Explorer.Automated=IsQA;Explorer.SetPaused(false);Explorer.Look(0,Explorer.Pitch-showcaseOriginalPitch);}loading=false;
   }
   BombSeconds=showcaseOriginalBomb;ShowcaseActive=false;ShowcasePaused=false;ShowcaseRestoring=false;showcaseOriginal=null;
   Time.timeScale=showcaseOriginalScale>0?showcaseOriginalScale:1;
   if(Explorer)Explorer.SetPaused(true);else director.SetPaused(true);
   Notice(guardianOnly?"已退出机甲战演示，原故事进度已恢复":"已退出主线演示，原故事进度已恢复");
   guardianOnly=false;
  }
  void DrawShowcaseSettings(Rect rect){
   if(ShowcaseActive||CinematicActive)return;
   var left=rect;left.x-=rect.width*.5f+8;
   var right=rect;right.x+=rect.width*.5f+8;
   if(Action(left,"主线演示"))Open("showcase");
   if(Action(right,"直看机甲战")&&!StartGuardianShowcase())Notice("请等待当前交互完成后开始演示");
  }
  void DrawShowcaseSetup(){
   Paragraph(new Rect(250,215,1100,95),"主线精华演示",29);
   Paragraph(new Rect(250,299,1100,175),"自动展示抓钩采矿、三段锻造、五个星界调查、守核机甲、45秒撤离与守望者结局。\n只做主线；跳过重复赶路，压缩重复开采与冶炼，并提供演示弹药和医疗补给。\n使用独立临时进度；随时按 Esc 暂停或退出，返回原故事。",22);
   Caption(new Rect(250,499,1100,35),"含剧情剧透 · 过场可按空格 / Enter 跳过 · 暂停时间不计入演示时长",17,CampaignGameUI.Gold);
   bool ready=!Busy&&!director.battle.IsTransitioning&&!director.forge.IsOpen&&director.hookState==SceneDirector.HookState.Swinging;
   if(!ready)Caption(new Rect(250,544,1100,30),"请先返回游戏，等待抓钩回收或当前交互完成，再开始演示。",17,CampaignGameUI.Warning);
   if(Action(new Rect(320,599,430,64),"开始自动主线演示",ready)){if(!StartShowcase())Notice("请等待当前交互完成后开始演示");}
   if(Action(new Rect(850,599,430,64),"返回游戏"))Close();
  }
  void DrawShowcaseOverlay(){
   if(!ShowcaseActive||!director.uiFont||CinematicActive)return;
   var matrix=GUI.matrix;var depth=GUI.depth;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);GUI.depth=-590;
   bool compact=Modal;
   float headerHeight=compact?62:ShowcaseHudHeight;
   CampaignGameUI.Solid(new Rect(0,0,1600,headerHeight),new Color(.015f,.028f,.04f,.96f));
   Caption(new Rect(35,12,1000,35),guardianOnly?"守核机甲战   /   自动演示":"主线演示   /   "+(ShowcaseStage+1).ToString("D2")+"   "+showcaseTitles[Mathf.Clamp(ShowcaseStage,0,showcaseTitles.Length-1)],22,CampaignGameUI.Gold);
   Caption(new Rect(compact?1050:1300,compact?12:19,265,35),Mathf.FloorToInt(ShowcaseElapsed/60).ToString("D2")+":"+Mathf.FloorToInt(ShowcaseElapsed%60).ToString("D2"),18,null,TextAnchor.MiddleRight);
   if(!compact){
    Caption(new Rect(35,49,1080,25),showcaseSubtitle,17);
    DrawShowcaseStatus(new Rect(1125,49,440,25));
    Paragraph(new Rect(35,79,1250,35),showcaseDetail,16);
   }
   if(!guardianOnly){
    float chapterProgress=(ShowcaseFinished&&ShowcaseError==""?showcaseTitles.Length:ShowcaseStage)/(float)showcaseTitles.Length;
    CampaignGameUI.Solid(new Rect(0,headerHeight-3,1600*Mathf.Clamp01(chapterProgress),3),CampaignGameUI.Gold);
   }
   if(compact&&showcaseReadout!=""){
    CampaignGameUI.Solid(new Rect(0,840,1600,60),new Color(.015f,.028f,.04f,.96f));
    string detail=showcaseReadoutTitle+"   "+string.Join("   ",showcaseReadout.Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries));
    Paragraph(new Rect(35,850,1530,48),detail,16);
   }
   if(!ShowcasePaused&&!ShowcaseFinished&&!ShowcaseRestoring&&CampaignGameUI.Button(new Rect(1350,compact?13:77,215,33),"Esc 暂停",director.uiFont))SetShowcasePaused(true);
   if(ShowcasePaused||ShowcaseFinished||ShowcaseRestoring){
    CampaignGameUI.Solid(new Rect(0,0,1600,900),new Color(.005f,.012f,.02f,.84f));
    Caption(new Rect(290,225,1020,70),ShowcaseRestoring?"正在恢复原故事…":ShowcaseFinished?(ShowcaseError==""?(guardianOnly?"守核机甲已击败":"主线演示完成"):"演示已中断"):"演示暂停",36,CampaignGameUI.Gold,TextAnchor.MiddleCenter);
    Paragraph(new Rect(360,329,880,140),ShowcaseFinished?(ShowcaseError==""?(guardianOnly?"退出后回到进入演示前的故事。":"五层矿井 · 五个星界 · 真相调查 · 守核机甲\n核心隔离与撤离 · 星球的守望者\n\n原游戏还保留独立的修船逃离结局。"):ShowcaseError):"演示计时、战斗与倒计时已暂停。\n退出后回到进入演示前的故事。",23);
    if(!ShowcaseRestoring){if(!ShowcaseFinished&&CampaignGameUI.Button(new Rect(440,548,720,55),"继续演示",director.uiFont))SetShowcasePaused(false);if(CampaignGameUI.Button(new Rect(440,624,720,55),"退出演示 · 恢复原故事",director.uiFont))StopShowcase();}
   }
   GUI.matrix=matrix;GUI.depth=depth;GUI.color=Color.white;
  }
  internal const float ShowcaseHudHeight=116;
  void DrawShowcaseStatus(Rect rect){
   if(BombSeconds>=0){
    Caption(rect,"撤离倒计时  "+Mathf.CeilToInt(BombSeconds)+" 秒",18,CampaignGameUI.Warning,TextAnchor.MiddleRight);
    return;
   }
   if(Explorer||director.forge.IsOpen)return;
   string ore="原矿 "+director.DisplayedOreCount;
   if(director.underground)ore="矿层 "+State.layer+"   ·   已采 "+Collected(State.layer)+" / "+CampaignRules.NodeCount(State.layer)+"   ·   "+ore;
   Caption(rect,ore,17,CampaignGameUI.Cyan,TextAnchor.MiddleRight);
  }
 }
 public sealed class CampaignShowcaseClock:MonoBehaviour {
  public CampaignRuntime owner;
  void Update(){if(owner)owner.TickShowcase();}
 }
}
