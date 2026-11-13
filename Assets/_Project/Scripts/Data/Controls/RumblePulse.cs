using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// One buzz of a controller's two motors: how hard each turns, 0 to 1, and for how long. The low motor is the heavy, slow
    /// rumble and the high motor the light, quick one.
    /// </summary>
    [Serializable]
    public struct RumblePulse
    {
        [SerializeField, Range(0f, 1f), Tooltip("The heavy, low-frequency motor's speed, 0 to 1.")]
        private float _low;

        [SerializeField, Range(0f, 1f), Tooltip("The light, high-frequency motor's speed, 0 to 1.")]
        private float _high;

        [SerializeField, Min(0f), Tooltip("Seconds the pulse lasts, in real time.")]
        private float _seconds;

        /// <summary>Makes a pulse. Speeds are kept within 0 to 1 and the length at 0 or more.</summary>
        public RumblePulse(float low, float high, float seconds)
        {
            _low = Mathf.Clamp01(low);
            _high = Mathf.Clamp01(high);
            _seconds = Mathf.Max(0f, seconds);
        }

        /// <summary>The heavy, low-frequency motor's speed, 0 to 1.</summary>
        public float Low => _low;

        /// <summary>The light, high-frequency motor's speed, 0 to 1.</summary>
        public float High => _high;

        /// <summary>Seconds the pulse lasts, in real time.</summary>
        public float Seconds => _seconds;
    }
}
