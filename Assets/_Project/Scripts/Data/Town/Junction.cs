using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A place where two streets cross.</summary>
    public readonly struct Junction
    {
        public Junction(Vector2 position, int column, int row)
        {
            Position = position;
            Column = column;
            Row = row;
        }

        public Vector2 Position { get; }
        public int Column { get; }
        public int Row { get; }
    }
}
