# Crulanda — current development state

Unity project: D:\code\mmo\New Unity Project (Unity 6000.6.3f1).
Playable scene: Assets/Crulanda/Scenes/PlayableEncounter.unity.
Foundation-only scene: Assets/Crulanda/Scenes/Phase0_TestMap.unity.

Start with Docs/ROADMAP.md for current status and next work. PROJECT_MASTER.md is the original vision,
not a claim that every listed system is implemented. ARCHITECTURE.md records current assembly boundaries.

Docs/PLAYABLE_ENCOUNTER.md covers controls and current scenario scope.
Docs/KNOWN_ISSUES.md lists limitations. Validation reports in Docs record the checks for each milestone.
User feedback: playable build worked; combat was too fast. The slower-combat pass is implemented.
Routine technical verification is automated; the user does not need to replay every change.

Shared, data-driven per-actor cooldown/casting logic is implemented and validated.
Next work follows ROADMAP.md rather than expanding directly to all classes/zones.

Phase 1 combat MVP is complete: 130 EditMode tests, 15 PlayMode tests and Windows build passed. Phase 2 is in progress: class profiles, unlock gates and hybrid build-tree rules are implemented. See Docs/PHASE2_CLASS_LOOPS.md and Docs/CLASS_BUILD_MATRIX.md. Playable talent selection/effects are not available yet.

