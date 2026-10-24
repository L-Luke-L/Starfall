using UnityEditor;
using UnityEngine;
public sealed class StarfallRealismImporter:AssetPostprocessor {
 const string Root="Assets/Resources/StarfallRealism/";
 void OnPreprocessModel(){if(!assetPath.StartsWith(Root))return;var model=(ModelImporter)assetImporter;model.importCameras=false;model.importLights=false;model.addCollider=false;model.isReadable=true;model.optimizeGameObjects=false;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;model.materialName=ModelImporterMaterialName.BasedOnMaterialName;model.materialSearch=ModelImporterMaterialSearch.Local;model.animationType=assetPath.Contains("Dinosaur")||assetPath.Contains("Wyvern")||assetPath.Contains("AncientDragon")||assetPath.Contains("Explorer")?ModelImporterAnimationType.Legacy:ModelImporterAnimationType.None;model.importAnimation=model.animationType==ModelImporterAnimationType.Legacy;}
 void OnPreprocessTexture(){if(!assetPath.StartsWith(Root))return;var texture=(TextureImporter)assetImporter;texture.mipmapEnabled=true;texture.anisoLevel=8;texture.maxTextureSize=assetPath.Contains("GasGiant")?8192:assetPath.Contains("StarMap")?4096:2048;
  bool normal=assetPath.Contains("_Normal")||assetPath.Contains("GroundNormal")||assetPath.Contains("MetalNormal");texture.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;texture.sRGBTexture=!normal&&!assetPath.Contains("MetalSmooth")&&!assetPath.Contains("_AO")&&!assetPath.Contains("Roughness")&&!assetPath.Contains("_Height")&&!assetPath.Contains("StarMap");texture.wrapMode=TextureWrapMode.Repeat;
 }
 [InitializeOnLoadMethod] static void RepairInitialImport(){EditorApplication.delayCall+=()=>{
  if(SessionState.GetBool("StarfallRealismImporterReady",false))return;SessionState.SetBool("StarfallRealismImporterReady",true);
  foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{Root+"Models"})){
   var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(ModelImporter)AssetImporter.GetAtPath(path);bool animated=path.Contains("Dinosaur")||path.Contains("Wyvern")||path.Contains("AncientDragon")||path.Contains("Explorer");
   if((animated&&importer.animationType!=ModelImporterAnimationType.Legacy)||!importer.isReadable||importer.materialName!=ModelImporterMaterialName.BasedOnMaterialName)importer.SaveAndReimport();
  }
  foreach(var guid in AssetDatabase.FindAssets("t:Texture",new[]{Root+"Textures"})){
   var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("_Normal")||path.Contains("GroundNormal")||path.Contains("MetalNormal");bool linear=normal||path.Contains("MetalSmooth")||path.Contains("_AO")||path.Contains("Roughness")||path.Contains("_Height")||path.Contains("StarMap");
   if((normal&&importer.textureType!=TextureImporterType.NormalMap)||importer.sRGBTexture==linear)importer.SaveAndReimport();
  }
 };}
}
