using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Draws the blow's area as a white watercolour ring sector, pointing right from its pivot (the blow's origin):
    /// a see-through wash with darker paint blotches and softly feathered edges, no outline. It is drawn from the
    /// blow's own angle and radii, so it always covers exactly the area the blow pushes.
    /// </summary>
    public static class BlowSectorPicture
    {
        /// <summary>The look of the picture; everything is in units, so it does not change with the resolution.</summary>
        public struct Look
        {
            public float PixelsPerUnit;
            public float WashAlpha;
            public float BlotchAlpha;
            public float BlotchSize;
            public float Feather;
            public float WashVariation;
        }

        /// <summary>Makes the picture for a cone of <paramref name="angle"/> degrees between two radii.</summary>
        public static Sprite Create(float angle, float innerRadius, float outerRadius, Look look)
        {
            float half = Mathf.Clamp(angle, 1f, 359f) * 0.5f * Mathf.Deg2Rad;
            float sinHalf = Mathf.Sin(half);
            float cosHalf = Mathf.Cos(half);

            // The bounding box of the ring sector, with the origin at (0, 0).
            float xMin = Mathf.Min(0f, Mathf.Min(innerRadius * cosHalf, outerRadius * cosHalf));
            float yHalf = half >= Mathf.PI * 0.5f ? outerRadius : outerRadius * sinHalf;
            float margin = look.Feather + 0.05f;
            float left = xMin - margin;
            float bottom = -yHalf - margin;
            int width = Mathf.Max(4, Mathf.CeilToInt((outerRadius + margin - left) * look.PixelsPerUnit));
            int height = Mathf.Max(4, Mathf.CeilToInt((2f * (yHalf + margin)) * look.PixelsPerUnit));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "Blow Sector",
            };

            var pixels = new Color32[width * height];
            float blotchScale = 1f / Mathf.Max(0.05f, look.BlotchSize);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float wx = left + (x + 0.5f) / look.PixelsPerUnit;
                    float wy = bottom + (y + 0.5f) / look.PixelsPerUnit;
                    float r = Mathf.Sqrt(wx * wx + wy * wy);
                    float a = Mathf.Abs(Mathf.Atan2(wy, wx));

                    // Distance inside the shape: to the inner arc, the outer arc and the nearer straight side.
                    float toSide = a <= half
                        ? (half - a < Mathf.PI * 0.5f ? r * Mathf.Sin(half - a) : r)
                        : -r * Mathf.Sin(Mathf.Min(a - half, Mathf.PI * 0.5f));
                    float inside = Mathf.Min(Mathf.Min(r - innerRadius, outerRadius - r), toSide);

                    if (inside <= -look.Feather)
                    {
                        continue;
                    }

                    float edge = look.Feather > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((inside + look.Feather) / (2f * look.Feather))) : (inside >= 0f ? 1f : 0f);
                    // 0 variation gives one even colour; more adds a paper-like unevenness.
                    // float paper = 0.85f + 0.15f * Mathf.PerlinNoise(wx * 3.1f + 11f, wy * 3.1f + 7f);
                    float paper = 1f - look.WashVariation * Mathf.PerlinNoise(wx * 3.1f + 11f, wy * 3.1f + 7f);
                    float wash = look.WashAlpha * paper;

                    // Darker paint blotches: big soft patches, with smaller marks inside them.
                    float big = Mathf.PerlinNoise(wx * blotchScale + 3.7f, wy * blotchScale + 1.3f);
                    float small = Mathf.PerlinNoise(wx * blotchScale * 2.7f + 9.1f, wy * blotchScale * 2.7f + 4.4f);
                    float blotch = look.BlotchAlpha <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.75f, big * 0.65f + small * 0.35f)) * look.BlotchAlpha;

                    float alpha = Mathf.Clamp01(Mathf.Max(wash, blotch) * edge);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var pivot = new Vector2(-left * look.PixelsPerUnit / width, 0.5f);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, look.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
    }
}
