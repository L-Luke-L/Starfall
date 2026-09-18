using UnityEngine;
namespace Starfall {
 public sealed class ShootingStar : MonoBehaviour {
  public LineRenderer trail,bloom;
  public SpriteRenderer head,halo;
  public float firstDelay=.7f,interval=5.2f,duration=1.15f;
  public int seed=34;
  public int visibleFrames {get;private set;}
  float nextStart,started=-99;
  Vector3 origin,velocity;
  System.Random rng;
  float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
  void Awake(){rng=new System.Random(seed);nextStart=firstDelay;}
  void Update(){
   float t=Time.timeSinceLevelLoad;
   if(t>=nextStart){started=t;origin=new Vector3(R(-4,14),R(4.7f,7.05f),0);velocity=new Vector3(R(-9,-6),R(-3.1f,-1.8f),0);nextStart=t+interval+R(-.7f,.9f);}
   float age=t-started,p=age/duration;bool visible=p>=0&&p<1;
   trail.enabled=bloom.enabled=head.enabled=halo.enabled=visible;
   if(!visible)return;visibleFrames++;
   float a=Mathf.SmoothStep(0,1,Mathf.Min(p*9,(1-p)*4));
   Vector3 pos=origin+velocity*age,back=-velocity.normalized;
   float length=Mathf.Min(age*velocity.magnitude,2.4f);
   for(int i=0;i<trail.positionCount;i++){var point=pos+back*(length*i/(trail.positionCount-1));trail.SetPosition(i,point);bloom.SetPosition(i,point);}
   var tint=new Color(.77f,.89f,1,a*.8f);trail.startColor=tint;trail.endColor=new Color(.37f,.56f,.75f,0);
   bloom.startColor=new Color(.42f,.66f,1,a*.15f);bloom.endColor=new Color(.2f,.5f,.7f,0);
   head.transform.position=halo.transform.position=pos;head.color=new Color(.89f,.96f,1,a);halo.color=new Color(.42f,.68f,1,a*.5f);
  }
 }
}
