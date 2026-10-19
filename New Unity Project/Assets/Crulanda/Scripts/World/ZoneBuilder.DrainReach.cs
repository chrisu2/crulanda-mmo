using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Drain Reach (GAME-ONLY place; Docs/WORLD_BOSS_1.md 1.3, build step 3): the Chemical River's north reach between the Rag Bridge and the
    /// zone's north edge, where the Drain Courts' drains empty into it, and where the Unraveller rises. Its ground is data (lowtowns.json
    /// "shoals", ZoneShoal; ZoneWater raises it and widens the river round it): a silt bar in the river's bend (about 30 m by 16 m, level and
    /// standable, the water wadeable round it on both sides), a neck of mud joining it to the west bank, and the brick apron of wet setts on the
    /// bank (about 9 m by 30 m). Here they are dressed: black mud and brick rubble on the bar, setts on the apron. No machines: mud, brick,
    /// timber, bone, rope and salt.
    /// While she is coming (the warning, five minutes before) and while she is up, the session turns on (<see cref="SetReachWarned"/>,
    /// <see cref="SetReachStock"/>): the reach's water stills and greys over, the Drain Courts' lamps gutter out, and Tibb Marrow's salt
    /// barrel and Ketta Rune's harpoon rack stand at the apron's north and south ends.
    /// The river is the books' Chemical River (CANON, book1 ch.15:335, ch.16:5, ch.16:75); the bar, the apron and the warnings are GAME-ONLY.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        public const string SiltBarName = "The silt bar", ApronName = "The Drain Reach apron";
        /// <summary>The silt bar's shoal (null in a zone without it).</summary>
        public ZoneShoal SiltBar { get { return Zone == null || Zone.shoals == null ? null : System.Array.Find(Zone.shoals, s => s != null && s.name == SiltBarName); } }
        /// <summary>The apron's shoal (null in a zone without it).</summary>
        public ZoneShoal ReachApron { get { return Zone == null || Zone.shoals == null ? null : System.Array.Find(Zone.shoals, s => s != null && s.name == ApronName); } }
        /// <summary>Where the salt barrel and the harpoon rack stand when they are out (the apron's north and south ends, at its river side), and
        /// where Tibb Marrow and Ketta Rune stand by them.</summary>
        public Vector3 ReachBarrelAt { get; private set; }
        public Vector3 ReachRackAt { get; private set; }
        public Vector3 ReachTibbAt { get; private set; }
        public Vector3 ReachKettaAt { get; private set; }
        /// <summary>The reach's water stilled and greyed (shown with the warning).</summary>
        public bool ReachStill { get { return reachStill != null && reachStill.activeSelf; } }
        /// <summary>The salt barrel and the harpoon rack set out.</summary>
        public bool ReachStockOut { get { return reachStock != null && reachStock.activeSelf; } }
        /// <summary>The Drain Courts' lamps guttered out.</summary>
        public bool DrainLampsOut { get; private set; }
        /// <summary>How far round the bar a lamp counts as the reach's (besides every lamp in the Drain Courts themselves).</summary>
        public const float ReachLampRange = 70;
        GameObject reachStill, reachStock;
        readonly List<(NightLight lamp, float day, float night)> outLamps = new List<(NightLight, float, float)>();

        void BuildDrainReach()
        {
            reachStill = reachStock = null; outLamps.Clear(); DrainLampsOut = false;
            if (Zone == null || Zone.shoals == null || Zone.shoals.Length == 0 || Water == null) return;
            var root = MarksRoot("The Drain Reach");
            var b = new Batch(); var r = new System.Random(Zone.seed * 13 + 191);
            var mud = Tint(art.stone, new Color(.075f, .07f, .065f)); var wetMud = Tint(art.stone, new Color(.05f, .048f, .045f));
            var brick = SootBrick(2); var brick2 = SootBrick(4);
            foreach (var w in Water.Shoals)
            {
                var s = w.def; var q = Quaternion.Euler(0, s.rotation, 0);
                // Its top, a hand over the ground (the ground under it is walked; no collider), a little in from its edge where it is level.
                var axis = q * Vector3.forward; var c = new Vector3(s.center.x, 0, s.center.y);
                var a2 = c - axis * (s.size.y / 2 - .2f); var b2 = c + axis * (s.size.y / 2 - .2f);
                var line = Densify(new[] { new Vector2(a2.x, a2.z), new Vector2(b2.x, b2.z) }, 1.25f);
                var top = MeshPart(ZoneMeshes.Ribbon(line, Mathf.Max(.5f, s.size.x - .3f), p => HeightAt(p.x, p.y) + .04f), root, Vector3.zero, s.kind == "apron" ? WetSetts : mud);
                top.name = (s.kind == "apron" ? "Setts " : "Silt ") + s.name;
                if (s.kind == "apron") continue;
                // The bar's rubble: broken brick from the drains, half sunk in the mud, and slick black patches where the water seeps up. Only
                // to see (a brick is ankle-high at most), none on the bar's middle where she rises.
                int n = Mathf.RoundToInt(s.size.x * s.size.y / 9);
                for (int i = 0; i < n; i++)
                {
                    float lx = ((float)r.NextDouble() - .5f) * (s.size.x - .8f), lz = ((float)r.NextDouble() - .5f) * (s.size.y - .8f);
                    if (Mathf.Abs(lx) < 3.5f && Mathf.Abs(lz) < 3.5f) continue;
                    var at = c + q * new Vector3(lx, 0, lz); at.y = HeightAt(at.x, at.z);
                    var turn = Quaternion.Euler(((float)r.NextDouble() - .5f) * 30, (float)r.NextDouble() * 360, ((float)r.NextDouble() - .5f) * 30);
                    if (r.NextDouble() < .7) b.Box(r.NextDouble() < .5 ? brick : brick2, at + Vector3.up * .03f, new Vector3(.22f, .07f, .11f) * (1 + (float)r.NextDouble() * .4f), turn);
                    else b.Box(wetMud, at + Vector3.up * .045f, new Vector3(.6f + (float)r.NextDouble(), .01f, .5f + (float)r.NextDouble()), Quaternion.Euler(0, (float)r.NextDouble() * 360, 0));
                }
            }
            b.Flush(this, root, "The Drain Reach");
            var bar = SiltBar; var apron = ReachApron;
            if (bar != null) BuildReachStill(root, bar);
            if (apron != null) BuildReachStock(root, apron);
            Debug.Log("Lowtowns: the Drain Reach built (" + Water.Shoals.Count + " shoals).");
        }

        /// <summary>The stilled water over the reach (hidden until the warning): the river's own rows from 25 m below the bar to 25 m above it, a
        /// dull grey film at the water's level, matt, no drift.</summary>
        void BuildReachStill(Transform root, ZoneShoal bar)
        {
            var w = Water.Shoals.Find(x => x.def == bar); if (w == null) return;
            var c = w.creek; var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i < c.pts.Length; i++)
            {
                ZoneWater.ShoalFrame(bar, c.pts[i].x, c.pts[i].y, out _, out float lz);
                if (Mathf.Abs(lz) > bar.size.y / 2 + 25) continue;
                var dir = i < c.pts.Length - 1 ? c.pts[i + 1] - c.pts[i] : c.pts[i] - c.pts[i - 1]; var side = new Vector2(-dir.y, dir.x).normalized * c.DrawHalf(c.wide[i]);
                var l = c.pts[i] + side; var rr = c.pts[i] - side;
                v.Add(new Vector3(l.x, c.level[i] + .06f, l.y)); v.Add(new Vector3(rr.x, c.level[i] + .06f, rr.y)); uv.Add(new Vector2(0, i)); uv.Add(new Vector2(1, i));
                if (v.Count >= 4) { int k = v.Count - 4; t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 }); }
            }
            if (t.Count == 0) return;
            var mesh = new Mesh { name = "The Drain Reach, stilled" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.SetUVs(0, uv);
            var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up; mesh.normals = n; mesh.RecalculateBounds();
            var shader = Shader.Find("Standard");
            var m = shader != null ? new Material(shader) { name = "Stilled water", color = new Color(.2f, .205f, .21f) } : new Material(art.stone) { color = new Color(.2f, .205f, .21f) };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .12f);
            reachStill = MeshPart(mesh, root, Vector3.zero, m); reachStill.name = "The Drain Reach, stilled";
            var mr = reachStill.GetComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            reachStill.SetActive(false);
        }

        /// <summary>Tibb Marrow's salt barrel at the apron's north end and Ketta Rune's harpoon rack at its south end, by the water (hidden until
        /// the warning): an oak barrel heaped with grey salt and a stack of paper twists; a timber rack of bone harpoons on amber rope.</summary>
        void BuildReachStock(Transform root, ZoneShoal apron)
        {
            var q = Quaternion.Euler(0, apron.rotation, 0); var c = new Vector3(apron.center.x, 0, apron.center.y);
            Vector3 On(float lx, float lz) { var p = c + q * new Vector3(lx, 0, lz); p.y = HeightAt(p.x, p.z); return p; }
            float riverSide = apron.size.x / 2 - 1.6f, ends = apron.size.y / 2 - 2.5f;
            // The barrel 25 m up from the rack, by the bar, where it always stood (the apron runs on north past it since tune-8, for her leap).
            float north = Mathf.Min(ends, -ends + 25);
            ReachBarrelAt = On(riverSide, north); ReachRackAt = On(riverSide, -ends);
            ReachTibbAt = On(riverSide - 1.6f, north - .4f); ReachKettaAt = On(riverSide - 1.6f, -ends + .4f);
            var stock = new GameObject("The Drain Reach stock").transform; stock.SetParent(root, false);
            var oak = Tint(art.timber, new Color(.32f, .22f, .14f)); var hoop = SootIron; var salt = Tint(art.stone, new Color(.78f, .78f, .74f));
            var paper = Tint(art.stone, new Color(.62f, .58f, .5f)); var bone = Tint(art.stone, new Color(.86f, .8f, .62f));
            var amber = Glowing(new Color(1f, .62f, .18f), .6f); var timber = TarredTimber;
            // The salt barrel: an oak barrel, two iron hoops, heaped with salt; a crate of twists beside it.
            var barrel = new GameObject("Tibb Marrow's salt barrel").transform; barrel.SetParent(stock, false); barrel.position = ReachBarrelAt;
            Part(PrimitiveType.Cylinder, barrel, new Vector3(0, .5f, 0), new Vector3(.8f, .5f, .8f), oak);
            Part(PrimitiveType.Cylinder, barrel, new Vector3(0, .2f, 0), new Vector3(.84f, .03f, .84f), hoop);
            Part(PrimitiveType.Cylinder, barrel, new Vector3(0, .8f, 0), new Vector3(.84f, .03f, .84f), hoop);
            Part(PrimitiveType.Sphere, barrel, new Vector3(0, 1.0f, 0), new Vector3(.74f, .3f, .74f), salt);
            Part(PrimitiveType.Cube, barrel, new Vector3(.75f, .2f, .1f), new Vector3(.5f, .4f, .4f), oak);
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Sphere, barrel, new Vector3(.62f + (i % 3) * .13f, .44f, -.02f + (i / 3) * .16f), new Vector3(.12f, .08f, .12f), paper);
            // The harpoon rack: two timber posts and a rail, six bone harpoons leaning on it, a coil of amber rope at its foot.
            var rack = new GameObject("Ketta Rune's harpoon rack").transform; rack.SetParent(stock, false); rack.position = ReachRackAt; rack.rotation = q;
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, rack, new Vector3(0, .75f, s * .9f), new Vector3(.12f, 1.5f, .12f), timber);
            Part(PrimitiveType.Cube, rack, new Vector3(0, 1.3f, 0), new Vector3(.1f, .1f, 1.95f), timber);
            for (int i = 0; i < 6; i++)
            {
                float z = -.75f + i * .3f;
                Part(PrimitiveType.Cylinder, rack, new Vector3(.18f, .85f, z), new Vector3(.05f, .85f, .05f), bone, Quaternion.Euler(0, 0, -12));
                Part(PrimitiveType.Cube, rack, new Vector3(.36f, 1.72f, z), new Vector3(.05f, .2f, .09f), bone, Quaternion.Euler(0, 0, -12));
            }
            Part(PrimitiveType.Cylinder, rack, new Vector3(-.45f, .08f, 0), new Vector3(.6f, .08f, .6f), amber);
            reachStock = stock.gameObject; reachStock.SetActive(false);
        }

        /// <summary>The warning's and her rising's look on the reach (on), or the reach as it always is (off): the water stilled and grey, and
        /// every lamp in the Drain Courts and within <see cref="ReachLampRange"/> of the bar guttered out (put back as they were after).</summary>
        public void SetReachWarned(bool on)
        {
            if (reachStill != null) reachStill.SetActive(on);
            if (on == DrainLampsOut) return;
            DrainLampsOut = on;
            if (on)
            {
                var bar = SiltBar; var courts = Zone.terraces == null ? null : System.Array.Find(Zone.terraces, t => t != null && t.name == "The Drain Courts");
                foreach (var l in NightLights)
                {
                    if (l == null || l.light == null) continue;
                    var p = l.light.transform.position; bool inCourts = false;
                    if (courts != null) { TerraceFrame(courts, p.x, p.z, out float lx, out float lz); inCourts = TerraceOut(courts, lx, lz) <= 0; }
                    bool nearBar = bar != null && Vector2.Distance(new Vector2(p.x, p.z), bar.center) <= ReachLampRange;
                    if (!inCourts && !nearBar) continue;
                    outLamps.Add((l, l.dayIntensity, l.nightIntensity)); l.dayIntensity = l.nightIntensity = 0; l.light.intensity = 0; l.light.enabled = false;
                }
            }
            else
            {
                foreach (var (lamp, day, night) in outLamps) { lamp.dayIntensity = day; lamp.nightIntensity = night; if (lamp.light != null) lamp.light.enabled = true; }
                outLamps.Clear();
            }
        }
        /// <summary>How many lamps are guttered out now.</summary>
        public int DrainLampsGuttered { get { return outLamps.Count; } }
        /// <summary>The salt barrel and the harpoon rack set out on the apron, or taken in.</summary>
        public void SetReachStock(bool on) { if (reachStock != null) reachStock.SetActive(on); }
    }
}
