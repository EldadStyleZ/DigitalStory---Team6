using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section AC, the settings panel's rules: a volume's percentage becomes a gain on a square curve (0 silent, 100
    /// full, 50 a quarter), a group's gain is the master's times its own, out-of-range values are held, the volumes survive a
    /// save and a load through the store that comes in; and the time scale has one say, the pause over the slow-down over the
    /// scale found, the slow-down holding still while paused.
    /// </summary>
    public sealed class SettingsRulesTests
    {
        [TestCase(0, 0f)]
        [TestCase(10, 0.01f)]
        [TestCase(50, 0.25f)]
        [TestCase(100, 1f)]
        [TestCase(-20, 0f)]
        [TestCase(140, 1f)]
        public void Curve_IsTheSquareOfTheShare_HeldToItsRange(int percent, float gain)
        {
            Assert.That(VolumeCurve.GainOf(percent), Is.EqualTo(gain).Within(1e-6f));
        }

        [Test]
        public void Curve_RisesWithEveryStep()
        {
            for (int percent = 1; percent <= 100; percent++)
            {
                Assert.That(VolumeCurve.GainOf(percent), Is.GreaterThan(VolumeCurve.GainOf(percent - 1)), $"{percent}%");
            }
        }

        [Test]
        public void Gain_IsTheMastersTimesTheGroups()
        {
            var volumes = new VolumeSettings(50, 100, 80);

            Assert.That(volumes.GainOf(VolumeGroup.Master), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(volumes.GainOf(VolumeGroup.Music), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(volumes.GainOf(VolumeGroup.Effects), Is.EqualTo(0.25f * 0.64f).Within(1e-6f));

            volumes.Set(VolumeGroup.Master, 0);
            Assert.That(volumes.GainOf(VolumeGroup.Music), Is.EqualTo(0f), "a silent master silences everything");
        }

        [Test]
        public void Set_HoldsTheRange_AndRaisesChangedOnlyOnAChange()
        {
            var volumes = new VolumeSettings(100, 100, 100);
            var changed = new List<VolumeGroup>();
            volumes.Changed += group => changed.Add(group);

            Assert.That(volumes.Set(VolumeGroup.Music, 140), Is.False, "100 is already the top");
            Assert.That(volumes.Step(VolumeGroup.Effects, -5), Is.True);
            Assert.That(volumes.Set(VolumeGroup.Master, -3), Is.True);

            Assert.That(volumes.PercentOf(VolumeGroup.Music), Is.EqualTo(100));
            Assert.That(volumes.PercentOf(VolumeGroup.Effects), Is.EqualTo(95));
            Assert.That(volumes.PercentOf(VolumeGroup.Master), Is.EqualTo(0));
            Assert.That(changed, Is.EqualTo(new[] { VolumeGroup.Effects, VolumeGroup.Master }));
        }

        [Test]
        public void Defaults_AreHeldToTheRange_AndUsedWhenTheStoreHasNothing()
        {
            var volumes = new VolumeSettings(120, 70, -5);

            volumes.Load(new MemoryStore());

            Assert.That(volumes.PercentOf(VolumeGroup.Master), Is.EqualTo(100));
            Assert.That(volumes.PercentOf(VolumeGroup.Music), Is.EqualTo(70));
            Assert.That(volumes.PercentOf(VolumeGroup.Effects), Is.EqualTo(0));
        }

        [Test]
        public void SaveAndLoad_KeepTheVolumes_AcrossARestart()
        {
            var store = new MemoryStore();
            var before = new VolumeSettings(100, 100, 100);
            before.Set(VolumeGroup.Master, 80);
            before.Set(VolumeGroup.Music, 35);
            before.Set(VolumeGroup.Effects, 60);

            before.Save(store);
            var after = new VolumeSettings(100, 100, 100);
            after.Load(store);

            Assert.That(store.Saves, Is.EqualTo(1));
            Assert.That(after.PercentOf(VolumeGroup.Master), Is.EqualTo(80));
            Assert.That(after.PercentOf(VolumeGroup.Music), Is.EqualTo(35));
            Assert.That(after.PercentOf(VolumeGroup.Effects), Is.EqualTo(60));
        }

        [Test]
        public void Load_AValueOutOfRangeInTheStore_IsHeld()
        {
            var store = new MemoryStore();
            store.Set(VolumeSettings.Keys[(int)VolumeGroup.Music], 900);
            var volumes = new VolumeSettings(100, 50, 100);

            volumes.Load(store);

            Assert.That(volumes.PercentOf(VolumeGroup.Music), Is.EqualTo(100));
        }

        [Test]
        public void LoadAndSave_WithoutAStore_Throw()
        {
            var volumes = new VolumeSettings(100, 100, 100);

            Assert.Throws<ArgumentNullException>(() => volumes.Load(null));
            Assert.Throws<ArgumentNullException>(() => volumes.Save(null));
        }

        [Test]
        public void Clock_WithoutRequests_KeepsTheScaleItFinds()
        {
            var clock = new TimeScaleRule();
            clock.Step(0.1f, 1f);

            Assert.That(clock.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Clock_ThePauseWinsOverTheSlowDown_AndTheSlowDownWaitsForIt()
        {
            var clock = new TimeScaleRule();
            clock.StartSlow(0.4f, 0.25f, 1f);
            clock.Wrote(clock.Scale);
            Assert.That(clock.Scale, Is.EqualTo(0.4f).Within(1e-6f));

            clock.Pause(true, clock.Scale);
            clock.Wrote(clock.Scale);
            Assert.That(clock.Scale, Is.EqualTo(0f));
            clock.Step(5f, 0f);
            Assert.That(clock.IsSlowing, Is.True, "the slow-down ran while paused");

            clock.Pause(false, 0f);
            clock.Wrote(clock.Scale);
            Assert.That(clock.Scale, Is.EqualTo(0.4f).Within(1e-6f), "closing goes back into the slow-down");
            clock.Step(0.3f, clock.Scale);
            Assert.That(clock.IsSlowing, Is.False);
            Assert.That(clock.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Clock_AScaleSetElsewhere_BecomesTheOneFound_AndEndsTheSlowDown()
        {
            var clock = new TimeScaleRule();
            clock.StartSlow(0.4f, 0.25f, 1f);
            clock.Wrote(clock.Scale);

            clock.Step(0.01f, 4f);

            Assert.That(clock.IsSlowing, Is.False);
            Assert.That(clock.Scale, Is.EqualTo(4f));
        }

        [Test]
        public void Clock_EndAll_LeavesTheScaleFound()
        {
            var clock = new TimeScaleRule();
            clock.StartSlow(0.4f, 0.25f, 2f);
            clock.Wrote(clock.Scale);
            clock.Pause(true, clock.Scale);
            clock.Wrote(clock.Scale);

            Assert.That(clock.EndAll(0f), Is.EqualTo(2f));
            Assert.That(clock.IsPaused || clock.IsSlowing, Is.False);
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
