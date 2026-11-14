using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A Strike squad arcs to the most threatening beekeepers and stings them, or wraps the air ahead when there is nothing in
    /// reach. One cast sends one sting for every <see cref="WaspsPerSting"/> living wasps (plan section O).
    /// </summary>
    /// <remarks>
    /// The one rule that reads the live wasp count, by the owner's decision (plan section O): every other song's strength
    /// comes from the swarm's level. The count is taken as the notes complete, before the price is paid, and the price is
    /// <see cref="SwarmSongEffect.WaspCost"/> for each sting sent.
    /// </remarks>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Sting", fileName = "StingSong")]
    public sealed class StingSong : SlotSongEffect
    {
        /// <summary>The most stings one cast can send, which the rule and the picture have room for.</summary>
        public const int MostStings = 10;

        // J K H. 5 wasps a sting (was 10), so a cast at the starting 800 wasps costs 10, as one sting did: cheap on purpose,
        // so the player always has a move. The playtest bot set it after plan section O.
        public StingSong()
            : base(new SongDefinition("Sting", "Wasps wrap around the most threatening beekeepers", 1.5f, 1, 2, 0), SongCategory.Attack, 0.3f, 5, knownFromStart: true)
        {
        }

        [field: SerializeField, Min(0f), Tooltip("Seconds the sting lasts, or until no sting has a living beekeeper left in reach.")]
        public float Seconds { get; private set; } = 4f;

        // Plan section O: the wasps arrive after 0.35 s (was 0.6 s), so the cast answers sooner.
        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps fly before they reach their targets. The damage starts when they arrive.")]
        public float ArriveSeconds { get; private set; } = 0.35f;

        // The whole cast's damage a second, shared evenly by its stings (songs-v2 V). The owner, 2026-11-14: "I want the Sting
        // to be less effective than the other attacks." 12, so a cast deals 43.8 from the arrival to the end at the first
        // level, less than any other attack deals three beekeepers (the Tornado 90). It was 20 for each sting (25 before plan
        // section O), so a cast at 4,000 wasps dealt ten times that.
        [field: SerializeField, Min(0f), Tooltip("Damage the whole cast deals each second once the wasps arrive, shared evenly by its stings and dealt in pulses on the squeezes. More wasps send more stings but never more damage. Stings that share a target add up.")]
        public float DamagePerSecond { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the core the wasps fly. A target further away is out of reach, and the wasps wrap the air this far toward it.")]
        public float MaxRange { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the wasps wrap the empty air when there is no living target in reach.")]
        public float AirSeconds { get; private set; } = 1f;

        [field: SerializeField, Min(1), Tooltip("Living wasps, at the cast, for each sting one cast sends. The owner's number: every 400 wasps, one sting.")]
        public int WaspsPerSting { get; private set; } = 400;

        [field: SerializeField, Range(1, MostStings), Tooltip("The most stings one cast sends, however big the swarm.")]
        public int MaxStings { get; private set; } = MostStings;

        [field: SerializeField, Min(0f), Tooltip("Seconds a sting whose target died takes to reach its next target before it hurts again: about the time the picture's spring carries its wrap over.")]
        public float ReaimSeconds { get; private set; } = 0.2f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between a sting's squeezes, the first on the arrival. On each squeeze a sting deals at once the damage of the time to the next one, as one hit, so the hit is felt as the rings squeeze. Keep it equal to StingTickSeconds in WaspSwarm.compute.")]
        public float SqueezeSeconds { get; private set; } = 0.25f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses what it strikes.</summary>
        public override bool IsAimed => true;

        /// <summary>The most threatening beekeepers within reach (plan section E, Threat), one for each sting.</summary>
        public override AimRule AimRule => AimRule.Threat;

        /// <summary>As far as it flies: <see cref="MaxRange"/>.</summary>
        public override float AimRange => MaxRange;

        /// <summary>The sting's picture.</summary>
        public override SwarmForm Form => SwarmForm.Sting;

        /// <summary>
        /// How many stings one cast sends with <paramref name="livingWasps"/> alive: one for every <see cref="WaspsPerSting"/>,
        /// at least 1 and at most <see cref="MaxStings"/>.
        /// </summary>
        public int StingCount(int livingWasps)
        {
            int most = Mathf.Clamp(MaxStings, 1, MostStings);
            return Mathf.Clamp(Math.Max(0, livingWasps) / Math.Max(1, WaspsPerSting), 1, most);
        }

        /// <summary>The Sting's price for each sting a cast with <paramref name="livingWasps"/> alive sends.</summary>
        public override int WaspCostFor(int livingWasps)
        {
            return WaspCost * StingCount(livingWasps);
        }

        /// <summary>
        /// Starts the Sting in the Strike slot. It strikes several beekeepers, so its rule asks the aim chooser for one a
        /// sting as it starts (<see cref="ISongAim.ChooseThreats"/>), instead of the song asking for a single target first.
        /// Nothing happens while the swarm is collapsed or the Colossus stands.
        /// </summary>
        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartSong(this, in context);
        }

        protected internal override SongRule CreateRule()
        {
            return new StingRule(this);
        }
    }
}
