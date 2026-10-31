using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A song's hold on a rat. A rat is in one trance at most, so a new song replaces the old one.</summary>
    /// <remarks>Each kind is a class of its own, so a new song plugs in without changing the swarm.</remarks>
    public abstract class RatTrance
    {
        /// <summary>True when cats ignore the rat and crowd songs leave it out. Dance still charms it.</summary>
        public virtual bool IsOutOfReach => false;

        /// <summary>True when the trance moves the rat itself. Other rats still count it as a neighbour.</summary>
        public virtual bool MovesTheRat => false;

        public virtual RatLook Look => RatLook.Normal;

        /// <summary>How a trance that moves the rat ends.</summary>
        public virtual TranceEnding Ending => TranceEnding.Quiet;

        /// <summary>How close to its target a rat in formation starts to slow down.</summary>
        public virtual float StopDistance => 0f;

        /// <summary>Moves the rat for one step, when <see cref="MovesTheRat"/> is true.</summary>
        public virtual void Move(ref Rat rat, in TranceStep step)
        {
        }

        /// <summary>Runs once, when a trance that moves the rat ends.</summary>
        public virtual void Finish(ref Rat rat, in TranceStep step)
        {
        }

        /// <summary>Where a rat in formation heads, when <see cref="MovesTheRat"/> is false.</summary>
        public virtual Vector2 Target(in Rat rat, Vector2 piperPosition) => piperPosition;
    }
}
