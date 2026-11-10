using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Starfall {
 public static class CampaignStateAuditChecks {
  static CampaignState Healthy(){var state=CampaignState.New();state.surfaceX=-9;var region=state.Region(1);region.enemies.Add(new SavedEnemy{id="stable-enemy",health=97});region.drops.Add(new BasinDrop{id=0,kind=BasinLootKind.Ammo,position=new Vector3(3,1,-9)});var crate=new SavedCrate{id="stable-crate",region=1,position=new Vector3(4,0,-8)};crate.items.Add(ItemId.StarOre,3);state.crates.Add(crate);return state;}
  public static void RunStoreChecks(Action<string,bool> check){
   string folder=Path.Combine(Path.GetTempPath(),"StarfallStateAudit-"+Guid.NewGuid().ToString("N"));var store=new CampaignStore(folder);
   try{
    Action<string,Action<CampaignState>> corrupt=(slot,mutate)=>{
     store.Save(Healthy(),slot);store.Save(Healthy(),slot);string path=store.PathFor(slot),backup=File.ReadAllText(path+".bak");var broken=Healthy();mutate(broken);string json=JsonUtility.ToJson(broken,true);File.WriteAllText(path,json);var loaded=store.Load(slot);
     check("Store recovers "+slot+" from valid backup",loaded!=null&&store.RecoveredFromBackup&&store.Error==null&&loaded.crates[0].items.Count(ItemId.StarOre)==3&&File.ReadAllText(path)==backup);
     bool preserved=false;foreach(string original in Directory.GetFiles(folder,slot+".json.corrupt-*"))preserved|=File.ReadAllText(original)==json;check("Store preserves corrupt original "+slot,preserved);
    };
    corrupt("surface-nan",s=>s.surfaceX=float.NaN);corrupt("surface-infinity",s=>s.surfaceX=float.PositiveInfinity);
    corrupt("crate-position",s=>s.crates[0].position=new Vector3(1,float.NaN,2));corrupt("drop-position",s=>s.regions[0].drops[0].position=new Vector3(float.NegativeInfinity,1,2));
    corrupt("crate-region-low",s=>s.crates[0].region=-1);corrupt("crate-region-high",s=>s.crates[0].region=6);
    corrupt("duplicate-crate",s=>s.crates.Add(new SavedCrate{id="stable-crate",region=1}));
    corrupt("region-low",s=>s.regions[0].id=-1);corrupt("region-high",s=>s.regions[0].id=6);corrupt("duplicate-region",s=>s.regions.Add(new RegionState{id=1}));
    corrupt("duplicate-enemy",s=>s.regions[0].enemies.Add(new SavedEnemy{id="stable-enemy",health=12}));
    var opaque=Healthy();opaque.regions[0].drops[0].id=-1;opaque.regions[0].drops.Add(new BasinDrop{id=-1,kind=BasinLootKind.Fuel,position=Vector3.right});store.Save(opaque,"opaque-drop-identities");var retained=store.Load("opaque-drop-identities");check("Opaque duplicate or negative legacy drop IDs retain both independent items",retained!=null&&!store.RecoveredFromBackup&&retained.regions[0].drops.Count==2&&retained.regions[0].drops[0].kind==BasinLootKind.Ammo&&retained.regions[0].drops[1].kind==BasinLootKind.Fuel);
    var legacy=Healthy();legacy.regions[0].id=0;legacy.regions[0].enemies[0].id="unknown-future-content";legacy.regions[0].enemies[0].health=9999;legacy.regions.Add(new RegionState{id=2});legacy.regions[1].enemies.Add(new SavedEnemy{id="unknown-future-content",health=9999});legacy.regions[1].drops.Add(new BasinDrop{id=0,kind=BasinLootKind.Fuel});store.Save(legacy,"compatible-identities");var valid=store.Load("compatible-identities");check("Store keeps region zero, future enemy IDs and per-region identities compatible",valid!=null&&!store.RecoveredFromBackup&&valid.regions.Count==2&&valid.regions[0].enemies[0].health==9999);
    var noBackup=Healthy();noBackup.crates[0].region=99;string brokenJson=JsonUtility.ToJson(noBackup);File.WriteAllText(store.PathFor("no-backup"),brokenJson);check("Invalid world records without backup are rejected without overwriting",store.Load("no-backup")==null&&!string.IsNullOrEmpty(store.Error)&&File.ReadAllText(store.PathFor("no-backup"))==brokenJson);
   }finally{Directory.Delete(folder,true);}
  }
  public static IEnumerator RunTreatmentChecks(CampaignRuntime campaign,Action<string,bool> check){
   var director=campaign.director;var timer=typeof(CampaignRuntime).GetField("treatmentTime",BindingFlags.Instance|BindingFlags.NonPublic);int oldHealth=campaign.State.health;Vector3 oldPosition=director.player.position;bool oldPause=director.IsPaused,addedDoctor=!campaign.Exp("doctor-rescued");
   try{
    campaign.Close();director.SetPaused(false);director.player.position=new Vector3(director.camp.transform.position.x,director.surfaceY,0);if(addedDoctor)campaign.State.expansionProgress.Add("doctor-rescued");campaign.State.health=50;
    check("Doctor treatment starts at camp",campaign.StartTreatment());director.SetPaused(true);float remaining=(float)timer.GetValue(campaign);yield return new WaitForSecondsRealtime(.4f);
    check("Game pause freezes doctor treatment and health",Mathf.Abs((float)timer.GetValue(campaign)-remaining)<.01f&&campaign.State.health==50);
    director.SetPaused(false);float end=Time.realtimeSinceStartup+5;while(campaign.State.health<100&&Time.realtimeSinceStartup<end)yield return null;check("Doctor treatment resumes and completes after unpausing",campaign.State.health==100&&(float)timer.GetValue(campaign)<=0);
   }finally{timer.SetValue(campaign,0f);campaign.State.health=oldHealth;if(addedDoctor)campaign.State.expansionProgress.Remove("doctor-rescued");director.player.position=oldPosition;director.SetPaused(oldPause);}
  }
  public static IEnumerator RunEmergencyChecks(CampaignRuntime campaign,Action<string,bool> check){
   var state=campaign.State;var bag=state.bag;var warehouse=state.warehouse;var crates=state.crates;var mined=state.minedNodes;var regions=state.regions;var director=campaign.director;Vector3 position=director.player.position;bool paused=director.IsPaused;int unlocked=state.unlockedLayer;float surfaceX=state.surfaceX;bool underground=state.underground;
   try{
    campaign.Close();director.SetPaused(false);director.player.position=new Vector3(director.camp.transform.position.x,director.surfaceY,0);state.unlockedLayer=1;state.bag=new ItemContainer(16);state.warehouse=new ItemContainer(48);state.crates=new List<SavedCrate>();state.regions=new List<RegionState>();state.minedNodes=new List<string>();for(int i=0;i<CampaignRules.NodeCount(1);i++)state.minedNodes.Add(CampaignRules.NodeId(1,i));state.bag.Add(ItemId.PurpleIngot,30);
    check("Refining every purple ore does not lock ammunition emergency recovery",campaign.CanEmergency(ItemId.PurpleOre));bool supplied=true;for(int i=0;i<4;i++)supplied&=campaign.CompleteEmergency(ItemId.PurpleOre);check("Real emergency actions replenish four raw ammunition ingredients then stop",supplied&&state.bag.Count(ItemId.PurpleOre)==4&&!campaign.CanEmergency(ItemId.PurpleOre)&&!campaign.CompleteEmergency(ItemId.PurpleOre));
    state.bag.Remove(ItemId.PurpleOre,4);state.warehouse.Add(ItemId.PurpleOre,4);check("Warehouse raw ingredients count toward emergency reserve",!campaign.CanEmergency(ItemId.PurpleOre));state.warehouse.Remove(ItemId.PurpleOre,4);var supply=new SavedCrate{id="raw-reserve",region=0};supply.items.Add(ItemId.PurpleOre,4);state.crates.Add(supply);check("Recovery-crate raw ingredients count toward emergency reserve",!campaign.CanEmergency(ItemId.PurpleOre));state.crates.Clear();state.minedNodes.Clear();check("Unmined purple ore prevents unnecessary emergency duplication",!campaign.CanEmergency(ItemId.PurpleOre));
    state.bag=new ItemContainer(16);var recovery=new SavedCrate{id="multi-stack",region=0};recovery.items.Add(ItemId.Ammo,174);state.crates.Add(recovery);check("A single crate action recovers every same-item stack",campaign.RecoverCrate(recovery.id)&&state.bag.Count(ItemId.Ammo)==174&&state.crates.Count==0);
    state.bag=new ItemContainer(1);state.bag.Add(ItemId.Ammo,90);recovery=new SavedCrate{id="partial-stack",region=0};recovery.items.Add(ItemId.Ammo,174);state.crates.Add(recovery);check("Limited crate recovery transfers exactly the available space",campaign.RecoverCrate(recovery.id)&&state.bag.Count(ItemId.Ammo)==99&&recovery.items.Count(ItemId.Ammo)==165);state.crates.Clear();
    var deposit=typeof(CampaignRuntime).GetMethod("DepositMatchingItems",BindingFlags.Instance|BindingFlags.NonPublic);state.bag=new ItemContainer(16);state.bag.Add(ItemId.Ammo,174);state.warehouse=new ItemContainer(48);state.warehouse.Add(ItemId.Ammo,1);int moved=(int)deposit.Invoke(campaign,null);check("Quick deposit transfers every matching same-item stack in one action",moved==174&&state.bag.Count(ItemId.Ammo)==0&&state.warehouse.Count(ItemId.Ammo)==175);
    state.bag=new ItemContainer(16);state.bag.Add(ItemId.Ammo,174);state.warehouse=new ItemContainer(1);state.warehouse.Add(ItemId.Ammo,90);moved=(int)deposit.Invoke(campaign,null);check("Limited quick deposit conserves exact partial quantities",moved==9&&state.bag.Count(ItemId.Ammo)==165&&state.warehouse.Count(ItemId.Ammo)==99);yield return null;
   }finally{state.bag=bag;state.warehouse=warehouse;state.crates=crates;state.minedNodes=mined;state.regions=regions;state.unlockedLayer=unlocked;state.surfaceX=surfaceX;state.underground=underground;director.player.position=position;director.SetPaused(paused);campaign.InvalidateGuidance();}
  }
 }
}
