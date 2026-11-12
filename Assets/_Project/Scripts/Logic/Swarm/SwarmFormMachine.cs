using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The one place the session and the view ask what the swarm is doing. The whole swarm takes one form: the river, Hive
    /// Call, the Colossus or the collapse. Beside it up to three songs run in their slots (Post, Guard and Strike), each with
    /// its own squad of wasps, and a Gust pulse may ripple through on top.
    /// </summary>
    /// <remarks>
    /// The session ticks <see cref="SwarmBody"/> itself. While Hive Call lasts, this machine calls every standing hive's store
    /// into the swarm (<see cref="HiveSet.HandOutAll"/>, then <see cref="SwarmBody.Gain"/>), so the call makes no wasp from
    /// nothing, and ends the call when the stores are empty or the swarm is full: at its capacity, which nests raise. Nothing starts while the swarm is
    /// collapsed or the Colossus stands: the Start methods then return false and change nothing.
    /// Hive Call ends the Strike song and keeps the Guard and Post songs; an Attack or Power cast ends Hive Call, and a
    /// Defense cast keeps it. The Colossus ends the Strike and Guard songs and Hive Call, and keeps the Post song. A collapse
    /// ends every slot and the Gust at once.
    /// The giant's time lives here (plan section I): it stands its song's seconds, each kill adds more up to its most, and a
    /// giant in the air (the leap slam) lands before its time can end it. A collapse ends it at once, in the air or not.
    /// What a running song changes in the match (the Dome's cut of every loss and its slower core, say) comes from its rule
    /// and is read here, multiplied over the running rules, so a new song needs no change to this class or to the session.
    /// </remarks>
    public sealed class SwarmFormMachine : IDamageShield
    {
        private const float NoGust = -1f;

        private readonly SwarmBody _body;
        private readonly FuryMeter _fury;
        private readonly SongSlots _slots;
        private readonly List<SongRule> _rules = new List<SongRule>();

        private SwarmForm _current = SwarmForm.River;
        private float _secondsInForm;
        private float _colossusSeconds;
        private float _colossusSecondsPerKill;
        private float _colossusMostSeconds;
        private bool _isColossusAirborne;
        private Vector3 _core;
        private float _gustSeconds = NoGust;
        private float _gustDuration;
        private Vector3 _gustCenter;
        private float _gustRadius;
        private float _gustSizeScale = 1f;
        private GustSong _gustSong;
        private IReadOnlyList<ISwarmTarget> _gustEnemies;

        /// <summary>Starts in the river with every slot empty. The squads' bands must lie within 0 to 1 and never overlap.</summary>
        public SwarmFormMachine(SwarmBody body, FuryMeter fury, SquadConfig squads)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _fury = fury ?? throw new ArgumentNullException(nameof(fury));
            _slots = new SongSlots(squads);
            _body.Collapsed += OnCollapsed;
            _body.Respawned += OnRespawned;
        }

        /// <summary>The whole swarm's form: River, HiveCall, Colossus or Collapsed. Slot songs and the Gust never change it.</summary>
        public SwarmForm Current => _current;

        /// <summary>Seconds since the whole swarm's form began.</summary>
        public float SecondsInForm => _secondsInForm;

        /// <summary>
        /// Seconds the giant has left to stand, for the HUD's ring: 0 unless the Colossus stands, and 0 while it finishes a
        /// leap after its time ran out.
        /// </summary>
        public float ColossusSecondsLeft => _current == SwarmForm.Colossus ? Math.Max(0f, _colossusSeconds - _secondsInForm) : 0f;

        /// <summary>
        /// Seconds the giant stands in all, as of now: its song's seconds and the time its kills have added, never past its
        /// most (plan section I). 0 unless the Colossus stands.
        /// </summary>
        public float ColossusSecondsTotal => _current == SwarmForm.Colossus ? _colossusSeconds : 0f;

        /// <summary>
        /// True while the Dome runs in the Guard slot. Its return beat does not count. What the dome does (fewer wasps lost, a
        /// slower core) comes from its rule, through <see cref="WaspLossFraction"/> and <see cref="CoreSpeedMultiplier"/>.
        /// </summary>
        public bool IsDomed => _slots.RunningForm(SongSlot.Guard) == SwarmForm.Dome;

        /// <summary>
        /// True in Hive Call: the core stands still while the call lasts. The Colossus does not root it: the core rides inside
        /// the giant, which the session moves.
        /// </summary>
        public bool IsCoreRooted => _current == SwarmForm.HiveCall;

        /// <summary>The share, from 0 to 1, of each wasp loss that reaches the swarm: what every running song lets through, multiplied. 1 with none.</summary>
        public float WaspLossFraction => _slots.WaspLossFraction();

        /// <summary>How much the running songs multiply the core's speed. 1 with none.</summary>
        public float CoreSpeedMultiplier => _slots.CoreSpeedMultiplier();

        /// <summary>True unless a running song keeps smoke from slowing the core.</summary>
        public bool SmokeSlowsCore => _slots.SmokeSlowsCore();

        /// <summary>Seconds since the last Gust began, or -1 when there is none or it has finished.</summary>
        public float GustSeconds => _gustSeconds;

        /// <summary>Where the last Gust began.</summary>
        public Vector3 GustCenter => _gustCenter;

        /// <summary>Metres the last Gust reaches, grown by the swarm's level when it was cast.</summary>
        public float GustRadius => _gustRadius;

        /// <summary>
        /// How many stings the Sting that holds the Strike slot sent (plan section O), while it runs and through its return
        /// beat, so the picture shares the squad between them and pops each where it was; 0 when no Sting holds the slot.
        /// </summary>
        public int StingCount => _slots.RuleIn(SongSlot.Strike) is StingRule sting ? sting.Count : 0;

        /// <summary>The shield that blocks a blow on the core: the running song that lets the least of a loss through, or River with none.</summary>
        internal SwarmForm CoreShieldForm => _slots.CoreShieldForm();

        private bool IsBlocked => _current == SwarmForm.Collapsed || _current == SwarmForm.Colossus;

        /// <summary>Raised with the new form every time the whole swarm's form changes.</summary>
        public event Action<SwarmForm> FormChanged;

        /// <summary>Raised with the song's form and the point when a slot song's strike visibly lands.</summary>
        public event Action<SwarmForm, Vector3> Landed;

        /// <summary>
        /// Raised once for every hit a running slot song lands on a beekeeper (plan section O), for the feel of it. The session
        /// passes it on (<see cref="HiveDefenseSession.SongLanded"/>), with the blows that shields block and the giant's.
        /// </summary>
        public event Action<SongHit> SongLanded;

        /// <summary>What the view needs to draw a slot.</summary>
        public SlotState Slot(SongSlot slot)
        {
            return _slots.State(slot);
        }

        /// <summary>
        /// Where sting number <paramref name="index"/> of the Sting that holds the Strike slot wraps now: its living target,
        /// followed as it walks, or the air it wraps. Zero past <see cref="StingCount"/>, and when no Sting holds the slot.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is below 0 or not below <see cref="StingSong.MostStings"/>.</exception>
        public Vector3 StingTarget(int index)
        {
            if (index < 0 || index >= StingSong.MostStings)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "A Sting sends from 1 to " + StingSong.MostStings + " stings.");
            }

            return _slots.RuleIn(SongSlot.Strike) is StingRule sting ? sting.Target(index) : Vector3.zero;
        }

        /// <summary>The shield that blocks a blow on hive number <paramref name="hive"/>: the running song that lets the least through to it, or River with none.</summary>
        internal SwarmForm HiveShieldForm(int hive)
        {
            return _slots.HiveShieldForm(hive);
        }

        /// <summary>The share, from 0 to 1, of the damage from sticks and smoke that reaches hive number <paramref name="hive"/>: what every running song lets through, multiplied. 1 with none.</summary>
        public float HiveDamageFraction(int hive)
        {
            return _slots.HiveDamageFraction(hive);
        }

        /// <summary>The stick <paramref name="attacker"/> hit hive number <paramref name="hive"/>, which has taken the damage. Running songs hear of it.</summary>
        public void NotifyHiveHit(int hive, ISwarmTarget attacker)
        {
            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            _slots.NotifyHiveHit(hive, attacker);
        }

        /// <summary>
        /// Starts a slot song in its slot, in place of whatever held it, whose strike that has not landed is lost. An Attack
        /// or Power song ends Hive Call. False, changing nothing, while the swarm is collapsed or the Colossus stands.
        /// </summary>
        public bool StartSong(SlotSongEffect song, in SwarmSongContext context)
        {
            if (song == null)
            {
                throw new ArgumentNullException(nameof(song));
            }

            if (song.Share < 0f || song.Share > 1f)
            {
                throw new ArgumentException($"{song.name}: the share must be from 0 to 1, but is {song.Share}.", nameof(song));
            }

            if (IsBlocked)
            {
                return false;
            }

            _core = context.CorePosition;

            if (_current == SwarmForm.HiveCall && song.Category != SongCategory.Defense)
            {
                SetForm(SwarmForm.River);
            }

            SongRule rule = RuleFor(song);
            rule.Start(in context);
            _slots.Replace(song.Slot, rule);
            return true;
        }

        /// <summary>
        /// Starts a Gust pulse at <paramref name="center"/> reaching <paramref name="radius"/> metres for the given seconds.
        /// It takes no slot and ends nothing. False while the swarm is collapsed or the Colossus stands.
        /// </summary>
        public bool StartGust(Vector3 center, float radius, float seconds)
        {
            return StartPulse(center, radius, seconds, null, null, 1f);
        }

        /// <summary>
        /// Starts <paramref name="song"/>'s pulse at <paramref name="center"/>, as <see cref="StartGust(Vector3, float, float)"/>
        /// does, and lets the song push each beekeeper in <paramref name="enemies"/> on the step its ring reaches them. The
        /// swarm's level multiplies the pulse's reach by <paramref name="sizeScale"/>.
        /// </summary>
        internal bool StartGust(GustSong song, Vector3 center, IReadOnlyList<ISwarmTarget> enemies, float sizeScale)
        {
            if (song == null)
            {
                throw new ArgumentNullException(nameof(song));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            return StartPulse(center, song.ClearRadius * sizeScale, song.Seconds, song, enemies, sizeScale);
        }

        /// <summary>
        /// Starts Hive Call, which calls every standing hive's store to the core while the core stays rooted. It lasts until
        /// the stores are empty, the swarm is full, the core moves, an Attack or Power song is cast, or the core is hit while
        /// no Guard song runs. It ends the Strike song at once and keeps the Guard and Post songs.
        /// </summary>
        public bool StartHiveCall()
        {
            if (IsBlocked)
            {
                return false;
            }

            _slots.End(SongSlot.Strike);
            SetForm(SwarmForm.HiveCall);
            return true;
        }

        /// <summary>
        /// Starts the Colossus for the given seconds, with no time added for kills. It ends the Strike and Guard songs at once,
        /// so their wasps fly straight into the build, ends Hive Call and keeps the Post song. False while the swarm is collapsed.
        /// </summary>
        public bool StartColossus(float seconds)
        {
            return StartColossus(seconds, 0f, seconds);
        }

        /// <summary>
        /// Starts the Colossus as <see cref="StartColossus(float)"/> does, for <paramref name="seconds"/>, and each beekeeper
        /// it kills (<see cref="AddColossusKills"/>) adds <paramref name="secondsPerKill"/>, never past
        /// <paramref name="mostSeconds"/> in all (plan section I).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A time is negative, or the most is below the seconds it starts with.</exception>
        public bool StartColossus(float seconds, float secondsPerKill, float mostSeconds)
        {
            RequireSeconds(seconds);
            if (secondsPerKill < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerKill), secondsPerKill, "A kill cannot take time away from the giant.");
            }

            if (mostSeconds < seconds)
            {
                throw new ArgumentOutOfRangeException(nameof(mostSeconds), mostSeconds, "The giant's most seconds cannot be below the seconds it starts with.");
            }

            if (_current == SwarmForm.Collapsed)
            {
                return false;
            }

            _slots.End(SongSlot.Strike);
            _slots.End(SongSlot.Guard);
            _colossusSeconds = seconds;
            _colossusSecondsPerKill = secondsPerKill;
            _colossusMostSeconds = mostSeconds;
            _isColossusAirborne = false;
            SetForm(SwarmForm.Colossus);
            return true;
        }

        /// <summary>
        /// The giant killed <paramref name="kills"/> beekeepers: each adds its seconds per kill to the time it stands, never
        /// past its most. Ignored unless the Colossus stands.
        /// </summary>
        public void AddColossusKills(int kills)
        {
            if (_current != SwarmForm.Colossus || kills <= 0)
            {
                return;
            }

            _colossusSeconds = Math.Min(_colossusMostSeconds, _colossusSeconds + (kills * _colossusSecondsPerKill));
        }

        /// <summary>
        /// Whether the giant is in the air (the leap slam). While it is, its time running out does not end it: it lands first,
        /// and ends on the first tick after it lands. A collapse ends it in the air all the same. Ignored unless the Colossus
        /// stands, and every new giant starts on the ground.
        /// </summary>
        public void SetColossusAirborne(bool isAirborne)
        {
            _isColossusAirborne = isAirborne && _current == SwarmForm.Colossus;
        }

        /// <summary>Damage the Colossus dealt. It fills Fury only while the Colossus stands.</summary>
        public void AddColossusDamage(float damage)
        {
            if (_current == SwarmForm.Colossus && damage > 0f)
            {
                _fury.AddDamage(damage);
            }
        }

        /// <summary>Where the core stands now. Songs that act around the core, such as the Tornado, act where it stands when they land.</summary>
        public void SetCore(Vector3 position)
        {
            _core = position;
        }

        /// <summary>The core was hit from <paramref name="from"/>. Running songs hear of it, and it ends Hive Call unless a Guard song runs.</summary>
        public void NotifyHit(Vector3 from)
        {
            _slots.NotifyCoreHit(GroundMath.Direction(_core, from));

            if (_current == SwarmForm.HiveCall && !_slots.IsRunning(SongSlot.Guard))
            {
                SetForm(SwarmForm.River);
            }
        }

        /// <summary>
        /// The match entered <paramref name="phase"/>: a wave started (Survive) or ended (Farm). Running songs hear of it, so a
        /// tower that guards a wave knows when the wave is over. Call it once on each change of phase.
        /// </summary>
        public void NotifyPhase(MatchPhase phase)
        {
            _slots.NotifyPhase(phase);
        }

        /// <summary>The core moved. Ends Hive Call.</summary>
        public void NotifyCoreMoved()
        {
            if (_current == SwarmForm.HiveCall)
            {
                SetForm(SwarmForm.River);
            }
        }

        /// <summary>Advances the whole swarm's form, every slot and the Gust. During Hive Call it calls the stores of <paramref name="hives"/>.</summary>
        public void Tick(float deltaTime, HiveSet hives)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Time cannot run backwards.");
            }

            if (hives == null)
            {
                throw new ArgumentNullException(nameof(hives));
            }

            _secondsInForm += deltaTime;
            TickForm(deltaTime, hives);
            _slots.Tick(deltaTime, _core);
            TickGust(deltaTime);
        }

        private static void RequireSeconds(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A song cannot last a negative time.");
            }
        }

        private bool StartPulse(Vector3 center, float radius, float seconds, GustSong pusher, IReadOnlyList<ISwarmTarget> enemies, float sizeScale)
        {
            RequireSeconds(seconds);

            if (radius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius cannot be negative.");
            }

            if (IsBlocked)
            {
                return false;
            }

            _gustSeconds = 0f;
            _gustDuration = seconds;
            _gustCenter = center;
            _gustRadius = radius;
            _gustSizeScale = sizeScale;
            _gustSong = pusher;
            _gustEnemies = enemies;
            return true;
        }

        private void EndGust()
        {
            _gustSeconds = NoGust;
            _gustSong = null;
            _gustEnemies = null;
        }

        private void TickForm(float deltaTime, HiveSet hives)
        {
            if (_current == SwarmForm.HiveCall)
            {
                _body.Gain(hives.HandOutAll(deltaTime, _body.Room));

                if (_body.Count >= _body.Capacity || !hives.HasStoredWasps)
                {
                    SetForm(SwarmForm.River);
                }
            }
            else if (_current == SwarmForm.Colossus && !_isColossusAirborne && _secondsInForm >= _colossusSeconds)
            {
                SetForm(SwarmForm.River);
            }
        }

        private void TickGust(float deltaTime)
        {
            if (_gustSeconds < 0f)
            {
                return;
            }

            float before = _gustSeconds;
            _gustSeconds += deltaTime;

            // The push lands with the ring the picture draws, not on the cast frame.
            if (_gustSong != null)
            {
                _gustSong.PushWhereTheRingPassed(_gustEnemies, _gustCenter, before, _gustSeconds, _gustSizeScale);
            }

            if (_gustSeconds >= _gustDuration)
            {
                EndGust();
            }
        }

        // Each song has one rule, made at its first cast and started afresh on every cast after, so casting allocates once.
        private SongRule RuleFor(SlotSongEffect song)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                if (ReferenceEquals(_rules[i].Song, song))
                {
                    return _rules[i];
                }
            }

            SongRule rule = song.CreateRule();

            if (rule == null || !ReferenceEquals(rule.Song, song))
            {
                throw new InvalidOperationException($"{song.name}: CreateRule must return a new rule of this song.");
            }

            rule.Landed += OnRuleLanded;
            rule.HitLanded += OnRuleHitLanded;
            _rules.Add(rule);
            return rule;
        }

        private void SetForm(SwarmForm form)
        {
            bool changed = form != _current;
            _current = form;
            _secondsInForm = 0f;

            if (changed)
            {
                FormChanged?.Invoke(form);
            }
        }

        private void OnRuleLanded(SongRule rule, Vector3 point)
        {
            Landed?.Invoke(rule.Song.Form, point);
        }

        private void OnRuleHitLanded(SongRule rule, SongHit hit)
        {
            SongLanded?.Invoke(hit);
        }

        private void OnCollapsed()
        {
            _slots.EndAll();
            EndGust();
            SetForm(SwarmForm.Collapsed);
        }

        private void OnRespawned()
        {
            if (_current == SwarmForm.Collapsed)
            {
                SetForm(SwarmForm.River);
            }
        }
    }
}
