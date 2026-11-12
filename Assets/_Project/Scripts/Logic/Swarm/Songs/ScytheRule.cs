using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The Scythe: a blade off the core that faces the aim, chosen at the cast: the aimed beekeeper while it lives, otherwise
    /// the aim point. At <see cref="ScytheSong.HitSeconds"/> it cuts once every living beekeeper within its radius of where the
    /// core stands then, in the half-disc facing the aim; the line through the core across the aim is the half-disc's edge
    /// and counts. Each cast sweeps the other way from the last. Its radius and damage grow with the swarm's level, as it
    /// stood at the cast, and the picture gets the grown radius.
    /// </summary>
    /// <remarks>
    /// The picture, as plan section B's "CPU:" line says: ParamA = (start angle, signed sweep, radius, wind-up seconds), and
    /// the centre is the live core. Angles are radians on the ground from the world's +x axis toward +z, so the direction at
    /// angle a is (cos a, 0, sin a). The sweep is centred on the aim: the start angle is the aim's angle less half the signed
    /// sweep, and the blade turns from there by the signed sweep (positive turns from +x toward +z). Added beyond the plan's
    /// line: ParamB = (cut seconds, sweep seconds, 0, 0), the cut at 0.3 s and the sweep's length after the wind-up, so the
    /// edge can cross the aim on the frame the blade cuts. The cut raises Landed once, at the core.
    /// </remarks>
    internal sealed class ScytheRule : SongRule
    {
        private readonly ScytheSong _song;

        private IReadOnlyList<ISwarmTarget> _enemies;
        private FuryMeter _fury;
        private Vector3 _facing;
        private float _radius;
        private float _damage;
        private float _startAngle;
        private float _signedSweep;
        private float _sweepWay = -1f;
        private bool _hasCut;

        public ScytheRule(ScytheSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The start angle, the signed sweep, the grown radius and the wind-up, as the class remarks say.</summary>
        public override Vector4 ParamA => new Vector4(_startAngle, _signedSweep, _radius, _song.WindUpSeconds);

        /// <summary>The cut's seconds and the sweep's seconds after the wind-up.</summary>
        public override Vector4 ParamB => new Vector4(_song.HitSeconds, _song.SweepSeconds, 0f, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _radius = _song.Radius * context.SizeScale;
            _damage = _song.Damage * context.DamageScale;
            ISwarmTarget aim = context.AimTarget;
            Vector3 toward = aim != null && aim.IsAlive ? aim.Position : context.AimPoint;
            _facing = GroundMath.Direction(context.CorePosition, toward);
            _sweepWay = -_sweepWay;
            _signedSweep = _sweepWay * _song.SweepDegrees * Mathf.Deg2Rad;
            _startAngle = Mathf.Atan2(_facing.z, _facing.x) - (_signedSweep * 0.5f);
            _hasCut = false;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (!_hasCut && tick.Seconds >= _song.HitSeconds)
            {
                _hasCut = true;
                Cut(tick.CorePosition);
            }

            if (tick.Seconds >= _song.Seconds)
            {
                Release(_song.Seconds);
            }
        }

        private void Cut(Vector3 core)
        {
            float radiusSquared = _radius * _radius;
            float totalDamage = 0f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (!enemy.IsAlive || GroundMath.DistanceSquared(enemy.Position, core) > radiusSquared)
                {
                    continue;
                }

                float ahead = ((enemy.Position.x - core.x) * _facing.x) + ((enemy.Position.z - core.z) * _facing.z);

                if (ahead < 0f)
                {
                    continue;
                }

                Hit(enemy, _damage);
                totalDamage += _damage;
            }

            if (totalDamage > 0f)
            {
                _fury.AddDamage(totalDamage);
            }

            Land(core);
        }
    }
}
