using UnityEngine;
using UnityEngine.UIElements;

namespace PiperToy
{
    /// <summary>
    /// The flute along the bottom of the screen, as in the old game. A hole lights up while it is held, from the
    /// keyboard or the mouse, and pressing a hole with the mouse plays the flute just like its key does.
    /// </summary>
    /// <remarks>The six holes are laid out in ToyHud.uxml, named hole-1 to hole-6, and styled in ToyHud.uss.</remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FluteBar : MonoBehaviour
    {
        private const string HeldClass = "hole--held";

        [SerializeField, Tooltip("The flute the holes play.")]
        private Flute _flute;

        private readonly VisualElement[] _holes = new VisualElement[Flute.HoleCount];
        private int _keyUnderTheMouse;

        public bool IsLit(int key) => _holes[key - 1].ClassListContains(HeldClass);

        // Start runs after every OnEnable, so the UIDocument has built its layout by then.
        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

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
