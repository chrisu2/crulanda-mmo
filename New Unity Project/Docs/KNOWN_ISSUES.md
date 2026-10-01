# Known issues and limitations

## Current playable scope
One player kit, one healer companion, three enemies, one equipment upgrade and a repeatable encounter.
No full class system, general inventory/vendor system, quests, dungeon, or offscreen world simulation yet.
Primitive art, no authored animation/audio, temporary IMGUI HUD and fixed keyboard bindings.
Camera captures verify world rendering but omit IMGUI; user reported the previous playable build worked.
The IMGUI HUD (including the B talent panel) is drawn on a fixed 1440x900 canvas with a non-uniform GUI.matrix scale,
so it stretches on non-16:10 aspect ratios (notably ultrawide). The talent panel and combat HUD were visually checked at 1440x900
via `Crulanda.exe --crulanda-ui-capture <dir>` (windowed; isolated temp save); other resolutions are unchecked.
Warrior talents: Mira is the only ally, so "allies" effects target her; there are no crits or enemy casts yet, so those
talent ideas were adapted (see warrior.json descriptions). F10 (development builds only) jumps to level 10 for review.

## Technical boundaries
- EncounterSession orchestrates ability effects; shared damage/healing, timed statuses and derived stats are now implemented. DOT/HoT/CC/dispels and advanced stacking remain future class-driven extensions.
- Cast interruption spends the original cost and preserves cooldowns; this is deliberate and documented.
- Standalone ability timing state is transient and reset on load; combat saves remain disallowed.
- Enemy proximity threat now uses elapsed time; test broader multi-enemy group tactics as classes expand.
- Entity uniqueness belongs to the future persistent-world layer; actor restore requires inactive objects.
- Actor.Initialize replaces Stats and resets pools; external listeners must rebind.
- Health.Pool is publicly exposed; gameplay should use Health APIs instead of directly mutating it.
- Both scene and domain reload disabled simultaneously remain unverified.
- GameTags reflection under IL2CPP stripping remains unverified.
- CrulandaLog's DEVELOPMENT_BUILD directive emits a Unity 6.6 deprecation warning.
- Git repository baseline has not been initialized.
- The foundation map builder preserves existing generated assets rather than rebuilding over them.
- Runtime material lifecycle and collision-aware orbit camera need a future polish pass.

## Content
Crulanda canon extraction awaits novels/supporting documents. Prototype names/content are game-only.
URP conversion is deferred; current project uses built-in rendering and Input System 1.20.0 with both input backends.
See ROADMAP.md for actual phase status; historical Phase 0 reports are not current feature inventories.

- Phase 2 foundation currently exposes only the Warrior reference kit; class/build selection, talent effects, tree UI and persisted allocations are not implemented. Build graph rules alone are not a playable talent system.

## Handoff update — 2026-09-28
Warrior nine-node talent prototype is now implemented with B-key UI, live effects, allocation/refund/respec and v1-to-v2 save migration. Verified: 142 EditMode tests, 22 PlayMode tests, Windows build success. UI has not been visually inspected and user has not tested this talent build. Earlier statements above saying no playable tree or schema 1 are historical. Full-class node calculator remains unfinished. See CLAUDE_HANDOFF.md for authoritative current status and next work.

## World life / day-night — 2026-09-28
- The world clock is not saved. Each launch starts at 08:30, and the hour carries across zone travel only within one run.
- Villagers, hens and eggs are only simulated while the zone is loaded. Nothing catches up off-screen.
- Eggs are counted at the coop; they become an item ("Oakhaven eggs") only once the hen-wife has sold them to the stall. The
  other goods the trades carry (grain, flour, bread, wood, hides, herbs) are a stock count the village talks about, not items.
- The village's stock and the day's errands are not saved: a new load starts the day's deliveries again.
- Chickens walk in straight lines (no pathfinding) and can cut through thin props.
- Night uses the same directional light as a moon. Windows glow the same by day and night.
