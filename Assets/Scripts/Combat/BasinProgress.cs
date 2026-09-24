using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public enum BasinLootKind {None,Fuel,Ore,Ammo,Medkit}
 [System.Serializable] public sealed class BasinDrop {
  public int id;public BasinLootKind kind;public Vector3 position;public bool collected;
 }
 // Owned by the main scene. Wormhole visits share one expedition; restarting starts a fresh expedition.
 public sealed class BasinProgress {
  public int magazine=12,reserve=60,health=100;
  public readonly Dictionary<string,int> enemyHealth=new Dictionary<string,int>();
  public readonly HashSet<int> mined=new HashSet<int>();
  public readonly List<BasinDrop> drops=new List<BasinDrop>();
  public static BasinLootKind RollLoot(float roll){return roll<.35f?BasinLootKind.Fuel:roll<.50f?BasinLootKind.Ore:roll<.62f?BasinLootKind.Ammo:roll<.72f?BasinLootKind.Medkit:BasinLootKind.None;}
 }
}
