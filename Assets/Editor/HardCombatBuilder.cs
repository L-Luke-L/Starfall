using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Starfall;

public static class HardCombatBuilder {
 const string Folder="Assets/Art/Combat/";
 static Material Mat(string name,Color color,float emission=0){var path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Metallic",.48f);mat.SetFloat("_Glossiness",.25f);if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(mat);return mat;}
 static Transform Group(string n,Transform p,Vector3 pos){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=pos;return t;}
 static Transform Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,PrimitiveType shape=PrimitiveType.Cube){var g=GameObject.CreatePrimitive(shape);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g.transform;}
 public static void Install(FirstPersonExplorer player){
  Directory.CreateDirectory(Folder);var steel=Mat("Gun graphite",new Color(.065f,.105f,.13f));var edge=Mat("Gun worn titanium",new Color(.25f,.32f,.36f));var orange=Mat("Fuel amber",new Color(.85f,.37f,.08f),.5f);var cyan=Mat("Cell cyan",new Color(.1f,.55f,.77f),1.4f);var grip=Mat("Glove dark leather",new Color(.11f,.10f,.09f));var med=Mat("Medical green",new Color(.16f,.61f,.30f),.8f);
  var expedition=player.arena.gameObject.AddComponent<BasinExpedition>();expedition.player=player;expedition.fuelMaterial=orange;expedition.oreMaterial=cyan;expedition.ammoMaterial=edge;expedition.medicalMaterial=med;
  var arsenal=player.gameObject.AddComponent<PlayerArsenal>();arsenal.player=player;arsenal.expedition=expedition;arsenal.tracerMaterial=cyan;expedition.arsenal=arsenal;player.arsenal=arsenal;player.expedition=expedition;
  var view=Group("First person weapon rig",player.viewCamera.transform,Vector3.zero);
  var overlay=Group("Weapon only camera",player.viewCamera.transform,Vector3.zero).gameObject.AddComponent<Camera>();overlay.clearFlags=CameraClearFlags.Depth;overlay.cullingMask=1<<30;overlay.depth=player.viewCamera.depth+1;overlay.fieldOfView=player.viewCamera.fieldOfView;overlay.nearClipPlane=.01f;overlay.farClipPlane=3;player.viewCamera.cullingMask&=~(1<<30);
  var rifle=Group("P12 field pulse rifle",view,new Vector3(.26f,-.27f,.60f));rifle.localScale=Vector3.one*.8f;arsenal.rifle=rifle;
  Box("Main receiver",rifle,Vector3.zero,new Vector3(.12f,.14f,.43f),steel);Box("Upper rail",rifle,new Vector3(0,.083f,.035f),new Vector3(.095f,.035f,.38f),edge);
  Box("Barrel shroud",rifle,new Vector3(0,.012f,.30f),new Vector3(.07f,.075f,.28f),edge);Box("Muzzle brake",rifle,new Vector3(0,.012f,.445f),new Vector3(.088f,.08f,.06f),steel);
  Box("Top sight rear",rifle,new Vector3(0,.12f,-.06f),new Vector3(.09f,.055f,.045f),steel);Box("Top sight front",rifle,new Vector3(0,.10f,.36f),new Vector3(.018f,.04f,.02f),cyan);
  Box("Power cell",rifle,new Vector3(-.066f,.005f,-.02f),new Vector3(.01f,.062f,.18f),cyan);Box("Magazine",rifle,new Vector3(0,-.11f,-.025f),new Vector3(.09f,.15f,.11f),edge);
  for(int i=0;i<5;i++)Box("Handguard vent "+i,rifle,new Vector3(-.043f,-.022f,.18f+i*.04f),new Vector3(.013f,.08f,.015f),steel);
  Box("Receiver latch",rifle,new Vector3(-.07f,.025f,-.12f),new Vector3(.024f,.033f,.045f),orange);
  Box("Handle",rifle,new Vector3(0,-.11f,-.16f),new Vector3(.075f,.17f,.065f),grip);Box("Right gloved hand",rifle,new Vector3(.018f,-.09f,-.145f),new Vector3(.105f,.12f,.10f),grip,PrimitiveType.Capsule);
  Box("Sleeve",rifle,new Vector3(.09f,-.16f,-.25f),new Vector3(.13f,.14f,.22f),steel,PrimitiveType.Capsule);
  arsenal.muzzle=Group("Shot muzzle",rifle,new Vector3(0,.012f,.49f));arsenal.muzzleLight=arsenal.muzzle.gameObject.AddComponent<Light>();arsenal.muzzleLight.range=3;arsenal.muzzleLight.color=Color.cyan;arsenal.muzzleLight.intensity=0;
  var tool=Group("Impact mining pick",view,new Vector3(-.23f,-.25f,.72f));tool.localScale=Vector3.one*.62f;arsenal.tool=tool;Box("Pick handle",tool,new Vector3(0,-.02f,0),new Vector3(.055f,.46f,.055f),grip);Box("Pick steel head",tool,new Vector3(0,.20f,0),new Vector3(.36f,.075f,.09f),edge);Box("Pick hot edge",tool,new Vector3(-.17f,.20f,0),new Vector3(.06f,.045f,.07f),cyan);Box("Left glove",tool,new Vector3(0,-.13f,0),new Vector3(.10f,.13f,.09f),grip,PrimitiveType.Capsule);
  Box("Left wrist cuff",tool,new Vector3(0,-.24f,0),new Vector3(.12f,.07f,.12f),edge);
  var forearm=Box("Left forearm sleeve",tool,new Vector3(0,-.43f,-.10f),new Vector3(.13f,.27f,.16f),steel,PrimitiveType.Capsule);forearm.localRotation=Quaternion.Euler(24,0,0);
  foreach(var t in view.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
  foreach(var c in player.arena.creatures){var health=c.gameObject.AddComponent<CreatureVitality>();health.maxHealth=c.kind==CreatureKind.RiftMauler?190:145;c.detectionRange=34;c.leashRadius=64;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
  var titan=Object.FindFirstObjectByType<SkyTitan>();titan.gameObject.AddComponent<CreatureVitality>().maxHealth=1800;
  player.arena.maxSimultaneousPursuers=3;
  var nodes=new List<BasinResource>();var root=Group("Harvestable mineral seams",player.arena.transform,Vector3.zero);
  Vector2[] anchors={new Vector2(-13,-17),new Vector2(22,-12),new Vector2(-36,6),new Vector2(-54,25),new Vector2(13,39),new Vector2(41,33),new Vector2(58,5),new Vector2(-28,-43),new Vector2(6,67),new Vector2(-62,-19),new Vector2(37,65),new Vector2(-39,58)};
  var nav=NavMesh.AddNavMeshData(player.arena.navigation);
  try{for(int i=0;i<anchors.Length;i++){
   var pos=new Vector3(anchors[i].x,AlienSurfaceLandmarks.Height(anchors[i].x,anchors[i].y),anchors[i].y);if(!NavMesh.SamplePosition(pos,out var hit,7,NavMesh.AllAreas))throw new System.Exception("No navigable ore site "+i);pos=hit.position;
   var group=Group("Mineral vein "+i,root,pos);var node=group.gameObject.AddComponent<BasinResource>();node.id=1400+i;node.metal=(RepairResource)(i%4);var material=Mat("Vein "+node.metal,OreInventory.MetalColor(node.metal)*.48f,.14f);
   var rock=Box("Anchored basalt base",group,new Vector3(0,-.07f,0),new Vector3(.8f,.42f,.62f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/AlienSurface/StratifiedBasalt.mat"));rock.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/AlienSurface/Rock0.asset");var crystals=Group("Collectible exposed mineral",group,Vector3.zero);node.crystals=crystals;
   for(int j=0;j<5;j++){var crystal=Box("Independent ore crystal "+j,crystals,new Vector3((j-2)*.22f,.18f,Mathf.Sin(j*2)*.22f),new Vector3(.16f,.62f+(j%3)*.23f,.18f),material);crystal.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/AlienSurface/Rock"+(j+1)+".asset");crystal.localRotation=Quaternion.Euler(j*7-13,j*37,j*9-18);}
   var collider=group.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.6f,0);collider.size=new Vector3(1.3f,1.2f,1);nodes.Add(node);
  }}finally{nav.Remove();}
  expedition.resources=nodes.ToArray();
 }
}
