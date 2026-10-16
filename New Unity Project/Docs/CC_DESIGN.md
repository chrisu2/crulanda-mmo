# Crowd control: design (draft 1, 2026-10-08)

Chris, after his first run of the Sealed Adit: "good start overall but needs to be deeper, and will be hard to do since we don't have
any CC classes yet, so that is something we need to look at next." Classic dungeons are built on it: a pull of three to five, one or two
taken out of the fight while the group kills the rest, casters interrupted. Crulanda has slows and roots (Ranger Pin and Snare, Druid
Stillroot and Briar Snare) but nothing that takes a mob out of a fight, no interrupts, and sims that would break CC anyway.

## Decisions for Chris (my picks first)
1. **Classic strength (pick):** long CC (20-30 s) that breaks on damage, short stuns (3-4 s) that don't. (Or modern: everything short.)
2. **Marking with keys (pick):** Ctrl+1 skull (kill first), Ctrl+2 moon (CC), Ctrl+3 cross (second CC), Ctrl+4 clear. Sims read the marks.
   (Or a right-click menu on the target frame.)
3. **One CC and one interrupt per class now,** and two CC classes (section 0): the Rogue, then the Archivist.

## 0. Two CC classes (Chris, 2026-10-08: "Archivist and Rogue both sound good. I love them in Crulanda")
Both drawn from the novels (CANON roots; the kits GAME-ONLY until checked against the books), both playable and both sims.

**The Rogue** (Valen's line, book 1: Thief's Grace, spent shards, vibration-locking). Melee, stealth, the classic pull-setter.
- **Sap**: from stealth, out of combat, a person or beast held 40 s; breaks on damage. The long CC that starts a pull.
- **Gouge**: a 4 s incapacitate in the fight (breaks on damage); **Blind**: a shard of dust in the eyes, the mob wanders 10 s.
- **Kick**: interrupt, silences 3 s. **Vanish**: drop out of the fight into stealth. **Pick lock** (the Adit's quiet gate).
- Damage between: combo points to a finisher. Leather, daggers. Talents: Thief's Grace (stealth, Sap), Shardwork (poisons, blind), Locksmith (vibration-locking: open locks and break a caster's ward).

