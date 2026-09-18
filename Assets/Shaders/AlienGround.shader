Shader "Starfall/AlienGround" {
 Properties { _Color("Rock",Color)=(.25,.3,.32,1) _Dust("Dust",Color)=(.49,.38,.26,1) _Scale("Grain scale",Float)=1 _Metallic("Metallic",Range(0,1))=.1 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color,_Dust;float _Scale,_Metallic;
 struct Input { float3 worldPos;float3 worldNormal; };
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y),f.z);}
 void surf(Input IN,inout SurfaceOutputStandard o){
  float3 p=IN.worldPos*_Scale;float broad=noise(p*.23),mid=noise(p*2.8),grain=noise(p*24);
  float strata=sin(p.y*4+noise(p*.6)*2)*.5+.5;
  float upward=saturate(IN.worldNormal.y);float dust=saturate(broad*.8+upward*.23-.17);
  o.Albedo=lerp(_Color.rgb,_Dust.rgb,dust)*(.65+mid*.35+grain*.20)*(1+strata*.12*(1-upward));
  o.Metallic=_Metallic;o.Smoothness=.14+mid*.13;o.Occlusion=.8+grain*.2;
 }
 ENDCG
 } FallBack "Diffuse"
}
