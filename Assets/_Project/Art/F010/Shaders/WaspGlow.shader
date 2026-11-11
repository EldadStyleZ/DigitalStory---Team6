// Draws a soft glow around every wasp in one Graphics.RenderMeshIndirect call, after the opaque geometry. It stands in
// for bloom, which the project cannot add without a package: each wasp gets a camera-facing quad with a round falloff,
// tinted like the wasp and brighter as it heats up. It reads the same buffers as WaspInstanced.shader.
// The glows blend like light on a screen (one plus the rest times what is left), so where dozens overlap in the river
// they ease toward white instead of clipping, and they never darken what is behind them, even in an HDR buffer. Each
// quad is pushed a little behind its own wasp, so it lights the gaps and edges of the swarm without washing the dark
// stripes of the wasps in front. SwarmGpuView makes the material and sets every property below.
Shader "DigitalStory/WaspGlow"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "DisableBatching" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "Glow"

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcColor
            ColorMask RGB

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            // 48 bytes, the same layout as WaspStatic in WaspSwarm.compute, WaspInstanced.shader and SwarmGpuView.
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
                float squad;        // which slot's squad the wasp joins; only WaspSwarm.compute reads it
            };

            // 48 bytes, the same layout as WaspDynamic in WaspSwarm.compute and WaspInstanced.shader.
            struct WaspDynamic
            {
                float3 position;
                float state;
                float3 velocity;
                float stateTime;
                float4 tint;        // rgb colour, a heat: how hot the wasp glows
            };

            StructuredBuffer<WaspStatic> _Static;
            StructuredBuffer<WaspDynamic> _Dynamic;

            // From SwarmConfig.
            float _GlowSize;
            float _GlowStrength;
            float _DyingSeconds;
            float _BuzzAmplitude;
            float _BuzzFrequency;

            static const float FullTurn = 6.28318530718;
            static const int StateDying = 1;
            static const int StateDead = 2;

            // The same buzz as WaspInstanced.shader, so each glow stays on its wasp.
            static const float3 BuzzRates = float3(1.0, 1.13, 0.87);
            static const float3 BuzzPhases = float3(3.0, 5.0, 7.0);

            // Share of the quad's size it sits behind its wasp, away from the camera.
            static const float PushBack = 0.4;

            // A calm wasp glows GlowBase times _GlowStrength; a wasp at full heat (an impact) glows GlowBase plus GlowHeat
            // times it.
            static const float GlowBase = 0.7;
            static const float GlowHeat = 0.6;

            // An impact (ImpactHeat, 1, in WaspSwarm.compute) whitens the glow for its few frames, as WaspInstanced.shader
            // whitens the wasp, so the flash shows around it too. Lower heats keep the wasp's colour.
            static const float FlashStart = 0.95;
            static const float FlashWhiten = 0.6;

            struct VertexInput
            {
                float4 position : POSITION;     // the quad's corners, -0.5 to 0.5 in x and y (WaspMeshFactory)
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 corner : TEXCOORD0;      // -1 to 1 across the quad
                float3 color : TEXCOORD1;       // the glow's colour at its middle
                UNITY_FOG_COORDS(2)
            };

            // Dead wasps glow at size 0, and dying ones shrink to it over DyingSeconds, as WaspInstanced.shader does.
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

            float3 Buzz(float seed)
            {
                float3 angle = _Time.y * _BuzzFrequency * FullTurn * BuzzRates + seed * FullTurn * BuzzPhases;
                return _BuzzAmplitude * sin(angle);
            }

            FragmentInput Vertex(VertexInput input, uint instanceId : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                uint index = GetIndirectInstanceID(instanceId);
                WaspStatic wasp = _Static[index];
                WaspDynamic body = _Dynamic[index];

                // A dead wasp's quad shrinks to a point, which draws nothing.
                float life = LifeScale(body);
                float size = _GlowSize * life;
                float3 center = body.position + Buzz(wasp.seed);

                // In view space the camera looks down -z, so lowering z pushes the quad away from the camera.
                float3 viewCenter = UnityWorldToViewPos(center);
                float3 viewCorner = viewCenter + float3(input.position.xy * size, -size * PushBack);

                float heat = saturate(body.tint.a);
                float strength = _GlowStrength * life * (GlowBase + GlowHeat * heat);
                float flash = saturate((heat - FlashStart) / (1.0 - FlashStart));

                FragmentInput output;
                output.position = UnityViewToClipPos(viewCorner);
                output.corner = input.position.xy * 2.0;
                output.color = lerp(body.tint.rgb, float3(1.0, 1.0, 1.0), flash * FlashWhiten) * strength;
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            float4 Fragment(FragmentInput input) : SV_Target
            {
                // 1 in the middle, easing to 0 at the circle that touches the quad's edges, and 0 in its corners.
                float reach = dot(input.corner, input.corner);
                float falloff = saturate(1.0 - reach);
                float glow = falloff * falloff;

                float4 result = float4(saturate(input.color * glow), 1.0);

                // Fog fades the glow to nothing rather than to the fog's colour, because the glow only adds light.
                UNITY_APPLY_FOG_COLOR(input.fogCoord, result, float4(0.0, 0.0, 0.0, 0.0));
                return result;
            }
            ENDCG
        }
    }
}
