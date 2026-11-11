using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Runs the rounds of a match (plan section L). Each round is a farm phase, timed and with no beekeeper on the map, and
    /// then a survive phase: the round's wave, whose beekeepers it asks for one by one (sticks first, then smokers, to spawn
    /// points in round robin). A wave is cleared when all of it has spawned and none is alive; the next round's farm phase
    /// follows, and after the last wave it reports the match won. The player may call a wave early, ending its farm phase.
    /// </summary>
    public sealed class WaveDirector
    {
        private readonly WaveConfig _config;
        private readonly int _spawnPointCount;
        private MatchPhase _phase = MatchPhase.Farm;
        private int _round = 1;
        private float _farmLeft;
        private bool _allCleared;
        private int _spawnedInWave;
        private int _totalInWave;
        private int _sticksInWave;
        private float _spawnTimer;
        private int _nextSpawnPoint;

        public WaveDirector(WaveConfig config, int spawnPointCount)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (config.Waves == null || config.Waves.Count == 0)
            {
                throw new ArgumentException("The wave config has no waves.", nameof(config));
            }

            if (spawnPointCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnPointCount), "At least one spawn point is needed.");
            }

            _spawnPointCount = spawnPointCount;
            _farmLeft = config.FirstFarmSeconds;
        }

        /// <summary>Farm or Survive. A match starts in a farm phase. After the last wave it stays Survive: no farm phase follows.</summary>
        public MatchPhase Phase => _phase;

        /// <summary>The round being played, from 1 to <see cref="RoundCount"/>. It moves on when a wave is cleared, and stays at the last after it.</summary>
        public int Round => _round;

        /// <summary>How many rounds there are: one for each wave.</summary>
        public int RoundCount => _config.Waves.Count;

        /// <summary>Seconds until the farm phase ends and the wave comes. 0 in a survive phase.</summary>
        public float FarmSecondsLeft => _phase == MatchPhase.Farm ? Math.Max(0f, _farmLeft) : 0f;

        /// <summary>True once the last wave is cleared.</summary>
        public bool AllWavesCleared => _allCleared;

        /// <summary>Raised when the phase changes: Survive when a wave starts, Farm when a wave other than the last is cleared.</summary>
        public event Action<MatchPhase> PhaseChanged;

        /// <summary>
        /// Raised with the wave's number, from 1, on the tick the wave is cleared: before the next round's farm phase
        /// starts, and before <see cref="Cleared"/> after the last wave.
        /// </summary>
        public event Action<int> WaveCleared;

        /// <summary>Raised for each beekeeper to spawn: its kind and the spawn point index.</summary>
        public event Action<BeekeeperKind, int> SpawnRequested;

        /// <summary>Raised when the player called a wave early, once it has started, with the seconds of farm phase skipped.</summary>
        public event Action<float> WaveCalledEarly;

        /// <summary>Raised once, after the last wave is cleared.</summary>
        public event Action Cleared;

        /// <summary>
        /// Ends a farm phase now: the round's wave starts at once with its first beekeeper. Returns the seconds of farm phase
        /// skipped and raises <see cref="WaveCalledEarly"/> with them. In a survive phase, or after the last wave, it does
        /// nothing and returns 0.
        /// </summary>
        public float CallWave()
        {
            if (_allCleared || _phase != MatchPhase.Farm)
            {
                return 0f;
            }

            float skipped = Math.Max(0f, _farmLeft);
            StartWave();
            SpawnDue();
            WaveCalledEarly?.Invoke(skipped);
            return skipped;
        }

        /// <summary>Advances the farm timer, or the wave's spawns. A wave is cleared when all of it has spawned and none is alive.</summary>
        public void Tick(float deltaTime, int beekeepersAlive)
        {
            if (_allCleared)
            {
                return;
            }

            if (_phase == MatchPhase.Farm)
            {
                _farmLeft -= deltaTime;
                if (_farmLeft > 0f)
                {
                    return;
                }

                StartWave();
                SpawnDue();
                return;
            }

            if (_spawnedInWave < _totalInWave)
            {
                _spawnTimer -= deltaTime;
            }

            // The alive count was taken before this tick's spawns, so it cannot speak for them.
            bool spawned = SpawnDue();
            if (!spawned && _spawnedInWave >= _totalInWave && beekeepersAlive <= 0)
            {
                FinishWave();
            }
        }

        private void StartWave()
        {
            WaveEntry entry = _config.Waves[_round - 1];
            _farmLeft = 0f;
            _sticksInWave = entry.Sticks;
            _totalInWave = entry.Sticks + entry.Smokers;
            _spawnedInWave = 0;
            _spawnTimer = 0f;
            SetPhase(MatchPhase.Survive);
        }

        // Spawns every beekeeper whose time has come. True when it spawned any.
        private bool SpawnDue()
        {
            bool spawned = false;
            while (_spawnedInWave < _totalInWave && _spawnTimer <= 0f)
            {
                Spawn();
                spawned = true;
            }

            return spawned;
        }

        private void Spawn()
        {
            BeekeeperKind kind = _spawnedInWave < _sticksInWave ? BeekeeperKind.Stick : BeekeeperKind.Smoker;
            int point = _nextSpawnPoint;
            _nextSpawnPoint = (_nextSpawnPoint + 1) % _spawnPointCount;
            _spawnedInWave++;
            _spawnTimer += _config.SpawnIntervalSeconds;
            SpawnRequested?.Invoke(kind, point);
        }

        private void FinishWave()
        {
            WaveCleared?.Invoke(_round);

            if (_round >= _config.Waves.Count)
            {
                _allCleared = true;
                Cleared?.Invoke();
                return;
            }

            _round++;
            _farmLeft = _config.FarmSeconds;
            SetPhase(MatchPhase.Farm);
        }

        private void SetPhase(MatchPhase phase)
        {
            _phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
