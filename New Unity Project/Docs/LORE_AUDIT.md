# Lore audit (2026-10-10, for Chris's decisions)

A review found game labels and lore that do not match the books. Each item below is checked against `D:\code\crulanda`. **All four were decided by
Chris on 2026-10-10** and are done in the game data (below). (The one change made: the Adit's item texts lose their
machine words, which Chris approved: "engine oil" became "drill-grease".)

## Rule, from Chris (2026-10-10)
- **No machine words in player-facing text** (engine, machine, machinery, motor...): use in-world words ("drill-grease", "the
  Rock-Eater's berth"). Code comments and design notes may still name things plainly.

## Which source wins: DECIDED (Chris, 2026-10-10: "Novels, bible, ledger")
The canon sources disagree with each other (see Klyther below), so every check needs an order. Proposed: **the novels' text first**
(book chapters), **then the world bible** (`world_bible.md`, `book1/world_bible.md`), **then the ledger** (`ledger_of_souls.md`, and the
series bible's status tables). A label says which source it rests on.

## 0. The classes have no canon label
- The class data carries no canonStatus; the only labels are code comments (the Archivist's "CANON roots", the Paladin's Vanguard
  "CANON-EXPANDED").
- Druid, Paladin and Ranger appear nowhere in the bibles; Rogue, Mage, Warrior and Archivist appear as words (not yet checked in
  context).
- **DECIDED (Chris, 2026-10-10: "Yes, GAME-ONLY"):** every class is GAME-ONLY as an archetype, noting a CANON root where one exists
  (the Archivist's Silent Pilgrims and echo-jars; the Cantor's Song of Mending). Recorded in CRULANDA_CANON.md (the class data has no
  label field).

## 1. The Salt-Menders: labelled CANON, but they are not what the books say
- Game (`EncounterContent/Quests/factions.json`, faction `saltmenders`, CANON): "Rebels of the Alliance who meet in old tunnels and mean
  to 'break the machine'. They move people the Concord wants."
- Books: book 3 ch.2-3 (and `world_bible.md` line 63): the refugee community on the Western Coast after the exodus, building the
  Salt-Wall, "mending" the world with the salt-magic of the West; Klyther, Tynara and Elara's followers among them.
- Also a machine word in player-facing text ("break the machine").
- **DECIDED (Chris, 2026-10-10): forerunners.** The game's Salt-Menders are the forerunners of book 3's (CANON-EXPANDED): folk of the
  Alliance who meet in old tunnels and mend what the Concord breaks; their name goes west with the refugees in years to come. Done in
  factions.json and the Adit quest's label; "break the machine" gone.

## 2. The Whispering Wood: placed and described against book 3
- Game: a dead wood beside Khaven, with grey wolves (`Quests/khaven.json`, `Quests/bounties.json`; GAME_BRIEF, WORLD_ZONES).
- Books: book 3 ch.2 "The Whispering Woods": the living, singing forest on the Western Coast of the Veridian Keepers, the forest
  the game builds as the Verdant Shore.
- Chris's own Khaven Village map (`khaven_village_map.png`) labels the wood "Whispering Wood": the clash is between the map and book 3.
- **DECIDED (Chris, 2026-10-10): rename anyway; book 3 wins.** Khaven's dead wood is **the Stillwood** (GAME-ONLY), in the zone, quests,
  bounties and docs; the map's label is noted as superseded (CRULANDA_LOCATIONS). The Verdant Shore's subtitle names "the Whispering
  Woods" beside "the Verdant Ocean", as book 3 does.

## 3. The Sandthrone toll on the Peaks: sourced (the review was wrong here)
- `chapter_5.md` ("The Geography of Division", a ~1,800-word draft): "The Peaks are crawling with Sandthrone mercenaries," Korath
  warned. "And they're charging a toll." Plus `world_bible.md` "Sandthrone (Antagonists / Mercenaries)".
- The finished Book 1 never names the Sandthrone (its only "toll" is a rat's in the sewers); the Peaks toll is only in the draft
  `chapter_5.md`.
- **DECIDED (Chris, 2026-10-10): CANON-EXPANDED.** Every label that cited "book1 ch.5" now cites the draft chapter_5.md (quests: Adit,
  Khaven, Peaks; the Peaks zone note and its letters).

## 4. Klyther: the books' own notes disagree
- `book1/world_bible.md` 30: Klyther Forgeheart, Lead Engineer, gruff, pragmatic, loyal to Kaelen.
- `world_bible.md` 32 and 63: Klyther Forgeheart (Tinker), mechanical genius (drones, scorpions), among the Salt-Menders.
- `book1/series_bible.md` 137: status UNKNOWN, fled via chute, likely surviving in the Ashlands.
- `ledger_of_souls.md` 25: Klyther Void-Singer, Independent, alive, "Bad": a techno-cultist, villain of the Verdant Shore.
- Book 3 ch.6: he treks to the Temple with Elara, Kai and Aris (an ally).
- **DECIDED (Chris, 2026-10-10): follow the novels.** Klyther Forgeheart, an engineer and an ally (book 3); the ledger's "Klyther
  Void-Singer" villain is set aside. Nothing in the game uses him yet.

## Still to do
- `CRULANDA_CANON.md` is a stub from Phase 0: fill it from the books once the decisions above are made.
- A full pass over every CANON label in the content (zones, quests, items) against the books, item by item.
