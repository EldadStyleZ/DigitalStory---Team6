using System;
using System.Globalization;
using DigitalStory.Logic;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The settings panel of plan section AC, built in the HUD's <c>settings-panel</c> element: a card with the Master, Music
    /// and Effects volumes, each a slider with its percentage. The chosen row wears a gold ring. The keys and the controller
    /// move between rows and change the chosen volume through the root (<see cref="MoveFocus"/>, the settings' Step); the
    /// mouse drags a slider or clicks a row. It only shows the <see cref="VolumeSettings"/> and writes what the mouse sets;
    /// the root pauses the match, saves the volumes and plays the sample.
    /// </summary>
    public sealed class SettingsPanelView : IDisposable
    {
        private const string ShownClass = "settings-panel--shown";
        private const string CardClass = "settings-card";
        private const string TitleClass = "settings-title";
        private const string RowClass = "settings-row";
        private const string FocusedClass = "settings-row--focused";
        private const string NameClass = "settings-name";
        private const string SliderClass = "settings-slider";
        private const string PercentClass = "settings-percent";
        private const string HintClass = "settings-hint";
        private const string KeysHint = "Up / Down: choose    Left / Right: change    Esc: close";
        private const string PadHint = "D-pad: choose and change    B or Start: close";

        // The panel's rows, in the order of VolumeGroup.
        private static readonly string[] Names = { "Master", "Music", "Effects" };

        private readonly VolumeSettings _volumes;
        private readonly VisualElement _panel;
        private readonly VisualElement[] _rows;
        private readonly SliderInt[] _sliders;
        private readonly Label[] _percents;
        private readonly Label _hint;

        /// <param name="root">The HUD's root, which holds an element named settings-panel.</param>
        /// <param name="volumes">The volumes it shows and the mouse sets.</param>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The HUD has no settings-panel element.</exception>
        public SettingsPanelView(VisualElement root, VolumeSettings volumes)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _volumes = volumes ?? throw new ArgumentNullException(nameof(volumes));
            _panel = root.Q<VisualElement>("settings-panel")
                ?? throw new InvalidOperationException("The HUD UXML has no element named settings-panel.");
            _panel.Clear();
            var card = new VisualElement();
            card.AddToClassList(CardClass);
            _panel.Add(card);
            var title = new Label("Settings");
            title.AddToClassList(TitleClass);
            card.Add(title);

            _rows = new VisualElement[Names.Length];
            _sliders = new SliderInt[Names.Length];
            _percents = new Label[Names.Length];
            for (int index = 0; index < Names.Length; index++)
            {
                card.Add(MakeRow(index));
            }

            _hint = new Label(KeysHint);
            _hint.AddToClassList(HintClass);
            card.Add(_hint);
            _panel.RemoveFromClassList(ShownClass);
            Focused = VolumeGroup.Master;
            ShowFocus();
            _volumes.Changed += OnVolumeChanged;
        }

        /// <summary>True while the panel shows.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The row the keys and the controller change.</summary>
        public VolumeGroup Focused { get; private set; }

        /// <summary>The percentage a row shows, for tests.</summary>
        public string PercentText(VolumeGroup group) => _percents[(int)group].text;

        /// <summary>Shows the panel on the first row.</summary>
        public void Open()
        {
            IsOpen = true;
            Focused = VolumeGroup.Master;
            ShowFocus();
            _panel.AddToClassList(ShownClass);
        }

        /// <summary>Hides the panel.</summary>
        public void Close()
        {
            IsOpen = false;
            _panel.RemoveFromClassList(ShownClass);
        }

        /// <summary>Moves the chosen row by <paramref name="rows"/> (negative is up), stopping at the first and the last.</summary>
        public void MoveFocus(int rows)
        {
            int next = Math.Max(0, Math.Min(Names.Length - 1, (int)Focused + rows));
            Focused = (VolumeGroup)next;
            ShowFocus();
        }

        /// <summary>Names the panel's keys for the device in the player's hands.</summary>
        public void ShowControls(ControlDevice device)
        {
            _hint.text = device == ControlDevice.Gamepad ? PadHint : KeysHint;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _volumes.Changed -= OnVolumeChanged;
        }

        private VisualElement MakeRow(int index)
        {
            var group = (VolumeGroup)index;
            var row = new VisualElement();
            row.AddToClassList(RowClass);
            var name = new Label(Names[index]);
            name.AddToClassList(NameClass);
            var slider = new SliderInt(0, VolumeCurve.Full) { focusable = false };
            slider.AddToClassList(SliderClass);
            slider.SetValueWithoutNotify(_volumes.PercentOf(group));
            slider.RegisterValueChangedCallback(change => _volumes.Set(group, change.newValue));
            var percent = new Label(PercentOf(group));
            percent.AddToClassList(PercentClass);
            row.Add(name);
            row.Add(slider);
            row.Add(percent);
            row.RegisterCallback<PointerDownEvent>(_ =>
            {
                Focused = group;
                ShowFocus();
            });

            _rows[index] = row;
            _sliders[index] = slider;
            _percents[index] = percent;
            return row;
        }

        private string PercentOf(VolumeGroup group)
        {
            return _volumes.PercentOf(group).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private void ShowFocus()
        {
            for (int index = 0; index < _rows.Length; index++)
            {
                _rows[index].EnableInClassList(FocusedClass, index == (int)Focused);
            }
        }

        private void OnVolumeChanged(VolumeGroup group)
        {
            int index = (int)group;
            _sliders[index].SetValueWithoutNotify(_volumes.PercentOf(group));
            _percents[index].text = PercentOf(group);
        }
    }
}
