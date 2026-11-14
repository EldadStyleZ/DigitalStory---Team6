using System;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// The settings panel of plan section AC, built in the HUD's <c>settings-panel</c> element: a card with the Master, Music
    /// and Effects volumes, each a slider with its percentage, and (plan section AD) the frame rate and the render scale, each
    /// a choice between arrows. The chosen row wears a gold ring. The keys and the controller move between rows and change
    /// the chosen one through the root (<see cref="MoveFocus"/>, the settings' Step); the mouse drags a slider, clicks a
    /// choice to take the next one, or clicks a row. It only shows the <see cref="VolumeSettings"/> and the
    /// <see cref="DisplaySettings"/> and writes what the mouse sets; the root pauses the match, saves and applies them.
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
        private const string ChoiceClass = "settings-choice";
        private const string HintClass = "settings-hint";
        private const string KeysHint = "Up / Down: choose    Left / Right: change    Esc: close";
        private const string PadHint = "D-pad: choose and change    B or Start: close";
        private const string ChoiceFormat = "<   {0}   >";
        private const int VolumeRows = 3;

        // The panel's rows, in the order of SettingsRow.
        private static readonly string[] Names = { "Master", "Music", "Effects", "Frame rate", "Render scale" };

        private readonly VolumeSettings _volumes;
        private readonly DisplaySettings _display;
        private readonly Func<FrameRateChoice, string> _frameRateText;
        private readonly Func<RenderScaleChoice, string> _renderScaleText;
        private readonly VisualElement _panel;
        private readonly VisualElement[] _rows;
        private readonly SliderInt[] _sliders;
        private readonly Label[] _values;
        private readonly Label _hint;

        /// <param name="root">The HUD's root, which holds an element named settings-panel.</param>
        /// <param name="volumes">The volumes it shows and the mouse sets.</param>
        /// <param name="display">The frame-rate and render-scale choices it shows and the mouse sets.</param>
        /// <param name="frameRateText">How a frame-rate choice reads on this display.</param>
        /// <param name="renderScaleText">How a render-scale choice reads on this display.</param>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The HUD has no settings-panel element.</exception>
        public SettingsPanelView(
            VisualElement root,
            VolumeSettings volumes,
            DisplaySettings display,
            Func<FrameRateChoice, string> frameRateText,
            Func<RenderScaleChoice, string> renderScaleText)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _volumes = volumes ?? throw new ArgumentNullException(nameof(volumes));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _frameRateText = frameRateText ?? throw new ArgumentNullException(nameof(frameRateText));
            _renderScaleText = renderScaleText ?? throw new ArgumentNullException(nameof(renderScaleText));
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
            _sliders = new SliderInt[VolumeRows];
            _values = new Label[Names.Length];
            for (int index = 0; index < Names.Length; index++)
            {
                card.Add(index < VolumeRows ? MakeVolumeRow(index) : MakeChoiceRow(index));
            }

            _hint = new Label(KeysHint);
            _hint.AddToClassList(HintClass);
            card.Add(_hint);
            _panel.RemoveFromClassList(ShownClass);
            FocusedRow = SettingsRow.Master;
            ShowFocus();
            _volumes.Changed += OnVolumeChanged;
            _display.Changed += OnDisplayChanged;
        }

        /// <summary>True while the panel shows.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>The row the keys and the controller change.</summary>
        public SettingsRow FocusedRow { get; private set; }

        /// <summary>The percentage a volume row shows, for tests.</summary>
        public string PercentText(VolumeGroup group) => _values[(int)group].text;

        /// <summary>The text a row shows as its value, for tests.</summary>
        public string ValueText(SettingsRow row) => _values[(int)row].text;

        /// <summary>Shows the panel on the first row.</summary>
        public void Open()
        {
            IsOpen = true;
            FocusedRow = SettingsRow.Master;
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
            int next = Math.Max(0, Math.Min(Names.Length - 1, (int)FocusedRow + rows));
            FocusedRow = (SettingsRow)next;
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
            _display.Changed -= OnDisplayChanged;
        }

        private VisualElement MakeVolumeRow(int index)
        {
            var group = (VolumeGroup)index;
            VisualElement row = MakeRow(index);
            var slider = new SliderInt(0, VolumeCurve.Full) { focusable = false };
            slider.AddToClassList(SliderClass);
            slider.SetValueWithoutNotify(_volumes.PercentOf(group));
            slider.RegisterValueChangedCallback(change => _volumes.Set(group, change.newValue));
            var percent = new Label(PercentOf(group));
            percent.AddToClassList(PercentClass);
            row.Add(slider);
            row.Add(percent);
            _sliders[index] = slider;
            _values[index] = percent;
            return row;
        }

        // A click on the value takes the next choice, and the first after the last.
        private VisualElement MakeChoiceRow(int index)
        {
            var settingsRow = (SettingsRow)index;
            VisualElement row = MakeRow(index);
            var value = new Label(ChoiceText(settingsRow));
            value.AddToClassList(ChoiceClass);
            value.RegisterCallback<ClickEvent>(_ => TakeNextChoice(settingsRow));
            row.Add(value);
            _values[index] = value;
            return row;
        }

        private VisualElement MakeRow(int index)
        {
            var row = new VisualElement();
            row.AddToClassList(RowClass);
            var name = new Label(Names[index]);
            name.AddToClassList(NameClass);
            row.Add(name);
            row.RegisterCallback<PointerDownEvent>(_ =>
            {
                FocusedRow = (SettingsRow)index;
                ShowFocus();
            });

            _rows[index] = row;
            return row;
        }

        private void TakeNextChoice(SettingsRow row)
        {
            if (row == SettingsRow.FrameRate && !_display.StepFrameRate(1))
            {
                _display.SetFrameRate(FrameRateChoice.Display);
            }
            else if (row == SettingsRow.RenderScale && !_display.StepRenderScale(1))
            {
                _display.SetRenderScale(RenderScaleChoice.Auto);
            }
        }

        private string ChoiceText(SettingsRow row)
        {
            string value = row == SettingsRow.FrameRate ? _frameRateText(_display.FrameRate) : _renderScaleText(_display.RenderScale);
            return string.Format(CultureInfo.InvariantCulture, ChoiceFormat, value);
        }

        private string PercentOf(VolumeGroup group)
        {
            return _volumes.PercentOf(group).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private void ShowFocus()
        {
            for (int index = 0; index < _rows.Length; index++)
            {
                _rows[index].EnableInClassList(FocusedClass, index == (int)FocusedRow);
            }
        }

        private void OnVolumeChanged(VolumeGroup group)
        {
            int index = (int)group;
            _sliders[index].SetValueWithoutNotify(_volumes.PercentOf(group));
            _values[index].text = PercentOf(group);
        }

        private void OnDisplayChanged()
        {
            _values[(int)SettingsRow.FrameRate].text = ChoiceText(SettingsRow.FrameRate);
            _values[(int)SettingsRow.RenderScale].text = ChoiceText(SettingsRow.RenderScale);
        }
    }
}
