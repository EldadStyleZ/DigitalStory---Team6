// Reeds and other tall plants that sway with the grass: the same wind as WindGrass (the _GrassWind* globals GrassField
// sets every frame), so gusts sweep through the reeds and the grass together. Height comes from the model's own up axis:
// the root (y 0) stays put and the bend grows toward _Height. Lit flat like the grass, and glows in the swarm's lights.
Shader "DigitalStory/WindReeds"
{
    Properties
    {
        _Color ("Color", Color) = (0.4, 0.48, 0.22, 1)
        _Height ("Plant Height (m)", Range(0.2, 4)) = 1.6
        _Sway ("Sway", Range(0, 3)) = 1.2
        _GlowResponse ("Swarm Light Response", Range(0, 4)) = 1.2
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"

    float4 _Color;
    float _Height;
    float _Sway;
    float _GlowResponse;

    float4 _GrassWindDir;
    float _GrassWindStrength;
    float _GrassWindSpeed;
    float _GrassWindWaveScale;
    float _GrassFlutter;

    struct Attributes
    {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    float3 Sway(Attributes input)
    {
        float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
        float3 root = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
        float h = saturate(input.vertex.y / _Height);
        float random = Hash(root.xz + input.vertex.xz * 3.1);

        float2 dir = _GrassWindDir.xz;
        dir = dot(dir, dir) > 0.0001 ? normalize(dir) : float2(1, 0);
        float2 side = float2(-dir.y, dir.x);

        // The grass's own travelling wave, read at this vertex, so the gusts match the field's.
        float wave = sin(dot(world.xz, dir) * _GrassWindWaveScale - _Time.y * _GrassWindSpeed) * 0.5 + 0.5;
        wave *= wave;
        float flutter = sin(_Time.y * (_GrassWindSpeed * 2.3 + 1.5) + random * 6.2832);

        float bend = _GrassWindStrength * (0.25 + wave) * h * h * _Sway;
        float shake = _GrassFlutter * flutter * h * h * _Sway * 0.35;
        world.xz += dir * (bend + shake * 0.5) + side * shake * 0.5;
        world.y -= (abs(bend) + abs(shake) * 0.5) * 0.3 * h;
        return world;
    }

    float3 FlatNormal(float3 world)
    {
        float3 normal = normalize(cross(ddy(world), ddx(world)));
        float3 toCamera = normalize(_WorldSpaceCameraPos - world);
        return dot(normal, toCamera) < 0 ? -normal : normal;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float height : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            Varyings Vertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 world = Sway(input);
                output.pos = UnityWorldToClipPos(world);
                output.world = world;
                output.height = saturate(input.vertex.y / _Height);
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = normalize(lerp(float3(0, 1, 0), FlatNormal(input.world), 0.6));
                float3 light = ShadeSH9(float4(normal, 1)) + _LightColor0.rgb * saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                float4 result = float4(_Color.rgb * lerp(0.55, 1.0, input.height) * light, 1);
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Cull Off
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fwdadd
            #include "AutoLight.cginc"

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            Varyings Vertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 world = Sway(input);
                output.pos = UnityWorldToClipPos(world);
                output.world = world;
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = FlatNormal(input.world);
                float3 toLight = normalize(UnityWorldSpaceLightDir(input.world));
                UNITY_LIGHT_ATTENUATION(attenuation, input, input.world);
                float facing = saturate((dot(normal, toLight) + 0.5) / 1.5);
                float4 result = float4(_Color.rgb * _LightColor0.rgb * facing * attenuation * _GlowResponse, 1);
                UNITY_APPLY_FOG_COLOR(input.fogCoord, result, float4(0, 0, 0, 0));
                return result;
            }
            ENDCG
        }
    }
}
