using System;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>Shows who won when the match ends. Hidden while the match is playing.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hive Defense End Screen View")]
    public sealed class EndScreenView : MonoBehaviour
    {
        private const string ShownClass = "end-screen--shown";
        private const string LostClass = "end-screen--lost";
        private const string TitleName = "end-title";
        private const string HintName = "end-hint";

        [SerializeField, Tooltip("The title when every wave was cleared.")]
        private string _wonTitle = "The hives are safe";

        [SerializeField, Tooltip("The title when every hive was destroyed.")]
        private string _lostTitle = "The hives have fallen";

        [SerializeField, Tooltip("The line under the title.")]
        private string _hint = "Press R to play again";

        private HiveDefenseSession _session;
        private VisualElement _root;
        private Label _title;
        private Label _hintLabel;
        private ControlDevice _device;

        /// <summary>True while the end screen is on screen, for tests and debugging.</summary>
        public bool IsShowing => _root != null && _root.ClassListContains(ShownClass);

        /// <summary>Connects the screen to the match. <paramref name="root"/> is the element named "end-screen".</summary>
        /// <exception cref="ArgumentNullException">A reference is missing.</exception>
        /// <exception cref="InvalidOperationException">The root has no title or hint label.</exception>
        public void Initialize(HiveDefenseSession session, VisualElement root)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _title = root.Q<Label>(TitleName)
                ?? throw new InvalidOperationException("The end screen has no label named " + TitleName + ".");
            _hintLabel = root.Q<Label>(HintName)
                ?? throw new InvalidOperationException("The end screen has no label named " + HintName + ".");

            _session.StateChanged += HandleStateChanged;
            Show(_session.State);
        }

        /// <summary>Names the restart key for the device in use: R, or Start on a controller.</summary>
        public void ShowControls(ControlDevice device)
        {
            _device = device;
            if (_hintLabel != null)
            {
                ControlSymbols.ShowPrompt(_hintLabel, _hint, device);
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(HiveDefenseState state) => Show(state);

        private void Show(HiveDefenseState state)
        {
            bool isOver = state != HiveDefenseState.Playing;
            _root.EnableInClassList(ShownClass, isOver);
            _root.EnableInClassList(LostClass, state == HiveDefenseState.Lost);

            if (isOver)
            {
                _title.text = state == HiveDefenseState.Won ? _wonTitle : _lostTitle;
                ControlSymbols.ShowPrompt(_hintLabel, _hint, _device);
            }
        }
    }
}
