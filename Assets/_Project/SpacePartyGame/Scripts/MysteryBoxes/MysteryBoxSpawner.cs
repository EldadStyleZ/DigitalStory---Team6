using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Brings mystery boxes out of the <see cref="MysteryBoxPool"/> while a round is played: one at a time, after a
    /// random wait (see <see cref="SpawnWait"/>), anywhere a vermin could appear. Nothing comes at the start and
    /// nothing after the round ends; boxes already out finish their time as normal. Plays the collect sound, and
    /// puts every box back on the start menu and the join screen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MysteryBoxSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The game's stages.")]
        private GameFlowManager _flow;

        [SerializeField, Tooltip("Where the boxes come from and go back to.")]
        private MysteryBoxPool _pool;

        [SerializeField, Tooltip("How often boxes come, and their collect sound.")]
        private MysteryBoxSettings _settings;

        [SerializeField, Tooltip("The vermin spawner, whose spots (on the floor, out of cages, away from players) boxes use too.")]
        private VerminSpawner _area;

        private AudioSource _audio;
        private float _secondsToNext;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();

            if (_flow == null || _pool == null || _settings == null || _area == null)
            {
                Debug.LogError($"{name}: MysteryBoxSpawner is missing a reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnCountdownFinished += HandlePlayBegins;
            _flow.StateChanged += HandleStateChanged;
            _pool.Collected += HandleCollected;
        }

        private void OnDisable()
        {
            _flow.OnCountdownFinished -= HandlePlayBegins;
            _flow.StateChanged -= HandleStateChanged;
            _pool.Collected -= HandleCollected;
        }

        private void HandlePlayBegins() => _secondsToNext = NextWait();

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.StartMenu || to == GameState.Join)
            {
                _pool.DespawnAll();
            }
        }

        private void HandleCollected(MysteryBox box, PlayerIdentity player)
        {
            if (_settings.CollectSound != null)
            {
                _audio.PlayOneShot(_settings.CollectSound, _settings.CollectVolume);
            }
        }

        private void Update()
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            _secondsToNext -= Time.deltaTime;

            if (_secondsToNext > 0f)
            {
                return;
            }

            _secondsToNext = NextWait();

            if (_area.TryPickSpot(out Vector2 spot))
            {
                _pool.Spawn(spot);
            }
        }

        private float NextWait() =>
            SpawnWait.Next(_settings.MinimumSecondsBetweenSpawns, _settings.AverageSecondsBetweenSpawns, Random.value);
    }
}
