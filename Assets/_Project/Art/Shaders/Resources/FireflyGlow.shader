// A firefly: a small soft dot of warm light with a hot centre and a halo that fades to nothing, added onto what is behind
// it, so it glows instead of reading as a flat disc. NestView's motes draw with it; their alpha is how lit they are.
Shader "DigitalStory/FireflyGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.95, 1, 0.45, 1)
        _Intensity ("Intensity", Range(0, 8)) = 2.2
        _Core ("Core Size", Range(0.02, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
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
            UNITY_INSTANCING_BUFFER_END(Props)

            float _Intensity;
            float _Core;

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

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float distance = length(input.uv - 0.5) * 2.0;           // 0 at the centre, 1 at the quad's edge
                float core = exp(-(distance * distance) / (_Core * _Core));
                float halo = pow(saturate(1.0 - distance), 3.0) * 0.45;
                // The core burns whiter than the halo, like a real firefly's lantern.
                float3 light = color.rgb * halo + lerp(color.rgb, float3(1, 1, 0.85), 0.5) * core;
                return float4(light * color.a * _Intensity, 0);
            }
            ENDCG
        }
    }
}
