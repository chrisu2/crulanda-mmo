"""QUEST_DESIGN, CHANGELOG, KNOWN_ISSUES and CLAUDE_HANDOFF: the deep, the hidden mouth, the trades' days."""
import io
D = r'D:\code\mmo\New Unity Project\Docs'
def patch(name, reps, append=None):
    p = D + '\\' + name
    s = io.open(p, encoding='utf-8', newline='').read()
    crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
    for a, b in reps:
        assert s.count(a) == 1, (name, a[:70])
        s = s.replace(a, b)
    if append: s = s.rstrip('\n') + '\n' + append
    io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print(name, 'ok')

patch('QUEST_DESIGN.md', [
("""The Chronicle runs `main.ashrim.5` -> `main.verdant.1` -> 2 -> 3 -> 4 (XP and gold follow the Rim's curve, +40-60 XP a level).""",
 """The Chronicle runs `main.ashrim.5` -> `main.verdant.1` -> 2 -> 3 -> 4 -> 5 (XP and gold follow the Rim's curve, +40-60 XP a level)."""),
("""  reaching the Shore (PROVISIONAL: Book 3's fog, foreshadowed). Willow-Whisper's last words foreshadow Book 3's survivors coming
  over the ridge, without playing it out.""",
 """  reaching the Shore (PROVISIONAL: Book 3's fog, foreshadowed). **The Root-Mother's Deep** (`main.verdant.5`, 13, after the Fog):
  the fog came up out of the ground; Willow-Whisper sends you down the Temple's root-stair the living roots closed, into the
  dungeon under the Temple (`WORLD_ZONES.md`, "Caves you walk into"): visit the Root Gallery, lay five withered Keepers and three
  mist-walkers to rest, bring down the Hollow Root-Warden (elite 13; he was Thorn-Hand, who planted the Guest-Tree) in the Heart,
  salt the cold in the root ("The cold in the root", usable, with any salt: Grohl's, the First Shore's) and return. 820 XP, 200
  gold, Keeper standing 600, and a fifth page, *What the Root-Mother Dreams* (Sister Iselle, from Willow-Whisper's lips: the
  Pale's king who wears a mirror, named the way you would name a disease). Willow-Whisper's last words foreshadow Book 3's
  survivors coming over the ridge, without playing it out."""),
("""- **A new faction**, the Veridian Keepers (`keepers`, CANON), with standing from the quests. Four Chronicle pages.""",
 """- **A new faction**, the Veridian Keepers (`keepers`, CANON), with standing from the quests. Five Chronicle pages."""),
])

patch('KNOWN_ISSUES.md', [
("""- Eggs are counted, not yet items. The hen-wife's basket is cosmetic.""",
 """- Eggs are counted at the coop; they become an item ("Oakhaven eggs") only once the hen-wife has sold them to the stall. The
  other goods the trades carry (grain, flour, bread, wood, hides, herbs) are a stock count the village talks about, not items.
- The village's stock and the day's errands are not saved: a new load starts the day's deliveries again."""),
])

