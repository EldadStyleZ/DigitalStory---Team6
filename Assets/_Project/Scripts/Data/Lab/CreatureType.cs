using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A creature the Piper can summon.</summary>
    [Serializable]
    public sealed class CreatureType
    {
        [SerializeField, Tooltip("Its name on screen.")]
        private string _name;

        [SerializeField, Min(1), Tooltip("The level that unlocks it.")]
        private int _unlockLevel;

        [SerializeField, Tooltip("The notes that summon it: 0 is H, 1 is J, 2 is K and 3 is L.")]
        private int[] _combo;

        [SerializeField, Min(0f), Tooltip("Stamina spent to summon it.")]
        private float _staminaCost;

        [SerializeField, Min(0f), Tooltip("Seconds before it can be summoned again.")]
        private float _cooldownSeconds;

        [SerializeField, Tooltip("What it can do in a fight.")]
        private FighterStats _stats;

        [SerializeField, Tooltip("How it is drawn.")]
        private FighterLook _look;

        public CreatureType(string name, int unlockLevel, int[] combo, float staminaCost, float cooldownSeconds, FighterStats stats, FighterLook look)
        {
            _name = name;
            _unlockLevel = unlockLevel;
            _combo = combo;
            _staminaCost = staminaCost;
            _cooldownSeconds = cooldownSeconds;
            _stats = stats;
            _look = look;
        }

        public string Name => _name;
        public int UnlockLevel => _unlockLevel;
        public IReadOnlyList<int> Combo => _combo ?? Array.Empty<int>();
        public float StaminaCost => _staminaCost;
        public float CooldownSeconds => _cooldownSeconds;
        public FighterStats Stats => _stats;
        public FighterLook Look => _look;
    }
}
