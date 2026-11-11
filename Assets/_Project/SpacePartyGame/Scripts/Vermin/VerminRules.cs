using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A vermin's random stroll, as the toy's rats do it: every few seconds it picks a new direction, or now and
    /// then stands still, and keeps to it until the time is up. The random numbers come in from outside, so tests
    /// can choose them.
    /// </summary>
    public sealed class VerminWander
    {
        private readonly float _speed;
        private readonly float _minSeconds;
        private readonly float _maxSeconds;
        private readonly float _pauseChance;
        private readonly Func<float> _random;

        private Vector2 _velocity;
        private float _secondsLeft;

        /// <param name="random">Returns a number from 0 to 1, such as <c>() => UnityEngine.Random.value</c>.</param>
        public VerminWander(float speed, float minSeconds, float maxSeconds, float pauseChance, Func<float> random)
        {
            _speed = speed;
            _minSeconds = minSeconds;
            _maxSeconds = maxSeconds;
            _pauseChance = pauseChance;
            _random = random;
        }

        /// <summary>Moves the clock on and returns the velocity to walk at now.</summary>
        public Vector2 Tick(float deltaTime)
        {
            _secondsLeft -= deltaTime;

            if (_secondsLeft <= 0f)
            {
                Pick();
            }

            return _velocity;
        }

        /// <summary>Forgets the current direction, so the next tick picks a new one, as when it is reused.</summary>
        public void Restart() => _secondsLeft = 0f;

        // Always rolls three numbers in the same order: stand still or walk, the direction, how long.
        private void Pick()
        {
            bool standsStill = _random() < _pauseChance;
            float angle = _random() * 2f * Mathf.PI;
            _secondsLeft = Mathf.Lerp(_minSeconds, _maxSeconds, _random());
            _velocity = standsStill ? Vector2.zero : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _speed;
        }
    }

    /// <summary>
    /// Whom a vermin follows. Hearing a call makes it follow the caller, and every time it hears that call again its
    /// timer starts over; once it has heard nothing for the follow time it stops. A call that started later than the
    /// one it follows takes it over, so it follows whoever called last.
    /// </summary>
    public sealed class VerminFollow
    {
        private readonly float _followSeconds;
        private int _leader = -1;
        private float _leaderCallStartedAt;
        private float _lastHeard = float.NegativeInfinity;

        public VerminFollow(float followSeconds)
        {
            _followSeconds = followSeconds;
        }

        /// <summary>The slot of the player it follows or last followed, or -1 if none yet.</summary>
        public int Leader => _leader;

        /// <summary>Whether it is following now: it has heard its leader within the follow time.</summary>
        public bool IsFollowing(float now) => _leader >= 0 && now - _lastHeard < _followSeconds;

        /// <summary>It hears a call from a player, a call that started at a given time.</summary>
        public void Hear(int caller, float callStartedAt, float now)
        {
            if (IsFollowing(now) && caller != _leader && callStartedAt <= _leaderCallStartedAt)
            {
                return;
            }

            _leader = caller;
            _leaderCallStartedAt = callStartedAt;
            _lastHeard = now;
        }

        /// <summary>Stops following at once, such as when it walks into its leader's own cage.</summary>
        public void Stop() => _lastHeard = float.NegativeInfinity;
    }

    /// <summary>Who can call a vermin.</summary>
    public static class CallRules
    {
        /// <summary>A vermin hears every call except its own cage owner's, so a player cannot call theirs back out.</summary>
        public static bool Hears(int caller, int? cageOwner) => cageOwner != caller;
    }

    /// <summary>Moving toward a target: the toy creatures' follow, slowing as it arrives so it settles.</summary>
    public static class Steering
    {
        /// <summary>The velocity toward a target, zero within the stop distance, never faster than the top speed.</summary>
        public static Vector2 Toward(Vector2 from, Vector2 target, float stopDistance, float topSpeed)
        {
            Vector2 offset = target - from;
            float distance = offset.magnitude;

            if (distance <= stopDistance)
            {
                return Vector2.zero;
            }

            float speed = Mathf.Min(topSpeed, (distance - stopDistance) * 4f);
            return offset / distance * speed;
        }
    }

    /// <summary>When to bring in more vermin: once few enough are left outside the cages.</summary>
    public static class VerminRefill
    {
        /// <summary>
        /// How many vermin to add now. Vermin still waiting to appear count as free, so one dip below the threshold
        /// adds one batch, not one batch every frame until they appear.
        /// </summary>
        public static int HowManyToAdd(int free, int queued, int threshold, int amount) =>
            free + queued <= threshold ? amount : 0;
    }

    /// <summary>Which way a vermin faces when it appears.</summary>
    public static class VerminFacing
    {
        /// <summary>Left (-1) when a 0-to-1 roll is under the chance of facing left, otherwise right (1).</summary>
        public static int AtSpawn(float roll, float leftChance) => roll < leftChance ? -1 : 1;
    }

    /// <summary>A fade from invisible to fully shown.</summary>
    public static class FadeIn
    {
        /// <summary>The alpha this long into a fade of this length: 0 at the start, rising evenly to 1.</summary>
        public static float AlphaAt(float elapsed, float duration) =>
            duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    /// <summary>Picks random places, such as where a vermin appears.</summary>
    public static class SpawnPicker
    {
        /// <summary>
        /// Tries random points in an area until one is valid, up to a number of tries. Each try rolls x, then y.
        /// </summary>
        public static bool TryPick(Rect area, Func<Vector2, bool> isValid, Func<float> random, int tries, out Vector2 point)
        {
            for (int i = 0; i < tries; i++)
            {
                float x = Mathf.Lerp(area.xMin, area.xMax, random());
                float y = Mathf.Lerp(area.yMin, area.yMax, random());
                point = new Vector2(x, y);

                if (isValid(point))
                {
                    return true;
                }
            }

            point = default;
            return false;
        }
    }
}
