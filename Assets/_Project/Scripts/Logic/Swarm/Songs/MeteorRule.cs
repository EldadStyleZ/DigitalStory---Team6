using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The meteor: its squad climbs and dives, and on landing hurts and stuns every beekeeper near the impact point. While the
    /// wasps climb the point follows the aimed beekeeper, pulled in to the song's range; it locks when the dive starts, or
    /// where the beekeeper was last seen alive. The rule lets go as it lands: the burst and the flow home are the slot's return beat.
    /// </summary>
    internal sealed class MeteorRule : SongRule
    {
        private readonly MeteorSong _song;

        private IReadOnlyList<ISwarmTarget> _enemies;
        private FuryMeter _fury;
        private ISwarmTarget _follow;
        private Vector3 _core;
        private Vector3 _impact;

        public MeteorRule(MeteorSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The impact point and the height the meteors climb to.</summary>
        public override Vector4 ParamA => new Vector4(_impact.x, _impact.y, _impact.z, _song.Height);

        /// <summary>The climb, the dive and the impact radius.</summary>
        public override Vector4 ParamB => new Vector4(_song.RiseSeconds, _song.DiveSeconds, _song.Radius, 0f);

        protected override void Begin(in SwarmSongContext context)
        {
            _enemies = context.Enemies ?? throw new ArgumentException("The song context has no beekeeper list.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            ISwarmTarget aim = context.AimTarget;
            bool isAimAlive = aim != null && aim.IsAlive;
            _core = context.CorePosition;
            _follow = isAimAlive ? aim : null;
            _impact = GroundMath.PullIn(isAimAlive ? aim.Position : context.AimPoint, _core, _song.MaxRange);
        }

        protected internal override void Tick(in SongTick tick)
        {
            Follow(tick.Seconds);
            float landing = _song.RiseSeconds + _song.DiveSeconds;

            if (tick.Seconds >= landing)
            {
                Impact();
                Release(landing);
            }
        }

        // The point trails the target while the wasps climb, and locks when the dive starts or the target dies.
        private void Follow(float seconds)
        {
            if (_follow == null)
            {
                return;
            }

            if (_follow.IsAlive)
            {
                _impact = GroundMath.PullIn(_follow.Position, _core, _song.MaxRange);
            }

            if (!_follow.IsAlive || seconds >= _song.RiseSeconds)
            {
                _follow = null;
            }
        }

        private void Impact()
        {
            float radiusSquared = _song.Radius * _song.Radius;

            for (int i = 0; i < _enemies.Count; i++)
            {
                ISwarmTarget enemy = _enemies[i];

                if (!enemy.IsAlive || GroundMath.DistanceSquared(enemy.Position, _impact) > radiusSquared)
                {
                    continue;
                }

                enemy.TakeDamage(_song.Damage);

                if (enemy.IsAlive)
                {
                    enemy.Stun(_song.StunSeconds);
                }

                _fury.AddDamage(_song.Damage);
            }

            Land(_impact);
        }
    }
}
