using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A combo that makes the Piper's creatures hit harder for a while.</summary>
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

        [SerializeField, Min(1f), Tooltip("Multiplies the damage of every creature while it lasts.")]
        private float _damageMultiplier;

        [SerializeField, Min(0f), Tooltip("How long it lasts, in seconds.")]
        private float _seconds;

        [SerializeField, Tooltip("How its tune is played.")]
        private SongStyle _style;

        public BattleSong(string name, string role, int unlockLevel, int[] combo, float staminaCost, float cooldownSeconds, float damageMultiplier, float seconds, SongStyle style = default)
        {
            _name = name;
            _role = role;
            _unlockLevel = unlockLevel;
            _combo = combo;
            _staminaCost = staminaCost;
            _cooldownSeconds = cooldownSeconds;
            _damageMultiplier = damageMultiplier;
            _seconds = seconds;
            _style = style;
        }

        public string Name => _name;
        public string Role => _role;
        public int UnlockLevel => _unlockLevel;
        public IReadOnlyList<int> Combo => _combo ?? Array.Empty<int>();
        public float StaminaCost => _staminaCost;
        public float CooldownSeconds => _cooldownSeconds;
        public float DamageMultiplier => _damageMultiplier;
        public float Seconds => _seconds;
        public SongStyle Style => _style;
    }
}
