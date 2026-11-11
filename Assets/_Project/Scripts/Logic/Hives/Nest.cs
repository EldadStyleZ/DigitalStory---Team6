using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// One wild nest in a tree (plan section H). It is closed until its wave is cleared, then open until the core stays beside
    /// it long enough to collect it, and then it is gone. <see cref="NestField"/> opens it and counts the stay.
    /// </summary>
    public sealed class Nest
    {
        // Float sums of small steps must not keep a nest that is due: 120 steps of 1/60 s can add up to just under 2 s.
        private const float DueEpsilon = 1e-4f;

        private readonly float _collectSeconds;
        private float _staySeconds;

        internal Nest(Vector3 position, float collectSeconds)
        {
            Position = position;
            _collectSeconds = collectSeconds;
        }

        /// <summary>Where the nest hangs.</summary>
        public Vector3 Position { get; }

        /// <summary>True while the nest can be collected: its wave has been cleared and it is not yet collected.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>True once the core has collected the nest. A collected nest is gone, and is no longer open.</summary>
        public bool IsCollected { get; private set; }

        /// <summary>How far the collecting has got, from 0 to 1: the share of the stay done. 1 once collected, 0 while closed.</summary>
        public float Progress01
        {
            get
            {
                if (IsCollected)
                {
                    return 1f;
                }

                return _collectSeconds > 0f ? Mathf.Clamp01(_staySeconds / _collectSeconds) : 0f;
            }
        }

        /// <summary>Opens the nest, so the core can collect it. A collected nest stays gone.</summary>
        internal void Open()
        {
            if (!IsCollected)
            {
                IsOpen = true;
            }
        }

        /// <summary>The stay was broken: the collecting starts again from nothing.</summary>
        internal void Restart()
        {
            _staySeconds = 0f;
        }

        /// <summary>
        /// The core stayed in reach for <paramref name="deltaTime"/> more. Returns true when that completes the stay and
        /// collects the nest. Only an open nest counts.
        /// </summary>
        internal bool Stay(float deltaTime)
        {
            if (!IsOpen)
            {
                return false;
            }

            _staySeconds += deltaTime;
            if (_staySeconds < _collectSeconds - DueEpsilon)
            {
                return false;
            }

            IsOpen = false;
            IsCollected = true;
            return true;
        }
    }
}
