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
