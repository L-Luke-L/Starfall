using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starfall {
 public sealed class DragonCombatVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<string> times=new List<string>();
  string folder;FirstPersonExplorer fps;TitanAreaAttack attack;CreatureArena arena;bool recording;int frame;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-dragonQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/dragon-v13"));Directory.CreateDirectory(folder+"/frames");yield return new WaitForSeconds(.8f);
   var d=GetComponent<SceneDirector>();int inventory=d.inventory.UnknownCount;d.battle.Enter(null);yield return Wait(()=>d.battle.Explorer&&!d.battle.IsTransitioning,18);
   fps=d.battle.Explorer;if(!fps){checks["loaded"]=false;Finish();yield break;}arena=fps.arena;attack=UnityEngine.Object.FindFirstObjectByType<TitanAreaAttack>();
   checks["sixDistributedDragons"]=arena.creatures.Length==6;float spacing=999;for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)spacing=Mathf.Min(spacing,CreatureArena.FlatDistance(arena.creatures[i].transform.position,arena.creatures[j].transform.position));checks["populationSpacingOver32Meters"]=spacing>=32;
   foreach(var c in arena.creatures){c.Agent.isStopped=true;c.enabled=false;}
   var titan=attack.titan;checks["giantFourLegsTwoWings"]=titan.tendrils.Length==4&&titan.wings.Length==2;int eyes=0;foreach(var t in titan.GetComponentsInChildren<Transform>())if(t.name.Contains("Single ancient dragon eye"))eyes++;checks["giantTwoEyes"]=eyes==2;
   checks["mouthSocketOnDragonHead"]=attack.muzzle.IsChildOf(titan.head);checks["giantWingspanOver200Meters"]=titan.VisualBounds.size.x>200;
   fps.Look(0,20);yield return Capture("01-eclipse-dragon");checks["safeBeaconDoesNotStartAttack"]=!attack.BeginAttack();
   Warp(new Vector3(0,0,0));Face(titan.head.position);int hp=arena.Health;checks["attackStartsOutsideBeacon"]=attack.BeginAttack();var center=attack.LockedCenter;
   recording=true;StartCoroutine(Record());yield return new WaitForSeconds(1);checks["warningDoesNotDamage"]=arena.Health==hp&&attack.Phase==1;
   Face(new Vector3(0,0,23));yield return Capture("02-terrain-warning");
   fps.SetPaused(true);float frozen=attack.PhaseTime;yield return new WaitForSeconds(.5f);checks["pauseFreezesAttack"]=attack.PhaseTime==frozen&&arena.Health==hp;fps.SetPaused(false);
   // Dodge through real CharacterController movement, not teleportation.
   fps.transform.rotation=Quaternion.identity;float end=Time.time+1.9f;while(Time.time<end){fps.Drive(Vector2.right,true,false,Time.deltaTime);yield return null;}
   checks["warningLocksFootprint"]=center==attack.LockedCenter;checks["runningReachesSafeCorridor"]=!attack.InBand(fps.transform.position,0)&&!attack.InBand(fps.transform.position,1)&&!attack.InBand(fps.transform.position,2);
   yield return Wait(()=>attack.IsFiring,6);Face(attack.muzzle.position);yield return Capture("03-mouth-breath");yield return new WaitForSeconds(.5f);Face(new Vector3(0,0,22));yield return Capture("04-ground-sweep");
   yield return Wait(()=>!attack.IsAttacking,8);checks["corridorAvoidsAllThreeBands"]=arena.Health==hp;checks["fullAttackFinishes"]=attack.ActiveBand==-1;recording=false;
   Warp(new Vector3(0,0,0));attack.BeginAttack();hp=arena.Health;yield return Wait(()=>attack.HitCount>0,7);checks["coveredGroundDeals65Damage"]=arena.Health==hp-65&&attack.HitCount==1;
   yield return new WaitForSeconds(.65f);checks["singleDamagePerAttack"]=arena.Health==hp-65;
   Warp(arena.beacon.position);hp=arena.Health;yield return Wait(()=>!attack.IsAttacking,7);checks["beaconRemainsProtected"]=arena.Health>=hp&&attack.HitCount==1;
   Warp(new Vector3(0,0,0));attack.BeginAttack();yield return new WaitForSeconds(.6f);arena.Damage(100,null);yield return null;checks["downedCancelsAreaAttack"]=!attack.IsAttacking;yield return Wait(()=>arena.Safe&&!arena.Downed,4);checks["deathReturnsSafely"]=arena.Health==100&&arena.Safe;
   d.battle.ReturnToExpedition();yield return Wait(()=>!d.battle.IsEngaged,12);checks["effectsUnloadWithBattle"]=!UnityEngine.Object.FindFirstObjectByType<TitanAreaAttack>()&&!SceneManager.GetSceneByName(BattleTravel.SceneName).isLoaded;checks["inventoryPreserved"]=d.inventory.UnknownCount==inventory;checks["wormholeStillUnlocked"]=d.wormhole.Unlocked;
   d.wormhole.TryEnter();yield return Wait(()=>d.battle.Explorer&&!d.battle.IsTransitioning,18);var next=UnityEngine.Object.FindFirstObjectByType<TitanAreaAttack>();checks["reentryResetsAttack"]=next&&next.AttackCount==0&&!next.IsAttacking;
   d.battle.ReturnToExpedition();yield return Wait(()=>!d.battle.IsEngaged,12);File.WriteAllLines(folder+"/frame-times.txt",times);Finish();
  }
  void Warp(Vector3 p){fps.Motor.enabled=false;fps.transform.position=new Vector3(p.x,AlienSurfaceLandmarks.Height(p.x,p.z)+.08f,p.z);fps.Motor.enabled=true;Physics.SyncTransforms();}
  void Face(Vector3 p){var v=p-fps.viewCamera.transform.position;fps.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg,0);fps.Look(0,fps.Pitch+Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg);}
  IEnumerator Wait(Func<bool> c,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!c()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Capture(string n){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+n+".png",t.EncodeToPNG());Destroy(t);}}
  IEnumerator Record(){while(recording&&frame<120){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frame++).ToString("D4")+".png",t.EncodeToPNG());Destroy(t);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frame+"\n}";File.WriteAllText(folder+"/dragon-report.json",json);Debug.Log(pass?"STARFALL_DRAGON_QA_PASS":"STARFALL_DRAGON_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
