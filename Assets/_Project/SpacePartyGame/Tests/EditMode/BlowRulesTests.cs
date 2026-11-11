using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class BlowChargeTests
    {
        [Test]
        public void New_IsNotCharging()
        {
            var charge = new BlowCharge(3f);

            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Progress(10f), Is.EqualTo(0f));
        }

        [Test]
        public void Progress_FillsOverTheChargeTime()
        {
            var charge = new BlowCharge(3f);

            charge.Begin(1f);

            Assert.That(charge.Progress(1f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(charge.Progress(2.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(charge.Progress(9f), Is.EqualTo(1f));
            Assert.That(charge.IsFull(9f), Is.True);
        }

        [Test]
        public void Release_WhenFull_FiresAndStops()
        {
            var charge = new BlowCharge(3f);
            charge.Begin(0f);

            Assert.That(charge.Release(3f), Is.True);
            Assert.That(charge.IsCharging, Is.False);
        }

        [Test]
        public void Release_Early_DoesNotFireAndEmpties()
        {
            var charge = new BlowCharge(3f);
            charge.Begin(0f);

            Assert.That(charge.Release(2.9f), Is.False);
            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Progress(5f), Is.EqualTo(0f));
        }

        [Test]
        public void Cancel_StopsWithoutFiring()
        {
            var charge = new BlowCharge(3f);
            charge.Begin(0f);

            charge.Cancel();

            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.Release(10f), Is.False);
        }

        [Test]
        public void Release_WithoutCharging_DoesNotFire()
        {
            Assert.That(new BlowCharge(3f).Release(10f), Is.False);
        }

        [TestCase(Note.Left, true)]
        [TestCase(Note.Root, false)]
        [TestCase(Note.RT, false)]
        public void Charges_OnlyTheLeftNote(Note note, bool expected)
        {
            Assert.That(BlowCharge.Charges(note), Is.EqualTo(expected));
        }
    }

    public sealed class BlowSoundTests
    {
        [Test]
        public void ChargeGain_GrowsFromStartToEnd()
        {
            Assert.That(BlowSound.ChargeGain(0f, 0.25f, 1.4f), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(BlowSound.ChargeGain(1f, 0.25f, 1.4f), Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(BlowSound.ChargeGain(2f, 0.25f, 1.4f), Is.EqualTo(1.4f).Within(0.0001f));
        }

        [Test]
        public void Ratio_MajorThirdAndFifth()
        {
            // E major on E (329.63 Hz): G sharp 415.30 Hz, B 493.88 Hz.
            Assert.That(329.63f * BlowSound.Ratio(4f), Is.EqualTo(415.30f).Within(0.05f));
            Assert.That(329.63f * BlowSound.Ratio(7f), Is.EqualTo(493.88f).Within(0.05f));
        }
    }

    public sealed class NoteHoldsUnlimitedTests
    {
        [Test]
        public void HoldWithoutLimit_KeepsTheNotePastTheLimit()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Left, 0f);

            holds.HoldWithoutLimit(Note.Left);

            Assert.That(holds.IsActive(Note.Left, 5f), Is.True);
        }

        [Test]
        public void HoldWithoutLimit_Null_PutsTheLimitBack()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Left, 0f);
            holds.HoldWithoutLimit(Note.Left);

            holds.HoldWithoutLimit(null);

            Assert.That(holds.IsActive(Note.Left, 5f), Is.False);
        }

        [Test]
        public void HoldWithoutLimit_OnlyThatNote()
        {
            var holds = new NoteHolds(1f);
            holds.Press(Note.Root, 0f);
            holds.HoldWithoutLimit(Note.Left);

            Assert.That(holds.IsActive(Note.Root, 5f), Is.False);
        }
    }

    public sealed class InstrumentPitchTests
    {
        [Test]
        public void ShiftBaseHertz_ScalesOnlyTheBasePitch()
        {
            string json = "{\"_baseHertz\":587.33,\"_vibratoHertz\":5.3}";

            string shifted = InstrumentPitch.ShiftBaseHertz(json, 2f);

            Assert.That(shifted, Does.Contain("\"_baseHertz\":1174.66"));
            Assert.That(shifted, Does.Contain("\"_vibratoHertz\":5.3"));
        }
    }

    public sealed class BlowConeTests
    {
        private static readonly Vector2 Origin = Vector2.zero;
        private static readonly Vector2 Right = Vector2.right;

        [Test]
        public void Contains_StraightAheadWithinReach()
        {
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(2f, 0f), 60f, 3f), Is.True);
        }

        [Test]
        public void Contains_PastTheReach_IsFalse()
        {
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(3.1f, 0f), 60f, 3f), Is.False);
        }

        [Test]
        public void Contains_JustInsideHalfTheAngle()
        {
            Vector2 at29 = Quaternion.Euler(0f, 0f, 29f) * Vector2.right * 2f;

            Assert.That(BlowCone.Contains(Origin, Right, at29, 60f, 3f), Is.True);
        }

        [Test]
        public void Contains_OutsideHalfTheAngle_IsFalse()
        {
            Vector2 at31 = Quaternion.Euler(0f, 0f, 31f) * Vector2.right * 2f;

            Assert.That(BlowCone.Contains(Origin, Right, at31, 60f, 3f), Is.False);
        }

        [Test]
        public void Contains_Behind_IsFalse()
        {
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(-1f, 0f), 60f, 3f), Is.False);
        }

        [Test]
        public void Contains_WithInnerRadius_LeavesOutTheGapByThePlayer()
        {
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(0.5f, 0f), 90f, 0.7f, 4.5f), Is.False);
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(0.8f, 0f), 90f, 0.7f, 4.5f), Is.True);
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(4.4f, 0f), 90f, 0.7f, 4.5f), Is.True);
            Assert.That(BlowCone.Contains(Origin, Right, new Vector2(4.6f, 0f), 90f, 0.7f, 4.5f), Is.False);
        }

        [Test]
        public void Contains_WithInnerRadius_StillKeepsTheAngle()
        {
            Vector2 at44 = Quaternion.Euler(0f, 0f, 44f) * Vector2.right * 2f;
            Vector2 at46 = Quaternion.Euler(0f, 0f, 46f) * Vector2.right * 2f;

            Assert.That(BlowCone.Contains(Origin, Right, at44, 90f, 0.7f, 4.5f), Is.True);
            Assert.That(BlowCone.Contains(Origin, Right, at46, 90f, 0.7f, 4.5f), Is.False);
        }

        [Test]
        public void FitAlong_MapsTheDrawingOntoTheArea()
        {
            float scale = BlowCone.FitAlong(0.04f, 7.72f, 0.7f, 4.5f, out float pivot);

            Assert.That(pivot + 0.04f * scale, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(pivot + 7.72f * scale, Is.EqualTo(4.5f).Within(0.0001f));
        }

        [Test]
        public void Push_AlongTheAim_WeakerAtTheEdge()
        {
            Vector2 near = BlowCone.Push(Origin, Vector2.up, new Vector2(0f, 0f), 3f, 10f, 0.4f);
            Vector2 far = BlowCone.Push(Origin, Vector2.up, new Vector2(1.8f, 2.4f), 3f, 10f, 0.4f);

            Assert.That(near.y, Is.EqualTo(10f).Within(0.001f));
            Assert.That(near.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(far.magnitude, Is.EqualTo(4f).Within(0.01f));
            Assert.That(far.x, Is.EqualTo(0f).Within(0.001f));
        }
    }

    public sealed class KnockbackTests
    {
        [Test]
        public void Velocity_FadesToNothing()
        {
            var shove = new Knockback(new Vector2(10f, 0f), 1f, 0.5f);

            Assert.That(shove.Velocity(1f).x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(shove.Velocity(1.25f).x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(shove.IsActive(1.5f), Is.False);
            Assert.That(shove.Velocity(2f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Default_IsNeverActive()
        {
            Assert.That(default(Knockback).IsActive(0f), Is.False);
        }

        [Test]
        public void Combine_KeepsOnlyPartOfOwnMovement()
        {
            Vector2 v = Knockback.Combine(new Vector2(0f, 4f), new Vector2(6f, 0f), 0.3f);

            Assert.That(v.x, Is.EqualTo(6f).Within(0.001f));
            Assert.That(v.y, Is.EqualTo(1.2f).Within(0.001f));
        }
    }

    public sealed class CameraShakeTests
    {
        [Test]
        public void Offset_StaysWithinTheAmplitude()
        {
            for (float t = 0f; t < 0.25f; t += 0.005f)
            {
                Vector2 offset = CameraShake.Offset(t, 0.25f, 0.2f);
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(0.2f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(0.2f));
            }
        }

        [Test]
        public void Offset_AfterTheShake_IsZero()
        {
            Assert.That(CameraShake.Offset(0.25f, 0.25f, 0.2f), Is.EqualTo(Vector2.zero));
            Assert.That(CameraShake.Offset(1f, 0.25f, 0.2f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Offset_MovesTheCameraDuringTheShake()
        {
            Assert.That(CameraShake.Offset(0.02f, 0.25f, 0.2f).magnitude, Is.GreaterThan(0.01f));
        }
    }
}
