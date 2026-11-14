using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The Fury meter that fills as the swarm deals damage and powers the Colossus.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Fury Config", fileName = "FuryConfig")]
    public sealed class FuryConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Fury needed for the Colossus.")]
        public float Max { get; private set; } = 100f;

        // 1 point per 8.5 damage, so the giant stays a highlight. Plan section I asked for 3, and the playtest bot settled on
        // 4 while the giant stood 11 s; with the giant of 20 to 30 s it set 7, and with the giant's hands (songs-v2 R), which
        // add a fifth of all the damage, 8.5, so the giant deals about 40% of the damage in the wins and the songs, and the
        // wasps they cost, still matter. 11.5 once the Sting became the weakest attack (songs-v2 V): the waves it no longer
        // clears were left to the giant, which rose to 46%; at 11.5 it deals about 40% again.
        [field: SerializeField, Min(0.01f), Tooltip("Damage that earns one point of Fury.")]
        public float DamagePerFuryPoint { get; private set; } = 11.5f;

        // None, by the playtest bot: a giant of 20 to 30 s filled the meter again by itself (about 600 damage, 150 points at
        // 1 per 4) and stood through nearly every wave.
        [field: SerializeField, Range(0f, 1f), Tooltip("Share of the giant's own damage that fills Fury: 0 none, 1 all of it, as the songs' damage does.")]
        public float ColossusDamageShare { get; private set; }
    }
}
