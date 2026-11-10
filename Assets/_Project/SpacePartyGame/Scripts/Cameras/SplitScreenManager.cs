using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Splits the screen when a match starts, so the split is already there during the countdown, and puts the
    /// single full-screen Main Camera back when the game returns to the menu or the join screen.
    /// </summary>
    /// <remarks>
    /// The Main Camera keeps the only AudioListener. During a split it stops drawing, unless three players leave a
    /// quadrant for it to show the whole map.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SplitScreenManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The players who joined.")]
        private PlayerJoinController _joining;

        [SerializeField, Tooltip("The full-screen camera, which is also the overview.")]
        private Camera _mainCamera;

        [SerializeField, Tooltip("The map, so the overview can fit all of it.")]
        private MapBounds _map;

        [SerializeField, Tooltip("P1 to P4's cameras.")]
        private FollowCamera[] _cameras = new FollowCamera[GameFlow.MaxPlayers];

        [SerializeField, Tooltip("P1 to P4's HUD areas.")]
        private PlayerHud[] _huds = new PlayerHud[GameFlow.MaxPlayers];

        [SerializeField, Tooltip("The line down the middle of the screen.")]
        private GameObject _verticalDivider;

        [SerializeField, Tooltip("The line across the middle of the screen.")]
        private GameObject _horizontalDivider;

        private float _fullScreenSize;

        /// <summary>Whether the screen is split now.</summary>
        public bool IsSplit { get; private set; }

        private void Awake()
        {
            if (_flow == null || _joining == null || _mainCamera == null || _map == null
                || _verticalDivider == null || _horizontalDivider == null)
            {
                Debug.LogError($"{name}: SplitScreenManager is missing a reference.", this);
                return;
            }

            _fullScreenSize = _mainCamera.orthographicSize;
        }

        private void OnEnable()
        {
            _flow.OnMatchStart += Split;
            _flow.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _flow.OnMatchStart -= Split;
            _flow.StateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            Unsplit();
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.StartMenu || to == GameState.Join)
            {
                Unsplit();
            }
        }

        private void Split()
        {
            int count = Mathf.Min(_joining.Players.Count, GameFlow.MaxPlayers);

            if (count == 0)
            {
                return;
            }

            SplitLayout layout = SplitLayout.For(count);

            for (int slot = 0; slot < GameFlow.MaxPlayers; slot++)
            {
                if (slot < count)
                {
                    PlayerInput player = _joining.Players[slot];
                    _cameras[slot].Follow(player.transform, layout.Players[slot]);
                    _huds[slot].Show(player.GetComponent<PlayerIdentity>(), layout.Players[slot]);
                }
                else
                {
                    _cameras[slot].Stop();
                    _huds[slot].Hide();
                }
            }

            if (layout.HasOverview)
            {
                _mainCamera.rect = layout.Overview;
                _mainCamera.orthographicSize = CameraFraming.SizeToFit(_map.Rect, _mainCamera.aspect);
                _mainCamera.transform.position = new Vector3(_map.Rect.center.x, _map.Rect.center.y, _mainCamera.transform.position.z);
                _mainCamera.enabled = true;
            }
            else
            {
                _mainCamera.enabled = false;
            }

            _verticalDivider.SetActive(layout.HasVerticalDivider);
            _horizontalDivider.SetActive(layout.HasHorizontalDivider);
            IsSplit = true;
        }

        private void Unsplit()
        {
            foreach (FollowCamera camera in _cameras)
            {
                camera.Stop();
            }

            foreach (PlayerHud hud in _huds)
            {
                hud.Hide();
            }

            _mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _mainCamera.orthographicSize = _fullScreenSize;
            _mainCamera.transform.position = new Vector3(0f, 0f, _mainCamera.transform.position.z);
            _mainCamera.enabled = true;
            _verticalDivider.SetActive(false);
            _horizontalDivider.SetActive(false);
            IsSplit = false;
        }
    }
}
