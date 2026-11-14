using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Plan section AD: turns a render-scale choice into the share of the display's width and height the 3D view is drawn
    /// at. Auto draws at most a set number of lines, so a very large display costs the GPU no more than a laptop's, and never
    /// draws a smaller display at more than its own lines.
    /// </summary>
    public static class RenderScaleRule
    {
        /// <summary>The least share any choice draws at.</summary>
        public const float LeastScale = 0.25f;

        private const float Full = 1f;
        private const float ThreeQuarters = 0.75f;
        private const float Half = 0.5f;

        /// <summary>The share of the display's width and height to draw at, <see cref="LeastScale"/> to 1.</summary>
        /// <param name="choice">The player's choice.</param>
        /// <param name="displayLines">The display's height in pixels; 0 or less when unknown, which draws Auto natively.</param>
        /// <param name="autoMostLines">The most lines Auto draws.</param>
        public static float ScaleFor(RenderScaleChoice choice, int displayLines, int autoMostLines)
        {
            float scale;
            switch (choice)
            {
                case RenderScaleChoice.Native:
                    scale = Full;
                    break;
                case RenderScaleChoice.ThreeQuarters:
                    scale = ThreeQuarters;
                    break;
                case RenderScaleChoice.Half:
                    scale = Half;
                    break;
                default:
                    scale = displayLines > 0 ? Math.Min(Full, (float)autoMostLines / displayLines) : Full;
                    break;
            }

            return Math.Max(LeastScale, Math.Min(Full, scale));
        }
    }
}
