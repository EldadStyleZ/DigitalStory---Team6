using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One optional impact effect: the particle prefab, its size, how many can show at once and how they are spaced.</summary>
    [Serializable]
    public struct ImpactFxSlot
    {
        [SerializeField, Tooltip("The particle prefab. Empty means the moment keeps only the game's own effect.")]
        private GameObject _prefab;

        [SerializeField, Min(0.01f), Tooltip("The prefab's size, times its own.")]
        private float _scale;

        [SerializeField, Min(1), Tooltip("How many can show at once. All are made at load; a new one reuses the oldest.")]
        private int _mostAtOnce;

        [SerializeField, Min(0f), Tooltip("The fewest seconds between two of them, which thins out a burst. 0 lets every one play.")]
        private float _leastGapSeconds;

        [SerializeField, Tooltip("Metres above the moment's point it plays: a hit at a beekeeper's chest, a landing on the ground.")]
        private float _heightMetres;

        public ImpactFxSlot(GameObject prefab, float scale, int mostAtOnce, float leastGapSeconds, float heightMetres)
        {
            _prefab = prefab;
            _scale = scale;
            _mostAtOnce = mostAtOnce;
            _leastGapSeconds = leastGapSeconds;
            _heightMetres = heightMetres;
        }

        public GameObject Prefab => _prefab;
        public float Scale => _scale;
        public int MostAtOnce => _mostAtOnce;
        public float LeastGapSeconds => _leastGapSeconds;
        public float HeightMetres => _heightMetres;

        /// <summary>True when the slot has a prefab.</summary>
        public bool IsSet => _prefab != null;
    }
}
