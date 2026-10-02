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
            // ones its lean-to would cover, and only those (two, at 0.7 and 3.3).
            var windows = new List<float>(); for (float x = -w / 2 + 1.5f; x < w / 2 - .8f; x += 2.6f) windows.Add(x);
            var covered = windows.Where(x => Mathf.Abs(x - local.x) < ZoneBuilder.KitchenSize.x / 2 + .85f).ToList();
            CollectionAssert.AreEqual(new[] { .7f, 3.3f }, covered.Select(x => Mathf.Round(x * 10) / 10).ToArray(), "The kitchen covers the two back windows the plan gives up, no others.");
            // The doors on the inner back wall: the rooms door and the kitchen's door, each clear of the other, of every window
            // left, of the bar top and of the hearth.
            float rooms = -w / 2 + 5.4f, kitchenDoor = local.x - ZoneBuilder.KitchenDoorX, jamb = .52f, glass = .53f;
            Assert.Greater(Mathf.Abs(kitchenDoor - rooms), 2 * jamb, "The rooms door and the kitchen door stand apart.");
            foreach (var x in windows.Except(covered))
            {
                Assert.Greater(Mathf.Abs(x - rooms), jamb + glass, "The rooms door clears the window at " + x);
                Assert.Greater(Mathf.Abs(x - kitchenDoor), jamb + glass, "The kitchen door clears the window at " + x);
            }
            Assert.Greater(rooms - jamb, -w / 2 + 2.6f + 2.1f, "The rooms door clears the bar top.");
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
            CollectionAssert.AreEquivalent(new[] { "food.brown_loaf", "food.harrow_cheese" }, db.StockFor("Hob Linden", "innkeeper", 2), "Hob sells bread and cheese.");
            foreach (var id in new[] { "food.brown_loaf", "food.harrow_cheese" }) Assert.IsNotNull(db.Get(id), id + " is an item.");
        }

        [Test] public void New_props_keep_clear_of_groves_and_the_creek()
        {
            var z = Oakhaven; var problems = new List<string>();
            var added = NewProps.Select(n => z.props.SingleOrDefault(p => p != null && p.name == n)).ToList();
            for (int i = 0; i < NewProps.Length; i++) Assert.IsNotNull(added[i], NewProps[i] + " is in Oakhaven's props, once.");
            // Appended: nothing the zone had before moves down the list (BuildProps draws in list order).
            var tail = z.props.Skip(z.props.Length - NewProps.Length).Select(p => p.name).ToArray();
            CollectionAssert.AreEqual(NewProps, tail, "The new props are the last six, in order.");
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
    }
}
