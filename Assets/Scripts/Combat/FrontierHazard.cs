using UnityEngine;

namespace Starfall {
 public sealed class FrontierHazard:MonoBehaviour {
  CreatureArena arena;AlienCreature owner;LineRenderer ring;Vector3 point;float radius,tell,duration,clock;int damage;bool friendly,frost,struck,playerHit;MaterialPropertyBlock tint;Mesh footprint;MeshRenderer surface;Color color;
  public bool Armed=>clock>=tell;public float Age=>clock;
  public void Configure(CreatureArena field,AlienCreature source,Vector3 position,float range,float warning,float linger,int hit,bool playerOwned,bool ice){arena=field;owner=source;point=position;radius=range;tell=warning;duration=linger;damage=hit;friendly=playerOwned;frost=ice;transform.position=point;ring=gameObject.AddComponent<LineRenderer>();ring.positionCount=49;
   color=ice?MonsterAttackFx.Frost:playerOwned?MonsterAttackFx.Heat:MonsterAttackFx.Magnetic;
   arena.MonsterFx.Style(ring,color,.12f,.12f);
   tint=new MaterialPropertyBlock();surface=arena.MonsterFx.GroundPatch(gameObject,point,radius,color,out footprint);InteractionFeel.Play(FeelCue.Alert,.4f);Paint();}
  void Paint(){for(int i=0;i<49;i++){float angle=i*Mathf.PI*2/48;var p=point+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);p.y=FieldSurface.Height(p.x,p.z,point.y)+.07f;ring.SetPosition(i,p);}}
  void Update(){if(!arena||!arena.Active)return;clock+=Time.deltaTime;float strength=Armed?.87f:Mathf.Lerp(.39f,.88f,Mathf.Clamp01(clock/Mathf.Max(.01f,tell)));
   Color current=color;current.a=strength;
   MonsterAttackFx.Tint(surface,tint,current);
   ring.startColor=ring.endColor=current;
   ring.startWidth=ring.endWidth=Armed?.24f:.14f+.04f*Mathf.Sin(clock*12);if(Armed&&!struck){struck=true;InteractionFeel.Play(FeelCue.Shot,friendly?.8f:.5f);arena.MonsterFx.Burst(point+Vector3.up*.2f,Vector3.up,color,frost?1.85f:1.45f);if(friendly){foreach(var actor in arena.creatures){if(!actor)continue;var vitality=actor.GetComponent<CreatureVitality>();if(!vitality||vitality.Dead||Vector3.Distance(actor.transform.position,point)>radius+1||!FrontierVisibility.Clear(point+Vector3.up*.5f,actor.transform.position+Vector3.up*.8f))continue;vitality.TakeDamage(damage,actor.transform.position+Vector3.up);}}}
   if(Armed&&!playerHit&&CreatureArena.FlatDistance(arena.player.transform.position,point)<radius&&Mathf.Abs(arena.player.transform.position.y-point.y)<2&&FrontierVisibility.Clear(point+Vector3.up*.4f,arena.player.transform.position+Vector3.up)){if(arena.Damage(friendly?15:damage,owner)){playerHit=true;if(frost)arena.player.arsenal.SpendStamina(Mathf.Min(20,arena.player.arsenal.Stamina));}}
   if(clock>=tell+duration)Destroy(gameObject);
  }
  void OnDestroy(){if(footprint)Destroy(footprint);}
 }
}
