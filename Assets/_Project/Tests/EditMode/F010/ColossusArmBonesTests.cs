using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Songs-v2 R: which of the baked Colossus bones make each hand and its forearm, the wasps that leave as a fist.</summary>
    public sealed class ColossusArmBonesTests
    {
        // The followed bones of ColossusSampleSet.asset, in its order.
        private static readonly string[] MixamoBones =
        {
            "mixamorig:Hips", "mixamorig:Spine1", "mixamorig:Spine2", "mixamorig:Head",
            "mixamorig:RightShoulder", "mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightHand",
            "mixamorig:RightHandThumb1", "mixamorig:RightHandThumb2", "mixamorig:RightHandThumb3",
            "mixamorig:RightHandIndex1", "mixamorig:RightHandIndex2", "mixamorig:RightHandIndex3",
            "mixamorig:RightHandMiddle1", "mixamorig:RightHandMiddle2", "mixamorig:RightHandMiddle3",
            "mixamorig:RightHandRing1", "mixamorig:RightHandRing2", "mixamorig:RightHandRing3",
            "mixamorig:RightHandPinky1", "mixamorig:RightHandPinky2", "mixamorig:RightHandPinky3",
            "mixamorig:LeftShoulder", "mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand",
            "mixamorig:LeftHandThumb1", "mixamorig:LeftHandThumb2", "mixamorig:LeftHandThumb3",
            "mixamorig:LeftHandIndex1", "mixamorig:LeftHandIndex2", "mixamorig:LeftHandIndex3",
            "mixamorig:LeftHandMiddle1", "mixamorig:LeftHandMiddle2", "mixamorig:LeftHandMiddle3",
            "mixamorig:LeftHandRing1", "mixamorig:LeftHandRing2", "mixamorig:LeftHandRing3",
            "mixamorig:LeftHandPinky1", "mixamorig:LeftHandPinky2", "mixamorig:LeftHandPinky3",
            "mixamorig:RightUpLeg", "mixamorig:RightLeg", "mixamorig:RightFoot", "mixamorig:RightToeBase",
            "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", "mixamorig:LeftFoot", "mixamorig:LeftToeBase",
        };

        [Test]
        public void Find_TheRightHand_IsItsForearmHandAndFingers()
        {
            bool isFound = ColossusArmBones.Find(MixamoBones, ColossusHand.Right, out int first, out int last, out int hand);

            Assert.That(isFound, Is.True);
            Assert.That(first, Is.EqualTo(6), "RightForeArm");
            Assert.That(last, Is.EqualTo(22), "RightHandPinky3");
            Assert.That(hand, Is.EqualTo(7), "RightHand");
        }

        [Test]
        public void Find_TheLeftHand_IsItsForearmHandAndFingers()
        {
            bool isFound = ColossusArmBones.Find(MixamoBones, ColossusHand.Left, out int first, out int last, out int hand);

            Assert.That(isFound, Is.True);
            Assert.That(first, Is.EqualTo(25), "LeftForeArm");
            Assert.That(last, Is.EqualTo(41), "LeftHandPinky3");
            Assert.That(hand, Is.EqualTo(26), "LeftHand");
        }

        [Test]
        public void Find_NamesWithoutThatSidesForearm_FindsNothing()
        {
            string[] legsOnly = { "mixamorig:Hips", "mixamorig:LeftHand", "mixamorig:LeftUpLeg" };

            bool isFound = ColossusArmBones.Find(legsOnly, ColossusHand.Left, out int first, out int last, out int hand);

            Assert.That(isFound, Is.False);
            Assert.That(first, Is.EqualTo(-1));
            Assert.That(last, Is.EqualTo(-1));
            Assert.That(hand, Is.EqualTo(-1));
        }

        [Test]
        public void Find_TheSidesBonesApart_FindsNothing()
        {
            string[] apart = { "mixamorig:RightForeArm", "mixamorig:Head", "mixamorig:RightHand" };

            Assert.That(ColossusArmBones.Find(apart, ColossusHand.Right, out _, out _, out _), Is.False);
        }

        [Test]
        public void Find_NamesWithoutARigPrefix_StillFindsThem()
        {
            string[] plain = { "Hips", "LeftForeArm", "LeftHand", "LeftHandIndex1" };

            bool isFound = ColossusArmBones.Find(plain, ColossusHand.Left, out int first, out int last, out int hand);

            Assert.That(isFound, Is.True);
            Assert.That(first, Is.EqualTo(1));
            Assert.That(last, Is.EqualTo(3));
            Assert.That(hand, Is.EqualTo(2));
        }
    }
}
