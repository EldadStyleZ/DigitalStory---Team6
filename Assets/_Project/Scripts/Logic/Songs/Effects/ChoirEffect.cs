using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Nearby rats are charmed, and charmed rats sing and hop.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Songs/Choir", fileName = "ChoirSong")]
    public sealed class ChoirEffect : CrowdSongEffect
    {
        public ChoirEffect()
            : base(new SongDefinition("Choir", "The rats sing", 10f, 0, 1, 2, 3), SingingRats())
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the rats sing.")]
        public float Seconds { get; private set; } = 4.2f;

        private static SoundCue SingingRats()
        {
            int[] melody = { 12, 16, 19, 24, 19, 16, 21, 19, 17, 16, 14, 12 };
            var tones = new Tone[melody.Length * 2];

            for (int index = 0; index < melody.Length; index++)
            {
                float hertz = 1500f * Mathf.Pow(2f, melody[index] / 12f);
                float delay = index * .17f;
                tones[index * 2] = new Tone(ToneShape.Triangle, hertz * 1.04f, hertz, .15f, .03f, delay);
                tones[(index * 2) + 1] = new Tone(ToneShape.Sine, hertz * 1.5f, hertz * 1.48f, .12f, .012f, delay);
            }

            return new SoundCue(tones);
        }

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
