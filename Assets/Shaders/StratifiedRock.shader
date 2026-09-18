Shader "Starfall/StratifiedRock" {
 Properties { _MainTex("Continuous basalt material",2D)="gray"{} _Tint("Rock tone",Color)=(1,1,1,1) }
 SubShader {Tags {"Queue"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;};struct v2f{float4 pos:SV_POSITION;float2 world:TEXCOORD0;};float4 _Tint;sampler2D _MainTex;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 f=frac(p),i=floor(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 fixed4 frag(v2f i):SV_Target {
  float2 p=floor(i.world*48)/48;float depth=max(0,-p.y-3.9);
  float warp=noise(float2(p.x*.16,depth*.23))*.48+sin(p.x*.29)*.09;
  float bed=depth+warp;
  float broad=noise(float2(p.x*.21,depth*.43));
  float2 uv=p*float2(.115,.19)+float2(warp*.035,0);
  float3 stone=tex2D(_MainTex,uv).rgb;
  float3 tone=lerp(float3(.75,.64,.48),float3(.46,.58,.64),smoothstep(.10,3.3,depth));
  tone=lerp(tone,float3(.32,.43,.49),smoothstep(6,16,depth));
  float3 base=stone*tone*(.92+broad*.14);
  float grain=(hash(floor(p*17))-.5)*.003;
  float centerLight=exp(-p.x*p.x*.036)*exp(-depth*.09);
  base+=grain+float3(.005,.018,.020)*centerLight;
  float vignette=1-smoothstep(9,19,abs(p.x))*.35;
  return float4(base*vignette*_Tint.rgb,1);
 }
 ENDCG }
 }
}
