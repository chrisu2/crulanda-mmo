# Classes: what exists and what is left

Written 2026-10-09 from the talent files (`EncounterContent/Talents/*.json`), the kits (`Scripts/Encounter/*Kit.cs`), `SimAdventurers.cs`
and the CHANGELOG. CLASS_BUILD_MATRIX.md is the design of every candidate class; this is the state of the built ones. Update it when a
class moves.

## The seven built classes

All seven are playable from the pause menu (a separate character each), all are played by the sims (`SimAdventurers.ClassIds`; sims 41
and up are Rogues and Archivists), all have a talent tree (the Paladin four paths since 2026-10-09, TALENT_DEPTH.md), an AI rotation, gear rules, one crowd control and one interrupt (CC_DESIGN.md),
and icons for everything on the bar (IconCoverageTests).

| Class | Resource | Builds (talent branches) | Talent ranks built per branch | Crowd control / interrupt | Figure |
|---|---|---|---|---|---|
| Warrior | Vigor | Tank, DPS, Support | 23 of 34 designed (rows 0-3 work; rows 4-6 written, not built) | Shout (fear 6 s) / Shield Bash (silence) | own |
| Druid | Wildstores | Barkhide tank, Thornclaw melee, Rootmend healer, Thornsong ranged | 25 of 35 designed (rows 0-3 work; rows 4-6 written, not built) | Sleep of the Wood (an eleventh slot) / Thornsong's interrupt | own; four forms |
| Paladin | Mana + Conviction | Oathguard tank, Judicator melee, Sanctuary healer, **Vanguard support** (2026-10-09) | 19 of 19 in the first three (rows 0-2); the Vanguard 23 of 32 (rows 0-3 built, 4-6 written) | Rebuke (stun, works on bosses) / Censure (silence) | own |
| Ranger | Focus | Marksman ranged, Beastbond pet, Pathfinder control | 17-19 of 17-19 (rows 0-2; nothing deeper written) | Snare Trap (hold 20 s) / Pin (silence) | own; the wolf |
| Mage | Mana + Heat | Combustion ranged, Heatweaver control, Spellbinder support | 17-19 of 17-19 (rows 0-2; nothing deeper written) | Ash Hex (held 25 s) / Quench (silence) | own; the staff |
| Rogue | Focus + combo points | Thief's Grace stealth, Shardwork poisons, Locksmith | 16-17 of 16-17 (rows 0-3; nothing deeper written) | Sap, Gouge, Blind / Kick; Vanish; pick-lock | borrows the Ranger's |
| Archivist | Mana | the Silent Vow, the Lull, the Archive | 16 of 16 (rows 0-3; nothing deeper written) | Lull (three sleep), Echo Bind / Hush; songs; echo-jar | borrows the Mage's |

**At the cap of 15 every class is complete:** talent points are your level plus one (16 at 15), and every branch has at least 16 ranks
built, so nothing a level-15 character can reach is missing.

**For the cap of 30 (Phase 9, levels 16-30) every class is short:** 31 points need about 30 ranks a branch. The Warrior and the Druid
have theirs designed (rows 4-6 in their files, marked `impl: false`); the Paladin, Ranger, Mage, Rogue and Archivist need rows 3-6 (or
4-6) designed first, then built. That is the "talent rows 4-6" item in the handoff. It is not needed until the cap rises.

## Left to do on the built classes, by size

1. **Deeper talents for 16-30** (above): the Warrior's and Druid's rows 4-6 to build (15 and 20 nodes); the other five to design and
   build. Several rounds; only when Phase 9's zones bring the levels.
2. **Own figures for the Rogue and the Archivist** (they borrow the Ranger's and the Mage's): a figure spec each in ActorVisual.Model.cs,
   a wardrobe capture for Chris. Small.
3. **Mira's Hush** (the companion's interrupt, CC_DESIGN C2): small.
4. **Turn the Unmade** (a Paladin fear against the Wasting's things, noted with C2): small.

## Planned, not started (ROADMAP Phase 9.2, from the novels; PROVISIONAL)

- **The Tinker** (Klyther Forgeheart: drones and scorpions, an engineer).
- **The Knight** (Talira Frostveil, Kaelith Dawnstrike: swordmaster, Stormlight); overlaps the Paladin's Oathguard, to resolve.
- **The Salt-Mender** (the salt-magic of the West).
- The Rogue and the Archivist were on that list and are done.

CLASS_BUILD_MATRIX.md holds 20 candidates in all (Cleric, Shaman, Necromancer, Warlock, Bard, Monk, Seer, Engineer and more); the
brief's target is up to 15 classes, and which merge or drop is undecided. Chris picks the next class when Phase 8 closes.
