# Integration and validation — 2026-09-28

Imported CrulandaUnity_Phase0.zip into D:\code\mmo\New Unity Project.
Editor: Unity 6000.6.3f1. Kept the existing built-in rendering and legacy input settings.
Test Framework 1.8.0 is now an explicit dependency (it was already resolved transitively).

## Fixes
- Disambiguated Crulanda.Core.EntityId from UnityEngine.EntityId.
- Actor reinitialization clears previous base stats and modifiers.
- Added inactive-actor initialization with a saved EntityId, retained on activation.
- Maximum-health reduction to zero current health triggers death once.
- VitalPool uses a wide intermediate to prevent addition overflow.
- Map builder refuses to overwrite existing scene/database/archetypes and offers to save dirty scenes.
- Corrected PlayMode test assembly references; editor fixture setup is excluded from player builds.

## Real Unity validation
Ran the installed editor in batch mode on a copy of this project's Assets, Packages and ProjectSettings;
the user's open Unity session was not interrupted. No Unity/NUnit substitutes were used.
- EditMode: 107 passed, 0 failed.
- PlayMode: 8 passed, 0 failed, including three new regressions for stats, identity, and death handling.
- Generated test map: four actors, two definitions, zero content validation problems.
- Builder preservation: edited the generated scene, reran the builder, verified scene and database unchanged.
- Copied generated scene/assets and Unity meta files into the original project.

## Manual editor check
1. Return to Unity and allow refresh/import.
2. Open Assets/Crulanda/Scenes/Phase0_TestMap.unity and press Play.
3. Press F1; try actor.list and actor.damage "Test Wolf A" 50.
4. Run Window > General > Test Runner to reproduce automated tests.

Interactive rendering and console input were not visually checked. No full player build was run.
The existing DEVELOPMENT_BUILD directive produces a deprecation warning in Unity 6.6.

## Remaining scope
Save files/migrations are infrastructure; complete game-state save/load is not implemented.
No movement, targeting, abilities, threat, companion AI, or inventory has been added.
URP and Input System setup remain future work. Novel canon sources are still missing.
Actor.Initialize resets pools and replaces Stats: external subscribers must rebind after initialization.
Use Health APIs rather than mutating Health.Pool directly. Entity uniqueness belongs to the future world layer.
Run Phase0Validation.ValidateGeneratedMap only in disposable validation copies.
