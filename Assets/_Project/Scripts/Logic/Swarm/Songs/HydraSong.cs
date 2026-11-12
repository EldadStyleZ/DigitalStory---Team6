using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A Strike squad rears up as necks behind the core, and each head snaps onto one of the beekeepers nearest the core and
    /// bites it. The swarm's level grows more heads (plan section H).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Songs/Hydra", fileName = "HydraSong")]
    public sealed class HydraSong : SlotSongEffect
    {
        // J J L. 60 wasps.
        public HydraSong()
            : base(new SongDefinition("Hydra", "Heads of wasps bite the nearest beekeepers", 6f, 1, 1, 3), SongCategory.Attack, 0.37f, 60)
        {
        }

        // Plan section O: the heads bite at 0.5 s (was 0.7 s), and the 0.9 s after the bite stays, so it lasts 1.4 s (was 1.6 s).
        [field: SerializeField, Min(0f), Tooltip("Seconds the Hydra lasts. Its necks recoil into the river after it, in the slot's return beat.")]
        public float Seconds { get; private set; } = 1.4f;

        [field: SerializeField, Min(0f), Tooltip("Seconds after the cast at which every head bites, the moment the picture's heads snap onto their targets.")]
        public float BiteSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f), Tooltip("Furthest metres from the core of a beekeeper a head may choose. A spare head strikes the ground no further out than this.")]
        public float Reach { get; private set; } = 9f;

        [field: SerializeField, Min(0f), Tooltip("Damage each head deals the beekeeper it bites.")]
        public float Damage { get; private set; } = 40f;

        [field: SerializeField, Range(1, 3), Tooltip("Heads at the swarm's first level. The level adds up to two more, and the picture has room for five.")]
        public int Heads { get; private set; } = 3;

        [field: SerializeField, Min(1), Tooltip("The swarm level from which the Hydra grows one more head.")]
        public int ExtraHeadFromLevel { get; private set; } = 4;

        [field: SerializeField, Min(1), Tooltip("The swarm level from which it grows a second extra head.")]
        public int SecondExtraHeadFromLevel { get; private set; } = 7;

        [field: SerializeField, Range(0f, 90f), Tooltip("Degrees between the spare heads, which fan out around the aim and strike the ground when the Hydra has more heads than beekeepers in reach.")]
        public float SpareFanDegrees { get; private set; } = 30f;

        /// <summary>The Strike slot.</summary>
        public override SongSlot Slot => SongSlot.Strike;

        /// <summary>Auto-aim chooses what its heads bite.</summary>
        public override bool IsAimed => true;

        /// <summary>The beekeepers nearest the core, or the cursor while the mouse is live (plan section E, ThreeNearest).</summary>
        public override AimRule AimRule => AimRule.ThreeNearest;

        /// <summary>As far as a head reaches: <see cref="Reach"/>.</summary>
        public override float AimRange => Reach;

        /// <summary>The Hydra's picture.</summary>
        public override SwarmForm Form => SwarmForm.Hydra;

        /// <summary>One of the big hits (plan section O): the swarm's motion freezes for a moment on each.</summary>
        public override bool HitsHard => true;

        /// <summary>
        /// How many heads the Hydra has at swarm level <paramref name="level"/>: <see cref="Heads"/>, one more from
        /// <see cref="ExtraHeadFromLevel"/> and another from <see cref="SecondExtraHeadFromLevel"/>.
        /// </summary>
        public int HeadCount(int level)
        {
            int heads = Heads;

            if (level >= ExtraHeadFromLevel)
            {
                heads++;
            }

            if (level >= SecondExtraHeadFromLevel)
            {
                heads++;
            }

            return heads;
        }

        /// <summary>
        /// Starts the Hydra in the Strike slot. It strikes several beekeepers, so its rule asks the aim chooser for one a head
        /// as it starts (<see cref="ISongAim.ChooseNearest"/>), instead of the song asking for a single target first.
        /// Nothing happens while the swarm is collapsed or the Colossus stands.
        /// </summary>
        public override void Cast(in SwarmSongContext context)
        {
            context.Forms.StartSong(this, in context);
        }

        protected internal override SongRule CreateRule()
        {
            return new HydraRule(this);
        }
    }
}
