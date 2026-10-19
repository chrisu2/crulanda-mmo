using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The trades' new buildings in Oakhaven's data (tools/wip/professions/ADDENDUM.md A.3 and B.2): the Cask's kitchen sits on
    /// the inn's back wall where the inn leaves room for it, and every new building keeps the rules that leave the zone's trees,
    /// creek, secrets, fields and camps as they were.
    /// </summary>
    public class WorkshopDataTests
    {
        /// <summary>The props the trades added, in the order they were appended.</summary>
        public static readonly string[] NewProps = { "Carder farmhouse", "Crisp cottage", "Tanner's leather shop", "Lisbet's drying hut", "The Cask's kitchen", "Moss's game rack" };
        static ZoneDefinition Oakhaven { get { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/oakhaven.json"))); } }
        static float ToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
        /// <summary>From a point to an upright rectangle (a grove's), 0 inside it.</summary>
        static float ToBox(Vector2 p, Vector2 center, Vector2 size)
        {
            return new Vector2(Mathf.Max(0, Mathf.Abs(p.x - center.x) - size.x / 2), Mathf.Max(0, Mathf.Abs(p.y - center.y) - size.y / 2)).magnitude;
        }
        /// <summary>From a point to a turned rectangle (a field's, turned as VillageLife turns it), 0 inside it.</summary>
        static float ToField(Vector2 p, ZoneRect f)
        {
            float a = f.rotation * Mathf.Deg2Rad; var d = p - f.center;
            var local = new Vector2(d.x * Mathf.Cos(a) + d.y * Mathf.Sin(a), -d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
            return ToBox(local, Vector2.zero, f.size);
        }
        /// <summary>How far a new building reaches from its centre: a radius over its furthest built part (a workshop's roof
        /// corners, racks, sign and pentice as ZoneBuilder.Workshops builds them; a house's half longer side).</summary>
        static float Reach(ZoneProp p)
        {
            switch (p.kind)
            {
                case "leathershop": return 5;     // the hanging sign's corner (about -3, -3.9) and the roof corners (about 4.8 m)
                case "dryhut": return 6;          // the second drying rack out in front (about 5.7 m)
                case "kitchen": return 3.7f;      // the water butt and the roof slab's corner
                case "gamerack": return 2.6f;     // the ring of fire stones
                default: return Mathf.Max(p.size.x, p.size.y) / 2;
            }
        }

        [Test] public void Kitchen_abuts_the_inn()
        {
            var z = Oakhaven;
            var inn = z.props.Single(p => p != null && p.kind == "inn"); var kitchen = z.props.Single(p => p != null && p.kind == "kitchen");
            Assert.AreEqual("The Cask's kitchen", kitchen.name);
            float w = inn.size.x, d = inn.size.y;
            // The kitchen's middle in the inn's own frame (as ZoneBuilder.KitchenBehind finds it).
            var local = Quaternion.Inverse(Quaternion.Euler(0, inn.rotation, 0)) * new Vector3(kitchen.at.x - inn.at.x, 0, kitchen.at.y - inn.at.y);
            float face = d / 2 + .15f;   // the back wall's outer face: the inn's walls are .3 thick (ZoneBuilder.Inn)
            Assert.AreEqual(face + ZoneBuilder.KitchenSize.y / 2, local.z, .3f, "The kitchen's back edge meets the inn's back wall.");
            Assert.AreEqual(0, Mathf.DeltaAngle(kitchen.rotation, inn.rotation + 180), 1, "The kitchen faces away from the inn.");
            Assert.LessOrEqual(Mathf.Abs(local.x) + ZoneBuilder.KitchenSize.x / 2, w / 2 - .17f, "The lean-to stays between the inn's corner posts.");
            // The ground-floor windows on the back wall (x as ZoneBuilder.Inn lays them out): the kitchen takes the place of the
            // ones its lean-to would cover, and only those (two: at 0.7 and 3.3 in the 12 m Cask; at 2.3 and 4.9 since it grew to
            // 14 m, Phase 8.6, 2026-10-15, the kitchen moved with its back wall).
            var windows = new List<float>(); for (float x = -w / 2 + 1.5f; x < w / 2 - .8f; x += 2.6f) windows.Add(x);
            var covered = windows.Where(x => Mathf.Abs(x - local.x) < ZoneBuilder.KitchenSize.x / 2 + .85f).ToList();
            CollectionAssert.AreEqual(new[] { 2.3f, 4.9f }, covered.Select(x => Mathf.Round(x * 10) / 10).ToArray(), "The kitchen covers the two back windows the plan gives up, no others.");
            // The doors on the inner back wall: the rooms door and the kitchen's door, each clear of the other, of every window
            // left, of the bar top and of the hearth.
            var plan = ZoneBuilder.InnPlan.For(inn.size, inn.variant);   // the taproom as the builder lays it out (Phase 8.6)
            float rooms = plan.roomsX, kitchenDoor = local.x - ZoneBuilder.KitchenDoorX, jamb = .52f, glass = .53f;
            Assert.Greater(Mathf.Abs(kitchenDoor - rooms), 2 * jamb, "The rooms door and the kitchen door stand apart.");
            foreach (var x in windows.Except(covered))
            {
                Assert.Greater(Mathf.Abs(x - rooms), jamb + glass, "The rooms door clears the window at " + x);
                Assert.Greater(Mathf.Abs(x - kitchenDoor), jamb + glass, "The kitchen door clears the window at " + x);
            }
            Assert.Greater(rooms - jamb, plan.bar.x + .05f + (plan.barSize.x + .2f) / 2, "The rooms door clears the bar top.");
            Assert.Less(kitchenDoor + jamb, w / 2 - 1.05f, "The kitchen door clears the hearth.");
        }

        [Test] public void Every_workshop_owner_is_a_villager()
        {
            var z = Oakhaven;
            // Everyone Oakhaven spawns who works: villagers by the default names, and residents who work (the innkeeper).
            var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : Crulanda.Encounter.VillageLife.DefaultNames;
            var folk = new HashSet<string>(); for (int i = 0; i < z.life.villagers; i++) folk.Add(names[i % names.Length]);
            foreach (var r in z.life.residents.Where(r => r != null && r.works)) folk.Add(r.name);
            Assert.Contains("Hob Linden", folk.ToList(), "The innkeeper is one of the village.");
            var hob = z.life.residents.Single(r => r != null && r.name == "Hob Linden");
            Assert.AreEqual("innkeeper", hob.role); Assert.AreEqual("Innkeeper", hob.title); Assert.IsTrue(hob.works, "Hob works the inn's day; he does not keep a post.");
            Assert.AreEqual(2, System.Array.IndexOf(z.life.residents, hob), "Hob comes third, after Quill and Warden Ivel (a resident's place in the list seeds their look).");
            Assert.Greater(z.life.workshops.Length, 0, "Oakhaven names its workshops.");
            var owners = new Dictionary<string, string>();
            foreach (var w in z.life.workshops)
            {
                Assert.IsTrue(folk.Contains(w.who), "The workshop '" + w.prop + "' belongs to '" + w.who + "', who is one of the village.");
                Assert.AreEqual(1, z.props.Count(p => p != null && p.name == w.prop), "'" + w.prop + "' is one prop of the zone.");
                Assert.IsFalse(owners.ContainsKey(w.prop), "'" + w.prop + "' has one owner (" + (owners.ContainsKey(w.prop) ? owners[w.prop] : "") + " and " + w.who + ").");
                owners[w.prop] = w.who;
            }
            // Each new workshop has its trade, and each trade with stands of its own keeps them.
            foreach (var (prop, who) in new[] { ("Tanner's leather shop", "Maud Tanner"), ("Lisbet's drying hut", "Lisbet Crane"), ("The Cask's kitchen", "Hob Linden"), ("The Golden Cask", "Hob Linden"), ("Moss's game rack", "Garet Moss"),
                ("Produce stall", "Ama Rusk"), ("Cloth and pots", "Tamsin Reed"), ("Bread stall", "Hedda Thorne"), ("Thorne's bakehouse", "Hedda Thorne"), ("Vell's smithy", "Brannoc Vell") })
                Assert.AreEqual(who, owners.TryGetValue(prop, out var o) ? o : null, prop + " is " + who + "'s.");
            // The Golden Cask's household is the innkeeper's: Hob at its head, Quill and Mira lodging.
            var cask = z.life.households.Single(h => h.house == "The Golden Cask");
            Assert.AreEqual("Hob Linden", cask.members.Single(m => m.kin == "head").name);
            CollectionAssert.AreEquivalent(new[] { "Quill", "Mira" }, cask.members.Where(m => m.kin == "lodger").Select(m => m.name).ToArray());
        }

        /// <summary>The innkeeper sells bread and cheese across the bar: the role-keyed vendor entry in items.json.</summary>
        [Test] public void The_innkeeper_sells_bread_and_cheese()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var db = Crulanda.Encounter.ItemDatabase.Parse(Directory.GetFiles(dir, "*.json").Select(File.ReadAllText).ToList());
            // And two cook's leaves (trades.json's vendors, the crafting branch): the trainers and vendors sell recipe leaves by design.
            CollectionAssert.AreEquivalent(new[] { "food.brown_loaf", "food.harrow_cheese", "leaf.cask_stew", "leaf.harvest_feast" }, db.StockFor("Hob Linden", "innkeeper", 2), "Hob sells bread and cheese, and two cook's leaves.");
            foreach (var id in new[] { "food.brown_loaf", "food.harrow_cheese" }) Assert.IsNotNull(db.Get(id), id + " is an item.");
        }

        [Test] public void New_props_keep_clear_of_groves_and_the_creek()
        {
            var z = Oakhaven; var problems = new List<string>();
            var added = NewProps.Select(n => z.props.SingleOrDefault(p => p != null && p.name == n)).ToList();
            for (int i = 0; i < NewProps.Length; i++) Assert.IsNotNull(added[i], NewProps[i] + " is in Oakhaven's props, once.");
            // Appended: nothing the zone had before moves down the list (BuildProps draws in list order).
            // (They were the last six until the training field was appended after them, 2026-10-13: only later rounds may follow them.)
            int first = Array.FindIndex(z.props, p => p != null && p.name == NewProps[0]);
            var block = z.props.Skip(first).Take(NewProps.Length).Select(p => p.name).ToArray();
            CollectionAssert.AreEqual(NewProps, block, "The new props are appended together, in order.");
            // Straight after them the training field; after that only rounds appended later still (the open ground's places and
            // the market pieces, 87e0ff83 and 402427bc): none of those is a name from before the trades or from the six.
            var later = z.props.Skip(first + NewProps.Length).ToArray();
            CollectionAssert.AreEqual(new[] { "training_yard", "training_dummy", "mending_dummy" }, later.Take(3).Select(p => p != null ? p.kind : null).ToArray(),
                "After them only what was appended later: the training yard and its dummies first.");
            var before = new HashSet<string>(z.props.Take(first).Where(p => p != null && !string.IsNullOrEmpty(p.name)).Select(p => p.name).Concat(NewProps));
            Assert.IsTrue(later.All(p => p != null && (string.IsNullOrEmpty(p.name) || !before.Contains(p.name))),
                "Nothing after them is a prop from before them, moved down the list.");
            foreach (var p in added)
            {
                string q = p.name + ": "; float half = Mathf.Max(p.size.x, p.size.y) / 2, reach = Reach(p);
                // A grove tree is dropped within max(size)/2 + 5 m of a prop's centre (ZoneBuilder.NearProp), and a tree pushed off a
                // neighbouring trunk can move 2 m more; a giant needs 9 m.
                foreach (var g in z.groves.Where(g => g != null && g.kind != "orchard"))
                    if (ToBox(p.at, g.center, g.size) <= half + (g.kind == "giant" ? 11 : 7)) problems.Add(q + "too near the grove '" + g.name + "' (" + ToBox(p.at, g.center, g.size).ToString("0.0") + " m)");
                // A prop within the creek's width + 10 m of its line (plus its own half size) holds the meander back (ZoneWater.Room).
                foreach (var w in z.water.Where(w => w != null && w.points.Length > 1))
                    if (ToPath(p.at, w.points) <= w.width + 10 + half) problems.Add(q + "near enough to '" + w.name + "' to move it (" + ToPath(p.at, w.points).ToString("0.0") + " m)");
                foreach (var s in z.secrets.Where(s => s != null))
                    if (Vector2.Distance(p.at, s.at) <= 16) problems.Add(q + "within 16 m of the secret '" + s.id + "'");
                foreach (var f in z.fields.Where(f => f != null))
                    if (ToField(p.at, f) <= reach) problems.Add(q + "reaches into the field '" + f.name + "'");
                foreach (var c in z.camps.Where(c => c != null))
                    if (Vector2.Distance(p.at, c.center) <= c.radius + reach) problems.Add(q + "reaches into the camp '" + c.name + "'");
                foreach (var c in z.tallGrass.Where(c => c != null))
                    if (Vector2.Distance(p.at, c.center) <= c.radius + reach) problems.Add(q + "reaches into the tall grass '" + c.name + "'");
                foreach (var r in z.roads.Where(r => r != null && r.points.Length > 1))
                    if (ToPath(p.at, r.points) <= r.width / 2 + reach) problems.Add(q + "stands on '" + r.name + "'");
            }
            // The four workshops carry no size: a sized prop would hold the creek and clear a wider ring of trees.
            foreach (var p in added.Where(p => p.kind != "house")) Assert.AreEqual(Vector2.zero, p.size, p.name + " has no size.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>How far a trade's workshop reaches from its centre (its furthest built part, as ZoneBuilder builds it), for
        /// the field check; zero for a prop that is no workshop.</summary>
        static float WorkshopReach(ZoneProp p)
        {
            switch (p.kind)
            {
                // The vat's rim and the pile of pelts (about 3.1 m out along the frames).
                case "tannery": return 3.3f;
                // The stack's stakes, the chopping block and the scattered split wood.
                case "woodpile": return 2.2f;
                // The plinth, the peel and the fire's woodstack.
                case "oven": return 2.8f;
                // The smithy's footprint corner (ZoneBuilder.Footprint: 2.9 by 2.4).
                case "forge": return 3.8f;
                case "stall": return 2.5f;
                case "leathershop": case "dryhut": case "kitchen": case "gamerack": return Reach(p);
                default: return 0;
            }
        }

        /// <summary>
        /// No trade's workshop (the tannery yard, the woodyard, the bakehouse oven, a smithy, a stall, the leather shop, the drying
        /// hut, the kitchen, the game rack) and no station of a zone's own (a field anvil, bench or cookfire) stands on a field, in
        /// any zone: nothing of it reaches the ploughed ground or stubble, which ZoneBuilder paints a metre past the field's edge.
        /// (Chris, 2026-10-17, "tools in the farmers field": Oakhaven's tannery yard stood in the West furrows.)
        /// </summary>
        [Test] public void No_workshop_or_station_stands_in_a_field()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"); var problems = new List<string>(); int fields = 0;
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(file));
                if (z == null || z.fields == null) continue;
                foreach (var f in z.fields.Where(f => f != null))
                {
                    fields++;
                    // The paint's furrows and stubble fade out a metre past the field's edge (ZoneBuilder.PaintGround).
                    const float paint = 1;
                    foreach (var p in (z.props ?? new ZoneProp[0]).Where(p => p != null && WorkshopReach(p) > 0))
                        if (ToField(p.at, f) < WorkshopReach(p) + paint) problems.Add(z.displayName + ": the " + p.kind + " '" + p.name + "' at " + p.at + " stands in the field '" + f.name + "'");
                    foreach (var s in (z.stations ?? new ZoneStation[0]).Where(s => s != null))
                        if (ToField(s.at, f) < 1.5f + paint) problems.Add(z.displayName + ": the " + s.kind + " '" + s.name + "' at " + s.at + " stands in the field '" + f.name + "'");
                }
            }
            Assert.Greater(fields, 0, "Some zone has fields to check against.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>
        /// Play note 252 (2026-10-19, "good for lumberjack in myassets - Wooden Canopy"): the canopy goes on one woodyard only, the
        /// lumberjack's (Oakhaven's Woodyard; no other zone has a lumberjack: Khaven's six villagers stop short of the role, the Lowtowns
        /// have no woods to cut), and every corner and edge of its ground keeps off the fields' paint, the roads and paths, the water,
        /// the camps and the tall grass, and clear of every other prop's middle.
        /// </summary>
        [Test] public void The_woodyard_canopy_stands_off_paths_and_fields()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"); var with = new List<string>();
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var zz = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(file));
                foreach (var p in (zz?.props ?? new ZoneProp[0]).Where(p => p != null && p.kind == "woodpile" && p.variant == ZoneBuilder.WoodyardCanopyVariant)) with.Add(zz.id + "/" + p.name);
            }
            CollectionAssert.AreEqual(new[] { "zone.oakhaven/Woodyard" }, with, "the canopy is on Oakhaven's Woodyard alone");
            var z = Oakhaven; var yard = z.props.Single(p => p != null && p.name == "Woodyard");
            Assert.AreEqual("Osk Farrow", z.life.workshops.Single(w => w.prop == "Woodyard").who, "the Woodyard is a working yard");
            // The canopy's ground in zone space: its corners, edge middles and middle, through the yard's turn.
            var turn = Quaternion.Euler(0, yard.rotation, 0); var pts = new List<Vector2>(); var half = ZoneBuilder.WoodyardCanopyFoot / 2;
            for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++)
            {
                var w = turn * (ZoneBuilder.WoodyardCanopyAt + new Vector3(i * half.x, 0, j * half.y)); pts.Add(yard.at + new Vector2(w.x, w.z));
            }
            var problems = new List<string>();
            foreach (var q in pts)
            {
                foreach (var f in z.fields.Where(f => f != null)) if (ToField(q, f) < 1.5f) problems.Add("in the field '" + f.name + "' at " + q);
                foreach (var r in z.roads.Where(r => r != null && r.points.Length > 1)) if (ToPath(q, r.points) < r.width / 2 + 1) problems.Add("on '" + r.name + "' at " + q);
                foreach (var w in z.water.Where(w => w != null && w.points.Length > 1)) if (ToPath(q, w.points) < w.width / 2 + 3) problems.Add("in '" + w.name + "' at " + q);
                foreach (var c in z.camps.Where(c => c != null)) if (Vector2.Distance(q, c.center) < c.radius + 1) problems.Add("in the camp '" + c.name + "' at " + q);
                foreach (var c in z.tallGrass.Where(c => c != null)) if (Vector2.Distance(q, c.center) < c.radius) problems.Add("in the tall grass '" + c.name + "' at " + q);
            }
            // No other prop's middle under it or within 2 m of it (a house's or barn's own half size added).
            foreach (var p in z.props.Where(p => p != null && p != yard && p.kind != "wall"))
            {
                var local = Quaternion.Inverse(turn) * new Vector3(p.at.x - yard.at.x, 0, p.at.y - yard.at.y) - ZoneBuilder.WoodyardCanopyAt;
                float gap = ToBox(new Vector2(local.x, local.z), Vector2.zero, ZoneBuilder.WoodyardCanopyFoot) - Mathf.Max(p.size.x, p.size.y) / 2;
                if (gap < 2) problems.Add("the " + p.kind + " '" + p.name + "' at " + p.at + " is " + gap.ToString("0.0") + " m from it");
            }
            Assert.IsEmpty(problems, "The Woodyard's canopy:\n" + string.Join("\n", problems));
            // The lumberjack's block stays in the open in front of it, not under it.
            var stand = new Vector3(1.4f, 0, -1.4f) - ZoneBuilder.WoodyardCanopyAt;
            Assert.Greater(ToBox(new Vector2(stand.x, stand.z), Vector2.zero, ZoneBuilder.WoodyardCanopyFoot), .3f, "the chopping block's stand is outside the shelter");
            Assert.Less(ToBox(new Vector2(stand.x, stand.z), Vector2.zero, ZoneBuilder.WoodyardCanopyFoot), 1.5f, "and close by it");
            // Each firewood stack under the roof (its 1.32 x 1.1 m inside the footprint).
            foreach (var f in ZoneBuilder.WoodyardFirewood)
            {
                var o = new Vector2(f.x - ZoneBuilder.WoodyardCanopyAt.x, f.y - ZoneBuilder.WoodyardCanopyAt.z);
                Assert.LessOrEqual(Mathf.Abs(o.x) + .7f, half.x, "a firewood stack at " + f + " is under the roof (x)");
                Assert.LessOrEqual(Mathf.Abs(o.y) + .7f, half.y, "a firewood stack at " + f + " is under the roof (z)");
            }
        }

        /// <summary>The Wooden Canopy's prefabs load from Resources, on the pack's own Built-in Standard materials, their maps capped at
        /// 1024 (Editor/ThirdPartyImport); placed as the Woodyard places it the shelter is 3 m tall over the ground WoodyardCanopyFoot
        /// says, and upright: its ridge (the top fifth) is narrow, its posts (the bottom fifth) stand wide.</summary>
        [Test] public void The_wooden_canopy_loads_upright_on_Standard_materials()
        {
            foreach (var path in new[] { "WoodenCanopy/Prefab/Canopy", "WoodenCanopy/Prefab/FireWood" })
            {
                var src = Resources.Load<GameObject>("Props/" + path); Assert.IsNotNull(src, path + " loads");
                foreach (var r in src.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m, path + ": every slot has a material");
                        Assert.AreEqual("Standard", m.shader.name, path + ": " + m.name + " is on the Built-in Standard shader");
                        var tex = m.mainTexture; Assert.IsNotNull(tex, m.name + " has its map");
                        Assert.LessOrEqual(Mathf.Max(tex.width, tex.height), 1024, m.name + "'s map is capped at 1024");
                    }
            }
            var holder = new GameObject("canopy test");
            try
            {
                var go = ZoneBuilder.ModelProp(holder.transform, "WoodenCanopy/Prefab/Canopy", Vector3.zero, 0, ZoneBuilder.WoodyardCanopyHeight);
                Assert.IsNotNull(go);
                var b = ZoneBuilder.PropBounds(holder.transform);
                Assert.AreEqual(ZoneBuilder.WoodyardCanopyHeight, b.size.y, .05f, "3 m tall");
                Assert.AreEqual(ZoneBuilder.WoodyardCanopyFoot.x, b.size.x, .2f, "its long side along x");
                Assert.AreEqual(ZoneBuilder.WoodyardCanopyFoot.y, b.size.z, .2f, "its short side along z");
                Assert.AreEqual(0, b.min.y, .02f, "on the ground");
                // Upright (lane B frame oakhaven-15-woodyard, 2026-10-19: it lay on its side, one roof slope on the grass): the posts'
                // feet, the lowest tenth, stand on the ground in two rows along the whole length (x) and most of the depth (z) apart,
                // with open ground between the rows; the ridge, the top tenth, runs the whole length and is narrow.
                var feet = new List<Vector3>(); var ridge = new List<Vector3>();
                foreach (var mf in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.IsTrue(mf.sharedMesh.isReadable, "the mesh is readable (ThirdPartyImport)");
                    var m = holder.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    foreach (var v in mf.sharedMesh.vertices)
                    {
                        var p = m.MultiplyPoint3x4(v);
                        if (p.y < b.min.y + b.size.y * .1f) feet.Add(p);
                        if (p.y > b.max.y - b.size.y * .1f) ridge.Add(p);
                    }
                }
                Assert.Greater(feet.Count, 8, "posts reach the ground");
                float Span(List<Vector3> l, Func<Vector3, float> f) { return l.Max(f) - l.Min(f); }
                string Say(List<Vector3> l) { return "x " + Span(l, p => p.x).ToString("0.00") + ", z " + Span(l, p => p.z).ToString("0.00") + " m"; }
                Assert.Greater(Span(feet, p => p.x), b.size.x * .8f, "the post feet stand along the whole length (" + Say(feet) + ")");
                Assert.Greater(Span(feet, p => p.z), b.size.z * .6f, "in two rows across the depth (" + Say(feet) + ")");
                Assert.IsFalse(feet.Any(p => Mathf.Abs(p.z - b.center.z) < b.size.z * .2f), "nothing touches the ground down the middle: the space under the roof is open");
                Assert.Greater(Span(ridge, p => p.x), b.size.x * .8f, "the ridge runs the length on top (" + Say(ridge) + ")");
                Assert.Less(Span(ridge, p => p.z), b.size.z * .3f, "and is narrow: the roof is up, not on its side (" + Say(ridge) + ")");
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
            // The firewood stacks: low and flat on the ground, as the Woodyard sets them (.56 m high).
            var stack = new GameObject("firewood test");
            try
            {
                Assert.IsNotNull(ZoneBuilder.ModelProp(stack.transform, "WoodenCanopy/Prefab/FireWood", Vector3.zero, 0, .56f));
                var s = ZoneBuilder.PropBounds(stack.transform);
                Assert.AreEqual(.56f, s.size.y, .03f, "a stack .56 m high"); Assert.AreEqual(0, s.min.y, .02f, "on the ground");
                Assert.Greater(Mathf.Min(s.size.x, s.size.z), 1f, "and over a metre each way: lying flat, not on its end (" + s.size.ToString("0.00") + ")");
            }
            finally { UnityEngine.Object.DestroyImmediate(stack); }
        }
    }
}
