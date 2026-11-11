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
