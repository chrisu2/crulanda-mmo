# Combat systems / Phase 1 completion

Unity 6000.6.3f1. This report covers the shared systems needed to leave the combat-sandbox phase.

## Changes
- Added Crulanda.Combat: CombatMath, Combatant, DerivedStatRules/Calculator/Controller,
  StatusEffectDefinition and StatusEffectRuntime.
- Guard uses a data-defined timed status. Damage uses shared armor/multiplier resolution.
- Primary attributes, level and equipment feed derived health, attack power, spell power and armor.
- Modifier batches invalidate all related caches before events fire. Multi-stat effects apply together.
- Reapplication refreshes a status ID without stacking magnitude; expiry/death/disable/rebuild remove its modifiers.
- Actor lifecycle notifications let derived-stat bindings reconnect after reinitialization.
- Enemy proximity threat is elapsed-time based; damage threat uses actual applied damage.

## Validation coverage
EditMode covers stat formulas, mitigation, status refresh/expiry/source ownership, invalid definitions,
consistent batched reads and frame-rate-independent proximity threat.
PlayMode covers timed protection, death cleanup, derived-stat updates/reinitialization, lethal stat changes,
atomic multi-stat application and the full encounter/save/load/movement regression.
Final results: 130/130 EditMode and 15/15 PlayMode tests passed; Windows player build succeeded. Reports are in ValidationResults/combat-*. Validation ran in Unity 6000.6.3f1 on a synchronized project copy, leaving the open working editor untouched.

## Phase boundary
This completes the reusable Phase 1 MVP. It does not claim a finished RPG framework.
Phase 2 focuses on distinct class loops, ability ownership/unlocks, resources, trainers and basic talents.
Generic vendors/inventory expansion, full parties and offscreen simulation remain in later phases.

## Compatibility and limitations
No save schema change. Existing save IDs, progression and equipment remain valid.
Temporary effects are cleared on load; long-lived persistent buffs require a future schema/policy.
Status effects currently support stat modifiers and incoming-damage multipliers. Periodic effects,
CC/dispels and advanced stacking are future class-driven extensions.
Combat remains deliberately paced: regular fights ~21/24 seconds; veteran ~35 seconds after corrected threat.
Visual art/HUD remain provisional. No user replay is needed for this technical milestone.

