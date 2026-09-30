using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  void CreateBonusNodes(){
   if(director.ores.Count==0)return;var model=director.ores[5];var spots=new[]{new Vector2(-5.7f,-9.6f),new Vector2(5.8f,-10.6f),new Vector2(.2f,-14.2f)};
   for(int i=0;i<3;i++){var g=Instantiate(model.gameObject,model.transform.parent);g.name="可疑矿团 · 第"+(i+2)+"层";g.transform.position=new Vector3(spots[i].x,spots[i].y,model.transform.position.z);var ore=g.GetComponent<Interactable>();ore.displayName="可疑矿团";ore.collected=false;var node=g.GetComponent<CampaignNode>();if(!node)node=g.AddComponent<CampaignNode>();node.bonus=true;node.bonusLayer=i+2;node.layer=i+2;node.index=100+i;node.quantity=3;node.item=ItemId.MachineParts;node.fragment=false;node.origin=g.transform.position;director.ores.Add(ore);g.SetActive(false);}
  }
  public bool Exp(string key)=>State!=null&&State.expansionProgress!=null&&State.expansionProgress.Contains(key);
  public bool MarkExp(string key){if(Exp(key))return false;State.expansionProgress.Add(key);Changed();return true;}
  public bool ModuleEquipped(string id)=>State!=null&&State.equippedModules!=null&&State.equippedModules.Contains(id);
  public bool WeaponUnlocked(int index)=>index==0||index==1&&Exp("engineer-rescued")||index==2&&Exp("crane-restored");
  public bool ScanUnlocked=>Exp("drone-rescued");
  public string QuestHint {
   get {
    if(!Exp("drone-rescued")&&State.coordinates.Contains(1))return "遗骸 · 修复三段天线";
    if(!Exp("engineer-rescued")&&State.coordinates.Contains(2))return "战场 · 打开避难舱";
    if(!Exp("doctor-rescued")&&State.coordinates.Contains(3))return "医疗站 · 解除隔离";
    if(!Exp("crane-restored")&&State.coordinates.Contains(4))return "船坞 · 恢复吊机";
    return "";
   }
  }
  public void ExpansionInteract(string key){
   if(string.IsNullOrEmpty(key))return;bool changed=false;string message=null;
   if(key.StartsWith("antenna-")){changed=AddExp(key);if(All("antenna-",3)){changed|=AddExp("drone-rescued");message="拾光已返回营地 · Q 扫描已解锁";}else message="天线已接通";}
   else if(key.StartsWith("signal-")){if(!ScanUnlocked){Notice("先修复拾光的天线");return;}changed=AddExp(key);if(All("signal-",3)){changed|=AddExp("signals-complete");message="异常图谱完成 · 猎手镜组解锁";}else message="异常信号已记录";}
   else if(key=="winch"){changed=AddExp(key);message="绞盘已解除，避难舱可以开启";}
   else if(key=="shelter"){if(!Exp("winch")){Notice("避难舱仍被绞盘锁住");return;}changed=AddExp(key)|AddExp("engineer-rescued");message="岚已返回营地 · 工坊与霰射器解锁";}
   else if(key=="power"){changed=AddExp(key);message="医疗站供电恢复";}
   else if(key=="medical-door"){if(!Exp("power")){Notice("隔离门缺少电力");return;}changed=AddExp(key)|AddExp("doctor-rescued");message="苏禾已返回营地 · 医疗设施解锁";}
   else if(key.StartsWith("sample-")){if(!Exp("doctor-rescued")){Notice("苏禾会辨认这些样本");return;}changed=AddExp(key);if(AllSamples()){changed|=AddExp("greenhouse");message="生态样本齐备 · 温室已启动";}else message="生态样本已封存";}
   else if(key=="crane"){if(!Exp("engineer-rescued")){Notice("需要岚修复控制器");return;}changed=AddExp(key)|AddExp("crane-restored");message="吊机已复位 · 捷径与磁轨枪解锁";}
   else if(key.StartsWith("relic-")){changed=AddExp(key);message="星界遗物已送至营地展柜";}
   else if(key.StartsWith("cache-")){if(Exp(key))return;GiveParts(5);changed=AddExp(key);message="机械组件 +5";}
   if(changed){Changed();Notice(message);}
  }
  bool AddExp(string key){if(Exp(key))return false;State.expansionProgress.Add(key);return true;}
  void ReconcileExpansionState(){if(All("antenna-",3))AddExp("drone-rescued");if(All("signal-",3))AddExp("signals-complete");if(Exp("shelter"))AddExp("engineer-rescued");if(Exp("medical-door"))AddExp("doctor-rescued");if(AllSamples())AddExp("greenhouse");if(Exp("crane"))AddExp("crane-restored");}
  bool All(string prefix,int count){for(int i=0;i<count;i++)if(!Exp(prefix+i))return false;return true;}
  bool AllSamples()=>Exp("sample-0")&&Exp("sample-1")&&Exp("sample-2");
  public void GiveParts(int amount){if(State.bag.Add(ItemId.MachineParts,amount)||State.warehouse.Add(ItemId.MachineParts,amount))return;var crate=new SavedCrate{id=Guid.NewGuid().ToString("N"),region=0,position=Vector3.zero};crate.items.Add(ItemId.MachineParts,amount);State.crates.Add(crate);}
  public void EliteDefeated(string id){if(Exp("elite-"+id))return;GiveParts(3);if(!State.bag.Add(ItemId.Ammo,12)&&!State.warehouse.Add(ItemId.Ammo,12)){var crate=new SavedCrate{id=Guid.NewGuid().ToString("N"),region=0,position=Vector3.zero};crate.items.Add(ItemId.Ammo,12);State.crates.Add(crate);}State.expansionProgress.Add("elite-"+id);Notice("精英已击败 · 机械组件 +3");Changed();}
  public bool CraftModule(string id){if(!AtCamp||!Exp("engineer-rescued")||State.craftedModules.Contains(id)||!ModuleAvailable(id))return false;var definition=LostRealmCatalog.Current?Array.Find(LostRealmCatalog.Current.modules,m=>m.id==id):null;int cost=definition!=null?definition.parts:5;if(!Transaction(State,new[]{new ItemStack(ItemId.MachineParts,cost)}))return false;State.craftedModules.Add(id);Changed();return true;}
  public bool ModuleAvailable(string id)=>id=="stamina"?Exp("doctor-rescued"):id=="reload"?Exp("engineer-rescued"):id=="magnet"?Exp("drone-rescued"):id=="weakness"&&Exp("signals-complete");
  public bool ToggleModule(string id){if(!AtCamp||!Exp("engineer-rescued")||!State.craftedModules.Contains(id))return false;if(State.equippedModules.Contains(id))State.equippedModules.Remove(id);else if(State.equippedModules.Count<2)State.equippedModules.Add(id);else return false;Changed();return true;}
  public bool StartTreatment(){if(!AtCamp||!Exp("doctor-rescued")||State.health>=100||treatmentTime>0)return false;treatmentTime=3;Notice("苏禾正在治疗");return true;}
  float treatmentTime;
  GameObject[] companionArt,facilityArt,relicArt;
  static readonly string[] CompanionFlags={"drone-rescued","engineer-rescued","doctor-rescued"};
  SpriteRenderer Piece(Transform parent,string name,Vector3 local,Vector3 scale,Color color,int order=30){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;t.localScale=scale;var renderer=t.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=director.pixel;renderer.color=color;renderer.sortingOrder=order;return renderer;}
  void CreateCompanions(){
   var ground=director.surfaceY;companionArt=new GameObject[3];facilityArt=new GameObject[3];relicArt=new GameObject[5];
   string[] portraitPaths={"Companions/ShiguangDrone","Companions/LanEngineer","Companions/SuheDoctor"};
   for(int i=0;i<3;i++){var root=new GameObject(i==0?"拾光 · 勘测无人机":i==1?"岚 · 工程师":"苏禾 · 医师");root.transform.position=new Vector3(i==0?-7.7f:i==1?-6.1f:-11.5f,ground+(i==0?1.35f:.88f),-.31f);companionArt[i]=root;
    var portrait=Resources.Load<Sprite>(portraitPaths[i]);
    if(portrait){var art=new GameObject("独立角色立绘");art.transform.SetParent(root.transform,false);var renderer=art.AddComponent<SpriteRenderer>();renderer.sprite=portrait;renderer.sortingOrder=36;float height=i==0?.95f:1.78f;art.transform.localScale=Vector3.one*(height/portrait.bounds.size.y);continue;}
    if(i==0){Piece(root.transform,"Drone body",Vector3.zero,new Vector3(.52f,.30f,1),new Color(.27f,.45f,.52f));Piece(root.transform,"Optic",new Vector3(0,.02f,-.02f),new Vector3(.19f,.15f,1),new Color(.45f,.88f,1));for(int s=-1;s<=1;s+=2)Piece(root.transform,"Wing "+s,new Vector3(s*.39f,.03f,0),new Vector3(.27f,.08f,1),new Color(.23f,.30f,.35f));}
    else{var coat=i==1?new Color(.45f,.32f,.23f):new Color(.70f,.84f,.82f);Piece(root.transform,"Boots",new Vector3(0,-.55f,0),new Vector3(.42f,.18f,1),new Color(.10f,.15f,.19f));Piece(root.transform,"Coat",new Vector3(0,-.15f,0),new Vector3(.44f,.72f,1),coat);Piece(root.transform,"Face",new Vector3(0,.38f,0),new Vector3(.27f,.29f,1),new Color(.83f,.65f,.50f));Piece(root.transform,"Hair",new Vector3(0,.57f,0),new Vector3(.34f,.14f,1),i==1?new Color(.16f,.13f,.16f):new Color(.22f,.35f,.40f));Piece(root.transform,"Visor",new Vector3(0,.39f,-.01f),new Vector3(.30f,.07f,1),new Color(.30f,.70f,.81f));}
   }
   CreateFacilityArt(ground);
   var shelf=new GameObject("五件独立星界遗物展柜");shelf.transform.position=new Vector3(-9.2f,ground+1.45f,-.56f);Piece(shelf.transform,"Shelf",Vector3.zero,new Vector3(3.3f,.10f,1),new Color(.31f,.36f,.39f),29);
   for(int i=0;i<5;i++){var root=new GameObject("Star relic "+(i+1));root.transform.SetParent(shelf.transform,false);root.transform.localPosition=new Vector3((i-2)*.55f,.26f,0);Piece(root.transform,"Relic case",Vector3.zero,new Vector3(.29f,.37f,1),new Color(.13f,.18f,.23f),30);Piece(root.transform,"Relic glow",new Vector3(0,.05f,-.01f),new Vector3(.13f,.19f,1),new Color(.34f+i*.09f,.72f-i*.06f,.82f),31);relicArt[i]=root;}
   CreateObservatory();
  }
  void TickCompanions(){if(companionArt==null||State==null)return;for(int i=0;i<3;i++){companionArt[i].SetActive(Exp(CompanionFlags[i]));if(companionArt[i].activeSelf)companionArt[i].transform.localPosition=new Vector3(companionArt[i].transform.localPosition.x,director.surfaceY+(i==0?1.35f:.88f)+Mathf.Sin(Time.time*(i==0?3:1.6f)+i)*.035f,companionArt[i].transform.localPosition.z);}
   facilityArt[0].SetActive(Exp("engineer-rescued"));facilityArt[1].SetActive(Exp("doctor-rescued"));facilityArt[2].SetActive(Exp("greenhouse"));for(int i=0;i<5;i++)relicArt[i].SetActive(Exp("relic-"+(i+1)));if(observatory)observatory.SetActive(Exp("atlas-restored"));}
  void TickTreatment(){if(treatmentTime<=0)return;if(!AtCamp||Modal&&panel!="workshop"&&!(panel=="companion"&&selectedCompanion==2)){treatmentTime=0;return;}treatmentTime-=Time.unscaledDeltaTime;if(treatmentTime<=0){State.health=100;Notice("治疗完成");Changed();}}
  void DrawWorkshop(){
   Caption(new Rect(195,184,1100,34),"岚的工坊 · 装备和材料",22);
   if(Action(new Rect(1110,184,230,36),"建设 / 战术整备"))panel="frontier";
   for(int i=0;i<3;i++){string name=i==0?"P12 手枪":i==1?"破障霰射器":"磁轨穿刺枪";if(Action(new Rect(200+i*395,246,360,54),name+(WeaponUnlocked(i)?" · 已解锁":" · 未解锁"),WeaponUnlocked(i))){State.selectedWeapon=i;Changed();Notice("已装备 "+name);}}
   var catalog=LostRealmCatalog.Current;
   string[] ids={"stamina","reload","magnet","weakness"};string[] names={"续航电容","快换机构","拾荒磁环","猎手镜组"};
   for(int i=0;i<4;i++){string id=ids[i];int y=346+i*79;bool available=ModuleAvailable(id),crafted=State.craftedModules.Contains(id),equipped=ModuleEquipped(id);string description=catalog&&catalog.ModuleAt(i)!=null?catalog.ModuleAt(i).description:"";
    Caption(new Rect(205,y,700,34),names[i]+"  ·  "+description,17,available?null:new Color(.45f,.52f,.54f));
    if(Action(new Rect(1040,y,305,43),equipped?"卸下":crafted?"安装":"制作 · 组件5",available&&Exp("engineer-rescued"))){if(crafted){if(!ToggleModule(id))Notice("最多安装两个模块");}else if(!CraftModule(id))Notice("机械组件不足");}
   }
   Caption(new Rect(205,677,650,28),"机械组件：背包 "+State.bag.Count(ItemId.MachineParts)+" · 仓库 "+State.warehouse.Count(ItemId.MachineParts)+"  ｜  槽位 "+State.equippedModules.Count+"/2",16);
   if(Exp("doctor-rescued")&&Action(new Rect(1000,679,345,42),treatmentTime>0?"治疗中…":"苏禾治疗 · 3秒",State.health<100&&treatmentTime<=0))StartTreatment();
  }
  void DrawExpansionJournal(){
   DrawVoyageJournal();
  }
 }
}
