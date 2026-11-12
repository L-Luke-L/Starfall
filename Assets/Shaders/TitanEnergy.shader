Shader "Starfall/TitanEnergy" {
    Properties {
        _Color ("Color", Color) = (.1,.7,1,.3)
        _Beam ("Beam", Float) = 0
        _Flow ("Flow", Float) = 0
        _Charge ("Charge", Float) = 0
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
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Color;
            float _Beam, _Flow, _Charge;

            Output vert(Input v) {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float4 frag(Output i) : SV_Target {
                if (_Beam > .5) {
                    float across = abs(i.uv.y * 2 - 1);
                    float halo = pow(saturate(1 - across), 2);
                    float core = pow(saturate(1 - across), 7);
                    float turbulence = sin(i.uv.x * 90 - _Flow * 31 + i.uv.y * 24);
                    float breakup = .79 + turbulence * .16;
                    return float4(lerp(_Color.rgb, float3(.79,.95,1), core * .72),
                                  _Color.a * (halo * .61 + core * .39) * breakup);
                }
                // Keep the illuminated border inside the locked hit footprint.
                float2 margin = min(i.uv, 1 - i.uv);
                float side = (1 - smoothstep(.008, .028, margin.x)) * smoothstep(0, .003, margin.x);
                float cap = (1 - smoothstep(.001, .009, margin.y)) * smoothstep(0, .001, margin.y);
                float inner = (1 - smoothstep(.003, .013, abs(margin.x - .068))) *
                              smoothstep(.18, .38, frac(i.uv.y * 24));
                float chevron = frac(i.uv.y * 17 - _Flow * .64 - abs(i.uv.x - .5) * 1.24);
                float ripples = (1 - smoothstep(.03, .16, chevron)) * sin(i.uv.x * UNITY_PI);
                float light = .07 + .42 * max(side, cap) + .15 * inner + (.05 + _Charge * .18) * ripples;
                return float4(_Color.rgb, _Color.a * light * (.72 + _Charge * .38));
            }
            ENDCG
        }
    }
}
