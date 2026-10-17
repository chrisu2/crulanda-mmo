# Balance report: retune round 1 (2026-10-12, 18:45)

Harness: `Assets/Crulanda/Tests/PlayMode/BalanceHarness.cs`. It runs all 8 classes and every talent build at level 15 in standard gear (9 of 9 slots worn). Fights last 90 s on 3 seeds with a fixed 0.05 s step, and a full run of all 8 classes takes about 6 min.
"Before" is harness v5 with no retune (`Docs/balance_v5_before.csv`). "After" is the retuned game (`Docs/balance_v5_after.csv`).

## Harness checks this round
- **Casts are let finish.** The rotation waits while `PlayerCasting` is true.
- **Mana-aware rotation.** A costly ability waits while the pool would drop under 20%. The cheapest filler never waits. No build runs dry any more: in v4 Paladin and Rogue went OOM in about 6 s.
- **Per-ability use counts.** Seed 0 writes a `# uses` line per build to the CSV, so a kit's rotation can be read straight off it.
- **Druid.** The use counts show thornsong spamming `seedshot` (47 casts, 12 DPS) and rootmend casting cat abilities (`rake`). In the heal test, rootmend casts only `swiftroot` (6 casts, 3.3 HPS). The harness does take the form (`druid.<branch>`), so either the form isn't sticking after `Kit.ClearTransient` or the Thornsong and Rootmend kits are really this weak. **Not retuned yet; I need to look into the kit first.**
- **Archivist splash.** `echo_jar` is the class's only splash. It has a long cooldown and goes off once a fight, so 1 of 3 dummies hit is the kit's real behaviour, not a reach problem. It has no AoE build.
- **Mage combustion** used its whole rotation (ember_bolt, scorch, smoulder, flare, cinder_field). It really was low, so it was retuned up.

## Retune
`Scripts/Encounter/EncounterSession.RoleScale.cs` holds `BuildDamageScale`. It scales the player's own blows by the build they have spent the most points in, applied in `EncounterEnemy.Receive`.
- Pets are not scaled. That is why the Ranger factors are steeper: the pet's share stays in.
- Healing, crowd control and utility are not touched.

| build | scale |
|---|---|
| ranger/marksman | ×0.67 |
| ranger/skirmisher | ×0.80 |
| druid/thornclaw | ×0.80 |
| paladin/judicator | ×0.92 |
| mage/combustion | ×1.65 |
| archivist/the-unwriting | ×2.4 |
| ranger/pathfinder | ×0.43 (cap) |
| rogue/thiefs-grace | ×0.82 (cap) |
| rogue/swashbuckler | ×0.72 (cap) |
| paladin/vanguard | ×0.92 (cap) |
| warrior/support | ×0.93 (cap) |
| warrior/jailer | ×0.93 (cap) |
| paladin/sanctuary | ×0.62 (cap) |
| saltmender/mender | ×0.85 (cap) |

**Role caps** (note 177; Chris 18:22: "control classes should not be top tier dps for sure" / "or healers"). Measured on single-target DPS against the DPS-role median:
- healer at most 50%
- control and support at most 75%
- 3 points of slack, for "about"

`BalanceHarness.CapViolations` fails the harness run when a build breaks its cap. It caught pathfinder at 84% on the first retune pass, and the scale was lowered after that.

## Before and after
DPS-role median (single target): before 68.7, after 69.5.

| class | build | role | single DPS before | after | % of DPS median before | after | cap | 3-target DPS after (foes hit) | OOM s after | status |
|---|---|---|---|---|---|---|---|---|---|---|
| mage | heatweaver | control | 48.0 | 48.0 | 70% | 69% | 75% | 48.5 (3) | - | ok |
| warrior | jailer | control | 51.7 | 47.9 | 75% | 69% | 75% | 47.9 (2) | - | ok |
| rogue | thiefs-grace | control | 58.1 | 47.5 | 85% | 68% | 75% | 47.3 (1) | - | ok |
| ranger | pathfinder | control | 81.6 | 47.0 | 119% | 68% | 75% | 28.2 (3) | - | ok |
| saltmender | salt-speaker | control | 39.5 | 39.5 | 57% | 57% | 75% | 40.9 (3) | 28.5 | ok |
| archivist | the-lull | control | 22.5 | 22.5 | 33% | 32% | 75% | 22.7 (1) | - | ok |
| paladin | judicator | dps | 80.1 | 73.7 | 117% | 106% | - | 71.7 (3) | - | ok |
| ranger | marksman | dps | 99.5 | 73.6 | 145% | 106% | - | 61.0 (3) | - | ok |
| ranger | skirmisher | dps | 86.1 | 73.2 | 125% | 105% | - | 50.9 (3) | - | ok |
| druid | thornclaw | dps | 91.8 | 73.2 | 134% | 105% | - | 72.2 (1) | - | ok |
| warrior | dps | dps | 68.7 | 70.2 | 100% | 101% | - | 69.2 (3) | - | ok |
| rogue | shardwork | dps | 68.8 | 68.8 | 100% | 99% | - | 66.6 (1) | - | ok |
| saltmender | sifter | dps | 65.7 | 65.7 | 96% | 95% | - | 65.7 (3) | - | ok |
| mage | combustion | dps | 38.3 | 63.3 | 56% | 91% | - | 71.0 (3) | - | ok |
| archivist | the-unwriting | dps | 26.1 | 62.9 | 38% | 91% | - | 62.9 (1) | - | ok |
| druid | thornsong | dps | 12.2 | 12.2 | 18% | 18% | - | 12.4 (1) | - | **still off** |
| paladin | sanctuary | healer | 52.2 | 32.5 | 76% | 47% | 50% | 31.9 (3) | - | ok |
| saltmender | mender | healer | 38.3 | 32.3 | 56% | 46% | 50% | 33.6 (3) | 31.7 | ok |
| archivist | the-cantor | healer | 22.7 | 22.7 | 33% | 33% | 50% | 22.7 (1) | - | ok |
| druid | rootmend | healer | 21.8 | 21.8 | 32% | 31% | 50% | 22.0 (1) | - | ok |
| ranger | beastbond | pet | 96.7 | 96.7 | 141% | 139% | - | 66.1 (3) | - | - |
| mage | bonecaller | pet | 39.1 | 39.1 | 57% | 56% | - | 39.3 (3) | - | - |
| paladin | vanguard | support | 52.5 | 48.3 | 76% | 70% | 75% | 48.5 (3) | - | ok |
| rogue | swashbuckler | support | 67.1 | 48.3 | 98% | 70% | 75% | 47.9 (1) | - | ok |
| warrior | support | support | 51.7 | 47.7 | 75% | 69% | 75% | 47.1 (1) | - | ok |
| mage | spellbinder | support | 40.0 | 40.0 | 58% | 58% | 75% | 40.4 (3) | - | ok |
| archivist | the-still-vow | support | 15.6 | 15.6 | 23% | 22% | 75% | 15.6 (1) | - | ok |
| paladin | oathguard | tank | 52.5 | 52.5 | 76% | 76% | - | 52.6 (3) | - | - |
| warrior | tank | tank | 48.8 | 48.1 | 71% | 69% | - | 48.6 (1) | - | - |
| druid | barkhide | tank | 42.1 | 42.1 | 61% | 61% | - | 45.2 (3) | - | - |
| saltmender | mason | tank | 41.2 | 41.2 | 60% | 59% | - | 40.9 (3) | 27.0 | - |
| rogue | locksmith | utility | 56.3 | 56.3 | 82% | 81% | - | 56.2 (1) | - | - |

