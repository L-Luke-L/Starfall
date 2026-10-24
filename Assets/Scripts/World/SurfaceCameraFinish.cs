using UnityEngine;
namespace Starfall {
 [RequireComponent(typeof(Camera))]
 public sealed class SurfaceCameraFinish:MonoBehaviour {
  Material finish;
  void OnEnable(){
   var shader=Resources.Load<Shader>("SurfaceFinish");
   if(shader&&shader.isSupported)finish=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
  }
  void OnRenderImage(RenderTexture source,RenderTexture destination){
   if(finish)Graphics.Blit(source,destination,finish);else Graphics.Blit(source,destination);
  }
  void OnDisable(){if(finish)Destroy(finish);finish=null;}
 }
}
