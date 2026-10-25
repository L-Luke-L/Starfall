Shader "Starfall/RealismPBR" {
 Properties {_AlphaClip("Alpha clip",Float)=0 _Cutoff("Cutoff",Range(0,1))=.5 _Color("Tint",Color)=(1,1,1,1) _MainTex("Albedo",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1 _MetallicGlossMap("Metal R / smoothness A",2D)="white"{} _MetalMapScale("Metal map multiplier",Range(0,1))=1 _Metallic("Metallic",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.2 _GlossMapScale("Smoothness scale",Float)=1 _OcclusionMap("AO G",2D)="white"{} _DetailNormalMap("Detail normal",2D)="bump"{} _DetailNormalMapScale("Detail strength",Float)=.2 _EmissionMap("Emission",2D)="white"{} _EmissionColor("Emission tint",Color)=(0,0,0,1) [Enum(UnityEngine.Rendering.CullMode)]_Cull("Cull",Float)=2 }
 SubShader {Tags {"RenderType"="Opaque"} Cull [_Cull] LOD 250
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 #pragma multi_compile __ _NORMALMAP
 #pragma multi_compile __ _METALLICGLOSSMAP
 #pragma multi_compile __ _DETAIL_MULX2
 #include "UnityStandardUtils.cginc"
 sampler2D _MainTex,_BumpMap,_MetallicGlossMap,_OcclusionMap,_DetailNormalMap,_EmissionMap;fixed4 _Color,_EmissionColor;half _AlphaClip,_Cutoff,_Metallic,_MetalMapScale,_Glossiness,_GlossMapScale,_BumpScale,_DetailNormalMapScale;
 struct Input{float2 uv_MainTex;float2 uv_BumpMap;float2 uv_DetailNormalMap;float facing:VFACE;};
 void surf(Input IN,inout SurfaceOutputStandard o){fixed4 baseColor=tex2D(_MainTex,IN.uv_MainTex);if(_AlphaClip>.5)clip(baseColor.a-_Cutoff);o.Albedo=baseColor.rgb*_Color.rgb;o.Normal=float3(0,0,1);
 #ifdef _NORMALMAP
 o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
 #endif
 #ifdef _DETAIL_MULX2
 o.Normal=BlendNormals(o.Normal,UnpackScaleNormal(tex2D(_DetailNormalMap,IN.uv_DetailNormalMap),_DetailNormalMapScale));
 #endif
 o.Normal*=IN.facing<0?-1:1;o.Metallic=_Metallic;o.Smoothness=_Glossiness;
 #ifdef _METALLICGLOSSMAP
 fixed4 mr=tex2D(_MetallicGlossMap,IN.uv_MainTex);o.Metallic=mr.r*_MetalMapScale;o.Smoothness=mr.a*_GlossMapScale;
 #endif
 o.Occlusion=tex2D(_OcclusionMap,IN.uv_MainTex).g;o.Emission=tex2D(_EmissionMap,IN.uv_MainTex).rgb*_EmissionColor.rgb;o.Alpha=1;}
 ENDCG }Fallback "Standard"
}
