using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A combo that helps the Piper's creatures, in whatever way its buff says.</summary>
    [Serializable]
    public sealed class BattleSong : IComboSong
    {
        [SerializeField, Tooltip("Its name on screen.")]
        private string _name;

        [SerializeField, Tooltip("What it is for, in a few words.")]
        private string _role;

        [SerializeField, Min(1), Tooltip("The level that unlocks it.")]
        private int _unlockLevel;

        [SerializeField, Tooltip("The notes that play it: 0 is H, 1 is J, 2 is K and 3 is L.")]
        private int[] _combo;

        [SerializeField, Min(0f), Tooltip("Stamina spent to play it.")]
        private float _staminaCost;

        [SerializeField, Min(0f), Tooltip("Seconds before it can be played again.")]
        private float _cooldownSeconds;

        [SerializeField, Tooltip("What it does.")]
        private SongBuff _buff;

        [SerializeField, Tooltip("How its tune is played.")]
        private SongStyle _style;

        public BattleSong(string name, string role, int unlockLevel, int[] combo, float staminaCost, float cooldownSeconds, SongBuff buff, SongStyle style = default)
        {
            _name = name;
            _role = role;
            _unlockLevel = unlockLevel;
            _combo = combo;
            _staminaCost = staminaCost;
            _cooldownSeconds = cooldownSeconds;
            _buff = buff;
            _style = style;
        }

        public string Name => _name;
        public string Role => _role;
        public int UnlockLevel => _unlockLevel;
        public IReadOnlyList<int> Combo => _combo ?? Array.Empty<int>();
        public float StaminaCost => _staminaCost;
        public float CooldownSeconds => _cooldownSeconds;
        public SongBuff Buff => _buff;
        public SongStyle Style => _style;
    }
}
