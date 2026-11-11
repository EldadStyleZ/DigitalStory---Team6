using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm builds a giant human. It needs a full Fury meter, and uses all of it.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Colossus", fileName = "ColossusSong")]
    public sealed class ColossusSong : SwarmSongEffect
    {
        public ColossusSong()
            : base(new SongDefinition("Colossus", "Needs full Fury: the swarm becomes a giant", 0f, 3, 4, 5, 4, 3), SongCategory.Attack, revealedWhenFuryFills: true)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the giant stands.")]
        public float Seconds { get; private set; } = 12f;

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
