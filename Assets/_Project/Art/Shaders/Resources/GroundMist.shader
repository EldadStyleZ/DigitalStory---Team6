// Low ground mist: a soft sheet of haze lying just above the grass, its shape a slow drifting noise in world space, fading
// out at the sheet's edges and wherever it meets the ground, the trees or the water (soft against the depth texture).
Shader "DigitalStory/GroundMist"
{
    Properties
    {
        _Color ("Color", Color) = (0.55, 0.66, 0.78, 1)
        _Density ("Density", Range(0, 1)) = 0.32
        _Scale ("Noise Scale", Range(0.01, 0.5)) = 0.07
        _Drift ("Drift Speed", Range(0, 2)) = 0.35
        _Soft ("Soft Edge (m)", Range(0.05, 4)) = 1.2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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

            float4 _Color;
            float _Density;
            float _Scale;
            float _Drift;
            float _Soft;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float4 screen : TEXCOORD2;
                UNITY_FOG_COORDS(3)
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
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                for (int octave = 0; octave < 4; octave++)
                {
                    sum += Noise(p) * amp;
                    p = p * 2.03 + 17.1;
                    amp *= 0.5;
                }

                return sum;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.world = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.screen = ComputeScreenPos(output.pos);
                output.screen.z = -UnityObjectToViewPos(input.vertex).z;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float t = _Time.y * _Drift;
                float2 p = input.world.xz * _Scale;
                float shape = Fbm(p + float2(t * 0.6, t * 0.25)) * 0.6 + Fbm(p * 1.7 - float2(t * 0.3, -t * 0.5)) * 0.4;
                shape = smoothstep(0.35, 0.75, shape);

                // Fade to nothing at the sheet's rim, so no square edge shows.
                float rim = saturate(1.0 - length(input.uv - 0.5) * 2.0);
                rim = rim * rim * (3.0 - 2.0 * rim);

                // Soft where it meets anything behind it.
                float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screen)));
                float soft = saturate((scene - input.screen.z) / _Soft);

                float alpha = shape * rim * soft * _Density;
                float4 color = float4(_Color.rgb, alpha);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