**The Archivist** (the Silent Pilgrims, the Original Song, echo-jars; world_bible). A song-caster: the purest CC, little damage.
- **Lull**: a hummed line; up to three mobs within 6 m sleep 20 s; breaks on damage. **Echo Bind**: one mob held in its own echo 30 s.
- **Hush**: interrupt and silence 5 s (the Pilgrims' vow made a weapon). **Unsong**: a mob that breaks its CC is slowed 50%.
- Songs (one at a time, around the Archivist): **Cadence** (the party moves and swings faster), **Dirge** (enemies slowed).
- **Echo-jar**: stores a mob's death-cry to replay as a fear on the next pull. Cloth, a staff or a bell. Talents: the Silent Vow (silences, interrupts), the Lull (sleeps, mesmerise), the Archive (echo-jars, songs).

Order: the Rogue first (closer to what the game has: melee, stealth already exists as sneaking), then the Archivist. Both take a
class slot each, a talent tree, an AI rotation for sims, looks and gear rules, as the five classes did in Phase 5.1.

## 1. The engine (on every mob)
| State | What it does | Breaks | Length |
|---|---|---|---|
| Incapacitated (hex, sleep, trap) | stands still, out of the fight: no swings, no casts, no calls, no threat | any damage (a damage-over-time too) | 25 s (elite 18 s) |
| Stunned | can't act | never | 3-4 s |
| Feared | runs away from its fearer, then comes back | 3 hits, or the time | 6-8 s |
| Silenced (interrupt) | its cast is stopped; it can't cast again for a while | never | 4 s |
| Rooted / Slowed | (already in the game) | damage past a share of its health (roots) | as now |

Rules:
- **Diminishing returns:** the same kind of CC on the same mob within 18 s lasts half as long; the third time it's immune.
- **Who's immune:** bosses take no long CC (stuns and interrupts only); beasts and plants can be slept, people hexed, the unmade
  only turned (Paladin). Each mob's kind says what works on it; the target frame says "Immune" when you try.
- When CC ends the mob comes back for whoever CC'd it (threat on them), so a CCer re-applies or the tank picks it up.
- A CC'd mob neither calls for help nor answers a call.

## 2. One CC and one interrupt per class (GAME-ONLY names)
| Class | CC | Interrupt |
|---|---|---|
| Mage (fire) | **Ash Hex**: turns a person or beast to a smouldering ash statue, 25 s, breaks on damage | **Quench** (the existing ability) stops a cast and silences 4 s |
| Druid | **Sleep of the Wood**: a beast or plant sleeps 25 s, breaks on damage | **Thorn Lash**: stops a cast |
| Ranger | **Snare Trap**: a trap on the ground; the first mob to step in is held 20 s, breaks on damage | **Pin** (existing) gains a 2 s interrupt |
| Paladin | **Rebuke**: stun 4 s; **Turn the Unmade**: the Hollow and the greyed flee 12 s | **Censure** (existing) silences 3 s |
| Warrior | **Shout**: nearby mobs flee 6 s (breaks easily) | **Shield Bash**: stops a cast, silences 3 s |
| Mira (healer) | none | **Hush**: silences a caster 4 s, when you ask (or by herself on a sleep or a heal) |

## 3. Marks and the sims
- Marks over heads (skull, moon, cross) and on the target frame. You set them (keys above); a sim leading a dungeon run marks for you
  on a pull of three or more: skull on the caster or the hardest hitter, moon and cross on what its party can CC.
- Party sims: the tank opens on skull; the damage assists skull; a sim with a CC puts it on its mark at the pull and puts it back
  when it breaks; nobody hits a moon or a cross. Sims say it in Party: "hexing moon", "trap down", "moon's loose!".
- Interrupts: each sim with one watches for casts in its reach and kicks the dangerous ones (sleeps, heals, fear).

## 4. What you see
- The CC on the mob: an ash statue, a sleep's drifting motes, a trap's jaws, a stun's stars, a fear's flight; a ring under it with
  its time running down; its name plate dimmed.
- The target frame: the state and its seconds ("Ash Hex 18 s"); "Immune" when it won't take.
- Combat text: "Hexed", "Broken!", "Interrupted".

## 5. The Sealed Adit around it
Pulls of three or four with one caster; the Grey Breach's sleepers want interrupts; the Ember Vent's fear and menders want CC on the
menders; the Rail Hall's waves want stuns and a trap. With CC in, the dungeon gets deeper (more rooms, longer branches), its pulls
tuned so a group without CC struggles and a group that uses it doesn't.

## 6. Build order
0. **The engine and the two classes come first** (Chris: a CC class); the per-class CC follows. Status 2026-10-08: C1 done; the Rogue and the Archivist playable (sims, pick-lock and their own looks still to do); C3 marks done (Ctrl+1-4; helpers leave held and moon/cross mobs alone); sims as Rogues and Archivists, sims using CC, and the per-class CC next.
1. **C1 the engine:** the states, the breaks, the diminishing returns, the immunities, threat on release; tests.
2. **C2 the abilities:** one CC and one interrupt per class, on the action bars, with their looks; Mira's Hush.
3. **C3 marks and sims:** the keys and markers; sims using CC and interrupts and marking on a sim-led run.
4. **C4 feedback:** the visuals, rings, frame text, combat text.
5. **C5 the Adit:** pulls re-tuned around CC, casters given real casts to interrupt, then the dungeon deepened.

## Signature moves for every mob, 2026-10-15
Chris, 2026-10-15: "also most mobs dont seem to be cc or aoe still a thing?" Until now only the Adit's seven casters and the elites did
more than swing. Now every ordinary (non-elite) camp mob in all six zones has one signature move: a short cast with its bar on the
nameplate and the target frame, stopped by any interrupt, stun or fear like any mob's cast (`MobCasts` in EncounterEnemy.Casts.cs).
Elites keep their own moves (EliteMoves.cs). Status: written and compiled 2026-10-15; the Unity tests and a look in Chris's build still to do.

What the 54 new moves are: 32 holds (root, stun, disorient or fear, on its foe or on everyone a few metres round its foe), 10 area
hits round the mob, 12 hits on its foe; 12 of those 22 throw you back.

Rules:
- **Numbers kept to trash:** a cast of 0.7 to 2 s, every 12 to 16 s, the first 4 to 6 s into the fight; holds 1.5 to 3 s; damage at
  most 2.5 swings (the sapper's Short Fuse, 3, stays the top). Step out of its reach before it lands and it misses.
- **Beasts and briars don't cast:** their move is a wind-up of under a second (0.7 to 1 s), but it shows and is stopped the same way.
- **The party's diminishing returns (new):** the same hold on the same one of the party within 18 s of the last that took lasts half as
  long, a third time "Immune" (section 1's rule for mobs, turned round; `MobCasts.Diminished`). A pull of three overseers is a stun, a
  short stun and an Immune, not a stun-lock. The netters' Weighted Net counts too; bosses' holds do not. Holds from trash don't break on damage (short, like the stuns in section 1).
- **Knockback for trash (new):** `MobCast.knock`, the metres a hit or an area hit throws whoever it lands on, through the elites' own
  knockback (`EncounterEnemy.Knock`: only ever onto walkable ground).
- **An area hit waits for its foe to be in it** (2026-10-15): it used to go off wherever its foe stood (a sapper could light its fuse at a foe 20 m off).
- **No slow on the party** exists (Slowed is a mob's state only), so the wolves' bite is a short root, not a slow. No WoW names
  (no Hamstring, no Gore), and no machines.
- **Grey Lull** (the grey whisperers): ADIT_REWORK.md section 5 asked a 4 s stun on the nearest one not its target; it is 3 s on its
  foe (trash's cap, and nothing aims at "someone else" yet). Slag Splash and the scribes' Cinder Bolt are as section 5 has them.
- **To watch:** Mira's Hush (every 15 s) and the sims' kicks (every 12 s) take the first cast they see, so they now go on trash moves too,
  and a mender's heal may get through more often. The group runs will show whether the Adit's pulls need easing.

**People with weapons: the Sandthrone company at work in the Adit, its deserters and riders outside, the Concord's wardens**

| Mob (zone) | Move | Kind | Numbers | Label |
|---|---|---|---|---|
| Sandthrone overseer (Adit) | Whip-Crack | hold | stun 1.5 s on its foe, reach 6 m; cast 1.2 s, every 14 s, first 5 s in | GAME-ONLY |
| Sandthrone digger (Adit) | Pick Sweep | area (blast) | 2 swings to all within 3 m; cast 1.5 s, every 14 s, first 6 s in | GAME-ONLY |
| Sandthrone geode-carrier (Adit) | Geode Toss | hit (bolt) + knock | 2 swings to its foe, reach 12 m, thrown back 2.5 m; cast 1.4 s, every 15 s, first 5 s in | GAME-ONLY |
| Sandthrone watchman (Adit) | Lamp-Flash | hold | disorient 2 s on its foe, reach 10 m; cast 1.4 s, every 15 s, first 5 s in | GAME-ONLY |
| Sandthrone platform guard (Adit) | Haft-Shove | hit (bolt) + knock | 1.6 swings to its foe, reach 4 m, thrown back 3 m; cast 1.2 s, every 13 s, first 5 s in | GAME-ONLY |
| Sandthrone docker (Adit) | Bale-Hook | hold | root 2 s on its foe, reach 6 m; cast 1.3 s, every 14 s, first 5 s in | GAME-ONLY |
| Sandthrone geode-cutter (Adit) | Shard Spray | area (blast) | 2 swings to all within 3.5 m; cast 1.5 s, every 15 s, first 6 s in | GAME-ONLY |
| Sandthrone sorter (Adit) | Cull-Stone | hit (bolt) | 1.8 swings to its foe, reach 18 m; cast 1.5 s, every 12 s, first 4 s in | GAME-ONLY |
| Sandthrone smelter (Adit) | Slag Splash | area (blast) | 2.5 swings to all within 4 m; cast 2 s, every 15 s, first 5 s in | GAME-ONLY |
| Sandthrone bargeman (Adit) | Barge-Pole Sweep | area (blast) | 1.6 swings to all within 3.5 m, thrown back 2.5 m; cast 1.6 s, every 16 s, first 6 s in | GAME-ONLY |
| Sandthrone outrider (Khaven) | Pommel Strike | hold | stun 1.5 s on its foe, reach 4 m; cast 1.2 s, every 15 s, first 5 s in | GAME-ONLY |
| Sandthrone deserter (Khaven, Oakhaven) | Dirty Kick | hold | disorient 1.5 s on its foe, reach 4 m; cast 1 s, every 15 s, first 6 s in | GAME-ONLY |
| Sandthrone salt-raider (Ash Rim) | Fistful of Salt | hold | disorient 2 s on all within 2.5 m of its foe, reach 6 m; cast 1.3 s, every 15 s, first 5 s in | GAME-ONLY |
| Sandthrone toll-guard (Peaks) | Halberd Sweep | area (blast) | 1.8 swings to all within 3.5 m, thrown back 2 m; cast 1.5 s, every 15 s, first 6 s in | GAME-ONLY |
| Hollow forager (Oakhaven) | Sling Stone | hit (bolt) | 1.6 swings to its foe, reach 18 m; cast 1.5 s, every 14 s, first 4 s in | GAME-ONLY |
| Concord warden (Oakhaven) | Warden's Manacle | hold | root 2 s on its foe, reach 5 m; cast 1.5 s, every 15 s, first 5 s in | GAME-ONLY |
| Grey-robed false pilgrim (Peaks) | Sleeve-Knife | hit (bolt) | 2.2 swings to its foe, reach 4 m; cast 1 s, every 14 s, first 4 s in | GAME-ONLY |

**The Ash cult**

| Mob (zone) | Move | Kind | Numbers | Label |
|---|---|---|---|---|
| Ash zealot (Adit) | Fervent Cry | hold | fear 2 s on all within 5 m of its foe, reach 8 m; cast 1.8 s, every 16 s, first 6 s in | GAME-ONLY |
| Ash scribe (Adit) | Cinder Bolt | hit (bolt) | 2.2 swings to its foe, reach 25 m; cast 2 s, every 12 s, first 4 s in | GAME-ONLY |
| Bone-masked cultist (Ash Rim) | Ash Cloud | hold | disorient 2 s on all within 4 m of its foe, reach 10 m; cast 1.8 s, every 16 s, first 5 s in | GAME-ONLY |

**The risen dead**

| Mob (zone) | Move | Kind | Numbers | Label |
|---|---|---|---|---|
| Ash-bound dead (Adit) | Grave-Grip | hold | root 2 s on its foe, reach 3.5 m; cast 1 s, every 14 s, first 4 s in | GAME-ONLY |
| Drowned dead (Khaven) | Black Water | area (blast) | 1.8 swings to all within 3.5 m; cast 1.5 s, every 15 s, first 5 s in | GAME-ONLY |

**The grey, the Hollow, the Pale and the Wasting's other things**

| Mob (zone) | Move | Kind | Numbers | Label |
|---|---|---|---|---|
| Greyed crawler (Adit) | Grey Clutch | hold | root 2 s on its foe, reach 4 m; cast 0.8 s, every 15 s, first 5 s in | GAME-ONLY |
| Grey whisperer (Adit) | Grey Lull | hold | stun 3 s on its foe, reach 15 m; cast 2 s, every 16 s, first 6 s in | GAME-ONLY |
| Pale shadow (Ash Rim, Peaks, Verdant) | Pale Dread | hold | fear 2 s on all within 3 m of its foe, reach 10 m; cast 1.6 s, every 16 s, first 5 s in | GAME-ONLY |
| Pale watcher (Peaks) | Unblinking Gaze | hold | stun 2 s on its foe, reach 15 m; cast 1.8 s, every 16 s, first 5 s in | GAME-ONLY |
| The Umbra Watcher (Peaks) | Umbra-Fall | hold | disorient 2.5 s on all within 4 m of its foe, reach 12 m; cast 2 s, every 15 s, first 5 s in | GAME-ONLY |
| Weave-Eater (Ash Rim) | Thread-Snare | hold | root 2 s on all within 3 m of its foe, reach 8 m; cast 1.2 s, every 15 s, first 5 s in | GAME-ONLY |
| Mist-walker (Verdant) | Palemist Breath | hold | disorient 2 s on all within 4 m of its foe, reach 8 m; cast 1.6 s, every 16 s, first 5 s in | GAME-ONLY |
| Void-Toucher (Verdant) | Void Pulse | area (blast) | 2.2 swings to all within 4 m; cast 1.8 s, every 15 s, first 5 s in | GAME-ONLY |
| Withered Keeper (Verdant) | Grey Roots | hold | root 2.5 s on all within 3 m of its foe, reach 10 m; cast 1.6 s, every 16 s, first 5 s in | GAME-ONLY |
| Creeping briar (Verdant) | Thorn Burst | area (blast) | 1.8 swings to all within 3.5 m; cast 0.8 s, every 14 s, first 4 s in | GAME-ONLY |

**Beasts: a wind-up of under a second, not a spell**

| Mob (zone) | Move | Kind | Numbers | Label |
|---|---|---|---|---|
| Grey wolf (Khaven, Oakhaven) | Heel-Snap | hold | root 1.5 s on its foe, reach 3.5 m; cast 0.7 s, every 15 s, first 6 s in | GAME-ONLY |
| Mountain wolf (Peaks) | Heel-Snap | hold | root 2 s on its foe, reach 3.5 m; cast 0.7 s, every 15 s, first 5 s in | GAME-ONLY |
| Ash hound (Ash Rim) | Ash-Shake | hold | disorient 1.5 s on all within 3 m of its foe, reach 4 m; cast 0.8 s, every 15 s, first 5 s in | GAME-ONLY |
| Company pit-hound (Adit) | Hold Fast | hold | root 2 s on its foe, reach 3.5 m; cast 0.7 s, every 14 s, first 4 s in | GAME-ONLY |
| Old Cinder-Jaw (Ash Rim) | Jaw-Lock | hold | stun 1.5 s on its foe, reach 3.5 m; cast 0.9 s, every 14 s, first 5 s in | GAME-ONLY |
| The Hush-Mother (Khaven) | Mother's Howl | hold | fear 2 s on all within 4 m of its foe, reach 6 m; cast 1 s, every 16 s, first 5 s in | GAME-ONLY |
| Wild boar (Oakhaven) | Tusk-Toss | hit (bolt) + knock | 1.5 swings to its foe, reach 3.5 m, thrown back 2.5 m; cast 0.8 s, every 14 s, first 6 s in | GAME-ONLY |
| Carrion boar (Khaven) | Tusk-Toss | hit (bolt) + knock | 1.8 swings to its foe, reach 3.5 m, thrown back 2.5 m; cast 0.8 s, every 14 s, first 5 s in | GAME-ONLY |
| Mire boar (Khaven) | Mud-Splash | hold | disorient 1.5 s on all within 3 m of its foe, reach 4 m; cast 0.8 s, every 15 s, first 5 s in | GAME-ONLY |
| Rockhide boar (Peaks) | Rockhide Ram | hit (bolt) + knock | 2 swings to its foe, reach 4 m, thrown back 3.5 m; cast 0.9 s, every 14 s, first 5 s in | GAME-ONLY |
| Moss-backed boar (Verdant) | Tusk-Toss | hit (bolt) + knock | 2 swings to its foe, reach 3.5 m, thrown back 3 m; cast 0.8 s, every 14 s, first 5 s in | GAME-ONLY |
| Old Brine-Tusk (Verdant) | Brine Charge | hit (bolt) + knock | 2.4 swings to its foe, reach 4 m, thrown back 3.5 m; cast 0.9 s, every 13 s, first 5 s in | GAME-ONLY |
| Brown bear (Oakhaven) | Paw-Cuff | hold | stun 1.5 s on its foe, reach 4 m; cast 0.9 s, every 15 s, first 6 s in | GAME-ONLY |
| Old Hazelmaw (Oakhaven) | Paw-Cuff | hold | stun 2 s on its foe, reach 4 m; cast 0.9 s, every 15 s, first 5 s in | GAME-ONLY |
| Mountain bear (Peaks) | Paw-Cuff | hold | stun 2 s on its foe, reach 4 m; cast 0.9 s, every 15 s, first 5 s in | GAME-ONLY |
| Greyed bear (Adit) | Grey Maul | area (blast) | 2.2 swings to all within 3.5 m, thrown back 2.5 m; cast 1 s, every 15 s, first 5 s in | GAME-ONLY |
| Red hart (Oakhaven) | Antler Toss | hit (bolt) + knock | 1.6 swings to its foe, reach 3.5 m, thrown back 3 m; cast 0.8 s, every 15 s, first 5 s in | GAME-ONLY |
| Moss-antler stag (Verdant) | Antler Sweep | area (blast) | 1.8 swings to all within 3 m, thrown back 2.5 m; cast 1 s, every 15 s, first 5 s in | GAME-ONLY |
| Carr spider (Oakhaven) | Web-Spit | hold | root 2 s on its foe, reach 12 m; cast 0.9 s, every 15 s, first 5 s in | GAME-ONLY |
| Charnel spider (Khaven) | Web-Spit | hold | root 2 s on its foe, reach 12 m; cast 0.9 s, every 15 s, first 5 s in | GAME-ONLY |
| Canopy spider (Verdant) | Web-Drop | hold | root 2 s on all within 3 m of its foe, reach 12 m; cast 1 s, every 16 s, first 5 s in | GAME-ONLY |
| Basalt spider (Verdant) | Grit-Spit | hold | disorient 1.5 s on its foe, reach 10 m; cast 0.9 s, every 15 s, first 5 s in | GAME-ONLY |
