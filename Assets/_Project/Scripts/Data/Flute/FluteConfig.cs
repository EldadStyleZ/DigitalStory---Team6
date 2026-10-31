using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>What each flute hole does and how far its music carries.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Flute Config", fileName = "FluteConfig")]
    public sealed class FluteConfig : ScriptableObject
    {
        [Header("Holes")]
        [SerializeField, Tooltip("The mode of each hole, from hole 1 upward.")]
        private FluteMode[] _holeModes =
        {
            FluteMode.Call,
            FluteMode.Call,
            FluteMode.March,
            FluteMode.March,
            FluteMode.Scare,
            FluteMode.Scare,
        };

        [field: Header("Music range")]
        [field: SerializeField, Min(0f), Tooltip("How far the Call mode reaches.")]
        public float CallRadius { get; private set; } = 240f;

        [field: SerializeField, Min(0f), Tooltip("How far the March mode reaches.")]
        public float MarchRadius { get; private set; } = 190f;

        [field: SerializeField, Min(0f), Tooltip("How far the Scare mode reaches.")]
        public float ScareRadius { get; private set; } = 200f;

        [field: Header("March")]
        [field: SerializeField, Min(0f), Tooltip("How far ahead of the Piper marching rats head.")]
        public float MarchDistance { get; private set; } = 150f;

        [field: SerializeField, Min(0f), Tooltip("Distance at which marching rats slow down.")]
        public float MarchStopDistance { get; private set; } = 10f;

        [field: Header("Scare")]
        [field: SerializeField, Min(0f), Tooltip("Strength of the push right beside the Piper.")]
        public float ScareStrength { get; private set; } = 700f;

        [field: SerializeField, Min(0f), Tooltip("Charm lost per second inside the scare.")]
        public float ScareCharmLossPerSecond { get; private set; } = 3f;

        public int HoleCount => _holeModes.Length;

        public FluteMode ModeOfHole(int hole) => _holeModes[hole];

        public float RadiusOf(FluteMode mode) => mode switch
        {
            FluteMode.Call => CallRadius,
            FluteMode.March => MarchRadius,
            FluteMode.Scare => ScareRadius,
            _ => 0f,
        };
    }
}
