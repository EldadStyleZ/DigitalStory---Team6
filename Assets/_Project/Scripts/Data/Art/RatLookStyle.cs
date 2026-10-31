using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How one <see cref="RatLook"/> appears on screen.</summary>
    [Serializable]
    public struct RatLookStyle
    {
        [SerializeField, Tooltip("The rat's color. Normal rats use the calm and charmed colors instead.")]
        private Color _tint;

        [SerializeField, Min(0f), Tooltip("The rat's size, where 1 is its usual size.")]
        private float _scale;

        public RatLookStyle(Color tint, float scale)
        {
            _tint = tint;
            _scale = scale;
        }

        public Color Tint => _tint;
        public float Scale => _scale;
    }
}
