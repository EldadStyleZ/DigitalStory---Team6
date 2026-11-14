using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Puts a glowing marker at the screen edge for each open nest that is out of sight: a firefly in a breathing ring of
    /// light, two sparks circling it, and a bright point on the side facing the nest. It is drawn with UI Toolkit's vector
    /// painter, so it needs no texture.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Nest Offscreen Indicator View")]
    public sealed class NestOffscreenIndicatorView : MonoBehaviour
    {
        private const string ArrowClass = "nest-arrow";
        private const string ShownClass = "nest-arrow--shown";
        private const string ArrowText = "";
        private const float MarkerSize = 46f;

        private static readonly Color Glow = new Color(1f, 0.82f, 0.32f, 1f);
        private static readonly Color Core = new Color(1f, 0.97f, 0.8f, 1f);

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

                // The class places it; the look is painted here instead of the plain disc.
                arrow.style.width = MarkerSize;
                arrow.style.height = MarkerSize;
                arrow.style.backgroundColor = Color.clear;
                arrow.style.borderTopLeftRadius = 0f;
                arrow.style.borderTopRightRadius = 0f;
                arrow.style.borderBottomLeftRadius = 0f;
                arrow.style.borderBottomRightRadius = 0f;
                float seed = nest * 1.3f;
                arrow.generateVisualContent += context => DrawMarker(context, seed);
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

                if (wants)
                {
                    _arrows[index].MarkDirtyRepaint();
                }
            }
        }

        // Drawn pointing right (+x); OffscreenArrow turns the element toward the nest.
        private static void DrawMarker(MeshGenerationContext context, float seed)
        {
            Painter2D painter = context.painter2D;
            var centre = new Vector2(MarkerSize * 0.5f, MarkerSize * 0.5f);
            float time = Time.unscaledTime;
            float breathe = 0.75f + (0.25f * Mathf.Sin((time * 3f) + seed));

            // Soft halo: a few wide, faint discs.
            for (int layer = 3; layer >= 1; layer--)
            {
                painter.fillColor = new Color(Glow.r, Glow.g, Glow.b, 0.06f * breathe * (4 - layer));
                painter.BeginPath();
                painter.Arc(centre, 6f + (layer * 4.5f), Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            // The ring of light.
            painter.strokeColor = new Color(Glow.r, Glow.g, Glow.b, 0.85f * breathe);
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.Arc(centre, 10f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            // Two sparks circling the ring.
            for (int spark = 0; spark < 2; spark++)
            {
                float angle = (time * 2.2f) + seed + (spark * Mathf.PI);
                Vector2 point = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f);
                painter.fillColor = Core;
                painter.BeginPath();
                painter.Arc(point, 1.6f, Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            // The firefly in the middle.
            painter.fillColor = new Color(Glow.r, Glow.g, Glow.b, 0.45f * breathe);
            painter.BeginPath();
            painter.Arc(centre, 6f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
            painter.fillColor = Core;
            painter.BeginPath();
            painter.Arc(centre, 3f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            // The bright point on the side facing the nest.
            painter.fillColor = new Color(Core.r, Core.g, Core.b, 0.95f);
            painter.BeginPath();
            painter.MoveTo(centre + new Vector2(13f, -5.5f));
            painter.LineTo(centre + new Vector2(21f, 0f));
            painter.LineTo(centre + new Vector2(13f, 5.5f));
            painter.LineTo(centre + new Vector2(15f, 0f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
