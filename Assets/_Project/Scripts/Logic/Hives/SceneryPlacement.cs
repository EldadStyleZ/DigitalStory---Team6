using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Where one prop of the scenery stands in a match's map, and how far it turns, or that the map left it out.</summary>
    public readonly struct SceneryPlacement
    {
        public SceneryPlacement(Vector3 position, float yawDegrees)
        {
            Position = position;
            YawDegrees = yawDegrees;
            IsPlaced = true;
        }

        /// <summary>The prop's pivot on the ground. Meaningless when <see cref="IsPlaced"/> is false.</summary>
        public Vector3 Position { get; }

        /// <summary>Degrees the prop turns about the vertical through its pivot, on top of the way it was built.</summary>
        public float YawDegrees { get; }

        /// <summary>False when the prop found no place that keeps the rules; it stays hidden for the match.</summary>
        public bool IsPlaced { get; }
    }
}
