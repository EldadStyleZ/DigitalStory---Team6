# Scene Structure

Hierarchy rules, same ideas as `docs/CODING_STANDARDS.md`. Apply to new scenes, new objects and objects you're already changing; reorganising a whole scene is its own feature and the scene must be yours in `FEATURES.md`.

## Principles

One GameObject, one role. Compose from small components/children. Others reference a prefab's root, never a deep child. Wire via Inspector refs from `Game` (composition root); never find by name at runtime. Missing refs log an error with the object as context.

## Root layout

```text
<Scene>
├── Game          Composition root + scene-wide systems only
├── Main Camera   Camera, Audio Listener, camera scripts
├── Environment   Empty parent: ground, walls, obstacles, decoration
├── <Actors>      One root per character/group (Piper, Rats)
├── Spawned       Empty parent for Play Mode objects
└── Canvas        Empty parent for all UI
```

- Group parents: Transform only, at (0,0,0) / (0,0,0) / (1,1,1); never scaled or rotated, moved by position only.
- Nothing else loose at the root. Leave out unneeded groups. Keep this order.
- `Instantiate` always with a parent (`Spawned` or the owning group).

## Objects, moving and scaling

Split an object when parts move/scale/turn/toggle independently. Root: Rigidbody, main collider, behaviour scripts, `[SelectionBase]`. Children: sprites/meshes, particles, extra audio, lights, anything animated.

```text
Piper          Rigidbody 2D, Collider 2D, scripts. Never scaled.
├── Body       Sprite Renderer; flips/scales for looks
├── Music Ring Own effect + script
└── Flute Voice Audio Source, FluteVoice
```

- Moving roots keep scale (1,1,1); size via the visual child. Static props may be scaled on the root (like `Obstacles › Shape` in `Toy`).
- No non-uniform scale on parents with rotated children.
- Place by local position. Wrong pivot → add an empty parent as pivot.

## Prefabs

Anything repeated or reusable is a prefab; edit the prefab, not copies. Variants, not copies. Root exposes the referenced components. Override sparingly (many same overrides → change prefab or variant). A prefab dropped into an empty scene errors naming the missing ref, never fails silently.

## UI under Canvas

UI Toolkit: `Canvas` is an empty GameObject (no Canvas component; uGUI needs a team decision, then the real Canvas goes on this object, with fast-changing parts on child Canvases).

- Every UI object (UI Documents, their scripts) under `Canvas`, one child per screen (`HUD`, `Pause Menu`, `End Screen`).
- Don't nest a UI Document under another's GameObject unless nesting the tree is intended.
- `Canvas` transform stays default; size/position in UXML/USS/UI Builder. Draw order = Sort Order. Share one Panel Settings asset unless a screen needs different scaling.
- World-space UI (e.g. a rat's health bar) belongs to that object's prefab.

## Names

Title Case, what it is not how it looks (`Music Ring`). Groups plural or area (`Obstacles`). Copies numbered from 1 (`Rat 1`). UI screens by screen name. Never `GameObject (1)`, `Cube`, `New Sprite`, `Empty`. No duplicate names among siblings (`unity.find` relies on it).

## Debuggability

`[SelectionBase]` on clickable roots. Ranges/targets in `OnDrawGizmosSelected`. `Debug.LogError(msg, this)`. Each part switchable (turning off `Body` breaks nothing else). No unexplained disabled objects; note any in `FEATURES.md`.

## Check queries (`rql-query`; fix member names with `rql-schema`)

```text
roots | select name transform.position transform.localScale
objects | where components.count() == 1 | select name parent=(transform.parent.name) transform.localPosition transform.eulerAngles transform.localScale
objects | get components | where $ is UIDocument and transform.root.name != "Canvas" | select gameObject.name
objects | where name starts-with "GameObject" or name in ["Cube", "Square", "New Sprite", "Empty"] | select name parent=(transform.parent.name)
```

1: matches the root layout. 2: empties (group parents all default). 3 and 4: should be empty.

## Checklist

- [ ] New objects under the right group; group parents empty and default.
- [ ] Moving roots unscaled; visuals on children.
- [ ] All UI under `Canvas`, one child per screen.
- [ ] Repeats are prefabs; no deep-child references.
- [ ] Real names; check queries clean.
