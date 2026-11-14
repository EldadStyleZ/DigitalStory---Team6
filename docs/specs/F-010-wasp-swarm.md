# F-010: The Wasp Piper, a wasp swarm that defends the hives

| | |
|---|---|
| Owner | EldadStyleZ |
| Repo | `EldadStyleZ/DigitalStory---Team6` |
| Branch | **`eldad-lab-2`**, made from Raz's `raz-art-test1` so his art is here |
| Claimed | The F-010 row is already on `main` |
| Scene | `Assets/_Project/Scenes/eldad-lab-2-scene.unity` (new) |
| Build prompt | `docs/specs/F-010-rider-prompt.md` |
| Later plan | [`docs/specs/F-010-songs-v2.md`](F-010-songs-v2.md) supersedes much of this spec |

> **Where the game stands now.** This file is the first spec. The songs, the economy, the farm and survive phases, the giant and the map have moved on since; [`F-010-songs-v2.md`](F-010-songs-v2.md) holds the plan, sections A to S, and what was built on 2026-11-14 for the owner's polish pass before bosses: T (a wider map and rings that follow their hives), U (a combo collects a nest), V (the Sting is the weakest attack), W (the giant throws red wasps), X (the swarm flows round trees and obstacles), Y (game feel: trauma shake and camera moments), Z (every song checked for lag in the built player) and AA (review fixes: combos per match, typos cast no song, sting numbers in tenths). Where the two disagree, the later plan wins.

## 1. The game

The player is **the Piper**. A swarm of 5,000 to 10,000 wasps follows them like a river. Playing short combos on a six-hole flute, with keys 1 to 6, commands the swarm. It can raise a dome, sting a beekeeper, spin a tornado, drop a meteor strike, blow away smoke, call more wasps from the hives, or build a **Colossus**: a giant in human shape that the player steers and fights with.

Two hives stand in a small open world, and they are the player's lives. Beekeepers come in waves to destroy them. Some fight up close with a stick, and some throw smoke grenades from a distance. Each hit the swarm takes knocks wasps out of it.

- **Lose:** both hives are destroyed.
- **Win:** the last wave is cleared.

The toy is **flow**. Everything the swarm does should look like a river or a spring.

Reference for the feel: **Leafborn** (Steam app 4247440), a 3D action game whose hero is a shape-shifting swarm of more than 10,000 leaves.

This is the team's Piper idea grown up: rats become wasps, cats become beekeepers, and the river becomes the hives. Reuse the tested flute combo code (`SongBook`).

## 2. Controls (Input Manager, which the team already uses)

| Action | Key |
|---|---|
| Walk | WASD, relative to the camera |
| Flute holes 1 to 6 | Number keys 1 to 6, or the keypad |
| Aim (sting target) | Mouse, which picks the beekeeper nearest the cursor's point on the ground |
| Colossus punch / kick | Left mouse or J / right mouse or K, only while the Colossus is active |
| Restart | R on the end screen |

**Camera:** a three-quarter view from above, about 55° down and about 14 m back. It follows the Piper with smoothing, so the river is easy to see. While the Colossus is active it pulls back to about 20 m.

## 3. The Piper

- **Art:** Raz's Piper is `Assets/_Project/Art/prefabs/Beekeeper_LowPoly.prefab`, despite its name. Raz confirmed it. Its Animator uses `Beekeeper.controller`, whose 4 states (`Idle`, `Run`, `PlayFluteTwoHands` and `RunFluteTwoHands`) come from `Beekeeper_LowPoly.fbx`. Use a **prefab variant** of it for the Piper.
- **Animator:** make our own Animator Controller by copying the 4 clips into `Assets/_Project/Art/F010/PiperSwarm.controller`.
  - It has a `Speed` float and an `IsPlaying` bool.
  - `IsPlaying` stays true for 0.6 s after any hole press, and during channelled songs.
  - **Never edit Raz's controller or prefabs. Use prefab variants.**
- **Movement:** a `CharacterController` at 4.5 m/s, with the Run clip's speed matched to the movement.
  - While playing a channelled song (Hive Call, or while the Colossus is active) the Piper stands still.
- **Flute:** `Flute_LowPoly` attached to the hand bone, unless the model already has one.
- **Health:** the Piper has no health of their own. **The swarm is the health** (see §5).

## 4. Flute songs (combos)

