using System;
using UnityEngine;

namespace Starfall {
 // A presentation adapter: navigation, hit volumes, and attack timing remain on AlienCreature.
 public sealed class ImportedCreatureAnimation:MonoBehaviour {
  public Transform artwork;
  public GameObject installedPrefab;
  static GameObject[] groundModels,flyingModels;
  AlienCreature actor;Animator animator;string currentState;int appearanceKey=int.MinValue;
  public string SourceModelName=>installedPrefab?installedPrefab.name:string.Empty;
  static void LoadLibrary(){
   if(groundModels==null||groundModels.Length==0){groundModels=Resources.LoadAll<GameObject>("OpenMonsters/Ground");Array.Sort(groundModels,(a,b)=>string.CompareOrdinal(a.name,b.name));}
   if(flyingModels==null||flyingModels.Length==0){flyingModels=Resources.LoadAll<GameObject>("OpenMonsters/Flying");Array.Sort(flyingModels,(a,b)=>string.CompareOrdinal(a.name,b.name));}
  }
  public static ImportedCreatureAnimation EnsureFor(AlienCreature creature){
   if(!creature)return null;LoadLibrary();var models=creature.kind==CreatureKind.RiftMauler?groundModels:flyingModels;if(models.Length==0)return null;
   var presentation=creature.GetComponent<ImportedCreatureAnimation>();if(!presentation)presentation=creature.gameObject.AddComponent<ImportedCreatureAnimation>();presentation.PrepareAppearance();return presentation;
  }
  static int StableHash(string value){unchecked{uint hash=2166136261;foreach(char c in value??string.Empty){hash^=c;hash*=16777619;}return (int)(hash&0x7fffffff);}}
  GameObject Find(GameObject[] models,string name){foreach(var model in models)if(model.name==name)return model;return models.Length>0?models[0]:null;}
  public void PrepareAppearance(){
   if(!actor)actor=GetComponent<AlienCreature>();if(!actor||!actor.visual)return;LoadLibrary();bool ground=actor.kind==CreatureKind.RiftMauler;var models=ground?groundModels:flyingModels;if(models.Length==0)return;
   var life=GetComponent<CreatureVitality>();var trait=GetComponent<FrontierCreatureTrait>();var elite=GetComponent<EliteCreature>();int region=CampaignRuntime.Current&&CampaignRuntime.Current.State!=null?CampaignRuntime.Current.State.region:0;
   string identity=life&&!string.IsNullOrEmpty(life.persistentId)?life.persistentId:gameObject.name;int key=StableHash(identity+":"+region+":"+(trait?"variant":elite?"elite":"ordinary"));
   if(key==appearanceKey&&artwork&&animator)return;
   string desired=ground?"Dino":"Dragon";
   if(trait)desired=ground?"Alien":"Glub_Evolved";
   else if(elite)desired=ground?"BlueDemon":"Dragon_Evolved";
   else if(region>1){string[] names=ground?new[]{"Dino","Alien","Yeti"}:new[]{"Dragon","Glub_Evolved","Dragon_Evolved"};desired=names[(key%names.Length+region)%names.Length];}
   var source=Find(models,desired);if(!source)return;
   if(!artwork||installedPrefab!=source){
    if(artwork){artwork.gameObject.SetActive(false);Destroy(artwork.gameObject);}
    var instance=Instantiate(source,actor.visual,false);instance.name="Open source monster artwork";artwork=instance.transform;installedPrefab=source;
   }
   foreach(var renderer in actor.visual.GetComponentsInChildren<Renderer>(true)){
    if(renderer.transform.IsChildOf(artwork)||renderer.transform==actor.core||renderer.transform.IsChildOf(actor.core))continue;
    if(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)renderer.enabled=false;
   }
   animator=artwork.GetComponentInChildren<Animator>(true);if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.updateMode=AnimatorUpdateMode.Normal;currentState=null;}
   appearanceKey=key;
   if(trait){var block=new MaterialPropertyBlock();var color=trait.frost?new Color(.49f,.70f,.82f):new Color(.53f,.39f,.64f);foreach(var renderer in artwork.GetComponentsInChildren<Renderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]&&mats[i].name.Contains("hide")){renderer.GetPropertyBlock(block,i);block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*.06f);renderer.SetPropertyBlock(block,i);}}}
   if(life)life.RefreshVisualRenderers();
  }
  void Start(){PrepareAppearance();}
  void LateUpdate(){
   if(!actor||!animator){PrepareAppearance();if(!actor||!animator)return;}
   // A saved dead creature hides its complete visual tree before this adapter
   // ticks. An inactive Animator has no live controller to cross-fade.
   if(!artwork||!artwork.gameObject.activeInHierarchy||!animator.isActiveAndEnabled)return;
   bool active=actor.arena&&actor.arena.Active;animator.speed=active?1:0;if(!active)return;
   var life=GetComponent<CreatureVitality>();bool dead=life&&life.Dead&&!actor.enabled;
   var elite=GetComponent<EliteCreature>();int elitePhase=elite?elite.SpecialPhase:0;
   bool moving=actor.Agent&&actor.Agent.isActiveAndEnabled&&actor.Agent.isOnNavMesh&&actor.Agent.velocity.sqrMagnitude>.15f;
   // Elites temporarily suspend AlienCreature during their own attack cycle.
   // Their phase is the authority for the artwork during that interval.
   string state=dead?"Death":actor.Staggered?"Hit":elitePhase>=1&&elitePhase<=3?"Attack":elitePhase==4?"Idle":actor.State==CreatureState.BiteTell||actor.State==CreatureState.VolleyTell||actor.State==CreatureState.Volley?"Attack":moving||actor.State==CreatureState.Charge?"Move":"Idle";
   if(state!=currentState){int hash=Animator.StringToHash(state);if(animator.HasState(0,hash)){animator.CrossFadeInFixedTime(hash,state=="Death"?.12f:.16f);currentState=state;}}
   if(state=="Move"&&actor.kind==CreatureKind.RiftMauler)animator.speed=actor.State==CreatureState.Charge?1.65f:Mathf.Clamp(actor.Agent.velocity.magnitude/3.2f,.7f,1.4f);
  }
 }
}
