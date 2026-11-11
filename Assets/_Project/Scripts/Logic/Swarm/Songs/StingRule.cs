using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The sting: its squad arcs to the target and wraps it, and hurts it each second from the moment it arrives until the
    /// time is up or the target dies. With no living target in reach it wraps the empty air at the aim for a moment.
    /// </summary>
    internal sealed class StingRule : SongRule
    {
        private readonly StingSong _song;

        private ISwarmTarget _target;
        private FuryMeter _fury;
        private Vector3 _point;
        private float _endSeconds;
        private bool _hasArrived;

        public StingRule(StingSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>
        /// Where the wrapped target or air is, following the target as it moves, and in w the seconds the flight takes, so
        /// the picture arrives on the frame the damage starts.
        /// </summary>
        public override Vector4 ParamA => new Vector4(_point.x, _point.y, _point.z, _song.ArriveSeconds);

        /// <summary>The second target, for the Twin Sting duet. With no duet it is the first.</summary>
        public override Vector4 ParamB => new Vector4(_point.x, _point.y, _point.z, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            ISwarmTarget aim = context.AimTarget;
            bool isAimAlive = aim != null && aim.IsAlive;
            Vector3 piper = context.PiperPosition;
            float range = _song.MaxRange;

            if (isAimAlive && GroundMath.DistanceSquared(aim.Position, piper) <= range * range)
            {
                _target = aim;
                _point = aim.Position;
                _endSeconds = _song.Seconds;
            }
            else
            {
                _target = null;
                _point = GroundMath.PullIn(isAimAlive ? aim.Position : context.AimPoint, piper, range);
                _endSeconds = _song.AirSeconds;
            }

            _hasArrived = false;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (_target != null)
            {
                if (!_target.IsAlive)
                {
                    Release(tick.SecondsBefore);
                    return;
                }

                _point = _target.Position;
            }

            if (!_hasArrived && tick.Seconds >= _song.ArriveSeconds)
            {
                _hasArrived = true;
                Land(_point);
            }

            if (_target != null)
            {
                Hurt(tick.SecondsBefore, tick.Seconds);

                if (!_target.IsAlive)
                {
                    Release(tick.Seconds);
                    return;
                }
            }

            if (tick.Seconds >= _endSeconds)
            {
                Release(_endSeconds);
            }
        }

        // Only the part of the step from the arrival to the end hurts.
        private void Hurt(float secondsBefore, float seconds)
        {
            float damage = _song.DamagePerSecond * GroundMath.Overlap(secondsBefore, seconds, _song.ArriveSeconds, _endSeconds);

            if (damage > 0f)
            {
                _target.TakeDamage(damage);
                _fury.AddDamage(damage);
            }
        }
    }
}
