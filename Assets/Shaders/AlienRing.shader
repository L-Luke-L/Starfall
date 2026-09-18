Shader "Starfall/AlienRing" {
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f{float4 pos:SV_POSITION;float2 ringCoord:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.ringCoord=v.vertex.xz;return o;}
 fixed4 frag(v2f i):SV_Target{float r=length(i.ringCoord);float grain=.65+.18*sin(r*410)+.08*sin(r*971);float edge=smoothstep(.64,.67,r)*(1-smoothstep(.96,1,r));float gap=1-.83*exp(-pow((r-.81)/.009,2));return float4(float3(.48,.47,.40)*grain,edge*gap*.60);}
 ENDCG }
 }
}
