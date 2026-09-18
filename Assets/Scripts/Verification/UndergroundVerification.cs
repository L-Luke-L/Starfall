using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall {
 public sealed class UndergroundVerification : MonoBehaviour {
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-undergroundQA"))yield break;
   var director=GetComponent<SceneDirector>();var camera=director.worldCamera;
   string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/underground-v3"));Directory.CreateDirectory(folder+"/frames");
   yield return null;QualitySettings.vSyncCount=0;Time.captureFramerate=15;
   var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
   for(int frame=0;frame<150;frame++){
    if(frame==10)StartCoroutine(director.Travel(true));
    yield return new WaitForEndOfFrame();
    Capture(camera,rt,image,folder+"/frames/"+frame.ToString("D4")+".png");
    if(frame==8)Capture(camera,rt,image,folder+"/01-surface.png");
    if(frame==85)Capture(camera,rt,image,folder+"/02-underground.png");
   }
   var samples=FindObjectsByType<BuriedSample>(FindObjectsSortMode.None);int materials=0,encounters=0,events=0,reachable=0;bool unbound=true,concealed=true,independent=true;
   var mapping=new Dictionary<MineralShell,HashSet<BuriedPayloadKind>>();
   foreach(var s in samples){
    var def=s.Definition;if(def.payloadKind==BuriedPayloadKind.RepairMaterial)materials++;else if(def.payloadKind==BuriedPayloadKind.DormantEncounter)encounters++;else events++;
    var d=s.transform.position-director.winchAnchor.position;float angle=Mathf.Abs(Mathf.Atan2(d.x,-d.y)*Mathf.Rad2Deg);
    if(d.magnitude<=director.maxCableLength&&angle<=director.swingDegrees)reachable++;
    unbound&=s.onRecovered.GetPersistentEventCount()==0;
    concealed&=s.visibleName=="矿物样本"&&s.GetComponent<Interactable>().displayName=="矿物样本";
    independent&=s.GetComponent<CircleCollider2D>()!=null&&s.transform.Find("Mineral shell")!=null;
    if(!mapping.ContainsKey(s.shell))mapping.Add(s.shell,new HashSet<BuriedPayloadKind>());mapping[s.shell].Add(def.payloadKind);
   }
   bool ambiguous=true;foreach(var entry in mapping)ambiguous&=entry.Value.Count>=2;
   var bounds=director.mine.transform.Find("Artwork").GetComponent<SpriteRenderer>().bounds;
   bool mineVisible=camera.WorldToViewportPoint(bounds.max).y<.91f&&camera.WorldToViewportPoint(bounds.min).y>0;
   bool pass=samples.Length==12&&materials==8&&encounters==2&&events==2&&reachable==samples.Length&&unbound&&concealed&&independent&&ambiguous&&mineVisible;
   string report="{\n  \"passed\":"+B(pass)+",\n  \"samples\":"+samples.Length+",\n  \"repairMaterialDefinitions\":"+materials+",\n  \"reservedEncounters\":"+encounters+",\n  \"reservedStoryEvents\":"+events+",\n  \"reachableSamples\":"+reachable+",\n  \"allTriggerInterfacesUnbound\":"+B(unbound)+",\n  \"publicLabelsConcealPayload\":"+B(concealed)+",\n  \"eachShellHasMultiplePossiblePayloads\":"+B(ambiguous)+",\n  \"independentObjectsAndColliders\":"+B(independent)+",\n  \"mineFullyVisible\":"+B(mineVisible)+"\n}";
   File.WriteAllText(folder+"/underground-report.json",report);
   camera.orthographicSize=4.0f;camera.transform.position=new Vector3(0,-10,-20);yield return new WaitForEndOfFrame();Capture(camera,rt,image,folder+"/03-mineral-detail.png");
   camera.orthographicSize=9;camera.transform.position=new Vector3(0,-8,-20);camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);Time.captureFramerate=0;
   Debug.Log(pass?"STARFALL_UNDERGROUND_QA_PASS":"STARFALL_UNDERGROUND_QA_FAIL");Application.Quit(pass?0:2);
  }
  static string B(bool b)=>b?"true":"false";
  static void Capture(Camera camera,RenderTexture target,Texture2D image,string file){var old=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;}
 }
}
