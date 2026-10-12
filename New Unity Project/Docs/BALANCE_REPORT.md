# Balance report (harness v4, 2026-10-12)

Harness: `Assets/Crulanda/Tests/PlayMode/BalanceHarness.cs` (PlayMode, Explicit, Category "Balance").
Every class and every talent branch at level 15, in standard gear: an uncommon level-15 piece in each of the 9 slots, all 9 worn for all 8 classes.
Fights last 90 s with 3 seeds, a fixed 0.05 s timestep, and dummies at 5M HP.
Raw CSV: `D:\crulanda-work\outputs\balance\balance-mage-archivist-saltmender-warrior-druid-paladin-ranger-rogue.csv`.
Each build is flagged when it is more than 15% off its role's median in that scenario. Roles with a single build (utility) are not flagged.

## Harness fixes this round
- **HPS read 0.** The heal test still had the last enemy selected, and every slot was tried in bar order, so the filler damage spell always went first. Now the heal test clears the target, moves the foes 80 m away, and tries heal-effect abilities first.
- **Seeds were identical.** Combat has no damage or crit rolls, so the fight is deterministic and `Random.InitState` changed nothing. Now each seed changes the opening: a start delay of 0.4 s per seed, a different tie order among equal abilities, and a rotated splash spread. Some kits still come out identical across seeds (sd 0). That is real determinism, not a bug.
- **AoE equal to single target.** The bar-order rotation never got past the filler. It now uses cooldown-first priority (cooldowns when ready, the filler last). A new `foes_hit` column counts the dummies that took damage.
- **Gear 0 of 9.** Gear was equipped before the level was set, so every level-15 item was refused. Fixed by setting the level first.
- **Class filter.** `CRULANDA_BALANCE_CLASSES=mage,archivist` selects classes, and each set writes `balance-<set>.csv`. The whole run of 8 classes takes about 5 min, not hours, because of the fixed timestep.

## Known harness caveats (before reading the outliers)
- The rotation is crude and the same for every class. Kits that rely on combo builders or form cycling, or that need procs set up, will read low.
- **Druid thornsong (12 DPS)** and **druid rootmend in the heal test (3.3 HPS)**: rootmend's seed sd of about 31 in single target suggests the form or opener fails on some seeds. Check these before any retune.
- **Archivist** builds hit only 1 of 3 dummies in AoE, and their numbers are flat across seeds. Its reach-limited jar splash may not be in the cooldown-first priority, or the dummies sit outside JarReach.
- **Rogue** and **Warrior support/tank** builds hit only 1 of 3. That may be correct for single-target kits.
- Time to OOM is short for Paladin (about 6 s) and Rogue (about 6-12 s). After that they run on regeneration.

## Results

### single

