# Features

Team tracker. Read before starting work; keep your row current.

**Concept:** The Piper (החלילן): lead the rats of Hamelin to the river with a six-hole flute before dawn.

## Board

| ID | Feature | Status | Owner | Branch | Holding | Verified by |
|---|---|---|---|---|---|---|
| F-001 | Choose the game concept | Done | Team | – | – | Team chose its HTML prototype |
| F-002 | Injection, advanced tools and fundamentals guides | Done | EldadStyleZ | `feature/F-002-engineering-guides` | – | 49 EditMode, 21 PlayMode (batch mode, scratch project) |
| F-003 | Lead the rats to the river | Done | EldadStyleZ | `feature/F-003-rats-to-river` | – | 94 EditMode, 10 PlayMode; played with keyboard/mouse; layout and ending in captures |
| F-004 | Cats and songs | Done | EldadStyleZ | `feature/F-004-cats-and-songs` | – | 207 EditMode, 17 PlayMode; owner played all nine songs, combos row, cat |
| F-005 | Menus and HUD | Done | EldadStyleZ | `feature/F-005-menus-and-hud` | – | 224 EditMode, 61 PlayMode; owner played screens, pause, retry, flute, minimap |
| F-006 | Night, effects and sound | Done | EldadStyleZ | `feature/F-006-night-effects-sound` | – | 247 EditMode, 156 PlayMode; owner played/listened; Profiler at 170 rats: 1.32 ms avg, 2.35 ms worst, 0.197 ms per rule tick |
| F-007 | Eldad's lab: summoner in a maze | In progress | EldadStyleZ | `eldad-lab` | `Assets/_Project/Scenes/eldad-lab-scene.unity` | – |
| F-008 | The toy for the course showing | In progress | EldadStyleZ | `toy-final` | `Assets/_Project/Scenes/Toy.unity` | 29 EditMode, 48 PlayMode (3 runs green); 30 deliberate breakages caught; glow, waves, disco lights and team art seen in captures; 20 frogs: conga formed 1.8 s after the song, 0.63–0.74 apart, danced 9 s; heart formed after 2.7 s (4 frogs stuck), held 7 s; no console errors. R restart checked via its method and key setting, not a real key press |
| F-009 | Space Party Game | In progress | oisin | `feature/F-009-space-party-game` | `Assets/_Project/SpacePartyGame/Scenes/Space Party Game.unity` | – |

Status: `Idea`, `Planned`, `In progress`, `Done`, `Dropped`. **Holding:** scenes/prefabs the owner is changing; nobody else edits them until cleared (scenes merge badly). **Verified by:** what was actually run.

Claim: set owner, status, holding; commit `FEATURES.md` alone to `main`. Finish: the PR sets `Done`, fills `Verified by`, clears `Holding`.

## Feature notes

Template: `### F-000: Name` then Goal (one sentence), Done when, Layers (Data/Logic/View), Tests, Notes.

### Porting The Piper (F-003 to F-006)

Rebuilds the HTML prototype (`hamelin mechanics.html`, outside the repo) slice by slice, keeping rules and numbers, not structure: globals → layered assemblies; numbers → config assets; `Math.random()`/`performance.now()` → injected `IRandomSource` and passed-in game time; `if (id === 'conga')` → one strategy asset per song; text states → state machine; effects in rules → Logic events shown by View; `setTimeout` sounds → View coroutines; HTML/CSS → UI Toolkit. `NightSession` (Logic) drives Piper/Breath/Flute and RatSwarm/Cats from `PiperInput` and config assets, and raises events for the views.

- **Left out:** `doShriek`, rat `fear`, `shriekReq`, `fluteHeld` (unreachable); levels, level select, unlocks, saves (one night).
- **Fixed:** song note window now uses game time (pausing expired notes); a rat holds one song at a time (balloons left conga/heart timers frozen); fainting rats and sleeping cats slow by time, not per frame (numbers keep their 60 fps meaning).
- **Shared decisions:** Logic keeps prototype coordinates/units; one View function maps to the scene. Rats use our rules, not `Rigidbody2D`. Sim ticks in `Update`, `deltaTime` capped at 0.033. Input Manager behind `PiperInput` (Input System is a team decision; only that class would change). Generated replaceable placeholder sprites. Text in English; UI/theme may differ. The night uses level 2's numbers (170 rats, quota 115, 150 s, first cat), in one asset.

### F-003: Lead the rats to the river

- **Goal/done:** walk the Piper through a generated town; holding hole 1 near rats makes them follow, rats crossing the bank drown; win at quota, lose at dawn.
- **Layers:** Data: town, night, Piper, breath, flute, rat configs. Logic: seeded town generator, river bank, house collision, breath, flute modes, Piper movement, rat swarm, night session. View: composition root, input, camera, town/Piper/rat views.
- **Tests:** generator and river match prototype values; house collision; breath drain/exhaustion/recovery; winning hole; flute radii; Piper speed and river limit; charm rise/decay/spread; rat speed cap; drowning; win, unreachable quota, dawn; a sim step allocates nothing.

### F-004: Cats and songs

- **Goal/done:** cats eat rats, holes 5–6 drive them off; every one of the nine songs casts from its keys.
- **Layers:** Data: cat config, song definitions, HUD config. Logic: cat behaviour, song matcher, one `SongEffect` per song, one `RatTrance` per hold kind. View: cat, song visuals, combos row.
- **Tests:** cat patrol/hunt/eat/flee/sleep; longest sequence first; 3 s note window in game time; cooldowns; each effect.
- **Notes:** combos row ships with the rules so every shown combo casts. Same-frame holes read lowest first, so a very fast descending roll can miss at low frame rates.

### F-005: Menus and HUD

