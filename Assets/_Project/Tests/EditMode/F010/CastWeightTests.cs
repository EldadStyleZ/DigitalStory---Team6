using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Plan section Z: a cast's camera punch follows the song's weight, so the quick Sting is the lightest.</summary>
    public sealed class CastWeightTests
    {
        [Test]
        public void PunchShare_TheSting_PunchesAtTheLightShare()
        {
            Assert.That(CastWeight.PunchShare(SwarmForm.Sting, 0.5f), Is.EqualTo(0.5f));
        }

        [TestCase(SwarmForm.Tornado)]
        [TestCase(SwarmForm.Meteor)]
        [TestCase(SwarmForm.Hydra)]
        [TestCase(SwarmForm.Lance)]
        public void PunchShare_EveryOtherSong_PunchesFully(SwarmForm form)
        {
            Assert.That(CastWeight.PunchShare(form, 0.5f), Is.EqualTo(1f));
        }

        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        [TestCase(float.NaN)]
        public void PunchShare_AShareOutsideZeroToOne_FailsLoudly(float share)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CastWeight.PunchShare(SwarmForm.Sting, share));
        }

        // The weight ladder of plan sections Y and Z: the Tornado, the Hydra, the Lance, then the Meteor and the giant's heavy
        // blows, and only a lost hive above them; the heavy ones shake at least a third of the full shake (trauma squared).
        [Test]
        public void FeelConfig_ImpactTrauma_ClimbsWithTheSongsWeight()
        {
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            try
            {
                Assert.That(feel.TornadoTrauma, Is.LessThan(feel.HydraTrauma));
                Assert.That(feel.HydraTrauma, Is.LessThan(feel.LanceTrauma));
                Assert.That(feel.LanceTrauma, Is.LessThan(feel.MeteorTrauma));
                Assert.That(feel.MeteorTrauma, Is.LessThan(feel.GiantHeavyTrauma));
                Assert.That(feel.GiantHeavyTrauma, Is.LessThan(feel.HiveLostTrauma));
                Assert.That(feel.MeteorTrauma * feel.MeteorTrauma, Is.GreaterThanOrEqualTo(1f / 3f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(feel);
            }
        }

        [Test]
        public void FeelConfig_LightCastShare_DefaultsToAHalf()
        {
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            try
            {
                Assert.That(feel.LightCastShare, Is.EqualTo(0.5f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(feel);
            }
        }
    }
}
