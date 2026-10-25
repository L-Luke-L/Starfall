Shader "Starfall/OpenMonsterSkin" {
 Properties {
  _Color("Skin tint",Color)=(0.82,0.85,0.84,1)
  _MainTex("Quaternius atlas",2D)="white"{}
  _Saturation("Atlas saturation",Range(0,1))=0.66
  _Glossiness("Smoothness",Range(0,1))=0.24
  _Metallic("Metallic",Range(0,1))=0.02
  [HDR] _EmissionColor("Combat glow",Color)=(0,0,0,1)
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;half _Saturation,_Glossiness,_Metallic;half4 _EmissionColor;
  struct Input {float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed3 source=tex2D(_MainTex,IN.uv_MainTex).rgb;half luminance=dot(source,half3(.2126,.7152,.0722));
   half chroma=max(source.r,max(source.g,source.b))-min(source.r,min(source.g,source.b));
   half skin=saturate(chroma*4)*saturate((luminance-.045)*8);
   o.Albedo=lerp(luminance.xxx,source,_Saturation)*lerp(half3(1,1,1),_Color.rgb,skin);
   o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Emission=_EmissionColor.rgb*skin;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