- **Goal/done:** full English flow from title to result: how to play, start, pause, result, retry.
- **Layers:** Data: text assets. Logic: flow state machine. View: UI Toolkit screens, on-screen flute, HUD, minimap. **Tests:** transitions; PlayMode screens load.
- **Notes:** `UIDocument` is built in. Hebrew text translated into text assets. Retry/quit reload the scene via the title. Pauses on focus loss. Touch untested.

### F-006: Night, effects and sound

- **Goal/done:** lamps light a dark town, sky warms toward dawn; notes, ripples, rings, flute and SFX play.
- **Layers:** Data: effect/audio configs. Logic: dawn value only. View: darkness overlay, pooled effects, generated flute sound. **Tests:** dawn value; Profiler at full rat count.
- **Notes:** mute button and moon-and-sun bar ship here. Audio device change stops sources and empties code-made clips, so the flute restarts and effects re-bake. Unity's first listener read is blank, so PlayMode tests use `ListenerMeter`. Disco lights orbit during Pause. Left out: cobblestones, roof detail, water lines, detailed Piper, rat tails/eye glints, March target line. Touch untested.

### F-008: The toy for the course showing

- **Goal/done:** in `Toy.unity` the Piper walks a field; 1–2 call rats, 3–4 march them, 5–6 scare them; the eight rat songs work from their combos; combos row and on-screen flute show it. No goal, timer or counter.
- **Layers:** deliberately no Data/Logic/View: standard Unity GameObjects wired in the Inspector, a Rat prefab, one SO per song, real asset files. `Assets/_Project/README.md` explains every object and script.
- **Tests:** combo timing; walking and field edge; call/march/scare by range; each song's mood; song rests; Crowd Surf lifts the Piper; Balloons float over others; combos row and flute bar.
- **Round one:** Piper walks through rats (rats ignore its collider). Crowd Surf gathers rats under the Piper and moves exactly with it. How-to-play line removed. Flute plays live through the old synth (0.05 s release fade; note files removed). Piper eases into turns. Balloon rats stop and rise slowly up and right, staying in the field.
- **Round two (team art):** Piper, frogs as rats (code still says rats), blob field with Edge Collider 2D following the outline (notch included), upright flute showing the played hole, team shapes 1, 2, 4, 5, 6 as obstacles (two each, 3–5 units, Polygon Collider 2D stopping Piper and frogs; clear of middle, edge and each other; no rat starts inside one). Music notes coloured by hole. Drawings (9922 px) trimmed by `Tools/toy-art/prepare_art.py`; originals outside the repo. Balloon rats keep their collider so they can't float off.
- **Round three (conga and heart):** `CongaLine` records the Piper's path; each rat stands its distance back along it, 0.7 apart (`RatHerd` › Conga Gap). Line starts straight behind, or swung aside if blocked; rats nearest the Piper's back take first places. Rats run to places faster than the Piper (`Rat` › Formation Speed). Conga and heart song time starts once every rat is placed or hasn't got closer for a second (`Rat` › Stuck Seconds). Heart spots go to the nearest free rat. Old extra 0.6 s per rat removed.
- **Round four:** disco rats spin in place round their own middle, half each way, over a soft light (`Rat` › Disco Light) cycling the six hole colours every half second (`DiscoPalette`), each starting on its own colour, tinting the rat. `MusicRing` replaces the gold ring: a glow in the held hole's colour with its edge at the hearing radius and waves fading in/out. `Tools/toy-art/make_glows.py` draws glow, wave and disco light. Once every Crowd Surf rat is in the crowd circle (or stuck), Piper and crowd move at 2× (`PiperMovement` › Carried Speed Factor). R reloads the scene (`Game` › `RestartKey`).
- **Notes:** `toy-final` holds only the toy; the old game (115 scripts) stays on `main`. No Lullaby (no cats). Rats ignore the flute for 0.5 s after a song (last notes are often scare holes). Sounds from `Tools/toy-sounds/make_sounds.py` (old game's pitches and jingles).

### F-009: Space Party Game

New scene from the team's `inGameSprites.zip` art (astronaut with flute, vermin, six cages). Planned step by step; rules, decisions and log in `Assets/_Project/SpacePartyGame/NOTES.md`. Everything under `Assets/_Project/SpacePartyGame/`; reuses the toy's `FluteSynth`/`FluteVoice` by reference. Built on `Oisin-prototype` (holds the toy), not `main`.

## Ideas

Not approved; don't build on these. None right now.

## Decisions

| Date | Decision | Why |
|---|---|---|
| 2026-10-30 | Layer code `View → Logic → Data`, one assembly per layer. | Testable rules; wrong dependencies don't compile. |
| 2026-10-30 | Tunable values in ScriptableObjects. | Anyone can tune without code. |
| 2026-10-30 | Code merges via reviewed PRs; only `FEATURES.md` claims go straight to `main`. | `main` stays working; claims visible at once. |
| 2026-10-30 | New packages only by team decision. | Size, risk, upkeep. |
| 2026-10-30 | Hand-injected dependencies, one composition root per scene, no DI container. | Testable without a package. |
| 2026-10-31 | The game is The Piper, rebuilt from the HTML prototype to our standards. | Prototype defines the rules. |
| 2026-10-31 | One night: no levels, unlocks or saves. | Team doesn't want levels. |
| 2026-11-10 | Add `com.unity.inputsystem`, Active Input Handling = Both (oisin, F-009). | Space Party Game uses the new Input System; old scenes keep working. |
| 2026-11-10 | Add uGUI `com.unity.ugui` (brings TextMeshPro) for F-009 (oisin). | EventSystem with `InputSystemUIInputModule` on its own UI actions, TMP name tags. Other scenes keep UI Toolkit. |
