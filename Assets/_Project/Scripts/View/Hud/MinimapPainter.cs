using System;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Draws the minimap into a pixel buffer, in town coordinates.</summary>
    public sealed class MinimapPainter
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _pixelsPerTownUnit;
        private readonly Color32[] _background;

        /// <param name="width">The map's width in pixels.</param>
        /// <param name="townWidth">The town's width, which fills the map.</param>
        /// <param name="townHeight">The town's height.</param>
        public MinimapPainter(int width, float townWidth, float townHeight)
        {
            _width = width;
            _pixelsPerTownUnit = width / townWidth;
            _height = Mathf.CeilToInt(townHeight * _pixelsPerTownUnit);
            _background = new Color32[_width * _height];
            Pixels = new Color32[_width * _height];
        }

        public int Width => _width;
        public int Height => _height;

        /// <summary>The height in town units of one map pixel.</summary>
        public float TownUnitsPerRow => 1f / _pixelsPerTownUnit;

        /// <summary>The finished picture, from the bottom row up, as textures store it.</summary>
        public Color32[] Pixels { get; }

        /// <summary>Paints an area of the unchanging background, such as a house.</summary>
        public void PaintBackground(Rect townArea, Color32 color)
        {
            Fill(_background, ToColumn(townArea.xMin), ToColumn(townArea.xMax), ToRow(townArea.yMin), ToRow(townArea.yMax), color);
        }

        /// <summary>Paints one row of the background, from a town position to the right edge of the map.</summary>
        public void PaintBackgroundRow(float townY, float fromTownX, Color32 color)
        {
            int row = ToRow(townY);

            for (int column = ToColumn(fromTownX); column < _width; column++)
            {
                _background[Index(column, row)] = color;
            }
        }

        /// <summary>Starts a new frame from the background.</summary>
        public void Clear() => Array.Copy(_background, Pixels, Pixels.Length);

        /// <summary>Paints a square centred on a town position.</summary>
        public void PaintDot(Vector2 townPosition, int size, Color32 color)
        {
            int left = ToColumn(townPosition.x) - (size / 2);
            int top = ToRow(townPosition.y) - (size / 2);
            Fill(Pixels, left, left + size, top, top + size, color);
        }

        /// <summary>Paints the outline of an area, clipped to the map.</summary>
        public void PaintFrame(Rect townArea, Color32 color)
        {
            int left = Mathf.Max(ToColumn(townArea.xMin), 0);
            int right = Mathf.Min(ToColumn(townArea.xMax), _width - 1);
            int top = Mathf.Max(ToRow(townArea.yMin), 0);
            int bottom = Mathf.Min(ToRow(townArea.yMax), _height - 1);

            Fill(Pixels, left, right + 1, top, top + 1, color);
            Fill(Pixels, left, right + 1, bottom, bottom + 1, color);
            Fill(Pixels, left, left + 1, top, bottom + 1, color);
            Fill(Pixels, right, right + 1, top, bottom + 1, color);
        }

        /// <summary>Returns the finished pixel at a town position.</summary>
        public Color32 PixelAt(Vector2 townPosition) => Pixels[Index(ToColumn(townPosition.x), ToRow(townPosition.y))];

        private int ToColumn(float townX) => Mathf.FloorToInt(townX * _pixelsPerTownUnit);

        private int ToRow(float townY) => Mathf.Clamp(Mathf.FloorToInt(townY * _pixelsPerTownUnit), 0, _height - 1);

        // Town rows run down the screen, but textures store the bottom row first.
        private int Index(int column, int row) => ((_height - 1 - row) * _width) + column;

        private void Fill(Color32[] target, int left, int right, int top, int bottom, Color32 color)
        {
            int firstColumn = Mathf.Max(left, 0);
            int lastColumn = Mathf.Min(right, _width);
            int firstRow = Mathf.Max(top, 0);
            int lastRow = Mathf.Min(bottom, _height);

            for (int row = firstRow; row < lastRow; row++)
            {
                for (int column = firstColumn; column < lastColumn; column++)
                {
                    target[Index(column, row)] = color;
                }
            }
        }
    }
}
