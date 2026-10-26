using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Starfall {
 // Opt-in player smoke test; no scene references or normal campaign saves required.
 public sealed class MonsterVisibilityVerification:MonoBehaviour {
  static bool Requested=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-monsterVisibilityQA");
  readonly List<string> report=new List<string>();
  string folder;int failures;FirstPersonExplorer explorer;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install(){
   if(!Requested||FindFirstObjectByType<MonsterVisibilityVerification>())return;
   var root=new GameObject("Monster visibility verification - opt in");
   DontDestroyOnLoad(root);root.AddComponent<MonsterVisibilityVerification>();
  }

  IEnumerator Start(){
   if(!Requested)yield break;
   Application.runInBackground=true;Time.timeScale=1;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"../QA/Reports/MonsterVisibility":"../../../QA/Reports/MonsterVisibility"));
   Directory.CreateDirectory(folder);
   var run=Run();
   while(true){
    bool more;object current=null;
    try{more=run.MoveNext();if(more)current=run.Current;}
    catch(Exception exception){Check("Unhandled runtime verification: "+exception,false);break;}
    if(!more)break;yield return current;
   }
   report.Add(failures==0?"STARFALL_MONSTER_VISIBILITY_QA_PASS":"STARFALL_MONSTER_VISIBILITY_QA_FAIL failures="+failures);
   File.WriteAllLines(Path.Combine(folder,"gameplay-report.txt"),report);
   Debug.Log(report[report.Count-1]);Application.Quit(failures==0?0:2);
  }

  IEnumerator Run(){
   yield return null;
   var director=FindFirstObjectByType<SceneDirector>();
   Require(director&&director.battle,"Scene director and battle travel exist");
   Require(!CampaignRuntime.Current,"Normal campaign runtime and saves are disabled for this QA parameter");
   director.battle.Enter(null);
   float deadline=Time.realtimeSinceStartup+12;
   while((!director.battle.Explorer||director.battle.IsTransitioning)&&Time.realtimeSinceStartup<deadline)yield return null;
   explorer=director.battle.Explorer;
   Require(explorer&&explorer.Ready&&explorer.arena&&!director.battle.IsTransitioning,"Real battle scene and explorer are ready");
   explorer.Automated=true;explorer.SetPaused(false);explorer.Motor.enabled=false;explorer.enabled=false;
   foreach(var attack in FindObjectsByType<TitanAreaAttack>(FindObjectsSortMode.None))attack.AttackEnabled=false;
   AlienCreature ground=null,flying=null;
   foreach(var creature in explorer.arena.creatures){
    if(!creature)continue;
    creature.enabled=false;
    if(creature.Agent&&creature.Agent.enabled&&creature.Agent.isOnNavMesh){creature.Agent.isStopped=true;creature.Agent.ResetPath();}
    var elite=creature.GetComponent<EliteCreature>();if(elite)elite.enabled=false;
    if(creature.kind==CreatureKind.RiftMauler&&!ground)ground=creature;
    if(creature.kind==CreatureKind.SpineFloater&&!flying)flying=creature;
   }
   Require(ground&&flying,"Battle contains a ground monster and a flying monster");
   Place(ground,new Vector3(-3,0,0));Place(flying,new Vector3(3,0,1));
   explorer.arena.ClearBolts();
   var actors=new[]{ground,flying};var animators=new Animator[2];var presentations=new ImportedCreatureAnimation[2];
   for(int i=0;i<actors.Length;i++){
    presentations[i]=ImportedCreatureAnimation.EnsureFor(actors[i]);
    Require(presentations[i]&&presentations[i].artwork,"Runtime presentation exists for "+actors[i].kind);
    animators[i]=presentations[i].artwork.GetComponentInChildren<Animator>(true);
    Require(animators[i]&&animators[i].isActiveAndEnabled&&animators[i].runtimeAnimatorController,"Live Animator exists for "+actors[i].kind);
   }
   yield return new WaitForSecondsRealtime(.4f);
   for(int i=0;i<actors.Length;i++){
    CheckArtwork(actors[i],presentations[i],"Idle");
    Require(animators[i].GetCurrentAnimatorStateInfo(0).IsName("Idle"),actors[i].kind+" runtime adapter selects Idle");
    presentations[i].enabled=false;
    Require(animators[i].HasState(0,Animator.StringToHash("Move")),actors[i].kind+" has live Move state");
    animators[i].speed=1;animators[i].Play("Move",0,0);animators[i].Update(0);
   }
   yield return new WaitForSecondsRealtime(.45f);
   for(int i=0;i<actors.Length;i++){
    Require(animators[i].GetCurrentAnimatorStateInfo(0).IsName("Move")&&animators[i].GetCurrentAnimatorStateInfo(0).normalizedTime>.01f,
     actors[i].kind+" Move animation advances in the player");
    CheckArtwork(actors[i],presentations[i],"Move");
   }
   var groundBounds=PosedBounds(presentations[0].artwork);var flyingBounds=PosedBounds(presentations[1].artwork);
   var combined=groundBounds;combined.Encapsulate(flyingBounds);
   Aim(combined.center+new Vector3(0,1,-14),combined.center);
   Capture("gameplay",null);
   Aim(groundBounds.center+new Vector3(3,1.4f,-7.5f),groundBounds.center);
   Capture("ground",presentations[0].artwork);
   Aim(flyingBounds.center+new Vector3(3,1.4f,-8.5f),flyingBounds.center);
   Capture("flying",presentations[1].artwork);
  }

  void Place(AlienCreature creature,Vector3 desired){
   desired.y=AlienSurfaceLandmarks.Height(desired.x,desired.z);
   Require(NavMesh.SamplePosition(desired,out var hit,8,NavMesh.AllAreas),creature.kind+" placement is on baked navigation");
   Require(creature.Agent&&creature.Agent.enabled,creature.kind+" navigation agent exists");
   Require(creature.Agent.Warp(hit.position),creature.kind+" navigation warp succeeds");
   creature.Agent.isStopped=true;creature.Agent.ResetPath();creature.Agent.updateRotation=false;
   creature.transform.rotation=Quaternion.Euler(0,180,0);
   Physics.SyncTransforms();
  }

  void CheckArtwork(AlienCreature actor,ImportedCreatureAnimation presentation,string state){
   Require(presentation.artwork.gameObject.activeInHierarchy,actor.kind+" "+state+" artwork is active");
   var renderers=presentation.artwork.GetComponentsInChildren<Renderer>(true);
   Require(renderers.Length>0,actor.kind+" "+state+" artwork has renderers");
   foreach(var renderer in renderers)Require(renderer.enabled&&renderer.gameObject.activeInHierarchy,actor.kind+" "+state+" renderer is enabled");
   Bounds bounds=PosedBounds(presentation.artwork);
   report.Add(actor.kind+" "+state+" actual posed bounds center="+bounds.center.ToString("F3")+" size="+bounds.size.ToString("F3")+" actor="+actor.transform.position.ToString("F3"));
   Require(Finite(bounds.center)&&Finite(bounds.size),actor.kind+" "+state+" bounds are finite");
   Require(bounds.size.y>(actor.kind==CreatureKind.RiftMauler?1f:.3f)&&bounds.size.y<6.5f&&bounds.size.x>.3f&&bounds.size.x<10f,
    actor.kind+" "+state+" has visible gameplay-scale geometry");
   Require(Vector3.Distance(bounds.center,actor.transform.position)<6f,actor.kind+" "+state+" geometry remains near its actor");
  }

  static Bounds PosedBounds(Transform artwork){
   var result=new Bounds();bool found=false;
   foreach(var renderer in artwork.GetComponentsInChildren<Renderer>(true)){
    Bounds current=renderer.bounds;
    if(renderer is SkinnedMeshRenderer skinned){
     var mesh=new Mesh();
     try{
      skinned.BakeMesh(mesh,false);
      if(mesh.vertexCount==0)throw new InvalidOperationException("Empty baked mesh: "+renderer.name);
      // Unity 6000.3 false already includes renderer scale; apply position/rotation once.
      current=GeometryUtility.CalculateBounds(mesh.vertices,Matrix4x4.TRS(renderer.transform.position,renderer.transform.rotation,Vector3.one));
     }finally{Destroy(mesh);}
    }
    if(!found){result=current;found=true;}else result.Encapsulate(current);
   }
   if(!found)throw new InvalidOperationException("Artwork contains no renderers");return result;
  }

  void Aim(Vector3 position,Vector3 target){
   var camera=explorer.viewCamera;camera.transform.position=position;camera.transform.LookAt(target);
   camera.fieldOfView=60;camera.nearClipPlane=.1f;camera.rect=new Rect(0,0,1,1);
  }

  void Capture(string name,Transform artwork){
   var camera=explorer.viewCamera;
   var image=Render(camera);
   try{
    File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
    Require(image.width==1280&&image.height==720,"Game camera renders "+name+".png");
    if(!artwork)return;
    var renderers=artwork.GetComponentsInChildren<Renderer>();var prior=new bool[renderers.Length];Texture2D hidden=null;
    try{
     for(int i=0;i<renderers.Length;i++){prior[i]=renderers[i].enabled;renderers[i].enabled=false;}
     hidden=Render(camera);var visiblePixels=image.GetPixels32();var hiddenPixels=hidden.GetPixels32();int differences=0;
     for(int i=0;i<visiblePixels.Length;i++){
      var a=visiblePixels[i];var b=hiddenPixels[i];
      if(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)>24)differences++;
     }
     report.Add(name+" actual artwork pixel coverage="+differences+"/"+visiblePixels.Length);
     Require(differences>150,name+" artwork covers actual game camera pixels");
    }finally{
     for(int i=0;i<renderers.Length;i++)renderers[i].enabled=prior[i];if(hidden)Destroy(hidden);
    }
   }finally{Destroy(image);}
  }

  static Texture2D Render(Camera camera){
   var target=RenderTexture.GetTemporary(1280,720,24);var oldTarget=camera.targetTexture;var active=RenderTexture.active;Texture2D image=null;
   try{
    camera.targetTexture=target;camera.Render();RenderTexture.active=target;
    image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();return image;
   }catch{if(image)Destroy(image);throw;}
   finally{camera.targetTexture=oldTarget;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
  }

  static bool Finite(Vector3 value){return !(float.IsNaN(value.x)||float.IsInfinity(value.x)||float.IsNaN(value.y)||float.IsInfinity(value.y)||float.IsNaN(value.z)||float.IsInfinity(value.z));}
  void Check(string message,bool pass){report.Add((pass?"PASS ":"FAIL ")+message);if(!pass)failures++;Debug.Log("MONSTER_VISIBILITY_QA "+report[report.Count-1]);}
  void Require(bool condition,string message){Check(message,condition);if(!condition)throw new InvalidOperationException(message);}
 }
}
