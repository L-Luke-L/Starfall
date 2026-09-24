using UnityEngine;

namespace Starfall {
 public sealed class CampaignNode:MonoBehaviour {
  public int layer,index,quantity;public ItemId item;public bool fragment;public Vector3 origin;
  GameObject lens;SpriteRenderer glow;SceneDirector director;Material portalMaterial;
  public void ConfigureVisual(SceneDirector owner){
   director=owner;var sample=GetComponent<BuriedSample>();Color color=item==ItemId.PurpleOre?new Color(.79f,.40f,.28f):item==ItemId.IronOre?new Color(.64f,.72f,.75f):item==ItemId.CopperOre?new Color(.93f,.52f,.24f):item==ItemId.CobaltOre?new Color(.35f,.70f,.90f):item==ItemId.StarOre?new Color(.71f,.49f,.92f):new Color(.51f,.94f,.84f);
   if(sample&&sample.mineralShell){sample.mineralShell.color=color;var sprites=CampaignRuntime.Current.mineralSprites;int spriteIndex=item==ItemId.AlienCrystal?5:(int)item;if(sprites!=null&&spriteIndex<sprites.Length&&sprites[spriteIndex]){var sr=sample.mineralShell;Vector3 size=Vector3.Scale(sr.sprite.bounds.size,sr.transform.localScale);sr.sprite=sprites[spriteIndex];sr.transform.localScale=new Vector3(size.x/sr.sprite.bounds.size.x,size.y/sr.sprite.bounds.size.y,1);}}
   if(!lens){lens=new GameObject("Space fragment · independent lens");lens.transform.SetParent(transform,false);lens.transform.localPosition=new Vector3(0,0,-.12f);glow=lens.AddComponent<SpriteRenderer>();glow.sprite=owner.glow;glow.sortingOrder=80;lens.transform.localScale=Vector3.one*.75f;
    var slit=GameObject.CreatePrimitive(PrimitiveType.Quad);slit.name="Moving sky inside fissure";slit.transform.SetParent(lens.transform,false);slit.transform.localPosition=new Vector3(0,0,-.02f);slit.transform.localScale=new Vector3(.23f,.60f,1);slit.transform.localRotation=Quaternion.Euler(0,0,-21);Destroy(slit.GetComponent<Collider>());portalMaterial=new Material(Shader.Find("Starfall/Wormhole"));var renderer=slit.GetComponent<MeshRenderer>();renderer.sharedMaterial=portalMaterial;renderer.sortingOrder=81;
   }
   lens.SetActive(fragment);if(glow)glow.color=new Color(.65f,.32f,1,.65f);
  }
  void Update(){if(fragment&&glow){float pulse=.56f+Mathf.Sin(Time.time*2.5f)*.13f;glow.color=new Color(.68f,.33f,1,pulse);lens.transform.localScale=Vector3.one*(.7f+Mathf.Sin(Time.time*1.4f)*.08f);}}
  void OnDestroy(){if(portalMaterial)Destroy(portalMaterial);}
  void OnGUI(){if(!fragment||!director||!director.underground||director.RecoveryLocked||!gameObject.activeInHierarchy||!CampaignRuntime.Current||CampaignRuntime.Current.Modal)return;var v=director.worldCamera.WorldToScreenPoint(transform.position);if(v.z<0)return;InteractionFeel.Text(new Rect(v.x-50,Screen.height-v.y-45,100,28),"◇ 裂纹",director.uiFont,14,new Color(.85f,.70f,1),TextAnchor.MiddleCenter);}
 }
}
