using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One sound effect, made of tones that may overlap.</summary>
    [Serializable]
    public sealed class SoundCue
    {
        [SerializeField, Tooltip("The tones in this sound.")]
        private Tone[] _tones;

        public SoundCue(params Tone[] tones) => _tones = tones;

        public IReadOnlyList<Tone> Tones => _tones ?? Array.Empty<Tone>();
    }
}
