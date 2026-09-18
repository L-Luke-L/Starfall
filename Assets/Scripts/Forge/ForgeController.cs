using System;
using UnityEngine;

namespace Starfall {
 public enum ForgeState { Closed, CampMenu, Ready, Timing, Striking, Success, Failure }
 public sealed partial class ForgeController : MonoBehaviour {
  public SceneDirector director;
  public OreInventory inventory;
  public Texture2D heroReady,heroLift,heroStrike,anvil,failureCG,oreArt,workshop;
  public Texture2D smithPoses,ingotSheet;
  public Shader workshopBlurShader;
  [Header("Three strikes per ore")]
  public float[] targetWidths={.12f,.10f,.085f};
  public float[] roundDurations={2.9f,2.5f,2.2f};
  public float[] pointerSpeeds={1.12f,1.30f,1.48f};
  public ForgeState State {get;private set;}
  public bool IsOpen=>State!=ForgeState.Closed;
  public bool IsWorking=>State==ForgeState.Timing||State==ForgeState.Striking;
  public bool Paused {get;private set;}
  public int Hits {get;private set;}
  public float Pointer {get;private set;}
  public float TargetCenter {get;private set;}
  public float TargetHome {get;private set;}
  public float PreviousPointer {get;private set;}
  public ForgeRhythmKind Rhythm {get;private set;}
  public float WarmupRemaining {get;private set;}
  public bool StrikeReady=>State==ForgeState.Timing&&!Paused&&WarmupRemaining<=0;
  public int RoundIndex {get;private set;}
  public float TargetWidth=>targetWidths[RoundIndex];
  public float RoundTimeFraction=>Mathf.Clamp01(TimeRemaining/roundDurations[RoundIndex]);
  public float TimeRemaining {get;private set;}
  public float StrikeAge {get;private set;}
  public float ResultAge {get;private set;}
  public string FailureReason {get;private set;}
  public RepairResource ResultMetal {get;private set;}
  public int TotalStrikes {get;private set;}
  public int LastInputFrame {get;private set;}=-1;
  UnknownOre activeOre;
  float roundElapsed;
  float rhythmPhase;
  readonly ForgeRhythmKind[] rhythmDeck={ForgeRhythmKind.Steady,ForgeRhythmKind.Surge,ForgeRhythmKind.Drift};
  int previousOpening=-1;
  bool reversePointer;
  public bool FailurePresentationActive=>State==ForgeState.Failure&&ResultAge<1.8f;
  public bool RewardPresentationActive=>State==ForgeState.Success&&ResultAge<1.15f;
  public float FailureBlur=>State==ForgeState.Failure?Mathf.SmoothStep(0,1,ResultAge/.85f):0;
  public float FailurePortraitX=>ResultAge<.52f?Mathf.Lerp(-1080,330,1-Mathf.Pow(1-Mathf.Clamp01(ResultAge/.52f),3)):ResultAge<.92f?330+(ResultAge-.52f)*85:Mathf.Lerp(364,1750,Mathf.Pow(Mathf.Clamp01((ResultAge-.92f)/.78f),2));
  bool soundEnabled=true;
  AudioSource audioSource;
  AudioClip hammerSound,failureSound;
  System.Random random=new System.Random();
  public bool OpenCamp(){
   if(IsOpen||director.RecoveryLocked||director.Transitioning||director.underground||director.hookState!=SceneDirector.HookState.Swinging||Mathf.Abs(director.player.position.x-director.camp.transform.position.x)>director.camp.radius)return false;
   InteractionFeel.Play(FeelCue.Click,.65f);State=ForgeState.CampMenu;Paused=false;return true;
  }
  public bool OpenWorkshop(){if(State!=ForgeState.CampMenu)return false;InteractionFeel.Play(FeelCue.Portal,.35f);State=ForgeState.Ready;Hits=0;return true;}
  public bool BeginForge(){
   if(Paused||FailurePresentationActive||RewardPresentationActive||(State!=ForgeState.Ready&&State!=ForgeState.Success&&State!=ForgeState.Failure))return false;
   if(!inventory.TryReserve(out activeOre)){InteractionFeel.Play(FeelCue.Deny,.55f);return false;}
   InteractionFeel.Play(FeelCue.Click,.65f);
   for(int i=2;i>0;i--){int j=random.Next(i+1);var swap=rhythmDeck[i];rhythmDeck[i]=rhythmDeck[j];rhythmDeck[j]=swap;}
   if((int)rhythmDeck[0]==previousOpening){var swap=rhythmDeck[0];rhythmDeck[0]=rhythmDeck[1];rhythmDeck[1]=swap;}
   previousOpening=(int)rhythmDeck[0];Hits=0;ResultAge=0;FailureReason="";BeginRound();return true;
  }
  void BeginRound(){
   RoundIndex=Mathf.Clamp(Hits,0,2);State=ForgeState.Timing;roundElapsed=0;reversePointer=random.Next(2)==0;Rhythm=rhythmDeck[RoundIndex];rhythmPhase=(float)random.NextDouble()*Mathf.PI*2;TargetHome=.32f+(float)random.NextDouble()*.36f;WarmupRemaining=ForgeRhythm.WarmupSeconds;TimeRemaining=roundDurations[RoundIndex];EvaluateRhythm();PreviousPointer=Pointer;
  }
  void EvaluateRhythm(){ForgeRhythm.Evaluate(Rhythm,roundElapsed,pointerSpeeds[RoundIndex],reversePointer,TargetHome,rhythmPhase,out float pointer,out float target);Pointer=pointer;TargetCenter=target;}
  public bool SubmitStrike(){
   if(!StrikeReady||LastInputFrame==Time.frameCount)return false;
   LastInputFrame=Time.frameCount;
   if(TimeRemaining<=0){Fail("犹豫太久，矿石冷却碎裂了。");return false;}
   if(Mathf.Abs(Pointer-TargetCenter)>TargetWidth*.5f){Fail(Pointer<TargetCenter?"落锤太早，矿石被敲碎了。":"错过时机，矿石被敲碎了。");return false;}
   Hits++;TotalStrikes++;StrikeAge=0;State=ForgeState.Striking;return true;
  }
  void Fail(string reason){
   if(!IsWorking)return;
   inventory.Resolve(activeOre,false);activeOre=null;State=ForgeState.Failure;ResultAge=0;FailureReason=reason;Play(false);
  }
  public bool TryClose(){
   if(IsWorking||FailurePresentationActive||RewardPresentationActive)return false;
   InteractionFeel.Play(FeelCue.Click,.5f);State=ForgeState.Closed;Paused=false;return true;
  }
  public void TogglePause(){if(IsWorking){Paused=!Paused;InteractionFeel.Play(FeelCue.Click,.5f);}}
  void Update(){
   if(!IsOpen)return;
   if(Input.GetKeyDown(KeyCode.Escape)){if(IsWorking)TogglePause();else TryClose();}
   if(!IsOpen||Paused)return;
   if(Input.GetKeyDown(KeyCode.Space)){if(State==ForgeState.CampMenu){OpenWorkshop();return;}if(State==ForgeState.Ready||((State==ForgeState.Success||State==ForgeState.Failure)&&!FailurePresentationActive&&!RewardPresentationActive)){BeginForge();return;}}
   float dt=Time.unscaledDeltaTime;
   if(State==ForgeState.Timing){
    if(WarmupRemaining>0){float consume=Mathf.Min(dt,WarmupRemaining);WarmupRemaining-=consume;dt-=consume;if(dt<=0)return;}
    roundElapsed+=dt;TimeRemaining=Mathf.Max(0,roundDurations[RoundIndex]-roundElapsed);
    PreviousPointer=Pointer;EvaluateRhythm();
    if(TimeRemaining<=0){Fail("没有及时落锤，矿石冷却碎裂了。");return;}
    if(Input.GetKeyDown(KeyCode.Space))SubmitStrike();
   }else if(State==ForgeState.Striking){
    float prior=StrikeAge;StrikeAge+=dt;if(prior<.28f&&StrikeAge>=.28f)Play(true);
    if(StrikeAge>=.88f){
     if(Hits==3){ResultMetal=activeOre.Metal;inventory.Resolve(activeOre,true);activeOre=null;State=ForgeState.Success;ResultAge=0;InteractionFeel.Play(FeelCue.Success,.8f);}
     else BeginRound();
    }
   }else if(State==ForgeState.Success||State==ForgeState.Failure)ResultAge+=dt;
  }
  void Play(bool hit){
   if(!soundEnabled)return;
   if(!audioSource){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=.24f;hammerSound=MakeSound(true);failureSound=MakeSound(false);}
   audioSource.pitch=hit?.95f+Hits*.06f:1;audioSource.PlayOneShot(hit?hammerSound:failureSound);
  }
  static AudioClip MakeSound(bool hit){
   int rate=24000,n=hit?6500:10500;var data=new float[n];var rng=new System.Random(314);
   for(int i=0;i<n;i++){float t=i/(float)rate;float env=Mathf.Exp(-t*(hit?19:7));float noise=(float)rng.NextDouble()*2-1;
    data[i]=hit?(Mathf.Sin(t*4200)+Mathf.Sin(t*6720)*.4f+noise*.7f)*env*.35f:Mathf.Sin(2*Mathf.PI*(270*t-230*t*t))*env*.4f;
   }
   var clip=AudioClip.Create(hit?"Forging metal strike":"Comic failure",n,1,rate,false);clip.SetData(data,0);return clip;
  }
  void OnDestroy(){if(hammerSound)Destroy(hammerSound);if(failureSound)Destroy(failureSound);ReleaseWorkshop();}
 }
}
