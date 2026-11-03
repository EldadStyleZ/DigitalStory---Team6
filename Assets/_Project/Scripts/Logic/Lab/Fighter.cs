using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Anything that walks the maze and fights: a creature or a minion. Besides its fight, it remembers
    /// what it has just done, so the view can move its legs, lunge and flash without guessing.
    /// </summary>
    public abstract class Fighter
    {
        private float _secondsToNextHit;
        private float _slowShare;
        private float _slowSecondsLeft;

        protected Fighter(FighterStats stats, FighterLook look, Vector2 position)
        {
            Stats = stats;
            Look = look;
            Position = position;
            Health = new Health(stats.MaxHealth);
        }

        public FighterStats Stats { get; }
        public FighterLook Look { get; }
        public Health Health { get; }
        public Vector2 Position { get; private set; }

        /// <summary>The way it faces, in degrees, with east as zero.</summary>
        public float Facing { get; private set; }

        /// <summary>How far it has walked in all, in tiles. Being pushed through a crowd does not count.</summary>
        public float DistanceWalked { get; private set; }

        /// <summary>Seconds since it last took a step that got it somewhere.</summary>
        public float SecondsSinceStep { get; private set; } = float.PositiveInfinity;

        /// <summary>Seconds since it last hit or healed.</summary>
        public float SecondsSinceStrike { get; private set; } = float.PositiveInfinity;

        /// <summary>Seconds since a hit last took some of its health.</summary>
        public float SecondsSinceHurt { get; private set; } = float.PositiveInfinity;

        public bool IsAlive => Health.IsAlive;
        public bool CanHitNow => _secondsToNextHit <= 0f;
        public bool IsSlowed => _slowSecondsLeft > 0f;

        private float Pace => IsSlowed ? 1f - _slowShare : 1f;

        /// <summary>Waits out the time between hits and any slow.</summary>
        public void Rest(float deltaTime)
        {
            _secondsToNextHit -= deltaTime * Pace;
            _slowSecondsLeft -= deltaTime;
        }

        /// <summary>Moves on the clocks of what it last did. Unlike resting, this goes on while it sleeps.</summary>
        public void PassTime(float deltaTime)
        {
            SecondsSinceStep += deltaTime;
            SecondsSinceStrike += deltaTime;
            SecondsSinceHurt += deltaTime;
        }

        /// <summary>Starts the wait before the next hit.</summary>
        public void MarkHit()
        {
            _secondsToNextHit = Stats.SecondsBetweenHits;
            SecondsSinceStrike = 0f;
        }

        public void WalkToward(Vector2 goal, Maze maze, float radius, float deltaTime)
        {
            Vector2 offset = goal - Position;
            float distance = offset.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 heading = offset / distance;
            Facing = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
            Vector2 start = Position;
            Position = maze.Move(Position, heading * Mathf.Min(Stats.MoveSpeed * Pace * deltaTime, distance), radius);

            // Only ground actually covered counts, so legs never run on the spot against a wall.
            float covered = Vector2.Distance(start, Position);

            if (covered > 0f)
            {
                DistanceWalked += covered;
                SecondsSinceStep = 0f;
            }
        }

        /// <summary>Turns to face a point without moving, such as whatever it is hitting.</summary>
        public void TurnToward(Vector2 point)
        {
            Vector2 offset = point - Position;

            if (offset.sqrMagnitude > Mathf.Epsilon)
            {
                Facing = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Takes a hit, less whatever its armour ignores.</summary>
        public void TakeHit(float damage)
        {
            float taken = damage * (1f - Stats.Traits.Armour);

            if (taken > 0f)
            {
                SecondsSinceHurt = 0f;
            }

            Health.Damage(taken);
        }

        /// <summary>Takes a share of its speed for a while.</summary>
        public void Slow(float share, float seconds)
        {
            _slowShare = share;
            _slowSecondsLeft = seconds;
        }

        /// <summary>Shifts it slightly without changing the way it faces.</summary>
        public void Nudge(Vector2 delta, Maze maze, float radius) => Position = maze.Move(Position, delta, radius);
    }
}
