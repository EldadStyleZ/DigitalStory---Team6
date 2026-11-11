using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The Fury meter that fills as the swarm deals damage and powers the Colossus.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Fury Config", fileName = "FuryConfig")]
    public sealed class FuryConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Fury needed for the Colossus.")]
        public float Max { get; private set; } = 100f;

        [field: SerializeField, Min(0.01f), Tooltip("Damage that earns one point of Fury.")]
        public float DamagePerFuryPoint { get; private set; } = 2f;
    }
}
