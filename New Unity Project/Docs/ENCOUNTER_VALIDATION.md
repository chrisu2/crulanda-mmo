# Playable encounter validation

Unity 6000.6.3f1, Windows x64. Built from the user's existing Unity project.

- 110 EditMode tests passed, zero failures.
- 11 PlayMode tests passed, zero failures.
- Windows development player build succeeded.
- Standalone smoke run passed: recruit, combat, loot and equip.
- Direct camera captures of camp, combat and reward scenes were visually inspected.
- Screenshots omit the IMGUI HUD; interactive HUD layout has not been visually verified.

The automated PlayMode suite covers the full three-enemy expedition, healer casting and mana,
XP/level progression, loot, equipping, save/reload, restored identity, death recovery,
and synthesized keyboard movement with camera follow. Tests use isolated temporary save directories.

A real high-frame-rate movement bug was caught and fixed: CharacterController.minMoveDistance
is explicitly zero so small per-frame movement steps are not discarded.
Input System 1.20.0 is pinned for compatibility with Unity 6000.6; both input backends are enabled.

To play: launch Crulanda.exe and follow START-HERE.txt. Keep all companion folders together.
Unity scene: Assets/Crulanda/Scenes/PlayableEncounter.unity (not Phase0_TestMap).
The existing foundation scene remains intact.

This is a short playable encounter with primitive art and a provisional HUD, not a complete RPG.
It includes one player kit, one recruitable healer, three enemies, one equipment upgrade,
relationship scoring, a repeatable patrol, and versioned progression saves.
No final Crulanda lore, authored character animation/audio, offscreen simulation, quests,
class selection, general inventory/item pipeline, or rebinding UI is implemented yet.

Useful feedback is about movement/camera comfort, combat pacing, healing behavior and engagement.
Routine compilation and regression testing were handled automatically.

## Slower combat pass
Normal sentry health increased to 240; veteran health to 340. Player/enemy weapon swings
now occur every 2.6 seconds; Strike has a 5-second cooldown and a smaller bonus.
The opener no longer delivers Strike and an automatic weapon hit in the same frame.
Healer damage bolts are spaced 4 seconds apart. Healing, movement and camera are unchanged.
Fresh-character automated fight durations: 20.8s, 24.4s and 30.8s.
All 11 PlayMode tests pass, including the full expedition and save/reload.
Existing saves are compatible. Upgraded characters may clear fights faster.
