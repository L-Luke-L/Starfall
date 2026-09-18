using UnityEngine;

namespace Starfall {
 // Procedural sprites remain separate scene objects, with deterministic source art.
 public static class WorldArt {
  public static Color Hex(string h){ColorUtility.TryParseHtmlString("#"+h,out var c);return c;}
  public static Texture2D Texture(int w,int h,System.Func<int,int,Color> sample){
   var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var p=new Color[w*h];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)p[y*w+x]=sample(x,y);
   t.SetPixels(p);t.Apply();t.filterMode=FilterMode.Point;return t;
  }
  public static Texture2D Terrain(){
   return Texture(1280,760,(x,y)=>{
    float depth=(759-y)/38f;float n=Mathf.PerlinNoise(x*.075f,y*.13f),fine=Mathf.PerlinNoise(x*.47f,y*.47f);
    var c=Color.Lerp(Hex("151722"),Hex("64534b"),Mathf.Exp(-depth*.25f));
    float strata=Mathf.Sin(depth*6.5f+Mathf.PerlinNoise(x*.009f,depth*.2f)*3);
    c*=.67f+n*.48f+fine*.26f+(strata>.8f?.13f:0);
    if(depth<.18f)c=Color.Lerp(Hex("c2ac7b"),Hex("857154"),n);
    else if(depth<.50f)c=Color.Lerp(Hex("84734f"),Hex("514839"),n);
    // Cavities in the geological cross-section, not a traversable lower floor.
    float cavern=Mathf.PerlinNoise(x*.005f+9,y*.008f);
    if(depth>1.5f&&cavern>.60f)c=Color.Lerp(c,Hex("080e18"),Mathf.Clamp01((cavern-.60f)*23));
    if(fine>.77f)c*=1.3f;
    return c;
   });
  }
  public static Texture2D Moon(){
   return Texture(256,256,(x,y)=>{
    float u=(x-127.5f)/126,v=(y-127.5f)/126,r=u*u+v*v;if(r>1)return Color.clear;
    float noise=Mathf.PerlinNoise(x*.035f,y*.035f)*.7f+Mathf.PerlinNoise(x*.18f,y*.18f)*.3f;
    float light=Mathf.Clamp01(-u*.55f+v*.25f+Mathf.Sqrt(1-r)*.65f);
    var c=Color.Lerp(Hex("10202f"),Hex("90aaa6"),light*(.5f+noise*.5f));
    if(r>.96)c=Color.Lerp(c,Hex("94c4bf"),light*.65f);return c;
   });
  }
  public static Texture2D Glow(){return Texture(64,64,(x,y)=>{float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32;return new Color(1,1,1,Mathf.Pow(Mathf.Max(0,1-r),2)*.42f);});}
  public static Texture2D Flame(){return Texture(20,40,(x,y)=>{
   float v=y/39f,u=(x-10)/10f;float center=Mathf.Sin(v*10)*v*.22f;
   float width=(1-v)*(.8f+Mathf.Sin(v*17)*.15f);
   if(Mathf.Abs(u-center)>width)return Color.clear;
   var c=Color.Lerp(Hex("ffe89a"),Hex("e45119"),Mathf.Clamp01(v*.9f+Mathf.Abs(u)));
   c.a=Mathf.Clamp01((1-v)*1.8f);return c;
  });}
  public static Texture2D Grain(){return Texture(128,128,(x,y)=>{
   float v=.64f+Mathf.PerlinNoise(x*.16f,y*.08f)*.45f+(Mathf.PerlinNoise(x*.71f,y*.71f)-.5f)*.19f;
   if(Mathf.Sin(y*.4f+Mathf.PerlinNoise(x*.03f,1)*4)>.92f)v*=.84f;
   return new Color(v,v,v,1);
  });}
  public static Texture2D Rock(){return Texture(64,40,(x,y)=>{
   float u=(x-32)/32f,v=y/40f;float top=.65f+Mathf.PerlinNoise(x*.09f,2)*.30f;
   if(v>top||Mathf.Abs(u)>.94f-v*.2f)return Color.clear;
   var c=Color.Lerp(Hex("3c3a40"),Hex("968979"),v*.70f+Mathf.PerlinNoise(x*.14f,y*.14f)*.30f);
   if(v>top-.07f)c*=1.24f;if(x%17<2)c*=.75f;return c;
  });}
  public static Texture2D Crystal(){return Texture(40,48,(x,y)=>{
   float u=Mathf.Abs(x-20)/20f,v=y/48f;if(u>.8f-Mathf.Abs(v-.50f)*.85f)return Color.clear;
   if(x<17)return Color.Lerp(Hex("276676"),Hex("94f6dd"),v);
   return Color.Lerp(Hex("123a55"),Hex("4ac5cc"),v);
  });}
 }
}
