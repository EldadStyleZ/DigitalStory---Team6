// Soft smoke puffs for particles: each billboard is shaded as a round, softly lit ball of smoke (a gentle light side from
// the moon, a cool rim), with a lumpy noise edge. Instead of fading, a puff dissolves: the particle's alpha is how much
// of it is left, so it breaks up into wisps at birth and death and its edge always stays crisp.
// Needs the particle vertex streams Position, Color, UV and StableRandom.x (into TEXCOORD0.z).
Shader "DigitalStory/StylizedSmoke"
{
    Properties
    {
        _Light ("Lit Colour", Color) = (0.6, 0.62, 0.68, 1)
        _Shadow ("Shadow Colour", Color) = (0.3, 0.32, 0.38, 1)
        _Rim ("Rim Colour", Color) = (0.55, 0.65, 0.85, 1)
        _Lumps ("Edge Lumps", Range(0, 1)) = 0.45
        _Opacity ("Opacity", Range(0, 1)) = 0.7
        _Softness ("Edge Softness", Range(0.02, 1)) = 0.45
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+50" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _Light;
            float4 _Shadow;
            float4 _Rim;
            float _Lumps;
            float _Opacity;
            float _Softness;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.uv.xy * 2.0 - 1.0;
                float seed = input.uv.z * 37.0;
                float r2 = dot(p, p);

                // A lumpy round outline that slowly churns.
                float churn = _Time.y * 0.25;
                float n = Noise(p * 2.2 + seed + churn) * 0.65 + Noise(p * 4.7 - seed - churn) * 0.35;
                float body = 1.0 - r2 + (n - 0.5) * _Lumps;

                // Dissolve instead of fading: what is left of the puff is its alpha.
                float left = input.color.a;
                float edge = body - (1.0 - left) * 0.9 - 0.05;
                // A soft edge, so the smoke reads as haze rather than a cut-out.
                float alpha = saturate(edge / _Softness);
                alpha *= alpha;

                // A round ball facing the camera, lit by the moon in two tones.
                float3 normal = normalize(float3(p.x, p.y, sqrt(saturate(1.0 - r2)) + 0.25));
                float3 toLight = normalize(mul((float3x3)UNITY_MATRIX_V, _WorldSpaceLightPos0.xyz));
                float lambert = dot(normal, toLight) * 0.5 + 0.5 + (n - 0.5) * 0.35;
                float tone = smoothstep(0.15, 0.95, lambert);
                float3 color = lerp(_Shadow.rgb, _Light.rgb * input.color.rgb, tone);
                float rim = pow(saturate(1.0 - normal.z), 3.0) * 0.25;
                color += _Rim.rgb * rim * tone;

                float4 result = float4(color, alpha * _Opacity);
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
}
