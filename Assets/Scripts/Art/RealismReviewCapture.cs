#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Starfall {
 [DefaultExecutionOrder(3000)]
 public sealed class RealismReviewCapture:MonoBehaviour {
  static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-artReview");
  static string Out=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../outputs/Starfall-Art-Fixes-Unity"));
  CampaignRuntime campaign;Camera camera;readonly List<string> notes=new List<string>();int errors;Action pendingCapture;readonly Dictionary<Transform,Quaternion> renderedRotations=new Dictionary<Transform,Quaternion>();
  void LateUpdate(){if(pendingCapture==null)return;var capture=pendingCapture;pendingCapture=null;capture();renderedRotations.Clear();foreach(var model in FindObjectsByType<RealismCreatureVisual>(FindObjectsSortMode.None))foreach(var bone in model.GetComponentsInChildren<Transform>())renderedRotations[bone]=bone.rotation;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Setup(){if(!Requested)return;SceneManager.sceneLoaded+=DisableOriginalQA;}
  static void DisableOriginalQA(Scene scene,LoadSceneMode mode){foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))if(b&&b.GetType().Name.EndsWith("Verification"))b.enabled=false;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(!Requested)return;var g=new GameObject("Art review only");DontDestroyOnLoad(g);g.AddComponent<RealismReviewCapture>();}
  void Error(string msg,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){if(stack.Contains("UnityEditor.Search.")){notes.Add("EDITOR_SEARCH_WARNING "+msg);return;}errors++;notes.Add("ERROR "+msg+"\n"+stack);}}
  IEnumerator Start(){Directory.CreateDirectory(Out);Application.logMessageReceived+=Error;yield return new WaitForSecondsRealtime(3);campaign=CampaignRuntime.Current;if(!campaign){Finish("Missing campaign");yield break;}
   foreach(int region in Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-artMotionOnly")?new[]{4}:new[]{1,2,3,4,5}){
    if(!campaign.State.coordinates.Contains(region))campaign.State.coordinates.Add(region);campaign.Close();campaign.EnterRegion(region);
    float end=Time.realtimeSinceStartup+45;while((!campaign.Explorer||campaign.director.battle.IsTransitioning)&&Time.realtimeSinceStartup<end)yield return null;
    notes.Add("Loaded scene: "+SceneManager.GetActiveScene().name);if(!campaign.Explorer){Finish("Region failed "+region);yield break;}
    var fps=campaign.Explorer;fps.Automated=true;fps.enabled=false;fps.SetPaused(false);fps.arena.SimulationEnabled=true;
    var cam=new GameObject("Review camera");camera=cam.AddComponent<Camera>();camera.CopyFrom(fps.viewCamera);camera.enabled=false;camera.transform.SetParent(null);camera.fieldOfView=55;
    foreach(var a in fps.arena.creatures)if(a){a.enabled=false;if(a.Agent&&a.Agent.isOnNavMesh){a.Agent.isStopped=true;a.Agent.ResetPath();}}
    var attack=FindFirstObjectByType<TitanAreaAttack>();if(attack){attack.Cancel();attack.AttackEnabled=false;}
    yield return new WaitForSecondsRealtime(4);notes.Add("Titan count="+FindObjectsByType<RealismTitanVisual>(FindObjectsSortMode.None).Length);foreach(var t in FindObjectsByType<SkyTitan>(FindObjectsInactive.Include,FindObjectsSortMode.None))notes.Add("TITAN "+t.name+" active="+t.gameObject.activeInHierarchy+" hp="+t.GetComponent<CreatureVitality>().Health+" ready="+(t.player&&t.player.Ready));foreach(var a in fps.arena.creatures)notes.Add("ACTOR "+a.name+" kind="+a.kind+" active="+a.gameObject.activeInHierarchy+" art="+a.GetComponentsInChildren<RealismCreatureVisual>(true).Length);
    if(region==1||region==4){var mast=GameObject.Find("Signal mast A");if(mast){var p=mast.transform.position;yield return Shot("01-antenna",p+new Vector3(3.1f,2.0f,-4.2f),p+Vector3.up*1.4f);var device=mast.GetComponent<RegionDeviceVisual>();device.SetCompleted(true);yield return Shot("02-antenna-active",p+new Vector3(-1.7f,1.45f,-2.2f),p+Vector3.up*1.2f);device.SetCompleted(false);notes.Add("Antenna completion signal toggled; original component retained");}
     var captured=new HashSet<CreatureKind>();foreach(var a in fps.arena.creatures){if(!a||!a.gameObject.activeInHierarchy||captured.Contains(a.kind))continue;var model=a.GetComponentInChildren<RealismCreatureVisual>();if(!model)continue;captured.Add(a.kind);var b=RealismAssets.Bounds(model.transform);string label=a.kind==CreatureKind.RiftMauler?"dinosaur":"wyvern";
      yield return Shot("03-"+label,b.center+a.transform.right*(b.size.magnitude*.55f)+a.transform.forward*(b.size.magnitude*.35f)+Vector3.up*.5f,b.center);
      var measuredHead=RealismAssets.Bone(model.transform,a.kind==CreatureKind.RiftMauler?"bn_Head":"head");var savedPlayerPosition=fps.transform.position;typeof(AlienCreature).GetProperty("State").SetValue(a,CreatureState.Pursue);Quaternion headStart=measuredHead&&renderedRotations.ContainsKey(measuredHead)?renderedRotations[measuredHead]:Quaternion.identity;for(int i=0;i<4;i++){fps.transform.position=a.transform.position+a.transform.forward*9+a.transform.right*(-6+i*4);yield return new WaitForSecondsRealtime(.45f);yield return Shot("motion-"+label+"-"+i,b.center+a.transform.right*(b.size.magnitude*.5f)+a.transform.forward*(b.size.magnitude*.45f)+Vector3.up*.5f,b.center);}
      fps.transform.position=savedPlayerPosition;notes.Add(label+" state="+a.State+" visualActive="+model.isActiveAndEnabled+" clock="+typeof(RealismCreatureVisual).GetField("clock",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(model)+" dt="+Time.deltaTime);notes.Add(label+" head="+(measuredHead?measuredHead.name:"MISSING")+" active="+a.arena.Active+" hp="+(a.GetComponent<CreatureVitality>()?a.GetComponent<CreatureVitality>().Health:-1)+" time="+Time.time);float headTurn=measuredHead?Quaternion.Angle(headStart,renderedRotations[measuredHead]):0;notes.Add(label+" head motion degrees="+headTurn);if(headTurn<3){errors++;notes.Add("ERROR head tracking did not respond: "+label);}notes.Add(label+" bounds="+b.size+" rootScale="+model.transform.lossyScale);
     }
     var titan=FindFirstObjectByType<RealismTitanVisual>();if(titan){var head=RealismAssets.Bone(titan.transform,"head");if(head){yield return Shot("04-ancient-head",head.position+new Vector3(26,9,-42),head.position);for(int i=0;i<4;i++){yield return new WaitForSecondsRealtime(.7f);yield return Shot("motion-ancient-"+i,head.position+new Vector3(26,9,-42),head.position);}}}
    }
    if(region==2){var structure=GameObject.Find("Collapsed hull");if(structure)yield return Shot("07-battlefield",structure.transform.position+new Vector3(7,3,-7),structure.transform.position);}
    if(region==5){var structure=GameObject.Find("D4 ring pylon 0");if(structure)yield return Shot("08-core",structure.transform.position+new Vector3(8,3,-10),structure.transform.position);}
    if(region==3){var habitat=GameObject.Find("Medical isolation wing 1");if(habitat)yield return Shot("05-medical-habitat",new Vector3(12,8,-1),new Vector3(0,2,13));}
    if(region==4){var support=GameObject.Find("Gantry foot 2");if(support)yield return Shot("06-dock-structure",support.transform.position+new Vector3(9,3,-10),support.transform.position+Vector3.up);}
    notes.Add("Region "+region+" reachable="+campaign.RegionView.ExpansionSitesReachable());Destroy(cam);fps.Return();end=Time.realtimeSinceStartup+30;while(campaign.director.battle.IsEngaged&&Time.realtimeSinceStartup<end)yield return null;yield return null;
   }Finish("Completed");
  }
  IEnumerator Shot(string name,Vector3 pos,Vector3 look){pendingCapture=()=>Capture(name,pos,look);while(pendingCapture!=null)yield return null;}
  void Capture(string name,Vector3 pos,Vector3 look){camera.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(look-pos));var rt=RenderTexture.GetTemporary(1600,1000,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(Path.Combine(Out,name+".png"),t.EncodeToPNG());Destroy(t);camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Debug.Log("ART_REVIEW_CAPTURE "+name);}
  void Finish(string status){notes.Add(status+" errors="+errors);File.WriteAllLines(Path.Combine(Out,"runtime-checks.txt"),notes);UnityEditor.EditorApplication.Exit(errors==0&&status=="Completed"?0:2);}
 }
}
#endif
