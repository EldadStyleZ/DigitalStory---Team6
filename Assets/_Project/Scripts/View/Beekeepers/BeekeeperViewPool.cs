using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Pool;

namespace DigitalStory.View
{
    /// <summary>Gives every spawned beekeeper a view from a pool and takes it back after its death animation.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Beekeeper View Pool")]
    public sealed class BeekeeperViewPool : MonoBehaviour
    {
        private const int KindCount = 2;
        private const int ExpectedViews = 32;

        private readonly Dictionary<BeekeeperBrain, BeekeeperView> _active = new Dictionary<BeekeeperBrain, BeekeeperView>(ExpectedViews);
        private readonly ObjectPool<BeekeeperView>[] _pools = new ObjectPool<BeekeeperView>[KindCount];
        private readonly BeekeeperView[] _prefabs = new BeekeeperView[KindCount];

        private HiveDefenseSession _session;

        /// <summary>Starts listening to the session. Call once, before this object starts.</summary>
        public void Initialize(HiveDefenseSession session, BeekeeperView stickPrefab, BeekeeperView smokerPrefab)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session), "BeekeeperViewPool needs the session.");
            }

            if (stickPrefab == null || smokerPrefab == null)
            {
                throw new ArgumentNullException(stickPrefab == null ? nameof(stickPrefab) : nameof(smokerPrefab), "BeekeeperViewPool needs a prefab for each beekeeper kind.");
            }

            if (_session != null)
            {
                throw new InvalidOperationException("BeekeeperViewPool is already initialized.");
            }

            _session = session;
            _prefabs[(int)BeekeeperKind.Stick] = stickPrefab;
            _prefabs[(int)BeekeeperKind.Smoker] = smokerPrefab;
            _pools[(int)BeekeeperKind.Stick] = MakePool(BeekeeperKind.Stick);
            _pools[(int)BeekeeperKind.Smoker] = MakePool(BeekeeperKind.Smoker);

            _session.BeekeeperSpawned += OnSpawned;
            _session.BeekeeperDied += OnDied;
        }

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            _session.BeekeeperSpawned -= OnSpawned;
            _session.BeekeeperDied -= OnDied;

            foreach (KeyValuePair<BeekeeperBrain, BeekeeperView> pair in _active)
            {
                pair.Value.DeathFinished -= OnDeathFinished;
            }

            _active.Clear();
        }

        private ObjectPool<BeekeeperView> MakePool(BeekeeperKind kind)
        {
            return new ObjectPool<BeekeeperView>(
                kind == BeekeeperKind.Stick ? CreateStick : (Func<BeekeeperView>)CreateSmoker,
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view => view.gameObject.SetActive(false),
                actionOnDestroy: view => Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: ExpectedViews);
        }

        private BeekeeperView CreateStick()
        {
            return Create(BeekeeperKind.Stick);
        }

        private BeekeeperView CreateSmoker()
        {
            return Create(BeekeeperKind.Smoker);
        }

        private BeekeeperView Create(BeekeeperKind kind)
        {
            BeekeeperView view = Instantiate(_prefabs[(int)kind], transform);
            view.DeathFinished += OnDeathFinished;
            return view;
        }

        private void OnSpawned(BeekeeperBrain brain)
        {
            // The session reuses brains, so the last life of this one may still be falling over.
            if (_active.TryGetValue(brain, out BeekeeperView old))
            {
                Release(old);
            }

            BeekeeperView view = _pools[(int)brain.Config.Kind].Get();
            view.Bind(brain);
            _active[brain] = view;
        }

        private void OnDied(BeekeeperBrain brain)
        {
            if (_active.TryGetValue(brain, out BeekeeperView view))
            {
                view.BeginDeath();
            }
        }

        private void OnDeathFinished(BeekeeperView view)
        {
            Release(view);
        }

        private void Release(BeekeeperView view)
        {
            BeekeeperBrain brain = view.Brain;
            if (brain != null && _active.TryGetValue(brain, out BeekeeperView current) && current == view)
            {
                _active.Remove(brain);
            }

            BeekeeperKind kind = view.Kind;
            view.Unbind();
            _pools[(int)kind].Release(view);
        }
    }
}
