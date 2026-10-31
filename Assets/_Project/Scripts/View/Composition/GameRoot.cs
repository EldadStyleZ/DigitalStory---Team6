using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Creates the night's rules and hands them to the objects that show them.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)] // Runs before other scripts, so they have what they need when they start.
    [AddComponentMenu("DigitalStory/Game Root")]
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField, Tooltip("The rats, the goal and the time until dawn.")]
        private NightConfig _night;

        [SerializeField, Tooltip("The town's size, river and generation.")]
        private TownConfig _town;

        [SerializeField, Tooltip("How the Piper moves.")]
        private PiperConfig _piper;

        [SerializeField, Tooltip("How playing uses breath.")]
        private BreathConfig _breath;

        [SerializeField, Tooltip("What each flute hole does.")]
        private FluteConfig _flute;

        [SerializeField, Tooltip("How rats behave.")]
        private RatConfig _rats;

        [Header("Look and words")]
        [SerializeField, Tooltip("Sprites and colors.")]
        private TownArtConfig _art;

        [SerializeField, Tooltip("The words shown when the night ends.")]
        private EndingTextConfig _endingTexts;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Reads the keyboard and mouse.")]
        private PiperInput _input;

        [SerializeField, Tooltip("Runs the night every frame.")]
        private NightRunner _runner;

        [SerializeField, Tooltip("Draws the town.")]
        private TownView _townView;

        [SerializeField, Tooltip("Draws the Piper.")]
        private PiperView _piperView;

        [SerializeField, Tooltip("Draws the rats.")]
        private RatSwarmView _swarmView;

        [SerializeField, Tooltip("Follows the Piper.")]
        private TownCamera _townCamera;

        [SerializeField, Tooltip("Shows the goal, time and breath.")]
        private NightStatusView _statusView;

        /// <summary>The running night, for tests and debugging.</summary>
        public NightSession Session { get; private set; }

        private void Awake()
        {
            if (!HasEverything())
            {
                enabled = false;
                return;
            }

            var rules = new GameRules(_town, _piper, _breath, _flute, _rats);
            var townRandom = new Mulberry32Random((uint)_night.Rules.Seed);
            var simulationRandom = new Mulberry32Random((uint)Environment.TickCount);
            Session = new NightSession(rules, _night.Rules, townRandom, simulationRandom);

            _input.Initialize(Session.Piper, _townCamera.GetComponent<Camera>(), _piper);
            _runner.Initialize(Session, _input);
            _townView.Initialize(Session.Town, _town, Session.Bank, _art);
            _piperView.Initialize(Session.Piper);
            _swarmView.Initialize(Session.Swarm, _art);
            _townCamera.Initialize(Session.Piper, _town);
            _statusView.Initialize(Session, _endingTexts);
        }

        private bool HasEverything()
        {
            bool isComplete = _night != null && _town != null && _piper != null && _breath != null
                && _flute != null && _rats != null && _art != null && _endingTexts != null
                && _input != null && _runner != null && _townView != null && _piperView != null
                && _swarmView != null && _townCamera != null && _statusView != null;

            if (!isComplete)
            {
                Debug.LogError($"{name}: GameRoot has an empty slot. Rebuild the scene or fill it in.", this);
            }

            return isComplete;
        }
    }
}
