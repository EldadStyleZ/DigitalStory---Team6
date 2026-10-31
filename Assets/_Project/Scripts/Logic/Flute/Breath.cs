using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The Piper's breath: playing spends it, and running out silences the flute for a while.</summary>
    public sealed class Breath
    {
        private readonly BreathConfig _config;

        public Breath(BreathConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));

            if (config.RecoveredAt >= config.Max)
            {
                throw new ArgumentException(
                    $"{nameof(BreathConfig.RecoveredAt)} ({config.RecoveredAt}) must be below {nameof(BreathConfig.Max)} ({config.Max}), or the flute never plays again.",
                    nameof(config));
            }

            Current = config.Max;
        }

        public float Current { get; private set; }
        public float Max => _config.Max;
        public bool IsExhausted { get; private set; }

        /// <summary>Advances breath by one step and returns whether the flute sounds.</summary>
        public bool Tick(bool wantsToPlay, float deltaTime)
        {
            if (IsExhausted && Current > _config.RecoveredAt)
            {
                IsExhausted = false;
            }

            if (wantsToPlay && !IsExhausted && Current > 0f)
            {
                return Spend(deltaTime);
            }

            Recover(wantsToPlay, deltaTime);
            return false;
        }

        private bool Spend(float deltaTime)
        {
            Current -= _config.DrainPerSecond * deltaTime;

            if (Current > 0f)
            {
                return true;
            }

            Current = 0f;
            IsExhausted = true;
            return false;
        }

        private void Recover(bool isHoldingHole, float deltaTime)
        {
            float rate = isHoldingHole ? _config.RecoveryWhileHeldPerSecond : _config.RecoveryPerSecond;
            Current = Mathf.Min(_config.Max, Current + (rate * deltaTime));
        }
    }
}
