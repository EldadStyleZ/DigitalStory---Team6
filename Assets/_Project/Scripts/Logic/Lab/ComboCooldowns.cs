using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Keeps each creature's combo waiting after it is used.</summary>
    public sealed class ComboCooldowns
    {
        private readonly List<CreatureType> _creatures;
        private readonly float[] _secondsLeft;

        public ComboCooldowns(IReadOnlyList<CreatureType> creatures)
        {
            _creatures = new List<CreatureType>(creatures ?? throw new ArgumentNullException(nameof(creatures)));
            _secondsLeft = new float[creatures.Count];
        }

        public bool IsReady(CreatureType creature) => SecondsLeft(creature) <= 0f;

        public float SecondsLeft(CreatureType creature) => _secondsLeft[_creatures.IndexOf(creature)];

        public void Start(CreatureType creature)
        {
            _secondsLeft[_creatures.IndexOf(creature)] = creature.CooldownSeconds;
        }

        public void Tick(float deltaTime)
        {
            for (int index = 0; index < _secondsLeft.Length; index++)
            {
                _secondsLeft[index] = Mathf.Max(0f, _secondsLeft[index] - deltaTime);
            }
        }

        public void Reset() => Array.Clear(_secondsLeft, 0, _secondsLeft.Length);
    }
}
