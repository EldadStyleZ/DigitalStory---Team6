using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Every standing hive's store streams to the core from wherever the hive stands, while the core stands still. It makes
    /// no wasp from nothing. It is never refused: with the swarm already full or the stores empty it ends on the next tick,
    /// so the player still finds it.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Hive Call", fileName = "HiveCallSong")]
    public sealed class HiveCallSong : SwarmSongEffect
    {
        // L J L. Free: it is how the swarm refills.
        public HiveCallSong()
            : base(new SongDefinition("Hive Call", "The hives send their stores to the core", 3f, 3, 1, 3), SongCategory.Power, 0)
        {
        }

        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartHiveCall();
        }
    }
}
