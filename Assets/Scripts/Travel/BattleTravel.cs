using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starfall {
 public sealed class BattleTravel:MonoBehaviour {
  public const string SceneName="Scene02_Encounter";
  public SceneDirector director;
  public RecoveryEncounter encounter;
  readonly BasinProgress legacyProgress=new BasinProgress();
  public BasinProgress Progress=>CampaignRuntime.Current?CampaignRuntime.Current.ProgressFor(CampaignRuntime.Current.State.region):legacyProgress;
  string activeScene=SceneName;
  public EncounterRequest Request {get;private set;}
  public FirstPersonExplorer Explorer {get;private set;}
  public bool IsEngaged {get;private set;}
  public bool IsTransitioning {get;private set;}
  public string TransitionError {get;private set;}="";
  public event Action<EncounterRequest> Entered;
  public event Action Returned;
  AudioListener expeditionListener;bool expeditionListenerEnabled;
  readonly List<GameObject> hiddenRoots=new List<GameObject>();
  Scene expedition;float fade;bool originalDirectorEnabled;float oldTimeScale;int oldAA;bool oldFog;Color oldFogColor,oldAmbient,oldSkyAmbient,oldEquator,oldGround;float oldFogDensity;Material oldSky;Light oldSun;UnityEngine.Rendering.AmbientMode oldAmbientMode;FogMode oldFogMode;CursorLockMode oldCursor;bool oldCursorVisible;
  void OnEnable(){if(encounter)encounter.BattleRequested+=Enter;}
  void OnDisable(){if(encounter)encounter.BattleRequested-=Enter;}
  IEnumerator Start(){
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-battlePreview"||x=="-titanPreview")){yield return null;Enter(null);}
  }
  public void Enter(EncounterRequest request){if(!director||IsEngaged||(director.forge&&director.forge.IsOpen))return;TransitionError="";Request=request;StartCoroutine(EnterRoutine());}
  IEnumerator FadeTo(float target){float from=fade;for(float t=0;t<.38f;t+=Time.unscaledDeltaTime){fade=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,t/.38f));yield return null;}fade=target;}
  IEnumerator EnterRoutine(){
   InteractionFeel.Play(FeelCue.Portal,.9f);IsEngaged=true;IsTransitioning=true;oldTimeScale=Time.timeScale;Time.timeScale=1;oldAA=QualitySettings.antiAliasing;expedition=gameObject.scene;originalDirectorEnabled=director.enabled;oldCursor=Cursor.lockState;oldCursorVisible=Cursor.visible;
   oldFog=RenderSettings.fog;oldFogColor=RenderSettings.fogColor;oldFogDensity=RenderSettings.fogDensity;oldSky=RenderSettings.skybox;oldAmbient=RenderSettings.ambientLight;
   oldSun=RenderSettings.sun;oldAmbientMode=RenderSettings.ambientMode;oldFogMode=RenderSettings.fogMode;oldSkyAmbient=RenderSettings.ambientSkyColor;oldEquator=RenderSettings.ambientEquatorColor;oldGround=RenderSettings.ambientGroundColor;
   yield return FadeTo(1);
   expeditionListener=director.worldCamera?director.worldCamera.GetComponent<AudioListener>():null;
   expeditionListenerEnabled=expeditionListener&&expeditionListener.enabled;
   if(expeditionListener)expeditionListener.enabled=false;
   int region=CampaignRuntime.Current?CampaignRuntime.Current.State.region:0;activeScene=region==1?"Scene03_WreckRift":region==2?"Scene04_Battlefield":region==3?"Scene05_Medical":region==4?"Scene06_Shipyard":region==5?"Scene07_Core":SceneName;
   AsyncOperation load=null;string failure=null;
   if(!Application.CanStreamedLevelBeLoaded(activeScene))failure="星界场景未包含在当前构建中："+activeScene;
   else try{load=SceneManager.LoadSceneAsync(activeScene,LoadSceneMode.Additive);}catch(Exception ex){failure="星界场景加载失败："+ex.Message;}
   if(load==null){yield return FailedEntry(failure??"星界场景加载失败",default);yield break;}yield return load;
   var battle=SceneManager.GetSceneByName(activeScene);
   if(!battle.IsValid()||!battle.isLoaded){yield return FailedEntry("星界场景加载未完成",battle);yield break;}
   foreach(var root in battle.GetRootGameObjects()){Explorer=root.GetComponentInChildren<FirstPersonExplorer>(true);if(Explorer)break;}
   if(!Explorer||!Explorer.viewCamera||!Explorer.arena||!Explorer.arsenal||!Explorer.expedition){yield return FailedEntry("星界场景缺少可用的探索者组件",battle);yield break;}
   SceneManager.SetActiveScene(battle);
   foreach(var root in expedition.GetRootGameObjects())if(root!=gameObject&&root.activeSelf){hiddenRoots.Add(root);root.SetActive(false);}
   director.enabled=false;QualitySettings.antiAliasing=4;
   if(Explorer){Explorer.Automated=(CampaignRuntime.Current&&CampaignRuntime.Current.ShowcaseActive)||Array.Exists(Environment.GetCommandLineArgs(),x=>x.EndsWith("QA",StringComparison.OrdinalIgnoreCase));Explorer.Begin(this);if(CampaignRuntime.Current)CampaignRuntime.Current.ConfigureRegion(Explorer);if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-titanPreview"))Explorer.Look(0,16);}
   yield return FadeTo(0);IsTransitioning=false;Entered?.Invoke(Request);
  }
  IEnumerator FailedEntry(string message,Scene loaded){
   TransitionError=message;Debug.LogWarning(message);
   if(expedition.IsValid()&&expedition.isLoaded)SceneManager.SetActiveScene(expedition);
   if(loaded.IsValid()&&loaded.isLoaded&&loaded!=expedition){var unload=SceneManager.UnloadSceneAsync(loaded);if(unload!=null)yield return unload;}
   Explorer=null;RestoreExpedition();Cursor.lockState=oldCursor;Cursor.visible=oldCursorVisible;
   yield return FadeTo(0);IsEngaged=false;IsTransitioning=false;
   var campaign=CampaignRuntime.Current;
   if(campaign){campaign.Returned();campaign.Notice("无法进入星界，已返回原场景。"+message);}else director.Say("无法进入星界，已返回原场景");
  }
  void RestoreExpedition(){
   foreach(var root in hiddenRoots)if(root)root.SetActive(true);hiddenRoots.Clear();if(director)director.enabled=originalDirectorEnabled;
   if(expeditionListener)expeditionListener.enabled=expeditionListenerEnabled;expeditionListener=null;
   RenderSettings.fog=oldFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogDensity=oldFogDensity;RenderSettings.skybox=oldSky;RenderSettings.ambientLight=oldAmbient;QualitySettings.antiAliasing=oldAA;Time.timeScale=oldTimeScale;
   RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.fogMode=oldFogMode;RenderSettings.ambientSkyColor=oldSkyAmbient;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;
  }
  public void ReturnToExpedition(){if(IsEngaged&&!IsTransitioning)StartCoroutine(ReturnRoutine());}
  IEnumerator ReturnRoutine(){
   InteractionFeel.Play(FeelCue.Portal,.65f);IsTransitioning=true;if(Explorer){if(Explorer.expedition)Explorer.expedition.Save();if(CampaignRuntime.Current)CampaignRuntime.Current.Save();Explorer.Ready=false;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   yield return FadeTo(1);SceneManager.SetActiveScene(expedition);var loaded=SceneManager.GetSceneByName(activeScene);if(loaded.IsValid()&&loaded.isLoaded){var unload=SceneManager.UnloadSceneAsync(loaded);if(unload!=null)yield return unload;}
   RestoreExpedition();
   Explorer=null;yield return FadeTo(0);IsEngaged=false;IsTransitioning=false;if(CampaignRuntime.Current)CampaignRuntime.Current.Returned();Returned?.Invoke();
  }
  void OnGUI(){if(fade<=0)return;var m=GUI.matrix;GUI.matrix=Matrix4x4.identity;GUI.depth=-500;GUI.color=new Color(.005f,.009f,.015f,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.matrix=m;}
 }
}
