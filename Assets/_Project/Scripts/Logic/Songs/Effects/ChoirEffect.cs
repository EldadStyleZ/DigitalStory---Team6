using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats are charmed, and charmed rats sing and hop.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Choir", fileName = "ChoirSong")]
    public sealed class ChoirEffect : CrowdSongEffect
    {
        public ChoirEffect()
            : base(new SongDefinition("Choir", "The rats sing", 10f, 0, 1, 2, 3))
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the rats sing.")]
        public float Seconds { get; private set; } = 4.2f;

        protected override void Affect(int[] rats, in SongContext context)
        {
            context.Stage.StartChoir(Seconds);

            foreach (int rat in rats)
            {
                context.Swarm.FullyCharm(rat);
            }
        }
    }
}
