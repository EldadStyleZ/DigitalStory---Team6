using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// How a volume slider's percentage becomes a gain (plan section AC): the square of its share, gain = (percent / 100)^2.
    /// 0% is silent and 100% is 1; 50% is a quarter (-12 dB, which sounds about half as loud) and 10% is -40 dB, so the
    /// slider's travel follows how loud it sounds rather than the raw amplitude. Out-of-range percentages are held to 0 to 100.
    /// </summary>
    public static class VolumeCurve
    {
        /// <summary>The highest percentage.</summary>
        public const int Full = 100;

        /// <summary>The percentage held to 0 to 100.</summary>
        public static int Clamp(int percent) => Math.Max(0, Math.Min(Full, percent));

        /// <summary>The gain of a percentage: (percent / 100) squared, 0 to 1.</summary>
        public static float GainOf(int percent)
        {
            float share = Clamp(percent) / (float)Full;
            return share * share;
        }
    }
}
