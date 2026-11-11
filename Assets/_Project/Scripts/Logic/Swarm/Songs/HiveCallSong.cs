using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Wasps stream from every standing hive to the Piper, who stands still and plays. It is never refused: with the swarm
    /// already full it ends on the next tick, so the player still finds it.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Hive Call", fileName = "HiveCallSong")]
    public sealed class HiveCallSong : SwarmSongEffect
    {
        // L J L.
        public HiveCallSong()
            : base(new SongDefinition("Hive Call", "The hives send wasps to the Piper", 3f, 3, 1, 3), SongCategory.Power)
        {
        }

        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartHiveCall();
        }
    }
}