patch('CHANGELOG.md', [], append="""
## 2026-10-01 — The Root-Mother's Deep, and Crowsfoot's mouth hidden in the hill (Chris: "can you also create a dungeon for the new zone? the dungeon in the first zone kind of stick out as just a rock. it needs to be built into a mountain or something and kinda hidden..no so obvious. they are bandits")
- **Crowsfoot Hollow's mouth** is a slot in a cliff face now, not a rock on the grass: two cliff scarps either side, the Crowsfoot
  brow raised over it, the North road ending short and a track bending through a pine thicket and round boulders to a mouth you
  only see from the last bend.
- **The Root-Mother's Deep**, the Verdant Shore's dungeon (GAME-ONLY, under the CANON Temple): a second cavern plan (`variant: 1`)
  grown through earth and root. The Temple's root-stair down into the Root Gallery (living root columns), the Sap Well (a pool
  of emerald sap against the wall, votive stones, drips from the roof), the Cold Stair, and the Heart: the Root-Mother, a vast
  knot of root with a hollow face and sap-light in it, and the cold lodged in her root (a black rod, hoarfrost, violet light).
  Sap veins for light instead of torches; no knoll shows on the land over it. Six camps down it (briars, withered Keepers,
  mist-walkers) and the Hollow Root-Warden (elite 13, drops the Root-Warden's Crown). The finale quest *The Root-Mother's Deep*
  (`main.verdant.5`), a fifth page, a Pilgrim's abandoned pack (a secret). Three new PlayMode tests (`RootDeepTests`).
- **Found by the tests:** the sap pool sat in the middle of the passage and cut the way (and a camp) off the navmesh; it hugs
  the east wall now and the way through runs along the west.

## 2026-10-01 — Every trade has a day, and the goods go round (Chris: "the hen maiden/mother should feed the chickens in the morning. water during the day. collect eggs. each profession should have a daily ai job schedule with tasks ... she should take some eggs to the merchants to sell. some home to eat. some eggs to the inn for food for the village, etc. all job are intertwined.")
- **Daily schedules** (`VillageWork.cs`): each trade's day is shifts (where to be, hour by hour) and errands (once a day, from an
  hour: pick something up at one place, carry it to another, hand it over, say so). Villagers follow the shift for the hour and
  run a due errand first; a trade whose errand needs a place the village lacks skips it.
- **The hen-wife:** opens up and feeds at first light; eggs to the inn's kitchen at 08:24; water from the well to a new pan by the
  ramp at 09:36 and 13:30 (the hens come to drink; the water dries over four hours); dinner at home; the afternoon feed; eggs to
  the produce stall at 15:24; the last eggs home for the pot; the hens at dusk as before.
- **The goods go round:** barley from the fields to the mill, flour to the bakehouse and the stall, the first loaves (the baker is
  up at 04:36) to the inn, logs to the woodyard and firewood to the inn and the forge, the hunter's hide to the tannery and his
  hares to the inn's pot, pelts from the snares, belts and ironwork to the stall, herbs to the stall and marigold for Mira, water
  from the well for every house, a loaf for Mum. Every load shows in their hands: a basket of eggs (as many as were laid), a
  bucket, sacks, a tray of loaves, logs on the shoulder, a hide, a bundle of herbs, a hare on a string, a crate.
- **Intertwined:** deliveries fill the village's stock; whoever is at the place answers; the trades talk about each other's goods
  ("The miller's flour came in. Thin stuff, but it rises."); and the merchant sells **Oakhaven eggs** (a new food) while the
  hen-wife's eggs last at the stall.
- Tests: `VillageWorkTests` (the schedules) and `VillageErrandTests` (eggs to the inn and sold on by the merchant; water to the
  pan; barley to the mill). The capture tour adds an errands line-up (`99-errands-lineup`).
""")

patch('CLAUDE_HANDOFF.md', [
("""## RESUME HERE (updated 2026-10-01, early morning)""", """## RESUME HERE (updated 2026-10-01, morning)"""),
("""    waterfalls, mushrooms, real dead trees). Design: `WORLD_ZONES.md` "The Verdant Shore" and "Painted plants and lush props",
    `QUEST_DESIGN.md`, the CHANGELOG.""",
 """    waterfalls, mushrooms, real dead trees). Design: `WORLD_ZONES.md` "The Verdant Shore" and "Painted plants and lush props",
    `QUEST_DESIGN.md`, the CHANGELOG.
  - and now **THE ROOT-MOTHER'S DEEP and the hidden Crowsfoot mouth** (published 2026-10-01 morning): the Verdant Shore's
    dungeon under the Temple (a second cavern plan, `variant: 1`, grown through earth and root: the Root Gallery, the Sap Well,
    the Cold Stair, the Heart with the Root-Mother and the cold in her root; six camps and the Hollow Root-Warden; the finale
    quest `main.verdant.5`); and Crowsfoot's mouth as a slot in a cliff face at the end of a bent track, not a rock on the
    grass. Design: `WORLD_ZONES.md` "Caves you walk into", `QUEST_DESIGN.md`, the CHANGELOG.
  - and now **EVERY TRADE HAS A DAY** (published 2026-10-01 morning): `VillageWork.cs`, daily shifts and errands for every
    trade, goods carried in their hands between the trades (eggs, water, grain, flour, bread, logs, hides, herbs, hares,
    wares), the village's stock, the trades talking about each other's goods, the hen-wife's full day (feed, eggs to the inn,
    water to the hens' pan, dinner, feed, eggs to the stall, eggs home) and Oakhaven eggs on the merchant's stall. Design:
    `WORLD_ZONES.md` "Life, day and night", the CHANGELOG."""),
("""~~THE VERDANT SHORE~~ DONE and published 2026-10-01. **NEXT JOB: THE PAINTED STYLE PASS on the four older zones**, reusing""",
 """~~THE VERDANT SHORE~~, ~~THE ROOT-MOTHER'S DEEP~~ and ~~EVERY TRADE HAS A DAY~~ DONE and published 2026-10-01. **NEXT JOB: THE
PAINTED STYLE PASS on the four older zones**, reusing"""),
])
