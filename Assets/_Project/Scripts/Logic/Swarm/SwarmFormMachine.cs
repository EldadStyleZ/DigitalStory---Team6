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
    /// The session ticks <see cref="SwarmBody"/> itself. While Hive Call lasts, this machine also calls
    /// <see cref="SwarmBody.Gain"/> and ends the call when the swarm is at its maximum. Nothing starts while the swarm is
    /// collapsed or the Colossus stands: the Start methods then return false and change nothing.
    /// Hive Call ends the Strike song and keeps the Guard and Post songs; an Attack or Power cast ends Hive Call, and a
    /// Defense cast keeps it. The Colossus ends the Strike and Guard songs and Hive Call, and keeps the Post song. A collapse
    /// ends every slot and the Gust at once.
    /// </remarks>
    public sealed class SwarmFormMachine
    {
        private const float NoGust = -1f;

        private readonly SwarmBody _body;
        private readonly FuryMeter _fury;
        private readonly SongSlots _slots;
        private readonly List<SongRule> _rules = new List<SongRule>();

        private SwarmForm _current = SwarmForm.River;
        private float _secondsInForm;
        private float _colossusSeconds;
        private Vector3 _piper;
        private float _gustSeconds = NoGust;
        private float _gustDuration;
        private Vector3 _gustCenter;
        private float _gustRadius;

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

        /// <summary>True while the Dome runs in the Guard slot, which cuts wasp losses. Its return beat does not count.</summary>
        public bool IsDomed => _slots.RunningForm(SongSlot.Guard) == SwarmForm.Dome;

        /// <summary>True in Hive Call and the Colossus: the Piper stands still and plays.</summary>
        public bool IsPiperRooted => _current == SwarmForm.HiveCall || _current == SwarmForm.Colossus;

        /// <summary>Seconds since the last Gust began, or -1 when there is none or it has finished.</summary>
        public float GustSeconds => _gustSeconds;

        /// <summary>Where the last Gust began.</summary>
        public Vector3 GustCenter => _gustCenter;

        /// <summary>Metres the last Gust reaches.</summary>
        public float GustRadius => _gustRadius;

        private bool IsBlocked => _current == SwarmForm.Collapsed || _current == SwarmForm.Colossus;

        /// <summary>Raised with the new form every time the whole swarm's form changes.</summary>
        public event Action<SwarmForm> FormChanged;

        /// <summary>Raised with the song's form and the point when a slot song's strike visibly lands.</summary>
        public event Action<SwarmForm, Vector3> Landed;

        /// <summary>What the view needs to draw a slot.</summary>
        public SlotState Slot(SongSlot slot)
        {
            return _slots.State(slot);
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

            _piper = context.PiperPosition;

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
            return true;
        }

        /// <summary>
        /// Starts Hive Call, which lasts until the Piper moves, an Attack or Power song is cast, the swarm is full, or the Piper
        /// is hit while no Guard song runs. It ends the Strike song at once and keeps the Guard and Post songs.
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
        /// Starts the Colossus for the given seconds. It ends the Strike and Guard songs at once, so their wasps fly straight
        /// into the build, ends Hive Call and keeps the Post song. False while the swarm is collapsed.
        /// </summary>
        public bool StartColossus(float seconds)
        {
            RequireSeconds(seconds);

            if (_current == SwarmForm.Collapsed)
            {
                return false;
            }

            _slots.End(SongSlot.Strike);
            _slots.End(SongSlot.Guard);
            _colossusSeconds = seconds;
            SetForm(SwarmForm.Colossus);
            return true;
        }

        /// <summary>Damage the Colossus dealt. It fills Fury only while the Colossus stands.</summary>
        public void AddColossusDamage(float damage)
        {
            if (_current == SwarmForm.Colossus && damage > 0f)
            {
                _fury.AddDamage(damage);
            }
        }

        /// <summary>Where the Piper stands now. Songs that act around the Piper, such as the Tornado, act where it stands when they land.</summary>
        public void SetPiper(Vector3 position)
        {
            _piper = position;
        }

        /// <summary>The Piper was hit from <paramref name="from"/>. Running songs hear of it, and it ends Hive Call unless a Guard song runs.</summary>
        public void NotifyHit(Vector3 from)
        {
            _slots.NotifyPiperHit(GroundMath.Direction(_piper, from));

            if (_current == SwarmForm.HiveCall && !_slots.IsRunning(SongSlot.Guard))
            {
                SetForm(SwarmForm.River);
            }
        }

        /// <summary>The Piper moved. Ends Hive Call.</summary>
        public void NotifyPiperMoved()
        {
            if (_current == SwarmForm.HiveCall)
            {
                SetForm(SwarmForm.River);
            }
        }

        /// <summary>Advances the whole swarm's form, every slot and the Gust.</summary>
        public void Tick(float deltaTime, int standingHives)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Time cannot run backwards.");
            }

            _secondsInForm += deltaTime;
            TickForm(deltaTime, standingHives);
            _slots.Tick(deltaTime, _piper);
            TickGust(deltaTime);
        }

        private static void RequireSeconds(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A song cannot last a negative time.");
            }
        }

        private void TickForm(float deltaTime, int standingHives)
        {
            if (_current == SwarmForm.HiveCall)
            {
                _body.Gain(deltaTime, standingHives);

                if (_body.Count >= _body.Max)
                {
                    SetForm(SwarmForm.River);
                }
            }
            else if (_current == SwarmForm.Colossus && _secondsInForm >= _colossusSeconds)
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

            _gustSeconds += deltaTime;

            if (_gustSeconds >= _gustDuration)
            {
                _gustSeconds = NoGust;
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

        private void OnCollapsed()
        {
            _slots.EndAll();
            _gustSeconds = NoGust;
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
