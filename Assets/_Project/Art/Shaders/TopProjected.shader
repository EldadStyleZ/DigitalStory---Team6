// Paints a mesh with one top-down image, projected straight down in the mesh's own space.
// Used for the sidewalk generator's "top view" PNG: every stone, dune and root gets exactly
// the colour it has in the generator, with no UVs needed.
Shader "DigitalStory/Top Projected"
{
    Properties
    {
        _MainTex ("Top-view image", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.08
        // x, z of the image centre and its size in metres, in the mesh's own space
        _Rect ("Centre X, Centre Z, Size X, Size Z", Vector) = (0,0,40,40)
        // -1 flips an axis (Unity's OBJ importer mirrors X, and image rows run along -Z)
        _Flip ("Flip X, Flip Z", Vector) = (-1,-1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        float4 _Rect;
        float4 _Flip;

        struct Input { float2 topUV; };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 p = v.vertex.xyz;
            o.topUV = float2(0.5 + _Flip.x * (p.x - _Rect.x) / _Rect.z,
                             0.5 + _Flip.y * (p.z - _Rect.y) / _Rect.w);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, saturate(IN.topUV)) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
