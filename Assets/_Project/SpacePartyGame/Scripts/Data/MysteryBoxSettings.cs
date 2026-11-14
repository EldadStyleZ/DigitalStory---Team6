using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>Every number for mystery boxes: how often they appear, how long they last, how they move and sound.</summary>
    [CreateAssetMenu(fileName = "MysteryBoxSettings", menuName = "Space Party/Mystery Box Settings")]
    public sealed class MysteryBoxSettings : ScriptableObject
    {
        [Header("Spawning (only while a round is played)")]
        [SerializeField, Min(0.1f), Tooltip("The average time between one box and the next, in seconds.")]
        private float _averageSecondsBetweenSpawns = 5f;

        [SerializeField, Min(0f), Tooltip("The shortest time between one box and the next, in seconds. Waits are often a little longer, now and then much longer.")]
        private float _minimumSecondsBetweenSpawns = 2f;

        [SerializeField, Min(0), Tooltip("How many boxes the pool makes up front. It makes more if they are all out at once.")]
        private int _poolSize = 4;

        [Header("Life")]
        [SerializeField, Min(0f), Tooltip("How long a box sits before it starts flashing, in seconds.")]
        private float _idleSeconds = 6f;

        [SerializeField, Min(0f), Tooltip("How long it flashes before it disappears, in seconds.")]
        private float _flashSeconds = 2f;

        [SerializeField, Min(0.1f), Tooltip("How many times a second it blinks while flashing.")]
        private float _flashesPerSecond = 5f;

        [Header("Idle motion")]
        [SerializeField, Tooltip("How fast it spins around the up axis, in degrees per second.")]
        private float _spinDegreesPerSecond = 120f;

        [SerializeField, Min(0f), Tooltip("How far it bobs up and down, in units.")]
        private float _bobHeight = 0.06f;

        [SerializeField, Min(0.1f), Tooltip("How long one bob up and down takes, in seconds.")]
        private float _bobSeconds = 1.8f;

        [Header("Collecting")]
        [SerializeField, Tooltip("The sound played when a player collects a box.")]
        private AudioClip _collectSound;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the collect sound is.")]
        private float _collectVolume = 0.8f;

        public float AverageSecondsBetweenSpawns => _averageSecondsBetweenSpawns;
        public float MinimumSecondsBetweenSpawns => _minimumSecondsBetweenSpawns;
        public int PoolSize => _poolSize;
        public float IdleSeconds => _idleSeconds;
        public float FlashSeconds => _flashSeconds;
        public float FlashesPerSecond => _flashesPerSecond;
        public float SpinDegreesPerSecond => _spinDegreesPerSecond;
        public float BobHeight => _bobHeight;
        public float BobSeconds => _bobSeconds;
        public AudioClip CollectSound => _collectSound;
        public float CollectVolume => _collectVolume;
    }
}
