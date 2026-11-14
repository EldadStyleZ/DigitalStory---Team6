using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section AC, the rules of the optional recorded sounds: the voice cap with the oldest taken over and the least gap
    /// (the impact pool of section AB, reused), the kill chain's pitch ladder, the clip choice that never repeats back to back,
    /// the ducking of the hum under big moments, the hum's mix of size, speed and mood, and which moment takes which sound.
    /// </summary>
    public sealed class GameSoundRulesTests
    {
        [Test]
        public void VoiceCap_AFullCapTakesTheOldestVoice_AndNeverMoreThanTheCap()
        {
            var voices = new ImpactFxPool(3, 0f);
            var taken = new HashSet<int>();
            for (int play = 0; play < 3; play++)
            {
                taken.Add(voices.Claim(play * 0.1f));
            }

            int fourth = voices.Claim(0.3f);

            Assert.That(taken.Count, Is.EqualTo(3));
            Assert.That(fourth, Is.EqualTo(0), "the voice that started first is taken over");
        }

        [Test]
        public void VoiceCap_TheLeastGap_RefusesASoundTooSoonAfterTheLast()
        {
            var voices = new ImpactFxPool(4, 0.06f);

            Assert.That(voices.Claim(1f), Is.Not.EqualTo(ImpactFxPool.None));
            Assert.That(voices.Claim(1.03f), Is.EqualTo(ImpactFxPool.None));
            Assert.That(voices.Claim(1.07f), Is.Not.EqualTo(ImpactFxPool.None));
        }

        [Test]
        public void KillChainLadder_ClimbsASemitoneAKill_UpToAnOctave()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3, 6, 10 });
            var rates = new float[15];
            for (int kill = 0; kill < rates.Length; kill++)
            {
                rates[kill] = SoundPitch.OfSemitones(chain.Kill(kill * 0.1f).Semitones);
            }

            Assert.That(rates[0], Is.EqualTo(1f).Within(1e-5f));
            Assert.That(rates[1], Is.EqualTo(Mathf.Pow(2f, 1f / 12f)).Within(1e-5f));
            Assert.That(rates[12], Is.EqualTo(2f).Within(1e-4f), "the thirteenth kill is an octave up");
            Assert.That(rates[14], Is.EqualTo(2f).Within(1e-4f), "and the ladder stops there");
            Assert.That(SoundPitch.OfSemitones(chain.Kill(10f).Semitones), Is.EqualTo(1f).Within(1e-5f), "a kill after a gap starts again");
        }

        [TestCase(0d, 0.95f)]
        [TestCase(0.5d, 1f)]
        [TestCase(0.999999d, 1.05f)]
        public void Varied_SpreadsTheRateByTheJitterEitherWay(double random01, float expected)
        {
            Assert.That(SoundPitch.Varied(1f, 0.05f, random01), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Varied_HoldsAJitterOutsideItsRange()
        {
            Assert.That(SoundPitch.Varied(1f, 2f, 0d), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(SoundPitch.Varied(1f, -1f, 0d), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ClipChoice_NeverRepeatsBackToBack_AndUsesEveryClip()
        {
            var choice = new ClipChoice(3, new Mulberry32Random(7u));
            var counts = new int[3];
            int last = -1;
            for (int pick = 0; pick < 3000; pick++)
            {
                int clip = choice.Next();
                Assert.That(clip, Is.Not.EqualTo(last), $"pick {pick}");
                counts[clip]++;
                last = clip;
            }

            for (int clip = 0; clip < counts.Length; clip++)
            {
                Assert.That(counts[clip], Is.InRange(800, 1200), $"clip {clip}");
            }
        }

        [Test]
        public void ClipChoice_TheSameSeed_ReplaysTheSameOrder_AndOneClipIsAlwaysIt()
        {
            var first = new ClipChoice(4, new Mulberry32Random(11u));
            var second = new ClipChoice(4, new Mulberry32Random(11u));
            for (int pick = 0; pick < 50; pick++)
            {
                Assert.That(first.Next(), Is.EqualTo(second.Next()));
            }

            var single = new ClipChoice(1, new Mulberry32Random(1u));
            Assert.That(single.Next(), Is.EqualTo(0));
            Assert.That(single.Next(), Is.EqualTo(0));
        }

        [Test]
        public void ClipChoice_BadArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClipChoice(0, new Mulberry32Random(1u)));
            Assert.Throws<ArgumentNullException>(() => new ClipChoice(2, null));
        }

        [Test]
        public void Duck_ABigMoment_LowersTheHumSixDecibels_ThenLetsItBack()
        {
            var duck = new SoundDuck(-6f, 0.3f, 0.03f, 0.15f);
            duck.Trigger();
            float lowest = 1f;
            for (int frame = 0; frame < 18; frame++)
            {
                lowest = Mathf.Min(lowest, duck.Step(1f / 60f));
            }

            Assert.That(20f * Mathf.Log10(lowest), Is.EqualTo(-6f).Within(0.3f), "down by 0.3 s");
            for (int frame = 0; frame < 60; frame++)
            {
                duck.Step(1f / 60f);
            }

            Assert.That(duck.Gain, Is.GreaterThan(0.95f), "back a second later");
        }

        [Test]
        public void Duck_NeverSteps_FrameToFrame()
        {
            var duck = new SoundDuck(-6f, 0.3f, 0.03f, 0.15f);
            float last = duck.Gain;
            duck.Trigger();
            for (int frame = 0; frame < 120; frame++)
            {
                float gain = duck.Step(1f / 120f);
                Assert.That(Mathf.Abs(gain - last), Is.LessThan(0.15f), $"frame {frame}");
                last = gain;
            }
        }

        [Test]
        public void Duck_BadNumbers_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundDuck(3f, 0.3f, 0.03f, 0.15f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundDuck(-6f, -1f, 0.03f, 0.15f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundDuck(-6f, 0.3f, 0f, 0.15f));
        }

        [Test]
        public void Hum_NoWasps_IsSilent_AndABiggerSwarmIsLouder()
        {
            SwarmHumMix hum = NewHum();

            Assert.That(hum.VolumeFor(0), Is.EqualTo(0f));
            Assert.That(hum.VolumeFor(1), Is.EqualTo(0.35f).Within(0.02f), "a single wasp hums at about the quiet share");
            Assert.That(hum.VolumeFor(800), Is.LessThan(hum.VolumeFor(2000)));
            Assert.That(hum.VolumeFor(4000), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(hum.VolumeFor(9000), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Hum_AFasterCore_HumsHigher_UpToTheTop()
        {
            SwarmHumMix hum = NewHum();

            Assert.That(hum.PitchFor(0f), Is.EqualTo(0.94f).Within(1e-5f));
            Assert.That(hum.PitchFor(2f), Is.GreaterThan(hum.PitchFor(1f)));
            Assert.That(hum.PitchFor(20f), Is.EqualTo(1.1f).Within(1e-5f));
        }

        [Test]
        public void Hum_AngryCrossFades_AtEqualPower_WithinItsTime_AndBack()
        {
            SwarmHumMix hum = NewHum();
            for (int frame = 0; frame < 120; frame++)
            {
                hum.Step(4000, 0f, false, 1f / 60f);
            }

            float full = hum.CalmVolume;
            for (int frame = 0; frame < 30; frame++)
            {
                hum.Step(4000, 0f, true, 1f / 60f);
                float power = (hum.CalmVolume * hum.CalmVolume) + (hum.AngryVolume * hum.AngryVolume);
                Assert.That(Mathf.Sqrt(power), Is.EqualTo(full).Within(0.01f), $"frame {frame}");
            }

            Assert.That(hum.AngryShare, Is.EqualTo(1f), "angry within 0.5 s");
            Assert.That(hum.CalmVolume, Is.EqualTo(0f).Within(1e-5f));
            for (int frame = 0; frame < 30; frame++)
            {
                hum.Step(4000, 0f, false, 1f / 60f);
            }

            Assert.That(hum.AngryShare, Is.EqualTo(0f));
        }

        [Test]
        public void Hum_EasesToANewSize_WithoutAStep()
        {
            SwarmHumMix hum = NewHum();
            hum.Step(4000, 0f, false, 1f / 60f);
            float last = hum.CalmVolume;
            for (int frame = 0; frame < 60; frame++)
            {
                hum.Step(4000, 0f, false, 1f / 60f);
                Assert.That(hum.CalmVolume - last, Is.LessThan(0.08f));
                last = hum.CalmVolume;
            }

            Assert.That(hum.CalmVolume, Is.EqualTo(1f).Within(0.02f), "full after a second");
        }

        [Test]
        public void Hum_Step_AllocatesNothing()
        {
            SwarmHumMix hum = NewHum();
            var duck = new SoundDuck(-6f, 0.3f, 0.03f, 0.15f);

            Assert.That(() => { hum.Step(1200, 2f, true, 0.016f); duck.Step(0.016f); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Choice_AKillTakesTheDeath_AHitTheHit_ABlockedBlowNone()
        {
            var kill = new SongHit(Vector3.zero, 40f, true, false, SwarmForm.Sting, false);
            var hit = new SongHit(Vector3.zero, 0.4f, false, false, SwarmForm.Sting, false);
            var blocked = new SongHit(Vector3.zero, 10f, false, true, SwarmForm.Dome, false);

            Assert.That(GameSoundChoice.ForHit(in kill, out GameSoundEvent killSound) && killSound == GameSoundEvent.BeekeeperDeath);
            Assert.That(GameSoundChoice.ForHit(in hit, out GameSoundEvent hitSound) && hitSound == GameSoundEvent.BeekeeperHit);
            Assert.That(GameSoundChoice.ForHit(in blocked, out _), Is.False);
        }

        [TestCase(ColossusAttack.Punch, GameSoundEvent.GiantPunch)]
        [TestCase(ColossusAttack.HeavyPunch, GameSoundEvent.GiantPunch)]
        [TestCase(ColossusAttack.Kick, GameSoundEvent.GiantKick)]
        [TestCase(ColossusAttack.SpinKick, GameSoundEvent.GiantKick)]
        [TestCase(ColossusAttack.Slam, GameSoundEvent.GiantSlam)]
        [TestCase(ColossusAttack.LeapSlam, GameSoundEvent.GiantLeapLanding)]
        public void Choice_EachGiantBlow_TakesItsSoundAsItLands(ColossusAttack attack, GameSoundEvent expected)
        {
            Assert.That(GameSoundChoice.ForColossusLanded(attack, out GameSoundEvent sound), Is.True);
            Assert.That(sound, Is.EqualTo(expected));
            Assert.That(GameSoundChoice.ForColossusStarted(attack, out _), Is.False);
        }

        [Test]
        public void Choice_TheRoarSoundsAsItStarts_NotAsItLands()
        {
            Assert.That(GameSoundChoice.ForColossusStarted(ColossusAttack.Roar, out GameSoundEvent sound) && sound == GameSoundEvent.GiantRoar);
            Assert.That(GameSoundChoice.ForColossusLanded(ColossusAttack.Roar, out _), Is.False);
        }

        [TestCase(NestComboStep.Advanced, true, GameSoundEvent.ComboRightNote)]
        [TestCase(NestComboStep.Broken, true, GameSoundEvent.ComboWrongNote)]
        [TestCase(NestComboStep.Restarted, true, GameSoundEvent.ComboWrongNote)]
        [TestCase(NestComboStep.Completed, false, GameSoundEvent.ComboRightNote)]
        [TestCase(NestComboStep.Ignored, false, GameSoundEvent.ComboRightNote)]
        public void Choice_ComboNotes(NestComboStep step, bool hasSound, GameSoundEvent expected)
        {
            Assert.That(GameSoundChoice.ForComboNote(step, out GameSoundEvent sound), Is.EqualTo(hasSound));
            if (hasSound)
            {
                Assert.That(sound, Is.EqualTo(expected));
            }
        }

        [Test]
        public void Choice_OnlyTheMeteorsLandingTakesAnImpact()
        {
            Assert.That(GameSoundChoice.ForFormLanded(SwarmForm.Meteor, out GameSoundEvent sound) && sound == GameSoundEvent.MeteorImpact);
            Assert.That(GameSoundChoice.ForFormLanded(SwarmForm.Tornado, out _), Is.False);
            Assert.That(GameSoundChoice.ForFormLanded(SwarmForm.Lance, out _), Is.False);
        }

        [Test]
        public void Choice_EverySongFamily_TakesItsCastSound_TheTowersShareOne_TheGiantNone()
        {
            AssertCast<StingSong>(GameSoundEvent.CastSting);
            AssertCast<TornadoSong>(GameSoundEvent.CastTornado);
            AssertCast<MeteorSong>(GameSoundEvent.CastMeteor);
            AssertCast<HydraSong>(GameSoundEvent.CastHydra);
            AssertCast<ScytheSong>(GameSoundEvent.CastScythe);
            AssertCast<LanceSong>(GameSoundEvent.CastLance);
            AssertCast<DomeSong>(GameSoundEvent.CastDome);
            AssertCast<GustSong>(GameSoundEvent.CastGust);
            AssertCast<HiveCallSong>(GameSoundEvent.CastHiveCall);
            AssertCast<HoneycombSong>(GameSoundEvent.TowerPlaced);
            AssertCast<NestSong>(GameSoundEvent.TowerPlaced);

            ColossusSong giant = ScriptableObject.CreateInstance<ColossusSong>();
            try
            {
                Assert.That(GameSoundChoice.ForCast(giant, out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(giant);
            }
        }

        [Test]
        public void Choice_BigMomentsDuck_AndRoutineOnesDoNot()
        {
            Assert.That(GameSoundChoice.IsBig(GameSoundEvent.GiantSlam), Is.True);
            Assert.That(GameSoundChoice.IsBig(GameSoundEvent.MeteorImpact), Is.True);
            Assert.That(GameSoundChoice.IsBig(GameSoundEvent.HiveLost), Is.True);
            Assert.That(GameSoundChoice.IsBig(GameSoundEvent.BeekeeperHit), Is.False);
            Assert.That(GameSoundChoice.IsBig(GameSoundEvent.CastSting), Is.False);
            Assert.That(GameSoundChoice.IsLoop(GameSoundEvent.SwarmLoop) && GameSoundChoice.IsLoop(GameSoundEvent.SwarmAngry), Is.True);
            Assert.That(GameSoundChoice.IsLoop(GameSoundEvent.Win), Is.False);
        }

        [Test]
        public void Library_SetAndClearSlots_AndAnEmptySlotIsNotSet()
        {
            GameSoundLibrary library = ScriptableObject.CreateInstance<GameSoundLibrary>();
            AudioClip clip = AudioClip.Create("StandIn", 480, 1, 48000, false);
            try
            {
                Assert.That(library.HasAnySound, Is.False);
                library.SetSlot(GameSoundEvent.Win, new GameSoundSlot(new[] { clip }, 0.5f, 0f, 1, 0f, 10));
                Assert.That(library.HasAnySound, Is.True);
                Assert.That(library.Slot(GameSoundEvent.Win).Priority, Is.EqualTo(10));
                Assert.That(new GameSoundSlot(new AudioClip[] { null }, 1f, 0f, 1, 0f, 0).IsSet, Is.False);

                library.ClearSlots();
                Assert.That(library.HasAnySound, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(library);
            }
        }

        private static SwarmHumMix NewHum() => new SwarmHumMix(4000f, 0.35f, 0.94f, 1.1f, 4.5f, 0.25f, 0.35f);

        private static void AssertCast<T>(GameSoundEvent expected) where T : SwarmSongEffect
        {
            T song = ScriptableObject.CreateInstance<T>();
            try
            {
                Assert.That(GameSoundChoice.ForCast(song, out GameSoundEvent sound), Is.True, typeof(T).Name);
                Assert.That(sound, Is.EqualTo(expected), typeof(T).Name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(song);
            }
        }
    }
}
