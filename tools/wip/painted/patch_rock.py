"""The painted style pass, part 2 (rock): natural rock (cliffs, crags, boulders, edge rocks, the knoll over a cave, secret cairns,
   the Verdant falls) moves from the Standard stone to art.rock (Crulanda/PaintedRock: world-projected strata, lit tops).
   Apply after ZoneArt has `rock` and the shader is in Assets/Crulanda/World/Shaders."""
import io
W = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World'
def patch(path, reps):
    s = io.open(path, encoding='utf-8', newline='').read()
    for a, b in reps:
        assert s.count(a) == 1, (path, a[:80])
        s = s.replace(a, b)
    io.open(path, 'w', encoding='utf-8', newline='').write(s); print('patched', path.split('\\')[-1])

patch(W + r'\ZoneBuilder.cs', [
# The helper, next to Tint.
("""        float R01 { get { return (float)rng.NextDouble(); } }
        Transform Root(ZoneProp p, Transform parent)""",
"""        /// <summary>Natural rock in a zone's tint: the painted rock (world-projected strata, lit tops), or plain stone if the art has none.</summary>
        Material Rock(Color color) { return Tint(art.rock != null ? art.rock : art.stone, color); }
        float R01 { get { return (float)rng.NextDouble(); } }
        Transform Root(ZoneProp p, Transform parent)"""),
("""            var stone = Tint(art.stone, Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog""",
 """            var stone = Rock(Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog"""),
("""            var mat = Tint(art.stone, Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)""",
 """            var mat = Rock(Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)"""),
("""            var knoll = roots ? Tint(art.soil, new Color(.24f, .2f, .13f)) : Tint(art.stone, Zone.biome == "meadow" ? new Color(.33f, .34f, .29f) : new Color(.46f, .44f, .41f));""",
 """            var knoll = roots ? Tint(art.soil, new Color(.24f, .2f, .13f)) : Rock(Zone.biome == "meadow" ? new Color(.33f, .34f, .29f) : new Color(.46f, .44f, .41f));"""),
("""            var loose = Tint(art.stone, new Color(.4f, .38f, .35f));""",
 """            var loose = Rock(new Color(.4f, .38f, .35f));"""),
("""            return Tint(art.stone, c * shade);""",
 """            return Rock(c * shade);"""),
("""            var mat = Tint(art.stone, Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));""",
 """            var mat = Rock(Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));"""),
("""            var rockMat = Tint(art.stone, ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));""",
 """            var rockMat = Rock(ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));"""),
])
patch(W + r'\ZoneBuilder.Verdant.cs', [
("""            var rock = Tint(art.stone, new Color(.44f, .44f, .42f)); var mossy = Tint(art.stone, new Color(.3f, .38f, .22f));""",
 """            var rock = Rock(new Color(.44f, .44f, .42f)); var mossy = Rock(new Color(.3f, .38f, .22f));"""),
])
