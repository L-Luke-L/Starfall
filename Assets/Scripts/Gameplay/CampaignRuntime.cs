using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 [DefaultExecutionOrder(-500)]
 public sealed partial class CampaignRuntime:MonoBehaviour {
  public static CampaignRuntime Current {get;private set;}
  public SceneDirector director;public CampaignState State {get;private set;}public CampaignStore Store {get;private set;}
  public CampaignRegion RegionView {get;private set;}public bool Busy {get;private set;}public float BombSeconds {get;private set;}=-1;
  public bool Modal=>panel!="";public bool IsQA=>Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-campaignQA"||x=="-lostRealmQA"||x=="-frontierQA"||x=="-guidanceQA");
  public bool AtCamp=>!director.battle.IsEngaged&&!director.underground&&!director.Transitioning&&Mathf.Abs(director.player.position.x-director.camp.transform.position.x)<=Mathf.Max(director.camp.radius,5.4f);
  public bool AtShip=>!director.battle.IsEngaged&&!director.underground&&Mathf.Abs(director.player.position.x-director.wreck.transform.position.x)<=director.wreck.radius;
  public FirstPersonExplorer Explorer=>director.battle.Explorer;
  bool loading,saveBlocked;float autosave,toastAge;string toast;Vector3[] oreOrigins;List<SubsurfaceDefinition> generated=new List<SubsurfaceDefinition>();
  readonly Dictionary<int,BasinProgress> progress=new Dictionary<int,BasinProgress>();
  void Awake(){
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>(x.EndsWith("QA",StringComparison.OrdinalIgnoreCase)&&x!="-campaignQA"&&x!="-lostRealmQA"&&x!="-frontierQA"&&x!="-guidanceQA")||x=="-battlePreview"||x=="-titanPreview")){enabled=false;return;}
   Current=this;if(!director)director=GetComponent<SceneDirector>();bool guidanceVerification=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-guidanceQA"||x=="-guidanceSave");Store=new CampaignStore(guidanceVerification?Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/GuidanceVerification")):Path.Combine(Application.persistentDataPath,IsQA?"CampaignVerification":"CampaignV1"));
   State=IsQA?CampaignState.New():Store.Load()??CampaignState.New();saveBlocked=!IsQA&&Store.Error!=null;RecoverForge();ReconcileExpansionState();ReconcileFrontierState();
  }
  IEnumerator Start(){
   if(Current!=this)yield break;yield return null;CreateBonusNodes();oreOrigins=new Vector3[director.ores.Count];for(int i=0;i<oreOrigins.Length;i++)oreOrigins[i]=director.ores[i].transform.position;
   ApplyLayer();CreateSurfaceSites();CreateCompanions();CreateFrontierCamp();SetupPresentation();director.player.position=new Vector3(State.surfaceX,director.surfaceY,0);
   if(saveBlocked){Notice("存档读取异常，已保留原文件。请在手账中选择新故事或重试读档。");Open("system");yield break;}
   if(State.region>0&&!State.Has("rift-destroyed")){int area=State.region;State.region=0;EnterRegion(area);}
   else if(State.underground)yield return director.Travel(true);
   if(State.Has("detonation-active"))yield return Restore("before-core");
   if(Store.RecoveredFromBackup)Notice("主存档损坏，已从备份恢复；原文件已保留。");
   if(State.Has("rift-destroyed")&&!State.saviorComplete)Notice("污染源已经关闭。营地等待你的决定。");
  }
  void RecoverForge(){if(State.pendingForge<0)return;var id=(ItemId)State.pendingForge;if(!State.bag.Add(id,1)&&!State.warehouse.Add(id,1)){var crate=new SavedCrate{id=Guid.NewGuid().ToString("N"),region=0,position=Vector3.zero};crate.items.Add(id,1);State.crates.Add(crate);}State.pendingForge=-1;}
  public void Notice(string message){toast=message;toastAge=3;InteractionFeel.Play(FeelCue.Click,.35f);}
  public void Changed(){InvalidateGuidance();director.inventory.NotifyChanged();Save();}
  public bool Save(string slot="campaign"){
   if(loading||saveBlocked||State==null)return false;
   if(Explorer&&Explorer.expedition&&Explorer.expedition.Initialized){Explorer.expedition.Save();Explorer.arsenal.Sync();State.health=Explorer.arena.Downed?100:Explorer.arena.Health;}
   State.surfaceX=director.player.position.x;State.underground=director.underground;
   foreach(var pair in progress){var target=State.Region(pair.Key);target.enemies.Clear();foreach(var e in pair.Value.enemyHealth)target.enemies.Add(new SavedEnemy{id=e.Key,health=e.Value});target.mined=new List<int>(pair.Value.mined);target.drops=pair.Value.drops;}
   try{Store.Save(State,slot);return true;}catch(Exception e){Debug.LogError("Campaign save failed: "+e.Message);toast="保存失败，请检查磁盘空间";toastAge=5;return false;}
  }
  public BasinProgress ProgressFor(int region){
   if(progress.TryGetValue(region,out var p))return p;p=new BasinProgress();var r=State.Region(region);foreach(var e in r.enemies)p.enemyHealth[e.id]=e.health;foreach(var id in r.mined)p.mined.Add(id);p.drops.AddRange(r.drops);p.magazine=State.magazine;p.reserve=State.bag.Count(ItemId.Ammo);p.health=State.health;progress[region]=p;return p;
  }
  public bool EnterRegion(int id){
   if(Busy||director.IsPaused||director.forge.IsOpen||director.battle.IsEngaged||director.Transitioning||director.hookState!=SceneDirector.HookState.Swinging||!State.coordinates.Contains(id)||State.Has("rift-destroyed"))return false;
   Close();State.region=id;var p=ProgressFor(id);p.magazine=State.magazine;p.reserve=State.bag.Count(ItemId.Ammo);p.health=State.health;Save();director.battle.Enter(null);return true;
  }
  public void ConfigureRegion(FirstPersonExplorer fps){RegionView=fps.gameObject.AddComponent<CampaignRegion>();RegionView.Configure(this,State.region);}
  public void Returned(){RegionView=null;State.region=0;Save();if(State.Has("rift-destroyed")&&!State.Has("ecology-chosen")){returnFlash=1;Open("remain");}}
  public bool CollectNode(CampaignNode node){
   string id=CampaignRules.NodeId(node.layer,node.index);if(State.minedNodes.Contains(id)||!State.bag.CanAdd(node.item,node.quantity))return false;
   State.bag.Add(node.item,node.quantity);State.minedNodes.Add(id);FrontierEvent("mining",id);
   if(node.fragment&&!State.coordinates.Contains(node.layer)){State.coordinates.Add(node.layer);Notice("发现空间碎片 · E 触碰核心");InteractionFeel.Play(FeelCue.Alert,.8f);}
   Changed();return true;
  }
  public int Collected(int layer){int n=0;for(int i=0;i<CampaignRules.NodeCount(layer);i++)if(State.minedNodes.Contains(CampaignRules.NodeId(layer,i)))n++;return n;}
  public bool LayerComplete(int layer)=>Collected(layer)==CampaignRules.NodeCount(layer);
  public bool StartDrill(){if(Busy||!director.underground||director.Transitioning||director.hookState!=SceneDirector.HookState.Swinging||!LayerComplete(State.layer)||State.layer<State.unlockedLayer||State.layer>=5)return false;StartCoroutine(Drill());return true;}
  IEnumerator Drill(){Busy=true;int next=State.layer+1;InteractionFeel.Play(FeelCue.Portal,.9f);float elapsed=0;var camera=director.worldCamera.transform;var origin=camera.position;while(elapsed<2.2f){elapsed+=Time.deltaTime;camera.position=origin+Vector3.right*(InteractionFeel.ReducedMotion?0:Mathf.Sin(elapsed*57)*.035f);yield return null;}camera.position=origin;State.unlockedLayer=Mathf.Max(State.unlockedLayer,next);State.layer=next;ApplyLayer();Busy=false;Notice("矿层 "+next+" 已贯通");Changed();}
  public bool SelectLayer(int layer){if(Busy||layer<1||layer>State.unlockedLayer||director.hookState!=SceneDirector.HookState.Swinging)return false;State.layer=layer;ApplyLayer();Changed();return true;}
  public void ApplyLayer(){
   if(oreOrigins==null)return;
   foreach(var definition in generated)if(definition)Destroy(definition);generated.Clear();
   for(int i=0;i<director.ores.Count;i++){
    var ore=director.ores[i];var extra=ore.GetComponent<CampaignNode>();if(extra&&extra.bonus){bool show=extra.bonusLayer==State.layer&&!State.minedNodes.Contains(CampaignRules.NodeId(extra.bonusLayer,extra.index));ore.collected=!show;ore.gameObject.SetActive(show);ore.transform.position=oreOrigins[i];if(show)extra.ConfigureVisual(director);continue;}bool available=i<CampaignRules.NodeCount(State.layer)&&!State.minedNodes.Contains(CampaignRules.NodeId(State.layer,i));ore.collected=!available;ore.gameObject.SetActive(available);ore.transform.position=oreOrigins[i];
    var node=ore.GetComponent<CampaignNode>();if(!node)node=ore.gameObject.AddComponent<CampaignNode>();node.layer=State.layer;node.index=i;node.item=CampaignRules.NodeItem(State.layer,i);node.quantity=CampaignRules.NodeQuantity(State.layer,i);node.fragment=i==CampaignRules.NodeCount(State.layer)-1;node.origin=oreOrigins[i];node.ConfigureVisual(director);
    var sample=ore.GetComponent<BuriedSample>();if(sample){var definition=ScriptableObject.CreateInstance<SubsurfaceDefinition>();generated.Add(definition);definition.contentId=CampaignRules.NodeId(State.layer,i);definition.payloadKind=BuriedPayloadKind.RepairMaterial;definition.repairResource=node.item==ItemId.AlienCrystal?RepairResource.RareComposite:CampaignRules.Metal(node.item);definition.materialQuantity=node.quantity;sample.Configure(definition,State.layer==1?MineralShell.AmberInclusion:sample.shell);}
   }
  }
  public static bool Transaction(CampaignState state,ItemStack[] cost,ItemStack result=null){
   var bag=state.bag.Copy();var warehouse=state.warehouse.Copy();
   foreach(var c in cost){if(c.count<=0||bag.Count(c.item)+warehouse.Count(c.item)<c.count)return false;int b=Math.Min(bag.Count(c.item),c.count);bag.Remove(c.item,b);warehouse.Remove(c.item,c.count-b);}
   if(result!=null&&!bag.Add(result.item,result.count))return false;state.bag=bag;state.warehouse=warehouse;return true;
  }
  public bool CraftAmmo(){if(!AtCamp||!State.Has("ammo-recipe")||!Transaction(State,CampaignRules.AmmoCost,new ItemStack(ItemId.Ammo,24)))return false;Notice("手枪弹药 +24");Changed();return true;}
  public bool CraftBomb(){if(!AtCamp||State.Has("bomb-crafted")||!State.Knows("J07")||State.unlockedLayer<5||!Transaction(State,CampaignRules.BombCost))return false;State.Flag("bomb-crafted");Notice("异晶炸弹已装入受保护任务栏");Changed();return true;}
  public bool Repair(int part){if(!AtShip||part<0||part>3||!State.Has("repair-plan")||State.repairs.Contains(part.ToString())||(part==3&&State.unlockedLayer<5)||!Transaction(State,CampaignRules.RepairCost(part)))return false;State.repairs.Add(part.ToString());Changed();return true;}
  public bool FuelShip(){if(!AtShip||State.repairs.Count!=4||State.Has("ship-fueled")||!Transaction(State,new[]{new ItemStack(ItemId.Fuel,8)}))return false;State.Flag("ship-fueled");Changed();return true;}
  public bool CalibrateNavigation(){if(!AtShip||State.repairs.Count!=4||!State.Has("repair-plan"))return false;State.Flag("ship-navigation");Changed();return true;}
  public bool EscapeReady=>State.repairs.Count==4&&State.Has("repair-plan")&&State.Has("ship-navigation")&&State.Has("ship-fueled");
  public bool Escape(){if(!AtShip||!EscapeReady)return false;if(!Save("before-flight"))return false;State.escapeComplete=true;State.Flag("escape-view");Save();StartCoroutine(Flight());return true;}
  public bool OpenWreckChest(){if(State.region!=1||RegionView==null||!RegionView.Near("chest"))return false;State.Flag("ammo-recipe");State.Flag("repair-plan");State.device=true;State.Learn("J01",1);ClaimSupplies(1);Changed();return true;}
  public void ClaimSupplies(int region){int fuel=region==1?4:6;if(!State.Has("supply-fuel-"+region)&&State.bag.Add(ItemId.Fuel,fuel))State.Flag("supply-fuel-"+region);if(!State.Has("supply-med-"+region)&&State.bag.Add(ItemId.Medkit,1))State.Flag("supply-med-"+region);}
  public bool CalibrateDevice(){if(!State.device||!Explorer||Vector3.Distance(Explorer.transform.position,Explorer.returnBeacon.position)>4)return false;State.calibrated=true;State.Learn("J02",State.region);Changed();return true;}
  public void Learn(string id){State.Learn(id,State.region);Changed();}
  public bool UseMedkit(){if(State.bag.Count(ItemId.Medkit)==0)return false;bool used=Explorer?Explorer.arena.Heal(25):State.health<100;if(!used)return false;if(!Explorer)State.health=Math.Min(100,State.health+25);State.bag.Remove(ItemId.Medkit,1);Changed();return true;}
  public SavedCrate DropOnDeath(Vector3 point){
   var c=new SavedCrate{id=Guid.NewGuid().ToString("N"),region=State.region,position=point,items=State.bag.Copy()};State.bag=new ItemContainer(State.bag.capacity);State.health=100;
   if(c.items.stacks.Count>0){if(NavMesh.SamplePosition(point,out var hit,10,NavMesh.AllAreas))c.position=hit.position;else if(Explorer)c.position=Explorer.returnBeacon.position+Vector3.right*3;State.crates.Add(c);if(RegionView)RegionView.SpawnCrate(c);}
   Changed();return c;
  }
  public bool RecoverCrate(string id){var c=State.crates.Find(x=>x.id==id);if(c==null)return false;bool near=c.region==0?AtCamp:State.region==c.region&&Explorer&&Vector3.Distance(Explorer.transform.position,c.position)<4;if(!near)return false;bool any=false;foreach(var s in new List<ItemStack>(c.items.stacks)){int n=Math.Min(s.count,FreeSpace(State.bag,s.item));if(n>0){c.items.TransferTo(State.bag,s.item,n);any=true;}}if(c.items.stacks.Count==0)State.crates.Remove(c);if(any){InteractionFeel.Play(FeelCue.Pickup,.8f);Changed();}return any;}
  static int FreeSpace(ItemContainer c,ItemId id){int n=(c.capacity-c.stacks.Count)*99;foreach(var s in c.stacks)if(s.item==id)n+=99-s.count;return n;}
  public bool TransportCrates(){if(!AtCamp&&!State.Has("boss-defeated"))return false;foreach(var c in State.crates){c.region=0;c.position=Vector3.zero;}Changed();return true;}
  public int Available(ItemId item){
   int n=State.bag.Count(item)+State.warehouse.Count(item);foreach(var c in State.crates)n+=c.items.Count(item);
   if((int)item<5){var ingot=(ItemId)((int)item+5);n+=State.bag.Count(ingot)+State.warehouse.Count(ingot);foreach(var c in State.crates)n+=c.items.Count(ingot);}
   for(int l=1;l<=State.unlockedLayer;l++)for(int i=0;i<CampaignRules.NodeCount(l);i++)if(CampaignRules.NodeItem(l,i)==item&&!State.minedNodes.Contains(CampaignRules.NodeId(l,i)))n+=CampaignRules.NodeQuantity(l,i);
   for(int area=2;area<=4;area++)if(State.coordinates.Contains(area)){var region=State.regions.Find(r=>r.id==area);progress.TryGetValue(area,out var active);for(int i=0;i<12;i++){var type=area==2?(i%2==0?ItemId.IronOre:ItemId.CopperOre):area==3?ItemId.CobaltOre:ItemId.StarOre;bool mined=active!=null?active.mined.Contains(1400+i):region!=null&&region.mined.Contains(1400+i);if(type==item&&!mined)n++;}}
   if(item==ItemId.Fuel)for(int area=1;area<=State.unlockedLayer;area++)if(!State.Has("supply-fuel-"+area))n+=area==1?4:6;
   foreach(var r in State.regions)foreach(var drop in r.drops)if(!drop.collected&&DropItem(drop.kind)==item)n+=DropCount(drop.kind);
   return n;
  }
  public int Needed(ItemId item){
   int n=0;for(int i=0;i<4;i++)if(!State.repairs.Contains(i.ToString()))foreach(var s in CampaignRules.RepairCost(i))if(s.item==item||((int)item<5&&(int)s.item==(int)item+5))n+=s.count;
   if(!State.Has("bomb-crafted"))foreach(var s in CampaignRules.BombCost)if(s.item==item||((int)item<5&&(int)s.item==(int)item+5))n+=s.count;
   if(item==ItemId.Fuel){if(!State.Has("ship-fueled"))n+=8;n+=2;}if(item==ItemId.PurpleOre)n+=4;return Math.Max(n,(int)item<5?2:item==ItemId.AlienCrystal?2:0);
  }
  public bool CanEmergency(ItemId item)=>CampaignRules.MinimumLayer(item)<=State.unlockedLayer&&((int)item<5||item==ItemId.Fuel||item==ItemId.AlienCrystal)&&Available(item)<Needed(item);
  public bool CompleteEmergency(ItemId item){if(!AtCamp||!CanEmergency(item)||!State.bag.Add(item,1))return false;Changed();return true;}
  public static ItemId DropItem(BasinLootKind kind)=>kind==BasinLootKind.Fuel?ItemId.Fuel:kind==BasinLootKind.Ammo?ItemId.Ammo:kind==BasinLootKind.Medkit?ItemId.Medkit:ItemId.IronOre;
  public static int DropCount(BasinLootKind kind)=>kind==BasinLootKind.Fuel?2:kind==BasinLootKind.Ammo?18:1;
  public bool SavingReady=>State.Knows("J04")&&State.Knows("J06")&&State.Knows("J07")&&State.Has("bomb-crafted")&&State.Has("boss-defeated")&&State.Has("coupling-cut")&&State.Has("evac-anchor")&&State.Has("life-support-checked");
  public bool InstallBomb(){if(State.region!=5||!RegionView||!RegionView.Near("core")||!SavingReady)return false;State.Flag("bomb-installed");TransportCrates();Changed();return true;}
  public bool BeginDetonation(){if(State.region!=5||!RegionView||!RegionView.Near("core")||!SavingReady||!State.Has("bomb-installed")||BombSeconds>=0)return false;Close();if(!Save("before-core"))return false;BombSeconds=45;State.Flag("detonation-active");Save();Notice("核心崩解 · 返回信标跃迁");return true;}
  public bool Evacuate(){if(BombSeconds<=0||!Explorer||Vector3.Distance(Explorer.transform.position,Explorer.returnBeacon.position)>4)return false;BombSeconds=-1;State.flags.Remove("detonation-active");State.Flag("rift-destroyed");Close();director.battle.ReturnToExpedition();InteractionFeel.Play(FeelCue.Portal,1);return true;}
  public IEnumerator Restore(string slot){
   var restored=Store.Load(slot);if(restored==null){Notice("没有可用的检查点");yield break;}director.CancelTravel();loading=true;bool escape=State.escapeComplete,savior=State.saviorComplete;Close();BombSeconds=-1;
   if(director.battle.IsEngaged){director.battle.ReturnToExpedition();while(director.battle.IsEngaged)yield return null;}
   Close();State=restored;State.escapeComplete|=escape;State.saviorComplete|=savior;State.region=0;State.underground=false;State.flags.Remove("escape-view");progress.Clear();RecoverForge();ReconcileExpansionState();ReconcileFrontierState();director.worldCamera.transform.position=new Vector3(0,0,-20);director.underground=false;director.player.position=new Vector3(State.surfaceX,director.surfaceY,0);ApplyLayer();loading=false;saveBlocked=false;Changed();Notice("已恢复检查点，物资与任务进度已保留");
  }
  public IEnumerator NewGame(){director.CancelTravel();Close();if(director.battle.IsEngaged){director.battle.ReturnToExpedition();while(director.battle.IsEngaged)yield return null;}if(!saveBlocked)Save("previous-story");State=CampaignState.New();ReconcileFrontierState();progress.Clear();saveBlocked=false;BombSeconds=-1;director.underground=false;director.worldCamera.transform.position=new Vector3(0,0,-20);director.player.position=new Vector3(-10,director.surfaceY,0);ApplyLayer();Changed();}
  void Update(){
   if(Current!=this||State==null)return;toastAge-=Time.unscaledDeltaTime;
   if(Input.GetKeyDown(KeyCode.Escape)&&Modal){Close();return;}
   if(!Busy&&!director.forge.IsOpen&&!director.battle.IsTransitioning){if(Input.GetKeyDown(KeyCode.B))Toggle("bag");if(Input.GetKeyDown(KeyCode.J))Toggle("journal");if(Input.GetKeyDown(KeyCode.F5)){director.CompleteTravel();Save();consumedFrame=Time.frameCount;}if(AtCamp&&Input.GetKeyDown(KeyCode.C))Toggle("frontier");}
   TickEmergency();TickEcology();TickPresentation();TickTreatment();TickCompanions();TickFrontierCamp();TickGuidance();
   if(BombSeconds>=0&&!Modal&&Explorer&&!Explorer.Paused&&!director.battle.IsTransitioning){BombSeconds-=Time.deltaTime;if(BombSeconds<=0){BombSeconds=-1;StartCoroutine(Restore("before-core"));}}
   autosave+=Time.unscaledDeltaTime;if(autosave>12){autosave=0;Save();}
  }
  void OnApplicationQuit(){Save();}
  void OnApplicationPause(bool paused){if(paused)Save();}
  void OnDestroy(){if(campPanel)Destroy(campPanel);if(campPanelTexture)Destroy(campPanelTexture);if(coreMaterial)Destroy(coreMaterial);foreach(var d in generated)if(d)Destroy(d);if(Current==this)Current=null;}
 }
}
