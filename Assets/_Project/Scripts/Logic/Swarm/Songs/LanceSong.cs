using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A Strike squad draws back into a spear behind the core and throws it down a line, piercing every beekeeper on it.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Lance", fileName = "LanceSong")]
    public sealed class LanceSong : SlotSongEffect
    {
        /// <summary>
        /// Seconds the thrown spear's tip takes from the core to the line's end. Fixed, because the picture flies it for
        /// the same time (LanceFlightSeconds in WaspSwarm.compute), so the line is hurt on the frame the tip lands.
        /// </summary>
        public const float FlightSeconds = 0.1f;

        // K L L H. 70 wasps.
        public LanceSong()
            : base(new SongDefinition("Lance", "A spear of wasps pierces a line of beekeepers", 8f, 2, 3, 3, 0), SongCategory.Attack, 0.37f, 70)
        {
        }

        // Plan section O: it fires at 0.35 s (was 0.5 s), and the 0.8 s after the hit stays, so it lasts 1.15 s (was 1.3 s).
        [field: SerializeField, Min(0f), Tooltip("Seconds the Lance lasts. The streak breaks into sparks and flies home after it, in the slot's return beat.")]
        public float Seconds { get; private set; } = 1.15f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the spear is drawn back. Until then it follows the core and the aimed beekeeper; then its line is locked and the spear is thrown, hitting 0.1 s later.")]
        public float DrawSeconds { get; private set; } = 0.25f;

        [field: SerializeField, Min(0f), Tooltip("Metres the line runs from the core toward the aim.")]
        public float Length { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("Metres across the line. A beekeeper within half of it on either side is on the line.")]
        public float Width { get; private set; } = 1.2f;

        [field: SerializeField, Min(0f), Tooltip("Damage to every beekeeper on the line.")]
        public float Damage { get; private set; } = 60f;

        [field: SerializeField, Min(0f), Tooltip("Metres per second each beekeeper on the line is pushed out to the side it is on.")]
        public float PushSpeed { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the spear's tail flies behind its tip. The picture's number only.")]
        public float TailLagSeconds { get; private set; } = 0.08f;

        /// <summary>
        /// Seconds after the cast at which the spear's tip reaches the line's end and everyone on the line is hit: the draw
        /// and then the <see cref="FlightSeconds"/>, so tuning the draw moves the hit with the picture.
        /// </summary>
        public float HitSeconds => DrawSeconds + FlightSeconds;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses where the spear points.</summary>
        public override bool IsAimed => true;

        /// <summary>The beekeeper whose line from the core holds the most beekeepers (plan section E, Line).</summary>
        public override AimRule AimRule => AimRule.Line;

        /// <summary>As far as the line runs: <see cref="Length"/>.</summary>
        public override float AimRange => Length;

        /// <summary>The Lance's picture.</summary>
        public override SwarmForm Form => SwarmForm.Lance;

        /// <summary>One of the big hits (plan section O): the swarm's motion freezes for a moment on each.</summary>
        public override bool HitsHard => true;

        protected internal override SongRule CreateRule()
        {
            return new LanceRule(this);
        }
    }
}
