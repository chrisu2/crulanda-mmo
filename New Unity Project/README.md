# Crulanda — current development state

Unity project: D:\code\mmo\New Unity Project (Unity 6000.6.3f1).
Git repo: D:\code\mmo (branch main). Backup: J:\claude\unity projects\mmo (run ..\tools\Backup.ps1).
Current state and every location: ..\CLAUDE.md and Docs/CLAUDE_HANDOFF.md (this README's status notes below are older).
Playable scene: Assets/Crulanda/Scenes/PlayableEncounter.unity.
Foundation-only scene: Assets/Crulanda/Scenes/Phase0_TestMap.unity.

Start with Docs/GAME_BRIEF.md (the standing direction) and Docs/CLAUDE_HANDOFF.md (current state). PROJECT_MASTER.md is the original vision; its example zones (Greenhaven, Ashwood, Blackstone) were never built and are superseded by the four Crulanda zones,
not a claim that every listed system is implemented. ARCHITECTURE.md records current assembly boundaries.

Docs/PLAYABLE_ENCOUNTER.md covers controls and current scenario scope.
Docs/KNOWN_ISSUES.md lists limitations. Validation reports in Docs record the checks for each milestone.
User feedback: playable build worked; combat was too fast. The slower-combat pass is implemented.
Routine technical verification is automated; the user does not need to replay every change.

Shared, data-driven per-actor cooldown/casting logic is implemented and validated.
Next work follows ROADMAP.md rather than expanding directly to all classes/zones.

Phase 1 combat MVP is complete: 130 EditMode tests, 15 PlayMode tests and Windows build passed. Phase 2 is in progress: class profiles, unlock gates and hybrid build-tree rules are implemented. See Docs/PHASE2_CLASS_LOOPS.md and Docs/CLASS_BUILD_MATRIX.md. Playable talent selection/effects are not available yet.

