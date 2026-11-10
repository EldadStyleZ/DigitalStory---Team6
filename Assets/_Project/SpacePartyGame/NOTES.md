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
