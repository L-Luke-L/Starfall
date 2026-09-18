using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Starfall;

public static class StarfallBuilder {
 static Sprite pixel,glow,rock;
 static Material lineMat,grainMat;
 static System.Random rng;
 static Color C(string h)=>WorldArt.Hex(h);
 static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
 static Transform Group(string name,Transform parent=null){var g=new GameObject(name).transform;if(parent)g.SetParent(parent);return g;}
 static Sprite Import(string path,Vector2 pivot,float ppu=100){
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=ppu;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;
  var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;ti.SetTextureSettings(settings);ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static Sprite Save(string name,Texture2D t,float ppu=100){string path="Assets/Art/Generated/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);return Import(path,new Vector2(.5f,.5f),ppu);}
 static SpriteRenderer Image(string name,Transform root,Sprite s,float x,float y,float width,int order,Color? tint=null){
  var g=Group(name,root);g.position=new Vector3(x,y,0);float k=width/s.bounds.size.x;g.localScale=Vector3.one*k;
  var sr=g.gameObject.AddComponent<SpriteRenderer>();sr.sprite=s;sr.sortingOrder=order;sr.color=tint??Color.white;return sr;
 }
 static Transform Rect(string name,Transform root,float x,float y,float w,float h,string color,int order){var s=Image(name,root,pixel,x,y,w,order,C(color));s.transform.localScale=new Vector3(w,h,1);return s.transform;}
 static void Polygon(string name,Transform root,Vector2[] points,Color c,int order){
  var o=Group(name,root);var m=new Mesh();var vs=new Vector3[points.Length];var colors=new Color[points.Length];var ts=new int[(points.Length-2)*3];
  for(int i=0;i<points.Length;i++){vs[i]=points[i];colors[i]=c;}for(int i=0;i<points.Length-2;i++){ts[i*3]=0;ts[i*3+1]=i+1;ts[i*3+2]=i+2;}
  m.vertices=vs;m.triangles=ts;m.colors=colors;var uv=new Vector2[vs.Length];for(int i=0;i<vs.Length;i++)uv[i]=new Vector2(vs[i].x*.28f,vs[i].y*.28f);m.uv=uv;m.RecalculateBounds();string path="Assets/Art/Generated/"+name.Replace(" ","_")+".asset";AssetDatabase.CreateAsset(m,path);
  o.gameObject.AddComponent<MeshFilter>().sharedMesh=m;var mr=o.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=name.StartsWith("Ridge")?grainMat:lineMat;mr.sortingOrder=order;
 }
 static LineRenderer Line(string name,Transform parent,Vector3[] points,float width,Color c,int order){var t=Group(name,parent);var l=t.gameObject.AddComponent<LineRenderer>();l.sharedMaterial=lineMat;l.useWorldSpace=true;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=l.endWidth=width;l.startColor=l.endColor=c;l.sortingOrder=order;return l;}
 static Interactable Site(string name,SiteKind kind,Transform root,Sprite art,float x,float y,float width,int order){
  var obj=Group(name,root);obj.position=new Vector3(x,y,0);Image("Artwork",obj,art,x,y,width,order);
  var site=obj.gameObject.AddComponent<Interactable>();site.kind=kind;site.displayName=kind==SiteKind.Camp?"前哨营地":kind==SiteKind.Mine?"中央矿井":"失事飞船";
  site.description="Independent interaction target. Add future responses to On Interact in the Inspector.";
  var col=obj.gameObject.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(width*.9f,2);col.offset=Vector2.up;
  return site;
 }
 [MenuItem("Starfall/Create Scene 01")]
 public static void Create(){
  rng=new System.Random(186);
  Directory.CreateDirectory("Assets/Art/Generated");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Prefabs");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  PlayerSettings.companyName="Starfall Team";PlayerSettings.productName="星陨边境 · 场景 01";
  PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
  PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
  QualitySettings.antiAliasing=0;
  lineMat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(lineMat,"Assets/Art/Generated/UnlitSprites.mat");
  pixel=Save("Pixel",WorldArt.Texture(1,1,(x,y)=>Color.white),1);glow=Save("Glow",WorldArt.Glow(),64);rock=Save("Basalt",WorldArt.Rock(),64);
  var flame=Save("Flame",WorldArt.Flame(),20);var grain=Save("Grain",WorldArt.Grain(),128);grain.texture.wrapMode=TextureWrapMode.Repeat;
  grainMat=new Material(lineMat);grainMat.mainTexture=grain.texture;AssetDatabase.CreateAsset(grainMat,"Assets/Art/Generated/RidgeGrain.mat");
  var moon=Save("Moon",WorldArt.Moon(),256);
  var tent=Import("Assets/Art/Camp.png",new Vector2(.5f,.282f));
  var rig=Import("Assets/Art/MineRig.png",new Vector2(.5f,.075f));
  var ship=Import("Assets/Art/WreckGrounded-v2.png",new Vector2(.5f,.229f));
  var actor=Import("Assets/Art/Astronaut.png",new Vector2(.5f,.074f));
  var director=Group("Scene Director").gameObject.AddComponent<SceneDirector>();director.pixel=pixel;director.glow=glow;director.flame=flame;
  var clearCamera=Group("Letterbox Camera").gameObject.AddComponent<Camera>();clearCamera.clearFlags=CameraClearFlags.SolidColor;clearCamera.backgroundColor=Color.black;clearCamera.cullingMask=0;clearCamera.depth=-2;
  var cam=Group("Main Camera").gameObject.AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=9;cam.transform.position=new Vector3(0,0,-20);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=C("040915");cam.depth=0;director.worldCamera=cam;
  var sky=Group("01 · Cosmos");var far=Group("02 · Distant landscape");var surface=Group("03 · Surface");var sites=Group("04 · Interactive sites");var under=Group("05 · Underground");var effects=Group("06 · Atmosphere and fire");director.atmosphere=effects;
  var nebula=GameObject.CreatePrimitive(PrimitiveType.Quad);nebula.name="Flowing Milky Way";nebula.transform.SetParent(sky);nebula.transform.position=new Vector3(0,4.5f,4);nebula.transform.localScale=new Vector3(38,23,1);UnityEngine.Object.DestroyImmediate(nebula.GetComponent<Collider>());
  var nm=new Material(Shader.Find("Starfall/LivingNebula"));AssetDatabase.CreateAsset(nm,"Assets/Art/Generated/Nebula.mat");nebula.GetComponent<MeshRenderer>().sharedMaterial=nm;nebula.GetComponent<MeshRenderer>().sortingOrder=-100;
  Image("Moon halo",sky,glow,-4.5f,4.7f,7.0f,-96,new Color(.28f,.59f,.64f,.22f));Image("Distant moon",sky,moon,-4.5f,4.7f,3.8f,-94);
  // A thin orbital ring is a separate editable renderer behind the moon.
  var ringPts=new Vector3[97];for(int i=0;i<97;i++){float a=i/96f*Mathf.PI*2;Vector2 p=new Vector2(Mathf.Cos(a)*3.6f,Mathf.Sin(a)*.50f);p=Quaternion.Euler(0,0,18)*p;ringPts[i]=new Vector3(p.x-4.5f,p.y+4.7f,0);}
  Line("Orbital dust ring",sky,ringPts,.025f,new Color(.38f,.55f,.6f,.40f),-95);
  for(int layer=0;layer<3;layer++){
   // Each mountain is a closed, editable mesh, with faceted lighting.
   for(int i=0;i<13;i++){
    float x=-19+i*3.3f,w=R(3.2f,6),height=R(.45f,layer==0?2.8f:1.5f),baseline=-3.65f+layer*.12f;
    float tip=x+w*.5f;
    Polygon("Ridge_"+layer+"_"+i,far,new[]{new Vector2(x,baseline),new Vector2(x+w*.28f,baseline+height*.45f),new Vector2(tip,baseline+height),new Vector2(x+w*.76f,baseline+height*.39f),new Vector2(x+w,baseline)},C(layer==0?"1c3443":layer==1?"233641":"303a40"),-60+layer*3);
    Polygon("RidgeFace_"+layer+"_"+i,far,new[]{new Vector2(tip,baseline+height),new Vector2(x+w*.76f,baseline+height*.39f),new Vector2(x+w,baseline),new Vector2(tip-.2f,baseline)},C(layer==0?"152c3c":layer==1?"1c2d38":"273038"),-59+layer*3);
   }
  }
  // Surface has thickness; the walking line stays y=-3.8.
  Rect("Ground top plane",surface,0,-3.77f,36,.17f,"9e8e6b",-10);
  Rect("Ground rim shadow",surface,0,-4.03f,36,.11f,"292b2a",-8);
  for(int i=0;i<210;i++){
   float x=R(-18,18),y=R(-4.20f,-3.76f);if(Mathf.Abs(x)<1)continue;
   Image("Surface stone "+i,surface,rock,x,y,R(.08f,.55f),-7,C(i%3==0?"ada491":"7c7b72"));
  }
  for(int i=0;i<24;i++){float x=R(-17,17);if(Mathf.Abs(x)<2.5f)continue;Image("Loose basalt "+i,surface,rock,x,-3.55f,R(.30f,.94f),1,C("89949a"));}
  director.camp=Site("Camp",SiteKind.Camp,sites,tent,-9.3f,-3.77f,6.1f,10);
  director.mine=Site("Mine gantry",SiteKind.Mine,sites,rig,0,-3.82f,4.7f,15);director.mine.radius=2.6f;
  director.wreck=Site("Burning wreck",SiteKind.Wreck,sites,ship,9.35f,-3.84f,5.0f,12);director.wreck.radius=2.6f;
  Image("Camp light spill",effects,glow,-10.6f,-3.0f,4,9,new Color(1,.60f,.18f,.23f));

  // Standalone camp crates, antenna and path lamps, ready for future interactions.
  for(int i=0;i<3;i++){
   float x=-5.5f+i*.38f;Rect("Supply crate "+i,sites,x,-3.51f,.33f,.48f,"283944",13);Rect("Crate lid "+i,sites,x,-3.24f,.37f,.07f,"82909a",14);Rect("Crate label "+i,sites,x,-3.47f,.12f,.05f,"b1ab80",14);
  }
  Rect("Antenna mast",sites,-12.8f,-2.3f,.06f,2.9f,"576b78",8);
  Line("Antenna dish",sites,new[]{new Vector3(-13.12f,-.87f),new Vector3(-12.8f,-1.0f),new Vector3(-12.5f,-.80f)},.07f,C("a1b8bb"),8);
  for(int i=0;i<7;i++){
   float x=-14+i*4.7f;if(Mathf.Abs(x)<2.5f)continue;
   Rect("Path beacon base "+i,sites,x,-3.50f,.12f,.58f,"47535b",11);Rect("Path beacon light "+i,sites,x,-3.19f,.13f,.12f,"76e1da",12);Image("Beacon aura "+i,effects,glow,x,-3.16f,1.3f,11,new Color(.32f,.87f,.85f,.42f));
  }
  var player=Group("Astronaut · horizontal controller",sites);player.position=new Vector3(-3.25f,-3.8f,0);director.player=player;
  var ps=Image("Character sprite",player,actor,-3.25f,-3.8f,1.25f,35);director.playerVisual=ps.transform;director.playerSprite=ps;
  Image("Foot contact shadow",player,glow,-3.25f,-3.82f,1,34,new Color(0,0,0,.8f)).transform.localScale=new Vector3(1,.18f,1);
  var motor=Group("Winch motor",director.mine.transform);motor.position=new Vector3(0,-1.36f,0);director.winchAnchor=motor;
  var hook=Group("Grappling hook",director.mine.transform);hook.position=new Vector3(0,-2.46f,0);director.hook=hook;
  Rect("Hook head",hook,0,-2.46f,.17f,.20f,"bcaa7c",32);
  var jaw=Line("Claw left",hook,new[]{new Vector3(-.08f,-2.48f),new Vector3(-.24f,-2.65f),new Vector3(-.20f,-2.86f),new Vector3(-.05f,-2.92f)},.065f,C("aab9b9"),32);jaw.useWorldSpace=false;jaw.transform.localPosition=Vector3.zero;for(int i=0;i<jaw.positionCount;i++)jaw.SetPosition(i,jaw.GetPosition(i)-hook.position);
  jaw=Line("Claw right",hook,new[]{new Vector3(.08f,-2.48f),new Vector3(.24f,-2.65f),new Vector3(.20f,-2.86f),new Vector3(.05f,-2.92f)},.065f,C("788e99"),32);jaw.useWorldSpace=false;jaw.transform.localPosition=Vector3.zero;for(int i=0;i<jaw.positionCount;i++)jaw.SetPosition(i,jaw.GetPosition(i)-hook.position);
  director.cable=Line("Winch cable",director.mine.transform,new[]{motor.position,hook.position},.032f,C("94b0b5"),31);
  UndergroundRework.Populate(director,under,pixel,glow);
  VisualRework.Populate(director,sky,pixel,glow,rock);
  ForgeBuilder.Configure(director);
  foreach(var site in new[]{director.camp,director.mine,director.wreck})PrefabUtility.SaveAsPrefabAssetAndConnect(site.gameObject,"Assets/Prefabs/"+site.kind+".prefab",InteractionMode.AutomatedAction);
  PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject,"Assets/Prefabs/Astronaut.prefab",InteractionMode.AutomatedAction);
  foreach(var ore in director.ores)PrefabUtility.SaveAsPrefabAssetAndConnect(ore.gameObject,"Assets/Prefabs/"+ore.gameObject.name+".prefab",InteractionMode.AutomatedAction);
  const string path="Assets/Scenes/Scene01_Starfall.unity";
  EditorSceneManager.SaveScene(scene,path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
  Debug.Log("STARFALL_SCENE_CREATED");
 }
 [MenuItem("Starfall/Build Windows")]
 public static void Build(){
  Directory.CreateDirectory("Builds/Windows");
  var scenes=File.Exists("Assets/Scenes/Scene02_Encounter.unity")?new[]{"Assets/Scenes/Scene01_Starfall.unity","Assets/Scenes/Scene02_Encounter.unity"}:new[]{"Assets/Scenes/Scene01_Starfall.unity"};
  var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName="Builds/Windows/Starfall.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(r.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+r.summary.result);
  File.WriteAllText("QA/build-result.txt","PASS / Unity "+Application.unityVersion+" / Windows x64 / "+DateTime.Now.ToString("s"));Debug.Log("STARFALL_BUILD_PASS");
 }
 public static void CreateAndBuild(){Create();Build();}
}
