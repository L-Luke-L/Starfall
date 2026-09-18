Shader "Starfall/HullReflection" {
 Properties { _MainTex("Sprite",2D)="white"{} }
 SubShader{Tags{"Queue"="Transparent"}Cull Off ZWrite Off Blend SrcAlpha One
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v vert(a p){v o;o.pos=UnityObjectToClipPos(p.vertex);o.uv=p.uv;return o;}
 fixed4 frag(v i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);float2 d=(i.uv-float2(.11,.51))*float2(4,2);float field=exp(-dot(d,d)*5);float bright=dot(c.rgb,float3(.3,.5,.2));float flicker=.7+.15*sin(_Time.y*8.7)+.1*sin(_Time.y*19);return float4(.85,.24,.035,c.a*field*bright*flicker*.62);}
 ENDCG}
 }
}
