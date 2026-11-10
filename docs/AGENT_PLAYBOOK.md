# Agent Playbook

Habits for AI agents working through the Unity MCP. `AGENTS.md` has the rules and what "verified" means, so this file only adds what it does not cover.

## Before changing anything

- Check the connection with one small query, such as `roots | select name`. If it fails, stop and say so. Do not fall back to editing files.
- Query the objects you will touch before changing them. Use `rql-schema(type)` for real member names instead of guessing.
- Read the console first, so errors that were already there are not blamed on you or hidden.

## Changing the Editor

- Make one small change, then check it. Use `dry_run: true` when a mutation touches more than a few objects.
- Do Editor setup yourself through the MCP. Do not hand the person a list of clicks you could have done.
- Save the scene or prefab after changing it.
- A one-off batch job may be an editor script in `Assets/_Project/Scripts/Editor/Temporary/`. Run it once, then delete it through Unity with `editor | call DeleteAsset("<path>")`, which removes its `.meta` file too. Never delete it on disk. Never commit one, because it can run again later and overwrite manual changes.

## Saving tokens

- Ask for the columns you need with `select`, and cap rows with `first N`.
- Read test results as failures only: `testResults | where status != "Passed" | select name message`.
- While building, run only the tests for what you changed. Run the full suite once, in `finish-feature`.
- Prove behaviour with a query, not a screenshot.

## OOP checklist

Check every class you write against `docs/CODING_STANDARDS.md`. The short version:

- [ ] One job. Its summary needs no "and".
- [ ] Game rules in a plain Logic class with a test. The MonoBehaviour only forwards and shows.
- [ ] `private` fields, with `[SerializeField]` for the Inspector.
- [ ] Dependencies through the constructor, the Inspector or `Initialize`. No `Find`, singletons or `static` state.
- [ ] A new variant is a new class or asset, not another `case`.
- [ ] Tunable numbers in a Data asset.

## Stop and ask when

- An MCP tool you need is disabled. Ask the person to enable it in the AI Game Developer window.
- Two reasonable designs would change the scene in different ways.
