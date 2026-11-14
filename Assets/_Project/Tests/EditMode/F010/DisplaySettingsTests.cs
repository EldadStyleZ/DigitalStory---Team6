using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section AD, the heat controls in the settings panel: a frame-rate choice becomes vsync pacing when the display's
    /// rate is a whole multiple of it and a frame-rate cap otherwise, never faster than the display; a render-scale choice
    /// becomes a share of the display's lines, Auto rendering at most the config's lines and never upscaling a small display;
    /// the choices step, hold their ends, and survive a save and a load through the store that comes in.
    /// </summary>
    public sealed class DisplaySettingsTests
    {
        [TestCase(FrameRateChoice.Display, 120.0, 1, -1)]
        [TestCase(FrameRateChoice.Display, 60.0, 1, -1)]
        [TestCase(FrameRateChoice.Rate120, 120.0, 1, -1)]
        [TestCase(FrameRateChoice.Rate120, 119.88, 1, -1)]
        [TestCase(FrameRateChoice.Rate60, 120.0, 2, -1)]
        [TestCase(FrameRateChoice.Rate60, 59.94, 1, -1)]
        [TestCase(FrameRateChoice.Rate120, 240.0, 2, -1)]
        [TestCase(FrameRateChoice.Rate60, 240.0, 4, -1)]
        [TestCase(FrameRateChoice.Rate120, 60.0, 1, -1)]
        [TestCase(FrameRateChoice.Rate120, 144.0, 0, 120)]
        [TestCase(FrameRateChoice.Rate60, 144.0, 0, 60)]
        [TestCase(FrameRateChoice.Rate60, 300.0, 0, 60)]
        [TestCase(FrameRateChoice.Rate60, 0.0, 0, 60)]
        [TestCase(FrameRateChoice.Rate60, double.NaN, 0, 60)]
        public void Pacing_SyncsToAWholeShareOfTheDisplay_OrCapsTheRate(FrameRateChoice choice, double displayHz, int vSync, int target)
        {
            FramePacing pacing = FrameRateRule.PacingFor(choice, displayHz);

            Assert.That(pacing.VSyncCount, Is.EqualTo(vSync), "vsync count");
            Assert.That(pacing.TargetFrameRate, Is.EqualTo(target), "target frame rate");
        }

        [Test]
        public void Pacing_NeverAsksForMoreThanFourRefreshesAFrame()
        {
            for (double hz = 30.0; hz <= 1000.0; hz += 0.5)
            {
                foreach (FrameRateChoice choice in new[] { FrameRateChoice.Display, FrameRateChoice.Rate120, FrameRateChoice.Rate60 })
                {
                    FramePacing pacing = FrameRateRule.PacingFor(choice, hz);
                    Assert.That(pacing.VSyncCount, Is.InRange(0, FrameRateRule.MostRefreshesPerFrame), $"{choice} at {hz} Hz");
                    Assert.That(pacing.VSyncCount > 0 || pacing.TargetFrameRate > 0, Is.True, $"{choice} at {hz} Hz paces nothing");
                }
            }
        }

        [TestCase(RenderScaleChoice.Native, 2160, 1f)]
        [TestCase(RenderScaleChoice.ThreeQuarters, 2160, 0.75f)]
        [TestCase(RenderScaleChoice.Half, 2160, 0.5f)]
        [TestCase(RenderScaleChoice.Auto, 2160, 1600f / 2160f)]
        [TestCase(RenderScaleChoice.Auto, 1600, 1f)]
        [TestCase(RenderScaleChoice.Auto, 1080, 1f)]
        [TestCase(RenderScaleChoice.Auto, 4320, 1600f / 4320f)]
        [TestCase(RenderScaleChoice.Auto, 0, 1f)]
        public void Scale_IsAShareOfTheDisplay_AutoCapsTheLinesAndNeverUpscales(RenderScaleChoice choice, int displayLines, float scale)
        {
            Assert.That(RenderScaleRule.ScaleFor(choice, displayLines, 1600), Is.EqualTo(scale).Within(1e-5f));
        }

        [Test]
        public void Scale_IsNeverBelowItsFloor()
        {
            Assert.That(RenderScaleRule.ScaleFor(RenderScaleChoice.Auto, 100000, 1600), Is.EqualTo(RenderScaleRule.LeastScale));
            Assert.That(RenderScaleRule.ScaleFor(RenderScaleChoice.Auto, 2160, 0), Is.EqualTo(RenderScaleRule.LeastScale));
        }

        [Test]
        public void Step_MovesThroughTheChoicesInTheirOrder_AndHoldsTheEnds()
        {
            var settings = new DisplaySettings(FrameRateChoice.Display, RenderScaleChoice.Auto);
            int changes = 0;
            settings.Changed += () => changes++;

            Assert.That(settings.StepFrameRate(-1), Is.False, "Display is the first choice");
            Assert.That(settings.StepFrameRate(1), Is.True);
            Assert.That(settings.FrameRate, Is.EqualTo(FrameRateChoice.Rate120));
            Assert.That(settings.StepFrameRate(1), Is.True);
            Assert.That(settings.FrameRate, Is.EqualTo(FrameRateChoice.Rate60));
            Assert.That(settings.StepFrameRate(1), Is.False, "60 is the last choice");

            Assert.That(settings.StepRenderScale(1), Is.True);
            Assert.That(settings.RenderScale, Is.EqualTo(RenderScaleChoice.Native));
            Assert.That(settings.StepRenderScale(5), Is.True);
            Assert.That(settings.RenderScale, Is.EqualTo(RenderScaleChoice.Half));
            Assert.That(changes, Is.EqualTo(4));
        }

        [Test]
        public void Defaults_AreUsedWhenTheStoreHasNothing_OrSomethingUnknown()
        {
            var store = new MemoryStore();
            store.Set(DisplaySettings.RenderScaleKey, 99);
            var settings = new DisplaySettings(FrameRateChoice.Rate60, RenderScaleChoice.Native);

            settings.Load(store);

            Assert.That(settings.FrameRate, Is.EqualTo(FrameRateChoice.Rate60));
            Assert.That(settings.RenderScale, Is.EqualTo(RenderScaleChoice.Native), "an unknown stored value falls back to the default");
        }

        [Test]
        public void SaveAndLoad_KeepTheChoices_AcrossARestart()
        {
            var store = new MemoryStore();
            var before = new DisplaySettings(FrameRateChoice.Display, RenderScaleChoice.Auto);
            before.SetFrameRate(FrameRateChoice.Rate60);
            before.SetRenderScale(RenderScaleChoice.ThreeQuarters);

            before.Save(store);
            var after = new DisplaySettings(FrameRateChoice.Display, RenderScaleChoice.Auto);
            after.Load(store);

            Assert.That(after.FrameRate, Is.EqualTo(FrameRateChoice.Rate60));
            Assert.That(after.RenderScale, Is.EqualTo(RenderScaleChoice.ThreeQuarters));
            Assert.That(store.Saves, Is.EqualTo(1));
        }

        [Test]
        public void Load_AndSave_RefuseAMissingStore()
        {
            var settings = new DisplaySettings(FrameRateChoice.Display, RenderScaleChoice.Auto);

            Assert.Throws<ArgumentNullException>(() => settings.Load(null));
            Assert.Throws<ArgumentNullException>(() => settings.Save(null));
        }

        [TestCase("display", FrameRateChoice.Display)]
        [TestCase("120", FrameRateChoice.Rate120)]
        [TestCase("60", FrameRateChoice.Rate60)]
        public void CommandLine_NamesAFrameRate(string word, FrameRateChoice choice)
        {
            Assert.That(DisplaySettings.TryParseFrameRate(word, out FrameRateChoice parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(choice));
        }

        [TestCase("auto", RenderScaleChoice.Auto)]
        [TestCase("native", RenderScaleChoice.Native)]
        [TestCase("75", RenderScaleChoice.ThreeQuarters)]
        [TestCase("50", RenderScaleChoice.Half)]
        public void CommandLine_NamesARenderScale(string word, RenderScaleChoice choice)
        {
            Assert.That(DisplaySettings.TryParseRenderScale(word, out RenderScaleChoice parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(choice));
        }

        [Test]
        public void CommandLine_RefusesAnUnknownWord()
        {
            Assert.That(DisplaySettings.TryParseFrameRate("90", out _), Is.False);
            Assert.That(DisplaySettings.TryParseRenderScale(null, out _), Is.False);
        }

        private sealed class MemoryStore : IVolumeStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int Saves { get; private set; }

            public bool TryGet(string key, out int value) => _values.TryGetValue(key, out value);

            public void Set(string key, int value) => _values[key] = value;

            public void Save() => Saves++;
        }
    }
}
