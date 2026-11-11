using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>Every number that decides how many vermin appear, when, and how they stroll.</summary>
    [CreateAssetMenu(fileName = "VerminSettings", menuName = "Space Party Game/Vermin Settings")]
    public sealed class VerminSettings : ScriptableObject
    {
        [Header("Spawning")]
        [SerializeField, Min(0), Tooltip("How many vermin appear when play begins.")]
        private int _startCount = 50;

        [SerializeField, Min(0), Tooltip("When this many or fewer are left outside the cages, more appear.")]
        private int _refillThreshold = 20;

        [SerializeField, Min(0), Tooltip("How many appear each time the free vermin run low.")]
        private int _refillAmount = 15;

        [SerializeField, Min(0f), Tooltip("Seconds between one vermin appearing and the next.")]
        private float _secondsBetweenSpawns = 0.06f;

        [SerializeField, Min(1), Tooltip("How many vermin the pool makes when the scene loads. No more are made during play.")]
        private int _poolSize = 150;

        [Header("Where they appear")]
        [SerializeField, Min(0f), Tooltip("Space kept between a new vermin and the floor's edge.")]
        private float _edgeMargin = 0.8f;

        [SerializeField, Min(0f), Tooltip("Space kept between a new vermin and a cage.")]
        private float _cageMargin = 0.5f;

        [SerializeField, Min(0f), Tooltip("Space kept between a new vermin and a player.")]
        private float _playerMargin = 1.2f;

        [SerializeField, Range(0f, 1f), Tooltip("The chance a new vermin faces left when it appears.")]
        private float _startFacingLeftChance = 0.5f;

        [Header("Wandering")]
        [SerializeField, Min(0f), Tooltip("How fast a vermin strolls, in units per second.")]
        private float _wanderSpeed = 0.7f;

        [SerializeField, Min(0f), Tooltip("The shortest time it keeps one direction, in seconds.")]
        private float _minWanderSeconds = 1f;

        [SerializeField, Min(0f), Tooltip("The longest time it keeps one direction, in seconds.")]
        private float _maxWanderSeconds = 3f;

        [SerializeField, Range(0f, 1f), Tooltip("The chance it stands still instead of picking a direction.")]
        private float _pauseChance = 0.3f;

        [Header("Following a call")]
        [SerializeField, Min(0f), Tooltip("How long it keeps following after it last heard its leader's call, in seconds.")]
        private float _followSeconds = 2f;

        [SerializeField, Min(0f), Tooltip("How fast it follows, in units per second (the toy creatures' follow speed).")]
        private float _followSpeed = 3.6f;

        [SerializeField, Min(0f), Tooltip("How close it walks up to its leader (the toy creatures' follow distance).")]
        private float _followDistance = 1.1f;

        public int StartCount => _startCount;
        public float FollowSeconds => _followSeconds;
        public float FollowSpeed => _followSpeed;
        public float FollowDistance => _followDistance;
        public int RefillThreshold => _refillThreshold;
        public int RefillAmount => _refillAmount;
        public float SecondsBetweenSpawns => _secondsBetweenSpawns;
        public int PoolSize => _poolSize;
        public float EdgeMargin => _edgeMargin;
        public float CageMargin => _cageMargin;
        public float PlayerMargin => _playerMargin;
        public float StartFacingLeftChance => _startFacingLeftChance;
        public float WanderSpeed => _wanderSpeed;
        public float MinWanderSeconds => _minWanderSeconds;
        public float MaxWanderSeconds => _maxWanderSeconds;
        public float PauseChance => _pauseChance;
    }
}
