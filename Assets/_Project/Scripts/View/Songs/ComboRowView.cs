using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Lists every song's keys, lights one up when it is cast and dims it while it rests.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Combo Row View")]
    public sealed class ComboRowView : MonoBehaviour
    {
        private const float RowMargin = 10f;
        private const float ChipGap = 3f;
        private const float ChipSidePadding = 8f;
        private const float ChipEndPadding = 3f;
        private const float ChipRadius = 8f;
        private const float ChipBorder = 2f;
        private const float TitleFontSize = 13f;
        private const float TitleGap = 6f;
        private const float KeySize = 17f;
        private const float KeyFontSize = 11f;
        private const float KeyGap = 2f;
        private const float RestingOpacity = .4f;
        private const float GlowSeconds = .9f;

        private static readonly Color TextColor = new Color32(0xF3, 0xEA, 0xD6, 0xFF);
        private static readonly Color KeyTextColor = new Color32(0x1A, 0x12, 0x09, 0xFF);
        private static readonly Color ChipColor = new Color32(0x12, 0x14, 0x22, 0xD6);
        private static readonly Color GlowColor = new Color32(0xE8, 0xC5, 0x47, 0xFF);

        private NightSession _session;
        private TownArtConfig _art;
        private VisualElement[] _chips;
        private string[] _chipTexts;
        private float[] _glowLeft;
        private bool[] _isGlowing;
        private bool[] _isDimmed;

        /// <summary>How many songs the row lists, for tests and debugging.</summary>
        public int ChipCount => _chips?.Length ?? 0;

        /// <summary>Supplies the songs to show. Call once, before this object is enabled.</summary>
        public void Initialize(NightSession session, TownArtConfig art)
        {
            _session = session;
            _art = art;

            int count = session.Songs.Count;
            _chips = new VisualElement[count];
            _chipTexts = new string[count];
            _glowLeft = new float[count];
            _isGlowing = new bool[count];
            _isDimmed = new bool[count];
        }

        /// <summary>The words on one song's chip, for tests and debugging.</summary>
        public string ChipText(int song) => _chipTexts[song];

        /// <summary>True while a song is lit up, for tests and debugging.</summary>
        public bool IsGlowing(int song) => _isGlowing[song];

        /// <summary>True while a song is dimmed, for tests and debugging.</summary>
        public bool IsDimmed(int song) => _isDimmed[song];

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.SongCast += HandleSongCast;
            }
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                BuildRow(GetComponent<UIDocument>().rootVisualElement);
            }
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            for (int song = 0; song < _chips.Length; song++)
            {
                _glowLeft[song] = Mathf.Max(0f, _glowLeft[song] - Time.deltaTime);
                ShowGlow(song, _glowLeft[song] > 0f);
                ShowRest(song, _session.Songs.IsCoolingDown(song));
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _session can be null.
            if (_session != null)
            {
                _session.SongCast -= HandleSongCast;
            }
        }

        private void BuildRow(VisualElement root)
        {
            var row = new VisualElement();
            row.style.position = Position.Absolute;
            row.style.left = RowMargin;
            row.style.right = RowMargin;
            row.style.bottom = RowMargin;
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.justifyContent = Justify.Center;

            for (int song = 0; song < _chips.Length; song++)
            {
                _chips[song] = BuildChip(_session.Songs[song]);
                _chipTexts[song] = TextOf(_session.Songs[song]);
                row.Add(_chips[song]);
            }

            root.Add(row);
        }

        private VisualElement BuildChip(SongDefinition song)
        {
            VisualElement chip = NewChip();

            var title = new Label(song.Title);
            title.style.color = TextColor;
            title.style.fontSize = TitleFontSize;
            title.style.marginRight = TitleGap;
            title.style.paddingLeft = 0f;
            title.style.paddingRight = 0f;
            chip.Add(title);

            foreach (int key in song.Keys)
            {
                chip.Add(NewKey(key));
            }

            return chip;
        }

        private static VisualElement NewChip()
        {
            var chip = new VisualElement();
            chip.style.flexDirection = FlexDirection.Row;
            chip.style.alignItems = Align.Center;
            chip.style.marginLeft = ChipGap;
            chip.style.marginRight = ChipGap;
            chip.style.marginTop = ChipGap;
            chip.style.marginBottom = ChipGap;
            chip.style.paddingLeft = ChipSidePadding;
            chip.style.paddingRight = ChipSidePadding;
            chip.style.paddingTop = ChipEndPadding;
            chip.style.paddingBottom = ChipEndPadding;
            chip.style.borderTopLeftRadius = ChipRadius;
            chip.style.borderTopRightRadius = ChipRadius;
            chip.style.borderBottomLeftRadius = ChipRadius;
            chip.style.borderBottomRightRadius = ChipRadius;
            chip.style.borderTopWidth = ChipBorder;
            chip.style.borderBottomWidth = ChipBorder;
            chip.style.borderLeftWidth = ChipBorder;
            chip.style.borderRightWidth = ChipBorder;
            chip.style.backgroundColor = ChipColor;
            SetBorderColor(chip, Color.clear);
            return chip;
        }

        private Label NewKey(int key)
        {
            var label = new Label(HoleName(key));
            label.style.width = KeySize;
            label.style.height = KeySize;
            label.style.marginLeft = KeyGap;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            label.style.borderTopLeftRadius = KeySize / 2f;
            label.style.borderTopRightRadius = KeySize / 2f;
            label.style.borderBottomLeftRadius = KeySize / 2f;
            label.style.borderBottomRightRadius = KeySize / 2f;
            label.style.backgroundColor = _art.HoleColor(key);
            label.style.color = KeyTextColor;
            label.style.fontSize = KeyFontSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        private static string TextOf(SongDefinition song)
        {
            return string.Join(" ", new[] { song.Title }.Concat(song.Keys.Select(HoleName)));
        }

        // Players know the holes from 1, but the rules count them from 0.
        private static string HoleName(int key) => (key + 1).ToString();

        private static void SetBorderColor(VisualElement chip, Color color)
        {
            chip.style.borderTopColor = color;
            chip.style.borderBottomColor = color;
            chip.style.borderLeftColor = color;
            chip.style.borderRightColor = color;
        }

        private void ShowGlow(int song, bool isGlowing)
        {
            if (_isGlowing[song] != isGlowing)
            {
                _isGlowing[song] = isGlowing;
                SetBorderColor(_chips[song], isGlowing ? GlowColor : Color.clear);
            }
        }

        private void ShowRest(int song, bool isDimmed)
        {
            if (_isDimmed[song] != isDimmed)
            {
                _isDimmed[song] = isDimmed;
                _chips[song].style.opacity = isDimmed ? RestingOpacity : 1f;
            }
        }

        private void HandleSongCast(int song) => _glowLeft[song] = GlowSeconds;
    }
}
