# Code examples

Full worked examples behind the rules in `CODING_STANDARDS.md`, for people learning the patterns. Agents need not read this.

## Worked example: health across all three layers

One small feature across all three layers.

**Data:** tuning values in an asset.

```csharp
// Assets/_Project/Scripts/Data/Health/HealthConfig.cs
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>Tuning values shared by everything using this health setup.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Health Config", fileName = "HealthConfig")]
    public sealed class HealthConfig : ScriptableObject
    {
        [field: SerializeField, Min(1), Tooltip("Health at spawn.")]
        public int MaxHealth { get; private set; } = 100;
    }
}
```

**Logic:** the rules, with no scene needed.

```csharp
// Assets/_Project/Scripts/Logic/Health/Health.cs
using System;

namespace DigitalStory.Logic
{
    /// <summary>Tracks current health and announces changes.</summary>
    public sealed class Health
    {
        public Health(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "Max health must be positive.");
            }

            Max = max;
            Current = max;
        }

        public event Action<int> Changed;
        public event Action Died;

        public int Max { get; }
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;

        public void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage cannot be negative.");
            }

            if (amount == 0 || !IsAlive)
            {
                return;
            }

            Current = Math.Max(0, Current - amount);
            Changed?.Invoke(Current);

            if (!IsAlive)
            {
                Died?.Invoke();
            }
        }
    }
}
```

**View:** a thin link to the GameObject.

```csharp
// Assets/_Project/Scripts/View/Health/HealthComponent.cs
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Events;

namespace DigitalStory.View
{
    /// <summary>Gives a GameObject health and exposes its death to the Inspector.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Health")]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField, Tooltip("Asset with the tuning values.")]
        private HealthConfig _config;

        [Header("Reactions")]
        [SerializeField, Tooltip("Hook up effects and sound here without code.")]
        private UnityEvent _died = new UnityEvent();

        private Health _health;

        public Health Health => _health;

        private void Awake()
        {
            if (_config == null)
            {
                // Passing "this" means clicking the log selects this object.
                Debug.LogError($"{name}: HealthComponent has no config.", this);
                enabled = false;
                return;
            }

            _health = new Health(_config.MaxHealth);
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            // Unity also calls this when Awake disables the component, so _health can be null.
            if (_health != null)
            {
                _health.Died -= HandleDied;
            }
        }

        private void OnValidate()
        {
            if (_config == null)
            {
                Debug.LogWarning($"{name}: assign a HealthConfig.", this);
            }
        }

        [ContextMenu("Debug/Take 10 Damage")]
        private void DebugTakeDamage() => _health?.TakeDamage(10);

        private void HandleDied() => _died.Invoke();
    }
}
```

**Test:** the rules, proven without entering Play Mode.

```csharp
// Assets/_Project/Tests/EditMode/Health/HealthTests.cs
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HealthTests
    {
        [TestCase(30, 70)]
        [TestCase(100, 0)]
        [TestCase(250, 0)]
        public void TakeDamage_AnyAmount_NeverGoesBelowZero(int damage, int expected)
        {
            var health = new Health(100);

            health.TakeDamage(damage);

            Assert.That(health.Current, Is.EqualTo(expected));
        }

        [Test]
        public void TakeDamage_WhenAlreadyDead_DoesNotRaiseDiedAgain()
        {
            var health = new Health(10);
            var diedCount = 0;
            health.Died += () => diedCount++;

            health.TakeDamage(10);
            health.TakeDamage(10);

            Assert.That(diedCount, Is.EqualTo(1));
        }
    }
}
```

## Common patterns

**Event channel:** lets systems react to the same moment without knowing each other. Create one asset per event and assign it in the Inspector.

```csharp
// Assets/_Project/Scripts/Data/Events/EventChannel.cs
using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A named event that senders and listeners share as an asset.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Event Channel", fileName = "EventChannel")]
    public sealed class EventChannel : ScriptableObject
    {
        public event Action Raised;

        public void Raise() => Raised?.Invoke();
    }
}
```

**State machine:** one class per state. Adding a state does not touch the others.

```csharp
// Assets/_Project/Scripts/Logic/States/IState.cs
namespace DigitalStory.Logic
{
    /// <summary>One mode of a behaviour.</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
```

```csharp
// Assets/_Project/Scripts/Logic/States/StateMachine.cs
namespace DigitalStory.Logic
{
    /// <summary>Runs one state at a time.</summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        public void ChangeTo(IState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
```

