"""WORLD_ZONES.md: the deep, the hidden Crowsfoot mouth, the hen-wife's day and the trades' schedules."""
import io
p = r'D:\code\mmo\New Unity Project\Docs\WORLD_ZONES.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s
s = s.replace('\r\n', '\n')
def rep(a, b):
    global s
    assert s.count(a) == 1, a[:70]
    s = s.replace(a, b)

# 1. The Verdant section: the deep among the places, camps and quests.
rep("""  and pale shadows round an unmade heart).
- **The Wending**, a slow river from the falls' pool west past Rootfast to the flats.""",
"""  and pale shadows round an unmade heart); and under the Temple, **the Root-Mother's Deep**, the zone's dungeon (GAME-ONLY; see
  "Caves you walk into").
- **The Wending**, a slow river from the falls' pool west past Rootfast to the flats.""")
rep("""  (13) and **Old Ninebranch** (13). New looks: `keeper`, `stag`, `spider`, `bramble`.
- **Quests:** the breadcrumb from Grohl; a main chain of four (Let the Wood Learn You, The Briar Way, Grey at the Heart, A Fog That
  Tastes of Lightning); seven side and NPC quests; four Chronicle pages; a new faction, the Veridian Keepers. `QUEST_DESIGN.md`.""",
"""  (13) and **Old Ninebranch** (13), and six more down the deep (see below). New looks: `keeper`, `stag`, `spider`, `bramble`.
- **Quests:** the breadcrumb from Grohl; a main chain of five (Let the Wood Learn You, The Briar Way, Grey at the Heart, A Fog That
  Tastes of Lightning, The Root-Mother's Deep); seven side and NPC quests; five Chronicle pages; a new faction, the Veridian Keepers.
  `QUEST_DESIGN.md`.""")

# 2. Caves: the Crowsfoot mouth rework, and the Root-Mother's Deep.
rep("""- **Crowsfoot Hollow** (GAME-ONLY) is Oakhaven's cave, at the end of the North road in the north hills: the Sandthrone
  deserters' hideout, and the game's first dungeon (Chris, 2026-09-30: "the cave should be deep and the first foray into
  dungeon crawling"). There is no loading; you walk in, and down.""",
"""- **Crowsfoot Hollow** (GAME-ONLY) is Oakhaven's cave, at the end of the North road in the north hills: the Sandthrone
  deserters' hideout, and the game's first dungeon (Chris, 2026-09-30: "the cave should be deep and the first foray into
  dungeon crawling"). There is no loading; you walk in, and down.
- **Hidden in the hill** (Chris, 2026-10-01: "the dungeon in the first zone kind of stick out as just a rock. it needs to be
  built into a mountain or something and kinda hidden..no so obvious. they are bandits"): the mouth is a slot in a cliff face,
  not a rock on the grass. Two cliff scarps (`Crowsfoot scarp, west` and `east`, 18 m, lifted 10 m) stand either side of it and
  the Crowsfoot brow (two `shapes`, 11-11.5 m high) rises over it, so the knoll's crag lumps heap onto a hill that was already
  there. The North road ends at (-13, 72); from there the Crowsfoot track (2.2 m wide) bends through a pine thicket and round
  three boulders to the mouth, which you only see from the last bend. The landmark's view is from the track's start.""")
rep("""- **Loot:** deserters drop filed company badges; Hesk his stores; Caddock always drops **Caddock's Tin Crown** (head). The
  Quartermaster's strongbox (a secret chest; its key is on the Drop) holds company silver, Hesk's Shuttered Lantern
  (off-hand) and the King's orders (a Chronicle page).""",
"""- **Loot:** deserters drop filed company badges; Hesk his stores; Caddock always drops **Caddock's Tin Crown** (head). The
  Quartermaster's strongbox (a secret chest; its key is on the Drop) holds company silver, Hesk's Shuttered Lantern
  (off-hand) and the King's orders (a Chronicle page).

### The Root-Mother's Deep (the Verdant Shore; GAME-ONLY, under a CANON Temple)
- A `cavern` with `variant: 1` (`ZoneBuilder.RootDeepPlan`, `RootDeepInterior`): the same `Hollow` passage model, but grown through
  earth and root instead of cut through rock. Its mouth is the Veridian Temple's root-stair at (-112, 130), facing south into the
  Temple's sunk hollow; the passage runs 100 m north under the Emerald Cathedral and the Temple's barrow (a `shapes` mound),
  sixteen metres down. No crag lumps show on the land over it (a root deep shows nothing); the walls are bark-tinted and the
  floor loam.
- **The way down** (plan frame: the mouth at the origin, north is +z): the **root-stair** down into the **Root Gallery** (a hall
  of living root columns, floor to roof; solid); a root-choked passage east; the **Sap Well**, a chamber with a pool of emerald
  sap in a rim of pale root against its east wall (the way through stays open along the west), drips of sap from the roof and a
  Keeper's votive stones round it; the **Cold Stair** west and down; and the **Heart**, 18 m wide, where the Root-Mother is a
  vast knot of root grown out of the back wall with a hollow where a face would be and sap-light in it, fourteen root limbs off
  her into floor and roof, and before her, lodged in a root gone pale, **the cold in the root**: a black many-faced rod,
  hoarfrost spreading from it and a violet light (usable: "Salt the cold root", for the finale quest). Bones of what came down
  before, and a Silent Pilgrim's abandoned pack (a secret, by the sap-pool).
- **Light:** no torches. Veins of glowing sap on the walls every six metres or so with a dim green light at every third, fungus
  round them and drips of sap on the floor; the Sap Well's pool and the Root-Mother's eyes are the bright places. Dark enough
  that the Heart is a fifth of the daylight.
- **Camps** (all `harder`): Root-stair briars (2, 12-13), the Gallery's withered Keepers (3, 12-13), the Sap Well's mist-walkers
  (3, 13), Cold Stair briars (2, 13), the Heart's withered (2, 13) and **the Hollow Root-Warden** (elite, 13; always drops the
  Root-Warden's Crown, head). Respawns 8-15 minutes.
- Tests: `RootDeepTests` (walkable leg by leg from the zone's start to the Heart; earth and root overhead and underfoot the
  whole way; dark inside, the camps on the floor, the Warden in the Heart, the cold root usable and the quest pointing at it).""")

