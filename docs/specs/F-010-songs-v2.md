# F-010 songs, version 2: H J K L, squads and hidden songs

The plan for the next version of the swarm's songs, chosen from three independent designs by a panel of three judges. It adds to [F-010-wasp-swarm.md](F-010-wasp-swarm.md); where the two differ, this file wins for songs, keys, squads, aiming and the HUD.

The key table was checked by script: no song can cast inside another, every song casts on its own last note from silence, and the three laws in section D hold.

```text
A. SONG TABLE: 14 songs. Attack (red) 7, Defense (blue) 5, Power (gold) 2. New songs: Hydra, Scythe, Lance, Nest, Maelstrom, Wings, Honeycomb.
The owner's candidates: Volley became Hydra, Trap became Nest, Guard became Honeycomb, and Lance stays.
Slots: S = Strike, G = Guard, P = Post (something left on the map); see C. Share is a fraction of the living swarm. All song damage fills Fury at 1 per 2 damage.
Song      Cat Keys      CD    Lasts                Share Slot Rule (every number is a field on the song asset) | the map problem it answers
Sting     A   J K H     1.5s  4s or target dies    .30   S  Arcs to the target (12 m max) and wraps it: 25 dmg/s from 0.6 s, when it arrives. | a lone stick at a hive. KNOWN AT START.
Tornado   A   J K K H   10s   1.5s                 .37   S  At 0.3 s, when the column is at full height: 30 dmg and 6 m/s knockback within 5 m. | surrounded.
Meteor    A   J K L K   9s    1.9s                 .35   S  As today: lands at 1.3 s on a point 18 m away at most, 60 dmg and 0.5 s stun within 3 m. | a crowd at the far hive.
Hydra     A   J J L     6s    1.6s                 .37   S  Three heads bite the 3 nearest within 9 m at 0.7 s, 40 dmg each. A spare head strikes the ground. | several spread out.
                                                          Built: 60 wasps; 4 heads from level 4 and 5 from level 7 (section H).
Scythe    A   K L H     4s    1.0s                 .37   S  At 0.3 s, 45 dmg to everyone within 4.5 m in the half-disc facing the aim. | a pack in front of you.
                                                          Built: 30 wasps; the aim faces the most threatening beekeeper within 6 m.
Lance     A   K L L H   8s    1.3s                 .37   S  0.4 s draw, then at 0.5 s a line 14 m long and 1.2 m wide: 60 dmg to each beekeeper on it, pushed sideways at 5 m/s. | a far smoker, a file walking in.
                                                          Built: 70 wasps.
Nest      A   K L J H   8s    until set off, 60s   .15   P  Lobbed to the aim (10 m max), arms at 0.6 s. The first beekeeper within 2.5 m sets it off; 0.3 s later 70 dmg and 1 s stun within 3.5 m. | a spawn lane.
                                                          Built: 100 wasps, in a farm phase too; it stays armed until a beekeeper sets it off, 60 s at most.
Dome      D   H K J     8s    5s                   .35   G  As today: hits on the Piper cost 85% fewer wasps, Piper at x0.5. | a melee crowd on you.
Maelstrom D   H K K J   14s   3s                   .35   G  A 5 m disc at the aim (12 m max). Beekeepers inside are pulled to its centre at 3 m/s and cannot start an attack (0.2 s stun, renewed). | a crowd on a hive.
Gust      D   H L K     6s    0.8s                 pulse -  Clears smoke within 6 m (as today). Beekeepers within 3 m are pushed out at 4 m/s. | smoke on you or on a hive.
Wings     D   H L L K   12s   5s                   .30   G  Piper at x1.6 speed, stick hits cost 50% fewer wasps, and smoke does not slow the Piper. | running between hives.
Honeycomb D   H J L K   15s   its wave; farm 60s   .20   P  A shell on one hive: 75% less damage from sticks and smoke, and each stick hit on it deals 10 back. | both hives attacked.
                                                          Built: 150 wasps, in a farm phase too; it lasts until the wave it guards is over, or 60 s cast in a farm phase with no wave.
Hive Call P   L J L     3s    see C                rest  -  As today: rooted, +100 wasps/s per standing hive. Ends when the Piper moves, an Attack or Power song is cast, the swarm is full, or the Piper is hit while no Guard song runs.
Colossus  P   L J K J L Fury  12s                  rest  -  As today; its category moves from Attack to Power. Its notes are revealed when Fury first fills, or when it is played whole before that.
Built for the five new songs: every size, reach and damage follows the swarm level (section H), and every song's first cast in a match is free (section M).
Every song except the Colossus casts even with nothing to hit. An aimed song strikes 6 m ahead of where the Piper faces, and Sting wraps air there for 1 s.
This lets the player find every song in a wave break, and no cast is ever refused. That matters because SwarmSongCaster starts the cooldown even on a refusal. It changes Sting's CanCast.

B. CHOREOGRAPHY: launch (L) | peak (P) | return (R). One target function per song.
Helpers shared by every slot song:
- u = (squad - lo) / (hi - lo) is the wasp's rank in its squad, evenly spread (see the squad attribute in C).
- t = seconds since the cast. tr = seconds since release; release is p1.w, and -1 while the song runs.
- Peel = smoothstep(0, .3, t - lane x .25) blends RiverTarget into the shape. The river's head leaves first, so every launch is a ribbon, never a pop.
- Home = smoothstep(0, .5, tr - u x .25) blends back to the river. No song needs any state from the previous form.
- Heat (glow): .4 to .8 through a wind-up, 1.0 for 0.15 s on the impact frame, then .5. The river's glow drops from .1 to .05 while any slot runs.
- Springs: strikes use today's meteor spring (400, .8, capped at 70 m/s); held shapes use the shape spring. Colour = category colour x heat.
- Impact feel, CPU only, with no post-processing: a 50 ms swarm hitstop (sim delta 0) and a 0.15 m camera shake on Hydra, Lance, Meteor, Nest and Colossus hits.
Sting     L A comet ribbon on today's Bezier arc (apex 1.2 m + .25 m per m). The head is tight and brightest; the tail widens with u by up to 0.6 m.
          P Two counter-rotating gyroscope rings (u<.5 tilted +35 deg, the rest -35), 0.8 m around the target's chest. They squeeze to 0.55 m with a glow spike on each 0.25 s tick.
          R On death or time-out the cocoon pops to a 1.6 m sphere in 0.15 s, then streams home on the reversed arc. CPU: p0 = target xyz (live), p1.xyz = second target (for the duet).
Tornado   L It grows from the ground up in 0.3 s. Wasps above the current top wait in a spinning 1.5 m skirt and spiral up as the top passes them.
          P Today's funnel. At 0.3 s the flung 25% shoot out flat to 5 m: a knee-high shock ring that shows the reach. The spin speeds up from 6 to 9 rad/s.
          R It collapses from the top down, unwinding outward like a skirt. CPU: p0 = (5, 3, spin, .3 hit time), p1.xy = (wall radius, second hit time or -1) for the duet.
Meteor    L Today's spiral climb to 12 m and ball. The 10% with u>.9 drop early and draw the 3 m target ring on the ground, turning.
          P In the last 0.2 s of the hang the ball tightens from 1.6 to 1 m and brightens (a heartbeat). Then today's streaked dive, and the impact ring flashes white.
          R Today's ring burst, then home. CPU: p0 = (impact xyz, height), p1.xyz = (rise, dive, radius).
Hydra     L 0 to .45 s: head = floor(3u), and the place on the neck s = frac(3u)^.6 packs wasps into the head. Three Bezier necks rise behind the Piper; the heads rear back to 4 m, swaying.
          P .45 to .7 s: the heads snap onto their targets on an ease-in t^3 and the necks whip straight. At 0.7 s each head bursts into a 1.2 m spiky splash and bite-shakes 0.2 m at 20 Hz for 0.35 s.
          R The necks recoil into the river like springs, head first. CPU: p0 = (t1.xz, t2.xz), p1.xyz = (t3.xz, ground y).
Scythe    L 0 to .25 s: a crescent blade reaching from 1 to 4.5 m off the Piper, curved .35u^2 rad, 0.9 m up, cocked 30 deg back.
          P It sweeps 200 deg on an ease-out cubic. Each wasp lags by phase01 x 0.6 rad, so a fan-shaped smear trails the edge. Glow 1 at 0.3 s.
          R The smear catches up, and the blade crumbles into embers that sink 0.8 m, then fly home. CPU: p0 = (start angle, signed sweep, 4.5, .25); the centre is the live Piper.
Lance     L The draw, 0 to .4 s: a needle on the aim axis from 5.5 m to 0.5 m behind the Piper, 1.2 m up, .3 to .05 m thick, quivering at 30 Hz, its tip white.
          P .4 to .65 s: each wasp flies 14 m forward on an ease-in, the tail up to .08 s later, so the spear stretches into a 14 m streak. The tip fans into a 1.5 m crown.
          R The streak breaks into sparks that drift 1 m sideways, then fly home. CPU: p0 = (origin.xz, dir.xz), locked at release; p1.xyz = (14, .4, .08).
Nest      L A lobbed comet on a high 0.5 s arc splashes into a 0.9 m ball, 1.3 m off the ground.
          P The ball breathes (+-.12 m at .8 Hz), and the 15% with u>.85 patrol the 2.5 m trigger ring. When set off, it crunches to .35 m in 0.3 s, then erupts as an urchin of 24 spikes
            (spike = floor(24 phase01), Fibonacci directions) that reach 3.5 m in 0.12 s.
          R The spikes droop and the wasps stream home. CPU: p0 = (position xyz, trigger time or -1), p1.xy = (2.5, 3.5).
Dome      L Woven ring by ring from the ground in 0.4 s. Each wasp leaves at height x .35 s and spirals into its Fibonacci point, placed by u.
          P Today's turning shell. Each blocked hit sends a ripple from the hit side (a 0.35 m bump whose front is at 4(t - hitTime) rad) with a white-blue flash.
          R It unzips from the top down and pours into the river. CPU: p0 = (2.2, hitDir.xz, hitTime).
Maelstrom L A stream pours to the spot and lands as a rim ring at 5 m.
          P A three-arm galaxy: rr = 5(1 - frac(u + .35t)), angle = arm x 120 deg + 2.2 ln(5/rr) + 2.5t, height = .3 + 2.5(1 - rr/5)^3.
            Wasps flow down the arms, the eye lifts into a 2.5 m spout, and the beekeepers slide in with them.
          R The arms unwind out to 6.5 m as the spin slows, then the wasps fly home. CPU: p0 = (centre xyz, 5), p1.xyz = (3 arms, .35, 2.5).
Gust      A pulse on every wasp within 6 m, whatever it is doing. L 0 to .12 s: an inhale; every wasp is pulled 0.5 m toward the Piper.
          P A ring grows from .6 to 6 m. As its front passes, wasps are pushed out 1.2 m and lifted .4 m, so it ripples through the Dome and the attacks. River wasps ride it out.
          R The ring slows and the river glides back. CPU: one global float4, _Pulse = (x, z, start, 6).
Wings     L Two streams climb the Piper's back and unfold to a 4.5 m span in 0.4 s.
          P Each wing is a curved triangle (u along the span, phase01 across the chord) flapping +-35 deg at 1.5 Hz and glowing on the downstroke. The tips lag with speed and shed a trail.
          R The wings fold forward, wrap the Piper in one spiral turn and drop into the river. CPU: p0 = (4.5, 1.5, 35, Piper speed 0..1).
Honeycomb L A stream arcs to the hive and winds up around it in a helix (0.6 s).
          P Wasps snap to the edges of hexagon cells (side .45 m) on a cylinder of radius 1.6 m and height 2.2 m plus a low cap, turning at .2 rad/s. A hit flashes the nearest cell white and bulges it.
          R The cells pop one by one in u order over 0.6 s, each bursting .4 m out, then the wasps fly home. CPU: p0 = (hive xyz, 1.6), p1.xyz = (hit angle, hit time, 2.2).
Hive Call L The river corkscrews up into a six-point gold crown: radius x (1 + .25 cos 6 theta), with the points lifted .4 m.
          P New wasps ride gold sky rivers in from each standing hive, and the crown thickens. R It melts down into the river.
Colossus  L Today's build from the feet up. P On each landed hit, the striking bone's wasps flare to glow 1 and 10% spray off as sparks (global _Strike = bone, time).
          R It pours down like sand from the head, wasps leaving by height01 over 0.8 s.

C. SLOT RULE
Squads
- The unused float in WaspStatic becomes squad = frac(0.5 + index x 0.7548777), so WaspStatic stays 48 bytes.
- The living wasps are the first N indexes. A band of width w therefore holds w x N wasps, within +-4 for every N from 50 to 4,000 (checked), and its wasps spread evenly along the river.
- Why not 0.618 (the tactics design's number): Lane already steps by 0.618, so each band would be one solid stretch of river and leave a gap in it. With 0.618 the Strike band covered only 5 of the river's 10 tenths (checked).
Bands
- Bands never move: Post [0, .20), Guard [.20, .55), Strike [.55, .92). Seeds [.92, 1) always stay river.
- A song takes [band start, band start + share); the rest of its band flies as river. At 2,000 wasps that is up to 400 Post, 700 Guard and 740 Strike.
- No wasp ever jumps between two running songs.
Which song goes where
- Strike: Sting, Tornado, Meteor, Hydra, Scythe, Lance. Guard: Dome, Maelstrom, Wings. Post: Nest, Honeycomb. Gust is a pulse with no slot.
- So one Attack and one Defense always run together, plus Nest (a second attack) or Honeycomb (a second defense), plus a Gust on top.
What the GPU gets
- float4 _Slots[9]: per slot a header (form, start time, lo, hi), p0 and p1, with p1.w the release time.
- A wasp inside a live slot's [lo, hi) runs that slot's function. Every other wasp runs the whole-swarm form (River, Hive Call, Colossus, Collapsed).
- Then the Gust pulse is added. Its time test reads one uniform, the same on every thread.
Casting and ending
- A cast replaces only the song in its own slot. The old rule stops at once (no more damage, and an impact that has not landed is lost), and its wasps spring into the new launch.
- A rule ends at release (time up, target dead, Nest set off). The slot then plays a 0.6 s return beat, which is visual only, and empties. A cast during the return replaces it.
Hive Call and the Colossus
- Hive Call ends the Strike song and keeps Guard and Post. Its crown is every wasp outside a live squad.
- Casting a Defense keeps Hive Call running, so Dome then Hive Call is the safe refill.
- The Colossus ends Strike, Guard and Hive Call, and their wasps fly straight into the build. It keeps Post and uses every wasp outside the Post squad.
- Nothing casts while the Colossus stands, and notes are not sent to the caster then, so presses made out of habit cannot leak into the next song's window.
Small swarm and collapse
- Rules never read the wasp count, so a thin Hydra hits just as hard; its skinny necks are the warning. No song is refused because the swarm is small.
- Below Low (600) the count bar turns red and the L pip shimmers.
- Collapse (below 240): every slot ends at once with no return beat. Songs are blocked for 3 s and the swarm respawns with 1,000, as today.
Hidden duets
- Each is a parameter change to an existing song. A duet is listed in the songbook once seen, under a "Duets 0/4" counter.
- Eye of the Storm: Tornado cast while the Dome stands moves its wall out to 3 m around the shell and hits a second time at 1.2 s.
- Swallowed Star: Meteor cast while a Maelstrom runs lands on its eye, and its radius grows from 3 to 5 m.
- Kicked Nest: a Gust within 6 m of an armed Nest sets it off at once.
- Twin Sting: Sting cast while the Wings flap splits into two ropes on the two best targets, 25 dmg/s each.

D. KEYS
H J K L are holes 0 to 3; 1 to 4 and keypad 1 to 4 also play them. SongBook gets holeCount 4.
Three laws, so each find predicts the next:
  1 The first note is the colour: H blue, J and K red, L gold. Every lit beginning leads to one colour only.
  2 Every blue song is a red song played backwards. The twins are Sting/Dome, Tornado/Maelstrom, Scythe/Gust, Lance/Wings and Nest/Honeycomb:
    wrap them / wrap me, spin out / spin in, cut a half circle / blow a full one, throw the swarm / ride it, a bomb left on the ground / a shell left on a hive.
    An attack ends on H exactly when it has a twin. Hydra and Meteor end on other notes, and their backwards notes are not a song.
  3 Gold songs read the same both ways (L J L, L J K J L), and no other song does.
check_final.py output on this table, condensed to fit:
  pair check: 182 ordered pairs, 0 early casts | suffix pairs: none | songs inside the Colossus: none
  from silence: 14/14 cast exactly themselves on their last note | each song played while it cools casts: nothing
  1 stray note before a song: 0/56 cast another song first
  2 stray notes before a song: 20/224 do: HK+Sting/Tornado/Meteor/Hydra->Dome, HL+Scythe/Nest/Lance->Gust, JK+Dome/Maelstrom/Gust/Wings/Honeycomb->Sting,
    KL+Dome/Maelstrom/Gust/Wings/Honeycomb->Scythe, JJ+HiveCall->Hydra, JJ+Colossus->Hydra, LJ+Colossus->HiveCall.
    In all 20 cases the two strays are a glowing beginning, so the player really did play that song's notes. Dark (unlit) strays never misfire: 0 cases, checked.
  laws 1, 2 and 3: hold | beginnings that lead to two colours: none
  lit beginnings: 2 notes 7/16 (HJ HK HL JJ JK KL LJ); 3 notes 8/64 (HJL HKK HLL JKK JKL KLJ KLL LJK); 4 notes LJKJ
  random masher, mean notes to first cast: 3-note songs 69 to 77, 4-note songs 228 to 247
search2.py went through every table that obeys the three laws with these song lengths. None has fewer than 20 two-stray misfires.
Of the 60 tables that score 20, I took the one that puts three songs on the known song's stem (J K).
Shared beginnings (first note | first two notes):
  J: Sting, Tornado, Meteor, Hydra | J K: Sting J K H, Tornado J K K H, Meteor J K L K | J J: Hydra alone
  K: Scythe, Nest, Lance           | K L: all three, Scythe K L H, Nest K L J H, Lance K L L H (K always leads to K L)
  H: all five Defenses             | H K: Dome, Maelstrom | H L: Gust, Wings | H J: Honeycomb alone
  L: Hive Call, Colossus           | L J: both
Twin attacks that share a stem have twin shields that share one too (J K..H gives H K..J). So a stem found on one side points to a stem on the other.
The notes follow the motion:
- Sting climbs and strikes down; Tornado spins on K K; Meteor climbs to the top and drops; Hydra coils twice and strikes high.
- Scythe swings up and down; Lance draws back (L L) and releases; Nest lobs up and lands; the gold songs go out and come back.
Known from the start: Sting (J K H).
Likely order for a curious player:
  1 Dome. Sting backwards is the first experiment anyone tries, and the banner teaches law 2.
  2 Tornado and Meteor. J K still glows after Sting, so the player tries other third notes (J K K and J K L glow). Then Maelstrom, by reversing Tornado.
  3 Gust (H L glows, and smoke on the Piper makes H shimmer), and Hive Call (revealed at the first red count; it teaches law 3).
  4 Scythe (K only ever glows into K L), then Nest and Lance (K L J and K L L glow), then their twins Wings and Honeycomb.
  5 Hydra (J J glows, and leads only to Hydra).
  6 The Colossus when Fury first fills, or earlier for a player who follows L J K on from Hive Call.
Nudges light one first note, never a whole song. Smoke on the Piper, or a hive under attack, makes H shimmer. A red count between waves makes L shimmer.

E. AUTO-AIM AND INPUT
The mouse wins while it moves
- The mouse is live for 1.0 s after it moves 4 px, and while a button is held. The aim is then its ground point, and the target is the beekeeper nearest that point within 3 m (today's rule).
Auto-aim while the mouse is idle
- One plain C# AutoAim class with five named rules and tests.
- Threat score = the beekeeper's distance to the Piper, minus 6 m if it stands within 3 m of a standing hive. The lowest score wins; ties go to the lower index.
- A chosen target is kept for at least 1 s while it lives and stays in range, so the aim never flickers.
- Threat: Sting (12 m) and Scythe (6 m, its direction).
- ThreeNearest: Hydra (9 m): the 3 nearest the Piper, or the 3 nearest the cursor while the mouse is live.
- Line: Lance (14 m). Of the lines from the Piper through each beekeeper in range, the one whose 1.2 m strip holds the most beekeepers wins; ties go to Threat.
- Crowd: Meteor (18 m), Nest (10 m) and Maelstrom (12 m). The beekeeper with the most others within 3.5 m wins; ties go to Threat.
- Hive: Honeycomb goes on the hive hit most recently in the last 3 s, otherwise on the nearest standing hive.
- Self: Tornado, Dome, Gust, Wings and Hive Call need no aim. With nobody in range, an aimed song strikes 6 m ahead of where the Piper faces.
- A red chevron always marks the current auto target, so no song fires at a surprise.
Keys
- WASD walks, camera-relative (left hand). H J K L play notes 1 to 4 (right hand, index to pinky). The mouse is optional. R restarts, Esc pauses.
- Colossus: WASD steers. Punch = Space or left mouse (today's 3-hit chain). Kick = Left Shift or right mouse. J and K no longer punch or kick.

F. HUD
1 Four pips in their family colours. Glow depth: 1 note dim, 2 lit, 3 or more bright and pulsing. The glow is white on the way to a known song and the family colour on the way to an unfound one.
2 A songbook rail in three colour groups, with twins side by side. Unfound tiles show only their length (? ? ?). After the first Defense find, a locked twin next to a found attack shows a mirror mark.
  Counters: Attack 3/7, Defense 1/5, Power 1/2, Duets 0/4. Hive Call's and the Colossus's letters show once they are revealed.
3 A banner and chime on a song's first cast, such as "NEW SONG  HYDRA  J J L". The first blue find adds "Played backwards, it shields", the first gold one "Gold reads the same both ways". Play never pauses.
4 Three slot rings beside the wasp count (Post, Guard, Strike) fill with the running song's colour and drain as its time runs out.
5 In the world: only the red chevron, plus a thin ground reticle of the song's shape once the lit notes lead to exactly one known aimed song. The wasps draw every other warning themselves.

G. BUILD ORDER
M1, the slot engine with no new songs:
- SongSlots in Logic (bands, replace, release, Hive Call, Colossus, collapse), tests written first.
- The squad attribute; _Slots[9], _Pulse, the band lookup and Peel/Home in the shader.
- Port Sting, Dome, Tornado and Meteor, one per commit, and move Gust to the pulse.
- holeCount 4 and this key table, with an EditMode test that replays check_final.py (pairs, silence, cooling, one stray note, the three laws).
- H J K L input; punch and kick on Space and Shift.
- Done when Dome and Sting run together in Play Mode and every existing test stays green.
M2, discovery and aim: AutoAim and the chevron, casting with nothing to hit, pip colours, nudges, the Hive Call reveal and the songbook rail and banner.
  HintNotes must also say whether a path leads to a known song, and the Colossus must be revealed on SongNotReady.
M3, attack spectacle: Hydra, Lance and Scythe; Tornado growing from the ground, Meteor's ring and heartbeat, Sting's squeeze; hitstop and shake.
  Still test: one frame per attack from the game camera, and a teammate must name each song from it.
M4, defense: Wings, Maelstrom, the Post slot with Nest and Honeycomb, the Dome ripple. The same still test for the Defenses.
M5: duets, the reticle, the Hive Call crown and the Colossus pour. Tune shares at 2,000 and 4,000 wasps.
  Profile with all three slots and a Gust running; target: the compute pass under 0.5 ms at 4,000. Write the numbers and how it was verified into FEATURES.md.
If time runs short, cut in this order:
  1 Duets. The laws carry the exploration at no extra cost.
  2 The reticle; the chevron stays.
  3 The wing flap: the wings stand still, and the rule stays the same.
  4 Maelstrom. Tornado's backwards notes then play nothing, and the songbook notes that one gap.
  5 The Post slot: Honeycomb moves into Guard, and Nest goes.
  Never cut the slot engine, Sting, Dome, Gust, Hive Call, Hydra or Lance.
Three biggest risks:
1 The refactor. SwarmFormMachine, SwarmGpuView and the shader handle a single form today, and other agents are editing them now.
  Build SongSlots beside them and port one song at a time behind the existing tests, or the merge breaks songs that work.
2 Thin or muddy shapes: three squads and the river at 2,000 wasps, seen from 13 m. Exact squad counts, glow only on impact, a dimmer river and the still test guard against this.
  Band widths, cell sizes and wasp scale are all data. Nobody has measured the cost of Honeycomb's hex snapping or Hydra's head packing, so profile them in M3 and M4, not only at the end.
3 Discovery pace. Only Sting is known, and survival needs Dome, Gust and Hive Call. The backwards law gives Dome at once, and Hive Call is revealed at the first red count.
  Playtest with someone new: they should find at least 6 songs by wave 3.
Where the judges disagreed, I chose:
- Colour by first note, not by contour: it colours one pip on the first press, and the backwards law still works because twinned attacks end on H.
- Gust as a pulse, not a Defense-slot song: it never cancels a running Dome, which was the trap the judges found.
- Hive Call keeps the Guard song (tactics) instead of ending it (spectacle): Dome then Hive Call becomes a defend-and-heal combo to discover.
- No 80-wasp floor (discovery): a refusal still burns the cooldown and blocks discovery during a lull.
- Duets stay (player judge) but are cut first (engineer): the laws now carry the exploration the duets were meant to give.

For the owner:
- The three laws fix most keys: the first note is the colour, backwards is the twin shield, and gold reads the same both ways. Keep them?
- Hive Call is revealed (notes shown, not yet known) the first time the swarm turns red. You could keep it fully hidden, or make it a second starting song.
- Wings (a speed shield) is the fifth Defense, chosen over Bristle (spikes that hit back). Swap them if you prefer.
- Four hidden duets, with only a counter showing. You could list their names as hints.
- The Colossus punches on Space and kicks on Left Shift. Sting now starts its damage when it arrives (0.6 s), not at the cast.
```

