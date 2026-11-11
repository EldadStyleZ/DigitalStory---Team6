# Agent Playbook

Unity MCP habits not covered by `AGENTS.md`.

- **First:** one small query (`roots | select name`); if it fails, stop and say so, never fall back to editing files. Read the console before changing anything. Query objects before touching them; `rql-schema(type)` for real member names.
- **Changing:** one small change, then check. `dry_run: true` for mutations touching more than a few objects. Do Editor setup yourself rather than listing clicks for the person. Save the scene/prefab.
- **One-off editor scripts:** only in `Assets/_Project/Scripts/Editor/Temporary/`; run once, delete via `editor | call DeleteAsset("<path>")` (never on disk), never commit.
- **Tokens:** `select` only needed columns, `first N`; test results as failures only (`testResults | where status != "Passed" | select name message`); run only relevant tests while building, the full suite in `finish-feature`; prove behaviour with queries, not screenshots.
- **Each class:** check against `docs/CODING_STANDARDS.md`.
- **Stop and ask** if a needed MCP tool is disabled (person enables it in the AI Game Developer window) or two reasonable designs would change the scene differently.
