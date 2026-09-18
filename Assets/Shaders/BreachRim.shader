Shader "Starfall/BreachRim" {
 Properties { _MainTex("Sprite",2D)="white"{} _Opening("Hole UV / radius",Vector)=(.105,.48,.067,.16) }
 SubShader{Tags{"Queue"="Transparent"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _Opening;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v vert(a p){v o;o.pos=UnityObjectToClipPos(p.vertex);o.uv=p.uv;return o;}
 fixed4 frag(v i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);float2 d=(i.uv-_Opening.xy)/_Opening.zw;c.a*=smoothstep(.84,1,dot(d,d));return c;}
 ENDCG}
 }
}
