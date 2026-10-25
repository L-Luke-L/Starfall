using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Starfall {
 // Decoration only: isolated random generator, no collision or persistent state.
 public sealed class SurfaceDetailPolish:MonoBehaviour {
  Mesh generated;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
  static void Loaded(Scene scene,LoadSceneMode mode){
   // Finish only perspective world cameras in 3D scenes, preserving 2D and UI rendering.
   if(scene.name.StartsWith("Scene0")&&!scene.name.StartsWith("Scene01"))
    foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true))
     if(!camera.orthographic&&camera.clearFlags!=CameraClearFlags.Depth&&camera.clearFlags!=CameraClearFlags.Nothing){
      camera.allowMSAA=true;
      if(!camera.GetComponent<SurfaceCameraFinish>())camera.gameObject.AddComponent<SurfaceCameraFinish>();
     }
   foreach(var root in scene.GetRootGameObjects())
    if(root.name=="Surface detail polish · decorative only")return;
   AlienSurfaceLandmarks landmarks=null;
   foreach(var root in scene.GetRootGameObjects()){
    landmarks=root.GetComponentInChildren<AlienSurfaceLandmarks>();if(landmarks)break;
   }
   if(!landmarks||!landmarks.ground)return;
   var ground=landmarks.ground;
   Mesh source=null;
   foreach(var filter in landmarks.GetComponentsInChildren<MeshFilter>())
    if(filter.sharedMesh&&filter.sharedMesh.name.StartsWith("Faceted basalt")){source=filter.sharedMesh;break;}
   if(!source)return;
   var instances=new List<CombineInstance>();var random=new System.Random(80421);
   for(int i=0;i<650;i++){
    float x=(float)random.NextDouble()*116-58,z=(float)random.NextDouble()*116-58;
    if(new Vector2(x,z+38).magnitude<10||new Vector2(x,z-9).magnitude<17)continue;
    if(!ground.Raycast(new Ray(new Vector3(x,80,z),Vector3.down),out var hit,160)||hit.normal.y<.85f)continue;
    float size=.07f+(float)random.NextDouble()*.19f;
    var rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,(float)random.NextDouble()*360,0);
    instances.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(hit.point-hit.normal*.025f,rotation,new Vector3(size*1.5f,size*.48f,size))});
   }
   var decoration=new GameObject("Surface detail polish · decorative only");
   SceneManager.MoveGameObjectToScene(decoration,scene);
   var owner=decoration.AddComponent<SurfaceDetailPolish>();
   owner.generated=new Mesh{name="Grounded basalt gravel",indexFormat=IndexFormat.UInt32};
   owner.generated.CombineMeshes(instances.ToArray());
   decoration.AddComponent<MeshFilter>().sharedMesh=owner.generated;
   var renderer=decoration.AddComponent<MeshRenderer>();
   renderer.sharedMaterial=ground.GetComponent<Renderer>().sharedMaterial;
   renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
   foreach(var root in scene.GetRootGameObjects())foreach(var light in root.GetComponentsInChildren<Light>())
    if(light.type==LightType.Directional&&light.shadows!=LightShadows.None){light.shadows=LightShadows.Soft;light.shadowBias=.035f;light.shadowNormalBias=.25f;}
  }
  void OnDestroy(){if(generated)Destroy(generated);}
 }
}
