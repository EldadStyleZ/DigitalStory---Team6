using NUnit.Framework;

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
}