Reuse `DigitalStory.Logic.SongBook`, `DigitalStory.Data.SongDefinition` and `SongConfig`:

- 6 holes;
- a 3 s note window;
- the last 5 notes remembered;
- the longest song wins.

Each song is **one strategy asset**, following the team's pattern of one asset per song. Make a new abstract `SwarmSongEffect : ScriptableObject` in Logic with `Cast(in SwarmSongContext)`, in the same style as the existing `SongEffect`. The existing rat songs are not used.

| Song | Keys | Lasts | Cooldown | Effect |
|---|---|---|---|---|
| **Dome** | 1-2-1 | 5 s | 8 s | The swarm forms a dome of radius 2.2 m around the Piper. Hits cost 85% fewer wasps. The Piper moves at × 0.5 |
| **Sting** | 3-4-5 | 4 s, or until the target dies | 1.5 s | 40% of the swarm streams in an arc to the aimed beekeeper and wraps around it. 25 damage a second. Fills Fury |
| **Tornado** | 6-5-4-6 | 1.5 s | 10 s | A spinning column around the Piper. 30 damage and 6 m/s knockback to every beekeeper within 5 m. Fills Fury |
| **Gust** | 2-2-3 | 0.8 s | 6 s | The swarm bursts outward in a ring and comes back. Clears every smoke cloud within 6 m |
| **Meteor** | 5-3-1 | 1.9 s | 9 s | 30% of the swarm climbs 12 m in 0.8 s, then dives onto the aimed beekeeper or point in 0.5 s. 60 damage and a 0.5 s stun to every beekeeper within 3 m of the impact, at most 18 m from the Piper. Fills Fury |
| **Hive Call** | 1-6-1 | Until the Piper moves, is hit or reaches the maximum | 3 s | **Power up.** Streams of wasps fly from every standing hive to the Piper, adding 250 a second per standing hive. The Piper is rooted and plays |
| **Colossus** | 4-5-6-5-4 | 12 s | Needs **Fury at 100%** | The swarm builds a giant human (§6). The player steers the giant, and the Piper stands still and plays |

- The Colossus keys must not cast anything at 100% Fury unless the Colossus is played whole.
- A test checks that playing each song's keys from silence casts exactly that song.

**Fury meter:** 0 to 100. It fills by 1 per 2 damage dealt by Sting, Tornado or the Colossus, and casting the Colossus uses all of it.

**Notes are sounded:** reuse `FluteSynth` from `View/Sound` if it plugs in cleanly. Otherwise skip sound, because sound is optional for F-010.

## 5. The swarm (the player's health)

| Value | Start |
|---|---|
| Starting wasps | 5,000 |
| Maximum wasps (GPU buffer size) | 10,000 |
| Collapse below | 600 wasps |
| After a collapse | All songs are blocked for 3 s, then the swarm respawns with 2,500 wasps flying in from a standing hive |

- **Losing wasps:** stick hits, smoke and Colossus damage remove wasps. The lost wasps visibly scatter away from the hit and fall.
- A hit cancels Hive Call.

## 6. The Colossus (the human form)

- **Puppet:** Mixamo's **Y Bot**, imported as **Humanoid**, scaled so it stands about 4 m tall. It moves with its own `CharacterController` and Animator, using these clips: Idle, Walking, Punching (1 or 2 variants), Mma Kick and Hit Reaction.
  - The import settings are changed in the Unity Editor, never in `.meta` files.
  - Its renderer is switched off. The wasps *are* its body.
- **It appears where the Piper stands.** The wasps flow from the river into the body, from the feet up, over 0.8 s.
- **Controls while active:** WASD moves the giant at 3.5 m/s, J or left mouse punches, and K or right mouse kicks. The Piper stays rooted and plays, and the beekeepers can still hit the Piper.

| Attack | Damage | Hit area | Hit after | Notes |
|---|---|---|---|---|
| Punch 1 and 2 | 45 | Sphere of radius 1.5 m, 2.5 m ahead | 0.2 s | Chain window 0.6 s |
| Punch 3 | 70 | Same | 0.25 s | Knockback 8 m/s |
| Kick | 60 | Sphere of radius 1.8 m, 2.8 m ahead | 0.3 s | Cooldown 0.8 s |

