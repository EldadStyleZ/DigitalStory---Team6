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
    /// The swarm is the player, and its core is the point it gathers around: the View moves the core, and the session
    /// reads it each tick. While the giant stands the core rides inside it, so the session moves the core with the giant and
    /// the View puts it where <see cref="CorePosition"/> says; when the giant ends, the core stays where it stood. The core
    /// and the giant keep to the play area, so a core the View moved past its edge is put back at the edge the same way.
    /// It is also the aim chooser of the songs it casts (<see cref="ISongAim"/>): an aimed song asks it for its target by the
    /// song's own rule, and the Hydra for one beekeeper a head by the ThreeNearest rule; the mouse wins while it is live. The
    /// marker's target, chosen every tick, uses the rule and reach of the aimed song the player knows from the start (the
    /// Sting), read from its asset, so the marker and that song agree.
    /// A match is rounds of a farm phase and a survive phase (plan section L), and wasps are its one currency (section M):
    /// songs cost wasps only in a survive phase, except a tower, which costs in a farm phase too, and each song's first cast
    /// in the match is free (<see cref="SongPrice"/>); the hives' stores pay for Hive Call, the respawn and what the core
    /// collects beside a hive, Give pays wasps into a hive's health, and calling a wave early earns wasps. The last hive
    /// starts hurt so the first round's lesson (section N) has a hive to heal; once no standing hive is hurt, the lesson's
    /// heal step is done. The running songs hear of each change of phase (<see cref="SwarmFormMachine.NotifyPhase"/>), so a
    /// Honeycomb guards its hive until the wave is cleared.
    /// The swarm grows (section H): each cleared wave opens more nests, and a collected nest raises the swarm's capacity,
    /// brings wasps, hums an unfound song and raises the swarm's level. The level, never the live wasp count, makes the songs
    /// and the giant bigger and stronger: each cast takes <see cref="Level"/>, <see cref="SizeScale"/> and
    /// <see cref="DamageScale"/> as they are then, and the giant keeps the level it stood up at.
    /// Each tick runs in this order: the core, the song caster and the aim, the notes pressed, the swarm's respawn, its form
    /// (Hive Call), the giant, the nests the core stands at, the hive the core stands at, the lesson's catch-up steps, smoke,
    /// the hives' stores, the beekeepers, the wave timeline, a wave called early, and last the state.
    /// </remarks>
    public sealed class HiveDefenseSession : ISongAim
    {
        // Not game tuning: a core that moved less than this (squared metres) since the last tick counts as standing still.
        private const float MovedSqrMetres = 0.0001f;

        // Not game tuning: a step shorter than 1 mm (the controller settling) does not turn the core.
        private const float TurnSqrMetres = 0.000001f;
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
        private readonly SlotSongEffect _markerSong;
        private readonly HiveExchange _exchange;

        private HiveDefenseState _state = HiveDefenseState.Playing;
        private Vector3 _corePosition;
        private Vector3 _coreFacing = Vector3.forward;
        private Vector3 _lastHitPoint;
        private Vector3 _aimPoint;
        private ISwarmTarget _aimTarget;
        private bool _isMouseLive;
        private Vector3 _mousePoint;
        private int _activeCount;
        private bool _isColossusActive;
        private bool _isCoreSettling;
        private bool _furyHasFilled;
        private Vector3 _colossusPosition;
        private Vector3 _colossusFacing = Vector3.forward;
        private float _colossusSizeScale = 1f;
        private float _colossusDamageScale = 1f;

        /// <summary>
        /// Builds the hives, the nests, the beekeeper pools and every other class from the rules. The nest positions come in
        /// the order the nests open (<see cref="NestField"/>), as many as <see cref="NestConfig.NestCount"/>; the scene checks
        /// where they stand with <see cref="NestLayout.Check"/>.
        /// </summary>
        public HiveDefenseSession(
            HiveDefenseRules rules,
            IReadOnlyList<Vector3> hivePositions,
            IReadOnlyList<Vector3> nestPositions,
            IReadOnlyList<Vector3> spawnPoints,
            Vector3 coreStart,
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

            _markerSong = FindMarkerSong(rules.Songs);
            _corePosition = KeepInPlayArea(coreStart);
            _lastHitPoint = _corePosition;

            Swarm = new SwarmBody(rules.Swarm);
            Fury = new FuryMeter(rules.Fury);
            Forms = new SwarmFormMachine(Swarm, Fury, rules.Squads);

            // The last hive starts hurt, so the first round's lesson has a hive to heal.
            var hives = new List<Hive>(hivePositions.Count);
            int lastHive = hivePositions.Count - 1;
            for (int index = 0; index < hivePositions.Count; index++)
            {
                hives.Add(new Hive(rules.Hive, hivePositions[index], index == lastHive));
            }

            Hives = new HiveSet(hives);
            Nests = new NestField(rules.Nests, nestPositions);
            _exchange = new HiveExchange(rules.Hive, Hives, Swarm);
            Waves = new WaveDirector(rules.Waves, spawnPoints.Count);
            Caster = new SwarmSongCaster(rules.Songs, rules.SongConfig, rules.HoleCount);
            Smoke = new SmokeField(rules.Smoke);
            _songs = rules.Songs;
            Discovery = new SongDiscovery(rules.Songs);
            Tutorial = new Tutorial();
            Caster.SongCast += Discovery.NoteCast;
            Caster.SongCast += OnSongCast;
            Caster.NotePlayed += OnNotePlayed;
            Fury.Changed += OnFuryChanged;
            Waves.PhaseChanged += OnPhaseChanged;
            Waves.WaveCleared += Nests.OpenAfterWave;
            Nests.NestCollected += OnNestCollected;
            _exchange.Gave += OnGave;

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
                brain.CoreHit += OnCoreHit;
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

        /// <summary>The wild nests: which are open, which are collected, and how far the collecting has got.</summary>
        public NestField Nests { get; }

        /// <summary>The swarm's level: 1 plus the nests collected. It, never the live wasp count, sets the songs' strength.</summary>
        public int Level => 1 + Nests.CollectedCount;

        /// <summary>What the level multiplies every song's radius, reach and height by, now (plan section H).</summary>
        public float SizeScale => _rules.SwarmLevel.SizeScale(Level);

        /// <summary>What the level multiplies every song's damage by, now (plan section H).</summary>
        public float DamageScale => _rules.SwarmLevel.DamageScale(Level);

        /// <summary>The rounds: each a farm phase and then a wave.</summary>
        public WaveDirector Waves { get; }

        /// <summary>Farm or Survive, as of the last tick (plan section L).</summary>
        public MatchPhase Phase => Waves.Phase;

        /// <summary>Seconds until the farm phase ends and the wave comes. 0 in a survive phase.</summary>
        public float FarmSecondsLeft => Waves.FarmSecondsLeft;

        /// <summary>The round being played, from 1 to <see cref="RoundCount"/>.</summary>
        public int Round => Waves.Round;

        /// <summary>How many rounds the match has.</summary>
        public int RoundCount => Waves.RoundCount;

        /// <summary>
        /// True in a farm phase, when songs cost no wasps, so trying phrases is always free; a tower still costs its price
        /// there. <see cref="SongPrice"/> says what a song costs now.
        /// </summary>
        public bool AreSongsFree => Waves.Phase == MatchPhase.Farm;

        /// <summary>The song caster, for the HUD.</summary>
        public SwarmSongCaster Caster { get; }

        /// <summary>
        /// The wasps casting song number <paramref name="song"/> would cost now, for the HUD's chips: nothing for its first
        /// cast in the match, nothing in a farm phase unless it is a tower, and its price otherwise.
        /// </summary>
        public int SongPrice(int song)
        {
            return Caster.Price(song, AreSongsFree);
        }

        /// <summary>
        /// Which songs the player knows, and how much of each song's beginning was played (the song table). A cast makes a
        /// song known, and the first full Fury reveals the songs that wait for it.
        /// </summary>
        public SongDiscovery Discovery { get; }

        /// <summary>The first round's lesson: the prompt to show, one at a time.</summary>
        public Tutorial Tutorial { get; }

        /// <summary>The smoke grenades and clouds.</summary>
        public SmokeField Smoke { get; }

        /// <summary>The standing hive within its collect radius of the core as of the last tick, or -1 when none is.</summary>
        public int NearHive => _exchange.NearHive;

        /// <summary>True while the nearby hive's store flows into the swarm.</summary>
        public bool IsCollecting => _exchange.IsCollecting;

        /// <summary>True while Give is held beside a hurt hive and wasps flow from the swarm into its health.</summary>
        public bool IsGiving => _exchange.IsGiving;

        /// <summary>Raised with the wasps given for calling a wave early, when any were (the swarm may be full).</summary>
        public event Action<int> WaspsGranted;

        /// <summary>
        /// Where the core is as of the last tick: where the View moved it, or, while the giant stands, the giant's place on
        /// the ground at the core's own height. When the giant ends the core stays where the giant stood. Either way it is
        /// inside the play area, a square <see cref="CoreMoveConfig.PlayAreaHalfSize"/> from the world origin on each side.
        /// </summary>
        public Vector3 CorePosition => _corePosition;

        /// <summary>
        /// The way the core last moved, a unit vector on the ground; forward until it first moves. While the giant walks the
        /// core moves with it. An aimed song with nothing to strike strikes the ground ahead of it.
        /// </summary>
        public Vector3 CoreFacing => _coreFacing;

        /// <summary>
        /// How much the core's speed is multiplied: by the running songs (the dome slows it), and in smoke unless a running
        /// song keeps smoke from slowing it. 1 otherwise.
        /// </summary>
        public float CoreSpeedMultiplier
        {
            get
            {
                float multiplier = Forms.CoreSpeedMultiplier;
                if (Smoke.IsCoreInSmoke && Forms.SmokeSlowsCore)
                {
                    multiplier *= _rules.CoreMove.SmokeSpeedMultiplier;
                }

                return multiplier;
            }
        }

        /// <summary>
        /// True while the core must stand still: Hive Call. The giant does not root it, because the core rides inside the
        /// giant and nothing is left standing behind.
        /// </summary>
        public bool IsCoreRooted => Forms.IsCoreRooted;

        /// <summary>
        /// The beekeeper an aimed song would strike as of the last tick, or null when there is none: while the mouse is live
        /// the living beekeeper nearest its ground point within reach, otherwise auto-aim's most threatening beekeeper.
        /// </summary>
        public ISwarmTarget AimTarget => _aimTarget != null && _aimTarget.IsAlive ? _aimTarget : null;

        /// <summary>
        /// Where an aimed song strikes as of the last tick: the aimed beekeeper, the mouse's ground point while the mouse is
        /// live, or else the ground ahead of where the core last moved.
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

        /// <summary>
        /// What the giant's size, and the reach of its hits, is multiplied by: <see cref="SizeScale"/> as it was when the
        /// giant stood up, kept while it stands. 1 before the first giant.
        /// </summary>
        public float ColossusSizeScale => _colossusSizeScale;

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

            ReadCore(in frame);
            Forms.SetCore(_corePosition);

            Caster.Tick(dt);
            _isMouseLive = frame.IsMouseLive;
            _mousePoint = frame.AimPoint;
            _autoAim.Tick(dt);
            Choose(_markerSong.AimRule, _markerSong.AimRange * SizeScale);
            PressHoles(frame.PressedHoleMask);

            Swarm.Tick(dt, Hives);
            Forms.Tick(dt, Hives);

            TickColossus(frame, dt);

            // A collapsed swarm has nothing to take a nest with, so its stay starts again.
            Nests.Tick(dt, _corePosition, !Swarm.IsCollapsed);
            _exchange.Tick(dt, _corePosition, frame.Give, Forms.Current == SwarmForm.HiveCall);
            TickLesson();
            Smoke.Tick(dt, Hives, Swarm, _corePosition, Forms);
            Hives.Tick(dt);
            TickBeekeepers(dt);
            Waves.Tick(dt, _activeCount);
            if (frame.CallWave)
            {
                CallWaveEarly();
            }

            if (Hives.AllDestroyed)
            {
                SetState(HiveDefenseState.Lost);
            }
            else if (Waves.AllWavesCleared)
            {
                SetState(HiveDefenseState.Won);
            }
        }

        // In a farm phase only: the wave comes now, and the seconds skipped earn wasps, as many as the swarm has room for.
        private void CallWaveEarly()
        {
            float skipped = Waves.CallWave();
            int earned = Mathf.FloorToInt(skipped * _rules.Waves.EarlyBonusPerSecond);
            if (earned <= 0)
            {
                return;
            }

            int granted = Swarm.Gain(earned);
            if (granted > 0)
            {
                WaspsGranted?.Invoke(granted);
            }
        }

        private void OnSongCast(int song)
        {
            if (ReferenceEquals(_songs[song], _markerSong))
            {
                Tutorial.Complete(TutorialStep.PlaySting);
            }
        }

        // Plan section H: the nest is gone, and the swarm can hold more, has more, and is one level stronger. It hums a song
        // the player has not found, chosen with the session's random source.
        private void OnNestCollected(int nest)
        {
            NestConfig config = _rules.Nests;
            Swarm.GrowCapacity(config.CapacityPerNest);
            Swarm.Gain(config.WaspsPerNest);
            Discovery.HumUnfound(_random, config.HummedNotes);
            Tutorial.Complete(TutorialStep.CollectNest);
        }

        // The song table fills in from every note the flute hears, before the note can cast a song and use the notes up.
        private void OnNotePlayed(int hole)
        {
            Discovery.RecordNotes(Caster);
        }

        // The running songs hear of every change of phase, so a tower that guards a wave knows when the wave is over.
        private void OnPhaseChanged(MatchPhase phase)
        {
            Forms.NotifyPhase(phase);

            if (phase == MatchPhase.Survive)
            {
                Tutorial.Finish();
            }
        }

        private void OnGave(int hive)
        {
            Tutorial.Complete(TutorialStep.HealHive);
        }

        // Each step that the player may have done before its turn, or that may have nothing to teach, is caught up here.
        // Checked each tick, not when the step changes, so StepChanged is never raised from inside itself.
        // A nest collected before its step came would leave the nest prompt up, and with every open nest taken it has
        // nothing to teach. The swarm starts full and a nest keeps it full, so a visit often takes nothing: reaching a
        // standing hive is the visit, and its store flows in by itself whenever there is room. Gave is raised only while a
        // hive is hurt, and a full hive stays full in the first farm phase, so with no standing hive hurt the heal step has
        // nothing to teach either.
        private void TickLesson()
        {
            if (Tutorial.Current == TutorialStep.CollectNest && (Nests.CollectedCount > 0 || Nests.OpenCount == 0))
            {
                Tutorial.Complete(TutorialStep.CollectNest);
            }

            if (Tutorial.Current == TutorialStep.VisitHive && _exchange.NearHive >= 0)
            {
                Tutorial.Complete(TutorialStep.VisitHive);
            }

            if (Tutorial.Current == TutorialStep.HealHive && !Hives.HasDamagedHive)
            {
                Tutorial.Complete(TutorialStep.HealHive);
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

        // The marker shows what the starting aimed song would strike. Without one it would mark beekeepers by numbers that no
        // song uses, so a song list without it is a setup mistake.
        private static SlotSongEffect FindMarkerSong(IReadOnlyList<SwarmSongEffect> songs)
        {
            for (int index = 0; index < songs.Count; index++)
            {
                if (songs[index] is SlotSongEffect song && song.IsAimed && song.KnownFromStart && song.AimRule == AimRule.Threat)
                {
                    return song;
                }
            }

            throw new ArgumentException(
                "The songs need one aimed song that is known from the start and aims by the Threat rule (the Sting): the aim marker uses its reach.",
                "rules");
        }

        // While the giant stands the core rides inside it, so the View's core is ignored and TickColossus moves it. Otherwise
        // the core is where the View moved it, held inside the play area, and faces the way it moved; only the step on the
        // ground counts, and a step too short to turn it keeps the last facing. The core is held in place while it is rooted,
        // so asking to move counts as moving: it ends Hive Call, and it is the lesson's first step. The first read after the
        // giant ends is the core settling where it was put, which can be inside a hive or a wall that the controller pushes
        // it out of, so it neither turns the core nor counts as a move.
        private void ReadCore(in HiveDefenseFrame frame)
        {
            if (_isColossusActive)
            {
                return;
            }

            Vector3 core = KeepInPlayArea(frame.CorePosition);
            if (_isCoreSettling)
            {
                _isCoreSettling = false;
                _corePosition = core;
                return;
            }

            Vector3 step = core - _corePosition;
            step.y = 0f;
            float stepSqr = step.sqrMagnitude;
            if (stepSqr > MovedSqrMetres || frame.Move.sqrMagnitude > 0f)
            {
                Forms.NotifyCoreMoved();
                Tutorial.Complete(TutorialStep.Move);
            }

            if (stepSqr > TurnSqrMetres)
            {
                _coreFacing = step / Mathf.Sqrt(stepSqr);
            }

            _corePosition = core;
        }

        // The ground ends, so the core and the giant keep to a square around the world origin, the middle of the map.
        // Only the place on the ground is held; the height stays.
        private Vector3 KeepInPlayArea(Vector3 position)
        {
            float half = _rules.CoreMove.PlayAreaHalfSize;
            position.x = Mathf.Clamp(position.x, -half, half);
            position.z = Mathf.Clamp(position.z, -half, half);
            return position;
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

        /// <summary>
        /// Chooses the Hydra's targets by the ThreeNearest rule, as it is cast: auto-aim's nearest beekeepers within reach,
        /// nearest first, as many as asked for and auto-aim holds (<see cref="AimConfig.NearestCount"/>). Its single pick
        /// becomes <see cref="AimTarget"/>, as for any cast.
        /// </summary>
        int ISongAim.ChooseNearest(float range, int count, ISwarmTarget[] targets, out Vector3 point)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            Choose(AimRule.ThreeNearest, range);
            point = _aimPoint;
            int chosen = Mathf.Min(Mathf.Min(count, targets.Length), _autoAim.NearestCount);
            for (int slot = 0; slot < chosen; slot++)
            {
                targets[slot] = _enemies[_autoAim.GetNearest(slot)];
            }

            return Mathf.Max(0, chosen);
        }

        private void Choose(AimRule rule, float range)
        {
            _autoAim.Select(rule, range, _corePosition, _coreFacing, _isMouseLive, _mousePoint);
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

                var context = new SwarmSongContext(
                    _corePosition, AimTarget, _aimPoint, Swarm, Forms, Fury, Hives, _enemies, Smoke, this, AreSongsFree,
                    SizeScale, DamageScale, Level);
                Caster.Press(hole, context);
            }
        }

        private void TickColossus(in HiveDefenseFrame frame, float dt)
        {
            bool formIsColossus = Forms.Current == SwarmForm.Colossus;
            if (formIsColossus && !_isColossusActive)
            {
                _isColossusActive = true;
                _colossusPosition = new Vector3(_corePosition.x, 0f, _corePosition.z);
                _colossusFacing = Vector3.forward;
                _colossusSizeScale = SizeScale;
                _colossusDamageScale = DamageScale;
            }
            else if (!formIsColossus && _isColossusActive)
            {
                _isColossusActive = false;
                _isCoreSettling = true;
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
                // The giant walks through hives and scenery, but never past the play area, where the ground ends.
                _colossusPosition = KeepInPlayArea(_colossusPosition + (new Vector3(move.x, 0f, move.y) * (_rules.Colossus.Speed * dt)));
                _colossusFacing = new Vector3(move.x, 0f, move.y).normalized;
                _coreFacing = _colossusFacing;
            }

            // The core rides inside the giant, so the beekeepers chase and hit the giant, and the core is left where the giant
            // stood when it ends.
            _corePosition = new Vector3(_colossusPosition.x, _corePosition.y, _colossusPosition.z);

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

                var senses = new BeekeeperSenses(_corePosition, Hives, SeparationFor(slot));
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
            Hives[hiveIndex].Damage(brain.Config.HiveDamage * Forms.HiveDamageFraction(hiveIndex));
            Forms.NotifyHiveHit(hiveIndex, brain);
        }

        private void OnCoreHit(BeekeeperBrain brain)
        {
            Swarm.Lose(brain.Config.WaspsKnockedOff, Forms.WaspLossFraction);
            Forms.NotifyHit(brain.Position);
            Nests.NotifyCoreHit();
            _lastHitPoint = _corePosition;
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

            // The giant hits as hard, and as far, as the level it stood up at.
            damage *= _colossusDamageScale;
            radius *= _colossusSizeScale;
            reach *= _colossusSizeScale;
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
