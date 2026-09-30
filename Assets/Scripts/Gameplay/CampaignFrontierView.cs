using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  int frontierTab,bagPage,selectedField;
  public void ViewSurvey(int region){Open("frontier");frontierTab=2;selectedField=Mathf.Clamp(region-1,0,4);}
  string FrontierTrackedHint {get {string key=State.trackedObjective;if(key=="surveys"){int n=0;for(int i=1;i<=5;i++)if(Exp("survey-"+i))n++;return n<5?"星界勘测 "+n+"/5 · 地标旁长按 E":"";}if(key=="ruins"){int n=0;for(int i=2;i<=4;i++)if(Exp("ruin-"+i))n++;return n<3?"遗迹机关 "+n+"/3 · 观察现场线索":"";}if(key!=null&&key.StartsWith("commission:")){var c=FrontierCatalog.Current?.CommissionFor(key.Substring(11));if(c!=null&&!Exp("commission-"+c.id))return c.title+" · "+FrontierCount(c.eventId)+"/"+c.goal;}return "";}}
  void DrawFrontier(){
   var catalog=FrontierCatalog.Current;if(!catalog)return;string[] tabs={"营地建设","长期委托","勘测与遗迹","野外图鉴"};for(int i=0;i<4;i++)if(Action(new Rect(195+i*310,191,293,40),tabs[i]))frontierTab=i;
   if(frontierTab==0){for(int i=0;i<catalog.projects.Length;i++){var p=catalog.projects[i];int y=256+i*90;bool done=Exp("build-"+p.id);Caption(new Rect(210,y,320,29),p.title,21);Caption(new Rect(535,y,800,29),p.effect,17);Paragraph(new Rect(210,y+34,990,43),done?"已建成":!Exp(p.requirement)?"等待伙伴与设施解锁":Cost(p.cost),14);if(Action(new Rect(1215,y+32,175,36),done?"完成":"建设",AtCamp&&!done&&Exp(p.requirement))&&!BuildProject(p.id))Notice("材料不足");}
    int x=210;foreach(var r in catalog.recipes){if(Action(new Rect(x,640,370,42),r.title,AtCamp&&Exp(r.requirement))&&!CraftFrontier(r.id))Notice("材料不足或背包空间不足");Paragraph(new Rect(x,691,370,52),Cost(r.cost),13);x+=405;}
   }else if(frontierTab==1){for(int i=0;i<catalog.commissions.Length;i++){var c=catalog.commissions[i];int y=258+i*72;bool done=Exp("commission-"+c.id);Caption(new Rect(215,y,665,30),c.title+"   "+FrontierCount(c.eventId)+" / "+c.goal,19);Caption(new Rect(215,y+30,815,27),RewardNames(c.reward),14);if(Action(new Rect(1060,y+6,130,36),"追踪",!done))TrackObjective("commission:"+c.id);if(Action(new Rect(1205,y+6,175,36),done?"已结算":"领取",AtCamp&&!done&&FrontierCount(c.eventId)>=c.goal))ClaimCommission(c.id);}
   }else if(frontierTab==2){for(int i=1;i<=5;i++){var l=catalog.LandmarkFor(i);bool known=Exp("survey-"+i);if(Action(new Rect(215,259+(i-1)*53,370,42),known?l.title:"待勘测 · "+CampaignRules.Regions[i]))selectedField=i-1;}var entry=catalog.LandmarkFor(selectedField+1);bool found=Exp("survey-"+(selectedField+1));Caption(new Rect(640,269,710,42),found?entry.title:"独立勘测站",25);Paragraph(new Rect(640,333,710,157),found?entry.story:"在地标旁停留三秒完成勘测。记录不会占用背包，也不会随死亡丢失。",22);
    Caption(new Rect(215,556,1100,34),"遗迹记录",22);for(int i=2;i<=4;i++){var r=catalog.RuinFor(i);Caption(new Rect(220+(i-2)*385,607,375,33),r.title+" · "+(Exp("ruin-"+i)?"已开启":"待调查"),18);}if(Action(new Rect(640,687,335,40),"追踪勘测"))TrackObjective("surveys");if(Action(new Rect(990,687,335,40),"追踪遗迹"))TrackObjective("ruins");
   }else{for(int i=0;i<catalog.species.Length;i++){var s=catalog.species[i];bool known=State.fieldRecords.Contains(s.id);Caption(new Rect(230,263+i*63,260,28),known?s.title:"尚未记录",20);Paragraph(new Rect(510,263+i*63,830,46),known?s.detail:"击败对应生物后记录其行为。",16);}}
  }
  static string RewardNames(ItemStack[] items){string text="奖励  ";foreach(var item in items)text+=CampaignRules.ItemNames[(int)item.item]+" ×"+item.count+"   ";return text;}
 }
}
