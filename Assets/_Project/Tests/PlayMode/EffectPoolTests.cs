using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class EffectPoolTests
    {
        private const float Tolerance = 0.001f;

        private GameObject _parent;
        private EffectPool _pool;

        [SetUp]
        public void CreatePool()
        {
            _parent = new GameObject("Test Effects");
            _pool = new EffectPool(_parent.transform);
        }

        [TearDown]
        public void DestroyPool() => Object.DestroyImmediate(_parent);

        [Test]
        public void Spawn_NewEffect_IsActive()
        {
            _pool.Spawn(OneSecondEffect());

            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_BeforeItsTime_KeepsTheEffect()
        {
            _pool.Spawn(OneSecondEffect());

            _pool.Tick(0.5f);

            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterItsTime_RecyclesTheEffect()
        {
            _pool.Spawn(OneSecondEffect());

            _pool.Tick(1.1f);

            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Spawn_AfterOneFinished_ReusesItsSprite()
        {
            _pool.Spawn(OneSecondEffect());
            _pool.Tick(1.1f);

            _pool.Spawn(OneSecondEffect());

            Assert.That(_pool.MadeCount, Is.EqualTo(1));
        }

        [Test]
        public void Spawn_TwoAtOnce_MakesTwoSprites()
        {
            _pool.Spawn(OneSecondEffect());
            _pool.Spawn(OneSecondEffect());

            Assert.That(_pool.MadeCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_OneOfManyFinishes_KeepsTheOthers()
        {
            _pool.Spawn(OneSecondEffect());
            _pool.Spawn(new EffectSpawn(null, 0, Vector2.zero, Vector2.zero, Color.white, 10f, 0f, 1f, 5f, 1f));

            _pool.Tick(1.1f);

            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_MovingEffect_FollowsItsVelocityAndGrows()
        {
            _pool.Spawn(new EffectSpawn(null, 0, Vector2.zero, Vector2.right * 100f, Color.white, 100f, 100f, 1f, 5f, 1f));

            _pool.Tick(1f);

            Transform sprite = _parent.transform.GetChild(0);
            Assert.That(sprite.position.x, Is.EqualTo(TownSpace.ToScene(Vector2.right * 100f).x).Within(Tolerance));
            Assert.That(sprite.localScale.x, Is.EqualTo(TownSpace.ToSceneLength(200f)).Within(Tolerance));
        }

        [Test]
        public void Tick_FlatEffect_StaysFlat()
        {
            _pool.Spawn(new EffectSpawn(null, 0, Vector2.zero, Vector2.zero, Color.white, 100f, 0f, 0.6f, 5f, 1f));

            _pool.Tick(0.1f);

            Transform sprite = _parent.transform.GetChild(0);
            Assert.That(sprite.localScale.y, Is.EqualTo(sprite.localScale.x * 0.6f).Within(Tolerance));
        }

        [Test]
        public void Tick_NearTheEnd_FadesOut()
        {
            var halfVisible = new Color(1f, 1f, 1f, 0.8f);
            _pool.Spawn(new EffectSpawn(null, 0, Vector2.zero, Vector2.zero, halfVisible, 10f, 0f, 1f, 2f, 1f));
            SpriteRenderer sprite = _parent.GetComponentInChildren<SpriteRenderer>();

            _pool.Tick(0.5f);
            Assert.That(sprite.color.a, Is.EqualTo(0.8f).Within(Tolerance), "Still at full strength.");

            _pool.Tick(1f);
            Assert.That(sprite.color.a, Is.EqualTo(0.4f).Within(Tolerance), "Half way through its fade.");
        }

        [Test]
        public void Tick_ManyEffects_AllocatesNothing()
        {
            for (int index = 0; index < 100; index++)
            {
                _pool.Spawn(new EffectSpawn(null, 0, Vector2.zero, Vector2.up, Color.white, 10f, 1f, 1f, 60f, 1f));
            }

            Assert.That(() => _pool.Tick(0.01f), Is.Not.AllocatingGCMemory());
        }

        private static EffectSpawn OneSecondEffect()
        {
            return new EffectSpawn(null, 0, Vector2.zero, Vector2.zero, Color.white, 10f, 0f, 1f, 1f, 1f);
        }
    }
}