## Added by the owner after the plan: nests, swarm level, the giant and the core loop

These sections were agreed with the owner on 2026-11-11. Where they differ from the plan above, they win.

### H. Nests and swarm level

The swarm grows during a match. Strength comes from the **swarm level**, never from the live wasp count, because the count is also the player's health: a hurt swarm must not hit softer. One exception since section O, by the owner's decision: the Sting sends one sting for every 400 living wasps.

| Value | Settled |
|---|---|
| Starting wasps and capacity | 800 |
| Nests on the map | 8, in Raz's `WaspNestTree_LowPoly` trees, each at least 12 m from a hive |
| Open at the start | 2, within 10 to 14 m of the middle |
| Opened later | 2 after wave 1, 2 after wave 2, 1 after wave 3, 1 after wave 4 |
| Collecting | Stay within 2.5 m of an open nest for 2 s. Leaving the circle or being hit starts the 2 s again |
| Each nest gives | +400 capacity (4,000 at most), +400 wasps now, +1 level. The nest is gone; the tree stays |
| Size per level | × (1 + 0.12 × (level − 1)) on every song's radius, reach and height |
| Damage per level | The same multiplier |
| Hydra's heads | 3, then 4 from level 4 and 5 from level 7 |
| Collapse | Below 12% of capacity. The respawn brings 50% of capacity |
| Low warning | Below 30% of capacity |

