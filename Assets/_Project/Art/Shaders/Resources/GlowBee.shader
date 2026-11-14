// A single low poly bee from WaspMeshFactory's mesh, striped like the swarm's wasps but lit from within by _Color, with
// beating wings (uv.y marks the wing vertices). HiveView's healing bees draw with it; their alpha is how lit they are.
Shader "DigitalStory/GlowBee"
{
    Properties
    {
        _Color ("Glow Color", Color) = (0.45, 1, 0.45, 1)
        _Glow ("Glow", Range(0, 6)) = 2.2
        _FlapSpeed ("Flap Speed", Range(0, 120)) = 70
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
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

            float _Glow;
            float _FlapSpeed;

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 local : TEXCOORD0;
                float2 marks : TEXCOORD1;
                float3 normal : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float4 vertex = input.vertex;
                if (input.uv.y > 0.5)
                {
                    // Wings swing up and down round the body's axis, the farther out the more.
                    float phase = unity_ObjectToWorld._m03 * 3.1 + unity_ObjectToWorld._m23 * 1.7;
                    vertex.y += sin(_Time.y * _FlapSpeed + phase) * abs(vertex.x) * 0.9;
                }

                output.pos = UnityObjectToClipPos(vertex);
                output.local = vertex.xyz;
                output.marks = input.uv;
                output.normal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float lit = 0.55 + 0.45 * saturate(input.normal.y);

                // Bands along the body like the swarm's wasps: dark and bright, the bright ones taking the glow's colour.
                float band = step(0.5, frac((input.local.z + 0.5) * 4.5));
                float3 bright = lerp(float3(1.0, 0.85, 0.3), color.rgb, 0.65);
                float3 body = lerp(color.rgb * 0.18, bright, band) * lit;
                float3 eye = color.rgb * 1.5;
                float3 wing = lerp(float3(0.85, 0.95, 0.9), color.rgb, 0.5);
                float3 shade = input.marks.y > 0.5 ? wing : (input.marks.x > 0.5 ? eye : body);
                return float4(shade * (1.0 + _Glow * color.a * 0.5) * max(color.a, 0.15), 1);
            }
            ENDCG
        }
    }
}
