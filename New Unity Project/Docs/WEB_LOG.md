# Web session log

One entry per `web/*` branch, newest first: the date, the branch, what changed, the exact test fixtures to run locally, and
what needs eyes in the game. The local session ticks each entry off when it has tested and merged the branch.

<!-- Example:
## 2026-10-09 web/adit-quests (PR #1)
- Added the seven Sealed Adit quests (Quests/adit.json) and their loot.
- Run: EditMode QuestDataTests, LootDataTests; PlayMode AditTests.
- Look at: the quest givers' places in the Peaks.
- [ ] tested and merged locally
-->

## 2026-10-08 web/adit-quests-loot (draft PR)
WEB_HANDOFF task 1: the Sealed Adit's seven quests (DUNGEON_DESIGN.md 6) and its loot (section 7). Not compiled or run: no Unity here.
- **Changed:** `Quests/adit.json` (+ `.meta`, new guid), four residents (`Zones/peaks.json`: Pib, Brother Cael, Lisle Tamber;
  `Zones/khaven.json`: Tamsin Rook), `Items/loot.adit.json` (the Adit-Runner's Kit, five pieces, and The Dead Line's three rewards),
  `ZoneBuilder.Adit.cs` (five usable cages, seven echo-jars), `tools/art/make_icons.py` (five quest-item entries), `LootIdBaseline.txt`.
- **Icons are not in the branch:** the cloud can't upload Git LFS objects (403). Run `python tools/art/make_icons.py` first: it paints
  the 13 new ones (5 quest items, 8 gear) with the tool's own deterministic `.meta` guids (`--check` reports 0 missing here), and
  `IconCoverageTests` fails until it has run. Commit the 13 PNGs and their `.meta` files.
- **Run first:** `python tools/art/make_icons.py`, then Crulanda > World > Build Oakhaven (it registers `Quests/adit.json` in Encounter.asset), then
  - EditMode: `QuestDataTests`, `QuestLogTests`, `LootDataTests`, `LootRollTests`, `ArmouryTests`, `IconCoverageTests`,
    `HouseholdDataTests`, `DiscoveryLogTests`, `BountyTests`;
  - PlayMode: `ZoneContentTests` (givers, the cages and jars, the kill targets, per zone), `AditTests`, `NamedLootTests`,
    `TalkReachTests`, `VillageDayTests`.
- **Look at in the game:**
  - the two new cages on the right wall of the Workings' first chamber (they must not pinch the way or the navmesh), and the seven
    echo-jars round the Gallery's floor (not inside dripstone; reachable; taken jars vanish and come back);
  - where the new people stand: Pib behind the Pilgrims' Rest by the barrels (-23, 1), Lisle Tamber at (-6, -4), Brother Cael below
    the Listening Shrine (6, 140), Tamsin Rook by Khaven's gallows tree (7, -3). **Pib is a goblin but spawns as a human villager
    (role stranger)**: there is no goblin figure yet.
- **Not done, and why:**
  - **Quest gear is never handed out.** No code grants any `quest:` gear at turn-in (the 15 older quest pieces neither); The Dead
    Line's "choice of three" needs a pick-one reward and a window. Question for Chris below.
  - The Weaver's unlocking (D4) is not a step of The Dead Line yet. Adding a step later moves saved quests' step numbers: put it
    before the kills, or give it its own quest.
  - The Pressed doesn't yet open the quiet gate (D3 should read `questsDone` for `side.adit.pressed`).
  - Geode Shards (trash material and a smithing recipe) and the hidden and rare bosses' pieces (Old Kettle, the Quiet Miner: not
    in the zone yet) are left for later.
  - Letter text: the Sandthrone commander's CANON name is left out (PROVISIONAL); put it in from the books.
  - Achievements: the quests' `zone` is where they are given, so "The Deeds of the Shattered Peaks" now wants the six Peaks Adit quests and
    Khaven's wants Embers Below. Achievements.cs expected the dungeon to get deeds of its own; decide which.
  - `Items/loot.adit.json` and `Zones/adit.json` have no `.meta` in git (from the local WIP commit a14c39b). They were not added
    here so they don't clash with the local ones: commit the local `.meta` files.
- **For Chris:** build quest gear rewards (a choice of one, at turn-in) as the next cloud task? It would hand out all 18 quest pieces.
- [ ] tested and merged locally
