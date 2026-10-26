using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Starfall;

// Uses isolated generated saves; never opens or modifies the player's save directory.
public static class CampaignDataIntegrityVerification {
 static readonly List<string> results=new List<string>();static int failures;
 static void Check(string name,bool passed){results.Add((passed?"PASS ":"FAIL ")+name);if(!passed)failures++;Debug.Log("DATA_INTEGRITY_QA "+results[results.Count-1]);}
 static CampaignState Healthy(){
  var state=CampaignState.New();state.unlockedLayer=5;state.layer=3;state.region=2;state.health=83;
  state.bag.Add(ItemId.AlienCrystal,7);state.warehouse.Add(ItemId.StarIngot,3);state.repairs.Add("0");state.repairs.Add("2");
  state.journal.Add(new JournalRecord{id="J01",region=0,acquired="legacy timestamp"});state.journal.Add(new JournalRecord{id="future-chapter",region=5,acquired=""});
  var region=new RegionState{id=2};region.enemies.Add(new SavedEnemy{id="future-creature",health=9999});region.mined.Add(1400);region.drops.Add(new BasinDrop{id=7,kind=BasinLootKind.Ore,position=new Vector3(3,4,5)});state.regions.Add(region);
  var crate=new SavedCrate{id="legacy-crate",region=0};crate.items.Add(ItemId.Fuel,5);state.crates.Add(crate);
  state.craftedModules.Add("reload");state.equippedModules.Add("reload");return state;
 }
 static string Mutate(string json,Action<CampaignState> mutation){var state=JsonUtility.FromJson<CampaignState>(json);mutation(state);return JsonUtility.ToJson(state);}
 static void RecoveryCase(CampaignStore store,string slot,Func<string,string> corrupt){
  store.Save(Healthy(),slot);store.Save(Healthy(),slot);string path=store.PathFor(slot),backup=File.ReadAllText(path+".bak"),broken=corrupt(backup);
  File.WriteAllText(path,broken);var state=store.Load(slot);
  Check(slot+" falls back to valid backup",state!=null&&store.RecoveredFromBackup&&store.Error==null);
  Check(slot+" preserves complete backup data",state!=null&&JsonUtility.ToJson(state)==JsonUtility.ToJson(JsonUtility.FromJson<CampaignState>(backup))&&File.ReadAllText(path)==backup);
  bool retained=false;foreach(var file in Directory.GetFiles(store.directory,slot+".json.corrupt-*"))if(File.ReadAllText(file)==broken)retained=true;
  Check(slot+" retains damaged original",retained);
 }
 [MenuItem("Starfall/Verification/Check Save Data Integrity")]
 public static void Run(){
  results.Clear();failures=0;string temp=Path.Combine(Path.GetTempPath(),"StarfallDataIntegrity-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   var store=new CampaignStore(temp);store.Save(CampaignState.New(),"new-game");Check("New game with empty story records remains valid",store.Load("new-game")!=null&&!store.RecoveredFromBackup);
   var complete=Healthy();complete.repairs=new List<string>{"0","1","2","3"};store.Save(complete,"valid");var loaded=store.Load("valid");
   Check("Valid repair IDs and journal boundary regions load",loaded!=null&&loaded.repairs.Count==4&&loaded.journal[0].region==0&&loaded.journal[1].region==5&&!store.RecoveredFromBackup);
   Check("Unknown nonempty journal IDs and legacy timestamps survive",loaded!=null&&loaded.journal[1].id=="future-chapter"&&loaded.journal[0].acquired=="legacy timestamp");
   Check("Nested content IDs, health and crate contents remain compatible",loaded!=null&&loaded.regions[0].enemies[0].id=="future-creature"&&loaded.regions[0].enemies[0].health==9999&&loaded.regions[0].drops[0].id==7&&loaded.crates[0].id=="legacy-crate"&&loaded.crates[0].items.Count(ItemId.Fuel)==5);
   RecoveryCase(store,"journal-region-high",json=>Mutate(json,s=>s.journal[0].region=99));
   RecoveryCase(store,"journal-region-negative",json=>Mutate(json,s=>s.journal[0].region=-1));
   RecoveryCase(store,"journal-null",json=>Regex.Replace(json,"\"journal\"\\s*:\\s*\\[[^\\]]*\\]","\"journal\":[null]"));
   RecoveryCase(store,"journal-empty-id",json=>Mutate(json,s=>s.journal[0].id=""));
   RecoveryCase(store,"repair-out-of-range",json=>Mutate(json,s=>s.repairs.Add("4")));
   RecoveryCase(store,"repair-duplicate",json=>Mutate(json,s=>s.repairs.Add("0")));
   RecoveryCase(store,"repair-null",json=>Mutate(json,s=>s.repairs.Add(null)));
   RecoveryCase(store,"pending-forge-high",json=>Mutate(json,s=>s.pendingForge=999));
   RecoveryCase(store,"pending-forge-negative",json=>Mutate(json,s=>s.pendingForge=-2));
   // Unity's inline serializer materializes [null] as an empty region 0. That
   // object is safe, and region 0 is also used by the baseline encounter scene.
   string emptyRegion=Mutate(JsonUtility.ToJson(Healthy()),s=>s.regions.Clear()).Replace("\"regions\":[]","\"regions\":[null]");string emptyRegionPath=store.PathFor("region-null");File.WriteAllText(emptyRegionPath,emptyRegion);loaded=store.Load("region-null");
   Check("Materialized null region remains safe and compatible",loaded!=null&&!store.RecoveredFromBackup&&loaded.Region(0).enemies.Count==0&&loaded.Region(0).mined.Count==0&&loaded.Region(0).drops.Count==0);
   if(loaded!=null)CampaignRuntime.SeedFrontierHistory(loaded);Check("Materialized null region can reconcile without overwriting source",loaded!=null&&loaded.bag.Count(ItemId.AlienCrystal)==7&&File.ReadAllText(emptyRegionPath)==emptyRegion);
   var baseline=Healthy();baseline.regions[0].id=0;store.Save(baseline,"region-zero");loaded=store.Load("region-zero");Check("Explicit region 0 preserves baseline encounter progress",loaded!=null&&!store.RecoveredFromBackup&&loaded.Region(0).enemies[0].id=="future-creature");
   RecoveryCase(store,"enemy-null",json=>Mutate(json,s=>s.regions[0].enemies.Clear()).Replace("\"enemies\":[]","\"enemies\":[null]"));
   RecoveryCase(store,"enemy-null-id",json=>Mutate(json,s=>s.regions[0].enemies[0].id=null));
   RecoveryCase(store,"enemy-empty-id",json=>Mutate(json,s=>s.regions[0].enemies[0].id=""));
   RecoveryCase(store,"drop-null",json=>Mutate(json,s=>s.regions[0].drops.Clear()).Replace("\"drops\":[]","\"drops\":[null]"));
   RecoveryCase(store,"drop-none",json=>Mutate(json,s=>s.regions[0].drops[0].kind=BasinLootKind.None));
   RecoveryCase(store,"drop-undefined-kind",json=>Mutate(json,s=>s.regions[0].drops[0].kind=(BasinLootKind)999));
   var rewards=Healthy();rewards.regions[0].drops.Clear();foreach(var kind in new[]{BasinLootKind.Fuel,BasinLootKind.Ore,BasinLootKind.Ammo,BasinLootKind.Medkit})rewards.regions[0].drops.Add(new BasinDrop{id=rewards.regions[0].drops.Count,kind=kind});store.Save(rewards,"drop-valid-kinds");loaded=store.Load("drop-valid-kinds");Check("All four actual pickup rewards remain valid",loaded!=null&&!store.RecoveredFromBackup&&loaded.regions[0].drops.Count==4);
   RecoveryCase(store,"crate-null",json=>Mutate(json,s=>s.crates.Clear()).Replace("\"crates\":[]","\"crates\":[null]"));
   RecoveryCase(store,"crate-null-id",json=>Mutate(json,s=>s.crates[0].id=null));
   RecoveryCase(store,"crate-null-items",json=>Mutate(json,s=>s.crates[0].items=null));
   foreach(int ore in new[]{0,4}){var reserved=Healthy();reserved.pendingForge=ore;string slot="pending-forge-valid-"+ore;store.Save(reserved,slot);var restored=store.Load(slot);Check("Pending forge boundary "+ore+" remains valid",restored!=null&&restored.pendingForge==ore&&!store.RecoveredFromBackup&&restored.bag.Count(ItemId.AlienCrystal)==7);}
   var legacy=Healthy();legacy.version=1;legacy.shotgunMagazine=-1;legacy.railMagazine=-1;legacy.selectedWeapon=99;legacy.expansionProgress=null;legacy.craftedModules=null;legacy.equippedModules=null;
   store.Save(legacy,"legacy");string legacyPath=store.PathFor("legacy"),legacyJson=File.ReadAllText(legacyPath);loaded=store.Load("legacy");
   Check("Version 1 migration remains compatible",loaded!=null&&loaded.version==2&&loaded.shotgunMagazine==8&&loaded.railMagazine==3&&loaded.selectedWeapon==0&&loaded.expansionProgress!=null&&loaded.craftedModules!=null&&loaded.equippedModules!=null);
   Check("Version 1 migration retains inventory and story progress",loaded!=null&&loaded.bag.Count(ItemId.AlienCrystal)==7&&loaded.warehouse.Count(ItemId.StarIngot)==3&&loaded.repairs.Count==2&&loaded.journal.Count==2);
   var originals=Directory.GetFiles(temp,"legacy.json.v1-*.json");Check("Version 1 original is preserved",originals.Length==1&&File.ReadAllText(originals[0])==legacyJson);
   string unrecoverable=Mutate(JsonUtility.ToJson(Healthy()),s=>s.journal[0].region=99),unrecoverablePath=store.PathFor("no-backup");File.WriteAllText(unrecoverablePath,unrecoverable);
   Check("Damaged save without backup is rejected without overwriting",store.Load("no-backup")==null&&!store.RecoveredFromBackup&&!string.IsNullOrEmpty(store.Error)&&File.ReadAllText(unrecoverablePath)==unrecoverable);
  }catch(Exception exception){Check("Unhandled exception: "+exception,false);}
  finally{Directory.Delete(temp,true);}
  string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Reports/DataIntegrity"));Directory.CreateDirectory(report);File.WriteAllLines(Path.Combine(report,"data-integrity-report.txt"),results);
  Debug.Log(failures==0?"STARFALL_DATA_INTEGRITY_PASS":"STARFALL_DATA_INTEGRITY_FAIL "+failures);if(Application.isBatchMode)EditorApplication.Exit(failures==0?0:2);
 }
}
