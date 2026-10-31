using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Feeds the player's input and the frame time into the night.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Night Runner")]
    public sealed class NightRunner : MonoBehaviour
    {
        // The rules were tuned at this longest step. After a stall, the game slows down instead of jumping.
        private const float LongestStepSeconds = 0.033f;

        private NightSession _session;
        private PiperInput _input;
        private GameFlow _flow;

        /// <summary>Supplies the night to run. Call once, before this object updates.</summary>
        public void Initialize(NightSession session, PiperInput input, GameFlow flow)
        {
            _session = session;
            _input = input;
            _flow = flow;
        }

        private void Update()
        {
            if (_flow != null && _flow.IsPlaying)
            {
                _session.Tick(_input.ReadIntent(), Mathf.Min(Time.deltaTime, LongestStepSeconds));
            }
        }
    }
}
