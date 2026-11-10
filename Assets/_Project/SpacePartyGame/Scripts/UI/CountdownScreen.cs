using TMPro;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>Shows 3, 2, 1 in big numbers before a match.</summary>
    public sealed class CountdownScreen : StageScreen
    {
        [SerializeField, Tooltip("The big number.")]
        private TMP_Text _number;

        protected override GameState ShownIn => GameState.Countdown;

        protected override void OnEnable()
        {
            base.OnEnable();
            Flow.CountdownChanged += Show;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Flow.CountdownChanged -= Show;
        }

        protected override void OnShown()
        {
            Show(Flow.CountdownShown);
        }

        private void Show(int number)
        {
            _number.text = number.ToString();
        }
    }
}
