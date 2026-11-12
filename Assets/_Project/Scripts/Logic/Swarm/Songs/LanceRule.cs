using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The Lance: while the spear is drawn (<see cref="LanceSong.DrawSeconds"/>) its line starts where the core stands and
    /// points at the aimed beekeeper while it lives, where it was last seen once it dies, or at the aim point when none was
    /// aimed at; at the release the line locks. At <see cref="LanceSong.HitSeconds"/> it hurts once every living beekeeper on
    /// the locked line, from its start out to its length and within half its width either side, and pushes each one still
    /// living out to the side it is on; one exactly on the middle goes to the line's left. Its length, width and damage grow
    /// with the swarm's level, as it stood at the cast; the push does not, as the Tornado's does not.
    /// </summary>
    /// <remarks>
    /// The picture, as plan section B's "CPU:" line says: ParamA = (origin x, origin z, direction x, direction z), the
    /// line's start on the ground and its unit direction, which follow the draw and lock at the release;
    /// ParamB = (length, draw seconds, tail lag seconds, 0), the length grown by the level. The hit lands
    /// <see cref="LanceSong.FlightSeconds"/> after the release, the time the picture's tip takes to fly to the line's end
    /// (LanceFlightSeconds in WaspSwarm.compute), so the picture needs only the draw to land on the hit's frame. The hit
    /// raises Landed once, at the tip.
    /// </remarks>
    internal sealed class LanceRule : SongRule
    {
        private readonly LanceSong _song;

        private IReadOnlyList<ISwarmTarget> _enemies;
        private FuryMeter _fury;
        private ISwarmTarget _follow;
        private Vector3 _toward;
        private Vector3 _origin;
        private Vector3 _direction;
        private float _length;
        private float _halfWidth;
        private float _damage;
        private bool _isLocked;
        private bool _hasHit;

        public LanceRule(LanceSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The line's start and direction on the ground, as the class remarks say.</summary>
        public override Vector4 ParamA => new Vector4(_origin.x, _origin.z, _direction.x, _direction.z);

        /// <summary>The grown length, the draw and the tail lag.</summary>
        public override Vector4 ParamB => new Vector4(_length, _song.DrawSeconds, _song.TailLagSeconds, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _length = _song.Length * context.SizeScale;
            _halfWidth = _song.Width * context.SizeScale * 0.5f;
            _damage = _song.Damage * context.DamageScale;
            ISwarmTarget aim = context.AimTarget;
            bool isAimAlive = aim != null && aim.IsAlive;
            _follow = isAimAlive ? aim : null;
            _toward = isAimAlive ? aim.Position : context.AimPoint;
            _isLocked = false;
            _hasHit = false;
            Draw(context.CorePosition);
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (!_isLocked)
            {
                Draw(tick.CorePosition);
                _isLocked = tick.Seconds >= _song.DrawSeconds;
            }

            if (!_hasHit && tick.Seconds >= _song.HitSeconds)
            {
                _hasHit = true;
                Thrust();
            }

            if (tick.Seconds >= _song.Seconds)
            {
                Release(_song.Seconds);
            }
        }

        // The drawn spear starts at the core and points at the aimed beekeeper while it lives, then where it was last seen.
        private void Draw(Vector3 core)
        {
            if (_follow != null)
            {
                if (_follow.IsAlive)
                {
                    _toward = _follow.Position;
                }
                else
                {
                    _follow = null;
                }
            }

            _origin = core;
            _direction = GroundMath.Direction(core, _toward);
        }

        private void Thrust()
        {
            var left = new Vector3(-_direction.z, 0f, _direction.x);
            float totalDamage = 0f;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (!enemy.IsAlive)
                {
                    continue;
                }

                float dx = enemy.Position.x - _origin.x;
                float dz = enemy.Position.z - _origin.z;
                float along = (dx * _direction.x) + (dz * _direction.z);
                float across = (dx * left.x) + (dz * left.z);

                if (along < 0f || along > _length || Math.Abs(across) > _halfWidth)
                {
                    continue;
                }

                Hit(enemy, _damage);
                totalDamage += _damage;

                if (enemy.IsAlive)
                {
                    enemy.Knockback(left * (across < 0f ? -_song.PushSpeed : _song.PushSpeed));
                }
            }

            if (totalDamage > 0f)
            {
                _fury.AddDamage(totalDamage);
            }

            Land(_origin + (_direction * _length));
        }
    }
}
