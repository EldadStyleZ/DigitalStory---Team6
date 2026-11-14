using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DigitalStory.View
{
    /// <summary>
    /// Plays the optional impact effects of plan section AB: pooled copies of the particle prefabs an
    /// <see cref="ImpactFxLibrary"/> names, every one made when the spawner is made (at load), so play never instantiates or
    /// allocates. Each kind shows at most its slot's number at once and a new effect reuses the oldest (<see cref="ImpactFxPool"/>).
    /// Without a library, or for a kind whose slot is empty, it plays nothing, and the game's own effects are all there is.
    /// </summary>
    public sealed class ImpactFxSpawner
    {
        private readonly KindPool[] _kinds;

        /// <param name="library">The effects; null for none.</param>
        /// <param name="parent">Holds the instances.</param>
        /// <exception cref="ArgumentNullException">The parent is missing.</exception>
        public ImpactFxSpawner(ImpactFxLibrary library, Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            int kindCount = Enum.GetValues(typeof(ImpactFxKind)).Length;
            _kinds = new KindPool[kindCount];
            if (library == null)
            {
                return;
            }

            for (int index = 0; index < kindCount; index++)
            {
                var kind = (ImpactFxKind)index;
                ImpactFxSlot slot = library.Slot(kind);
                if (!slot.IsSet)
                {
                    continue;
                }

                if (slot.MostAtOnce < 1 || !(slot.Scale > 0f))
                {
                    Debug.LogError($"{library.name}: the {kind} slot needs at least one instance and a size above 0 (it has {slot.MostAtOnce} and {slot.Scale}), so it plays nothing.", library);
                    continue;
                }

                _kinds[index] = new KindPool(kind, slot, parent);
            }
        }

        /// <summary>True when at least one kind has an effect.</summary>
        public bool HasEffects
        {
            get
            {
                for (int index = 0; index < _kinds.Length; index++)
                {
                    if (_kinds[index] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Every instance made, of every kind.</summary>
        public int InstanceCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < _kinds.Length; index++)
                {
                    count += _kinds[index] != null ? _kinds[index].Rule.Count : 0;
                }

                return count;
            }
        }

        /// <summary>How many effects have played since the spawner was made. For tests and the benchmark.</summary>
        public int PlayCount { get; private set; }

        /// <summary>The instances of one kind; 0 when it has no effect.</summary>
        public int InstanceCountOf(ImpactFxKind kind)
        {
            KindPool pool = _kinds[(int)kind];
            return pool != null ? pool.Rule.Count : 0;
        }

        /// <summary>The root of one instance of a kind, for tests. The kind must have an effect.</summary>
        public Transform Instance(ImpactFxKind kind, int index) => _kinds[(int)kind].Instances[index];

        /// <summary>
        /// Plays one effect of the kind at a point (its slot lifts it by its height), sized by the slot's scale times
        /// <paramref name="sizeScale"/>. Returns false when the kind has no effect or its least gap refused this one.
        /// </summary>
        public bool Play(ImpactFxKind kind, Vector3 point, float now, float sizeScale)
        {
            KindPool pool = _kinds[(int)kind];
            if (pool == null)
            {
                return false;
            }

            int index = pool.Rule.Claim(now);
            if (index == ImpactFxPool.None)
            {
                return false;
            }

            pool.Fire(index, point, sizeScale);
            PlayCount++;
            return true;
        }

        /// <summary>
        /// Plays every instance once at a point, at load, so each material is drawn once before play and no effect's first
        /// use is a slow frame. Put the point where the ground hides it: the GPU still draws what fails the depth test.
        /// Leaves the pool rules untouched, so play may take any instance at once.
        /// </summary>
        public void Warm(Vector3 point)
        {
            for (int index = 0; index < _kinds.Length; index++)
            {
                _kinds[index]?.WarmAt(point);
            }
        }

        /// <summary>Destroys every instance. The spawner plays nothing after.</summary>
        public void Dispose()
        {
            for (int index = 0; index < _kinds.Length; index++)
            {
                _kinds[index]?.Destroy();
                _kinds[index] = null;
            }
        }

        // One kind's instances, each with every particle system under it, made once.
        private sealed class KindPool
        {
            private readonly ParticleSystem[][] _systems;
            private readonly float _scale;
            private readonly float _heightMetres;

            public KindPool(ImpactFxKind kind, ImpactFxSlot slot, Transform parent)
            {
                Rule = new ImpactFxPool(slot.MostAtOnce, slot.LeastGapSeconds);
                _scale = slot.Scale;
                _heightMetres = slot.HeightMetres;
                Instances = new Transform[slot.MostAtOnce];
                _systems = new ParticleSystem[slot.MostAtOnce][];
                for (int index = 0; index < slot.MostAtOnce; index++)
                {
                    GameObject instance = Object.Instantiate(slot.Prefab, parent);
                    instance.name = $"{kind} {index}";
                    instance.SetActive(true);
                    Instances[index] = instance.transform;
                    _systems[index] = instance.GetComponentsInChildren<ParticleSystem>(true);
                    StopAndClear(_systems[index]);
                }
            }

            public ImpactFxPool Rule { get; }

            public Transform[] Instances { get; }

            public void Fire(int index, Vector3 point, float sizeScale)
            {
                Transform instance = Instances[index];
                instance.position = point + (Vector3.up * _heightMetres);
                float size = _scale * sizeScale;
                instance.localScale = new Vector3(size, size, size);
                ParticleSystem[] systems = _systems[index];
                StopAndClear(systems);
                for (int system = 0; system < systems.Length; system++)
                {
                    systems[system].Play(false);
                }
            }

            public void WarmAt(Vector3 point)
            {
                for (int index = 0; index < Instances.Length; index++)
                {
                    Fire(index, point, 1f);
                }
            }

            public void Destroy()
            {
                for (int index = 0; index < Instances.Length; index++)
                {
                    if (Instances[index] != null)
                    {
                        Object.Destroy(Instances[index].gameObject);
                    }
                }
            }

            private static void StopAndClear(ParticleSystem[] systems)
            {
                for (int system = 0; system < systems.Length; system++)
                {
                    systems[system].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }
}
