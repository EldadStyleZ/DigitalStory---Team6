using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>Which minions and bosses live in the lab's maze, and how deep.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Population Config", fileName = "PopulationConfig")]
    public sealed class PopulationConfig : ScriptableObject
    {
        [SerializeField, Range(0f, 1f), Tooltip("The chance that a cell holds a minion.")]
        private float _chancePerCell = 0.09f;

        [SerializeField, Min(0), Tooltip("How many cells of walking from the entrance stay empty.")]
        private int _safeCells = 8;

        [SerializeField, Tooltip("The ordinary minions.")]
        private MinionType[] _minions =
        {
            new MinionType("Imp", 12f, 6, 0f, 0.4f, new FighterStats(12f, 3.6f, 2f, 1f, 0.5f), new FighterLook(new Color32(0xD8, 0x5C, 0x5C, 0xFF), 0.9f, FighterShape.Rat)),
            new MinionType("Brute", 14f, 16, 0.15f, 0.7f, new FighterStats(40f, 3.2f, 6f, 1.2f, 0.6f), new FighterLook(new Color32(0xB0, 0x3A, 0x48, 0xFF), 1.4f, FighterShape.Cat)),
            new MinionType("Shaman", 16f, 28, 0.4f, 1f, new FighterStats(34f, 3.4f, 6f, 1.5f, 4f, 3f), new FighterLook(new Color32(0xA8, 0x5C, 0xE0, 0xFF), 1.2f, FighterShape.Dot)),
            new MinionType("Ogre", 14f, 55, 0.65f, 1f, new FighterStats(110f, 2.8f, 14f, 1.6f, 1f, 2.5f), new FighterLook(new Color32(0x6A, 0x8F, 0x3A, 0xFF), 2f, FighterShape.Cat)),
        };

        [SerializeField, Tooltip("The bosses from shallowest to deepest. Beating the last one wins the game.")]
        private BossPlacement[] _bosses =
        {
            new BossPlacement(new MinionType("Imp King", 16f, 60, 0f, 0f, new FighterStats(160f, 3.8f, 5f, 0.9f, 0.8f), new FighterLook(new Color32(0xFF, 0x70, 0x70, 0xFF), 2.2f, FighterShape.Rat)), 0.25f),
            new BossPlacement(new MinionType("Iron Brute", 16f, 150, 0f, 0f, new FighterStats(420f, 3.4f, 12f, 1.2f, 1f, 2.5f), new FighterLook(new Color32(0xD0, 0x40, 0x50, 0xFF), 2.8f, FighterShape.Cat)), 0.5f),
            new BossPlacement(new MinionType("Elder Shaman", 18f, 260, 0f, 0f, new FighterStats(650f, 3.4f, 12f, 1.4f, 5f, 3.5f), new FighterLook(new Color32(0xC8, 0x80, 0xFF, 0xFF), 2.6f, FighterShape.Dot)), 0.75f),
            new BossPlacement(new MinionType("Ogre Lord", 18f, 500, 0f, 0f, new FighterStats(1400f, 3f, 22f, 1.5f, 1.4f, 4f), new FighterLook(new Color32(0x90, 0xC0, 0x40, 0xFF), 3.4f, FighterShape.Cat)), 1f),
        };

        /// <summary>The plan the rules follow.</summary>
        public PopulationPlan Plan => new PopulationPlan(_minions, _bosses, _chancePerCell, _safeCells);
    }
}
