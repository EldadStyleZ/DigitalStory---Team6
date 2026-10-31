using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats play dead, which also hides them from cats.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Faint", fileName = "FaintSong")]
    public sealed class FaintEffect : CrowdSongEffect
    {
        public FaintEffect()
            : base(new SongDefinition("Faint", "Everyone plays dead", 10f, 5, 3, 1))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Shortest faint, in seconds.")]
        public float ShortestSeconds { get; private set; } = 4f;

        [field: SerializeField, Min(0f), Tooltip("Random extra seconds on top of the shortest faint.")]
        public float ExtraSeconds { get; private set; } = 1.5f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of its speed a fainting rat keeps each sixtieth of a second.")]
        public float Slowdown { get; private set; } = .8f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            var trance = new FaintTrance(Slowdown);

            foreach (int rat in rats)
            {
                float seconds = ShortestSeconds + ((float)context.Random.NextDouble() * ExtraSeconds);
                context.Swarm.Enchant(rat, trance, seconds);
            }
        }
    }
}
