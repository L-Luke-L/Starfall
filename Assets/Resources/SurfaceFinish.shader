Shader "Hidden/Starfall/SurfaceFinish" {
 Properties { _MainTex("Scene",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 fixed4 frag(v2f_img i):SV_Target {
  float4 source=tex2D(_MainTex,i.uv);
  float3 color=max(source.rgb,0);
  // Gentle highlight shoulder; no exposure pumping or motion-dependent effects.
  float3 compressed=color*(1+color*.18)/(1+color*.28);
  float luminance=dot(compressed,float3(.2126,.7152,.0722));
  compressed=lerp(luminance.xxx,compressed,.96);
  float2 centered=(i.uv-.5)*2;
  float edge=smoothstep(.4,1.6,dot(centered,centered));
  return float4(compressed*(1-edge*.075),source.a);
 }
 ENDCG }
 } Fallback Off
}
