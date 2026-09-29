# Architecture

## Assemblies and dependencies

| Assembly | Dependencies | Responsibility |
|---|---|---|
| Core | Unity basics | IDs, tags, vocabulary, logging, services, events, RNG |
| Data | Core | Static actor/content definitions and registry |
| Gameplay | Core, Data | Actor adapters, stats, health/resources, actor registry |
| Persistence | Core | JSON envelopes, file storage and migration infrastructure |
| Abilities | None | Serializable ability data and plain-C# per-actor cast/cooldown runtime |
| DevTools | Core, Data, Gameplay | Developer console and commands |
| Game | Core, Data, Gameplay, Persistence, DevTools | Phase 0 composition root |
| Encounter | Core, Data, Gameplay, Persistence, Abilities, Input System | Playable scenario orchestration and presentation |
| Editor | Foundation, Game, DevTools, Encounter, Abilities | Scene generation and build/validation tools |
| Tests | Explicit tested assemblies | EditMode logic and PlayMode integration |

## Playable encounter
EncounterSession composes actors from EncounterContent, restores versioned progress and coordinates interaction.
AdventurerMotor handles player movement/camera; EncounterEnemy and HealerCompanion use NavMesh agents.
EncounterNavigation builds a local NavMesh for the flat arena. Decorative scenery is intentionally nonblocking.
EncounterHud provides the temporary IMGUI interface. No runtime dependency on editor tools exists.

## Ability boundary
AbilityRuntime owns cast scheduling, personal cooldowns, global cooldowns, interruption and resource-commit order.
The runtime takes explicit simulation time and has no scene references. Each actor owns a separate instance.
AbilityDefinition contains stable IDs, effect kind, power/cost, range, timing and duration. Definitions remain static.
The current content asset embeds these records; independent per-ability assets are not required yet.
The encounter adapter still validates targets and dispatches Damage/Taunt/Guard/Heal effects.
Reusable effect/status systems are next; this is not yet a full ability/effect framework.

## Persistence boundary
EncounterProgress is a DTO for this small scenario; it is not the final world or SimAdventurer database.
Actor restoration uses persistent IDs assigned while inactive. Actor.Initialize replaces the stat block,
so external subscribers must rebind after reinitialization. Combat transients are cleared on load.

## Principles
Separate simulation data from scene representation. Use static asset data plus per-actor runtime state.
Keep dependencies one-way. Promote prototype systems into shared assemblies as actual reuse emerges.
Preserve the playable encounter as an integration test while extracting reusable systems.

## Combat assembly and effect lifecycle
Crulanda.Combat depends on Core/Gameplay and owns DerivedStatCalculator/DerivedStatsController,
StatusEffectRuntime and Combatant. The encounter depends on Combat; Gameplay does not reference Combat.
Actor exposes Rebuilt/LevelChanged notifications so downstream stat bindings can reattach safely.
CombatMath combines armor and incoming-damage multipliers; Health remains the bounded health adapter.
Status definitions are static records embedded in EncounterContent. Same-ID reapplication refreshes time
without stacking magnitude. Effect modifiers are owned by runtime source tokens and removed on expiry,
death, disable or rebuild. Temporary effects are not persisted; loading reconstructs clean combat state.

## Phase 2 foundation
ClassDefinition/ClassLoadout (currently in Crulanda.Encounter) own class identity, role, resource/stat profiles and ordered unlocks. Session resolves abilities by stable ID, validates content before spawning, and gates costs/effects by player level. BuildTree validates whole proposed allocations without applying combat effects or mutating saves. It supports cross-branch investment and enforces graph reachability, ranks, prerequisites, level/branch gates, budgets and exclusive choices. Extract to a dedicated class module as the second class integrates.

