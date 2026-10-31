using DigitalStory.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>The mute button and the M key. The choice is remembered, so it survives Retry.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Mute View")]
    public sealed class MuteView : MonoBehaviour
    {
        /// <summary>Where the choice is saved, for tests and debugging.</summary>
        public const string MutedKey = "DigitalStory.Muted";

        private const float TopMargin = 12f;

        // Clear of the minimap, which sits in the corner.
        private const float RightMargin = 170f;
        private const float FontSize = 13f;
        private const float SidePadding = 10f;
        private const float EndPadding = 5f;
        private const float Radius = 8f;

        private HudLayout _layout;
        private MenuTextConfig _texts;
        private SoundConfig _sound;
        private HudConfig _hud;

        /// <summary>True while the game is silent.</summary>
        public bool IsMuted { get; private set; }

        /// <summary>The button on screen, for tests and debugging.</summary>
        public Button Button { get; private set; }

        /// <summary>Supplies what to show. Call once, before this object starts.</summary>
        public void Initialize(HudLayout layout, MenuTextConfig texts, SoundConfig sound, HudConfig hud)
        {
            _layout = layout;
            _texts = texts;
            _sound = sound;
            _hud = hud;
        }

        /// <summary>Turns the sound off, or back on.</summary>
        public void Toggle()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ShowTheChoice();
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_layout == null)
            {
                return;
            }

            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
            Button = NewButton();
            _layout.Layer.Add(Button);
            _layout.AddControl(Button);
            ShowTheChoice();
        }

        private void Update()
        {
            if (Button != null && Input.GetKeyDown(KeyCode.M))
            {
                Toggle();
            }
        }

        private Button NewButton()
        {
            // Without this, Space would press it, and Space is also a flute hole.
            var button = new Button(Toggle) { focusable = false };
            button.style.position = Position.Absolute;
            button.style.top = TopMargin;
            button.style.right = RightMargin;
            button.style.fontSize = FontSize;
            button.style.color = _hud.TextColor;
            button.style.backgroundColor = _hud.PanelColor;
            button.style.paddingLeft = SidePadding;
            button.style.paddingRight = SidePadding;
            button.style.paddingTop = EndPadding;
            button.style.paddingBottom = EndPadding;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderTopLeftRadius = Radius;
            button.style.borderTopRightRadius = Radius;
            button.style.borderBottomLeftRadius = Radius;
            button.style.borderBottomRightRadius = Radius;
            return button;
        }

        private void ShowTheChoice()
        {
            AudioListener.volume = IsMuted ? 0f : _sound.MasterVolume;
            Button.text = IsMuted ? _texts.UnmuteButton : _texts.MuteButton;
        }
    }
}
