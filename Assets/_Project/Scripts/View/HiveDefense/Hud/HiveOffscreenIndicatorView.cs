using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Puts an arrow at the screen edge for each hive that is under attack and out of sight.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hive Offscreen Indicator View")]
    public sealed class HiveOffscreenIndicatorView : MonoBehaviour
    {
        private const string ArrowClass = "hive-arrow";
        private const string ShownClass = "hive-arrow--shown";
        private const string ArrowText = ">";

        [SerializeField, Min(0f), Tooltip("How far in from the screen edge the arrow's centre sits, in panel pixels.")]
        private float _edgeMargin = 48f;

        private HiveSet _hives;
        private Camera _camera;
        private VisualElement _layer;
        private Label[] _arrows;
        private bool[] _isShown;

        /// <summary>How many arrows are showing, for tests and debugging.</summary>
        public int ShownCount
        {
            get
            {
                int count = 0;
                for (int hive = 0; _isShown != null && hive < _isShown.Length; hive++)
                {
                    if (_isShown[hive])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Builds one arrow per hive in <paramref name="layer"/>, which must cover the whole screen.</summary>
        /// <exception cref="ArgumentNullException">A reference is missing.</exception>
        public void Initialize(HiveSet hives, Camera camera, VisualElement layer)
        {
            _hives = hives ?? throw new ArgumentNullException(nameof(hives));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));

            _arrows = new Label[hives.Count];
            _isShown = new bool[hives.Count];
            for (int hive = 0; hive < _arrows.Length; hive++)
            {
                var arrow = new Label(ArrowText);
                arrow.AddToClassList(ArrowClass);
                arrow.pickingMode = PickingMode.Ignore;
                _layer.Add(arrow);
                _arrows[hive] = arrow;
            }
        }

        private void Update()
        {
            if (_hives == null)
            {
                return;
            }

            float width = _layer.resolvedStyle.width;
            float height = _layer.resolvedStyle.height;
            bool hasSize = width > 0f && height > 0f;

            for (int index = 0; index < _arrows.Length; index++)
            {
                Hive hive = _hives[index];
                bool wants = hasSize && !hive.IsDestroyed && hive.IsUnderAttack
                    && TryPlace(_arrows[index], hive.Position, width, height);
                if (_isShown[index] != wants)
                {
                    _isShown[index] = wants;
                    _arrows[index].EnableInClassList(ShownClass, wants);
                }
            }
        }

        // Returns false when the hive is on screen. Otherwise moves the arrow to the edge, pointing at the hive.
        private bool TryPlace(Label arrow, Vector3 world, float width, float height)
        {
            Vector3 view = _camera.WorldToViewportPoint(world);
            bool isInFront = view.z > 0f;
            if (isInFront && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f)
            {
                return false;
            }

            // Panel pixels from the centre, y down. A hive behind the camera points the opposite way.
            float sign = isInFront ? 1f : -1f;
            float dx = (view.x - 0.5f) * width * sign;
            float dy = -(view.y - 0.5f) * height * sign;
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            {
                dy = 1f;
            }

            float halfWidth = Mathf.Max(1f, width * 0.5f - _edgeMargin);
            float halfHeight = Mathf.Max(1f, height * 0.5f - _edgeMargin);
            float scale = Mathf.Min(
                Mathf.Abs(dx) > 0.0001f ? halfWidth / Mathf.Abs(dx) : float.MaxValue,
                Mathf.Abs(dy) > 0.0001f ? halfHeight / Mathf.Abs(dy) : float.MaxValue);

            arrow.style.left = width * 0.5f + dx * scale;
            arrow.style.top = height * 0.5f + dy * scale;
            arrow.style.rotate = new Rotate(new Angle(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, AngleUnit.Degree));
            return true;
        }
    }
}