- Hive Call refills up to the capacity, at its usual rate.
- A collected nest **hums**: the first two notes of one song the player has not found are shown on its empty slot in the songbook. A nest collected when every song is found hums nothing.
- Wasps knocked off by a hit stay absolute numbers, so a bigger swarm is also a tougher one.
- Waves 4 and 5 grow to match the stronger swarm. The playtest bot sets the numbers: lost without nests, winnable with them.
- The HUD shows the level, a marker at the screen edge for each open nest, and a ring that fills while collecting. The end screen counts nests.
- Left out for now: beekeepers that destroy nests, and wild wasps that defend them.

### I. The giant, version 2

The owner played the Colossus and found it too short and too shallow.

| Value | Settled |
|---|---|
| Lasts | 20 s, plus 2 s for each beekeeper the giant kills, 30 s at most (was 11 s, the playtest bot's after section O) |
| Fury | 1 point per 3 damage (was 2), so the giant stays a highlight. The playtest bot set 4, then 7 with the giant of 20 s, and the giant's own blows fill none (section M) |
| Size and damage | Follow the level the giant stood up at: every damage by the damage multiplier, every radius, reach and the leap's range by the size multiplier. Cooldowns, stuns and throws stay |

While the giant stands, H J K L (and 1 to 4) are its moves and do not reach the songs:

| Keys | Move | Rule |
|---|---|---|
| J | Punch | The existing three-hit chain; the third is heavy |
| K | Kick | As today |
| L | Ground slam | 40 damage and a 4 m/s throw within 5 m of the giant, landing 0.3 s after the press. 3 s cooldown |
| H | Roar | Stuns every beekeeper within 6 m for 1.2 s, 0.2 s after the press. No damage. 6 s cooldown |
| K K | Spin kick | The second K within 0.6 s of the first: 55 damage all around within 3.5 m, 0.3 s after it starts |
| L L | Leap slam | The second L within 0.6 s of the first: the giant leaps to the aimed spot, 10 m at most, lands after 0.6 s and deals 70 damage and a 6 m/s throw within 4 m |

- Space or left mouse still punches, and Left Shift or right mouse still kicks. A key and its button on the same frame are one press.
- A phrase does not wait for the cooldown of its first move. A second press while the first move is still in the air waits, and the phrase starts as that move lands, so a quick K K is not lost. Any other move in between breaks it, and a third press is no second phrase.
- The note that casts the giant is not a move. When the giant ends, a move under way is dropped and nothing it waited for starts, so the next notes reach the songs as usual.
- The leap goes to the aim as it takes off: the mouse's ground point while the mouse is live (the chase camera's look point; in the chase the mouse is live for 1 s after it turns the view and while a mouse button is held), otherwise the most threatening beekeeper within the Sting's reach, otherwise 6 m ahead. In the air the giant cannot be steered or attack. If its time runs out in the air it lands, and hits, first. A collapse ends it at once, in the air too.
- Every blow is reported for the hit feel (section O). The heavy ones are the slam, the leap's landing and the third punch; the roar hurts nobody, so it reports nothing. Each kill by any blow adds the 2 s.
- While the giant stands it steers relative to the camera: W walks the way the camera looks on the ground, D to its right.
- A strip on the HUD names the four moves while the giant stands, and a ring shows the time left.

