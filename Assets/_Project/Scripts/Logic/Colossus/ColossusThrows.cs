using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Songs-v2 W: which wasps each of the giant's moves throws off its body, from which limb, in which shape and with which of
    /// <see cref="ColossusConfig"/>'s numbers. A look only: it never changes a move's damage, timing or reach. The punch and the
    /// kick throw from the side that strikes; the View tells which, from where the hands and feet are on the hit.
    /// </summary>
    public static class ColossusThrows
    {
        // Not game tuning: how a Mixamo rig names the bones of each part.
        private const string RightSide = "Right";
        private const string LeftSide = "Left";
        private const string ForearmWord = "ForeArm";
        private const string HandWord = "Hand";
        private const string ShinWord = "Leg";
        private const string FootWord = "Foot";
        private const string ToeWord = "Toe";
        private const string HeadWord = "Head";
        private const string NeckWord = "Neck";
        private const string ChestWord = "Spine2";

        /// <summary>
        /// The part a bone belongs to, by its name: a forearm, hand or finger is that side's arm; a shin, foot or toe that side's
        /// leg; the head, the neck and the upper chest the head and chest. Every other bone (hips, belly, upper arms and thighs)
        /// is none, so the giant keeps its trunk while it throws.
        /// </summary>
        /// <param name="boneName">The bone's name, with or without a rig's prefix ("mixamorig:RightHand").</param>
        public static ColossusLimb LimbOf(string boneName)
        {
            string word = ColossusArmBones.BoneWord(boneName);
            if (word.StartsWith(RightSide, StringComparison.Ordinal))
            {
                return SideLimb(word.Substring(RightSide.Length), ColossusLimb.RightArm, ColossusLimb.RightLeg);
            }

            if (word.StartsWith(LeftSide, StringComparison.Ordinal))
            {
                return SideLimb(word.Substring(LeftSide.Length), ColossusLimb.LeftArm, ColossusLimb.LeftLeg);
            }

            bool isHead = string.Equals(word, HeadWord, StringComparison.Ordinal) || string.Equals(word, NeckWord, StringComparison.Ordinal)
                || string.Equals(word, ChestWord, StringComparison.Ordinal);
            return isHead ? ColossusLimb.HeadAndChest : ColossusLimb.None;
        }

        /// <summary>Fills <paramref name="limbs"/> with the part of each bone in <paramref name="boneNames"/>, by <see cref="LimbOf"/>.</summary>
        /// <returns>How many bones were filled: the fewer of the names and the array's length.</returns>
        /// <exception cref="ArgumentNullException">The names or the array are missing.</exception>
        public static int FindLimbs(IReadOnlyList<string> boneNames, ColossusLimb[] limbs)
        {
            if (boneNames == null)
            {
                throw new ArgumentNullException(nameof(boneNames));
            }

            if (limbs == null)
            {
                throw new ArgumentNullException(nameof(limbs));
            }

            int count = Math.Min(boneNames.Count, limbs.Length);
            for (int bone = 0; bone < count; bone++)
            {
                limbs[bone] = LimbOf(boneNames[bone]);
            }

            return count;
        }

        /// <summary>
        /// The parts a move throws wasps off. A punch throws the striking arm, a kick the striking leg, the slam both arms, the
        /// roar the head and chest, the spin kick both legs and the leap's landing all four limbs.
        /// </summary>
        /// <param name="attack">The move that landed.</param>
        /// <param name="strikingSide">The side whose hand punched or foot kicked; the other moves ignore it.</param>
        public static ColossusLimb LimbsThrownBy(ColossusAttack attack, ColossusHand strikingSide)
        {
            bool isRight = strikingSide == ColossusHand.Right;
            switch (attack)
            {
                case ColossusAttack.Punch:
                case ColossusAttack.HeavyPunch:
                    return isRight ? ColossusLimb.RightArm : ColossusLimb.LeftArm;
                case ColossusAttack.Kick:
                    return isRight ? ColossusLimb.RightLeg : ColossusLimb.LeftLeg;
                case ColossusAttack.Slam:
                    return ColossusLimb.RightArm | ColossusLimb.LeftArm;
                case ColossusAttack.Roar:
                    return ColossusLimb.HeadAndChest;
                case ColossusAttack.SpinKick:
                    return ColossusLimb.RightLeg | ColossusLimb.LeftLeg;
                case ColossusAttack.LeapSlam:
                    return ColossusLimb.RightArm | ColossusLimb.LeftArm | ColossusLimb.RightLeg | ColossusLimb.LeftLeg;
                default:
                    return ColossusLimb.None;
            }
        }

        /// <summary>Whether the move strikes with one side, so the View must tell which hand or foot struck.</summary>
        public static bool StrikesWithOneSide(ColossusAttack attack)
        {
            return attack == ColossusAttack.Punch || attack == ColossusAttack.HeavyPunch || attack == ColossusAttack.Kick;
        }

        /// <summary>
        /// The shape a move's thrown wasps fly in: the punch a cone, the kick a fan, the slam a ring, the roar a spray, the spin
        /// kick a spiral and the leap's landing a crater of two rings.
        /// </summary>
        public static ColossusThrowShape ShapeOf(ColossusAttack attack)
        {
            switch (attack)
            {
                case ColossusAttack.Punch:
                case ColossusAttack.HeavyPunch:
                    return ColossusThrowShape.Cone;
                case ColossusAttack.Kick:
                    return ColossusThrowShape.Fan;
                case ColossusAttack.Slam:
                    return ColossusThrowShape.Ring;
                case ColossusAttack.Roar:
                    return ColossusThrowShape.Spray;
                case ColossusAttack.SpinKick:
                    return ColossusThrowShape.Spiral;
                case ColossusAttack.LeapSlam:
                    return ColossusThrowShape.Crater;
                default:
                    return ColossusThrowShape.None;
            }
        }

        /// <summary>The numbers of the move's throw on <paramref name="config"/>, or null for a move that throws nothing.</summary>
        /// <exception cref="ArgumentNullException">The config is missing.</exception>
        public static ColossusThrow ThrowOf(ColossusConfig config, ColossusAttack attack)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            switch (attack)
            {
                case ColossusAttack.Punch:
                case ColossusAttack.HeavyPunch:
                    return config.PunchThrow;
                case ColossusAttack.Kick:
                    return config.KickThrow;
                case ColossusAttack.Slam:
                    return config.SlamThrow;
                case ColossusAttack.Roar:
                    return config.RoarThrow;
                case ColossusAttack.SpinKick:
                    return config.SpinKickThrow;
                case ColossusAttack.LeapSlam:
                    return config.LeapThrow;
                default:
                    return null;
            }
        }

        /// <summary>
        /// How many of the first <paramref name="waspCount"/> wasps stand on <paramref name="limbs"/>. Wasp i takes sample i,
        /// wrapping when there are fewer samples than wasps, as the swarm places them.
        /// </summary>
        /// <param name="sampleBones">The bone of each sample.</param>
        /// <param name="boneLimbs">The part of each bone, from <see cref="FindLimbs"/>.</param>
        /// <param name="waspCount">The wasps in the swarm.</param>
        /// <param name="limbs">The parts to count.</param>
        /// <exception cref="ArgumentNullException">The samples or the parts are missing.</exception>
        public static int WaspsOn(IReadOnlyList<int> sampleBones, IReadOnlyList<ColossusLimb> boneLimbs, int waspCount, ColossusLimb limbs)
        {
            if (sampleBones == null)
            {
                throw new ArgumentNullException(nameof(sampleBones));
            }

            if (boneLimbs == null)
            {
                throw new ArgumentNullException(nameof(boneLimbs));
            }

            if (sampleBones.Count == 0 || limbs == ColossusLimb.None)
            {
                return 0;
            }

            int count = 0;
            for (int wasp = 0; wasp < waspCount; wasp++)
            {
                int bone = sampleBones[wasp % sampleBones.Count];
                if (bone >= 0 && bone < boneLimbs.Count && (boneLimbs[bone] & limbs) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// About how many wasps a move throws: its share of the wasps on the limbs it throws. The swarm picks them by each wasp's
        /// own seed, so the number on the GPU is near this, not exact.
        /// </summary>
        public static int ThrownCount(int waspsOnLimbs, ColossusThrow style)
        {
            if (style == null || waspsOnLimbs <= 0)
            {
                return 0;
            }

            return (int)Math.Round(waspsOnLimbs * Math.Max(0f, Math.Min(1f, style.Share)), MidpointRounding.AwayFromZero);
        }

        private static ColossusLimb SideLimb(string rest, ColossusLimb arm, ColossusLimb leg)
        {
            if (string.Equals(rest, ForearmWord, StringComparison.Ordinal) || rest.StartsWith(HandWord, StringComparison.Ordinal))
            {
                return arm;
            }

            bool isLeg = string.Equals(rest, ShinWord, StringComparison.Ordinal) || rest.StartsWith(FootWord, StringComparison.Ordinal)
                || rest.StartsWith(ToeWord, StringComparison.Ordinal);
            return isLeg ? leg : ColossusLimb.None;
        }
    }
}
