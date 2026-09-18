using UnityEngine;

namespace Starfall {
 public sealed partial class ForgeController {
  public bool RewardArtAvailable=>ingotSheet;
  public static Rect IngotUV(RepairResource metal){int i=Mathf.Clamp((int)metal,0,3);return new Rect((i%2)*.5f,i<2?.5f:0,.5f,.5f);}
  void DrawReward(){
   float appear=Mathf.SmoothStep(0,1,ResultAge/.42f);
   Fill(new Rect(0,0,1600,900),new Color(.012f,.025f,.035f,appear*.34f));
   if(!rewardGlow){rewardGlow=new Texture2D(128,128,TextureFormat.RGBA32,false);var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;pixels[y*128+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}rewardGlow.SetPixels(pixels);rewardGlow.Apply();rewardGlow.wrapMode=TextureWrapMode.Clamp;}
   var tint=OreInventory.MetalColor(ResultMetal);float pulse=1+Mathf.Sin(ResultAge*2.4f)*.035f;
   GUI.color=new Color(tint.r,tint.g,tint.b,appear*.92f);GUI.DrawTexture(new Rect(800-340*pulse,440-340*pulse,680*pulse,680*pulse),rewardGlow);GUI.color=Color.white;
   for(int i=0;i<16;i++){float a=i*Mathf.PI/8+ResultAge*.055f;var n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Stroke(new Vector2(800,440)+n*(165+i%3*7),new Vector2(800,440)+n*(220+i%4*17),1.3f,new Color(tint.r,tint.g,tint.b,appear*.18f));}
   float size=470*Mathf.Lerp(.65f,1,appear),rise=Mathf.Sin(ResultAge*1.8f)*5;
   GUI.color=new Color(1,1,1,appear);if(ingotSheet)GUI.DrawTextureWithTexCoords(new Rect(800-size/2,433-size/2+rise,size,size),ingotSheet,IngotUV(ResultMetal));GUI.color=Color.white;
   for(int i=0;i<12;i++){float phase=ResultAge*(.16f+i*.009f)+i*.618f;float t=phase%1;Vector2 p=new Vector2(800+Mathf.Sin(i*7.3f)*230,570-t*280);float alpha=Mathf.Sin(t*Mathf.PI)*appear*.65f;Stroke(p-Vector2.right*3,p+Vector2.right*3,1,new Color(tint.r,tint.g,tint.b,alpha));Stroke(p-Vector2.up*3,p+Vector2.up*3,1,new Color(tint.r,tint.g,tint.b,alpha));}

   Label(new Rect(480,629,640,55),OreInventory.MetalName(ResultMetal)+" ×1",34,new Color(tint.r,tint.g,tint.b,appear),true,TextAnchor.MiddleCenter);
  }
 }
}
