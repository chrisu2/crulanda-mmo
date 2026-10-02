# Playable encounter — implementation notes

Scene: Assets/Crulanda/Scenes/PlayableEncounter.unity
This is a provisional, game-only test encounter, not canon world content.

## Play loop
Recruit Mira at camp, move along the trail, target sentries, use Strike/Challenge/Guard,
protect the healer, loot bodies, equip the blade, gain levels, and save/load.
After all three enemies die, Repeat Trail starts a new patrol while retaining progression.

## Controls
WASD movement; Space jump; hold right mouse to orbit; wheel zoom.
Tab cycles living enemies within 25m; left click selects an enemy.
The HUD shows the next automatic swing, a swing progress bar, or the out-of-range state. Auto-attacks swing every 2.6 seconds independently of ability cooldowns. 1 Strike (also starts auto-attacks); 2 Challenge (taunt); 3 Guard.
E recruits/revives the healer or loots a nearby corpse: a camp body holding gear opens the loot window, where E takes all (what does not fit stays on the body). I opens inventory.
F5 saves outside combat; F9 loads. Esc pauses. R recovers after death.

## Architecture
Crulanda.Encounter depends on Core, Data, Gameplay, Persistence and Unity.InputSystem.
- EncounterContent: editable ScriptableObject for archetypes, abilities and reward values.
- AdventurerMotor / EncounterInput: CharacterController movement, orbit camera, centralized input.
- EncounterEnemy / EncounterThreat: NavMesh travel, melee, leash, threat and taunt.
- HealerCompanion: follow, timed healing casts, mana regeneration, opportunistic damage.
- EncounterProgress: kill/loot deduplication, XP, equipment, party memory.
- EncounterSave: version-1 payload over existing atomic save envelope/file storage.
- EncounterSession: encounter orchestration, interaction and spawn/restore.
- EncounterHud: temporary runtime HUD; world labels, party/target frames, action buttons and inventory.
- EncounterNavigation: local NavMesh built at startup for the flat test area.

## Persistence
Saves go to Application.persistentDataPath/CrulandaEncounter/encounter.save.json.
World and actor IDs, health, healer mana, position, recruited state, relationship score,
XP, gold, inventory, equipped item and each enemy's death/loot flags are persisted.
Combat saves are blocked. Saves auto-update every 30 seconds outside combat and at normal exit.
Loading clears transient combat/cooldowns and respawns actors from persistent records.
Version/type mismatches and malformed payloads block loading; unreadable startup saves are protected from overwrite.

## Deliberate scope limits
One encounter, one player kit, one healer, three sentries, one weapon upgrade.
Characters/environment are primitive placeholders. Solid scenery blocks player movement and is excluded from agent navigation; foliage and flat trail decoration remain non-solid.
The companion has simple trust accumulation, not a full personality/social simulation.
No quests, vendors, offscreen simulation, full item database or class selection.
Input uses the new Input System with a legacy fallback for an already-open editor awaiting restart.
Key bindings are centralized in code, not yet user-rebindable. Both input backends are enabled.
HUD is provisional IMGUI. No authored audio, character animation, or final art.

## Automated validation
Tests exercise the full recruit/combat/heal/loot/equip/save/load loop, death recovery,
keyboard movement and camera follow, threat expiry, reward deduplication, and save round trips.
EncounterCapture runs only when explicitly launched with --crulanda-capture <directory>,
uses an isolated temporary save, captures rendered frames and exits automatically.
## Combat pacing pass
User feedback: enemies died too quickly.
- Normal enemy health: 110 -> 240. Veteran: 170 -> 340.
- Weapon swing: 1.8s -> 2.6s. Strike: 3s -> 5s cooldown, bonus damage 16 -> 12.
- Removed same-frame opening Strike plus automatic weapon hit.
- Enemy swings: 2s -> 2.6s; healer damage bolts: 3s -> 4s.
- Healing, movement and camera timing are unchanged.
- Combat timing values now live in EncounterContent for future tuning.
- Existing saves remain compatible; Repeat Trail uses the new tuning with existing gear/XP.


