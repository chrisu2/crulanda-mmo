@title: Crulanda: everything built so far
@subtitle: Every build, addition, change and removal since the project began
@dates: 28 September 2026 to 2 October 2026
@cover: world-captures/oakhaven-03-the-golden-cask.png
@covercaption: Oakhaven: the Golden Cask and the village well, from the build check of 1 October 2026.
@covernote: Written for Chris on 2 October 2026 (playtest note 14).
@covernote2: The record runs to commit f046de3 on main, 2 October 2026, 00:06. Rebuild this PDF with tools\docs\make_history_pdf.py.
@footer: Record to 2 Oct 2026
@author: Crulanda project

# At a glance

## What the game is

Crulanda is a single-player game that feels like logging in to a classic MMORPG. You play alone, offline, in a world built from the Crulanda novels. It is made in Unity 6. The bar, in the words of the game brief: a beautiful MMO-style game with a big emphasis on details, graphics, lighting, mood and visual feel.

The game is set before Oakhaven is erased. You start in Oakhaven, find Mira the healer in the Golden Cask, and follow the story west and north through five zones, from level 1 to level 13.

Work began on 28 September 2026. This document covers five calendar days: 28 September to the first minutes of 2 October.

## The numbers that matter

{tiles}
5 | zones, levels 1 to 13
2 | dungeons you walk into
94 | named places
56 | quests
61 | camps, holding 206 mobs
12 | named elites and bosses
42 | hidden finds
26 | Chronicle pages to read
224 | hand-made items
104 | of them named loot
46 | recipes
140 | gathering nodes
2 | playable classes
9 | save formats so far
24 | builds published
171 | commits in git
{/tiles}

- **Tests:** 499 automatic tests are in the project today (341 EditMode, 158 PlayMode). On 28 September there were 115.
- **Size:** 200 C# files with about 45,400 lines; 8 shaders made for the game; 21 data files with about 23,600 lines; 28 documents.
- **People and animals:** 26 villagers, 21 named residents, 21 households; 81 rabbits and 39 deer to hunt; crows, sheep, chickens and cats that cannot be hunted.

## What is in the build you can play now

The playable build is `Crulanda.exe` in the `Crulanda-Playable` folder. Its files were written at 23:04 on 1 October. It is publish 24 (round 5). It holds everything marked [PUBLISHED] in this document:

- Five zones: Oakhaven, Khaven Village, the Shattered Peaks, the Ashland Rim and the Verdant Shore.
- Two dungeons: Crowsfoot Hollow and the Root-Mother's Deep.
- Day and night, weather in every zone, water you can wade and swim.
- The painted look on buildings, rock, masonry and caves.
- The Warrior and the Druid, with talents. Mira the healer at your side.
- 56 quests, the Chronicle, six factions with standing, 42 hidden finds.
- A village that works: every trade has a day, goods go round, households have purses, the out of work drink at the inn.
- Gathering in every zone, Maud's four trade bags, stations, charcoal, cooking (ten recipes) and hunting for hides.
- Weapons and armour that show on your body, loot beams, the loot window, compare tooltips, and all 104 named pieces dropping.

## Built, but not yet in your build

These are finished and merged on the main branch (round 6, merged at 23:14 on 1 October). No record says they have been copied to the playable folder yet. A full check of this build ran late on 1 October and another began at 00:06 on 2 October.

- **Blacksmithing:** 5 smelts and 21 pieces to make at a forge. [BUILT, NOT YET PUBLISHED]
- **Alchemy:** 5 draughts at a herbalist's bench. [BUILT, NOT YET PUBLISHED]
- **The two-craft rule:** you may hold two crafts and can forget one. [BUILT, NOT YET PUBLISHED]
- **The Armoury:** a record of every named piece and look you have found. Save format 9. [BUILT, NOT YET PUBLISHED]
- **Polish:** a stronger glow on worn rare and epic gear; no talking through walls; herbs that lean with the slope. [BUILT, NOT YET PUBLISHED]

## Being built next

- **Your playtest notes come first.** Three are being built on branches that are not merged yet: Oakhaven grown to about 560 m with Crowsfoot Hollow moved far from the village (note 1); mobs that come together and elites with moves of their own (notes 2 and 3); less green on the ore (note 5). [IN PROGRESS]
- **Then the rest of the notes:** the sheep, the fallen timber, cats' tails, sun and bloom, cave lighting, real icons, flowing characters, high-fantasy colour. [PLANNED]
- **Trades step 14:** elixirs, "well fed" meals, a smith who makes a piece from the ore you sell him. [PLANNED]
- **Loot steps L4 to L6:** quests that hand out their named gear, a turnable figure and icons on the character sheet, cloaks as a tenth slot. [PLANNED]

## How to read this document

- [PUBLISHED] means it is in a build that was copied to your playable folder.
- [BUILT, NOT YET PUBLISHED] means it is finished and on the main branch, but no record says it reached your folder.
- [IN PROGRESS] means it is being built on a side branch.
- [PLANNED] means it is written in a plan and nothing is built.
- Your own words are set apart in italics, like this:

> "give me a pdf of all items/process/builds/additions/subtractions to this whole game since we started" - you, 2 October (playtest note 14)

**Where the record comes from.** Git only starts at 02:14 on 29 September. Everything before that (all of 28 September) is known from the changelog and the project documents, not from git. Where a number or a date is uncertain, this document says so. The chapter "Where this record is uncertain" lists every doubt.

# Every build published

A publish means: the tests ran, the game was built, the zones were toured and the pictures were looked at, and then the build was copied to the `Crulanda-Playable` folder for you to play.

There are 24 publishes on record, plus the builds made before git existed. Times are the times of the commit that recorded the publish; the copy itself happened a few minutes before. The test column is EditMode / PlayMode tests passing at that build.

{table: widths=5,13,62,20; size=8}
| # | When | What the build contained | Tests |
| 0 | 28 Sep, from 19:19 | **Before git.** The first playable builds: the trail encounter with Mira and three sentries, then the slower combat, Warrior talents, the Druid, Oakhaven, day and night, village trades, quests, and the four zones to level 10. The last of these (save format 6) went out at about 02:06 to 02:14 on 29 Sep. | 166 / 47 at the end |
| 1 | 29 Sep 13:07 | Water, world edges and night: see-through water, a current in the creeks, ripple rings, bridges without the centre pier, an uneven shoreline, hills past the edge of every zone, a moonlit night. | 166 / 47 |
| 2 | 29 Sep 14:07 | The camera looks through trees. A ring under your target. A gold box round the selected nameplate. | 166 / 49 |
| 3 | 29 Sep 18:50 | Zone looks: Khaven's permanent dusk, the Peaks as real mountains, the grey Ash Rim, the Wasting's edge. The fix for zone travel. | 166 / 50 |
| 4 | 29 Sep 20:34 | The living Great Oak, better tree trunks, legs on the animals, the Weave-Eater remodelled, floating props fixed, landmarks built to match their names. | 166 / 50 |
| 5 | 29 Sep 23:03 | HUD labels that no longer overlap, road signs at exits, and the stylized turquoise water. | 166 / 50 |
| 6 | 30 Sep 00:01 | Fix round for the creatures and props, and the fix for losing your character on a temp-save run. | 166 / 51 |
| 7 | 30 Sep 00:28 | Cliffs as rock, not slabs. | 166 / 51 |
| 8 | 30 Sep 02:34 | Tree crowns as painted leaf cards; full pines. | 166 / 51 |
| 9 | 30 Sep 16:10 | Weather: clouds, cloud shadows, wind, rain, storms with lightning, flurries, mist, ash squalls. | 174 / 56 |
| 10 | 30 Sep 17:37 | Crowsfoot Hollow, the first cave: the deserters, Caddock the Bandit King, the quest "The Tin Crown". Mobs need to see you to notice you. | 174 / 65 |
| 11 | 30 Sep 19:12 | Secrets: 20 hidden finds, the Discoveries tab, save format 7. | 183 / 72 |
| 12 | 30 Sep 20:27 | Crowsfoot Hollow goes deep: the first dungeon. Quartermaster Hesk, the strongbox and its key. | 183 / 76 |
| 13 | 30 Sep 22:20 | The zones grown to 340 to 380 m: 37 new places, 18 camps, 14 secrets. Faster loading. | 183 / 76 |
| 14 | 1 Oct 00:04 | The Verdant Shore, the fifth zone (levels 11 to 13), and the lush art in every zone. Level cap 13. | 183 / 76 |
| 15 | 1 Oct 13:33 | The Root-Mother's Deep (the second dungeon). Crowsfoot's mouth hidden in a cliff. Every trade has a day. | 186 / 81 |
| 16 | 1 Oct 14:30 | The painted style pass, parts 1 to 4: buildings, rock, sky and colour, chunky props. | 186 / 81 |
| 17 | 1 Oct 14:48 | The painted style pass, part 5: painted masonry on towers, walls, the gate, the keep, crypts and ruins. | 186 / 81 |
| 18 | 1 Oct 15:21 | The out of work drink at the inn. Visual review, first batch: blue air, darker storms, a better night, leafy crowns, a greener meadow, real gable ends. | 186 / 83 (one test corrected) |
| 19 | 1 Oct 15:42 | Visual review, second batch: the Ash Rim's ground, the world's edge, ruins, the Peaks' rock, painted cave walls. | 186 / 83 |
| 20 | 1 Oct 19:19 | Round 1: trades begin (save format 8, materials, tools, the Trades window), six new village buildings, weapons and shields shown in your hands. | 221 / 89 |
| 21 | 1 Oct 20:42 | Round 2: households and homes, gathering in Oakhaven, armour shown on your body, buildings that sit into slopes, the hero inns and smithy. | 240 / 108 |
| 22 | 1 Oct 21:25 | Round 3: Maud's trade bags, Hob Linden the innkeeper, gathering in every zone, the loot list drafted (104 named pieces, not yet dropping). | 285 of 286 / 115 of 116 |
| 23 | 1 Oct 22:32 | Round 4: household purses, stations and charcoal, loot beams, the loot window, compare tooltips. | 310 of 312 / 133 of 135 |
| 24 | 1 Oct 23:11 | Round 5: cooking, hunting for hides, named loot dropping. **This is the build in your folder now.** | 319 of 320 / 150 of 151 |
| - | merged 1 Oct 23:14 | Round 6: Blacksmithing, Alchemy, the two-craft rule, the Armoury (save format 9), glow, no talk through walls, herbs on slopes. [BUILT, NOT YET PUBLISHED] | 341 / 158 tests exist; no result recorded |

Notes on this table:

- Where a test count reads "285 of 286", the missing ones were a test skipped on purpose, or a test that was itself wrong and was fixed right after. Publish 24 went out with one known flaky test (a rabbit that should bolt sometimes stood and watched); the fix went in with the publish commit.
- Publishes 11 to 14 and publish 2 are called published by the handoff document, not by their own commit messages. Publish 18's own commit says it was not yet published at that moment. So the count of 24 could be off by one or two.
- The handoff counts ten publishes on 1 October. They are numbers 15 to 24 here.

# What was added

This chapter goes area by area. Inside each area the work is in the order it came.

## How it began: the brief and the first three days of groundwork

**The brief.** The project started from a master brief of 76 sections (kept as `PROJECT_MASTER.md`). Its vision: a fantasy RPG that feels like a populated classic MMORPG server, played offline, with simulated adventurers. It set out nine phases, from a foundation (phase 0) to a first vertical slice (phase 8). The Crulanda novels are the primary source for the world.

**The work before Claude.** The project was started with another assistant. The handoff to Claude is dated 28 September and says it was asked for because you ran out of tokens. The documents do not name that assistant; the work folders are named "Codex". At the handoff there were 142 EditMode and 22 PlayMode tests, a Windows build, a Warrior talent prototype of nine talents, and a design document for 20 classes.

**Phase 0, the foundation** (28 September, before git) [PUBLISHED]

- The project layout, logging, an event bus, ids, tags, seeded random numbers, a content database, actors with health and resources, a save envelope with migrations, a dev console.
- Brought into Unity 6000.6.3f1 and tested there. Seven faults were fixed on the way in. Tests: 107 EditMode, 8 PlayMode.
- The method that has been kept ever since was set here: Unity runs in the background on a copy of the project, so your open Unity is never interrupted.

**The first playable encounter** (28 September) [PUBLISHED]

- One trail, one fight: recruit Mira the healer at camp, walk the trail, fight three sentries, loot a blade, equip it, level up, save and load.
- Three Warrior abilities: Strike, Challenge (a taunt) and Guard. Mira heals and throws bolts.
- Controls: WASD, Space, right mouse to look, Tab to target, 1 2 3 for abilities, E to use, I for the bag, F5 save, F9 load.
- You confirmed the build worked, and said combat was too fast. The pacing pass followed (see "What was changed").

**Phase 1, shared combat** (28 September) [PUBLISHED]

- One shared system for ability timing, damage, healing, armour, timed effects, threat, taunts, leashing, death, experience and levels.
- All 11 exit requirements met. Tests: 130 EditMode, 15 PlayMode. Windows build made.
- After your playtest: a visible swing timer for automatic attacks, and solid scenery you cannot walk through.

**Git begins** (29 September, 02:14). The whole prototype went into git as one commit of 520 files. From here on every piece of work is a commit.

## The world and its zones

**Oakhaven, the first real zone** (28 September) [PUBLISHED]

- Zones are built from data. Each zone is one file; the game builds the ground, roads, water, buildings, trees and light when it loads.
- Oakhaven was laid out from the canon village map and became the opening scene. Characters got bodies and a walk. The camera stopped going through walls.

**Four zones to level 10** (29 September) [PUBLISHED]

- The ladder: Oakhaven 1 to 2, Khaven Village 3 to 5, the Shattered Peaks 6 to 8, the Ashland Rim 9 to 10. The Peaks and the Ash Rim were new.
- Five camps of mobs in every zone that come back after you clear them, some with an elite. Ambush camps hide in tall grass; hold Ctrl to sneak past.
- Quests lead you from zone to zone. Exits show the next zone's levels. The world map shows the roads.
- Standing requests of yours that this answered: camps for grinding in every zone, and breadcrumb quests to the next zone.

**Zone looks** (29 September) [PUBLISHED]

- Khaven became a permanent dusk: low orange sun, violet air, a grey dead wood.
- The Shattered Peaks became real mountains: steep knolls, rock ribs, cliff shelves.
- The Ashland Rim became grey ash with cracks, grey ruins and a bruised sky, with ash falling.
- The Wasting's edge became a ragged front with a fog-coloured curtain.

**Props, landmarks and creatures** (29 September) [PUBLISHED]

> "this tree looks terrible. this should be the center piece of town" - you, 29 September

- The Great Oak: a living tree about 21 m tall and 24 m across, with a stone bench ring. The book says it was alive before the erasure.
- Tree trunks that taper and lean, with roots. Legs on cats, chickens, crows and rabbits.
- The Pale as tall robed figures. The Weave-Eater as a drifting tangle of threads, as the book has it.
- Landmarks built to match their names: the wayshrine, the drowned graveyard, the toll gate, the Captain's Eyrie, the Ash-Walker enclave, the Old Ribcage and more.

