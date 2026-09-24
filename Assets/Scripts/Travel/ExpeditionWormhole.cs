using System;
using UnityEngine;

namespace Starfall {
 public sealed class ExpeditionWormhole:MonoBehaviour {
  public const string ProgressKey="Starfall.BasinWormholeUnlocked.v1";
  public SceneDirector director;
  public BattleTravel travel;
  public GameObject portalVisual;
  public Transform[] arcs;
  public bool Unlocked {get;private set;}
  public bool IsVerification=>Array.Exists(Environment.GetCommandLineArgs(),a=>a.EndsWith("QA",StringComparison.OrdinalIgnoreCase));
  public string StorageKey=>ProgressKey+(IsVerification?".verification":"");
  public bool CanEnter=>Unlocked&&director&&director.enabled&&travel&&!travel.IsEngaged&&!director.IsPaused&&Time.timeScale>0&&!director.Transitioning&&!director.RecoveryLocked&&director.hookState==SceneDirector.HookState.Swinging&&(!director.forge||!director.forge.IsOpen);
  bool pendingNotice;
  void Awake(){ReloadSavedState();}
  void OnEnable(){if(travel){travel.Entered+=OnEntered;travel.Returned+=RefreshVisual;}}
  void OnDisable(){if(travel){travel.Entered-=OnEntered;travel.Returned-=RefreshVisual;}}
  public void ReloadSavedState(){Unlocked=CampaignRuntime.Current?CampaignRuntime.Current.State.calibrated:PlayerPrefs.GetInt(StorageKey,0)==1;RefreshVisual();}
  void OnEntered(EncounterRequest request){if(CampaignRuntime.Current||Unlocked)return;Unlocked=true;PlayerPrefs.SetInt(StorageKey,1);PlayerPrefs.Save();pendingNotice=true;RefreshVisual();}
  void RefreshVisual(){if(CampaignRuntime.Current)Unlocked=CampaignRuntime.Current.State.calibrated&&!CampaignRuntime.Current.State.Has("rift-destroyed");if(portalVisual)portalVisual.SetActive(Unlocked&&travel&&!travel.IsEngaged);}
  void Update(){
   RefreshVisual();if(!portalVisual||!portalVisual.activeInHierarchy)return;
   if(pendingNotice&&director.enabled&&!travel.IsEngaged){pendingNotice=false;director.Say("虫洞已稳定");}
   for(int i=0;i<arcs.Length;i++)if(arcs[i])arcs[i].localRotation=Quaternion.Euler(0,0,Time.time*(i%2==0?12:-9)+i*65);
  }
  public bool TryEnter(){if(!CanEnter){InteractionFeel.Play(FeelCue.Deny,.4f);return false;}if(CampaignRuntime.Current){CampaignRuntime.Current.Open("coordinates");return true;}travel.Enter(null);return travel.IsEngaged;}
 }
}
