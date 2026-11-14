// The smoke cloud's reach on the ground: a soft haze inside, a crisp pale rim, and slow wisps curling round it.
// Draws on a unit-radius disc (object space x and z run -1 to 1); the transform's scale sets the real radius.
Shader "DigitalStory/SmokeGroundRing"
{
    Properties
    {
        _Color ("Colour", Color) = (0.62, 0.68, 0.85, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-1" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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
            float _Opacity;

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 local : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            Varyings Vertex(float4 vertex : POSITION)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(vertex);
                output.local = vertex.xz;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float r = length(input.local);
                float angle = atan2(input.local.y, input.local.x);
                float t = _Time.y;

                float rim = smoothstep(0.9, 0.95, r) * (1.0 - smoothstep(0.98, 1.0, r));
                float haze = (1.0 - r) * 0.18;
                // Two wisps chasing each other round the inside of the rim.
                float wisp = sin(angle * 3.0 - t * 0.8 + r * 6.0) * 0.5 + 0.5;
                wisp = smoothstep(0.75, 0.95, wisp) * smoothstep(0.55, 0.85, r) * (1.0 - smoothstep(0.88, 0.92, r));

                float alpha = saturate(rim * 0.55 + haze + wisp * 0.25) * _Opacity;
                float4 result = float4(_Color.rgb, alpha);
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
}
