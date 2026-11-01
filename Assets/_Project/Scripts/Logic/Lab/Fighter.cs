using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Anything that walks the maze and fights: a creature or a minion.</summary>
    public abstract class Fighter
    {
        private float _secondsToNextHit;

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

        public bool IsAlive => Health.IsAlive;
        public bool CanHitNow => _secondsToNextHit <= 0f;

        /// <summary>Waits out the time between hits.</summary>
        public void Rest(float deltaTime) => _secondsToNextHit -= deltaTime;

        /// <summary>Starts the wait before the next hit.</summary>
        public void MarkHit() => _secondsToNextHit = Stats.SecondsBetweenHits;

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
            Position = maze.Move(Position, heading * Mathf.Min(Stats.MoveSpeed * deltaTime, distance), radius);
        }

        /// <summary>Shifts it slightly without changing the way it faces.</summary>
        public void Nudge(Vector2 delta, Maze maze, float radius) => Position = maze.Move(Position, delta, radius);
    }
}
