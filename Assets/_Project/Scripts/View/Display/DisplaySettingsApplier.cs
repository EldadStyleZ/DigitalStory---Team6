using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Plan section AD: puts the player's frame-rate and render-scale choices on screen. The frame rate becomes vsync pacing
    /// or a frame-rate cap (<see cref="FrameRateRule"/>); the render scale becomes the size of the full-screen window's
    /// backbuffer (<see cref="RenderScaleRule"/>), which the system scales up to the display, so every pixel-bound cost falls
    /// with it. The HUD keeps its layout, as its panel scales with the screen. A window keeps the size the player gave it.
    /// </summary>
    /// <remarks>
    /// Only a built player is changed: in the Editor the game view sizes itself, and a frame-rate change made in Play Mode
    /// could be saved into the project's quality settings. The camera's dynamic resolution would keep the HUD sharp, but
    /// with Crest's water it sampled its refraction copy at the wrong scale (a pale band over the river), so it is not used.
    /// </remarks>
    public sealed class DisplaySettingsApplier
    {
        private readonly DisplayConfig _config;

        /// <param name="config">The most lines Auto draws; never null.</param>
        public DisplaySettingsApplier(DisplayConfig config)
        {
            _config = config != null ? config : throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>The pacing last applied (or that would be, in the Editor), for tests and the run's notes.</summary>
        public FramePacing LastPacing { get; private set; }

        /// <summary>The render scale last applied (or that would be, in the Editor), for tests and the run's notes.</summary>
        public float LastScale { get; private set; } = 1f;

        /// <summary>The display's refresh rate, or 0 when unknown.</summary>
        public static double DisplayHz
        {
            get
            {
                double hz = Screen.currentResolution.refreshRateRatio.value;
                return double.IsNaN(hz) ? 0.0 : hz;
            }
        }

        /// <summary>Applies both choices.</summary>
        public void Apply(DisplaySettings settings)
        {
            LastPacing = FrameRateRule.PacingFor(settings.FrameRate, DisplayHz);
            LastScale = ScaleOf(settings.RenderScale);
            if (Application.isEditor)
            {
                return;
            }

            QualitySettings.vSyncCount = LastPacing.VSyncCount;
            Application.targetFrameRate = LastPacing.TargetFrameRate;
            ApplyScale(LastScale);
        }

        /// <summary>The share of the display a render-scale choice draws at here.</summary>
        public float ScaleOf(RenderScaleChoice choice)
        {
            return RenderScaleRule.ScaleFor(choice, Screen.currentResolution.height, _config.AutoMostLines);
        }

        /// <summary>How a frame-rate choice reads in the panel on this display.</summary>
        public string FrameRateText(FrameRateChoice choice)
        {
            switch (choice)
            {
                case FrameRateChoice.Rate120:
                    return "120 fps";
                case FrameRateChoice.Rate60:
                    return "60 fps (cooler)";
                default:
                    double hz = DisplayHz;
                    return hz > 0.0 ? "Display (" + Mathf.RoundToInt((float)hz).ToString(CultureInfo.InvariantCulture) + " Hz)" : "Display";
            }
        }

        /// <summary>How a render-scale choice reads in the panel on this display.</summary>
        public string RenderScaleText(RenderScaleChoice choice)
        {
            string percent = Mathf.RoundToInt(ScaleOf(choice) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
            switch (choice)
            {
                case RenderScaleChoice.Auto:
                    return "Auto (" + percent + ")";
                case RenderScaleChoice.Native:
                    return "Native (" + percent + ")";
                default:
                    return percent;
            }
        }

        private static void ApplyScale(float scale)
        {
            if (!Screen.fullScreen)
            {
                return;
            }

            Resolution display = Screen.currentResolution;
            int width = Mathf.Max(1, Mathf.RoundToInt(display.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(display.height * scale));
            if (width != Screen.width || height != Screen.height)
            {
                Screen.SetResolution(width, height, Screen.fullScreenMode);
            }
        }
    }
}
