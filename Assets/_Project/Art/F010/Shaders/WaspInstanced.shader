// Draws every wasp in one Graphics.RenderMeshIndirect call. WaspSwarm.compute has already moved them;
// this shader reads both wasp buffers by instance id, points each wasp along its velocity and flaps its wings,
// and stretches a striking wasp (a sting's comet, a diving meteor) into a streak.
// The wasps glow at night, like the artist's pictures: the light bands are mostly emissive, in the colour the compute
// shader blends for each wasp, and whiter where a facet faces the camera, so each wasp has a bright core. A little
// moonlight shows the facets. The dark stripes and the head stay dark. WaspGlow.shader draws the soft glow around each.
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            // 48 bytes, the same layout as WaspStatic in WaspSwarm.compute, WaspGlow.shader and SwarmGpuView.
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

            // 48 bytes, the same layout as WaspDynamic in WaspSwarm.compute and WaspGlow.shader.
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

            // Only a strike (a sting's comet, a climbing or diving meteor) flies faster than the swarm's speed cap
            // (MaxSpeed, 25 m/s, in WaspSwarm.compute). Such a wasp draws stretched back along its flight, so it reads as
            // a streak.
            static const float StreakSpeed = 26.0;
            static const float StreakPerSpeed = 0.15;   // extra body lengths per metre per second above StreakSpeed
            static const float MostStreak = 5.0;        // extra body lengths at most
            static const float HeadZ = 0.5;             // the head end of the mesh, which stays put as the body stretches

            // Cheap sine noise: three detuned sines with seed-spread phases.
            static const float3 BuzzRates = float3(1.0, 1.13, 0.87);
            static const float3 BuzzPhases = float3(3.0, 5.0, 7.0);

            // Stripes along the body, from local z: the mesh runs from z -0.5 (the sting) to 0.5 (the head). They are
            // few and wide, so each band is two or three pixels across from the game camera, 12 to 14 m away. From the
            // sting: a dark tip, then light and dark bands, the last dark one at the waist, the light thorax, and the
            // dark head, which starts where the next dark band would.
            static const float TailZ = -0.5;
            static const float StripePeriod = 0.27;
            static const float DarkStripeShare = 0.38;  // well under half, so a far swarm still reads by its glowing colour
            static const float HeadStart = 0.25;
            static const float StripeSoftness = 0.75;   // pixels each stripe edge blends over, so the bands do not shimmer
            static const float3 StripeDark = float3(0.07, 0.05, 0.035);
            static const float3 HeadDark = float3(0.035, 0.03, 0.03);
            static const float DarkLit = 0.5;           // the dark parts take moonlight times this plus their own colour
            static const float DarkWarmth = 0.06;       // share of the wasp's colour a dark band keeps, so it reads warm

            // The light bands glow. A facet facing the camera glows fully and turns partly white, the core; one seen
            // edge-on glows EdgeGlow as much in the plain colour. The heat (the alpha of the colour) brightens them.
            static const float BandGlow = 1.25;           // over 1, so the light bands burn bright from the game camera
            static const float HeatGlow = 0.25;
            static const float EdgeGlow = 0.55;
            static const float CoreWhiten = 0.45;

            // An impact (ImpactHeat, 1, in WaspSwarm.compute) whitens the light bands for its few frames, so a hit reads
            // on the frame it lands. A sting's tick (0.9) and every lower heat stay in their colour.
            static const float FlashStart = 0.95;
            static const float FlashWhiten = 0.7;
            static const float ShapeLight = 0.2;        // share of the moonlight and ambient light added, so the facets show

            // The eyes are dark on a calm wasp and burn in its colour as it heats up, like the red wasps in the pictures.
            static const float EyeGlow = 0.15;
            static const float EyeHeatGlow = 1.2;

            // Pale and only partly tinted, so the wings read as thin and lit by the body rather than as more body.
            static const float3 WingColor = float3(0.85, 0.88, 0.95);
            static const float WingTint = 0.35;
            static const float WingGlow = 0.45;
            static const float WingLit = 0.35;

            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;      // x is 1 on eye vertices and y is 1 on wing vertices (WaspMeshFactory)
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 body : TEXCOORD1;    // x local z, y 1 on the wings, z 1 on the eyes
                float4 tint : TEXCOORD2;    // rgb colour, a heat
                float3 toCamera : TEXCOORD3;
                UNITY_FOG_COORDS(4)
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
            // WaspGlow.shader adds the same buzz, so each glow stays on its wasp.
            float3 Buzz(float seed)
            {
                float3 angle = _Time.y * _BuzzFrequency * FullTurn * BuzzRates + seed * FullTurn * BuzzPhases;
                return _BuzzAmplitude * sin(angle);
            }

            // 1 up to StreakSpeed, then longer the faster the wasp flies.
            float StreakOf(float3 velocity)
            {
                float extra = max(length(velocity) - StreakSpeed, 0.0) * StreakPerSpeed;
                return 1.0 + min(extra, MostStreak);
            }

            // The moonlight and the ambient light on a facet, capped at white.
            float3 LightOn(float3 normal)
            {
                // _WorldSpaceLightPos0 is the main directional light's direction, or zero when there is none.
                float3 toLight = _WorldSpaceLightPos0.xyz;
                float lightLength = length(toLight);
                float lambert = lightLength > 0.00001 ? saturate(dot(normal, toLight / lightLength)) : 0.0;
                float3 ambient = ShadeSH9(float4(normal, 1.0));
                return min(_LightColor0.rgb * lambert + ambient, 1.0);
            }

            // 1 on a light band and 0 on a dark one, blended over StripeSoftness pixels at each edge.
            float LightBandOf(float localZ)
            {
                float phase = (localZ - TailZ) / StripePeriod;
                float fromDarkMiddle = abs(frac(phase + 0.5) - 0.5);    // 0 in a dark band's middle, 0.5 between two
                float edge = DarkStripeShare * 0.5;
                float blur = max(fwidth(phase) * StripeSoftness, 0.0001);
                return smoothstep(edge - blur, edge + blur, fromDarkMiddle);
            }

            // 0 behind the head and 1 on it, blended the same way.
            float HeadShareOf(float localZ)
            {
                float blur = max(fwidth(localZ) * StripeSoftness, 0.0001);
                return smoothstep(HeadStart - blur, HeadStart + blur, localZ);
            }

            float3 GlowingBand(float3 tint, float heat, float facing, float3 light)
            {
                float level = (BandGlow + HeatGlow * heat) * lerp(EdgeGlow, 1.0, facing);
                float flash = saturate((heat - FlashStart) / (1.0 - FlashStart));
                float core = saturate(facing * facing * CoreWhiten + flash * FlashWhiten);
                float3 glowing = lerp(tint * level, float3(1.0, 1.0, 1.0), core);
                return glowing + tint * light * ShapeLight;
            }

            float3 BodyColor(float localZ, float3 tint, float heat, float facing, float3 light)
            {
                float3 band = GlowingBand(tint, heat, facing, light);
                float3 dark = StripeDark * (DarkLit + light) + tint * DarkWarmth;
                float3 head = HeadDark * (DarkLit + light) + tint * (DarkWarmth * 0.5);
                float3 striped = lerp(dark, band, LightBandOf(localZ));
                return lerp(striped, head, HeadShareOf(localZ));
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
                float alongZ = HeadZ + (localPosition.z - HeadZ) * StreakOf(body.velocity);
                float3 shape = right * localPosition.x + up * localPosition.y + forward * alongZ;
                float3 world = body.position + Buzz(wasp.seed) + shape * scale;

                FragmentInput output;
                output.position = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                output.normal = right * localNormal.x + up * localNormal.y + forward * localNormal.z;
                output.body = float3(input.position.z, input.uv.y, input.uv.x);
                output.tint = body.tint;
                output.toCamera = _WorldSpaceCameraPos.xyz - world;
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            float4 Fragment(FragmentInput input) : SV_Target
            {
                float3 normal = normalize(input.normal);
                float3 toCamera = normalize(input.toCamera);
                float3 tint = input.tint.rgb;
                float heat = saturate(input.tint.a);
                float3 light = LightOn(normal);
                float facing = saturate(dot(normal, toCamera));

                // Each part's colour is worked out on every pixel and the marks pick one, so the stripe and head blends
                // (which take screen derivatives) always run on every pixel.
                float3 body = BodyColor(input.body.x, tint, heat, facing, light);
                float3 eye = tint * (EyeGlow + EyeHeatGlow * heat);
                float3 wing = lerp(WingColor, tint, WingTint) * (WingGlow + WingLit * light);

                float3 color = lerp(body, eye, saturate(input.body.z));
                color = lerp(color, wing, saturate(input.body.y));

                float4 result = float4(saturate(color), 1.0);
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
}
