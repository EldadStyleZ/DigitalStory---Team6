using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The size of a maze: how many cells it has and how wide its corridors and walls are.</summary>
    [Serializable]
    public struct MazeShape
    {
        [SerializeField, Min(1), Tooltip("Cells from west to east.")]
        private int _columns;

        [SerializeField, Min(1), Tooltip("Cells from south to north.")]
        private int _rows;

        [SerializeField, Min(1), Tooltip("How wide a corridor is, in tiles.")]
        private int _corridorTiles;

        [SerializeField, Min(1), Tooltip("How thick a wall is, in tiles.")]
        private int _wallTiles;

        [SerializeField, Range(0f, 1f), Tooltip("The chance of opening each remaining wall, which adds loops and shortcuts.")]
        private float _loopChance;

        public MazeShape(int columns, int rows, int corridorTiles, int wallTiles, float loopChance)
        {
            _columns = columns;
            _rows = rows;
            _corridorTiles = corridorTiles;
            _wallTiles = wallTiles;
            _loopChance = loopChance;
        }

        public int Columns => _columns;
        public int Rows => _rows;
        public int CorridorTiles => _corridorTiles;
        public int WallTiles => _wallTiles;
        public float LoopChance => _loopChance;

        /// <summary>The distance in tiles from one cell to the next.</summary>
        public int CellPitch => _corridorTiles + _wallTiles;

        public int CellCount => _columns * _rows;
        public int TileColumns => (_columns * CellPitch) + _wallTiles;
        public int TileRows => (_rows * CellPitch) + _wallTiles;
    }
}
