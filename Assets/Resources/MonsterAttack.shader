Shader "Starfall/Monster Attack" {
    Properties {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Profile ("Profile", Float) = 0
        _Billboard ("Billboard", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Input {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct Output {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            float4 _Tint;
            float _Profile, _Billboard;

            Output vert(Input v) {
                Output o;
                float3 center = UnityObjectToViewPos(float3(0,0,0));
                float2 scale = float2(length(unity_ObjectToWorld._m00_m10_m20),
                                      length(unity_ObjectToWorld._m01_m11_m21));
                o.position = _Billboard > .5
                    ? mul(UNITY_MATRIX_P, float4(center.xy + v.vertex.xy * scale, center.z, 1))
                    : UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(Output i) : SV_Target {
                float4 tint = i.color * _Tint;
                if (_Profile > 1.5) {
                    float radius = length(i.uv * 2 - 1);
                    float falloff = 1 - smoothstep(.0, 1, radius);
                    float ripple = pow(saturate(sin(radius * 24 - _Time.y * 11)), 6);
                    return float4(tint.rgb, tint.a * falloff * (.18 + ripple * .11));
                }
                if (_Profile > .5) {
                    float radius = length(i.uv * 2 - 1);
                    float halo = pow(saturate(1 - radius), 2.5);
                    float core = pow(saturate(1 - radius), 7);
                    return float4(lerp(tint.rgb, float3(1,.98,.88), core * .85),
                                  tint.a * (halo * .52 + core * .48));
                }
                float across = abs(i.uv.y * 2 - 1);
                float halo = pow(saturate(1 - across), 1.8);
                float filament = pow(saturate(1 - across), 8);
                float flow = .81 + .19 * sin(i.uv.x * 75 - _Time.y * 21);
                return float4(lerp(tint.rgb, float3(1,.96,.86), filament * .70),
                              tint.a * flow * (.52 * halo + .48 * filament));
            }
            ENDCG
        }
    }
}
