using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// A song that runs in one of the three slots, beside the songs in the other two, with its own squad of wasps.
    /// A new slot song needs only its asset class, its <see cref="SongRule"/> and one target function in the compute shader.
    /// </summary>
    public abstract class SlotSongEffect : SwarmSongEffect
    {
        [SerializeField, Range(0f, 1f), Tooltip("The share of the living swarm the song takes, from the start of its slot's band. The rest of the band flies as river.")]
        private float _share;

        /// <summary>Sets the song's keys, category, share and price. Pass the plan's numbers as the defaults.</summary>
        protected SlotSongEffect(SongDefinition song, SongCategory category, float share, int waspCost, bool knownFromStart = false)
            : base(song, category, waspCost, knownFromStart)
        {
            _share = share;
        }

        /// <summary>The share of the living swarm the song takes from the start of its slot's band.</summary>
        public float Share => _share;

        /// <summary>The slot the song runs in. A cast replaces only the song in this slot.</summary>
        public abstract SongSlot Slot { get; }

        /// <summary>The song's picture: the form id its squad flies in the compute shader.</summary>
        public abstract SwarmForm Form { get; }

        /// <summary>True when the song strikes a target that auto-aim chooses; false for a song around the core.</summary>
        public virtual bool IsAimed => false;

        /// <summary>How auto-aim chooses the song's target (plan section E). Only read while <see cref="IsAimed"/>.</summary>
        public virtual AimRule AimRule => AimRule.Threat;

        /// <summary>
        /// Metres from the core within which auto-aim may choose the song's target, at the swarm's first level: each level
        /// grows it as it grows the song's reach. Only read while <see cref="IsAimed"/>.
        /// </summary>
        public virtual float AimRange => 0f;

        /// <summary>
        /// Starts the song in its slot. An aimed song first asks the context's aim chooser, when there is one, for its
        /// target by its own rule, within its reach at the context's level. Nothing happens while the swarm is collapsed or
        /// the Colossus stands.
        /// </summary>
        public override void Cast(in SwarmSongContext context)
        {
            if (IsAimed && context.Aim != null)
            {
                context.Aim.Choose(AimRule, AimRange * context.SizeScale, out ISwarmTarget target, out Vector3 point);
                SwarmSongContext aimed = context.AimedAt(target, point);
                context.Forms.StartSong(this, in aimed);
                return;
            }

            context.Forms.StartSong(this, in context);
        }

        /// <summary>Makes the song's rule. The slot engine calls it once, then starts the same rule afresh on every cast.</summary>
        protected internal abstract SongRule CreateRule();
    }
}
