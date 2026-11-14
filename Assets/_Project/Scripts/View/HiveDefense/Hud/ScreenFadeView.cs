using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Opens the game from black: a full screen veil over everything that fades away over a few seconds, slow at first
    /// and quicker at the end, like eyes getting used to the night.
    /// </summary>
    public sealed class ScreenFadeView : IDisposable
    {
        private const float HoldSeconds = 0.6f;
        private const float FadeSeconds = 3f;

        private readonly VisualElement _veil;
        private float _elapsed;

        public ScreenFadeView(VisualElement hudRoot)
        {
            _veil = new VisualElement { pickingMode = PickingMode.Ignore, name = "fade-in" };
            _veil.style.position = Position.Absolute;
            _veil.style.left = 0;
            _veil.style.right = 0;
            _veil.style.top = 0;
            _veil.style.bottom = 0;
            _veil.style.backgroundColor = new Color(0.01f, 0.01f, 0.02f, 1f);
            (hudRoot ?? throw new ArgumentNullException(nameof(hudRoot))).Add(_veil);
            _veil.BringToFront();
        }

        public bool IsDone { get; private set; }

        public void Tick(float deltaTime)
        {
            if (IsDone)
            {
                return;
            }

            // The first frames after loading can report a long time (the load itself), which would skip the fade; a frame
            // counts for at most a thirtieth of a second here.
            _elapsed += Mathf.Min(deltaTime, 1f / 30f);
            float t = Mathf.Clamp01((_elapsed - HoldSeconds) / FadeSeconds);
            _veil.style.opacity = 1f - (t * t * (3f - (2f * t)));
            if (t >= 1f)
            {
                IsDone = true;
                _veil.RemoveFromHierarchy();
            }
        }

        public void Dispose()
        {
            _veil.RemoveFromHierarchy();
        }
    }
}
