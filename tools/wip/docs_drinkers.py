"""Docs: the out of work at the inn; painted masonry; the first worklist batch."""
import io
D = 'D:/code/mmo/New Unity Project/Docs/'
def patch(name, reps, append=None):
    p = D + name; s = io.open(p, encoding='utf-8', newline='').read()
    crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
    for a, b in reps:
        assert s.count(a) == 1, (name, a[:70])
        s = s.replace(a, b)
    if append: s = s.rstrip('\n') + '\n' + append
    io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print(name, 'ok')

patch('WORLD_ZONES.md', [
("  - Tests: `VillageWorkTests` (every trade's day covers its hours;",
 """  - **The out of work drink** (Chris, 2026-10-01: "have unemployed npc show up at the inn and drink till gone or passed out"):
    the drinkers, and anyone whose trade this village has no place for (a smith with no forge, a farmer with no fields: in a
    village with an inn they become drinkers), loiter the morning away and are at the inn from 11:00. Each round is a tankard off
    the day's cask (`inn.ale` in the village's stock: ten left from yesterday each dawn, twelve more when the merchant brings "a
    cask for the inn" at 08:36), drunk sitting with the tankard in hand (`ActorPose.Drink`), and leaves them a little further gone;
    their talk slurs with it. When the cask is dry they grumble and call it a day. Past their limit (it differs by person) some
    fold over the table and snore for three to five hours (`ActorPose.Slump`; "Zzz..." is all you get from them), the rest say
    goodnight and reel home (`ActorVisual.Stagger`); spent, they stay indoors until morning. `VillageDrinkTests`.
  - Tests: `VillageWorkTests` (every trade's day covers its hours;"""),
("- **Chunky props** (part 4;",
 """- **Built stone** (part 5; `Dressed`, `Ashlar`, `Stonework`, `ZoneMeshes.Spire`): towers as one turned masonry drum with a plinth
  course and a corbel ring under the battlements, slate spire roofs, arrow slits; curtain walls, the gate and the keep as one
  joined masonry mesh each with coping and merlons; crypts, ruins, wayshrines and the Cracked Hearth's chimney in coursed stone;
  headstones, waystones and altars as painted rock (five courses on a headstone read as a toy pillar).
- **Chunky props** (part 4;"""),
])
patch('CHANGELOG.md', [], append="""
## 2026-10-01 — Painted masonry, and the out of work at the inn
- **Built stone:** towers, curtain walls, the toll gate, the keep, crypts, ruins and wayshrines in painted coursed masonry (turned
  drums with plinth and corbel courses, slate spires, coping); headstones and waystones in painted rock.
- **The out of work drink at the inn** (Chris: "have unemployed npc show up at the inn and drink till gone or passed out"): the
  drinkers, and any trade a village has no workplace for, are at the inn from 11:00, a tankard in hand, drinking the day's cask
  (the merchant brings a fresh one each morning). When the ale is gone they go home; past their limit some pass out over the table
  and the rest reel home to sleep it off.
- **The first batch of the visual review's worklist** (four reviewers looked at every capture of the published painted pass and
  ranked what still falls short; `tools/wip/painted/visual_review.md`): Oakhaven's haze turned to blue air; storms that darken
  the day; night with firelit windows and a moonlit blue base instead of bleached white on mud; broadleaf crowns built of leafy
  lumps instead of one brown ball; a greener, denser meadow whose tufts fade out with distance; hedges and haystacks with shape;
  gable ends that are wall, with barge boards and a tie beam, instead of roof.
""")
