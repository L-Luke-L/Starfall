using UnityEngine;

namespace Starfall {
 public sealed class RegionDeviceVisual:MonoBehaviour {
  FirstPersonExplorer player;Renderer lens;Light lamp;Color tint;MaterialPropertyBlock block;float nextUpdate;
  public bool Completed {get;private set;}
  public void Configure(FirstPersonExplorer owner,Renderer display,Light light,Color color,bool complete){player=owner;lens=display;lamp=light;tint=color;block=new MaterialPropertyBlock();Completed=complete;Paint();}
  public void Complete(){Completed=true;Paint();}
  public void SetCompleted(bool value){Completed=value;Paint();}
  void Paint(){if(!lens)return;var color=Completed?new Color(.35f,.76f,.61f):tint;float strength=Completed?.6f:.8f+Mathf.Sin(Time.time*2+transform.position.x)*.18f;block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*strength);lens.SetPropertyBlock(block);if(lamp){lamp.color=color;lamp.intensity=Completed?.35f:.65f;}}
  void Update(){if(!player)return;if(lamp)lamp.enabled=player.Ready&&Vector3.SqrMagnitude(player.transform.position-transform.position)<400;if(Time.time<nextUpdate||player.Paused)return;nextUpdate=Time.time+.16f;Paint();}
 }
}
