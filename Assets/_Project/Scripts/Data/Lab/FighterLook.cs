using System;
using System.Collections.Generic;
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

        [SerializeField, Tooltip("Which placeholder draws it, when it has no picture of its own.")]
        private FighterShape _shape;

        [SerializeField, Tooltip("Its own picture, seen from above and facing east, standing still.")]
        private Sprite _sprite;

        [SerializeField, Tooltip("Its pictures while walking, in step order, also facing east. With none, it glides.")]
        private Sprite[] _walkFrames;

        public FighterLook(Color color, float size, FighterShape shape, Sprite sprite = null, Sprite[] walkFrames = null)
        {
            _color = color;
            _size = size;
            _shape = shape;
            _sprite = sprite;
            _walkFrames = walkFrames;
        }

        public Color Color => _color;
        public float Size => _size;
        public FighterShape Shape => _shape;
        public Sprite Sprite => _sprite;
        public IReadOnlyList<Sprite> WalkFrames => _walkFrames ?? Array.Empty<Sprite>();
    }
}
