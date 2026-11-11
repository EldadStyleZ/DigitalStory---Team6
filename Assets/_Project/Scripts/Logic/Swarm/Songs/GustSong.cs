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
    /// <remarks>
    /// The pulse first draws the wasps in, then its ring grows from where the Gust began. Each beekeeper within
    /// <see cref="PushRadius"/> is pushed on the step the ring reaches it, so the push lands where the picture shows it.
    /// The radius it clears and pulses through, and the push's reach, grow with the swarm's level, as it stood at the cast;
    /// the ring still starts at <see cref="RingStartRadius"/>, where the shader starts it.
    /// </remarks>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Gust", fileName = "GustSong")]
    public sealed class GustSong : SwarmSongEffect
    {
        // Not game tuning: the ring has not started while the pulse still draws the wasps in.
        private const float NoRing = -1f;

        // H L K. 20 wasps.
        public GustSong()
            : base(new SongDefinition("Gust", "A ring of wasps clears the smoke", 6f, 0, 3, 2), SongCategory.Defense, 20)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the pulse ripples through the swarm.")]
        public float Seconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the core in which smoke is cleared, and that the pulse reaches.")]
        public float ClearRadius { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the core within which beekeepers are pushed out, each as the ring reaches it. Keep it within ClearRadius, where the ring stops.")]
        public float PushRadius { get; private set; } = 3f;

        [field: SerializeField, Min(0f), Tooltip("Metres per second a beekeeper is pushed away from the core.")]
        public float PushSpeed { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the pulse draws the wasps in before its ring starts. Keep it equal to GustInhaleSeconds in WaspSwarm.compute, so the push lands with the ring.")]
        public float InhaleSeconds { get; private set; } = 0.12f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the core at which the ring starts. Keep it equal to GustStartRadius in WaspSwarm.compute.")]
        public float RingStartRadius { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the ring takes to grow from its start to ClearRadius, slowing as it goes. Keep it equal to GustRingSeconds in WaspSwarm.compute.")]
        public float RingSeconds { get; private set; } = 0.5f;

        public override void Cast(in SwarmSongContext context)
        {
            Vector3 core = context.CorePosition;
            IReadOnlyList<ISwarmTarget> enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));

            if (!context.Forms.StartGust(this, core, enemies, context.SizeScale))
            {
                return;
            }

            context.Smoke?.ClearWithin(core, ClearRadius * context.SizeScale);
        }

        /// <summary>
        /// Metres from where the Gust began that its ring has reached <paramref name="seconds"/> after the cast, or -1 while
        /// the pulse still draws the wasps in. It eases out from <see cref="RingStartRadius"/> to <see cref="ClearRadius"/>,
        /// at the swarm's first level.
        /// </summary>
        public float RingRadius(float seconds)
        {
            return RingRadius(seconds, 1f);
        }

        /// <summary>
        /// As <see cref="RingRadius(float)"/>, for a Gust cast at a level that multiplies its sizes by
        /// <paramref name="sizeScale"/>: the ring eases out from the same start to <see cref="ClearRadius"/> times it.
        /// </summary>
        public float RingRadius(float seconds, float sizeScale)
        {
            if (seconds < InhaleSeconds)
            {
                return NoRing;
            }

            float grown = RingSeconds > 0f ? Mathf.Clamp01((seconds - InhaleSeconds) / RingSeconds) : 1f;
            float eased = 1f - ((1f - grown) * (1f - grown));
            return Mathf.Lerp(RingStartRadius, ClearRadius * sizeScale, eased);
        }

        // Pushes every living beekeeper within the grown PushRadius of the centre that the ring reached between the two
        // times. The ring only grows, so each beekeeper it passes is pushed once.
        internal void PushWhereTheRingPassed(IReadOnlyList<ISwarmTarget> enemies, Vector3 center, float before, float after, float sizeScale)
        {
            float inner = RingRadius(before, sizeScale);
            float outer = Mathf.Min(RingRadius(after, sizeScale), PushRadius * sizeScale);

            if (outer < 0f || outer <= inner)
            {
                return;
            }

            float innerSquared = inner < 0f ? NoRing : inner * inner;
            float outerSquared = outer * outer;

            for (int i = 0; i < enemies.Count; i++)
            {
                ISwarmTarget enemy = enemies[i];

                if (!enemy.IsAlive)
                {
                    continue;
                }

                float squared = GroundMath.DistanceSquared(enemy.Position, center);

                if (squared > innerSquared && squared <= outerSquared)
                {
                    enemy.Knockback(GroundMath.Direction(center, enemy.Position) * PushSpeed);
                }
            }
        }
    }
}
