using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Starfall;

// Visual-only, repeatable pass. Existing objects and gameplay components are preserved.
public static class SceneDetailPolish {
 const string RootName="Surface detail polish · decorative only";
 [MenuItem("Starfall/Polish 3D Scene Details")]
 public static void Apply(){
  Directory.CreateDirectory("QA/detail-polish");
  foreach(var path in Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p)){
   if(path.Contains("Scene01_"))continue;
   var scene=EditorSceneManager.OpenScene(path);
   var landmarks=UnityEngine.Object.FindFirstObjectByType<AlienSurfaceLandmarks>();
   if(!landmarks||!landmarks.ground)throw new InvalidOperationException("Missing ground: "+path);
   var previous=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
   if(previous)UnityEngine.Object.DestroyImmediate(previous);
   var ground=landmarks.ground;
   var root=new GameObject(RootName);
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/AlienSurface/Rock0.asset");
   var groundMaterial=ground.GetComponent<Renderer>().sharedMaterial;
   var rng=new System.Random(80421);
   // A single combined renderer avoids hundreds of additional draw calls.
   var combine=new System.Collections.Generic.List<CombineInstance>();
   for(int i=0;i<650;i++){
    float x=(float)rng.NextDouble()*116-58,z=(float)rng.NextDouble()*116-58;
    // Keep arrival, return beacon and central arena visually uncluttered.
    if(new Vector2(x,z+38).magnitude<10||new Vector2(x,z-9).magnitude<17)continue;
    var ray=new Ray(new Vector3(x,80,z),Vector3.down);
    if(!ground.Raycast(ray,out var hit,160)||hit.normal.y<.85f)continue;
    float size=.07f+(float)rng.NextDouble()*.19f;
    var rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
    combine.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(hit.point-hit.normal*.025f,rotation,new Vector3(size*1.5f,size*.48f,size))});
   }
   var combined=new Mesh{name="Grounded basalt gravel",indexFormat=IndexFormat.UInt32};
   combined.CombineMeshes(combine.ToArray());
   string assetPath="Assets/Art/AlienSurface/Detail-"+scene.name+".asset";
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
   if(existing){EditorUtility.CopySerialized(combined,existing);UnityEngine.Object.DestroyImmediate(combined);combined=existing;EditorUtility.SetDirty(existing);}
   else AssetDatabase.CreateAsset(combined,assetPath);
   root.AddComponent<MeshFilter>().sharedMesh=combined;
   var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=groundMaterial;
   renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
   root.isStatic=true; // No collider, scripts, lights, particles or navigation changes.
   foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
    if(light.type==LightType.Directional&&light.shadows!=LightShadows.None){light.shadows=LightShadows.Soft;light.shadowBias=.035f;light.shadowNormalBias=.25f;}
   EditorSceneManager.SaveScene(scene);
   Capture(scene.name);
   File.AppendAllText("QA/detail-polish/report.txt",scene.name+": gravel="+combine.Count+", decorativeColliders="+root.GetComponentsInChildren<Collider>().Length+"\n");
  }
  AssetDatabase.SaveAssets();
  var errors=ShaderUtil.GetShaderMessages(Shader.Find("Starfall/AlienGround")).Where(m=>m.severity.ToString()=="Error").ToArray();
  if(errors.Length>0)throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.message)));
  Debug.Log("SCENE_DETAIL_POLISH_PASS");
 }
 static void Capture(string name){
  var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>!c.orthographic&&c.targetTexture==null);
  if(!camera)return;
  var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("QA/detail-polish/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
 }
}
