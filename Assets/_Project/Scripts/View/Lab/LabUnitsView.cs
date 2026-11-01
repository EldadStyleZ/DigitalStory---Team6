using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Draws every creature and minion, with a health bar on anything hurt.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Lab/Lab Units View")]
    public sealed class LabUnitsView : MonoBehaviour
    {
        private const int MinionOrder = 3;
        private const int CreatureOrder = 4;
        private const int BarOrder = 6;

        private readonly List<UnitSprites> _creatureSprites = new List<UnitSprites>();
        private readonly List<UnitSprites> _minionSprites = new List<UnitSprites>();

        private Battle _battle;
        private TownArtConfig _sprites;
        private LabArtConfig _art;

        /// <summary>How many fighters are drawn, for tests and debugging.</summary>
        public int DrawnCount { get; private set; }

        /// <summary>Supplies what to draw. Call once, before this object updates.</summary>
        public void Initialize(Battle battle, TownArtConfig sprites, LabArtConfig art)
        {
            _battle = battle;
            _sprites = sprites;
            _art = art;
        }

        private void LateUpdate()
        {
            if (_battle == null)
            {
                return;
            }

            DrawnCount = 0;
            Draw(_battle.Creatures, _creatureSprites, CreatureOrder, _art.CreatureBarColor);
            Draw(_battle.Minions, _minionSprites, MinionOrder, _art.MinionBarColor);
        }

        private void Draw<T>(IReadOnlyList<T> fighters, List<UnitSprites> sprites, int order, Color barColor) where T : Fighter
        {
            for (int index = 0; index < fighters.Count; index++)
            {
                if (index == sprites.Count)
                {
                    sprites.Add(NewUnit(order, barColor));
                }

                DrawOne(fighters[index], sprites[index]);
                DrawnCount++;
            }

            for (int index = fighters.Count; index < sprites.Count; index++)
            {
                sprites[index].Hide();
            }
        }

        private void DrawOne(Fighter fighter, UnitSprites unit)
        {
            FighterLook look = fighter.Look;
            Sprite sprite = SpriteOf(look.Shape);

            unit.Body.enabled = true;
            unit.Body.sprite = sprite;
            unit.Body.color = look.Color * TintOf(fighter);
            unit.Body.transform.SetPositionAndRotation(fighter.Position, Quaternion.Euler(0f, 0f, fighter.Facing));
            unit.Body.transform.localScale = Vector3.one * (look.Size / sprite.bounds.size.x);

            DrawBar(fighter, unit);
        }

        private Color TintOf(Fighter fighter)
        {
            if (fighter is Minion minion)
            {
                return minion.IsAwake ? Color.white : _art.SleepingTint;
            }

            return _battle.IsBoosted ? _art.BoostTint : Color.white;
        }

        private void DrawBar(Fighter fighter, UnitSprites unit)
        {
            float share = fighter.Health.Current / fighter.Health.Max;
            bool isHurt = share < 1f;
            unit.Back.enabled = isHurt;
            unit.Fill.enabled = isHurt;

            if (!isHurt)
            {
                return;
            }

            float width = fighter.Look.Size;
            Vector2 middle = fighter.Position + (Vector2.up * width);
            unit.Back.transform.position = middle;
            unit.Back.transform.localScale = new Vector3(width, _art.BarThickness, 1f);
            unit.Fill.transform.position = middle + (Vector2.left * (width * (1f - share) / 2f));
            unit.Fill.transform.localScale = new Vector3(width * share, _art.BarThickness, 1f);
        }

        private Sprite SpriteOf(FighterShape shape)
        {
            switch (shape)
            {
                case FighterShape.Cat:
                    return _sprites.CatSprite;
                case FighterShape.Dot:
                    return _sprites.DotSprite;
                default:
                    return _sprites.RatSprite;
            }
        }

        private UnitSprites NewUnit(int order, Color barColor)
        {
            SpriteRenderer back = NewSprite("Bar Back", BarOrder, _art.BarBackColor);
            SpriteRenderer fill = NewSprite("Bar Fill", BarOrder + 1, barColor);
            back.sprite = _sprites.BlockSprite;
            fill.sprite = _sprites.BlockSprite;
            return new UnitSprites(NewSprite("Fighter", order, Color.white), back, fill);
        }

        private SpriteRenderer NewSprite(string spriteName, int order, Color color)
        {
            var sprite = new GameObject(spriteName).AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(transform, false);
            sprite.sortingOrder = order;
            sprite.color = color;
            return sprite;
        }

        private sealed class UnitSprites
        {
            public UnitSprites(SpriteRenderer body, SpriteRenderer back, SpriteRenderer fill)
            {
                Body = body;
                Back = back;
                Fill = fill;
            }

            public SpriteRenderer Body { get; }
            public SpriteRenderer Back { get; }
            public SpriteRenderer Fill { get; }

            public void Hide()
            {
                Body.enabled = false;
                Back.enabled = false;
                Fill.enabled = false;
            }
        }
    }
}
