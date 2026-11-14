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
| Collecting | Stand within 2.5 m of an open nest and play its combo of 4 or 5 notes (section U; until 2026-11-14: stay 2 s) |
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
- The HUD shows the level, a marker at the screen edge for each open nest, and a ring that fills as the combo is played. The end screen counts nests.
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
| Sting | 5 for each sting | One sting for every 400 living wasps (section O), so a cast at the starting 800 costs 10. Cheap on purpose, so the player always has a move. Since section V the stings share one cast's damage, the weakest of the attacks |
| Gust | 20 | |
| Dome | 40 | |
| Tornado | 60 | |
| Meteor | 90 | |
| Hydra | 60 | |
| Scythe | 30 | |
| Lance | 70 | |
| Colossus | 0 | Fury is its price: 1 point per 11.5 damage of the songs (8.5 until section V), 100 to fill. The giant's own blows, its fists included, fill none |
| Hive Call | 0 | |
| Healing a hive | 4 wasps for each point of health (3.5 until section V) | Hold E within 3 m of a damaged hive: 60 wasps a second flow in. A hive has 450 health |
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

`EconomyPlaytestTests` plays whole matches through the rules at 1/30 s with seven plans on five seeds, prints a table to the log (lines starting `[F010] bot:`, with one line per plan of the damage each song dealt) and checks the owner's targets here and in section H: doing nothing loses, by round 3; Sting alone, attacking only, healing only and towers with healing only win none (at most 2 until the giant had hands); the mix that never fetches a nest wins at most 2 (lost without nests); the same mix fetching the nests wins all 5 (winnable with them; at least 4 until the giant had hands), with clearly more hive health and a higher level. In the wins of the two mixes, healing takes 15 to 45% of the wasps spent and the towers at least 8%; every won match lasts 6 to 9 minutes; and across all the wins no one song, the giant included, deals more than half the damage, and the giant less than 45%. It knows every song, so it measures the economy, not discovery. Its explicit `WideSweep_ThirtySeeds_HoldsTheRates` plays thirty seeds, each on its own map (section S), and holds the same shares, with the nest player winning at least 27 of 30. The damage line also says how much of all the damage the giant's fists dealt.

How the plans use the new songs. Every attacking plan plays, of the Strike songs whose shape is in front of it and that it can pay for, the one that would deal the most damage: the Meteor at a crowd, the Scythe at two or more in the half-disc in front (with the Scythe ready it steps in to the blade's reach), the Tornado when surrounded, the Lance at a file of two or more or at a smoker beyond the Sting's reach, the Hydra at two or more within its reach. Both mixes keep the one Post slot busy: before each wave a Nest on the lane the wave's first beekeeper walks, 6 m out from the hive it walks to, laid as they call the wave or, if they never got ready to call it, 12 s before the farm phase ends; in the wave, once that nest has gone off, the Honeycomb on the hive they leave (the threatened hive they are not guarding, once auto-aim would shell it), or a Nest lobbed onto a pack. Towers only does the same with towers alone and heals. The bot plays from the keyboard, so it places a tower as auto-aim does: it faces the spot and plays the song 6 m from it.

How the plans play the rules of section O. Sticks go for the hives and turn on the core only when a song hurt them, so both mixes guard where the sticks hit: they stand beside the weaker of the hives with a beekeeper within 8 m and fight from there, and while no hive is threatened they fly at the aimed beekeeper on its way in. They raise the Dome there while that hive is being hit and its middle is inside the Dome, and on the core when a stick they provoked swings at it; without the Dome they run from such a stick, which follows them off the hive. Two or more beekeepers within 2.5 m of the hive they guard are a pack: they throw it off with the Tornado, or with the Gust while the Tornado cannot be played. They count on several stings: two or more beekeepers in the Sting's reach are a shape for it, each with its own sting, compared with the other Strike songs by the damage the stings would deal. Attack only and Sting only play as before. Heal only still stands beside a hive, but the sticks walk past it, so it can no longer stall a wave.

How the plans fight as the giant (section I). The plans that cast it (attack only and both mixes) press its keys as a player does: one key at a time at the player's note pace, each once the move before it has landed, except the second key of a phrase. They walk it to the pack at the threatened hive (of the beekeepers within 8 m of the hive with the most, the one nearest the giant), or to the nearest beekeeper while no hive is threatened. They roar when two or more beekeepers in the roar's reach are within 4 m of a hive, slam when two or more are in the slam's reach, and leap (L L) onto a pack beyond the slam where the leap would land, auto-aim's pick, since the bot plays from the keyboard; with two or more close while the slam cools down they spin (K K), and otherwise punch the beekeeper in front, three in a row, then kick. Between the keys they launch a hand (section R) at auto-aim's pick within the fist's range whenever it stands beyond the leap's range, hive under attack or not, the right hand first and the left while the right regrows. `PlaytestBotTests` checks that a match without nests uses every move, both phrases and both hands.

Changed from the starting numbers: waves 3 + 0, 4 + 1, 4 + 3, 6 + 4, 8 + 5 became the table in section L; one beekeeper every 1.2 s became 3 s; the stick's 60 health 90; a hive's 300 health 450; healing 2 wasps a point 2.5, and 3 once the Sting hit from its arrival; farm phases 35 s 40 s; Fury 1 point per 2 damage 1 per 4 (section I asked for 3; at 3 attacking only won 9 matches in 30, and the giant stood so often that the songs, and their price, mattered less); with the towers, wave 4 12 + 8 became 16 + 10, and once the bot shelled the hive auto-aim really shells, waves 3 to 5 became 5 + 3, 16 + 11 and 18 + 12; after section O, waves 4 and 5 became 18 + 13 and 20 + 13, one beekeeper every 3 s 2.5 s, the Sting's 10 wasps a sting 5 and its 25 damage a second 20, the Nest's 100 wasps 200, and the giant's 12 s 11 s; with the giant of section I (20 s, 2 s a kill, 30 s at most, by the owner's decision), its own blows stopped filling Fury, Fury 1 point per 4 damage became 1 per 7, the Nest's 200 wasps 300, and waves 4 and 5 18 + 13 and 20 + 13 became 22 + 14 and 23 + 14; with the giant's hands (section R) and a map for each seed (section S), Fury 1 point per 7 damage became 1 per 8.5, healing 3 wasps a point 3.5, and waves 4 and 5 22 + 14 and 23 + 14 became 24 + 14 and 25 + 14. The other song prices, the other numbers of the five new songs, store, production, collecting, the early call, nests and levels kept their starting numbers.

| Plan | Wins in 5 | Typical round reached | Healing share in wins | Towers share in wins |
|---|---|---|---|---|
| Idle | 0 | Lost in round 1 | none | none |
| Sting only | 0 | Lost in round 2, now and then 3 | none | none |
| Attack only (never heals, shields, places towers or fetches nests) | 0 | Lost in round 4, now and then 5 | none, by design | none, by design |
| Heal only (never attacks) | 0 | Lost in round 1: the sticks walk past it to the hives | none | none |
| Towers only (towers and healing, no Strike song) | 0 | Lost in round 1 | none: no wins | none: no wins |
| Mixed (no nests) | 0 | Lost in round 4 or 5 | none: no wins | none: no wins |
| Mixed with nests | 5 | Won at level 9; 56% hive health left | 16 to 25% | 13 to 19% |

Measured again after section V (2026-11-14), with the wider map and the even split (T), the nest combo (U) and the Sting as the weakest attack (V), on five seeds and on thirty. Won matches last 7.2 to 7.9 minutes (7.0 to 7.9 over thirty). Over thirty seeds Mixed wins none and Mixed with nests all 30; the other plans win none. The damage in the wins (five seeds): the giant 40% (its fists 14%), the Meteor 18%, the Sting 15%, the Hydra 11%, the Lance, the Nest and the Tornado 4% each, the Honeycomb 3%. Over thirty: the giant 40% (fists 15%), the Meteor 17%, the Sting 16%, the Hydra 11%, the Nest 5%, the Lance and the Tornado 4%. Over thirty, healing is the thin share: 15 of the 30 wins heal with less than 15% (seed 20 with nothing), and seed 28 spends 8% on the towers; the 15% and 8% targets are checked on the five seeds, which all hold them. Section V has the levers and what was tried. The table above is this measurement; the paragraphs below are the earlier ones.

