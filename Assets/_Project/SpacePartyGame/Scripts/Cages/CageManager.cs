using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Puts one cage per player on the map when a match starts, so they are there during the countdown, removes
    /// them on the start menu and join screen, and keeps each player's live score and HUD in step with their cage.
    /// With scene cages set, it gives each player their placed cage instead and leaves the cages in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CageManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("The players who joined, and their spawn points.")]
        private PlayerJoinController _joining;

        [SerializeField, Tooltip("Each slot's cage picture.")]
        private PlayerPalette _palette;

        [SerializeField, Tooltip("The playable floor.")]
        private MapBounds _map;

        [SerializeField, Tooltip("Cages placed in the scene, P1's first. When set, players are given these instead of copies of the prefab.")]
        private Cage[] _sceneCages = Array.Empty<Cage>();

        [SerializeField, Tooltip("The cage to copy for each player, when no scene cages are set.")]
        private Cage _cagePrefab;

        [SerializeField, Tooltip("The parent for the cages.")]
        private Transform _cagesParent;

        [SerializeField, Tooltip("P1 to P4's HUD areas.")]
        private PlayerHud[] _huds = new PlayerHud[GameFlow.MaxPlayers];

        [SerializeField, Min(0), Tooltip("Points for each vermin in a player's cage.")]
        private int _pointsPerVermin = 5;

        [Header("Placement")]
        [SerializeField, Tooltip("The painters' cage spots on BackgroundWithCages: orange, pink, green, blue, teal, violet.")]
        private Vector2[] _paintedSpots =
        {
            new Vector2(-2.74f, 2.76f),
            new Vector2(1.87f, 3.00f),
            new Vector2(-6.38f, 0.32f),
            new Vector2(5.35f, 0.40f),
            new Vector2(-3.86f, -3.00f),
            new Vector2(2.48f, -2.78f),
        };

        [SerializeField, Tooltip("The painted spots used with 2 players, P1 first: left and right.")]
        private int[] _twoPlayerSpots = { 2, 3 };

        [SerializeField, Tooltip("The painted spots used with 3 players: left, top right, bottom right.")]
        private int[] _threePlayerSpots = { 2, 1, 5 };

        [SerializeField, Tooltip("The painted spots used with 4 players: the four corners.")]
        private int[] _fourPlayerSpots = { 0, 1, 4, 5 };

        [SerializeField, Min(0f), Tooltip("Space kept between a cage and the floor's edge.")]
        private float _borderMargin = 0.4f;

        [SerializeField, Min(0f), Tooltip("Space kept between two cages.")]
        private float _gap = 0.6f;

        [SerializeField, Min(0f), Tooltip("Space kept between a cage and a spawn point.")]
        private float _spawnClearance = 0.6f;

        [SerializeField, Min(0f), Tooltip("How far a cage's centre stays from the middle of the map.")]
        private float _minCentreDistance = 3f;

        [SerializeField, Min(0.1f), Tooltip("How far a cage may be moved from its painted spot to fit.")]
        private float _searchRadius = 3f;

        private readonly List<Cage> _cages = new List<Cage>();
        private Scoreboard _scores;

        /// <summary>The cages on the map, P1's first.</summary>
        public IReadOnlyList<Cage> Cages => _cages;

        /// <summary>Every player's score, P1 first.</summary>
        public IReadOnlyList<int> Scores => _scores.Scores;

        /// <summary>Whether the scores are frozen.</summary>
        public bool ScoresLocked => _scores.IsLocked;

        /// <summary>The cage a vermin is in, or null.</summary>
        public Cage CageOf(Transform vermin)
        {
            foreach (Cage cage in _cages)
            {
                if (cage.Holds(vermin))
                {
                    return cage;
                }
            }

            return null;
        }

        /// <summary>A player's score.</summary>
        public int ScoreOf(int slot) => _scores.ScoreOf(slot);

        /// <summary>Freezes every score, such as when a round ends.</summary>
        public void LockScores() => _scores.Lock();

        private void Awake()
        {
            if (_flow == null || _joining == null || _palette == null || _map == null || _cagePrefab == null || _cagesParent == null
                || Array.IndexOf(_sceneCages, null) >= 0)
            {
                Debug.LogError($"{name}: CageManager is missing a reference.", this);
            }

            _scores = new Scoreboard(GameFlow.MaxPlayers, _pointsPerVermin);
        }

        private void OnEnable()
        {
            _flow.OnMatchStart += SpawnCages;
            _flow.StateChanged += HandleStateChanged;
            _scores.Changed += ShowScore;
        }

        private void OnDisable()
        {
            _flow.OnMatchStart -= SpawnCages;
            _flow.StateChanged -= HandleStateChanged;
            _scores.Changed -= ShowScore;
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.StartMenu || to == GameState.Join)
            {
                RemoveCages();
            }
        }

        private void SpawnCages()
        {
            RemoveCages();
            _scores.Reset();

            if (_sceneCages.Length > 0)
            {
                AssignSceneCages();
                return;
            }

            int count = _joining.Players.Count;
            Vector2[] places = CagePlacement.Place(PreferredSpots(count), Rules());

            for (int i = 0; i < count; i++)
            {
                PlayerInput player = _joining.Players[i];
                var identity = player.GetComponent<PlayerIdentity>();
                Cage cage = Instantiate(_cagePrefab, places[i], Quaternion.identity, _cagesParent);
                cage.name = $"Cage P{identity.Slot + 1}";
                cage.Initialize(identity, _palette.CageOf(identity.Slot));
                cage.ContentsChanged += HandleContentsChanged;
                _cages.Add(cage);
            }
        }

        // Gives P1 the first scene cage, P2 the second and so on. Scene cages stay on the map between matches.
        private void AssignSceneCages()
        {
            int count = Mathf.Min(_joining.Players.Count, _sceneCages.Length);

            for (int i = 0; i < count; i++)
            {
                var identity = _joining.Players[i].GetComponent<PlayerIdentity>();
                Cage cage = _sceneCages[i];
                cage.Initialize(identity, _palette.CageOf(identity.Slot));
                cage.ContentsChanged += HandleContentsChanged;
                _cages.Add(cage);
                HandleContentsChanged(cage);
            }
        }

        private void RemoveCages()
        {
            foreach (Cage cage in _cages)
            {
                if (cage != null)
                {
                    cage.ContentsChanged -= HandleContentsChanged;

                    // Scene cages are kept; only the copies made from the prefab are removed.
                    // Destroy(cage.gameObject);
                    if (Array.IndexOf(_sceneCages, cage) < 0)
                    {
                        Destroy(cage.gameObject);
                    }
                }
            }

            _cages.Clear();
        }

        private void HandleContentsChanged(Cage cage)
        {
            _scores.SetCount(cage.Owner.Slot, cage.Count);
        }

        private void ShowScore(int slot, int score)
        {
            if (slot < _huds.Length && _huds[slot] != null)
            {
                _huds[slot].SetScore(score);
            }
        }

        private Vector2[] PreferredSpots(int players)
        {
            int[] picks = players <= 2 ? _twoPlayerSpots : players == 3 ? _threePlayerSpots : _fourPlayerSpots;
            var spots = new Vector2[players];

            for (int i = 0; i < players; i++)
            {
                spots[i] = _paintedSpots[picks[Math.Min(i, picks.Length - 1)]];
            }

            return spots;
        }

        private CagePlacement.Rules Rules() => new CagePlacement.Rules
        {
            IsOnFloor = _map.Contains,
            CageHalfSize = _cagePrefab.OuterHalfSize,
            BorderMargin = _borderMargin,
            Gap = _gap,
            SpawnPoints = _joining.SpawnPoints,
            SpawnClearance = _spawnClearance,
            Centre = _map.Rect.center,
            MinCentreDistance = _minCentreDistance,
            SearchRadius = _searchRadius,
            SearchStep = 0.1f,
        };
    }
}
