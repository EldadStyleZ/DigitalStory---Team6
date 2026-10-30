# Features

The team's shared tracker. Read it before starting work, and keep your row current while you work.

**Concept:** not decided yet. Keep shared code general until F-001 is done.

## Board

| ID | Feature | Status | Owner | Branch | Holding | Verified by |
|---|---|---|---|---|---|---|
| F-001 | Choose the game concept | In progress | Team | – | – | – |
| F-002 | Add injection, advanced tools and fundamentals guides | In progress | EldadStyleZ | `feature/F-002-engineering-guides` | – | – |

- **Status:** `Idea`, `Planned`, `In progress`, `Done` or `Dropped`.
- **Holding:** the scenes and prefabs the owner is changing. Nobody else edits them until this cell is cleared, because Unity scenes merge badly.
- **Verified by:** what was actually run, such as `EditMode tests, Play Mode check`.

To claim work, set the owner, status and holding, then commit `FEATURES.md` alone straight to `main`. To finish, the pull request sets `Done`, fills in `Verified by` and clears `Holding`, so merging it releases the scenes.

## Feature notes

Copy this block for each feature once it is planned.

```markdown
### F-000: Feature name

- **Goal:** what the player gets, in one sentence.
- **Done when:** what someone can see or test.
- **Layers:** what changes in Data, Logic and View.
- **Tests:** the behaviours the tests prove.
- **Notes:** anything the next person should know.
```

## Ideas

Not approved work. Do not build on these until they move to the board.

- A flute that attracts mice when you play keyboard combos.

## Decisions

Add a row when the team decides something that future work must follow.

| Date | Decision | Why |
|---|---|---|
| 2026-10-30 | Layer code as `View → Logic → Data`, with one assembly per layer. | Keeps game rules testable and turns a wrong dependency into a compile error. |
| 2026-10-30 | Keep tunable values in ScriptableObjects. | Lets anyone change the game's feel without touching code. |
| 2026-10-30 | Merge code through reviewed pull requests. Only `FEATURES.md` claims go straight to `main`. | Keeps `main` working while making claims visible at once. |
| 2026-10-30 | Add new packages only by team decision. | Each package adds build size, risk and upkeep. |
