using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Starfall;

public static class TitanBuilder {
 const string Folder="Assets/Art/SkyTitan/";
 static Mesh segment,horn;static Material armor,edge,under,web,vein,black;
 static T Save<T>(T value,string name)where T:Object{var path=Folder+name;var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior){EditorUtility.CopySerialized(value,prior);Object.DestroyImmediate(value);EditorUtility.SetDirty(prior);return prior;}AssetDatabase.CreateAsset(value,path);return value;}
 static Transform Group(string name,Transform parent,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
 static Transform Part(string name,Transform parent,Vector3 pos,Vector3 size,Mesh mesh,Material mat){var t=Group(name,parent,pos);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
 static Transform Bone(string name,Transform parent,Vector3 a,Vector3 b,float width,Material mat){var t=Part(name,parent,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.53f,width),segment,mat);t.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);return t;}
 static Material Hide(string name,Color baseColor,Color dust){var m=new Material(Shader.Find("Starfall/AlienGround"));m.name=name;m.SetColor("_Color",baseColor);m.SetColor("_Dust",dust);m.SetFloat("_Scale",.4f);return Save(m,name+".mat");}
 static Material Lit(string name,Color color,float emissive=0){var m=new Material(Shader.Find("Standard"));m.name=name;m.color=color;m.SetFloat("_Glossiness",.19f);m.SetFloat("_Metallic",.13f);if(emissive>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emissive);}return Save(m,name+".mat");}
 static Mesh Flat(List<Vector3> vertices,List<int> indices,string name){var v=new List<Vector3>();var t=new List<int>();foreach(int index in indices){t.Add(v.Count);v.Add(vertices[index]);}var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
 static Mesh Body(){
  float[] zs={-38,-29,-14,6,26,44,61,76},width={2,12,21,23,20,14,7,.1f},height={3,8,13,14,12,9,5,.1f};var v=new List<Vector3>();var t=new List<int>();const int sides=18;
  for(int j=0;j<zs.Length;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*width[j],Mathf.Sin(a)*height[j],zs[j]));}
  for(int j=0;j<zs.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;t.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}return Flat(v,t,"Leviathan armored fuselage");
 }
 static Mesh Membrane(Vector3 a,Vector3 b){
  var v=new List<Vector3>();var tri=new List<int>();const int n=8;Vector3 anchor=new Vector3(0,0,7);
  for(int side=0;side<2;side++)for(int r=0;r<=n;r++)for(int c=0;c<=n;c++){float u=r/(float)n,f=c/(float)n;var p=Vector3.Lerp(anchor,Vector3.Lerp(a,b,f),u);p.y-=Mathf.Sin(u*Mathf.PI)*4+Mathf.Sin(f*Mathf.PI)*u*2;p.y+=(side==0?.23f:-.23f);v.Add(p);}
  for(int r=0;r<n;r++)for(int c=0;c<n;c++){int k=r*(n+1)+c,o=(n+1)*(n+1);tri.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2,k+o,k+1+o,k+n+1+o,k+1+o,k+n+2+o,k+n+1+o});}
  return Flat(v,tri,"Scalloped wing sail");
 }
 public static void Install(FirstPersonExplorer player,Light sun){
  Directory.CreateDirectory(Folder);segment=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Creatures/OrganicSegment.asset");horn=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Creatures/ChitinSpike.asset");
  armor=Hide("Ancient slate carapace",new Color(.075f,.10f,.12f),new Color(.24f,.26f,.27f));edge=Hide("Weathered dorsal plates",new Color(.17f,.16f,.15f),new Color(.34f,.30f,.25f));under=Hide("Ribbed underside",new Color(.035f,.055f,.065f),new Color(.14f,.18f,.19f));web=Hide("Heavy wing membrane",new Color(.07f,.10f,.12f),new Color(.22f,.26f,.28f));vein=Lit("Ember sensory fissures",new Color(.64f,.20f,.07f),1.2f);black=Lit("Deep mouth cavity",new Color(.008f,.011f,.013f));
  var root=Group("蚀穹古龙 · Eclipse Wyrm",null,new Vector3(0,57,62));var titan=root.gameObject.AddComponent<SkyTitan>();
  Part("114m segmented core",root,Vector3.zero,Vector3.one,Save(Body(),"LeviathanBody.asset"),armor);
  var collision=Group("Body interaction volume",root,new Vector3(0,0,8));var box=collision.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(38,24,83);
  for(int i=0;i<9;i++){
   float z=-24+i*9.1f;float width=19-Mathf.Abs(z-5)*.17f;
   Part("Overlapping dorsal shield "+i,root,new Vector3(0,11-Mathf.Abs(z-5)*.065f,z),new Vector3(width*.66f,3.2f,6.2f),segment,i%3==0?edge:armor);
   var crest=Part("Skyline spine "+i,root,new Vector3(0,14-Mathf.Abs(z-5)*.08f,z),new Vector3(1.4f,15-i*.8f,6),horn,edge);crest.localRotation=Quaternion.Euler(22,0,0);
   for(int side=-1;side<=1;side+=2){var rib=Part("Ventral curved rib",root,new Vector3(side*(width*.67f),-10+Mathf.Abs(z-5)*.06f,z),new Vector3(1.5f,3,6.4f),segment,under);rib.localRotation=Quaternion.Euler(0,0,side*28);Part("Subtle flank fissure",root,new Vector3(side*(width+.4f),-1,z),new Vector3(.18f,.55f,3.5f),segment,vein);}
  }
  titan.head=Group("Slow tracking skull",root,new Vector3(0,-7,-33));
  Part("Broad armored cranium",titan.head,new Vector3(0,0,-1),new Vector3(10,8,15),segment,armor);
  Part("Long saurian muzzle",titan.head,new Vector3(0,1,-19),new Vector3(7.8f,4.8f,14),segment,armor);
  Part("Forehead keel",titan.head,new Vector3(0,6,-8),new Vector3(5,5,14),segment,edge);
  Part("Maw shadow cavity",titan.head,new Vector3(0,-4,-12),new Vector3(7,4.5f,14),segment,black);
  var organs=new List<Renderer>();
  for(int side=-1;side<=1;side+=2){
   var brow=Part("Armored eye brow",titan.head,new Vector3(side*9,3,-12),new Vector3(5.2f,2,7),segment,edge);brow.localRotation=Quaternion.Euler(0,side*15,-side*12);
   var eye=Part("Single ancient dragon eye "+side,titan.head,new Vector3(side*8.6f,2.1f,-13.8f),new Vector3(.85f,.48f,1.25f),segment,vein);organs.Add(eye.GetComponent<Renderer>());

   var crown=Part("Swept temple horn",titan.head,new Vector3(side*11,4,0),new Vector3(3.4f,19,4),horn,edge);crown.localRotation=Quaternion.Euler(24,0,-side*24);
   var cheek=Part("Swept cheek horn",titan.head,new Vector3(side*12,-3,-3),new Vector3(2.2f,9,3),horn,armor);cheek.localRotation=Quaternion.Euler(65,0,-side*28);
   for(int j=0;j<4;j++){var tooth=Part("Upper fang",titan.head,new Vector3(side*6.2f,-3.8f,-29+j*4.3f),new Vector3(.8f,4.3f+j*.55f,1),horn,edge);tooth.localRotation=Quaternion.Euler(180,0,side*9);}
  }
  titan.jaw=Group("Articulated lower jaw",titan.head,new Vector3(0,-4,3));
  Part("Mandible armor",titan.jaw,new Vector3(0,-4,-16),new Vector3(7.3f,2.4f,17),segment,under);
  for(int side=-1;side<=1;side+=2){Bone("Mandible edge",titan.jaw,new Vector3(side*7,-1,1),new Vector3(side*6.5f,-4,-30),1.7f,edge);for(int i=0;i<4;i++)Part("Lower tooth",titan.jaw,new Vector3(side*6.1f,-3,-29+i*4.3f),new Vector3(.65f,3,.75f),horn,edge);}
  for(int i=0;i<4;i++){var organ=Part("Deep throat slit",titan.head,new Vector3(0,-3.5f-i*.8f,-19+i),new Vector3(3.5f-i*.5f,.18f,.14f),segment,vein);organs.Add(organ.GetComponent<Renderer>());}
  var skullBox=titan.head.gameObject.AddComponent<BoxCollider>();skullBox.center=new Vector3(0,-1,-4);skullBox.size=new Vector3(27,20,32);
  titan.wings=new Transform[2];Vector3[] tips={new Vector3(0,0,-24),new Vector3(25,7,-39),new Vector3(59,10,-38),new Vector3(100,4,-20),new Vector3(86,-3,18),new Vector3(60,-6,44),new Vector3(26,-5,53),new Vector3(0,-2,37)};
  for(int side=-1;side<=1;side+=2){
   var wing=Group(side<0?"Port 118m wing":"Starboard 118m wing",root,new Vector3(side*18,2,3));titan.wings[side<0?0:1]=wing;
   for(int j=0;j<tips.Length-1;j++){var a=tips[j];var b=tips[j+1];a.x*=side;b.x*=side;Part("Scalloped sail panel "+j,wing,Vector3.zero,Vector3.one,Save(Membrane(a,b),"Wing_"+side+"_"+j+".asset"),web);}
   for(int j=1;j<tips.Length-1;j++){var tip=tips[j];tip.x*=side;var joint=Vector3.Lerp(new Vector3(0,0,7),tip,.55f)+Vector3.up*1.6f;Bone("Massive wing finger root",wing,new Vector3(0,0,7),joint,1.8f-j*.12f,armor);Bone("Tapered wing finger",wing,joint,tip,.9f,edge);var barb=Part("Trailing wing talon",wing,tip,new Vector3(1.15f,8,1.8f),horn,edge);barb.localRotation=Quaternion.Euler(65,0,-side*40);}
   Bone("Leading edge armor",wing,tips[0],new Vector3(side*59,10,-38),2.2f,edge);Bone("Outer leading edge",wing,new Vector3(side*59,10,-38),new Vector3(side*100,4,-20),1.25f,armor);
  }
  var tail=new List<Transform>();Transform previous=root;Vector3 anchor=new Vector3(0,0,65);for(int i=0;i<5;i++){var joint=Group("Articulated tail segment "+i,previous,anchor);tail.Add(joint);Part("Tail armor",joint,new Vector3(0,0,8),new Vector3(6-i,4-i*.65f,11),segment,armor);if(i<4)Part("Tail keel",joint,new Vector3(0,3-i*.5f,7),new Vector3(1.3f,4,4),horn,edge);previous=joint;anchor=new Vector3(0,0,16);}titan.tail=tail.ToArray();
  var limbs=new List<Transform>();for(int side=-1;side<=1;side+=2)for(int i=0;i<2;i++){
   var t=Group((i==0?"Muscular foreleg ":"Powerful hind leg ")+side,root,new Vector3(side*(i==0?12:15),-7,i==0?-20:31));limbs.Add(t);
   Vector3 knee=new Vector3(side*6,-12,i==0?3:8),ankle=new Vector3(side*7,-23,i==0?-5:1);
   Bone("Thick scaled upper limb",t,Vector3.zero,knee,i==0?4.3f:5.5f,armor);Bone("Defined reptile shin",t,knee,ankle,2.6f,under);
   Part("Armored knee",t,knee,new Vector3(4,4.5f,4),segment,edge);Part("Broad clawed paw",t,ankle+new Vector3(0,-1,-3),new Vector3(4.5f,2,6),segment,armor);
   for(int j=0;j<3;j++){var claw=Part("Curved ivory talon",t,ankle+new Vector3((j-1)*2.8f,-1,-7),new Vector3(.85f,5,1.2f),horn,edge);claw.localRotation=Quaternion.Euler(-105,0,0);}
  }titan.tendrils=limbs.ToArray();titan.organs=organs.ToArray();
  var attack=root.gameObject.AddComponent<TitanAreaAttack>();attack.titan=titan;attack.muzzle=Group("Atomic breath mouth socket",titan.head,new Vector3(0,-4,-32));
  var energy=new Material(Shader.Find("Starfall/TitanEnergy"));energy.name="Dragon breath energy";attack.energyMaterial=Save(energy,"DragonEnergy.mat");

  titan.breath=Group("Colossal breathing",root,Vector3.zero).gameObject.AddComponent<AudioSource>();titan.breath.playOnAwake=false;titan.breath.spatialBlend=.65f;titan.breath.minDistance=60;titan.breath.maxDistance=320;titan.breath.volume=.32f;
  titan.voice=Group("Deep airborne roar",titan.head,Vector3.zero).gameObject.AddComponent<AudioSource>();titan.voice.playOnAwake=false;titan.voice.spatialBlend=.7f;titan.voice.minDistance=60;titan.voice.maxDistance=340;titan.voice.volume=.48f;
  var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"EclipseLeviathan.prefab");Object.DestroyImmediate(root.gameObject);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);titan=instance.GetComponent<SkyTitan>();titan.player=player;titan.arena=player.arena;
  sun.transform.rotation=Quaternion.Euler(38,-48,0);sun.shadowStrength=.86f;QualitySettings.shadowDistance=260;QualitySettings.shadowCascades=4;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
 }
}
