using System.Collections;
using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRegion {
  float eventClock,eventTime;Color eventFog;float fogBase;BasinResource[] resonanceVeins;GameObject powerBarrier;
  float scanVisibleUntil;
  public void Scan(){scanVisibleUntil=Time.time+4;campaign.Notice("拾光正在标记附近物体");}
  void DrawScanMarkers(){if(Time.time>scanVisibleUntil||!player||!player.viewCamera)return;foreach(var pair in sites){if(pair.Key=="wreck"||pair.Key.StartsWith("crate:")||!pair.Value||Vector3.Distance(player.transform.position,pair.Value.position)>25)continue;DrawMarker(pair.Value.position+Vector3.up*2);}
   foreach(var r in player.expedition.resources)if(r&&r.gameObject.activeInHierarchy&&!r.Depleted&&Vector3.Distance(player.transform.position,r.transform.position)<25)DrawMarker(r.transform.position+Vector3.up*1.5f);
  }
  void DrawMarker(Vector3 point){var screen=player.viewCamera.WorldToScreenPoint(point);if(screen.z<=0)return;InteractionFeel.Text(new Rect(screen.x-65,Screen.height-screen.y-17,130,34),"◇",player.uiFont,24,new Color(.45f,.88f,1,.8f),TextAnchor.MiddleCenter);}
  bool IsExpansionSite(string key)=>key.StartsWith("antenna-")||key.StartsWith("signal-")||key.StartsWith("sample-")||key.StartsWith("cache-")||key.StartsWith("relic-")||key=="winch"||key=="shelter"||key=="power"||key=="medical-door"||key=="crane";
  string ExpansionSiteName(string key){if(key.StartsWith("antenna-"))return "接通天线";if(key.StartsWith("signal-"))return "记录异常信号";if(key.StartsWith("sample-"))return "采集生态样本";if(key.StartsWith("cache-"))return "拆解物资箱";if(key.StartsWith("relic-"))return "收起星界遗物";if(key=="winch")return "解除绞盘";if(key=="shelter")return "开启避难舱";if(key=="power")return "重接电源";if(key=="medical-door")return "解除隔离";return "恢复吊机";}
  void Device(string key,Vector3 position,string name,Color tint,float height=1.4f){
   bool collected=key.StartsWith("sample-")||key.StartsWith("cache-")||key.StartsWith("relic-");bool complete=campaign.Exp(key);if(complete&&collected)return;var site=MakeSite(key,position,name);if(complete)sites.Remove(key);
   bool antenna=key.StartsWith("antenna-");bool sample=key.StartsWith("sample-");bool relic=key.StartsWith("relic-");if(sample||relic)height=.85f;
   Box(name+" body",site,new Vector3(0,height*.45f,0),new Vector3(antenna?.24f:sample?.45f:relic?.4f:.9f,height,antenna?.24f:.7f),steel,false);
   var lens=Box(name+" lens",site,new Vector3(0,height*.8f,-.37f),new Vector3(antenna?.85f:relic?.25f:.52f,relic?.38f:.24f,.06f),accent,false).GetComponent<Renderer>();
   if(antenna){var dish=Box(name+" angled aerial",site,new Vector3(0,height+.15f,0),new Vector3(1.25f,.12f,.6f),steel,false);dish.localRotation=Quaternion.Euler(-28,0,0);Box(name+" cross receiver",site,new Vector3(0,height+.44f,0),new Vector3(.09f,.6f,.09f),dark,false);}
   if(key.StartsWith("cache-")){Box(name+" lid",site,new Vector3(0,height+.03f,0),new Vector3(1.05f,.12f,.8f),dark,false);for(int side=-1;side<=1;side+=2)Box(name+" retention latch",site,new Vector3(side*.29f,height*.5f,-.38f),new Vector3(.08f,height,.08f),dark,false);}
   if(sample)Box(name+" sealed glass vial",site,new Vector3(0,height+.17f,0),new Vector3(.19f,.42f,.19f),accent,false);
   if(relic)lens.transform.localRotation=Quaternion.Euler(0,0,25);
   var lamp=new GameObject(name+" breathing signal").AddComponent<Light>();lamp.transform.SetParent(site,false);lamp.transform.localPosition=Vector3.up*(height+0.25f);lamp.color=tint;lamp.range=3;lamp.intensity=.75f;
   site.gameObject.AddComponent<RegionDeviceVisual>().Configure(player,lens,lamp,tint,complete);
  }
  void RefreshExpansionSite(string key){if(!campaign.Exp(key)||!sites.TryGetValue(key,out var site))return;sites.Remove(key);if(site){if(key.StartsWith("sample-")||key.StartsWith("cache-")||key.StartsWith("relic-"))Destroy(site.gameObject);else site.GetComponent<RegionDeviceVisual>().Complete();}focus=null;}
  void BuildExpansion(){
   var cyan=new Color(.31f,.83f,.86f);var amber=new Color(.95f,.55f,.21f);var blue=new Color(.36f,.64f,1);var violet=new Color(.73f,.48f,.95f);
   if(id==1){Device("antenna-0",new Vector3(-17,0,-18),"Signal mast A",cyan,2.4f);Device("antenna-1",new Vector3(18,0,-4),"Signal mast B",cyan,2.4f);Device("antenna-2",new Vector3(-13,0,18),"Signal mast C",cyan,2.4f);Device("relic-1",new Vector3(28,0,17),"Explorer star chart",amber);}
   if(id==2){Device("winch",new Vector3(-23,0,3),"Evacuation winch",amber);Device("shelter",new Vector3(-27,0,13),"Engineer shelter",cyan,2.1f);Device("signal-0",new Vector3(28,0,22),"Battlefield anomaly",violet);Device("cache-2-0",new Vector3(-33,0,-8),"Scavenged supply",amber);Device("cache-2-1",new Vector3(38,0,4),"Scavenged supply",amber);Device("relic-2",new Vector3(-42,0,28),"Evacuation token",amber);}
   if(id==3){Device("power",new Vector3(-22,0,-4),"Medical power switch",blue);Device("medical-door",new Vector3(-27,0,9),"Isolation lock",cyan,2.4f);Device("signal-1",new Vector3(29,0,-18),"Medical anomaly",violet);Device("sample-0",new Vector3(35,0,-5),"Biological sample A",cyan);Device("sample-1",new Vector3(-39,0,29),"Biological sample B",cyan);Device("cache-3-0",new Vector3(-34,0,-20),"Medical salvage",blue);Device("cache-3-1",new Vector3(35,0,32),"Medical salvage",blue);Device("relic-3",new Vector3(8,0,40),"Hospital nameplate",blue);}
   if(id==4){Device("crane",new Vector3(31,0,-8),"Dock crane control",amber,2.1f);Device("signal-2",new Vector3(-29,0,16),"Dock anomaly",violet);Device("sample-2",new Vector3(40,0,-17),"Biological sample C",cyan);Device("cache-4-0",new Vector3(-34,0,2),"Dock salvage",amber);Device("cache-4-1",new Vector3(44,0,34),"Dock salvage",amber);Device("relic-4",new Vector3(17,FieldSurface.Height(17,27,5),27),"Fleet insignia",amber);}
   if(id==5)Device("relic-5",new Vector3(-30,0,23),"D4 crystal record",violet);
   if(id==3)CreateResonanceVeins();
   if(id==4){var gate=Box("Power barrier · optional cargo shortcut",root.transform,new Vector3(35,AlienSurfaceLandmarks.Height(35,17)+1.5f,17),new Vector3(.3f,3,8),accent,true);powerBarrier=gate.gameObject;powerBarrier.SetActive(!campaign.Exp("crane-restored"));}
   if(id==2||id==4)SpawnElite(id==2);
   BuildRelays();
   eventFog=RenderSettings.fogColor;fogBase=RenderSettings.fogDensity;eventClock=32;
  }
  void SpawnElite(bool ground){
   var source=System.Array.Find(player.arena.creatures,c=>c&&c.kind==(ground?CreatureKind.RiftMauler:CreatureKind.SpineFloater));if(!source)return;
   var pos=ground?new Vector3(-43,0,32):new Vector3(44,0,30);pos.y=AlienSurfaceLandmarks.Height(pos.x,pos.z);
   if(UnityEngine.AI.NavMesh.SamplePosition(pos,out var hit,12,UnityEngine.AI.NavMesh.AllAreas))pos=hit.position;
   var g=Instantiate(source.gameObject,pos,Quaternion.identity,player.arena.transform);g.name=ground?"玄甲地龙 · 战场精英":"雷冠翼龙 · 船坞精英";
   var actor=g.GetComponent<AlienCreature>();actor.arena=player.arena;actor.SetHome(pos);actor.detectionRange=25;actor.leashRadius=31;actor.patrolRadius=6;
   var vitality=g.GetComponent<CreatureVitality>();vitality.persistentId=ground?"lost-realm-elite-ground":"lost-realm-elite-wing";vitality.maxHealth=ground?420:340;
   var elite=g.AddComponent<EliteCreature>();elite.Configure(actor,ground,player.arena,campaign);
   var list=new System.Collections.Generic.List<AlienCreature>(player.arena.creatures);list.Add(actor);player.arena.creatures=list.ToArray();player.arena.maxSimultaneousPursuers=3;
  }
  void CreateResonanceVeins(){if(player.expedition.resources.Length==0)return;resonanceVeins=new BasinResource[2];var source=player.expedition.resources[0];for(int i=0;i<2;i++){float x=i==0?31:-33,z=i==0?32:31;var g=Instantiate(source.gameObject,new Vector3(x,AlienSurfaceLandmarks.Height(x,z),z),Quaternion.identity,player.expedition.transform);g.name="Resonance cobalt seam "+i;var r=g.GetComponent<BasinResource>();r.id=1500+i;r.metal=RepairResource.CobaltSilicate;resonanceVeins[i]=r;g.SetActive(false);}var list=new System.Collections.Generic.List<BasinResource>(player.expedition.resources);list.AddRange(resonanceVeins);player.expedition.resources=list.ToArray();}
  void RelocateSavedLoot(){
   if(!UnityEngine.AI.NavMesh.SamplePosition(player.returnBeacon.position,out var start,8,UnityEngine.AI.NavMesh.AllAreas))return;var path=new UnityEngine.AI.NavMeshPath();int moved=0;
   foreach(var box in campaign.State.crates){if(box.region!=id)continue;if(UnityEngine.AI.NavMesh.SamplePosition(box.position,out var hit,5,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(start.position,hit.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){box.position=hit.position;continue;}box.region=0;box.position=Vector3.zero;moved++;}
   var progress=campaign.ProgressFor(id);foreach(var drop in progress.drops){if(drop.collected)continue;if(UnityEngine.AI.NavMesh.SamplePosition(drop.position,out var hit,5,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(start.position,hit.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete){drop.position=hit.position+Vector3.up*.6f;continue;}drop.position=start.position+new Vector3(2+moved*.6f,.6f,0);moved++;}
   if(moved>0)StartCoroutine(SaveRelocatedLoot());
  }
  IEnumerator SaveRelocatedLoot(){yield return new WaitUntil(()=>player&&player.expedition&&player.expedition.Progress!=null);campaign.Changed();}
  public bool ExpansionSitesReachable(){if(!UnityEngine.AI.NavMesh.SamplePosition(player.returnBeacon.position,out var start,8,UnityEngine.AI.NavMesh.AllAreas))return false;var path=new UnityEngine.AI.NavMeshPath();bool all=true;foreach(var site in sites){if((!IsExpansionSite(site.Key)&&!IsRelaySite(site.Key))||!site.Value)continue;if(!UnityEngine.AI.NavMesh.SamplePosition(site.Value.position,out var goal,6,UnityEngine.AI.NavMesh.AllAreas)||!UnityEngine.AI.NavMesh.CalculatePath(start.position,goal.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete){Debug.LogWarning("Unreachable region site "+id+" "+site.Key+" at "+site.Value.position);all=false;}}return all;}
  void TickRegionalEvent(){
   if(powerBarrier&&campaign.Exp("crane-restored"))powerBarrier.SetActive(false);
   if(id<2||id>4||!player.arena.Active)return;
   eventClock-=Time.deltaTime;
   if(eventClock<=0){eventClock=72;eventTime=10;campaign.Notice(id==2?"远方沙墙正在靠近":id==3?"结晶共鸣正在增强":"船坞电磁脉冲正在汇聚");InteractionFeel.Play(FeelCue.Alert,.55f);}
   if(eventTime>0){eventTime-=Time.deltaTime;if(eventTime>7)return;
    float strength=Mathf.Clamp01(Mathf.Min(7-eventTime,eventTime)/2);
    RenderSettings.fogColor=Color.Lerp(eventFog,id==2?new Color(.49f,.28f,.15f):id==3?new Color(.12f,.41f,.53f):new Color(.22f,.19f,.38f),strength*.65f);
    RenderSettings.fogDensity=fogBase+(id==2?.009f:.003f)*strength;
    bool active=eventTime>0&&eventTime<=7;if(resonanceVeins!=null)foreach(var vein in resonanceVeins)if(vein&&!vein.Depleted)vein.gameObject.SetActive(active||player.expedition.Focused==vein.transform);if(powerBarrier)powerBarrier.SetActive(!active&&!campaign.Exp("crane-restored"));
    if(eventTime<=0){RenderSettings.fogColor=eventFog;RenderSettings.fogDensity=fogBase;}
   }
  }
 }
}
