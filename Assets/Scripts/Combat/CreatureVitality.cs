using UnityEngine;

namespace Starfall {
 public sealed class CreatureVitality:MonoBehaviour {
  public int maxHealth=180;public string persistentId;
  string SaveKey=>CampaignRuntime.Current?persistentId:gameObject.name;
  public int Health {get;private set;}
  public bool Dead=>Health<=0;
  public string DisplayName=>giant?"蚀穹古龙":actor.kind==CreatureKind.RiftMauler?"玄脊地龙":"苍翼飞龙";
  AlienCreature actor;SkyTitan giant;CreatureArena arena;BasinExpedition expedition;bool initialized;float flash,fall;
  Vector3 visualPosition;Quaternion visualRotation;MaterialPropertyBlock tint;
  public void Initialize(BasinExpedition owner){
   if(initialized)return;initialized=true;expedition=owner;arena=owner.player.arena;actor=GetComponent<AlienCreature>();giant=GetComponent<SkyTitan>();
   Health=owner.Progress.enemyHealth.TryGetValue(SaveKey,out var saved)?saved:maxHealth;tint=new MaterialPropertyBlock();
   if(actor){visualPosition=actor.visual.localPosition;visualRotation=actor.visual.localRotation;}
   if(Dead)ApplyDeath(false);
  }
  public bool TakeDamage(int damage,Vector3 point){
   if(!initialized||Dead||!arena.CanTarget||damage<=0)return false;
   InteractionFeel.Play(FeelCue.Hit,.55f);Health=Mathf.Max(0,Health-damage);expedition.Progress.enemyHealth[SaveKey]=Health;flash=.12f;
   if(actor)actor.AlertToPlayer();if(Dead)ApplyDeath(true);if(CampaignRuntime.Current)CampaignRuntime.Current.Changed();return true;
  }
  public float DamageMultiplier(Vector3 point){if(giant)return Vector3.Distance(point,giant.head.position)<28?1.5f:.35f;return point.y>transform.position.y+(actor.kind==CreatureKind.RiftMauler?2.2f:.2f)?1.65f:1;}
  void ApplyDeath(bool reward){
   foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
   if(actor){actor.enabled=false;actor.warning.enabled=false;actor.glow.enabled=false;arena.Release(actor);if(actor.Agent&&actor.Agent.isOnNavMesh){actor.Agent.isStopped=true;actor.Agent.ResetPath();}actor.Agent.enabled=false;}
   if(giant){var attack=GetComponent<TitanAreaAttack>();if(attack){attack.Cancel();attack.AttackEnabled=false;}giant.enabled=false;giant.breath.Stop();giant.voice.Stop();foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;}
   if(reward){expedition.OnDefeated(this,giant!=null);arena.Impact(transform.position+Vector3.up,Color.cyan);}
   else if(actor)actor.visual.gameObject.SetActive(false);
  }
  void Update(){
   if(!initialized||!arena.Active)return;
   if(flash>0){flash-=Time.deltaTime;tint.SetColor("_EmissionColor",flash>0?new Color(.9f,.24f,.08f):Color.black);foreach(var r in GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial&&r.sharedMaterial.name.Contains("hide"))r.SetPropertyBlock(tint);}
   if(Dead&&actor){fall+=Time.deltaTime;actor.visual.localRotation=Quaternion.Slerp(visualRotation,Quaternion.Euler(0,0,85),Mathf.Clamp01(fall*2));actor.visual.localPosition=visualPosition+Vector3.down*Mathf.Min(fall*.45f,.8f);if(fall>8)actor.visual.gameObject.SetActive(false);}
  }
 }
}
