using System;
using UnityEngine;

namespace Starfall {
 [CreateAssetMenu(menuName="Starfall/Lost Realms Catalog")]
 public sealed class LostRealmCatalog:ScriptableObject {
  [Serializable] public sealed class Weapon {public string id,title;public int magazine,ammoPerShot,bodyDamage,weakDamage;public float range,reload,interval,charge;}
  [Serializable] public sealed class Module {public string id,title,description,unlock;public int parts=5;}
  [Serializable] public sealed class Region {public int id;public string title,environment,uniqueEvent;public Color fog,accent;}
  [Serializable] public sealed class Quest {public string id,title,companion,reward;}
  [Serializable] public sealed class Relay {public int region,parts=4;public string title,story;public float seconds=22;public Vector3[] anchors;}
  [Serializable] public sealed class Relic {public int region;public string title,story;}
  public Weapon[] weapons;public Module[] modules;public Region[] regions;public Quest[] quests;public Relay[] relays;public Relic[] relics;
  static LostRealmCatalog current;
  public static LostRealmCatalog Current=>current?current:current=Resources.Load<LostRealmCatalog>("LostRealmCatalog");
  public Weapon WeaponAt(int index){return weapons!=null&&index>=0&&index<weapons.Length?weapons[index]:null;}
  public Module ModuleAt(int index){return modules!=null&&index>=0&&index<modules.Length?modules[index]:null;}
  public Relay RelayFor(int region)=>relays==null?null:Array.Find(relays,r=>r.region==region);
  public Relic RelicFor(int region)=>relics==null?null:Array.Find(relics,r=>r.region==region);
 }
}
