---
name: debug-unity
description: Use when Unity shows a compile error, exception or console error, or when something in the game does not fire, does not move, looks wrong or behaves differently in Play Mode, before changing code or settings.
---

# Debug in Unity

## Steps

1. **Read the console.** `rql-query`: `console | where logType == "Error" | sort sequence desc | first 10 | select message stackTrace`
2. **Inspect the real state** with `rql-query` or the `unity-rql` skill: the object, its components, layer and tag. Treat any suggested cause as a guess until this confirms it.
3. **Find the cause** before changing anything, one possibility at a time. Use the table below and the toolkit in `docs/CODING_STANDARDS.md`.
4. **Check `FEATURES.md`** before changing a scene or prefab. If someone holds it, stop and tell the person.
5. **Fix the cause** with the smallest change.
6. **Prove it.** For a game rule, add a test that fails without the fix. Then enter Play Mode and see it work.
7. **Clean up** temporary logs and report the cause, the fix and the proof.

## Common causes

| Symptom | Check |
|---|---|
| `OnTriggerEnter` or `OnCollisionEnter` does not fire | Exact method name and parameter, 2D or 3D mismatch, `isTrigger`, a Rigidbody or CharacterController on one object, layer matrix, tag checks in the script |
| Unity message never runs | Object active, component enabled, script on this object, method spelling |
| `NullReferenceException` on a serialized field | Empty Inspector slot, or a field renamed without `[FormerlySerializedAs]` |
| `MissingReferenceException` | A destroyed object is still referenced, often hidden by `?.` or `??` |
| Inspector values reset | A field or property was renamed |
| Data asset keeps changes after Play Mode | Runtime code is writing to a ScriptableObject |
| Handler runs twice or after unload | Missing unsubscribe in `OnDisable` |
| Coroutine stops early | Its object was deactivated |
| Movement speed changes with frame rate | Missing `Time.deltaTime`, or physics in `Update` |
| Works in the Editor, not in a build | Editor-only code, script start order, stripped assets |
