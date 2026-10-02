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
- enemies: records of id, dead and looted; prevent duplicate rewards after reload. Camp mobs are not saved, and neither is what lies on their bodies (loot step L1: drops are rolled when a mob dies and live on the body until it is emptied or respawns) or an open loot window.
- Named loot (loot step L2) adds no field and no format step: its `loot.*` ids sit in the bags and equipment like any item id, and set bonuses, effects and uniqueness are worked out from them on load. An older build shows a `loot.*` id as "?" and keeps it. A new character in the zones is saved with the Tempered Trailblade already in the main hand; saved characters are not changed.

Combat saves are rejected. Automatic saves occur outside combat; load reconstructs actors and
clears threat, casts, personal cooldowns, global cooldowns and temporary Guard state.
The shared ability runtime does not change the persistent schema, so no version bump is required.
Unsupported format/type or invalid payloads are rejected. An unreadable startup save blocks overwrite.

## Format history (current EncounterSave.FormatVersion = 8; formats 4-8 are described at the end)
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
EncounterSave uses it from format 6 on: it registers one step per format (6 → 7: `AddDiscoveriesMigration`,
7 → 8: `AddProfessionsMigration`) and runs the chain in memory before reading the payload. Formats 1-5 are older than the chain and are still upgraded in memory in
`EncounterSave.Read`. Versions above the current one are rejected.
To change saved fields, add the next step (8 → 9) and bump `FormatVersion`. No migration is needed for combat tuning.

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

## Format 8 (2026-10-01): trades
Two new lists at the end of the payload, for the whole professions feature (one format bump, one migration):
- `professions`: `List<ProfessionSkill>` (`id` and `skill`), the trades the character has and the skill in each.
  - Ids come from `EncounterContent/Professions/*.json` (`mining`, `woodcutting`, `herbalism`, `cooking`, `blacksmithing`, `alchemy`).
  - `ProfessionLog.Bind` adds the trades everyone has from the start (`herbalism`, `cooking`) at skill 1 when they are missing, so
    a migrated character gains those two entries at its next save. A gathering tool adds its skill at 1 when first used
    (the tool item is used up; nothing else records it).
  - On load: blank ids are dropped, and so is a later entry for an id already listed. A `skill` below 0 or above 100 refuses the
    save ("Invalid profession data."): it is not loaded and the file is not changed. An id this build's content doesn't know is
    kept in the save and ignored by `ProfessionLog`, so content can change. A known trade's skill is brought to at least 1 on bind.
- `pouches`: `List<string>`, the trade bags worn, by item id, in the order they were put on.
  - Each worn bag adds its slots to the end of `bag` (after slot 24) in this order (the leatherworker's bags, trades step 5):
    the wallet's 6, the sling's 6, the scrip's 8, the poke's 8, so four bags make a 52-slot `bag`. The list and the slots are
    saved as they are; on load the session pads `bag` to cover every known worn bag (`Inventory.EnsurePouches`) and never
    shortens it.
  - An unknown bag id (content that no longer has it) has no slots: the known bags' slots are counted from slot 24 without it,
    and slots past them take nothing new but can be emptied.
  - On load: blank ids and repeats are dropped. More than 8 entries (`EncounterSave.MaxPouches`) refuses the save
    ("Invalid item data."), as a `bag` longer than 96 slots already did. An unknown bag id is kept.
- Migration 7 → 8 is `AddProfessionsMigration`, a SaveMigrator step of the same kind as 6 → 7.
  - It inserts `"professions":[]` and `"pouches":[]`, in that order, before the payload's closing brace and leaves every other
    character of the v7 payload as it was. A list that is already there is left alone (only the missing one is added). A payload
    that isn't a JSON object is refused: the save isn't loaded or changed.
  - A format-6 save runs both steps (6 → 7 → 8) in one read.
  - The step's source was compiled outside Unity and run, read-only, on Chris's save and its `.bak` (both still format 6 on
    2026-10-01): all 26 fields came through byte-identical, and only `discoveries`, `professions` and `pouches` were added, empty.
    Back the save folder up to `work\save-backups\<date>-pre-format-8` before the first run of this build.
- Formats 1-5 load with both lists empty.
- Not saved: which nodes have been worked (they regrow in memory), known recipes (they follow from skill).
- As before, migration happens in memory on read. The file is written as format 8 on the next save.
