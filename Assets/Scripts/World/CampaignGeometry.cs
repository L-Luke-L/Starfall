using System.Collections.Generic;
using UnityEngine;
namespace Starfall {
 // A reusable faceted armor profile; meshes remain independent from interaction colliders.
 public static class CampaignGeometry {
  public static Mesh Armor(){
   Vector2[] ring={new Vector2(-.32f,-.5f),new Vector2(.32f,-.5f),new Vector2(.5f,-.32f),new Vector2(.5f,.32f),new Vector2(.32f,.5f),new Vector2(-.32f,.5f),new Vector2(-.5f,.32f),new Vector2(-.5f,-.32f)};
   var vertices=new List<Vector3>();var triangles=new List<int>();
   for(int i=0;i<8;i++){var a=ring[i];var b=ring[(i+1)%8];int k=vertices.Count;vertices.Add(new Vector3(a.x,a.y,-.5f));vertices.Add(new Vector3(a.x,a.y,.5f));vertices.Add(new Vector3(b.x,b.y,.5f));vertices.Add(new Vector3(b.x,b.y,-.5f));triangles.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});
    k=vertices.Count;vertices.Add(new Vector3(0,0,-.5f));vertices.Add(new Vector3(b.x,b.y,-.5f));vertices.Add(new Vector3(a.x,a.y,-.5f));triangles.AddRange(new[]{k,k+1,k+2});
    k=vertices.Count;vertices.Add(new Vector3(0,0,.5f));vertices.Add(new Vector3(a.x,a.y,.5f));vertices.Add(new Vector3(b.x,b.y,.5f));triangles.AddRange(new[]{k,k+1,k+2});
   }
   var m=new Mesh{name="Faceted alloy plate"};m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;
  }
 }
}
