using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Starfall {
 public sealed partial class CampaignRuntime {
  int guidanceCategory,guidanceHelpLevel;string guidanceSelected="";Vector2 guidanceListScroll,guidanceDescriptionScroll;
  void DrawGuidanceSettings(Rect r){string[] names={"关闭","简洁","详细"};if(Action(r,"任务指引 · "+names[GuidanceDetail])){GuidanceDetail=(GuidanceDetail+1)%3;InvalidateGuidance();}}
  void DrawGuidanceHud(){
   if(GuidanceDetail==0)return;var g=CurrentGuidance;var n=GetGuidanceNavigation();float height=GuidanceDetail==2?152:78;
   GUI.color=new Color(.015f,.035f,.045f,.82f);GUI.DrawTexture(new Rect(35,82,485,height),Texture2D.whiteTexture);GUI.color=Color.white;
   Caption(new Rect(48,87,458,27),g.title+(g.total>0?"   "+g.current+" / "+g.total:""),17,new Color(.95f,.81f,.52f));
   Paragraph(new Rect(48,116,455,40),g.instruction,15);
   if(GuidanceDetail==2){Caption(new Rect(48,159,455,25),n.available?n.label+(!n.screen?" · 直线约 "+Mathf.CeilToInt(n.distance)+" 米":""):g.complete?"J 选择其他任务":"查看 J 手账中的前置条件",14,new Color(.54f,.84f,.90f));Paragraph(new Rect(48,185,455,42),!string.IsNullOrEmpty(n.problem)?n.problem:!string.IsNullOrEmpty(g.blockedReason)?g.blockedReason+"；"+n.instruction:n.instruction,13);}
   if(director.underground&&!Explorer&&Action(new Rect(44,239,190,29),"选择矿层",director.hookState==SceneDirector.HookState.Swinging&&!director.Transitioning))Open("layers");
   if(!g.complete&&n.available&&string.IsNullOrEmpty(n.problem))DrawGuidanceMarker(n);
   if(GuidanceDetail==2&&Time.unscaledTime<guidanceTutorialUntil)Paragraph(new Rect(46,282,450,70),guidanceTutorial,14);
  }
  void DrawGuidanceMarker(GuidanceNavigation n){
   var cam=Explorer?Explorer.viewCamera:director.worldCamera;if(!cam)return;Vector2 p;bool edge=false;
   if(n.screen)p=n.screenPoint;else{var v=cam.WorldToViewportPoint(n.point+Vector3.up*(Explorer?1.0f:.6f));p=new Vector2(v.x*1600,(1-v.y)*900);if(v.z<=0){p=new Vector2(1600-p.x,900-p.y);edge=true;}if(p.x<28||p.x>1572||p.y<55||p.y>735)edge=true;p.x=Mathf.Clamp(p.x,28,1572);p.y=Mathf.Clamp(p.y,55,735);}
   if(p.x<530&&p.y>75&&p.y<275)p.x=540;
   string glyph=edge?(p.x<100?"◀":p.x>1500?"▶":p.y<150?"▲":"▼"):"◇";
   Caption(new Rect(p.x-18,p.y-19,36,38),glyph,26,new Color(1,.81f,.35f),TextAnchor.MiddleCenter);
   float x=Mathf.Clamp(p.x-118,22,1342),y=p.y+20;if(Mathf.Abs(p.x-800)<80&&Mathf.Abs(p.y-450)<80){x=p.x+42;y=p.y-15;}
   GUI.color=new Color(.02f,.035f,.04f,.8f);GUI.DrawTexture(new Rect(x,y,236,29),Texture2D.whiteTexture);GUI.color=Color.white;Caption(new Rect(x+5,y,226,29),edge?"转向 · "+n.label:n.label,13,new Color(1,.86f,.55f),TextAnchor.MiddleCenter);
  }
  void DrawGuidanceJournal(){
   if(Action(new Rect(1175,124,135,35),"存档 / 检查点"))panel="system";
   string[] categories={"主线 / 修船","伙伴支线","星界探索","建设 / 制作","长期委托","物资回收"};for(int i=0;i<categories.Length;i++)if(Action(new Rect(200+i*199,247,188,35),categories[i])){guidanceCategory=i;guidanceSelected="";guidanceListScroll=guidanceDescriptionScroll=Vector2.zero;guidanceHelpLevel=0;}
   var context=GetGuidanceContext();var keys=CampaignGuidance.TaskKeys(State,context,guidanceCategory);
   if(!keys.Contains(guidanceSelected))guidanceSelected=keys.Contains(State.trackedObjective)?State.trackedObjective:keys.Count>0?keys[0]:"";
   guidanceListScroll=GUI.BeginScrollView(new Rect(200,300,370,390),guidanceListScroll,new Rect(0,0,343,Mathf.Max(386,keys.Count*56)));
   for(int i=0;i<keys.Count;i++){var task=CampaignGuidance.Describe(State,context,keys[i]);if(task==null)continue;string mark=task.complete?"✓ ":task.claimable?"待领取 · ":keys[i]==State.trackedObjective?"▶ ":"";if(Action(new Rect(0,i*56,340,48),mark+task.title)){guidanceSelected=keys[i];guidanceDescriptionScroll=Vector2.zero;guidanceHelpLevel=0;}}
   GUI.EndScrollView();
   var selected=CampaignGuidance.Describe(State,context,guidanceSelected);
   if(selected==null){Paragraph(new Rect(625,330,690,150),guidanceCategory==5?"没有待回收物资。死亡后可在这里追踪回收箱；也能在营地转运取回。":"进入新区域后，这里会登记可进行的任务。先跟随主线取得区域坐标。",21);}
   else{
    Caption(new Rect(610,302,720,35),selected.title+" · "+(selected.complete?"已完成":selected.claimable?"待领取":!string.IsNullOrEmpty(selected.blockedReason)?"缺少前置":"可进行"),23);
    string body=GuidanceDescription(selected);float contentHeight=Mathf.Max(340,body.Split('\n').Length*29+180);
    guidanceDescriptionScroll=GUI.BeginScrollView(new Rect(605,349,745,331),guidanceDescriptionScroll,new Rect(0,0,710,contentHeight));Paragraph(new Rect(3,0,700,contentHeight),body,18);GUI.EndScrollView();
    if(Action(new Rect(610,696,225,38),selected.complete?"任务已完成":"追踪此任务",!selected.complete)){TrackObjective(guidanceSelected);guidanceHelpLevel=0;}
    if(Action(new Rect(850,696,225,38),"返回主线追踪"))TrackObjective("");
    if(Action(new Rect(1090,696,245,38),selected.stepId=="ending:remain"?"决定是否留下":"查看详细提示")){if(selected.stepId=="ending:remain")Open("remain");else{guidanceHelpLevel=Mathf.Min(3,guidanceHelpLevel+1);InvalidateGuidance();}}
   }
   if(Action(new Rect(205,699,355,34),"查看全部操作说明"))panel="guide-help";
  }
  void PrepareGuidanceJournal(){tab=0;string key=CurrentGuidance.key;if(guidanceSelected!=key)guidanceHelpLevel=0;guidanceCategory=key.StartsWith("quest:")?1:key.StartsWith("relay:")||key.StartsWith("survey")||key.StartsWith("ruin")||key=="relics"?2:key.StartsWith("build:")||key.StartsWith("module:")||key.StartsWith("craft:")?3:key.StartsWith("commission:")?4:key.StartsWith("crate:")?5:0;guidanceSelected=key;guidanceDescriptionScroll=guidanceListScroll=Vector2.zero;}
  string GuidanceDescription(GuidanceStep g){
   var text=new StringBuilder();text.AppendLine("当前步骤："+g.instruction);if(!string.IsNullOrEmpty(g.blockedReason))text.AppendLine("前置条件："+g.blockedReason);text.AppendLine("位置："+CampaignRules.Regions[Mathf.Clamp(g.region,0,5)]+(g.layer>0?" · 第 "+g.layer+" 层":""));text.AppendLine("操作："+g.action);if(g.total>0)text.AppendLine("进度："+g.current+" / "+g.total);if(!string.IsNullOrEmpty(g.detail))text.AppendLine(g.detail);
   text.AppendLine();AppendGuidanceChecklist(text,g.key);
   if(g.materials!=null&&g.materials.Length>0){text.AppendLine("所需材料（背包与仓库可直接使用）：");foreach(var cost in g.materials){int bag=State.bag.Count(cost.item),warehouse=State.warehouse.Count(cost.item),recover=0;foreach(var box in State.crates)recover+=box.items.Count(cost.item);text.AppendLine(CampaignRules.ItemNames[(int)cost.item]+"：需要 "+cost.count+" · 背包 "+bag+" · 仓库 "+warehouse+" · 仍缺 "+Mathf.Max(0,cost.count-bag-warehouse)+(recover>0?" · 待回收 "+recover:""));}text.AppendLine("原矿需先在营地锻造。导航会按当前缺料选择矿层、区域补给或可用的应急回收。");}
   if(g.key.StartsWith("commission:")){var d=Array.Find(GetGuidanceContext().commissions,x=>x.id==g.key.Substring(11));if(d!=null&&d.reward!=null)text.AppendLine(RewardNames(d.reward));}
   if(guidanceHelpLevel>0){text.AppendLine();text.AppendLine("详细帮助：");text.AppendLine(GuidancePuzzleHelp(g));}
   return text.ToString();
  }
  void AppendGuidanceChecklist(StringBuilder b,string key){
   Action<bool,string> row=(done,name)=>b.AppendLine((done?"✓ ":"□ ")+name);
   if(key.StartsWith("quest:")&&int.TryParse(key.Substring(6),out int q)){
    if(q==0)for(int i=0;i<3;i++)row(Exp("antenna-"+i),"接通天线 "+(i+1));
    if(q==1){row(ScanUnlocked,"修复拾光并解锁扫描");for(int i=0;i<3;i++)row(Exp("signal-"+i),CampaignRules.Regions[i+2]+"异常信号");}
    if(q==2){row(Exp("winch"),"解除避难舱绞盘");row(Exp("engineer-rescued"),"打开避难舱救出岚");}
    if(q==3){row(Exp("engineer-rescued"),"救出岚");row(Exp("crane-restored"),"恢复船坞吊机");}
    if(q==4){row(Exp("power"),"恢复医疗站电源");row(Exp("doctor-rescued"),"解除隔离，救出苏禾");}
    if(q==5){row(Exp("doctor-rescued"),"救出苏禾");for(int i=0;i<3;i++)row(Exp("sample-"+i),(i==2?"船坞":"医疗站")+"样本 "+(i+1));}
   }else if(key=="main:escape"){row(State.device,"取得遗骸货舱图纸与装置");row(State.calibrated,"返回信标校准");for(int i=0;i<4;i++)row(State.repairs.Contains(i.ToString()),CampaignRules.RepairNames[i]);row(State.Has("ship-navigation"),"校准飞船导航");row(State.Has("ship-fueled"),"装填八份燃料");row(State.escapeComplete,"主动确认起飞");}
   else if(key=="main:investigate"){string[] ids={"J04","J06","J07"};for(int i=0;i<3;i++)row(State.Knows(ids[i]),"调查 "+CampaignRules.Regions[i+3]+" 档案");if(State.Knows("J07")){row(State.Has("bomb-crafted"),"制作异晶炸弹");row(State.Has("boss-defeated"),"击败守核机甲");row(State.Has("life-support-checked"),"核查生命维持舱");row(State.Has("coupling-cut"),"切断现实耦合");row(State.Has("evac-anchor"),"锁定撤离锚点");row(State.Has("bomb-installed"),"安装炸弹并主动确认引爆");row(State.Has("rift-destroyed"),"及时返回入口信标跃迁");}}
   else if(key=="relics"){for(int i=1;i<=5;i++)row(Exp("relic-"+i),CampaignRules.Regions[i]+"遗物");row(Exp("atlas-restored"),"回营地复原归航星图");}
   else if(key=="surveys")for(int i=1;i<=5;i++)row(Exp("survey-"+i),CampaignRules.Regions[i]+"勘测");
   else if(key=="ruins")for(int i=2;i<=4;i++)row(Exp("ruin-"+i),CampaignRules.Regions[i]+"遗迹");
  }
  string GuidancePuzzleHelp(GuidanceStep g){
   if(g.stepId.StartsWith("ruin:")){
    int r=g.region;var def=Array.Find(GetGuidanceContext().ruins,x=>x.region==r);if(def==null)return "先取得该区域坐标并进入遗迹。";
    if(guidanceHelpLevel==1)return r==2?"站到控制器旁，对照每座控制器后方的实体箭头；按 E 将前方箭头转到相同方向。":r==3?"靠近播放器按 E，等灯光播放结束，再按同样顺序触碰四种结晶；错误时会自动重新播放。":"观察每座电容背后的亮格，按 E 让前方填充高度与它对应。";
    if(r==3){string[] colors={"蓝色","橙色","紫色","绿色"};int step=RegionView&&State.region==3?RegionView.MemoryStep:0;if(RegionView&&State.region==3&&RegionView.MemoryPlaying)return "先等待结晶灯序列播放结束，再输入。";int next=def.solution[Mathf.Clamp(step,0,def.solution.Length-1)];return "下一步触碰 "+colors[next]+"结晶（从左数第 "+(next+1)+" 座）。导航会标记当前需要的结晶。";}
    var record=State.ruins.Find(x=>x.region==r);for(int i=0;i<3;i++){int current=record==null?0:record.values[i],steps=(def.solution[i]-current+4)%4;if(steps>0)return "操作从左数第 "+(i+1)+" 座控制器。"+(guidanceHelpLevel>=3?"按 E "+steps+" 次，使其与后方线索一致。":"观察它后方的目标指示；再次点击详细提示可查看剩余操作次数。");}return "本处遗迹已完成。";
   }
   return g.detail+"\n"+g.action+"\n若目标在其他区域，先跟随箭头返回信标，再从星界坐标入口选择目标区域。任务区会说明下一步。";
  }
  internal string GuidanceActiveTarget(GuidanceStep g){
   if(guidanceHelpLevel<2||guidanceSelected!=g.key||!g.stepId.StartsWith("ruin:")||g.region!=State.region||!RegionView)return g.targetKey;
   var def=Array.Find(GetGuidanceContext().ruins,x=>x.region==g.region);if(def==null)return g.targetKey;
   if(g.region==3)return RegionView.MemoryPlaying?"ruin-replay":"ruin-control-"+def.solution[Mathf.Clamp(RegionView.MemoryStep,0,def.solution.Length-1)];
   var record=State.ruins.Find(x=>x.region==g.region);for(int i=0;i<3;i++)if((record==null?0:record.values[i])!=def.solution[i])return "ruin-control-"+i;return g.targetKey;
  }
  void DrawGuidanceHelp(){
   Paragraph(new Rect(230,215,1140,455),"任务：跟随左上角当前步骤；J 选择任务、查看前置条件和详细提示。\n\n地表：A / D 移动，靠近目标按 E。地下：空格下放抓钩；顶部切换矿层或启动钻机，右下角返回地表。\n\n星界：WASD 移动，鼠标观察，Shift 奔跑，空格跳跃，E 交互或长按开采；勘测需要长按 E 三秒。\n\n战斗：左键射击，右键近战，R 换弹，1/2/3 切枪，左 Ctrl 闪避。解锁后 Q 扫描、F 震荡脉冲。G 爆裂芯，T 声纹诱饵，必须携带对应道具。\n\n整备：B 背包，手动选择急救剂使用；营地 E 开制作、仓库、锻造和回收，C 建设与委托。\n\n锻造：空格开始与落锤，连续三次成功产出金属。死亡：普通背包留在回收箱，营地可转运取回。F5 保存，Esc 关闭或暂停。",20);
   DrawGuidanceSettings(new Rect(250,698,340,42));if(Action(new Rect(900,698,410,42),"返回任务列表"))panel="journal";
  }
 }
}
