# Save format

## Storage envelope
File: Application.persistentDataPath/CrulandaEncounter/encounter.save.json.
The previous primary file is retained as .bak. Writes use a temporary file followed by replacement.
SaveEnvelope contains formatVersion, payloadType, gameVersion, savedAtUtc and payloadJson.

## Current playable payload
formatVersion: 1. payloadType: CrulandaEncounter. DTO: EncounterProgress.
- playerId and companionId: stable instance IDs, restored before actor activation.
- experience, health, mana (healer), companionHealth.
  - Since 2026-10-01 Mira's full health grows with the player's level (130 at level 1, 22 more a level). companionHealth is
    still her current health and the format did not change. A saved value of exactly her level-1 full health (130) is read as
    full health at any level (`EncounterSession.CompanionHealthFromSave`), so a save from before she grew loads her whole and
    not wounded. Any other value is her health as saved.
- x/y/z: player position; finite values within 500 m of the zone's centre and 250 m of sea level, checked at load (`EncounterSave.Placeable`;
  the depth limit was 60 m until a save on the drawn-out Sealed Adit's deep floors came back a new character, 2026-10-11).
- recruited, relationship, gold.
- inventory: item-ID list; equippedItem: item ID.
- enemies: records of id, dead and looted; prevent duplicate rewards after reload. Camp mobs are not saved, and neither is what lies on their bodies (loot step L1: drops are rolled when a mob dies and live on the body until it is emptied or respawns) or an open loot window.
- Named loot (loot step L2) adds no field and no format step: its `loot.*` ids sit in the bags and equipment like any item id, and set bonuses, effects and uniqueness are worked out from them on load. An older build shows a `loot.*` id as "?" and keeps it. A new character in the zones is saved with the Tempered Trailblade already in the main hand; saved characters are not changed.

Combat saves are rejected. Automatic saves occur outside combat; load reconstructs actors and
clears threat, casts, personal cooldowns, global cooldowns and temporary Guard state.
The shared ability runtime does not change the persistent schema, so no version bump is required.
Unsupported format/type or invalid payloads are rejected. An unreadable startup save blocks overwrite.

## Format history (current EncounterSave.FormatVersion = 11; formats 4-11 are described at the end)
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
7 → 8: `AddProfessionsMigration`, 8 → 9: `AddArmouryMigration`, 9 → 10: `SteeperCurveMigration`, 10 → 11: `AddRecipesMigration`) and runs the chain in memory before reading the payload. Formats 1-5 are older than the chain and are still upgraded in memory in
`EncounterSave.Read`. Versions above the current one are rejected.
To change saved fields, add the next step (11 → 12) and bump `FormatVersion`. No migration is needed for combat tuning.

## The world slot (2026-10-06, Phase 5.2)
File: the same root, slot "world" (world.save.json, .bak kept), payloadType CrulandaWorld, formatVersion 1, DTO WorldSave:
seed and the list of SimAdventurer (id, name, folk, classId, homeZone, zone, level, variant, gearSeed, bold, friendly, chatty,
onlineFrom, onlineHours, x, z; later regard, friend, greetedHour, guild), the party (sim ids, round 27) and guildedUpTo
(how many sims have had their guild chosen; round 27). Shared by every character; created when first missing (SimRoster.LoadOrCreate) and written with
each character autosave (SimPopulation.Persist). An unreadable world slot falls back to its .bak, and failing that is made anew.
Not migrated yet: bump WorldSave.FormatVersion and add a step when its fields change. Fields added since read as empty in an older
world slot (format 1 still); an older world of twenty sims grows to forty on load (SimRoster.Grow, round 27). The character
keeps its guild in EncounterProgress.guild ("" none; round 27), which an older save reads as none.

## Future world persistence
CharacterSaveData/QuestSaveData/InventorySaveData/FactionSaveData
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
- Not saved: which nodes have been worked (they regrow in memory), known recipes (they follow from skill; from format 11 the found
  recipes learned from leaves are saved, see below).
- As before, migration happens in memory on read. The file is written as format 8 on the next save.

