using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Builds the visible town from a generated layout.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Town View")]
    public sealed class TownView : MonoBehaviour
    {
        private const int GroundOrder = 0;
        private const int WaterOrder = 1;
        private const int HouseOrder = 2;

        private TownLayout _layout;
        private TownConfig _town;
        private RiverBank _bank;
        private TownArtConfig _art;

        /// <summary>Supplies what to draw. Call once, before this object starts.</summary>
        public void Initialize(TownLayout layout, TownConfig town, RiverBank bank, TownArtConfig art)
        {
            _layout = layout;
            _town = town;
            _bank = bank;
            _art = art;
        }

        private void Start()
        {
            // GameRoot has already reported the problem if nothing was supplied.
            if (_layout == null)
            {
                return;
            }

            AddBlock("Ground", 0f, 0f, _town.Width, _town.Height, _art.GroundColor, GroundOrder);
            AddRiver();
            AddHouses();
        }

        private void AddRiver()
        {
            float stripHeight = _art.RiverStripHeight;
            float bankWidth = _art.BankWidth;

            for (float y = 0f; y < _town.Height; y += stripHeight)
            {
                float edge = _bank.EdgeAt(y);
                float endOfShallows = edge + _town.ShallowsWidth;

                AddBlock("Bank", edge - bankWidth, y, bankWidth, stripHeight, _art.BankColor, WaterOrder);
                AddBlock("Shallows", edge, y, _town.ShallowsWidth, stripHeight, _art.ShallowsColor, WaterOrder);
                AddBlock("River", endOfShallows, y, _town.Width - endOfShallows, stripHeight, _art.RiverColor, WaterOrder);
            }
        }

        private void AddHouses()
        {
            foreach (House house in _layout.Houses)
            {
                AddBlock("House", house.X, house.Y, house.Width, house.Height, _art.RoofColor(house.RoofStyle), HouseOrder);
            }
        }

        private void AddBlock(string blockName, float x, float y, float width, float height, Color color, int order)
        {
            var block = new GameObject(blockName);
            block.transform.SetParent(transform, false);
            block.transform.position = TownSpace.ToScene(new Vector2(x + (width / 2f), y + (height / 2f)));
            block.transform.localScale = new Vector3(TownSpace.ToSceneLength(width), TownSpace.ToSceneLength(height), 1f);

            var blockRenderer = block.AddComponent<SpriteRenderer>();
            blockRenderer.sprite = _art.BlockSprite;
            blockRenderer.color = color;
            blockRenderer.sortingOrder = order;
        }
    }
}
