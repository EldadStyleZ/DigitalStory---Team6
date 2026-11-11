using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How many beekeepers of each kind one wave sends.</summary>
    [Serializable]
    public struct WaveEntry
    {
        [SerializeField, Min(0), Tooltip("Stick beekeepers in this wave.")]
        private int _sticks;

        [SerializeField, Min(0), Tooltip("Smoker beekeepers in this wave.")]
        private int _smokers;

        /// <summary>Makes a wave entry.</summary>
        public WaveEntry(int sticks, int smokers)
        {
            _sticks = sticks;
            _smokers = smokers;
        }

        /// <summary>Stick beekeepers in this wave.</summary>
        public int Sticks => _sticks;

        /// <summary>Smoker beekeepers in this wave.</summary>
        public int Smokers => _smokers;
    }
}
