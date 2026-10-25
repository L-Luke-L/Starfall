using System;
using System.Collections.Generic;
using UnityEngine;
namespace Starfall {
 [Serializable] public sealed class RealismCatalog {public RealismModel[] models;}
 [Serializable] public sealed class RealismModel {public string id;public RealismSurface[] materials;}
 [Serializable] public sealed class RealismSurface {public string name,albedo,normal,metalSmooth,ao,emission;public float[] color,emissive;public float metallic,smoothness;public bool doubleSided,alphaClip;public float alphaCutoff;}
 public static class RealismAssets {
  const string Root="StarfallRealism/";
  static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
  static readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
  static bool ready;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){materials.Clear();models.Clear();ready=false;}
  public static Texture2D Texture(string name)=>Resources.Load<Texture2D>(Root+name);
  static void Initialize(){if(ready)return;ready=true;var json=Resources.Load<TextAsset>(Root+"catalog");if(!json){Debug.LogError("Starfall realism catalog missing; retaining original artwork.");return;}
   foreach(var model in JsonUtility.FromJson<RealismCatalog>(json.text).models)foreach(var data in model.materials){
    var m=new Material(Resources.Load<Shader>(Root+"RealismPBR")){name=data.name,enableInstancing=true};
    m.color=data.color!=null&&data.color.Length==4?new Color(data.color[0],data.color[1],data.color[2],data.color[3]):Color.white;
    m.SetFloat("_Cull",data.doubleSided?0:2);m.SetFloat("_AlphaClip",data.alphaClip?1:0);m.SetFloat("_Cutoff",data.alphaCutoff);m.mainTexture=Texture(data.albedo);m.SetFloat("_Metallic",data.metallic);m.SetFloat("_Glossiness",data.smoothness);
    var normal=Texture(data.normal);if(normal){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");}
    var metallic=Texture(data.metalSmooth);if(metallic){m.SetTexture("_MetallicGlossMap",metallic);m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",1);}
    var ao=Texture(data.ao);if(ao)m.SetTexture("_OcclusionMap",ao);
    var emissive=Texture(data.emission);if(emissive){m.SetTexture("_EmissionMap",emissive);m.SetColor("_EmissionColor",Color.white);m.EnableKeyword("_EMISSION");}
    if(model.id=="Dinosaur"||model.id=="Wyvern"||model.id=="AncientDragon"){
     m.SetFloat("_MetalMapScale",0);m.SetFloat("_GlossMapScale",.42f);m.SetFloat("_BumpScale",.65f);
     m.SetTexture("_DetailNormalMap",Texture("Textures/ScaleDetail_Normal"));m.SetTextureScale("_DetailNormalMap",Vector2.one*(model.id=="AncientDragon"?48:6));m.SetFloat("_DetailNormalMapScale",.17f);m.EnableKeyword("_DETAIL_MULX2");
    }
    if(model.id=="Ship"){m.mainTexture=Texture("Textures/MetalColor");m.SetTexture("_BumpMap",Texture("Textures/MetalNormal"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_Metallic",.8f);m.SetFloat("_Glossiness",.26f);m.mainTextureScale=Vector2.one*4;m.SetTextureScale("_BumpMap",Vector2.one*4);}
    materials[data.name]=m;
   }
   var armour=new Material(Resources.Load<Shader>(Root+"RealismPBR")){name="AncientDragon__Armour",color=new Color(.8f,.8f,.75f),enableInstancing=true};armour.mainTexture=Texture("Textures/ScaleDetail_Albedo");armour.mainTextureScale=Vector2.one*3;armour.SetTextureScale("_BumpMap",Vector2.one*3);armour.SetTexture("_BumpMap",Texture("Textures/ScaleDetail_Normal"));armour.SetFloat("_BumpScale",.45f);armour.SetFloat("_Glossiness",.18f);armour.EnableKeyword("_NORMALMAP");materials[armour.name]=armour;
   var horn=new Material(Resources.Load<Shader>(Root+"RealismPBR")){name="AncientDragon__Horn",color=new Color(.40f,.32f,.21f),enableInstancing=true};horn.SetFloat("_Glossiness",.22f);materials[horn.name]=horn;
  }
  public static GameObject Instantiate(string id,Transform parent){Initialize();if(!models.TryGetValue(id,out var source)){source=Resources.Load<GameObject>(Root+"Models/"+id);models[id]=source;}if(!source)return null;
   var wrapper=new GameObject("Realism appearance · "+id);wrapper.transform.SetParent(parent,false);var art=UnityEngine.Object.Instantiate(source,wrapper.transform,false);art.name=id;
   foreach(var c in art.GetComponentsInChildren<Collider>(true)){c.enabled=false;UnityEngine.Object.Destroy(c);}
   foreach(var r in art.GetComponentsInChildren<Renderer>(true)){
    var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++){if(!slots[i])continue;string name=slots[i].name.Replace(" (Instance)","");if(materials.TryGetValue(name,out var m))slots[i]=m;}r.sharedMaterials=slots;r.receiveShadows=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
    if(r is SkinnedMeshRenderer skin){skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;}
   }
   foreach(var animator in art.GetComponentsInChildren<Animator>(true))animator.applyRootMotion=false;
   return wrapper;
  }
  public static Bounds Bounds(Transform root){var renderers=root.GetComponentsInChildren<Renderer>(true);var b=new Bounds(root.position,Vector3.zero);bool first=true;foreach(var r in renderers){if(!(r is MeshRenderer)&&!(r is SkinnedMeshRenderer))continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}return b;}
  public static void Fit(Transform root,float size,int axis=3){var b=Bounds(root);float extent=axis==0?b.size.x:axis==1?b.size.y:axis==2?b.size.z:Mathf.Max(b.size.x,b.size.y,b.size.z);if(extent<.0001f)return;root.localScale*=size/extent;b=Bounds(root);root.position+=new Vector3(root.position.x-b.center.x,root.position.y-b.min.y,root.position.z-b.center.z);}
  public static Transform Bone(Transform root,string prefix){foreach(var t in root.GetComponentsInChildren<Transform>(true))if((t.name==prefix||(t.name.StartsWith(prefix,StringComparison.Ordinal)&&t.name.Length>prefix.Length&&(char.IsDigit(t.name[prefix.Length])||t.name[prefix.Length]=='.'||t.name[prefix.Length]=='_'))))return t;return null;}
  public static void Orient(Transform root,string headPrefix){var head=Bone(root,headPrefix);if(!head)return;var delta=head.position-Bounds(root).center;delta.y=0;if(delta.sqrMagnitude>.0001f)root.rotation=Quaternion.FromToRotation(delta,root.parent?root.parent.forward:Vector3.forward)*root.rotation;}
  public static void HideOriginal(Transform root){foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(!InAppearance(r.transform)&&(r is MeshRenderer||r is SkinnedMeshRenderer))r.forceRenderingOff=true;}
  public static bool InAppearance(Transform t){for(;t;t=t.parent)if(t.name.StartsWith("Realism appearance"))return true;return false;}
 }
}
