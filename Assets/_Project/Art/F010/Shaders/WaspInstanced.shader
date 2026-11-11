// Draws every wasp in one Graphics.RenderMeshIndirect call. WaspSwarm.compute has already moved them;
// this shader reads both wasp buffers by instance id, points each wasp along its velocity and flaps its wings.
// Spec: docs/specs/F-010-wasp-swarm.md, section 9.6. SwarmGpuView makes the material and sets every property below.
Shader "DigitalStory/WaspInstanced"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" "DisableBatching" = "True" }

        // There is no ShadowCaster pass and no Fallback, so the wasps never cast shadows.
        Pass
        {
            Name "Wasp"
            Tags { "LightMode" = "ForwardBase" }

            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            // 48 bytes, the same layout as WaspStatic in WaspSwarm.compute and SwarmGpuView.
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
                float unused;
            };

            // 48 bytes, the same layout as WaspDynamic in WaspSwarm.compute.
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

            // From SwarmConfig.
            float _WaspLength;
            float _DyingSeconds;
            float _BuzzAmplitude;
            float _BuzzFrequency;

            static const float FullTurn = 6.28318530718;
            static const int StateDying = 1;
            static const int StateDead = 2;

            // The wing beat of spec 9.6: sin(time x 40 + seed x 6.28).
            static const float FlapRate = 40.0;
            static const float FlapAngle = 0.9;

            // Below this speed a wasp faces its resting heading instead, so it never spins when it hovers.
            static const float StillSpeed = 0.05;

            // Cheap sine noise: three detuned sines with seed-spread phases.
            static const float3 BuzzRates = float3(1.0, 1.13, 0.87);
            static const float3 BuzzPhases = float3(3.0, 5.0, 7.0);

            // Stripes along the body. The mesh is one unit long, from z -0.5 (sting) to 0.5 (head).
            static const float StripeCount = 7.0;
            static const float DarkStripeShare = 0.4;   // less than half, so a far swarm still reads yellow, not brown
            static const float HeadStart = 0.3;
            static const float3 StripeDark = float3(0.05, 0.04, 0.03);

            // Grey-blue and half tinted, so the wings read as see-through and carry the form's colour, rather than
            // turning the swarm white from above.
            static const float3 WingColor = float3(0.62, 0.66, 0.72);
            static const float WingTint = 0.45;

            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;      // y is 1 on wing vertices (WaspMeshFactory)
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 body : TEXCOORD1;    // x local z, y 1 on the wings
                float4 tint : TEXCOORD2;    // rgb colour, a glow
            };

            // Turns a wing about the body's long axis. Wing vertices lie flat at the hinge height,
            // so the turn needs no offset. Body vertices have wing 0 and do not move.
            void Flap(float wing, float seed, inout float3 position, inout float3 normal)
            {
                float flap = FlapAngle * sin(_Time.y * FlapRate + seed * FullTurn) * wing;
                float side = position.x >= 0.0 ? 1.0 : -1.0;
                float turnSin;
                float turnCos;
                sincos(flap * side, turnSin, turnCos);
                position = float3(position.x * turnCos, position.y + position.x * turnSin, position.z);
                normal = float3(normal.x * turnCos - normal.y * turnSin, normal.x * turnSin + normal.y * turnCos, normal.z);
            }

            void Facing(float3 velocity, float seed, out float3 forward, out float3 right, out float3 up)
            {
                float heading = seed * FullTurn;
                float3 resting = float3(sin(heading), 0.0, cos(heading));
                float3 along = velocity + resting * StillSpeed;
                float alongLength = length(along);
                forward = alongLength > 0.00001 ? along / alongLength : resting;

                float3 side = cross(float3(0.0, 1.0, 0.0), forward);
                float sideLength = length(side);
                right = sideLength > 0.0001 ? side / sideLength : float3(1.0, 0.0, 0.0);
                up = cross(forward, right);
            }

            // Dead wasps draw at scale 0, and dying ones shrink to it over DyingSeconds.
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

            // The buzz of spec 9.4, added where the wasp is drawn rather than as a force: a 9 Hz force stepped at 30 fps
            // aliases, and its speed would swing every wasp's heading. At 9 Hz the spring barely answers anyway.
            float3 Buzz(float seed)
            {
                float3 angle = _Time.y * _BuzzFrequency * FullTurn * BuzzRates + seed * FullTurn * BuzzPhases;
                return _BuzzAmplitude * sin(angle);
            }

            float3 Albedo(float localZ, float wing, float3 tint)
            {
                float band = step(DarkStripeShare, frac((localZ + 0.5) * StripeCount));
                float3 bodyColor = localZ > HeadStart ? StripeDark : lerp(StripeDark, tint, band);
                float3 wingColor = lerp(WingColor, tint, WingTint);
                return lerp(bodyColor, wingColor, wing);
            }

            FragmentInput Vertex(VertexInput input, uint instanceId : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                uint index = GetIndirectInstanceID(instanceId);
                WaspStatic wasp = _Static[index];
                WaspDynamic body = _Dynamic[index];

                float3 localPosition = input.position.xyz;
                float3 localNormal = input.normal;
                Flap(input.uv.y, wasp.seed, localPosition, localNormal);

                float3 forward;
                float3 right;
                float3 up;
                Facing(body.velocity, wasp.seed, forward, right, up);

                float scale = _WaspLength * LifeScale(body);
                float3 shape = right * localPosition.x + up * localPosition.y + forward * localPosition.z;
                float3 world = body.position + Buzz(wasp.seed) + shape * scale;

                FragmentInput output;
                output.position = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                output.normal = right * localNormal.x + up * localNormal.y + forward * localNormal.z;
                output.body = float2(localPosition.z, input.uv.y);
                output.tint = body.tint;
                return output;
            }

            float4 Fragment(FragmentInput input) : SV_Target
            {
                float3 normal = normalize(input.normal);
                float3 albedo = Albedo(input.body.x, input.body.y, input.tint.rgb);

                // _WorldSpaceLightPos0 is the main directional light's direction, or zero when there is none.
                float3 toLight = _WorldSpaceLightPos0.xyz;
                float lightLength = length(toLight);
                float lambert = lightLength > 0.00001 ? saturate(dot(normal, toLight / lightLength)) : 0.0;
                float3 ambient = ShadeSH9(float4(normal, 1.0));

                // The light is capped at white, so a lit yellow stays yellow instead of clipping to pale lemon. The form's
                // glow blends toward its flat colour rather than adding to it, because an added orange clips to yellow.
                float3 light = min(_LightColor0.rgb * lambert + ambient, 1.0);
                float3 color = lerp(albedo * light, input.tint.rgb, saturate(input.tint.a));
                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}
