# Features

The team's shared tracker. Read it before starting work, and keep your row current while you work.

**Concept:** The Piper (החלילן). Lead the rats of Hamelin to the river with a six-hole flute before dawn.

## Board

| ID | Feature | Status | Owner | Branch | Holding | Verified by |
|---|---|---|---|---|---|---|
| F-001 | Choose the game concept | Done | Team | – | – | Team chose its HTML prototype, The Piper |
| F-002 | Add injection, advanced tools and fundamentals guides | Done | EldadStyleZ | `feature/F-002-engineering-guides` | – | 49 EditMode and 21 PlayMode tests in Unity batch mode on a scratch project |
| F-003 | Lead the rats to the river | Done | EldadStyleZ | `feature/F-003-rats-to-river` | – | 94 EditMode and 10 PlayMode tests in the Editor; Play Mode check with real keyboard and mouse (walking, charming, drowning, status); layout and ending seen in Play Mode captures |
| F-004 | Cats and songs | Done | EldadStyleZ | `feature/F-004-cats-and-songs` | – | 207 EditMode and 17 PlayMode tests in the Editor; the owner played it with real keys and the nine songs, combos row and cat all worked |
| F-005 | Menus and HUD | Done | EldadStyleZ | `feature/F-005-menus-and-hud` | – | 224 EditMode and 61 PlayMode tests in the Editor; the owner played it with real keys and mouse and the screens, pause, retry, flute and minimap all worked |
| F-006 | Night, effects and sound | Done | EldadStyleZ | `feature/F-006-night-effects-sound` | – | 247 EditMode and 156 PlayMode tests in the Editor; the owner played and listened and the flute, effects, mute and night look all worked; Profiler at 170 rats with all effects on: 1.32 ms average and 2.35 ms worst per frame in the Editor, 0.197 ms per rule tick |
| F-007 | Eldad's lab: summoner in a maze | In progress | EldadStyleZ | `eldad-lab` | `Assets/_Project/Scenes/eldad-lab-scene.unity` | – |
| F-010 | Wasp swarm defends the hives | In progress | EldadStyleZ | `eldad-lab-2` (polish pass on `eldad-lab-2-next-juice`) | `Assets/_Project/Scenes/eldad-lab-2-scene.unity` and its new prefabs and prefab variants | 2026-11-14 polish pass (plan sections T to AA in `docs/specs/F-010-songs-v2.md`), in Unity batch mode: 1,873 of 1,873 EditMode tests with the thirty-seed bot sweep, 149 of 149 F-010 PlayMode tests, the scene builder OK, no new compile warning. The bot wins all 30 maps mixing songs with nests. The built Mac player's benchmark at 120 Hz: 18,269 frames, mean 8.34 ms, 99th percentile 9.76 ms, six frames over 12.5 ms (the player's own frame at 19.5 s, the giant's cursor lock, one more). Snapshots and clips looked at. Not yet played by a person on this build. Later the same day, the optional local impact effects from the owner's Epic Toon FX (plan section AB, nothing of the pack in git): 1,893 of 1,894 EditMode tests (sweep skipped), 153 of 153 F-010 PlayMode tests with and without a library, the scene builder OK, three player benchmarks at mean 8.34 ms with no long frame where an effect plays. Then the note voices, the optional local recorded sounds and the volume settings (plan section AC, nothing of the packs in git): 1,973 of 1,974 EditMode tests (sweep skipped), 165 of 165 F-010 PlayMode tests, the scene builder OK, two player benchmarks at mean 8.34 and 8.33 ms with only the known long frames; not yet heard by a person Then the merge with Raz's forest clearing, on the local branch `eldad-x-raz-try-1-perf` (plan section AD: it ran at about 40 fps with four cores busy, mostly the grass): the grass kept on the GPU, the water's depth copy off, far grass thinned, the minimap repainted at 30 Hz, at most two grenade lights, and frame-rate and render-scale settings (Auto by default). Verified in Unity batch mode and the built Mac player: 2,015 of 2,016 EditMode tests (sweep skipped), 160 of 167 F-010 PlayMode tests (the 6 pre-merge design failures, and one new black strip picture in full runs whose cause is not found), the benchmark at 120 Hz with default settings holding every song and giant window at 120 fps (the attacks into the wave of 39 not yet), 101 fps uncapped at native and 197 in a 2560x1600 window (42 and 36 on the merge). Not yet played by a person on this build |

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

### Porting The Piper

F-003 to F-006 rebuild the team's HTML prototype (`hamelin mechanics.html`, kept outside the repo) one playable slice at a time. We keep its rules and numbers, not its structure.

```mermaid
flowchart LR
    Config["Config assets<br/>Data"] --> Session["NightSession<br/>Logic"]
    Input["PiperInput<br/>View"] -->|"move and held holes"| Session
    Session --> Piper["Piper, Breath, Flute<br/>Logic"]
    Session --> Swarm["RatSwarm, Cats<br/>Logic"]
    Session -->|"events"| Views["Town, Piper, rat and HUD views<br/>View"]
```

