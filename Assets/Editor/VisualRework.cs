using System.IO;
using UnityEditor;
using UnityEngine;
using Starfall;

// All atmosphere and wreck parts are authored in the scene/prefab, not spawned as a flattened background.
public static class VisualRework {
 static System.Random rng;
 static Material spriteMaterial;
 static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
 static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 static SpriteRenderer Sprite(string name,Transform parent,Sprite art,Vector2 position,Vector2 size,int order,Color color){
  var t=Group(name,parent);t.localPosition=position;t.localScale=new Vector3(size.x/art.bounds.size.x,size.y/art.bounds.size.y,1);
  var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=art;sr.sortingOrder=order;sr.color=color;return sr;
 }
 static Sprite Save(string name,Texture2D texture){
  string path="Assets/Art/Generated/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spritePixelsPerUnit=128;ti.alphaIsTransparency=true;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static Material Mat(string shader,string name){var m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,"Assets/Art/Generated/"+name+".mat");return m;}
 static MeshRenderer Quad(string name,Transform parent,Vector2 pos,Vector2 size,Material mat,int order){
  var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=new Vector3(size.x,size.y,1);Object.DestroyImmediate(g.GetComponent<Collider>());var mr=g.GetComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.sortingOrder=order;return mr;
 }
 static LineRenderer Trail(string name,Transform root,float width,int order){
  var l=Group(name,root).gameObject.AddComponent<LineRenderer>();l.sharedMaterial=spriteMaterial;l.useWorldSpace=true;l.positionCount=12;l.startWidth=width;l.endWidth=.001f;l.sortingOrder=order;l.enabled=false;return l;
 }
 public static void Populate(SceneDirector director,Transform sky,Sprite pixel,Sprite glow,Sprite rock){
  rng=new System.Random(2751);spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/UnlitSprites.mat");
  var galaxy=Group("Milky Way · independent moving layers",sky);
  Color[] colors={new Color(.27f,.31f,.49f,.71f),new Color(.15f,.37f,.41f,.63f),new Color(.33f,.25f,.40f,.52f)};
  float[] speeds={.021f,-.013f,.033f};
  for(int i=0;i<3;i++){
   var m=Mat("Starfall/NebulaStrand","GalaxyLayer"+i);m.SetColor("_Tint",colors[i]);m.SetFloat("_Layer",i);m.SetFloat("_Drift",speeds[i]);
   var mr=Quad("Dust strand "+(i+1),galaxy,new Vector2(0,3.2f),new Vector2(36,16),m,-99+i%2);mr.gameObject.AddComponent<NebulaLayer>().driftSpeed=speeds[i];
  }
  var stars=Group("Stars · individual twinkle and matching halo",sky);
  for(int i=0;i<610;i++){
   var t=Group("Star "+i,stars);t.localPosition=new Vector3(R(-17.5f,17.5f),R(-2,9.5f),0);
   float size=i%41==0?R(.025f,.041f):R(.011f,.023f);Color tint=i%7==0?new Color(.90f,.77f,.57f):new Color(.59f,.77f,.91f);
   var star=t.gameObject.AddComponent<TwinkleStar>();star.phase=R(0,6.28f);star.frequency=R(.65f,2.4f);star.brightness=R(.32f,.91f);
   star.core=Sprite("Stellar core",t,pixel,Vector2.zero,Vector2.one*size,-97,tint);
   star.halo=Sprite("Soft optical halo",t,glow,Vector2.zero,Vector2.one*size*5,-97,tint);
  }
  var meteors=Group("Shooting stars · tapered trails",sky);
  for(int i=0;i<2;i++){
   var t=Group("Meteor "+(i+1),meteors);var meteor=t.gameObject.AddComponent<ShootingStar>();meteor.firstDelay=.7f+i*2.7f;meteor.interval=5.5f;meteor.seed=345+i*89;
   meteor.trail=Trail("Fading ion trail",t,.017f,-92);meteor.bloom=Trail("Trail bloom",t,.058f,-93);
   meteor.head=Sprite("Meteor head",t,pixel,Vector2.zero,Vector2.one*.031f,-91,Color.white);
   meteor.halo=Sprite("Head bloom",t,glow,Vector2.zero,Vector2.one*.22f,-92,Color.white);meteor.head.enabled=meteor.halo.enabled=false;
  }
  // Mottled translucent smoke; no opaque cloud disks or repeated flame stamps.
  var smoke=Save("SmokeSoft",WorldArt.Texture(128,128,(x,y)=>{
   float u=(x+.5f)/64-1,v=(y+.5f)/64-1,r=Mathf.Sqrt(u*u+v*v);
   float n=Mathf.PerlinNoise(x*.037f+32,y*.037f+81)*.65f+Mathf.PerlinNoise(x*.10f+8,y*.10f+5)*.35f;
   return new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),1.4f)*Mathf.Clamp01(n*1.6f-.2f));
  }));
  var scar=Save("ScorchedContact",WorldArt.Texture(256,64,(x,y)=>{
   float u=(x+.5f)/128-1,v=(y+.5f)/32-1;float n=Mathf.PerlinNoise(x*.075f,y*.14f);
   float alpha=Mathf.Clamp01(1-u*u-v*v)*Mathf.Clamp01(n*1.5f-.1f);return new Color(.08f,.069f,.058f,alpha*.9f);
  }));
  var wreck=director.wreck.transform;
  Sprite("Scorched crash bed",wreck,scar,new Vector2(0,-.025f),new Vector2(5.8f,.30f),6,Color.white);
  Sprite("Belly contact occlusion",wreck,glow,new Vector2(.10f,.016f),new Vector2(5.05f,.15f),8,new Color(.02f,.015f,.01f,.94f));
  Sprite("Displaced dark soil",wreck,scar,new Vector2(1.6f,-.025f),new Vector2(2.0f,.23f),17,new Color(.63f,.60f,.54f,.72f));
  var hull=wreck.Find("Artwork").GetComponent<SpriteRenderer>();
  var fireOrigin=Group("Engine breach · attached combustion",wreck);fireOrigin.localPosition=new Vector3(-1.975f,.59f,0);
  var fire=fireOrigin.gameObject.AddComponent<WreckFire>();fire.seed=3.12f;
  var combustion=Mat("Starfall/AnchoredCombustion","EngineCombustion");
  fire.flame=Quad("Central flame tongue",fireOrigin,new Vector2(.015f,.65f),new Vector2(.43f,1.30f),combustion,14);
  fire.secondaryFlames=new[]{Quad("Left flame tongue",fireOrigin,new Vector2(-.19f,.48f),new Vector2(.37f,.96f),combustion,14),Quad("Right flame tongue",fireOrigin,new Vector2(.19f,.44f),new Vector2(.33f,.83f),combustion,14)};
  fire.heatCore=Sprite("Heat in engine cavity",fireOrigin,glow,new Vector2(0,.05f),new Vector2(.64f,.51f),13,new Color(1,.27f,.04f,.5f));
  // Ground illumination stays on the ground while smoke and flames rise from the breach.
  fire.groundLight=Sprite("Fire reflection on crash bed",wreck,glow,new Vector2(-1.7f,.035f),new Vector2(2.25f,.24f),10,new Color(1,.3f,.07f,.3f));
  fire.smoke=new SpriteRenderer[19];fire.embers=new SpriteRenderer[13];
  for(int i=0;i<fire.smoke.Length;i++)fire.smoke[i]=Sprite("Smoke wisp "+i,fireOrigin,smoke,Vector2.zero,Vector2.one,11,new Color(.16f,.17f,.19f,0));
  for(int i=0;i<fire.embers.Length;i++)fire.embers[i]=Sprite("Ember "+i,fireOrigin,pixel,Vector2.zero,Vector2.one*.014f,18,new Color(1,.5f,.1f,0));
  var rim=Object.Instantiate(hull.gameObject,wreck).GetComponent<SpriteRenderer>();rim.name="Hull rim · occludes embedded flame";rim.sortingOrder=15;rim.sharedMaterial=Mat("Starfall/BreachRim","BreachOcclusion");
  var reflection=Object.Instantiate(hull.gameObject,wreck).GetComponent<SpriteRenderer>();reflection.name="Heat reflected on torn metal";reflection.sortingOrder=16;reflection.sharedMaterial=Mat("Starfall/HullReflection","HullHeatReflection");
  for(int i=0;i<29;i++){
   float x=R(-2.6f,2.7f);float size=R(.055f,.20f);
   var sr=Sprite("Contact gravel "+i,wreck,rock,new Vector2(x,R(-.018f,.022f)),new Vector2(size,size*.52f),19,new Color(.47f,.43f,.35f));sr.transform.localRotation=Quaternion.Euler(0,0,R(-14,14));
  }
  for(int i=0;i<9;i++){
   float x=i<4?R(-3.3f,-2.55f):R(2.5f,3.1f),w=R(.07f,.22f);
   var shard=Group("Separated metal fragment "+i,wreck);shard.localPosition=new Vector3(x,R(.018f,.07f),0);shard.localRotation=Quaternion.Euler(0,0,R(-20,20));
   Sprite("Broken panel",shard,pixel,Vector2.zero,new Vector2(w,.055f),13,new Color(.22f,.25f,.25f));Sprite("Edge highlight",shard,pixel,new Vector2(0,.025f),new Vector2(w,.012f),14,new Color(.48f,.49f,.44f));
  }
  Sprite("Camp ground contact",director.camp.transform,glow,new Vector2(0,-.01f),new Vector2(5.4f,.18f),7,new Color(0,0,0,.65f));
  Sprite("Mine ground contact",director.mine.transform,glow,new Vector2(0,-.025f),new Vector2(4.5f,.17f),7,new Color(0,0,0,.72f));
  director.gameObject.AddComponent<VisualVerification>();
 }
}
