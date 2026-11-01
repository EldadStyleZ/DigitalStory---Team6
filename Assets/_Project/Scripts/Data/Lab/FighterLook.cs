using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How a creature or minion is drawn.</summary>
    [Serializable]
    public struct FighterLook
    {
        [SerializeField, Tooltip("The color it is tinted.")]
        private Color _color;

        [SerializeField, Min(0.1f), Tooltip("How long it is drawn, in tiles.")]
        private float _size;

        [SerializeField, Tooltip("Which sprite draws it.")]
        private FighterShape _shape;

        public FighterLook(Color color, float size, FighterShape shape)
        {
            _color = color;
            _size = size;
            _shape = shape;
        }

        public Color Color => _color;
        public float Size => _size;
        public FighterShape Shape => _shape;
    }
}
