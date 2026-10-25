Shader "Starfall/RealismRing" {
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _SFSunDirection,_SFPlanet,_SFRingPlane,_SFRingRadii;
 struct v2f{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {float3 sun=normalize(_SFSunDirection.xyz),q=i.world-_SFPlanet.xyz;float r=length(q);float t=saturate((r-_SFRingRadii.x)/max(.001,_SFRingRadii.y-_SFRingRadii.x));float grain=.38+.10*sin(t*9)+.06*sin(t*27)+.07*sin(t*83)+.035*sin(t*193)+.015*sin(t*457);float gap=1-.94*exp(-pow((t-.63)/.018,2));gap*=1-.72*exp(-pow((t-.34)/.008,2));float density=saturate(grain*gap)*smoothstep(0,.03,t)*(1-smoothstep(.98,1,t));
 float along=-dot(q,sun);float shadow=along>0&&length(q+sun*along)<_SFPlanet.w?.085:1;float incidence=abs(dot(normalize(i.normal),sun));float forward=pow(saturate(dot(normalize(_WorldSpaceCameraPos-i.world),-sun)),6);float3 c=lerp(float3(.25,.22,.17),float3(.61,.56,.44),saturate(grain));c*=shadow*(.08+incidence*.82+forward*.35);float alpha=1-exp(-density*1.4/max(.25,abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))));return float4(c,alpha);}
 ENDCG }}
}