| Prototype | Our version |
|---|---|
| One file with global state | `View → Logic → Data` assemblies |
| Numbers in code | Config assets |
| `Math.random()` and `performance.now()` | Injected `IRandomSource`, with game time passed in |
| `if (id === 'conga')` chain | One strategy asset per song |
| Text states such as `'play'` | State machine |
| Effects created inside the rules | Logic raises events and View shows them |
| `setTimeout` for sounds | Coroutines in View |
| HTML and CSS | UI Toolkit |

**Left out on purpose:**

- `doShriek`, rat `fear`, `shriekReq` and `fluteHeld`, because nothing in the prototype reaches them.
- Levels, level select, unlocks and saved progress, because the game is one night.

**Fixed while porting:**

- The song note window used wall-clock time, so pausing expired notes. It uses game time.
- The balloon song left conga and heart timers frozen, so rats went back to an old formation after popping. A rat now holds one song at a time, so a new song replaces the old one.
- Fainting rats and sleeping cats slowed once per frame, so frame rate changed them. They now slow by time, and the tuned numbers keep their meaning at 60 frames a second.

**Shared decisions:**

- Logic keeps the prototype's coordinates and units, so its tuning numbers carry over unchanged. One View function maps them to the scene.
- Rats use our own rules, not `Rigidbody2D`, so tests run without a scene.
- The simulation ticks in `Update` with `deltaTime` capped at 0.033, as the prototype does.
- Input stays on the Input Manager behind `PiperInput`. Moving to the Input System package is a team decision, and only that class would change.
- The prototype has no image or audio files. We generate replaceable placeholder sprites, so art can be swapped without code changes.
- Game text is in English. The UI and theme may differ from the prototype, because the team may change them later.
- The night uses the numbers of the prototype's second level: 170 rats, a quota of 115 and 150 seconds. It is the first with a cat, and one asset holds the numbers, so they are easy to change.

### F-003: Lead the rats to the river

- **Goal:** the player walks the Piper through a generated town, charms rats with the flute and leads them into the river before dawn.
- **Done when:** in Play Mode, holding hole 1 near rats makes them follow, rats that cross the bank drown, and the night is won at the quota and lost at dawn.
- **Layers:** Data holds town, night, Piper, breath, flute and rat configs. Logic holds the seeded town generator, river bank, house collision, breath, flute modes, Piper movement, rat swarm and night session. View holds the composition root, input, camera and the town, Piper and rat views.
- **Tests:** town generator and river curve match values captured from the prototype; house collision; breath drain, exhaustion and recovery; which held hole wins; flute mode radii; Piper speed and river limit; rat charm rise, decay and spread; rat speed cap; drowning; win, unreachable quota and dawn endings; a simulation step allocates nothing.
- **Notes:** also replaces the `AGENTS.md` line that says the concept is not final.

### F-004: Cats and songs

- **Goal:** cats hunt rats, and note sequences cast the nine songs.
- **Done when:** in Play Mode, a cat eats rats, holes 5 and 6 drive it off, and every song casts from its keys.
- **Layers:** Data holds cat config, song definitions and HUD config. Logic holds cat behaviour, the song matcher, one `SongEffect` strategy per song and one `RatTrance` per kind of hold on a rat. View holds the cat view, song effect visuals and the combos row, which lists each song's keys.
- **Tests:** cat patrol, hunt, eat pause, flee and sleep; longest sequence matches first; the three-second note window in game time; cooldowns; each song's effect.
- **Notes:** the combos row ships here, with the rules, so every combo it shows really casts. The on-screen flute stays in F-005. Holes pressed in the same frame are read lowest first, so a very fast descending roll can miss its combo at a low frame rate.

### F-005: Menus and HUD

- **Goal:** the full game flow in English, from the title screen to the result screen.
- **Done when:** a player can read how to play, start the night, pause, see the result and retry.
- **Layers:** Data holds text assets. Logic holds the game flow state machine. View holds the UI Toolkit screens, on-screen flute, HUD and minimap.
- **Tests:** flow transitions, plus a PlayMode test that the screens load.
- **Notes:** `UIDocument` is built in, so no package is needed. The prototype's Hebrew text is translated to English and kept in text assets. Retry and quitting reload the scene, so every night starts clean, and both go through the title. The game also pauses when it loses focus, because it stops then anyway. Touch is not tested.

### F-006: Night, effects and sound

- **Goal:** the game's night mood and sound.
- **Done when:** lamps light a dark town, the sky warms towards dawn, and notes, ripples, rings, the flute and sound effects all play.
- **Layers:** Data holds effect and audio configs. Logic only exposes the dawn value. View holds the darkness overlay, pooled effects and generated flute sound.
- **Tests:** the dawn value, plus a Profiler check at the full rat count.
- **Notes:** the prototype's mute button and its moving moon-and-sun bar ship here, because they need sound and the dawn value. Changing audio device stops every source and empties clips made in code, so the flute restarts and the effects bake again. Unity's first read of the listener is blank even with sound playing, so PlayMode tests read it through `ListenerMeter`. Disco lights keep orbiting during Pause, as in the prototype. Left out on purpose: cobblestones, roof detail, water flow lines, a detailed Piper, rat tails and eye glints, and the March target line. Touch is not tested.