### J. The core loop and its feedback

- **Every few seconds:** read a threat, choose, play three or four notes, watch the swarm answer, see the result. Each step has one clear signal: a wind-up lean or a ring on the ground, a pitched note and a glowing pip, a change of shape and colour with a flash of light, and a hit that lands when it looks like it lands.
- **Every wave:** a banner when a wave starts and when it is cleared. The break is time to try phrases and fetch a nest.
- **Every match:** the end screen shows hives saved, wasps left, songs found, nests collected and a grade. R restarts at once.
- **Impact feel:** a 50 ms pause of the swarm and a small camera shake on the big hits, as section B says.
- **The playtest bot** plays whole matches through the rules: an idle player loses, a player with only Sting struggles, and a skilled player wins. It runs after every batch of songs.

## The lecturer's feedback: the swarm is the player, an economy, and farm and survive phases

Agreed with the owner on 2026-11-11, after the lecturer saw the game. The deadline is the week after. Where these sections differ from anything above, they win. The squads, the H J K L keys, auto-aim, the hidden songs and the night look all stay.

Build order, agreed: the swarm as the player, then phases and the economy, then the fill-in song table, then nests (section H), then more songs, starting with the two towers (Honeycomb and Nest).

### K. The swarm is the player

- There is no Piper. WASD moves the swarm's **core**, an invisible point the camera follows. The swarm idles as a ring around the core and trails behind it as the river.
- Beekeepers notice, chase and hit the core with the ranges they used for the Piper. A hit knocks wasps off, as before.
- Songs that were centred on the Piper are centred on the core.
- The Colossus is the whole swarm. The core rides inside it, so nothing is left standing behind, and when it ends the swarm is where the giant stood.
- In the code, "Piper" becomes "core" wherever F-010 uses the word, so nothing is named after something that is not there.

