using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Builds a town from a seed.</summary>
    /// <remarks>
    /// Every random call happens in the prototype's order, including those for roofs and chimneys.
    /// Moving or skipping one changes every town that follows.
    /// </remarks>
    public sealed class TownGenerator
    {
        private readonly TownConfig _config;
        private readonly IRandomSource _random;

        private List<House> _houses;
        private List<Junction> _junctions;
        private List<Vector2> _lamps;

        public TownGenerator(TownConfig config, IRandomSource random)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public TownLayout Generate()
        {
            _houses = new List<House>();
            _junctions = new List<Junction>();
            _lamps = new List<Vector2>();

            int columns = Mathf.FloorToInt((_config.RiverX - _config.RiversideMargin) / _config.BlockWidth);
            int rows = Mathf.CeilToInt(_config.Height / _config.BlockHeight);

            AddBlocks(columns, rows);
            AddJunctions(columns, rows);
            AddRiversideLamps();

            return new TownLayout(columns, rows, _houses, _junctions, _lamps);
        }

        private void AddBlocks(int columns, int rows)
        {
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    if (!Roll(_config.EmptyBlockChance))
                    {
                        AddBlock(column, row);
                    }
                }
            }
        }

        private void AddBlock(int column, int row)
        {
            float halfStreet = _config.StreetWidth / 2f;
            float left = (column * _config.BlockWidth) + halfStreet;
            float top = (row * _config.BlockHeight) + halfStreet;
            float width = _config.BlockWidth - _config.StreetWidth;
            float height = Mathf.Min(_config.BlockHeight - _config.StreetWidth, _config.Height - top - halfStreet);

            if (height < _config.MinimumBlockHeight)
            {
                return;
            }

            double kind = _random.NextDouble();

            if (kind < _config.SideBySideChance)
            {
                AddSideBySideHouses(left, top, width, height);
            }
            else if (kind < _config.SideBySideChance + _config.StackedChance)
            {
                AddStackedHouses(left, top, width, height);
            }
            else
            {
                AddHouse(left, top, width, height);
            }
        }

        private void AddSideBySideHouses(float left, float top, float width, float height)
        {
            float firstWidth = RoundedShare(width, _config.SideBySideMinimumShare, _config.SideBySideExtraShare);
            AddHouse(left, top, firstWidth, height);
            AddHouse(left + firstWidth, top, width - firstWidth, height);
        }

        private void AddStackedHouses(float left, float top, float width, float height)
        {
            float firstHeight = RoundedShare(height, _config.StackedMinimumShare, _config.StackedExtraShare);
            AddHouse(left, top, width, firstHeight);
            AddHouse(left, top + firstHeight, width, height - firstHeight);
        }

        private void AddHouse(float x, float y, float width, float height)
        {
            int roofStyle = (int)Math.Floor(_random.NextDouble() * _config.RoofStyleCount);
            bool hasChimney = Roll(_config.ChimneyChance);
            float chimneyX = (float)_random.NextDouble();
            float chimneyY = (float)_random.NextDouble();
            _houses.Add(new House(x, y, width, height, roofStyle, hasChimney, new Vector2(chimneyX, chimneyY)));
        }

        private void AddJunctions(int columns, int rows)
        {
            float lowestStreet = _config.Height - _config.BottomStreetInset;

            for (int column = 0; column <= columns; column++)
            {
                for (int row = 0; row <= rows; row++)
                {
                    var position = new Vector2(column * _config.BlockWidth, Mathf.Min(row * _config.BlockHeight, lowestStreet));
                    _junctions.Add(new Junction(position, column, row));

                    if (Roll(_config.JunctionLampChance))
                    {
                        _lamps.Add(position + (Vector2.one * _config.JunctionLampOffset));
                    }
                }
            }
        }

        private void AddRiversideLamps()
        {
            float x = _config.RiverX - _config.RiversideLampSetback;

            for (float y = _config.RiversideLampStart; y < _config.Height; y += _config.RiversideLampSpacing)
            {
                _lamps.Add(new Vector2(x, y + ((float)_random.NextDouble() * _config.RiversideLampJitter)));
            }
        }

        private bool Roll(float chance) => _random.NextDouble() < chance;

        // Whole units keep every wall on the same pixel grid as the prototype.
        private float RoundedShare(float length, float minimumShare, float extraShare)
        {
            double share = minimumShare + (_random.NextDouble() * extraShare);
            return (float)Math.Floor((length * share) + 0.5d);
        }
    }
}
