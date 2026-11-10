# Scene Structure

How we lay out scenes and prefabs, so anyone can find an object, move it, scale it and debug it without breaking something else. `AGENTS.md` has the workflow and hard rules. `docs/CODING_STANDARDS.md` has the code rules, and this file applies the same ideas to the Hierarchy.

These rules are for new scenes, new objects and objects you are already changing. Do not reorganise a scene just to follow them: that is its own feature, and the scene must be yours in `FEATURES.md`.

## 1. Principles

The Hierarchy follows the same OOP ideas as the code.

| Code idea | In the Hierarchy |
|---|---|
| One job each | One GameObject has one role. A child does one visual or physical part of it. |
| Composition over inheritance | An object is built from small components and small children, not one giant script on one object. |
| Encapsulation | A prefab hides its insides. Other objects reference its root, never a deep child. |
| Dependency injection | Objects are wired by Inspector references from `Game`, the composition root. Nothing finds objects by name at runtime. |
| Fail loudly | A missing reference logs an error that selects the object, as in `HealthComponent`. |

## 2. Root layout

Every scene starts from this shape. Leave out a group the scene does not need, because Git does not miss empty objects and nor do we.

```text
<Scene>
├── Game            Composition root and scene-wide systems. Nothing else.
├── Main Camera     Camera, Audio Listener, camera scripts
├── Environment     An empty parent: ground, walls, obstacles, decoration
├── <Actors>        One root per character or group, such as Piper or Rats
├── Spawned         An empty parent for objects made in Play Mode
└── Canvas          An empty parent. Every UI object goes under it.
```

- **Group parents are plain empties.** Only a Transform, at position `(0, 0, 0)`, rotation `(0, 0, 0)` and scale `(1, 1, 1)`. Then a child's local values are also its world values, and moving one child never moves another.
- **Nothing sits loose at the root** except the objects above. A new prop goes under `Environment`, a new system goes on or under `Game`.
- **Spawned objects get a parent.** Pass a parent to `Instantiate`, such as `Spawned` or the group that owns them, like `Rats`. A Play Mode Hierarchy should look as tidy as the edit-time one.
- **Order matters only for reading.** Keep the root order above so every scene reads the same way.

## 3. One object, one role

Split an object when its parts move, scale, turn or switch off on their own. Each part becomes a child with a clear name.

```text
Piper               Rigidbody 2D, Collider 2D, the behaviour scripts. Never scaled.
├── Body            Sprite Renderer. Turns, flips and scales for looks.
├── Music Ring      Its own effect, with its own script.
└── Flute Voice     Audio Source, FluteVoice.
```

| Put it on the root | Put it on a child |
|---|---|
| Rigidbody, the main collider, behaviour scripts, `[SelectionBase]` | Sprites and meshes, particles, extra audio, lights, anything that animates |

Why: the root is what physics and code move. The child is what the player sees. You can then flip, squash or swap the picture without touching physics, and you can turn the picture off to debug the collider.

## 4. Moving and scaling safely

| Rule | Why |
|---|---|
| A moving object's root keeps scale `(1, 1, 1)`. Size it by scaling the visual child. | Scaling a physics root scales every child and its colliders, and movement code reads as if the world changed size. |
| A static prop may be scaled on its root, so its collider follows. | Matches how `Obstacles › Shape` works in `Toy`. |
| Never scale non-uniformly, such as `(2, 1, 1)`, on a parent that has rotated children. | The children skew, and the result is hard to undo. |
| Group parents are never scaled or rotated. Move the group by moving the parent's position only. | Every child keeps its own numbers, so a value in the Inspector means the same everywhere. |
| Place objects by their local position under their parent. | Moving the parent then moves the whole group in one step. |
| A pivot that is in the wrong place gets an empty parent as the new pivot. | Rotating around the right point without editing the art. |

## 5. Prefabs

- Anything that appears more than once, or that someone else might reuse, is a prefab. Edit the prefab, not each copy.
- Use a Prefab Variant for a version of something, not a copy of the prefab.
- A prefab's root holds its public parts: the components other objects reference. Other objects never reference a deep child of a prefab.
- Override sparingly. If many copies override the same field, change the prefab or make a variant.
- A prefab works on its own. Dropping it into an empty scene gives an error that names the missing reference, not a silent failure.

