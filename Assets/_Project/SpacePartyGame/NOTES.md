# Space Party Game: notes

Feature F-009 in `FEATURES.md`, on branch `feature/F-009-space-party-game`, which splits from `Oisin-prototype`. Later steps refer to this file instead of repeating the rules.

## Ground rules

- Work only in the scene `Space Party Game`, in `Assets/_Project/SpacePartyGame/Scenes/`. Never change existing scenes, or scripts, prefabs and assets that other scenes use. Copy them instead. The one exception is the flute, below.
- All new scripts, prefabs, input assets, audio and sprites go under `Assets/_Project/SpacePartyGame/`.
- Art matches the style of the sprites from `inGameSprites.zip`.
- New Input System only. Use our own `SpacePartyControls.inputactions`, and never change existing input assets.
- Gameplay vermin come from an object pool. No `Instantiate` or `Destroy` during gameplay.
- Commit and push at the end of every step, on `feature/F-009-space-party-game` only. Stage only our own paths, never `git add -A` or `git add .`. Never commit the stray files already in the working folder: `Assets/_Project/Scenes/Town.unity`, `ProjectSettings/SceneTemplateSettings.json` and `ProjectSettings/ProjectAuditorSettings.asset`.
- Nothing goes to `main` except a commit that changes only `FEATURES.md`.
- At the end of every step, `git fetch`. If `origin/Oisin-prototype` has moved, merge it into `feature/F-009-space-party-game` (a normal merge; stop and report on any conflict). Then fast-forward `Oisin-prototype` to the feature branch. The user pushes both branches through Rider.
- Each step adds at most 10 lines to "Decisions and created assets" at the bottom.
- `AGENTS.md` and `docs/CODING_STANDARDS.md` still apply.

## Decisions

- **Project:** the nested clone `DigitalStory---Team6`, the project Unity has open.
- **Folder:** `Assets/_Project/SpacePartyGame/`.
- **Branch:** `feature/F-009-space-party-game`, split from `Oisin-prototype`, so the toy's flute and `Toy.unity` are available.
- **Input:** the `com.unity.inputsystem` package, with Active Input Handling set to Both, so existing scenes keep the old input. Recorded in `FEATURES.md` under Decisions.
- **Flute:** reuse the toy's `FluteSynth` and `FluteVoice` (namespace `PiperToy`) by reference. Don't copy or change them. Instruments for this scene wrap them or follow their pattern.
- **Notes (default, the user may change them before the instruments step):**

| Control | Note | Hz | Interval from the root |
|---|---|---|---|
| Root | D5 | 587.33 | root |
| Up | D6 | 1174.66 | octave |
| Left | G5 | 783.99 | 4th |
| Right | A5 | 880.00 | 5th |
| LT | E5 | 659.26 | 2nd |
| RT | B5 | 987.77 | 6th |

## Findings

### The flute (`Assets/_Project/Scripts/Flute/`, used in `Toy.unity`)

- `Flute.cs` holds the six holes, keys 1 to 6, and the note pressed last wins. `PiperInput` reads the keys and calls `Press` and `Release`.
- `FluteVoice.cs` plays the flute through an AudioSource in `OnAudioFilterRead`. The sound is made in code, with no clips.
- `FluteSynth.cs` is a plain C# synth with EditMode tests (`FluteSynthTests`):
  - a triangle wave plus an overtone an octave up, at 0.25
  - vibrato at 5.3 Hz, 6 Hz deep
  - a low-pass filter at 2800 Hz
  - breath noise around 1900 Hz, at 0.12
  - a glide between notes, a dip and swell on each new note, and a fade on release
- `FluteSound.cs` sets hole 1 to 587.33 Hz, with the holes at semitones 0, 2, 5, 7, 9 and 12. The values in `Toy.unity` are the same.
- Holes 1 to 6: D5 587.33, E5 659.26, G5 783.99, A5 880.00, B5 987.77 and D6 1174.66 Hz.
- The scale is D E G A B D: a five-note scale with no third (the notes of G major pentatonic, rooted on D). The root is D5.
- The song jingles are separate clips: `Assets/_Project/Audio/Songs/*.wav` (8 of them) and `Audio/Pop.wav`, made by `Tools/toy-sounds/make_sounds.py`.

### Sprites (`Assets/inGameSprites.zip`)

Seven PNGs, all with Hebrew filenames. Each is 1920 by 1080 and mostly empty, so trim them the way `Tools/toy-art/prepare_art.py` does. The style is hand-painted digital watercolour, with soft edges and pastel fills.

