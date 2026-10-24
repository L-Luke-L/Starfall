Shader "Starfall/RealismStarSky" {
 Properties {_StarMap("NASA star catalogue panorama",2D)="black"{} _StarExposure("Visible sky exposure",Range(0,2))=.12}
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _StarMap;float _StarExposure;float4 _SFSunDirection;
 struct app{float4 vertex:POSITION;};struct v2f{float4 position:SV_POSITION;float3 ray:TEXCOORD0;};
 v2f vert(app v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.ray=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target{float3 d=normalize(i.ray);float2 uv=float2(atan2(d.z,d.x)/6.2831853+.5,asin(clamp(d.y,-1,1))/3.14159265+.5);float3 stars=tex2D(_StarMap,uv).rgb*_StarExposure;float up=saturate(d.y);float3 gradient=lerp(float3(.12,.17,.21),float3(.006,.011,.025),pow(up,.45));float sun=dot(d,normalize(_SFSunDirection.xyz));gradient+=pow(saturate(sun),80)*float3(.17,.12,.065);return float4(gradient+stars*smoothstep(.02,.3,up),1);}
 ENDCG }}
}
