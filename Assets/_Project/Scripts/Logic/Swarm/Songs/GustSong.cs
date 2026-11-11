using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm bursts outward in a ring and blows nearby smoke away.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Gust", fileName = "GustSong")]
    public sealed class GustSong : SwarmSongEffect
    {
        public GustSong()
            : base(new SongDefinition("Gust", "A ring of wasps clears the smoke", 6f, 1, 1, 2))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the burst lasts.")]
        public float Seconds { get; private set; } = 0.8f;

        [field: SerializeField, Min(0f), Tooltip("Metres around the Piper in which smoke is cleared.")]
        public float ClearRadius { get; private set; } = 6f;

        public override void Cast(in SwarmSongContext context)
        {
            if (!context.Forms.StartGust(Seconds))
            {
                return;
            }

            context.Smoke?.ClearWithin(context.PiperPosition, ClearRadius);
        }
    }
}
