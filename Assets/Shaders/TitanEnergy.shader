Shader "Starfall/TitanEnergy" {
 Properties { _Color("Color",Color)=(.1,.7,1,.3) _Beam("Beam",Float)=0 _Flow("Flow",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Off
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct app { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
   struct varying { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   float4 _Color;float _Beam,_Flow;
   varying vert(app v){varying o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   float4 frag(varying i):SV_Target {
    if(_Beam>.5){float width=abs(i.uv.y*2-1);float core=pow(saturate(1-width),2);float streak=.8+.2*sin(i.uv.x*95-_Flow*24+sin(i.uv.y*17));return float4(lerp(_Color.rgb,float3(.8,1,1),core*.65),_Color.a*core*streak);}
    float edge=pow(abs(i.uv.x*2-1),8);float ripple=pow(saturate(sin(i.uv.y*95-_Flow*5)),12)*.14;return float4(_Color.rgb,_Color.a*(.10+.75*edge+ripple));
   }
   ENDCG
  }
 }
}
