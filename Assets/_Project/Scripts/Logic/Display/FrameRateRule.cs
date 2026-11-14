using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Plan section AD: turns a frame-rate choice into pacing for this display. When the display refreshes at a whole multiple
    /// of the chosen rate, frames wait for every n-th refresh (smooth, no tearing); a display no faster than the rate is
    /// simply followed; any other display gets the rate as a cap without vsync.
    /// </summary>
    public static class FrameRateRule
    {
        /// <summary>The most refreshes Unity lets a frame wait for.</summary>
        public const int MostRefreshesPerFrame = 4;

        // How far a display's rate may sit from a whole multiple of the chosen rate and still count as one: 119.88 Hz is 120.
        private const double MultipleTolerance = 0.03;
        private const int NoCap = -1;
        private const int OneRefresh = 1;
        private const int NoVSync = 0;

        /// <summary>The pacing for a choice on a display refreshing <paramref name="displayHz"/> times a second.</summary>
        /// <param name="choice">The player's choice.</param>
        /// <param name="displayHz">The display's refresh rate; 0 or not a number when unknown.</param>
        public static FramePacing PacingFor(FrameRateChoice choice, double displayHz)
        {
            if (choice == FrameRateChoice.Display)
            {
                return new FramePacing(OneRefresh, NoCap);
            }

            int rate = RateOf(choice);
            bool isKnown = !double.IsNaN(displayHz) && displayHz > 0.0;
            if (!isKnown)
            {
                return new FramePacing(NoVSync, rate);
            }

            if (displayHz <= rate * (1.0 + MultipleTolerance))
            {
                return new FramePacing(OneRefresh, NoCap);
            }

            int refreshes = (int)Math.Round(displayHz / rate);
            bool isMultiple = Math.Abs(displayHz - (refreshes * rate)) <= rate * MultipleTolerance;
            return isMultiple && refreshes <= MostRefreshesPerFrame
                ? new FramePacing(refreshes, NoCap)
                : new FramePacing(NoVSync, rate);
        }

        /// <summary>The frames a second a fixed choice stands for.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The choice is Display, which has no fixed rate, or unknown.</exception>
        public static int RateOf(FrameRateChoice choice)
        {
            switch (choice)
            {
                case FrameRateChoice.Rate120:
                    return 120;
                case FrameRateChoice.Rate60:
                    return 60;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choice), choice, "Only a fixed frame-rate choice has a rate.");
            }
        }
    }
}