- A hit enemy is stunned for 0.5 s.
- **Ends** after 12 s, or when the swarm falls below the collapse value. The wasps then flow back into the river.
- **Fallback if Mixamo is missing:** build the Colossus from the Piper rig scaled ×2.2, with the Run and Idle clips. Its attacks then use the same timings and a scale-punch tween in place of animation.

## 7. Hives and beekeepers

### Hives (the player's lives)

| Value | Start |
|---|---|
| Hives | 2 × `Beehive_LowPoly 1`, about 25 m apart |
| Health | 300 each |
| "Under attack" shows for | 2 s after the last hit |

A destroyed hive stops feeding Hive Call. When both are destroyed, the game is lost.

### Beekeepers

There are two `BeekeeperConfig` assets.

| Value | Stick (close) | Smoker (far) |
|---|---|---|
| Health | 60 | 40 |
| Speed | 2.6 m/s | 2.2 m/s |
| Notices the Piper within | 7 m | 7 m |
| Gives up the chase beyond | 14 m | 14 m |
| Attack range | 1.8 m | 14 m (throws a grenade) |
| Wind-up (visible warning) | 0.6 s | 0.8 s |
| Cooldown | 1.4 s | 4.5 s |
| Hive damage | 6 per hit | Through smoke |
| Wasps knocked off | 180 per hit | Through smoke |

**Behaviour:** `BeekeeperBrain` is one class whose states are GoToHive, ChasePiper, WindUp, Recover, Stunned and Dead. `IsTargetingHive` tells a swing at a hive from a swing at the Piper.

1. A beekeeper walks to the nearest standing hive and attacks it.
2. When the Piper comes within 7 m, or the beekeeper is hit, it attacks the Piper.
3. When the Piper is more than 14 m away, it goes back to the hive.
4. If its hive falls, it goes to the other hive.

One stick beekeeper alone destroys a hive in about a minute, so the player has time to react.

**Model:** the beekeepers are another **prefab variant** of the same `Beekeeper_LowPoly` model, so they read as the enemy and not as the Piper:

- a different tint, such as a white beekeeper suit;
- no flute;
- a primitive prop on the hand bone: a stick, or a smoke can for the smoker.

The model uses its Run clip while moving and Idle while attacking, with a lean or bob tween for the wind-up and the hit.

**Movement:** simple steering toward the target that pushes beekeepers apart, with **no NavMesh**, because baking it needs a package in Unity 6. Keep the play area open.

### Smoke grenade and cloud

| Value | Start |
|---|---|
| Flight | 1.2 s in an arc, with a ring on the ground where it will land |
| Cloud | Radius 4 m, lasts 5 s |
| Hive damage | 4 per second |
| Swarm loss | 120 wasps per second while the Piper is inside |
| Piper speed in smoke | × 0.6 |
| Counter | Gust clears it |

### Waves

| Wave | Stick | Smoker |
|---|---|---|
| 1 | 3 | 0 |
| 2 | 4 | 1 |
| 3 | 4 | 3 |
| 4 | 6 | 4 |
| 5 | 8 | 5 |

- The first wave comes after 8 s. There is a 15 s break between waves, which is time for Hive Call.
- Beekeepers spawn from 4 points at the edges of the map, 1.2 s apart.
- **Win:** wave 5 is cleared.

## 8. What the player sees (HUD, in UI Toolkit, like the existing HUD)

- **Wasp count:** a bar and a number, such as `5,000 / 10,000`, that turns red below 1,500.
- **Fury:** a bar that glows when full, with the Colossus keys shown.
- **Songs row:** each song's name, its keys and a cooldown fill, like the F-004 combos row.
- **Recent notes:** the last notes played, which fade with the 3 s window.
- **Hives:**
  - one panel per hive, with a health bar, that flashes red while under attack;
  - in the world, a health bar over each hive and a red pulsing ring while it is under attack;
  - off screen, an arrow at the screen edge pointing to any hive under attack.
- **Waves:** `Wave 2/5` and `Next wave in 10`.
- **End screen:** win or lose, and "Press R to play again".

## 9. Technical design

### 9.1 The core decision: wasps are not GameObjects

All wasps live in GPU buffers:

- one compute shader, `WaspSwarm.compute`, moves them;
- **one** `Graphics.RenderMeshIndirect` call draws them all.

This works in the project's Built-in Render Pipeline with **no new package**. The CPU cost does not change with the number of wasps, and **the CPU never reads GPU data back**.