### F-010: Wasp swarm defends the hives

- **Goal:** the player is the Piper, whose swarm of up to 10,000 wasps flows behind them like a river and takes shapes from flute combos to defend two hives from beekeepers.
- **Done when:** in Play Mode, the Piper walks with the river behind, each song casts from its keys, beekeepers attack the hives in five waves, and the match can be won and lost.
- **Layers:** Data holds the swarm, Piper movement, Fury, Colossus, hive, beekeeper, smoke and wave configs. Logic holds the swarm body, form machine, songs, Fury, path trail, hives, beekeeper brain, smoke, waves and the session. View holds the composition root, input, Piper motor, camera, the GPU swarm, hive, beekeeper and smoke views and the HUD.
- **Tests:** the EditMode tests listed in section 11 of the spec.
- **Notes:** the full spec is [`docs/specs/F-010-wasp-swarm.md`](docs/specs/F-010-wasp-swarm.md). The wasps live on the GPU (one compute shader and one indirect draw) instead of Jobs and Burst, because DOTS would need new packages. The scene is separate from Town, so The Piper keeps working. Raz's prefabs, controllers and `testing.unity` are not changed: the Piper and the beekeepers are prefab variants.
- **Built from code:** `DigitalStory/Build F-010 Scene` makes the scene, prefabs, controllers and config assets, and keeps tuned config values. `Build F-010 Scene (reset configs)` starts the configs again from the code defaults.
- **Added by the owner:** a seventh song, Meteor (keys 5-3-1).
- **Choices made while building:**
  - The dome also cuts the wasps lost to smoke, not only to hits.
  - The Colossus's body points are baked into an asset in the Editor, because imported meshes cannot be read in a build.
  - The Colossus is Mixamo's X Bot, with its clips in `Art/Mixamo`. Without that folder the builder falls back to the Piper's rig.
  - Raz's clips are not set to loop, so the builder saves looping copies in `Art/F010/Clips` and leaves his files alone.
  - The scene is not in the build settings, because that file is shared. Adding it is a team decision. In the Editor, R reloads the scene by path.
  - Flute notes are silent. `FluteVoice` needs the night session, so it did not plug in cleanly.
- **Performance, measured in batch mode only:** at 10,000 wasps the main thread takes 0.2 ms a frame on average and F-010's code allocates nothing per frame. Any `UIDocument` in this project, the Town HUD included, costs about 1.2 KB a frame, because UI Toolkit asks the Input Manager for buttons named "Next" and "Previous" that the project does not define. Adding those two entries is a team decision. GPU time and frame rate still need the Editor Profiler.
- **Polish pass, 2026-11-14:** rings that follow their hives on a wider map, a combo to collect each nest, the Sting as the weakest attack, red wasps thrown off the giant, the swarm flowing round trees, the feel layer (trauma shake, camera moments, kill chain) and a lag check of every song in the built player, all in our own code with no new package. Sections T to AA of [`docs/specs/F-010-songs-v2.md`](docs/specs/F-010-songs-v2.md) record what was built, the numbers and how each was verified.
- **Known:** three older PlayMode audio tests (`FluteVoiceTests` twice, `SoundViewTests` once) fail in batch mode on the owner's Mac, with and without this branch.

## Ideas

Not approved work. Do not build on these until they move to the board.

- None right now.

## Decisions

Add a row when the team decides something that future work must follow.

| Date | Decision | Why |
|---|---|---|
| 2026-10-30 | Layer code as `View → Logic → Data`, with one assembly per layer. | Keeps game rules testable and turns a wrong dependency into a compile error. |
| 2026-10-30 | Keep tunable values in ScriptableObjects. | Lets anyone change the game's feel without touching code. |
| 2026-10-30 | Merge code through reviewed pull requests. Only `FEATURES.md` claims go straight to `main`. | Keeps `main` working while making claims visible at once. |
| 2026-10-30 | Add new packages only by team decision. | Each package adds build size, risk and upkeep. |
| 2026-10-30 | Inject dependencies by hand, with one composition root per scene. No DI container for now. | Keeps Logic testable without adding a package. |
| 2026-10-31 | The game is The Piper, rebuilt from the team's HTML prototype to our standards. | The prototype already defines the rules, so we keep them and replace the structure. |
| 2026-10-31 | The game is one night, with no levels, unlocks or saved progress. | The team does not want levels. |
| 2026-11-13 | Add `com.unity.inputsystem` 1.20.1 with Active Input Handling set to Both, for F-010's controller support and rumble. F-009 uses the same version and setting. | The Input Manager cannot rumble a controller. Keyboard and mouse code keeps working because both systems stay on. |
