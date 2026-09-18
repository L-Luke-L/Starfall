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
  public readonly BasinProgress Progress=new BasinProgress();
  public EncounterRequest Request {get;private set;}
  public FirstPersonExplorer Explorer {get;private set;}
  public bool IsEngaged {get;private set;}
  public bool IsTransitioning {get;private set;}
  public event Action<EncounterRequest> Entered;
  public event Action Returned;
  readonly List<GameObject> hiddenRoots=new List<GameObject>();
  Scene expedition;float fade;bool originalDirectorEnabled;float oldTimeScale;int oldAA;bool oldFog;Color oldFogColor,oldAmbient,oldSkyAmbient,oldEquator,oldGround;float oldFogDensity;Material oldSky;Light oldSun;UnityEngine.Rendering.AmbientMode oldAmbientMode;FogMode oldFogMode;
  void OnEnable(){if(encounter)encounter.BattleRequested+=Enter;}
  void OnDisable(){if(encounter)encounter.BattleRequested-=Enter;}
  IEnumerator Start(){
   if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-battlePreview"||x=="-titanPreview")){yield return null;Enter(null);}
  }
  public void Enter(EncounterRequest request){if(IsEngaged||(director.forge&&director.forge.IsOpen))return;Request=request;StartCoroutine(EnterRoutine());}
  IEnumerator FadeTo(float target){float from=fade;for(float t=0;t<.38f;t+=Time.unscaledDeltaTime){fade=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,t/.38f));yield return null;}fade=target;}
  IEnumerator EnterRoutine(){
   InteractionFeel.Play(FeelCue.Portal,.9f);IsEngaged=true;IsTransitioning=true;oldTimeScale=Time.timeScale;Time.timeScale=1;oldAA=QualitySettings.antiAliasing;expedition=gameObject.scene;originalDirectorEnabled=director.enabled;
   oldFog=RenderSettings.fog;oldFogColor=RenderSettings.fogColor;oldFogDensity=RenderSettings.fogDensity;oldSky=RenderSettings.skybox;oldAmbient=RenderSettings.ambientLight;
   oldSun=RenderSettings.sun;oldAmbientMode=RenderSettings.ambientMode;oldFogMode=RenderSettings.fogMode;oldSkyAmbient=RenderSettings.ambientSkyColor;oldEquator=RenderSettings.ambientEquatorColor;oldGround=RenderSettings.ambientGroundColor;
   yield return FadeTo(1);
   var load=SceneManager.LoadSceneAsync(SceneName,LoadSceneMode.Additive);if(load==null){IsEngaged=false;IsTransitioning=false;yield return FadeTo(0);yield break;}yield return load;
   var battle=SceneManager.GetSceneByName(SceneName);SceneManager.SetActiveScene(battle);
   foreach(var root in expedition.GetRootGameObjects())if(root!=gameObject&&root.activeSelf){hiddenRoots.Add(root);root.SetActive(false);}
   director.enabled=false;QualitySettings.antiAliasing=4;
   foreach(var root in battle.GetRootGameObjects()){Explorer=root.GetComponentInChildren<FirstPersonExplorer>(true);if(Explorer)break;}
   if(Explorer){Explorer.Automated=Array.Exists(Environment.GetCommandLineArgs(),x=>x.EndsWith("QA",StringComparison.OrdinalIgnoreCase));Explorer.Begin(this);if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-titanPreview"))Explorer.Look(0,16);}
   yield return FadeTo(0);IsTransitioning=false;Entered?.Invoke(Request);
  }
  public void ReturnToExpedition(){if(IsEngaged&&!IsTransitioning)StartCoroutine(ReturnRoutine());}
  IEnumerator ReturnRoutine(){
   InteractionFeel.Play(FeelCue.Portal,.65f);IsTransitioning=true;if(Explorer){if(Explorer.expedition)Explorer.expedition.Save();Explorer.Ready=false;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   yield return FadeTo(1);SceneManager.SetActiveScene(expedition);yield return SceneManager.UnloadSceneAsync(SceneName);
   foreach(var root in hiddenRoots)if(root)root.SetActive(true);hiddenRoots.Clear();director.enabled=originalDirectorEnabled;
   RenderSettings.fog=oldFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogDensity=oldFogDensity;RenderSettings.skybox=oldSky;RenderSettings.ambientLight=oldAmbient;QualitySettings.antiAliasing=oldAA;Time.timeScale=oldTimeScale;
   RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.fogMode=oldFogMode;RenderSettings.ambientSkyColor=oldSkyAmbient;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;
   Explorer=null;yield return FadeTo(0);IsEngaged=false;IsTransitioning=false;Returned?.Invoke();
  }
  void OnGUI(){if(fade<=0)return;var m=GUI.matrix;GUI.matrix=Matrix4x4.identity;GUI.depth=-500;GUI.color=new Color(.005f,.009f,.015f,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.matrix=m;}
 }
}
