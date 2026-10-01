# Quests — design (draft 2026-09-29)

Status 2026-09-29: the **framework is implemented** (section 7). Oakhaven has 10 quests: Chronicle I and II, six NPC quests
and two faction introductions. The Hollow Men, the well at night, Khaven content and Sandthrone contracts are still design only.
2026-09-30: quests can wait for a level (`minLevel`, section 7), and Oakhaven has its first side quest, **The Tin Crown**
(Crowsfoot Hollow, level 4, from level 3; section 5). The same night Crowsfoot Hollow became the first dungeon, 16 m deep.
Lore sources: `D:\code\crulanda` (world_bible.md, series_overview.md, book1\chapter_4.md, chapter_5.md, chapter_20.md,
ledger_of_souls.md). Labels:
- CANON: stated in the books.
- CANON-EXPANDED: built on canon facts.
- GAME-ONLY: invented for the game.

## 1. What canon gives us for the east
- **The Oakhaven atrocity (CANON, book1\chapter_4.md):**
  - The black-iron wagon of the High Council Investigation Bureau held child manacles.
  - Its ledger reads: "Total Population (Pre-Operation): 240 Souls", "Viable Resonance Candidates Positively Identified: 3", and a Level 4 **Void-Seed** planted in the water supply to force a local Wasting "to hide our tracks".
  - The grey advanced in a perfectly straight line. The villagers became **Hollow Men**, sustained by a violet crystal in the well.
- **The children (CANON):** they "sparked", were chained together and marched away, and went to feed the **Spoke**. Veyra's
  seven-year-old brother (probably Jace) was taken first.
- **Factions near the east (CANON):**
  - High Council / Concord: Seekers, Praetorians, the Investigation Bureau.
  - Iron Pact (Ironhold).
  - Sandthrone mercenaries, who charge tolls in the Shattered Peaks and are "often employed by the Concord but with their own agenda".
  - Ash-Walkers: salt against Weave-Eaters, in caves on the Eastern Ridge.
  - Preservationist Guild: Vala Solari and her Wardens.
  - The Alliance / Salt-Menders: rebels who meet in the old tunnels.
- **No canon exists for Khaven**, only its map. Its story is ours to write.
- **Canon conflict to settle:**
  - Oakhaven's tavern is the **Golden Cask**, as in the book. The game used "Whispering Barrel" (from the village map) until 2026-09-29, when Chris chose the book's name.
  - "Pale Things" appears once; the books say **Pale Kings**.

The player is not Veyra. The game's hero arrives in Oakhaven in the days before it is erased and uncovers what Veyra
later finds in the ruins. This echoes canon without replacing her story.

## 2. Quest kinds
| Kind | Where it comes from | Marker | Notes |
|---|---|---|---|
| Main (Chronicle) | Story NPCs, chained chapters | gold ! / ? with a crown | Can't be abandoned. Zone-to-zone spine. |
| Side | Anyone with a story | gold ! / ? | One-offs and short chains. |
| NPC / trade | Villagers by trade (smith, baker, hen-wife...) | gold ! / ? | Tied to that NPC's routine and workplace; can repeat daily. |
| Faction | Faction contacts | blue ! / ? | Earn reputation; chains unlock by standing; some factions oppose each other. |
| Notice board | Board by the inn | parchment icon | Bounties and odd jobs (repeatable). |

- Greyed ! means "too low level".
- Markers appear over heads and on the minimap and zone map. Objective areas are drawn as circles on the maps.

## 3. Objectives (data-driven, like zones and talents)
Quest JSON in `EncounterContent/Quests/*.json`. Each quest has an id, title, kind, giver, turn-in, level, an optional
minLevel, zone, and requires (quests / reputation / time of day). It has an ordered list of **steps**; each step holds one or more objectives. Rewards are xp, coin,
items, reputation and unlocks. Text: offer / progress / complete, plus canonStatus.

