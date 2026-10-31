using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Collects the lights near the camera into the arrays the darkness shader reads.</summary>
    public sealed class LightField
    {
        /// <summary>Matches MOST_LIGHTS in Darkness.shader.</summary>
        public const int MostLights = 32;

        private Rect _view;

        public Vector4[] Lights { get; } = new Vector4[MostLights];
        public Vector4[] Glows { get; } = new Vector4[MostLights];
        public int Count { get; private set; }

        /// <summary>Starts a new frame for a view of the scene.</summary>
        public void Begin(Rect sceneView)
        {
            _view = sceneView;
            Count = 0;
        }

        /// <summary>Adds a light that clears the darkness. Lights out of view, or past the limit, are left out.</summary>
        /// <param name="scenePosition">Where it is.</param>
        /// <param name="reach">How far it clears the darkness.</param>
        /// <param name="clearing">How much it clears at the centre, from 0 to 1.</param>
        /// <param name="glow">The color it adds, where alpha is strength.</param>
        /// <param name="glowReach">How far the glow reaches, or 0 for none.</param>
        public void Add(Vector2 scenePosition, float reach, float clearing, Color glow = default, float glowReach = 0f)
        {
            if (Count == MostLights || !ReachesTheView(scenePosition, Mathf.Max(reach, glowReach)))
            {
                return;
            }

            Lights[Count] = new Vector4(scenePosition.x, scenePosition.y, reach, clearing);
            Glows[Count] = new Vector4(glow.r * glow.a, glow.g * glow.a, glow.b * glow.a, glowReach);
            Count++;
        }

        private bool ReachesTheView(Vector2 position, float reach)
        {
            return position.x + reach > _view.xMin
                && position.x - reach < _view.xMax
                && position.y + reach > _view.yMin
                && position.y - reach < _view.yMax;
        }
    }
}