`docs/CODING_STANDARDS.md` says "profile first, then Jobs and Burst". We go to the GPU on purpose, because 10,000 independently animated agents is a known GPU workload, and DOTS would need four new packages, which is a team decision. Say this in a code comment in `SwarmGpuView` and in the pull request.

**Gameplay never touches individual wasps.**

- Hits, damage and blocking use Logic counts and simple distances: the Piper's position, the beekeepers' positions, the dome radius, the sting target and the Colossus's hit spheres.
- The GPU only *shows* the result.
- The sting's damage comes from Logic: "target wrapped for this many seconds".

### 9.2 Buffers

| Buffer | Holds | Written |
|---|---|---|
| Static, per wasp | `seed` (0..1), `lane` (0..1 position along the river), `ring` (angle and radius in the river's cross-section), `boneIndex`, `boneOffset` (float3, for the Colossus), `height01` | Once, at start |
| Dynamic, per wasp | `position`, `velocity`, `state` (Alive, Dying or Dead), `stateTime` | By the compute shader |
| Trail | 128 × float4 points of the Piper's recent path, from `PathTrail` in Logic | By the CPU each frame, in place |
| Bones | Up to 64 × `float4x4` of the Colossus's bones | By the CPU each frame, only while the Colossus is active |
| Uniforms | Time, dt, form, `formStartTime`, active count, the Piper's position and facing, the sting target and fraction, the dome and tornado radii, hive positions, the last hit point, colours | Each frame |

Make every buffer once in `OnEnable`, release it in `OnDisable`, and reuse the arrays. There are no garbage-collection allocations per frame.

### 9.3 Where each wasp wants to be (targets per form)

| Form | Target |
|---|---|
| **River** (default) | A point along the trail at `lane`, which **drifts forward over time** (`lane + time × flowSpeed`, wrapped), plus the cross-section offset, plus a slow sine meander. The wasps keep flowing along the path like water. When the trail is shorter than 2 m (the Piper is standing), the river curls into a lazy torus about 1.5 m around the Piper |
| **Dome** | A Fibonacci hemisphere point by index, radius 2.2 m around the Piper, slowly rotating. Blue tint |
| **Sting** | Wasps with `seed < stingFraction` follow a Bezier arc from the Piper to the target, then orbit a 0.8 m sphere around it. The rest stay in the River. Orange glow |
| **Tornado** | A spiral cylinder around the Piper, radius from 1.5 to 5 m and height 3 m, spinning fast |
| **Gust** | An outward ring burst to 6 m, then back to the River |
| **Meteor** | Wasps with `seed < fraction` climb in a column to a ball above the Piper, dive in streaks onto the target, burst outward in a ring, then go back to the River. The rest stay in the River. Red-orange glow |
| **Hive Call** | A tight halo around the Piper. Newly active wasps start at a standing hive and fly an arc to the halo. Gold tint |
| **Colossus** | `bones[boneIndex] × boneOffset` |
| **Dying** | A burst away from `lastHitPoint`, gravity, shrink to zero over 1.5 s, then Dead |
| **Dead** | Scale 0, so it is not drawn. It comes back to life at a hive when its index drops below the active count |

### 9.4 How wasps move (a spring, for the flow)

```text
acceleration = k * (target - position) - 2 * zeta * sqrt(k) * velocity + buzz(seed, time)
```

| Form | Spring |
|---|---|
| River | `k` 40, `zeta` 0.6. Soft, so it flows |
| Dome and Colossus | `k` 180, `zeta` 0.75. The shape reads clearly |

- **Form change as a wave:** each wasp switches to its new target at `formStartTime + order × 0.6 s`.
  - For the Colossus, `order` is `height01`, so it builds from the feet up.
  - For the other forms, `order` is `lane`, so the change flows along the river.
- **Spring wobble:** when a form starts, `zeta` drops to 0.3 for 0.35 s.
- **Buzz:** about 0.04 m at about 9 Hz, from cheap sine noise on the seed.
- **Cap:** dt is capped at 1/30 s.
- **Active count:** the CPU sets `activeCount` from `SwarmBody`.
  - A wasp with index ≥ activeCount that is still Alive starts Dying.
  - A wasp with index < activeCount that is Dead starts Alive at the nearest standing hive.
  - The draw always covers the full maximum, and Dead wasps cost nothing visible.

### 9.5 Colossus bone sampling (once, at start)

From the Y Bot `sharedMesh`:

1. Pick a random point on a random triangle, weighted by area.
2. Take the dominant bone of that triangle's first vertex.
3. Store `boneOffset = bindposes[bone].MultiplyPoint3x4(point)` and `height01` (feet 0, head 1).

Each frame of the Colossus, upload `bones[i].localToWorldMatrix`. This needs neither the skinned vertex buffer nor a visible renderer.

**Check it:** draw 200 targets as gizmos in Play Mode and confirm they sit on the body before you trust the shader.

### 9.6 Drawing

- **Mesh:** a wasp of about 24 triangles made in code by `WaspMeshFactory`: a body and two wing quads. The yellow and black stripes come from local Z in the shader.
  - Raz's `waspa_fly` is rigged and animated, so it cannot be instanced. Its mesh may be used only if it is under about 200 triangles.
- **Shader:** `WaspInstanced.shader`, a Built-in Render Pipeline vertex and fragment shader with `#pragma target 4.5`.
  - It reads the buffers by `SV_InstanceID` and points each wasp along its velocity.
  - The wings flap with `sin(time × 40 + seed × 6.28)` on the wing vertices.
  - Lighting is Lambert plus ambient, plus a tint and emission per form.
  - It casts no shadows.
- **Draw:** `Graphics.RenderMeshIndirect` with `RenderParams.worldBounds` set to a 60 m box around the Piper.

### 9.7 Performance budget, measured in the Profiler and written in `FEATURES.md`

- 60 FPS or better at **10,000** wasps, with 13 beekeepers and 2 smoke clouds.
- The swarm's GPU work takes under 2 ms, and the CPU side of the swarm under 0.3 ms.
- There are 0 B of garbage-collection allocation per frame in gameplay.

## 10. Layers and files

Namespaces follow the layer, such as `DigitalStory.Logic`. Folders are per feature.

### Data (`Scripts/Data/Swarm/`, `Hives/`, `Beekeepers/`; assets in `Assets/_Project/GameData/F010/`)

- `SwarmConfig`: wasp counts, collapse, respawn, Hive Call rate, the springs, flow speed, wave delay, buzz and form colours.
- `PiperMoveConfig`: speed and the multipliers in the dome and in smoke.
- `SwarmSongTimings`, or fields on each song asset: durations, cooldowns, radii, damage and sting fraction.
- `FuryConfig`.
- `ColossusConfig`: scale, speed, attacks, chain window and stun. The duration is on the `ColossusSong` asset.
- `HiveConfig`.
- `BeekeeperConfig`, with two assets: `StickBeekeeper` and `SmokerBeekeeper`.
- `SmokeConfig`.
- `WaveConfig`.
- A `SongConfig` asset for this game, with a 3 s window and 5 notes. Reuse the existing class.

### Logic (`Scripts/Logic/...`, plain C#, every rule tested)

| Class | Job |
|---|---|
| `SwarmBody` | Count, maximum, `Lose(amount, isDomed)`, `Gain(deltaTime, standingHives)`, collapse and respawn. Events `CountChanged`, `Collapsed` and `Respawned` |
| `SwarmFormMachine` | The forms River, Dome, Sting, Tornado, Gust, HiveCall, Colossus, Collapsed and Meteor, which change is allowed when, and the timers. Event `FormChanged` |
| `SwarmSongEffect` (abstract ScriptableObject) | The base of each song. The songs are `DomeSong`, `StingSong`, `TornadoSong`, `GustSong`, `MeteorSong`, `HiveCallSong` and `ColossusSong`, with `SwarmSongContext` |
| `FuryMeter` | Fill, full and use |
| `PathTrail` | A fixed ring buffer of the Piper's path, resampled by distance (0.15 m), giving its length and a point at any lane |
| `ColossusAttackChain` | The punch chain window, the kick cooldown, and which attack happens and when it lands |
| `Hive` and `HiveSet` | Health, under attack, destroyed, standing count and nearest standing hive |
| `BeekeeperBrain` | The state machine in §7. It takes positions and dt, and gives back a move direction and `AttackLanded` and `GrenadeThrown` events |
| `SmokeCloud` and `SmokeField` | Area, lifetime, hive damage, swarm loss, slow, and clearing by Gust |
| `WaveDirector` | The timeline, spawn requests and "all waves cleared" |
| `HiveDefenseSession` | Owns the above, ticks them in a fixed order, and decides Playing, Won or Lost. Randomness comes through the existing `IRandomSource` |

**Reused as they are:** `SongBook`, `SongDefinition`, `SongConfig` and `IRandomSource`.

### View (`Scripts/View/...`)

| Script | Job |
|---|---|
| `HiveDefenseRoot` | The scene's single composition root |
| `HiveDefenseInput` | Input Manager: WASD, holes 1 to 6, mouse aim on the ground, J/K/left/right mouse, and R |
| `PiperMotor` | `CharacterController`, Animator hashes and the flute on the hand |
| `ColossusMotor` | The giant's controller and Animator, and its hit timing |
| `FollowCamera` | The three-quarter follow camera |
| `SwarmGpuView`, `WaspSwarm.compute`, `WaspInstanced.shader` and `WaspMeshFactory` | The GPU swarm |
| `HiveView` and `BeekeeperView` | Show hives and beekeepers. Beekeepers are pooled with `ObjectPool<T>` |
| `SmokeGrenadeView` and `SmokeCloudView` | Pooled, using a ParticleSystem |
| `HiveDefenseHudView` (UXML and USS), `HiveOffscreenIndicatorView` and `EndScreenView` | The HUD and the end screen |

### Editor

`HiveDefenseSceneBuilder` (menu `DigitalStory/Build F-010 Scene`), in the style of `TownSceneBuilder`. It creates the configs if they are missing, keeping tuned values, and builds the scene:

- a ground plane of about 70 × 70 m;
- Raz's trees, bushes and ruins around the edge;
- 2 hives;
- 4 spawn points;
- the Piper, the Colossus puppet, the camera, the HUD and the root.

## 11. Tests (EditMode, written first and seen failing)

- `SwarmBody`:
  - Loss with and without the dome, minus 85%.
  - Gain of 250 a second per standing hive, capped at 10,000.
  - Collapse below 600, the 3 s block, and the respawn with 2,500.
- `SongBook` with this game's songs:
  - Each song's keys played from silence cast exactly that song.
  - Cooldowns block a recast.
  - The Colossus needs full Fury.
- `SwarmFormMachine`:
  - Songs are blocked while collapsed.
  - A hit and a move each cancel Hive Call.
  - Each form ends after its duration.
- `FuryMeter`: fill from damage, full, and used by the Colossus.
- `PathTrail`: distance resampling, its length, the point at a lane, and wrapping.
- `ColossusAttackChain`: the 3-punch window, the heavy third punch, and the kick cooldown.
- `Hive` and `HiveSet`: damage, under attack for 2 s, nearest standing hive, and Lost when both fall.
- `BeekeeperBrain`:
  - It goes to the nearest hive and retargets when that hive falls.
  - It chases within 7 m and gives up beyond 14 m.
  - It lands a hit only after the wind-up, and a stun delays the attack.
  - The smoker throws from 14 m.
- `SmokeField`: hive damage, swarm loss, the slow, expiry, and clearing by Gust.
- `WaveDirector`: the counts per wave, the breaks, and Won after wave 5.
- `HiveDefenseSession`: Lost when both hives fall, and Won after wave 5.
- PlayMode, if time allows:
  - The scene loads with no errors.
  - `SwarmGpuView` makes and releases its buffers.

## 12. Milestones

Commit and push after each one.

| # | Result you can see | Must ship |
|---|---|---|
| M0 | Branch synced, assets checked, configs and the empty scene from the builder | Yes |
| M1 | **The core:** the Piper walks and animates, 5,000 wasps flow behind as a river and curl around the Piper when standing still. Profiler at 10,000 | Yes |
| M2 | Hives, stick beekeepers, songs Sting and Dome, hits knock off wasps, Fury, the HUD (count, songs row, notes, hives and arrows), and losing | Yes |
| M3 | Hive Call with streams from the hives, smokers and smoke, Gust, Tornado, all 5 waves and winning | Yes |
| M4 | The Colossus: Mixamo import, bone sampling, the build-up wave, punches and kick | Cut first if late |
| M5 | Full verification, `FEATURES.md`, review and the pull request | Yes |

If time runs short inside M3, cut Tornado first.

## 13. Left out on purpose

- Levels, upgrades and saving.
- Sound beyond the flute notes.
- NavMesh.
- Jumping.
- Multiplayer.
- Changing Raz's assets or his `testing.unity`.
