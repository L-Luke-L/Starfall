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
    try{if(!File.Exists(file))continue;var s=JsonUtility.FromJson<CampaignState>(File.ReadAllText(file));if(s==null||s.version!=1||s.bag==null||s.warehouse==null||s.unlockedLayer<1||s.unlockedLayer>5||s.layer<1||s.layer>s.unlockedLayer||s.region<0||s.region>5||s.magazine<0||s.magazine>12||s.coordinates==null||s.flags==null||s.minedNodes==null||s.repairs==null||s.journal==null||s.regions==null||s.crates==null)throw new InvalidDataException("不兼容或损坏的存档");Validate(s.bag);Validate(s.warehouse);foreach(var crate in s.crates)Validate(crate.items);foreach(var r in s.regions)if(r.enemies==null||r.mined==null||r.drops==null)throw new InvalidDataException("区域存档损坏");if(file!=path){if(File.Exists(path))File.Copy(path,path+".corrupt-"+DateTime.UtcNow.Ticks);File.Copy(file,path,true);RecoveredFromBackup=true;}Error=null;return s;}
    catch(Exception e){Error=e.Message;}
   }return null;
  }
  static void Validate(ItemContainer c){if(c.capacity<1||c.capacity>48||c.stacks==null||c.stacks.Count>c.capacity)throw new InvalidDataException("容器损坏");foreach(var s in c.stacks)if(s.count<1||s.count>99||(int)s.item<0||(int)s.item>13)throw new InvalidDataException("物品损坏");}
  public static CampaignState Copy(CampaignState state)=>JsonUtility.FromJson<CampaignState>(JsonUtility.ToJson(state));
 }
}
