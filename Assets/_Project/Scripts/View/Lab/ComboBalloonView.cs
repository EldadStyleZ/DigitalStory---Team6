using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// A speech balloon over the Piper listing every combo the notes so far could become, with each letter
    /// lighting up as it is played.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Runs after the camera moves, so the balloon never trails the Piper.
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Lab/Combo Balloon View")]
    public sealed class ComboBalloonView : MonoBehaviour
    {
        private const float Padding = 8f;
        private const float Radius = 10f;
        private const float NameWidth = 72f;
        private const float NameFontSize = 13f;
        private const float LetterFontSize = 17f;
        private const float LetterWidth = 16f;
        private const float RowGap = 2f;
        private const float TailSize = 12f;
        private const float PopScale = 0.25f;

        private readonly List<BalloonRow> _rows = new List<BalloonRow>();

        private LabSession _session;
        private LabTextConfig _texts;
        private LabArtConfig _art;
        private Camera _camera;
        private VisualElement _balloon;
        private VisualElement _tail;
        private int _rowsInUse;
        private int _lastNote;
        private bool _isBuilding;
        private bool _isPopping;
        private float _endSecondsLeft;

        /// <summary>True while the balloon is on screen, for tests and debugging.</summary>
        public bool IsShowing => _balloon != null && _balloon.style.display == DisplayStyle.Flex;

        /// <summary>The combos in the balloon, for tests and debugging.</summary>
        public int ShownRows => _rowsInUse;

        /// <summary>The balloon's color, for tests and debugging.</summary>
        public Color ShownColor => _balloon.style.backgroundColor.value;

        /// <summary>Where the balloon's tip is on the panel, for tests and debugging.</summary>
        public Vector2 ShownPosition => new Vector2(_balloon.style.left.value.value, _balloon.style.top.value.value);

        /// <summary>Supplies what to show. Call once, before this object starts.</summary>
        public void Initialize(LabSession session, LabTextConfig texts, LabArtConfig art, Camera mainCamera)
        {
            _session = session;
            _texts = texts;
            _art = art;
            _camera = mainCamera;
        }

        /// <summary>The name in one row, for tests and debugging.</summary>
        public string ShownName(int row) => _rows[row].Name.text;

        /// <summary>The letters in one row, for tests and debugging.</summary>
        public string ShownLetters(int row)
        {
            var letters = string.Empty;

            foreach (Label letter in _rows[row].Letters)
            {
                letters += letter.style.display == DisplayStyle.Flex ? letter.text : string.Empty;
            }

            return letters;
        }

        /// <summary>The color of one letter, for tests and debugging.</summary>
        public Color LetterColor(int row, int letter) => _rows[row].Letters[letter].style.color.value;

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_session == null)
            {
                return;
            }

            Build(GetComponent<UIDocument>().rootVisualElement);
            _session.NotePlayed += HandleNotePlayed;
            _session.ComboBuilding += HandleBuilding;
            _session.ComboPlayed += HandlePlayed;
            _session.ComboFailed += HandleFailed;
            _session.SummonRefused += HandleRefused;
        }

        private void LateUpdate()
        {
            if (!IsShowing)
            {
                return;
            }

            FinishEnding();

            // Continuing after a death drops the combo without saying so.
            if (_isBuilding && _session.Combos.Notes.Count == 0)
            {
                Hide();
            }

            Vector2 tip = _session.Piper.Position + (Vector2.up * _art.BalloonLift);
            Vector2 onPanel = RuntimePanelUtils.CameraTransformWorldToPanel(_balloon.panel, tip, _camera);
            _balloon.style.left = onPanel.x;
            _balloon.style.top = onPanel.y;
        }

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so _balloon can be null.
            if (_balloon != null)
            {
                _session.NotePlayed -= HandleNotePlayed;
                _session.ComboBuilding -= HandleBuilding;
                _session.ComboPlayed -= HandlePlayed;
                _session.ComboFailed -= HandleFailed;
                _session.SummonRefused -= HandleRefused;
            }
        }

        // It sits first in the document so the pause and ending screens cover it.
        private void Build(VisualElement root)
        {
            _balloon = new VisualElement();
            _balloon.pickingMode = PickingMode.Ignore;
            _balloon.style.position = Position.Absolute;
            _balloon.style.display = DisplayStyle.None;
            _balloon.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-100f));
            _balloon.style.paddingLeft = Padding;
            _balloon.style.paddingRight = Padding;
            _balloon.style.paddingTop = Padding;
            _balloon.style.paddingBottom = Padding;
            _balloon.style.borderTopLeftRadius = Radius;
            _balloon.style.borderTopRightRadius = Radius;
            _balloon.style.borderBottomLeftRadius = Radius;
            _balloon.style.borderBottomRightRadius = Radius;

            _tail = new VisualElement();
            _tail.pickingMode = PickingMode.Ignore;
            _tail.style.position = Position.Absolute;
            _tail.style.width = TailSize;
            _tail.style.height = TailSize;
            _tail.style.left = Length.Percent(50f);
            _tail.style.bottom = -TailSize / 2f;
            _tail.style.marginLeft = -TailSize / 2f;
            _tail.style.rotate = new Rotate(45f);

            _balloon.Add(_tail);
            root.Insert(0, _balloon);
        }

        private void Show(Color color)
        {
            _balloon.style.display = DisplayStyle.Flex;
            _balloon.style.backgroundColor = color;
            _balloon.style.opacity = 1f;
            _balloon.style.scale = new Scale(Vector3.one);
            _tail.style.backgroundColor = color;
        }

        private void Hide()
        {
            _balloon.style.display = DisplayStyle.None;
            _isBuilding = false;
            _endSecondsLeft = 0f;
        }

        private void ShowCombos(IReadOnlyList<IComboSong> songs, int played)
        {
            for (int index = 0; index < songs.Count; index++)
            {
                FillRow(RowAt(index), songs[index], played);
            }

            UseRows(songs.Count);
        }

        private void UseRows(int count)
        {
            _rowsInUse = count;

            for (int index = 0; index < _rows.Count; index++)
            {
                _rows[index].Root.style.display = index < count ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void FillRow(BalloonRow row, IComboSong song, int played)
        {
            Color lit = Color.Lerp(_art.BalloonStartColor, _art.BalloonDoneColor, played / (float)song.Combo.Count);
            row.Name.text = song.Name;

            for (int index = 0; index < song.Combo.Count; index++)
            {
                FillLetter(row, index, _texts.NoteNames[song.Combo[index]], index < played ? lit : _art.BalloonWaitingColor, index < played);
            }

            HideLettersFrom(row, song.Combo.Count);
        }

        private void FillLetter(BalloonRow row, int index, string text, Color color, bool isPlayed)
        {
            Label letter = LetterAt(row, index);
            letter.text = text;
            letter.style.color = color;
            letter.style.unityFontStyleAndWeight = isPlayed ? FontStyle.Bold : FontStyle.Normal;
            letter.style.display = DisplayStyle.Flex;
        }

        private static void HideLettersFrom(BalloonRow row, int first)
        {
            for (int index = first; index < row.Letters.Count; index++)
            {
                row.Letters[index].style.display = DisplayStyle.None;
            }
        }

        private BalloonRow RowAt(int index)
        {
            if (index == _rows.Count)
            {
                _rows.Add(NewRow());
            }

            return _rows[index];
        }

        private BalloonRow NewRow()
        {
            var root = new VisualElement();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.marginTop = RowGap;
            root.style.marginBottom = RowGap;

            Label name = NewLabel(NameFontSize, _art.BalloonTextColor);
            name.style.width = NameWidth;
            root.Add(name);
            _balloon.Add(root);
            return new BalloonRow(root, name);
        }

        private Label LetterAt(BalloonRow row, int index)
        {
            if (index == row.Letters.Count)
            {
                Label letter = NewLabel(LetterFontSize, _art.BalloonWaitingColor);
                letter.style.width = LetterWidth;
                letter.style.unityTextAlign = TextAnchor.MiddleCenter;
                row.Root.Add(letter);
                row.Letters.Add(letter);
            }

            return row.Letters[index];
        }

        private static Label NewLabel(float fontSize, Color color)
        {
            var label = new Label();
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = fontSize;
            label.style.color = color;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.marginTop = 0f;
            label.style.marginBottom = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            return label;
        }

        private void BeginEnding(Color color, bool isPopping)
        {
            Show(color);
            _isBuilding = false;
            _isPopping = isPopping;
            _endSecondsLeft = _art.BalloonEndSeconds;
        }

        private void FinishEnding()
        {
            if (_endSecondsLeft <= 0f)
            {
                return;
            }

            _endSecondsLeft -= Time.unscaledDeltaTime;

            if (_endSecondsLeft <= 0f)
            {
                Hide();
                return;
            }

            float gone = 1f - (_endSecondsLeft / _art.BalloonEndSeconds);
            _balloon.style.opacity = 1f - (gone * gone);

            if (_isPopping)
            {
                _balloon.style.scale = new Scale(Vector3.one * (1f + (gone * PopScale)));
            }
        }

        private void HandleNotePlayed(int note) => _lastNote = note;

        private void HandleBuilding()
        {
            ShowCombos(_session.Combos.Candidates, _session.Combos.Notes.Count);
            Show(_art.BalloonColor);
            _isBuilding = true;
            _endSecondsLeft = 0f;
        }

        private void HandlePlayed(IComboSong song)
        {
            FillRow(RowAt(0), song, song.Combo.Count);
            UseRows(1);
            BeginEnding(_art.BalloonColor, true);
        }

        private void HandleRefused(SummonRefusal refusal) => BeginEnding(_art.BalloonFailedColor, false);

        // A wrong first note never built anything, so the note itself is all there is to show.
        private void HandleFailed()
        {
            if (!IsShowing || !_isBuilding)
            {
                ShowLoneNote();
            }

            BeginEnding(_art.BalloonFailedColor, false);
        }

        private void ShowLoneNote()
        {
            BalloonRow row = RowAt(0);
            row.Name.text = string.Empty;
            FillLetter(row, 0, _texts.NoteNames[_lastNote], _art.BalloonTextColor, true);
            HideLettersFrom(row, 1);
            UseRows(1);
        }

        private sealed class BalloonRow
        {
            public BalloonRow(VisualElement root, Label name)
            {
                Root = root;
                Name = name;
            }

            public VisualElement Root { get; }
            public Label Name { get; }
            public List<Label> Letters { get; } = new List<Label>();
        }
    }
}
