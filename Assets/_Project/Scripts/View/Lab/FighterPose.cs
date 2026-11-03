using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Turns what a fighter has just done into how it is drawn: its walk frame, lunge, flash and breath.</summary>
    public static class FighterPose
    {
        /// <summary>
        /// Which walk picture shows after walking this far. Following the distance rather than the clock keeps
        /// the legs in step with the ground, at any speed.
        /// </summary>
        public static int WalkFrame(float distanceWalked, float strideTiles, int frameCount)
        {
            if (frameCount <= 0 || strideTiles <= 0f)
            {
                return 0;
            }

            float intoStride = Mathf.Repeat(distanceWalked, strideTiles);
            return Mathf.Min((int)(intoStride / strideTiles * frameCount), frameCount - 1);
        }

        /// <summary>How far out a lunge is, from 0 at the strike up to 1 halfway and back to 0.</summary>
        public static float Lunge(float secondsSinceStrike, float lungeSeconds)
        {
            return secondsSinceStrike < lungeSeconds ? Mathf.Sin(Mathf.PI * secondsSinceStrike / lungeSeconds) : 0f;
        }

        /// <summary>How strong a hurt flash is, from 1 at the hit down to 0.</summary>
        public static float Flash(float secondsSinceHurt, float hurtSeconds)
        {
            return secondsSinceHurt < hurtSeconds ? 1f - (secondsSinceHurt / hurtSeconds) : 0f;
        }

        /// <summary>Where a breath is, from -1 to 1. The phase, from 0 to 1, keeps a crowd from breathing as one.</summary>
        public static float Breath(float seconds, float phase, float breathSeconds)
        {
            return Mathf.Sin(2f * Mathf.PI * ((seconds / breathSeconds) + phase));
        }
    }
}
