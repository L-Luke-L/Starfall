using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  string panel="";int tab,selected=-1,warehousePage,selectedJournal;bool fromWarehouse,priorPaused;float priorScale=1,emergencyTime;int emergencyItem=-1;int consumedFrame=-1;
  public bool ConsumedInput=>consumedFrame==Time.frameCount;
  public void Open(string page){if(!Modal){priorScale=Time.timeScale;priorPaused=Explorer?Explorer.Paused:director.IsPaused;if(Explorer)Explorer.SetPaused(true);else Time.timeScale=0;}panel=page;selected=-1;consumedFrame=Time.frameCount;InteractionFeel.Play(FeelCue.Click,.5f);}
  public void Close(){if(!Modal)return;panel="";emergencyItem=-1;consumedFrame=Time.frameCount;if(Explorer)Explorer.SetPaused(priorPaused);Time.timeScale=priorScale;}
  void Toggle(string page){if(Modal)Close();else Open(page);}
  public bool SurfaceInteract(){
   if(Modal||Busy)return true;
   if(State.Has("ecology-chosen")&&!director.underground&&TryEcology())return true;
   if(director.underground){if(State.coordinates.Contains(State.layer)){Open("coordinates");return true;}return false;}
   if(TryFrontierTerminal()||TryCampCompanion())return true;
   if(AtCamp){Open("camp");return true;}if(AtShip){Open("ship");return true;}return false;
  }
  string Objective {
   get {if(State.saviorComplete)return "生态站运行中";if(State.Has("ecology-chosen"))return "关闭污染 · 修复净水 · 播下种子";if(State.Has("rift-destroyed"))return "回到营地，决定留下";if(State.escapeComplete&&State.Has("escape-view"))return "航程继续";
    if(!string.IsNullOrEmpty(TrackedHint))return TrackedHint;
    if(!State.coordinates.Contains(1))return "矿井 · 回收紫铜与裂纹矿物";if(!State.device)return "星界遗骸 · 调查中央货舱";if(!State.calibrated)return "返回信标 · 校准跃迁装置";
    if(State.repairs.Count<4)return "探索矿层 "+State.unlockedLayer+" · 收集修船材料";if(!State.Knows("J07"))return "飞船可修复 · 调查同盟档案";if(!State.Has("bomb-crafted"))return "修船离开 / 制作异晶炸弹";if(!State.Has("boss-defeated"))return "核心 · 击破三处供能节点";return "切断耦合 · 校准撤离锚点";}
  }
  void Caption(Rect r,string text,int size=18,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft){InteractionFeel.Text(r,text,director.uiFont,size,color??new Color(.84f,.92f,.92f),align);}
  bool Action(Rect r,string text,bool enabled=true)=>InteractionFeel.Button(r,text,director.uiFont,17,new Color(.94f,.73f,.38f),enabled&&!Busy);
  void Paragraph(Rect r,string text,int size=18){var style=new GUIStyle(GUI.skin.label){font=director.uiFont,fontSize=size,wordWrap=true,normal={textColor=new Color(.8f,.88f,.89f)}};GUI.Label(r,text,style);}
  void OnGUI(){
   if(Current!=this||State==null||!director.uiFont)return;var previous=GUI.matrix;GUI.depth=-160;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,Vector3.one*scale);
   if(!director.forge.IsOpen&&!Modal){
    if(!director.battle.IsEngaged&&!director.underground)DrawCampPrompt();
    Caption(new Rect(44,88,780,26),Objective,14,new Color(.72f,.83f,.85f,.9f));Caption(new Rect(1130,director.underground&&!director.battle.IsEngaged?802:849,430,25),"B 背包   J 手账   F5 保存",12,null,TextAnchor.MiddleRight);
    if(director.underground&&!director.battle.IsEngaged){Caption(new Rect(525,45,550,32),"矿层 "+State.layer+"  ·  "+Collected(State.layer)+" / "+CampaignRules.NodeCount(State.layer),18,null,TextAnchor.MiddleCenter);if(Action(new Rect(650,93,300,35),Busy?"钻进中…":LayerComplete(State.layer)&&State.layer==State.unlockedLayer&&State.layer<5?"启动钻机 ↓":"切换已解锁矿层",!Busy)){if(LayerComplete(State.layer)&&State.layer==State.unlockedLayer&&State.layer<5)StartDrill();else Open("layers");}}
    if(BombSeconds>=0)Caption(new Rect(550,136,500,50),"核心崩解  "+Mathf.CeilToInt(BombSeconds)+"s · 返回信标",26,new Color(1,.45f,.23f),TextAnchor.MiddleCenter);
   }
   if(toastAge>0&&!Modal)Caption(new Rect(390,725,820,42),toast,19,null,TextAnchor.MiddleCenter);
   if(Modal){
    GUI.color=new Color(.015f,.026f,.037f,.90f);GUI.DrawTexture(new Rect(120,90,1360,720),Texture2D.whiteTexture);GUI.color=Color.white;
    Caption(new Rect(175,122,950,42),Title(panel),30);if(Action(new Rect(1335,124,90,35),"Esc"))Close();
    if(panel=="frontier")DrawFrontier();else if(panel=="companion")DrawCompanion();else if(panel=="bag"||panel=="camp"||panel=="warehouse")DrawInventory();else if(panel=="workshop")DrawWorkshop();else if(panel=="forge")DrawForgeSelect();else if(panel=="craft")DrawCraft();else if(panel=="ship")DrawShip();else if(panel=="takeoff")DrawTakeoff();else if(panel=="coordinates")DrawCoordinates();else if(panel=="journal")DrawJournal();else if(panel=="system")DrawSystem();else if(panel=="new-game")DrawNewGame();else if(panel=="layers")DrawLayers();else if(panel=="chest")DrawChest();else if(panel=="terminal")DrawTerminal();else if(panel=="permission")DrawPermission();else if(panel=="core")DrawCore();else if(panel=="detonate")DrawDetonate();else if(panel=="remain")DrawRemain();else if(panel=="escaped"||panel=="saved")DrawEnding();
    if(toastAge>0)Caption(new Rect(180,755,1240,32),toast,15,new Color(1,.79f,.42f));
   }
   DrawPresentation();GUI.matrix=previous;GUI.color=Color.white;
  }
  static string Title(string page){switch(page){case "frontier":return "余烬重建";case "companion":return "营地伙伴";case "workshop":return "营地工坊";case "camp":case "warehouse":return "营地 · 物资整备";case "bag":return "随身背包";case "forge":return "选择原矿";case "craft":return "制作与应急回收";case "ship":return "飞船维修终端";case "coordinates":return "已记录坐标";case "journal":return "探险者手账";case "system":return "故事与存档";case "new-game":return "开始新的故事？";case "layers":return "矿井升降控制";case "chest":return "同盟探险船 · 密封货舱";case "permission":return "现实端出口 · 权限确认";case "terminal":return "区域档案终端";case "core":return "D4核心控制台";case "detonate":return "确认引爆？";case "takeoff":return "确认起飞？";case "remain":return "裂隙已经熄灭";case "escaped":return "结局 · 下一段航程";case "saved":return "结局 · 星球的守望者";default:return page;}}
  void DrawInventory(){
   bool camp=AtCamp;Caption(new Rect(180,183,1100,32),"绑定装备：手枪  ·  冲击镐  ·  抓钩"+(State.device?"  ·  同盟跃迁装置":"")+"   ｜   装备与已记录知识死亡不掉落",15);
   if(camp){if(Action(new Rect(180,225,180,38),"锻造"))panel="forge";if(Action(new Rect(370,225,220,38),"制作 / 回收"))panel="craft";if(Action(new Rect(600,225,180,38),"手账"))panel="journal";if(Action(new Rect(790,225,180,38),"存档"))panel="system";if(Action(new Rect(980,225,180,38),Exp("engineer-rescued")?"工坊 / 医疗":"医疗",Exp("engineer-rescued")||Exp("doctor-rescued")))panel="workshop";if(Action(new Rect(1170,225,195,38),"转运回收箱")){TransportCrates();Notice("回收箱已转运到营地");}}
   bagPage=Mathf.Clamp(bagPage,0,(State.bag.capacity-1)/16);Caption(new Rect(180,277,550,28),"背包 "+State.bag.stacks.Count+" / "+State.bag.capacity,18);ContainerGrid(State.bag,new Vector2(180,318),false,bagPage*16);
   if(State.bag.capacity>16){if(Action(new Rect(200,633,160,30),"上一页"))bagPage=1-bagPage;Caption(new Rect(370,633,70,30),(bagPage+1)+" / 2",15);if(Action(new Rect(460,633,160,30),"下一页"))bagPage=1-bagPage;}
   if(camp){Caption(new Rect(830,277,480,28),"帐篷仓库 "+State.warehouse.stacks.Count+" / 48",18);ContainerGrid(State.warehouse,new Vector2(830,318),true,warehousePage*16);if(Action(new Rect(850,633,160,30),"上一页"))warehousePage=(warehousePage+2)%3;Caption(new Rect(1020,633,70,30),(warehousePage+1)+" / 3",15);if(Action(new Rect(1110,633,160,30),"下一页"))warehousePage=(warehousePage+1)%3;}
   else Paragraph(new Rect(845,325,500,175),"普通物资死亡后留在回收箱。\n到帐篷可存取仓库与制作。\n\n任务栏："+(State.Has("bomb-crafted")?"异晶炸弹（受保护）":"暂无主线成品"));
   var container=fromWarehouse?State.warehouse:State.bag;
   if(selected>=0&&selected<container.stacks.Count){var stack=container.stacks[selected];Caption(new Rect(180,671,350,32),CampaignRules.ItemNames[(int)stack.item]+" ×"+stack.count,18);
    if(camp){if(Action(new Rect(550,671,180,34),fromWarehouse?"取出全部":"存入全部")){if(!container.TransferTo(fromWarehouse?State.bag:State.warehouse,stack.item,stack.count))Notice("目标容器空间不足");Changed();selected=-1;}if(selected>=0&&Action(new Rect(735,671,150,34),"转移一半")){if(!container.TransferTo(fromWarehouse?State.bag:State.warehouse,stack.item,Math.Max(1,stack.count/2)))Notice("目标容器空间不足");Changed();selected=-1;}}
    if(selected>=0&&Action(new Rect(900,671,140,34),"拆分")){if(!container.Split(selected))Notice("需要空格且数量至少为2");Changed();}
    if(!fromWarehouse&&stack.item==ItemId.Medkit&&Action(new Rect(1070,671,200,34),"使用急救剂")){if(!UseMedkit())Notice("护甲已满");}
   }
   if(Action(new Rect(180,715,150,32),"合并 / 排序")){State.bag.Sort();if(camp)State.warehouse.Sort();selected=-1;Changed();}
   if(camp&&Action(new Rect(370,715,220,32),"存入仓库已有同类")){foreach(var s in new List<ItemStack>(State.bag.stacks))if(State.warehouse.Count(s.item)>0)State.bag.TransferTo(State.warehouse,s.item,Math.Min(s.count,FreeSpace(State.warehouse,s.item)));Changed();}
   if(camp){int i=0;foreach(var c in new List<SavedCrate>(State.crates)){if(c.region!=0)continue;if(Action(new Rect(790+i*150,715,145,32),"回收箱 "+(++i))){if(!RecoverCrate(c.id))Notice("背包空间不足");}if(i>=4)break;}}
  }
  void ContainerGrid(ItemContainer c,Vector2 origin,bool warehouse,int offset){
   for(int i=0;i<16;i++){int index=i+offset;var r=new Rect(origin.x+i%4*137,origin.y+i/4*75,129,65);bool exists=index<c.stacks.Count;GUI.color=selected==index&&fromWarehouse==warehouse?new Color(.16f,.30f,.34f,.9f):new Color(.09f,.13f,.17f,.85f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;if(exists){var s=c.stacks[index];if(Action(r,CampaignRules.ItemNames[(int)s.item]+"\n×"+s.count)){selected=index;fromWarehouse=warehouse;}}}
  }
  void DrawForgeSelect(){int i=0;foreach(var metal in new[]{RepairResource.PurpleCopper,RepairResource.FerricAlloy,RepairResource.CopperCompound,RepairResource.CobaltSilicate,RepairResource.RareComposite}){var id=CampaignRules.Raw(metal);int b=State.bag.Count(id),w=State.warehouse.Count(id);if(Action(new Rect(200,220+i*81,550,61),OreInventory.OreName(metal)+"   背包 "+b+" / 仓库 "+w,b+w>0)){director.inventory.SelectedMetal=metal;Close();director.forge.OpenCamp();director.forge.OpenWorkshop();}i++;}Paragraph(new Rect(855,260,490,230),"三次落锤产出一锭。\n稀有矿物的节奏更复杂。\n\n紫铜原矿也用于制作弹药，请留出原矿。\n稳定异晶不参与金属锻造。");}
  void DrawCraft(){
   Paragraph(new Rect(190,193,1100,45),"优先使用背包，其次仓库；产物放入背包。放不下时不扣材料。",16);
   if(Action(new Rect(180,252,610,48),"手枪弹药 ×24   ·   紫铜原矿2 + 燃料1",State.Has("ammo-recipe"))&&!CraftAmmo())Notice("材料不足或背包空间不足");
   if(Action(new Rect(180,321,610,48),State.Has("bomb-crafted")?"异晶炸弹已制作":"异晶炸弹   ·   异晶6 + 燃料4 + 星髓锭2",State.Knows("J07")&&!State.Has("bomb-crafted"))&&!CraftBomb())Notice("材料不足，或尚未进入第五层");
   Caption(new Rect(185,420,600,35),"应急回收 · 无弹药消耗",22);Paragraph(new Rect(185,463,590,120),"主线所需物资不足时，可用冲击镐低效率回收。地面掉落、未采矿点和死亡箱中的物资均计入判断。\n关闭面板会中断回收；不会改变矿层完成率。",16);
   int row=0;foreach(var item in new[]{ItemId.PurpleOre,ItemId.IronOre,ItemId.CopperOre,ItemId.CobaltOre,ItemId.StarOre,ItemId.Fuel,ItemId.AlienCrystal}){if(CampaignRules.MinimumLayer(item)>State.unlockedLayer)continue;if(Action(new Rect(850,230+row*59,460,44),CampaignRules.ItemNames[(int)item]+(CanEmergency(item)?" · 回收8秒":" · 现有来源足够"),CanEmergency(item)&&emergencyItem<0)){emergencyItem=(int)item;emergencyTime=0;}row++;}
   if(emergencyItem>=0){InteractionFeel.Ring(new Vector2(465,658),32,emergencyTime/8,new Color(.4f,.85f,.84f));Caption(new Rect(340,695,500,30),"回收 "+CampaignRules.ItemNames[emergencyItem],16);}
  }
  void TickEmergency(){if(emergencyItem<0)return;if(panel!="craft"||!AtCamp){emergencyItem=-1;return;}emergencyTime+=Time.unscaledDeltaTime;if(emergencyTime>=8){bool ok=CompleteEmergency((ItemId)emergencyItem);Notice(ok?"应急物资 +1":"无需回收或背包已满");emergencyItem=-1;}}
  string Cost(ItemStack[] costs){string text="";foreach(var s in costs)text+=CampaignRules.ItemNames[(int)s.item]+" "+s.count+"（"+State.bag.Count(s.item)+"+"+State.warehouse.Count(s.item)+"）  ";return text;}
  void DrawShip(){Caption(new Rect(190,197,1100,32),State.Has("repair-plan")?"维修档案已适配 · 数量显示为背包+仓库":"缺少修复图纸 · 调查第一层星界遗骸",17);for(int i=0;i<4;i++){bool done=State.repairs.Contains(i.ToString());Caption(new Rect(200,259+i*80,260,40),CampaignRules.RepairNames[i],20);Paragraph(new Rect(470,259+i*80,640,50),done?"已安装":Cost(CampaignRules.RepairCost(i)),15);if(Action(new Rect(1170,259+i*80,200,40),done?"完成":"提交材料",!done&&State.Has("repair-plan"))&&!Repair(i))Notice("缺少材料；稳定器需第五层材料");}
   if(Action(new Rect(190,606,350,45),State.Has("ship-navigation")?"导航已校准":"校准导航",State.repairs.Count==4))CalibrateNavigation();if(Action(new Rect(560,606,350,45),State.Has("ship-fueled")?"燃料已装填":"装填燃料 ×8",State.repairs.Count==4&&!State.Has("ship-fueled"))&&!FuelShip())Notice("燃料不足");if(Action(new Rect(1020,666,340,50),"准备起飞",EscapeReady))panel="takeoff";
  }
  void DrawTakeoff(){Paragraph(new Rect(290,270,1020,210),"起飞将结束本轮故事。\n\n会保留独立的“起飞前”存档，你可以从中继续调查。是否查明战争真相，不影响离开的资格。");if(Action(new Rect(350,585,360,60),"确认起飞"))Escape();if(Action(new Rect(850,585,360,60),"继续探索"))panel="ship";}
  void DrawCoordinates(){int n=0;foreach(var id in State.coordinates){if(Action(new Rect(290,226+n*78,940,55),id+"  "+CampaignRules.Regions[id]+(State.calibrated?" · 已记录":" · 临时裂隙"),!State.Has("rift-destroyed"))){Close();EnterRegion(id);}n++;}if(n==0)Paragraph(new Rect(300,285,1000,100),"尚未记录坐标。回收带裂纹的矿物，靠近核心主动触碰。");if(director.underground&&Action(new Rect(560,674,480,48),"返回地表")){Close();StartCoroutine(director.Travel(false));}}
  void DrawLayers(){for(int i=1;i<=5;i++)if(Action(new Rect(360,217+i*77,880,52),"第 "+i+" 层   "+(i<=State.unlockedLayer?Collected(i)+" / "+CampaignRules.NodeCount(i):"尚未贯通"),i<=State.unlockedLayer)){SelectLayer(i);Close();}}
  void DrawChest(){Paragraph(new Rect(225,225,1150,200),State.device?"已永久记录：弹药配方、飞船修复图纸、探险者手账。\n同盟便携跃迁装置已绑定，不占背包，不会死亡掉落。\n\n普通燃料与急救剂仅在背包有空间时取出，剩余物资保留在箱内。":"探险者的密封货舱。箱内装有撤离档案和一台仍有电力的跃迁装置。",21);if(Action(new Rect(450,546,700,60),State.device?"取出剩余补给":"解锁档案与跃迁装置")){if(OpenWreckChest())Notice("知识已记录 · 返回入口信标校准");}if(State.Knows("J01")&&Action(new Rect(600,635,400,45),"阅读探险者手账"))panel="journal";}
  void DrawTerminal(){RegionView?.DrawTerminalView();}
  public void ReviewPermission(){Open("permission");State.Flag("permission-reviewed");Changed();}
  public bool SetExitPermission(bool authorize){if(State.region!=3||!RegionView||!RegionView.Near("terminal"))return false;if(authorize)State.Flag("exit-authorized");else State.flags.Remove("exit-authorized");Changed();return true;}
  void DrawPermission(){Paragraph(new Rect(275,245,1040,235),"完整授权会向中继开放现实端出口权限。\n质量请求超过单人救援需求，你尚无法确认对方的身份。\n\n局部诊断足以继续探索。完整授权可在此撤销；核心隔离会强制切断该权限。",23);if(Action(new Rect(310,585,450,58),"保留锁定 / 撤销授权")){SetExitPermission(false);panel="terminal";}if(Action(new Rect(845,585,450,58),"确认开放完整出口权限")){if(SetExitPermission(true))Notice("完整权限已开放，调查军用质量请求");panel="terminal";}}
  void DrawCore(){RegionView?.DrawCoreView();}
  void DrawDetonate(){Paragraph(new Rect(275,240,1040,240),"确认前可返回营地补给并保存。\n\n已切断现实耦合、锁定安全锚点、检查全部生命维持舱。\n启动后有45秒返回信标跃迁，装置将永久失效。\n未及时撤离会恢复“引爆前”检查点。",23);if(Action(new Rect(350,596,400,65),"启动核心崩解",SavingReady&&State.Has("bomb-installed")))BeginDetonation();if(Action(new Rect(870,596,400,65),"暂不引爆"))panel="core";}
  void DrawRemain(){Paragraph(new Rect(270,238,1040,225),"爆炸亮起时，你回到了现实。背后的裂隙熄灭，D4与军用中继不复存在。\n\n污染源停止了，但荒原尚未恢复。\n飞船剩余设备可以改建为固定生态站。",23);if(Action(new Rect(400,583,800,65),"留下，将设备改造成生态站")){State.Flag("ecology-chosen");Changed();Close();}}
  void DrawEnding(){string text=panel=="escaped"?(State.Knows("J06")?"你驶离了星球，带走了调查记录。\n你知道裂隙后还留着什么，但选择继续自己的航程。\n异空间的未来仍未确定。":"飞船离开了荒原。\n你保住了自己找到的记录。\n远处，微弱的异常信号渐渐消失在导航屏外。") :"数月后。\n净水设施重新工作，第一簇幼苗扎下了根。\n这片星球不会一夜复苏，而你选择留下，继续修复。";Paragraph(new Rect(270,240,1060,270),text,26);if(Action(new Rect(380,597,370,58),"继续结局前的探索"))StartCoroutine(Restore(panel=="escaped"?"before-flight":"before-core"));if(Action(new Rect(870,597,370,58),"查看手账"))panel="journal";}
  void DrawSystem(){Paragraph(new Rect(245,226,1070,96),"自动保存 + F5 手动保存。旧试玩的虫洞记录独立保留，新故事使用统一存档。\n绑定装备、知识、安装结构和回收箱跨读档保留。",18);if(Action(new Rect(250,371,430,50),"保存当前进度")){if(Save())Notice("已保存");}if(Action(new Rect(870,371,430,50),"读取当前存档"))StartCoroutine(Restore("campaign"));if(Action(new Rect(250,461,430,50),"恢复起飞前",System.IO.File.Exists(Store.PathFor("before-flight"))))StartCoroutine(Restore("before-flight"));if(Action(new Rect(870,461,430,50),"恢复引爆前",System.IO.File.Exists(Store.PathFor("before-core"))))StartCoroutine(Restore("before-core"));if(Action(new Rect(510,626,570,50),"新故事（保留上一轮备份）"))panel="new-game";}
  void DrawNewGame(){Paragraph(new Rect(310,265,980,180),"会从迫降重新开始。当前故事先保存到独立备份，正常自动存档将用于新故事。\n\n新故事从手枪12发装填、24发备用与2份应急燃料开始。",23);if(Action(new Rect(380,580,360,60),"确认开始"))StartCoroutine(NewGame());if(Action(new Rect(860,580,360,60),"返回"))panel="system";}
  void DrawJournal(){
   string[] pages={"当前目标","已获线索","已证实事实","物种与矿物","修复清单","失落星界","余烬重建"};for(int i=0;i<pages.Length;i++)if(Action(new Rect(195+i*176,190,166,42),pages[i]))tab=i;
   if(tab==0){Paragraph(new Rect(250,281,1100,220),Objective+"\n\n矿井深度："+State.unlockedLayer+" / 5\n已发现区域："+State.coordinates.Count+"\n飞船修复："+State.repairs.Count+" / 4",24);if(Action(new Rect(1020,635,300,50),"存档与检查点"))panel="system";}
   if(tab==1){for(int i=0;i<State.journal.Count;i++){var r=State.journal[i];if(Action(new Rect(200,250+i*51,305,43),r.id+" · "+CampaignJournal.Title(r.id)))selectedJournal=i;}if(State.journal.Count>0){selectedJournal=Mathf.Clamp(selectedJournal,0,State.journal.Count-1);var r=State.journal[selectedJournal];Caption(new Rect(565,260,750,50),CampaignJournal.Title(r.id),25);Paragraph(new Rect(565,330,750,320),CampaignJournal.Text(r.id),21);Caption(new Rect(565,693,750,32),CampaignRules.Regions[r.region]+" · "+r.acquired+" UTC",13);}else Paragraph(new Rect(260,285,1100,100),"找到遗骸中的探险者手账后，可以记录线索。");}
   if(tab==2){int i=0;foreach(var id in new[]{"J04","J06","J07"}){Paragraph(new Rect(260,278+i*133,1090,105),State.Knows(id)?CampaignJournal.Fact(id):"未证实 · 继续调查区域档案",22);i++;}}
   if(tab==3){Paragraph(new Rect(245,270,1130,340),"紫铜矿：已识别，原矿用于弹药；金属用于配电。\n"+(State.Has("identified-0")?"陨铁：结构金属。\n":"")+(State.Has("identified-1")?"赤辉铜：与紫铜不同的能源材料。\n":"")+(State.Has("identified-2")?"霜钴：耐热与冷却。\n":"")+(State.Has("identified-3")?"星髓：导航稳定材料。\n":"")+(State.unlockedLayer==5?"稳定异晶：D4结晶，直接使用，不参与锻造。\n":"")+(State.Knows("J04")?"地龙与飞龙：变异本土生物。D4污染改变了它们的生理与行为。":"物种来源尚未查明。"),22);}
   if(tab==4){for(int i=0;i<4;i++)Paragraph(new Rect(235,271+i*100,1150,80),CampaignRules.RepairNames[i]+" · "+(State.repairs.Contains(i.ToString())?"已安装":Cost(CampaignRules.RepairCost(i))),20);}
   if(tab==5)DrawExpansionJournal();
   if(tab==6){if(Action(new Rect(450,355,700,60),"建设、委托与野外记录"))panel="frontier";Paragraph(new Rect(450,447,700,90),"可选成长不影响修船与结局。在营地按 C 打开整备台。",21);}
  }
 }
 public static class CampaignJournal {
  public static string Title(string id){switch(id){case "J01":return "第七次回收";case "J02":return "现实端锚点";case "J03":return "撤离战场";case "J04":return "裂隙与变异";case "J05":return "超额通道";case "J06":return "门后的舰队";case "J07":return "隔离方案";default:return "种源库";}}
  public static string Fact(string id)=>id=="J04"?"D4武器导致空间撕裂，并使本土物种变异。":id=="J06"?"远征议会发动战争；残党正准备夺取现实坐标，携D4出逃。":"切断现实端耦合后，核心坍缩可局限于相连的异空间。";
  public static string Text(string id){switch(id){case "J01":return "第七次回收。那块紫铜的裂纹里有一片不属于这里的天空，里面的云向相反方向移动。\n\n跃迁器还能运行，现实端却没有回应。我把维修档案和弹药配方锁进货舱。请先给自己留一条回去的路。\n\n远处广播使用同盟频率，却答不上撤离口令。不要把仍有人说话，误当成战争已经结束。";case "J02":return "装置仅支持有限质量的个人往返。现实位置已写入安全锚点。\n普通碎片记录内部坐标，无法从内部开启稳定出口。完整出口权限需要持有者明确授权。";case "J03":return "断裂道路旁保留着撤离编号。战争末期，同盟试图疏散平民，撤离队列却被空间异常截断。\n\n救援站信号：请修复中继，我们可以一起回家。\n此联络人的身份尚未证实。";case "J04":return Fact(id)+"\n\n医疗报告与附带裂隙实验吻合：能量谱与D4武器一致；地龙组织与灾前本土物种高度同源。它们仍表现出护群、筑巢和避噪行为。";case "J05":return "中继的目标质量远超平民撤离需求，足以输送大型舰船。\n\n请求字段包含现实端出口权限。局部诊断不必开放完整权限。完整授权必须由持有者明确确认，并可在医疗终端撤销。";case "J06":return Fact(id)+"\n\n军令、舰队质量记录与D4原型维护档案彼此印证。所谓救援站属于远征议会军需系统，正在利用你的校准数据打开军用航道。";case "J07":return Fact(id)+"\n\n异晶炸弹必须安装在共用核心，先切断现实耦合，再锁定撤离锚点。所有区域将依次坍缩，便携装置随后永久失效。启动前必须核查并提供撤离窗口。";default:return "同盟种源库保存着第一批耐污染植物。净水与污染控制恢复后，它们可能重新扎根。\n生态需要长期修复，不会在爆炸后立即复原。";}}
 }
}
