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

## Format history (current EncounterSave.FormatVersion = 6; formats 4-6 are described at the end)
- 1: no class/talents. Read migrates to classId class.warrior with an empty talent list.
- 2: classId + talents using the 9-node prototype ids (tank.armor, dps.power, ...). Read maps them through
  EncounterSave.LegacyTalentIds to the data-driven ids; if the mapped allocation is illegal under the new tier
  rules, talents are cleared (points refunded) and everything else is kept. Unknown legacy ids reject the save.
- 3: talents use ids from EncounterContent/Talents/<class>.json (only nodes flagged impl). Validated with
  TalentTree (level + 1 budget, 5 points per tier in a branch, fully-ranked prerequisites).
Migrations happen in memory on read; the new format is written on the next normal save.
Transient talent state (weapon pressure, Exposed, barriers, intercept, standard) is never saved.

## Generic infrastructure
SaveFileStore handles envelope I/O/backups. SaveMigrator provides version chaining but the current
single-version EncounterSave adapter rejects unknown versions. Add an explicit migration before
changing saved fields or separating companion/world profiles. No migration is needed for combat tuning.

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