Measured on 2026-11-13, overnight, with the giant's hands and a map for each seed, on five seeds and on thirty. Won matches last 6.8 to 7.0 minutes (6.8 to 7.4 over thirty seeds). Over thirty seeds Mixed wins 1 (seed 13) and Mixed with nests all 30, keeping 50 to 85% of the hives' health; the other plans win none. The damage in the wins (five seeds, all with nests): the giant 40% (its fists 19.5% of all the damage, about 46 hits a match), the Sting 36%, the Meteor 17%, the Lance 4%, the Nest 3%, every other song under 1%. Over thirty seeds, across every win: the giant 40% (fists 21%), the Sting 37%, the Meteor 18%, the Lance 3%, the Nest 2%. Over thirty seeds four wins with nests heal with nothing (seeds 6, 9, 21 and 29: wave 4 left both hives above 60%, the line where the plans heal, and the last wave's harm is never healed), and one spends 7.8% on the towers (seed 28); the 15% and 8% targets are checked on the five seeds, which all hold them.

Tuning log, one group at a time. With the new plans and the starting numbers Mixed won 4 in 5 and 21 in 30; every other target held. Taking one song out of Mixed's hands: without the Honeycomb it won 8 in 30, without the Nest 22, without the three new attacks 20, so the shell made the difference. The five songs first: the Honeycomb at 250 wasps won Mixed 19 in 30, at 350 22; letting through 50% of the damage instead of 25% 19, 75% 19; 250 wasps and 40% 18. Only a shell Mixed could no longer afford (450 wasps) or one that protects nothing brought it to 8 to 11 in 30, which throws the song away, so the five songs kept their numbers. Then the wave table: wave 4 at 16 + 10 holds every target on five and thirty seeds. Also tried: waves 4 and 5 at 14 + 9 and 20 + 13 (12 in 30, but a win with nests spent 7% on towers), wave 4 at 15 + 10 (14 in 30), 16 + 9 (18), 17 + 10 (16, the giant 50.4%), 18 + 11 (10, the giant 51 to 52%), and wave 3 at 7 + 4 or 8 + 4 (19 to 24).

Measured again on 2026-11-12. Until then the bot read a hive that had never been hit as the one hit last, so until both hives had been hit it expected the Honeycomb on the hive auto-aim would not shell. With that fixed (`PlaytestBotTests`) the five-seed targets still held, but Mixed won 13 in 30, and the Towers only row did not change. The shell now lands where it protects, which helps Mixed most. About 400 wave tables for waves 3 to 5 were tried outside Unity, then the best ones again with the beekeepers' speed nudged by 0.0001 to 0.0003 m/s, because the Editor's runtime rounds floats a little differently and a long match can end the other way. At 16 + 10 and 17 + 13 Mixed won 12 in 30 outside Unity but 13 in the Editor, and 10 to 17 with the nudges, so a table at the limit is luck. At 5 + 3, 16 + 11 and 18 + 12 Mixed wins 4 to 8 in 30 with every nudge, and the five seeds hold every target; with two of the six nudges Mixed also wins seed 3 while healing with 13%, below the 15% target, so that target is the thin one. Also tried: wave 5 at 17 + 12 or 18 + 11 (13 and 15 in 30), 18 + 12 or 18 + 14 (11 and 10, but a five-seed win with nests spends 6% on towers), 21 + 13 (9, the giant 49.9%); wave 4 at 15 + 10 or 17 + 10 (14 and 17); waves 3 to 5 at 5 + 3, 16 + 11 and 18 + 11 (8 in 30, 12 with one nudge). The Honeycomb experiments above were not repeated with the fixed bot.

Measured again after section O (2026-11-12). With the plans changed for the new rules and the numbers as they were, Mixed won 3 in 5 and 23 in 30, and the five wins with nests healed with 0 to 16% of their wasps and spent 2 to 5% on the towers: a Sting cast at 4,000 wasps cost 100, so the songs took nearly every wasp, and the lane nest seldom went off because the swarm killed the beekeepers far out on the lanes. First the wave table and the beekeepers: 48 tables for waves 3 to 5, then the stick's health, speed, hive damage, knock-off and cooldown, the smoker's health, the provocation's length and the spawn interval. The best brought Mixed to 9 to 14 in 30, but none lifted the towers share of a win with nests above 7% or its healing to 15%. Then the songs: the Sting at 5 wasps a sting and 20 damage a second, and the Nest at 150 or 200, held every five-seed target (healing 16 to 26%, towers 9 to 14%). The giant was still the main source of damage, at 42%. Shortening it to 11 s, or Fury at 1 point per 4.5 or 5 damage, let Mixed win 11 to 21 in 30, because a shorter giant leaves more of each wave to the Dome at the hives; so the giant was shortened and waves 4 and 5 grew by two sticks and two smokers, and two sticks and a smoker. Each candidate was then played with eleven nudges of the beekeepers' speed (0.0001 to 0.0004 m/s), as the Editor rounds floats a little differently. The best set that kept the giant at 12 s failed one nudge (Mixed won a match while healing with 10%); this set held all eleven: Mixed 0 in 5 and 2 to 7 in 30, healing 16 to 19%, towers 10 to 12%. In the Editor Mixed won 5 in 30.

Measured again after the Sting's damage came in pulses from its arrival (2026-11-12). The Sting deals the same damage per song but up to 0.25 s sooner, so kills came sooner and the hives took fewer hits: with nests, seeds 1 and 4 won while healing with 14.7% and 14.1% of their wasps, below the 15% target. Lowering the Sting's 20 damage a second instead held only at 18 together with healing at 2.6 to 2.75 wasps a point (at 18.5 to 19.5 a win with nests spent 6% on the towers); healing at 2.7 to 3 wasps a point alone held every target with all eleven speed nudges. At 3: Mixed wins 1 in 5 and 6 in 30, with nests 5 in 5 and 30 in 30; the five-seed wins heal with 16 to 22% and spend 10 to 17% on the towers; won matches last 6.4 to 7.3 minutes; the giant deals 38% of the damage in the wins (40% over thirty seeds). Towers only now loses in round 1 instead of stalling until the 20-minute limit.

Measured again after the giant of section I (2026-11-13). The giant stands 20 s, plus 2 s a kill, 30 s at most, by the owner's decision, so that stayed. With the bot still only punching, Mixed won 3 in 5 and the giant dealt 82% of the damage in the wins; teaching the bot the giant's moves (above) changed little (Mixed 3 in 5, the giant 80%), because the giant filled Fury by itself: a giant dealt about 600 damage, 150 points at 1 per 4, so Fury was full again as it ended, and it stood 30 s 7 to 9 times a match, through nearly every wave. Of the three ways to lower its share (its blows' damage, slower Fury, or its blows filling no Fury), only the last breaks that loop. Slower Fury alone keeps the loop, with a cliff (later, with its blows filling Fury, 1 per 12 left the giant at 47% and 1 per 14 at 31%); lower damage barely moves the share even without the loop (at half damage 55% against 56%), since the swarm at level 9 kills with any blow. So the giant's own blows stopped filling Fury (`FuryConfig.ColossusDamageShare`, 0), and the blows keep their numbers. One group at a time after that. Fury: the giant at 1 point per 4 dealt 56% (Mixed 4 in 5), per 6 45%, per 7 44%, per 8 41% and per 10 38%. Fewer giants leave more of each wave to the Sting, which kills the beekeepers far out before they set off the lane nest, so at every rate from 6 up some wins with nests spent only 0 to 7% on the towers. A shorter-lived nest (30 or 40 s) and farm phases of 45 or 50 s did not change that; the Nest at 300 wasps did (250 was not enough): at 1 per 7 the five-seed wins spent 9.5 to 21%. Mixed then won 3 in 5 and healed with 13%, so waves 4 and 5 grew: 19 + 13 and 21 + 14 won Mixed 1 in 5 but healed with 14.9%; 20 + 13 and 22 + 14 won 1 to 2 in 5 with the nudges; 21 + 14 and 22 + 14 1 to 2 in 5, healing with 14.9% and the giant at 45.0% with one nudge; 22 + 14 for both 0 in 5; 22 + 14 and 23 + 14 0 in 5 at the beekeepers' speed and with three nudges of it (0.0001 to 0.0004 m/s), Mixed 2 or 3 in 30, the giant 42.5 to 43.8%. At 1 point per 6.5 or 7.25 a win with nests spent 0 or 7% on the towers, so 7 is a narrow window.

Measured again after the giant's hands (section R) and a map for each seed (section S), 2026-11-13, overnight. The swarm's new motion (section Q) changes nothing the bot plays: the rules read none of its numbers, no song's data changed, and its strikes keep their springs and pins, so every song lands when it did. The bot launches the hands as a player does, at a beekeeper beyond the leap's range, hive under attack or not, with both hands. With the numbers as they were, the hands dealt about a fifth of all the damage and the giant stayed at 42 to 44%, but the healing and towers targets failed: a fist provokes the stick it hits (section O), so beekeepers on their way in left the hives for the giant, the hives came out of waves 1 to 4 above the 60% line where the plans heal, and wins healed with nothing; and a win could spend nothing on the towers. A farm-by-farm trace showed why for the towers: the mixes laid the lane's Nest only on the way to calling the wave, and with nests the swarm (4,000 at level 9) never filled, so the farm phase ran out and the wave came without one. That is the bot's fault, not the economy's, so the mixes now lay it 12 s before the farm phase ends if they never got ready to call (`MixedPlan`). Also tried: walking the giant to the nests and stores when it stands into a farm phase (it can collect and give there), which changed nothing; healing below 70% or 75% instead of 60%, which barely changed the healing share, so the 60% line stays; longer farm phases (45 or 50 s), which let Mixed win up to 9 in 30; and Fury at 7.5, 8 or 8.5, the fists at 10, 15 or 20 damage and 2 or 3 s to regrow, the Nest at 350 wasps and a 12 m fist range. With the lane nests laid the towers held everywhere (14 to 25% in most wins) and healing became the thin target: 0 to 14% in a third of the thirty wins. Healing at 3.5 wasps a point lifts the share where there is healing; waves 4 and 5 two sticks bigger make wave 4 hurt the hives a little more and keep Mixed losing; Fury at 1 per 8.5 keeps the giant, hands included, at 40%. Nearby sets: Fury 1 per 8 with waves 22 + 14 and 23 + 14 held the five seeds too but Mixed won 1 to 2 of them and 4 of 30, the towers dropped to 9%, and the giant dealt 41.6%; 24 + 14 and 25 + 14 at Fury 1 per 8 healed a five-seed win with 10%; 26 + 14 and 27 + 14, or 16 smokers, healed with 0 to 13%. The fists keep section R's numbers.

The maps. All thirty seeds drew a map without the fallback, each with three spawn points nearer the west hive (the west lean), hives 24.1 to 26.5 m apart. Mixed with nests wins all thirty; the hardest map is seed 29 (50% of the hives' health kept, the weaker hive at 38%), then seed 9 (51%, 31%); the easiest is seed 23 (85%, 79%), then seed 8 (84%). Mixed without nests wins only seed 13, with 23% of the hives' health left. No map is unfair either way, so `LayoutConfig` keeps its bands and its lean.

Open for the owner:
- The giant now stands 20 to 30 s about 5 times a match with nests, and is the biggest single source in the wins (40%, its fists half of that), under the 42% aimed for. Fury fills only from the songs, at 1 point per 8.5 damage, almost three times section I's 3.
- A fist provokes the stick it hits, as every hurt does since section O, so a player can pull beekeepers off a hive by throwing fists at them. It is strong, and it is why the hands moved the healing numbers. A person should judge whether it feels right.
- Healing is the thinnest target: 16% in the poorest five-seed win, and four wins in thirty (seeds 6, 9, 21, 29) heal with nothing, because wave 4 left both hives above the 60% line and nothing heals after the last wave. A player who follows the lesson (section N) heals the second hive in the first farm phase; the bot does not, and if it did, every win would heal with about 7% more.
- Towers only loses every match in round 1, and attacking only in round 3 to 5.
- The Nest at 300 wasps costs twice the Honeycomb's 150, so a lane trap is the dearest tower by far.
- The Tornado, the Gust, the Dome, the Hydra and the Scythe are each played less than once a match with nests, because the swarm with nests and the giant's hands kill most beekeepers before they reach a hive.
- Mixed with nests still wins all thirty matches.

### N. Guidance

- **The song table fills in.** Each song's slot shows the notes of its beginning that the player has played in order at least once, and dots for the rest, for example `J K ·`. It keeps them for the whole match. A song still becomes known only when it is cast.
- **The first farm phase teaches one thing at a time.** Each prompt goes away when the player has done it:
  1. Move with W A S D.
  2. Play J K H.
  3. Fly to the glowing nest and play the notes shown (section U).
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

### P. Controller

Added 2026-11-13. A controller works beside the keyboard and mouse: the left stick moves; the face buttons are the notes, West H, South J, East K and North L, so they are also the giant's moves; the right stick aims from the core (8 m at a full lean, live while it leans) or turns the chase view; RB gives (E), Start calls the wave or plays again (Enter, R), RT punches and LT kicks. While the controller is the device used last, the HUD draws each note as a diamond of four dots with its button filled and says RB and Start, and the controller rumbles for notes, casts, hits, the giant's slam and leap, and a lost hive or collapsed swarm.

### Q. Motion: the swarm moves as one living ribbon

Added 2026-11-13, overnight, after the owner said the movement "looks a bit weird" and the hand-over between idle and moving is "too sharp, I want a wave movement". A player build already held 120 fps everywhere, so this is about how the motion is computed and paced.

**What games and demos do.** Big swarms fly each particle to a target the game computes (a formation, a path, a shape) on a spring. They look alive through three things: every change is staggered by a per-particle delay, so it runs through the flock as a wave; the body follows the leader's recorded path with a delay, so a turn bends down it; and layered noise (curl noise is the swirl-only kind). Smooth means critically damped springs (damping 1: the fastest settle with no overshoot), stepped so the result does not depend on the frame rate, by an exact solution or by fixed sub-steps. Neighbour rules (boids) need a spatial grid rebuilt every frame; a target-driven swarm does not.

**Data structure.** Each wasp reads only its own state and a few shared values: one thread per wasp, O(n), no neighbour queries. At 800 to 6,000 wasps a spatial hash or grid would add dispatches and a sort for nothing, so the two structured buffers stay. The one new structure is the core's past, `CoreHistory` in Logic: 128 samples at 60 a second (2.1 s), 2 KB of constants.

**What changed.**
- Follow with a delay. Each free wasp follows the core 0.8 s x rank^1.5 late (rank: 0 at the river's head, 1 at its tail, 8% from its seed). From `_History` it reads the metres the core walked since then, which move its river place that far back along the path. It also reads the swarm's wound share then and the eight's swing then; the eight it winds into stands where the core stood then. So a stop lets the tail flow on and curl in last, a turn bends down the river, and the head (rank under 0.1) answers within about 25 ms.
- On a start, the river is drawn out of the coil: it uses only the path walked since the swarm was last wound (`_RiverReach`), so it never lays itself back along the way the core came.
- The river has 8 m of lane places (was 11); walking, the delay stretches it to about 11.6 m, as before. Its tail thins to 55% of its width. Places are measured from the core (`PathTrail.Lead`), so they glide instead of jumping 0.15 m whenever the trail stores a point.
- Whole-swarm forms (Hive Call, the Colossus, the river) hand over as an eased wave: each wasp starts at its river place x 0.6 s, as before, and eases over 0.3 s instead of jumping. A second change that comes before the first wave is through (Hive Call ending as the stores run dry) blends from the form before. Winding into the eight and out of it takes each wasp 1.0 s (was 0.75 s, and in practice half that).
- Springs: the river and the eight use stiffness 100 at damping 1 (was 40 at 0.6, which overshot by 10%). All springs step in equal sub-steps of at most 1/240 s, so 60 and 120 fps take the same steps. Only shapes wobble after forming, at 0.5 (was 0.3, the river too). Strike springs and pins are unchanged, so no song's timing moved.
- First cast: Mono's `RuntimeHelpers.PrepareMethod` compiles nothing (3,598 methods in 0.1 ms), so the warm-up did nothing. It now calls `GetFunctionPointer` for each method, which compiles it: 42 ms over the first 17 frames.

**Measured** with `HiveDefenseMotionTests`, 200 of 800 wasps read back every frame, at 60 fps (120 fps within 0.5), before then after:

| Hand-over | Peak mean acceleration (m/s²) | Answers, 10th to 90th percentile | Front |
|---|---|---|---|
| Start | 17.1 then 13.0 | | 0.10 then 0.23 s |
| Stop | 52.9 then 12.7 | 0.017 then 0.58 s apart | 0.13 then 0.13 s |
| Turn start | 47.9 then 10.4 | | 0.13 then 0.17 s |
| Turn stop | 52.3 then 12.9 | 0.000 then 0.60 s apart | 0.13 then 0.13 s |
| Hive Call | 78.7 then 47.7 | 0.53 then 0.57 s apart | 0.13 then 0.12 s |

- The mean velocity at 60 and at 120 fps differed by 1.19 m/s at most, and now by 0.26 m/s.
- The old start "front" was quicker because the whole eight was dragged along at once. Per wasp, the new river spring reaches 60% of the core's speed in 0.2 s; the old one took 0.24 s.
- Hive Call's remaining peak is the wave itself. Round the standing eight, river order is loop order, so the wasps that ease over at any moment come from one stretch of the loop and move together.
- First Sting in the Editor: 2.23 ms against 0.51 ms for the second one before, and 2.33 against 0.46 after. About 1.9 ms of first-use cost remains in script updates. The game's own non-generic methods are now compiled ahead, so this cost lies elsewhere: generic or engine code, or a first sound. It has not been measured in a player.

Not done: curl noise, and banking, which needs a field that the 48-byte wasp state does not have.

### R. The giant's hands, and five small fixes

Added 2026-11-13, overnight. The owner: "In giant mode I want a punch where the hand moves forward and then all the bees on that hand fly off to the target. R2 does it with the right hand and L2 with the left hand." Where this section differs from sections I and P, it wins.

**Hand launch.** Right hand: R2, the right mouse button or Space. Left hand: L2, the left mouse button or Left Shift. These buttons punched and kicked before; J and K still do, and outside the giant the buttons still do nothing but keep the mouse live while held.

| Value (ColossusConfig) | Set |
|---|---|
| Thrust, press to launch | 0.25 s |
| Damage, times the level's damage scale | 20 (a punch is 45) |
| Radius, times the level's size scale | 1.2 m |
| Throw and stun | none |
| Speed | 24 m/s, at least 0.2 s of flight |
| Range, times the level's size scale | 14 m |
| Regrow, from the hit until the arm is whole | 2 s |

- The arm thrusts with the heavy punch's clip (Cross Punch). The scene builder plays it on the character, finds that it punches with the right hand and reaches furthest 1.13 s in, and plays it 4.53 times as fast, mirrored for the left hand, so the reach comes as the fist leaves.
- The target is chosen at the press. The giant always stands in the chase view, where the mouse and the right stick turn the camera, so the camera's way on the ground is the aim: first the beekeeper within the fist's range nearest that way, within 25 degrees of it (`FistAimConeDegrees`); otherwise auto-aim's most threatening beekeeper within the range; otherwise along the camera's way to the range. The mouse's point never moves a fist, so a click aims as Space and R2 do (changed after review: a click used to aim at the invisible spot 12 to 16 m ahead where the camera looks, and mostly hit empty ground, while Space homed on a beekeeper). A giant standing still turns to the target. The fist homes on its beekeeper and hits everyone within its radius, a heavy hit that fills no Fury; each kill adds to the giant's time.
- While a fist flies and its arm grows back, that hand cannot launch; the other hand never waits for it. No launch in the air. When the giant ends, a fist in flight never hits, and its wasps ease back into the swarm from where they were.
- GPU: the wasps on the forearm, hand and finger bones of a side (`ColossusArmBones`, 46 and 43 of 800 wasps, 239 and 256 of 4,000) leave the arm in turn, the hand first, within 0.1 s, fly pinned to an arc that rises 20% of its length (at most 2 m) and speeds up into the hit, all arriving together, so the bolt tightens. They burst to 80% of the hit's radius, turn there, and stream home along an arc over the regrow, the forearm first and the fingers last, so the arm grows back from the elbow. Two hands, three float4s each (`_Fists`), and the bone ranges (`_FistBones`); no new buffer, no readback.
- Feel: a firm rumble (0.6 and 0.5 for 0.12 s) on the launch and on the hit, a small camera push (0.25) on the launch; the hit has the heavy hits' shake and 50 ms hold.
- HUD: a second hint line names the hands ("Right click or Space: right fist · Left click or Shift: left fist", and "RT: right fist · LT: left fist" on a controller), and the strip has a mark for each hand ("RMB fist", "LMB fist", or "RT fist", "LT fist").
- The playtest bot launches a hand when auto-aim's pick stands beyond the leap's range, hive under attack or not, the right hand first and the left while the right regrows. Its fists count as the giant's damage, and the bot's table also shows their share. The economy playtests play with the hands since the retune of section M (Fury 1 point per 8.5 damage, healing 3.5 wasps a point, waves 4 and 5 24 + 14 and 25 + 14); the fists kept these numbers.

**Small fixes.**
1. A ring marks where the leap (L L) will land, from its take-off until it lands, 25% wider than the landing's reach at first and closing to it.
2. While the giant stands, the time ring and its marks take the songbook's corner, bottom right, and the lines at the bottom (the "New song" toast among them) move to the bottom left. The hint lines moved up from 19% to 11% of the height, above the giant's head.
3. A restart carries the device used last to the next scene, so the HUD keeps naming the controller's buttons.
4. When the match ends the rumble takes no new pulse but lets the running one play out, so the last hive's long, low rumble is felt in full. Pause, focus loss and unloading still stop it at once.
5. The first Sting's slow frame was not taken further: section Q left about 1.9 ms of first-use cost, unmeasured in a player.

**After review** (closing stage of the night):
- A fist out when the match ends no longer hangs in the air, glowing, on the end screen. The rules stop at Won or Lost, so `ColossusMotor` stops feeding the fists then, and the swarm's own clock flies the fist on, bursts it and brings its wasps home.
- A wasp leaving a shape for the river no longer takes the shape's wobble damping (0.5) through the first half of its hand-over. Only the form being taken up wobbles.
- The fist's burst (0.15 s) and way home (0.55 s) are squeezed to fit a regrow shorter than 0.7 s, so every wasp is home when the rules make the arm ready again. A burst takes at most a quarter of the regrow. At the 2 s regrow nothing changes.
- The arm's thrust follows `FistThrustSeconds` without a rebuild. The scene builder gives the fist states a `FistPace` speed parameter, and `ColossusMotor` sets it to 1 / `FistThrustSeconds` whenever the giant stands.

### S. A new map each match

Added 2026-11-13, overnight. The owner: "I want the hives to be placed somewhat randomly, and the trees as well." Each match draws its own map from a seed (`LayoutGenerator`, plain C#), random within `LayoutConfig`'s rules and `NestConfig`'s (section H), so every map is fair. The bands come from the old fixed map, so distances stay in its range.

| Value (LayoutConfig) | Set | Old fixed map |
|---|---|---|
| Hives from the middle | 12 to 13.5 m, the first west and the second east | 12.5 m |
| A hive's turn off due west or east | 15 degrees at most | 0 |
| Hives apart | 24 m at least | 25 m |
| Spawn points | 4, 31 to 33 m out, turned with the hives' line by up to 8 degrees: ahead, behind, beyond the east hive, beyond the west hive | 32 m: north, south, east, west |
| Spawn point to any hive | 19 m at least | 19.5 m |
| Ahead and behind spawn points | 1 m nearer the west hive | a tie, which went to the west hive |
| Nests open at the start | 15 to 40 degrees either side of ahead (NestConfig's 10 to 14 m out) | 20 to 35 degrees |
| Later nests | one in each 60 degree slice, turned with the hives' line, 10 degrees of jitter (NestConfig's 16 to 30 m, and 12 m clear of hives) | the same, unturned |
| Nest trees | inside 24 m, 8 m apart, 8 m from spawn points, 8 m from the start | the same |
| Scenery | 25.5 to 34 m out, pivots 2 m apart and half their two footprints; its footprint 6 m clear of hives, 3 m of nest trees, 4 m of spawn points, 10 m of the start, and 2 m of every lane (spawn point to hive) | 4 m from spawn points only |
| Tries | 30 maps, 200 per hive pair, spawn point or nest, 40 per prop | none |

- **Fallback.** If 30 maps all fail, the config's fixed map stands: the old map, with the ahead and behind spawn points 2 m toward the west hive. A prop that finds no place is hidden for that match. Over 200 seeds no map needed the fallback, and at least 63 of 70 test props were placed on every map. The scene's three snapshot maps placed 65 to 67 of 70.
- **The west lean is on purpose.** Beekeepers walk to the nearest standing hive. On the old map the ahead and behind spawn points were a tie, so the west hive took three of every four beekeepers and the east hive, which starts hurt, took one. The economy was tuned on that split. Without it, the share of wasps the nest player spent healing swung from 0% to 30% between maps. The stage that retunes may drop the lean.
- **Why the hive bands are tight.** With hives 11 to 14 m out, 22 m apart and turned up to 20 degrees, Mixed (no nests), which lost every match in round 5 on the old map, won one in five with only 10% of its wasps spent on healing. The nests and the scenery carry most of the variety.
- **Seed.** A fresh seed each match, from the clock. `-f010Seed n` on the command line plays seed n first and n + 1, n + 2 on each restart, so a demo can be replayed. PlayMode tests fix every match to 1010 (`F010MapSeedFixture`). Each match logs "[F010] Map seed n" so a map you liked can be replayed. Nothing about the seed is on the HUD. The benchmark finds its spots from the session, so it runs on any map, but runs compare fairly only with the same `-f010Seed`.
- **Scene.** The composition root draws the map in `Awake`, once. It moves the props (`SceneryView`, turned at random about their pivots) and the spawn points, puts the nest trees in place, and gives the session the hives, nests and spawn points. Hive and nest views move themselves to the session's positions as before, along with their lights, rings, bars and HUD markers. The chase camera's blocker boxes are measured again where the props now stand. The scene builder draws the saved scene's map with seed 1010, so the Editor shows a real map before Play.
- **No static batching on the props and nest trees.** A statically batched mesh keeps drawing where it was built. Runtime batching needs readable meshes, and Raz's are not. Main-thread time in the full-swarm test stayed at 0.21 ms. The extra draw calls of about 70 unbatched props were not measured in a player.
- **Playtest bot.** `PlaytestMatch` draws the map from its seed, so every seed of the economy playtests plays its own map. At the code defaults, on five seeds: Mixed won 1 of 5 (21% healing, 15% towers), MixedWithNests won 5 of 5 (healing 18 to 22%, towers 8 to 17%), and the rest won none. After the retune with the giant's hands (section M), Mixed wins none of the five and 1 of 30, MixedWithNests all 35; the west lean stays.

### T. A wider map, fair by rule, and rings that follow their hives

Added 2026-11-14. The owner: "there's a UI bug of the hives' circles: now when the hives are random, their circle UI is still fixed at the old hives' positions", and "make it more random on a bigger area".

- **The rings bug.** `HiveView.Initialize` built its attack ring, collect ring and health bar as children placed at the hive's world position, and only then moved itself to the hive. Moving a parent carries its children, so every ring and bar landed off its hive by however far the hive had moved from where the saved scene had it. The tests never saw it: they played seed 1010, the seed the scene builder saves the scene's map with, so the move was zero. Now the view moves first. Nest views, the hive lamps (children at a local offset), the HUD markers and the off-screen arrows already read the rules' positions.
- **The bug cannot hide again.** The PlayMode tests now play seed 2026 (`F010MapSeedFixture`), whose hives and spawn points stand more than 20 m from the saved scene's (`MatchSeed.SavedSceneSeed`, still 1010). `HiveDefenseLayoutTests` checks that distance, and checks every ring, bar, light and part of each hive and nest tree on the match's map, on the first match and after a restart onto another seed.
- **A wider map.** The pair of hives lies along any line: the first at any bearing, 8 to 20 m out; the second across the middle from it, turned up to 60 degrees off straight across, 8 to 20 m out, 18 m apart at least. Over 200 seeds every eighth of the circle holds the first hive at least 12 times. The arena and the play area grow from 34 to 42 m (the ground reaches 80 m, and the camera follows the core, so nothing else had to change). Nest trees open after a wave stand 16 to 34 m out (was 30) inside 34 m (was 24), and the scenery fills 22 to 42 m (was 25.5 to 34). The nests open at the start stand ahead, square to the hives' line, on the side the opening camera looks (north), so they are in or near the first view whichever way the hives lie; the hives may start off screen, where their arrows point to them.
- **Fair by rule.** Each hive has two spawn points of its own (`LayoutGenerator.HiveOfSpawn`: 0 and 2 the first hive's, 1 and 3 the second's), out beyond it 20 to 70 degrees to either side, 18 to 22 m from it, at least 4 m farther from the other hive, 20 m from the start and 10 m from each other. Each spawn point serves its nearest hive, fixed when the match starts, and its beekeepers walk to that hive while it stands, whoever they chase on the way (`BeekeeperBrain.HomeHive`); once it falls they go for the nearest standing hive. The waves take the spawn points in turn, so every wave alternates between the hives on every map. The west lean of section S (`SpawnWestLeanMetres`) is gone: the old map sent three beekeepers of four to the west hive, now each hive gets half.
- **Kept.** Lanes clear of props, nothing inside a hive's rings, every try bounded with the fixed fallback map (its spawn points moved to the new rules). Over 200 seeds no map needs the fallback and every prop is placed.
- **Economy.** Not retuned. With the even split the economy playtests move; the ones that left their bands are listed in `Logs/f010/known-red.txt` for the Balance stage.

### U. A combo collects a nest

Added 2026-11-14. The owner: "for each hive we collect in the forest, instead of standing to collect the bees, we have to play a random combo of 4-5 letters that is shown on the screen when we stand at the tree."

- **The rule** (`NestComboRule`, `Nest`, `NestField`, plain C#). Each nest draws a combo when the match starts, from the match's random source, so a seed replays its combos: 4 or 5 notes (`NestConfig.ComboShortest`, `ComboLongest`), never the same note twice in a row, so it reads as a tune. No song's keys lie inside a combo and no song's keys hold it, so a combo never casts a song; over 200 seeds of 8 nests none did through the song book. 64 random tries, then the first fitting combo in counting order; a config where none fits throws.
- **Playing it.** Standing within 2.5 m of an open nest makes it the active nest (the nearest wins when rings overlap, the first on a tie) and shows its combo. Standing still collects nothing and there is no timer. A note that continues the combo fills a slot; any other note empties it, and counts as the first when it is the first. Leaving the ring empties it. A hit on the core costs nothing (it used to restart the 2 s). While the swarm is collapsed or the giant stands, no combo shows (the keys are the giant's moves). Songs still work in a ring: every note reaches both. The last note collects the nest with exactly the reward of section H.
- **Known side effect.** Combo notes are flute notes, so they fill the song table's beginnings like any notes (section N). A notes run that starts before the ring and ends inside the combo can still finish a song; only the combo's own notes are guaranteed song-free.
- **HUD** (`NestComboView`). A row of 4 or 5 large slots, 44 px, near the top centre under the phase lines, titled "Play the nest's notes". Each slot wears its note's colour, dim (38%) until played, with the key's letter, or the four-dot diamond once the pad was used last. A right note fills its slot with a pop (0.18 s, up to 35% bigger) and the flute's note rises 2 semitones per filled slot (`SwarmFluteVoice.LiftNote`); a wrong note shakes the row (0.3 s, 12 px, 22 swings a second) and empties it; the last note bursts the row (0.45 s, 60% bigger, fading) while the nest's own collected burst and the "Level n" line play. The timings run on real time, so they stay crisp in slow motion. All these numbers are on `NestConfig` under "Combo prompt". The old "Collecting" bar is gone.
- **Hooks for effects.** `NestField.ComboShown`, `ComboHidden` and `ComboNote(nest, step)` (`NestComboStep`: Advanced, Broken, Restarted, Completed) are the attach points; the HUD and the flute listen to them and nothing calls the effects directly, so a later stage can add camera or particle juice by listening too.
- **Lesson.** Step 3 now reads "Fly to the glowing nest and play the notes shown."
- **Playtest bot.** It reads a combo for 0.6 s once it shows, then plays a note every 0.3 s (`PlaytestBot.NestComboReadSeconds`, `NestComboNoteSeconds`), so a nest takes 1.8 to 2.1 s, about the old 2 s. The economy is not retuned; band tests that moved are listed in `Logs/f010/known-red.txt` for the Balance stage. The benchmark plays the combo the same way.

### V. The Sting is the weakest attack

Added 2026-11-14. The owner: "I want the Sting to be less effective than the other attacks." It had become the top song: 35% of the damage in the wins, because a cast at 4,000 wasps sent ten stings of 73 damage each.

- **The rule** (`StingRule`). The stings now share one cast's damage: `StingSong.DamagePerSecond` is the whole cast's, split evenly between its stings. The owner's "every 400 wasps, one sting" stays, so a bigger swarm wraps more beekeepers, but it no longer hits harder; like every other song, the Sting grows only with the level. The price stays 5 wasps a sting.
- **The numbers.** `DamagePerSecond` 12 for the whole cast (was 20 for each sting), so a cast deals 43.8 at the first level at any swarm (was 146 at 800 wasps and 730 at 4,000). Against three beekeepers in reach, the other attacks deal: the Tornado 90, the Hydra 120, the Scythe 135, the Meteor 180 and the Lance 180. For each wasp sent (its squad, 30% of the swarm against 35 to 37%), the Sting deals about 60% of the Tornado's. Its cooldown (1.5 s) and its three notes stay the quickest. For each wasp paid it is still the thriftiest at 800 wasps (4.4 damage a wasp; the Scythe 4.5), and the dearest at 4,000 (0.9), where a cast costs 50. `StingSongTests` checks the order per cast and per wasp sent at 400, 800, 1,200 and 4,000 wasps, by a measured cast against three beekeepers.
- **Two more levers, because of the Sting.** Fury takes 11.5 damage a point (was 8.5): the beekeepers the Sting no longer kills were left to the giant, which rose to 46% of the damage. Healing costs 4 wasps a point (was 3.5): the big songs that do the Sting's work cost more, so healing became a smaller share of the spending, and a five-seed win healed with 12%.
- **The bot.** It still plays the Sting when nothing bigger fits, which is what a player does with a quick jab. It now counts a Sting at its cast's damage, not a sting's damage times the stings, so it chooses a big song that fits whenever one is ready (`StrikeChoice`). Spending the Meteor on a lone beekeeper sooner (`RichInCasts` 6, 4, 3 or 2) changed nothing measurable, so it stays at 6.
- **The early waves.** Without nests, a bot that knows only the Sting, the Dome, the Gust and Hive Call (and the giant once Fury fills), the songs the lesson and the backwards law give first, clears waves 1 and 2 on all thirty maps and wave 3 on 29. With the Tornado and the Meteor as well, the next two the discovery order gives, it clears waves 1 to 3 on all thirty. The Sting alone still clears wave 1 on every map. A mixing player gets through the early waves because every song's first cast is free and the Tornado, the Meteor and the Dome cover what the Sting no longer kills. These were measured with healing at 4.5 wasps a point; it barely matters before wave 4.
- **Targets tightened** (`EconomyPlaytestTests`). In the wins no song but the giant deals more than 40% of the damage (was 50%), the Sting at most 25% and less than the top song, and the giant under 45%.
- **What was tried** (one Unity run each, many sets at a time, on thirty seeds). Capping the stings at 2, 3 or 4 with the old rule worked as well, but threw away the owner's growing count. With the shared rule: the Sting at 20, 18, 16, 14 or 13 a second stayed the top song (17 to 22%, the Meteor 16 to 18%), since the bot fills every gap with it; at 12 it falls under the Meteor once Fury is 11.5 or more, and at 10 always. Fury at 8.5 left the giant at 45 to 47%, at 10 to 10.5 at 42 to 44%, at 11.5 at 40%, at 12 at 38% (the Sting then often the top song again). Healing at 3.5 held the five seeds by a hair (one win at 15%); at 4.5 a win on seed 7 lost a hive, and at 5 a five-seed win healed with 15%.
- **The maps.** Mixed with nests wins all thirty. The hardest is seed 8 (31% of the hives' health kept, the weaker at 28%), then seed 7 (32%, the weaker at 6%) and seed 20 (33%); the easiest is seed 19 (69%), then seed 23 (68%) and seed 10 (65%). Mixed without nests wins none, reaching round 4 or 5 on every map. No map is unwinnable or trivial, so `LayoutGenerator` and `LayoutConfig` are unchanged.
- **Open for the owner.** Healing is thin across thirty seeds: 15 of 30 wins heal with less than 15% (seed 20 with nothing), against 6 before; the five checked seeds hold 16 to 25%. The Sting is now 16% of the damage, the Meteor 17%: the margin is small, and the sweep checks it. Whether a 44-damage Sting still feels good to press is for a person to judge.

### W. The giant throws red wasps

Added 2026-11-14. The owner: "I want that on the giant attack, each attack will throw off the bees on the target with a style. I want the thrown bees to be red." A look only: no damage, timing, reach or Fury changes, so section V's balance holds.

- **When and from where.** On the hit the rules already raise (`ColossusAttackLanded`), `ColossusMotor` throws a share of the wasps on the limb that struck. `ColossusThrows` (Logic) names the limbs by bone name (`ColossusLimb`: each forearm with its hand and fingers, each shin with its foot and toes, and the head, neck and upper chest; the hips, belly, upper arms and thighs never leave, so the trunk stays whole) and which a move throws: a punch the arm that punched, a kick the leg that kicked (the View picks the hand or toe furthest ahead on the hit, a kick's also by its height), the slam both arms, the roar the head and chest, the spin kick both legs, the leap's landing all four limbs. Input and the move's own animation never wait for it.
- **Shapes** (`ColossusThrowShape`, in WaspSwarm.compute): punch a cone straight out of the fist, streaming from it; kick a low fan swept across from the foot, one side leaving first; slam a ring racing outward along the ground; roar a spray rising from the head and chest in a wide arc ahead; spin kick one spiral arm unwinding outward and down around the giant; leap a crater ring with a second, slower ring that arcs up. Each wasp eases off its place, flies out for 40% of the throw's time, holds 0.05 s and streams home along a low arc, the starts spread over 0.12 s.
- **Numbers** (`ColossusConfig`, one `ColossusThrow` per move: share of the limb's wasps, spread either side of the facing, speed, seconds until back): punch 0.65, 14 degrees, 16 m/s, 0.6 s; kick 0.55, 60, 13, 0.65; slam 0.8, all round, 16, 0.75; roar 0.6, 70, 10, 0.75; spin kick 0.6, all round, 11, 0.7; leap 0.55, all round, 15, 0.8. Out share 0.4, the leap's second ring half the wasps at half the speed, the spiral turning 9 rad/s. On the baked body at 800 and at 4,000 wasps a punch throws about 4% of the body, the slam 10%, the spin kick 13% and the leap, the biggest, 18 to 19%; every move is back within 0.8 s (`ColossusThrowsTests`).
- **Red.** The colour is per wasp already (`WaspDynamic.tint`), so the state did not grow. A thrown wasp flashes white for 0.04 s as it leaves, turns to `ThrowColor` (1, 0.12, 0.04) over `ThrowRedSeconds` (0.08 s, through the tint's own easing), burns at heat 0.6 while out and fades back to gold as it rejoins. The flying fists turn red the same way and stay red through their burst, fading as they stream home. Like the Strike songs' red, the wasp shader's white core gives the highlights a salmon edge.
- **GPU and cost.** A ring of the latest four throws (`_Throws`, four float4s each) and each bone's limb bits (`_BoneLimbs`, 16 float4s, set once), pushed with the other uniforms; no new buffer, no readback, no allocation. A throw picks its wasps by seed in a band that moves on at each throw, so a quick chain of punches throws fresh wasps; a wasp two throws claim flies the later one, and a flying fist keeps its own. Main thread in the performance test 0.218 to 0.219 ms (0.223 before), garbage unchanged.
- **Tests.** `ColossusThrowsTests` (EditMode: limbs by bone, limbs and shape per move, config entries, counts, the fifth and the 0.8 s); `HiveDefenseGiantThrowTests` (PlayMode, reading the swarm back): on a punch, the striking arm's wasps leave on the hit, red, and are back and gold after the throw, the other arm whole; on a slam both arms leave and come back. Pictures `snap-23-throw-<move>.png` and `snap-23-throw-fist.png`, from the giant's side.
- **Open.** The config asset holds no throw values yet: the scene builder's Build keeps tuned values and does not save an unchanged config, so the code defaults apply until someone saves it. From the side the roar's spray and the punch's cone are both bursts ahead, told apart by height and width; whether they read as different enough in play, and whether the red is hot enough next to the gold glow, is for a person to judge.

### X. The swarm flows round trees and obstacles

Added 2026-11-14. The owner: "when the bees go through a tree or an obstacle or object, make a nice split and unite animation." Until now the free swarm (the river, the idle eight, wasps between forms) flew straight through trees, ruins, nest trees and hives. A look only: no rule changes.

- **Obstacles.** Once a match, after the map is applied (a restart loads the scene and does it again), `HiveDefenseRoot` sends the swarm one upright cylinder per box: each shown prop at least 2 m tall and each nest tree (the boxes `DrawMap` measures for the chase camera, before the nests' rings exist), and each hive's model (`HiveView.TryMeasureModel`, without its bars and rings). `SwarmObstacles` (Logic) turns a box into the box's middle on the ground, a radius of `ObstacleRadiusShare` (0.8) times the mean of its half-width and half-depth, and its top. The shader holds up to 96 (`OBSTACLE_MOST`); the saved map has 55. `SwarmGpuView.SetObstacles` uploads the fixed array and its count to the compute shader then and never per frame; `Initialize` clears it, so another scene never flows round the last match's.
- **The bend** (`FlowAroundObstacles` in WaspSwarm.compute, on the goal, once a frame, before the spring steps, so the springs fly the bent path and are never fought). In each obstacle's frame, along the stream and across it, the goal's distance across is eased out to at least the radius plus `ObstacleMargin` (0.45 m) from `ObstacleLeadMeters` (2.5 m) before the edge to `ObstacleCloseMeters` (2 m) past it, through a smooth maximum so it eases onto the edge instead of creasing. Each wasp goes round on the side its body is on (its seed picks when it is dead centre), and a push only moves it further onto that side, so the stream parts in two and closes behind. The goal also runs `ObstacleSpeedUp` (0.25) ahead past the middle and as far behind before it, so the wasps bunch a little before, fly 25% faster round and ease off after. Where the obstacle is fully on, the bent goal lies outside the radius plus the margin whatever the frame. Over an obstacle's top the bend fades out within 0.6 m; nothing above it bends. `ObstacleStrength` (1) scales it all.
- **The frame.** The stream's own way: the path's direction at the wasp's place in the river and the loop's tangent in the eight, blended as the wasp winds, signed the way the wasp flies. The first build used each wasp's velocity; the river's swirl turns that by up to 10 degrees, which flipped the side of wasps 2 m before the tree and threw a few through it at 20 m/s (124 wasp-frames inside, 1.6 m deep). Between a shape and the river, where there is no stream yet, the wasp's velocity is used, and a nearly still wasp is pushed out on its own side.
- **Who bends.** Each wasp carries how much of the free swarm it flies: 1 in the river, the eight and Collapsed, 0 in Hive Call's halo and on the giant, blended through each form's hand-over, times one minus a slot song's share. So Sting, Meteor, Lance, the towers, domes and every other slot song fly their own paths unbent; a wasp peeling off to a strike loses the bend as it leaves and one streaming home gains it back as it rejoins. The giant's body, its thrown wasps and its fists never bend.
- **Cost.** A loop over the obstacles per free wasp, culled by distance; no grid was needed. The GPU step of 4,000 wasps (timed in the test over 200 dispatches) is 0.012 ms with the 55 obstacles against 0.006 ms with none, under 1% of a 120 fps frame; the main thread is unchanged (0.220 ms on average in the performance test at 4,000 wasps, 0.218 before; garbage unchanged at the 290 B floor).
- **Tests.** `SwarmObstaclesTests` (EditMode: the box rule, skipping, the cap, the shader's array size, the defaults). `HiveDefenseFlowTests` (PlayMode, reading every wasp back each frame) walks the river 24 m straight through a nest tree, once with the obstacles and once with none: without them 38,326 wasp-frames are inside the tree; with them none are, 34% of the wasps going by take the left and 66% the right (the walk runs 0.25 m right of the middle), and 3.7 m past the tree's edge the stream is 1.75 m wide (10th to 90th percentile across it) against 1.81 m without the tree. The motion test's bounds hold: peak mean accelerations at 60 fps 13.0, 12.6, 10.6 and 12.9 m/s² on the start, stop, turn start and turn stop and 51.3 on Hive Call (13.0, 12.7, 10.4, 12.9 and 51.3 before), 60 against 120 fps 0.279 m/s apart (0.264 before; the turn walks east past an obstacle). Pictures `snap-24-flow-river-*.png` (six frames 0.15 s apart as the river passes the tree, from a steep camera beside the walk) and `snap-24-flow-eight.png` (the eight with the core standing 0.8 m past the tree's edge).
- **Open.** A destroyed hive keeps its standing height as an obstacle. A long wall's box is one circle, so the stream gives it a wide berth. The eight round a core standing at a nest tree to play its combo wraps round the tree, so its crossing is no longer straight over the core there; whether that reads well while playing a combo is for a person to judge.

### Y. Juice: one feel director, trauma shake and camera moments

Added 2026-11-14. The owner: "add more juice and creative camera work, animations, shaking and more. Research what successful games did to make the feel addictive." Cinemachine and the owner's paid Feel asset may not enter this public repo (packages need a team decision), so all of it is our own camera and UI code. Feedback only: no game rule changes.

- **The research, in short.** One trauma value drives every shake: events add 0 to 1, capped at 1, falling linearly; the view moves by trauma squared through smooth noise, one seed per axis, and in 3D it turns more than it moves (Eiserloh, GDC 2016). A weight ladder keeps it readable: hit < kill < medium (multi-kill, giant blow, hive hit) < big (slam, Meteor, hive lost, last kill) < huge; only medium and up shake visibly, only big and up slow time, and the moments of one frame merge to the strongest, never their sum. Hit-stop only on giant hits and never eating input (Sakurai). Rising pitch on kill chains (Peggle, the Mario stomp chain). Pop every number (Jonasson and Purho, "Juice It or Lose It"). Camera moments last about a second, ride on the follow and never steal control. Caps: roll at most 3 degrees, no HUD shake, no flash more than 3 times a second (XAG 118), and a 0 to 100% master scale for reduced motion. Effects never fix unclear rules: readability first.
- **The director.** `HitFeedbackView` already listened to every hit, cast, impact and giant blow; it is now the one feel director, with its numbers in a new `FeelConfig` (GameData/F010, made by the scene builder, reset by ResetConfigsAndBuild). Trauma by moment: a hit `HitTrauma` 0.12 x damage / 80 (so the Sting's light hits add least), kill 0.06, chain milestone 0.15, Tornado 0.15, Hydra and Nest 0.2, Lance 0.25, Meteor 0.45, giant punch, kick, spin kick and landed fist 0.3, slam, leap landing and third punch 0.55, becoming the giant 0.5, hive hit 0.2 (at most once per 0.4 s per hive), hive lost 0.7, wave cleared (its last kill) 0.35. Nothing shakes on a note press or during a nest combo.
- **The shake** (`TraumaShake`, Logic, and `FollowCamera`). Decay 1.2 per second, exponent 2, at full trauma 2.5 degrees of roll, 1 degree of tilt either way and 0.15 m of movement, noise speed 18. It replaces the old sine shake (0.6 m, 0.25 s). Steady Sting fire should stay at a low trauma whose square barely moves the view (an estimate, not measured); a slam or a lost hive is felt.
- **Camera moments.** A: a cast punches the zoom 4% narrower in 0.06 s and back over 0.3 s, with 1 degree of roll alternating sides (the old forward kick stays). B: the giant's punch, third punch, kick and landed fists kick the view 0.4 m along the giant's facing and spring back (4 Hz, damping 0.5). F: becoming the giant punches the zoom 8% with trauma 0.5, on top of the existing swoop and slow-down. The existing push-ins (heavy songs, the giant's heavy blows), the wave pull-back, the lean toward a hive under attack and the slow-downs on the giant forming and the wave cleared are kept; the slow-down stays the root's, so one owner sets the time scale. Every moment is an offset on the follow, so steering moves the view at once, and the view ends exactly where the plain follow is.
- **Kill chain** (`KillChain`, Logic). A kill within 0.5 s of the last extends the chain; each kill's pop plays one semitone higher, up to an octave (`HitSoundView`); at 3, 6 and 10 kills a gold "x3 / x6 / x10" stamp lands from scale 1.6 in 0.12 s high in the middle and fades over 0.9 s (`DamageNumbersView`), with trauma 0.15.
- **Number pop.** Every hit number pops 0.6, up to 1.2 (a kill's to 1.27) and back to 1 over 0.15 s, through the style's scale only, no layout.
- **Master scale.** `FeelConfig.MasterScale` (1) sets `FollowCamera.ShakeScale` at the start; 0 stills every shake, kick, punch and nudge and turns off the shield flash, the Tornado ring and the cast's slot flash.
- **Cost.** No allocation per frame: the trauma, the chain and the spring are fields; the stamp texts are made once; the camera reads `Mathf.PerlinNoise` five times a frame. No new pass, no readback.
- **Tests.** `FeelRulesTests` (EditMode: trauma adds, caps, falls, squares, clears; the chain climbs, restarts, stamps each milestone once, tops out). `FollowCameraTests`: trauma falls to rest and the view ends where it began; the master scale at 0 leaves the camera still; a moment yields to steering every frame and ends where the plain follow is. `HiveDefenseFeelTests`: a note pressed during the swarm's hit-stop is not dropped and the time scale is untouched. Pictures `snap-25-juice-tornado.png` and `snap-25-juice-giant-forms.png`; `snap-17-giant-punch.png` shows the kick.
- **Left out, for now.** Local hit-stop on the victims and a fist wind-up, the note-press pop and the swarm's breath, the song-completed release and the failed fizzle, the nest combo's rising chime and stream of wasps, the last-kill zoom (moment D), the hive alarm move (moment E), the wave-intro drop (G), look-ahead, ground decals, sound variation pools and ducking, the song streak, the round tally and farm-phase goals. Each fits on this director and FeelConfig later.
- **Open.** How hard the shake and the punches feel in play, whether the x3 stamp is loud enough or too loud, and whether the kill pop's climb reads over the flute, are for a person to judge; pictures cannot show motion. The shake runs on game time, so it lasts longer under the slow-downs.

### Z. Every song checked for lag, and its weight in the feel

Added 2026-11-14. The owner: "refine the combos' feel and animations, make sure everything is smooth and not laggy, go through each combo and make sure it does not cause lag." Measured in the built Mac player (`player-bench.sh`: 5120x2160 at 120 Hz, so 8.33 ms is a perfect frame; every song cast twice, the six attacks into waves of 8 and 39, the giant's nine moves, the nest combo and a walk past the obstacles). No damage, cost, hit time or range changed.

- **The benchmark missed the giant.** Since section V a point of Fury costs 11.5 damage, and the benchmark still added 1,000: the first giant was refused and the giant's nine move windows (with section W's thrown red wasps) never ran. It now adds damage until the meter is full. It also records `gc_used_bytes` (the "GC Used Memory" counter, which a release player keeps), so a drop on a long frame shows the garbage collector ran in it, and takes `-f010BenchOff nocursorlock`, which stops the chase view locking the mouse.
- **Before, per song** (longest frame outside setup; 2,000 wasps / 4,000 wasps): Sting 10.8 / 11.6 (the process's first cast), Tornado 9.7 / 12.0, Meteor 9.6 / 10.0, Hydra 9.4 / 9.7, Scythe 9.5 / 9.4, Lance 9.4 / 9.6, Dome 19.0 / 22.1, Gust 9.8 / 10.1, Honeycomb 9.1 / 11.0, Nest 15.6 / 9.5, Hive Call 9.3 / 9.5, Colossus 13.5 / 16.6, the giant's moves - / 10.1, the nest combo 9.4 / 9.4, the walk past the obstacles 12.4 / 10.1. Means 8.33 to 8.43 ms and 99th percentiles 8.7 to 10.5 ms in every window.
- **The long frames, one by one.** In every run, two long frames come at the same real time after launch, about 19.6 s and 24.9 s, whatever the game is doing then (an idle Dome window or the wait after a Tornado, before or after its cast, at 2,000 or 4,000 wasps; 18 to 35 ms): the player's, not a song's, and once a session. The GC counter does not drop on them, so they are not garbage collection. Nest 15.6 ms (two frames after a Nest's cast) and the last wave's 124.6 ms (14 frames after the benchmark puts all 39 beekeepers on the field at once) came once in the first run and never again in three more. The first Sting of the process runs 10.8 to 13.9 ms with the main thread at 9.1 ms, so its rest is on the GPU side; section Q's switches found no single cause, and it is once a session. **The giant's start and end were real:** 13.5 ms the frame after it forms, and 12.5 + 16.6 ms and 19.9 ms within five frames of it ending, every time. With `-f010BenchOff nocursorlock` at 4,000 wasps they are gone: the Colossus windows top out at 9.6 ms (16.6 with the lock), the giant forming at 8.9 (12.2), the frames after it ends at 8.9 (19.9). The cause is the chase view locking and unlocking the mouse cursor (`Cursor.lockState` in `HiveDefenseRoot.LockCursor` and `UnlockCursor`), a window-server call on macOS. It is not changed here: the lock is what lets the mouse turn the giant's view, and taking it out is a design choice (see Open).
- **The feel's weight ladder.** Every cast punched the zoom 4% and rolled 1 degree, so the Sting, cast every 1.5 s, rocked the view as hard as a Meteor's cast. `CastWeight` (Logic) gives the Sting `FeelConfig.LightCastShare` (0.5) of the zoom punch, the roll and the forward kick; every other song punches fully. The heavy impacts were mild under trauma squared (the Meteor 0.45 shook at 0.2 of the full shake, the slam 0.55 at 0.3); the Meteor's landing is now 0.6 (0.36: about 0.9 degrees of roll) and the giant's slam, leap landing and third punch 0.65 (0.42), between the Meteor and a lost hive (0.7). `CastWeightTests` checks the share, its bounds and the ladder (Tornado < Hydra < Lance < Meteor < giant heavy < hive lost, the Meteor at least a third of the full shake). `HiveDefenseSceneBuilder.ResetOneConfigAndBuild` with `-f010ResetConfig FeelConfig` remade only the FeelConfig from its defaults, so no other config lost a tuned value.
- **The look, song by song** (`F010ClipTakes.Take4_SongStrips`: each song cast from a core 6 m south of the middle with three stunned sticks ahead, six game-camera frames from the cast to the settle, 800 wasps above and 4,000 below, one `strip-<Song>.png` each under the snapshot folder's clips/take-4-song-strips). Sting: red peel at 0.1 s, streams by 0.25 s, the white hit flash, then red wraps round each target (two at 800, three at 4,000), gold again on the way home. Tornado: red gathering, the white burst and the orange ring at the hit, the ring fading by 1 s, wasps streaming home. Meteor: a tall red column climbing out of the top of the frame, the dive, a flat ring on the ground at 1.1 s, a dissolving cloud at 1.4 s, the eight back by 2.2 s. Hydra: the necks rear and curl, bite the three sticks with white hit flashes, and draw back. Lance: the draw back behind the core, a long thin spear by 0.25 s, the flash at its tip, the wasps hanging red along the line and then home. Colossus: the giant stands formed and the chase view settled by 0.8 s, solid at 4,000 and readable at 800. Every shape reads at both sizes; nothing was changed in the shader's form code.
- **After, per song** (2,000 wasps / 4,000 wasps with the cursor lock off): Sting 13.9 / 11.9 (the first cast), Tornado 20.1 / 18.6 (both the 24.9 s frame in its wait; every other Tornado frame under 9.4), Meteor 9.5 / 9.9, Hydra 9.6 / 9.8, Scythe 9.8 / 10.0, Lance 9.8 / 9.9, Dome 23.1 / 35.5 (the 19.6 s frame; otherwise under 9.7), Gust 9.4 / 10.1, Honeycomb 9.4 / 9.1, Nest 9.4 / 11.7, Hive Call 9.4 / 9.9, Colossus 12.4 / 9.6, the giant's nine moves 9.9 / 8.8, the nest combo 9.6 / 9.0, the walk past the obstacles 9.8 / 10.6. 99th percentiles 8.5 to 9.7 ms in every song window. No song, attack into a wave, giant move, combo or obstacle flow makes a frame over 12.5 ms of its own; the feel changes cost nothing measurable.
- **Timings.** Unchanged and still pinned by tests: Sting 0.35 s (`StingSongTests`), Meteor 1.0 s (`MeteorSongTests`), Hydra 0.5 s (`HydraSongTests`), Lance 0.35 s (`LanceSongTests`), the shader's own reads tied to them (`WaspSwarmTimingTests`); the cast's feedback still fires on the cast's own frame from `SongCastNow` (`HiveDefenseColdCastTests`).
- **Tests.** EditMode 1,857 of 1,858 (the explicit thirty-seed sweep skipped; `CastWeightTests` new, 10 cases), PlayMode 148 of 148 with the filter `HiveDefense|SwarmFluteVoice|FollowCamera`, the scene builder OK, no new compile warning. Runs `songs-before`, `songs-mid4000`, `songs-after` and `songs-after4000-nolock` under the logs' bench folder.
- **Open.** The cursor lock's long frame at the giant's start and end (about 4 to 11 ms over a frame, once each): keeping the lock for the whole match would need another way to aim with the mouse in the top view, and dropping it lets the cursor leave the window while turning the giant's view; the start is under the slow-down and the swoop, the end is felt. Whether to keep it is for the owner; the benchmark switch shows the gain. The two player frames at about 19.6 s and 24.9 s after launch are outside the game's code and come in the first minute of a session. The Meteor's climb leaves the top of the game camera's frame between about 0.4 and 0.9 s, so its wind-up is half seen; a lower climb is a look value on the song asset, left alone because that asset holds the balance just retuned. How the lighter Sting punch and the heavier Meteor and slam feel in play is for a person: pictures cannot show motion.
