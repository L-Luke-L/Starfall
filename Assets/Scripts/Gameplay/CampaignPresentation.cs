using System.Collections;
using UnityEngine;
namespace Starfall {
 public sealed partial class CampaignRuntime {
  public Sprite[] mineralSprites;
  GameObject recoveredCore;Material coreMaterial;float returnFlash;Vector3 wreckRest;bool presentationReady;
  void SetupPresentation(){
   wreckRest=director.wreck.transform.position;presentationReady=true;
   recoveredCore=new GameObject("Recovered spatial core · voluntary interaction");recoveredCore.transform.position=new Vector3(.8f,director.surfaceY+.9f,-.8f);
   var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Moving sky core";quad.transform.SetParent(recoveredCore.transform,false);quad.transform.localScale=new Vector3(.54f,.70f,1);Destroy(quad.GetComponent<Collider>());coreMaterial=new Material(Shader.Find("Starfall/Wormhole"));var r=quad.GetComponent<MeshRenderer>();r.sharedMaterial=coreMaterial;r.sortingOrder=70;
  }
  void TickPresentation(){
   if(!presentationReady)return;returnFlash=Mathf.Max(0,returnFlash-Time.unscaledDeltaTime*.55f);
   recoveredCore.SetActive(director.underground&&State.coordinates.Contains(State.layer)&&!director.battle.IsEngaged&&!State.Has("rift-destroyed"));
   if(recoveredCore.activeSelf)recoveredCore.transform.localScale=Vector3.one*(1+Mathf.Sin(Time.time*2.4f)*.035f);
   bool cool=State.repairs.Contains("2")||State.Has("pollution-off");foreach(var fire in director.wreck.GetComponentsInChildren<WreckFire>(true)){fire.gameObject.SetActive(!cool);if(fire.groundLight)fire.groundLight.enabled=!cool;}
  }
  IEnumerator Flight(){
   Close();Busy=true;InteractionFeel.Play(FeelCue.Portal,1);var art=director.wreck.transform.Find("Artwork");var vessel=new GameObject("Repaired vessel · departing").transform;vessel.position=wreckRest;
   var hull=new GameObject("Flight hull").AddComponent<SpriteRenderer>();hull.transform.SetParent(vessel,false);if(art){var source=art.GetComponent<SpriteRenderer>();hull.sprite=source.sprite;hull.transform.localPosition=art.localPosition;hull.transform.localScale=art.localScale;hull.color=new Color(.81f,.89f,.92f);hull.sortingOrder=50;art.gameObject.SetActive(false);}
   var trail=new GameObject("Ion exhaust").AddComponent<SpriteRenderer>();trail.transform.SetParent(vessel,false);trail.sprite=director.glow;trail.color=new Color(.3f,.77f,1,.7f);trail.sortingOrder=49;trail.transform.localPosition=new Vector3(-2,.4f,0);trail.transform.localScale=new Vector3(3,.7f,1);
   for(float t=0;t<3.6f;t+=Time.unscaledDeltaTime){float n=t/3.6f;vessel.position=wreckRest+new Vector3(n*n*15,n*n*13,0);vessel.rotation=Quaternion.Euler(0,0,n*28);yield return null;}
   Destroy(vessel.gameObject);if(art)art.gameObject.SetActive(true);Busy=false;Open("escaped");
  }
  void DrawPresentation(){
   if(returnFlash>0){GUI.color=new Color(.68f,.89f,1,returnFlash*.7f);GUI.DrawTexture(new Rect(0,0,1600,900),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(recoveredCore&&recoveredCore.activeSelf&&!Modal)Caption(new Rect(760,178,390,30),"E  触碰空间核心",15,new Color(.72f,.72f,1));
  }
 }
}
