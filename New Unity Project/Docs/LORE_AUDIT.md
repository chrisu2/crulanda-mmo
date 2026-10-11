# Lore audit (2026-10-10, for Chris's decisions)

A review found game labels and lore that do not match the books. Each item below is checked against `D:\code\crulanda`. **Nothing in
the game has been changed for these yet:** each waits for Chris's word. (The one change made: the Adit's item texts lose their
machine words, which Chris approved: "engine oil" became "drill-grease".)

## Rule, from Chris (2026-10-10)
- **No machine words in player-facing text** (engine, machine, machinery, motor...): use in-world words ("drill-grease", "the
  Rock-Eater's berth"). Code comments and design notes may still name things plainly.

## 1. The Salt-Menders: labelled CANON, but they are not what the books say
- Game (`EncounterContent/Quests/factions.json`, faction `saltmenders`, CANON): "Rebels of the Alliance who meet in old tunnels and mean
  to 'break the machine'. They move people the Concord wants."
- Books: book 3 ch.2-3 (and `world_bible.md` line 63): the refugee community on the Western Coast after the exodus, building the
  Salt-Wall, "mending" the world with the salt-magic of the West; Klyther, Tynara and Elara's followers among them.
- Also a machine word in player-facing text ("break the machine").
- **Decide:** rewrite the faction to the books (they cannot exist yet in the game's time, before the exodus: a forerunner group?), or
  keep it and label it GAME-ONLY under another name.

## 2. The Whispering Wood: placed and described against book 3
- Game: a dead wood beside Khaven, with grey wolves (`Quests/khaven.json`, `Quests/bounties.json`; GAME_BRIEF, WORLD_ZONES).
- Books: book 3 ch.2 "The Whispering Woods": the living, singing forest on the Western Coast of the Veridian Keepers, the forest
  the game builds as the Verdant Shore.
- **Decide:** rename Khaven's wood (it is GAME-ONLY then) and give the name to the Verdant Shore's forest, or keep both and label
  Khaven's GAME-ONLY.

## 3. The Sandthrone toll on the Peaks: sourced (the review was wrong here)
- `chapter_5.md` ("The Geography of Division", a ~1,800-word draft): "The Peaks are crawling with Sandthrone mercenaries," Korath
  warned. "And they're charging a toll." Plus `world_bible.md` "Sandthrone (Antagonists / Mercenaries)".
- **Decide:** CANON (if chapter 5 is canon) or CANON-EXPANDED (if it is a draft).

## 4. Klyther: the books' own notes disagree
- `book1/world_bible.md` 30: Klyther Forgeheart, Lead Engineer, gruff, pragmatic, loyal to Kaelen.
- `world_bible.md` 32 and 63: Klyther Forgeheart (Tinker), mechanical genius (drones, scorpions), among the Salt-Menders.
- `book1/series_bible.md` 137: status UNKNOWN, fled via chute, likely surviving in the Ashlands.
- `ledger_of_souls.md` 25: Klyther Void-Singer, Independent, alive, "Bad": a techno-cultist, villain of the Verdant Shore.
- Book 3 ch.6: he treks to the Temple with Elara, Kai and Aris (an ally).
- **Decide:** which is true for the game (the game does not use him yet, so nothing breaks either way).

## Still to do
- `CRULANDA_CANON.md` is a stub from Phase 0: fill it from the books once the decisions above are made.
- A full pass over every CANON label in the content (zones, quests, items) against the books, item by item.
