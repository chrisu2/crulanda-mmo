"""Docs for the review fixes (2026-10-01): WORLD_ZONES, QUEST_DESIGN, CHANGELOG."""
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
("""- **Camps** (all `harder`): Root-stair briars (2, 12-13), the Gallery's withered Keepers (3, 12-13), the Sap Well's mist-walkers
  (3, 13), Cold Stair briars (2, 13), the Heart's withered (2, 13) and **the Hollow Root-Warden** (elite, 13; always drops the
  Root-Warden's Crown, head). Respawns 8-15 minutes.""",
"""- **Camps** (all `harder`): Root-stair briars (2, 12-13), the Gallery's withered Keepers (3, 12-13), the Sap Well's mist-walkers
  (3, 13), Cold Stair briars (2, 13), the Heart's withered (2, 13) and **the Hollow Root-Warden** (elite, 13; always drops the
  Root-Warden's Crown, head). Respawns 8-15 minutes. The deep's Keepers and walkers carry their own tags (`deepwithered`,
  `deepwalker`, with the surface camps' loot), so the finale counts only kills made down here.
- **The Heart's roof** stays 8 m high to its back wall (the plan's rows at z 95 and 98), then closes at once: the Root-Mother
  stands seven metres tall against it, face and all under the roof.
- **Salting the cold** is once and for good (saved): the rod, its violet light and the hoarfrost go; the pale root stays.
- **A place down a cave is visited from down in the cave** (`EncounterSession.OnItsLevel`): where a passage floor lies under a
  quest's `visit` point, the visitor must stand within four metres of that floor's height, so the Root Gallery is not "reached"
  from the barrow over it. At a cave's mouth the two heights are the same."""),
])
patch('QUEST_DESIGN.md', [
("""  dungeon under the Temple (`WORLD_ZONES.md`, "Caves you walk into"): visit the Root Gallery, lay five withered Keepers and three
  mist-walkers to rest,""",
"""  dungeon under the Temple (`WORLD_ZONES.md`, "Caves you walk into"): visit the Root Gallery (from inside, not from the barrow
  above), lay five withered Keepers and three mist-walkers of the deep to rest (`mob.deepwithered.verdant*`,
  `mob.deepwalker.verdant*`: the surface camps do not count),"""),
])
patch('CHANGELOG.md', [], append="""
## 2026-10-01 — A review of the deep and the trades' days, before publishing
A read-only review by five reviewers, each finding checked by a second who tried to refute it, found real defects. Fixed:
- **Grey wolves were about to become ash hounds:** a new name rule for the Verdant creatures ("Grey ") caught every Grey wolf in
  Oakhaven and Khaven. Removed.
- **The hens' water never showed:** the disc sat inside a solid pan. A shallow pan, the water lying in its top.
- **The finale never gave its page** (`rewards.documents` is a list), its kills could be made at the surface camps (the deep's
  camps have their own tags now), and its first step completed from the barrow over the Gallery (a place down a cave is
  visited from down in the cave).
- **The Root-Mother:** her roots "into the floor" went 14 m up to the mouth's level; her upper knot, face and eyes were above
  the roof at the Heart's tapered end (the Heart stays tall to its back wall now); the salted cold came back after 90 seconds
  (it stays gone, the pale root stays).
- **The trades:** the village's stock is the day's (cleared before dawn); a delivery home says its line at the door, then goes
  in; the hen-wife speaks of eggs at the inn or the stall only once they got there; nobody could draw water with a collector
  seven metres from the well (villagers keep 7 m from enemies, not 12, and a blocked errand says so).
- **Tests:** three flaky spots in `VillageErrandTests`; `run_focus.ps1` (a fast filtered run) registers the content as the full
  run does.
""")
