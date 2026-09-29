# Shared ability runtime validation

Editor: Unity 6000.6.3f1. Date: 2026-09-28.

## Implemented
- Crulanda.Abilities assembly with plain-C# AbilityRuntime and serializable AbilityDefinition.
- Per-actor personal cooldowns, 1.5-second offensive GCD, cast progress and interruption.
- Resource costs/cooldowns commit at start. Interrupted casts never apply effects and do not refund costs.
- Explicit off-GCD Guard; data-driven taunt/guard duration and stable ability identities.
- Player and companion now use the same scheduler; healer spells live in the content asset.
- Host adapters still validate targets and dispatch effect kinds. A full reusable effect/status system is next.

## Results
- 118 EditMode tests passed, zero failed (including eight new timing/resource tests).
- 11 PlayMode tests passed, zero failed (full expedition, healing, movement, death/recovery, save/load).
- Fight durations unchanged from slower pass: 20.8s, 24.4s, 30.8s.
- No persistent schema changes; existing saves remain version 1.

Windows x64 development build succeeded.
Automated runs use a validation copy and isolated test saves; no user replay is required.

## Files and documentation
New: Scripts/Abilities/AbilityDefinition.cs, AbilityRuntime.cs, Crulanda.Abilities.asmdef,
and Tests/EditMode/AbilityRuntimeTests.cs.
Updated: encounter content/schema, player/healer adapters, HUD cooldown query, scene builder and assembly references.
Updated Markdown: ROADMAP, ARCHITECTURE, KNOWN_ISSUES, SIMPLAYER_DESIGN, DATA_SCHEMA,
SAVE_FORMAT, ADR, CHANGELOG and project README.
