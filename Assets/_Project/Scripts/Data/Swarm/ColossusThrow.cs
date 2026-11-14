using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// How one of the giant's moves throws wasps off the limb that strikes (songs-v2 W): how many leave, how wide they spread,
    /// how fast they fly out and how soon they are all back on the body. A look only: the move's damage, timing and reach are
    /// elsewhere on <see cref="ColossusConfig"/> and never change with these.
    /// </summary>
    [Serializable]
    public sealed class ColossusThrow
    {
        [SerializeField, Range(0f, 1f), Tooltip("Share of the striking limb's wasps that leave on the hit.")]
        private float _share;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees either side of the giant's facing the burst spreads: the cone's half angle, the fan's and the spray's half width. Rings and spirals go all around and ignore it.")]
        private float _spreadDegrees;

        [SerializeField, Min(0f), Tooltip("Metres per second the wasps fly out, times the level's size scale. They fly out for ColossusConfig.ThrowOutShare of the time until they are back.")]
        private float _speed;

        [SerializeField, Min(0.1f), Tooltip("Seconds from the hit until every thrown wasp is back in its place on the body.")]
        private float _backSeconds;

        /// <summary>Creates a throw with the given numbers, for the config's defaults.</summary>
        public ColossusThrow(float share, float spreadDegrees, float speed, float backSeconds)
        {
            _share = share;
            _spreadDegrees = spreadDegrees;
            _speed = speed;
            _backSeconds = backSeconds;
        }

        /// <summary>Share of the striking limb's wasps that leave, 0 to 1.</summary>
        public float Share => _share;

        /// <summary>Degrees either side of the facing the burst spreads.</summary>
        public float SpreadDegrees => _spreadDegrees;

        /// <summary>Metres per second the wasps fly out, before the level's size scale.</summary>
        public float Speed => _speed;

        /// <summary>Seconds from the hit until every thrown wasp is back.</summary>
        public float BackSeconds => _backSeconds;
    }
}
