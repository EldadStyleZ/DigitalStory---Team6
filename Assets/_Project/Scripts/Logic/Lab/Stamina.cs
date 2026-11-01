using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The Piper's stamina: summoning spends it, failed combos lose it, and time brings it back.</summary>
    public sealed class Stamina
    {
        private readonly StaminaConfig _config;

        public Stamina(StaminaConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            Current = config.Max;
        }

        public float Current { get; private set; }
        public float Max => _config.Max;

        /// <summary>Takes the cost if there is enough, and otherwise takes nothing.</summary>
        public bool TrySpend(float cost)
        {
            if (cost > Current)
            {
                return false;
            }

            Current -= cost;
            return true;
        }

        /// <summary>Takes up to this much, stopping at zero.</summary>
        public void Lose(float amount) => Current = Mathf.Max(0f, Current - amount);

        public void Tick(float deltaTime)
        {
            Current = Mathf.Min(_config.Max, Current + (_config.RecoveryPerSecond * deltaTime));
        }

        public void Refill() => Current = _config.Max;
    }
}
