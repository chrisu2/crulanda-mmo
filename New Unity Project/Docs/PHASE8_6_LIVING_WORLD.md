# Phase 8.6: the living world's second pass (PROVISIONAL)

Written 2026-10-15 from Chris's words of 2026-10-14 (PLAYTEST_NOTES 203-205; ROADMAP "Phase 8.6 plan"). Nothing here is built.
Every item is PROVISIONAL until Chris reads it; the open decisions are at the end, one line each. Lore is labelled as everywhere:
CANON, CANON-EXPANDED, GAME-ONLY, PROVISIONAL. No machines anywhere in it (CRULANDA_CANON).

## 1. The brief (Chris, verbatim)
- 203 (23:30) "so what is the phases look like? the villagers and npcs need better ai"
- 204 (23:35) "more things to do. more incentives for gathering/professions. make better gear. have gear change the way your character
  looks when equipping"
- 205 (23:40, asked what the villagers did) "running into each other and sometimes get stuck or delayed at door of inn..prob need double
  doors and inn a bit bigger so for people. need more / better loot obtained from crafting armor/weapons/potions/food buffs. find
  recipes/ book of lore/still havent found many secrets. all this for ALL Zones"

## 2. What exists today (by area, with the files)
**Villagers, sims and the inn's door.** `Scripts/Encounter/WorldLife.cs` (VillageLife, Villager: NavMeshAgent radius .3, speed 1.6,
stoppingDistance .4, avoidancePriority 60, Unity's default high-quality avoidance; states Travel/Activity/Hidden; `Arrived`/`Stranded`
only notice a partial path, never a body in the way), `VillageWork.cs` (shifts and errands by trade), `VillageEconomy.cs` (purses).
Sims: `SimFigure.cs` (radius .3, priority 80 "a sim steps aside"; `AtInn` walks to the door point and hides at 2.6 m, or when "jammed":
within 5 m and still for 2 s, the patch for note 98 "people get stuck in inn door"). The inn: `Scripts/World/ZoneBuilder.cs` `Inn()`:
size from the zone file (the Golden Cask 12x8, the Cracked Hearth 11x7), one plank door 1.6 m wide on one hinge, kept open; the
navmesh is baked with `NavMesh.GetSettingsByIndex(0)` (agent radius .5, `EncounterNavigation.cs`), so the walkable strip through the
door is about 0.6 m: **one body at a time, nobody can pass anybody**. Inside: a bar, three tables with three seats each (nine
seats), the hearth; the seat layout is written twice (ZoneBuilder.Inn and WorldLife line 613 "same layout as ZoneBuilder.Inn").
Villagers go to the seats through the door; sims only reach the door and vanish. `ZoneDoor.cs`: kinds house/rooms/home/inn; only the
inn's opens. Only Oakhaven and Khaven have an inn: the Peaks' Pilgrims' Rest is a barred `house`, the Rim's Wain's Rest a
`ruined_house`, the Shore's Hearth-Tree a `treehouse`, so `SimFigure.Inn` finds nothing there and sims never rest indoors in three zones.
Doors elsewhere: houses are barred (villagers hide at the door), the barn door is 3.4 m, the coop has a hen queue (`Coop.RampQueue`).

