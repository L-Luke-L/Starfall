using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Starfall;

public static class CampaignBuilder {
 [MenuItem("Starfall/Install Story Campaign and Build")]
 public static void InstallAndBuild(){
  var surface=EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Starfall.unity");
  var director=UnityEngine.Object.FindFirstObjectByType<SceneDirector>();Configure(director);
  EditorSceneManager.SaveScene(surface);
  var battle=EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Encounter.unity");
  var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonExplorer>();
  int id=0;foreach(var actor in player.arena.creatures){actor.GetComponent<CreatureVitality>().persistentId="c69221a3-3291-473b-ab32-"+(++id).ToString("D12");}
  var titan=UnityEngine.Object.FindFirstObjectByType<SkyTitan>();if(titan)titan.GetComponent<CreatureVitality>().persistentId="c69221a3-3291-473b-ab32-000000000099";
  Pistol(player);EditorSceneManager.SaveScene(battle);
  var keep=player.arena.creatures.Where(x=>x.kind==CreatureKind.RiftMauler).Take(2).ToArray();
  foreach(var actor in player.arena.creatures)if(!keep.Contains(actor))UnityEngine.Object.DestroyImmediate(actor.gameObject);
  player.arena.creatures=keep;player.arena.maxSimultaneousPursuers=2;
  if(titan)UnityEngine.Object.DestroyImmediate(titan.gameObject);
  for(int i=0;i<keep.Length;i++){var actor=keep[i];float x=i==0?-12:13,z=i==0?2:8;actor.transform.position=new Vector3(x,AlienSurfaceLandmarks.Height(x,z)+.2f,z);actor.detectionRange=16;actor.leashRadius=23;actor.patrolRadius=9;}
  foreach(var ore in player.expedition.resources)UnityEngine.Object.DestroyImmediate(ore.gameObject);player.expedition.resources=new BasinResource[0];
  EditorSceneManager.SaveScene(battle,"Assets/Scenes/Scene03_WreckRift.unity");
  LostRealmBuilder.Generate();
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Scene01_Starfall.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene02_Encounter.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene03_WreckRift.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene04_Battlefield.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene05_Medical.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene06_Shipyard.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Scene07_Core.unity",true)};
  AssetDatabase.SaveAssets();StarfallBuilder.Build();Debug.Log("CAMPAIGN_BUILD_PASS");
 }
 public static void Configure(SceneDirector director){var c=director.GetComponent<CampaignRuntime>();if(!c)c=director.gameObject.AddComponent<CampaignRuntime>();c.director=director;c.mineralSprites=new[]{"AmberNodule","FerricNodule","AmberNodule","BlueNodule","VeiledGeode","VeiledGeode"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Underground/"+n+".png")).ToArray();if(!director.GetComponent<CampaignVerification>())director.gameObject.AddComponent<CampaignVerification>();if(!director.GetComponent<LostRealmVerification>())director.gameObject.AddComponent<LostRealmVerification>();}
 static Transform Box(string name,Transform parent,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<MeshFilter>().sharedMesh=ArmorAsset();g.layer=30;return g.transform;}
 static Mesh ArmorAsset(){const string path="Assets/Art/Combat/CampaignArmor.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=CampaignGeometry.Armor();AssetDatabase.CreateAsset(mesh,path);}return mesh;}
 static void Pistol(FirstPersonExplorer player){
  var arsenal=player.arsenal;var parent=arsenal.rifle.parent;UnityEngine.Object.DestroyImmediate(arsenal.rifle.gameObject);
  var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/Gun graphite.mat");var edge=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/Gun worn titanium.mat");var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/Cell cyan.mat");var grip=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/Glove dark leather.mat");
  var gun=new GameObject("P12 survival pistol").transform;gun.SetParent(parent,false);gun.localPosition=new Vector3(.22f,-.22f,.47f);gun.gameObject.layer=30;arsenal.rifle=gun;
  Box("Short slide",gun,new Vector3(0,.02f,.06f),new Vector3(.085f,.085f,.27f),edge);Box("Slide rail",gun,new Vector3(0,-.034f,.065f),new Vector3(.076f,.045f,.25f),steel);
  var handle=Box("Pistol grip",gun,new Vector3(0,-.12f,-.032f),new Vector3(.072f,.17f,.085f),grip);handle.localRotation=Quaternion.Euler(-12,0,0);
  Box("Trigger guard",gun,new Vector3(0,-.094f,.06f),new Vector3(.055f,.014f,.1f),steel);Box("Guard front",gun,new Vector3(0,-.071f,.106f),new Vector3(.055f,.06f,.012f),steel);
  Box("Rear sight",gun,new Vector3(0,.074f,-.04f),new Vector3(.078f,.028f,.025f),steel);Box("Front sight",gun,new Vector3(0,.071f,.165f),new Vector3(.015f,.022f,.018f),glow);
  for(int i=0;i<4;i++)Box("Slide serration "+i,gun,new Vector3(-.044f,.025f,-.046f+i*.016f),new Vector3(.008f,.066f,.006f),steel);
  Box("Right glove",gun,new Vector3(.012f,-.12f,-.045f),new Vector3(.10f,.10f,.105f),grip);Box("Wrist cuff",gun,new Vector3(.045f,-.17f,-.13f),new Vector3(.11f,.09f,.14f),steel);Box("Sleeve",gun,new Vector3(.065f,-.21f,-.24f),new Vector3(.13f,.12f,.20f),steel);
  arsenal.muzzle=new GameObject("Pistol muzzle").transform;arsenal.muzzle.SetParent(gun,false);arsenal.muzzle.localPosition=new Vector3(0,.02f,.21f);arsenal.muzzleLight=arsenal.muzzle.gameObject.AddComponent<Light>();arsenal.muzzleLight.color=Color.cyan;arsenal.muzzleLight.range=3;arsenal.muzzleLight.intensity=0;
 }
}
