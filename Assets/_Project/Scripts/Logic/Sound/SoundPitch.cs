using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The pitch a recorded sound plays at (plan section AC): semitones as a playback rate, so a kill climbs the kill chain's
    /// ladder (<see cref="KillChainStep.Semitones"/>, one semitone per chained kill up to the top), and a small random variation
    /// either way so repeats never sound identical.
    /// </summary>
    public static class SoundPitch
    {
        /// <summary>The playback rate that raises a sound by <paramref name="semitones"/>: 2 for an octave, 1 for none.</summary>
        public static float OfSemitones(float semitones) => (float)Math.Pow(2d, semitones / 12d);

        /// <summary>
        /// The rate varied by up to <paramref name="jitter"/> either way, as a share: <paramref name="random01"/> 0 gives the
        /// lowest, 0.5 the rate itself and just under 1 the highest. A jitter outside 0 to 0.5 is held to it.
        /// </summary>
        public static float Varied(float rate, float jitter, double random01)
        {
            float share = Math.Max(0f, Math.Min(0.5f, jitter));
            return rate * (1f + (share * (float)((random01 * 2d) - 1d)));
        }
    }
}
