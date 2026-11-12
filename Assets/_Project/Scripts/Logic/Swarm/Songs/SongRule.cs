using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// What one slot song does while it runs: its damage over time, when it lets go, and the numbers its picture needs.
    /// Each <see cref="SlotSongEffect"/> makes one rule, which the slot engine starts afresh on every cast of that song.
    /// </summary>
    /// <remarks>
    /// A rule lets go (<see cref="Release"/>) when its time is up or it has nothing left to do. Its slot then plays a return
    /// beat, which is only a picture: the rule is not ticked again, so it hurts nothing more. A rule that a cast, Hive Call,
    /// the Colossus or a collapse ends is simply never ticked again, so a strike that has not landed is lost.
    /// While it runs, a rule may also change the match through its modifiers (<see cref="WaspLossFraction"/>,
    /// <see cref="CoreSpeedMultiplier"/>, <see cref="SmokeSlowsCore"/>, <see cref="HiveDamageFraction"/>) and hear of
    /// hits (<see cref="NotifyCoreHit"/>, <see cref="NotifyHiveHit"/>). The machine reads them only from running rules, so
    /// a rule that ends or lets go stops changing anything at once.
    /// A rule hurts a beekeeper through <see cref="Hit"/>, which reports the hit (plan section O), so every hit a song lands
    /// is felt once; a rule that hurts a little each step reports its own hits (<see cref="ReportHit"/>).
    /// </remarks>
    public abstract class SongRule
    {
        private const float StillRunning = -1f;

        /// <summary>Makes the rule of <paramref name="song"/>.</summary>
        protected SongRule(SlotSongEffect song)
        {
            Song = song != null ? song : throw new ArgumentNullException(nameof(song));
        }

        /// <summary>The song this rule belongs to.</summary>
        public SlotSongEffect Song { get; }

        /// <summary>Seconds after the cast at which the rule let go, or -1 while it runs.</summary>
        public float ReleasedAt { get; private set; } = StillRunning;

        /// <summary>True once the rule has let go.</summary>
        public bool IsReleased => ReleasedAt >= 0f;

        /// <summary>The song's own numbers for its picture. See <see cref="SlotState.ParamA"/>.</summary>
        public abstract Vector4 ParamA { get; }

        /// <summary>The song's next numbers for its picture. Keep w at 0: the view uses it. See <see cref="SlotState.ParamB"/>.</summary>
        public abstract Vector4 ParamB { get; }

        /// <summary>The share, from 0 to 1, of each wasp loss (a hit on the core, or smoke) that still reaches the swarm while the rule runs.</summary>
        protected internal virtual float WaspLossFraction => 1f;

        /// <summary>How much the core's speed is multiplied while the rule runs.</summary>
        protected internal virtual float CoreSpeedMultiplier => 1f;

        /// <summary>False while the rule keeps smoke from slowing the core.</summary>
        protected internal virtual bool SmokeSlowsCore => true;

        /// <summary>Raised with the rule and the point when its strike visibly lands.</summary>
        internal event Action<SongRule, Vector3> Landed;

        /// <summary>Raised with the rule and the hit for every hit the rule lands on a beekeeper.</summary>
        internal event Action<SongRule, SongHit> HitLanded;

        /// <summary>Starts the rule afresh for a cast, dropping whatever it was doing.</summary>
        internal void Start(in SwarmSongContext context)
        {
            ReleasedAt = StillRunning;
            Begin(in context);
        }

        /// <summary>Takes what the rule needs from the cast, such as its target, the beekeepers and the Fury meter.</summary>
        protected abstract void Begin(in SwarmSongContext context);

        /// <summary>Advances the rule by one step. Called only while the rule runs.</summary>
        protected internal abstract void Tick(in SongTick tick);

        /// <summary>
        /// The core was hit from <paramref name="direction"/> (a unit vector on the ground, from the core toward the hit)
        /// <paramref name="seconds"/> after the cast. Called only while the rule runs.
        /// </summary>
        protected internal virtual void NotifyCoreHit(Vector3 direction, float seconds)
        {
        }

        /// <summary>
        /// The share, from 0 to 1, of the damage from sticks and smoke that still reaches hive number <paramref name="hive"/>
        /// while the rule runs.
        /// </summary>
        protected internal virtual float HiveDamageFraction(int hive)
        {
            return 1f;
        }

        /// <summary>
        /// The stick beekeeper <paramref name="attacker"/> hit hive number <paramref name="hive"/> <paramref name="seconds"/>
        /// after the cast; the hive has already taken the damage. Called only while the rule runs.
        /// </summary>
        protected internal virtual void NotifyHiveHit(int hive, ISwarmTarget attacker, float seconds)
        {
        }

        /// <summary>
        /// The match entered <paramref name="phase"/> <paramref name="seconds"/> after the cast: a wave started (Survive) or
        /// ended (Farm). Called only while the rule runs, on each change of phase, never twice for the same phase in a row.
        /// A tower that guards a wave uses it to know when the wave is over.
        /// </summary>
        protected internal virtual void NotifyPhase(MatchPhase phase, float seconds)
        {
        }

        /// <summary>Lets go at <paramref name="seconds"/> after the cast. Only the first call counts.</summary>
        protected void Release(float seconds)
        {
            if (!IsReleased)
            {
                ReleasedAt = Math.Max(0f, seconds);
            }
        }

        /// <summary>Tells the slot engine that the strike visibly lands at <paramref name="point"/> now.</summary>
        protected void Land(Vector3 point)
        {
            Landed?.Invoke(this, point);
        }

        /// <summary>
        /// Hurts <paramref name="target"/> by <paramref name="damage"/> and reports the hit where it stands, as a kill when it
        /// dies of it and as heavy when the song is one of the big hits (<see cref="SlotSongEffect.HitsHard"/>). Nothing is
        /// reported for no damage. The rule fills Fury itself, as it always has.
        /// </summary>
        protected void Hit(ISwarmTarget target, float damage)
        {
            if (!(damage > 0f))
            {
                return;
            }

            target.TakeDamage(damage);
            ReportHit(target.Position, damage, !target.IsAlive);
        }

        /// <summary>Reports a hit the rule dealt by itself, such as a sting's squeeze, for the feel of it (plan section O).</summary>
        protected void ReportHit(Vector3 position, float damage, bool isKill)
        {
            HitLanded?.Invoke(this, new SongHit(position, damage, isKill, false, Song.Form, Song.HitsHard));
        }
    }
}
