---
name: build-scene
description: Use when adding, moving, renaming, reparenting or scaling GameObjects in a scene or prefab, building a new scene, or adding any UI, through the Unity MCP or by hand.
---

# Build a scene

1. **Check holds.** Read `FEATURES.md`. If someone else holds the scene or prefab, stop and tell the person.
2. **Read** `docs/SCENE_STRUCTURE.md`, and `docs/AGENT_PLAYBOOK.md` if you have not this session. Then query the current layout: `roots | select name transform.position transform.localScale`.
3. **Plan the objects** in a short tree, like the ones in that file: each object's parent, name, components and which part is the visual child. Show it to the person unless they said not to wait.
4. **Build top down** with `rql-mutate`: group parents first, then roots, then children. One small step at a time, checking each.
5. **Put UI under `Canvas`.** Create the `Canvas` empty at the root if the scene has none, then one child per screen with its UI Document.
6. **Wire references** in the Inspector or through `Game`, the composition root. Never by name at runtime.
7. **Check** with the queries in section 9 of `docs/SCENE_STRUCTURE.md`, then the console, then Play Mode.
8. **Save** the scene or prefab and report what changed, using its checklist.

## Common mistakes

| You are about to | Do this instead |
|---|---|
| Leave a new object at the scene root | Put it under its group, such as `Environment` |
| Scale a moving object's root | Scale its visual child |
| Add a UI Document anywhere but under `Canvas` | Move it under `Canvas`, one child per screen |
| Add a Canvas component | We use UI Toolkit. `Canvas` stays an empty, and uGUI would need a team decision |
| Keep the name `GameObject (1)` | Name it for what it is |
| Copy an object a third time | Make a prefab |
| Edit the `.unity` file as text | Use the MCP tools, as `AGENTS.md` says |
