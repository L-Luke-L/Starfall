using System;
using UnityEngine;

namespace Starfall {
 [CreateAssetMenu(menuName="Starfall/Lost Realms Catalog")]
 public sealed class LostRealmCatalog:ScriptableObject {
  [Serializable] public sealed class Weapon {public string id,title;public int magazine,ammoPerShot,bodyDamage,weakDamage;public float range,reload,interval,charge;}
  [Serializable] public sealed class Module {public string id,title,description,unlock;public int parts=5;}
  [Serializable] public sealed class Region {public int id;public string title,environment,uniqueEvent;public Color fog,accent;}
  [Serializable] public sealed class Quest {public string id,title,companion,reward;}
  public Weapon[] weapons;public Module[] modules;public Region[] regions;public Quest[] quests;
  static LostRealmCatalog current;
  public static LostRealmCatalog Current=>current?current:current=Resources.Load<LostRealmCatalog>("LostRealmCatalog");
  public Weapon WeaponAt(int index){return weapons!=null&&index>=0&&index<weapons.Length?weapons[index]:null;}
  public Module ModuleAt(int index){return modules!=null&&index>=0&&index<modules.Length?modules[index]:null;}
 }
}
