# The Sealed Adit: dungeon design (draft 1, 2026-10-07; levels 10-12 since the cap-15 rescale, see ROADMAP)

Chris: "7-9 dungeon creation that rivals wow dungeons like these. research these completely before dungeon design. leveling
takes longer in land of crulanda", then "not 3..one big dungeon" and "we will design others later". So this is **one** big
dungeon for **levels 7-9**. It takes the best of three classic dungeons (research below). It uses **no** names, assets or
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
- Bosses that each teach one thing. The pilot whose machine you kill first and then him (two-part fight). The first mate who
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

- Crulanda levels slower (cap 15 now, 30 in the end; the Peaks are 9-12). The classic band of about 15-24 out of 60 maps to **10-12** here (the numbers below are the pre-rescale 7-9 and move up by three with round 29):
  - trash 7-8;
  - branch bosses 8;
  - the hall's bosses 9;
  - the last boss 9 elite, `harder`;
  - the hidden boss 10.
- **Length:** a full clear is 55-75 minutes with a party of sims (Chris, 2026-10-07). It is built so it **does not have to be done in one go**:
  - **Wing 1 plus one branch is a 25-minute run** (the short-dungeon lesson);
  - a **spirit stone at the Gallery** (the respawn point once reached);
  - **the rail as the way out** at the end (fixes the long walk back).
- Party: you and up to four sims (the round-27 dungeon runs: a sim leads, camp by camp).

## 4. The layout

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
        │  three sigils wake the cage-lift
        ▼
   [4] THE RAIL HALL  ── the reveal: a vast cavern, the old mag-rail, an armoured rail-carriage being loaded with geodes
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
- What changes: the seal is broken open. Sandthrone pickets (level 7) stand on the yard, with a cart track running inside.
- The quest givers are near by (section 6).
- The door is a dark cut with lum-tube green light inside.

### [1] The Old Workings (wing 1, the learning wing)
- Timbered drifts, rusted ladders, ore carts on bent rails, dripping water, green lum-tubes.
- Trash: Sandthrone diggers (non-elite, in 2s), overseers (elite, with whips), sappers (throw a charge you must step out of).
- **Pressed goblins** sit in cages. They are not hostile. The quest "The Pressed" frees them, and freed goblins help later.
- **Boss: Gang-Boss Haddo Lusk** (8, elite). He hits hard and slow and has a lot of armour, with two guards.
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
  - a **rock-borer** engine that the goblins repair if you kill it before them.
- **Boss: Nix and the Rock-Eater** (8, elite). Two-part fight:
  - first the Rock-Eater, a goblin boring-engine;
  - when it breaks, Nix, the goblin who sold out his own Warren (a Razzle-like traitor; GAME-ONLY), jumps out and fights on
    foot, vibration-locking your weapon (disarm for 4 s).
- Drops the **Amber Sigil**.

### [3b] The Ember Vent (the Cult of Ash: the lava chasm)
- A hot fissure off the workings. A path runs round a lake of molten rock, with ash in the air and red light.
- The Ash cell feeds geodes to the fire, because they think the memories burning in them are an offering.
- Trash:
  - Ash initiates in groups of 2-3 (one heals, one **casts fear**: plan the pull);
  - ember elementals (non-elite, pulled 1-2).
- **Boss: Cinder-Warden Ysolt** (8, elite).
  - **Ember Blow:** an uppercut that **knocks the tank back** (into the lava if you stand at the edge, so fight her in the
    middle).
  - **Flame Ring:** a burst round her, the same knockback idea.
- Optional: the **Vent-Hound** (8, elite), which wanders the far ledge.
- Drops the **Ember Sigil**.

### [3c] The Grey Breach (the Wasting: the corrupted cave)
- The eastern drifts end where the void has already eaten the rock:
  - grey static instead of stone, and edges that flicker;
  - silence where the Gallery sings;
  - one patch of floor that is not there.
