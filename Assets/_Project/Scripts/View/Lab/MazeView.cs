using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DigitalStory.View
{
    /// <summary>
    /// Draws the maze and its arrows as tilemaps, because a sprite for each tile would cost thousands of
    /// objects.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grid))]
    [AddComponentMenu("DigitalStory/Lab/Maze View")]
    public sealed class MazeView : MonoBehaviour
    {
        private const int ArrowOrder = 1;
        private const float DegreesPerDirection = 90f;

        private Maze _maze;
        private Signposts _signposts;
        private TownArtConfig _sprites;
        private LabArtConfig _art;
        private Tile _wallTile;
        private Tile _floorTile;
        private Tile _arrowTile;

        /// <summary>The drawn tiles, for tests and debugging.</summary>
        public Tilemap Tiles { get; private set; }

        /// <summary>The drawn arrows, for tests and debugging.</summary>
        public Tilemap Arrows { get; private set; }

        /// <summary>Supplies what to draw. Call once, before this object starts.</summary>
        public void Initialize(Maze maze, Signposts signposts, TownArtConfig sprites, LabArtConfig art)
        {
            _maze = maze;
            _signposts = signposts;
            _sprites = sprites;
            _art = art;
        }

        /// <summary>The tile an arrow sits on: the middle of its cell.</summary>
        public static Vector3Int ArrowTileOf(Maze maze, int cell)
        {
            Vector2 center = maze.CenterOf(cell);
            return new Vector3Int(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y), 0);
        }

        private void Start()
        {
            // LabRoot has already reported the problem if nothing was supplied.
            if (_maze == null)
            {
                return;
            }

            _wallTile = NewTile(_sprites.BlockSprite, _art.WallColor);
            _floorTile = NewTile(_sprites.BlockSprite, _art.FloorColor);
            _arrowTile = NewTile(_art.ArrowSprite, _art.ArrowColor);

            Tiles = NewLayer("Tiles", 0);
            Arrows = NewLayer("Arrows", ArrowOrder);
            Draw();
            DrawArrows();
            _signposts.Changed += DrawArrows;
        }

        private void OnDestroy()
        {
            // Unity also calls this when the scene unloads before Start ran, so nothing may be drawn yet.
            if (Arrows == null)
            {
                return;
            }

            _signposts.Changed -= DrawArrows;
            Destroy(_wallTile);
            Destroy(_floorTile);
            Destroy(_arrowTile);
        }

        private Tilemap NewLayer(string layerName, int order)
        {
            var layer = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(transform, false);
            layer.GetComponent<TilemapRenderer>().sortingOrder = order;
            return layer.GetComponent<Tilemap>();
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

        private void DrawArrows()
        {
            foreach (int cell in _signposts.Cells)
            {
                DrawArrow(ArrowTileOf(_maze, cell), _signposts.DirectionAt(cell));
            }
        }

        private void DrawArrow(Vector3Int tile, int direction)
        {
            if (direction == Signposts.NoDirection)
            {
                Arrows.SetTile(tile, null);
                return;
            }

            Quaternion turn = Quaternion.Euler(0f, 0f, direction * DegreesPerDirection);
            Arrows.SetTile(tile, _arrowTile);
            Arrows.SetTransformMatrix(tile, Matrix4x4.TRS(Vector3.zero, turn, Vector3.one * _art.ArrowTiles));
        }

        private static Tile NewTile(Sprite sprite, Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            return tile;
        }
    }
}
