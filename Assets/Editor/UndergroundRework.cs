using System.IO;
using UnityEditor;
using UnityEngine;
using Starfall;

public static class UndergroundRework {
 static Sprite pixel,glow,wall;
 static Material plain;
 static Color C(string h)=>WorldArt.Hex(h);
 static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 static Sprite Import(string file){
  string path="Assets/Art/Underground/"+file+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;ti.alphaIsTransparency=true;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;
  var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static SpriteRenderer Image(string name,Transform root,Sprite art,float x,float y,float w,float h,int order,Color color){
  var t=Group(name,root);t.localPosition=new Vector3(x,y,0);t.localScale=new Vector3(w/art.bounds.size.x,h/art.bounds.size.y,1);var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=art;sr.sortingOrder=order;sr.color=color;return sr;
 }
 static void Rect(string name,Transform root,float x,float y,float w,float h,int order,string color){Image(name,root,pixel,x,y,w,h,order,C(color));}
 static void Line(string name,Transform root,Vector3[] pts,float width,int order,Color color){
  var l=Group(name,root).gameObject.AddComponent<LineRenderer>();l.sharedMaterial=plain;l.useWorldSpace=false;l.positionCount=pts.Length;l.SetPositions(pts);l.startWidth=l.endWidth=width;l.startColor=l.endColor=color;l.sortingOrder=order;
 }
 static void Band(string name,Transform root,float[] xs,float[] upper,float[] lower,int order,Color color){
  var mesh=new Mesh();var vertices=new Vector3[xs.Length*2];var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];var tris=new int[(xs.Length-1)*6];
  for(int i=0;i<xs.Length;i++){vertices[i*2]=new Vector3(xs[i],upper[i],0);vertices[i*2+1]=new Vector3(xs[i],lower[i],0);colors[i*2]=colors[i*2+1]=color;uv[i*2]=new Vector2(xs[i],upper[i]);uv[i*2+1]=new Vector2(xs[i],lower[i]);if(i==xs.Length-1)continue;int j=i*6,k=i*2;tris[j]=k;tris[j+1]=k+1;tris[j+2]=k+2;tris[j+3]=k+1;tris[j+4]=k+3;tris[j+5]=k+2;}
  mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/Art/Generated/UG_"+name.Replace(' ','_')+".asset");
  var t=Group(name,root);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=t.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=plain;mr.sortingOrder=order;
 }
 static SpriteRenderer Shelf(string name,Transform root,float x,float y,float w,float h,int order,Color color,bool flip=false){var sr=Image(name,root,wall,x,y,w,h,order,color);sr.flipX=flip;return sr;}
 public static void Populate(SceneDirector director,Transform underground,Sprite unit,Sprite softGlow){
  // Replace all legacy underground geometry and placeholders. Surface objects are preserved.
  while(underground.childCount>0)Object.DestroyImmediate(underground.GetChild(0).gameObject);
  director.ores.Clear();pixel=unit;glow=softGlow;plain=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/UnlitSprites.mat");
  Directory.CreateDirectory("Assets/Content/BuriedSamples");Directory.CreateDirectory("Assets/Prefabs/Underground");
  wall=Import("RockShelf");Sprite[] shells={Import("FerricNodule"),Import("AmberNodule"),Import("BlueNodule"),Import("VeiledGeode")};
  var embeddedMaterial=new Material(Shader.Find("Starfall/EmbeddedSample"));AssetDatabase.CreateAsset(embeddedMaterial,"Assets/Art/Generated/EmbeddedMineral.mat");
  var geology=Group("01 · Continuous geological layers",underground);var walls=Group("02 · Fractured basalt banks",underground);var shaft=Group("03 · Shaft throat and support",underground);var sockets=Group("04 · Mineral-bearing rock pockets",underground);var samples=Group("05 · Recoverable mineral shells",underground);var atmosphere=Group("06 · Subsurface atmosphere",underground);
  var back=GameObject.CreatePrimitive(PrimitiveType.Quad);back.name="Continuous stratified bedrock";back.transform.SetParent(geology,false);back.transform.localPosition=new Vector3(0,-12,0);back.transform.localScale=new Vector3(36,16,1);Object.DestroyImmediate(back.GetComponent<Collider>());
  const string wallPath="Assets/Art/Underground/BasaltWall.png";AssetDatabase.ImportAsset(wallPath,ImportAssetOptions.ForceSynchronousImport);var wallImporter=(TextureImporter)AssetImporter.GetAtPath(wallPath);wallImporter.textureType=TextureImporterType.Default;wallImporter.wrapMode=TextureWrapMode.Repeat;wallImporter.filterMode=FilterMode.Bilinear;wallImporter.mipmapEnabled=true;wallImporter.textureCompression=TextureImporterCompression.Uncompressed;wallImporter.SaveAndReimport();
  var mat=new Material(Shader.Find("Starfall/StratifiedRock"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(wallPath);AssetDatabase.CreateAsset(mat,"Assets/Art/Generated/UndergroundBedrock.mat");back.GetComponent<MeshRenderer>().sharedMaterial=mat;back.GetComponent<MeshRenderer>().sortingOrder=-40;
  float[] xs={-18,-15,-12,-9,-6,-3,0,3,6,9,12,15,18};float[] top=new float[xs.Length],bottom=new float[xs.Length];
  for(int i=0;i<xs.Length;i++){top[i]=-4;bottom[i]=-4.58f-Mathf.Sin(i*1.8f)*.12f;}
  Band("Weathered surface crust",geology,xs,top,bottom,-34,C("454337"));
  var edge=new Vector3[xs.Length];for(int i=0;i<xs.Length;i++)edge[i]=new Vector3(xs[i],bottom[i],0);Line("Crust lower fracture",geology,edge,.035f,-33,C("252e2e"));
  float[] roofX={-14.5f,-9.1f,-3.85f,3.85f,9.1f,14.5f};
  for(int i=0;i<roofX.Length;i++)Shelf("Shallow rock shelf "+i,walls,roofX[i],-5.03f,6.25f,1.82f,-25,new Color(.68f,.64f,.53f),i%2==0);
  Shelf("Left upper basalt buttress",walls,-13.6f,-8.0f,10,5.4f,-24,new Color(.42f,.52f,.56f));
  Shelf("Right upper basalt buttress",walls,13.8f,-8.2f,10.2f,5.6f,-24,new Color(.42f,.51f,.56f),true);
  Shelf("Left deep rock bank",walls,-13.9f,-12.1f,11.1f,6.2f,-23,new Color(.34f,.45f,.51f),true);
  Shelf("Right deep rock bank",walls,13.9f,-12.5f,11.4f,6.3f,-23,new Color(.33f,.44f,.51f));
  Shelf("Low foreland left",walls,-7.3f,-16.65f,12.5f,3.9f,-8,new Color(.27f,.36f,.41f));
  Shelf("Low foreland right",walls,6.9f,-17.05f,13.8f,4.2f,-8,new Color(.27f,.36f,.41f),true);
  // The central mining field is continuous bedrock. No floating platform silhouettes.
  // The narrow man-made throat ends at the rock seam; there is no giant flat triangular void.
  Rect("Shaft throat shadow",shaft,0,-4.83f,1.66f,2.02f,-16,"111e22");
  for(int side=-1;side<=1;side+=2){
   Rect("Shaft vertical steel "+side,shaft,side*.87f,-4.84f,.12f,2.10f,-13,"465352");
   Rect("Shaft inward edge "+side,shaft,side*.80f,-4.84f,.025f,2.10f,-12,"737c6d");
   for(int i=0;i<4;i++){Rect("Anchor foot "+side+" "+i,shaft,side*.92f,-4.22f-i*.45f,.23f,.085f,-11,"5c6660");}
  }
  Line("Ladder rail left",shaft,new[]{new Vector3(-.65f,-4.12f),new Vector3(-.65f,-5.68f)},.036f,-10,C("697c79"));
  Line("Ladder rail right",shaft,new[]{new Vector3(-.37f,-4.12f),new Vector3(-.37f,-5.68f)},.036f,-10,C("697c79"));
  for(int i=0;i<6;i++)Rect("Ladder rung "+i,shaft,-.51f,-4.22f-i*.26f,.3f,.033f,-9,"7f8f88");
  Image("Daylight fading down throat",atmosphere,glow,0,-5.18f,3.4f,3.8f,-15,new Color(.36f,.56f,.54f,.15f));
  Image("Soft reflected cave light",atmosphere,glow,0,-9.0f,17,11,-29,new Color(.13f,.34f,.38f,.105f));
  // Outer shell choice deliberately does not identify the hidden payload kind.
  Vector2[] positions={new Vector2(0,-6.55f),new Vector2(-1.65f,-7.65f),new Vector2(-3.05f,-8.70f),new Vector2(3.0f,-8.80f),new Vector2(-.70f,-9.60f),new Vector2(1.65f,-10.50f),new Vector2(-4.6f,-11.30f),new Vector2(4.8f,-11.50f),new Vector2(-2.25f,-12.20f),new Vector2(.35f,-12.85f),new Vector2(3.20f,-13.20f),new Vector2(-4.55f,-13.25f)};
  int[] exterior={0,2,0,1,0,3,2,1,2,3,0,1};
  float[] widths={1.08f,.98f,1.15f,1.10f,1.01f,1.14f,1.20f,1.23f,1.09f,1.21f,1.23f,1.24f};
  for(int i=0;i<positions.Length;i++){
   var pos=positions[i];int visual=exterior[i];var definition=ScriptableObject.CreateInstance<SubsurfaceDefinition>();definition.contentId="subsurface.sample."+i.ToString("D2");
   definition.payloadKind=(i==2||i==8)?BuriedPayloadKind.DormantEncounter:(i==5||i==11)?BuriedPayloadKind.StoryEvent:BuriedPayloadKind.RepairMaterial;
   definition.repairResource=visual==0?RepairResource.FerricAlloy:visual==1?RepairResource.CopperCompound:visual==2?RepairResource.CobaltSilicate:RepairResource.RareComposite;
   if(definition.payloadKind==BuriedPayloadKind.DormantEncounter){definition.encounterId="reserved.encounter."+i.ToString("D2");definition.fuelRewardId="reserved.fuel.cell";}
   if(definition.payloadKind==BuriedPayloadKind.StoryEvent)definition.storyEventId="reserved.story."+i.ToString("D2");
   AssetDatabase.CreateAsset(definition,"Assets/Content/BuriedSamples/Sample"+i.ToString("D2")+".asset");
   var pocket=Group("Host rock pocket "+i.ToString("D2"),sockets);pocket.localPosition=pos;
   Image("Recess contact shadow",pocket,glow,0,-.10f,widths[i]*1.35f,widths[i]*.70f,-7,new Color(.015f,.025f,.030f,.58f));
   var obj=Group("Buried sample "+i.ToString("D2"),samples);obj.localPosition=pos;
   float ratio=shells[visual].rect.height/shells[visual].rect.width;
   var sr=Image("Mineral shell",obj,shells[visual],0,0,widths[i],widths[i]*ratio,5,new Color(.84f,.88f,.91f));sr.flipX=i%3==1;sr.transform.localRotation=Quaternion.Euler(0,0,(i%3-1)*9);
   var target=obj.gameObject.AddComponent<Interactable>();target.kind=SiteKind.Ore;target.displayName="矿物样本";target.description="Unidentified mineral shell. Hidden content is configured separately; integrations are currently unbound.";target.radius=widths[i]*.36f;
   var collider=obj.gameObject.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=target.radius;
   sr.sharedMaterial=embeddedMaterial;
   var sample=obj.gameObject.AddComponent<BuriedSample>();sample.mineralShell=sr;sample.looseMaterial=plain;sample.Configure(definition,(MineralShell)visual);director.ores.Add(target);
   // A short host vein remains behind when its specimen is recovered.
   float sign=i%2==0?-1:1;
   Line("Fine host seam "+i.ToString("D2"),pocket,new[]{new Vector3(sign*.34f,-.23f),new Vector3(sign*.63f,-.26f),new Vector3(sign*.78f,-.15f),new Vector3(sign*1.02f,-.23f)},.018f,-6,new Color(.34f,.43f,.43f,.27f));
  }
  if(!director.GetComponent<UndergroundVerification>())director.gameObject.AddComponent<UndergroundVerification>();
 }
}
