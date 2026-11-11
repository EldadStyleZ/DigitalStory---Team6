using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// The blow's charge: holding the Left note fills it over the charge time. Letting go when it is full fires the
    /// blow; letting go early, or pressing another note, empties it without firing.
    /// </summary>
    public sealed class BlowCharge
    {
        private readonly float _chargeSeconds;
        private float _startedAt;

        public BlowCharge(float chargeSeconds)
        {
            _chargeSeconds = Mathf.Max(0.01f, chargeSeconds);
        }

        /// <summary>The note that charges the blow: Left (Left arrow on the keyboard, X on a gamepad).</summary>
        public static bool Charges(Note note) => note == Note.Left;

        /// <summary>Whether the blow is charging now.</summary>
        public bool IsCharging { get; private set; }

        /// <summary>How full the charge is, 0 to 1; 0 when not charging.</summary>
        public float Progress(float now) =>
            IsCharging ? Mathf.Clamp01((now - _startedAt) / _chargeSeconds) : 0f;

        /// <summary>Whether the charge is full.</summary>
        public bool IsFull(float now) => Progress(now) >= 1f;

        /// <summary>Starts charging from empty.</summary>
        public void Begin(float now)
        {
            IsCharging = true;
            _startedAt = now;
        }

        /// <summary>Lets go: returns true when the charge was full, so the blow fires. Either way it empties.</summary>
        public bool Release(float now)
        {
            bool fires = IsFull(now);
            IsCharging = false;
            return fires;
        }

        /// <summary>Empties the charge without firing, such as when another note is pressed.</summary>
        public void Cancel()
        {
            IsCharging = false;
        }
    }
}
