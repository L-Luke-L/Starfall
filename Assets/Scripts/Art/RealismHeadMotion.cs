using System.Collections.Generic;
using UnityEngine;
namespace Starfall {
 // Local anatomical axes are calibrated from the bind pose, not assumed from FBX bone axes.
 public sealed class RealismHeadMotion {
  readonly Transform head,jaw,frame;readonly Transform[] neck;readonly Quaternion[] neutral;
  readonly Quaternion headNeutral,jawNeutral;readonly Vector3 headForward,headUp,jawAxis;
  readonly Vector3[] neckForward;
  readonly Vector3 skullForward,skullUp;
  float groundYawVelocity,groundPitchVelocity;
  float yaw,pitch,open;public Transform Head=>head;
  public RealismHeadMotion(Transform model,Transform body,bool dinosaur){
   frame=body;head=RealismAssets.Bone(model,dinosaur?"bn_Head":"head");jaw=RealismAssets.Bone(model,dinosaur?"bn_Jaw":"jaw_01");
   var chain=new List<Transform>();foreach(var b in model.GetComponentsInChildren<Transform>())if(dinosaur?(b.name.StartsWith("bn_Neck")):(b.name.StartsWith("neck_")&&!b.name.Contains("Spike")))chain.Add(b);
   chain.Sort((a,b)=>Depth(a).CompareTo(Depth(b)));neck=chain.ToArray();neutral=new Quaternion[neck.Length];for(int i=0;i<neck.Length;i++)neutral[i]=neck[i].localRotation;
   if(head){headNeutral=head.localRotation;headForward=head.InverseTransformDirection(body.forward);headUp=head.InverseTransformDirection(body.up);}
   if(dinosaur&&head){
    // The FBX's first pose already turns its muzzle left. Derive the skull frame
    // from the eyes so aiming at zero actually points the muzzle forward.
    var left=RealismAssets.Bone(model,"bn_LeftEye");var right=RealismAssets.Bone(model,"bn_RightEye");
    if(left&&right){
     Vector3 across=(right.position-left.position).normalized;
     Vector3 up=Vector3.ProjectOnPlane(body.up,across).normalized;
     skullForward=head.InverseTransformDirection(Vector3.Cross(across,up));
     skullUp=head.InverseTransformDirection(up);
    }else{skullForward=headForward;skullUp=headUp;}
    neckForward=new Vector3[neck.Length];
    for(int i=0;i<neck.Length;i++){
     var next=i+1<neck.Length?neck[i+1]:head;
     neckForward[i]=neck[i].InverseTransformDirection(next.position-neck[i].position);
    }
   }
   if(jaw){jawNeutral=jaw.localRotation;jawAxis=jaw.InverseTransformDirection(dinosaur&&head?head.TransformDirection(Vector3.Cross(skullUp,skullForward)):body.right);}
  }
  static int Depth(Transform t){int d=0;for(;t;t=t.parent)d++;return d;}
  public void ApplyGround(Vector3 target,CreatureState state,float phase,float clock,float movement,float dt){
   if(!head||!frame||neckForward==null||dt<=0)return;
   bool track=state!=CreatureState.Roam&&state!=CreatureState.Charge;
   var local=frame.InverseTransformDirection(target-head.position);
   float ahead=Mathf.Max(1.2f,local.z);
   float desiredYaw=track?Mathf.Clamp(Mathf.Atan2(local.x,ahead)*Mathf.Rad2Deg,-33,33):Mathf.Sin(clock*1.07f)*7;
   float desiredPitch=track?Mathf.Clamp(-Mathf.Atan2(local.y,Mathf.Max(2,new Vector2(local.x,local.z).magnitude))*Mathf.Rad2Deg,-13,17):2;
   float stride=Mathf.Sin(clock*(state==CreatureState.Charge?15:9));
   float nod=Mathf.Sin(clock*2.2f)*1.4f+stride*movement*2.2f;
   float jawTarget=.06f+Mathf.Sin(clock*2.2f)*.035f;
   if(state==CreatureState.BiteTell){
    float anticipation=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.24f,phase));
    float strike=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.42f,phase));
    desiredPitch+=-8*anticipation+21*strike;
    jawTarget=Mathf.Lerp(anticipation,.07f,strike);
   }else if(state==CreatureState.ChargeTell||state==CreatureState.Charge){
    desiredPitch+=state==CreatureState.Charge?7:4;
    jawTarget=.24f;
   }
   yaw=Mathf.SmoothDampAngle(yaw,desiredYaw,ref groundYawVelocity,.19f,145,dt);
   pitch=Mathf.SmoothDamp(pitch,desiredPitch+nod,ref groundPitchVelocity,state==CreatureState.BiteTell?.065f:.17f,180,dt);
   open=Mathf.Lerp(open,jawTarget,1-Mathf.Exp(-dt*15));

   for(int i=0;i<neck.Length;i++)neck[i].localRotation=neutral[i];
   head.localRotation=headNeutral;
   for(int i=0;i<neck.Length;i++){
    var bone=neck[i];
    float weight=(i+1f)/(neck.Length+1);
    var current=Vector3.ProjectOnPlane(bone.TransformDirection(neckForward[i]),frame.up);
    var wanted=Quaternion.AngleAxis(yaw*weight,frame.up)*frame.forward;
    float straighten=Vector3.SignedAngle(current,wanted,frame.up);
    bone.rotation=Quaternion.AngleAxis(Mathf.Clamp(straighten,-34,34),frame.up)*
     Quaternion.AngleAxis(pitch*.52f/(neck.Length+1),frame.right)*bone.rotation;
   }
   var direction=Quaternion.AngleAxis(yaw,frame.up)*Quaternion.AngleAxis(pitch,frame.right)*frame.forward;
   float bank=-yaw*.065f+stride*movement*1.8f;
   var up=Quaternion.AngleAxis(bank,direction)*frame.up;
   var currentAxes=Quaternion.LookRotation(head.TransformDirection(skullForward),head.TransformDirection(skullUp));
   head.rotation=Quaternion.LookRotation(direction,up)*Quaternion.Inverse(currentAxes)*head.rotation;
   if(jaw)jaw.localRotation=jawNeutral*Quaternion.AngleAxis(open*32,jawAxis);
  }

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
