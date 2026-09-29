# Project roadmap

Updated 2026-09-28. PROJECT_MASTER.md remains the design brief; this file records implementation status.
User direction: continue development from these documents, handle technical validation automatically,
and request play feedback only at meaningful gameplay milestones. Do not gate each feature on manual user testing.

## Current state
- Phase 0 integrated and validated in Unity 6000.6.3f1.
- First playable encounter implemented: movement/camera, targeting, three player abilities,
  threat, recruitable healer, death/recovery, XP, loot/equipment, save/load and repeatable patrol.
- User confirmed the playable build worked without issues, then reported combat was too fast.
- Slower combat pass: fresh-character fights measured 20.8s, 24.4s, 30.8s; 11 PlayMode tests passed.
- Completed development milestone: replaced prototype ability timing with shared per-actor ability runtime,
  global cooldowns, casts, cancellation and authored ability identities/effects. Verified: 118 EditMode and 11 PlayMode tests; Windows build succeeded.

| Phase | Goal | Status |
|---|---|---|
| 0 | Foundation | Integrated and validated; Git baseline and warning cleanup still outstanding |
| 1 | Combat sandbox | MVP complete: shared abilities, combat, timed effects and derived stats validated |
| 2 | First class loops | In progress: class profiles, unlock gates and hybrid build-graph rules implemented |
| 3 | Inventory/items/vendors | One-item loot/equipment loop exists; generic inventory/vendors not started |
| 4 | Questing | Not started; objective HUD is not a quest system |
| 5 | SimAdventurer MVP | One persistent companion prototype; world population/offscreen simulation not started |
| 6 | Group gameplay | Player + healer recruitment only; full parties/roles/loot rules not started |
| 7 | First dungeon | Not started |
| 8 | Vertical slice | Not started |

## Next work, in order
1. Phase 2 foundation implemented: class profiles, owned abilities, unlock gates, action bars and build-graph rules.
2. Build the first playable Warrior Tank/DPS/Support tree, with allocation UI, defining effects, respec and versioned saves.
3. Extend to Druid Tank/Melee/Healer/Ranged paths, then equipment restrictions and trainers. See CLASS_BUILD_MATRIX.md.
4. Phase 3: reusable inventory/equipment, item definitions and vendors.
5. Later phases: questing, persistent companion profiles, population simulation and full groups.

Phase 1 exit checks: 130 EditMode tests passed, 15 PlayMode tests passed, Windows player build succeeded.
See PHASE1_EXIT.md and COMBAT_SYSTEMS_VALIDATION.md for scope and evidence.
See PHASE2_CLASS_LOOPS.md for the next implementation sequence.
Avoid expanding the race/class roster or finalizing locations before consulting the Crulanda novels.
The starter prototype uses provisional game-only names. Eight races/fifteen classes are caps, not immediate deliverables.

## Validation policy
- Agent runs compilation, targeted unit/integration tests and builds for material changes.
- User feedback is for combat feel, pacing, companion behavior and Crulanda identity.
- No repeated user replay is needed for the slower-combat pass.
- Save compatibility and unchanged reward deduplication are regression requirements.
- Phase 1 completion includes the shared systems and automated checks, not just the prototype encounter.



Full-roster design coverage: CLASS_BUILD_MATRIX.md now covers all 12 initial classes plus later candidates (20 entries, 61 proposed paths, with Rogue/Assassin grouped). Every eventual class receives 3–4 paths. Warrior/Druid are implementation slices, not the limit of the matrix. Candidate merges and the final roster remain unresolved.

## Handoff update — 2026-09-28
Warrior nine-node talent prototype is now implemented with B-key UI, live effects, allocation/refund/respec and v1-to-v2 save migration. Verified: 142 EditMode tests, 22 PlayMode tests, Windows build success. UI has not been visually inspected and user has not tested this talent build. Earlier statements above saying no playable tree or schema 1 are historical. Full-class node calculator remains unfinished. See CLAUDE_HANDOFF.md for authoritative current status and next work.
