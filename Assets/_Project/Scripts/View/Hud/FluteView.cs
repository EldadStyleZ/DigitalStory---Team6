using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>The flute on screen: it can be played with the pointer, and it shows which hole sounds.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Flute View")]
    public sealed class FluteView : MonoBehaviour
    {
        private const int HolesPerGroup = 2;
        private const float TubeTopMargin = 4f;
        private const float TubeSidePadding = 22f;
        private const float TubeEndPadding = 3f;
        private const float TubeRadius = 14f;
        private const float GroupGap = 14f;
        private const float GroupLabelFontSize = 11f;
        private const float HoleSize = 30f;
        private const float HoleGap = 4f;
        private const float HoleFontSize = 12f;

        private NightSession _session;
        private FluteConfig _flute;
        private HudLayout _layout;
        private MenuTextConfig _texts;
        private TownArtConfig _art;
        private HudConfig _hud;
        private VisualElement _tube;
        private Label[] _holes;
        private int _litHole = Flute.NoHole;
        private int _heldHole = Flute.NoHole;
        private int _pressedHoles;

        /// <summary>One bit per hole held with the pointer.</summary>
        public int HeldHoles => _heldHole == Flute.NoHole ? 0 : 1 << _heldHole;

        /// <summary>True while the pointer is down on the flute, so a press does not also walk the Piper.</summary>
        public bool IsBeingPlayed { get; private set; }

        /// <summary>How many holes are on screen, for tests and debugging.</summary>
        public int HoleCount => _holes?.Length ?? 0;

        /// <summary>True while the flute looks out of breath, for tests and debugging.</summary>
        public bool IsDimmed { get; private set; }

        /// <summary>Supplies what to show. Call once, before this object starts.</summary>
        public void Initialize(
            NightSession session,
            FluteConfig flute,
            HudLayout layout,
            MenuTextConfig texts,
            TownArtConfig art,
            HudConfig hud)
        {
            _session = session;
            _flute = flute;
            _layout = layout;
            _texts = texts;
            _art = art;
            _hud = hud;
        }

        /// <summary>True while a hole is lit, for tests and debugging.</summary>
        public bool IsLit(int hole) => hole == _litHole;

        /// <summary>Starts a press on the flute, on a hole or between holes.</summary>
        public void StartPlaying(int hole)
        {
            IsBeingPlayed = true;
            SlideTo(hole);
        }

        /// <summary>Slides the press to another hole, or off the holes.</summary>
        public void SlideTo(int hole)
        {
            if (hole == _heldHole)
            {
                return;
            }

            _heldHole = hole;

            if (hole != Flute.NoHole)
            {
                _pressedHoles |= 1 << hole;
            }
        }

        public void StopPlaying()
        {
            IsBeingPlayed = false;
            _heldHole = Flute.NoHole;
        }

        /// <summary>Returns one bit per hole pressed since the last call, and forgets them.</summary>
        public int TakePressedHoles()
        {
            int pressed = _pressedHoles;
            _pressedHoles = 0;
            return pressed;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_session != null)
            {
                BuildFlute(_layout.FluteSlot);
            }
        }

        private void Update()
        {
            if (_holes != null)
            {
                ShowLitHole(_session.Flute.CurrentHole);
                ShowBreath(_session.Breath.IsExhausted);
            }
        }

        private void BuildFlute(VisualElement parent)
        {
            _holes = new Label[_flute.HoleCount];
            _tube = NewTube();
            string[] groupLabels = { _texts.CallLabel, _texts.MarchLabel, _texts.ScareLabel };

            for (int group = 0; group < groupLabels.Length; group++)
            {
                _tube.Add(BuildGroup(group, groupLabels[group]));
            }

            _tube.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            _tube.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            _tube.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            _tube.RegisterCallback<PointerCaptureOutEvent>(HandlePointerCaptureOut);
            parent.Add(_tube);
        }

        private VisualElement NewTube()
        {
            var tube = new VisualElement();
            tube.style.alignSelf = Align.Center;
            tube.style.flexDirection = FlexDirection.Row;
            tube.style.marginTop = TubeTopMargin;
            tube.style.paddingLeft = TubeSidePadding;
            tube.style.paddingRight = TubeSidePadding;
            tube.style.paddingTop = TubeEndPadding;
            tube.style.paddingBottom = TubeEndPadding;
            tube.style.borderTopLeftRadius = TubeRadius;
            tube.style.borderTopRightRadius = TubeRadius;
            tube.style.borderBottomLeftRadius = TubeRadius;
            tube.style.borderBottomRightRadius = TubeRadius;
            tube.style.backgroundColor = _hud.FluteColor;
            return tube;
        }

        private VisualElement BuildGroup(int group, string label)
        {
            int firstHole = group * HolesPerGroup;

            var column = new VisualElement();
            column.style.marginLeft = group == 0 ? 0f : GroupGap;
            column.style.alignItems = Align.Center;

            var word = new Label(label);
            word.pickingMode = PickingMode.Ignore;
            word.style.fontSize = GroupLabelFontSize;
            word.style.color = _hud.HoleColor;
            word.style.paddingTop = 0f;
            word.style.paddingBottom = 0f;
            column.Add(word);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            for (int hole = firstHole; hole < firstHole + HolesPerGroup; hole++)
            {
                _holes[hole] = NewHole(hole);
                row.Add(_holes[hole]);
            }

            column.Add(row);
            return column;
        }

        private Label NewHole(int hole)
        {
            var label = new Label((hole + 1).ToString());
            label.style.width = HoleSize;
            label.style.height = HoleSize;
            label.style.marginLeft = HoleGap;
            label.style.marginRight = HoleGap;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            label.style.paddingTop = 0f;
            label.style.paddingBottom = 0f;
            label.style.borderTopLeftRadius = HoleSize / 2f;
            label.style.borderTopRightRadius = HoleSize / 2f;
            label.style.borderBottomLeftRadius = HoleSize / 2f;
            label.style.borderBottomRightRadius = HoleSize / 2f;
            label.style.fontSize = HoleFontSize;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return PaintHole(label, hole, false);
        }

        private Label PaintHole(Label label, int hole, bool isLit)
        {
            label.style.backgroundColor = isLit ? _art.HoleColor(hole) : _hud.HoleColor;
            label.style.color = isLit ? _hud.KeyTextColor : _hud.HoleNumberColor;
            return label;
        }

        private void ShowLitHole(int hole)
        {
            if (hole == _litHole)
            {
                return;
            }

            if (_litHole != Flute.NoHole)
            {
                PaintHole(_holes[_litHole], _litHole, false);
            }

            if (hole != Flute.NoHole)
            {
                PaintHole(_holes[hole], hole, true);
            }

            _litHole = hole;
        }

        private void ShowBreath(bool isExhausted)
        {
            if (IsDimmed != isExhausted)
            {
                IsDimmed = isExhausted;
                _tube.style.opacity = isExhausted ? _hud.ExhaustedFluteOpacity : 1f;
            }
        }

        private int HoleAt(Vector2 position)
        {
            for (int hole = 0; hole < _holes.Length; hole++)
            {
                if (_holes[hole].worldBound.Contains(position))
                {
                    return hole;
                }
            }

            return Flute.NoHole;
        }

        private void HandlePointerDown(PointerDownEvent pointer)
        {
            _tube.CapturePointer(pointer.pointerId);
            StartPlaying(HoleAt(pointer.position));
        }

        private void HandlePointerMove(PointerMoveEvent pointer)
        {
            if (_tube.HasPointerCapture(pointer.pointerId))
            {
                SlideTo(HoleAt(pointer.position));
            }
        }

        private void HandlePointerUp(PointerUpEvent pointer) => _tube.ReleasePointer(pointer.pointerId);

        private void HandlePointerCaptureOut(PointerCaptureOutEvent pointer) => StopPlaying();
    }
}
