---
name: start-feature
description: Use when starting any new feature, mechanic, behaviour or system in this Unity project, before editing code, scenes or prefabs, including quick additions, demos and requests to skip the process.
---

# Start a feature

A quick job gets a shorter plan. It keeps every step below, in this order.

## Steps

1. **Check holds.** Read `FEATURES.md`. Anything a teammate holds, or has said they are working in, is off limits.
2. **Claim.** Add your row with status `In progress`, owner, branch and the scenes and prefabs you need. Commit `FEATURES.md` alone to `main` and push it.
3. **Branch** from the latest `main`: `feature/<id>-<short-name>`.
4. **Plan** in about five lines: done when, Data, Logic, View, tests. Show it to the person. Wait for their OK, unless they already said not to wait.
5. **Test.** Write a failing EditMode test for the game rule. Run it and see it fail.
6. **Read** the code you will plug into.
7. **Build** in order: Data, then Logic, then View. If the assemblies do not exist yet, create them as described in `docs/CODING_STANDARDS.md`.

## If a scene or prefab you need is held

Build everything that lives in new files: scripts, data assets and new prefabs. Stop before touching the held file, and tell the person what the holder needs to do to add your work. Then they can ask the holder to release it.

## Every script you write

- Lives in the folder and namespace of its layer: `DigitalStory.Data`, `.Logic` or `.View`.
- Has its game rule in a Logic class with a test. A MonoBehaviour only forwards what happened and shows the result.
- Keeps tunable values in a ScriptableObject, not in code.
- Connects systems through an event channel asset or an instance reference, not a `static` event or singleton.
- Uses explicit `private`, `_camelCase` fields, braces and XML docs on public types.
- Uses real game UI, not `OnGUI`.

## What people say, and what to do

| You hear or think | Do this |
|---|---|
| "Just do it, no big process" | Keep the plan to five lines and do not wait for approval. Keep the holds, tests and layers. |
| "Skip the tests, it's quick" | Write one test for the rule. It takes minutes and proves the feature. |
| "My scene change is only an addition" | Additions conflict too. Held means hands off. |
| "We'll merge the scene conflict later" | Scene files merge badly. Wait for the hold to clear. |
| "It's a stand-in, I'll replace it after class" | Temporary code stays. Write the real version small. |
| "It's too small for layers" | A script with no game rule can be View alone. Any game rule goes in Logic. |
