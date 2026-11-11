# Coding Standards

Workflow and hard rules: `AGENTS.md`. Naming/formatting also enforced by `.editorconfig`. Full code examples: `docs/CODE_EXAMPLES.md`. Breaking a rule needs a reason in a code comment and the PR.

## Principles

Readable first. One job per class/method (no "and" in its summary). Data drives behaviour (tuning in assets). Add variants by plugging in a new asset/class, not a `case`. Simple beats clever: abstract at the second real use. Fail loudly on bad setup.

## Layers

| Layer | Holds | Rules |
|---|---|---|
| Data | SO configs, event channels, plain structs, enums | Knows nothing of Logic/View. No game rules. |
| Logic | Game rules, state, state machines | Plain C#. May use Data, `Mathf`, `Vector3`. No `GameObject`, `Transform`, `Time`, input; take `deltaTime`/input as parameters. |
| View | MonoBehaviours: input, physics, animation, audio, UI | Forwards events to Logic, shows results. A game-rule `if` belongs in Logic. |

A script with no game rule (e.g. a spinner) can be View alone; don't create empty layers.

| Problem | Use | Avoid |
|---|---|---|
| Tuning/content | SO asset | Numbers in code |
| Modes | State machine (one class per state) | `bool` flag piles |
| Reacting / one system telling another | C# event / event channel asset | Polling in `Update`, `Find`, singletons |
| Service, randomness, time, storage | Dependency injection | Creating inside, `static` access |
| Behaviour variants | Interface or SO strategy | `switch` on type |
| Frequent spawns | `ObjectPool<T>` | `Instantiate`/`Destroy` every few frames |
| Short timed sequences | Coroutine or `Awaitable` (+ `destroyCancellationToken`) | Timer fields in `Update` |
| Thousands of objects | Profile, then Jobs/Burst | ECS by default |

## Assemblies

| Assembly | References |
|---|---|
| `DigitalStory.Data` | none |
| `DigitalStory.Logic` | Data |
| `DigitalStory.View` | Data, Logic |
| `DigitalStory.Editor` | Data, Logic, View; Editor only |
| `DigitalStory.Tests.EditMode` | Data, Logic + Unity test assemblies |
| `DigitalStory.Tests.PlayMode` | Data, Logic, View + Unity test assemblies |

