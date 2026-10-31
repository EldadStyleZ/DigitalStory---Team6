using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the Piper moves.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Piper Config", fileName = "PiperConfig")]
    public sealed class PiperConfig : ScriptableObject
    {
        [field: Header("Body")]
        [field: SerializeField, Min(0f), Tooltip("The Piper's collision radius.")]
        public float Radius { get; private set; } = 11f;

        [field: SerializeField, Min(0f), Tooltip("Closest the Piper gets to the town edge and the end of the shallows.")]
        public float TownMargin { get; private set; } = 12f;

        [field: Header("Start")]
        [field: SerializeField, Min(0f), Tooltip("Starting distance from the river.")]
        public float StartRiverSetback { get; private set; } = 120f;

        [field: SerializeField, Tooltip("Starting direction in radians. Pi faces away from the river.")]
        public float StartFacing { get; private set; } = Mathf.PI;

        [field: Header("Movement")]
        [field: SerializeField, Min(0f), Tooltip("Speed on land.")]
        public float WalkSpeed { get; private set; } = 150f;

        [field: SerializeField, Min(0f), Tooltip("Speed in the water.")]
        public float WadeSpeed { get; private set; } = 85f;

        [field: SerializeField, Min(0f), Tooltip("How quickly the Piper turns to face the way it walks.")]
        public float TurnRate { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Pointer distance below which the Piper stops following it.")]
        public float PointerDeadZone { get; private set; } = 12f;
    }
}
