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
Scythe    A   K L H     4s    1.0s                 .37   S  At 0.3 s, 45 dmg to everyone within 4.5 m in the half-disc facing the aim. | a pack in front of you.
Lance     A   K L L H   8s    1.3s                 .37   S  0.4 s draw, then at 0.5 s a line 14 m long and 1.2 m wide: 60 dmg to each beekeeper on it, pushed sideways at 5 m/s. | a far smoker, a file walking in.
Nest      A   K L J H   8s    up to 15s            .15   P  Lobbed to the aim (10 m max), arms at 0.6 s. The first beekeeper within 2.5 m sets it off; 0.3 s later 70 dmg and 1 s stun within 3.5 m. | a spawn lane.
Dome      D   H K J     8s    5s                   .35   G  As today: hits on the Piper cost 85% fewer wasps, Piper at x0.5. | a melee crowd on you.
Maelstrom D   H K K J   14s   3s                   .35   G  A 5 m disc at the aim (12 m max). Beekeepers inside are pulled to its centre at 3 m/s and cannot start an attack (0.2 s stun, renewed). | a crowd on a hive.
Gust      D   H L K     6s    0.8s                 pulse -  Clears smoke within 6 m (as today). Beekeepers within 3 m are pushed out at 4 m/s. | smoke on you or on a hive.
Wings     D   H L L K   12s   5s                   .30   G  Piper at x1.6 speed, stick hits cost 50% fewer wasps, and smoke does not slow the Piper. | running between hives.
Honeycomb D   H J L K   15s   10s                  .20   P  A shell on one hive: 75% less damage from sticks and smoke, and each stick hit on it deals 10 back. | both hives attacked.
Hive Call P   L J L     3s    see C                rest  -  As today: rooted, +100 wasps/s per standing hive. Ends when the Piper moves, an Attack or Power song is cast, the swarm is full, or the Piper is hit while no Guard song runs.
Colossus  P   L J K J L Fury  12s                  rest  -  As today; its category moves from Attack to Power. Its notes are revealed when Fury first fills, or when it is played whole before that.
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

The swarm grows during a match. Strength comes from the **swarm level**, never from the live wasp count, because the count is also the player's health: a hurt swarm must not hit softer.

| Value | Start |
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

| Value | Start |
|---|---|
| Lasts | 20 s, plus 2 s for each beekeeper the giant kills, 30 s at most |
| Fury | 1 point per 3 damage (was 2), so the giant stays a highlight |
| Size and damage | Follow the swarm level |

While the giant stands, H J K L are its moves and do not reach the songs:

| Keys | Move | Rule |
|---|---|---|
| J | Punch | The existing three-hit chain; the third is heavy |
| K | Kick | As today |
| L | Ground slam | 40 damage and 4 m/s knockback within 5 m of the giant. 3 s cooldown |
| H | Roar | Stuns every beekeeper within 6 m for 1.2 s. 6 s cooldown |
| K K | Spin kick | The second K within 0.6 s: 55 damage all around within 3.5 m |
| L L | Leap slam | The second L within 0.6 s: the giant leaps to the aimed spot, 10 m at most, lands after 0.6 s and deals 70 damage within 4 m |

- Space or left mouse still punches, and Left Shift or right mouse still kicks.
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

| Value | Start |
|---|---|
| Farm phase | 35 s, and 45 s in the first round |
| Call the wave early | Enter. Each second skipped gives 5 wasps, up to the capacity |
| Survive phase | The wave. It ends when every beekeeper of the wave is dead |
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
| Sting | 10 | Cheap on purpose, so the player always has a move |
| Gust | 20 | |
| Dome | 40 | |
| Tornado | 60 | |
| Meteor | 90 | |
| Colossus | 0 | Fury is its price |
| Hive Call | 0 | |
| Healing a hive | 2 wasps for each point of health | Hold E within 3 m of a damaged hive: 60 wasps a second flow in |
| Towers (later) | Honeycomb 150, Nest 100 | The wasps stay there |

- Costs apply only in a survive phase.
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
- Later: station wasps as a tower at the hive you are leaving.

The playtest bot plays three plans through whole matches: attack only, heal only, and a mix. The economy is right when none of the three always wins and doing nothing always loses. The numbers above are the starting point for that tuning.

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
