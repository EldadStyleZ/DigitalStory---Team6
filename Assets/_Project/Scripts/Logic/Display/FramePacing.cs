namespace DigitalStory.Logic
{
    /// <summary>
    /// How Unity paces frames: <see cref="VSyncCount"/> display refreshes per frame, or, at 0, no vsync and at most
    /// <see cref="TargetFrameRate"/> frames a second (-1 when vsync paces them).
    /// </summary>
    public readonly struct FramePacing
    {
        public FramePacing(int vSyncCount, int targetFrameRate)
        {
            VSyncCount = vSyncCount;
            TargetFrameRate = targetFrameRate;
        }

        /// <summary>Display refreshes per frame, 0 to 4; 0 paces by <see cref="TargetFrameRate"/>.</summary>
        public int VSyncCount { get; }

        /// <summary>The most frames a second while <see cref="VSyncCount"/> is 0; -1 otherwise.</summary>
        public int TargetFrameRate { get; }
    }
}
