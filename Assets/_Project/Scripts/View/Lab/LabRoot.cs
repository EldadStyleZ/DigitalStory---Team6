using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Creates the lab's rules and hands them to the objects that show them.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)] // Runs before other scripts, so they have what they need when they start.
    [AddComponentMenu("DigitalStory/Lab/Lab Root")]
    public sealed class LabRoot : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField, Tooltip("How the maze is generated.")]
        private MazeConfig _maze;

        [SerializeField, Tooltip("How the Piper moves.")]
        private LabPlayerConfig _player;

        [SerializeField, Tooltip("How stamina is spent and recovered.")]
        private StaminaConfig _stamina;

        [SerializeField, Tooltip("How long a combo can take.")]
        private ComboConfig _combos;

        [SerializeField, Tooltip("How much experience each level needs.")]
        private LevelConfig _levels;

        [SerializeField, Tooltip("How creatures and minions move and fight.")]
        private BattleConfig _battle;

        [SerializeField, Tooltip("Every creature the Piper can summon.")]
        private CreatureRoster _roster;

        [SerializeField, Tooltip("Which minions and bosses live in the maze.")]
        private PopulationConfig _population;

        [Header("Look")]
        [SerializeField, Tooltip("The town's sprites.")]
        private TownArtConfig _sprites;

        [SerializeField, Tooltip("The lab's colors and sizes.")]
        private LabArtConfig _art;

        [SerializeField, Tooltip("The colors of the panels on screen.")]
        private HudConfig _hud;

        [SerializeField, Tooltip("The lab's words.")]
        private LabTextConfig _texts;

        [SerializeField, Tooltip("How the lab sounds.")]
        private LabSoundConfig _sound;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Runs the lab from the keyboard.")]
        private LabRunner _runner;

        [SerializeField, Tooltip("Draws the maze.")]
        private MazeView _mazeView;

        [SerializeField, Tooltip("Draws the Piper.")]
        private LabPiperView _piperView;

        [SerializeField, Tooltip("Follows the Piper.")]
        private LabCamera _camera;

        [SerializeField, Tooltip("Draws the creatures and minions.")]
        private LabUnitsView _unitsView;

        [SerializeField, Tooltip("Shows hits and area attacks.")]
        private LabEffectsView _effectsView;

        [SerializeField, Tooltip("Shows the level, bars and combos.")]
        private LabHudView _hudView;

        [SerializeField, Tooltip("Shows the pause, death and victory screens.")]
        private LabScreensView _screensView;

        [SerializeField, Tooltip("Plays the sound effects.")]
        private LabSoundView _soundView;

        /// <summary>The running lab, for tests and debugging.</summary>
        public LabSession Session { get; private set; }

        /// <summary>Which part of the lab the player is in, for tests and debugging.</summary>
        public LabFlow Flow { get; private set; }

        private void Awake()
        {
            if (!HasEverything())
            {
                enabled = false;
                return;
            }

            Session = new LabSession(new LabRules(
                _maze.Shape, _maze.Seed, _player, _stamina, _combos, _levels, _battle, _roster.Creatures, _population.Plan));

            Flow = new LabFlow();
            Session.PiperDied += Flow.Die;
            Session.Won += Flow.Win;

            _runner.Initialize(Session, Flow);
            _mazeView.Initialize(Session.Maze, _sprites, _art);
            _piperView.Initialize(Session.Piper, _art);
            _camera.Initialize(Session.Piper, _art);
            _unitsView.Initialize(Session.Battle, _sprites, _art);
            _effectsView.Initialize(Session, _sprites, _art);
            _hudView.Initialize(Session, _roster.Creatures, _texts, _art, _hud);
            _screensView.Initialize(Flow, Session, _texts, _hud);
            _soundView.Initialize(Session, _sound);
        }

        private bool HasEverything()
        {
            bool isComplete = _maze != null && _player != null && _stamina != null && _combos != null && _levels != null
                && _battle != null && _roster != null && _population != null && _sprites != null && _art != null
                && _hud != null && _texts != null && _sound != null
                && _runner != null && _mazeView != null && _piperView != null && _camera != null
                && _unitsView != null && _effectsView != null && _hudView != null && _screensView != null && _soundView != null;

            if (!isComplete)
            {
                Debug.LogError($"{name}: LabRoot has an empty slot. Rebuild the scene or fill it in.", this);
            }

            return isComplete;
        }
    }
}
