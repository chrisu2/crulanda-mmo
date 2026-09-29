# Phase 1 exit criteria

Status: MVP complete. Final validation: 130 EditMode / 15 PlayMode tests passed; Windows build succeeded.

The first playable encounter and reusable core combat systems are the Phase 1 deliverable.
Phase 1 does not include all future RPG content or the final ability/effect feature set.

| Requirement | Evidence/status |
|---|---|
| Movement and third-person camera | Automated keyboard movement/camera-follow integration test; prior user play confirmation |
| Targeting and combat feedback | Tab/click target selection, target frame, health/resource display |
| Auto attacks and three player abilities | Playable scenario and full expedition regression |
| Shared ability timing | Per-actor cooldown/GCD/casts, interruption, resource-commit tests |
| Damage and healing | Shared Combatant + CombatMath path, armor and effect multipliers |
| Temporary effects | Duration, same-ID refresh, non-stacking magnitudes, source cleanup, death/disable cleanup |
| Derived stats | Shared level/attribute/equipment rules with event-driven updates and no drift after expiry |
| Threat and enemy AI | Damage/healing threat, taunt, leash; proximity accumulation is now time-based |
| Death, XP, levels, loot | Full expedition and death/recovery regressions |
| Save/load | Version-1 progress restore, stable actor IDs, reward deduplication |
| Windows player | Unity 6000.6.3f1 Windows player build succeeded |

## Non-blocking follow-up work
Periodic damage/healing, crowd control, dispels and more advanced stacking policies arrive as class kits need them.
Generic inventory/vendors belong to Phase 3. Full parties and offscreen simulation belong to later phases.
Placeholder art, fixed bindings, IMGUI HUD and the logging deprecation warning remain documented technical debt.

User replay is not an exit requirement for routine changes. Use automated tests/builds and retain existing saves.

