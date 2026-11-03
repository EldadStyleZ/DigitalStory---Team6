using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// One song: the keys that play it and what it does to the rats. It is a ScriptableObject, so every song is an
    /// asset in the Songs folder that can be changed in the Inspector. To add a song, use
    /// Assets > Create > Piper Toy > Song, fill it in and drag it into the Flute's Songs list.
    /// </summary>
    [CreateAssetMenu(menuName = "Piper Toy/Song", fileName = "Song")]
    public sealed class Song : ScriptableObject
    {
        [SerializeField, Tooltip("The name on the combos row.")]
        private string _title = "Song";

        [SerializeField, Tooltip("What it does, in a few words.")]
        private string _description = "";

        [SerializeField, Tooltip("The flute keys that play it, in order, from 1 to 6.")]
        private int[] _keys = { 1, 2, 3 };

        [SerializeField, Tooltip("What it does to the rats.")]
        private SongEffect _effect;

        [SerializeField, Min(0f), Tooltip("How far from the Piper a rat can be and still hear it, in units.")]
        private float _radius = 6f;

        [SerializeField, Min(0f), Tooltip("How long the rats keep doing it, in seconds.")]
        private float _seconds = 6f;

        [SerializeField, Min(0f), Tooltip("How long it rests before it can be played again, in seconds.")]
        private float _restSeconds = 10f;

        [SerializeField, Tooltip("The color of its rats and of its glow on the combos row.")]
        private Color _color = new Color(1f, 0.77f, 0.32f);

        [SerializeField, Tooltip("The tune that plays when it is cast.")]
        private AudioClip _sound;

        public string Title => _title;
        public string Description => _description;
        public IReadOnlyList<int> Keys => _keys;
        public SongEffect Effect => _effect;
        public float Radius => _radius;
        public float Seconds => _seconds;
        public float RestSeconds => _restSeconds;
        public Color Color => _color;
        public AudioClip Sound => _sound;
    }
}
