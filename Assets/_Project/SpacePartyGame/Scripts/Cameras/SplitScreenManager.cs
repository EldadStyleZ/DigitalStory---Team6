using System.Collections;
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

        [SerializeField, Tooltip("The blow's numbers, for the camera shake.")]
        private BlowSettings _blowSettings;

        [SerializeField, Min(0f), Tooltip("How long the winner's view takes to widen to the whole screen at the round's end, in seconds.")]
        private float _winnerWidenSeconds = 1.5f;

        [SerializeField, Tooltip("How the widening eases, from 0 (start) to 1 (whole screen).")]
        private AnimationCurve _winnerWidenEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float _fullScreenSize;
        private Rect[] _viewports = new Rect[GameFlow.MaxPlayers];
        private Coroutine _widening;
        private RectTransform _verticalLine;
        private RectTransform _horizontalLine;
        private Vector2 _verticalAnchorMin, _verticalAnchorMax, _horizontalAnchorMin, _horizontalAnchorMax;

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
            _verticalLine = _verticalDivider.GetComponent<RectTransform>();
            _horizontalLine = _horizontalDivider.GetComponent<RectTransform>();

            if (_verticalLine != null)
            {
                _verticalAnchorMin = _verticalLine.anchorMin;
                _verticalAnchorMax = _verticalLine.anchorMax;
            }

            if (_horizontalLine != null)
            {
                _horizontalAnchorMin = _horizontalLine.anchorMin;
                _horizontalAnchorMax = _horizontalLine.anchorMax;
            }
        }

        /// <summary>
        /// At the round's end, widens the winner's view until it fills the screen, squeezing the others' views to
        /// nothing and sliding the dividers along. With no winner (a draw) it goes back to the single overview.
        /// </summary>
        public void ShowWinner(int winnerSlot)
        {
            if (!IsSplit || winnerSlot < 0 || winnerSlot >= _joining.Players.Count)
            {
                Unsplit();
                return;
            }

            if (_widening != null)
            {
                StopCoroutine(_widening);
            }

            _widening = StartCoroutine(Widen(winnerSlot));
        }

        private IEnumerator Widen(int winner)
        {
            int count = Mathf.Min(_joining.Players.Count, GameFlow.MaxPlayers);
            var from = (Rect[])_viewports.Clone();
            var to = new Rect[count];

            for (int slot = 0; slot < count; slot++)
            {
                to[slot] = slot == winner ? new Rect(0f, 0f, 1f, 1f) : SplitLayout.SqueezedAway(from[slot], from[winner]);

                if (slot != winner)
                {
                    _huds[slot].Hide();
                }
            }

            for (float t = 0f; t < _winnerWidenSeconds; t += Time.deltaTime)
            {
                ShowWidening(from, to, count, winner, _winnerWidenEase.Evaluate(t / _winnerWidenSeconds));
                yield return null;
            }

            ShowWidening(from, to, count, winner, 1f);

            for (int slot = 0; slot < count; slot++)
            {
                if (slot != winner)
                {
                    _cameras[slot].Stop();
                }
            }

            _verticalDivider.SetActive(false);
            _horizontalDivider.SetActive(false);
            _widening = null;
        }

        private void ShowWidening(Rect[] from, Rect[] to, int count, int winner, float k)
        {
            Rect wide = SplitLayout.Lerp(from[winner], to[winner], k);

            for (int slot = 0; slot < count; slot++)
            {
                _viewports[slot] = SplitLayout.Lerp(from[slot], to[slot], k);
                _cameras[slot].SetViewport(_viewports[slot]);

                // A view squeezed to almost nothing stops drawing, so Unity never gets a zero-sized viewport.
                _cameras[slot].Camera.enabled = _viewports[slot].width > 0.002f && _viewports[slot].height > 0.002f;
            }

            _huds[winner].Place(wide);

            // A divider follows the edge of the winner's view that faces the others.
            if (_verticalLine != null && _verticalDivider.activeSelf)
            {
                float x = from[winner].center.x < 0.5f ? wide.xMax : wide.xMin;
                _verticalLine.anchorMin = new Vector2(x, _verticalAnchorMin.y);
                _verticalLine.anchorMax = new Vector2(x, _verticalAnchorMax.y);
            }

            if (_horizontalLine != null && _horizontalDivider.activeSelf)
            {
                float y = from[winner].center.y < 0.5f ? wide.yMax : wide.yMin;
                _horizontalLine.anchorMin = new Vector2(_horizontalAnchorMin.x, y);
                _horizontalLine.anchorMax = new Vector2(_horizontalAnchorMax.x, y);
            }
        }

        private void RestoreDividers()
        {
            if (_verticalLine != null)
            {
                _verticalLine.anchorMin = _verticalAnchorMin;
                _verticalLine.anchorMax = _verticalAnchorMax;
            }

            if (_horizontalLine != null)
            {
                _horizontalLine.anchorMin = _horizontalAnchorMin;
                _horizontalLine.anchorMax = _horizontalAnchorMax;
            }
        }

        private void OnEnable()
        {
            _flow.OnMatchStart += Split;
            _flow.StateChanged += HandleStateChanged;
            _joining.PlayerJoined += ListenForBlows;
        }

        private void OnDisable()
        {
            _flow.OnMatchStart -= Split;
            _flow.StateChanged -= HandleStateChanged;
            _joining.PlayerJoined -= ListenForBlows;
        }

        // A blow shakes the blower's view, and the view of every player it shoves.
        private void ListenForBlows(PlayerInput player)
        {
            Transform target = player.transform;

            if (player.TryGetComponent(out PlayerAbilities abilities))
            {
                abilities.Blew += aim => ShakeViewOf(target);
            }

            if (player.TryGetComponent(out PlayerMovement movement))
            {
                movement.Pushed += () => ShakeViewOf(target);
            }
        }

        private void ShakeViewOf(Transform player)
        {
            if (_blowSettings == null)
            {
                return;
            }

            foreach (FollowCamera camera in _cameras)
            {
                if (camera != null && camera.Target == player)
                {
                    camera.Shake(_blowSettings.ShakeAmplitude, _blowSettings.ShakeSeconds);
                }
            }
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
                    _viewports[slot] = layout.Players[slot];
                    _cameras[slot].Follow(player.transform, layout.Players[slot]);
                    _cameras[slot].Camera.enabled = true;
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
            if (_widening != null)
            {
                StopCoroutine(_widening);
                _widening = null;
            }

            RestoreDividers();

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
