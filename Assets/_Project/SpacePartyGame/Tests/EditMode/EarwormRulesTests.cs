using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class EarwormHoldTests
    {
        // An earworm lasts 8 seconds.
        private static EarwormHold Make() => new EarwormHold(8f);

        [Test]
        public void New_HoldsNobody()
        {
            EarwormHold hold = Make();

            Assert.That(hold.IsActive(0f), Is.False);
            Assert.That(hold.Holder, Is.EqualTo(-1));
        }

        [Test]
        public void Caught_IsHeldByThePlayerForTheEarwormTime()
        {
            EarwormHold hold = Make();

            hold.Catch(1, 10f);

            Assert.That(hold.Holder, Is.EqualTo(1));
            Assert.That(hold.IsActive(17.9f), Is.True);
            Assert.That(hold.IsActive(18f), Is.False);
        }

        [Test]
        public void WhileHeld_OnlyTheOtherPlayersCallsAreBlocked()
        {
            EarwormHold hold = Make();
            hold.Catch(1, 0f);

            Assert.That(hold.BlocksCall(0, 4f), Is.True);
            Assert.That(hold.BlocksCall(1, 4f), Is.False);
        }

        [Test]
        public void AfterItWearsOff_NoCallIsBlocked()
        {
            EarwormHold hold = Make();
            hold.Catch(1, 0f);

            Assert.That(hold.BlocksCall(0, 8f), Is.False);
        }

        [Test]
        public void AnotherPlayersEarworm_TakesItOverForAFullEarwormTime()
        {
            EarwormHold hold = Make();
            hold.Catch(1, 0f);

            hold.Catch(0, 5f);

            Assert.That(hold.Holder, Is.EqualTo(0));
            Assert.That(hold.IsActive(12.9f), Is.True);
            Assert.That(hold.BlocksCall(1, 6f), Is.True);
        }

        [Test]
        public void End_LetsGoAtOnce()
        {
            EarwormHold hold = Make();
            hold.Catch(1, 0f);

            hold.End();

            Assert.That(hold.IsActive(1f), Is.False);
            Assert.That(hold.BlocksCall(0, 1f), Is.False);
        }
    }

    public sealed class EarwormRulesTests
    {
        [TestCase(Note.Left, true)]
        [TestCase(Note.LT, false)]
        [TestCase(Note.RT, false)]
        [TestCase(Note.Root, false)]
        public void Charges_OnlyTheLeftNote_OnTheLTButton(Note note, bool expected)
        {
            Assert.That(EarwormRules.Charges(note), Is.EqualTo(expected));
        }

        [Test]
        public void Catches_FreeVermin_ButNotCagedOnes()
        {
            Assert.That(EarwormRules.Catches(isCaged: false), Is.True);
            Assert.That(EarwormRules.Catches(isCaged: true), Is.False);
        }
    }

    public sealed class OutlineToneTests
    {
        // A player colour at hue 0.6, saturation 0.5, brightness 0.8. Calls halve the saturation, earworms double it.
        private static readonly Color Player = Color.HSVToRGB(0.6f, 0.5f, 0.8f);

        private static Vector3 HsvOf(Color colour)
        {
            Color.RGBToHSV(colour, out float h, out float s, out float v);
            return new Vector3(h, s, v);
        }

        [Test]
        public void UnderACall_IsLessSaturated_SameHueAndBrightness()
        {
            Vector3 hsv = HsvOf(OutlineTone.For(Player, isEarwormed: false, 0.5f, 2f));

            Assert.That(hsv.x, Is.EqualTo(0.6f).Within(1e-3f));
            Assert.That(hsv.y, Is.EqualTo(0.25f).Within(1e-3f));
            Assert.That(hsv.z, Is.EqualTo(0.8f).Within(1e-3f));
        }

        [Test]
        public void UnderAnEarworm_IsMoreSaturated_AtTheSameBrightness()
        {
            Vector3 call = HsvOf(OutlineTone.For(Player, isEarwormed: false, 0.5f, 2f));
            Vector3 earworm = HsvOf(OutlineTone.For(Player, isEarwormed: true, 0.5f, 2f));

            Assert.That(earworm.x, Is.EqualTo(0.6f).Within(1e-3f));
            Assert.That(earworm.y, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(earworm.z, Is.EqualTo(call.z).Within(1e-3f));
            Assert.That(earworm.y, Is.GreaterThan(call.y));
        }

        [Test]
        public void Saturation_NeverGoesPastFull()
        {
            Vector3 hsv = HsvOf(OutlineTone.For(Color.HSVToRGB(0.1f, 0.9f, 0.8f), isEarwormed: true, 0.5f, 3f));

            Assert.That(hsv.y, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void KeepsTheAlpha()
        {
            Color faded = Player;
            faded.a = 0.5f;

            Assert.That(OutlineTone.For(faded, isEarwormed: true, 0.5f, 2f).a, Is.EqualTo(0.5f));
            Assert.That(OutlineTone.For(faded, isEarwormed: false, 0.5f, 2f).a, Is.EqualTo(0.5f));
        }
    }

    public sealed class VerminFollowForceTests
    {
        [Test]
        public void Force_FollowsThatPlayerAtOnce_EvenFromAnotherLeader()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(0, 5f, 5f);

            follow.Force(1, 6f);

            Assert.That(follow.Leader, Is.EqualTo(1));
            Assert.That(follow.IsFollowing(7.9f), Is.True);
            Assert.That(follow.IsFollowing(8f), Is.False);
        }

        [Test]
        public void AfterForce_AnyOtherCallCanTakeItOver()
        {
            var follow = new VerminFollow(2f);
            follow.Hear(0, 0f, 0f);
            follow.Force(1, 6f);

            // A call held since before the force still takes it over, once nothing blocks it.
            follow.Hear(0, 0f, 6.5f);

            Assert.That(follow.Leader, Is.EqualTo(0));
        }
    }
}
