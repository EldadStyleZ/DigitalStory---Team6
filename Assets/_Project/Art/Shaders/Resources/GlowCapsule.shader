// A glowing capsule bar: a dark glass pill with a lit rim, filled from the left with light that is brightest along its
// middle, a shimmer running along the fill, and a soft glow spilling round it. HiveView draws a hive's store with it.
// The quad is the capsule plus _Size.z of room on every side for the glow; uv runs 0 to 1 across the whole quad.
Shader "DigitalStory/GlowCapsule"
{
    Properties
    {
        [HDR] _Color ("Fill Color", Color) = (1, 0.78, 0.15, 1)
        _Fill ("Fill", Range(0, 1)) = 0.5
        _Size ("Width, Height, Glow Room (m)", Vector) = (2, 0.26, 0.25, 0)
        _Glass ("Glass Color", Color) = (0.05, 0.04, 0.02, 0.6)
        _Intensity ("Intensity", Range(0, 6)) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+30" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
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
                UNITY_DEFINE_INSTANCED_PROP(float, _Fill)
            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _Size;
            float4 _Glass;
            float _Intensity;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            // Signed distance to a capsule lying along x, centred: below 0 inside.
            float Capsule(float2 p, float width, float height)
            {
                float radius = height * 0.5;
                float reach = max(width * 0.5 - radius, 0.0);
                float2 q = float2(max(abs(p.x) - reach, 0.0), p.y);
                return length(q) - radius;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float fill = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Fill));

                float width = _Size.x;
                float height = _Size.y;
                float room = _Size.z;
                float2 p = (input.uv - 0.5) * float2(width + room * 2.0, height + room * 2.0);

                float d = Capsule(p, width, height);
                float edge = max(fwidth(d), 0.0005);
                float inside = smoothstep(edge, -edge, d);

                // The fill runs inside a slightly smaller capsule, so a sliver of glass shows round it.
                float inset = height * 0.16;
                float dFill = Capsule(p, width - inset * 2.0, height - inset * 2.0);
                float along = (p.x + width * 0.5) / width;
                float front = smoothstep(fill + 0.004, fill - 0.004, along);
                float fillMask = smoothstep(edge, -edge, dFill) * front * step(0.001, fill);

                // Light brightest along the middle line, and a shimmer drifting along the fill.
                float middle = 1.0 - saturate(abs(p.y) / (height * 0.5 - inset));
                float shimmer = 0.85 + 0.15 * sin(along * 40.0 - _Time.y * 4.0);
                float3 fillLight = color.rgb * (0.55 + 0.9 * middle * middle) * shimmer * _Intensity;

                // The rim: a thin lit line on the glass's edge, warmer where the fill reaches.
                float rim = exp(-abs(d) / (height * 0.06)) * 0.5;
                float3 rimLight = lerp(float3(1, 0.9, 0.6) * 0.25, color.rgb, 0.6) * rim;

                // Glow outside, stronger the fuller the bar.
                float glow = exp(-max(d, 0.0) / (room * 0.35)) * (1.0 - inside) * (0.25 + 0.75 * fill) * 0.6;

                float glassAlpha = _Glass.a * inside;
                float3 light = fillLight * fillMask + rimLight + color.rgb * glow * _Intensity * 0.5;
                float3 rgb = _Glass.rgb * glassAlpha * (1.0 - fillMask) + light;
                return float4(rgb, glassAlpha);
            }
            ENDCG
        }
    }
}
