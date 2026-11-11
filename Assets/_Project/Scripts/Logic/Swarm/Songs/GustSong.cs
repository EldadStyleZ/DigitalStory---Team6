using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A pulse ripples out through the whole swarm, whatever it is doing, blows nearby smoke away and pushes close beekeepers
    /// back. It takes no slot, so it never ends a running Dome.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Gust", fileName = "GustSong")]
    public sealed class GustSong : SwarmSongEffect
    {
        // H L K.
        public GustSong()
            : base(new SongDefinition("Gust", "A ring of wasps clears the smoke", 6f, 0, 3, 2), SongCategory.Defense)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the pulse ripples through the swarm.")]
        public float Seconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the Piper in which smoke is cleared, and that the pulse reaches.")]
        public float ClearRadius { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the Piper within which beekeepers are pushed out.")]
        public float PushRadius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Metres per second a beekeeper is pushed away from the Piper.")]
        public float PushSpeed { get; private set; } = 4f;

        public override void Cast(in SwarmSongContext context)
        {
            Vector3 piper = context.PiperPosition;

            if (!context.Forms.StartGust(piper, ClearRadius, Seconds))
            {
                return;
            }

            context.Smoke?.ClearWithin(piper, ClearRadius);
            PushAway(context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context)), piper);
        }

        private void PushAway(IReadOnlyList<ISwarmTarget> enemies, Vector3 piper)
        {
            float reachSquared = PushRadius * PushRadius;

            for (int i = 0; i < enemies.Count; i++)
            {
                ISwarmTarget enemy = enemies[i];

                if (enemy.IsAlive && GroundMath.DistanceSquared(enemy.Position, piper) <= reachSquared)
                {
                    enemy.Knockback(GroundMath.Direction(piper, enemy.Position) * PushSpeed);
                }
            }
        }
    }
}