# 3. Life: the hen-wife's day and the trades' schedules.
rep("""  green, Nan Pennock at Brook farm), and chickens belong to the nearest coop. Her day runs like this:
  - 05:45: opens the coop, and the hens come down the ramp one by one from 06:00.
  - Morning and afternoon: scatters feed at the trough, and every hen in the yard comes running.
  - During the day: hens lay up to one egg each. She collects them from the nest boxes and carries the basket home.
  - From 18:36: hens head in on their own.
  - From 19:18: she herds the stragglers (walks round behind the farthest one), shuts the door once all are in, and goes to bed.
  - Night: hens roost inside and are hidden.""",
"""  green, Nan Pennock at Brook farm), and chickens belong to the nearest coop. Her day (Chris, 2026-10-01: "the hen
  maiden/mother should feed the chickens in the morning. water during the day. collect eggs ... she should take some eggs to
  the merchants to sell. some home to eat. some eggs to the inn for food for the village"):
  - 05:45: opens the coop, and the hens come down the ramp one by one from 06:00; then the morning feed at the trough, and
    every hen in the yard comes running.
  - 08:24: collects the eggs from the nest boxes (hens lay up to one each through the day) and carries the basket, eggs
    showing in it, to the inn's kitchen.
  - 09:36 and 13:30: draws water at the well and carries the bucket back to fill the water pan by the ramp; the hens come to
    drink, and the water sinks as the day dries it (about four hours).
  - 12:12-13:30: dinner at home.
  - 14:36: the afternoon feed.
  - 15:24: the next eggs go to the produce stall, where the merchant sells them on ("Oakhaven eggs", food, while they last).
  - 17:12: the last eggs go home for the pot.
  - From 18:36: hens head in on their own; from 19:18: she herds the stragglers (walks round behind the farthest one), shuts
    the door once all are in, and goes to bed.
  - Night: hens roost inside and are hidden.
- **Daily schedules and errands** (`Scripts/Encounter/VillageWork.cs`; Chris, 2026-10-01: "each profession should have a daily
  ai job schedule with tasks ... all jobs are intertwined"):
  - Every trade has a `WorkDay`: shifts (from-to hours, the kinds of place to work at, a repeat weighting the choice) and
    errands (once a day from an hour, dropped if not started by a later one: walk to a place, pick up a load, carry it to
    another, hand it over and say so). `Villager.ChooseNext` picks the shift's places; a due errand comes first. Off the
    schedule (a role without a day) they potter as before.
  - The loads are built from primitives on the arm or shoulder (`LoadProps`): a basket of eggs (as many as were laid), a bucket,
    sacks of grain and flour, a tray of loaves, logs, a hide, a bundle of herbs, a hare on a string, a crate of goods.
  - What is delivered where goes into the village's stock (`VillageLife.Stock`, keyed "place.good": inn.eggs, mill.grain,
    oven.flour, forge.wood...). Whoever is working at the place answers ("Ta, Goody. Warm, are they?"); the trades talk about
    each other's goods ("The miller's flour came in. Thin stuff, but it rises."); and the merchant's wares gain the eggs.
  - The chain: the farmer's barley to the mill (10:00) and seed-corn home; the miller's flour to the bakehouse (11:00) and the
    stall; the baker (up at 04:36) takes the first loaves to the inn (07:00) and a second batch to the stall; the woodcutter's logs
    to the woodyard, then firewood to the inn and the forge; the smith fetches oak for the forge and takes ironwork to the
    stall; the hunter's hide to the tannery and his hares to the inn's pot; the skinner's pelts from the snares; the
    leatherworker's belts to the stall; the herbalist's herbs to the stall, marigold for Mira at the inn, and herbs home to dry;
    the merchant shutters up at 17:48 and carries what didn't sell home; gossips and children fetch water from the well
    (and a child fetches a loaf for Mum); the elders sit on the green and at the inn; drinkers drink.
  - A trade whose errand needs a place the village lacks (no mill, no tannery) skips it for the day. Deliveries go to the
    place where someone of that trade is working now (the stall with the merchant behind it), else any of that kind.
  - Tests: `VillageWorkTests` (every trade's day covers its hours; every errand runs between known places in a window and
    carries a good; the hen-wife's seven) and `VillageErrandTests` (eggs reach the inn in a basket you can see and the merchant
    sells them on; water fills the pan and the hens come; the farmer's barley reaches the mill).""")
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print('WORLD_ZONES ok')
