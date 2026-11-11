# Space Party Game: notes

F-009, branch `feature/F-009-space-party-game` (split from `Oisin-prototype`). Steps refer here instead of repeating rules. `AGENTS.md` and `docs/CODING_STANDARDS.md` still apply.

## Ground rules

- Work only in scene `Space Party Game` (`Assets/_Project/SpacePartyGame/Scenes/`). Never change existing scenes or anything other scenes use; copy instead (exception: the flute, below). Everything new goes under `Assets/_Project/SpacePartyGame/`.
- Art matches `inGameSprites.zip`. New Input System only, via our `SpacePartyControls.inputactions`; never change existing input assets.
- Gameplay vermin come from a pool: no `Instantiate`/`Destroy` during gameplay.
- Commit and push each step on the feature branch only. Stage our own paths only (never `git add -A`/`.`). Never commit `Assets/_Project/Scenes/Town.unity`, `ProjectSettings/SceneTemplateSettings.json`, `ProjectSettings/ProjectAuditorSettings.asset`. Only `FEATURES.md`-only commits go to `main`.
- End of each step: `git fetch`; if `origin/Oisin-prototype` moved, merge it into the feature branch (stop on conflict), then fast-forward `Oisin-prototype` to it. The user pushes both via Rider.
- Each step adds at most 10 lines to the log below.

## Decisions

- Project: the nested clone `DigitalStory---Team6` (what Unity has open).
- Input: `com.unity.inputsystem`, Active Input Handling = Both (recorded in `FEATURES.md`).
- Flute: reuse the toy's `FluteSynth` and `FluteVoice` (namespace `PiperToy`) by reference; don't copy or change. Our instruments wrap them or follow their pattern.
- Notes (default; user may change before the instruments step): Root D5 587.33, Up D6 1174.66 (octave), Left G5 783.99 (4th), Right A5 880.00 (5th), LT E5 659.26 (2nd), RT B5 987.77 (6th).

## Findings

