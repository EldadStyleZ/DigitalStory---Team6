using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Counts experience and turns it into levels.</summary>
    public sealed class LevelTrack
    {
        private readonly LevelConfig _config;

        public LevelTrack(LevelConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Raised with the new level, once for each level gained.</summary>
        public event Action<int> LevelledUp;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public bool IsAtMaxLevel => Level >= _config.XpToReach.Count;

        /// <summary>How far through this level the Piper is, from 0 to 1.</summary>
        public float Progress
        {
            get
            {
                if (IsAtMaxLevel)
                {
                    return 1f;
                }

                int start = _config.XpToReach[Level - 1];
                int end = _config.XpToReach[Level];
                return Mathf.InverseLerp(start, end, Xp);
            }
        }

        public void Add(int xp)
        {
            Xp += xp;

            while (!IsAtMaxLevel && Xp >= _config.XpToReach[Level])
            {
                Level++;
                LevelledUp?.Invoke(Level);
            }
        }
    }
}