**Professions.** `Scripts/Encounter/Professions.cs`; data `EncounterContent/Professions/professions.json`: six trades (Mining,
Woodcutting, Herbalism to gather; Cooking free; Blacksmithing and Alchemy as crafts, two craft slots), 17 nodes in five tiers (skill
1/20/40/60/80: copper, bog-iron, adit ore, cinder, veridian; oak to ghost-oak; yarrow to dewfern), 51 recipes: Blacksmithing 36 (five
bars, twenty `craft.*` pieces of quality 2 at levels 1/8/11/14, five `temper.*` weapons of quality 3 needing a geode shard, the Heartwood
greatblade at 100), Alchemy 5 (all healing draughts), Cooking 10 (all heal over 10 s), Woodcutting 5 (charcoal). **Every recipe is
known the moment the skill reaches it**: no learned-recipe list in the save (format 10) and no recipe item kind (`Items.cs` kinds: gear,
consumable, material, junk, tool, bag). Consumables (`EncounterSession.UseItem`): a potion heals at once on a 30 s cooldown, food heals
over 10 s out of combat; **no buffs of any kind**. Vendors (`items.json` vendors) sell tools, vials, flour, salt, bags, two potions.
Hides (`hide.*`, `junk.*_hide`) are sold to the tanner's stock; the shepherds' fleeces (2026-10-14) are village stock, not an item.
Crafted gear is only metal (plate-ish) and only Blacksmithing makes gear: nothing for leather, cloth, bows, staves, wands, jewellery.
Named loot (`EncounterContent/Items/loot.*.json`): 18-26 named pieces a zone, three sets (the Deserter King's Due, the Adit-Runner's
Kit, the Vigil of the Root-Mother), the Armoury tab records them (save format 9). Crafted gear sits below that loot at every band.

