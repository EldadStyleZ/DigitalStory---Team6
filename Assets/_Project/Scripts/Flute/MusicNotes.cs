using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// Musical notes floating up out of the Piper while a note is held, as in the old game: each note takes the
    /// colour of the hole being played. The Particle System does the floating and fading; this script says when to
    /// make new notes and what colour they are.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class MusicNotes : MonoBehaviour
    {
        [SerializeField, Tooltip("The flute whose notes this shows.")]
        private Flute _flute;

        [SerializeField, Tooltip("The colour of each hole's notes, from hole 1 to hole 6. The old game's colours.")]
        private Color[] _holeColors =
        {
            new Color32(0xF2, 0xB5, 0x44, 0xFF),
            new Color32(0xF2, 0x9A, 0x44, 0xFF),
            new Color32(0x8F, 0xD3, 0x6A, 0xFF),
            new Color32(0x5F, 0xC9, 0xA8, 0xFF),
            new Color32(0x8F, 0xB8, 0xFF, 0xFF),
            new Color32(0xD9, 0xD2, 0xFF, 0xFF),
        };

        private ParticleSystem _particles;

        public Color ColorOf(int key) => _holeColors[key - 1];

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            _particles.Play();
        }

        // The Particle System keeps running; only making new notes stops, so the ones in the air float away.
        private void Update()
        {
            int key = _flute.SoundingKey;
            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = key != 0;

            if (key != 0)
            {
                ParticleSystem.MainModule main = _particles.main;
                main.startColor = ColorOf(key);
            }
        }
    }
}
