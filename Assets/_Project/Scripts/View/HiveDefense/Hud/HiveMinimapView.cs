using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// A small glowing round map in the bottom-left corner of the HUD, centred on the core and turned so the camera's forward
    /// is up. It reaches ViewReach metres; anything farther, such as a far nest, sits on the rim pointing its way. It shows
    /// the hives with their health, the open nests glowing gold, the closed ones dim, the beekeepers as red dots and the core
    /// as a bright arrow. It draws itself with UI Toolkit's vector painter and reads the session each frame; it changes
    /// nothing in the logic.
    /// </summary>
    public sealed class HiveMinimapView : IDisposable
    {
        private const float Size = 150f;
        private const float Margin = 22f;
        private const float Padding = 8f;
        private const float GlowWidth = 9f;
        private const float ViewReach = 65f;              // metres from the core to the map's rim

        private static readonly Color Background = new Color(0.02f, 0.04f, 0.07f, 0.6f);
        private static readonly Color InnerShade = new Color(1f, 0.8f, 0.35f, 0.05f);
        private static readonly Color Border = new Color(1f, 0.82f, 0.38f, 1f);
        private static readonly Color AreaColor = new Color(1f, 0.85f, 0.45f, 0.16f);
        private static readonly Color ViewColor = new Color(1f, 0.9f, 0.6f, 0.13f);
        private static readonly Color HiveWhole = new Color(0.35f, 0.95f, 0.4f, 1f);
        private static readonly Color HiveLow = new Color(1f, 0.2f, 0.12f, 1f);
        private static readonly Color HiveFallen = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color NestOpen = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color NestClosed = new Color(0.55f, 0.5f, 0.4f, 0.6f);
        private static readonly Color BeekeeperColor = new Color(1f, 0.25f, 0.2f, 1f);
        private static readonly Color CoreColor = new Color(1f, 1f, 0.85f, 1f);
        private static readonly float[] DotHaloReach = { 3.2f, 2.2f, 1.5f };
        private static readonly float[] DotHaloAlpha = { 0.07f, 0.15f, 0.3f };
        private static readonly float[] CoreHaloReach = { 11f, 8f };
        private static readonly float[] CoreHaloAlpha = { 0.1f, 0.22f };

        private readonly HiveDefenseSession _session;
        private readonly Func<Vector3> _cameraForward;
        private readonly float _playAreaHalfSize;
        private readonly VisualElement _map;

        private Vector2 _forward = Vector2.up;
        private Vector2 _right = Vector2.right;
        private Vector2 _centre;
        private Vector3 _lastCore;
        private bool _hasLastCore;
        private Vector2 _heading = new Vector2(0f, 1f);   // the swarm's way on the ground, in world x and z
        private float _scale;

        /// <param name="session">The match to show.</param>
        /// <param name="hudRoot">The HUD's root element. The map is added to it.</param>
        /// <param name="playAreaHalfSize">The play area's half size, so the whole area fits.</param>
        /// <param name="cameraForward">The camera's way on the ground. The map turns so it points up.</param>
        public HiveMinimapView(HiveDefenseSession session, VisualElement hudRoot, float playAreaHalfSize, Func<Vector3> cameraForward)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _cameraForward = cameraForward ?? throw new ArgumentNullException(nameof(cameraForward));
            _playAreaHalfSize = Mathf.Max(1f, playAreaHalfSize);

            _map = new VisualElement { name = "minimap", pickingMode = PickingMode.Ignore };
            _map.style.position = Position.Absolute;
            _map.style.left = Margin;
            _map.style.bottom = Margin;
            _map.style.width = Size + (GlowWidth * 2f);
            _map.style.height = Size + (GlowWidth * 2f);
            _map.generateVisualContent += Draw;
            (hudRoot ?? throw new ArgumentNullException(nameof(hudRoot))).Add(_map);
        }

        /// <summary>Redraws the map. Call it once a frame.</summary>
        public void Tick()
        {
            Vector3 forward = _cameraForward();
            var flat = new Vector2(forward.x, forward.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                _forward = flat.normalized;
                _right = new Vector2(_forward.y, -_forward.x);
            }

            // The way the swarm is really going, from how the core moved, eased so a jitter does not spin the arrow.
            Vector3 core = _session.CorePosition;
            if (_hasLastCore)
            {
                var step = new Vector2(core.x - _lastCore.x, core.z - _lastCore.z);
                if (step.sqrMagnitude > 0.0004f)
                {
                    _heading = Vector2.Lerp(_heading, step.normalized, 0.25f).normalized;
                }
            }

            _lastCore = core;
            _hasLastCore = true;
            _map.MarkDirtyRepaint();
        }

        public void Dispose()
        {
            _map.generateVisualContent -= Draw;
            _map.RemoveFromHierarchy();
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect rect = _map.contentRect;
            float radius = (Mathf.Min(rect.width, rect.height) * 0.5f) - GlowWidth;
            _centre = rect.center;

            // The map follows the core and reaches ViewReach metres, or the whole area when that is smaller.
            _scale = (radius - Padding) / Mathf.Min(ViewReach, _playAreaHalfSize * 1.42f);
            float breathe = 0.85f + (0.15f * Mathf.Sin(Time.unscaledTime * 1.6f));

            // A soft glow outside the rim: wide faint strokes under a thin bright one.
            for (int layer = 3; layer >= 1; layer--)
            {
                painter.strokeColor = new Color(Border.r, Border.g, Border.b, 0.07f * breathe * (4 - layer));
                painter.lineWidth = layer * 3f;
                painter.BeginPath();
                painter.Arc(_centre, radius + (layer * 1.5f), Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Stroke();
            }

            painter.fillColor = Background;
            painter.BeginPath();
            painter.Arc(_centre, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            // A faint inner ring, so the face reads as a lens rather than a flat disc.
            painter.strokeColor = InnerShade;
            painter.lineWidth = radius * 0.35f;
            painter.BeginPath();
            painter.Arc(_centre, radius * 0.82f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            DrawView(painter, radius);
            DrawPlayArea(painter);
            DrawNests(painter);
            DrawHives(painter);
            DrawBeekeepers(painter);
            DrawCore(painter);

            painter.strokeColor = new Color(Border.r, Border.g, Border.b, 0.9f * breathe);
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.Arc(_centre, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            // A small notch at the top: the way the camera looks.
            painter.fillColor = Border;
            painter.BeginPath();
            painter.MoveTo(_centre + new Vector2(0f, -radius + 1f));
            painter.LineTo(_centre + new Vector2(4f, -radius - 5f));
            painter.LineTo(_centre + new Vector2(-4f, -radius - 5f));
            painter.ClosePath();
            painter.Fill();
        }

        // A faint wedge ahead of the core, the way the swarm is heading, so it and the arrow always agree.
        private void DrawView(Painter2D painter, float radius)
        {
            Vector2 point = Map(_session.CorePosition);
            Vector2 ahead = HeadingOnMap();
            var side = new Vector2(-ahead.y, ahead.x);
            painter.fillColor = ViewColor;
            painter.BeginPath();
            painter.MoveTo(point);
            painter.LineTo(point + (ahead * radius * 0.7f) + (side * radius * 0.45f));
            painter.LineTo(point + (ahead * radius * 0.7f) - (side * radius * 0.45f));
            painter.ClosePath();
            painter.Fill();
        }

        private void DrawPlayArea(Painter2D painter)
        {
            float h = _playAreaHalfSize;
            painter.strokeColor = AreaColor;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(Map(new Vector3(-h, 0f, -h)));
            painter.LineTo(Map(new Vector3(h, 0f, -h)));
            painter.LineTo(Map(new Vector3(h, 0f, h)));
            painter.LineTo(Map(new Vector3(-h, 0f, h)));
            painter.ClosePath();
            painter.Stroke();
        }

        private void DrawNests(Painter2D painter)
        {
            NestField nests = _session.Nests;
            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            for (int index = 0; index < nests.Count; index++)
            {
                Nest nest = nests[index];
                if (nest.IsCollected)
                {
                    continue;
                }

                Vector2 point = Map(nest.Position);
                if (nest.IsOpen)
                {
                    Dot(painter, point, 3f + pulse, NestOpen);
                }
                else
                {
                    Dot(painter, point, 1.6f, NestClosed);
                }
            }
        }

        private void DrawHives(Painter2D painter)
        {
            HiveSet hives = _session.Hives;
            for (int index = 0; index < hives.Count; index++)
            {
                Hive hive = hives[index];
                Vector2 point = Map(hive.Position);
                if (hive.IsDestroyed)
                {
                    Dot(painter, point, 3.5f, HiveFallen);
                    continue;
                }

                float health = hive.MaxHealth > 0f ? Mathf.Clamp01(hive.Health / hive.MaxHealth) : 0f;
                Dot(painter, point, 3.5f, Color.Lerp(HiveLow, HiveWhole, health));

                // A glowing arc round the hive, lit as far as its health reaches: a wide faint stroke under a thin bright one.
                Color color = Color.Lerp(HiveLow, HiveWhole, health);
                for (int layer = 2; layer >= 0; layer--)
                {
                    painter.strokeColor = new Color(color.r, color.g, color.b, layer == 0 ? 0.95f : 0.18f / layer);
                    painter.lineWidth = 1.2f + (layer * 2.5f);
                    painter.BeginPath();
                    painter.Arc(point, 6.5f, Angle.Degrees(-90f), Angle.Degrees(-90f + (360f * health)));
                    painter.Stroke();
                }
            }
        }

        private void DrawBeekeepers(Painter2D painter)
        {
            int count = _session.ActiveBeekeeperCount;
            for (int index = 0; index < count; index++)
            {
                BeekeeperBrain beekeeper = _session.ActiveBeekeeper(index);
                if (beekeeper != null && beekeeper.IsAlive)
                {
                    Dot(painter, Map(beekeeper.Position), 2f, BeekeeperColor);
                }
            }
        }

        // The core is a glowing arrow pointing the way the swarm is heading.
        private void DrawCore(Painter2D painter)
        {
            Vector2 point = Map(_session.CorePosition);
            Vector2 ahead = HeadingOnMap();
            var side = new Vector2(-ahead.y, ahead.x);

            for (int layer = 0; layer < CoreHaloReach.Length; layer++)
            {
                painter.fillColor = new Color(CoreColor.r, CoreColor.g, CoreColor.b, CoreHaloAlpha[layer]);
                painter.BeginPath();
                painter.Arc(point, CoreHaloReach[layer], Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            painter.fillColor = CoreColor;
            painter.BeginPath();
            painter.MoveTo(point + (ahead * 6.5f));
            painter.LineTo(point - (ahead * 4.5f) + (side * 4.5f));
            painter.LineTo(point - (ahead * 2f));
            painter.LineTo(point - (ahead * 4.5f) - (side * 4.5f));
            painter.ClosePath();
            painter.Fill();
        }

        // The swarm's heading turned onto the map, as a unit vector in map pixels (y down).
        private Vector2 HeadingOnMap()
        {
            var ahead = new Vector2(Vector2.Dot(_heading, _right), -Vector2.Dot(_heading, _forward));
            return ahead.sqrMagnitude > 0.0001f ? ahead.normalized : new Vector2(0f, -1f);
        }

        // A world point on the map: turned so the camera's forward is up, and kept inside the circle.
        private Vector2 Map(Vector3 world)
        {
            var flat = new Vector2(world.x - _session.CorePosition.x, world.z - _session.CorePosition.z);
            var local = new Vector2(Vector2.Dot(flat, _right), -Vector2.Dot(flat, _forward)) * _scale;
            float limit = (Mathf.Min(_map.contentRect.width, _map.contentRect.height) * 0.5f) - GlowWidth - 4f;
            if (local.magnitude > limit)
            {
                local = local.normalized * limit;
            }

            return _centre + local;
        }

        // A glowing dot, as everything is in the dark: fading halos, the colour, and a near-white hot centre.
        private static void Dot(Painter2D painter, Vector2 point, float radius, Color color)
        {
            for (int layer = 0; layer < DotHaloReach.Length; layer++)
            {
                painter.fillColor = new Color(color.r, color.g, color.b, color.a * DotHaloAlpha[layer]);
                painter.BeginPath();
                painter.Arc(point, radius * DotHaloReach[layer], Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(point, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            painter.fillColor = Color.Lerp(color, Color.white, 0.65f);
            painter.BeginPath();
            painter.Arc(point, radius * 0.45f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }
    }
}
