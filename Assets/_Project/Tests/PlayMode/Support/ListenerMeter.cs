using System.Collections;
using UnityEngine;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>Measures what reaches the audio listener.</summary>
    internal static class ListenerMeter
    {
        private const int SampleCount = 1024;
        private const float SecondsToConnect = 0.05f;

        private static readonly float[] Samples = new float[SampleCount];

        /// <summary>
        /// Call once after the audio system starts or resets, and before measuring. Unity's first
        /// read comes back blank even with sound playing, so without this a loud game reads as
        /// silent and a silence test proves nothing.
        /// </summary>
        public static IEnumerator Connect()
        {
            AudioListener.GetOutputData(Samples, 0);
            yield return new WaitForSecondsRealtime(SecondsToConnect);
        }

        /// <summary>The average level of the latest sound. Call Connect first.</summary>
        public static float Loudness()
        {
            AudioListener.GetOutputData(Samples, 0);
            float total = 0f;

            foreach (float sample in Samples)
            {
                total += sample * sample;
            }

            return Mathf.Sqrt(total / SampleCount);
        }

        /// <summary>The highest level of the latest sound. Call Connect first.</summary>
        public static float Peak()
        {
            AudioListener.GetOutputData(Samples, 0);
            float peak = 0f;

            foreach (float sample in Samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            return peak;
        }
    }
}
