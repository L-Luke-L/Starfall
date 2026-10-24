using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starfall {
 public sealed class BattleVerification:MonoBehaviour {
  string folder;readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<Task> writes=new List<Task>();readonly List<string> frameTimes=new List<string>();bool recording;int frames;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-battleQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/first-person-v9"));Directory.CreateDirectory(folder+"/frames");
   yield return new WaitForSeconds(1);var d=GetComponent<SceneDirector>();var travel=d.battle;var inv=d.inventory;
   var ore=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.RepairMaterial);
   d.AwardRecoveredOre(ore.GetComponent<BuriedSample>().Definition,99101);yield return d.Travel(true);Vector3 originalPlayer=d.player.position;Vector3 originalCamera=d.worldCamera.transform.position;
   var enemy=d.ores.Find(o=>o.GetComponent<BuriedSample>().Definition.payloadKind==BuriedPayloadKind.DormantEncounter);foreach(var o in d.ores)o.collected=true;enemy.collected=false;
   enemy.transform.position=d.winchAnchor.position+(d.hook.position-d.winchAnchor.position).normalized*5;checks["hookStartsEncounterCapture"]=d.Cast();
   float deadline=Time.realtimeSinceStartup+16;while(!d.encounter.IsPresenting&&Time.realtimeSinceStartup<deadline)yield return null;
   checks["exclamationBeforeTravel"]=d.encounter.IsPresenting&&!travel.IsEngaged;
   yield return WaitUntil(()=>travel.Explorer&&!travel.IsTransitioning,20);
   var fps=travel.Explorer;if(!fps){checks["battleLoaded"]=false;Finish();yield break;}
   checks["battleLoaded"]=SceneManager.GetActiveScene().name==BattleTravel.SceneName;
   checks["encounterContextPreserved"]=travel.Request!=null&&travel.Request.source==enemy.GetComponent<BuriedSample>().Definition;
   checks["hiddenEnemyNotAddedAsOre"]=inv.UnknownCount==1;
   checks["perspectiveCameraAndGroundController"]=!fps.viewCamera.orthographic&&fps.Motor.enabled;
   checks["originalSceneSuspended"]=!d.enabled&&!d.worldCamera.gameObject.activeInHierarchy&&!d.Cast();
   for(int i=0;i<35;i++){fps.Drive(Vector2.zero,false,false,1/60f);yield return null;}
   Vector3 spawn=fps.transform.position;checks["spawnLandsOnSurface"]=fps.IsGrounded;
   yield return Capture("01-first-person-arrival");recording=true;StartCoroutine(Record());
   for(int i=0;i<210;i++){fps.Look(.065f,0);fps.Drive(Vector2.up,false,false,1/60f);yield return null;}
   yield return Capture("02-basin-exploration");
   for(int i=0;i<120;i++){fps.Look(-.28f,-.03f);fps.Drive(new Vector2(.25f,.4f),false,false,1/60f);yield return null;}
   recording=false;
   Warp(fps,spawn);for(int i=0;i<60;i++){fps.Drive(Vector2.up,false,false,1/60f);yield return null;}float walk=HorizontalDistance(spawn,fps.transform.position);
   checks["walkMovesFreely"]=walk>3.8f&&walk<5.2f;
   Warp(fps,spawn);for(int i=0;i<60;i++){fps.Drive(Vector2.up,true,false,1/60f);yield return null;}float run=HorizontalDistance(spawn,fps.transform.position);checks["sprintIsFaster"]=run>walk*1.35f;
   Warp(fps,spawn);for(int i=0;i<60;i++){fps.Drive(Vector2.one,false,false,1/60f);yield return null;}checks["diagonalSpeedNormalized"]=Mathf.Abs(HorizontalDistance(spawn,fps.transform.position)-walk)<.4f;
   Warp(fps,spawn);for(int i=0;i<8;i++){fps.Drive(Vector2.zero,false,false,1/60f);yield return null;}float top=fps.transform.position.y;
   for(int i=0;i<90;i++){fps.Drive(Vector2.zero,false,i==0,1/60f);top=Mathf.Max(top,fps.transform.position.y);yield return null;}
   checks["jumpAndLand"]=top>spawn.y+.6f&&fps.IsGrounded&&Mathf.Abs(fps.transform.position.y-spawn.y)<.15f;
   // Jump momentum must remain in world space even when the player turns and reverses input.
   Warp(fps,spawn);fps.ResetMotion();
   for(int i=0;i<30;i++){fps.Drive(Vector2.up,false,false,1/60f);yield return null;}
   var takeoff=fps.transform.position;fps.Drive(Vector2.up,false,true,1/60f);yield return null;
   fps.Look(90,0);bool airborne=!fps.IsGrounded;float sideways=0;
   for(int i=0;i<90&&!fps.IsGrounded;i++){
    fps.Drive(Vector2.left,true,false,1/60f);sideways=Mathf.Max(sideways,Mathf.Abs(fps.transform.position.x-takeoff.x));yield return null;
   }
   checks["jumpKeepsWorldMomentumDespiteTurnAndInput"]=airborne&&fps.IsGrounded&&sideways<.15f&&fps.transform.position.z>takeoff.z+1;
   Warp(fps,spawn);fps.ResetMotion();
   for(int i=0;i<15;i++){fps.Drive(Vector2.zero,false,false,1/60f);yield return null;}
   var stationary=fps.transform.position;fps.Drive(Vector2.zero,false,true,1/60f);yield return null;
   for(int i=0;i<90&&!fps.IsGrounded;i++){fps.Drive(Vector2.one,true,false,1/60f);yield return null;}
   checks["stationaryJumpCannotAccelerateInAir"]=fps.IsGrounded&&HorizontalDistance(stationary,fps.transform.position)<.1f;
   Warp(fps,spawn);fps.Look(135,200);checks["mouseLookClampedPitch"]=fps.Pitch==-85&&Mathf.Abs(Mathf.DeltaAngle(fps.transform.eulerAngles.y,135))<1;
   fps.SetPaused(true);var held=fps.transform.position;float pitch=fps.Pitch;fps.Drive(Vector2.one,true,true,1);fps.Look(20,30);checks["pauseStopsMovementAndLook"]=held==fps.transform.position&&pitch==fps.Pitch;fps.SetPaused(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   Warp(fps,new Vector3(7,AlienSurfaceLandmarks.Height(7,-36)+.1f,-36));for(int i=0;i<150;i++){fps.Drive(Vector2.up,false,false,1/60f);yield return null;}
   checks["solidBoulderBlocksPlayer"]=fps.transform.position.z<-31.2f&&fps.transform.position.z>-35.5f;
   Warp(fps,new Vector3(0,AlienSurfaceLandmarks.Height(0,9)+.2f,9));for(int i=0;i<30;i++){fps.Drive(Vector2.zero,false,false,1/60f);yield return null;}checks["craterFloorSupportsPlayer"]=fps.IsGrounded;
   fps.Look(145,-13);yield return Capture("03-crater-and-rocks");
   travel.ReturnToExpedition();yield return WaitUntil(()=>!travel.IsEngaged,10);
   checks["returnRestoresMineCameraAndPosition"]=d.enabled&&d.worldCamera.gameObject.activeInHierarchy&&d.underground&&d.player.position==originalPlayer&&d.worldCamera.transform.position==originalCamera;
   checks["returnPreservesOreAndCollectedSample"]=inv.UnknownCount==1&&!enemy.gameObject.activeSelf&&enemy.collected;
   checks["battleSceneUnloaded"]=!SceneManager.GetSceneByName(BattleTravel.SceneName).isLoaded;
   yield return Capture("04-return-to-mine");travel.Enter(travel.Request);yield return WaitUntil(()=>travel.Explorer&&!travel.IsTransitioning,10);checks["secondEntryWorks"]=travel.Explorer&&travel.Explorer.Ready;
   travel.ReturnToExpedition();yield return WaitUntil(()=>!travel.IsEngaged,10);checks["secondReturnWorks"]=d.enabled&&!SceneManager.GetSceneByName(BattleTravel.SceneName).isLoaded&&inv.UnknownCount==1;
   while(writes.Exists(t=>!t.IsCompleted))yield return null;checks["recordingCompleted"]=!writes.Exists(t=>t.IsFaulted);Finish();
  }
  static float HorizontalDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
  static void Warp(FirstPersonExplorer f,Vector3 position){f.Motor.enabled=false;f.transform.position=position;f.transform.rotation=Quaternion.identity;f.Look(0,f.Pitch);f.Motor.enabled=true;Physics.SyncTransforms();}
  IEnumerator WaitUntil(Func<bool> predicate,float timeout){float until=Time.realtimeSinceStartup+timeout;while(!predicate()&&Time.realtimeSinceStartup<until)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+name+".png",t.EncodeToPNG());Destroy(t);}}
  IEnumerator Record(){while(recording&&frames<220){yield return new WaitForSecondsRealtime(.08f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){frameTimes.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));var px=t.GetPixels32();int w=t.width,h=t.height;string path=folder+"/frames/"+(frames++).ToString("D4")+".bmp";Destroy(t);writes.Add(Task.Run(()=>WriteFrame(path,px,w,h)));}}File.WriteAllLines(folder+"/frame-times.txt",frameTimes);}
  static void WriteFrame(string path,Color32[] px,int width,int height){using(var s=new MemoryStream(54+px.Length*4))using(var w=new BinaryWriter(s)){w.Write((ushort)0x4d42);w.Write(54+px.Length*4);w.Write(0);w.Write(54);w.Write(40);w.Write(width);w.Write(height);w.Write((ushort)1);w.Write((ushort)32);w.Write(0);w.Write(px.Length*4);w.Write(2835);w.Write(2835);w.Write(0);w.Write(0);foreach(var p in px){w.Write(p.b);w.Write(p.g);w.Write(p.r);w.Write(p.a);}File.WriteAllBytes(path,s.ToArray());}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frames+"\n}";File.WriteAllText(folder+"/battle-report.json",json);Debug.Log(pass?"STARFALL_BATTLE_QA_PASS":"STARFALL_BATTLE_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
