using System;
using System.Globalization;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// One panel per hive, down the left side: its health, its store of wasps (a smaller bar and a number), a mark when it is
    /// not producing, an alarm flash while it is attacked and, when the core is near a damaged hive, a hint to heal it.
    /// </summary>
    /// <remarks>Timers are floats advanced by <see cref="Tick"/>. Text, widths and classes change only when a value changes.</remarks>
    public sealed class HivePanelsView
    {
        private const string FlashClass = "hive-panel--flash";
        private const string DestroyedClass = "hive-panel--destroyed";
        private const string StoppedShownClass = "hive-stopped--shown";
        private const string HintShownClass = "hive-hint--shown";
        private const float BarScale = 1000f;

        // The store is wasps waiting to be taken, in the lesson's words ("take its wasps").
        private const string StoreSuffix = " wasps";

        private const int HintNone = 0;
        private const int HintHold = 1;
        private const int HintHealing = 2;

        private readonly HiveDefenseSession _session;
        private readonly float _flashSeconds;
        private readonly string _holdText;
        private readonly string _healingText;
        private readonly VisualElement[] _panels;
        private readonly VisualElement[] _healthFills;
        private readonly VisualElement[] _storeFills;
        private readonly Label[] _storeLabels;
        private readonly Label[] _stopped;
        private readonly Label[] _hints;
        private readonly int[] _healthShown;
        private readonly int[] _storeBarShown;
        private readonly int[] _storeShown;
        private readonly int[] _hintShown;
        private readonly bool[] _stoppedShown;
        private readonly float[] _flashLeft;
        private readonly bool[] _flashOn;
        private readonly bool[] _destroyedShown;

        // The store's text for each whole number of wasps, made the first time it shows, so a store that flows in or out
        // every frame builds no string.
        private readonly string[] _storeTexts;
        private ControlDevice _device;

        // The emblem: a glowing hexagon cell for each hive. Its rim is the hive's health, running round the cell in a light
        // that turns from green to red, and honey rises inside it as the store fills.
        private const float EmblemSize = 48f;
        private const float EmblemRadius = 17f;
        private static readonly Color HealthWhole = new Color(0.4f, 1f, 0.45f, 1f);
        private static readonly Color HealthLow = new Color(1f, 0.25f, 0.15f, 1f);
        private static readonly Color Honey = new Color(1f, 0.72f, 0.22f, 1f);
        private static readonly Color CellDark = new Color(0.04f, 0.05f, 0.03f, 0.85f);
        private readonly VisualElement[] _emblems;
        private readonly float[] _health01;
        private readonly float[] _store01;

        /// <summary>Builds the panels into the column, one for each hive of the match.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        public HivePanelsView(HiveDefenseSession session, VisualElement column, float flashSeconds, string holdText, string healingText)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            _flashSeconds = flashSeconds;
            _holdText = holdText ?? string.Empty;
            _healingText = healingText ?? string.Empty;

            int count = session.Hives.Count;
            _panels = new VisualElement[count];
            _emblems = new VisualElement[count];
            _health01 = new float[count];
            _store01 = new float[count];
            _healthFills = new VisualElement[count];
            _storeFills = new VisualElement[count];
            _storeLabels = new Label[count];
            _stopped = new Label[count];
            _hints = new Label[count];
            _healthShown = new int[count];
            _storeBarShown = new int[count];
            _storeShown = new int[count];
            _hintShown = new int[count];
            _stoppedShown = new bool[count];
            _flashLeft = new float[count];
            _flashOn = new bool[count];
            _destroyedShown = new bool[count];

            int mostStored = 0;
            for (int index = 0; index < count; index++)
            {
                mostStored = Mathf.Max(mostStored, Mathf.CeilToInt(session.Hives[index].StoreCapacity));
            }

            _storeTexts = new string[mostStored + 1];

            for (int index = 0; index < count; index++)
            {
                _healthShown[index] = -1;
                _storeBarShown[index] = -1;
                _storeShown[index] = -1;
                Build(column, index);
            }
        }

        /// <summary>Names the heal key for the device in use (E, or RB on a controller), from the next Tick.</summary>
        public void ShowControls(ControlDevice device)
        {
            if (device == _device)
            {
                return;
            }

            _device = device;
            for (int index = 0; index < _hintShown.Length; index++)
            {
                _hintShown[index] = -1; // Written again on the next Tick.
            }
        }

        /// <summary>Updates the panels. Call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            HiveSet hives = _session.Hives;
            for (int index = 0; index < _panels.Length; index++)
            {
                Hive hive = hives[index];
                ShowBars(index, hive);
                ShowState(index, hive);
                ShowFlash(index, hive, deltaTime);
                ShowHint(index, hive);
            }
        }

        private void Build(VisualElement column, int index)
        {
            var panel = new VisualElement();
            panel.AddToClassList("hive-panel");
            panel.pickingMode = PickingMode.Ignore;

            var emblem = new VisualElement { pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("hive-emblem");
            emblem.style.width = EmblemSize;
            emblem.style.height = EmblemSize;
            emblem.style.flexShrink = 0;
            int at = index;
            emblem.generateVisualContent += context => DrawEmblem(context, at);
            emblem.Add(EmblemNumber(index + 1, 15));
            panel.Add(emblem);
            _emblems[index] = emblem;

            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("hive-body");
            body.style.flexGrow = 1;
            panel.Add(body);

            var row = new VisualElement();
            row.AddToClassList("row");
            row.pickingMode = PickingMode.Ignore;
            var title = new Label("Hive " + (index + 1).ToString(CultureInfo.InvariantCulture));
            title.AddToClassList("hive-title");
            title.pickingMode = PickingMode.Ignore;
            row.Add(title);
            var stopped = new Label("not producing");
            stopped.AddToClassList("hive-stopped");
            stopped.pickingMode = PickingMode.Ignore;
            row.Add(stopped);
            var storeLabel = new Label(string.Empty);
            storeLabel.AddToClassList("hive-store-label");
            storeLabel.pickingMode = PickingMode.Ignore;
            row.Add(storeLabel);
            body.Add(row);

            body.Add(Track("hive-fill", "bar-track", out VisualElement healthFill));
            body.Add(Track("hive-store-fill", "bar-track--thin", out VisualElement storeFill));

            var hint = new Label(string.Empty);
            hint.AddToClassList("hive-hint");
            hint.pickingMode = PickingMode.Ignore;
            body.Add(hint);

            column.Add(panel);
            _panels[index] = panel;
            _healthFills[index] = healthFill;
            _storeFills[index] = storeFill;
            _storeLabels[index] = storeLabel;
            _stopped[index] = stopped;
            _hints[index] = hint;
        }

        private static VisualElement Track(string fillClass, string trackClass, out VisualElement fill)
        {
            var track = new VisualElement();
            track.AddToClassList("bar-track");
            track.AddToClassList(trackClass);
            track.pickingMode = PickingMode.Ignore;
            fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.AddToClassList(fillClass);
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);
            return track;
        }

        private void ShowBars(int index, Hive hive)
        {
            int health = BarValue(hive.MaxHealth > 0f ? hive.Health / hive.MaxHealth : 0f);
            float store01 = hive.StoreCapacity > 0f ? Mathf.Clamp01(hive.Store / hive.StoreCapacity) : 0f;
            if (health != _healthShown[index] || Mathf.Abs(store01 - _store01[index]) > 0.004f)
            {
                _health01[index] = health / BarScale;
                _store01[index] = store01;
                _emblems[index].MarkDirtyRepaint();
            }

            if (health != _healthShown[index])
            {
                _healthShown[index] = health;
                _healthFills[index].style.width = Length.Percent(health / BarScale * 100f);
            }

            int storeBar = BarValue(hive.StoreCapacity > 0f ? hive.Store / hive.StoreCapacity : 0f);
            if (storeBar != _storeBarShown[index])
            {
                _storeBarShown[index] = storeBar;
                _storeFills[index].style.width = Length.Percent(storeBar / BarScale * 100f);
            }

            int store = Mathf.FloorToInt(hive.Store);
            if (store != _storeShown[index])
            {
                _storeShown[index] = store;
                _storeLabels[index].text = StoreText(store);
            }
        }

        private void ShowState(int index, Hive hive)
        {
            if (hive.IsDestroyed != _destroyedShown[index])
            {
                _destroyedShown[index] = hive.IsDestroyed;
                _panels[index].EnableInClassList(DestroyedClass, hive.IsDestroyed);
            }

            bool isStopped = !hive.IsDestroyed && !hive.IsProducing;
            if (isStopped != _stoppedShown[index])
            {
                _stoppedShown[index] = isStopped;
                _stopped[index].EnableInClassList(StoppedShownClass, isStopped);
            }
        }

        // The alarm colour for one period, normal for the next, for as long as the attack lasts.
        private void ShowFlash(int index, Hive hive, float deltaTime)
        {
            bool isAttacked = hive.IsUnderAttack && !hive.IsDestroyed;
            bool flashOn = false;
            if (isAttacked)
            {
                _flashLeft[index] -= deltaTime;
                flashOn = _flashOn[index];
                if (_flashLeft[index] <= 0f)
                {
                    _flashLeft[index] = _flashSeconds;
                    flashOn = !flashOn;
                }
            }
            else
            {
                _flashLeft[index] = 0f;
            }

            if (flashOn != _flashOn[index])
            {
                _flashOn[index] = flashOn;
                _panels[index].EnableInClassList(FlashClass, flashOn);
                _emblems[index].MarkDirtyRepaint();
            }
        }

        // Only for a damaged hive the core is near: hold E to heal it, or "Healing" while the wasps flow.
        private void ShowHint(int index, Hive hive)
        {
            int hint = HintNone;
            if (_session.NearHive == index && !hive.IsDestroyed && hive.Health < hive.MaxHealth)
            {
                hint = _session.IsGiving ? HintHealing : HintHold;
            }

            if (hint == _hintShown[index])
            {
                return;
            }

            _hintShown[index] = hint;
            Label label = _hints[index];
            ControlSymbols.ShowPrompt(label, hint == HintHealing ? _healingText : _holdText, _device);
            label.EnableInClassList(HintShownClass, hint != HintNone);
        }

        private string StoreText(int store)
        {
            if (store < 0 || store >= _storeTexts.Length)
            {
                return store.ToString(CultureInfo.InvariantCulture) + StoreSuffix;
            }

            if (_storeTexts[store] == null)
            {
                _storeTexts[store] = store.ToString(CultureInfo.InvariantCulture) + StoreSuffix;
            }

            return _storeTexts[store];
        }

        /// <summary>The hive's number, in the middle of its emblem.</summary>
        public static Label EmblemNumber(int number, int fontSize)
        {
            var label = new Label(number.ToString(CultureInfo.InvariantCulture)) { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.right = 0;
            label.style.top = 0;
            label.style.bottom = 0;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;
            label.style.paddingLeft = 0;
            label.style.paddingRight = 0;
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = fontSize;
            label.style.color = new Color(0.22f, 0.12f, 0.03f, 1f);
            return label;
        }

        private void DrawEmblem(MeshGenerationContext context, int index)
        {
            PaintEmblem(context.painter2D, new Vector2(EmblemSize * 0.5f, EmblemSize * 0.5f), EmblemRadius, _health01[index], _store01[index], _flashOn[index], _destroyedShown[index]);
        }

        /// <summary>Paints a hive's emblem: a glowing cell whose rim is its health and whose honey is its store.</summary>
        public static void PaintEmblem(Painter2D paint, Vector2 centre, float EmblemRadius, float health, float store, bool alarm, bool fallen)
        {
            Color rim = fallen ? new Color(0.4f, 0.4f, 0.4f, 1f) : (alarm ? HealthLow : Color.Lerp(HealthLow, HealthWhole, health));

            // The soft glow round the cell.
            for (int ring = 3; ring >= 1; ring--)
            {
                Hexagon(paint, centre, EmblemRadius + (ring * 2.6f));
                paint.fillColor = new Color(rim.r, rim.g, rim.b, (fallen ? 0.03f : 0.07f) * (4 - ring) * (alarm ? 1.6f : 1f));
                paint.Fill();
            }

            Hexagon(paint, centre, EmblemRadius);
            paint.fillColor = CellDark;
            paint.Fill();

            // Honey: the store, rising from the bottom of the cell.
            if (store > 0.01f)
            {
                float top = centre.y + EmblemRadius - (store * EmblemRadius * 2f);
                if (HoneyBelow(paint, centre, EmblemRadius - 2.5f, top))
                {
                    paint.fillColor = new Color(Honey.r, Honey.g, Honey.b, 0.9f);
                    paint.Fill();
                }
            }

            // The rim: dim all round, lit as far as the health reaches, from the top corner clockwise.
            Hexagon(paint, centre, EmblemRadius);
            paint.strokeColor = new Color(rim.r, rim.g, rim.b, 0.18f);
            paint.lineWidth = 3f;
            paint.Stroke();
            if (health > 0.001f)
            {
                HexagonArc(paint, centre, EmblemRadius, health);
                paint.strokeColor = rim;
                paint.lineWidth = 3f;
                paint.lineCap = LineCap.Round;
                paint.Stroke();
            }
        }

        // Pointy-topped, like the cells of a hive.
        private static Vector2 Corner(Vector2 centre, float radius, int corner)
        {
            float angle = (corner * Mathf.PI / 3f) - (Mathf.PI * 0.5f);
            return centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        private static void Hexagon(Painter2D paint, Vector2 centre, float radius)
        {
            paint.BeginPath();
            paint.MoveTo(Corner(centre, radius, 0));
            for (int corner = 1; corner < 6; corner++)
            {
                paint.LineTo(Corner(centre, radius, corner));
            }

            paint.ClosePath();
        }

        private static void HexagonArc(Painter2D paint, Vector2 centre, float radius, float share)
        {
            float sides = Mathf.Clamp01(share) * 6f;
            paint.BeginPath();
            paint.MoveTo(Corner(centre, radius, 0));
            for (int side = 0; side < 6 && sides > 0f; side++)
            {
                Vector2 from = Corner(centre, radius, side);
                Vector2 to = Corner(centre, radius, side + 1);
                paint.LineTo(Vector2.Lerp(from, to, Mathf.Min(sides, 1f)));
                sides -= 1f;
            }
        }

        private static readonly Vector2[] HoneyPoints = new Vector2[12];

        // The hexagon cut off above a line (screen y grows downward), as one closed path. False when nothing is left.
        private static bool HoneyBelow(Painter2D paint, Vector2 centre, float radius, float lineY)
        {
            int count = 0;
            for (int side = 0; side < 6; side++)
            {
                Vector2 a = Corner(centre, radius, side);
                Vector2 b = Corner(centre, radius, side + 1);
                bool aIn = a.y >= lineY;
                bool bIn = b.y >= lineY;
                if (aIn)
                {
                    HoneyPoints[count++] = a;
                }

                if (aIn != bIn)
                {
                    HoneyPoints[count++] = Vector2.Lerp(a, b, (lineY - a.y) / (b.y - a.y));
                }
            }

            if (count < 3)
            {
                return false;
            }

            paint.BeginPath();
            paint.MoveTo(HoneyPoints[0]);
            for (int index = 1; index < count; index++)
            {
                paint.LineTo(HoneyPoints[index]);
            }

            paint.ClosePath();
            return true;
        }

        private static int BarValue(float fraction) => (int)(Mathf.Clamp01(fraction) * BarScale);
    }
}
