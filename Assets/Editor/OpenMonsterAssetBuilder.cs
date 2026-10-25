using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Starfall;
using Object=UnityEngine.Object;

// Quaternius Ultimate Monsters (CC0), retained in ThirdParty with its original license.
// First editor import builds the runtime library; subsequent imports are idempotent.
[InitializeOnLoad]
public static class OpenMonsterAssetBuilder {
 const string Sources="Assets/ThirdParty/Quaternius/Monsters/";
 const string Art="Assets/Art/Creatures/OpenMonsters/";
 const string Runtime="Assets/Resources/OpenMonsters/";
 const string Version="quaternius-monsters-v2";
 static readonly string[] Ground={"Dino","Alien","BlueDemon","Yeti"};
 static readonly string[] Flying={"Dragon","Dragon_Evolved","Glub_Evolved"};
 static bool building;
 static OpenMonsterAssetBuilder(){if(!Application.isBatchMode)EditorApplication.delayCall+=AutoUpgrade;}
 static void AutoUpgrade(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=AutoUpgrade;return;}
  if(!File.Exists(Sources+"Dino.fbx"))return;
  bool current=CurrentVersion();
  if(current&&LibraryComplete()){
   bool upgraded=true;foreach(string name in new[]{"RiftMauler","SpineFloater"}){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Creatures/"+name+".prefab");if(prefab&&!prefab.GetComponent<ImportedCreatureAnimation>())upgraded=false;}
   if(upgraded)return;UpgradeCreaturePrefabs();AssetDatabase.SaveAssets();return;
  }
  try{RebuildAll();}catch(Exception exception){Debug.LogError("Open monster import could not finish. Use Starfall/Monsters/Rebuild CC0 Monster Library after import completes. "+exception);}
 }
 static T Save<T>(T value,string path)where T:Object{
  var prior=AssetDatabase.LoadAssetAtPath<T>(path);if(prior){EditorUtility.CopySerialized(value,prior);Object.DestroyImmediate(value);EditorUtility.SetDirty(prior);return prior;}
  AssetDatabase.CreateAsset(value,path);return value;
 }
 static bool Looping(string name){name=name.ToLowerInvariant();return name.Contains("idle")||name.Contains("walk")||name.Contains("run")||name.Contains("flying");}
 static bool CurrentVersion()=>File.Exists(Art+"BuildVersion.txt")&&File.ReadAllText(Art+"BuildVersion.txt").Trim()==Version;
 static bool LibraryComplete(){
  foreach(string name in Ground.Concat(Flying)){
   string path=Runtime+(Ground.Contains(name)?"Ground/":"Flying/")+name+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)return false;
   var animator=prefab.GetComponentInChildren<Animator>(true);if(!animator||!animator.runtimeAnimatorController)return false;
   var renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);if(renderers.Length==0)return false;
   foreach(var renderer in renderers)if(!renderer.enabled||!renderer.sharedMesh||renderer.sharedMesh.vertexCount==0||renderer.bones.Length==0||!renderer.sharedMaterial||!(renderer.sharedMaterial.mainTexture is Texture2D))return false;
  }return true;
 }
 static void ImportModel(string name){
  string path=Sources+name+".fbx";var importer=AssetImporter.GetAtPath(path)as ModelImporter;
  if(importer==null)throw new InvalidOperationException("Missing Quaternius model: "+path);
  importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  importer.importAnimation=true;importer.importBlendShapes=true;importer.importCameras=false;importer.importLights=false;
  importer.optimizeGameObjects=false;importer.addCollider=false;importer.meshCompression=ModelImporterMeshCompression.Off;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;
  var clips=importer.defaultClipAnimations;
  foreach(var clip in clips){clip.loopTime=Looping(clip.name);clip.loopPose=clip.loopTime;clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;}
  if(clips.Length>0)importer.clipAnimations=clips;importer.SaveAndReimport();
 }
 static AnimationClip Clip(AnimationClip[] clips,params string[] candidates){
  foreach(string candidate in candidates)foreach(var clip in clips){string simple=clip.name.Substring(clip.name.LastIndexOf('|')+1);if(string.Equals(simple,candidate,StringComparison.OrdinalIgnoreCase))return clip;}
  return clips.FirstOrDefault();
 }
 static AnimatorController Controller(string name,bool flying,AnimationClip[] clips){
  string path=Art+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
  var machine=controller.layers[0].stateMachine;foreach(var child in machine.states)machine.RemoveState(child.state);
  var idle=Clip(clips,flying?"Flying_Idle":"Idle","Idle","Flying_Idle");
  string[] names={"Idle","Move","Attack","Death","Hit"};
  AnimationClip[] motions={idle,Clip(clips,flying?"Fast_Flying":"Run",flying?"Flying_Idle":"Walk","Idle"),Clip(clips,flying?"Headbutt":"Punch","Punch","Attack","Idle"),Clip(clips,"Death","Idle"),Clip(clips,"HitReact","Idle")};
  for(int i=0;i<names.Length;i++){var state=machine.AddState(names[i],new Vector3(240,i*65,0));state.motion=motions[i]?motions[i]:idle;state.writeDefaultValues=true;if(i==0)machine.defaultState=state;}
  EditorUtility.SetDirty(controller);return controller;
 }
 static Material Skin(){
  string texturePath=Sources+"Atlas_Monsters.png";var importer=AssetImporter.GetAtPath(texturePath)as TextureImporter;
  if(importer!=null){importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
  var shader=Shader.Find("Starfall/OpenMonsterSkin");if(!shader)shader=Shader.Find("Standard");
  var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(!atlas)throw new InvalidOperationException("Quaternius atlas was not imported as a 2D texture: "+texturePath);
  var material=new Material(shader){name="Quaternius atlas hide"};material.mainTexture=atlas;material.color=new Color(.82f,.85f,.84f);
  material.SetFloat("_Glossiness",.24f);material.SetFloat("_Metallic",.02f);if(material.HasProperty("_Saturation"))material.SetFloat("_Saturation",.66f);
  material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);return Save(material,Art+"Quaternius atlas hide.mat");
 }
 static GameObject Appearance(string name,bool flying,Material skin){
  string sourcePath=Sources+name+".fbx";var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);if(!source)throw new InvalidOperationException("Unity did not import "+sourcePath);
  var clips=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
  if(clips.Length==0)throw new InvalidOperationException("Quaternius model has no imported animation clips: "+name);
  var controller=Controller(name,flying,clips);var wrapper=new GameObject(name);var fit=new GameObject("Artwork fit").transform;fit.SetParent(wrapper.transform,false);var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.transform.SetParent(fit,false);
  try{
   var idle=Clip(clips,flying?"Flying_Idle":"Idle","Idle");if(idle)idle.SampleAnimation(instance,0);
   var animator=instance.GetComponent<Animator>();if(!animator)animator=instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var renderers=instance.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidOperationException("Quaternius model has no renderers: "+name);
   Bounds bounds=PosedBounds(renderers[0]);foreach(var renderer in renderers){bounds.Encapsulate(PosedBounds(renderer));var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=skin;renderer.sharedMaterials=materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;if(renderer is SkinnedMeshRenderer skinned){skinned.updateWhenOffscreen=true;skinned.quality=SkinQuality.Bone4;}}
   float targetHeight=flying?1.7f:3.05f;float scale=targetHeight/Mathf.Max(.01f,bounds.size.y);
   if(flying)scale=Mathf.Min(scale,6.4f/Mathf.Max(.01f,bounds.size.x));
   // Fit above the Animator so imported animation curves cannot overwrite the placement.
   // Preserve the actor's origin, collision, and mouth coordinates. Artwork alone is fitted.
   fit.localScale=Vector3.one*scale;
   fit.localPosition=-new Vector3(bounds.center.x,flying?bounds.center.y:bounds.min.y,bounds.center.z)*scale;
   fit.localPosition+=flying?new Vector3(0,0,.05f):new Vector3(0,.03f,.32f);
   string path=Runtime+(flying?"Flying/":"Ground/")+name+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,path);
   long triangles=0;foreach(var renderer in renderers){Mesh mesh=renderer is SkinnedMeshRenderer smr?smr.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)for(int i=0;i<mesh.subMeshCount;i++)triangles+=(long)mesh.GetIndexCount(i)/3;}
   Debug.Log("OPEN_MONSTER_ASSET "+name+" renderers="+renderers.Length+" triangles="+triangles+" clips="+clips.Length+" scale="+scale.ToString("F3"));return prefab;
  }finally{Object.DestroyImmediate(wrapper);}
 }
 static Bounds PosedBounds(Renderer renderer){
  if(!(renderer is SkinnedMeshRenderer skinned))return renderer.bounds;
  // BakeMesh(false/default) already includes the renderer's scale. Compensate it
  // before applying localToWorldMatrix, or FBX unit scaling is counted twice.
  var baked=new Mesh();try{skinned.BakeMesh(baked,true);return baked.vertexCount>0?GeometryUtility.CalculateBounds(baked.vertices,skinned.transform.localToWorldMatrix):renderer.bounds;}finally{Object.DestroyImmediate(baked);}
 }
 public static void EnsureImportedAssets(){
  if(CurrentVersion()&&LibraryComplete())return;
  RebuildAll();
 }
 static void BuildLibrary(){
  Directory.CreateDirectory(Art);Directory.CreateDirectory(Runtime+"Ground");Directory.CreateDirectory(Runtime+"Flying");AssetDatabase.Refresh();
  foreach(string name in Ground.Concat(Flying))ImportModel(name);var skin=Skin();
  foreach(string name in Ground)Appearance(name,false,skin);foreach(string name in Flying)Appearance(name,true,skin);AssetDatabase.SaveAssets();
 }
 // Safe for newly built roots and LoadPrefabContents: no existing transforms/components are deleted.
 public static bool Apply(AlienCreature creature){
  if(!creature||!creature.visual)return false;
  string path=Runtime+(creature.kind==CreatureKind.RiftMauler?"Ground/Dino":"Flying/Dragon")+".prefab";
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source){if(!File.Exists(Sources+"Dino.fbx"))return false;EnsureImportedAssets();source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)return false;}
  var presentation=creature.GetComponent<ImportedCreatureAnimation>();if(!presentation)presentation=creature.gameObject.AddComponent<ImportedCreatureAnimation>();
  foreach(var renderer in creature.visual.GetComponentsInChildren<Renderer>(true)){if(renderer.transform==creature.core||renderer.transform.IsChildOf(creature.core))continue;if(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)renderer.enabled=false;}
  if(!presentation.artwork||presentation.installedPrefab!=source){if(presentation.artwork)Object.DestroyImmediate(presentation.artwork.gameObject);var artwork=(GameObject)PrefabUtility.InstantiatePrefab(source);artwork.name="Open source monster artwork";artwork.transform.SetParent(creature.visual,false);presentation.artwork=artwork.transform;presentation.installedPrefab=source;}
  foreach(var renderer in presentation.artwork.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
  EditorUtility.SetDirty(creature);EditorUtility.SetDirty(presentation);return true;
 }
 public static void UpgradeCreaturePrefabs(){
  foreach(string name in new[]{"RiftMauler","SpineFloater"}){
   string path="Assets/Art/Creatures/"+name+".prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
   var root=PrefabUtility.LoadPrefabContents(path);try{if(Apply(root.GetComponent<AlienCreature>()))PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
 }
 [MenuItem("Starfall/Monsters/Rebuild CC0 Monster Library")]
 public static void RebuildAll(){
  if(building)return;building=true;
  try{BuildLibrary();UpgradeCreaturePrefabs();File.WriteAllText(Art+"BuildVersion.txt",Version+Environment.NewLine);AssetDatabase.Refresh();AssetDatabase.SaveAssets();Debug.Log("STARFALL_OPEN_MONSTERS_BUILD_PASS: 7 CC0 models, animated resources, stable creature prefab roots. Scenes inherit prefabs or use the runtime adapter.");}
  finally{building=false;}
 }
}
