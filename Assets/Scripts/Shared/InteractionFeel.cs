using System.Collections.Generic;
using UnityEngine;

namespace Starfall {
 public enum FeelCue {Hover,Click,Deny,Step,Cast,Catch,Pickup,Portal,Shot,Hit,Kill,Reload,Slash,Chip,Hurt,Alert,Success}
 public sealed class InteractionFeel:MonoBehaviour {
  static InteractionFeel instance;
  public static bool ReducedMotion;
  static bool muted;
  public static bool Muted {get=>muted;set{muted=value;AudioListener.volume=value?0:1;}}
  public static int Played {get;private set;}
  readonly Dictionary<FeelCue,AudioClip> clips=new Dictionary<FeelCue,AudioClip>();
  readonly Dictionary<FeelCue,float> last=new Dictionary<FeelCue,float>();
  static readonly Dictionary<string,float> hover=new Dictionary<string,float>();
  AudioSource source;
  static InteractionFeel Ensure(){if(!instance){var g=new GameObject("Interaction feedback audio");DontDestroyOnLoad(g);instance=g.AddComponent<InteractionFeel>();instance.source=g.AddComponent<AudioSource>();instance.source.playOnAwake=false;instance.source.spatialBlend=0;instance.source.volume=.34f;}return instance;}
  public static void Play(FeelCue cue,float strength=1){
   if(Muted)return;var fx=Ensure();float now=Time.unscaledTime;float gap=cue==FeelCue.Deny?.35f:cue==FeelCue.Hover?.10f:cue==FeelCue.Step?.18f:.045f;
   if(fx.last.TryGetValue(cue,out var prior)&&now-prior<gap)return;fx.last[cue]=now;
   if(!fx.clips.TryGetValue(cue,out var clip)){clip=Make(cue);fx.clips[cue]=clip;}
   fx.source.PlayOneShot(clip,Mathf.Clamp01(strength));Played++;
  }
  static AudioClip Make(FeelCue cue){
   const int rate=24000;float duration=cue==FeelCue.Portal?.8f:cue==FeelCue.Success?.55f:cue==FeelCue.Pickup?.28f:cue==FeelCue.Reload?.18f:cue==FeelCue.Step?.09f:.13f;
   var data=new float[Mathf.CeilToInt(duration*rate)];var random=new System.Random(1100+(int)cue);float low=0;
   for(int i=0;i<data.Length;i++){float t=i/(float)rate,u=t/duration,noise=(float)random.NextDouble()*2-1;low=low*.78f+noise*.22f;float attack=Mathf.Min(1,t/.004f),env=attack*Mathf.Pow(1-u,2),tone=0;
    switch(cue){
     case FeelCue.Shot:tone=(Mathf.Sin(2*Mathf.PI*(950*t-1800*t*t))*.45f+noise*.27f+Mathf.Sin(t*440)*.2f)*Mathf.Exp(-t*18);break;
     case FeelCue.Step:tone=low*.48f+Mathf.Sin(t*390)*.12f;break;
     case FeelCue.Pickup:case FeelCue.Success:tone=(Mathf.Sin(t*2*Mathf.PI*(u<.45f?660:990))+Mathf.Sin(t*2*Mathf.PI*1320)*.25f)*.23f;break;
     case FeelCue.Hit:case FeelCue.Catch:case FeelCue.Chip:tone=noise*.32f+Mathf.Sin(t*4600)*.2f+Mathf.Sin(t*900)*.16f;break;
     case FeelCue.Portal:tone=low*.5f+Mathf.Sin(2*Mathf.PI*(70*t+190*t*t))*.22f;env=Mathf.Sin(u*Mathf.PI);break;
     case FeelCue.Slash:case FeelCue.Cast:tone=low*.65f+noise*.10f;env=Mathf.Sin(u*Mathf.PI);break;
     case FeelCue.Hurt:tone=low*.65f+Mathf.Sin(t*470)*.2f;break;
     case FeelCue.Reload:tone=noise*(u<.2f?.34f:.1f)+Mathf.Sin(t*2900)*.18f;break;
     case FeelCue.Deny:tone=Mathf.Sin(t*1100)*.22f;break;
     case FeelCue.Kill:tone=Mathf.Sin(t*2000)*.23f+Mathf.Sin(t*3100)*.15f;break;
     case FeelCue.Alert:tone=Mathf.Sin(t*2*Mathf.PI*(440+u*400))*.25f;break;
     default:tone=Mathf.Sin(t*(cue==FeelCue.Hover?4300:3300))*.18f;break;
    }data[i]=Mathf.Clamp(tone*env,-.8f,.8f);
   }var clip=AudioClip.Create("Feedback "+cue,data.Length,1,rate,false);clip.SetData(data,0);return clip;
  }
  public static void Text(Rect r,string text,Font font,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){
   var style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,alignment=align,normal={textColor=new Color(.005f,.014f,.021f,color.a*.85f)}};GUI.Label(new Rect(r.x+1,r.y+1,r.width,r.height),text,style);style.normal.textColor=color;GUI.Label(r,text,style);
  }
  public static bool Button(Rect r,string text,Font font,int size,Color color,bool enabled=true){
   string id=text+":"+r.x+":"+r.y;bool over=enabled&&GUI.enabled&&r.Contains(Event.current.mousePosition);hover.TryGetValue(id,out var amount);
   if(Event.current.type==EventType.Repaint){if(over&&amount<=0)Play(FeelCue.Hover,.4f);amount=Mathf.MoveTowards(amount,over?1:0,Time.unscaledDeltaTime*10);hover[id]=amount;}
   var ink=Color.Lerp(color,Color.white,amount);if(!enabled||!GUI.enabled)ink.a*=.32f;
   Text(new Rect(r.x,r.y-amount*1.5f,r.width,r.height),text,font,size,ink,TextAnchor.MiddleCenter);
   if(amount>0){GUI.color=new Color(color.r,color.g,color.b,amount*.75f);GUI.DrawTexture(new Rect(r.center.x-r.width*.26f,r.yMax-5,r.width*.52f,1),Texture2D.whiteTexture);GUI.color=Color.white;}
   bool old=GUI.enabled;GUI.enabled=old&&enabled;bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=old;if(hit)Play(FeelCue.Click,.65f);return hit;
  }
  public static void Ring(Vector2 center,float radius,float progress,Color color,float width=2){
   var original=GUI.matrix;for(int i=0;i<40*Mathf.Clamp01(progress);i++){float a=(-90+i*9)*Mathf.Deg2Rad,b=(-90+(i+1)*9)*Mathf.Deg2Rad;var start=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;var end=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
    GUI.matrix=original*Matrix4x4.TRS(start,Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg),Vector3.one);GUI.color=color;GUI.DrawTexture(new Rect(0,-width*.5f,Vector2.Distance(start,end),width),Texture2D.whiteTexture);
   }GUI.matrix=original;GUI.color=Color.white;
  }
  void OnDestroy(){foreach(var c in clips.Values)if(c)Destroy(c);if(instance==this)instance=null;}
 }
}