| class | build | role | DPS | ±sd | HPS | foes hit | OOM s | vs role median | flag |
|---|---|---|---|---|---|---|---|---|---|
| archivist | the-lull | control | 22.5 | 0.0 | 0.0 | 1 | - | -53% (n=6) | **OUTLIER** |
| mage | heatweaver | control | 48.0 | 3.0 | 0.0 | 1 | - | +1% (n=6) |  |
| ranger | pathfinder | control | 83.6 | 0.9 | 0.0 | 1 | 33.1 | +76% (n=6) | **OUTLIER** |
| rogue | thiefs-grace | control | 58.8 | 0.8 | 0.0 | 1 | 9.0 | +24% (n=6) | **OUTLIER** |
| saltmender | salt-speaker | control | 35.4 | 2.1 | 0.0 | 1 | 25.5 | -25% (n=6) | **OUTLIER** |
| warrior | jailer | control | 46.7 | 0.3 | 0.0 | 1 | 28.2 | -1% (n=6) |  |
| archivist | the-unwriting | dps | 26.1 | 0.2 | 0.0 | 1 | - | -61% (n=10) | **OUTLIER** |
| druid | thornclaw | dps | 91.8 | 0.3 | 0.0 | 1 | - | +36% (n=10) | **OUTLIER** |
| druid | thornsong | dps | 12.2 | 0.1 | 0.0 | 1 | - | -82% (n=10) | **OUTLIER** |
| mage | combustion | dps | 38.3 | 6.3 | 0.0 | 1 | - | -43% (n=10) | **OUTLIER** |
| paladin | judicator | dps | 82.0 | 1.3 | 0.0 | 1 | 6.1 | +22% (n=10) | **OUTLIER** |
| ranger | marksman | dps | 98.5 | 2.6 | 0.0 | 1 | 51.0 | +46% (n=10) | **OUTLIER** |
| ranger | skirmisher | dps | 88.0 | 0.6 | 0.0 | 1 | 39.2 | +31% (n=10) | **OUTLIER** |
| rogue | shardwork | dps | 70.2 | 1.6 | 0.0 | 1 | 11.6 | +4% (n=10) |  |
| saltmender | sifter | dps | 64.5 | 0.3 | 0.0 | 1 | 30.0 | -4% (n=10) |  |
| warrior | dps | dps | 61.6 | 0.4 | 0.0 | 1 | 38.0 | -8% (n=10) |  |
| archivist | the-cantor | healer | 22.7 | 0.2 | 0.0 | 1 | - | -8% (n=4) |  |
| druid | rootmend | healer | 21.8 | 30.8 | 0.0 | 1 | - | -12% (n=4) |  |
| paladin | sanctuary | healer | 54.8 | 0.2 | 0.0 | 1 | 18.7 | +122% (n=4) | **OUTLIER** |
| saltmender | mender | healer | 26.8 | 0.8 | 0.0 | 1 | 24.1 | +8% (n=4) |  |
| mage | bonecaller | pet | 39.1 | 2.7 | 3.6 | 1 | - | -44% (n=2) | **OUTLIER** |
| ranger | beastbond | pet | 100.2 | 0.2 | 0.0 | 1 | 34.5 | +44% (n=2) | **OUTLIER** |
| archivist | the-still-vow | support | 15.6 | 0.0 | 0.0 | 1 | - | -67% (n=5) | **OUTLIER** |
| mage | spellbinder | support | 40.0 | 3.2 | 0.0 | 1 | - | -16% (n=5) | **OUTLIER** |
| paladin | vanguard | support | 54.6 | 2.0 | 0.0 | 1 | 6.1 | +15% (n=5) |  |
| rogue | swashbuckler | support | 71.9 | 0.8 | 0.0 | 1 | 5.8 | +51% (n=5) | **OUTLIER** |
| warrior | support | support | 47.6 | 0.3 | 0.0 | 1 | 30.7 | +0% (n=5) |  |
| druid | barkhide | tank | 42.9 | 4.3 | 0.0 | 1 | - | -4% (n=4) |  |
| paladin | oathguard | tank | 55.1 | 0.9 | 0.0 | 1 | 6.1 | +23% (n=4) | **OUTLIER** |
| saltmender | mason | tank | 37.2 | 0.3 | 0.0 | 1 | 21.0 | -17% (n=4) | **OUTLIER** |
| warrior | tank | tank | 46.4 | 0.9 | 0.0 | 1 | 48.1 | +4% (n=4) |  |
| rogue | locksmith | utility | 51.9 | 0.2 | 0.0 | 1 | 6.2 | +0% (n=1) |  |

### aoe3

