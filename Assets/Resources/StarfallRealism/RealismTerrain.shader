Shader "Starfall/RealismTerrain" {
 Properties {_MainTex("Scanned albedo",2D)="white"{} _NormalTex("Scan normal",2D)="bump"{} _RoughTex("Roughness",2D)="white"{} _AOTex("AO",2D)="white"{} _Scale("Repeats per metre",Float)=.32 }
 SubShader {Tags {"RenderType"="Opaque"} LOD 250
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex,_NormalTex,_RoughTex,_AOTex;float _Scale;
 struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
 void vert(inout appdata_full v){float3 n=normalize(v.normal);float3 axis=abs(n.y)<.95?float3(0,1,0):float3(1,0,0);v.tangent=float4(normalize(cross(axis,n)),1);}
 float3 triplanar(sampler2D map,float3 p,float3 w){return tex2D(map,p.zy).rgb*w.x+tex2D(map,p.xz).rgb*w.y+tex2D(map,p.xy).rgb*w.z;}
 void surf(Input IN,inout SurfaceOutputStandard o){float3 n=normalize(WorldNormalVector(IN,float3(0,0,1))),w=pow(abs(n),4);w/=max(dot(w,1),.0001);float3 p=IN.worldPos*_Scale;float3 albedo=triplanar(_MainTex,p,w);o.Albedo=albedo*.88;o.Occlusion=lerp(1,triplanar(_AOTex,p,w).r,.65);o.Smoothness=1-triplanar(_RoughTex,p,w).r;o.Metallic=0;
 // The playable terrain is mostly horizontal; scan normal is attenuated on steep projections.
 float3 detail=UnpackNormal(tex2D(_NormalTex,p.xz));o.Normal=normalize(lerp(float3(0,0,1),detail,.55*saturate(n.y)));}
 ENDCG }Fallback "Standard"
}
