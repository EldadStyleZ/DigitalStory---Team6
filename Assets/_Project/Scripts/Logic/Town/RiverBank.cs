using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The curved line where the land meets the river.</summary>
    public sealed class RiverBank
    {
        private readonly TownConfig _config;

        public RiverBank(TownConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Returns the x position of the bank at a height.</summary>
        public float EdgeAt(float y)
        {
            float firstWave = _config.FirstWaveSize * Mathf.Sin(y / _config.FirstWaveLength);
            float secondWave = _config.SecondWaveSize * Mathf.Sin((y / _config.SecondWaveLength) + _config.SecondWaveOffset);
            return _config.RiverX + firstWave + secondWave;
        }
    }
}
