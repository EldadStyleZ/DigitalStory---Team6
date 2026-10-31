using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>The rat breakdances on the spot.</summary>
    public sealed class SpinTrance : RatTrance
    {
        private readonly DiscoEffect _song;

        public SpinTrance(DiscoEffect song) => _song = song;

        public override bool MovesTheRat => true;

        public override void Move(ref Rat rat, in TranceStep step)
        {
            float angle = (step.Time * _song.SpinRate) + rat.Phase;
            rat.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _song.SpinSpeed;
            rat.Position += rat.Velocity * (step.DeltaTime * _song.SpinTravel);
            step.Houses.PushOut(ref rat.Position, ref rat.Velocity, step.RatRadius);
        }
    }
}