### Healer HPS (heal test: player hurt 4%/s, no foe in reach)

| class | build | HPS before | after |
|---|---|---|---|
| archivist | the-cantor | 24.6 | 24.6 |
| druid | rootmend | 3.3 | 3.3 |
| paladin | sanctuary | 25.3 | 25.3 |
| saltmender | mender | 23.4 | 23.4 |
## Still open
- **Druid thornsong** (18% of the DPS median) and **rootmend HPS** (3.3): see the Druid check above. They need a look at the kit or form before any number changes.
- **Ranger beastbond** (pet, 139%). Its damage is almost all the pet, which `BuildDamageScale` doesn't touch. It is the only pet build besides Bonecaller (57%), so the pet role is far apart. Next step is a pet damage scale.
- **Archivist builds and Mage spellbinder** are well under their caps (Archivist 22-33%, spellbinder 59%). Under a cap is allowed. Whether they're viable depends on the value of their crowd control and support, which this harness doesn't measure.
- **Tanks** have no cap. Paladin oathguard is at 77% of the DPS median.

---

## Round 2 (2026-10-12, 20:00): harness v6, pet damage scale, party heal test
Raw CSVs: `Docs/balance_v6.csv` and `Docs/party_heal_v1.csv`. The harness passes 2/2, including the role-cap check.

