using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Brings vermin onto the floor from the <see cref="VerminPool"/>: a first batch when play begins, then another
    /// whenever few enough are left outside the cages (see <see cref="VerminRefill"/>). They appear one by one, each
    /// fading in, at random spots away from the rim, the cages and the players. All of them go back to the pool on
    /// the start menu and the join screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerminSpawner : MonoBehaviour
    {
        private const int TriesPerSpot = 40;

        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("Where the vermin come from and go back to.")]
        private VerminPool _pool;

        [SerializeField, Tooltip("How many appear, when, and where.")]
        private VerminSettings _settings;

        [SerializeField, Tooltip("The playable floor.")]
        private MapBounds _map;

        [SerializeField, Tooltip("The cages, to keep new vermin out of them and to count the free ones.")]
        private CageManager _cages;

        [SerializeField, Tooltip("The players, so vermin do not appear on top of them.")]
        private PlayerJoinController _players;

        private int _queued;
        private float _secondsToNext;

        /// <summary>How many vermin are waiting to appear.</summary>
        public int Queued => _queued;

        /// <summary>How many spawned vermin are outside every cage.</summary>
        public int FreeCount
        {
            get
            {
                int free = 0;

                foreach (Vermin vermin in _pool.Active)
                {
                    if (_cages.CageOf(vermin.transform) == null)
                    {
                        free++;
                    }
                }

                return free;
            }
        }

        private void Awake()
        {
            if (_flow == null || _pool == null || _settings == null || _map == null || _cages == null || _players == null)
            {
                Debug.LogError($"{name}: VerminSpawner is missing a reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnCountdownFinished += HandlePlayBegins;
            _flow.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            _flow.OnCountdownFinished -= HandlePlayBegins;
            _flow.StateChanged -= HandleStateChanged;
        }

        private void HandlePlayBegins()
        {
            _queued += _settings.StartCount;
            _secondsToNext = 0f;
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.StartMenu || to == GameState.Join)
            {
                _queued = 0;
                _pool.DespawnAll();
            }
        }

        private void Update()
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            _queued += VerminRefill.HowManyToAdd(FreeCount, _queued, _settings.RefillThreshold, _settings.RefillAmount);
            SpawnQueued(Time.deltaTime);
        }

        // One vermin each interval, so they appear one by one rather than all at once.
        private void SpawnQueued(float deltaTime)
        {
            _secondsToNext -= deltaTime;

            while (_queued > 0 && _secondsToNext <= 0f)
            {
                _queued--;
                _secondsToNext += _settings.SecondsBetweenSpawns;
                SpawnOne();
            }

            if (_queued == 0)
            {
                _secondsToNext = Mathf.Max(_secondsToNext, 0f);
            }
        }

        private void SpawnOne()
        {
            if (!SpawnPicker.TryPick(_map.Rect, IsGoodSpot, () => Random.value, TriesPerSpot, out Vector2 spot))
            {
                return;
            }

            Vermin vermin = _pool.Spawn(spot);

            if (vermin != null)
            {
                vermin.Appearance.FadeInOnSpawn();
            }
        }

        private bool IsGoodSpot(Vector2 point) =>
            IsWellInsideFloor(point) && IsClearOfCages(point) && IsClearOfPlayers(point);

        // The point and eight points around it at the edge margin must all be on the floor.
        private bool IsWellInsideFloor(Vector2 point)
        {
            if (!_map.Contains(point))
            {
                return false;
            }

            float margin = _settings.EdgeMargin;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;

                if (!_map.Contains(point + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * margin)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsClearOfCages(Vector2 point)
        {
            foreach (Cage cage in _cages.Cages)
            {
                Vector2 half = cage.OuterHalfSize + (Vector2.one * _settings.CageMargin);
                Vector2 offset = point - (Vector2)cage.transform.position;

                if (Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsClearOfPlayers(Vector2 point)
        {
            foreach (PlayerInput player in _players.Players)
            {
                if (Vector2.Distance(point, player.transform.position) < _settings.PlayerMargin)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
