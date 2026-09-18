Shader "Starfall/AnchoredCombustion" {
 Properties { _Clock("Clock",Float)=0 _Seed("Seed",Float)=1 _Wind("Wind",Float)=.24 }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Clock,_Seed,_Wind;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 f=frac(p),i=floor(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){return noise(p)*.57+noise(p*2.03+7)*.28+noise(p*4.11)*.15;}
 fixed4 frag(v2f i):SV_Target{
  float2 uv=i.uv;float t=_Clock*1.35+_Seed*12;
  float n=fbm(float2(uv.x*7+_Seed,uv.y*5-t*2));
  float bend=(fbm(float2(uv.y*4,t*.63))-.5)*uv.y*.63+_Wind*uv.y*.30;
  float x=uv.x-.5-bend;
  float width=.36*pow(saturate(1-uv.y),.8)+.014;
  float shape=width-abs(x)+(n-.5)*(.10+uv.y*.26);
  float envelope=smoothstep(-.025,.065,shape)*smoothstep(1,.60,uv.y);
  float flow=fbm(float2((uv.x+bend)*12,uv.y*7-t*3)+n*1.7);
  float filaments=smoothstep(.29,.67,flow);
  float a=envelope*(.10+.79*filaments)*smoothstep(0,.028,uv.y);
  float heat=saturate(filaments*1.10+envelope*.20-uv.y*.34);
  float3 color=lerp(float3(.49,.060,.012),float3(.98,.34,.035),heat);
  float core=pow(saturate((heat-.60)*2.5),2)*(1-uv.y);
  color=lerp(color,float3(1,.73,.27),core*.78);
  return float4(color,a);
 }
 ENDCG }
 }
}
