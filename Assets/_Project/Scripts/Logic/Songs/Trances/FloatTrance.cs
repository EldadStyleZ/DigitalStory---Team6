using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The rat swells up and drifts on the wind until it lands.</summary>
    public sealed class FloatTrance : RatTrance
    {
        private readonly BalloonsEffect _song;

        public FloatTrance(BalloonsEffect song) => _song = song;

        public override bool IsOutOfReach => true;
        public override bool MovesTheRat => true;
        public override RatLook Look => RatLook.Floating;
        public override TranceEnding Ending => TranceEnding.Popped;

        public override void Move(ref Rat rat, in TranceStep step)
        {
            float bob = Mathf.Sin((step.Time * _song.BobRate) + rat.Phase) * _song.BobSize;
            float x = rat.Position.x + ((_song.WindSpeed + rat.TranceDrift.x) * step.DeltaTime);
            float y = rat.Position.y + ((rat.TranceDrift.y + bob) * step.DeltaTime);

            rat.Position.x = Mathf.Min(step.Town.Width - _song.SideMargin, x);
            rat.Position.y = Mathf.Clamp(y, _song.EdgeMargin, step.Town.Height - _song.EdgeMargin);
        }

        public override void Finish(ref Rat rat, in TranceStep step)
        {
            rat.Velocity = Vector2.zero;
            step.Houses.PushOut(ref rat.Position, ref rat.Velocity, step.RatRadius);
        }
    }
}
