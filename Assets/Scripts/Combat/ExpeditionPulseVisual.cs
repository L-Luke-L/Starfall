using UnityEngine;

namespace Starfall {
 public sealed class ExpeditionPulseVisual:MonoBehaviour {
  CreatureArena arena;LineRenderer ring;float age;readonly float[] heights=new float[33];Vector3 center;
  public void Configure(CreatureArena owner,Vector3 origin,Material material){arena=owner;center=origin;transform.position=origin;ring=gameObject.AddComponent<LineRenderer>();ring.sharedMaterial=material;ring.positionCount=33;ring.startWidth=.065f;ring.endWidth=.065f;ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;for(int i=0;i<heights.Length;i++){float a=i*Mathf.PI*2/32;heights[i]=FieldSurface.Height(center.x+Mathf.Sin(a)*6,center.z+Mathf.Cos(a)*6,center.y)+.12f;}Paint(0);}
  void Paint(float t){float r=Mathf.Lerp(.25f,6,t);ring.startWidth=ring.endWidth=.06f*(1-t);for(int i=0;i<heights.Length;i++){float a=i*Mathf.PI*2/32;ring.SetPosition(i,new Vector3(center.x+Mathf.Sin(a)*r,Mathf.Lerp(center.y+.15f,heights[i],t),center.z+Mathf.Cos(a)*r));}}
  void Update(){if(!arena||!arena.Active)return;age+=Time.deltaTime;Paint(Mathf.Clamp01(age/.65f));if(age>=.65f)Destroy(gameObject);}
 }
}
