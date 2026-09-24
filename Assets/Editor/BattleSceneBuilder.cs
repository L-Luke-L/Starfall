using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Starfall;

public static class BattleSceneBuilder {
 const string Folder="Assets/Art/AlienSurface/",ScenePath="Assets/Scenes/Scene02_Encounter.unity";
 static System.Random random;
 static Material groundMat,rockMat,darkRock,crystalMat,steel,amber,cyan;
 static Mesh[] rocks;
 static float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
 static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent)t.SetParent(parent);return t;}
 static T Save<T>(T item,string name) where T:UnityEngine.Object {string path=Folder+name;var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior){EditorUtility.CopySerialized(item,prior);UnityEngine.Object.DestroyImmediate(item);EditorUtility.SetDirty(prior);return prior;}AssetDatabase.CreateAsset(item,path);return item;}
 static Material Lit(string name,Color c,float metallic=0,float smooth=.2f){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);return Save(m,name+".mat");}
 static Material Glow(string name,Color c){var m=Lit(name,c,.2f,.4f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.7f);EditorUtility.SetDirty(m);return m;}
 static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material mat,bool collide=true){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent);o.transform.position=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;if(!collide)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
 static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat,Vector3 position,Vector3 scale,bool collide=true){var o=Group(name,parent).gameObject;o.transform.position=position;o.transform.localScale=scale;o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collide)o.AddComponent<MeshCollider>().sharedMesh=mesh;o.isStatic=true;return o;}
 static Mesh FacetedRock(int seed){
  var rng=new System.Random(seed);const int sides=7,levels=5;var rings=new Vector3[levels*sides];
  for(int y=0;y<levels;y++)for(int i=0;i<sides;i++){float a=(i/(float)sides)*Mathf.PI*2;float rad=(y==levels-1?.32f:y==0?.80f:1)*(float)(.83+rng.NextDouble()*.25);rings[y*sides+i]=new Vector3(Mathf.Cos(a)*rad+(y/(float)levels)*.15f,y/(float)(levels-1),Mathf.Sin(a)*rad);}
  var verts=new List<Vector3>();var triangles=new List<int>();
  void Tri(Vector3 a,Vector3 b,Vector3 c){int k=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);}
  for(int y=0;y<levels-1;y++)for(int i=0;i<sides;i++){int j=(i+1)%sides;Tri(rings[y*sides+i],rings[(y+1)*sides+i],rings[y*sides+j]);Tri(rings[y*sides+j],rings[(y+1)*sides+i],rings[(y+1)*sides+j]);}
  for(int i=0;i<sides;i++)Tri(Vector3.up,rings[(levels-1)*sides+(i+1)%sides],rings[(levels-1)*sides+i]);
  var mesh=new Mesh{name="Faceted basalt "+seed};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 static Mesh GroundMesh(){
  const int n=161;var v=new Vector3[n*n];var uv=new Vector2[v.Length];var t=new int[(n-1)*(n-1)*6];int k=0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){float px=-120+x*1.5f,pz=-120+z*1.5f;int i=z*n+x;v[i]=new Vector3(px,AlienSurfaceLandmarks.Height(px,pz),pz);uv[i]=new Vector2(px,pz);if(x<n-1&&z<n-1){t[k++]=i;t[k++]=i+n;t[k++]=i+1;t[k++]=i+1;t[k++]=i+n;t[k++]=i+n+1;}}
  var mesh=new Mesh{name="240m alien basalt basin"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 static Mesh RingMesh(){var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<=192;i++){float a=i*Mathf.PI*2/192;v.Add(new Vector3(Mathf.Cos(a)*.64f,0,Mathf.Sin(a)*.64f));v.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));if(i>0){int k=i*2;t.Add(k-2);t.Add(k);t.Add(k-1);t.Add(k-1);t.Add(k);t.Add(k+1);}}var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();return m;}
 static Transform CrystalCluster(Transform parent,Vector3 point,int id){var group=Group("Luminous mineral cluster "+id,parent);group.position=point;for(int j=0;j<4;j++){var g=MeshObject("Crystal "+j,group,rocks[j%rocks.Length],crystalMat,point+new Vector3(R(-.65f,.65f),-.08f,R(-.65f,.65f)),new Vector3(R(.14f,.32f),R(.6f,1.7f),R(.14f,.32f)));g.transform.rotation=Quaternion.Euler(R(-20,20),R(0,360),R(-18,18));}return group;}
 public static void ConfigureGateway(SceneDirector d){var bridge=d.GetComponent<BattleTravel>();if(!bridge)bridge=d.gameObject.AddComponent<BattleTravel>();bridge.director=d;bridge.encounter=d.encounter;d.battle=bridge;if(!d.GetComponent<BattleVerification>())d.gameObject.AddComponent<BattleVerification>();if(!d.GetComponent<CreatureVerification>())d.gameObject.AddComponent<CreatureVerification>();if(!d.GetComponent<TitanVerification>())d.gameObject.AddComponent<TitanVerification>();if(!d.GetComponent<DragonCombatVerification>())d.gameObject.AddComponent<DragonCombatVerification>();if(!d.GetComponent<HardCombatVerification>())d.gameObject.AddComponent<HardCombatVerification>();if(!d.GetComponent<ImmersionVerification>())d.gameObject.AddComponent<ImmersionVerification>();WormholeBuilder.Configure(d);CampaignBuilder.Configure(d);if(!d.GetComponent<WormholeVerification>())d.gameObject.AddComponent<WormholeVerification>();}
 [MenuItem("Starfall/Create First Person Encounter and Build")]
 public static void InstallAndBuild(){
  BuildScene();var original=EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Starfall.unity");ForgeBuilder.Configure(UnityEngine.Object.FindFirstObjectByType<SceneDirector>());EditorSceneManager.SaveScene(original);AssetDatabase.SaveAssets();StarfallBuilder.Build();
 }
 public static void BuildScene(){
  Directory.CreateDirectory(Folder);random=new System.Random(901186);var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var g=new Material(Shader.Find("Starfall/AlienGround"));g.SetColor("_Color",new Color(.23f,.28f,.30f));g.SetColor("_Dust",new Color(.62f,.40f,.23f));groundMat=Save(g,"BasinSoil.mat");
  g=new Material(groundMat);g.SetColor("_Color",new Color(.10f,.16f,.19f));g.SetColor("_Dust",new Color(.32f,.34f,.31f));g.SetFloat("_Scale",.7f);rockMat=Save(g,"StratifiedBasalt.mat");
  g=new Material(groundMat);g.SetColor("_Color",new Color(.07f,.10f,.14f));g.SetColor("_Dust",new Color(.22f,.25f,.3f));darkRock=Save(g,"DarkBasalt.mat");
  steel=Lit("BeaconSteel",new Color(.20f,.28f,.31f),.65f,.3f);amber=Glow("BeaconAmber",new Color(.8f,.43f,.13f));cyan=Glow("BeaconCyan",new Color(.10f,.57f,.68f));crystalMat=Glow("MineralCyan",new Color(.08f,.37f,.45f));
  rocks=new Mesh[8];for(int i=0;i<rocks.Length;i++)rocks[i]=Save(FacetedRock(600+i),"Rock"+i+".asset");
  var world=Group("Alien surface · independent 3D environment");var landmarks=world.gameObject.AddComponent<AlienSurfaceLandmarks>();
  var ground=MeshObject("Walkable undulating ground",world,Save(GroundMesh(),"BasinGround.asset"),groundMat,Vector3.zero,Vector3.one);landmarks.ground=ground.GetComponent<Collider>();
  var formations=Group("Basalt outcrops and cliffs",world);
  for(int i=0;i<74;i++){float a=i*Mathf.PI*2/74;float r=R(66,98),x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r;float h=R(7,23);var o=MeshObject("Cliff column "+i,formations,rocks[i%8],i%4==0?darkRock:rockMat,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)-1,z),new Vector3(R(3.8f,7),h,R(3.8f,7)));o.transform.rotation=Quaternion.Euler(R(-9,9),R(0,360),R(-8,8));}
  for(int i=0;i<45;i++){float x=R(-57,57),z=R(-53,60);if(new Vector2(x,z+38).magnitude<11||new Vector2(x,z-9).magnitude<17)continue;var o=MeshObject("Field boulder "+i,formations,rocks[i%8],rockMat,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)-.3f,z),new Vector3(R(1,3.8f),R(1.1f,4.6f),R(1.1f,3.6f)));o.transform.Rotate(0,R(0,360),0);}
  var landmark=Group("Split basalt spires",world);for(int i=0;i<5;i++){float x=16+i*2.1f,z=17+Mathf.Sin(i)*2;var o=MeshObject("Slanted spire "+i,landmark,rocks[i],darkRock,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)-.3f,z),new Vector3(2.2f,12-i*1.2f,2.1f));o.transform.rotation=Quaternion.Euler(0,i*29,-14+i*3);}
  var detail=Group("Loose surface stones",world);for(int i=0;i<260;i++){float x=R(-76,76),z=R(-75,75);if(new Vector2(x,z+38).magnitude<5)continue;MeshObject("Loose rock "+i,detail,rocks[i%8],rockMat,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)-.09f,z),new Vector3(R(.12f,.55f),R(.08f,.40f),R(.15f,.55f)),false);}
  var crystals=Group("Independent luminous minerals",world);for(int i=0;i<15;i++){float a=i*Mathf.PI*2/15,r=R(18,49),x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r;CrystalCluster(crystals,new Vector3(x,AlienSurfaceLandmarks.Height(x,z),z),i);}
  // Return beacon doubles as a clear landmark visible from the starting clearing.
  var beacon=Group("Return beacon · interaction",world);beacon.position=new Vector3(-3,0,-39);
  Primitive("Landing plate",PrimitiveType.Cylinder,beacon,beacon.position+Vector3.up*.07f,new Vector3(3,.07f,3),steel);
  Primitive("Beacon column",PrimitiveType.Cylinder,beacon,beacon.position+Vector3.up*1.1f,new Vector3(.38f,1.03f,.38f),steel);
  Primitive("Return signal",PrimitiveType.Cylinder,beacon,beacon.position+Vector3.up*2.12f,new Vector3(.46f,.14f,.46f),cyan,false);
  for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Primitive("Amber landing marker "+i,PrimitiveType.Cube,beacon,beacon.position+new Vector3(Mathf.Cos(a)*1.2f,.17f,Mathf.Sin(a)*1.2f),new Vector3(.18f,.12f,.18f),amber,false);}
  var lamp=Group("Beacon local light",beacon).gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.2f,.72f,.85f);lamp.range=7;lamp.intensity=1.5f;lamp.transform.position=beacon.position+Vector3.up*2;
  landmarks.returnBeacon=beacon;
  var testRock=MeshObject("Arrival-side solid boulder",formations,rocks[0],rockMat,new Vector3(7,AlienSurfaceLandmarks.Height(7,-30)-.25f,-30),new Vector3(2.5f,3.5f,2.5f));landmarks.collisionTestRock=testRock.GetComponent<Collider>();
  var bounds=Group("Outer safety limits · behind cliffs",world);for(int i=0;i<4;i++){var t=Group("Boundary "+i,bounds);t.position=i<2?new Vector3(i==0?-116:116,13,0):new Vector3(0,13,i==2?-116:116);var c=t.gameObject.AddComponent<BoxCollider>();c.size=i<2?new Vector3(2,60,236):new Vector3(236,60,2);}
  var spawn=Group("Arrival marker",world);spawn.position=new Vector3(0,.08f,-38);landmarks.arrival=spawn;
  var future=Group("Reserved enemy spawn points",world);landmarks.futureEnemySpawns=new Transform[4];for(int i=0;i<4;i++){var p=Group("Encounter spawn "+i,future);float x=(i%2==0?-1:1)*10,z=i<2?0:26;p.position=new Vector3(x,AlienSurfaceLandmarks.Height(x,z)+.2f,z);landmarks.futureEnemySpawns[i]=p;}
  var sky=Group("Sky objects · separate planet and ring",world);var planet=Primitive("Distant gas giant",PrimitiveType.Sphere,sky,new Vector3(-140,119,285),Vector3.one*126,Save(new Material(Shader.Find("Starfall/AlienPlanet")),"GasGiant.mat"),false);planet.transform.rotation=Quaternion.Euler(8,0,-22);
  var ring=MeshObject("Planet orbital ring",sky,Save(RingMesh(),"OrbitRing.asset"),Save(new Material(Shader.Find("Starfall/AlienRing")),"RingDust.mat"),planet.transform.position,Vector3.one*108,false);ring.transform.rotation=Quaternion.Euler(20,0,-18);
  // Ring shading measures local radius; keep its vertices out of static world-space batches.
  ring.isStatic=false;
  var sun=Group("Low alien sun").gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(19,-48,0);sun.color=new Color(1,.78f,.55f);sun.intensity=1.15f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;
  RenderSettings.skybox=Save(new Material(Shader.Find("Starfall/AlienSky")),"AlienSky.mat");RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.40f,.56f);RenderSettings.ambientEquatorColor=new Color(.22f,.29f,.34f);RenderSettings.ambientGroundColor=new Color(.10f,.11f,.14f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.20f,.27f,.31f);RenderSettings.fogDensity=.0048f;
  QualitySettings.shadowDistance=100;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadows=ShadowQuality.All;
  var player=Group("First-person explorer");player.position=spawn.position;var motor=player.gameObject.AddComponent<CharacterController>();motor.height=1.84f;motor.center=new Vector3(0,.92f,0);motor.radius=.33f;motor.stepOffset=.30f;motor.slopeLimit=48;motor.skinWidth=.035f;
  var fps=player.gameObject.AddComponent<FirstPersonExplorer>();fps.returnBeacon=beacon;
  var camera=Group("First-person camera",player).gameObject.AddComponent<Camera>();camera.transform.localPosition=new Vector3(0,1.64f,0);camera.fieldOfView=78;camera.nearClipPlane=.06f;camera.farClipPlane=650;camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();fps.viewCamera=camera;
  CreatureBuilder.Install(fps,landmarks);
  TitanBuilder.Install(fps,sun);HardCombatBuilder.Install(fps);
  EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Scene01_Starfall.unity",true),new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
 }
}

