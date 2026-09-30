using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public sealed partial class CampaignRegion {
  Transform surveyArt,ruinArt;Transform[] ruinControls,ruinIndicators;Renderer[] memoryLights;MaterialPropertyBlock frontierTint;
  float surveyTime,memoryClock;int surveyDamage,memoryStep;Vector3 surveyOrigin;bool surveyStarted,memoryPlayback;
  public float SurveyProgress=>surveyTime/3;public bool MemoryPlaying=>memoryPlayback;public int MemoryStep=>memoryStep;
  public bool RuinReady {get {if(!AtRuin)return false;var definition=FrontierCatalog.Current.RuinFor(id);if(definition==null)return false;if(id==3)return memoryStep==definition.solution.Length;var record=campaign.RuinFor(id);for(int i=0;i<3;i++)if(record.values[i]!=definition.solution[i])return false;return true;}}
  public bool AtRuin {get {if(ruinControls==null)return false;for(int i=0;i<ruinControls.Length;i++)if(Near("ruin-control-"+i))return true;return false;}}
  bool IsFrontierSite(string key)=>key=="survey"||key=="ruin-replay"||key.StartsWith("ruin-control-");
  Vector3 FrontierAnchor(Vector3 preferred,int controls){var start=player.returnBeacon.position;var path=new NavMeshPath();for(int attempt=0;attempt<49;attempt++){float angle=(attempt%8)*Mathf.PI/4;float radius=attempt==0?0:(1+(attempt-1)/8)*2;var probe=preferred+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);probe.y=FieldSurface.Height(probe.x,probe.z,AlienSurfaceLandmarks.Height(probe.x,probe.z));if(!NavMesh.SamplePosition(probe,out var center,1.5f,NavMesh.AllAreas))continue;bool good=NavMesh.CalculatePath(start,center.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
    for(int i=0;i<controls&&good;i++){var target=center.position+new Vector3((i-(controls-1)*.5f)*2,0,0);if(!NavMesh.SamplePosition(target,out var nav,.8f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-center.position.y)>.45f||!NavMesh.CalculatePath(start,nav.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){good=false;break;}if(Physics.CheckCapsule(nav.position+Vector3.up*.5f,nav.position+Vector3.up*1.5f,.28f,~0,QueryTriggerInteraction.Ignore))good=false;}
    if(good)return center.position;
   }Debug.LogWarning("Frontier landmark uses reachable entrance fallback "+id);return FieldSurface.ReachableDrop(start+Vector3.right*5,start)-Vector3.up*.6f;
  }
  string FrontierSiteName(string key)=>key=="ruin-replay"?"重放结晶记忆":key=="survey"?(campaign.Exp("survey-"+id)?"查看勘测记录":"长按 · 勘测三秒"):campaign.RuinFor(id).solved?"遗迹已开启":id==2?"转动方向盘":id==3?"触碰结晶":"调节配平电容";
  void BuildFrontier(){
   var catalog=FrontierCatalog.Current;if(!catalog)return;frontierTint=new MaterialPropertyBlock();
   var landmark=catalog.LandmarkFor(id);if(landmark!=null){surveyArt=MakeSite("survey",FrontierAnchor(landmark.position,1),landmark.title);Box("Survey tripod center",surveyArt,new Vector3(0,.55f,0),new Vector3(.16f,1.1f,.16f),steel,false);for(int leg=0;leg<3;leg++){var p=Box("Independent survey tripod leg",surveyArt,new Vector3(0,.34f,0),new Vector3(.055f,.77f,.055f),steel,false);p.localRotation=Quaternion.Euler(20,leg*120,0);}Box("Survey optic housing",surveyArt,new Vector3(0,1.24f,0),new Vector3(.65f,.3f,.42f),dark,false);Box("Survey optic aperture",surveyArt,new Vector3(0,1.25f,-.23f),new Vector3(.42f,.18f,.035f),accent,false);var ring=Box("Survey independent gimbal",surveyArt,new Vector3(0,1.65f,0),new Vector3(.8f,.07f,.8f),steel,false);ring.localRotation=Quaternion.Euler(0,25,18);}
   var ruin=catalog.RuinFor(id);if(ruin!=null){int count=id==3?4:3;ruinArt=MakeSite("ruin-center",FrontierAnchor(ruin.position,count),ruin.title);sites.Remove("ruin-center");ruinControls=new Transform[count];ruinIndicators=new Transform[count];memoryLights=new Renderer[count];
    Box("Ruin buried footing",ruinArt,new Vector3(0,.12f,0),new Vector3(count*2+.9f,.24f,1.5f),dark,false);
    for(int i=0;i<count;i++){var local=new Vector3((i-(count-1)*.5f)*2,0,0);var pad=MakeSite("ruin-control-"+i,ruinArt.position+local,"Independent ruin control "+i);pad.SetParent(ruinArt,true);ruinControls[i]=pad;Box("Ruin socket",pad,new Vector3(0,.42f,0),new Vector3(.9f,.84f,.9f),steel,false);
     ruinIndicators[i]=new GameObject("Independent clue response "+i).transform;ruinIndicators[i].SetParent(pad,false);ruinIndicators[i].localPosition=Vector3.up*1.04f;
     if(id==2){Arrow(ruinIndicators[i],accent);var clue=new GameObject("Physical orientation clue "+i).transform;clue.SetParent(ruinArt,false);clue.localPosition=local+new Vector3(0,1.02f,1.3f);clue.localRotation=Quaternion.Euler(0,ruin.solution[i]*90,0);Arrow(clue,steel);Box("Clue pedestal",clue,new Vector3(0,-.45f,0),new Vector3(.14f,.9f,.14f),steel,false);}
     else if(id==3){memoryLights[i]=Box("Memory crystal "+i,ruinIndicators[i],Vector3.up*.08f,new Vector3(.45f,.72f,.45f),accent,false).GetComponent<Renderer>();memoryLights[i].transform.localRotation=Quaternion.Euler(0,45,12);}
     else{for(int cell=0;cell<3;cell++){Box("Physical target capacitor",pad,new Vector3(.52f,.37f+cell*.20f,.6f),new Vector3(.11f,.13f,.06f),cell<ruin.solution[i]?accent:dark,false);}Box("Capacitor fill",ruinIndicators[i],Vector3.zero,new Vector3(.45f,.08f,.45f),accent,false);}
    }
    if(id==3){var replay=MakeSite("ruin-replay",ruinArt.position+new Vector3(0,.001f,-1.7f),"结晶灯序列播放器");Box("Memory playback plinth",replay,Vector3.up*.35f,new Vector3(.6f,.7f,.6f),steel,false);Box("Memory playback indicator",replay,Vector3.up*.76f,new Vector3(.45f,.11f,.45f),accent,false);}
    RefreshRuin();if(id==3&&!campaign.RuinFor(id).solved)BeginMemory();
   }
   if(id==2||id==3)SpawnFrontierVariant(id==3);player.gameObject.AddComponent<FrontierGadgets>().Configure(player);
  }
  void Arrow(Transform root,Material material){Box("Direction stem",root,new Vector3(0,0,-.11f),new Vector3(.09f,.065f,.45f),material,false);for(int side=-1;side<=1;side+=2){var wing=Box("Direction arrow wing",root,new Vector3(side*.10f,0,.11f),new Vector3(.08f,.065f,.29f),material,false);wing.localRotation=Quaternion.Euler(0,side*42,0);}}
  bool FrontierInteract(string key){if(key=="survey"){if(campaign.Exp("survey-"+id)){campaign.ViewSurvey(id);return true;}if(!surveyStarted){surveyOrigin=player.transform.position;surveyDamage=player.arena.DamageCount;surveyStarted=true;}return true;}if(key=="ruin-replay"){if(!campaign.RuinFor(id).solved)BeginMemory();return true;}if(key.StartsWith("ruin-control-")&&int.TryParse(key.Substring(13),out var index))return PressRuin(index);return false;}
  public bool AdvanceSurvey(bool held,float dt){if(player.Paused||campaign.Modal||!player.arena.Active)return false;if(campaign.Exp("survey-"+id))return false;
   if(!held||!Near("survey")||dt<=0){surveyTime=0;surveyStarted=false;return false;}if(!surveyStarted){surveyStarted=true;surveyOrigin=player.transform.position;surveyDamage=player.arena.DamageCount;}
   if(player.arena.DamageCount!=surveyDamage||Vector3.Distance(player.transform.position,surveyOrigin)>.7f){surveyTime=0;surveyOrigin=player.transform.position;surveyDamage=player.arena.DamageCount;return false;}
   surveyTime+=Mathf.Min(dt,.25f);if(surveyTime>=3){surveyTime=0;surveyStarted=false;return campaign.CompleteSurvey(id);}return false;
  }
  public bool PressRuin(int index){if(player.Paused||campaign.Modal||!player.arena.Active||ruinControls==null||index<0||index>=ruinControls.Length||!Near("ruin-control-"+index))return false;var record=campaign.RuinFor(id);if(record.solved)return false;var definition=FrontierCatalog.Current.RuinFor(id);InteractionFeel.Play(FeelCue.Click,.45f);
   if(id==3){if(memoryPlayback)return false;if(index!=definition.solution[memoryStep]){memoryStep=0;BeginMemory();InteractionFeel.Play(FeelCue.Deny,.5f);return false;}memoryStep++;TintMemory(index,true);if(memoryStep==definition.solution.Length){campaign.CompleteRuin(id);RefreshRuin();return true;}return true;}
   record.values[index]=(record.values[index]+1)%4;RefreshRuin();bool correct=true;for(int i=0;i<3;i++)correct&=record.values[i]==definition.solution[i];if(correct)campaign.CompleteRuin(id);else campaign.Changed();RefreshRuin();return true;
  }
  void BeginMemory(){memoryPlayback=true;memoryClock=-.8f;memoryStep=0;}
  void TintMemory(int index,bool on){if(memoryLights==null||!memoryLights[index])return;Color[] colors={new Color(.3f,.68f,1),new Color(1,.64f,.24f),new Color(.58f,.35f,.96f),new Color(.25f,.83f,.61f)};frontierTint.SetColor("_Color",colors[index]*(on?.65f:.2f));frontierTint.SetColor("_EmissionColor",colors[index]*(on?.4f:.02f));memoryLights[index].SetPropertyBlock(frontierTint);}
  void RefreshRuin(){if(ruinControls==null)return;var record=campaign.RuinFor(id);for(int i=0;i<ruinControls.Length;i++){if(id==2)ruinIndicators[i].localRotation=Quaternion.Euler(0,record.values[i]*90,0);else if(id==3)TintMemory(i,record.solved);else{var fill=ruinIndicators[i].GetChild(0);fill.localScale=new Vector3(.45f,.08f+record.values[i]*.16f,.45f);fill.localPosition=Vector3.up*((record.values[i]*.16f)/2-.23f);}}
  }
  void TickFrontier(){AdvanceSurvey(focus=="survey"&&Input.GetKey(KeyCode.E),Time.deltaTime);if(id!=3||ruinControls==null||campaign.RuinFor(id).solved)return;if(memoryPlayback){memoryClock+=Time.deltaTime;var solution=FrontierCatalog.Current.RuinFor(id).solution;int index=Mathf.FloorToInt(memoryClock/.8f);for(int i=0;i<4;i++)TintMemory(i,index>=0&&index<solution.Length&&solution[index]==i&&memoryClock% .8f<.53f);if(memoryClock>=solution.Length*.8f+.4f){memoryPlayback=false;for(int i=0;i<4;i++)TintMemory(i,false);}}}
  void DrawFrontierHud(){if(surveyTime>0){InteractionFeel.Ring(new Vector2(Screen.width*.5f,Screen.height*.62f),24,SurveyProgress,new Color(.4f,.8f,.86f));}}
  public Vector3 FrontierSitePosition(string key)=>sites.TryGetValue(key,out var site)?site.position:Vector3.positiveInfinity;
  void SpawnFrontierVariant(bool frost){var source=Array.Find(player.arena.creatures,c=>c&&!c.GetComponent<EliteCreature>()&&c.kind==(frost?CreatureKind.SpineFloater:CreatureKind.RiftMauler));if(!source)return;
   var preferred=frost?new Vector3(-35,0,-11):new Vector3(34,0,34);var pos=preferred;bool found=false;var path=new NavMeshPath();for(int attempt=0;attempt<65&&!found;attempt++){float angle=(attempt%8)*Mathf.PI/4;float radius=attempt==0?0:(1+(attempt-1)/8)*3;var probe=preferred+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);if(Mathf.Abs(probe.x)>46||Mathf.Abs(probe.z)>46)continue;probe.y=FieldSurface.Height(probe.x,probe.z,AlienSurfaceLandmarks.Height(probe.x,probe.z));if(!NavMesh.SamplePosition(probe,out var hit,2,NavMesh.AllAreas)||CreatureArena.FlatDistance(hit.position,player.returnBeacon.position)<20||!NavMesh.CalculatePath(player.returnBeacon.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;found=true;foreach(var other in player.arena.creatures)if(other&&CreatureArena.FlatDistance(other.Home,hit.position)<12){found=false;break;}if(found)pos=hit.position;}if(!found){Debug.LogWarning("No dispersed variant anchor in region "+id);return;}
   var g=Instantiate(source.gameObject,pos,Quaternion.identity,player.arena.transform);g.name=frost?"霜环翼龙 · 支路变体":"磁蚀地龙 · 支路变体";var actor=g.GetComponent<AlienCreature>();actor.arena=player.arena;actor.SetHome(pos);actor.detectionRange=19;actor.leashRadius=26;actor.patrolRadius=5;
   var life=g.GetComponent<CreatureVitality>();life.persistentId=frost?"frontier-frost-wing":"frontier-magnetic-ground";life.maxHealth=frost?190:240;g.AddComponent<FrontierCreatureTrait>().Configure(actor,frost);var list=new List<AlienCreature>(player.arena.creatures){actor};player.arena.creatures=list.ToArray();
  }
 }
}
