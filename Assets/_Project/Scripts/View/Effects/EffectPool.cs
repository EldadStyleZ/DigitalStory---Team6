using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DigitalStory.View
{
    /// <summary>Runs short-lived effects on reused sprites, so they create no garbage during a night.</summary>
    public sealed class EffectPool
    {
        private const int ExpectedEffects = 256;

        private readonly Transform _parent;
        private readonly ObjectPool<SpriteRenderer> _sprites;
        private readonly List<RunningEffect> _running = new List<RunningEffect>(ExpectedEffects);

        /// <param name="parent">Where the sprites live in the scene.</param>
        public EffectPool(Transform parent)
        {
            _parent = parent;
            _sprites = new ObjectPool<SpriteRenderer>(
                NewSprite,
                actionOnGet: sprite => sprite.enabled = true,
                actionOnRelease: sprite => sprite.enabled = false,
                actionOnDestroy: sprite => Object.Destroy(sprite.gameObject),
                collectionCheck: false,
                defaultCapacity: ExpectedEffects);
        }

        /// <summary>How many effects are on screen.</summary>
        public int ActiveCount => _running.Count;

        /// <summary>How many sprites have ever been made.</summary>
        public int MadeCount { get; private set; }

        public void Spawn(in EffectSpawn spawn)
        {
            SpriteRenderer sprite = _sprites.Get();
            sprite.sprite = spawn.Sprite;
            sprite.sortingOrder = spawn.Order;

            var effect = new RunningEffect(in spawn, sprite);
            Draw(in effect);
            _running.Add(effect);
        }

        /// <summary>Moves every effect on by one step and recycles the finished ones.</summary>
        public void Tick(float deltaTime)
        {
            for (int index = _running.Count - 1; index >= 0; index--)
            {
                RunningEffect effect = _running[index];
                effect.Advance(deltaTime);

                if (effect.IsFinished)
                {
                    Finish(index, effect.Sprite);
                    continue;
                }

                _running[index] = effect;
                Draw(in effect);
            }
        }

        private static void Draw(in RunningEffect effect)
        {
            Transform sprite = effect.Sprite.transform;
            sprite.position = TownSpace.ToScene(effect.Position);

            float width = TownSpace.ToSceneLength(effect.Size);
            sprite.localScale = new Vector3(width, width * effect.Flatness, 1f);
            effect.Sprite.color = effect.CurrentColor;
        }

        // The last effect moves into the gap, which is cheaper than closing it and order does not matter.
        private void Finish(int index, SpriteRenderer sprite)
        {
            int last = _running.Count - 1;
            _running[index] = _running[last];
            _running.RemoveAt(last);
            _sprites.Release(sprite);
        }

        private SpriteRenderer NewSprite()
        {
            MadeCount++;
            var effect = new GameObject("Effect");
            effect.transform.SetParent(_parent, false);
            return effect.AddComponent<SpriteRenderer>();
        }

        private struct RunningEffect
        {
            private readonly Vector2 _velocity;
            private readonly float _growth;
            private readonly Color _color;
            private readonly float _fadeSeconds;

            private float _secondsLeft;

            public RunningEffect(in EffectSpawn spawn, SpriteRenderer sprite)
            {
                Sprite = sprite;
                Position = spawn.Position;
                Size = spawn.Size;
                Flatness = spawn.Flatness;
                _velocity = spawn.Velocity;
                _growth = spawn.Growth;
                _color = spawn.Color;
                _fadeSeconds = spawn.FadeSeconds;
                _secondsLeft = spawn.Seconds;
            }

            public SpriteRenderer Sprite { get; }
            public Vector2 Position { get; private set; }
            public float Size { get; private set; }
            public float Flatness { get; }

            public readonly bool IsFinished => _secondsLeft <= 0f;

            public readonly Color CurrentColor
            {
                get
                {
                    Color color = _color;
                    color.a *= Mathf.Clamp01(_secondsLeft / _fadeSeconds);
                    return color;
                }
            }

            public void Advance(float deltaTime)
            {
                _secondsLeft -= deltaTime;
                Position += _velocity * deltaTime;
                Size += _growth * deltaTime;
            }
        }
    }
}