| File | Meaning | What it shows |
|---|---|---|
| `יצור 1.png` | creature 1 | One vermin: a purple, segmented, pill-shaped bug with small legs. |
| `כמה יצורים.png` | some creatures | About 10 of the same vermin in a loose group. |
| `אסטרונאוט.png` | astronaut | A small blue-and-white astronaut with a backpack, playing a flute, side view. |
| `כלובים.png` | cages | Six square cages with a round knob at each corner: orange with a yellow frame, pink, green, blue, mint and violet. |
| `רקע לבד.png` | background alone | The background without the cages. Not opened. |
| `רקע עם כלובים.png` | background with cages | An orange and beige crater floor inside a dark red-brown rim, with the six cages in place: two at the top, one left, one right, two at the bottom. |
| `עיגול.png` | circle | A soft mint glow with a dashed green edge, such as a hearing range or target ring. |

There are six cage colours. Which four are used for P1 to P4 is still to be decided.

### Menus and audio

- On this branch the only existing scene is `Toy.unity`. There is no main menu scene, because the old game's menus (F-005) are on `main`.
- There is no AudioMixer.
- There is no ambient or pad audio that could serve as background music. The song clips are short jingles, not loops.

## Sprites

Use only the English sprites in `Assets/_Project/SpacePartyGame/Sprites/`. `Sprites/Source/` keeps the zip's originals, under their Hebrew names, untouched. Make them again with `python Tools/space-party-art/prepare_art.py Assets/inGameSprites.zip`.

- **Import:** Sprite (2D and UI), Single, **100 Pixels Per Unit**, Bilinear, no mipmaps, high-quality compression, Clamp, max size 2048.
- **Scale:** nothing is resized. All the art was drawn on one 1920 by 1080 canvas, so at 100 PPU the background is 19.2 by 10.8 units, which an orthographic camera of size 5.4 fills exactly, and every other sprite keeps the size it was drawn at against it.
- **Placing:** a sprite drawn centred at canvas pixel (x, y) goes at ((x - 960) / 100, (540 - y) / 100). The tool prints each centre.

| Sprite | What it is | Size in units | Drawn centre (px) |
|---|---|---|---|
| `Background` | The crater floor and rim, with no cages | 19.20 x 10.80 | (960, 540) |
| `BackgroundWithCages` | The same, with the six cages painted in | 19.20 x 10.80 | (960, 540) |
| `Astronaut` | The player: an astronaut playing a flute, facing right | 1.35 x 1.72 | (904, 534) |
| `Vermin` | One vermin: a purple segmented bug | 0.79 x 0.60 | (1116, 504) |
| `VerminGroup` | About 10 vermin, as drawn. For reference, not for gameplay | 2.59 x 4.28 | (1194, 662) |
| `CallRing` | A mint glow with a dashed edge, such as a hearing range | 4.47 x 4.25 | (878, 520) |
| `Cages` | All six cages on one sheet. For reference | 14.13 x 8.50 | (908, 540) |
| `CageOrange` | Orange fill, yellow frame. Top left | 2.36 x 2.48 | (686, 264) |
| `CagePink` | Pink. Top right | 2.38 x 2.48 | (1147, 240) |
| `CageGreen` | Green. Left | 2.35 x 2.48 | (322, 508) |
| `CageBlue` | Blue. Right | 2.38 x 2.48 | (1495, 500) |
| `CageTeal` | Teal, which earlier notes called mint. Bottom left | 2.37 x 2.49 | (574, 840) |
| `CageViolet` | Violet. Bottom right | 2.41 x 2.52 | (1208, 818) |

Relative sizes: the astronaut is about 2.9 times a vermin's height and 1.7 times its width. A cage is about 1.4 times the astronaut's height, and fits about 3 vermin across and 4 down.

## Decisions and created assets

### Step 0

- Claimed F-009 in `FEATURES.md`, with the Input System decision.
- Created `Assets/_Project/SpacePartyGame/` with this file and `Scenes/Space Party Game.unity`, an empty scene.
- Added `com.unity.inputsystem` and set Active Input Handling to Both.

### Step 1

- Added `Tools/space-party-art/prepare_art.py`, which needs only the Python standard library. The toy's tool needs macOS and hard-codes the toy's names, so it did not fit.
- Created `Sprites/Source/`, with the seven originals under their Hebrew names, and `Sprites/`, with 13 English sprites: 7 trimmed copies and the 6 cages cut from `Cages.png`.
- Cages are named by their measured hue: orange 29, pink 333, green 98, blue 214, teal 146 and violet 236 degrees.
- PPU 100 for every sprite, with no resizing. Checked side by side in the scene, and the temporary objects were removed.

