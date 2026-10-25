using System;
using System.Collections.Generic;

namespace Starfall {
 public sealed class GuidanceStep {
  public string key="",title="",stepId="",instruction="",detail="",targetKey="",blockedReason="",action="";
  public int region,layer,current,total;public bool complete,claimable;public ItemStack[] materials=new ItemStack[0];
 }
 public sealed class GuidanceContext {
  public bool underground,bossWeakOpen;public int nodesRemaining=3,relayRegion,relayMask;public float bombSeconds=-1,relayRemaining;
  public FrontierCatalog.Project[] projects=new FrontierCatalog.Project[0];
  public FrontierCatalog.Commission[] commissions=new FrontierCatalog.Commission[0];
  public FrontierCatalog.Recipe[] recipes=new FrontierCatalog.Recipe[0];
  public FrontierCatalog.Ruin[] ruins=new FrontierCatalog.Ruin[0];
  public LostRealmCatalog.Module[] modules=new LostRealmCatalog.Module[0];
  public LostRealmCatalog.Relay[] relays=new LostRealmCatalog.Relay[0];
  public Func<ItemId,bool> canEmergency;
  public Func<int,ItemId,bool> hasRegionOre;
  public Func<int,bool> hasLivingEnemy;
 }
 public static class CampaignGuidance {
  static readonly string[] QuestNames={"拾光 · 修复天线","拾光 · 异常图谱","岚 · 战场营救","岚 · 船坞吊机","苏禾 · 医疗隔离","苏禾 · 生态样本"};
  static readonly string[] QuestFlags={"drone-rescued","signals-complete","engineer-rescued","crane-restored","doctor-rescued","greenhouse"};
  static readonly string[] EcologyFlags={"pollution-off","water-restored","seed-planted"};
  static readonly string[] EcologyNames={"关闭污染管道","修复净水设施","播下同盟种子"};
  static bool Exp(CampaignState s,string key)=>s.expansionProgress!=null&&s.expansionProgress.Contains(key);
  static int Count(CampaignState s,string prefix,int start,int end){int n=0;for(int i=start;i<end;i++)if(Exp(s,prefix+i))n++;return n;}
  static GuidanceStep Step(string key,string title,string id,int region,string target,string instruction,string action="按 E 交互",string detail="")=>new GuidanceStep{key=key,title=title,stepId=id,region=region,targetKey=target,instruction=instruction,action=action,detail=detail};
  static GuidanceStep Done(string key,string title){var g=Step(key,title,"done",0,"","任务已完成","J 查看其他任务");g.complete=true;return g;}
  public static GuidanceStep Resolve(CampaignState s,GuidanceContext c){
   if(c.bombSeconds>=0)return Step("emergency:evacuate","核心撤离","evacuate",5,"beacon","立即返回入口信标","靠近后按 E 跃迁","剩余 "+Math.Max(0,(int)Math.Ceiling(c.bombSeconds))+" 秒；超时或死亡会恢复引爆前检查点。");
   if(s.saviorComplete)return Done("main:investigate","生态站运行中");
   if(s.Has("ecology-chosen")){for(int i=0;i<3;i++)if(!s.Has(EcologyFlags[i]))return Step("main:investigate","修复地表生态","ecology:"+i,0,"ecology:"+i,EcologyNames[i],"按 E 后保持位置约三秒");return Done("main:investigate","生态站运行中");}
   if(s.Has("rift-destroyed"))return Step("main:investigate","裂隙已经熄灭","ending:remain",0,"camp","回到营地，决定是否留下","J 查看结局说明","返回后主动选择留下，再修复三处生态设施。");
   if(c.relayRemaining>0&&c.relayRegion>=2&&c.relayRegion<=4){int index=0;while(index<3&&(c.relayMask&(1<<index))!=0)index++;if(index<3){var relay=Step("relay:"+c.relayRegion,"中继同步进行中","relay-active:"+index,c.relayRegion,"relay:"+index,"接通未同步的中继 "+(index+1),"靠近后按 E 同步","剩余 "+(int)Math.Ceiling(c.relayRemaining)+" 秒；失败可重试，不扣材料。");relay.current=BitCount(c.relayMask);relay.total=3;return relay;}}
   var selected=Describe(s,c,s.trackedObjective);if(selected!=null&&!selected.complete)return selected;
   return Describe(s,c,s.guidanceRoute=="investigate"?"main:investigate":"main:escape");
  }
  static int BitCount(int value){int n=0;for(int i=0;i<3;i++)if((value&(1<<i))!=0)n++;return n;}
  public static GuidanceStep Describe(CampaignState s,GuidanceContext c,string key){return Describe(s,c,key,0);}
  static GuidanceStep Describe(CampaignState s,GuidanceContext c,string key,int depth){
   if(s==null||c==null||string.IsNullOrEmpty(key)||depth>10)return null;
   if(key=="main:escape"||key=="main:investigate")return Main(s,c,key,depth);
   if(key.StartsWith("quest:")&&int.TryParse(key.Substring(6),out int quest)&&quest>=0&&quest<6)return Quest(s,c,quest,depth);
   if(key.StartsWith("repair:")&&int.TryParse(key.Substring(7),out int part)&&part>=0&&part<4)return Repair(s,c,part,depth);
   if(key=="relics"){
    if(Exp(s,"atlas-restored"))return Done(key,"归航星图");int n=Count(s,"relic-",1,6);if(n==5){var atlas=Step(key,"复原归航星图","atlas",0,"atlas","回营地，在手账中复原星图","J → 失落星界 → 星界遗物 → 复原归航星图");atlas.current=5;atlas.total=5;return atlas;}
    int r=NextRegion(s,1,5,i=>!Exp(s,"relic-"+i));var g=Gate(s,c,Step(key,"收集星界遗物","relic:"+r,r,"relic-"+r,"寻找本区域未收集的遗物","靠近后按 E 收取","遗物不占背包；收齐五件后回营复原星图。"));g.current=n;g.total=5;return g;
   }
   if(key=="surveys"||key.StartsWith("survey:")){
    int r;if(key=="surveys"){r=NextRegion(s,1,5,i=>!Exp(s,"survey-"+i));if(r==0)return Done(key,"五区勘测");}else if(!int.TryParse(key.Substring(7),out r)||r<1||r>5)return null;
    if(Exp(s,"survey-"+r))return Done(key,"区域勘测");var g=Gate(s,c,Step(key,"星界勘测","survey:"+r,r,"survey","前往勘测仪，停留完成记录","靠近后长按 E 三秒","松开 E、移动或受伤会中断；暂停停止计时。"));g.current=Count(s,"survey-",1,6);g.total=5;return g;
   }
   if(key=="ruins"||key.StartsWith("ruin:")){
    int r;if(key=="ruins"){r=NextRegion(s,2,4,i=>!Exp(s,"ruin-"+i));if(r==0)return Done(key,"调查三处遗迹");}else if(!int.TryParse(key.Substring(5),out r)||r<2||r>4)return null;
    if(Exp(s,"ruin-"+r))return Done(key,"遗迹机关");var g=Gate(s,c,Step(key,r==2?"遗迹 · 方向锁":r==3?"遗迹 · 结晶记忆":"遗迹 · 余载配平","ruin:"+r,r,r==3?"ruin-replay":"ruin-control-0",r==2?"对照后方箭头，转动三个方向盘":r==3?"重放并观察四种结晶的亮起顺序":"对照背后的指示格，调整三座电容",r==3?"按 E 重放，再按相同顺序触碰结晶":"靠近每个控制器按 E 调节","J → 当前目标 → 查看详细提示，可逐步获得操作帮助。"));g.current=Count(s,"ruin-",2,5);g.total=3;return g;
   }
   if(key.StartsWith("relay:")&&int.TryParse(key.Substring(6),out int relayRegion)&&relayRegion>=2&&relayRegion<=4){
    if(Exp(s,"relay-complete-"+relayRegion))return Done(key,"回声中继");var d=Array.Find(c.relays,x=>x.region==relayRegion);return Gate(s,c,Step(key,d==null?"回声中继":d.title,"relay-start:"+relayRegion,relayRegion,"relay:0","先找到三座中继，再接通任意一座","靠近后按 E 开始限时同步","开启后在 "+(d==null?"限定":Math.Ceiling(d.seconds).ToString())+" 秒内接通另外两座；失败可重试。"));
   }
   if(key.StartsWith("commission:")){
    var d=Array.Find(c.commissions,x=>x.id==key.Substring(11));if(d==null)return null;if(Exp(s,"commission-"+d.id))return Done(key,d.title);var count=s.frontierCounters.Find(x=>x.id==d.eventId)?.count??0;
    if(count>=d.goal){var claim=Step(key,d.title,"claim:"+d.id,0,"camp",d.title+"目标已达成，回营地领取奖励","C → 长期委托 → 领取","背包放不下的奖励会依次送入仓库或营地回收箱。");claim.claimable=true;claim.current=count;claim.total=d.goal;return claim;}
    GuidanceStep g;if(d.eventId=="survey")g=Describe(s,c,"surveys",depth+1);else if(d.eventId=="ruin")g=Describe(s,c,"ruins",depth+1);else if(d.eventId=="forging")g=ForgeWork(s,c,depth+1);
    else if(d.eventId=="mining")g=Mining(s,c,null);else if(d.eventId=="variant"){int r=!s.fieldRecords.Contains("magnetic")?2:3;g=Gate(s,c,Step(key,d.title,"variant:"+r,r,"variant","击败尚未记录的龙类变体","观察地面预警，离开危险范围后反击"));}
    else g=CombatWork(s,c);
    return Wrap(g,key,d.title,count,d.goal);
   }
   if(key.StartsWith("build:")){
    var d=Array.Find(c.projects,x=>x.id==key.Substring(6));if(d==null)return null;if(Exp(s,"build-"+d.id))return Done(key,d.title);var need=Requirement(s,c,d.requirement,depth+1);var g=need??Materials(s,c,d.cost,depth+1)??Step(key,d.title,"build:"+d.id,0,"camp","回营地建设 "+d.title,"C → 营地建设 → 建设",d.effect);g.materials=d.cost??new ItemStack[0];return Wrap(g,key,d.title);
   }
   if(key.StartsWith("module:")){
    var d=Array.Find(c.modules,x=>x.id==key.Substring(7));if(d==null)return null;if(s.equippedModules.Contains(d.id))return Done(key,d.title);var need=Requirement(s,c,"engineer-rescued",depth+1)??Requirement(s,c,d.unlock,depth+1);var costs=s.craftedModules.Contains(d.id)?new ItemStack[0]:new[]{new ItemStack(ItemId.MachineParts,d.parts)};var g=need??Materials(s,c,costs,depth+1)??Step(key,d.title,"module:"+d.id,0,"camp",s.craftedModules.Contains(d.id)?"在工坊安装模块":"在工坊制作并安装模块","E → 岚的工坊 → 制作 / 安装","最多安装两个模块；槽位满时先卸下另一模块。");g.materials=costs;return Wrap(g,key,d.title);
   }
   if(key.StartsWith("craft:")){
    var d=Array.Find(c.recipes,x=>x.id==key.Substring(6));if(d==null)return null;if(s.bag.Count(d.result.item)>0)return Done(key,d.title);var g=Requirement(s,c,d.requirement,depth+1)??Materials(s,c,d.cost,depth+1)??Step(key,d.title,"craft:"+d.id,0,"camp","回营地制作并携带 "+d.title,"C → 营地建设 → 制作",d.id=="blast"?"第一人称按 G 投放；避开爆炸范围。":d.id=="decoy"?"第一人称按 T 投放，吸引视线内普通龙类。":"B 背包 → 选择急救剂 → 使用");g.materials=d.cost;return Wrap(g,key,d.title);
   }
   if(key.StartsWith("crate:")){var box=s.crates.Find(x=>x.id==key.Substring(6));return box==null?Done(key,"回收遗失物资"):Gate(s,c,Step(key,"回收遗失物资",key,box.region,key,"回到回收箱取回物资","靠近后按 E 回收","背包放不下时先清理；也可回营 E → 转运死亡箱，再逐箱取回。"));}
   return null;
  }
  static GuidanceStep Main(CampaignState s,GuidanceContext c,string key,int depth){
   if(s.saviorComplete||s.Has("ecology-chosen")||s.Has("rift-destroyed"))return Resolve(s,c);
   GuidanceStep g;
   if(!s.coordinates.Contains(1))g=Coordinate(s,c,1);
   else if(!s.device)g=Gate(s,c,Step(key,"调查星界遗骸","cargo",1,"chest","调查中央遗骸货舱","靠近后按 E → 解锁档案与跃迁装置","可绕行避开两只外围地龙，无需清怪。"));
   else if(!s.calibrated)g=Step(key,"校准跃迁装置","calibrate",1,"beacon","返回入口信标，校准并返回","靠近后按 E 校准并跃迁","完成后可从地表星界坐标入口前往已记录区域。");
   else if(key=="main:escape"){
    if(s.escapeComplete&&s.Has("escape-view"))return Done(key,"航程继续");int part=0;while(part<4&&s.repairs.Contains(part.ToString()))part++;
    if(part<4)g=Repair(s,c,part,depth+1);
    else if(!s.Has("ship-navigation"))g=Step(key,"修船离开","ship:navigation",0,"ship","校准飞船导航","靠近飞船按 E → 校准导航");
    else if(!s.Has("ship-fueled"))g=Materials(s,c,new[]{new ItemStack(ItemId.Fuel,8)},depth+1)??Step(key,"修船离开","ship:fuel",0,"ship","为飞船装填八份燃料","靠近飞船按 E → 装填燃料");
    else g=Step(key,"准备起飞","ship:confirm",0,"ship","在飞船终端确认起飞","E → 检查起飞条件 → 确认起飞","起飞结束本轮故事，保留起飞前检查点；不要求完成支线或核心战斗。");
   }else{
    string[] ids={"J04","J06","J07"};int[] areas={3,4,5};g=null;for(int i=0;i<ids.Length;i++)if(!s.Knows(ids[i])){g=Gate(s,c,Step(key,"追查异常档案","archive:"+ids[i],areas[i],"terminal","调查 "+CampaignRules.Regions[areas[i]]+" 的档案终端","按 E → 读取档案与领取补给","只提示已展开的调查；完整出口权限由玩家主动决定。"));break;}
    if(g==null&&!s.Has("bomb-crafted"))g=Materials(s,c,CampaignRules.BombCost,depth+1)??Step(key,"隔离核心","core:craft",0,"camp","在营地制作异晶炸弹","E → 制作 → 异晶炸弹","炸弹为受保护任务装备，不占普通背包。");
    if(g==null&&!s.Has("boss-defeated"))g=Gate(s,c,Step(key,"隔离核心",c.nodesRemaining>0?"core:nodes":"core:weak",5,c.nodesRemaining>0?"boss-node":"boss",c.nodesRemaining>0?"击破三处供能节点，解除核心护盾":c.bossWeakOpen?"攻击机甲开放的胸部弱点":"等待机甲胸部弱点开放",c.nodesRemaining>0?"左键射击或右键近战破坏节点":c.bossWeakOpen?"瞄准发光弱点攻击":"等待弱点开放，躲避地面预警","节点与开放弱点均可近战攻击；无弹药也能完成战斗。"));
    if(g==null){string[] flags={"life-support-checked","coupling-cut","evac-anchor"};string[] labels={"核查生命维持舱并广播撤离","切断现实端能量耦合","锁定现实安全锚点"};for(int i=0;i<3;i++)if(!s.Has(flags[i])){g=Step(key,"隔离核心","core:"+flags[i],5,"core",labels[i],"按 E 打开核心控制台 → "+labels[i]);break;}}
    if(g==null)g=Step(key,"隔离核心",s.Has("bomb-installed")?"core:confirm":"core:install",5,"core",s.Has("bomb-installed")?"检查撤离路线后主动确认引爆":"在控制台安装异晶炸弹",s.Has("bomb-installed")?"E → 检查倒计时与撤离 → 启动核心崩解":"E → 安装异晶炸弹","引爆后有 45 秒返回入口信标；可暂不引爆并先补给。");
   }
   if(key=="main:escape"&&s.repairs.Count==4&&!s.Has("ship-fueled"))g.materials=new[]{new ItemStack(ItemId.Fuel,8)};
   if(key=="main:investigate"&&s.Knows("J04")&&s.Knows("J06")&&s.Knows("J07")&&!s.Has("bomb-crafted"))g.materials=CampaignRules.BombCost;
   return Wrap(g,key,key=="main:escape"?"主线 · 修船离开":"主线 · 追查异常");
  }
  static GuidanceStep Quest(CampaignState s,GuidanceContext c,int q,int depth){
   string key="quest:"+q,title=QuestNames[q];if(Exp(s,QuestFlags[q]))return Done(key,title);GuidanceStep g;
   if(q==0){int i=0;while(i<3&&Exp(s,"antenna-"+i))i++;if(i==3)return Done(key,title);g=Step(key,title,"antenna:"+i,1,"antenna-"+i,"接通未修复的天线 "+(i+1),"靠近后按 E 接通","三段天线完成后拾光会回营，解锁 Q 扫描。");g.current=Count(s,"antenna-",0,3);g.total=3;}
   else if(q==1){if(!Exp(s,"drone-rescued"))return Wrap(Quest(s,c,0,depth+1),key,title,0,3,"先修复拾光的三段天线");int i=0;while(i<3&&Exp(s,"signal-"+i))i++;if(i==3)return Done(key,title);g=Step(key,title,"signal:"+i,i+2,"signal-"+i,"记录区域异常信号","靠近后按 E 记录","三处信号分别在战场、医疗站、船坞；完成后解锁猎手镜组。");g.current=Count(s,"signal-",0,3);g.total=3;}
   else if(q==2)g=Step(key,title,Exp(s,"winch")?"shelter":"winch",2,Exp(s,"winch")?"shelter":"winch",Exp(s,"winch")?"前往避难舱，救出岚":"解除锁住避难舱的绞盘","靠近后按 E "+(Exp(s,"winch")?"开启避难舱":"解除绞盘"),"岚通过信标回营，无需护送；回营后可打开工坊。");
   else if(q==3){if(!Exp(s,"engineer-rescued"))return Wrap(Quest(s,c,2,depth+1),key,title,0,1,"需要先救出工程师岚");g=Step(key,title,"crane",4,"crane","恢复船坞吊机","靠近后按 E 恢复","吊机开启货运捷径并解锁磁轨枪；1/2/3 切换武器。");}
   else if(q==4)g=Step(key,title,Exp(s,"power")?"medical-door":"power",3,Exp(s,"power")?"medical-door":"power",Exp(s,"power")?"解除隔离，救出苏禾":"重接医疗站电源","靠近后按 E "+(Exp(s,"power")?"解除隔离":"恢复供电"),"苏禾回营后提供三秒治疗，无需护送。");
   else {if(!Exp(s,"doctor-rescued"))return Wrap(Quest(s,c,4,depth+1),key,title,0,3,"先恢复电源并救出苏禾");int i=0;while(i<3&&Exp(s,"sample-"+i))i++;if(i==3)return Done(key,title);g=Step(key,title,"sample:"+i,i==2?4:3,"sample-"+i,"封存未采集的生态样本","靠近后按 E 采集","医疗站两份、船坞一份；完成后营地温室启用。");g.current=Count(s,"sample-",0,3);g.total=3;}
   return Gate(s,c,g);
  }
  static GuidanceStep Repair(CampaignState s,GuidanceContext c,int part,int depth){
   string key="repair:"+part,title="维修 · "+CampaignRules.RepairNames[part];if(s.repairs.Contains(part.ToString()))return Done(key,title);
   var costs=CampaignRules.RepairCost(part);GuidanceStep g;if(!s.Has("repair-plan")&&!s.device)g=Main(s,c,"main:escape",depth+1);else if(part==3&&s.unlockedLayer<5)g=Coordinate(s,c,5);else g=Materials(s,c,costs,depth+1)??Step(key,title,"repair:"+part,0,"ship","安装 "+CampaignRules.RepairNames[part],"靠近飞船按 E → 对应维修项 → 安装");g.materials=costs;return Wrap(g,key,title);
  }
  static GuidanceStep Requirement(CampaignState s,GuidanceContext c,string flag,int depth){
   if(string.IsNullOrEmpty(flag)||Exp(s,flag))return null;for(int i=0;i<QuestFlags.Length;i++)if(flag==QuestFlags[i]){var q=Quest(s,c,i,depth+1);q.blockedReason="先完成 "+QuestNames[i];return q;}
   if(flag.StartsWith("build-"))return Describe(s,c,"build:"+flag.Substring(6),depth+1);return Step("","解锁条件","requirement:"+flag,0,"camp","先完成所需伙伴或设施条件","J 查看相关任务",flag);
  }
  static GuidanceStep Materials(CampaignState s,GuidanceContext c,ItemStack[] costs,int depth){
   if(costs==null)return null;foreach(var cost in costs){int owned=s.bag.Count(cost.item)+s.warehouse.Count(cost.item);if(owned>=cost.count)continue;
    var recovered=RecoverMaterial(s,cost.item);if(recovered!=null)return recovered;
    var item=cost.item;if((int)item>=5&&(int)item<=9){var raw=(ItemId)((int)item-5);if(s.bag.Count(raw)+s.warehouse.Count(raw)>0)return Step("","锻造所需金属","forge:"+(int)raw,0,"camp","将 "+CampaignRules.ItemNames[(int)raw]+" 锻造成金属","E → 锻造 → 选择原矿 → 空格开始与落锤","连续三次成功得到一锭；当前仍缺 "+(cost.count-owned)+" 份成品。失败后可继续收集原料。");var lostRaw=RecoverMaterial(s,raw);if(lostRaw!=null)return lostRaw;item=raw;}
    if(item==ItemId.MachineParts)return ComponentWork(s,c,depth+1);
    if(item==ItemId.Fuel){foreach(int r in s.coordinates)if(!s.Has("supply-fuel-"+r))return Step("","补充燃料","fuel:"+r,r,r==1?"chest":"terminal","领取区域固定燃料补给",r==1?"E → 取出剩余补给":"E → 读取档案与领取补给","背包需留出空间；未取出的固定补给不会消失。");}
    if(c.canEmergency!=null&&c.canEmergency(item))return Step("","补充主线材料","emergency:"+(int)item,0,"camp","从营地应急回收补充 "+CampaignRules.ItemNames[(int)item],"E → 制作与应急回收 → 选择材料并等待八秒","应急回收条件由当前实际物资与矿层决定。");
    return Mining(s,c,item);
   }return null;
  }
  static GuidanceStep RecoverMaterial(CampaignState s,ItemId item){foreach(var box in s.crates)if(box.items.Count(item)>0)return Step("","回收缺少的材料","material-crate:"+box.id,box.region,"crate:"+box.id,"回收 "+CampaignRules.ItemNames[(int)item],"靠近后按 E 回收；也可在营地转运死亡箱","回收箱内材料尚不能直接用于制作。");return null;}
  static GuidanceStep ForgeWork(CampaignState s,GuidanceContext c,int depth){
   for(int i=0;i<5;i++){var raw=(ItemId)i;if(s.bag.Count(raw)+s.warehouse.Count(raw)>0){var g=Step("","成功锻造任意金属","forge:"+i,0,"camp","在营地锻造 "+CampaignRules.ItemNames[i],"E → 锻造 → 选择该原矿 → 空格开始及落锤","任一种原矿成功锻造都计入委托；三次落锤命中产出一锭，失败不计入。");g.materials=new[]{new ItemStack(raw,1)};return g;}}
   for(int i=0;i<5;i++){var lost=RecoverMaterial(s,(ItemId)i);if(lost!=null)return lost;}
   for(int i=0;i<5;i++){var next=Materials(s,c,new[]{new ItemStack((ItemId)i,1)},depth+1);if(next!=null&&next.stepId!="material:help")return next;}
   return Step("","取得锻造原料","forge:help",0,"camp","查看可回收原矿与营地应急回收","E → 制作与应急回收","已知可用原矿不足；成品金属不能再次计为成功锻造。");
  }
  static GuidanceStep ComponentWork(CampaignState s,GuidanceContext c,int depth){
   for(int r=2;r<=4;r++)if(s.coordinates.Contains(r))for(int i=0;i<2;i++)if(!Exp(s,"cache-"+r+"-"+i))return Step("","收集机械组件","parts:"+r+":"+i,r,"cache-"+r+"-"+i,"拆解物资箱，取得机械组件","靠近后按 E 拆解","每箱首次提供五份；委托、遗迹和可选精英也提供组件。");
   foreach(var d in c.commissions){if(Exp(s,"commission-"+d.id)||d.reward==null||!Array.Exists(d.reward,x=>x.item==ItemId.MachineParts))continue;int n=s.frontierCounters.Find(x=>x.id==d.eventId)?.count??0;if(n>=d.goal)return Describe(s,c,"commission:"+d.id,depth+1);}
   for(int r=2;r<=4;r++)if(s.coordinates.Contains(r)&&!Exp(s,"ruin-"+r))return Describe(s,c,"ruin:"+r,depth+1);
   foreach(var d in c.commissions)if(!Exp(s,"commission-"+d.id)&&d.eventId!="ruin"&&d.reward!=null&&Array.Exists(d.reward,x=>x.item==ItemId.MachineParts))return Describe(s,c,"commission:"+d.id,depth+1);
   for(int r=2;r<=4;r++)if(!s.coordinates.Contains(r))return Coordinate(s,c,r);
   if(!Exp(s,"elite-ground"))return Step("","收集机械组件","parts:elite-ground",2,"elite","击败战场可选精英，取得固定组件","诱导冲撞实体掩体，趁侧腹暴露反击");
   if(!Exp(s,"elite-wing"))return Step("","收集机械组件","parts:elite-wing",4,"elite","击败船坞可选精英，取得固定组件","保持移动躲避三段落雷，停栖时反击");
   var blocked=Step("","补充机械组件","parts:exhausted",0,"camp","核对营地回收箱与未回收的地下可疑矿团","J 查看物资回收与探索记录","已知箱子、遗迹、委托和精英奖励均已结算，避免反复寻找已领取的奖励。");blocked.blockedReason="当前已知固定组件来源已用尽";return blocked;
  }
  static GuidanceStep CombatWork(CampaignState s,GuidanceContext c){
   int region=0;if(s.region>=1&&s.region<=4&&(c.hasLivingEnemy==null||c.hasLivingEnemy(s.region)))region=s.region;
   if(region==0)for(int r=4;r>=1;r--)if(s.coordinates.Contains(r)&&(c.hasLivingEnemy==null||c.hasLivingEnemy(r))){region=r;break;}
   if(region==0){for(int r=2;r<=4;r++)if(!s.coordinates.Contains(r))return Coordinate(s,c,r);return Step("","检查战斗记录","combat:help",0,"camp","已知区域暂无可计入的存活目标","C 查看长期委托领取状态");}
   return Step("","击败独立目标","combat",region,"enemy","击败一个尚未计入的目标","左键射击；R 换弹；右键近战","普通战斗目标不重复计数；近战不消耗弹药，优先躲避预警。");
  }
  static GuidanceStep Mining(CampaignState s,GuidanceContext c,ItemId? item){
   int min=item.HasValue?CampaignRules.MinimumLayer(item.Value):1;if(s.unlockedLayer<min)return Coordinate(s,c,min);
   for(int l=1;l<=s.unlockedLayer;l++)for(int i=0;i<CampaignRules.NodeCount(l);i++)if((!item.HasValue||CampaignRules.NodeItem(l,i)==item.Value)&&!s.minedNodes.Contains(CampaignRules.NodeId(l,i))){var g=Step("","回收所需矿物","mine:"+l+":"+(item.HasValue?(int)item.Value:-1),0,c.underground?"ore:"+(item.HasValue?(int)item.Value:-1):"mine","前往第 "+l+" 层回收 "+(item.HasValue?CampaignRules.ItemNames[(int)item.Value]:"未开采矿点"),c.underground?"空格下放抓钩；顶部可切换已解锁矿层":"靠近中央矿井按 E 下井","B 查看背包；背包满时先回营存放。固定矿点全部回收后可启动钻机。");g.layer=l;return g;}
   if(item.HasValue)for(int r=2;r<=4;r++)if(s.coordinates.Contains(r)&&(c.hasRegionOre!=null?c.hasRegionOre(r,item.Value):HasSavedOre(s,r,item.Value)))return Step("","开采区域矿脉","vein:"+r+":"+(int)item.Value,r,"vein:"+(int)item.Value,"采集区域中的 "+CampaignRules.ItemNames[(int)item.Value],"靠近矿脉，长按 E 开采","矿脉耗尽后从导航候选移除。");
   if(!item.HasValue&&s.unlockedLayer<5)return Coordinate(s,c,s.unlockedLayer+1);
   return Step("","检查材料来源","material:help",0,"camp","回营检查回收箱、固定补给与应急回收","E → 制作与应急回收","可用固定矿点已采完；检查任务缺料和已掉落的物资，避免重复搜索空矿点。");
  }
  static bool HasSavedOre(CampaignState s,int r,ItemId item){if(r==2&&item!=ItemId.IronOre&&item!=ItemId.CopperOre||r==3&&item!=ItemId.CobaltOre||r==4&&item!=ItemId.StarOre)return false;var state=s.regions.Find(x=>x.id==r);for(int i=0;i<12;i++)if((r!=2||((i%2==0)==(item==ItemId.IronOre)))&&(state==null||!state.mined.Contains(1400+i)))return true;return false;}
  static GuidanceStep Coordinate(CampaignState s,GuidanceContext c,int desired){
   int l=Math.Min(desired,s.unlockedLayer);int collected=0;for(int i=0;i<CampaignRules.NodeCount(l);i++)if(s.minedNodes.Contains(CampaignRules.NodeId(l,i)))collected++;
   bool drill=collected==CampaignRules.NodeCount(l)&&l<desired;var g=Step("","取得区域坐标","coordinate:"+desired+":"+l,0,!c.underground?"mine":drill?"drill":"ore:-1",drill?"矿层已采完，启动钻机进入下一层":"在第 "+l+" 层回收矿物与裂纹矿物",!c.underground?"靠近中央矿井按 E 下井":drill?"点击顶部“启动钻机 ↓”":"空格下放抓钩；采完固定矿点后启动钻机","目标区域："+CampaignRules.Regions[desired]+"。坐标来自对应矿层的裂纹矿物。");g.layer=l;g.current=collected;g.total=CampaignRules.NodeCount(l);return g;
  }
  static GuidanceStep Gate(CampaignState s,GuidanceContext c,GuidanceStep g){if(g.region>0&&!s.coordinates.Contains(g.region))return Wrap(Coordinate(s,c,g.region),g.key,g.title,g.current,g.total,"尚未取得 "+CampaignRules.Regions[g.region]+" 坐标");return g;}
  static GuidanceStep Wrap(GuidanceStep g,string key,string title,int current=-1,int total=-1,string reason=null){if(g==null)return null;g.key=key;g.title=title;if(current>=0)g.current=current;if(total>=0)g.total=total;if(reason!=null)g.blockedReason=reason;return g;}
  static int NextRegion(CampaignState s,int first,int last,Func<int,bool> needed){if(s.region>=first&&s.region<=last&&s.coordinates.Contains(s.region)&&needed(s.region))return s.region;for(int i=first;i<=last;i++)if(s.coordinates.Contains(i)&&needed(i))return i;for(int i=first;i<=last;i++)if(needed(i))return i;return 0;}
  public static List<string> TaskKeys(CampaignState s,GuidanceContext c,int category){
   var keys=new List<string>();if(category==0){keys.Add("main:escape");if(s.Knows("J03")||s.Knows("J04")||s.Knows("J07"))keys.Add("main:investigate");for(int i=0;i<4;i++)if(s.device)keys.Add("repair:"+i);}
   if(category==1){int[] areas={1,2,2,4,3,3};for(int i=0;i<6;i++)if(s.coordinates.Contains(areas[i]))keys.Add("quest:"+i);}
   if(category==2){if(s.coordinates.Contains(1))keys.Add("relics");keys.Add("surveys");keys.Add("ruins");foreach(int r in s.coordinates){keys.Add("survey:"+r);if(r>=2&&r<=4){keys.Add("relay:"+r);keys.Add("ruin:"+r);}}}
   if(category==3){foreach(var d in c.projects)keys.Add("build:"+d.id);foreach(var d in c.modules)keys.Add("module:"+d.id);foreach(var d in c.recipes)keys.Add("craft:"+d.id);}
   if(category==4)foreach(var d in c.commissions)keys.Add("commission:"+d.id);
   if(category==5)foreach(var box in s.crates)keys.Add("crate:"+box.id);
   return keys;
  }
 }
}