## Format 9 (2026-10-01): the Armoury (loot step L3)
Three new lists at the end of the payload, after `pouches`, in this order (one format bump, one migration):
- `armoury`: `List<string>`, the named pieces of gear ever found, by item id (`loot.*` and the twelve older `item.*` pieces that have
  a gear entry in the loot files), in the order found.
  - A piece is found the first time it is in the bags or worn. It stays found after it is sold or destroyed.
  - `ArmouryLog.Sweep` looks over `bag` and `equipment` twice a second, so loot, buying, crafting and quest rewards are all noticed.
  - From format 9, "owned" in the drop rules means found here or held now: signature lists give a piece not yet found, and an epic
    already found drops at a quarter of its chance and keeps no pity count.
- `looks`: `List<string>`, every appearance that has been in the bags or worn, by appearance key (`GearLooks.AppearanceKey` of the
  item's resolved look: family, variant, palette, crafted tint prefix and forced glow, as `family:variant/palette*tint+glow`). The
  key holds no colours, quality, shade, detail or tier: a better copy of a piece already seen is not a new look, and retuning a hex
  colour in `looks.json` keeps every saved key. (`GearLooks.LookKey`, which has the colours, is only the in-memory icon and render
  key.) The first time a key is added the game raises the "NEW LOOK" toast. Generated gear adds a look and never an `armoury` entry.
- `lootLuck`: `List<LootLuck>` (`source`, `kills`, `dry`), kill counts by loot source.
  - `source` = a drop list id (`drop.oak.caddock`): kills of mobs that roll a list naming a zone, camp tag or mob. Any kill makes that
    list's pieces known in the Armoury (their names show in grey). So does finding one of the list's pieces (a save from before
    format 9 counted no kills, so wearing Caddock's Tin Crown is what names his other pieces); that adds no kill, because pity
    reads only real kills. World lists (a level band alone) are not counted.
  - `source` = a drop list id and group (`drop.oak.caddock#1`): an epic's pity counter, `kills` while it was unfound and `dry`, the
    current run of kills without it. The epic is certain on the kill that would make `dry` reach the group's `pity` (10 for dungeon
    end bosses, 25 outdoors), and `dry` goes back to 0 when it drops.
- On load: a missing list is empty; blank ids and sources are dropped, and so is a later entry for one already listed (the first is
  kept). A negative `kills` or `dry` refuses the save ("Invalid loot data."): it is not loaded and the file is not changed. Ids and
  sources this build's content doesn't know are kept in the save and ignored, so content can change.
- First bind: `ArmouryLog.Bind` (on every session start and every load) quietly marks as found the named gear in the bags or worn and
  the named gear of hidden finds already in `discoveries` (by the gear entry's `secret:` source, or the zone secret's `item`), and
  adds the looks of what is held. It raises no toast and changes no other field. The game writes the result on its next save.
- Migration 8 → 9 is `AddArmouryMigration`, a SaveMigrator step of the same kind as 7 → 8.
  - It inserts `"armoury":[]`, `"looks":[]` and `"lootLuck":[]`, in that order, before the payload's closing brace and leaves every
    other character of the v8 payload as it was. A list that is already there is left alone (only the missing ones are added). A
    payload that isn't a JSON object is refused: the save isn't loaded or changed.
  - A format-6 save runs all three steps (6 → 7 → 8 → 9) in one read. Chris's Warrior save (`encounter.save.json`) was format 6 on
    the morning of 2026-10-01 (1,329 XP in Oakhaven, the Training Blade in hand, a wolf pelt in the bags) and format 8 that evening,
    after he played the trades build (2,137 XP, Caddock's Tin Crown and generated pieces worn, the Training Blade and a generated
    mantle in the bags). `OwnerSaveFixtures` (in `ArmouryTests.cs`) holds both shapes, and
    `SaveMigratorTests.V8Payload_MigratesTo9_...` migrates each one: every other field comes through as it was. On the first bind
    the format-8 save gains `"armoury":["item.training_blade","item.tin_crown"]` (bags first, then worn) and one look for each
    distinct appearance of the gear it holds (five pieces in the fixture, so at most five); Caddock's other pieces (the Due Cleaver,
    the Due Coat and the Broken Oath Sabre) show as known. The format-6 save gains `"armoury":["item.training_blade"]` and the
    blade's one look.
  - What to expect on his real save: a later read-only check (the evening of 2026-10-01) reported the Tin Crown and five generated
    pieces worn and the Training Blade in the bags. The rule is the same whatever he holds: `armoury` gains the named pieces held
    (here the blade and the crown), `looks` one key for each distinct appearance held (up to one per piece of gear), `lootLuck`
    stays empty, and nothing else changes. His second slot, `encounter-druid.save.json` (format 8, reported to hold no gear),
    migrates too and gains the three lists empty.
  - Back up the whole `CrulandaEncounter` folder (both character saves, their `.bak` files and `profile.save.json`) to
    `work\save-backups\<date>-pre-format-9` before the first run of this build.
