using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PiperToy
{
    /// <summary>
    /// The combos row along the bottom of the screen, as in the old game: one chip per song with its name and keys.
    /// A chip glows in its song's color when the song plays, and fades while the song rests.
    /// </summary>
    /// <remarks>
    /// The layout is in ToyHud.uxml and the look in ToyHud.uss, which UI Builder can open. Each chip is a copy of
    /// ComboChip.uxml; this script only fills in the songs and lights them up.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ComboRow : MonoBehaviour
    {
        private const string RestingClass = "chip--resting";
        private const string GlowingClass = "chip--glowing";

        [SerializeField, Tooltip("The flute whose songs the row shows.")]
        private Flute _flute;

        [SerializeField, Tooltip("The chip copied once for every song.")]
        private VisualTreeAsset _chipTemplate;

        [SerializeField, Min(0f), Tooltip("How long a chip glows after its song plays, in seconds.")]
        private float _glowSeconds = 1.2f;

        private readonly List<VisualElement> _chips = new List<VisualElement>();
        private readonly List<string> _chipTexts = new List<string>();
        private readonly Dictionary<Song, float> _glowingUntil = new Dictionary<Song, float>();

        /// <summary>How many songs the row shows, for tests.</summary>
        public int ChipCount => _chips.Count;

        /// <summary>The words on one chip, such as "Dance 1 3 5", for tests.</summary>
        public string ChipText(int song) => _chipTexts[song];

        public bool IsGlowing(int song) => _chips[song].ClassListContains(GlowingClass);

        public bool IsResting(int song) => _chips[song].ClassListContains(RestingClass);

        private void OnEnable()
        {
            _flute.SongPlayed += HandleSongPlayed;
        }

        private void OnDisable()
        {
            _flute.SongPlayed -= HandleSongPlayed;
        }

        // Start runs after every OnEnable, so the UIDocument has built its layout by then.
        private void Start()
        {
            VisualElement row = GetComponent<UIDocument>().rootVisualElement.Q("combo-row");
            row.Clear();

            foreach (Song song in _flute.Songs)
            {
                VisualElement chip = NewChip(song);
                row.Add(chip);
                _chips.Add(chip);
            }
        }

        private void Update()
        {
            for (int index = 0; index < _chips.Count; index++)
            {
                Song song = _flute.Songs[index];
                bool isGlowing = _glowingUntil.TryGetValue(song, out float until) && Time.time < until;

                _chips[index].EnableInClassList(GlowingClass, isGlowing);
                _chips[index].EnableInClassList(RestingClass, !isGlowing && !_flute.IsReady(song));
                ShowBorder(_chips[index], isGlowing ? new StyleColor(song.Color) : new StyleColor(StyleKeyword.Null));
            }
        }

        // A color set from code beats the style sheet, so clearing it lets ToyHud.uss decide the border again.
        private static void ShowBorder(VisualElement chip, StyleColor color)
        {
            chip.style.borderTopColor = color;
            chip.style.borderBottomColor = color;
            chip.style.borderLeftColor = color;
            chip.style.borderRightColor = color;
        }

        private VisualElement NewChip(Song song)
        {
            // Instantiate makes a fresh copy of the template; the chip is its first child.
            VisualElement chip = _chipTemplate.Instantiate()[0];
            chip.Q<Label>("title").text = song.Title;

            VisualElement keys = chip.Q("keys");
            string text = song.Title;

            foreach (int key in song.Keys)
            {
                var label = new Label(key.ToString());
                label.AddToClassList("key");
                keys.Add(label);
                text += $" {key}";
            }

            _chipTexts.Add(text);
            return chip;
        }

        private void HandleSongPlayed(Song song)
        {
            _glowingUntil[song] = Time.time + _glowSeconds;
        }
    }
}
