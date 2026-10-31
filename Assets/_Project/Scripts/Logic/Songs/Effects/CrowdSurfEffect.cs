using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm lifts the Piper up, and rats close by stay under its spell.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Crowd Surf", fileName = "CrowdSurfSong")]
    public sealed class CrowdSurfEffect : CrowdSongEffect
    {
        public CrowdSurfEffect()
            : base(new SongDefinition("Crowd Surf", "The swarm lifts you up", 10f, 2, 2, 5))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the Piper is carried.")]
        public float Seconds { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("How close a rat must be to stay fully charmed.")]
        public float CharmZoneRadius { get; private set; } = 220f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            context.Stage.StartSurf(Seconds, CharmZoneRadius);

            foreach (int rat in rats)
            {
                context.Swarm.FullyCharm(rat);
            }
        }
    }
}
