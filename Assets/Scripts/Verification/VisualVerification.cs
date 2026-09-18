using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Starfall {
 // Captures the actual rendered scene in the standalone player, including offscreen runs.
 public sealed class VisualVerification : MonoBehaviour {
  IEnumerator Start(){
   if(!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-visualQA"))yield break;
   var dir=GetComponent<SceneDirector>();var cam=dir.worldCamera;
   string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/v2"));Directory.CreateDirectory(folder+"/frames");
   yield return null;QualitySettings.vSyncCount=0;Time.captureFramerate=15;
   var stars=FindObjectsByType<TwinkleStar>(FindObjectsSortMode.None);var meteor=FindObjectsByType<ShootingStar>(FindObjectsSortMode.None);var layers=FindObjectsByType<NebulaLayer>(FindObjectsSortMode.None);var fire=FindFirstObjectByType<WreckFire>();
   float starMin=1,starMax=0,fireMin=1,fireMax=0;
   var target=new RenderTexture(1280,720,24);var read=new Texture2D(1280,720,TextureFormat.RGB24,false);
   for(int frame=0;frame<120;frame++){
    yield return new WaitForEndOfFrame();
    Capture(cam,target,read,folder+"/frames/"+frame.ToString("D4")+".png");
    if(frame==17)Capture(cam,target,read,folder+"/01-surface.png");
    starMin=Mathf.Min(starMin,stars[0].core.color.a);starMax=Mathf.Max(starMax,stars[0].core.color.a);fireMin=Mathf.Min(fireMin,fire.animatedIntensity);fireMax=Mathf.Max(fireMax,fire.animatedIntensity);
   }
   int meteorFrames=0;foreach(var m in meteor)meteorFrames+=m.visibleFrames;
   var hull=dir.wreck.transform.Find("Artwork").GetComponent<SpriteRenderer>();
   bool attached=fire.transform.IsChildOf(dir.wreck.transform)&&fire.groundLight.transform.IsChildOf(dir.wreck.transform);
   string report="{\n  \"stars\":"+stars.Length+",\n  \"independentGalaxyLayers\":"+layers.Length+",\n  \"starAlphaVariation\":"+(starMax-starMin).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",\n  \"fireIntensityVariation\":"+(fireMax-fireMin).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",\n  \"meteorVisibleFrames\":"+meteorFrames+",\n  \"fireAttachedToWreck\":"+attached.ToString().ToLowerInvariant()+",\n  \"wreckFullSpriteWidth\":"+hull.bounds.size.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"\n}";
   File.WriteAllText(folder+"/visual-report.json",report);
   cam.orthographicSize=2.3f;cam.transform.position=new Vector3(9.15f,-2.6f,-20);Directory.CreateDirectory(folder+"/detail-frames");
   for(int frame=0;frame<60;frame++){yield return new WaitForEndOfFrame();Capture(cam,target,read,folder+"/detail-frames/"+frame.ToString("D4")+".png");if(frame==22)Capture(cam,target,read,folder+"/02-wreck-detail.png");}
   cam.orthographicSize=9;cam.transform.position=new Vector3(0,0,-20);
   cam.targetTexture=null;target.Release();Destroy(target);Destroy(read);Time.captureFramerate=0;
   Debug.Log("STARFALL_VISUAL_QA_PASS "+report);Application.Quit();
  }
  static void Capture(Camera camera,RenderTexture target,Texture2D read,string file){
   var old=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();File.WriteAllBytes(file,read.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;
  }
 }
}
