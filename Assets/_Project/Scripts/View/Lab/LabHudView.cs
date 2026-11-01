using System.Collections.Generic;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows the Piper's level, bars and combos, and says what just happened.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Lab/Lab Hud View")]
    public sealed class LabHudView : MonoBehaviour
    {
        private const float Margin = 12f;
        private const float StatsWidth = 210f;
        private const float BossWidth = 320f;
        private const float BarHeight = 9f;
        private const float BarGap = 5f;
        private const float PanelPadding = 10f;
        private const float PanelRadius = 8f;
        private const float BigFontSize = 18f;
        private const float BodyFontSize = 13f;
        private const float SmallFontSize = 11f;
        private const float ComboWidth = 138f;
        private const float ComboGap = 6f;
        private const float MessageSeconds = 1.6f;

        private readonly List<ComboPanel> _combos = new List<ComboPanel>();
        private readonly StringBuilder _noteText = new StringBuilder();

        private LabSession _session;
        private IReadOnlyList<IComboSong> _songs;
        private LabTextConfig _texts;
        private LabArtConfig _art;
        private HudConfig _hud;
        private VisualElement _comboRow;
        private VisualElement _bossPanel;
        private Label _levelLabel;
        private Label _statusLabel;
        private Label _messageLabel;
        private Label _boostLabel;
        private Label _bossLabel;
        private LabBar _bossBar;
        private float _messageSecondsLeft;
        private int _shownBoostSeconds;

        /// <summary>The health bar, for tests and debugging.</summary>
        public LabBar HealthBar { get; private set; }

        /// <summary>The stamina bar, for tests and debugging.</summary>
        public LabBar StaminaBar { get; private set; }

        /// <summary>The experience bar, for tests and debugging.</summary>
        public LabBar XpBar { get; private set; }

        /// <summary>The words above the combos, for tests and debugging.</summary>
        public string ShownMessage => _messageLabel?.text;

        /// <summary>The level on screen, for tests and debugging.</summary>
        public string ShownLevel => _levelLabel?.text;

        /// <summary>How many combos can be played now, for tests and debugging.</summary>
        public int ShownCombos { get; private set; }

        /// <summary>True while the boost shows, for tests and debugging.</summary>
        public bool IsShowingABoost => _boostLabel != null && _boostLabel.style.display == DisplayStyle.Flex;

        /// <summary>True while a boss's bar is on screen, for tests and debugging.</summary>
        public bool IsShowingABoss => _bossPanel != null && _bossPanel.style.display == DisplayStyle.Flex;

        /// <summary>Supplies what to show. Call once, before this object starts.</summary>
        public void Initialize(LabSession session, LabTextConfig texts, LabArtConfig art, HudConfig hud)
        {
            _session = session;
            _songs = session.Songs;
            _texts = texts;
            _art = art;
            _hud = hud;
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            Build(GetComponent<UIDocument>().rootVisualElement);
            RebuildCombos();
            _session.SummonRefused += HandleRefused;
            _session.ComboFailed += HandleComboFailed;
            _session.ComboPlayed += HandleComboPlayed;
            _session.Levels.LevelledUp += HandleLevelledUp;
            _session.Battle.CreatureLeft += HandleCreatureLeft;
            _session.Ambushes.Warned += HandleAmbushWarned;
        }

        private void Update()
        {
            if (_levelLabel == null)
            {
                return;
            }

            HealthBar.Show(_session.PiperHealth.Current / _session.PiperHealth.Max);
            StaminaBar.Show(_session.Stamina.Current / _session.Stamina.Max);
            XpBar.Show(_session.Levels.Progress);
            _statusLabel.text = _session.Battle.IsInBattle ? _texts.InBattleLabel : _texts.QuietLabel;
            _statusLabel.style.color = _session.Battle.IsInBattle ? _art.HealthColor : _hud.CaptionColor;
            ShowBoost();
            ShowCooldowns();
            ShowBoss();
            FadeMessage();
        }

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so _levelLabel can be null.
            if (_levelLabel != null)
            {
                _session.SummonRefused -= HandleRefused;
                _session.ComboFailed -= HandleComboFailed;
                _session.ComboPlayed -= HandleComboPlayed;
                _session.Levels.LevelledUp -= HandleLevelledUp;
                _session.Battle.CreatureLeft -= HandleCreatureLeft;
                _session.Ambushes.Warned -= HandleAmbushWarned;
            }
        }

        private void Build(VisualElement root)
        {
            var layer = new VisualElement();
            layer.pickingMode = PickingMode.Ignore;
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.right = 0f;
            layer.style.top = 0f;
            layer.style.bottom = 0f;

            layer.Add(BuildStats());
            layer.Add(BuildBoss());
            layer.Add(BuildBottom());
            root.Add(layer);
        }

        private VisualElement BuildStats()
        {
            VisualElement panel = NewPanel();
            panel.style.position = Position.Absolute;
            panel.style.left = Margin;
            panel.style.top = Margin;
            panel.style.width = StatsWidth;

            _levelLabel = NewLabel(BigFontSize, _hud.TextColor);
            _statusLabel = NewLabel(BodyFontSize, _hud.CaptionColor);
            HealthBar = NewBar(_art.HealthColor);
            StaminaBar = NewBar(_art.StaminaColor);
            XpBar = NewBar(_art.XpColor);
            _boostLabel = NewLabel(BodyFontSize, _art.StaminaColor);
            _boostLabel.style.display = DisplayStyle.None;

            panel.Add(_levelLabel);
            panel.Add(HealthBar.Root);
            panel.Add(StaminaBar.Root);
            panel.Add(XpBar.Root);
            panel.Add(_statusLabel);
            panel.Add(_boostLabel);
            return panel;
        }

        private VisualElement BuildBoss()
        {
            _bossPanel = NewPanel();
            _bossPanel.style.position = Position.Absolute;
            _bossPanel.style.top = Margin;
            _bossPanel.style.width = BossWidth;
            _bossPanel.style.left = Length.Percent(50f);
            _bossPanel.style.marginLeft = -BossWidth / 2f;
            _bossPanel.style.display = DisplayStyle.None;

            _bossLabel = NewLabel(BodyFontSize, _hud.TextColor);
            _bossBar = NewBar(_art.HealthColor);
            _bossPanel.Add(_bossLabel);
            _bossPanel.Add(_bossBar.Root);
            return _bossPanel;
        }

        private VisualElement BuildBottom()
        {
            var bottom = new VisualElement();
            bottom.pickingMode = PickingMode.Ignore;
            bottom.style.position = Position.Absolute;
            bottom.style.left = 0f;
            bottom.style.right = 0f;
            bottom.style.bottom = Margin;
            bottom.style.alignItems = Align.Center;

            _messageLabel = NewLabel(BodyFontSize, _hud.TextColor);
            _comboRow = new VisualElement();
            _comboRow.pickingMode = PickingMode.Ignore;
            _comboRow.style.flexDirection = FlexDirection.Row;
            _comboRow.style.flexWrap = Wrap.Wrap;
            _comboRow.style.justifyContent = Justify.Center;

            bottom.Add(_messageLabel);
            bottom.Add(_comboRow);
            return bottom;
        }

        // Known combos, then the next one to unlock, so the player can see what they are working towards.
        private void RebuildCombos()
        {
            _combos.Clear();
            _comboRow.Clear();
            ShownCombos = 0;
            bool hasShownALock = false;

            foreach (IComboSong song in _songs)
            {
                if (song.UnlockLevel <= _session.Levels.Level)
                {
                    AddCombo(song);
                    ShownCombos++;
                }
                else if (!hasShownALock)
                {
                    AddLockedCombo(song);
                    hasShownALock = true;
                }
            }

            _levelLabel.text = $"{_texts.LevelLabel} {_session.Levels.Level}";
        }

        private void AddCombo(IComboSong song)
        {
            VisualElement panel = NewComboPanel();
            Label name = NewLabel(BodyFontSize, _hud.TextColor);
            Label notes = NewLabel(SmallFontSize, _hud.GlowColor);
            Label role = NewLabel(SmallFontSize, _hud.CaptionColor);
            name.text = $"{song.Name} ({song.StaminaCost:0})";
            notes.text = NoteNames(song.Combo);
            role.text = song.Role;
            role.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(name);
            panel.Add(notes);
            panel.Add(role);
            _comboRow.Add(panel);
            _combos.Add(new ComboPanel(song, name, notes));
        }

        private void AddLockedCombo(IComboSong song)
        {
            VisualElement panel = NewComboPanel();
            Label name = NewLabel(BodyFontSize, _art.CoolingColor);
            Label level = NewLabel(SmallFontSize, _art.CoolingColor);
            name.text = song.Name;
            level.text = $"{_texts.LockedLabel} {song.UnlockLevel}";
            panel.Add(name);
            panel.Add(level);
            _comboRow.Add(panel);
        }

        private string NoteNames(IReadOnlyList<int> notes)
        {
            _noteText.Clear();

            foreach (int note in notes)
            {
                _noteText.Append(_texts.NoteNames[note]).Append(' ');
            }

            return _noteText.ToString().TrimEnd();
        }

        // Only rewrites the words when the number changes, because this runs every frame.
        private void ShowBoost()
        {
            bool isBoosted = _session.Battle.IsBoosted;
            _boostLabel.style.display = isBoosted ? DisplayStyle.Flex : DisplayStyle.None;
            int seconds = Mathf.CeilToInt(_session.Battle.BoostSecondsLeft);

            if (isBoosted && seconds != _shownBoostSeconds)
            {
                _shownBoostSeconds = seconds;
                _boostLabel.text = $"{_texts.BoostLabel} {seconds}s";
            }
        }

        private void ShowCooldowns()
        {
            foreach (ComboPanel combo in _combos)
            {
                float seconds = _session.Cooldowns.SecondsLeft(combo.Song);
                bool isReady = seconds <= 0f;
                combo.Name.style.color = isReady ? _hud.TextColor : _art.CoolingColor;
                combo.Notes.style.color = isReady ? _hud.GlowColor : _art.CoolingColor;
                combo.Notes.text = isReady ? NoteNames(combo.Song.Combo) : $"{seconds:0.0}s";
            }
        }

        private void ShowBoss()
        {
            Minion boss = NearestAwakeBoss();
            _bossPanel.style.display = boss != null ? DisplayStyle.Flex : DisplayStyle.None;

            if (boss != null)
            {
                _bossLabel.text = boss.Type.Name;
                _bossBar.Show(boss.Health.Current / boss.Health.Max);
            }
        }

        private Minion NearestAwakeBoss()
        {
            foreach (Minion minion in _session.Battle.AwakeMinions)
            {
                if (minion.IsBoss)
                {
                    return minion;
                }
            }

            return null;
        }

        private void FadeMessage()
        {
            if (_messageSecondsLeft <= 0f)
            {
                return;
            }

            _messageSecondsLeft -= Time.unscaledDeltaTime;

            if (_messageSecondsLeft <= 0f)
            {
                _messageLabel.text = string.Empty;
            }
        }

        private void Say(string message)
        {
            _messageLabel.text = message;
            _messageSecondsLeft = MessageSeconds;
        }

        private void HandleRefused(SummonRefusal refusal)
        {
            switch (refusal)
            {
                case SummonRefusal.CoolingDown:
                    Say(_texts.CoolingDownMessage);
                    break;
                case SummonRefusal.TooTired:
                    Say(_texts.TooTiredMessage);
                    break;
                default:
                    Say(_texts.NoRoomMessage);
                    break;
            }
        }

        private void HandleComboFailed() => Say(_texts.FailedComboMessage);

        private void HandleComboPlayed(IComboSong song) => Say(song.Name);

        private void HandleCreatureLeft(Creature creature) => Say(_texts.CreatureLeftMessage);

        private void HandleAmbushWarned(Vector2 position) => Say(_texts.AmbushMessage);

        private void HandleLevelledUp(int level)
        {
            RebuildCombos();
            Say($"{_texts.LevelUpMessage} {level}");
        }

        private VisualElement NewPanel()
        {
            var panel = new VisualElement();
            panel.pickingMode = PickingMode.Ignore;
            panel.style.paddingLeft = PanelPadding;
            panel.style.paddingRight = PanelPadding;
            panel.style.paddingTop = PanelPadding;
            panel.style.paddingBottom = PanelPadding;
            panel.style.borderTopLeftRadius = PanelRadius;
            panel.style.borderTopRightRadius = PanelRadius;
            panel.style.borderBottomLeftRadius = PanelRadius;
            panel.style.borderBottomRightRadius = PanelRadius;
            panel.style.backgroundColor = _hud.PanelColor;
            return panel;
        }

        private VisualElement NewComboPanel()
        {
            VisualElement panel = NewPanel();
            panel.style.width = ComboWidth;
            panel.style.marginLeft = ComboGap / 2f;
            panel.style.marginRight = ComboGap / 2f;
            panel.style.marginTop = ComboGap;
            return panel;
        }

        private LabBar NewBar(Color fillColor)
        {
            var bar = new LabBar(BarHeight, fillColor, _hud.BreathTrackColor);
            bar.Root.style.marginTop = BarGap;
            return bar;
        }

        private static Label NewLabel(float fontSize, Color color)
        {
            var label = new Label();
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            return label;
        }

        private readonly struct ComboPanel
        {
            public ComboPanel(IComboSong song, Label name, Label notes)
            {
                Song = song;
                Name = name;
                Notes = notes;
            }

            public IComboSong Song { get; }
            public Label Name { get; }
            public Label Notes { get; }
        }
    }
}
