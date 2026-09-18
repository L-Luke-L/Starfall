using UnityEngine;
namespace Starfall {
 public sealed class CreatureImpact:MonoBehaviour {
  public CreatureArena arena;float age;
  void Update(){if(!arena||!arena.Active)return;age+=Time.deltaTime;transform.localScale=Vector3.one*(.22f+age*3);if(age>.2f)Destroy(gameObject);}
 }
}
