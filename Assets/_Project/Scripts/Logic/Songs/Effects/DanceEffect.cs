using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Charms every rat nearby at once.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Dance", fileName = "DanceSong")]
    public sealed class DanceEffect : SongEffect
    {
        public DanceEffect()
            : base(
                new SongDefinition("Dance", "Charms everything nearby", 12f, 0, 2, 4),
                new SongFlourish(new Color32(0xF2, 0xB5, 0x44, 0xFF), 380f, 520f))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("How far the song reaches.")]
        public float Radius { get; private set; } = 380f;

        public override void Cast(in SongContext context)
        {
            context.Swarm.CharmEveryRatNear(context.PiperPosition, Radius);
        }
    }
}
