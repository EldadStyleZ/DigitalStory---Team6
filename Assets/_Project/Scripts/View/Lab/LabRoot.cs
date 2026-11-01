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

        [Header("Look")]
        [SerializeField, Tooltip("The town's sprites.")]
        private TownArtConfig _sprites;

        [SerializeField, Tooltip("The lab's colors and sizes.")]
        private LabArtConfig _art;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Runs the lab from the keyboard.")]
        private LabRunner _runner;

        [SerializeField, Tooltip("Draws the maze.")]
        private MazeView _mazeView;

        [SerializeField, Tooltip("Draws the Piper.")]
        private LabPiperView _piperView;

        [SerializeField, Tooltip("Follows the Piper.")]
        private LabCamera _camera;

        /// <summary>The running lab, for tests and debugging.</summary>
        public LabSession Session { get; private set; }

        private void Awake()
        {
            if (!HasEverything())
            {
                enabled = false;
                return;
            }

            Session = new LabSession(new LabRules(_maze, _player));

            _runner.Initialize(Session);
            _mazeView.Initialize(Session.Maze, _sprites, _art);
            _piperView.Initialize(Session.Piper, _art);
            _camera.Initialize(Session.Piper, _art);
        }

        private bool HasEverything()
        {
            bool isComplete = _maze != null && _player != null && _sprites != null && _art != null
                && _runner != null && _mazeView != null && _piperView != null && _camera != null;

            if (!isComplete)
            {
                Debug.LogError($"{name}: LabRoot has an empty slot. Rebuild the scene or fill it in.", this);
            }

            return isComplete;
        }
    }
}
