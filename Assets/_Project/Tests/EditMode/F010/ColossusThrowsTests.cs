using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Songs-v2 W: which wasps each of the giant's moves throws off its body, from which limb, in which shape, and how many,
    /// so the giant never looks hollow and every wasp is back soon.
    /// </summary>
    public sealed class ColossusThrowsTests
    {
        private const string SamplesPath = "Assets/_Project/GameData/F010/ColossusSampleSet.asset";
        private const float MostBodyOut = 0.21f;
        private const float MostBackSeconds = 0.8f;
        private const int SmallSwarm = 800;
        private const int BigSwarm = 4000;

        private ColossusConfig _config;

        [SetUp]
        public void CreateConfig() => _config = ScriptableObject.CreateInstance<ColossusConfig>();

        [TearDown]
        public void DestroyConfig() => Object.DestroyImmediate(_config);

        [TestCase("mixamorig:RightForeArm", ColossusLimb.RightArm)]
        [TestCase("mixamorig:RightHand", ColossusLimb.RightArm)]
        [TestCase("mixamorig:RightHandPinky3", ColossusLimb.RightArm)]
        [TestCase("mixamorig:LeftHandThumb1", ColossusLimb.LeftArm)]
        [TestCase("mixamorig:RightLeg", ColossusLimb.RightLeg)]
        [TestCase("mixamorig:RightFoot", ColossusLimb.RightLeg)]
        [TestCase("mixamorig:LeftToeBase", ColossusLimb.LeftLeg)]
        [TestCase("mixamorig:Head", ColossusLimb.HeadAndChest)]
        [TestCase("mixamorig:Spine2", ColossusLimb.HeadAndChest)]
        [TestCase("Neck", ColossusLimb.HeadAndChest)]
        [TestCase("mixamorig:Hips", ColossusLimb.None)]
        [TestCase("mixamorig:Spine1", ColossusLimb.None)]
        [TestCase("mixamorig:RightArm", ColossusLimb.None)]
        [TestCase("mixamorig:RightShoulder", ColossusLimb.None)]
        [TestCase("mixamorig:LeftUpLeg", ColossusLimb.None)]
        public void LimbOf_ABone_IsItsPart_AndTheTrunkIsNone(string bone, ColossusLimb expected)
        {
            Assert.That(ColossusThrows.LimbOf(bone), Is.EqualTo(expected));
        }

        [TestCase(ColossusAttack.Punch, ColossusHand.Right, ColossusLimb.RightArm)]
        [TestCase(ColossusAttack.Punch, ColossusHand.Left, ColossusLimb.LeftArm)]
        [TestCase(ColossusAttack.HeavyPunch, ColossusHand.Right, ColossusLimb.RightArm)]
        [TestCase(ColossusAttack.Kick, ColossusHand.Right, ColossusLimb.RightLeg)]
        [TestCase(ColossusAttack.Kick, ColossusHand.Left, ColossusLimb.LeftLeg)]
        [TestCase(ColossusAttack.Slam, ColossusHand.Right, ColossusLimb.RightArm | ColossusLimb.LeftArm)]
        [TestCase(ColossusAttack.Roar, ColossusHand.Left, ColossusLimb.HeadAndChest)]
        [TestCase(ColossusAttack.SpinKick, ColossusHand.Right, ColossusLimb.RightLeg | ColossusLimb.LeftLeg)]
        [TestCase(ColossusAttack.LeapSlam, ColossusHand.Left, ColossusLimb.RightArm | ColossusLimb.LeftArm | ColossusLimb.RightLeg | ColossusLimb.LeftLeg)]
        [TestCase(ColossusAttack.None, ColossusHand.Right, ColossusLimb.None)]
        public void LimbsThrownBy_EachMove_IsTheLimbThatStrikes(ColossusAttack attack, ColossusHand side, ColossusLimb expected)
        {
            Assert.That(ColossusThrows.LimbsThrownBy(attack, side), Is.EqualTo(expected));
        }

        [Test]
        public void StrikesWithOneSide_OnlyThePunchesAndTheKick()
        {
            Assert.That(ColossusThrows.StrikesWithOneSide(ColossusAttack.Punch), Is.True);
            Assert.That(ColossusThrows.StrikesWithOneSide(ColossusAttack.HeavyPunch), Is.True);
            Assert.That(ColossusThrows.StrikesWithOneSide(ColossusAttack.Kick), Is.True);
            Assert.That(ColossusThrows.StrikesWithOneSide(ColossusAttack.Slam), Is.False);
            Assert.That(ColossusThrows.StrikesWithOneSide(ColossusAttack.SpinKick), Is.False);
        }

        [Test]
        public void ShapeOf_EveryMove_HasItsOwnShape()
        {
            var shapes = new HashSet<ColossusThrowShape>
            {
                ColossusThrows.ShapeOf(ColossusAttack.Punch),
                ColossusThrows.ShapeOf(ColossusAttack.Kick),
                ColossusThrows.ShapeOf(ColossusAttack.Slam),
                ColossusThrows.ShapeOf(ColossusAttack.Roar),
                ColossusThrows.ShapeOf(ColossusAttack.SpinKick),
                ColossusThrows.ShapeOf(ColossusAttack.LeapSlam),
            };

            Assert.That(shapes.Count, Is.EqualTo(6));
            Assert.That(shapes, Has.No.Member(ColossusThrowShape.None));
            Assert.That(ColossusThrows.ShapeOf(ColossusAttack.HeavyPunch), Is.EqualTo(ColossusThrowShape.Cone));
            Assert.That(ColossusThrows.ShapeOf(ColossusAttack.None), Is.EqualTo(ColossusThrowShape.None));
        }

        [Test]
        public void ThrowOf_EveryMove_IsItsOwnEntryOnTheConfig()
        {
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.Punch), Is.SameAs(_config.PunchThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.HeavyPunch), Is.SameAs(_config.PunchThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.Kick), Is.SameAs(_config.KickThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.Slam), Is.SameAs(_config.SlamThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.Roar), Is.SameAs(_config.RoarThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.SpinKick), Is.SameAs(_config.SpinKickThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.LeapSlam), Is.SameAs(_config.LeapThrow));
            Assert.That(ColossusThrows.ThrowOf(_config, ColossusAttack.None), Is.Null);
        }

        [Test]
        public void WaspsOn_CountsTheWaspsWhoseSampleIsOnTheLimbs_WrappingTheSamples()
        {
            int[] sampleBones = { 0, 1, 2, 1 };
            ColossusLimb[] limbs = { ColossusLimb.None, ColossusLimb.RightArm, ColossusLimb.LeftLeg };

            Assert.That(ColossusThrows.WaspsOn(sampleBones, limbs, 8, ColossusLimb.RightArm), Is.EqualTo(4));
            Assert.That(ColossusThrows.WaspsOn(sampleBones, limbs, 8, ColossusLimb.RightArm | ColossusLimb.LeftLeg), Is.EqualTo(6));
            Assert.That(ColossusThrows.WaspsOn(sampleBones, limbs, 8, ColossusLimb.HeadAndChest), Is.EqualTo(0));
        }

        [Test]
        public void ThrownCount_IsTheShareOfTheWaspsOnTheLimbs()
        {
            Assert.That(ColossusThrows.ThrownCount(46, new ColossusThrow(0.5f, 10f, 10f, 0.6f)), Is.EqualTo(23));
            Assert.That(ColossusThrows.ThrownCount(0, new ColossusThrow(0.5f, 10f, 10f, 0.6f)), Is.EqualTo(0));
            Assert.That(ColossusThrows.ThrownCount(46, null), Is.EqualTo(0));
        }

        // The owner wants the giant never to look hollow: on the baked body, no move throws more than about a fifth of it, and
        // every move's wasps are back within 0.8 s, at the starting swarm and at the biggest.
        [Test]
        public void EveryMove_OnTheBakedBody_ThrowsAtMostAFifth_AndIsBackWithinAMomentAfter([Values(SmallSwarm, BigSwarm)] int wasps)
        {
            var samples = AssetDatabase.LoadAssetAtPath<ColossusSampleSet>(SamplesPath);
            Assert.That(samples, Is.Not.Null);
            var limbs = new ColossusLimb[samples.BoneCount];
            ColossusThrows.FindLimbs(samples.BoneNames, limbs);
            var moves = new[]
            {
                ColossusAttack.Punch, ColossusAttack.Kick, ColossusAttack.Slam, ColossusAttack.Roar, ColossusAttack.SpinKick,
                ColossusAttack.LeapSlam,
            };

            foreach (ColossusAttack move in moves)
            {
                ColossusThrow style = ColossusThrows.ThrowOf(_config, move);
                int onLimbs = ColossusThrows.WaspsOn(samples.BoneIndices, limbs, wasps, ColossusThrows.LimbsThrownBy(move, ColossusHand.Right));
                int thrown = ColossusThrows.ThrownCount(onLimbs, style);
                Assert.That(thrown, Is.GreaterThan(wasps / 50), $"{move} throws too few to see");
                Assert.That(thrown / (float)wasps, Is.LessThanOrEqualTo(MostBodyOut), $"{move} throws too much of the body");
                Assert.That(style.BackSeconds, Is.LessThanOrEqualTo(MostBackSeconds), $"{move}'s wasps are back too late");
            }
        }

        // A look only: the throw's numbers never touch a move's damage or timing.
        [Test]
        public void Defaults_LeaveEveryMovesDamageAndTimingAsTheyWere()
        {
            Assert.That(_config.PunchDamage, Is.EqualTo(45f));
            Assert.That(_config.PunchHitDelay, Is.EqualTo(0.2f));
            Assert.That(_config.SlamHitDelay, Is.EqualTo(0.3f));
            Assert.That(_config.LeapSeconds, Is.EqualTo(0.6f));
            Assert.That(_config.ThrowColor.r, Is.GreaterThan(_config.ThrowColor.g * 4f), "the throw colour is not red");
        }
    }
}