### Harness fixes
- **Druid forms.** The form shift was failing on the GCD, which left the previous build's form on: rootmend was fighting in Thornclaw and thornsong in Rootmend. The harness now retries the shift for up to 6 s and logs `# form ...: Active`. All four Druid builds now hold their form on every fight.
- **Fillers taken in turn.** Equal-ranked abilities with no cooldown now alternate, which is Thornsong's Seedshot and Thornbolt loop. Seedling can now be used every time it's ready (it builds Rootmend's Sap).
- **Lock log.** At the start of each fight, `# locks` lists every locked ability and the reason.

### Pet damage scale
| build | before | after |
|---|---|---|
| ranger/beastbond | 139% | 103% |
| mage/bonecaller | 57% | 93% |
| ranger/pathfinder (control) | 82% | 66% (cap 75%) |

The scales are player and pet ×0.75 for beastbond, ×1.6 for bonecaller, and pet ×0.5 for pathfinder.

### Druid, with forms now held (single target, % of the 70.6 DPS median)
- **Thornsong:** 12.2 → 31.4 DPS (44%). Still low: a real retune target.
- **Rootmend:** 0 DPS in its own form, which is correct for a healer. Solo heal test: 4.3 HPS.
- **Party test:** 17 HPS at 5-9% overheal. Low next to the other healers.

### Open: the Archivist moved with the harness change
From v5 to v6 the Archivist builds moved sharply, though their kit was not changed:

| build | v5 | v6 |
|---|---|---|
| the-unwriting | 92% | 137% |
| the-lull | 33% | 6% |
| the-cantor | 33% | 8% |
| the-still-vow | 23% | 4% |

The no-cooldown filler alternation likely now casts Lull and Hush (sleep and silence) on the dummies. That stops their own damage. **Until that's checked, the v6 Archivist numbers are not trusted, and no Archivist retune is made.**

### Party heal test v1 (note 178)
90 s fights on 3 seeds. Every member loses 3% per second, and one member chosen by the seed loses 30% every 6 s.
**Only 2 members (you and Mira):** none of the 3 sims joined, because `Invite` refused or found no sim nearby. Sims in the party are the next fix.

| healer | HPS | overheal | to others | mana used / left |
|---|---|---|---|---|
| paladin sanctuary | 41.8 | 32% | 41% | 920 / 99 |
| saltmender mender | 38.8 | 21% | 42% | 1080 / 100 |
| archivist the-cantor | 38.5 | 61% | 38% | 961 / 99 |
| druid rootmend | 17.2 | 7% | 40% | 0 (it runs on Sap) / - |

Nobody dropped under 25% in any fight, so the damage is too light to tell the healers apart. v2 will add the sims and heavier spikes. Cleric and Shaman join it when they're built (`Docs/CLASS_CLERIC_SHAMAN.md`).


---

## Round 3 (2026-10-12, 20:09 run, read 20:48): harness v7, Druid retune, heavier party test
Raw: `Docs/balance_v7.csv`, `Docs/party_heal_v2.csv`.

### Druid retune (`BuildDamageScale` thornsong ×2.1; `BuildHealScale` rootmend ×2.2, applied in DruidKit.HealActor)
| build | before | after |
|---|---|---|
| thornsong DPS | 31.4 (44% of median) | 65.6 (96%) |
| rootmend solo HPS | 4.3 | 7.3 |
| rootmend party HPS | 17.2 | 32.6-37.3 (heavier damage) |

### Party heal test v2 (5%/s to everyone, plus a 45% spike every 4 s; 90 s; 3 seeds)
**Still only you and Mira.** The test scene has **0 sim figures**, so no sims could be invited. Next fix: spawn three sims for the test directly.

| healer | HPS | overheal | to others | mana used / left | member-seconds under 25% |
|---|---|---|---|---|---|
| archivist the-cantor | 67.9-77.5 | 20-29% | 36-41% | 843-1020 / 97-100 | 0 |
| paladin sanctuary | 53.8-55.4 | 1-2% | 38-44% | 842-960 / 98-100 | 43-45 |
| saltmender mender | 48.5 | 1% | 32-39% | 1080 / 100 | 35-47 |
| druid rootmend | 32.6-37.3 | 2-3% | 37-46% | 0 (Sap) | 60-93 |

- Under this damage, the-cantor is the only healer that keeps everyone above 25%.
- Rootmend is still the weakest: 60-93 member-seconds under 25%.
- Paladin and Salt-Mender sit at about 1% overheal, so they are throughput-capped: they heal all they can and still fall behind. Their spare mana says the cap comes from their cooldowns and GCD, not mana.

### Harness regression found in v7
Taking equal-ranked damage fillers in turn helped Thornsong but cost the casters most of their damage (v6 → v7, % of median):

| build | v6 | v7 |
|---|---|---|
| mage combustion | 91% | 64% |
| mage heatweaver | 69% | 56% |
| mage spellbinder | 59% | 44% |
| mage bonecaller | 93% | 75% |
| archivist builds | (various) | 1-29% |

**The fix:** turn-taking is now only used for the Druid, and every other kit keeps bar order. **The v7 Mage and Archivist numbers are not valid.** Treat the v5/v6 Mage numbers as current until v8 runs.


---

## Round 4 (v8, 2026-10-12 22:19): confirm run
Raw: Docs/balance_v8.csv, Docs/party_heal_v3.csv. The harness passes 2/2, including the role caps.
- The turn-taking fix worked: the Mage and Archivist are back to their v5/v6 numbers. Combustion 92%, bonecaller 91%, heatweaver 70%, spellbinder 58%, the-unwriting 92%, the-lull 33%, the-cantor 33%, the-still-vow 23% (all of the 68.6 DPS median).
- The Druid holds: thornsong 96%, thornclaw 105%.
- Party test: still **0 sims joined**, even when they are put in the zone directly. The test scene's SimPopulation is likely null or has no sims. Next: build three SimAdventurers outright. HPS is the same as v2 (cantor 65-73, sanctuary 54-55, mender 48.5, rootmend 33-37).

## Chris, 2026-10-13 00:37 (brief, verbatim)

"yeh marksman too high. 62-72 is good range"

My reading: on the fixed dummies (level 15, no armour, no blows; 180 s fights) a DPS build should land at **62-72 single-target DPS**; the Marksman (Ranger) is over it and comes down first. Healer and control/support caps (50% / 75% of the DPS median) stand.

## Chris, 2026-10-13 (briefs, verbatim)

"you should create test dummies to test the dps and wounded test dummies for the heal test so there are no variables in the mobs"
"well i need a number for each build, each class and designate it as tank/dps/support/healer/CC?"
"why is the druid healing so low and needs to be fixed"

## Round 5 (2026-10-13, 00:44-02:30): fixed dummies, every class and build with a role, a number and a target

**Method.** The dummies are fixed (level 15, no armour, 5,000,000 health, no blows); the heal test's wounded dummy is Mira at 3,000
health losing 4% a second (120 a second, the most any healer can show). Fights are 180 s, three seeds averaged. Tables:
`tools\validation\balance_merge.py` writes `D:\crulanda-work\outputs\balance\BALANCE_ALL.md`; `tools\validation\balance_retune.py`
moves a build outside its target to the middle of it (scales in `EncounterSession.RoleScale.cs`).

**Roles and targets** (single-target DPS on the dummies):

| Role | Target (level 15) | Source |
|---|---|---|
| DPS (pet builds too) | 62-72 | Chris, 00:37 |
| CC | 45-52 | proposed, settled by Chris 02:07 |
| Support (the Rogue's utility build too) | 45-52 | proposed, settled by Chris 02:07 |
| Tank | 42-55 | proposed, settled by Chris 02:07 |
| Healer | at most 35 DPS; 40-50 HPS on the wounded dummy | proposed, settled by Chris 02:07 |

Chris, 2026-10-13 02:07 (verbatim): "the #s you proposed are good til lvl 15 and will change as we progress to 30". These are the targets
for the level cap of 15 (the dummies are level 15). They are set again as the cap rises to 30 (Phase 9).

**Functionality found by the dummies (each fixed, TESTED in the harness, not yet seen by Chris):**
- The Druid's healing (8.6 HPS): a heal or damage over time recast before its first tick restarted its clock (`PeriodicEffects.Add`).
  Seedling, recast every 1.8 s on its 2 s tick, never healed, so no Sap, so no Quickbloom or Burst Bloom. A refresh now keeps the
  schedule (EditMode test). Rootmend went to 35 HPS, then 40.6 with its heal scale. The same fix lifted the Archivist's Cantor from 25 to
  119 HPS.
- The Druid and Paladin healed only you and Mira, never the sims in your party (`HealTarget`). In the party test a party of five spent
  350-590 member-seconds low under them against 10-40 under the Cleric and Shaman. Both now heal the most hurt member of the whole party.
- The Shaman's Thunderhead never fired: the Squall charge was wiped every frame once six quiet seconds had passed. It now fades six
  seconds after the last Squall or fight.
- The harness, not the kits, sank the Cleric and Shaman: their bars lock nothing by build, so damage fights spent most casts on heals
  for nobody. Damage fights now cast no heals. Brand, Unwrite and Marrow Drain (damage over time with no cooldown) were never cast,
  starved by the filler; they are now kept up. The healers' songs and chants (Song of Mending, the Mender's chant) were never started;
  they now start once. In the heal tests heals take turns, so Quickbloom, Lay On, Long Mend and Chain Mend get cast.

**Retune applied** (scales; the build's own blows, its pet's where it has one, its own heals):
- DPS down to 62-72: Marksman .75 to .64, Skirmisher .85 to .75, Judicator .92 to .80 to .64 (Lay On no longer eats its fight),
  Thornclaw .80 to .74, Beastbond .75 to .70 (pet too), Warrior DPS 1 to .92.
- Up: Cleric Censure 1.68, Shaman Stormcaller 1.67; CC and Support into 45-52: the Lull 2.14, the Still Vow 3.11, Spellbinder 1.16,
  Salt-speaker 1.27, Gravewind 1.15, Cleric Warden 2.32; Tank Mason 1.25. Locksmith down to .87.
- Heal scale: Rootmend 2.2 to 2.8, the Cantor .16 (it healed all 120 a second), Paladin Sanctuary .83. The Archivist, Paladin and
  Salt-Mender kits now apply the heal scale at all (only the Cleric, Druid and Shaman did).

After the first retune 35 of 40 builds were in range (the run at 01:15). The table below is the confirm run.

## Chris, 2026-10-13 02:08-02:15 (briefs, verbatim)

"is there only two pet classes? give the shaman dps a pet (uncontrollable that throws out small dots and heals),"
"have the druid healer have a fairy to help heal to catch up #s"
"remember pet+class should be on par for 62-72 combined" / "since it is a dps class"
"well if not a druid fairy then something canon related for druid that has the same functionality"
"ok but the shaman should find something canon related also"
"put a few test dummies in town so the player can test their builds maybe just one for dps and one for heals. maybe near the blacksmith or training grounds/area"

Answer: two classes had pets (the Ranger's wolf for Beastbond, kept at half strength by the Pathfinder; the Mage's Bonecaller thrall).
Done (`SpiritCompanion.cs`, a spirit at the shoulder that nobody commands, coming with the build): the Stormcaller's storm spirit stings
the foes in your fight with small damage over time and mends a little (its canon root being looked up: the name is PROVISIONAL); the
Rootmend's glade-fire mends the most hurt of the party (CANON-EXPANDED: no fairies in the canon, only the phrase "fairy tale"; the
witch-fire wisp of book 1 ch. 13, a hovering orb of light bound to its maker's will, lit with the Whispering Glade's regenerative gold and
silver light, book 3 ch. 5). The meter shows each on its own row; a pet build's DPS is player and pet together, on 62-72.

