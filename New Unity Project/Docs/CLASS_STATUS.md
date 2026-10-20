# Classes: what exists and what is left

Written 2026-10-09 from the talent files (`EncounterContent/Talents/*.json`), the kits (`Scripts/Encounter/*Kit.cs`), `SimAdventurers.cs`
and the CHANGELOG. CLASS_BUILD_MATRIX.md is the design of every candidate class; this is the state of the built ones. Update it when a
class moves.

## The built classes (eight since 2026-10-11: the Salt-Mender)

The first seven are playable from the pause menu (the Salt-Mender too) (a separate character each), all are played by the sims (`SimAdventurers.ClassIds`; sims 41
and up are Rogues and Archivists), all have a talent tree (the Paladin four paths since 2026-10-09, TALENT_DEPTH.md), an AI rotation, gear rules, one crowd control and one interrupt (CC_DESIGN.md),
and icons for everything on the bar (IconCoverageTests).

| Class | Resource | Builds (talent branches) | Talent ranks built per branch | Crowd control / interrupt | Figure |
|---|---|---|---|---|---|
| Warrior | Vigor | Tank, DPS, Support, **Jailer control** (2026-10-10) | 33 of the first three (rows 0-3 work; rows 4-6 written, not built); the Jailer 10 of 15 (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): 23 a branch, the Jailer 26: already 21+ (nothing new)** | Shout (fear 6 s) / Shield Bash (silence) / Hock Cut (slow) / Weighted Net (root) / Shackle Cast (stun) | own |
| Druid | Wildstores | Barkhide tank, Thornclaw melee, Rootmend healer, Thornsong ranged | 25 of 35 designed (rows 0-3 work; rows 4-6 written, not built). **At the cap of 20 (2026-10-16, 21 points): 25 a branch: already 21+** | Sleep of the Wood (an eleventh slot) / Thornsong's interrupt | own; four forms |
| Paladin | Mana + Conviction | Oathguard tank, Judicator melee, Sanctuary healer, **Vanguard support** (2026-10-09) | 19 of 19 in the first three (rows 0-2); the Vanguard 23 of 32 (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): Oathguard, Judicator, Sanctuary 22 (Iron Oath, Sentence Passed, Shared Mercy), the Vanguard 23** | Rebuke (stun, works on bosses) / Censure (silence) | own |
| Ranger | Focus | Marksman ranged, Beastbond pet, Pathfinder control, **Skirmisher melee** (2026-10-11) | 17-19 of 17-19 (rows 0-2; nothing deeper written); the Skirmisher 16 of 21 ranks (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): Marksman 22, Beastbond 21, Pathfinder 22, the Skirmisher 22 (Catch Your Breath built in tier 3)** | Snare Trap (hold 20 s) / Pin (silence); Feint (the next blow misses) | own; the wolf |
| Mage | Mana + Heat | Combustion ranged, Heatweaver control, Spellbinder support, **Cinderwright pets** (2026-10-11) | 17-19 of 17-19 (rows 0-2; nothing deeper written); the Cinderwright 16 of 21 ranks (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): Combustion 21, Heatweaver 22, Spellbinder 22, the Bonecaller 22 (Ossuary Coat built in tier 3)** | Ash Hex (held 25 s) / Quench (silence) | own; the staff; the ember hound |
| Rogue | Focus + combo points | Thief's Grace stealth, Shardwork poisons, Locksmith, **Swashbuckler support** (2026-10-11) | 16-17 of 16-17 (rows 0-3; nothing deeper written); the Swashbuckler 16 of 21 ranks (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): Thief's Grace, Shardwork, Locksmith 21, the Swashbuckler 22 (Crowd Pleaser built in tier 3)** | Sap, Gouge, Blind / Kick; Vanish; pick-lock; Opening Cut (Exposed), Tumble; Throw Knife (the pull, 2026-10-11) | borrows the Ranger's; a pair of Lowtown dirks, one in each hand (2026-10-11) |
| Salt-Mender (2026-10-11, the first class from the canon; CLASS_BUILD_MATRIX.md) | Salt | Salt-Speaker control, Mason tank, Sifter ranged, **Mender healer** (2026-10-12, CANON-EXPANDED) | 32 of 32 (rows 0-2; the Mender rows 0-3, 25 ranks). **At the cap of 20 (2026-10-16, 21 points): Salt-Speaker 22 (Ready Salt), Mason and Sifter 21, the Mender 25** | Binding Salt (held 8 s) / Seal the Wound (silence); Calcify (stun, a talent); Draw the Line (taunt) | its own (ActorLook.SaltMender: salt-white robe, sea-grey hood, salt pouch, salt-stone charm); six sims (sims 51-56; heal when no Druid is along) |
| Archivist | Mana | the Silent Vow, the Lull, the Archive, **the Cantor healer** (2026-10-10) | 21 of the first three (rows 0-3; nothing deeper written); the Cantor 10 of 15 (rows 0-3 built, 4-6 written). **At the cap of 20 (2026-10-16, 21 points): the Still Vow 23, the Lull 21, the Unwriting 23, the Cantor 26** | Lull (three sleep), Echo Bind / Hush; songs; echo-jar; Mending Phrase, the Song of Mending, Chorus of the Weave | borrows the Mage's |

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
3. ~~Mira's Hush~~: BUILT with C5 (HealerCompanion.Hush; its test ArchivistLoopTests, PlayMode); listed here as left by mistake until 2026-10-11.
4. **Turn the Unmade** (a Paladin fear against the Wasting's things, noted with C2): small.

## Planned, not started (ROADMAP Phase 9.2, from the novels; PROVISIONAL)

- ~~The Tinker~~ (Klyther Forgeheart: drones and scorpions): **set aside 2026-10-11, machinery** (no machines). Klyther's book-3 craft
  (salt-stone, sap-mortar) is the Salt-Mender's Mason path (CLASS_BUILD_MATRIX.md, "Phase 9.2 and 9.3 from the canon").
- **The Knight** (Talira Frostveil, Kaelith Dawnstrike: swordmaster, Stormlight); overlaps the Paladin's Oathguard, to resolve.
- **The Salt-Mender** (the salt-magic of the West): designed 2026-10-11 from Book 1's Salt-Speaker and book 3's Mender-Craft
  (CLASS_BUILD_MATRIX.md): Salt-Speaker control, Mason tank, Sifter ranged. **Built
  2026-10-11** (SaltMenderKit, Talents/saltmender.json, eight workings, icons; SaltMenderRulesTests, SaltMenderLoopTests). **2026-10-12:
  the Mender** (Chris: "a bard like healer" "with some shamanistic"; CANON-EXPANDED, no salt-magic heals in the books): Close the Cut
  (level 7, a direct heal on the worst wound), Mender-Craft (a chant kept up: the party within 12 m mends every 2 s for Salt a verse),
  Salt Circle (a ring poured at your feet: mends and 10% less inside), Salt Cairn (stacked salt-stone: mends, sifts the Wasting's grey,
  grounds part of each blow; Call the Deep Earth, the signature); 11 talents, rows 0-3, 25 ranks; icons; its own figure; six Salt-Mender
  sims (SimRoster 56) that heal (the mend, a chant, a cairn) when no Druid is along. TESTED (lane A), not seen by Chris. Left: rows 3-6 of
  the first three paths, a Frost-Edge; no enemy yet lays a grey on the party for the cairn to sift.
- The Rogue and the Archivist were on that list and are done.

CLASS_BUILD_MATRIX.md holds 20 candidates in all (Cleric, Shaman, Necromancer, Warlock, Bard, Monk, Seer, Engineer and more); the
brief's target is up to 15 classes, and which merge or drop is undecided. Chris picks the next class when Phase 8 closes.

## Mage — Bonecaller (replaces Cinderwright) — GAME-ONLY
- Why this path: Cinderwright was the pet path, so the minion code carries over. Spellbinder is the Mage's only support path, so it stays.
- Label: GAME-ONLY. D:\code\crulanda has no necromancy; the word only appears as an insult. This is not the Wasting.
- Talents: 12 nodes. Tiers 1-3 are live (8 ids, at least 16 ranks reachable). Tiers 4-6 are written but impl:false.
- Abilities: Raise Thrall (a skeleton pet), Marrow Drain (damage over time; each tick heals you and the thrall 50%), Bone Spikes (an area hit around the thrall that slows foes and costs the thrall 10% health).
- Sim rotation: SimCompanion.PathRotation; odd mage variants play Bonecaller.
- Saves: TalentMigration refunds retired cw- picks. Chris's saves were backed up in save-backups\20261012-1040-before-path-rework.
- Status: UNVERIFIED (needs Chris).

## Archivist — the Unwriting (replaces the Archive) and the Still Vow (replaces the Silent Vow)
- The Unwriting (damage over time, like a shadow priest; prefix uw-; 10 nodes, 23 ranks): Unwrite is a damage-over-time effect whose Hollow Echo ticks heal; Erasure hits an unwritten target x1.5; Spreading Grey; capstone The Unwritten.
- The Still Vow (shields/support, like Discipline; prefix vw-; 9 nodes, 20 ranks): Vow of Stillness puts a barrier on whoever is most hurt; Atonement makes Shard Note damage heal; also Hushed Pain, Second Ward, Grace, Steady Voice; capstone Vow Kept.
- New abilities: archivist.unwrite, archivist.erasure, archivist.stillness (slots 10-12, 13 actions in total).
- Sim rotation: SimCompanion.PathRotation (Unwrites, Wards). Odd archivist variants play the Unwriting; even ones play the Still Vow.
- Saves: TalentMigration refunds retired ar- and sv- picks. Chris's save (ar-resonance, sv-quick-hush) was backed up first.
- Open: the Original Song (Lull) and the old Hush talents can no longer be reached.
- Status: UNVERIFIED (needs Chris).
