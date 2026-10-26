using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  int terminalSelection;
  int inventoryFilter;
  Vector2 journalScroll,journalListScroll;
  static readonly string[] InventoryCategories={"全部物资","原矿 / 异晶","精炼材料","补给物资","组件 / 战术"};
  static readonly string[] JournalSections={"任务追踪","调查线索","已证实事实","物种与矿物","飞船修复","失落星界","余烬重建"};
  static readonly string[] JournalSectionIcons={"IconFlag","IconBook","IconCheck","IconFlask","IconWrench","IconStar","IconGear"};
  void TerminalText(Rect r,string text,int size=18,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft,bool wrap=false)=>CampaignGameUI.Text(r,text,director.uiFont,size,color,align,wrap);
  bool TerminalButton(Rect r,string text,bool enabled=true,bool active=false,int size=17)=>CampaignGameUI.Button(r,text,director.uiFont,enabled&&!Busy,active,size);
  void TerminalHeader(float x,float y,float width,string title,string subtitle=null){
   TerminalText(new Rect(x,y,width,30),title,21);if(subtitle!=null)TerminalText(new Rect(x,y+32,width,25),subtitle,16,CampaignGameUI.Muted);
  }
  void DrawInventoryTerminal(){
   bool camp=AtCamp;CampaignGameUI.Panel(new Rect(154,184,1286,58),true);CampaignGameUI.Icon(new Rect(172,198,28,28),"IconShield",CampaignGameUI.Gold);
   TerminalText(new Rect(216,190,835,25),"绑定装备   手枪 · 冲击镐 · 抓钩"+(State.device?" · 同盟跃迁装置":""),17);
   TerminalText(new Rect(216,215,860,21),"装备与知识永久保留，普通物资在死亡后留于回收箱。",14,CampaignGameUI.Muted);
   CampaignGameUI.Badge(new Rect(1211,201,208,25),camp?"仓库已连接":"野外 · 仓库离线",director.uiFont,camp?CampaignGameUI.Green:CampaignGameUI.Muted);
   if(camp){
    string[] tabs={"营地锻造","制作 / 回收","探险手账","故事存档",Exp("engineer-rescued")?"工坊 / 医疗":"营地医疗","转运回收箱"};
    for(int i=0;i<tabs.Length;i++)if(TerminalButton(new Rect(154+i*215,251,204,34),tabs[i],i!=4||Exp("engineer-rescued")||Exp("doctor-rescued"))){if(i<4)panel=new[]{"forge","craft","journal","system"}[i];else if(i==4)panel="workshop";else{TransportCrates();Notice("回收箱已转运到营地");}}
   }else{
    if(TerminalButton(new Rect(154,251,210,34),"探险手账"))panel="journal";
    TerminalText(new Rect(385,251,1000,34),"靠近营地帐篷，接入仓库并使用锻造与制作。",16,CampaignGameUI.Muted);
   }
   for(int i=0;i<InventoryCategories.Length;i++)if(TerminalButton(new Rect(154+i*164,296,154,30),InventoryCategories[i],true,inventoryFilter==i,15)){inventoryFilter=i;bagPage=warehousePage=0;selected=-1;}
   TerminalText(new Rect(1000,296,440,30),camp?"Shift + 单击物品  快速存取":"按类别查找随身物资",15,CampaignGameUI.Muted,TextAnchor.MiddleRight);
   const float top=338;CampaignGameUI.Panel(new Rect(154,top,426,390),true);CampaignGameUI.Panel(new Rect(596,top,426,390),true);CampaignGameUI.Panel(new Rect(1038,top,402,390),true);
   var bagIndices=InventoryIndices(State.bag);var storeIndices=InventoryIndices(State.warehouse);
   int bagPages=InventoryPages(State.bag,bagIndices.Count),storePages=InventoryPages(State.warehouse,storeIndices.Count);bagPage=Mathf.Clamp(bagPage,0,bagPages-1);warehousePage=Mathf.Clamp(warehousePage,0,storePages-1);
   InventoryContainerHeading(State.bag,172,"随身背包","FIELD PACK");
   InventoryTerminalGrid(State.bag,bagIndices,new Vector2(170,391),false,bagPage*16);
   InventoryPageControls(new Rect(172,691,388,25),State.bag,ref bagPage,bagPages);
   if(camp){InventoryContainerHeading(State.warehouse,614,"帐篷仓库","CAMP STORAGE");InventoryTerminalGrid(State.warehouse,storeIndices,new Vector2(612,391),true,warehousePage*16);InventoryPageControls(new Rect(614,691,388,25),State.warehouse,ref warehousePage,storePages);}
   else DrawProtectedEquipment();
   DrawInventorySelection(camp);
   if(TerminalButton(new Rect(154,744,172,36),"合并 / 排序")){State.bag.Sort();if(camp)State.warehouse.Sort();selected=-1;Changed();}
   if(camp){
    if(TerminalButton(new Rect(338,744,235,36),"同类物资快速归仓")){int moved=0;foreach(var s in new List<ItemStack>(State.bag.stacks))if(State.warehouse.Count(s.item)>0){int n=Math.Min(s.count,FreeSpace(State.warehouse,s.item));if(n>0&&State.bag.TransferTo(State.warehouse,s.item,n))moved+=n;}selected=-1;if(moved>0)Changed();Notice(moved>0?"已归仓 "+moved+" 份同类物资":"暂无可归仓的同类物资，或仓库已满");}
    int i=0;foreach(var crate in new List<SavedCrate>(State.crates)){if(crate.region!=0)continue;if(TerminalButton(new Rect(594+i*210,744,196,36),"回收箱 "+(i+1))&&!RecoverCrate(crate.id))Notice("背包空间不足");if(++i>=4)break;}
   }else TerminalText(new Rect(345,744,1050,36),"选择物品查看用途。急救剂可直接使用，战术装备在野外投放。",16,CampaignGameUI.Muted);
  }
  static bool InventoryMatches(ItemId item,int category){int id=(int)item;return category==0||category==1&&(id<5||item==ItemId.AlienCrystal)||category==2&&id>=5&&id<10||category==3&&(item==ItemId.Fuel||item==ItemId.Ammo||item==ItemId.Medkit)||category==4&&(item==ItemId.MachineParts||item==ItemId.BlastCore||item==ItemId.SoundDecoy);}
  List<int> InventoryIndices(ItemContainer container){var indices=new List<int>();for(int i=0;i<container.stacks.Count;i++)if(InventoryMatches(container.stacks[i].item,inventoryFilter))indices.Add(i);return indices;}
  int InventoryPages(ItemContainer container,int matches)=>Math.Max(1,((inventoryFilter==0?container.capacity:matches)+15)/16);
  void InventoryContainerHeading(ItemContainer container,float x,string title,string code){
   TerminalText(new Rect(x,349,245,28),title,20);TerminalText(new Rect(x+245,350,143,25),container.stacks.Count+" / "+container.capacity+" 格",16,CampaignGameUI.CapacityColor(container.stacks.Count/(float)container.capacity),TextAnchor.MiddleRight);
   TerminalText(new Rect(x,376,340,13),code,10,CampaignGameUI.Muted);
  }
  void InventoryTerminalGrid(ItemContainer container,List<int> indices,Vector2 origin,bool warehouse,int offset){
   for(int i=0;i<16;i++){
    int displayIndex=i+offset,index=displayIndex<indices.Count?indices[displayIndex]:-1;var r=new Rect(origin.x+i%4*99,origin.y+i/4*72,94,66);bool exists=index>=0,active=exists&&selected==index&&fromWarehouse==warehouse,available=displayIndex<container.capacity;
    bool hover=exists&&r.Contains(Event.current.mousePosition);CampaignGameUI.Slot(r,active,hover,!available);
    if(!available){CampaignGameUI.Icon(new Rect(r.center.x-10,r.center.y-10,20,20),"IconLock",CampaignGameUI.Muted);continue;}
    if(!exists){TerminalText(r,"+",16,new Color(.29f,.38f,.40f,.45f),TextAnchor.MiddleCenter);continue;}
    var stack=container.stacks[index];var itemColor=CampaignGameUI.ItemColor(stack.item);CampaignGameUI.Solid(new Rect(r.x+8,r.yMax-3,r.width-16,1),new Color(itemColor.r,itemColor.g,itemColor.b,.5f));
    CampaignGameUI.ItemIcon(new Rect(r.x+13,r.y+5,42,37),stack.item,mineralSprites);
    TerminalText(new Rect(r.x+50,r.y+5,36,24),stack.count.ToString(),17,CampaignGameUI.Ink,TextAnchor.UpperRight);
    string itemName=CampaignRules.ItemNames[(int)stack.item];float uiScale=Mathf.Min(Screen.width/1600f,Screen.height/900f);if(uiScale<.75f&&itemName.Length>4)itemName=stack.item==ItemId.StarIngot?"星髓合金":itemName.Substring(0,4);
    TerminalText(new Rect(r.x+3,r.y+43,r.width-6,20),itemName,uiScale<.75f?18:15,active?CampaignGameUI.Gold:CampaignGameUI.Ink,TextAnchor.MiddleCenter);
    bool quick=Event.current.shift;
    if(!Busy&&GUI.Button(r,new GUIContent("",CampaignRules.ItemNames[(int)stack.item]),GUIStyle.none)){selected=index;fromWarehouse=warehouse;InteractionFeel.Play(FeelCue.Click,.5f);if(quick&&AtCamp){MoveInventorySelection(stack.count);return;}}
   }
   if(indices.Count==0)TerminalText(new Rect(origin.x+18,origin.y+106,352,67),inventoryFilter==0?"暂无物资\n探索矿井与星界，收集新的补给。":"此分类暂无物资",17,CampaignGameUI.Muted,TextAnchor.MiddleCenter,true);
  }
  void InventoryPageControls(Rect r,ItemContainer container,ref int page,int pages){
   CampaignGameUI.Progress(new Rect(r.x,r.y+8,148,7),container.stacks.Count/(float)container.capacity,CampaignGameUI.CapacityColor(container.stacks.Count/(float)container.capacity));
   if(pages>1){if(TerminalButton(new Rect(r.x+174,r.y-1,45,27),"‹")){page=(page+pages-1)%pages;selected=-1;}TerminalText(new Rect(r.x+224,r.y-1,100,27),(page+1)+" / "+pages,16,CampaignGameUI.Muted,TextAnchor.MiddleCenter);if(TerminalButton(new Rect(r.xMax-45,r.y-1,45,27),"›")){page=(page+1)%pages;selected=-1;}}
   else TerminalText(new Rect(r.x+180,r.y-1,206,27),"每组最多 99",16,CampaignGameUI.Muted,TextAnchor.MiddleRight);
  }
  void DrawProtectedEquipment(){
   TerminalHeader(616,351,380,"装备与任务物品","永久绑定，不占用背包格位");
   string[] labels={"手枪 / 冲击镐 / 抓钩",State.device?"同盟跃迁装置 · 已绑定":"同盟跃迁装置 · 未获得",State.Has("bomb-crafted")?"异晶炸弹 · 已制作":"异晶炸弹 · 未制作"};
   string[] icons={"IconWrench","IconCore","IconStar"};
   for(int i=0;i<3;i++){var r=new Rect(616,423+i*72,384,59);CampaignGameUI.Panel(r,true);CampaignGameUI.Icon(new Rect(r.x+13,r.y+14,28,28),icons[i],i==2?CampaignGameUI.Gold:CampaignGameUI.Cyan);TerminalText(new Rect(r.x+57,r.y+8,r.width-70,42),labels[i],16);}
   TerminalText(new Rect(616,662,378,42),"任务成品与知识通过手账永久记录。",15,CampaignGameUI.Muted,TextAnchor.MiddleLeft,true);
  }
  void DrawInventorySelection(bool camp){
   var container=fromWarehouse?State.warehouse:State.bag;
   if(selected<0||selected>=container.stacks.Count||fromWarehouse&&!camp){
    TerminalText(new Rect(1063,350,350,22),"物资档案  /  ITEM DETAILS",13,CampaignGameUI.Muted);CampaignGameUI.Icon(new Rect(1193,437,80,80),"IconBag",new Color(.37f,.46f,.48f));TerminalText(new Rect(1064,544,350,38),"选择一件物品",22,CampaignGameUI.Ink,TextAnchor.MiddleCenter);
    TerminalText(new Rect(1080,602,318,86),camp?"查看用途与储量，或按住 Shift 单击物品快速存取。":"查看物品用途、当前数量与可用操作。",17,CampaignGameUI.Muted,TextAnchor.UpperCenter,true);return;
   }
   var stack=container.stacks[selected];var color=CampaignGameUI.ItemColor(stack.item);CampaignGameUI.ItemIcon(new Rect(1067,356,74,74),stack.item,mineralSprites);
   TerminalText(new Rect(1156,357,258,35),CampaignRules.ItemNames[(int)stack.item],23);TerminalText(new Rect(1156,400,258,25),CampaignGameUI.ItemType(stack.item),15,color);
   CampaignGameUI.Divider(new Rect(1062,444,354,1));
   TerminalText(new Rect(1064,464,350,82),CampaignGameUI.ItemDescription(stack.item),17,CampaignGameUI.Ink,TextAnchor.UpperLeft,true);
   TerminalText(new Rect(1064,557,170,23),"当前堆叠",14,CampaignGameUI.Muted);TerminalText(new Rect(1244,557,170,23),fromWarehouse?"仓库总量":"随身总量",14,CampaignGameUI.Muted);
   TerminalText(new Rect(1064,582,170,32),stack.count.ToString("D2"),26,CampaignGameUI.Gold);TerminalText(new Rect(1244,582,170,32),container.Count(stack.item).ToString(),26,CampaignGameUI.Ink);
   if(camp){
    var destination=fromWarehouse?State.bag:State.warehouse;bool space=FreeSpace(destination,stack.item)>0;
    if(TerminalButton(new Rect(1064,633,170,36),fromWarehouse?"取出此组":"存入此组",space,true))MoveInventorySelection(stack.count);
    if(selected>=0&&TerminalButton(new Rect(1244,633,170,36),"转移一半",space&&stack.count>1))MoveInventorySelection(Math.Max(1,stack.count/2));
    if(!space)TerminalText(new Rect(1064,612,350,19),"目标容器已满",13,CampaignGameUI.Gold);
   }
   if(selected>=0&&TerminalButton(new Rect(1064,camp?680:633,170,36),"拆分堆叠",stack.count>1&&container.stacks.Count<container.capacity)){if(container.Split(selected))Changed();}
   int health=Explorer?Explorer.arena.Health:State.health;
   if(selected>=0&&!fromWarehouse&&stack.item==ItemId.Medkit&&TerminalButton(new Rect(1244,camp?680:633,170,36),health<100?"使用急救剂":"护甲已满",health<100)){if(!UseMedkit())Notice("护甲已满");if(!container.stacks.Contains(stack))selected=-1;}
  }
  bool MoveInventorySelection(int requested){
   if(!AtCamp||Busy||requested<=0)return false;var source=fromWarehouse?State.warehouse:State.bag;
   if(selected<0||selected>=source.stacks.Count)return false;var stack=source.stacks[selected];var destination=fromWarehouse?State.bag:State.warehouse;
   int amount=Math.Min(Math.Min(requested,stack.count),Math.Max(0,FreeSpace(destination,stack.item)));
   if(amount==0||!source.TransferTo(destination,stack.item,amount)){Notice("目标容器空间不足");return false;}
   selected=-1;Changed();Notice("已"+(fromWarehouse?"取出 ":"存入 ")+CampaignRules.ItemNames[(int)stack.item]+" ×"+amount+(amount<requested?" · 剩余物资保留在原处":""));return true;
  }
  void DrawJournalTerminal(){
   CampaignGameUI.Panel(new Rect(154,192,209,584),true);CampaignGameUI.Panel(new Rect(379,192,340,584),true);CampaignGameUI.Panel(new Rect(735,192,705,584),true);
   CampaignGameUI.SectionLabel(new Rect(173,208,170,25),"探险数据库",director.uiFont);
   for(int i=0;i<JournalSections.Length;i++){
    var r=new Rect(169,255+i*57,179,46);
    if(TerminalButton(r,"",true,tab==i,17)){tab=i;terminalSelection=0;journalScroll=journalListScroll=Vector2.zero;}
    CampaignGameUI.Icon(new Rect(r.x+10,r.y+12,22,22),JournalSectionIcons[i],tab==i?CampaignGameUI.Cyan:CampaignGameUI.Muted);
    TerminalText(new Rect(r.x+44,r.y,r.width-48,r.height),JournalSections[i],17,tab==i?Color.white:CampaignGameUI.Ink);
   }
   CampaignGameUI.Divider(new Rect(173,687,171,1));TerminalText(new Rect(173,704,171,24),"记录随探索永久保留",13,CampaignGameUI.Muted,TextAnchor.MiddleCenter);
   TerminalText(new Rect(173,733,171,22),State.journal.Count+" 份线索  /  "+State.coordinates.Count+" 个区域",13,CampaignGameUI.Muted,TextAnchor.MiddleCenter);
   switch(tab){case 0:DrawTaskTerminal();break;case 1:DrawClueTerminal();break;case 2:DrawFactTerminal();break;case 3:DrawCodexTerminal();break;case 4:DrawRepairTerminal();break;case 5:DrawVoyageTerminal();break;default:DrawFrontierTerminalSummary();break;}
  }
  bool JournalRow(int index,float y,string title,string detail,bool active,bool complete=false,float height=56,float x=395,float width=308){
   var r=new Rect(x,y,width,height);if(TerminalButton(r,"",true,active)){terminalSelection=index;selectedJournal=index;journalScroll=Vector2.zero;}
   if(complete)CampaignGameUI.Icon(new Rect(x+12,y+15,19,19),"IconCheck",CampaignGameUI.Green);
   TerminalText(new Rect(x+(complete?40:14),y+5,width-(complete?54:28),26),title,17,complete?CampaignGameUI.Green:active?CampaignGameUI.Gold:CampaignGameUI.Ink);
   TerminalText(new Rect(x+14,y+32,width-31,18),detail,14,CampaignGameUI.Muted);return active;
  }
  void DetailHeading(string title,string subtitle,string icon="IconFlag",Color? tint=null){
   CampaignGameUI.Icon(new Rect(765,225,37,37),icon,tint??CampaignGameUI.Gold);TerminalText(new Rect(820,217,586,41),title,26);
   TerminalText(new Rect(820,264,586,25),subtitle,15,tint??CampaignGameUI.Muted);CampaignGameUI.Divider(new Rect(763,307,646,1));
  }
  void DetailParagraph(string text,float y=331,float height=200,int size=21){TerminalText(new Rect(769,y,630,height),text,size,CampaignGameUI.Ink,TextAnchor.UpperLeft,true);}
  void DrawTaskTerminal(){DrawGuidanceJournal();}
  void DetailProgress(float y,string title,int current,int total){TerminalText(new Rect(770,y,430,26),title,17,CampaignGameUI.Muted);TerminalText(new Rect(1250,y,146,26),current+" / "+total,17,CampaignGameUI.Ink,TextAnchor.MiddleRight);CampaignGameUI.Progress(new Rect(770,y+31,626,9),total>0?current/(float)total:0);}
  void DrawClueTerminal(){
   TerminalHeader(397,211,300,"已记录线索",State.journal.Count+" 份调查记录");
   if(State.journal.Count==0){TerminalText(new Rect(401,303,291,150),"尚无调查记录\n\n调查遗骸货舱与区域终端，收集探险者留下的手账。",18,CampaignGameUI.Muted,TextAnchor.UpperLeft,true);DetailHeading("等待新的线索","调查记录不会在死亡后丢失","IconBook");DetailParagraph("探险者的记录会随着调查自动收入手账。已获线索、已证实事实与任务进度会分别保存。",342,220);return;}
   selectedJournal=Mathf.Clamp(selectedJournal,0,State.journal.Count-1);journalListScroll=GUI.BeginScrollView(new Rect(395,280,308,460),journalListScroll,new Rect(0,0,288,Mathf.Max(460,State.journal.Count*56)));for(int i=0;i<State.journal.Count;i++)JournalRow(i,i*56,CampaignJournal.Title(State.journal[i].id),CampaignRules.Regions[State.journal[i].region],selectedJournal==i,false,52,0,288);GUI.EndScrollView();
   var record=State.journal[selectedJournal];DetailHeading(CampaignJournal.Title(record.id),record.id+" / "+CampaignRules.Regions[record.region],"IconBook");
   var style=new GUIStyle(GUI.skin.label){font=director.uiFont,fontSize=21,wordWrap=true,normal={textColor=CampaignGameUI.Ink}};string text=CampaignJournal.Text(record.id);float height=Mathf.Max(300,style.CalcHeight(new GUIContent(text),610));
   journalScroll=GUI.BeginScrollView(new Rect(768,332,633,325),journalScroll,new Rect(0,0,610,height));GUI.Label(new Rect(0,0,610,height),text,style);GUI.EndScrollView();
   string recordedAt=string.IsNullOrEmpty(record.acquired)?"未记录":record.acquired.Replace("T"," ")+" UTC";
   CampaignGameUI.Divider(new Rect(770,678,626,1));TerminalText(new Rect(770,691,625,33),"记录时间  "+recordedAt,14,CampaignGameUI.Muted);
   TerminalText(new Rect(770,735,625,25),"滚动阅读调查记录  ·  已获得的线索不会丢失",13,CampaignGameUI.Muted);
  }
  void DrawFactTerminal(){
   TerminalHeader(397,211,300,"已证实的事实","只保留调查中确认的结论");string[] ids={"J04","J06","J07"};
   for(int i=0;i<ids.Length;i++)JournalRow(i,284+i*75,State.Knows(ids[i])?CampaignJournal.Title(ids[i]):"等待证实",State.Knows(ids[i])?"证据已记录":"继续调查区域档案",terminalSelection==i,State.Knows(ids[i]));
   terminalSelection=Mathf.Clamp(terminalSelection,0,2);string id=ids[terminalSelection];bool known=State.Knows(id);DetailHeading(known?CampaignJournal.Title(id):"未证实的档案",known?"证据完整 / 已归档":"尚无完整调查证据",known?"IconCheck":"IconLock");DetailParagraph(known?CampaignJournal.Fact(id):"继续调查医疗站、军用船坞与核心档案，确认裂隙与战争背后的事实。",341,240,23);
  }
  void DrawCodexTerminal(){
   TerminalHeader(397,211,300,"物种与矿物","探索中取得的知识");string[] names={"矿物与锻造","变异本土生物"};for(int i=0;i<2;i++)JournalRow(i,284+i*72,names[i],i==0?"原矿 / 金属 / 异晶":"地龙与飞龙",terminalSelection==i);
   if(terminalSelection==1){DetailHeading("变异本土生物",State.Knows("J04")?"医疗调查已解锁":"物种来源尚未查明","IconFlask");DetailParagraph(State.Knows("J04")?"地龙与飞龙是受到 D4 污染影响的本土生物。\n\n污染改变了它们的生理与行为。它们仍表现出护群、筑巢与避噪行为。":"继续调查医疗站的组织报告，了解星界生物的来源与行为。",343,260);return;}
   DetailHeading("矿物与锻造","原矿用于制作，金属锭用于修复","IconFlask");
   for(int i=0;i<5;i++){var item=(ItemId)i;bool known=i==0||State.Has("identified-"+(i-1));CampaignGameUI.ItemIcon(new Rect(773,335+i*57,40,40),item,mineralSprites);TerminalText(new Rect(832,336+i*57,558,36),known?CampaignRules.ItemNames[i]+"  /  "+(i==0?"配电与弹药":i==1?"船体结构":i==2?"能源系统":i==3?"引擎冷却":"导航稳定"):"未识别矿物",18,known?CampaignGameUI.Ink:CampaignGameUI.Muted);}
   TerminalText(new Rect(774,643,617,65),State.unlockedLayer==5?"稳定异晶来自 D4 结晶，可直接使用，不参与锻造。":"探索更深的矿层，寻找稳定异晶。",18,CampaignGameUI.Muted,TextAnchor.UpperLeft,true);
  }
  void DrawRepairTerminal(){
   TerminalHeader(397,211,300,"飞船修复清单",State.repairs.Count+" / 4 项已安装");for(int i=0;i<4;i++)JournalRow(i,282+i*72,CampaignRules.RepairNames[i],State.repairs.Contains(i.ToString())?"已安装":"等待精炼材料",terminalSelection==i,State.repairs.Contains(i.ToString()));
   terminalSelection=Mathf.Clamp(terminalSelection,0,3);bool done=State.repairs.Contains(terminalSelection.ToString());DetailHeading(CampaignRules.RepairNames[terminalSelection],done?"修复完成 / 已安装":"等待提交修复材料","IconWrench",done?CampaignGameUI.Green:(Color?)null);
   TerminalText(new Rect(773,334,621,63),done?"该结构已修复。安装进度与已提交的物资会永久保留。":"在飞船旁提交所需材料。优先取用随身背包，其次营地仓库。",20,CampaignGameUI.Ink,TextAnchor.UpperLeft,true);
   var costs=CampaignRules.RepairCost(terminalSelection);for(int i=0;i<costs.Length;i++){var cost=costs[i];int have=State.bag.Count(cost.item)+State.warehouse.Count(cost.item);CampaignGameUI.ItemIcon(new Rect(779,427+i*66,42,42),cost.item,mineralSprites);TerminalText(new Rect(840,428+i*66,400,40),CampaignRules.ItemNames[(int)cost.item],20);TerminalText(new Rect(1244,428+i*66,145,40),have+" / "+cost.count,20,have>=cost.count?CampaignGameUI.Green:CampaignGameUI.Gold,TextAnchor.MiddleRight);}
   DetailProgress(597,"修复总进度",State.repairs.Count,4);if(TerminalButton(new Rect(772,678,621,42),AtShip?"打开飞船维修终端":"前往飞船旁提交材料",AtShip))panel="ship";
  }
  void DrawVoyageTerminal(){
   TerminalHeader(397,211,300,"失落星界","伙伴、回声与遗物");string[] tabs={"伙伴","中继","遗物"};for(int i=0;i<3;i++)if(TerminalButton(new Rect(395+i*104,278,98,34),tabs[i],true,voyageTab==i,16)){voyageTab=i;terminalSelection=0;}
   if(voyageTab==0){
    for(int i=0;i<6;i++)JournalRow(i,327+i*59,QuestTitles[i],QuestComplete(i)?"已完成":QuestNext(i),terminalSelection==i,QuestComplete(i));terminalSelection=Mathf.Clamp(terminalSelection,0,5);int quest=terminalSelection;bool done=QuestComplete(quest);DetailHeading(QuestTitles[quest],done?"伙伴支线已完成":"伙伴支线 / 当前目标",done?"IconCheck":"IconFlag");DetailParagraph(done?"任务已完成。返回营地，与伙伴交谈，查看新的帮助与营地变化。":QuestNext(quest),345,200,23);if(TerminalButton(new Rect(773,670,620,43),done?"已完成":"追踪伙伴支线",!done,State.trackedObjective=="quest:"+quest))TrackObjective("quest:"+quest);
   }else if(voyageTab==1){
    var catalog=LostRealmCatalog.Current;if(!catalog||catalog.relays==null||catalog.relays.Length==0){DetailHeading("回声中继","继续调查异常信号","IconDecoy");return;}
    terminalSelection=Mathf.Clamp(terminalSelection,0,catalog.relays.Length-1);for(int i=0;i<catalog.relays.Length;i++){var relay=catalog.relays[i];JournalRow(i,329+i*75,relay.title,Exp("relay-complete-"+relay.region)?"已同步":Mathf.RoundToInt(relay.seconds)+"秒同步窗口",terminalSelection==i,Exp("relay-complete-"+relay.region));}
    var entry=catalog.relays[terminalSelection];bool done=Exp("relay-complete-"+entry.region);DetailHeading(entry.title,done?"中继同步已完成":"探索调查 / 限时同步","IconDecoy");DetailParagraph(done?entry.story:"接通任意一座后，在窗口内同步另外两座。失步可以重试，奖励只结算一次。",342,187);
    TerminalText(new Rect(773,559,616,80),PulseUnlocked?"震荡脉冲已解锁\nF / 6米 / 35体力 / 18秒冷却":"救出拾光与岚，并同步三处回声中继，以解锁震荡脉冲。",19,PulseUnlocked?CampaignGameUI.Green:CampaignGameUI.Muted,TextAnchor.UpperLeft,true);
    if(TerminalButton(new Rect(773,671,620,42),done?"已完成":"追踪中继调查",!done&&State.coordinates.Contains(entry.region),State.trackedObjective=="relay:"+entry.region))TrackObjective("relay:"+entry.region);
   }else{
    var catalog=LostRealmCatalog.Current;for(int i=1;i<=5;i++){bool found=Exp("relic-"+i);var relic=catalog?catalog.RelicFor(i):null;JournalRow(i-1,327+(i-1)*65,found&&relic!=null?relic.title:"未发现的遗物",CampaignRules.Regions[i],selectedRelic==i-1,found);}selectedRelic=Mathf.Clamp(terminalSelection,0,4);var entry=catalog?catalog.RelicFor(selectedRelic+1):null;bool known=Exp("relic-"+(selectedRelic+1));DetailHeading(known&&entry!=null?entry.title:"遗物记录",RelicCount+" / 5 件已找回","IconStar");DetailParagraph(known&&entry!=null?entry.story:"在星界的探索支路寻找旧日遗物。拾取后会自动送回营地展柜。",342,190);DetailProgress(551,"归航星图",RelicCount,5);
    if(TerminalButton(new Rect(773,620,620,40),Exp("atlas-restored")?"归航星图已复原":"在营地复原归航星图",AtCamp&&RelicCount==5&&!Exp("atlas-restored")))RestoreAtlas();if(TerminalButton(new Rect(773,674,620,40),"追踪遗物收集",!Exp("atlas-restored"),State.trackedObjective=="relics"))TrackObjective("relics");
   }
  }
  void DrawFrontierTerminalSummary(){
   TerminalHeader(397,211,300,"余烬重建","营地建设与长期成长");string[] names={"营地建设","长期委托","勘测与遗迹","野外图鉴"};for(int i=0;i<4;i++)JournalRow(i,283+i*73,names[i],new[]{"设施 / 战术装备","事件进度 / 领取奖励","记录地标 / 调查机关","击败生物 / 记录行为"}[i],terminalSelection==i);
   DetailHeading(names[Mathf.Clamp(terminalSelection,0,3)],"可选成长 / 不影响修船与结局","IconGear");DetailParagraph("营地会随着你的探索逐步重建。\n\n设施、委托、野外记录与遗迹进展会永久保存。回到营地后，可与伙伴合作建设并领取委托奖励。",343,220);
   if(TerminalButton(new Rect(773,669,620,43),"打开"+names[Mathf.Clamp(terminalSelection,0,3)])){frontierTab=Mathf.Clamp(terminalSelection,0,3);panel="frontier";}
  }
 }
}
