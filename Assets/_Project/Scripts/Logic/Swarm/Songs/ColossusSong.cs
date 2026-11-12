using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The swarm builds a giant human. It needs a full Fury meter, and uses all of it. The giant stands its
    /// <see cref="Seconds"/>, and each beekeeper it kills adds <see cref="SecondsPerKill"/>, never past
    /// <see cref="MostSeconds"/> (plan section I).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Colossus", fileName = "ColossusSong")]
    public sealed class ColossusSong : SwarmSongEffect
    {
        // L J K J L. No wasps: Fury is its price.
        public ColossusSong()
            : base(new SongDefinition("Colossus", "Needs full Fury: the swarm becomes a giant", 0f, 3, 1, 2, 1, 3), SongCategory.Power, 0, revealedWhenFuryFills: true)
        {
        }

        // 20 s (was 11), plus 2 s a kill and 30 s at most: plan section I, after the owner found the giant too short. The
        // playtest bot had set 11 s after plan section O, when the giant was still the main source of damage, so its
        // balance targets need setting again with these.
        [field: SerializeField, Min(0f), Tooltip("Seconds the giant stands, before the time its kills add.")]
        public float Seconds { get; private set; } = 20f;

        [field: SerializeField, Min(0f), Tooltip("Seconds each beekeeper the giant kills adds to the time it stands.")]
        public float SecondsPerKill { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("The most seconds the giant stands, with the time its kills add. Not below Seconds.")]
        public float MostSeconds { get; private set; } = 30f;

        public override bool CanCast(in SwarmSongContext context)
        {
            return context.Fury.IsFull;
        }

        public override void Cast(in SwarmSongContext context)
        {
            if (context.Swarm.IsCollapsed || !context.Fury.TryUse())
            {
                return;
            }

            context.Forms.StartColossus(Seconds, SecondsPerKill, MostSeconds);
        }
    }
}