| class | build | role | DPS | ±sd | HPS | foes hit | OOM s | vs role median | flag |
|---|---|---|---|---|---|---|---|---|---|
| archivist | the-lull | control | 22.7 | 0.2 | 0.0 | 1 | - | -53% (n=6) | **OUTLIER** |
| mage | heatweaver | control | 48.5 | 3.2 | 0.0 | 3 | - | +1% (n=6) |  |
| ranger | pathfinder | control | 67.5 | 0.8 | 0.0 | 3 | 21.6 | +41% (n=6) | **OUTLIER** |
| rogue | thiefs-grace | control | 59.9 | 0.2 | 0.0 | 1 | 8.1 | +25% (n=6) | **OUTLIER** |
| saltmender | salt-speaker | control | 36.9 | 0.3 | 0.0 | 3 | 24.3 | -23% (n=6) | **OUTLIER** |
| warrior | jailer | control | 47.3 | 0.6 | 0.0 | 3 | 24.0 | -1% (n=6) |  |
| archivist | the-unwriting | dps | 26.1 | 0.2 | 0.0 | 1 | - | -61% (n=10) | **OUTLIER** |
| druid | thornclaw | dps | 90.4 | 0.0 | 0.0 | 1 | - | +36% (n=10) | **OUTLIER** |
| druid | thornsong | dps | 12.4 | 0.1 | 0.0 | 1 | - | -81% (n=10) | **OUTLIER** |
| mage | combustion | dps | 42.9 | 4.5 | 0.0 | 3 | - | -36% (n=10) | **OUTLIER** |
| paladin | judicator | dps | 87.4 | 3.1 | 0.0 | 3 | 22.5 | +31% (n=10) | **OUTLIER** |
| ranger | marksman | dps | 91.7 | 1.6 | 0.0 | 3 | 52.5 | +37% (n=10) | **OUTLIER** |
| ranger | skirmisher | dps | 68.2 | 1.5 | 0.0 | 3 | 35.2 | +2% (n=10) |  |
| rogue | shardwork | dps | 71.2 | 1.3 | 0.0 | 1 | 15.6 | +7% (n=10) |  |
| saltmender | sifter | dps | 64.5 | 0.3 | 0.0 | 3 | 30.5 | -3% (n=10) |  |
| warrior | dps | dps | 65.2 | 0.4 | 0.0 | 3 | 46.0 | -2% (n=10) |  |
| archivist | the-cantor | healer | 22.7 | 0.2 | 0.0 | 1 | - | -10% (n=4) |  |
| druid | rootmend | healer | 22.0 | 31.1 | 0.0 | 1 | - | -13% (n=4) |  |
| paladin | sanctuary | healer | 55.5 | 0.0 | 0.0 | 3 | 6.1 | +120% (n=4) | **OUTLIER** |
| saltmender | mender | healer | 27.9 | 0.0 | 0.0 | 3 | 26.3 | +10% (n=4) |  |
| mage | bonecaller | pet | 39.3 | 3.4 | 0.1 | 3 | - | -27% (n=2) | **OUTLIER** |
| ranger | beastbond | pet | 68.5 | 0.7 | 0.0 | 3 | 37.0 | +27% (n=2) | **OUTLIER** |
| archivist | the-still-vow | support | 15.6 | 0.0 | 0.0 | 1 | - | -66% (n=5) | **OUTLIER** |
| mage | spellbinder | support | 40.4 | 2.9 | 0.0 | 3 | - | -13% (n=5) |  |
| paladin | vanguard | support | 54.9 | 2.2 | 0.0 | 3 | 6.1 | +18% (n=5) | **OUTLIER** |
| rogue | swashbuckler | support | 71.4 | 1.4 | 0.0 | 1 | 6.1 | +54% (n=5) | **OUTLIER** |
| warrior | support | support | 46.4 | 1.6 | 0.0 | 1 | 37.1 | +0% (n=5) |  |
| druid | barkhide | tank | 46.0 | 0.0 | 0.0 | 3 | - | -2% (n=4) |  |
| paladin | oathguard | tank | 55.6 | 0.1 | 0.0 | 3 | 6.5 | +18% (n=4) | **OUTLIER** |
| saltmender | mason | tank | 37.0 | 0.0 | 0.0 | 3 | 22.5 | -21% (n=4) | **OUTLIER** |
| warrior | tank | tank | 47.9 | 0.6 | 0.0 | 1 | 45.9 | +2% (n=4) |  |
| rogue | locksmith | utility | 51.4 | 0.7 | 0.0 | 1 | 5.2 | +0% (n=1) |  |

### heal

| class | build | role | DPS | ±sd | HPS | foes hit | OOM s | vs role median | flag |
|---|---|---|---|---|---|---|---|---|---|
| archivist | the-cantor | healer | 0.0 | 0.0 | 24.6 | 0 | - | +6% (n=4) |  |
| druid | rootmend | healer | 0.0 | 0.0 | 3.3 | 0 | - | -86% (n=4) | **OUTLIER** |
| paladin | sanctuary | healer | 0.0 | 0.0 | 25.3 | 0 | - | +9% (n=4) |  |
| saltmender | mender | healer | 0.0 | 0.0 | 22.0 | 0 | - | -6% (n=4) |  |