**Secrets.** `Scripts/World/ZoneDefinition.cs` `ZoneSecret` (kinds vista, cache, note, herb, chest, key; xp, gold, item, document,
needs), `Scripts/Encounter/Discoveries.cs` (DiscoveryLog: found by standing on a lookout or E within 2.8 m; `Tally` shows the quest
book only a count per zone), `ZoneBuilder.BuildSecrets` (built after the map, so no marker; props "small and subtle"), WORLD_ZONES.md
"Secrets". Counts: Oakhaven 15, Khaven 8, the Peaks 8, the Rim 8, the Shore 6, the Sealed Adit 0, the Root-Mother's Deep 0.
**Nothing in the world hints at one**: no villager line, no notice, no sound, no glint; the Discoveries tab says "8 hidden" and no more.
The five rare herbs found as secrets (moonbell, widow's-lamp, frostbell, last-light, lantern-moss) are one-off consumables, not nodes.

**The book of lore.** The quest book (L) has Quests, Chronicle, Standing, Discoveries, Achievements, Armoury
(`EncounterHud.Quests.cs`, `EncounterHud.Discoveries.cs`). The Chronicle holds the main quest's chapters and 35 readable pages
(`documents` in `EncounterContent/Quests/*.json`, `Progress.documents`), each labelled with its canon line; pages come from quests and
secrets. There is no codex of places, people, creatures, herbs or ores, and no page found by reading something in the world (the
notice board is the only "read" prop, kind `board`).

**Gear that changes the look.** Built: all nine slots (head, neck, shoulders, chest, hands, legs, feet, main hand, off hand) are drawn
on the figure and rebuilt when the item changes (`ActorVisual.Gear.cs` `ApplyGear`; armour `ActorVisual.GearArmor.cs`, 1482 lines of
families; weapons `GearWeapons.cs`; model pieces `GearModels.cs`; `GearLooks.cs` and `Resources/Gear/looks.json` resolve
family:variant/palette from the item). Sims and villagers are dressed the same way. What Chris does not see change: **helms are off by
default** (Esc > Display "Show helms", note 88, until Phase 8.5 fits them); generated gear at a band shares one family per piece word, so
most pieces of a level look alike; low-tier chest, hand and foot pieces are recolours of the body more than shapes; the crafted kits are
tints (`looks.json` tints: craft.copper_ ... craft.heartwood_) on ordinary families, so a crafted set does not read as a set; no back,
belt or wrist slot. (The level bands were re-cut for the cap of 15 in round 29, `GearLooks.BandOf`: 1-5, 6-9, 10-12, 13-14, 15;
only the comments in GearLooks.cs lines 29, 63 and 332 still say 1-2 ... 11-13.)

## 3. The plan, by area

### 3a. The villagers' way-finding and giving way (every zone)
- **The door first (the real cause):** a doorway's walkable width must take two bodies. The inn's door becomes a **double door**: two
  leaves on two hinges (left and right jambs), 2.6 m wide, `doorW` a parameter of `Inn()`/`InnFront()`; the nav gap then about 1.6 m.
  Both leaves open by day; one leaf shut after the innkeeper's bedtime (VillageWork.InnkeeperBed) so the late drinkers go out one at
  a time, which is the look of a village at night, not a bug.
- **A bigger taproom:** the Golden Cask 12x8 to 14x10, the Cracked Hearth 11x7 to 13x9 (data: the inn prop's `size` in oakhaven.json
  and khaven.json), five tables (fifteen seats), a longer bar with two stools, the hearth bench. The seat list moves out of WorldLife
  into a `ZoneInn` record the builder registers (seats, bar spots, the door's outside and inside points), so the room and the life
  cannot drift apart again. Neighbours that the bigger footprint meets (the Cask's kitchen lean-to, the hitching rail, the lamp) are
  moved in data; `BuildingGroundTests`/`ZoneContentTests` already catch overlaps.
- **Giving way at a doorway:** a `Doorway` lane on every openable door and gate: an outside point and an inside point 1.2 m apart.
  Whoever is on the lane goes first; the next waits at the point 1 m off the lane (the coop's `RampQueue`/`QueueSpot` pattern, already
  working for hens), in through one leaf, out through the other once the door is double. Priorities by purpose: a carrier with a load and
  the innkeeper first, then villagers, then sims (80 today), then idlers. A figure that has stopped (at the bar, a seat, a workplace)
  drops to priority 99 or carves (`NavMeshObstacle`), so walkers are never pushed by the seated.
- **A stuck watchdog in `Villager` (sims have one, villagers do not):** no progress for 3 s while travelling: step 0.6 m sideways and
  re-path; 10 s: give the errand up the way `Stranded` does. Shared with `SimFigure.AtInn`'s "jammed" test (one helper, both use it).
- **The same for other jams:** the Cask's kitchen door (`InnerDoor`, `KitchenDoorX`), Khaven's gate (`ZoneGate`), the bridges, the
  Adit's gates and the Deep's passages: measured by a test (3h), widened in the builder where under 1.2 m.
- **Somewhere to rest in every zone** (so "ALL Zones" holds for sims and villagers): the Peaks' Pilgrims' Rest and the Rim's Wain's
  Rest rebuilt as walk-in taprooms (`kind: inn`, variant 2 "waystation": one storey, a long table, bunks), the Shore's Hearth-Tree given
  an openable door and seats; `SimFigure.Inn` then finds one in every zone.
- **"Better ai" beyond the door (PROVISIONAL, small, Chris to pick):** stepping off the road for the player and the party; a nod or a
  line when you pass someone you have talked to (the social memory exists for sims); villagers under a roof when it rains
  (`ZoneBuilder.UnderRoof` exists); children's games on the green; the drinkers in twos at a table (partners exist). Nothing larger.

### 3b. Crafting loot worth having (armour, weapons, potions, food buffs)
- **The rule:** at every band, a plain crafted piece beats the vendor's and the camps' generated uncommon by one step in stats; a
  tempered/mastered piece (rare) equals the zone's named loot and needs one rare thing from the zone (a geode shard today; add a dungeon
  boss drop, an elite's drop, a secret's find); one epic a tier from a found recipe only (the heartwood greatblade is the pattern; the
  look system and the loot rules already know epic). Every tier fills its nine slots across the crafts, so a crafted kit is a kit.
- **Tiers (exist, GAME-ONLY names):** copper 1-5 (Oakhaven), bog-iron 5-9 (Khaven), ridge-steel 9-12 (the Peaks), ash-steel 12-15
  (the Rim), veridian 13-15 (the Shore); a sixth for 16-19 comes with Phase 9's Lowtowns. Stats and armour re-cut against
  `loot.*.json` and the balance page (`balance.html`) so the sims' gear tiers (SimGearTests) and the player's line up.
- **New crafts, so every class has a maker (PROVISIONAL set; decision 4):** **Leatherworking** from the hides that already drop
  (boar, coney, hill deer, bear, wolf, ash, dappled, moss; the tanner and leatherworker villagers exist): leather kits for Ranger,
  Rogue, Scoundrel, Druid; **Weaving** from the shepherds' fleeces (made an item, `mat.fleece`, and the stall's good): wool and dyed
  cloth kits for Mage, Archivist, Cleric; **Bowyer** work from the logs (bows, staves, wands, shields, arrows if ammunition ever comes),
  either as Woodcutting recipes (as charcoal is) or its own craft; **geode-cutting** (aether-geodes are CANON, book1 ch.3; the shard is
  our use): neck pieces and the tempering stone. Craft slots 2 or 3: decision 3.
- **Alchemy (rename: decision 5):** beyond healing: a draught for each class's pool (mana, energy, Nerve), elixirs of one stat for 30
  minutes, a salve of armour, a cure for the CC poisons/bleeds (CC_DESIGN), and one flask a tier from the rare herbs (3d makes them
  regrow). The books' alchemy is the Council's industry (corrosives, flash-pots, foundries: book1); the village's draughts stay
  GAME-ONLY and are never called the Council's.
- **Cooking:** every dish from skill 20 gives a **well-fed buff** (stamina plus one stat by dish, 30 minutes, tiered by level), feasts
  that feed the party of five (the sims' runs), the inn's kitchen sells the zone's dish. Buffs ride the aura system (`AuraTests`) and
  show on the buff row (note 190); a consumable gets `buff` fields in `ItemDef` (stat, amount, seconds). Buffs are not saved.
- **The sims make and sell them too:** SimEconomy's forging and brewing (5.3c) takes the new recipes; crafted goods at the stall and in
  trades with you (5.7). Gathering incentive: every node's material is wanted by at least two recipes, and the village stock buys
  surplus (hides and fleeces already; add ore, logs, herbs at the stall).

### 3c. Recipes to find
- **Two kinds:** `taught` (known at the skill, as today: bars, charcoal, the plain pieces) and `found` (a `recipe` item kind, `teaches` the
  recipe id, learned on use like a tool; `Progress.recipes`, **save format 11**, Chris's save backed up first). The trade window shows a
  found recipe you lack as a grey "?" row with its band, nothing else.
- **Where (6-8 a zone, 30-40 in all):** the trade's own villager sells two for coin (the blacksmith, the herbalist, the cook at the inn),
  one behind standing with the zone's faction; camp bosses, elites and dungeon bosses drop them beside named gear (`LootTableDef`
  entries); secrets hold them (the Bone-Carver's Bundle teaches the leviathan-bone harpoon; the Tapper's Stump the sap glaze; the
  Shepherd's Tin a cheese); village-work quests reward them (the baker's, the tanner's); one or two are written on Chronicle pages
  (a cook's receipt in the Cask's kitchen book).
- **Naming (decision 9):** the project's voice already calls a page a leaf ("A Leaf from the Chapel Book"): "a smith's leaf: Bog-iron
  coif", "a cook's leaf", "a herbalist's leaf". Not "pattern/formula/plans/schematic" (another game's words).

### 3d. The book of lore
- **What it is:** a **Lore tab** in the quest book beside the Chronicle (the Chronicle stays the story and its pages), with chapters:
  Places (every landmark, filled when you reach it, from the zones' `canonNote` and a line each), People (every named villager and
  sim you have spoken to), Creatures (each kind seen and killed: where, what it drops, what it is for), Herbs and Ores (each node worked:
  where it grows, what it makes), Factions (the Standing tab's text), Pages (the 35 documents, as today), Leaves (the recipes found).
  A chapter completed for a zone gives an achievement and a title. Data: `EncounterContent/Lore/*.json`, one file a zone, each entry
  with `canonStatus`.
- **What fills it, where its pages are found:** doing (arriving, talking, killing, gathering) fills the plain lines; the better half of
  an entry needs a page found in the world: a new `read` prop kind beside the notice board: shelves in the inns and houses (the Cask's
  kitchen book, Moss's lodge), the Adit's Scriptorium (the rework has one), the chapel book at Oakhaven, the pilgrims' cairns on the
  Peaks, the Ash-Walkers' idols on the Rim, the Hearth-Tree on the Shore, plus `note` secrets and bosses' drops. About 10 pages a zone.
- **Lore labels:** CANON only with a chapter cited (as the documents do: "CANON, book1 ch.4"); CANON-EXPANDED round canon places and
  factions; GAME-ONLY for the villages' people, the metals, the herbs; PROVISIONAL foreshadowing kept to what the documents already
  allow (the Pale), since the game is set before the books. The world bible names the healer Mirene Thistledown and "medicinal herbs"
  and nothing more on trades: every herb, ore and dish is GAME-ONLY; Veridian sap is CANON (book3 ch.4); salt and the Salt-Menders
  CANON; ale and bread as ordinary things CANON.
- Decision 8: a tab, or a book item you must find first and carry.

### 3e. Secrets in every zone
- **Targets:** Oakhaven 15 to 20 (560 m); Khaven, the Peaks, the Rim 8 to 14 each; the Shore 6 to 14; the Sealed Adit 0 to 5 (the
  Scriptorium's page, a miner's cache, a key and its chest, a lookout over the Breach); the Root-Mother's Deep 0 to 3; Crowsfoot Hollow
  keeps its two. About 90 in all (decision 7).
- **New kinds:** `carving` (a mark on a rock or trunk: a Lore line only), `read` (a shelf or book: a page), `grave` (a named stone: a
  Lore line), `remains` (a dead adventurer: gear and a page), `shrine` (a wayshrine's offering: a coin for a short blessing).
  `DiscoveryLog.Kinds`, `ZoneBuilder.BuildSecrets` looks, `SecretPlacementTests` rules extend; the `needs` chains get longer (a page
  that names where the key hangs).
- **Hints, because today there are none (decision 6, how loud):** (1) a `hint` line on every secret, said by the trade nearest it in its
  idle talk (the shepherd knows the tin at the fold, the hunter the poacher's camp, the sexton the crossroads coins) and never naming
  the spot's landmark (`DiscoveryLog.Validate` checks it); (2) the notice board's lost-and-found; (3) the Discoveries tab counts by
  region ("3 hidden in the hills") using the zone's landmark names, and one clue line unlocked by a found page; (4) a soft chime within
  6 m of an unfound searchable, no glint (Chris's bar is subtle; a glint is a marker).
- **The rare herbs regrow:** moonbell, widow's-lamp, frostbell, last-light and lantern-moss become nodes with a game-day respawn after
  their first find, the inputs of 3b's flasks: the reason to come back with Herbalism.

### 3f. Gear that changes the look on equip (with Phase 8.5)
- **Per slot, on the figures we have:** head: the drawn caps, hoods, coifs and kettles on by default now (the model helms stay behind
  the switch until 8.5 fits them); shoulders: a visible pauldron from tier 1 (mantles at tier 0 only); chest: a silhouette from tier 1
  (a jerkin's skirt, a hauberk's hem, a cuirass's plates) rather than a recolour; hands and feet: cuffs and boot-tops as shapes; legs:
  greaves' plates; neck: small and as today; weapons: the models, as today.
- **Crafted kits get their own families** (copper scale, bog-iron rivets, ridge-steel fluting, ash-steel blackened, veridian
  green-bronze, heartwood), so a crafted set reads as a set, with the rare accents on the tempered pieces; the three loot sets too.
- **A preview:** the trade window dresses the paper doll (`ApplyGearIds`, the wardrobe mannequin) in the piece before you make it.
- **The stale band comments** in GearLooks.cs brought up to round 29's cut (a one-line tidy; the code is right).
- **The overlap with 8.5 (decision 2):** 8.5 is the fit (the helms, the Modular Hero rig, route (a) or (b)); 8.6 is what the pieces are.
  If 8.5 picks route (a), 8.6's new families are made as that pack's parts; if (b), as drawn parts. Do not draw a family twice: the looks
  work waits for 8.5's choice; the rest of 8.6 does not.

## 4. Order of work, sizes, tests, labels
| # | Step | Size | Tests (EditMode / PlayMode / captures) |
|---|---|---|---|
| 1 | The double door and the bigger taproom, both inns; `ZoneInn` record; neighbours moved | M | `InnLayoutTests` (door 2.6 m, nav gap over 1.4 m, seats inside the walls, no overlap); captures: the Cask's front at dusk, the taproom with twelve inside |
| 2 | The doorway lane and queue; stopped figures yield; the villager watchdog shared with sims | M | `DoorwayTests` (every inn door and gate's walkable width over 1.2 m); PlayMode `VillageDoorTests`: twelve villagers and four sims through the Cask's door over a simulated evening, none still for over 4 s within 3 m of it |
| 3 | Taprooms for the Peaks, the Rim and the Shore | M | `SimLifeTests`: sims rest indoors in every zone |
| 4 | Consumables with buffs: well-fed, elixirs, draughts by pool; the buff row | M | `ConsumableBuffTests` (an aura with the right stat and time; not in combat for food; the party feast feeds five) |
| 5 | Found recipes: the item kind, the learned list, format 11, the grey rows, the sources | M | `ProfessionLogTests` (unknown until learned, learned once), `SaveMigratorTests` (10 to 11), `LootDataTests` (every leaf is in a table, a secret, a quest or a shelf) |
| 6 | Crafting tiers filled, the new crafts, the sims making and selling | L | `ProfessionDataTests` (every tier's nine slots, every material wanted twice), `SimEconomyTests`, the balance page re-run on the crafted kits |
| 7 | The Lore tab, its data, the `read` props, the unlock hooks | L | `LoreBookTests` (entries unlock, every entry labelled, no entry names a secret's spot), `IconCoverageTests` |
| 8 | Secrets to target with hints, the new kinds, the regrowing herbs | L (M a zone) | `SecretPlacementTests` (counts, hints present and not naming landmarks, reachable), `DiscoveryTests`; a capture of one hint scene a zone |
| 9 | Gear looks: crafted families, silhouettes, helms by default, the preview | L (after 8.5's choice) | `GearLookTests`, `GearVisualTests`; the wardrobe line-up (`--crulanda-wardrobe-capture`) of every crafted kit on every class |

Sizes: S under a round, M a round, L two to three. Steps 1-3 are one sitting and Chris can see them; 4-6 are the professions; 7-8 the
world; 9 waits on 8.5. Each step: fix, targeted tests, build, capture, commit, publish (test-budget: no full suite until the phase ends).
Labels: the metals, herbs, dishes, recipes, inns, villagers: GAME-ONLY; aether-geodes, Veridian sap, salt, the Silent Pilgrims, the
Sandthrone, the Ash-Walkers: CANON where cited; lore entries labelled one by one; this whole plan PROVISIONAL.

**Status (p86-lore), 2026-10-15, steps 7 and 8, a helper's pass on branch `p86-lore`: written and compile-checked (Roslyn), not yet run in Unity; OPEN.**
- Step 7, the book of lore: `LoreBook.cs` (data `EncounterContent/Lore/<zone>.json`, one a zone, 160 entries, each labelled; registered as
  `EncounterContent.loreFiles` in Encounter.asset and by Build Oakhaven). The Lore tab (`EncounterHud.Lore.cs`) has its seven chapters: Places
  (every landmark, its line and the zone's canonNote), People (named folk spoken to, with their stories; adventurers hailed), Creatures (kinds
  killed: camps, levels, drops), Herbs and Ores (nodes worked, rare herbs found: trade, ground, makings), Factions, Pages (the 2026-10-15 book)
  and Leaves (a hook for step 5: `EncounterSession.LoreLeaves`). A better half unlocks with its page or its find: 6 to 15 a zone. A zone's
  chapter filled gives "The Book of <zone>" and the title "Chronicler of <zone>"; all of them "the Rememberer". What doing records is kept in
  `EncounterProgress.explored` under keys of its own (`lore|met|...`, `lore|slain|...`, `lore|worked|...`, `lore|sim|...`): no new field, the
  save format stays 10. The `read` kind is a hidden find (a lectern, a cairn's book, slates); shelves inside the inns wait for step 1's `ZoneInn`.
- Step 8, secrets: 76 to 91 (Khaven 10 to 13, the Peaks 10 to 13, the Rim 10 to 13, the Shore 8 to 14 with three in the Root-Mother's Deep;
  Oakhaven 25 and the Adit 13 kept, Crowsfoot Hollow's two kept). New kinds carving, read, grave, remains, shrine (`ZoneBuilder.SecretLooks.cs`);
  the Adit's 13 got their clues and hints (`Resources/Lore/adit.json`); six new lore pages. The Discoveries tab counts the hidden by the zone's
  great places ("3 hidden about the Mastwood"). A found rare herb grows back a game day after it is picked (Herbalism 10-75 by the herb); a
  wayshrine blesses (3% less taken, 10 minutes) when found, then for a crown. Tests: `LoreBookTests` (new), `DiscoveryLogTests`,
  `ZoneLoreTests` (EditMode); `SecretPlacementTests`, `DiscoveryTests` (PlayMode).

## 5. Open decisions for Chris (one line each)
1. **Place:** 8.6 straight after Phase 8 (his 23:25 "work on phase 8"), before or after the talents' fourth paths and 8.5?
2. **8.5 and 8.6 together** as one gear phase, or 8.6's looks (3f, step 9) alone waiting on 8.5's rig choice?
3. **Craft slots:** keep 2 (the choice bites) or 3 with the new crafts?
4. **Which new crafts:** Leatherworking, Weaving (fleeces), Bowyer (or Woodcutting recipes), geode-cutting: all, some, names?
5. **Rename Alchemy** (the books' alchemy is the Council's industry): "Simpling" (the bag is already a simples wallet), "Herb-craft", or keep?
6. **How loud the secret hints are:** villagers' lines only; plus the Discoveries tab by region; plus a soft chime near one; a glint (not advised)?
7. **Secrets target:** about 90 in all (Oakhaven 20, the others 14, the dungeons 3-5), or fewer and better?
8. **The book of lore:** a Lore tab in the quest book, or a book item found first and carried?
9. **Recipe items' name:** "a smith's leaf / a cook's leaf", or plain "Recipe:"?
10. **Buff size:** well-fed and elixirs at 30 minutes and one stat; draughts per class pool: yes?
11. **Found recipes behind standing** with the zone's faction, or coin only?
12. **Taprooms for the Peaks, the Rim and the Shore** (Pilgrims' Rest, Wain's Rest, the Hearth-Tree): yes?
13. **"Better ai" beyond the door:** which of 3a's small behaviours, or stop at way-finding and giving way?
14. **The inns' new size:** the Cask 14x10 and the Hearth 13x9 with fifteen seats (the kitchen lean-to and the hitching rail move)?

**Chris's answer (2026-10-15 ~22:00, the question prompt, verbatim): "Yes, your defaults (Recommended)"; with "keep going finish phase 8"
and "them move to 9".** The defaults as asked: double doors and bigger inns, taprooms in every zone; three craft slots with Leatherworking
and Weaving added; coin-only recipes called "a smith's leaf"; 30-minute food and elixir buffs; about 90 secrets with villager hints plus a
count by region; the Lore tab in the quest book; villagers step aside and shelter from rain. So, one by one:
1. 8.6 now, finishing Phase 8; then Phase 9. 2. The looks (3f, step 9) wait on 8.5's rig: Chris chose route (a), the hero pack's body
   (ROADMAP Phase 8.5). 3. Three craft slots. 4. Leatherworking and Weaving are new crafts; bowyer work is Woodcutting recipes (as charcoal
   is) and geode-cutting is recipes of an existing craft, no new slot (PROVISIONAL). 5. Alchemy's display name becomes "Simpling" (its id
   stays, so saves keep their skill; PROVISIONAL, not asked by name). 6. Hints: villagers' lines and the Discoveries tab's count by region;
   no chime, no glint. 7. About 90 secrets. 8. The Lore tab in the quest book (built 2026-10-15). 9. "a smith's leaf / a cook's leaf".
   10. Well-fed and elixirs 30 minutes, one stat; a draught for each class's pool. 11. Coin only. 12. Taprooms for the Peaks, the Rim
   and the Shore. 13. Stepping aside for you and the party, and shelter under a roof in rain. 14. The Cask 14x10, the Hearth 13x9,
   fifteen seats.
