using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats float away on the wind, and drown if they land in the river.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Balloons", fileName = "BalloonsSong")]
    public sealed class BalloonsEffect : CrowdSongEffect
    {
        public BalloonsEffect()
            : base(new SongDefinition("Balloons", "Rats swell up and float", 10f, 1, 3, 5))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Shortest flight, in seconds.")]
        public float ShortestSeconds { get; private set; } = 6f;

        [field: SerializeField, Min(0f), Tooltip("Random extra seconds on top of the shortest flight.")]
        public float ExtraSeconds { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("Speed of the wind that carries rats towards the river.")]
        public float WindSpeed { get; private set; } = 22f;

        [field: SerializeField, Min(0f), Tooltip("Range of each rat's own random drift.")]
        public float DriftRange { get; private set; } = 24f;

        [field: SerializeField, Min(0f), Tooltip("Speed of the up and down bob.")]
        public float BobSize { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("How quickly a rat bobs.")]
        public float BobRate { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("Closest a rat floats to the far side of the town.")]
        public float SideMargin { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Closest a rat floats to the top and bottom of the town.")]
        public float EdgeMargin { get; private set; } = 8f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            var trance = new FloatTrance(this);

            foreach (int rat in rats)
            {
                float seconds = ShortestSeconds + ((float)context.Random.NextDouble() * ExtraSeconds);
                var drift = new Vector2(RandomDrift(context.Random), RandomDrift(context.Random));
                context.Swarm.Enchant(rat, trance, seconds, drift: drift);
            }
        }

        private float RandomDrift(IRandomSource random) => ((float)random.NextDouble() - 0.5f) * DriftRange;
    }
}