**Object pool:** reuses objects instead of creating garbage.

```csharp
// Assets/_Project/Scripts/View/Spawning/PooledSpawner.cs
using UnityEngine;
using UnityEngine.Pool;

namespace DigitalStory.View
{
    /// <summary>Spawns and reuses instances of one prefab.</summary>
    public sealed class PooledSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab to reuse.")]
        private GameObject _prefab;

        [SerializeField, Tooltip("Parent for the instances, such as Spawned.")]
        private Transform _parent;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(_prefab, _parent),
                actionOnGet: instance => instance.SetActive(true),
                actionOnRelease: instance => instance.SetActive(false),
                actionOnDestroy: instance => Destroy(instance));
        }

        public GameObject Spawn() => _pool.Get();

        public void Despawn(GameObject instance) => _pool.Release(instance);
    }
}
```

**Async:** use `Awaitable` with `destroyCancellationToken`, so waiting stops when the object is destroyed.

```csharp
// Assets/_Project/Scripts/View/Effects/HitFlash.cs
using System;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Hides a renderer briefly as a hit effect.</summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Seconds the object stays hidden.")]
        private float _duration = 0.1f;

        private Renderer _renderer;

        private void Awake() => _renderer = GetComponent<Renderer>();

        public async Awaitable PlayAsync()
        {
            _renderer.enabled = false;

            try
            {
                await Awaitable.WaitForSecondsAsync(_duration, destroyCancellationToken);
                _renderer.enabled = true;
            }
            catch (OperationCanceledException)
            {
                // The object was destroyed mid-flash, so there is nothing to restore.
            }
        }
    }
}
```

## Dependency injection, step by step

**Step 1: Logic declares what it needs.** The interface lives in Logic, so Logic never depends on Unity's version.

```csharp
// Assets/_Project/Scripts/Logic/Rolls/IRandomSource.cs
namespace DigitalStory.Logic
{
    /// <summary>Supplies random numbers, so tests can control them.</summary>
    public interface IRandomSource
    {
        /// <summary>Returns a whole number from the minimum up to, but not including, the maximum.</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
```

```csharp
// Assets/_Project/Scripts/Logic/Rolls/CriticalHitRule.cs
using System;

namespace DigitalStory.Logic
{
    /// <summary>Decides whether a hit is critical.</summary>
    public sealed class CriticalHitRule
    {
        private const int FullChance = 100;

        private readonly IRandomSource _random;
        private readonly int _chancePercent;

        public CriticalHitRule(IRandomSource random, int chancePercent)
        {
            if (chancePercent < 0 || chancePercent > FullChance)
            {
                throw new ArgumentOutOfRangeException(nameof(chancePercent), chancePercent, "Chance must be 0 to 100.");
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            _chancePercent = chancePercent;
        }

        public bool Roll() => _random.NextInt(0, FullChance) < _chancePercent;
    }
}
```

**Step 2: A test passes in a fake.** The result no longer depends on luck.

```csharp
// Assets/_Project/Tests/EditMode/Rolls/CriticalHitRuleTests.cs
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CriticalHitRuleTests
    {
        [TestCase(24, true)]
        [TestCase(25, false)]
        public void Roll_ComparedToChance_IsCriticalOnlyBelowIt(int rolled, bool expected)
        {
            var rule = new CriticalHitRule(new FixedRandomSource(rolled), 25);

            Assert.That(rule.Roll(), Is.EqualTo(expected));
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int _value;

            public FixedRandomSource(int value) => _value = value;

            public int NextInt(int minInclusive, int maxExclusive) => _value;
        }
    }
}
```

**Step 3: View supplies the real version.**

```csharp
// Assets/_Project/Scripts/View/Rolls/UnityRandomSource.cs
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Random numbers from Unity's generator.</summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public int NextInt(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    }
}
```

**Step 4: A MonoBehaviour takes dependencies through `Initialize`.**

```csharp
// Assets/_Project/Scripts/View/Rolls/Attacker.cs
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Rolls for critical hits on behalf of a GameObject.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Attacker")]
    public sealed class Attacker : MonoBehaviour
    {
        private CriticalHitRule _criticalHits;

        /// <summary>Supplies this attacker's rules. Call once, before the first attack.</summary>
        public void Initialize(CriticalHitRule criticalHits) => _criticalHits = criticalHits;

        public bool RollCritical()
        {
            Debug.Assert(_criticalHits != null, $"{name}: call Initialize before attacking.", this);
            return _criticalHits.Roll();
        }
    }
}
```

