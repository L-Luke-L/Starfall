Shader "Starfall/AlienPlanet" {
 SubShader { Tags { "RenderType"="Opaque" } Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float3 n:TEXCOORD0;float3 local:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.n=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.n);float bands=sin(i.local.y*84+sin(i.local.x*12+i.local.z*7)*1.1+sin(i.local.y*21)*.65)*.5+.5;float detail=sin(i.local.y*290+sin(i.local.z*60))*sin(i.local.x*83)*.014;float3 c=lerp(float3(.36,.44,.51),float3(.68,.59,.46),bands*.65+.15)+detail;float light=saturate(dot(n,normalize(float3(.7,.25,-.7))));return float4(c*(.20+light*.80),1);}
 ENDCG } }
}
