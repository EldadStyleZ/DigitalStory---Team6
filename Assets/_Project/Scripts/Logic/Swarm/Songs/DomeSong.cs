using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The swarm forms a dome around the Piper, so hits cost far fewer wasps.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Dome", fileName = "DomeSong")]
    public sealed class DomeSong : SwarmSongEffect
    {
        public DomeSong()
            : base(new SongDefinition("Dome", "A dome of wasps shields the Piper", 8f, 0, 1, 0), SongCategory.Defense)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the dome lasts.")]
        public float Seconds { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Metres from the Piper to the dome.")]
        public float Radius { get; private set; } = 2.2f;

        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartDome(Seconds);
        }
    }
}