**Step 5: One composition root per scene connects everything.** Reading this class shows how the scene is wired.

```csharp
// Assets/_Project/Scripts/Data/Rolls/CriticalHitConfig.cs
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>Tuning values for critical hits.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Critical Hit Config", fileName = "CriticalHitConfig")]
    public sealed class CriticalHitConfig : ScriptableObject
    {
        [field: SerializeField, Range(0, 100), Tooltip("Chance that a hit is critical.")]
        public int ChancePercent { get; private set; } = 10;
    }
}
```

```csharp
// Assets/_Project/Scripts/View/Composition/GameRoot.cs
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Creates this scene's services and hands them to the objects that need them.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)] // Runs before other scripts, so services exist before they start.
    [AddComponentMenu("DigitalStory/Game Root")]
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField, Tooltip("Tuning values for critical hits.")]
        private CriticalHitConfig _criticalHitConfig;

        [SerializeField, Tooltip("Attackers placed in this scene.")]
        private Attacker[] _attackers = new Attacker[0];

        private void Awake()
        {
            if (_criticalHitConfig == null)
            {
                Debug.LogError($"{name}: GameRoot has no critical hit config.", this);
                return;
            }

            var random = new UnityRandomSource();
            var criticalHits = new CriticalHitRule(random, _criticalHitConfig.ChancePercent);

            foreach (Attacker attacker in _attackers)
            {
                attacker.Initialize(criticalHits);
            }
        }
    }
}
```

## Advanced tools

**ScriptableObject strategy:** each behaviour is an asset, so designers swap it in the Inspector. It holds a rule, so it lives in Logic even though it is a ScriptableObject. The base is an abstract class because Unity can only save a ScriptableObject as an asset, and an interface cannot be one.

```csharp
// Assets/_Project/Scripts/Logic/Targeting/TargetPicker.cs
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A rule for choosing a target, saved as an asset.</summary>
    public abstract class TargetPicker : ScriptableObject
    {
        /// <summary>Returns the chosen index, or -1 when there are no targets.</summary>
        public abstract int Pick(IReadOnlyList<float> distances);
    }
}
```

```csharp
// Assets/_Project/Scripts/Logic/Targeting/NearestTargetPicker.cs
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Chooses the closest target.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Target Pickers/Nearest", fileName = "NearestTargetPicker")]
    public sealed class NearestTargetPicker : TargetPicker
    {
        public override int Pick(IReadOnlyList<float> distances)
        {
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < distances.Count; index++)
            {
                if (distances[index] < nearestDistance)
                {
                    nearestDistance = distances[index];
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }
    }
}
```

**Generic event channel:** one class holds the logic, and each value type gets a small class, because Unity cannot create an asset from an open generic type.

```csharp
// Assets/_Project/Scripts/Data/Events/EventChannelOfT.cs
using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A shared event that carries one value.</summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        public event Action<T> Raised;

        public void Raise(T value) => Raised?.Invoke(value);
    }
}
```

```csharp
// Assets/_Project/Scripts/Data/Events/IntEventChannel.cs
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>An event channel that carries a whole number.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Int Event Channel", fileName = "IntEventChannel")]
    public sealed class IntEventChannel : EventChannel<int>
    {
    }
}
```

**Command objects:** turn an action into an object, so it can wait in a queue.

```csharp
// Assets/_Project/Scripts/Logic/Commands/ICommand.cs
namespace DigitalStory.Logic
{
    /// <summary>One action that can be stored and run later.</summary>
    public interface ICommand
    {
        void Execute();
    }
}
```

```csharp
// Assets/_Project/Scripts/Logic/Commands/CommandQueue.cs
using System;
using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>Holds commands and runs them in order.</summary>
    public sealed class CommandQueue
    {
        private readonly Queue<ICommand> _pending = new Queue<ICommand>();

        public int Count => _pending.Count;

        public void Enqueue(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _pending.Enqueue(command);
        }

        /// <summary>Runs the oldest command. Returns false when the queue is empty.</summary>
        public bool TryExecuteNext()
        {
            if (_pending.Count == 0)
            {
                return false;
            }

            _pending.Dequeue().Execute();
            return true;
        }
    }
}
```
