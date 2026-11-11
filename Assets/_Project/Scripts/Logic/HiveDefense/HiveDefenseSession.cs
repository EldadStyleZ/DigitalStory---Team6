using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Owns every F-010 Logic class and ticks them in a fixed order. It turns events into effects (hive damage,
    /// wasp loss, smoke throws, spawns from a pool of beekeepers), drives the giant, and decides Playing, Won or Lost.
    /// After Won or Lost, <see cref="Tick"/> does nothing.
    /// </summary>
    /// <remarks>
    /// It is also the aim chooser of the songs it casts (<see cref="ISongAim"/>): an aimed song asks it for its target by the
    /// song's own rule, and the mouse wins while it is live.
    /// </remarks>
    public sealed class HiveDefenseSession : ISongAim
    {
        // Not game tuning: a Piper that moved less than this (squared metres) since the last tick counts as standing still.
        private const float MovedSqrMetres = 0.0001f;
        private const float CoincidentSqrMetres = 0.00000001f;
        private const int MaxHoleBits = 31;

        private readonly HiveDefenseRules _rules;
        private readonly IReadOnlyList<Vector3> _spawnPoints;
        private readonly IRandomSource _random;
        private readonly BeekeeperBrain[] _pool;
        private readonly bool[] _isActive;
        private readonly ISwarmTarget[] _enemies;
        private readonly int _stickPoolSize;
        private readonly ColossusAttackChain _chain;
        private readonly IReadOnlyList<SwarmSongEffect> _songs;
        private readonly AutoAim _autoAim;

        private HiveDefenseState _state = HiveDefenseState.Playing;
        private Vector3 _piperPosition;
        private Vector3 _piperFacing = Vector3.forward;
        private Vector3 _lastHitPoint;
        private Vector3 _aimPoint;
        private ISwarmTarget _aimTarget;
        private bool _isMouseLive;
        private Vector3 _mousePoint;
        private int _activeCount;
        private bool _isColossusActive;
        private bool _furyHasFilled;
        private Vector3 _colossusPosition;
        private Vector3 _colossusFacing = Vector3.forward;

        /// <summary>Builds the hives, the beekeeper pools and every other class from the rules.</summary>
        public HiveDefenseSession(
            HiveDefenseRules rules,
            IReadOnlyList<Vector3> hivePositions,
            IReadOnlyList<Vector3> spawnPoints,
            Vector3 piperStart,
            IRandomSource random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (hivePositions == null)
            {
                throw new ArgumentNullException(nameof(hivePositions));
            }

            _spawnPoints = spawnPoints ?? throw new ArgumentNullException(nameof(spawnPoints));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            if (hivePositions.Count == 0)
            {
                throw new ArgumentException("At least one hive position is needed.", nameof(hivePositions));
            }

            if (spawnPoints.Count == 0)
            {
                throw new ArgumentException("At least one spawn point is needed.", nameof(spawnPoints));
            }

            if (rules.HoleCount < 1 || rules.HoleCount > MaxHoleBits)
            {
                throw new ArgumentOutOfRangeException(nameof(rules), "HoleCount must be from 1 to " + MaxHoleBits + ".");
            }

            _piperPosition = piperStart;
            _lastHitPoint = piperStart;

            Swarm = new SwarmBody(rules.Swarm);
            Fury = new FuryMeter(rules.Fury);
            Forms = new SwarmFormMachine(Swarm, Fury, rules.Squads);

            var hives = new List<Hive>(hivePositions.Count);
            for (int index = 0; index < hivePositions.Count; index++)
            {
                hives.Add(new Hive(rules.Hive, hivePositions[index]));
            }

            Hives = new HiveSet(hives);
            Waves = new WaveDirector(rules.Waves, spawnPoints.Count);
            Caster = new SwarmSongCaster(rules.Songs, rules.SongConfig, rules.HoleCount);
            Smoke = new SmokeField(rules.Smoke);
            _songs = rules.Songs;
            Discovery = new SongDiscovery(rules.Songs);
            Caster.SongCast += Discovery.NoteCast;
            Fury.Changed += OnFuryChanged;

            int sticks = 0;
            int smokers = 0;
            IReadOnlyList<WaveEntry> waves = rules.Waves.Waves;
            for (int index = 0; index < waves.Count; index++)
            {
                sticks = Mathf.Max(sticks, waves[index].Sticks);
                smokers = Mathf.Max(smokers, waves[index].Smokers);
            }

            _stickPoolSize = sticks;
            _pool = new BeekeeperBrain[sticks + smokers];
            _isActive = new bool[_pool.Length];
            _enemies = new ISwarmTarget[_pool.Length];
            for (int index = 0; index < _pool.Length; index++)
            {
                var brain = new BeekeeperBrain(index < sticks ? rules.Stick : rules.Smoker);
                brain.HiveHit += OnHiveHit;
                brain.PiperHit += OnPiperHit;
                brain.GrenadeThrown += OnGrenadeThrown;
                brain.Died += OnBeekeeperDied;
                _pool[index] = brain;
                _enemies[index] = brain;
            }

            _chain = new ColossusAttackChain(rules.Colossus);
            _chain.Started += OnColossusStarted;
            _chain.Landed += OnColossusLanded;

            _autoAim = new AutoAim(rules.Aim, _enemies, Hives);
            Waves.SpawnRequested += OnSpawnRequested;
        }

        /// <summary>Playing, Won or Lost.</summary>
        public HiveDefenseState State => _state;

        /// <summary>Raised once, when the state leaves Playing.</summary>
        public event Action<HiveDefenseState> StateChanged;

        /// <summary>The swarm, which is the player's health.</summary>
        public SwarmBody Swarm { get; }

        /// <summary>The Fury meter.</summary>
        public FuryMeter Fury { get; }

        /// <summary>The swarm's forms.</summary>
        public SwarmFormMachine Forms { get; }

        /// <summary>The hives.</summary>
        public HiveSet Hives { get; }

        /// <summary>The wave timeline.</summary>
        public WaveDirector Waves { get; }

        /// <summary>The song caster, for the HUD.</summary>
        public SwarmSongCaster Caster { get; }

        /// <summary>Which songs the player knows. A cast makes a song known, and the first full Fury reveals the songs that wait for it.</summary>
        public SongDiscovery Discovery { get; }

        /// <summary>The smoke grenades and clouds.</summary>
        public SmokeField Smoke { get; }

        /// <summary>The Piper's position as of the last tick.</summary>
        public Vector3 PiperPosition => _piperPosition;

        /// <summary>How much the Piper's speed is multiplied: slower in the dome and in smoke, 1 otherwise.</summary>
        public float PiperSpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                if (Forms.IsDomed)
                {
                    multiplier *= _rules.PiperMove.DomeSpeedMultiplier;
                }

                if (Smoke.IsPiperInSmoke)
                {
                    multiplier *= _rules.PiperMove.SmokeSpeedMultiplier;
                }

                return multiplier;
            }
        }

        /// <summary>True while the Piper must stand still (Hive Call and the giant).</summary>
        public bool IsPiperRooted => Forms.IsPiperRooted;

        /// <summary>
        /// The beekeeper an aimed song would strike as of the last tick, or null when there is none: while the mouse is live
        /// the living beekeeper nearest its ground point within reach, otherwise auto-aim's most threatening beekeeper.
        /// </summary>
        public ISwarmTarget AimTarget => _aimTarget != null && _aimTarget.IsAlive ? _aimTarget : null;

        /// <summary>
        /// Where an aimed song strikes as of the last tick: the aimed beekeeper, the mouse's ground point while the mouse is
        /// live, or else the ground ahead of where the Piper faces.
        /// </summary>
        public Vector3 AimPoint => _aimPoint;

        /// <summary>Where the swarm was last hit, for the scatter effect.</summary>
        public Vector3 LastHitPoint => _lastHitPoint;

        /// <summary>How many beekeepers are alive on the field.</summary>
        public int ActiveBeekeeperCount => _activeCount;

        /// <summary>The n-th active beekeeper, counting from 0 in pool order.</summary>
        public BeekeeperBrain ActiveBeekeeper(int index)
        {
            if (index < 0 || index >= _activeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int seen = 0;
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot])
                {
                    continue;
                }

                if (seen == index)
                {
                    return _pool[slot];
                }

                seen++;
            }

            throw new InvalidOperationException("The active count does not match the pool.");
        }

        /// <summary>Raised when a beekeeper is spawned, after it stands at its spawn point.</summary>
        public event Action<BeekeeperBrain> BeekeeperSpawned;

        /// <summary>Raised when a beekeeper dies, for the View to release it.</summary>
        public event Action<BeekeeperBrain> BeekeeperDied;

        /// <summary>True while the giant stands.</summary>
        public bool IsColossusActive => _isColossusActive;

        /// <summary>Where the giant stands.</summary>
        public Vector3 ColossusPosition => _colossusPosition;

        /// <summary>The direction the giant faces, a unit vector on the ground.</summary>
        public Vector3 ColossusFacing => _colossusFacing;

        /// <summary>Raised when a giant attack starts, for the animation.</summary>
        public event Action<ColossusAttack> ColossusAttackStarted;

        /// <summary>Raised when a giant attack lands, after its hit checks.</summary>
        public event Action<ColossusAttack> ColossusAttackLanded;

        /// <summary>Advances the match. Does nothing after Won or Lost. <paramref name="deltaTime"/> is clamped to the swarm's maximum.</summary>
        public void Tick(in HiveDefenseFrame frame, float deltaTime)
        {
            if (_state != HiveDefenseState.Playing || deltaTime <= 0f)
            {
                return;
            }

            float dt = Mathf.Min(deltaTime, _rules.Swarm.MaxDeltaTime);

            Vector3 step = frame.PiperPosition - _piperPosition;
            step.y = 0f;
            if (step.sqrMagnitude > MovedSqrMetres)
            {
                Forms.NotifyPiperMoved();
            }

            _piperPosition = frame.PiperPosition;
            _piperFacing = GroundFacing(frame.PiperFacing, _piperFacing);
            Forms.SetPiper(_piperPosition);

            Caster.Tick(dt);
            _isMouseLive = frame.IsMouseLive;
            _mousePoint = frame.AimPoint;
            _autoAim.Tick(dt);
            Choose(AimRule.Threat, _rules.Aim.StingRange);
            PressHoles(frame.PressedHoleMask);

            int standing = Hives.StandingCount;
            Swarm.Tick(dt, standing);
            Forms.Tick(dt, standing);

            TickColossus(frame, dt);
            Smoke.Tick(dt, Hives, Swarm, _piperPosition, Forms.IsDomed);
            Hives.Tick(dt);
            TickBeekeepers(dt);
            Waves.Tick(dt, _activeCount);

            if (Hives.AllDestroyed)
            {
                SetState(HiveDefenseState.Lost);
            }
            else if (Waves.AllWavesCleared)
            {
                SetState(HiveDefenseState.Won);
            }
        }

        private void OnFuryChanged(float fraction)
        {
            if (_furyHasFilled || !Fury.IsFull)
            {
                return;
            }

            _furyHasFilled = true;
            for (int song = 0; song < _songs.Count; song++)
            {
                if (_songs[song].RevealedWhenFuryFills)
                {
                    Discovery.Reveal(song);
                }
            }
        }

        // Only the direction on the ground counts. A facing with no length keeps the last one.
        private static Vector3 GroundFacing(Vector3 facing, Vector3 last)
        {
            facing.y = 0f;
            float sqr = facing.sqrMagnitude;
            return sqr > CoincidentSqrMetres ? facing / Mathf.Sqrt(sqr) : last;
        }

        private void SetState(HiveDefenseState state)
        {
            _state = state;
            StateChanged?.Invoke(state);
        }

        /// <summary>Chooses an aimed song's target by its rule, as the song is cast. The choice becomes <see cref="AimTarget"/>.</summary>
        void ISongAim.Choose(AimRule rule, float range, out ISwarmTarget target, out Vector3 point)
        {
            Choose(rule, range);
            target = AimTarget;
            point = _aimPoint;
        }

        private void Choose(AimRule rule, float range)
        {
            _autoAim.Select(rule, range, _piperPosition, _piperFacing, _isMouseLive, _mousePoint);
            _aimTarget = _autoAim.Target;
            _aimPoint = _autoAim.AimPoint;
        }

        private void PressHoles(int mask)
        {
            if (mask == 0)
            {
                return;
            }

            for (int hole = 0; hole < _rules.HoleCount; hole++)
            {
                if ((mask & HiveDefenseFrame.HoleBit(hole)) == 0)
                {
                    continue;
                }

                // While the giant stands no note reaches the caster, so presses made out of habit cannot leak into the
                // next song's window.
                if (Forms.Current == SwarmForm.Colossus)
                {
                    return;
                }

                var context = new SwarmSongContext(_piperPosition, AimTarget, _aimPoint, Swarm, Forms, Fury, Hives, _enemies, Smoke, this);
                Caster.Press(hole, context);
            }
        }

        private void TickColossus(in HiveDefenseFrame frame, float dt)
        {
            bool formIsColossus = Forms.Current == SwarmForm.Colossus;
            if (formIsColossus && !_isColossusActive)
            {
                _isColossusActive = true;
                _colossusPosition = new Vector3(_piperPosition.x, 0f, _piperPosition.z);
                _colossusFacing = Vector3.forward;
            }
            else if (!formIsColossus && _isColossusActive)
            {
                _isColossusActive = false;
            }

            // The chain always ticks, so an attack that was started cannot linger into the next giant.
            _chain.Tick(dt);

            if (!_isColossusActive)
            {
                return;
            }

            Vector2 move = frame.Move;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (move.sqrMagnitude > 0f)
            {
                _colossusPosition += new Vector3(move.x, 0f, move.y) * (_rules.Colossus.Speed * dt);
                _colossusFacing = new Vector3(move.x, 0f, move.y).normalized;
            }

            if (frame.Punch)
            {
                _chain.TryPunch();
            }

            if (frame.Kick)
            {
                _chain.TryKick();
            }
        }

        private void TickBeekeepers(float dt)
        {
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot])
                {
                    continue;
                }

                var senses = new BeekeeperSenses(_piperPosition, Hives, SeparationFor(slot));
                _pool[slot].Tick(in senses, dt);
            }
        }

        private Vector3 SeparationFor(int slot)
        {
            BeekeeperBrain self = _pool[slot];
            float radius = self.Config.SeparationRadius;
            Vector3 push = Vector3.zero;
            if (radius <= 0f)
            {
                return push;
            }

            float radiusSqr = radius * radius;
            for (int other = 0; other < _pool.Length; other++)
            {
                if (other == slot || !_isActive[other])
                {
                    continue;
                }

                Vector3 offset = self.Position - _pool[other].Position;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr >= radiusSqr)
                {
                    continue;
                }

                if (sqr < CoincidentSqrMetres)
                {
                    float angle = (float)(_random.NextDouble() * 2.0 * Math.PI);
                    push += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    continue;
                }

                float distance = Mathf.Sqrt(sqr);
                push += offset / distance * (1f - distance / radius);
            }

            return push;
        }

        private void OnSpawnRequested(BeekeeperKind kind, int spawnPointIndex)
        {
            int first = kind == BeekeeperKind.Stick ? 0 : _stickPoolSize;
            int end = kind == BeekeeperKind.Stick ? _stickPoolSize : _pool.Length;
            for (int slot = first; slot < end; slot++)
            {
                if (_isActive[slot])
                {
                    continue;
                }

                _isActive[slot] = true;
                _activeCount++;
                _pool[slot].Spawn(_spawnPoints[spawnPointIndex]);
                BeekeeperSpawned?.Invoke(_pool[slot]);
                return;
            }

            throw new InvalidOperationException("The " + kind + " pool is empty: a wave asked for more beekeepers than its capacity.");
        }

        private void OnBeekeeperDied(BeekeeperBrain brain)
        {
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (_pool[slot] != brain)
                {
                    continue;
                }

                if (_isActive[slot])
                {
                    _isActive[slot] = false;
                    _activeCount--;
                    BeekeeperDied?.Invoke(brain);
                }

                return;
            }
        }

        private void OnHiveHit(BeekeeperBrain brain, int hiveIndex)
        {
            Hives[hiveIndex].Damage(brain.Config.HiveDamage);
        }

        private void OnPiperHit(BeekeeperBrain brain)
        {
            Swarm.Lose(brain.Config.WaspsKnockedOff, Forms.IsDomed);
            Forms.NotifyHit(brain.Position);
            _lastHitPoint = _piperPosition;
        }

        private void OnGrenadeThrown(BeekeeperBrain brain, Vector3 landing)
        {
            Smoke.Throw(brain.Position, landing);
        }

        private void OnColossusStarted(ColossusAttack attack)
        {
            ColossusAttackStarted?.Invoke(attack);
        }

        private void OnColossusLanded(ColossusAttack attack)
        {
            if (_isColossusActive)
            {
                ApplyColossusHit(attack);
            }

            ColossusAttackLanded?.Invoke(attack);
        }

        private void ApplyColossusHit(ColossusAttack attack)
        {
            ColossusConfig config = _rules.Colossus;
            float damage;
            float radius;
            float reach;
            float knockback = 0f;
            switch (attack)
            {
                case ColossusAttack.Punch:
                    damage = config.PunchDamage;
                    radius = config.PunchRadius;
                    reach = config.PunchReach;
                    break;
                case ColossusAttack.HeavyPunch:
                    damage = config.HeavyPunchDamage;
                    radius = config.PunchRadius;
                    reach = config.PunchReach;
                    knockback = config.HeavyPunchKnockback;
                    break;
                case ColossusAttack.Kick:
                    damage = config.KickDamage;
                    radius = config.KickRadius;
                    reach = config.KickReach;
                    break;
                default:
                    return;
            }

            Vector3 center = _colossusPosition + _colossusFacing * reach;
            float radiusSqr = radius * radius;
            float dealt = 0f;
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot] || !_pool[slot].IsAlive)
                {
                    continue;
                }

                BeekeeperBrain brain = _pool[slot];
                Vector3 offset = brain.Position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                dealt += Mathf.Min(damage, brain.Health);
                brain.TakeDamage(damage);
                if (brain.IsAlive)
                {
                    brain.Stun(config.StunSeconds);
                    if (knockback > 0f)
                    {
                        brain.Knockback(_colossusFacing * knockback);
                    }
                }
            }

            if (dealt > 0f)
            {
                Forms.AddColossusDamage(dealt);
            }
        }
    }
}
