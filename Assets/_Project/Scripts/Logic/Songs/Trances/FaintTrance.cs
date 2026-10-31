using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>The rat drops where it stands and plays dead.</summary>
    public sealed class FaintTrance : RatTrance
    {
        private readonly float _slowdown;

        /// <param name="slowdown">Share of its speed the rat keeps each step.</param>
        public FaintTrance(float slowdown) => _slowdown = slowdown;

        public override bool IsOutOfReach => true;
        public override bool MovesTheRat => true;
        public override RatLook Look => RatLook.Fainted;

        public override void Move(ref Rat rat, in TranceStep step) => rat.Velocity *= _slowdown;
    }
}
