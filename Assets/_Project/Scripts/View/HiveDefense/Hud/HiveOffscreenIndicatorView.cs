using System;
using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Puts a glowing alarm beacon at the screen edge for each hive that is under attack and out of sight: a hexagon of light
    /// that pulses fast, shock rings rolling out of it, an arc of its health, and a bright point on the side facing the hive.
    /// It is drawn with UI Toolkit's vector painter, so it needs no texture.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hive Offscreen Indicator View")]
    public sealed class HiveOffscreenIndicatorView : MonoBehaviour
    {
        private const string ArrowClass = "hive-arrow";
        private const string ShownClass = "hive-arrow--shown";
        private const string ArrowText = "";
        private const float MarkerSize = 64f;

        private static readonly Color Alarm = new Color(1f, 0.42f, 0.12f, 1f);
        private static readonly Color Hot = new Color(1f, 0.92f, 0.75f, 1f);
        private static readonly Color HealthLow = new Color(1f, 0.2f, 0.12f, 1f);
        private static readonly Color HealthWhole = new Color(0.35f, 0.95f, 0.4f, 1f);

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

                // The class places it; the look is painted here instead of the plain disc.
                arrow.style.width = MarkerSize;
                arrow.style.height = MarkerSize;
                arrow.style.backgroundColor = Color.clear;
                arrow.style.borderTopLeftRadius = 0f;
                arrow.style.borderTopRightRadius = 0f;
                arrow.style.borderBottomLeftRadius = 0f;
                arrow.style.borderBottomRightRadius = 0f;
                int index = hive;
                arrow.generateVisualContent += context => DrawBeacon(context, index);
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

                if (wants)
                {
                    _arrows[index].MarkDirtyRepaint();
                }
            }
        }

        // Drawn pointing right (+x); OffscreenArrow turns the element toward the hive.
        private void DrawBeacon(MeshGenerationContext context, int index)
        {
            Painter2D painter = context.painter2D;
            var centre = new Vector2(MarkerSize * 0.5f, MarkerSize * 0.5f);
            float time = Time.unscaledTime;
            float beat = 0.5f + (0.5f * Mathf.Sin(time * 9f));

            // Shock rings rolling outward, fading as they grow.
            for (int ring = 0; ring < 2; ring++)
            {
                float t = Mathf.Repeat((time * 1.4f) + (ring * 0.5f), 1f);
                painter.strokeColor = new Color(Alarm.r, Alarm.g, Alarm.b, 0.5f * (1f - t));
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.Arc(centre, 12f + (t * 16f), Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Stroke();
            }

            // Soft halo.
            for (int layer = 3; layer >= 1; layer--)
            {
                painter.fillColor = new Color(Alarm.r, Alarm.g, Alarm.b, (0.05f + (0.05f * beat)) * (4 - layer));
                painter.BeginPath();
                painter.Arc(centre, 7f + (layer * 4.5f), Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            // The hexagon of light, the hive's shape, glowing under a thin hot line.
            for (int layer = 1; layer >= 0; layer--)
            {
                painter.strokeColor = layer == 0 ? Color.Lerp(Alarm, Hot, beat * 0.6f) : new Color(Alarm.r, Alarm.g, Alarm.b, 0.3f);
                painter.lineWidth = layer == 0 ? 2.2f : 6f;
                painter.BeginPath();
                for (int corner = 0; corner < 6; corner++)
                {
                    float angle = (corner * 60f + 30f) * Mathf.Deg2Rad;
                    Vector2 point = centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 11f);
                    if (corner == 0)
                    {
                        painter.MoveTo(point);
                    }
                    else
                    {
                        painter.LineTo(point);
                    }
                }

                painter.ClosePath();
                painter.Stroke();
            }

            // An arc of the hive's health round the hexagon.
            if (_hives != null && index < _hives.Count)
            {
                Hive hive = _hives[index];
                float health = hive.MaxHealth > 0f ? Mathf.Clamp01(hive.Health / hive.MaxHealth) : 0f;
                Color color = Color.Lerp(HealthLow, HealthWhole, health);
                painter.strokeColor = color;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.Arc(centre, 15f, Angle.Degrees(-90f), Angle.Degrees(-90f + (360f * health)));
                painter.Stroke();
            }

            // The hot heart.
            painter.fillColor = Color.Lerp(Alarm, Hot, 0.5f + (0.5f * beat));
            painter.BeginPath();
            painter.Arc(centre, 3.5f + beat, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            // The bright point on the side facing the hive.
            painter.fillColor = Hot;
            painter.BeginPath();
            painter.MoveTo(centre + new Vector2(18f, -6.5f));
            painter.LineTo(centre + new Vector2(27f, 0f));
            painter.LineTo(centre + new Vector2(18f, 6.5f));
            painter.LineTo(centre + new Vector2(20.5f, 0f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
