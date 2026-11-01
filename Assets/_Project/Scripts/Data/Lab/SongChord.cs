using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One chord under a combo's tune.</summary>
    [Serializable]
    public sealed class SongChord
    {
        [SerializeField, Min(1f), Tooltip("The bass note, in hertz.")]
        private float _bassHertz;

        [SerializeField, Tooltip("The notes played together above the bass, in hertz.")]
        private float[] _hertz;

        public SongChord(float bassHertz, params float[] hertz)
        {
            _bassHertz = bassHertz;
            _hertz = hertz;
        }

        public float BassHertz => _bassHertz;
        public IReadOnlyList<float> Hertz => _hertz ?? Array.Empty<float>();
    }
}
