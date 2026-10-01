"""Docs for the painted style pass (2026-10-01): WORLD_ZONES section, CHANGELOG entry."""
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
("## Level ladder and camps (GAME-ONLY)",
"""## The painted style pass (2026-10-01; `ZoneSceneBuilder.Painted*.cs`, `Crulanda/PaintedRock`)
Chris: "keep going with the painted style pass". The older zones take the hand-painted look the Verdant Shore set; each keeps its
mood. How it was made: a first draft was pre-checked by agents who ported the generated textures to Python and looked at them
(the draft did not tile and did not read as painted), then restaged as patch scripts verified on a scratch copy
(`tools/wip/painted/v2`, parts p1 to p5), applied one part at a time, each toured and looked at.
- **Painted textures** (generated once by the editor like the rest of the art; PNG names end in a number because a generated PNG
  is written once and then reused): plaster in broad brush patches with elongated strokes and a few stains; thatch in seven ragged
  layers a tile; slate in staggered shingles of uneven value with a lit edge and moss; coursed stone (`masonry`, its own material,
  so natural rock is untouched) in five uneven courses with rounded blocks; timber in planks with grain and knots; bedded rock.
  All seamless (`Tiled` blends the noise across the tile's edges; any stretch or offset goes inside the sample).
- **Metre UVs:** walls, plinths and chimneys are `ZoneMeshes.Box` (through `ZoneBuilder.BoxPart`), 2 m a tile with the texture's
  origin at the building's, so plaster runs unbroken across an inn's wall pieces. Roofs tile 2.5 m, v running the true distance
  up the slope (`GableRoof`; |z| + y hardly changes up a 45 degree roof, which had smeared one row over the whole slope).
- **Building trim** (`Eaves`, `Window`, `PlankDoor`): fascia boards and rafter ends under the eaves, a ridge cap, framed windows
  with sills and shutters (dropped where a corner post or the door is in the way), plank doors with iron bands and a ring,
  masonry plinths, chimneys with caps.
- **Natural rock** (`RockTint`, `art.rock`, shader `Crulanda/PaintedRock`): the bedded-rock paint projected in world space on three
  axes, so strata run level across a cliff of separate lumps; upward faces take a top light (`_Top`, set per biome: warm in the
  meadow, near neutral on ash and in gloom), undersides sink into shade. On cliffs, crags, boulders, edge rocks, the cave knoll,
  secret stones, the waterfall and the backdrop tors (their own clone, 22 m a tile). Cave interiors stay on plain stone.
- **Sky, light and ground** (zone JSON `lighting`, `ZonePost` grades, the meadow ground palette): Oakhaven a blue sky, a greener
  meadow and bluer distance; the Peaks crisp alpine blue with a warm sun; Khaven keeps its dusk with the warm sun split from the
  violet shade; the Ash Rim keeps its grey with deeper shadow and a little more distance.
- **Chunky props** (part 4; `PropMesh`, `Turned`, `Cutout`, `Stake`, `Barrel`, `Crate`, `Wheel`, `Signpost`): hewn capped fence
  posts with lapped rails that follow each post's lean; carts with spoked wheels, side boards and shafts; lamp posts on a masonry
  footing with an iron bracket and a roofed lantern; the well as a turned masonry ring with a roofed windlass, rope and bucket;
  stalls with a scalloped valance, thicker posts and turned baskets and pots; turned barrels with hoops; battened crates; a
  woodpile with pale cut ends and a sawhorse; stone bridges with coping and corner piers; leaning signposts.
- **The rules every part kept:** the zone's random stream is untouched (each prop method makes the draws it always made, in the
  same order), every collider, navmesh blocker, workplace, light and registered door or usable prop is where it was, and the
  tests ran green after each part.
- **The art is in the repo now:** `tools/validation/build_art.ps1` builds new generated art in the validation copy and brings it
  home (before, each run's mirror deleted it and the register step made it again with new GUIDs).

## Level ladder and camps (GAME-ONLY)"""),
])
patch('CHANGELOG.md', [], append="""
## 2026-10-01 — The painted style pass (Chris: "keep going with the painted style pass")
- **Buildings:** hand-painted plaster, thatch in ragged layers, slate shingles, coursed masonry and planked timber, on walls and
  roofs whose textures now tile per metre; eaves with fascia and rafter ends, ridge caps, framed windows with sills and shutters,
  plank doors with iron bands, masonry plinths and capped chimneys.
- **Rock:** cliffs, crags and boulders in bedded painted rock with lit tops (a new shader, `Crulanda/PaintedRock`).
- **Colour:** Oakhaven under a blue sky on a green meadow; the Peaks crisp and blue; Khaven and the Ash Rim keep their moods
  with more depth.
- **Props:** chunky, hand-made fences, carts, lamps, wells, stalls, barrels, crates, woodpiles, bridges and signposts.
- **How:** drafted, pre-checked by agents that rendered and looked at the textures and recomputed the geometry, restaged as
  verified patches, then applied a part at a time with the tests and a capture tour after each. Found on the first tour and
  fixed: roof textures smeared up the slope (the roof's UV used |z| + y, which is constant up a 45 degree roof); the rock's
  joints made cliffs read as dry-stone walls.
- The zone layouts are unchanged (the random stream is untouched) and so is everything gameplay stands on.
""")
