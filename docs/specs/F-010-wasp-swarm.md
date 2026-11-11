# F-010: Wasp swarm defends the hives

Owner: EldadStyleZ. Branch: `eldad-lab-2` (made from Raz's `raz-art-test1`, so his art is here).
Scene: `Assets/_Project/Scenes/eldad-lab-2-scene.unity` (new).

## 1. The game in one paragraph

The player is a swarm of 5,000 to 10,000 wasps flying in the shape of a person. The shape walks and runs like a normal human. Two hives stand in a small open world, and they are the player's lives. Beekeepers come in waves to destroy the hives. Some fight up close with a stick, and others throw smoke grenades from a distance. The player punches and kicks them, and the swarm can reshape into a shield, flow into a giant fist for a strong attack, or stand still and power up by calling more wasps from the hives. Every hit the player takes knocks wasps out of the swarm. The player loses when both hives are destroyed and wins after the last wave.

Reference for the feel: **Leafborn** (Steam app 4247440), a 3D action game whose hero is a shape-shifting swarm of more than 10,000 leaves.

## 2. The toy: flow

Everything the swarm does should look like a river or a spring:

- **Walking and running** look like a normal human. The wasps hold the body shape tightly and only buzz a little at the edges.
- **Jumping** turns the body into a spring. The head leads, the lower body trails behind in an arc, and on landing the swarm wobbles and settles.
- **Changing form** is a wave. Wasps leave the old shape in order, from top to bottom, so the eye sees the swarm *flow* into the new shape.
- **Powering up** pulls visible streams of wasps from the hives into the player.

## 3. Controls (Input Manager, which the team already uses)

| Action | Key | Notes |
|---|---|---|
| Move | WASD | Relative to the camera |
| Run | Hold Left Shift | |
| Jump | Space | Spring look in the air |
| Punch | Left mouse or J | Up to 3 punches in a chain; the third is heavier |
| Kick | Right mouse or K | |
| Shield | Hold Q | Slower movement, no attacks |
| Strong strike | Hold E, release to hit | Only when the strike meter is at 100% |
| Power up | Hold R | Standing still only. Adds wasps up to the maximum |
| Camera | Mouse | Third person, cursor locked |
| Restart | R on the end screen | |

Power up is on R, not a long press of Space. Space jumps on key down, and a long press would delay every jump.

## 4. Rules and numbers

All numbers go in ScriptableObject configs. These are the starting values.

### Swarm (the player's health)

| Value | Start |
|---|---|
| Starting wasps | 5,000 |
| Maximum wasps | 10,000 |
| Power-up rate | 250 wasps per second per hive still standing |
| Collapse below | 600 wasps |
| After a collapse | Respawn at a standing hive after 3 s with 2,500 wasps |

- A hit removes wasps. Those wasps visibly scatter and fall away.
- While powering up the player cannot move or attack. A hit cancels the power up.

### Movement

| Value | Start |
|---|---|
| Walk speed | 2.4 m/s |
| Run speed | 6 m/s |
| Jump height | 1.8 m |
| Gravity | -20 m/s² |
| Speed in shield form | × 0.45 |

### Player attacks

| Attack | Damage | Reach | Hit after | Notes |
|---|---|---|---|---|
| Punch 1 and 2 | 20 | 1.6 m, radius 0.9 | 0.15 s | Next punch only within a 0.5 s window |
| Punch 3 | 35 | 1.8 m, radius 1.0 | 0.2 s | Knocks back |
| Kick | 30 | 2.0 m, radius 1.0 | 0.25 s | 0.7 s cooldown |
| Strong strike | 120 | 4.5 m area in front | 1 s charge | Knocks back, empties the meter |

- Each landed hit adds 12% to the strike meter.
- A hit enemy is stunned for 0.5 s.
- The shield cuts wasp loss by 85% for hits from the front 120°. Smoke still goes through.

### Hives (the player's lives)

| Value | Start |
|---|---|
| Hives | 2, about 25 m apart |
| Hive health | 300 |
| "Under attack" shows for | 2 s after the last hit |

### Beekeepers

| Value | Close (stick) | Far (smoke) |
|---|---|---|
| Health | 60 | 40 |
| Speed | 2.6 m/s | 2.2 m/s |
| Notices the player within | 7 m | 7 m |
| Gives up the chase beyond | 14 m | 14 m |
| Attack range | 1.8 m | 14 m (throw) |
| Wind-up (visible warning) | 0.6 s | 0.8 s |
| Cooldown | 1.4 s | 4.5 s |
| Damage to a hive | 6 per hit | Through smoke |
| Wasps lost by the player | 180 per hit | Through smoke |

- A beekeeper walks to the nearest standing hive and attacks it. When the player gets close or hits it, it fights the player, and it goes back to the hive when the player is far away.
- If its hive is destroyed it goes to the other hive.
- One stick beekeeper alone destroys a hive in about a minute, so the player has time to react.

### Smoke grenade

| Value | Start |
|---|---|
| Flight time | 1.2 s, in an arc, with a ring on the ground where it will land |
| Cloud radius | 4 m |
| Cloud lasts | 5 s |
| Hive damage | 4 per second |
| Wasps the player loses | 120 per second |
| Player speed in smoke | × 0.6 |

### Waves

| Wave | Stick | Smoke |
|---|---|---|
| 1 | 3 | 0 |
| 2 | 4 | 1 |
| 3 | 4 | 3 |
| 4 | 6 | 4 |
| 5 | 8 | 5 |

- The first wave comes after 8 s. There is a 15 s break between waves, which is time to power up.
- Beekeepers come in from 4 points at the edges of the map, 1.2 s apart.
- **Win:** wave 5 is cleared. **Lose:** both hives are destroyed.

## 5. What the player sees (HUD, in UI Toolkit)

- **Wasp count:** a bar and a number, such as `5,000 / 10,000`, that turns red below 1,500.
- **Form:** an icon for body, shield, strike or power up.
- **Strike meter:** fills up and glows at 100%.
- **Hives:** one small panel per hive, with a health bar, that flashes red while under attack.
- **In the world:** a health bar over each hive, and a red pulsing ring around a hive under attack.
- **Off screen:** an arrow at the screen edge pointing to any hive under attack, so the player always knows where to go.
- **Waves:** `Wave 2/5` and `Next wave in 10`.
- **End screen:** win or lose, and "Press R to play again".

## 6. Technical design

### 6.1 The decision: wasps are not GameObjects

10,000 GameObjects or Animators cannot run smoothly. Every wasp lives in a GPU buffer:

- A **compute shader** moves all wasps each frame.
- **One** `Graphics.RenderMeshIndirect` call draws them all.
- The CPU cost does not change with the number of wasps, and the CPU never reads data back from the GPU.

Both work in the Built-in Render Pipeline the project uses, with no new package. DOTS/ECS would need four new packages, which is a team decision, and it is too big for this deadline. `docs/CODING_STANDARDS.md` says "Profile first, then Jobs and Burst". We go straight to the GPU on purpose, and the code comment and the pull request must say why.

**Gameplay never touches individual wasps.** Hits, health and blocking all use the puppet's capsule and the counts in Logic. The GPU only shows the result.

### 6.2 The puppet

- An invisible humanoid, the **puppet**, has an Animator with Idle, Walk, Run, Jump, Punch ×2 or 3, Kick, Block and Hit clips. It moves with a `CharacterController`. Its animations are an ordinary person's animations.
- **Source:** Mixamo's "Y Bot" with these clips. Download them as FBX for Unity, with "In Place" for the movement clips, and set the rig to Humanoid **in the Unity import settings**, never by editing `.meta` files.
- The puppet's own renderer is switched off. The swarm *is* its body.

### 6.3 Where each wasp goes (targets)

When the game starts, sample the puppet's mesh once on the CPU:

1. Pick a random point on a random triangle, weighted by triangle area.
2. Take the bone with the highest weight at that point.
3. Save the bone index and the point in that bone's space: `offset = bindposes[bone].MultiplyPoint3x4(meshPoint)`.

Also save:

- a random `seed`;
- `height01`, the point's height from feet (0) to head (1).

These go into a **static buffer**, which never changes.

**Each frame** the CPU uploads only the puppet's bone matrices (`bone.localToWorldMatrix`, about 20 to 60 of them) into a reused buffer. There is no allocation. The body target of each wasp is `boneMatrix[bone] × offset`.

This is more robust than reading the skinned vertex buffer, because it does not depend on the renderer being visible or on the vertex layout. `SkinnedMeshRenderer.GetVertexBuffer` is the fallback if more detail is ever needed.

**Form targets.** The shader computes these from the index and seed, with no extra buffer:

| Form | Targets |
|---|---|
| Body | Bone × offset, as above |
| Shield | A slightly curved disc of about 1.6 m in front of the chest, laid out as a Fibonacci disc by index. Blue tint |
| Strike | A ball around the right hand bone, whose radius grows from 0.3 m to 1.2 m with the charge. Orange glow |
| Power up | A loose spinning shell around the body. Gold tint. New wasps start at a hive and fly in |
| Collapsed | Fall to the ground and spread out |

### 6.4 How each wasp moves (a spring)

Per-wasp dynamic data: position, velocity, state (alive, dying or dead) and state time.

```text
acceleration = k * (target - position) - 2 * zeta * sqrt(k) * velocity + buzz(seed, time)
```

| Situation | What changes |
|---|---|
| Walking and running | Stiff and damped: `k` 220, `zeta` 0.75. The shape reads as a human |
| In the air | `k` drops from the head to the feet down to 25. The target is pushed back by `velocity × 0.12 s × (1 - height01)`, so the body becomes a stretched arc |
| Landing | `zeta` 0.3 for 0.35 s, which gives the spring wobble |
| Form change | Each wasp switches target at `changeTime + (1 - height01) × 0.45 s`, so the change flows top to bottom like a wave |
| Buzz | About 0.04 m at about 9 Hz, from cheap sine noise on the seed |

- `deltaTime` is capped at 1/30 s.
- The CPU sets `_ActiveCount` as a uniform. Wasps at index `_ActiveCount` or above that are still alive start dying: they get a burst away from `_LastHitPoint`, fall, shrink and become dead after 1.5 s.
- Wasps below `_ActiveCount` that are dead come back to life at the nearest standing hive and fly in.
- The draw call always draws the full maximum. Dead wasps collapse to zero size in the vertex shader, which costs almost nothing.

### 6.5 Drawing the wasps

- **Mesh:** a tiny wasp made in code, of about 24 triangles: a body and two wing quads. The stripes come from the shader.
  - Raz's `waspa_fly` is rigged and animated, so it cannot be instanced as it is. Its mesh may be used instead only if it is under about 200 triangles.
- **Shader:** a Built-in Render Pipeline vertex and fragment shader.
  - It reads from `StructuredBuffer`s by `SV_InstanceID` and points each wasp along its velocity.
  - The wings flap in the vertex shader, using `sin(time × 40 + seed)` on the wing vertices.
  - Lighting is simple Lambert plus ambient light, plus a tint and emission per form.
  - `#pragma target 4.5`, with no shadows.
- **Draw:** `Graphics.RenderMeshIndirect`, with `RenderParams.worldBounds` set to a box of about 60 m around the player. All buffers are made once in `OnEnable` and released in `OnDisable`.

### 6.6 Performance budget

- 60 FPS or better at 10,000 wasps, with all beekeepers alive.
- The swarm's compute shader and draw take under 2 ms on the GPU, and the CPU work around them takes under 0.3 ms.
- There are no garbage-collection allocations per frame.
- Measure this in the Profiler and write the numbers in `FEATURES.md`.

## 7. Layers and files

### Data (`Assets/_Project/Scripts/Data/Swarm/`, `.../Hives/`, `.../Beekeepers/`)

- `SwarmConfig`: wasp counts, power-up rate, collapse and respawn, spring values, jump lag, wave delay, buzz and form colours.
- `PlayerMoveConfig`: speeds, jump, gravity and the shield multiplier.
- `PlayerCombatConfig`: punches, kick, strike, meter gain, stun and shield reduction.
- `HiveConfig`: health and how long "under attack" shows.
- `BeekeeperConfig`, with **two assets**, `StickBeekeeper` and `SmokeBeekeeper`: health, speed, ranges, wind-up, cooldown and damage, plus an attack-kind enum.
- `SmokeConfig`.
- `WaveConfig`: a list of waves and the timings.

### Logic (`Assets/_Project/Scripts/Logic/...`, plain C#, every rule tested)

- `SwarmBody`: count, maximum, `Lose(amount, isShielded)`, `PowerUp(deltaTime, standingHives)`, collapse and respawn. Events `CountChanged`, `Collapsed` and `Respawned`.
- `SwarmFormMachine`: the states Body, Shield, StrikeCharging, StrikeRelease, PoweringUp and Collapsed, and which change is allowed when. Event `FormChanged`.
- `StrikeMeter`: fill, full and use.
- `AttackChain`: the punch combo window, the kick cooldown, and which attack happens and when its hit lands.
- `Hive` and `HiveSet`: health, under attack, destroyed, and how many still stand.
- `BeekeeperBrain`: the states GoToHive, AttackHive, ChasePlayer, WindUp, Recover, Stunned and Dead. It takes positions and `deltaTime`, and gives back a move direction and attack events. Stick and smoke behaviour differ only through config.
- `SmokeCloud`: area, lifetime, damage to hives, wasp loss and slow.
- `WaveDirector`: the timeline of waves and breaks, spawn requests, and "all waves cleared".
- `HiveDefenseSession`: owns all of the above, ticks them and decides Playing, Won or Lost. Randomness comes through the existing `IRandomSource` in `Logic/Random`.

### View (`Assets/_Project/Scripts/View/...`)

| Script | Job |
|---|---|
| `HiveDefenseRoot` | The scene's single composition root |
| `PlayerInputReader` | Input Manager, cached |
| `PuppetMotor` | `CharacterController`, gravity and Animator parameters through `Animator.StringToHash` |
| `ThirdPersonCamera` | Orbit and follow with smoothing |
| `SwarmGpuView` | Buffers, bone upload, dispatch and draw, with `WaspSwarm.compute`, `WaspInstanced.shader` and `WaspMeshFactory` |
| `HiveView` | Shows one hive |
| `BeekeeperView` | Raz's beekeeper prefab: its Run and Idle clips, a stick primitive on the hand bone, and a lean or bob tween for the attack. Simple steering, with no NavMesh, because baking needs a package in Unity 6 |
| `SmokeGrenadeView` and `SmokeCloudView` | Pooled with `ObjectPool<T>`, using a ParticleSystem |
| `HiveDefenseHudView` | UI Toolkit UXML and USS |
| `HiveOffscreenIndicatorView` | The edge arrows |
| `EndScreenView` | Win or lose screen |

### Editor

`HiveDefenseSceneBuilder`, in the same style as the existing `TownSceneBuilder`, builds the scene from Raz's prefabs:

- the ground;
- trees, bushes and ruins around the edges;
- 2 × `Beehive_LowPoly 1`;
- 4 spawn points;
- the player and the root.

Use **prefab variants** for the beekeepers and hives. Do not change Raz's original prefabs, and do not touch his `testing.unity`.

## 8. Tests (EditMode, written first and failing)

- `SwarmBody`:
  - A hit removes wasps, and the shield cuts the loss by 85%.
  - Power up adds 250 per second per standing hive and stops at the maximum.
  - Falling below 600 collapses the swarm, which respawns with 2,500 after 3 s.
- `SwarmFormMachine`:
  - Shield blocks attacks.
  - Power up only starts while standing still, and a hit cancels it.
  - Strike only starts with a full meter.
- `StrikeMeter`: +12% per hit, and full at 100%.
- `AttackChain`:
  - Three punches inside the window, and the window resets after it.
  - The third punch has the heavy values.
  - The kick has a cooldown.
- `Hive` and `HiveSet`:
  - Damage, destroyed, and "under attack" for 2 s.
  - Losing both hives means Lost.
- `BeekeeperBrain`:
  - It goes to the nearest hive and retargets when that hive falls.
  - It chases within 7 m and gives up beyond 14 m.
  - It hits only after the wind-up, and a stun delays the attack.
  - The smoke beekeeper throws from 14 m.
- `SmokeCloud`: damage per second to a hive and to the swarm, the slow, and expiry.
- `WaveDirector`: the counts per wave, the breaks, and Won after wave 5.
- PlayMode, if time allows:
  - The scene loads with no errors.
  - The swarm buffers are made and released.

## 9. Milestones

Commit and push after each one, so there is always a playable build.

| # | Time | Result you can see |
|---|---|---|
| M0 | 0:00 to 0:15 | Claim in `FEATURES.md`, worktree, Mixamo import, empty scene from the builder |
| M1 | 0:15 to 0:55 | **The core:** the puppet walks, runs and turns with the camera, and 5,000 wasps hold its shape and buzz. Profiler at 10,000 |
| M2 | 0:55 to 1:35 | Hives, stick beekeepers, punches, hits remove wasps, HUD count, hive bars and edge arrows, losing |
| M3 | 1:35 to 2:05 | Power up with streams from the hives, smoke beekeepers and smoke, waves and winning |
| M4 | 2:05 to 2:30 | The jump spring and the shield form |
| M5 | 2:30 to 2:45 | The strong strike and the kick |
| M6 | 2:45 to 3:00 | Full verification, `FEATURES.md` and the pull request |

If time runs short, cut from the bottom: M5, then M4. M1 to M3 are the game.

## 10. Left out on purpose

- Levels, upgrades and saving.
- Sound, beyond hooks.
- NavMesh.
- Combos beyond the 3-punch chain.
- Multiplayer.
- Changing Raz's assets.
