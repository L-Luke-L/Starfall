using UnityEngine;
namespace Starfall {
 // One scene object, one phase: selecting/moving a star never affects its neighbours.
 public sealed class TwinkleStar : MonoBehaviour {
  public SpriteRenderer core,halo;
  public float phase,frequency=1,brightness=.7f;
  Color tint;
  void Awake(){tint=core.color;}
  void Update(){
   float t=Time.timeSinceLevelLoad;
   float pulse=.45f+.55f*Mathf.Pow(.5f+.5f*Mathf.Sin(t*frequency+phase),3);
   core.color=new Color(tint.r,tint.g,tint.b,pulse*brightness);
   if(halo)halo.color=new Color(tint.r,tint.g,tint.b,pulse*brightness*.48f);
  }
 }
}