- Trash: **greyed** cave beasts (bats, crawlers, a bear), unmade at the edges:
  - they **flee at low health** and bring more;
  - a grey caster puts one of you **to sleep** (interrupt it).
- **Boss: The Foreman Who Forgot** (8, elite). A Sandthrone foreman half unmade.
  - He **blinks** in and out.
  - At half health he **forgets** his target (aggro reset).
  - He **terrifies** at 25%.
- **Rare (one run in five): the Quiet Miner**, an older, wholly grey shape who walks the deepest drift.
- Drops the **Grey Sigil**.
- Killing the Foreman opens a **short way back** to the Gallery (the waterfall-shortcut lesson).

### [4] The Rail Hall (the finale)
- **The cage-lift** wakes when the three sigils are set in its frame, and goes down a long way.
- **The reveal:** you step out into the biggest space in the game:
  - an old station cut into the mountain's root, high and dark;
  - the mag-rail line running off into the black both ways;
  - on it, an **armoured rail-carriage** loaded with crates of glowing geodes, Sandthrone loading it from a platform;
  - steam and lamps, and the sound of the Gallery far above.
- **The gate onto the platform:**
  - **quiet:** a freed goblin (from "The Pressed") vibration-locks it open, and the hall does not know you are there;
  - **loud:** blasting powder from a crate in the side gallery blows it open, and the Quartermaster sends a wave at you.
- **Boss: Quartermaster Brannigan Sorrel** (9, elite).
  - **Two guards jump you** from the platform's sides as he comes down.
  - **At 2/3 and 1/3 health he stamps:** the whole party is stunned for 2 s while he goes to his weapon rack, and comes back
    with a heavier weapon (more damage each time).
- **The event: the Weaver's unlocking.**
  - The freed goblin elder, **Mother Quillet** (GAME-ONLY), must reach the carriage's resonance lock and match it. Matched,
    the geodes go dark (their memories let go) and the carriage cannot run.
  - You escort her down the platform. **Two waves** come: Sandthrone from the carriage, then dockers from the far tunnel.
  - **The surprise:** with the lock half matched, the carriage roof opens and the last boss stands up.
- **Last boss: Rail-Captain Orsk Danner** (9, elite, `harder`), fought on the carriage roof.
  - **When he is attacked, two guards step out of the shadows. At half health, two more.**
  - **Kill the guards and he calls new ones** (so burn him down).
  - He carries **a sealed letter** addressed to the Sandthrone's commander (CANON; named only), which starts the next chain.
- **Hidden boss: Old Kettle** (10, elite), the hall's cook, below the platform's far side. Drop down to find him. He throws
  the pot.
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

## 9. What has to be built (engine work, in order)

1. **Cavern variant 3** (the Hollow cavern builder): the Workings, the Gallery hub with three branches, the cage-lift shaft and
   the Rail Hall as one cavern system under the Peaks' Sealed Adit. Lights by part.
2. **Boss mechanics:**
   - phase triggers at health thresholds (stun the party, a weapon change, summon guards, aggro reset, terrify, knockback);
   - a two-part fight (machine, then pilot).
3. **Dungeon objects:**
   - gates (opened by a key, a freed NPC, or powder, the loud way alerting a boss);
   - sigils in a frame;
   - the cage-lift;
   - patrols released when a boss dies;
   - a rare spawn chance;
   - a spirit stone.
4. **The escort event:** an NPC who walks, waves on a timer, a boss who rises at a step.
5. **Mob behaviours:** call for help, flee at low health, sleep, fear, knockback, a charge to step out of.
6. **Quests and loot:** the seven quests, the set and boss tables.
7. **Sim support:** the dungeon run leads through the wings and branches in order, waits at gates, and joins the escort.
8. **The rail ride out.**

Biggest risk: step 1 (the largest cavern by far) and the knockback into lava (it must never put anyone under the world).