asmdef: `rootNamespace` = name, `autoReferenced: false`. Namespace = assembly (feature folders don't add to it). Root stays `DigitalStory` even if the game is renamed.

## Naming

PascalCase: types (nouns), methods (verbs), properties, constants, `static readonly`. `I` prefix for interfaces. `_camelCase` private fields (serialized too). `camelCase` params/locals. Booleans read as questions (`IsAlive`, `CanJump`). Events past tense (`Died`); handlers `Handle<Event>`. `Async` suffix. Assets PascalCase, type last (`PlayerHealthConfig`). Spell words out (`damagePerSecond`); `Hp`, `Xp`, `Ui`, `Vfx` are fine.

## Classes

- One public type per file, file named after it. Explicit access modifiers always. `sealed` unless designed for inheritance.
- Member order: constants, serialized fields, private fields, properties, events, Unity messages (call order), public methods, private methods.
- Short methods, early returns, braces always. Name numbers: tunables in Data, fixed values `const`.
- Composition over deep inheritance. Comments say why. XML docs on public APIs.
- Serialized: `[SerializeField] private`, or `[field: SerializeField]` with `{ get; private set; }`. Add `[Tooltip]`, `[Min]`/`[Range]`, `[Header]`.
- MonoBehaviours: `[DisallowMultipleComponent]`, `[AddComponentMenu("DigitalStory/...")]` where useful; on missing refs `Debug.LogError($"{name}: ...", this)` and `enabled = false`; warn in `OnValidate`. Initialize `UnityEvent` fields (`= new UnityEvent()`).
- Subscribe in `OnEnable`, unsubscribe in `OnDisable` (null-guard if `Awake` may have bailed).
- SO strategies holding a rule live in Logic; base is an abstract SO class (interfaces can't be assets). Generic event channel: abstract `EventChannel<T>` plus one concrete subclass per type.

## Dependency injection (by hand; a container needs a team decision)

| Kind | Use for |
|---|---|
| Constructor (default) | Plain C# classes |
| Inspector `[SerializeField]` | SOs, prefabs, scene objects |
| `Initialize(...)` method | MonoBehaviours needing plain C# services |

- Inject services, randomness, time, storage, anything tests control. Not plain values, pure helpers (`Mathf`) or single-use helpers.
- Logic owns interfaces (e.g. `IRandomSource`); View implements them (`UnityRandomSource`); tests pass fakes.
- Only the scene's composition root (`GameRoot`, `[DefaultExecutionOrder(-1000)]`) creates services and calls `Initialize`. Spawners get dependencies from it and call `Initialize` right after `Instantiate`.
- Use injected dependencies from `Start` on (`Instantiate` runs `Awake`/`OnEnable` first). No passing the root around, no global registry (service locator).

## Advanced tools: use only when needed

Interface (two versions or a test fake). SO strategy (designers pick per asset). Generic event channel (event carries a value). Command objects (queue/rebind/undo/replay). `readonly struct` (small immutable values). `ListPool<T>` (per-frame temp lists). `InternalsVisibleTo` (test needs non-public member the public API can't prove). `[SerializeReference]` (mixed plain `[Serializable]` classes in one asset; may need custom Inspector). Custom editor tools (recurring setup mistake). Jobs/Burst (Profiler shows heavy math). DI container: not now.

## Unity traps

- `?.`, `??`, `is null` skip Unity's destroyed check: use `== null` / `if (obj)` on Unity objects.
- Renaming serialized fields: `[FormerlySerializedAs("old")]`; for properties `[field: FormerlySerializedAs("<Old>k__BackingField")]`, same commit.
- Don't write to SOs at runtime (Play Mode edits persist); runtime state in plain classes.
- Start order isn't guaranteed: `Awake` sets up self, `Start` talks to others.
- `enabled = false` in `Awake` still calls `OnDisable`.
- Trigger/collision messages reach disabled scripts; check `enabled` if needed.
- Mutable `static` state leaks between runs; avoid, or reset via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Forces in `FixedUpdate`, input in `Update`, scale per-frame movement by `Time.deltaTime`.
- Coroutines stop when their object deactivates. `Destroy` happens at end of frame.

## Debugging hooks

`Debug.LogError(msg, this)`, `Debug.Assert(cond, msg, this)`, `OnValidate`, `[RequireComponent]`, `[DisallowMultipleComponent]`, `[SelectionBase]` on root, `[ContextMenu("Debug/...")]`, `OnDrawGizmosSelected`/`Debug.DrawRay`, Inspector Debug mode, `Debug.Break()`/Rider breakpoints, Profiler + `ProfilerMarker`, Frame Debugger, Physics Debugger. Remove temporary logs before merging; kept logs name the object and values.

## Performance (always, in per-frame code)

No allocations (`new` classes, LINQ, closures, string building). No `Find*` or repeated `GetComponent`; cache in `Awake`. `CompareTag`. Hash `Animator`/`Shader` IDs once. `NonAlloc` physics with reused arrays. Pool anything spawned several times a second. No `Debug.Log`. Profile before optimizing further.

## Tests

- Every Logic rule has an EditMode test. PlayMode only for engine needs (lifecycle, physics, scene setup).
- Name `Method_Condition_ExpectedResult`; `[TestCase]` for variations; floats `Is.EqualTo(x).Within(0.001f)`.
- Test the decision, not the physics outcome. Bug fixes add a test that fails without the fix.
- Never delete a failing test to pass; fix the code, or fix a wrong test and say why. Hard to test = move the rule out of the MonoBehaviour.

## Dependencies and safety

Built-ins first (`ObjectPool<T>`, `Awaitable`, `JsonUtility`). Copying a small library function: check the license; comment source URL, version, license, author. No home-made crypto or standard-format parsers. Validate external data (save files) on load and fail safely. No secrets in repo or build. Online features need a team decision and an abuse plan.
