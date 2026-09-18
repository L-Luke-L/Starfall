using UnityEngine;
using UnityEditor;
using Starfall;

public static class WormholeBuilder {
 const string Folder="Assets/Art/Wormhole/";
 static Material Mat(string file,Shader shader){var p=Folder+file+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,p);}else {m.shader=shader;EditorUtility.SetDirty(m);}return m;}
 public static void Configure(SceneDirector director){
  System.IO.Directory.CreateDirectory(Folder);
  var portal=director.GetComponent<ExpeditionWormhole>();if(!portal)portal=director.gameObject.AddComponent<ExpeditionWormhole>();
  portal.director=director;portal.travel=director.battle;director.wormhole=portal;
  if(portal.portalVisual)Object.DestroyImmediate(portal.portalVisual);
  var root=new GameObject("Persistent wormhole · independent portal");root.transform.position=new Vector3(4.3f,director.surfaceY,-.5f);portal.portalVisual=root;
  var site=root.AddComponent<Interactable>();site.kind=SiteKind.Wormhole;site.displayName="进入虫洞";site.description="重返断崖盆地";site.radius=1.5f;director.wormholeSite=site;
  var lens=GameObject.CreatePrimitive(PrimitiveType.Quad);lens.name="Animated spacetime lens";lens.transform.SetParent(root.transform,false);lens.transform.localPosition=new Vector3(0,1.53f,0);lens.transform.localScale=new Vector3(2.1f,3.8f,1);Object.DestroyImmediate(lens.GetComponent<Collider>());var lensRenderer=lens.GetComponent<MeshRenderer>();lensRenderer.sharedMaterial=Mat("SpacetimeLens",Shader.Find("Starfall/Wormhole"));lensRenderer.sortingOrder=30;
  var lineMat=Mat("OrbitFilaments",Shader.Find("Sprites/Default"));portal.arcs=new Transform[3];
  for(int j=0;j<3;j++){
   var ellipse=new GameObject("Elliptical energy orbit "+j).transform;ellipse.SetParent(root.transform,false);ellipse.localPosition=new Vector3(0,1.53f,-.02f);ellipse.localScale=new Vector3(.82f,1.45f,1);
   var arc=new GameObject("Rotating arc "+j).transform;arc.SetParent(ellipse,false);portal.arcs[j]=arc;var line=arc.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=lineMat;line.useWorldSpace=false;line.positionCount=31;line.startWidth=.015f;line.endWidth=.004f;line.sortingOrder=31;line.startColor=new Color(.39f,.89f,.93f,.6f);line.endColor=new Color(.15f,.28f,.49f,.08f);
   for(int i=0;i<31;i++){float a=i/30f*Mathf.PI*.83f;float r=1.04f+j*.045f;line.SetPosition(i,new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));}
  }
  var glow=new GameObject("Soft contact light");glow.transform.SetParent(root.transform,false);glow.transform.localPosition=new Vector3(0,.06f,-.04f);glow.transform.localScale=new Vector3(2.2f,.24f,1);var sr=glow.AddComponent<SpriteRenderer>();sr.sprite=director.glow;sr.color=new Color(.12f,.63f,.77f,.35f);sr.sortingOrder=29;
  root.SetActive(false);
 }
}
