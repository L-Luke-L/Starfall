using System;
using UnityEngine;
namespace Starfall {
 public sealed class AlienBolt:MonoBehaviour {
  public CreatureArena arena;
  public AlienCreature owner;
  public Vector3 velocity;
  public bool HitWorld {get;private set;}
  float age;
  void Update(){if(arena&&arena.Active)Step(Time.deltaTime);}
  public void Step(float dt){
   if(!arena||!arena.Active||dt<=0)return;
   age+=dt;if(age>5||arena.Downed){Destroy(gameObject);return;}
   var hits=Physics.SphereCastAll(transform.position,.16f,velocity.normalized,velocity.magnitude*dt,~0,QueryTriggerInteraction.Ignore);
   Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits){
    if(hit.collider.GetComponentInParent<AlienCreature>()==owner)continue;
    var player=hit.collider.GetComponentInParent<FirstPersonExplorer>();
    if(player==arena.player){if(arena.Damage(CampaignRuntime.Current?CampaignRules.EnemyDamage(19):19,owner)&&owner)owner.RegisterHit();}else {HitWorld=true;arena.BlockedProjectiles++;}
    arena.Impact(hit.point,Color.cyan);Destroy(gameObject);return;
   }
   transform.position+=velocity*dt;
  }
 }
}
