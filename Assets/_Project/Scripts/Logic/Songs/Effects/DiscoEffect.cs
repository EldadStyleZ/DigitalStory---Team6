using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats breakdance on the spot, in the open where cats can still catch them.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Disco", fileName = "DiscoSong")]
    public sealed class DiscoEffect : CrowdSongEffect
    {
        public DiscoEffect()
            : base(new SongDefinition("Disco", "Breakdancing in the street", 10f, 5, 0, 5))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the rats dance.")]
        public float Seconds { get; private set; } = 7f;

        [field: SerializeField, Min(0f), Tooltip("How quickly a rat spins.")]
        public float SpinRate { get; private set; } = 16f;

        [field: SerializeField, Min(0f), Tooltip("How fast a spinning rat turns.")]
        public float SpinSpeed { get; private set; } = 25f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Share of the spin speed that actually moves the rat.")]
        public float SpinTravel { get; private set; } = .3f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            var trance = new SpinTrance(this);
            context.Stage.StartDisco(Seconds);

            foreach (int rat in rats)
            {
                context.Swarm.Enchant(rat, trance, Seconds);
            }
        }
    }
}
