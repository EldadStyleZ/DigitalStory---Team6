// A night sky for the hive defense scene: deep blue overhead, a purple band at the horizon, dark below it,
// small fixed stars and a big moon with a soft halo. No lighting. The stars come from a hash of the view direction,
// so they stay put when the camera moves. HiveDefenseSceneBuilder makes the material and sets the moon's direction.
Shader "Skybox/DigitalStory/NightSky"
{
    Properties
    {
        _ZenithColor ("Colour overhead", Color) = (0.02, 0.04, 0.14, 1)
        _HorizonColor ("Colour at the horizon", Color) = (0.2, 0.12, 0.3, 1)
        _GroundColor ("Colour below the horizon", Color) = (0.015, 0.02, 0.05, 1)
        _HorizonWidth ("Horizon band width", Range(0.05, 1)) = 0.45
        _StarColor ("Star colour", Color) = (0.85, 0.9, 1, 1)
        _StarDensity ("Star density (cells across the sky)", Range(20, 200)) = 90
        _StarAmount ("Share of cells with a star", Range(0, 1)) = 0.35
        _StarSize ("Star size", Range(0.02, 0.5)) = 0.16
        _StarBrightness ("Star brightness", Range(0, 4)) = 1.6
        _MoonDirection ("Direction to the moon", Vector) = (-0.3, 0.45, 0.8, 0)
        _MoonColor ("Moon colour", Color) = (0.95, 0.96, 1, 1)
        _MoonSize ("Moon size (angular radius, radians)", Range(0.02, 0.5)) = 0.13
        _HaloColor ("Moon halo colour", Color) = (0.45, 0.55, 0.9, 1)
        _HaloSize ("Moon halo size (radians)", Range(0.05, 1.5)) = 0.55
        _HaloStrength ("Moon halo strength", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            fixed4 _ZenithColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            half _HorizonWidth;
            fixed4 _StarColor;
            half _StarDensity;
            half _StarAmount;
            half _StarSize;
            half _StarBrightness;
            float4 _MoonDirection;
            fixed4 _MoonColor;
            half _MoonSize;
            fixed4 _HaloColor;
            half _HaloSize;
            half _HaloStrength;

            struct Input
            {
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vertex(Input input)
            {
                Varyings output;
                output.clip = UnityObjectToClipPos(input.position);
                output.direction = input.position.xyz;
                return output;
            }

            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            // One possible star per grid cell of the direction. Neighbouring cells are checked so a star near a cell edge is whole.
            float Stars(float3 direction)
            {
                float3 scaled = direction * _StarDensity;
                float3 cell = floor(scaled);
                float total = 0;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 neighbour = cell + float3(x, y, z);
                            float3 random = Hash33(neighbour);
                            if (random.x > _StarAmount)
                            {
                                continue;
                            }

                            float3 centre = neighbour + 0.2 + 0.6 * Hash33(neighbour + 17.0);
                            float size = _StarSize * (0.5 + random.y);
                            float distanceToStar = length(scaled - centre);
                            float glow = saturate(1.0 - distanceToStar / size);
                            total += glow * glow * (0.35 + 0.65 * random.z);
                        }
                    }
                }

                return total * _StarBrightness;
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float height = direction.y;

                // Deep blue overhead, fading to the purple band, then the dark below.
                float up = saturate(height / max(_HorizonWidth, 0.001));
                float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, sqrt(up));
                float down = saturate(-height / max(_HorizonWidth * 0.5, 0.001));
                color = lerp(color, _GroundColor.rgb, down);

                // Stars fade out toward the horizon so they do not sit in the haze.
                float aboveHorizon = saturate(height * 4.0);
                color += _StarColor.rgb * Stars(direction) * aboveHorizon;

                // The moon: a bright disc with a soft edge, and a halo that falls off with the angle.
                float3 moon = normalize(_MoonDirection.xyz);
                float angle = acos(clamp(dot(direction, moon), -1.0, 1.0));
                float disc = 1.0 - smoothstep(_MoonSize * 0.94, _MoonSize, angle);
                float halo = pow(saturate(1.0 - angle / _HaloSize), 2.0) * _HaloStrength;
                color += _HaloColor.rgb * halo;
                color = lerp(color, _MoonColor.rgb, disc);

                return fixed4(color, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
