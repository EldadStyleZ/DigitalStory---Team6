using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One generated house, in town units.</summary>
    public readonly struct House
    {
        public House(float x, float y, float width, float height, int roofStyle, bool hasChimney, Vector2 chimneyOffset)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            RoofStyle = roofStyle;
            HasChimney = hasChimney;
            ChimneyOffset = chimneyOffset;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public int RoofStyle { get; }
        public bool HasChimney { get; }

        /// <summary>Where the chimney sits on the roof, from 0 to 1 on each axis.</summary>
        public Vector2 ChimneyOffset { get; }

        public float Right => X + Width;
        public float Bottom => Y + Height;
    }
}
