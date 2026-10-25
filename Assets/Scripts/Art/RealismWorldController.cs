using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Starfall {
 [DefaultExecutionOrder(900)]
 public sealed class RealismWorldController:MonoBehaviour {
  readonly HashSet<Transform> processed=new HashSet<Transform>();float nextScan;Material terrain,metal;bool initialized;RealismStructures structures;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Existing(){Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
  static void Loaded(Scene scene,LoadSceneMode mode){if(!scene.name.StartsWith("Scene0")||scene.name.StartsWith("Scene01"))return;foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<RealismWorldController>())return;var host=new GameObject("Starfall realism · appearance layer");SceneManager.MoveGameObjectToScene(host,scene);host.AddComponent<RealismWorldController>();}
  IEnumerator Start(){yield return null;yield return null;var landmarks=FindFirstObjectByType<AlienSurfaceLandmarks>();if(!landmarks){enabled=false;yield break;}MakeMaterials();structures=new RealismStructures();UpgradeSky();Scan();initialized=true;}
  void Update(){if(!initialized||Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+1;Scan();}
  void MakeMaterials(){terrain=new Material(Resources.Load<Shader>("StarfallRealism/RealismTerrain"));terrain.name="Scanned moon soil · visual only";terrain.SetTexture("_MainTex",RealismAssets.Texture("Textures/GroundColor"));terrain.SetTexture("_NormalTex",RealismAssets.Texture("Textures/GroundNormal"));terrain.SetTexture("_RoughTex",RealismAssets.Texture("Textures/GroundRoughness"));terrain.SetTexture("_AOTex",RealismAssets.Texture("Textures/GroundAO"));terrain.SetFloat("_Scale",.32f);
   metal=new Material(Shader.Find("Standard")){name="Detailed industrial alloy",color=new Color(.65f,.69f,.72f),mainTexture=RealismAssets.Texture("Textures/MetalColor")};metal.SetFloat("_Metallic",.72f);metal.SetFloat("_Glossiness",.24f);metal.SetTexture("_BumpMap",RealismAssets.Texture("Textures/MetalNormal"));metal.EnableKeyword("_NORMALMAP");metal.mainTextureScale=Vector2.one*2;metal.SetTextureScale("_BumpMap",Vector2.one*2);
  }
  void Scan(){processed.RemoveWhere(t=>!t);foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    if(!t||RealismAssets.InAppearance(t)||processed.Contains(t))continue;processed.Add(t);
    if(t.GetComponent<RegionDeviceVisual>()&&t.name.StartsWith("Signal mast")){structures.Antenna(t);continue;}
    if(t.GetComponentInParent<RegionDeviceVisual>()&&t.parent&&t.parent.name.StartsWith("Signal mast"))continue;
    var actor=t.GetComponent<AlienCreature>();if(actor){Creature(actor);continue;}var titan=t.GetComponent<SkyTitan>();if(titan){if(!titan.player||!titan.player.Ready||!titan.player.expedition||!titan.player.expedition.Initialized){processed.Remove(t);continue;}Titan(titan);continue;}
    if(t.GetComponentInParent<FirstPersonExplorer>()||t.GetComponentInParent<AlienCreature>()||t.GetComponentInParent<SkyTitan>())continue;
    if(t.name=="Half buried explorer ship"){Replace(t,"Ship",RealismAssets.Bounds(t));continue;}
    var renderer=t.GetComponent<MeshRenderer>();if(!renderer)continue;string n=t.name;
    if(n=="Cold glass window"){structures.Window(t);continue;}if(n.StartsWith("Independent frost crystal")){structures.Crystal(t);continue;}
    if(n=="Walkable undulating ground"){renderer.sharedMaterial=terrain;continue;}
    if(n.StartsWith("Field boulder")||n.StartsWith("Cliff column")||n=="Arrival-side solid boulder"||n.StartsWith("Slanted spire")||n.StartsWith("Suspended basalt shard")){Replace(t,"Rock"+(1+Mathf.Abs((Mathf.RoundToInt(t.position.x*11)+Mathf.RoundToInt(t.position.z*17))%3)),renderer.bounds);continue;}
    if(n=="Sealed recovery case"||n=="Cargo base"){Replace(t,"Crate",renderer.bounds);continue;}
    if(n=="Console"||n=="Isolation terminal"){Replace(t,"Terminal",renderer.bounds);continue;}
    if(n.EndsWith(" body")&&t.GetComponentInParent<RegionDeviceVisual>()){
     // Retain the original signal lens, vial, completion colour, and interaction transform.
     bool sample=n.Contains("sample")||n.Contains("chart")||n.Contains("token")||n.Contains("nameplate")||n.Contains("insignia")||n.Contains("record");if(!sample)Replace(t,n.Contains("supply")||n.Contains("salvage")?"Crate":n.Contains("mast")?"Tower":"Control",renderer.bounds);continue;
    }
    if(n.StartsWith("Independent hull rib")||n=="Hull crown"||n=="Buried keel"||n=="Engine pod"||n=="Dock support"||n=="Military gantry"||n=="Medical habitat"||n=="Stored D4 cells"||n.StartsWith("Medical isolation")||n.StartsWith("Habitat ")){structures.Housing(t);continue;}
    if(n.StartsWith("Evacuation barrier")||n.StartsWith("Gantry foot")||n.StartsWith("Suspended gantry")||n.StartsWith("Vessel ")||n.StartsWith("Broken roof")||n.StartsWith("Broken foredeck")||n.StartsWith("Broken vessel prow")||n.StartsWith("Trench wall")||n.StartsWith("Greenhouse bed")||n.StartsWith("Independent cargo container")||n.StartsWith("D4 ring pylon")||n=="Collapsed hull"||n=="Dark cabin"||n.StartsWith("Distant broken signal tower")||n.StartsWith("Core blast shelter")||n.StartsWith("Dock deck")||n.StartsWith("Shipyard platform")||n=="Broken road"||n.StartsWith("Outer approach cover")||n.StartsWith("Inner approach cover"))structures.Housing(t);
   }}
  static void Lods(Transform root){if(root.GetComponentInChildren<LODGroup>())return;var all=root.GetComponentsInChildren<Renderer>(true);var levels=new List<LOD>();for(int i=0;i<4;i++){var list=new List<Renderer>();foreach(var r in all)if(r.name.Contains("_LOD"+i))list.Add(r);if(list.Count>0)levels.Add(new LOD(i==0?.25f:i==1?.12f:i==2?.055f:.018f,list.ToArray()));}if(levels.Count>0){var group=root.gameObject.AddComponent<LODGroup>();group.SetLODs(levels.ToArray());group.RecalculateBounds();}}
  void Replace(Transform original,string id,Bounds target){var model=RealismAssets.Instantiate(id,original);if(!model)return;var wrapper=model.transform;wrapper.SetParent(null,false);wrapper.localScale=Vector3.one;wrapper.position=target.center-Vector3.up*target.extents.y;wrapper.rotation=original.rotation;RealismAssets.Fit(wrapper,Mathf.Max(target.size.x,target.size.y,target.size.z));
   // Fit with a uniform scale. Inherited nonuniform primitive scale must never distort an imported model.
   var fitted=RealismAssets.Bounds(wrapper);float fit=Mathf.Min(target.size.x/Mathf.Max(.001f,fitted.size.x),Mathf.Min(target.size.y/Mathf.Max(.001f,fitted.size.y),target.size.z/Mathf.Max(.001f,fitted.size.z)));
   {wrapper.localScale*=fit;fitted=RealismAssets.Bounds(wrapper);wrapper.position+=target.center-fitted.center;wrapper.position+=Vector3.up*(target.min.y-RealismAssets.Bounds(wrapper).min.y);}
   // Keep the same anchor and bounds envelope; the original collider is never replaced.
   wrapper.SetParent(original,true);RealismAssets.HideOriginal(original);Lods(wrapper);if(original.name.StartsWith("Suspended basalt shard")){var tint=new MaterialPropertyBlock();tint.SetColor("_Color",new Color(.20f,.18f,.22f));foreach(var r in wrapper.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(tint);}
  }
  void Creature(AlienCreature actor){if(!actor.visual||actor.visual.GetComponentInChildren<RealismCreatureVisual>(true))return;var model=RealismAssets.Instantiate(actor.kind==CreatureKind.RiftMauler?"Dinosaur":"Wyvern",actor.visual);if(!model)return;
   RealismAssets.Orient(model.transform,actor.kind==CreatureKind.RiftMauler?"bn_Head":"head");RealismAssets.Fit(model.transform,actor.kind==CreatureKind.RiftMauler?3.1f:6.8f,actor.kind==CreatureKind.RiftMauler?1:3);if(actor.kind==CreatureKind.RiftMauler){var hips=RealismAssets.Bone(model.transform,"bn_Spine");if(hips){var offset=actor.visual.position-hips.position;offset.y=0;model.transform.position+=offset;}}if(actor.kind==CreatureKind.SpineFloater)model.transform.localPosition+=Vector3.down*.65f;
   RealismAssets.HideOriginal(actor.visual);model.AddComponent<RealismCreatureVisual>().Configure(actor);
  }
  void Titan(SkyTitan titan){if(titan.GetComponentInChildren<RealismTitanVisual>(true))return;var life=titan.GetComponent<CreatureVitality>();if(life&&life.Dead)return;var model=RealismAssets.Instantiate("AncientDragon",titan.transform);if(!model)return;RealismAssets.Orient(model.transform,"head");model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;RealismAssets.Fit(model.transform,220,0);RealismAssets.HideOriginal(titan.transform);model.AddComponent<RealismTitanVisual>().Configure(titan);}
  void UpgradeSky(){Transform planet=null,ring=null;foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){if(t.name=="Distant gas giant")planet=t;if(t.name=="Planet orbital ring")ring=t;}if(!planet||!ring)return;
   planet.gameObject.isStatic=false;ring.gameObject.isStatic=false;var planetMat=new Material(Resources.Load<Shader>("StarfallRealism/RealismPlanet"));planetMat.SetTexture("_MainTex",RealismAssets.Texture("Textures/GasGiantClouds"));planet.GetComponent<Renderer>().sharedMaterial=planetMat;
   float radius=planet.GetComponent<Renderer>().bounds.extents.x;var mesh=RingMesh();ring.GetComponent<MeshFilter>().sharedMesh=mesh;ring.position=planet.position;ring.localScale=Vector3.one*(radius*2.3f);var ringMat=new Material(Resources.Load<Shader>("StarfallRealism/RealismRing"));ring.GetComponent<Renderer>().sharedMaterial=ringMat;
   var binding=ring.gameObject.AddComponent<RealismSkyBinding>();binding.planet=planet;binding.radius=radius;
   var sky=new Material(Resources.Load<Shader>("StarfallRealism/RealismStarSky"));sky.SetTexture("_StarMap",RealismAssets.Texture("Textures/StarMap"));RenderSettings.skybox=sky;
  }
  static Mesh RingMesh(){var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int steps=512;for(int i=0;i<=steps;i++){float a=i*Mathf.PI*2/steps;vertices.Add(new Vector3(Mathf.Cos(a)*.5565f,0,Mathf.Sin(a)*.5565f));vertices.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));uv.Add(new Vector2(i/(float)steps,0));uv.Add(new Vector2(i/(float)steps,1));if(i>0){int k=i*2;triangles.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}}var m=new Mesh{name="Saturn style density rings · no collision"};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
  void OnDestroy(){if(terrain)Destroy(terrain);if(metal)Destroy(metal);if(structures!=null)structures.Dispose();}
 }
 public sealed class RealismSkyBinding:MonoBehaviour {
  public Transform planet;public float radius;
  void LateUpdate(){if(!planet)return;var sun=RenderSettings.sun;Vector3 dir=sun?-sun.transform.forward:new Vector3(-1,.65f,.8f).normalized;Shader.SetGlobalVector("_SFSunDirection",new Vector4(dir.x,dir.y,dir.z,0));Shader.SetGlobalVector("_SFPlanet",new Vector4(planet.position.x,planet.position.y,planet.position.z,radius));var normal=transform.up;Shader.SetGlobalVector("_SFRingPlane",new Vector4(normal.x,normal.y,normal.z,0));Shader.SetGlobalVector("_SFRingRadii",new Vector4(radius*1.28f,radius*2.3f,0,0));}
 }
}
