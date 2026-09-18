Shader "Starfall/AlienSky" {
 Properties { _Tint("Tint",Color)=(.11,.19,.28,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct app {float4 vertex:POSITION;};struct vary {float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
 vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.ray=v.vertex.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);}
 fixed4 frag(vary i):SV_Target {
  float3 d=normalize(i.ray);float up=saturate(d.y);
  float3 color=lerp(float3(.22,.29,.32),float3(.018,.032,.075),pow(up,.42));
  float band=exp(-abs(dot(d,normalize(float3(.2,.66,-.72))))*13);
  float n=noise(d*6+float3(_Time.y*.002,0,0))*.6+noise(d*19)*.25+noise(d*49)*.15;
  color+=band*pow(n,2)*float3(.12,.13,.23)*smoothstep(.04,.35,up);
  float3 p=d*340,cell=floor(p);float star=1-smoothstep(.02,.12,length(frac(p)-.5));star*=step(.988,hash(cell));
  color+=star*(.65+.35*sin(_Time.y*(.7+hash(cell)*2)+hash(cell)*50))*smoothstep(.06,.38,up)*float3(.8,.9,1);
  float sun=dot(d,normalize(float3(.7,.14,.45)));color+=pow(saturate(sun),35)*float3(.36,.20,.09)+pow(saturate(sun),1600)*float3(1,.81,.51);
  return float4(color,1);
 }
 ENDCG }
 }
}
