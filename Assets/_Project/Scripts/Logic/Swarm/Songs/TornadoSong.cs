using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A spinning column of wasps hurts and throws back every beekeeper near the Piper.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Tornado", fileName = "TornadoSong")]
    public sealed class TornadoSong : SwarmSongEffect
    {
        public TornadoSong()
            : base(new SongDefinition("Tornado", "A column of wasps throws beekeepers back", 10f, 5, 4, 3, 5), SongCategory.Attack)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the column spins.")]
        public float Seconds { get; private set; } = 1.5f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the Piper that the column reaches.")]
        public float Radius { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper in reach.")]
        public float Damage { get; private set; } = 30f;

        [field: SerializeField, Min(0f), Tooltip("Metres per second a beekeeper is thrown away from the Piper.")]
        public float KnockbackSpeed { get; private set; } = 6f;

        public override void Cast(in SwarmSongContext context)
        {
            if (context.Swarm.IsCollapsed)
            {
                return;
            }

            Vector3 piper = context.PiperPosition;
            float reachSquared = Radius * Radius;
            float totalDamage = 0f;

            for (int i = 0; i < context.Enemies.Count; i++)
            {
                ISwarmTarget enemy = context.Enemies[i];

                if (!enemy.IsAlive)
                {
                    continue;
                }

                float dx = enemy.Position.x - piper.x;
                float dz = enemy.Position.z - piper.z;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared > reachSquared)
                {
                    continue;
                }

                enemy.TakeDamage(Damage);
                enemy.Knockback(AwayFrom(dx, dz, distanceSquared) * KnockbackSpeed);
                totalDamage += Damage;
            }

            context.Forms.StartTornado(Seconds, totalDamage);
        }

        private static Vector3 AwayFrom(float dx, float dz, float distanceSquared)
        {
            if (distanceSquared < 1e-8f)
            {
                return Vector3.forward;
            }

            float inverse = 1f / Mathf.Sqrt(distanceSquared);
            return new Vector3(dx * inverse, 0f, dz * inverse);
        }
    }
}
