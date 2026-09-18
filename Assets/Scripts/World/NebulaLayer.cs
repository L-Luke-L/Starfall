using UnityEngine;
namespace Starfall {
 public sealed class NebulaLayer : MonoBehaviour {
  public float driftSpeed=.02f;
  Material material;
  void Awake(){material=GetComponent<Renderer>().material;}
  void Update(){material.SetFloat("_Clock",Time.timeSinceLevelLoad);material.SetFloat("_Drift",driftSpeed);}
  void OnDestroy(){if(material)Destroy(material);}
 }
}
