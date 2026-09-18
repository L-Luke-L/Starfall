using UnityEngine;

namespace Starfall {
 public sealed class BasinResource:MonoBehaviour {
  public int id;public RepairResource metal;public Transform crystals;
  public bool Depleted {get;private set;}
  public string DisplayName=>OreInventory.OreName(metal);
  public void Exhaust(){Depleted=true;if(crystals)crystals.gameObject.SetActive(false);}
 }
 public sealed class BasinPickup:MonoBehaviour {
  public BasinDrop data;public BasinExpedition expedition;Vector3 origin;float t;
  public string DisplayName=>data.kind==BasinLootKind.Fuel?"燃料 ×2":data.kind==BasinLootKind.Ore?"未知矿石 ×1":data.kind==BasinLootKind.Ammo?"弹药 ×18":"急救剂 +25";
  void Start(){origin=transform.position;}
  void Update(){if(!expedition.player.arena.Active)return;t+=Time.deltaTime;transform.position=origin+Vector3.up*(Mathf.Sin(t*2)*.08f);transform.Rotate(0,Time.deltaTime*24,0);}
 }
}
