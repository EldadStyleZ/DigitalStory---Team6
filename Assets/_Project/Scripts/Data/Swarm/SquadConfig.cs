using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The swarm's three squads: the band of wasp seeds (0 to 1) each song slot may take, and how long a slot's wasps take
    /// to fly home after its song lets go. The bands never move and never overlap, so no wasp ever jumps between two
    /// running songs. Seeds above every band always stay river.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Squad Config", fileName = "SquadConfig")]
    public sealed class SquadConfig : ScriptableObject
    {
        [field: Header("Post: something left on the map")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Lowest wasp seed of the Post band.")]
        public float PostLow { get; private set; } = 0f;

        [field: SerializeField, Range(0f, 1f), Tooltip("End of the Post band: its seeds run up to, but not including, this.")]
        public float PostHigh { get; private set; } = 0.2f;

        [field: Header("Guard: a defense around the core")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Lowest wasp seed of the Guard band.")]
        public float GuardLow { get; private set; } = 0.2f;

        [field: SerializeField, Range(0f, 1f), Tooltip("End of the Guard band: its seeds run up to, but not including, this.")]
        public float GuardHigh { get; private set; } = 0.55f;

        [field: Header("Strike: an attack")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Lowest wasp seed of the Strike band.")]
        public float StrikeLow { get; private set; } = 0.55f;

        [field: SerializeField, Range(0f, 1f), Tooltip("End of the Strike band: its seeds run up to, but not including, this.")]
        public float StrikeHigh { get; private set; } = 0.92f;

        [field: Header("Return")]
        [field: SerializeField, Min(0f), Tooltip("Seconds a slot's wasps fly home after its song lets go. Only a picture: it hurts nothing, and a cast replaces it.")]
        public float ReturnSeconds { get; private set; } = 0.6f;
    }
}
