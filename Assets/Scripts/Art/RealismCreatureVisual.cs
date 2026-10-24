using UnityEngine;
namespace Starfall {
 [DefaultExecutionOrder(1000)]
 public sealed class RealismCreatureVisual:MonoBehaviour {
  [SerializeField] AlienCreature actor;Animation animationPlayer;AnimationState pose;CreatureState previous;float clock,phase;RealismHeadMotion headMotion;
  Transform leftLeg,rightLeg,leftToe,rightToe;Quaternion leftRest,rightRest;Material variantMaterial;Mesh variantMesh;
  // Always sample before procedural head correction; visibility-driven evaluation can overwrite the correction when a camera renders.
  public void Configure(AlienCreature value){actor=value;headMotion=new RealismHeadMotion(transform,actor.transform,actor.kind==CreatureKind.RiftMauler);animationPlayer=GetComponentInChildren<Animation>();if(animationPlayer){animationPlayer.playAutomatically=false;animationPlayer.cullingType=AnimationCullingType.AlwaysAnimate;foreach(AnimationState state in animationPlayer){pose=state;break;}if(pose!=null){animationPlayer.Play(pose.name);pose.speed=0;pose.wrapMode=WrapMode.ClampForever;}}
   leftLeg=RealismAssets.Bone(transform,"bn_LeftUpLeg");rightLeg=RealismAssets.Bone(transform,"bn_RightUpLeg");if(leftLeg)leftRest=leftLeg.localRotation;if(rightLeg)rightRest=rightLeg.localRotation;leftToe=RealismAssets.Bone(transform,"bn_LeftFootMiddle2");rightToe=RealismAssets.Bone(transform,"bn_RightFootMiddle2");VariantCrest();
  }
  void VariantCrest(){
   var trait=actor.GetComponent<FrontierCreatureTrait>();var elite=actor.GetComponent<EliteCreature>();if(!trait&&!elite)return;foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Variant crest")return;
   var points=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();const int rows=9;
   for(int j=0;j<=rows;j++){float u=j/(float)rows,w=.16f*Mathf.Pow(1-u,.65f)+.003f,y=u*.6f,z=u*u*.22f;points.Add(new Vector3(-w,y,z-.04f));points.Add(new Vector3(w,y,z-.04f));points.Add(new Vector3(w,y,z+.04f));points.Add(new Vector3(-w,y,z+.04f));if(j>0)for(int k=0;k<4;k++){int a=(j-1)*4+k,b=(j-1)*4+(k+1)%4;triangles.AddRange(new[]{a,b,a+4,b,b+4,a+4});}}
   variantMesh=new Mesh{name="Curved species crest"};variantMesh.SetVertices(points);variantMesh.SetTriangles(triangles,0);variantMesh.RecalculateNormals();variantMesh.RecalculateBounds();variantMaterial=new Material(Resources.Load<Shader>("StarfallRealism/RealismPBR")){name="Species identification crest",color=trait&&trait.frost?new Color(.35f,.56f,.64f):trait?new Color(.27f,.18f,.33f):new Color(.24f,.21f,.14f)};variantMaterial.SetFloat("_Glossiness",.23f);variantMaterial.SetTexture("_BumpMap",RealismAssets.Texture("Textures/ScaleDetail_Normal"));variantMaterial.EnableKeyword("_NORMALMAP");
   var bone=RealismAssets.Bone(transform,actor.kind==CreatureKind.RiftMauler?"bn_Spine":"neck_01");if(!bone)bone=transform;
   for(int i=0;i<4;i++){var g=new GameObject("Variant crest");g.transform.SetParent(bone,false);g.transform.position=bone.position+actor.transform.up*(actor.kind==CreatureKind.RiftMauler?.95f:.22f)-actor.transform.forward*(i*.28f);g.transform.rotation=actor.transform.rotation;g.transform.localScale=new Vector3(1/bone.lossyScale.x,1/bone.lossyScale.y,1/bone.lossyScale.z);g.AddComponent<MeshFilter>().sharedMesh=variantMesh;g.AddComponent<MeshRenderer>().sharedMaterial=variantMaterial;}
  }
  void OnDestroy(){if(variantMesh)Destroy(variantMesh);if(variantMaterial)Destroy(variantMaterial);}
  void Start(){if(!actor)actor=GetComponentInParent<AlienCreature>();if(actor&&!animationPlayer)Configure(actor);}
  void LateUpdate(){if(!actor||!actor.arena||!actor.arena.Active)return;var life=actor.GetComponent<CreatureVitality>();if(life&&life.Dead)return;
   float dt=Time.deltaTime;clock+=dt;if(previous!=actor.State){previous=actor.State;phase=0;}phase+=dt;
   if(pose!=null){pose.time=actor.kind==CreatureKind.SpineFloater?clock%Mathf.Max(pose.length,.01f):actor.State==CreatureState.BiteTell?Mathf.Min(phase/.42f,1)*pose.length*.5f:pose.length*.015f;animationPlayer.Sample();}
   headMotion?.Apply(actor.arena.player.transform.position+Vector3.up,actor.State!=CreatureState.Roam,actor.IsWindingUp?.7f:actor.State==CreatureState.Volley?1:0,clock,dt);
   if(actor.kind==CreatureKind.RiftMauler){float moving=actor.Agent?Mathf.Clamp01(actor.Agent.velocity.magnitude/5):0;if(actor.State==CreatureState.Charge)moving=1;float gait=Mathf.Sin(clock*(actor.State==CreatureState.Charge?15:9))*21*moving;
    if(leftLeg)leftLeg.localRotation=leftRest*Quaternion.AngleAxis(gait,Vector3.forward);if(rightLeg)rightLeg.localRotation=rightRest*Quaternion.AngleAxis(-gait,Vector3.forward);
    if(leftToe&&rightToe){float l=FieldSurface.Height(leftToe.position.x,leftToe.position.z,actor.transform.position.y)+.07f-leftToe.position.y;float r=FieldSurface.Height(rightToe.position.x,rightToe.position.z,actor.transform.position.y)+.07f-rightToe.position.y;transform.position+=Vector3.up*Mathf.Clamp(Mathf.Max(l,r),-.25f,.25f)*(1-Mathf.Exp(-dt*16));}
   }
  }
 }
 [DefaultExecutionOrder(1100)]
 public sealed class RealismTitanVisual:MonoBehaviour {
  [SerializeField] SkyTitan titan;Animation player;AnimationState pose;Transform breathAnchor,headAnchor;LineRenderer[] breathLines;Light chargingLight;RealismHeadMotion headMotion;Transform facing;
  public void Configure(SkyTitan value){titan=value;var f=new GameObject("Anatomical forward frame").transform;f.SetParent(titan.transform,false);f.localRotation=Quaternion.Euler(0,180,0);facing=f;headMotion=new RealismHeadMotion(transform,f,false);player=GetComponentInChildren<Animation>();if(player){player.playAutomatically=false;player.cullingType=AnimationCullingType.AlwaysAnimate;foreach(AnimationState state in player){pose=state;break;}if(pose!=null){player.Play(pose.name);pose.speed=0;pose.wrapMode=WrapMode.Loop;}}
   breathAnchor=RealismAssets.Bone(transform,"fireBreath");headAnchor=RealismAssets.Bone(transform,"head");var lines=new System.Collections.Generic.List<LineRenderer>();if(titan.arena){foreach(var line in titan.arena.GetComponentsInChildren<LineRenderer>(true))if(line.name=="Mouth anchored breath halo"||line.name=="White hot breath core")lines.Add(line);foreach(var light in titan.GetComponentsInChildren<Light>(true))if(light.name=="Charging throat light")chargingLight=light;}breathLines=lines.ToArray();
  }
  void OnDestroy(){if(facing)Destroy(facing.gameObject);}
  void Start(){if(!titan)titan=GetComponentInParent<SkyTitan>();if(titan&&!player)Configure(titan);}
  void LateUpdate(){if(!titan)return;var life=titan.GetComponent<CreatureVitality>();if(life&&life.Dead){foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;return;}if(!titan.Active)return;
   if(pose!=null){pose.time=titan.AnimationTime*.22f%Mathf.Max(.01f,pose.length);player.Sample();}
   var breathAttack=titan.GetComponent<TitanAreaAttack>();headMotion?.Apply(titan.player.transform.position+Vector3.up,true,titan.IsRoaring||(breathAttack&&breathAttack.IsFiring)?1:breathAttack&&breathAttack.IsAttacking?.5f:0,titan.AnimationTime,Time.deltaTime,true);
   // Keep the visible head centred on the original head hit volume. Only redraw the breath origin;
   // the attack muzzle transform, locked footprint and damage source are left untouched.
   if(headAnchor&&titan.head)transform.position+=titan.head.position-headAnchor.position;
   if(breathAnchor){if(chargingLight)chargingLight.transform.position=breathAnchor.position;var attack=titan.GetComponent<TitanAreaAttack>();if(attack&&attack.IsFiring&&breathLines!=null)foreach(var line in breathLines)if(line&&line.positionCount>0)line.SetPosition(0,breathAnchor.position);}
  }
 }
}
