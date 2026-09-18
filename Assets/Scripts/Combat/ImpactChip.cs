using UnityEngine;

namespace Starfall {
 public sealed class ImpactChip:MonoBehaviour {
  public CreatureArena arena;public Vector3 velocity;float age;Vector3 size;
  void Start(){size=transform.localScale;}
  void Update(){
   if(!arena){Destroy(gameObject);return;}if(!arena.Active)return;
   float dt=Time.deltaTime;age+=dt;velocity+=Vector3.down*(5*dt);transform.position+=velocity*dt;
   transform.rotation=Quaternion.LookRotation(velocity)*Quaternion.Euler(90,0,0);transform.localScale=size*Mathf.Clamp01(1-age/.32f);
   if(age>=.32f)Destroy(gameObject);
  }
 }
}
