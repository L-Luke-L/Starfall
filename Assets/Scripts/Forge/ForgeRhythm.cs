using UnityEngine;

namespace Starfall {
 public enum ForgeRhythmKind { Steady, Surge, Drift }
 public static class ForgeRhythm {
  public const float WarmupSeconds=.32f;
  public const float DriftAmplitude=.05f;
  public static string Name(ForgeRhythmKind kind)=>kind==ForgeRhythmKind.Surge?"变速":kind==ForgeRhythmKind.Drift?"游移":"匀速";
  public static void Evaluate(ForgeRhythmKind kind,float elapsed,float speed,bool reverse,float center,float phase,out float pointer,out float target){
   // Integral of a smooth speed wave: always forward along the bounce path, never a random jump.
   float distance=speed*elapsed;
   if(kind==ForgeRhythmKind.Surge)distance+=speed*.17f/4.2f*(Mathf.Cos(phase)-Mathf.Cos(4.2f*elapsed+phase));
   float travel=Mathf.PingPong(distance,1);pointer=reverse?1-travel:travel;
   target=center+(kind==ForgeRhythmKind.Drift?DriftAmplitude*Mathf.Sin(elapsed*2.1f+phase):0);
  }
 }
}
