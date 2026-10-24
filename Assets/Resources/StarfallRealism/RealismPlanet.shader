Shader "Starfall/RealismPlanet" {
 Properties {_MainTex("Gas giant clouds",2D)="white"{} }
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _SFSunDirection,_SFPlanet,_SFRingPlane,_SFRingRadii;
 struct v2f{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.texcoord;return o;}
 float density(float r){float t=saturate((r-_SFRingRadii.x)/max(.001,_SFRingRadii.y-_SFRingRadii.x));float bands=.38+.10*sin(t*9)+.06*sin(t*27)+.07*sin(t*83)+.035*sin(t*193)+.015*sin(t*457);float gap=1-.94*exp(-pow((t-.63)/.018,2));gap*=1-.72*exp(-pow((t-.34)/.008,2));return saturate(bands*gap)*smoothstep(0,.03,t)*(1-smoothstep(.98,1,t));}
 fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.normal),sun=normalize(_SFSunDirection.xyz);float mu=dot(n,sun);float3 color=tex2D(_MainTex,i.uv).rgb;float shadow=1;float denominator=dot(sun,_SFRingPlane.xyz);
 if(abs(denominator)>.001){float t=dot(_SFPlanet.xyz-i.world,_SFRingPlane.xyz)/denominator;float3 q=i.world+sun*t-_SFPlanet.xyz;float r=length(q);if(t>.01&&r>_SFRingRadii.x&&r<_SFRingRadii.y)shadow=exp(-density(r)*.95/max(abs(denominator),.25));}
 float light=max(mu,0)*shadow;float limb=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))),4)*smoothstep(-.08,.3,mu);color=color*(.008+light*.95)*smoothstep(-.035,.025,mu)+float3(.15,.23,.32)*limb*.16;return float4(color,1);}
 ENDCG }}
}
