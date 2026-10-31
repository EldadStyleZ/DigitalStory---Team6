using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats form a dance line behind the Piper.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Conga", fileName = "CongaSong")]
    public sealed class CongaEffect : CrowdSongEffect
    {
        public CongaEffect()
            : base(new SongDefinition("Conga", "A dance line behind you", 10f, 0, 1, 0, 1))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the line lasts.")]
        public float Seconds { get; private set; } = 9f;

        [field: SerializeField, Min(1), Tooltip("How many places ahead a rat looks for a dancer to follow.")]
        public int LookAhead { get; private set; } = 8;

        [field: SerializeField, Min(0f), Tooltip("How close to the dancer ahead a rat starts to slow down.")]
        public float StopDistance { get; private set; } = 14f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            var trance = new CongaTrance(context.Swarm, rats, LookAhead, StopDistance);

            for (int slot = 0; slot < rats.Length; slot++)
            {
                context.Swarm.Enchant(rats[slot], trance, Seconds, slot);
            }
        }
    }
}