## Chris, 2026-10-13 02:16-02:25 (briefs, verbatim)

"i do want some classes to specialize in AOE (like a cleave paladin, or cleave warrior) or a ranger with scatter arrows or volley but i
dont want dps vs 1 and dps vs 3 to be the same for every class ..this should be based on specializations"
"just make the heal dummy party aligned?" / "or target aligned if solo"
"what are the top 3 best healers, best 3 top dps, and top 3 best tanks! remember i dont want one class dominating all 3 positions"
"these dummies should be targetable and hitable / healable of course"

**Area by specialization** (`EncounterSession.Spread.cs`, GAME-ONLY): while the lead build is an area specialist, each of your own
blows splashes a share onto up to two more foes near the struck one (never a held foe, a hidden one, a rider or game; your pet's and
spirit's blows do not splash). Specialists, PROPOSED: Warrior DPS (Sweeping blows, .55, 5 m), Paladin Judicator (Wide judgement, .55,
5 m), Ranger Skirmisher (Scatter shot, .5, 8 m), Mage Combustion (Spreading flame, .5, 6 m), Shaman Stormcaller (Forked storm, .45, 6 m).
Every other damage build is a single-target specialist. Table targets, PROPOSED: an area build does at least 1.7 times against three what
it does against one; a single-target build at most 1.25 times.

**Found on the way:** the harness cast the once-a-fight toggles again each fight, so the Ranger's wolf was sent away in every fight
against three (the Rangers did less against three than against one), and the Bonecaller's thrall, the Song of Mending and the Mender's
chant the same. The harness now leaves a pet, song or chant that is already up.

