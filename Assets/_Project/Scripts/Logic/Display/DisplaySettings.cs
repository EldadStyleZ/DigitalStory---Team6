using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The player's frame-rate and render-scale choices (plan section AD), beside the volumes of plan section AC: they step
    /// through their choices in order and hold at the ends, and load from and save to the same store, so the game keeps them
    /// in PlayerPrefs and tests in memory. An unknown stored value falls back to the default.
    /// </summary>
    public sealed class DisplaySettings
    {
        /// <summary>The store's key of the frame-rate choice.</summary>
        public const string FrameRateKey = "WaspPiper.Display.FrameRate";

        /// <summary>The store's key of the render-scale choice.</summary>
        public const string RenderScaleKey = "WaspPiper.Display.RenderScale";

        private const int FrameRateCount = 3;
        private const int RenderScaleCount = 4;

        private readonly FrameRateChoice _defaultFrameRate;
        private readonly RenderScaleChoice _defaultRenderScale;

        public DisplaySettings(FrameRateChoice frameRate, RenderScaleChoice renderScale)
        {
            _defaultFrameRate = IsKnown(frameRate) ? frameRate : FrameRateChoice.Display;
            _defaultRenderScale = IsKnown(renderScale) ? renderScale : RenderScaleChoice.Auto;
            FrameRate = _defaultFrameRate;
            RenderScale = _defaultRenderScale;
        }

        /// <summary>Raised when either choice changed.</summary>
        public event Action Changed;

        /// <summary>The chosen frame rate.</summary>
        public FrameRateChoice FrameRate { get; private set; }

        /// <summary>The chosen render scale.</summary>
        public RenderScaleChoice RenderScale { get; private set; }

        /// <summary>Reads a frame-rate choice from a word: display, 120 or 60. False for anything else.</summary>
        public static bool TryParseFrameRate(string word, out FrameRateChoice choice)
        {
            switch (word?.Trim().ToLowerInvariant())
            {
                case "display":
                    choice = FrameRateChoice.Display;
                    return true;
                case "120":
                    choice = FrameRateChoice.Rate120;
                    return true;
                case "60":
                    choice = FrameRateChoice.Rate60;
                    return true;
                default:
                    choice = FrameRateChoice.Display;
                    return false;
            }
        }

        /// <summary>Reads a render-scale choice from a word: auto, native, 75 or 50. False for anything else.</summary>
        public static bool TryParseRenderScale(string word, out RenderScaleChoice choice)
        {
            switch (word?.Trim().ToLowerInvariant())
            {
                case "auto":
                    choice = RenderScaleChoice.Auto;
                    return true;
                case "native":
                    choice = RenderScaleChoice.Native;
                    return true;
                case "75":
                    choice = RenderScaleChoice.ThreeQuarters;
                    return true;
                case "50":
                    choice = RenderScaleChoice.Half;
                    return true;
                default:
                    choice = RenderScaleChoice.Auto;
                    return false;
            }
        }

        /// <summary>Chooses a frame rate. Returns true and raises <see cref="Changed"/> when it changed.</summary>
        public bool SetFrameRate(FrameRateChoice choice)
        {
            if (!IsKnown(choice) || choice == FrameRate)
            {
                return false;
            }

            FrameRate = choice;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Chooses a render scale. Returns true and raises <see cref="Changed"/> when it changed.</summary>
        public bool SetRenderScale(RenderScaleChoice choice)
        {
            if (!IsKnown(choice) || choice == RenderScale)
            {
                return false;
            }

            RenderScale = choice;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Moves the frame-rate choice by <paramref name="step"/> in its order, holding at the ends.</summary>
        public bool StepFrameRate(int step) => SetFrameRate((FrameRateChoice)Held((int)FrameRate + step, FrameRateCount));

        /// <summary>Moves the render-scale choice by <paramref name="step"/> in its order, holding at the ends.</summary>
        public bool StepRenderScale(int step) => SetRenderScale((RenderScaleChoice)Held((int)RenderScale + step, RenderScaleCount));

        /// <summary>Takes both choices from the store, or the defaults where it has none or an unknown value.</summary>
        /// <exception cref="ArgumentNullException">The store is missing.</exception>
        public void Load(IVolumeStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            FrameRateChoice frameRate = store.TryGet(FrameRateKey, out int rate) && IsKnown((FrameRateChoice)rate)
                ? (FrameRateChoice)rate
                : _defaultFrameRate;
            RenderScaleChoice renderScale = store.TryGet(RenderScaleKey, out int scale) && IsKnown((RenderScaleChoice)scale)
                ? (RenderScaleChoice)scale
                : _defaultRenderScale;
            SetFrameRate(frameRate);
            SetRenderScale(renderScale);
        }

        /// <summary>Keeps both choices in the store and writes it out.</summary>
        /// <exception cref="ArgumentNullException">The store is missing.</exception>
        public void Save(IVolumeStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            store.Set(FrameRateKey, (int)FrameRate);
            store.Set(RenderScaleKey, (int)RenderScale);
            store.Save();
        }

        private static bool IsKnown(FrameRateChoice choice) => choice >= FrameRateChoice.Display && (int)choice < FrameRateCount;

        private static bool IsKnown(RenderScaleChoice choice) => choice >= RenderScaleChoice.Auto && (int)choice < RenderScaleCount;

        private static int Held(int value, int count) => Math.Max(0, Math.Min(count - 1, value));
    }
}
