using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 // Kenney CC0 geometry, with the weathered metal and warm instrument lights of
 // the camp. Drawing stays in the campaign's 1600 x 900 coordinate system.
 public static class CampaignGameUI {
  public static readonly Color Ink=new Color(.88f,.92f,.91f);
  public static readonly Color Muted=new Color(.56f,.65f,.67f);
  public static readonly Color Cyan=new Color(.44f,.71f,.74f);
  public static readonly Color Gold=new Color(.85f,.68f,.42f);
  public static readonly Color Green=new Color(.49f,.76f,.61f);
  public static readonly Color Background=new Color(.045f,.073f,.092f);
  public static readonly Color Border=new Color(.22f,.30f,.33f);
  public static readonly Color Warning=new Color(.91f,.51f,.32f);
  static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
  static readonly Dictionary<string,GUIStyle> slices=new Dictionary<string,GUIStyle>();
  static readonly Dictionary<LabelKey,GUIStyle> labels=new Dictionary<LabelKey,GUIStyle>();
  static readonly Dictionary<int,float> buttonHover=new Dictionary<int,float>();
  static readonly int buttonHint="Starfall.Campaign.Button".GetHashCode();
  readonly struct LabelKey:System.IEquatable<LabelKey> {
   readonly int font,size;readonly TextAnchor align;readonly bool wrap;
   public LabelKey(Font font,int size,TextAnchor align,bool wrap){this.font=font?font.GetInstanceID():0;this.size=size;this.align=align;this.wrap=wrap;}
   public bool Equals(LabelKey other)=>font==other.font&&size==other.size&&align==other.align&&wrap==other.wrap;
   public override bool Equals(object other)=>other is LabelKey key&&Equals(key);
   public override int GetHashCode()=>(((font*397)^size)*397^(int)align)*397^(wrap?1:0);
  }
  public static Texture2D Texture(string name){if(!textures.TryGetValue(name,out var t)){t=Resources.Load<Texture2D>("UI/KenneySciFi/"+name);textures[name]=t;}return t;}
  public static void Solid(Rect r,Color color){if(r.width<=0||r.height<=0||color.a<=0)return;var prior=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=prior;}
  public static void Outline(Rect r,Color color,float width=1){
   width=Mathf.Min(width,Mathf.Min(r.width,r.height)*.5f);if(width<=0)return;
   Solid(new Rect(r.x,r.y,r.width,width),color);Solid(new Rect(r.x,r.yMax-width,r.width,width),color);
   Solid(new Rect(r.x,r.y+width,width,r.height-width*2),color);Solid(new Rect(r.xMax-width,r.y+width,width,r.height-width*2),color);
  }
  public static void Divider(Rect r,Color? color=null){Solid(new Rect(r.x,r.y+Mathf.Max(0,(r.height-1)*.5f),r.width,1),color??Border);}
  public static void Slice(Rect r,string name,Color color,int border=12){
   if(Event.current.type!=EventType.Repaint||r.width<=0||r.height<=0)return;var texture=Texture(name);if(!texture){Solid(r,color);return;}
   string key=name+":"+border;if(!slices.TryGetValue(key,out var style)){style=new GUIStyle{border=new RectOffset(border,border,border,border),normal={background=texture}};slices[key]=style;}
   var prior=GUI.color;GUI.color=color;style.Draw(r,GUIContent.none,false,false,false,false);GUI.color=prior;
  }
  public static void Panel(Rect r,bool inset=false){
   if(!inset)Solid(new Rect(r.x+4,r.y+6,r.width,r.height),new Color(0,0,0,.23f));
   Slice(r,inset?"Inset":"Panel",inset?new Color(.095f,.145f,.18f):new Color(.145f,.205f,.25f),inset?12:32);
   Outline(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),inset?new Color(.19f,.26f,.29f):new Color(.32f,.36f,.36f));
   Solid(new Rect(r.x+8,r.y+2,r.width-16,1),new Color(.47f,.55f,.55f,inset?.08f:.12f));
  }
  public static void Modal(Rect r){
   Solid(new Rect(0,0,1600,900),new Color(.008f,.014f,.02f,.78f));
   Solid(new Rect(r.x-5,r.y-5,r.width+10,r.height+10),new Color(0,0,0,.16f));
   Solid(new Rect(r.x+9,r.y+14,r.width,r.height),new Color(0,0,0,.32f));Panel(r);
   Solid(new Rect(r.x+3,r.y+3,r.width-6,110),new Color(.075f,.107f,.128f,.88f));
   Divider(new Rect(r.x+26,r.y+115,r.width-52,1));Solid(new Rect(r.x+26,r.y+114,84,2),Gold);
   // Machined corner marks frame the terminal without competing with its text.
   Solid(new Rect(r.x+2,r.y+2,26,2),Gold*.8f);Solid(new Rect(r.x+2,r.y+2,2,20),Gold*.8f);
   Solid(new Rect(r.xMax-28,r.yMax-4,26,2),Gold*.55f);Solid(new Rect(r.xMax-4,r.yMax-22,2,20),Gold*.55f);
  }
  public static void Text(Rect r,string text,Font font,int size=18,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft,bool wrap=false){
   var key=new LabelKey(font,size,align,wrap);if(!labels.TryGetValue(key,out var style)){
    style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=FontStyle.Normal,alignment=align,wordWrap=wrap,richText=false,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0),margin=new RectOffset(0,0,0,0)};labels[key]=style;
   }
   style.normal.textColor=color??Ink;GUI.Label(r,text,style);
  }
  public static void SectionLabel(Rect r,string text,Font font,string value=null){
   Solid(new Rect(r.x,r.center.y-3,3,6),Gold);
   bool hasValue=!string.IsNullOrEmpty(value);Text(new Rect(r.x+13,r.y,hasValue?r.width*.55f-21:r.width-13,r.height),text,font,14,Muted);
   if(hasValue)Text(new Rect(r.x+r.width*.55f,r.y,r.width*.45f,r.height),value,font,14,Muted,TextAnchor.MiddleRight);
  }
  public static void Badge(Rect r,string text,Font font,Color? color=null){
   Color tint=color??Gold;Solid(r,new Color(tint.r*.12f,tint.g*.12f,tint.b*.12f,.96f));
   Outline(r,new Color(tint.r,tint.g,tint.b,.4f));Text(r,text,font,13,tint,TextAnchor.MiddleCenter);
  }
  public static void Icon(Rect r,string name,Color? tint=null){var texture=Texture(name);if(!texture)return;var prior=GUI.color;GUI.color=tint??Ink;GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);GUI.color=prior;}
  public static void Slot(Rect r,bool selected=false,bool hovered=false,bool locked=false){
   Slice(r,"Slot",locked?new Color(.075f,.105f,.13f):selected?new Color(.23f,.27f,.28f):hovered?new Color(.20f,.26f,.29f):new Color(.14f,.20f,.24f),9);
   Outline(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),locked?new Color(.13f,.18f,.21f):selected?Gold:hovered?Cyan:new Color(.23f,.30f,.32f));
   if(selected){Solid(new Rect(r.x+2,r.y+2,3,r.height-4),Gold);Solid(new Rect(r.x+7,r.y+3,15,1),Gold*.7f);}
  }
  public static bool Button(Rect r,string text,Font font,bool enabled=true,bool selected=false,int size=17){
   int id=GUIUtility.GetControlID(buttonHint,FocusType.Keyboard,r);var evt=Event.current;
   bool available=enabled&&GUI.enabled,hover=available&&r.Contains(evt.mousePosition),clicked=false;
   // Explicit keyboard focus also makes icon-only and empty-label list rows usable.
   switch(evt.GetTypeForControl(id)){
    case EventType.MouseDown:if(hover&&evt.button==0){GUIUtility.hotControl=id;GUIUtility.keyboardControl=id;evt.Use();}break;
    case EventType.MouseDrag:if(GUIUtility.hotControl==id)evt.Use();break;
    case EventType.MouseUp:if(GUIUtility.hotControl==id&&evt.button==0){GUIUtility.hotControl=0;clicked=hover;evt.Use();}break;
    case EventType.KeyDown:if(available&&GUIUtility.keyboardControl==id&&(evt.keyCode==KeyCode.Return||evt.keyCode==KeyCode.KeypadEnter||evt.keyCode==KeyCode.Space)){clicked=true;evt.Use();}break;
   }
   buttonHover.TryGetValue(id,out float amount);
   if(evt.type==EventType.Repaint){
    if(hover&&amount<=0)InteractionFeel.Play(FeelCue.Hover,.32f);
    amount=InteractionFeel.ReducedMotion?(hover?1:0):Mathf.MoveTowards(amount,hover?1:0,Time.unscaledDeltaTime*12);
    if(buttonHover.Count>512)buttonHover.Clear();buttonHover[id]=amount;
   }
   bool focused=available&&GUIUtility.keyboardControl==id,pressed=available&&GUIUtility.hotControl==id&&hover;
   Color tint=selected?new Color(.235f,.265f,.27f):new Color(.145f,.205f,.245f);
   tint=Color.Lerp(tint,new Color(.25f,.32f,.35f),amount*.75f);if(pressed)tint*=.72f;if(!available)tint=new Color(.09f,.125f,.15f);tint.a=1;
   Slice(r,"Button",tint,16);
   Outline(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),selected?new Color(Gold.r,Gold.g,Gold.b,.78f):Color.Lerp(new Color(.26f,.33f,.35f),Cyan,amount*.7f));
   if(selected)Solid(new Rect(r.x+2,r.y+3,3,r.height-6),Gold);
   if(focused)Outline(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),new Color(Cyan.r,Cyan.g,Cyan.b,.8f));
   Text(r,text,font,size,available?(selected?new Color(.98f,.91f,.77f):Ink):new Color(.43f,.50f,.52f),TextAnchor.MiddleCenter);
   if(clicked)InteractionFeel.Play(FeelCue.Click,.6f);return clicked;
  }
  public static bool IconButton(Rect r,string icon,string text,Font font,bool enabled=true,bool selected=false){
   bool hit=Button(r,"",font,enabled,selected);bool hasText=!string.IsNullOrEmpty(text);float edge=Mathf.Min(24,r.height-12);
   Icon(new Rect(hasText?r.x+13:r.center.x-edge*.5f,r.center.y-edge*.5f,edge,edge),icon,enabled?(selected?Gold:Cyan):Muted);
   if(hasText)Text(new Rect(r.x+46,r.y,r.width-54,r.height),text,font,16,enabled?Ink:Muted);return hit;
  }
  public static Color CapacityColor(float amount)=>amount>=1f?Warning:amount>=.8f?Gold:Cyan;
  public static void Progress(Rect r,float amount,Color? color=null){
   if(r.width<=4||r.height<=3)return;if(float.IsNaN(amount))amount=0;amount=Mathf.Clamp01(amount);
   Solid(r,new Color(.025f,.044f,.056f));Outline(r,new Color(.22f,.30f,.32f));
   var inner=new Rect(r.x+2,r.y+2,r.width-4,Mathf.Max(1,r.height-4));float width=inner.width*amount;Color fill=color??Cyan;
   if(width>0){Solid(new Rect(inner.x,inner.y,width,inner.height),fill);Solid(new Rect(inner.x,inner.y,width,1),Color.Lerp(fill,Ink,.25f));}
   // Neutral ticks keep amber and green fills true to their status colors.
   int ticks=r.width>=300?10:r.width>=120?4:0;for(int i=1;i<ticks;i++)Solid(new Rect(inner.x+inner.width*i/ticks,inner.y,1,inner.height),new Color(.025f,.044f,.056f,.58f));
  }
  public static Color ItemColor(ItemId item){int id=(int)item;if(id<10)return OreInventory.MetalColor(CampaignRules.Metal((ItemId)(id%5)));switch(item){case ItemId.Fuel:return Gold;case ItemId.Medkit:return Green;case ItemId.Ammo:return new Color(.9f,.85f,.62f);case ItemId.AlienCrystal:return new Color(.75f,.58f,1);case ItemId.BlastCore:return new Color(1,.49f,.25f);default:return Cyan;}}
  public static void ItemIcon(Rect r,ItemId item,Sprite[] minerals){
   int id=(int)item;Color tint=ItemColor(item);
   if((id<5||item==ItemId.AlienCrystal)&&minerals!=null&&minerals.Length>0){int index=item==ItemId.AlienCrystal?minerals.Length-1:Mathf.Min(id,minerals.Length-1);var sprite=minerals[index];if(sprite){var source=sprite.textureRect;var texture=sprite.texture;float aspect=source.width/source.height;float width=Mathf.Min(r.width,r.height*aspect),height=width/aspect;var fit=new Rect(r.center.x-width*.5f,r.center.y-height*.5f,width,height);var prior=GUI.color;GUI.color=id==2?new Color(1,.76f,.65f):Color.white;GUI.DrawTextureWithTexCoords(fit,texture,new Rect(source.x/texture.width,source.y/texture.height,source.width/texture.width,source.height/texture.height),true);GUI.color=prior;return;}}
   string name=id<10?"IconIngots":item==ItemId.Fuel?"IconFuel":item==ItemId.Ammo?"IconAmmo":item==ItemId.Medkit?"IconMedkit":item==ItemId.MachineParts?"IconGear":item==ItemId.BlastCore?"IconCore":item==ItemId.SoundDecoy?"IconDecoy":"IconStar";
   Icon(r,name,tint);
  }
  public static string ItemType(ItemId item){int id=(int)item;return id<5?"原矿 · 锻造材料":id<10?"金属锭 · 维修材料":item==ItemId.Fuel?"消耗品 · 能源":item==ItemId.Ammo?"消耗品 · 通用弹药":item==ItemId.Medkit?"消耗品 · 医疗":item==ItemId.AlienCrystal?"稀有材料 · 稳定异晶":item==ItemId.MachineParts?"组件 · 营地建设":"战术装备 · 消耗品";}
  public static string ItemDescription(ItemId item){
   switch(item){
    case ItemId.PurpleOre:return "用于制作手枪弹药，也可在营地锻成紫铜锭，用于飞船配电。";
    case ItemId.IronOre:return "耐用的结构矿物。营地锻造后可用于修复船体与支撑。";
    case ItemId.CopperOre:return "用于能源系统的赤辉铜原矿。它与紫铜属于不同材料。";
    case ItemId.CobaltOre:return "耐热的霜蓝钴原矿。锻造后可修复引擎冷却结构。";
    case ItemId.StarOre:return "用于导航稳定系统的稀有原矿。需先在营地锻造。";
    case ItemId.Fuel:return "飞船起飞、弹药制作与异晶炸弹所需的通用燃料。";
    case ItemId.Ammo:return "枪械共用的备用弹药。换弹时从随身背包取用。";
    case ItemId.Medkit:return "恢复 25 点护甲。护甲已满时不会消耗。";
    case ItemId.AlienCrystal:return "第五层取得的稳定异晶，可直接参与制作，不需要锻造。";
    case ItemId.MachineParts:return "用于营地建设、工坊与模块制作的机械组件。";
    case ItemId.BlastCore:return "在野外使用 G 投放爆裂芯，对目标区域造成爆炸。";
    case ItemId.SoundDecoy:return "在野外使用 T 投放声纹诱饵，干扰生物行动。";
    default:return "已完成锻造的精炼金属。可在飞船维修终端或营地制作中使用。";
   }
  }
 }
}
