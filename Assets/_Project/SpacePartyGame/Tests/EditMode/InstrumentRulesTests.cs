using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class NotePressLatchTests
    {
        [Test]
        public void APress_CountsOnce_UntilReleased()
        {
            var latch = new NotePressLatch();

            Assert.That(latch.Press(Note.Left), Is.True);
            Assert.That(latch.Press(Note.Left), Is.False);
        }

        [Test]
        public void AfterARelease_TheNextPressCountsAgain()
        {
            var latch = new NotePressLatch();
            latch.Press(Note.Left);

            Assert.That(latch.Release(Note.Left), Is.True);
            Assert.That(latch.Press(Note.Left), Is.True);
        }

        [Test]
        public void AReleaseWithoutAPress_CountsForNothing()
        {
            Assert.That(new NotePressLatch().Release(Note.RT), Is.False);
        }

        [Test]
        public void Notes_AreLatchedSeparately()
        {
            var latch = new NotePressLatch();
            latch.Press(Note.Left);

            Assert.That(latch.Press(Note.RT), Is.True);
        }

        [Test]
        public void ReleaseAll_LetsEveryNoteBePressedAgain()
        {
            var latch = new NotePressLatch();
            latch.Press(Note.Left);
            latch.Press(Note.RT);

            latch.ReleaseAll();

            Assert.That(latch.Press(Note.Left), Is.True);
            Assert.That(latch.Press(Note.RT), Is.True);
        }
    }

    public sealed class NoteHoldsTests
    {
        [Test]
        public void Press_MakesTheNoteActiveAndSounding()
        {
            var holds = new NoteHolds(1f);

            holds.Press(Note.Root, 0f);

            Assert.That(holds.IsActive(Note.Root, 0.5f), Is.True);
            Assert.That(holds.Sounding(0.5f), Is.EqualTo(Note.Root));
        }

        [Test]
        public void Release_EndsTheNote()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);

            holds.Release(Note.Root);

            Assert.That(holds.IsActive(Note.Root, 0.2f), Is.False);
            Assert.That(holds.Sounding(0.2f), Is.Null);
        }

        [Test]
        public void Holding_PastTheLimit_EndsTheNote()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);

            Assert.That(holds.IsActive(Note.Root, 0.99f), Is.True);
            Assert.That(holds.IsActive(Note.Root, 1f), Is.False);
            Assert.That(holds.Sounding(1.5f), Is.Null);
        }

        [Test]
        public void TwoNotes_TheLastPressedSounds_AndEachKeepsItsOwnTimer()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);
            holds.Press(Note.Up, 0.6f);

            Assert.That(holds.Sounding(0.7f), Is.EqualTo(Note.Up));
            Assert.That(holds.IsActive(Note.Root, 0.7f), Is.True);
            Assert.That(holds.IsActive(Note.Root, 1.1f), Is.False);
            Assert.That(holds.Sounding(1.1f), Is.EqualTo(Note.Up));
            Assert.That(holds.Sounding(1.6f), Is.Null);
        }

        [Test]
        public void ReleasingTheLastNote_FallsBackToAnEarlierOneStillActive()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);
            holds.Press(Note.Up, 0.2f);

            holds.Release(Note.Up);

            Assert.That(holds.Sounding(0.3f), Is.EqualTo(Note.Root));
        }

        [Test]
        public void PressingAgain_RestartsItsSecond()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);
            holds.Release(Note.Root);

            holds.Press(Note.Root, 2f);

            Assert.That(holds.IsActive(Note.Root, 2.9f), Is.True);
        }

        [Test]
        public void StartedAt_IsWhenTheActiveNoteWasPressed()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0.4f);

            Assert.That(holds.StartedAt(Note.Root), Is.EqualTo(0.4f));
        }
    }

    public sealed class InstrumentKeysTests
    {
        // LT and Left are swapped from the flute's order, so running (LT, RT, LT, RT) trills G and B, a sweet
        // major third, instead of the hollow fifth E and B.
        [TestCase(Note.Root, 1)]
        [TestCase(Note.Left, 2)]
        [TestCase(Note.LT, 3)]
        [TestCase(Note.Right, 4)]
        [TestCase(Note.RT, 5)]
        [TestCase(Note.Up, 6)]
        public void KeyOf_LowToHigh(Note note, int key)
        {
            Assert.That(InstrumentKeys.KeyOf(note), Is.EqualTo(key));
        }

        [Test]
        public void KeyOf_Nothing_IsSilent()
        {
            Assert.That(InstrumentKeys.KeyOf(null), Is.EqualTo(0));
        }
    }

    public sealed class RunHoldTests
    {
        // Walk 4, run 7, 0.1 s from walking to full run (so 30 units per second, per second, both ways).
        private static RunHold Make() => new RunHold(4f, 7f, 0.1f);

        [Test]
        public void New_IsWalkingSpeed()
        {
            Assert.That(Make().Speed, Is.EqualTo(4f));
            Assert.That(Make().Boost, Is.EqualTo(0f));
        }

        [Test]
        public void Holding_ReachesHalfwayInHalfTheAccelerationTime()
        {
            RunHold run = Make();

            run.Tick(true, 0.05f);

            Assert.That(run.Speed, Is.EqualTo(5.5f).Within(1e-4f));
            Assert.That(run.Boost, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Holding_ReachesTheRunSpeedInTheAccelerationTime_AndStopsThere()
        {
            RunHold run = Make();

            run.Tick(true, 0.1f);
            Assert.That(run.Speed, Is.EqualTo(7f).Within(1e-4f));

            run.Tick(true, 1f);
            Assert.That(run.Speed, Is.EqualTo(7f));
            Assert.That(run.Boost, Is.EqualTo(1f));
        }

        [Test]
        public void Releasing_FromFullRun_DropsAtOnce_AndTakesTheAccelerationTime()
        {
            RunHold run = Make();
            run.Tick(true, 1f);

            run.Tick(false, 0.05f);
            Assert.That(run.Speed, Is.EqualTo(5.5f).Within(1e-4f));

            run.Tick(false, 0.05f);
            Assert.That(run.Speed, Is.EqualTo(4f).Within(1e-4f));
        }

        [Test]
        public void Releasing_WhileStillSpeedingUp_SlowsBackInTheTimeSpentSpeedingUp()
        {
            RunHold run = Make();
            run.Tick(true, 0.04f);

            run.Tick(false, 0.04f);

            Assert.That(run.Speed, Is.EqualTo(4f).Within(1e-4f));
        }

        [Test]
        public void PressingAgain_WhileSlowing_SpeedsUpFromTheCurrentSpeed()
        {
            RunHold run = Make();
            run.Tick(true, 1f);
            run.Tick(false, 0.05f);

            run.Tick(true, 0.02f);

            Assert.That(run.Speed, Is.EqualTo(6.1f).Within(1e-4f));
        }

        [Test]
        public void NotHolding_StaysAtWalking()
        {
            RunHold run = Make();

            run.Tick(false, 1f);

            Assert.That(run.Speed, Is.EqualTo(4f));
        }

        [Test]
        public void ZeroAccelerationTime_SwitchesAtOnce()
        {
            RunHold run = new RunHold(4f, 7f, 0f);

            run.Tick(true, 0.01f);
            Assert.That(run.Speed, Is.EqualTo(7f));

            run.Tick(false, 0.01f);
            Assert.That(run.Speed, Is.EqualTo(4f));
        }
    }

    public sealed class CelloSynthTests
    {
        private const int SampleRate = 48000;

        private static float Loudest(CelloSynth synth, int buffers)
        {
            float loudest = 0f;
            var buffer = new float[1024];

            for (int i = 0; i < buffers; i++)
            {
                synth.Fill(buffer, 2);

                foreach (float sample in buffer)
                {
                    loudest = Mathf.Max(loudest, Mathf.Abs(sample));
                }
            }

            return loudest;
        }

        [Test]
        public void Silent_UntilANoteIsPlayed()
        {
            var synth = new CelloSynth(new CelloSound(), SampleRate);

            Assert.That(Loudest(synth, 4), Is.EqualTo(0f));
        }

        [Test]
        public void Play_MakesSound()
        {
            var synth = new CelloSynth(new CelloSound(), SampleRate);

            synth.Play(1);

            Assert.That(Loudest(synth, 20), Is.GreaterThan(0.01f));
        }

        [Test]
        public void Silence_FadesOut()
        {
            var synth = new CelloSynth(new CelloSound(), SampleRate);
            synth.Play(1);
            Loudest(synth, 20);

            synth.Play(CelloSynth.Silent);
            Loudest(synth, 60);

            Assert.That(Loudest(synth, 2), Is.LessThan(0.001f));
        }

        [Test]
        public void Pitches_AreAnOctaveBelowTheFlute()
        {
            // The toy flute's holes: D5 E5 G5 A5 B5 D6 (587.33 Hz and 0, 2, 5, 7, 9, 12 semitones up).
            float[] flute = { 587.33f, 659.26f, 783.99f, 880.00f, 987.77f, 1174.66f };
            var cello = new CelloSound();

            for (int key = 1; key <= 6; key++)
            {
                Assert.That(cello.HertzOf(key), Is.EqualTo(flute[key - 1] / 2f).Within(0.05f));
            }
        }
    }

    public sealed class VerminFollowTests
    {
        [Test]
        public void New_IsNotFollowing()
        {
            Assert.That(new VerminFollow(2f).IsFollowing(0f), Is.False);
        }

        [Test]
        public void Hear_FollowsTheCaller_UntilTheTimerRunsOut()
        {
            var follow = new VerminFollow(2f);

            follow.Hear(1, callStartedAt: 0f, now: 0f);

            Assert.That(follow.Leader, Is.EqualTo(1));
            Assert.That(follow.IsFollowing(1.9f), Is.True);
            Assert.That(follow.IsFollowing(2f), Is.False);
        }

        [Test]
        public void HearingAgain_ResetsTheTimer()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(1, 0f, 0f);

            follow.Hear(1, 0f, 1.5f);

            Assert.That(follow.IsFollowing(3f), Is.True);
        }

        [Test]
        public void ALaterCall_TakesItOver()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(0, callStartedAt: 0f, now: 1f);

            follow.Hear(1, callStartedAt: 0.5f, now: 1f);

            Assert.That(follow.Leader, Is.EqualTo(1));
        }

        [Test]
        public void AnEarlierCall_DoesNotTakeItOver()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(1, callStartedAt: 0.5f, now: 1f);

            follow.Hear(0, callStartedAt: 0f, now: 1.2f);

            Assert.That(follow.Leader, Is.EqualTo(1));
            Assert.That(follow.IsFollowing(3.1f), Is.False);
        }

        [Test]
        public void AfterTheTimerRunsOut_AnyCallerCanTakeIt()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(1, callStartedAt: 5f, now: 5f);

            follow.Hear(0, callStartedAt: 1f, now: 8f);

            Assert.That(follow.Leader, Is.EqualTo(0));
            Assert.That(follow.IsFollowing(8.5f), Is.True);
        }

        [Test]
        public void Stop_EndsFollowingAtOnce()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(1, 0f, 0f);

            follow.Stop();

            Assert.That(follow.IsFollowing(0.1f), Is.False);
        }
    }

    public sealed class CallRulesTests
    {
        [TestCase(0, null, true)]
        [TestCase(0, 1, true)]
        [TestCase(0, 0, false)]
        public void Hears_UnlessInTheCallersOwnCage(int caller, int? cageOwner, bool expected)
        {
            Assert.That(CallRules.Hears(caller, cageOwner), Is.EqualTo(expected));
        }
    }

    public sealed class SteeringTests
    {
        [Test]
        public void Toward_FarAway_GoesAtTopSpeed()
        {
            Vector2 velocity = Steering.Toward(Vector2.zero, new Vector2(10f, 0f), 1.1f, 3.6f);

            Assert.That(velocity.x, Is.EqualTo(3.6f).Within(1e-4f));
            Assert.That(velocity.y, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Toward_WithinTheStopDistance_Stops()
        {
            Assert.That(Steering.Toward(Vector2.zero, new Vector2(1f, 0f), 1.1f, 3.6f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Toward_Arriving_SlowsDown()
        {
            Vector2 velocity = Steering.Toward(Vector2.zero, new Vector2(1.3f, 0f), 1.1f, 3.6f);

            Assert.That(velocity.x, Is.EqualTo(0.8f).Within(1e-4f));
        }
    }
}
