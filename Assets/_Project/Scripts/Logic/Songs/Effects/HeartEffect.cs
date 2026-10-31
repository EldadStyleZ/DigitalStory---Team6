using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats stand in the shape of a heart around the Piper.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Heart", fileName = "HeartSong")]
    public sealed class HeartEffect : CrowdSongEffect
    {
        public HeartEffect()
            : base(new SongDefinition("Heart", "Rats form a heart", 10f, 3, 0, 3))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the rats hold the heart.")]
        public float Seconds { get; private set; } = 7f;

        [field: SerializeField, Min(0f), Tooltip("How much each rat grows the heart.")]
        public float ScalePerRat { get; private set; } = 13f / 88f;

        [field: SerializeField, Min(0f), Tooltip("Smallest size of the heart.")]
        public float SmallestScale { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Largest size of the heart.")]
        public float LargestScale { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("How close to its place a rat starts to slow down.")]
        public float StopDistance { get; private set; } = 6f;

        /// <summary>How big the heart is for this many rats.</summary>
        public float ScaleFor(int ratCount) => Mathf.Clamp(ratCount * ScalePerRat, SmallestScale, LargestScale);

        protected override void Affect(int[] rats, in SongContext context)
        {
            var trance = new HeartTrance(rats.Length, ScaleFor(rats.Length), StopDistance);

            for (int slot = 0; slot < rats.Length; slot++)
            {
                context.Swarm.Enchant(rats[slot], trance, Seconds, slot);
            }
        }
    }
}
