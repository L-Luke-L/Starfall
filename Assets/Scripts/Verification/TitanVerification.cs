using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starfall {
 public sealed class TitanVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<string> times=new List<string>();string folder;int frame;bool recording;FirstPersonExplorer fps;SkyTitan titan;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-titanQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/titan-v11"));Directory.CreateDirectory(folder+"/frames");yield return new WaitForSeconds(.8f);
   var director=GetComponent<SceneDirector>();var travel=director.battle;int inventory=director.inventory.UnknownCount;travel.Enter(null);yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);fps=travel.Explorer;titan=UnityEngine.Object.FindFirstObjectByType<SkyTitan>();
   if(!fps||!titan){checks["titanLoaded"]=false;Finish();yield break;}
   UnityEngine.Object.FindFirstObjectByType<TitanAreaAttack>().AttackEnabled=false;
   foreach(var enemy in fps.arena.creatures){enemy.enabled=false;enemy.Agent.isStopped=true;}
   checks["singleSeparateGiant"]=UnityEngine.Object.FindObjectsByType<SkyTitan>(FindObjectsSortMode.None).Length==1&&titan.transform.localScale==Vector3.one;
   checks["sixSmallMonstersRetained"]=fps.arena.creatures.Length==6;
   var bounds=titan.VisualBounds;checks["wingspanExceeds200Meters"]=bounds.size.x>200;checks["bodyLengthExceeds160Meters"]=bounds.size.z>160;checks["toweringOverGround"]=bounds.max.y>75&&bounds.min.y>5;
   checks["independentArticulatedParts"]=titan.wings.Length==2&&titan.tail.Length==5&&titan.tendrils.Length==4&&titan.head!=titan.jaw&&titan.GetComponentsInChildren<MeshRenderer>().Length>120;
   yield return Capture("01-arrival-default-view");fps.Look(0,16);yield return Capture("02-colossus-over-basin");
   var planes=GeometryUtility.CalculateFrustumPlanes(fps.viewCamera);int visible=0;foreach(var r in titan.GetComponentsInChildren<MeshRenderer>())if(GeometryUtility.TestPlanesAABB(planes,r.bounds))visible++;
   checks["visibleFromArrival"]=visible>30;
   int hits=0;for(int y=5;y<10;y++)for(int x=1;x<12;x++){var ray=fps.viewCamera.ViewportPointToRay(new Vector3(x/12f,y/10f,0));if(Physics.Raycast(ray,out var hit,250)&&hit.collider.GetComponentInParent<SkyTitan>())hits++;}checks["real3DInteractionVolumes"]=hits>0;
   checks["largeShadowsEnabled"]=QualitySettings.shadowDistance>=240&&titan.wings[0].GetComponentInChildren<MeshRenderer>().shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off;
   float begin=titan.AnimationTime;var position=titan.transform.position;var wing=titan.wings[0].localRotation;var tendril=titan.tendrils[0].localRotation;
   recording=true;StartCoroutine(Record());yield return new WaitForSeconds(3);
   checks["slowIndependentFlightAndRigMotion"]=titan.AnimationTime>begin+2&&Vector3.Distance(position,titan.transform.position)>.1f&&Quaternion.Angle(wing,titan.wings[0].localRotation)>.1f&&Quaternion.Angle(tendril,titan.tendrils[0].localRotation)>.1f;
   fps.SetPaused(true);begin=titan.AnimationTime;position=titan.transform.position;wing=titan.wings[0].localRotation;yield return new WaitForSeconds(.6f);
   checks["pauseFreezesGiant"]=titan.AnimationTime==begin&&position==titan.transform.position&&wing==titan.wings[0].localRotation;
   checks["pauseStopsRoarAndBreathingAudio"]=!titan.voice.isPlaying&&!titan.breath.isPlaying;
   fps.SetPaused(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   yield return Wait(()=>titan.IsRoaring,10);yield return new WaitForSeconds(2.2f);checks["jawOpensDuringTimedRoar"]=titan.IsRoaring&&Quaternion.Angle(titan.jaw.localRotation,Quaternion.identity)>15&&titan.RoarCount==1;
   checks["generatedRoarClipAndAudioPlayback"]=titan.voice.clip&&titan.voice.clip.length>5&&titan.voice.isPlaying;
   Face(titan.head.position);yield return Capture("03-open-maw-roar");
   for(int i=0;i<100;i++){fps.Drive(Vector2.up,false,false,1/60f);yield return null;}
   checks["playerCanMoveBeneathGiant"]=fps.transform.position.z>-32&&fps.IsGrounded;
   Face(titan.head.position);yield return Capture("04-under-colossal-head");
   yield return new WaitForSeconds(1);recording=false;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,10);checks["returnUnloadsGiantAndAudio"]=UnityEngine.Object.FindObjectsByType<SkyTitan>(FindObjectsSortMode.None).Length==0&&!SceneManager.GetSceneByName(BattleTravel.SceneName).isLoaded;
   checks["inventoryAndMineRemainIntact"]=director.enabled&&director.inventory.UnknownCount==inventory;
   travel.Enter(null);yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);var next=UnityEngine.Object.FindFirstObjectByType<SkyTitan>();checks["reentryCreatesOneFreshGiant"]=next&&next.RoarCount==0&&UnityEngine.Object.FindObjectsByType<SkyTitan>(FindObjectsSortMode.None).Length==1;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,10);File.WriteAllLines(folder+"/frame-times.txt",times);Finish();
  }
  void Face(Vector3 point){var d=point-fps.viewCamera.transform.position;fps.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);fps.Look(0,fps.Pitch);fps.Look(0,Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
  IEnumerator Wait(Func<bool> condition,float duration){float end=Time.realtimeSinceStartup+duration;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture){File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);}}
  IEnumerator Record(){while(recording&&frame<140){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();if(texture){times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frame++).ToString("D4")+".png",texture.EncodeToPNG());Destroy(texture);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frame+"\n}";File.WriteAllText(folder+"/titan-report.json",json);Debug.Log(pass?"STARFALL_TITAN_QA_PASS":"STARFALL_TITAN_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
