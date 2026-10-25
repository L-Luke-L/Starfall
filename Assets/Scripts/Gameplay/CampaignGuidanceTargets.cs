using System;
using System.Collections.Generic;
using UnityEngine;
namespace Starfall {
 public sealed partial class CampaignRegion {
  MeshFilter[] guidanceRampMeshes;
  public bool TryGuidancePoint(string key,out Vector3 point){
   point=Vector3.zero;if(key=="beacon"&&player.returnBeacon){point=player.returnBeacon.position;return true;}
   if(sites.TryGetValue(key,out var site)&&site&&site.gameObject.activeInHierarchy){point=site.position;return true;}
   if(key.StartsWith("vein:")&&int.TryParse(key.Substring(5),out int item)){foreach(var resource in player.expedition.resources)if(resource&&resource.gameObject.activeInHierarchy&&!resource.Depleted&&(int)CampaignRules.Raw(resource.metal)==item){point=resource.transform.position;return true;}return false;}
   if(key=="boss-node"&&boss){foreach(var part in boss.GetComponentsInChildren<CampaignBossPart>())if(part.node>=0&&part.gameObject.activeInHierarchy){point=part.transform.position;return true;}return false;}
   if(key=="boss"&&boss){point=boss.transform.position+Vector3.up*1.6f;return true;}
   if(key=="enemy"||key=="variant"||key=="elite"){foreach(var actor in player.arena.creatures){if(!actor||!actor.gameObject.activeInHierarchy)continue;var life=actor.GetComponent<CreatureVitality>();if(!life||life.Dead)continue;var trait=actor.GetComponent<FrontierCreatureTrait>();if(key=="variant"&&(!trait||campaign.State.fieldRecords.Contains(trait.frost?"frost":"magnetic")))continue;if(key=="elite"&&!actor.GetComponent<EliteCreature>())continue;point=actor.transform.position;return true;}}
   return false;
  }
  public void PreferGuidanceFocus(){
   if(Vector3.Distance(player.transform.position,player.returnBeacon.position)<4){focus=null;return;}
   if(CampaignRuntime.GuidanceDetail==0)return;var g=campaign.CurrentGuidance;string key=campaign.GuidanceActiveTarget(g);if(g.region==id&&sites.ContainsKey(key)&&Near(key))focus=key;
  }
  public bool TryGuidanceRamp(Vector3 destination,out Vector3 point){
   point=Vector3.zero;if(id!=4||destination.y<player.transform.position.y+2)return false;float best=float.PositiveInfinity;bool found=false;
   if(guidanceRampMeshes==null){var ramps=new List<MeshFilter>();foreach(var sceneRoot in gameObject.scene.GetRootGameObjects())foreach(var candidate in sceneRoot.GetComponentsInChildren<MeshFilter>())if(candidate.sharedMesh&&candidate.sharedMesh.name.IndexOf("ramp",StringComparison.OrdinalIgnoreCase)>=0)ramps.Add(candidate);guidanceRampMeshes=ramps.ToArray();}
   foreach(var mesh in guidanceRampMeshes){
    if(!mesh||!mesh.sharedMesh)continue;var vertices=mesh.sharedMesh.vertices;if(vertices.Length<4)continue;float low=float.PositiveInfinity,high=float.NegativeInfinity;foreach(var v in vertices){float y=mesh.transform.TransformPoint(v).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}if(high<player.transform.position.y+.7f||low>player.transform.position.y+1.2f)continue;
    Vector3 start=Vector3.zero,end=Vector3.zero;int n=0,m=0;foreach(var v in vertices){var p=mesh.transform.TransformPoint(v);if(p.y<low+.2f){start+=p;n++;}if(p.y>high-.2f){end+=p;m++;}}if(n==0||m==0)continue;start/=n;end/=m;
    var direction=end-start;float length=direction.magnitude;float t=length>.1f?Vector3.Dot(player.transform.position-start,direction)/direction.sqrMagnitude:0;var projected=start+direction*Mathf.Clamp01(t);Vector3 next=start;
    if(t>=0&&t<1&&Vector3.Distance(projected,player.transform.position)<3.5f)next=Vector3.Lerp(start,end,Mathf.Clamp01(t+3/Mathf.Max(1,length)));
    float distance=Vector3.Distance(player.transform.position,next);if(distance<best){best=distance;point=next;found=true;}
   }return found;
  }
  string GuidanceFocusPrompt(string title){
   if(focus=="survey"&&!campaign.Exp("survey-"+id))return "长按 E 三秒 · 保持位置";
   if(focus=="shelter"&&!campaign.Exp("winch"))return "避难舱锁住 · 先解除绞盘";
   if(focus=="medical-door"&&!campaign.Exp("power"))return "隔离门缺电 · 先重接电源";
   if(focus=="crane"&&!campaign.Exp("engineer-rescued"))return "需要先救出工程师岚";
   if(focus.StartsWith("signal-")&&!campaign.ScanUnlocked)return "先修复拾光的三段天线";
   if(focus.StartsWith("sample-")&&!campaign.Exp("doctor-rescued"))return "先救出苏禾，再封存样本";
   return "E  "+title;
  }
 }
}
