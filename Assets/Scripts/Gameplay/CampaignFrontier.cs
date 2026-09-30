using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  public int FrontierCount(string key)=>State.frontierCounters.Find(x=>x.id==key)?.count??0;
  // Callers save after their complete inventory/world mutation, so one action has one commit.
  public bool FrontierEvent(string eventId,string receipt){
   var catalog=FrontierCatalog.Current;if(!catalog||string.IsNullOrEmpty(receipt)||State.frontierReceipts.Contains(eventId+":"+receipt))return false;
   int goal=0;foreach(var c in catalog.commissions)if(c.eventId==eventId)goal=Mathf.Max(goal,c.goal);if(goal==0||FrontierCount(eventId)>=goal)return false;
   State.frontierReceipts.Add(eventId+":"+receipt);var counter=State.frontierCounters.Find(x=>x.id==eventId);if(counter==null){counter=new FrontierCounter{id=eventId};State.frontierCounters.Add(counter);}counter.count=Mathf.Min(goal,counter.count+1);return true;
  }
  public bool BuildProject(string id){var project=FrontierCatalog.Current?.ProjectFor(id);if(project==null||!AtCamp||!Exp(project.requirement)||Exp("build-"+id)||!Transaction(State,project.cost))return false;AddExp("build-"+id);ReconcileFrontierState();Changed();InteractionFeel.Play(FeelCue.Success,.8f);Notice(project.title+"已建成");return true;}
  public bool CraftFrontier(string id){var recipe=FrontierCatalog.Current?Array.Find(FrontierCatalog.Current.recipes,x=>x.id==id):null;if(recipe==null||!AtCamp||!Exp(recipe.requirement)||!Transaction(State,recipe.cost,recipe.result))return false;Changed();InteractionFeel.Play(FeelCue.Pickup,.6f);Notice(recipe.title);return true;}
  public bool ClaimCommission(string id){var c=FrontierCatalog.Current?.CommissionFor(id);if(c==null||!AtCamp||FrontierCount(c.eventId)<c.goal||Exp("commission-"+id))return false;AddExp("commission-"+id);DeliverFrontier(c.reward);Changed();Notice("委托已结算 · 物资送达营地");return true;}
  public void DeliverFrontier(ItemStack[] reward){foreach(var item in reward){int remaining=item.count;int bag=Math.Min(remaining,FreeSpace(State.bag,item.item));State.bag.Add(item.item,bag);remaining-=bag;int warehouse=Math.Min(remaining,FreeSpace(State.warehouse,item.item));State.warehouse.Add(item.item,warehouse);remaining-=warehouse;if(remaining>0){var crate=new SavedCrate{id=Guid.NewGuid().ToString("N"),region=0};crate.items.Add(item.item,remaining);State.crates.Add(crate);}}}
  public bool CompleteSurvey(int region){if(region!=State.region||!RegionView||!RegionView.Near("survey" )||Exp("survey-"+region))return false;AddExp("survey-"+region);FrontierEvent("survey",region.ToString());Changed();Notice("勘测记录已收入手账");return true;}
  public RuinRecord RuinFor(int region){var record=State.ruins.Find(x=>x.region==region);if(record==null){record=new RuinRecord{region=region};State.ruins.Add(record);}return record;}
  public bool CompleteRuin(int region){var definition=FrontierCatalog.Current?.RuinFor(region);var record=State.ruins.Find(x=>x.region==region);if(region!=State.region||!RegionView||!RegionView.RuinReady||definition==null||record==null||record.solved||Exp("ruin-"+region))return false;record.solved=true;AddExp("ruin-"+region);FrontierEvent("ruin",region.ToString());DeliverFrontier(definition.reward);Changed();Notice("遗迹开启 · 回收物资已入库");return true;}
  public void RecordCreature(CreatureVitality target,bool giant){if(!target)return;FrontierEvent("kill",State.region+":"+target.persistentId);var variant=target.GetComponent<FrontierCreatureTrait>();var elite=target.GetComponent<EliteCreature>();string species=variant?(variant.frost?"frost":"magnetic"):elite?(elite.Id=="ground"?"armored":"thunder"):giant?"titan":target.GetComponent<AlienCreature>()?.kind==CreatureKind.SpineFloater?"flyer":"mauler";if(!State.fieldRecords.Contains(species))State.fieldRecords.Add(species);if(variant)FrontierEvent("variant",target.persistentId);}
  void ReconcileFrontierState(){if(State.frontierCounters==null)State.frontierCounters=new List<FrontierCounter>();if(State.frontierReceipts==null)State.frontierReceipts=new List<string>();if(State.fieldRecords==null)State.fieldRecords=new List<string>();if(State.ruins==null)State.ruins=new List<RuinRecord>();State.bag.capacity=Exp("build-carrier")?24:Mathf.Max(16,State.bag.capacity);SeedFrontierHistory(State);}
  // Existing finite mineral/encounter records are credited once, so completed old worlds do not strand commissions.
  public static void SeedFrontierHistory(CampaignState state){if(state.expansionProgress.Contains("frontier-initialized"))return;int mining=state.minedNodes.Count,kills=0,forges=0,identified=0;foreach(var area in state.regions){mining+=area.mined.Count;foreach(var enemy in area.enemies)if(enemy.health==0)kills++;}for(int i=5;i<=9;i++){var item=(ItemId)i;forges+=state.bag.Count(item)+state.warehouse.Count(item);foreach(var box in state.crates)forges+=box.items.Count(item);}for(int i=0;i<4;i++)if(state.repairs.Contains(i.ToString()))foreach(var cost in CampaignRules.RepairCost(i))if((int)cost.item>=5&&(int)cost.item<=9)forges+=cost.count;for(int i=0;i<5;i++)if(state.Has("identified-"+i))identified++;
   string[] keys={"mining","kill","forging"};int[] counts={Mathf.Min(6,mining),Mathf.Min(4,kills),Mathf.Min(5,Mathf.Max(forges,identified))};for(int i=0;i<keys.Length;i++){var existing=state.frontierCounters.Find(x=>x.id==keys[i]);if(existing==null&&counts[i]>0)state.frontierCounters.Add(new FrontierCounter{id=keys[i],count=counts[i]});else if(existing!=null)existing.count=Mathf.Max(existing.count,counts[i]);}state.expansionProgress.Add("frontier-initialized");
  }
  public static void ValidateFrontierState(CampaignState s){
   if(s.frontierCounters==null)s.frontierCounters=new List<FrontierCounter>();if(s.frontierReceipts==null)s.frontierReceipts=new List<string>();if(s.fieldRecords==null)s.fieldRecords=new List<string>();if(s.ruins==null)s.ruins=new List<RuinRecord>();
   var catalog=FrontierCatalog.Current;var ids=new HashSet<string>();foreach(var count in s.frontierCounters){var goal=catalog?Array.Find(catalog.commissions,c=>c.eventId==count?.id)?.goal??0:6;if(count==null||string.IsNullOrEmpty(count.id)||!ids.Add(count.id)||count.count<0||count.count>goal)throw new InvalidDataException("委托记录损坏");}
   ids.Clear();foreach(var key in s.frontierReceipts)if(string.IsNullOrEmpty(key)||!ids.Add(key))throw new InvalidDataException("重复进度记录");
   ids.Clear();foreach(var key in s.fieldRecords)if(string.IsNullOrEmpty(key)||!ids.Add(key)||(catalog&&Array.Find(catalog.species,x=>x.id==key)==null))throw new InvalidDataException("物种记录损坏");
   var areas=new HashSet<int>();foreach(var ruin in s.ruins){if(ruin==null||ruin.region<2||ruin.region>4||!areas.Add(ruin.region)||ruin.values==null||ruin.values.Length!=3)throw new InvalidDataException("遗迹记录损坏");foreach(var value in ruin.values)if(value<0||value>3)throw new InvalidDataException("遗迹机关损坏");}
   foreach(var timer in new[]{s.blastCooldown,s.decoyCooldown})if(float.IsNaN(timer)||float.IsInfinity(timer)||timer<0||timer>12.1f)throw new InvalidDataException("战术装置损坏");
  }
 }
}
