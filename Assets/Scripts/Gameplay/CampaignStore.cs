using System;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed class CampaignStore {
  public readonly string directory;
  public string Error {get;private set;}
  public bool RecoveredFromBackup {get;private set;}
  public CampaignStore(string path){directory=path;Directory.CreateDirectory(path);}
  public string PathFor(string slot)=>Path.Combine(directory,slot+".json");
  public void Save(CampaignState state,string slot="campaign"){
   state.lastSaved=DateTime.UtcNow.ToString("s");string path=PathFor(slot),temp=path+".tmp";
   File.WriteAllText(temp,JsonUtility.ToJson(state,true));
   if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
  }
  public CampaignState Load(string slot="campaign"){
   Error=null;RecoveredFromBackup=false;string path=PathFor(slot);if(!File.Exists(path)&&!File.Exists(path+".bak"))return null;
   foreach(var file in new[]{path,path+".bak"}){
    try{if(!File.Exists(file))continue;var s=JsonUtility.FromJson<CampaignState>(File.ReadAllText(file));if(s==null||(s.version!=1&&s.version!=2)||s.bag==null||s.warehouse==null||s.unlockedLayer<1||s.unlockedLayer>5||s.layer<1||s.layer>s.unlockedLayer||s.region<0||s.region>5||s.magazine<0||s.magazine>12||s.coordinates==null||s.flags==null||s.minedNodes==null||s.repairs==null||s.journal==null||s.regions==null||s.crates==null)throw new InvalidDataException("不兼容或损坏的存档");Validate(s.bag);Validate(s.warehouse);foreach(var crate in s.crates)Validate(crate.items);foreach(var r in s.regions)if(r.enemies==null||r.mined==null||r.drops==null)throw new InvalidDataException("区域存档损坏");if(s.version==1){var original=path+".v1-"+DateTime.UtcNow.Ticks+".json";File.Copy(file,original);s.version=2;s.shotgunMagazine=8;s.railMagazine=3;s.selectedWeapon=0;}if(s.shotgunMagazine<0||s.shotgunMagazine>8||s.railMagazine<0||s.railMagazine>3||s.selectedWeapon<0||s.selectedWeapon>2)throw new InvalidDataException("装备存档损坏");if(s.expansionProgress==null)s.expansionProgress=new System.Collections.Generic.List<string>();if(s.craftedModules==null)s.craftedModules=new System.Collections.Generic.List<string>();if(s.equippedModules==null)s.equippedModules=new System.Collections.Generic.List<string>();ValidateEquipment(s);CampaignRuntime.ValidateFrontierState(s);if(file!=path){if(File.Exists(path))File.Copy(path,path+".corrupt-"+DateTime.UtcNow.Ticks);File.Copy(file,path,true);RecoveredFromBackup=true;}Error=null;return s;}
    catch(Exception e){Error=e.Message;}
   }return null;
  }
  static void ValidateEquipment(CampaignState s){
   string[] ids={"stamina","reload","magnet","weakness"};var crafted=new System.Collections.Generic.HashSet<string>();foreach(var id in s.craftedModules)if(Array.IndexOf(ids,id)<0||!crafted.Add(id))throw new InvalidDataException("模块制作记录损坏");
   var equipped=new System.Collections.Generic.HashSet<string>();if(s.equippedModules.Count>2)throw new InvalidDataException("模块槽位损坏");foreach(var id in s.equippedModules)if(!crafted.Contains(id)||!equipped.Add(id))throw new InvalidDataException("模块安装记录损坏");
   if(float.IsNaN(s.pulseCooldown)||float.IsInfinity(s.pulseCooldown)||s.pulseCooldown<0||s.pulseCooldown>18.1f||s.health<0||s.health>100||s.shotgunMagazine%2!=0||s.railMagazine%3!=0)throw new InvalidDataException("角色状态损坏");
   var areas=new System.Collections.Generic.HashSet<int>();foreach(var area in s.coordinates)if(area<1||area>5||!areas.Add(area))throw new InvalidDataException("区域坐标损坏");
   if(s.trackedObjective==null)s.trackedObjective="";
  }
  static void Validate(ItemContainer c){if(c==null||c.capacity<1||c.capacity>48||c.stacks==null||c.stacks.Count>c.capacity)throw new InvalidDataException("容器损坏");foreach(var s in c.stacks)if(s==null||s.count<1||s.count>99||(int)s.item<0||(int)s.item>=CampaignRules.ItemNames.Length)throw new InvalidDataException("物品损坏");}
  public static CampaignState Copy(CampaignState state)=>JsonUtility.FromJson<CampaignState>(JsonUtility.ToJson(state));
 }
}
