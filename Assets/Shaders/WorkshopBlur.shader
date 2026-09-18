Shader "Starfall/WorkshopBlur" {
 Properties { _MainTex ("Texture", 2D) = "white" {} }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   float4 _MainTex_TexelSize;
   float2 _Direction;
   fixed4 frag(v2f_img i):SV_Target {
    float2 d=_MainTex_TexelSize.xy*_Direction;
    fixed4 c=tex2D(_MainTex,i.uv)*.227027;
    c+=(tex2D(_MainTex,i.uv+d*1.384615)+tex2D(_MainTex,i.uv-d*1.384615))*.316216;
    c+=(tex2D(_MainTex,i.uv+d*3.230769)+tex2D(_MainTex,i.uv-d*3.230769))*.070270;
    return c;
   }
   ENDCG
  }
 }
}