### Step 2

- `Input/SpacePartyControls.inputactions`, with schemes `Keyboard&Mouse` and `Gamepad`. Generated class `SpacePartyGame.SpacePartyControls` in `Input/SpacePartyControls.cs`.
- Maps: `Join` (Join), `Player` (Move, NoteRoot, NoteUp, NoteLeft, NoteRight, NoteLT, NoteRT, Pause), `UI` (Navigate, Submit, Cancel, Point, Click, ScrollWheel) and `Global` (ResetGame, Ctrl+R).
- Shared keys and buttons, so enable only the maps a moment needs: Esc is Pause and Cancel; Start is Join and Pause; A is NoteRoot and Submit; B is NoteRight and Cancel.
- The EventSystem was blocked until the user chose to add uGUI (`com.unity.ugui` 2.6.0, with TextMeshPro). Recorded under Decisions in `FEATURES.md`.
- `Prefabs/EventSystem.prefab`: EventSystem plus `InputSystemUIInputModule`, using `SpacePartyControls`' UI map (Point, Click, ScrollWheel, Navigate, Submit, Cancel; middle and right click unbound). It is in the scene at the root.
- `SpacePartyGame.asmdef` (namespace `SpacePartyGame`) holds every script under `SpacePartyGame/`, including the generated controls. It references `Toy` (for the flute), `Unity.InputSystem`, `UnityEngine.UI` and `Unity.TextMeshPro`.

### Step 3

