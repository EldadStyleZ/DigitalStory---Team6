using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A kind of enemy in the maze.</summary>
    [Serializable]
    public sealed class MinionType
    {
        [SerializeField, Tooltip("Its name on screen.")]
        private string _name;

        [SerializeField, Min(0f), Tooltip("How far it notices the Piper or a creature, in tiles of walking.")]
        private float _detectTiles;

        [SerializeField, Min(0), Tooltip("Experience for killing it.")]
        private int _xpReward;

        [SerializeField, Range(0f, 1f), Tooltip("How deep it starts to appear: 0 is the entrance and 1 the furthest cell.")]
        private float _firstDepth;

        [SerializeField, Range(0f, 1f), Tooltip("How deep it stops appearing.")]
        private float _lastDepth;

        [SerializeField, Tooltip("What it can do in a fight.")]
        private FighterStats _stats;

        [SerializeField, Tooltip("How it is drawn.")]
        private FighterLook _look;

        public MinionType(string name, float detectTiles, int xpReward, float firstDepth, float lastDepth, FighterStats stats, FighterLook look)
        {
            _name = name;
            _detectTiles = detectTiles;
            _xpReward = xpReward;
            _firstDepth = firstDepth;
            _lastDepth = lastDepth;
            _stats = stats;
            _look = look;
        }

        public string Name => _name;
        public float DetectTiles => _detectTiles;
        public int XpReward => _xpReward;
        public float FirstDepth => _firstDepth;
        public float LastDepth => _lastDepth;
        public FighterStats Stats => _stats;
        public FighterLook Look => _look;
    }
}
