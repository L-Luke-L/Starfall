using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 // All geometry is visual only, authored in metres under a unit-scale root.
 public sealed class RealismStructures {
  readonly List<Mesh> meshes=new List<Mesh>();
  readonly List<Material> materials=new List<Material>();
  readonly Material alloy,dark,paint,concrete;
  public RealismStructures(){
   alloy=Surface("Brushed alloy",new Color(.43f,.46f,.46f),.72f,.32f);
   dark=Surface("Rubber and recessed panels",new Color(.065f,.08f,.085f),.15f,.24f);
   paint=Surface("Ceramic equipment coating",new Color(.56f,.59f,.56f),.22f,.28f);
   concrete=Surface("Foundation aggregate",new Color(.26f,.24f,.20f),0,.12f);
  }
  Material Surface(string name,Color color,float metal,float gloss){var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",gloss);m.mainTexture=RealismAssets.Texture("Textures/MetalColor");m.SetTexture("_BumpMap",RealismAssets.Texture("Textures/MetalNormal"));m.EnableKeyword("_NORMALMAP");m.enableInstancing=true;materials.Add(m);return m;}
  Transform Root(Transform source,string name,Vector3 position){var t=new GameObject("Realism appearance · "+name).transform;t.SetPositionAndRotation(position,source.rotation);t.SetParent(source,true);return t;}
  Transform Part(Transform parent,string name,Mesh mesh,Material mat,Vector3 p,Quaternion q){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;t.localRotation=q;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;}
  public void Antenna(Transform site){
   var root=Root(site,"braced communication antenna",site.position);
   foreach(var r in site.GetComponentsInChildren<MeshRenderer>())if(!RealismAssets.InAppearance(r.transform)&&!r.name.EndsWith(" lens"))r.forceRenderingOff=true;
   Box(root,"Anchored foundation",new Vector3(0,.09f,0),new Vector3(.78f,.18f,.70f),.035f,concrete);
   Box(root,"Bolted mast shoe",new Vector3(0,.23f,0),new Vector3(.44f,.12f,.42f),.025f,alloy);
   Tube(root,"Lower mast",new Vector3(0,.25f,0),new Vector3(0,1.72f,0),.105f,alloy);
   Tube(root,"Upper mast",new Vector3(0,1.45f,0),new Vector3(0,2.38f,0),.075f,alloy);
   for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f+Mathf.PI*.25f;var foot=new Vector3(Mathf.Cos(a)*.26f,.19f,Mathf.Sin(a)*.26f);Tube(root,"Diagonal brace",foot,new Vector3(0,.98f,0),.026f,alloy);Tube(root,"Anchor bolt",foot-Vector3.up*.015f,foot+Vector3.up*.08f,.024f,dark,12);}
   for(int i=0;i<3;i++)Tube(root,"Mast clamp",new Vector3(0,.6f+i*.52f,0),new Vector3(0,.65f+i*.52f,0),.122f,dark);
   Box(root,"Sealed control cabinet",new Vector3(.26f,.66f,.035f),new Vector3(.42f,.62f,.35f),.045f,paint);
   Box(root,"Cabinet gasket",new Vector3(.26f,.66f,-.148f),new Vector3(.34f,.52f,.025f),.02f,dark);
   Box(root,"Access cover",new Vector3(.26f,.66f,-.166f),new Vector3(.30f,.47f,.024f),.016f,alloy);
   for(int k=0;k<5;k++)Box(root,"Vent louvre",new Vector3(.26f,.50f+k*.035f,-.183f),new Vector3(.20f,.012f,.014f),.004f,dark);
   Tube(root,"Protected cable conduit",new Vector3(-.12f,.3f,.04f),new Vector3(-.12f,2.2f,.04f),.017f,dark,12);
   var dish=new GameObject("Articulated reflector").transform;dish.SetParent(root,false);dish.localPosition=new Vector3(0,2.32f,-.05f);dish.localRotation=Quaternion.Euler(22,0,0);
   Tube(root,"Elevation hinge",new Vector3(-.17f,2.28f,0),new Vector3(.17f,2.28f,0),.10f,dark);
   Part(dish,"Double sided paraboloid shell",Dish(),paint,Vector3.zero,Quaternion.identity);
   for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Vector3 p=new Vector3(Mathf.Cos(a)*.52f,Mathf.Sin(a)*.52f,-.19f);Tube(dish,"Reflector rear rib",new Vector3(0,0,.035f),p+Vector3.forward*.045f,.014f,alloy,10);if(i%2==0)Tube(dish,"Feed support",p,new Vector3(0,0,-.53f),.012f,dark,10);}
   Tube(dish,"Feed horn",new Vector3(0,0,-.57f),new Vector3(0,0,-.45f),.046f,alloy);
   foreach(var r in site.GetComponentsInChildren<MeshRenderer>())if(!RealismAssets.InAppearance(r.transform)&&r.name.EndsWith(" lens")){r.transform.localPosition=new Vector3(.26f,.85f,-.192f);r.transform.localScale=new Vector3(.18f,.055f,.014f);}
   Combine(root);
  }
  public void Housing(Transform original){
   var filter=original.GetComponent<MeshFilter>();var renderer=original.GetComponent<MeshRenderer>();if(!filter||!renderer)return;
   Vector3 size=Vector3.Scale(filter.sharedMesh.bounds.size,Abs(original.lossyScale));
   if(size.x<.12f||size.y<.12f||size.z<.12f)return;
   Vector3 center=original.TransformPoint(filter.sharedMesh.bounds.center);var root=Root(original,"fabricated "+original.name,center);var combinedRoot=root;renderer.forceRenderingOff=true;
   float bevel=Mathf.Min(.075f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.12f);
   bool building=Mathf.Max(size.x,size.z)>3&&size.y>2;
   Box(root,"Chamfered structural shell",Vector3.zero,size,bevel,building?paint:alloy);
   if(building){
    if(size.z>size.x){var detail=new GameObject("Panel orientation").transform;detail.SetParent(root,false);detail.localRotation=Quaternion.Euler(0,90,0);root=detail;size=new Vector3(size.z,size.y,size.x);}

    int count=Mathf.Clamp(Mathf.CeilToInt(size.x/1.25f),2,8);float step=size.x/count;
    for(int i=0;i<count;i++){float x=-size.x*.5f+step*(i+.5f);Box(root,"Recessed wall panel",new Vector3(x,0,-size.z*.5f-.008f),new Vector3(step-.065f,size.y-.28f,.032f),.012f,dark);Box(root,"Raised wall skin",new Vector3(x,0,-size.z*.5f-.030f),new Vector3(step-.11f,size.y-.34f,.035f),.018f,paint);}
    for(int side=-1;side<=1;side+=2){Box(root,"Corner reinforcement",new Vector3(side*(size.x*.5f-.095f),0,-size.z*.5f-.05f),new Vector3(.13f,size.y-.1f,.13f),.025f,alloy);Box(root,"Service vent surround",new Vector3(side*(size.x*.5f+.015f),.1f,0),new Vector3(.08f,.85f,1.22f),.025f,dark);for(int j=0;j<8;j++)Box(root,"Vent blade",new Vector3(side*(size.x*.5f+.065f),-.23f+j*.09f,0),new Vector3(.05f,.035f,1.1f),.008f,alloy);}
    Box(root,"Roof weather seam",new Vector3(0,size.y*.5f-.02f,0),new Vector3(size.x+.10f,.14f,size.z+.10f),.04f,alloy);
    Box(root,"Foundation skirt",new Vector3(0,-size.y*.5f+.08f,0),new Vector3(size.x+.08f,.17f,size.z+.08f),.025f,concrete);
   }else if(size.y>size.x*2.5f){
    for(int i=0;i<Mathf.Min(6,Mathf.CeilToInt(size.y/1.2f));i++){float y=-size.y*.5f+.2f+i*1.1f;Box(root,"Structural splice collar",new Vector3(0,y,0),new Vector3(size.x+.07f,.10f,size.z+.07f),.015f,dark);}
    Box(root,"Load spreading foot",new Vector3(0,-size.y*.5f+.08f,0),new Vector3(size.x+.16f,.16f,size.z+.16f),.025f,alloy);
   }else{
    for(int side=-1;side<=1;side+=2)Box(root,"End cap seam",new Vector3(side*(size.x*.5f-.09f),0,0),new Vector3(.045f,size.y+.012f,size.z+.012f),.01f,dark);
   }
   if(original.name.StartsWith("Independent cargo container")){
    for(int side=-1;side<=1;side+=2){
     for(int i=0;i<9;i++)Box(root,"Pressed cargo wall rib",new Vector3(side*(size.x*.5f+.016f),0,-size.z*.5f+.25f+i*(size.z-.5f)/8),new Vector3(.045f,size.y-.22f,.065f),.012f,paint);
     Box(root,"Door gasket",new Vector3(side*size.x*.245f,0,-size.z*.5f-.014f),new Vector3(size.x*.48f,size.y-.18f,.026f),.014f,dark);
     Box(root,"Cargo door skin",new Vector3(side*size.x*.245f,0,-size.z*.5f-.033f),new Vector3(size.x*.48f-.055f,size.y-.24f,.025f),.012f,paint);
     Tube(root,"Door locking rod",new Vector3(side*size.x*.30f,-size.y*.39f,-size.z*.5f-.068f),new Vector3(side*size.x*.30f,size.y*.39f,-size.z*.5f-.068f),.021f,alloy,12);
     Box(root,"Latch lever",new Vector3(side*size.x*.30f,-.12f,-size.z*.5f-.095f),new Vector3(.19f,.035f,.033f),.009f,dark);
     for(int j=-1;j<=1;j++)Box(root,"Door hinge",new Vector3(side*(size.x*.5f-.09f),j*size.y*.33f,-size.z*.5f-.055f),new Vector3(.12f,.075f,.055f),.015f,alloy);
     for(int z=-1;z<=1;z+=2)Box(root,"Cargo corner protector",new Vector3(side*(size.x*.5f-.06f),0,z*(size.z*.5f-.06f)),new Vector3(.14f,size.y+.025f,.14f),.025f,alloy);
    }
   }
   if(original.name.StartsWith("Broken roof")){
    // Shallow joists bridge the original 10 cm gap to the wall top, above all walkable space.
    for(int i=0;i<4;i++)Box(root,"Roof supporting joist",new Vector3(0,-size.y*.5f-.105f,-size.z*.5f+.14f+i*(size.z-.28f)/3),new Vector3(size.x,.23f,.12f),.018f,alloy);
    for(int i=1;i<5;i++)Box(root,"Roof standing seam",new Vector3(-size.x*.5f+size.x*i/5,size.y*.5f+.012f,0),new Vector3(.035f,.035f,size.z-.1f),.008f,alloy);
   }
   Combine(combinedRoot);
  }
  static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
  public void Window(Transform source){
   var r=source.GetComponent<MeshRenderer>();if(!r)return;var glass=new Material(Shader.Find("Standard")){name="Smoked pressure glazing",color=new Color(.085f,.16f,.19f)};glass.SetFloat("_Metallic",.42f);glass.SetFloat("_Glossiness",.82f);materials.Add(glass);r.sharedMaterial=glass;
   var size=Vector3.Scale(source.GetComponent<MeshFilter>().sharedMesh.bounds.size,Abs(source.lossyScale));var root=Root(source,"pressure window frame",r.bounds.center);var combinedRoot=root;
   if(size.x<size.z){var oriented=new GameObject("Window frame orientation").transform;oriented.SetParent(root,false);oriented.localRotation=Quaternion.Euler(0,90,0);root=oriented;size=new Vector3(size.z,size.y,size.x);}
   for(int side=-1;side<=1;side+=2){Box(root,"Window gasket",new Vector3(0,side*(size.y*.5f+.025f),0),new Vector3(size.x+.15f,.10f,size.z+.10f),.02f,dark);Box(root,"Frame jamb",new Vector3(side*(size.x*.5f+.025f),0,0),new Vector3(.10f,size.y+.15f,size.z+.10f),.02f,alloy);}
   for(int i=1;i<4;i++)Box(root,"Pressure mullion",new Vector3(-size.x*.5f+size.x*i/4,0,0),new Vector3(.045f,size.y,size.z+.05f),.01f,alloy);
   Combine(combinedRoot);
  }
  public void Crystal(Transform source){
   var f=source.GetComponent<MeshFilter>();var r=source.GetComponent<MeshRenderer>();if(!f||!r)return;var bounds=f.sharedMesh.bounds;var verts=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
   const int sides=7;for(int j=0;j<4;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,rad=j==3?.06f:j==2?.75f:1;verts.Add(bounds.center+Vector3.Scale(new Vector3(Mathf.Cos(a)*.5f*rad,j==0?-.5f:j==1?.20f:j==2?.36f:.5f,Mathf.Sin(a)*.5f*rad),bounds.size));uv.Add(new Vector2(i/(float)sides,j/3f));}
   for(int j=0;j<3;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;triangles.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
   var flatVerts=new List<Vector3>();var flatUv=new List<Vector2>();var flatTriangles=new List<int>();foreach(int index in triangles){flatTriangles.Add(flatVerts.Count);flatVerts.Add(verts[index]);flatUv.Add(uv[index]);}var mesh=new Mesh{name="Mineral growth facets"};mesh.SetVertices(flatVerts);mesh.SetUVs(0,flatUv);mesh.SetTriangles(flatTriangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();meshes.Add(mesh);f.sharedMesh=mesh;
   var m=new Material(Shader.Find("Standard")){name="Frosted mineral",color=new Color(.18f,.37f,.40f)};m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.45f);m.SetTexture("_BumpMap",RealismAssets.Texture("Textures/GroundNormal"));m.SetTextureScale("_BumpMap",Vector2.one*2);m.SetFloat("_BumpScale",.16f);m.EnableKeyword("_NORMALMAP");materials.Add(m);r.sharedMaterial=m;
  }
  void Box(Transform root,string name,Vector3 center,Vector3 size,float radius,Material mat){Part(root,name,RoundedBox(size,radius),mat,center,Quaternion.identity);}
  void Combine(Transform root){
   var groups=new Dictionary<Material,List<CombineInstance>>();var source=root.GetComponentsInChildren<MeshRenderer>();
   foreach(var r in source){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();groups.Add(r.sharedMaterial,list);}list.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});}
   foreach(var pair in groups){var m=new Mesh{name="Combined fabrication · "+pair.Key.name};m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.CombineMeshes(pair.Value.ToArray(),true,true);meshes.Add(m);Part(root,m.name,m,pair.Key,Vector3.zero,Quaternion.identity);}
   foreach(var r in source){r.enabled=false;Object.Destroy(r.gameObject);}
  }
  Mesh RoundedBox(Vector3 size,float radius){
   radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f);Vector3 half=size*.5f,inner=half-Vector3.one*radius;
   var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   for(int face=0;face<6;face++){int axis=face/2;float sign=face%2==0?1:-1;int u=(axis+1)%3,w=(axis+2)%3;int start=v.Count;float[] us={-half[u],-half[u]+radius*.29f,-inner[u],inner[u],half[u]-radius*.29f,half[u]};float[] ws={-half[w],-half[w]+radius*.29f,-inner[w],inner[w],half[w]-radius*.29f,half[w]};
    for(int j=0;j<6;j++)for(int i=0;i<6;i++){Vector3 p=Vector3.zero;p[axis]=half[axis]*sign;p[u]=us[i];p[w]=ws[j];Vector3 q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));Vector3 normal=(p-q).normalized;v.Add(q+normal*radius);n.Add(normal);uv.Add(new Vector2(p[u],p[w])*.9f);}
    for(int j=0;j<5;j++)for(int i=0;i<5;i++){int a=start+j*6+i,b=a+1,c=a+6,d=c+1;if(sign>0)tris.AddRange(new[]{a,b,c,b,d,c});else tris.AddRange(new[]{a,c,b,b,c,d});}
   }
   var mesh=new Mesh{name="Metre-scale rounded fabrication"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
  }
  void Tube(Transform root,string name,Vector3 a,Vector3 b,float radius,Material mat,int segments=20){
   var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();float len=(b-a).magnitude;
   for(int j=0;j<2;j++)for(int i=0;i<=segments;i++){float angle=i*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Cos(angle)*radius,j*len,Mathf.Sin(angle)*radius));uv.Add(new Vector2(i/(float)segments,j*len));}
   for(int i=0;i<segments;i++){int k=segments+1;tr.AddRange(new[]{i,i+k,i+1,i+1,i+k,i+k+1});}int bot=v.Count;v.Add(Vector3.zero);uv.Add(Vector2.zero);int top=v.Count;v.Add(Vector3.up*len);uv.Add(Vector2.one);for(int i=0;i<segments;i++)tr.AddRange(new[]{bot,i,i+1,top,i+segments+2,i+segments+1});
   var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateTangents();meshes.Add(mesh);Part(root,name,mesh,mat,a,Quaternion.FromToRotation(Vector3.up,b-a));
  }
  Mesh Dish(){var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int seg=64,rows=12;for(int side=0;side<2;side++)for(int j=0;j<=rows;j++)for(int i=0;i<=seg;i++){float r=.56f*j/rows,a=i*2*Mathf.PI/seg;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-.70f*r*r+side*.018f));uv.Add(new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r));}int layer=(seg+1)*(rows+1);for(int side=0;side<2;side++)for(int j=0;j<rows;j++)for(int i=0;i<seg;i++){int a=side*layer+j*(seg+1)+i,b=a+1,c=a+seg+1,d=c+1;if(side==0)tr.AddRange(new[]{a,c,b,b,c,d});else tr.AddRange(new[]{a,b,c,b,d,c});}for(int i=0;i<seg;i++){int a=rows*(seg+1)+i;tr.AddRange(new[]{a,a+1,a+layer,a+1,a+layer+1,a+layer});}var m=new Mesh{name="Parabolic aluminium reflector"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateTangents();meshes.Add(m);return m;}
  public void Dispose(){foreach(var m in meshes)if(m)Object.Destroy(m);foreach(var m in materials)if(m)Object.Destroy(m);}
 }
}
