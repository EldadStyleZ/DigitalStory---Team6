using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The honeycomb: a shell on one hive, the standing hive nearest the aim point (the Hive aim rule puts the aim point on
    /// the hive hit most recently, or the nearest one). While it stands that hive takes only
    /// <see cref="HoneycombSong.DamageFraction"/> of the damage from sticks and smoke, and each stick hit on it hurts the
    /// beekeeper who struck. No other hive is protected. It lasts until the wave it was cast in is over; cast in a farm phase
    /// it lasts <see cref="HoneycombSong.FarmSeconds"/>, unless a wave starts first, and then until that wave is over. It
    /// ends at once when its hive is destroyed, and at once with no hive standing at the cast. The shell's sizes and the thorn
    /// damage grow with the swarm's level, as it stood at the cast; the share it lets through does not.
    /// </summary>
    /// <remarks>
    /// The rule learns of the phase from the cast (<see cref="SwarmSongContext.AreSongsFree"/> true means a farm phase) and
    /// then from <see cref="NotifyPhase"/>. A wave is over when the match enters the farm phase.
    /// </remarks>
    internal sealed class HoneycombRule : SongRule
    {
        // Not game tuning: the picture reads -1 as "no stick hit yet", and a hive index of -1 as "no hive".
        private const float NoHit = -1f;
        private const int NoHive = -1;

        private readonly HoneycombSong _song;

        private HiveSet _hives;
        private FuryMeter _fury;
        private int _hive = NoHive;
        private Vector3 _hivePosition;
        private float _shellRadius;
        private float _burstRadius;
        private float _thornDamage;
        private bool _isWaveOn;
        private float _hitAngle;
        private float _hitSeconds = NoHit;

        public HoneycombRule(HoneycombSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>The hive's position and the metres of the shell's wall, as grown by the level (plan section B).</summary>
        public override Vector4 ParamA => new Vector4(_hivePosition.x, _hivePosition.y, _hivePosition.z, _shellRadius);

        /// <summary>The angle on the ground (radians, from +x toward +z) of the stick that hit last, its seconds after the cast or -1, and the metres its cells burst out to when the shell ends (grown by the level). w is 0.</summary>
        public override Vector4 ParamB => new Vector4(_hitAngle, _hitSeconds, _burstRadius, 0f);

        /// <summary>The index of the hive the shell is on, or -1 when it had none.</summary>
        public int HiveIndex => _hive;

        protected internal override float HiveDamageFraction(int hive)
        {
            return hive == _hive ? _song.DamageFraction : 1f;
        }

        protected override void Begin(in SwarmSongContext context)
        {
            _hives = context.Hives ?? throw new ArgumentException("The song context has no hives.", nameof(context));
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _shellRadius = _song.ShellRadius * context.SizeScale;
            _burstRadius = _song.BurstRadius * context.SizeScale;
            _thornDamage = _song.ThornDamage * context.DamageScale;
            _isWaveOn = !context.AreSongsFree;
            _hitAngle = 0f;
            _hitSeconds = NoHit;
            _hive = NoHive;
            _hivePosition = context.AimPoint;

            if (_hives.TryGetNearestStanding(context.AimPoint, out int index))
            {
                _hive = index;
                _hivePosition = _hives[index].Position;
            }
            else
            {
                Release(0f);
            }
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (_hives[_hive].IsDestroyed)
            {
                Release(tick.Seconds);
            }
            else if (!_isWaveOn && tick.Seconds >= _song.FarmSeconds)
            {
                Release(_song.FarmSeconds);
            }
        }

        protected internal override void NotifyPhase(MatchPhase phase, float seconds)
        {
            if (phase == MatchPhase.Survive)
            {
                _isWaveOn = true;
            }
            else if (_isWaveOn)
            {
                Release(seconds);
            }
        }

        protected internal override void NotifyHiveHit(int hive, ISwarmTarget attacker, float seconds)
        {
            if (hive != _hive)
            {
                return;
            }

            if (_hives[hive].IsDestroyed)
            {
                Release(seconds);
                return;
            }

            if (!attacker.IsAlive)
            {
                return;
            }

            Vector3 away = GroundMath.Direction(_hivePosition, attacker.Position);
            _hitAngle = Mathf.Atan2(away.z, away.x);
            _hitSeconds = seconds;
            attacker.TakeDamage(_thornDamage);
            _fury.AddDamage(_thornDamage);
        }
    }
}
