using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DigitalStory.View
{
    /// <summary>Draws the maze as one tilemap, because a sprite for each tile would cost thousands of objects.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grid))]
    [AddComponentMenu("DigitalStory/Lab/Maze View")]
    public sealed class MazeView : MonoBehaviour
    {
        private Maze _maze;
        private TownArtConfig _sprites;
        private LabArtConfig _art;
        private Tile _wallTile;
        private Tile _floorTile;

        /// <summary>The drawn tiles, for tests and debugging.</summary>
        public Tilemap Tiles { get; private set; }

        /// <summary>Supplies what to draw. Call once, before this object starts.</summary>
        public void Initialize(Maze maze, TownArtConfig sprites, LabArtConfig art)
        {
            _maze = maze;
            _sprites = sprites;
            _art = art;
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_maze == null)
            {
                return;
            }

            _wallTile = NewTile(_art.WallColor);
            _floorTile = NewTile(_art.FloorColor);

            var tiles = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tiles.transform.SetParent(transform, false);
            Tiles = tiles.GetComponent<Tilemap>();
            Draw();
        }

        private void OnDestroy()
        {
            Destroy(_wallTile);
            Destroy(_floorTile);
        }

        private void Draw()
        {
            int columns = _maze.Shape.TileColumns;
            int rows = _maze.Shape.TileRows;
            var tiles = new TileBase[columns * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    tiles[x + (y * columns)] = _maze.IsFloor(x, y) ? _floorTile : _wallTile;
                }
            }

            Tiles.SetTilesBlock(new BoundsInt(0, 0, 0, columns, rows, 1), tiles);
        }

        private Tile NewTile(Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = _sprites.BlockSprite;
            tile.color = color;
            return tile;
        }
    }
}