- Formats 1-8 load with all three lists empty.
- Not saved: the count on the Armoury tab (pieces new since the tab was last open), and whether a piece is known (it follows from
  `lootLuck`, `armoury`, `quests` and `questsDone`).
- An older build refuses a format-9 save (its version is above theirs) and leaves the file alone.
- As before, migration happens in memory on read. The file is written as format 9 on the next save.

## Format 10 (2026-10-08, round 29): the cap-15 curve
`SteeperCurveMigration` (9 → 10) rewrites `experience` so the character keeps its level and the fraction of it it had under the old
curve (200 + 90 a level, cap 13) on the new one (400 + 170 a level, cap 15); nothing else in the payload changes. The world slot
(the sims) is not migrated: a sim's level is the saved truth and its experience only delays its next level.

### Within format 10 (2026-10-09, dungeon step D3): keys and spirit stones
Two lists added without a format change (a save without them loads with both empty; `EncounterSave` also fills in a missing one,
drops blanks and repeats):
- `keys`: `List<string>`, kept for good: the Sealed Adit's rail sigils (`sigil.amber`, `sigil.ember`, `sigil.grey`, taken from the
  branch bosses, `ZoneCamp.key`) and `adit.lift` once they are set in the cage-lift's frame (the lift gate then stands open on every
  visit).
- `spiritStones`: `List<string>`, the spirit stones touched, by `ZoneInteractable.Key` (`zone.adit|The Gallery's spirit stone|x|z`):
  `RecoveryPoint` in that zone is beside the stone.
- Not saved: the platform gate (shut again on every visit) and a keg of powder in hand.

## The profile slot, format 2 (2026-10-15, p9-exchange): the Treasury Vault
File: the same root, slot "profile" (profile.save.json, .bak kept), payloadType CrulandaProfile. `CharacterProfile.FormatVersion = 2`.
- Format 1 was `lastClass` alone (which character opens). Format 2 adds the Treasury Vault, shared by every character (Chris,
  2026-10-15: "Shared by all (Recommended)"): `vault`, `List<ItemStack>` (the list index is the slot, an empty stack an empty slot;
  48 slots, 12 more for each row bought) and `vaultRows`, the rows of 12 bought past the first four (0-4).
- Migration 1 → 2 is `AddVaultMigration` (SaveMigrator, in `CharacterProfile.cs`): it inserts `"vault":[]` and `"vaultRows":0` before
  the payload's closing brace and leaves every other character as it was (so `lastClass` comes through). A field already there is
  left alone; a payload that is not a JSON object is refused. A format-1 profile, or none, opens an empty vault of 48; the file is
  written as format 2 on the next vault change or character choice.
- On read: a negative stack count or `vaultRows` outside 0-4 refuses the profile ("Invalid vault data."). A profile that is there but
  unreadable, or newer than this build, opens no vault and is never written over by it (`LoadVault` null, `SaveVault` false); the
  character choice reads as before.
- `CharacterProfile.Remember` (choosing a character) now reads the profile and keeps the vault; before, it wrote `lastClass` alone.
- Writes: the vault is written to the profile first, then the character is saved, after each deposit, withdrawal or row bought (a
  crash between leaves the item in both, never in neither). Nothing is done in a fight, or with saving off.
- An older build reads `lastClass` from a format-2 profile, but its next character choice writes format 1 without the vault: do not
  run an older build against a profile that holds a vault.

