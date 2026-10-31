using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The numbers that define the night.</summary>
    [Serializable]
    public struct NightRules
    {
        [SerializeField, Tooltip("Decides the town layout and where rats start.")]
        private int _seed;

        [SerializeField, Min(1), Tooltip("Rats in the town at the start.")]
        private int _ratCount;

        [SerializeField, Min(1), Tooltip("Rats to drown to win.")]
        private int _quota;

        [SerializeField, Min(1f), Tooltip("Seconds until dawn.")]
        private float _timeLimit;

        [SerializeField, Min(1), Tooltip("How many nests the rats start in.")]
        private int _nestCount;

        [SerializeField, Min(0), Tooltip("Cats hunting in the town.")]
        private int _catCount;

        public NightRules(int seed, int ratCount, int quota, float timeLimit, int nestCount, int catCount = 0)
        {
            _seed = seed;
            _ratCount = ratCount;
            _quota = quota;
            _timeLimit = timeLimit;
            _nestCount = nestCount;
            _catCount = catCount;
        }

        public int Seed => _seed;
        public int RatCount => _ratCount;
        public int Quota => _quota;
        public float TimeLimit => _timeLimit;
        public int NestCount => _nestCount;
        public int CatCount => _catCount;
    }
}
