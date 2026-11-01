using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>Every creature the Piper can summon and every song that helps them. Add a row here to add one, with no new code.</summary>
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
            new CreatureType(
                "Mouse", 1, new[] { H, J }, 10f, 1f,
                new FighterStats(14f, 6.5f, 3f, 0.7f, 0.5f, 0f, new FighterTraits(packBonus: 0.3f)),
                new FighterLook(new Color32(0xC9, 0xBC, 0xA8, 0xFF), 0.8f, FighterShape.Rat),
                "Swarm: stronger together",
                new SongStyle(200f, ToneShape.Triangle, 2f, 0.5f, 3, 0.5f)),
            new CreatureType(
                "Rat", 2, new[] { J, H, K }, 15f, 2f,
                new FighterStats(24f, 6f, 0f, 1f, 4f, 0f, new FighterTraits(healing: 6f)),
                new FighterLook(new Color32(0x9B, 0x7B, 0x5B, 0xFF), 1.1f, FighterShape.Rat),
                "Healer: mends allies and you",
                new SongStyle(120f, ToneShape.Sine, 2f, 1f, 1, 0f)),
            new CreatureType(
                "Cat", 3, new[] { K, L, H, J }, 25f, 4f,
                new FighterStats(36f, 8f, 11f, 0.6f, 0.6f, 0f, new FighterTraits(huntsBackline: true)),
                new FighterLook(new Color32(0xE0, 0x9A, 0x3C, 0xFF), 1.5f, FighterShape.Cat),
                "Hunter: reaches the back line",
                new SongStyle(170f, ToneShape.Square, 1f, 0.5f, 2, 0.8f)),
            new CreatureType(
                "Skunk", 4, new[] { L, K, J, H, L }, 35f, 7f,
                new FighterStats(45f, 5f, 6f, 1.2f, 1.5f, 3.5f, new FighterTraits(slowShare: 0.5f, slowSeconds: 2.5f)),
                new FighterLook(new Color32(0x8E, 0xE0, 0x6B, 0xFF), 1.4f, FighterShape.Cat),
                "Cloud: slows a whole group",
                new SongStyle(100f, ToneShape.Sawtooth, 1f, 0.5f, 1, 0.5f)),
            new CreatureType(
                "Bear", 5, new[] { H, L, K, J, H, L }, 50f, 12f,
                new FighterStats(180f, 5.5f, 8f, 1.2f, 1f, 0f, new FighterTraits(guardTiles: 8f, armour: 0.5f)),
                new FighterLook(new Color32(0x7A, 0x4E, 0x2D, 0xFF), 2.2f, FighterShape.Cat),
                "Guardian: draws every attack",
                new SongStyle(90f, ToneShape.Sawtooth, 0.5f, 0.5f, 1, 1f)),
        };

        [SerializeField, Tooltip("Songs that help the creatures. The same rule about combos applies.")]
        private BattleSong[] _battleSongs =
        {
            new BattleSong(
                "War Song", "Boost: creatures hit 50% harder", 2, new[] { J, K, L }, 30f, 20f, 1.5f, 8f,
                new SongStyle(150f, ToneShape.Square, 1f, 0.5f, 2, 1f)),
        };

        public IReadOnlyList<CreatureType> Creatures => _creatures;
        public IReadOnlyList<BattleSong> BattleSongs => _battleSongs;
    }
}
