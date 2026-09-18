using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public sealed class HardCombatVerification:MonoBehaviour {
  readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<string> times=new List<string>();
  FirstPersonExplorer fps;CreatureArena arena;PlayerArsenal gun;BasinExpedition expedition;TitanAreaAttack titanAttack;string folder;bool recording;int frames;
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-hardQA"))yield break;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/combat-v14"));Directory.CreateDirectory(folder+"/frames");yield return new WaitForSeconds(.8f);
   var director=GetComponent<SceneDirector>();director.battle.Enter(null);yield return Wait(()=>director.battle.Explorer&&!director.battle.IsTransitioning,18);fps=director.battle.Explorer;yield return new WaitForSeconds(.3f);
   if(!fps||!fps.expedition||fps.expedition.Progress==null){checks["combatLoads"]=false;Finish();yield break;}
   arena=fps.arena;gun=fps.arsenal;expedition=fps.expedition;titanAttack=FindFirstObjectByType<TitanAreaAttack>();titanAttack.AttackEnabled=false;
   foreach(var c in arena.creatures){c.enabled=false;c.Agent.isStopped=true;}
   checks["sixMonstersThreePursuers"]=arena.creatures.Length==6&&arena.maxSimultaneousPursuers==3;
   checks["expandedPursuitTerritories"]=arena.creatures[0].detectionRange==34&&arena.creatures[0].leashRadius==64;
   checks["twelveIndependentMineralNodes"]=expedition.resources.Length==12;
   checks["gunAndPickAreSeparateObjects"]=gun.rifle&&gun.tool&&gun.rifle!=gun.tool;
   checks["initialAmmunitionLimited"]=gun.Magazine==12&&gun.Reserve==60;
   checks["safeBeaconPreventsSniping"]=!gun.Attack()&&!gun.Attack(true);
   yield return Capture("01-hard-expedition");
   Warp(new Vector3(0,0,0));fps.transform.rotation=Quaternion.identity;float x=fps.transform.position.x;float end=Time.time+4.5f;
   while(Time.time<end){fps.Drive(Vector2.right,true,false,Time.deltaTime);yield return null;}
   checks["sprintingConsumesFiniteStamina"]=gun.Stamina<30&&fps.transform.position.x>x+10;
   yield return new WaitForSeconds(3);checks["staminaRecoversAfterRest"]=gun.Stamina>45;
   var enemy=arena.creatures[0];ResetEnemy(enemy,new Vector3(0,0,0));Warp(new Vector3(0,0,9));var vitality=enemy.GetComponent<CreatureVitality>();Face(enemy.transform.position+Vector3.up*1.55f);
   recording=true;StartCoroutine(Record());int hp=vitality.Health;int ammo=gun.Magazine;checks["primaryAttackFires"]=gun.Attack();checks["raycastDamagesMonster"]=vitality.Health==hp-26&&gun.Magazine==ammo-1;checks["fireCadenceBlocksSpam"]=!gun.Attack();yield return Capture("02-player-rifle-hit");
   yield return new WaitForSeconds(.35f);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA ballistic obstruction";wall.transform.position=(fps.transform.position+enemy.transform.position)*.5f+Vector3.up*1.5f;wall.transform.localScale=new Vector3(8,5,1);Physics.SyncTransforms();hp=vitality.Health;gun.Attack();checks["solidCoverBlocksPlayerShot"]=vitality.Health==hp;Destroy(wall);yield return new WaitForSeconds(.35f);
   Face(enemy.transform.position+Vector3.up*2.78f);hp=vitality.Health;gun.Attack();checks["headshotsRewardAim"]=hp-vitality.Health>26;
   yield return new WaitForSeconds(.35f);fps.SetPaused(true);ammo=gun.Magazine;checks["pauseBlocksAttacks"]=!gun.Attack()&&gun.Magazine==ammo;fps.SetPaused(false);
   Face(new Vector3(70,40,0));while(gun.Magazine>0){gun.Attack();yield return new WaitForSeconds(.3f);}checks["emptyMagazineCannotShoot"]=!gun.Attack();checks["emptyStartsReload"]=gun.ReloadRemaining>0;
   fps.SetPaused(true);float reload=gun.ReloadRemaining;yield return new WaitForSeconds(.5f);checks["pauseFreezesReload"]=gun.ReloadRemaining==reload;fps.SetPaused(false);int reserve=gun.Reserve;yield return Wait(()=>gun.ReloadRemaining==0,4);checks["reloadMovesReserveToMagazine"]=gun.Magazine==12&&gun.Reserve==reserve-12;
   Warp(enemy.transform.position+Vector3.forward*2.9f);Face(enemy.transform.position+Vector3.up*1.6f);hp=vitality.Health;float stamina=gun.Stamina;bool meleeAccepted=gun.Attack(true);checks["meleeWaitsForContact"]=meleeAccepted&&vitality.Health==hp;yield return new WaitForSeconds(.18f);checks["meleeAttackConnects"]=meleeAccepted&&vitality.Health==Mathf.Max(0,hp-44);checks["meleeCostsStamina"]=gun.Stamina<=stamina-22;yield return Capture("03-impact-pick");yield return new WaitForSeconds(.9f);
   Warp(new Vector3(0,0,8));Face(enemy.transform.position+Vector3.up*1.6f);while(!vitality.Dead){if(gun.Magazine==0)gun.Reload();gun.Attack();yield return new WaitForSeconds(.3f);}checks["deathDisablesNavigationAndCollision"]=!enemy.Agent.enabled&&!enemy.GetComponent<Collider>().enabled;int kills=expedition.Kills,dropCount=expedition.Progress.drops.Count;checks["deadEnemyCannotDuplicateRewards"]=!vitality.TakeDamage(500,enemy.transform.position)&&expedition.Kills==kills&&expedition.Progress.drops.Count==dropCount;yield return Capture("04-defeated-dragon");
   checks["lootTableHasNoDropAndFourRewards"]=BasinProgress.RollLoot(.9f)==BasinLootKind.None&&BasinProgress.RollLoot(.1f)==BasinLootKind.Fuel&&BasinProgress.RollLoot(.4f)==BasinLootKind.Ore&&BasinProgress.RollLoot(.55f)==BasinLootKind.Ammo&&BasinProgress.RollLoot(.67f)==BasinLootKind.Medkit;
   var pickup=expedition.AddDrop(BasinLootKind.Fuel,fps.transform.position+fps.transform.forward*1.5f);yield return null;int fuel=director.inventory.FuelCount;checks["fuelPickupReachesMainInventory"]=expedition.Collect(pickup)&&director.inventory.FuelCount==fuel+2;checks["pickupCannotCollectTwice"]=!expedition.Collect(pickup);
   pickup=expedition.AddDrop(BasinLootKind.Ammo,fps.transform.position+fps.transform.forward*1.5f);yield return null;reserve=gun.Reserve;checks["ammoDropRestoresReserve"]=expedition.Collect(pickup)&&gun.Reserve==reserve+18;
   arena.Damage(20,null);pickup=expedition.AddDrop(BasinLootKind.Medkit,fps.transform.position+fps.transform.forward*1.5f);yield return null;hp=arena.Health;checks["medicalDropHeals"]=expedition.Collect(pickup)&&arena.Health>hp;
   var node=expedition.resources[0];Warp(node.transform.position+Vector3.back*2.2f);Face(node.transform.position+Vector3.up*.65f);yield return new WaitForSeconds(.5f);int ore=director.inventory.UnknownCount;
   expedition.Mine(node,true,.8f);checks["miningRequiresSustainedHold"]=!node.Depleted&&expedition.MiningProgress>0&&director.inventory.UnknownCount==ore;
   arena.Damage(10,null);expedition.Mine(node,true,.1f);checks["damageInterruptsMining"]=expedition.MiningProgress==0;
   end=Time.time+7;while(Time.time<end&&!node.Depleted){expedition.Interact(true,Time.deltaTime);yield return null;}Debug.Log("Mining result depleted="+node.Depleted+" ores="+director.inventory.UnknownCount+" before="+ore+" progress="+expedition.MiningProgress);checks["miningAwardsOneForgeableOre"]=node.Depleted&&director.inventory.UnknownCount==ore+1;checks["depletedNodeCannotRepeat"]=!expedition.Mine(node,true,10)&&director.inventory.UnknownCount==ore+1;yield return Capture("05-mineral-harvest");
   var node2=expedition.resources[1];Warp(node2.transform.position+Vector3.back*2.2f);Face(node2.transform.position+Vector3.up*.7f);expedition.Mine(node2,true,.8f);fps.SetPaused(true);float mining=expedition.MiningProgress;yield return new WaitForSeconds(.3f);checks["pauseCancelsMining"]=expedition.MiningProgress==0;fps.SetPaused(false);
   expedition.MiningProgressReset();expedition.Tell(null);expedition.Interact(false,Time.deltaTime);yield return Capture("06-unmined-seam");recording=false;
   end=Time.time+7;while(Time.time<end&&!node2.Depleted){expedition.Interact(true,Time.deltaTime);yield return null;}
   bool firstForge=director.inventory.TryReserve(out var firstOre)&&director.inventory.Resolve(firstOre,true);
   bool secondForge=director.inventory.TryReserve(out var secondOre)&&secondOre.Metal==node2.metal&&director.inventory.Resolve(secondOre,true);
   checks["gatheredOreKeepsItsMetalThroughForging"]=firstForge&&secondForge&&director.inventory.MetalCount(node2.metal)>0;
   // Actual tougher AI attacks, isolated from the other five patrols.
   var ground=arena.creatures[2];ground.gameObject.SetActive(true);ResetEnemy(ground,new Vector3(0,0,0));ground.enabled=true;Warp(new Vector3(0,0,12));arena.RestoreHealth(100);int charge=ground.Charges;yield return Wait(()=>ground.State==CreatureState.ChargeTell,8);hp=arena.Health;yield return Wait(()=>arena.Health<hp,5);checks["hardChargeDeals44"]=arena.Health==hp-44&&ground.Charges>charge;ground.enabled=false;ground.Agent.isStopped=true;ground.gameObject.SetActive(false);
   var flyer=arena.creatures[1];ResetEnemy(flyer,new Vector3(0,0,0));flyer.enabled=true;Warp(new Vector3(0,0,14));arena.RestoreHealth(100);int shots=flyer.Shots;yield return Wait(()=>flyer.Shots>=shots+5,8);checks["wyvernFiresFiveShotVolley"]=flyer.Shots>=shots+5;yield return Wait(()=>arena.Health<100,3);checks["wyvernBoltDeals19"]=arena.Health<=81;flyer.enabled=false;flyer.Agent.isStopped=true;arena.ClearBolts();
   Warp(new Vector3(0,0,0));arena.RestoreHealth(100);yield return new WaitForSeconds(.5f);titanAttack.AttackEnabled=true;titanAttack.BeginAttack();checks["giantShorterReadableWarning"]=TitanAreaAttack.WarningSeconds==3.2f&&titanAttack.Phase==1;yield return Wait(()=>titanAttack.HitCount>0,6);checks["giantAttackDeals65"]=arena.Health==35;titanAttack.Cancel();titanAttack.AttackEnabled=false;
   var titan=titanAttack.GetComponent<CreatureVitality>();checks["giantHasDamageableHealth"]=titan.Health==1800&&titan.TakeDamage(26,titan.transform.position)&&titan.Health==1774;
   Warp(new Vector3(0,0,0));yield return new WaitForSeconds(.5f);arena.Damage(100,null);yield return Wait(()=>arena.Safe&&!arena.Downed,4);checks["deathRecoversWithoutRevivingKilledMonster"]=arena.Health==100&&vitality.Dead&&!enemy.Agent.enabled;
   Warp(new Vector3(0,0,0));arena.Damage(20,null);hp=arena.Health;ammo=gun.Magazine;reserve=gun.Reserve;ore=director.inventory.UnknownCount;fuel=director.inventory.FuelCount;
   director.battle.ReturnToExpedition();yield return Wait(()=>!director.battle.IsEngaged,12);checks["mainSceneKeepsOreAndFuel"]=director.inventory.UnknownCount==ore&&director.inventory.FuelCount==fuel;
   director.wormhole.TryEnter();yield return Wait(()=>director.battle.Explorer&&!director.battle.IsTransitioning,18);yield return new WaitForSeconds(.3f);fps=director.battle.Explorer;checks["wormholeDoesNotRefillAmmoOrHealth"]=fps.arsenal.Magazine==ammo&&fps.arsenal.Reserve==reserve&&fps.arena.Health==hp;checks["wormholeKeepsDefeatedAndMinedState"]=fps.arena.creatures[0].GetComponent<CreatureVitality>().Dead&&fps.expedition.resources[0].Depleted;checks["giantWoundsPersist"]=FindFirstObjectByType<SkyTitan>().GetComponent<CreatureVitality>().Health==1774;
   director.battle.ReturnToExpedition();yield return Wait(()=>!director.battle.IsEngaged,12);File.WriteAllLines(folder+"/frame-times.txt",times);Finish();
  }
  void Warp(Vector3 p){fps.Motor.enabled=false;fps.transform.position=new Vector3(p.x,AlienSurfaceLandmarks.Height(p.x,p.z)+.08f,p.z);fps.Motor.enabled=true;fps.ResetMotion();fps.Drive(Vector2.zero,false,false,.016f);Physics.SyncTransforms();}
  void ResetEnemy(AlienCreature c,Vector3 p){c.ResetToHome();if(NavMesh.SamplePosition(new Vector3(p.x,AlienSurfaceLandmarks.Height(p.x,p.z),p.z),out var h,6,NavMesh.AllAreas))c.Agent.Warp(h.position);c.Agent.isStopped=true;c.transform.rotation=Quaternion.identity;Physics.SyncTransforms();}
  void Face(Vector3 p){var v=p-fps.viewCamera.transform.position;fps.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg,0);fps.Look(0,fps.Pitch+Mathf.Atan2(v.y,new Vector2(v.x,v.z).magnitude)*Mathf.Rad2Deg);}
  IEnumerator Wait(Func<bool> c,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!c()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Capture(string n){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){File.WriteAllBytes(folder+"/"+n+".png",t.EncodeToPNG());Destroy(t);}}
  IEnumerator Record(){while(recording&&frames<160){yield return new WaitForSecondsRealtime(.16f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t){times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllBytes(folder+"/frames/"+(frames++).ToString("D4")+".png",t.EncodeToPNG());Destroy(t);}}}
  void Finish(){bool pass=true;string json="{\n";foreach(var c in checks){pass&=c.Value;json+="  \""+c.Key+"\":"+(c.Value?"true":"false")+",\n";}json+="  \"passed\":"+(pass?"true":"false")+",\n  \"recordedFrames\":"+frames+"\n}";File.WriteAllText(folder+"/hard-report.json",json);Debug.Log(pass?"STARFALL_HARD_QA_PASS":"STARFALL_HARD_QA_FAIL");Application.Quit(pass?0:2);}
 }
}
