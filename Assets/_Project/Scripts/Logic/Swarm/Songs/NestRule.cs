using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The nest: a trap left on a lane. It lands at the aimed beekeeper (or the aim point), pulled in to the song's range,
    /// and stays where it landed. It arms <see cref="NestSong.ArmSeconds"/> after the cast; the first moment a living
    /// beekeeper stands within the trigger radius of an armed nest (one already there as it arms sets it off at once) it is
    /// set off, and <see cref="NestSong.FuseSeconds"/> later it hurts and stuns every living beekeeper within the blast
    /// radius. An unset nest ends at <see cref="NestSong.MaxSeconds"/> hurting nobody. The rule lets go at the burst. Its
    /// radii and damage grow with the swarm's level, as it stood at the cast, and the picture gets the grown radii.
    /// </summary>
    internal sealed class NestRule : SongRule
    {
        // Not game tuning: the picture reads -1 as "not set off yet".
        private const float NotSetOff = -1f;

        private readonly NestSong _song;

        private IReadOnlyList<ISwarmTarget> _enemies;
        private FuryMeter _fury;
        private Vector3 _position;
        private float _triggerRadius;
        private float _blastRadius;
        private float _damage;
        private float _setOffAt = NotSetOff;

        public NestRule(NestSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>Where the nest lies, and the seconds after the cast at which a beekeeper set it off, or -1 (plan section B).</summary>
        public override Vector4 ParamA => new Vector4(_position.x, _position.y, _position.z, _setOffAt);

        /// <summary>
        /// The metres within which a beekeeper sets it off and the metres its burst reaches, as grown by the level (plan
        /// section B), and beyond the plan's line the fuse's seconds (<see cref="NestSong.FuseSeconds"/>), so the picture
        /// erupts on the burst's frame however the fuse is tuned. w is 0.
        /// </summary>
        public override Vector4 ParamB => new Vector4(_triggerRadius, _blastRadius, _song.FuseSeconds, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _triggerRadius = _song.TriggerRadius * context.SizeScale;
            _blastRadius = _song.BlastRadius * context.SizeScale;
            _damage = _song.Damage * context.DamageScale;
            ISwarmTarget aim = context.AimTarget;
            Vector3 point = aim != null && aim.IsAlive ? aim.Position : context.AimPoint;
            _position = GroundMath.PullIn(point, context.CorePosition, _song.MaxRange * context.SizeScale);
            _setOffAt = NotSetOff;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (_setOffAt < 0f)
            {
                if (tick.Seconds >= _song.MaxSeconds)
                {
                    Release(_song.MaxSeconds);
                    return;
                }

                if (tick.Seconds < _song.ArmSeconds || !IsAnyoneNear())
                {
                    return;
                }

                // One who was already there as it armed sets it off at the moment it arms.
                _setOffAt = tick.SecondsBefore < _song.ArmSeconds ? _song.ArmSeconds : tick.Seconds;
            }

            float burstAt = _setOffAt + _song.FuseSeconds;

            if (tick.Seconds >= burstAt)
            {
                Burst();
                Release(burstAt);
            }
        }

        private bool IsAnyoneNear()
        {
            float radiusSquared = _triggerRadius * _triggerRadius;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (enemy.IsAlive && GroundMath.DistanceSquared(enemy.Position, _position) <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private void Burst()
        {
            float radiusSquared = _blastRadius * _blastRadius;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (!enemy.IsAlive || GroundMath.DistanceSquared(enemy.Position, _position) > radiusSquared)
                {
                    continue;
                }

                Hit(enemy, _damage);

                if (enemy.IsAlive)
                {
                    enemy.Stun(_song.StunSeconds);
                }

                _fury.AddDamage(_damage);
            }

            Land(_position);
        }
    }
}
