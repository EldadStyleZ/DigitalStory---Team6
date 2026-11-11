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

    /// <summary>How the blow sounds: the charging note swells, and the blow plays a chord on that note.</summary>
    public static class BlowSound
    {
        /// <summary>The charging note's loudness: from <paramref name="start"/> when empty to <paramref name="end"/> when full.</summary>
        public static float ChargeGain(float progress, float start, float end) => Mathf.Lerp(start, end, Mathf.Clamp01(progress));

        /// <summary>How much higher than the root a chord note is, as a pitch ratio: 4 semitones is a major third.</summary>
        public static float Ratio(float semitones) => Mathf.Pow(2f, semitones / 12f);
    }

    /// <summary>Where the blow reaches: a cone in front of the player, and how hard it pushes at each point.</summary>
    public static class BlowCone
    {
        /// <summary>
        /// Whether a point is inside the cone: within <paramref name="reach"/> of the origin and within half of
        /// <paramref name="angle"/> (degrees) of the aim. A point right at the origin counts.
        /// </summary>
        public static bool Contains(Vector2 origin, Vector2 aim, Vector2 point, float angle, float reach)
        {
            Vector2 offset = point - origin;
            float distance = offset.magnitude;

            if (distance > reach)
            {
                return false;
            }

            if (distance < 0.01f || aim.sqrMagnitude < 1e-6f)
            {
                return true;
            }

            return Vector2.Angle(aim, offset) <= angle * 0.5f;
        }

        /// <summary>
        /// The push at a point, along the aim: full <paramref name="speed"/> at the origin, falling to
        /// <paramref name="edgeStrength"/> of it at the edge of the reach.
        /// </summary>
        public static Vector2 Push(Vector2 origin, Vector2 aim, Vector2 point, float reach, float speed, float edgeStrength)
        {
            float distance = Vector2.Distance(origin, point);
            float strength = Mathf.Lerp(1f, edgeStrength, reach > 0f ? Mathf.Clamp01(distance / reach) : 1f);
            return aim.normalized * (speed * strength);
        }
    }

    /// <summary>A shove that fades to nothing over its time, such as from the blow.</summary>
    public struct Knockback
    {
        private Vector2 _push;
        private float _startedAt;
        private float _seconds;

        /// <summary>Starts a shove of <paramref name="push"/> (units per second) fading over <paramref name="seconds"/>.</summary>
        public Knockback(Vector2 push, float now, float seconds)
        {
            _push = push;
            _startedAt = now;
            _seconds = Mathf.Max(0.01f, seconds);
        }

        /// <summary>Whether the shove is still going.</summary>
        public bool IsActive(float now) => _seconds > 0f && now < _startedAt + _seconds;

        /// <summary>The shove's velocity now: full at the start, fading in a straight line to zero.</summary>
        public Vector2 Velocity(float now) =>
            IsActive(now) ? _push * (1f - (now - _startedAt) / _seconds) : Vector2.zero;

        /// <summary>A pushed body's velocity: only <paramref name="control"/> (0 to 1) of its own, plus the shove.</summary>
        public static Vector2 Combine(Vector2 own, Vector2 shove, float control) => own * Mathf.Clamp01(control) + shove;
    }

    /// <summary>A short camera shake that dies away, such as when a blow goes off.</summary>
    public static class CameraShake
    {
        /// <summary>
        /// How far the camera is moved <paramref name="elapsed"/> seconds into a shake: a quick wobble no bigger
        /// than <paramref name="amplitude"/>, shrinking to nothing at <paramref name="seconds"/>.
        /// </summary>
        public static Vector2 Offset(float elapsed, float seconds, float amplitude)
        {
            if (seconds <= 0f || elapsed < 0f || elapsed >= seconds)
            {
                return Vector2.zero;
            }

            float left = 1f - elapsed / seconds;
            float size = amplitude * left * left;
            return new Vector2(Mathf.Sin(elapsed * 61f), Mathf.Sin(elapsed * 47f + 1.3f)) * size;
        }
    }
}