### L. Farm and survive

A match is five rounds. Each round is a farm phase and then a survive phase.

| Value | Settled |
|---|---|
| Farm phase | 40 s, and 45 s in the first round |
| Call the wave early | Enter. Each second skipped gives 5 wasps, up to the capacity |
| Survive phase | The wave. It ends when every beekeeper of the wave is dead |
| Waves (sticks + smokers) | 4 + 0, 5 + 1, 5 + 3, 22 + 14, 23 + 14. Sticks come first, one beekeeper every 2.5 s |
| A stick | 90 health. Hits a hive for 6, or knocks 72 wasps off the core, every 2 s. A smoker has 40 health |
| Win | The fifth wave is cleared |
| Lose | Both hives are destroyed |

- No beekeeper is on the map during a farm phase.
- **Songs cost nothing in a farm phase**, so trying phrases is always free.
- A banner and a timer always say which phase it is.

### M. The economy

Wasps are the one currency. They are the player's health, the price of an attack, the material that heals a hive, and what a tower is made of.

**Spending**

| Use | Cost | Notes |
|---|---|---|
| Sting | 5 for each sting | One sting for every 400 living wasps (section O), so a cast at the starting 800 costs 10. Cheap on purpose, so the player always has a move |
| Gust | 20 | |
| Dome | 40 | |
| Tornado | 60 | |
| Meteor | 90 | |
| Hydra | 60 | |
| Scythe | 30 | |
| Lance | 70 | |
| Colossus | 0 | Fury is its price: 1 point per 7 damage of the songs, 100 to fill. The giant's own blows fill none |
| Hive Call | 0 | |
| Healing a hive | 3 wasps for each point of health | Hold E within 3 m of a damaged hive: 60 wasps a second flow in. A hive has 450 health |
| Towers | Honeycomb 150, Nest 300 | The wasps stay there, so a tower costs its price in a farm phase too. One Post slot: a new tower replaces the old one |

- Costs apply only in a survive phase, except a tower's.
- The first cast of each song in a match is free, in either phase, so finding a song is never punished.
- The spent wasps are the ones that do not come home: when the song releases, that many of its squad burn out.
- A cast that would take the swarm below its collapse line is refused with "Not enough wasps". A refused cast starts no cooldown and teaches nothing false: the notes still fill in the table.

**Earning**

| Source | Amount |
|---|---|
| A hive at half health or more | Makes 6 wasps a second into its own store, 300 at most |
| A hive below half health | Makes nothing, which is the reason to heal it |
| Collecting a store | Automatic within 3 m of the hive, 150 wasps a second, up to the capacity |
| Hive Call (L J L) | Calls every store from anywhere, while the core stays rooted |
| A nest | Section H |
| Calling a wave early | 5 wasps for each second skipped |

**The decisions this makes**

- Spend on attacks to end the wave fast, or keep the wasps as health.
- Heal a hive now, which costs wasps, to keep its income, or let it stop producing.
- Fly back to collect a store, or stay in the fight.
- Station wasps as a tower: a shell on the hive you are leaving, or a nest on the lane the wave comes from.

