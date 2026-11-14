using System;
using System.Globalization;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Shows the hive defense HUD: wasps over the capacity, the swarm's level, nests, Fury, the songbook of found songs, recent notes, hives, the phase and the first round's lesson. It also creates and
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
        private const string NoteShownClass = "note--shown";
        private const string NoteHintClass = "note--hint";
        private const string LineShownClass = "line--shown";
        private const string HoleClassPrefix = "hole-";
        private const int HoleColours = 6;
        private const float BarScale = 1000f;

        [SerializeField, Tooltip("The swarm's look: the category colours.")]
        private SwarmConfig _swarmConfig;

        [SerializeField, Tooltip("The nests' numbers: how many wasps a nest gives and how many notes it hums.")]
        private NestConfig _nestConfig;

        [SerializeField, Min(0.1f), Tooltip("How long the 'A nest hums' line stays, in seconds.")]
        private float _humSeconds = 5f;

        [SerializeField, Min(0.1f), Tooltip("How long the 'Level 3' flash stays after a nest is collected, in seconds.")]
        private float _levelSeconds = 2.5f;

        [SerializeField, Min(0.1f), Tooltip("How long the 'New song' line stays, in seconds.")]
        private float _discoverySeconds = 3f;

        [SerializeField, Min(0.1f), Tooltip("How long a refusal line ('Not enough wasps') stays, in seconds.")]
        private float _notReadySeconds = 1.2f;

        [SerializeField, Min(0.1f), Tooltip("How long the banner at a phase change ('Wave 2', 'Wave cleared') stays, in seconds.")]
        private float _bannerSeconds = 2f;

        [SerializeField, Min(0.1f), Tooltip("How long '+N wasps' stays after a wave is called early, in seconds.")]
        private float _grantedSeconds = 2f;

        [SerializeField, Tooltip("The hint shown in a farm phase.")]
        private string _callWaveHint = "Enter: call the wave early";

        [SerializeField, Tooltip("The hint under a damaged hive the core is near.")]
        private string _holdToHealText = "Hold E to heal";

        [SerializeField, Tooltip("The hint under a hive while wasps flow into it.")]
        private string _healingText = "Healing";

        [SerializeField, Tooltip("Lesson step 1: moving.")]
        private string _moveText = "Move with W A S D.";

        [SerializeField, Tooltip("Lesson step 2: the first song. The notes are the Sting's.")]
        private string _stingText = "Play J K H.";

        [SerializeField, Tooltip("Lesson step after the Sting: collecting a nest.")]
        private string _collectNestText = "Fly to the glowing nest and play the notes shown.";

        [SerializeField, Tooltip("Lesson step 3: taking wasps from a hive.")]
        private string _visitHiveText = "Fly to a hive to take its wasps.";

        [SerializeField, Tooltip("Lesson step 4: giving wasps back. Only the hurt hive can take them, so the prompt names it.")]
        private string _healHiveText = "Hold E at the hurt hive to give wasps back and heal it.";

        [SerializeField, Tooltip("Lesson step 5: calling the wave.")]
        private string _callWaveText = "Enemies come when the timer ends. Press Enter when ready.";

        [SerializeField, Tooltip("The line while the swarm is gone and returning. {0} is the whole seconds left.")]
        private string _swarmReturnsText = "The swarm returns in {0}";

        [SerializeField, Tooltip("The line while a hive is under attack and the Dome is known.")]
        private string _hiveUnderAttackText = "Hive under attack: the Dome shields what is inside it";

        [SerializeField, Min(0.05f), Tooltip("Seconds between the alarm colour and normal while a hive is under attack.")]
        private float _flashSeconds = 0.25f;

        [SerializeField, Min(1), Tooltip("How many recent notes there is room for. Match SongConfig.NotesRemembered.")]
        private int _noteSlots = 5;

        [SerializeField, Tooltip("The camera that looks at the hives, for the off-screen arrows. Main Camera when empty.")]
        private Camera _camera;

        private HiveDefenseSession _session;
        private bool _isReady;
        private HiveOffscreenIndicatorView _indicator;
        private NestOffscreenIndicatorView _nestIndicator;
        private NestHudView _nestHud;
        private NestComboView _nestCombo;
        private EndScreenView _endScreen;
        private GiantHudView _giantHud;

        private Label _waspCount;
        private VisualElement _waspFill;
        private VisualElement _furyPanel;
        private VisualElement _furyFill;
        private SongbookView _songbook;
        private PhaseView _phase;
        private LessonView _lesson;
        private HivePanelsView _hives;
        private Label _furyKeys;
        private Label _messageLine;
        private Label _swarmLine;
        private Label _domeLine;
        private DamageNumbersView _damageNumbers;
        private HitSoundView _hitSound;
        private int _shownReturnSeconds = -1;
        private bool _isDomeLineShown;
        private int _colossusSong = -1;
        private bool _isFuryRevealed;
        private float _messageLeft;
        private Label[] _noteCells;
        private bool[] _noteHints;
        private int[] _noteHoles;
        private int _shownWasps = -1;
        private int _shownMax = -1;
        private bool _isLow;
        private bool _isLowKnown;
        private int _shownWaspBar = -1;
        private int _shownFuryBar = -1;
        private bool _isFuryFull;
        private string _furyKeysText;
        private ControlDevice _device;

        /// <summary>The text on the wasp count, for tests and debugging.</summary>
        public string WaspCountText => _waspCount?.text;

        /// <summary>The device whose controls the HUD names, for tests and debugging.</summary>
        public ControlDevice ShownDevice => _device;

        /// <summary>The nest combo prompt (plan section U), for tests.</summary>
        public NestComboView NestCombo => _nestCombo;

        /// <summary>Builds the HUD's cells from the match. Call once, before the first Update.</summary>
        /// <exception cref="ArgumentNullException">The session is missing.</exception>
        /// <exception cref="InvalidOperationException">The UXML lacks an element, or there is no camera.</exception>
        public void Initialize(HiveDefenseSession session)
        {
            _isReady = false;
            ReleaseSongs();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _device = ControlDevice.KeyboardMouse;

            VisualElement root = GetComponent<UIDocument>().rootVisualElement
                ?? throw new InvalidOperationException("The UIDocument has no Visual Tree Asset. Set HiveDefenseHud.uxml.");

            _waspCount = Require<Label>(root, "wasp-count");
            _waspFill = Require<VisualElement>(root, "wasp-fill");
            if (_swarmConfig == null)
            {
                throw new InvalidOperationException("The HUD needs the SwarmConfig. Set it on the HiveDefenseHudView.");
            }

            if (_nestConfig == null)
            {
                throw new InvalidOperationException("The HUD needs the NestConfig. Set it on the HiveDefenseHudView.");
            }

            _furyPanel = Require<VisualElement>(root, "fury-panel");
            _furyFill = Require<VisualElement>(root, "fury-fill");
            _furyKeys = Require<Label>(root, "fury-keys");
            Label lessonLine = Require<Label>(root, "prompt-line");
            _messageLine = Require<Label>(root, "message-line");
            VisualElement songbook = Require<VisualElement>(root, "songbook");
            VisualElement topCenter = Require<VisualElement>(root, "top-center");
            VisualElement notes = Require<VisualElement>(root, "notes");
            VisualElement hives = Require<VisualElement>(root, "hives");
            VisualElement offscreen = Require<VisualElement>(root, "offscreen-layer");
            VisualElement endScreen = Require<VisualElement>(root, "end-screen");

            _swarmLine = Require<Label>(root, "swarm-line");
            _domeLine = Require<Label>(root, "dome-line");
            _domeLine.text = _hiveUnderAttackText;
            _swarmLine.RemoveFromClassList(LineShownClass);
            _domeLine.RemoveFromClassList(LineShownClass);
            _shownReturnSeconds = -1;
            _isDomeLineShown = false;
            _colossusSong = ColossusSongIndex();
            _isFuryRevealed = false;
            _furyKeysText = null;
            ControlSymbols.ShowPrompt(_furyKeys, string.Empty, _device);
            _songbook = new SongbookView(session, _swarmConfig, songbook);
            session.Discovery.Discovered += OnDiscovered;
            session.Caster.SongNotReady += OnSongNotReady;
            _phase = new PhaseView(session, topCenter, _callWaveHint, _bannerSeconds, _grantedSeconds);
            _lesson = new LessonView(session.Tutorial, lessonLine, _moveText, _stingText, _collectNestText, _visitHiveText, _healHiveText, _callWaveText);
            _messageLeft = 0f;
            _messageLine.RemoveFromClassList(LineShownClass);
            _nestHud = new NestHudView(session, _nestConfig, root, _humSeconds, _levelSeconds);
            _nestCombo = new NestComboView(session.Nests, _nestConfig, root);
            BuildNotes(notes);
            _hives = new HivePanelsView(session, hives, _flashSeconds, _holdToHealText, _healingText);

            Camera camera = _camera != null ? _camera : Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("The HUD needs a camera for the off-screen hive arrows. Set one, or tag the main camera.");
            }

            // The edge arrows keep clear of every block of the HUD, so none of them ever covers a panel or a line.
            VisualElement[] keepOut =
            {
                Require<VisualElement>(root, "top-left"), topCenter, hives, Require<VisualElement>(root, "bottom"), songbook,
            };
            _indicator = GetOrAdd<HiveOffscreenIndicatorView>();
            _indicator.Initialize(session.Hives, camera, offscreen, keepOut);
            _nestIndicator = GetOrAdd<NestOffscreenIndicatorView>();
            _nestIndicator.Initialize(session.Nests, camera, offscreen, keepOut);
            _damageNumbers = GetOrAdd<DamageNumbersView>();
            _damageNumbers.Initialize(session, camera, root);
            _hitSound = GetOrAdd<HitSoundView>();
            _hitSound.Initialize(session);
            _endScreen = GetOrAdd<EndScreenView>();
            _endScreen.Initialize(session, endScreen);
            _giantHud = GetOrAdd<GiantHudView>();
            _giantHud.Initialize(session);
            _isReady = true; // Last, so Update never runs on a half-built HUD.
        }

        /// <summary>
        /// Names the controls of <paramref name="device"/> everywhere the HUD names a key: the notes, the songbook, the lesson,
        /// the hints and the giant's lines. Call it after <see cref="Initialize"/> and whenever the player switches device.
        /// </summary>
        public void ShowControls(ControlDevice device)
        {
            if (!_isReady || device == _device)
            {
                return;
            }

            _device = device;
            if (_furyKeysText != null)
            {
                ControlSymbols.ShowPrompt(_furyKeys, _furyKeysText, device);
            }

            for (int slot = 0; slot < _noteCells.Length; slot++)
            {
                if (_noteHoles[slot] >= 0)
                {
                    ControlSymbols.ShowHole(_noteCells[slot], _noteHoles[slot], device);
                }
            }

            _songbook.ShowControls(device);
            _phase.ShowControls(device);
            _lesson.ShowControls(device);
            _nestHud.ShowControls(device);
            _nestCombo.ShowControls(device);
            _hives.ShowControls(device);
            _endScreen.ShowControls(device);
            _giantHud.ShowControls(device);
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            ShowWasps();
            ShowFury();
            _phase.Tick(Time.deltaTime);
            _lesson.Tick();
            _songbook.Tick(Time.deltaTime);
            _nestHud.Tick(Time.deltaTime);
            _nestCombo.Tick(Time.unscaledDeltaTime);
            ShowNotes();
            ShowLines(Time.deltaTime);
            ShowSwarmLine();
            ShowDomeLine();
            _hives.Tick(Time.deltaTime);
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

            if (_phase != null)
            {
                _phase.Dispose();
                _phase = null;
            }

            if (_nestHud != null)
            {
                _nestHud.Dispose();
                _nestHud = null;
            }

            if (_nestCombo != null)
            {
                _nestCombo.Dispose();
                _nestCombo = null;
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
        private int ColossusSongIndex() => SongIndexOf<ColossusSong>();

        private int SongIndexOf<T>() where T : SwarmSongEffect
        {
            SwarmSongCaster caster = _session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is T)
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

        private static int HoleColour(int hole) => ((hole % HoleColours) + HoleColours) % HoleColours;

        private void ShowWasps()
        {
            SwarmBody swarm = _session.Swarm;
            int count = swarm.Count;
            int max = swarm.Capacity;
            if (count != _shownWasps || max != _shownMax)
            {
                _shownWasps = count;
                _shownMax = max;
                _waspCount.text = count.ToString("N0", CultureInfo.InvariantCulture)
                    + " / " + max.ToString("N0", CultureInfo.InvariantCulture);
            }

            bool isLow = swarm.IsLow;
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
                _furyKeysText = KeysText(_session.Caster[_colossusSong].Song);
                ControlSymbols.ShowPrompt(_furyKeys, _furyKeysText, _device);
            }

            if (fury.IsFull != _isFuryFull)
            {
                _isFuryFull = fury.IsFull;
                _furyPanel.EnableInClassList(FuryFullClass, _isFuryFull);
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
                    ControlSymbols.ShowHole(cell, hole, _device);
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

        // While the swarm is gone: "The swarm returns in 3". The text is rebuilt only when the whole second changes.
        private void ShowSwarmLine()
        {
            SwarmBody swarm = _session.Swarm;
            if (!swarm.IsCollapsed)
            {
                if (_shownReturnSeconds != -1)
                {
                    _shownReturnSeconds = -1;
                    _swarmLine.RemoveFromClassList(LineShownClass);
                }

                return;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(swarm.SecondsToRespawn));
            if (seconds == _shownReturnSeconds)
            {
                return;
            }

            if (_shownReturnSeconds == -1)
            {
                _swarmLine.AddToClassList(LineShownClass);
            }

            _shownReturnSeconds = seconds;
            _swarmLine.text = string.Format(CultureInfo.InvariantCulture, _swarmReturnsText, seconds);
        }

        // While a hive is under attack and the player knows the Dome (HiveDefenseSession.ShowsDomeHint): one short line that
        // says what the Dome is for.
        private void ShowDomeLine()
        {
            bool isShown = _session.ShowsDomeHint;
            if (isShown == _isDomeLineShown)
            {
                return;
            }

            _isDomeLineShown = isShown;
            _domeLine.EnableInClassList(LineShownClass, isShown);
        }

        private void ShowLines(float deltaTime)
        {
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
        private void OnSongNotReady(int song, SongRefusal refusal)
        {
            string text;
            switch (refusal)
            {
                case SongRefusal.NotEnoughWasps:
                    text = "Not enough wasps";
                    break;
                case SongRefusal.NeedsFury:
                    text = "Needs full Fury";
                    break;
                case SongRefusal.Collapsed:
                    text = "The swarm is too small";
                    break;
                case SongRefusal.GiantStanding:
                    text = "The giant is standing";
                    break;
                default:
                    text = _session.Discovery.IsKnown(song) ? _session.Caster[song].Song.Title + " is not ready" : "Not ready";
                    break;
            }

            ShowMessage(text, Color.clear, _notReadySeconds);
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

        private static int BarValue(float fraction) => (int)(Mathf.Clamp01(fraction) * BarScale);
    }
}
