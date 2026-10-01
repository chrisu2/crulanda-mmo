"""The painted style pass, part 2 (natural rock): patch. Usage: python patch_p2.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda). Apply after part 1.

   Natural rock (cliffs, crags, boulders, edge rocks, the knoll over a cave, secret cairns, the beacon's stones, the Verdant
   falls, the backdrop's tors) moves from the Standard stone to art.rock (Crulanda/PaintedRock: world-projected strata, lit
   tops) through one helper, RockTint, which falls back to art.stone while the art asset has no rock. Every tint is carried
   over unchanged. Built stone, salt, soot, bone, dark holes and cave interiors stay on art.stone.

   Every anchor in every file is checked (exactly one occurrence) before anything is written, so a second run, or a run on a
   tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'

PATCHES = [
(r'Scripts\World\ZoneArt.cs', [
("""        public Material masonry;
    }""",
"""        public Material masonry;
        [Tooltip("Crulanda/PaintedRock: natural rock (crags, cliffs, boulders), world-projected strata with lit tops. Null = ZoneBuilder falls back to stone.")]
        public Material rock;
    }"""),
]),
(r'Editor\ZoneSceneBuilder.cs', [
("""            PaintedTextures(art);   // the painted style pass: plaster, thatch, slate and timber repainted, and the masonry material (ZoneSceneBuilder.Painted.cs)
""",
"""            PaintedTextures(art);   // the painted style pass: plaster, thatch, slate and timber repainted, and the masonry material (ZoneSceneBuilder.Painted.cs)
            EnsurePaintedRock(art);   // and the painted natural rock (ZoneSceneBuilder.PaintedRock.cs)
"""),
]),
(r'Scripts\World\ZoneBuilder.cs', [
# The helper, next to Tint. Not named Rock: Cliff has a local function of that name, which would hide it there.
("""        float R01 { get { return (float)rng.NextDouble(); } }
        Transform Root(ZoneProp p, Transform parent)""",
"""        Material rockBase;
        /// <summary>Natural rock in a zone's tint: the painted rock (world-projected strata, tops lit by biome), or plain stone if the art has none.</summary>
        Material RockTint(Color color)
        {
            if (art.rock == null) return Tint(art.stone, color);
            if (rockBase == null)
            {
                rockBase = new Material(art.rock) { name = "Painted rock " + Zone.biome };   // the biome in the name keeps Tint's key apart per zone
                rockBase.SetVector("_Top", Zone.biome == "ash" || Gloom ? new Vector4(1.08f, 1.08f, 1.09f, 1) : Zone.biome == "mountain" ? new Vector4(1.08f, 1.06f, 1f, 1) : new Vector4(1.18f, 1.14f, 1.02f, 1));   // ash and gloom: neutral, grey stays grey; mountain: a small lift, the crag stays under the fog
            }
            return Tint(rockBase, color);
        }
        float R01 { get { return (float)rng.NextDouble(); } }
        Transform Root(ZoneProp p, Transform parent)"""),
# The plain "rock" prop: Stone.mat's own colour, so the tint is unchanged.
("""float s = 1 + p.variant * .6f; var stone = art.stone;""",
 """float s = 1 + p.variant * .6f; var stone = RockTint(new Color(.52f, .51f, .48f));"""),
("""stone = Tint(art.stone, MountainStone); if (string.IsNullOrEmpty(p.interact))""",
 """stone = RockTint(MountainStone); if (string.IsNullOrEmpty(p.interact))"""),
# Cliff lumps and scree.
("""            var stone = Tint(art.stone, Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog""",
 """            var stone = RockTint(Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog"""),
# Perch boulders.
("""            var mat = Tint(art.stone, Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)""",
 """            var mat = RockTint(Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)"""),
# The Wallow's two rubbing boulders (the `wet` prefix keeps this apart from the chimney that has the same stone line).
("""var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = Tint(art.stone, new Color(.42f, .4f, .37f));""",
 """var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = RockTint(new Color(.42f, .4f, .37f));"""),
# The ash Cave face (same grey as the ash Cliff it runs into); dark and salt stay on stone.
("""var rock = Tint(art.stone, new Color(.37f, .365f, .37f)); var dark""",
 """var rock = RockTint(new Color(.37f, .365f, .37f)); var dark"""),
# The knoll over a cave (the roots variant stays soil; the shell inside stays stone).
("""            var knoll = roots ? Tint(art.soil, new Color(.24f, .2f, .13f)) : Tint(art.stone, Zone.biome == "meadow" ? new Color(.33f, .34f, .29f) : new Color(.46f, .44f, .41f));""",
 """            var knoll = roots ? Tint(art.soil, new Color(.24f, .2f, .13f)) : RockTint(Zone.biome == "meadow" ? new Color(.33f, .34f, .29f) : new Color(.46f, .44f, .41f));"""),
("""            var loose = Tint(art.stone, new Color(.4f, .38f, .35f));""",
 """            var loose = RockTint(new Color(.4f, .38f, .35f));"""),
# The cairn's greyed top stone, and the stone of every find's scene.
("""Unmade(at.x, at.y) > .2f ? Tint(art.stone, new Color(.6f, .6f, .62f)) : null""",
 """Unmade(at.x, at.y) > .2f ? RockTint(new Color(.6f, .6f, .62f)) : null"""),
("""            return Tint(art.stone, c * shade);""",
 """            return RockTint(c * shade);"""),
# The beacon's fire-cracked stones (soot stays on stone: it also paints the flat charred disc).
("""var burnt = Tint(art.stone, new Color(.28f, .26f, .24f));""",
 """var burnt = RockTint(new Color(.28f, .26f, .24f));"""),
# EdgeRock.
("""            var mat = Tint(art.stone, Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));""",
 """            var mat = RockTint(Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));"""),
# The backdrop's tors: their own clone at 22 m a tile, since 5 m tiles mip to a flat colour at that distance.
("""            var rockMat = Tint(art.stone, ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));""",
 """            var rockMat = RockTint(ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));
            if (rockMat.HasProperty("_Scale")) { rockMat = new Material(rockMat) { name = "Backdrop rock" }; rockMat.SetFloat("_Scale", 22); }   // painted rock: 22 m a tile (beds of 2.6 to 5.7 m), so the strata still read from the zone; its own clone, the near boulders keep 5 m"""),
]),
(r'Scripts\World\ZoneBuilder.Verdant.cs', [
("""            var rock = Tint(art.stone, new Color(.44f, .44f, .42f)); var mossy = Tint(art.stone, new Color(.3f, .38f, .22f));""",
 """            var rock = RockTint(new Color(.44f, .44f, .42f)); var mossy = RockTint(new Color(.3f, .38f, .22f));"""),
]),
]

# Read and check everything first; write only when every anchor in every file matched exactly once.
out = []
for rel, reps in PATCHES:
    path = os.path.join(ROOT, rel)
    assert os.path.isfile(path), ('missing file', path)
    s = io.open(path, encoding='utf-8', newline='').read()
    assert 'RockTint' not in s and 'EnsurePaintedRock' not in s and 'public Material rock;' not in s, ('already patched', path)
    crlf = '\r\n' in s
    for a, b in reps:
        if crlf: a, b = a.replace('\n', '\r\n'), b.replace('\n', '\r\n')
        assert s.count(a) == 1, ('anchor must occur exactly once', path, s.count(a), a[:90])
        s = s.replace(a, b)
    out.append((path, s))
for path, s in out:
    io.open(path, 'w', encoding='utf-8', newline='').write(s); print('patched', os.path.basename(path))
print('patched p2')
