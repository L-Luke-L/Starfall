using UnityEngine;
namespace Starfall {
 // Local origin is the torn hull opening. Fire, smoke, light and embers follow it.
 public sealed class WreckFire : MonoBehaviour {
  public MeshRenderer flame;
  public MeshRenderer[] secondaryFlames;
  public SpriteRenderer groundLight,heatCore;
  public SpriteRenderer[] smoke,embers;
  public float wind=.24f,seed=1,smokeHeight=2.0f;
  public float animatedIntensity {get;private set;}
  Material fireMaterial;
  Material[] secondaryMaterials;
  void Awake(){fireMaterial=flame.material;secondaryMaterials=new Material[secondaryFlames.Length];for(int i=0;i<secondaryMaterials.Length;i++)secondaryMaterials[i]=secondaryFlames[i].material;}
  void Update(){
   float t=Time.timeSinceLevelLoad;
   animatedIntensity=.74f+.16f*Mathf.Sin(t*8.7f+seed)+.1f*Mathf.Sin(t*19+seed*5);
   fireMaterial.SetFloat("_Clock",t);fireMaterial.SetFloat("_Seed",seed);fireMaterial.SetFloat("_Wind",wind);
   for(int i=0;i<secondaryMaterials.Length;i++){secondaryMaterials[i].SetFloat("_Clock",t);secondaryMaterials[i].SetFloat("_Seed",seed+i*3.78f+6);secondaryMaterials[i].SetFloat("_Wind",wind);}
   if(heatCore)heatCore.color=new Color(.93f,.17f,.025f,animatedIntensity*.34f);
   if(groundLight)groundLight.color=new Color(.95f,.37f,.105f,animatedIntensity*.30f);
   for(int i=0;i<smoke.Length;i++){
    float p=Mathf.Repeat(t*(.17f+(i%3)*.013f)+i*.61803f,1),s=.28f+p*.95f;
    var sr=smoke[i];sr.transform.localPosition=new Vector3(wind*p*2+Mathf.Sin(i*7+p*6)*p*.20f,p*smokeHeight+.08f,0);
    sr.transform.localScale=new Vector3(s,s*1.22f,1);sr.transform.localRotation=Quaternion.Euler(0,0,i*61+p*20);
    sr.color=Color.Lerp(new Color(.18f,.15f,.13f,0),new Color(.13f,.17f,.22f,0),p)+new Color(0,0,0,Mathf.Sin(p*Mathf.PI)*.20f);
   }
   for(int i=0;i<embers.Length;i++){
    float p=Mathf.Repeat(t*(.30f+(i%4)*.042f)+i*.38197f,1);
    var sr=embers[i];sr.transform.localPosition=new Vector3(wind*p*2+Mathf.Sin(i*9+p*3)*p*.28f,.06f+p*(1.4f+(i%3)*.23f),0);
    sr.color=new Color(1,.43f+.2f*(1-p),.14f,Mathf.Sin(p*Mathf.PI)*.72f);sr.transform.localScale=Vector3.one*(.012f+(i%3)*.005f);
   }
  }
  void OnDestroy(){if(fireMaterial)Destroy(fireMaterial);if(secondaryMaterials!=null)foreach(var m in secondaryMaterials)if(m)Destroy(m);}
 }
}
