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
  readonly int[] metals=new int[4];
  UnknownOre reserved;
  public int FuelCount {get;private set;}
  int gatheredId=-100000;
  public void AddFuel(int amount){FuelCount+=Math.Max(0,amount);Changed?.Invoke();}
  public void AddGathered(RepairResource metal){unknown.Enqueue(new UnknownOre{gatheredMetal=metal,sourceId=gatheredId--});Changed?.Invoke();}
  public int UnknownCount=>unknown.Count;
  public int MetalTotal=>metals[0]+metals[1]+metals[2]+metals[3];
  public bool HasReservation=>reserved!=null;
  public event Action Changed;
  public int MetalCount(RepairResource metal)=>metals[(int)metal];
  public static string MetalName(RepairResource metal){switch(metal){case RepairResource.CopperCompound:return "赤辉铜锭";case RepairResource.CobaltSilicate:return "霜钴锭";case RepairResource.RareComposite:return "星髓合金锭";default:return "陨铁锭";}}
  public static string OreName(RepairResource metal){switch(metal){case RepairResource.CopperCompound:return "赤辉铜矿";case RepairResource.CobaltSilicate:return "霜蓝钴矿";case RepairResource.RareComposite:return "星髓矿";default:return "陨铁矿";}}
  public static Color MetalColor(RepairResource metal){switch(metal){case RepairResource.CopperCompound:return new Color(1,.47f,.22f);case RepairResource.CobaltSilicate:return new Color(.30f,.79f,1);case RepairResource.RareComposite:return new Color(.73f,.48f,1);default:return new Color(.67f,.83f,.94f);}}
  public bool AddRecovered(SubsurfaceDefinition source,int sourceId){
   if(source&&source.payloadKind==BuriedPayloadKind.DormantEncounter)return false;
   if(!recoveredSources.Add(sourceId))return false;
   unknown.Enqueue(new UnknownOre{source=source,sourceId=sourceId});Changed?.Invoke();return true;
  }
  public bool TryReserve(out UnknownOre ore){
   ore=null;if(reserved!=null||unknown.Count==0)return false;
   reserved=unknown.Dequeue();ore=reserved;Changed?.Invoke();return true;
  }
  public bool Resolve(UnknownOre ore,bool success){
   if(ore==null||!ReferenceEquals(ore,reserved))return false;
   if(success)metals[(int)ore.Metal]++;
   reserved=null;Changed?.Invoke();return true;
  }
 }
}
