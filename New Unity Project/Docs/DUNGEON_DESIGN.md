# Dungeons: the brief and the plan (read this first)

## 0. Chris's brief (restated 2026-10-10; it holds over everything below)
- **The first dungeon is a good mix of three classic dungeons: the Deadmines, Wailing Caverns and Scarlet Monastery.**
  Draft 1 (below, 2026-10-07) took its third model from Ragefire Chasm instead of Scarlet Monastery, and the cave builder made every
  space of the Sealed Adit a rock tunnel, so the dungeon reads as one long cave. Both were wrong; this section corrects them.
- **Four dungeons in all, up to level 30 eventually:** the Sealed Adit (10-12) and three on the 16-30 road (ROADMAP Phase 9: the
  Sludge-Tunnels 18-20, the Deep Veins 23-25, the Transit Hub 27-29). Each is held to the same bar as the first.
- **Dungeons are 5-player group instances: 1 tank, 1 healer, 3 damage dealers or support** (Chris, 2026-10-10). In this game the five
  are you, Mira (the healer) and three sims (MaxPartySims is 3); when you play a healer, a sim takes the tank's place and Mira supports.
- **Three bands** (Chris, 2026-10-10): **low** dungeons from about level 10 to 15, **mid** 15 to 25, **high** 25 to 35. The Sealed Adit
  (10-12) is the low one; the Sludge-Tunnels (18-20) and the Deep Veins (23-25) are mid; the Transit Hub (27-29) is high. The high band
  reaches 35, past ROADMAP's cap of 30: to settle when Phase 9's levels are set.
- **"A mix" means the places as well as the mechanics:** mine tunnels AND built halls AND a stronghold of stone, each wing looking
  unlike the last, as well as the bosses, gates, escorts and patrols. Lessons only: no names, layouts or assets from those games.

- **Scale and content (Chris, 2026-10-10 evening, verbatim, after seeing the first built halls):** "the whole place needs to be
  bigger. doesnt seem like deadmines or wailing caves like wow dungeons"; asked how much bigger, he chose **"3x, full-length run"**
  (about a kilometre of cave or more, a 45-60 minute run like the full Deadmines); "if need to make it its on zone you zone into so
  there are no limitations"; and **"not just bigger but more too do!"** So: the Adit (already its own zone, zone.adit) grows to three
  times its length with chambers far wider and taller, and the new length is filled: more packs and patrols, more bosses and
  mini-bosses, events, side areas, secrets and things to use, not empty tunnel.
- **The Adit rework (Chris, 2026-10-14, verbatim):** "i think adit dungeon need to be more complex like wow's wailing caves or the deadmine. adit is nothing like those". The plan (a web of wings at three heights round the Singing Gallery; design only, his four questions first) is in ADIT_REWORK.md.
- **Checkpoints (Chris, 2026-10-16 ~22:40, verbatim):** "one thing to help respawns is to have checkpoints in the dungeon so they don't have to run all the back from the original spawn area. maybe 3 checkpoints in dungeons". So every dungeon has three checkpoints along its leader's order; the first touched by the party is where a fallen member recovers from then on (the latest reached), not the entrance.
  - **Built 2026-10-16 (GAME-ONLY; tests written, not yet run in Unity):** `ZoneDefinition.checkpoints` (name, look, and a place given as an anchor's is: cave, along, aside, rise, or `at` on open ground), three in each dungeon's json in the run's order; `ZoneBuilder.Checkpoints` builds a marker for each beside the way (no collider, nothing walks round it); `EncounterSession.Checkpoints` marks one reached when you, a party sim or Mira comes within 4 m of it alive (chat: "Checkpoint reached: ..."), and Recover wakes you, and the fallen of the party beside you, at the furthest along the run reached (by its place in the list, so walking back past an earlier one does not move it), ahead of the spirit stone and the zone's start. Kept for the visit, not saved (no save change). The group run logs "checkpoint reached" and "recovered at" lines.
    - **The Sealed Adit, three miners' waycairns** (flat stones stacked chest-high, a pale chalk rune and a soft light): the Overlook cairn at the end of the Old Workings (Sealed Adit 369 m, 3.6 m aside, under the overlook), the Gallery's east cairn on the Gallery's floor between the Foundry's and the Ash Garden's ways (210 m, 26 m aside: the hub the middle wings go out from and come back to), the Rail Hall cairn just inside the platform gate across from the rail spirit stone (Deep Way 168 m, 5 m aside: the Rail Hall and the Black Cove after it).
    - **Hallow's Creek, three still lamps** (an iron post and a lantern whose flame stopped with the town and has not moved since): at the lip before the bridge (-7, -238), on Main Street by the yards after the bakery step (7.2, -57), by the Archive's steps (34, 64).
    - Each stands 10 m or more from every camp, 5 m from a patrol's round, 6 m from a secret, off the pools, the creek, the chasm and a road's middle (CheckpointDataTests); on the floor where it is drawn and walkable from the yard (CheckpointTests).
- **Not every dungeon a cave (Chris, 2026-10-16 ~20:10, verbatim):** "just fyi all dungeons don't have to be cave systems. just a note". So the later dungeons can be built places above ground or in the open (a town, a keep, a tower, a walled district, a ruin), not tunnels by default; Hallow's Creek (DUNGEON2_DESIGN.md) is a paused town in the open with cellars under it.

## 0.05 No machines (Chris, 2026-10-11, about 01:03, on the Rock-Eater: "more machines!!!!!  i told you NO Machines!")
Nothing in the dungeon is a machine. Chris's choices (01:04, the question prompt):
- **The Rock-Eater becomes "a great burrowing beast"**: a huge rock-eating cave beast that Nix rides and drives. The fight stays: the
  beast, then Nix.
- **The carriage, the rails and the cage-lift become "hand-worked"**: an ore wagon on the old rails pulled by a beast or by hand, a
  rope-and-winch cage worked by people; no engine anywhere.

## 0.055 What was done for 0.05 and 0.06 (2026-10-11, after noon; TESTED or BUILT as CLAUDE_HANDOFF.md says)
- The Rock-Eater: the bear's body at 3.6 m, a stone-grey hide, violet geode crystal through its back, a saddle with Nix in it.
- The carriage: two plank-sided, iron-banded ore wagons on wooden wheels, the geodes in crates, a driver's bench, traces out to two pit
  mules standing up the line; the loading crane gone; the "levers" and "train" words gone from the text and the loot.
- The winch gate (was the cage-lift): a timber windlass with its rope up to the grille.
- The fill: 67 camps (33 before, 148 mobs from 70), eight patrols of two walking the drifts, every camp spread over its widened ground;
  mobs stroll their camps and stand a body's width apart (all zones); rock outcrops, rubble, dripstone, fallen timber and geode clutches
  over the wide chambers' floors.
- The built halls: the kit's walls turned to face into the halls (they faced the rock), end walls back to back, the floor flags darkened.

## 0.07 The group runs Chris watched (2026-10-11, 16:50-18:00; PLAYTEST_NOTES 133-141; toolsalidation\group_run.ps1)
A party of five led by a sim through the Adit at three times speed, on his screen, each run logged (`D:\crulanda-work
un-captures`).
What the runs settled:
- **Pace:** with the scaling eased, the sims armoured and hitting like players, and no blow over 60% of a member's health, the 17:37
  run cleared 37 of 67 camps in 30 game-minutes (two deaths, both at the Gang-Boss), on course for about 55-60 minutes: the 60-75 minute
  brief (0.06) is within reach of the bosses' fights and the wings' events.
- **The duo** (Chris: "the duo boss needs to be tougher. maybe perm CC one while killing one?"): Nix leaps down as the Rock-Eater is
  engaged, marked for the party to hold; free, he goads the beast (its blows half again, mending half a percent a second); held (he can
  be, for the full time, as long as the party keeps him held), it is calm; Nix killed first, it rampages. The Rock-Eater's health 1.3
  (was .95). The play: hold Nix, kill the beast, then Nix. (EncounterSession.TickDuo; EliteMove.holdable.) First cut at 18:23 (health 1.6,
  a 2% mend, its shake every 12 s, Nix's shriek every 15 s) was a stalemate: 23 game-minutes on the beast with no kill; eased at 18:43.
- **Bosses that hold the party** (Chris: "needs some of the bosses to stun or cc members of the party? maybe freeze? stun? fear?
  disorient?"): Haddo Lusk floors whoever he fights (stun 2 s, every 14); the Rock-Eater shakes everyone within 5 m (disoriented 3 s,
  every 20: your steps go the wrong way); Nix's shriek (fear 3 s within 4 m, every 25); Ysolt's "the fire sees you" (fear 4 s within
  6 m, every 16); Danner's coupling chain (root 4 s, every 14). EliteMove.cc; EncounterSession.HoldPlayer; SimCompanion.HoldUntil.
- **A camp nobody can reach** held the 17:37 run (an Ash mender up where no one got at it): a leader gives a camp up after 45 s with
  nothing to fight, and strollers keep to their own floor.
- The scorecard at the end (0.08 below), the party leaving held mobs alone, the leader waiting for the whole party: CHANGELOG 16:50.

## 0.075 Boss loot (Chris, 2026-10-11, 18:59: "any good loot drops?")
Two runs, two bosses each, coin only in the player's bags (the named piece rolled at 50% and, in a party, rolled for round the five).
Decided: **every Adit boss drops one of its named pieces every time** (the signature groups of loot.adit.json at chance 1; the 20%
"runner's" extras as they were). The Rock-Eater has no table of its own: its pieces are Nix's. The group's roll still decides who takes
it; the scorecard says who did.

## 0.08 The scorecard (Chris, 2026-10-11, 17:44)
When a sim-led run is called cleared or the dungeon's last boss falls (and by /scorecard): every member with its class, damage and
healing a second over the run's fighting time, deaths; the MVP (the most damage and healing together, a death counted against it as a
tenth of the best) and the healer of the run; what dropped, off whom, to whom; the coin. CombatMeter.Scorecard, EncounterHud.DrawScorecard.

## 0.06 How long a run takes (Chris, 2026-10-11, about 01:21)
"all you did in the dungeon was make it bigger which in turn has less mobs and would not take 60-75mins to complete". **A full run of the
Sealed Adit takes 60-75 minutes.** The scale-up (0.25) made the caves three times longer with the same camps; the time has to come from
what is in them: more camps and mobs, patrols, side rooms and events, the wings' bosses (0.25's table), not from walking.

## 0.1 What Scarlet Monastery brings (lessons only)
- **A fortified house of zealots, built in stone:** halls, stairs and doors, not caves. The order feels like an army in its own house.
- **Wings, each a short run to its own end boss and each with its own look:** a graveyard of the order's dead, a library, an armoury
  with a training yard, and a cathedral. A group picks a wing to suit its time.
- **Packs of fanatics** that heal each other, call out for help and fight to the end; a hall full of them is the challenge.
- **A finale pair:** the commander, and the priestess who raises him once when he falls: kill her first or fight him twice.
- **Rooms you know from one look:** rows of pews, shelves of books, racks of weapons and dummies, banners, a great window.
- **A secret:** a hidden door to a side chamber.

## 0.2 How the Sealed Adit takes all three (the rework)
| Wing | Its model | Built as | Status |
|---|---|---|---|
| [1] The Old Workings, [2] the Singing Gallery | the Deadmines (the mine) | cave tunnels, timber sets, rails | built (D1-D8) |
| [3a] The Geode Floor | the Deadmines (the goblins' foundry) | **a built foundry hall:** stone walls, catwalks on columns, the Rock-Eater's berth (a beast's since 2026-10-11, 0.05) | to rebuild (D9) |
| [3b] The Ember Vent, becoming **the Cult of Ash's chapter-house** | **Scarlet Monastery** | **a built stronghold in the vent:** the ash-garden of urns (graveyard), the scriptorium (library), the drill yard (armoury), the chapel over the lava lake (cathedral), each with its boss; the finale pair is the Cinder-Warden and a priestess who raises him once | to build (D9-D10) |
| [3c] The Grey Breach | Wailing Caverns (corrupted caves) | cave | built |
| [4] The Rail Hall | the Deadmines (the reveal) | **a built station hall** | walls, columns, tiles 2026-10-10 |

Wailing Caverns' lessons stay where they are: the three branches in any order, the escort with waves (the Weaver), mobs that flee
for help, casters that put you to sleep, and the shortcut back (the rail ride).

## 0.25 The full-length Adit (Chris, 2026-10-10 evening: "3x, full-length run", "not just bigger but more too do!")
The layout grows (tools/wip/dungeon/adit_layout.py): passages about three times as long, chambers about 2.5 times as wide and twice
as tall, drifts half as wide again (still a mine's drifts); a kilometre and more of cave. The new length is filled, wing by wing
(lessons only, all GAME-ONLY):
| Wing | Model | What is added |
|---|---|---|
| [1] The Old Workings | the Deadmines (the mine) | patrols on the long drifts, ambushes from side galleries, a flooded lower working with a mini-boss, a collapsed-shaft shortcut back |
| [2] The Singing Gallery | Wailing Caverns | a great multi-level water cavern (ledges, pools, a fall), sleep-casters and corrupted beasts, an optional boss off the path, a jump-down shortcut to the hub |
| [3a] The Geode Floor | the Deadmines (the foundry) | the foundry hall and the Rock-Eater's berth (built, D9); a sorting hall of sluices and swinging ore-buckets on ropes, worked by hand, to time your way past (no machines, 0.05); an overseer before Nix and the Rock-Eater |
| [3b] The Ember Vent | Scarlet Monastery | four wings with an end boss each: the ash-garden (an urn-keeper who raises the dead), the scriptorium (a loremaster), the drill yard (a drillmaster), the chapel (Ysolt and a priestess who raises her once); packs of fanatics that heal each other |
| [3c] The Grey Breach | Wailing Caverns (corruption) | the grey spreading deeper in, the Foreman, the Quiet Miner (rare) |
| [4] The Rail Hall into the Great Cove | the Deadmines (the cove) | the line runs on into a vast underground harbour: a moored ore barge, its cannon fired the loud way, the last boss on deck, the hidden boss below |
Order: the scale-up first (layout, every hard-coded distance, tests), then the wings one at a time, each shown to Chris in frames.

## 0.3 The room builder (build step D9)
- **Rooms from a plan in data:** each room a rectangle or an L, with a floor height, a wall height, doors to its neighbours and a
  style (temple, foundry, crypt, library, armoury, station).
- **Built from the modular kit** (Resources/Props/Dungeon, Chris's 41 pieces): walls, floors, columns, stairs and arches. Colliders
  and nav blockers follow the walls, and doors are openings.
- **Dressed by style:** pews, bookshelves, weapon racks and dummies, urns, banners, candelabra, carpets.
- **Caves and rooms join:** a tunnel ends at a room's door. The same builder makes the three later dungeons' halls.

## 0.4 The three later dungeons (a sketch; each gets its own design section before it is built)
- **The Sludge-Tunnels (18-20, the Forge District):** sewer tunnels (cave builder) into a built smelting works and a syndicate's
  counting-house. A mix of the Stockade's short city run and Gnomeregan's long built halls (no machines, 0.05).
  - **Designed in full 2026-10-14:** see **The Sludge-Tunnels** below. Built when the cap reaches 20. Its givers stand on its own
    open ground (the Outfall), so it waits for no other zone.
- **The Deep Veins (23-25, the Warrens):** the penal-legion mine (tunnels) with a built prison block and an overseer's keep: the mine
  and the gaol.
  - **Designed in full 2026-10-14:** see **The Deep Veins** below. Built when the cap passes 22.
- **The Transit Hub (27-29, the Crown):** a built junction of lifts, platforms and cargo halls, with service tunnels between. The
  last boss drops the letter that opens the Spire.
  - **Designed in full 2026-10-14:** see **The Transit Hub** below. Built when the cap passes 29. It stands over the Half-Light,
    between the Lowtowns and the Crown, and has at most three rope rides, each worked by people (0.05; never "lift" in player text).
- **The rules the three share** (each a 45-60 minute full clear; a party of five; 10 m ways; every named elite holds the party;
  every named piece every time; spirit stones clear of camps; a crowd budget; one owner for each faction and set piece; names;
  the story chain from the Adit's letter to the Spire): see **The three later dungeons: the rules they share**, just before the
  Sludge-Tunnels.
- **Chris's brief, 2026-10-14 about 01:30 (verbatim):** "i think adit dungeon need to be more complex like wow's wailing caves or
  the deadmine. adit is nothing like those". Said of the Adit (as built it reads as one long route of camps, 66 in a row); it is
  the bar for the three below too, and each checks itself against it.
- **Out of date above:** "MaxPartySims is 3" (section 0, l.10). It has been 4 since 2026-10-11 and counts sims only; Mira is apart
  from it (EncounterSession.Party.cs l.15, l.100-104). How a party of five is kept: the shared rules.

# Draft 1: The Sealed Adit (2026-10-07; levels 10-12 since the cap-15 rescale, see ROADMAP)

Chris: "7-9 dungeon creation that rivals wow dungeons like these. research these completely before dungeon design. leveling
takes longer in land of crulanda", then "not 3..one big dungeon" and "we will design others later". So this is **one** big
dungeon for **levels 10-12**. It takes the best of three classic dungeons (research below). It uses **no** names, assets or
layouts from them, only design lessons.

## 1. Where it is and what it is (lore)

| Thing | Label | Source |
|---|---|---|
| Old mining tunnels under the Shattered Peaks, with a deep mag-rail line south under the range to a coastal transit hub; rusted iron ladders, the smell of old oil and stagnant water, green lum-tube light | CANON | book2 ch.19 |
| Aether-geodes: raw ore cut from deep seams; really fossilised memory and soul-residue, the fuel the Council burns | CANON | book1 ch.3, world_bible |
| The Shattered Peaks hold back the Wasting, but the void is slowly eating through the stone; what it touches is *unmade* (grey static); the Unmade | CANON | world_bible, book2 ch.18 |
| Sandthrone mercenaries hold the Peaks and charge a toll | CANON | book1 ch.5 |
| The Cult of Ash: fire magic, Torvyn Embercloak, the Ash-Walkers | CANON | world_bible |
| Goblins, the Weavers of the Warrens: Vibration-Locking (a mechanism comes apart when its resonance is matched), Static-Charting | CANON | world_bible |
| Silent Pilgrims and their echo-jars (fragments of lost eras) | CANON | world_bible |
| The Sealed Adit's mouth on the Peaks (the secret, the ore at its face, the yard and the spoil heap) | CANON-EXPANDED | WORLD_ZONES |
| That the Sandthrone have broken the seal and are secretly cutting geodes below, with pressed goblin labour, to ship them south by the old rail | GAME-ONLY | this doc |
| That an Ash cell meets in a hot vent off the workings, and that the eastern drifts have broken into ground the Wasting has already eaten | GAME-ONLY | this doc |
| Every person, boss, mob, item and quest named below | GAME-ONLY | this doc |

The story in one line: **the Sandthrone toll on the pass is a front.** The real money runs underneath it, out of the old
workings and south on a rail that everyone thinks is dead. The game is set before Oakhaven's erasure, so no canon character
dies here. The Sandthrone's commander (CANON) is only named in the letter the last boss carries. That letter is the hook for
the next dungeon.

## 2. What we took from each classic dungeon

**From the best-loved (the mine with the ship at the bottom):**
- A story told across the zone **before** you go in (a chain of quests in the open world), so the dungeon pays it off.
- Mostly one way through, but every room is its own place: the mine, the workshop floor, the foundry, then the cove.
- **The reveal:** you come out of tight tunnels into a vast cavern with a whole ship in it. This is the most remembered moment
  of the dungeon.
- A gate you open by **doing something in the world** (loot the powder, fire the cannon). The loud way alerts the next boss.
  The quiet way (picking the lock) does not.
- Bosses that each teach one thing. The rider whose beast you bring down first and then him (two-part fight; it was a pilot and his machine until 0.05). The first mate who
  **stuns the whole group at 2/3 and 1/3 health** and runs to his chest for a heavier weapon. The captain on deck with two
  guards. The leader who **calls guards out of the shadows at half health**.
- **Patrols walk in behind you after each boss** (the doors open), so the group never fully relaxes.
- The last boss drops **a letter that starts the next chain** (it leads to the next dungeon).
- A hidden extra boss (jump off the far side of the ship) and a **rare spawn** (about one run in five).
- A gear set that drops across the whole dungeon.
- Weakness: 2 to 2.5 hours, with no repairs or vendors inside.

**From the cave with the four druid lords:**
- An **approach area outside** the door, with its own mobs and quests (the oasis).
- **Four lieutenants, in any order**, who must all die before the finale opens. This makes the dungeon non-linear.
- An **escort event**: you walk a friendly NPC to a ritual spot, hold off **waves**, and then a **surprise boss** rises.
- A theme of corrupted ("deviate") wildlife. Casters put people to **sleep**, wounded mobs **flee** for help, and beasts
  **call for help**.
- Optional bosses off the main path. One of them unlocks a **shortcut back** (the waterfall).
- Atmosphere: the cave **wails** (wind through the vents), which is where its name comes from.
- Weakness: a maze. People got lost and gave up. We keep the branching but make every branch obvious.

**From the short lava chasm under the capital:**
- **Short, easy and complete**: the best first dungeon to learn on (under an hour).
- **Path round a lava lake.** The big boss's uppercut can knock you **into the lava or into the next pull**.
- A cult hiding in the heart of your own capital: the enemy within.
- Pulls of 1-2 at first, then cult groups of 2-3 that need a plan. Every mob elite.
- Weakness: a long walk back from where you respawn.

## 3. Level and length

- Crulanda levels slower (cap 15 now, 30 in the end; the Peaks are 9-12). The classic band of about 15-24 out of 60 maps to **10-12** here :
  - trash 10-11;
  - branch bosses 11;
  - the hall's bosses 12;
  - the last boss 12 elite, `harder`;
  - the hidden boss 13.
- **Length:** a full clear is 55-75 minutes with a party of sims (Chris, 2026-10-07). It is built so it **does not have to be done in one go**:
  - **Wing 1 plus one branch is a 25-minute run** (the short-dungeon lesson);
  - a **spirit stone at the Gallery** (the respawn point once reached);
  - **the rail as the way out** at the end (fixes the long walk back).
- Party: you and up to four sims (the round-27 dungeon runs: a sim leads, camp by camp).

## 4. The layout

> **The reworked Adit (2026-10-14/15; Docs/ADIT_REWORK.md governs; on the `adit-rework` branch, not live yet).** Sections 4-9 below are
> Draft 1's Adit, kept for what it chose and why. The rework makes it a web: the Old Workings down to an overlook 14 m up the Singing
> Gallery's wall; the Gallery crossed at three heights (the trestle, the rope bridge and its tower, the floor and island); three sigil wings
> in any order (the Geode Foundry, entered high and left by the chute; the Cult of Ash's chapter-house, a fork that meets at the Chapel of
> Embers; the Sunless River into the Grey Breach, left by the falls); the Wake on the island opens the singing door (the winch gate's
> successor) to the Deep Way and the Rail Hall. Notes marked **(S7)** below are the polish built 2026-10-15 (ADIT_REWORK section 21).

```
 THE SHATTERED PEAKS
   Adit yard (outside: Sandthrone pickets, spoil heap, quest givers near by)
        │
   [1] THE OLD WORKINGS  ── timbered drifts, ore carts, cages of pressed goblins
        │  boss: Gang-Boss Haddo Lusk (+2 guards)
        ▼
   [2] THE SINGING GALLERY ── a great natural cave the miners broke into; wind sings in the vents
        │   spirit stone · echo-jars · three ways on, each marked by its own light
   ┌────┼──────────────┬────────────────┐
   ▼    (amber/violet) ▼  (red)         ▼  (grey)
 [3a] GEODE FLOOR   [3b] EMBER VENT   [3c] THE GREY BREACH
  cutters, sorters   Ash cult, lava    unmade drifts, grey beasts
  boss: Nix and      boss: Cinder-     boss: The Foreman
  the Rock-Eater     Warden Ysolt      Who Forgot
   └── each drops a RAIL SIGIL ──┴───────────────┘
        │  three sigils open the winch gate
        ▼
   [4] THE RAIL HALL  ── the reveal: a vast cavern, the old mag-rail, an ore carriage on the old line, mule-hauled (0.05), being loaded with geodes
        gate: quiet (a freed goblin unlocks it) or loud (blasting powder: the hall comes for you)
        boss: Quartermaster Brannigan Sorrel (stuns, weapon rack)
        event: the Weaver's unlocking (escort and waves)
        boss: Rail-Captain Orsk Danner on the carriage (guards from the shadows)
        hidden: Old Kettle, the hall's cook (drop off the platform's far side)
        rare: the Quiet Miner (Grey Breach, one run in five)
        way out: ride the carriage back up the line to the Adit yard
```

### [0] The Adit yard (outside, on the Peaks)
- What exists now: the Sealed Adit's face, the yard, the spoil heap, ore nodes, the Signal-tower pickets.
- What changes: the seal is broken open. Sandthrone pickets (level 10) stand on the yard, with a cart track running inside.
- The quest givers are near by (section 6).
- The door is a dark cut with lum-tube green light inside.

### [1] The Old Workings (wing 1, the learning wing)
- Timbered drifts, rusted ladders, ore carts on bent rails, dripping water, green lum-tubes.
- Trash: Sandthrone diggers (non-elite, in 2s), overseers (elite, with whips), sappers (throw a charge you must step out of).
- **Pressed goblins** sit in cages. They are not hostile. The quest "The Pressed" frees them, and freed goblins help later.
- **Boss: Gang-Boss Haddo Lusk** (11, elite). He hits hard and slow and has a lot of armour, with two guards.
  - If you wait for the guards to walk off, you can pull him alone (the first boss's lesson).
  - When he dies, **a patrol of three walks in from the door behind** (patrols after bosses).

### [2] The Singing Gallery (the hub)
- A great natural cave the miners broke into. Wind through the high vents makes it **sing**, rising and falling. The
  Listening Shrine on the Peaks above is where pilgrims come to hear it (a tie to CANON-EXPANDED).
- The **spirit stone** is here. The **echo-jars** for the pilgrim's quest are here too.
- **Three ways on, each lit its own colour, with a sign:**
  - amber and violet: the geode floor;
  - red: the vent;
  - grey: the breach.
- It branches but is not a maze: you can always see the hub's light behind you.
- Mobs: cave-bats that **call for help** and Sandthrone carriers moving geodes between the branches (a patrol that walks the
  hub).

### [3a] The Geode Floor (Sandthrone and goblins: the workshop-and-foundry rooms)
- Cutting benches, sorting troughs and crates of glowing violet geodes. Faces seem to move inside them.
- Trash:
  - cutters (elite);
  - goblin turncoats who throw spanners (knockdown);
  - ~~a **rock-borer** engine that the goblins repair if you kill it before them~~ (no machines, 0.05).
- **Boss: Nix and the Rock-Eater** (11, elite). Two-part fight:
  - first the Rock-Eater, a great burrowing beast Nix rides (it was a goblin boring-engine until 2026-10-11, 0.05);
  - when it breaks, Nix, the goblin who sold out his own Warren (a Razzle-like traitor; GAME-ONLY), jumps out and fights on
    foot, vibration-locking your weapon (disarm for 4 s).
- Drops the **Amber Sigil**.
- **(S7) The Geode Foundry's swing-loads** (Chris, 2026-10-15: "Yes, add them"; GAME-ONLY): three ore buckets on ropes from blocks
  lashed under timber tie-beams, swinging across the high catwalk (two) and the cross-walk (one), hauled out on tag lines by rope teams
  on the hall floor (pressed goblins and a Sandthrone hand). Cross between swings; a bucket that meets you knocks you back along the
  walk, never off it, with no damage. Rope and people only: no wheel, winch or crane (0.05).

### [3b] The Ember Vent (the Cult of Ash: the lava chasm)
- A hot fissure off the workings. A path runs round a lake of molten rock, with ash in the air and red light.
- The Ash cell feeds geodes to the fire, because they think the memories burning in them are an offering.
- Trash:
  - Ash initiates in groups of 2-3 (one heals, one **casts fear**: plan the pull);
  - ember elementals (non-elite, pulled 1-2).
- **Boss: Cinder-Warden Ysolt** (11, elite).
  - **Ember Blow:** an uppercut that **knocks the tank back** (into the lava if you stand at the edge, so fight her in the
    middle).
  - **Flame Ring:** a burst round her, the same knockback idea.
- Optional: the **Vent-Hound** (11, elite), which wanders the far ledge.
- Drops the **Ember Sigil**.

### [3c] The Grey Breach (the Wasting: the corrupted cave)
- The eastern drifts end where the void has already eaten the rock:
  - grey static instead of stone, and edges that flicker;
  - silence where the Gallery sings;
  - one patch of floor that is not there.
- Trash: **greyed** cave beasts (bats, crawlers, a bear), unmade at the edges:
  - they **flee at low health** and bring more;
  - a grey caster puts one of you **to sleep** (interrupt it).
- **Boss: The Foreman Who Forgot** (11, elite). A Sandthrone foreman half unmade.
  - He **blinks** in and out.
  - At half health he **forgets** his target (aggro reset).
  - He **terrifies** at 25%.
- **Rare (one run in five): the Quiet Miner**, an older, wholly grey shape who walks the deepest drift.
- Drops the **Grey Sigil**.
- Killing the Foreman opens a **short way back** to the Gallery (the waterfall-shortcut lesson).
- **(S7) The Grey Spreads** (GAME-ONLY): three lines across the Breach. Crossing one greys the lamps behind you, shows the static there
  and lets a seep of two crawlers out 25 m behind you. The Foreman's death stops it, and the lamps come back a little. (The short way
  back is built in the rework: the sluice-boards open on his death, and the falls drop into the Gallery's plunge pool.)

### [4] The Rail Hall (the finale)
- **The winch gate** (the cage-lift until 2026-10-11, 0.05) is wound open by hand when the three sigils are set in its frame.
- **The reveal:** you step out into the biggest space in the game:
  - an old station cut into the mountain's root, high and dark;
  - the mag-rail line running off into the black both ways;
  - on it, an **armoured rail-carriage** loaded with crates of glowing geodes, Sandthrone loading it from a platform;
  - steam and lamps, and the sound of the Gallery far above.
- **The gate onto the platform:**
  - **quiet:** a freed goblin (from "The Pressed") vibration-locks it open, and the hall does not know you are there;
  - **loud:** blasting powder from a crate in the side gallery blows it open, and the Quartermaster sends a wave at you.
- **Boss: Quartermaster Brannigan Sorrel** (12, elite).
  - **Two guards jump you** from the platform's sides as he comes down.
  - **At 2/3 and 1/3 health he stamps:** the whole party is stunned for 2 s while he goes to his weapon rack, and comes back
    with a heavier weapon (more damage each time).
- **The event: the Weaver's unlocking.**
  - The freed goblin elder, **Mother Quillet** (GAME-ONLY), must reach the carriage's resonance lock and match it. Matched,
    the geodes go dark (their memories let go) and the carriage cannot run.
  - You escort her down the platform. **Two waves** come: Sandthrone from the carriage, then dockers from the far tunnel.
  - **The surprise:** with the lock half matched, the carriage roof opens and the last boss stands up.
- **Last boss: Rail-Captain Orsk Danner** (12, elite, `harder`), fought on the carriage roof.
  - **When he is attacked, two guards step out of the shadows. At half health, two more.**
  - **Kill the guards and he calls new ones** (so burn him down).
  - He carries **a sealed letter** addressed to the Sandthrone's commander (CANON; named only), which starts the next chain.
- **Hidden boss: Old Kettle** (13, elite), the hall's cook, below the platform's far side. Drop down to find him. He throws
  the pot.
  - *In the rework he is a pitch-boiler in the Pitch Pit off the Rail Hall's north-west corner (a chalked tally by the platform points
    to him). (S7) His pitch holds 2 s every 20 (was 3 every 18), so it seldom holds you inside his Hot Pitch ring.*
- **The way out:** with the lock matched, the goblins set the carriage to run **back up the line** to a siding under the Adit
  yard. One ride takes you out.

## 5. Mobs (all GAME-ONLY)

| Where | Mobs | Behaviour |
|---|---|---|
| Yard | Sandthrone picket, picket hound | ranged first, then melee |
| Workings | digger (non-elite), overseer, sapper | sapper's charge: step out of the circle |
| Gallery | cave-bat, geode carrier (patrol) | bats call for help |
| Geode Floor | cutter, goblin turncoat, rock-borer | spanner knockdown; borer repaired if left |
| Ember Vent | Ash initiate, Ash mender, ember elemental | fear; heal; pulls of 2-3 |
| Grey Breach | greyed bat, crawler, greyed bear, grey whisperer | flee at low health; sleep |
| Rail Hall | platform guard, docker, carriage gunner | waves; the gunner fires at range from the roof |

## 6. Quests (given outside, in the Peaks and Khaven)

1. **Under the Toll** (the Pass-trader, Peaks).
   - "The toll's too low for what they spend. Where's their money from?"
   - Kill Gang-Boss Lusk and bring his tally book.
2. **The Pressed** (Pib, an escaped goblin hiding at the pilgrims' rest).
   - Open 5 cages in the Workings.
   - This makes the quiet gate and the escort possible.
3. **Echoes in the Stone** (a Silent Pilgrim at the Listening Shrine).
   - Bring 6 echo-jars from the Gallery.
   - The jars play a line of a lost era when you loot them.
4. **Embers Below** (Khaven's Gallows-keeper, who has seen Ash marks on the road).
   - Kill Cinder-Warden Ysolt and bring her brand.
   - A tie to the Ash Rim, the next zone up.
5. **What the Grey Takes** (a Salt-Mender scholar on the Peaks; the faction is CANON, the person GAME-ONLY).
   - Bring a shard of unmade stone from the Foreman Who Forgot.
6. **The Dead Line** (the Pass-trader, after 1).
   - Stop the carriage: the Weaver's unlocking, then Orsk Danner.
   - Choice of three rare pieces.
7. **A Letter Under Seal** (dropped by Danner).
   - Leads to the next dungeon (later).
8. **The Weaver's Lock** (Pib, after "The Pressed"; added with D4).
   - Walk Mother Quillet down the platform to the carriage's lock and keep the Company off her while she matches it.

## 7. Loot

- **The Adit-Runner's set** (rare, 5 pieces):
  - one from each sigil boss;
  - gloves from the Quartermaster;
  - the chest from Danner.
  - Set bonus at 3 and 5 pieces.
- Each boss also has 2-3 pieces of its own:
  - the Rock-Eater's **Borer Bit** (a weapon);
  - Ysolt's **Ember-Warden's Brand** (an off-hand);
  - the Foreman's **Grey Lantern** (it makes things look washed out);
  - the Quartermaster's **three weapons** (one drops: the one he swapped to last);
  - Danner's **Rail-Captain's Coat**.
- **Geode Shards** drop from the geode floor's trash, a crafting material for the Blacksmith.
- The hidden and rare bosses each have one better piece.

## 8. Atmosphere

- **Sound:**
  - the Gallery sings (wind in the vents) and is heard faintly everywhere;
  - the Breach is silent, with a low hum;
  - the Vent roars;
  - the Hall has steam and clanks.
- **Light:** each part has its own colour, so you always know where you are:
  - Workings: green lum-tubes;
  - Geode Floor: violet;
  - Vent: red;
  - Breach: grey with no shadows;
  - Hall: cold blue lamps.
- **Readable layout:** every branch has one landmark you can see from the hub, and the hub's light behind you.
- **(S7) The living lichen:** a faint glowing trail on the floor along the run's dark ways (the dark drift, where no lamp is; the falls
  stair; the Breach; the cloister stair home; the shaft stair), drawn out toward the way on, giving out for a stride round the rusted key.
- **(S7) The Breach goes grey behind you** as you cross its lines (above), and the Foundry's buckets creak across the catwalk.

## 9. What has to be built (build work, in order)

1. **Cavern variant 3** (the Hollow cavern builder): the Workings, the Gallery hub with three branches, the winch gate's stair and
   the Rail Hall as one cavern system under the Peaks' Sealed Adit. Lights by part.
2. **Boss mechanics:**
   - phase triggers at health thresholds (stun the party, a weapon change, summon guards, aggro reset, terrify, knockback);
   - a two-part fight (beast, then rider). *Built 2026-10-09 (D5); since 2026-10-11 (0.05) the Rock-Eater is a great burrowing beast on
     the bear's body (stone hide, geode crystal through its back, a saddle), a second elite of Nix's camp; Nix rides in the saddle, seen but
     out of reach, and leaps down when it falls (EncounterSession.RockEater.cs, ActorLook.RockEater, ActorVisual.RockEaterGear).*
3. **Dungeon objects:** *Done 2026-10-09 (D3): the cage-lift is a gate at the stair's head (Chris; the winch gate, wound by hand, since 2026-10-11), the sigils are kept once
   won, the platform gate opens quietly (The Pressed) or loudly (powder), the spirit stone, the Quiet Miner (one visit in five);
   the Foreman's short way back to the Gallery is not built yet.*
   - gates (opened by a key, a freed NPC, or powder, the loud way alerting a boss);
   - sigils in a frame;
   - the winch gate (was the cage-lift);
   - patrols released when a boss dies;
   - a rare spawn chance;
   - a spirit stone.
4. **The escort event:** an NPC who walks, waves on a timer, a boss who rises at a step. *Built 2026-10-09 (D4): Mother Quillet waits by
   the platform gate after the quiet way in; spoken to, she walks to the carriage's resonance lock (one of your party for every mob,
   Mira and the sims), platform guards come at a third of the way and dockers at two thirds, Danner stands up out of the dark with the
   lock half matched; matched, the geodes go dark and "adit.lock" is kept; Pib's "The Weaver's Lock" (EncounterSession.Weaver.cs,
   AditWeaver). The carriage roof, a goblin figure and the sims joining the escort on their own wait.*
5. **Mob behaviours:** call for help, flee at low health, sleep, fear, knockback, a charge to step out of. *Calls, the sapper's blast,
   heals, bolts, drains and knockbacks came with C5 and D2; flee at low health (`ZoneCamp.flee`, the Breach's crawlers) and the gunners'
   and pickets' ranged shots 2026-10-09 (D5). The Foreman's terrify stands for a mob's fear; a sleep on you waits for a mob that sings it.*
6. **Quests and loot:** the seven quests, the set and boss tables. *Written 2026-10-08 on web/adit-quests-loot (Quests/adit.json, the
   Adit-Runner's Kit in loot.adit.json); quest gear is handed out at turn-in since 2026-10-09 (QuestLog.GearRewards; QuestGearTests passed 2026-10-11), and the Geode Shards and the hidden and rare
   bosses' pieces wait (WEB_LOG.md).*
7. **Sim support:** the dungeon run leads through the wings and branches in order, waits at gates, and joins the escort. *Built
   2026-10-09 (D7): the order came with round 27; the run now keeps the camps past a gate, walks to a shut one and waits, saying what it
   wants (SimCompanion.TickGate), and goes on when it opens; every party sim guards Mother Quillet while she walks.*
8. **The rail ride out.** *Built 2026-10-09 (D8): E at the carriage's gangway once the lock is matched, five seconds on the bar, and you,
   Mira and the sims stand at the siding under the yard (EncounterSession.Adit.RideCarriage). The carriage does not move on screen yet.*

Biggest risk: step 1 (the largest cavern by far) and the knockback into lava (it must never put anyone under the world).

**(S7, 2026-10-15; ADIT_REWORK section 21; made and compiled, not yet run in Unity):** the Grey Spreads; the swing-loads; the lichen
built from the zone's crumbs; every secret paying something worth finding (two Adit-Runner pieces, a Last-light, the lamp-men's mixing
card, more coin); the Old Burrower and Old Kettle eased; `/dungeon quick|full` named in /help; the deck rails, posts and bodies standing
where they belong again (they had stood at the root since 2026-10-14); a cave's face straddling a raised mouth's or lip's passage cut.

# The three later dungeons: the rules they share (2026-10-14)

The Sludge-Tunnels, the Deep Veins and the Transit Hub were designed on the same day by different hands, and the first drafts
repeated each other and broke section 0 in the same ways. These rules settle that once, for all three. Each design below keeps
to them and says so where it matters. Section 0 governs over this; where a rule here reads section 0, section 0 wins.

## Status
- All three are designs only. The level cap is 15 (EncounterProgress.LevelCap), and Quests.cs refuses a quest whose minLevel is
  over the cap. Each is built when the cap passes its band (the Sludge-Tunnels at 20, the Deep Veins past 22, the Hub past 29).
- None waits for a host zone. Each puts its quest givers in its own zone's open ground (the Outfall, the Bread Drop, the
  Half-Light), so it can be built and tested alone. They move into the host zones (the Forge District and the Rust Market, the
  Warrens, the Lowtowns and the Crown) when those are built.

## Run length (section 0: "a 45-60 minute run like the full Deadmines")
- **A full clear of each is 45-60 game minutes.** 0.06's 60-75 is the Sealed Adit's own figure (Chris set it for the Adit, after
  its scale-up); it is not the bar for these three.
- **The pace to plan with is the Adit's measured one:** 66-67 camps in 71-78 game minutes on the 2026-10-13 group runs
  (CLAUDE_HANDOFF, the Adit group run), about **1.1 minutes a camp**, with fights, bosses, events and walking included. The
  17:37 run's 0.85 a camp was a partial run and is not used.
- So each dungeon has **about 52-54 camps in all**, counting its optional, hidden and rare ones (54 × 1.1 = 59 minutes), against
  the Adit's 67. Its main way is about 40-48 minutes.
- **"Not just bigger but more too do!"** is read as density, not a camp count: **something new at least every five minutes** (a
  choice, a thing to use, a new kind of enemy, a view, an event or a boss). Fewer camps than the Adit is not "less to do".
- Each design gives its own pace table and a trim order for when a group run comes out over 60.

## A party of five (section 0: "5-player group instances")
- Five go in: you and four others. With Mira, that is you, Mira and three sims; without her, you and four sims.
- **The code does not hold this yet.** MaxPartySims is 4 and counts sims only (EncounterSession.Party.cs l.15); Mira is apart
  from it (Progress.recruited, InviteMira, l.100-104). So today you, Mira and four sims make six. Section 0's "MaxPartySims is 3"
  (l.10) is out of date: it has been 4 since 2026-10-11.
- **Build (with the first of the three):** in a `dungeon:true` zone the party is held to five. The zone line refuses a sixth
  ("Five go in. One of you waits at the door."), and inside one InviteRefusal and InviteMira say "Your party is full" at five
  (PartySims.Count plus Mira if recruited). The run tool takes you and four sims, without Mira, as group_run.ps1 does today.

## Ways, stairs and size
- **Every way the leader walks is at least 10 m wide (5 m half-width).** The Adit's 5 m lanes caused 26 warps to the leader.
  A side way with no camp in or beyond it (the leader never goes in) may be as narrow as 6 m; each design names its few.
- **No stair or slope steeper than 25°.** The navmesh's limit is 34°; 25° leaves room.
- **No chamber wider than the Adit's Rail Hall (85 m wall to wall, 42.5 m half-width) or taller than its 28 m**, until a bigger
  one has been built and its frame time captured. "The biggest space in the game" is said of nothing until then.
- No camp on a ledge, catwalk, column top, stage, beam or crate stack, nor across water, slag or a blocked channel.

## Bosses: every named elite holds the party (0.07)
- **Every named elite** (every boss, mini-boss, optional, hidden and rare) has a hold that reaches past its own target: a stun,
  root, fear or disorient on **everyone within a reach**, or on **a member it names**. A hold "on whoever it fights" alone does
  not count: that holds only the tank.
- No silence on party members anywhere: a silence on Mira mid-fight wipes groups.
- **No single blow over 60% of a member's health** (EncounterEnemy.MaxBlowShare), hazards and stacks included.
- **A hazard never lands on someone held with no way out:** its warning is longer than the longest hold in its room, and it
  never starts while someone in it is held.
- Only each dungeon's last boss is `boss:true`, so the scorecard (0.08) opens on it.

## Loot (0.075)
- **Every named elite in a boss table drops one of its named pieces every time** (the signature group at chance 1), with 20%
  extras. The group's roll decides who takes it, and the scorecard says who did. Nothing named drops "at a chance".

## Spirit stones
- Two in each dungeon. **Each stands at least 20 m from every camp's centre and off every patrol's road**, so a party that wakes
  there does not wake into a fight. The layout script checks it.
- **Build (with the first of the three):**
  - BoundStone returns the first stone touched in the zone's list (EncounterSession.Adit.cs l.214-219). It must return the
    **deepest** touched: each stone's place along the run, from the layout.
  - TouchStone's line says "the Gallery's song" in every zone (l.210). Each zone gets its own line.

## The crowd (frame rate and navmesh)
- **No more than about 40 animated figures in any one view.**
- Standing workers and prisoners get no NavMeshAgent and no collider. Each plays one looped clip, offset in time, and its
  Animator is culled off-screen.
- Walking crowds get agents only while they walk, and are removed where they leave.
- None of them carves the navmesh.
- **A frame-time capture** at each dungeon's busiest view before it is dressed further, and again at its busiest event. If it
  is worse than the Adit's Rail Hall, the design's named crowd is cut first.

## One owner for each faction and set piece
The three must not repeat each other. Each of these belongs to one dungeon only:

| Thing | Belongs to | The others |
|---|---|---|
| The Indentured's collars and tethers of light as something you play (cut a tether, free a collar), and a boss fed by tethers | the Sludge-Tunnels (Ebert Lade, the Stoke-Master) | the Hub's porters wear them, shown only; the Deep Veins' convicts go in irons |
| The Spent (dead Indentured walked by their collars; 0.05, decision 1 there) | the Sludge-Tunnels | none |
| The Iron Teeth | the Sludge-Tunnels | the Deep Veins' Dock is the Iron Pact quartermaster's; the Hub's skim is the Company's |
| A red-brick sewer wing | the Sludge-Tunnels | the Deep Veins begins in a rubble-stone undercroft and shows brick only at the Junction |
| A works-floor riot (Down Tools) | the Sludge-Tunnels | |
| Gas set off by carried fire (b1 ch.9:139) | the Deep Veins | the Sludge-Tunnels' ossuary gas is lore only; its tool there is the bone-stacks |
| A gaol rising: cells, an escort, waves at a gate, a captain at half (the Rising) | the Deep Veins | |
| Stone-flesh (the Forgotten, the Half-Bound, the Binder, the Count-Master's binding) | the Deep Veins | |
| The Deep-Rats | the Deep Veins | |
| Spiders and crawlers (the spider model) | the Deep Veins | the Sludge-Tunnels has none |
| Seekers, Inquisitors, Redaction, Ordinance 9 and the Silencers | the Transit Hub | none placed in the other two |
| A record room with a boss (the Ink-House), a great scale, and a hidden door to a side room (the bookcase) | the Transit Hub | the Sludge-Tunnels' house has a pay-hall without scales, and its secret is under the hearth |
| Nila Glass-Stitcher and her Tuning-Shard | the Transit Hub | no Tuning-Shard is a reward in the other two |
| The Sandthrone Company | the Transit Hub | |
| The Cult of Ash (the hostile cult) | the Sludge-Tunnels (the ossuary) | none |
| A commander and a priestess who raises him once | the Sealed Adit (the Ember Vent) | none of the three |

Shared freely: the Iron Pact as gaolers and soldiers, the Council's foremen and overseers, the Salt-Menders, holds, heavy blows,
rallies and calls, and the hazard strip (built with the Sludge-Tunnels).

## Canon rules all three keep
- **Seekers** (the Hub only) are stunned, held and knocked back like anyone. Canon shows a force wave "launching them backward"
  (b1 ch.1:271); they rise at once. "Didn't even stagger" is a Lowtowns rumour (lore_stories #2) and never a game rule.
- **The Copper-Tithe are not enemies anywhere.** The game set them aside as villains because they are sympathetic in canon and
  turn in its climax (HANDOFF_ARCHIVE l.678). The Hub asks Chris whether to use them; until he says yes, no.
- **The Spent** (0.05): one place, one explanation, one label. The Sludge-Tunnels only, and only on Chris's yes (its decision 1).
  The game's reading: the tether's spell holds the body up. Canon's "rudimentary locomotive engine" is never used. Label:
  CANON-EXPANDED (canon's image, b1 ch.12:37-39; the name "the Spent" and the spell are the game's).
- **Canon people as quest givers only on Chris's yes**, and each in one dungeon only. Until he answers, a GAME-ONLY giver
  stands in. Gorm Rock-Wall: the Sludge-Tunnels (its decision 2). Razzle, Rill and Chieftain Grohl: the Deep Veins (its
  question 1). Nila: the Hub (its question 7).
- **"Insufficient"** is the Architect's word (lore_stories #4, "The Architect's Shadow"). It belongs to the Sludge-Tunnels'
  Former Miner and his quest, and to nobody else.
- **Silver dust** does one job: it scatters the readers (b1 ch.3:103). In the Sludge-Tunnels it is the party's notice-halving
  buff. Nowhere is it a weapon.
- **Book 1 is near.** The soldier's boy, taken now at seven, is still seven and still held when Veyra speaks of him
  (b1 ch.7:221-225). So the game's now is shortly before Book 1. No design says Book 1's events are "years off".

## Words and names
- **No machine words in player text** (0.05): engine, machine, mechanism, gear, piston, pump, valve, press, conveyor, pneumatic,
  clockwork, battery, grid, crane, lift. The Hub's people-worked rope rides are called by their names (the Porters' Basket,
  the Low Haul, the Seekers' Cage), never "lift". Canon names with machines in them stay out (the Sludge-Tunnels' name hazards).
- **No World of Warcraft names**, and never **"the Maw"** for anything (the author's maps use it twice: the Sludge-Tunnels' corner
  is "the Outfall", and the Deep Veins' beast is "the Bread-Eater"). The classic models are described, never named, below
  section 0.
- **Lessons, not copies.** No encounter is taken whole from a classic dungeon. Each design lists what it did not take.
- **No name that a game NPC, a sim or a canon person already has**, first name or surname, and no two near-alike across the
  three. Checked on 2026-10-14 against the game's quests, items and figures, SimAdventurers' first names and bynames, and the
  lore in D:\code\crulanda. Taken already, among others: Bram Tolley, Edda Pell, Old Pell, Lisbet Crane, Wenna Coyle, Tam and
  Aldo Crisp, Sister Iselle, Grenna Holt, Wat Corley, Hedda Thorne, Ondine Varro, Siv Harl, Corwin Ashby, Ansel Morrow, Nan
  Pennock, Hob Linden, Mother Quillet, Mother Vane, Silas Vane, Archivist Thorne; the sims' Ansel, Cato, Edric, Fenna, Jory,
  Orla, Quill, Tamsin, Wren and the bynames Ashby, Brook, Dray, Fallow, Hale, Kettle, Rooke, Sallow, Thatch, Wick.
- **Renamed on 2026-10-14** (the drafts' names on the left):

| Dungeon | Was | Now | Why |
|---|---|---|---|
| Sludge | Harl Grue | Leof Grue | Siv Harl |
| Sludge | Edda Varn | Gisla Mott | Edda Pell; near Varro and the Veins' Varrow |
| Sludge | Corvin Lade | Ebert Lade | Corwin Ashby |
| Sludge | Wenna Thatch | Gytha Lamb | Wenna Coyle; Thatch is a sim byname |
| Sludge | Wenlock Dray | Wenlock Sculler | Dray is a sim byname |
| Sludge | Bram Tolle | Abel Ledgard | Bram Tolley |
| Sludge | Hild Marrow | Hild Seaver | near Ansel Morrow and the Veins' Ilda Marrow |
| Sludge | Sexton Wulfric Hale | Sexton Wulfric Dene | Hale is a sim byname |
| Sludge | Sister Ebba Thorne | Sister Ebba Yew | canon Archivist Thorne; Hedda Thorne |
| Sludge | Grandmother Fault (a spider) | Odo Stannard, the Last Foreman | spiders are the Veins'; too many Mothers |
| Sludge | Seeker Unit 9 | the Reader of the Works | Seekers are the Hub's; Silencer Unit 9 |
| Sludge | Osric Tull | cut (no warden rises in Down Tools) | the Rising is the Veins' |
| Veins | Sergeant Hobb Varrow | Sergeant Alger Wyle | Ondine Varro; Hob Linden |
| Veins | Fence-Captain Ilda Marrow | Quartermaster Selwyn Thrupp | Hild Marrow; the Iron Teeth are the Sludge's |
| Veins | Mother Clatter | the Clatterer | too many Mothers |
| Veins | Chief Warder Edda Crane | Chief Warder Hawise Stour | Edda Pell; Lisbet Crane; "crane" (0.055) |
| Veins | Captain Iselde Marr | Captain Brunna Hardwick | Sister Iselle; near Marrow |
| Veins | the Redactor | the Binder, Severin Lisk | the Hub's Redactor Vellum |
| Veins | Overseer Aldous Kemble | Overseer Lucan Kemble | the Hub's Aldous Mordaunt |
| Veins | the Gutter Maw | the Bread-Eater | "the Maw" is a WoW name |
| Veins | the Seeker on the count | Bellman, the Count's Hound | Seekers are the Hub's |
| Veins | Old Scald | cut (the arena is gone) | |
| Veins | Corrie Vane | Corrie Fisk | canon Silas, Madam and Mother Vane |
| Veins | Nan and Tam Hessel | Joan and Kit Breward | Nan Pennock; Tam Crisp; the Sludge's Long Hessick |
| Veins | Pell | Sprat | Edda Pell, Old Pell |
| Veins | Old Fennick | Pottle | near Fenn and the sims' Fenna |
| Veins | Old Mab Tunney | Mab Tunney | too many "Old" |
| Veins | Ottilie Sand | Gerda Holm | the Sludge's Ottilie Grist |
| Hub | Mother Haggitt | Collector Wilmot Gaunt, the Finder | the Copper-Tithe are not enemies; too many Mothers |
| Hub | Sallis Doone | Wace Tuckett | Sallow (a sim byname); near Hesta Dunne |
| Hub | Silencer Unit 9 | Silencer Unit 12 | the Sludge's Seeker Unit 9 |
| Hub | Floor-Marshal Rennick Holt | Floor-Marshal Rennick Prowse | Grenna Holt |
| Hub | Pact-Sergeant Ruel Haskett | Pact-Sergeant Ruel Bannard | the Veins' Haska |
| Hub | Ward-Sister Aldis Fenn | cut (the Adit's priestess pair) | |
| Hub | Sovereign-Warden Aldous Mordaunt | Sovereign-Warden Cassian Mordaunt | the Veins' Aldous Kemble |
| Hub | Bram Corley | Outrider-Captain Arn Feld | Bram Tolley; Wat Corley |
| Hub | Mouse | cut (no captive in the lamp) | |

## Lore labels
One label for each thing in all three: CANON, CANON-EXPANDED (with the canon source named) or GAME-ONLY. Where two designs once
labelled the same thing two ways, the label in this section is the one used.

## The story chain (one thread, four dungeons)
1. **The Sealed Adit.** A Letter Under Seal: "Forty crates this month... to the hub at the coast. The Council's factor there
   weighs them and pays on the nail" (Quests/adit.json). Lisle Tamber wants the factor's name. The factor is at **the coast
   station**, not at the Transit Hub.
2. **The Sludge-Tunnels.** Where the Light Goes (Lisle; requires A Letter Under Seal): the factor sells the geodes on to the
   foundry. Under the Paymaster's hearth, the Peaks account gives his mark and his station ("H.O., the coast station"), not his
   name. The Paymaster's ledger pays "to the factor at the coast, his tenth", and buys "hands of the Veins legion, bought of the
   Count-Master: forty, spent". Lisle (The Ledger's Last Page) sends you to Gerda Holm, the Salt-Menders' woman at the Bread Drop.
3. **The Deep Veins.** The Count-Master sells the legion's convicts to the foundry and is cutting pens for ten thousand. His
   writ: the Veins' geodes go "to the High-Sovereign Transit Hub for sorting", and "berths for ten thousand, ready by the year's
   turn". Gerda Holm's A Writ for the Hub leads to the Hub's quest 11.
4. **The Transit Hub.** The Factor's Name (Lisle; requires Where the Light Goes): the Weigh-House ledger names Factor Hamlin Orr
   of the coast station. Berths for Ten Thousand (Gerda Holm; requires the Deep Veins' The Count-Master): the Census Vault holds
   the order the writ answers, "for the Spire's intake", and no more. The Writ of Carriage opens the Spire.

Each step is a side quest. A player who skips a dungeon misses its link and can still take every other quest in the next.

## Quests are given outside
The pattern (QUEST_DESIGN, The Sealed Adit): a dungeon's quests are given and turned in outside. Pib gives The Weaver's Lock
outside; Mother Quillet gives no quest. Two exceptions, each made on purpose and said where it stands: the Sludge-Tunnels'
quest 11 can be turned in to Dace Kessock on the Sunk Quay, and the Hub's Scrivet gives his quest inside the Catalogers' Way.

## Chris's brief of 2026-10-14, about 01:30 (verbatim; recorded in 0.4)
"i think adit dungeon need to be more complex like wow's wailing caves or the deadmine. adit is nothing like those". It is said
of the Adit, and it sets the bar for these three: each is complex like the models, not one long route of camps. Each design
checks itself against it (branching, loops, levels, choices, set pieces) and against the brief's three models (section 0: the
mine with the ship at the bottom, the cave of the four lords, the zealots' house), as the Adit was.

# The Sludge-Tunnels (levels 18-20, the mid band; designed 2026-10-14, built when the level cap rises past 15)

**Status: design only, PROVISIONAL until Chris confirms.** The level cap is 15 (EncounterProgress.cs l.100). **This dungeon is built when the cap reaches 20.** The bosses and the quests stand at 20, so a cap of 18 is not enough. It does not wait for another zone: its givers stand on the Outfall, the dungeon zone's own open ground, as the Deep Veins' stand on the Bread Drop and the Hub's in the Half-Light. ROADMAP puts this dungeon in the Forge District (19-22, l.79 and l.83); the Outfall lies at the foot of that district's wall, where the Rust Market's lanes (the Lowtowns, 16-19) come down to the river. When either zone is built, the givers move up into it and the zone gets its exit there. Every number below is a target. The layout script and the group runs settle it. The rules the three later dungeons share are in the section above (run length, a party of five, ways and size, holds, loot, spirit stones, the crowd, owners, names, the story chain).

**Three things for Chris to decide first (short):**
1. **The Spent.** These are dead Indentured whose collars still walk them. They are canon (b1 ch.12:37-39), but the novels call the collar "a rudimentary locomotive engine", which is a machine (0.05). The game would make it the tether's spell holding the body up. Keep them, or swap them for living gaolers? They are in this dungeon only: the Deep Veins and the Hub have none (shared rules).
2. **Gorm Rock-Wall as a giver.** He is CANON: Valen's orc partner, alive before Book 1 (b1 ch.1:21). He would give The Soul Tax on the Outfall. Yes, or a GAME-ONLY orc docker? (Until you answer: the docker.) Gorm gives quests in this dungeon only. **Answered (Chris, 2026-10-15 ~22:12, the Lowtowns question prompt): "Yes, Gorm himself (Recommended)".**
3. **Orcs among the freed and the lightermen?** Canon says the Council sees orcs, goblins and ogres as "disposable manual labor" (b1 ch.16:353). Some freed and some lightermen would use the orc figures. That needs a line of code for orc non-player figures.

## 1. Where it is and what it is (lore)

| Thing | Label | Source |
|---|---|---|
| The sludge-tunnels, "the city's primary waste management system": the Lowtowns' red-brick sewers, waded chest-deep, "two miserable miles... completely underground" | CANON | b1 ch.9:7, 29, 41 |
| The Forge District: walled, smoke-black, "the most heavily secure square mile in this entire accursed city"; layered wards; "a full legion of Seekers"; snipers on the upper gantries | CANON (its War-Golems dropped, 0.05; no Seeker is placed here: Seekers are the Hub's, shared rules) | b1 ch.7:239, ch.12:51 |
| The district's drainage outfall at the foot of its wall: a "heavily restricted drainage output pipe" that "vents every six hours to clear the industrialised sludge", climbable from inside | CANON | b1 ch.7:299 |
| The outfall's corner of the wall. The author's map labels it "The Maw"; the game calls it **the Outfall** (see the name hazards below) | CANON place, GAME-ONLY name | maps\the_forge_district_map_1771814038095.png |
| The Iron Pact Wall and the Foundry Gates | CANON (map names) | the same map |
| The Chemical River: iridescent, oily, tastes of vinegar; dead fifty years, since the manufactories opened; "The chemical runoff interferes with magical tracking. We're invisible to the Seekers" | CANON | b1 ch.16:5, 75, 85 |
| The old river's bed under the wall, and the riverside quarter walled over when the wall went out over the old bank | CANON-EXPANDED | from b1 ch.16:85 |
| River smugglers. Horgus, an orc captain on the dead river, "quietly run[s] smuggled cargo" | CANON (Horgus is never shown or named in player text: his barge burns a furnace, 0.05) | b1 ch.15:341; ch.16:337, 353 |
| **The lightermen:** smugglers in Horgus's trade who pole flat lighters along the buried bed by hand | GAME-ONLY name; CANON-EXPANDED trade | this doc |
| Methane in the sewers and the ossuary: "one single, accidental spark from your heavy armor"; the Council sealed the ossuary over it | CANON (lore only here: the gas is played in the Deep Veins, shared rules) | b1 ch.9:139; ch.10:15 |
| Silver dust, smeared on windows "to rapidly scatter the Inquisitor's resonance readers" | CANON | b1 ch.3:103 |
| The resonance readers | CANON (the readers); **the Reader of the Works**, who walks the Crucible Hall with the Pact: CANON-EXPANDED | b1 ch.3:103 |
| The Bone Cathedral: red-brick sewers smashed through the lowest ossuary levels of "the incredibly old Cathedral district". A vault of skulls cemented in grey mortar, with femur ribbing and pelvis rosettes. Sealed fifty years ago over "leaking shadows" | CANON | b1 ch.10:3-35 |
| The Cult of Ash nesting in the ossuary. They hang in the vault and drop; bone masks wired with rusting iron; "The eternal Void hungers for the light!" | CANON (hostile; the game splits it from the allied Ash-Walkers, WORLD_ZONES l.235) | b1 ch.10:39-65 |
| The Chimney of the World: the intake shaft, iron rungs in black stone, scalding steam, the air of burning copper | CANON | b1 ch.11:5 |
| The Foundry: "a city of fire and motion, built inside the hollowed-out chest of the mountain"; diamond-mesh catwalks; stacks of cooling iron crates | CANON (its golems, whistle, press and lift dropped, 0.05) | b1 ch.12:5-71 |
| The Great Crucible, the Fire Pits and the Slag Rivers; ladles of molten metal hung on chains | CANON (the author's maps; their gears dropped) | maps\forge_district_map.png |
| The Indentured: debtors and dissidents who could not pay the Council's Soul Tax. Grey clothes, Arcane Tethers of light, iron collars that drain their strength | CANON (played here only: shared rules) | b1 ch.12:19-27 |
| The collar that walks a dead man on: "He is already dead on his feet". **The Spent** | CANON-EXPANDED (canon's image; the name and the tether's spell are the game's; decision 1) | b1 ch.12:37-39 |
| A freed collar starts a riot on the floor | CANON (the beat; this riot, **Down Tools**, is GAME-ONLY) | b1 ch.12 |
| Foremen in oil-stained robes | CANON-EXPANDED (canon's Artificers, b1 ch.12:87; their golem work dropped) | b1 ch.12 |
| The Iron Pact as the foundry's gaolers | CANON-EXPANDED (the Iron Pact is canon; here they stand in for canon's Overseer Golems) | b1 ch.2:131 |
| Luminous blue coal, "raw, unrefined aetheric ore", shovelled by hand | CANON | b1 ch.16:171, 333 |
| Iron "eats the weave" | CANON | b1 ch.12:59 |
| The Iron Teeth: "a brutal crime syndicate dealing in black-market aether". Their jagged sign in yellow ochre; their fortified warehouse in the Rust Market (neutral ground up there); their bounties | CANON (this dungeon's villains; the other two have none, shared rules) | B1WB:26; b1 ch.7:21; lore_stories #8 |
| The Former Miner: "I worked the mines in sector 4". A man in noble's robes drained a crystal "the size of a wagon" black, said "Insufficient", and two days later the mine collapsed | CANON (left unnamed, as canon leaves him; the tale's title names the Architect, and the game never does. "Insufficient" is said in this dungeon only) | lore_stories #4 |
| **The Old Crystal Working:** that collapsed mine lies under the foundry, which was built out over it; its black crystal is still there; the Drained still walk it, and its last foreman with them | GAME-ONLY (placement and reading) | this doc |
| The Rust Market is Sector 4, at the Forge District's walls | CANON | b1 ch.3:7 |
| Gorm Rock-Wall: an orc, Lowtown muscle, Valen's partner, alive in the game's era | CANON | b1 ch.1:21; lore_stories #2 |
| Shards, "spent magical batteries used by the poor"; aether-geodes are fossilised memory and soul-residue; the Deep Veins' raw ore goes to the Forge District | CANON | world_bible; WB:101; b1 ch.3:133-137 |
| Weave-Eaters: the Lowtown mothers' "shadowy spirits born from the friction of the High City's industry". Steel that passes through them turns black and brittle; thrown salt calcifies them into white crystal; bone harpoons on amber ropes pin them | CANON | B1WB:121; b1 ch.20:111-203 |
| Acid reclamation vats where what falls in becomes "tomorrow's brick mortar" | CANON (a flavour line only; the vats are not built) | b1 ch.9:45 |
| Lowtown names are short, with Anglo-Saxon or Germanic roots | CANON (the naming rule used for every new name) | B1WB:133 |
| That the crucibles leave memory-light in their slag; that the night foremen sluice it down the drains on purpose; that the Iron Teeth sift it out in the sewers and sell it in the Rust Market as cheap shards | GAME-ONLY | this doc |
| **The Counting-House:** the foundry's old pay-office in its foundations, empty since the Indentured came and wages stopped, quietly let to the Iron Teeth by the night foremen | GAME-ONLY | this doc |
| The Outfall's landing, the Toshers' Cistern, the Weirs, the Old River cut, the Sunk Quay, the Catacombs' Charnel and Wiring-Room, the Sifting Beds, the Stoke-Hole, the Tether-Rows, the Casting Floor and the Cooling Stacks | GAME-ONLY (the ossuary, the shaft, the crucible and the cooling crates are CANON) | this doc |
| Every person, boss, mob, item and quest named below, unless marked otherwise | GAME-ONLY | this doc |

**The story in one line: the foundry's waste is not waste.**
- The Adit's geodes go to the factor at the coast station (Danner's letter: "to the hub at the coast. The Council's factor there weighs them", Quests/adit.json). The factor sells them on to the Forge District, and the Deep Veins' raw ore comes to it too, to be smelted for the Spire.
- The night foremen sluice the crucibles' slag down the drains while it still glows with memory.
- The Iron Teeth sift it out in the sewers and sell it to the Lowtowns as cheap shards. The Lowtowns burn their own dead for light, a shard at a time.
- The foremen are paid in the Iron Teeth's counting-house under the foundry floor.
- The Paymaster's ledger says who else is paid:
  - the Count-Master of the Deep Veins, who sells the foundry convicts from the penal legion ("hands of the Veins legion, bought of the Count-Master: forty, spent");
  - and a tenth "to the factor at the coast", the factor Danner's letter names.
- The Count-Master is the hook to the next dungeon, the Deep Veins. The factor's thread runs on to the Transit Hub, where his name is found (shared rules: the story chain).

**Kept out on purpose:**
- The game is set before Book 1, and near it (shared rules). The Spoke stands unfinished beyond the haze and is never shown, and no canon person dies here.
- Kara is not placed here, because Book 1 frees her on the Foundry floor.
- **Seekers, Inquisitors, Redaction, Ordinance 9 and the Silencers** are the Transit Hub's. None is placed here. The Crucible Hall's round is a reader and the Pact.
- **The Copper-Tithe** are not villains anywhere in this dungeon, nor in the other two. They turn in canon's climax, and the game rejected them as villains once already (HANDOFF_ARCHIVE l.678: "sympathetic in canon").
- **The gas and the Deep-Rats** (b1 ch.9) are the Deep Veins'. **Nila and her Tuning-Shards** are the Hub's.
- Silas Vane, Valen and the Architect appear nowhere. The Former Miner's robed man stays unnamed.
- Horgus is never shown, because his barge has a furnace.
- Nothing is a machine (0.05). Section 4.3 gives every translation.

**Name hazards.** These are canon names that collide with World of Warcraft or break the no-machines rule. They never appear in player text:
- "The Maw" (the map's name for the outfall corner) is a WoW zone. The game says **the Outfall**, and never says "the Maw" for anything, in any of the three dungeons.
- "Blacksmoke Spire" (the map's tallest stack) is too close to a WoW dungeon. The game says **the chimney stacks**.
- "Houndmaster" is a WoW boss title. Never use it; if a kennel-keeper is ever wanted, say **Kennel-Keeper**.
- "the Blue Anduin" is the Chemical River's old name (b1 ch.16:85). Never use it. Say "the old river".
- Never name the classic models in player text, nor in any section below section 0 (the city gaol, the tinkers' city and the others are described, never named). This design's riot is **Down Tools**; the Deep Veins' gaol event is the Rising.
- Canon names with machines in them: "Gear-Grinder", "Fizzle-Bang", "the Mule", "the Plaza of Gears".
- Canon's golems, gears, whistles, presses, drop hammers, lifts, bellows and conveyors are all dropped (0.05, and 4.3 below).

## 2. What we took from each classic dungeon (lessons only: no names, layouts or assets)

**From 0.4's first model, the short city run (the gaol riot under the capital):**
- **A dungeon in the city's own gut**, entered from the street, with many givers above it. Here the Outfall lies below the Rust Market's lanes, and the givers and the bounty board stand on its landing.
- **A short, complete run:** the Outfall and the Sludge-Tunnels make a 13-minute run with its own boss (Leof Grue), a gate, a rare spawn and its own way out (the toshers' ladder).
- **A bounty board of named heads** posted for the stall-holders ("Heads for the Board").
- **A riot:** Down Tools, on the works floor. It has no cells, no escort and no waves: the gaol, its cells and its escort are the Deep Veins' (shared rules).
- **Packs of people in tight brick runs, men stepping out of side doors:** the drain-cutters leaping from the culverts.
- Its weakness was being too short to be worth the walk. Here the short run is the first quarter of the whole, and the whole is a full evening.

**From 0.4's second model, the long built halls (the fallen tinkers' city):**
- **Long built halls round a great central hall you see from above:** the Works. You climb out of the Chimney onto the Gallery, and the whole Crucible Hall lies below you.
- **The place's own people turned against it:** the Indentured, the Spent, and the river people's quarter buried under the works (the Sunk Quay).
- **Something in the air that is bad for you:** the Chimney's steam, the Catacombs' bone-dust, and the Spent's grey blow.
- **Things you would rather not meet on the big floor:** the Reader of the Works on the gaolers' round.
- **A back door for a returning group:** the toshers' ladder, and the flue ladder.
- **Tokens that open doors:** the sump key and the Stoke-Master's pay-tally.
- **A last boss who sold out her own:** the Paymaster is a Lowtowner who sells the Lowtowns their own dead.
- Its weakness was that it was too long, people got lost, and the halls were all alike. Here:
  - each hall has one job and one look;
  - every way is lit its own colour;
  - there are two spirit stones and two ways out;
  - the main line never doubles back.
- Its halls were machine halls. Ours are worked by hand: shovels, poles, chains, windlasses, buckets and collared people (0.05).

**From the mine with the ship at the bottom (as in the Adit):**
- The story is told outside first.
- **A real mine:** the Old Crystal Working, a dead crystal mine with a wagon-sized black crystal, timber sets and coal seams. This gives the brief's "mine tunnels" literally.
- The reveal is the Crucible Hall.
- A gate opened by doing something in the world: the shift bell (quiet) or the great ladle (loud).
- Each boss teaches one thing, and the first boss can be pulled clean (Leof Grue).
- Patrols walk in after bosses.
- The last boss carries the paper that starts the next chain.
- There is a hidden boss (Odo Stannard, the Last Foreman, dug out with the old miner's pick), a rare spawn (Old Tosh) and a set across the dungeon.
- The classic's weakness was no repairs or vendors inside. Here the lightermen's fence mends and trades on the Sunk Quay.

**From the cave of the four lords:**
- An approach outside with its own mobs (the Outfall).
- Side wings in any order: the Catacombs and the Sifting Beds off the hub; the Tether-Rows and the Old Working off the Works.
- Wounded mobs that bolt for help: rakers and bookmen.
- Corrupted beasts: the slag-fed swine with violet eyes.
- An optional boss off the path that opens a short way back (the Flue-Eater's ladder).
- Its weakness was a maze. Here every branch is obvious from its hub by its light.
- **Not taken here:** the escort with waves and a surprise boss. The Deep Veins' ram-gang and the Hub's Wren have it; this dungeon's freeing is a riot instead.

**From the zealots' house:**
- **A stronghold of stone with rooms you know from one look:** the Paying-Yard, the Pay-Hall and the Strongroom. The ossuary counts too.
- Packs that heal each other: the Charnel's ash mender and the house's sawbones.
- Wings that are each a short run to their own end boss.
- **A finale pair:** the Counter, who pays the house's guards in, and the Paymaster. (Not the Adit's commander and priestess: nobody here raises anybody.)
- **Not taken here:** the hidden door to a side chamber. That is the Hub's bookcase. This house's secret is under the hearth.

**What this dungeon adds that the Adit has not:**
- **Timed ground you must leave:** the flush, the steam vents and the Pour. The bone-stacks are the same kind of ground, but you tip them yourself.
- Things you throw at a boss: salt and harpoons.
- **Freeing workers in the middle of a fight to weaken the boss:** the stokers' tethers. It is the one tether fight in the three later dungeons (shared rules).
- **One death that changes a whole floor:** when the Stoke-Master falls, or the sixth tether is cut, every Spent left in the Works lies down.
- **A riot that changes the floor and costs you a door:** Down Tools sets the gangs on their drivers and stops the Pour, but the house hears it, and the quiet door is shut that visit. This holds for the visit only.
- **The careful way, three times over:**
  - lamps that show the hanging cult, so you can pull it a pack at a time;
  - runoff or silver dust, so the reader's round walks past you;
  - the shift bell, so you walk into the house unseen.
- **A pursuit:** the nest follows you up the Chimney.
- **Neutral ground inside:** the lightermen, who trade unless struck.
- **A quest with two turn-ins for one item**, where taking one closes the other.
- **A run that changes temperature:** cold brick, then bone, then white heat, then dead black crystal, then cold stone and gold.

**The brief's "mix" of places** (0: "a mix means the places as well as the mechanics"):
- **Tunnels:** the brick sewers, the Chimney's flue, the Sifting Beds' cut rock, and the Old Working's timbered drifts. The red-brick sewer wing is this dungeon's alone among the three (shared rules).
- **Built halls:** the ossuary, the crypts, the Works and the Tether-Rows.
- **A stone stronghold:** the Counting-House.
- No two wings in a row look alike.

**Chris's brief of 2026-10-14, checked** (0.4: "more complex like wow's wailing caves or the deadmine"; complex, not one long route of camps):
- **Branching:** two hubs. The Bone Cathedral has two wings off it; the Crucible Hall has three ways off it (the Stoke-Hole, the Tether-Rows, the Old Working). A side area off the first wing (the Sunk Quay) and a spur off the climb (the old flue).
- **Loops:** the toshers' ladder (out at the Cistern, and the back way in); the Flue-Eater's ladder down to the hub's stone; the coin-chute out at the end.
- **Levels:** the Weirs' three steps down; the Prior's crypt sunk in the Charnel's floor; the Chimney's 22 m climb; the Gallery 6 m over the Crucible Hall; the stair down to the house.
- **Choices that change the run:** the lamps or the drop; the quay struck or left in peace; Down Tools or not (the Pour against the quiet door); the quiet or the loud house door; the seal to the fence or to the chandler.
- **Set pieces:** laying the plank-way under thrown slag; the Nest; the pursuit; the reveal from the Gallery; the collars going dark down the hall; Down Tools; the Pour; the books in the stove.

## 3. Level and length

- **Mid band (15-25).** Levels by stretch:
  - trash is 18 at the Outfall and in the sewers up to the Old River cut;
  - 19 from the cut through the Cistern, the hub, its wings, the Sunk Quay and the Chimney;
  - 20 in the Works, the Tether-Rows, the Old Working and the Counting-House;
  - the wing bosses are 19: Leof Grue, Corse, Senna, the Prior, Gisla Mott, Long Hessick;
  - the Works' bosses are 20: Ebert Lade, Hagan Brack, the Reader of the Works, and the Flue-Eater;
  - the last boss is 20, elite and `harder`;
  - the hidden boss is 21, and the rare 19.
- **The cap is 15, so this is designed now and built when the cap reaches 20.** That one number limits:
  - quest minLevel (Quests.cs l.128);
  - the sims' levels and the talents;
  - the balance targets, which are set for 15;
  - the run tool's fixed level 11 (RunCapture.cs l.32, 49).
- **Size:** about 1.55 km of passage and halls, a little more than the Adit's 1.35 km.
  - The main way is 785 m.
  - The branches add 765 m:
    - the Sunk Quay 160;
    - the Catacombs 120;
    - the Sifting Beds 140;
    - the old flue 50;
    - the Stoke-Hole 40;
    - the Tether-Rows 70;
    - the Old Crystal Working 185.
  - There is no branch of a branch. The Prior's crypt is sunk in the Charnel's floor, and the Paymaster's strongbox is under the Strongroom's hearth.
- **Content:**
  - **54 camps and 133 mobs**, counting the rare, the optional and the hidden (the Adit: 67 camps and 148 mobs). The run is held to section 0's 45-60 minutes, so there are fewer camps than the Adit and more to do between them (shared rules: run length).
  - Plus 10 mobs from waves (the Nest 3, the pursuit 7), and 9 neutral lightermen.
  - Six patrols: five of two, and the round of three.
  - 15 named elites.
  - Two spirit stones, two ways out, a back way in, and a shop inside.
- **Time.** At the Adit's measured pace (66-67 camps in 71-78 game minutes on the 2026-10-13 group runs, about 1.1 minutes a camp with the walking, the bosses and the events):

| Run | What | Minutes |
|---|---|---|
| The short run | the Outfall and the Sludge-Tunnels, Leof Grue, out by the toshers' ladder (10 camps) | about 13 |
| The main way | everything on the main line, by the quiet door, with no wings, no Down Tools, no Old Working and no optional camp (33 camps) | about 40 |
| **A full clear** | the main way, the Catacombs, the Sifting Beds, the Tether-Rows and Down Tools, and the Old Working (47 camps; no optional, hidden, rare or neutral camps) | **about 52** |
| Everything | plus Old Tosh, the Sunk Quay's hogs, the Prior, the Flue-Eater, the round, and the Fault with Odo Stannard (54 camps) | about 59 |

- **The counts are the tuning knob.** If a group run's full clear comes out over 60 minutes, take the third mob out of the sewer's and the Works' threes first, then the Bone Cathedral's rim patrol.
- **It need not be done in one go:**
  - spirit stones at the Bone Cathedral's font and at the Gallery's west end (section 4, the ground rules);
  - the toshers' ladder out at the Cistern (the short run's end). Once climbed, it is the back way in;
  - the Flue-Eater's ladder, a short way down from the Chimney to the hub;
  - the coin-chute out at the end.
- **Party:** five, 1 tank, 1 healer, 3 damage or support: you and four others, Mira among them or not (shared rules: a party of five). MaxPartySims is 4 and counts sims only, Mira apart; the dungeon's door holds the party to five. The run tool takes you and four sims.

**The 0.07-0.08 checklist (what the Adit's group runs taught), built in from the start:**
- [ ] **The leader waits for the whole party** (built), and walks to every shut gate and waits there, saying what it wants (SimCompanion.TickGate). The gates are: the plank-way, the grille, the Iron Door and the Fall.
- [ ] **Every named elite holds the party** with a stun, root, fear or disorient on everyone near or on a member it names (the table in 4.4; shared rules). There is no silence on party members anywhere: a silence on Mira mid-fight wipes groups.
- [ ] **No single blow over 60% of a member's health.** This covers:
  - heavy blows;
  - the flush (30%);
  - the vents (25%);
  - the bone-stacks (35%);
  - the Pour (45%);
  - Settle the Account at six stacks of Interest.
- [ ] **A hazard never lands on someone held with no way out.** Each hazard's warning is longer than the longest hold in its room. Gisla, Brack and the Chimney's pursuit never start a hold while a mark is down.
- [ ] **Only the last boss is `boss:true`.** The scorecard (0.08) opens when Ottilie Grist falls.
- [ ] **Every named elite drops one named piece every time** (0.075; signature at chance 1). The group's roll decides who takes it, and the scorecard says who did.
- [ ] **Every camp stands on floor a path reaches.** None on a catwalk, the crucible's frame, a column top, or across a blocked channel or the old river. The far bank's camp, beyond the old river, is reached once the plank-way is laid.
- [ ] **Every way is at least 10 m wide** (the Adit's 5 m lanes caused 26 warps to the leader). There is no walkway split by a channel, and no stair steeper than 25°.
- [ ] **Each spirit stone stands at least 20 m from every camp and off every patrol's road.**
- [ ] **Patrols walk in after bosses:** Grue, Ebert Lade, Brack and the Paymaster.
- [ ] **Held mobs are left alone** (built). Abel Ledgard is holdable for the full time, as Nix is.
- [ ] **A camp nobody can reach is given up after 45 s** (built). The nav tests make sure there is none.
- [ ] **Five in the party, no more** (shared rules).

## 4. The layout

```
 THE OUTFALL'S LANDING (the zone's open ground): the givers, the bounty board
        │  (the zone's exits to the Forge District and the Rust Market's lanes, once those are built)
   [0] THE OUTFALL (outside): the Chemical River, the slag-banks, the Iron Teeth's toll-house, the drain's brick arch
        │
   [1] THE SLUDGE-TUNNELS ── red-brick sewer, one wide walk, the sludge channel along the east wall      0-255 m, down 14 m
        │  an ambush from a culvert · rare: Old Tosh
        │  G1 THE OLD RIVER CUT (110 m): lay the plank-way ──────► [1b] THE SUNK QUAY (west; tallow-yellow):
        │                                                              the lightermen's neutral ground, the fence, the old bank
        │  the Weirs
        │  boss: Leof Grue, the Sump-Boss, in the Toshers' Cistern ── carries the sump key
        │  WAY OUT: the toshers' ladder (the short run ends here; the back way in)
        ▼
   [2] THE BONE CATHEDRAL (the hub) ── skull vault, six lamp-niches, the cult hanging in the dark        255-365 m
        │  event: the Nest (Corse; Into the Vault) · spirit stone
   ┌────┼──────────────────────────────┐
   ▼  (violet)                          ▼ (amber)
 [3a] THE CATACOMBS                  [3b] THE SIFTING BEDS
  niches, bone-stacks, the Charnel     the salt store, the beds, slag-fed swine,
  (the Prior's crypt sunk in it),      the Sluice-House and the flush
  the Wiring-Room                      boss: Gisla Mott, the Sluice-Master
  boss: Senna the Wirewife
        │  G2 the Iron Teeth's grille (the sump key) · the runoff basin              (red)
        ▼
   [4] THE CHIMNEY OF THE WORLD ── the flue stair, the steam vents, the pursuit (Long Hessick)        365-470 m, up 22 m
        │  opt: the old flue: the Flue-Eater (salt and harpoons; then its ladder down to the hub)
        ▼
   [5] THE WORKS ── the Gallery: the reveal · spirit stone                                          470-660 m
        │  the Crucible Hall: the round (the Reader of the Works) · the chain-gang at the Crucible
        │    ├──► the Stoke-Hole (east bay): boss Ebert Lade (cut the tethers; the floor goes quiet)
        │    └──► [5a] THE TETHER-ROWS (west; collar-blue): Gytha Lamb; event: Down Tools (the floor riots)
        │  the Casting Floor and the Cooling Stacks: Hagan Brack, the Pour-Master (no Pour after Down Tools)
        │    └──► [5b] THE OLD CRYSTAL WORKING (east; dead black, pale blue): the black crystal, the Drained;
        │              hidden: dig the Fall with the old pick: Odo Stannard, the Last Foreman
        │  G3 the Iron Door: ring the shift bell with the pay-tally (quiet) or, after Down Tools, tip the great ladle (loud)
        ▼
   [6] THE COUNTING-HOUSE ── the stone stronghold, cold and quiet                                    660-785 m, down 8 m
        the Paying-Yard · the Pay-Hall · the Strongroom: the Counter and the Paymaster (the ledger, the stove)
        secret: under the hearth-stone
        WAY OUT: the coin-chute down to the Outfall
```

**The shape:**
- One main line with a hub two-fifths of the way in.
- A side area early on: the neutral quay.
- Two side wings off the hub, and a short optional spur in the climb.
- A second hub, the Crucible Hall, with three ways off it: the Stoke-Hole, the Tether-Rows, and the Old Working beyond the Casting Floor.
- A stronghold at the end.

**The zone and the plan:**
- **The zone:** zone.sludge, `dungeon:true`, about 1,000 m square, levels 18-20. Until the Forge District or the Lowtowns exist, the world map's travel lands you on the Outfall's landing. Then the landing gets a zone exit to each: east through the toll-house yard's gate to the Forge District (ROADMAP's host), and west up the Rust Market's lanes.
- **The land over the caves** (heights are relative to the Outfall's arch floor, 0):
  - The Outfall is the zone's south edge.
  - The Iron Pact Wall is a cliff 24 m high, and the district's ground behind it stands at +24.
  - That ground rises north to a mount about 60 m high and 450 m across, over the Works, the Old Working and the Counting-House.
  - Roofs against the land:
    - the highest roof is the Crucible Hall's, at +28, with 32 m of ground over it;
    - the hub's is +12, under +24;
    - the Sunk Quay's is +8. It runs at least 30 m north of the wall's foot, so it stays under 16 m of the district's ground and never near the Outfall's open ground.
- **The main way:**
  - runs north under the wall (the sewers);
  - goes through the hub;
  - climbs north-east up the Chimney;
  - runs north through the Works;
  - turns north-west into the Counting-House.
- **The branches:**
  - the Sunk Quay runs west along the old river's bed from G1;
  - the Catacombs run west from the hub, about 160 m north of the quay;
  - the Sifting Beds run east from the hub;
  - the old flue runs east off the Chimney, at least 60 m north of the Sifting Beds;
  - the Stoke-Hole is a bay east off the Crucible Hall;
  - the Tether-Rows run west off the Crucible Hall;
  - the Old Working runs east off the Casting Floor, 85 m north of the Stoke-Hole, which is only 40 m deep.
- **Nothing crosses in plan view** (Hollow is 2D). The layout script settles the coordinates.

**Sizes along the main way** (floor heights relative to the arch; widths are whole widths):

| Stretch | Along | Width | Height | Floor |
|---|---|---|---|---|
| The Throat | 0-40 m | 12 → 16 m | 8 m | 0 → -1 |
| The Long Walk | 40-110 m | 16 m: one walk 12 m wide, the channel 3 m along the east wall behind a 1 m kerb | 8.5 m | -1 → -3 |
| G1, the Old River cut | 110-125 m | 16 m; the old river's bed crosses it 14 m wide; the plank-way 10 m wide | 9 m | -3 |
| The Weirs | 125-170 m | 14 m | 9 m | -3 → -11 (three steps of 2.7 m, each a 6 m stair at 24°) |
| The Toshers' Cistern | 170-230 m | 40 m round | 14 m | -11 |
| The Breach | 230-255 m | 12 m | 7 m | -11 → -14 |
| The Bone Cathedral | 255-365 m | 24 → 60 → 40 m | 20 → 28 m | -14 → -16 |
| The Chimney's foot (G2, the basin) | 365-380 m | 14 m | 9 m | -16 |
| The Chimney | 380-470 m | 12 m | 12 m | -16 → +6 (flights at up to 20°; landings at 400-410 and 440-450 m) |
| The Gallery (the Crucible Hall's south lip) | 470-485 m | 84 m (the hall) | 22 m above the lip | +6 |
| The Gallery stair | 485-500 m | 30 m | | +6 → 0 (22°) |
| The Crucible Hall's floor | 500-600 m | 84 m | 28 m | 0 |
| The Casting Floor | 600-660 m | 56 m, with the Cooling Stacks' bay (30 × 20 m) on its west side | 20 m | 0 → -1 |
| The stair down, G3 at its foot | 660-675 m | 12 m | 8 m | -1 → -8 (25°) |
| The Paying-Yard | 675-705 m | 40 m | 12 m | -8 |
| The Pay-Hall | 705-745 m | 44 m | 16 m | -8 |
| The strong passage | 745-755 m | 12 m | 7 m | -8 |
| The Strongroom | 755-785 m | 28 m | 10 m | -8 |

**The branches:**

| Branch | Opens at | Length and parts | Width | Height | Floor |
|---|---|---|---|---|---|
| [1b] The Sunk Quay (west) | G1's near bank, 112 m | 160 m: the Bank Walk 0-40, the Quay 40-120, the Sump End 120-160 | 22 m (a bank 12 m wide beside the old bed's 10 m of black water); the Quay 44 m; the Sump End 20 m | 7; 14; 9 m | -3 → -6 |
| [3a] The Catacombs (west) | 280 m | 120 m: the Niche Walk 0-45, the Charnel 45-85, the passage 85-95, the Wiring-Room 95-120 | corridors 10 m; the Charnel 32 m, with the Prior's crypt sunk in its south end (16 × 12 m, 4 m down a stair 10 m wide); the Wiring-Room 34 m | 7-12 m | -16 → -26 (the crypt -30) |
| [3b] The Sifting Beds (east) | 310 m | 140 m: the Salt Store (a side room at 15 m), the Beds 25-85, the Sluice-House 90-140 | 14 m; the Salt Store 16 m; the Sluice-House 36 m | 7-12 m | -16 → -10 |
| The old flue (east) | 425 m | 50 m | 10 m | 8 m | level with its mouth (about -6) |
| The Stoke-Hole (a bay, east) | 545 m | 40 m deep | 44 m | 18 m | 0 |
| [5a] The Tether-Rows (west) | 565 m | 70 m: the Rows 0-45, the tally-yard 45-70 | 14 m; the yard 24 m | 7 m | 0 |
| [5b] The Old Crystal Working (east) | 630 m | 185 m: the Old Head 0-50, the Coal Seams 50-95, the Black Crystal 95-148, the Fall at 150, the Fault 150-185 | 12; 32; 44; 28 m | 7; 10; 18; 12 m | -1 → -13 |

**Ground rules from the Adit's runs** (and the shared rules):
- Every way is at least 10 m wide, and no stair is steeper than 25°.
- **The sludge channel never splits a sewer into lanes.** It runs along one wall, and the walk beside it is 12 m wide.
- The only crossing of water on the main line is G1, and it is 10 m wide.
- No camp stands on a catwalk, the crucible's frame, a column top, or across water or slag.
- **No chamber is bigger than the Adit's Rail Hall.** The Crucible Hall is 84 m wide and 28 m tall, as wide and as tall as the Rail Hall, not more.
- **The two spirit stones** (the font, 310 m; the Gallery's west end, 478 m) each stand at least 20 m from every camp and off every patrol's road.

### [0] The Outfall (outside, the dungeon zone's open ground; trash 18)
- **Where:** the corner of the Forge District's wall where its drain empties into the Chemical River. The approach is about 160 × 120 m of the zone's south end. The Rust Market's lanes come down to it from the west.
- **The landing (the givers):** a row of tarred-timber shanties and net-sheds on the slag-bank at the Outfall's west end, where the lanes come down, at least 60 m from the toll-house. The givers stand here as residents (`life.residents`: fixed posts, no sleep), as Oakhaven's Quill does. The stall-holders' notice board ("Heads for the Board") is nailed to the end shed. When the Rust Market or the Forge District is built, the givers move up into it.
- **The river:** carved water, oily, iridescent and murky, as Khaven's Gloom Creek is murky. Nothing lives in it. It is shallow at the bank, so you can wade.
- **The ground and the wall:**
  - black slag-banks;
  - the Iron Pact Wall as a black cliff 24 m high, with a wall line and two towers on top (outdoor kinds). Nobody stands on top: no fights on roofs;
  - the chimney stacks smoking behind it;
  - the drain's brick arch, 14 m wide and 10 m high, at the cliff's foot.
- **The sludge-fall:** every few minutes a fall of grey sludge pours from the arch. This is the Sifting Beds' flush coming out, the gangs' work deep inside. It is particles only, with no damage outside.
- **The toll-house:** the Iron Teeth's toll-house of tarred timber against the cliff, with their jagged ochre sign painted on its roof (CANON look).
- Crows on the slag (the Living Birds; critters).
- **Camps:**
  - O1, the toll-watch at the arch: a knee-breaker (elite) and two runners, 18, social call;
  - O2, the slag-bank swine: three sewer swine, 18, pack.
- **Packs:** 2 camps, 6 mobs.

### [1] The Sludge-Tunnels (wing 1, the city run; trash 18-19): 255 m, down 14 m
**Built as:**
- The cave builder with a **new red-brick lining**: brick courses on the shell. The rings' smoothing makes a round vault, and the Village kit's brick arches stand at side culverts and weirs.
- **An egg-section sewer 16 m wide and 8-9 m to the crown.**
- **The sludge channel**, 3 m wide, runs along the east wall behind a 1 m brick kerb. It is blocked like the Adit's lava lake: a glossy green-black strip with glints of slag-light and floating scum, doing no damage.
- **The walk** is the whole west side, 12 m wide.
- Dark side culverts open every 15 m on the west wall: brick arches 3 m high, all decor but the ambush culvert.
- **Light:** sickly green from the channel, and amber from the Iron Teeth's hooded lanterns on wall hooks.
- **Smell line on entering** (canon): hot copper and long-dead river fish.

**Stretches:**
- **The Throat (0-40 m):** the arch narrows into the main sewer. The toll-watch at the arch outside is its guard.
- **The Long Walk (40-110 m):** the longest straight.
  - S1, the culvert ambush: two drain-cutters leap from a side culvert at 50 m (`ambush`), 18.
  - S2, the first rakers: three rakers, 18, at 62 m. They flee at 25% and call.
  - S3, the swine bay: a side bay 16 m across at 78 m, with three sewer swine, 18. One run in five, **Old Tosh** wallows here (rare).
  - S4, the slag-lighters: two slag-lighters and a runner, 18-19, at 98 m. They stand in culvert mouths and throw lit slag down the walk (the blast cast).
- **G1, the Old River cut (110-125 m):**
  - The sewer crosses the buried bed of the old river (CANON-EXPANDED). It is a cut 14 m wide of slow black water running wall to wall, blocked and carved. It is **the only way on**: there is no other crossing.
  - The Iron Teeth's **plank-way** is a staging of planks 10 m wide on two ropes. The Teeth haul it up against the far bank at night to keep their road their own.
  - Lay it at the near windlass with E and a 5 s bar, by your own hands, while S5 on the far bank throws slag.
  - It is a ZoneGate with a plank-way's look: shut, it blocks the crossing; laid, it is the way. The leader walks to it and waits for you.
  - S5, the far bank: a knee-breaker (elite), a raker and a slag-lighter, 19, at 128 m.
  - **The Sunk Quay** opens west along the near bank, so you can reach it without laying the plank.
- **The Weirs (125-170 m):** the floor steps down three times beside sludge-falls.
  - S6, the lantern-men: two runners walking 125-225 m (a patrol).
  - Secret, **"Road-marks in the brick"** (note; CANON-EXPANDED, b1 ch.9:41): a thief's careful scratches in the brick at 140 m, the kind a man cuts on his way out from the Watch. They are Valen's, though the note never names him.
- **The Toshers' Cistern (170-230 m):**
  - A round brick cistern 40 m across and 14 m tall, ribbed in brick.
  - The sump is a blocked disc 12 m across in the middle, with a 14 m rim all round.
  - Sifting tables, racks of rakes and tally-boards stand round the rim.
  - S7, the cistern sifters: three rakers, 19, on the south-west rim at 185 m. They flee and call.
  - S8, Leof Grue and his two hookmen (non-elite), on the north rim at 210 m.
  - **The toshers' ladder:** a rusted iron ladder up a brick shaft in the west wall at 195 m (the image is CANON, b1 ch.11:5).
    - E and a 5 s bar takes the party up into the toll-house yard at the Outfall.
    - Climbed once, it works from the top too: the back way in (key `sludge.ladder`).
    - **The short run ends here.**
  - Secret, **"A tosher's cache"**: behind a loose brick, coin and a twist of silver dust (CANON material; one use of the buff).
- **The Breach (230-255 m):** the red brick is smashed through into old grey stone (the CANON image), and the skulls begin. No camp: the hub is in sight.

**Boss: Leof Grue, the Sump-Boss** (19, elite; Iron Teeth). He holds the sewer road.
- **His lesson** (the first boss's): his hookmen stand with him, but the sifters bolt to him when hurt. Pull the sifters away first, or they bring him.
- **Sump-Rake:** a heavy blow with a windup and a mark. Step out of it.
- **The hook:** root for 3 s on everyone within 4 m, every 15 s ("he sweeps the rake low").
- At half he bellows for his rakers: a call within 20 m.
- When he dies, the lantern-men (S6) walk in from the Weirs if they still live (deathCall, reach 60 m).
- He carries **the sump key** for G2, kept for good.

**Packs:** 8 camps and the rare, 23 mobs.

### [1b] The Sunk Quay (side area west: neutral ground; tallow-yellow light; 19)
**Lore:**
- The river people's quarter, on the old bank. They lived on the river before the manufactories opened (b1 ch.16:85).
- Their lanes were walled over when the Forge District's wall went out over the old bank (CANON-EXPANDED).
- **The lightermen** keep the old quay now. They follow Horgus's trade, the canon river smugglers (b1 ch.16:337).
  - They pole flat lighters by hand along the buried bed, out under the wall to the Chemical River.
  - They carry what the Iron Teeth won't touch, and pay the Teeth's toll at the plank-way.
  - They hate the Teeth, and they keep the peace.

**Built as:**
- A stone river stair down from the bank.
- House-fronts from the Village kit, half sunk in grey sludge, their doors still painted with family names.
- Net-racks, upturned boats (the Megapack Boat), and the old river wall with mooring rings.
- Three flat lighters (the Boat, lengthened) moored on the old bed's black water, which is blocked and carved.
- Tallow lamps on poles.
- No furnace, no engine: poles and ropes only.

**Camps:**
- L1, the lamp-watch: three lightermen, 19, neutral, at 55 m.
- L2, the fence's stall: **Dace Kessock**, the fence, and two lightermen, neutral, at 85 m.
- L3, the Lighter-Master's lighter: **Wenlock Sculler** (19, elite, neutral) and two lightermen, at 105 m.
- Q1, the Sump End: three sludge-hogs (the Wild Boar in a mire tint), 19, hostile, at 140 m. They are solitary and charge.

**Neutral ground** (new: a `neutral` camp):
- The lightermen do not fight unless one of them is struck.
- Then the whole quay fights, and Kessock's shop shuts for good (key `sludge.quay`). On later visits the quay is just three camps.
- The leader passes them by, and they do not count against "cleared".
- **Kessock** buys loot, mends gear, sells silver dust (CANON; the notice-halving buff, section 4.1), and takes the tally-seal (quest 11). That turn-in is inside the dungeon: the one exception, on purpose, to the "given outside" pattern (shared rules), because the fence never leaves his quay.

**If you fight: Wenlock Sculler** (19, elite; optional):
- **A flash-pot** (CANON, b1 ch.7:283): disorient 3 s on everyone within 6 m, every 20 s.
- **His pole-hook:** a heavy blow that knocks back 4 m, never off walkable ground and never into the water.
- He calls the quay.

**Secret:** a fisher family's name-board nailed over a sunk door (note). On it, the river's dead year.

**Packs:** 1 hostile camp and 3 mobs, plus 3 neutral camps and 9 figures.

### [2] The Bone Cathedral (the hub; trash 19): 110 m, up to 60 m wide and 28 m to the vault
**Built as:**
- The kit's walls in two courses round the chamber, in a **new bone finish**: skulls cemented in grey mortar.
- The kit's columns in bone, as ribs every 7 m rising into the vault. A pelvis-rosette prop at the crossings.
- Above them, the shell's lining is skull-and-mortar too. The floor is old flags, with a broken font in the middle (310 m).
- **Six lamp-niches** of the old ossuary stand round the nave, three each side, at 280, 310 and 340 m.
- **Light:** bone-white from candle clusters set in the skulls' eyes, an ash-grey haze, and black between. Gold where you have lit a lamp.
- **Smell line** (canon): old dust and dry marrow.

**The ways on, each lit its own colour:**
- the Breach (south): green behind you;
- the Catacombs (west, 280 m): violet;
- the Sifting Beds (east, 310 m): amber;
- the Chimney's foot (north, 365 m): red.

**Camps:**
- B1, the rim pair: two wire-masks walking the hub's edge, 265-355 m, along the walls (a patrol; its road keeps 25 m and more from the font).
- B2, the grille-watch: a knee-breaker (elite) and two runners, 19, at G2 (360 m).
- N1-N4, **the hanging cult**: four packs of three nest-hangers, 19. They hang hidden in the vault over the nave's sides at 285 and 335 m, left and right, each more than 30 m from the font.
- N5, **Corse**, hidden in the crown over the font.

**The lamps (a thing to use; new: per visit):**
- Lighting a lamp takes E and 2 s.
- A lit lamp shows what hangs in the vault above its side. Each pack hangs between two lamps, and either one shows it.
- A shown pack can be pulled on its own: it drops on whoever strikes it, or on the first of the party to stand under it.
- So a careful party can clear the nest a pack at a time before the font. This is the quiet way.
- The lamps gutter out when the camps come back.
- The sims light no lamps (yours to work), and leave hanging packs alone until shown.

**The event: the Nest.**
- When the first of the party reaches the font, the vault drops.
- **Wave 1:** three nest-hangers fall round the font. They are the event's own, made as the Weaver's waves are: fresh mobs copying a camp's look and level.
- **Wave 2**, when those are down: **Corse, the Hanging Brother** (19, elite) drops.
- **The spirit stone** is the broken font's base. Touch it when the vault is quiet. Once Corse is down, no camp stands within 20 m of it.

**Boss: Corse, the Hanging Brother** (19, elite; Cult of Ash).
- **His lesson:** light the lamps and clear the nest first, or fight it all at once.
- **His cry:** fear for 3 s on everyone within 6 m, every 18 s, as he cries the cult's line: "The eternal Void hungers for the light!" (CANON).
- **Hook-Knife:** a heavy blow with a windup and a mark.
- **Into the Vault, at 2/3 and 1/3** (from the duo's HideInside, reused without a mount):
  - he climbs the bone wall into the vault, seen but out of reach;
  - every hanging pack still in the vault drops (a call on hidden camps);
  - when what came down is dead, he drops back;
  - at 1/3 he comes back enraged;
  - **with the nest already cleared by lamplight, nothing drops and he comes straight back down.**
- He drops the Hanging Brother's Mask.

**Packs:** 7 camps and 18 mobs, plus 3 from the wave.

### [3a] The Catacombs (branch west; the old Cathedral district's ossuary levels; 19): 120 m, down 10 m
**Built as:**
- Crypt corridors 10 m wide and 7 m tall: the kit's walls in three courses, lined with rows of bone niches (the kit's Bones and Bones2).
- The corridors join two chambers.
- **Light:** violet-blue grave candles, and the cult's low ember braziers.
- **A haze of bone-dust** hangs where the cult works and sorts.
- The ossuary's old gas is a line of lore only ("the cult keeps its braziers low; the Council sealed this place over something worse than the dead", b1 ch.10:15). Gas you set off is the Deep Veins' (shared rules).

**The bone-stacks** (a thing to use; the hazard strip set off by your own hand):
- Two stacks of sorted skulls and long bones, each 6 m high against a wall: one at 45 m beside C1, one in the Charnel's north end beside C2.
- **Tip one:** E, 1 s. A 3 s creak and a sift of dust (the warning), then it falls along a marked lane 10 m long and 4 m wide.
- **The fall:** 35% of max health and a 2 s knockdown to everything in the lane, the cult and the party alike (under the 60% rule).
- So pull a pack into the lane, step out, and tip it. Or tip it onto C1 before you pull.
- Tipped, a stack is spent until the camps come back.
- The sims step out of the mark, as they do from a heavy blow's. The leader tips none (yours to work).
- The cult keeps its stacks; it is your idea to use them.

**Stretches:**
- **The Niche Walk (0-45 m):**
  - C1, the stack-watch: two wire-masks and an ash chanter, 19, standing in the first stack's lane at 45 m.
- **The Charnel (45-85 m):** a chamber 32 m across and 10 m tall, where the cult sorts bones for its masks. Work tables heaped with skulls and coils of rusty wire.
  - C2: two wire-masks and an ash mender, 19, at 70 m. The mender heals the others: kill it first (the zealots' lesson).
  - Secret, **"In the name of the First Kin"** (note): an inscription on a lintel (the CANON oath, b1 ch.4:63).
- **The Prior's crypt:** sunk in the Charnel's south end, 16 × 12 m, 4 m down a stair 10 m wide. It is part of the Charnel's chamber, not a branch.
  - C3: **the Prior Under the Floor** (19, elite, optional) and two ossuary dead.
- **The Wiring-Room (95-120 m):** a chamber 34 m across and 12 m tall, with niches round its walls.
  - In each niche stands a **wired thrall**: bones wired together with rusting iron, the cult's craft. These use the Toon RTS undead, which any skeleton-look camp named "thrall" wears.
  - C4, Senna's alcoves: four wired thralls, 19. They stand still until she calls them.
  - C5, Senna the Wirewife.

**Boss: Senna the Wirewife** (19, elite; Cult of Ash). She makes the cult's masks.
- **Her lesson:** clear the room, or fight all of it.
- **The wire:** root for 4 s on everyone within 6 m, every 14 s (the built hold).
- At 2/3 she calls the thralls out of the niches (a call). At 1/3 she calls again for any that are left (rally).
- She drops the Wirewife's Hood (the set's head).

**Optional: the Prior Under the Floor** (19, elite). The old cathedral's prior, dead these fifty years, who will not lie down.
- **His hand-bell:** disorient for 3 s on everyone within 8 m, every 20 s ("the bell tolls for the hour you missed").
- A heavy blow.
- He falls into a heap of bones.

**Packs:** 5 camps and 14 mobs.

### [3b] The Sifting Beds (branch east; the Iron Teeth's works; 19): 140 m, up 6 m
**Built as:**
- Rough-cut rock that the Iron Teeth cut, with the Adit's timber sets, 14 m wide and 7 m tall.
- **The slag-race** runs down one side: a blocked strip 3 m wide where the foundry's waste comes down, glowing amber with slag-light.
- Sifting frames, buckets, barrows, and tally-boards chalked with weights.
- **Light:** amber.

**Stretches:**
- **The Salt Store:** a side room at 15 m, 16 m across, with sacks of grey salt to the roof. The Iron Teeth salt their flues to keep out the things born of the smoke (canon salt). Its door is barred from the passage side; lift the bar (E) and walk in.
  - **Take a fistful of salt** (E). It has 3 uses, comes back, and is the Flue-Eater's thing to use.
- **The Beds (25-85 m):** low beds where the waste is spread to be raked.
  - F1, the sifters: three rakers, 19, at 40 m. They flee and call.
  - F2, slag-fed swine: three sewer swine with violet eyes, fat on the glowing waste (the corrupted-beast lesson), 19, at 62 m.
  - F3, the barrow-men: two runners walking 20-90 m (a patrol).
  - **Slag-light shards** for Agna's quest lie here in crates. Each whispers a line of someone's memory when looted, as the Adit's echo-jars do, and the crates come back.
- **The Sluice-House (90-140 m):** 50 m long, 36 m wide and 12 m tall.
  - At the far end, two timber sluice-boards stand over the slag-race's head. Each is raised by a windlass that a gang turns by hand, in the Adit's winch-gate look.
  - The middle 20 m of the floor is the low bed the flush sweeps.
  - F4, the east sluice-gang: two sluice-hands, 19.
  - F5, the west sluice-gang: two sluice-hands, 19.
  - F6, Gisla Mott.

**Boss: Gisla Mott, the Sluice-Master** (19, elite; Iron Teeth).
- **Her lesson: the flush.**
  - While either gang lives at its windlass, every 30 s the boards go up: "The sluice-boards groan" (3 s of warning).
  - Then the flush runs down the low bed: 30% of max health and a 1 s knockdown on anyone in the band.
  - At half: "All of it!" While a gang lives, the flush comes every 15 s.
  - Either cross the bed and kill the gangs first, or fight her at the room's ends between flushes.
- **Her gaff:** she sweeps it round: stun for 2 s on everyone within 5 m, every 16 s. She never uses it while the boards are groaning, nor on anyone standing in the bed.
- When she dies, any living gang drops its windlass and bolts.

**Packs:** 6 camps and 13 mobs.

### [4] The Chimney of the World (the way on; 19-20): 105 m, up 22 m
**Built as:**
- A great round flue of black brick banded with iron, 12 m wide and 12 m tall. Its floor is a stair cut in its side, with a rope rail and iron-shod treads.
- Two landings break the climb, at 400-410 m and 440-450 m.
- Side flues breathe scalding steam across the stair (CANON: "scalding steam"). The steam is the furnaces' own heat rising through the flues: there are no fans, and nothing pushes it.
- One rung hangs by a single bolt (b1 ch.11).
- **Light:** red, with an orange glow growing above.
- **Smell line** (canon): burning copper.

**Along the climb:**
- **G2, the Iron Teeth's grille at the foot (365 m).** It opens with the sump key. The Iron Teeth grilled their own road.
- **The runoff basin** (370 m; a thing to use; CANON mechanic, b1 ch.16:75):
  - The Works' drain-water runs down a gutter in the Chimney's side into a stone basin at its foot. It is the Chemical River's own filth.
  - Smear it on (E, 2 s): you and your party are **hidden from the readers** for 20 minutes. Every mob's notice of you is halved (section 4.1).
  - It stinks, and the text says so.
- **The steam vents:**
  - Three side flues, at 390, 420 and 455 m, each breathe across 4 m of the stair every 12 s.
  - Each burst is shown 2 s before by a rising hiss of white. It does 25% of max health and no hold.
  - Time your way past. The landings are clear of the vents.
- **The pursuit** (new: waves on the party's progress, built on the escort's wave code). "The nest comes up after you."
  - When the first of the party steps onto the first landing (400 m), three nest-hangers come up the stair from the foot.
  - At the second landing (440 m), three more come with **Long Hessick, the Hanging Man** (19, elite), an old nest-elder.
  - It fires once a visit, whether or not the hub's nest was cleared. What follows you is what was hanging in the flue.
  - **The lesson:** fight on the landing with your back to the climb.
- H1, the slag-carriers: a night foreman and an Iron Teeth runner, 20, walking the night's sweepings down the flue to the grille and coming back with coin (a patrol, 380-465 m). The two factions are seen together here.

**Long Hessick, the Hanging Man** (19, elite; Cult of Ash; the pursuit's end):
- **The drop:** every 16 s he leaps onto the landing among the party: stun for 2 s on everyone within 4 m of where he lands. He never leaps while a vent is hissing over the landing's edge.
- A heavy blow.

**The old flue** (a branch east at 425 m; 50 m long; dead and soot-choked):
- At its mouth the Iron Teeth left a salt line (3 uses) and a rack of bone harpoons on amber ropes (CANON tools).
- H2, the Flue-Eater.

**Optional boss: the Flue-Eater** (20, elite; a Weave-Eater, CANON, born of the city's smoke as the Lowtown mothers say).
- **Its lesson:** things to use.
- **It drifts:** it steps behind its target every 12 s (blink).
- **It blackens steel:** whoever strikes it with steel has brittle armour, -25% for 10 s, stacking to two.
- **It drinks the weave:** disorient for 2 s on everyone within 6 m, every 20 s.
- **Weapons do 60% less unless it is salted:**
  - **Throw a fistful of salt** and it goes white: stunned for 4 s, and taking half again as much for 6 s.
  - **Throw a bone harpoon** from the rack and it is pinned for 8 s: rooted, so it cannot drift.
- When it dies, its body calcifies into white crystal (CANON). A soot-black ladder at the flue's end is then clear.
  - That ladder is **the short way down to the hub's spirit stone** (E and a work bar; key `sludge.flue`): the way-back lesson.

**Packs:** 2 camps and 3 mobs, plus 7 from the pursuit.

### [5] The Works: under the Great Crucible (the reveal; 20): 190 m
**Built as:**
- The kit's walls four courses high (8 m) in a black-iron finish, with columns every 8 m and soot-black rock above.
- Catwalks of diamond-mesh round the walls at 10 m and 20 m, **for the eye only**.
- Heat-haze and sparks.
- **Light:** white-hot orange, with the collars' blue points moving in lines.
- **Smell line** (canon): sulphur and burning copper.
- Mira says once that the iron is so thick here she can hardly hear the weave (b1 ch.12:59). Flavour only.
- Nothing here is a machine (4.3).

**The Gallery (470-485 m): the reveal.**
- You climb out of the Chimney onto a stone lip 6 m above the hall's floor, and the whole Crucible Hall lies below you. It is as wide and tall as the Adit's Rail Hall, the biggest built (shared rules: nothing bigger until it is built and its frame time captured).
- At its far end the Crucible hangs in its frame. The chain-gang's blue collar-line runs through the haze.
- W1, the gallery guard: two Iron Pact gaolers, 20, at 475 m, in the middle of the lip.
- **Spirit stone:** at the Gallery's west end (478 m, 35 m from the middle), more than 20 m from the gallery guard and before the stair; the round's road starts at the stair's foot (500 m), below and beyond it.
- Secret (vista), **"The first sight of the Crucible"**, at the lip.
- A stair 30 m wide goes down to the floor (485-500 m).

**The Crucible Hall (500-600 m; 84 m wide, 28 m tall):**
- **The Great Crucible:** a pot of iron-banded fireclay the size of a house. It hangs in chains from a frame of black timber and iron 24 m high at the hall's north end.
  - **The chain-gang**, 24 Indentured, tips it with poles. They are figures, not targets (see the crowd budget, 4.2).
  - Its pour-lip runs into a stone race down to the Casting Floor's ladles.
- **Slag rivers** cross the floor in blocked channels (the lava lake's rule: blocked, no damage), with three stone bridges 12 m wide.
- Ladles hang still on their chains overhead (decor).
- **Camps:**
  - W2, the gaolers' post: three Iron Pact gaolers, 20, at 535 m, 15 m right of the middle bridge.
  - **W3, the round: the Reader of the Works** (a Concord resonance reader, 20, elite) **with an Iron Pact serjeant (elite) and a gaoler** (a patrol, optional). They walk the hall's middle line from the stair's foot to the Crucible's foot and back (500-590 m), over the middle bridge.
  - W4, the north rows: three Spent, 20, at 575 m, 25 m left.
  - W5, the chain-gang drivers: two foremen and a gantry-shot, 20, at the Crucible's foot (590 m, 15 m left). The gantry-shot has a crossbow and closes in between shots, as built.
- **Every camp stands at least 12 m off the round's road.**

**The round, and the careful way past it:**
- **Unhidden**, the round sees you at 8 m as any mob does. It joins whatever fight is nearest its road, or meets you on the middle bridge.
- **Hidden** (the runoff basin, or silver dust from the fence or the tosher's cache), its notice is 4 m. Stand at a bridge's edge or by a column and let it walk past. Silver dust is canon's own answer to the readers (b1 ch.3:103).
- **Fought:** three mobs with two elites. It is a hard pull, and nothing on the main way needs it.
- **The Reader of the Works** (CANON-EXPANDED: a hooded scryer with a disc of dark glass):
  - **Read:** disorient for 3 s on everyone within 6 m, every 18 s ("The dark glass turns your way, and your feet forget which way is which");
  - the serjeant's heavy blow knocks back 4 m;
  - **no silence and no doubled notice.** They never silence the healer.
- **Sims:** the leader treats the round as optional and holds at a bridge's foot while it is within 15 m (new, small).

**The Stoke-Hole (a bay east at 545 m; 44 m wide, 40 m deep, 18 m tall):**
- Along its east wall are the furnace's six fire-pit mouths. Their flues run under the hall to the Crucible.
- At each pit, an Indentured stoker shovels luminous blue coal (CANON). Each is chained to a ring by an Arcane Tether of light (CANON).
- The stokers are non-hostile figures with a work routine. Heaps of blue coal, with shovels in them.
- **Camps:**
  - W6, the pit Spent: three dead Indentured still walked by their collars, 20, 15 m in. Their blow greys you, as the Hollow Men's does. **This is decision 1.**
  - W7, the stoke-drivers: two foremen, 20, Ebert's guards, 28 m in.
  - W8, Ebert Lade, 34 m in.

**Boss: Ebert Lade, the Stoke-Master** (20, elite; a Council foreman in oil-stained robes).
- **His lesson: cut the tethers.** It is the one tether fight in the three later dungeons (shared rules).
  - While three or more pits burn high he is **white-hot**: his blows are half again as hard, and he mends 0.5% a second (as the Rock-Eater mends while goaded).
  - Cut a stoker's tether with E and a 2 s bar. Anyone in the party can do it, before the fight or during it. Cutting one within 20 m of him pulls him.
  - The freed stoker drops the shovel and runs for the Tether-Rows, and that pit banks down.
  - At two pits or fewer he is only a man with a whip.
- **The tether-whip:** root for 3 s on everyone within 5 m, every 15 s.
- At half he rings his hand-bell, and the gaolers' post (W2) comes if it lives (a call).
- When he dies, the gallery guard (W1) walks down if it lives (deathCall).
- He carries **the pay-tally**, the quiet way's key (`sludge.tally`).
- **The floor goes quiet** (new: camps laid down by tag):
  - The tethers hold the Spent up. When Ebert dies, or the sixth tether is cut, whichever comes first, **every Spent left in the Works lies down where it stands**.
  - Their collars' light goes out, down the length of the hall. In a sim-led run that is W4 on the north rows, in sight through the Stoke-Hole's mouth.
  - Line: "Down the length of the Works, the collars go dark."
  - The fallen count for quest 10, Lay Them Down.

**[5a] The Tether-Rows** (a branch west at 565 m; 70 m long, 14 m wide, 7 m tall; a built hall; cold collar-blue):
- **Not a gaol** (the gaol is the Deep Veins'). It is where the Indentured sleep between shifts: rows of iron rings set in the floor, each with a tether of light to a sleeper on a plank pallet, buckets, a water-trough. No cells and no bars.
- The tally-yard is at the end, 24 m across: the foremen's desk, where the shifts are counted out and in.
- **Camps:**
  - P1, the row gaolers: three, 20, at 25 m.
  - P2, the tally-desk: two foremen, 20, at 55 m.
- **Six collars to cut** for The Soul Tax (the sleepers at the rings; the cage kind, quest-counted). Gytha's is one of them.
- **Gytha Lamb**, an Indentured debtor, sits chained at the last ring.
- Secret: a key on a hook behind the tally-desk. Its chest is in the Cooling Stacks.

**The event: Down Tools** (the canon riot on the floor, b1 ch.12; GAME-ONLY as played). It is a riot, not an escort: no cells, no walk under waves, no boss rising. The gaol rising with all of those is the Deep Veins' (shared rules).
- **The choice.** With P1 down, cut Gytha's collar (E, 3 s). She stands, rubs her neck, and asks: "Shall I give them the word?"
  - **Not yet:** she waits at the ring, and you can come back to her this visit.
  - **Give it:** she walks out of the Rows onto the floor to the Crucible's foot and calls the gangs: "Down tools!"
- **What happens, for this visit** (new: per-visit flags, cleared when the camps come back):
  - the chain-gang drops the Crucible's poles, and the ladle-gang on the Casting Floor drops its ladle-poles. Both fall on their drivers with the poles;
  - **the drivers are beset:** the chain-gang's drivers (W5) and the ladle guard (W10), if they still stand, start at half health and stand fighting the rioters where they are, until you come (no call, no walk-in);
  - **the Pour is off:** Hagan Brack has nobody to tip his ladles;
  - **the house hears it:** the riot's noise carries down the stair to the Counting-House, and the doorman will not draw the bolt for the shift bell. **The quiet way at G3 is shut this visit.** The loud way is open: the freed wait by the great ladle;
  - when the fighting near them stops, the freed go down the drain-head in the floor's south-west corner by themselves, and are gone.
- **Kept for good:** `sludge.downtools`, for the quest.
- On the next visit the gangs are back on their poles. The hall's set piece is never lost for good.
- **The play:** riot first, and the Works' drivers are half beaten and the Pour never comes, but the house will be waiting for you. Or leave the floor at work, take Brack's Pour, and walk into the house unseen with the tally.
- The leader never gives the word (it uses no interactables): a sim-led run fights the floor at work and goes in quietly.

**The Casting Floor (600-660 m; 56 m wide, 20 m tall):**
- Three **sand beds** pressed with moulds lie across the floor's middle, each 14 × 10 m with 6 m of flags between them. Ingots cool in rows.
- Two ladles hang on chains from a timber beam over the beds. They are filled from the Crucible's race, and a ladle-gang of the chain-gang swings and tips them with poles.
- **The great ladle** hangs on chains over the head of the stair down.
- **The shift bell** hangs in a timber frame with its rope (CANON: the city's bells, rung by hand).
- **The drain-head**, where the freed go out, is in the floor's south-west corner.
- **Camps:**
  - W9, Hagan Brack and a slag-hand, 20, on the beds at 630 m.
  - W10, the ladle guard: an Iron Pact serjeant (elite) and two gaolers, 20, at 645 m.
- **The Cooling Stacks:** a bay on the west side, 30 × 20 m, with rows of cooling iron crates (CANON) stacked high and ways at least 10 m wide between them. No camp: the secret chest is here.

**Boss: Hagan Brack, the Pour-Master** (20, elite; a free man paid by the pour). He wears a leather apron and an iron face-guard, and swings a casting-hammer.
- **His lesson:** step off the poured bed, or riot the floor first and there is no pour.
- **Hammer-Fall:** a heavy blow with a ground mark (windup 2.2 s, reach 4.2 m). It knocks back 4 m, never off walkable ground.
- **Quench-Hiss:** he dashes a bucket into a trough, and the steam stuns everyone within 6 m for 2 s, every 20 s. He never uses it during a Pour's mark.
- **The Pour, at 3/4, 1/2 and 1/4** (the hazard strip, placed at one of three fixed beds):
  - he roars "Tip!", and the ladle-gang swings a ladle over a bed;
  - a 4 s mark covers whichever bed holds the most of the party;
  - then molten iron floods that bed: 45% of max health when it comes, then the bed burns for 6 s (15% a second for anyone who stays or walks in);
  - step onto another bed or the flags between. The sims step off as they do from a heavy blow's mark.
- **If Down Tools ran this visit:** "Brack roars for the ladle and nobody comes: the gang's poles lie on the floor." There are no Pours, and he enrages at 40% instead of 30%.
- When he dies, the ladle guard (W10) comes if it lives (deathCall).

**G3, the Iron Door at the foot of the stair (675 m).** There are two ways through:
- **Quiet:** with Ebert's pay-tally, ring the shift bell (E).
  - It is the shift change. The house's doorman draws the wicket's bolt for the foremen to come in and be paid, and you walk in with the tally.
  - The house does not know you are there: no wave.
  - **Not after Down Tools this visit:** the doorman heard the floor, and the bolt stays drawn. "Nobody comes to the wicket."
- **Loud:** with Down Tools done this visit, the freed tip the great ladle (E: Gytha's people lean on the poles).
  - Molten slag runs down the stair and the iron door goes.
  - The house comes for you: the Paying-Yard's guards and every non-elite camp within 78 m (SendWave).
  - The Counter still has men to pay.
- The leader waits at the shut door: "There's the bell, if we've the tally. Or the ladle, if the floor's with us. Your call."

**[5b] The Old Crystal Working** (a branch east at 630 m; 185 m, falling 12 m; dead black, pale blue coal-glow, faint violet at the edges):
- **Lore:** the Former Miner's working (lore_stories #4, "the mines in sector 4"). It collapsed two days after the robed man said "Insufficient", and the foundry was built out over its head (GAME-ONLY). Its foreman went back in for the others and did not come out.
- **Built as:**
  - **The Old Head (0-50 m):** crushed timber sets, black and splintered, the floor heaved. Unlike the Adit's living drifts, this working is dead.
  - **The Coal Seams (50-95 m):** blue coal glowing in the walls (CANON; a ZoneNode `under` for gathering), and a miner's pick left in a seam.
  - **The Black Crystal (95-148 m):** a crystal the size of a wagon, matt black, with only a faint violet left at its edges. It is the Crystals helper tinted black and scaled up.
  - **The Fall (150 m):** a wall of rubble across the way on.
  - **The Fault (150-185 m):** where the last shift lay down, crusted with black crystal, the men's lamps still hanging on their hooks.
- **Camps:**
  - X1, the Coal Seams: three of the Drained, 20, at 65 m, 8 m left;
  - X2, the Black Crystal: three of the Drained, 20, at 120 m;
  - X3, **Odo Stannard, the Last Foreman** (hidden), beyond the Fall;
  - X4, his men: three of the Drained in the Fault's side pockets, out of sight until he calls.
- **The Drained:** the hollow look in a crystal-grey tint. They are the miners the drain emptied along with the crystal (GAME-ONLY reading of "Insufficient"). Their touch is **the existing grey**: their target takes 10% more damage for 12 s, and a cleanse lifts it.
- **Things to use:**
  - **the pick** (E, kept for good: `sludge.pick`);
  - **a shard of the black crystal** (E, for quest 9).
- **Hidden: the Fall.** It is a gate dressed as rubble (new kind `dig`).
  - With the pick, dig through it: E and 8 s.
  - The leader passes it unless you carry the pick and walk to it.

**Hidden boss: Odo Stannard, the Last Foreman** (21, elite; GAME-ONLY). The foreman who went back in for his men, drained with them: the hollow look in crystal-grey, larger, black crystal grown over his shoulders, a dead tally-lamp at his belt.
- **Black Crystal:** root for 4 s on everyone within 6 m, every 16 s ("the crystal grows over your boots").
- **Shard-Shake:** a ring burst of black splinters, every 15 s (ringEvery).
- At half, his men come out of the side pockets (a call).
- He enrages at 30%.
- When he falls, the lamp at his belt flickers once.

**Packs in the Works:** 10 camps and 25 mobs in the Crucible Hall, the Stoke-Hole and the Casting Floor; 2 camps and 5 mobs in the Tether-Rows; 4 camps and 10 mobs in the Old Working.

### [6] The Counting-House (the stone stronghold; the finale; 20): 125 m, down 8 m
**Built as:**
- The stronghold in the foundry's old foundations: grey ashlar walls three courses (6 m) high.
- The kit's columns, and arched doors with iron grilles.
- Flagged floors, and oil lamps of cold gold light.
- **Nothing burns here but the counting-room stove: the cold after the heat.**
- No chat lines but the scratch of quills.
- **Smell line:** lamp-oil and ink.

**Stretches:**
- **The Paying-Yard (675-705 m; 40 m wide, 12 m tall):** where the foremen queue to be paid. A queue rail, a bench, and a pay-window into the Pay-Hall.
  - K1, the pay-window: a house guard (elite) and two runners, 20, at 690 m.
  - K2, the yard watch: two house guards walking the yard, 678-705 m (a patrol).
- **The Pay-Hall (705-745 m; 44 m wide, 16 m tall):** a pay-office, not a weigh-house (the Hub weighs).
  - Long pay-tables with coin in counted stacks, racks of notched tally-sticks, sacks of coin, and crates of sifted slag-light glowing amber. No scales.
  - K3, the pay-clerks: two bookmen (they flee and call), a house guard and a sawbones, 20, at 718 m, left. The sawbones heals: kill it first.
  - A bookman's slip, "Grist keeps her own book under the hearth", points to the secret.
- **The Strongroom (755-785 m; 28 m across, 10 m tall; the end):**
  - Iron-bound chests, coin piles, the Paymaster's table, the Counter's desk with his ledgers, and the counting-room stove on its broad stone hearth.
  - K4, the strongroom door: two house guards (elite), 20, at 752 m.
  - K5, Abel Ledgard, at his desk (770 m).
  - K6, Ottilie Grist, at her table (778 m).

**The finale pair:**
- **Abel Ledgard, the Counter** (20, elite, holdable). He keeps the house's purse.
  - **Short Weight:** he flings a fistful of coin into the faces of everyone within 5 m: disoriented for 2 s, every 20 s.
  - **He pays in a guard:** every 20 s while he is free, one living mob from the Paying-Yard's or the Pay-Hall's camps walks in. It is a call that brings only mobs that already exist.
  - **His lesson:** clear the house first, and there is no one left to pay.
  - Held (sap, hex, root; kept held as Nix is), he pays no one and throws nothing.
  - Killed first, he sends the Paymaster mad over her books: she enrages at once.
- **Ottilie Grist, the Paymaster** (20, elite, `harder`, `boss:true`; the last boss).
  - **Settle the Account:** a heavy blow with a windup and a mark. Step out. It stays under 60% even at six stacks of Interest.
  - **Interest** (new: a stacking grey):
    - each of her blows on her target adds a stack: 5% more damage taken for 15 s, up to six;
    - a cleanse (Mira's, or a Paladin's) lifts them all.
  - **"The Teeth collect":** fear for 3 s on everyone within 6 m, every 22 s.
  - **"Called to Account", at 2/3 and 1/3** (new: a phase that stuns any one member, not only the player):
    - she opens the ledger and reads a name: any member but her target;
    - that member is stunned for 3 s, and every guard in the room turns on them for 6 s;
    - the healer has to answer.
    - With the house cleared and the Counter held, nobody comes.
  - **The books, at 1/4** (new: a timed interactable in a fight):
    - she throws the ledger into the counting-room stove;
    - you have 15 s to pull it out (E, 3 s) while the party holds her;
    - **saved:** the Paymaster's Ledger drops whole (the drop carries `whole`), and quests 2 and 3 pay more;
    - **lost:** it drops charred, with half the names gone. The quests still finish.
  - She enrages at 20%.
  - When she dies, the house's last guards walk in (deathCall), and the scorecard opens.
  - She carries **the Paymaster's Ledger** and **the Teeth's tally-seal** (quest 11).
- **The play:**
  - clear the house;
  - hold the Counter;
  - lift the Interest;
  - answer the named member;
  - save the books;
  - burn her down, then him.

**Secret: under the hearth.** The stove's hearth-stone lifts (E, 3 s; it is found by searching it, or the bookman's slip marks it; a secret with key `sludge.hearth`).
- Under it, in a stone-lined pit, are the Paymaster's own strongbox (a chest) and **the Peaks account** (a note: the Adit's geode money, paid through here to "H.O., the coast station"; the follow-on to quest 1 for Lisle).
- The house's one secret is in its floor. A hidden door to a side room is the Hub's (shared rules).

**The way out: the coin-chute.** It is the house's drain behind the Strongroom: E, 5 s on the bar, and the party stands at the Outfall.

**Packs:** 6 camps and 13 mobs.

### 4.1 Hidden from the readers (the stealth, two canon sources)
- **The runoff basin** at the Chimney's foot (b1 ch.16:75: "The chemical runoff interferes with magical tracking"). Smeared on, it lasts 20 minutes on the whole party.
- **Silver dust** (b1 ch.3:103: smeared "to rapidly scatter the Inquisitor's resonance readers"). Bought from Kessock, or found in the tosher's cache. Used (E), it lasts 30 minutes on the whole party. It is the party's tool here, and in none of the three dungeons is it a weapon (shared rules).
- **What it does:** every mob's notice of the party is halved, from 8 m to 4 m. Sneaking still shrinks it further (EncounterEnemy l.276).
- **Where it matters:**
  - the reader's round in the Crucible Hall (the designed case);
  - the Works' wider floors, where camps stand 12 m and more apart.
- It does nothing to a camp you strike or that is called.

### 4.2 The crowd (frame rate and navmesh)
The Works hold more non-hostile figures than any place in this dungeon:
- the chain-gang (24);
- the stokers (6);
- the sleepers in the Tether-Rows and Gytha (7);
- the ladle-gang (4, of the chain-gang);
- the lightermen on the quay (9, as mobs).

A's first draft had fifty in the chain-gang; that is cut to 24. **The budget** (shared rules: the crowd):
- **No more than about 40 animated figures in any one view.** The worst is the Gallery, looking down the hall; then Down Tools, when the gangs fall on their drivers.
- **Standing workers** (the chain-gang, the stokers, the ladle-gang, the sleepers) get no NavMeshAgent and no collider. They play one looped work clip each, offset in time, and their Animators are culled when off-screen.
- **The rioters** in Down Tools play their attack clip in place beside the beset drivers; they get no agents and strike nothing (the drivers' half health is the flag's, not the figures').
- **The freed** get agents only while they walk to the drain-head after Down Tools, and are removed there.
- **The navmesh:** none of these carve it. The slag rivers are NavBlockers with bridge gaps, as the lava lake is.
- **A capture with the frame time** from the Gallery, before the Works are dressed further, and again at Down Tools' busiest moment. If it is worse than the Adit's Rail Hall, the chain-gang drops to 16.

### 4.3 No machines (0.05): what canon has and what the game builds

| Canon (the novels and maps) | The game |
|---|---|
| A steam whistle at the shift change | The shift bell, rung on a rope; the doorman draws the wicket's bolt by hand |
| Overseer and Enforcer Golems; War-Golems at the crossings; the Golem bay | Iron Pact gaolers, foremen and a reader with the Pact; the bay dropped |
| Conveyors, the stamping press, drop hammers "the size of manor houses" | Hand-barrows, porter chains and sledge-gangs at anvils (decor) |
| The Architect's glass lift | Not in the dungeon |
| The great ladles and crucibles, drawn with gears | Ladles and the Crucible hung on chains, tipped by gangs with poles; no gears |
| Bellows | None. The Megapack's Bellows is a machine prop: never placed. The fire is fed by stokers with shovels |
| Sluice gates that vent on a schedule | Timber sluice-boards lifted by windlasses that gangs turn by hand |
| A drawbridge | The plank-way, let down from a windlass by your own hands |
| Horgus's barge and its furnace | Not here. The lightermen pole flat lighters by hand |
| The collar walking a dead man "as a rudimentary locomotive engine" | The tether's spell holds the body up (decision 1) |
| The intake shaft's draught | Heat rising through a flue by itself; no fans |
| Snipers on the upper gantries | Crossbowmen on the floor under the gantries (no fighting on catwalks) |
| Strongroom shutters that come down | None. Nothing in the house moves without a hand on it |
| A collapsed mine cleared by machines | The Fall, dug by hand with the old miner's pick |

Machine words are kept out of player text (shared rules: engine, machine, mechanism, gear, piston, pump, valve, press, conveyor, pneumatic, clockwork, battery, grid, crane, lift). So are canon names with them in (section 1's name hazards). The canon's own substitute word is "artifice" (WB:6).

### 4.4 The bosses at a glance (all GAME-ONLY; every named elite holds the party; no blow over 60% of a member's health)

| Boss | Lvl | Where | Teaches | Holds the party | New code? |
|---|---|---|---|---|---|
| Leof Grue, the Sump-Boss | 19 E | the Toshers' Cistern | pull the bolters away; the first boss | root 3 s within 4 m / 15 s | none (calls, deathCall, cc built) |
| Corse, the Hanging Brother | 19 E | the hub (the Nest) | light the lamps; clear the nest first | fear 3 s within 6 m / 18 s | the Nest (waves at a point); lamps that show hidden camps; Into the Vault (HideInside without a mount, a call on hidden camps) |
| Senna the Wirewife | 19 E | the Wiring-Room | clear the room or fight it all | root 4 s within 6 m / 14 s | none (cc, call and rally built) |
| The Prior Under the Floor (optional) | 19 E | the Prior's crypt | the bell | disorient 3 s within 8 m / 20 s | none |
| Gisla Mott, the Sluice-Master | 19 E | the Sluice-House | the flush; stop the gangs | stun 2 s within 5 m / 16 s | the hazard strip, tied to the gangs |
| Long Hessick, the Hanging Man | 19 E | the Chimney (the pursuit) | fight on the landing | stun 2 s within 4 m of where he lands / 16 s | the pursuit (waves on progress) |
| The Flue-Eater (optional) | 20 E | the old flue | salt and harpoons | disorient 2 s within 6 m / 20 s | salt, harpoon, calcify, its weapon resistance |
| The Reader of the Works (the round; optional) | 20 E | the Crucible Hall | let it pass hidden, or fight three | disorient 3 s within 6 m / 18 s | the notice-halving buff |
| Ebert Lade, the Stoke-Master | 20 E | the Stoke-Hole | cut the tethers | root 3 s within 5 m / 15 s | white-hot from live tethers; the Spent laid down by tag |
| Hagan Brack, the Pour-Master | 20 E | the Casting Floor | step off the poured bed, or riot the floor first | stun 2 s within 6 m / 20 s | the Pour (the strip at the bed most of the party stands in), off under Down Tools' per-visit flag |
| Abel Ledgard, the Counter | 20 E | the Strongroom | hold him; clear the house first | disorient 2 s within 5 m / 20 s (Short Weight) | pays in a guard every 20 s while free |
| Ottilie Grist, the Paymaster | 20 E harder, boss | the Strongroom | the finale pair; Interest; the named member; the books | fear 3 s within 6 m / 22 s; Called to Account: stun 3 s on one named member at 2/3 and 1/3 | Interest (a stacking grey); Called to Account (a phase stun on any one member, guards turn on them); the stove (a timed interactable mid-fight) |
| Odo Stannard, the Last Foreman (hidden) | 21 E | the Fault | find the pick and dig | root 4 s within 6 m / 16 s; a ring burst / 15 s | the dig (a gate opened by a key and a work bar) |
| Old Tosh (rare, 0.2) | 19 E | the swine bay | luck | his charge: stun 2 s within 4 m of where he stops / 18 s; it knocks back | none |
| Wenlock Sculler (only if the quay is fought) | 19 E | the Sunk Quay | a choice with a cost | disorient 3 s within 6 m / 20 s | neutral camps |

- **Patrols walk in after:**
  - Leof Grue (the lantern-men);
  - Ebert Lade (the gallery guard);
  - Hagan Brack (the ladle guard);
  - the Paymaster (the house's last guards).
- Only the Paymaster is `boss:true`, so the scorecard opens on her.
- Down Tools has no boss of its own: the Deep Veins' Rising has the captain who rises (shared rules).

### 4.5 Things to use, events, secrets and gates at a glance
- **Things to use:**
  - the plank-way;
  - the toshers' ladder;
  - the fence's shop;
  - the six lamp-niches;
  - the two bone-stacks;
  - salt (the store and the line);
  - bone harpoons;
  - the runoff basin;
  - silver dust;
  - the flue ladder;
  - the six stokers' tethers;
  - the six collars in the Rows, and Gytha's word;
  - the old miner's pick and the black crystal's shard;
  - the shift bell;
  - the great ladle;
  - the counting-room stove;
  - the hearth-stone;
  - the coin-chute;
  - the whispering slag-light shards;
  - two spirit stones.
- **Events:**
  - the Nest (a wave and Corse, then Into the Vault);
  - the pursuit up the Chimney (two waves and Long Hessick);
  - the flush (timed);
  - the steam vents (timed);
  - the bone-stacks (set off by you);
  - the Pour (timed, off after Down Tools);
  - the floor going quiet;
  - Down Tools (a riot: the drivers beset, the Pour off, the quiet door shut);
  - the loud way (the house comes for you);
  - the Counter paying in guards;
  - the books in the stove.
- **Secrets** (seven, on no map):
  - road-marks in the brick (note);
  - a tosher's cache (cache, with silver dust);
  - a fisher family's name-board (note);
  - "In the name of the First Kin" (note);
  - the first sight of the Crucible (vista);
  - the key behind the tally-desk and its chest in the Cooling Stacks (key and chest);
  - under the hearth (chest, and the Peaks account).
- **Gates:**
  - G1, the plank-way (lay it);
  - G2, the Iron Teeth's grille (the sump key);
  - G3, the Iron Door (the bell and the pay-tally, unless Down Tools ran this visit; or the ladle after Down Tools);
  - the Fall (the pick);
  - the hearth-stone (the secret).
- **Side areas:**
  - the swine bay;
  - the Sunk Quay;
  - the Toshers' Cistern;
  - the Prior's crypt;
  - the Salt Store;
  - the old flue;
  - the Stoke-Hole;
  - the Tether-Rows;
  - the Old Crystal Working and the Fault;
  - the Cooling Stacks.

## 5. Mobs (all GAME-ONLY as mobs; factions and bodies labelled)

| Where | Mobs (look) | Behaviour | Label |
|---|---|---|---|
| Outfall, Sludge-Tunnels, Sifting Beds, Chimney | Iron Teeth runner, raker, knee-breaker (elite), slag-lighter, drain-cutter, sluice-hand, hookman (new look "ironteeth" on the people kit: oiled leather, the ochre jagged-teeth sign) | rakers flee at 25% and call; slag-lighters throw lit slag (blast); drain-cutters ambush from culverts; knee-breakers' heavy blow | Iron Teeth: CANON |
| Outfall, Sludge-Tunnels, Sifting Beds | sewer swine (the Wild Boar with a sludge-grey tint; slag-fed ones with violet eyes); Old Tosh (rare, larger) | pack; charge | GAME-ONLY (canon's giant grey sewer rats, b1 ch.9:71, have no model, and the Deep-Rats are the Deep Veins') |
| Sunk Quay | sludge-hog (the Wild Boar in a mire tint) | solitary; charge | GAME-ONLY |
| Sunk Quay | lightermen; Dace Kessock; Wenlock Sculler (villager kit in oilskins and river-boots; poles and hooks) | neutral until struck, then the quay calls | the trade CANON-EXPANDED (Horgus's, b1 ch.16:337); the lightermen GAME-ONLY |
| Bone Cathedral, Catacombs | ossuary dead (the ossuary knight; fall into bones) | slow, heavy | CANON-EXPANDED (the priests' "angry dead", b1 ch.10:15) |
| Bone Cathedral, Catacombs, Chimney | wire-mask, ash chanter (drain), ash mender (heal), nest-hanger (cultist look with a wired bone mask, new) | menders heal; nest-hangers hang hidden, drop, and come in waves | Cult of Ash: CANON |
| Catacombs | wired thrall (the Toon RTS undead; it crumbles) | thralls stand in niches until called | CANON-EXPANDED (the cult's wired bone, b1 ch.10:39) |
| Chimney, old flue | the Flue-Eater (the weaveeater body) | see 4.4 | Weave-Eaters: CANON |
| Works, Tether-Rows | foreman (the Concord kit in oil-black robes); Iron Pact gaoler and serjeant (the warden kit in iron grey); gantry-shot (crossbow; closes between shots) | the serjeant is elite; gaolers in threes | CANON-EXPANDED |
| Works (the round) | the Reader of the Works (new look "reader": a hooded scryer with a disc of dark glass) | elite; Read (a disorient round him); walks with the Pact | CANON readers (b1 ch.3:103); the Reader CANON-EXPANDED |
| Works | the Spent (the hollow look with a glowing collar and a jerking walk) | slow; their blow greys you (+10% damage taken for 12 s; a cleanse lifts it); they lie down when Ebert falls | CANON-EXPANDED (canon's image, b1 ch.12:37-39; decision 1) |
| Works, Tether-Rows | the Indentured: stokers, chain-gang, ladle-gang, the sleepers, Gytha (villager kit in grey, collar, tether of light) | non-hostile; work loops; the rioters strike in place in Down Tools; the freed walk to the drain-head | CANON |
| Old Crystal Working | the Drained (the hollow look in crystal-grey); Odo Stannard (the same, larger, crystal-crusted, a dead lamp at his belt) | the Drained's touch is the grey | GAME-ONLY (from lore_stories #4) |
| Counting-House | house guard (elite and not), bookman, sawbones, runner | bookmen flee and call; the sawbones heals | Iron Teeth: CANON |

- **The wired thrall's pack has only idle, walk and attack clips.** Since 2026-10-14 it rocks back when struck (ModelBeast, for a creature with no hit clip), which is enough for niche-standers that die fast. Swap it if a fuller undead pack comes.
- **No bats, rats or flying mobs:** none can be built (no model, and every enemy is a NavMeshAgent).
- **No spiders:** the spider model's crawlers are the Deep Veins' (shared rules). The Old Working's dead are the Drained and their foreman.
- **No birds or squirrels underground:** the crows stay on the Outfall's slag-banks.

## 6. Quests (given outside, on the Outfall's landing)

**None of these can be offered until the cap reaches their minLevel (Quests.cs l.128).** Their givers stand on the Outfall's landing, in the dungeon zone's own open ground, so the quests can be built and tested without the Lowtowns or the Forge District; they move up into those zones when they exist. They follow the Adit's pattern (QUEST_DESIGN.md, The Sealed Adit):
- kills are named `mob.<tag>.sludge.*`;
- a boss's item is "collect" from `kill:mob.<tag>.sludge.*`;
- events read `key:<id>` flags.

1. **Where the Light Goes** (`side.sludge.light`; Lisle Tamber, the Salt-Mender scholar from the Adit, now on the landing; requires A Letter Under Seal; level 18).
   - "The geodes go to the factor at the coast. The factor sells them on to the foundry. And something's selling the foundry's dregs on every stall in the Rust Market."
   - Bring Leof Grue's tally-book, and see the Sifting Beds (visit).
   - Reward: Salt-Menders standing.
   - Follow-on: bring her the Peaks account from under the Paymaster's hearth, for more standing. It gives the factor's mark and station, "H.O., the coast station", not his name: Lisle: "Initials. He's careful." His name is found at the Transit Hub (its quest 5, The Factor's Name, which needs this quest).
2. **The Night-Shift's Pay** (`side.sludge.pay`; Lisle, after 1; level 20).
   - Get into the Counting-House by either way, bring down the Counter and the Paymaster, and bring the ledger.
   - Reward: a choice of three rare pieces. A fourth choice is added if the ledger came whole.
3. **The Ledger's Last Page** (`side.sludge.ledger`; dropped by the Paymaster; to Lisle).
   - The ledger's columns:
     - slag-light by the weight;
     - the night foremen's cut;
     - "hands of the Veins legion, bought of the Count-Master: forty, spent";
     - "to the factor at the coast, his tenth".
   - Lisle: "Forty, spent. They buy people by the forty. Go down under the Warrens and see who sells them. Gerda Holm keeps the Salt-Menders' post at the Bread Drop." **This is the hook to the Deep Veins** (its "way here"). The factor is the long hook to the Transit Hub.
   - **Whole:** every name and the factor's line are there, and the Salt-Menders' standing is doubled. **Charred:** half the names are gone, but the Count-Master's and the factor's lines survive and the hooks stand.
   - Canon roots: the penal legions mine raw ore in the Deep Veins (b1 ch.3:133); the "holding pens" under the Warren District (b1 ch.24:21).
4. **The Soul Tax** (`side.sludge.soultax`; **Gorm Rock-Wall**, CANON, b1 ch.1:21, alive; decision 2, until answered a GAME-ONLY orc docker; level 19).
   - "Gytha Lamb kept the soup-pot going through the Shatter-Riots. The Council took her for the Soul Tax. Cut her loose."
   - Cut six collars in the Tether-Rows (interact ×6), and see Down Tools through (flag `key:sludge.downtools`).
   - Reward: Lowtown Folk standing, a new GAME-ONLY local standing like Oakhaven Folk.
5. **Dregs** (`side.sludge.dregs`; **Agna Rudd**, a shard-pedlar on the landing who sells the Iron Teeth's cheap shards and has started to hear them; GAME-ONLY; level 18).
   - Bring eight slag-light shards from the Sifting Beds.
   - Agna: "They're not dregs. They're people, ground small."
   - Reward: the Slag-Light Charm (a trinket). (No Tuning-Shard: those are Nila's, and Nila is the Hub's.)
6. **The Cult That Wears Our Name** (`side.sludge.wirewife`; Duruk, an Ash-Walker salt-scout come to the city; the people are CANON, the person GAME-ONLY; level 18).
   - Kill Senna the Wirewife, and bring five wired bone masks (collect from the cult's kills).
   - The allied Ash-Walkers hate the cult for the name it took: the game's canon split.
7. **Salt for the Flue** (`side.sludge.flue`; Duruk, after 6; level 19).
   - He gives three handfuls of salt. Kill the Flue-Eater, and bring its calcified heart.
8. **Rest for the Prior** (`side.sludge.prior`; Sexton Wulfric Dene of the old Cathedral district above; level 18).
   - Put the Prior Under the Floor to rest, and bring back his ring.
9. **Insufficient** (`side.sludge.miner`; **the Former Miner**, on the landing; CANON, lore_stories #4, unnamed as canon leaves him; level 19).
   - "I want to know if it's still black."
   - Visit the Black Crystal and bring him a shard of it (interact).
   - **9b, What the Mountain Kept** (`side.sludge.fault`; after 9; level 20).
     - "My foreman went back in for the others."
     - Find the old pick, dig through the Fall, put Odo Stannard to rest, and bring back his tally-lamp (collect from his kill).
10. **Lay Them Down** (`side.sludge.spent`; Sister Ebba Yew, a Preservationist in green leaf; the faction is CANON, the person GAME-ONLY; level 20).
    - Lay down eight of the Spent: kills, or the ones that fall when the Stoke-Master does (tag `spent`). Bring five collar-links.
    - Reward: Preservationist standing.
11. **The Teeth's Seal** (`side.sludge.seal`; starts from the tally-seal, dropped by the Paymaster; level 20). **A real choice** (new: two turn-ins for one item; taking one closes the other):
    - **To Dace Kessock**, the lightermen's fence, inside on the Sunk Quay (the one turn-in inside the dungeon, on purpose: shared rules). The lightermen stamp their own cargo with the Teeth's seal and run it past the tolls. Reward: coin and the Lighterman's Oilskin (a cloak), at a cost in Lowtown Folk standing.
    - **Or to Hild Seaver**, the chandler on the landing who speaks for the stall-holders the Teeth squeeze. She breaks the seal on the landing, and every stall learns the Teeth's sacks by sight. Reward: Lowtown Folk standing.
    - If the quay was fought, only Hild's turn-in is open.
12. **Heads for the Board** (the stall-holders' notice board on the landing: repeatable bounties, the city-gaol lesson).
    - Four postings, put up by the stall-holders the Iron Teeth squeeze: Leof Grue, Gisla Mott, Senna the Wirewife, Ottilie Grist.
    - Reward: coin and Lowtown Folk standing.

## 7. Loot

**The Night-Shift Kit** (rare, five pieces). It is what the foundry's night shift and the men who buy from them wear: oiled leather, iron, the ochre sign.
- The pieces:
  - Grue's Sump-Waders (feet);
  - the Wirewife's Hood (head);
  - the Sifter's Gauntlets (Gisla Mott, hands);
  - the Stoke-Master's Leathers (Ebert Lade, legs);
  - the Paymaster's Coat (chest).
- Two of the five are in the hub's side wings, which is the reason to run them.
- **Bonuses:** at 3 pieces, a stat. At 5, "The Cut": +15% coin from kills, and luck (the `coins` and `luck` kinds).

**Every named elite drops one named piece every time** (0.075: signature at chance 1; shared rules), with 20% extras. The group's roll decides who takes it.

| Boss | Pieces |
|---|---|
| Leof Grue | Grue's Sump-Rake (two-handed hooked polearm); the Tosher's Lamp (off-hand); the Sump-Waders (set) |
| Corse | the Hanging Brother's Mask (head: bone wired with rusting iron); the Wire-Cord (waist) |
| Senna | the Wirewife's Hood (set); Bone-Wired Bracers |
| The Prior (optional) | the Prior's Hand-Bell (off-hand); the Ring of the Old Cathedral (finger) |
| Gisla Mott | the Sifter's Gauntlets (set); the Sluice-Master's Gaff (one-handed); the Sluice-Key (trinket) |
| Long Hessick | Hessick's Bone-Hook (one-handed) |
| The Flue-Eater (optional) | the Flue-Black Edge (a sword gone black and still sound); the Calcified Heart (quest, then a trinket) |
| The Reader of the Works (optional) | the Reader's Glass (off-hand: a disc of dark glass) |
| Ebert Lade | the Stoke-Master's Leathers (set); the Stoke-Master's Shovel (two-handed mace) |
| Hagan Brack | Brack's Casting-Hammer (two-handed); the Pour-Master's Face-Guard (head) |
| Wenlock Sculler (only if the quay is fought) | Sculler's River-Boots (feet) |
| Abel Ledgard | the Counter's Quill (caster off-hand); Tally-Stick Bracers |
| Ottilie Grist | the Paymaster's Coat (set); Iron Tooth (a jagged dagger); the Paymaster's Purse (trinket, coins); the Paymaster's Ledger and the Teeth's tally-seal (quest) |

**Rare and hidden** (one better piece each):
- Old Tosh: Old Tosh's Tusk (dagger).
- Odo Stannard: the Last Foreman's Mantle (shoulders), or a Shard of the Black Crystal (neck). Plus his tally-lamp (quest 9b).

**Materials and curios:**
- **slag-light shard:** Agna's quest, and a smithing material like the Geode Shards;
- **luminous blue coal:** CANON; a smithing fuel, gathered from the Old Working's seams (ZoneNode `under`) and the Stoke-Hole's heaps;
- **silver dust:** CANON; Kessock's shop and the tosher's cache; the notice-halving buff;
- **collar-links:** the Spent; quest 10;
- **Iron Teeth tallies:** vendor trash.

## 8. Atmosphere
- **Sound is not built yet** (KNOWN_ISSUES), so these come as text and light:
  - the flush's groan before it comes, and the sludge-fall at the arch;
  - the cult's hiss as the vault drops;
  - the bone-stack's creak before it falls;
  - the pursuit's scrabble on the stair below you;
  - the shift bell's toll as a line every four minutes in the Works;
  - "Tip!" before the Pour, and "Down tools!" across the floor;
  - the Counting-House's silence.
- **Smell lines on entering each part** (the novels lean on smell, and most of these are canon's own):
  - copper and dead fish in the sewers;
  - wet rope and tallow on the quay;
  - dust and dry marrow in the ossuary;
  - burning copper in the Chimney;
  - sulphur in the Works;
  - cold stone and old coal-damp in the Old Working;
  - lamp-oil and ink in the house.
- **Light by part, so you always know where you are:**
  - the Outfall: smog-yellow;
  - the Sludge-Tunnels: sludge-green and lantern amber;
  - the Sunk Quay: tallow-yellow on black water;
  - the hub: bone-white and ash-grey, gold where a lamp is lit;
  - the Catacombs: grave violet, in a haze of bone-dust;
  - the Sifting Beds: slag amber;
  - the Chimney: red, with orange growing above;
  - the Works: white-hot orange, with the tethers' blue lines;
  - the Tether-Rows: cold collar-blue;
  - the Old Working: dead black and pale blue;
  - the Counting-House: cold lamp-gold.
- **The moments it is built round:**
  - laying the plank-way under thrown slag;
  - lighting the lamps and seeing the cult hanging over you;
  - climbing out onto the Gallery;
  - **the collars going dark down the whole hall when the Stoke-Master falls;**
  - the gangs downing tools and turning on their drivers;
  - the black crystal in the dark, and the foreman's lamp;
  - the Counting-House's silence after the fire;
  - pulling the books out of the stove.
- **The run's temperature:** cold brick, then cold bone, then the white heat of the Works, then the dead cold of the black crystal, then cold stone and gold. The last boss's room is the quietest in the dungeon.
- **A readable layout:**
  - from the hub, each way is lit its own colour;
  - the Crucible Hall is the second hub, with every way off it in sight from the Gallery;
  - the slag-light glows in the Iron Teeth's crates from the Sifting Beds to the Pay-Hall, so you follow the light to the money.

## 9. What has to be built (in order; each step shown to Chris in frames before the next)

1. **Prerequisite: the level cap to 20.** That means EncounterProgress.LevelCap, quest minLevel, the sims' levels, the talents, balance targets at 18-20, and the run tool's level 11 (RunCapture.cs l.32, 49) made a parameter. Nothing else: the givers stand on the Outfall's landing, so no other zone is needed.
2. **The layout:**
   - `tools/wip/dungeon/sludge_layout.py`, copied from adit_layout.py, with its own CAVES and CAMPS. It writes the plans, the camp centres, the routes and `Docs/art/sludge-plan.png`.
   - zone.sludge: `dungeon:true`, about 1,000 m, levels 18-20; its exits added when the Forge District and the Lowtowns are built.
   - The land: the cliff, the district's ground at +24, and the mount at +60 over the Works.
   - The root cave and seven branches. There is no branch of a branch: the Prior's crypt and the hearth pit are inside their chambers.
   - Checks:
     - nothing crosses in plan view;
     - no stair is over 25°;
     - every way is at least 10 m wide;
     - no chamber is wider than 85 m or taller than 28 m;
     - every roof is under the land, including the quay's (+8 under +24);
     - every camp stands at least 12 m off the round's road;
     - each spirit stone stands at least 20 m from every camp's centre and off every patrol's road.
3. **Cavern variant 3 and ZoneBuilder.Sludge.cs:**
   - **Linings:** red brick, bone and skull, black brick for the Chimney, black iron for the Works, dead timber for the Old Working, and ashlar for the house.
   - **Blocked strips:** the sludge channel along one wall, the old river's cut and bed, the slag rivers with bridge gaps, and the slag-race.
   - **Halls from the kit:** bone walls, crypt niches, black-iron Works walls, the Tether-Rows' floor-rings and pallets, the ashlar Counting-House.
   - **Set pieces:**
     - the Crucible on its chains and its frame;
     - the ladles and their beam;
     - the sand beds;
     - the plank-way and its windlass;
     - the sluice windlasses;
     - the lamp-niches;
     - the bone-stacks;
     - the shift bell, the stove and its hearth-stone;
     - the black crystal;
     - the sunk house-fronts and the lighters.
   - Catwalks for the eye only; light by part. No bellows, no Windmill, nothing that turns by itself.
4. **Looks, and the crowd budget (4.2):**
   - "ironteeth" on the people kit;
   - foreman and gaoler retints;
   - the reader (a hooded scryer with a disc of dark glass);
   - the sewer swine and mire tints on the Wild Boar;
   - the nest-hanger's wired bone mask;
   - the Spent (decision 1), the Drained, and Odo Stannard;
   - the lightermen;
   - Indentured figures with collar and tether, non-hostile and working (stokers, chain-gang, the sleepers), and their riot loop;
   - orc non-player figures (decision 3).
   - The wired thrall, ossuary dead and Weave-Eater already exist.
   - **A frame-time capture from the Gallery before going further.**
5. **The hazard strip** (one system, four uses here: the flush, the steam vents, the Pour and the bone-stacks):
   - a box on the floor with a 2-4 s warning, then damage and an optional knockdown, under the 60% rule;
   - started by a timer, by a boss's phase (picking the box most of the party stands in), or by an interactable;
   - switched off by a key, a per-visit flag, or the death of the camps working it;
   - never started while someone in it is held;
   - sims step out of it, as they do from a heavy blow's mark.
   - This is the game's first floor hazard. The Hub reuses it (Kell's Drive).
6. **Interactable kinds,** each with its own `sludge.*` key, never the Adit's global "adit.lift":
   - the plank-way (a ZoneGate let down from a windlass);
   - the ladder and the chute (the carriage ride generalised: a work bar, then a teleport within the zone or to another zone);
   - lamps (they show hidden camps; per visit);
   - bone-stacks (tip one to set off its strip);
   - the basin and silver dust (the buff);
   - salt and harpoons (picked up, then used on the Flue-Eater);
   - tethers (cut; counted by the Stoke-Master);
   - collars (the cage kind, built), and Gytha's word (a conversation window: Down Tools or not yet);
   - the pick and the dig;
   - the bell and the ladle (G3's two ways: tally plus bell unless Down Tools ran this visit, or Down Tools' per-visit flag plus ladle, which calls SendWave);
   - the stove (a timed use inside a fight);
   - the hearth-stone (a secret that opens a pit with a chest).
7. **Boss and event code:**
   - EliteMoves entries for the fifteen;
   - Ebert's white-hot (a buff while three or more tethers stand);
   - camps laid down by tag (the Spent);
   - Corse's Into the Vault (HideInside without a mount, and a call on hidden camps);
   - the Flue-Eater's salt and pin, and its 60% weapon resistance;
   - the Counter's paying (a call of one living mob every 20 s while not held);
   - Interest (a stacking grey);
   - Called to Account (a phase stun on any one member, and guards turning on them);
   - **waves at a point** (the Nest at the font, the pursuit at the landings), generalised from the Weaver's waves;
   - **Down Tools:** per-visit flags that set named camps beset (half health, held in place, no call), switch the Pour off and shut G3's quiet way, with the rioters' loop dressed beside the beset camps;
   - **per-visit flags**, cleared when the camps come back (the lamps, Down Tools' effects).
8. **Neutral ground and stealth:**
   - neutral camps that turn hostile when struck, with a kept key;
   - a shop and mender inside a dungeon (Kessock);
   - the party buff that halves notice.
9. **Shared pieces, built here first** (shared rules), because this is the first of the three to be built:
   - the party of five in a `dungeon:true` zone (the door refuses a sixth; Mira counts);
   - BoundStone returns the deepest stone touched; TouchStone's line per zone.
10. **Content:**
    - Quests/sludge.json: 12 quests (with 9b) and 4 bounties, the two-turn-in choice in quest 11 (new: a quest item with two turn-ins, one closing the other);
    - loot.sludge.json: the kit, the boss tables (every named piece at chance 1), the materials;
    - a new local standing, Lowtown Folk;
    - the givers as residents on the Outfall's landing;
    - registration in the scene's ZoneBuilder.zones and Encounter.asset's questFiles and itemFiles. A missing zone file broke 35 tests last time.
11. **Sims:**
    - the leader runs the root cave and splices in each branch at its opening;
    - **an optional flag on a camp:** the leader leaves the Prior, the Flue-Eater, the round, the quay's hogs and the Fall unless you walk to them;
    - it passes neutral camps and hidden packs, lights no lamps, tips no bone-stack and gives no word to Gytha;
    - it waits at the plank-way, the grille, the Iron Door and the Fall for you;
    - it holds at a bridge's foot while the round is near;
    - the sims step out of hazard strips;
    - the run tool's level is set for 18-20 once the cap allows.
12. **Tests:**
    - **SludgeNavTests:**
      - every camp is walkable from the way in, with the plank-way laid and the gates open;
      - no camp stands across water or slag;
      - every way is at least 10 m wide, and no stair is steeper than 25°;
      - each spirit stone is 20 m from every camp and off every patrol's road.
    - **Hazard tests:** a strip never lands on a held member, and every warning is longer than the room's longest hold.
    - **Boss tests:**
      - Ebert's tethers, and the Spent lying down;
      - Corse's vault with and without the lamps;
      - Gisla's flush;
      - the Pour with and without Down Tools;
      - the Counter's paying and his Short Weight;
      - the Paymaster's Interest, Called to Account, and the books saved and lost;
      - the Flue-Eater's salt;
      - the round with and without the buff.
    - **Down Tools** end to end: the beset drivers, the Pour off, the quiet door shut and the loud door open, and the per-visit reset.
    - **The party of five** at the door, with and without Mira.
    - The quests end to end, including the choice.
    - **A group run with group_run.ps1 at level 20 on Chris's screen,** with the scorecard, against the 52-minute full clear, the 59-minute everything and the 13-minute short run.

**New code, honestly counted:** seventeen pieces, against the Adit's dozen, plus the two shared pieces of step 9.
- Many are small or share a base: the four hazards are one system, and the Nest and the pursuit are one wave system. Down Tools is flags, not an escort.
- **Needed for the dungeon to run:** steps 5-7, step 9 and the sim rules.
- **Can follow in a second pass without breaking anything:**
  - the stove (until then, the ledger always drops whole);
  - the neutral quay and its shop (until then, only the Sump End's hogs);
  - the notice buff (until then, the round is fought or dodged);
  - the two-turn-in choice (until then, the seal goes to Hild only).

**Biggest risks:**
- **The crowd in the Works:** about forty non-hostile figures, frame rate and navmesh. Hold to the budget in 4.2 and capture the frame time first.
- **The first floor hazard,** four ways at once. It must never fire under a sim that cannot get out, nor stack with a boss's hold into a death.
- **The Crucible Hall's scale:** 84 m wide and 130 m from the Gallery to the Casting Floor, as wide as the Rail Hall. Every camp is on the floor, none near the slag rivers' far banks, and the round's road is kept clear.
- **The plan:** seven branches off one line, none crossing, every roof under the land.
- **The length:** 54 camps come to about 59 minutes at the Adit's pace. If a group run's full clear passes 60, trim the threes first.
- **The build cost:** seventeen new pieces. Use the second-pass line above.
- **It waits only on the cap of 20.**

# The Deep Veins (levels 23-25, the mid band; designed 2026-10-14, built when the level cap rises past 15)

**Status: design only. Nothing here is built.**
- The level cap is 15 (EncounterProgress.LevelCap), and Quests.cs refuses a `minLevel` over the cap, so nothing here can be offered, levelled, balanced or run yet.
- It is built when the cap passes 22 (ROADMAP Phase 9 puts the Warrens at 22-25) and balanced at 23-25 as the cap reaches them.
- It does **not** wait for the Warrens zone. Its quest givers stand on the Bread Drop's rim walk, inside the dungeon zone's own open ground (as Oakhaven's Quill is a resident), and they move into the Warrens when that zone is built.
- It is held to section 0's bar:
  - its own zone, zoned into;
  - tunnels AND built halls AND a stone stronghold, each wing unlike the last;
  - "not just bigger but more too do!";
  - no machines (0.05);
  - the group-run lessons (0.07-0.08);
  - Chris's brief of 2026-10-14 (0.4): the dungeon should be complex like the models, not "one long route of camps".
- The rules the three later dungeons share are in the section before the Sludge-Tunnels (run length, a party of five, ways and size, holds, loot, spirit stones, the crowd, owners, names, the story chain).

**For Chris (short):**
1. Canon people as quest givers outside the dungeon (Razzle and Rill, with Chieftain Grohl taking one turn-in): yes, or keep the GAME-ONLY givers written here? (Until you answer: GAME-ONLY.) Gorm is not asked here: he is the Sludge-Tunnels' question.
2. Refuse the toll and the party fights the Toll-Collector. It escapes down its pipe at a fifth of its health, because it is alive in Book 1. Yes, or never fight it and have a GAME-ONLY Bailiff fight you instead?
3. The last boss's writ says "berths for ten thousand" and never says what for. Keep the number, or "as many as the rock will hold"? (The Hub's quest 11 follows the writ to "the Spire's intake" and says no more.)
4. The gas: does only a carried lantern light it, or do the party's own fire spells too? (Until you answer: only the lantern.)
5. The rats need a rat model, which the game does not have. Shall I look for one? Until one exists, the toll is pay-only and every rat is shown by red eyes in the dark (section 9, step 1).

## 1. Where it is and what it is (lore)

**Why the place exists.**
- Under the Lowtowns of Argentis, under the Warrens and the red-brick sewers, the stone turns to black basalt cut in hard, severe angles. These are the Deep Veins, "the dead roots of the entire mountain" (CANON).
- The Council has always sent its unwanted people down here:
  - Centuries ago it bound dissidents in stone-flesh magic and left them in the walls as living statues. Their faces are still there under a moss that pulses like a slow heartbeat (the Forgotten, CANON).
  - Today it sends debtors and bread thieves ("seven years in the deep dark", CANON) to the penal legion. The legion digs the raw geode ore of the Veins (CANON) that lights the High City (CANON).
- Now, under the Warren District, the legion is cutting something new and far too big: pens.
  - The pens are CANON: "the massive holding pens constructed beneath The Warren District".
  - That the legion is cutting them now is GAME-ONLY.
- **The old method, brought back.** The Count-Master has had the Council's stone-flesh used again, a little at a time, on convicts who will not work: the stone creeps up from the hands, and the bound work on, grey and slow, held to the last order given. These are **the Half-Bound** (GAME-ONLY; from the CANON stone-flesh of b1 ch.9).

**Who holds it.** The High Council. Day to day:
- the **Sump Watch** on the back door, a post of the City Watch (CANON-EXPANDED);
- **gaolers, turnkeys and a Chief Warder** in the gaol (GAME-ONLY);
- **trusties**: convicts who drive other convicts in return for a gilt collar and a cot (GAME-ONLY);
- **a binder**, a mage of the Council who keeps the stone-flesh (the man is GAME-ONLY; stone-flesh is CANON);
- **a company of the Iron Pact** that keeps the overseer's house (the army is CANON; the company is GAME-ONLY). Its quartermaster sells the legion's stolen ore at a pier on the old drowned workings, to buyers who come by water and give no names (GAME-ONLY trade);
- over all of them, **the Count-Master**, the overseer (GAME-ONLY). His writ carries the seal of Aethelgard, the Grand High Mystic (CANON, named only). He also sells the foundry its convict labour (the Sludge-Tunnels' ledger: "hands of the Veins legion, bought of the Count-Master: forty, spent").
- In the old drains, the **Deep-Rats** keep their own law and take their own toll (CANON), and the Council leaves them alone.

**Why the party goes in.**
- The Warrens drop bread down the drains for their own people in the deep dark, and never hear back.
- The Blind Crow's urchins (CANON) know the back way down and mark it in blue chalk (CANON: blue chalk means safe).
- The goblin Weavers (CANON) have found that the ground under the Warren District rings hollow.
- The Salt-Menders want proof of what the Council is cutting under their streets, and who sells the foundry its "forty, spent" (the Sludge-Tunnels' last quest sends you to their post at the Bread Drop).

**The story in one line:** the party goes down the urchins' back way, past the faces in the wall and the rats' toll and through the legion's mine. It frees the gaol, breaks into the overseer's keep and finds the pens, a gaol for ten thousand with no beds in it. The overseer's writ sends them on to the High-Sovereign Transit Hub.

**The canon route.** Book 1 chapter 9 walks the way in, in this order:
1. the Blind Crow;
2. the sewers;
3. the junction where three tunnels meet in a black whirlpool and red brick turns to black basalt;
4. the Ancient Drains;
5. the Hall of Silent Faces;
6. the cistern with the weeping angel and the Toll-Collector;
7. the gas;
8. the Skittering Dark.

The dungeon keeps that order, but for one step: the sewers (2) are passed, not played. The Bread Drop's sinkhole falls below them into an old cistern's undercroft, and the red-brick sewer wing is the Sludge-Tunnels' (shared rules); here the brick shows only where the three sewers pour into the Junction. The dungeon then goes on into the legion's mine (b1 ch.3:133), the gaol, the keep and the pens (b1 ch.24:21).

| Thing | Label | Source |
|---|---|---|
| The Deep Veins: "Absolute foundation level", "the dead roots of the entire mountain"; black basalt in "severe, geometric angles", heavy arches, toxic white mould, green moss that pulses like a slow heartbeat | CANON | b1 ch.9:47-55 |
| The junction: three rushing tunnels meet in a black whirlpool; the red brick ends and black basalt begins | CANON | b1 ch.9:47 |
| The way down: the Blind Crow (a Weaver's shop in the Warrens, the urchins' signal station; blue chalk on the lintel means safe), the sludge-tunnels, the Ancient Drains | CANON | b1 ch.9:7-9, 29-31 |
| The Hall of Silent Faces: thousands of real faces in the walls; dissidents bound in "stone-flesh magic" and left as living statues; the moss keeps the faces fresh; some are warped by the magic that pools low | CANON | b1 ch.9:55-67, 165 |
| **The Half-Bound**, convicts bound in stone-flesh a little at a time and worked on; **the Binder** who does it; the Count-Master's own binding | GAME-ONLY (from CANON stone-flesh; stone-flesh is played in this dungeon only, shared rules) | b1 ch.9:55-67 |
| The Bread Riots of '92; the lie that dissidents were sent "to manually farm the ash" | CANON | b1 ch.9:169-177 |
| The cistern; the weeping stone angel, green slime from its eyes | CANON | b1 ch.9:81 |
| The Deep-Rats: dog-sized, red eyes, "a highly organized trade union"; the Toll-Collector; pay or be eaten; kill one and a hundred come; paid in silver, which it bites; every red eye goes out when paid; it leaves by a rusted pipe | CANON | b1 ch.9:83-129 |
| Giant grey sewer rats | CANON (not placed: no rat model; the Hall's packs are mould-crawlers and sewer beasts) | b1 ch.9:71 |
| The Watch (Valen's old escape from it) | CANON; the Sump Watch post: CANON-EXPANDED | b1 ch.9:37 |
| The gas: "one single, accidental spark from your heavy armor" | CANON (played here only, shared rules) | b1 ch.9:139 |
| The Skittering Dark: dry claws on stone ahead, "not a regular rat" | CANON (name and sound); what makes it: GAME-ONLY | b1 ch.9:191-201 |
| Raw geode ore "painstakingly mined in the Deep Veins by the penal legions"; geodes are fossilised memory; polished ones show faces | CANON | b1 ch.3:133-137; world_bible |
| "Seven years. They gave me seven years in the deep dark" (for bread) | CANON (the sentence). Kara herself is not used: she is found in the Foundry, and the game does not say where her years were served | b1 ch.12:133 |
| **The penal legion in irons**: the convicts go in leg-irons and chains, the old way, not the Foundry's collars of light | CANON-EXPANDED (the legion, b1 ch.3:133; the collars and tethers are played in the Sludge-Tunnels only, shared rules) | b1 ch.3:133; ch.12:133 |
| The holding pens beneath the Warren District | CANON (the pens); the legion cutting them now: GAME-ONLY | b1 ch.24:21 |
| The Warrens: tunnels, cellars, collapsed sewage lines; cold "from the stone itself"; blind pale cave-bats | CANON | b1 ch.6:41, 125-161 |
| The Rust & Ruin; the Warrens' silent cant | CANON | b1 ch.24:77; ch.8:41 |
| Goblins, the Weavers of the Warrens: once the High City's record-keepers and light-catalogers; Vibration-Locking | CANON | world_bible 86-87; b1 ch.1:13; ch.3:85 |
| Razzle, Rill, Chieftain Grohl | CANON; all alive; none comes into the dungeon; givers only on Chris's yes | b1 ch.9:15-23; ch.3:85; ch.20 |
| The Council counts "the Orcs, the Goblins, the Ogres" as disposable manual labour | CANON | b1 ch.16:353 |
| Iron "actively eats the weave" | CANON | b1 ch.12:59 |
| The Iron Pact | CANON | b1 ch.2:131 |
| Aether-Echoes: in the old basalt, sound turns the air to glass spikes | CANON (the phenomenon). The Echo Gallery is GAME-ONLY. **The Vault of Whispers itself is not used:** it is the High Council's vault behind a wine cellar, robbed in Book 1 | b1 ch.8:5-15 |
| The First Era aqueduct under the Warren District, pale seamless stone; its access hatch leads toward the Spire | CANON | b1 ch.25:5, 99 |
| The High-Sovereign Transit Hub: "the junction for every soul-prism and aether-geode entering the High City" | CANON | b1 ch.1:179 |
| Aethelgard and "The Greater Good"; the Council's creed of efficiency | CANON (named only, on seals and orders) | b1 ch.12; ch.13:185 |
| The author's map names: the Toxic Flow, Sewer Beasts, the Ossuary of the Forgotten, the Gutter Maw, giant rats, a spider; the Drowned Cisterns, the Old Shafts (a timber headframe), the Bioluminescent Grotto, the Smuggler's Dock | CANON (names; what each holds is GAME-ONLY). "The Gutter Maw" stays a label on the map: the beast in play is the Bread-Eater, and "Maw" is never said (shared rules) | maps/deep_veins_map.png; maps/deep_veins_map_1771814087069.png |
| The Salt-Menders | CANON-EXPANDED | LORE_AUDIT; historical_evidence |
| The Bread Drop, the Undercroft, trusties, the Half-Bound (as a name), the gang-bars, the Count-Hall, the Hole, the Gullet, the keep, the Count-Master, and every boss, mob, person, item and quest named below unless marked | GAME-ONLY | this doc |

**The rules this design keeps:**
- **Before Book 1, and near it** (shared rules).
  - The pens stand empty: the harvest is foreshadowed, never shown.
  - The Toll-Collector lives to take Valen's button.
  - The Blind Crow stands unbroken.
  - No canon person appears or dies. Aethelgard appears only as a seal.
  - Valen's aunt's face (b1 ch.9:169) is never named or found.
- **No machines (0.05).** Everything is worked by a hand or a beast:
  - the headframe's hoist and the pens' hoist are wound by convicts;
  - ore goes up in baskets passed hand to hand, and in carts pulled by pit mules;
  - the gaol's grille is wound by hand, and its gang-bars are drawn by hand;
  - the keep's gate is broken by a ram that twelve people carry;
  - the smugglers' skiff is poled.
  - The Iron-Golem with a saw (b1 ch.9:71) and the Overseer Golems (b1 ch.12) are left out.
  - **Stone-flesh is magic, and the text says so.** Nothing in this dungeon moves, turns or runs by itself.
  - No player text says engine, machine, gear, lift, grid, battery or crane (shared rules).
- **Names.**
  - "The Warrens" are the tunnels (b1 ch.6) and "the Warren District" is the streets over them (b1 ch.24-25).
  - The Bread Drop and the drains are in the Warrens. The gaol, the keep and the pens lie under the Warren District.
  - ROADMAP's "unmade labour" is read as the Half-Bound; "Unmade" stays the Wasting's word.
  - No names from other games; "Alliance" is not used (the game has the Salt-Menders); canon's modern words (searchlights, turrets, "neon") are not used; never "the Maw".
- **Left to other designs** (shared rules: one owner each).
  - The Bone Cathedral and its Ash-Walker nest (b1 ch.10) belong to the Sludge-Tunnels.
  - **The Foundry's labour riot belongs to the Sludge-Tunnels (Down Tools). Coordinated on 2026-10-14:** Down Tools is a riot on a works floor, with no cells, no escort, no waves and no boss rising: it weakens the floor and costs the quiet door. The Rising here is a gaol's: cells, an escort of the ram-gang, a siege at a gate with sallies, and a captain at half.
  - The red-brick sewer wing, the Indentured's collars and tethers, the Spent and the Iron Teeth belong to the Sludge-Tunnels. Here the convicts go in irons, and the Dock is the Iron Pact quartermaster's.
  - Seekers, Inquisitors, Redaction and Ordinance 9 belong to the Transit Hub. None is placed here.

## 2. What we took from each classic dungeon (lessons only; no names, layouts or assets)

**Chris's brief, 2026-10-14 about 01:30 (verbatim; recorded in 0.4):** "i think adit dungeon need to be more complex like wow's wailing caves or the deadmine. adit is nothing like those".

The Deep Veins is drawn complex from the start:
- **Branching:** a hub (the Landing) with three branches, each with its own job (below), and five side areas off the main way.
- **Loops:**
  - the rats' crawl from the Grotto back to the cistern;
  - the pens' hoist back up to the Bread Drop.
- **Levels:**
  - the rim walk 25 m over the Drop;
  - the Hole seen from its ramp head 10 m above;
  - the Pens seen from the study stair's head 8 m above;
  - a floor that falls 55 m in stages.
- **Choices that change the run:**
  - pay the toll or refuse it (once the rats exist);
  - the quiet or the loud way into the gaol;
  - which branch you need, and which you take for the full clear.
- **Set pieces:**
  - the toll;
  - the line where the old stone becomes basalt;
  - the Rising;
  - the Pens' reveal;
  - Set in Stone in the finale.

**The bar the Adit set (the brief's three models, section 0), checked** (the dungeon-brief rule: every dungeon against all three):
- **The mine with the ship at the bottom:**
  - the story told outside first: the families' bread on the grates, the urchins' chalk, the Salt-Menders' post;
  - a real mine: the Landing under the Old Shaft's headframe, and the Violet Seam's stopes;
  - the reveal: the Pens from the head of the study stair;
  - a gate opened by doing something in the world, quiet or loud: the Shift-Gate (the quartermaster's postern key, or a lantern over the gas);
  - bosses that each teach one thing; patrols that walk in behind you after them;
  - the last boss carries the paper that starts the next chain: the Count-Master's writ;
  - a hidden boss (the Bread-Eater) and a rare (Bellman, the Count's Hound);
  - a set across the dungeon.
- **The cave of the four lords:**
  - an approach outside with its own mobs: the Bread Drop and the Watch's picket;
  - branches in any order off a hub, each obvious: the Landing's three ways, lit and chalked;
  - an escort with waves and a surprise boss: the Rising's ram-gang, its sallies and Captain Hardwick;
  - mobs that run for help or raise the alarm: the Watch's bell, the roll-callers' whistle;
  - corrupted creatures: the mould-crawlers in the face-walls, the basalt crawlers of the Skittering Dark, the Half-Bound;
  - a shortcut back: the rats' crawl, and the hoist out.
- **The zealots' house:**
  - a stronghold of stone with rooms you know at a look: the keep's gatehouse, drill yard, great hall, roll-room, cellar and study;
  - packs that heal each other: the Binder's acolytes;
  - wings that are each a short run to their own boss: the three branches;
  - a secret: the strongbox behind the cellar's racks, its key in the study's desk.
  - **Not taken:** its finale pair of a commander and a priestess who raises him. That is the Adit's Ember Vent (shared rules).

**Other models** (lessons only):

**The prison city under the black mountain** (the long one with the gaol block and the vault):
- **You walk through the place where the enemy lives and works:** a whole order with its own rules, not a lair.
- **A gaol block:** cells down both sides, a warder who carries the keys, and prisoners you free who matter later (the Rising).
- **A house you can deal with instead of fighting, where the choice changes what comes later:** the toll, which the union pays back in the finale.
- **Many bosses, each with its own room and trick, and a strongbox behind a secret:** the cellar.
- **Its weakness:** two to three hours, and a maze. Ours is one line with side ways, with blue chalk at every turn.

**The dig with the stone keepers** (the excavation that breaks into older halls):
- **Diggers cutting into something far older than they are:**
  - the line where the new stone stops and the old begins (the Junction);
  - the Breach, where the legion's picks meet the ancient drains.
- **Stone that wakes:** the Man in the Wall tears out of a face, and the Count-Master sets the living in stone.
- **A story read off old stone:** the faces, and the Roll of '92.
- **Its weakness:** long, dull corridors. Every stretch here is dressed, and the light changes wing by wing.

**The desert city's stair** (the temple stair with the caged prisoners):
- **Free the caged, and the enemy comes in waves while they fight their way out:** the Rising, with its ram-gang and two sallies.
- **The keeper of the cages carries the key:** Chief Warder Hawise Stour carries the Key of the Count.
- **An approach under the open sky:** the Bread Drop.
- **Its weakness:** its stair event overwhelmed groups that did not see it coming. The Rising shows a count and has only two sallies.

**The keep on the hill** (the haunted keep):
- **Freeing the prisoners opens the way into the keep.**
- **The keep's rooms are each known at a glance** (a gatehouse, a yard, a great hall, a roll-room, a study), walked one after another.
- **Its weakness:** it was short and went one way. Here the keep is the third kind of place, not the whole dungeon.

**Not taken from them** (each would read as a copy): an arena where the keepers loose their beasts on you and then their champion; a great beast called up out of the water by an act of your own. The Hole is a side room with a mini-boss, and the Bread-Eater is found, not called.

**Kept from the Sealed Adit and the group runs (0.06-0.08; shared rules):**
- The leader waits for the party, and for you at every gate, saying what it wants.
- Every named elite holds the party (stun, root, fear or disorient on everyone near it or on a member it names; sims too).
- No single blow takes over 60% of a member's health (EncounterEnemy.MaxBlowShare). The gas blast and the glass spikes take a third.
- Only the last boss is `boss:true`, so the scorecard opens on him.
- Every named elite drops one named piece every time (0.075).
- Patrols walk in behind you after bosses (deathCall).
- There is a quiet way and a loud way in.
- There is a hidden boss and a rare.
- A set drops across the dungeon.
- There are spirit stones, each 20 m from any camp and off any patrol's road, and a way out at the end.
- The story is given outside, and the last boss drops the letter that leads to the next dungeon.
- No camp that nobody can reach, and no way narrower than 10 m where the leader walks.

**What the Deep Veins does not repeat from the Adit:**
- **The three branches are not three keys for one gate. Each does a different job, and the text says which:**
  - the Violet Seam is **needed** (its ganger keeps the Breaking-Iron, the wedge the ram needs);
  - the Smuggler's Dock **changes the way in** (its quartermaster carries the postern key, the quiet way);
  - the Skittering Dark is **for the full clear** (loot, a quest, the Grotto and the way back).
- The loud way is canon's gas, carried down in a shuttered lantern, not powder.
- The escort is a ram-gang at a keep's gate, not a lock-matcher.
- The last boss sets your own people in stone, and you break them out.

**New for the mid band:** interrupts matter. They answer Wyle's bell, the roll-callers' whistle, Ruth Tallow's Lay On, the acolytes' Mend Stone and the Binder's Petrify.

## 3. Level and length

- **Levels** (mid band, 15-25, at its top; the Sludge-Tunnels are 18-20):
  - trash 23 on the Bread Drop and in the Undercroft, 24 from the Hall through the gaol, 25 in the keep and the Pens;
  - Wyle 23;
  - the Man in the Wall, the Toll-Collector and the branch bosses 24;
  - Stour and the Breaker 24; Hardwick and the Binder 25;
  - the last boss 25, elite, `harder`, `boss:true`;
  - the hidden boss 26;
  - the rare 25.
- **Party:** five, 1 tank, 1 healer, 3 damage or support: you and four others, Mira among them or not (shared rules: a party of five). MaxPartySims is 4 and counts sims only; the dungeon's door holds the party to five. The run tool takes you and four sims.
- **Fill** (held to section 0's 45-60 minutes, shared rules: run length):
  - **52 camps and about 115 mobs on a full clear with the toll paid**, plus the hidden (1) and the rare (1): **54 in all**, against the Adit's 67 and 148. Fewer camps than the Adit, more to do between them;
  - 7 patrols of two;
  - 10 elites on every run. Up to 3 more: the Toll-Collector (or the Bailiff) if you refuse the toll, the Bread-Eater if you find it, and Bellman one run in five;
  - **events add:**
    - the Rising's two sallies (10), and Hardwick with her two guards (3);
    - the toll's swarm (20) and its six union rats, if refused (only once a rat model exists);
    - the Skittering Dark's union camp (4), if refused (the same);
    - three clutches (9), each only if left unstamped.
- **Size:** about 1.45 km of cave and built hall: the root 905 m and six branches 540 m. The Adit is about 1.35 km.
- **Time,** at the Adit's measured pace (66-67 camps in 71 and 78 game-minutes on the 2026-10-13 group runs, about 1.1 a camp with fights, bosses and events included):
  - **a sim-led full clear: about 57 minutes.** The sim leader runs every cave in plan order and takes every camp it can see, so a sim-led run is always the full clear; it is inside section 0's 45-60. It never pulls the Gullet's bars, so it does not meet the Bread-Eater.
  - **everything** (the Gullet's Bread-Eater too): about 60.
  - **a player-led main line:** about 45 minutes paying the toll, going in loud and leaving the Ossuary, the Dock, the Skittering Dark, the Hole and the Gullet. About 49 with the Dock and the quiet way.
  - **a short run:** about 25 minutes for the Bread Drop to the Landing and one branch, then out. From the Skittering Dark, the rats' crawl takes you straight back to the cistern.
  - **If a group run's full clear passes 60:** trim the Pens' work-gang first, then the keep's household, then the Seam's basket-line.
  - **Spirit stones:** the Landing's, and the keep's in the gatehouse passage. You wake at the deepest one you have touched (shared rules: built with the first of the three).

**The pace, stretch by stretch** (the minutes are estimates for the group run to measure):

| Stretch | Camps | Min | What there is to do |
|---|---|---|---|
| [0] The Bread Drop | 1 | 1 | the givers on the rim walk; the rope stair down; take a loaf (for Kit); the Watch picket; the Spire's Shadow |
| [1] The Undercroft and the Junction | 5 | 5 | take a dark-lantern at the Watch post; the lantern-men's oil; interrupt Wyle's bell; his key to the grille; the line where the old stone becomes basalt |
| [2] The Ancient Drains and the Hall of Silent Faces | 4 | 4 | Pale Stone; crawlers out of the face-walls; the chalked face (a rubbing); the Man in the Wall |
| [2a] The Ossuary and the Echo Gallery | 1 | 2 | the dumped dead; walk the gallery quietly for the Roll of '92 |
| [2b] The Weeping Angel | 0 paid / 1 refused | 1 / 5 | the toll: pay in silver, or (once rats exist) refuse and hold the cistern against the swarm |
| [3] The Breach and the Landing | 5 | 5 | NO FLAME; the convict chain; the first Half-Bound; touch the spirit stone; read the chalk at the three mouths; the gate-warden's post |
| [3a] The Violet Seam (needed) | 5 | 5 | salt five seams; mine raw geode; the gas bay; interrupt Ruth Tallow's Lay On; the Breaking-Iron; a cache |
| [3b] The Smuggler's Dock (the quiet way) | 5 | 5 | the causeways over the Toxic Flow; the bucket-chain; crossbowmen; Quartermaster Thrupp and the postern key |
| [3c] The Skittering Dark (the full clear) | 4 | 4 | stamp three clutches; the Clatterer; the Grotto's herbs and cache; the rats' crawl back |
| The Shift-Gate | 0 | 1 quiet / 2 loud | open the postern with the key, or open the lantern over the gas |
| [4] The Count-Hall | 6 | 6 | interrupt the roll-callers; carry Kit his loaf; Stour and the Key of the Count; the rare hound |
| [4a] The Hole | 3 | 3 | the gaol's dogs; the Breaker; Haska's grille |
| The Rising | 2 | 4 | draw the gang-bars; fit the Breaking-Iron to the beam; walk the ram-gang; two sallies; Hardwick rises |
| [5] The Count-Master's Keep | 6 | 6 | the gatehouse and the second spirit stone; the yard and its crossbowmen; the Binder and his acolytes in the great hall; Clerk Wimbrel in the roll-room; the strongbox |
| [6] The Pens | 5 | 5 | the reveal from the stair head; the work-gang; the Count-Master and Set in Stone; the hoist out |
| **Full clear** | **52** | **about 57** | |
| [6a] The Gullet (hidden) | 1 | 3 | read the families' notes; pull the loose bars; the Bread-Eater |

**When the cap passes 22, these follow:**
- the zone's band;
- quest `minLevel` 22;
- the sims' cap (SimAdventurers);
- talents;
- balance targets for 23-25 (set for 15 only today);
- the run tool's fixed level 11 (RunCapture), set to 24.

## 4. The layout

```
 THE BREAD DROP (the zone's open ground: a sinkhole open to the smog)
   the rim walk, 25 m up: the quest givers; a tenement back door (the zone line to the Warrens)
   │  the rope-and-plank stair down the sinkhole wall
   the floor: families' bread on the drain grates; the Sump Watch's picket
   │  the outfall arch
 [1] THE UNDERCROFT ── an old cistern's rubble-stone vaults, the Watch's lanterns     take a dark-lantern at the Watch post
   │  boss: Sergeant Alger Wyle (interrupt the bell)      gate: the Sump grille (his key)
   │  THE JUNCTION: three brick sewers pour into the black whirlpool; the old stone ends, black basalt begins
   ▼
 [2] THE ANCIENT DRAINS ── Pale Stone (the walled-up First Era aqueduct, seen, not opened)
     THE HALL OF SILENT FACES ── a basalt nave; ten thousand faces; the moss's heartbeat
   │  mini-boss: the Man in the Wall        side: [2a] THE OSSUARY OF THE FORGOTTEN → the Echo Gallery (secret)
   ▼
 [2b] THE WEEPING ANGEL ── the drowned cistern; the Toll-Collector: PAY (or, once rats exist, REFUSE)      gate: the rats in the arch
   ▼  THE BREACH: the legion's picks break into the old drains; "NO FLAME" cut over the way
 [3] THE LANDING ── the mine's foot under the Old Shaft; the convict chain; SPIRIT STONE; the urchins' chalk at each mouth
   ├──► west, violet  [3a] THE VIOLET SEAM (needed)        boss: Ganger Ruth Tallow → the BREAKING-IRON
   ├──► east, yellow  [3b] THE SMUGGLER'S DOCK (quiet way) boss: Quartermaster Selwyn Thrupp → the POSTERN KEY
   ├──► south-west    [3c] THE SKITTERING DARK (full clear) boss: the Clatterer → the Grotto; the rats' crawl back to the cistern
   │  THE SHIFT-GATE ── quiet (the postern key) or loud (a dark-lantern opened over the firedamp sump)
   ▼
 [4] THE COUNT-HALL ── the gaol: cells both sides, cold white lamps, the warder's cage      rare: Bellman, the Count's Hound
   ├──► east  [4a] THE HOLE ── the oubliettes; the gaol's dogs; the Breaker; Haska
   │  boss: Chief Warder Hawise Stour → the KEY OF THE COUNT
   │  EVENT: THE RISING ── the gang-bars drawn (key) and the ram made (iron); the ram-gang to the keep's gate; two sallies; Captain Brunna Hardwick rises
   ▼
 [5] THE COUNT-MASTER'S KEEP ── white stone and gold in the black: gatehouse (SPIRIT STONE), yard, great hall, roll-room, cellar, study
   │  boss: the Binder, Severin Lisk          secret: the strongbox
   ▼  the study stair: the Pens seen from above
 [6] THE PENS ── rows of empty pens, iron rings, no beds; the Half-Bound at work
      LAST BOSS: the Count-Master (Set in Stone: break your own out)
      way out: the pens' hoist, wound by the freed convicts, up to the Bread Drop
   └──► south-east [6a] THE GULLET (hidden): the loose bars of the grate where the Toxic Flow runs out; the Bread-Eater
```

**One rule for the whole place:** from the Breach down, no flame.
- Below the Breach the light comes from the geodes, the moss and the lamps of cold geode-light.
- The one exception is the Count-Master's keep: its cavern is high and clear of the gas, and he likes candles.

**The caves.** Plan rows as in the Adit (x, z, half-width, height, drop below the mouth). A new `tools/wip/dungeon/veins_layout.py`, copied from `adit_layout.py`, writes them, every camp's centre (from cave, along and aside) and every patrol route.

| Stretch | Along (m) | Half-width (m) | Height (m) | Floor below the mouth (m) |
|---|---|---|---|---|
| **Root cave "The Deep Veins"** | | | | |
| The outfall arch (the mouth, in the Bread Drop's south wall) | 0-20 | 5 | 8 | 0 to -1 |
| The first vault (the Watch's post) | 20-55 | 9 | 9 | -1 to -3 |
| The long vault (a stone stair at 75-89: 5 m down, 20 degrees) | 55-110 | 6 | 7 | -3 to -8 |
| The grille room (Wyle; the Sump grille at 128) | 110-130 | 11 | 9 | -8 to -10 |
| The Junction (the whirlpool; the threshold line at 160) | 130-165 | 17 | 16 | -10 to -14 |
| The Ancient Drains (Pale Stone at 190) | 165-215 | 5 | 6 | -14 to -18 |
| The Hall of Silent Faces | 215-325 | 11 | 30 | -18 to -24 |
| The Weeping Angel (the rats' gate at 382) | 325-385 | 24 | 18 | -24 to -28 |
| The Breach | 385-430 | 5 | 6 | -28 to -36 (10 degrees) |
| The Landing (spirit stone at 434) | 430-515 | 28 | 24 | -36 to -40 |
| The shift-way, the firedamp sump (525-535) and the Shift-Gate (538) | 515-545 | 6.5 | 8 | -40 to -42 |
| The Count-Hall (the warder's cage at 640) | 545-680 | 13 | 16 | -42 |
| The gatehouse (the keep's gate at 688; the spirit stone in its passage at 692, just inside the gate) | 680-695 | 9 | 14 | -42 |
| The keep's cavern: yard 695-735, great hall 735-760, roll-room 760-778, cellar 778-784, study 784-800 | 695-800 | 26 at the yard, 16 at the great hall, 12 at the roll-room and cellar, 11 at the study | 30 | -42 to -44 |
| The study stair | 800-820 | 5 | 6 | -44 to -52 (22 degrees) |
| The Pens | 820-905 | 32 | 24 | -52 to -55 |
| **Branches** (each opens out of its parent's floor) | | | | |
| [2a] The Ossuary of the Forgotten (off the Hall at 270, east); the Echo Gallery is its last 22 m | 55 | 7; the gallery 5 | 9; the gallery 12 | -21 to -26 |
| [3a] The Violet Seam (off the Landing at 445, west then north-west): drive 0-35, first stope 35-80 (the gas bay at its side, 75-90), drive 80-110, second stope 110-150 | 150 | 5 in the drives, 14 in the stopes | 6 / 10 | -37 to -50 |
| [3b] The Smuggler's Dock (off the Landing at 475, east): the drain-arch 0-20, the causeways 20-85, the Dock 85-130 | 130 | 5; 12; 20 | 8 / 10 / 14 | -38 to -46 |
| [3c] The Skittering Dark (off the Landing at 505, south-west): the drive 0-40, the old drain 40-75, the lair 75-100, the Grotto 100-120 | 120 | 5; 8; 16; 10 | 6 / 8 / 12 / 10 | -39 to -48 |
| [4a] The Hole (off the Count-Hall at 610, east): the ramp 0-25 (10 m down, 22 degrees), the round chamber 25-55 | 55 | 5; 16 | 16 | -42 to -52 |
| [6a] The Gullet (off the Pens at 902, south-east, through the grate): the drain-crawl 0-15, the sump 15-30 | 30 | 5; 8 | 6 / 8 | -55 to -57 |

**In plan view:**
- The root runs south from the outfall to the Junction, then south-south-west through the Hall to the Angel. The Breach turns south-west to the Landing. The root then runs south through the gaol and the keep, and the Pens bend south-east.
- The Ossuary goes east off the Hall.
- The Violet Seam goes west, then north-west, more than 100 m clear of the Angel.
- The Dock goes east, 80 m south of the Angel.
- The Skittering Dark goes south-west, 100 m clear of the Count-Hall.
- The Hole goes east off the Count-Hall, 150 m south of the Dock.
- The Gullet goes south-east off the Pens' low end, clear of everything.
- No branch has a branch of its own.
- No two caves cross in plan view (FloorAt is 2D). The layout script checks this.

**Rules from the runs, for every camp (0.07 and the shared rules; the Adit's 26 warps and its lava-ledge stall):**
- **No way narrower than 5 m half-width (10 m wall to wall) anywhere the leader walks.** In this design the narrowest are the outfall arch, the Ancient Drains, the Breach, the Seam's drives, the Hole's ramp, the study stair and the Gullet's crawl, all at 5. The Echo Gallery is 5 at the floor, with its walls leaning in; it has no camp, so the leader does not go in.
- **No floor steeper than 25 degrees** (34 is the limit). The steepest are the study stair and the Hole's ramp, at 22.
- **No camp on a ledge, plinth, column top or stage.** Draft B's two rat camps "on the ledges" of the Hall are gone. The Landing's timber stages carry only non-hostile convicts.
- **No mob fights from a roof.** The Rising's crossbowmen and the keep's crossbowmen stand in their yards.
- **The two spirit stones** (the Landing at 434; the gatehouse passage at 692) each stand at least 20 m from every camp's centre and off every patrol's road. The layout script checks it.
- Camps are spread over wide floor, and mobs stroll their own ground.
- Respawn 900 s.

**The built halls: D9's Hall, no room builder needed.**
- ZoneBuilder.Adit's `Hall(s0, s1, w, ...)` already builds walls, end walls, doors and columns along a stretch of cave. It puts each wall where the rock stands at least w off the way: `h.Half[i] * .82f >= w`.
- So every built room here is a Hall run, and each is checked against that rule:

| Room | Along | w | Courses (2 m each) | Cave half-width there | Half x .82 | Holds? |
|---|---|---|---|---|---|---|
| The Count-Hall | 545-680 | 10.5 | 3 | 13 | 10.66 | yes |
| The keep's gate wall (EndWall with the Entrance2 arch and a ZoneGate) | 688 | 7 | 4 | 9 | 7.38 | yes |
| The yard | 695-735 | 20 | 3 | 26 | 21.32 | yes |
| The great hall | 735-760 | 12 | 4 | 16 | 13.12 | yes |
| The roll-room | 760-778 | 9 | 4 | 12 | 9.84 | yes |
| The cellar | 778-784 | 8 | 2 | 12 | 9.84 | yes |
| The study | 784-800 | 8 | 3 | 11 | 9.02 | yes |
| The Shift-Gate wall (EndWall, a grille and a postern) | 538 | 5 | 4 | 6.5 | 5.33 | yes |

- **A new Cell helper:**
  - kit-wall dividers (ModularStoneWall turned across the wall) every 4 m, 3 m deep;
  - the kit's **Bars** (in Resources/Props/Dungeon, unused today) across each front, with a hasp for the gang-bar;
  - a plank bed, a bucket, a chalk tally and a wall-ring with irons inside.
  - It builds the Count-Hall's cells.
- **The Pens' rows:** a small dressing helper lays low one-course kit walls in rows, with aisles 10 m wide along and across (shared rules: the leader walks them). Camps stand in the aisles.
- **Everything else is cave with dressing:**
  - the Undercroft is a rubble-stone lining with squat piers (a new lining);
  - the Hall of Silent Faces is a face-relief lining on the rock, with no walls built;
  - the Ossuary's arch is the kit's Entrance worked with Bones;
  - the Hole's oubliettes are iron grilles laid over blocked pits.

**Readable layout:**
- **The urchins' blue chalk** (CANON: blue chalk means safe; using it as way-marks is GAME-ONLY) marks every turn.
- **At each branch mouth the chalk says what the branch is for:**
  - a wedge at the Seam;
  - a key at the Dock;
  - a skull at the Dark.
- Each mouth shows its own light. From the Landing's spirit stone you can see all four lights.
- From the Breach down, the Toxic Flow's green channel runs along one wall to the Pens: you follow the green down.

### [0] The Bread Drop (the zone's open ground; a GAME-ONLY place in a CANON district)
- **The place:** a sinkhole about 70 m across in the Warrens, where an old cistern roof fell in.
  - It is open to the smog.
  - The Spire's iron pillars and the underside of the High City loom in the murk (the backdrop).
- **Two levels:**
  - **The rim walk, 25 m up:** old cistern paving under the backs of tenements, washing lines and back doors.
    - You arrive here. The quest givers stand here as residents (`life.residents`: fixed posts, no sleep), as Oakhaven's Quill does.
    - A tenement back door on the rim walk is the zone line into the Warrens. Until that zone is built, it leads to the 16-30 road's first built zone, and the run tool zones straight in.
    - When the Warrens is built, the rim walk becomes its corner. Pottle and Sprat move to the Blind Crow, and Morga to the Rust & Ruin.
  - **The rope-and-plank stair:** broad timber flights in switchbacks down the sinkhole wall, 22 degrees and 10 m wide (the leader walks them: shared rules). They are hand-built, with rope rails.
  - **The floor:** the Drop itself, the Watch's empty hut and the outfall.
- **The Drop:**
  - Families with someone in the deep dark come here and drop bread, wrapped in cloth, through the drain grates. Names are chalked on the brick beside stubs of candle.
  - The Watch lets the bread fall, and takes a coin to shout a name down the drain.
  - Loaves lie on the grates. **A cloth-wrapped loaf** can be taken once a visit (E): "You take a loaf. Someone below won't get it, unless you carry it down to him." Kit Breward's is one (quest 1).
- **The mouth:** an old cistern's outfall arch, 8 m high and 10 m wide, in the floor's south wall, breathing cold air.
- **Camp (23):** the Watch picket at the outfall: watchmen x2. Their hut stands empty: its men are on the picket.
- **Secret (vista): The Spire's Shadow.** From the rim walk's far end, the Spire's pillars show through a gap in the smog.
- **Small life:** crows on the washing lines (the Living Birds).

### [1] The Undercroft and the Junction (root, 0-165)
- **The place:** the vaulted undercroft of the old cistern whose roof fell in to make the Drop. It is older than the Lowtowns' red brick (the red-brick sewers are the Sludge-Tunnels', shared rules).
  - Low vaults of grey rubble-stone on squat piers, slick old flags underfoot.
  - A channel of black water runs down one side. It is blocked, and nobody swims in a cave.
  - Rusted iron ladders climb into nothing (decor).
  - The Watch's lanterns stand on posts: warm orange light on wet grey stone.
- **The Watch post** (the first vault, 35): a table, a rack of coats, a brazier, and **a rack of shuttered dark-lanterns**.
  - E "Take a Watch dark-lantern" is the loud way's pick-up: the Adit's powder keg, made per zone. You carry one at a time.
  - "Shuttered, it gives no light and no spark. Keep it that way below the Breach."
  - The lantern-men drop one too.
- **The Junction** (130-165): a round chamber 34 m across and 16 m high.
  - Three red-brick sewers pour into a black whirlpool basin: the only brick in this dungeon, the Lowtowns' sewers coming in from above (CANON). The basin is a dark sheet turning slowly, blocked like the Adit's lava lake, and the path rings it. Two of the sewer mouths are choked, for show.
  - On the far side the old stone stops dead at a line across the floor and walls, and black basalt begins: one arch cut in severe angles.
  - **Stepping over the line:** "The stone changes under your feet. It is older than the city."
- **Camps (23):**
  - the Watch post: watchmen x3 at 35. Watchmen drop **Watch buttons** (25%), the silver for the toll;
  - lantern-men x2 at 98. Their burning-oil lamp is a bolt cast, and they drop a dark-lantern;
  - Wyle's watchmen x2 at 118;
  - Wyle at 122;
  - sewer beasts x3 at 150, in the Junction (the map's name; pale, hairless, swollen hogs of the drains).
- **Boss: Sergeant Alger Wyle of the Sump Watch** (23, elite).
  - A tidy, bored man who holds the key to the old dark and has never been through it.
  - **Lesson: interrupt the bell** (section 5).
  - He always drops a Watch button and **the Sump Key** ("veins.sump"), which opens the Sump grille into the Junction.
  - When he falls, the lantern-men walk in from behind if they live (deathCall).
- **Small life:** blind pale cave-bats burst from the roof as you pass (CANON, b1 ch.6:161; critters, if a bat can be had).

### [2] The Ancient Drains and the Hall of Silent Faces (root, 165-325)
- **The Ancient Drains** (165-215):
  - black basalt, low, with heavy pointed arches and nothing curved;
  - toxic white mould in patches;
  - rotting crates float in the side channel (decor);
  - the first faces begin in the walls: a few, then more. No camp: the faces are enough.
- **Secret (vista): Pale Stone** (at 190).
  - An arch of pale, seamless First Era stone, walled up in the black: the old aqueduct (CANON, b1 ch.25:5), whose access hatch runs toward the Spire (ch.25:99).
  - It is seen, not opened: a canon road to the Spire, kept for later.
- **The Hall of Silent Faces** (215-325; CANON):
  - A nave 22 m across whose roof goes up out of sight, 30 m.
  - Its walls are faces, thousands of them (a new face-relief lining on the rock). Mouths are open in silent screams, and some faces are warped.
  - The moss over them glows green and swells and fades every 4 seconds, like a slow heartbeat (a light pulse). That pulse is the only light.
  - Condensation drips into the dark like hammer-strikes (CANON; text now, sound later).
  - The urchins have chalked one face in blue, in the middle of the west wall at 268 (quest 3).
- **Camps (23-24), all on the floor:**
  - **mould-crawlers** x3 at 232, an ambush camp at the west wall's foot. Small and white-mould coloured, they wait in niches in the face-walls and come out as you pass;
  - mould-crawlers x3 at 292, an ambush at the east wall's foot;
  - **patrol:** sewer beasts x2 walking 220-320;
  - **mini-boss: the Man in the Wall** (24, elite) at 312, an ambush camp.
- **The Man in the Wall** (GAME-ONLY; one of the Forgotten, CANON, whose binding never fully took):
  - His face is wet when the others are dry.
  - As the party passes he tears himself out of the wall: a grey stone man with moss in his mouth.
  - **Lesson: follow him** (section 5).
  - When he dies he goes back into his face for good, and the face is dry.
  - When he falls, the Hall's patrol walks in.
- **Things to use:** the chalked face (quest 3: E, "Take a rubbing").

### [2a] The Ossuary of the Forgotten and the Echo Gallery (branch, 55 m; a side area)
- **The place:**
  - a basalt arch of skulls, the map's own gate (the kit's Entrance worked with Bones and Bones2);
  - beyond it, a bone-yard: the bones of those the moss did not keep, heaped by some older order.
- **Camp (24):** the ossuary dead x3 at 22: the dumped dead, walking with the magic that pools low. They fall to a heap of bones (the Fantasy Skeleton, as Khaven's drowned dead).
- **The Echo Gallery** (33-55; CANON-EXPANDED: the Aether-Echoes of b1 ch.8; the place is GAME-ONLY):
  - a crack where sound turns the air to glass, 5 m half-width at the floor with its walls leaning in.
  - **The rule watches you alone:** running, casting or striking inside it raises a ground mark of glass spikes under you. Step out, or lose a third of your health.
  - Mira and the sims wait at its mouth. There is no camp in it, so the leader never goes in.
  - At the far end lies a dead clerk with a satchel: **the secret "The Roll of '92"**, a Chronicle page.
    - It holds the names of those taken in the Bread Riots, kept by someone who was counting.
    - The last line is a woman's name, blacked out.
  - The quiet rule can be cut. The Roll then lies at the Ossuary's end.

### [2b] The Weeping Angel (root, 325-385)
- **The Drowned Cistern** (the map's name; the chapter's angel, CANON): a round chamber 48 m across and 18 m high.
  - Its water broke through into the legion's workings long ago. What is left is a black slick round the plinth: painted, ankle-deep to look at, and walkable. The deep middle is blocked.
  - On the plinth stands the weeping stone angel, its wings half sunk, green slime weeping from its blank eyes.
  - On its bowed head sits **the Toll-Collector**, a non-hostile figure.
  - Red eye-pairs glint round the dark edge: the union's six, lying hidden.
  - **The rats' gate:** the far arch toward the Breach is packed with rats.
    - It is a ZoneGate dressed as a living crowd (a nav blocker), and it parts on "veins.toll".
- **Until a rat model exists** (question 5; section 9, step 1), every rat here is shown by its eyes, never its body:
  - the Toll-Collector is a voice in the dark under the angel's wing and a pair of red eyes (light points, no model);
  - the rats in the arch are a seething dark, a shadow on the nav blocker with dozens of red eye-lights moving in it;
  - paying puts every eye out and the dark draws back off the arch;
  - the toll is pay-only: refusing says "A hundred red eyes. You do not like the odds."
- **The leader stops at the cistern's edge:** "The rat wants paying. Your call."
- **E on the angel opens the conversation window: Pay, or Refuse.**
  - Without silver, it says "It wants silver. A Watch button would do."
- **Pay** (anything silver: a Watch button, or Sprat's button from quest 2).
  - It bites the coin, gives a small nod, every red eye goes out (CANON), and the rats in the arch part.
  - It turns its head toward the Landing's south-west drive and squeaks once, sharply. The Skittering Dark is not theirs.
  - Kept: "veins.toll" and "veins.toll.paid".
  - **The union keeps its bargains:**
    - its camps here and in the Skittering Dark are gone for good (`unlessKey`);
    - its crawl from the Grotto back to this cistern stands open;
    - in the Pens, at the Count-Master's first Shift, the union takes his two bodyguards down the drains. With the rat model, rats drag them. Until then, red eyes flood the drain-mouths, the two are pulled backwards into the dark, and the text says "The union keeps its bargains."
- **Refuse** (only once the rat model exists; the plan, if Chris says yes to question 2):
  - The Toll-Collector (24, elite, small and quick) leaps down, and the six rise (`rise` camps).
  - At a fifth of its health it leaps into the rusted pipe and is gone (CANON: it lives to take Valen's button).
  - Then **the swarm**: three waves of Deep-Rats out of the drains (6, then 6, then 8; non-elite, fast and weak), spawned as the Adit's escort waves are. Hold the cistern.
  - Kept: "veins.toll". The union remembers:
    - its rats in the Skittering Dark fight on sight;
    - its crawl stays shut until you search the wall;
    - nothing comes up in the Pens.
  - The Toll-Collector's hoard on the plinth (a chest): bitten buttons, coin and its named piece.
- **The fallback, if Chris says no to fighting a canon survivor:**
  - Refused, the Toll-Collector only squeaks and goes down its pipe at once.
  - **The Bailiff** (24, elite, GAME-ONLY: a Deep-Rat the size of a pony) drops off the plinth with the six, and the swarm's waves come as above.
  - No escape rule is needed.
- **Lesson:** a house you can deal with, and a choice that is paid back later.

### [3] The Breach and the Landing (root, 385-545)
- **The Breach** (385-430):
  - This is where the legion's hand-cut drive broke into the ancient drains: a ragged hole. Beyond it the basalt is rough from picks and propped with square timber sets.
  - Chalk tallies cover the walls: convicts counting their days in fours, with a stroke through.
  - The first violet glints of raw geode show in the rock.
  - **"NO FLAME" is cut deep over the way** (GAME-ONLY: the old warning about the gas).
  - The light turns violet (the geodes), with the cold white of geode-lamps. The Toxic Flow's green channel picks up along the left wall.
- **The Landing** (430-515): the mine's foot, a chamber 56 m wide and 24 m high under **the Old Shaft** (the map's Old Shafts, with a timber headframe).
  - **The shaft:** the headframe stands at the shaft's foot, its ropes going up into the dark. At the top, a gang of convicts winds the hoist by hand.
  - **Ore and the people who move it:**
    - Ore heaps and baskets.
    - **The convict chain:** living convicts in leg-irons (not hostile) pass ore baskets hand to hand up timber stages on the wall and into the shaft, a long moving line. The stages are dressing; nobody fights up there.
    - A bailing gang passes buckets up from the sump.
    - Pit mules pull ore carts on a hand-laid track (the Donkey critter).
  - **The legion's post:** the trusties' bench, and gaolers' lamp-posts with cold violet-white geode lamps and no flame.
  - **The spirit stone** (at 434, by the north-east wall where the Breach comes in): a stone the convicts touch for luck as they pass, worn smooth (GAME-ONLY). The nearest camp is 31 m off, and the trusties' road turns 26 m short of it.
  - **Corrie Fisk:** an old convict at the bailing gang (a non-hostile resident) who talks to you (quest 1). The convicts cower when a fight starts and come to no harm.
  - **Three ways on, each lit its own colour and chalked with what it is for:**
    - **west, the Violet Seam:** violet light, and a wedge in chalk. **Needed**: its ganger keeps the Breaking-Iron, the wedge the ram needs.
    - **east, the Smuggler's Dock:** the smugglers' hooded yellow lanterns, and a key in chalk. **The quiet way** into the gaol.
    - **south-west, the Skittering Dark:** only the white mould's glow, a gang's tally crossed out, and a skull in chalk. **Nothing you need**: loot, a quest, and the way back.
    - **south, the shift-way to the gaol:** cold white lamps.
- **Camps (24):**
  - **the Half-Bound** x3 at 405 (the Breach; GAME-ONLY, from CANON stone-flesh):
    - They work at the rock until you come near; then they turn on you, slow, the stone up to their elbows.
    - Slow, sturdy non-elites with a stiff walk and grey, cracked skin; moss in the cracks.
    - They were bound for disobedience and work on to the last order given. No collar walks them (shared rules).
  - gaolers x3 at 465, on the Landing's west side;
  - **trusties** x2 at the bench, 495: convicts with gilt collars and whips. Their "Lash", a whip-crack at range, is an interruptible bolt cast;
  - **patrol:** trusties x2 walking 460-510;
  - the gate-warden's gaolers x3 at 520, outside the grille.
- **The Shift-Gate** (515-545): an iron grille in a basalt arch across the shift's way to the gaol (EndWall, four courses), with a postern beside it.
  - **The firedamp sump** lies at its foot (525-535). Painted on the wall: NO FLAME BELOW THIS MARK.
  - **Quiet:** E at the postern with the postern key ("veins.postern", Quartermaster Thrupp's).
    - It is the quartermaster's own door, for the Dock trade. His man on the inside winds the grille up by hand.
    - The Count-Hall's mess sits at table until struck: the hall does not know you are there. Kept: "veins.quiet".
  - **Loud:** carry a Watch dark-lantern to the sump and press E: "Open the lantern over the gas."
    - Four seconds pass ("Get back!"), then the gas goes up.
    - Everyone within 12 m takes a third of their health.
    - The grille is blown out of its arch, and the gaol comes: SendWave sends every non-elite camp within 78 m (the mess and the first roll-callers).
    - Kept: "veins.loud". The loud way needs no branch boss, only nerve.
  - **The leader waits at the shut gate:** "The quartermaster has a key to that postern, or there's the gas. Your call."

### [3a] The Violet Seam (branch, 150 m; NEEDED: the Breaking-Iron)
- **The place:** the geode face.
  - A drive 10 m wide and 6 m high opens into two stopes, 28 m wide and 10 m high.
  - There, seams of raw geode run violet through the black and glow in the walls (CANON: raw, unrefined ore).
  - Gangs of living convicts cut it by hand under trusties, with the Half-Bound cutting beside them. Baskets go back toward the Landing hand to hand.
  - **The gas bay** (75-90), at the first stope's side, is thick with a yellow-green haze (the fog's tint). It is dressing, unless Chris says fire spells light the gas (question 4).
- **Camps (24):**
  - Half-Bound diggers x3 at 25;
  - trusties x2 at 50;
  - **patrol:** the basket-line, Half-Bound x2 walking 20-130;
  - Tallow's trusties x2 at 138;
  - **Ruth Tallow** at 145.
- **Boss: Ganger Ruth Tallow, the Trusty** (24, elite; GAME-ONLY).
  - A Lowtown woman given seven years in the deep dark for a loaf, twelve years ago. She took the gilt collar and a cot, and now she drives the face and counts the baskets.
  - **Lesson: interrupt Lay On** (section 5).
  - She keeps **the Breaking-Iron**: the face's great iron wedge, which the trusties keep chained to her bench because it is the one thing down here that could break a gate ("veins.iron", kept for good). The ram cannot break the keep's gate without it.
  - Dying, she says: "Seven years. I did twelve."
  - When she falls, the basket-line walks in.
- **Things to use:**
  - raw geode ore nodes on the floor (ZoneNode.under; the miner's trade; `mat.geode_shard`);
  - five seams marked with a trusty's tally, to salt for the Salt-Menders (quest 4).
- **Secret (cache): A Seam the Trusties Didn't Count.** A clutch of raw geode in a crack, with one polished geode among it that has a face inside (CANON-EXPANDED: polished geodes show faces).

### [3b] The Smuggler's Dock (branch, 130 m; THE QUIET WAY: the postern key)
- **The place:** the map's Smuggler's Dock (CANON name). The Iron Pact company's quartermaster sells the legion's stolen ore here, to buyers who come by water and give no names (GAME-ONLY).
  - Pale, water-worn cistern stone, older work than the gaol's.
  - **The causeways** (20-85): the Toxic Flow (the map's name) runs as a channel of glowing green sludge 4 m wide down the middle. It is blocked like the lava lake and crossed by three stone causeways 10 m wide where the blocker breaks.
  - **The Dock** (85-130):
    - a channel of black water (blocked);
    - a rotting, iron-ringed pier along it, with a black-hulled skiff tied up (the Megapack Boat, poled);
    - crates of raw ore with the legion's brand burnt off;
    - the smugglers' hooded lanterns, dull yellow.
- **Camps (24):**
  - the quartermaster's men x3 at 40: Iron Pact soldiers out of their tabards, guarding the bucket-chain. Bought convicts bail the workings by hand behind them (not hostile);
  - crossbowmen x2 at 65 (they shoot, then close);
  - dock hands x3 at 95;
  - Thrupp's crossbowmen x2 at 118;
  - **Quartermaster Selwyn Thrupp** at 122.
- **Boss: Quartermaster Selwyn Thrupp of the Iron Pact company** (24, elite; the army CANON, the man GAME-ONLY).
  - **Lesson: keep the healer behind the tank: he goes for the back** (section 5).
  - He carries **the postern key** ("veins.postern"): his own door into the gaol, and the quiet way in.
  - When he falls, the dock hands walk in if they live.

### [3c] The Skittering Dark (branch, 120 m; THE FULL CLEAR)
- **The place:** the name is CANON (b1 ch.9: dry claws on stone ahead); what lives there is GAME-ONLY.
  - Two years ago the legion's south-west drive broke into an older drain. The gang that broke through never came back, and their tally is crossed out at the mouth.
  - Beyond lies the oldest basalt: no faces, no moss, only the white mould's faint glow and long white scores on the floor from claws. It is the darkest wing.
- **Camps (24):**
  - the union's Deep-Rats x4 at 15. They are gone if the toll was paid (`unlessKey`) and hostile if it was refused, so they exist only once a rat model does;
  - **basalt crawlers** x3 at 50: long-legged, eyeless, the size of a pony, claws clicking (the crypt spider in a basalt palette, white eyes);
  - basalt crawlers x2 at 72;
  - **patrol:** crawlers x2 walking 20-75;
  - **the Clatterer** at 92.
- **The clutches** (things to use):
  - Three egg-clutches lie in the lair's floor niches (80, 86, 96). Each is a `rise` camp of three crawlings.
  - E for 2 s stamps one. That keeps "veins.clutch.N", and the camp is gone (`unlessKey`).
  - Any clutch left unstamped hatches when the Clatterer calls at 60%.
  - The leader leaves them alone.
- **Boss: the Clatterer** (24, elite; GAME-ONLY), the great crawler, three times the size of the rest. It is what Valen heard.
  - **Lesson: get ready before the pull** (section 5).
  - When it falls, the crawler patrol walks in.
- **The Bioluminescent Grotto** (100-120; the map's name): the secret, with herbs and a vista.
  - The moss blazes aqua-green here: the source of the Hall's light.
  - Moss-light herb nodes and a cache.
- **The rats' crawl** (the shortcut back: the corrupted cave's waterfall lesson):
  - A drain-crawl at the Grotto's back wall comes out at the Weeping Angel's rusted pipe. E and 5 s on the bar, and the party stands in the cistern (the hoist's ride pattern).
  - It is open if the toll was paid (always, while the toll is pay-only). If refused, search the back wall (E) to find it.

### [4] The Count-Hall (root, 545-680; the built prison block)
- **The place:** a hall 135 m long and 26 m wide (a Hall run, w 10.5, three courses of cut stone: 6 m).
  - The rock roof is 16 m up, and the floor is flagged with a gutter down the middle.
  - **Cells along both walls** (the Cell helper) in two blocks, 555-600 and 615-660, about 44 cells. Each is 4 m wide with a plank bed, a bucket, a chalk tally and a wall-ring with irons.
  - **The gang-bars:** each block's cells are shut by one long iron bar run through every door's hasp along the block, padlocked at its end.
  - A second tier of barred fronts shows high on the wall. It is dressing only; nothing stacks.
  - Cold white geode lamps on posts down the middle throw hard bar-shadows across the floor ("every bright, beautiful light": CANON).
  - The count-slate at the far end is chalked with the day's number. Its frame is one great beam.
  - **The warder's cage** (at 640; GAME-ONLY): an iron cage in the middle of the hall where the gang-bars' padlock keys hang on a board, under the warder's own lock.
  - The living convicts sit in the cells in irons, not hostile. One is **Kit Breward** (quest 1).
- **Camps (24):**
  - the mess: gaolers x3 at 555 (seated at table if you came quietly);
  - **roll-callers** x2 at 585 (GAME-ONLY): turnkeys with the count-roll and a tin whistle.
    - "Pipe the Hall" is a 4 s alarm cast that can be interrupted.
    - If it lands, the two nearest camps and the patrol come.
  - roll-callers x2 at 645;
  - **patrol:** turnkeys x2 walking 550-665;
  - Stour's turnkeys x2 at 665;
  - **Chief Warder Hawise Stour** at 670.
- **Rare (one run in five): Bellman, the Count's Hound** (25, elite; GAME-ONLY).
  - A brindled gaol mastiff the size of a pony (the wolf, scaled up and scarred), with a brass name-plate on its collar. It walks the rows with the turnkeys, 555-665, and sniffs each cell.
  - It comes last in the camp list.
- **Boss: Chief Warder Hawise Stour, Keeper of the Keys** (24, elite; GAME-ONLY).
  - Twenty years a warder. She knows every convict's crime and tells you theirs, and yours.
  - **Lesson: pull her clear of the cell rows, or her rallies bring the hall** (section 5).
  - She drops **the Key of the Count** ("veins.count"), which opens the warder's cage.
  - When she falls, the turnkey patrol walks in.
- **The Hole** opens east at 610.

### [4a] The Hole (branch, 55 m; a side area)
- **The place:** the punishment cells (GAME-ONLY): a round chamber 32 m across at the foot of a ramp 25 m long that falls 10 m. From the ramp head you see the whole floor below.
  - **Four oubliettes:** iron grilles set in the floor over pits a man cannot stand up in (dressing: the pits are blocked).
  - Its walls are lined with iron plates, so that a convict with any resonance cannot use the weave (CANON: iron "eats the weave"). This is dressing, with no rule in play.
  - Red-shaded geode lamps.
  - The Breaker's bench, with his chains hung on pegs.
- **Camps (24):**
  - handlers x2 at 28;
  - gaol mastiffs x2 at 40;
  - **the Breaker** (24, elite) at his bench, 52.
- **Haska of the Ridge** (GAME-ONLY; an orc of the Eastern Ridge's Ash-Walkers, a CANON people, taken for labour: CANON "disposable manual labor"):
  - She is in the oubliette beside the Breaker's bench. She does not fight.
  - When the Breaker falls, his key opens her grille (E; the `cage` kind made per zone).
  - She climbs out, tears at her irons and walks up the ramp ("veins.haska"; quest 8).
- **Lesson:** a side room for a quest, a mini-boss and his pieces. Nothing shuts behind the party, and nothing comes in waves.

### The Rising (the event: from the warder's cage at 640 to the keep's gate at 688)
- **What it needs:** the Key of the Count (Stour) and the Breaking-Iron (Tallow).
  - The leader waits at the cage and says what is missing: "We'll not get through that gate without the whole gaol behind us. We need the warder's key and the ganger's iron."
- **Opening the gaol** (E, 3 s at the cage, with both):
  - The key opens the cage, and you take down the gang-bars' keys.
  - The gang-bars' padlocks come off, and the convicts heave the bars out of the hasps from inside. The cell doors swing open.
  - **People open the irons:** the goblins among the convicts hum their leg-irons open, and then each other's (Vibration-Locking, CANON). The convicts come out: a crowd.
- **The ram:** the convicts pull the great beam out of the count-slate's frame and fit the Breaking-Iron to its head. Twelve of them carry it.
  - **The walk:** you walk the ram-gang down the last 45 m, and every party sim guards it, as Mother Quillet.
  - **The blows:** at the gate they swing for about 90 s, 30 blows shown as a count. The gang ducks when attacked.
  - **If the gang breaks,** it drops the ram and runs back to the cage. Speak to it (E) to try again.
- **Sally 1** (at the first blow): four Iron Pact gaol-guards come out of the wicket into the gatehouse yard.
- **Sally 2** (at a third): three Pact crossbowmen and three guards come out of the wicket.
  - The crossbowmen stand on the gatehouse yard's floor, shoot, then close. Nobody fights from the gatehouse top.
- **The surprise** (at half): **Captain Brunna Hardwick** and her two guards come out of the wicket. They have been lying hidden in the gate passage (`rise` camps).
- **Done:** the gate falls. Kept: "veins.rising".
  - The convicts pour into the keep's yard and then, being sensible, out: back up the shift-way toward the shaft. A few stay at the pens' hoist for the end.
- **Lesson:** the stair with the caged prisoners. Free them, waves come, and a captain comes last.
- It is the one gaol rising in the three later dungeons (shared rules): the Sludge-Tunnels' Down Tools is a riot on a works floor with none of this.

### [5] The Count-Master's Keep (root, 680-800; the stone stronghold)
- **The place:** a stronghold of white stone and gold built inside a cavern of black basalt. The Council's taste is carried down into the dark.
  - It is all on one level: rooms walked through one after another, each known at a glance (the Hall runs above).
- **The rooms:**
  - **The gatehouse** (680-695): the curtain wall, four courses, with the Entrance2 arch holding the keep's gate, which the Rising broke.
    - **The second spirit stone:** the Pact's watch-stone, in the gate passage at 692, just inside the broken gate: 22 m from where the Chief Warder stood and 22 m from the yard's first camp.
  - **The yard** (695-735):
    - The Pact's drill yard, with weapon racks and dummies.
    - On three plinths stand **the stone-bound**: convicts the Count-Master has had bound whole in stone-flesh as a warning, still breathing (CANON-EXPANDED: the old method, brought back "for discipline"). They set up his fight.
  - **The great hall** (735-760):
    - a long table set for one, a crimson carpet, a chandelier of gilt geode-lamps, banners of the High City;
    - the kit's Window set high in the walls;
    - **the Binder** keeps his table at its head.
  - **The roll-room** (760-778):
    - shelves of the legion's rolls to the wall-top, every convict a line;
    - lecterns and clerks' desks;
    - **Clerk Wimbrel** (GAME-ONLY; a goblin, of the CANON people who kept the High City's records) is chained to one desk. No boss: the record room with a boss is the Hub's (shared rules).
  - **The cellar** (778-784): barrels and racks. The strongbox stands behind the racks, where the basalt breathes cold.
  - **The study** (784-800):
    - a desk of black petrified wood, wax candles, and a built great window that looks out on nothing but black rock;
    - its back door opens on the stair down to the Pens.
- **Camps (25):**
  - gatehouse guards (Iron Pact gaol-guards: iron-grey half-plate, black tabard, shield and spear) x3 at 714;
  - Pact crossbowmen x3 at 722, on the yard floor behind the dummies (they shoot, then close);
  - **patrol:** a Pact pair walking 716-732;
  - the household: footmen x3 at 742, with the steward among them;
  - the Binder's acolytes x3 at 750, in grey robes with stone-dust on their hands. Their "Mend Stone" heals an ally under 70% and can be interrupted;
  - **the Binder** at 755.
- **Boss: the Binder, Severin Lisk** (25, elite; GAME-ONLY: a mage of the Council; stone-flesh is CANON).
  - He binds the gaol's troublemakers in stone-flesh, a hand at a time, and the Count-Master signs for each. The stone-bound in the yard are his best work.
  - **Lesson: interrupt, and kill or hush the acolytes first** (section 5).
  - The acolytes still alive when he falls run.
  - When he falls, the yard's pair walks in.
- **Things to use:**
  - Clerk Wimbrel's chain (E): she says the rolls are hers, in a hand she was made to write (quest 9);
  - the study's desk, which holds the strongbox key (E).
- **Secret (chest): the Count-Master's strongbox.**
  - It holds coin, a polished geode and a Chronicle page, **"Berths"**: his own note of how many the pens will hold. A number is written, struck through, and written larger.

### [6] The Pens (root, 820-905; the reveal and the finale)
- **The reveal:**
  - Out of the study's back door you go down the study stair. From its head, 8 m up, you see it below: the widest view in the Deep Veins. (Not the biggest space in the game: the Sludge-Tunnels' Crucible Hall and the Adit's Rail Hall are wider. Shared rules.)
  - A new-cut cavern under the Warren District (CANON: "the massive holding pens"), 64 m across and 24 m high.
  - Its far side is lost in the dark. That is done by light and haze, not by size.
- **The look:**
  - Raw grey stone with fresh pick-marks.
  - Rows on rows of pens: low walls of one course, iron rings set in the floor, aisles 10 m wide.
  - **No beds, no buckets, no tallies on the walls: nobody has lived here yet.**
  - Pole-lamps in rows, cold white, going off into the dark.
  - Timber scaffolds against the walls where new pens are being cut.
  - A great slate with a number on it.
  - **Four pillars of uncut rock**, left to hold the roof round the slate.
  - At the low end, the Toxic Flow runs out through a great drain-mouth under a grate.
  - The Half-Bound work in a gang, and their picks are the only sound.
- **Nobody says what the pens are for.** The quest text says only: "There are no beds."
- **Camps (25):**
  - Half-Bound work-gang x4 at 835;
  - gaolers x3 at 855;
  - **patrol:** gaolers x2 walking 830-880;
  - the Count-Master's bodyguards (Iron Pact gaol-guards) x2 at 890;
  - **the Count-Master** at 893, before the slate among the four pillars.
- **Last boss: the Count-Master, Overseer Lucan Kemble** (25, elite, `harder`, `boss:true`; GAME-ONLY).
  - A lieutenant of the High Council, in a white robe edged with gold, holding a rod of grey stone. His writ bears the Grand High Mystic's seal (CANON, named only).
  - "Efficiency. The Lowtowns call it cruelty because they cannot count." (The Council's creed: CANON, b1 ch.12.)
  - **Lesson: break your own out of the stone, then him** (section 5).
  - When he falls, the scorecard opens, and the Pens' patrol walks in (deathCall).
  - He always drops **the Count-Master's Writ**:
    - a sealed order, almost all of it struck through in black;
    - what can still be read: the Veins' geodes go "to the High-Sovereign Transit Hub for sorting", and "berths for ten thousand, ready by the year's turn".
- **The way out: the pens' hoist.**
  - A new-cut shaft goes up to the Warren District, with a rope and an ore basket.
  - After the Rising, freed convicts stand at its windlass. E at the basket and 5 s on the bar, and the party stands on the Bread Drop's floor. It is wound by hand (the Adit's ride-out pattern; the climb is not walked on screen).

### [6a] The Gullet (hidden; branch, 30 m, off the Pens through the grate)
- **The grate** (at 902) is choked with cloth, bread and little folded notes in the families' hands: everything the Bread Drop throws down ends up here, and something eats it.
- **Secret (note): "Down the Drain".** Reading one of the notes is the secret, and the clue: "Kit, it's your mum." Under it, an urchin's blue chalk: two of the grate's bars are loose.
- **The loose bars** (E, 6 s; key "veins.grate"): pull them, and a drain-crawl 10 m wide opens into **the Gullet**, a round sump 16 m across where the Toxic Flow pools before it runs on.
- **Hidden boss: the Bread-Eater** (26, elite; GAME-ONLY. The author's map labels this drain "the Gutter Maw"; the game never says "Maw").
  - A huge, pale, toothed beast half out of the sump's black water at its far side, among drifts of sodden bread and cloth.
  - It never leaves the water's edge.
  - It is found, not called: nothing you do brings it up. The leader never pulls the bars.
  - **Death:** it coughs up bread, cloth and the families' notes.

### Gates, things to use and secrets (all GAME-ONLY unless marked)
- **Gates.** Each key is kept for good in `Progress.keys` under a `veins.*` id. No Adit id is shared, in particular not the Adit's global "adit.lift".
  - the Sump grille (Wyle's key, "veins.sump");
  - the rats' gate at the Angel ("veins.toll");
  - the Shift-Gate: the postern key, or the lantern ("veins.quiet" or "veins.loud");
  - the keep's gate (the Rising, "veins.rising");
  - the Gullet's loose bars ("veins.grate").
- **Things to use:**
  - the dark-lantern rack, and the lantern opened over the sump;
  - silver for the toll, and the conversation at the angel;
  - the loaf (taken at the Drop, carried to Kit's cell);
  - the chalked face;
  - five marked seams, and the ore nodes;
  - the clutches;
  - the rats' crawl;
  - the postern;
  - Kit's cell;
  - the warder's cage and the gang-bars;
  - the ram-gang;
  - Haska's grille;
  - Clerk Wimbrel's chain, and the study's desk;
  - the stone shells in the finale (struck, not used: Set in Stone);
  - the grate's loose bars;
  - the hoist;
  - two spirit stones.
- **Secrets (seven):**
  1. The Spire's Shadow (vista, the rim walk).
  2. Pale Stone (vista, the Ancient Drains; CANON).
  3. The Roll of '92 (note, the Echo Gallery).
  4. A Seam the Trusties Didn't Count (cache, the Violet Seam).
  5. The Bioluminescent Grotto (herb and vista, the Skittering Dark).
  6. The Count-Master's strongbox (chest, the cellar; its key is in the study).
  7. Down the Drain (note, the Pens' grate; it leads to the Gullet).
- **Side areas:**
  - the Ossuary and the Echo Gallery;
  - the Skittering Dark and the Grotto;
  - the Hole;
  - the cellar;
  - the Gullet.

### The crowd (frame rate and navmesh; shared rules)
**The figures:**
- the convict chain on the Landing's stages (12);
- the bailing gang (4) and the pit mules (3);
- the convicts cutting in the Seam (6);
- the Dock's bucket-chain (4);
- the Count-Hall's convicts: one seated in every other cell (22);
- in the Rising, 10 of those walk out, and the ram-gang (12) is drawn from them;
- the stone-bound (3, still), Haska, Wimbrel, Corrie, Kit, and the freed at the hoist (4).

**The budget:**
- **No more than about 40 animated figures in any one view.** The worst is the Count-Hall during the Rising: 22 seated, the ram-gang, the sallies and the party. Next is the Landing from its spirit stone.
- **Seated convicts, the chain and the bailers** get no NavMeshAgent and no collider. Each plays one looped clip, offset in time; Animators are culled off-screen.
- **The ram-gang moves as one:** one agent carries the ram, and the twelve carriers are parented to it with no agents of their own.
- **The 10 who walk out** get agents only while they walk to the shift-way, and are removed there. The other 12 stay in their cells, cheering.
- **The navmesh:** none of these carve it.
- **A frame-time capture** from the Landing's spirit stone before the Landing is dressed further, and in the Count-Hall at the Rising's busiest moment. If either is worse than the Adit's Rail Hall, the seated convicts drop to one cell in three (15) and the chain to 8.

## 5. Bosses and mobs (all GAME-ONLY people and creatures unless marked)

### 5a. Bosses
Every named elite holds the party, sims too, on everyone near it or on a member it names (shared rules), and no blow takes over 60% of a member's health.

| Boss | Where, level | Its hold on the party | Its other moves | The lesson |
|---|---|---|---|---|
| Sergeant Alger Wyle (Sump Watch, CANON-EXPANDED) | the grille room, 23 | **Truncheon Sweep:** stun 2 s on everyone within 4 m, every 15 s | **Rouse the Watch:** from 80%, every 20 s, a 3 s alarm cast with a hand-bell. Interrupted, nothing happens; if it rings, every Watch camp still standing in the Undercroft comes | Interrupt the bell |
| The Man in the Wall (CANON root: the Forgotten) | the Hall, 24, mini-boss | **The stone remembers:** root 4 s within 6 m, every 18 s ("Your feet are stone a moment; then they are yours again") | Every 14 s he steps into the wall and out of a face behind whoever he fights (blinkEvery); a slow heavy blow; at 30% he says a name nobody has said in two hundred years (a terrify on the party near him) | Follow him: melee moves, ranged turns |
| The Toll-Collector (CANON) | the Angel, 24, only if refused (once rats exist) | **Red Eyes:** fear 3 s within 6 m, every 20 s, as a dozen red eyes open in the dark | blinks behind whoever it fights every 10 s; escapes at 20% down its pipe; then the swarm, 3 waves | A choice has a price: you could have paid |
| (fallback) the Bailiff (GAME-ONLY Deep-Rat) | the Angel, 24, only if refused | **Hundred-Throat Squeal:** fear 3 s within 6 m, every 22 s | a bite-burst round it every 10 s (ringEvery); the swarm's waves first | The same |
| Ganger Ruth Tallow | the Violet Seam, 24 | **Lash:** stun 2 s on everyone within 5 m, every 16 s (the whip cracks round) | **Lay On:** every 20 s a 2 s cast on a party member who is not her target, which can be interrupted. If it lands, a heavy lash (40% of max health) and she mends a little; **"Face! Face!"** at 60%: the seam's Half-Bound within 40 m come | Interrupt Lay On |
| Quartermaster Selwyn Thrupp (Iron Pact: CANON army) | the Dock, 24 | **Smoke-Pot:** disorient 3 s within 6 m, every 20 s | steps behind his target every 14 s (blinkEvery); the Gutting Hook (a heavy blow with a ground mark); two crossbowman guards (shoot, then close); enrage at 30% | Keep the healer behind the tank: he goes for the back |
| The Clatterer (CANON: "the Skittering Dark") | the lair, 24 | **Web:** root 4 s within 6 m, every 18 s | Clatter (a heavy blow that knocks 3 m back); at 60% its call hatches every unstamped clutch; enrage at 30% | Get ready before the pull: stamp the clutches |
| Chief Warder Hawise Stour | the Count-Hall, 24 | **Clap in Irons:** root 3 s within 6 m, every 16 s (shackles flung) | the Ring of Keys (a heavy blow that knocks 4 m back); rallies at 66% and 33% (her turnkeys, the hall's patrol and the camps within 30 m); enrage at 30% | Pull her clear of the cell rows, or the rallies bring the hall |
| The Breaker | the Hole, 24 | **Chain:** root 4 s on everyone within 6 m, every 14 s (the chain swung round) | Breaking Blow (knocks 4 m back); enrage at 30% | Steel and patience: a side fight for a quest |
| Captain Brunna Hardwick (Iron Pact: CANON army) | the gatehouse yard, 25, rising at half the ram's count | **Shield-Wall:** stun 2 s on everyone within 5 m, every 14 s | **Form Up** (a heavy blow that knocks 5 m back); two guards | Split: the tank turns her off the ram-gang while the rest finish the sally |
| The Binder, Severin Lisk (a Council mage; stone-flesh CANON) | the great hall, 25 | **Grey Hand:** stun 2 s on everyone within 6 m, every 18 s ("Your hands go grey and still") | **Petrify** (a 3 s cast on a member who is not his target, which can be interrupted: if it lands, that member is stone for 6 s, stunned and taking half harm); his three acolytes mend him (Mend Stone) | Interrupt: his Petrify and the acolytes' Mend Stone |
| **The Count-Master, Overseer Lucan Kemble** | the Pens, 25, last boss, `harder`, `boss:true` | **Bind** (stone-flesh, CANON magic): stun 3 s within 6 m, every 16 s ("The stone takes your legs, then your arms") | **Set in Stone** at 75%, 50% and 25%: he reads a number off the roll and points at a member who is not his target. Stone closes over them: a shell they cannot act from, in which they take no harm. The shell can be struck (it has about three times a trash mob's health; the leader marks it, and the sims strike marked targets). Broken, they step out. Not broken in 15 s, they step out at a third of their health and greyed (10% more damage taken for 20 s; a cleanse lifts it). It is never cast on a member already held. **The Shift** at 66% and 33%: "Shift! To me!", the Half-Bound gangs within 45 m come (a rally). **The bargain:** toll paid, at the first Shift the union takes his two bodyguards down the drains ("The union keeps its bargains."). Enrage at 25%: "He stops counting." | Break your own out of the stone, then him |
| The Bread-Eater | the Gullet, 26, hidden | **Gulp:** stun 2 s on everyone within 5 m of its mouth, every 20 s (its jaws close on the air in front of it) | **Spew** (a burst of the Flow round its mouth every 15 s, 6 m); a long-reach heavy blow (5 m, a 2.4 s windup) that knocks 4 m back; never leaves the water's edge | A fight you find, against a boss that won't come to you |
| Bellman, the Count's Hound | the Count-Hall, 25, rare (one run in five) | **Bay:** fear 3 s on everyone within 6 m, every 20 s | a mauling heavy blow; it runs to the nearest turnkey camp at 40% and brings it; enrage at 25% | Kill it where it stands, or it brings the turnkeys |

`boss:true` is on the Count-Master alone, so the scorecard opens when he falls.
- **Living Statue (draft B) and the tethered Spent (draft A) are replaced by Set in Stone.** Tethers that feed a boss are the Sludge-Tunnels' (shared rules).
- The Toll-Collector is never killed.
- Draft B's arena bosses (Old Scald the Pit-Bear, the pit-bell's kennels) and the Seeker on the count are cut: shared rules (no arena copied whole; Seekers are the Hub's).

### 5b. Mobs
| Where | Mob | Level | Look (today's kit, or new) | Behaviour | Label |
|---|---|---|---|---|---|
| Bread Drop, Undercroft | Sump Watch watchman | 23 | the Warden look in Watch blue with silver buttons | in threes; calls; drops Watch buttons | CANON-EXPANDED |
| Undercroft | lantern-man | 23 | a watchman with a lantern on a pole | burning-oil bolt (interruptible); drops a dark-lantern | GAME-ONLY |
| Junction, Hall | sewer beast | 23-24 | the wild boar, pale and hairless | packs | CANON map name |
| Hall | mould-crawler | 23 | the crypt spider, small, in a white-mould palette | ambush from the face-wall niches | GAME-ONLY |
| Ossuary | the ossuary dead | 24 | skeleton (the Fantasy Skeleton, as Khaven's drowned dead) | slow; fall to bones | GAME-ONLY |
| Angel, Skittering Dark | Deep-Rat (the union) | 24 | **rat model (not yet)**, dog-sized, red eyes; until then, eyes only | lie hidden until refused, or gone when paid (no neutral state is needed) | CANON |
| Breach, Seam, Pens | the Half-Bound | 24-25 | **convict figure (new) in stone-grey:** grey smock, shaved head, cracked stone skin up to the elbows, moss in the cracks; a stiff walk | slow and sturdy; work until you come near | GAME-ONLY (from CANON stone-flesh) |
| Breach, Landing, Seam | trusty | 24 | the convict with a gilt collar and a whip | Lash (an interruptible bolt) | GAME-ONLY |
| Landing to Pens | gaoler, turnkey | 24-25 | the Warden look in a grey-blue coat; turnkeys with key-rings and cudgels | pairs and threes; call | GAME-ONLY |
| Count-Hall | roll-caller | 24 | a turnkey with the count-roll and a tin whistle | Pipe the Hall (a 4 s alarm cast): interrupt it, or the hall comes | GAME-ONLY |
| Hole | gaol mastiff, handler | 24 | wolf, brindled and scarred; gaolers with goads | handlers and dogs as two camps | GAME-ONLY |
| Dock | the quartermaster's men, crossbowman, dock hand | 24 | Iron Pact soldiers without tabards, in oilskins | the crossbowmen shoot, then close | CANON army (the skim GAME-ONLY) |
| Skittering Dark | basalt crawler; crawlings | 24 | the crypt spider in a basalt palette, pony-sized, white eyes | twos and threes; the clutches hatch at the Clatterer's call | GAME-ONLY |
| Rising, Keep, Pens | Iron Pact gaol-guard, Pact crossbowman | 25 | iron-grey half-plate, black tabard, shield and spear | in threes; the crossbowmen shoot from the yard floor, then close | CANON army |
| Keep | footman, steward | 25 | household livery | non-elite | GAME-ONLY |
| Keep | the Binder's acolyte | 25 | grey robes, stone-dust on the hands | Mend Stone heals an ally under 70% (interruptible) | GAME-ONLY (stone-flesh CANON) |

**Not hostile:**
- the living convicts, in irons: in the cells, on the chain, at the face, bailing at the Dock. They cower when a fight starts. The goblins among them are on the PROVISIONAL child figure;
- Corrie Fisk;
- Kit Breward;
- Haska (an orc NPC);
- Clerk Wimbrel;
- the stone-bound on their plinths;
- pit mules (the Donkey critter);
- crows on the rim (the Living Birds);
- pale cave-bats (CANON; critters only, once a bat exists).

## 6. Quests (given outside, on the Bread Drop's rim walk; ids `side.veins.*`, kills `mob.<tag>.veins.*`)

All are GAME-ONLY quests, labelled with the canon they stand on.
- Levels 23-25, `minLevel` 22: none can be offered until the cap rises.
- Bosses' items come as "collect from a kill"; flags read `key:veins.*` from `Progress.keys` (the Adit's pattern).
- **Standings:**
  - **Warrens Folk** (GAME-ONLY, like Oakhaven Folk), from 1, 2, 3 and 5;
  - **the Weavers of the Warrens** (a CANON people; the standing is GAME-ONLY), from 6 and 9;
  - **the Salt-Menders** (existing), from 4 and 10;
  - **the Ash-Walkers** (existing), from 8.

**The givers** (GAME-ONLY people, residents on the rim walk until the Warrens zone exists):
- **Joan Breward**, a washerwoman at her line;
- **Sprat**, an urchin of the Blind Crow's network, at the runners' chalk post;
- **Mab Tunney**, who chalks the names on the brick by the stair head;
- **Pottle**, a goblin Weaver of the Blind Crow, with a pedlar's tray;
- **Gerda Holm**, the Salt-Menders' woman at the Drop;
- **Brother Wend**, a Silent Pilgrim;
- **Morga**, an orc dock-hand of the Rust & Ruin.

If Chris says yes to question 1:
- Razzle gives quest 2 and an extra, **Razzle's Marker**: a debt-paper in the roll-room that he burns, saying "There'll be a copy. There's always a copy." It foreshadows his canon choice and plays nothing out.
- Rill gives quest 6.
- Grohl takes Haska's word.

1. **Seven Years in the Deep Dark** (Joan Breward, 23). Her boy Kit took a loaf and got seven years (CANON sentence).
   - Ask Corrie Fisk at the Landing where Kit is (talk).
   - Carry him a loaf from the Drop and find his cell in the Count-Hall (deliver, at the cell).
   - See the gaol opened (flag `key:veins.rising`), then go back to Joan.
   - On the roll by Kit's cell, a dozen lines in the same clerk's hand say "bread: seven years". No canon name is used.
   - Reward: a piece of quest gear.
2. **Silver for the Union** (Sprat, 23).
   - Sprat hands you a silver button: "Silver. They like silver. They bite it. Pay it. Don't kill one."
   - Settle with the Toll-Collector (flag `key:veins.toll`, set whether you pay or refuse). More coin if it was paid.
3. **Faces in the Black** (Mab Tunney, 23).
   - Her sister was taken in the Bread Riots of '92 (CANON): "They told us exile. To farm the ash." (CANON lie.)
   - Take a rubbing of the face the urchins chalked in the Hall (interact), and bring it back (deliver).
   - Chronicle page: **To Farm the Ash**.
4. **Salt the Seams** (Gerda Holm, 24; CANON: geodes are stolen memory).
   - Salt five marked seams in the Violet Seam (interact, with salt in your bags), so the ore cut there lets go of what it holds.
   - Bring down Ruth Tallow.
   - Reward: a piece of quest gear.
5. **The Postern** (Sprat, after 2; 24).
   - "The Pact's quartermaster has his own door into the gaol. That means a key."
   - Kill Quartermaster Thrupp, and go in through the postern (flag `key:veins.quiet`).
6. **The Note of the Irons** (Pottle, 24).
   - A Weaver wants the lock-note of the legion's irons (Vibration-Locking, CANON), so the urchins can open them themselves.
   - Bring six shackle-locks off the Half-Bound (collect).
7. **What the Dark Eats** (Brother Wend, 24).
   - A pilgrim went down to hear the Forgotten and did not come back.
   - Lay three of the ossuary dead to rest (kill).
   - Bring her echo-jar out of the Clatterer's lair (collect from the kill).
8. **Where the Ridge-Folk Go** (Morga, 25).
   - "They take ours off the Ridge for labour. Find where."
   - Reach the Hole and open Haska's grille (flag `key:veins.haska`).
   - Morga's turn-in sends word to the Ridge. No follow-on quest needs a key.
   - Reward: a piece of quest gear.
9. **Under the Ink** (started by the Count-Master's Writ; turned in to Pottle).
   - Free Clerk Wimbrel in the roll-room (interact; this can be done first).
   - Take the writ to Pottle (deliver), and the Weavers, once the High City's light-catalogers (CANON), read under the black.
   - Chronicle page: **Under the Ink**. It names the seal, the Transit Hub and "berths for ten thousand". What they are for, the ink still hides.
10. **The Count-Master** (`side.veins.master`; Gerda Holm, after 4; 25).
    - Kill the Count-Master and bring his writ. A choice of three rare pieces (section 7).
    - Then **A Writ for the Hub** (Gerda Holm): **the hook into the Transit Hub.** It hands you to the Hub's quest 11, Berths for Ten Thousand, which Gerda gives in the Half-Light.

**The way here:** the Sludge-Tunnels' quest 3, The Ledger's Last Page, ends with Lisle Tamber sending the player to Gerda Holm on the rim walk ("Go down under the Warrens and see who sells them"). When the Warrens exist, its first quest on arrival does the same (QUEST_DESIGN section 8's breadcrumb pattern).

## 7. Loot (`loot.veins.json`; levels 24-25, quality 3)
The slots are the game's own: head, neck, shoulders, chest, hands, legs, feet, mainhand and offhand.

- **The Rising's Kit** (rare, 5 pieces; gaol gear the Rising took back):
  - neck: the Trusty's Gilt Chain (Ruth Tallow);
  - feet: the Turnkey's Boots (Chief Warder Stour);
  - shoulders: the Pact Pauldron (Captain Hardwick);
  - hands: the Binder's Grey Gloves (the Binder);
  - chest: the Count-Master's Coat (the Count-Master).
  - **Set bonus** (provisional until the 23-25 balance targets exist):
    - at 3, +80 health;
    - at 5, +14 attack power, and each kill gives back 30 health and 15 power (onKillHeal, onKillPower).
- **Every named elite drops one named piece every time** (signature chance 1, as 0.075; shared rules), with 20% extras:
  - Wyle: the Sump-Watch Truncheon (mainhand), the Bell-Sergeant's Handbell (offhand);
  - the Man in the Wall: the Moss-Kept Torc (neck), Basalt Patience (offhand, a shield);
  - the Toll-Collector, if refused: the Toll-Collector's String (neck: bitten silver buttons). The fallback Bailiff drops Union Dues (neck);
  - Ruth Tallow: the Lampless Lantern (offhand), and her set piece;
  - Quartermaster Thrupp: the Quartermaster's Gutting-Hook (mainhand), the Quartermaster's Oilskin Hood (head);
  - the Clatterer: Clatterfang (mainhand), the Basalt-Chitin Greaves (legs);
  - Stour: the Count-Hall Lamp (offhand), and her set piece;
  - the Breaker: the Breaker's Chain (mainhand), the Hole's Iron Gauntlets (hands);
  - Hardwick: the Shield of the Deep Watch (offhand), Hardwick's Standard-Spear (mainhand), and her set piece;
  - the Binder: the Binder's Rod (mainhand), the Grey Mask (head), and his set piece;
  - the Count-Master: the Stone-Flesh Rod (mainhand), the Count-Master's Seal (neck), his set piece, and the writ (a quest item).
- **The hidden and the rare each have one better piece:**
  - the Bread-Eater: the Bread-Eater's Tooth (mainhand), and the Drain-Gullet Mantle (shoulders) as its 20% extra;
  - Bellman: Bellman's Brass (neck: the hound's brass name-plate on a chain, worn).
- **Quest gear:**
  - The Count-Master gives a choice of three:
    - the Ram-Head Maul (mainhand);
    - Runner's Chalk-Gloves (hands);
    - a Polished Geode on a Thong (neck; CANON-EXPANDED: polished geodes show faces). No Tuning-Shard: those are Nila's, and the Hub's (shared rules).
  - One piece each from quests 1, 4 and 8.
- **Trade goods and trash:**
  - raw geode ore (`mat.geode_shard`, CANON), from the Seam's nodes and the Half-Bound, for the Blacksmith;
  - moss-light (the Grotto's herb nodes), for the alchemist;
  - Watch buttons (the toll; a few coppers otherwise);
  - shackle-locks (quest 6);
  - the quartermaster's chits and bitten silver (vendor coin).

## 8. Atmosphere
- **Light by part,** so you always know where you are:

| Part | Light |
|---|---|
| The Bread Drop | smog-grey daylight |
| The Undercroft | lantern orange on wet grey stone |
| The Junction | black water, the three brick mouths, and the hard line of basalt |
| The Hall of Silent Faces | moss green, swelling and fading every 4 s |
| The Weeping Angel | green slime-glow; red eyes at the edge that go out when you pay |
| The Breach and the Landing | geode violet and lamp white; no flame |
| The Violet Seam | deep violet, and a yellow-green haze in the gas bay |
| The Smuggler's Dock | the Toxic Flow's acid green from below; the smugglers' hooded yellow lanterns |
| The Skittering Dark | almost black: the white mould's dull glow and the crawlers' eyes |
| The Grotto | blazing aqua-green |
| The Count-Hall | cold white lamps, hard bar-shadows |
| The Hole | dull red over iron |
| The Keep | warm gold, white stone, crimson; candles at last |
| The Pens | flat grey; rows of white pole-lamps going off into the dark |
| The Gullet | acid green on black water |

- **Lining by part:**
  - grey rubble-stone (the Undercroft);
  - black basalt with faces under moss;
  - pale cistern stone;
  - picked basalt with violet seams and timber sets;
  - bone;
  - cut stone with bars;
  - white stone and gilt;
  - raw grey new-cut.
- **The faces** have to read as faces at a glance, thousands of them, not as noise. This is the art risk of the whole place.
- **Sound:** the game has none yet (KNOWN_ISSUES), so these are written for when it comes, and narration carries them until then.
  - the banner on coming in: "The air tastes of rust and sulphur. Somewhere, water is falling.";
  - the drip in the Hall like hammer-strikes (CANON);
  - the rats' whistle;
  - the convict chain counting under its breath;
  - irons chinking;
  - the keep's thick quiet;
  - in the Pens, picks far off and nothing else.
  - Until then, the Rising's ram is a screen shake and a count, and the bell is a line in the chat.
- **Readable layout:**
  - blue chalk at every turn, and at each branch mouth the sign of what it is for;
  - each branch's colour at its mouth, all four seen from the Landing's spirit stone;
  - the Toxic Flow's green leading down from the Breach to the Pens;
  - each built part looks unlike the last: rubble-stone, carved basalt, a cistern, a mine, a dock, bone, iron bars, a keep, raw new-cut pens.
- **Small life:**
  - crows on the rim;
  - bats bursting from the Undercroft's roof;
  - the convicts at work;
  - the mules on the Landing;
  - the rats' eyes;
  - the Toxic Flow's slow surface.

## 9. What has to be built (in order; nothing until the cap passes 22)

0. **The cap.** LevelCap past 22, and with it:
   - quest `minLevel` (the validation reads LevelCap);
   - the sims' cap;
   - talents;
   - balance targets for 23-25;
   - the run tool's fixed level 11 (RunCapture), set to 24.

1. **Art first, shown to Chris in frames (front, back, side) before it goes in:**
   - **The rat model:** a Deep-Rat and a larger Toll-Collector, with idle, walk, run, attack, hit and death.
     - The toll's refusal cannot be built without it.
     - **The fallback, until it exists** (question 5), covers every rat in the design:
       - the toll is pay-only;
       - the Toll-Collector is a voice and a pair of red eyes under the angel's wing (light points, no body);
       - the rats in the arch are a seething dark on the nav blocker, with moving red eye-lights; paying puts them out;
       - the bargain in the Pens is red eyes flooding the drain-mouths and the two bodyguards pulled backwards into the dark, with the line;
       - the Skittering Dark's union camp does not exist (it stands only if the toll is refused);
       - there are no sewer rats in this design: the Hall's packs are mould-crawlers and sewer beasts.
   - **Figures:**
     - the convict figure in irons, living, and the Half-Bound (the same in stone-grey, a stiff walk);
     - the trusty;
     - the gaolers, turnkeys and roll-callers;
     - the Iron Pact guard and crossbowman, and the quartermaster's men out of tabard;
     - the Binder and his acolytes (grey robes);
     - the Man in the Wall (the Hollow figure in basalt grey, with moss).
   - **Creatures:**
     - the basalt crawler and the Clatterer (the crypt spider in a basalt palette, and three times its size);
     - the Bread-Eater (a model to find, or the bear's body hairless and swollen as a stand-in);
     - Bellman (the wolf, brindled, scarred and scaled up) and the gaol mastiffs;
     - the ossuary dead (the Fantasy Skeleton, as Khaven's drowned dead).
   - **Orc and goblin NPCs:**
     - Haska and Morga: one line of code, since orcs are player and sim figures only today;
     - Pottle, Wimbrel and the singing convicts on the PROVISIONAL child figure until a goblin exists (Phase 9.3).
   - **Dressing:**
     - the face-relief lining with the moss pulse;
     - the rubble-stone undercroft lining with its piers;
     - cold geode lamps;
     - the Toxic Flow and black-water sheets (blocked, like the lava lake);
     - the whirlpool;
     - the rats' crowd at the gate (and its eyes-only fallback);
     - a bat critter, if one can be had.

2. **The layout and the zone:**
   - `tools/wip/dungeon/veins_layout.py` from `adit_layout.py`:
     - the plans, the camp centres and the patrol routes;
     - a plan picture;
     - the check that no two caves cross;
     - an assert that every row on a camp's way is at least 5 m half-width and no floor is over 25 degrees;
     - a check that each spirit stone is 20 m from every camp's centre and off every patrol's road.
   - `zone.veins`:
     - `dungeon:true`, levels 23-25;
     - seven caves with `within`;
     - 52 camps, the hidden camp behind its key, plus the `rise` and `unlessKey` camps, with the rare camp last;
     - routes;
     - keys `veins.*`.
   - The open ground: the rim walk, the rope stair and the floor.
   - The exit: the rim walk's back door.

3. **Cavern variant and `ZoneBuilder.Veins.cs`:**
   - linings by part;
   - the threshold line;
   - D9's Hall runs as tabled in section 4, each checked against `h.Half[i] * .82f >= w`;
   - **the Cell helper** (kit-wall dividers, the kit's Bars, hasps and irons) for the Count-Hall's cells, and the gang-bars along each block;
   - the warder's cage;
   - the Shift-Gate and keep-gate EndWalls;
   - the Pens' rows and pillars;
   - the Landing's headframe, stages and windlass;
   - the Dock's pier and skiff;
   - the skull arch;
   - the Hole's floor grilles;
   - the Gullet's grate and its loose bars;
   - the blockers, with causeways where the Flow is crossed;
   - lights by part.

4. **Things to use.** The interactable switch knows only the Adit's kinds, so this makes them per zone, with per-zone constants in place of the global LiftWoken and a carried thing per zone in place of CarryingPowder:
   - the lantern rack and the sump;
   - the angel (the conversation, silver taken);
   - the rats' gate;
   - the postern;
   - the loaf (taken at the Drop, delivered at Kit's cell);
   - the clutches;
   - the rats' crawl and the hoist (the ride pattern);
   - the warder's cage and the gang-bars (key and iron);
   - Haska's grille (`cage` per zone);
   - Wimbrel's chain;
   - the desk and the strongbox;
   - the grate's loose bars (a secret that opens a way);
   - two spirit stones (shared rules: BoundStone takes the deepest stone touched, and the zone's own line; built with the first of the three).
   - Already built: the spirit stone, ore and herb nodes, secrets, keyed ZoneGates, SendWave.

5. **Encounters:**
   - EliteMoves for 14 elites (data in C#, by name);
   - MobCasts:
     - the roll-callers' whistle and Wyle's bell, as the new alarm kind;
     - the trusties' Lash, the lantern-men's oil and the crossbowmen's bolts;
     - the acolytes' Mend Stone;
     - Tallow's Lay On;
     - the Binder's Petrify, a hold cast.
   - the new pieces below.

6. **Content and registration:**
   - `Quests/veins.json`: ten quests and their pages (To Farm the Ash, The Roll of '92, Under the Ink, Berths, Down the Drain, the writ), and A Writ for the Hub as the breadcrumb;
   - `loot.veins.json` (every named piece at chance 1);
   - the Warrens Folk and the Weavers of the Warrens in factions.json;
   - the zone added to the scene's ZoneBuilder.zones (a missing one broke 35 tests);
   - the quest and item files added to EncounterContent and Encounter.asset.

7. **Sims:**
   - The leader skips `rise` camps and camps gone by `unlessKey`.
   - It waits at the rats' gate, the Shift-Gate and the warder's cage, saying what it wants.
   - Every party sim guards the ram-gang.
   - The leader never enters the Echo Gallery and never pulls the Gullet's bars.
   - The leader marks a stone shell in the finale, and the sims strike it.
   - The run tool pays the toll and opens the gates as it does the Adit's, at level 24.
   - The party of five at the door (shared rules).
   - **Optional later:** a rule that lets the leader skip caves marked optional, so a sim-led run can take the main line.

8. **Tests:**
   - `VeinsNavTests.Every_camp_can_be_walked_to_from_the_way_in`;
   - every way 10 m wide where the leader walks, and the spirit stones clear of camps and roads;
   - the toll paid, and refused (and the Bailiff fallback) once the rat model exists;
   - the Shift-Gate, quiet and loud;
   - the Rising: the cage, the gang-bars, the ram with and without the Breaking-Iron, the sallies, Hardwick rising, the gate falling, a gang that breaks and tries again;
   - Set in Stone: broken out, and left to run its 15 s;
   - the clutches;
   - the Echo Gallery;
   - the Gullet's bars and the Bread-Eater;
   - the quests end to end;
   - a group run (`group_run.ps1 --crulanda-zone zone.veins`) measured against the pace table (about 57 minutes);
   - front, back and side frames of every wing for Chris.

**The mechanic budget.**
- **Reused as they are:**
  - holds (stun, root, fear, disorient) on the target or the party near;
  - the heavy blow with a ground mark, and its knockback;
  - ring bursts, blinks, calls and rallies, enrages;
  - deathCall walk-ins and patrols;
  - ambush camps (today's rule hides them in tall grass: check it hides them on a cave floor);
  - mob casts (heal, bolt, drain), and interrupts;
  - the rare camp;
  - keys from elites, and keyed gates;
  - the powder pattern, and SendWave;
  - cages;
  - Danner's hidden-and-rise;
  - the escort with waves (AditWeaver);
  - the ride out;
  - secrets.
- **New (seven):**
  1. Per-zone things (constants, and the carried thing).
  2. Two camp flags: `rise` (hidden until an act raises it; the leader skips it) and `unlessKey` (gone once a key is kept). Together they replace a neutral state, which is not built.
  3. The alarm cast (a MobCast kind that calls when it lands).
  4. The escape phase (the Toll-Collector). Not needed if Chris picks the Bailiff, nor until the rat model exists.
  5. Set in Stone (a struck shell over a member who is held inside it, with a timer).
  6. The escort generalised to data, for the ram-gang working a gate on a shown count.
  7. The Echo Gallery's quiet rule (it can be cut).
- **Small lines:**
  - the bargain removes two named mobs on a key;
  - the Bread-Eater does not move;
  - Petrify, a cast that holds its mark.
- **Shared with the other two** (built with whichever comes first): the party of five; BoundStone's deepest stone and the zone's own line.
- **Cut from draft B:**
  - Walked On (a Spent rising again);
  - Lock-Up (a teleport into a cell);
  - Gulp as a swallow;
  - Redact's silence and the blacked-out name in the party frames;
  - the Pit's spell-damping;
  - silver dust against the readers;
  - Marr's standard;
  - blink-to-the-wall;
  - a quest that needs a key (Word for the Ridge);
  - Haska's yield-and-spare, and allies faked through holds;
  - Living Statue, and Count Off's one-time whole-party stun;
  - the trusties' haste cast;
  - the arena grille that shut by itself;
  - the gas-gallery blast in the Seam (the loud way is at the gate now);
  - the Grotto as a branch of a branch;
  - the Maw's own cave as draft B drew it (the Gullet is 30 m and has no branch);
  - the 0.3 room builder;
  - the wait for the Warrens zone.
- **Cut on 2026-10-14, so the three later dungeons do not repeat each other** (shared rules):
  - the Spent, their collars and the Tether-Post (the Sludge-Tunnels'); the convicts go in irons, and the Rising opens gang-bars;
  - the tethered Spent who fed the Count-Master (the Sludge-Tunnels' tether fight); replaced by Set in Stone;
  - the Tether-Rod; Tallow keeps the Breaking-Iron instead;
  - the Iron Teeth at the Dock (the Sludge-Tunnels'); the Dock is the Pact quartermaster's;
  - the red-brick Brick Mouths (the Sludge-Tunnels'); the wing is the Undercroft;
  - the Redactor and his Ordinance Nine, the Concord readers and the Bureau's clerks (the Hub's); the Binder, the roll-callers and the acolytes instead;
  - the Seeker on the count (the Hub's); Bellman instead;
  - the pit-bell's arena and Old Scald (an arena copied whole); the Hole instead;
  - the Gutter Maw called up with a loaf (a beast called out of the water, copied whole; and "Maw"); the Bread-Eater is found;
  - a Tuning-Shard among the rewards (the Hub's).

**Biggest risks:**
- the face-walls (art);
- the rat model (the toll's refusal hangs on it; the eyes-only fallback is written in step 1);
- generalising the Weaver's escort into the ram-gang without breaking the Adit's;
- the crowd in the Count-Hall at the Rising (the budget is in section 4);
- the Pens' rows (prop count) and their dark (frame rate, the navmesh bounds), though at 64 m across they are far smaller than draft B's 120;
- Set in Stone with sims: they must strike a marked shell, not their target;
- fourteen elites tuned at 23-25 before any balance targets for that band exist.

**Where this design came from** (merged 2026-10-14 from two drafts, then changed the same day to keep to the shared rules):
- **Draft B gave the base:** the route, the places, the bosses, the Rising, the keep and the Pens.
- **Draft A gave:**
  - the givers in the zone's own open ground;
  - D9's Hall with the 0.82 check, and the Cell helper;
  - the camp rules;
  - each branch doing its own job, and the explicit lantern pick-up;
  - the pace table;
  - Pale Stone;
  - the Smuggler's Dock and its boss;
  - the Bailiff fallback.
- **Dropped from both:**
  - A's Vault of Whispers (it collides with Book 1);
  - B's line about Kara;
  - B's camps on ledges and crossbowmen on a roof;
  - draft B's 3 m study stair.

# The Transit Hub (levels 27-29, the high band; designed 2026-10-14, built when the level cap rises past 15)

**Status: DESIGN ONLY.** The level cap is 15 (`EncounterProgress.LevelCap`). Until it passes 29:
- no quest here can be offered (Quests.cs: minLevel must be within the cap);
- the sims stop at 15;
- the run tool plays at level 11 (RunCapture);
- balance targets exist only for level 15.

Nothing here is built, tested or seen by Chris. Every quest giver stands in zone.hub's own approach, the Half-Light, so nothing outside the dungeon waits on the Lowtowns or the Crown being built (section 6). The rules the three later dungeons share are in the section before the Sludge-Tunnels (run length, a party of five, ways and size, holds, loot, spirit stones, the crowd, owners, names, the story chain).

**How this design was made.** Two drafts were written on 2026-10-14 and checked against the brief. This one keeps draft B's shape:
- the Half-Light;
- the Porters' Stair;
- the sky deck;
- the Concourse and its three wings;
- the Crown Stair and the Intake.

Into that shape go draft A's simpler systems:
- runners and watch-bells the whole way through;
- a hub that changes while you are away;
- a walking escort;
- A's all-inside layout as the fallback (Plan I).

The same day it was changed again so the three later dungeons do not repeat each other or copy a classic encounter whole (shared rules). The end of section 9 lists what was cut and why.

## Ask Chris first (short)
1. **Sky deck or all inside?**
   - **(a) The deck:** you come up out of the rock onto an open platform halfway to the clouds, with the end in sight. It needs new code first: the zone's high ground, caves that climb, and a sim leader who crosses open ground and rides the rope rides.
   - **(b) All inside:** the deck becomes a roofed cargo hall whose railed arches open on a painted drop, and today's leader can run it. **But it loses the climb.** Today's builder can only take floors down from the gate, so in Plan I you walk 46 m down through a story about going up, the deck is a hall, and the end is never in sight from below.
   - *Recommended: (a) if the two proofs in section 9, step 1, pass; (b) if they don't. Every room, camp, boss, event and quest is the same either way. The climb, the open deck and the end in sight are Plan D's alone.*
2. **Wren is rescued here** (the Oakhaven girl the collectors took). The soldier's boy has already gone up: his casket stands open and empty. Canon says only that his sister "cannot save him". Yes?
3. **One tone call.** High Inquisitor Vax Mal-Cor watching through the Spire's door as the Warden falls, then walking away. Keep it?
4. **The rope rides:** three on the deck plan, two inside. Each is a rope, a basket or cage, and a timber windlass with people at its bars. Nothing else lifts: no hoist arm, capstan, cradle, treadwheel or cable car, and player text never says "lift" (the Porters' Basket, the Low Haul, the Seekers' Cage). All right?
5. **Levels:** 27-29 as the road has them, or 31-33, nearer the top of the high band?
6. **The Copper-Tithe as enemies?** In canon they point out resonant children to the Inquisitors (b1 ch.3:13-17). The game set them aside as villains because they are sympathetic in canon and turn in its climax (HANDOFF_ARCHIVE l.678). Here the Half-Light's enemies are the Concord's collectors instead. Use the Tithe? (Until you answer: no.)
7. **Nila as a quest giver?** She is CANON and alive (b1 ch.1:161-221). Here she sells her Tuning-Shard at her door, as canon has her do, and Tibb gives the quests she would give. Let her give quest 2 and take the Writ of Carriage (quest 10)? (Until you answer: no.) Gorm is not used here: he is the Sludge-Tunnels' question.

## 1. Where it is and what it is (lore)

| Thing | Label | Source |
|---|---|---|
| The High-Sovereign Transit Hub. Nila calls it "the heart of their logistics", "the junction for every soul-prism and aether-geode entering the High City", and warns: "there are no cells. Only redaction." | CANON | b1 ch.1:179 |
| "A titanic platform of iron and marble suspended halfway to the clouds": a warehouse where the deep mines' raw resources are sorted before they go up to the elite. Seekers come up "from the guard-barracks" below it. | CANON | b1 ch.1:197, 217 |
| Transit-caskets: black armoured boxes under the High Council's seal. The Proximity Ward turns the lock into a scrying mirror, a crimson light pulses, and an alarm sounds through the Hub. | CANON | b1 ch.1:199-213 |
| Souls carried in lead-lined, warded crates in ore-transports and "Resonance Transports", and the silence inside them | CANON | b1 ch.7:211, 289; ch.9:19 |
| The Half-Light: soot-black iron pillars under the High City's marble foundations, scaffolding, chain-ladders, maintenance shafts, illegal dwellings and toxic runoff. Nila Glass-Stitcher lives there and sells Tuning-Shards "tuned to the cargo lifts". Her shard makes a lock take you for a Senior Scribe. | CANON (canon's words; player text says "tuned to the transit", shared rules) | b1 ch.1:161-189 |
| The Silver Foundation: iridescent silver-brick under the High City's white stone, left by an earlier cycle that mastered gravity | CANON | b1 world_bible l.145 |
| The Crown: white stone and gold, and bells everywhere. The Praetorian Guard: white enamel, gold filigree, maces. | CANON | world_bible; b1 ch.26:11; ch.15:183 |
| The Indentured: debtors and dissidents in grey who march "in a slow, perfect lock-step", shackled by Arcane Tethers of light. Their iron collars are a siphon into the Spire's grid. "Seven years... in the deep dark." | CANON. Here they are the Hub's porters, living, collars and tethers shown and never played: cutting tethers is the Sludge-Tunnels' fight, and the Spent are the Sludge-Tunnels' (shared rules) | b1 ch.12:19-29, 133 |
| Seekers: white plate and featureless gold masks (ch.1), or grey cloaks, eyeless masks and void-bolts (ch.15). Struck by a force wave, two are "launch[ed]... backward" and rise at once. A Lowtowns rumour says one "didn't even stagger". | CANON. Here only (shared rules). They are held and knocked back like anyone; the rumour stays a rumour and is never a game rule. | b1 ch.1:217, 271; ch.15:183; lore_stories #2 |
| Inquisitors: crimson cloaks and smooth silver masks. High Inquisitor Vax Mal-Cor wears a weeping silver mask. Their black ink redacts. Redaction is the sentence for "Interference with Council Business". | CANON (here only) | b1 ch.13:23, 183; ch.1:303 |
| "Silencer" units for the Lowtowns; the Council's "Recycler" squads. "Redaction squads" is the game's name for them. | CANON (the name CANON-EXPANDED) | historical_evidence.md; chars/argentis |
| The Investigation Bureau's black sunburst | CANON | b1 ch.3:145 |
| Lysara, Aethelgard's second, who wanted "summary redaction" (named only) | CANON | world_bible; b1 ch.2:191 |
| The Copper-Tithe point out resonant children to the Inquisitors. Lowtowners smear silver dust on their windows to scatter the resonance readers. | CANON. The Tithe are not enemies here (question 6) | b1 ch.3:13-17, 103 |
| The High Concord's collectors, who took Wren and the soldier's boy from Oakhaven | GAME-ONLY (the game's own; the Concord is CANON) | Quests/oakhaven.json; QUEST_DESIGN |
| The Sandthrone, "often employed by the Concord" | CANON (the company). Its landing, checkpoint and skim at the Hub are GAME-ONLY. | b1 world_bible l.58 |
| Goblins catalogued the city's light until the Council found them "too resonant for clerical work"; static-charting. The Blind Crow is the urchin network's signal station in the Warrens, and Rill is one of its gang (named only). | CANON | b1 ch.1:13; world_bible; b1 ch.9:7 |
| Aether-geodes are fossilised memory, and polished ones show faces. Every light in the High City burns a stolen memory. | CANON | b1 ch.3:133-137 |
| Conduits: living people burned out to power the Council's work | CANON | b1 ch.2:19 |
| The Pale Things are "cosmic entities/auditors, not just monsters". In the game one is only glimpsed, never fought. | CANON (the glimpse PROVISIONAL) | world_bible pillar 8; b1 ch.12 interlude |
| Old Garris, taken by Seekers for "Hoarding aether" | CANON | historical_evidence.md (a Lowtowner's letter) |
| The Silent Pilgrims' echo-jars | CANON | world_bible |
| Oakhaven's ledger: "Viable Resonance Candidates Positively Identified: 3" | CANON | b1 ch.4:301 |
| The soldier's boy: Veyra's younger brother, seven, of Oakhaven, "taken first because he sparked". She "cannot save him. The Spoke consumes Conduits." The game's ledger calls him "J." and Corwin calls him "the soldier's little brother". No player text names Veyra. Taken now at seven and still seven when she speaks, so Book 1 is near. | CANON-EXPANDED | b1 ch.7:221-225; Quests/oakhaven.json |
| W. Harrow (Wren, nine) and T. Crisp (Tam); Sel Harrow | GAME-ONLY (the game's own Oakhaven people) | Quests/oakhaven.json |
| The game's own thread: "Two children, small, chained at the wrist. Brought up by Sandthrone riders" (Maddoc's book); Tarsk: "The children are gone up the road to Argentis"; Corwin: "So Wren and the soldier's boy are beyond us. For now." | GAME-ONLY | khaven.json (main.khaven.3); peaks.json; ashrim.json |
| The Adit's letter: "Forty crates this month... to the hub at the coast. The Council's factor there weighs them". Lisle wants his name. The Sludge-Tunnels found his mark and station. | GAME-ONLY | Quests/adit.json (side.adit.letter) |
| The Count-Master's writ: "berths for ten thousand, ready by the year's turn" (the Deep Veins) | GAME-ONLY (the pens are CANON, b1 ch.24:21) | this doc, the Deep Veins |
| Where the game puts the Hub: in the High City's south wall over the Half-Light, beside the Spire, which stands against the wall. The porters climb to it by service ways cut through the Silver Foundation. | CANON-EXPANDED | b1 ch.1:161-227 (the climb, the platform, Valen's fall into a Lowtowns alley); maps/argentis_city_map.png |
| The Spire of Agony (the raid). The cargo door the Writ opens is GAME-ONLY. | CANON | b1 ch.24:5-7 |
| Left out (0.05): the Emerald Gondola Line, the conveyors, the gravity-lock, the alarm's synthetic voice, the golems. What replaces them is in the next table. | GAME-ONLY translation | b1 ch.1:187-217; ch.12:37 |
| Everything else named below: the Sovereign Deck, the Porters' Stair, the Catalogers' Way, the Skim, the Concourse, the Guard-Barracks, the Weigh-House, the Ink-House, the Crown Stair, the Crown Intake, the Haul Bell, the docket and its three stamps, the watch-bells, every person, boss, mob, item and quest. The canon people: Nila (alive; a seller, and a giver only on Chris's yes), and Rill, Lysara and Vax Mal-Cor (named only). | GAME-ONLY | this doc |

**What the drafts got wrong, corrected here:**
1. The soldier's boy's source is b1 ch.7:221-225, not ch.4:301, which gives only the count of three. In play he stays "the soldier's boy", as the game already calls him.
2. Canon does not say where he was taken. His open casket is the game's choice.
3. The Seekers' "stagger" is a rumour in canon, and here it stays one. Canon shows them knocked back (b1 ch.1:271).
4. A Pale Thing is never a boss.
5. The Sandthrone at the Hub is labelled GAME-ONLY.
6. **"Valen's botched job at the Hub is years off" was wrong.** The soldier's boy, taken now at seven, is still seven and still held for the Spoke when Veyra speaks of him in Book 1 (ch.7:221-225). The game's now is shortly before Book 1, and Valen's job here is not far off.
7. **"Insufficient" was given to a GAME-ONLY assayer.** It is the Architect's word (lore_stories #4, "The Architect's Shadow"), kept for the Sludge-Tunnels' Former Miner. The Assayer says "Light".

**The story in one line: nothing goes up that has not been weighed, warded and written.** The Hub is where the city's stolen lives become freight. Through it pass:
- geodes from the Adit and the Deep Veins;
- crystals from the Forge District;
- children, in black caskets.

The party goes from the gutter to the gold, through every rung of the city's order (in Plan D it climbs; in Plan I the painted drop tells the height):
- the Half-Light's slum;
- the porters' stair;
- the Company's deck;
- the Concord's halls;
- the Crown's gate.

**Why the party goes in** (threads the game already has):
- **Wren and the soldier's boy.** Oakhaven: "We can't bring back Wren, or the soldier's boy. Not yet." Tarsk: "gone up the road to Argentis." The Hub is where that road ends.
- **The Adit's letter.** Lisle Tamber wants the name of "the Council's factor" at the coast station. The Sludge-Tunnels found his initials; the Hub weighs his crates and pays for them, and its ledger has his name.
- **The Deep Veins' writ.** "Berths for ten thousand": the order it answers is in the Ink-House's Census Vault.
- **The Spire.** The last boss carries the Writ of Carriage, which opens the Spire's cargo door to the raid.

**When:** before Book 1, and near it (shared rules):
- the Spoke is still being built;
- the soldier's boy was taken this season at seven, and is still seven when Veyra speaks of him, so Valen's botched job at the Hub is not far off;
- no canon person dies here.

The bosses are GAME-ONLY lieutenants. Lysara and Vax Mal-Cor appear only by name, and Rill only in Tibb's words.

**No machines (0.05), checked against the canon Hub.**

| The book's Hub has | The game's Hub has instead |
|---|---|
| conveyors | porter chains passing crates hand to hand, and hand-barrows |
| self-moving cargo lifts | three rope rides, each a rope, a basket or cage, and a timber windlass with people at its bars: the Porters' Basket (deck plan only), the Low Haul, the Seekers' Cage |
| the haul going up on its own | the haul carried in by the Indentured on their shoulders |
| the gravity-lock | nothing |
| the alarm's synthetic voice | wards that scream, and watch-bells and the Haul Bell rung by hand |
| the golems | living overseers |
| the self-moving gondola | nothing |

Also:
- The Great Scale is a beam balance.
- The wards and the great lamp are magic, not mechanism.
- There is no hoist arm, capstan wheel, cradle, treadwheel or cable car.
- No machine word appears in the player's text, "lift" among them (shared rules).

## 2. What we take from each classic dungeon (lessons only: no names, layouts or assets)

The classic band that maps to 27-29 here is about 52-60 out of 60. The Adit mapped 15-24 to 10-12.

### The Hub's own models

**The great city under the mountain** (the biggest built dungeon of that band):

| Its lesson | In the Hub |
|---|---|
| A working city with its own people and order, a hall for each kind of them | porters on the Stair, the Company on the deck, clerks in the Concourse and the Weigh-House, soldiers in the Barracks, scribes in the Ink-House |
| Many wings off one great hall, each with its own look and boss. Most bosses are optional: a menu. | the Concourse and its three coloured doors. 16 named fights. Seven can be left: Gaunt, Tuckett, the Census-Keeper, the Keeper of Seals, the Unmasked, the Floor-Marshal (wait for him to pass), and any wing's lieutenant (stamp the docket and leave him: he meets you at the finale). |
| A prison you break open, whose prisoners rise with you | the Collar-Post cages. Freed, the porters wind your Basket and later show you the back stair out. |
| The key to the raid, won inside | the Writ of Carriage |
| Its weakness: a maze and a long way back | Kept out. The doors are coloured and seen from the hub. The Seekers' Cage brings you back from the deepest wing, the Low Haul takes you out from the deck, and the back stair takes you out from the end. |

**Not taken from it:** its captive at the ruler's side who must not be harmed.

**The burning city with the noble's rescue:**

| Its lesson | In the Hub |
|---|---|
| Two halves of one place, each with its own enemies and look | the porters' side (rope, timber, the Stair and the deck) and the Concord's side (marble and gold, the Concourse and the Crown) |
| Something big walking the streets | the Floor-Marshal's round, and a redaction squad after each wing |
| A captive you can reach early, or late at a cost | Wren: freed on the deck if the deck is kept quiet, or after the Warden if the haul went in |

**Not taken from it** (each would read as a copy): its rescue clock; its boxes that bring out their keeper when opened; its last gate opened by the bosses at its corners. Here the rescue turns on noise, not time; the Keeper of Seals is found behind a door; the Seal Gate opens to stamps you earn by acts, not by kills.

**The ruined court-library (the tribute run):**

| Its lesson | In the Hub |
|---|---|
| Guards you may take or leave | the Hundredth's Ranks |
| A disguise to walk past guards | the Catalogers' Writ at the checkpoint |
| A library whose keepers hush it | the Ink-House's ward-lamps (the Hush) |

**Not taken from it:** its boss's shield held up by things round the room. The Warden's ward is the lamp's light on his dais: move him out of it.

### The bar the Adit set (the brief's three models, section 0), checked

**The mine with the ship at the bottom:**
- the story outside before you go in: Wren's thread from Oakhaven, the Adit's letter, the Deep Veins' writ, Nila's shard;
- one way up (Plan D), each room its own place: the Collar-Post, the Stair, the Turn, the deck;
- the reveal: the Basket into daylight, with the Intake's window in sight (Plan D; in Plan I, the Cargo Hall's arches on the drop);
- a gate opened by doing something, quiet or loud: the checkpoint;
- bosses that each teach one thing;
- patrols after bosses: Kell's drivers, Hesta's carriers, Vellum's Silencers, the squads;
- the first mate's stun on the whole group at two thirds and one third: the Warden's Sovereign's Word;
- the last boss drops the letter: the Writ;
- a hidden boss and a rare: the Keeper of Seals, the Unmasked;
- a set across the dungeon.

**The cave with the four lords:**
- an approach outside with its own mobs and quests: the Half-Light;
- lieutenants in any order before the finale: three wings, two stamps of three;
- the escort with waves and a surprise boss: the Proximity Ward (two waves, Silencer Unit 12), then Wren's walk to the Low Haul (one wave);
- mobs that flee for help: the collectors' runners outside, the watch-bell runners inside;
- corrupted creatures: not here. The Hub is people all the way up, and its corruption is the order itself;
- the shortcut back: the Seekers' Cage;
- every branch obvious.

**The order's fortified house:**
- the order at home in stone: the Guard-Barracks;
- packs that mend each other and call: the Muster squads;
- rooms you know at a look: cots, mask-rails, weapon racks and dummies, shelves and lecterns, the great window;
- **not its finale pair:** a commander and a priestess who raises him once is the Adit's (the Ember Vent). The Hundredth fights with his Ranks instead;
- the hidden door: the bookcase in the Counting-Room (the one hidden door in the three later dungeons, shared rules).

**"A mix" means places too (section 0):**
- **tunnels:** the Porters' Stair cut through the Silver Foundation (the cave builder's own look, with plank treads and rope rails, never brick), the goblins' crawl, and the Skim's rough rock;
- **built halls:** the Concourse, the Weigh-House, the Ink-House, the Intake, and the deck itself;
- **a stronghold of stone:** the Guard-Barracks and the Crown Stair.

No two parts share a look (section 8).

**Chris's brief of 2026-10-14, checked** (0.4: "more complex like wow's wailing caves or the deadmine"; complex, not one long route of camps):
- **Branching:** three wings off the Concourse; two side ways off the Turn (the Catalogers' Way and the Skim); the Company Landing off the deck; side bays in two wings.
- **Loops:** the Low Haul down to the Half-Light and back up; the Seekers' Cage from the deepest wing to the hub; the back stair out from the end.
- **Levels:** in Plan D the Half-Light 40 m under the deck, and the deck under the Intake's window; the barracks falling 14 m below the Concourse; the Crown Stair. In Plan I, the Cargo Hall's arches over the painted drop.
- **Choices that change the run:** quiet or loud at the checkpoint; cut the ropes before the pulls or not; stamp a wing and go, or clear it; Wren freed early or after the Warden.
- **Set pieces:** the reveal; the Proximity Ward; Wren's walk; the hub filling with squads; the Drive's lanes on the Stair Head; the Warden pulled out of his light.

## 3. Level and length

**Levels** (high band, 25-35):
- **Trash:** 27; Seekers, Silencers, assessors and the Company's guards 28; Praetorians 29.
- **27:** Collector Gaunt and the Collar-Smith.
- **28:**
  - Tether-Master Kell, Wace Tuckett and Hesta Dunne;
  - Silencer Unit 12, the Floor-Marshal and Pact-Sergeant Bannard;
  - the Hundredth, Assayer Quarle, the Census-Keeper and Redactor Vellum.
- **29:** Praetorian Ostrand, the Stair-Ward.
- **The last boss:** Sovereign-Warden Cassian Mordaunt, 29, elite, `harder`. He is the only `boss:true`, so the scorecard opens on him.
- **Rare:** the Unmasked, 29, one run in five.
- **Hidden:** the Keeper of Seals, 30.
- If Chris sets the band higher (question 5), the whole Hub moves up as one.

**Party:** five, 1 tank, 1 healer and 3 damage or support: you and four others, Mira among them or not (shared rules: a party of five). MaxPartySims is 4 and counts sims only; the dungeon's door holds the party to five. The run tool takes you and four sims.

**The shape (structure first):**
1. **From the gutter to the gold.** You begin in the Half-Light's slum, go through the porters' tunnels, out onto the Company's deck, into the Concord's halls and up to the Crown's gate. Plan D climbs it; Plan I walks down and tells the height with the painted drop (question 1).
2. **The reveal comes a quarter of the way in.** The deck, at about 13 minutes. In Plan D the end of the dungeon is in sight: the Intake's great window high in the white wall.
3. **The rescue turns on noise, not time.** The Haul Bell is rung when the deck is warned: by a fight at the checkpoint, or by a second watch-bell rung on the Stair or the deck. Rung, the haul is carried in. Kept quiet, it waits for you. There is no clock.
4. **The hub is crossed again and again, and it changes.** After each wing's boss a redaction squad walks onto the Concourse, and the Floor-Marshal walks it the whole time.
5. **The docket.** Each wing has one act that stamps your carriage-docket: Warded, Weighed, Written. Two stamps open the Seal Gate. Each wing's lieutenant you leave alive stands with the Warden. A full run clears three wings for an easier finale; a fast run stamps two and leaves lieutenants.
6. **Quiet or loud the whole way, not at one gate.** Runners break for watch-bell ropes in every part, and every bell they ring is one more pair of guards at the end.
7. **Side areas inside the parts.** Also a ride back from the deepest wing and a walk out at the end.
8. **The rule for every stretch: no more than about five minutes without something new** (shared rules). That can be a choice, a thing to use, a new kind of enemy, a view or a boss.

**Pacing** (the main way: every wing, no side area; a sim-led party; game minutes; about 1.1 minutes a camp, the Adit's measured pace, events included):

| Minutes | Stretch | What is new |
|---|---|---|
| 0-3 | [0] The Half-Light | the givers; collectors' runners who run for the next camp; the deck's iron legs overhead (Plan D) |
| 3-6 | the gate drift, the Collar-Post | the first watch-bell rope (cut it); the Collar-Post cages; the Collar-Smith |
| 6-9 | the Long Stair, the Turn | porter chains in lock-step; a patrol on the stair; the tally-desk runner; a crack in the west wall and the Skim's door (sides) |
| 9-12 | the Upper Stair, the Stair Head | Tether-Master Kell and the Drive |
| 12-13 | the Basket (deck plan) / the Lower Stair and the Basket Arch (inside plan) | the reveal; the spirit stone |
| 13-16 | the checkpoint, the weigh-post, the Haul Bell | quiet (the writ) or loud (the Haul Bell rings); the deck's Seekers; the vista |
| 16-18 | the stage foot | Carrier-Captain Hesta Dunne |
| 18-21 | the casket stage | the Proximity Ward (90 s, two waves, Silencer Unit 12); Wren's walk to the Low Haul (one wave). *The short run leaves here, at about 21.* |
| 21-23 | the Inner Gate, the Concourse | spirit stone; the clerks' desks (runners) and a blank docket; the Floor-Marshal; three coloured doors |
| 23-30 | [3a] the barracks stair, the Bunk-Hall, the Mask-Room, the Armoury, the Drill Hall | a patrol; a Muster squad; the ward-glass (Warded); Pact-Sergeant Bannard; the Ranks and the Hundredth; the Seekers' Cage up |
| 30-31 | the Concourse | the first redaction squad |
| 31-36 | [3b] the Weigh-Floor, the Sorting Halls, the Counting-Room | the Great Scale (Weighed); a patrol; Assayer Quarle's three memories; her brass key |
| 36-37 | the Concourse | the second squad; the Marshal again |
| 37-43 | [3c] the Roll-Galleries, the Scriptorium, the Redactor's Chamber | the Hush: lamps and their keepers; informers; the copyists' roll (Written); Redactor Vellum |
| 43-44 | the Concourse, the Seal Gate | the third squad; the docket at the gate |
| 44-46 | [4] the Crown Stair | Praetorians; the first clean light; Ostrand |
| 46-48 | the Crown Intake | Mordaunt in the lamp's light; the walk out |

**Side areas** (minutes added to a run):
- Gaunt, 2;
- the Catalogers' Way, 1;
- the Skim, 3;
- the Company Landing, 1;
- the Polishing Room, 1;
- the Census Vault, 1;
- the Sealed Room and the Keeper of Seals, 2.

**The runs:**

| Run | What | About (game minutes) |
|---|---|---|
| The short run | the Half-Light, the Stair, the deck, Wren; out by the Low Haul | 21 |
| The fast way | two wings stamped, no side area; the third wing's lieutenant at the finale | 42 |
| The main way | every wing, no side area | 48 |
| The full clear | every camp, every side area, the Keeper; quiet at the checkpoint (loud adds its two camps) | 57-59 |

These times are on paper, at the Adit's measured pace: 66-67 camps in 71-78 game minutes on the 2026-10-13 group runs, about 1.1 a camp. The Hub has **54 camps** in all, counting the hidden and the rare, so the full clear sits inside section 0's 45-60. 0.06's 60-75 is the Adit's own figure (shared rules: run length). Fewer camps than the Adit's 67 is not "less to do": something new comes at least every five minutes. Measure on the group runs. If the full clear runs past 60, trim in this order:
1. the deck's lengthwise patrol;
2. the Mask-Room's squad;
3. the Roll-Galleries' patrol;
4. the Crown Stair's first landing.

**What the group runs taught, carried here (0.07-0.08; shared rules):**
- The leader waits for the whole party.
- **Every named elite holds the party, not only the tank** (section 4, "How each boss holds the party").
- No single blow takes more than 60% of a member's health (MaxBlowShare).
- Only the last boss is `boss:true`.
- **Every way the leader walks is at least 10 m wide** (the Adit's 5 m lanes caused 26 warps), and no stair is steeper than 25°. The Catalogers' Way is 6 m: no camp, so the leader never goes in.
- Every camp stands on floor the navmesh reaches. Nothing stands on the casket stage, the Scale's beam, a column top or a crate stack.
- **Every named elite drops one named piece every time** (0.075).
- **Each spirit stone stands at least 20 m from every camp and off every patrol's road.**
- The party leaves held mobs alone.
- Five in the party, no more.

## 4. The layout

```
 THE LOWTOWNS (the Rust Market; not built)       THE CROWN (not built)
             └──────────────────┬──────────────────┘  (zone exits once those zones exist)
 [0] THE HALF-LIGHT (outside: the givers, the collectors; the deck's iron legs overhead)
          │  optional: Collector Wilmot Gaunt, the Finder
          ▼  the Porters' Gate
 [1] THE PORTERS' STAIR (tunnels through the Silver Foundation; porters in lock-step)
          │  side: the Catalogers' Way (Scrivet: the writ) · the Skim (Wace Tuckett)
          │  boss: Tether-Master Ambrose Kell (the Drive: mind the lanes)
          ▼  the Porters' Basket (deck plan) · the Lower Stair and the Basket Arch (inside plan)
 [2] THE SOVEREIGN DECK (the reveal; in the inside plan, the roofed Cargo Hall) · spirit stone
          │  the Haul Bell: rung if the deck is warned
          │  gate: the Company's checkpoint (quiet: the writ · loud: a fight, and the Haul Bell rings)
          │  boss: Carrier-Captain Hesta Dunne
          │  event: the Proximity Ward (two waves; Silencer Unit 12), then Wren's walk to the Low Haul (one wave)
          │  side: the Company Landing · the Low Haul (down to the Half-Light: the short run's way out)
          ▼  the Inner Gate (Hesta's token, or left open after the Ward)
 [3] THE CONCOURSE (the hub: spirit stone; the Floor-Marshal's round; a redaction squad after each wing's boss)
   ┌───────────────────┼─────────────────────────┐
   ▼ (white, down)      ▼ (violet)                 ▼ (crimson)
 [3a] GUARD-BARRACKS   [3b] WEIGH-HOUSE           [3c] INK-HOUSE
  Muster squads,        the Great Scale (Weighed), the Hush, the copyists'
  the ward-glass        the Polishing Room         roll (Written), the Census Vault
  (Warded), the Ranks
  boss: the Hundredth   boss: Assayer Quarle       boss: Redactor Vellum
  rare: the Unmasked    hidden: the Keeper of Seals
  the Seekers' Cage up   (behind the bookcase)
   └──────── the docket: Warded · Weighed · Written (two stamps open the gate) ────────┘
          │  the Seal Gate (each wing's lieutenant left alive goes to the Warden)
          ▼
 [4] THE CROWN STAIR (white and gold; Praetorians; Praetorian Ostrand, the Stair-Ward)
          ▼
     THE CROWN INTAKE (the casket rack, the dais and the great lamp, the Spire's cargo door)
        last boss: Sovereign-Warden Cassian Mordaunt (keep him out of the lamp's light)
          (+ each lieutenant left alive; + a pair of Seekers for each watch-bell rung)
        drops: the Writ of Carriage (it opens the Spire)
        way out: the porters' back stair, down to the Half-Light · the Crown door (once the Crown exists)
```

### Two plans, one design
Every room, camp, boss, event and quest below is the same in both plans. What differs is how the parts are joined, and **what Plan I loses**: the climb (its floors go down from the gate, the builder's limit today), the open deck in daylight, and the end in sight from it. Plan I never calls its way a climb; the painted drop through the Cargo Hall's arches tells the height instead.

**Plan D, the deck** (draft B; recommended if it can be built). zone.hub is its own zone, size 800.
- **The zone in plan view:**
  - The Half-Light is the low ground along the south.
  - The deck is a mesa 40 m above it (fallback 25 m), jutting south from the High City's wall. Its faces are dressed with iron legs and scaffold, so from below it reads as a platform on pillars.
  - North of both, a massif rises to about 90 m (fallback 60 m), faced with cliffs: white stone courses over silver-brick over black rock. White walls and gold-roofed towers stand along its top, for the eye only.
  - East beyond the deck, the Spire's black needle rises out of the smog past the wall's top: a landmark, not walkable (the city map puts the Spire against the High City's south wall).
- **The caves:**
  - **Cave A**, the Porters' Stair, climbs north-east into the massif from the Half-Light, east of the deck. Its head stays under the massif: the deck's land is too low over a 16 m chamber.
  - **Cave B**, the Inner Works, runs north from the deck's back wall: the Concourse, its three wings, the Crown Stair and the Intake.
  - The caves never overlap in plan view, and nothing crosses over itself.
- **The rides** join the parts:
  - the Basket, from cave A's head to the deck;
  - the Low Haul, between the deck and the Half-Light;
  - the Seekers' Cage, from the Drill Hall to the Concourse;
  - the back stair, from the Intake to the Half-Light.
- **New code it needs first** (section 9): the terrain; caves that climb; the leader across two root caves, open ground and rides.

**Cave A, the Porters' Stair** (along from the Porters' Gate):

| Along | Part | Width × height | Floor |
|---|---|---|---|
| 0-40 m | the gate drift | 10 × 6 m | 0 to +1 m |
| 40-70 m | **the Collar-Post** | 24 × 10 m | +2 m |
| 70-150 m | **the Long Stair**: two flights of about 20°, a landing between | 10 × 9 m | +2 to +14 m |
| 150-200 m | **the Turn**: the Catalogers' Way off its west wall at 170 m, the Skim off its east wall at 190 m | 32 × 14 m | +15 m |
| 200-260 m | the Upper Stair | 10 × 8 m | +15 to +26 m |
| 260-300 m | **the Stair Head**: the Porters' Basket at its closed end, its rope going up a dark shaft in the roof | 36 × 16 m | +26 m |

**Cave B, the Inner Works** (along from the Inner Gate in the deck's back wall):

| Along | Part | Width × height | Floor |
|---|---|---|---|
| 0-20 m | the vestibule | 12 × 10 m | 0 |
| 20-140 m | **the Concourse**: the Barracks off the west wall at 50 m, the Weigh-House off the east wall at 80 m, the Ink-House off the east wall at 125 m (turning north-east, clear of the Weigh-House) | 40 × 20 m | 0 |
| 140-150 m | the Seal Gate | 10 × 7 m | 0 |
| 150-270 m | **the Crown Stair**: three flights, landings 22 m wide at 185 and 230 m | 12 × 12-14 m | 0 to +20 m |
| 270-390 m | **the Crown Intake** | 48 × 26 m | +20 m |

**Branches of cave B:**
- the Guard-Barracks, 180 m, falling 14 m;
- the Weigh-House, 170 m, with the Sealed Room (30 m) at its own end past the bookcase door;
- the Ink-House, 170 m, level.

**Totals (Plan D):** about 1.4 km of tunnel and hall, plus about 300 m in the open (the Half-Light and the deck).

**Plan I, all inside** (draft A's layout as the ready fallback). zone.hub, size 900.
- **One root cave, "The Transit Hub".** Every other cave is a branch of it, met in order of distance, so today's leader (which takes only the first root cave, splicing in each branch where it opens) can run it. No branch has a branch of its own.
- **Outside**, the Half-Light is the zone's open ground, as in Plan D. The Porters' Gate is cut in the foundation's face, and only the gate watch stands within 14 m of the mouth, so the leader takes it first. The Half-Light's other camps are the player's approach.
- **Inside**, the floors only go down from the gate (proven by the Adit). That is the cost of this plan, and the text is written for it: the stairs are "the service way" and "the Lower Stair", never a climb.
  - The deck becomes **the Cargo Hall**: roofed by the Silver Foundation's underside, its east side a row of great railed arches open on a painted drop. Through them: smog, the Lowtowns' fires far below, the High City's lights as smeared halos above, and the Spire's black flank rising along the city's face. The painted drop, not the walk, says "halfway to the clouds".
  - The Basket is not ridden. The Lower Stair and a low arch lead into the hall.
  - The Crown Stair runs down 10 m as the Intake Stair.
  - The Intake's great window opens on a painted sky, as the Cargo Hall's arches do. Nobody has seen it from outside: in Plan I the end is not in sight from the deck.
  - Heights inside are the builder's. What the eye is told comes from the painted drop.
- **The ride kept from A:** the Seekers' Cage carries you back from the deepest wing (the Drill Hall) to the Concourse.

| Along | Part | Width × height | Floor (below the gate) |
|---|---|---|---|
| 0-40 m | the gate drift | 10 × 6 m | 0 to -1 m |
| 40-70 m | the Collar-Post | 24 × 10 m | -2 m |
| 70-150 m | the Porters' Stair, two flights, landing at 110 m | 10 × 9 m | -2 to -16 m |
| 150-200 m | the Turn (the Catalogers' Way west at 170 m; the Skim east at 190 m) | 32 × 14 m | -16 m |
| 200-250 m | the Lower Stair, two flights (no flight steeper than 25°) | 10 × 8 m | -16 to -36 m |
| 250-262 m | the Basket Arch (the low arch before the reveal) | 10 × 6 m | -36 m |
| 262-442 m | **the Cargo Hall** (the deck, roofed; arches along the east side) | 84 × 28 m | -36 m |
| 442-452 m | the Inner Gate | 10 × 7 m | -36 m |
| 452-472 m | the vestibule | 12 × 10 m | -36 m |
| 472-592 m | the Concourse (the Barracks west at 502 m; the Weigh-House east at 532 m; the Ink-House east at 577 m, turning north-east) | 40 × 20 m | -36 m |
| 592-602 m | the Seal Gate | 10 × 7 m | -36 m |
| 602-722 m | the Intake Stair, three flights, landings at 637 and 682 m | 12 × 12 m | -36 to -46 m |
| 722-842 m | the Crown Intake | 48 × 26 m | -46 m |

**Branches of the spine (Plan I):**
- the Catalogers' Way, 80 m;
- the Skim, 55 m and its den;
- the Guard-Barracks, 180 m, down to -50 m;
- the Weigh-House and the Sealed Room, 200 m;
- the Ink-House, 170 m.

**Checks (Plan I):**
- About 1.55 km in all. The spine is laid in an S to fit the zone.
- **Width:** the Cargo Hall's half-width is 42 m, inside the widest chamber built (the Rail Hall's 42.5). Its 28 m height is the Rail Hall's (shared rules: no chamber bigger).
- **Ways:** every way the leader walks is at least 10 m wide; the Catalogers' Way (6 m) has no camp.
- **Stairs:** no flight steeper than 25°.
- **Roofs:** every roof stays at least 8 m under land at the gate's height. The Cargo Hall's roof is at -8 m, the Intake's at -20 m.
- **Branches in plan view:** the two east branches are 45 m apart, and the Ink-House turns away. The Catalogers' Way and the Skim open on opposite walls.

### [0] The Half-Light (outside; the approach)
- **What it is:** the vertical slum under the High City's foundations.
  - Soot-black iron pillars stand "like the iron legs of a silent god".
  - Scaffold towers, chain-ladders and shanties of rusted copper cling to them.
  - Runoff from the aether-labs lies in three shallow pools, 0.4 m deep and violet-green. They do no harm (there are no damage zones).
  - In Plan D, the deck's underside hangs overhead on its iron legs, and the white wall climbs into the cloud above it: **the way on is in sight from the start.**
- **Size:** about 140 m from the Porters' Alley to the Porters' Gate, 90 m wide between the rows of pillars. The zone's start point is by the alley.
- **Getting in:** from the Rust Market by the Porters' Alley (south edge), or down the Beggars' Stair from the Crown (GAME-ONLY). Both are zone exits once those zones exist; until then, the world map's travel lands you at the alley.
- **Dressing:** shanties from the Village kit's timber, the Megapack's towers as scaffold, the Fantasy kit's chain and rope, braziers, washing lines.
- **Light:** rust-orange braziers in grey smog.
- **Camps** (4):
  - **H1** collectors' runners ×3 at the alley mouth (non-elite, 27): Lowtown boys paid by the Concord's collectors to watch the gate. At 40% they run for H2 and bring it.
  - **H2** the Finder's crew: 2 collectors under the iron legs. Runners: at 40% they run for Gaunt's shanty and bring him.
  - **H3** Collector Wilmot Gaunt and 2 collectors at his shanty (side).
  - **H4** the Porters' Gate watch: 2 Concord drivers, within 14 m of the gate.
- **Mini-boss: Collector Wilmot Gaunt, the Finder** (27, elite, optional; GAME-ONLY, of the High Concord's collectors, who took Wren and the soldier's boy from Oakhaven). They say he can smell a resonant child through a wall. He buys the Lowtowns' candidates and walks them to the Porters' Gate for the bounty.
  - **Lime in the Eyes (his hold):** every 18 s, everyone within 6 m is disoriented for 3 s (a fistful of quicklime).
  - **Short Change:** his heavy blow.
  - **At 60%** he calls: "Lads!" His collectors come, and any runner within 30 m.
  - *Teaches: spread out.*
  - **Drops:** the Finder's Book (a document listing children by weight and price), Finder's Silver (neck; luck) and the silver Nila will take for her shard.
- **Non-hostile:**
  - Nila Glass-Stitcher (CANON) at the foot of the chain-ladder to her door: she sells her Tuning-Shard (a shop), and gives quests only on Chris's yes (question 7);
  - Tibb, an urchin of the Blind Crow's network;
  - Tull Marsh, an escaped porter with a collar-scar;
  - Mags Dorran, old Garris's neighbour;
  - Outrider-Captain Arn Feld of the Sandthrone, who wants the Company's skimmer;
  - Gerda Holm, the Salt-Menders' woman from the Bread Drop, come up after the Count-Master's writ;
  - and, until their own zones exist, Lisle Tamber and Brother Cael.

### [1] The Porters' Stair (tunnels; the porters' side)
- **What it is:** the service way the porters climb with the Hub's freight on their backs, cut through the Silver Foundation.
  - The rock is seamless, gently curved and faintly warm, older than the city, with a silver sheen and a hint of rainbow in it.
  - The porters have laid plank treads and rope rails over it.
  - **The Indentured carry here in lock-step**, tethered by lines of white light (two porter chains at the landings, fixed posts). The tethers are shown, never cut: that fight is the Sludge-Tunnels' (shared rules).
- **Light:** cold silver-blue walls, white tether-light, the drivers' orange braziers.
- **Camps** (8 on the way, and 2 in the Skim):
  - **S1** gate drift: 2 Concord drivers.
  - **S2** Collar-Post: the collar clerk (runner) and a driver. The Stair's first watch-bell rope hangs by the cages.
  - **S3** Collar-Post: **the Collar-Smith** and his 2 mates.
  - **S4** Collar-Post: a Concord overseer (elite, 28; he drains with his cane) and 2 drivers' hounds (the wolf model, charcoal).
  - **S5** patrol: 2 drivers walking the Long Stair.
  - **S6** the Turn, the tally-desk: a tally-man (runner) and an overseer. A watch-bell rope hangs on the wall behind the desk.
  - **S7** the Turn, among the crate stacks: 2 Company stair-wardens and a driver.
  - **S8** the Stair Head: Kell and 2 overseers (his guards).
- **Mini-boss: the Collar-Smith** (27, elite; GAME-ONLY). He rivets the collars on.
  - **Riveted (his hold):** every 18 s, everyone within 4 m is rooted for 3 s.
  - **The Riveting Blow:** his heavy blow, with knockback 4.
  - **At 60%** he calls his mates.
  - **Drops:** the Collar-Smith's Hammer (war hammer).
- **Things to use:**
  - **the Collar-Post cages:** eight Indentured sit in cages of iron bars (the Dungeon kit's Bars). Open them as the Adit's cages are opened (E; the quest Lock-Step);
  - **the watch-bell ropes** (E, 2 s to cut; see "Running through all of it").
- **Boss: Tether-Master Ambrose Kell** (28, elite; GAME-ONLY). The Concord's master of porters, who drives every chain on the Stair. A grey coat, and a rod of black iron that the lines of light run from.
  - **The Tether (his hold):** every 16 s, a line of light runs from his rod to everyone within 8 m, rooting them for 4 s.
  - **The Drive** (his lesson; the hazard strip built with the Sludge-Tunnels):
    - Two porter chains in lock-step stand ready at the Stair Head, each at the end of a marked lane 6 m wide across the floor.
    - Every 15 s he lashes one, and it marches the length of its lane, because it cannot stop: "The chain takes up the step."
    - The warning is 5 s, longer than the Tether's root, and the Drive never starts while anyone in the lane is held.
    - Anyone in the lane when it passes takes 30% of max health and a 1 s knockdown. The lanes take turns.
    - The porters cannot be struck and come to no harm. The sims step out of the mark.
    - Fight him between the lanes.
  - **The Weight:** his heavy blow (wind-up 2.2 s, 3.8 times his swing).
  - **At 50%,** a call: the tally-desk camp (S6), if it stands.
  - **When he dies,** three drivers come up the stair behind you: "Below on the stair, a driver's whistle."
  - *Teaches: mind the lanes.*
  - **Drops:** the Tether-Master's keys (for Lock-Step) and Lock-Step Boots.
  - **Afterwards:**
    - With Kell dead, the chains stand still. In Plan D the porters take the Basket's bars: "We'll wind you up. Then we're going down, and we're not coming back." E at the basket, 6 s on the bar, and the party steps out at the deck's Basket Head.
    - In Plan I they point you down the Lower Stair.
  - **Lock-Step**, draft B's tether binding two of the party, is not in the first build. It is added only once the sims can keep beside a partner (section 9).
- **Side area [1b]: the Catalogers' Way.**
  - **The way in:** its mouth is a dark crack behind a stack of crates in the Turn's west wall, not on the map, seen only up close.
  - **Size:** 80 m long, 6 m wide, 3.5-4 m high, level. No camp, so the leader never goes in (shared rules: a narrow side way).
  - **What it is:** the goblins' old service crawl from the days when they catalogued the city's light (CANON-EXPANDED). Chalk marks, the Weavers' crossed shuttles, and shelves of small glass plates holding the light of lamps long gone out. Warm amber candle-stubs. No hostile camps: a quiet room on purpose.
  - **Scrivet** (GAME-ONLY goblin, on the PROVISIONAL stand-in figure Pib uses) is the last cataloger. He has hidden here since the Purge and keeps the Hub's books in secret. He gives The Catalogers' Writ: the quiet way. He gives it here, inside: an exception to the "given outside" pattern, on purpose, because he is a secret (shared rules).
  - **Secret:** the Catalogers' Chart (a note and a Chronicle page): a goblin static-chart of the Hub's service ways.
- **Side area [1c]: the Skim.**
  - **Size:** a branch 55 m long and 10 m wide, opening into a den 18 m across and 7 m high.
  - **What it is:** a Company skim-room, where crooked Sandthrone riders pass geodes out of the haul to the Lowtowns' fences. Their own mark, a rider's spur, is scratched on the wall under shuttered lanterns, over rough rock.
  - **K1:** Company cutters ×3 (non-elite), hidden behind the crates until you are close, as the tall-grass ambushes are (check that it works off the grass).
  - **K2:** **Wace Tuckett, the Skimmer** (28, elite, optional; GAME-ONLY, a Company paymaster gone crooked), with a Company fence (elite) and a cutter.
    - **Flare (his hold):** every 16 s he kicks over a crate of loose geodes, and they flare with the faces in them: everyone within 5 m is disoriented for 3 s.
    - **Behind you:** every 12 s he steps behind whoever he fights.
    - **The Cut:** his heavy blow.
    - **At 30%,** a rally: his cutters.
    - *Teaches: watch your back.*
    - **Drops:** the Skimmer's Weights (neck; coins), Scrivet's seal-die and his take-book (both quest items).
  - **Secrets:**
    - the Skim's Take (a cache behind a loose silver-brick): coin and a raw aether-crystal;
    - **Tuckett's copied key** (search the key-board on the den wall, E). A key tagged "W.H." that he had cut. It opens the Weigh-House coffer: a key in one wing that opens a chest in another.
- **Secret on the Long Stair's landing:** the Featherweight Brick (a cache): a loose silver brick that weighs nothing, a piece of the earlier cycle's work (CANON-EXPANDED from the Silver Foundation).

### [2] The Sovereign Deck (the reveal; the Cargo Hall in Plan I)
- **The reveal (Plan D):** the Basket comes up through the rock into daylight, and you step out onto a platform of iron and marble halfway to the clouds.
  - **Over the south parapet, 40 m down:** the Half-Light's scaffolds and the pools you crossed. Beyond them, the Lowtowns' orange fires in grey smog.
  - **East:** the Spire's black needle, close across the gulf.
  - **North:** the white wall of the High City climbs another 50 m. High in it, a great window glows gold: the Crown Intake, **the end of the dungeon, in sight.**
  - **On the deck:** wind and snapping canvas awnings, crates in rows, porters in chains, and the Haul Bell on its frame.
  - **In Plan I** you come through the Basket Arch onto the Cargo Hall's floor. The same things stand in it, and the arches give the drop and the Spire's flank. The Intake's window is not in sight.
- **Size and build (Plan D):**
  - a mesa top 170 m east to west and 100 m north to south, solid ground (no stacked floor);
  - its open sides are a marble balustrade topped with iron spikes, with colliders and nav blockers along it, never walkable. Knockback stops at it, and a recovery point on the deck catches anyone who leaves the walkable ground.
- **Weather (Plan D only):** smog (thick mist), sooty drizzle, and clear high air when the wind turns.
- **Light:** grey-gold daylight through the smog, and the crimson glow of the caskets' wards.
- **From east to west:**
  - **the Basket Head:** where you arrive. **The spirit stone** stands here, 25 m east of the checkpoint; no patrol walks east of the checkpoint (in Plan I, by the Basket Arch, as far from the hall's first camp);
  - **the Company's checkpoint:** a barrier of crates and a timber gate across the deck;
  - **the cargo rows:** crates on pallets, hand-barrows and chains of Indentured carrying;
  - **the weigh-post**, with a watch-bell on it;
  - **the Haul Bell:** a great bronze bell on a timber frame, rung by hand with a rope. The bell-ringer is not hostile;
  - **the Company Landing** (south-east, off the way; in Plan I a bay at the hall's south end): the Sandthrone's stores and their mules (Donkey critters). **The Low Haul**, a basket on a rope over the edge down to the Half-Light, is wound at a windlass by the Company's porters;
  - **the casket stage** (north-west): twelve black caskets on a timber stage, beside **the porters' back-stair door** in the white wall, where the haul goes in;
  - **the Inner Gate** (north, in the white wall): an arch of white stone under the black sunburst, shut. Beside it is **the Watch Door**, the barracks' way up (not usable).
- **Camps** (7 on the way, and 1 on the Landing):
  - **D1** the checkpoint: 2 Company guards (elite, 28, the outrider look) and the gate clerk (runner). A watch-bell rope hangs at the gate post. It stands only without the writ.
  - **D2** the checkpoint: 2 Company crossbowmen (they shoot, then close in). It stands only without the writ.
  - **D3** the weigh-post: a weigh-clerk (runner) and a Seeker of the deck.
  - **D4** the Haul Bell: 2 Seekers of the deck.
  - **D5** patrol: 2 Seekers walking the deck's length, west of the checkpoint.
  - **D6** the stage foot: Hesta Dunne and 2 carriers-at-arms.
  - **D7** the casket warders: a ward-weaver (she mends) and a Seeker, by the stage.
  - **L1** the Landing (side): the Company quartermaster (elite) and 2 guards.
- **The Haul Bell (the alarm's last word; no clock):**
  - The bell-ringer stands at the Haul Bell with the rope in his hands. He rings it **when the deck is warned**:
    - if the checkpoint is fought (Hesta sends word at once), or
    - if a second watch-bell is rung anywhere on the Stair or the deck before the Ward has begun (one bell is the Stair's own business; two mean trouble).
  - **Rung:** "The Haul Bell rings. The porters take up the caskets." The Indentured shoulder the caskets and carry them in by the porters' back stair. The stage is left empty but for one open casket. The caskets now stand in the Crown Intake's rack (key hub.haul), and two more Seekers of the watch stand by them at the finale.
  - **Kept quiet,** the haul waits on its stage until you reach it. **Once the Ward has begun,** the ringer stops: "The bell-ringer stops with the rope in his hands."
  - Nothing is failed: Wren can still be freed after the Warden.
  - There is no timer and no bar on the HUD. A quiet party keeps the better end however long it takes; a loud one loses it.
- **Gate: the Company's checkpoint.**
  - **Quiet:** show Scrivet's writ. The guards read the old seal and stand aside (the camps stand only without the writ).
  - **Loud:** fight them. Hesta hears, and the Haul Bell rings.
- **Boss: Carrier-Captain Hesta Dunne** (28, elite; GAME-ONLY, of the CANON Sandthrone, the company the Concord hires). She brought the Oakhaven children up from the Peaks herself ("Two children, small, chained at the wrist": main.khaven.3), and the coast's geode crates after them. The wrist-chain hangs from her belt, wound round the haft of her mace.
  - **Chain-Wrap (her hold):** every 16 s, everyone within 5 m is rooted for 3 s as the chain wraps their legs.
  - **The Long Chain:** every 15 s, a ring. Everyone within 5 m takes 1.6 times her swing.
  - **Paid in Full:** her heavy blow, which throws its target back 4 m (the parapet stops it).
  - **On the pull** her 2 carriers-at-arms come. **At 50%** she calls the deck's patrol, wherever it walks.
  - **When she dies,** two carriers walk up from the Landing.
  - *Teaches: stand clear of the chain. Inside 5 m you are rooted in the ring.*
  - **Drops:** her **gate-token** (it opens the Inner Gate and works the Low Haul), the set's gloves, and the Carrier's Chain (a flanged mace, the chain wound round its haft).
- **The event: the Proximity Ward** (the canon scene, b1 ch.1:199-213).
  - **The caskets:** Wren's casket is black and armoured, under the Council's seal. Next to it, **one casket stands open and empty**. Its brass plate reads: "J. Oakhaven. Sent up."
  - **Opening it:** press E at the lock with Nila's Tuning-Shard. The black surface ripples into a scrying mirror and a crimson light pulses. The ward screams. There is no voice: crimson light floods the stage and the white wall, and the text reads "The ward screams. Every Seeker in the Hub can hear it."
  - **The hold:** the shard works the ward for 90 s, with a lock bar over the stage as the Weaver's lock had. The party holds the stage, and the sims guard it as they guarded Mother Quillet.
    - **At 25 s, out of the Watch Door:** 2 Seekers of the watch (elite) and 2 Iron Pact soldiers, up from the guard-barracks.
    - **At 55 s, out of the Inner Gate:** a redaction squad. **Silencer Unit 12** (28, elite; GAME-ONLY, of the CANON Silencers) and 2 Silencers, in grey cloaks and eyeless masks.
  - **Unit 12:**
    - **Redaction Mark (his hold):** every 18 s, everyone within 6 m flees in fear for 3 s ("The eyeless mask turns to you.").
    - **Void-bolts:** his shots grey the one they hit: 10% more damage taken for 12 s. A cleanse lifts it.
    - *Teaches: who to kill first, while the ward screams.*
    - **Drops:** the Eyeless Mask (head).
  - **When the ward breaks,** the note runs down the row and every casket on the stage opens. Wren climbs out, white and cold, and alive. Ten more, children and grown folk, climb out of the others (GAME-ONLY). The Inner Gate stands open behind the squad (key hub.ward).
  - **Without the shard,** the lock only shows its crimson mirror: "Nila Glass-Stitcher, in the Half-Light, sells things tuned to the transit frequency."
- **The event: Wren's walk** (the escort; the cave-with-four-lords lesson).
  - Wren leads the freed across the deck to the Low Haul: 150 m in Plan D, 100 m down the hall in Plan I. She waits for you and ducks when struck, and the freed follow her.
  - **At half way, one wave:** three carriers-at-arms climb the Low Haul's ladder to take back the Company's freight. If the Landing has not been cleared, its camp joins in: clear it first, or fight through.
  - **At the Haul:** Tibb's people are waiting. The Company's porters see Tibb's knife and wind the freed down. Kept for good: hub.wren.
- **Things to use:** the writ at the checkpoint; Wren's casket (the shard); the Low Haul (with Hesta's token, E and a 5 s bar, down to the Half-Light: the short run's way out, and back up); the spirit stone.
- **Secret:** Halfway to the Clouds, a vista at the south-west parapet (in Plan I, the Lookout Arch at the hall's north-east corner). The drop, the Lowtowns and the Spire. Far below, between two tenements, a line of washing hangs over a stack of cabbage crates: a nod to b1 ch.1:223-227, Valen's landing, not far off now.

### [3] The Concourse (built hall; the hub of the wings)
- **What it is:** the Hub's counting-hall, through the Inner Gate. In Plan D it is cut into the white wall; in Plan I it lies past the Cargo Hall's north wall.
  - a marble floor, rows of high clerks' desks, and counters with brass scales;
  - the ordinances in gold letters on the walls, among them "Ordinance 9. Interference with Council Business" (b1 ch.1:303);
  - the black sunburst over every door;
  - **the spirit stone**, in the vestibule's side bay: 20 m from the hall's first camp, and the round turns 25 m short of it;
  - **three doors, each lit its own colour, with a sign:** white (the Guard-Barracks, down), violet (the Weigh-House) and crimson (the Ink-House). From the stone, each door's light can be seen;
  - at the far end, the Seal Gate and its frame with three empty slots for the docket's stamps.
- **Light:** warm lamplight on marble.
- **Size:** the vestibule is 20 m long, 12 wide and 10 high. The hall is 120 m long, 40 m wide and 20 m high, with walls 15 m either side of its spine, three courses high, and columns every 8 m.
- **Camps** (5):
  - **C1** the hall's south end (30 m): 2 Seekers.
  - **C2** the west desks: 2 clerks (runners). A watch-bell rope hangs by the desks. **A blank carriage-docket** lies on the desk (E, once C2 is down; Scrivet's writ carries one too).
  - **C3** the counters: 2 Seeker escorts (elite).
  - **C4** the Seal Gate: 2 Iron Pact door-wards (elite).
  - **C5** **the Floor-Marshal's round:** Floor-Marshal Rennick Prowse and 2 Seekers, the length of the hall from 35 m to 135 m, about three minutes a round.
- **The hub changes while you are away.** After each wing's boss falls, a redaction squad of 3 Silencers comes in by the Seal Gate's side door and walks the hall: the first after one boss, the second after two, the third after three (camps keyed on the wing bosses' deaths). The longer you are below, the busier the Concourse grows.
- **Mini-boss: Floor-Marshal Rennick Prowse** (28, elite; GAME-ONLY).
  - **"Halt!" (his hold):** every 16 s, everyone within 4 m is stunned for 2 s.
  - His heavy blow knocks back 4 m.
  - Fight him, or wait in a doorway for him to walk on.
  - **Drops:** the Marshal's Baton (mace).
- **The Seal Gate.** It opens to a docket with **two** of its three stamps. Each stamp is an act in its wing, not a kill:
  - **Warded,** in the Guard-Barracks: stand the docket under the ward-glass in the Mask-Room (E, 3 s). The glass marks it as one of theirs.
  - **Weighed,** in the Weigh-House: stand in the Great Scale's pan while the balance is worked, and the weigh-clerk's stamp on the beam-post marks the docket (E at the beam, 5 s).
  - **Written,** in the Ink-House: enter the party's names in the copyists' roll at the Scriptorium's far desk (E, 5 s, once the Scriptorium's lamp is out).
  - The keys are hub.stamp.warded, hub.stamp.weighed and hub.stamp.written.
  - Set the docket in the gate's frame with two stamps and the ward fails, and the grille is wound up by hand. The line: "Nothing goes up that has not been weighed, warded and written."
  - **Each wing's lieutenant left alive** (the Hundredth, Quarle, Vellum) goes to the Warden by the officers' stair when you pass the Seal Gate, and stands beside him at the finale (section 4, [4]).
  - The run waits here: "Stamps on the docket 0/2."
  - The gate does not open to the wing bosses' deaths: a last gate opened by the bosses at its corners would read as a copy (shared rules).

### [3a] The Guard-Barracks (a stone stronghold; white)
- **What it is:** where the Seekers come up from (canon: they "ascended from the guard-barracks"). Bare white stone, iron and no ornament, and cold white lamps in iron cages. Everything in rows.
- **The rooms:**
  - **the barracks stair:** 40 m, 10 m wide, 7 m high, falling 8 m;
  - **the Bunk-Hall:** 50 m, 28 m wide and 10 m high, with cots and footlockers in rows. **The Mask-Room** opens off its south wall as a side bay, where hundreds of gold masks hang on peg rails, and **the ward-glass** stands at its end (the Warded stamp);
  - **the Armoury:** 30 m, 16 m wide and 8 m high, with weapon racks, lead boxes of void-bolts (CANON, b1 ch.15:183) and whetstones on benches;
  - **the Drill Hall:** 60 m, 44 m wide and 14 m high, falling to 14 m below the Concourse. Sanded floor, dummies at the far end, and at its end **the Seekers' Cage** in its well.
- **Camps** (7):
  - **B1** patrol: 2 Seekers walking the Bunk-Hall.
  - **B2** the Mask-Room: a **Muster squad**, a Seeker blade, a Seeker crossbowman (shoots, then closes) and a ward-weaver. The order's fanatics: they mend each other and call.
  - **B3** the Armoury: Pact-Sergeant Ruel Bannard and 2 soldiers.
  - **B4-B6** the Drill Hall: **the Ranks**, three pairs of Seekers standing **still** along the walls, facing in. A rank pulled alone brings no other rank.
  - **B7** the Hundredth, in the middle of the Drill Hall.
- **Mini-boss: Pact-Sergeant Ruel Bannard** (28, elite; GAME-ONLY, of the CANON Iron Pact).
  - **Pike-Wall (his hold):** every 18 s, the pikes come level: everyone within 6 m flees in fear for 3 s.
  - His heavy blow knocks back 5.
  - **At 60%** he calls his soldiers.
  - *Teaches: hold the line.*
  - **Drops:** the Pact-Sergeant's Pike (polearm).
- **Boss: the Hundredth** (28, elite; GAME-ONLY). The captain of the Hub's hundred Seekers, who has no name, only the count. White plate, a gold mask, and a tower shield notched a hundred times.
  - **Rank and File (his hold):** every 18 s, the shield-wall slams: everyone within 8 m is stunned for 2 s.
  - **The Ranks** (the tribute lesson, built from the calls that exist): at 75, 50 and 25% he barks "Rank!", and the nearest rank still standing steps out and joins him. Thin the ranks first, a pair at a time, or fight him with them.
  - **The Count:** his heavy blow.
  - **When he dies,** any rank still standing comes for you: "They step out of rank without an order. That has never happened before."
  - *Teaches: thin the guard, then the captain.*
  - The order's finale pair (a commander, and a priestess who raises him once) is the Adit's Ember Vent, and is not repeated here (shared rules).
  - **Drops:** the Hundredth's Count (a heater shield) and the set's shoulders.
- **The ride back:** when the Hundredth falls, the Indentured at the cage's windlass will wind it for you: E, a 5 s bar, and the party stands in the Concourse by the white door. In Plan I this is A's cage ride up from the deepest wing. The leader walks back. The cage is for players until the leader can take rides (section 9).
- **Rare: the Unmasked** (29, elite, one run in five). He walks the passage between the Armoury and the Drill Hall: a Seeker who has lost his mask and keeps his face turned to the wall. You never see the face (PROVISIONAL: canon leaves the rumour open).
  - **He Turns (his hold):** every 20 s, everyone within 5 m flees in fear for 3 s: "He turns his face toward you. You run before you see it."
  - A heavy blow and an enrage.
  - **Drops:** a Seeker's mask with a name scratched inside it (head; a better piece).
- **Secret:** A Name Inside the Mask, a note and a Chronicle page in the Mask-Room. One mask among hundreds has a name scratched inside it with a nail, and the words "I was a baker".

### [3b] The Weigh-House (built halls; violet)
- **What it is:** where every geode and every casket is weighed, graded and logged. Dark timber and brass. Long sorting tables lit from below by the geodes' sick violet light, and racks of polished geodes, each with a face in it. The goblins kept these books before the Purge (CANON-EXPANDED); now the Bureau's assessors keep them. It is the one great scale in the three later dungeons (shared rules).
- **The rooms:**
  - **the Weigh-Floor:** 60 m, 36 m wide and 16 m high. **The Great Scale** stands in the middle: a timber beam with two brass pans, each 6 m across, on chains. A balance worked by hand;
  - **the Sorting Halls:** 70 m, 28 m wide and 10 m high, with the long tables. **The Polishing Room** opens off the north wall as a side bay, 18 m across;
  - **the Counting-Room:** 40 m, 24 m wide and 10 m high. The Assayer's desk, the ledgers and the coffer. A bookcase stands in its end wall;
  - **the Sealed Room**, past the bookcase door: 30 m, 16 m across and 8 m high, the wing's own end (not a branch of a branch).
- **Camps** (4 on the way, 1 in the bay, and the Keeper behind the bookcase):
  - **W1** the Weigh-Floor: a weigh-clerk (bolts) and a tally-clerk (runner). A watch-bell rope hangs at the Scale's foot.
  - **W2** the Weigh-Floor: 2 Bureau assessors (elite; they drain).
  - **W3** patrol: 2 assessors walking the Sorting Halls.
  - **W4** the Polishing Room (side): 2 polish-masters (elite; they drain) and 2 polishers (runners). A rope hangs in the bay.
  - **W5** the Counting-Room: Quarle and 2 apprentices (casters who heal her).
  - **W6** the Sealed Room: the Keeper of Seals (hidden).
- **Things to use:**
  - **the Great Scale** (the Weighed stamp);
  - **the geode racks** (E: "Let it go dark."). The face in it smiles, or doesn't, and its light goes out. Six of them for Faces in the Light;
  - **the ledger** on Quarle's desk (E, for The Factor's Name);
  - **the Weigh-House coffer** (a chest; Tuckett's copied key);
  - **the bookcase door** (Quarle's brass key).
- **Boss: Assayer Imelda Quarle** (28, elite; GAME-ONLY). She grades every geode, and every child, by the weight of what is in them. Her word for anything short is "Light." Her three memories are told by the text and by the face flaring in her geode, and are built from phases that exist (a rally, a stamp and a terrify).
  - **Weighed (her hold):** every 18 s, everyone within 6 m is stunned for 2 s: "She weighs you, and you are still while she does it."
  - **The Verdict:** her heavy blow (wind-up 2 s, 4 times her swing). Every 12 s she steps behind whoever she fights.
  - **At 70%,** a rally: her apprentices heal her (interrupt them), and the Sorting Halls' patrol comes if it stands.
  - **At 66%, a porter's memory:** she drinks the light out of a geode. Everyone near her is stunned for 2 s, and she comes back a fifth heavier: "She swings like a man who carried for thirty years."
  - **At 33%, a child's memory:** the party near her cowers ("She remembers being small, and so do you").
  - *Teaches: read the change. Three fights in one.*
  - **Drops:** the Assayer's Loupe (off-hand), the set's legs, and **her brass key** (the bookcase door).
- **The ledger** (for The Factor's Name): "Received of Factor Hamlin Orr, the coast station: forty crates, weighed and paid. Countersigned, C. Mordaunt, Sovereign-Warden."
- **Hidden boss: the Keeper of Seals** (30, elite; GAME-ONLY). The ward-weaver who set every ward in the Hub.
  - **Found, not raised.** Behind the bookcase, under the Bureau's black sunburst, he sits at his work among three black caskets that have never been opened (dressing: they stay shut, and nothing in the room raises him). He rises from his desk when you come in.
  - **The Room in the Glass (his hold):** every 18 s, everyone within 6 m is disoriented for 3 s. Their steps go the wrong way, as if the room were seen in a mirror.
  - **Every 12 s** he steps out of the nearest ward-glass, behind whoever he fights.
  - **At 30%** he enrages.
  - **Drops:** the Scrying Glass (off-hand; luck), the better piece.
  - The sim leader never opens the bookcase.
- **Secrets:**
  - Weighed, a vista at the Great Scale's foot: someone has chalked a mark where you would stand, and beside it, small, "Light.";
  - the Weigh-House Coffer (a chest; Tuckett's key);
  - **the Auditor**, a vista in the Sealed Room after the Keeper falls (PROVISIONAL; pillar 8). Through a grille in the far wall, something impossibly tall in an eyeless gold mask stands with its back to you, turning the pages of the Hub's ledgers. Look again and the room behind the grille is empty, the ledger open at today's date. It is never fought and drops nothing.

### [3c] The Ink-House (built halls; crimson and black; the library)
- **What it is:** where the Hub writes everything down, and strikes people out. It is the one record room with a boss in the three later dungeons (shared rules).
  - the Census-rolls on shelves without end;
  - lecterns and copying desks;
  - vats of the Inquisitors' black ink;
  - crimson carpet that swallows every footstep;
  - gold leaf on the rock overhead;
  - suppression wards (CANON-EXPANDED from the Concord library's wards): pale gold lamps that hush every voice.

  The Inquisitors keep it. Level, in both plans.
- **The rooms:**
  - **the Roll-Galleries:** 80 m, 24 m wide and 12 m high, lined with shelves;
  - **the Scriptorium of Redaction:** 50 m, 28 m wide and 12 m high, with lecterns, ink vats and the copyists' desks; **the copyists' roll** at its far desk (the Written stamp). **The Census Vault** opens off its north wall as a side bay of the oldest rolls;
  - **the Redactor's Chamber:** 40 m, 26 m wide and 12 m high.
- **The Hush** (the wing's mechanic; the library-ward lesson):
  - Four ward-lamps: one in the galleries, one in the Scriptorium, two in the Redactor's Chamber.
  - Within 12 m of a lit lamp, your casts take half again as long.
  - A ward-keeper (a caster who heals) keeps each lamp. Kill the keeper and the lamp goes out.
  - The leader marks the keepers to kill first.
  - If the Hush has to wait (section 9), the wing still stands on its runners and keepers.
- **Camps** (6 on the way, and 1 in the Vault):
  - **I1** the Roll-Galleries: lamp 1, a ward-keeper and an Inquisitor novice (non-elite; bolts).
  - **I2** the Roll-Galleries: 2 paid informers (runners; GAME-ONLY Lowtowners who came to sell names). A rope hangs at the gallery's end.
  - **I3** patrol: 2 Inquisitors walking the galleries.
  - **I4** the Scriptorium, the copyists' desks: 4 clerks, all runners, with two ropes at the far end. The runner rule at its hardest.
  - **I5** the Scriptorium: lamp 2, a keeper and an Inquisitor (elite; drains).
  - **I6** the Census Vault (side): the Census-Keeper and 2 Silencers (elite).
  - **I7** the Redactor's Chamber: Vellum and the keepers of lamps 3 and 4 (his guards).
- **Mini-boss: the Census-Keeper** (28, elite, optional; GAME-ONLY). He reads the names aloud, in chat: "Garris, of the Lower District. Hoarding aether."
  - **Your Name, Read Aloud (his hold):** every 16 s, everyone within 5 m flees in fear for 3 s.
  - **The Long Roll:** his heavy blow.
  - **At 60%** he calls.
  - **Drops:** the Long Roll (off-hand).
- **Boss: Redactor Ennis Vellum** (28, elite; GAME-ONLY). An Inquisitor who answers to High Inquisitor Vax Mal-Cor (named only). He strikes names out of the Census, and the people go with them. He is the one Redactor in the three later dungeons (shared rules).
  - **A Line Through Your Name (his hold):** every 16 s, everyone within 6 m is rooted for 4 s: "He draws a line through every name near him. Your feet won't move."
  - **Redaction, at 50%** (the reset): "For a moment you don't know who you were fighting." He forgets whom he was fighting, and the tank must take him back.
  - **At 25%** he terrifies.
  - **The Hush:** the keepers of his chamber's two lamps fight beside him.
  - **When he dies,** two Silencers walk in from the Vault.
  - *Teaches: kill the keepers, then take him back when he forgets you.*
  - **Drops:** the Redactor's Pen (a knife) and the set's feet.
- **Things to use:**
  - the copyists' roll (the Written stamp);
  - the roll in the Census Vault (E: "Look for Garris", for Hoarding Aether);
  - the intake rolls in the Census Vault (E: "Look for the berths", for Berths for Ten Thousand).
- **Secrets:**
  - an echo-jar on a top shelf of the Roll-Galleries (a cache): a voice reading out a manifest of names (the Silent Pilgrims' jars are CANON);
  - Your Own Name, a note in the Census Vault: your own name, already entered in a clean hand, with the verdict left blank.

### [4] The Crown Stair and the Crown Intake (the finale; white and gold)
- **The Crown Stair:** through the Seal Gate, three flights of white stone with gilded rails and two landings. The first clean light in the dungeon falls from above. In Plan D the stair climbs 20 m; in Plan I it runs down 10 m (question 1).
- **Camps on the stair** (4):
  - **X1** a Praetorian pair (elite; slow, heavy maces; knockback);
  - **X2** 2 gilded Seekers on the first landing;
  - **X3** 2 gilded Seekers on the second landing;
  - **X4** on the top landing, Praetorian Ostrand, the Stair-Ward, and 2 Praetorians.
- **Mini-boss: Praetorian Ostrand, the Stair-Ward** (29, elite; GAME-ONLY, of the CANON Praetorian Guard).
  - **"Kneel!" (his hold):** every 18 s, everyone within 6 m is stunned for 2 s.
  - **Mace of the Stair:** his heavy blow, with knockback 6 (the rails stop it).
  - **At 60%** he calls.
  - *Teaches: the gatekeeper.*
  - **Drops:** the Mace of the Stair.
- **The Crown Intake:** 120 m long, 48 m wide and 26 m high, with walls 19 m either side and columns every 9 m.
  - **Light:** in Plan D, white-gold daylight from the great window high in its south wall (the one you saw from the deck). In Plan I the same window opens on a painted sky, as the Cargo Hall's arches do. In both, the violet-white of prism-lamps.
  - the casket rack, empty, or full if the haul went in;
  - **the Warden's dais and the great lamp:** a dais of white stone 12 m across under a great lamp that burns polished geodes (CANON: every light in the High City burns a stolen memory). Its light falls on the dais and nowhere else;
  - at the far end, **the Spire's cargo door**, black, under the black sunburst, sealed: the raid's door, set in the Spire's flank where it meets the wall;
  - in the west wall, **the Crown door**, shut;
  - in the east wall, **the porters' back stair**, the way the haul comes in.
- **Camps** (2), and those that stand only on a key:
  - **X5** Mordaunt and 2 Praetorians on the dais;
  - **X6** 2 Praetorians at the Spire's door (they come at 50%);
  - **a pair of Seekers of the watch for each watch-bell rung**, at most three pairs (keyed on the alarm);
  - **two more Seekers of the watch by the rack** if the haul went in (keyed on hub.haul);
  - **each wing's lieutenant left alive**, alone: the Hundredth without his Ranks, Quarle without her apprentices, Vellum without his lamps (keyed on each wing boss's death not being kept).
- **Last boss: Sovereign-Warden Cassian Mordaunt** (29, elite, `harder`, `boss:true`; GAME-ONLY).
  - **Who he is:** the master of the Hub, and Lysara's man (named only). He wears a Seeker's white plate gilded at every edge and an Inquisitor's crimson half-cloak, and carries a gold sceptre-mace. He wears no mask: his is the only face in the Hub's command.
  - **In the Light of Office** (his lesson):
    - While he stands in the great lamp's light on the dais, a ward stands round him: he takes 60% less harm and mends half a percent of his health a second.
    - Off the dais he is only a man. The tank pulls him off on the pull and fights him with his back to the light.
    - **The Sovereign's Word, at two thirds and one third:** he says one word in the voice of the Crown, and every member of the party is stunned for 2 s, wherever they stand. While you cannot move, he walks back into the light: "He steps back into the light." Pull him off again. His blows are a fifth heavier each time.
    - Nothing in the room holds the ward up but where he stands: no stones to break and no one bound in the lamp.
  - **The Chain of Office (his hold):** every 16 s, everyone within 6 m is rooted for 4 s.
  - **By Order:** his heavy blow (wind-up 2.2 s, 3.6 times his swing).
  - **On the pull** his 2 Praetorians come, with the alarm's pairs, the rack's guards and any lieutenant. **At 50%,** a rally: the 2 Praetorians at the Spire's door.
  - **At 30%** he enrages: "You've put me out of the light. Now there's only me." On the dais, he mends instead.
  - **No single blow over 60%** of a member's health.
  - **The play:** the tank takes the Warden off the dais and holds him there. The others take his guards and any lieutenant. After each Word, the tank pulls him off again. Then burn him down.
  - *Teaches: where he stands matters, and the run you chose stands with him.*
  - **Drops:**
    - **the Writ of Carriage** (every time);
    - the Sovereign-Warden's Sceptre (mace);
    - the Chain of Office (neck);
    - the set's chest;
    - coin.
  - **When he falls:**
    - **the porters' back stair** opens as the way out. The Indentured who carried the haul show you down: E at its door, a 5 s bar, and the party stands in the Half-Light by the Porters' Gate. "The way they've come up for seven years." No ride: a walk;
    - **the Crown door** opens. It is a zone exit into the Crown once the Crown is built;
    - if the haul went in, the caskets in the rack open with his death, Wren and the ten climb out, and they walk out with you (hub.wren);
    - PROVISIONAL (question 3): through the grille of the cargo door, a figure in a weeping silver mask, an iron chain at his belt, looks at you for a long moment and turns away up into the Spire. He cannot be reached, and the door stays sealed.
- **The Writ of Carriage** (the letter is GAME-ONLY; its parts are CANON):
  - a writ under the Investigation Bureau's black sunburst, in Lysara's hand;
  - it names its bearer a Senior Scribe of the Spire's intake;
  - a sliver of Tuning-Shard on the Council's transit frequency is set in the wax;
  - "Bearer and haul to pass the Spire's cargo door at any bell."
  - It opens the Spire of Agony (the raid; Phase 9 endgame).

### Running through all of it

**The runners and the watch-bells (quiet or loud, the whole way):**
- **Who runs:** clerks, tally-men, weigh-clerks, polishers, paid informers and the gate clerk (non-elite). At 40% health each breaks for the nearest watch-bell rope.
- **What happens:** one that reaches a rope and lives 3 s there rings it. The chat says "A watch-bell rings somewhere above." There are ten ropes in all, each within about 12 m of its runners.
- **Stopping a runner:**
  - kill it;
  - hold it (stun, root, incapacitate, or a marked sap from a sim Rogue; the leader marks runners for the sims' holds);
  - or cut the rope before the pull (E, 2 s). A cut rope stays cut, and its runners, with nowhere to run, fight on.
- **The alarm:** each bell rung is kept (hub.alarm, up to three). Each puts one more pair of Seekers of the watch beside the Warden. **A second bell rung on the Stair or the deck before the Ward has begun also rings the Haul Bell** (the haul goes in). It does nothing else, so the rule stays simple.
- **Outside, in the Half-Light,** there are no ropes. The collectors' runners run for the next camp and bring it: the flee-for-help lesson.

**The Haul Bell:** on the deck only. Rung by a fight at the checkpoint, or by a second watch-bell before the Ward. Late costs the better end, never the run. No clock.

**Gates, keys and rides:**

| Gate or ride | Opens to | Plan |
|---|---|---|
| the Porters' Gate | always open | both |
| the Collar-Post cages (8) | E | both |
| the Porters' Basket (up to the deck) | Kell's death (the porters he drove wind it) | D |
| the Company's checkpoint | the Catalogers' Writ (quiet), or a fight (loud: the Haul Bell rings) | both |
| the Inner Gate | Hesta's gate-token, or the end of the Ward (hub.ward) | both |
| the Low Haul (down to the Half-Light, and back up) | Hesta's gate-token | both |
| the Seal Gate | a docket with two stamps of three (acts, not kills) | both |
| the Weigh-House coffer | Tuckett's copied key | both |
| the bookcase door | Quarle's brass key | both |
| the Seekers' Cage (the Drill Hall up to the Concourse) | the Hundredth's death | both |
| the porters' back stair (out to the Half-Light) | the Warden's death | both |
| the Crown door | the Warden's death; a zone exit once the Crown exists | both |
| the Spire's cargo door | sealed; the Writ opens it for the raid | both |

**How each boss holds the party** (0.07 and the shared rules: every named elite, not just the tank; every kind is EliteMove.cc today):

| Boss | Hold | Kind | Reach | Every | Seconds |
|---|---|---|---|---|---|
| Collector Gaunt | Lime in the Eyes | disorient | 6 m | 18 s | 3 |
| the Collar-Smith | Riveted | root | 4 m | 18 s | 3 |
| Tether-Master Kell | the Tether (with the Drive's lanes) | root | 8 m | 16 s | 4 |
| Wace Tuckett | Flare | disorient | 5 m | 16 s | 3 |
| Hesta Dunne | Chain-Wrap (with the Long Chain's ring) | root | 5 m | 16 s | 3 |
| Silencer Unit 12 | Redaction Mark | fear | 6 m | 18 s | 3 |
| Floor-Marshal Prowse | "Halt!" | stun | 4 m | 16 s | 2 |
| Pact-Sergeant Bannard | Pike-Wall | fear | 6 m | 18 s | 3 |
| the Hundredth | Rank and File | stun | 8 m | 18 s | 2 |
| Assayer Quarle | Weighed (and her stamp and terrify) | stun | 6 m | 18 s | 2 |
| the Census-Keeper | Your Name, Read Aloud | fear | 5 m | 16 s | 3 |
| Redactor Vellum | A Line Through Your Name (and his terrify) | root | 6 m | 16 s | 4 |
| Praetorian Ostrand | "Kneel!" | stun | 6 m | 18 s | 2 |
| Sovereign-Warden Mordaunt | the Chain of Office (and the Word on the whole party at ⅔ and ⅓) | root | 6 m | 16 s | 4 |
| the Keeper of Seals | the Room in the Glass | disorient | 6 m | 18 s | 3 |
| the Unmasked | He Turns | fear | 5 m | 20 s | 3 |

Each kind appears three to five times, and each fight adds its own tool: the ring, the blink, the knockback, the Drive's lanes, the Ranks, the memories, the reset or the light on the dais.

**Things to use:**
- the Collar-Post cages;
- ten watch-bell ropes;
- the Porters' Basket;
- the writ at the checkpoint;
- Wren's casket (the shard);
- the Low Haul;
- the docket, and its three stamps (the ward-glass, the Great Scale, the copyists' roll);
- six geode racks;
- Quarle's ledger;
- the Weigh-House coffer;
- the bookcase door;
- the Seekers' Cage;
- the Census roll and the intake rolls;
- the Seal Gate's frame;
- the back stair;
- two spirit stones.

**Secrets (ten):**
1. the Featherweight Brick (cache, the Long Stair);
2. the Catalogers' Chart (note, the Catalogers' Way);
3. the Skim's Take (cache);
4. Tuckett's copied key (key, the Skim);
5. the Weigh-House Coffer (chest, the Counting-Room);
6. Halfway to the Clouds (vista, the deck);
7. A Name Inside the Mask (note, the Mask-Room);
8. Weighed (vista, the Great Scale);
9. the echo-jar (cache, the Roll-Galleries);
10. Your Own Name (note, the Census Vault).

The Auditor (vista, the Sealed Room) is an eleventh, PROVISIONAL.

**The crowd (frame rate and navmesh; shared rules):**
- **The figures:**
  - two porter chains at the Long Stair's landings (6 each, 12);
  - Kell's two marching chains at the Stair Head (6 each, 12);
  - the eight Indentured in the Collar-Post cages;
  - the deck's carrying chains (8), the bell-ringer and the Company's mules (3);
  - Wren and the ten freed (11);
  - the Indentured at the Seekers' Cage (4) and at the back stair (6).
- **The budget:**
  - **No more than about 40 animated figures in any one view.** The worst views are the Stair Head in Kell's fight (24 porters and the fight) and the casket stage in the Ward (the freed, two waves and the party).
  - Standing chains and caged figures get no NavMeshAgent and no collider; each plays one looped clip (lock-step, or sitting), offset in time, culled off-screen.
  - **Each marching chain moves as one:** one agent leads it down its lane and the other five are parented to it, with no agents of their own.
  - Wren and the freed get agents only while they walk to the Low Haul, and are removed there.
  - None of them carves the navmesh.
  - **A frame-time capture** at the Stair Head and on the deck before either is dressed further, and in the Ward at its busiest moment. If any is worse than the Adit's Rail Hall, the Stair's chains drop to 4 each and the freed to 6.

## 5. Mobs (every individual is GAME-ONLY; the factions are CANON)

| Where | Mob | Faction (label) | Level | Behaviour |
|---|---|---|---|---|
| The Half-Light | collectors' runner, collector, the Finder's crew | the High Concord's collectors (the Concord CANON; the collectors the game's own) | 27 | runners run for the next camp at 40%; collectors call |
| The Stair | Concord driver, drivers' hound | the High Concord (CANON) | 27 | drivers patrol the stairs, a lash at range, then close; hounds pack |
| The Stair | Concord overseer, the Collar-Smith's mates | the High Concord | 27-28 | overseers drain (with a cane; no collar is played here) |
| The Stair | Company stair-warden | the Sandthrone (CANON; here GAME-ONLY) | 27 | in pairs; a heavy blow |
| The Stair, the deck, every hall | collar clerk, tally-man, gate clerk, weigh-clerk, tally-clerk, clerk, polisher | the High Concord | 27 | runners: break for a watch-bell rope at 40% |
| The Skim | Company cutter, Company fence | the Sandthrone (CANON; the skim GAME-ONLY) | 27-28 | cutters lie hidden behind the crates |
| The deck | Company guard, crossbowman, carrier, carrier-at-arms, quartermaster | the Sandthrone (CANON; here GAME-ONLY) | 27-28 | crossbows shoot, then close in; the checkpoint stands aside to the writ |
| The deck, the Concourse, the Barracks | Seeker of the deck, Seeker escort, Seeker blade, Seeker crossbowman, gilded Seeker | the Seekers (CANON; here only) | 28 | never flee, never speak; stunned, held and knocked back like anyone (b1 ch.1:271) |
| The deck, the Barracks | ward-weaver | a CANON calling (b1 ch.15) | 28 | mends an ally under 70% (interruptible) |
| The waves, the Concourse, the Ink-House | Silencer (redaction squad) | CANON (the squads CANON-EXPANDED) | 28 | its blows grey: 10% more damage taken for 12 s; a cleanse lifts it |
| The Concourse, the Barracks | Iron Pact soldier, door-ward | the Iron Pact (CANON; its men serving here CANON-EXPANDED) | 27-28 | pikes; door-wards hold the Seal Gate |
| The Barracks | the Ranks | the Seekers | 28 | stand still until called |
| The Weigh-House | weigh-clerk, Bureau assessor, polish-master, apprentice | the Investigation Bureau (CANON) | 27-28 | clerks cast bolts; assessors and polish-masters drain; apprentices heal |
| The Ink-House | ward-keeper, Inquisitor novice, Inquisitor | the Inquisitors (CANON; here only) | 27-28 | keepers heal and keep the Hush; novices cast bolts; Inquisitors drain |
| The Ink-House | paid informer | GAME-ONLY Lowtowners (no faction) | 27 | runners |
| The Crown | Praetorian, gilded Seeker, Seeker of the watch | the Praetorian Guard; the Seekers (CANON) | 28-29 | slow and heavy, with knockback; never flee |
| Not hostile | the Indentured, the bell-ringer, the Company's mules, Nila, Tibb, Tull Marsh, Mags Dorran, Arn Feld, Gerda Holm, Scrivet, Wren and the freed | | | |

**Totals:**
- **54 camps and about 130 mobs**, counting the hidden Keeper and the rare (the Adit: 67 and 148). The run is held to section 0's 45-60 minutes (shared rules: run length).
- **About 160 fought in a full clear,** with:
  - the Ward's two waves (7);
  - the walk's wave (3);
  - three redaction squads (9);
  - the reliefs after Kell, Hesta and Vellum (7);
  - the alarm's pairs (0-6), and the rack's pair if the haul went in;
  - any lieutenant left alive.
- **Patrols (six):** the Long Stair, the deck, the Floor-Marshal's round, the Bunk-Hall, the Sorting Halls and the Roll-Galleries. Then the three squads, the rare's walk and the reliefs.
- **Named elites:** sixteen, counting the hidden and the rare.

**Looks:**
- **The Seekers** take canon's two pictures, one for each role. White plate and gold masks go on the deck, the Concourse and the Barracks (b1 ch.1). Grey cloaks and eyeless masks go on the Silencers (b1 ch.15). This is a GAME-ONLY choice that reconciles the two.
- **The porters** are living Indentured in grey, collars glowing, heads down, in lock-step. There are no Spent here.
- **No machines anywhere:** no golems, drones, turrets or engines. Every guard is a living person.

## 6. Quests (given outside, in the Half-Light)

**Where the givers stand:**
- Tibb, Tull Marsh, Mags Dorran, Arn Feld and Gerda Holm stand in zone.hub's approach, outside the Porters' Gate. Nila stands there too, as a seller; she gives quests only on Chris's yes (question 7). Their quests can be tested without any other zone.
- **Scrivet gives his quest inside**, in the Catalogers' Way. That breaks the "dungeon quests given outside" pattern on purpose: he is a secret, found only by finding the Way. (The Adit's pattern: Pib gives The Weaver's Lock outside; Mother Quillet gives no quest. QUEST_DESIGN, The Sealed Adit.)
- Lisle Tamber and Brother Cael stand in the Half-Light until the Lowtowns or the Crown exist (ROADMAP 9.1), then move there.

**Format:**
- Levels 27-29, minLevel 26. XP and coin follow the 27-29 curve, set when the cap rises.
- Kills name the Hub's camps (`mob.<tag>.hub.*`). The bosses' items are "collect from a kill". Flags are `key:hub.*` objectives.

1. **What Goes Up** (`side.hub.wren`).
   - **Giver and turn-in:** Tibb (GAME-ONLY; an urchin of the CANON Blind Crow's network).
   - **Zone:** zone.hub. **Level:** 28.
   - **canonStatus:** CANON-EXPANDED (the Hub, its caskets, Oakhaven's three candidates); Wren, Tibb and the rescue are GAME-ONLY.
   - **Offer:** "Your Oakhaven two came up the Porters' Stair a fortnight back, chained at the wrist. The boy's gone up already. The girl's in a box on the deck, waiting on the haul. Keep the deck quiet and the haul waits." Rill (CANON, named only) sent her.
   - **Steps:**
     1. Reach the Sovereign Deck (visit).
     2. Free Wren: open her casket and see her to the Low Haul, or open the rack in the Intake after the Warden (`key:hub.wren`).
     3. Return to Tibb.
   - **Rewards:**
     - XP and gold;
     - Salt-Menders standing;
     - a choice of three pieces;
     - the Chronicle page *Not Yet*, Tibb's letter for Sel Harrow: Wren is safe in the Warrens with the urchins, and cannot come home while the Bureau's list still holds her name;
     - freed before the Haul Bell, a second page as well, *Before the Bell*.
   - **Later (Phase 9):** carry the page to Sel Harrow in Oakhaven.
2. **Tuned to the Transit** (`side.hub.shard`).
   - **Giver:** Tibb (Nila on Chris's yes, question 7). **Level:** 27.
   - "Nila sells the shards. She won't sell for any silver but the kind she trusts."
   - **Steps:**
     1. Bring six soul-prism slivers from the collectors' runners.
     2. Bring Collector Gaunt's silver (collect from the kill).
     3. Buy Nila's Tuning-Shard at her door with it.
   - **Reward:** Nila's Tuning-Shard, kept. It opens the Proximity Ward. (The one Tuning-Shard among the three later dungeons' rewards, shared rules.)
   - **Her warning** as she sells it (canon: her shard set off Valen's ward): "It'll open the box. It won't keep it quiet. Nothing keeps those quiet. And if you see a black one with the Bureau's sun on it, leave it be."
3. **Lock-Step** (`side.hub.lockstep`).
   - **Giver:** Tull Marsh (GAME-ONLY; an escaped porter). **Level:** 27.
   - **Steps:**
     1. Open the eight Collar-Post cages on the Stair.
     2. Bring the Tether-Master's keys (collect from the kill).
   - **Rewards:** a quest piece; Salt-Menders standing.
   - Tull: "Seven years. That's the usual."
4. **The Catalogers' Writ** (`side.hub.cataloger`). The quiet way.
   - **Giver:** Scrivet (GAME-ONLY goblin), inside the Catalogers' Way (the exception above). **Level:** 27.
   - **Step:** bring back his seal-die from the Skim (collect from Tuckett).
   - **Rewards:** the Catalogers' Writ (kept; it passes the checkpoint, and carries a blank docket) and the Chronicle page *The Light, Catalogued* (the goblins' years at the Hub; CANON-EXPANDED).
   - It is found only by finding the Way, which is a secret.
5. **The Factor's Name** (`side.hub.factor`).
   - **Giver:** Lisle Tamber (GAME-ONLY Salt-Mender scholar). **Requires:** Where the Light Goes (`side.sludge.light`), the Sludge-Tunnels' quest that found the factor's initials and station. **Level:** 28.
   - It does not start from A Letter Under Seal: the chain runs the Adit, then the Sludge-Tunnels, then here (shared rules: the story chain).
   - **Steps:**
     1. Read the coast factor's name in the Counting-Room ledger (E).
     2. Bring down Assayer Quarle.
   - **The end:** "Hamlin Orr. H.O. Now I can write to him. And Mordaunt I can't write to at all."
   - **Rewards:** Salt-Menders standing, gold, a piece.
   - It is the hook toward Port Caelum (Phase 9).
6. **Faces in the Light** (`side.hub.faces`).
   - **Giver:** Brother Cael (GAME-ONLY; a Silent Pilgrim, CANON order, the Adit's echo-jars). **Level:** 28.
   - **Step:** let six polished geodes go dark in the Sorting Halls (E).
   - **Reward:** the document *What the Faces Said* (six fragments).
   - **Label:** CANON-EXPANDED (geodes as memory, the Pilgrims).
7. **Hoarding Aether** (`side.hub.garris`).
   - **Giver:** Mags Dorran (GAME-ONLY; old Garris's neighbour; Garris is CANON). **Level:** 28.
   - **Steps:**
     1. Find Garris in the Census-rolls (E in the Census Vault).
     2. Bring down the Census-Keeper.
     3. Bring her the page.
   - Mags burns it: "Better he's nowhere than in their book."
   - **Rewards:** XP, gold, a piece.
8. **A Hundred Masks** (`side.hub.masks`).
   - **Giver:** Tull Marsh, after Lock-Step (GAME-ONLY). **Level:** 28.
   - "The Hundredth counted us up the stair every morning. I want his mask."
   - **Step:** bring down the Hundredth and bring Tull the mask.
   - **The end:** he turns it over. Inside, it is smooth, polished and warm. "Warm. Huh." The rumour stays open, as canon leaves it.
   - **Rewards:** a piece, gold.
9. **The Company's Own** (`side.hub.skim`).
   - **Giver:** Outrider-Captain Arn Feld (GAME-ONLY, of the CANON Sandthrone). **Level:** 28.
   - Wace Tuckett has been skimming the Company's haul. The Company deals with its own.
   - **Step:** find his room and bring his take-book (collect from the kill).
   - **Rewards:** coin and a piece, and Sandthrone standing at a cost in Salt-Menders standing (the Sandthrone contracts' pattern: a real choice).
10. **The Writ of Carriage** (`side.hub.carriage`).
    - Dropped by Mordaunt and turned in to Tibb (to Nila on Chris's yes). **Level:** 29.
    - Tibb takes it to Nila's door and comes back: "Nila says it's not a pass. It's a key."
    - It leads to the Spire of Agony (the raid).
    - **Label:** the letter is GAME-ONLY; its parts are CANON.
11. **Berths for Ten Thousand** (`side.hub.berths`; the answer to the Deep Veins' writ).
    - **Giver:** Gerda Holm (GAME-ONLY; the Salt-Menders' woman from the Bread Drop). **Requires:** The Count-Master (`side.veins.master`); the Deep Veins' A Writ for the Hub sends you here. **Level:** 28.
    - **Steps:**
      1. Find the order the Count-Master's writ answers, in the Census Vault's intake rolls (E: "Look for the berths").
      2. Bring it to Gerda.
    - **What it says:** "Berths below the Warren District: ten thousand, by the year's turn, for the Spire's intake." It says no more. What the Spire takes them for stays unsaid.
    - **Rewards:** Salt-Menders standing; the Chronicle page *Intake*.
    - **Label:** CANON-EXPANDED (the pens, b1 ch.24:21); the order GAME-ONLY. PROVISIONAL with the Deep Veins' question 3 (the number).

## 7. Loot

**The set: the Haul-Breaker's Kit** (rare, 5 pieces):
- the shoulders, from the Hundredth;
- the legs, from the Assayer;
- the feet, from the Redactor;
- the gloves, from Hesta Dunne;
- the chest, from the Warden.

A lieutenant left alive and fought at the finale drops its own table there, set piece and all.

Its bonuses: at 3 pieces, health; at 5, a breath of power back on each kill (onKillPower).

**Every named elite drops one of its named pieces every time** (0.075; shared rules), with 20% extras. The group's roll decides who takes it, and the scorecard says who did. (The draft's 50% drops are gone.)

| Boss | Named pieces |
|---|---|
| Collector Gaunt | Finder's Silver (neck; luck) |
| the Collar-Smith | the Collar-Smith's Hammer (war hammer) |
| Tether-Master Kell | Lock-Step Boots (feet) |
| Wace Tuckett | the Skimmer's Weights (neck; coins) |
| Hesta Dunne | the Carrier's Chain (flanged mace) |
| Silencer Unit 12 | the Eyeless Mask (head) |
| Floor-Marshal Prowse | the Marshal's Baton (mace) |
| Pact-Sergeant Bannard | the Pact-Sergeant's Pike (polearm) |
| the Hundredth | the Hundredth's Count (heater shield) |
| Assayer Quarle | the Assayer's Loupe (off-hand) |
| the Census-Keeper | the Long Roll (off-hand) |
| Redactor Vellum | the Redactor's Pen (knife) |
| Praetorian Ostrand | the Mace of the Stair |
| Sovereign-Warden Mordaunt | the Sovereign-Warden's Sceptre (mace); the Chain of Office (neck); the Writ |

**The hidden and the rare** each have one better piece:
- the Keeper of Seals: the Scrying Glass (off-hand; luck);
- the Unmasked: the Named Mask (head).

**Materials and coin:**
- **Geode Shards** (`mat.geode_shard`, the Blacksmith's material) from the deck's carriers, the Weigh-House's clerks and polishers, and the Sorting Halls' racks. They are also owed to the Adit's trash (WEB_LOG).
- **Silver-brick dust** from the Stair's trash: a new smithing reagent (CANON-EXPANDED, the Silver Foundation).
- **The Company's pay-chits** as coin.
- **Coin and crystal** from the Skim's Take and the Weigh-House Coffer (secrets).

**Quest gear:** What Goes Up (a choice of three), Lock-Step, The Factor's Name, Hoarding Aether, A Hundred Masks and The Company's Own (one piece each).

**Documents:**
- the Finder's Book;
- Tuckett's take-book;
- the Catalogers' Chart;
- Garris's page;
- *What the Faces Said*;
- *Not Yet* and *Before the Bell*;
- *The Light, Catalogued*;
- *Intake*;
- the Writ of Carriage.

## 8. Atmosphere

**Light, by part, so you always know where you are:**

| Part | Light | Lining |
|---|---|---|
| The Half-Light | rust-orange braziers in grey smog; violet-green pools | (outside) |
| The Porters' Stair | cold silver-blue; white tether-light; the drivers' braziers | seamless silver-brick with a faint rainbow; plank treads |
| The Catalogers' Way | warm amber candle-stubs | old plaster and chalk |
| The Skim | shuttered yellow lanterns | rough rock |
| The deck / the Cargo Hall | grey-gold daylight through smog; crimson ward-glow | (outside) / silver-brick vault and railed arches |
| The Concourse | warm lamplight on marble | white stone |
| The Barracks | cold white | white limestone |
| The Weigh-House | violet, from below | dark timber and brass |
| The Ink-House | candle-dim crimson and gold; pale gold halos round the wards | black, with gold leaf |
| The Crown | white-gold daylight (Plan D) or painted sky (Plan I); violet-white prism-lamps; the great lamp's pool on the dais | white marble and gold |

The Seal Gate's frame carries the three wings' colours: white, violet and crimson.

**The two halves:**
- the porters' side: rope, timber, plank and iron, smog, the shuffle of lock-step, and the drivers' whistles;
- the Concord's side: marble, gold, silence and order.

**Sound** (as text and light until the game has sound, KNOWN_ISSUES):
- the Half-Light drips and creaks;
- the Stair has the shuffle of feet in lock-step and a driver's whistle;
- the deck has wind, canvas, and the Haul Bell if it rings;
- the watch-bells ring "somewhere above";
- the Concourse has the scratch of pens and, after each wing, the squads' boots;
- the barracks have boots on stone, and silence;
- the Weigh-House has the Scale's chains;
- the Ink-House has nothing at all: the wards;
- the Crown has bells far off.

**Weather:** only in the Half-Light and on the deck (Plan D): smog, sooty drizzle, clear high air. Nothing falls inside. In Plan I, smog drifts in through the arches (the mist particles).

**A layout you can read:**
- the deck shows where you came from (the Half-Light below) and, in Plan D, where you are going (the Intake's window above);
- every door off the Concourse is lit its own colour and carries a sign;
- no branch splits twice;
- each side area is lit its own colour, or found only up close on purpose (the Catalogers' Way).

**No machines (0.05):** the rope rides, the haul, the bells, the Scale, the wards and the great lamp are as section 1 says. No word in the player's text is a machine word.

## 9. What has to be built (in order)

**0. Before anything.**
- **The cap past 29:**
  - LevelCap;
  - the quests' minLevel;
  - the sims' levels and talents;
  - balance targets at 27-29 (CLAUDE_HANDOFF: "set them again for levels 16-30");
  - RunCapture's fixed level 11 made a setting.
- **New looks:**
  - Seekers (two cuts);
  - Inquisitor and novice;
  - Praetorian;
  - Iron Pact soldier;
  - the Indentured porters (grey smocks, glowing collars, tether lines shown only);
  - the Concord's collectors and drivers (the warden kit recoloured), and the drivers' hounds (the wolf, charcoal);
  - the Company in Sandthrone colours, and its crooked cutters;
  - Silencers;
  - a goblin figure, or the PROVISIONAL stand-in.

  They need Phase 8.5's armour fitting to look right.

**1. Choose the plan (question 1), and for Plan D prove two things first.**
- **The terrain:** zone.hub with the Half-Light's low ground, the deck's mesa 40 m up (faced with iron legs and scaffold) and the massif to about 90 m with cliff faces. Tests:
  - no navmesh leaks off the deck;
  - nobody can walk, be knocked or warp past the parapet;
  - a recovery point on the deck.

  Fallback heights: the deck 25 m, the plateau 60 m.
- **A cave that climbs:** Hollow adds each row's drop to the mouth's height, so a positive drop should make the floor rise, with the roof still under the land. Prove it with cave A's plan and a HubNavTests copy of the Adit's walk-to-every-camp test.

If either proof fails, build Plan I. Its floors only go down, and its widths and heights stay inside the Adit's. It loses the climb, the open deck and the end in sight (section 4, "Two plans, one design").

**2. The layout script:** `tools/wip/dungeon/hub_layout.py`, a copy of `adit_layout.py`.
- Its CAVES list:
  - Plan D: caves A and B, five branches, no branch of a branch;
  - Plan I: one spine, five branches.
- The camps by (cave, along, aside) and the patrol routes. In Plan D also the Half-Light's and the deck's camps on open ground.
- Checks: every way the leader walks at least 10 m wide; no stair over 25°; no chamber over the Rail Hall's 85 × 28 m; each spirit stone 20 m from every camp's centre and off every patrol's road.
- It writes zone.hub's plans and camp centres, and `Docs/art/hub-plan.png`.

**3. Built, not cave** (the lesson of the Adit's draft 1). This is the biggest piece of work: `ZoneBuilder.Hub.cs`, cavern variant 4.
- **Linings, after AditLining:**
  - seamless silver-brick and plank treads for the Stair;
  - white stone for the Concourse, the Barracks and the Crown;
  - dark timber for the Weigh-House;
  - black and gold for the Ink-House.
- **Halls that read as roofed,** from the room builder's kit (0.3).
- **The edge:**
  - Plan D: the parapet;
  - Plan I: kit Window and Entrance pieces as railed arches, with colliders and nav blockers, a painted drop behind them (smog, the Lowtowns' fires, the High City's lights, the Spire's flank), and grey daylight through them; the Intake's great window on a painted sky.
- **Furnishing helpers:**
  - caskets and the stage;
  - the Great Scale (beam, chains, pans);
  - windlasses;
  - the Haul Bell's frame;
  - the Collar-Post cages;
  - porter chains of collared figures with tethers of light, standing and marching;
  - mask rails, the ward-glass, cots, Census shelves, ink vats;
  - ward-lamps, the great lamp and the dais under its light;
  - bell-ropes;
  - crate stacks as NavBlockers;
  - the great window.
- **Lights by part.**

**4. Encounter code.** Most of it exists; the new pieces are small and few.

| What | Today | Work |
|---|---|---|
| Holds on the party (stun, root, fear, disorient, with a reach), heavy blows, rings, knockback, blinks, enrages, calls, callOnPull, deathCall, holdable, the rally, stamp, terrify and reset phases | exist (EliteMove, EncounterEnemy.Boss) | data: sixteen EliteMoves entries |
| The grey (Silencers, Unit 12's shots) | exists (EncounterEnemy.Grey) | data, under its own name |
| Mob casts (bolts, drains, heals) and interrupts (Mira hushes; sims with an interrupt) | exist (EncounterEnemy.Casts) | data |
| Patrols, the ambush (check it off the grass for the Skim), the rare, keys from bodies, spirit stones, secrets with keys and chests | exist | data |
| Phase holds on the whole party (the Sovereign's Word; Quarle's and Vellum's phases) | a stamp or terrify holds only you | small |
| The runners: flee to a rope at 40% and ring after 3 s; a rope cut with E; the alarm kept in Progress.keys; a sim with a hold uses it on a runner | flee runs from the foe and comes back; CC marks exist | small to medium |
| Keyed camps: stand only with a key, or only without one (the alarm's pairs, the squads, the lieutenants at the finale, the checkpoint standing aside, the rack's guards) | none | small (one field on ZoneCamp) |
| The docket: n of m stamps from things to use, keys per zone | LiftWoken ("adit.lift") is a global constant | generalise |
| The Drive: the hazard strip (built with the Sludge-Tunnels) on a timer, two lanes taking turns, with a chain of figures marching each lane | the hazard strip, once the Sludge-Tunnels build it | small |
| The light on the dais: a boss warded while he stands in a marked circle, walking back into it under the Word; the sim tank fights a marked boss outside the circle | none | small to medium |
| The Ranks: camps that stand still until called | camps stroll | small |
| The escort made general: stages (hold the stage against two waves from two doors, then a walk with one wave), freed figures that follow, a key at the end | AditWeaver and EncounterSession.Weaver are the Adit's own (the Deep Veins generalises it too: build once) | generalise |
| The Haul Bell: rung on a key (the checkpoint fought, or a second watch-bell before hub.ward); the haul swapping caskets by key, and the rack's guards | none | small (no clock, no HUD bar) |
| Rides made general: the Basket (Plan D), the Low Haul, the Seekers' Cage, the back stair; the Crown door as a zone exit that opens on a key | RideCarriage | generalise |
| The Hush: casts slowed near a lit lamp while its keeper lives; the leader marks keepers to kill first | none | medium. It can wait; the wing still works without it |
| Shared with the other two (built with whichever comes first): the party of five; BoundStone's deepest stone and the zone's own line | | small |

**5. The leader.**
- **Plan I:** today's Gather is enough: one root cave, branches by distance, the gate watch taken first. New: pass optional caves and shut optional gates (the Catalogers' Way, the Skim, the Landing, the Polishing Room, the Census Vault, the Sealed Room) unless the run is a full clear. It walks back out of a wing; the cage is for players.
- **Plan D:** a run order in the zone data: the Half-Light's route, cave A, the Basket, the deck's route, then cave B and its branches. It walks to a ride and waits for you to work it, as it waits at a shut gate.
- **Both:**
  - it still waits for the party and opens no gate itself; it walks to each stamp and waits for you (the run tool works the stamps as it opens the Adit's gates);
  - the sims guard the stage and follow the escort as they did Mother Quillet;
  - the tank pulls the Warden off the dais (the sim rule in step 4);
  - no race mode is needed: there is no clock.
- **Later, only if wanted:** a "stamp and go" switch for the fast way, and the leader taking rides in Plan I.

**6. Content.**
- `Quests/hub.json`: eleven quests, their items and documents. The givers are residents of the Half-Light.
- `Items/loot.hub.json`: the set, the named pieces (every one at chance 1), silver-brick dust, Geode Shards in the trash tables.
- zone.hub from hub_layout.py.
- **Registration:** in the scene's ZoneBuilder.zones and in Encounter.asset's questFiles and itemFiles (a missing zone broke 35 tests once), and the zone's place on the world map, between the Lowtowns and the Crown.

**7. Tests.**
- **Reach:**
  - HubNavTests: every camp can be walked to from the way in (in Plan D, across the rides);
  - every way the leader walks 10 m wide, no stair over 25°, and the spirit stones clear of camps and roads;
  - no camp or leader on the stage, the Scale, a column top or a crate stack;
  - nobody leaves the walkable ground by walking, knockback, a runner's flight or a fear (Plan D: the parapet; Plan I: the arches).
- **Mechanics:**
  - each boss's holds and phases;
  - the runners, the ropes, the alarm's pairs and the Haul Bell (rung by the checkpoint, rung by a second bell, not rung once the Ward has begun);
  - the docket at 2 of 3, with each lieutenant left alive at the finale;
  - the Drive (never on a held member; its warning longer than the Tether's root);
  - the Warden in and out of the light, and back into it under the Word;
  - the Ward and Wren's walk (PlayMode).
- **Content:** the quests, end to end, with The Factor's Name after the Sludge-Tunnels' and Berths for Ten Thousand after the Deep Veins'.
- **The group run:** one Chris can watch (`group_run.ps1 --crulanda-zone zone.hub`, at level 28), measured against the short (21), main (48) and full (57-59) marks.

**8. Later, only if wanted.**
- **Lock-Step:** a tether binding two of the party for 8 s, snapping them together if they part. Only once the sims can keep beside a partner.
- **The Vax glimpse** (question 3).
- **The Hush,** if it waited.

**Cut from both drafts, and why:**
- **A's nine-bell clock from the Door, and B's 20-minute Haul Bell clock.** A timed rescue read as a copy of a classic run, and sim-led runs would have arrived late. The haul now goes in on the alarm, not the clock.
- **A's Emerald Line car hauled out over the smog, the great cradle, the Spire windlass's hundred unseen hands and the cage wells; B's Crown Hoist (a gallows-arm and a twelve-bar capstan) and its treadwheel question.** Chris said no contraptions at all. Now three people-worked rope rides at most, the haul carried in by hand, and the way out on foot.
- **A's 104 m Great Floor.** It is wider than any chamber built. The Cargo Hall is 84 m (shared rules).
- **A's givers in two zones that don't exist.** Every giver is in zone.hub's Half-Light, so the quests can be tested.
- **B's Pale Auditor as a killable boss.** It breaks world bible pillar 8. The Keeper of Seals is the hidden boss; the Pale Thing is only glimpsed.
- **B's Seekers immune to stun and knockback, and A's "does not stagger" Captain.** Both make a rumour a rule, and canon shows Seekers knocked back (b1 ch.1:271). It stays a rumour (Tull's "Warm. Huh.").
- **B's Struck from the Roll aimed past the tank, the Assayer's move-swapping phases, and the Ranks' void-bolt volleys.** Each was new code. They become holds on everyone near and phases that exist.
- **B's Weave-Eaters in the Polishing Room.** A stretch. Polish-masters and runners take their place.
- **B's Lock-Step.** It needs new pairing AI. Lock-Step waits on the sims.
- **A's hidden-door search behind a crate wall.** New code. The bookcase opens to a key, and the crack is simply seen only up close.
- **A's nested branches.** Untried. Side rooms are bays, or a wing's own end behind a gate.
- **The seals from the wing bosses (B's all three, then two of three).** A last gate opened by the bosses at its corners reads as a copy. The docket's stamps are acts, and the lieutenants are a menu with a cost.
- **The camp count.** B had 61 camps and the merged draft 70, reading "more to do" as more camps than the Adit's 67. Section 0 asks for 45-60 minutes, which at the Adit's pace is about 54 camps; "more to do" is read as something new at least every five minutes (shared rules).
- **B's Doone (a disarm only on you) and Haskett (knockback only).** Neither held the party. Both, now Tuckett and Bannard, hold everyone near.
- **Cut on 2026-10-14, so the three later dungeons do not repeat each other or copy a classic encounter whole** (shared rules):
  - Kell's tethered porters, cut to stop his mending: tethers that feed a boss are the Sludge-Tunnels'. Kell's lesson is the Drive;
  - Mordaunt's binding-stones round the dais: a boss's shield held up by stones round the room reads as a copy. His ward is the light he stands in;
  - Mouse, a child bound in the lamp beside the Warden who must not be harmed: a captive at the ruler's side reads as a copy. The lamp burns geodes;
  - the three black caskets that brought out the Keeper of Seals when broken: boxes that bring out their keeper read as a copy. He is found behind the bookcase;
  - Ward-Sister Aldis Fenn, who restored the Hundredth once: the Adit's Ember Vent pair;
  - the Spent on the Stair (the Sludge-Tunnels'); the porters are living;
  - the Iron Teeth's skim (the Sludge-Tunnels'); the skim is the Company's own;
  - the Copper-Tithe as enemies (question 6), Mother Haggitt among them; the collectors instead;
  - silver dust as a boss's disorient: it is the readers' bane and the Sludge-Tunnels' party buff; Gaunt throws quicklime;
  - "Insufficient" as the Assayer's word: it is the Architect's, and the Sludge-Tunnels' Former Miner's;
  - Gorm as a giver (the Sludge-Tunnels' question 2), and Nila as one without asking (question 7);
  - "Tuned to the Cargo Lifts" as a quest title: no "lift" in player text.
- **Folded together:**
  - A's Pryor, Strake, Carrick, 4-Alpha and Orme go into B's bosses: Kell takes Carrick's place, the Hundredth takes 4-Alpha's, Tuckett takes Gessel's rally, Mordaunt takes Orme's Chain of Office;
  - Holt (now Floor-Marshal Prowse) and the Keeper of Seals keep their roles.

**The biggest risks:**
1. **Plan D's terrain, and the leader across caves, open ground and rides** (steps 1 and 5). Plan I is the answer if they fail, at the cost of the climb.
2. **The Hub reads as a cave again** (the Adit's draft 1 fault). The linings, the roofs and the parapet or arches are the work that prevents it.
3. **The length.** By the Adit's pace the full clear is 57-59 minutes, close to section 0's 60. Measure on the group runs, and trim in the order section 3 gives.
4. **The crowd** on the Stair Head and the stage (the budget in section 4).
5. **The new looks,** which wait on Phase 8.5.
