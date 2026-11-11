using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The dome: a shell around the Piper for its seconds. While it runs, only its share of each wasp loss gets through and
    /// the Piper walks slower, and each hit it blocks is remembered so the picture can ripple from the hit's side.
    /// </summary>
    internal sealed class DomeRule : SongRule
    {
        // Not game tuning: the picture reads -1 as "no blocked hit yet".
        private const float NoHit = -1f;

        private readonly DomeSong _song;

        private Vector3 _hitDirection;
        private float _hitSeconds = NoHit;

        public DomeRule(DomeSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The radius, the side of the last blocked hit on the ground (x and z), and its seconds after the cast, or -1.</summary>
        public override Vector4 ParamA => new Vector4(_song.Radius, _hitDirection.x, _hitDirection.z, _hitSeconds);

        /// <summary>Nothing yet.</summary>
        public override Vector4 ParamB => Vector4.zero;

        /// <summary>The dome's share of each loss: <see cref="DomeSong.LossFraction"/>.</summary>
        protected internal override float WaspLossFraction => _song.LossFraction;

        /// <summary>The Piper walks slower inside the dome: <see cref="DomeSong.SpeedMultiplier"/>.</summary>
        protected internal override float PiperSpeedMultiplier => _song.SpeedMultiplier;

        protected override void Begin(in SwarmSongContext context)
        {
            _hitDirection = Vector3.zero;
            _hitSeconds = NoHit;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (tick.Seconds >= _song.Seconds)
            {
                Release(_song.Seconds);
            }
        }

        protected internal override void NotifyPiperHit(Vector3 direction, float seconds)
        {
            _hitDirection = direction;
            _hitSeconds = seconds;
        }
    }
}
