using System;
using UnityEngine;

namespace Starfall {
 // A separate airborne creature: meters are authored in the mesh, not applied as a giant root scale.
 public sealed class SkyTitan:MonoBehaviour {
  public FirstPersonExplorer player;
  public CreatureArena arena;
  public Transform head,jaw;
  public Transform[] wings,tail,tendrils;
  public Renderer[] organs;
  public AudioSource breath,voice;
  public float AnimationTime {get;private set;}
  public int RoarCount {get;private set;}
  public bool IsRoaring {get;private set;}
  public event Action Roared;
  public bool Active=>player&&player.Ready&&!player.Paused&&(!arena||arena.Active)&&(!player.session||!player.session.IsTransitioning);
  Vector3 home;Quaternion headRest,jawRest;Quaternion[] wingRest,tailRest,tendrilRest;MaterialPropertyBlock emission;AudioClip rumbleClip,roarClip;bool audioPaused;
  static Quaternion[] Rotations(Transform[] parts){var q=new Quaternion[parts.Length];for(int i=0;i<parts.Length;i++)q[i]=parts[i].localRotation;return q;}
  void Awake(){home=transform.position;headRest=head.localRotation;jawRest=jaw.localRotation;wingRest=Rotations(wings);tailRest=Rotations(tail);tendrilRest=Rotations(tendrils);emission=new MaterialPropertyBlock();}
  void Start(){
   rumbleClip=Sound(false);roarClip=Sound(true);breath.clip=rumbleClip;breath.loop=true;voice.clip=roarClip;
   // These are bounded synthesized creature sounds, independent from the player's movement.
   breath.Play();if(!Active){breath.Pause();audioPaused=true;}
  }
  public Bounds VisualBounds {get{var bounds=new Bounds(transform.position,Vector3.zero);foreach(var r in GetComponentsInChildren<MeshRenderer>())bounds.Encapsulate(r.bounds);return bounds;}}
  void Update(){
   if(!Active){if(!audioPaused){breath.Pause();voice.Pause();audioPaused=true;}return;}
   if(audioPaused){breath.UnPause();voice.UnPause();audioPaused=false;}
   float dt=Time.deltaTime;AnimationTime+=dt;float t=AnimationTime;
   transform.position=home+new Vector3(Mathf.Sin(t*.034f)*9,Mathf.Sin(t*.11f)*1.2f,Mathf.Sin(t*.026f)*6);
   transform.rotation=Quaternion.Euler(Mathf.Sin(t*.05f)*.8f,Mathf.Sin(t*.032f)*3,Mathf.Sin(t*.06f)*.7f);
   for(int i=0;i<wings.Length;i++){float sign=i==0?-1:1;wings[i].localRotation=wingRest[i]*Quaternion.Euler(0,0,sign*(Mathf.Sin(t*.24f)*3.8f));}
   for(int i=0;i<tail.Length;i++)tail[i].localRotation=tailRest[i]*Quaternion.Euler(Mathf.Sin(t*.18f-i*.65f)*2.3f,Mathf.Sin(t*.14f-i*.65f)*4,0);
   for(int i=0;i<tendrils.Length;i++)tendrils[i].localRotation=tendrilRest[i]*Quaternion.Euler(Mathf.Sin(t*.29f+i*.67f)*3,0,Mathf.Sin(t*.21f+i)*5);
   var direction=transform.InverseTransformDirection(player.transform.position-head.position);float yaw=Mathf.Clamp(Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg,-9,9);
   head.localRotation=Quaternion.Slerp(head.localRotation,headRest*Quaternion.Euler(0,yaw,Mathf.Sin(t*.12f)),dt*.22f);
   float cycle=t%28;bool roaring=cycle>=8&&cycle<13.2f;
   if(roaring&&!IsRoaring){RoarCount++;voice.Play();Roared?.Invoke();}IsRoaring=roaring;
   float intensity=roaring?Mathf.Sin((cycle-8)/5.2f*Mathf.PI):0;
   var attack=GetComponent<TitanAreaAttack>();if(attack&&attack.IsAttacking)intensity=Mathf.Max(intensity,attack.IsFiring?1:.55f);
   jaw.localRotation=jawRest*Quaternion.Euler(-intensity*22,0,0);
   foreach(var organ in organs){emission.SetColor("_EmissionColor",new Color(.64f,.20f,.07f)*(1.1f+intensity*2.1f));organ.SetPropertyBlock(emission);}
  }
  static AudioClip Sound(bool roar){
   const int rate=22050;int count=(int)(rate*(roar?5.2f:8));var data=new float[count];var rng=new System.Random(roar?11019:11011);float filtered=0;
   for(int i=0;i<count;i++){float t=i/(float)rate;float u=i/(float)(count-1);float envelope=roar?Mathf.Pow(Mathf.Sin(Mathf.PI*u),.75f):.7f+.3f*Mathf.Cos(t*Mathf.PI/4);filtered=filtered*.94f+((float)rng.NextDouble()*2-1)*.06f;
    float carrier=Mathf.Sin(2*Mathf.PI*(roar?43:31)*t+Mathf.Sin(t*2.7f)*2)*.16f+Mathf.Sin(2*Mathf.PI*67*t)*.075f;
    data[i]=(carrier+filtered*(roar?.55f:.18f))*envelope*(roar?1:.45f);
   }
   var clip=AudioClip.Create(roar?"Eclipse leviathan call":"Colossal breathing",count,1,rate,false);clip.SetData(data,0);return clip;
  }
  void OnDestroy(){if(rumbleClip)Destroy(rumbleClip);if(roarClip)Destroy(roarClip);}
 }
}