**Top three at 01:15** (before the second retune): DPS Marksman 70.5, Skirmisher 69.9, Shardwork 69.6 (Judicator 83.6, rescaled);
healers (one target) Mender 49.6, Sanctuary 45.3, Rainspeaker 42.1 (the Cantor 105, rescaled), (party of five) Rainspeaker 102, Choir 98,
Lantern 89; tanks, on damage only, Oathguard 53.1, Warrior Tank 49.0, Mason 48.8. The Paladin sat near the top of all three: Judicator's
rescale takes it out of DPS. Tanks need their own test (dummies that strike: damage taken, self-healing, threat), still to build.

## Round 6 (2026-10-13, 12:42-13:23): the honest dummies

**Harness faults found and fixed this morning** (each made earlier numbers wrong):
- The test scene's other sentries stood beside the dummy. Every build with a spell that hits several foes (the Mage's fields, the
  Shaman's hail and wind, the Druid's brambles, the Salt-Mender's lines) had been landing it on them, so its "single-target" number was
  inflated; the area builds' new splash doubled the Warrior's. Every other mob within 30 m is now stilled and taken off, once per class.
- The dummies were picked afresh each fight (the nearest mobs); after the clearing that once chose a mob taken off the navmesh, and the
  Druid fought from off the mesh and cast nothing (Rootmend 0 HPS, Thornsong 0 DPS). One fight spot and three dummies per class run now.
- The area spread was cached on the talent point count, the same for every build, so it never switched on. Keyed on the ranks list now.
- The tank test hit for a flat 40 and kept threat across fights. It now uses a tough level-15 mob's blow (EncounterEnemy.MobHit) and
  clears threat at each fight's start.
- A comment had swallowed the end of the rotation's ordering line, so nothing compiled at 12:42 (CS1026).

**The table at 13:23** (before the retune below; `outputs\balance\archive-20261013-chainR14`): 27 of 40 builds in range. Single-target
numbers fell where the sentries had padded them (the Mage's four, Sifter, Thornsong, Barkhide, the Shaman's four); the Unwriting rose to
96 (Unwrite, now kept up). Area builds against three: Warrior x2.39, Judicator x2.45, Skirmisher x1.62, Combustion x2.74, Stormcaller
x3.40.

