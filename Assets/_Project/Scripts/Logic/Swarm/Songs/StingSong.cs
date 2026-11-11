using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A share of the swarm streams to the aimed beekeeper and stings it.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Sting", fileName = "StingSong")]
    public sealed class StingSong : SwarmSongEffect
    {
        public StingSong()
            : base(new SongDefinition("Sting", "Wasps wrap around the aimed beekeeper", 1.5f, 2, 3, 4), SongCategory.Attack, knownFromStart: true)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the sting lasts, or until the target dies.")]
        public float Seconds { get; private set; } = 4f;

        [field: SerializeField, Range(0f, 1f), Tooltip("The share of the swarm that streams to the target.")]
        public float Fraction { get; private set; } = 0.4f;

        [field: SerializeField, Min(0f), Tooltip("Damage the target takes each second.")]
        public float DamagePerSecond { get; private set; } = 25f;

        public override bool CanCast(in SwarmSongContext context)
        {
            return context.AimTarget != null && context.AimTarget.IsAlive;
        }

        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartSting(context.AimTarget, Seconds, Fraction, DamagePerSecond);
        }
    }
}
