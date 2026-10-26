using System;
using UnityEngine;
namespace Starfall {
 public sealed class AlienBolt:MonoBehaviour {
  public CreatureArena arena;
  public AlienCreature owner;
  public Vector3 velocity;
  public bool HitWorld {get;private set;}
  public Vector3 HitPosition {get;private set;}
  float age;bool resolved;
  void Update(){if(arena&&arena.Active)Step(Time.deltaTime);}
  public void Step(float dt){
   if(resolved||!arena||!arena.Active||dt<=0)return;
   age+=dt;if(age>5||arena.Downed){resolved=true;Destroy(gameObject);return;}
   var hits=Physics.SphereCastAll(transform.position,.16f,velocity.normalized,velocity.magnitude*dt,~0,QueryTriggerInteraction.Ignore);
   Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits){
    if(IsOwner(hit.collider))continue;
    // SphereCastAll reports zero as the point for initial overlaps. Keep the
    // impact at the actual contact instead of spawning it at the world origin.
    Hit(hit.collider,hit.distance<=0?hit.collider.ClosestPoint(transform.position):hit.point);return;
   }
   transform.position+=velocity*dt;
  }
  bool IsOwner(Collider collider)=>owner&&collider.GetComponentInParent<AlienCreature>()==owner;
  void Hit(Collider collider,Vector3 point){
   resolved=true;HitPosition=point;bool damaged=false;var player=collider.GetComponentInParent<FirstPersonExplorer>();
   if(player==arena.player){damaged=arena.Damage(CampaignRuntime.Current?CampaignRules.EnemyDamage(19):19,owner);if(damaged&&owner)owner.RegisterHit();}
   else {HitWorld=true;arena.BlockedProjectiles++;}
   arena.ReportProjectileHit(this,collider,damaged);arena.Impact(point,Color.cyan);Destroy(gameObject);
  }
 }
}
