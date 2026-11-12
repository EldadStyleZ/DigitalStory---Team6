// Development tool, not game art. Counts how many of the swarm's glow quads cover each pixel: it places every quad
// exactly as WaspGlow.shader does (same size, buzz, push back and shrinking of dying wasps) and adds 1 per fragment into
// a one-channel float target. GlowOverdrawProbe draws it once after a measured window; the game never uses it.
Shader "Hidden/DigitalStory/F010GlowOverdraw"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #include "UnityCG.cginc"

            // The same layouts as WaspGlow.shader.
            struct WaspStatic
            {
                float seed;
                float lane;
                float ringAngle;
                float ringRadius;
                float3 boneOffset;
                float boneIndex;
                float height01;
                float level01;
                float phase01;
                float squad;
            };

            struct WaspDynamic
            {
                float3 position;
                float state;
                float3 velocity;
                float stateTime;
                float4 tint;
            };

            StructuredBuffer<WaspStatic> _Static;
            StructuredBuffer<WaspDynamic> _Dynamic;

            float _GlowSize;
            float _DyingSeconds;
            float _BuzzAmplitude;
            float _BuzzFrequency;

            static const float FullTurn = 6.28318530718;
            static const int StateDying = 1;
            static const int StateDead = 2;
            static const float3 BuzzRates = float3(1.0, 1.13, 0.87);
            static const float3 BuzzPhases = float3(3.0, 5.0, 7.0);
            static const float PushBack = 0.4;

            float LifeScale(WaspDynamic body)
            {
                int state = (int)(body.state + 0.5);
                if (state == StateDead)
                {
                    return 0.0;
                }

                if (state == StateDying)
                {
                    return 1.0 - saturate(body.stateTime / max(_DyingSeconds, 0.0001));
                }

                return 1.0;
            }

            float4 Vertex(float4 position : POSITION, uint instanceId : SV_InstanceID) : SV_POSITION
            {
                WaspStatic wasp = _Static[instanceId];
                WaspDynamic body = _Dynamic[instanceId];
                float size = _GlowSize * LifeScale(body);
                float3 angle = _Time.y * _BuzzFrequency * FullTurn * BuzzRates + wasp.seed * FullTurn * BuzzPhases;
                float3 center = body.position + _BuzzAmplitude * sin(angle);
                float3 viewCenter = UnityWorldToViewPos(center);
                float3 viewCorner = viewCenter + float3(position.xy * size, -size * PushBack);
                return UnityViewToClipPos(viewCorner);
            }

            float4 Fragment() : SV_Target
            {
                return float4(1.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
    }
}
