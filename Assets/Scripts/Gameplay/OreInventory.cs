using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 [Serializable] public sealed class UnknownOre {
  public SubsurfaceDefinition source;
  public int sourceId;
  public RepairResource gatheredMetal;
  public RepairResource Metal=>source?source.repairResource:gatheredMetal;
 }
 public sealed class OreInventory : MonoBehaviour {
  readonly Queue<UnknownOre> unknown=new Queue<UnknownOre>();
  readonly HashSet<int> recoveredSources=new HashSet<int>();
  readonly int[] metals=new int[5];
  CampaignRuntime Campaign=>CampaignRuntime.Current;
  public RepairResource SelectedMetal=RepairResource.PurpleCopper;
  UnknownOre reserved;
  int fuelCount;public int FuelCount {get=>Campaign?Campaign.State.bag.Count(ItemId.Fuel):fuelCount;private set=>fuelCount=value;}
  int gatheredId=-100000;
  public void NotifyChanged(){Changed?.Invoke();}
  public bool CanGather(RepairResource metal)=>!Campaign||Campaign.State.bag.CanAdd(CampaignRules.Raw(metal),1);
  public void AddFuel(int amount){if(Campaign){if(Campaign.State.bag.Add(ItemId.Fuel,Math.Max(0,amount)))Campaign.Changed();return;}FuelCount+=Math.Max(0,amount);Changed?.Invoke();}
  public void AddGathered(RepairResource metal){if(Campaign){if(Campaign.State.bag.Add(CampaignRules.Raw(metal),1))Campaign.Changed();return;}unknown.Enqueue(new UnknownOre{gatheredMetal=metal,sourceId=gatheredId--});Changed?.Invoke();}
  public int UnknownCount {get{if(!Campaign)return unknown.Count;int n=0;for(int i=0;i<5;i++)n+=Campaign.State.bag.Count((ItemId)i);return n;}}
  public int MetalTotal {get{int n=0;for(int i=0;i<5;i++)n+=MetalCount((RepairResource)i);return n;}}
  public bool HasReservation=>reserved!=null;
  public event Action Changed;
  public int MetalCount(RepairResource metal)=>Campaign?Campaign.State.bag.Count(CampaignRules.Ingot(metal)):metals[(int)metal];
  public static string MetalName(RepairResource metal){switch(metal){case RepairResource.PurpleCopper:return "紫铜锭";case RepairResource.CopperCompound:return "赤辉铜锭";case RepairResource.CobaltSilicate:return "霜钴锭";case RepairResource.RareComposite:return "星髓合金锭";default:return "陨铁锭";}}
  public static string OreName(RepairResource metal){switch(metal){case RepairResource.PurpleCopper:return "紫铜矿";case RepairResource.CopperCompound:return "赤辉铜矿";case RepairResource.CobaltSilicate:return "霜蓝钴矿";case RepairResource.RareComposite:return "星髓矿";default:return "陨铁矿";}}
  public static Color MetalColor(RepairResource metal){switch(metal){case RepairResource.PurpleCopper:return new Color(.93f,.48f,.35f);case RepairResource.CopperCompound:return new Color(1,.47f,.22f);case RepairResource.CobaltSilicate:return new Color(.30f,.79f,1);case RepairResource.RareComposite:return new Color(.73f,.48f,1);default:return new Color(.67f,.83f,.94f);}}
  public bool AddRecovered(SubsurfaceDefinition source,int sourceId){
   if(Campaign){string key="recovered-"+sourceId;if(Campaign.State.Has(key)||!Campaign.State.bag.Add(CampaignRules.Raw(source?source.repairResource:RepairResource.PurpleCopper),source?source.materialQuantity:1))return false;Campaign.State.Flag(key);Campaign.Changed();return true;}
   if(source&&source.payloadKind==BuriedPayloadKind.DormantEncounter)return false;
   if(!recoveredSources.Add(sourceId))return false;
   unknown.Enqueue(new UnknownOre{source=source,sourceId=sourceId});Changed?.Invoke();return true;
  }
  public bool TryReserve(out UnknownOre ore){
   if(Campaign){ore=null;if(reserved!=null||Campaign.State.pendingForge>=0)return false;var raw=CampaignRules.Raw(SelectedMetal);var bag=Campaign.State.bag.Copy();var warehouse=Campaign.State.warehouse.Copy();if(!bag.Remove(raw,1)&&(!Campaign.AtCamp||!warehouse.Remove(raw,1)))return false;if(!bag.CanAdd(CampaignRules.Ingot(SelectedMetal),1))return false;Campaign.State.bag=bag;Campaign.State.warehouse=warehouse;Campaign.State.pendingForge=(int)raw;reserved=new UnknownOre{gatheredMetal=SelectedMetal};ore=reserved;Campaign.Changed();return true;}
   ore=null;if(reserved!=null||unknown.Count==0)return false;
   reserved=unknown.Dequeue();ore=reserved;Changed?.Invoke();return true;
  }
  public bool Resolve(UnknownOre ore,bool success){
   if(ore==null||!ReferenceEquals(ore,reserved))return false;
   if(Campaign){if(success){if(!Campaign.State.bag.Add(CampaignRules.Ingot(ore.Metal),1))return false;Campaign.State.Flag("identified-"+(int)ore.Metal);}Campaign.State.pendingForge=-1;reserved=null;Campaign.Changed();return true;}
   if(success)metals[(int)ore.Metal]++;
   reserved=null;Changed?.Invoke();return true;
  }
 }
}