| Objective type | Example |
|---|---|
| `talk` npc | Ask Goody Marl about the missing hens |
| `kill` enemy tag x N | Drive off 5 Concord collectors |
| `collect` item x N from source (loot / node / critter / prop) | 5 yarrow from the meadow |
| `deliver` item to npc | Take the eggs to Hedda Thorne |
| `visit` place / landmark / radius | Walk the straight grey line |
| `interact` prop | Search the black-iron wagon |
| `herd` critters to target | Shoo 3 stray hens into the coop |
| `escort` npc to place | Walk the Harrow boy to the mill |
| `at time` (modifier) | ...at night (the well glows violet after dark) |

The runtime is a **QuestLog** in the save (format 4): active quests and step progress, completed ids, reputation, coin and chronicle
entries. Gameplay raises events (EnemyKilled, Talked, Collected, Visited, Interacted, CritterHerded) and the log advances.

## 4. Interface
- **Quest dialog:** a parchment window with the NPC's text, the objectives and rewards, and Accept / Decline or Complete. It opens with E.
- **Quest book (L)** has four tabs:
  - **Quests:** grouped by zone and kind, with details and track / abandon buttons.
  - **Chronicle:** completed story, plus documents found, such as the ledger page as a readable item. Pages found as secrets land here too.
  - **Reputation:** each faction's standing on six tiers: Hostile, Distrusted, Neutral, Trusted, Honoured, Sworn.
  - **Discoveries** (2026-09-30):
    - each zone's hidden finds, found out of total, with the current zone first;
    - the found ones by name and text; the rest only as "N more lie hidden in X";
    - secrets are on no map (the data is `ZoneSecret` in the zone files; the logic is `DiscoveryLog`, save format 7).
- **Tracker:** up to 5 tracked quests replace the current three fixed lines.

## 5. Oakhaven content (first pass)
### Main — Chronicle I: "Two Hundred and Forty Souls" (CANON-EXPANDED)
1. **Find Mira** at the inn (existing).
2. **Drive off the collectors** (existing). Mira: "They're guarding something by the east road."
3. **The black-iron wagon.** Search the Bureau wagon near the grey (new prop) and find the child manacles and the **ledger**:
   240 souls, 3 resonance candidates, and a Void-Seed "to hide our tracks". The ledger goes into the Chronicle.
4. **Three names.** The ledger lists the candidates by initials. Ask the families (the Harrows already say the collectors took their
   girl), the elder and the gossips. The first entry is a seven-year-old boy who "sparked". GAME-ONLY names for the other two.
5. **What's in the well.** The elder says the water "went bitter". At night the Communal Well glows violet. Descend and break
   the Void-Seed crystal while **Hollow Men** (stone-grey villagers who blink and whisper "Hungry...") rise around it. CANON creature.
6. **The ruts go west.** The wagon tracks lead along the West road to Khaven, where Sandthrone outriders took the children over. This is
   GAME-ONLY and hands off to Chronicle II in Khaven.
The game is set **before** Oakhaven is erased (Chris, 2026-09-29), so the village stays standing. The erasure is not
shown. The main quest can foreshadow it (the Void-Seed and the straight grey line) but never plays it out.

### NPC / trade quests (GAME-ONLY, each uses the living-village systems)
| Giver | Quest | Uses |
|---|---|---|
| Goody Marl, hen-wife | *Count Them In*: three hens bolted at dusk; herd them back before she shuts the coop | herd, time of day |
| Goody Marl, then Hedda Thorne | *A Dozen for the Oven*: carry the day's eggs to the bakehouse | deliver, trade link |
| Hedda Thorne, baker | *Ash in the Flour*: sample the flour, have the herbalist test it, bring clean grain from Harrow | talk, deliver, collect |
| Brannoc Vell, smith | *Good Iron*: recover the confiscated iron crates from the collectors' camp; reward: reforged blade | kill, collect, item reward |
| Garet Moss, hunter | *What the Deer Know*: follow tracks that stop dead at the grey | visit, lore |
| Lisbet Crane, herbalist | *Yarrow for Mira*: gather herbs on the meadow (new gather nodes) | collect nodes |
| Osk Farrow, woodcutter | *Grey from the Heart*: mark grey-hearted trees in Harrow wood before they fall | interact |
| Aldo Crisp, miller | *The Wheel Won't Turn*: clear the drift jamming the water wheel | interact |
| Corwin Ashby, elder | *Sleep Now, Stone and Sky*: collect the verses of the old Oakhaven lullaby (CANON lullaby) | talk chain, Chronicle |
| Pim and the children | *Hide and Seek*: find the three hiding children | visit |

