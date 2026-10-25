Shader "Starfall/AlienGround" {
 Properties { _Color("Rock",Color)=(.25,.3,.32,1) _Dust("Dust",Color)=(.49,.38,.26,1) _Scale("Grain scale",Float)=1 _Metallic("Metallic",Range(0,1))=.1 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 fixed4 _Color,_Dust;float _Scale,_Metallic;
 struct Input { float3 worldPos;float3 worldNormal; INTERNAL_DATA };
 // Procedural rock meshes have no UV tangents. Supply a stable basis for relief.
 void vert(inout appdata_full v){
  float3 n=normalize(v.normal);
  float3 axis=abs(n.y)<.95?float3(0,1,0):float3(1,0,0);
  v.tangent=float4(normalize(cross(axis,n)),1);
 }
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y),f.z);}
 void surf(Input IN,inout SurfaceOutputStandard o){
  float3 p=IN.worldPos*_Scale;float broad=noise(p*.23),mid=noise(p*2.8);
  // Fade subpixel grain rather than allowing distant rocks to shimmer.
  float grainVisibility=1-smoothstep(.025,.16,max(length(ddx(p)),length(ddy(p))));
  float grain=lerp(.5,noise(p*24),grainVisibility);
  float strata=sin(p.y*4+noise(p*.6)*2)*.5+.5;
  float upward=saturate(WorldNormalVector(IN,float3(0,0,1)).y);float dust=saturate(broad*.8+upward*.23-.17)*smoothstep(.15,.85,upward);
  o.Albedo=lerp(_Color.rgb,_Dust.rgb,dust)*(.65+mid*.35+grain*.20)*(1+strata*.12*(1-upward));
  // World-space grain remains continuous across the terrain and independent rocks.
  // Screen derivatives add subtle relief without changing geometry or collision.
  float erosion=noise(p*.8);
  float seam=1-smoothstep(.025,.085,abs(sin(p.y*4+erosion*2)));
  o.Albedo*=1-seam*.16*(1-upward);
  float relief=mid*.055+grain*.012-seam*.018*(1-upward);
  float3 dpdx=ddx(IN.worldPos),dpdy=ddy(IN.worldPos);
  float3 n=normalize(WorldNormalVector(IN,float3(0,0,1)));
  float3 r1=cross(dpdy,n),r2=cross(n,dpdx);
  float determinant=dot(dpdx,r1);
  float3 gradient=sign(determinant)*(ddx(relief)*r1+ddy(relief)*r2)/max(abs(determinant),.00001);
  float3 perturbed=normalize(n-gradient*.32);
  float3 tangent=WorldNormalVector(IN,float3(1,0,0));
  float3 bitangent=WorldNormalVector(IN,float3(0,1,0));
  o.Normal=normalize(float3(dot(perturbed,tangent),dot(perturbed,bitangent),dot(perturbed,n)));
  o.Metallic=_Metallic*(1-dust*.65);
  o.Smoothness=lerp(.24,.09,dust)+mid*.055;
  o.Occlusion=.87+grain*.13;
 }
 ENDCG
 } FallBack "Diffuse"
}
