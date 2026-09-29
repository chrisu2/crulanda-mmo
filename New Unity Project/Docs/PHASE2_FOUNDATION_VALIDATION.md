# Phase 2 foundation validation

Implemented class profiles (identity, role, resource, derived stats), stable-ID ability resolution,
level unlock enforcement before costs/effects, and class-driven action ordering with keys 1–8.
Current content retains the Warrior reference kit and its three level-1 abilities.

BuildTree validates complete proposed allocations: rank limits, ownership, level gates,
prerequisite ranks, branch investment, point budgets, exclusive choices and graph reachability.
Cross-branch investment is supported. There is no playable tree UI or applied talent effect yet.

Unity 6000.6.3f1 validation on a synchronized project copy:
- 139/139 EditMode tests passed.
- 18/18 PlayMode tests passed, including resource/effect gating and the full encounter regression.
- Windows player build succeeded; packaged playable updated.

Saves remain schema 1 and imply the single reference Warrior; existing progress is preserved.
Selectable class/build identity and allocated talents require a future explicit save migration.
The accepted matrix is Warrior Tank/DPS/Support and Druid Tank/Melee/Healer/Ranged,
with limited hybrid investment. See CLASS_BUILD_MATRIX.md.

