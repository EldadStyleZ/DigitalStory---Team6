using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Runs the waves: waits, starts a wave, asks for its beekeepers one by one (sticks first, then smokers,
    /// to spawn points in round robin), waits for them to die, breaks, and finally reports the last wave cleared.
    /// </summary>
    public sealed class WaveDirector
    {
        private readonly WaveConfig _config;
        private readonly int _spawnPointCount;
        private int _waveNumber;
        private float _countdown;
        private bool _isRunning;
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
            _countdown = config.FirstWaveDelaySeconds;
        }

        /// <summary>0 before the first wave, then 1-based.</summary>
        public int WaveNumber => _waveNumber;

        /// <summary>How many waves there are.</summary>
        public int WaveCount => _config.Waves.Count;

        /// <summary>Seconds until the next wave starts. 0 while a wave is running or after the last wave.</summary>
        public float SecondsToNextWave => _isRunning || _allCleared ? 0f : _countdown;

        /// <summary>True from a wave's start until it is cleared.</summary>
        public bool IsWaveRunning => _isRunning;

        /// <summary>True once the last wave is cleared.</summary>
        public bool AllWavesCleared => _allCleared;

        /// <summary>Raised when a wave starts, with its 1-based number.</summary>
        public event Action<int> WaveStarted;

        /// <summary>Raised for each beekeeper to spawn: its kind and the spawn point index.</summary>
        public event Action<BeekeeperKind, int> SpawnRequested;

        /// <summary>Raised once, after the last wave is cleared.</summary>
        public event Action Cleared;

        /// <summary>Advances the timers. A wave is cleared when all of it has spawned and none is alive.</summary>
        public void Tick(float deltaTime, int beekeepersAlive)
        {
            if (_allCleared)
            {
                return;
            }

            bool justStarted = false;
            if (!_isRunning)
            {
                _countdown -= deltaTime;
                if (_countdown > 0f)
                {
                    return;
                }

                StartWave();
                justStarted = true;
            }

            bool spawned = false;
            if (!justStarted && _spawnedInWave < _totalInWave)
            {
                _spawnTimer -= deltaTime;
            }

            while (_spawnedInWave < _totalInWave && _spawnTimer <= 0f)
            {
                Spawn();
                spawned = true;
            }

            // The alive count was taken before this tick's spawns, so it cannot speak for them.
            if (!spawned && _spawnedInWave >= _totalInWave && beekeepersAlive <= 0)
            {
                FinishWave();
            }
        }

        private void StartWave()
        {
            WaveEntry entry = _config.Waves[_waveNumber];
            _waveNumber++;
            _isRunning = true;
            _countdown = 0f;
            _sticksInWave = entry.Sticks;
            _totalInWave = entry.Sticks + entry.Smokers;
            _spawnedInWave = 0;
            _spawnTimer = 0f;
            WaveStarted?.Invoke(_waveNumber);
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
            _isRunning = false;
            if (_waveNumber >= _config.Waves.Count)
            {
                _allCleared = true;
                Cleared?.Invoke();
                return;
            }

            _countdown = _config.BreakSeconds;
        }
    }
}
