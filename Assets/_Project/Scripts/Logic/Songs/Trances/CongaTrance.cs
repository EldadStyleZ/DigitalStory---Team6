using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The rat joins a dance line, following the nearest dancer ahead of it.</summary>
    public sealed class CongaTrance : RatTrance
    {
        private readonly RatSwarm _swarm;
        private readonly int[] _line;
        private readonly int _lookAhead;

        /// <param name="swarm">The rats in the line.</param>
        /// <param name="line">The rats, from the front of the line.</param>
        /// <param name="lookAhead">How many places ahead a rat looks for a dancer to follow.</param>
        /// <param name="stopDistance">How close to the dancer ahead a rat starts to slow down.</param>
        public CongaTrance(RatSwarm swarm, int[] line, int lookAhead, float stopDistance)
        {
            _swarm = swarm;
            _line = line;
            _lookAhead = lookAhead;
            StopDistance = stopDistance;
        }

        public override float StopDistance { get; }

        public override Vector2 Target(in Rat rat, Vector2 piperPosition)
        {
            int furthest = Mathf.Max(0, rat.TranceSlot - _lookAhead);

            for (int slot = rat.TranceSlot - 1; slot >= furthest; slot--)
            {
                ref readonly Rat ahead = ref _swarm[_line[slot]];

                if (ahead.IsAlive && ahead.Trance == this)
                {
                    return ahead.Position;
                }
            }

            return piperPosition;
        }
    }
}