The playtest bot plays three plans through whole matches: attack only, heal only, and a mix. The economy is right when none of the three always wins and doing nothing always loses. The numbers above are the starting point for that tuning. With the towers it also plays towers with healing only.

**What the playtest bot measured**

`EconomyPlaytestTests` plays whole matches through the rules at 1/30 s with seven plans on five seeds, prints a table to the log (lines starting `[F010] bot:`, with one line per plan of the damage each song dealt) and checks the owner's targets here and in section H: doing nothing loses, by round 3; Sting alone, attacking only, healing only, towers with healing only, and the mix that never fetches a nest each win at most 2 (lost without nests; Sting alone was held to 1 until section O); the same mix fetching the nests wins at least 4 (winnable with them), with clearly more hive health and a higher level. In the wins of the two mixes, healing takes 15 to 45% of the wasps spent and the towers at least 8%; every won match lasts 6 to 9 minutes; and across all the wins no one song, the giant included, deals more than half the damage, and the giant less than 45%. It knows every song, so it measures the economy, not discovery. Its explicit `WideSweep_ThirtySeeds_HoldsTheRates` plays thirty seeds and holds the same shares.

How the plans use the new songs. Every attacking plan plays, of the Strike songs whose shape is in front of it and that it can pay for, the one that would deal the most damage: the Meteor at a crowd, the Scythe at two or more in the half-disc in front (with the Scythe ready it steps in to the blade's reach), the Tornado when surrounded, the Lance at a file of two or more or at a smoker beyond the Sting's reach, the Hydra at two or more within its reach. Both mixes keep the one Post slot busy: before each wave a Nest on the lane the wave's first beekeeper walks, 6 m out from the hive it walks to; in the wave, once that nest has gone off, the Honeycomb on the hive they leave (the threatened hive they are not guarding, once auto-aim would shell it), or a Nest lobbed onto a pack. Towers only does the same with towers alone and heals. The bot plays from the keyboard, so it places a tower as auto-aim does: it faces the spot and plays the song 6 m from it.

How the plans play the rules of section O. Sticks go for the hives and turn on the core only when a song hurt them, so both mixes guard where the sticks hit: they stand beside the weaker of the hives with a beekeeper within 8 m and fight from there, and while no hive is threatened they fly at the aimed beekeeper on its way in. They raise the Dome there while that hive is being hit and its middle is inside the Dome, and on the core when a stick they provoked swings at it; without the Dome they run from such a stick, which follows them off the hive. Two or more beekeepers within 2.5 m of the hive they guard are a pack: they throw it off with the Tornado, or with the Gust while the Tornado cannot be played. They count on several stings: two or more beekeepers in the Sting's reach are a shape for it, each with its own sting, compared with the other Strike songs by the damage the stings would deal. Attack only and Sting only play as before. Heal only still stands beside a hive, but the sticks walk past it, so it can no longer stall a wave.

How the plans fight as the giant (section I). The plans that cast it (attack only and both mixes) press its keys as a player does: one key at a time at the player's note pace, each once the move before it has landed, except the second key of a phrase. They walk it to the pack at the threatened hive (of the beekeepers within 8 m of the hive with the most, the one nearest the giant), or to the nearest beekeeper while no hive is threatened. They roar when two or more beekeepers in the roar's reach are within 4 m of a hive, slam when two or more are in the slam's reach, and leap (L L) onto a pack beyond the slam where the leap would land, auto-aim's pick, since the bot plays from the keyboard; with two or more close while the slam cools down they spin (K K), and otherwise punch the beekeeper in front, three in a row, then kick. `PlaytestBotTests` checks that a match without nests uses every move and both phrases.

Changed from the starting numbers: waves 3 + 0, 4 + 1, 4 + 3, 6 + 4, 8 + 5 became the table in section L; one beekeeper every 1.2 s became 3 s; the stick's 60 health 90; a hive's 300 health 450; healing 2 wasps a point 2.5, and 3 once the Sting hit from its arrival; farm phases 35 s 40 s; Fury 1 point per 2 damage 1 per 4 (section I asked for 3; at 3 attacking only won 9 matches in 30, and the giant stood so often that the songs, and their price, mattered less); with the towers, wave 4 12 + 8 became 16 + 10, and once the bot shelled the hive auto-aim really shells, waves 3 to 5 became 5 + 3, 16 + 11 and 18 + 12; after section O, waves 4 and 5 became 18 + 13 and 20 + 13, one beekeeper every 3 s 2.5 s, the Sting's 10 wasps a sting 5 and its 25 damage a second 20, the Nest's 100 wasps 200, and the giant's 12 s 11 s; with the giant of section I (20 s, 2 s a kill, 30 s at most, by the owner's decision), its own blows stopped filling Fury, Fury 1 point per 4 damage became 1 per 7, the Nest's 200 wasps 300, and waves 4 and 5 18 + 13 and 20 + 13 became 22 + 14 and 23 + 14. The other song prices, the other numbers of the five new songs, store, production, collecting, the early call, nests and levels kept their starting numbers.

| Plan | Wins in 5 | Typical round reached | Healing share in wins | Towers share in wins |
|---|---|---|---|---|
| Idle | 0 | Lost in round 1 | none | none |
| Sting only | 0 | Lost in round 2 | none | none |
| Attack only (never heals, shields, places towers or fetches nests) | 0 | Lost in round 4, now and then 3 or 5 | none, by design | none, by design |
| Heal only (never attacks) | 0 | Lost in round 1: the sticks walk past it to the hives | none | none |
| Towers only (towers and healing, no Strike song) | 0 | Lost in round 1 | none: no wins | none: no wins |
| Mixed (no nests) | 0 | Lost in round 5 | none: no wins | none: no wins |
| Mixed with nests | 5 | Won at level 9; 59% hive health left | 18 to 19% | 9 to 16% |

Measured on 2026-11-13 after the giant of section I, in the Editor and outside it alike, on five seeds and on thirty. Won matches last 7.3 to 7.5 minutes (7.1 to 7.8 over thirty seeds). Over thirty seeds Mixed wins 3 (seeds 16, 19 and 23) and Mixed with nests all 30, keeping 47 to 81% of the hives' health; the other plans win none. The damage in the wins (five seeds, all with nests): the giant 43.8%, the Sting 34.6%, the Meteor 17.2%, the Lance 2.4%, the Nest 1.6%, every other song under 1%. Over thirty seeds, across every win, the giant 42.8%, the Sting 34.3%, the Meteor 17.5%, the Lance 2.7%, the Nest 1.4%; in Mixed's three wins alone the giant deals 49%. The giant stands about 6 times a match with nests and 4 without. In thirty seeds three wins with nests spend 7% on the towers (seeds 7, 9 and 15) and one win of Mixed heals with 13% (seed 23); the 15% and 8% targets are checked on the five seeds, which all hold them. The thin margins are the giant's 43.8% against 45% and the towers' 8.7% (seed 1 with nests) against 8%.

