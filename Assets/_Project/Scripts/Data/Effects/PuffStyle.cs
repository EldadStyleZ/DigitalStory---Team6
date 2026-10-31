using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A small burst of dots.</summary>
    [Serializable]
    public struct PuffStyle
    {
        [SerializeField, Tooltip("The color of the dots.")]
        private Color _color;

        [SerializeField, Min(0), Tooltip("How many dots.")]
        private int _count;

        [SerializeField, Min(0f), Tooltip("The fastest a dot flies, in town units per second.")]
        private float _speed;

        [SerializeField, Min(0f), Tooltip("Seconds a dot lasts.")]
        private float _seconds;

        public PuffStyle(Color color, int count, float speed, float seconds)
        {
            _color = color;
            _count = count;
            _speed = speed;
            _seconds = seconds;
        }

        public Color Color => _color;
        public int Count => _count;
        public float Speed => _speed;
        public float Seconds => _seconds;
    }
}
