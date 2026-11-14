using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A pool of objects made once, up front, so nothing is created or destroyed during play. The objects live as
    /// inactive children of the pool; <see cref="Spawn"/> takes one off the stack and activates it, and
    /// <see cref="Despawn"/> deactivates it and pushes it back on the stack.
    /// </summary>
    /// <typeparam name="T">The component on the pooled prefab's root, such as <see cref="Vermin"/>.</typeparam>
    /// <remarks>Unity cannot add a generic component, so each pool is a small subclass, such as <see cref="VerminPool"/>.</remarks>
    public abstract class SimplePool<T> : MonoBehaviour where T : Component
    {
        [SerializeField, Tooltip("The prefab to copy into the pool.")]
        private T _prefab;

        [SerializeField, Min(0), Tooltip("How many copies the pool makes when it starts.")]
        private int _size = 50;

        private readonly Stack<T> _pool = new Stack<T>();
        private readonly List<T> _active = new List<T>();
        private bool _isFilled;
        private int _made;

        /// <summary>How many objects are waiting in the pool.</summary>
        public int Available => _pool.Count;

        /// <summary>The objects spawned and not yet despawned.</summary>
        public IReadOnlyList<T> Active => _active;

        /// <summary>How many copies to make. A subclass may take it from its settings instead.</summary>
        protected virtual int Size => _size;

        /// <summary>Whether to make one more copy when every one is out, instead of returning null. Off by default.</summary>
        protected virtual bool GrowsWhenEmpty => false;

        /// <summary>Sets the prefab and size and fills the pool, for tests and pools built from code.</summary>
        public void Initialize(T prefab, int size)
        {
            _prefab = prefab;
            _size = size;
            Fill();
        }

        /// <summary>
        /// Takes an object from the pool, puts it at a position and activates it. If the pool is empty it returns
        /// null, or first makes one more when the pool grows.
        /// </summary>
        public T Spawn(Vector3 position)
        {
            Fill();

            if (_pool.Count == 0 && GrowsWhenEmpty && _prefab != null)
            {
                _pool.Push(CreateOne());
            }

            if (_pool.Count == 0)
            {
                return null;
            }

            T item = _pool.Pop();
            item.transform.position = position;
            item.gameObject.SetActive(true);
            _active.Add(item);
            OnSpawned(item);
            return item;
        }

        /// <summary>Deactivates an object and puts it back in the pool. Objects already in the pool are ignored.</summary>
        public void Despawn(T item)
        {
            if (item == null || !_active.Remove(item))
            {
                return;
            }

            OnDespawned(item);
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }

        /// <summary>Puts every spawned object back in the pool.</summary>
        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Despawn(_active[i]);
            }
        }

        protected virtual void Awake()
        {
            Fill();
        }

        /// <summary>Called once for each copy as the pool makes it.</summary>
        protected virtual void OnCreated(T item)
        {
        }

        /// <summary>Called after an object is activated by <see cref="Spawn"/>.</summary>
        protected virtual void OnSpawned(T item)
        {
        }

        /// <summary>Called before an object is deactivated by <see cref="Despawn"/>.</summary>
        protected virtual void OnDespawned(T item)
        {
        }

        private void Fill()
        {
            if (_isFilled)
            {
                return;
            }

            if (_prefab == null)
            {
                Debug.LogError($"{name}: {GetType().Name} has no prefab to pool.", this);
                return;
            }

            _isFilled = true;

            for (int i = 0; i < Size; i++)
            {
                _pool.Push(CreateOne());
            }
        }

        private T CreateOne()
        {
            T item = Instantiate(_prefab, transform);
            item.name = $"{_prefab.name} {++_made}";
            item.gameObject.SetActive(false);
            OnCreated(item);
            return item;
        }
    }
}
