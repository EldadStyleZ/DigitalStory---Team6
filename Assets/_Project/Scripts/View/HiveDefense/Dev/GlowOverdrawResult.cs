namespace DigitalStory.View
{
    /// <summary>Development tool, not game code. How many glow quads covered the picture's pixels in one frame.</summary>
    internal readonly struct GlowOverdrawResult
    {
        public GlowOverdrawResult(int width, int height, long covered, double fragments, int most, int median, int p95, int p99)
        {
            Width = width;
            Height = height;
            Covered = covered;
            Fragments = fragments;
            Most = most;
            Median = median;
            P95 = p95;
            P99 = p99;
        }

        /// <summary>The picture's width in pixels.</summary>
        public int Width { get; }

        /// <summary>The picture's height in pixels.</summary>
        public int Height { get; }

        /// <summary>Pixels covered by at least one quad.</summary>
        public long Covered { get; }

        /// <summary>Glow fragments in the frame: the sum of the count over every pixel.</summary>
        public double Fragments { get; }

        /// <summary>The most quads on one pixel.</summary>
        public int Most { get; }

        /// <summary>The median count over the covered pixels.</summary>
        public int Median { get; }

        /// <summary>The 95th percentile count over the covered pixels.</summary>
        public int P95 { get; }

        /// <summary>The 99th percentile count over the covered pixels.</summary>
        public int P99 { get; }
    }
}
