using System;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Shows the Wasp Piper's HUD: wasps, Fury, the songbook of found songs, recent notes, hives and waves. It also creates and
    /// initializes the <see cref="HiveOffscreenIndicatorView"/> and <see cref="EndScreenView"/> on this object.
    /// </summary>
    /// <remarks>The UIDocument's Visual Tree Asset must be HiveDefenseHud.uxml. Everything shown is a label or a bar width.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Hive Defense Hud View")]
    public sealed class HiveDefenseHudView : MonoBehaviour
    {
        private const string LowClass = "wasp-count--low";
        private const string LowFillClass = "wasp-fill--low";
        private const string FuryFullClass = "fury-panel--full";
        private const string FlashClass = "hive-panel--flash";
        private const string DestroyedClass = "hive-panel--destroyed";
        private const string NoteShownClass = "note--shown";
        private const string NoteHintClass = "note--hint";
        private const string LineShownClass = "line--shown";
        private const string HoleClassPrefix = "hole-";
        private const int HoleColours = 6;
        private const float BarScale = 1000f;

        [SerializeField, Tooltip("The swarm's look and numbers: the category colours and the low-wasps warning.")]
        private SwarmConfig _swarmConfig;

        [SerializeField, Tooltip("The line shown in the first seconds of a match.")]
        private string _promptText = "Play notes with H J K L. Find the songs.";

        [SerializeField, Min(0f), Tooltip("How long the first line stays, in seconds.")]
        private float _promptSeconds = 8f;

        [SerializeField, Min(0.1f), Tooltip("How long the 'New song' line stays, in seconds.")]
        private float _discoverySeconds = 3f;

        [SerializeField, Min(0.1f), Tooltip("How long the 'Not ready' line stays, in seconds.")]
        private float _notReadySeconds = 1.2f;

        [SerializeField, Min(0.05f), Tooltip("Seconds between the alarm colour and normal while a hive is under attack.")]
        private float _flashSeconds = 0.25f;

        [SerializeField, Min(1), Tooltip("How many recent notes there is room for. Match SongConfig.NotesRemembered.")]
        private int _noteSlots = 5;

        [SerializeField, Tooltip("The camera that looks at the hives, for the off-screen arrows. Main Camera when empty.")]
        private Camera _camera;

        private HiveDefenseSession _session;
        private bool _isReady;
        private HiveOffscreenIndicatorView _indicator;
        private EndScreenView _endScreen;

        private Label _waspCount;
        private VisualElement _waspFill;
        private VisualElement _furyPanel;
        private VisualElement _furyFill;
        private Label _waveLabel;
        private Label _nextWaveLabel;
        private SongbookView _songbook;
        private Label _furyKeys;
        private Label _promptLine;
        private Label _messageLine;
        private int _colossusSong = -1;
        private bool _isFuryRevealed;
        private float _promptLeft;
        private float _messageLeft;
        private Label[] _noteCells;
        private bool[] _noteHints;
        private int[] _noteHoles;
        private VisualElement[] _hivePanels;
        private VisualElement[] _hiveFills;
        private int[] _hiveBarShown;
        private float[] _hiveFlashLeft;
        private bool[] _hiveFlashOn;
        private bool[] _hiveDestroyedShown;

        private int _shownWasps = -1;
        private int _shownMax = -1;
        private bool _isLow;
        private bool _isLowKnown;
        private int _shownWaspBar = -1;
        private int _shownFuryBar = -1;
        private bool _isFuryFull;
        private int _shownWave = -1;
        private int _shownWaveCount = -1;
        private int _shownNextWave = -1;

        /// <summary>The text on the wasp count, for tests and debugging.</summary>
        public string WaspCountText => _waspCount?.text;

        /// <summary>Builds the HUD's cells from the match. Call once, before the first Update.</summary>
        /// <exception cref="ArgumentNullException">The session is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element, or there is no camera.</exception>
        public void Initialize(HiveDefenseSession session)
        {
            _isReady = false;
            ReleaseSongs();
            _session = session ?? throw new ArgumentNullException(nameof(session));

            VisualElement root = GetComponent<UIDocument>().rootVisualElement
                ?? throw new InvalidOperationException("The UIDocument has no Visual Tree Asset. Set HiveDefenseHud.uxml.");

            _waspCount = Require<Label>(root, "wasp-count");
            _waspFill = Require<VisualElement>(root, "wasp-fill");
            if (_swarmConfig == null)
            {
                throw new InvalidOperationException("The HUD needs the SwarmConfig. Set it on the HiveDefenseHudView.");
            }

            _furyPanel = Require<VisualElement>(root, "fury-panel");
            _furyFill = Require<VisualElement>(root, "fury-fill");
            _furyKeys = Require<Label>(root, "fury-keys");
            _promptLine = Require<Label>(root, "prompt-line");
            _messageLine = Require<Label>(root, "message-line");
            VisualElement songbook = Require<VisualElement>(root, "songbook");
            _waveLabel = Require<Label>(root, "wave-label");
            _nextWaveLabel = Require<Label>(root, "next-wave-label");
            VisualElement notes = Require<VisualElement>(root, "notes");
            VisualElement hives = Require<VisualElement>(root, "hives");
            VisualElement offscreen = Require<VisualElement>(root, "offscreen-layer");
            VisualElement endScreen = Require<VisualElement>(root, "end-screen");

            _colossusSong = ColossusSongIndex();
            _isFuryRevealed = false;
            _furyKeys.text = string.Empty;
            _songbook = new SongbookView(session, _swarmConfig, songbook);
            session.Discovery.Discovered += OnDiscovered;
            session.Caster.SongNotReady += OnSongNotReady;
            _promptLine.text = _promptText;
            _promptLeft = _promptSeconds;
            _promptLine.EnableInClassList(LineShownClass, _promptSeconds > 0f);
            _messageLeft = 0f;
            _messageLine.RemoveFromClassList(LineShownClass);
            BuildNotes(notes);
            BuildHives(hives);

            Camera camera = _camera != null ? _camera : Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("The HUD needs a camera for the off-screen hive arrows. Set one, or tag the main camera.");
            }

            _indicator = GetOrAdd<HiveOffscreenIndicatorView>();
            _indicator.Initialize(session.Hives, camera, offscreen);
            _endScreen = GetOrAdd<EndScreenView>();
            _endScreen.Initialize(session, endScreen);
            _isReady = true; // Last, so Update never runs on a half-built HUD.
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            ShowWasps();
            ShowFury();
            ShowWaves();
            _songbook.Tick(Time.deltaTime);
            ShowNotes();
            ShowLines(Time.deltaTime);
            ShowHives(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ReleaseSongs();
        }

        private void ReleaseSongs()
        {
            if (_songbook != null)
            {
                _songbook.Dispose();
                _songbook = null;
            }

            if (_session != null)
            {
                _session.Discovery.Discovered -= OnDiscovered;
                _session.Caster.SongNotReady -= OnSongNotReady;
            }
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException("The HUD UXML has no element named " + name + ".");
        }

        private T GetOrAdd<T>() where T : Component
        {
            T existing = GetComponent<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        // The Colossus song is found by its type, once, so the HUD never repeats the keys by hand.
        private int ColossusSongIndex()
        {
            SwarmSongCaster caster = _session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is ColossusSong)
                {
                    return song;
                }
            }

            return -1;
        }

        private static string KeysText(SongDefinition song)
        {
            var text = new System.Text.StringBuilder();
            for (int index = 0; index < song.Keys.Count; index++)
            {
                if (index > 0)
                {
                    text.Append(' ');
                }

                text.Append(HiveDefenseInput.HoleName(song.Keys[index]));
            }

            return text.ToString();
        }

        private void BuildNotes(VisualElement row)
        {
            _noteCells = new Label[_noteSlots];
            _noteHoles = new int[_noteSlots];
            _noteHints = new bool[_noteSlots];
            for (int index = 0; index < _noteSlots; index++)
            {
                var note = new Label();
                note.AddToClassList("note");
                note.pickingMode = PickingMode.Ignore;
                row.Add(note);
                _noteCells[index] = note;
                _noteHoles[index] = -1;
            }
        }

        private void BuildHives(VisualElement column)
        {
            HiveSet hives = _session.Hives;
            _hivePanels = new VisualElement[hives.Count];
            _hiveFills = new VisualElement[hives.Count];
            _hiveBarShown = new int[hives.Count];
            _hiveFlashLeft = new float[hives.Count];
            _hiveFlashOn = new bool[hives.Count];
            _hiveDestroyedShown = new bool[hives.Count];

            for (int index = 0; index < hives.Count; index++)
            {
                var panel = new VisualElement();
                panel.AddToClassList("hive-panel");
                panel.pickingMode = PickingMode.Ignore;

                var title = new Label("Hive " + (index + 1).ToString(CultureInfo.InvariantCulture));
                title.AddToClassList("hive-title");
                panel.Add(title);

                var track = new VisualElement();
                track.AddToClassList("bar-track");
                track.pickingMode = PickingMode.Ignore;
                var fill = new VisualElement();
                fill.AddToClassList("bar-fill");
                fill.AddToClassList("hive-fill");
                fill.pickingMode = PickingMode.Ignore;
                track.Add(fill);
                panel.Add(track);

                column.Add(panel);
                _hivePanels[index] = panel;
                _hiveFills[index] = fill;
                _hiveBarShown[index] = -1;
            }
        }

        private static int HoleColour(int hole) => ((hole % HoleColours) + HoleColours) % HoleColours;

        private void ShowWasps()
        {
            SwarmBody swarm = _session.Swarm;
            int count = swarm.Count;
            int max = swarm.Max;
            if (count != _shownWasps || max != _shownMax)
            {
                _shownWasps = count;
                _shownMax = max;
                _waspCount.text = count.ToString("N0", CultureInfo.InvariantCulture)
                    + " / " + max.ToString("N0", CultureInfo.InvariantCulture);
            }

            bool isLow = count < _swarmConfig.LowWasps;
            if (!_isLowKnown || isLow != _isLow)
            {
                _isLowKnown = true;
                _isLow = isLow;
                _waspCount.EnableInClassList(LowClass, isLow);
                _waspFill.EnableInClassList(LowFillClass, isLow);
            }

            int bar = BarValue(max > 0 ? (float)count / max : 0f);
            if (bar != _shownWaspBar)
            {
                _shownWaspBar = bar;
                _waspFill.style.width = Length.Percent(bar / BarScale * 100f);
            }
        }

        private void ShowFury()
        {
            FuryMeter fury = _session.Fury;
            int bar = BarValue(fury.Fraction);
            if (bar != _shownFuryBar)
            {
                _shownFuryBar = bar;
                _furyFill.style.width = Length.Percent(bar / BarScale * 100f);
            }

            if (!_isFuryRevealed && _colossusSong >= 0 && _session.Discovery.IsRevealed(_colossusSong))
            {
                _isFuryRevealed = true;
                _furyKeys.text = KeysText(_session.Caster[_colossusSong].Song);
            }

            if (fury.IsFull != _isFuryFull)
            {
                _isFuryFull = fury.IsFull;
                _furyPanel.EnableInClassList(FuryFullClass, _isFuryFull);
            }
        }

        private void ShowWaves()
        {
            WaveDirector waves = _session.Waves;
            int wave = waves.WaveNumber;
            int count = waves.WaveCount;
            if (wave != _shownWave || count != _shownWaveCount)
            {
                _shownWave = wave;
                _shownWaveCount = count;
                _waveLabel.text = "Wave " + wave.ToString(CultureInfo.InvariantCulture)
                    + "/" + count.ToString(CultureInfo.InvariantCulture);
            }

            // 0 while a wave runs or after the last one, so the line is hidden then, and its backing with it.
            int next = Mathf.CeilToInt(waves.SecondsToNextWave);
            if (next != _shownNextWave)
            {
                _shownNextWave = next;
                _nextWaveLabel.text = next > 0 ? "Next wave in " + next.ToString(CultureInfo.InvariantCulture) : string.Empty;
                _nextWaveLabel.style.display = next > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ShowNotes()
        {
            SwarmSongCaster caster = _session.Caster;
            int count = Mathf.Min(caster.RecentNoteCount, _noteCells.Length);
            int hint = _session.Discovery.HintNotes(caster);
            int hintFrom = count - Mathf.Min(hint, count);
            for (int slot = 0; slot < _noteCells.Length; slot++)
            {
                Label cell = _noteCells[slot];
                if (slot >= count)
                {
                    if (_noteHoles[slot] != -1)
                    {
                        _noteHoles[slot] = -1;
                        cell.RemoveFromClassList(NoteShownClass);
                    }

                    SetHint(slot, false);

                    continue;
                }

                int hole = caster.RecentNoteHole(slot);
                if (hole != _noteHoles[slot])
                {
                    if (_noteHoles[slot] == -1)
                    {
                        cell.AddToClassList(NoteShownClass);
                    }
                    else
                    {
                        cell.RemoveFromClassList(HoleClassPrefix + HoleColour(_noteHoles[slot]));
                    }

                    _noteHoles[slot] = hole;
                    cell.AddToClassList(HoleClassPrefix + HoleColour(hole));
                    cell.text = HiveDefenseInput.HoleName(hole);
                }

                SetHint(slot, slot >= hintFrom && hint > 0);
                cell.style.opacity = 1f - caster.RecentNoteAge01(slot);
            }
        }

        private void SetHint(int slot, bool isHint)
        {
            if (_noteHints[slot] != isHint)
            {
                _noteHints[slot] = isHint;
                _noteCells[slot].EnableInClassList(NoteHintClass, isHint);
            }
        }

        private void ShowLines(float deltaTime)
        {
            if (_promptLeft > 0f)
            {
                _promptLeft -= deltaTime;
                if (_promptLeft <= 0f)
                {
                    _promptLine.RemoveFromClassList(LineShownClass);
                }
            }

            if (_messageLeft > 0f)
            {
                _messageLeft -= deltaTime;
                if (_messageLeft <= 0f)
                {
                    _messageLine.RemoveFromClassList(LineShownClass);
                }
            }
        }

        private void OnDiscovered(int song)
        {
            SwarmSongEffect effect = _session.Caster[song];
            ShowMessage("New song: " + effect.Song.Title, _swarmConfig.ColorOf(effect.Category), _discoverySeconds);
        }

        // A known song is named, and its chip in the songbook nudges too. An unknown one is not named, so it stays hidden.
        private void OnSongNotReady(int song)
        {
            if (_session.Discovery.IsKnown(song))
            {
                ShowMessage(_session.Caster[song].Song.Title + " not ready", Color.clear, _notReadySeconds);
                return;
            }

            ShowMessage("Not ready", Color.clear, _notReadySeconds);
        }

        private void ShowMessage(string text, Color color, float seconds)
        {
            _messageLine.text = text;
            if (color.a > 0f)
            {
                _messageLine.style.color = color;
            }
            else
            {
                _messageLine.style.color = StyleKeyword.Null;
            }

            _messageLeft = seconds;
            _messageLine.AddToClassList(LineShownClass);
        }

        private void ShowHives(float deltaTime)
        {
            HiveSet hives = _session.Hives;
            for (int index = 0; index < _hivePanels.Length; index++)
            {
                Hive hive = hives[index];
                int bar = BarValue(hive.MaxHealth > 0f ? hive.Health / hive.MaxHealth : 0f);
                if (bar != _hiveBarShown[index])
                {
                    _hiveBarShown[index] = bar;
                    _hiveFills[index].style.width = Length.Percent(bar / BarScale * 100f);
                }

                if (hive.IsDestroyed != _hiveDestroyedShown[index])
                {
                    _hiveDestroyedShown[index] = hive.IsDestroyed;
                    _hivePanels[index].EnableInClassList(DestroyedClass, hive.IsDestroyed);
                }

                // The alarm colour for one period, normal for the next, for as long as the attack lasts.
                bool isAttacked = hive.IsUnderAttack && !hive.IsDestroyed;
                bool flashOn = false;
                if (isAttacked)
                {
                    _hiveFlashLeft[index] -= deltaTime;
                    flashOn = _hiveFlashOn[index];
                    if (_hiveFlashLeft[index] <= 0f)
                    {
                        _hiveFlashLeft[index] = _flashSeconds;
                        flashOn = !flashOn;
                    }
                }
                else
                {
                    _hiveFlashLeft[index] = 0f;
                }

                if (flashOn != _hiveFlashOn[index])
                {
                    _hiveFlashOn[index] = flashOn;
                    _hivePanels[index].EnableInClassList(FlashClass, flashOn);
                }
            }
        }

        private static int BarValue(float fraction) => (int)(Mathf.Clamp01(fraction) * BarScale);
    }
}
