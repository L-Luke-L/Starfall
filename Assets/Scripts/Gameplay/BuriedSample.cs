using UnityEngine;
using UnityEngine.Events;

namespace Starfall {
 public enum MineralShell { IronSeam, AmberInclusion, BlueSeam, SealedGeode }

 [System.Serializable] public sealed class SampleRecoveredEvent : UnityEvent<SubsurfaceDefinition> {}
 [RequireComponent(typeof(Interactable))]
 public sealed class BuriedSample : MonoBehaviour {
  [Header("Visible mineral shell · independent of hidden payload")]
  public MineralShell shell;
  public string visibleName="矿物样本";
  public SpriteRenderer mineralShell;
  public Material looseMaterial;
  [SerializeField] SubsurfaceDefinition definition;
  [Header("Reserved integration hook · deliberately unbound in this scene")]
  public SampleRecoveredEvent onRecovered=new SampleRecoveredEvent();
  public SubsurfaceDefinition Definition=>definition;
  public bool Recovered {get;private set;}
  public void Configure(SubsurfaceDefinition content,MineralShell exterior){definition=content;shell=exterior;}
  public void LiftFromRock(){if(mineralShell){mineralShell.sharedMaterial=looseMaterial;mineralShell.color=Color.white;}}
  public void NotifyRecovered(){
   if(Recovered)return;
   Recovered=true;
   // Future systems can subscribe. No dispatch to combat, fuel, repair or story here.
   onRecovered.Invoke(definition);
  }
 }
}
