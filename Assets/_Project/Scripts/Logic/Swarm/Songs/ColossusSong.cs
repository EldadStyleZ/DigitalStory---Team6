using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm builds a giant human. It needs a full Fury meter, and uses all of it.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Colossus", fileName = "ColossusSong")]
    public sealed class ColossusSong : SwarmSongEffect
    {
        // L J K J L. No wasps: Fury is its price.
        public ColossusSong()
            : base(new SongDefinition("Colossus", "Needs full Fury: the swarm becomes a giant", 0f, 3, 1, 2, 1, 3), SongCategory.Power, 0, revealedWhenFuryFills: true)
        {
        }

        // 11 s (was 12), set by the playtest bot after plan section O: the giant was still the main source of damage, so it
        // stands a second less, and waves 4 and 5 grew by a beekeeper each so a player without nests still loses.
        [field: SerializeField, Min(0f), Tooltip("Seconds the giant stands.")]
        public float Seconds { get; private set; } = 11f;

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

            context.Forms.StartColossus(Seconds);
        }
    }
}
