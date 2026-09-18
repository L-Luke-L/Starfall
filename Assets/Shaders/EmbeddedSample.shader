Shader "Starfall/EmbeddedSample" {
 Properties { [PerRendererData] _MainTex("Mineral",2D)="white"{} }
 SubShader {Tags {"Queue"="Transparent" "CanUseSpriteAtlas"="True"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
 fixed4 frag(v i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;float burial=smoothstep(.44,.12,i.uv.y);c.rgb*=lerp(float3(1,1,1),float3(.46,.57,.62),burial);c.a*=1-burial*.36;return c;}
 ENDCG }
 }
}
