using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class SimplePoolTests
    {
        // SimplePool is abstract, as every real pool names its own type; this one pools plain Transforms.
        private sealed class TransformPool : SimplePool<Transform>
        {
        }

        // A pool that makes one more copy when it runs out, as the mystery box pool does.
        private sealed class GrowingTransformPool : SimplePool<Transform>
        {
            protected override bool GrowsWhenEmpty => true;
        }

        [Test]
        public void AFixedPool_ReturnsNullWhenEmpty()
        {
            _pool.Spawn(Vector3.zero);
            _pool.Spawn(Vector3.zero);
            _pool.Spawn(Vector3.zero);

            Assert.That(_pool.Spawn(Vector3.zero), Is.Null);
        }

        [Test]
        public void AGrowingPool_MakesAnotherWhenEmpty()
        {
            var poolObject = new GameObject("Growing Pool");
            GrowingTransformPool pool = poolObject.AddComponent<GrowingTransformPool>();
            pool.Initialize(_template.transform, 1);

            Transform first = pool.Spawn(Vector3.zero);
            Transform second = pool.Spawn(Vector3.one);

            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.parent, Is.EqualTo(poolObject.transform));
            Assert.That(pool.Active.Count, Is.EqualTo(2));
            Object.DestroyImmediate(poolObject);
        }

        private GameObject _poolObject;
        private GameObject _template;
        private TransformPool _pool;

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("Thing");
            _poolObject = new GameObject("Pool");
            _pool = _poolObject.AddComponent<TransformPool>();
            _pool.Initialize(_template.transform, 3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_poolObject);
            Object.DestroyImmediate(_template);
        }

        [Test]
        public void Initialize_MakesInactiveChildrenOfThePool()
        {
            Assert.That(_poolObject.transform.childCount, Is.EqualTo(3));
            Assert.That(_pool.Available, Is.EqualTo(3));

            foreach (Transform child in _poolObject.transform)
            {
                Assert.That(child.gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void Spawn_ActivatesOneAtThePositionAndTakesItFromThePool()
        {
            Transform thing = _pool.Spawn(new Vector3(1f, 2f, 0f));

            Assert.That(thing.gameObject.activeSelf, Is.True);
            Assert.That(thing.position, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(thing.parent, Is.EqualTo(_poolObject.transform));
            Assert.That(_pool.Available, Is.EqualTo(2));
            Assert.That(_pool.Active, Has.Member(thing));
        }

        [Test]
        public void Despawn_DeactivatesItAndPutsItBack()
        {
            Transform thing = _pool.Spawn(Vector3.zero);

            _pool.Despawn(thing);

            Assert.That(thing.gameObject.activeSelf, Is.False);
            Assert.That(_pool.Available, Is.EqualTo(3));
            Assert.That(_pool.Active, Is.Empty);
        }

        [Test]
        public void Despawn_Twice_PutsItBackOnce()
        {
            Transform thing = _pool.Spawn(Vector3.zero);

            _pool.Despawn(thing);
            _pool.Despawn(thing);

            Assert.That(_pool.Available, Is.EqualTo(3));
        }

        [Test]
        public void Spawn_WhenEmpty_ReturnsNullAndMakesNoMore()
        {
            _pool.Spawn(Vector3.zero);
            _pool.Spawn(Vector3.zero);
            _pool.Spawn(Vector3.zero);

            Assert.That(_pool.Spawn(Vector3.zero), Is.Null);
            Assert.That(_poolObject.transform.childCount, Is.EqualTo(3));
        }

        [Test]
        public void DespawnAll_PutsEveryActiveOneBack()
        {
            _pool.Spawn(Vector3.zero);
            _pool.Spawn(Vector3.zero);

            _pool.DespawnAll();

            Assert.That(_pool.Available, Is.EqualTo(3));
            Assert.That(_pool.Active, Is.Empty);
        }
    }
}
