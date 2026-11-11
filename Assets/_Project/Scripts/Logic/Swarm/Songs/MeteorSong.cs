using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Part of the swarm climbs into the sky and falls on the target like meteors.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Meteor", fileName = "MeteorSong")]
    public sealed class MeteorSong : SwarmSongEffect
    {
        public MeteorSong()
            : base(new SongDefinition("Meteor", "Wasps fall from the sky on the target", 9f, 4, 2, 0), SongCategory.Attack)
        {
        }

        [field: SerializeField, Range(0f, 1f), Tooltip("The share of the swarm that climbs and falls.")]
        public float Fraction { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Metres above the Piper the meteors climb to.")]
        public float Height { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the meteors climb.")]
        public float RiseSeconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the meteors dive. They land when the dive ends.")]
        public float DiveSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps burst outward and flow back into the river.")]
        public float AfterSeconds { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the Piper that the impact can be.")]
        public float MaxRange { get; private set; } = 18f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the impact point that the landing reaches.")]
        public float Radius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach.")]
        public float Damage { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Seconds every beekeeper in reach is stunned.")]
        public float StunSeconds { get; private set; } = 0.5f;

        public override void Cast(in SwarmSongContext context)
        {
            if (context.Swarm.IsCollapsed)
            {
                return;
            }

            bool hasTarget = context.AimTarget != null && context.AimTarget.IsAlive;
            Vector3 point = hasTarget ? context.AimTarget.Position : context.AimPoint;

            context.Forms.StartMeteor(
                SwarmFormMachine.PullIn(point, context.PiperPosition, MaxRange),
                hasTarget ? context.AimTarget : null,
                context.PiperPosition,
                MaxRange,
                Fraction,
                RiseSeconds,
                DiveSeconds,
                AfterSeconds,
                Height,
                context.Enemies,
                Radius,
                Damage,
                StunSeconds);
        }
    }
}
