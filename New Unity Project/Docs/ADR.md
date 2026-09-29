# Architecture Decision Records

## ADR-001 — Assembly layering with asmdefs
Status: Accepted
Decision: One asmdef per subsystem with one-way dependencies (see ARCHITECTURE.md).
Reason: Enforces the brief's "no circular dependencies", speeds iteration compile time, keeps pure logic testable.
Alternatives: Single Assembly-CSharp (no enforcement); per-class asmdefs (overhead).
Consequences: New subsystems need an asmdef and explicit references. Shared vocabulary enums live in Core.

## ADR-002 — Gameplay tags are dotted path strings, declared in code
Status: Accepted
Decision: `GameTag` is a struct wrapping a path ("Damage.Fire"); hierarchical matching; canonical list in `GameTags`.
Reason: Serializable, inspectable, diff-friendly text; no asset per tag; AI-friendly. Hierarchy comes free.
Alternatives: ScriptableObject tags (hundreds of assets, GUID churn); enums (no hierarchy, reorder risk).
Consequences: Typos are caught by `OnValidate` warnings + Validate Content, not the compiler. Never rename released tags.
A property drawer with a dropdown is a possible later convenience.

## ADR-003 — Stats: base + modifiers with three operations, no derived formulas
Status: Accepted
Decision: `StatBlock` = (Base + Flat) * (1 + sum PercentAdd) * prod(1 + PercentMult), lazily cached, source-keyed removal.
Reason: Covers gear, buffs, debuffs; removal by source makes equip/unequip and buff expiry trivial.
Consequences: Attribute-to-derived-stat rules are a separate system (Phase 1). Percent values are fractions (0.1 = 10%).

## ADR-004 — ContentId vs EntityId
Status: Accepted
Decision: Definitions use human-readable lowercase `ContentId`; persistent runtime entities use GUID `EntityId`.
Reason: Saves must survive asset renames/moves (no asset paths, no Unity GUIDs) and every persistent entity needs a stable id.
Consequences: Content ids are permanent once shipped. Validate Content enforces uniqueness.

## ADR-005 — Explicit ContentDatabase, no Resources.LoadAll, no Addressables yet
Status: Accepted
Decision: A ScriptableObject lists all definitions; `ContentRegistry` is built from it at startup.
Reason: Deterministic, debuggable, easy to swap for Addressables later behind the same registry.
Consequences: New definitions must be added to the database (Validate Content warns when forgotten).

## ADR-006 — Assembly/namespace named DevTools, not Debug
Status: Accepted
Decision: `Crulanda.DevTools` replaces the brief's suggested "Debug".
Reason: A `Crulanda.Debug` namespace would shadow `UnityEngine.Debug` in every Crulanda namespace.

## ADR-007 — IMGUI for the dev console
Status: Accepted
Decision: Throwaway IMGUI console/HUD line, toggled through IMGUI key events.
Reason: Zero assets, works in any input-handling mode, fast to extend with commands. Not the player UI.
Consequences: Disables itself when `Debug.isDebugBuild` is false. Replace or supplement later if needed.

## ADR-008 — JSON saves via JsonUtility inside a versioned envelope
Status: Accepted (revisit if DTO needs outgrow JsonUtility)
Decision: `SaveEnvelope { formatVersion, payloadType, payloadJson }`, atomic writes with `.bak`, `SaveMigrator` chain.
Reason: Simple, readable, migratable from day one. JsonUtility cannot serialize dictionaries/polymorphism.
Consequences: DTOs use arrays/lists of records, not dictionaries. If that becomes painful, swap the serializer
(e.g. Newtonsoft `com.unity.nuget.newtonsoft-json`) behind `SaveFileStore` without touching the envelope contract.

## ADR-009 — Deferred on purpose
Status: Accepted
Not yet: DOTS/ECS, Addressables, NavMesh abstraction for offscreen travel, UI Toolkit vs uGUI decision,
Input System action maps, online/multiplayer. Each needs a demonstrated need first (brief sections 44/48A).

## ADR-010 — Shared ability timing before more classes
Status: Implemented and validated; results in ABILITY_RUNTIME_VALIDATION.md.
Decision: A plain-C# AbilityRuntime owns per-actor cooldowns/GCD/casts with explicit simulation time.
Static definitions remain embedded in EncounterContent; encounter adapters apply effects and validate targets.
Reason: Removes duplicate player/healer timing logic without prematurely implementing a giant effect graph.
Consequences: Casts spend resource/start cooldown at cast start; interruptions do not refund either.
Guard opts out of GCD, offensive spells share 1.5 seconds. Save/load clears transient ability state.
Next: reusable effect/status application, then class kits. Content stays provisional until canon extraction.

## ADR-011 — Source-owned timed effects and shared derived stats
Status: Implemented; verification in COMBAT_SYSTEMS_VALIDATION.md.
Decision: Add Crulanda.Combat with pure math/effect state plus thin actor adapters.
Effect refresh uses one active instance per status ID; recasts extend duration without stacking magnitude.
Derived stats are recalculated from current attributes, level and equipment, never incrementally accumulated.
Reason: Enables class buffs/defenses and avoids permanent stat drift or leftover effects after death.
Consequences: Temporary effects are cleared on death/disable/load. Persisting long-lived world effects will
require a future explicit schema and policies; DOT/HoT/CC/dispels are not implied by this initial runtime.
