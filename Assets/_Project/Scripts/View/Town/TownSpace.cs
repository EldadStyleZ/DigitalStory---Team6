using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Converts between town space and the Unity scene.</summary>
    /// <remarks>
    /// Logic keeps the prototype's space, where y grows downward and one unit is one pixel,
    /// so its tuning numbers carry over unchanged. This is the only place that knows the difference.
    /// </remarks>
    public static class TownSpace
    {
        private const float SceneUnitsPerTownUnit = 0.01f;

        public static Vector3 ToScene(Vector2 townPosition)
        {
            return new Vector3(townPosition.x * SceneUnitsPerTownUnit, -townPosition.y * SceneUnitsPerTownUnit, 0f);
        }

        public static Vector2 ToTown(Vector3 scenePosition)
        {
            return new Vector2(scenePosition.x / SceneUnitsPerTownUnit, -scenePosition.y / SceneUnitsPerTownUnit);
        }

        public static float ToSceneLength(float townLength) => townLength * SceneUnitsPerTownUnit;

        /// <summary>Returns the rotation for a town-space angle in radians.</summary>
        public static Quaternion ToSceneRotation(float townAngle)
        {
            return Quaternion.Euler(0f, 0f, -townAngle * Mathf.Rad2Deg);
        }
    }
}