## 6. UI goes under Canvas

Our UI is built with UI Toolkit, so `Canvas` is an empty GameObject that groups the UI. It has no Canvas component, because that comes with the uGUI package, which we do not use.

```text
Canvas              An empty. Position (0, 0, 0), scale (1, 1, 1).
├── HUD             UI Document, the HUD's scripts
├── Pause Menu      UI Document, its scripts
└── End Screen      UI Document, its scripts
```

- **Every UI object lives under `Canvas`.** That includes UI Documents, the scripts that drive them, and anything that only exists for the UI.
- **One screen, one child.** A panel that opens and closes on its own gets its own GameObject, so you can switch it off in the Hierarchy to debug the rest.
- **Do not nest a UI Document under another UI Document's GameObject** unless you mean to nest its tree inside the parent's. The `Canvas` empty has no UI Document, so its children stay separate.
- **Moving or scaling `Canvas` does nothing to UI Toolkit**, which draws in screen space. Change size and position in the UXML and USS, or in UI Builder. Keep the `Canvas` transform at its defaults.
- **Draw order** comes from each UI Document's Sort Order, not from the Hierarchy order.
- **Share one Panel Settings asset** unless a screen really needs different scaling.
- **World-space UI**, such as a health bar over a rat, belongs to that object's prefab, not to `Canvas`.

If the team ever decides to add uGUI, record it in `FEATURES.md`. The real Canvas component then goes on this same `Canvas` object, and its fast-changing parts get their own child Canvas, so a ticking timer does not rebuild the whole screen.

## 7. Names

| Item | Style | Example |
|---|---|---|
| GameObjects | Title Case words, saying what it is | `Music Ring`, `Flute Voice` |
| Group parents | Plural, or the area | `Obstacles`, `Environment` |
| Copies | The name and a number, starting at 1 | `Rat 1`, `Shape 3` |
| UI screens | The screen's name | `HUD`, `Pause Menu` |

- No `GameObject (1)`, `Cube`, `New Sprite` or `Empty`. Rename it straight away.
- A name says what an object is, not how it looks, so a rename is not needed when the art changes.
- Two objects at the same level never share a name. RQL's `unity.find` and the Hierarchy search both rely on it.

## 8. Built for debugging

- Put `[SelectionBase]` on the root component of anything you click in the Scene view, so a click selects the whole object, not its sprite.
- Draw ranges and targets in `OnDrawGizmosSelected`. A hearing radius you can see is a hearing radius you can check.
- Log with the object as context, `Debug.LogError(message, this)`, so clicking the log selects it.
- Keep each part switchable. If turning off `Body` hides the picture and nothing else breaks, the split is right.
- Do not leave disabled objects in a scene without a reason. If one must stay, say why in `FEATURES.md`.

## 9. Checking a scene with the Unity MCP

Run these with `rql-query` after changing a scene. They come from the `unity-rql` skill. If one fails, check the member names with `rql-schema`.

```text
roots | select name transform.position transform.localScale
```

Shows the root layout. Compare it with section 2.

```text
objects | where components.count() == 1 | select name parent=(transform.parent.name) transform.localPosition transform.eulerAngles transform.localScale
```

Lists empties. Group parents should all read `(0, 0, 0)`, `(0, 0, 0)` and `(1, 1, 1)`.

```text
objects | get components | where $ is UIDocument and transform.root.name != "Canvas" | select gameObject.name
```

Lists UI Documents outside `Canvas`. It should be empty.

```text
objects | where name starts-with "GameObject" or name in ["Cube", "Square", "New Sprite", "Empty"] | select name parent=(transform.parent.name)
```

Lists unnamed objects. It should be empty.

## 10. Checklist

Before you call a scene change done:

- [ ] Every new object is under the right group, and nothing new sits loose at the root.
- [ ] Group parents are empty and at default position, rotation and scale.
- [ ] Moving roots keep scale `(1, 1, 1)`, and visuals are on children.
- [ ] Every UI object is under `Canvas`, one child per screen.
- [ ] Repeated objects are prefabs, and nothing references a prefab's deep child.
- [ ] Every object has a real name.
- [ ] The queries in section 9 show nothing unexpected.
