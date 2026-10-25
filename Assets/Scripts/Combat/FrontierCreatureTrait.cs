using UnityEngine;

namespace Starfall {
 public sealed class FrontierCreatureTrait:MonoBehaviour {
  public bool frost;public string DisplayName=>frost?"霜环翼龙":"磁蚀地龙";public int HazardsCreated {get;private set;}
  AlienCreature actor;CreatureVitality life;CreatureState previous;int trailCount;float trailClock;Vector3 lastTrail;
  public void Configure(AlienCreature creature,bool ice){actor=creature;frost=ice;life=GetComponent<CreatureVitality>();ImportedCreatureAnimation.EnsureFor(actor);previous=CreatureState.Roam;var block=new MaterialPropertyBlock();var color=ice?new Color(.33f,.50f,.59f):new Color(.29f,.23f,.34f);foreach(var renderer in actor.visual.GetComponentsInChildren<Renderer>()){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++){if(!materials[i]||!materials[i].name.Contains("hide"))continue;renderer.GetPropertyBlock(block,i);block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*.06f);renderer.SetPropertyBlock(block,i);}}actor.visual.localScale*=ice?1.10f:1.12f;
   // Separate dorsal plates use the established dragon materials and retain the animated skeleton.
   for(int i=0;i<4;i++){var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name=ice?"独立霜冠鳍":"独立磁蚀背甲";plate.transform.SetParent(actor.core,false);plate.transform.localPosition=new Vector3(0,.55f,-.45f+i*.25f);plate.transform.localRotation=Quaternion.Euler(0,0,ice?25:-12);plate.transform.localScale=new Vector3(.12f,.4f,.26f);Destroy(plate.GetComponent<Collider>());plate.GetComponent<Renderer>().sharedMaterial=actor.glow?actor.warning.sharedMaterial:actor.core.GetComponent<Renderer>().sharedMaterial;}
  }
  void Update(){if(!actor||!life||life.Dead||!actor.arena.Active)return;var state=actor.State;
   if(!frost){if(state==CreatureState.Charge&&previous!=CreatureState.Charge){trailCount=0;trailClock=0;lastTrail=transform.position;}if(state==CreatureState.Charge){trailClock-=Time.deltaTime;if(trailCount<3&&trailClock<=0&&Vector3.Distance(lastTrail,transform.position)>1.5f){lastTrail=transform.position;trailClock=.25f;trailCount++;Create(lastTrail,1.6f,.8f,4,20);}}}
   else if(previous==CreatureState.Volley&&state==CreatureState.Orbit&&actor.arena.CanTarget){Create(actor.arena.player.transform.position,3,1.5f,.35f,10);}
   previous=state;
  }
  void Create(Vector3 p,float range,float tell,float linger,int damage){p.y=FieldSurface.Height(p.x,p.z,p.y)+.08f;var g=new GameObject(frost?"霜环 · 独立锁定预警":"磁痕 · 独立地表预警");g.transform.SetParent(actor.arena.transform);g.AddComponent<FrontierHazard>().Configure(actor.arena,actor,p,range,tell,linger,damage,false,frost);HazardsCreated++;}
 }
}
