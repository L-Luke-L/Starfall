using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  readonly float[] ecologyX={11.9f,-4.1f,-12.8f};readonly string[] ecologyKeys={"pollution-off","water-restored","seed-planted"};readonly string[] ecologyNames={"关闭污染管道","修复净水设施","播下同盟种子"};
  GameObject[] ecologyProps;GameObject vegetation;int ecologyTask=-1;float ecologyTime,greenAge;
  void CreateSurfaceSites(){
   ecologyProps=new GameObject[3];for(int i=0;i<3;i++){var g=new GameObject("Ecology station "+i);g.transform.position=new Vector3(ecologyX[i],director.surfaceY,-.4f);ecologyProps[i]=g;
    for(int j=0;j<3;j++){var sprite=new GameObject("Independent module "+j).AddComponent<SpriteRenderer>();sprite.transform.SetParent(g.transform,false);sprite.sprite=director.pixel;sprite.sortingOrder=25;sprite.color=i==0?new Color(.24f,.30f,.31f):i==1?new Color(.23f,.44f,.48f):new Color(.24f,.34f,.20f);sprite.transform.localPosition=new Vector3((j-1)*.23f,.20f+j%2*.22f,0);sprite.transform.localScale=new Vector3(.22f,.3f+j%2*.35f,1);}}
   vegetation=new GameObject("Restored local vegetation");vegetation.transform.position=new Vector3(0,director.surfaceY,-.42f);
   for(int i=0;i<65;i++){var spr=new GameObject("Independent seedling "+i).AddComponent<SpriteRenderer>();spr.transform.SetParent(vegetation.transform,false);spr.sprite=director.pixel;spr.sortingOrder=26;spr.color=new Color(.22f+i%3*.03f,.38f+i%5*.04f,.22f);spr.transform.localPosition=new Vector3(-13.5f+i*.4f,.1f,0);spr.transform.localRotation=Quaternion.Euler(0,0,(i%3-1)*15);spr.transform.localScale=new Vector3(.045f,.10f+(i%7)*.025f,1);}
  }
  bool TryEcology(){for(int i=0;i<3;i++)if(Mathf.Abs(director.player.position.x-ecologyX[i])<1.4f&&!State.Has(ecologyKeys[i])){ecologyTask=i;ecologyTime=0;Notice(ecologyNames[i]+" · 保持位置");return true;}return false;}
  void TickEcology(){
   if(ecologyProps==null)return;for(int i=0;i<3;i++)ecologyProps[i].SetActive(State.Has("ecology-chosen"));
   if(ecologyTask>=0){if(Modal||director.IsPaused)return;if(Mathf.Abs(director.player.position.x-ecologyX[ecologyTask])>1.4f||director.battle.IsEngaged){ecologyTask=-1;return;}ecologyTime+=Time.deltaTime;if(ecologyTime>2.5f){State.Flag(ecologyKeys[ecologyTask]);Notice(ecologyNames[ecologyTask]+" · 完成");ecologyTask=-1;Changed();if(State.Has("pollution-off")&&State.Has("water-restored")&&State.Has("seed-planted")){State.saviorComplete=true;Save();Open("saved");}}}
   vegetation.SetActive(State.Has("seed-planted"));if(vegetation.activeSelf){greenAge+=Time.unscaledDeltaTime;vegetation.transform.localScale=new Vector3(1,Mathf.Lerp(.3f,1,Mathf.Clamp01(greenAge/8)),1);}
   if(State.Has("ecology-chosen")&&!State.saviorComplete&&!Modal&&!director.underground&&!director.battle.IsEngaged)for(int i=0;i<3;i++)if(Mathf.Abs(director.player.position.x-ecologyX[i])<1.4f&&!State.Has(ecologyKeys[i]))director.Say("E  "+ecologyNames[i]);
  }
 }
}
