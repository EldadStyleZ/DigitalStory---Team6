using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plays a whole match through the rules, as a player at the keyboard would: each tick it asks a plan what to do, moves
    /// the core toward where the plan wants it at the core's speed, presses the holes of a song's real keys one by one, holds
    /// Give, calls the wave and swings the giant, then ticks the session at a fixed 1/30 s and measures: where the wasps came
    /// from and went (the towers among the songs), and which song dealt each point of damage. It knows every song, so
    /// finding songs is not what it measures. It plays from the keyboard only, so it places a tower as auto-aim would: it
    /// faces the spot and plays the song that far from it, and it reads which hive a Honeycomb would shell. Like a person it
    /// is not instant, and each seed is a different person: how long it takes to react after a song, how fast it plays its
    /// notes, how close it dares stand to a beekeeper and which way it turns when it runs from one are drawn from the seed.
    /// </summary>
    internal sealed class PlaytestBot
    {
        /// <summary>Seconds of game time each tick advances.</summary>
        public const float Step = 1f / 30f;

        // The player each seed draws, not game tuning: the moment it takes to react after a song (a quick player's, and
        // how much slower a slow one is at most), the gap between two notes, and how far from a beekeeper it stands.
        private const float QuickestReactionSeconds = 0.2f;
        private const float SlowestReactionSeconds = 0.5f;
        private const float LongestHesitationSeconds = 0.7f;
        private const float FastestNoteSeconds = 0.09f;
        private const float SlowestNoteSeconds = 0.18f;
        private const float BoldestStandOffMetres = 6f;
        private const float WariestStandOffMetres = 9f;

        // Running from a stick: how far ahead it aims, how much it bends sideways so it circles instead of running into the
        // edge, and how far inside the play area's edge it turns back toward the middle.
        private const float EvadeAheadMetres = 4f;
        private const float EvadeSideways = 0.6f;
        private const float EdgeMarginMetres = 5f;

        // The giant turns to face a beekeeper by stepping toward it this far, and faces it once the cosine is above this.
        private const float TurnStepMetres = 0.3f;
        private const float FacingCosine = 0.9f;

        // Within this many metres of where it flies, the core counts as there and stands.
        private const float ArriveMetres = 0.25f;

        // Mixes the match's seed into a different stream for the bot's own timing.
        private const uint BotSeedSalt = 0x9E3779B9u;

        // More notes than the longest song has.
        private const int LongestSong = 8;

        // A tower is lobbed this far ahead of where the core faces (auto-aim's empty aim) once the core is within this much
        // of that distance from the spot and faces it this well.
        private const float PlaceToleranceMetres = 0.5f;
        private const float PlaceFacingCosine = 0.97f;

        // Not game tuning: no hive has been hit yet. Minus infinity, so a hive never hit is never one hit recently.
        private const float NeverHit = float.NegativeInfinity;

        private readonly PlaytestMatch _match;
        private readonly HiveDefenseSession _session;
        private readonly IRandomSource _random;
        private readonly float _reactionSeconds;
        private readonly float _noteSeconds;
        private readonly float _standOffMetres;
        private readonly float _evadeSide;
        private readonly int[] _typing = new int[LongestSong];
        private readonly float[] _hiveHealth;
        private readonly int[] _pricesBeforePress;
        private readonly float[] _hiveHitAt;
        private readonly PlaytestRun _run = new PlaytestRun();

        // Damage by song: every beekeeper the match has spawned and its health as last measured. A song's strike is measured
        // as it lands, the giant's as its blow lands, a shell's thorn as the stick's hit lands, and whatever is left at the end
        // of a tick belongs to the song in the Strike slot (the Sting hurts as it holds on, with nothing that lands).
        private readonly List<BeekeeperBrain> _brains = new List<BeekeeperBrain>();
        private readonly List<float> _brainHealth = new List<float>();
        private readonly int[] _songOfForm;
        private readonly int _colossusSong = -1;

        // Ticks played. Time is counted in whole ticks, so adding up floats cannot end a match a tick early on one runtime.
        private int _ticks;
        private Vector3 _core;
        private Vector3 _destination;
        private bool _hasDestination;
        private bool _give;
        private bool _callWave;
        private bool _punch;
        private bool _kick;
        private int _typingCount;
        private int _typingNext;
        private float _nextNoteAt;
        private float _nextSongAt;
        private int _lastCount;
        private int _knownCapacity;
        private float _healedHealth;
        private int _lost;
        private float _quietSeconds;
        private int _spawned;

        /// <summary>Gets the bot ready to play <paramref name="match"/>, timing itself from <paramref name="seed"/>.</summary>
        public PlaytestBot(PlaytestMatch match, uint seed)
        {
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _session = match.Session;
            _random = new Mulberry32Random(seed ^ BotSeedSalt);
            _reactionSeconds = Mathf.Lerp(QuickestReactionSeconds, SlowestReactionSeconds, (float)_random.NextDouble());
            _noteSeconds = Mathf.Lerp(FastestNoteSeconds, SlowestNoteSeconds, (float)_random.NextDouble());
            _standOffMetres = Mathf.Lerp(BoldestStandOffMetres, WariestStandOffMetres, (float)_random.NextDouble());
            _evadeSide = _random.NextDouble() < 0.5 ? -1f : 1f;
            _core = _session.CorePosition;
            _hiveHealth = new float[_session.Hives.Count];
            _pricesBeforePress = new int[_session.Caster.Count];
            _hiveHitAt = new float[_session.Hives.Count];
            for (int hive = 0; hive < _hiveHitAt.Length; hive++)
            {
                _hiveHealth[hive] = _session.Hives[hive].Health;
                _hiveHitAt[hive] = NeverHit;
            }

            _run.Seed = seed;
            _lastCount = _session.Swarm.Count;
            _knownCapacity = _session.Swarm.Capacity;

            int songs = _session.Caster.Count;
            _run.SongNames = new string[songs];
            _run.DamageBySong = new float[songs];
            _run.CastsBySong = new int[songs];
            _songOfForm = new int[Enum.GetValues(typeof(SwarmForm)).Length];
            for (int form = 0; form < _songOfForm.Length; form++)
            {
                _songOfForm[form] = -1;
            }

            for (int song = 0; song < songs; song++)
            {
                SwarmSongEffect effect = _session.Caster[song];
                _run.SongNames[song] = effect.Song.Title;
                if (effect is SlotSongEffect slotSong)
                {
                    _songOfForm[(int)slotSong.Form] = song;
                }
                else if (effect is ColossusSong)
                {
                    _colossusSong = song;
                }
            }

            _session.Swarm.CountChanged += OnCountChanged;
            _session.Swarm.Collapsed += OnCollapsed;
            _session.Caster.SongCast += OnSongCast;
            _session.WaspsGranted += OnWaspsGranted;
            _session.BeekeeperSpawned += OnBeekeeperSpawned;
            _session.Forms.Landed += OnSongLanded;
            _session.ColossusAttackLanded += OnColossusLanded;
        }

        /// <summary>The match being played.</summary>
        public HiveDefenseSession Session => _session;

        /// <summary>The match's configs and songs.</summary>
        public PlaytestMatch Match => _match;

        /// <summary>Seconds of game time played.</summary>
        public float Time => _ticks * Step;

        /// <summary>Where the core is.</summary>
        public Vector3 Core => _core;

        /// <summary>True while the bot is still pressing a song's notes.</summary>
        public bool IsTyping => _typingNext < _typingCount;

        /// <summary>True in a survive phase.</summary>
        public bool IsSurvive => _session.Phase == MatchPhase.Survive;

        /// <summary>How far from the beekeeper it attacks this player stands: inside the Sting's reach, near a stick's notice.</summary>
        public float StandOffMetres => _standOffMetres;

        /// <summary>
        /// Plays the match with <paramref name="plan"/> until it is won or lost, or <paramref name="maxSeconds"/> of game time
        /// have passed, and returns what it measured.
        /// </summary>
        public PlaytestRun Play(IPlaytestPlan plan, float maxSeconds)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            _run.Plan = plan.Name;
            while (_session.State == HiveDefenseState.Playing && Time < maxSeconds)
            {
                _hasDestination = false;
                _give = false;
                _callWave = false;
                _punch = false;
                _kick = false;
                plan.Decide(this);
                TickOnce();
            }

            return Finish();
        }

        // ---- Actions: what the plan asks for this tick ----

        /// <summary>Flies the core toward <paramref name="point"/> this tick, at the core's speed, and stands once there.</summary>
        public void FlyTo(Vector3 point)
        {
            _destination = new Vector3(point.x, _core.y, point.z);
            _hasDestination = true;
        }

        /// <summary>Flies toward <paramref name="point"/> until within <paramref name="metres"/> of it, then stands.</summary>
        public void Approach(Vector3 point, float metres)
        {
            Vector3 offset = Flat(point - _core);
            float distance = offset.magnitude;
            if (distance > metres)
            {
                FlyTo(_core + (offset * ((distance - metres) / distance)));
            }
        }

        /// <summary>Holds Give this tick: beside a hurt hive the swarm pays wasps into its health.</summary>
        public void HoldGive()
        {
            _give = true;
        }

        /// <summary>Calls the wave this tick, as pressing Enter does. Only once the bot has reacted to its last song.</summary>
        public void CallWave()
        {
            if (Time >= _nextSongAt && !IsTyping)
            {
                _callWave = true;
            }
        }

        /// <summary>
        /// Starts pressing the song's keys, one note at the bot's pace, when it can be cast: the bot is not pressing another,
        /// has reacted to its last one, the song has cooled down, and the swarm can pay its price in a survive phase.
        /// Returns true when it started.
        /// </summary>
        public bool TryPlay(int song)
        {
            if (IsTyping || Time < _nextSongAt || _session.IsColossusActive || _session.Swarm.IsCollapsed
                || !IsReady(song) || !CanAfford(song))
            {
                return false;
            }

            IReadOnlyList<int> keys = _session.Caster[song].Song.Keys;
            for (int index = 0; index < keys.Count; index++)
            {
                _typing[index] = keys[index];
            }

            _typingCount = keys.Count;
            _typingNext = 0;
            _nextNoteAt = Time;
            return true;
        }

        /// <summary>
        /// Lobs an aimed song onto <paramref name="spot"/> with no beekeeper in its reach, as a player at the keyboard does:
        /// auto-aim then strikes the ground a fixed distance ahead of where the core faces, so the bot flies toward the spot,
        /// stands once that far from it and facing it, and plays the song. Flies back first when it is too close. Returns
        /// true once it started the song's notes; until then it has only moved.
        /// </summary>
        public bool TryPlayAhead(int song, Vector3 spot)
        {
            if (IsTyping)
            {
                return false;
            }

            float ahead = _match.Aim.EmptyAimDistance;
            Vector3 offset = Flat(spot - _core);
            float distance = offset.magnitude;
            if (distance > ahead + PlaceToleranceMetres)
            {
                FlyTo(spot);
                return false;
            }

            Vector3 toward = distance > 0f ? offset / distance : _session.CoreFacing;
            if (distance < ahead - PlaceToleranceMetres)
            {
                FlyTo(spot - (toward * (ahead + 1f)));
                return false;
            }

            if (Vector3.Dot(toward, _session.CoreFacing) < PlaceFacingCosine)
            {
                // Turns by stepping toward the spot, as the giant does: the core faces the way it last moved.
                FlyTo(_core + (toward * TurnStepMetres));
                return false;
            }

            return TryPlay(song);
        }

        /// <summary>
        /// Swings the giant at the nearest beekeeper: walks it there and, once the beekeeper is where its fists land, punches
        /// and kicks. Does nothing while the giant does not stand.
        /// </summary>
        public void FightAsGiant()
        {
            if (!_session.IsColossusActive)
            {
                return;
            }

            BeekeeperBrain target = NearestBeekeeper(_core);
            if (target == null)
            {
                return;
            }

            // A fist lands PunchReach ahead of where the giant faces and hits within PunchRadius of that, so the beekeeper
            // must stand in front of it, neither too far nor too close, and the giant only turns by walking.
            float scale = _session.ColossusSizeScale;
            float reach = _match.Colossus.PunchReach * scale;
            float radius = _match.Colossus.PunchRadius * scale;
            Vector3 offset = Flat(target.Position - _core);
            float distance = offset.magnitude;
            Vector3 toward = distance > 0f ? offset / distance : _session.ColossusFacing;
            if (distance > reach + (radius * 0.5f))
            {
                FlyTo(target.Position);
                return;
            }

            if (distance < reach - radius)
            {
                FlyTo(_core - (toward * reach));
                return;
            }

            if (Vector3.Dot(toward, _session.ColossusFacing) < FacingCosine)
            {
                FlyTo(_core + (toward * TurnStepMetres));
                return;
            }

            _punch = true;
            _kick = true;
        }

        /// <summary>
        /// Steps off an open nest the core has started to collect, for a plan that never collects nests. False, doing
        /// nothing, when the core is collecting none.
        /// </summary>
        public bool StepOffNest()
        {
            for (int index = 0; index < _session.Nests.Count; index++)
            {
                Nest nest = _session.Nests[index];
                Vector3 offset = Flat(_core - nest.Position);
                if (!nest.IsOpen || nest.IsCollected || nest.Progress01 <= 0f || offset.sqrMagnitude <= 0f)
                {
                    continue;
                }

                FlyTo(nest.Position + (offset.normalized * (_match.Nests.CollectRadius + 1f)));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Runs from the nearest stick that is on the core within <paramref name="metres"/>, bending sideways so it circles
        /// rather than running into the edge, and turning toward the middle near the edge. False, doing nothing, when no stick
        /// is that close.
        /// </summary>
        public bool Evade(float metres)
        {
            BeekeeperBrain stick = NearestStickOnCore(metres);
            if (stick == null)
            {
                return false;
            }

            Vector3 away = Flat(_core - stick.Position);
            away = away.sqrMagnitude > 0f ? away.normalized : Vector3.forward;
            var sideways = new Vector3(-away.z, 0f, away.x);
            Vector3 direction = (away + (sideways * (EvadeSideways * _evadeSide))).normalized;
            Vector3 ahead = _core + (direction * EvadeAheadMetres);
            float inside = _match.CoreMove.PlayAreaHalfSize - EdgeMarginMetres;
            if (Mathf.Abs(ahead.x) > inside || Mathf.Abs(ahead.z) > inside)
            {
                direction = (direction + Flat(-_core).normalized).normalized;
                ahead = _core + (direction * EvadeAheadMetres);
            }

            FlyTo(ahead);
            return true;
        }

        // ---- What the plan can see ----

        /// <summary>True when the song has cooled down.</summary>
        public bool IsReady(int song)
        {
            return _session.Caster.CooldownFraction(song) <= 0f;
        }

        /// <summary>
        /// True when the swarm can pay what the song costs now (<see cref="HiveDefenseSession.SongPrice"/>): nothing for its
        /// first cast, nothing in a farm phase unless it is a tower, and its price otherwise.
        /// </summary>
        public bool CanAfford(int song)
        {
            return _session.Swarm.Spendable >= _session.SongPrice(song);
        }

        /// <summary>Wasps the song costs when it is paid for: its list price, which a plan budgets with.</summary>
        public int CostOf(int song)
        {
            return _session.Caster[song].WaspCost;
        }

        /// <summary>True while the slot's song runs (not its return beat).</summary>
        public bool IsRunning(SongSlot slot)
        {
            SlotState state = _session.Forms.Slot(slot);
            return state.IsActive && state.ReleasedSeconds < 0f;
        }

        /// <summary>The living beekeeper nearest <paramref name="point"/> on the ground, or null when none lives.</summary>
        public BeekeeperBrain NearestBeekeeper(Vector3 point)
        {
            BeekeeperBrain nearest = null;
            float best = float.MaxValue;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                float distance = Flat(brain.Position - point).sqrMagnitude;
                if (brain.IsAlive && distance < best)
                {
                    best = distance;
                    nearest = brain;
                }
            }

            return nearest;
        }

        /// <summary>How many living beekeepers stand within <paramref name="metres"/> of <paramref name="point"/> on the ground.</summary>
        public int CountWithin(Vector3 point, float metres)
        {
            int count = 0;
            float squared = metres * metres;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                if (brain.IsAlive && Flat(brain.Position - point).sqrMagnitude <= squared)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// How many living beekeepers stand within <paramref name="metres"/> of the core in the half-disc facing
        /// <paramref name="toward"/>, the line across it included: what the Scythe would cut.
        /// </summary>
        public int CountInFront(Vector3 toward, float metres)
        {
            Vector3 facing = Flat(toward - _core);
            facing = facing.sqrMagnitude > 0f ? facing.normalized : _session.CoreFacing;
            int count = 0;
            float squared = metres * metres;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                Vector3 offset = Flat(brain.Position - _core);
                if (brain.IsAlive && offset.sqrMagnitude <= squared && Vector3.Dot(offset, facing) >= 0f)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// The beekeeper the Threat aim rule would choose within <paramref name="metres"/> of the core, or null: the nearest,
        /// counting one that stands near a standing hive as <see cref="AimConfig.HiveThreatBonus"/> nearer.
        /// </summary>
        public BeekeeperBrain MostThreatening(float metres)
        {
            BeekeeperBrain best = null;
            float bestScore = float.MaxValue;
            float squared = metres * metres;
            float hiveSquared = _match.Aim.HiveThreatRadius * _match.Aim.HiveThreatRadius;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                float distanceSquared = Flat(brain.Position - _core).sqrMagnitude;
                if (!brain.IsAlive || distanceSquared > squared)
                {
                    continue;
                }

                float score = Mathf.Sqrt(distanceSquared);
                for (int hive = 0; hive < _session.Hives.Count; hive++)
                {
                    Hive candidate = _session.Hives[hive];
                    if (!candidate.IsDestroyed && Flat(brain.Position - candidate.Position).sqrMagnitude <= hiveSquared)
                    {
                        score -= _match.Aim.HiveThreatBonus;
                        break;
                    }
                }

                if (score < bestScore)
                {
                    best = brain;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>
        /// The most beekeepers one straight line from the core through a beekeeper within <paramref name="length"/> would
        /// pierce, <paramref name="width"/> wide: what the Lance would hit.
        /// </summary>
        public int BestLine(float length, float width)
        {
            int best = 0;
            float squared = length * length;
            float half = width * 0.5f;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain through = _session.ActiveBeekeeper(index);
                Vector3 offset = Flat(through.Position - _core);
                if (!through.IsAlive || offset.sqrMagnitude > squared || offset.sqrMagnitude <= 0f)
                {
                    continue;
                }

                Vector3 direction = offset.normalized;
                int count = 0;
                for (int other = 0; other < _session.ActiveBeekeeperCount; other++)
                {
                    BeekeeperBrain brain = _session.ActiveBeekeeper(other);
                    Vector3 to = Flat(brain.Position - _core);
                    float along = Vector3.Dot(to, direction);
                    float across = Mathf.Abs((to.x * direction.z) - (to.z * direction.x));
                    if (brain.IsAlive && along >= 0f && along <= length && across <= half)
                    {
                        count++;
                    }
                }

                best = Math.Max(best, count);
            }

            return best;
        }

        /// <summary>
        /// True when a living smoker stands further than <paramref name="near"/> from the core but within
        /// <paramref name="far"/>: out of the Sting's reach, in the Lance's.
        /// </summary>
        public bool IsSmokerBetween(float near, float far)
        {
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                float distance = Flat(brain.Position - _core).magnitude;
                if (brain.IsAlive && brain.Config.Kind == BeekeeperKind.Smoker && distance > near && distance <= far)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The hive a Honeycomb cast now would shell, as the Hive aim rule chooses it: the standing hive hit most recently,
        /// within <see cref="AimConfig.HiveRecentSeconds"/>, otherwise the standing hive nearest the core; -1 when none stands.
        /// </summary>
        public int ShellHive()
        {
            int recent = -1;
            for (int hive = 0; hive < _hiveHitAt.Length; hive++)
            {
                bool isRecent = !_session.Hives[hive].IsDestroyed && Time - _hiveHitAt[hive] <= _match.Aim.HiveRecentSeconds;
                if (isRecent && (recent < 0 || _hiveHitAt[hive] > _hiveHitAt[recent]))
                {
                    recent = hive;
                }
            }

            if (recent >= 0)
            {
                return recent;
            }

            return _session.Hives.TryGetNearestStanding(_core, out int nearest) ? nearest : -1;
        }

        /// <summary>The spawn point the next beekeeper comes from: the points take turns, one beekeeper each.</summary>
        public Vector3 NextSpawnPoint => _match.SpawnPoints[_spawned % _match.SpawnPoints.Count];

        /// <summary>
        /// A spot on the lane the next beekeeper walks, <paramref name="metres"/> out from the hive it walks to (the standing
        /// hive nearest its spawn point) toward that spawn point; -1 in <paramref name="hive"/> and the spawn point itself
        /// when no hive stands.
        /// </summary>
        public Vector3 LaneSpot(float metres, out int hive)
        {
            Vector3 spawn = NextSpawnPoint;
            if (!_session.Hives.TryGetNearestStanding(spawn, out hive))
            {
                return spawn;
            }

            Vector3 from = _session.Hives[hive].Position;
            Vector3 lane = Flat(spawn - from);
            return from + (lane.normalized * Mathf.Min(metres, lane.magnitude));
        }

        /// <summary>
        /// The most beekeepers that stand within <paramref name="radius"/> of one living beekeeper, itself included, among
        /// those within <paramref name="reach"/> of the core: how many one blast there could catch.
        /// </summary>
        public int BiggestCrowd(float reach, float radius)
        {
            int best = 0;
            float reachSquared = reach * reach;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                if (brain.IsAlive && Flat(brain.Position - _core).sqrMagnitude <= reachSquared)
                {
                    best = Math.Max(best, CountWithin(brain.Position, radius));
                }
            }

            return best;
        }

        /// <summary>True while a stick within <paramref name="metres"/> chases or swings at the core: the moment for the Dome.</summary>
        public bool IsCoreUnderAttack(float metres)
        {
            return NearestStickOnCore(metres) != null;
        }

        /// <summary>The nearest living stick within <paramref name="metres"/> that chases or swings at the core, or null.</summary>
        public BeekeeperBrain NearestStickOnCore(float metres)
        {
            BeekeeperBrain nearest = null;
            float best = metres * metres;
            for (int index = 0; index < _session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = _session.ActiveBeekeeper(index);
                bool isOnCore = brain.State == BeekeeperState.ChaseCore
                    || ((brain.State == BeekeeperState.WindUp || brain.State == BeekeeperState.Recover) && !brain.IsTargetingHive);
                float distance = Flat(brain.Position - _core).sqrMagnitude;
                if (brain.IsAlive && brain.Config.Kind == BeekeeperKind.Stick && isOnCore && distance <= best)
                {
                    best = distance;
                    nearest = brain;
                }
            }

            return nearest;
        }

        /// <summary>
        /// The standing hive with the most living beekeepers within <paramref name="metres"/> of it, the more hurt one on a
        /// tie, or -1 when none has any.
        /// </summary>
        public int MostThreatenedHive(float metres)
        {
            int best = -1;
            int bestCount = 0;
            for (int hive = 0; hive < _session.Hives.Count; hive++)
            {
                Hive candidate = _session.Hives[hive];
                if (candidate.IsDestroyed)
                {
                    continue;
                }

                int count = CountWithin(candidate.Position, metres);
                bool isBetter = count > bestCount
                    || (count == bestCount && count > 0 && candidate.Health < _session.Hives[best].Health);
                if (isBetter)
                {
                    best = hive;
                    bestCount = count;
                }
            }

            return best;
        }

        /// <summary>The standing hive with the least health as a share of its full health, below <paramref name="share"/>, or -1.</summary>
        public int MostHurtHive(float share)
        {
            int best = -1;
            float bestShare = share;
            for (int hive = 0; hive < _session.Hives.Count; hive++)
            {
                Hive candidate = _session.Hives[hive];
                float health = candidate.Health / candidate.MaxHealth;
                if (!candidate.IsDestroyed && health < bestShare)
                {
                    best = hive;
                    bestShare = health;
                }
            }

            return best;
        }

        /// <summary>The standing hive with the most wasps in its store, or -1 when none stands.</summary>
        public int RichestHive()
        {
            int best = -1;
            float bestStore = -1f;
            for (int hive = 0; hive < _session.Hives.Count; hive++)
            {
                Hive candidate = _session.Hives[hive];
                if (!candidate.IsDestroyed && candidate.Store > bestStore)
                {
                    best = hive;
                    bestStore = candidate.Store;
                }
            }

            return best;
        }

        /// <summary>Wasps in every standing hive's store together.</summary>
        public float StoredWasps()
        {
            float stored = 0f;
            for (int hive = 0; hive < _session.Hives.Count; hive++)
            {
                stored += _session.Hives[hive].Store;
            }

            return stored;
        }

        /// <summary>The open nest nearest the core, or -1 when none is open.</summary>
        public int NearestOpenNest()
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int index = 0; index < _session.Nests.Count; index++)
            {
                Nest nest = _session.Nests[index];
                float distance = Flat(nest.Position - _core).sqrMagnitude;
                if (nest.IsOpen && !nest.IsCollected && distance < bestDistance)
                {
                    best = index;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Where to stand to guard hive <paramref name="hive"/> and take its store: just inside its collect radius, on the
        /// side toward the middle of the map.
        /// </summary>
        public Vector3 BesideHive(int hive)
        {
            Vector3 position = _session.Hives[hive].Position;
            Vector3 toMiddle = Flat(-position);
            float inside = _match.Hive.CollectRadius * 0.6f;
            return toMiddle.sqrMagnitude > 0f ? position + (toMiddle.normalized * inside) : position + (Vector3.forward * inside);
        }

        // ---- The tick ----

        private void TickOnce()
        {
            int holes = 0;
            if (IsTyping && Time >= _nextNoteAt)
            {
                holes = HiveDefenseFrame.HoleBit(_typing[_typingNext]);
                _typingNext++;
                _nextNoteAt = Time + _noteSeconds;
                if (!IsTyping)
                {
                    // Mostly quick, now and then slow: the square leans the hesitation toward 0.
                    float hesitation = (float)_random.NextDouble();
                    _nextSongAt = Time + _reactionSeconds + (hesitation * hesitation * LongestHesitationSeconds);
                }
            }

            Vector3 direction = Vector3.zero;
            float distance = 0f;
            if (_hasDestination)
            {
                Vector3 offset = Flat(_destination - _core);
                distance = offset.magnitude;
                if (distance > ArriveMetres)
                {
                    direction = offset / distance;
                }
            }

            // As the View's motor: the core stays put while rooted or inside the giant, and the move then only steers the giant.
            if (!_session.IsCoreRooted && !_session.IsColossusActive && direction != Vector3.zero)
            {
                float speed = _match.CoreMove.Speed * _session.CoreSpeedMultiplier;
                _core += direction * Mathf.Min(speed * Step, distance);
            }

            if (holes != 0)
            {
                for (int song = 0; song < _pricesBeforePress.Length; song++)
                {
                    _pricesBeforePress[song] = _session.SongPrice(song);
                }
            }

            var frame = new HiveDefenseFrame(
                _core, _core, new Vector2(direction.x, direction.z), holes, _punch, _kick, false, _give, _callWave);
            _session.Tick(in frame, Step);
            _core = _session.CorePosition;
            _ticks++;
            MeasureRestOfDamage();

            // Measured against the health at the end of the last tick, as auto-aim notes a hit, so a change between ticks
            // counts too. In a match only the session's tick changes a hive's health.
            for (int hive = 0; hive < _hiveHealth.Length; hive++)
            {
                float health = _session.Hives[hive].Health;
                float change = health - _hiveHealth[hive];
                _hiveHealth[hive] = health;
                if (change > 0f)
                {
                    _healedHealth += change;
                }
                else if (change < 0f)
                {
                    _hiveHitAt[hive] = Time;
                }
            }

            MeasureQuiet();
        }

        // A survive phase with no beekeeper in the Sting's reach of the core and no hive under attack: nothing to decide.
        private void MeasureQuiet()
        {
            bool isQuiet = IsSurvive && _session.State == HiveDefenseState.Playing
                && CountWithin(_core, _match.Sting.MaxRange * _session.SizeScale) == 0;
            for (int hive = 0; isQuiet && hive < _session.Hives.Count; hive++)
            {
                isQuiet = !_session.Hives[hive].IsUnderAttack;
            }

            if (IsSurvive)
            {
                _run.SurviveSeconds += Step;
            }

            _quietSeconds = isQuiet ? _quietSeconds + Step : 0f;
            _run.LongestQuietSeconds = Math.Max(_run.LongestQuietSeconds, _quietSeconds);
        }

        // Every change of the count is sorted by where it came from. A nest raises the capacity just before its wasps
        // arrive, and an early call's bonus is told apart by the event that follows its change. Every other gain came from
        // the stores: collected beside a hive, called by Hive Call, or taken by a respawn (whose floor is counted with them).
        // Losses are sorted at the end, once the prices and the healing are known.
        private void OnCountChanged(int count)
        {
            int delta = count - _lastCount;
            _lastCount = count;
            if (delta < 0)
            {
                _lost -= delta;
                return;
            }

            if (_session.Swarm.Capacity != _knownCapacity)
            {
                _knownCapacity = _session.Swarm.Capacity;
                _run.EarnedFromNests += delta;
                return;
            }

            _run.EarnedFromStores += delta;
        }

        private void OnCollapsed()
        {
            _run.Collapses++;
        }

        // A cast pays the price it had just before the note that cast it: the first cast of a song is free, and a tower costs
        // in a farm phase too.
        private void OnSongCast(int song)
        {
            _run.SpentOnSongs += _pricesBeforePress[song];
            _run.CastsBySong[song]++;
            if (_session.Caster[song] is SlotSongEffect slotSong && slotSong.Slot == SongSlot.Post)
            {
                _run.SpentOnTowers += _pricesBeforePress[song];
            }

            if (!_session.AreSongsFree)
            {
                _run.SongsCast++;
            }
        }

        private void OnWaspsGranted(int wasps)
        {
            _run.EarnedFromStores -= wasps;
            _run.EarnedFromEarlyCalls += wasps;
        }

        // A beekeeper is measured from the health it spawns with. The bot hears a stick's hive hit after the session, which has
        // applied any shell's thorn by then.
        private void OnBeekeeperSpawned(BeekeeperBrain brain)
        {
            _spawned++;
            int index = _brains.IndexOf(brain);
            if (index < 0)
            {
                _brains.Add(brain);
                _brainHealth.Add(brain.Health);
                brain.HiveHit += OnHiveHit;
                return;
            }

            _brainHealth[index] = brain.Health;
        }

        private void OnSongLanded(SwarmForm form, Vector3 point)
        {
            MeasureDamage(_songOfForm[(int)form], null);
        }

        private void OnColossusLanded(ColossusAttack attack)
        {
            MeasureDamage(_colossusSong, null);
        }

        private void OnHiveHit(BeekeeperBrain brain, int hive)
        {
            SlotState post = _session.Forms.Slot(SongSlot.Post);
            if (post.IsActive && post.Form == SwarmForm.Honeycomb)
            {
                MeasureDamage(_songOfForm[(int)SwarmForm.Honeycomb], brain);
            }
        }

        // What is left at the end of a tick is the Sting's, which hurts while it holds on and lands nothing.
        private void MeasureRestOfDamage()
        {
            SlotState strike = _session.Forms.Slot(SongSlot.Strike);
            MeasureDamage(strike.IsActive ? _songOfForm[(int)strike.Form] : -1, null);
        }

        // Gives every beekeeper's health lost since it was last measured (only <paramref name="only"/>'s when given) to song
        // number <paramref name="song"/>, or to nobody when it is -1.
        private void MeasureDamage(int song, BeekeeperBrain only)
        {
            for (int index = 0; index < _brains.Count; index++)
            {
                BeekeeperBrain brain = _brains[index];
                float lost = _brainHealth[index] - brain.Health;
                if (lost <= 0f || (only != null && brain != only))
                {
                    continue;
                }

                _brainHealth[index] = brain.Health;
                if (song >= 0)
                {
                    _run.DamageBySong[song] += lost;
                }
                else
                {
                    _run.UnattributedDamage += lost;
                }
            }
        }

        private PlaytestRun Finish()
        {
            HiveSet hives = _session.Hives;
            float health = 0f;
            float full = 0f;
            float weakest = 1f;
            for (int hive = 0; hive < hives.Count; hive++)
            {
                health += hives[hive].Health;
                full += hives[hive].MaxHealth;
                weakest = Math.Min(weakest, hives[hive].Health / hives[hive].MaxHealth);
            }

            _run.Won = _session.State == HiveDefenseState.Won;
            _run.TimedOut = _session.State == HiveDefenseState.Playing;
            _run.Round = _session.Round;
            _run.HiveHealthShare = health / full;
            _run.WeakestHiveShare = weakest;
            _run.HivesStanding = hives.StandingCount;
            _run.WaspsLeft = _session.Swarm.Count;
            _run.Capacity = _session.Swarm.Capacity;
            _run.SpentOnHealing = Mathf.RoundToInt(_healedHealth * _match.Hive.WaspsPerHealthPoint);
            _run.LostToHits = Math.Max(0, _lost - _run.SpentOnSongs - _run.SpentOnHealing);
            _run.Level = _session.Level;
            _run.Seconds = Time;
            return _run;
        }

        private static Vector3 Flat(Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
