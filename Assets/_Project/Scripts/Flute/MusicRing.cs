using System;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// Shows how far the flute's music reaches while a note is held: a soft glow in the colour of the held hole,
    /// the same colours as the floating notes, with soft waves rolling out from the Piper to its edge. It fades in and
    /// out instead of popping on and off, and sizes itself so its edge sits exactly at the flute's hearing radius.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MusicRing : MonoBehaviour
    {
        [SerializeField, Tooltip("The flute whose music this shows.")]
        private Flute _flute;

        [SerializeField, Tooltip("The floating notes, whose hole colours the glow and the waves share.")]
        private MusicNotes _notes;

        [SerializeField, Tooltip("The wave pictures, used in turn. They are children of this object, so they grow with it.")]
        private SpriteRenderer[] _waves = Array.Empty<SpriteRenderer>();

        [SerializeField, Range(0.1f, 1f), Tooltip("Where the edge is drawn, as a share of the picture's half-width. Tools/toy-art/make_glows.py draws it at 0.92.")]
        private float _edgeShare = 0.92f;

        [SerializeField, Range(0f, 1f), Tooltip("How strong the glow is.")]
        private float _glowStrength = 0.55f;

        [SerializeField, Range(0f, 1f), Tooltip("How strong a wave is as it sets off.")]
        private float _waveStrength = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("How long the glow takes to fade in or out, in seconds.")]
        private float _fadeSeconds = 0.2f;

        [SerializeField, Min(0.1f), Tooltip("How long a wave takes to roll out to the edge, in seconds.")]
        private float _waveSeconds = 1.1f;

        [SerializeField, Min(0.1f), Tooltip("How often a new wave sets off while a note is held, in seconds.")]
        private float _secondsBetweenWaves = 0.55f;

        [SerializeField, Min(0f), Tooltip("How quickly the glow changes to a new hole's colour. Higher is faster.")]
        private float _colorSharpness = 12f;

        private SpriteRenderer _glow;
        private float[] _waveAges;
        private Color[] _waveColors;
        private int _nextWave;
        private float _nextWaveAt;
        private float _shown;
        private Color _color = Color.white;

        /// <summary>How far from the Piper the edge is drawn, in units.</summary>
        public float EdgeRadius => _glow.bounds.extents.x * _edgeShare;

        private void Awake()
        {
            _glow = GetComponent<SpriteRenderer>();
            _waveAges = new float[_waves.Length];
            _waveColors = new Color[_waves.Length];

            for (int index = 0; index < _waves.Length; index++)
            {
                _waveAges[index] = _waveSeconds;
            }

            FitToTheMusic();
        }

        private void OnEnable()
        {
            _flute.NotePlayed += HandleNotePlayed;
        }

        private void OnDisable()
        {
            _flute.NotePlayed -= HandleNotePlayed;
        }

        private void Update()
        {
            FitToTheMusic();

            int key = _flute.SoundingKey;
            _shown = Mathf.MoveTowards(_shown, key != 0 ? 1f : 0f, Time.deltaTime / _fadeSeconds);

            if (key != 0)
            {
                // Each frame the colour moves part of the way to the hole's colour, so a change of note blends in.
                float share = 1f - Mathf.Exp(-_colorSharpness * Time.deltaTime);
                _color = Color.Lerp(_color, _notes.ColorOf(key), share);

                if (Time.time >= _nextWaveAt)
                {
                    SendAWave(_notes.ColorOf(key));
                }
            }

            _glow.color = WithAlpha(_color, _glowStrength * _shown);
            RollTheWaves();
        }

        // A new note always sends a wave at once. Starting from silence, the glow starts in that note's colour.
        private void HandleNotePlayed(int key)
        {
            if (_shown <= 0f)
            {
                _color = _notes.ColorOf(key);
            }

            SendAWave(_notes.ColorOf(key));
        }

        private void SendAWave(Color color)
        {
            if (_waves.Length == 0)
            {
                return;
            }

            _waveAges[_nextWave] = 0f;
            _waveColors[_nextWave] = color;
            _nextWave = (_nextWave + 1) % _waves.Length;
            _nextWaveAt = Time.time + _secondsBetweenWaves;
        }

        // A wave grows quickly at first and slows as it nears the edge, fading as it goes.
        private void RollTheWaves()
        {
            for (int index = 0; index < _waves.Length; index++)
            {
                _waveAges[index] += Time.deltaTime;
                float share = Mathf.Clamp01(_waveAges[index] / _waveSeconds);
                float grown = 1f - ((1f - share) * (1f - share));

                _waves[index].transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1f, grown);
                _waves[index].color = WithAlpha(_waveColors[index], _waveStrength * (1f - share));
            }
        }

        // Scales the glow so its drawn edge is exactly as far out as the rats can hear the flute.
        private void FitToTheMusic()
        {
            float pictureRadius = _glow.sprite.bounds.extents.x * _edgeShare;
            float scale = _flute.HearingRadius / pictureRadius;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
