using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Puts a small warm arrow at the screen edge for each open nest that is out of sight.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Nest Offscreen Indicator View")]
    public sealed class NestOffscreenIndicatorView : MonoBehaviour
    {
        private const string ArrowClass = "nest-arrow";
        private const string ShownClass = "nest-arrow--shown";
        private const string ArrowText = ">";

        [SerializeField, Min(0f), Tooltip("How far in from the screen edge the arrow's centre sits, in panel pixels.")]
        private float _edgeMargin = 72f;

        [SerializeField, Min(0f), Tooltip("Panel pixels the arrow's centre keeps from each HUD block: half the arrow's size and a small gap.")]
        private float _clearance = 17f;

        private NestField _nests;
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
                for (int nest = 0; _isShown != null && nest < _isShown.Length; nest++)
                {
                    if (_isShown[nest])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Builds one arrow per nest in <paramref name="layer"/>, which must cover the whole screen. The arrows keep clear of
        /// the HUD blocks in <paramref name="keepOut"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">A reference is missing.</exception>
        public void Initialize(NestField nests, Camera camera, VisualElement layer, IReadOnlyList<VisualElement> keepOut)
        {
            _keepOut = keepOut ?? throw new ArgumentNullException(nameof(keepOut));
            _nests = nests ?? throw new ArgumentNullException(nameof(nests));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));

            if (_arrows != null)
            {
                for (int old = 0; old < _arrows.Length; old++)
                {
                    _arrows[old].RemoveFromHierarchy();
                }
            }

            _arrows = new Label[nests.Count];
            _isShown = new bool[nests.Count];
            for (int nest = 0; nest < _arrows.Length; nest++)
            {
                var arrow = new Label(ArrowText);
                arrow.AddToClassList(ArrowClass);
                arrow.pickingMode = PickingMode.Ignore;
                _layer.Add(arrow);
                _arrows[nest] = arrow;
            }
        }

        private void Update()
        {
            if (_nests == null)
            {
                return;
            }

            float width = _layer.resolvedStyle.width;
            float height = _layer.resolvedStyle.height;
            bool hasSize = width > 0f && height > 0f;

            for (int index = 0; index < _arrows.Length; index++)
            {
                Nest nest = _nests[index];
                bool wants = hasSize && nest.IsOpen && !nest.IsCollected
                    && OffscreenArrow.TryPlace(_camera, _arrows[index], nest.Position, width, height, _edgeMargin, _keepOut, _clearance);
                if (_isShown[index] != wants)
                {
                    _isShown[index] = wants;
                    _arrows[index].EnableInClassList(ShownClass, wants);
                }
            }
        }
    }
}