## The world slot, format 2 (2026-10-15, p9-exchange): the Exchange
`WorldSave.FormatVersion = 2`. One new field at the end, `exchange` (`ExchangeState`, Exchange.cs):
- `listings`: the lots up (id, item, count, seller (a sim id, "" yours), sellerName, owner (your character's class id), buyout for the
  lot, startBid (0 buyout only), bid, bidder (a sim id, or "you:" and a class id), bidderName, deposit, listedAt, endsAt).
- `parcels`: what waits at the clerk for a character (owner by class id, coin, item, count, note): sale coin with the deposit back,
  bids given back, lots won, lots taken down or unsold.
- `history`: the last 60 lines of what happened to your lots and bids (owner, line, at).
- The Exchange's own clock: `hours` (world-hours since it opened, only rising; `endsAt` counts in them), `tickedTo` (the last whole
  hour run), `clockAt` (the world-clock hour last seen) and `realUtc` (real time at the last world save: the next start runs the time
  away at 36 world-hours a real hour, 48 at most). `nextId` numbers the lots.
- Migration 1 → 2 is `AddExchangeMigration` (in `SimAdventurers.cs`), run by `SimRoster.Load`: it inserts `"exchange":{}` before the
  closing brace and leaves every other character (the sims, the party, the guilds) as it was; `Exchange.Ensure` fills the empty lists.
  A payload that already has it is left alone; one that is not a JSON object is refused.
- An older build refuses a format-2 world ("Unsupported world save") and, as `LoadOrCreate` does with any unreadable world, makes a
  new one and writes it (the format-2 world survives one save as world.save.json.bak). Do not run an older build after this one.
- Back up the whole `CrulandaEncounter` folder (every character save, `profile.save.json`, `world.save.json` and their `.bak` files)
  to `work\save-backups\<date>-pre-p9-exchange` before the first run of this build. The character format (10) is unchanged.
## The world slot, format 3 (2026-10-19, wb-reach; WORLD_BOSS_1.md build step 3): the world boss's timer
`WorldSave.FormatVersion = 3`. One new field at the end, `unraveller` (`WorldBossState`, WorldBoss.cs): `state` ("waiting", "warned",
"up"), `played` (play-seconds since her last kill or sinking, any zone, paused time not counted), `riseAt` (the play-second of this cycle
she rises at: 3 hours and a moment of the next 30 minutes, -1 until rolled), `upFor` (seconds she has been up), `wipes` (this rising),
`cycles` (risings ended: the stream each moment is drawn from), `kills`.
- Migration 2 → 3 is `AddWorldBossMigration` (in `SimAdventurers.cs`), run by `SimRoster.Load` after 1 → 2: it inserts `"unraveller":{}`
  before the closing brace and leaves every other character (the sims, the party, the guilds, the Exchange) as it was. An empty timer
  reads as waiting with nothing played: an older world starts its first 3 hours on its first play.
- An older build refuses a format-3 world (as with format 2). Do not run an older build after this one.
- Back up the whole `CrulandaEncounter` folder to `work\save-backups\<date>-pre-world-format-3` before the first run of a build with it.
  The character format (11) is unchanged.
## Format 11 (2026-10-15, Phase 8.6 step 5): found recipes
One new list, for the found recipes (Docs/PHASE8_6_LIVING_WORLD.md 3c; one format bump, one migration):
- `recipes`: `List<string>`, the found recipes learned from a leaf (an item of kind `recipe` whose `teaches` names the recipe;
  `ProfessionLog.UseLeaf`), by recipe id, in the order learned. A taught recipe (bars, charcoal, the plain pieces) is never listed: it
  follows from the skill, as before. A found recipe not listed shows in the Trades window as a grey "?" with its trade and skill.
  - A leaf is used up when it teaches. A second leaf for a recipe already listed is refused and kept ("You know that one already.").
  - Forgetting a craft keeps its leaves learned: the list is not touched by `ProfessionLog.Forget`.
  - On load (`EncounterSave.Read` and `ProfessionLog.Bind`): blank ids are dropped, and so is a later entry for an id already listed.
    An id this build's content doesn't know is kept in the save and ignored, so content can change.
  - Where it sits: `EncounterProgress` declares it before `discoveries` (with `usedInteractables` before it), not at the end, so a
    written payload still ends `...,"lootLuck":[...]}` as formats 9 and 10 did. The order of a JSON object's fields means nothing
    to the read.
