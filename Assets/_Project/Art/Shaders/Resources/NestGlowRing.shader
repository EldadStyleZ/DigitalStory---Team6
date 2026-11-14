// The ring of light on the ground around a wasp nest. NestView draws two: a faint full ring while the nest is open, and a
// bright one that fills clockwise from the top while the core collects it. It is a band of light, not a painted stripe:
// a hot core line with a soft halo that fades into the ground, a shimmer that runs around it, and a flare at the
// filling front. Additive, so it lights the grass and the ground under it rather than covering them.
// uv.x runs once around the ring, clockwise from the top (0 to 1); uv.y runs across the band (0 inside, 1 outside).
// The mesh also has a low wall of light standing on the ring (uv2.x is 1 on it, and uv.y runs up it), so the light
// rises off the ground like a glowing curtain and fades out as it climbs.
Shader "DigitalStory/NestGlowRing"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.8, 0.3, 1)
        _Progress ("Progress", Range(0, 1)) = 1
        _Intensity ("Intensity", Range(0, 8)) = 2
        _Track ("Unfilled Track", Range(0, 1)) = 0.12
        _Sharpness ("Core Sharpness", Range(1, 200)) = 60
        _Halo ("Halo", Range(0, 2)) = 0.6
        _Shimmer ("Shimmer", Range(0, 1)) = 0.25
        _Wall ("Rising Light", Range(0, 2)) = 0.7
        _WallFade ("Rising Light Fade", Range(0.5, 6)) = 2.2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Progress)
                UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
            UNITY_INSTANCING_BUFFER_END(Props)

            float _Track;
            float _Sharpness;
            float _Halo;
            float _Shimmer;
            float _Wall;
            float _WallFade;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float isWall : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.isWall = input.uv2.x;
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float progress = UNITY_ACCESS_INSTANCED_PROP(Props, _Progress);
                float intensity = UNITY_ACCESS_INSTANCED_PROP(Props, _Intensity);

                // On the band: distance from its centre line, 0 on it and 1 at its edges. On the wall: brightest at its
                // foot and fading as it climbs.
                float across = abs(input.uv.y - 0.5) * 2.0;
                float core = exp(-across * across * _Sharpness);
                float halo = exp(-across * 4.0) * _Halo * (1.0 - across);
                float rising = pow(saturate(1.0 - input.uv.y), _WallFade) * _Wall;
                bool isWall = input.isWall > 0.5;
                core = isWall ? rising : core;
                halo = isWall ? 0.0 : halo;

                // The lit part runs from the top round to the progress, with a soft front.
                float around = input.uv.x;
                float lit = 1.0 - smoothstep(progress - 0.006, progress + 0.006, around);
                lit = progress >= 0.999 ? 1.0 : lit;

                // A flare rides the filling front, so the ring reads as charging.
                float behind = progress - around;
                float flare = (progress > 0.001 && progress < 0.999) ? exp(-behind * behind * 900.0) * step(-0.01, behind) * 2.5 : 0.0;

                float shimmer = 1.0 - _Shimmer + _Shimmer * (0.5 + 0.5 * sin(around * 75.0 - _Time.y * 5.0));
                float strength = (core + halo) * lerp(_Track, 1.0, lit) * shimmer + flare * (core + halo * 0.5);

                return float4(color.rgb * color.a * strength * intensity, 0);
            }
            ENDCG
        }
    }
}
