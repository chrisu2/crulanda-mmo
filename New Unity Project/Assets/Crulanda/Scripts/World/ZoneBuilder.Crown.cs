using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Crown of Argentis (Docs/CAPITAL_HUB.md, Phase 9 step 1, 2026-10-15; Chris: "use the map for reference design"): the walled High
    /// City as maps\argentis_city_map.png draws it. The place is CANON (world_bible.md: "The Crown: the highest tier, built of white stone
    /// and gold, housing the Council and the wealthy"); every building's look, name and plan here is GAME-ONLY.
    /// - The ground (<see cref="ZoneTerrace"/>): rectangles levelled over the land, a later one over an earlier: the plateau the High City
    ///   stands on, the terrace of the Crown Square above it, the ramps between them. The edges ease back over a metre or two, a cliff the
    ///   walls stand on. A paved terrace is flagged in white stone (<see cref="BuildPaving"/>) and grows no grass (<see cref="Paved"/>).
    /// - The kinds (<see cref="CityProp"/>): the Council Keep (the castle, its tall gilt spire the central point), the domed Council Hall,
    ///   the many-spired Archive, the civic halls (the Exchange, the Treasury Vault, the Hall of Arms, the Armoury), the four guild halls,
    ///   all walked into (each wall its own collider and navmesh blocker, the floor the levelled ground, a ceiling the camera stops under);
    ///   gilt houses, colonnades, fountains, statues, lamps, banners and gardens; the curtain wall with its square towers and its gates;
    ///   and the backdrop below the south wall (the Spire wreathed in cloud, the Lowtowns' roofs and smoke) and the sea to the west.
    /// - No machines anywhere (Chris, 2026-10-11): the climb is ramps and stairs, the gates are doors on hinges and a bar.
    /// - The Council's device, a gold crown on a white field, is GAME-ONLY.
    /// Each kind draws only from a stream of its own keyed on where it stands, so nothing else in the zone moves for it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        // ---------- terraces ----------
        /// <summary>A point in a terrace's own frame (x across, z along, from its centre).</summary>
        static void TerraceFrame(ZoneTerrace t, float x, float z, out float lx, out float lz)
        {
            float r = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r), dx = x - t.center.x, dz = z - t.center.y;
            lx = dx * c - dz * s; lz = dx * s + dz * c;
        }
        /// <summary>A terrace's level at a distance along it: its height, or on a ramp from height at its -z end to heightTo at its +z end.</summary>
        public static float TerraceLevel(ZoneTerrace t, float lz)
        {
            if (!t.ramp || t.size.y <= 0) return t.height;
            return Mathf.Lerp(t.height, t.heightTo, Mathf.Clamp01((lz + t.size.y / 2) / t.size.y));
        }
        /// <summary>How far a point (in the terrace's frame) lies outside its rectangle; 0 inside.</summary>
        static float TerraceOut(ZoneTerrace t, float lx, float lz)
        {
            float ox = Mathf.Abs(lx) - t.size.x / 2, oz = Mathf.Abs(lz) - t.size.y / 2;
            return ox > 0 || oz > 0 ? new Vector2(Mathf.Max(0, ox), Mathf.Max(0, oz)).magnitude : 0;
        }
        /// <summary>The land under the terraces: each levels it in its rectangle and eases back over its blend outside it.</summary>
        float Terraces(float x, float z, float h)
        {
            foreach (var t in Zone.terraces)
            {
                if (t == null) continue;
                float blend = Mathf.Max(.25f, t.blend), reach = (t.size.x + t.size.y) / 2 + blend;
                if (Mathf.Abs(x - t.center.x) > reach || Mathf.Abs(z - t.center.y) > reach) continue;   // most of the zone: a cheap reject
                TerraceFrame(t, x, z, out float lx, out float lz);
                float d = TerraceOut(t, lx, lz); if (d >= blend) continue;
                h = Mathf.Lerp(h, TerraceLevel(t, lz), 1 - Mathf.SmoothStep(0, 1, d / blend));
            }
            return h;
        }
        /// <summary>Whether a point lies on a paved terrace (a city's streets and squares): no grass, flowers or plants grow there.</summary>
        bool Paved(Vector2 p)
        {
            if (Zone.terraces == null) return false;
            foreach (var t in Zone.terraces)
            {
                if (t == null || !t.paved) continue;
                TerraceFrame(t, p.x, p.y, out float lx, out float lz);
                if (TerraceOut(t, lx, lz) <= .5f) return true;
            }
            return false;
        }
        Material PavingStone { get { return Dressed(new Color(.8f, .78f, .73f)); } }
        Material WayStone { get { return Dressed(new Color(.88f, .82f, .68f)); } }   // the Processional Way: warmer, a little gold in it
        /// <summary>
        /// The flags of every paved terrace, a little over the ground, in cells of about 2.5 m where that terrace is the ground (a cell
        /// under a higher terrace or on an edge's slope is left out, so two never lie in one place); and in a city, the roads as paved
        /// ribbons following the ground (the Processional Way, the Gilt Approach). No colliders: the ground under them is walked.
        /// </summary>
        void BuildPaving()
        {
            if (Zone.terraces == null || Zone.terraces.Length == 0) return;
            var root = new GameObject("Paving").transform; root.SetParent(statics, false);
            foreach (var t in Zone.terraces)
            {
                if (t == null || !t.paved) continue;
                var mesh = PavingMesh(t); if (mesh == null) continue;
                MeshPart(mesh, root, Vector3.zero, PavingStone).name = "Paving " + t.name;
            }
            if (!Zone.city) return;
            foreach (var r in Zone.roads)
            {
                if (r == null || r.points == null || r.points.Length < 2) continue;
                var pts = Densify(r.points, 1.25f);
                MeshPart(ZoneMeshes.Ribbon(pts, r.width, q => HeightAt(q.x, q.y) + .07f), root, Vector3.zero, WayStone).name = "Paving " + r.name;
            }
        }
        Mesh PavingMesh(ZoneTerrace t)
        {
            const float cell = 2.5f;
            int nx = Mathf.Max(1, Mathf.CeilToInt(t.size.x / cell)), nz = Mathf.Max(1, Mathf.CeilToInt(t.size.y / cell));
            float sx = t.size.x / nx, sz = t.size.y / nz, r = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var tri = new List<int>();
            Vector3 World(float lx, float lz, out bool level)
            {
                // Local to world: the terrace's frame turned back (the inverse of TerraceFrame).
                float x = t.center.x + lx * c + lz * s, z = t.center.y - lx * s + lz * c, h = HeightAt(x, z);
                level = Mathf.Abs(h - TerraceLevel(t, lz)) < .06f;
                return new Vector3(x, h + .04f, z);
            }
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < nz; k++)
                {
                    float x0 = -t.size.x / 2 + i * sx, z0 = -t.size.y / 2 + k * sz;
                    var a = World(x0, z0, out bool la); var b = World(x0, z0 + sz, out bool lb); var cc = World(x0 + sx, z0 + sz, out bool lc); var d = World(x0 + sx, z0, out bool ld);
                    if (!(la && lb && lc && ld)) continue;
                    int at = v.Count; v.Add(a); v.Add(b); v.Add(cc); v.Add(d);
                    foreach (var p in new[] { a, b, cc, d }) { uv.Add(new Vector2(p.x, p.z) / 2.2f); n.Add(Vector3.up); }
                    // Wound like ZoneMeshes.Box's top face, so it faces up whatever the terrace's turn.
                    if (Vector3.Cross(b - a, cc - a).y >= 0) tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
                    else tri.AddRange(new[] { at, at + 2, at + 1, at, at + 3, at + 2 });
                }
            if (v.Count == 0) return null;
            var m = new Mesh { name = "Paving", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
        }

        // ---------- the kinds ----------
        static readonly string[] CityWalkIns = { "council_keep", "dome_hall", "archive", "mansion", "guild_hall" };
        /// <summary>A city's buildings you walk into (with the moving props: their parts fade one by one where they hide you, RoofFade).</summary>
        static bool WalksInCity(string kind) { return Array.IndexOf(CityWalkIns, kind) >= 0; }
        /// <summary>A city building's size across its walls (x) and front to back (y): the data's, else the kind's own.</summary>
        public static Vector2 CitySize(ZoneProp p)
        {
            if (p.size.x > 0 && p.size.y > 0) return p.size;
            switch (p.kind)
            {
                case "council_keep": return new Vector2(30, 24);
                case "dome_hall": return new Vector2(24, 24);
                case "archive": return new Vector2(26, 20);
                case "mansion": return p.variant == 2 ? new Vector2(26, 12) : p.variant == 3 ? new Vector2(12, 10) : p.variant == 1 ? new Vector2(16, 16) : new Vector2(20, 16);
                case "guild_hall": return new Vector2(14, 11);
                case "gilt_house": return new Vector2(9, 8);
                default: return Vector2.zero;
            }
        }
        /// <summary>Half a city building's footprint, eaves included (InBuilding: no rain, no node or station inside); zero for anything else.</summary>
        Vector2 CityFootprint(ZoneProp p)
        {
            switch (p.kind)
            {
                case "council_keep": case "dome_hall": case "archive": case "mansion": case "guild_hall": case "gilt_house": return CitySize(p) / 2 + Vector2.one * .8f;
                default: return Vector2.zero;
            }
        }
        /// <summary>Builds a city's prop kind; false for a kind that is none of them (BuildProps then warns of it).</summary>
        bool CityProp(Transform t, ZoneProp p)
        {
            var size = CitySize(p);
            switch (p.kind)
            {
                case "council_keep": CouncilKeep(t, size); return true;
                case "dome_hall": DomeHall(t, size); return true;
                case "archive": Archive(t, size); return true;
                case "mansion": Mansion(t, size, p.variant); return true;
                case "guild_hall": GuildHall(t, size, p.variant); return true;
                case "gilt_house": GiltHouse(t, size, p.variant); return true;
                case "colonnade": Colonnade(t, p.size.x > 0 ? p.size.x : 24, p.size.y > 0 ? p.size.y : 4); return true;
                case "fountain": Fountain(t, p.variant); return true;
                case "statue": Statue(t, p.variant); return true;
                case "city_lamp": CityLamp(t); return true;
                case "council_banner": CouncilBannerPole(t, p.size.y > 0 ? p.size.y : 7); return true;
                case "parterre": Parterre(t, p.size.x > 0 ? p.size : new Vector2(12, 8)); return true;
                case "stair": Stair(t, p.size.x > 0 ? p.size : new Vector2(10, 20)); return true;
                case "city_wall": CityWall(t, p.points, p.variant, p.size.x > 0 ? p.size.x : 40); return true;
                case "city_gate": CityGate(t, p.size.x > 0 ? p.size.x : 6, p.variant == 1); return true;
                case "spire": TheSpire(t, p.size.y > 0 ? p.size.y : 95); return true;
                case "lowtowns": Lowtowns(t, p.size.x > 0 ? p.size : new Vector2(200, 80)); return true;
                case "sea": Sea(t, p.size.x > 0 ? p.size : new Vector2(40, 200)); return true;
                case "spot": return true;   // a named place and nothing to see (where a guild's members stand in their hall: "hall.lantern-watch")
                default: return false;
            }
        }
        /// <summary>A stream of its own for a prop, keyed on where it stands (nothing else in the zone draws differently for it).</summary>
        System.Random CityRandom(Transform t, int salt) { return new System.Random(Zone.seed ^ (Mathf.RoundToInt(t.position.x * 8) * 73856093) ^ (Mathf.RoundToInt(t.position.z * 8) * 19349663) ^ salt); }

        // ---------- the city's stuff ----------
        Material CityWhite { get { return Dressed(new Color(.83f, .81f, .76f)); } }
        Material CityShade { get { return Dressed(new Color(.68f, .66f, .62f)); } }    // plinths and the curtain wall's battered foot
        Material Marble { get { return Tint(art.stone, new Color(.93f, .91f, .87f)); } }
        Material GiltRoof { get { return Tint(art.slate, new Color(1f, .8f, .34f)); } }  // gilt tiles
        Material Gilt { get { return Tint(art.metal, new Color(1f, .8f, .36f)); } }      // finials, bands, the crown device
        Material BlueSlate { get { return Tint(art.slate, new Color(.52f, .58f, .68f)); } }
        Material CouncilCloth { get { return Tint(art.cloth, new Color(.96f, .95f, .9f)); } }
        Material CouncilGold { get { return Tint(art.cloth, new Color(.92f, .7f, .2f)); } }
        Material DarkOak { get { return Tint(art.timber, new Color(.26f, .17f, .1f)); } }
        /// <summary>The four guilds' colours, in SimGuilds' order (GAME-ONLY): the Lantern Watch's night blue and lantern gold, Oak and
        /// Ember's green and ember, the Long Road's russet and road white, the Scree Hounds' slate grey and red.</summary>
        static readonly Color[][] GuildColours = {
            new[] { new Color(.1f, .16f, .42f), new Color(.95f, .76f, .26f) }, new[] { new Color(.14f, .36f, .16f), new Color(.92f, .45f, .12f) },
            new[] { new Color(.5f, .24f, .12f), new Color(.92f, .9f, .82f) }, new[] { new Color(.32f, .33f, .36f), new Color(.72f, .1f, .08f) } };

        // ---------- the Modular Castle Kit ----------
        /// <summary>
        /// Quaternius's Modular Castle Kit (CC0; Resources/Props/Castle/ModularCastle.fbx, brought in 2026-10-15 for the castle): one file of
        /// eighteen pieces laid side by side, each a node turned -90 about x (Blender, Z up) and scaled 100, in flat colours (LightRock,
        /// DarkRock, Black, Celing). Read from the file (2026-10-15): Cube.005 the plain tall wall (1.52 long, 2.37 tall, .42 thick, its
        /// battlements on it); TallWallEntrance.002 the tall wall with an arched opening (the opening 58% of its width, its arch's crown
        /// about 39% of its height); LargeSimpleTower.002 the square tower under its cap (.8 across, 3.7 tall); Cube.042 the large square
        /// tower (1.3 across, 4.2 tall). A piece is taken by its node's name, set in a holder of our own and stretched to a box
        /// (<see cref="CastlePiece"/>); LightRock is tinted the city's white stone, DarkRock its shaded stone, Celing (the caps) gilt.
        /// </summary>
        static GameObject castleKit; static bool castleTried;
        static Transform CastleSource(string piece)
        {
            if (!castleTried) { castleTried = true; castleKit = Resources.Load<GameObject>("Props/Castle/ModularCastle"); }
            if (castleKit == null) return null;
            foreach (var tr in castleKit.GetComponentsInChildren<Transform>(true)) if (tr.name == piece && tr.GetComponent<MeshFilter>() != null) return tr;
            return null;
        }
        /// <summary>Whether the castle kit is there (the tests' bare scenes may lack it; every piece then has its painted stand-in).</summary>
        static bool HasCastleKit { get { return CastleSource("Cube.005") != null; } }
        Material CastleTint(Material m)
        {
            if (m == null) return CityWhite; string n = m.name;
            if (n.Contains("LightRock")) return Tint(m, new Color(.9f, .88f, .83f));
            if (n.Contains("DarkRock")) return Tint(m, new Color(.72f, .7f, .65f));
            if (n.Contains("Celing")) return Tint(m, new Color(1f, .8f, .34f));
            if (n.Contains("Black")) return Tint(m, new Color(.14f, .15f, .2f));
            return m;
        }
        /// <summary>
        /// A castle kit piece under <paramref name="parent"/>, stretched so its bounds are <paramref name="box"/> (x along it, y up, z through
        /// it), the middle of its foot at <paramref name="foot"/> (parent-local), turned <paramref name="rot"/>. Only its meshes come along (the
        /// importer's colliders go: the builder gives the solid parts their own). Null when the kit or the piece is missing.
        /// </summary>
        GameObject CastlePiece(Transform parent, string piece, Vector3 foot, Quaternion rot, Vector3 box)
        {
            var src = CastleSource(piece); if (src == null) return null;
            var holder = new GameObject("Castle " + piece).transform; holder.SetParent(parent, false);
            var inst = Instantiate(src.gameObject, holder, false); inst.name = piece; inst.transform.localPosition = Vector3.zero;   // its node's own turn and scale kept
            foreach (var c in inst.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            var b = PropBounds(holder); if (b.size.x < .001f || b.size.y < .001f || b.size.z < .001f) return holder.gameObject;
            var k = new Vector3(box.x / b.size.x, box.y / b.size.y, box.z / b.size.z);
            holder.localScale = k; holder.localRotation = rot;
            holder.localPosition = foot + rot * new Vector3(-b.center.x * k.x, -b.min.y * k.y, -b.center.z * k.z);
            foreach (var r in holder.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = CastleTint(mats[i]); r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            }
            return holder.gameObject;
        }

        /// <summary>A block of stone that stops you and is cut out of the navmesh.</summary>
        GameObject SolidBox(Transform t, Vector3 at, Vector3 size, Material m, Quaternion? rot = null)
        {
            var o = BoxPart(t, at, size, m, rot); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
        }
        /// <summary>A collider with nothing to see, cut out of the navmesh (a run of wall whose stone is joined into one mesh).</summary>
        void Blocker(Transform t, Vector3 at, Vector3 size, Quaternion? rot = null)
        {
            var o = new GameObject("Blocker"); o.transform.SetParent(t, false); o.transform.localPosition = at; if (rot.HasValue) o.transform.localRotation = rot.Value;
            o.AddComponent<BoxCollider>().size = size; o.AddComponent<NavBlocker>();
        }
        /// <summary>A part made solid (a column, a counter): a box collider round its mesh, cut out of the navmesh.</summary>
        GameObject Solidify(GameObject o) { if (o != null) { o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); } return o; }
        /// <summary>Blocks joined into one mesh of 32-bit indices (a long wall's stone is more than 65k vertices' worth).</summary>
        GameObject Joined32(string name, Transform parent, Material m, List<CombineInstance> blocks)
        {
            if (blocks.Count == 0) return null;
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(blocks.ToArray(), true); mesh.RecalculateBounds();
            foreach (var b in blocks) DestroyImmediate(b.mesh);
            return MeshPart(mesh, parent, Vector3.zero, m);
        }
        /// <summary>The Council's device (GAME-ONLY): a gold crown, a band and three points, flat on a face; its front looks along yaw's -Z.</summary>
        void CrownDevice(Transform t, Vector3 at, float yaw, float size, Material gold)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cube, t, at + q * new Vector3(0, -size * .2f, 0), new Vector3(size, size * .22f, .05f), gold, q);
            for (int k = -1; k <= 1; k++)
                Part(PrimitiveType.Cube, t, at + q * new Vector3(k * size * .38f, size * (k == 0 ? .14f : .06f), 0), new Vector3(size * .24f, size * .24f, .05f), gold, q * Quaternion.Euler(0, 0, 45));
            Part(PrimitiveType.Sphere, t, at + q * new Vector3(0, size * .36f, -.02f), Vector3.one * size * .12f, gold);
        }
        /// <summary>A Council banner hung from a gilt rod (white field, gold crown and hem; GAME-ONLY), its top at <paramref name="top"/>,
        /// its face looking along yaw's -Z.</summary>
        void CouncilBanner(Transform t, Vector3 top, float yaw, float w, float h)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cylinder, t, top, new Vector3(.08f, w / 2 + .2f, .08f), Gilt, q * Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, top + q * new Vector3(0, -h / 2 - .05f, 0), new Vector3(w, h, .04f), CouncilCloth, q);
            Part(PrimitiveType.Cube, t, top + q * new Vector3(0, -h - .02f, -.01f), new Vector3(w, .16f, .05f), CouncilGold, q);   // the hem
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, top + q * new Vector3(s * (w / 2 - .06f), -h / 2 - .05f, -.01f), new Vector3(.08f, h, .05f), CouncilGold, q);
            CrownDevice(t, top + q * new Vector3(0, -h * .32f, -.04f), yaw, w * .55f, Gilt);
        }
        /// <summary>A guild's banner (GAME-ONLY): its field, a band of its second colour, and its sign: the Lantern Watch a lantern, Oak and
        /// Ember an oak leaf over an ember, the Long Road a road running up it, the Scree Hounds a hound's head.</summary>
        void GuildBanner(Transform t, Vector3 top, float yaw, float w, float h, int guild)
        {
            var q = Quaternion.Euler(0, yaw, 0); var c = GuildColours[Mathf.Abs(guild) % GuildColours.Length];
            Material field = Tint(art.cloth, c[0]), mark = Tint(art.cloth, c[1]);
            Part(PrimitiveType.Cylinder, t, top, new Vector3(.07f, w / 2 + .15f, .07f), Gilt, q * Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, top + q * new Vector3(0, -h / 2 - .05f, 0), new Vector3(w, h, .04f), field, q);
            Part(PrimitiveType.Cube, t, top + q * new Vector3(0, -h * .12f, -.01f), new Vector3(w, h * .07f, .05f), mark, q);
            var sign = top + q * new Vector3(0, -h * .55f, -.04f);
            switch (Mathf.Abs(guild) % 4)
            {
                case 0: Part(PrimitiveType.Cube, t, sign, new Vector3(w * .28f, w * .36f, .05f), mark, q); Part(PrimitiveType.Cube, t, sign + q * new Vector3(0, 0, -.02f), new Vector3(w * .14f, w * .2f, .05f), art.glass, q); break;   // a lantern, lit
                case 1: Part(PrimitiveType.Sphere, t, sign + q * new Vector3(0, w * .12f, 0), new Vector3(w * .42f, w * .3f, .05f), Tint(art.foliage, new Color(.3f, .6f, .22f)), q); Part(PrimitiveType.Sphere, t, sign - q * new Vector3(0, w * .2f, 0), new Vector3(w * .2f, w * .2f, .05f), mark, q); break;   // a leaf over an ember
                case 2: Part(PrimitiveType.Cube, t, sign, new Vector3(w * .16f, h * .5f, .05f), mark, q * Quaternion.Euler(0, 0, 12)); break;   // a road
                default: Part(PrimitiveType.Sphere, t, sign, new Vector3(w * .34f, w * .28f, .05f), mark, q); Part(PrimitiveType.Cube, t, sign + q * new Vector3(w * .18f, w * .08f, 0), new Vector3(w * .2f, w * .1f, .05f), mark, q * Quaternion.Euler(0, 0, 30)); break;   // a hound's head
            }
        }
        /// <summary>Gilt letters over a door: the building's name, both its faces' worth on the front only.</summary>
        void Lettering(Transform t, string text, Vector3 at, float width)
        {
            if (string.IsNullOrEmpty(text)) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (font == null) return;
            var tm = new GameObject("Lettering").AddComponent<TextMesh>(); tm.transform.SetParent(t, false); tm.transform.localPosition = at;
            tm.text = text.ToUpperInvariant(); tm.font = font; tm.fontSize = 64; tm.characterSize = Mathf.Min(.06f, width / Mathf.Max(6, text.Length) * .26f); tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.62f, .45f, .12f);
            var r = tm.GetComponent<MeshRenderer>(); r.sharedMaterial = SignTextMaterial(font); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        /// <summary>A gilt spire on a point (a cone of gilt tiles, a ball and a pin at its tip).</summary>
        void GiltSpire(Transform t, Vector3 foot, float radius, float height, int sides = 8)
        {
            MeshPart(PropMesh("City spire " + Mathf.RoundToInt(radius * 100) + "x" + Mathf.RoundToInt(height * 100) + "x" + sides, () => ZoneMeshes.Spire(radius, height, sides, 2)), t, foot, GiltRoof, Quaternion.Euler(0, sides == 4 ? 45 : 0, 0));
            Part(PrimitiveType.Sphere, t, foot + Vector3.up * (height + radius * .08f), Vector3.one * Mathf.Max(.25f, radius * .14f), Gilt);
            Part(PrimitiveType.Cylinder, t, foot + Vector3.up * (height + radius * .25f + .5f), new Vector3(.08f, .6f, .08f), Gilt);
        }
        /// <summary>A pinnacle: a white shaft with a gilt spire on it (the Archive's many spires; the keep's crown of them).</summary>
        void Pinnacle(Transform t, Vector3 foot, float shaft, float width, float spire)
        {
            BoxPart(t, foot + Vector3.up * shaft / 2, new Vector3(width, shaft, width), CityWhite, null, 1.5f);
            Part(PrimitiveType.Cube, t, foot + Vector3.up * (shaft - .2f), new Vector3(width + .16f, .2f, width + .16f), Gilt);
            GiltSpire(t, foot + Vector3.up * shaft, width * .72f, spire, 4);
        }
        /// <summary>Merlons along a line (local a to b) at height y: stone blocks with gaps, into a list to be joined.</summary>
        static void Merlons(List<CombineInstance> into, Vector3 a, Vector3 b, float y, float size = .8f, float step = 1.7f)
        {
            var d = b - a; d.y = 0; float len = d.magnitude; if (len < .5f) return;
            int n = Mathf.Max(1, Mathf.FloorToInt(len / step)); float yaw = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++) { var p = Vector3.Lerp(a, b, (i + .5f) / n); p.y = y + size * .65f; into.Add(Ashlar(p, new Vector3(size, size * 1.3f, size * .8f), 1.75f, Quaternion.Euler(0, yaw, 0))); }
        }
        /// <summary>A ring of merlons round a square top (half-width hw, at height y), joined into one stonework.</summary>
        void BattlementRing(Transform t, Vector3 centre, float hw, float y, Material m)
        {
            var list = new List<CombineInstance>();
            Merlons(list, centre + new Vector3(-hw, 0, -hw), centre + new Vector3(hw, 0, -hw), y); Merlons(list, centre + new Vector3(hw, 0, -hw), centre + new Vector3(hw, 0, hw), y);
            Merlons(list, centre + new Vector3(hw, 0, hw), centre + new Vector3(-hw, 0, hw), y); Merlons(list, centre + new Vector3(-hw, 0, hw), centre + new Vector3(-hw, 0, -hw), y);
            Joined32("Battlements", t, m, list);
        }
        /// <summary>A tall window (glass that glows at night, through the wall) in a gilt frame, on the face looking along yaw's -Z.</summary>
        void CityWindow(Transform t, Vector3 at, float yaw, float w, float h, float wall)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cube, t, at, new Vector3(w, h, wall + .1f), art.glass, q);
            Part(PrimitiveType.Cube, t, at + q * new Vector3(0, h / 2 + .1f, -wall / 2 - .06f), new Vector3(w + .3f, .2f, .12f), Gilt, q);
            Part(PrimitiveType.Cube, t, at + q * new Vector3(0, -h / 2 - .08f, -wall / 2 - .08f), new Vector3(w + .4f, .16f, .2f), CityWhite, q);   // the sill
            Part(PrimitiveType.Cube, t, at + q * new Vector3(0, 0, -wall / 2 - .04f), new Vector3(.08f, h, .06f), Gilt, q);   // a mullion
        }
        /// <summary>A bookcase against a wall, solid; its open front looks along yaw's -Z: a dark oak case (back, sides, five shelves, a cornice)
        /// full of books (2026-10-16, Chris in the Archive: "looks like tables above table, but should be bookshelves": the KayKit wall shelf,
        /// stretched to 2.4 m, read as benches stacked in rows).</summary>
        void BookShelf(Transform t, Vector3 foot, float yaw, System.Random r)
        {
            var q = Quaternion.Euler(0, yaw, 0); const float W = 2.2f, H = 2.6f, D = .5f;
            Solidify(BoxPart(t, foot + q * new Vector3(0, H / 2, .22f), new Vector3(W, H, .06f), DarkOak, q));   // the back
            foreach (int s in new[] { -1, 1 }) BoxPart(t, foot + q * new Vector3(s * (W / 2 - .04f), H / 2, 0), new Vector3(.08f, H, D), DarkOak, q);
            for (int row = 0; row < 5; row++) BoxPart(t, foot + q * new Vector3(0, .08f + row * .58f, 0), new Vector3(W - .1f, .05f, D), DarkOak, q);
            BoxPart(t, foot + q * new Vector3(0, H + .06f, -.03f), new Vector3(W + .12f, .12f, D + .1f), DarkOak, q);   // the cornice
            Part(PrimitiveType.Cube, t, foot + q * new Vector3(0, H - .02f, -D / 2 - .02f), new Vector3(W, .04f, .02f), Gilt, q);
            for (int row = 0; row < 4; row++)
            {
                float x = -W / 2 + .14f;
                while (x < W / 2 - .2f)
                {
                    float bw = .05f + (float)r.NextDouble() * .05f, bh = .34f + (float)r.NextDouble() * .14f;
                    if (r.NextDouble() < .08) { x += .18f; continue; }   // a gap where a book is out
                    var c = Color.HSVToRGB((float)r.NextDouble(), .4f + (float)r.NextDouble() * .2f, .25f + (float)r.NextDouble() * .3f);
                    float lean = r.NextDouble() < .1 ? 8 : 0;
                    Part(PrimitiveType.Cube, t, foot + q * new Vector3(x + bw / 2, .105f + row * .58f + bh / 2, -.02f), new Vector3(bw, bh, .3f), Tint(art.cloth, c), q * Quaternion.Euler(0, 0, lean));
                    x += bw + .01f;
                }
            }
        }
        /// <summary>A table of dark oak (top, four legs), solid; long along x of yaw's frame.</summary>
        void CityTable(Transform t, Vector3 foot, float yaw, float len, float wide)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cube, t, foot + Vector3.up * .78f, new Vector3(len, .08f, wide), DarkOak, q);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, foot + q * new Vector3(sx * (len / 2 - .15f), .38f, sz * (wide / 2 - .12f)), new Vector3(.1f, .76f, .1f), DarkOak, q);
            Blocker(t, foot + Vector3.up * .4f, new Vector3(len, .8f, wide), q);
        }
        /// <summary>A bench of dark oak, no collider (it is stepped round, not walked into).</summary>
        void CityBench(Transform t, Vector3 foot, float yaw, float len)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cube, t, foot + Vector3.up * .45f, new Vector3(len, .07f, .42f), DarkOak, q);
            Part(PrimitiveType.Cube, t, foot + q * new Vector3(0, .8f, .2f), new Vector3(len, .55f, .06f), DarkOak, q);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, foot + q * new Vector3(s * (len / 2 - .1f), .22f, 0), new Vector3(.08f, .44f, .4f), DarkOak, q);
        }
        void HallLight(Transform t, Vector3 at, float range, float intensity)
        {
            var l = new GameObject("Hall light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = at;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = new Color(1, .85f, .62f);
        }
        /// <summary>
        /// The shell of a hall you walk into (the inn's way: built at ground level, no plinth under the floor): a marble floor, four walls
        /// <paramref name="wallH"/> high each its own collider and navmesh blocker, the front (-Z) open in a doorway
        /// <paramref name="doorW"/> wide under a lintel at <paramref name="doorH"/> (its jambs' colliders .1 m short of the opening, as the
        /// inn's: the navmesh needs the room), a ceiling at <paramref name="H"/> the camera stops under, a gilt door frame, windows down the
        /// sides and the back, and the light inside.
        /// </summary>
        void HallShell(Transform t, float w, float d, float H, float wallH, float doorW, float doorH, Material wall)
        {
            const float th = .5f;
            Part(PrimitiveType.Cube, t, new Vector3(0, .025f, 0), new Vector3(w - th, .05f, d - th), Marble);
            GameObject Piece(Vector3 at, Vector3 size) { var o = BoxPart(t, at, size, wall); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o; }
            Piece(new Vector3(0, wallH / 2, d / 2), new Vector3(w, wallH, th));
            foreach (int s in new[] { -1, 1 }) Piece(new Vector3(s * w / 2, wallH / 2, 0), new Vector3(th, wallH, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                var jamb = Piece(new Vector3(s * (doorW / 2 + side / 2), wallH / 2, -d / 2), new Vector3(side, wallH, th)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(.1f, 0, 0); jamb.center += new Vector3(s * .05f, 0, 0);
            }
            Piece(new Vector3(0, doorH + (wallH - doorH) / 2, -d / 2), new Vector3(doorW, wallH - doorH, th));
            // The ceiling: solid, so the third-person camera pulls in under it instead of poking through (the inn's).
            Part(PrimitiveType.Cube, t, new Vector3(0, H + .1f, 0), new Vector3(w - th, .2f, d - th), Tint(art.plaster, new Color(.92f, .9f, .84f))).AddComponent<BoxCollider>();
            // A plinth course and a cornice outside, a gilt band under the cornice.
            // The front's plinth stops either side of the doorway (2026-10-16, Chris in the Armoury: "floor is blocked": a 0.6 m course ran
            // across every hall's door).
            foreach (int s in new[] { -1, 1 }) { float span = (w + .3f - doorW) / 2; Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + span / 2), .3f, -(d / 2 + .12f)), new Vector3(span, .6f, .25f), CityShade).name = "Plinth"; }
            foreach (int sz in new[] { -1, 1 }) { if (sz > 0) Part(PrimitiveType.Cube, t, new Vector3(0, .3f, sz * (d / 2 + .12f)), new Vector3(w + .3f, .6f, .25f), CityShade).name = "Plinth"; Part(PrimitiveType.Cube, t, new Vector3(0, wallH - .25f, sz * (d / 2 + .15f)), new Vector3(w + .5f, .5f, .3f), CityWhite); Part(PrimitiveType.Cube, t, new Vector3(0, wallH - .62f, sz * (d / 2 + .06f)), new Vector3(w + .1f, .16f, .14f), Gilt); }
            foreach (int sx in new[] { -1, 1 }) { Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .12f), .3f, 0), new Vector3(.25f, .6f, d + .3f), CityShade).name = "Plinth"; Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .15f), wallH - .25f, 0), new Vector3(.3f, .5f, d + .5f), CityWhite); Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .06f), wallH - .62f, 0), new Vector3(.14f, .16f, d + .1f), Gilt); }
            // The door's gilt frame, outside.
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + .15f), doorH / 2, -d / 2 - .3f), new Vector3(.3f, doorH, .14f), Gilt);
            Part(PrimitiveType.Cube, t, new Vector3(0, doorH + .18f, -d / 2 - .3f), new Vector3(doorW + .6f, .36f, .14f), Gilt);
            // Windows: tall ones in the sides and the back between the corners, glowing through the wall.
            float wy = Mathf.Min(H * .55f, 3.2f), wh = Mathf.Min(H * .45f, 3.4f);
            for (float x = -w / 2 + 2.6f; x <= w / 2 - 2.6f; x += 3.4f) CityWindow(t, new Vector3(x, wy, d / 2), 180, 1.1f, wh, th);
            foreach (int s in new[] { -1, 1 }) for (float z = -d / 2 + 2.8f; z <= d / 2 - 2.6f; z += 3.4f) CityWindow(t, new Vector3(s * w / 2, wy, z), s < 0 ? 90 : 270, 1.1f, wh, th);
            foreach (int s in new[] { -1, 1 }) for (float x = doorW / 2 + 2.2f; x <= w / 2 - 2f; x += 3.4f) CityWindow(t, new Vector3(s * x, wy, -d / 2), 0, 1.1f, wh, th);
            HallLight(t, new Vector3(0, H - 1.2f, 0), Mathf.Max(w, d) * .75f, 1.3f);
            if (w * d > 300) foreach (int s in new[] { -1, 1 }) HallLight(t, new Vector3(0, H - 1.2f, s * d / 4), Mathf.Max(w, d) * .5f, .8f);
        }
        /// <summary>A gable roof of blue-grey slate (or gilt tiles) over a hall, ridge along x, closed at its ends, a gilt ridge.</summary>
        void HallRoof(Transform t, float w, float d, float wallH, float roofH, Material roofMat)
        {
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1, roofH, .3f), t, new Vector3(0, wallH, 0), roofMat);
            Part(PrimitiveType.Cube, t, new Vector3(0, wallH + roofH + .05f, 0), new Vector3(w + 1.1f, .16f, .2f), Gilt);
        }
        /// <summary>A portico before a hall's door: columns, an entablature with the hall's name in gilt and a pediment with the crown.</summary>
        void Portico(Transform t, float d, float wide, float high, string name)
        {
            float z = -d / 2 - 2.2f; int pairs = wide >= 13 ? 2 : 1;
            for (int k = 0; k < pairs; k++) foreach (int s in new[] { -1, 1 })
            {
                float x = s * (2.4f + k * 3.6f);
                Solidify(Part(PrimitiveType.Cylinder, t, new Vector3(x, high / 2, z), new Vector3(.62f, high / 2, .62f), Marble));
                Part(PrimitiveType.Cube, t, new Vector3(x, .2f, z), new Vector3(.95f, .4f, .95f), CityShade); Part(PrimitiveType.Cube, t, new Vector3(x, high - .15f, z), new Vector3(.9f, .3f, .9f), Gilt);
            }
            float span = pairs == 2 ? 13.6f : 6.4f;
            BoxPart(t, new Vector3(0, high + .55f, z + .9f), new Vector3(span, 1.1f, 2.6f), CityWhite);
            MeshPart(ZoneMeshes.Gable(span, 2.2f, .5f), t, new Vector3(0, high + 1.1f, z - .2f), CityWhite);
            MeshPart(ZoneMeshes.GableRoof(span + .4f, 2.8f, 0.01f, .2f), t, new Vector3(0, high + 1.1f, z + .9f), BlueSlate);
            CrownDevice(t, new Vector3(0, high + 1.75f, z - .5f), 0, 1.1f, Gilt);
            Lettering(t, name, new Vector3(0, high + .55f, z - .45f), span * .85f);
        }

        // ---------- the Council Keep ----------
        /// <summary>
        /// The Council Keep (GAME-ONLY name; the Council in the Crown is CANON): the castle at the north-centre, the city's central point. A
        /// white block four towers at its corners, crenellated, gilt pyramids on them; from its back rises the great tower to 46 m with a
        /// pinnacle at each corner and the tall gilt spire, its tip about 73 m up. Inside, the audience hall: columns down both sides, a
        /// white and gold runner to a dais, the throne of white stone and gilt at the back (where High Councilor Aethelgard stands before
        /// it, ZoneLife), Council banners on the walls and chandeliers. Door to the south (-Z).
        /// </summary>
        void CouncilKeep(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 11, wallH = 17; var r = CityRandom(t, 0x6b65);
            HallShell(t, w, d, H, wallH, 4, 6, CityWhite);
            Part(PrimitiveType.Cube, t, new Vector3(0, wallH + .2f, 0), new Vector3(w, .4f, d), BlueSlate);   // the leads
            BattlementRing(t, Vector3.zero, w / 2 - .3f, wallH, CityWhite);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var c = new Vector3(sx * w / 2, 0, sz * d / 2);
                Blocker(t, c + Vector3.up * 13, new Vector3(6, 26, 6));
                // The castle kit's square tower under its cap (gilt), else the painted one: a white shaft, battlements, a gilt pyramid.
                if (CastlePiece(t, "LargeSimpleTower.002", c, Quaternion.identity, new Vector3(6.4f, 31, 6.4f)) == null)
                {
                    BoxPart(t, c + Vector3.up * 13, new Vector3(6, 26, 6), CityWhite);
                    Part(PrimitiveType.Cube, t, c + Vector3.up * 20, new Vector3(6.2f, .3f, 6.2f), Gilt);
                    BattlementRing(t, c, 2.7f, 26, CityWhite);
                    GiltSpire(t, c + Vector3.up * 26.4f, 3.6f, 7, 4);
                    CityWindow(t, c + new Vector3(0, 21.5f, sz * 3), sz < 0 ? 0 : 180, .8f, 2.2f, .3f);
                }
            }
            // The great tower (the kit's large square tower, else painted) and its spire, the tallest thing in the Crown.
            var gt = new Vector3(0, 0, 3);
            if (CastlePiece(t, "Cube.042", gt + Vector3.up * (wallH - .5f), Quaternion.identity, new Vector3(10.4f, 47 - wallH, 10.4f)) == null)
            {
                BoxPart(t, gt + Vector3.up * ((wallH + 46) / 2), new Vector3(10, 46 - wallH, 10), CityWhite, null, 1.75f);
                foreach (float y in new[] { 26f, 36f }) Part(PrimitiveType.Cube, t, gt + Vector3.up * y, new Vector3(10.2f, .35f, 10.2f), Gilt);
                for (int f = 0; f < 4; f++)
                {
                    float yaw = f * 90; var q = Quaternion.Euler(0, yaw, 0);
                    CityWindow(t, gt + q * new Vector3(0, 31, -5), yaw, 1.4f, 4, .3f); CityWindow(t, gt + q * new Vector3(0, 41, -5), yaw, 1.4f, 3, .3f);
                }
                BattlementRing(t, gt, 4.7f, 46, CityWhite);
            }
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Pinnacle(t, gt + new Vector3(sx * 4.4f, 46, sz * 4.4f), 4, 1.2f, 5);
            GiltSpire(t, gt + Vector3.up * 46.5f, 5.6f, 26, 8);
            // The front: the door under a pediment with the crown, banners either side.
            MeshPart(ZoneMeshes.Gable(8, 2.6f, .4f), t, new Vector3(0, 7.2f, -d / 2 - .45f), CityWhite);
            CrownDevice(t, new Vector3(0, 8.2f, -d / 2 - .7f), 0, 1.5f, Gilt);
            Lettering(t, t.name, new Vector3(0, 6.6f, -d / 2 - .4f), 7);
            foreach (int s in new[] { -1, 1 }) CouncilBanner(t, new Vector3(s * 7.5f, 14.5f, -d / 2 - .35f), 0, 2.4f, 6.5f);
            // Inside: columns, the runner, the dais and the throne.
            for (int k = 0; k < 3; k++) foreach (int s in new[] { -1, 1 })
            {
                var at = new Vector3(s * 9, 0, -6 + k * 5);
                Solidify(Part(PrimitiveType.Cylinder, t, at + Vector3.up * H / 2, new Vector3(1.1f, H / 2, 1.1f), Marble));
                Part(PrimitiveType.Cube, t, at + Vector3.up * .25f, new Vector3(1.6f, .5f, 1.6f), CityShade); Part(PrimitiveType.Cube, t, at + Vector3.up * (H - .3f), new Vector3(1.5f, .6f, 1.5f), Gilt);
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, .07f, -2.6f), new Vector3(3, .03f, d - 6.4f), CouncilCloth);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.45f, .08f, -2.6f), new Vector3(.18f, .03f, d - 6.4f), CouncilGold);
            Part(PrimitiveType.Cube, t, new Vector3(0, .08f, d / 2 - 2.85f), new Vector3(12, .16f, 5.2f), Marble);   // the dais: a low step, walked onto
            Part(PrimitiveType.Cube, t, new Vector3(0, .17f, d / 2 - 5.45f), new Vector3(12, .04f, .12f), Gilt);
            var throne = new Vector3(0, .16f, d / 2 - 1.6f);
            BoxPart(t, throne + new Vector3(0, .5f, 0), new Vector3(1.8f, 1, 1.3f), Marble);
            BoxPart(t, throne + new Vector3(0, 2.1f, .5f), new Vector3(1.8f, 3.2f, .35f), Marble);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, throne + new Vector3(s * .95f, 1.25f, 0), new Vector3(.22f, .7f, 1.2f), Gilt);
            Part(PrimitiveType.Cube, t, throne + new Vector3(0, 1.08f, -.05f), new Vector3(1.4f, .12f, 1.1f), CouncilCloth);
            CrownDevice(t, throne + new Vector3(0, 3.4f, .3f), 0, 1, Gilt);
            Blocker(t, throne + new Vector3(0, 1.8f, .1f), new Vector3(2.2f, 3.6f, 1.8f));
            foreach (int s in new[] { -1, 1 }) for (int k = 0; k < 2; k++) CouncilBanner(t, new Vector3(s * (w / 2 - .3f), 9.5f, -5 + k * 8), s < 0 ? 270 : 90, 1.8f, 5);
            CouncilBanner(t, new Vector3(-5, 9.5f, d / 2 - .3f), 0, 1.8f, 5); CouncilBanner(t, new Vector3(5, 9.5f, d / 2 - .3f), 0, 1.8f, 5);   // on the back wall, facing the door
            foreach (float z in new[] { -5f, 3f }) ModelProp(t, "Fantasy/Chandelier", new Vector3(0, H - 2.2f, z), 0, 1.4f);
            foreach (int s in new[] { -1, 1 }) { Brazier(t, new Vector3(s * 5.4f, .16f, d / 2 - 1.6f), Gilt); Glow(t, new Vector3(s * 5.4f, 1.6f, d / 2 - 1.6f), 7, 1.1f, new Color(1, .7f, .4f), 1.6f); }
            _ = r;
        }

        // ---------- the Council Hall ----------
        /// <summary>
        /// The Council Hall (GAME-ONLY building; the Council is CANON), east of the keep: the map's domed hall. A square hall, a drum and a
        /// gilt dome with a lantern; a portico of four columns. Inside, the Council's seats in a ring round a lectern, and on the back wall
        /// the edicts board (the Council's notices: GAME-ONLY). Door to the south (-Z).
        /// </summary>
        void DomeHall(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 10, wallH = 12, rad = Mathf.Min(w, d) * .4f;
            HallShell(t, w, d, H, wallH, 4, 5.5f, CityWhite);
            Part(PrimitiveType.Cube, t, new Vector3(0, wallH + .2f, 0), new Vector3(w, .4f, d), BlueSlate);
            MeshPart(PropMesh("Dome drum " + Mathf.RoundToInt(rad * 10), () => Turned(new[] { new Vector2(rad + .2f, 0), new Vector2(rad + .2f, .4f), new Vector2(rad, .5f), new Vector2(rad, 3), new Vector2(rad + .3f, 3.1f), new Vector2(rad + .3f, 3.4f), new Vector2(0, 3.4f) }, 28, 1.75f)), t, new Vector3(0, wallH + .4f, 0), CityWhite);
            var dome = new List<Vector2>(); for (int k = 0; k <= 10; k++) { float a = k * Mathf.PI / 20; dome.Add(new Vector2(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad * 1.05f)); }
            MeshPart(PropMesh("Dome " + Mathf.RoundToInt(rad * 10), () => Turned(dome.ToArray(), 28, 1.5f)), t, new Vector3(0, wallH + 3.8f, 0), GiltRoof);
            float top = wallH + 3.8f + rad * 1.05f;
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * (rad + .05f), wallH + 2, Mathf.Sin(a) * (rad + .05f)), new Vector3(.5f, 3, .5f), CityWhite, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, top + 1, 0), new Vector3(2.2f, 1.1f, 2.2f), CityWhite);
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * 1.12f, top + 1, Mathf.Sin(a) * 1.12f), new Vector3(.3f, 1.2f, .05f), art.glass, Quaternion.Euler(0, 90 - a * Mathf.Rad2Deg, 0)); }
            GiltSpire(t, new Vector3(0, top + 2.1f, 0), 1.4f, 3.4f, 8);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Pinnacle(t, new Vector3(sx * (w / 2 - .6f), wallH, sz * (d / 2 - .6f)), 2.4f, 1, 3);
            Portico(t, d, 14, 8, t.name);
            // Inside: the Council's seats in a ring round the lectern, and the edicts board.
            var mid = new Vector3(0, 0, 1.5f);
            for (int k = 0; k < 9; k++)
            {
                float a = (20 + k * 17.5f) * Mathf.Deg2Rad; var at = mid + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 7.2f;
                CityBench(t, at, 90 - a * Mathf.Rad2Deg + 180, 2.4f);
            }
            Solidify(BoxPart(t, mid + new Vector3(0, .55f, 0), new Vector3(.9f, 1.1f, .7f), Marble)); Part(PrimitiveType.Cube, t, mid + new Vector3(0, 1.14f, -.05f), new Vector3(1, .08f, .8f), Gilt, Quaternion.Euler(-12, 0, 0));
            var board = new Vector3(0, 2.6f, d / 2 - .32f);
            BoxPart(t, board, new Vector3(4.6f, 2.8f, .12f), DarkOak);
            foreach (int s in new[] { -1, 1 }) { Part(PrimitiveType.Cube, t, board + new Vector3(s * 2.35f, 0, -.06f), new Vector3(.12f, 3, .1f), Gilt); Part(PrimitiveType.Cube, t, board + new Vector3(0, s * 1.45f, -.06f), new Vector3(4.8f, .12f, .1f), Gilt); }
            var rb = CityRandom(t, 0x5ed1);
            for (int k = 0; k < 10; k++) Part(PrimitiveType.Cube, t, board + new Vector3(-1.8f + (k % 5) * .9f + ((float)rb.NextDouble() - .5f) * .2f, (k < 5 ? .62f : -.62f) + ((float)rb.NextDouble() - .5f) * .2f, -.08f), new Vector3(.62f, .82f, .01f), Tint(art.plaster, new Color(.95f, .93f, .86f)), Quaternion.Euler(0, 0, ((float)rb.NextDouble() - .5f) * 8));
            CrownDevice(t, board + new Vector3(0, 1.85f, -.05f), 0, .9f, Gilt);
            Lettering(t, "Edicts of the Council", board + new Vector3(0, -1.75f, -.1f), 3.6f);
            ModelProp(t, "Fantasy/Chandelier", mid + Vector3.up * (H - 2), 0, 1.4f);
            foreach (int s in new[] { -1, 1 }) CouncilBanner(t, new Vector3(s * (w / 2 - .3f), 8.5f, 4), s < 0 ? 270 : 90, 1.8f, 4.5f);
        }

        // ---------- the Archive ----------
        /// <summary>
        /// The Archive (CANON-EXPANDED: the High City's archives, world_bible.md), west of the keep: the map's many-spired, cathedral-like
        /// mass. A nave under a steep slate roof, two front towers with tall gilt spires, pinnacles down both sides and at the back, a rose
        /// window over the door. Inside, shelves down the walls and in rows, reading desks with candles, the archivist's lectern by the door.
        /// </summary>
        void Archive(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 9, wallH = 14, roofH = 9; var r = CityRandom(t, 0xa2c1);
            HallShell(t, w, d, H, wallH, 3.2f, 4.6f, CityWhite);
            MeshPart(ZoneMeshes.GableRoof(d + 1.2f, w + 1.2f, roofH, .3f), t, new Vector3(0, wallH, 0), BlueSlate, Quaternion.Euler(0, 90, 0));
            Part(PrimitiveType.Cube, t, new Vector3(0, wallH + roofH + .05f, 0), new Vector3(.2f, .16f, d + 1.3f), Gilt);
            foreach (int s in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(w, roofH, .5f), t, new Vector3(0, wallH, s * d / 2), CityWhite);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, wallH + 3.2f, -d / 2 - .28f), new Vector3(4.2f, .06f, 4.2f), art.glass, Quaternion.Euler(90, 0, 0));   // the rose window
            Part(PrimitiveType.Cylinder, t, new Vector3(0, wallH + 3.2f, -d / 2 - .3f), new Vector3(4.6f, .05f, 4.6f), Gilt, Quaternion.Euler(90, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                var c = new Vector3(s * (w / 2 - 2.5f), 0, -d / 2 + 2.5f);
                SolidBox(t, c + Vector3.up * 15, new Vector3(5, 30, 5), CityWhite);
                foreach (float y in new[] { 14f, 22f }) Part(PrimitiveType.Cube, t, c + Vector3.up * y, new Vector3(5.2f, .3f, 5.2f), Gilt);
                CityWindow(t, c + new Vector3(0, 25, -2.5f), 0, 1, 3.2f, .3f);
                BattlementRing(t, c, 2.2f, 30, CityWhite);
                GiltSpire(t, c + Vector3.up * 30.4f, 3, 14, 8);
                foreach (int k in new[] { -1, 1 }) Pinnacle(t, c + new Vector3(k * 2, 30, -2), 1.6f, .7f, 3);
            }
            foreach (int s in new[] { -1, 1 })
            {
                for (float z = -3; z <= d / 2 - 1; z += 4.5f) Pinnacle(t, new Vector3(s * (w / 2 + .45f), 0, z), 19, 1.2f, 5);
                Pinnacle(t, new Vector3(s * 3.5f, wallH, d / 2), 4, 1, 4);
            }
            // Inside: shelves down the walls, two rows of them each side, desks, the archivist's lectern.
            foreach (int s in new[] { -1, 1 })
            {
                for (float z = -5; z <= d / 2 - 1.8f; z += 2.6f) BookShelf(t, new Vector3(s * (w / 2 - .65f), 0, z), s < 0 ? 270 : 90, r);
                for (int k = 0; k < 3; k++) { BookShelf(t, new Vector3(s * 7.5f, 0, 1.8f + k * 2.6f), 0, r); BookShelf(t, new Vector3(s * 7.5f, 0, 2.4f + k * 2.6f), 180, r); }
                CityTable(t, new Vector3(s * 7.5f, 0, -3.8f), 0, 3, 1.2f);
                foreach (int k in new[] { -1, 1 }) ModelProp(t, "Medieval/Prefabs/CandleV1", new Vector3(s * 7.5f + k * .9f, .82f, -3.8f), 0, .2f);
                ModelProp(t, "KayKit/chair", new Vector3(s * 7.5f, 0, -4.9f), 0, 1.1f);
            }
            var lectern = new Vector3(-4.4f, 0, -6.6f);
            Solidify(BoxPart(t, lectern + Vector3.up * .55f, new Vector3(.7f, 1.1f, .6f), DarkOak));
            Part(PrimitiveType.Cube, t, lectern + new Vector3(0, 1.15f, 0), new Vector3(.6f, .06f, .45f), Tint(art.plaster, new Color(.94f, .9f, .78f)), Quaternion.Euler(-15, 0, 0));
            foreach (float z in new[] { -3f, 4f }) ModelProp(t, "Fantasy/Chandelier", new Vector3(0, H - 2, z), 0, 1.2f);
            Lettering(t, t.name, new Vector3(0, 5.3f, -d / 2 - .4f), 6);
        }

        // ---------- the civic halls ----------
        /// <summary>
        /// A civic hall (GAME-ONLY): two storeys of white stone with pilasters, a gilt cornice, a slate roof, a portico with the hall's name.
        /// variant 0 the Exchange (the counter where lots are listed and bought, its boards of lots; the auctioneer and Port Caelum's factor
        /// stand before it), 1 the Treasury Vault (the round gilt vault door, the strongboxes, the counter; the banker), 2 the Hall of Arms
        /// (weapon stands down the back, a dummy in each front corner; the class trainers stand in a row), 3 the Armoury (mannequins, a tall
        /// glass, wardrobe chests; the dresser). Door to the south (-Z). The people's places are in the zone's data (ZoneLife).
        /// </summary>
        void Mansion(Transform t, Vector2 size, int variant)
        {
            float w = size.x, d = size.y, H = 6.5f, wallH = 10; var r = CityRandom(t, 0x3a75 + variant);
            HallShell(t, w, d, H, wallH, 3.2f, 4.4f, CityWhite);
            HallRoof(t, w, d, wallH, Mathf.Max(3, d * .32f), BlueSlate);
            foreach (int s in new[] { -1, 1 }) for (float x = 2.6f; x < w / 2 - .3f; x += 3.2f) Part(PrimitiveType.Cube, t, new Vector3(s * x, wallH / 2, -d / 2 - .2f), new Vector3(.5f, wallH, .3f), CityWhite);
            foreach (int s in new[] { -1, 1 }) for (float x = 1.2f; x < w / 2 - 1; x += 3.2f) CityWindow(t, new Vector3(s * x, 7.9f, -d / 2), 0, .9f, 1.6f, .5f);
            Portico(t, d, w >= 18 ? 14 : 7, 6.4f, t.name);
            switch (variant)
            {
                case 0:   // the Exchange
                {
                    Solidify(BoxPart(t, new Vector3(0, .55f, d / 2 - 3.5f), new Vector3(Mathf.Min(14, w - 6), 1.1f, .9f), DarkOak));
                    Part(PrimitiveType.Cube, t, new Vector3(0, 1.13f, d / 2 - 3.5f), new Vector3(Mathf.Min(14, w - 6) + .2f, .07f, 1.1f), Gilt);
                    for (int k = 0; k < 3; k++)
                    {
                        var b = new Vector3(-5 + k * 5, 3, d / 2 - .32f); BoxPart(t, b, new Vector3(3.6f, 2.4f, .1f), DarkOak);
                        for (int j = 0; j < 8; j++) Part(PrimitiveType.Cube, t, b + new Vector3(-1.35f + (j % 4) * .9f, j < 4 ? .5f : -.5f, -.07f), new Vector3(.6f, .7f, .01f), Tint(art.plaster, new Color(.95f, .92f, .84f)), Quaternion.Euler(0, 0, ((float)r.NextDouble() - .5f) * 6));
                    }
                    foreach (int s in new[] { -1, 1 })
                    {
                        if (ModelProp(t, "KayKit/crates_stacked", new Vector3(s * (w / 2 - 1.4f), 0, d / 2 - 1.6f), s * 20, 2, true) == null) Solidify(BoxPart(t, new Vector3(s * (w / 2 - 1.4f), .8f, d / 2 - 1.6f), new Vector3(1.6f, 1.6f, 1.6f), Tint(art.timber, new Color(.5f, .38f, .24f))));
                        CityBench(t, new Vector3(s * 6, 0, -d / 2 + 3.4f), 0, 2.6f);
                    }
                    break;
                }
                case 1:   // the Treasury Vault
                {
                    var door = new Vector3(0, 2.3f, d / 2 - .3f);
                    Part(PrimitiveType.Cylinder, t, door, new Vector3(3.8f, .12f, 3.8f), Gilt, Quaternion.Euler(90, 0, 0));
                    Part(PrimitiveType.Cylinder, t, door + new Vector3(0, 0, -.12f), new Vector3(2.6f, .08f, 2.6f), Tint(art.metal, new Color(.75f, .6f, .3f)), Quaternion.Euler(90, 0, 0));
                    for (int k = 0; k < 6; k++) Part(PrimitiveType.Cube, t, door + Quaternion.Euler(0, 0, k * 60) * new Vector3(0, .9f, 0) + new Vector3(0, 0, -.2f), new Vector3(.12f, .9f, .08f), Gilt, Quaternion.Euler(0, 0, k * 60));   // the wheel's spokes
                    Solidify(BoxPart(t, new Vector3(0, .55f, d / 2 - 4), new Vector3(6, 1.1f, .9f), DarkOak));
                    Part(PrimitiveType.Cube, t, new Vector3(0, 1.13f, d / 2 - 4), new Vector3(6.2f, .07f, 1.1f), Gilt);
                    foreach (int s in new[] { -1, 1 }) for (int k = 0; k < 3; k++)
                    {
                        var at = new Vector3(s * (w / 2 - 1.2f), 0, -1 + k * 2.2f);
                        if (ModelProp(t, "KayKit/chest", at, s < 0 ? 90 : 270, 1, true) == null) Solidify(BoxPart(t, at + Vector3.up * .45f, new Vector3(1.1f, .9f, .8f), Tint(art.timber, new Color(.35f, .22f, .12f))));
                    }
                    break;
                }
                case 2:   // the Hall of Arms
                {
                    for (float x = -w / 2 + 2.4f; x <= w / 2 - 2.4f; x += 4.3f)
                        if (ModelProp(t, "Fantasy/WeaponStand", new Vector3(x, 0, d / 2 - 1), 180, 1.6f, true) == null) Solidify(BoxPart(t, new Vector3(x, .8f, d / 2 - 1), new Vector3(1.4f, 1.6f, .5f), DarkOak));
                    foreach (int s in new[] { -1, 1 })
                        if (ModelProp(t, "Fantasy/Dummy", new Vector3(s * (w / 2 - 1.6f), 0, -d / 2 + 1.8f), s * 30, 1.9f, true) == null) Solidify(Part(PrimitiveType.Cylinder, t, new Vector3(s * (w / 2 - 1.6f), .95f, -d / 2 + 1.8f), new Vector3(.5f, .95f, .5f), Tint(art.hay, new Color(.8f, .7f, .45f))));
                    foreach (int s in new[] { -1, 1 }) CouncilBanner(t, new Vector3(s * (w / 2 - 4), 5.8f, d / 2 - .3f), 0, 1.6f, 3.6f);
                    break;
                }
                default:   // the Armoury: the dresser's
                {
                    foreach (float x in new[] { -3.6f, -1.6f, 1.6f, 3.6f })
                        if (ModelProp(t, "SmallProps/Manequin_1_A_001", new Vector3(x, 0, d / 2 - 1.2f), 180, 1.8f, true) == null) Solidify(Part(PrimitiveType.Capsule, t, new Vector3(x, .9f, d / 2 - 1.2f), new Vector3(.5f, .9f, .4f), Tint(art.cloth, Color.HSVToRGB((float)r.NextDouble(), .4f, .55f))));
                    var glassAt = new Vector3(-w / 2 + .32f, 1.5f, -.5f);
                    Part(PrimitiveType.Cube, t, glassAt, new Vector3(.06f, 2.2f, 1.1f), Tint(art.metal, new Color(.82f, .86f, .9f)));
                    Part(PrimitiveType.Cube, t, glassAt + new Vector3(-.03f, 0, 0), new Vector3(.08f, 2.4f, 1.3f), Gilt);
                    if (ModelProp(t, "KayKit/trunk_large_A", new Vector3(w / 2 - 1.2f, 0, .2f), 270, 1, true) == null) Solidify(BoxPart(t, new Vector3(w / 2 - 1.2f, .5f, .2f), new Vector3(.9f, 1, 1.4f), DarkOak));
                    break;
                }
            }
        }
        /// <summary>
        /// A guild's hall on the Guild Row (GAME-ONLY; the four guilds of round 27, SimGuilds' order: 0 the Lantern Watch, 1 Oak and Ember,
        /// 2 the Long Road, 3 the Scree Hounds): white stone with a slate roof, the guild's banners on poles by the door and one over it, its
        /// name in gilt. Inside, a long table and benches, the guild's great banner on the back wall (the master stands before it, ZoneLife),
        /// a notice board of its runs and asks, a hearth.
        /// </summary>
        void GuildHall(Transform t, Vector2 size, int guild)
        {
            float w = size.x, d = size.y, H = 6, wallH = 8.5f; var r = CityRandom(t, 0x9417 + guild);
            HallShell(t, w, d, H, wallH, 3.2f, 4, CityWhite);
            HallRoof(t, w, d, wallH, Mathf.Max(3, d * .36f), BlueSlate);
            foreach (int s in new[] { -1, 1 })
            {
                var pole = new Vector3(s * 3, 0, -d / 2 - 1.4f);
                Part(PrimitiveType.Cylinder, t, pole + Vector3.up * 3.2f, new Vector3(.12f, 3.2f, .12f), DarkOak); Part(PrimitiveType.Sphere, t, pole + Vector3.up * 6.5f, Vector3.one * .26f, Gilt);
                GuildBanner(t, pole + new Vector3(0, 6, -.12f), 0, 1.4f, 3.2f, guild);
            }
            GuildBanner(t, new Vector3(0, wallH - 1, -d / 2 - .35f), 0, 2, 3, guild);
            Lettering(t, t.name, new Vector3(0, 4.75f, -d / 2 - .45f), 6.5f);
            CityTable(t, new Vector3(0, 0, -.6f), 0, 5, 1.4f);
            foreach (int s in new[] { -1, 1 }) CityBench(t, new Vector3(0, 0, -.6f + s * 1.25f), s < 0 ? 0 : 180, 4.6f);
            for (int k = 0; k < 4; k++) ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(-1.8f + k * 1.2f, .82f, -.6f + ((k % 2) - .5f) * .5f), k * 40, .13f);
            GuildBanner(t, new Vector3(0, 5.2f, d / 2 - .3f), 0, 2.6f, 3.6f, guild);   // the great banner on the back wall, facing the door
            var board = new Vector3(-w / 2 + .32f, 2, -.5f);
            BoxPart(t, board, new Vector3(.1f, 1.6f, 2.4f), DarkOak);
            for (int k = 0; k < 6; k++) Part(PrimitiveType.Cube, t, board + new Vector3(.07f, k < 3 ? .35f : -.35f, -.8f + (k % 3) * .8f), new Vector3(.01f, .5f, .45f), Tint(art.plaster, new Color(.95f, .92f, .84f)), Quaternion.Euler(((float)r.NextDouble() - .5f) * 8, 0, 0));
            var hearth = new Vector3(w / 2 - .7f, 0, 1.6f);
            SolidBox(t, hearth + Vector3.up * 1.1f, new Vector3(.9f, 2.2f, 2.2f), Masonry);
            Part(PrimitiveType.Cube, t, hearth + new Vector3(-.46f, .55f, 0), new Vector3(.06f, .7f, 1.3f), art.glass);
            Glow(t, hearth + new Vector3(-1, 1, 0), 7, 1.2f, new Color(1, .58f, .28f), 1.6f);
        }
        /// <summary>
        /// A gilt house of the High City (GAME-ONLY): three storeys (four on variant 3) of white stone with gilt string courses and
        /// windows, a door in a gilt frame, a roof of gilt tiles (blue-grey slate on variant 2) and a chimney. Not walked into: one
        /// collider. Its front door is a home's (ZoneDoor: a resident with a household goes in there; knocking answers).
        /// </summary>
        void GiltHouse(Transform t, Vector2 size, int variant)
        {
            float w = size.x, d = size.y, storey = 3.3f, walls = storey * (variant == 3 ? 4 : 3), roofH = Mathf.Max(2.6f, d * .55f);
            var wall = Dressed(variant % 2 == 0 ? new Color(.84f, .82f, .77f) : new Color(.86f, .8f, .7f));
            BoxPart(t, new Vector3(0, walls / 2, 0), new Vector3(w, walls, d), wall);
            Part(PrimitiveType.Cube, t, new Vector3(0, .35f, 0), new Vector3(w + .3f, .7f, d + .3f), CityShade).name = "Plinth";
            for (int s = 1; s * storey < walls - .1f; s++) Part(PrimitiveType.Cube, t, new Vector3(0, s * storey, 0), new Vector3(w + .14f, .14f, d + .14f), Gilt);
            Part(PrimitiveType.Cube, t, new Vector3(0, walls - .15f, 0), new Vector3(w + .5f, .3f, d + .5f), CityWhite);
            for (int s = 0; s * storey < walls - .1f; s++)
                foreach (int f in new[] { 0, 1, 2, 3 })
                {
                    float face = f < 2 ? d / 2 : w / 2, run = f < 2 ? w : d, yaw = f == 0 ? 0 : f == 1 ? 180 : f == 2 ? 270 : 90;
                    var q = Quaternion.Euler(0, yaw, 0);
                    for (float x = -run / 2 + 1.5f; x <= run / 2 - 1.4f; x += 2.2f)
                    {
                        if (s == 0 && f == 0 && Mathf.Abs(x) < 1.3f) continue;   // the door
                        CityWindow(t, q * new Vector3(x, 0, -face) + Vector3.up * (s * storey + 1.8f), yaw, .8f, 1.5f, .1f);
                    }
                }
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.3f, -d / 2 - .06f), new Vector3(1.3f, 2.5f, .12f), DarkOak);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .76f, 1.35f, -d / 2 - .12f), new Vector3(.2f, 2.7f, .14f), Gilt);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.8f, -d / 2 - .12f), new Vector3(1.8f, .22f, .16f), Gilt);
            MeshPart(ZoneMeshes.GableRoof(w + .8f, d + 1, roofH, .25f), t, new Vector3(0, walls, 0), variant == 2 ? BlueSlate : GiltRoof);
            Part(PrimitiveType.Cube, t, new Vector3(0, walls + roofH + .04f, 0), new Vector3(w + .9f, .12f, .14f), Gilt);
            BoxPart(t, new Vector3(w / 2 - 1.2f, walls + roofH * .7f, d * .18f), new Vector3(.8f, roofH * 1.1f, .8f), CityWhite, null, 1);
            if (art.particle != null && variant != 3) Smoke(t, new Vector3(w / 2 - 1.2f, walls + roofH * 1.3f, d * .18f));
            Solid(t, new Vector3(0, (walls + roofH) / 2, 0), new Vector3(w + .3f, walls + roofH, d + .3f));
            Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .9f)) });
        }

        // ---------- in the streets ----------
        /// <summary>A colonnade (GAME-ONLY): two rows of marble columns <paramref name="length"/> long along x, <paramref name="depth"/> apart,
        /// under an entablature and a flat roof with a gilt cornice. Walked under; each column solid.</summary>
        void Colonnade(Transform t, float length, float depth)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(length / 3)); float high = 5;
            for (int i = 0; i <= n; i++) foreach (int s in new[] { -1, 1 })
            {
                var at = new Vector3(-length / 2 + i * length / n, 0, s * depth / 2);
                Solidify(Part(PrimitiveType.Cylinder, t, at + Vector3.up * high / 2, new Vector3(.5f, high / 2, .5f), Marble));
                Part(PrimitiveType.Cube, t, at + Vector3.up * .15f, new Vector3(.8f, .3f, .8f), CityShade); Part(PrimitiveType.Cube, t, at + Vector3.up * (high - .12f), new Vector3(.75f, .25f, .75f), Gilt);
            }
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(0, high + .35f, s * depth / 2), new Vector3(length + .8f, .7f, .8f), CityWhite);
            BoxPart(t, new Vector3(0, high + .85f, 0), new Vector3(length + 1, .3f, depth + 1.2f), CityWhite);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, high + 1.05f, s * (depth / 2 + .6f)), new Vector3(length + 1.1f, .14f, .1f), Gilt);
        }
        /// <summary>A fountain (GAME-ONLY): a marble basin of still water, a pedestal and two bowls, a gilt finial (variant 1: the bigger
        /// basin of the Crown Square with a gilt crown on its top). Solid round its rim.</summary>
        void Fountain(Transform t, int variant)
        {
            float R = variant == 1 ? 4.6f : 3;
            MeshPart(PropMesh("Fountain basin " + variant, () => Turned(new[] { new Vector2(R, 0), new Vector2(R, .7f), new Vector2(R - .35f, .7f), new Vector2(R - .35f, .25f), new Vector2(0, .25f) }, 32, 1.2f)), t, Vector3.zero, Marble);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .02f, 0), new Vector3(R * 2 + .6f, .02f, R * 2 + .6f), CityShade);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .55f, 0), new Vector3((R - .38f) * 2, .02f, (R - .38f) * 2), art.water != null ? art.water : art.glass);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.3f, 0), new Vector3(.6f, 1.3f, .6f), Marble);
            foreach (var (y, rr) in new[] { (1.6f, R * .45f), (2.7f, R * .25f) })
            {
                MeshPart(PropMesh("Fountain bowl " + Mathf.RoundToInt(rr * 100), () => Turned(new[] { new Vector2(.2f, -.35f), new Vector2(rr, 0), new Vector2(rr, .12f), new Vector2(rr - .12f, .12f), new Vector2(0, -.05f) }, 24, 1)), t, Vector3.up * y, Marble);
                Part(PrimitiveType.Cylinder, t, Vector3.up * (y + .06f), new Vector3((rr - .14f) * 2, .01f, (rr - .14f) * 2), art.water != null ? art.water : art.glass);
            }
            if (variant == 1) CrownDevice(t, new Vector3(0, 3.6f, 0), 0, 1, Gilt); else GiltSpire(t, Vector3.up * 2.8f, .3f, .8f, 6);
            var col = t.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, .6f, 0); col.height = 1.2f + R * 2; col.radius = R; t.gameObject.AddComponent<NavBlocker>();
        }
        /// <summary>A statue on a plinth (GAME-ONLY figures, none of them a named person): a robed figure in marble with a gilt circlet,
        /// holding a book (variant 0) or a gilt crown raised (variant 1). Solid at its plinth.</summary>
        void Statue(Transform t, int variant)
        {
            SolidBox(t, new Vector3(0, 1, 0), new Vector3(1.8f, 2, 1.8f), CityShade);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.85f, 0), new Vector3(1.95f, .2f, 1.95f), Gilt);
            MeshPart(PropMesh("Statue robe", () => Turned(new[] { new Vector2(.62f, 0), new Vector2(.5f, .9f), new Vector2(.36f, 1.7f), new Vector2(.3f, 2.1f), new Vector2(0, 2.15f) }, 14, 1)), t, Vector3.up * 2, Marble);
            Part(PrimitiveType.Sphere, t, new Vector3(0, 4.35f, 0), Vector3.one * .42f, Marble);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 4.52f, 0), new Vector3(.4f, .05f, .4f), Gilt);
            if (variant == 1)
            {
                Part(PrimitiveType.Capsule, t, new Vector3(.3f, 4.55f, -.1f), new Vector3(.18f, .55f, .18f), Marble, Quaternion.Euler(-8, 0, -20));
                CrownDevice(t, new Vector3(.5f, 5.3f, -.15f), 0, .55f, Gilt);
                Part(PrimitiveType.Capsule, t, new Vector3(-.36f, 3.5f, 0), new Vector3(.17f, .5f, .17f), Marble, Quaternion.Euler(0, 0, 10));
            }
            else
            {
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, t, new Vector3(s * .28f, 3.45f, -.28f), new Vector3(.17f, .45f, .17f), Marble, Quaternion.Euler(-60, 0, -s * 20));
                Part(PrimitiveType.Cube, t, new Vector3(0, 3.3f, -.55f), new Vector3(.55f, .08f, .4f), Gilt, Quaternion.Euler(-20, 0, 0));
            }
        }
        /// <summary>A street lamp of the Crown: a white stone post, a gilt lantern on it, lit after dark.</summary>
        void CityLamp(Transform t)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, .25f, 0), new Vector3(.6f, .5f, .6f), CityShade);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.8f, 0), new Vector3(.24f, 1.6f, .24f), CityWhite);
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.55f, 0), new Vector3(.42f, .5f, .42f), art.glass);
            MeshPart(PropMesh("City lantern roof", () => ZoneMeshes.Cone(.36f, .34f, 4)), t, new Vector3(0, 3.8f, 0), Gilt, Quaternion.Euler(0, 45, 0));
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.28f, 0), new Vector3(.5f, .06f, .5f), Gilt);
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 3.4f, 0);
            l.type = LightType.Point; l.range = 10; l.intensity = 1.2f; l.color = new Color(1, .78f, .45f);
            NightLights.Add(new NightLight { light = l, dayIntensity = 0, nightIntensity = 1.6f });
            Solid(t, new Vector3(0, 1.7f, 0), new Vector3(.5f, 3.4f, .5f));
        }
        /// <summary>A Council banner on its own pole (a square, a gate, the stair head): a gilt-capped pole and a crossbar.</summary>
        void CouncilBannerPole(Transform t, float height)
        {
            Part(PrimitiveType.Cylinder, t, Vector3.up * height / 2, new Vector3(.14f, height / 2, .14f), CityWhite);
            Part(PrimitiveType.Sphere, t, Vector3.up * (height + .1f), Vector3.one * .3f, Gilt);
            CouncilBanner(t, new Vector3(0, height - .4f, -.14f), 0, 1.5f, 3.6f);
            Part(PrimitiveType.Cube, t, new Vector3(0, .2f, 0), new Vector3(.7f, .4f, .7f), CityShade);
        }
        /// <summary>A clipped garden (GAME-ONLY): a low box hedge round it with a gap in each side, cross walks, four beds of flowers and a
        /// topiary ball in the middle. Nothing solid: it is walked through.</summary>
        void Parterre(Transform t, Vector2 size)
        {
            var hedge = Tint(art.foliage, new Color(.16f, .34f, .14f)); var r = CityRandom(t, 0x9a2d);
            float hx = size.x / 2, hz = size.y / 2;
            var box = new List<CombineInstance>();
            foreach (int s in new[] { -1, 1 })
            {
                foreach (int k in new[] { -1, 1 })
                {
                    box.Add(Piece(ZoneMeshes.Box(new Vector3(hx - 1.2f, .55f, .5f), 1), new Vector3(k * (hx + 1.2f) / 2, .27f, s * hz)));
                    box.Add(Piece(ZoneMeshes.Box(new Vector3(.5f, .55f, hz - 1.2f), 1), new Vector3(s * hx, .27f, k * (hz + 1.2f) / 2)));
                }
            }
            var mesh = new Mesh { name = "Parterre hedges" }; mesh.CombineMeshes(box.ToArray(), true); mesh.RecalculateBounds(); foreach (var b in box) DestroyImmediate(b.mesh);
            MeshPart(mesh, t, Vector3.zero, hedge);
            Color[] blooms = { new Color(.85f, .2f, .25f), new Color(.95f, .8f, .2f), new Color(.6f, .35f, .85f), new Color(.95f, .95f, .9f) };
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var c = new Vector3(sx * hx / 2, 0, sz * hz / 2); var bloom = Tint(art.foliage, blooms[(int)(r.NextDouble() * blooms.Length) % blooms.Length]);
                Part(PrimitiveType.Cube, t, c + Vector3.up * .08f, new Vector3(hx - 2, .16f, hz - 2), Tint(art.soil != null ? art.soil : art.timber, new Color(.32f, .24f, .17f)));
                for (int k = 0; k < 10; k++) Part(PrimitiveType.Sphere, t, c + new Vector3(((float)r.NextDouble() - .5f) * (hx - 2.6f), .28f, ((float)r.NextDouble() - .5f) * (hz - 2.6f)), new Vector3(.45f, .3f, .45f), bloom);
            }
            Part(PrimitiveType.Cylinder, t, Vector3.up * .5f, new Vector3(.3f, .5f, .3f), CityShade);
            Part(PrimitiveType.Sphere, t, Vector3.up * 1.5f, Vector3.one * 1.2f, hedge);
        }
        /// <summary>
        /// Stairs over a ramp terrace (GAME-ONLY): white treads a .45 m going, each a block from under the ground at its low edge up to the
        /// ground at its high edge, so the ramp under them is what is walked (a foot sinks a step's rise at most; the navmesh never sees
        /// them). <paramref name="size"/>: across (x) and up the ramp (along its local z, which rises).
        /// </summary>
        void Stair(Transform t, Vector2 size)
        {
            var list = new List<CombineInstance>(); int n = Mathf.Max(1, Mathf.RoundToInt(size.y / .45f)); float go = size.y / n;
            for (int k = 0; k < n; k++)
            {
                float z0 = -size.y / 2 + k * go, z1 = z0 + go, low = Mathf.Min(LocalGround(t, 0, z0), LocalGround(t, 0, z1)) - .3f, high = Mathf.Max(LocalGround(t, 0, z1), LocalGround(t, 0, z0)) + .02f;
                list.Add(Ashlar(new Vector3(0, (low + high) / 2, (z0 + z1) / 2), new Vector3(size.x, high - low, go), 1.75f));
            }
            Joined32("Stair treads", t, Dressed(new Color(.86f, .84f, .8f)), list);
        }

        // ---------- the walls and gates ----------
        /// <summary>
        /// A run of wall along points (world metres; the prop is not turned). Each stretch of up to 4 m stands from below the lower ground
        /// beside it to over the higher: variant 0 the curtain wall (3 m thick, 11 m over the high side, merlons along its outer side, a
        /// battered foot of plain stone down the cliff on its low side, and a square tower every <paramref name="towerEvery"/> metres and at
        /// each bend (none at a run's ends: a gate's towers stand there), crenellated, every other one under a gilt pyramid (with the castle kit: its
        /// capped square tower and its large square tower, turn about, over a plain shaft down the cliff; the kit's tall wall along the top), a Council banner down each tower's outer face); variant 1 a parapet
        /// (.7 m thick, 1.15 m over the high side, a white coping): the retaining wall of a terrace, the side of a ramp. One collider a
        /// stretch, each cut out of the navmesh, the stone of the whole run joined into a mesh or two.
        /// </summary>
        void CityWall(Transform t, Vector2[] points, int variant, float towerEvery)
        {
            if (points == null || points.Length < 2) return;
            bool curtain = variant == 0, kit = HasCastleKit; float thick = curtain ? 3 : .7f, over = curtain ? 11 : 1.15f;
            var body = new List<CombineInstance>(); var foot = new List<CombineInstance>(); var top = new List<CombineInstance>();
            var towers = new List<(Vector2 at, Vector2 dir)>();
            float since = towerEvery / 2;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector2 a = points[i], b = points[i + 1], dir = b - a; float len = dir.magnitude; if (len < .1f) continue; dir /= len;
                var side = new Vector2(-dir.y, dir.x); float yaw = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg; var q = Quaternion.Euler(0, yaw, 0);
                if (curtain && i > 0) { towers.Add((a, dir)); since = 0; }   // at each bend (a run's ends are a gate's towers)
                int n = Mathf.Max(1, Mathf.CeilToInt(len / 4)); float piece = len / n;
                for (int k = 0; k < n; k++)
                {
                    var c = a + dir * (k + .5f) * piece;
                    float hl = HeightAt(c.x - side.x * (thick / 2 + .7f), c.y - side.y * (thick / 2 + .7f)), hr = HeightAt(c.x + side.x * (thick / 2 + .7f), c.y + side.y * (thick / 2 + .7f));
                    float high = Mathf.Max(hl, hr), low = Mathf.Min(hl, hr);
                    if (curtain) foreach (int s in new[] { -1, 1 }) low = Mathf.Min(low, HeightAt(c.x + s * side.x * (thick / 2 + 4.5f), c.y + s * side.y * (thick / 2 + 4.5f)));
                    float y0 = low - 1.5f, y1 = high + over; var outward = hl < hr ? -side : side;   // the low side is outside
                    var centre = t.InverseTransformPoint(new Vector3(c.x, (y0 + y1) / 2, c.y));
                    Blocker(t, centre, new Vector3(piece, y1 - y0, thick), q);
                    // The curtain's top is the castle kit's tall wall (its battlements on it) stretched to the stretch, at the kit's own
                    // proportions up it; the plain white stone below runs down the cliff. Without the kit: stone and merlons of our own.
                    float kitH = curtain && kit ? Mathf.Min(piece * 1.56f, y1 - y0) : 0;
                    if (kitH > 0) CastlePiece(t, "Cube.005", t.InverseTransformPoint(new Vector3(c.x, y1 - kitH, c.y)), Quaternion.Inverse(t.rotation) * q, new Vector3(piece + .04f, kitH, thick));
                    var bodyCentre = t.InverseTransformPoint(new Vector3(c.x, (y0 + y1 - kitH + .05f) / 2, c.y));
                    body.Add(Ashlar(bodyCentre, new Vector3(piece + .02f, y1 - kitH + .05f - y0, thick), 1.75f, q));
                    if (curtain)
                    {
                        var m = t.InverseTransformPoint(new Vector3(c.x + outward.x * (thick / 2 - .4f), 0, c.y + outward.y * (thick / 2 - .4f)));
                        if (!kit) Merlons(top, m - q * new Vector3(piece / 2, 0, 0), m + q * new Vector3(piece / 2, 0, 0), y1 - t.position.y, .9f, 1.8f);
                        if (high - low > 2.5f)   // the battered foot down the cliff below the wall
                        {
                            var f = t.InverseTransformPoint(new Vector3(c.x + outward.x * (thick / 2 + 1.75f), (y0 + high - .35f) / 2, c.y + outward.y * (thick / 2 + 1.75f)));
                            foot.Add(Ashlar(f, new Vector3(piece + .02f, high - .35f - y0, 3.5f), 1.75f, q));
                        }
                    }
                    else top.Add(Ashlar(t.InverseTransformPoint(new Vector3(c.x, y1 + .07f, c.y)), new Vector3(piece + .04f, .14f, thick + .18f), 1.75f, q));
                    since += piece;
                    if (curtain && since >= towerEvery && (b - c).magnitude > 10) { towers.Add((c, dir)); since = 0; }
                }
            }
            Joined32("Wall", t, curtain ? CityWhite : Dressed(new Color(.86f, .84f, .79f)), body);
            Joined32("Wall foot", t, CityShade, foot);
            Joined32(curtain ? "Merlons" : "Coping", t, curtain ? CityWhite : Marble, top);
            for (int i = 0; i < towers.Count; i++) WallTower(t, towers[i].at, towers[i].dir, i % 2 == 0);
        }
        /// <summary>A square tower of the curtain wall (7 m, 15 m over the high ground beside it), crenellated, a gilt pyramid on it when
        /// <paramref name="gilt"/>, a Council banner down its outer face. Solid.</summary>
        void WallTower(Transform t, Vector2 at, Vector2 dir, bool gilt)
        {
            var side = new Vector2(-dir.y, dir.x); float yaw = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg; var q = Quaternion.Euler(0, yaw, 0);
            float hl = HeightAt(at.x - side.x * 5, at.y - side.y * 5), hr = HeightAt(at.x + side.x * 5, at.y + side.y * 5), high = Mathf.Max(hl, hr), low = Mathf.Min(low0(), Mathf.Min(hl, hr));
            float low0() { float m = float.MaxValue; for (int k = 0; k < 8; k++) m = Mathf.Min(m, HeightAt(at.x + Mathf.Cos(k * .785f) * 6, at.y + Mathf.Sin(k * .785f) * 6)); return m; }
            var outward = hl < hr ? -side : side; float y0 = low - 1.5f, y1 = high + 15;
            var c = t.InverseTransformPoint(new Vector3(at.x, (y0 + y1) / 2, at.y)); var tq = Quaternion.Inverse(t.rotation) * q;
            Blocker(t, c, new Vector3(7, y1 - y0, 7), tq);
            var head = t.InverseTransformPoint(new Vector3(at.x, y1, at.y));
            if (HasCastleKit)
            {
                // The castle kit's towers over a plain white shaft down the cliff: the square tower under its gilt cap and the large square
                // tower, turn about (the map's crown of square towers).
                float from = high - 2;
                var shaft = t.InverseTransformPoint(new Vector3(at.x, (y0 + from) / 2, at.y));
                if (from - y0 > .2f) MeshPart(ZoneMeshes.Box(new Vector3(7, from - y0, 7), 1.75f, shaft), t, shaft, CityWhite, tq).name = "Wall tower shaft";
                var kitFoot = t.InverseTransformPoint(new Vector3(at.x, from, at.y));
                if (gilt) CastlePiece(t, "LargeSimpleTower.002", kitFoot, tq, new Vector3(7.4f, y1 + 6 - from, 7.4f));
                else CastlePiece(t, "Cube.042", kitFoot, tq, new Vector3(7.4f, y1 + 1 - from, 7.4f));
                var bannerAt = t.InverseTransformPoint(new Vector3(at.x + outward.x * 3.8f, y1 - 2.2f, at.y + outward.y * 3.8f));
                CouncilBanner(t, bannerAt, Mathf.Atan2(-outward.x, -outward.y) * Mathf.Rad2Deg - t.eulerAngles.y, 2.2f, 6);
                return;
            }
            var o = MeshPart(ZoneMeshes.Box(new Vector3(7, y1 - y0, 7), 1.75f, c), t, c, CityWhite, tq); o.name = "Wall tower";
            Part(PrimitiveType.Cube, t, head - Vector3.up * 3, new Vector3(7.2f, .3f, 7.2f), Gilt, tq);
            var list = new List<CombineInstance>();
            for (int f = 0; f < 4; f++) { var r = tq * Quaternion.Euler(0, f * 90, 0); Merlons(list, head + r * new Vector3(-3.2f, 0, -3.2f), head + r * new Vector3(3.2f, 0, -3.2f), head.y, .9f, 1.8f); }
            Joined32("Tower merlons", t, CityWhite, list);
            if (gilt) MeshPart(PropMesh("Tower pyramid", () => ZoneMeshes.Spire(4.4f, 5, 4, 2)), t, head + Vector3.up * .1f, GiltRoof, tq * Quaternion.Euler(0, 45, 0));
            var face = t.InverseTransformPoint(new Vector3(at.x + outward.x * 3.56f, y1 - 2.2f, at.y + outward.y * 3.56f));
            float faceYaw = Mathf.Atan2(-outward.x, -outward.y) * Mathf.Rad2Deg - t.eulerAngles.y;   // the banner's front looks outward
            CouncilBanner(t, face, faceYaw, 2.2f, 6);
        }
        /// <summary>
        /// A gate in the curtain wall (GAME-ONLY names: the Gilt Gate, the Low Stair): two towers either side of a passage
        /// <paramref name="w"/> wide and 8 m deep through the wall, an arch span over it with the Council's crown and the gate's name, gilt
        /// pyramids on the towers, banners down their faces, lanterns. Its front (-Z) faces out of the city. Two doors of oak on hinges:
        /// folded back against the passage, or <paramref name="shut"/> across it with a bar (then solid: the Low Stair, until the Lowtowns
        /// open). No portcullis, no winch: no machines.
        /// </summary>
        void CityGate(Transform t, float w, bool shut)
        {
            const float D = 8, tw = 5; float top = 12.5f; bool kit = HasCastleKit;
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (w / 2 + tw / 2), low = 0;
                for (int k = -1; k <= 1; k += 2) for (int j = -1; j <= 1; j += 2) low = Mathf.Min(low, LocalGround(t, x + k * tw / 2, j * (D / 2 + 3)));
                float y0 = low - 1.5f, h = top + 5 - y0;
                Blocker(t, new Vector3(x, y0 + h / 2, 0), new Vector3(tw, h, D));
                // The castle kit's large square tower on a plain shaft down to the lowest ground under it; else painted stone.
                if (kit && CastlePiece(t, "Cube.042", new Vector3(x, -.1f, 0), Quaternion.identity, new Vector3(tw + .4f, top + 5.6f, D + .2f)) != null)
                { if (y0 < -.2f) BoxPart(t, new Vector3(x, y0 / 2, 0), new Vector3(tw, -y0, D), CityWhite).name = "Gate tower shaft"; }
                else
                {
                    BoxPart(t, new Vector3(x, y0 + h / 2, 0), new Vector3(tw, h, D), CityWhite).name = "Gate tower";
                    BattlementRing(t, new Vector3(x, 0, 0), tw / 2 - .4f, top + 5, CityWhite);
                }
                Part(PrimitiveType.Cube, t, new Vector3(x, top + 1, 0), new Vector3(tw + .5f, .3f, D + .4f), Gilt);
                MeshPart(PropMesh("Gate pyramid", () => ZoneMeshes.Spire(3.6f, 5.5f, 4, 2)), t, new Vector3(x, top + 5.1f, 0), GiltRoof, Quaternion.Euler(0, 45, 0));   // inside the kit tower's battlements
                CouncilBanner(t, new Vector3(x, top + 2.5f, -D / 2 - .06f), 0, 2.2f, 5.5f);
                CouncilBanner(t, new Vector3(x, top + 2.5f, D / 2 + .06f), 180, 2.2f, 5.5f);
                var lantern = new Vector3(s * (w / 2 + .3f), 4.2f, -D / 2 - .4f);
                Part(PrimitiveType.Cube, t, lantern, new Vector3(.4f, .55f, .4f), art.glass); Part(PrimitiveType.Cube, t, lantern + Vector3.up * .34f, new Vector3(.5f, .1f, .5f), Gilt);
                Glow(t, lantern, 9, .4f, new Color(1, .76f, .42f), 1.6f);
            }
            // The arch over the passage: the castle kit's tall wall entrance, its opening (58% of its width) the passage's width and its
            // arch's crown (39% of its height) 5.8 m up; else a span of painted stone over a square opening 7.2 m high.
            float under = 7.2f;
            if (kit && CastlePiece(t, "TallWallEntrance.002", Vector3.zero, Quaternion.identity, new Vector3(w / .584f, 15, D)) != null) under = 5.8f;
            else { BoxPart(t, new Vector3(0, (under + top) / 2, 0), new Vector3(w + .2f, top - under, D), CityWhite); BattlementRing(t, Vector3.zero, w / 2 - .2f, top, CityWhite); }
            foreach (int f in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, under + .2f, f * (D / 2 + .25f)), new Vector3(w + .3f, .4f, .12f), Gilt);
                CrownDevice(t, new Vector3(0, under + 3.4f, f * (D / 2 + .32f)), f < 0 ? 0 : 180, 1.8f, Gilt);
            }
            Lettering(t, t.name, new Vector3(0, under + 1.2f, -D / 2 - .32f), w * .9f);
            var oak = Tint(art.timber, new Color(.32f, .2f, .11f));
            foreach (int s in new[] { -1, 1 })
            {
                var hinge = new GameObject("Gate hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(s * w / 2, 0, -1);
                hinge.localRotation = Quaternion.Euler(0, shut ? 0 : -s * 92, 0);
                float leaf = Mathf.Min(6.6f, under - .4f);
                Part(PrimitiveType.Cube, hinge, new Vector3(-s * w / 4, leaf / 2, 0), new Vector3(w / 2 - .05f, leaf, .22f), oak);
                for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, hinge, new Vector3(-s * w / 4, .8f + k * (leaf - 1.2f) / 3, -.13f), new Vector3(w / 2 - .2f, .14f, .05f), Gilt);
            }
            if (shut)
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, 3.2f, -1.2f), new Vector3(w + .4f, .3f, .25f), oak);   // the bar across, in its brackets
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (w / 2 - .1f), 3.2f, -1.25f), new Vector3(.3f, .5f, .35f), Gilt);
                Blocker(t, new Vector3(0, 3.5f, -1), new Vector3(w, 7, .8f));
            }
        }

        // ---------- below the walls: the backdrop ----------
        /// <summary>
        /// The Spire (CANON: it stands below the High City's south wall on the city map; world_bible.md, "the clutch of the Spire"): a dark
        /// stone needle rising from the Lowtowns far over the walls, ringed by balconies, its windows a cold glow, wreathed in cloud. A
        /// backdrop: never reached, nothing solid, nothing said of what it is (the Spoke is not shown or named).
        /// </summary>
        void TheSpire(Transform t, float height)
        {
            var dark = Dressed(new Color(.3f, .3f, .34f));
            var shaft = Turned(new[] { new Vector2(8, -2), new Vector2(7.6f, height * .1f), new Vector2(5.6f, height * .35f), new Vector2(4.2f, height * .62f), new Vector2(2.8f, height * .86f), new Vector2(2.4f, height * .9f), new Vector2(0, height * .9f) }, 8, 2.5f);
            shaft.name = "Spire shaft"; MeshPart(shaft, t, Vector3.zero, dark);
            MeshPart(PropMesh("Spire point", () => ZoneMeshes.Spire(2.8f, height * .2f, 8, 2)), t, Vector3.up * height * .9f, dark);
            foreach (float f in new[] { .2f, .45f, .7f })
            {
                float rr = Mathf.Lerp(7.6f, 2.8f, (f - .1f) / .76f) + 1.2f;
                Part(PrimitiveType.Cylinder, t, Vector3.up * height * f, new Vector3(rr * 2, .35f, rr * 2), Dressed(new Color(.24f, .24f, .27f)));
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + f; Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * (rr - 1.1f), height * f + 2.2f, Mathf.Sin(a) * (rr - 1.1f)), new Vector3(.5f, 1.6f, .1f), Glowing(new Color(.62f, .72f, .95f), 1.2f), Quaternion.Euler(0, 90 - a * Mathf.Rad2Deg, 0)); }
            }
            var cloud = Tint(art.plaster, new Color(.94f, .95f, .97f)); var r = CityRandom(t, 0xc10d);
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI * 2 / 16 + (float)r.NextDouble() * .3f, rr = 9 + (float)r.NextDouble() * 7, y = height * (.42f + (float)r.NextDouble() * .14f), s = 7 + (float)r.NextDouble() * 6;
                Part(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr), new Vector3(s * 1.4f, s * .45f, s), cloud, Quaternion.Euler(0, (float)r.NextDouble() * 180, 0)).name = "Cloud";
            }
        }
        /// <summary>
        /// The Lowtowns seen from the wall (CANON: the shadowed districts at the mountain's base, world_bible.md; ZONE_LOWTOWNS.md, levels
        /// 16-19, later): dark roofs crowded over <paramref name="size"/> (x across, y deep) on the ground below, none within 18 m of the
        /// Spire, and smoke rising from a few chimneys. A backdrop: nothing solid.
        /// </summary>
        void Lowtowns(Transform t, Vector2 size)
        {
            var r = CityRandom(t, 0x10e1); float R() { return (float)r.NextDouble(); }
            var walls = new List<CombineInstance>(); var roofs = new List<CombineInstance>();
            Vector2? spire = null; foreach (var p in Zone.props) if (p != null && p.kind == "spire") spire = p.at;
            int n = Mathf.Min(260, Mathf.RoundToInt(size.x * size.y / 70)); var smokers = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                float x = (R() - .5f) * size.x, z = (R() - .5f) * size.y, w = 3.5f + R() * 4, d = 3.5f + R() * 3.5f, h = 2.6f + R() * 4, yaw = R() * 30 - 15;
                var world = t.TransformPoint(new Vector3(x, 0, z));
                if (spire.HasValue && Vector2.Distance(new Vector2(world.x, world.z), spire.Value) < 18) continue;
                float g = LocalGround(t, x, z); var q = Quaternion.Euler(0, yaw, 0);
                walls.Add(Ashlar(new Vector3(x, g + h / 2 - .5f, z), new Vector3(w, h + 1, d), 1.75f, q));
                roofs.Add(Piece(ZoneMeshes.GableRoof(w + .5f, d + .6f, d * .5f, .2f), new Vector3(x, g + h - .5f, z), q));
                if (i % 23 == 5) smokers.Add(new Vector3(x, g + h + d * .5f, z));
            }
            Joined32("Lowtown walls", t, Tint(art.plaster, new Color(.36f, .32f, .28f)), walls);
            Joined32("Lowtown roofs", t, Tint(art.slate, new Color(.26f, .25f, .27f)), roofs);
            if (art.particle == null) return;
            foreach (var at in smokers)
            {
                var ps = Smoke(t, at); var main = ps.main;
                main.startLifetime = 16; main.startSpeed = 1.8f; main.startSize = new ParticleSystem.MinMaxCurve(4, 8); main.maxParticles = 60;
                main.startColor = new Color(.32f, .31f, .3f, .55f);
            }
        }
        /// <summary>The sea to the west below the cliffs (the city map: the land falls to the sea on the west): a still sheet of water over
        /// <paramref name="size"/>, a backdrop.</summary>
        void Sea(Transform t, Vector2 size)
        {
            Part(PrimitiveType.Cube, t, Vector3.up * .3f, new Vector3(size.x, .1f, size.y), Tint(art.plaster, new Color(.17f, .27f, .36f)));
        }
    }
}
