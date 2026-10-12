# Balance report (note 174)

Chris, 17:13: "ok definitely keep working and test all the new classes and all the new test builds and compare dps and hps to see if they are viable builds"

Harness: `Assets/Crulanda/Tests/PlayMode/BalanceHarness.cs` (explicit; run by name). Every class, each talent branch bought out at level 15, on the live kits against held-still dummies: one dummy, three together, and for healing roles the player losing 4% of max health a second. Seeded runs. Raw rows: `D:\crulanda-work\outputs\balance\balance.csv`.

## Run 1 (2026-10-12, PARTIAL, harness v1: not yet usable for tuning)

60 s fights, 3 seeds, no gear, a crude "first slot that fires" rotation. Only the Warrior and the Druid finished before the run was stopped to free Unity for the thrall capture. Known harness faults in this run, all fixed in v2 (not yet run):
- The Druid spent every tick re-casting a form (so 0 DPS for Barkhide and Thornsong, single digits for Thornclaw). v2 takes the build's own form once at the start and keeps forms, escapes and crowd control out of the rotation.
- The player died in the first healing fight (HPS 0.4 with 4%/s incoming), so every later Druid row is 0. v2 never lets the harness's damage kill, and revives between fights.
- AoE is about the same as single target: either the rotation does not reach the AoE abilities or held-still dummies do not take splash. Still to check.

So the outlier flags below are mostly the harness, not the classes. No retuning yet.

| class | build | role | scenario | runs | DPS | HPS | resource used | OOM at (s) | vs role median | flag |
|---|---|---|---|---|---|---|---|---|---|---|
| warrior | jailer | control | aoe3 | 3 | 29.4 | 0.0 | 349 | 24.1 | +0% |  |
| druid | thornclaw | dps | aoe3 | 3 | 8.1 | 0.0 | 777 | 41.5 | +0% |  |
| druid | thornsong | dps | aoe3 | 3 | 0.0 | 0.0 | 0 | - | -100% | **OUTLIER** |
| warrior | dps | dps | aoe3 | 3 | 43.7 | 0.0 | 372 | 51.6 | +442% | **OUTLIER** |
| druid | rootmend | healer | aoe3 | 3 | 2.8 | 0.0 | 268 | 40.6 | +0% |  |
| warrior | support | support | aoe3 | 3 | 29.0 | 0.0 | 357 | 25.1 | +0% |  |
| druid | barkhide | tank | aoe3 | 3 | 0.0 | 0.0 | 860 | - | -100% | **OUTLIER** |
| warrior | tank | tank | aoe3 | 3 | 27.8 | 0.0 | 377 | 37.1 | +100% | **OUTLIER** |
| druid | rootmend | healer | heal | 3 | 4.5 | 0.4 | 67 | 7.6 | +0% |  |
| warrior | jailer | control | single | 3 | 29.6 | 0.0 | 330 | 17.1 | +0% |  |
| druid | thornclaw | dps | single | 3 | 6.8 | 0.0 | 802 | 42.3 | +0% |  |
| druid | thornsong | dps | single | 3 | 0.0 | 0.0 | 0 | - | -100% | **OUTLIER** |
| warrior | dps | dps | single | 3 | 46.1 | 0.0 | 382 | 44.5 | +581% | **OUTLIER** |
| druid | rootmend | healer | single | 3 | 2.8 | 0.0 | 268 | 40.6 | +0% |  |
| warrior | support | support | single | 3 | 28.4 | 0.0 | 388 | 28.0 | +0% |  |
| druid | barkhide | tank | single | 3 | 0.0 | 0.0 | 856 | - | -100% | **OUTLIER** |
| warrior | tank | tank | single | 3 | 28.4 | 0.0 | 362 | 30.5 | +100% | **OUTLIER** |
## Next
Run v2 for all 8 classes; then 3 min fights, 5 seeds, level-15 uncommon gear (EliteBalance.Geared), Mira in the healing fight; then flag and retune.