Tuning log, one group at a time. With the new plans and the starting numbers Mixed won 4 in 5 and 21 in 30; every other target held. Taking one song out of Mixed's hands: without the Honeycomb it won 8 in 30, without the Nest 22, without the three new attacks 20, so the shell made the difference. The five songs first: the Honeycomb at 250 wasps won Mixed 19 in 30, at 350 22; letting through 50% of the damage instead of 25% 19, 75% 19; 250 wasps and 40% 18. Only a shell Mixed could no longer afford (450 wasps) or one that protects nothing brought it to 8 to 11 in 30, which throws the song away, so the five songs kept their numbers. Then the wave table: wave 4 at 16 + 10 holds every target on five and thirty seeds. Also tried: waves 4 and 5 at 14 + 9 and 20 + 13 (12 in 30, but a win with nests spent 7% on towers), wave 4 at 15 + 10 (14 in 30), 16 + 9 (18), 17 + 10 (16, the giant 50.4%), 18 + 11 (10, the giant 51 to 52%), and wave 3 at 7 + 4 or 8 + 4 (19 to 24).

Measured again on 2026-11-12. Until then the bot read a hive that had never been hit as the one hit last, so until both hives had been hit it expected the Honeycomb on the hive auto-aim would not shell. With that fixed (`PlaytestBotTests`) the five-seed targets still held, but Mixed won 13 in 30, and the Towers only row did not change. The shell now lands where it protects, which helps Mixed most. About 400 wave tables for waves 3 to 5 were tried outside Unity, then the best ones again with the beekeepers' speed nudged by 0.0001 to 0.0003 m/s, because the Editor's runtime rounds floats a little differently and a long match can end the other way. At 16 + 10 and 17 + 13 Mixed won 12 in 30 outside Unity but 13 in the Editor, and 10 to 17 with the nudges, so a table at the limit is luck. At 5 + 3, 16 + 11 and 18 + 12 Mixed wins 4 to 8 in 30 with every nudge, and the five seeds hold every target; with two of the six nudges Mixed also wins seed 3 while healing with 13%, below the 15% target, so that target is the thin one. Also tried: wave 5 at 17 + 12 or 18 + 11 (13 and 15 in 30), 18 + 12 or 18 + 14 (11 and 10, but a five-seed win with nests spends 6% on towers), 21 + 13 (9, the giant 49.9%); wave 4 at 15 + 10 or 17 + 10 (14 and 17); waves 3 to 5 at 5 + 3, 16 + 11 and 18 + 11 (8 in 30, 12 with one nudge). The Honeycomb experiments above were not repeated with the fixed bot.

Measured again after section O (2026-11-12). With the plans changed for the new rules and the numbers as they were, Mixed won 3 in 5 and 23 in 30, and the five wins with nests healed with 0 to 16% of their wasps and spent 2 to 5% on the towers: a Sting cast at 4,000 wasps cost 100, so the songs took nearly every wasp, and the lane nest seldom went off because the swarm killed the beekeepers far out on the lanes. First the wave table and the beekeepers: 48 tables for waves 3 to 5, then the stick's health, speed, hive damage, knock-off and cooldown, the smoker's health, the provocation's length and the spawn interval. The best brought Mixed to 9 to 14 in 30, but none lifted the towers share of a win with nests above 7% or its healing to 15%. Then the songs: the Sting at 5 wasps a sting and 20 damage a second, and the Nest at 150 or 200, held every five-seed target (healing 16 to 26%, towers 9 to 14%). The giant was still the main source of damage, at 42%. Shortening it to 11 s, or Fury at 1 point per 4.5 or 5 damage, let Mixed win 11 to 21 in 30, because a shorter giant leaves more of each wave to the Dome at the hives; so the giant was shortened and waves 4 and 5 grew by two sticks and two smokers, and two sticks and a smoker. Each candidate was then played with eleven nudges of the beekeepers' speed (0.0001 to 0.0004 m/s), as the Editor rounds floats a little differently. The best set that kept the giant at 12 s failed one nudge (Mixed won a match while healing with 10%); this set held all eleven: Mixed 0 in 5 and 2 to 7 in 30, healing 16 to 19%, towers 10 to 12%. In the Editor Mixed won 5 in 30.

Measured again after the Sting's damage came in pulses from its arrival (2026-11-12). The Sting deals the same damage per song but up to 0.25 s sooner, so kills came sooner and the hives took fewer hits: with nests, seeds 1 and 4 won while healing with 14.7% and 14.1% of their wasps, below the 15% target. Lowering the Sting's 20 damage a second instead held only at 18 together with healing at 2.6 to 2.75 wasps a point (at 18.5 to 19.5 a win with nests spent 6% on the towers); healing at 2.7 to 3 wasps a point alone held every target with all eleven speed nudges. At 3: Mixed wins 1 in 5 and 6 in 30, with nests 5 in 5 and 30 in 30; the five-seed wins heal with 16 to 22% and spend 10 to 17% on the towers; won matches last 6.4 to 7.3 minutes; the giant deals 38% of the damage in the wins (40% over thirty seeds). Towers only now loses in round 1 instead of stalling until the 20-minute limit.

