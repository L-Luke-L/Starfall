using System;
using UnityEngine;

namespace Starfall {
 [CreateAssetMenu(menuName="Starfall/Frontier Catalog")]
 public sealed class FrontierCatalog:ScriptableObject {
  [Serializable] public sealed class Project {public string id,title,requirement,effect;public ItemStack[] cost;}
  [Serializable] public sealed class Commission {public string id,title,eventId;public int goal;public ItemStack[] reward;}
  [Serializable] public sealed class Landmark {public int region;public string title,story;public Vector3 position;}
  [Serializable] public sealed class Ruin {public int region;public string title;public Vector3 position;public int[] solution;public ItemStack[] reward;}
  [Serializable] public sealed class Recipe {public string id,title,requirement;public ItemStack[] cost;public ItemStack result;}
  [Serializable] public sealed class Species {public string id,title,detail;}
  public Project[] projects;public Commission[] commissions;public Landmark[] landmarks;public Ruin[] ruins;public Recipe[] recipes;public Species[] species;
  static FrontierCatalog cached;public static FrontierCatalog Current=>cached?cached:cached=Resources.Load<FrontierCatalog>("FrontierCatalog");
  public Project ProjectFor(string id)=>Array.Find(projects,x=>x.id==id);
  public Commission CommissionFor(string id)=>Array.Find(commissions,x=>x.id==id);
  public Landmark LandmarkFor(int region)=>Array.Find(landmarks,x=>x.region==region);
  public Ruin RuinFor(int region)=>Array.Find(ruins,x=>x.region==region);
 }
}
