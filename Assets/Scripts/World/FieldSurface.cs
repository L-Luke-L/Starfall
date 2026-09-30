using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 public static class FieldSurface {
  static readonly RaycastHit[] buffer=new RaycastHit[48];
  // Runtime effects use colliders; the procedural function remains a fallback for editor geometry.
  public static float Height(float x,float z,float referenceY){
   var origin=new Vector3(x,referenceY+3,z);
   int count=Physics.RaycastNonAlloc(origin,Vector3.down,buffer,40,~(1<<30),QueryTriggerInteraction.Ignore);float distance=float.MaxValue,height=0;
   for(int i=0;i<count;i++){var hit=buffer[i];float separation=Mathf.Abs(hit.point.y-referenceY);if(separation>=distance||hit.normal.y<.5f||hit.collider.GetComponentInParent<FirstPersonExplorer>()||hit.collider.GetComponentInParent<CreatureVitality>()||hit.collider.GetComponentInParent<BasinPickup>()||hit.collider.GetComponent<CampaignBossPart>())continue;distance=separation;height=hit.point.y;}if(distance<float.MaxValue)return height;
   return AlienSurfaceLandmarks.Height(x,z);
  }
  public static Vector3 ReachableDrop(Vector3 desired,Vector3 entrance){
   var path=new NavMeshPath();
   if(NavMesh.SamplePosition(desired,out var goal,7,NavMesh.AllAreas)&&NavMesh.SamplePosition(entrance,out var start,8,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return goal.position+Vector3.up*.6f;
   if(NavMesh.SamplePosition(entrance+Vector3.right*2,out var safe,5,NavMesh.AllAreas))return safe.position+Vector3.up*.6f;
   return new Vector3(desired.x,Height(desired.x,desired.z,desired.y)+.6f,desired.z);
  }
 }
}
