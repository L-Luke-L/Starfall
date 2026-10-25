using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Starfall {
 public sealed class CreatureVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();string folder;FirstPersonExplorer fps;CreatureArena arena;AlienCreature ground,flyer;bool recording;int frame;readonly List<string> times=new List<string>();
  readonly StringBuilder combatLog=new StringBuilder();
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-creatureQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/creatures-v10"));Directory.CreateDirectory(folder+"/frames");
   yield return new WaitForSeconds(.8f);var director=GetComponent<SceneDirector>();var travel=director.battle;int inventory=director.inventory.UnknownCount;travel.Enter(null);
   yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);fps=travel.Explorer;
   if(!fps||!fps.arena){checks["arenaLoaded"]=false;Finish();yield break;}
   UnityEngine.Object.FindFirstObjectByType<TitanAreaAttack>().AttackEnabled=false;
   arena=fps.arena;arena.ProjectileHit+=ProjectileHit;ground=arena.creatures[0];flyer=arena.creatures[1];
   // Isolate one representative of each species for attack checks; population QA covers all six together.
   for(int i=2;i<arena.creatures.Length;i++)arena.creatures[i].gameObject.SetActive(false);
   checks["twoDistinctIndependentMonsters"]=ground.kind!=flyer.kind&&ground.visual!=flyer.visual&&ground.limbs.Length==2&&flyer.limbs.Length==2&&flyer.fins.Length==2;
   yield return new WaitForSeconds(.5f);checks["bothOnBakedNavigation"]=ground.Agent.isOnNavMesh&&flyer.Agent.isOnNavMesh;
   Vector3 a=ground.transform.position,b=flyer.transform.position;float groundTravel=0,flyerTravel=0,patrolEnd=Time.realtimeSinceStartup+5;
   while(Time.realtimeSinceStartup<patrolEnd){yield return null;groundTravel+=CreatureArena.FlatDistance(a,ground.transform.position);flyerTravel+=CreatureArena.FlatDistance(b,flyer.transform.position);a=ground.transform.position;b=flyer.transform.position;}
   checks["bothPatrolFreely"]=groundTravel>2&&flyerTravel>2;
   Debug.Log("Patrol distance: ground="+groundTravel+", flyer="+flyerTravel);
   checks["arrivalBeaconPreventsDamage"]=arena.Safe&&!arena.Damage(20,ground)&&arena.Health==100;
   var path=new NavMeshPath();NavMesh.SamplePosition(new Vector3(7,0,-36),out var pathStart,8,NavMesh.AllAreas);NavMesh.SamplePosition(new Vector3(7,0,-24),out var pathEnd,8,NavMesh.AllAreas);
   checks["navigationRoutesAroundSolidRock"]=NavMesh.CalculatePath(pathStart.position,pathEnd.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete&&path.corners.Length>2;
   flyer.gameObject.SetActive(false);Reset(ground,new Vector3(0,0,0));WarpPlayer(new Vector3(0,0,12));Face(ground.transform.position+Vector3.up);
   recording=true;StartCoroutine(Record());yield return Wait(()=>ground.State==CreatureState.ChargeTell,10);
   checks["maulerTelegraphsCharge"]=ground.State==CreatureState.ChargeTell;yield return Capture("01-mauler-charge-tell");
   int hp=arena.Health,attacks=ground.Charges;WarpPlayer(new Vector3(6,0,12));Face(ground.transform.position+Vector3.up);yield return new WaitForSeconds(2);
   checks["committedChargeCanBeDodged"]=ground.Charges>attacks&&arena.Health==hp;
   Reset(ground,new Vector3(0,0,0));WarpPlayer(new Vector3(0,0,12));Face(ground.transform.position+Vector3.up);hp=arena.Health;attacks=ground.Charges;
   yield return Wait(()=>arena.Health<hp,8);checks["maulerChargeDamagesPlayer"]=arena.Health<hp&&ground.Charges>attacks;yield return Capture("02-mauler-impact");
   Reset(ground,new Vector3(0,0,8));WarpPlayer(new Vector3(0,0,10));hp=arena.Health;yield return Wait(()=>ground.State==CreatureState.BiteTell,7);checks["closeRangeBiteTelegraph"]=ground.State==CreatureState.BiteTell;
   yield return Wait(()=>arena.Health<hp,3);checks["closeRangeBiteDamagesPlayer"]=arena.Health<hp;
   arena.RestoreHealth(100);ground.gameObject.SetActive(false);flyer.gameObject.SetActive(true);Reset(flyer,new Vector3(0,0,0));WarpPlayer(new Vector3(0,0,14));Face(flyer.transform.position);hp=arena.Health;
   checks["stationaryPlayerHasNoTeleportVelocity"]=fps.Motor.velocity.sqrMagnitude<.0001f;VolleySnapshot("stationary setup");
   yield return Wait(()=>flyer.State==CreatureState.VolleyTell,10);checks["floaterTelegraphsVolley"]=flyer.State==CreatureState.VolleyTell;Face(flyer.transform.position);yield return Capture("03-floater-volley-tell");int shots=flyer.Shots;
   yield return Wait(()=>flyer.Shots>shots,2);VolleySnapshot("first shot");
   yield return Wait(()=>flyer.Shots>=shots+5,3);checks["wyvernFiresFiveShotBurst"]=flyer.Shots==shots+5;yield return Capture("04-plasma-volley");
   yield return Wait(()=>arena.Health<hp,3);checks["plasmaDamagesPlayer"]=arena.Health<hp;VolleySnapshot("volley result");
   checks["floaterStaysAboveGround"]=flyer.transform.position.y-AlienSurfaceLandmarks.Height(flyer.transform.position.x,flyer.transform.position.z)>2.4f;
   Reset(flyer,new Vector3(0,0,0));WarpPlayer(new Vector3(0,0,4));float before=CreatureArena.FlatDistance(flyer.transform.position,fps.transform.position);yield return new WaitForSeconds(1);
   checks["floaterRetreatsAtCloseRange"]=CreatureArena.FlatDistance(flyer.transform.position,fps.transform.position)>before+1;
   var bolt=arena.Fire(flyer,new Vector3(0,10,0),new Vector3(20,10,0));fps.SetPaused(true);a=flyer.transform.position;b=bolt.transform.position;hp=arena.Health;yield return new WaitForSeconds(.6f);
   checks["pauseFreezesMonstersProjectilesAndDamage"]=flyer.transform.position==a&&bolt.transform.position==b&&!arena.Damage(20,flyer)&&arena.Health==hp;
   Debug.Log("Pause drift: actor="+Vector3.Distance(a,flyer.transform.position)+", bolt="+Vector3.Distance(b,bolt.transform.position));
   fps.SetPaused(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;arena.ClearBolts();recording=false;
   flyer.gameObject.SetActive(false);WarpPlayer(new Vector3(45,0,48));var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA line-of-sight wall";wall.transform.position=new Vector3(45,fps.transform.position.y+1,44);wall.transform.localScale=new Vector3(6,5,1);Physics.SyncTransforms();
   hp=arena.Health;int blocked=arena.BlockedProjectiles;var origin=new Vector3(45,fps.transform.position.y+1,40);checks["wallBlocksLineOfSight"]=!arena.ClearSight(origin,fps.transform.position+Vector3.up,flyer);
   arena.Fire(flyer,origin,fps.transform.position+Vector3.up);yield return new WaitForSeconds(.8f);checks["wallStopsProjectileBeforePlayer"]=arena.BlockedProjectiles>blocked&&arena.Health==hp;
   blocked=arena.BlockedProjectiles;var ownerless=arena.Fire(null,origin,fps.transform.position+Vector3.up);var visualCollider=ownerless.GetComponent<Collider>();checks["projectileVisualHasNoActiveCollider"]=!visualCollider||!visualCollider.enabled;
   ownerless.Step(.8f);checks["ownerlessProjectileHitsWorld"]=ownerless.HitWorld&&arena.BlockedProjectiles==blocked+1;ownerless.Step(.8f);checks["resolvedProjectileHitsOnlyOnce"]=arena.BlockedProjectiles==blocked+1;
   blocked=arena.BlockedProjectiles;var embedded=arena.Fire(flyer,wall.transform.position,fps.transform.position+Vector3.up);embedded.Step(.016f);checks["projectileStartingInCoverIsBlocked"]=embedded.HitWorld&&arena.BlockedProjectiles==blocked+1;checks["overlappingProjectileImpactUsesContactPoint"]=Vector3.Distance(embedded.HitPosition,wall.transform.position)<.1f;Destroy(wall);
   yield return new WaitForSeconds(.4f);hp=arena.Health;var playerTarget=fps.transform.position+Vector3.up;arena.Fire(null,playerTarget+Vector3.forward*.7f,playerTarget).Step(.1f);checks["ownerlessProjectileHitsPlayer"]=arena.Health<hp;
   yield return new WaitForSeconds(.4f);fps.Motor.Move(Vector3.down*.2f);fps.SendMessage("Dash",Vector2.up);var dashStart=fps.transform.position;fps.Drive(Vector2.zero,false,false,.02f);bool dashed=CreatureArena.FlatDistance(dashStart,fps.transform.position)>.15f;
   fps.ResetMotion();dashStart=fps.transform.position;fps.Drive(Vector2.zero,false,false,.02f);checks["resetMotionCancelsRemainingDash"]=dashed&&CreatureArena.FlatDistance(dashStart,fps.transform.position)<.01f;
   yield return new WaitForSeconds(.6f);int recovered=arena.RespawnCount;arena.Damage(100,ground);checks["zeroHealthStartsRecovery"]=arena.Downed;
   yield return Wait(()=>arena.RespawnCount>recovered,3);checks["recoveryReturnsToSafeBeacon"]=arena.Health==100&&arena.Safe&&arena.RespawnCount==recovered+1;
   ground.gameObject.SetActive(true);flyer.gameObject.SetActive(true);Reset(ground,new Vector3(-3,0,5));Reset(flyer,new Vector3(4,0,7));WarpPlayer(new Vector3(0,0,-5));yield return null;arena.SimulationEnabled=false;ground.transform.rotation=Quaternion.Euler(0,180,0);flyer.transform.rotation=Quaternion.Euler(0,180,0);Face(new Vector3(0,1.2f,6));yield return Capture("05-two-creatures");arena.SimulationEnabled=true;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,10);checks["returnUnloadsActorsAndProjectiles"]=!SceneManager.GetSceneByName(BattleTravel.SceneName).isLoaded&&UnityEngine.Object.FindObjectsByType<AlienCreature>(FindObjectsSortMode.None).Length==0;
   checks["returnPreservesInventory"]=director.inventory.UnknownCount==inventory;
   travel.Enter(null);yield return Wait(()=>travel.Explorer&&!travel.IsTransitioning,15);checks["reentryRestoresSixFreshMonsters"]=travel.Explorer&&travel.Explorer.arena&&travel.Explorer.arena.creatures.Length==6&&travel.Explorer.arena.Health==100;
   travel.ReturnToExpedition();yield return Wait(()=>!travel.IsEngaged,10);while(recording)yield return null;File.WriteAllLines(folder+"/frame-times.txt",times);Finish();
  }
  void Reset(AlienCreature c,Vector3 p){c.ResetToHome();if(NavMesh.SamplePosition(new Vector3(p.x,AlienSurfaceLandmarks.Height(p.x,p.z),p.z),out var hit,8,NavMesh.AllAreas)){c.Agent.Warp(hit.position);c.transform.rotation=Quaternion.identity;}Physics.SyncTransforms();}
  void WarpPlayer(Vector3 p){
   fps.ResetMotion();fps.Motor.enabled=false;fps.transform.position=new Vector3(p.x,AlienSurfaceLandmarks.Height(p.x,p.z)+.08f,p.z);fps.Motor.enabled=true;
   // Automated players do not run Drive each frame. Settle the capsule, then clear
   // the artificial movement velocity so a volley predicts a stationary target.
   fps.Motor.Move(Vector3.down*.2f);fps.Motor.Move(Vector3.zero);Physics.SyncTransforms();
  }
  void VolleySnapshot(string phase){var line="CREATURE_VOLLEY "+phase+" time="+Time.time.ToString("F3")+" player="+fps.transform.position.ToString("F3")+" velocity="+fps.Motor.velocity.ToString("F3")+" mouth="+flyer.mouth.position.ToString("F3")+" aim="+flyer.VolleyAim.ToString("F3")+" state="+flyer.State+" shots="+flyer.Shots+" health="+arena.Health+" blocked="+arena.BlockedProjectiles+" active="+arena.Active+" safe="+arena.Safe;combatLog.AppendLine(line);Debug.Log(line);}
  void ProjectileHit(AlienBolt bolt,Collider collider,bool damaged){combatLog.AppendLine("PROJECTILE_HIT time="+Time.time.ToString("F3")+" collider="+collider.name+" player="+(collider.GetComponentInParent<FirstPersonExplorer>()==fps)+" damageApplied="+damaged+" position="+bolt.HitPosition.ToString("F3")+" velocity="+bolt.velocity.ToString("F3")+" health="+arena.Health+" active="+arena.Active+" safe="+arena.Safe);}
  void OnDestroy(){if(arena)arena.ProjectileHit-=ProjectileHit;}
  void Face(Vector3 point){Vector3 d=point-fps.viewCamera.transform.position;fps.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);fps.Look(0,fps.Pitch);fps.Look(0,Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
  IEnumerator Wait(Func<bool> condition,float duration){float deadline=Time.realtimeSinceStartup+duration;while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+name+".png",t.EncodeToPNG());Destroy(t);}}
  IEnumerator Record(){while(recording&&frame<160){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frame++).ToString("D4")+".png",t.EncodeToPNG());Destroy(t);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frame+"\n}";File.WriteAllText(folder+"/creature-report.json",json);File.WriteAllText(folder+"/combat-diagnostics.txt",combatLog.ToString());Debug.Log(pass?"STARFALL_CREATURE_QA_PASS":"STARFALL_CREATURE_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