**Tanks** (three dummies striking a tough level-15 mob's blow every 2 s each; net damage taken a second, lower is sturdier):

| rank | tank | taken /s | self-heal /s | threat /s | near-deaths in 180 s |
|---|---|---|---|---|---|
| 1 | Warrior Tank | 46.0 | 0 | 94 | 14 |
| 2 | Shaman Stonebound | 53.5 | 0 | 44 | 17 |
| 3 | Druid Barkhide | 62.2 | 0.7 | 227 | 18 |
| 4 | Salt-Mender Mason | 76.2 | 0 | 50 | 26 |
| 5 | Paladin Oathguard | 80.5 | 0.7 | 54 | 26 |

The Paladin no longer leads all three roles: Oathguard is the softest tank, Judicator sits under the DPS range before the retune, and
Sanctuary heals 45. The Druid holds threat best by far; the Shaman worst.

**Retune applied at 13:24** (commit d2a4880; confirm run chainR17): 16 scales (single-target builds to 69, area builds to 64, CC, support
and tank to 48.5, Rootmend's heal with its glade-fire to 45) and the splash shares toward x2.0 against three (Warrior .39, Judicator .38,
Skirmisher .81, Combustion .29, Stormcaller .19).

**For Chris (design, not tuning):** four single-target builds already hit several foes with their own spells, so their numbers against
three run high: the Unwriting x1.42, Thornsong x1.29, Bonecaller x1.51 (the thrall), Sifter x1.34. The Shaman's kit is area-heavy in every
path (Stonebound, Gravewind and Rainspeaker do about twice as much against three as against one), so its scales are large (2.45, 2.5).
Either their area spells move to the area builds or the single-target limit (x1.25) relaxes for them.

## Chris, 2026-10-13 ~13:40 (brief, verbatim)

"why is one in the river? sparring area needs to resemble something like this? try the small props pack in downloads to help."
(With two reference pictures: a fenced sparring yard with tents, banners, dummies on stands, weapon racks and crates; and a stone
training court with archery targets in a row and recruits drilling. References for the layout only: nothing is copied from them.)

Why in the river: the spot was chosen by a script that kept clear of roads, buildings and props but never checked water; the creek runs
through that field and the mending dummy stood on its bank. The yard moves to dry, open ground and is laid out as a fenced sparring
ground (Docs/TRAINING_YARD.md).

**Confirm run (13:32-13:55, `outputs\balance\BALANCE_ALL.md`): 40 of 40 builds in range.** Single-target DPS builds 66.9-69.6; area
builds 63.8-65.3 against one and x1.94-2.60 against three (Stormcaller eased again afterwards, .19 to .12, commit 4c059c8); CC, support and
tanks 46.2-53.2; healers 44.6-49.6 HPS on the wounded dummy.

**Top three now:** single target: Shardwork 69.6, Thornsong 69.3, Censure/the Unwriting/Sifter 69.0 (all within three points by design);
against three: Stormcaller 166.7, Combustion 141.9, Warrior DPS 132.3; healers (one target): Mender 49.6, Rootmend 49.1, the Cantor 47.6;
tanks: Warrior, Shaman Stonebound, Druid Barkhide. The Druid sits near the top in all three roles, but only within the noise of a tight
band; the Paladin no longer leads any.

**Healing a party of five** (each loses 5% a second, a 45% blow every 4 s; seconds of member-time spent under 25%, lower is better):

| healer | HPS | member-seconds low |
|---|---|---|
| Shaman Rainspeaker | 107.9 | 5 |
| Cleric Choir | 97.2 | 5 |
| Cleric Lantern | 89.7 | 24 |
| Druid Rootmend | 87.6 | 52 (was 59 HPS, 380 low: the whole-party heal target and the glade-fire) |
| Paladin Sanctuary | 56.8 | 457 |
| Salt-Mender Mender | 49.3 | 525 |
| Archivist Cantor | 16.5 | 699 (its heal scale was cut to .16 because it filled the one-target test) |

Open for Chris: a party target for healers. The Paladin, Salt-Mender and the Cantor heal one at a time and leave a party low; they need a
group heal of their own, or the party test needs its own band.

## Chris, 2026-10-13 ~14:25 (briefs, verbatim)

"looks good except target should all be facing with back to fence." (the training yard)
"yes give a slower group heal" (answer to: should every healer also meet a party target; the Paladin's Sanctuary, the Salt-Mender's Mender
and the Archivist's Cantor heal one at a time and leave a party of five low)
"they are limited on single targets if they are an aoe build" (answer to: four single-target builds hit several foes with their own
spells, the Unwriting, Thornsong, Bonecaller, Sifter, and the Shaman's kit is area-heavy). Applied: a damage build doing over x1.25 against
three is held to the area band against one (62-67, moved to 64), as the splash builds are: the Unwriting 1.72 to 1.60, Thornsong 2.62 to
2.42, Bonecaller 2.37 to 2.21 (its thrall's scale with it), Sifter 1.41 to 1.31. The table labels them "(area spells)".
## Levels 16-20 (the cap of 20, 2026-10-16): targets, not yet measured
Chris, 2026-10-16: "raise top level to 20". The numbers the harness sets its targets from, as they stand (no run yet at 16-20):
- **XP:** XpToNext is unchanged (400 + 170 a level): 16 to 17 is 2,950, 19 to 20 is 3,460; 20 is 36,670 in all (15 was 21,070).
- **Talents:** 21 points at 20; every branch can take all 21 in itself (TalentDataTests). The 47 new nodes are about 3% a rank at most.
- **Mobs:** MobHealth and MobHit keep their curves: a normal mob at 20 has 690 health and hits for 50.8 (15: 540, 39.8). A camp elite
  (x5.5 health, x3 hit growing 9% a level) at 20 has 3,795 health and hits for about 413 (15: 2,970 and 270): the elite rules hold to
  the cap (SocialRulesTests loops 1-20), but the elite's hit grows faster than the player's health past 15. **To measure first**: the
  Lowtowns' two elites (Mother of Drains, Knuckle Brannoc, 19-20) in a group run; if a level-20 tank cannot hold one, cap EliteHitPerLevel's
  growth at 15 rather than retune the curve below it.
- **Gear:** generated gear climbs on its curve to 20 (the 16-20 band's words Soot-stained, Tar-sealed, Lamp-lit; looks reuse the top
  band's families). Crafted tier 6, slag-iron (required 19): plain pieces the uncommon curve one level up (a chest 62 armour, a weapon
  48 damage, 36 stat points), tempered rares the named rares' (57 damage, 75 armour, 51 points), the epic hammer 70 damage and 63 points.
- **Targets for the harness at 16-20 (to set when it runs):** the same shares as at 15 (each build's role band against the DPS median,
  healer HPS against the 4%/s test), measured at level 18 with the band's uncommon kit and at 20 with tier 6; sims at 16-20 wear the
  16-20 band (SimGear) and live in the Lowtowns' band (SimRoster.Homes, 15-20).

## The Hallow's Creek group run at 19 (2026-10-16): who does the damage, and who is the tank
Chris, 2026-10-16, after the run at level 19: "still not balanced i dont think". His meter after 581 s of fighting: you (the bot playing
the Paladin) 51 a second, the Rogue 45, the Ranger 40 and its wolf 15, the Druid healer 11 (and 16 healing), the Warrior tank 22.
Health and armour: you 607 and 375, the Warrior tank 568 and 329, the others 533.

**What was wrong**
- **The bot had no talents.** The run's temp save never spent a point, so at 19 the Paladin had 20 unspent and no build. The damage
  scale that holds each build to its band (`EncounterSession.RoleScale`) goes by the build with the most points, so the Judicator's
  x0.71 never applied, and the untalented kit hit at full strength (Smite and its swings on the weapon's power). Nothing from the cap of
  20 was in play: no level-20 talents, and the bot wore its starter gear.
- **Not the sims' rotation.** Sims have no talents or abilities to forget at 16-20: their blow is a formula (12 + 4 a level in melee,
  10 + 3.6 at range, x1.6 for a damage-dealer, one every 2 s or 2.4 s), which grows faster a level than a player's attack power (2 a
  level). On paper the Rogue (70 a second at 19), the Ranger with its wolf (about 70) and the untalented Paladin (about 70) were level.
  The Rogue landed less of it: it holds the marked mob (the held mob is not struck) and stood in melee with the tank through the bosses'
  stuns. Counted with its wolf, the Ranger (55) out-did you already.
- **The tank's health and armour.** Sims' health was one formula for the mail classes (150 + 22 a level), under a player's 24-25 a
  level, so a Paladin player passed the Warrior tank from about level 12 and the gap grew past 15. A few sims go bareheaded or without a
  neck piece or a mantle by their seed; the tank in this run was one of them.

**What changed**
- The run bot spends its points as the balance harness buys a build: all in its class's first path for the part it plays
  (`RunCapture.SpendForRole`; a Paladin with a Warrior along plays damage: the Judicator, x0.71). The log says what it bought.
- The Warrior sim (always the party's tank) has a player Warrior's health, 180 and 25 a level past the first (630 at 19, was 568), the
  most of any class at every level; the other sims keep theirs.
- The tank owns all seven pieces (`SimGear.Tank`, never bareheaded), and its armour is its pieces x1.1 (`SimCompanion.TankArmourEdge`:
  the Warrior's tank training that a sim spends no points on). At 11-20 with its two bought upgrades it is over the run's player
  (a Paladin in a sim's best pieces) at every seed. The bot's armour is set from the pieces only (`SimCompanion.PiecesArmour`).
- EditMode test: `SimGearTests.The_warrior_tank_is_the_toughest_of_a_party`.

**Asked of Chris** (his answers below, the same day): whether a hybrid's damage path should sit under the pure damage builds, and
whether the sims' health should count their gear's Stamina.

## Chris, 2026-10-16 ~22:55 (answers, verbatim)
- Question: "Should a Paladin built for damage do less damage than a Rogue or Ranger?" Answer: "A bit lower"
- Question: "Sims' health ignores the stamina on their gear... Fix it?" Answer: "Fix it now"

## Hybrids a bit lower; the sims' gear Stamina counted (2026-10-16, after Chris's answers)
**Hybrids.** A hybrid is a class with a tank's or a healer's path besides its damage path: Warrior, Druid, Paladin, Archivist, Salt-Mender,
Cleric, Shaman (`EncounterSession.HybridClasses`; `TalentDataTests` checks the list against the talent files). The pure damage classes are
the Ranger, Mage, Rogue and Scoundrel. A hybrid's damage path now targets 90% of the pure band (`EncounterSession.HybridDpsShare`):
single-target 59-65 (to 62) in place of 66-72, area 56-60 (to 58) in place of 62-67, at level 15 on the dummies. Each hybrid damage
scale is the old one x0.9: Judicator .71 to .64, Thornclaw .74 to .67, Thornsong 2.42 to 2.18, the Unwriting 1.6 to 1.44, Warrior DPS 1.1
to .99, Censure 1.68 to 1.51, Stormcaller 2.08 to 1.87, Sifter 1.31 to 1.18. A sim of a hybrid class in the damage part hits x0.9 too
(`SimCompanion.Hit`). The harness's role check (`BalanceHarness.CapViolations`) now also fails a hybrid damage build over 93% of the pure
classes' median; `toolsalidationalance_retune.py` and `balance_merge.py` use the hybrid band. The Warrior counts as a hybrid because
it has a tank's path: a choice for Chris to overturn if he sees the Warrior as a pure damage class. Not yet measured in the harness.

**The sims' gear Stamina.** A sim's health is its level's plus 5 for each point of Stamina on the pieces it wears
(`SimCompanion.MaxHealthFor(sim, items)`, `GearStamina`), as a player's is. Uncommon and rare pieces carry a lot of it: at level 11
a sim gains about 230-330 health (+60-80%), at 19 about 390-550. To keep the Warrior the toughest of a party:
- it buys its armour first: uncommon pieces from the start, rare from level 8 (`SimGear.QualityFor`);
- its health is a player Warrior's (180 + 25 a level) plus its gear's, x1.09 (`TankHealthEdge`, its Scarred Veteran, which a sim
  spends no point on);
- `SimGearTests.The_warrior_tank_is_the_toughest_of_a_party`: over every other class's sim of the same seed and purse, at every level.
  Over two sims of different seeds it is the sturdiest nearly always but not by rule: a cloth or leather sim in all rares with the
  best Stamina pieces can pass a tank in uncommons.

**The fights kept as hard as before.** The group scaling (`EncounterEnemy.GroupScale`: each sim adds a share to a mob's health and
blows) was tuned on 2026-10-11 for sims whose health counted no gear. A mob fighting a party now also strikes x the party's sims' mean
gear toughness (`SimCompanion.GearToughness`: health with the gear over health without), so a sim falls as fast as it did. A sim
healer's mend is 16% of the hurt member's health, so it rises with them. You and Mira alone are unchanged (no sims: x1). The run bot,
and the Nix fight test's player, wear a sim's pieces' Stamina as well as their armour (`SimCompanion.KitPlayerLike`), so they keep
their place in the party. **Side effect for Chris's own play in a group:** his health already counted his gear, so the mobs now hit
him harder in a party with sims, by about the sims' gain (x1.6-1.9). The group tests assert that the party wins; none of them asserts
how hard the fight is, so the only measure is a group run. The elite tests (`EliteBalanceTests`) fight you alone on paper and do not
move. Not measured yet: the group run at 17, Adit runs at 11.


- **Warrior (Chris, 2026-10-16 ~22:40):** asked "Is the Warrior a hybrid or a pure damage class?", answered "Hybrid": Warrior damage builds stay 10% under the pure damage classes (Rogue, Ranger, Mage, Scoundrel), as built in 67d05e37.

## The sims' damage in a party fight (2026-10-17)
Chris, watching the level-17 Hallow's Creek group run's meter (`run-captures\hallows-4`, 1,829 s of 42 min 46 s fighting; you went
17 to 20, the sims with you): "still not balanced i dont think". You (the bot, Judicator x0.64) 63.8 a second, in the hybrid band; the
Rogue 42.2, the Ranger 36.7 and its wolf 13.9 (50.6 together), the Warrior tank 16.8, the Druid healer 9.9 and 41.5 healing.

**How the meter counts.** `CombatMeter`: each member's damage over the seconds the party was in a fight (`EncounterSession.InCombat`:
your target fought, or any mob on the party). Seconds in which a sim does nothing while a mob is still on the party count against it.

**Where the sims' time went.** The run logs what each member is doing every 20 s; 90 of those were in a fight:

| | attacking | at your side / following | closing | stunned, rooted, disoriented | guarding Elvet | mending |
|---|---|---|---|---|---|---|
| Rogue | 51 | 24 | 6 | 5 | 4 | |
| Ranger | 62 | 24 | | | 4 | |
| Druid | 46 | 23 | | 1 | 4 | 16 |
| Warrior | 72 | 5 (waiting) | 5 | 7 | 1 | |

- **The last mob of a pack (the most of it).** In 21 of the 90 the Rogue, the Ranger and the Druid stood at your side while the tank
  fought one mob alone, often for 15-30 s, up to the camp being cleared. It was the one the Rogue had held under the moon: a mob with the
  moon (or a cross) was "leave it alone" for the whole fight, also after its hold ran out (`EncounterEnemy.LeaveAlone`), so nobody but the
  tank (who pulls what is left of its camp) and you struck it; three times nobody did, the tank waiting for the party while the mob hit
  you. About a quarter of the Rogue's fighting time (some 430 s), a fifth of the Ranger's and the healer's.
- **Boss roots.** A rooted sim did nothing ("rooted (The Fallen Mason)"); a root holds the feet only. Stuns and disorients are real.
- **The formula sat under the band at every level, not just past 15.** A sim's blow was 1.6 x (12 + 4 a level in melee, 10 + 3.6 at
  range), every 2 s (2.4 s at range). That formula grows exactly as a mob's health does (90 + 30 a level), so its level scaling is
  sound, but on a dummy it gave (at mid practice):

| level | pure band | Rogue was | Ranger + wolf was | Mage was | tank | healer |
|---|---|---|---|---|---|---|
| 11 | 51.3-56.0 | 47.0 | 34.7 + 14.0 = 48.7 | 34.7 | 14.7 | 10.9 |
| 15 | 66-72 | 60.5 | 44.8 + 18.0 = 62.8 | 44.8 | 18.9 | 14.0 |
| 17 | 73.3-80.0 | 67.2 | 49.8 + 20.0 = 69.8 | 49.8 | 21.0 | 15.6 |
| 20 | 84.3-92.0 | 77.3 | 57.4 + 23.0 = 80.4 | 57.4 | 24.2 | 17.9 |

  In the run (17-20) the Rogue landed 58% of its dummy rate, the Ranger 68%, the wolf 65%: the time above, not the level.
- Not the cause: the sims' holds cost them no time (instant), and the area blows add damage in packs.

**The bands at other levels.** The harness measures at 15 only. A sim's band at level L is the level-15 band x MobHealth(L) /
MobHealth(15) (`SimCompanion.BandScale`): a damage-dealer kills a mob of its own level in the same time at every level. The run's
Paladin bears it out: 63.8 on the meter at 17-20 is the Judicator's 62 at 15 x 1.19 (the scale at 18.5) at about 86% of the time in
reach.

**What changed**
- **The last one is fought** (`SimCompanion.LeftForLast`): when nothing free is on the party, a sim takes the nearest held or
  moon- or cross-marked mob on the party, and its mark comes off. While anything free is on the party the held one waits, as before.
- **Rooted sims fight on**: a rooted sim strikes or shoots what is in its reach (stunned, disoriented and feared ones still do not).
- **The damage part is set from the band** (`SimCompanion.Damage`): its blow is the band's middle (69 x BandScale), a hybrid's 90%,
  less its pet's bites, over its blows a second; its yard practice moves it 4% either way (66.2-71.8 at 15). The Ranger's wolf and the
  Bonecaller's thrall are counted in their keeper's damage, so the Ranger with its wolf is in the band; the wolf bites by the sim's level
  (it went by yours). The Unwriting's unwrite is counted too. New blows at mid practice: Rogue and Scoundrel 107 / 138 / 153 / 176 at
  11 / 15 / 17 / 20 (every 2 s); Ranger 95 / 122 / 136 / 156 (every 2.4 s, the wolf 14 / 18 / 20 / 23 a second besides); Mage 129 /
  166 / 184 / 212. The Mage rises most (45 to 69 at 15): it had no pet to make up the formula's shortfall.
- **The tank and the healer are unchanged** (half the formula's blow: the tank 27% of a damage-dealer's band, the healer about 20%).
  Chris's rule of 2026-10-15 ("tank is really doing too much damage compared to actual dps users") stands.
- **The fights kept as long** (`EncounterEnemy.DamagePartHealth` 1.2): a damage-part sim adds 1.2 shares to a mob's health (its blows
  unchanged), so the two damage sims of a party of four add about 7% to the mobs' health, against their blows' rise of 10-14%. The last
  mob no longer stands for 15-30 s at the end of a pack, so the runs will be quicker in time; the hard part of a fight (the pack all up,
  the boss) is about as long as it was. No group test asserts how hard a fight is; the group run is the measure.
- Test: `SimDummyTests` (PlayMode): a Rogue, a Ranger with its wolf, a Warrior tank and a Druid healer, and a Mage, a Scoundrel and an
  Archivist (a hybrid), at 11, 15, 17 and 20 on a dummy: each damage sim within 3% of its aim and its aim inside its band; the tank a
  fifth to two fifths of a damage-dealer's, the healer a tenth to a third. And the last mob held under the moon is fought by the party.
  `SimPartyTests.Mobs_grow_with_the_group_by_its_levels` checks health by the new health shares.

**What the meter should show in the next run at 17-20** (not yet measured): the Rogue about 82 x 0.8 of its time in reach, about 66;
the Ranger with its wolf about 82 x 0.88, about 72; you, the Judicator, about 64 (90% of them, as Chris chose). To run: the Hallow's
Creek group run at 17 and an Adit run at 11.
