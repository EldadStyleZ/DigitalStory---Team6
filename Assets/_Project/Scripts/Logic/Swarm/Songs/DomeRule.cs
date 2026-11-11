using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The dome: a shell around the core for its seconds. While it runs, only its share of each wasp loss gets through and
    /// the core moves slower, and each hit it blocks is remembered so the picture can ripple from the hit's side. Its radius
    /// grows with the swarm's level, as it stood at the cast; what it blocks does not.
    /// </summary>
    internal sealed class DomeRule : SongRule
    {
        // Not game tuning: the picture reads -1 as "no blocked hit yet".
        private const float NoHit = -1f;

        private readonly DomeSong _song;

        private Vector3 _hitDirection;
        private float _hitSeconds = NoHit;
        private float _radius;

        public DomeRule(DomeSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The radius, the side of the last blocked hit on the ground (x and z), and its seconds after the cast, or -1.</summary>
        public override Vector4 ParamA => new Vector4(_radius, _hitDirection.x, _hitDirection.z, _hitSeconds);

        /// <summary>Nothing yet.</summary>
        public override Vector4 ParamB => Vector4.zero;

        /// <summary>The dome's share of each loss: <see cref="DomeSong.LossFraction"/>.</summary>
        protected internal override float WaspLossFraction => _song.LossFraction;

        /// <summary>The core moves slower inside the dome: <see cref="DomeSong.SpeedMultiplier"/>.</summary>
        protected internal override float CoreSpeedMultiplier => _song.SpeedMultiplier;

        protected override void Begin(in SwarmSongContext context)
        {
            _radius = _song.Radius * context.SizeScale;
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

        protected internal override void NotifyCoreHit(Vector3 direction, float seconds)
        {
            _hitDirection = direction;
            _hitSeconds = seconds;
        }
    }
}
