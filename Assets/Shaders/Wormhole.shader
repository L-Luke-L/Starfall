Shader "Starfall/Wormhole" {
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 fixed4 frag(v2f i):SV_Target {
  float2 p=(i.uv-.5)*2;float r=length(p);float a=atan2(p.y,p.x);float t=_Time.y;
  float edge=exp(-pow((r-.78)/.027,2));float haze=exp(-pow((r-.79)/.15,2));
  float swirl=pow(saturate(sin(a*4+r*19-t*.9)),8)*smoothstep(.12,.73,r)*(1-smoothstep(.68,.80,r));
  float arcs=pow(saturate(sin(a*3-t*.5)),3)*exp(-pow((r-.70)/.035,2));
  float2 st=float2(a*8+r*5+t*.04,r*26-t*.11);float stars=step(.986,hash(floor(st)))*(1-smoothstep(.04,.16,length(frac(st)-.5)))*(1-smoothstep(.62,.75,r));
  float3 c=float3(.004,.013,.030)+float3(.022,.060,.12)*swirl+float3(.18,.76,.82)*edge+float3(.08,.20,.32)*haze+float3(.33,.2,.50)*arcs+stars*float3(.60,.77,.92);
  float alpha=max((1-smoothstep(.69,.77,r))*.97,haze*.7);return float4(c,alpha);
 }
 ENDCG }
 }
}
