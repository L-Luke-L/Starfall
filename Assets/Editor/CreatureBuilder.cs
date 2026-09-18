using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Starfall;

public static class CreatureBuilder {
 const string Folder="Assets/Art/Creatures/";
 static Mesh pebble,spike,wing;static Material shell,plate,joint,amber,teal,membrane,black,warningMat;
 static T Save<T>(T value,string name) where T:Object{var path=Folder+name;var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior){EditorUtility.CopySerialized(value,prior);Object.DestroyImmediate(value);EditorUtility.SetDirty(prior);return prior;}AssetDatabase.CreateAsset(value,path);return value;}
 static Material Mat(string name,Color color,float glow=0){var m=new Material(Shader.Find("Standard"));m.name=name;m.color=color;m.SetFloat("_Glossiness",.25f);m.SetFloat("_Metallic",.16f);if(glow>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*glow);}return Save(m,name+".mat");}
 static Transform Group(string name,Transform parent,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
 static Transform Part(string name,Transform parent,Vector3 pos,Vector3 scale,Mesh mesh,Material material){var t=Group(name,parent,pos);t.localScale=scale;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
 static Transform Bone(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material){var p=Part(name,parent,(a+b)/2,new Vector3(radius,(b-a).magnitude*.5f,radius),pebble,material);p.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);return p;}
 static Mesh Facet(Vector3[] vertices,int[] indices,string name){var v=new List<Vector3>();var t=new List<int>();foreach(int i in indices){t.Add(v.Count);v.Add(vertices[i]);}var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
 static Mesh Stone(){var v=new List<Vector3>();var t=new List<int>();const int n=10,rows=6;for(int r=0;r<=rows;r++){float p=Mathf.PI*r/rows;for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a)));}}for(int r=0;r<rows;r++)for(int i=0;i<n;i++){int a=r*n+i,b=r*n+(i+1)%n;t.AddRange(new[]{a,b,a+n,b,b+n,a+n});}return Facet(v.ToArray(),t.ToArray(),"Faceted organic segment");}
 static Mesh Spike(){var v=new List<Vector3>{new Vector3(0,1,0)};for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;v.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));}var t=new List<int>();for(int i=0;i<7;i++)t.AddRange(new[]{0,1+(i+1)%7,1+i});return Facet(v.ToArray(),t.ToArray(),"Chitin spike");}
 static void Materials(){Directory.CreateDirectory(Folder);pebble=Save(Stone(),"OrganicSegment.asset");spike=Save(Spike(),"ChitinSpike.asset");wing=Save(Facet(new[]{new Vector3(0,0,-.65f),new Vector3(0,.12f,.8f),new Vector3(1.5f,.25f,-.2f),new Vector3(.4f,-.1f,-.3f)},new[]{0,1,2,0,2,3,1,3,2,0,3,1},"Swept fin"),"SweptFin.asset");shell=Mat("Basalt hide",new Color(.17f,.20f,.23f));plate=Mat("Oxidized carapace",new Color(.39f,.25f,.15f));joint=Mat("Dark sinew",new Color(.065f,.075f,.09f));amber=Mat("Molten sensory organs",new Color(.93f,.29f,.055f),1.8f);teal=Mat("Plasma organs",new Color(.10f,.75f,.76f),1.7f);membrane=Mat("Blue violet chitin",new Color(.19f,.25f,.39f));black=Mat("Obsidian claws",new Color(.055f,.065f,.075f));warningMat=Mat("Charge ground trace",new Color(.8f,.23f,.06f),1);}
 static AlienCreature Model(CreatureKind kind){
  var root=Group(kind==CreatureKind.RiftMauler?"玄脊地龙 · Basalt Drake":"苍翼飞龙 · Azure Wyvern",null,Vector3.zero);var c=root.gameObject.AddComponent<AlienCreature>();c.kind=kind;
  var agent=root.GetComponent<NavMeshAgent>();agent.radius=kind==CreatureKind.RiftMauler?.85f:.75f;agent.height=kind==CreatureKind.RiftMauler?3.2f:1.6f;agent.baseOffset=kind==CreatureKind.RiftMauler?0:3;agent.acceleration=10;agent.stoppingDistance=kind==CreatureKind.RiftMauler?3f:.7f;agent.enabled=false;agent.autoBraking=true;agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;agent.avoidancePriority=kind==CreatureKind.RiftMauler?40:55;
  var collider=root.gameObject.AddComponent<CapsuleCollider>();collider.radius=kind==CreatureKind.RiftMauler?.85f:.75f;collider.height=kind==CreatureKind.RiftMauler?3.1f:1.6f;collider.center=Vector3.up*(kind==CreatureKind.RiftMauler?1.6f:.15f);
  c.visual=Group("Articulated body",root,Vector3.zero);var limbs=new List<Transform>();var fins=new List<Transform>();
  if(kind==CreatureKind.RiftMauler){
   Part("Muscular reptile chest",c.visual,new Vector3(0,1.65f,-.3f),new Vector3(.78f,1.04f,1.05f),pebble,shell);
   Part("Armored belly",c.visual,new Vector3(0,1.45f,.45f),new Vector3(.56f,.83f,.4f),pebble,plate);
   Bone("Rising neck",c.visual,new Vector3(0,2,.3f),new Vector3(0,2.65f,.95f),.48f,shell);
   Part("Dragon skull",c.visual,new Vector3(0,2.63f,1),new Vector3(.54f,.43f,.62f),pebble,shell);
   Part("Long crocodilian muzzle",c.visual,new Vector3(0,2.49f,1.58f),new Vector3(.43f,.26f,.69f),pebble,shell);
   Part("Mouth separation",c.visual,new Vector3(0,2.26f,1.58f),new Vector3(.39f,.06f,.60f),pebble,joint);
   Part("Strong lower jaw",c.visual,new Vector3(0,2.14f,1.51f),new Vector3(.40f,.12f,.63f),pebble,plate);
   for(int side=-1;side<=1;side+=2){
    Part("Single amber eye",c.visual,new Vector3(side*.45f,2.72f,1.33f),new Vector3(.065f,.08f,.14f),pebble,amber);
    Bone("Heavy brow",c.visual,new Vector3(side*.42f,2.84f,1.05f),new Vector3(side*.43f,2.83f,1.56f),.11f,plate);
    var horn=Part("Swept skull horn",c.visual,new Vector3(side*.4f,2.92f,.68f),new Vector3(.16f,.57f,.17f),spike,plate);horn.localRotation=Quaternion.Euler(-36,0,-side*17);
    for(int j=0;j<4;j++){var tooth=Part("Ivory tooth",c.visual,new Vector3(side*.32f,2.31f,1.27f+j*.21f),new Vector3(.044f,.14f,.047f),spike,plate);tooth.localRotation=Quaternion.Euler(180,0,0);}
    var hip=Group("Powerful hind leg "+side,c.visual,new Vector3(side*.60f,1.35f,-.49f));limbs.Add(hip);
    Bone("Haunch",hip,Vector3.zero,new Vector3(side*.18f,-.61f,.25f),.40f,shell);
    Bone("Hock",hip,new Vector3(side*.18f,-.61f,.25f),new Vector3(side*.17f,-1.12f,-.08f),.23f,plate);
    Part("Weight bearing foot",hip,new Vector3(side*.16f,-1.2f,.18f),new Vector3(.29f,.15f,.53f),pebble,shell);
    for(int j=0;j<3;j++)Part("Foot talon",hip,new Vector3(side*.16f+(j-1)*.18f,-1.23f,.62f),new Vector3(.075f,.06f,.22f),pebble,black);
    var arm=Group("Short forearm "+side,c.visual,new Vector3(side*.62f,2.05f,.36f));
    Bone("Upper arm",arm,Vector3.zero,new Vector3(side*.16f,-.35f,.25f),.19f,shell);Bone("Reaching wrist",arm,new Vector3(side*.16f,-.35f,.25f),new Vector3(side*.11f,-.46f,.66f),.13f,plate);
    for(int j=0;j<3;j++)Part("Hand claw",arm,new Vector3(side*.11f+(j-1)*.1f,-.48f,.8f),new Vector3(.04f,.08f,.18f),pebble,black);
   }
   for(int j=0;j<7;j++){float z=-1.18f+j*.29f;var dorsal=Part("Jagged dorsal plate",c.visual,new Vector3(0,2.2f+Mathf.Sin(j*.4f)*.31f,z),new Vector3(.10f,.72f,.37f),spike,plate);dorsal.localRotation=Quaternion.Euler(-24,0,0);}
   Vector3 prev=new Vector3(0,1.1f,-1.08f);for(int j=0;j<6;j++){var next=new Vector3(Mathf.Sin(j*.35f)*.48f,1.03f-j*.13f,-1.6f-j*.53f);Bone("Counterbalancing reptile tail",c.visual,prev,next,.40f-j*.06f,shell);prev=next;}
   c.core=Part("Recessed throat ember",c.visual,new Vector3(0,2.30f,1.54f),new Vector3(.16f,.04f,.25f),pebble,amber);c.mouth=Group("Bite origin",c.visual,new Vector3(0,2.28f,2.18f));
  }else{
   Part("Streamlined drake torso",c.visual,new Vector3(0,0,-.32f),new Vector3(.51f,.58f,1.17f),pebble,membrane);
   Part("Pale throat keel",c.visual,new Vector3(0,-.28f,.20f),new Vector3(.31f,.31f,.8f),pebble,plate);
   Bone("Arched dragon neck",c.visual,new Vector3(0,.15f,.45f),new Vector3(0,.46f,1.1f),.31f,membrane);
   Part("Angular dragon head",c.visual,new Vector3(0,.47f,1.27f),new Vector3(.34f,.31f,.50f),pebble,shell);
   Part("Long drake snout",c.visual,new Vector3(0,.36f,1.70f),new Vector3(.25f,.17f,.43f),pebble,membrane);
   Part("Drake lower jaw",c.visual,new Vector3(0,.15f,1.61f),new Vector3(.24f,.09f,.46f),pebble,plate);
   for(int side=-1;side<=1;side+=2){
    Part("Single cyan eye",c.visual,new Vector3(side*.29f,.54f,1.52f),new Vector3(.055f,.065f,.095f),pebble,teal);
    var horn=Part("Swept dragon horn",c.visual,new Vector3(side*.25f,.67f,1.05f),new Vector3(.10f,.6f,.12f),spike,plate);horn.localRotation=Quaternion.Euler(-48,0,-side*24);
    var fin=Group("Articulated dragon wing "+side,c.visual,new Vector3(side*.40f,.2f,-.15f));fins.Add(fin);
    Vector3 shoulder=Vector3.zero,elbow=new Vector3(side*1.24f,.36f,.42f),tip=new Vector3(side*3.25f,.10f,-.4f);
    Bone("Wing humerus",fin,shoulder,elbow,.15f,shell);Bone("Long wing radius",fin,elbow,tip,.09f,plate);
    Vector3[] ends={tip,new Vector3(side*2.4f,-.05f,-1.40f),new Vector3(side*1.3f,-.16f,-1.75f),new Vector3(side*.1f,-.18f,-1.05f)};
    for(int j=0;j<3;j++){var mesh=Facet(new[]{elbow,ends[j],ends[j+1]},new[]{0,1,2,2,1,0},"Taut dragon wing membrane");Part("Independent wing panel",fin,Vector3.zero,Vector3.one,Save(mesh,"DrakeWing_"+side+"_"+j+".asset"),membrane);Bone("Wing finger",fin,elbow,ends[j],.055f,plate);}
    var leg=Group("Tucked hind leg "+side,c.visual,new Vector3(side*.35f,-.32f,-.65f));limbs.Add(leg);Bone("Thigh",leg,Vector3.zero,new Vector3(side*.16f,-.45f,.2f),.16f,membrane);Bone("Folded shin",leg,new Vector3(side*.16f,-.45f,.2f),new Vector3(side*.14f,-.54f,-.24f),.09f,plate);
    for(int j=0;j<3;j++)Part("Curled hind talon",leg,new Vector3(side*.15f+(j-1)*.10f,-.57f,-.08f),new Vector3(.045f,.09f,.21f),pebble,black);
   }
   Vector3 prev=new Vector3(0,0,-1.2f);for(int j=0;j<6;j++){var next=new Vector3(Mathf.Sin(j*.5f)*.36f,j*.035f,-1.5f-j*.49f);Bone("Long tapering dragon tail",c.visual,prev,next,.25f-j*.035f,membrane);prev=next;}
   for(int j=0;j<6;j++)Part("Back crest",c.visual,new Vector3(0,.43f,-1+j*.32f),new Vector3(.055f,.30f,.14f),spike,plate);
   c.core=Part("Recessed plasma throat",c.visual,new Vector3(0,.24f,1.72f),new Vector3(.12f,.05f,.17f),pebble,teal);c.mouth=Group("Dragon breath origin",c.visual,new Vector3(0,.25f,2.06f));
  }
  c.limbs=limbs.ToArray();c.fins=fins.ToArray();c.glow=Group("Attack tell light",c.core,Vector3.zero).gameObject.AddComponent<Light>();c.glow.type=LightType.Point;c.glow.color=kind==CreatureKind.RiftMauler?new Color(1,.25f,.03f):new Color(.1f,1,1);c.glow.range=4;c.glow.intensity=1;
  c.warning=Group("Committed charge trajectory",root,Vector3.zero).gameObject.AddComponent<LineRenderer>();c.warning.sharedMaterial=warningMat;c.warning.positionCount=20;c.warning.startWidth=.17f;c.warning.endWidth=.34f;c.warning.useWorldSpace=true;c.warning.enabled=false;c.warning.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+(kind==CreatureKind.RiftMauler?"RiftMauler":"SpineFloater")+".prefab");Object.DestroyImmediate(root.gameObject);return prefab.GetComponent<AlienCreature>();
 }
 public static void Install(FirstPersonExplorer player,AlienSurfaceLandmarks landmarks){
  Materials();var ground=Model(CreatureKind.RiftMauler);var flyer=Model(CreatureKind.SpineFloater);
  var arena=new GameObject("Creature encounter · navigation and actors").AddComponent<CreatureArena>();arena.player=player;arena.beacon=landmarks.returnBeacon;arena.projectileMaterial=teal;arena.impactMaterial=amber;player.arena=arena;
  // Only static environmental colliders contribute; actors and the first-person capsule are excluded.
  var sources=new List<NavMeshBuildSource>();foreach(var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){
   if(col.GetComponentInParent<FirstPersonExplorer>())continue;
   if(col is MeshCollider mesh&&mesh.sharedMesh)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh.sharedMesh,transform=mesh.transform.localToWorldMatrix,area=0});
   else if(col is BoxCollider box)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(box.transform.TransformPoint(box.center),box.transform.rotation,box.transform.lossyScale),size=box.size,area=1});
  }
  var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.95f;settings.agentHeight=2;settings.agentClimb=.45f;settings.agentSlope=42;settings.overrideVoxelSize=true;settings.voxelSize=.22f;settings.overrideTileSize=true;settings.tileSize=128;
  var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(238,90,238)),Vector3.zero,Quaternion.identity);if(!data)throw new System.Exception("Creature navigation bake failed");arena.navigation=Save(data,"BasinNavigation.asset");
  Vector2[] anchors={new Vector2(-24,-12),new Vector2(20,1),new Vector2(-49,30),new Vector2(8,50),new Vector2(48,39),new Vector2(-39,-48)};
  arena.creatures=new AlienCreature[anchors.Length];var navigation=NavMesh.AddNavMeshData(arena.navigation);
  try{
   if(!NavMesh.SamplePosition(landmarks.arrival.position,out var arrival,6,NavMesh.AllAreas))throw new System.Exception("No walkable arrival point");
   for(int i=0;i<anchors.Length;i++){
    bool found=false;Vector3 position=Vector3.zero;var path=new NavMeshPath();
    for(int attempt=0;attempt<25;attempt++){
     float angle=attempt*2.4f,radius=attempt==0?0:2+attempt*.3f;float x=anchors[i].x+Mathf.Cos(angle)*radius,z=anchors[i].y+Mathf.Sin(angle)*radius;
     if(!NavMesh.SamplePosition(new Vector3(x,AlienSurfaceLandmarks.Height(x,z),z),out var hit,4,NavMesh.AllAreas))continue;
     if(CreatureArena.FlatDistance(hit.position,landmarks.returnBeacon.position)<28)continue;
     bool spaced=true;for(int j=0;j<i;j++)if(CreatureArena.FlatDistance(hit.position,arena.creatures[j].transform.position)<32)spaced=false;
     if(!spaced||!NavMesh.CalculatePath(arrival.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
     position=hit.position;found=true;break;
    }
    if(!found)throw new System.Exception("No connected, spaced spawn for creature "+i);
    var prefab=i%2==0?ground:flyer;var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);actor.name=prefab.name+" · Territory "+(i+1);actor.transform.SetParent(arena.transform);actor.transform.position=position;
    var c=actor.GetComponent<AlienCreature>();c.arena=arena;c.patrolRadius=11;c.detectionRange=22;c.leashRadius=40;arena.creatures[i]=c;
   }
  }finally{navigation.Remove();}
 }
}
