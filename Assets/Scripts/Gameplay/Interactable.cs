using UnityEngine;
using UnityEngine.Events;

namespace Starfall {
 public enum SiteKind { Camp, Mine, Wreck, Ore, Wormhole }
 public sealed class Interactable : MonoBehaviour {
  public SiteKind kind;
  public string displayName;
  [TextArea] public string description;
  public float radius=2.2f;
  public UnityEvent onInteract=new UnityEvent();
  public bool collected;
  public void Invoke(){onInteract.Invoke();}
 }
}