- **Flute** (`Assets/_Project/Scripts/Flute/`, used in `Toy.unity`): `Flute.cs` six holes, keys 1–6, last pressed wins; `PiperInput` calls `Press`/`Release`. `FluteVoice` synthesises in `OnAudioFilterRead`, no clips. `FluteSynth` (plain C#, `FluteSynthTests`): triangle + octave overtone at 0.25, vibrato 5.3 Hz/6 Hz deep, low-pass 2800 Hz, breath noise ~1900 Hz at 0.12, glide, dip-and-swell per note, release fade. `FluteSound` hole 1 = 587.33 Hz, semitones 0,2,5,7,9,12 (= D E G A B D, G major pentatonic rooted on D). Song jingles: `Assets/_Project/Audio/Songs/*.wav` (8) and `Audio/Pop.wav`, from `Tools/toy-sounds/make_sounds.py`.
- **Menus/audio:** only `Toy.unity` exists on this branch (old menus F-005 are on `main`). No AudioMixer, no loopable music.

## Sprites

Use only English sprites in `Sprites/`; `Sprites/Source/` keeps the zip's 7 Hebrew-named originals (watercolour, 1920×1080 canvas, mostly empty). Regenerate: `python Tools/space-party-art/prepare_art.py Assets/inGameSprites.zip`.

Import: Sprite, Single, 100 PPU, Bilinear, no mipmaps, HQ compression, Clamp, max 2048. Nothing resized: background is 19.2×10.8 units, filled by ortho size 5.4. Canvas pixel (x, y) → world ((x−960)/100, (540−y)/100); the tool prints centres.

| Sprite | Units | Centre px | Note |
|---|---|---|---|
| Background / BackgroundWithCages | 19.20×10.80 | 960,540 | crater floor, without/with cages |
| Astronaut | 1.35×1.72 | 904,534 | player, faces right |
| Vermin | 0.79×0.60 | 1116,504 | purple segmented bug |
| VerminGroup | 2.59×4.28 | 1194,662 | reference only |
| CallRing | 4.47×4.25 | 878,520 | mint glow, dashed edge |
| Cages | 14.13×8.50 | 908,540 | reference sheet |
| CageOrange (yellow frame) | 2.36×2.48 | 686,264 | top left |
| CagePink | 2.38×2.48 | 1147,240 | top right |
| CageGreen | 2.35×2.48 | 322,508 | left |
| CageBlue | 2.38×2.48 | 1495,500 | right |
| CageTeal ("mint") | 2.37×2.49 | 574,840 | bottom left |
| CageViolet | 2.41×2.52 | 1208,818 | bottom right |

Astronaut ≈ 2.9× vermin height, 1.7× width; cage ≈ 1.4× astronaut height, fits ~3×4 vermin.

## Log: decisions and created assets

**Step 0:** claimed F-009 with the Input System decision; created this folder, this file and an empty scene; added `com.unity.inputsystem`, input handling Both.

**Step 1:** `Tools/space-party-art/prepare_art.py` (stdlib only; the toy's tool needs macOS). 13 sprites: 7 trimmed + 6 cages cut from the sheet, named by hue (orange 29, pink 333, green 98, blue 214, teal 146, violet 236°). PPU 100, checked side by side.

**Step 2:** `Input/SpacePartyControls.inputactions` (schemes Keyboard&Mouse, Gamepad; generated `SpacePartyGame.SpacePartyControls`). Maps: Join (Join); Player (Move, NoteRoot/Up/Left/Right/LT/RT, Pause); UI (Navigate, Submit, Cancel, Point, Click, ScrollWheel); Global (ResetGame, Ctrl+R). Shared bindings, so enable only needed maps: Esc = Pause/Cancel, Start = Join/Pause, A = NoteRoot/Submit, B = NoteRight/Cancel. Added uGUI `com.unity.ugui` 2.6.0 + TextMeshPro (user's decision, in `FEATURES.md`). `Prefabs/EventSystem.prefab` with `InputSystemUIInputModule` on our UI map (middle/right click unbound). `SpacePartyGame.asmdef` covers all our scripts; refs `Toy`, `Unity.InputSystem`, `UnityEngine.UI`, `Unity.TextMeshPro`.

**Step 3:** layers 8–11 `Player`, `Vermin`, `MapBorder`, `CageWall`. Collisions: Player–MapBorder; Vermin–MapBorder and CageWall (no Vermin–Vermin; spacing in code). Default untouched. `Prefabs/Map.prefab`: `Map` (`MapBounds`) > `Floor` (Background, order −100) + `Border` (closed `EdgeCollider2D`, 96 points, radius 0.05, traced 12 px inside the rim by `Tools/space-party-art/trace_floor.py --overlay`; no inner colliders, dark patches walkable). `MapBounds.Rect` and `Contains()` via `PolygonArea` (10 tests). `Prefabs/MapWall.prefab`: 1×1 box on MapBorder. Main Camera ortho 5.4 at (0,0,−10), bg #7B534C; narrower than 16:9 needs bigger size. Checked: 48 bodies at 15 u/s stayed inside 300 steps.

**Step 4:** `Prefabs/Player.prefab` (Player layer). Root = feet: dynamic `Rigidbody2D` (no gravity, rotation frozen, interpolated, continuous), `CircleCollider2D` r 0.3, `PlayerInput` (default map Player, C# events), `PlayerIdentity`, `PlayerMovement` (speed 4), `PlayerNotes`. Children `Body` (Astronaut, order 10, 0.78 up, flips) and `Name Tag` (TMP, 1.9 up, never flips). `PlayerMovement.Facing`/`FacingDirection`/`FacingChanged` for the attack wave; rules `FacingRule`, `PlayerMotion` (13 tests, 23 total). `PlayerIdentity.Initialize(slot, colour)`; `NameTag` follows `Changed`. `PlayerNotes.NotePressed`/`NoteReleased`/`IsHeld(note)`. TMP Essentials moved to `SpacePartyGame/TextMesh Pro/`. Tip: click the Game view first, or keyboard input and time don't reach Play Mode.

**Step 5:** `GameFlow` states StartMenu, Join, Countdown, Playing, Paused, RoundEnd, NameEntry, Leaderboard with allowed-moves table; `CountdownTimer`; `PlayerInputModes` (Join→Join map, Playing→Player, Paused/NameEntry/Leaderboard→UI, else off). 45 tests (68). `Game`: `GameFlowManager` (`allowSinglePlayer`, 3 s countdown; `StateChanged`, `OnMatchStart`, `OnCountdownFinished`, `CountdownChanged`; `TryMoveTo`), `PlayerInputManager` (max 4), `PlayerJoinController`; players under `Players`. Joining only on the join screen; the manager's join action copies Join/Join bindings (first PlayerInput locks the shared asset to its devices). Joined player presses Join again to start (needs 2, or 1 with the flag). Spawns (−2,1), (2,1), (−2,−1.5), (2,−1.5). `GameData/PlayerPalette.asset`: P1 #D6BF66 (orange cage's yellow frame), P2 #6389CD blue, P3 #74CC67 green, P4 #7F72CF violet. `Canvas` (Overlay, 1920×1080): `Start Menu`, `Join Screen` (4 slot cards), `Countdown`; each a `StageScreen`. Checked with virtual pads. Not checked: real gamepad A / mouse / Space join with Game view unfocused.

**Step 6:** scene follows `docs/SCENE_STRUCTURE.md`: `Game`, `Main Camera`, `Environment` (Map), `Players`, `Canvas` (`Dividers`, `Player HUDs`, menus, `EventSystem`). `SplitLayout.For(n)`: 1 full, 2 left/right, 3–4 quadrants (P1 TL, P2 TR, P3 BL, P4 BR; with 3, overview BR). `CameraFraming` Clamp/HalfExtents/SizeToFit (14 tests, 82). `Game/Split Screen`: `SplitScreenManager` + `Follow Camera 1-4` (ortho 3, smoothing 0.15 s, clamped to `MapBounds.Rect`), splits on `OnMatchStart`, back to one camera (5.4) on StartMenu/Join. Main Camera = overview (4.86 at 16:9), sole AudioListener, stops drawing when no quadrant spare. Dividers 8 px #3B211D. `PlayerHud` per viewport "Pn  score" (`SetScore`). PlayerInputManager cap off (−1, avoids warning); controller stops at 4.

**Step 6b, look and menus:** font Saira (OFL 1.1, `Fonts/Saira/OFL.txt`, © 2020 The Saira Project Authors, no RFN); static Light/Regular/SemiBold TTFs from Google Fonts service (repo has only the variable font); SDF `Saira-<weight> SDF` (ASCII + · – — …, 72 pt, padding 8); Regular is TMP default. Style: uppercase SemiBold titles, spacing 36–40; Light body; 2 px mint rules; framed START button. Text #E8EEF3, accent mint #A9DCC6 (CallRing glow #65CAA0 at half alpha). Player colours only on tags, scores, join cards. Materials `Saira-<weight> SDF - Soft Shadow`, `Saira-SemiBold SDF - Name Tag`. `MenuStack` + `MenuRules.MenuFor(state)` (16 tests, 98); `MenuManager` on `Canvas` (`Push`/`Pop`, e.g. Settings over Pause); `Canvas/Menu Backdrop` near-black 72% while any menu is open. Menus derive from `MenuScreen` (StartMenu, Join done; Pause, Settings, Winner, NameEntry, Leaderboard ids ready); Countdown is a `StageScreen`, no backdrop. Join cards show P1–P4 below the cage. Known: unplugging a joined controller logs "Cannot find matching control scheme"; handle disconnects later.

**Step 7 (not committed, per the user):** `Prefabs/Cage.prefab`: `Body` (order 5), four `CageWall` boxes (outer half-size 0.93×0.98, 0.18 thick, corners overlap), `Interior` trigger 1.5×1.6. `Cage` recounts active vermin centres inside each physics step; API `Owner`, `Count`, `Contents`, `Holds`. `Game` > `CageManager`: a cage per player on `OnMatchStart` under `Environment/Cages`, removed on StartMenu/Join. Colours: 2p green, blue; 3p green, pink, violet; 4p orange, pink, teal, violet; nearest valid spots to painted ones (inside `MapBounds` with 0.4 margin, no overlap, clear of spawns and centre). 5 points per vermin (serialized), live in `PlayerHud`; API `CageOf`, `Scores`, `ScoreOf`, `LockScores`. `CageRules.cs` (`CagePlacement`, `CageCounting`, `Scoreboard`), 14 tests (112). Checked layouts, walls at 25 u/s both ways, players pass through, scoring, locking. **Open:** closed walls mean vermin can't enter; Step 8 decides how.