- Migration 10 → 11 is `AddRecipesMigration`, a SaveMigrator step of the same kind as 6 → 7.
  - It inserts `"recipes":[]` before the payload's closing brace and leaves every other character of the v10 payload as it was. A
    payload that already has the list is left alone. One that isn't a JSON object is refused: the save isn't loaded or changed.
  - A format-6 save runs all five steps (6 → 7 → 8 → 9 → 10 → 11) in one read. `SaveMigratorTests.V10Payload_MigratesTo11_...` takes
    the owner's format-8 fixture (`OwnerSaveFixtures`) to format 10 and through the step and the save: every other field comes through
    as it was and the list is empty.
  - What to expect on Chris's save: `recipes` appears, empty; nothing else changes. Back up the whole `CrulandaEncounter` folder
    (every character slot, the `.bak` files and `profile.save.json`) to `D:\crulanda-work\save-backups\<date>-pre-format-11` before the
    first run of this build (CLAUDE.md: back up Chris's save before any save-format change). Not yet done on 2026-10-15: the p86-craft
    helper does not run the game.
- Formats 1-10 load with the list empty.
- Not saved: the consumables' buffs (well fed, a tincture, a salve; Phase 8.6 step 4). They ride the status effects, so a load, a
  fall or a zone change starts the character without them.
- An older build refuses a format-11 save (its version is above theirs) and leaves the file alone.
- As before, migration happens in memory on read. The file is written as format 11 on the next save.

### Also format 11 (2026-10-15, branch p86-lockout): lockouts
Chris, 2026-10-15: "bosses/elites will not respawn so no one is trying to exploit the gear/loot drops" ("Until a lockout resets",
"Everywhere"; "every 2 days for loot"; "open world boss is once per week"; "raids once per week"). One more list, in the same format
and the same step as the recipes (no format 12):
- `lockouts`: `List<Lockout>` (`id`, `until`): each boss or elite this character's party has killed and may not loot again yet.
  - `id` is the camp's key, the mob's id without its last part (`mob.captain.peaks.3` from `mob.captain.peaks.3.0`; the Rock-Eater's
    own `mob.rockeater.adit.m21`), as the deeds' `elitesSlain` keys are. `until` is when the lockout ends, as UTC ticks
    (`DateTime.Ticks`).
  - How long (`Lockouts.Kinds`): a raid zone's (`ZoneDefinition.raid`, none built yet) and an open-world boss's (an elite with a boss's
    move) a week, a dungeon's bosses and elites two days, any other open-world elite a day; each ends at 06:00 local time (the first
    six o'clock after the kill, then a day for each further day). A kill inside a lockout does not push it back.
  - On load (`EncounterSave.Read`, `Lockouts.Normalize`): a blank id, an `until` of zero or less, and a later entry for an id already
    listed are dropped (the first is kept). A lockout that has run out is kept until the session prunes it (on each zone's load and
    every few seconds while playing). An id this build's content doesn't know is kept.
  - Where it sits: `EncounterProgress` declares it after `usedInteractables` and before `recipes`, so a written payload still has
    `"recipes":[...],"discoveries":[...]` together and still ends `...,"lootLuck":[...]}`.
- Migration 10 → 11 (`AddRecipesMigration`) now inserts `"recipes":[]` and then `"lockouts":[]` before the payload's closing brace, each
  only when it is missing (as `AddArmouryMigration` does for its three), and leaves every other character as it was. A payload with both
  is left alone. `SaveMigratorTests.V10Payload_MigratesTo11_...` and `LockoutTests` check it.
- What to expect on Chris's save: `lockouts` appears, empty, next to `recipes`; nothing else changes. The same backup as for the recipes
  (`<date>-pre-format-11`) covers it.
- Formats 1-10 load with the list empty. A format-11 save written by the p86-craft build (recipes, no lockouts) also loads with it empty.
- Not saved: the dungeon queue's run (which dungeon, which sims it brought: kept in memory across the dungeon's load only) and which
  bosses are put away this visit (that follows from the list).
