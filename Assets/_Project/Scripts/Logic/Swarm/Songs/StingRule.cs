using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The stings (plan section O). One cast sends <see cref="StingSong.StingCount"/> stings for the wasps alive as its notes
    /// completed. Each takes its own target among the most threatening beekeepers within reach (the aim chooser's Threat
    /// rule); when there are fewer targets than stings, the spare ones double up, most threatening first, and their damage
    /// adds. Each sting arcs to its target and wraps it, and hurts it from the arrival until the time is up, at its even
    /// part of <see cref="StingSong.DamagePerSecond"/>, the whole cast's, in pulses on the picture's squeezes: more wasps
    /// spread the Sting over more beekeepers but never multiply it (the owner, 2026-11-14: the Sting is the weakest attack,
    /// so its strength comes from the level, like every other song's). A sting whose target dies takes the
    /// most threatening beekeeper in reach that no other sting wraps, or the most threatening one when every one is wrapped,
    /// or else doubles up on what another sting still wraps, and hurts again once it has flown there
    /// (<see cref="StingSong.ReaimSeconds"/>); with nothing left it ends, and once every sting has ended the song lets go.
    /// With no living target in reach every sting wraps the empty air at the aim for a moment. Reach and damage grow with
    /// the swarm's level, as it stood at the cast; the count follows the living wasps, by the owner's decision.
    /// </summary>
    /// <remarks>
    /// The picture: ParamA = (the first sting's target, followed live, and the arrival's seconds), so the picture arrives on
    /// the frame the damage starts; ParamB = (the count of stings, 0, 0, 0), so the squad is shared evenly between them. Each
    /// sting's target is read by <see cref="Target"/>, through <see cref="SwarmFormMachine.StingTarget"/>.
    /// The pulses (plan section O, the felt hit): the picture's rings squeeze on the arrival and every
    /// <see cref="StingSong.SqueezeSeconds"/> after it, and on each squeeze every sting deals at once the damage of the time to
    /// the next squeeze, or to the end, and reports it as one hit. So the impact is felt on the frame it lands, and the rule,
    /// the squeezes and the feel agree. A sting that flew to a new target deals its first pulse as it arrives there, for the
    /// time to the next squeeze; the time it already dealt to the old target is not dealt again.
    /// Without an aim chooser, every sting takes the aimed beekeeper when it lives within reach.
    /// </remarks>
    internal sealed class StingRule : SongRule
    {
        private readonly StingSong _song;
        private readonly ISwarmTarget[] _targets = new ISwarmTarget[StingSong.MostStings];
        private readonly ISwarmTarget[] _candidates = new ISwarmTarget[StingSong.MostStings];
        private readonly Vector3[] _points = new Vector3[StingSong.MostStings];

        // Each sting's next pulse: the seconds after the cast it falls (the arrival, a squeeze, or its arrival at a new
        // target), and the squeeze it deals the time up to, counted from the arrival's squeeze 0. Between them the sting
        // keeps SqueezeAt(_pulseTo - 1) <= _pulseFrom < SqueezeAt(_pulseTo).
        private readonly float[] _pulseFrom = new float[StingSong.MostStings];
        private readonly int[] _pulseTo = new int[StingSong.MostStings];
        private readonly bool[] _isLive = new bool[StingSong.MostStings];

        private FuryMeter _fury;
        private ISongAim _aim;
        private float _range;
        private float _endSeconds;
        private float _damagePerSecond;
        private int _count;
        private bool _isAir;
        private bool _hasArrived;

        public StingRule(StingSong song)
            : base(song)
        {
            _song = song;
        }

        /// <summary>How many stings the cast sent.</summary>
        public int Count => _count;

        /// <summary>
        /// Where the first sting's target or air is, following the target as it moves, and in w the seconds the flight takes,
        /// so the picture arrives on the frame the damage starts.
        /// </summary>
        public override Vector4 ParamA => new Vector4(_points[0].x, _points[0].y, _points[0].z, _song.ArriveSeconds);

        /// <summary>How many stings the cast sent, in x, so the picture shares the squad evenly between them.</summary>
        public override Vector4 ParamB => new Vector4(_count, 0f, 0f, 0f);

        /// <summary>Where sting number <paramref name="sting"/> wraps now: its target, followed live, or the air. Zero past the count.</summary>
        public Vector3 Target(int sting)
        {
            return sting >= 0 && sting < _count ? _points[sting] : Vector3.zero;
        }

        protected override void Begin(in SwarmSongContext context)
        {
            _fury = context.Fury ?? throw new ArgumentException("The song context has no Fury meter.", nameof(context));
            _aim = context.Aim;
            _range = _song.MaxRange * context.SizeScale;
            _count = _song.StingCount(context.LivingWasps);
            _damagePerSecond = _song.DamagePerSecond * context.DamageScale / _count;
            int found = ChooseFirstTargets(in context, out Vector3 aim);
            _isAir = found == 0;
            _endSeconds = _isAir ? _song.AirSeconds : _song.Seconds;
            Vector3 air = GroundMath.PullIn(aim, context.CorePosition, _range);

            for (int sting = 0; sting < StingSong.MostStings; sting++)
            {
                bool isSent = sting < _count;
                _targets[sting] = isSent && !_isAir ? _candidates[sting % found] : null;
                _points[sting] = !isSent ? Vector3.zero : (_isAir ? air : _targets[sting].Position);
                _pulseFrom[sting] = _song.ArriveSeconds;
                _pulseTo[sting] = 1;
                _isLive[sting] = isSent;
            }

            _hasArrived = false;
        }

        protected internal override void Tick(in SongTick tick)
        {
            if (!_isAir)
            {
                // A target that died since the last step, whoever killed it, sends its sting on.
                for (int sting = 0; sting < _count; sting++)
                {
                    if (_isLive[sting] && !_targets[sting].IsAlive)
                    {
                        Reaim(sting, tick.SecondsBefore);
                    }
                }

                if (!IsAnyLive())
                {
                    Release(tick.SecondsBefore);
                    return;
                }

                Follow();
            }

            if (!_hasArrived && tick.Seconds >= _song.ArriveSeconds)
            {
                _hasArrived = true;
                LandEach();
            }

            if (!_isAir)
            {
                for (int sting = 0; sting < _count; sting++)
                {
                    if (_isLive[sting])
                    {
                        Hurt(sting, tick.Seconds);
                    }
                }

                if (!IsAnyLive())
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

        // The aim chooser's most threatening beekeepers in reach, one for each sting; without one, the aimed beekeeper when it
        // lives within reach. Returns how many it found, and gives the aim that stings wrap when they found none.
        private int ChooseFirstTargets(in SwarmSongContext context, out Vector3 aim)
        {
            if (_aim != null)
            {
                int chosen = _aim.ChooseThreats(_range, _count, _candidates, out aim);
                return Math.Max(0, Math.Min(chosen, _count));
            }

            ISwarmTarget aimed = context.AimTarget;
            bool isAimAlive = aimed != null && aimed.IsAlive;
            aim = isAimAlive ? aimed.Position : context.AimPoint;

            if (isAimAlive && GroundMath.DistanceSquared(aimed.Position, context.CorePosition) <= _range * _range)
            {
                _candidates[0] = aimed;
                return 1;
            }

            return 0;
        }

        private void Follow()
        {
            for (int sting = 0; sting < _count; sting++)
            {
                if (_isLive[sting])
                {
                    _points[sting] = _targets[sting].Position;
                }
            }
        }

        // Each target, or the air, lands once as the wasps arrive: stings that double up land as one.
        private void LandEach()
        {
            for (int sting = 0; sting < _count; sting++)
            {
                if (_isLive[sting] && !IsSharedWithAnEarlierSting(sting))
                {
                    Land(_points[sting]);
                }
            }
        }

        private bool IsSharedWithAnEarlierSting(int sting)
        {
            for (int other = 0; other < sting; other++)
            {
                if (_isLive[other] && (_isAir || ReferenceEquals(_targets[other], _targets[sting])))
                {
                    return true;
                }
            }

            return false;
        }

        // Every pulse due by `seconds` (a long step can hold several), each dealt and reported at once, up to the end. A target
        // another sting killed in this step sends this sting on instead; one this sting kills sends it on at once.
        private void Hurt(int sting, float seconds)
        {
            ISwarmTarget target = _targets[sting];

            if (!target.IsAlive)
            {
                Reaim(sting, seconds);
                return;
            }

            while (_pulseFrom[sting] <= seconds && _pulseFrom[sting] < _endSeconds)
            {
                float squeeze = SqueezeAt(_pulseTo[sting]);
                float damage = _damagePerSecond * (Math.Min(squeeze, _endSeconds) - _pulseFrom[sting]);
                _pulseFrom[sting] = squeeze;
                _pulseTo[sting]++;

                if (!(damage > 0f))
                {
                    continue;
                }

                Hit(target, damage);
                _fury.AddDamage(damage);

                if (!target.IsAlive)
                {
                    Reaim(sting, seconds);
                    return;
                }
            }
        }

        // The most threatening beekeeper in reach that no other sting wraps, or the most threatening one when every one is
        // wrapped; else what another sting still wraps. With none, the sting ends. It deals its next pulse as it arrives at
        // the new target, unless its last pulse already dealt that time to the old one.
        private void Reaim(int sting, float seconds)
        {
            ISwarmTarget next = NextThreat(sting) ?? TargetOfAnotherSting(sting);

            if (next == null)
            {
                _isLive[sting] = false;
                return;
            }

            _targets[sting] = next;
            _points[sting] = next.Position;
            float arrives = Math.Max(seconds + _song.ReaimSeconds, _song.ArriveSeconds);

            if (arrives > _pulseFrom[sting])
            {
                _pulseFrom[sting] = arrives;

                while (SqueezeAt(_pulseTo[sting]) <= arrives)
                {
                    _pulseTo[sting]++;
                }
            }
        }

        // Seconds after the cast of the picture's squeeze number `squeeze`: 0 is the arrival, then one every SqueezeSeconds.
        private float SqueezeAt(int squeeze)
        {
            return _song.ArriveSeconds + (squeeze * _song.SqueezeSeconds);
        }

        private ISwarmTarget NextThreat(int sting)
        {
            if (_aim == null)
            {
                return null;
            }

            int found = Math.Min(_aim.ChooseThreats(_range, StingSong.MostStings, _candidates, out _), StingSong.MostStings);
            ISwarmTarget mostThreatening = null;

            for (int index = 0; index < found; index++)
            {
                ISwarmTarget candidate = _candidates[index];

                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                if (mostThreatening == null)
                {
                    mostThreatening = candidate;
                }

                if (!IsWrappedByAnother(candidate, sting))
                {
                    return candidate;
                }
            }

            return mostThreatening;
        }

        private bool IsWrappedByAnother(ISwarmTarget target, int sting)
        {
            for (int other = 0; other < _count; other++)
            {
                if (other != sting && _isLive[other] && ReferenceEquals(_targets[other], target))
                {
                    return true;
                }
            }

            return false;
        }

        // The stings were given out most threatening first, so the first one that still wraps a living beekeeper wraps the
        // most threatening of them.
        private ISwarmTarget TargetOfAnotherSting(int sting)
        {
            for (int other = 0; other < _count; other++)
            {
                if (other != sting && _isLive[other] && _targets[other].IsAlive)
                {
                    return _targets[other];
                }
            }

            return null;
        }

        private bool IsAnyLive()
        {
            for (int sting = 0; sting < _count; sting++)
            {
                if (_isLive[sting])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
