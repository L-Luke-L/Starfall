using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public enum ItemId {PurpleOre,IronOre,CopperOre,CobaltOre,StarOre,PurpleIngot,IronIngot,CopperIngot,CobaltIngot,StarIngot,Fuel,Ammo,Medkit,AlienCrystal,MachineParts}
 [Serializable] public sealed class ItemStack {public ItemId item;public int count;public ItemStack(ItemId id,int n){item=id;count=n;}public ItemStack Copy()=>new ItemStack(item,count);}
 [Serializable] public sealed class ItemContainer {
  public int capacity;public List<ItemStack> stacks=new List<ItemStack>();
  public ItemContainer(int slots){capacity=slots;}
  public int Count(ItemId item){int n=0;foreach(var s in stacks)if(s.item==item)n+=s.count;return n;}
  public ItemContainer Copy(){var c=new ItemContainer(capacity);foreach(var s in stacks)c.stacks.Add(s.Copy());return c;}
  public bool CanAdd(ItemId item,int count){if(count<0)return false;int space=(capacity-stacks.Count)*99;foreach(var s in stacks)if(s.item==item)space+=99-s.count;return space>=count;}
  public bool Add(ItemId item,int count){if(count<0||!CanAdd(item,count))return false;foreach(var s in stacks){if(s.item!=item)continue;int n=Math.Min(99-s.count,count);s.count+=n;count-=n;}while(count>0){int n=Math.Min(99,count);stacks.Add(new ItemStack(item,n));count-=n;}return true;}
  public bool Remove(ItemId item,int count){if(count<0||Count(item)<count)return false;for(int i=stacks.Count-1;i>=0&&count>0;i--){if(stacks[i].item!=item)continue;int n=Math.Min(count,stacks[i].count);stacks[i].count-=n;count-=n;if(stacks[i].count==0)stacks.RemoveAt(i);}return true;}
  public bool TransferTo(ItemContainer other,ItemId item,int count){if(other==this||count<=0||Count(item)<count||!other.CanAdd(item,count))return false;Remove(item,count);other.Add(item,count);return true;}
  public bool Split(int index){if(index<0||index>=stacks.Count||stacks.Count>=capacity||stacks[index].count<2)return false;var s=stacks[index];int n=s.count/2;s.count-=n;stacks.Insert(index+1,new ItemStack(s.item,n));return true;}
  public void Sort(){var sums=new int[CampaignRules.ItemNames.Length];foreach(var s in stacks)if((int)s.item>=0&&(int)s.item<sums.Length)sums[(int)s.item]+=s.count;stacks.Clear();for(int i=0;i<sums.Length;i++)Add((ItemId)i,sums[i]);}
 }
 [Serializable] public sealed class SavedEnemy {public string id;public int health;}
 [Serializable] public sealed class SavedCrate {public string id;public int region;public Vector3 position;public ItemContainer items=new ItemContainer(48);}
 [Serializable] public sealed class RegionState {public int id;public List<SavedEnemy> enemies=new List<SavedEnemy>();public List<int> mined=new List<int>();public List<BasinDrop> drops=new List<BasinDrop>();}
 [Serializable] public sealed class JournalRecord {public string id;public string acquired;public int region;}
 [Serializable] public sealed class CampaignState {
  public int version=2;public ItemContainer bag=new ItemContainer(16),warehouse=new ItemContainer(48);
  public int magazine=12,shotgunMagazine=8,railMagazine=3,selectedWeapon,health=100,unlockedLayer=1,layer=1,region;public float surfaceX=-10;
  public bool underground,device,calibrated,escapeComplete,saviorComplete;public List<int> coordinates=new List<int>();
  public List<string> minedNodes=new List<string>(),flags=new List<string>(),repairs=new List<string>();
  public List<JournalRecord> journal=new List<JournalRecord>();public List<RegionState> regions=new List<RegionState>();public List<SavedCrate> crates=new List<SavedCrate>();
  public List<string> expansionProgress=new List<string>(),craftedModules=new List<string>(),equippedModules=new List<string>();
  public int pendingForge=-1;public string lastSaved;
  public static CampaignState New(){var s=new CampaignState();s.bag.Add(ItemId.Ammo,24);s.bag.Add(ItemId.Fuel,2);return s;}
  public bool Has(string key)=>flags.Contains(key);
  public void Flag(string key){if(!Has(key))flags.Add(key);}
  public bool Knows(string id)=>journal.Exists(x=>x.id==id);
  public void Learn(string id,int area){if(!Knows(id))journal.Add(new JournalRecord{id=id,region=area,acquired=DateTime.UtcNow.ToString("s")});}
  public RegionState Region(int id){var r=regions.Find(x=>x.id==id);if(r==null){r=new RegionState{id=id};regions.Add(r);}return r;}
 }
 public static class CampaignRules {
  public static readonly string[] ItemNames={"紫铜原矿","陨铁原矿","赤辉铜原矿","霜蓝钴原矿","星髓原矿","紫铜锭","陨铁锭","赤辉铜锭","霜钴锭","星髓合金锭","燃料","备用弹药","急救剂","稳定异晶","机械组件"};
  public static readonly string[] Regions={"营地","探险船遗骸","撤离战场","同盟医疗站","军用船坞","D4空间核心"};
  public static readonly string[] RepairNames={"船体与支撑","配电与能源","引擎冷却","导航与稳定器"};
  public static ItemId Raw(RepairResource r)=>r==RepairResource.PurpleCopper?ItemId.PurpleOre:(ItemId)((int)r+1);
  public static RepairResource Metal(ItemId i)=>i==ItemId.PurpleOre?RepairResource.PurpleCopper:(RepairResource)((int)i-1);
  public static ItemId Ingot(RepairResource r)=>(ItemId)((int)Raw(r)+5);
  public static int NodeCount(int layer)=>layer==3?8:layer==4?10:12;
  public static ItemId NodeItem(int layer,int index)=>layer==1?ItemId.PurpleOre:layer==2?(index<6?ItemId.IronOre:ItemId.CopperOre):layer==3?ItemId.CobaltOre:layer==4?ItemId.StarOre:ItemId.AlienCrystal;
  public static int NodeQuantity(int layer,int index)=>layer==1?(index==11?1:index<7?3:2):layer==2?(index<8?2:1):1;
  public static string NodeId(int layer,int index)=>"c287b0e1-0a26-4dfb-9"+layer+"00-"+index.ToString("D12");
  public static int MinimumLayer(ItemId id)=>id==ItemId.AlienCrystal?5:id==ItemId.StarOre||id==ItemId.StarIngot?4:id==ItemId.CobaltOre||id==ItemId.CobaltIngot?3:id==ItemId.IronOre||id==ItemId.IronIngot||id==ItemId.CopperOre||id==ItemId.CopperIngot?2:1;
  public static ItemStack[] RepairCost(int part){switch(part){case 0:return new[]{new ItemStack(ItemId.IronIngot,6)};case 1:return new[]{new ItemStack(ItemId.PurpleIngot,2),new ItemStack(ItemId.CopperIngot,4)};case 2:return new[]{new ItemStack(ItemId.CobaltIngot,4)};default:return new[]{new ItemStack(ItemId.StarIngot,3),new ItemStack(ItemId.AlienCrystal,2)};}}
  public static ItemStack[] AmmoCost=>new[]{new ItemStack(ItemId.PurpleOre,2),new ItemStack(ItemId.Fuel,1)};
  public static ItemStack[] BombCost=>new[]{new ItemStack(ItemId.AlienCrystal,6),new ItemStack(ItemId.Fuel,4),new ItemStack(ItemId.StarIngot,2)};
  public static int GunDamage(float multiplier)=>Mathf.RoundToInt(26*.85f*multiplier);
  public static int EnemyDamage(int damage)=>Mathf.RoundToInt(damage*1.30f);
  public static float EnemySpeed(float speed)=>speed*1.10f;
  public static void ForgeProfile(RepairResource metal,out float window,out float speed,out float duration,out int rhythms){
   if(metal==RepairResource.PurpleCopper){window=2;speed=.60f;duration=1.50f;rhythms=1;}
   else if(metal==RepairResource.FerricAlloy||metal==RepairResource.CopperCompound){window=1.65f;speed=.75f;duration=1.30f;rhythms=2;}
   else if(metal==RepairResource.CobaltSilicate){window=1.30f;speed=.90f;duration=1.15f;rhythms=3;}
   else{window=speed=duration=1;rhythms=3;}
  }
 }
}
