using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Draws every creature and minion with its shadow, and a health bar on anything hurt. Fighters walk,
    /// lunge, flash and breathe from what the battle says they just did, so nothing here keeps its own state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Lab/Lab Units View")]
    public sealed class LabUnitsView : MonoBehaviour
    {
        private const int ShadowOrder = 2;
        private const int MinionOrder = 3;
        private const int CreatureOrder = 4;
        private const int BarOrder = 6;
        private const int PhaseSteps = 1024;

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
            bool isWalking = fighter.SecondsSinceStep < _art.StillSeconds;
            Sprite sprite = PictureOf(fighter, isWalking);
            Quaternion turn = Quaternion.Euler(0f, 0f, fighter.Facing);

            unit.Body.enabled = true;
            unit.Body.sprite = sprite;
            unit.Body.color = Color.Lerp(look.Color * TintOf(fighter), _art.HurtColor, FighterPose.Flash(fighter.SecondsSinceHurt, _art.HurtSeconds));
            unit.Body.transform.SetPositionAndRotation(fighter.Position + LungeOf(fighter), turn);
            unit.Body.transform.localScale = Vector3.one * (look.Size / sprite.bounds.size.x * GrowthOf(fighter, isWalking));

            DrawShadow(fighter, unit.Shadow, turn);
            DrawBar(fighter, unit);
        }

        // A fighter with no walk pictures glides on its standing one.
        private Sprite PictureOf(Fighter fighter, bool isWalking)
        {
            FighterLook look = fighter.Look;

            if (look.Sprite == null)
            {
                return SpriteOf(look.Shape);
            }

            IReadOnlyList<Sprite> frames = look.WalkFrames;

            if (!isWalking || frames.Count == 0)
            {
                return look.Sprite;
            }

            return frames[FighterPose.WalkFrame(fighter.DistanceWalked, look.Size * _art.StrideBodyLengths, frames.Count)];
        }

        // Healers never hit, so instead of lunging they swell as they heal.
        private Vector2 LungeOf(Fighter fighter)
        {
            if (fighter.Stats.Traits.Heals)
            {
                return Vector2.zero;
            }

            float reach = FighterPose.Lunge(fighter.SecondsSinceStrike, _art.LungeSeconds) * _art.LungeBodyLengths * fighter.Look.Size;
            float angle = fighter.Facing * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
        }

        private float GrowthOf(Fighter fighter, bool isWalking)
        {
            float swell = fighter.Stats.Traits.Heals ? FighterPose.Lunge(fighter.SecondsSinceStrike, _art.LungeSeconds) * _art.HealPulseShare : 0f;
            float breath = isWalking ? 0f : FighterPose.Breath(_battle.ElapsedSeconds, PhaseOf(fighter), _art.BreathSeconds) * _art.BreathShare;
            return (1f + swell) * (1f + breath);
        }

        // Each fighter's own number keeps its breath steady from frame to frame, without remembering anything.
        private static float PhaseOf(Fighter fighter)
        {
            return (RuntimeHelpers.GetHashCode(fighter) & (PhaseSteps - 1)) / (float)PhaseSteps;
        }

        // The shadow turns with its fighter, but always falls the same way, because the light does not move.
        private void DrawShadow(Fighter fighter, SpriteRenderer shadow, Quaternion turn)
        {
            float length = fighter.Look.Size;
            shadow.enabled = true;
            shadow.transform.SetPositionAndRotation(fighter.Position + (_art.ShadowOffset * length), turn);
            shadow.transform.localScale = new Vector3(_art.ShadowBodyLengths.x * length, _art.ShadowBodyLengths.y * length, 1f) / shadow.sprite.bounds.size.x;
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
            SpriteRenderer shadow = NewSprite("Shadow", ShadowOrder, _art.ShadowColor);
            SpriteRenderer back = NewSprite("Bar Back", BarOrder, _art.BarBackColor);
            SpriteRenderer fill = NewSprite("Bar Fill", BarOrder + 1, barColor);
            shadow.sprite = _art.ShadowSprite;
            back.sprite = _sprites.BlockSprite;
            fill.sprite = _sprites.BlockSprite;
            return new UnitSprites(NewSprite("Fighter", order, Color.white), shadow, back, fill);
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
            public UnitSprites(SpriteRenderer body, SpriteRenderer shadow, SpriteRenderer back, SpriteRenderer fill)
            {
                Body = body;
                Shadow = shadow;
                Back = back;
                Fill = fill;
            }

            public SpriteRenderer Body { get; }
            public SpriteRenderer Shadow { get; }
            public SpriteRenderer Back { get; }
            public SpriteRenderer Fill { get; }

            public void Hide()
            {
                Body.enabled = false;
                Shadow.enabled = false;
                Back.enabled = false;
                Fill.enabled = false;
            }
        }
    }
}
