using System.Collections.Generic;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Holds everything drawn over the night, and shows it only while a night is running.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("DigitalStory/Hud Layout")]
    public sealed class HudLayout : MonoBehaviour
    {
        private const float BottomMargin = 8f;
        private const float TopRightMargin = 12f;
        private const float TopRightGap = 8f;

        private readonly List<VisualElement> _controls = new List<VisualElement>();

        private GameFlow _flow;
        private VisualElement _layer;
        private VisualElement _comboSlot;
        private VisualElement _fluteSlot;
        private VisualElement _muteSlot;
        private VisualElement _mapSlot;

        /// <summary>The whole HUD. Children place themselves in it.</summary>
        public VisualElement Layer
        {
            get
            {
                Build();
                return _layer;
            }
        }

        /// <summary>The place for the combos row, above the flute.</summary>
        public VisualElement ComboSlot
        {
            get
            {
                Build();
                return _comboSlot;
            }
        }

        /// <summary>The place for the flute, at the bottom of the screen.</summary>
        public VisualElement FluteSlot
        {
            get
            {
                Build();
                return _fluteSlot;
            }
        }

        /// <summary>The place for the mute button, beside the minimap.</summary>
        public VisualElement MuteSlot
        {
            get
            {
                Build();
                return _muteSlot;
            }
        }

        /// <summary>The place for the minimap, in the corner of the screen.</summary>
        public VisualElement MapSlot
        {
            get
            {
                Build();
                return _mapSlot;
            }
        }

        /// <summary>True while the HUD is on screen, for tests and debugging.</summary>
        public bool IsShowing => _layer != null && _layer.style.display == DisplayStyle.Flex;

        /// <summary>Supplies the flow that decides when to show the HUD. Call once, before this object is enabled.</summary>
        public void Initialize(GameFlow flow) => _flow = flow;

        /// <summary>Marks something the mouse can press, so pressing it does not also walk the Piper.</summary>
        public void AddControl(VisualElement control) => _controls.Add(control);

        /// <summary>True when a screen position is over a control.</summary>
        /// <remarks>Pointer events arrive after Update, so this also works on the frame of a press.</remarks>
        public bool IsOverControl(Vector2 screenPosition)
        {
            // Screen positions start at the bottom of the window, but panels start at the top.
            var fromTheTop = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

            foreach (VisualElement control in _controls)
            {
                if (control.panel != null
                    && control.worldBound.Contains(RuntimePanelUtils.ScreenToPanel(control.panel, fromTheTop)))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            if (_flow != null)
            {
                _flow.Changed += HandleFlowChanged;
            }
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_flow != null)
            {
                Build();
                HandleFlowChanged(_flow.Phase);
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when the scene unloads before Initialize ran, so _flow can be null.
            if (_flow != null)
            {
                _flow.Changed -= HandleFlowChanged;
            }
        }

        // Built on first use, because other views ask for their place in their own Start.
        private void Build()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = new VisualElement();
            _layer.pickingMode = PickingMode.Ignore;
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0f;
            _layer.style.right = 0f;
            _layer.style.top = 0f;
            _layer.style.bottom = 0f;

            var bottom = new VisualElement();
            bottom.pickingMode = PickingMode.Ignore;
            bottom.style.position = Position.Absolute;
            bottom.style.left = 0f;
            bottom.style.right = 0f;
            bottom.style.bottom = BottomMargin;
            bottom.style.alignItems = Align.Center;

            _comboSlot = NewSlot();
            _fluteSlot = NewSlot();
            bottom.Add(_comboSlot);
            bottom.Add(_fluteSlot);
            _layer.Add(bottom);
            _layer.Add(BuildTopRight());

            GetComponent<UIDocument>().rootVisualElement.Add(_layer);
        }

        private VisualElement BuildTopRight()
        {
            var topRight = new VisualElement();
            topRight.pickingMode = PickingMode.Ignore;
            topRight.style.position = Position.Absolute;
            topRight.style.top = TopRightMargin;
            topRight.style.right = TopRightMargin;
            topRight.style.flexDirection = FlexDirection.Row;

            _muteSlot = new VisualElement();
            _muteSlot.pickingMode = PickingMode.Ignore;
            _muteSlot.style.marginRight = TopRightGap;

            _mapSlot = new VisualElement();
            _mapSlot.pickingMode = PickingMode.Ignore;

            topRight.Add(_muteSlot);
            topRight.Add(_mapSlot);
            return topRight;
        }

        private static VisualElement NewSlot()
        {
            var slot = new VisualElement();
            slot.pickingMode = PickingMode.Ignore;
            slot.style.alignSelf = Align.Stretch;
            return slot;
        }

        private void HandleFlowChanged(GamePhase phase)
        {
            bool isNightOnScreen = phase == GamePhase.Playing || phase == GamePhase.Paused;
            _layer.style.display = isNightOnScreen ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
