using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Follows a player at once, whoever it followed before, as if it just heard them, such as under an
        /// earworm. Any later call can take it over again, even one held since before.
        /// </summary>
        public void Force(int leader, float now)
        {
            _leader = leader;
            _leaderCallStartedAt = float.NegativeInfinity;
            _lastHeard = now;
        }
    }

    /// <summary>
    /// The earworm's hold on one vermin: for the earworm time it follows the player whose earworm caught it, and no
    /// other player's call can take it. Only another player's earworm takes it over (for a full earworm time).
    /// </summary>
    public sealed class EarwormHold
    {
        private readonly float _seconds;
        private float _until = float.NegativeInfinity;

        public EarwormHold(float seconds)
        {
            _seconds = seconds;
        }

        /// <summary>The slot of the player whose earworm caught it last, or -1 if none yet.</summary>
        public int Holder { get; private set; } = -1;

        /// <summary>Whether the earworm still holds it.</summary>
        public bool IsActive(float now) => Holder >= 0 && now < _until;

        /// <summary>A player's earworm catches it: it is theirs for the earworm time from now.</summary>
        public void Catch(int player, float now)
        {
            Holder = player;
            _until = now + _seconds;
        }

        /// <summary>Whether a call from this player is ignored now: any player but the holder, while it lasts.</summary>
        public bool BlocksCall(int caller, float now) => IsActive(now) && caller != Holder;

        /// <summary>Lets go at once, such as when the holder cages it or the round ends.</summary>
        public void End() => _until = float.NegativeInfinity;
    }

    /// <summary>
    /// The colour of a follower's outline: its leader's hue and brightness, with the saturation scaled. A call's
    /// outline is softer (less saturated) and an earworm's is vivid (more saturated), at the same brightness, so the
    /// earworm reads as the stronger effect.
    /// </summary>
    public static class OutlineTone
    {
        /// <param name="callSaturation">The call outline's saturation, as a share of the leader colour's (0.5 = half).</param>
        /// <param name="earwormSaturation">The earworm outline's saturation, as a multiple of the leader colour's, up to full.</param>
        public static Color For(Color leaderColour, bool isEarwormed, float callSaturation, float earwormSaturation)
        {
            Color.RGBToHSV(leaderColour, out float hue, out float saturation, out float brightness);
            float scale = isEarwormed ? earwormSaturation : callSaturation;
            Color tone = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * Mathf.Max(0f, scale)), brightness);
            tone.a = leaderColour.a;
            return tone;
        }
    }

    /// <summary>What the earworm (LT, charged like the blow) does and does not reach.</summary>
    public static class EarwormRules
    {
        /// <summary>The note that charges the earworm: Left (Left Shift on the keyboard, LT on a gamepad).</summary>
        public static bool Charges(Note note) => note == Note.Left;

        /// <summary>It catches free vermin; ones in any cage are left alone, though the wave is still drawn over them.</summary>
        public static bool Catches(bool isCaged) => !isCaged;
    }

    /// <summary>
    /// Keeps vermin from bunching on one spot: each steers away from neighbours closer than the spacing radius, more
    /// strongly the closer they are. It is a nudge added to its own movement, not a collision, so crowds spread out
    /// softly instead of shoving.
    /// </summary>
    public static class VerminSpacing
    {
        private const float SameSpot = 1e-4f;

        /// <summary>The push away from close neighbours, never longer than the strength (in units per second).</summary>
        /// <param name="ignoreSelf">Skip neighbours on exactly its own spot, such as itself in a physics query's results.</param>
        public static Vector2 Push(Vector2 self, IReadOnlyList<Vector2> neighbours, float radius, float strength, bool ignoreSelf = false)
        {
            Vector2 sum = Vector2.zero;

            foreach (Vector2 other in neighbours)
            {
                Vector2 away = self - other;
                float distance = away.magnitude;

                if (distance >= radius)
                {
                    continue;
                }

                if (distance < SameSpot)
                {
                    if (ignoreSelf)
                    {
                        continue;
                    }

                    // Exactly on top of each other: any direction will do to start them apart.
                    sum += Vector2.right;
                    continue;
                }

                sum += away / distance * (1f - (distance / radius));
            }

            return Vector2.ClampMagnitude(sum, 1f) * strength;
        }
    }

    /// <summary>Where a follower stands near its leader, its own spot, so followers do not all aim at one point.</summary>
    public static class FollowSpot
    {
        /// <summary>Its own stopping distance: the middle distance, give or take the spread, from a 0-to-1 roll.</summary>
        public static float Distance(float middle, float spread, float roll) => middle + (((roll * 2f) - 1f) * spread);

        /// <summary>Its own small offset from the leader, anywhere within the radius, from two 0-to-1 rolls.</summary>
        public static Vector2 Offset(float radius, float angleRoll, float distanceRoll)
        {
            float angle = angleRoll * 2f * Mathf.PI;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * Mathf.Sqrt(distanceRoll));
        }
    }

    /// <summary>
    /// A caged vermin called by another player does not slip straight through the bars: it has to push against a
    /// wall for a while first, flattening a little as it does, before it gets through.
    /// </summary>
    public sealed class CageSqueeze
    {
        private readonly float _seconds;
        private float _pressed;

        public CageSqueeze(float seconds)
        {
            _seconds = seconds;
        }

        /// <summary>Whether it is trying to squeeze out now.</summary>
        public bool IsActive { get; private set; }

        /// <summary>How far through the squeeze it is, from 0 to 1.</summary>
        public float Progress => !IsActive ? 0f : _seconds <= 0f ? 1f : Mathf.Clamp01(_pressed / _seconds);

        /// <summary>Starts trying to squeeze out, with no pushing done yet.</summary>
        public void Begin()
        {
            IsActive = true;
            _pressed = 0f;
        }

        /// <summary>It pushes against a wall for a while; returns whether it has now pushed long enough to get through.</summary>
        public bool Press(float deltaTime)
        {
            _pressed += deltaTime;
            return _pressed >= _seconds;
        }

        /// <summary>Stops squeezing, such as once it is through or no longer called.</summary>
        public void End()
        {
            IsActive = false;
            _pressed = 0f;
        }

        /// <summary>
        /// The outward direction of the wall nearest a point, given its offset from the cage's centre and the
        /// cage's inner half size.
        /// </summary>
        public static Vector2 WallNormal(Vector2 offset, Vector2 innerHalfSize)
        {
            float across = Mathf.Abs(offset.x) / Mathf.Max(innerHalfSize.x, 1e-4f);
            float along = Mathf.Abs(offset.y) / Mathf.Max(innerHalfSize.y, 1e-4f);

            if (across >= along)
            {
                return offset.x >= 0f ? Vector2.right : Vector2.left;
            }

            return offset.y >= 0f ? Vector2.up : Vector2.down;
        }

        /// <summary>
        /// How to scale the sprite while it pushes on a wall: flattened toward the wall by the amount at full
        /// progress, and widened along the wall by half that.
        /// </summary>
        public static Vector2 Scale(Vector2 wallNormal, float progress, float amount)
        {
            float squash = 1f - (amount * progress);
            float bulge = 1f + (amount * progress * 0.5f);
            return Mathf.Abs(wallNormal.x) >= Mathf.Abs(wallNormal.y) ? new Vector2(squash, bulge) : new Vector2(bulge, squash);
        }
    }

    /// <summary>Who can call a vermin.</summary>
    public static class CallRules
    {
        /// <summary>A vermin hears every call except its own cage owner's, so a player cannot call theirs back out.</summary>
        public static bool Hears(int caller, int? cageOwner) => cageOwner != caller;
    }

    /// <summary>How fast followers go: faster when their leader runs, so they keep up.</summary>
    public static class FollowPace
    {
        /// <summary>
        /// The follow speed now: <paramref name="followSpeed"/> scaled by how much faster than walking the leader can
        /// go (<paramref name="leaderTopSpeed"/> over <paramref name="leaderWalkSpeed"/>). <paramref name="match"/>
        /// (0 to 1) is how much of that boost followers take; it never drops below the normal follow speed.
        /// </summary>
        public static float Speed(float followSpeed, float leaderWalkSpeed, float leaderTopSpeed, float match)
        {
            if (leaderWalkSpeed <= 0f)
            {
                return followSpeed;
            }

            float boost = Mathf.Max(1f, leaderTopSpeed / leaderWalkSpeed);
            return followSpeed * Mathf.Lerp(1f, boost, Mathf.Clamp01(match));
        }
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
