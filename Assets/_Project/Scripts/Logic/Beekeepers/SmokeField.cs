using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Smoke grenades in flight and the clouds they leave. Clouds hurt hives and drain the swarm
    /// while the Piper stands in one. Grenades and clouds are pooled (capacity 16), so nothing allocates per tick.
    /// A hive or the Piper is hurt once per tick however many clouds overlap it.
    /// </summary>
    public sealed class SmokeField : ISmokeClearer
    {
        /// <summary>The most grenades in flight and clouds on the ground, each.</summary>
        public const int Capacity = 16;

        private readonly SmokeConfig _config;
        private readonly SmokeCloud[] _clouds = new SmokeCloud[Capacity];
        private readonly Vector3[] _grenadeTargets = new Vector3[Capacity];
        private readonly float[] _grenadeSecondsLeft = new float[Capacity];
        private readonly bool[] _grenadeActive = new bool[Capacity];
        private int _cloudCount;

        /// <summary>Creates a field that uses <paramref name="config"/>.</summary>
        public SmokeField(SmokeConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            for (int i = 0; i < Capacity; i++)
            {
                _clouds[i] = new SmokeCloud();
            }
        }

        /// <summary>Raised when a grenade leaves a hand: from, to, flight seconds.</summary>
        public event Action<Vector3, Vector3, float> GrenadeThrown;

        /// <summary>Raised when a grenade lands and becomes a cloud.</summary>
        public event Action<SmokeCloud> CloudFormed;

        /// <summary>Raised when a cloud expires or is cleared.</summary>
        public event Action<SmokeCloud> CloudEnded;

        /// <summary>True when the Piper stood in a cloud as of the last <see cref="Tick"/>.</summary>
        public bool IsPiperInSmoke { get; private set; }

        /// <summary>How many clouds are on the ground.</summary>
        public int CloudCount => _cloudCount;

        /// <summary>Throws a grenade. It lands at <paramref name="to"/> after the flight time. Ignored when the pool is full.</summary>
        public void Throw(Vector3 from, Vector3 to)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_grenadeActive[i])
                {
                    continue;
                }

                _grenadeActive[i] = true;
                _grenadeTargets[i] = to;
                _grenadeSecondsLeft[i] = _config.FlightSeconds;
                GrenadeThrown?.Invoke(from, to, _config.FlightSeconds);
                return;
            }
        }

        /// <summary>Advances grenades and clouds, hurts hives, drains the swarm and updates <see cref="IsPiperInSmoke"/>.</summary>
        public void Tick(float deltaTime, HiveSet hives, SwarmBody swarm, Vector3 piperPosition, bool isDomed)
        {
            if (hives == null)
            {
                throw new ArgumentNullException(nameof(hives));
            }

            if (swarm == null)
            {
                throw new ArgumentNullException(nameof(swarm));
            }

            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "deltaTime cannot be negative.");
            }

            for (int i = 0; i < Capacity; i++)
            {
                if (_clouds[i].IsActive)
                {
                    _clouds[i].StepSeconds = deltaTime;
                }
            }

            LandGrenades(deltaTime);
            HurtHives(hives);
            HurtSwarm(deltaTime, swarm, piperPosition, isDomed);
            AgeClouds();
        }

        /// <inheritdoc />
        public int ClearWithin(Vector3 center, float radius)
        {
            int cleared = 0;
            float radiusSquared = radius * radius;
            for (int i = 0; i < Capacity; i++)
            {
                SmokeCloud cloud = _clouds[i];
                if (!cloud.IsActive)
                {
                    continue;
                }

                float dx = cloud.Position.x - center.x;
                float dz = cloud.Position.z - center.z;
                if (dx * dx + dz * dz <= radiusSquared)
                {
                    End(cloud);
                    cleared++;
                }
            }

            return cleared;
        }

        private void LandGrenades(float deltaTime)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!_grenadeActive[i])
                {
                    continue;
                }

                _grenadeSecondsLeft[i] -= deltaTime;
                if (_grenadeSecondsLeft[i] > 0f)
                {
                    continue;
                }

                _grenadeActive[i] = false;
                SmokeCloud cloud = FreeCloud();
                if (cloud == null)
                {
                    continue;
                }

                cloud.Form(_grenadeTargets[i], _config.Radius, _config.LifeSeconds);
                cloud.StepSeconds = -_grenadeSecondsLeft[i];
                _cloudCount++;
                CloudFormed?.Invoke(cloud);
            }
        }

        private SmokeCloud FreeCloud()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!_clouds[i].IsActive)
                {
                    return _clouds[i];
                }
            }

            return null;
        }

        private void HurtHives(HiveSet hives)
        {
            for (int c = 0; c < Capacity; c++)
            {
                SmokeCloud cloud = _clouds[c];
                if (cloud.IsActive)
                {
                    cloud.EffectSeconds = Mathf.Min(cloud.StepSeconds, Mathf.Max(0f, cloud.SecondsLeft));
                }
            }

            for (int h = 0; h < hives.Count; h++)
            {
                Hive hive = hives[h];
                if (hive.IsDestroyed)
                {
                    continue;
                }

                float seconds = LongestEffectAt(hive.Position, out _);
                if (seconds > 0f)
                {
                    hive.Damage(_config.HiveDamagePerSecond * seconds);
                }
            }
        }

        private void HurtSwarm(float deltaTime, SwarmBody swarm, Vector3 piperPosition, bool isDomed)
        {
            float seconds = LongestEffectAt(piperPosition, out bool inside);
            IsPiperInSmoke = inside;
            if (seconds > 0f)
            {
                swarm.Drain(_config.WaspLossPerSecond, seconds, isDomed);
            }
        }

        private float LongestEffectAt(Vector3 point, out bool inside)
        {
            float longest = 0f;
            inside = false;
            for (int c = 0; c < Capacity; c++)
            {
                SmokeCloud cloud = _clouds[c];
                if (!cloud.IsActive || !cloud.Contains(point))
                {
                    continue;
                }

                inside = true;
                if (cloud.EffectSeconds > longest)
                {
                    longest = cloud.EffectSeconds;
                }
            }

            return longest;
        }

        private void AgeClouds()
        {
            for (int c = 0; c < Capacity; c++)
            {
                SmokeCloud cloud = _clouds[c];
                if (!cloud.IsActive)
                {
                    continue;
                }

                cloud.Age(cloud.StepSeconds);
                if (cloud.SecondsLeft <= 0f)
                {
                    End(cloud);
                }
            }
        }

        private void End(SmokeCloud cloud)
        {
            cloud.Deactivate();
            _cloudCount--;
            CloudEnded?.Invoke(cloud);
        }
    }
}
