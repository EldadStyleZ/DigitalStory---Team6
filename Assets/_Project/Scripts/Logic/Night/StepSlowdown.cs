using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Applies a slowdown the same way at any frame rate.</summary>
    public static class StepSlowdown
    {
        // The prototype slowed things once per frame at 60 frames a second, so its numbers mean this step.
        private const float TunedStepsPerSecond = 60f;

        /// <summary>Returns the share of speed left after this step.</summary>
        /// <param name="sharePerTunedStep">The share of speed kept each sixtieth of a second.</param>
        /// <param name="deltaTime">The length of this step.</param>
        public static float ShareLeft(float sharePerTunedStep, float deltaTime)
        {
            return Mathf.Pow(sharePerTunedStep, deltaTime * TunedStepsPerSecond);
        }
    }
}
