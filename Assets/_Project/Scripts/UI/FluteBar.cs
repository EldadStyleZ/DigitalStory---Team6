using UnityEngine;
using UnityEngine.UIElements;

namespace PiperToy
{
    /// <summary>
    /// The team's flute on the side of the screen. While a note sounds it shows the picture of that hole pressed,
    /// and pressing a hole with the mouse plays the flute just like its key does.
    /// </summary>
    /// <remarks>
    /// The flute and its six holes are laid out in ToyHud.uxml, named flute and hole-1 to hole-6, and styled in
    /// ToyHud.uss. The holes sit over the holes in the picture.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FluteBar : MonoBehaviour
    {
        private const string HeldClass = "hole--held";

        [SerializeField, Tooltip("The flute the holes play.")]
        private Flute _flute;

        [SerializeField, Tooltip("The flute's picture with every hole open.")]
        private Sprite _openFlute;

        [SerializeField, Tooltip("The flute's picture with each hole pressed, from hole 1 to hole 6.")]
        private Sprite[] _pressedFlutes = new Sprite[Flute.HoleCount];

        private readonly VisualElement[] _holes = new VisualElement[Flute.HoleCount];
        private VisualElement _picture;
        private int _keyUnderTheMouse;

        /// <summary>The flute picture on screen now, for tests.</summary>
        public Sprite ShownPicture { get; private set; }

        public bool IsLit(int key) => _holes[key - 1].ClassListContains(HeldClass);

        // Start runs after every OnEnable, so the UIDocument has built its layout by then.
        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _picture = root.Q("flute");

            for (int key = 1; key <= Flute.HoleCount; key++)
            {
                int thisKey = key;
                VisualElement hole = root.Q($"hole-{key}");

                // Capturing the pointer keeps the note playing until the button comes up, even if the mouse slips off.
                hole.RegisterCallback<PointerDownEvent>(down =>
                {
                    PressWithTheMouse(thisKey);
                    hole.CapturePointer(down.pointerId);
                });
                hole.RegisterCallback<PointerUpEvent>(up => hole.ReleasePointer(up.pointerId));
                hole.RegisterCallback<PointerCaptureOutEvent>(_ => ReleaseTheMouse());
                _holes[key - 1] = hole;
            }
        }

        private void Update()
        {
            for (int key = 1; key <= Flute.HoleCount; key++)
            {
                _holes[key - 1].EnableInClassList(HeldClass, _flute.IsHeld(key));
            }

            ShowPicture(_flute.SoundingKey == 0 ? _openFlute : _pressedFlutes[_flute.SoundingKey - 1]);
        }

        // Only a new picture is set, so nothing changes on the many frames when the same note keeps sounding.
        private void ShowPicture(Sprite picture)
        {
            if (picture != ShownPicture)
            {
                ShownPicture = picture;
                _picture.style.backgroundImage = new StyleBackground(picture);
            }
        }

        private void PressWithTheMouse(int key)
        {
            ReleaseTheMouse();
            _keyUnderTheMouse = key;
            _flute.Press(key);
        }

        private void ReleaseTheMouse()
        {
            if (_keyUnderTheMouse != 0)
            {
                _flute.Release(_keyUnderTheMouse);
                _keyUnderTheMouse = 0;
            }
        }
    }
}
