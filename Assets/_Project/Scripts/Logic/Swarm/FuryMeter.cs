using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>The Colossus meter: damage dealt by the swarm fills it, and casting the Colossus empties it.</summary>
    public sealed class FuryMeter
    {
        private readonly FuryConfig _config;
        private float _value;

        public FuryMeter(FuryConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (!(config.Max > 0f))
            {
                throw new ArgumentException("FuryConfig.Max must be above zero.", nameof(config));
            }

            if (!(config.DamagePerFuryPoint > 0f))
            {
                throw new ArgumentException("FuryConfig.DamagePerFuryPoint must be above zero.", nameof(config));
            }
        }

        /// <summary>The meter's value, from 0 to Max.</summary>
        public float Value => _value;

        /// <summary>The meter's value as a share of Max, from 0 to 1.</summary>
        public float Fraction => _value / _config.Max;

        /// <summary>True when the meter is at Max.</summary>
        public bool IsFull => _value >= _config.Max;

        /// <summary>Raised when the value changes, with the new fraction.</summary>
        public event Action<float> Changed;

        /// <summary>Adds damage / DamagePerFuryPoint to the meter, capped at Max.</summary>
        public void AddDamage(float damage)
        {
            if (!(damage > 0f) || IsFull)
            {
                return;
            }

            _value = Math.Min(_config.Max, _value + damage / _config.DamagePerFuryPoint);
            Changed?.Invoke(Fraction);
        }

        /// <summary>Empties the meter and returns true, but only when it is full.</summary>
        public bool TryUse()
        {
            if (!IsFull)
            {
                return false;
            }

            _value = 0f;
            Changed?.Invoke(0f);
            return true;
        }
    }
}
