# Phase 2 — classes and hybrid build trees

## Goal
Each class has 3–4 recognizable base builds, with limited cross-path investment.
User examples: Warrior Tank/DPS/Support and Druid Tank/Melee/Healer/Ranged.
See CLASS_BUILD_MATRIX.md for the accepted hybrid design. These are provisional mechanical names;
novel-backed lore, spell names, forms and restrictions remain separate decisions.

## Implementation order
1. Class definitions with stable identity, role, resource/stat profiles and ability unlock levels.
2. Ability ownership/unlock enforcement and data-driven action bars.
3. Graph rules for ranked nodes, prerequisites, level/branch gates, point budgets and exclusive choices.
4. First playable Warrior tree with Tank/DPS/Support paths and defining abilities through level 10.
5. Allocation UI, meaningful combat effects, versioned build persistence and respec delivered together.
6. Druid's four paths, including form/resource behavior, on the proven shared system.
7. Equipment restrictions and trainer flow; decide additional classes after these loops are established.

## Validation
- Reject invalid identities, duplicate or unresolved unlocks and illegal build allocations.
- Enforce ownership and level gates before spending resources or applying effects.
- Test hybrid allocations, prerequisite removal, budget overruns, conflicting choices and unreachable tiers.
- Preserve current combat pacing and save behavior while introducing class data.
- Add a save migration when selectable identity and talent allocations become persistent.
- Run technical checks automatically. User feedback is reserved for meaningful playstyle decisions.

## Exit criteria
Distinct playable builds from the accepted matrix, level-10 prototype unlock coverage, meaningful
resource differences and tradeoffs, class equipment restrictions, trainer flow, and functional trees
with allocation, effects, persistence and respec. Data or validation code alone does not complete Phase 2.

## Current status
Phase 2 in progress. Class ownership, profiles, action bars and shared build-graph validation are implemented.
Current player is the Warrior reference kit, with its original three abilities. There is no selectable build
or tree UI yet. The next gameplay milestone is the Warrior Tank/DPS/Support tree.

Full-roster design coverage: CLASS_BUILD_MATRIX.md now covers all 12 initial classes plus later candidates (20 entries, 61 proposed paths, with Rogue/Assassin grouped). Every eventual class receives 3–4 paths. Warrior/Druid are implementation slices, not the limit of the matrix. Candidate merges and the final roster remain unresolved.

## Update 2026-09-28 (later) — data-driven Warrior slice, steps 4–5 done for Warrior
- Talent trees are data: EncounterContent/Talents/warrior.json (same file as the design calculator). TalentTree loads
  only nodes flagged impl and refuses any flagged node without effect code in WarriorKit.ImplementedTalents.
- 27 Warrior talents (rows 0–2 of Tank/DPS/Support) are playable, including three talent-granted actions:
  Intercept, Breaching Blow and Muster. New shared mechanics: weapon pressure (0–5), Exposed (+5% party damage
  taken), barriers (Combatant.AddBarrier), enemy stagger and a visible enemy swing timer for Timed Guard.
- Budget is level + 1 (11 points at the level-10 cap): exactly one fully committed row-2 talent, or a 5/5 hybrid.
- Save format 3 with v1/v2 migration. HUD talent panel redrawn as a tree and visually verified in a player build.
- Remaining Phase 2: a second playable class (Druid) on this system, class selection, equipment restrictions, trainers.

## Handoff update — 2026-09-28
Warrior nine-node talent prototype is now implemented with B-key UI, live effects, allocation/refund/respec and v1-to-v2 save migration. Verified: 142 EditMode tests, 22 PlayMode tests, Windows build success. UI has not been visually inspected and user has not tested this talent build. Earlier statements above saying no playable tree or schema 1 are historical. Full-class node calculator remains unfinished. See CLAUDE_HANDOFF.md for authoritative current status and next work.
