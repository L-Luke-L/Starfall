using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Starfall;

public static class LostRealmBuilder {
 const string RegionFolder="Assets/Art/LostRealms/";
 static readonly string[] ScenePaths={"","","Assets/Scenes/Scene04_Battlefield.unity","Assets/Scenes/Scene05_Medical.unity","Assets/Scenes/Scene06_Shipyard.unity","Assets/Scenes/Scene07_Core.unity"};
 static readonly Color[] Fog={Color.black,Color.black,new Color(.41f,.27f,.17f),new Color(.15f,.30f,.42f),new Color(.23f,.25f,.33f),new Color(.12f,.10f,.18f)};
 static readonly Color[] Metal={Color.black,Color.black,new Color(.36f,.28f,.22f),new Color(.34f,.49f,.53f),new Color(.28f,.34f,.42f),new Color(.19f,.18f,.24f)};
 static readonly Color[] Glow={Color.black,Color.black,new Color(.94f,.47f,.18f),new Color(.39f,.86f,.98f),new Color(.80f,.60f,.27f),new Color(.72f,.41f,1)};
 static Material SaveMaterial(string name,Color color,bool bright=false){var path=RegionFolder+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);if(!result){result=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(result,path);}result.name=name;result.color=color;result.SetFloat("_Metallic",.36f);result.SetFloat("_Glossiness",.24f);if(bright){result.EnableKeyword("_EMISSION");result.SetColor("_EmissionColor",color*1.3f);}EditorUtility.SetDirty(result);return result;}
 static Material SaveGround(string name,Color rock,Color dust){var path=RegionFolder+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);if(!result){result=new Material(Shader.Find("Starfall/AlienGround"));AssetDatabase.CreateAsset(result,path);}result.SetColor("_Color",rock);result.SetColor("_Dust",dust);result.SetFloat("_Scale",.95f);EditorUtility.SetDirty(result);return result;}
 static void ThemeExisting(int region){Color baseRock=region==2?new Color(.33f,.25f,.23f):region==3?new Color(.16f,.29f,.34f):region==4?new Color(.17f,.22f,.27f):new Color(.12f,.11f,.19f);Color dust=region==2?new Color(.65f,.35f,.18f):region==3?new Color(.31f,.52f,.57f):region==4?new Color(.36f,.33f,.31f):new Color(.27f,.18f,.37f);
  var ground=SaveGround("Region "+region+" soil",baseRock,dust);var rock=SaveGround("Region "+region+" stone",baseRock*.6f,dust*.55f);var dark=SaveGround("Region "+region+" deep rock",baseRock*.34f,dust*.31f);
  var originalGround=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/AlienSurface/BasinSoil.mat");var originalRock=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/AlienSurface/StratifiedBasalt.mat");var originalDark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/AlienSurface/DarkBasalt.mat");
  foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){var slots=renderer.sharedMaterials;bool changed=false;for(int i=0;i<slots.Length;i++){if(slots[i]==originalGround){slots[i]=ground;changed=true;}else if(slots[i]==originalRock){slots[i]=rock;changed=true;}else if(slots[i]==originalDark){slots[i]=dark;changed=true;}}if(changed)renderer.sharedMaterials=slots;}
 }
 static Transform Group(string name,Transform parent=null){var g=new GameObject(name).transform;if(parent)g.SetParent(parent);return g;}
 static Transform Part(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collide=true,bool walkable=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=pos;g.transform.localScale=scale;var renderer=g.GetComponent<Renderer>();renderer.sharedMaterial=mat;if(!collide||scale.y<2.5f)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(!collide)Object.DestroyImmediate(g.GetComponent<Collider>());else if(!walkable){var nav=g.AddComponent<NavMeshObstacle>();nav.shape=NavMeshObstacleShape.Box;nav.size=Vector3.one;nav.carving=true;}return g.transform;}
 static float Y(float x,float z)=>AlienSurfaceLandmarks.Height(x,z);
 static void Ramp(string name,Transform root,Vector3 start,Vector3 end,Material metal){
  // One walkable surface avoids overlapping solid boxes being baked as low ceilings.
  var direction=end-start;var right=Vector3.Cross(Vector3.up,direction).normalized*2.4f;start+=Vector3.up*.125f;end+=Vector3.up*.125f;
  var mesh=new Mesh{name=name};mesh.vertices=new[]{start-right,start+right,end-right,end+right};mesh.triangles=new[]{0,2,1,2,3,1};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.RecalculateNormals();mesh.RecalculateBounds();
  string path=RegionFolder+(name.Contains("internal")?"ShipyardInternalRamp":"ShipyardEntranceRamp")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
  var g=new GameObject(name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=metal;g.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 static void StripResources(FirstPersonExplorer player,int region){if(region!=5)return;foreach(var actor in player.arena.creatures)Object.DestroyImmediate(actor.gameObject);player.arena.creatures=new AlienCreature[0];foreach(var r in player.expedition.resources)Object.DestroyImmediate(r.gameObject);player.expedition.resources=new BasinResource[0];var titan=Object.FindFirstObjectByType<SkyTitan>();if(titan)Object.DestroyImmediate(titan.gameObject);}
 static void ImportCompanions(){string[] names={"ShiguangDrone","LanEngineer","SuheDoctor"};foreach(var name in names){var path="Assets/Resources/Companions/"+name+".png";if(!File.Exists(path))continue;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();}}
 public static void Generate(){Directory.CreateDirectory(RegionFolder);Directory.CreateDirectory("Assets/Resources");ImportCompanions();Catalog();VoyageCatalogBuilder.Generate();for(int region=2;region<=5;region++){
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Encounter.unity");var player=Object.FindFirstObjectByType<FirstPersonExplorer>();StripResources(player,region);ThemeExisting(region);
   var structure=SaveMaterial("Region "+region+" metal",Metal[region]);var dark=SaveMaterial("Region "+region+" dark",Metal[region]*.42f);var light=SaveMaterial("Region "+region+" signal",Glow[region],true);
   var root=Group("Region "+region+" · independent landmarks");
   if(region==2)Battlefield(root,structure,dark,light);
   else if(region==3)Medical(root,structure,dark,light);
   else if(region==4)Shipyard(root,structure,dark,light);
   else Core(root,structure,dark,light);
   RenderSettings.fog=true;RenderSettings.fogColor=Fog[region];RenderSettings.fogDensity=region==2?.0065f:.005f;RenderSettings.ambientSkyColor=Color.Lerp(Fog[region],Color.white,.36f);RenderSettings.ambientEquatorColor=Color.Lerp(Fog[region],Color.white,.21f);RenderSettings.ambientGroundColor=Fog[region]*.38f;
   player.arena.maxSimultaneousPursuers=3;
   BakeNavigation(player,region);
   EditorSceneManager.SaveScene(scene,ScenePaths[region],true);
  }}
 static void Battlefield(Transform root,Material metal,Material dark,Material glow){
  // The through road is open; collapsed bridge and trenches create two lateral combat loops.
  for(int i=0;i<6;i++){float z=-25+i*9;Part("Fractured evacuation road "+i,root,new Vector3(0,Y(0,z)+.05f,z),new Vector3(6,.13f,7),dark,false);if(i%2==1){Part("Raised broken roadway",root,new Vector3(6,Y(6,z)+.27f,z),new Vector3(4,.5f,5),metal,true,true);}}
  for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++){float x=side*(17+i*5),z=-17+i*12;Part("Trench wall "+side+" "+i,root,new Vector3(x,Y(x,z)+.35f,z),new Vector3(2,.75f,9),dark,true);Part("Evacuation barrier "+side+" "+i,root,new Vector3(x+side*3,Y(x+side*3,z)+.6f,z+2),new Vector3(3,1.2f,.65f),metal,true);}
  for(int i=0;i<7;i++){float x=(i%2==0?-1:1)*(24+i*3),z=-27+i*11;var vehicle=Group("Burned evacuation vehicle "+i,root);Part("Collapsed hull",vehicle,new Vector3(x,Y(x,z)+.7f,z),new Vector3(3.5f,1.2f,5.4f),metal,true);Part("Dark cabin",vehicle,new Vector3(x,Y(x,z)+1.35f,z-1),new Vector3(2.7f,.9f,2.5f),dark,false);Part("Signal plate",vehicle,new Vector3(x,Y(x,z)+1.6f,z-2.26f),new Vector3(.8f,.19f,.05f),glow,false);}
  for(int i=0;i<4;i++){float x=-35+i*19,z=42;Part("Distant broken signal tower",root,new Vector3(x,Y(x,z)+6,z),new Vector3(1,12,1),metal,true);Part("Tower warning",root,new Vector3(x,Y(x,z)+12,z),new Vector3(.7f,.7f,.7f),glow,false);}
 }
 static void Medical(Transform root,Material metal,Material dark,Material glow){
  for(int wing=0;wing<3;wing++){float x=-22+wing*22,z=7+wing*6;var house=Group("Medical isolation wing "+wing,root);
   Part("Habitat floor",house,new Vector3(x,Y(x,z)+.11f,z),new Vector3(12,.22f,11),metal,true,true);
   for(int side=-1;side<=1;side+=2){Part("Habitat side wall",house,new Vector3(x+side*5.7f,Y(x,z)+2,z),new Vector3(.55f,4,11),metal,true);Part("Cold glass window",house,new Vector3(x+side*5.99f,Y(x,z)+2.1f,z),new Vector3(.05f,1.2f,4),glow,false);}
   Part("Habitat rear",house,new Vector3(x,Y(x,z)+2,z+5.5f),new Vector3(12,4,.6f),dark,true);
   Part("Broken roof left",house,new Vector3(x-3,Y(x,z)+4.3f,z+1),new Vector3(5,.4f,8),metal,true);
   Part("Broken roof right",house,new Vector3(x+3.5f,Y(x,z)+4.3f,z+3),new Vector3(3,.4f,4),metal,true);
  }
  for(int i=0;i<12;i++){float a=i*2.4f,x=Mathf.Cos(a)*35,z=Mathf.Sin(a)*27+12;var crystal=Part("Independent frost crystal "+i,root,new Vector3(x,Y(x,z)+1.4f,z),new Vector3(.6f,3+i%3,1),glow,false);crystal.rotation=Quaternion.Euler(i%4*10,i*37,i%3*11);}
  for(int i=0;i<9;i++){float x=-40+i*10,z=-7+i%3*10;Part("Greenhouse bed "+i,root,new Vector3(x,Y(x,z)+.35f,z),new Vector3(2,.7f,4),dark,true);Part("Dormant plant tube "+i,root,new Vector3(x,Y(x,z)+1.0f,z),new Vector3(.35f,1,.35f),glow,false);}
 }
 static void Shipyard(Transform root,Material metal,Material dark,Material glow){
  var footprint=new Bounds(new Vector3(18,5,12),new Vector3(24,30,56));foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if((renderer.name.StartsWith("Cliff column")||renderer.name.StartsWith("Loose rock")||renderer.name.StartsWith("Field boulder")||renderer.name.StartsWith("Slanted spire"))&&renderer.bounds.Intersects(footprint))Object.DestroyImmediate(renderer.gameObject);
  var hull=Group("Broken capital vessel · walkable shell",root);
  Part("Broken vessel prow on horizon",hull,new Vector3(1,Y(1,31)+5,31),new Vector3(15,10,8),dark,true);
  Part("Vessel lower keel entrance",hull,new Vector3(17,Y(17,19)+1,6),new Vector3(17,2,9),dark,true,true);
  // The ramp occupies an actual opening, not a second surface over a solid floor.
  for(int side=-1;side<=1;side+=2)Part("Vessel lower side corridor "+side,hull,new Vector3(17+side*5.55f,Y(17,19)+1,19),new Vector3(5.9f,2,35),dark,true,true);
  Part("Vessel lower aft passage",hull,new Vector3(17,Y(17,19)+1,30.25f),new Vector3(5.2f,2,12.5f),dark,true,true);
  Part("Vessel port armor",hull,new Vector3(7,Y(7,19)+4,19),new Vector3(1.4f,8,28),metal,true);
  Part("Vessel starboard armor",hull,new Vector3(27,Y(27,19)+4,19),new Vector3(1.4f,8,28),metal,true);
  Part("Broken foredeck",hull,new Vector3(17,Y(17,30)+5,29),new Vector3(17,.5f,13),metal,true,true);
  Ramp("Accessible repair ramp · ground to lower vessel",hull,new Vector3(17,Y(17,-12)-.12f,-12),new Vector3(17,Y(17,19)+1.88f,2.8f),metal);
  Ramp("Accessible internal ramp · lower vessel to foredeck",hull,new Vector3(17,Y(17,19)+1.70f,10),new Vector3(17,Y(17,30)+5.13f,23.7f),metal);
  for(int i=0;i<5;i++){float z=-20+i*12;Part("Gantry foot "+i,root,new Vector3(-28,Y(-28,z)+3,z),new Vector3(1.2f,6,1.2f),metal,true);Part("Suspended gantry "+i,root,new Vector3(-19,Y(-19,z)+6,z),new Vector3(19,.7f,1.7f),dark,true);Part("Hazard light "+i,root,new Vector3(-28,Y(-28,z)+6.4f,z),new Vector3(.6f,.2f,.6f),glow,false);}
  for(int i=0;i<9;i++){float x=-37+i%3*7,z=9+i/3*9;Part("Independent cargo container "+i,root,new Vector3(x,Y(x,z)+.9f,z),new Vector3(3.5f,1.8f,3.5f),i%2==0?metal:dark,true);}
 }
 static void Core(Transform root,Material metal,Material dark,Material glow){
  for(int i=0;i<18;i++){float a=i*Mathf.PI*2/18,x=Mathf.Cos(a)*28,z=11+Mathf.Sin(a)*25;var shard=Part("Suspended basalt shard "+i,root,new Vector3(x,Y(x,z)+5+i%4,z),new Vector3(1.6f,4+i%3,2),dark,false);shard.rotation=Quaternion.Euler(i*13,i*31,i*19);}
  for(int i=0;i<8;i++){float a=i*Mathf.PI*2/8,x=Mathf.Cos(a)*17,z=11+Mathf.Sin(a)*17;Part("D4 ring pylon "+i,root,new Vector3(x,Y(x,z)+3,z),new Vector3(1.6f,6,1.6f),metal,true);Part("D4 pylon beam "+i,root,new Vector3(x,Y(x,z)+6.1f,z),new Vector3(.9f,.15f,.9f),glow,false);}
  for(int side=-1;side<=1;side+=2){Part("Outer approach cover "+side,root,new Vector3(side*14,Y(side*14,-4)+1.2f,-4),new Vector3(4,2.4f,2),dark,true);Part("Inner approach cover "+side,root,new Vector3(side*8,Y(side*8,5)+1.2f,5),new Vector3(3,2.4f,2),metal,true);}
 }
 static void BakeNavigation(FirstPersonExplorer player,int region){var sources=new List<NavMeshBuildSource>();foreach(var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){if(col.GetComponentInParent<FirstPersonExplorer>()||col.GetComponentInParent<AlienCreature>()||col.GetComponentInParent<SkyTitan>())continue;
   if(col is MeshCollider mesh&&mesh.sharedMesh)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh.sharedMesh,transform=mesh.transform.localToWorldMatrix,area=0});
   else if(col is BoxCollider box)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(box.transform.TransformPoint(box.center),box.transform.rotation,box.transform.lossyScale),size=box.size,area=box.GetComponent<NavMeshObstacle>()?1:0});}
   var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.95f;settings.agentHeight=2;settings.agentClimb=.25f;settings.agentSlope=42;settings.overrideVoxelSize=true;settings.voxelSize=.22f;var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(238,90,238)),Vector3.zero,Quaternion.identity);if(!data)throw new System.Exception("Lost realm navigation failed "+region);
   if(region==4){var instance=NavMesh.AddNavMeshData(data);Vector3[] checks={player.returnBeacon.position,new Vector3(17,Y(17,-12),-12),new Vector3(17,Y(17,19)+2,8),new Vector3(17,Y(17,19)+3,18),new Vector3(17,Y(17,30)+5.25f,27)};for(int i=1;i<checks.Length;i++){var route=new NavMeshPath();bool start=NavMesh.SamplePosition(checks[i-1],out var from,4,NavMesh.AllAreas),end=NavMesh.SamplePosition(checks[i],out var to,4,NavMesh.AllAreas);bool connected=start&&end&&NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,route)&&route.status==NavMeshPathStatus.PathComplete;Debug.Log("SHIPYARD_NAV "+i+" "+connected+" from="+from.position+" to="+to.position+" status="+route.status);}instance.Remove();}
   string path=RegionFolder+"Region"+region+"Navigation.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);if(old){EditorUtility.CopySerialized(data,old);Object.DestroyImmediate(data);data=old;}else AssetDatabase.CreateAsset(data,path);player.arena.navigation=data;
 }
 static void Catalog(){var path="Assets/Resources/LostRealmCatalog.asset";var catalog=AssetDatabase.LoadAssetAtPath<LostRealmCatalog>(path);if(!catalog){catalog=ScriptableObject.CreateInstance<LostRealmCatalog>();AssetDatabase.CreateAsset(catalog,path);}
  catalog.weapons=new[]{new LostRealmCatalog.Weapon{id="pistol",title="P12手枪",magazine=12,ammoPerShot=1,bodyDamage=22,weakDamage=36,range=160,reload=2.35f,interval=.28f},new LostRealmCatalog.Weapon{id="scatter",title="破障霰射器",magazine=8,ammoPerShot=2,bodyDamage=14,weakDamage=23,range=18,reload=2.7f,interval=.72f},new LostRealmCatalog.Weapon{id="rail",title="磁轨穿刺枪",magazine=3,ammoPerShot=3,bodyDamage=70,weakDamage=120,range=100,reload=1.8f,interval=1.25f,charge=.9f}};
  catalog.modules=new[]{new LostRealmCatalog.Module{id="stamina",title="续航电容",description="体力上限 +25",unlock="doctor-rescued"},new LostRealmCatalog.Module{id="reload",title="快换机构",description="换弹时间 -20%",unlock="engineer-rescued"},new LostRealmCatalog.Module{id="magnet",title="拾荒磁环",description="普通掉落自动拾取",unlock="drone-rescued"},new LostRealmCatalog.Module{id="weakness",title="猎手镜组",description="弱点伤害 +20%",unlock="signals-complete"}};
  catalog.quests=new[]{new LostRealmCatalog.Quest{id="antenna",title="修复三段天线",companion="拾光",reward="扫描"},new LostRealmCatalog.Quest{id="signals",title="记录异常信号",companion="拾光",reward="猎手镜组"},new LostRealmCatalog.Quest{id="engineer",title="开启避难舱",companion="岚",reward="霰射器与工坊"},new LostRealmCatalog.Quest{id="crane",title="恢复吊机",companion="岚",reward="磁轨枪"},new LostRealmCatalog.Quest{id="doctor",title="解除隔离",companion="苏禾",reward="医疗设施"},new LostRealmCatalog.Quest{id="samples",title="收集生态样本",companion="苏禾",reward="温室"}};
  catalog.regions=new[]{new LostRealmCatalog.Region{id=1,title="探险船遗骸",environment="断崖盆地",uniqueEvent="天线阵列",fog=new Color(.2f,.27f,.31f),accent=Glow[2]},new LostRealmCatalog.Region{id=2,title="撤离战场",environment="沙尘战壕",uniqueEvent="沙墙",fog=Fog[2],accent=Glow[2]},new LostRealmCatalog.Region{id=3,title="同盟医疗站",environment="结晶废墟",uniqueEvent="结晶共鸣",fog=Fog[3],accent=Glow[3]},new LostRealmCatalog.Region{id=4,title="军用船坞",environment="巨舰废墟",uniqueEvent="电磁脉冲",fog=Fog[4],accent=Glow[4]},new LostRealmCatalog.Region{id=5,title="D4空间核心",environment="玄武岩环",uniqueEvent="核心脉冲",fog=Fog[5],accent=Glow[5]}};EditorUtility.SetDirty(catalog);
 }
}
