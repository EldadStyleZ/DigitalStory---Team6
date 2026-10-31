using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>What a song is called, how to play it and how long it rests after casting.</summary>
    [Serializable]
    public sealed class SongDefinition
    {
        [SerializeField, Tooltip("The name on the combos row.")]
        private string _title;

        [SerializeField, TextArea, Tooltip("What the song does, for the how-to screen.")]
        private string _description;

        [SerializeField, Min(0f), Tooltip("Seconds before this song can be cast again.")]
        private float _cooldownSeconds;

        [SerializeField, Tooltip("The holes to play in order, counting from 0.")]
        private int[] _keys;

        public SongDefinition(string title, string description, float cooldownSeconds, params int[] keys)
        {
            _title = title;
            _description = description;
            _cooldownSeconds = cooldownSeconds;
            _keys = keys;
        }

        public string Title => _title;
        public string Description => _description;
        public float CooldownSeconds => _cooldownSeconds;
        public IReadOnlyList<int> Keys => _keys;
    }
}
