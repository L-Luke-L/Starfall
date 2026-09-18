Shader "Starfall/NebulaStrand" {
 Properties { _Clock("Clock",Float)=0 _Layer("Layer",Float)=0 _Drift("Drift",Float)=.02 _Tint("Tint",Color)=(.12,.2,.3,.4) }
 SubShader {Tags {"Queue"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha One
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Clock,_Layer,_Drift;float4 _Tint;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){float v=0,a=.5;for(int j=0;j<5;j++){v+=noise(p)*a;p=p*2.06+11;a*=.5;}return v;}
 fixed4 frag(v2f i):SV_Target{
  float2 uv=floor(i.uv*float2(1600,900))/float2(1600,900);
  float2 p=uv*float2(7,5)+float2(_Clock*_Drift,_Clock*_Drift*.22)+_Layer*17;
  float warp=fbm(p*1.2),n=fbm(p*2.2+warp);
  float center=.30+uv.x*.52+sin(uv.x*5+_Layer)*.025+(_Layer-1)*.025;
  float band=exp(-pow((uv.y-center+(warp-.5)*.20)*8,2));
  float cloud=saturate((n-.28)*2.4)*band;
  float lane=fbm(p*5+float2(0,-_Clock*_Drift*.8));
  cloud*=smoothstep(.22,.62,lane);
  float starDust=pow(saturate(noise(p*110)),18)*band*.34;
  float alpha=(cloud+starDust)*_Tint.a*smoothstep(.05,.28,uv.y);
  return float4(_Tint.rgb*(.63+n*.7),alpha);
 }
 ENDCG }
 }
}
