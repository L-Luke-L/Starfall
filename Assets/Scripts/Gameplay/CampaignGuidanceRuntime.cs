using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Starfall {
 public sealed class GuidanceNavigation {
  public bool available,screen;public Vector3 point,destination;public Vector2 screenPoint;public float distance;public string label="",instruction="",problem="";
 }
 public sealed partial class CampaignRuntime {
  GuidanceStep guidanceStep;CampaignState guidanceState;GuidanceContext guidanceContext;float guidanceRefresh,guidanceNavRefresh,guidanceIdle;
  GuidanceNavigation guidanceNavigation;string guidanceIdentity="",guidanceNavIdentity="",guidanceTracked="",guidanceTutorial="";float guidanceTutorialUntil;
  int guidanceLastRegion=-1;readonly NavMeshPath guidancePath=new NavMeshPath();
  public static int GuidanceDetail {get=>Mathf.Clamp(PlayerPrefs.GetInt("Starfall.TaskGuidance.Detail",2),0,2);set{PlayerPrefs.SetInt("Starfall.TaskGuidance.Detail",Mathf.Clamp(value,0,2));PlayerPrefs.Save();}}
  public GuidanceContext GetGuidanceContext(){
   if(guidanceContext==null){guidanceContext=new GuidanceContext();guidanceContext.canEmergency=CanEmergency;guidanceContext.hasRegionOre=GuidanceRegionOre;guidanceContext.hasLivingEnemy=GuidanceHasLivingEnemy;}
   var f=FrontierCatalog.Current;var l=LostRealmCatalog.Current;
   guidanceContext.projects=f&&f.projects!=null?f.projects:new FrontierCatalog.Project[0];guidanceContext.commissions=f&&f.commissions!=null?f.commissions:new FrontierCatalog.Commission[0];guidanceContext.recipes=f&&f.recipes!=null?f.recipes:new FrontierCatalog.Recipe[0];guidanceContext.ruins=f&&f.ruins!=null?f.ruins:new FrontierCatalog.Ruin[0];
   guidanceContext.modules=l&&l.modules!=null?l.modules:new LostRealmCatalog.Module[0];guidanceContext.relays=l&&l.relays!=null?l.relays:new LostRealmCatalog.Relay[0];
   guidanceContext.underground=director.underground;guidanceContext.bombSeconds=BombSeconds;guidanceContext.relayRegion=RegionView?RegionView.id:0;guidanceContext.relayRemaining=RegionView?RegionView.RelayRemaining:0;guidanceContext.relayMask=RegionView?RegionView.RelayMask:0;guidanceContext.nodesRemaining=RegionView&&RegionView.boss?RegionView.boss.NodesRemaining:3;guidanceContext.bossWeakOpen=RegionView&&RegionView.boss&&RegionView.boss.WeakOpen;return guidanceContext;
  }
  bool GuidanceRegionOre(int region,ItemId item){
   if(region==2&&item!=ItemId.IronOre&&item!=ItemId.CopperOre||region==3&&item!=ItemId.CobaltOre||region==4&&item!=ItemId.StarOre)return false;
   progress.TryGetValue(region,out var active);var saved=State.regions.Find(x=>x.id==region);for(int i=0;i<12;i++)if((region!=2||((i%2==0)==(item==ItemId.IronOre)))&&!(active!=null?active.mined.Contains(1400+i):saved!=null&&saved.mined.Contains(1400+i)))return true;return false;
  }
  bool GuidanceHasLivingEnemy(int region){
   if(RegionView&&RegionView.id==region){foreach(var actor in RegionView.player.arena.creatures){if(!actor||!actor.gameObject.activeInHierarchy)continue;var life=actor.GetComponent<CreatureVitality>();if(life&&!life.Dead)return true;}return false;}
   if(region==1){progress.TryGetValue(1,out var active);int dead=0;if(active!=null){foreach(var hp in active.enemyHealth.Values)if(hp<=0)dead++;}else{var area=State.regions.Find(x=>x.id==1);if(area!=null)foreach(var enemy in area.enemies)if(enemy.health<=0)dead++;}if(dead>=2)return false;}
   return region>=1&&region<=4;
  }
  public GuidanceStep CurrentGuidance {get {
   if(guidanceStep==null||guidanceState!=State||guidanceTracked!=State.trackedObjective||Time.unscaledTime>=guidanceRefresh){guidanceStep=CampaignGuidance.Resolve(State,GetGuidanceContext());guidanceState=State;guidanceTracked=State.trackedObjective;guidanceRefresh=Time.unscaledTime+.2f;}
   return guidanceStep;
  }}
  public void InvalidateGuidance(){guidanceRefresh=guidanceNavRefresh=-1;}
  public GuidanceNavigation GetGuidanceNavigation(){
   var g=CurrentGuidance;string targetKey=GuidanceActiveTarget(g);string identity=g.key+"/"+g.stepId+"/"+State.region+"/"+State.layer+"/"+director.underground+"/"+targetKey;
   if(guidanceNavigation!=null&&identity==guidanceNavIdentity&&Time.unscaledTime<guidanceNavRefresh)return guidanceNavigation;
   guidanceNavIdentity=identity;guidanceNavRefresh=Time.unscaledTime+.65f;var n=new GuidanceNavigation{label=GuidanceLabel(targetKey),instruction=g.action};guidanceNavigation=n;if(g.complete||string.IsNullOrEmpty(targetKey))return n;
   if(Explorer&&g.region!=State.region){n.destination=Explorer.returnBeacon.position;n.label="返回入口信标";n.instruction="靠近后按 E 返回，再前往 "+CampaignRules.Regions[Mathf.Clamp(g.region,0,5)];n.available=true;}
   else if(!Explorer&&g.region>0){
    n.available=true;n.label="前往 "+CampaignRules.Regions[g.region];
    if(director.underground){if(!State.coordinates.Contains(State.layer)){n.screen=true;n.screenPoint=new Vector2(139,252);n.destination=director.winchAnchor.position;n.label="切换到有坐标的矿层";n.instruction="点击任务区“选择矿层”，切换到第 "+g.region+" 层，再按 E 打开坐标";}else{if(recoveredCore&&recoveredCore.activeInHierarchy)n.destination=recoveredCore.transform.position;else n.destination=director.winchAnchor.position;n.instruction="按 E 打开坐标，选择 "+CampaignRules.Regions[g.region];}}
    else if(State.calibrated){n.screen=true;n.screenPoint=new Vector2(127,53);n.destination=director.wormhole&&director.wormhole.portalVisual?director.wormhole.portalVisual.transform.position:director.mine.transform.position;n.instruction="点击左上角“星界坐标”，选择 "+CampaignRules.Regions[g.region];}
    else {n.destination=director.mine.transform.position;n.instruction="靠近矿井按 E 下井，再按 E 选择 "+CampaignRules.Regions[g.region];}
   }
   else if(!Explorer){
    if(director.underground&&g.layer==0){n.available=n.screen=true;n.screenPoint=new Vector2(1465,858);n.destination=director.winchAnchor.position;n.label="返回地表";n.instruction="点击右下角“返回地表”，再前往目标";}
    else if(director.underground&&g.layer>0&&State.layer!=g.layer){n.available=n.screen=true;n.screenPoint=new Vector2(139,252);n.destination=director.winchAnchor.position;n.label="切换到第 "+g.layer+" 层";n.instruction="点击任务区下方“选择矿层”，选择第 "+g.layer+" 层";}
    else if(g.targetKey=="drill"){n.available=n.screen=true;n.screenPoint=new Vector2(800,110);n.destination=director.winchAnchor.position;n.label="启动钻机";}
    else if(g.targetKey.StartsWith("ore:")){int.TryParse(g.targetKey.Substring(4),out int item);foreach(var ore in director.ores){if(!ore||!ore.gameObject.activeInHierarchy||ore.collected)continue;var node=ore.GetComponent<CampaignNode>();if(!node||node.bonus||node.layer!=State.layer||item>=0&&(int)node.item!=item)continue;n.destination=ore.transform.position;n.available=true;n.label=node.fragment?"裂纹矿物":"待回收矿点";break;}}
    else {Transform t=g.targetKey=="mine"?director.mine.transform:g.targetKey=="ship"?director.wreck.transform:g.targetKey.StartsWith("ecology:")&&int.TryParse(g.targetKey.Substring(8),out int e)&&ecologyProps!=null&&e>=0&&e<ecologyProps.Length?ecologyProps[e].transform:director.camp.transform;n.available=t;n.destination=t?t.position:Vector3.zero;}
   }
   else if(RegionView)n.available=RegionView.TryGuidancePoint(targetKey,out n.destination);
   if(!n.available){n.problem="目标暂不可用，请查看 J 手账中的前置条件或返回主路。";return n;}
   n.point=n.destination;n.distance=Explorer?Vector3.Distance(Explorer.transform.position,n.destination):Vector3.Distance(director.player.position,n.destination);
   if(Explorer&&!n.screen&&n.distance>3.4f){
    if(TryGuidancePath(n.destination,out var corners)){for(int i=1;i<corners.Length;i++)if(Vector3.Distance(Explorer.transform.position,corners[i])>1.4f){n.point=corners[i];break;}}
    else if(State.region==4&&RegionView.TryGuidanceRamp(n.destination,out var ramp)){n.point=ramp;n.label="经坡道前往 · "+n.label;n.instruction="沿维修坡道上行，再前往目标";}
    else n.problem=Mathf.Abs(n.destination.y-Explorer.transform.position.y)>3?"目标在不同高度；先返回主路寻找坡道入口。":"当前位置暂未找到完整通路，请先回到主路再接近目标。";
   }
   return n;
  }
  bool TryGuidancePath(Vector3 target,out Vector3[] corners){
   corners=null;if(!Explorer||!NavMesh.SamplePosition(Explorer.transform.position,out var start,3,NavMesh.AllAreas))return false;
   for(int i=0;i<9;i++){float a=(i-1)*Mathf.PI/4;var probe=target+(i==0?Vector3.zero:new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*2.4f);if(!NavMesh.SamplePosition(probe,out var goal,2,NavMesh.AllAreas)||Mathf.Abs(goal.position.y-target.y)>2)continue;if(NavMesh.CalculatePath(start.position,goal.position,NavMesh.AllAreas,guidancePath)&&guidancePath.status==NavMeshPathStatus.PathComplete){corners=guidancePath.corners;return corners.Length>0;}}
   return false;
  }
  static string GuidanceLabel(string key){
   if(key=="winch")return "避难舱绞盘";if(key=="shelter")return "工程师避难舱";if(key=="power")return "医疗站电源";if(key=="medical-door")return "医疗隔离门";if(key=="crane")return "吊机控制器";if(key=="chest")return "遗骸货舱";if(key=="terminal")return "区域档案终端";if(key=="core")return "核心控制台";if(key=="beacon")return "入口信标";if(key=="mine")return "中央矿井";if(key=="ship")return "飞船维修终端";if(key=="camp")return "营地帐篷";if(key=="atlas")return "回营复原星图";if(key=="survey")return "区域勘测仪";if(key=="boss-node")return "未破坏的供能节点";if(key=="boss")return "机甲胸部弱点";if(key.StartsWith("antenna-"))return "待修复天线";if(key.StartsWith("signal-"))return "异常信号";if(key.StartsWith("sample-"))return "生态样本";if(key.StartsWith("cache-"))return "可拆解物资箱";if(key.StartsWith("relic-"))return "未收集遗物";if(key.StartsWith("relay:"))return "未接通中继";if(key.StartsWith("ruin-"))return "遗迹控制器";if(key.StartsWith("vein:"))return "未采空矿脉";if(key.StartsWith("crate:"))return "物资回收箱";if(key.StartsWith("ecology:"))return "生态设施";if(key=="variant")return "未记录的龙类变体";if(key=="enemy")return "未击败的目标";return "任务目标";
  }
  void TickGuidance(){
   if(State.guidanceSeen==null)State.guidanceSeen=new List<string>();var g=CurrentGuidance;string identity=g.key+"/"+g.stepId;if(identity!=guidanceIdentity){guidanceIdentity=identity;guidanceIdle=0;}
   if(Modal||Busy||director.IsPaused||director.Transitioning||director.forge.IsOpen||director.battle.IsTransitioning||Explorer&&(Explorer.Paused||Explorer.arena.Downed))return;
   string stage=Explorer?"fps":director.underground?"underground":"surface";
   if(!State.guidanceSeen.Contains(stage)){State.guidanceSeen.Add(stage);guidanceTutorial=stage=="surface"?"A / D 移动 · 靠近目标按 E · J 查看任务步骤":stage=="underground"?"空格下放抓钩 · 采完本层后点击顶部启动钻机":"WASD 移动 · 鼠标观察 · E 交互 · Shift 奔跑 · 空格跳跃";guidanceTutorialUntil=Time.unscaledTime+12;}
   if(State.region>0&&guidanceLastRegion!=State.region){guidanceLastRegion=State.region;string key="region-"+State.region;if(!State.guidanceSeen.Contains(key)){State.guidanceSeen.Add(key);Notice("本区域任务已收入 J 手账 · 伙伴、探索与委托可选择追踪");}}
   bool combat=false;if(Explorer)foreach(var actor in Explorer.arena.creatures)if(actor&&actor.State!=CreatureState.Roam&&actor.State!=CreatureState.Recover&&Vector3.Distance(actor.transform.position,Explorer.transform.position)<18){combat=true;break;}
   if(Explorer&&(combat||Explorer.arsenal.ActiveMagazine<Explorer.arsenal.ActiveCost)){guidanceTutorial=Explorer.arsenal.ActiveMagazine<Explorer.arsenal.ActiveCost?"弹匣不足：按 R 换弹 · 备用弹药在背包 · 无弹药时可右键近战":"左键射击 · R 换弹 · 右键近战 · 观察预警并离开危险范围";guidanceTutorialUntil=Time.unscaledTime+2;}
   if(Explorer&&Explorer.arena.Health<50){guidanceTutorial="生命不足：B 背包 → 选择急救剂 → 使用；也可返回营地找苏禾治疗";guidanceTutorialUntil=Time.unscaledTime+2;}
   if(!combat&&!g.complete&&GetGuidanceContext().relayRemaining<=0&&BombSeconds<0){guidanceIdle+=Time.deltaTime;if(guidanceIdle>=60){guidanceTutorial="需要帮助时按 J：查看当前步骤、前置条件与详细提示";guidanceTutorialUntil=Time.unscaledTime+8;guidanceIdle=-120;}}
  }
 }
}
