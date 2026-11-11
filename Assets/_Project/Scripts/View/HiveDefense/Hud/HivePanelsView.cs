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
            panel.Add(row);

            panel.Add(Track("hive-fill", "bar-track", out VisualElement healthFill));
            panel.Add(Track("hive-store-fill", "bar-track--thin", out VisualElement storeFill));

            var hint = new Label(string.Empty);
            hint.AddToClassList("hive-hint");
            hint.pickingMode = PickingMode.Ignore;
            panel.Add(hint);

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
            label.text = hint == HintHealing ? _healingText : _holdText;
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

        private static int BarValue(float fraction) => (int)(Mathf.Clamp01(fraction) * BarScale);
    }
}
