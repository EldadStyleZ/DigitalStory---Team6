using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Wasps stream from every standing hive to the Piper, who stands still and plays.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Hive Call", fileName = "HiveCallSong")]
    public sealed class HiveCallSong : SwarmSongEffect
    {
        public HiveCallSong()
            : base(new SongDefinition("Hive Call", "The hives send wasps to the Piper", 3f, 0, 5, 0), SongCategory.Power)
        {
        }

        public override bool CanCast(in SwarmSongContext context)
        {
            return context.Hives.StandingCount > 0 && context.Swarm.Count < context.Swarm.Max;
        }

        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartHiveCall();
        }
    }
}
