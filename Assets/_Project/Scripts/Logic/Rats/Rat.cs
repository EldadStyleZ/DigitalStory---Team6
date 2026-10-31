using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One rat's state.</summary>
    /// <remarks>
    /// A mutable struct with public fields, because the swarm updates hundreds of these in an array
    /// every step and passes their fields by reference.
    /// </remarks>
    public struct Rat
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Home;
        public float WanderAngle;
        public float Charm;
        public float Phase;
        public bool IsAlive;

        /// <summary>The song holding this rat, or null.</summary>
        public RatTrance Trance;

        public float TranceLeft;

        /// <summary>This rat's place in a formation.</summary>
        public int TranceSlot;

        /// <summary>This rat's own random drift while floating.</summary>
        public Vector2 TranceDrift;

        /// <param name="position">Where the rat starts.</param>
        /// <param name="home">The nest it returns to.</param>
        /// <param name="wanderAngle">Its first wandering direction, in radians.</param>
        /// <param name="phase">An offset that stops rats animating in step.</param>
        public Rat(Vector2 position, Vector2 home, float wanderAngle, float phase)
        {
            Position = position;
            Velocity = Vector2.zero;
            Home = home;
            WanderAngle = wanderAngle;
            Charm = 0f;
            Phase = phase;
            IsAlive = true;
            Trance = null;
            TranceLeft = 0f;
            TranceSlot = 0;
            TranceDrift = Vector2.zero;
        }

        /// <summary>True when cats ignore this rat and crowd songs leave it out.</summary>
        public readonly bool IsOutOfReach => Trance != null && Trance.IsOutOfReach;
    }
}
