using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The maths of a controller's sticks: the dead zone, and where the right stick aims on the ground.</summary>
    public static class GamepadStick
    {
        /// <summary>
        /// The stick with a round dead zone taken out: zero while it leans no further than <paramref name="deadZone"/>, then
        /// growing again from zero to 1 at a full lean, the same way it leans. A reading past 1, as a square-gated pad gives in
        /// its corners, counts as 1.
        /// </summary>
        /// <param name="stick">The stick's raw reading, each axis -1 to 1.</param>
        /// <param name="deadZone">How far the stick must lean before it counts, 0 to below 1.</param>
        public static Vector2 DeadZone(Vector2 stick, float deadZone)
        {
            float lean = stick.magnitude;
            float zone = Mathf.Clamp(deadZone, 0f, 0.99f);
            if (!(lean > zone))
            {
                return Vector2.zero;
            }

            float scaled = (Mathf.Min(lean, 1f) - zone) / (1f - zone);
            return stick * (scaled / lean);
        }

        /// <summary>
        /// Where the right stick aims on the ground: the core plus the stick's lean times the reach, turned the camera's way as
        /// the walking keys are (up on the stick is away from the camera), at the core's height.
        /// </summary>
        /// <param name="core">The swarm's core.</param>
        /// <param name="stick">The stick after its dead zone, length 0 to 1.</param>
        /// <param name="cameraForward">The camera's forward; only its way on the ground counts.</param>
        /// <param name="reachMetres">How far a full lean aims from the core.</param>
        public static Vector3 AimPoint(Vector3 core, Vector2 stick, Vector3 cameraForward, float reachMetres)
        {
            return core + (HiveDefenseFrame.GroundStep(stick, cameraForward) * reachMetres);
        }
    }
}
