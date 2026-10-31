using System;

namespace DigitalStory.View
{
    /// <summary>Wave shapes shared by the live flute and the baked sounds.</summary>
    public static class Waveforms
    {
        /// <summary>Returns a triangle wave from -1 to 1.</summary>
        /// <param name="phase">How far through the wave, from 0 to 1.</param>
        public static double Triangle(double phase) => (4d * Math.Abs(phase - .5d)) - 1d;
    }
}
