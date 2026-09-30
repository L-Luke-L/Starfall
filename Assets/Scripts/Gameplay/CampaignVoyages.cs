using System;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  int voyageTab,selectedRelic,selectedCompanion;GameObject observatory;
  static readonly string[] QuestTitles={"拾光 · 修复天线","拾光 · 异常图谱","岚 · 战场营救","岚 · 船坞吊机","苏禾 · 医疗隔离","苏禾 · 生态样本"};
  public bool PulseUnlocked=>Exp("drone-rescued")&&Exp("engineer-rescued")&&Exp("relay-complete-2")&&Exp("relay-complete-3")&&Exp("relay-complete-4");
  public int RelicCount {get {int n=0;for(int i=1;i<=5;i++)if(Exp("relic-"+i))n++;return n;}}
  public bool CompleteRelay(int region){var definition=LostRealmCatalog.Current?LostRealmCatalog.Current.RelayFor(region):null;if(definition==null||Exp("relay-complete-"+region))return false;GiveParts(definition.parts);AddExp("relay-complete-"+region);Changed();Notice(PulseUnlocked?"三处回声同步 · F 震荡脉冲解锁":"中继同步完成 · 机械组件 +"+definition.parts);return true;}
  public bool RestoreAtlas(){if(!AtCamp||RelicCount!=5||Exp("atlas-restored"))return false;AddExp("atlas-restored");Changed();Notice("归航星图复原 · 营地观测台启用");return true;}
  int CountProgress(string prefix,int count){int n=0;for(int i=0;i<count;i++)if(Exp(prefix+i))n++;return n;}
  bool QuestComplete(int index){switch(index){case 0:return Exp("drone-rescued");case 1:return Exp("signals-complete");case 2:return Exp("engineer-rescued");case 3:return Exp("crane-restored");case 4:return Exp("doctor-rescued");default:return Exp("greenhouse");}}
  string QuestNext(int index){switch(index){case 0:return "遗骸 · 修复天线 "+CountProgress("antenna-",3)+"/3";case 1:return "战场 / 医疗站 / 船坞 · 记录异常 "+CountProgress("signal-",3)+"/3";case 2:return Exp("winch")?"战场 · 开启避难舱":"战场 · 解除绞盘";case 3:return "船坞 · 恢复吊机";case 4:return Exp("power")?"医疗站 · 解除隔离":"医疗站 · 重接电源";default:return "医疗站两份 / 船坞一份 · 样本 "+CountProgress("sample-",3)+"/3";}}
  public string TrackedHint {get {var key=State.trackedObjective;if(string.IsNullOrEmpty(key))return "";if(key.StartsWith("quest:")&&int.TryParse(key.Substring(6),out var quest)&&quest>=0&&quest<6&&!QuestComplete(quest))return QuestNext(quest);if(key.StartsWith("relay:")&&int.TryParse(key.Substring(6),out var region)&&!Exp("relay-complete-"+region)){var relay=LostRealmCatalog.Current?LostRealmCatalog.Current.RelayFor(region):null;if(relay!=null)return relay.title+" · 同步三座中继";}if(key=="relics"&&!Exp("atlas-restored"))return "星界遗物 "+RelicCount+"/5 · 回营复原星图";return "";}}
  public void TrackObjective(string key){State.trackedObjective=key??"";Changed();Notice(string.IsNullOrEmpty(TrackedHint)?"已追踪主线":"已追踪 · "+TrackedHint);}
  int NearbyCompanion(){if(!AtCamp||companionArt==null)return -1;int nearest=-1;float distance=.82f;for(int i=0;i<companionArt.Length;i++){if(!companionArt[i]||!companionArt[i].activeSelf)continue;float d=Mathf.Abs(director.player.position.x-companionArt[i].transform.position.x);if(d<distance){distance=d;nearest=i;}}return nearest;}
  bool TryCampCompanion(){int index=NearbyCompanion();if(index>=0){selectedCompanion=index;Open("companion");return true;}if(AtCamp&&observatory&&observatory.activeSelf&&Mathf.Abs(director.player.position.x-observatory.transform.position.x)<.8f){Open("journal");tab=5;voyageTab=2;return true;}return false;}
  void DrawCampPrompt(){int index=NearbyCompanion();if(index>=0)Caption(new Rect(570,675,460,35),"E  "+(index==0?"拾光":index==1?"岚":"苏禾"),16,new Color(.94f,.76f,.48f),TextAnchor.MiddleCenter);else if(AtCamp&&observatory&&observatory.activeSelf&&Mathf.Abs(director.player.position.x-observatory.transform.position.x)<.8f)Caption(new Rect(570,675,460,35),"E  归航星图",16,null,TextAnchor.MiddleCenter);}
  void DrawCompanion(){
   string[] names={"拾光","岚","苏禾"};string[] paths={"Companions/ShiguangDrone","Companions/LanEngineer","Companions/SuheDoctor"};var sprite=Resources.Load<Sprite>(paths[selectedCompanion]);if(sprite)GUI.DrawTexture(new Rect(240,230,300,415),sprite.texture,ScaleMode.ScaleToFit,true);
   Caption(new Rect(615,245,700,45),names[selectedCompanion],28);
   string text=selectedCompanion==0?(PulseUnlocked?"回声频率已经复原。短距脉冲能让普通地龙和飞龙失去平衡，厚重装甲不受影响。":Exp("signals-complete")?"异常图谱已完成。三处失步中继还藏着一段共同的频率。":"把异常信号带回来，我会把彼此无关的光点连成路线。"):
    selectedCompanion==1?(Exp("crane-restored")?"吊机重新工作了。你现在可以根据距离选择武器，空弹匣会替你教会这个道理。":"船坞吊机需要重新校准。我把维修频道留在了控制台上。"):
    Exp("greenhouse")?"培养槽里的植物正在扎根。每次返回，你都能看见它们留下的一点绿色。":"医疗站和船坞还留着生态样本。把它们交给我，我们能让营地长出第一片叶子。";
   Paragraph(new Rect(615,325,695,130),text,23);
   int quest=selectedCompanion==0?(Exp("signals-complete")?0:1):selectedCompanion==1?3:5;
   if(Action(new Rect(615,522,310,50),"追踪伙伴支线",!QuestComplete(quest)))TrackObjective("quest:"+quest);
   if(selectedCompanion==1&&Action(new Rect(980,522,320,50),"打开工坊"))Open("workshop");
   if(selectedCompanion==2&&Action(new Rect(980,522,320,50),treatmentTime>0?"治疗中…":"治疗 · 三秒",State.health<100&&treatmentTime<=0))StartTreatment();
   if(selectedCompanion==0&&Action(new Rect(980,522,320,50),"查看中继调查")){Open("journal");tab=5;voyageTab=1;}
   if(Action(new Rect(615,618,685,48),"查看伙伴、回声与遗物记录")){Open("journal");tab=5;}
  }
  void CreateObservatory(){
   observatory=new GameObject("归航星图 · 独立营地观测台");observatory.transform.position=new Vector3(-4.3f,director.surfaceY,-.35f);
   var t=observatory.transform;var metal=new Color(.39f,.44f,.47f);Panel(t,"观测台基座",0,.13f,1.2f,.25f,new Color(.18f,.24f,.27f));
   Panel(t,"观测仪左脚",-.32f,.58f,.08f,1.08f,metal,29,-18);Panel(t,"观测仪右脚",.32f,.58f,.08f,1.08f,metal,29,18);Panel(t,"观测仪枢轴",0,1.12f,.30f,.20f,new Color(.22f,.29f,.33f),30);
   Panel(t,"独立观测镜筒",.15f,1.50f,1.05f,.31f,metal,31,25);Panel(t,"镜筒遮光罩",.63f,1.73f,.19f,.40f,new Color(.18f,.25f,.28f),32,25);Panel(t,"冷光镜片",.72f,1.77f,.04f,.29f,new Color(.35f,.64f,.73f),33,25);
   Panel(t,"观测操作台",-.63f,.63f,.28f,.18f,new Color(.17f,.28f,.31f),30,-8);
   for(int i=0;i<5;i++){float angle=i*Mathf.PI*2/5;Piece(observatory.transform,"独立星图光点 "+i,new Vector3(Mathf.Cos(angle)*.55f,2.15f+Mathf.Sin(angle)*.27f,0),new Vector3(.055f,.055f,1),new Color(.43f,.78f,.88f),31);}observatory.SetActive(false);
  }
  void DrawVoyageJournal(){
   string[] tabs={"伙伴支线","回声中继","星界遗物"};for(int i=0;i<3;i++)if(Action(new Rect(250+i*368,249,345,38),tabs[i]))voyageTab=i;
   if(voyageTab==0){for(int i=0;i<6;i++){bool done=QuestComplete(i);Caption(new Rect(260,312+i*58,760,34),QuestTitles[i]+"  ·  "+(done?"已完成":QuestNext(i)),17,done?new Color(.44f,.76f,.63f):(Color?)null);if(Action(new Rect(1100,312+i*58,230,34),State.trackedObjective=="quest:"+i?"追踪中":"追踪",!done))TrackObjective("quest:"+i);}if(Action(new Rect(980,693,350,34),"回到主线目标"))TrackObjective("");}
   else if(voyageTab==1){var catalog=LostRealmCatalog.Current;if(catalog&&catalog.relays!=null)for(int i=0;i<catalog.relays.Length;i++){var relay=catalog.relays[i];int y=307+i*113;bool done=Exp("relay-complete-"+relay.region);Caption(new Rect(260,y,800,32),relay.title+"  ·  "+(done?"已同步":Mathf.RoundToInt(relay.seconds)+"秒同步窗口"),20);Paragraph(new Rect(260,y+40,810,57),done?relay.story:"接通任意一座后，在窗口内同步另外两座。失步可以重试，奖励只结算一次。",17);if(Action(new Rect(1120,y+16,210,42),done?"已完成":"追踪",!done&&State.coordinates.Contains(relay.region)))TrackObjective("relay:"+relay.region);}
    Caption(new Rect(260,673,1070,37),PulseUnlocked?"F 震荡脉冲 · 6米 · 35体力 · 18秒冷却 · 精英与大型目标免疫":"拾光与岚会在三处回声同步后解锁震荡脉冲。",17);
   }
   else {var catalog=LostRealmCatalog.Current;for(int i=1;i<=5;i++){bool found=Exp("relic-"+i);var relic=catalog?catalog.RelicFor(i):null;if(Action(new Rect(250,312+(i-1)*65,390,49),found&&relic!=null?relic.title:"未发现 · "+CampaignRules.Regions[i]))selectedRelic=i-1;}
    var selected=catalog?catalog.RelicFor(selectedRelic+1):null;bool known=Exp("relic-"+(selectedRelic+1));Caption(new Rect(705,318,620,48),known&&selected!=null?selected.title:"遗物记录",26);Paragraph(new Rect(705,390,620,150),known&&selected!=null?selected.story:"在星界的探索支路寻找旧日遗物。拾取后会送回营地展柜。",23);
    Caption(new Rect(705,554,620,33),"已找回 "+RelicCount+" / 5",18);
    if(Action(new Rect(705,609,620,45),Exp("atlas-restored")?"归航星图已复原":"在营地复原归航星图",AtCamp&&RelicCount==5&&!Exp("atlas-restored")))RestoreAtlas();
    if(Action(new Rect(705,681,620,34),"追踪遗物收集",!Exp("atlas-restored")))TrackObjective("relics");
   }
  }
 }
}
