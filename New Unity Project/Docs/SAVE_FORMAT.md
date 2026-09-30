# Save format

## Storage envelope
File: Application.persistentDataPath/CrulandaEncounter/encounter.save.json.
The previous primary file is retained as .bak. Writes use a temporary file followed by replacement.
SaveEnvelope contains formatVersion, payloadType, gameVersion, savedAtUtc and payloadJson.

## Current playable payload
formatVersion: 1. payloadType: CrulandaEncounter. DTO: EncounterProgress.
- playerId and companionId: stable instance IDs, restored before actor activation.
- experience, health, mana (healer), companionHealth.
- x/y/z: player position; bounds and finite values validated at load.
- recruited, relationship, gold.
- inventory: item-ID list; equippedItem: item ID.
- enemies: records of id, dead and looted; prevent duplicate rewards after reload.

Combat saves are rejected. Automatic saves occur outside combat; load reconstructs actors and
clears threat, casts, personal cooldowns, global cooldowns and temporary Guard state.
The shared ability runtime does not change the persistent schema, so no version bump is required.
Unsupported format/type or invalid payloads are rejected. An unreadable startup save blocks overwrite.

## Format history (current EncounterSave.FormatVersion = 7; formats 4-7 are described at the end)
- 1: no class/talents. Read migrates to classId class.warrior with an empty talent list.
- 2: classId + talents using the 9-node prototype ids (tank.armor, dps.power, ...). Read maps them through
  EncounterSave.LegacyTalentIds to the data-driven ids; if the mapped allocation is illegal under the new tier
  rules, talents are cleared (points refunded) and everything else is kept. Unknown legacy ids reject the save.
- 3: talents use ids from EncounterContent/Talents/<class>.json (only nodes flagged impl). Validated with
  TalentTree (level + 1 budget, 5 points per tier in a branch, fully-ranked prerequisites).
Migrations happen in memory on read; the new format is written on the next normal save.
Transient talent state (weapon pressure, Exposed, barriers, intercept, standard) is never saved.

## Generic infrastructure
SaveFileStore handles envelope I/O/backups. SaveMigrator chains ISaveMigration steps on the payload text.
EncounterSave uses it from format 6 on: it registers one step per format (6 → 7: `AddDiscoveriesMigration`) and runs
the chain in memory before reading the payload. Formats 1-5 are older than the chain and are still upgraded in memory in
`EncounterSave.Read`. Versions above the current one are rejected.
To change saved fields, add the next step (7 → 8) and bump `FormatVersion`. No migration is needed for combat tuning.

## Future world persistence
WorldSaveData/CharacterSaveData/SimAdventurerSaveData/QuestSaveData/InventorySaveData/FactionSaveData
are planned; do not describe them as already implemented. Use stable content/instance IDs, not scene
object pointers, asset paths or Unity GUIDs. JSON records use lists rather than dictionaries.

## Format 4 (2026-09-29): quests
New EncounterProgress lists:
- `quests`: QuestState, one per active quest (id, step, counts, tracked).
- `questsDone`: ids of completed quests.
- `reputation`: FactionStanding (faction, value), stored only once changed from the faction's start.
- `documents`: Chronicle pages found.
- `questItems`: the quest bag, one entry per item.
- `usedInteractables`: emptied once-only props, keyed "zone|name|x|z".

Formats 1-3 load with these lists empty. On load, `EncounterSession.ReconcileQuests` starts the zone's Chronicle and fast-forwards
any step already satisfied: Mira recruited, enemies dead, blade equipped. A quest entry with an empty id or a negative step makes
the save invalid, and the store's backup handling applies.

## Format 5 (2026-09-29): XP curve
The level curve changed to `XpToNext(L) = 200 + 90*(L-1)` (cap 10, `XpForLevel(10) = 5040`).
`EncounterProgress.MigrateExperience` maps old experience onto the new curve, keeping the level and the fraction of the way
through it. It runs first for any save below format 5.

## Format 6 (2026-09-29): bag and equipment
- `bag`: `List<ItemStack>` (`item` id and `count`). The list index is the bag slot (24 slots), and an empty stack is an empty slot.
- `equipment`: `List<ItemStack>` with one entry per `EquipSlot`, indexed by the enum. A list of any other length rejects the save.
- Item ids resolve through `ItemDatabase` (`EncounterContent/Items/*.json`). Generated gear ids (`gen.<slot>.<level>.<quality>.<seed>`)
  rebuild the same item from the id, so no stats are stored.
- Migration from formats below 6: the old `inventory` ids move into free bag slots, and `equippedItem` moves to MainHand.
  Then both old fields are cleared.
- Camp mobs are not saved. They respawn on load, and story enemies keep their records in `enemies`.
- Position: when the player is in water deeper than 0.3 m, `Save` writes the last dry footing instead of the swim position.

## Format 7 (2026-09-30): discoveries
- `discoveries`: `List<string>`, the ids of the hidden finds already found (`secret.<zone>.<slug>`, from `ZoneDefinition.secrets`).
  - Each secret pays once, ever. A found id is never paid again, after a load or a reload.
  - What a find pays goes into the existing fields: `experience`, `gold`, a `bag` slot and `documents` (a Chronicle page).
    Nothing else is stored.
  - Blank ids are dropped on load.
- Migration 6 → 7 is `AddDiscoveriesMigration`, a SaveMigrator step.
  - It inserts `"discoveries":[]` before the payload's closing brace and leaves every other character of the v6 payload as it was.
  - A payload that already has the list is left alone. One that isn't a JSON object is refused: the save isn't loaded or changed.
  - The compiled step was run (outside Unity, read-only) on Chris's format-6 save and its `.bak`: all 26 fields came through
    byte-identical, and only the empty list was added. The save was backed up first to
    `work\save-backups\20260930-1739-before-format7`.
- Formats 1-5 load with an empty list.
- As before, migration happens in memory on read. The file is written as format 7 on the next save.
