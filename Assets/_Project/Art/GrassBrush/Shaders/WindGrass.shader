// Low poly grass blades for the Grass Brush. GrassField keeps every clump on the GPU, in one buffer per square cell
// (_GrassClumps: the clump's root, size and turn), and draws a cell's clumps as instances of one clump mesh; this
// shader places each instance from the buffer (plan section AD: nothing is uploaded per frame), and bends each blade
// with the wind: slow waves travel across the field in the wind's direction, and a quick flutter shakes each clump on
// its own. Far away the field may thin out: a cell's clumps are shuffled, so the first share of them is an even
// sample, and clumps past the share kept at their distance shrink away (_GrassThin*; off when the far share is 1).
// The blade's height (uv.y, 0 at the root and 1 at the tip) decides how far it bends, so roots stay put.
// Each face is lit flat, so the blades read as facets. GrassField sets the _GrassWind* globals every frame.
// The second pass adds every pixel point light, such as the swarm's glow and attack lights, so the grass
// around the swarm lights up as it moves. Blades let light through, so they glow from both sides.
Shader "DigitalStory/WindGrass"
{
    Properties
    {
        _BaseColor ("Root Color", Color) = (0.08, 0.16, 0.06, 1)
        _TipColor ("Tip Color", Color) = (0.35, 0.55, 0.18, 1)
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.3
        _Brightness ("Brightness", Range(0, 3)) = 1
        _Facets ("Facet Shading", Range(0, 1)) = 0.6
        _ColorSteps ("Color Steps (0 = smooth)", Range(0, 6)) = 3
        _GlowResponse ("Swarm Light Response", Range(0, 4)) = 1.5
        _GlowWrap ("Light Through Blades", Range(0, 1)) = 0.5
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"

    float4 _BaseColor;
    float4 _TipColor;
    float _ColorVariation;
    float _Brightness;
    float _Facets;
    float _ColorSteps;
    float _GlowResponse;
    float _GlowWrap;

    // One clump: its root on the ground, its size in metres and its turn about the up axis in radians. GrassField's
    // GpuClump has the same layout.
    struct GrassClump
    {
        float3 position;
        float size;
        float turn;
    };

    StructuredBuffer<GrassClump> _GrassClumps;
    float _GrassCount;          // this cell's clumps, for each clump's place in the shuffled order
    float _GrassThinStart;      // metres from the camera where the field starts to thin
    float _GrassThinEnd;        // metres where it has thinned to _GrassThinKeep
    float _GrassThinKeep;       // the share of clumps kept from _GrassThinEnd on (1 keeps all)
    float _GrassThinFade;       // the share of clumps over which a clump past the kept share shrinks away

    float4 _GrassWindDir;
    float _GrassWindStrength;
    float _GrassWindSpeed;
    float _GrassWindWaveScale;
    float _GrassFlutter;

    struct Attributes
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    // The share of a clump that shows at its distance: 1 near, 0 for a clump past the share kept there, and in between
    // for the few at the edge of that share, so the field thins smoothly as the camera moves.
    float ThinGrowth(float3 root, uint instance)
    {
        float away = length(root - _WorldSpaceCameraPos);
        float far = saturate((away - _GrassThinStart) / max(_GrassThinEnd - _GrassThinStart, 0.001));
        float keep = lerp(1.0, _GrassThinKeep, far);
        float rank = (instance + 0.5) / max(_GrassCount, 1.0);
        float fade = max(_GrassThinFade, 0.001);
        return saturate((keep + fade - rank) / fade);
    }

    // Bends the blade with the wind and returns its world position. Also gives the blade's shade.
    float3 WindBlade(Attributes input, uint instance, out float tint)
    {
        GrassClump clump = _GrassClumps[instance];
        float3 root = clump.position;
        float size = clump.size * ThinGrowth(root, instance);
        float turnSin;
        float turnCos;
        sincos(clump.turn, turnSin, turnCos);
        // Unity's turn about y (Quaternion.Euler(0, turn, 0)), then the clump's size, then its root.
        float3 local = input.vertex.xyz * size;
        float3 world = root + float3((local.x * turnCos) + (local.z * turnSin), local.y, (local.z * turnCos) - (local.x * turnSin));
        float h = input.uv.y;
        float random = Hash(root.xz);

        float2 dir = _GrassWindDir.xz;
        dir = dot(dir, dir) > 0.0001 ? normalize(dir) : float2(1, 0);
        float2 side = float2(-dir.y, dir.x);

        // A wave that travels along the wind, so gusts sweep across the field.
        float wave = sin(dot(root.xz, dir) * _GrassWindWaveScale - _Time.y * _GrassWindSpeed) * 0.5 + 0.5;
        wave *= wave;
        // Each clump also flutters at its own pace.
        float flutter = sin(_Time.y * (_GrassWindSpeed * 2.3 + 1.5) + random * 6.2832);

        float bend = _GrassWindStrength * (0.25 + wave) * h * h * size;
        float shake = _GrassFlutter * flutter * h * h * size * 0.35;

        world.xz += dir * (bend + shake * 0.5) + side * shake * 0.5;
        world.y -= (abs(bend) + abs(shake) * 0.5) * 0.35 * h;

        // Each blade gets its own shade (uv.x) mixed with the clump's, for a patchy low poly field.
        float shade = frac(input.uv.x + random);
        tint = (1 + (shade - 0.5) * 2 * _ColorVariation) * _Brightness;
        return world;
    }

    // The face's own normal, from the screen-space slope of its position, so every triangle is lit flat.
    float3 FlatNormal(float3 world)
    {
        float3 normal = normalize(cross(ddy(world), ddx(world)));
        float3 toCamera = normalize(_WorldSpaceCameraPos - world);
        normal = dot(normal, toCamera) < 0 ? -normal : normal;
        // Blend toward the sky normal so the facets show without the back of a blade going black.
        return normalize(lerp(float3(0, 1, 0), normal, _Facets));
    }

    float3 BladeColor(float height, float tint)
    {
        float h = _ColorSteps >= 1 ? floor(height * _ColorSteps + 0.5) / _ColorSteps : height;
        return lerp(_BaseColor.rgb, _TipColor.rgb, h) * tint;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        // Moonlight and the sky.
        Pass
        {
            Name "Grass"
            Tags { "LightMode" = "ForwardBase" }
            Cull Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
                float tint : TEXCOORD1;
                float3 world : TEXCOORD3;
                UNITY_FOG_COORDS(2)
            };

            Varyings Vertex(Attributes input, uint instance : SV_InstanceID)
            {
                Varyings output;
                float tint;
                float3 world = WindBlade(input, instance, tint);
                output.pos = UnityWorldToClipPos(world);
                output.height = input.uv.y;
                output.tint = tint;
                output.world = world;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = FlatNormal(input.world);
                float3 light = ShadeSH9(float4(normal, 1)) + _LightColor0.rgb * saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                float4 result = float4(BladeColor(input.height, input.tint) * light, 1);
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }

        // Each pixel light nearby, such as the swarm's glow, added on top.
        Pass
        {
            Name "GrassLights"
            Tags { "LightMode" = "ForwardAdd" }
            Cull Off
            Blend One One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_fog
            #pragma multi_compile_fwdadd
            #include "AutoLight.cginc"

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
                float tint : TEXCOORD1;
                float3 world : TEXCOORD3;
                UNITY_FOG_COORDS(2)
            };

            Varyings Vertex(Attributes input, uint instance : SV_InstanceID)
            {
                Varyings output;
                float tint;
                float3 world = WindBlade(input, instance, tint);
                output.pos = UnityWorldToClipPos(world);
                output.height = input.uv.y;
                output.tint = tint;
                output.world = world;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = FlatNormal(input.world);
                float3 toLight = normalize(UnityWorldSpaceLightDir(input.world));
                UNITY_LIGHT_ATTENUATION(attenuation, input, input.world);

                // Wrapped light: a blade facing away still glows a little, as thin leaves do.
                float facing = saturate((dot(normal, toLight) + _GlowWrap) / (1 + _GlowWrap));
                // Tips catch more light than roots, which sit in the shade of the clump.
                float reach = lerp(0.45, 1, input.height);

                float3 color = BladeColor(input.height, input.tint) * _LightColor0.rgb * facing * attenuation * reach * _GlowResponse;
                float4 result = float4(color, 1);
                UNITY_APPLY_FOG_COLOR(input.fogCoord, result, float4(0, 0, 0, 0));
                return result;
            }
            ENDCG
        }
    }
}
