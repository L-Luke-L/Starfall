Shader "Starfall/LivingNebula" {
 Properties { _MainTex ("Texture",2D)="white"{} }
 SubShader { Tags { "Queue"="Background" "RenderType"="Opaque" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3.-2.*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){float v=0.,a=.5;for(int i=0;i<5;i++){v+=a*noise(p);p=p*2.03+13.7;a*=.5;}return v;}
 fixed4 frag(v2f i):SV_Target {
 float2 uv=floor(i.uv*float2(1000,600))/float2(1000,600);
 float3 col=lerp(float3(.012,.024,.040),float3(.016,.025,.052),uv.y);
 return float4(col,1);
 }
 ENDCG }
 }
}
