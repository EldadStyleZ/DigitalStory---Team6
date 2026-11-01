using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Where a hit landed, for the view to show.</summary>
    public readonly struct StrikeReport
    {
        public StrikeReport(Vector2 position, float areaRadius, bool isByCreature)
        {
            Position = position;
            AreaRadius = areaRadius;
            IsByCreature = isByCreature;
        }

        public Vector2 Position { get; }

        /// <summary>Tiles around the target the hit covered, or zero for one target.</summary>
        public float AreaRadius { get; }

        public bool IsByCreature { get; }
    }
}
