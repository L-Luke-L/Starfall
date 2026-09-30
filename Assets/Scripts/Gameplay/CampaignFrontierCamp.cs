using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  GameObject[] frontierCamp;SpriteRenderer[] frontierLamps;
  void CreateFrontierCamp(){
   frontierCamp=new GameObject[4];frontierLamps=new SpriteRenderer[7];float[] xs={-13.5f,-5.25f,-14.65f,-8.8f};
   for(int i=0;i<4;i++){var g=new GameObject("余烬建设 · "+FrontierCatalog.Current.projects[i].title);g.transform.position=new Vector3(xs[i],director.surfaceY,-.42f);frontierCamp[i]=g;var t=g.transform;var metal=new Color(.33f,.39f,.41f);var darkColor=new Color(.15f,.20f,.23f);var cyan=new Color(.36f,.70f,.74f);
    if(i==0){for(int side=-1;side<=1;side+=2)Panel(t,"货架立柱",side*.61f,.56f,.10f,1.14f,metal);for(int row=0;row<3;row++){Panel(t,"载货层板",0,.2f+row*.38f,1.37f,.08f,metal);for(int n=0;n<3;n++){Panel(t,"独立收纳箱",(n-1)*.39f,.36f+row*.38f,.32f,.24f,darkColor);Panel(t,"箱扣",(n-1)*.39f,.35f+row*.38f,.05f,.16f,new Color(.56f,.41f,.27f),31);}}}
    else if(i==1){Panel(t,"整备台架",0,.36f,1.35f,.72f,darkColor);Panel(t,"工作台面",0,.78f,1.52f,.12f,metal,31);for(int n=0;n<4;n++){Panel(t,"战术芯支架",-.5f+n*.34f,1.02f,.18f,.36f,metal);Panel(t,"独立芯体",-.5f+n*.34f,1.09f,.11f,.22f,n<2?new Color(.83f,.40f,.17f):cyan,32);}Panel(t,"装备锁扣",0,.46f,.40f,.11f,cyan,32);}
    else if(i==2){Panel(t,"培养槽底座",0,.20f,1.10f,.4f,metal);for(int n=0;n<4;n++){Panel(t,"培养管壁",-.4f+n*.27f,.69f,.20f,.7f,darkColor);Panel(t,"培养液",-.4f+n*.27f,.63f,.12f,.45f,new Color(.21f,.53f,.39f),31);Panel(t,"药用幼苗",-.4f+n*.27f,.82f,.035f,.32f,new Color(.42f,.67f,.38f),32,18);Panel(t,"密封顶盖",-.4f+n*.27f,1.06f,.23f,.08f,metal,33);}}
    else{Panel(t,"档案台脚",0,.45f,.14f,.9f,metal);Panel(t,"档案屏框",0,.98f,.84f,.50f,darkColor,34);Panel(t,"冷光记录屏",0,1.0f,.71f,.34f,cyan*.65f,35);for(int line=0;line<3;line++)Panel(t,"档案扫描线",-.1f,1.11f-line*.09f,.39f,.018f,cyan,36);for(int lamp=0;lamp<7;lamp++){float x=(lamp-3)*.85f;Panel(t,"营地独立灯缆",x,2.2f+Mathf.Abs(lamp-3)*.06f,.89f,.02f,metal,29,-(lamp-3)*2);frontierLamps[lamp]=Panel(t,"独立灯盏",x,2.09f+Mathf.Abs(lamp-3)*.06f,.12f,.17f,new Color(.95f,.70f,.34f),30);}}
    g.SetActive(false);
   }
  }
  void TickFrontierCamp(){if(frontierCamp==null)return;for(int i=0;i<4;i++)frontierCamp[i].SetActive(Exp("build-"+FrontierCatalog.Current.projects[i].id));}
  bool TryFrontierTerminal(){if(AtCamp&&Exp("build-archive")&&Mathf.Abs(director.player.position.x+8.8f)<.45f){Open("frontier");frontierTab=1;return true;}return false;}
 }
}
