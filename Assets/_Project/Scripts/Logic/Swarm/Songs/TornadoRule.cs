using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The tornado: a column around the core that, once it stands at full height, hurts and throws every beekeeper in reach
    /// of where the core stands then, once, and each one it throws and does not kill cannot act for
    /// <see cref="TornadoSong.StunSeconds"/> (plan section O). Its reach, height, wall and damage grow with the swarm's
    /// level, as it stood at the cast, and the picture gets the grown sizes; the throw does not grow.
    /// </summary>
    internal sealed class TornadoRule : SongRule
    {
        // Not game tuning: the picture reads -1 as "no second hit". Only the Eye of the Storm duet will set one.
        private const float NoSecondHit = -1f;

        private readonly TornadoSong _song;

        private IReadOnlyList<ISwarmTarget> _enemies;
        private FuryMeter _fury;
        private float _radius;
        private float _height;
        private float _wallRadius;
        private float _damage;
        private bool _hasHit;

        public TornadoRule(TornadoSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The reach, the height, the spin and the hit time.</summary>
        public override Vector4 ParamA => new Vector4(_radius, _height, _song.SpinRadiansPerSecond, _song.HitSeconds);

        /// <summary>The wall's radius and the second hit time, which is -1 with no duet.</summary>
        public override Vector4 ParamB => new Vector4(_wallRadius, NoSecondHit, 0f, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _radius = _song.Radius * context.SizeScale;
            _height = _song.Height * context.SizeScale;
            _wallRadius = _song.WallRadius * context.SizeScale;
            _damage = _song.Damage * context.DamageScale;
            _hasHit = false;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (!_hasHit && tick.Seconds >= _song.HitSeconds)
            {
                _hasHit = true;
                Hit(tick.CorePosition);
            }

            if (tick.Seconds >= _song.Seconds)
            {
                Release(_song.Seconds);
            }
        }

        private void Hit(Vector3 core)
        {
            float reachSquared = _radius * _radius;
            float totalDamage = 0f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (!enemy.IsAlive || GroundMath.DistanceSquared(enemy.Position, core) > reachSquared)
                {
                    continue;
                }

                Hit(enemy, _damage);
                enemy.Knockback(GroundMath.Direction(core, enemy.Position) * _song.KnockbackSpeed);
                totalDamage += _damage;

                if (enemy.IsAlive)
                {
                    enemy.Stun(_song.StunSeconds);
                }
            }

            if (totalDamage > 0f)
            {
                _fury.AddDamage(totalDamage);
            }

            Land(core);
        }
    }
}
