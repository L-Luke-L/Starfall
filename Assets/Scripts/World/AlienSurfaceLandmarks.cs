using UnityEngine;
namespace Starfall {
 public sealed class AlienSurfaceLandmarks:MonoBehaviour {
  public Transform arrival,returnBeacon;
  public Transform[] futureEnemySpawns;
  public Collider ground,collisionTestRock;
  public static float Height(float x,float z){
   float baseHeight=(Mathf.PerlinNoise(x*.026f+39,z*.026f+24)-.5f)*2.6f+(Mathf.PerlinNoise(x*.12f+71,z*.12f+56)-.5f)*.34f;
   float r=new Vector2(x-3,z-9).magnitude;
   float crater=-2.2f*Mathf.Exp(-r*r/100)+1.65f*Mathf.Exp(-Mathf.Pow((r-14)/3.2f,2));
   float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((new Vector2(x,z).magnitude-66)/39))*9;
   float flatten=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((new Vector2(x,z+38).magnitude-7)/7));
   return Mathf.Lerp(baseHeight+crater+edge,0,flatten);
  }
 }
}
