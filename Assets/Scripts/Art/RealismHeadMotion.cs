using System.Collections.Generic;
using UnityEngine;
namespace Starfall {
 // Local anatomical axes are calibrated from the bind pose, not assumed from FBX bone axes.
 public sealed class RealismHeadMotion {
  readonly Transform head,jaw,frame;readonly Transform[] neck;readonly Quaternion[] neutral;
  readonly Quaternion headNeutral,jawNeutral;readonly Vector3 headForward,headUp,jawAxis;
  float yaw,pitch,open;public Transform Head=>head;
  public RealismHeadMotion(Transform model,Transform body,bool dinosaur){
   frame=body;head=RealismAssets.Bone(model,dinosaur?"bn_Head":"head");jaw=RealismAssets.Bone(model,dinosaur?"bn_Jaw":"jaw_01");
   var chain=new List<Transform>();foreach(var b in model.GetComponentsInChildren<Transform>())if(dinosaur?(b.name.StartsWith("bn_Neck")):(b.name.StartsWith("neck_")&&!b.name.Contains("Spike")))chain.Add(b);
   chain.Sort((a,b)=>Depth(a).CompareTo(Depth(b)));neck=chain.ToArray();neutral=new Quaternion[neck.Length];for(int i=0;i<neck.Length;i++)neutral[i]=neck[i].localRotation;
   if(head){headNeutral=head.localRotation;headForward=head.InverseTransformDirection(body.forward);headUp=head.InverseTransformDirection(body.up);}
   if(jaw){jawNeutral=jaw.localRotation;jawAxis=jaw.InverseTransformDirection(body.right);}
  }
  static int Depth(Transform t){int d=0;for(;t;t=t.parent)d++;return d;}
  public void Apply(Vector3 target,bool engaged,float jawAmount,float time,float dt,bool colossal=false){
   if(!head||!frame)return;
   Vector3 local=frame.InverseTransformDirection(target-head.position);float wantYaw=engaged?Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,colossal?-24:-42,colossal?24:42):Mathf.Sin(time*.48f)*9;
   float wantPitch=engaged?Mathf.Clamp(-Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,-18,colossal?22:26):Mathf.Sin(time*.71f)*3;
   float speed=colossal?1.6f:5;float blend=1-Mathf.Exp(-dt*speed);yaw=Mathf.LerpAngle(yaw,wantYaw,blend);pitch=Mathf.Lerp(pitch,wantPitch,blend);open=Mathf.Lerp(open,jawAmount,1-Mathf.Exp(-dt*8));
   // Restore only neck/head channels. Wing, leg and tail animation remains sampled from the source clip.
   for(int i=0;i<neck.Length;i++)neck[i].localRotation=neutral[i];head.localRotation=headNeutral;
   float share=neck.Length>0?.65f/neck.Length:0;foreach(var b in neck)b.rotation=Quaternion.AngleAxis(yaw*share,frame.up)*Quaternion.AngleAxis(pitch*share,frame.right)*b.rotation;
   Vector3 wanted=(Quaternion.AngleAxis(yaw,frame.up)*Quaternion.AngleAxis(pitch,frame.right))*frame.forward;
   Quaternion currentAxes=Quaternion.LookRotation(head.TransformDirection(headForward),head.TransformDirection(headUp));
   head.rotation=Quaternion.LookRotation(wanted,frame.up)*Quaternion.Inverse(currentAxes)*head.rotation;
   if(jaw)jaw.localRotation=jawNeutral*Quaternion.AngleAxis(open*(colossal?17:22),jawAxis);
  }
 }
}
