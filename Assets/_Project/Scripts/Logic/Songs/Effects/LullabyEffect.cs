using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Puts nearby cats to sleep.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Lullaby", fileName = "LullabySong")]
    public sealed class LullabyEffect : SongEffect
    {
        public LullabyEffect()
            : base(new SongDefinition("Lullaby", "Puts cats to sleep", 12f, 2, 1, 0))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("How far the song reaches.")]
        public float Radius { get; private set; } = 520f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the cats sleep.")]
        public float SleepSeconds { get; private set; } = 9f;

        public override void Cast(in SongContext context)
        {
            context.Cats.PutToSleepNear(context.PiperPosition, Radius, SleepSeconds);
        }
    }
}
