using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Starfall;

public static class ForgeBuilder {
 static Texture2D Art(string name){string path="Assets/Art/Forge/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
 public static void Configure(SceneDirector director){
  var inventory=director.GetComponent<OreInventory>();if(!inventory)inventory=director.gameObject.AddComponent<OreInventory>();
  var forge=director.GetComponent<ForgeController>();if(!forge)forge=director.gameObject.AddComponent<ForgeController>();
  director.inventory=inventory;director.forge=forge;forge.director=director;forge.inventory=inventory;
  var encounter=director.GetComponent<RecoveryEncounter>();if(!encounter)encounter=director.gameObject.AddComponent<RecoveryEncounter>();encounter.director=director;director.encounter=encounter;
  BattleSceneBuilder.ConfigureGateway(director);
  forge.heroReady=Art("HeroReady");forge.heroLift=Art("HeroLift");forge.heroStrike=Art("HeroStrikeAligned");forge.anvil=Art("Anvil");forge.failureCG=Art("FailureCutout");forge.workshop=Art("Workshop");forge.oreArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Underground/FerricNodule.png");
  forge.workshopBlurShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorkshopBlur.shader");
  forge.smithPoses=Art("SmithPoses");forge.anvil=Art("GroundedAnvil");forge.ingotSheet=Art("NamedIngots");
  forge.targetWidths=new[]{.12f,.10f,.085f};forge.roundDurations=new[]{2.9f,2.5f,2.2f};forge.pointerSpeeds=new[]{1.12f,1.30f,1.48f};
  if(!director.GetComponent<ForgeVerification>())director.gameObject.AddComponent<ForgeVerification>();
  if(!director.GetComponent<EncounterVerification>())director.gameObject.AddComponent<EncounterVerification>();
 }
 [MenuItem("Starfall/Install Forging and Build")]
 public static void InstallAndBuild(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Starfall.unity");Configure(Object.FindFirstObjectByType<SceneDirector>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();StarfallBuilder.Build();
 }
}
