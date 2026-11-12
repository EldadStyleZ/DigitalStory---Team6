using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// One hit a song landed on a beekeeper, or one blow a shield blocked, for the feel of each hit (plan section O): the
    /// beekeeper's flash, the number that rises, the camera's shake, the swarm's short freeze on a big hit, and the sound.
    /// The session raises one for each (<see cref="HiveDefenseSession.SongLanded"/>).
    /// </summary>
    public readonly struct SongHit
    {
        /// <summary>Makes a hit.</summary>
        public SongHit(Vector3 position, float damage, bool isKill, bool isBlocked, SwarmForm form, bool isHeavy)
        {
            Position = position;
            Damage = damage;
            IsKill = isKill;
            IsBlocked = isBlocked;
            Form = form;
            IsHeavy = isHeavy;
        }

        /// <summary>Where it landed: the beekeeper hit, or, for a blocked blow, what the shield guarded (the core or the hive).</summary>
        public Vector3 Position { get; }

        /// <summary>
        /// The damage the song dealt. For a blocked blow, the harm the shield stopped: wasps for a blow on the core, health
        /// for a blow on a hive.
        /// </summary>
        public float Damage { get; }

        /// <summary>True when the hit killed the beekeeper. Never for a blocked blow.</summary>
        public bool IsKill { get; }

        /// <summary>True for a beekeeper's blow that a shield (the Dome or the Honeycomb) blocked, on the core or on a hive.</summary>
        public bool IsBlocked { get; }

        /// <summary>The song that hit, or the shield that blocked. The giant's blows say <see cref="SwarmForm.Colossus"/>.</summary>
        public SwarmForm Form { get; }

        /// <summary>True for the big hits (the Meteor, the Lance, the Hydra, the Nest, the Tornado's throw and the giant's blows), on which the swarm's motion freezes for a moment.</summary>
        public bool IsHeavy { get; }
    }
}
