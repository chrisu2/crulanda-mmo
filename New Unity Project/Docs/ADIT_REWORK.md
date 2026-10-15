# The Sealed Adit, reworked (design; 2026-10-14)

## Chris's words (2026-10-14, verbatim)
"what are the three new dungeons? you should have the build guide on levels. i think adit dungeon need to be more complex like wow's
wailing caves or the deadmine. adit is nothing like those"

**Status: BUILDING, OPEN.** Written in the worktree `D:\code\mmo-adit` (branch `adit-rework`). Build steps 1 (the builder), 2 (the
layout and its content, section 14) and 3 (the run, section 15) are written and compile; Unity has not been run on them and nothing has
been committed.
It holds over section 4 of DUNGEON_DESIGN.md once Chris approves it; until then section 0 there (the standing brief) holds over everything here. Lessons only: no World of Warcraft names,
layouts or assets anywhere in the game. British English throughout.

**Labels.** Every new place, person, camp and mechanic below is **GAME-ONLY**. What is CANON or CANON-EXPANDED is in section 13. The
lessons come from the Deadmines, Wailing Caverns and Scarlet Monastery (Chris's three, DUNGEON_DESIGN 0).

Every code name in this plan was grepped against the worktree. The new class, file and field names are free;
`EncounterSession.Spread.cs` is taken, so the Breach event goes in `EncounterSession.GreySpread.cs`.

## 0. Chris's first question: the three dungeons after the Adit, and the level guide (answered first)
The three dungeons after the Adit (DUNGEON_DESIGN 0.4; ROADMAP Phase 9):
- **The Sludge-Tunnels, levels 18-20, in the Forge District:** sewer tunnels into a built smelting works and a syndicate's
  counting-house.
- **The Deep Veins, levels 23-25, in the Warrens:** the penal-legion mine, with a built prison block and an overseer's keep.
- **The Transit Hub, levels 27-29, in the Crown:** a built junction of platforms and cargo halls. Its last boss drops the letter that
  opens the Spire. Its "lifts" must be hand-worked (rule 0.05).

The level guide (DUNGEON_DESIGN section 0, Chris 2026-10-10):
- **low, 10-15:** the Sealed Adit (10-12);
- **mid, 15-25:** the Sludge-Tunnels and the Deep Veins;
- **high, 25-35:** the Transit Hub. 35 is past ROADMAP's cap of 30 and is still to be settled.

## 0.1 What the research says (lessons only; short; sourced)
The three dungeons Chris named, read again for what makes them more than one long way through. Only the shape of each lesson is
taken: no names, layouts or assets. Sources are listed under the table.

| # | The lesson | From | Where the Adit takes it |
|---|---|---|---|
| L1 | **A web, not a queue.** The lieutenants fall in any order before the finale opens, so the dungeon is non-linear. | Wailing Caverns [WH-WC; DD2] | three sigil wings in any order off one hub (sections 2, 9) |
| L2 | **The way home is new ground.** After an optional boss a jump off a waterfall takes you back toward the entrance, so nobody walks a wing twice. | Wailing Caverns [WH-WC; EXP-WC] | every wing ends in a drop or a loop onto the hub; the dead ends left are optional side rooms |
| L3 | **Enter high, work down; fight up a ramp.** The foundry has ramps and catwalks round its edge, so you come in above the floor and fight your way down; the ship at the bottom is climbed by ramps. Raised ground with a ramp makes a pull come down to you, or makes you fight up. | the Deadmines [WW-DM] | the Foundry's catwalk, walkway and ramps; the trestle, the bridge and the tower; the tally stage on its spoil heap |
| L4 | **The reveal is seen from above.** Tight tunnels open on a vast cave you look down into before you walk it. | the Deadmines [DD2; WW-DM] | the overlook lip 14 m up the Singing Gallery; the Rail Hall stays the second reveal |
| L5 | **Doors the bosses open, and patrols behind you.** A boss's death opens the next door; after each of the first bosses a few elites wander the room and the mine round it; the last door wants powder or a picked lock. | the Deadmines [WW-DM; DD2] | bossdoors, camps released after a boss, the powder and the quiet gate (sections 4, 6, 8) |
| L6 | **Wings with their own look, sized to your time.** Four wings, each a short run to its own end boss, easiest to hardest; one wing's last boss gives the key to others. | Scarlet Monastery [WH-SM] | the chapter-house: the garden, a fork of library or yard, the chapel; the sigils as the keys |
| L7 | **The finale pair.** The priestess puts the party to sleep, raises the commander and heals them both: kill her first or fight him twice. | Scarlet Monastery [WH-SM; DUNGEON_DESIGN 0.1] | Cinder-Warden Ysolt and Sister Ashe Varrow (section 5) |
| L8 | **Waves with breathing room, then a surprise.** The escort is held at its spot against waves, then a final boss comes. The gaps between waves let a party recover; the boss comes from where the party has been looking. | Wailing Caverns [WH-WC; DD2] | the Wake on the island (section 6, E1) |
| L9 | **A maze loses people.** The cave's remembered weakness is getting lost: keep the branches, make every branch obvious. | Wailing Caverns [DD2] | lamp rings at the wing mouths, the lichen trail, two spirit stones, a cave map by height (sections 3, 9, 11) |
| L10 | **Things off the path.** Optional bosses off the main way, a rare, a hidden boss; casters who put you to sleep; wounded beasts that flee for help. | all three [WH-WC; WW-DM; DD2] | Brack, Old Bristle, the Old Burrower, Old Kettle, the Quiet Miner; the Lulls; the crawlers that flee |

Sources (read 2026-10-14; paraphrased, nothing copied):
- [WW-DM] Warcraft Wiki, "Deadmines (Classic)": https://warcraft.wiki.gg/wiki/Deadmines_(Classic)
- [WH-WC] Wowhead, the Wailing Caverns strategy guide for Classic: https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic
- [EXP-WC] expcarry, Wailing Caverns route guide (the waterfall shortcut back toward the entrance): https://expcarry.com/wow-forever-wailing-caverns-guide
- [WH-SM] Wowhead, the Scarlet Monastery strategy guide for Classic: https://www.wowhead.com/classic/guide/scarlet-monastery-dungeon-strategy-wow-classic
- [DD2] DUNGEON_DESIGN.md section 2 (Draft 1's research, 2026-10-07) and 0.1.

## 0.2 The audit: where today's Adit falls short
What the Adit is today, checked against the data, the builder and tonight's run (evening-3:
`D:\crulanda-work\run-captures\adit-group-1013-evening-3\run-log.txt`: a level-11 Paladin with a Warrior tank, a Druid healer, a
Ranger and a Rogue, led by the tank; 71 min 02 s, 3405 s of it fighting, every camp cleared).

**What it already does right (kept):** the length (71 minutes is inside 0.06's 60-75); the bosses that hold the party (0.07); the
duo; the quiet and loud ways through the platform gate; the Weaver's escort; the ride out; every boss dropping a named piece (0.075);
the scorecard.

**Where it falls short (the gaps):**
| # | The gap | What the Adit does today (evidence) | Lesson | Fixed by |
|---|---|---|---|---|
| G1 | **One queue, not a web.** | A tree of four tubes: the Sealed Adit and three caves `within` it (the Geode Floor, the Ember Vent, the Grey Breach; adit.json props of kind "cavern"). The main tube ends at about z +284. The leader orders the camps by `along` down each cave and plays it as one route: "Leader: Tamsin Larkin, 67 camps", cleared 1/67 to 67/67 in a row. Each wing is a dead end, and about 571 m of every run is walking back out of them. | L1, L2 | sections 2, 9 |
| G2 | **Respawns fill the walk back.** | 65 of 67 camps respawn at 900 s (two at 1200). After Nix (28:10) the next camp fell at 35:25: 7 min 15 s, with Gang-Boss Lusk fought a second time at 33:25. After Ysolt (45:22) the next fell at 52:45: 7 min 23 s against the Workings' diggers, pickets, overseers and sappers again. About 15 of the run's 71 minutes. | L2 | Q1 (respawn 7200), no walk-backs |
| G3 | **One height.** | Every fight is on a tube's floor. The foundry's catwalk is dressing at 2.7 m that nobody walks (no `NavWalkable` anywhere in ZoneBuilder.Adit.cs). No decks, no drops, no water to wade. The only reveal is the Rail Hall, at the very end. | L3, L4 | the overlook, the trestle, the bridge and tower, the Foundry's catwalk, the chute and falls drops |
| G4 | **The hub is a corridor.** | The Singing Gallery is a stretch of the main tube with three side doors: no pools, no falls, nothing to cross at heights. The Foreman's short way back is still not built (DUNGEON_DESIGN 9.3). | L2, L4, L9 | the Singing Gallery (section 3, G) |
| G5 | **The stronghold is dressing in one tube.** | The chapter-house (`ChapterHouse` in ZoneBuilder.Adit.cs) is an ash-garden vestibule, one nave round the lava lake with shelves and lecterns down one side and racks and dummies down the other, and a chapel: one boss (Ysolt). 0.25's four wings with an end boss each, and the priestess who raises the Warden, are not there. | L6, L7 | V1, VS, VM, VC (section 3) |
| G6 | **Few bosses, and nothing walks in behind you.** | 8 elites in all: Lusk, Sorrel, Danner, Nix and the Rock-Eater, the Vent-Hound, Ysolt, the Foreman, the Quiet Miner. A boss's death only wakes kin within reach (`DeathCall`); no camp is released after a boss. No mini-boss in a flooded working, and the Vent-Hound is the only optional boss. | L5, L10 | sections 4, 5, E2 (20 elites) |
| G7 | **One event.** | The Weaver's escort is the only event; the three sigils just open the winch gate. | L8 | the Wake (E1) |
| G8 | **No secrets.** | adit.json has no `secrets` at all. | L6, L10 | section 7 |
| G9 | **The leader assumes one way down each cave.** | `GateAhead` and `GateBefore` (EncounterSession.Adit.cs:245-258) find a camp's gate by cave and `along`. In a network that fails: the Foundry hall's camps lie past the chute door by `along` but are reached from the other end. | L1 | `ZoneCamp.gate`, written by the gate flood (section 8) |
| G10 | **The maps show the land.** | The minimap and the zone map draw `ZoneBuilder.MapTexture` (HudMaps.cs:176, :243), which says nothing about floors at different heights. | L9 | the cave map (S3) |

## 1. Decisions to ask Chris at S0 (the question prompt; recommended answer first)
**Answered 2026-10-15 ~12:45 (the question prompt; Chris's picks verbatim): Q1 "20 minute respawn"; Q2 "Keep ~75 min (Recommended)";
Q3 "Build the Cove"; Q4 "Yes, add them".** So: every camp respawns at 1200 s (adit_layout.py RESPAWN, adit.json); the length stays;
S6 is built; the SwingLoad buckets go in (S7), hand-hauled, nothing that reads as a machine. Asked that morning after Chris's "i told
you to finish adit before shutting down" (the night's end had gone to the lag, the published build and a shutdown).
**Q1. No respawns while you are inside?** Recommended: yes.
- Data only: every Adit camp gets respawn 7200 (today 900, two at 1200).
- Leaving and coming back builds a fresh Adit anyway.
- Evening-3 (71 min 02 s, 3405 s of it fighting) lost about 15 minutes to respawned trash (0.2, G2).

**Q2. Length**, at evening-3's pace (about 45 s a trash fight, 2-4 minutes a boss):
- required (the leader's quick run): about 68 minutes;
- the default run: about 75 minutes;
- everything: about 90 minutes.
OK, or trim to about 60 by cutting 8 trash camps and making the Urn-Keeper optional?

**Q3. The Cove** (stage S6, last and cuttable):
- the line runs on into a black-water cove;
- Danner fights on a moored ore barge hauled by rope teams;
- no cannon, no engine.
Build it, or keep the Rail Hall finale? Recommended: build it last, behind data.

**Q4. Ore buckets swinging on ropes across the Foundry's catwalks, hand-hauled?** Recommended: no.
- Treat it as no until he says yes.
- The hand-turned cutting wheels and the two great goblin-pumped bellows are cut either way. Only small hand bellows stay, at the
  forges.
- Memory rule: "NO Machines", no contraptions at all. Section 0.25 was written before 0.05.

**Done without asking:**
- "The Drill Yard" becomes **"The Muster Yard"**, and "Drillmaster" becomes **"Master-at-Arms"**, because the memory rule lists
  "drills".
- The cook on a raft becomes **a pitch-boiler in a pit** (Old Kettle).

## 2. The shape
**Today:**
- The Adit is a tree of four tubes (the main tube ends at about z +284).
- The leader turns it into one queue of 67 camps, and about 571 m of every run is walking back out of dead ends.

**The rework:**
1. **A descent through the Old Workings:** a bowl, a stair, a tally hall, a dark drift.
   - It braids round an optional Flooded Working.
   - It comes out on a lip 14 m up the wall of one great natural chamber, the Singing Gallery. **This is the reveal.**
2. **The hub is crossed at three heights:**
   - a timber trestle down its west wall (+14 to +12, then down to the floor);
   - a rope bridge with a timber tower at +12;
   - the floor of pools, a shallow river and an island.
3. **Three required sigil wings leave the hub at different heights and can be done in any order:**
   - **The Geode Foundry:** entered high, from the bridge into a built hall's catwalk; left by the chute drop (+6) into the hub's
     chute pool.
   - **The Cult of Ash's chapter-house** (the stone stronghold): the Ash Garden, then a fork of the Scriptorium or the Muster Yard.
     Both open on the Chapel of Embers, then the cloister stair loops home onto the hub's NE floor.
   - **The Sunless River** (natural water caves): a wet/dry braid, the Lurker's Pool and the falls stair, into the Grey Breach, then
     home by the falls drop (+12) into the hub's plunge pool.
4. **No required wing is walked back through.** The dead ends that are left (the Pen, the Quiet Miner's drift, the Pitch Pit) are
   optional side rooms.
5. **The three sigils start the Wake,** a hold-the-island event in the hub. It opens the singing door to the Deep Way:
   - the shaft stair;
   - the platform gate;
   - the Rail Hall (Sorrel, the Weaver, Danner).
   S6 may extend this with the Dead Line and the Black Cove.

**Approximate placement** (x east, z north; the mouth stays at 0,-370, so the Peaks exit is unchanged). adit_layout.py v2 solves the
exact rows.

| Cave | Runs | Extent incl. rock (x; z) | Floors (m below the yard) |
|---|---|---|---|
| W The Sealed Adit (root) | (0,-370) north, then NE to the hub's SW wall at (30,-14) | -26..41; -379..-3 | 0 to -44 |
| FW The Flooded Working | off the bowl's NW, west and north, into the tally hall's W wall | -78..3; -284..-167 | -8 to -16, then -26 |
| P The Pen | off the tally hall's E wall, east | 7..86; -196..-164 | -26 |
| G The Singing Gallery | W 240, east and north (the sump drift), chamber x 24..100 by z -42..+92, north neck (62,100) | 7..106; -101..124 | -33 to -58 |
| F1 The Cutting Drift | hub E wall (98,-2) at +12, SE to the cutting floor, NE into F2's far end | 89..238; -84..7 | -46 to -38 |
| F2 The Geode Foundry | hub E wall (98,20) at +6, east: chute, hall, berth | 88..286; -27..37 | -52 to -50 |
| V1 The Ember Vent (steps and garden) | hub E wall (98,60), east | 88..212; 40..84 | -58 to -66 |
| VS The Scriptorium | garden N wall, north, to VC's W door | 152..206; 66..188 | -66 to -70 |
| VM The Muster Yard | garden E end, east then north, to VC's E door | 186..310; 42..188 | -66 to -70 |
| VC The Chapel of Embers and cloister | chapel (205..258, z 185), cloister SW to the hub's NE floor (96,90) | 86..282; 80..209 | -70 back to -58 |
| R The Sunless River | hub W wall (22,48), west; pool at about (-125,35); falls stair north | -194..33; 5..105 | -58 to -62, then -40 |
| R2 The Dry Ledge | R 38, south loop, back into R at 122 | -114..-5; -10..45 | -58 to -62 |
| B The Grey Breach | falls head (-178,90), east at z 100-120, to the falls lip on the hub's W wall (26,75) | -181..37; 70..140 | -40 to -46 |
| Q The Quiet Miner's Drift | B 100, north 45 m | -115..-85; 125..195 | -42 |
| D The Deep Way | hub neck (62,100), north stair, the Rail Hall running west (40..-90, z 300); S6: the Dead Line west and the Cove at (-320, z 232..112) | -130..90 (S3-S5), -381..90 (S6); 61..346 | -58 to -100 (S6: -121) |
| DP The Powder Cubby | D 120, west | 28..60; 216..236 | -95 |
| KP The Pitch Pit | the Rail Hall's NW corner, NW | -181..-109; 321..368 | -100 to -108 |

**Extent check:**
- Everything, rock included, lies within x -381..310 and z -379..368. That fits the 800 m zone (±400). A Python sketch of these
  polylines found no plan overlaps except at mouths and joins.
- About 2.9 km of cave without S6, about 3.2 km with it. Today it is about 1.4 km.
- Layout check 8 makes this a hard gate. If any ring plus its 6 m knoll margin passes ±395, the zone size goes to 1000 at S0,
  before anything is built.

**Stacking rule** (this keeps away from deep engine changes):
- No cave lies over another cave in plan, except:
  - a child's first metres in its parent's air (as today);
  - a joining cave's last 25 m in its target's air.
- Every other "many floors at one x,z" is a deck inside a cave.
- There is no Sump or Well, and no floor built in cells.

## 3. Every space (look; size; floor; links)
Notation:
- "+n" is a lip or deck height above the floor of the cave it stands in or opens into.
- "within X at s, rise r" means the mouth is on X's floor at s, r metres up.
- "joins X" means the end stays full width and opens into X's air.
- Floors are in metres below the yard.

### The Old Workings (wing "workings", L10-11, the Sandthrone; green lum-tubes until the tally hall)
- **W1 The Cut, W 0-40.**
  - Tunnel (a timbered drift), half-width 3-4, height 4.5. Floor 0 to -2.
  - The mouth is on the yard. It runs on into W2.
- **W2 The Cage Bowl, W 40-110.**
  - Tunnel chamber, 70 m long, half-width 20, height 12.
  - Floor: -3 at the south rim, -12 at the bottom (W 78), -8 at the north rim.
  - The overseers' walk: a timber deck 3 m wide along the west wall, railed.
    - Its top stays level at -3 from W 45 to W 90 (rise 0 to +6.5 over the falling floor), then ramps down to -8 at W 108 (under 20
      degrees).
  - The five goblin cages (anchors cage1-5) stand on the bowl floor.
  - Links: on to W3. The Flooded Working leaves the west wall at W 100.
- **W3 The Sappers' Stair, W 112-165.**
  - Tunnel, half-width 3.4 (two 6 m alcove bulges at 125 and 145), height 4.6.
  - Floor -9 to -26 (18 degrees), with timber treads.
- **FW The Flooded Working** (OPTIONAL; within W at 100, west wall; joins W4's west wall, level).
  - Tunnel: an old timbered working, half drowned, about 160 m.
  - A drift at half-width 3.4, floor -8 to -14.
  - The bailing chamber: 25 m long, half-width 10, height 7, floor -16 under 1.1 m of standing water (wading).
  - A rising drift, -16 to -26.
  - The bailers pass buckets by hand down a line of men (living, no machine).
  - This is the braid of 0.25's "a flooded lower working with a mini-boss".
- **W4 Lusk's Tally Hall, W 172-215.**
  - Built hall: timber and cut stone, 2 courses, columns. Half-width 14, height 9, floor -26.
  - A spoil heap carries the tally stage: a deck 14x9 m at +3.5, ramps north and south (the hill-with-a-ramp lesson, L3).
  - FW comes in on the west wall; the Pen leaves the east wall (W 190).
  - The far door is Lusk's door (a bossdoor).
- **P The Pen** (OPTIONAL; within W at 190, east wall).
  - Drift (half-width 2.8), then a timber-stockaded pen chamber: half-width 10, height 7, floor -26, 45 m in all. A dead end.
  - The pen gate is opened by "Lift the pen bar".
- **W5 The Dark Drift, W 216-300.**
  - Tunnel, half-width 3.2, height 4.4, floor -26 to -42. No lamps; the living lichen trail starts here.
  - At W 240 the sump drift (G's mouth) leaves the east wall, behind the sump bar.
- **W6 The Overlook, W 300-315.**
  - Cleft, half-width 5-6, height 6-8, floor -44.
  - W joins G: the end is a lip +14 up the Gallery's SW wall. **The reveal:** the whole hub below, the falls on the NW wall, the
    chute pool to the east, three lamp rings, the bridge.

### The Singing Gallery (G, the hub; a natural great cave in pale light from high up; spirit stone 1)
- **G0 The Sump Drift, G 0-100.**
  - Tunnel, half-width 3.4, height 4.5, floor -33 to -58 (16 degrees).
  - It hooks east of W5 and enters the chamber on its axis at the south end.
  - The sump bar at G 6 is lifted from the hub side only: the way back up to the yard for a group that leaves early (0.25's
    collapsed-shaft shortcut).
- **G1 The Chamber, G 110-245.**
  - 125 m long, half-width 38-40 (about 80 m across), roof 26-30 m, floor -58 level, north neck half-width 6.
  - **The Miners' Trestle** (timber deck, 3.5 m, rope rail on the open side), pass 1:
    - from the overlook lip (+14, z -14) north along the west wall to the bridge landing (+12, z -2);
    - then on down the west wall to the floor at about z +30 (16 degrees).
  - **The Rope Bridge** (rope deck, 2.6 m, rails both sides) at +12, from the landing to F1's doorway on the east wall, about 70 m,
    pass 2.
    - At mid-span stands **the Tower**, a timber 4x4 m platform at +12 on posts from the floor; the post blocks end under the deck.
  - **The floor** (pass 3):
    - a shallow river, a 5 m band 0.6 m deep between rubble banks, from the plunge pool south along the west side and out through R's
      mouth;
    - the central pool (radius 14, 1.2 m) with **the island**: a stone deck, radius 5, at +1.6, solid, two scree ramps. On it stand the
      Singing Stone (the sigil sockets) and spirit stone 1;
    - the chute pool (radius 8, 1.3 m) under F2's lip on the east;
    - the plunge pool (radius 9, 1.4 m) under the falls lip on the NW.
    - All pools are wading depth.
  - **Three lamp rings** of glowing stones on the floor at the wing mouths:
    - amber under the bridge's east end;
    - red at the ash steps (east wall, z 60);
    - grey at R's mouth (west wall, z 48).
  - **Seven echo-jars:** 2 on the trestle, 1 on the tower, 2 by the river, 1 on the island, 1 by the chute pool.
  - Links:
    - in: W lip (+14, SW); F2 chute lip (+6, E, one-way); B falls lip (+12, NW, one-way); the VC cloister (floor, NE, level join);
    - out: F1 (+12, E, by the bridge); R (floor, W); V1 (floor, E); D (the north neck).

### The Geode Foundry (wing "foundry", L11, the Sandthrone and goblins; built halls, violet light with furnace orange; the amber sigil)
- **F1 The Cutting Drift** (within G at 155, east wall, rise +12).
  - A Company door in the rock at the bridge's east end: 25 m of drift (half-width 3, floor -46).
  - The cutting floor: 50 m, half-width 18, height 9, floor -45. Benches where cutters split geodes with chisel and mallet, sorting
    troughs, ore baskets hung from timber beams.
  - The haul drift: 45 m, half-width 3.5, rising to -38.
  - F1 joins F2's hall at its far end, +12 over F2's floor, where the catwalk starts.
- **F2 The Geode Foundry** (within G at 185, east wall, rise +6: its mouth is the chute lip).
  - The ore chute, F2 0-45: half-width 3.5-4, floor -52 to -50, with the chute door at 46.
  - **The foundry hall, F2 50-135:** built hall, half-width 24-26, height 22, floor -50; walls of 6 courses (12 m) with columns.
    - The catwalk: a timber deck 3 m wide at +12 along the F1-side wall, from F2 135 back to F2 55.
    - The end ramp across the near end, +12 to +6.
    - The walkway at +6 along the opposite wall, F2 55-120.
    - The far ramp, +6 to the floor, F2 120-138.
    - Railings on every open edge. This is the enter-high, ramp-round-the-edge lesson (L3).
    - The centre: the crack-furnace (stone, a glowing mouth); hand forges with small hand bellows down the south wall (one a usable
      ZoneStation forge); the slag pit, a 6 m glowing disc that is a NavBlocker.
  - **The Rock-Eater's berth, F2 140-170:** half-width 12, height 10. The gantry and the racks, and the beast's own bored tunnels in the
    back rock.
  - **Way home:** down the hall floor, through the chute door (opened by Nix's death), then the drop off the chute lip into the hub's
    chute pool (one-way).

### The Cult of Ash's chapter-house (the stone stronghold, red light, L11; the ember sigil)
- **V1 The Ash Steps and the Ash Garden** (within G at 220, east wall, rise 0).
  - The steps: 35 m, half-width 3.5, -58 to -66, to a gatehouse arch.
  - The garden: 60 m, half-width 16, height 10-12, floor -66. Urns, graves, falling ash, 2-course walls.
  - It ends in a tip. VS leaves the north wall; VM leaves the east end.
- **VS The Scriptorium** (wing "scriptorium", group "ventside"; within V1; joins VC's west door).
  - A 20 m drift, then the library: 55 m, half-width 12, height 10, floor -66 to -70. Bookcases, lecterns, reading desks.
  - The bookcase (a hidden door) opens on **VSx, the Censored Room:** a tiny cave within VS, 15 m, half-width 4, floor -68.
- **VM The Muster Yard** (wing "musteryard", group "ventside"; within V1; joins VC's east door).
  - A 15 m drift, then the yard: 60 m, half-width 14, height 10, floor -66 to -70. Weapon racks and ranks of dummies.
  - The railed kennel sits in a side bay at least 8 m off the yard's line.
  - A 70 m drift then runs north to VC.
- **VC The Chapel of Embers and the cloister** (wing "chapel", after group "ventside"; within VS at its end).
  - The chapel: 80 m, half-width 18, height 20, floor -70. Pews, the carpet, the lava lake mid-nave with a way round both sides, the
    altar in the north apse, the great window with the fire behind it.
  - The cloister door (a bossdoor, opened by Ysolt's final death) stands behind the altar.
  - The cloister stair: 130 m, half-width 3.5, climbing -70 to -58. VC joins G level on its NE floor (the loop).
  - From the hub side the cloister door is shut, so no one can walk in the back way.

### The Sunless River and the Grey Breach (natural caves, the Wailing Caverns lesson, then the Wasting's corruption; blue-green, then grey; the grey sigil)
The river was called "the Flooded Gallery" in one draft; it is renamed so it is not confused with the Flooded Working.
- **R The Sunless River** (within G at 210, west wall, rise 0).
  - The river drift, 0-40: half-width 5, height 6, floor -58 to -61.
  - The wet way, 40-120: half-width 7, height 5, floor -62, water 1.2 m from wall to wall. Short.
  - The Lurker's Pool, 125-175: half-width 22, height 14, a 1.3 m pool.
  - The falls stair, 180-230: half-width 4, height 6, -62 to -42 (22 degrees), beside a waterfall (a falling water sheet, mist and a
    glow).
  - The falls head, 235-245: half-width 10, floor -40.
- **R2 The Dry Ledge** (within R at 38; joins R at 122, level).
  - 105 m; floor -61 to -58.
  - The pillared hall: half-width 16, height 10, natural rock pillars from floor to roof. Then down to -62.
  - The braid: the wet way is short, the dry way long. The leader takes the dry way (wading costs 6).
- **B The Grey Breach** (within R at 238; heads back east; joins G at the falls lip, +12 on the NW wall).
  - 230 m, grey static on the walls:
    - 0-60: half-width 4-8, floor -40 to -41;
    - the Hollow drift, 70-110: half-width 12, height 8;
    - the Unmade Gallery, 130-175: half-width 14, height 12, with the patch of floor that is not there;
    - the Foreman's drowned drift, 180-215: half-width 16, height 10, floor -44, water 0.5 m;
    - 220-230: half-width 5, floor -46. The sluice-boards (a bossdoor) at 222, then the lip. The stream runs out over the falls the
      party saw from the overlook.
- **Q The Quiet Miner's Drift** (OPTIONAL; within B at 100).
  - 45 m, half-width 3 opening to 9, floor -42. An old timbered dead end.

### The Deep Way (D: wing "deep", L12; tunnel, then a built station hall; cold blue light)
- **The singing door at D 8:** an old goblin resonance door (vibration-locking is CANON; the door is GAME-ONLY). Song, not gear.
- **D1 The Shaft Stair, D 15-150.**
  - Tunnel, half-width 4.5-5, three landings at half-width 7. Floor -60 to -100 (16 degrees).
  - Timber treads, a rope rail, cold blue lamps.
- **DP The Powder Cubby** (within D at 120, west): a 14 m niche, floor -95. The keg.
- **The platform gate** at about D 155, at the narrowest ring.
- **D2 The Rail Hall, D 160-300.** Built station hall: half-width 40, height 26-28, floor -100. **The second reveal.**
  - The Platform: a stone plinth deck at +1.2, with ramps at both ends.
  - The Gunners' Gallery: a timber deck at +6 along the right wall, with stairs at both ends.
  - The carriage on the line: two plank wagons and two pit mules.
  - Spirit stone 2 stands just inside the gate.
  - The finale stays here until S6 passes.
- **KP The Pitch Pit** (OPTIONAL, hidden; within D at the hall's NW corner, behind stacked sleepers).
  - A crawl, half-width 2.5, -100 to -106, then the pit: 25 m, half-width 10, height 7, floor -108.
  - Pitch kettles over fire pits; barrels of pitch for the sleepers and the axles.
- **S6 only:**
  - **D3 The Dead Line, D 310-470:** rail tunnel, half-width 4.5, height 5.5, -101 to -114, sidings of half-width 9 at two places.
  - **D4 The Black Cove, D 480-610:** natural cave, half-width 45, height 36. Black water 2.6 m deep over a floor at -121 (swim; area 1
    for agents). In it:
    - the quay (a deck 8 m wide along the west wall at water +0.8);
    - the ore barge, 45x10 m: plank and iron-band hull, oars racked, tow-ropes to the bollards. Its deck is at water +4.2, the stern
      castle at +7.5;
    - the scaffold along the hull, tier 1 at quay +2.2 and tier 2 at +3.4 with the gangway; the ramps alternate ends;
    - a scree landing stage, so a swimmer can climb out.

## 4. Camps and patrols
Each camp has its wing and step from the run order: [n] is its step; P is a patrol (step 0, not on the route); "after" is a camp
released later (step 0).

**Rules:**
- One mob type per camp, as the data model has it. Camps of the same faction that stand close answer each other (call, kin).
- **Pinned indices,** so no save migration is needed: Lusk 5, Sorrel 13, Danner 16, Nix 21, the Vent-Hound 25, Ysolt 27, the
  Foreman 31 (their places in today's adit.json, checked 2026-10-14).
  - adit_layout.py writes the array in this order and asserts it.
  - 0-31 are filled as listed at the end of this section. The rest follow; the patrols come just before the end, and the Quiet Miner
    is last.
- Respawn 7200 (Q1).
- "Gang-boss's guards", "Workshop guards" and the other guard camp names are kept, because the `guards` fields name them.

**WORKINGS (wing workings):**
- [1] Adit pickets: 2 Sandthrone pickets, L10 (crossbows).
- P Drift patrol: 2 pickets, W 10-45.
- [2] The bowl's rim: 3 overseers, L10, rise on the overseers' walk. Called down by the floor.
- [3] The cages' guard: 3 diggers, L10, at the bowl's bottom.
- [4] Ore-cart loaders: 3 diggers, L10, on the far slope.
- P Bowl patrol: 2 overseers, the bowl floor end to end.
- [5] Sappers' drift: 3 sappers, L11, in the stair and its alcoves (Short Fuse).
- [6] Spoil heaps: 3 diggers, L11, round the heap.
- [7] Gang-boss's guards: 2 overseers, L11, rise 3.5 on the stage's ramps.
- [7] Gang-Boss Haddo Lusk: elite, rise 3.5. One fight with his guards.
- after kill:Lusk, Lusk's relief shift: 3 overseers, L11. They appear in the bowl and walk in behind you.
- [8] Timber crew: 3 diggers, L11.
- P Sergeants' round: 2 overseers, W 250-300.
- [9] Overlook watch: 3 watchmen, L11.

**FLOODED (wing "flooded", optional, after workings):**
- Bailers' chain: 3 diggers, L11, wading.
- Brack the Bailer: elite 11; guards "Bailers' chain".

**PEN (wing "pen", optional, after workings):**
- Pit-hounds: 2 Company pit-hounds (wolf look; their loot needs a hide, ItemTests), L11.
- Old Bristle: elite 12, boar look, penned.

**LEDGE (wing "ledge", after workings):**
- [1] Landing guard: 3 watchmen, L11, rise 12 on the trestle landing.
- [2] Bridge-tower crossbows: 2 pickets, L11, rise 12 on the tower. You cross within 5 m of them.

**FLOOR (wing "floor", after ledge):**
- [1] Gallery carriers: 3 geode-carriers, L11, floor east. Call reach 30: open-chamber linked pulls.
- [2] Pool watch: 3 watchmen, L11, at the island's ramp foot. Touch stone 1.
- [3] Gallery lamp-men: 2 watchmen, L11, by the plunge pool.
- P Gallery porters: 3 carriers, round the central pool.

**FOUNDRY (wing "foundry", after ledge):**
- [1] The cutting floor: 3 geode-cutters, L11.
- P Floor runners: 2 cutters, from the cutting floor to the haul drift.
- [2] Haul-drift porters: 3 carriers, L11.
- [3] Catwalk sorters: 3 sorters, L11, rise 12. Call reach 40, so a pull brings the walkway.
- [4] Whip-hands: 2 overseers, L11, rise 6 (Vosk's guards).
- [4] Overseer Grael Vosk: elite 11, rise 6.
- [5] Workshop guards: 2 overseers.
- [5] Nix and the Rock-Eater: elite.
- after kill:Nix, Berth night crew: 3 cutters, down the far ramp behind you.
- [6] Smelters at the furnace: 4 Sandthrone smelters, L11 (Slag Splash).
- [7] Chute-door guards: 3 overseers, L11, inside the chute door.
- Exit: drop. Regroup at the anchor "chute pool".

**VENT (wing "vent", after ledge):**
- [1] Gatehouse wardens: 3 Ash zealots, L11.
- [2] Gatehouse menders: 2 Ash menders, L11 (Ember Mend: zealots and menders hold each other up).
- [3] Garden mourners: 3 initiates.
- P Ash procession: 3 initiates, round the graves.
- [4] Urn-tenders: 2 initiates (Sabel's guards).
- [4] Urn-Keeper Sabel: elite 11.
- after loose:Sabel, Urn-risen (first) and Urn-risen (second): 2 "Ash-bound dead" each (skeleton look, L10), at the urn anchors.

**SCRIPTORIUM (wing scriptorium, group ventside, after vent):**
- [1] Copyists: 3 Ash scribes, L11 (Cinder Bolt).
- [2] Lectors: 2 scribes (Senn's guards).
- [2] Loremaster Ivo Senn: elite 11.

**MUSTER YARD (wing musteryard, group ventside, after vent):**
- [1] Zealots in ranks: 4 zealots, L11.
- [2] Armourers: 2 zealots (Corvane's guards).
- [2] Master-at-Arms Corvane: elite 11.
- The Vent-Hound: elite, optional, alone in the kennel, social solitary.

**CHAPEL (wing chapel, after ventside):**
- [1] Chapel faithful: 3 zealots, in the pews.
- [2] Lake-tenders: 3 menders, round the lake (they heal each other).
- [3] Cinder-Warden Ysolt: elite, guards "Sister Ashe Varrow".
- [3] Sister Ashe Varrow: elite 11.
- after kill:Ysolt (the final death only), The cloister bell: 3 zealots in at the chapel's other door, behind you.
- P Cloister walkers: 2 zealots, on the cloister stair.
- Exit: walk. Regroup at the anchor "cloister foot".

**RIVER (wing "river", after ledge):**
- [1] River-mouth crawlers: 3 greyed crawlers, ambush in the river at R's mouth, flee 0.25.
- Under the water: 3 crawlers, an ambush in the wet way, flee 0.25. Optional: the leader takes the dry way.
- [2] The Hollow sump: 3 Hollow Men, R2 (Grey Drain).
- [3] Pillar whisperers: 2 grey whisperers, R2 (Grey Lull), flee 0.2.
- [4] Pool crawlers: 3 crawlers, flee 0.25.
- The Old Burrower: elite 13, passive, optional; it wanders a loop round the pool.
- P Wandering Hollow: 2 Hollow Men, the falls stair.

**BREACH (wing "breach", after river):**
- [1] Breach mouth crawlers: 3, flee 0.25.
- [2] The Hollow drift: 3 Hollow Men.
- [3] Grey den: 2 greyed bears.
- [4] Whisperer's hollow: 2 grey whisperers (her guards).
- [4] The Grey Whisperer: elite 11, required.
- after loose:Whisperer, The Whisperer's flock: 4 crawlers, out of the grey.
- P Wandering grey: 2 crawlers.
- [5] The Foreman's shift: 3 Hollow Men (his guards).
- [5] The Foreman Who Forgot: elite.
- after kill:Foreman, What the grey kept: 2 Hollow Men, out of the walls behind you.
- Exit: drop. Regroup at "plunge pool".

**QUIET (wing "quiet", optional, after breach):**
- The Quiet Miner: elite 12, rare 0.2, last in the array.

**WAKE (wing "wake", after foundry, chapel, breach and floor).** Its camps are template camps, all "after":
- after event:wake1, Wake porters: 4 carriers, from the trestle.
- after event:wake2, Wake zealots: 4 zealots, up the ash steps.
- after event:wake3, Wake crawlers: 5 crawlers, out of R's mouth, flee 0.25.
- after event:wake4, Brother Hollowsong: elite 12.
- after loose:Hollowsong, Hollowsong's choir: 3 Hollow Men.

**DEEP (wing "deep", after wake):**
- [1] Stair guard: 3 platform guards, L12.
- P Stair patrol: 2 watchmen.
- [2] Middle landing: 3 platform guards.
- Powder store: 2 sappers, L12, optional (the loud way needs it).
- after event:powder, Powder-Sergeant Hobb (elite 12) and Hobb's sappers (2). They come down the stair behind you.
- [3] Lower stair guard: 3 platform guards.
- [4] Siding dockers: 4 dockers.
- [5] Carriage gunners: 3, rise 6 on the Gunners' Gallery (Rail-shot).
- [6] Platform guards: 3.
- P Platform round: 2 platform guards.
- [7] Quartermaster's guards: 2.
- [7] Quartermaster Brannigan Sorrel: elite.
- after kill:Sorrel, Sorrel's dockers: 3 dockers, from the line's far end.
- [8] Rail-Captain's guard: 2.
- [8] Rail-Captain Orsk Danner: elite. Hidden while the escort can happen, as now.

**PITCH (wing "pitch", optional, after deep):**
- Pitch-gangers: 2 dockers, L12.
- Old Kettle, the pitch-boiler: elite 13, hidden.

**Counts:**
- about 97 camp entries and about 210 mobs with the releases (today 67 camps and 148 mobs);
- 11 patrols;
- 20 elites: the old 8, plus Brack, Old Bristle, Vosk, Sabel, Senn, Corvane, Varrow, the Grey Whisperer, the Old Burrower,
  Hollowsong, Hobb and Old Kettle;
- required: about 42 trash fights (5 of them releases) and 11 boss fights, plus the Wake.

**The pinned array head (0-31):**
- 0 Adit pickets, 1 The bowl's rim, 2 The cages' guard, 3 Sappers' drift, 4 Gang-boss's guards, 5 LUSK;
- 6 Timber crew, 7 Gallery carriers, 8 Landing guard, 9 Bridge-tower crossbows, 10 Platform guards, 11 Siding dockers,
  12 Quartermaster's guards, 13 SORREL;
- 14 Carriage gunners, 15 Rail-Captain's guard, 16 DANNER;
- 17 The cutting floor, 18 Haul-drift porters, 19 Catwalk sorters, 20 Workshop guards, 21 NIX;
- 22 Gatehouse wardens, 23 Garden mourners, 24 Zealots in ranks, 25 VENT-HOUND;
- 26 Chapel faithful, 27 YSOLT;
- 28 Breach mouth crawlers, 29 The Hollow drift, 30 The Foreman's shift, 31 FOREMAN.

## 5. Bosses and side bosses (what holds the party; the fields are EliteMove's unless marked NEW)
**Required (11 fights):**
1. **Gang-Boss Haddo Lusk.**
   - Floored: stun 2 s every 14 s, as now.
   - On the tally stage, so you fight up the ramps.
   - His death opens Lusk's door and releases his relief shift. His deathCall stays 3, but the release replaces the kin search (see
     `DeathCall` in S3).
2. **Overseer Grael Vosk.**
   - Lash: root 3 s on his target every 12 s (cc root).
   - At 50% a rally, "Drive them!": callReach 50, so any sorters left on the catwalk come down.
   - Fought at +6 on the walkway with the hall below. The room is the fight: clear the catwalk from above first.
3. **Nix and the Rock-Eater** (the duo, unchanged).
   - Hold Nix, kill the beast. Shaken: disorient within 5 m every 20 s. Shriek: fear within 4 m every 25 s.
   - NEW: deathCall 2, which releases the Berth night crew.
4. **Urn-Keeper Sabel.**
   - Ash in the Eyes: disorient 3 s within 6 m, every 18 s.
   - NEW phase kind "loose" at 0.66 and 0.33: Urn-risen (first) and Urn-risen (second) climb out of the urns.
5. **Loremaster Ivo Senn** (if the Scriptorium is taken).
   - callOnPull: the copyists come.
   - Lullaby of Ash: NEW MobCast kind "hold", area 6 m, stun 4 s, a 3 s cast every 18 s. Interrupt it.

   **Or Master-at-Arms Corvane** (if the Muster Yard is taken).
   - callOnPull, "Form up!".
   - Shield-bash: stun 2 s on his target every 12 s.
   - NEW phase "loose" at 0.5, "Loose the hound!", naming camp "The Vent-Hound". If the hound lives it leaves the kennel and joins.
     Kill the optional hound first, or face it mid-fight.
6. **Cinder-Warden Ysolt with Sister Ashe Varrow** (the finale-pair lesson, L7).
   - Ysolt as now: "The fire sees you" (fear 4 s within 6 m every 16 s), Ember Blow's knockback, Flame Ring.
   - Varrow: Ember Mend on Ysolt (MobCast heal); Kindling, ringEvery 14 at reach 4.5.
   - Ysolt's camp names Varrow's camp in `guards`, so they always pull together. This needs a one-line change in
     `SocialAggro.GuardCamps` (see S4).
   - NEW `EliteMove.raisedBy = "Sister Ashe Varrow"`. If Varrow lives when Ysolt falls, Ysolt rises after 5 s at 50%, once.
   - No loot, XP, key, quest count or death call on that first fall. The ember sigil and her loot come only on her final death.
   - Ysolt NEW: deathCall 3, which releases The cloister bell.
7. **The Grey Whisperer** (required).
   - The Long Lull: NEW MobCast "hold", a 3 s cast every 14 s, stun 6 s on the party member nearest her who is not her target. It
     stands for sleep. Interrupt it.
   - At 0.4, "loose" The Whisperer's flock: the crawlers who fled come back out of the grey, more of them.
8. **The Foreman Who Forgot.**
   - Blinks; resets aggro at 50%; terrifies at 25%, as now.
   - NEW: deathCall 2, which releases What the grey kept.
   - His death opens the sluice-boards and, at S7, stops the Grey Spreads.
9. **Brother Hollowsong** (the Wake's boss; a Silent Pilgrim long since unmade, GAME-ONLY).
   - The Unsung: stun 3 s within 8 m every 18 s.
   - Grey Drain (MobCast drain).
   - At 0.5, "loose" Hollowsong's choir.
   - He walks out of the singing door. He does not rise from the pool; that is the Wailing Caverns ending, so it stays a lesson only.
10. **Quartermaster Brannigan Sorrel.**
    - Stamps at 2/3 and 1/3, as now.
    - NEW: deathCall 3, which releases Sorrel's dockers.
11. **Rail-Captain Orsk Danner**, as now.
    - Chained: root 4 s every 14 s. Guards out of the dark on the pull and at 50%.
    - S6 only: +2 per call if the haul reached the quay.

**Optional and hidden** (each drops one named piece every time, 0.075):
- **Brack the Bailer:** knock 4. Bucket Douse: disorient 3 s within 4 m every 16 s. Fought in the wading pool.
- **Old Bristle:** released by lifting the pen bar. Charge knock 5. Trample: disorient 3 s within 5 m every 18 s.
- **The Vent-Hound:** unchanged.
- **The Old Burrower** (the Rock-Eater's look without a saddle):
  - NEW `ZoneCamp.passive`: it ignores you until struck.
  - Rock-Crush: knock 6. Burrow: disorient within 6 m every 16 s.
- **The Quiet Miner:** unchanged (rare).
- **Powder-Sergeant Hobb:** Short Fuse (a new MobCast entry for him). Ambushes the party when it takes the keg.
- **Old Kettle** (the pitch-boiler):
  - Hot Pitch: ringEvery 14, reach 4.5.
  - Stuck in the pitch: root 3 s within 4.5 m every 18 s.
  - A note by the platform, "A pitch-boiler's tally", points to his pit.

**New trash kits** (MobCasts, EncounterEnemy.Casts.cs):
- Sandthrone smelter: Slag Splash, blast, reach 4, power 2.5.
- Ash scribe: Cinder Bolt, bolt.
- Grey whisperer: Grey Lull, hold, stun 4 s on the nearest non-target, every 16 s; interruptible.
- Sister Ashe Varrow: Ember Mend, heal.
- Powder-Sergeant Hobb: Short Fuse, blast.
- (S6) Harbour netter: Weighted Net, hold, root 3 s, area 4 m.

## 6. Events
**E1. The Wake** (required; S4; behind data).
- If `zone.events` holds no "wake", the sigils open the singing door as the winch gate opens today.
- With it:
  1. E at the Singing Stone on the island ("Set the rail sigils (3/3)"). The stone sings for 150 s while the party holds the island.
  2. Waves come from the lamp rings, each ring flaring as its wave comes, with breathing room between (lesson L8):
     - at 15 s, Wake porters down the trestle (you see them coming);
     - at 60 s, Wake zealots up the ash steps;
     - at 105 s, Wake crawlers out of R's mouth.
  3. At 150 s the singing door cracks from inside and Brother Hollowsong walks out of the north neck toward the stone.
  4. His death: the door comes apart, "adit.lift" is kept, the door stays open for good.
- If the whole party is down: the stone goes quiet, and the living waves and Hollowsong go back into the dark (WeaverFell's
  pattern). The sigils stay set; E starts it again.
- Code: EncounterSession.Wake.cs, with the AditWake component ticking time (mirroring AditWeaver).

**E2. Releases** ("after" camps; from S3).
- kill:<mob> releases go through `DeathCall`; event:<id> and loose (a boss phase) releases go through the session.
- A released camp spawns from its own seeded stream, with RespawnSeconds 1e6 (once), and is sent at the foe.

**E3. The Weaver's Lock** (unchanged until S6).
- The quiet platform gate, Mother Quillet, two waves, Danner rising at half matched.

**E4. The powder.**
- Taking the keg releases Hobb and his sappers behind you.
- Blowing the gate still brings Sorrel's wave (`SendWave`).

**E5. S6 only: the Haul.**
- Sorrel's death brings Danner's voice: "Haul."
- 4 haulers (the docker template) and the two mules drag the carriage down the Dead Line at 1.8 m/s. It rolls only while a hauler
  stands within 8 m of the traces.
- Rearguards drop off at the two sidings.
- Kill the haulers and it stops. "Cut the traces" frees the mules and opens the geode-crates cache.
- If it reaches the quay, Danner's calls bring 2 more each.
- Code: the carriage under one root, NavMeshObstacle carving boxes, and the AditHaul component.

**E6. S7: the Grey Spreads.**
- Three lines across B, at 45, 125 and 185. Crossing one dims the Breach's lights behind it to grey, shows hidden static, and releases
  event:spread1-3 (3 crawlers each, 25 m behind, flee 0.25).
- The Foreman's death stops it, and the lights come back a little.
- File EncounterSession.GreySpread.cs. No member named Spread*: use GreyLine, greyLinesCrossed, GreySeep.

## 7. Secrets
`ZoneSecret` gets NEW fields `cave`, `along`, `aside` and `rise`, so each secret stands on its own floor or deck. Ids are
`secret.adit.<slug>`.
1. **shoulder:** a vista and a cache above the cut in the yard. A Silent Pilgrim's bundle with a note: "the stone wants all three
   notes, and then it wants holding".
2. **nail_key:** a key, "A rusted key on a nail", behind fallen timber in the dark drift, where the lichen trail has a gap.
3. **behind_falls:** a chest that needs the rusted key, on a hidden 2x6 m deck behind the waterfall at the plunge pool (+1.4). Wade
   in to reach it.
4. **lamp_niche:** a cache on a deck 0.8 m above the trestle landing, reached only by a jump. The sims never go there.
5. **censored_room:** the bookcase in the Scriptorium (a gate of kind hiddendoor, "Search the shelves"). Behind it, a chest and a
   note: the cult's struck-out history, also the document for The Margins.
6. **grey_key:** the grey key always lies at the Quiet drift's dead end, whether the rare is there or not. It opens the Foreman's
   strongbox (a chest) in his drowned drift; today that box is only dressing.
7. **drowned_purse:** a cache in the Lurker's Pool.
8. **ember_cap:** a herb, a glowing fungus on the cloister stair.
9. **kettle_tally:** a note by the Rail Hall platform pointing to the Pitch Pit.
10. (S6) **scaffold_a/b/c:** small caches on each scaffold tier.

## 8. Gates (all in data: `ZoneDefinition.gates`)
| Gate | Where | Kind | Opened by |
|---|---|---|---|
| The sump bar | G 6 | bar (side far) | lifted from the hub side only |
| Lusk's door | W 214 | bossdoor | Lusk's death |
| The pen gate | P 34 | pen | "Lift the pen bar" (Old Bristle charges) |
| The chute door | F2 46 | bossdoor | Nix's death |
| The bookcase | VS 60 | hiddendoor | "Search the shelves" |
| The cloister door | VC 92 | bossdoor | Ysolt's final death |
| The sluice-boards | B 222 | bossdoor | the Foreman's death |
| The singing door | D 8 | liftgate | the Wake (or the sigils, with no event); kept with adit.lift |
| The platform gate | D 155 | platformgate | quiet or loud, as now |

- Bossdoors and bars shut again on every visit.
- **Layout check 7: no loop or drop gets round a gate.** A grid flood over floors, decks and joins (drops one-way) proves every path
  from the yard to a camp behind a gate crosses that gate.
- `ZoneCamp.gate` (NEW) names the gate the leader waits at for each camp. It is written by the flood.
  - This fixes a real bug in today's `GateAhead` (cave plus along) for the network (0.2, G9). F2's hall camps have along > the chute
    door, but they are reached from F1, so the leader would wait at the shut chute door.

## 9. Run order, optional parts, modes, time
**The leader's default order.** The wing planner picks the nearest available wing by NavMesh path length; the order below is what
that gives.
1. **Workings:** about 11 minutes. Pickets, the bowl, the stair, Lusk and the relief shift, the dark drift, the overlook.
2. **Ledge:** about 2 minutes. The trestle landing, then the bridge tower.
3. **Foundry:** about 11 minutes. Nearest from the bridge. The catwalk, Vosk, the far ramp, Nix, the hall floor, the chute, the drop.
4. **Floor:** about 3 minutes. The nearest after the chute drop. Touch stone 1.
5. **Chapter-house:** about 15 minutes. The garden, Sabel, then one fork side (steered by The Margins), the chapel (Ysolt and Varrow),
   the cloister home. **River and Breach:** about 13 minutes. R2's dry way, the pool, the falls stair, the Whisperer, the Foreman, the
   falls drop home. Whichever is nearer goes first.
6. **The Wake:** about 5 minutes.
7. **Deep:** about 13-16 minutes. The stair, the platform gate, the Rail Hall, Sorrel, Danner, the Weaver if the way was quiet, the
   ride out.
**Total required: about 68 minutes.** Walking is about 2.2 km with no retracing.

**Optional, about 20 minutes more:**
- the Flooded Working and Brack;
- the Pen and Old Bristle;
- the other fork side and its boss;
- the Vent-Hound;
- the wet-way ambush;
- the Old Burrower;
- the Quiet Miner's drift;
- the powder cubby (needed for the loud way);
- the Pitch Pit and Old Kettle;
- the secrets;
- the sump bar shortcut home.

**Leader modes (S7).** The dungeon command is "/dungeon" (ZoneChat.cs:160, EncounterSession.Party.cs:207); "/lead" at :159 is the
general `Lead(arg)`, so one draft had it wrong.
- "/dungeon quick": required only.
- "/dungeon": required, plus the optional camps inside wings it takes (the Vent-Hound if the Yard is taken, the Old Burrower).
- "/dungeon full": every wing, both fork sides, all optional camps.

**Keeping a lost party found** (L9):
- every wing ends in a way home onto new ground;
- the lamp rings mark the wing mouths;
- a living lichen trail runs along each wing's required steps (`ZoneCrumb`, written from the step order);
- two spirit stones;
- the leader regroups at every landing.

## 10. Quests, loot, save
**Quests/adit.json** (the targets keep their tags; text only unless marked):
- side.adit.pressed: "the five goblin cages at the bottom of the Cage Bowl in the Old Workings".
- side.adit.echoes: "six of the seven echo-jars round the Singing Gallery: its floor, its trestle, its bridge tower and its island".
- side.adit.embers: "in the Chapel of Embers at the heart of the Cult of Ash's chapter-house; mind the priestess".
- side.adit.dead_line: "past the singing door, which wakes only when the Gallery is held". (S6: "on his ore barge in the Black
  Cove".)
- side.adit.toll, grey, weaver, letter: unchanged until S6.
- **NEW side.adit.margins, "The Margins":**
  - Tamsin Rook, Khaven, after Embers Below, level 11, minLevel 10.
  - A kill of mob.loremaster.adit.*, which drops Senn's daybook, then deliver it to Tamsin. The Censored Room's note is its document.
  - The planner prefers a group wing whose elite an active quest's kill target matches (`QuestLog.Matches`).
  - QuestDataTests' list grows to 9.

**Loot** (tools/wip/dungeon/adit_loot.py, writing loot.adit.json):
- signature lists for the 12 new elites (one boss-unique piece each, every time; Kettle's is level 13);
- hides for the pit-hound and Bristle tables (ItemTests);
- the geode-crates cache (S6);
- LootDataTests' elite count goes from 20 to 32.

**Save** (no format change, so no migration):
- The pinned indices keep elitesSlain "mob.<tag>.adit.<i>".
- These keys are kept: sigil.amber/ember/grey, adit.lift, adit.lock.
- Spirit-stone and cage keys are by position, so the old ones go stale (harmless). Chris touches the stones again, and every cage
  shows open once The Pressed is done.
- A saved position in zone.adit with no cave floor or deck within 4 m loads at the yard spawn.
- All of this goes in SAVE_FORMAT.md. Back up Chris's save at S0 and before S5.

## 11. Builder, leader and data changes, in build order
**Every stage:**
- a Python bracket and quote balance check on each changed .cs;
- every name grepped;
- new .cs files and folders get .meta files with fresh GUIDs (uuid4 hex);
- EditMode, then PlayMode, in lane A (one Unity job at a time);
- a commit on adit-rework;
- tools\Backup.ps1;
- front, back and side frames to Chris;
- status OPEN until he confirms in his build.

**Sync caution:** tools\validation\run_tests.ps1:4 and release_build.ps1:10 mirror D:\code\mmo\...\Assets, the main checkout. Add a
-Source parameter, or mirror from D:\code\mmo-adit by hand. Otherwise lanes A and B test the wrong tree.

### S0. Docs and decisions
- DUNGEON_DESIGN.md:
  - section 0 points here with Chris's line verbatim (done 2026-10-14, with this file): "i think adit dungeon need to be more complex
    like wow's wailing caves or the deadmine. adit is nothing like those";
  - once Chris approves: a section 0.26 with his line and his answers to Q1-Q4, this plan as 0.27, and section 4's diagram pointed to
    it.
- Ask Q1-Q4 short (section 1) and record the answers verbatim.
- Back up Chris's save to D:\crulanda-work\save-backups.
- adit_layout.py v2, geometry only:
  - the caves of section 2 with within, rise, joins, style;
  - a join solver: the end rows are laid inside the target's air at the target's floor plus the lip, no 0.3 m taper on a joining
    cave;
  - checks 1-8 (S3 lists them).
  - It draws Docs/art/adit-plan.png (coloured by floor height, decks, drops as arrows, run-order numbers) and adit-levels.png (side
    elevations).
  - Chris approves the picture before S3 content.

### S1. The engine, additive only
adit.json is untouched, so Crowsfoot Hollow, the Root-Mother's Deep and today's Adit play and test exactly as before.
- World/ZoneDefinition.cs ([Serializable], every array `= new X[0]`, JsonUtility):
  - ZoneProp: + `public float rise; public string joins, style;`
  - ZoneCamp: + `public string wing, after, gate; public int step; public bool optional, passive; public float rise;`
  - ZoneSecret: + `public string cave; public float along, aside, rise;`
  - ZoneDefinition: + `decks, drops, caveWater, gates, anchors, wings, events, crumbs`.
  - New classes:
    - ZoneDeck { name, cave, style (timber|stone|rope|scree), rails (both|left|right|none), wall (-1|0|1), from, to, aside, aside2,
      rise, rise2, width = 3, solid, canonStatus };
    - ZoneDrop { name, fromCave, from, fromAside, fromRise, toCave, to, toAside, toRise, width = 2 };
    - ZoneCaveWater { name, cave, from, to, aside, half, radius, depth };
    - ZoneGateDef { title, cave, along, kind, prompt, lord, opensOn, look, side };
    - ZoneAnchor { name, cave, along, aside, rise };
    - ZoneWing { id, name, after, group, exit (drop|walk), home (anchor), optional };
    - ZoneEvent { id, kind, wing, step, anchor, waves: ZoneWave[] { at, camp } };
    - ZoneCrumb { cave, from, to, aside };
    - plain ZoneDeckSpot (not serialized), like ZoneSecretSpot.
- World/Hollow.cs:
  - + `public Hollow Join; public float JoinAlong; public string Style;`
  - `bool[] floored`, `public bool Floored(int i)`, and `public void ComputeFloored()`: floored[i] = !OpenInOther(this, Centre[i] +
    up*.3f, .8f), the same test as the floor mesh at ZoneBuilder.cs:4574.
  - FloorAt and FloorSmooth return false when the nearest ring is not floored.
  - `public static void FloorsUnder(Vector2 p, List<float> into)`.
  - A 16 m grid of ring indices behind Nearest: search the 3x3 cells first and fall back to the full scan when the best found is over
    16 m, so it gives the same answers as now. It is used by Open and OpenInOther, which run per face per cave.
- ZoneBuilder.cs:
  - PrepareHollows (:4319): mouthY = parent floor + p.rise. After the loop, resolve joins by name, set Style, call ComputeFloored on
    all.
  - Cavern (:4374): `linked` (:4411) also when `o.Join == h || h.Join == o`. Skip the knoll's tip rows and faces (:4465-4476) when
    h.Join != null.
  - StandAt (:237): candidates are the land, FloorsUnder, and deck tops under p; return the nearest to `near`. With near = -inf, the
    first cave floor (today's rule; decks only for a finite near).
  - `public static string ZoneJsonOverride`, read at :73 when set (used by fixture tests and S3).
- NEW World/ZoneBuilder.Decks.cs:
  - planks, flags or rope by style;
  - the walkable slab: a cube Part with its renderer off, plus BoxCollider and NavWalkable (the RopeBridge pattern,
    ZoneBuilder.Verdant.cs:325-330); a solid plinth under 2.2 m;
  - a wall-hugging deck takes its aside from the ring's wall at that height (as WallLamp does);
  - posts as Block (NavBlocker) ending 0.15 m under the slab;
  - rails as Block boxes 1.6 m tall on open edges, gaps only at drop starts;
  - `public readonly List<ZoneDeckSpot> DeckSpots`, `public bool DeckTop(Vector2 p, float near, out float y)`;
  - cave dressing inside a deck's volume removed (the linked-dressing pass).
  - Drops: `public readonly List<(Vector3 from, Vector3 to, float width)> Drops`, with lip rubble.
  - `public readonly Dictionary<string, Vector3> Anchors`.
  - `public bool FloorsAt(Vector2 p, List<float> into)`: caves plus decks.
- NEW World/ZoneBuilder.CaveWater.cs:
  - flat surface meshes (WaterMaterial(), ZoneBuilder.cs:883) and rim rocks; `CaveWaters`;
  - `public bool WaterAt(Vector3 p, out float surface, out float depth)`: cave water first, else the Vector2 version (:878).
  - AdventurerMotor.cs:68 calls it with `here`. Grep the WaterAt( callers: a Vector3 caller now gets cave water too.
- World/ZoneGate.cs: + `public string Kind, OpensOn; public bool FromFar; public Vector3 Slide;` (the bookcase slides rather than
  rising).
- ZoneBuilder.Adit.cs: Gate() (:791) can be driven by ZoneGateDef entries for its cave. The hard-coded calls at :714 and :775 stay
  until S2.
- Encounter/EncounterNavigation.cs:
  - `public const int DropArea = 4;` (next to ShallowsArea, :93).
  - After AddNavMeshData (:27): for each zone drop, NavMesh.AddLink(new NavMeshLinkData { startPosition, endPosition, width,
    bidirectional = false, area = DropArea, agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID }). Keep the instances;
    NavMesh.RemoveLink each in OnDestroy (:132).
  - Cave water: ModifierBox sources, ShallowsArea below 1.45 m and area 1 at swim depth.
- Encounter/EncounterSession.cs SpawnCamps (:1543):
  - `if (!string.IsNullOrEmpty(camp.after)) continue;` before any rng draw, so other zones are unchanged.
  - Camps with rise > 0 stand at StandAt(spot, cave.At(along, aside).y + rise, 1), patrol roads likewise. Others keep -inf.
  - After AddAgent: `a.GetComponent<NavMeshAgent>().areaMask &= ~(1 << EncounterNavigation.DropArea);`
  - NOT in AddAgent (:1650), which sims, wolves and Mira share (:92/172/1453/1571, Party.cs:63).
- Tests:
  - NEW EditMode HollowGridTests: grid Nearest equals brute force on 2000 random points; Floored on a two-cave fixture.
  - NEW PlayMode AditCaveBuildTests, on an inline fixture JSON via ZoneJsonOverride: a hub, a child at rise +10, a +12 joining lip, a
    level join, a deck, a drop, a pool. Asserts:
    - each mouth or join is a hole (a ray down its axis meets no shell);
    - FloorAt under the lip is the hub's floor;
    - StandAt picks the deck or the floor by near;
    - CalculatePath lip to pool is complete, pool to lip is not, and a masked mob agent gets no path down;
    - WaterAt depth 0.8-1.2 mid-pool;
    - a shut bossdoor cuts the path.
  - Every existing test green.

### S2. Port today's Adit onto the data path, no visible change
- adit.json gains:
  - style per prop (main "", geode, vent, breach);
  - gates: the winch gate and the platform gate at today's computed along values;
  - anchors for every Widest window (gallery, hall, cages, boss, lake, foreman, cut, shop, nix).
- ZoneBuilder.Adit.cs:
  - switch(h.Name) (:404-410, :534) becomes switch(h.Style), with the names as fallback;
  - Widest(...X) (:578, :614, :648, GeodeFloor, EmberVent, GreyBreach) becomes Anchor(name) when present, else Widest;
  - gates from data when present.
- No AditCtx refactor: the closures stay inside AditInterior and the file grows.
- Lane B before/after captures match. AditTests, AditGateTests, AditNavTests, RockEaterTests and AditWeaverTests pass unchanged.

### S3. V2 in parallel, never live: the vertical slice
The slice:
- the Workings (W1-W6, FW with Brack, P with Bristle);
- the hub at three heights (trestle, bridge, tower, floor pools, island);
- the Foundry with the chute drop;
- today's shaft stair and Rail Hall finale (Danner and the Weaver as now).
- The singing door is a liftgate opened by the sigils (1/1: Nix's).

The work:
- **The dev path:**
  - NEW Assets/Crulanda/Resources/Staged/ (folder .meta) holding adit-v2.json (+ .meta).
  - `ZoneBuilder.AditV2` (static), set by `--crulanda-adit-v2` (parsed beside --crulanda-zone in EncounterCapture.cs:18) or by
    tests. When the zone built is zone.adit, ZoneJsonOverride is set to Resources.Load<TextAsset>("Staged/adit-v2").text.
  - Same id, so mob ids, loot, quests, deeds and pinned keys match.
  - Nothing goes under Zones/, because the zone-wide EditMode tests (loot, bounties, deeds, nodes) iterate that folder.
  - group_run.ps1 gets `-AditV2`.
- **adit_layout.py v2 writes the staged JSON:**
  - decks, drops, water, gates, anchors, wings, crumbs, secrets;
  - camps with wing, step, rise, optional, after, gate, passive, in the pinned order;
  - respawn 7200 if Q1 is yes.

  Its checks (exit on failure):
  1. A child's first floored ring lies beyond its parent's half-width; its earlier rings are in the parent's air.
  2. A join's end lies in its target's air at the planned lip.
  3. No floor over a floor except at mouths, joins and decks; the old guard holds everywhere else.
  4. Deck ends meet a floor or another deck within 0.3 m, except at drop starts.
  5. Every camp circle lies on its floor or deck, not under a lip.
  6. Slopes: tubes at most 23 degrees (water and decks exempt); decks at most 20.
  7. The gate flood (section 8).
  8. Extents within ±395.
- **Releases:** NEW Encounter/EncounterSession.Release.cs:
  - factor the per-camp body of SpawnCamps into `SpawnCampMobs(int c, System.Random rng, string zoneShort)` (mirror it exactly);
  - `int ReleaseAfter(string key, Actor foe)`: spawns camps whose after == key from `new System.Random(seed*31 + 5 + 7919*(c+1))`,
    RespawnSeconds 1e6, threat on foe;
  - EncounterEnemy.Boss.cs DeathCall (:149) calls session.ReleaseAfter("kill:" + MobName, foe) first and skips the kin search when
    it released any.
  - EncounterSession.cs EnemyDied (:2023) opens ZoneGates whose OpensOn == MobName.
  - GateAhead and GateBefore (EncounterSession.Adit.cs:245-258) return the gate named by camp.gate when it is set ("none" means
    none), else the old rule.
- **The leader** (SimCompanion.cs):
  - route (:302) becomes `List<RouteStep>`, with RouteStep { at, name, wing, ev, boss, gate, camp } (grep every route[ use,
    :297/:311-330/:333-382/:465-479).
  - LeadDungeon (:333) uses the wing planner when zone.wings is not empty, else Gather (:355) as today.
  - The planner:
    - available = wings not done whose `after` ids are all done (a group id counts as done when any wing in it is);
    - skip optional wings, and a group already satisfied;
    - pick the minimum NavMesh.CalculatePath corner length (AllAreas, so drops count) from the leader to the wing's first step;
    - prefer a wing whose elite a live quest wants;
    - a wing's steps are its camps with step > 0 in step order (no patrols, after camps or optional camps), skipping camps already
      cleared;
    - then a "regroup" step at the wing's home anchor, which waits until PartyBehind() < 12.
  - CampCleared (:383) also needs |dy| < 5, so a deck camp is not held up by mobs 12 m below.
  - CampsLeft = steps left plus the required camps of wings not yet done.
  - RunCapture (:34+) logs "wing W: camp i/N".
- **Maps:** NEW World/ZoneBuilder.CaveMap.cs:
  - `public Texture2D CaveMap(float y)`, cached per 4 m band;
  - floored rings painted as discs of Half radius, decks as rectangles, water blue;
  - bright when within 8 m of y, dim otherwise.
  - HudMaps (:176 minimap, :243 zone map) draws it instead of MapTexture when zone.Zone.dungeon and caves exist.
- **Tests:** NEW PlayMode AditV2Tests (AditV2 = true):
  - every camp where it stands, deck camps within 0.3 m of their decks;
  - every camp walkable from the yard with gates open;
  - drops one-way;
  - the planner's first wings are workings, ledge, foundry;
  - releases spawn on Lusk's and Nix's deaths;
  - the cave map is bright on the hub's floor band.
- **The gate:** the slice finishes a full group run on Chris's screen (group_run.ps1 -AditV2). No stall; wing lines in the log.

### S4. The other two wings and the Wake into v2
- Layout:
  - V1, VS, VM, VC (the fork rejoining, the cloister loop, the kennel, the bookcase);
  - R, R2, B (the braid, the pool, the falls stair, the falls drop), Q, the grey key and the strongbox;
  - the sigils become 3.
- Code:
  - EliteMoves.cs: Vosk, Sabel, Senn, Corvane, Varrow, the Whisperer, Hollowsong, Hobb, Brack, Bristle, the Burrower, Kettle;
    deathCall and deathText on Nix, Ysolt, the Foreman and Sorrel; `public string raisedBy`.
  - EncounterEnemy.Boss.cs: BossPhase + `public string camp`; RunPhase case "loose": session.Loose(this, p.camp, Victim). This
    releases an after camp, or makes a live idle camp's mobs Answer.
  - EncounterEnemy.Casts.cs: MobCast + `public string cc; public float seconds; public bool area;`; kind "hold" lands through
    HoldPlayer and SimCompanion.HoldUntil, as HoldParty does (:30); the new entries.
  - EncounterEnemy.cs:
    - OnDeath (:383) calls `if (session.RaiseOnce(this)) return;` before DeathCall;
    - `public void RiseAgain(float fraction)`, mirroring Respawn's revive without the warp;
    - Passive: skip the proximity notice (:289) until struck.
  - SocialAggro.GuardCamps (:114): `camp.elite && names == null`, so an elite counts as a guard only when named.
  - EncounterSession.Adit.cs:
    - the pen gate, the hiddendoor, the sump bar (interactable on the far side);
    - TouchStone moves the key to the end of the list; BoundStone returns the last touched;
    - the keg releases event:powder;
    - TakeKey's text points at the Singing Stone when a wake event exists.
  - NEW Encounter/EncounterSession.Wake.cs and Encounter/AditWake.cs (E1). The "singingstone" interactable; UseLiftGate unchanged when
    there is no event.
  - The planner gets an event step (ev "wake"): wait at the stone ("set the sigils on the stone?"); RunCapture with
    `--crulanda-run-events` uses it.
- Tests:
  - NEW AditEventTests: the three waves and Hollowsong; a failed wake resets with the sigils kept.
  - NEW AditBossTests:
    - Ysolt rises once at half when Varrow lives; key and loot only on her final death;
    - Corvane's loose with the hound alive and with it dead;
    - the Whisperer's flock at 40%;
    - Sabel's urn-risen;
    - the releases on death;
    - an interrupted Lull holds nobody.

### S5. Swap live (one commit)
- adit_layout.py --live writes Zones/adit.json (= v2) and peaks.json. The staged JSON and its meta are deleted; the flag becomes
  inert.
- Data: quest texts plus side.adit.margins; adit_loot.py run.
- Tests in the same commit:
  - AditTests: the caves and parents/joins, mouths and joins open, CampsLeft = required, first "Adit pickets", mobs at least 180,
    patrol mobs at least 22.
  - AditGateTests: the Wake opens the door for good; bossdoors; the sump bar from the hub side only; powder; quiet gate; cages; stones
    by anchor; the rare one visit in five; first camp past the door is "Stair guard".
  - AditNavTests: pockets including decks; deck slabs excluded from "no walkable mesh on solids"; drops one-way.
  - RockEaterTests, AditWeaverTests, ZoneContentTests, QuestDataTests (9).
  - LootDataTests (32 elites), ItemTests (hides).
  - The landmark reach range 6-18 m (AchievementTests:32).
- SAVE_FORMAT.md notes; the yard fallback for stale positions; cages open once The Pressed is done.
- Save backed up. A lane B group run with --crulanda-run-events, timed against 60-75 minutes, with the scorecard; frames to Chris.

### S6. The Deadmines end-piece (last, cuttable; only if Q3 is yes)
Built 2026-10-15 on branch `adit-cove` (Q3 "Build the Cove"): section 20 says what was made and what Unity must run.
- D grows the Dead Line and the Black Cove (quay, scaffold, barge, stern castle decks; swim water).
- NEW EncounterSession.Haul.cs and AditHaul.cs (E5); netters' Weighted Net; bargemen.
- Danner stays in the Rail Hall and keeps rising in the Weaver escort until the Cove's own group run passes. Only then:
  - his camp moves to the stern castle;
  - the halfway rise is removed;
  - RideCarriage needs `keys.Contains(WeaverLock) || Danner dead`;
  - the dead_line and weaver quest text changes.
- Old Kettle stays in the Pitch Pit.
- NEW AditCoveTests.

### S7. Polish
- The Grey Spreads (EncounterSession.GreySpread.cs).
- SwingLoad buckets only if Q4 is yes.
- The /dungeon quick|full modes (ZoneChat.cs:160 passes arg; LeadDungeon(string mode)).
- Optional-boss tuning, the secrets' rewards, the lichen crumbs.
- Docs: DUNGEON_DESIGN 4-9, WORLD_ZONES, CHANGELOG, CLAUDE_HANDOFF.

## 12. What this merge fixes
Two drafts (A and B) were merged: B's skeleton with A's grafts.

**B's faults, and the fix for each:**
- The file collision: the event is GreySpread.cs, and no Spread* names are used.
- Machines: the wheels and great bellows are cut, the buckets are put to Chris (Q4), and the Drill Yard is renamed.
- The cook: Old Kettle is a pitch-boiler in a pit, not on a raft. Hollowsong walks out of the door; he does not rise from the pool.
- Extents: about 3.2 km is laid out in two dimensions and checked within ±395.
- The dead-end spurs: the fork rejoins.
- Scope: the Haul and the Cove are S6 only, and there is no AditCtx refactor.
- The one-step finale move: Danner stays until the Cove passes.
- Red tests during the rewrite: v2 is built behind a flag and swapped with its tests in one commit.

**A's faults, and the fix for each:**
- No Sump or Well stacking, and no floor built in cells.
- The areaMask is set in SpawnCamps, not AddAgent.
- No ranged-only Winding.
- No save migration.
- The Cove is kept, as Chris's question Q3.
- Chris's question is answered first.

**Also caught here:**
- GateAhead's along rule fails in the network (ZoneCamp.gate fixes it).
- The command is /dungeon, not /lead.
- The validation sync scripts point at D:\code\mmo.
- GuardCamps ignores elite camps, so the priestess pair needs the named-elite tweak.

**Machines, final check (0.05).** Everything is timber, stone, rope or hand-worked: a rope bridge, a trestle, stagings, a hand-hauled
carriage and mules, hand bellows, buckets passed hand to hand, powder lit by hand, a song door, and a rope-hauled barge (S6). There is
no cannon, crane, pulley, wheel, engine or lift anywhere.

## 13. Lore labels
| Thing | Label | Source |
|---|---|---|
| The old workings under the Shattered Peaks and the deep line south; stagnant water, rusted iron ladders, green lum-tubes | CANON | book2 ch.19 |
| The Sandthrone (mercenaries holding the Peaks) | CANON | book1 ch.5 |
| The Cult of Ash | CANON | world_bible |
| Goblins, the Weavers of the Warrens, and vibration-locking | CANON | world_bible |
| The Hollow Men | CANON | book1 ch.4 |
| The Wasting and what it unmakes (grey static) | CANON | world_bible, book2 ch.18 |
| The Silent Pilgrims and their echo-jars | CANON | world_bible |
| The Sealed Adit's mouth on the Peaks; the Listening Shrine above the Gallery | CANON-EXPANDED | WORLD_ZONES, DUNGEON_DESIGN 4 |
| Every new place (the Cage Bowl, the Tally Hall, the Flooded Working, the Pen, the Singing Gallery's trestle, bridge, tower and island, the Geode Foundry, the chapter-house's garden, Scriptorium, Muster Yard, Chapel of Embers and cloister, the Sunless River, the Dry Ledge, the Quiet Miner's drift, the Deep Way, the Powder Cubby, the Pitch Pit, the Dead Line, the Black Cove) | GAME-ONLY | this doc |
| Every person, boss, camp and mob named here (Brack, Old Bristle, Vosk, Sabel, Senn, Corvane, Varrow, the Grey Whisperer, the Old Burrower, Brother Hollowsong, Hobb, Old Kettle, Tamsin Rook, and the rest), every item, quest, secret and note | GAME-ONLY | this doc |
| Every mechanic (the Wake, the singing door, releases, the raise once, the Grey Spreads, the Haul) | GAME-ONLY | this doc |

## 14. As built: the layout and its content (build step 2, 2026-10-14; WRITTEN, not run in Unity, OPEN)
Made by `tools/wip/dungeon/adit_layout.py` (version 2; `--live` writes `EncounterContent/Zones/adit.json`, the Peaks' way in unchanged;
it draws `Docs/art/adit-plan.png` and `adit-levels.png`). Section 2's table was approximate; these are the numbers built.
- **18 caves, about 2.86 km** (today 1.4 km). The Old Workings (`The Sealed Adit`, style workings) end in a lip 14 m up the Singing
  Gallery's south-west wall (the overlook, with a timber balcony); the Gallery (gallery: 80 m across, 30 high, floor 58 m down) is
  entered too by its sump drift off the dark drift. Off the Gallery: the Cutting Drift (12 m up the east wall at the rope bridge's end;
  it ends 12 m up the Geode Foundry's far wall), the Geode Foundry (foundry: its mouth the chute lip 6 m up; a hall 46 m across and 24
  high), the Ash Garden (ashgarden), the Sunless River (river) and the Deep Way (deep: the singing door, the shaft stair, the platform
  gate, the Rail Hall running west 100 m down). The Scriptorium and the Muster Yard fork from the garden; the Chapel of Embers runs west
  from the Scriptorium's end (the Yard joins it at its north-east door), and its cloister comes home onto the Gallery's north-east floor.
  The Dry Ledge loops round the river's wet way; the Grey Breach runs from the falls head back east to a lip 12 m up the Gallery's
  north-west wall. Side rooms: the Flooded Working (a loop off the Cage Bowl into Lusk's Tally Hall), the Pen, the Kennel, the Censored
  Room, the Quiet Miner's Drift, the Pitch Pit. The Dead Line and the Black Cove (S6) are not built.
- **Decks:** the overseers' walk round the Cage Bowl, Lusk's tally stage and its two ramps, the overlook balcony, the Miners' Trestle
  (+14, the landing at +12, down to the floor), the rope bridge at +12 and its tower, the island and two causeways, the haul drift's
  landing, the Foundry's catwalk (+12), cross-walk, north ramp, walkway (+6) and far ramp. Decks carrying camps stand free of the walls
  (explicit asides inside the rock's narrowest bulge), so a camp's deck is where the data says.
- **Drops:** the chute (Foundry to the chute pool) and the falls (the Breach's lip to the plunge pool). **Water:** the bailing pool,
  the central, chute and plunge pools, the Gallery's river, the river drift's stream, the wet way, the Lurker's Pool, the drowned drift.
- **Gates (9):** the sump bar, Lusk's door, the pen gate, the chute door, the bookcase, the cloister door, the sluice-boards, the singing
  door (`ZoneBuilder.AditLiftGate`, renamed from the winch gate; look song, no windlass) and the platform gate.
- **Camps: 97** (the head 0-31 pinned as section 4; 12 patrols counting the Old Burrower's loop; 15 released later; 11 optional; 20
  elites); **228 mobs, 185 there from the start.** Respawn stays 900 s until Chris answers Q1.
- **Anchors** for the dressing and the run (cages, boss, gallery, spirit stone, island, the lamp rings, chute pool, plunge pool, cloister
  foot, furnace, slag pit, berth, lake, altar, foreman, hall, rail spirit stone and more), **16 wings**, **the Wake** (four waves),
  **5 lichen trails**, **11 secrets** (with three new pages in Quests/adit.json), **15 places** on the map.
- **Dressing** (ZoneBuilder.Adit.cs): the main way's dressing split by style (workings / gallery / deep); a foundry hall of six courses
  with the catwalks along its walls, the crack-furnace, the slag pit and the berth; the chapter-house's rooms (garden, scriptorium, muster
  yard, chapel round its lake with the window over the cloister door); the river's blue-green lamps; the Gallery's three lamp rings; a
  second spirit stone in the Rail Hall.
- **Checked:** the generator's checks (mouths on floors, joins' lips, no rock in another cave's way, slopes, camps on their floors or
  decks, deck ends, extents) and the game's own Hollow.cs run outside Unity on the zone (every camp, gate, anchor, secret and patrol point
  on its floor; floors give way only at mouths and joins). All 13 assemblies compile.
- **Left for the next steps** (the camps and their data are ready for them): the session (released camps, deck camps at their rise,
  drop masks, bosses' doors opened by their deaths, the bar, the pen and the bookcase worked), the leader (the wing planner, `camp.gate`),
  the twelve new elites' moves and signature loot, the Wake, The Margins, the cave map. Until then some Adit tests stay red: the elites'
  moves and loot (SocialRulesTests, EliteBalanceTests, LootDataTests), Ysolt's call reaching Varrow, and the runs a sim leads past
  Lusk's door (AditGateTests).

## 15. As built: the run (build step 3, 2026-10-14; WRITTEN, not run in Unity, OPEN)
How a sim leads the party through the web (`/dungeon`; the group run). GAME-ONLY; the lessons are the Wailing Caverns' web and way home
and the Deadmines' doors (section 0.1, L1-L5), nothing copied.

**The order** (`Scripts/Encounter/DungeonPlan.cs`, new; `SimCompanion.LeadDungeon`):
- Wing by wing. Of the wings now open (their `after` done; a group such as `ventside` counts as done when one side is), the leader takes
  the nearest by the walk there on the navmesh (drops taken down, gates as they stand), each metre up or down counting as three more:
  the run keeps to the height it is at (enter high, work down). So from the bridge's tower it goes through the Company door into the
  Geode Foundry before it goes down the trestle.
- Each wing's fights in its own step order; the camps of one step are one fight (a boss and his guards), led by the boss. No wing is
  walked twice: once left, a wing is not gone back to.
- A regroup at a wing's home (its `home` anchor) after its last fight when its way out is a drop (the chute pool, the plunge pool) or
  the home lies beyond its last fight (the overlook, the cloister foot); none back toward a wing's first fight (the trestle landing). The
  regroup waits until the party is within 12 m. It waits at the wing's own exit door if that is still shut (the chute door, the cloister
  door, the sluice-boards). A regroup after a wing the leader never fought through is passed by.
- One side of the fork (Scriptorium or Muster Yard), the nearer, or the side a live quest wants (The Margins, when it is written).
- **Modes** (`/dungeon quick`, `/dungeon`, `/dungeon full`; RunCapture `--crulanda-run-mode`, group_run.ps1 `-Mode`): quick takes what
  must be done; the standard run adds the optional elites inside the wings it takes (the Old Burrower round his pool, followed as he
  walks; the Vent-Hound when the Yard is taken); full adds every optional camp and wing (the Flooded Working and Brack, the Pen and Old
  Bristle, the wet way, the powder store, the Pitch Pit and Old Kettle, the Quiet Miner when he is there) and both sides of the fork,
  the second straight after the first. Each side fight is fitted in where it adds the least walking, so a side room is cleared as the
  run passes its mouth (Brack between the stair and the spoil heaps, Old Bristle after Lusk, the powder store on the stair).
- Camps that are down, rare camps not there this visit, and camps released later are not steps. Patrols come to the party.
- Without wings in its data a dungeon is led as before (Crowsfoot Hollow).

**The leader on the way:** a shut gate before a step is waited at on the leader's own side of it, and it says what the gate wants (the
singing door's sigils; a boss's door "opens when Gang-Boss Haddo Lusk falls"). The gate is the camp's own (`ZoneCamp.gate`), so the
Foundry hall's camps, reached from its far end, never wait at the chute door. A side room behind a gate that stays shut (the pen, until
its bar can be lifted) is left after 45 s. A camp up on a deck is cleared when nothing is left on its own deck (a mob 12 m below does not
hold it up, nor is it pulled from there). Nothing is pulled while the party regroups.

**The session hooks the run needs** (`EncounterSession`): a camp released later (`after`) is not spawned at the start (released by the
later step's code); a camp with a rise stands on its deck (`EncounterSession.DeckHeight`); a camp's mobs keep off the one-way drops; a
boss's death opens the doors that name him (`OpensOn`: Lusk's door, the chute door, the cloister door, the sluice-boards); `GateAhead`
and `GateBefore` use the camp's own gate.

**The group run** (`RunCapture`): the gates the run's quests open (the singing door, the platform gate) stand open, and for the full run
the pen and the bookcase; the bosses' doors are left to the bosses. The log has the run's wings in order, a line as each wing begins,
each camp's place in its wing ("wing The Geode Foundry: camp 3/7"), each regroup and each wait at a gate. A gate still shut after a
minute's wait is opened for the run and the log says so (a boss's death should have opened it).

**Tests (written, not yet run in Unity):** EditMode `DungeonPlanTests` (new: the order, the fork, the quest's side, the height, the
modes and where side fights go, the regroups, what is down) and `QuestDataTests.The_Adit_run_passes_every_quest_boss_and_its_secrets_give_written_pages`;
PlayMode `AditTests.A_sim_leads_the_whole_adit_wing_by_wing` (replacing `A_sim_leads_the_whole_adit_in_order`) and
`Every_camp_is_spawned_where_it_stands` (released camps absent, deck camps on their decks), `AditGateTests.A_bosss_death_opens_his_door_and_a_run_waits_at_one_still_shut`
(new) and `The_gates_are_shut_and_a_sim_run_stops_at_the_lift` (updated), `AditNavTests.The_web_joins_up_and_its_ways_home_run_one_way`
(new: every join walked or dropped, the chute and the falls one way and never taken by mobs, the cloister stair the short way home),
`ZoneContentTests` (deck camps walked to on their decks). A run of the planner outside Unity on the real zone, with straight-line
distances standing in for the navmesh, gave quick 47 fights, standard 48, full 56.

**Left:** the releases, the Wake and its event step, the bosses' new moves and loot, the bar, the pen bar and the bookcase worked by hand,
The Margins, the cave map. The order from the bridge's tower (the Foundry before the floor and the river) rests on the navmesh's lengths
and the height rule: `A_sim_leads_the_whole_adit_wing_by_wing` checks it on the first Unity run.

## 16. First Unity run (2026-10-14, chainR68; main merged in; lane A)
**EditMode 488/495, the Adit PlayMode classes 19/30.** TESTED as failing; the rework stays on `adit-rework`, not live. What is red, in the
order to fix:
- **The cave web's geometry (Hollow.cs / ZoneBuilder.AditDecks):** the overlook loop opens over the Hub knoll; the Cutting Drift's lip
  into the Geode Foundry is not joined on the navmesh; two solids carry walkable tops (one in the Sealed Adit near (11, -6, -303), one in
  the Muster Yard); a solid's top under the Scriptorium is on the navmesh; Sandthrone pickets at about (61, -43, -3) stand 12 m up with no
  way there; the way through is walked by 56 of 480 breadcrumbs (over 100 wanted); the yard-to-Adit walk breaks 189 m in; two Sandthrone
  overseers spawn 1.2 m apart.
- **The run:** from the bridge's tower the leader takes the Gallery before the Geode Foundry (the height rule against the navmesh's
  lengths, section 15); a run does not pass Lusk's door after his death; the planner's modes test counts six steps for a boss and his
  guard where it wants two.
- **The new elites (build step S4, not written yet):** the twelve need their own moves and signature loot lists (Brack the Bailer, Old
  Bristle, Overseer Grael Vosk, Urn-Keeper Sabel, Loremaster Ivo Senn and the rest); the guard count in the elite balance test (57, the
  test wants 30); Cinder-Warden Ysolt's call reaches Sister Ashe Varrow.
- Results: `D:\crulanda-work\encounter-validation\aditEdit1.xml` and `aditPlay1.xml`.

## 17. The twelve new elites' moves and loot (2026-10-14, 14:50; WRITTEN, compiled, not run in Unity)
- **Moves** (EliteMoves.cs): Vosk's Lash (a root) and his rally at half health; Sabel's Ash in the Eyes; Senn's Lullaby of Ash (a stun
  within 6 m) and his copyists on the pull; Corvane's Shield-Bash and "Form up!"; Varrow's Kindling ring; the Whisperer's Long Lull;
  Hollowsong's Unsung note and his choir at half; Brack's Bucket Douse; Old Bristle's charge and trample; the Old Burrower's Burrow;
  Hobb's Short Fuse ring; Old Kettle's Hot Pitch ring and the pitch that holds your boots. With the engine's moves only: what section 5
  marks NEW (a cast to interrupt, a phase that looses more, Ysolt raised by her sister, a death that releases a shift) waits for S4.
- **Loot** (loot.adit.json, nineteen pieces, icons painted): two for each required boss (Vosk, Sabel, Senn, Corvane, Varrow, the
  Whisperer, Hollowsong), one for each optional or hidden one (Brack, Old Bristle, the Old Burrower, Hobb, Old Kettle), on the
  generated curve at their levels. LootDataTests' boss count 63 to 82; LootIdBaseline has the ids.
- SocialRulesTests lets a designed pair (Ysolt and Varrow, each the other's guard) call each other.

## 18. The decks walked: the evening's runs (2026-10-14, 19:24-21:00; chainR74f, R80, R82-R85; lane A)
What the navmesh diagnostics (AditNavDiagnostics, `D:/crulanda-work/encounter-validation/adit-nav-diagnostics-R85.txt`) found and what
was done, in order. TESTED where a run shows it; the rework stays on `adit-rework`, not live.
- **The overseers' walk** ran level 8.4 m over the Cage Bowl's falling floor at aside 12.5, where the wall (Half 20, Height 12,
  bumps to 19 %) leans in to about 8 m: its planks were in the shell from z -298 on, and its stair after it. Now the walk comes down
  to 5 m over the floor at its north end and runs at aside 11.5 (`adit_layout.py`); the rim camp rides it. Then a 3 m rock block at
  the bowl's floor rose through its first metre (the block guard tried the block's foot alone): `ZoneBuilder.Block` tries the whole
  height (DeckOverlaps). R83: every deck whole, every junction complete, the walk reachable.
- **The decks' blocks named** (post, rope post, rail, body, of which deck) and the diagnostics print what stands in a deck's way with
  its bounds; a floor map of every hall (samples every 3 m, reached from the yard or not, the first unreached sample's neighbours),
  a crossing check for each gate.
- **Lusk's tally stage** (solid, 3.5 m up): the navmesh read the body's faces under the floor and left the floor under the stage a
  walkable island, walled in; the centreline walk, the Pen's mouth and the Flooded Working's join all targeted it. A first fix (the
  body stopping half a metre over the floor, a skirt below) made the body's own underside the island (R83); now a block that is not
  walkable fills a solid body from under the floor to a hand under the planks (R84-R86: the centreline walk still samples a point at
  -24.52 under the stage within 2 m, so the test's own sample needs a look as much as the mesh).
- **A camp's mobs** drawn again when the navmesh puts two on one footing (the rim's overseers 1.2 m apart on a 3 m deck).
- **The EditMode set** after the elites' moves and loot: Ysolt's guard pair (the data names Sister Ashe Varrow), fifteen answered
  calls, 94 rares, the Old Burrower's Earth-Heave (a second Rock-Crush), Brother Hollowsong among the end bosses, the planner test's
  camps planned once (three calls stacked three copies of the room's camps: six in one fight). R83: EditMode 44/44.
- **The floor map (R84, gates opened and three frames waited):** the Sealed Adit 511/544 reached, the Flooded Working all, the
  Singing Gallery 1025/1029, the Ash Garden 186/188, the Scriptorium 58/115, the Deep Way 458/1142; the Pen, the Cutting Drift
  (9/213), the Geode Foundry, the Muster Yard, the Chapel (2/263), the Sunless River (9/340), the Dry Ledge, the Grey Breach, the Quiet
  Miner's Drift and the Pitch Pit not at all. Each branch is reached only for its first metres past its mouth. The tests waited three
  frames after `OpenGates()`; a gate's carve (NavMeshObstacle) is lifted by the navmesh's own update, so they wait a second now, and
  R85: every gate crossed, open; the first unreached sample of each hall has a neighbouring cave's shell or knoll within 1.5 m (the
  Pen the Sealed Adit's shell, the Cutting Drift the Gallery's knoll, the Muster Yard the Ash Garden's knoll, the Chapel the
  Scriptorium's knoll, the Dry Ledge the River's knoll). `ZoneBuilder.Cut` now also drops a face with a corner well inside another
  cave's passage (shrink .7): R86 reached the Sunless River 59/341 (was 9) and the Dry Ledge 54/189 (was 0), the rest unchanged, so
  the corner rule is not the whole of it. R87's map names, for each hall, the path status from the nearest reached sample to the
  first unreached one and what the straight line between them meets: read `adit-nav-diagnostics-R87.txt` first.
- **The finding of 21:12 (R87):** for most halls the first "unreached" sample is PathComplete from a reached sample 3-30 m away (the
  Muster Yard: 3.4 m). One `NavMesh.CalculatePath` from the yard gives up part way through the drawn-out dungeon (its search budget),
  so the floor map, the camps test (AditNavTests "Every camp can be walked to"), the breadcrumbs and the run order were measuring the
  budget, not the caves. `NavReach.Length` already searched leg by leg but gave up when a leg ended farther off as the crow flies (a leg
  round the Gallery does); now a leg only has to get somewhere. The map and the camps test use it. The real short blocks the map
  names: the Pen's floor from the hall beside Lusk's stage (a "Solid" at (7, -24, -183): PathPartial over 11 m) and the Cutting
  Drift's floor from the Gallery below it (the drift is entered by the bridge; its own floor from the Company door needs the walk
  from the bridge, not the floor). R88 (the map and AditNavTests, AditTests, AditGateTests with the new search): 16/23, AditGateTests all
  green (the second's wait after a door opens); the map's reach rose (the Scriptorium 114/115, the Muster Yard 174/218, the Chapel
  124/263, the River 90, the Dry Ledge 119, the Deep Way 659/1142) but "unreached" samples complete from a reached one 3 m off
  remained, and sixty-four legs (R89) changed nothing: a leg-by-leg search ends in a pocket the straight-line heuristic prefers. R90
  adds the honest measure, a flood fill by hops (reached sample to unreached within 30 m by a short complete path, until nothing
  changes). **R90, the measure of the mesh itself (`adit-nav-diagnostics-R90.txt`):** wholly reachable from the yard: the Flooded
  Working, the Scriptorium, the Sunless River, the Dry Ledge, the Quiet Miner's Drift, the Pitch Pit; all but a few samples: the
  Singing Gallery (4), the Ash Garden (2), the Muster Yard (5, at x 205), the Grey Breach (1), the Deep Way (7), the Sealed Adit (33:
  the bowl's east pocket at (9..12, -303) and under Lusk's stage). **Really cut off:** the Pen (all 74: from the hall beside the stage,
  a "Solid" at (7, -24, -183) in the way), the Cutting Drift past its first metres (204 of 213: the Gallery's knoll at about x 107,
  "by the first") and so the whole Geode Foundry (458), and the Chapel's west half (109 of 263, x 109-119, z 137-140). Four blocks,
  then, not forty; the tests' reach (NavReach) wants the hop search so they say the same.
- **Where to start (21:45):** the "Solid" at (7, -24, -183) is a camp dressing block (ZoneBuilder.Adit.cs ~509-513: a brazier's or a
  crate's, of the "Spoil heaps" camp at (9, -190)); the hall beside the stage is 8 m wide there (the stage to x 6, the wall at x 14) and
  the Pen's mouth sits in that wall at z -181..-175, so the block alone should not close it: run the diagnostics with the camp's
  dressing skipped (or Block() told to keep out of a child's mouth) and read the Pen's row. For the Cutting Drift, the Gallery's knoll
  is about 13 m thick at the crown where the drift's mouth sits 12 m up (Thick(): 2 + swells + sin(th) * Height * .35), so its faces
  are wide there: try `Cut` with the corner rule at shrink .9 for knolls, or cut any knoll face within a tube's Half of its centreline.
- **The pictures (R91, `navmesh-pen-and-stage.png`, `-cutting-drift-mouth.png`, `-chapel-west.png`, `-foundry-lip.png`; 50 m
  across, a tenth of a metre a pixel, north up, walkable blue, samples reached green and not red):** the stage still has mesh under it
  (an island of red inside its outline: the body block did not take it); the hall's east strip beside the stage (x 6-14) is cut from
  the floor south of z -185, where the "Spoil heaps" camp's dressing blocks stand round (9, -190) by the south ramp's rubble flank,
  and the Pen hangs off that strip; the Chapel's west half is parted from the built hall by one straight wall at x about 110 across the
  whole passage (a shell or knoll face line, not slivers); the Foundry's floor is all mesh and all red (it hangs off the drift); the
  drift's floor past x 107 is a narrowing, not a wall. **R92 (21:48): the Spoil heaps camp moved to the hall's west
  side, and the Pen is wholly reachable (0 of 74 unreached, was 74); the Sealed Adit's own pocket 22 (was 33).** **R93 (21:56): `Cut` also
  drops a face whose edge's middle is inside another cave's passage (the Gallery's knoll faces are wider than the drift's tube), and
  the Cutting Drift and the whole Geode Foundry are reachable (0 unreached, were 204 and 458).** One block is left: the Chapel's west
  half (109 samples: the straight wall at x about 110). The chapel's lake of fire was capped to leave a 2.6 m walkway either side
  (c83a16d4) and that was not it; look at R93's `navmesh-chapel-west.png` and the chapel's Hall() west end and door next.
  **What that picture shows (21:58):** the built nave (pews and columns, all green) fills x 110-140, z 138-163; its west wall at x about
  110 runs the nave's whole depth with no gap to be seen; west of it (x 90-109: the cave's narrowing and the top of the cloister
  stair, all red) the mesh is whole. The chapel's along runs from its mouth at the Scriptorium (east) westward, so the nave's "out" end
  is the west one, at `e = Mathf.Min(nave.y, altarAt + 5)`: if the altar's anchor sits at x about 115, the out wall and its door stand
  at x 110, and the door (`Piece("Entrance", On(s, 0), ...)` with its side blocks at 2.05 m) is the thing to check: its box
  (.6 x 3.4 x 4.6) is the doorway's width if it is a NavBlocker. Also a thin strip of cave floor runs along the nave's south wall at
  z about 138, red west of x 120 and green east of it: split there too.
- **R95 (23:41): it was the altar.** The cave narrows at the altar's anchor, so the nave's west wall was built there too and the altar's
  block and candelabra stood in the doorway to the cloister stair; the altar stands 4.5 m inside the wall now (c16a0410) and the
  chapel is wholly reachable. **Every floor of the Adit is reachable from the yard** but the bowl's east pocket (9..12, -303), the
  island under Lusk's stage, four samples at the Gallery's south rim, two in the Ash Garden, five in the Muster Yard's east (x 205), one
  in the Breach, seven in the Deep Way. AditNavTests, AditTests, AditGateTests and the diagnostics 18/23; the camps test and the
  gates green. Red: the solid tops (two, and the Scriptorium's); the breadcrumbs (56 of 480) and the centreline walk, both broken at
  the stage's island, which the body blocks did not take (a Box source is its faces; the floor under it kept its head-room): every
  NavBlocker box now also goes in as a ModifierBox, which marks its whole inside not walkable; the web test's last line (the cloister
  door shut and re-carved, three frames waited: a second now).
- **R97-R99 (23:56-00:13): the island under Lusk's stage.** The ModifierBox took the bowl's east pocket and the Gallery's rim (R97) but
  not the floor under the stage; R98's new table showed the stage's eight body blocks in place and the floor under its middle still on
  the mesh. The bake reads surfaces: a floor under a body whose bottom is below it keeps its head-room. Now a solid body has shelves
  inside it every 1.2 m or less (2dab45f9), so no span in it, the floor included, is tall enough to stand in. **R99: the Sealed Adit 0
  unreached — every floor of the Adit is reachable from the yard; the breadcrumbs test green; 19/23.** The centreline walk moved on to
  the Foundry's first floor and its one search from the yard (5ef7f190: leg by leg now; R100, 00:24: still 19/23, the Foundry's first floor
  still unreached from the yard by that search though the hop map reaches all of it: NavReach's heuristic, the first thing left).
- **00:49: the "solids" with mesh on their tops were knolls.** A branch cave's knoll (the rock mound built over its passage) was a
  navmesh source like a walk-in cave's hill, and its crown poked into the parent's hall near the wall as a floating floor (the
  Censored Room's over the Scriptorium at y -59.7, the Kennel's in the Muster Yard). A branch cave's knoll is buried rock now, not a
  source (6353aafd): both solid-top tests green. With the helpers of the small hours: `AditDataTests` (every reference in the zone
  file resolves: 97 camps, 9 gates, 27 anchors, 22 decks, the planner's three runs), the Adit camera tour
  (`--crulanda-adit-tour <dir>`, 28 frames), and the release of `after` camps (a boss's relief shift, the Wake's waves, Hollowsong's
  choir), which the data names and the code never spawned: being written.
- **01:03:** with the decks as breadcrumbs of the tests' reach helper the Foundry's first floor is walked from the yard: the Adit nav,
  camps and gate tests and the fixture class 25/28. The diagnostics' step-by-step walk of every cave found no broken step, so the
  cloister stair is whole; the altar's way home of 236 m is a detour that starts somewhere at the chapel's west door (the table flags
  a complete step four times its straight length now). The fixture test's two (its knoll ray; a rail check that stopped passing
  when the deck bodies changed) are the test cave's, not the Adit's.
- **01:41: the Adit nav, camps, gate and release tests 26/26.** The cloister stair's foot sat 1.5 m below the Gallery's floor (the
  step walk found it: a 321 m detour three steps from the end); its last rows raised. The releases (a helper's: `EncounterSession.Release.cs`)
  pass their three: Lusk's relief shift three seconds after his death, the Wake's four waves in order with Hollowsong's choir at half,
  the keg's release of Hobb. The camera tour (28 frames, `world-captures/adit-tour`) shows the chapel as a nave now; the kit's
  entrance-top module placed by height came out a 20 m slab across the chapel's west end and the Scriptorium (a thin up-axis): the
  packs helper sizes every piece by an explicit box. The fixture test's two (its knoll ray, its rail check) are the reds left.
- **Left (2026-10-15):**
  the web test's shut-door line (`home`, the stair way with the door open, came out 236 m: NavReach.Length pads a way with legs that
  wander into pockets, so the shut way is not 50 m longer; NavReach wants the hop search, and an honest length with it); the fixture
  test's knoll ray; then the run itself, played.
  Then NavReach gets the hop search and the camps, breadcrumbs and run-order tests are read again.
- **Still red after R86 (PlayMode 19/29; see R88 above for the change):** the camps nobody can walk to (the Pen's, the Cutting Drift's and everything past them),
  the breadcrumbs (56 of 480), the sim's run order (the Foundry unreachable), the web's joins (the cloister stair from the altar),
  the centreline walk at the tally stage, the fixture trestle's rail (a raycast; new in R83, not looked at), the fixture knoll ray,
  the solid tops. The first of these is the one to fix: every branch floor reachable, and most of the rest follows.
- The fixture test's "loop opens over the hall" ray meets the Hub's own knoll (the knoll is not cut by a joining cave's air); the two
  solids with walkable tops (the bowl at (11, -6, -303), the Muster Yard) and the Scriptorium's; the breadcrumbs (56 of 480) and the
  run order follow the floor map.

## 19. The chapter-house on KayKit's grid, and its doors (2026-10-15, 01:30-02:20; chainR116 the tour, R118 the proving build; lane B)
Chris on the 02:00 tour frames of the chapel ("still not sure what this is and looks bad", "just the shape and design is bad", "use some of
the dungeon assedt not just plain walls and arch") and of the Scriptorium's door ("now this looks blocked??", "does the door open?").
- The four rooms rebuilt on the KayKit Dungeon Pack's 4 m grid (`ZoneBuilder.Adit.cs` KitHall/KitEndWall/Kit): floor tiles, walls in bays
  with pillars, arched windows in the chapel's third row, torches, banners, the altar's dais and stair, the apse's window.
- **The door that looked blocked was blocked.** KayKit's `wall_arched` is a blind arch: 124 vertices of stone brick inside the arch's
  opening (|x| < 0.85, y 0.15..2.2). The swap to `wall_doorway` (d74570a2) put a shut red door leaf with a ring there instead (24 vertices
  of planks and knocker). The one wall piece with nothing in its slot is `wall_doorway_sides` (0 vertices in |x| < 0.95 below y 2.5): a
  2 m slot with 4 m returns, boxed (10, 8, KitThick): a 4 m clear, 5 m tall opening on both faces, the returns flattened into the wall's
  metre (their inner faces are the reveals). The side runs start at |a| 5 so no wall module lands in the bay; corners at the ends; the
  jamb Blocks (3, 8, 1) at a = +-3.5, so the navmesh gap stays 4 m. A flat lintel: the kit has no open arch (`wall_archedwindow_open`
  boxed over the slot would fake one but shows its sill; not done). Commit e7f5f6a1; proven by R118's tour frame, not yet by Chris.
- The chapel's tour shot (R116's frame 19 was a pink close-up: the camera at 4.5 m up stood in a wall) now from 9 m up by the east door.

## 20. S6 as built: the Cove (2026-10-15; branch `adit-cove` off `adit-rework`; WRITTEN, compiled outside Unity, not run in Unity, OPEN)
Chris, 2026-10-15 (the question prompt, verbatim): Q3 "Build the Cove"; Q2 "Keep ~75 min (Recommended)". All of it **GAME-ONLY** (the
Sandthrone are CANON; the old line is CANON-EXPANDED, book2 ch.19); the Deadmines' ship in its cove is a lesson in shape only, nothing
copied. **No machine of any kind** (memory rule, "i told you NO Machines!"): rails, timber, rope, rubble and hands. No save-format change.

**What was made:**
- **Two caves** (`tools/wip/dungeon/adit_layout.py`; `--live` wrote `Zones/adit.json`, the staged copy matches):
  - **The Dead Line** (style `deadline`, 182 m): out of the Rail Hall's far end, west. A rail tunnel, -100 to -114, two sidings.
  - **The Black Cove** (style `cove`, 200 m): off the Dead Line's end, a beach of black scree down into a natural cave 88 m across and 36
    high. Black water wall to wall (`caveWater` flood, 2.6 m over a floor 121 m under the yard: swum, not walked).
  - 20 caves, about 3.24 km (was 2.86).
- **Ten decks** (the plan's section 3, D4):
  - the quay along the west wall (water +0.8, railed on the water side);
  - the scaffold: tier 1 (quay +2.2, its ramp at the north end), tier 2 (+3.4, its ramp at the south end);
  - the gangway from tier 2 onto the barge;
  - the ore barge (35 m of deck at water +4.2, a solid plank hull from the floor);
  - the stern castle (10 m at +7.5, so the barge is 45 m) and its stair up the barge's east side;
  - the scree landing stage at the quay's south end, where a swimmer climbs out.
  - Check 6 lets a deck's end stop in the open on purpose (`free=`: the barge's bow, the castle's ends).
- **Anchors:** `carriage` (where the carriage stood, 14 m short of Danner's old camp, so the Weaver's lock, waves and mules do not move),
  `cove`, `haul`, `bollards north`, `bollards south`, `barge bow`, `stern castle`, `drain`.
- **Camps** (after every camp of the rework, so none before them moves; only the patrols and the Quiet Miner move on six places):
  - Danner (pinned 16) and his guard (15) on the stern castle, wing `cove`, step 5;
  - the Siding rearguard (3 dockers, the Dead Line's first siding, step 1);
  - Rope team (north) and Rope team (south) (3 Sandthrone bargemen each, on the quay, steps 2 and 4);
  - the Harbour netters (3, on tier 1, step 3);
  - Up from the hold (first) and (second) (2 bargemen each, released by `event:haul1` and `event:haul2`).
  - The Rail Hall's Siding dockers are optional now (time, Q2).
  - 103 camps, 244 mobs. Wing `cove` comes after `deep`. Two scaffold caches (secrets `scaffold_a`, `scaffold_b`). Two places on the map.
  - One switch: `COVE_FINALE` in the layout (True here). False writes the zone as it was before S6. Then revert the two quest texts below.
- **The dressing** (`ZoneBuilder.Adit.cs`, `DeadLine()` and `BlackCove()`):
  - the Dead Line: rails, cold lamps, a spare wagon and stacked sleepers at each siding, the buffer-stop, and the spare wagon at the line's
    end (a second "carriage": the way home from the Cove is not the walk back, lesson L2);
  - the Cove: lantern posts on the quay, three timber bollards to a group, and the warps from them to the barge's bow and stern
    (`ZoneBuilder.CoveWarps`, and "Cut the warps" at the north bollards);
  - the barge: iron bands down her hull, the hold's hatch, geode crates under a violet glow, racked oars, her stem post, a long steering
    sweep, a lantern on the stern castle; the drain's dark mouth at the south end.
- **The Haul** (E5, `EncounterSession.Haul.cs`, `AditHaul.cs`):
  - The barge lies aground on the bar beside the scaffold. Two rope teams heave her off, hand over hand round the bollards.
  - It starts when you come onto the quay (26 m of the `haul` anchor) or a hauler is struck.
  - It goes on only while bargemen are on the ropes (alive, not fighting, within 8 m of their place), at the share of them still hauling:
    75 s with all six.
  - Every hauler dead, or the warps cut (E at the bollards, with nobody on that rope), and she stays fast.
  - Floated, each of Danner's calls (the pull, and his "More." at half) brings two of her crew up out of the hold (`HaulCall` from
    `Rally`), twice a visit at most.
  - Nothing is saved.
  - (This is the task's "the barge hauled by rope teams". The plan's older E5 had the haulers drag the carriage down the line. That would
    take the ride-out carriage away, so the barge is hauled instead.)
- **The netters' Weighted Net:** `MobCast` kind `net` (new fields `cc`, `seconds`, `area`). A 1.8 s cast, every 14 s, up to 18 m. It roots
  everyone of the party within 4 m of its target for 3 s. Interrupting the cast stops it.
- **Danner** (`CoveFinale`: true when his camp stands in a cave of style `cove`; read from the data):
  - he no longer lies hidden for the Weaver's escort, and does not rise at half the lock (`StartWeaver`, `WeaverHalfway`);
  - he fights on the stern castle with his guard;
  - `RideAllowed`: the lock matched, **or** Danner dead this visit (`DannerDead`). The ride's refusal says where he is.
- **Quest text** (`Quests/adit.json`):
  - The Dead Line: Sorrel in the Rail Hall, Danner on the stern castle of his barge; his death line.
  - The Weaver's Lock: one sentence in its summary (the lock, or Danner dead, frees the carriage).
- **Found on the way and fixed (its own commit):** `ZoneBuilder.DeckBlock`'s position and turn had stood inside a trailing comment since
  cbc484dc (2026-10-14 20:11).
  - So every deck's post, rail and solid body block was built at the Decks root's origin, not where it belongs. The R97-R99 fight with
    "the body blocks did not take it" under Lusk's stage was likely this.
  - The tour lamp's colour and shadows were the same mistake.
  - **This changes every deck in the Adit** (rails and posts now block where they stand): run the nav tests below before reading anything
    else.

**Time (Q2, keep about 75 minutes), at evening-3's pace:**
- What it adds:
  - about 0.4 km more walking, about 2.5 minutes;
  - four more trash fights (the rearguard, two rope teams, the netters), about 3 minutes;
  - Danner's fight moves; it is not added.
- What it takes off:
  - the Siding dockers are optional (about 0.75 minutes);
  - the spare wagon at the line's end saves the walk back up the Dead Line to the carriage.
- **Net: about +5 minutes.** The required run goes from about 68 to about 73 minutes; the default run from about 75 to about 80.
- To come back to 75: make the Rail Hall's Platform guards optional, or make the two rope teams one fight. Each saves about 45 s. Not done
  without Chris.

**Untested (nothing here has run in Unity):**
- the navmesh over the quay, scaffold, gangway, barge and castle;
- the flood's swim area round the barge's hull;
- the warps' and crates' places;
- the haul's timing at 75 s against a real party;
- whether the haulers' working pose reads;
- the netters' net in a fight;
- the hold's crew on the barge's deck;
- the carriage anchor reproducing the old carriage place exactly;
- whether the run's planner takes the Cove last;
- the drain's mouth;
- the time.

**What Unity must run (lane A, one job at a time; then lane B for the frames):**
- EditMode:
  - `AditDataTests` (haul events, the run ends in the Cove);
  - `SocialRulesTests` (the `bargeman` and `netter` tags);
  - `DungeonPlanTests`, `QuestDataTests`, `LootDataTests`, `EliteBalanceTests`.
- PlayMode, in this order:
  - `AditNavTests` and `AditNavDiagnostics` (the DeckBlock fix touches every deck, then the Cove's decks);
  - `AditCoveTests` (new, five tests: the Cove walked from the Rail Hall and its water swum; the haul run and floated; stopped by its
    haulers or the warps cut; Danner on his stern castle and his loot; the carriage on the lock or his death, and the spare wagon);
  - `AditWeaverTests` (updated: Danner keeps his barge, no rise);
  - `AditTests` (every camp where it stands, deck camps on their decks);
  - `AditGateTests`, `AditReleaseTests`, `AditCaveBuildTests`, `MobBehaviourTests`, `BossPhaseTests`.
- The Adit camera tour (`--crulanda-adit-tour <dir> --crulanda-zone zone.adit --crulanda-temp-save`): the four new frames
  - `dead-line`;
  - `black-cove` (from the beach);
  - `ore-barge` (from the quay);
  - `stern-castle` (along the barge's deck);
  - and front, back and side frames of the barge for Chris.
- Then the Cove's own group run (`group_run.ps1`, the Adit, events on). Only when it passes does `adit-cove` merge into `adit-rework`
  (the plan's "until the Cove's own group run passes": the branch is that gate; `COVE_FINALE = False` is the way back).
