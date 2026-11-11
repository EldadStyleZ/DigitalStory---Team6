using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section H, where the nests stand, on the default NestConfig: each at least 12 m from every hive; the two open at
    /// the start 10 to 14 m from the middle; the rest 16 to 30 m from it, spread around, and all inside the play area. The
    /// hives stand where the scene builder puts them, 12.5 m either side of the middle, and the play area reaches 34 m.
    /// </summary>
    public sealed class NestLayoutTests
    {
        private const float PlayArea = 34f;
        private static readonly Vector3[] Hives = { new Vector3(-12.5f, 0f, 0f), new Vector3(12.5f, 0f, 0f) };

        private NestConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NestConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // Two 12 m ahead of and behind the middle, then six 24 to 26 m out, one every 60 degrees.
        private static Vector3[] Valid()
        {
            return new[]
            {
                new Vector3(0f, 0f, 12f),
                new Vector3(0f, 0f, -12f),
                new Vector3(12f, 0f, 20.8f),
                new Vector3(26f, 0f, 0f),
                new Vector3(12f, 0f, -20.8f),
                new Vector3(-12f, 0f, -20.8f),
                new Vector3(-26f, 0f, 0f),
                new Vector3(-12f, 0f, 20.8f),
            };
        }

        [Test]
        public void Check_ALayoutThatKeepsEveryRule_Passes()
        {
            Assert.That(() => NestLayout.Check(Valid(), Hives, _config, PlayArea), Throws.Nothing);
        }

        [Test]
        public void Check_MissingArguments_Throw()
        {
            Assert.That(() => NestLayout.Check(null, Hives, _config, PlayArea), Throws.ArgumentNullException);
            Assert.That(() => NestLayout.Check(Valid(), null, _config, PlayArea), Throws.ArgumentNullException);
            Assert.That(() => NestLayout.Check(Valid(), Hives, null, PlayArea), Throws.ArgumentNullException);
        }

        [Test]
        public void Check_ANestMissing_Throws()
        {
            Vector3[] seven = new Vector3[7];
            System.Array.Copy(Valid(), seven, 7);

            Assert.That(() => NestLayout.Check(seven, Hives, _config, PlayArea), Throws.ArgumentException);
        }

        [Test]
        public void Check_ANestWithinTwelveMetresOfAHive_ThrowsNamingIt()
        {
            Vector3[] nests = Valid();
            nests[3] = new Vector3(24f, 0f, 0f);

            Assert.That(
                () => NestLayout.Check(nests, Hives, _config, PlayArea),
                Throws.ArgumentException.With.Message.Contains("Nest 3").And.Message.Contains("hive"));
        }

        [Test]
        public void Check_ANestExactlyTwelveMetresFromAHive_Passes()
        {
            Vector3[] nests = Valid();
            nests[3] = new Vector3(24.5f, 0f, 0f);

            Assert.That(() => NestLayout.Check(nests, Hives, _config, PlayArea), Throws.Nothing);
        }

        [TestCase(9.9f)]
        [TestCase(14.1f)]
        public void Check_ANestOpenAtTheStartOutsideTenToFourteenMetres_ThrowsNamingIt(float distance)
        {
            Vector3[] nests = Valid();
            nests[1] = new Vector3(0f, 0f, -distance);

            Assert.That(() => NestLayout.Check(nests, Hives, _config, PlayArea), Throws.ArgumentException.With.Message.Contains("Nest 1"));
        }

        [TestCase(15.9f)]
        [TestCase(30.1f)]
        public void Check_ALaterNestOutsideSixteenToThirtyMetres_ThrowsNamingIt(float distance)
        {
            Vector3[] nests = Valid();
            float bearing = 150f * Mathf.Deg2Rad;
            nests[4] = new Vector3(Mathf.Sin(bearing) * distance, 0f, Mathf.Cos(bearing) * distance);

            Assert.That(() => NestLayout.Check(nests, Hives, _config, PlayArea), Throws.ArgumentException.With.Message.Contains("Nest 4"));
        }

        [Test]
        public void Check_ANestOutsideThePlayArea_ThrowsNamingIt()
        {
            Assert.That(
                () => NestLayout.Check(Valid(), Hives, _config, 25f),
                Throws.ArgumentException.With.Message.Contains("Nest 3").And.Message.Contains("play area"));
        }

        // Six later nests 26 m out, from straight right to straight back: three quarters of the map has none.
        [Test]
        public void Check_LaterNestsBunchedOnOneSide_Throws()
        {
            Vector3[] nests = Valid();
            for (int later = 0; later < 6; later++)
            {
                float bearing = (90f + (18f * later)) * Mathf.Deg2Rad;
                nests[2 + later] = new Vector3(Mathf.Sin(bearing) * 26f, 0f, Mathf.Cos(bearing) * 26f);
            }

            Assert.That(() => NestLayout.Check(nests, Hives, _config, PlayArea), Throws.ArgumentException.With.Message.Contains("spread"));
        }
    }
}
