using System;
using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Finds which of the Colossus's followed bones make one hand and its forearm, by their names, so the swarm knows which wasps
    /// leave as that hand's fist. A rig names its bones by side (Mixamo: "mixamorig:RightForeArm", "mixamorig:RightHand",
    /// "mixamorig:RightHandIndex1"), and a baked sample set keeps each arm's bones together, from the forearm to the last
    /// finger.
    /// </summary>
    public static class ColossusArmBones
    {
        // Not game tuning: how a Mixamo rig names its arm bones.
        private const string RightSide = "Right";
        private const string LeftSide = "Left";
        private const string ForearmWord = "ForeArm";
        private const string HandWord = "Hand";
        private const char PrefixMark = ':';

        /// <summary>
        /// Finds the hand's bones among <paramref name="boneNames"/>: the forearm, the hand and every finger bone of that side.
        /// </summary>
        /// <param name="boneNames">The followed bones' names, in the sample set's order.</param>
        /// <param name="hand">The side.</param>
        /// <param name="first">The first of the side's forearm, hand and finger bones.</param>
        /// <param name="last">The last of them. Every bone from <paramref name="first"/> to it is one of them.</param>
        /// <param name="handBone">The hand bone itself, where the fist leaves from.</param>
        /// <returns>False, with -1 everywhere, when the names hold no forearm and hand of that side, or they are not together.</returns>
        /// <exception cref="ArgumentNullException">The names are missing.</exception>
        public static bool Find(IReadOnlyList<string> boneNames, ColossusHand hand, out int first, out int last, out int handBone)
        {
            if (boneNames == null)
            {
                throw new ArgumentNullException(nameof(boneNames));
            }

            first = -1;
            last = -1;
            handBone = -1;
            string side = hand == ColossusHand.Right ? RightSide : LeftSide;
            string forearm = side + ForearmWord;
            string handWord = side + HandWord;
            int foundFirst = -1;
            int foundLast = -1;
            int foundHand = -1;
            for (int bone = 0; bone < boneNames.Count; bone++)
            {
                string name = BoneWord(boneNames[bone]);
                bool isForearm = string.Equals(name, forearm, StringComparison.Ordinal);
                bool isHandOrFinger = name.StartsWith(handWord, StringComparison.Ordinal);
                if (!isForearm && !isHandOrFinger)
                {
                    continue;
                }

                // The side's bones must stand together: one apart from the others is not this arm.
                if (foundLast >= 0 && bone != foundLast + 1)
                {
                    return false;
                }

                foundFirst = foundFirst < 0 ? bone : foundFirst;
                foundLast = bone;
                if (string.Equals(name, handWord, StringComparison.Ordinal))
                {
                    foundHand = bone;
                }

                if (isForearm && bone != foundFirst)
                {
                    return false;
                }
            }

            bool hasForearm = foundFirst >= 0 && string.Equals(BoneWord(boneNames[foundFirst]), forearm, StringComparison.Ordinal);
            if (!hasForearm || foundHand < 0)
            {
                return false;
            }

            first = foundFirst;
            last = foundLast;
            handBone = foundHand;
            return true;
        }

        // The name after a rig's prefix: "mixamorig:LeftHand" is "LeftHand".
        private static string BoneWord(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            int colon = name.LastIndexOf(PrefixMark);
            return colon >= 0 ? name.Substring(colon + 1) : name;
        }
    }
}
