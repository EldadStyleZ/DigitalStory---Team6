using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>What the player sees when a song is cast.</summary>
    [Serializable]
    public sealed class SongFlourish
    {
        [SerializeField, Tooltip("The color of the ring.")]
        private Color _ringColor;

        [SerializeField, Min(1f), Tooltip("How far the ring reaches, in town units.")]
        private float _ringReach;

        [SerializeField, Min(1f), Tooltip("How fast the ring grows.")]
        private float _ringSpeed;

        public SongFlourish(Color ringColor, float ringReach, float ringSpeed)
        {
            _ringColor = ringColor;
            _ringReach = ringReach;
            _ringSpeed = ringSpeed;
        }

        public Color RingColor => _ringColor;
        public float RingReach => _ringReach;
        public float RingSpeed => _ringSpeed;
    }
}
