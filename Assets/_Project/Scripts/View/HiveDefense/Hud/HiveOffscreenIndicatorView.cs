using System;
using System.Collections.Generic;
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

        [SerializeField, Min(0f), Tooltip("Panel pixels the arrow's centre keeps from each HUD block: half the arrow's size and a small gap.")]
        private float _clearance = 24f;

        private HiveSet _hives;
        private Camera _camera;
        private VisualElement _layer;
        private IReadOnlyList<VisualElement> _keepOut;
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

        /// <summary>
        /// Builds one arrow per hive in <paramref name="layer"/>, which must cover the whole screen. The arrows keep clear of
        /// the HUD blocks in <paramref name="keepOut"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">A reference is missing.</exception>
        public void Initialize(HiveSet hives, Camera camera, VisualElement layer, IReadOnlyList<VisualElement> keepOut)
        {
            _keepOut = keepOut ?? throw new ArgumentNullException(nameof(keepOut));
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
                    && OffscreenArrow.TryPlace(_camera, _arrows[index], hive.Position, width, height, _edgeMargin, _keepOut, _clearance);
                if (_isShown[index] != wants)
                {
                    _isShown[index] = wants;
                    _arrows[index].EnableInClassList(ShownClass, wants);
                }
            }
        }
    }
}
