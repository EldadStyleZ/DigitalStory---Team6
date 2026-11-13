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
    /// What changed after the owner played (plan section O): a collapsed swarm cannot be seen, chased, hit or smoked, so the
    /// beekeepers' senses say so until it respawns; the session raises <see cref="SongCastNow"/> on the tick a song is cast,
    /// and <see cref="SongLanded"/> for every hit a song or the giant lands on a beekeeper and every blow a shield blocks,
    /// for the feel of each; the Sting asks it for one target a sting (<see cref="ISongAim.ChooseThreats"/>); and
    /// <see cref="ShowsDomeHint"/> says when the HUD names the Dome's use.
    /// The giant, version 2 (plan section I): while it stands the holes are its moves and no note reaches the songs (H roars,
    /// J or the punch button punches, K or the kick button kicks, L slams, and K K and L L are the spin kick and the leap
    /// slam), and the move steers it relative to the camera's way on the ground (<see cref="HiveDefenseFrame.CameraForward"/>).
    /// Each kill adds to its time (<see cref="ColossusSecondsLeft"/>), and the leap slam flies to this tick's aim, pulled in
    /// to its range, and lands before its time can end it. Every blow is reported through <see cref="SongLanded"/>; the slam,
    /// the leap and the third punch are the heavy ones.
    /// </remarks>
    public sealed class HiveDefenseSession : ISongAim
    {
        // Not game tuning: a core that moved less than this (squared metres) since the last tick counts as standing still.
        private const float MovedSqrMetres = 0.0001f;

        // Not game tuning: a step shorter than 1 mm (the controller settling) does not turn the core.
        private const float TurnSqrMetres = 0.000001f;
        private const float CoincidentSqrMetres = 0.00000001f;
        private const int MaxHoleBits = 31;

        // Not game tuning: which hole is which of the giant's moves while it stands (plan section I). Holes count from 0 as
        // H J K L: H roars, J punches, K kicks and L slams.
        private const int RoarHole = 0;
        private const int PunchHole = 1;
        private const int KickHole = 2;
        private const int SlamHole = 3;

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
        private readonly int _domeSong;

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
        private bool _isLeaping;
        private Vector3 _leapStart;
        private Vector3 _leapTarget;
        private float _leapSeconds;

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
            _domeSong = FindDomeSong(rules.Songs);
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
            Forms.SongLanded += OnFormsSongLanded;
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
        /// cast in the match, nothing in a farm phase unless it is a tower, and its price otherwise. The Sting's price is for
        /// each sting the living wasps would send now (plan section O), so its chip changes with the count.
        /// </summary>
        public int SongPrice(int song)
        {
            return Caster.Price(song, AreSongsFree, Swarm.Count);
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
        /// Raised with the song's index on the tick a song is cast, the tick its last note lands and before any wasp has moved
        /// (plan section O), so the squad can flash, a cast sound play and the camera kick at once. Not raised for a refused cast.
        /// </summary>
        public event Action<int> SongCastNow;

        /// <summary>
        /// Raised once for every hit a song or the giant lands on a beekeeper, and every beekeeper's blow a shield blocks, on the
        /// core or on a hive (plan section O), for the feel of each hit. See <see cref="SongHit"/>.
        /// </summary>
        public event Action<SongHit> SongLanded;

        /// <summary>
        /// True while a standing hive is under attack and the player knows the Dome (plan section O), so the HUD can say in
        /// one short line that the Dome shields what is inside it, hives included. False when the songs have no Dome.
        /// </summary>
        public bool ShowsDomeHint => _domeSong >= 0 && Discovery.IsKnown(_domeSong) && Hives.HasHiveUnderAttack;

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

        /// <summary>
        /// Seconds the giant has left to stand as of the last tick, for the HUD's ring (plan section I); 0 when it does not
        /// stand, and 0 while it finishes a leap after its time ran out.
        /// </summary>
        public float ColossusSecondsLeft => Forms.ColossusSecondsLeft;

        /// <summary>
        /// Seconds the giant stands in all as of the last tick: its song's seconds and the time its kills have added, never
        /// past its most. 0 when it does not stand.
        /// </summary>
        public float ColossusSecondsTotal => Forms.ColossusSecondsTotal;

        /// <summary>
        /// True while the giant is in the air on a leap slam, from take-off until it lands. It cannot be steered or attack
        /// then, and its time running out waits for the landing.
        /// </summary>
        public bool IsColossusLeaping => _isLeaping;

        /// <summary>
        /// Where the giant's last leap slam lands, on the ground: the aim as it took off, pulled in to the leap's range. Kept
        /// after the landing, for the ring the View draws there.
        /// </summary>
        public Vector3 ColossusLeapTarget => _leapTarget;

        /// <summary>How much of the giant's slam cooldown is left, for the HUD: 1 just used, 0 ready. 0 when a giant forms.</summary>
        public float SlamCooldown01 => _chain.SlamCooldown01;

        /// <summary>How much of the giant's roar cooldown is left, for the HUD: 1 just used, 0 ready. 0 when a giant forms.</summary>
        public float RoarCooldown01 => _chain.RoarCooldown01;

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

            SongCastNow?.Invoke(song);
        }

        private void OnFormsSongLanded(SongHit hit)
        {
            SongLanded?.Invoke(hit);
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

        // The Dome's index, for the hint that names its use, or -1 when the songs have none.
        private static int FindDomeSong(IReadOnlyList<SwarmSongEffect> songs)
        {
            for (int index = 0; index < songs.Count; index++)
            {
                if (songs[index] is SlotSongEffect song && song.Form == SwarmForm.Dome)
                {
                    return index;
                }
            }

            return -1;
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

        /// <summary>
        /// Chooses the Sting's targets, one a sting, as it is cast or as a sting's target dies: first the beekeeper the Threat
        /// rule picks now, as for any cast, so it agrees with the marker and the mouse, then the rest of the living beekeepers
        /// within reach, most threatening first (<see cref="AutoAim.FillThreats"/>), as many as asked for and auto-aim lists
        /// (<see cref="AimConfig.ThreatCount"/>). Its single pick becomes <see cref="AimTarget"/>, as for any cast.
        /// </summary>
        int ISongAim.ChooseThreats(float range, int count, ISwarmTarget[] targets, out Vector3 point)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            Choose(AimRule.Threat, range);
            point = _aimPoint;
            int most = Mathf.Min(count, targets.Length);
            int written = 0;
            ISwarmTarget first = AimTarget;
            if (first != null && written < most)
            {
                targets[written] = first;
                written++;
            }

            _autoAim.FillThreats(_corePosition, range);
            for (int slot = 0; slot < _autoAim.ThreatCount && written < most; slot++)
            {
                ISwarmTarget next = _enemies[_autoAim.GetThreat(slot)];
                if (!ReferenceEquals(next, first))
                {
                    targets[written] = next;
                    written++;
                }
            }

            return written;
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

                // While the giant stands the holes are its moves (plan section I) and no note reaches the caster, so the
                // presses cannot leak into the next song's window.
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

        // Plan section I: while the giant stands the holes are its moves, and the move steers it relative to the camera. In
        // the air on a leap it flies to the landing and can neither be steered nor attack. The tick it forms on, the holes
        // are the song's last note, not a move.
        private void TickColossus(in HiveDefenseFrame frame, float dt)
        {
            bool formIsColossus = Forms.Current == SwarmForm.Colossus;
            bool isFormingNow = formIsColossus && !_isColossusActive;
            if (isFormingNow)
            {
                StandUpColossus();
            }
            else if (!formIsColossus && _isColossusActive)
            {
                EndColossus();
            }

            if (_isColossusActive && _isLeaping)
            {
                FlyLeap(dt);
            }

            // The chain lands what is under way, the leap included, before this tick's steering and presses.
            _chain.Tick(dt);

            if (!_isColossusActive)
            {
                return;
            }

            if (!_isLeaping)
            {
                SteerColossus(frame.Move, frame.CameraForward, dt);
            }

            // The core rides inside the giant, so the beekeepers chase and hit the giant, and the core is left where the giant
            // stood when it ends.
            _corePosition = new Vector3(_colossusPosition.x, _corePosition.y, _colossusPosition.z);

            if (!_isLeaping)
            {
                PressColossusMoves(in frame, isFormingNow ? 0 : frame.PressedHoleMask);
            }
        }

        // A new giant starts afresh: where the core is, facing forward, at the level the swarm has now, and with every move
        // ready.
        private void StandUpColossus()
        {
            _isColossusActive = true;
            _colossusPosition = new Vector3(_corePosition.x, 0f, _corePosition.z);
            _colossusFacing = Vector3.forward;
            _colossusSizeScale = SizeScale;
            _colossusDamageScale = DamageScale;
            _isLeaping = false;
            _leapTarget = _colossusPosition;
            _chain.Reset();
        }

        // The giant ends from its time or a collapse, in the air too. Nothing of it lingers: a move under way never lands
        // and a waiting phrase never starts.
        private void EndColossus()
        {
            _isColossusActive = false;
            _isCoreSettling = true;
            _isLeaping = false;
            _chain.Reset();
        }

        // W walks along the camera's way on the ground and D to its right; with no camera way the move is the world's x and z.
        // The giant walks through hives and scenery, but never past the play area, where the ground ends.
        private void SteerColossus(Vector2 move, Vector3 cameraForward, float dt)
        {
            Vector3 step = HiveDefenseFrame.GroundStep(move, cameraForward);
            if (!(step.sqrMagnitude > 0f))
            {
                return;
            }

            _colossusPosition = KeepInPlayArea(_colossusPosition + (step * (_rules.Colossus.Speed * dt)));
            _colossusFacing = step.normalized;
            _coreFacing = _colossusFacing;
        }

        // Plan section I: J or Space punches, K or Left Shift kicks, L slams and H roars. A hole and its button pressed on one
        // tick are one press, so they cannot make a phrase by themselves.
        private void PressColossusMoves(in HiveDefenseFrame frame, int holes)
        {
            if (frame.Punch || IsPressed(holes, PunchHole))
            {
                _chain.TryPunch();
            }

            if (frame.Kick || IsPressed(holes, KickHole))
            {
                _chain.TryKick();
            }

            if (IsPressed(holes, SlamHole))
            {
                _chain.TrySlam();
            }

            if (IsPressed(holes, RoarHole))
            {
                _chain.TryRoar();
            }
        }

        private static bool IsPressed(int holes, int hole)
        {
            return (holes & HiveDefenseFrame.HoleBit(hole)) != 0;
        }

        // The leap slam takes off toward this tick's aim (the mouse's ground point while it is live, as for an aimed song),
        // pulled in to the leap's range grown by the giant's level, and on the ground inside the play area. It faces the way
        // it leaps, and its time cannot end it until it lands.
        private void TakeOff()
        {
            float range = _rules.Colossus.LeapRange * _colossusSizeScale;
            var aim = new Vector3(_aimPoint.x, 0f, _aimPoint.z);
            _leapStart = _colossusPosition;
            _leapTarget = KeepInPlayArea(GroundMath.PullIn(aim, _colossusPosition, range));
            _leapSeconds = 0f;
            _isLeaping = true;
            Forms.SetColossusAirborne(true);
            if (GroundMath.DistanceSquared(_leapTarget, _leapStart) > TurnSqrMetres)
            {
                _colossusFacing = GroundMath.Direction(_leapStart, _leapTarget);
                _coreFacing = _colossusFacing;
            }
        }

        // Straight along the ground; the View adds the arc in the air.
        private void FlyLeap(float dt)
        {
            _leapSeconds += dt;
            float seconds = _rules.Colossus.LeapSeconds;
            float flown = seconds > 0f ? Mathf.Clamp01(_leapSeconds / seconds) : 1f;
            _colossusPosition = Vector3.Lerp(_leapStart, _leapTarget, flown);
        }

        private void TouchDown()
        {
            _isLeaping = false;
            _colossusPosition = _leapTarget;
            Forms.SetColossusAirborne(false);
        }

        private void TickBeekeepers(float dt)
        {
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot])
                {
                    continue;
                }

                // Read for each beekeeper, so a hit that collapses the swarm blinds the ones that think after it at once.
                var senses = new BeekeeperSenses(_corePosition, Hives, SeparationFor(slot), !Swarm.IsCollapsed);
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
            Hive hive = Hives[hiveIndex];
            float harm = brain.Config.HiveDamage;
            float fraction = Forms.HiveDamageFraction(hiveIndex);
            SwarmForm shield = Forms.HiveShieldForm(hiveIndex);
            hive.Damage(harm * fraction);
            ReportBlocked(hive.Position, harm, fraction, shield);
            Forms.NotifyHiveHit(hiveIndex, brain);
        }

        // The shield is read before the loss, which may collapse the swarm and end every song.
        private void OnCoreHit(BeekeeperBrain brain)
        {
            int harm = brain.Config.WaspsKnockedOff;
            float fraction = Forms.WaspLossFraction;
            SwarmForm shield = Forms.CoreShieldForm;
            Vector3 core = _corePosition;
            Swarm.Lose(harm, fraction);
            ReportBlocked(core, harm, fraction, shield);
            Forms.NotifyHit(brain.Position);
            Nests.NotifyCoreHit();
            _lastHitPoint = _corePosition;
        }

        // A blow a running song cut is a blocked hit (plan section O), so the shield flashes where it guards. It carries the
        // harm the shield stopped. Smoke's slow harm is not a blow, so it is never reported.
        private void ReportBlocked(Vector3 guarded, float harm, float fraction, SwarmForm shield)
        {
            if (!(harm > 0f) || !(fraction < 1f))
            {
                return;
            }

            SongLanded?.Invoke(new SongHit(guarded, harm * (1f - fraction), false, true, shield, false));
        }

        private void OnGrenadeThrown(BeekeeperBrain brain, Vector3 landing)
        {
            Smoke.Throw(brain.Position, landing);
        }

        private void OnColossusStarted(ColossusAttack attack)
        {
            if (attack == ColossusAttack.LeapSlam && _isColossusActive)
            {
                TakeOff();
            }

            ColossusAttackStarted?.Invoke(attack);
        }

        // The leap slam lands where it was aimed before it hits around that spot.
        private void OnColossusLanded(ColossusAttack attack)
        {
            if (attack == ColossusAttack.LeapSlam && _isLeaping)
            {
                TouchDown();
            }

            if (_isColossusActive)
            {
                ApplyColossusHit(attack);
            }

            ColossusAttackLanded?.Invoke(attack);
        }

        // Plan section I. The blows ahead land their reach in front of where the giant faces; the slam, the spin kick and the
        // leap's landing hit all around a point. The slam, the leap and the third punch are the heavy hits. The roar only
        // stuns, so it has no hit to report.
        private void ApplyColossusHit(ColossusAttack attack)
        {
            ColossusConfig config = _rules.Colossus;
            switch (attack)
            {
                case ColossusAttack.Punch:
                    HitAhead(config.PunchDamage, config.PunchRadius, config.PunchReach, 0f, false);
                    break;
                case ColossusAttack.HeavyPunch:
                    HitAhead(config.HeavyPunchDamage, config.PunchRadius, config.PunchReach, config.HeavyPunchKnockback, true);
                    break;
                case ColossusAttack.Kick:
                    HitAhead(config.KickDamage, config.KickRadius, config.KickReach, 0f, false);
                    break;
                case ColossusAttack.SpinKick:
                    HitAround(_colossusPosition, config.SpinKickDamage, config.SpinKickRadius, 0f, false);
                    break;
                case ColossusAttack.Slam:
                    HitAround(_colossusPosition, config.SlamDamage, config.SlamRadius, config.SlamKnockback, true);
                    break;
                case ColossusAttack.LeapSlam:
                    HitAround(_colossusPosition, config.LeapDamage, config.LeapRadius, config.LeapKnockback, true);
                    break;
                case ColossusAttack.Roar:
                    Roar(config.RoarRadius * _colossusSizeScale, config.RoarStunSeconds);
                    break;
            }
        }

        // The giant's reach grows with the level it stood up at, as its radius does.
        private void HitAhead(float damage, float radius, float reach, float knockback, bool isHeavy)
        {
            Vector3 center = _colossusPosition + (_colossusFacing * (reach * _colossusSizeScale));
            Hit(center, damage, radius, knockback, false, isHeavy);
        }

        private void HitAround(Vector3 center, float damage, float radius, float knockback, bool isHeavy)
        {
            Hit(center, damage, radius, knockback, true, isHeavy);
        }

        // Every living beekeeper on the ground within the radius of the centre takes the damage, as hard and as far as the
        // level the giant stood up at, and each hit is reported once (plan section O). One that lives is stunned and, with a
        // knockback, thrown: away from the centre for a hit all around, along the giant's facing for a blow ahead. Only
        // FuryConfig.ColossusDamageShare of the damage fills Fury (none by default, so a giant cannot fill the meter again by
        // itself), and each kill adds to the giant's time.
        private void Hit(Vector3 center, float damage, float radius, float knockback, bool isAround, bool isHeavy)
        {
            damage *= _colossusDamageScale;
            radius *= _colossusSizeScale;
            float radiusSqr = radius * radius;
            float stunSeconds = _rules.Colossus.StunSeconds;
            float dealt = 0f;
            int kills = 0;
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot] || !_pool[slot].IsAlive)
                {
                    continue;
                }

                BeekeeperBrain brain = _pool[slot];
                if (GroundMath.DistanceSquared(brain.Position, center) > radiusSqr)
                {
                    continue;
                }

                dealt += Mathf.Min(damage, brain.Health);
                brain.TakeDamage(damage);
                bool isKill = !brain.IsAlive;
                SongLanded?.Invoke(new SongHit(brain.Position, damage, isKill, false, SwarmForm.Colossus, isHeavy));
                if (isKill)
                {
                    kills++;
                    continue;
                }

                brain.Stun(stunSeconds);
                if (knockback > 0f)
                {
                    Vector3 away = isAround ? GroundMath.Direction(center, brain.Position) : _colossusFacing;
                    brain.Knockback(away * knockback);
                }
            }

            float forFury = dealt * _rules.Fury.ColossusDamageShare;
            if (forFury > 0f)
            {
                Forms.AddColossusDamage(forFury);
            }

            if (kills > 0)
            {
                Forms.AddColossusKills(kills);
            }
        }

        // The roar stuns every living beekeeper within its radius of the giant, and hurts none.
        private void Roar(float radius, float stunSeconds)
        {
            float radiusSqr = radius * radius;
            for (int slot = 0; slot < _pool.Length; slot++)
            {
                if (!_isActive[slot] || !_pool[slot].IsAlive)
                {
                    continue;
                }

                if (GroundMath.DistanceSquared(_pool[slot].Position, _colossusPosition) <= radiusSqr)
                {
                    _pool[slot].Stun(stunSeconds);
                }
            }
        }
    }
}
