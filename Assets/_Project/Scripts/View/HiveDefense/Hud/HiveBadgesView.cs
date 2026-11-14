using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The hive's emblem floating over each hive in the world: the same glowing cell as on its panel, with its number, its
    /// rim the hive's health and its honey the store. It replaces the bars that hung over the hives.
    /// </summary>
    public sealed class HiveBadgesView : IDisposable
    {
        private const float Size = 46f;
        private const float Radius = 15f;
        private const float Height = 3.4f;               // metres above the hive

        private readonly HiveDefenseSession _session;
        private readonly Camera _camera;
        private readonly VisualElement[] _badges;
        private readonly float[] _health;
        private readonly float[] _store;
        private readonly bool[] _alarm;
        private readonly bool[] _fallen;
        private float _blink;

        public HiveBadgesView(HiveDefenseSession session, VisualElement hudRoot, Camera camera)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _camera = camera;
            int count = session.Hives.Count;
            _badges = new VisualElement[count];
            _health = new float[count];
            _store = new float[count];
            _alarm = new bool[count];
            _fallen = new bool[count];
            for (int index = 0; index < count; index++)
            {
                var badge = new VisualElement { pickingMode = PickingMode.Ignore, name = "hive-badge" };
                badge.style.position = Position.Absolute;
                badge.style.width = Size;
                badge.style.height = Size;
                int at = index;
                badge.generateVisualContent += context => HivePanelsView.PaintEmblem(
                    context.painter2D, new Vector2(Size * 0.5f, Size * 0.5f), Radius, _health[at], _store[at], _alarm[at], _fallen[at]);
                badge.Add(HivePanelsView.EmblemNumber(index + 1, 13));
                hudRoot.Add(badge);
                badge.SendToBack();
                _badges[index] = badge;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_camera == null)
            {
                return;
            }

            _blink += deltaTime;
            bool blinkOn = Mathf.Repeat(_blink, 0.5f) < 0.25f;
            for (int index = 0; index < _badges.Length; index++)
            {
                Hive hive = _session.Hives[index];
                VisualElement badge = _badges[index];
                Vector3 screen = _camera.WorldToScreenPoint(hive.Position + (Vector3.up * Height));
                if (screen.z <= 0f || badge.panel == null)
                {
                    badge.style.display = DisplayStyle.None;
                    continue;
                }

                badge.style.display = DisplayStyle.Flex;
                Vector2 point = RuntimePanelUtils.ScreenToPanel(badge.panel, new Vector2(screen.x, Screen.height - screen.y));
                badge.style.left = point.x - (Size * 0.5f);
                badge.style.top = point.y - (Size * 0.5f);

                float health = hive.MaxHealth > 0f ? Mathf.Clamp01(hive.Health / hive.MaxHealth) : 0f;
                float store = hive.StoreCapacity > 0f ? Mathf.Clamp01(hive.Store / hive.StoreCapacity) : 0f;
                bool alarm = hive.IsUnderAttack && !hive.IsDestroyed && blinkOn;
                if (Mathf.Abs(health - _health[index]) > 0.002f || Mathf.Abs(store - _store[index]) > 0.004f
                    || alarm != _alarm[index] || hive.IsDestroyed != _fallen[index])
                {
                    _health[index] = health;
                    _store[index] = store;
                    _alarm[index] = alarm;
                    _fallen[index] = hive.IsDestroyed;
                    badge.MarkDirtyRepaint();
                }
            }
        }

        public void Dispose()
        {
            foreach (VisualElement badge in _badges)
            {
                badge.RemoveFromHierarchy();
            }
        }
    }
}
