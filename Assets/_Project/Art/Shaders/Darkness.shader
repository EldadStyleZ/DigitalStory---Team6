// Draws the night over the town, cleared by lights and warmed by their glow.
// Blending matches the prototype: each light multiplies the darkness away, then glow is added on top.
Shader "DigitalStory/Darkness"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define MOST_LIGHTS 32

            // xy: position in the scene. z: reach. w: how much it clears the darkness.
            float4 _Lights[MOST_LIGHTS];

            // rgb: glow color, already scaled by strength. w: glow reach, or 0 for none.
            float4 _Glows[MOST_LIGHTS];

            float _LightCount;
            float4 _Darkness;
            float4 _DawnWash;

            struct VertexInput
            {
                float4 position : POSITION;
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 scenePosition : TEXCOORD0;
            };

            FragmentInput Vertex(VertexInput input)
            {
                FragmentInput output;
                output.position = UnityObjectToClipPos(input.position);
                output.scenePosition = mul(unity_ObjectToWorld, input.position).xy;
                return output;
            }

            float4 Fragment(FragmentInput input) : SV_Target
            {
                float cleared = 1.0;
                float3 glow = float3(0.0, 0.0, 0.0);

                for (int index = 0; index < _LightCount; index++)
                {
                    float4 light = _Lights[index];
                    float4 lightGlow = _Glows[index];
                    float distanceToLight = distance(input.scenePosition, light.xy);

                    cleared *= 1.0 - (light.w * saturate(1.0 - (distanceToLight / light.z)));

                    if (lightGlow.w > 0.0)
                    {
                        glow += lightGlow.rgb * saturate(1.0 - (distanceToLight / lightGlow.w));
                    }
                }

                float darkness = _Darkness.a * cleared;
                float3 color = _Darkness.rgb * darkness;

                color = (_DawnWash.rgb * _DawnWash.a) + (color * (1.0 - _DawnWash.a));
                darkness = _DawnWash.a + (darkness * (1.0 - _DawnWash.a));

                return float4(color + glow, darkness);
            }
            ENDCG
        }
    }
}
