using UnityEngine;

namespace Starfall {
 public sealed partial class CampaignRuntime {
  Sprite campPanel;Texture2D campPanelTexture;
  SpriteRenderer Panel(Transform parent,string name,float x,float y,float w,float h,Color color,int order=28,float angle=0){
   if(!campPanel){var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
    for(int j=0;j<64;j++)for(int i=0;i<64;i++){float value=.70f+((i*17+j*29)%13)*.011f;if(i<2||j<2)value=.34f;else if(i>61||j>61)value=.92f;if(j==8||j==54)value*=.78f;texture.SetPixel(i,j,new Color(value,value,value,1));}texture.Apply();campPanel=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,64);campPanelTexture=texture;}
   var part=Piece(parent,name,new Vector3(x,y,0),new Vector3(w,h,1),color,order);part.sprite=campPanel;part.transform.localRotation=Quaternion.Euler(0,0,angle);return part;
  }
  void CreateFacilityArt(float ground){
   Color metal=new Color(.39f,.44f,.45f),dark=new Color(.16f,.21f,.23f),warm=new Color(.51f,.38f,.24f),cold=new Color(.40f,.73f,.77f);
   for(int i=0;i<3;i++){var root=new GameObject(i==0?"岚的工坊":i==1?"苏禾的医疗设施":"生态温室");root.transform.position=new Vector3(i==0?-5.6f:i==1?-12.7f:-14.6f,ground,-.32f);facilityArt[i]=root;var t=root.transform;
    if(i==0){
     Panel(t,"工作台左支脚",-.46f,.27f,.12f,.55f,dark);Panel(t,"工作台右支脚",.46f,.27f,.12f,.55f,dark);Panel(t,"加固横梁",0,.16f,1.1f,.09f,metal);Panel(t,"厚重台面",0,.62f,1.3f,.12f,metal,30);Panel(t,"工具柜",0,.33f,.55f,.44f,warm,29);
     for(int j=0;j<2;j++){Panel(t,"抽屉缝 "+j,0,.30f+j*.17f,.49f,.025f,dark,30);Panel(t,"抽屉把手 "+j,0,.36f+j*.17f,.13f,.035f,metal,31);}
     Panel(t,"工具背板",0,1.04f,1.08f,.67f,dark,27);for(int j=0;j<4;j++){float x=(j-1.5f)*.22f;Panel(t,"独立工具柄 "+j,x,1.04f,.05f,.25f,warm,29,j%2==0?10:-12);Panel(t,"独立工具头 "+j,x,1.18f,.15f,.07f,metal,30);}
     Panel(t,"武器展示托架",.76f,.88f,.06f,1.04f,dark,27);Panel(t,"展示枪身",.75f,1.04f,.17f,.48f,metal,29,-18);Panel(t,"展示握把",.82f,.78f,.10f,.20f,warm,30,-18);Panel(t,"维修终端",-.39f,.76f,.27f,.20f,dark,31);Piece(t,"终端冷光",new Vector3(-.39f,.78f,0),new Vector3(.20f,.10f,1),cold*.8f,32);
    }else if(i==1){
     Panel(t,"医疗箱底座",0,.26f,1.0f,.44f,dark);Panel(t,"折叠治疗床",0,.64f,1.28f,.12f,metal,29);Panel(t,"床垫",.05f,.73f,1.1f,.11f,new Color(.43f,.54f,.53f),30);Panel(t,"靠背",-.57f,.87f,.13f,.40f,metal,30,-12);
     Panel(t,"监护支架",.45f,1.08f,.055f,.72f,metal,27);Panel(t,"独立监护屏",.45f,1.43f,.43f,.30f,dark,29);Piece(t,"监护冷光",new Vector3(.45f,1.43f,0),new Vector3(.32f,.19f,1),new Color(.15f,.35f,.36f),30);for(int j=0;j<4;j++)Panel(t,"生命信号 "+j,.33f+j*.07f,1.43f+Mathf.Sin(j*2)*.04f,.06f,.025f,cold,31,j%2==0?20:-20);
     Panel(t,"急救标志竖",0,.31f,.07f,.22f,cold,31);Panel(t,"急救标志横",0,.31f,.22f,.07f,cold,31);
    }else{
     Panel(t,"培养槽",0,.22f,1.5f,.37f,dark,29);Panel(t,"培养槽边框",0,.44f,1.57f,.07f,metal,31);Piece(t,"独立温室玻璃",new Vector3(0,1.03f,0),new Vector3(1.36f,1.12f,1),new Color(.24f,.51f,.55f,.18f),26);
     for(int side=-1;side<=1;side+=2){Panel(t,"温室立柱 "+side,side*.73f,.93f,.07f,1.1f,metal,28);Panel(t,"独立顶架 "+side,side*.36f,1.70f,.86f,.055f,metal,28,-side*31);}
     for(int j=0;j<5;j++){float x=(j-2)*.25f,height=.33f+j%3*.10f;Panel(t,"植物茎 "+j,x,.45f+height*.5f,.025f,height,new Color(.25f,.42f,.24f),30);for(int side=-1;side<=1;side+=2)Panel(t,"独立叶片 "+j+" / "+side,x+side*.07f,.58f+height*.55f,.18f,.06f,new Color(.31f,.49f,.29f),31,side*28);}
     Panel(t,"温室顶灯",0,1.87f,.38f,.045f,cold*.7f,29);
    }
   }
  }
 }
}