Measured again after the giant of section I (2026-11-13). The giant stands 20 s, plus 2 s a kill, 30 s at most, by the owner's decision, so that stayed. With the bot still only punching, Mixed won 3 in 5 and the giant dealt 82% of the damage in the wins; teaching the bot the giant's moves (above) changed little (Mixed 3 in 5, the giant 80%), because the giant filled Fury by itself: a giant dealt about 600 damage, 150 points at 1 per 4, so Fury was full again as it ended, and it stood 30 s 7 to 9 times a match, through nearly every wave. Of the three ways to lower its share (its blows' damage, slower Fury, or its blows filling no Fury), only the last breaks that loop. Slower Fury alone keeps the loop, with a cliff (later, with its blows filling Fury, 1 per 12 left the giant at 47% and 1 per 14 at 31%); lower damage barely moves the share even without the loop (at half damage 55% against 56%), since the swarm at level 9 kills with any blow. So the giant's own blows stopped filling Fury (`FuryConfig.ColossusDamageShare`, 0), and the blows keep their numbers. One group at a time after that. Fury: the giant at 1 point per 4 dealt 56% (Mixed 4 in 5), per 6 45%, per 7 44%, per 8 41% and per 10 38%. Fewer giants leave more of each wave to the Sting, which kills the beekeepers far out before they set off the lane nest, so at every rate from 6 up some wins with nests spent only 0 to 7% on the towers. A shorter-lived nest (30 or 40 s) and farm phases of 45 or 50 s did not change that; the Nest at 300 wasps did (250 was not enough): at 1 per 7 the five-seed wins spent 9.5 to 21%. Mixed then won 3 in 5 and healed with 13%, so waves 4 and 5 grew: 19 + 13 and 21 + 14 won Mixed 1 in 5 but healed with 14.9%; 20 + 13 and 22 + 14 won 1 to 2 in 5 with the nudges; 21 + 14 and 22 + 14 1 to 2 in 5, healing with 14.9% and the giant at 45.0% with one nudge; 22 + 14 for both 0 in 5; 22 + 14 and 23 + 14 0 in 5 at the beekeepers' speed and with three nudges of it (0.0001 to 0.0004 m/s), Mixed 2 or 3 in 30, the giant 42.5 to 43.8%. At 1 point per 6.5 or 7.25 a win with nests spent 0 or 7% on the towers, so 7 is a narrow window.

Open for the owner:
- The giant now stands 20 to 30 s about 6 times a match with nests and 4 without, and is the biggest single source in the wins (44%), just under the 45% target. Fury fills only from the songs, at 1 point per 7 damage, more than twice section I's 3. A faster Fury would bring the giant more often, but at 6 or 6.5 some wins with nests spend under 8% on the towers.
- Towers only stalls every match until the 20-minute limit: the Honeycomb and healing hold a hive through a wave that nothing ends. The bot counts it as not won; a person may find it a stall worth forbidding.
- The towers share of a win with nests is now the thinnest target: 8.7% on the five seeds, and three wins in thirty at 7%. Healing holds at 18 to 19% on the five seeds, with one win in thirty below 15%.
- The Nest at 300 wasps costs twice the Honeycomb's 150, so a lane trap is the dearest tower by far.
- The Tornado and the Gust are played about 7 and 4 times a match without nests (throwing packs off a hive, and the Gust also clearing smoke), and less than once and twice with them, because the swarm with nests kills most beekeepers before they reach a hive. The Dome is raised about 6 times a match without nests and once with them.
- The Scythe is still played less than once a match: beekeepers come one every 2.5 s from four lanes, so two seldom stand within its 4.5 m in front of the swarm.
- Mixed with nests still wins all thirty matches.

### N. Guidance

- **The song table fills in.** Each song's slot shows the notes of its beginning that the player has played in order at least once, and dots for the rest, for example `J K ·`. It keeps them for the whole match. A song still becomes known only when it is cast.
- **The first farm phase teaches one thing at a time.** Each prompt goes away when the player has done it:
  1. Move with W A S D.
  2. Play J K H.
  3. Fly to the glowing nest and stay until it is yours.
  4. Fly to a hive to take its wasps. Hold E to give some back and heal it.
  5. Enemies come when the timer ends. Press Enter when ready.
- The second hive starts the match at 80% health, so step 4 has something to heal.
- Costs show on the song chips, and each hive panel shows its health and its store.

### O. What changed after the owner played

Agreed with the owner on 2026-11-12, after he played the build. Where this section differs from anything above, it wins. His words are quoted.

| He said | The rule now |
|---|---|
| "I have 0 wasps but they still chase me" | A collapsed swarm cannot be seen, chased, hit or smoked. Every beekeeper ignores the core until the respawn: a hunt or a provocation ends, a swing or a throw at the core is dropped, and no cloud slows or drains it. The HUD says the swarm is returning and counts the seconds to the respawn. |
| "if I camp the hive they try to hit me instead of the hive" | Beekeepers go for the hives. A stick never turns on the core because it is near. It chases the core only when a song of the swarm hurts it, for 4 s wherever the core is, and each hurt starts the 4 s again; then it goes back to its hive. Smokers hunt the swarm: they keep the 7 m notice and 14 m give-up ranges and throw at the core while they hunt it, otherwise at a hive. |
| "every 400, 1 sting attack feels good" | One Sting cast sends one sting for every 400 living wasps at the cast, at least 1 and at most 10. Each takes its own target among the most threatening beekeepers in reach (the Threat rule). Spare stings double up, most threatening first, and their damage adds. The price is the Sting's for each sting sent: 5 wasps (10 until the playtest bot's round in section M). The Strike squad is shared evenly between the stings. A sting whose target dies takes the most threatening beekeeper no other sting wraps, or doubles up, and hurts again 0.2 s later; with nothing left it ends. |
| "the defenses are weird, I never know when to use it" | The Dome shields everything inside it: a hive whose centre is within its radius takes the Dome's 15% of the damage from sticks and smoke. The Gust shoves every beekeeper within its 6 m at 9 m/s (was 3 m at 4 m/s), stuns it for 0.5 s, and still clears smoke. While a hive is under attack and the Dome is known, the HUD says "Hive under attack: the Dome shields what is inside it". |
| "the tornado feels like nothing happens" | The Tornado keeps its 30 damage and throws every beekeeper in reach at 12 m/s (was 6), and each cannot act for 1 s. |
| "maybe add a haptic when we hit an enemy" | A keyboard has no haptics, so each hit is felt on screen and in sound. The session reports every hit a song or the giant lands on a beekeeper (place, damage, kill) and every blow the Dome or the Honeycomb blocks, on the core or on a hive (the harm it stopped). Each sting reports its damage on every 0.25 s squeeze. The big hits are the Meteor, the Lance, the Hydra, the Nest, the Tornado's throw and the giant's blows. Smoke's slow harm is not a blow, so it is not reported. |
| "there's still a bug about the pivot of the wasps, it is a weird ring of wasps" and "let's try no marker, and when idle make the wasps do some kind of weird 8 3D shape" | Nothing marks the core: the inner ring is gone. While the core stands, the whole free swarm flows round a figure-eight in the air: a closed loop that crosses itself 1.4 m over the core, its two lobes 2.6 m long, tilted 0.5 rad so one rises as the other dips, a ribbon 0.45 m to each side of its line, once round in 5 s. The wasps linger over the crossing and hurry round the tips, so the crossing on the core is the densest part. When the core moves the eight unwinds into the river, head first; when it stops the river winds back into it. Its sizes grow with the swarm's level, as the songs' do. |
| "most of the combos lagged" | The session says a song was cast on the tick its last note lands, before any wasp moves. Impacts come sooner: the Sting arrives at 0.35 s (was 0.6), the Meteor lands at 1.0 s (was 1.3: a 0.6 s climb and a 0.4 s dive), the Hydra bites at 0.5 s (was 0.7) and lasts 1.4 s, and the Lance fires at 0.35 s (was 0.5: a 0.25 s draw) and lasts 1.15 s. Each keeps its time after the impact. |

- **Response and first-use stutters.** On the frame a song is cast its squad flashes, a cast sound plays and the camera kicks, before any wasp moves. A hit flashes the beekeeper white and recoils it, raises a number, shakes the camera by its damage and plays a thump (a brighter pop on a kill); a big hit holds the swarm still for 50 ms; a blocked blow flashes blue on what the shield guards, with a duller knock. A thrown beekeeper flies up in an arc, and the Tornado's hit flashes a red ring on the ground at its reach. Every light exists from the first frame and stays enabled, dark while unused; nest lights are not important. While this scene is loaded its root sets the moon's shadows to 45 m and 2 cascades, and puts the quality level's values back when it unloads.
- **The Sting reads the live count, by the owner's decision.** Section H says strength comes from the level, never from the live wasp count. The number of stings is the one exception. The count is taken as the notes complete, before the price is paid, so a hurt swarm sends fewer stings. Each sting's damage and reach still follow the level.
- The playtest bot's targets were set before these changes. With the plans unchanged, Mixed without nests won 5 in 5 (the target allows 2), and the healing and tower shares of the wins missed their targets. The plans now play these rules and the numbers were tuned again (section M): waves 4 and 5 grew, beekeepers come every 2.5 s, the Sting costs 5 wasps a sting and deals 20 a second, the Nest costs 200 and the giant stands 11 s.