### Side quest: *The Tin Crown* (GAME-ONLY quest; CANON faction, the Sandthrone)
Chris's brief: "a quest appears at lvl 3 to take out the bandit and bandit king in the cave so they stop harassing the villagers".
- **Crowsfoot Hollow** (GAME-ONLY) is the walk-in cave where the North road ends in the hills north of the village, and the
  game's first dungeon (Chris: "the cave should be deep and the first foray into dungeon crawling"): lookouts at the mouth,
  a camp of **Sandthrone deserters** in the first chamber, sentries down the Drop, **Quartermaster Hesk** (elite) and his
  guards in the Store Caves, a watch on the Deep Stair, and sixteen metres down, the Echoing Hall of **Caddock, the Bandit
  King** (elite) and his guard. Levels 3-5. The Sandthrone are the canon mercenary company that holds the Peaks toll; this
  band walked away from it and lives off Oakhaven's farms. Layout: `WORLD_ZONES.md` "Caves you walk into".
- **Caddock** is GAME-ONLY: a deserter sergeant who crowned himself with beaten tin. No canon Sandthrone name is used.
- **Giver and turn-in:** Wil Carder, the farmer whose barn they emptied (Garet Moss tracked them to the hollow).
- **Level 4, minLevel 3:** nothing shows before level 3, then a gold !. (It was level 3 until the hollow went deep; level 4
  sits in the middle of the dungeon's 3-5.)
- **Steps:**
  1. Kill 6 deserters and Caddock, in either order.
  2. Search the deserters' plunder in his hall.
  3. Return to Wil.
- **Rewards:** 190 XP, 25 gold, +300 Oakhaven Folk. The dungeon pays too: Caddock always drops **Caddock's Tin Crown**
  (head), and the Quartermaster's strongbox (a secret; its key hangs on the Drop) holds **Hesk's Shuttered Lantern** and
  a Chronicle page, **By Order of the King**.
- **Looks:** the deserters wear the company's sand gone to dirt: a torn tabard over company mail, a hood or head-wrap, an ochre
  scarf over the face, one leather pauldron and one mail sleeve, a falchion or a club. Each is put together differently.
  Caddock wears a long dark coat, the torn company sash, a crooked crown of beaten tin, and carries a two-handed cleaver.
- **Villager talk** (`VillageLife.HollowQuest`): Oakhaven folk mention the raids from Crowsfoot until the quest is done,
  and then thank you for it.

### The Verdant Shore (levels 11-13; CANON place, GAME-ONLY quests)
The Chronicle runs `main.ashrim.5` -> `main.verdant.1` -> 2 -> 3 -> 4 -> 5 (XP and gold follow the Rim's curve, +40-60 XP a level).
- **The Old Salt Road** (`main.ashrim.5`, Chieftain Grohl, after Heart of the Brood): the Rim's salt-pans are greying, so Grohl sends
  you west over the ash-mountains with an Ash-Walker salt-cord, to ask the wood-folk for the salt his grandmothers took from the
  western shore. Turned in to Willow-Whisper.
- **Let the Wood Learn You** (11): Willow-Whisper has the wood learn you: the falls, the glade, the mere. **The Briar Way** (12):
  Oak-Bane has you cut the briars off the old way to the Veridian Temple. **Grey at the Heart** (12): the Greying, withered Keepers
  and Greyheart. **A Fog That Tastes of Lightning** (13): Palemist Hollow, the mist-walkers and the pale shadows: the Pale's touch
  reaching the Shore (PROVISIONAL: Book 3's fog, foreshadowed). **The Root-Mother's Deep** (`main.verdant.5`, 13, after the Fog):
  the fog came up out of the ground; Willow-Whisper sends you down the Temple's root-stair the living roots closed, into the
  dungeon under the Temple (`WORLD_ZONES.md`, "Caves you walk into"): visit the Root Gallery, lay five withered Keepers and three
  mist-walkers to rest, bring down the Hollow Root-Warden (elite 13; he was Thorn-Hand, who planted the Guest-Tree) in the Heart,
  salt the cold in the root ("The cold in the root", usable, with any salt: Grohl's, the First Shore's) and return. 820 XP, 200
  gold, Keeper standing 600, and a fifth page, *What the Root-Mother Dreams* (Sister Iselle, from Willow-Whisper's lips: the
  Pale's king who wears a mirror, named the way you would name a disease). Willow-Whisper's last words foreshadow Book 3's
  survivors coming over the ridge, without playing it out.
- **Side and NPC quests:** The Reeds Go Quiet (Reed-Song), Antlers for the Carver (Alder-Knot), Silk for the Lanterns (Moss-Lantern),
  The Three Notes (Sister Iselle, a Silent Pilgrim), Green Gold (Ondine Varro: gold at the cost of Keeper standing), The Old King of
  the Deep Wood (Oak-Bane; Old Ninebranch), Salt of the First Shore (carried back to Mother Vane on the Rim).
- **A new faction**, the Veridian Keepers (`keepers`, CANON), with standing from the quests. Five Chronicle pages.
- Two named vendors: Moss-Lantern (sap-cakes, dewfern) and Ondine Varro.

### Faction introductions (CANON factions, GAME-ONLY quests)
- **Oakhaven Folk** (village standing): earned from the NPC quests. Raises prices at the stalls, opens a room at the inn, and gets villagers to share rumours.
- **Salt-Menders (the Alliance):** a quiet stranger at the inn asks you to spoil the collectors' supplies. Their chain works against the Concord.
- **Preservationist Guild:** a Warden in Harrow wood (green and brown, living-vine armour) asks you to save living things from the grey.
- **Ash-Walkers:** a salt-scout at the Wasting edge teaches salt against Weave-Eaters. Leads to the Eastern Ridge (future zone).
- **Sandthrone:** in Khaven, an outrider captain offers paid contracts. Mercenary work that costs Salt-Menders standing: a real choice.
- **High Concord:** always hostile; no reputation to earn.

## 6. Build order
1. **Framework:** quest data and validation, QuestLog and save v4, events, dialog, ! / ? markers, quest book (L), tracker, reputation, coin.
   Port today's three objectives into Chronicle I steps 1-2.
2. **Oakhaven content:** Chronicle I steps 3-6, four NPC quests, and two faction intros. Also new pieces: the wagon prop, the ledger document, herb nodes, Hollow Men, and the well at night.
3. **Khaven:** Chronicle II, the Sandthrone contract chain, and the smith's Khaven iron.
4. **Later:** notice boards and repeatables.

## 7. Implemented (2026-09-29)
**Code**
- `Scripts/Encounter/Quests.cs` holds the data classes plus:
  - `QuestDatabase`: merges every file in `EncounterContent.questFiles` and validates them, listing every problem.
  - `QuestLog`: pure logic, EditMode-tested.
- `EncounterSession` wires in events:
  - Kill, talk, deliver and interact.
  - Visit, checked every 0.5 s.
  - Flags: `recruited` and `equipped:<item>`.
  - `ReconcileQuests()` catches new steps up with what the character has already done.
- **Level gates** (`QuestLog.For`, `Marker`, `Accept`):
  - A quest is offered from 3 levels below its `level`, with a grey ! until then.
  - An optional `minLevel` hides it completely below that level: no offer, no ! of any colour, and `Accept` refuses it.
  - Without `minLevel`, a quest behaves as before.
  - `minLevel` must be 0-10.
- A kill or flag objective's `say` line is narration. The chat no longer prints the enemy id as if it were the speaker.
- UI lives in `EncounterHud.Quests.cs`:
  - Tracker for up to 5 quests, with the Chronicle first.
  - Gold or grey ! / ? over heads, and on the minimap and zone map. Quest places show as gold rings.
  - Parchment conversation window with Accept / Decline / Complete.
  - Quest book on **L** with Quests (track, abandon), Chronicle (pages to read) and Standing (six tiers) tabs.

**World and save**
- Interactable props: any zone prop with `interact` (a prompt), optional `item` (a quest item) and `once`.
  - New prop kinds: `wagon` (the Bureau's black-iron wagon) and `herb` (yarrow and similar).
  - Items are only given while a quest wants them.
  - Crates stay empty once used; herbs regrow after 90 s.
- Residents (`life.residents`): named NPCs with a fixed post who don't sleep. Oakhaven has two:
  - **Quill**, the Salt-Mender contact at the Cask.
  - **Warden Ivel**, of the Preservationist Guild, at the edge of the Harrow wood.
- Khaven now has its own villager names.
- Save format 4 adds quests, questsDone, reputation, documents, questItems and usedInteractables.
  - Older saves load with these empty, and the Chronicle catches up on load.

**Content** (`EncounterContent/Quests/`)
- `factions.json`: Oakhaven Folk, Salt-Menders, Preservationist Guild, Ash-Walkers, Sandthrone, and High Concord (fixed Hostile).
- `oakhaven.json`, 11 quests:
  - Chronicle I, "Two Hundred and Forty Souls": Mira, the collectors, the blade, the wagon and ledger, then Corwin.
  - Chronicle II, "The Miller's Boy".
  - NPC quests: A Dozen for the Oven, Ash in the Flour, Yarrow for Mira, What the Deer Know, Good Iron, and Sleep Now, Stone and Sky.
  - Faction quests: Salt in the Tithe (Salt-Menders) and Roots Before Ruin (Preservationists).
  - Side quest: The Tin Crown (`side.oakhaven.crowsfoot`, Wil Carder, minLevel 3): Crowsfoot Hollow, see section 5.
- Chronicle pages: the Bureau ledger and the Oakhaven lullaby.

**Tests**
- EditMode: `QuestLogTests` (10).
- PlayMode: `OakhavenQuestTests`, which checks that every giver, target and interactable exists, and plays Chronicle I end to end.
- PlayMode: `HollowQuestTests` covers The Tin Crown:
  - the level gate;
  - that its kill targets are the hollow's camps, in their own looks;
  - that the plunder exists;
  - a full play-through;
  - that a mob doesn't notice you through solid scenery.

## 8. The road to level 10 (2026-09-29)
Each zone's last main quest is turned in to someone in the next zone. This makes the Chronicle a breadcrumb trail:
- **Oakhaven:** `main.oakhaven.3` "The Ruts Go West", level 2. Corwin sends you to Wenna Coyle in Khaven.
- **Khaven:** `main.khaven.1-3`, levels 3-5.
  - Break the Sandthrone Hold → What the Pale Thing Counts → The Toll Road North.
  - The last one is turned in to Maddoc Vire in the Peaks.
- **Peaks:** `main.peaks.1-3`, levels 6-8.
  - The Toll-Book → Under a Seeker's Seal → Salt of the First Sea.
  - The last one is turned in to Mother Vane on the Ash Rim.
- **Ash Rim:** `main.ashrim.1-4`, levels 9-10.
  - Salt Is Not Given → The Tear-Marked Shrine → Heart of the Brood → Salt for the Well.

Each zone also has NPC quests that send you into its camps, plus one faction quest in the Peaks (Sandthrone, "The Dead-Drop").
Content lives in `Quests/khaven.json`, `peaks.json` and `ashrim.json`.

When a tracked step's target is in another zone, the tracker shows "→ Travel to X (via Y)", and a gold ring marks the exit
to take (`EncounterSession.ExitToward`, `HudMaps.QuestMarks`).

**Next**
- Quest rewards that give real equipment: `reward.items` exists in the data but nothing grants it yet. Gear comes only from camp loot and merchants.
- The well at night and the Hollow Men (new enemy look), finishing Chronicle II.
- Khaven: Chronicle III and the Sandthrone contracts, which cost Salt-Mender standing.
