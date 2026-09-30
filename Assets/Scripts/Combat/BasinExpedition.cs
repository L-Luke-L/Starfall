using UnityEngine;

namespace Starfall {
 public sealed class BasinExpedition:MonoBehaviour {
  public FirstPersonExplorer player;public PlayerArsenal arsenal;public BasinResource[] resources;
  public Material fuelMaterial,oreMaterial,ammoMaterial,medicalMaterial;
  public BasinProgress Progress {get;private set;}
  public bool Initialized=>initialized;
  public float MiningProgress {get;private set;}
  public string Prompt {get;private set;}
  public string Notice {get;private set;}
  public float NoticeAlpha=>Mathf.Clamp01(noticeTime*1.8f);
  public Transform Focused {get;private set;}
  Renderer[] focusRenderers;MaterialPropertyBlock focusBlock;float chipTimer;
  public int Kills {get;private set;}
  public const float MiningSeconds=3.4f;
  readonly System.Collections.Generic.List<BasinPickup> pickups=new System.Collections.Generic.List<BasinPickup>();
  readonly RaycastHit[] visibilityHits=new RaycastHit[48];
  BasinResource mining;int damageStamp;float noticeTime,magnetTick;bool initialized;OreInventory localInventory;
  public OreInventory Inventory=>player.session?player.session.director.inventory:localInventory;
  public void Initialize(){
   if(initialized)return;initialized=true;Progress=player.session!=null?player.session.Progress:new BasinProgress();
   if(!player.session)localInventory=gameObject.AddComponent<OreInventory>();arsenal.Initialize(Progress);player.arena.RestoreHealth(Progress.health);
   foreach(var c in player.arena.creatures)if(c&&c.gameObject.activeInHierarchy)c.GetComponent<CreatureVitality>().Initialize(this);
   var titan=FindFirstObjectByType<SkyTitan>();if(titan)titan.GetComponent<CreatureVitality>().Initialize(this);
   foreach(var r in resources)if(Progress.mined.Contains(r.id))r.Exhaust();foreach(var drop in Progress.drops)if(!drop.collected)SpawnPickup(drop);
  }
  void Update(){
   if(!initialized){if(player.Ready&&(!player.session||!player.session.IsTransitioning))Initialize();return;}
   if(noticeTime>0&&player.arena.Active){noticeTime-=Time.deltaTime;if(noticeTime<=0)Notice=null;}
   if(!player.Ready||player.Paused||player.arena.Downed){MiningProgress=0;return;}
   if(CampaignRuntime.Current&&CampaignRuntime.Current.ModuleEquipped("magnet")){magnetTick-=Time.deltaTime;if(magnetTick<=0){magnetTick=.25f;for(int i=pickups.Count-1;i>=0;i--){var pickup=pickups[i];if(!pickup||pickup.data.collected){pickups.RemoveAt(i);continue;}if((player.transform.position-pickup.transform.position).sqrMagnitude<16)Collect(pickup);}}}
   if(!player.Automated)Interact(Input.GetKey(KeyCode.E),Time.deltaTime);
  }
  public void Save(){if(!initialized||!player||!player.arena||!arsenal)return;Progress.health=player.arena.Health;Progress.magazine=arsenal.Magazine;Progress.reserve=arsenal.Reserve;}
  public void MiningProgressReset(){MiningProgress=0;mining=null;}
  public void Tell(string text){Notice=text;noticeTime=2.4f;}
  public void OnDefeated(CreatureVitality target,bool giant){
   InteractionFeel.Play(FeelCue.Kill,.8f);Kills++;Tell("击败");var elite=target.GetComponent<EliteCreature>();if(elite&&CampaignRuntime.Current)CampaignRuntime.Current.EliteDefeated(elite.Id);var kind=BasinProgress.RollLoot(Random.value);if(CampaignRuntime.Current&&CampaignRuntime.Current.State.region==1&&kind==BasinLootKind.Ore)kind=BasinLootKind.Fuel;if(kind!=BasinLootKind.None){var p=target.transform.position;if(giant)p=player.transform.position+player.transform.forward*3;AddDrop(kind,p);}
  }
  public BasinPickup AddDrop(BasinLootKind kind,Vector3 point){
   point=FieldSurface.ReachableDrop(point,player.returnBeacon?player.returnBeacon.position:player.transform.position);var d=new BasinDrop{id=Progress.drops.Count,kind=kind,position=point};Progress.drops.Add(d);if(CampaignRuntime.Current)CampaignRuntime.Current.Changed();return SpawnPickup(d);
  }
  BasinPickup SpawnPickup(BasinDrop data){
   bool canister=data.kind==BasinLootKind.Fuel||data.kind==BasinLootKind.Medkit;var g=GameObject.CreatePrimitive(canister?PrimitiveType.Cylinder:PrimitiveType.Cube);g.name="Dropped supply · "+data.kind;g.transform.SetParent(transform);g.transform.position=data.position;g.transform.localScale=canister?new Vector3(.34f,.24f,.34f):new Vector3(.42f,.32f,.30f);var material=data.kind==BasinLootKind.Fuel?fuelMaterial:data.kind==BasinLootKind.Ore?oreMaterial:data.kind==BasinLootKind.Ammo?ammoMaterial:medicalMaterial;g.GetComponent<Renderer>().sharedMaterial=material;
   var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(cap.GetComponent<Collider>());cap.name="Supply retaining band";cap.transform.SetParent(g.transform,false);cap.transform.localPosition=Vector3.up*(canister?.85f:.20f);cap.transform.localScale=new Vector3(1.06f,.17f,1.06f);cap.GetComponent<Renderer>().sharedMaterial=ammoMaterial;
   var p=g.AddComponent<BasinPickup>();p.data=data;p.expedition=this;pickups.Add(p);return p;
  }
  public bool Collect(BasinPickup p){
   if(!p||p.expedition!=this||p.data.collected||!player.arena.CanTarget||Vector3.Distance(player.viewCamera.transform.position,p.transform.position)>(CampaignRuntime.Current&&CampaignRuntime.Current.ModuleEquipped("magnet")?4.3f:3.3f)||!Visible(p.transform.position,p.transform))return false;
   if(CampaignRuntime.Current){var campaign=CampaignRuntime.Current;if(!campaign.State.bag.Add(CampaignRuntime.DropItem(p.data.kind),CampaignRuntime.DropCount(p.data.kind))){Tell("背包已满");return false;}p.data.collected=true;campaign.Changed();InteractionFeel.Play(FeelCue.Pickup,.75f);Tell(p.DisplayName);Destroy(p.gameObject);return true;}
   if(p.data.kind==BasinLootKind.Fuel){if(!Inventory)return false;Inventory.AddFuel(2);}
   else if(p.data.kind==BasinLootKind.Ore){if(!Inventory)return false;Inventory.AddGathered(RepairResource.FerricAlloy);}
   else if(p.data.kind==BasinLootKind.Ammo){if(arsenal.Reserve>=180){Tell("弹药已满");InteractionFeel.Play(FeelCue.Deny,.35f);return false;}arsenal.AddAmmo(18);}
   else if(!player.arena.Heal(25)){Tell("护甲已满");InteractionFeel.Play(FeelCue.Deny,.35f);return false;}
   InteractionFeel.Play(FeelCue.Pickup,.75f);p.data.collected=true;Tell(p.DisplayName);Destroy(p.gameObject);return true;
  }
  public bool Visible(Vector3 point,Transform target){
   var origin=player.viewCamera.transform.position;var delta=point-origin;int count=Physics.RaycastNonAlloc(origin,delta.normalized,visibilityHits,delta.magnitude+.1f,~(1<<30),QueryTriggerInteraction.Ignore);
   if(count==visibilityHits.Length)return false;float nearest=float.MaxValue;Transform obstruction=null;
   for(int i=0;i<count;i++){var h=visibilityHits[i];if(h.collider.GetComponentInParent<FirstPersonExplorer>()||h.distance>=nearest)continue;nearest=h.distance;obstruction=h.transform;}
   return !obstruction||obstruction==target||obstruction.IsChildOf(target);
  }
  void Focus(Transform next){
   if(Focused==next)return;
   if(focusRenderers!=null)foreach(var r in focusRenderers)if(r)r.SetPropertyBlock(null);
   Focused=next;focusRenderers=next?next.GetComponentsInChildren<Renderer>():null;if(!next)return;InteractionFeel.Play(FeelCue.Hover,.3f);if(focusBlock==null)focusBlock=new MaterialPropertyBlock();
   foreach(var r in focusRenderers)if(r&&r.sharedMaterial&&r.sharedMaterial.HasProperty("_EmissionColor")){var original=r.sharedMaterial.GetColor("_EmissionColor");focusBlock.Clear();focusBlock.SetColor("_EmissionColor",original+new Color(.10f,.17f,.16f));r.SetPropertyBlock(focusBlock);}
  }
  public void Interact(bool held,float dt){
   Prompt=null;if(!initialized||!player.arena.CanTarget){Focus(null);MiningProgress=0;mining=null;return;}
   if(!Physics.Raycast(player.viewCamera.transform.position,player.viewCamera.transform.forward,out var hit,3.3f,~(1<<30),QueryTriggerInteraction.Ignore)){Focus(null);MiningProgress=0;mining=null;return;}
   var pickup=hit.collider.GetComponentInParent<BasinPickup>();if(pickup){Focus(pickup.transform);Prompt="E  "+pickup.DisplayName;if(held)Collect(pickup);MiningProgress=0;return;}
   var resource=hit.collider.GetComponentInParent<BasinResource>();if(!resource||resource.Depleted){Focus(null);MiningProgress=0;mining=null;return;}
   Focus(resource.transform);Prompt="按住 E  "+resource.DisplayName;Mine(resource,held,dt);
  }
  public bool Mine(BasinResource resource,bool held,float dt){
   if(!initialized||!player.arena.CanTarget||!held||arsenal.IsBusy||dt<=0||!resource||resource.Depleted||!Inventory||Vector3.Distance(player.viewCamera.transform.position,resource.transform.position+Vector3.up*.5f)>3.3f||!Visible(resource.transform.position+Vector3.up*.5f,resource.transform)){MiningProgress=0;mining=null;return false;}
   if(mining!=resource){mining=resource;MiningProgress=0;damageStamp=player.arena.DamageCount;}
   if(damageStamp!=player.arena.DamageCount){damageStamp=player.arena.DamageCount;MiningProgress=0;InteractionFeel.Play(FeelCue.Deny,.5f);Tell("中断");return false;}
   chipTimer-=dt;if(chipTimer<=0){chipTimer=.38f;InteractionFeel.Play(FeelCue.Chip,.55f);player.arena.Impact(resource.transform.position+Vector3.up*.65f,Color.cyan);}
   MiningProgress+=dt/MiningSeconds;if(MiningProgress<1)return false;
   if(!Inventory.CanGather(resource.metal)){Tell("背包已满");MiningProgress=0;return false;}
   if(!Progress.mined.Add(resource.id)){MiningProgress=0;return false;}InteractionFeel.Play(FeelCue.Pickup,.8f);Focus(null);resource.Exhaust();Inventory.AddGathered(resource.metal);MiningProgress=0;Tell(resource.DisplayName+" +1");return true;
  }
  void OnDestroy(){Focus(null);Save();}
 }
}
