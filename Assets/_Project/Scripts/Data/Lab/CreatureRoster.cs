using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>Every creature the Piper can summon. Add a row here to add a creature, with no new code.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Creature Roster", fileName = "CreatureRoster")]
    public sealed class CreatureRoster : ScriptableObject
    {
        private const int H = 0;
        private const int J = 1;
        private const int K = 2;
        private const int L = 3;

        [SerializeField, Tooltip("Every creature. No combo may be the start of another, or the longer one could never be played.")]
        private CreatureType[] _creatures =
        {
            new CreatureType("Mouse", 1, new[] { H, J }, 10f, 1f, new FighterStats(14f, 6.5f, 3f, 0.7f, 0.5f), new FighterLook(new Color32(0xC9, 0xBC, 0xA8, 0xFF), 0.8f, FighterShape.Rat)),
            new CreatureType("Rat", 2, new[] { J, H, K }, 15f, 2f, new FighterStats(24f, 6f, 5f, 0.8f, 0.5f), new FighterLook(new Color32(0x9B, 0x7B, 0x5B, 0xFF), 1.1f, FighterShape.Rat)),
            new CreatureType("Cat", 3, new[] { K, L, H, J }, 25f, 4f, new FighterStats(40f, 7.5f, 9f, 0.6f, 0.6f), new FighterLook(new Color32(0xE0, 0x9A, 0x3C, 0xFF), 1.5f, FighterShape.Cat)),
            new CreatureType("Skunk", 4, new[] { L, K, J, H, L }, 35f, 7f, new FighterStats(45f, 5f, 6f, 1.2f, 1.5f, 3.5f), new FighterLook(new Color32(0x8E, 0xE0, 0x6B, 0xFF), 1.4f, FighterShape.Cat)),
            new CreatureType("Bear", 5, new[] { H, L, K, J, H, L }, 50f, 12f, new FighterStats(120f, 4.5f, 14f, 1.2f, 1f, 2.8f), new FighterLook(new Color32(0x7A, 0x4E, 0x2D, 0xFF), 2.2f, FighterShape.Cat)),
        };

        public IReadOnlyList<CreatureType> Creatures => _creatures;
    }
}
