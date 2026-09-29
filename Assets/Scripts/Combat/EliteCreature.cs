using UnityEngine;

namespace Starfall {
 public sealed class EliteCreature:MonoBehaviour {
  public string Id=>ground?"ground":"wing";
  public string DisplayName=>ground?"玄甲地龙":"雷冠翼龙";
  public bool Exposed=>exposed>0;
  public bool Landed=>landed>0;
  AlienCreature actor;CreatureArena arena;CampaignRuntime campaign;bool ground;
  float exposed,cycle,tell,landed;int phase;Vector3 locked;
  LineRenderer[] marks;
  public void Configure(AlienCreature creature,bool isGround,CreatureArena owner,CampaignRuntime state){actor=creature;ground=isGround;arena=owner;campaign=state;cycle=5;marks=new LineRenderer[3];for(int i=0;i<3;i++){var g=new GameObject("Elite attack warning "+i);g.transform.SetParent(transform);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=actor.warning.sharedMaterial;line.positionCount=25;line.startWidth=.10f;line.endWidth=.10f;line.enabled=false;marks[i]=line;}}
  public void Expose(float seconds){exposed=Mathf.Max(exposed,seconds);}
  public int ModifyDamage(int damage){if(!actor||!arena||!arena.player)return damage;if(!ground)return landed>0?Mathf.RoundToInt(damage*1.35f):damage;var toPlayer=arena.player.transform.position-transform.position;toPlayer.y=0;bool front=Vector3.Dot(transform.forward,toPlayer.normalized)>.15f;return exposed>0?Mathf.RoundToInt(damage*1.35f):front?Mathf.Max(1,Mathf.RoundToInt(damage*.30f)):damage;}
  void Update(){if(!actor||!arena||!arena.Active||GetComponent<CreatureVitality>().Dead){Warn(false);return;}float dt=Time.deltaTime;exposed=Mathf.Max(0,exposed-dt);landed=Mathf.Max(0,landed-dt);
   if(arena.Safe||arena.Downed){Warn(false);return;}float distance=CreatureArena.FlatDistance(transform.position,arena.player.transform.position);if(distance>22)return;
   cycle-=dt;if(phase==0&&cycle<=0){phase=1;tell=ground?.95f:1.15f;locked=arena.player.transform.position;InteractionFeel.Play(FeelCue.Alert,.75f);}
   if(phase==1){tell-=dt;Warn(true);if(tell<=0){Warn(false);phase=2;tell=ground?.34f:.52f;if(!ground){actor.enabled=false;actor.Agent.isStopped=false;}}return;}
   if(phase==2){tell-=dt;if(!ground){var flat=locked-transform.position;flat.y=0;actor.Agent.Move(flat.normalized*15*dt);actor.Agent.baseOffset=Mathf.MoveTowards(actor.Agent.baseOffset,.5f,dt*7);}
    if(tell<=0){if(ground){if(distance<4&&arena.ClearSight(transform.position+Vector3.up*1.5f,arena.player.transform.position+Vector3.up,actor))arena.Damage(CampaignRules.EnemyDamage(29),actor);}else{for(int i=0;i<3;i++){Vector3 p=locked+new Vector3((i-1)*3,0,i*2);if(CreatureArena.FlatDistance(arena.player.transform.position,p)<2.2f&&arena.ClearSight(transform.position+Vector3.up,arena.player.transform.position+Vector3.up,actor))arena.Damage(CampaignRules.EnemyDamage(24),actor);}landed=1.6f;actor.Agent.baseOffset=.5f;actor.enabled=true;actor.ResumeFromElite();}
     arena.Impact(locked,ground?Color.red:Color.cyan);phase=0;cycle=ground?7:9;}
   }
  }
  void Warn(bool active){if(marks==null)return;for(int i=0;i<marks.Length;i++){var line=marks[i];if(!line)continue;line.enabled=active&&(ground?i==0:true);if(!line.enabled)continue;Vector3 center=ground?transform.position:locked+new Vector3((i-1)*3,0,i*2);for(int j=0;j<25;j++){float a=j*Mathf.PI*2/24,x=center.x+Mathf.Cos(a)*(ground?4:2.2f),z=center.z+Mathf.Sin(a)*(ground?4:2.2f);line.SetPosition(j,new Vector3(x,AlienSurfaceLandmarks.Height(x,z)+.15f,z));}}}
 }
}