- Layers 8 to 11: `Player`, `Vermin`, `MapBorder` and `CageWall`. Collisions: Player with MapBorder only; Vermin with MapBorder and CageWall only (Vermin-vs-Vermin off, so crowds don't jam in cage doors; spacing belongs in code); CageWall with Vermin only. Only pairs that involve these new layers changed. Existing content uses Default, which is untouched.
- `Prefabs/Map.prefab`: `Map` (`MapBounds`), with child `Floor` (`Background` sprite, order -100) and child `Border` (`MapBorder` layer, one closed `EdgeCollider2D` loop of 96 points, edge radius 0.05). It sits in the scene at (0, 0). An edge loop keeps bodies inside it, where a polygon collider would push them out.
- The border is traced from the dark rim by `Tools/space-party-art/trace_floor.py` (`--overlay` draws it for checking), 12 px inside the dark paint. Darker patches inside the floor are walkable, as the user decided, so there are no inner colliders.
- `MapBounds` (`Scripts/Map/`): `Rect`, for cameras, and `Contains(worldPoint)`, for spawners, both read from the Border loop through `PolygonArea`, a plain class with 10 EditMode tests.
- `Prefabs/MapWall.prefab`: a 1 by 1 `BoxCollider2D` on `MapBorder`, for extra walls.
- `Main Camera`: orthographic, size 5.4, at (0, 0, -10), background #7B534C (the rim's colour). It shows the whole map at 16:9; narrower screens need a bigger size. This becomes the overview camera later.
- Checked: 48 bouncing test bodies (Player and Vermin, 15 units/s, continuous collision) stayed inside for 300 physics steps, then were removed. The loop, drawn in the scene, sits on the rim edge with no gaps.

### Step 4

- `Prefabs/Player.prefab` on the `Player` layer. The root is the feet: `Rigidbody2D` (dynamic, no gravity, rotation frozen, interpolated, continuous), `CircleCollider2D` (radius 0.3), `PlayerInput` (`SpacePartyControls`, default map Player, C# events), `PlayerIdentity`, `PlayerMovement` (speed 4) and `PlayerNotes`. Children: `Body` (`Astronaut`, order 10, 0.78 up, flips) and `Name Tag` (TextMeshPro, 1.9 up, never flips).
- `PlayerMovement.Facing` (1 right, -1 left) and `FacingDirection`, plus a `FacingChanged` event, for the attack wave. Rules in `FacingRule` and `PlayerMotion`, with 13 EditMode tests (23 in all).
- `PlayerIdentity.Initialize(slot, colour)` sets P1 to P4 and the colour. `NameTag` follows its `Changed` event.
- Note hooks: `PlayerNotes.NotePressed` and `NoteReleased` (`Note`: Root, Up, Left, Right, LT, RT) and `IsHeld(note)`. No instruments yet.
- TMP Essential Resources were imported and moved to `SpacePartyGame/TextMesh Pro/` (TMP finds `Resources/TMP Settings` anywhere). `Materials/NameTag.mat` gives the tag a dark outline and shadow.
- Testing tip: with Unity not focused, keyboard input does not reach Play Mode and time does not pass. Click the Game view first.

### Step 5

- Flow: `GameFlow` (states StartMenu, Join, Countdown, Playing, Paused, RoundEnd, NameEntry, Leaderboard, with an allowed-moves table), `CountdownTimer` and `PlayerInputModes` (the map per state: Join in Join; Player in Playing; UI in Paused, NameEntry and Leaderboard; input off otherwise). 45 EditMode tests (68 in all).
- `Game` (scene root): `GameFlowManager` (`allowSinglePlayer` debug flag, 3-second countdown; events `StateChanged`, `OnMatchStart`, `OnCountdownFinished`, `CountdownChanged`; `TryMoveTo` for later steps), `PlayerInputManager` (Player.prefab, at most 4) and `PlayerJoinController`. Joined players go under `Players`.
- Joining opens only on the join screen. The manager's join action copies the Join/Join bindings, because the first player's PlayerInput uses the shared asset and limits it to its own devices. A joined player presses Join again, on their own Join map, to start; it needs 2 players, or 1 with `allowSinglePlayer`. Spawn points (-2, 1), (2, 1), (-2, -1.5) and (2, -1.5) are checked against `MapBounds`.
- `GameData/PlayerPalette.asset`: P1 CageOrange #D6BF66, P2 CageBlue #6389CD, P3 CageGreen #74CC67, P4 CageViolet #7F72CF, measured from each cage's frame band with the corner knobs left out. The orange cage's frame is painted yellow, so P1 is sandy yellow, not orange; change it here if wanted.
- `Canvas` (Screen Space Overlay, 1920x1080 scaling): `Start Menu` (title, tagline, the teal cage as the Start button, selected so A submits), `Join Screen` (four slot cards: cage, name in colour, device) and `Countdown`. Text uses `Materials/UIText.mat` (outline and shadow). Each screen is a `StageScreen` that shows itself in one state.
- Checked with virtual gamepads, input fed in and the loop stepped by hand: Start on the menu does nothing; the Start button opens Join; each pad joins once; one player cannot start; two can; the countdown blocks movement and notes, shows 3-2-1, ends in Playing, and fires its events once; in Playing the stick moves and all six notes fire. `allowSinglePlayer` lets one player start.
- Not checked without the Game view focused: gamepad A or mouse on the Start button, keyboard Space to join, and how the screens look. The UI module and keyboard ignore an unfocused Game view.

### Step 6

- Scene layout now follows `docs/SCENE_STRUCTURE.md`: roots `Game`, `Main Camera`, `Environment` (holds `Map`), `Players` and `Canvas` (`Dividers`, `Player HUDs`, `Start Menu`, `Join Screen`, `Countdown`, `EventSystem`). Built with the `build-scene` skill; its section 9 queries come back clean.
- `SplitLayout.For(n)`: 1 player fills the screen; 2 split it left and right; 3 or 4 take quadrants (P1 top left, P2 top right, P3 bottom left, P4 bottom right), and with 3 the overview fills bottom right. `CameraFraming`: Clamp (centres an axis when the view is bigger than the map), HalfExtents and SizeToFit. 14 EditMode tests (82 in all).
- `Game/Split Screen`: `SplitScreenManager` plus `Follow Camera 1-4` (switched off until needed, orthographic size 3, smoothing 0.15 s, clamped to `MapBounds.Rect`). It splits on `OnMatchStart`, so the countdown shows the split, and goes back to one full-screen Main Camera (size 5.4) on StartMenu or Join.
- Main Camera is the overview (sized to fit the map in its quadrant, 4.86 at 16:9) and holds the only AudioListener; when no quadrant is spare it just stops drawing. Dividers are 8 px #3B211D. `PlayerHud` areas take each viewport's anchors and show "Pn  score" in the player's colour (`SetScore` for Step 7).
- PlayerInputManager's own player cap is off (-1), because it logs a warning when the last slot fills. `PlayerJoinController` stops joining at 4 players itself.
- Checked with virtual pads: 2-, 3- and 4-player rects, cameras, HUD anchors and dividers; one AudioListener throughout; back to one camera on Join; a 5th pad cannot join; follow cameras clamp at the map's left, top and right edges.