![The Great Oak on Oakhaven's green.](world-captures/oakhaven-02-the-great-oak.png)

**Secrets in every zone** (30 September) [PUBLISHED]

- Twenty hidden finds, five a zone: lookouts, caches, pages, glowing herbs, and a key with its chest in Khaven. None is on any map.
- Each pays once: experience, gold, an item or a Chronicle page.
- A Discoveries tab in the quest book counts what you have found in each zone.
- New to find: four gear pieces, four healing herbs and seven documents. Save format 7.

**The zones grow** (30 September) [PUBLISHED]

> "zones do need to be bigger with more places and secrets to explore" - you, 29 September

- Oakhaven went from 260 m to 380 m. Khaven from 240 to 340. The Peaks and the Ash Rim from 260 to 360.
- 37 new places. Among them: Crowsfoot Ridge, the Mastwood, the Old Barrow and the Watchtower (Oakhaven); the Gibbet Crossroads, the Plague Pit and the Hush (Khaven); the Signal Tower, the Sealed Adit and the Cold Tarn (the Peaks); Wain's Rest, the Drowned Leviathan and the Silent Statue (the Ash Rim).
- 18 new camps, with four named elites: Old Whitefoot, the Pale Reckoner, Old Scree-Tusk and the Ash-Deacon.
- 14 new secrets and five new Chronicle pages.
- Loading got faster: Oakhaven 5.6 s (was 8.8), Khaven 3.7 (was 7.3), the Peaks 3.2 (was 7.1), the Ash Rim 1.6 (was 3.8).

![Khaven Village: the Cracked Hearth under its permanent dusk.](world-captures/khaven-04-the-cracked-hearth.png)

![The Shattered Peaks: the Sandthrone toll gate.](world-captures/peaks-03-the-toll-gate.png)

![The Ashland Rim: the Ash-Walker enclave.](world-captures/ashrim-02-ash-walker-enclave.png)

**The Verdant Shore, the fifth zone** (published 1 October, 00:04) [PUBLISHED]

> "next zone we need a truly lush zone as pictured here" - you, 30 September

- The canon Verdant Shore of Book 3: 360 m, levels 11 to 13. The level cap rose to 13 (talent points stop at level 10).
- Places: the Ridge of Long Shadows, Rootfast with its five treehouses, the Veridian Temple, Mossveil Falls, the Mistmere, the Whispering Glade, the Fern Hollow, the Fallen Ghost-Oak, the Salt-Flats, the Greying and Palemist Hollow.
- Nine camps with two elites (Greyheart and Old Ninebranch), five Keepers who talk, a Silent Pilgrim, a glass-ship scout, twelve quests, four pages, five secrets, a new faction (the Veridian Keepers), 17 items and two merchants.
- Lush art that every zone now shares: ferns, broad leaves, reeds and flowers; giant trees 28 m tall; treehouses; waterfalls; mushrooms that glow; a fallen giant; rope footbridges.
- New creatures: the Veridian Keeper, a great forest stag, a forest spider, a bramble-thing.
- The Ash Rim gained the Old west road that leads here.

![The Verdant Shore: Rootfast, where the Keepers live in the trees.](world-captures/verdant-04-rootfast.png)

**Still to come for the world:** Oakhaven grown again to about 560 m, with nothing hostile within about 120 m of any village (your note 1). [IN PROGRESS]

## The dungeons

**Crowsfoot Hollow, the first cave** (30 September) [PUBLISHED]

- You asked for a level-3 quest to clear out bandits and their king in a cave.
- A cave you walk into with no loading screen, at the end of Oakhaven's North road. Torches, a camp, a hall with a throne.
- The Sandthrone deserters hold it: eight of them, levels 3 to 4. Their leader is Caddock, the Bandit King, with a crown of hammered tin. Caddock and his band are made for the game; the Sandthrone company is from the books.
- The quest "The Tin Crown", from Wil Carder, whose barn they raided. It is hidden until you reach level 3.
- Mobs must now see you before they notice you. Nothing aggros through rock or a wall.

**Crowsfoot Hollow goes deep, the first dungeon** (30 September) [PUBLISHED]

> "the cave should be deep and the first foray into dungeon crawling" - you, 30 September

- The cave now runs 114 m from its mouth and 16 m down: the camp, the Drop (eleven metres down on plank treads), the Store Caves, the Deep Stair, and the Echoing Hall, 18 m across and 9 m high.
- Six groups to fight, harder as you go down. A new elite, Quartermaster Hesk. Caddock rose to an elite of level 5 with three guards.
- Loot: Caddock's Tin Crown. Hesk's strongbox is a locked secret; its key hangs on a nail on the Drop. Inside is Hesk's Shuttered Lantern and a page, "By Order of the King".
- The band comes back after 5 to 15 minutes, not 90 seconds. "The Tin Crown" became a level-4 quest paying 190 experience and 25 gold.

![Crowsfoot Hollow: Caddock's guard in the Echoing Hall.](world-captures/oakhaven-86-hollow-hall.png)

**Crowsfoot's mouth hidden, and the Root-Mother's Deep** (1 October) [PUBLISHED]

> "can you also create a dungeon for the new zone? the dungeon in the first zone kind of stick out as just a rock. it needs to be built into a mountain or something and kinda hidden..no so obvious. they are bandits" - you, 1 October

- Crowsfoot's mouth became a slot in a cliff face. The road ends short; a track bends through pines and boulders, and you see the mouth only from the last bend.
- The Root-Mother's Deep is the Verdant Shore's dungeon, under the Veridian Temple: the root-stair, the Root Gallery, the Sap Well, the Cold Stair and the Heart. Sap veins light it, not torches.
- Six camps of briars, withered Keepers and mist-walkers. The boss is the Hollow Root-Warden, an elite of level 13, who drops the Root-Warden's Crown.
- The Root-Mother herself: a vast knot of root with a hollow face, and the cold lodged in her root.
- A finale quest, "The Root-Mother's Deep", a fifth page for the zone, and a hidden pack.

![The Root-Mother's Deep: the Heart, with the Root-Mother behind.](world-captures/verdant-86-hollow-hall.png)

**Still to come for the dungeons:** Crowsfoot Hollow moved about 280 m from the village green (note 1); bosses with moves of their own and guards who join (note 2); baked, moodier cave light (note 10). [IN PROGRESS] for notes 1 and 2, [PLANNED] for note 10. Later ideas on the list: a crypt under Khaven, a mine in the Peaks, a sunken ruin on the Rim.

## Day, night, weather and water

**Day and night** (28 September) [PUBLISHED]

- A game day lasts 40 real minutes. The sun becomes the moon. Dawn, dusk and night change the light, the fog and the sky. Lamps light after dark. The time shows on the minimap.
- Villagers go home to bed at night and come out at dawn.

**Water** (29 September) [PUBLISHED]

- One water model: creeks run downhill in carved channels and can be waded; ponds can be swum, with floating, a swim pose and splashes; bridges can be walked. Animals and trees stay out of the water.
- Then, after the first visual review: water lit properly, a shoreline that is not a perfect circle, creeks that wander.

> "more water-like, more see-through, better physics" - you, 29 September

- See-through water: you see the bed in the shallows. Creeks push you gently downstream. Standing in water sends out rings.
- Then your art direction. You shared a picture of a stylized fantasy cove and chose that look: hand-painted turquoise water with soft foam and painted sparkle, for Oakhaven, the Peaks and the Ash Rim. Khaven's Gloom Creek stays murky. The picture was a style reference only.

**Weather** (30 September) [PUBLISHED]

> "move weather up after the trees" - you, 30 September

- Every zone has its own weather. Spells last 7 minutes and change one step at a time, blending over 50 seconds.
- Oakhaven: clear to fair, overcast, rain and storms. Khaven: mostly mist, overcast and rain; it never clears. The Peaks: flurries. The Ash Rim: ash squalls; it never rains on the ash. The Verdant Shore: clear, mist, rain and storms.
- A painted cloud layer, and cloud shadows that sweep the land. Wind in the grass and the leaves.
- Rain with streaks and splashes, rings on the water, ground that darkens and dries. Nothing falls under a roof.
- Storms with lightning every 8 to 26 seconds.
- A chat line when the weather turns, and the weather under the minimap clock.
- Nine kinds in all: clear, fair, windy, overcast, mist, rain, flurries, ash squall, storm.

![A storm over Oakhaven.](world-captures/oakhaven-80-weather-storm.png)

**Night, improved** (1 October) [PUBLISHED]

- Firelit windows and lamps on a moonlit blue base. You said: "lights look better" (note 8). That look is to be kept.

![The Golden Cask at night, with its windows lit.](world-captures/oakhaven-99-inn-front-night.png)

**Still to come:** a sun you can see, with a halo, glare, shafts and glints on water (note 9). [PLANNED] There is no sound in the game yet; rain and thunder sound is on the later list.

## The look

**The first graphics pass** (29 September) [PUBLISHED]

- Denser grass that sways, tall-grass patches, falling leaves, falling ash on the Rim.
- A lighting pass: bloom, sun shafts, a colour grade for each kind of land, a night sky tint.
- New looks for Hollows, cultists, wolves, boars and Weave-Eaters. Faces and hair on villagers.

**The first visual review** (29 September). A reviewer looked at every screenshot of every zone and a second reviewer checked each fault found: 81 faults confirmed, 7 rejected. They were worked off in numbered steps over the next two days (water and night, zone looks, props and creatures, the HUD, then the painted pass).

**The camera and the trees** (29 September) [PUBLISHED]

- Trees fade out when they hide you and come back after. Trunks no longer pull the camera in. You can click through a faded tree.

**Cliffs as rock** (30 September) [PUBLISHED]

- Every cliff became a stack of faceted rock lumps with a ragged crest and fallen blocks at the foot.

**Tree crowns** (30 September) [PUBLISHED]

> "trees still look bad" - you, 30 September

- Crowns became painted leaf cards on boughs, with a new leaf shader that sways in the wind. Pines became full triangles of drooping boughs. Bushes and far trees followed.
- This took two passes in one day: the first left the pines thin.

**The game brief** (30 September). `GAME_BRIEF.md` was written to hold your direction: the look and feel is the bar every piece is judged against; painted, saturated, hand-crafted; every zone keeps its own mood; trees are judged hardest.

**The painted style pass** (1 October) [PUBLISHED]

> "keep going with the painted style pass" - you, 1 October

- Buildings: hand-painted plaster, thatch in ragged layers, slate, coursed stone and planked timber. Eaves, ridge caps, framed windows with shutters, plank doors with iron bands, chimneys.
- Rock: cliffs, crags and boulders in bedded painted rock.
- Colour: Oakhaven under a blue sky on a green meadow; the Peaks crisp and blue; Khaven and the Ash Rim keep their moods, with more depth.
- Props: chunky hand-made fences, carts, lamps, wells, stalls, barrels, crates, woodpiles, bridges and signposts.
- **Masonry** (part 5): towers, walls, the toll gate, the keep, crypts, ruins and wayshrines in painted stone.

**The second visual review** (1 October) [PUBLISHED]

- Four reviewers looked at every picture of the published painted pass and ranked what still fell short: 14 items. All 14 are done.
- First batch: blue air over Oakhaven, storms that darken the day, the better night, leafy tree crowns, a greener meadow, hedges and haystacks with shape, gable ends that are wall.
- Second batch: drifted ash on the Rim, land that carries on past the zone's edge, a carved waystone, ruins with broken outlines, bedded rock on the Peaks, painted cave walls.
- Items 10 and 11: buildings that sit into slopes on stone footings with steps; the Golden Cask and the Cracked Hearth as hero buildings (a jettied front, dormers, a porch with a lantern, a sign twice the size); Vell's smithy as a slate-roofed hearth house.

**No bought art.** Every figure, prop, texture and icon in the game is made in code. Nothing is copied from another game.

**Still to come for the look:** flowing characters to replace the block figures (note 12), high-fantasy colour (note 13), a woolly sheep (note 4), a broken windfall tree (note 6), a cat's tail that moves (note 7). [PLANNED] Small leftovers on the review list: Mossveil Falls as a flat rectangle, flat discs for mud and puddles, a straight moss line on the giant trunks, squiggles on the salt flats, bright ferns on the Peaks' scree, hard edges on tall-grass patches.

## Combat and the classes

**The Warrior** (28 September) [PUBLISHED]

- First a prototype of nine talents with a talent window (B), respec, and a save that remembers them.
- Then talents from data: 27 playable Warrior talents in three branches (Tank, DPS, Support). Three new actions from talents: Intercept, Breaching Blow and Muster.
- New rules to play with: weapon pressure (0 to 5), Exposed targets, barriers, enemy stagger, and a visible enemy swing timer for timed Guards.
- Save format 3. Tests: 145 EditMode, 26 PlayMode.

**The class design work** (28 September). A build matrix for all classes (20 entries, 61 proposed paths) and a talent calculator page (20 classes, 61 trees, 875 proposed talents). These are design tools, not game code. Only the Warrior and the Druid are playable.

**The Druid** (28 September) [PUBLISHED]

- Four forms: Barkhide (tank), Thornclaw (melee), Rootmend (healer), Thornsong (ranged). 22 abilities and 28 playable talents.
- Damage and healing over time, snares and roots, cast bars, a save per class, switching characters, a 10-slot action bar.

**Targeting** (29 September) [PUBLISHED]

- Left-click selects people. E talks to the one you selected. A ring under your target: red for an enemy, gold for a body with loot, green for a friend.
- Experience on a new curve; kills give less as mobs fall below your level and nothing at four levels below.

**Still to come for combat:** mobs that pull together by kind, and elites with more health, a harder hit and moves of their own (notes 2 and 3). [IN PROGRESS] Of the talents designed, 21 Warrior and 28 Druid talents are not playable yet (the deeper rows). You asked on 28 September that the world come first and per-skill balancing later.

## Quests

**The quest system** (29 September) [PUBLISHED]

- Quests are written as data. Four kinds: main (the Chronicle), side, NPC and faction. Seven kinds of step: talk, deliver, collect, kill, use, visit, flag.
- Markers over heads and on the map (! and ?), a conversation window, a quest book (L) with Quests, Chronicle and Standing tabs, a tracker on screen.
- Six factions with standing. Chronicle pages you can read.
- Oakhaven got ten quests, the black-iron wagon and the Bureau ledger. Two new residents: Quill the Salt-Mender and Warden Ivel.
- The inn was renamed the Golden Cask, the book's name. Save format 4.

**Quests through the zones** (29 September to 1 October) [PUBLISHED]

- A main chain from Oakhaven to Khaven, the Peaks, the Ash Rim and the Verdant Shore.
- "The Tin Crown" (30 September), with a new rule that hides a quest until you reach its level.
- Twelve quests on the Verdant Shore and its finale, "The Root-Mother's Deep" (1 October).
- Maud Tanner's four bag quests (1 October): "A Wallet for Simples", "A Strap for the Woodyard", "The Cook's Scrip", "Ore Wants a Stout Bag".
- Today: 56 quests (19 main, 29 NPC, 5 side, 3 faction) and 26 Chronicle pages. All are listed in Appendix B.

## Village life

**Hen-wives and coops** (28 September) [PUBLISHED]

- You asked for a chicken farmer who gathers eggs, feeds the chickens and puts them in the coops at night.
- Oakhaven got three coops, each with a hen-wife who opens up, feeds, collects eggs, herds the hens in at dusk and shuts the door. Hens roost at night.

**Village trades** (28 September) [PUBLISHED]

- Outfits and tools for 13 trades: blacksmith, merchant, baker, hen-wife, farmer, hunter, leatherworker, skinner, woodcutter, herbalist, miller, elder, drinker.
- A trade under each name. Workplaces: a forge, stalls, an oven, a tannery, a woodpile. Work poses.
- Oakhaven got a smithy, three market stalls, a bakehouse, a tannery yard and a woodyard, with 20 villagers who each have their own lines.

![Oakhaven's market row.](world-captures/oakhaven-12-market-row.png)

**Every trade has a day, and the goods go round** (1 October) [PUBLISHED]

> "the hen maiden/mother should feed the chickens in the morning. water during the day. collect eggs. each profession should have a daily ai job schedule with tasks ... she should take some eggs to the merchants to sell. some home to eat. some eggs to the inn for food for the village, etc. all job are intertwined." - you, 1 October

- Each trade's day is shifts (where to be, hour by hour) and errands (pick up, carry, hand over, say so).
- The hen-wife's day: feed at first light; eggs to the inn's kitchen at 08:24; water from the well at 09:36 and 13:30; dinner at home; the afternoon feed; eggs to the stall at 15:24; the hens in at dusk.
- The goods go round: barley to the mill, flour to the bakehouse and stall, the first loaves to the inn (the baker is up at 04:36), logs to the woodyard, firewood to the inn and forge, hides to the tannery, hares to the inn's pot, herbs to the stall, well water for every house.
- Every load shows in the hands: a basket of eggs, a bucket, sacks, a tray of loaves, logs on the shoulder.
- The trades talk about each other's goods. The merchant sells Oakhaven eggs while the hen-wife's eggs last.

**The out of work at the inn** (1 October) [PUBLISHED]

> "have unemployed npc show up at the inn and drink till gone or passed out" - you, 1 October

- The drinkers, and any trade a village has no workplace for, are at the inn from 11:00 with a tankard, drinking the day's cask. When the ale is gone they go home. Past their limit some pass out over the table and the rest reel home.

**New buildings** (1 October, trades step 1) [PUBLISHED]

> "each npc has their own house and each profession has their own workshop" - you, 1 October

- The Carder farmhouse, the Crisp cottage, Maud Tanner's leather shop by the South road (she works there 9 to 12 and 14 to 18), Lisbet's drying hut, the Golden Cask's kitchen, the game rack at Moss's lodge. Every house carries its name.

**Households and homes** (1 October, trades step 3) [PUBLISHED]

- Every Oakhaven villager lives behind a named door: 15 households then (16 today, with the inn), and 5 in Khaven.
- The Tanners (Maud, Fen and Nettie) share a house. Farmers live with their hen-wife. Aldo Crisp has his cottage; Garet Moss his lodge.
- Knock on a door and you get an answer: wares or quest business opens it; otherwise you hear where the head of the house is, or that they are abed.

**The innkeeper and the workshops** (1 October, trades step 6) [PUBLISHED]

- Hob Linden keeps the Golden Cask: up before dawn, behind the bar till late, selling brown loaves and Harrow cheese. He is made for the game.
- Eggs, loaves and hares are handed over at the kitchen's back door. Dinner goes out to the tables.
- Every trade works its own workshop. Ama, Tamsin and Hedda keep their own stalls.

**Purses and spending** (1 October, trades step 7) [PUBLISHED]

> "so he can buy food/lumber for his family" - you, 1 October

- Every household has a purse. What you pay Maud reaches the Tanner house and is spent where you can see it: Nettie fetches a loaf, Fen carries firewood home, the chimney smokes.
- Short of coin, they go without and say so ("Can't stretch to firewood today."). With no trade bag sold, the Tanners' hearth is cold at dusk two days in three.

## Trades

> "we need to gather ore, lumber,herbs etc and sell or refine. also have a profession for the character. blacksmith, alchemist, cook, etc." - you, 1 October

You then chose, by the question prompt: two crafting professions at most; gathering free for everyone; Cooking free for everyone; the first crafts are Blacksmith, Alchemist and Cook, each at a village workplace.

The plan has 14 steps on two tracks. Steps 1, 3, 6 and 7 are the village steps above. The rest are here.

**The groundwork** (step 2, 1 October) [PUBLISHED]

- Save format 8 remembers what you have learned of the trades and your trade bags.
- Nineteen materials: ores, logs, herbs, charcoal, flour, salt, vials. A miner's pick and a woodcutter's hatchet to buy from Ama Rusk or the smith.
- The Trades window (K): Mining, Woodcutting and Herbalism free to everyone; Cooking for everyone; two crafts to choose.

**Gathering in Oakhaven** (step 4, 1 October) [PUBLISHED]

- Copper seams on the rocks (rich ones in Crowsfoot Hollow), windfall oaks, yarrow.
- A work bar, a skill that rises, nodes that rest and come back. The village notices what you sell.

**Maud's trade bags** (step 5, 1 October) [PUBLISHED]

> "have the leatherworker make bags for professions by quest (gather leathers) or just buy them outright from him" - you, 1 October

{table: widths=22,34,12,14,18; size=8.5}
| Bag | Holds | Slots | Price | Or earn it with |
| Simples-wallet | herbs | 6 | 12 gold | 3 grey wolf pelts |
| Log-sling | timber | 6 | 16 gold | 3 hill-deer hides |
| Larder-scrip | eggs, cheese and the larder | 8 | 20 gold | 5 coney skins |
| Ore-poke | ore, bars and charcoal | 8 | 24 gold | 3 boar hides |

- Use a bag to wear it. Its slots are a labelled row under your bags, and gathered goods go into it first. The coin goes to Maud's household.

**Gathering in every zone** (step 8, 1 October) [PUBLISHED]

- Ten ore seams, eight windfalls and ten herbs in each other zone. Khaven: bog-iron, black pine, mourner's cap. The Peaks: Adit iron, stone pine, tarnwort. The Ash Rim: cinder ore, ash-snags, cinder-thistle. The Verdant Shore: Veridian ore, ghost-oak, dewfern, with rich seams in the Root-Mother's Deep.
- Every node was placed by a script that checks roads, water, buildings, camps, secrets and caves, and walks to it from the zone's start.

**Stations and charcoal** (step 9, 1 October) [PUBLISHED]

- Stations to work at: Vell's forge, the bakehouse oven, Lisbet's bench, the Golden Cask's kitchen range and hearth. The other zones have field anvils, herbalists' benches and cookfires.
- A recipe pane in the Trades window (Make, Make all). The first five recipes: charcoal from each kind of wood.
- Skill rises always while a recipe is orange, half the time when yellow, one time in ten when green, never when grey.

**Cooking** (step 10, 1 October) [PUBLISHED]

- Everyone cooks at any fire. Ten recipes, from griddle bread and boar stew to venison pie, each better than bought food at its level.
- Wolves, hounds, mossboars and stags drop meat. Meat goes in the larder-scrip.

![The Trades window: cooking recipes at the Golden Cask's kitchen. The row of trade bags is on the right.](ui-captures/warrior-26-cooking-recipes.png)

**Hunting for hides** (step 11, 1 October) [PUBLISHED]

> "this also means all animals are huntable for their leather" - you, 1 October

> "farm animals stay . not huntable" - you, 1 October

- Deer and rabbits are game. They graze, look up and bolt. Sneaking (Ctrl) gets you close. They give no experience and no coin: E at the body reads "Skin the body" and gives its hide.
- Wolves, boar, hounds and stags give hides the same way.
- Hens, sheep, cats and crows can never be targeted.
- Fen the skinner sells salt. Maud's other three bags can be earned with hides.

**Blacksmithing and the two-craft rule** (step 12, merged 1 October) [BUILT, NOT YET PUBLISHED]

- At a forge: five smelts (copper, bog-iron, ridge-steel, ash-steel and Veridian bars) and 21 pieces of gear, from a copper-shod cudgel to the Heartwood greatblade. Only a Blacksmith smelts.
- You may take up two crafts. You can forget one, with a confirmation.

**Alchemy** (step 13, merged 1 October) [BUILT, NOT YET PUBLISHED]

- At a herbalist's bench: five draughts, from the minor healing draught to the dewfern draught.

**Depth and tuning** (step 14) [PLANNED]

- Five elixirs, "Well fed" on seven foods, a smith who puts a piece on sale when you sell him six ore, and a tuning pass on prices.

## Loot

> "time to improve armor and weapons and each has a different visual appearance when worn. so we need to start building a database of loot and start catering to the people who love loot" - you, 1 October

**Items, before the loot plan** (29 September) [PUBLISHED]

- A 24-slot bag (I). A character sheet (C) with eight gear slots then (nine today). Merchants by trade who buy, sell and buy back. A loot list for each kind of mob. Gear made by the game from a level and a quality. Food and potions. Save format 6.

**Weapons in your hand** (loot step A1, 1 October) [PUBLISHED]

- Every weapon and shield has its own shape: short and arming swords, falchions, knives, cleavers, axes, clubs, maces, war hammers, staves, polearms; bucklers, round, kite and heater shields; lanterns and censers.
- Coloured by where it comes from. Better quality shows in the trim and a glowing accent. Slung on your back when you swim.

**Armour you can see** (loot step A2, 1 October) [PUBLISHED]

- Every head, neck, shoulder, chest, hand, leg and foot piece has its own shape: caps, hoods, helms, masks, crowns; pendants; mantles and pauldrons; tunics, hauberks, cuirasses, robes; gloves and gauntlets; breeches and greaves; shoes, boots and sabatons.
- Armour recolours what it covers and tucks hair under a helm. Today there are 54 families of shape with 146 variants and 10 colour palettes.

![Armour on the body: one common set from each level band, 2 to 13.](ui-captures/wardrobe/04-sets-front-a.png)

**The loot list drafted** (loot step A3, 1 October) [PUBLISHED]

- 104 named items across the five zones and the world, each with its look, its numbers, where it drops and a line of flavour. Lists for every boss and elite. Two sets. The roll rules.
- At this step nothing dropped yet. The pieces were shown on mannequins to be judged first.

**Loot that shines** (loot step L1, 1 October) [PUBLISHED]

- Loot is rolled when a mob dies and lies on the body. Beams by quality: a white twinkle for coins and junk, a green glint, a blue beam for rare, a purple beam with a turning ring for epic.
- E opens a loot window (Take all). What does not fit stays on the body.
- Tooltips compare against what you wear ("+9 weapon damage" in green, losses in red, "An upgrade"). Green arrows on better pieces. A call-out for every rare or epic.

![Loot beams on bodies by quality: junk, uncommon, rare and epic.](ui-captures/loot/01-beams-day.png)

**Named loot goes live** (loot step L2, 1 October) [PUBLISHED]

- The 104 named pieces drop from their mobs, elites and bosses. Each boss gives one piece of its list that you do not hold yet. A unique you hold is never offered twice. Set pieces count toward their bonus.
- Five merchants sell a named piece each. Rare gear from elites can come out epic.
- A new character starts with the Trailblade in hand.
- One honest gap: 15 of the 104 are marked as quest rewards, and the quests do not hand them over yet. That is loot step L4.

**The Armoury** (loot step L3, merged 1 October) [BUILT, NOT YET PUBLISHED]

- A tab that records every named piece and every look you have found, with the ones still to find in grey. A "NEW LOOK" call-out. Save format 9.
- Bad luck protection for epics is remembered between sessions.

**Loot steps L4 to L6** [PLANNED]

- L4: quests hand out their named gear (15 quests).
- L5: a turnable 3D figure on the character sheet, icons, and try-on.
- L6: cloaks, as a tenth gear slot.

## The screens

- **The HUD** (28 September on): frames for you, your target, Mira and friends; nameplates; an action bar (6 slots, then 10); an experience bar; a chat log; a swing timer.
- **The minimap** (28 to 30 September): the time with a sun or moon, the weather word, exits with their levels.
- **The zone map and world map** (M) (29 September): places, camps with their levels, roads, the painted map of Crulanda.
- **The quest book** (L) (29 September): Quests, Chronicle, Standing; Discoveries added 30 September; the Armoury tab is built and not yet published.
- **Bags** (I) and the **character sheet** (C) (29 September): drag to move, drag out to destroy; trade-bag rows added 1 October.
- **The Trades window** (K) (1 October): your skills, the recipes at a station, Make and Make all.
- **The talent window** (B) (28 September).
- **The merchant window, the loot window, conversation windows, tooltips** that compare gear.
- **HUD readability** (29 September): labels are placed so they do not overlap or cover the panels; dark outlines on text; road signs at exits from 60 m.

![The zone map of Oakhaven (M).](ui-captures/warrior-04-zone-map.png)

**Still to come for the screens:** real painted icons for abilities and items in place of letters (note 11). [PLANNED] The HUD is drawn for a 1440 by 900 screen and stretches on other shapes.

# What was changed or fixed

Changes and fixes, in the order they came. Things that were removed or replaced outright are in the next chapter.

## 28 September

- **Combat slowed down.** You said enemies died too quickly. Enemy health went from 110 to 240 (veterans 170 to 340). Your weapon swing from 1.8 s to 2.6 s. Strike's cooldown from 3 s to 5 s and its bonus damage from 16 to 12. Enemy swings from 2 s to 2.6 s. Mira's bolts from 3 s to 4 s. Fights then measured 20.8 s, 24.4 s and 30.8 s.
- **Scenery became solid.** Camp props, trunks, rocks and ruins now block you and the mobs.
- **Automatic attacks got a visible countdown.** Swings carry on between abilities. No separate auto-attack toggle, as you asked.
- **Seven faults fixed** when the foundation was first brought into Unity (ids that clashed with Unity's own, a death that could trigger twice, an overflow in the health sums, a map builder that could overwrite your scene, and three smaller ones).
- **Threat that depended on frame rate** was fixed.
- **A broken line in the encounter's data** had made Unity silently use default values for the class. Fixed.
- **Villagers coming out of a house** showed a hidden placeholder shape. Fixed.
- **Your save showed dead mobs from old progress.** The save was backed up and you started fresh.
- **Direction changed, at your word:** stop testing and balancing each skill; "the game needs to evolve first". Graphics, zones and maps from the lore came first from here on.

## 29 September

- **Zone travel was blocked.** You could not travel in the build of 14:07. The cause: a "phantom Wasting". Zones that have no Wasting (Khaven, the Peaks) were given a default one, with grey ground and an invisible wall across Khaven's east side and the Peaks' road east. Fixed. A new test now walks the player into every exit of every zone.
- **Travel is refused only by a real fight:** an enemy engaged within 40 m. The message names who is still after you.
- **Water lighting and banks:** reflections that fade under the horizon, a broken foam line, ripples that flow downstream, an uneven shoreline, creeks that wander.
- **Sawtooth shading on slopes** fixed.
- **The mill wheel** now sits on the creek. Weapons go on your back when you swim.
- **Night:** a moonlit blue floor to the light so night sides are not pure black.
- **Fix round for the zone looks** (7 items): Khaven's roads and grass, Peaks pines on steep ground, darker Peaks rock, thinner Ash Rim cracks, a wider Wasting curtain.
- **Floating and clipping props** fixed: the inn sign, the tannery beam, the bakehouse's woodstack, house doors, rails on slopes, stacked crates, ruin beams, crows' wings.
- **A straight seam in the sky** behind a faded tree, fixed.
- **Enemy nameplates** centred over their bars. Labels kept off the HUD panels.
- **The hen coop** moved out of the village centre.

## 30 September

- **Losing your character on a road.** On a temp-save run every zone made a new save folder, so level, gear and talents started fresh. Fixed: one folder for the whole session. Real saves were never affected.

> "talents reset when I zoned into the Ashland Rim" - you, 29 September

- **Creature fix round:** the Weave-Eater as 30 curved threads round a core; the Pale's deep cowl; crows that fold their wings on the ground; Rockhide Wallow as a wet pool; the Great Oak's crown in layers.
- **Weather, before it shipped:** seven faults found on the first tour (a yellow band on the horizon, sunny-looking overcast, lightning far too bright, pale wet ground, faint rain, water glowing under rain, storms that never came for some seeds). Then ten more found by a second reviewer reading the code (clouds that jumped, rain that flickered near walls, shadows on fogged land, and others). All fixed.
- **The cave's first build** had a fault that broke every Oakhaven load, and a slab floating over the tunnel. Fixed before it shipped.
- **A kill step's line** printed the mob's internal name as the speaker. It is now narration.
- **The Peaks outrider's axe head** floated off its haft. Fixed.
- **Going down the dungeon:** a gap between the cave floor and the road, and the fall-catch that sent you back to the village on the way down. Fixed.
- **Zones grown:** villagers keep their work and walks within 125 m of the village. Maps draw at about 4 pixels a metre.
- **Found by tests on the grown zones:** ground drawn magenta by a misplaced line (never shipped); Khaven's flood pools too shallow; a secret on an unreachable shelf and another on an island of ground. Fixed.
- **Your real save was touched by a test.** Two throwaway tests loaded and re-saved it. Only its format number and your height in the world changed. It was restored byte for byte from the backup. Since then a test run can never touch the real save folder.
- **On the Verdant Shore:** a river that cut under its own pool, a lookout on a 66-degree wall, a flattened ledge behind the waterfall. Fixed.

## 1 October

- **A review before the first publish of the day** found a dozen real faults the tests had missed. Among them: a new name rule would have turned every Grey wolf in Oakhaven and Khaven into an ash hound; the hens' water never showed; the finale quest never gave its page and could be finished from the surface; the Root-Mother's face was above the cave roof. All fixed.
- **The trades' day:** the village's stock is the day's (cleared before dawn); nobody could draw water with a collector seven metres from the well, so villagers now keep 7 m from enemies, not 12.
- **The painted pass, first tour:** roof textures smeared up the slope; rock joints made cliffs read as dry-stone walls. Fixed.
- **Round 1's full check:** the Crisp cottage's door could not be reached; held weapons at life size read as twigs. Weapons are now drawn 1.35 times life size and shields 1.15 times.
- **21 faults** were found and fixed in review before round 1 was merged.
- **Herbs** were smaller than the meadow flowers. They became knee-high clumps, then a third larger than life.
- **The hauberk's surcoat** became wider, slit at the hem and belted.
- **Khaven's Cracked Hearth:** nobody could walk into the taproom. The door's frame was fixed.
- **Vendor item names** in dark ink; common names had been faint grey.
- **Dewfern** was rebuilt; its fronds ran into the ground.
- **The "empty hands" start was reversed:** a new character first started with nothing in hand; one publish later it starts with the Trailblade again. Existing saves were not touched.
- **Game animals** bolt only to ground they can reach. This is the fix for the rabbit that stood and watched.
- **Tests that were wrong, not the game** (found and corrected): an armour test that skipped the whole body; a workshop test with bad timing; a skill-up test with the wrong number; a hearth test with Mira in the way.
- **The long check runs with no window** on your desktop, so closing a window cannot kill it.

## 2 October (first minutes)

- **Mourner's cap in Khaven:** two clusters on the creek bank were sunk with their small caps buried. Fixed. Three test checks on the save were tightened. [BUILT, NOT YET PUBLISHED]

# What was taken out

Almost nothing was deleted as a file. Git records only three deleted files in the whole history. Nearly everything here was replaced in place: the old thing went and a better one took its spot. Nothing published was ever rolled back.

## Looks that were replaced

{table: widths=27,33,10,30; size=8}
| What it was | What replaced it | When | Why |
| **Water** | | | |
| A grey pier under every bridge | Removed | 29 Sep | It showed as a slab on the water |
| A uniform ring of foam; then foam bands and blotches | Foam only where water touches something | 29 Sep | It looked wrong |
| Chrome-like water in Khaven | A softer reflection set per zone | 29 Sep | The visual review: "Gloom Creek is chrome" |
| Water you could not see into | See-through water with depth | 29 Sep | Your words: "more water-like, more see-through" |
| Ripple rings drawn as white discs | A real ring | 29 Sep | A fault |
| The realistic see-through look | Stylized hand-painted turquoise water | 29 Sep | Your art direction, from the cove picture |
| A planned mirror reflection for water | Dropped before it was applied; its file deleted | 29 Sep | It clashed with the painted direction |
| **Trees and plants** | | | |
| Dead trees with five thin sticks at the base | A root flare | 29 Sep | They read as sticks laid round the base |
| Trunks like grey pipes with a collar | Tapered, leaning trunks with roots, six bark shades | 29 Sep | You said the trunks looked bad |
| The Great Oak as a dead tree of cylinders | A living centrepiece, about 21 m tall | 29 Sep | Your words: "this tree looks terrible" |
| Crowns as smooth balls; pines as solid cones | Painted leaf cards on boughs | 30 Sep | Your words: "trees still look bad" |
| The first leaf cards (thin pines) | A fuller second pass the same day | 30 Sep | The first tour showed it |
| Broadleaf crowns reading as one brown ball | Crowns built of leafy lumps | 1 Oct | The second visual review |
| Dead trees that looked like umbrellas | Bent, tapering trunks with crooked limbs | 30 Sep | Your remark |
| Mourner's cap drawn as yellow flowers | Grey mushroom caps | 1 Oct | The quest calls it a black-gilled mushroom |
| **Rock, cliffs and ground** | | | |
| Umbra Scarp as stepped blocks | Faceted rock | 30 Sep | It got worse first: a wall of grey slabs. Redone the same night |
| Cliffs as smooth slabs with flat tops | Stacked rock lumps with ragged crests | 30 Sep | "Cliffs as rock, not slabs" |
| The Ash Rim as tan sand with "worm trail" cracks | Grey ash with angular cracks | 29 Sep | The visual review |
| The Ash Rim's crust reading as paving | Drifted ash over broken crust | 1 Oct | The second visual review |
| The Peaks as grassy hills | Real mountains | 29 Sep | The visual review |
| The Wasting's curtain like glass | A fog-coloured sheet with crawling bands | 29 Sep | A hard line against the sky |
| A brown plane or a void past the exits | Hills and tree lines past every edge | 29 Sep | You could see the world's edge |
| The Silent Statue as a stretched cliff, then stacked shapes | One turned figure, ten metres tall | 30 Sep | It read as a column with things on it |
| **Buildings and props** | | | |
| Plain walls and roofs | Painted plaster, thatch, slate, stone and timber | 1 Oct | The painted style pass |
| Plain fences, carts, lamps, wells, stalls, barrels | Chunky hand-made ones | 1 Oct | The painted style pass |
| Plain towers, walls, crypts and ruins | Painted masonry | 1 Oct | The painted style pass |
| Gable ends drawn as roof | Gable ends that are wall | 1 Oct | The second visual review |
| Ruins as plain boxes | Broken outlines | 1 Oct | The second visual review |
| Buildings floating on slopes | Stone footings with steps | 1 Oct | Review item 10 |
| Plain inn fronts and a forge under a roof | Hero inns and a hearth-house smithy | 1 Oct | Review item 11 |
| Crowsfoot's entrance as a rock on the grass | A slot in a cliff, hidden until the last bend | 1 Oct | Your words: it "stick out as just a rock" |
| Plain cave walls | Painted rock, and earth packed with roots | 1 Oct | The second visual review |
| **Light** | | | |
| Night sides rendered pure black | A moonlit blue floor to the light | 29 Sep | The visual review |
| Night as bleached white on mud | Firelit windows on a moonlit blue base | 1 Oct | The second visual review. You: "lights look better" |
| Khaven in ordinary daylight | A permanent dusk | 29 Sep | The visual review |
| **Creatures and gear** | | | |
| A Weave-Eater shaped like a dog | A drifting tangle of threads, no head, no legs | 29 to 30 Sep | The book: it is not a dog |
| The Pale with a floating head | An attached head under a deep cowl | 29 to 30 Sep | The visual review |
| Cats, chickens, rabbits and crows with no legs | Legs, paws and toes | 29 Sep | Your words: "cats have no legs" |
| Copper ore as orange lumps with green beads | Faceted red-brown ore with flat green flecks | 1 Oct | Your words: "what are the green peas/circles on the ore?" |
| Weapons drawn at life size | 1.35 times life size (shields 1.15) | 1 Oct | They read as twigs |
| The class's fixed sword and shield always in hand | Your hands show what you wear | 1 Oct | The point of visible gear |
| A pale glow on worn rare and epic gear | A glow about twice as strong (round 6) | 1 Oct | It did not show through the bloom |
| **The HUD** | | | |
| Labels and speech bubbles on top of each other and the panels | Labels placed as a set, stacked, off the panels | 29 Sep | The visual review |
| The zone name and hint always on screen | They fade after 14 seconds | 29 Sep | Clutter |

## Code and systems that were replaced

{table: widths=30,34,10,26; size=8}
| What it was | What replaced it | When | Why |
| The nine-talent Warrior prototype, written in code | Talents loaded from data (27 playable) | 28 Sep | So trees can be written, not coded |
| The calculator's generated placeholder talents (793) | Hand-written trees (875 proposed talents) | 28 Sep | The placeholders were templates |
| An early "Build Workshop" page | The talent calculator page | 28 Sep | It did not match your reference |
| Combat rules written only for the Warrior | Rules any class can use | 28 Sep | The Druid |
| The old experience curve | A new curve; old saves are carried over | 29 Sep | The ladder to level 10 |
| Save formats 1 to 8 | Each replaced by the next; old saves upgrade on load | 28 Sep to 1 Oct | Nothing is lost |
| The phantom Wasting in Khaven and the Peaks | Gone | 29 Sep | It blocked travel |
| A new save folder on every zone (temp-save runs) | One folder per session | 30 Sep | You lost your character on the road |
| Weather drawn from a weak random source | A better one | 30 Sep | One seed went 600 spells without a storm |
| A "Grey" name rule for Verdant creatures | Removed | 1 Oct | It turned Grey wolves into ash hounds |
| Homes dealt out to villagers by counting | Named households | 1 Oct | Your words: "each npc has their own house" |
| Loot handed over as a chat line | Loot on the body and a loot window | 1 Oct | Nothing is lost to full bags any more |
| The stand-in that showed the loot list on mannequins only | The live loot; its file deleted | 1 Oct | Named loot went live |
| The loot files in their own folder | Moved beside the items | 1 Oct | They load after the items |
| Zip-file checkpoints | Git, plus the backup on J: | 29 Sep | There was no git on the machine before |
| Prepared patch files for visual fixes | Branches with review and merge | 1 Oct | Patches broke when the code moved |
| A visible window for the long check | No window | 1 Oct | Closing it would kill the run |

## Content that was cut, renamed or moved

{table: widths=30,36,10,24; size=8}
| What | The change | When | Why |
| The inn's name, the Whispering Barrel | Renamed the Golden Cask | 29 Sep | It is the book's name |
| Oakhaven's erasure as something you see | Not shown: the game is set before it | 29 Sep | Your decision |
| The same-frame opening Strike plus a free weapon hit | Removed | 28 Sep | Combat was too fast |
| The hen coop in the village centre | Moved out | 29 Sep | |
| The North road that ran to the edge and stopped | It ends at Crowsfoot Hollow; later, short of it, with a track | 30 Sep, 1 Oct | |
| Khaven's Fallen Smithy | Moved off the inn's sight line and out of a doorway | 30 Sep | |
| Two secrets in places you could not reach | Moved | 30 Sep | Found by the tests |
| Zone sizes of 260 m (Khaven 240 m) | 380, 340, 360 and 360 m | 30 Sep | Your words: zones "do need to be bigger" |
| Level cap 10 | Level cap 13 | 30 Sep | The Verdant Shore |
| "The Tin Crown" at level 3: 110 experience, 20 gold | Level 4: 190 experience, 25 gold | 30 Sep | The cave became a dungeon |
| Caddock as an elite of level 4 | An elite of level 5 | 30 Sep | The same |
| The dungeon's band back after 90 seconds | Back after 5 to 15 minutes | 30 Sep | So the way stays clear |
| The sap pool in the middle of the Root-Mother's passage | Against the east wall | 1 Oct | It cut the way |
| The cold in the root coming back after 90 seconds | It stays gone | 1 Oct | The review |
| Rich copper seams inside the deserters' camps | Moved to clear walls | 1 Oct | Not inside a camp |
| Ore seams beside the Root-Warden | Moved 15 m or more away | 1 Oct | Not boss-room loot |
| Maud at the tannery yard all day | Her own leather shop | 1 Oct | A workshop for each trade |
| Hides and boar meat as junk | Materials for Maud and for cooking | 1 Oct | Trade bags and cooking |
| Deer and rabbits as scenery | Game you can hunt | 1 Oct | Your words on hunting |
| Boss trophies dropping every kill | "One you do not hold yet" | 1 Oct | So they cannot be farmed in stacks |
| A new character starting empty-handed | Starts with the Trailblade | 1 Oct | Reversed within a day |

## Behaviour that changed

{table: widths=36,42,10,12; size=8}
| It used to | It now | When | Asked by |
| Let you walk through scenery | Scenery is solid | 28 Sep | You |
| Pull the camera in at tree trunks | Trees fade; the camera stays out | 29 Sep | You |
| Refuse travel for any fight anywhere | Refuse only for an enemy engaged within 40 m | 29 Sep | You |
| Let mobs notice you through rock and walls | Mobs need a clear line of sight | 30 Sep | The cave |
| Let you use things through rock | Nothing is used through rock | 30 Sep | The dungeon |
| Let you talk through walls | Walls block talk (round 6, not yet published) | 1 Oct | Polish |
| Catch a fall at 5 m under the ground | Catch it 5 m under the deepest cave | 30 Sep | The dungeon |
| Stop the rain when the camera brushed a wall | "Indoors" needs a roof overhead | 30 Sep | The weather review |
| Lose loot when bags were full | What does not fit stays on the body | 1 Oct | Loot plan |
| Sell a trade bag at a quarter price on right-click | Right-click wears it | 1 Oct | Review |
| Tour every zone on every round | Skip the tours on rounds that do not change the look | 1 Oct | You |
| Test and balance each skill | Stopped; the world comes first | 28 Sep | You |
| Let an editor test run touch the real save | Never | 30 Sep | The save incident |

## What was decided against

{table: widths=30,34,36; size=8}
| The idea | What happened | The reason given |
| Moving the game to Unreal | You asked on 1 Oct. The answer was no. [DECLINED] | The art, not the engine, is the limit. A port rewrites everything. A trial of a newer Unity lighting pipeline was offered as the cheap experiment; you did not ask for it. This is recorded in one line of the handoff, not in your own words. |
| Moving to Unity's newer render pipeline (URP) | Put off | The project stays on the built-in renderer for now |
| Six technologies in the brief (among them online play, a new input system, a new UI system) | Put off on purpose, 28 Sep | Each needs a shown need first |
| Realistic water with a mirror | Dropped, 29 Sep | Your painted direction |
| A separate auto-attack toggle | Not added, 28 Sep | Your instruction: show the countdown instead |
| The Copper-Tithe as the cave's bandits | Rejected, 29 Sep | They are sympathetic in the books. Sandthrone deserters were chosen |
| A book name for the Bandit King | Not used | The lore has no bandit king. Caddock is made for the game and labelled so |
| Copying names or art from other games | Never | A standing rule. Reference pictures are style only |
| Shipping all 20 classes | Not decided | 20 are candidates; the target of up to 15 is unresolved |
| Showing the calculator's sums as damage numbers | Declined, 28 Sep | It would be a fake simulation |
| A skinning skill | No, 1 Oct | A hide comes from searching the body |
| Low skill blocking a gathering node | No, 1 Oct | Gathering is free for everyone |
| Anyone smelting ore | No, 1 Oct | Only a Blacksmith smelts |
| A third craft | No, 1 Oct | Your choice: two crafts at most |
| Hunting farm animals | Never, 1 Oct | Your words: "farm animals stay . not huntable" |
| Saving the village purses | No, 1 Oct | So a restart can never load a stuck economy |
| Rare spawns, chests that roll for your weakest slot, salvage, weapon speeds, two-handers, sockets | Cut from the first loot plan | Listed there as cut or for later |
| Unity's own light baking for the caves | Cannot be used | Zones are built when they load. The zone builder must do the baking itself (note 10) |

## Still to be retired, in your words

These are asked for and not yet done. Each is in the playtest notes chapter.

- **The block characters** (note 12): "time to retire the block characters".
- **The muted colours** (note 13): "need high fantasy not pale".
- **The letter icons** (note 11): "can't tell what anything is".
- The sheep (note 4), the green on the ore (note 5), the stick-like windfall (note 6), the cat's stiff tail (note 7).

# How the work is done

## The steps, from your words to your build

1. **Your words are written down first,** word for word, in an owner-notes file in the plan folder. Nothing is designed until they are.
2. **A design panel.** Readers map the code. Three designers each write a design from a different angle. One writer merges them and says what was taken from each. For trades the three were called Lean, Classic and Village. For loot they were appearance, database and thrill.
3. **A critique.** Reviewers check the plan's claims against the real code. The plan lists what was checked and what was not.
4. **Questions for you come with a default.** If you do not answer, the default is built. The defaults told to you so far: the leatherworker is Maud, with Fen the skinner and Nettie as her family; a hide comes from searching the body; only a Blacksmith smelts; low skill never blocks a node; empty gear slots show empty; cloaks come last.
5. **The plan is cut into small steps** that can each be published. Trades has 14 steps. Loot has 9.
6. **Several engineers build at once,** each on its own branch in its own copy of the project. Round 1 had three; later rounds three or four.
7. **Review through three lenses.** Then a second reviewer tries to prove each finding wrong. Only findings that survive are fixed. Round 1 had 21 real faults fixed this way before anything was merged.
8. **Merge, one branch at a time,** onto a round branch.
9. **The full check** (below).
10. **Look.** The pictures from the check are opened and judged. This finds what tests cannot, such as weapons that read as twigs.
11. **Publish:** copy the build to your `Crulanda-Playable` folder, but only if the game is not running.
12. **Write it down:** the changelog entry with your words at the top, and the handoff's resume point.
13. **Commit and back up** to J:.
14. **Tell you** at each publish.

On 1 October this cycle ran six times in one day.

## The full check before a publish

One script runs the whole check with no window, so nothing can interrupt it.

1. Copy the project's assets into the validation copy.
2. Run every EditMode and PlayMode test. PlayMode loads every zone many times and is given up to an hour.
3. Build the Windows game. If the build fails, stop: never photograph an old build as if it were the new one.
4. Tour all five zones in the built game and take pictures of every place, node, station and weather.
5. Count shader errors in the build log. The count must be zero, because a broken shader fails silently.
6. Take the HUD pictures, the wardrobe line-up of every piece of gear, and the loot pictures.
7. Write a log and a "done" file.

Your rule of 1 October: skip the zone tours on rounds that do not change how the world looks.

A quicker check runs only the named tests, in about three minutes, before the full one.

## How visual work is judged before it is built

- **The first visual review** (29 September): one reviewer went through every tour picture, a second re-checked each fault. 81 confirmed, 7 rejected. Oakhaven 27, Khaven 21, the Peaks 15, the Ash Rim 18.
- **The painted pass** (1 October): the first draft was checked by four agents before anything was built. They drew the planned textures and looked at them, re-did the geometry sums, and tried to refute each other's findings. Verdict on the draft: the textures did not tile and did not look painted. It was redone, then applied one part at a time, with tests and a tour after each of five parts.
- **The second visual review** (1 October): four reviewers ranked 14 items. All 14 are done.
- **Gear** is lined up in a wardrobe picture and judged before any of it drops.

## The rules

- Every file made for the game goes inside `D:\code\mmo`. The game is never committed to the other repo in `D:\code`.
- After a meaningful piece of work: commit, then back up.
- Never run Unity against your open project. Tests and builds run in the validation copy.
- Back up your save before any change to the save format.
- Tests never touch your real save.
- Keep every file's Unity id, so nothing in a scene loses its link.
- Label lore honestly: CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL.
- Do not copy names or art from other games.
- You skim. A decision is asked first and short, or by the question prompt.
- Farm animals are never huntable.
- Zone layouts stay put. New art must not move a tree, a rock or a creek. Tests guard this.
- Never edit the game's assets while a check is running.
- Publish only when the game is not running.
- Be candid: say what is proposed and what is measured. No claim that every class is playable.
- Finish each step (fix, test, picture, check, commit, publish, back up) before the next.
- Work in order of importance, and keep going when you are away. You gave that authority on the evening of 30 September, with the safety rules still holding.

## Where everything lives

{table: widths=30,70; size=8}
| What | Where |
| The project and its git repository (branch main) | `D:\code\mmo` |
| The Unity project | `D:\code\mmo\New Unity Project` |
| The documents | `D:\code\mmo\New Unity Project\Docs` |
| This PDF | `D:\code\mmo\New Unity Project\Docs\Crulanda-Everything-Built-So-Far.pdf` |
| What makes this PDF | `D:\code\mmo\tools\docs` (`make_history_pdf.py` and `history_content.md`) |
| The plans | `D:\code\mmo\tools\wip` (trades, loot, the painted pass) |
| The checking scripts | `D:\code\mmo\tools\validation` |
| The backup | `J:\claude\unity projects\mmo` (run `tools\Backup.ps1`) |
| The lore: novels, world bible, maps | `D:\code\crulanda` |
| Unity 6000.6.3f1 | `D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe` |
| The validation copy, where tests and builds run | `C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation` |
| **The build you play** | `C:\Users\chris\Documents\Codex\2026-09-28\hel\outputs\Crulanda-Playable\Crulanda.exe` |
| **Your save** | `C:\Users\chris\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter` |
| Save backups | `C:\Users\chris\Documents\Codex\2026-09-28\hel\work\save-backups` |
| Tour pictures | `...\hel\work\world-captures` |
| HUD, wardrobe and loot pictures | `...\hel\work\ui-captures` |
| A copy of the handoff | `...\hel\outputs\Crulanda-Claude-Handoff.md` |
| The talent calculator and class matrix | `...\hel\outputs` |
| A first draft of Steam store text | `D:\code\mmo\marketing\steam-blurb.md` |

## Backups and your save

- **The backup to J:** adds to what is there. J: is slow, about 2 MB a second; a full copy takes about ten minutes. If J: is missing, you are told. Once, after a reboot, J: came up as a different, nearly empty drive; that was reported, you moved the files back, and the next backup worked.
- **Your save** is one file with a spare copy beside it. It is written to a temporary file first and then swapped in. A save made by a newer game is refused by an older one. Saving in combat is refused.
- **Old saves upgrade when read.** There have been nine formats. Each one is listed in Appendix H.
- **Your save is backed up before every format change.** There are ten backup folders:

{table: widths=42,58; size=8}
| Backup folder | Taken |
| `20260928-201321` | 28 Sep, the first |
| `20260928-213908-pre-v3` | before format 3 |
| `20260929-pre-v4` | before format 4 |
| `20260929-020630-pre-v6` | before format 6 |
| `20260930-1739-before-format7` | before format 7 (secrets) |
| `20260930-2205-written-by-diagnostic` | the copy a test wrote by mistake, kept as evidence |
| `20261001-1545-before-format8` | before format 8 (trades) |
| `20261001-2050-before-round3` | before round 3 |
| `20261001-2235-pre-loot` | before named loot went live. Your save was still format 6 on disk then |
| `20261001-2315-before-format9` | before format 9 (the Armoury) |

- **The one incident** (30 September): two throwaway tests loaded and re-saved your real save. Only the format number and one height value changed. It was restored byte for byte from the backup taken that afternoon. The cause was that the validation copy shares the game's save folder. Now any test run in the editor is sent to a throwaway folder.

## Who and what did the work

- Every commit is authored as you. 152 of the 171 carry a co-author line: Claude Opus 5.5 on 109, Claude Fable 5.1 on 43. The first four have none.
- The helpers inside each build round are not named anywhere.
- The tools: Unity in the background for tests, builds and art; PowerShell scripts; Python for patches, texture previews and node placement; git with worktrees.
- Commits by day: 20 on 29 September, 20 on 30 September, 130 on 1 October, 1 so far on 2 October. 23 of them are merges.

## Lessons that became rules

{table: widths=50,50; size=8}
| What went wrong | The rule or fix that followed |
| Tests re-saved your real save | Test runs go to a throwaway folder |
| A failed build left the old game in place, which could have been photographed as new | The tour is skipped if the build fails |
| A broken shader falls back without a word | Count shader errors in every build log |
| A time limit killed Unity part-way through a long run | Long runs are started detached and left alone |
| The run's window showed on your desktop | It runs with no window |
| A quick test run gave two false failures | It now sets up the game's data first, as the full run does |
| Script ids changed on every sync | The 16 missing id files were added |
| Editing assets during a run changed what the run tested | Never edit assets while a run is going |
| J: came up as the wrong drive after a reboot | The backup stops and says so |

# Your notes from playing

You started giving notes as you played on the evening of 1 October. They are logged in `Docs\PLAYTEST_NOTES.md` with your words, what was measured, and what the fix needs. They come before any other work.

Status in the notes file at the time of writing: 13 open, 1 to keep. None is in a published build yet.

{table: widths=4,36,20,28,12; size=8}
| # | Your words | What was measured | What the fix needs | Status |
| 1 | "one thing i notice is the bandit camp is way too close to the village. i asked before to expand the zone." | The first deserters stand 93 m from the green. The cave mouth is 100 m out: a 15 to 20 second walk from the Great Oak. | Grow Oakhaven again. Move all of Crowsfoot Hollow out, mouth about 280 m from the green. Nothing hostile within about 120 m of any village. | [OPEN] Being built on a branch: Oakhaven at 560 m. Not merged. |
| 2 | "elite was too easy for the loot obtain." and "bandit king way too easy for the loot obtained" | Caddock has 528 health and hits for about 25, alone, with no move of his own. An elite is a normal mob with 2.2 times the health and 1.4 times the hit. There are twelve. | More health and a harder hit. One or two moves each. Guards who join. Dungeon bosses harder than outdoor beasts. | [OPEN] Being built on a branch. Not merged. |
| 3 | "some mobs need to be more social. i can pull them easy 1 at a time even when they stand next to each other" | Each mob notices you alone, within 5 m. | Pack beasts come together. People call those in earshot. Solitary beasts stay single. Sneaking still peels the edge. | [OPEN] Being built on the same branch as note 2. |
| 4 | "fix the sheep. looks like bugs on sticks" | One pale blob, a black ball for a head, four thin black legs. | A woolly fleece, short sturdy legs, a dark face with ears, a tail, a grazing head. Then a line-up picture of every animal. | [OPEN] |
| 5 | "less green in the ore" | Seven lumps in ten still carry a bright green patch. | A hint of patina only. | [OPEN] One commit on a branch. Not merged. |
| 6 | "lumber trees . trunk just has 4-5 stick stickup up. make it more broken/chopped down looking" | Four thin stub limbs, two pointing up, a plain log, a small stump. | A torn stump, a heavy trunk with snapped boughs, the litter of a fall. | [OPEN] |
| 7 | "cats tails need to be more flexible" | The tail is one stiff rod. | A tail in segments that curves, sways and flicks. | [OPEN] |
| 8 | "lights look better" | The new night: firelit windows and lamps on a moonlit blue base. | Nothing. Do not lose it. | [KEEP] |
| 9 | "bloom sun effects?" | Bloom and sun shafts exist but are tuned low and do not show in play. | A sun you can see, a warm halo, glare, shafts, glints on water, without washing out the colours. | [OPEN] |
| 10 | "need baked lighting in the caves. more atmospheric" | Caves are lit evenly, with a few live lights. | Light baked by the zone builder from each torch and fire, blocked by walls. Haze. Shafts at the mouth. | [OPEN] |
| 11 | "need set icons for the hot bar. items in bags. not just letters and colors. can't tell what anything is" | Abilities are three letters. Items are two letters on a colour. | A painted icon for every ability, talent and item. | [OPEN] |
| 12 | "armor and weapons look good but armor is way too blocky. needs to feel flowing. time to retire the block characters" | Every figure is built in code from boxes, capsules and balls. | Smooth bodies, cloth that hangs and moves, armour that follows the form. | [OPEN] Needs your choice of approach. |
| 13 | "more colors as well seems like all is muted pallets..need high fantasy not pale" | Gear colours, villagers' clothes and much of the world are muted earth tones. | Jewel tones, heraldic contrast, gold and blued steel. Each zone keeps its mood. | [OPEN] |
| 14 | "give me a pdf of all items/process/builds/additions/subtractions to this whole game since we started" | | This document. | [OPEN] in the notes file. Answered by this PDF. |

Notes 12 to 14 are dated 2 October in the notes file; they were logged at 23:58 on 1 October.

# What is being built now and next

## Now

- **Round 6 is waiting to be published.** Blacksmithing, Alchemy, the two-craft rule, the Armoury and the polish items are merged. Your save was backed up before format 9. A full check was run on this build late on 1 October and again from 00:06 on 2 October. The result is not written down yet. [BUILT, NOT YET PUBLISHED]
- **Note 1** is on the branch `fix/n1-oakhaven-grows`: Oakhaven grown to 560 m, Crowsfoot Hollow moved out into the north hills, new ground dressed (foothills, Thornshaw, Lark Hill). Seven commits. [IN PROGRESS]
- **Notes 2 and 3** are on the branch `fix/n23-social-elites`: social aggro by kind, elites and bosses with moves of their own, a balance table. Four commits. [IN PROGRESS]
- **Note 5** is on the branch `fix/n56-ore-windfall`: ore with only a hint of patina. One commit. Note 6 is not started there yet. [IN PROGRESS]
- Work folders are open for notes 4, 9 and 11, with nothing committed yet.

## Next

- The remaining playtest notes: 4, 6, 7, 9, 10, 11, 12, 13. Note 12 needs your choice of approach first. [PLANNED]
- Trades step 14: elixirs, "well fed" meals, the smith's daily piece, a tuning pass. [PLANNED]
- Loot L4: quests give their named gear. L5: a turnable figure and icons. L6: cloaks. [PLANNED]

## Decisions waiting on you

- **How to replace the block characters** (note 12).
- **The store title:** "The Quiet Trail" or "The Land of Crulanda". And whether *The First Spoke* is out.
- **The class roster:** 20 candidates; the target of up to 15 is unresolved.

## Known limits today

- There is no sound.
- The clock and the weather are not saved; each launch starts at 08:30.
- Villagers, hens and eggs only live while you are in their zone. The village's stock and purses are not saved.
- The HUD is drawn for one screen shape and stretches on others.
- Every figure is made of simple shapes, with no made models or animation. Windows are flat glowing panes.
- Only the Warrior and the Druid can be played. 49 of their 104 designed talents are not playable yet.
- From the brief's big plan, still not started: simulated adventurers who level and group with you (only Mira exists), and group play.

# Where this record is uncertain

- **28 September has no git.** The repository begins at 02:14 on 29 September with the whole prototype as one commit. Seven early changelog entries carry no date. Their order is known; their hours are not.
- **The count of 24 publishes** could be off by one or two. Four are called published only by the handoff; one says in its own commit that it was not yet published.
- **Round 6** is on the main branch. No commit, changelog entry or handoff line says it was published, and the playable folder's files are dated 23:04, which is round 5. A check of round 6 has run; its result is not recorded. This document treats round 6 as built and not yet published.
- **The fix branches** for notes 1, 2, 3 and 5 are not merged. How complete they are is not known from the record.
- **Test numbers** before 29 September come from the documents. From 29 September they are counts of tests in the code, which matched the reported results at every publish where both exist. No result is recorded for the 499 tests at the latest commit.
- **The Unreal question** is recorded in one line of the handoff, in summary, not in your words.
- **The earlier assistant** is not named in any document.
- **The inventory in the appendices** was counted by a script from the game's data files at commit 9dfaa46 (1 October, 23:58). The one later commit changed no data. Which camps belong to each dungeon is worked out from the zone notes, not from a field in the data.
- **Named loot from quests:** 15 pieces are marked as quest rewards in the loot files, but only Maud's four bag quests hand out an item today. The rest wait for loot step L4.
- **Some documents are out of date** and were not used as fact: the lower half of the handoff and most of the known-issues file describe the game of 28 September.

# Appendix A. The zones: places, camps, secrets, people

Everything in the appendices was counted by a script from the game's own data files, as they stood at 23:58 on 1 October 2026 (the main branch, round 6 included). Nothing was counted by eye.

Places are named landmarks. Nodes are spots to gather ore, timber or herbs.

{table: widths=18,6,7,7,7,8,7,8,7,10,7,8; size=8}
| Zone | Size (m) | Levels | Places | Props | Camps | Elites | Secrets | Nodes | Villagers | Named folk | House- holds |
| Oakhaven | 380 | 1-2 | 32 | 188 | 18 | 3 | 12 | 28 | 20 | 3 | 16 |
| Khaven Village | 340 | 3-5 | 20 | 147 | 10 | 2 | 8 | 28 | 6 | 3 | 5 |
| The Shattered Peaks | 360 | 6-8 | 14 | 96 | 9 | 2 | 8 | 28 | 0 | 4 | 0 |
| The Ashland Rim | 360 | 9-10 | 16 | 129 | 9 | 2 | 8 | 28 | 0 | 4 | 0 |
| The Verdant Shore | 360 | 11-13 | 12 | 119 | 15 | 3 | 6 | 28 | 0 | 7 | 0 |
| **Total** |  | 1-13 | 94 | 679 | 61 | 12 | 42 | 140 | 26 | 21 | 21 |



## Oakhaven

*An eastern farming village at the edge of the Wasting.*

- Lore label: CANON-EXPANDED. Size 380 m across, levels 1-2.
- Weather (how often, by weight): clear 2, fair 3, windy 1.5, overcast 2.5, rain 3, storm 1.2
- The Wasting's grey curtain stands along its east side.
- Ways out (2): West road to Khaven Village; South road to the Ashland Rim (dangerous, 9-10)
- Roads (13): North road, West road, Road into the grey, South road, Mill road, Harrow track, Mill lane, Brook track, Downs track, Ridge path, Barrow path, Hollin lane, Crowsfoot track
- Creeks and rivers (1): Oak creek
- Lakes and pools (2): Brook pond (9 m radius, 2.8 m deep), Withy pool (7 m radius, 1 m deep)
- Fields (13): West furrows [soil], Stubble field [stubble], North furrows [soil], Last harvest [stubble], Creek field [soil], Harrow furrows [soil], Harrow stubble [stubble], Brook field [soil], South furlong [soil], Hollin home field [soil], Hollin east field [stubble], Long acre [soil], Last field [stubble]
- Clearings 11, shaped pieces of ground 14, tall-grass patches 8
- Groves (20): broadleaf 10, pine 5, dead 3, orchard 2. Names: The Old Orchard, Harrow wood, North pines, South copse, Grey-edge copse, Ridge pines, Hill hazels, Mastwood, Whitefoot's thicket, Cider orchard, Bound copse, Withies, Carr wood, Southwood, Hollin orchard, Greying oaks, Brook spinney, Grey wood, East pines, Crowsfoot thicket
- Named places (32): The Great Oak; The Golden Cask; Communal Well; The Wasting; Oak creek mill; Harrow farm; The Old Orchard; Old wayshrine; Harrow hen coop; Vell's smithy; Market row; Thorne's bakehouse; Tannery yard; Woodyard; The black-iron wagon; Brook pond; The tall grass; Crowsfoot Hollow; Crowsfoot Ridge; The Mastwood; Moss's lodge; The Old Fold; The Bound Stone; The Cider Barn; Withy pool; The Old Barrow; Hollin farm; Hallow's Creek milestone; The Watchtower; Carder farm; Tanner's leather shop; Lisbet's drying hut
- Things placed by hand (188), by kind: rock 25, tree 18, fence 14, house 14, crates 11, haystack 9, lamp 9, herb 8, barn 7, barrels 7, cart 7, grave 7, ruin 6, signpost 6, dead oak 4, coop 3, pine 3, stall 3, bridge 2, cliff 2, hedge 2, perch 2, cavern 1, crypt 1, dryhut 1, forge 1, gamerack 1, great oak 1, inn 1, kitchen 1, leathershop 1, mill 1, oven 1, ruined house 1, tannery 1, tower 1, wagon 1, wallow 1, wayshrine 1, well 1, woodpile 1
- Named buildings and works (46): The Great Oak (great oak); The Golden Cask (inn); Communal Well (well); Tithe barn (barn); Reed house (house); Farrow house (house); Crane house (house); The Elder's house (house); Rusk house (house); Jory's house (house); Vell house (house); Pell house (house); Thorne house (house); Tanner house (house); Mill footbridge (bridge); Old stone bridge (bridge); Oak creek mill (mill); Harrow farmhouse (house); Harrow barn (barn); Brook farmhouse (house); Brook barn (barn); Old wayshrine (wayshrine); Harrow hen coop (coop); West field hen coop (coop); Brook hen coop (coop); Vell's smithy (forge); Produce stall (stall); Cloth and pots (stall); Bread stall (stall); Thorne's bakehouse (oven); Tannery yard (tannery); Woodyard (woodpile); Crowsfoot Hollow (cavern); Moss's lodge (barn); Shepherd's hut (barn); The Cider Barn (barn); The Old Barrow (crypt); Hollin farmhouse (ruined house); Hollin barn (barn); The Watchtower (tower); Carder farmhouse (house); Crisp cottage (house); Tanner's leather shop (leathershop); Lisbet's drying hut (dryhut); The Cask's kitchen (kitchen); Moss's game rack (gamerack)
- Dungeon: Crowsfoot Hollow
- Things you can use (17): Bureau wagon [Search the black-iron wagon] x1; Tithe crate [Take back the confiscated iron] x3; Concord supply cart [Salt the collectors' supplies] x1; Harrow grain bin [Take a sack of clean grain] x1; Grey-hearted oak [Bind the trunk with vine-cord] x3; Yarrow [Gather yarrow] x8
- Story enemies placed by name (5): Concord collector (level 2), Concord collector (level 2), Concord warden (level 2) veteran, Concord collector (level 2), Concord collector (level 2)
- The zone's first goals: Find Mira inside the Golden Cask / Drive off the Concord collectors / Arm yourself with a recovered blade [I]

### Camps (18, holding 52 mobs)

{table: widths=27,27,8,9,17,12; size=8}
| Camp | Mob | Count | Levels | Notes | Comes back |
| Harrow wood wolves | Grey wolf | 4 | 1-2 |  | 75 s |
| South copse boars | Wild boar | 4 | 1-2 |  | 75 s |
| North pines wolves | Grey wolf | 4 | 2 |  | 75 s |
| Tall-grass stalkers | Grey wolf | 4 | 1-2 | ambush | 75 s |
| Brookside boars | Wild boar | 4 | 2 |  | 75 s |
| Hollow lookouts | Sandthrone deserter | 2 | 3 | above the zone's levels | 5 min |
| Deserters' camp | Sandthrone deserter | 4 | 3-4 | above the zone's levels | 8 min |
| King's guard | Sandthrone deserter | 3 | 4-5 | above the zone's levels | 10 min |
| Caddock's hall | Caddock, the Bandit King | 1 | 5 | elite, above the zone's levels | 15 min |
| Drop sentries | Sandthrone deserter | 2 | 3-4 | above the zone's levels | 8 min |
| Store Caves | Sandthrone deserter | 2 | 4 | above the zone's levels | 10 min |
| The Quartermaster's desk | Quartermaster Hesk | 1 | 4 | elite, above the zone's levels | 15 min |
| Deep Stair watch | Sandthrone deserter | 2 | 4-5 | above the zone's levels | 10 min |
| Mastwood boars | Wild boar | 4 | 1-2 |  | 75 s |
| Whitefoot's den | Old Whitefoot | 1 | 3 | elite, above the zone's levels | 4 min |
| Whitefoot's pack | Grey wolf | 3 | 2 |  | 75 s |
| Withy pool boars | Wild boar | 4 | 2 |  | 75 s |
| Hollin farm wolves | Grey wolf | 3 | 2 |  | 75 s |


### Hidden finds (12)

{table: widths=27,9,44,20; size=8}
| Hidden find | Kind | Pays | Needs |
| The Road's End | lookout | 25 XP |  |
| The Poacher's Cold Camp | cache | 30 XP, 5 gold, Poacher's Oilskin Hood |  |
| A Salt-Mender's Drop | cache | 25 XP, 6 gold, Minor healing draught |  |
| A Leaf from the Chapel Book | page | 30 XP, page "The Acorn Oath" |  |
| Moonbells | herb | 20 XP, Moonbell |  |
| A Key on the Drop | key | 15 XP |  |
| The Quartermaster's Strongbox | chest | 60 XP, 18 gold, Hesk's Shuttered Lantern, page "By Order of the King" | the key: "A Key on the Drop" |
| The Overlook | lookout | 35 XP |  |
| The Shepherd's Tin | cache | 30 XP, 4 gold, Harrow cheese |  |
| A Grave-Robber's Bundle | cache | 30 XP, 8 gold, Minor healing draught |  |
| A Letter from Hallow's Creek | page | 35 XP, page "A Letter from Hallow's Creek" |  |
| A Page from the Watch Book | page | 35 XP, page "The Watch Book" |  |


### Gathering nodes (28)

Copper seam x6 (mining, skill 1, gives Crowsfoot copper ore); Rich copper seam x4 (mining, skill 1, gives Crowsfoot copper ore); Windfall oak x8 (woodcutting, skill 1, gives Harrow oak log); Yarrow x10 (herbalism, skill 1, gives Meadow yarrow)

### Stations

- The zone's own: none
- At workplaces (5): The Golden Cask (fire, the hearth); Vell's smithy (forge); Thorne's bakehouse (fire); Lisbet's drying hut (bench); The Cask's kitchen (fire)

### People and animals

- Villagers: 20 (mood: wary)
- Named residents (3): Quill, Salt-Mender [stranger]; Warden Ivel, Preservationist Warden [warden]; Hob Linden, Innkeeper [innkeeper]
- Households (16, with 26 named members): Vell @ Vell house (Brannoc Vell [head]); Thorne @ Thorne house (Hedda Thorne [head]); Tanner @ Tanner house (Maud Tanner [head], Fen Walker [husband], Nettie [daughter]) (stipend 3); Rusk @ Rusk house (Ama Rusk [head]); Reed @ Reed house (Tamsin Reed [head]); Farrow @ Farrow house (Osk Farrow [head], Pim [son]); Crane @ Crane house (Lisbet Crane [head]); Ashby @ The Elder's house (Corwin Ashby [head]); Pell @ Pell house (Old Tobin [head], Edda Pell [wife]); Jory @ Jory's house (Jory [head]) (stipend 2, needs bread); Harrow farm @ Harrow farmhouse (Sel Harrow [head], Ilse Brandt [hand], Goody Marl [aunt]); Carder farm @ Carder farmhouse (Wil Carder [head], Hettie Brook [aunt]); Brook farm @ Brook farmhouse (Grete Lowe [head], Nan Pennock [lodger]); Crisp @ Crisp cottage (Aldo Crisp [head]); Moss @ Moss's lodge (Garet Moss [head]); The Golden Cask @ The Golden Cask (Hob Linden [head], Quill [lodger], Mira [lodger])
- Workshops owned (13): Brannoc Vell: Vell's smithy; Hedda Thorne: Thorne's bakehouse; Hedda Thorne: Bread stall; Ama Rusk: Produce stall; Tamsin Reed: Cloth and pots; Fen Walker: Tannery yard; Maud Tanner: Tanner's leather shop; Lisbet Crane: Lisbet's drying hut; Hob Linden: The Cask's kitchen; Hob Linden: The Golden Cask; Garet Moss: Moss's game rack; Osk Farrow: Woodyard; Aldo Crisp: Oak creek mill
- Animals (groups / animals): chicken 3/14, rabbit 8/36 (game you can hunt), crow 5/23, deer 5/14 (game you can hunt), sheep 2/13, cat 1/3

## Khaven Village

*A walled village of gallows and crypts beside the Whispering Wood.*

- Lore label: CANON-EXPANDED. Size 340 m across, levels 3-5.
- Weather (how often, by weight): fair 1, windy 1, overcast 3, mist 4, rain 2
- Ways out (2): Gloom road to Oakhaven; North road to the Shattered Peaks
- Roads (4): Gate road, Crypt path, North road, Corpse road
- Creeks and rivers (1): Gloom Creek
- Lakes and pools (3): Drowned graves (5.5 m radius, 0.85 m deep), Drowned field pool (west) (5 m radius, 0.8 m deep), Drowned field pool (east) (4.5 m radius, 0.8 m deep)
- Fields: none
- Clearings 10, shaped pieces of ground 7, tall-grass patches 16
- Groves (24): dead 18, pine 5, broadleaf 1. Names: The Whispering Wood, North pines, Cliffside scrub, Deep Whispering Wood, Gloom thicket, Ridge pines, Carrion scrub, Gloom alders, The Hush, Hush fringe wood, Whispering Wood (west), Gloom thicket (west), Gibbet scrub, Corpse-road scrub, Fever-field scrub, Drowned alders, Barrow scrub, Carrion scrub (east), Ridge pines (north), Ridge pines (east), Barrow thorns, Heights scrub (south), Fallen Watch scrub, Bound-wall pines
- Named places (20): Blood-Stone Well; The Gallows Tree; The Cracked Hearth; The Whispering Wood; The north road; Deep Whispering Wood; Carrion Cliffs; Drowned graveyard; Creek barrow; Outrider camp; The Gibbet Crossroads; The Corpse Road; The Plague Pit; The Drowned Fields; The Carrion Heights; The Charnel Barrow; The Old Bound Wall; The Fallen Watch; The Listener's Hut; The Hush
- Things placed by hand (147), by kind: rock 43, grave 18, ruin 13, fence 11, cliff 7, crates 7, herb 7, house 5, cart 4, lamp 4, signpost 4, tower 4, barrels 3, crypt 3, ruined house 3, bridge 2, gallows 2, brazier 1, dead oak 1, inn 1, stall 1, wall 1, wayshrine 1, well 1
- Named buildings and works (21): Gate tower (north) (tower); Gate tower (south) (tower); The Cracked Hearth (inn); Fallen Smithy (ruined house); Fallen Smithy (west) (ruined house); Blood-Stone Well (well); The Gallows Tree (gallows); Crypt-Keeper's Hovel (house); Grane house (house); Vey house (house); Jenn house (house); Tabor house (house); The Old Crypt (crypt); Gloom bridge (bridge); Chandler's stall (stall); Creek barrow (crypt); The Gibbet Crossroads (gallows); Corpse bridge (bridge); Pit shrine (wayshrine); The Charnel Barrow (crypt); The Listener's Hut (ruined house)
- Things you can use (11): Outrider saddlebags [Search the outriders' saddlebags] x1; Mourner's cap [Pick mourner's cap] x7; Stolen goods crate [Take back Khaven's goods] x3
- Story enemies placed by name (3): Sandthrone outrider (level 4), Sandthrone outrider (level 4), Pale watcher (level 5) veteran
- The zone's first goals: Keep Mira at your side [E] / Break the Sandthrone hold / Arm yourself with a recovered blade [I]

### Camps (10, holding 36 mobs)

{table: widths=27,27,8,9,17,12; size=8}
| Camp | Mob | Count | Levels | Notes | Comes back |
| Whispering Wood wolves | Grey wolf | 5 | 3-4 |  | 75 s |
| Carrion boars | Carrion boar | 4 | 3-4 | ambush | 75 s |
| Sandthrone outrider camp | Sandthrone outrider | 4 | 4-5 |  | 90 s |
| Gloom Creek hollows | Hollow Man | 4 | 5 |  | 90 s |
| The Grey Sexton | The Grey Sexton | 1 | 5 | elite | 4 min |
| Hush wolves | Grey wolf | 5 | 4-5 |  | 75 s |
| Plague pit hollows | Hollow Man | 4 | 4-5 |  | 90 s |
| Sandthrone picket | Sandthrone outrider | 4 | 5 |  | 90 s |
| Mire boars | Mire boar | 4 | 4-5 |  | 75 s |
| The Pale Reckoner | The Pale Reckoner | 1 | 7 | elite, above the zone's levels | 5 min |


### Hidden finds (8)

{table: widths=27,9,44,20; size=8}
| Hidden find | Kind | Pays | Needs |
| The Old Beacon | lookout | 60 XP |  |
| A Key Under the Gallows | key | 40 XP |  |
| The Outrider's Hoard | chest | 70 XP, 18 gold, Outrider's Hooked Knife, page "What the Captain Doesn't Count" | the key: "A Key Under the Gallows" |
| A Page from the Sexton's Register | page | 55 XP, page "The Low Row" |  |
| Widow's-Lamps | herb | 45 XP, Widow's-lamp |  |
| Where the Crows Wait | lookout | 70 XP |  |
| The Listener's Daybook | page | 55 XP, page "What the Wood Repeats" |  |
| Coins for the Hanged | cache | 45 XP, 16 gold, Healing draught |  |


### Gathering nodes (28)

Bog-iron seam x10 (mining, skill 20, gives Carrion bog-iron ore); Windfall black pine x8 (woodcutting, skill 20, gives Black pine log); Mourner's cap x10 (herbalism, skill 20, gives Mourner's cap)

### Stations

- The zone's own (1): Wenna Coyle's bench (bench)
- At workplaces (1): The Cracked Hearth (fire, the hearth)

### People and animals

- Villagers: 6 (mood: afraid)
- Villager names (8): Hollis Grane, Dorra Vey, Mattock Jenn, Siv Harl, Old Kestrel, Wynn Tabor, Brisa Thorn, Col Emmet
- Named residents (3): Wenna Coyle, Hedge-Witch [herbalist]; Ansel Morrow, Crypt-Keeper [elder]; Cato Brisk, Chandler [merchant]
- Households (5, with 7 named members): Grane @ Grane house (Hollis Grane [head]); Vey @ Vey house (Dorra Vey [head], Old Kestrel [father]); Jenn @ Jenn house (Mattock Jenn [head], Siv Harl [wife]); Tabor @ Tabor house (Wynn Tabor [head]); Morrow @ Crypt-Keeper's Hovel (Ansel Morrow [head])
- Animals (groups / animals): crow 8/34, rabbit 7/21 (game you can hunt), cat 1/2, deer 2/5 (game you can hunt)

## The Shattered Peaks

*The Sandthrone toll road over the eastern border mountains.*

- Lore label: CANON-EXPANDED. Size 360 m across, levels 6-8.
- Weather (how often, by weight): clear 2, fair 3, windy 2.5, overcast 2, flurries 3
- Ways out (2): Toll road down to Khaven Village; Toll road east to the Ashland Rim
- Roads (7): Toll road, Eyrie track, Tower path, Drove track, Ore road, Tarn path, Scree path
- Creeks and rivers: none
- Lakes and pools (1): The Cold Tarn (8 m radius, 2.2 m deep)
- Fields (1): Shieling hay meadow [stubble]
- Clearings 10, shaped pieces of ground 8, tall-grass patches 3
- Groves (13): pine 12, dead 1. Names: Wolf pines, South pines, High pines, East pines, Gate pines, Tower pines, Shieling pines, Ore-road pines, Tarn pines, Umbra pines, South-east pines, Ledge pines, Avalanche deadfall
- Named places (14): Pilgrims' Rest; The Toll Gate; Ruined waystation; Captain's eyrie; The High Ledge; Umbra scarp; Rockhide wallow; The Signal Tower; Goatherd's Shieling; The Sealed Adit; The Cold Tarn; The Listening Shrine; The Broken Post; Avalanche scar
- Things placed by hand (96), by kind: rock 29, cliff 15, ruin 11, crates 5, perch 4, tower 4, lamp 3, ruined house 3, signpost 3, woodpile 3, barrels 2, cart 2, house 2, wall 2, brazier 1, gallows 1, gate 1, keep 1, stall 1, wallow 1, wayshrine 1, well 1
- Named buildings and works (15): Pilgrims' Rest (house); Pilgrims' well (well); Pass-trader's stall (stall); Toll tower (east) (tower); Toll tower (west) (tower); Toll gate (gate); Toll-house (house); Toll gibbet (gallows); Captain's eyrie (keep); Eyrie lookout (tower); Ruined waystation (ruined house); The Signal Tower (tower); Goatherd's Shieling (ruined house); The Listening Shrine (wayshrine); The Broken Post (ruined house)
- Things you can use (6): Toll-house strongbox [Break open the toll strongbox] x1; Collapsed hearth [Search the collapsed hearth] x1; Pilgrim cairn [Lay a stone on the cairn] x3; Hollow waymarker [Search the hollow waymarker] x1
- The zone's first goals: Find the pilgrims' rest below the toll gate / Learn where the Sandthrone sent the children

### Camps (9, holding 34 mobs)

{table: widths=27,27,8,9,17,12; size=8}
| Camp | Mob | Count | Levels | Notes | Comes back |
| Toll-gate guards | Sandthrone toll-guard | 5 | 6-7 |  | 90 s |
| Wolf pines pack | Mountain wolf | 5 | 6-7 | ambush | 75 s |
| Rockhide wallow | Rockhide boar | 5 | 7 |  | 75 s |
| Captain's eyrie | Sandthrone captain | 1 | 8 | elite | 3 min |
| The High Ledge | Pale watcher | 3 | 8 |  | 2 min |
| Signal-tower pickets | Sandthrone picket | 4 | 6-7 |  | 90 s |
| Shieling wolves | Mountain wolf | 5 | 6-7 |  | 75 s |
| Scar rockhides | Rockhide boar | 5 | 7-8 |  | 75 s |
| Old Scree-Tusk | Old Scree-Tusk | 1 | 8 | elite | 3 min |


### Hidden finds (8)

{table: widths=27,9,44,20; size=8}
| Hidden find | Kind | Pays | Needs |
| Above the East Road | lookout | 110 XP |  |
| The Goat-Path Camp | cache | 100 XP, 22 gold, Mountain jerky, page "Six on the Goat Path" |  |
| A Silent Pilgrim's Echo-Jar | cache | 120 XP, Silent Pilgrim's Echo-Jar, page "What the Jar Remembers" |  |
| A Letter Never Sent | page | 90 XP, page "To Ysolde, by Any Caravan" |  |
| Frostbells | herb | 90 XP, Frostbell |  |
| The Picket's Skim | cache | 100 XP, 30 gold, Healing draught |  |
| Scored in the Mortar | page | 110 XP, page "The Foreman's Notice" |  |
| The Spur's End | lookout | 110 XP |  |


### Gathering nodes (28)

Adit iron seam x10 (mining, skill 40, gives Adit iron ore); Windfall stone-pine x8 (woodcutting, skill 40, gives Stone-pine log); Tarnwort x10 (herbalism, skill 40, gives Tarnwort)

### Stations

- The zone's own (2): Pass-trader's field anvil (forge); Pass cookfire (fire)
- At workplaces: none

### People and animals

- Villagers: 0 (mood: afraid)
- Named residents (4): Maddoc Vire, Keeper of the Rest [elder]; Yara Quell, Pass-Trader [merchant]; Tarsk, Ash-Walker Scout [hunter]; Hadrik Sull, Sandthrone Broker [stranger]
- Households: none
- Animals (groups / animals): crow 5/15, deer 3/9 (game you can hunt), rabbit 4/13 (game you can hunt), sheep 1/3

## The Ashland Rim

*Grey ash at the edge of the Wasting, where the Ash-Walkers hold the Eastern Ridge.*

- Lore label: CANON-EXPANDED. Size 360 m across, levels 9-10.
- Weather (how often, by weight): fair 1, windy 2, overcast 3, ash squall 3
- The Wasting's grey curtain stands along its east side.
- Ways out (3): Ridge road to the Shattered Peaks; North road to Oakhaven; Old west road over the mountains to the Verdant Shore (11-13)
- Roads (5): Ridge road, North road, Waste road, Orchard lane, Old west road
- Creeks and rivers: none
- Lakes and pools: none
- Fields: none
- Clearings 4, shaped pieces of ground 10, tall-grass patches 0
- Groves (16): dead 16. Names: Ashen snags, Grey thicket, Rim snags, South snags, Cinderfold orchard, The Last Orchard, West snags, Wayside snags, Rim-road snags, North snags, North-east snags, Ridge-back snags, Grey husks, Sea-floor snags, Pit snags, South-west snags
- Named places (16): Ash-Walker enclave; The Unwoven Flats; Tear-marked shrine; The old ribcage; Cinderfold; The brood; The Wasting; Wain's Rest; The Last Orchard; The Drowned Leviathan; The Ash Pit; The Walled Mouth; The Fraying; The Silent Statue; The Hunters' Knoll; The Reach-Stones
- Things placed by hand (129), by kind: rock 31, rib 20, ruin 17, barrels 9, cliff 9, idol 9, brazier 8, ruined house 8, crates 3, shelter 3, signpost 3, spine 2, brood 1, cave 1, monolith 1, perch 1, shrine 1, stall 1, wayshrine 1
- Named buildings and works (16): Ash-Walker caves (cave); Hide shelter (shelter); Hide shelter (shelter); Salt-trader's stall (stall); Purple-tear shrine (shrine); Cult hut (ruined house); Cinderfold house (ruined house); Cinderfold house (ruined house); Cinderfold house (ruined house); The Brine house (ruined house); Wain's Rest (ruined house); Wain's Rest stable (ruined house); Salt-road shrine (wayshrine); Orchard-keeper's house (ruined house); The Silent Statue (monolith); Hunters' hide (shelter)
- Things you can use (10): Purple-tear shrine [Topple the purple-tear shrine] x1; Stolen salt-cask [Take back the salt-cask] x3; Petrified rib [Chip off petrified bone] x6
- The zone's first goals: Find the Ash-Walker enclave on the Eastern Ridge / Earn the Salt of the First Sea

### Camps (9, holding 38 mobs)

{table: widths=27,27,8,9,17,12; size=8}
| Camp | Mob | Count | Levels | Notes | Comes back |
| Unwoven Flats eaters | Weave-Eater | 5 | 9-10 |  | 90 s |
| Ash hound pack | Ash hound | 5 | 9 |  | 75 s |
| Tear-marked shrine | Bone-masked cultist | 5 | 9-10 |  | 90 s |
| The Weave-Eater brood | Brood Weave-Eater | 2 | 10 | elite | 3 min |
| Cinderfold hollows | Hollow Man | 5 | 10 |  | 90 s |
| Ash-pit cultists | Bone-masked cultist | 5 | 9-10 |  | 90 s |
| The Ash-Deacon | The Ash-Deacon | 1 | 10 | elite | 4 min |
| Fraying eaters | Weave-Eater | 5 | 9-10 |  | 90 s |
| Orchard hounds | Ash hound | 5 | 9-10 |  | 75 s |


### Hidden finds (8)

{table: widths=27,9,44,20; size=8}
| Hidden find | Kind | Pays | Needs |
| The Salt Line | lookout | 180 XP |  |
| An Ash-Walker Cache | cache | 170 XP, Leviathan-Bone Harpoon |  |
| A Cultist's Hidden Letter | page | 160 XP, 30 gold, page "Ash in the Bread" |  |
| Last-Light | herb | 150 XP, Last-light |  |
| The Cinderfold Tin | cache | 160 XP, 45 gold, Salt-cured tonic |  |
| A Page from the Waybook | page | 170 XP, 20 gold, page "The Waybook at Wain's Rest" |  |
| A Bone-Carver's Bundle | cache | 180 XP, 25 gold, Leviathan-Tooth Charm |  |
| Last-Light in the Grey | herb | 160 XP, Last-light |  |


### Gathering nodes (28)

Cinder seam x10 (mining, skill 60, gives Cinder ore); Fallen ash-snag x8 (woodcutting, skill 60, gives Ash-snag wood); Cinder-thistle x10 (herbalism, skill 60, gives Cinder-thistle)

### Stations

- The zone's own (3): Oska's bone-anvil (forge); Mother Vane's salt-bench (bench); Enclave cookfire (fire)
- At workplaces: none

### People and animals

- Villagers: 0 (mood: afraid)
- Named residents (4): Chieftain Grohl, Ash-Walker Chieftain [warden]; Mother Vane, Salt-Speaker [herbalist]; Sefa Brine, Salt-Trader [merchant]; Oska, Harpoon-Carver [blacksmith]
- Households: none
- Animals (groups / animals): crow 8/26

## The Verdant Shore

*The Verdant Ocean of the western coast, where the Veridian Keepers keep the Shore of the First Cycle.*

- Lore label: CANON-EXPANDED. Size 360 m across, levels 11-13.
- Weather (how often, by weight): clear 3, fair 3, overcast 1.5, mist 2.5, rain 2.5, storm 1.5
- Ways out (1): Ash-road east over the mountains to the Ashland Rim
- Roads (5): The Ash-road, The Temple way, The Glade path, The Shore path, The Mere path
- Creeks and rivers (1): The Wending
- Lakes and pools (5): Mossveil pool (9 m radius, 2.4 m deep), The Mistmere (15 m radius, 2.2 m deep), Tidal pool (north) (4 m radius, 0.85 m deep), Tidal pool (middle) (5 m radius, 0.9 m deep), Tidal pool (south) (3.5 m radius, 0.8 m deep)
- Fields: none
- Clearings 3, shaped pieces of ground 22, tall-grass patches 10
- Groves (25): giant 15, broadleaf 7, dead 2, pine 1. Names: The Emerald Cathedral, Northwood, Rootfast canopy, Glade wood, Ridge-foot canopy, Riverbank canopy, Eastwood, Southwood, Fernwood, Mere canopy, Tappers' wood, Westwood, Far north wood, Far south wood, South-west wood, North-west wood, Mere-path wood, South-east ridge-foot wood, North glade wood, North ridge-foot canopy, Deep south wood, Westbank canopy, Ridge pines, The Greying, Palemist deadwood
- Named places (12): The Ridge of Long Shadows; The Verdant Ocean; Rootfast; The Veridian Temple; Mossveil Falls; The Mistmere; The Whispering Glade; The Fern Hollow; The Fallen Ghost-Oak; The Salt-Flats; The Greying; Palemist Hollow
- Things placed by hand (119), by kind: rock 29, mushrooms 23, giant tree 19, dead oak 8, lamp 7, rib 6, barrels 5, treehouse 5, cliff 4, bridge 2, perch 2, signpost 2, cavern 1, crates 1, fallen giant 1, shelter 1, waterfall 1, wayshrine 1, woodpile 1
- Named buildings and works (13): Alder-Knot's tree (treehouse); The Guest-Tree (treehouse); Willow-Whisper's tree (treehouse); The Hearth-Tree (treehouse); Moss-Lantern's tree (treehouse); Alder-Knot's carving-pile (woodpile); Rootfast footbridge (bridge); Reed-bed footbridge (bridge); Last salt-road shrine (wayshrine); Mossveil Falls (waterfall); The Fallen Ghost-Oak (fallen giant); Scout's lean-to (shelter); The Root-Mother's Deep (cavern)
- Dungeon: The Root-Mother's Deep
- Things you can use (11): Glowing caps [Gather glade-light from the glowing caps] x4; Sap-spout [Draw sap from the spout] x3; Salt-crust [Break off a cake of salt-crust] x4
- The zone's first goals: Find the Veridian Keepers at Rootfast / Learn what troubles the Shore

### Camps (15, holding 46 mobs)

{table: widths=27,27,8,9,17,12; size=8}
| Camp | Mob | Count | Levels | Notes | Comes back |
| Mistmere reed-boars | Moss-backed boar | 5 | 11 | ambush | 75 s |
| Antler Meadow stags | Moss-antler stag | 5 | 11-12 |  | 90 s |
| Fallen Ghost-Oak spiders | Canopy spider | 5 | 11-12 |  | 90 s |
| The Briar Way | Creeping briar | 5 | 12-13 |  | 90 s |
| The Greying | Withered Keeper | 4 | 12-13 |  | 90 s |
| Greyheart | Greyheart | 1 | 13 | elite | 4 min |
| Palemist mist-walkers | Mist-walker | 5 | 12-13 |  | 90 s |
| Palemist shadows | Pale shadow | 2 | 13 |  | 2 min |
| Old Ninebranch | Old Ninebranch | 1 | 13 | elite | 4 min |
| Root-stair briars | Creeping briar | 2 | 12-13 | above the zone's levels | 8 min |
| Gallery withered | Withered Keeper | 3 | 12-13 | above the zone's levels | 10 min |
| Sap Well walkers | Mist-walker | 3 | 13 | above the zone's levels | 10 min |
| Cold Stair briars | Creeping briar | 2 | 13 | above the zone's levels | 8 min |
| The Heart's withered | Withered Keeper | 2 | 13 | above the zone's levels | 10 min |
| The Root-Warden | The Hollow Root-Warden | 1 | 13 | elite, above the zone's levels | 15 min |


### Hidden finds (6)

{table: widths=27,9,44,20; size=8}
| Hidden find | Kind | Pays | Needs |
| Above Mossveil Falls | lookout | 210 XP |  |
| The Mossy Outcrop | lookout | 200 XP |  |
| The Tapper's Stump | cache | 200 XP, 30 gold, Sap-Tapper's Gloves |  |
| A Glass-Ship's Log | page | 190 XP, 25 gold, page "A Glass-Ship's Log" |  |
| Lantern-Moss | herb | 180 XP, Lantern-moss |  |
| A Pilgrim's Abandoned Pack | cache | 220 XP, 60 gold, Dewfern draught |  |


### Gathering nodes (28)

Veridian seam x6 (mining, skill 80, gives Veridian ore); Rich Veridian seam x4 (mining, skill 80, gives Veridian ore); Windfall ghost-oak x8 (woodcutting, skill 80, gives Ghost-oak heartwood); Dewfern x10 (herbalism, skill 80, gives Dewfern frond)

### Stations

- The zone's own (3): Alder-Knot's ember-stone (forge); Moss-Lantern's bench (bench); Hearth-Tree fire (fire)
- At workplaces: none

### People and animals

- Villagers: 0 (mood: keepers)
- Named residents (7): Willow-Whisper, Voice of the Wood [elder]; Moss-Lantern, Keeper of the Lanterns [merchant]; Alder-Knot, Bark-Carver [blacksmith]; Reed-Song, Keeper of the Mere [hunter]; Oak-Bane, Guardian of the Deep Wood [warden]; Sister Iselle, Silent Pilgrim [pilgrim]; Ondine Varro, Glass-Ship Scout [merchant]
- Households: none
- Animals (groups / animals): deer 4/11 (game you can hunt), rabbit 3/11 (game you can hunt), crow 2/7

## The twelve named elites and bosses

{table: widths=15,19,6,60; size=8}
| Zone | Name | Level | Its own pieces |
| Oakhaven | Caddock, the Bandit King | 5 | Caddock's Notched Cleaver [Rare], The King's Stolen Coat [Rare], Broken-Oath Sabre [Epic], Caddock's Tin Crown [Rare] |
| Oakhaven | Quartermaster Hesk | 4 | Hesk's Counting Gloves [Rare], The Quartermaster's Strongbox Lid [Rare] |
| Oakhaven | Old Whitefoot | 3 | Whitefoot's Winter Mantle [Rare], The Old Dog's Tooth [Rare], Den-Mother's Paw Wraps [Uncommon] |
| Khaven Village | The Grey Sexton | 5 | The Sexton's Spade [Rare], Shawl of the Uncounted [Rare], Mourner's Iron Band [Rare] |
| Khaven Village | The Pale Reckoner | 7 | Pane of the Final Sum [Rare], Gauntlets of the Cold Count [Rare], The Unpaid Debt [Epic] |
| The Shattered Peaks | Sandthrone captain | 8 | The Captain's Toll [Rare], Eyrie Cuirass [Rare], Signet of the Toll Road [Rare] |
| The Shattered Peaks | Old Scree-Tusk | 8 | Scree-Tusk's Broken Tusk [Rare], Scree-Hide Legguards [Rare], Rockfall [Epic] |
| The Ashland Rim | Brood Weave-Eater | 10 | Brood-Glass Ward [Rare], Threadcutter [Rare], Weave-Eaten Mantle [Rare] |
| The Ashland Rim | The Ash-Deacon | 10 | The Deacon's Censer [Rare], Cassock of the Last Sermon [Rare], Ash-Pit Treads [Rare], The Ember That Remembers [Epic] |
| The Verdant Shore | Greyheart | 13 | Greyheart's Withered Boughs [Rare], Ward of Living Heartwood [Rare], The Last Green Leaf [Epic], Greyheart's Salt-Wood Stave [Rare] |
| The Verdant Shore | Old Ninebranch | 13 | Crown of Nine Branches [Rare], The Old King's Hide [Rare], Tine of the Old King [Rare] |
| The Verdant Shore | The Hollow Root-Warden | 13 | Vigil Bark-Plate [Rare], The Warden's Root-Maul [Rare], The Root-Mother's Patience [Epic], The Root-Warden's Crown [Rare] |


# Appendix B. Every quest

**Factions.** Six are in the factions file: Oakhaven Folk, the Salt-Menders, the Preservationist Guild and the Ash-Walkers all start at 0; Sandthrone starts at -500; the High Concord starts at -4000. A seventh, the Veridian Keepers, comes with the Verdant Shore.

## Oakhaven: 16 quests

{table: widths=17,7,4,15,20,37; size=7.5}
| Quest | Kind | Lvl | From, to | Rewards | What you do |
| Two Hundred and Forty Souls | main | 1 | starts by itself, to Corwin Ashby | 60 xp, 15 gold, +250 Oakhaven Folk | Concord collectors hold Oakhaven. Find Mira, drive them off, and learn what their black-iron wagon is for. |
| The Miller's Boy | main | 1 | Corwin Ashby | 70 xp, +250 Oakhaven Folk | Speak with the Harrows and the miller, then ask Mira how a boy might be hidden from the Concord. |
| The Ruts Go West | main | 2 | Corwin Ashby, to Wenna Coyle | 70 xp, 12 gold, +150 Oakhaven Folk | Follow the collectors' ruts along the West road to Khaven Village and find out who took Wren and the soldier's boy. |
| Salt in the Tithe | faction | 1 | Quill | 60 xp, 10 gold, +500 Salt-Menders, +100 Oakhaven Folk | Spoil the Concord's supply cart on the east road so the next wagon is delayed. |
| A Dozen for the Oven | npc | 1 | Goody Marl, to Hedda Thorne | 25 xp, 4 gold, +100 Oakhaven Folk | Carry Goody Marl's eggs to Hedda Thorne at the bakehouse, west of the green. |
| Ash in the Flour | npc | 1 | Hedda Thorne | 45 xp, 8 gold, +150 Oakhaven Folk | Have the herbalist test Hedda's grey flour, then fetch clean grain from the Harrow barn. |
| Yarrow for Mira | npc | 1 | Lisbet Crane, to Mira | 40 xp, +100 Oakhaven Folk, +100 Preservationists | Gather five bunches of yarrow on the open meadows around the village and take them to Mira. |
| What the Deer Know | npc | 1 | Garet Moss | 40 xp, +100 Oakhaven Folk | Follow the deer tracks east toward the grey, then check the grey-edge copse. |
| Good Iron | npc | 2 | Brannoc Vell | 50 xp, 10 gold, +150 Oakhaven Folk | Take back three bars of confiscated iron from the tithe crates near the Concord wagon. |
| Sleep Now, Stone and Sky | npc | 1 | Corwin Ashby | 45 xp, page "Sleep Now, Stone and Sky", +200 Oakhaven Folk, +100 Preservationists | Collect the verses of Oakhaven's old lullaby from Goody Marl, Old Tobin and Hettie Brook. |
| Roots Before Ruin | faction | 1 | Warden Ivel | 55 xp, +500 Preservationists, +100 Oakhaven Folk | Bind the three grey-hearted oaks at the edge of the Harrow wood with living vine-cord, so the grey cannot spread through their roots. |
| The Tin Crown | side | 4 | Wil Carder | 190 xp, 25 gold, +300 Oakhaven Folk | Go down through Crowsfoot Hollow, the deserters' cave at the top of the North road, to the hall deep under the hills, bring down Caddock, their tin-crowned king, and recover what they stole from Oakhaven's farms. |
| A Wallet for Simples | npc | 1 | Maud Tanner | 20 xp, the Simples-wallet, +75 Oakhaven Folk | Bring Maud Tanner three grey wolf pelts. Wolves run in the Harrow wood and the North pines. She keeps shop by the South road, 9 to 12 and 2 to 6. |
| A Strap for the Woodyard | npc | 1 | Maud Tanner | 25 xp, the Log-sling, +75 Oakhaven Folk | Bring Maud Tanner three hill-deer hides. Deer graze west of the Old Orchard, in the Mastwood and on the slopes under Crowsfoot Ridge; go quietly (Ctrl) or they're gone. She keeps shop by the South road, 9 to 12 and 2 to 6. |
| The Cook's Scrip | npc | 2 | Maud Tanner | 25 xp, the Larder-scrip, +75 Oakhaven Folk | Bring Maud Tanner five coney skins. Rabbits run in every field: by Harrow farm, round Brook pond and out past the Cider Barn. She keeps shop by the South road, 9 to 12 and 2 to 6. |
| Ore Wants a Stout Bag | npc | 2 | Maud Tanner | 30 xp, the Ore-poke, +75 Oakhaven Folk | Bring Maud Tanner three boar hides. Wild boar root in the Mastwood and on the downs about Withy pool. She keeps shop by the South road, 9 to 12 and 2 to 6. |


**Chronicle pages:** Two Hundred and Forty Souls; Sleep Now, Stone and Sky; The Acorn Oath; By Order of the King; A Letter from Hallow's Creek; The Watch Book

**Quest items:** Bureau ledger; Goody Marl's eggs; Twist of flour; Sack of Harrow grain; Yarrow; Confiscated iron

## Khaven Village: 9 quests

{table: widths=17,7,4,15,20,37; size=7.5}
| Quest | Kind | Lvl | From, to | Rewards | What you do |
| Break the Sandthrone Hold | main | 3 | Wenna Coyle | 100 xp, 18 gold, +150 Oakhaven Folk, -100 Sandthrone | Drive the two Sandthrone outriders out of Khaven, then search their saddlebags outside the gate for proof of the handover. |
| What the Pale Thing Counts | main | 4 | Ansel Morrow | 120 xp, 22 gold, +100 Oakhaven Folk | Drive off the Pale watcher that has stood at the crypt since the wagon came, then walk the north road where Ansel saw the riders go. |
| The Toll Road North | main | 5 | Ansel Morrow, to Maddoc Vire | 150 xp, 30 gold, +100 Oakhaven Folk | Take the north road up into the Shattered Peaks and find the pilgrims' rest below the Sandthrone toll gate. |
| Grey at the Wood's Edge | npc | 3 | Wenna Coyle | 90 xp, 15 gold | Thin the grey wolves hunting the Whispering Wood beyond the walls. |
| Mourner's Cap | npc | 3 | Wenna Coyle | 90 xp, 15 gold | Gather five mourner's caps from the banks of Gloom Creek for Wenna's salves. |
| Tallow for the Dark | npc | 4 | Cato Brisk | 120 xp, 25 gold | Bring Cato five lumps of tallow from the carrion boars rooting under the Carrion Cliffs. |
| What the Outriders Took | npc | 5 | Cato Brisk | 150 xp, 32 gold, -150 Sandthrone | Raid the Sandthrone outrider camp beyond the north pines and take back the goods they carried off as 'toll'. |
| The Ones Who Came Back | npc | 5 | Ansel Morrow | 150 xp, 30 gold | Lay to rest the grey dead walking out of the drowned graveyard south of Gloom Creek. |
| The Grey Sexton | npc | 5 | Ansel Morrow | 160 xp, 35 gold | Destroy the Grey Sexton, the hollowed thing that keeps the creek barrow south of Gloom Creek. It is dangerous alone. |


**Chronicle pages:** Forty Sovereigns; The Low Row; What the Captain Doesn't Count; What the Wood Repeats

**Quest items:** Child's shackle; Mourner's cap; Carrion boar tallow; Khaven's goods

## The Shattered Peaks: 9 quests

{table: widths=17,7,4,15,20,37; size=7.5}
| Quest | Kind | Lvl | From, to | Rewards | What you do |
| The Toll-Book | main | 6 | Maddoc Vire | 220 xp, 45 gold, +150 Oakhaven Folk, -200 Sandthrone | Fight through the Sandthrone toll-guards at the gate and take the toll-book from the strongbox by the toll-house. |
| Under a Seeker's Seal | main | 7 | Maddoc Vire, to Tarsk | 260 xp, 55 gold, +150 Oakhaven Folk, +150 Ash-Walkers, -200 Sandthrone | Take the Seeker's warrant from the Sandthrone captain in his eyrie above the toll gate, then show it to Tarsk the Ash-Walker. |
| Salt of the First Sea | main | 8 | Tarsk, to Mother Vane | 300 xp, 60 gold, +250 Ash-Walkers | Follow the toll road down to the Ashland Rim and ask Mother Vane, the Ash-Walkers' Salt-Speaker, for salt to bind the seed in Oakhaven's well. |
| Stones for the Lost | npc | 6 | Maddoc Vire | 200 xp, 40 gold | Lay a stone on each of the three pilgrims' cairns along the toll road. |
| Wolves on the Mule Track | npc | 6 | Yara Quell | 220 xp, 45 gold | Cull six mountain wolves in the pines on the western slope. |
| Rockhide | npc | 7 | Yara Quell | 240 xp, 50 gold | Bring Yara five rockhides from the boars in the wallow on the southern slopes. |
| The Waystation Keeper | npc | 7 | Maddoc Vire | 240 xp, 50 gold | Search the ruined waystation on the lower road for its last keeper's journal. |
| The Dead-Drop | faction | 7 | Hadrik Sull | 240 xp, 70 gold, +600 Sandthrone, -400 Salt-Menders | Hadrik Sull will pay well for the Salt-Menders' letters hidden in a waymarker on the eastern descent. Taking this contract costs you standing with the Salt-Menders. |
| What the Pale Ones Watch | npc | 8 | Tarsk | 300 xp, 60 gold, +150 Ash-Walkers | Drive the Pale watchers off the high ledge above the east road, so Tarsk can see what they were watching. |


**Chronicle pages:** The Toll-Book; Under a Seeker's Seal; The Waystation Keeper's Last Page; Six on the Goat Path; To Ysolde, by Any Caravan; What the Jar Remembers; The Foreman's Notice

**Quest items:** Sandthrone toll-book; Seeker's warrant; Rockhide; Salt-Mender letters; Soot-black journal

## The Ashland Rim: 10 quests

{table: widths=17,7,4,15,20,37; size=7.5}
| Quest | Kind | Lvl | From, to | Rewards | What you do |
| Salt Is Not Given | main | 9 | Mother Vane, to Chieftain Grohl | 380 xp, 85 gold, +350 Ash-Walkers | Hunt Weave-Eaters on the Unwoven Flats with Ash-Walker salt, and come back alive to Chieftain Grohl. |
| The Tear-Marked Shrine | main | 9 | Chieftain Grohl | 400 xp, 90 gold, +350 Ash-Walkers | Break the bone-masked cultists at the Cult of Ash shrine in the south-west and topple their shrine. They have been stealing the Ash-Walkers' salt. |
| Heart of the Brood | main | 10 | Mother Vane | 450 xp, 100 gold, page "Salt of the First Sea", +400 Ash-Walkers | Destroy the Weave-Eater brood at the Wasting's edge and bring its heart-crystal to Mother Vane, so the salt is strong enough to bind a seed. The brood is dangerous alone. |
| Salt for the Well | main | 10 | Mother Vane, to Corwin Ashby | 500 xp, 120 gold, +500 Oakhaven Folk, +250 Ash-Walkers | Carry the Salt of the First Sea home by the dangerous north road, salt Oakhaven's Communal Well, and give what is left to Corwin Ashby. |
| Hounds on the Harpoon-Line | npc | 9 | Chieftain Grohl | 360 xp, 80 gold, +250 Ash-Walkers | Kill six ash hounds hunting in the dead snags west of the enclave. |
| White Shards | npc | 9 | Mother Vane | 380 xp, 85 gold, +250 Ash-Walkers | Gather six calcified shards from the Weave-Eaters you salt and shatter. |
| Leviathan Bone | npc | 9 | Oska | 380 xp, 85 gold, +250 Ash-Walkers | Chip five pieces of petrified bone from the old ribcage in the south for Oska's harpoons. |
| The Stolen Casks | npc | 10 | Sefa Brine | 420 xp, 95 gold, +250 Ash-Walkers | Take back three salt-casks the Cult of Ash carried off to their shrine in the south-west. |
| Cinderfold | side | 10 | Sefa Brine | 450 xp, 100 gold, +250 Ash-Walkers | Put down the Hollow Men in the ruins of Cinderfold, north-west of the enclave, and find the house with the carved fish. |
| The Old Salt Road | main | 11 | Chieftain Grohl, to Willow-Whisper | 520 xp, 120 gold, +250 Ash-Walkers, +250 Veridian Keepers | Take the old west road over the mountains to the Verdant Shore, and show Grohl's salt-cord to Willow-Whisper of the Veridian Keepers. |


**Chronicle pages:** The Tear-Marked Litany; Salt of the First Sea; Ash in the Bread; The Waybook at Wain's Rest

**Quest items:** Brood heart-crystal; Calcified shard; Salt-cask; Petrified bone; Salt of the First Sea

## The Verdant Shore: 12 quests

{table: widths=17,7,4,15,20,37; size=7.5}
| Quest | Kind | Lvl | From, to | Rewards | What you do |
| Let the Wood Learn You | main | 11 | Willow-Whisper | 540 xp, 125 gold, page "The Song of the First Cycle", +350 Veridian Keepers | Walk the Shore so the wood can learn you: the pool under Mossveil Falls, the Whispering Glade and the shore of the Mistmere. Take nothing. |
| The Briar Way | main | 12 | Willow-Whisper, to Oak-Bane | 600 xp, 140 gold, +350 Veridian Keepers | Find Oak-Bane on the old way to the Veridian Temple, cut back the creeping briars that have closed it, and look on the Temple's root-stair. |
| Grey at the Heart | main | 12 | Oak-Bane, to Willow-Whisper | 640 xp, 150 gold, +400 Veridian Keepers | In the Greying, a grove going grey in the north-east under the ridge, lay the withered Keepers to rest and take what is lodged in the heart of Greyheart, the eldest of them. Bring it to Willow-Whisper. |
| A Fog That Tastes of Lightning | main | 13 | Willow-Whisper | 720 xp, 170 gold, page "The Shore Remembers", +500 Veridian Keepers | Go down into Palemist Hollow under the Ridge of Long Shadows, break the mist-walkers and the pale shadows in the fog, and bring Willow-Whisper one of the black rods the walkers carry. |
| The Reeds Go Quiet | npc | 11 | Reed-Song | 520 xp, 120 gold, +250 Veridian Keepers | Drive off six moss-backed boars rooting up the reed-beds on the far shore of the Mistmere. They lie up in the reeds. |
| Antlers for the Carver | npc | 11 | Alder-Knot | 540 xp, 125 gold, +250 Veridian Keepers | Bring Alder-Knot five moss-crowned antlers from the stags of the Antler Meadow, north-west of Rootfast. |
| Silk for the Lanterns | npc | 12 | Moss-Lantern | 600 xp, 140 gold, +250 Veridian Keepers | Gather six skeins of web-silk from the canopy spiders nesting in the Fallen Ghost-Oak, west of Rootfast, for the grove's lantern-wicks. |
| The Three Notes | side | 12 | Sister Iselle | 600 xp, 140 gold, page "The Three Notes" | Gather three caps of glade-light in the Whispering Glade for Sister Iselle, a Silent Pilgrim who has been listening there for eleven days. |
| Green Gold | side | 12 | Ondine Varro | 580 xp, 260 gold, -300 Veridian Keepers | Draw three flasks of Veridian sap from the spouts in the old Ghost-Oaks east of the Salt-Flats for Ondine Varro of Port Caelum. The Keepers will not like it. |
| The Old King of the Deep Wood | npc | 13 | Oak-Bane | 720 xp, 165 gold, +300 Veridian Keepers | Put down Old Ninebranch, the great stag of the deep wood west of the Antler Meadow, before the grey in his antlers spreads to the herd. He is very dangerous. |
| Salt of the First Shore | side | 12 | Willow-Whisper, to Mother Vane | 560 xp, 130 gold, +400 Ash-Walkers, +100 Veridian Keepers | Break three cakes of salt-crust on the Salt-Flats where the Wending meets the sea, and carry them back over the mountains to Mother Vane on the Ashland Rim. |
| The Root-Mother's Deep | main | 13 | Willow-Whisper | 820 xp, 200 gold, +600 Veridian Keepers, page "What the Root-Mother Dreams" | Go down the Temple's root-stair into the Root-Mother's Deep, through the withered Keepers and the mist-walkers that have got in, bring down the Hollow Root-Warden in the Heart, and salt the cold that has lodged in the root. |


**Chronicle pages:** The Song of the First Cycle; The Three Notes; A Glass-Ship's Log; The Shore Remembers; What the Root-Mother Dreams

**Quest items:** Grohl's salt-cord; Glade-light; Moss-crowned antler; Skein of web-silk; Flask of Veridian sap; Greyheart's cold glass; Void-Husk; Salt of the First Shore

**In all:** 56 quests (19 main, 3 faction, 29 NPC, 5 side), 26 Chronicle pages, 28 quest items.

# Appendix C. Every item


There are 224 hand-made items: 120 everyday items in this appendix and the 104 named pieces in Appendix D. On top of these the game makes ordinary gear itself, from a level, a slot and a quality, so that gear is not listed anywhere (see Appendix G). The 28 quest items are listed with the quests in Appendix B.

## Older named gear (12)

{table: widths=12,34,13,6,28,7; size=8}
| Slot | Name | Quality | Lvl | Numbers | Value |
| head | Poacher's Oilskin Hood | Uncommon | 2 | 4 armor, 1 Sta, 2 Agi | 6 |
| head | Caddock's Tin Crown | Rare | 5 | 14 armor, 5 Str, 5 Sta | 24 |
| head | The Root-Warden's Crown | Rare | 13 | 34 armor, 6 Str, 9 Sta, 6 Spi | 70 |
| neck | Silent Pilgrim's Echo-Jar | Rare | 7 | 3 Sta, 7 Int, 4 Spi | 28 |
| neck | Leviathan-Tooth Charm | Rare | 10 | 6 Str, 7 Sta, 4 Agi | 42 |
| neck | Tine of the Old King | Rare | 13 | 7 Str, 9 Sta, 6 Agi | 52 |
| hands | Sap-Tapper's Gloves | Rare | 11 | 26 armor, 6 Sta, 7 Agi, 4 Spi | 40 |
| mainhand | Tempered Trailblade | Uncommon | 1 | 7 dmg | 5 |
| mainhand | Outrider's Hooked Knife | Rare | 5 | 17 dmg, 3 Str, 2 Sta, 3 Agi | 28 |
| mainhand | Leviathan-Bone Harpoon | Rare | 10 | 30 dmg, 8 Str, 6 Sta | 56 |
| mainhand | Greyheart's Salt-Wood Stave | Rare | 13 | 38 dmg, 7 Sta, 8 Int, 6 Spi | 66 |
| offhand | Hesk's Shuttered Lantern | Rare | 4 | 16 armor, 3 Sta, 5 Int, 4 Spi | 22 |


By slot: head 3, neck 3, hands 1, mainhand 4, offhand 1

## Hides (7)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Grey wolf pelt | Common | 1 | 2 | 10 | Maud works it; sold at the tannery |
| Ash-matted hide | Common | 1 | 5 | 10 | Maud works it; sold at the tannery |
| Moss-matted hide | Common | 1 | 7 | 10 | Maud works it; sold at the tannery |
| Dappled stag hide | Common | 1 | 8 | 10 | Maud works it; sold at the tannery |
| Coney skin | Common | 1 | 1 | 10 | Maud works it; sold at the tannery |
| Hill-deer hide | Common | 1 | 2 | 10 | Maud works it; sold at the tannery |
| Boar hide | Common | 1 | 2 | 10 | Maud works it; sold at the tannery |


## Junk (20)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Wolf fang | Poor | 1 | 1 | 10 |  |
| Boar tusk | Poor | 1 | 2 | 10 |  |
| Concord tithe token | Poor | 1 | 3 | 10 |  |
| Sandthrone toll coin | Poor | 1 | 4 | 10 |  |
| Frayed desert wrap | Poor | 1 | 2 | 10 |  |
| Petrified shard | Poor | 1 | 5 | 10 |  |
| Bone mask fragment | Poor | 1 | 6 | 10 |  |
| Static-glass shard | Poor | 1 | 8 | 10 |  |
| Pale filament | Poor | 1 | 7 | 10 |  |
| Filed company badge | Poor | 1 | 3 | 10 |  |
| Cracked hand-bell | Poor | 1 | 4 | 10 |  |
| Moss-green tusk | Poor | 1 | 6 | 10 |  |
| Velvet antler tine | Poor | 1 | 8 | 10 |  |
| Canopy-spider chitin | Poor | 1 | 7 | 10 |  |
| Pale-green venom gland | Poor | 1 | 9 | 10 |  |
| Briar thorns | Poor | 1 | 6 | 10 |  |
| Briar-heart knot | Poor | 1 | 9 | 10 |  |
| Grey bark | Poor | 1 | 9 | 10 |  |
| Cold trinket | Poor | 1 | 10 | 10 |  |
| Sliver of cold glass | Poor | 1 | 11 | 10 |  |


## Meat (5)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Tough boar meat | Common | 1 | 1 | 20 | the inn buys it; fits the larder-scrip |
| Lean wolf haunch | Common | 1 | 1 | 20 | the inn buys it; fits the larder-scrip |
| Ash-hound flank | Common | 1 | 4 | 20 | the inn buys it; fits the larder-scrip |
| Mossback chop | Common | 1 | 5 | 20 | the inn buys it; fits the larder-scrip |
| Shore venison | Common | 1 | 6 | 20 | the inn buys it; fits the larder-scrip |


## Foods (15)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Oakhaven brown loaf | Common | 1 | 1 | 20 | heals 150 |
| Oakhaven eggs | Common | 1 | 2 | 20 | heals 180; fits the larder-scrip |
| Harrow cheese | Common | 3 | 2 | 20 | heals 260; fits the larder-scrip |
| Mountain jerky | Common | 6 | 4 | 20 | heals 420 |
| Griddle bread | Common | 1 | 1 | 20 | heals 190 |
| Boar stew | Common | 1 | 3 | 20 | heals 220 |
| Harrow hearth-cake | Common | 1 | 2 | 20 | heals 200 |
| Wolf-haunch skewer | Common | 3 | 5 | 20 | heals 340 |
| Harrow pasty | Common | 4 | 5 | 20 | heals 380 |
| Pass-smoked loin | Common | 6 | 8 | 20 | heals 520 |
| Salt-baked hound flank | Common | 9 | 12 | 20 | heals 700 |
| Cinder-crust loaf | Common | 9 | 8 | 20 | heals 660 |
| Dewfern-glazed mossback chop | Common | 11 | 18 | 20 | heals 820 |
| Shore venison pie | Common | 12 | 16 | 20 | heals 900 |
| Sap-cake | Common | 10 | 5 | 20 | heals 640 |


## Potions and draughts (5)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Minor healing draught | Common | 1 | 3 | 10 | heals 90 |
| Healing draught | Common | 4 | 8 | 10 | heals 200 |
| Salt-cured tonic | Uncommon | 8 | 15 | 10 | heals 360 |
| Tarnwater draught | Common | 6 | 11 | 10 | heals 280 |
| Dewfern draught | Common | 11 | 18 | 10 | heals 520 |


## Rare healing herbs (hidden finds) (5)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Moonbell | Uncommon | 1 | 3 | 10 | heals 120 |
| Widow's-lamp | Uncommon | 3 | 6 | 10 | heals 250 |
| Frostbell | Uncommon | 6 | 10 | 10 | heals 400 |
| Last-light | Rare | 9 | 16 | 10 | heals 560 |
| Lantern-moss | Rare | 11 | 20 | 10 | heals 760 |


## Materials (ore, bars, wood, herbs, sundries) (24)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Crowsfoot copper ore | Common | 1 | 1 | 20 | the smith buys it; fits the ore-poke |
| Carrion bog-iron ore | Common | 1 | 2 | 20 | the smith buys it; fits the ore-poke |
| Adit iron ore | Common | 1 | 3 | 20 | the smith buys it; fits the ore-poke |
| Cinder ore | Common | 1 | 4 | 20 | the smith buys it; fits the ore-poke |
| Veridian ore | Common | 1 | 6 | 20 | the smith buys it; fits the ore-poke |
| Harrow oak log | Common | 1 | 1 | 20 | the smith buys it; fits the log-sling |
| Black pine log | Common | 1 | 2 | 20 | the smith buys it; fits the log-sling |
| Stone-pine log | Common | 1 | 3 | 20 | the smith buys it; fits the log-sling |
| Ash-snag wood | Common | 1 | 4 | 20 | the smith buys it; fits the log-sling |
| Ghost-oak heartwood | Common | 1 | 6 | 20 | the smith buys it; fits the log-sling |
| Meadow yarrow | Common | 1 | 1 | 20 | the stall buys it; fits the simples-wallet |
| Mourner's cap | Common | 1 | 2 | 20 | the stall buys it; fits the simples-wallet |
| Tarnwort | Common | 1 | 3 | 20 | the stall buys it; fits the simples-wallet |
| Cinder-thistle | Common | 1 | 4 | 20 | the stall buys it; fits the simples-wallet |
| Dewfern frond | Common | 1 | 6 | 20 | the stall buys it; fits the simples-wallet |
| Charcoal | Common | 1 | 1 | 20 | the smith buys it; fits the ore-poke |
| Mill flour | Common | 1 | 1 | 20 | fits the larder-scrip |
| Rim salt | Common | 1 | 1 | 20 | fits the larder-scrip |
| Stoppered vial | Common | 1 | 1 | 20 | fits the simples-wallet |
| Copper bar | Common | 1 | 4 | 20 | the smith buys it; fits the ore-poke |
| Bog-iron bar | Common | 1 | 7 | 20 | the smith buys it; fits the ore-poke |
| Ridge-steel bar | Common | 1 | 10 | 20 | the smith buys it; fits the ore-poke |
| Ash-steel bar | Common | 1 | 13 | 20 | the smith buys it; fits the ore-poke |
| Veridian bar | Common | 1 | 19 | 20 | the smith buys it; fits the ore-poke |


## Tools (2)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Miner's pick | Common | 1 | 2 | 1 | needed for Mining |
| Woodcutter's hatchet | Common | 1 | 2 | 1 | needed for Woodcutting |


## Trade bags (4)

{table: widths=30,12,6,8,8,36; size=8}
| Name | Quality | Lvl | Value | Stack | Notes |
| Simples-wallet | Common | 1 | 3 | 1 | 6 slots for herbs |
| Log-sling | Common | 1 | 4 | 1 | 6 slots for timber |
| Larder-scrip | Common | 1 | 5 | 1 | 8 slots for the larder |
| Ore-poke | Common | 1 | 6 | 1 | 8 slots for ore, bars and charcoal |


## Crafted gear (Blacksmithing) (21)

{table: widths=12,34,13,6,28,7; size=8}
| Slot | Name | Quality | Lvl | Numbers | Value |
| head | Bog-iron helm | Uncommon | 4 | 9 armor, 2 Sta | 15 |
| head | Ash-steel helm | Uncommon | 9 | 19 armor, 3 Sta | 30 |
| shoulders | Ridge-steel pauldrons | Uncommon | 7 | 15 armor, 3 Str | 24 |
| chest | Copper-scale jerkin | Uncommon | 1 | 6 armor, 1 Sta | 6 |
| chest | Bog-iron hauberk | Uncommon | 4 | 15 armor, 2 Str | 15 |
| chest | Ridge-steel cuirass | Uncommon | 7 | 24 armor, 3 Sta | 24 |
| chest | Ash-steel hauberk | Uncommon | 9 | 30 armor, 3 Sta | 30 |
| chest | Veridian breastplate | Uncommon | 12 | 39 armor, 4 Sta | 39 |
| hands | Copper-scale gauntlets | Uncommon | 1 | 4 armor, 1 Str | 6 |
| legs | Bog-iron greaves | Uncommon | 4 | 15 armor, 2 Sta | 15 |
| legs | Veridian legplates | Uncommon | 12 | 39 armor, 4 Str | 39 |
| feet | Ash-steel sabatons | Uncommon | 9 | 19 armor, 3 Agi | 30 |
| mainhand | Copper-shod cudgel | Uncommon | 1 | 7 dmg, 1 Str | 8 |
| mainhand | Bog-iron hatchet | Uncommon | 4 | 14 dmg, 2 Str | 21 |
| mainhand | Ridge-steel blade | Uncommon | 7 | 20 dmg, 3 Str | 34 |
| mainhand | Ash-steel cleaver | Uncommon | 9 | 25 dmg, 3 Str | 42 |
| mainhand | Veridian war-blade | Uncommon | 12 | 31 dmg, 4 Str | 55 |
| mainhand | Heartwood greatblade | Rare | 12 | 36 dmg, 5 Str, 5 Sta | 60 |
| offhand | Copper-banded buckler | Uncommon | 1 | 7 armor, 1 Sta | 6 |
| offhand | Ridge-steel shield | Uncommon | 7 | 28 armor, 3 Sta | 24 |
| offhand | Veridian tower shield | Uncommon | 12 | 46 armor, 4 Sta | 39 |


By slot: head 2, shoulders 1, chest 5, hands 1, legs 2, feet 1, mainhand 6, offhand 3

## What the merchants sell

- Any merchant (plus ordinary gear for the zone): Minor healing draught, Healing draught, Oakhaven brown loaf, Harrow cheese, Miner's pick, Woodcutter's hatchet, Mill flour, Rim salt, Stoppered vial
- Any blacksmith (plus ordinary gear for the zone): Miner's pick, Woodcutter's hatchet, Charcoal
- Any baker: Oakhaven brown loaf, Harrow cheese
- Any innkeeper: Oakhaven brown loaf, Harrow cheese
- Any leatherworker: Simples-wallet, Log-sling, Larder-scrip, Ore-poke
- Any skinner: Rim salt
- Any herbalist: Minor healing draught, Healing draught, Stoppered vial
- Moss-Lantern: Sap-cake, Dewfern draught, Salt-cured tonic
- Ondine Varro: Dewfern draught, Salt-cured tonic
- Sefa Brine, a named piece: Sefa's Cask-Head Shield
- Cato Brisk, a named piece: Cato's Tallow-Proofed Capelet
- Ama Rusk, a named piece: Market-Day Breeches
- Yara Quell, a named piece: Yara's Climbing Gloves
- Ondine Varro, a named piece: Ondine's Sap-Sealed Boots

## What each kind of mob drops (31 lists)

{table: widths=24,9,11,56; size=8}
| Dropped by | Levels | Gear chance | Other drops (chance, how many) |
| wolves | 1-10 | 12% | Grey wolf pelt (70%), Wolf fang (35%, 1-2), Lean wolf haunch (40%) |
| boars | 1-10 | 12% | Boar tusk (60%), Tough boar meat (50%), Boar hide (70%) |
| Concord collectors | 1-10 | 18% | Concord tithe token (60%), Minor healing draught (15%) |
| Sandthrone outriders and pickets | 1-10 | 18% | Sandthrone toll coin (60%), Frayed desert wrap (40%), Healing draught (10%) |
| Hollow Men | 1-10 | 10% | Petrified shard (70%) |
| bone-masked cultists | 1-10 | 20% | Bone mask fragment (60%), Salt-cured tonic (8%) |
| Weave-Eaters | 1-10 | 14% | Static-glass shard (70%) |
| Pale watchers | 1-10 | 25% | Pale filament (80%) |
| Sandthrone deserters | 1-10 | 14% | Filed company badge (60%), Minor healing draught (12%) |
| Quartermaster Hesk | 1-10 | 70% | Minor healing draught (100%, 1-2), Filed company badge (100%) |
| Caddock, the Bandit King | 1-10 | 70% | Caddock's Tin Crown (100%) |
| ash hounds | 1-10 | 12% | Ash-matted hide (70%), Ash-hound flank (45%) |
| Brood Weave-Eaters | 1-10 | 70% | Static-glass shard (100%, 1-3), Salt-cured tonic (30%) |
| the Ash-Deacon | 1-10 | 70% | Bone mask fragment (100%), Salt-cured tonic (50%) |
| toll-guards | 1-10 | 18% | Sandthrone toll coin (70%, 1-2), Frayed desert wrap (30%), Healing draught (10%) |
| the Sandthrone captain | 1-10 | 70% | Sandthrone toll coin (100%, 2-4), Healing draught (50%) |
| the Grey Sexton | 1-10 | 70% | Cracked hand-bell (100%), Petrified shard (50%) |
| moss-backed boars | 10-13 | 12% | Moss-matted hide (70%), Moss-green tusk (35%), Mossback chop (50%) |
| moss-antler stags | 10-13 | 12% | Velvet antler tine (60%), Dappled stag hide (50%), Shore venison (50%) |
| canopy spiders | 10-13 | 14% | Canopy-spider chitin (60%), Pale-green venom gland (30%), Dewfern draught (5%) |
| creeping briars | 10-13 | 14% | Briar thorns (70%, 1-2), Briar-heart knot (25%) |
| withered Keepers | 10-13 | 18% | Grey bark (70%), Dewfern draught (8%) |
| Greyheart | 10-13 | 70% | Greyheart's Salt-Wood Stave (100%), Grey bark (100%, 2-3) |
| mist-walkers | 10-13 | 20% | Cold trinket (60%), Dewfern draught (10%) |
| pale shadows | 10-13 | 25% | Sliver of cold glass (80%) |
| Old Ninebranch | 10-13 | 70% | Tine of the Old King (100%), Velvet antler tine (100%, 2-3), Dappled stag hide (100%) |
| the Hollow Root-Warden | 10-13 | 70% | The Root-Warden's Crown (100%), Grey bark (100%, 2-4), Sliver of cold glass (60%) |
| withered Keepers in the Deep | 10-13 | 18% | Grey bark (70%), Dewfern draught (8%) |
| mist-walkers in the Deep | 10-13 | 20% | Cold trinket (60%), Dewfern draught (10%) |
| rabbits (skinned) | 1-13 | 0% | Coney skin (100%) |
| deer (skinned) | 1-13 | 0% | Hill-deer hide (100%) |


# Appendix D. Named loot: all 104 pieces

Named loot is the hand-made gear: each piece has its own name, look, numbers and place it comes from. 49 are uncommon, 48 rare and 7 epic. 39 are unique (you can only hold one). 13 carry a special effect. By where they come from: 43 drop from a zone's ordinary mobs, 35 are boss pieces, 15 are quest rewards, 6 can drop anywhere in a level range, and 5 are sold by a merchant. Rules: an ordinary kill gives at most one named piece and an elite at most two; a boss gives one piece of its list that you do not hold yet; a unique you hold is never offered again.


## Oakhaven (21 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| Whitefoot's Winter Mantle | shoulders | Rare | 2 | 7 armor, 4 Sta, 4 Agi |  | Old Whitefoot (boss) | unique |
| The Old Dog's Tooth | neck | Rare | 2 | 3 Str, 3 Sta, 2 Agi |  | Old Whitefoot (boss) | unique |
| Den-Mother's Paw Wraps | hands | Uncommon | 2 | 6 armor, 2 Sta, 2 Agi |  | Old Whitefoot (boss) | unique |
| Hollin's Bent Pitchfork | mainhand | Uncommon | 1 | 7 dmg, 2 Str, 2 Sta |  | wolves |  |
| Mastwood Tusk-Guard | offhand | Uncommon | 1 | 7 armor, 1 Str, 3 Sta |  | boars |  |
| Brookside Waders | feet | Uncommon | 1 | 4 armor, 2 Sta, 2 Agi |  | boars |  |
| Collector's Turned Coat | chest | Uncommon | 1 | 6 armor, 1 Str, 3 Sta |  | quest "The Ruts Go West" |  |
| Brannoc's Good Iron | mainhand | Uncommon | 1 | 7 dmg, 3 Str, 1 Sta |  | quest "Good Iron" |  |
| Salt-Mender's Knotted Cord | neck | Uncommon | 1 | 2 Sta, 2 Spi |  | quest "Salt in the Tithe" |  |
| Ivel's Leaf-Stitched Cap | head | Uncommon | 1 | 4 armor, 2 Sta, 2 Agi |  | quest "Roots Before Ruin" |  |
| Market-Day Breeches | legs | Uncommon | 1 | 6 armor, 2 Sta, 2 Agi |  | sold by Ama Rusk |  |
| Company-Issue Falchion | mainhand | Uncommon | 3 | 12 dmg, 3 Str, 2 Sta |  | Sandthrone deserters |  |
| Sand-Scoured Half-Helm | head | Uncommon | 3 | 8 armor, 2 Str, 3 Sta |  | Sandthrone deserters |  |
| Lookout's Patched Breeches | legs | Uncommon | 3 | 12 armor, 2 Sta, 3 Agi |  | Sandthrone deserters |  |
| Hesk's Counting Gloves | hands | Rare | 3 | 10 armor, 3 Str, 4 Sta, 3 Agi |  | Quartermaster Hesk (boss) | unique |
| The Quartermaster's Strongbox Lid | offhand | Rare | 3 | 18 armor, 4 Str, 6 Sta |  | Quartermaster Hesk (boss) | unique |
| Caddock's Notched Cleaver | mainhand | Rare | 4 | 17 dmg, 8 Str, 5 Sta |  | Caddock, the Bandit King (boss) | unique, set piece |
| The King's Stolen Coat | chest | Rare | 4 | 19 armor, 5 Str, 8 Sta |  | Caddock, the Bandit King (boss) | unique, set piece |
| Boots of the Deep Stair | feet | Rare | 4 | 12 armor, 5 Sta, 6 Agi |  | Sandthrone deserters | unique, set piece |
| Broken-Oath Sabre | mainhand | Epic | 4 | 20 dmg, 9 Str, 7 Sta | Each kill restores 12 health. | Caddock, the Bandit King (boss) | unique |
| Harrow Wood Luck-Knot | neck | Rare | 2 | 2 Sta, 2 Agi | +10% luck on lucky drops. | any mob in Oakhaven |  |


**Set: The Deserter King's Due** (4 pieces): Caddock's Notched Cleaver, The King's Stolen Coat, Boots of the Deep Stair, Caddock's Tin Crown. Wearing them gives: with 2 pieces, +30 health; with 3 pieces, +6 attack power.

#### Drop chances

- **Old Whitefoot:** 50% for one of 2 signature pieces; 15% for the lucky piece
- **Quartermaster Hesk:** 100% for one of 2 signature pieces
- **Caddock, the Bandit King:** 100% for one of 3 signature pieces; 10% for the lucky piece (certain after 10 kills without it)
- **Wolves:** 3% for the lucky piece
- **Boars:** 3% for one of 2 lucky pieces
- **Sandthrone deserters:** 3% for one of 3 lucky pieces
- **Sandthrone deserters, levels 4-13:** 4% for the lucky piece
- **Any mob in the zone:** 0.4% for the lucky piece


## Khaven Village (17 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| Outrider's Bearded Axe | mainhand | Uncommon | 4 | 14 dmg, 4 Str, 3 Sta |  | Sandthrone outriders and pickets |  |
| Dune-Red Head Scarf | head | Uncommon | 4 | 9 armor, 3 Sta, 4 Agi |  | Sandthrone outriders and pickets |  |
| Picket's Hide Shield | offhand | Uncommon | 4 | 18 armor, 2 Str, 5 Sta |  | Sandthrone outriders and pickets |  |
| Gloom Creek Waders | feet | Uncommon | 4 | 9 armor, 4 Sta, 3 Agi |  | Hollow Men |  |
| Petrified Knuckle | mainhand | Uncommon | 4 | 14 dmg, 2 Str, 5 Sta |  | Hollow Men |  |
| Carrion-Hide Leggings | legs | Uncommon | 3 | 12 armor, 3 Sta, 2 Agi |  | boars |  |
| Hush-Wolf Pelt | shoulders | Uncommon | 4 | 9 armor, 3 Sta, 4 Agi |  | wolves |  |
| The Sexton's Spade | mainhand | Rare | 4 | 17 dmg, 6 Str, 7 Sta |  | The Grey Sexton (boss) | unique |
| Shawl of the Uncounted | shoulders | Rare | 4 | 12 armor, 5 Str, 8 Sta |  | The Grey Sexton (boss) | unique |
| Mourner's Iron Band | neck | Rare | 4 | 5 Str, 6 Sta |  | The Grey Sexton (boss) | unique |
| Pane of the Final Sum | offhand | Rare | 6 | 31 armor, 9 Str, 9 Sta |  | The Pale Reckoner (boss) | unique |
| Gauntlets of the Cold Count | hands | Rare | 6 | 17 armor, 9 Str, 9 Sta |  | The Pale Reckoner (boss) | unique |
| The Unpaid Debt | neck | Epic | 6 | 8 Str, 10 Sta, 4 Agi | +5% maximum health. | The Pale Reckoner (boss) | unique |
| Wenna's Wood-Edge Jerkin | chest | Uncommon | 3 | 12 armor, 3 Sta, 2 Agi |  | quest "Grey at the Wood's Edge" |  |
| Hold-Breaker's Maul | mainhand | Uncommon | 3 | 12 dmg, 3 Str, 2 Sta |  | quest "Break the Sandthrone Hold" |  |
| Morrow's Road Greaves | legs | Rare | 4 | 19 armor, 5 Str, 6 Sta |  | quest "The Toll Road North" |  |
| Cato's Tallow-Proofed Capelet | shoulders | Uncommon | 4 | 9 armor, 4 Sta, 3 Agi |  | sold by Cato Brisk |  |


#### Drop chances

- **The Grey Sexton:** 50% for one of 2 signature pieces; 15% for the lucky piece
- **The Pale Reckoner:** 50% for one of 2 signature pieces; 4% for the lucky piece (certain after 25 kills without it)
- **Sandthrone outriders and pickets:** 3% for one of 3 lucky pieces
- **Hollow Men:** 3% for one of 2 lucky pieces
- **Boars:** 3% for the lucky piece
- **Wolves:** 3% for the lucky piece


## The Shattered Peaks (17 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| Toll-Keeper's Billhook | mainhand | Uncommon | 6 | 18 dmg, 5 Str, 4 Sta |  | toll-guards |  |
| Toll-Gate Kettle Helm | head | Uncommon | 6 | 13 armor, 3 Str, 6 Sta |  | toll-guards |  |
| Signal-Tower Mitts | hands | Uncommon | 6 | 13 armor, 4 Sta, 5 Agi |  | Sandthrone outriders and pickets |  |
| Mule-Track Hobnails | feet | Uncommon | 6 | 13 armor, 5 Sta, 4 Agi |  | wolves |  |
| Shieling Crook | mainhand | Uncommon | 6 | 18 dmg, 5 Sta, 2 Int, 2 Spi |  | wolves |  |
| Rockhide Jerkin | chest | Uncommon | 7 | 24 armor, 4 Str, 7 Sta |  | boars |  |
| Watcher's Cold Lens | neck | Rare | 7 | 4 Str, 7 Sta, 6 Agi |  | Pale watchers |  |
| Spaulders of Cold Glass | shoulders | Rare | 7 | 19 armor, 8 Str, 9 Sta |  | Pale watchers |  |
| The Captain's Toll | mainhand | Rare | 7 | 25 dmg, 12 Str, 8 Sta |  | Sandthrone captain (boss) | unique |
| Eyrie Cuirass | chest | Rare | 7 | 30 armor, 8 Str, 12 Sta |  | Sandthrone captain (boss) | unique |
| Signet of the Toll Road | neck | Rare | 7 | 9 Str, 8 Sta | +15% coins from bodies. | Sandthrone captain (boss) | unique |
| Scree-Tusk's Broken Tusk | mainhand | Rare | 7 | 25 dmg, 8 Str, 12 Sta |  | Old Scree-Tusk (boss) | unique |
| Scree-Hide Legguards | legs | Rare | 7 | 30 armor, 11 Sta, 9 Agi |  | Old Scree-Tusk (boss) | unique |
| Rockfall | offhand | Epic | 7 | 44 armor, 10 Str, 15 Sta | +10% armour. | Old Scree-Tusk (boss) | unique |
| Seeker's Sealed Pendant | neck | Uncommon | 6 | 4 Str, 5 Sta |  | quest "Under a Seeker's Seal" |  |
| Tarsk's Salt-Rimed Helm | head | Rare | 7 | 19 armor, 7 Str, 10 Sta |  | quest "Salt of the First Sea" |  |
| Yara's Climbing Gloves | hands | Uncommon | 7 | 15 armor, 5 Sta, 6 Agi |  | sold by Yara Quell |  |


#### Drop chances

- **Sandthrone captain:** 50% for one of 2 signature pieces; 15% for the lucky piece
- **Old Scree-Tusk:** 50% for one of 2 signature pieces; 4% for the lucky piece (certain after 25 kills without it)
- **Toll-guards:** 3% for one of 2 lucky pieces
- **Sandthrone outriders and pickets:** 3% for the lucky piece
- **Wolves:** 3% for one of 2 lucky pieces
- **Boars:** 3% for the lucky piece
- **Pale watchers:** 2% for the lucky piece
- **Pale watchers:** 2% for the lucky piece


## The Ashland Rim (19 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| Tear-Marked Bone Mask | head | Uncommon | 9 | 19 armor, 4 Str, 7 Sta, 3 Spi |  | bone-masked cultists |  |
| Skull-Knob Staff | mainhand | Uncommon | 9 | 25 dmg, 5 Sta, 6 Int, 3 Spi |  | bone-masked cultists |  |
| Cinder-Sewn Vestment | chest | Uncommon | 9 | 30 armor, 6 Str, 8 Sta |  | bone-masked cultists |  |
| Ash-Hound Wraps | hands | Uncommon | 8 | 17 armor, 5 Sta, 7 Agi |  | ash hounds |  |
| Harpoon-Line Boots | feet | Uncommon | 8 | 17 armor, 7 Sta, 5 Agi |  | ash hounds |  |
| Static-Glass Shiv | mainhand | Uncommon | 9 | 25 dmg, 4 Sta, 10 Agi |  | Weave-Eaters |  |
| Cord of Unwoven Thread | neck | Uncommon | 9 | 4 Str, 6 Sta, 4 Agi |  | Weave-Eaters |  |
| Cinderfold Greaves | legs | Uncommon | 9 | 30 armor, 5 Str, 9 Sta |  | Hollow Men |  |
| Brood-Glass Ward | offhand | Rare | 9 | 44 armor, 10 Str, 16 Sta |  | Brood Weave-Eater (boss) | unique |
| Threadcutter | mainhand | Rare | 9 | 30 dmg, 8 Str, 10 Sta, 8 Agi |  | Brood Weave-Eater (boss) | unique |
| Weave-Eaten Mantle | shoulders | Rare | 9 | 24 armor, 11 Sta, 10 Agi |  | Brood Weave-Eater (boss) | unique |
| The Deacon's Censer | offhand | Rare | 9 | 44 armor, 10 Sta, 10 Int, 6 Spi |  | The Ash-Deacon (boss) | unique |
| Cassock of the Last Sermon | chest | Rare | 9 | 37 armor, 11 Str, 15 Sta |  | The Ash-Deacon (boss) | unique |
| Ash-Pit Treads | feet | Rare | 9 | 24 armor, 11 Sta, 10 Agi |  | The Ash-Deacon (boss) | unique |
| The Ember That Remembers | neck | Epic | 9 | 12 Str, 14 Sta, 6 Agi | +10 health per rest tick out of combat. | The Ash-Deacon (boss) | unique |
| Grohl's Line-Hauler Gauntlets | hands | Uncommon | 8 | 17 armor, 6 Str, 6 Sta |  | quest "Hounds on the Harpoon-Line" |  |
| Oska's Bone-Plate Kilt | legs | Rare | 8 | 34 armor, 9 Str, 10 Sta |  | quest "Leviathan Bone" |  |
| Pauldrons of the Old Salt Road | shoulders | Rare | 9 | 24 armor, 9 Str, 12 Sta |  | quest "The Old Salt Road" |  |
| Sefa's Cask-Head Shield | offhand | Uncommon | 9 | 35 armor, 5 Str, 9 Sta |  | sold by Sefa Brine |  |


#### Drop chances

- **Brood Weave-Eater:** 50% for one of 2 signature pieces; 15% for the lucky piece
- **The Ash-Deacon:** 50% for one of 2 signature pieces; 15% for the lucky piece; 4% for the lucky piece (certain after 25 kills without it)
- **Bone-masked cultists:** 3% for one of 3 lucky pieces
- **Ash hounds:** 3% for one of 2 lucky pieces
- **Weave-Eaters:** 3% for one of 2 lucky pieces
- **Hollow Men:** 4% for the lucky piece


## The Verdant Shore (24 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| Reed-Wader Boots | feet | Uncommon | 10 | 21 armor, 8 Sta, 7 Agi |  | moss-backed boars |  |
| Velvet-Tine Club | mainhand | Uncommon | 11 | 29 dmg, 9 Str, 7 Sta |  | moss-antler stags |  |
| Dappled Hide Mantle | shoulders | Uncommon | 11 | 23 armor, 8 Sta, 8 Agi |  | moss-antler stags |  |
| Canopy-Silk Wraps | hands | Uncommon | 11 | 23 armor, 6 Sta, 10 Agi |  | canopy spiders |  |
| Chitin Buckler | offhand | Uncommon | 11 | 42 armor, 11 Sta, 5 Agi |  | canopy spiders |  |
| Briar-Wound Circlet | head | Uncommon | 12 | 25 armor, 9 Str, 9 Sta |  | creeping briars |  |
| Heart-of-Briar Maul | mainhand | Rare | 12 | 38 dmg, 10 Str, 10 Sta, 8 Agi |  | creeping briars |  |
| Grey-Bark Breastplate | chest | Uncommon | 12 | 39 armor, 7 Str, 11 Sta |  | withered Keepers |  |
| Shroud of a Forgotten Name | chest | Rare | 12 | 49 armor, 16 Sta, 12 Agi |  | mist-walkers |  |
| Sliver of the Pale | mainhand | Rare | 12 | 38 dmg, 8 Sta, 20 Agi |  | pale shadows |  |
| Greyheart's Withered Boughs | shoulders | Rare | 12 | 31 armor, 15 Str, 18 Sta |  | Greyheart (boss) | unique |
| Ward of Living Heartwood | offhand | Rare | 12 | 57 armor, 13 Str, 20 Sta |  | Greyheart (boss) | unique |
| The Last Green Leaf | neck | Epic | 12 | 12 Str, 18 Sta, 11 Agi | +20 maximum resource. | Greyheart (boss) | unique |
| Crown of Nine Branches | head | Rare | 12 | 31 armor, 17 Sta, 16 Agi |  | Old Ninebranch (boss) | unique |
| The Old King's Hide | legs | Rare | 12 | 49 armor, 18 Sta, 15 Agi |  | Old Ninebranch (boss) | unique |
| Vigil Bark-Plate | chest | Rare | 12 | 49 armor, 14 Str, 19 Sta |  | The Hollow Root-Warden (boss) | unique, set piece |
| Vigil Root-Grips | hands | Rare | 12 | 31 armor, 13 Str, 15 Sta |  | withered Keepers in the Deep | unique, set piece |
| Vigil Sap-Treads | feet | Rare | 12 | 31 armor, 15 Sta, 13 Agi |  | mist-walkers in the Deep | unique, set piece |
| The Warden's Root-Maul | mainhand | Rare | 12 | 38 dmg, 17 Str, 16 Sta |  | The Hollow Root-Warden (boss) | unique |
| The Root-Mother's Patience | mainhand | Epic | 12 | 47 dmg, 21 Str, 20 Sta | Each kill restores 30 health. | The Hollow Root-Warden (boss) | unique |
| Leggings the Wood Learned | legs | Uncommon | 10 | 33 armor, 9 Sta, 6 Agi |  | quest "Let the Wood Learn You" |  |
| Oak-Bane's Carved Torc | neck | Rare | 12 | 9 Str, 12 Sta, 7 Agi |  | quest "The Old King of the Deep Wood" |  |
| Lantern of the Deep's Quiet | offhand | Rare | 12 | 57 armor, 14 Sta, 8 Int, 6 Spi |  | quest "The Root-Mother's Deep" |  |
| Ondine's Sap-Sealed Boots | feet | Uncommon | 11 | 23 armor, 8 Sta, 8 Agi |  | sold by Ondine Varro |  |


**Set: Vigil of the Root-Mother** (4 pieces): Vigil Bark-Plate, Vigil Root-Grips, Vigil Sap-Treads, The Root-Warden's Crown. Wearing them gives: with 2 pieces, +60 health; with 3 pieces, +12 attack power; with 4 pieces, Each kill restores 25 health.

#### Drop chances

- **Greyheart:** 50% for one of 3 signature pieces; 4% for the lucky piece (certain after 25 kills without it)
- **Old Ninebranch:** 50% for one of 3 signature pieces
- **The Hollow Root-Warden:** 100% for one of 3 signature pieces; 10% for the lucky piece (certain after 10 kills without it)
- **Moss-backed boars:** 3% for the lucky piece
- **Moss-antler stags:** 3% for one of 2 lucky pieces
- **Canopy spiders:** 3% for one of 2 lucky pieces
- **Creeping briars:** 3% for the lucky piece
- **Creeping briars:** 1.5% for the lucky piece
- **Withered Keepers:** 3% for the lucky piece
- **Mist-walkers:** 2% for the lucky piece
- **Pale shadows:** 4% for the lucky piece
- **Withered Keepers in the Deep:** 5% for the lucky piece
- **Mist-walkers in the Deep:** 5% for the lucky piece


## World drops (6 pieces)

{table: widths=21,8.5,10.5,4,18,14,16,8; size=7.5}
| Name | Slot | Quality | Lvl | Numbers | Effect | Comes from | Notes |
| The Golden Cask's Lost Tankard | mainhand | Rare | 2 | 11 dmg, 2 Str, 4 Sta | Each kill restores 5 resource. | world drop, levels 1-5 |  |
| Assessor's Honest Scale | offhand | Rare | 4 | 22 armor, 5 Str, 6 Sta | +20% coins from bodies. | world drop, levels 3-7 |  |
| Pilgrim's Last Mile | feet | Rare | 7 | 19 armor, 9 Sta, 8 Agi | +6 health per rest tick out of combat. | world drop, levels 6-10 |  |
| Hare's-Foot Torc | neck | Rare | 9 | 8 Sta, 8 Agi | +15% luck on lucky drops. | world drop, levels 8-13 |  |
| Helm With No Maker's Mark | head | Rare | 11 | 29 armor, 12 Str, 14 Sta |  | world drop, levels 11-13 |  |
| Vial of the First Sea | neck | Epic | 12 | 13 Str, 16 Sta, 12 Agi | +8% maximum health. | world drop, levels 11-13 | unique |


The twelve older named pieces (listed under "Older named gear" in Appendix C) get their look and source here: Tempered Trailblade (quest "Two Hundred and Forty Souls"); Poacher's Oilskin Hood (hidden find "The Poacher's Cold Camp"); Hesk's Shuttered Lantern (hidden find "The Quartermaster's Strongbox"); Caddock's Tin Crown (Caddock (boss)); Outrider's Hooked Knife (hidden find "The Outrider's Hoard"); Silent Pilgrim's Echo-Jar (hidden find "A Silent Pilgrim's Echo-Jar"); Leviathan-Bone Harpoon (hidden find "An Ash-Walker Cache"); Leviathan-Tooth Charm (hidden find "A Bone-Carver's Bundle"); Sap-Tapper's Gloves (hidden find "The Tapper's Stump"); Tine of the Old King (Old Ninebranch (boss)); Greyheart's Salt-Wood Stave (Greyheart (boss)); The Root-Warden's Crown (The Hollow Root-Warden (boss)).


#### Drop chances

- **Any normal mob, levels 1-5:** 0.25% for the lucky piece
- **Any elite mob, levels 1-5:** 2% for the lucky piece
- **Any normal mob, levels 3-7:** 0.25% for the lucky piece
- **Any elite mob, levels 3-7:** 2% for the lucky piece
- **Any normal mob, levels 6-10:** 0.25% for the lucky piece
- **Any elite mob, levels 6-10:** 2% for the lucky piece
- **Any normal mob, levels 8-13:** 0.25% for the lucky piece
- **Any elite mob, levels 8-13:** 2% for the lucky piece
- **Any normal mob, levels 11-13:** 0.25% for the lucky piece
- **Any elite mob, levels 11-13:** 2% for the lucky piece
- **Any normal mob, levels 11-13:** 0.1% for the lucky piece
- **Any elite mob, levels 11-13:** 1% for the lucky piece


**In all:** 104 pieces. By slot: head 9, neck 14, shoulders 9, chest 10, hands 9, legs 9, feet 10, main hand 22, off hand 12.

# Appendix E. The trades and every recipe

Six trades. Mining, Woodcutting and Herbalism are gathering and free to everyone. Cooking is free to everyone. Blacksmithing and Alchemy are crafts; you may hold two crafts. Blacksmithing and Alchemy are built and not yet published (round 6). 17 kinds of node, 46 recipes.

{table: widths=13,8,12,8,10,8.5,40.5; size=8}
| Trade | Kind | Tool | Station | Teacher | Recipes | In the game's words |
| Mining | gather | Miner's pick |  |  | 0 | Ore lies in seams along the cliffs and on cave floors. It takes a pick and a strong back, and any smith will pay for what you carry out. |
| Woodcutting | gather | Woodcutter's hatchet |  |  | 5 | Wind-felled timber is anyone's to cut. It takes a hatchet. Standing trees are left to stand. |
| Herbalism | gather |  |  |  | 0 | Bare hands and a good eye. Herbs grow where the ground suits them, and grow back when picked. |
| Cooking | free |  | fire |  | 10 | Meat, flour and eggs become something worth eating at any fire: an inn's hearth, a bake oven, a cookfire. Everyone can cook. Few do it well. |
| Blacksmithing | craft |  | forge | blacksmith | 26 | Ore to bars, and bars to blades and plate, at a forge. A craft takes years, and a body has time for two. |
| Alchemy | craft |  | bench | herbalist | 5 | Herbs to draughts, at a herbalist's bench. A craft takes years, and a body has time for two. |


## The 17 kinds of gathering node

{table: widths=24,15,8,27,8,10,8; size=8}
| Node | Trade | Skill | Gives | Yield | Comes back | Work |
| Copper seam | mining | 1 | Crowsfoot copper ore | 1-3 | 3 min | 2 s |
| Rich copper seam | mining | 1 | Crowsfoot copper ore | 2-4 | 3 min | 2 s |
| Bog-iron seam | mining | 20 | Carrion bog-iron ore | 1-3 | 3 min | 2 s |
| Adit iron seam | mining | 40 | Adit iron ore | 1-3 | 3 min | 2 s |
| Cinder seam | mining | 60 | Cinder ore | 1-3 | 3 min | 2 s |
| Veridian seam | mining | 80 | Veridian ore | 1-3 | 3 min | 2 s |
| Rich Veridian seam | mining | 80 | Veridian ore | 2-4 | 3 min | 2 s |
| Windfall oak | woodcutting | 1 | Harrow oak log | 1-3 | 3 min | 2 s |
| Windfall black pine | woodcutting | 20 | Black pine log | 1-3 | 3 min | 2 s |
| Windfall stone-pine | woodcutting | 40 | Stone-pine log | 1-3 | 3 min | 2 s |
| Fallen ash-snag | woodcutting | 60 | Ash-snag wood | 1-3 | 3 min | 2 s |
| Windfall ghost-oak | woodcutting | 80 | Ghost-oak heartwood | 1-3 | 3 min | 2 s |
| Yarrow | herbalism | 1 | Meadow yarrow | 1-2 | 90 s | 1.5 s |
| Mourner's cap | herbalism | 20 | Mourner's cap | 1-2 | 90 s | 1.5 s |
| Tarnwort | herbalism | 40 | Tarnwort | 1-2 | 90 s | 1.5 s |
| Cinder-thistle | herbalism | 60 | Cinder-thistle | 1-2 | 90 s | 1.5 s |
| Dewfern | herbalism | 80 | Dewfern frond | 1-2 | 90 s | 1.5 s |


## Woodcutting recipes (5)

{table: widths=22,6,10,28,34; size=8}
| Recipe | Skill | Station | Takes | Makes |
| Charcoal from oak | 1 | forge or fire | 1 Harrow oak log | 1 x Charcoal |
| Charcoal from black pine | 20 | forge or fire | 1 Black pine log | 2 x Charcoal |
| Charcoal from stone-pine | 40 | forge or fire | 1 Stone-pine log | 3 x Charcoal |
| Charcoal from ash-snag | 60 | forge or fire | 1 Ash-snag wood | 4 x Charcoal |
| Charcoal from ghost-oak | 80 | forge or fire | 1 Ghost-oak heartwood | 5 x Charcoal |


## Cooking recipes (10)

{table: widths=22,6,10,28,34; size=8}
| Recipe | Skill | Station | Takes | Makes |
| Griddle bread | 1 | fire | 1 Mill flour | 1 x Griddle bread [heals 190] |
| Boar stew | 1 | fire | 2 Tough boar meat | 1 x Boar stew [heals 220] |
| Harrow hearth-cake | 5 | fire | 1 Mill flour, 1 Oakhaven eggs | 2 x Harrow hearth-cake [heals 200] |
| Wolf-haunch skewer | 20 | fire | 2 Lean wolf haunch, 1 Mourner's cap | 1 x Wolf-haunch skewer [heals 340] |
| Harrow pasty | 30 | fire | 1 Mill flour, 1 Harrow cheese, 1 Tough boar meat | 1 x Harrow pasty [heals 380] |
| Pass-smoked loin | 40 | fire | 2 Tough boar meat, 1 Tarnwort, 1 Rim salt | 1 x Pass-smoked loin [heals 520] |
| Salt-baked hound flank | 60 | fire | 2 Ash-hound flank, 1 Rim salt | 1 x Salt-baked hound flank [heals 700] |
| Cinder-crust loaf | 70 | fire | 2 Mill flour, 1 Cinder-thistle, 1 Charcoal | 1 x Cinder-crust loaf [heals 660] |
| Dewfern-glazed mossback chop | 80 | fire | 2 Mossback chop, 1 Dewfern frond | 1 x Dewfern-glazed mossback chop [heals 820] |
| Shore venison pie | 90 | fire | 2 Shore venison, 1 Mill flour | 1 x Shore venison pie [heals 900] |


## Blacksmithing recipes (26)

{table: widths=22,6,10,28,34; size=8}
| Recipe | Skill | Station | Takes | Makes |
| Copper bar | 1 | forge | 2 Crowsfoot copper ore, 1 Charcoal | 1 x Copper bar |
| Copper-shod cudgel | 5 | forge | 2 Copper bar, 1 Harrow oak log | 1 x Copper-shod cudgel [mainhand, Uncommon, L1, 7 dmg, 1 Str] |
| Copper-banded buckler | 8 | forge | 1 Copper bar, 2 Harrow oak log | 1 x Copper-banded buckler [offhand, Uncommon, L1, 7 armor, 1 Sta] |
| Copper-scale gauntlets | 12 | forge | 2 Copper bar | 1 x Copper-scale gauntlets [hands, Uncommon, L1, 4 armor, 1 Str] |
| Copper-scale jerkin | 16 | forge | 3 Copper bar | 1 x Copper-scale jerkin [chest, Uncommon, L1, 6 armor, 1 Sta] |
| Bog-iron bar | 20 | forge | 2 Carrion bog-iron ore, 1 Charcoal | 1 x Bog-iron bar |
| Bog-iron hatchet | 24 | forge | 3 Bog-iron bar, 1 Black pine log | 1 x Bog-iron hatchet [mainhand, Uncommon, L4, 14 dmg, 2 Str] |
| Bog-iron helm | 28 | forge | 2 Bog-iron bar | 1 x Bog-iron helm [head, Uncommon, L4, 9 armor, 2 Sta] |
| Bog-iron greaves | 32 | forge | 3 Bog-iron bar | 1 x Bog-iron greaves [legs, Uncommon, L4, 15 armor, 2 Sta] |
| Bog-iron hauberk | 36 | forge | 4 Bog-iron bar | 1 x Bog-iron hauberk [chest, Uncommon, L4, 15 armor, 2 Str] |
| Ridge-steel bar | 40 | forge | 2 Adit iron ore, 1 Charcoal | 1 x Ridge-steel bar |
| Ridge-steel blade | 44 | forge | 3 Ridge-steel bar, 1 Stone-pine log | 1 x Ridge-steel blade [mainhand, Uncommon, L7, 20 dmg, 3 Str] |
| Ridge-steel shield | 48 | forge | 2 Ridge-steel bar, 2 Stone-pine log | 1 x Ridge-steel shield [offhand, Uncommon, L7, 28 armor, 3 Sta] |
| Ridge-steel pauldrons | 52 | forge | 3 Ridge-steel bar | 1 x Ridge-steel pauldrons [shoulders, Uncommon, L7, 15 armor, 3 Str] |
| Ridge-steel cuirass | 56 | forge | 4 Ridge-steel bar | 1 x Ridge-steel cuirass [chest, Uncommon, L7, 24 armor, 3 Sta] |
| Ash-steel bar | 60 | forge | 2 Cinder ore, 1 Charcoal | 1 x Ash-steel bar |
| Ash-steel cleaver | 64 | forge | 3 Ash-steel bar, 1 Ash-snag wood | 1 x Ash-steel cleaver [mainhand, Uncommon, L9, 25 dmg, 3 Str] |
| Ash-steel helm | 68 | forge | 3 Ash-steel bar | 1 x Ash-steel helm [head, Uncommon, L9, 19 armor, 3 Sta] |
| Ash-steel sabatons | 72 | forge | 3 Ash-steel bar | 1 x Ash-steel sabatons [feet, Uncommon, L9, 19 armor, 3 Agi] |
| Ash-steel hauberk | 76 | forge | 4 Ash-steel bar | 1 x Ash-steel hauberk [chest, Uncommon, L9, 30 armor, 3 Sta] |
| Veridian bar | 80 | forge | 2 Veridian ore, 1 Charcoal | 1 x Veridian bar |
| Veridian war-blade | 84 | forge | 4 Veridian bar, 1 Ghost-oak heartwood | 1 x Veridian war-blade [mainhand, Uncommon, L12, 31 dmg, 4 Str] |
| Veridian tower shield | 88 | forge | 3 Veridian bar, 2 Ghost-oak heartwood | 1 x Veridian tower shield [offhand, Uncommon, L12, 46 armor, 4 Sta] |
| Veridian legplates | 92 | forge | 4 Veridian bar | 1 x Veridian legplates [legs, Uncommon, L12, 39 armor, 4 Str] |
| Veridian breastplate | 96 | forge | 5 Veridian bar | 1 x Veridian breastplate [chest, Uncommon, L12, 39 armor, 4 Sta] |
| Heartwood greatblade | 100 | forge | 8 Veridian bar, 2 Ghost-oak heartwood, 4 Charcoal | 1 x Heartwood greatblade [mainhand, Rare, L12, 36 dmg, 5 Str, 5 Sta] |


## Alchemy recipes (5)

{table: widths=22,6,10,28,34; size=8}
| Recipe | Skill | Station | Takes | Makes |
| Minor healing draught | 1 | bench | 2 Meadow yarrow, 1 Stoppered vial | 1 x Minor healing draught [heals 90] |
| Healing draught | 20 | bench | 2 Mourner's cap, 1 Meadow yarrow, 1 Stoppered vial | 1 x Healing draught [heals 200] |
| Tarnwater draught | 40 | bench | 2 Tarnwort, 1 Mourner's cap, 1 Stoppered vial | 1 x Tarnwater draught [heals 280] |
| Salt-cured tonic | 60 | bench | 2 Cinder-thistle, 1 Rim salt, 1 Stoppered vial | 1 x Salt-cured tonic [heals 360] |
| Dewfern draught | 80 | bench | 2 Dewfern frond, 1 Tarnwort, 1 Stoppered vial | 1 x Dewfern draught [heals 520] |


# Appendix F. Classes, abilities and talents


## Warrior

**Resource:** Vigor. Vigor builds from landed Strikes, blows taken and well-timed Guards and is spent on Challenge, Guard and weapon techniques; DPS builds also stack weapon pressure (0-5) from Strikes to spend on finishers.
**In a line:** A flexible frontline weapon fighter who reads enemy swings and decides, moment to moment, whether Vigor becomes a block, a finisher or a rally for the group.
**Abilities (6):**

{table: widths=17,12,6,10.5,6,7,41.5; size=8}
| Ability | Unlocked | Cost | Cooldown | Cast | Range | What it does |
| Strike | level 1 | 15 | 5 s | 0 s | 3.2 m | A heavy strike. Starts automatic weapon attacks while in melee range. |
| Challenge | level 1 | 10 | 8 s | 0 s | 10 m | Forces your target to attack you for 3 seconds. Use it to protect Mira. |
| Guard | level 1 | 20 | 12 s | 0 s | 0 m | Reduce incoming damage for 5 seconds. |
| Intercept | by talent | 20 | 15 s | 0 s | 0 m | Dash to Mira and take the next blow aimed at her within 3 seconds at Guard strength. Talent. |
| Breaching Blow | by talent | 0 | 10 s | 0 s | 3.2 m | Spend all weapon pressure for 12 damage per pressure and Expose the target for 4 seconds. Talent. |
| Muster | by talent | 25 | 30 s | 0 s | 12 m | Give you and nearby Mira a 20-point barrier for 6 seconds. Talent. |


### Warrior talents: the Tank branch (tank): 16 talents, 9 playable

*How it plays:* Spend vigor on blocks, intercepts and threat; time Guard against enemy swings instead of holding it. *The cost:* Reduced offensive spending: Vigor committed to Guard and Challenge is not available for weapon techniques.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Hardened Grip | 3 | passive |  | yes | Blows you absorb with Guard generate +1 Vigor per rank, so a well-held line funds its own next block. |
| 0 | Tempered Armor | 2 | passive |  | yes | +8 armor per rank. Reduces incoming weapon damage. |
| 0 | Timed Guard | 3 | passive |  | yes | An enemy blow landing within 0.5s of you raising Guard is a timed Guard and refunds 3 Vigor per rank. Watch the enemy swing timer on the target frame. |
| 1 | Scarred Veteran | 3 | passive |  | yes | +3% maximum health per rank. |
| 1 | Steady Challenge | 1 | modifier | Tempered Armor | yes | Challenge refunds 10 Vigor when it lands, making taunts self-funding during pulls. |
| 1 | Intercept | 1 | active |  | yes | New action: dash to Mira (up to 10m) and take the next enemy blow aimed at her within 3s at Guard strength. 20 Vigor, 15s cooldown. |
| 2 | Bulwark | 1 | modifier | Steady Challenge | yes | Guard lasts 7s and reduces incoming damage by 75% instead of 60%. |
| 2 | Long Reach | 2 | passive | Intercept | yes | Intercept range +4m per rank; the enemy whose blow you intercept is Challenged for 2s per rank. |
| 2 | Grudge Ledger | 3 | passive |  | yes | Enemies you have Challenged in the last 10s deal 2% less damage to you per rank. |
| 3 | Iron Reserve | 3 | passive |  | no | +10 maximum Vigor per rank while you are below 50% health. |
| 3 | Held Line | 1 | signature | Bulwark | no | Signature stance: Guard no longer expires; it drains 4 Vigor/s instead. Each timed Guard converts the blocked hit into threat and 6 Vigor. Moving more than 2m breaks the stance. |
| 4 | Unbroken Line | 3 | passive | Held Line | no | Each timed Guard during Held Line lowers its drain by 1 Vigor/s per rank for 5s (stacks up to 3 times). |
| 4 | Rebuke | 2 | passive |  | no | Timed Guards return 10% of the blocked damage per rank to the attacker as high-threat damage. |
| 5 | Stand Fast | 2 | passive |  | no | A hit that would drop you below 20% health instead triggers a free 3s Guard. 90s cooldown, reduced 20s per rank. |
| 5 | Forgespire Plating | 3 | passive |  | no | Guard also reduces periodic and spell damage at 20% of its normal strength per rank. |
| 6 | Mountain's Keel | 1 | capstone | Unbroken Line | no | While Held Line is up, Challenge becomes an 8m area taunt and allies within 8m take 15% less damage from Challenged enemies. Held Line's drain rises to 6 Vigor/s. |


### Warrior talents: the DPS branch (dps): 16 talents, 9 playable

*How it plays:* Build weapon pressure with Strikes and spend it during exposed-target windows opened by whiffed or Guarded enemy attacks. *The cost:* Fewer defensive responses: Guard and Challenge compete with finishers for Vigor, and deep damage talents weaken Guard.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Read the Opening | 3 | passive |  | yes | Exposed windows last 0.5s longer per rank. Enemies are Exposed for 3s when your Guard absorbs their blow; Exposed enemies take 5% more damage from your party. |
| 0 | Heavy Hand | 2 | passive |  | yes | +2 weapon damage per rank, including automatic swings. |
| 0 | Edge Discipline | 3 | passive |  | yes | Automatic swings have a 5% chance per rank to add 1 weapon pressure. |
| 1 | Breaching Blow | 1 | active |  | yes | New action: spend all weapon pressure (Strike builds 1, max 5) on a blow dealing 12 damage per pressure that Exposes the target for 4s. 10s cooldown. |
| 1 | Finishing Strike | 1 | modifier | Heavy Hand | yes | Strike deals +8 damage against targets below 40% health. |
| 1 | Honed Edge | 3 | passive |  | yes | Strike deals +4% damage per rank against Exposed targets. |
| 2 | Gathering Swing | 3 | passive |  | yes | Each weapon pressure held raises attack speed by 1% per rank. |
| 2 | Deep Breach | 2 | passive | Breaching Blow | yes | Breaching Blow at 5 pressure staggers the target, delaying its next swing by 0.5s per rank. |
| 2 | Sweeping Strike | 1 | modifier | Finishing Strike | yes | Strike also deals half its raw damage to other enemies within 4m of its target. Can pull extra enemies. |
| 3 | Press the Breach | 1 | signature | Sweeping Strike | no | Signature action: for 6s after you Expose a target, Strikes cost no Vigor and build 2 pressure, and every finisher resets Breaching Blow. 45s cooldown. |
| 3 | Battle Rhythm | 3 | passive |  | no | Spending 5 weapon pressure at once restores 4 Vigor per rank. |
| 4 | Reckless Stance | 2 | passive |  | no | +5% damage per rank, but Guard reduces 10% less damage per rank. |
| 4 | Relentless Press | 3 | passive | Press the Breach | no | If a finisher kills during Press the Breach, it lasts 1s longer per rank and carries to the nearest enemy, Exposing it. |
| 5 | Paired Blades | 3 | passive |  | no | Dual grip: automatic swings strike twice at 55% damage, and each second hit has a 10% chance per rank to add 1 weapon pressure. |
| 5 | Greatweapon Arc | 2 | passive |  | no | Two-handed grip: Sweeping Strike hits a wide 150-degree arc and deals +10% per rank, but Strike recovers 0.1s slower per rank. |
| 6 | Forgefall | 1 | capstone | Relentless Press | no | Breaching Blow at 5 pressure becomes Forgefall: a leaping blow dealing triple damage to the Exposed target and half to nearby foes. You cannot Guard for 4s afterward. |


### Warrior talents: the Support branch (support): 16 talents, 9 playable

*How it plays:* Rally allies and expose enemies; intercept at the right moment so a teammate's worst hit lands on you instead. *The cost:* Personal damage funds party gains: rally effects cost Vigor and deep nodes lower your own Strike output.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Called Cadence | 3 | passive |  | yes | Challenge rallies you for 6s: +4% attack speed per rank. Mira, if within 10m, casts and recovers 4% faster per rank for the same 6s. |
| 0 | Rally Reserve | 2 | passive |  | yes | +10 maximum Vigor per rank. More capacity, not faster regeneration. |
| 0 | Mark the Gap | 3 | passive |  | yes | Strike Exposes its target for 1s per rank (Exposed enemies take 5% more damage from your party). |
| 1 | Steady Voice | 3 | passive |  | yes | Your rally effects (Rallying Challenge, Called Cadence, Muster, Shared Shelter, Planted Standard) reach 2m further per rank. |
| 1 | Rallying Challenge | 1 | modifier | Rally Reserve | yes | Challenge restores 12 mana to Mira when she is recruited, alive and within 13m. |
| 1 | Muster | 1 | active |  | yes | New action: spend 25 Vigor to give you and Mira (within 12m) a 20-point barrier for 6s. 30s cooldown. |
| 2 | Shared Shelter | 1 | modifier | Rallying Challenge | yes | Guard also heals Mira for 20 and reduces her incoming damage by 30% for 5s. Requires 13m range. |
| 2 | Planted Standard | 2 | passive | Muster | yes | Muster plants a standard for 4s per rank; you and Mira regain 2 Vigor or mana per second within 8m of it. |
| 2 | Scatter Their Guard | 3 | passive |  | yes | Exposed enemies take a further 3% damage per rank from your party. |
| 3 | Shared Second Wind | 3 | passive |  | no | Muster restores 3 Vigor per rank for each ally it reaches. |
| 3 | Answering Step | 1 | signature | Shared Shelter | no | Signature action: within 1s of an ally being targeted by a heavy attack, swap places, take the hit at Guard strength and Expose the attacker to your party for 5s. 20s cooldown. |
| 4 | Shoulder to Shoulder | 3 | passive | Answering Step | no | The ally saved by Answering Step takes 5% less damage per rank for 6s; if they survive, its cooldown drops 2s per rank. |
| 4 | Drilled Formation | 2 | passive |  | no | Allies within 8m of you take 2% less damage per rank from enemies you have Challenged. |
| 5 | Lend the Edge | 2 | passive |  | no | Your Strike deals 5% less damage per rank; that amount is added to the next ally attack on the same target. |
| 5 | Weathered Voice | 3 | passive |  | no | Muster's barriers absorb 10% more per rank and also remove one slowing effect. |
| 6 | Stormlight Standard | 1 | capstone | Shoulder to Shoulder | no | Answering Step also plants your standard: for 8s allies within 10m share 20% of incoming damage with you and receive your Vigor refunds as their resource. Your damage is halved meanwhile. |


**Warrior talents in all:** 48 in 3 branches (102 ranks). 27 are playable. 21 are designed and not yet playable.

## Druid

**Resource:** Wildstores. Each form keeps its own pool (Bark, Fang, Sap, Glimmer) filled only by that form's actions; changing form spends shared Shift Breath and never refills any pool for free.
**In a line:** A deliberate generalist recognised by committing to one form at a time and paying a visible Shift Breath cost to leave it.
**Abilities (22):**

{table: widths=17,12,6,10.5,6,7,41.5; size=8}
| Ability | Unlocked | Cost | Cooldown | Cast | Range | What it does |
| Barkhide | level 1 | 25 | 1.5 s | 0 s | 0 m | Shift into Barkhide: armor and health up, Bark builds from blows. Costs Shift Breath. |
| Thornclaw | level 1 | 25 | 1.5 s | 0 s | 0 m | Shift into Thornclaw: faster, quicker swings, Fang combo finishers. Costs Shift Breath. |
| Rootmend | level 1 | 25 | 1.5 s | 0 s | 0 m | Shift into Rootmend: Seedling heals build Sap for bigger blooms. Costs Shift Breath. |
| Thornsong | level 1 | 25 | 1.5 s | 0 s | 0 m | Shift into Thornsong: alternate Seedshot and Thornbolt to build Glimmer. Costs Shift Breath. |
| Bough Strike | level 1 | 0 | 4.5 s | 0 s | 3.2 m | Heavy blow with double threat. Builds 12 Bark. |
| Bellowing Roar | level 2 | 0 | 10 s | 0 s | 10 m | Taunt your target for 3 seconds and add threat to every enemy within 8 metres. |
| Barkmend | level 4 | 0 | 8 s | 0 s | 0 m | Spend 30 Bark to heal yourself for 12% of maximum health. |
| Heartwood Brace | by talent | 0 | 15 s | 0 s | 0 m | Spend 40 Bark: take 35% less damage for 6 seconds. Talent. |
| Rake | level 1 | 0 | 0 s | 0 s | 3.2 m | Claw the target and make it bleed for 6 seconds. Builds 1 Fang. |
| Lunge | level 3 | 0 | 12 s | 0 s | 12 m | Leap to an enemy within 12 metres. Builds 1 Fang. |
| Tear | level 1 | 0 | 0 s | 0 s | 3.2 m | Finisher: spend all Fang for 10 damage per Fang. |
| Rending Flurry | by talent | 0 | 10 s | 0 s | 3.2 m | Finisher: spend 3-5 Fang for 4-8 rapid strikes over 2 seconds. Talent. |
| Seedling | level 1 | 0 | 0 s | 0 s | 25 m | Heal-over-time on your lowest-health ally for 10 seconds. Each tick builds Sap. |
| Quickbloom | level 2 | 0 | 0 s | 1.5 s | 25 m | 1.5s cast: spend 25 Sap on a direct heal. Moving interrupts. |
| Verdant Ward | level 5 | 0 | 12 s | 0 s | 25 m | Give your lowest-health ally a barrier for 8 seconds. |
| Burst Bloom | by talent | 0 | 8 s | 0 s | 25 m | Spend 30 Sap: consume a Seedling and heal its remaining ticks plus 25%. Talent. |
| Seedshot | level 1 | 0 | 0 s | 0 s | 25 m | Instant thorn shot with a short damage-over-time. Alternate with Thornbolt for Glimmer. |
| Thornbolt | level 1 | 0 | 0 s | 1.8 s | 25 m | 1.8s cast: heavy thorn bolt. Alternate with Seedshot for Glimmer. Moving interrupts. |
| Briar Snare | level 4 | 0 | 12 s | 0 s | 25 m | Slow the target by 40% for 6 seconds. |
| Bramblestorm | by talent | 0 | 20 s | 0 s | 25 m | Spend all Glimmer (min 30): 3-second channel of thorn rain. Talent. |
| Swiftroot | level 3 | 0 | 15 s | 0 s | 25 m | Instant emergency heal on your lowest-health ally. Rootmend only unless talented. |
| Stillroot | level 6 | 0 | 20 s | 0 s | 25 m | Root the target in place for 4 seconds. Thornsong only unless talented. |


### Druid talents: the Barkhide branch (tank): 14 talents, 7 playable

*How it plays:* Commit to the heavy Barkhide form, build Bark from absorbed hits to hold threat, and time Bark-spending recoveries around enemy spikes. *The cost:* Healing and ranged output are restricted while in Barkhide, and shifting out strands unspent Bark.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Ringed Hide | 5 | passive |  | yes | +2% armor per rank in Barkhide, and each blow you take in Barkhide grants 1 extra Bark per rank. |
| 0 | Rooted Stance | 3 | modifier |  | yes | After 2s without moving, Bough Strike also hits 1/2/3 other enemies within 4m of its target and threatens each. |
| 1 | Mosscoat | 3 | passive |  | yes | Shifting into Barkhide costs 10/20/30% less Shift Breath and grants a moss barrier worth 3% of your maximum health per rank for 8s. |
| 1 | Heartwood Brace | 1 | active |  | yes | New action (Barkhide): spend 40 Bark to reduce damage taken by 35% for 6s. Unusable below 40 Bark, so it must be banked from earlier blows. |
| 1 | Thorned Bark | 2 | passive |  | yes | In Barkhide, enemies that strike you take 3 nature damage per rank plus 5% of your armor per rank, with double threat. |
| 2 | Sapwood Core | 3 | modifier | Heartwood Brace | yes | When Heartwood Brace ends, 10/20/30% of the damage it prevented returns as healing over 8s. |
| 2 | Lodged Roots | 2 | passive |  | yes | Bellowing Roar snares its target by 20/40% for 4s; while snared it deals 5% less damage to anyone other than you. |
| 3 | Deep Grain | 3 | passive | Mosscoat | no | While Mosscoat's shield holds, Bark generation is +10/20/30%. Staying in Barkhide for 20s raises the Bark cap by 10 per rank. |
| 3 | Bristle Roar | 3 | passive |  | no | Your area threat roar applies Bristled: affected enemies deal 3/6/9% less damage to you specifically for 8s. |
| 4 | Ancient Bulwark | 1 | signature |  | no | Swell into an ironbark colossus for 12s. Hits taken bank Bark past the cap as Overgrowth, released as a self-heal when it ends. Leaving form early forfeits all Overgrowth. |
| 4 | Knotted Pulse | 3 | passive |  | no | Every 20 Bark you spend reduces Heartwood Brace's cooldown by 1/2/3s, rewarding a steady spend rhythm over hoarding. |
| 5 | Grove Guard | 3 | passive |  | no | The ally nearest you takes 4/8/12% less damage; the prevented amount drains your Bark instead. Protects a partner at the cost of your own buffer. |
| 5 | Last Ring | 2 | passive | Knotted Pulse | no | Dropping below 30% health triggers Heartwood Brace at no Bark cost, once every 90/60s. |
| 6 | Worldroot Stand | 1 | capstone | Ancient Bulwark | no | Ancient Bulwark lasts 6s longer and forces every enemy within 10m to attack you, but you cannot move or shift until it ends. |


### Druid talents: the Thornclaw branch (dps): 14 talents, 7 playable

*How it plays:* Use the mobile Thornclaw form to build Fang with quick rakes and spend it on finishing chains before it decays. *The cost:* Lower durability than Barkhide; Fang decays outside combat and shifting away strands it.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Chain Pounce | 3 | modifier |  | yes | Tear or Rending Flurry used within 3s of Lunge counts as 1/2/3 extra Fang (up to 5). |
| 0 | Keen Claws | 5 | passive |  | yes | +3% Thornclaw damage per rank. At 5 ranks, every third Rake grants an extra Fang. |
| 1 | Scent of Blood | 2 | passive |  | yes | Rake bleeds deal 10% more per rank to targets below 35% health; at rank 2 those Rakes also grant an extra Fang. |
| 1 | Rending Flurry | 1 | active |  | yes | New action (Thornclaw): spend 3-5 Fang for 4-8 rapid strikes over 2s. You may move while it runs; shifting mid-flurry loses the remaining strikes. |
| 1 | Quickpad | 3 | passive |  | yes | Movement speed +5/10/15% in Barkhide and Thornclaw. |
| 2 | Bramble Feint | 2 | passive |  | yes | Blows you take in Thornclaw grant 1 Fang, at most once every 6/4s. |
| 2 | Feral Cadence | 3 | modifier | Rending Flurry | yes | If the target survives every strike of Rending Flurry, the final strike refunds 1/2/3 Fang. |
| 3 | Open Veins | 3 | passive |  | no | Your bleeds last 2/4/6s longer and each tick has a 5% chance to add 1 Fang. |
| 3 | Tracker's Shift | 3 | passive | Quickpad | no | With Quickpad, shifting into Thornclaw converts 10/20/30% of the pool you leave into Fang. It is a conversion, not a refill: the old pool is emptied. |
| 4 | Thornlash | 3 | passive |  | no | Finishers spending 5 Fang apply Thornlash: your damage to that target +2/4/6% for 8s. |
| 4 | Wildrun | 1 | signature |  | no | For 10s, every finisher chains a free Lunge to the nearest wounded enemy. Ends early if a single hit deals over 15% of your max health. |
| 5 | Bloodied Verve | 2 | passive | Thornlash | no | Finishers against a Thornlashed target heal you for 1/2% max health, keeping the fragile form alive without shifting out. |
| 5 | Pack Mark | 3 | passive |  | no | Finishers mark the target; allies' physical hits on it restore 1 Fang to you, at most once per 3/2/1s. |
| 6 | Apex Chase | 1 | capstone | Wildrun | no | Wildrun lasts until a chained Lunge misses; each consecutive Lunge adds 4% damage. While it runs you cannot shift and healing above 70% health is wasted. |


### Druid talents: the Rootmend branch (healer): 14 talents, 7 playable

*How it plays:* Seed allies with slow restoration ahead of damage, then spend Sap on reactive blooms when spikes land. *The cost:* Little offense; leaving Rootmend to rescue in another form costs Shift Breath and strands Sap.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Slow Sap | 5 | passive |  | yes | Seedling heals 4% more per rank. Seedling ticks are the main source of Sap. |
| 0 | Pre-bloom | 3 | modifier |  | yes | Seedling ticks on an ally at full health are stored (up to 1/2/3) and released instantly when that ally drops below 60%. |
| 1 | Dew Cleanse | 3 | passive |  | yes | Swiftroot can be cast from any form, costing 15/10/5 Shift Breath outside Rootmend. A cheap emergency heal for hybrids. |
| 1 | Burst Bloom | 1 | active |  | yes | New action (Rootmend): spend 30 Sap to consume the Seedling on your heal target, healing its remaining ticks instantly plus 25%. |
| 1 | Canopy | 2 | passive |  | yes | Allies carrying your Seedling take 3/6% less damage. |
| 2 | Grafting | 3 | modifier | Burst Bloom | yes | Burst Bloom leaves a 3/5/7s sprout that re-seeds the ally; it costs 5 more Sap. |
| 2 | Sap Thrift | 2 | passive |  | yes | 10/20% of Seedling overhealing returns as Sap. |
| 3 | Rain Root | 3 | passive | Dew Cleanse | no | Cleansing with Dew Cleanse plants a free 4s Seedling on the target and allies within 5/7/9m. |
| 3 | Heartwood Reading | 3 | passive |  | no | Telegraphed attacks on seeded allies are shown 1s earlier per rank, so preparation beats reaction. |
| 4 | Living Grove | 1 | signature |  | no | Plant a stationary grove for 12s: Seedlings on allies inside spread to one unseeded ally every 2s, and Burst Bloom cast inside costs no Sap. |
| 4 | Deep Rings | 3 | passive |  | no | Seedlings that run their full duration refund 2/4/6 Sap. |
| 5 | Thornward | 3 | passive |  | no | Burst Bloom also grants a bark shield worth 5/10/15% of the heal for 6s. |
| 5 | Evergreen | 2 | passive | Deep Rings | no | Deep Rings refunds past the Sap cap fill a 20/40 reserve that only Burst Bloom can spend. |
| 6 | Worldbloom | 1 | capstone | Living Grove | no | When Living Grove ends it erupts, instantly blooming every Seedling in range at 60% value. Afterward Sap cannot generate for 8s. |


### Druid talents: the Thornsong branch (dps): 14 talents, 7 playable

*How it plays:* Alternate Seedshot and Thornbolt casts to build Glimmer, then spend it on a channelled Bramblestorm while exposed. *The cost:* Long cast exposure and weak frontline survival; Glimmer builds only through alternation, so interrupts break the rhythm.

{table: widths=5,16,7,9,13,9,41; size=7.5}
| Tier | Talent | Ranks | Kind | Needs | Playable | What it does |
| 0 | Germinate | 3 | modifier |  | yes | Seedshot plants a seed instead of its damage-over-time; your next Thornbolt on that target detonates it for 15/30/45% bonus damage. |
| 0 | Green Voice | 5 | passive |  | yes | +2% Thornsong damage per rank. At ranks 3 and 5, each Seedshot/Thornbolt alternation grants 5 more Glimmer. |
| 1 | Pollen Veil | 2 | passive |  | yes | Enemies hit by Seedshot deal 3/6% less damage to you for 6s. |
| 1 | Bramblestorm | 1 | active |  | yes | New action (Thornsong): spend all Glimmer (min 30) to channel 3s of thorn rain around your target; damage scales with Glimmer spent. Moving breaks it. |
| 1 | Stillroot Cast | 3 | passive |  | yes | Stillroot lasts 1/2/3s longer and can be cast from any form without Shift Breath. |
| 2 | Cadence Thread | 2 | passive |  | yes | Three perfect alternations in a row make your next Thornbolt instant (holds 1/2 charges). |
| 2 | Thornbound Channel | 3 | modifier | Bramblestorm | yes | Moving no longer breaks Bramblestorm, but it costs 10 more Glimmer and its ticks deal 40/25/10% less damage while you move. |
| 3 | Quartzine Seed | 3 | passive |  | no | Germinated seeds that are detonated by a critical Thornbolt shatter into quartzine shards hitting enemies within 3/4/5m. |
| 3 | Deep Root Snare | 3 | passive | Stillroot Cast | no | Enemies held by Stillroot Cast take 5/10/15% more damage from Thornbolt. |
| 4 | Sporecount | 3 | passive |  | no | Each 10 Glimmer held raises Thornbolt damage by 1/2/3%, rewarding patience before a Bramblestorm. |
| 4 | Overgrowth Aria | 1 | signature |  | no | For 10s both casts count as each other, so every cast is a perfect alternation and Glimmer builds twice as fast. You cannot move during the Aria. |
| 5 | Ripened Glimmer | 2 | passive | Sporecount | no | Glimmer can exceed its cap by 15/30; the overflow decays by 1 per second. |
| 5 | Seedward | 3 | passive |  | no | Starting Bramblestorm grants a bark ward absorbing 4/8/12% of your max health. |
| 6 | Thousand Thorns | 1 | capstone | Overgrowth Aria | no | Overgrowth Aria ends with an automatic Bramblestorm at double Glimmer value. Afterward Glimmer cannot be generated for 6s. |


**Druid talents in all:** 56 in 4 branches (140 ranks). 28 are playable. 28 are designed and not yet playable.

## Mira, the healer companion

- Restorative weave: Heal, power 42, cost 18, cooldown 3.5 s, cast 1.5 s, range 13 m. A deliberate restorative cast.
- Light bolt: Damage, power 7, cost 0, cooldown 4 s, cast 0 s, range 12 m. Opportunistic support damage.

# Appendix G. How gear looks and how ordinary gear is named

## Gear shapes: 54 families, 146 variants

Every piece of gear is drawn on your body from a family (its basic shape) and a variant (the version of it). The list below gives each family with its variants in brackets.

- **Main hand (17 families):** short sword (plain, notched); arming sword (straight, curved, disc); falchion (clipped, heavy); sabre (officer, broken); leaf sword (bronze, root); great sword (steel, wood, living); knife (hooked, needle, glass, sickle); hand axe (wedge, hatchet); bearded axe (plain, hooked); crescent axe (plain, spiked); cleaver (slab, notched); club (plain, studded, bound, tusk, tankard); flanged mace (six, fist); war hammer (pick, maul); root mace (burl, antler, briar); polearm (spear, harpoon, fork, billhook, spade); staff (knob, crook, forked, skull).
- **Off hand (6):** buckler (plain, tusk, chitin); round shield (boards, hide, cask, lid); heater shield (plain, striped, glass); kite shield (plain, slab); leaf shield (bronze, bark); hung things (lantern, shuttered lantern, moss lantern, censer, scale).
- **Head (9):** cap (plain, flaps, leaf); hood (cloth, oilskin); coif (mail); wrap (scarf); kettle hat (plain, half); barbute (plain, rimed); mask (bone, tear); circlet (band, briar); crown (tin, root, antler).
- **Shoulders (3):** mantle (cloth, fur, hide, frayed, shawl); pauldron (dome, bone); spaulder (lames, glass, bark).
- **Chest (6):** tunic (plain); jerkin (leather, hide, scale); hauberk (mail); coat (grey, skirted); cuirass (plate, bark); robe (vestment, cassock, shroud).
- **Hands (3):** gloves (plain, fingerless, mitts); wraps (cloth, fur, silk); gauntlets (plate, bone, glass, root).
- **Legs (4):** breeches (plain, patched); leggings (garter, hide); greaves (plate, full); kilt (bone).
- **Feet (3):** shoes (plain); boots (plain, waders, hobnail, root); sabatons (plate).
- **Neck (3):** pendant (drop, glass, seal, signet, ember, leaf, vial, jar); cord (beads, knot, fang, tooth, tine); torc (metal, band, wood, charm).

## Colour palettes (10)

Gear is coloured by where it comes from: Oakhaven, Concord, Sandthrone, Khaven, the toll road, pilgrim, Ash-Walker, cult, Veridian and Pale. Of the named and crafted pieces, 32 use the Veridian palette, 22 Sandthrone, 20 Ash-Walker, 18 Oakhaven, 15 Khaven, 14 toll road, 13 Pale, 7 pilgrim, 6 cult and 2 Concord. 24 pieces carry a glow.

Crafted metal has its own tint: copper, bog-iron, ridge-steel, ash-steel, Veridian and heartwood.

You have asked for richer, high-fantasy colour here (note 13). [PLANNED]

## How quality shows on the body

- Poor: faded, with a rust mark.
- Common: plain trim.
- Uncommon: trim in the palette's colour and one extra part.
- Rare: polished trim and one glowing accent.
- Epic: gold-toned trim and two pulsing accents.

Armour also grows heavier-looking by level band.

## Ordinary gear, made by the game

Most gear that drops is not hand-made. The game makes it from a slot, a level, a quality and a number, and builds its name from three words.

- **The first word, by level:** levels 1 to 2 Homespun, Frayed, Patched; 3 to 5 Tanned, Riveted, Stitched; 6 to 8 Toll-road, Ridge-forged, Pilgrim's; 9 to 10 Ash-hardened, Salt-cured, Emberbound; 11 to 13 Veridian, Root-bound, Sap-steeped.
- **The piece word, three for each slot:** head Cap, Hood, Coif; neck Pendant, Cord, Torc; shoulders Mantle, Pauldrons, Spaulders; chest Jerkin, Tunic, Hauberk; hands Gloves, Wraps, Gauntlets; legs Breeches, Leggings, Greaves; feet Boots, Shoes, Sabatons; main hand Blade, Hatchet, Cudgel; off hand Buckler, Shield, Lantern.
- **The ending, one of six:** of the Oak, of the Hare, of the Owl, of the Bear, of the Hearth, of Salt.
- So a piece might be called "Ridge-forged Blade of the Owl".
- **Slots (9):** head, neck, shoulders, chest, hands, legs, feet, main hand, off hand. Cloaks would be a tenth. [PLANNED]
- **Qualities (5):** poor, common, uncommon, rare, epic.
- **Bags:** 24 slots, and up to 52 with all four trade bags (6 + 6 + 8 + 8).

# Appendix H. Save formats

Your save is one file, `encounter.save.json`, with a spare copy beside it. There is a second file for the Druid and a small profile file. An old save is upgraded in memory when it is read, and written in the new format at the next save.

{table: widths=8,18,74; size=8.5}
| Format | Date | What it added |
| 1 | not dated in the doc (first playable) | Player and companion ids, experience, health, mana, position, recruited, relationship, gold, inventory id list, equipped item, enemy dead/looted records. No class or talents (read as Warrior with no talents). |
| 2 | not dated in the doc | classId and talents, with the 9-node prototype talent ids (mapped through EncounterSave.LegacyTalentIds on read). |
| 3 | not dated in the doc | Talents by the data-driven ids from Talents/<class>.json (only impl nodes), validated by TalentTree (level + 1 points, 5 points a tier, prerequisites). |
| 4 | 2026-09-29 | Quests: quests, questsDone, reputation, documents (Chronicle pages), questItems, usedInteractables. |
| 5 | 2026-09-29 | New XP curve, XpToNext(L) = 200 + 90*(L-1); old experience mapped onto it keeping level and fraction. |
| 6 | 2026-09-29 | Bags and equipment: bag (24 slots of item + count) and equipment (one entry per slot); old inventory and equippedItem moved in. Position saved as the last dry footing when swimming. |
| 7 | 2026-09-30 | discoveries: ids of hidden finds found (each pays once). Migration AddDiscoveriesMigration, the first SaveMigrator step. |
| 8 | 2026-10-01 | Trades: professions (id + skill, 0-100) and pouches (trade bags worn; they extend bag up to 52 slots, hard limit 96). Migration AddProfessionsMigration. |
| 9 | 2026-10-01 | The Armoury (loot step L3): armoury (named pieces ever found), looks (appearance keys seen), lootLuck (kills by drop list, and epic pity counters). Migration AddArmouryMigration. |

**Not saved, on purpose:** camp mobs and what lies on their bodies, an open loot window, worked nodes, known recipes (they follow from skill), the weather, the hour of day, village purses, game animals.

**Formats 1 to 3** are not dated in the save document; the changelog puts format 3 on 28 September. Format 9 is on the main branch and not yet in a published build.

**Planned and not built:** saving the wider world, simulated adventurers and factions as separate records.

# Appendix I. The systems in the game

What the code holds today: 112 game scripts (about 31,000 lines), 9 editor scripts, 75 test files with 499 tests (341 EditMode, 158 PlayMode), 8 shaders, 3 scenes. One scene builds every zone; the other two are the old trail encounter and the first test map.

{table: widths=22,78; size=8.5}
| System | What is there |
| Zones from data | Each zone is one JSON file; ZoneBuilder generates ground, painted ground texture, roads, fields, water, props, forest edge, the Wasting, lighting, boundaries and navmesh at load. One scene builds any zone. |
| Travel | exits in the data; E at an exit saves and rebuilds the next zone; Mira comes along. Roads: Oakhaven - Khaven - Peaks - Ash Rim - Verdant Shore, and Ash Rim - Oakhaven. |
| Day and night | 40 real minutes a day, starting 08:30; sun by day, moon at night; dawn, day, dusk and night keyframes blended with each zone's palette; lamps light after dark; the hour shows on the minimap; F11 skips an hour in dev builds. Not saved. |
| Weather | 9 kinds (clear, fair, windy, overcast, mist, rain, flurries, ash squall, storm); 7-minute spells from a seeded schedule per zone, one severity step at a time; clouds on a dome, cloud shadows, wind in grass and leaves, rain streaks, splashes and wet ground, no rain under roofs, lightning in storms, snow, mist, ash; chat announces a turn; F8 cycles in dev builds. Not saved. |
| Water and swimming | Creeks (carved, flowing, with current) and lakes; wading and swimming (swim past 1.45 m depth), float spring, splashes and rings; navmesh prefers bridges; stylised see-through water shader with depth colour, foam and sparkle; weapons slung on the back while swimming. |
| Caves and dungeons | Two walk-in dungeons with no loading screen: Crowsfoot Hollow (Oakhaven; levels 3-5; 8 camps flagged harder with 17 mobs, incl. Quartermaster Hesk and Caddock, the Bandit King; 16 m down) and the Root-Mother's Deep (Verdant Shore; levels 12-13; 6 camps with 13 mobs, incl. the Hollow Root-Warden; 100 m long, 16 m down). Passage model Hollow, its own floor and rock shell, cave light and air. Baked cave lighting is asked for (note 10) and not built. |
| World edge and the Wasting | A backdrop skirt of hills past each zone edge; the Wasting's grey curtain and unmade ground on the east of Oakhaven and the Ash Rim. |
| Nature and post-processing | Instanced grass with wind, tall grass that hides ambushers, painted fern, leaf, reed and flower cards, falling leaves and ash, tree fade near the camera, bloom, sun shafts, ACES tone map, per-biome grade, vignette. |
| Painted style pass | Hand-painted plaster, thatch, slate, masonry, timber and bedded rock; metre UVs; building trim; stepped footings on slopes; hero inns and smithy. |
| Combat core | Actors, health and resource pools, stats and modifiers, threat and taunt, statuses, ability runtime (casts, cooldowns, global cooldown), level con colours, leash and reset, ambush camps, respawns. |
| Classes | Warrior (6 abilities, 3 talent branches) and Druid (4 forms, 22 abilities, 4 branches); talent window; level cap 13 (talent points stop at 10). |
| Companion | Mira, the healer (Restorative weave, Light bolt), recruited in the Golden Cask. |
| Quests and the Chronicle | 56 quests over 5 zones (main chains, faction, NPC, side), Chronicle pages to read, a quest-item bag, 6 factions with reputation, quest tracker and quest book. |
| Secrets | 42 hidden finds (vistas, caches, notes, herbs, keys, chests), each paid once. |
| Village life and jobs | Villagers with roles, homes, daily schedules (WorkDay shifts and errands), carried loads, a goods chain between trades (farmer, miller, baker, woodcutter, smith, hunter, skinner, leatherworker, herbalist, merchant, innkeeper), hen-wives and coops, drinkers at the inn, a night routine, talk and barks, fleeing danger. |
| Households and knocking | 21 households (16 Oakhaven, 5 Khaven) with named houses; knocking on a door gets an answer that fits who is home. |
| Purses | One purse per household; a daily stipend; bread, firewood and eggs bought between households; cold chimneys when firewood is unaffordable; the player's coin reaches the seller's household. Not saved. |
| Gathering | Mining (needs a pick), Woodcutting (needs a hatchet), Herbalism (bare hands); 140 nodes, 28 a zone (10 ore, 8 timber, 10 herbs), 5 tiers at skill 1, 20, 40, 60, 80; nodes rest and regrow. |
| Crafting | Blacksmithing (26 recipes: 5 smelts, 21 pieces) and Alchemy (5 draughts); two craft slots, taken up at a station or from a trainer, forgettable; Woodcutting's 5 charcoal burns; stations (forge, bench, fire) across the zones; the trade's own person speeds the work. |
| Cooking | 10 recipes at any fire; everyone has it from the start. |
| Hunting | Deer and rabbits are game animals (120 in the data: 81 rabbits, 39 deer) that graze, watch and bolt; skinned for hides; meat from wolves, boars, hounds, mossboars and stags. |
| Items, bags, vendors | A 24-slot bag, 9 equipment slots, four trade bags (ore-poke, log-sling, simples-wallet, larder-scrip), vendors by role and by name, sell junk, upgrade arrows, compare tooltips. |
| Loot | Loot rolled onto the body at death, a loot window, quality beacons (twinkle, green glint, blue beam, purple beam), named loot by zone and boss, signature lists, uniques, two sets, worn effects, luck, epic pity. |
| Gear looks | Every slot shown on the body; 54 families, 146 variants, 10 palettes; quality reads on the body (glow on rare and epic). |
| The Armoury | A record of every named piece and look found, with "NEW LOOK" toasts and known-but-unfound pieces in grey (save format 9). |
| HUD and windows | Player, target, party and friend frames; nameplates; action bar (10 slots) and XP bar; minimap with the hour and weather; zone map and world map (M); quest tracker and quest book / Chronicle (L); bags (I); character sheet with the Armoury tab (C); Trades window (K); talent/build window (B); vendor window; loot window; conversations and speech bubbles; chat log; tooltips; discovery toasts; place names; micro menu; pause menu; F5 save, F9 load, R recover after death, Tab target, E use, Ctrl sneak. |
| Capture tools | Command-line tours for looking without playing: --crulanda-world-capture, --crulanda-ui-capture, --crulanda-wardrobe-capture, --crulanda-loot-capture, plus --crulanda-zone, --crulanda-class, --crulanda-weather, --crulanda-temp-save. |
| Saving | Versioned envelope, backup file, chained migrations, format 9. |
| Dev tools | A dev console and debug commands. |

