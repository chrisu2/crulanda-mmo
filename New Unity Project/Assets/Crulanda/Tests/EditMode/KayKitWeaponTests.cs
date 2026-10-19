using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The drawn weapons and shields as KayKit models (play note 245, "weapons . KayKit : Fantasy Weapons Bits"; KayKitWeapons,
    /// ActorVisual.KayKitWeapons.cs): every drawn main-hand and off-hand variant has a model or an explicit fallback to its drawing;
    /// every model the map names is in Resources/Props/KayKitWeapons and loads with a readable mesh, and the atlas can be read to
    /// tint it; each model, laid in its frame, is as long as its family's drawing was (within 15%), lies along the frame's +Y
    /// (a shield faces +Y, its height along X), and the fist closes round its grip: the frame's origin is its pivot (within 3 cm),
    /// and the mesh's cross-section through the origin is a handle centred on the hand's axis, narrow enough to hold. On a figure
    /// the held piece and its put-away copy are the model, a fallback stays drawn, and the Ranger holds the strung KayKit bow.
    /// The tinted atlas changes the steel and wood swatches by palette and nothing else. The wardrobe line-up shows every
    /// mapped weapon. Measured, not looked at: the captures are WardrobeCapture's "19-kaykit" shots.
    /// </summary>
    public class KayKitWeaponTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        bool models;
        [SetUp] public void SmoothFigures() { models = ActorVisual.Models; ActorVisual.Models = false; }
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; }

        static IEnumerable<GearFamily> HandFamilies { get { return GearLooks.Families.Where(f => (f.slot == "mainhand" || f.slot == "offhand") && !f.name.StartsWith("model.")); } }

        [Test] public void Every_drawn_weapon_and_shield_variant_has_a_model_or_an_explicit_fallback()
        {
            var keys = new HashSet<string>();
            foreach (var f in HandFamilies)
                foreach (var v in f.variants)
                {
                    string key = f.name + ":" + v; keys.Add(key);
                    Assert.IsTrue(KayKitWeapons.Map.ContainsKey(key), key + " has a KayKit model or an explicit \"\" (its drawing).");
                    if (KayKitWeapons.ModelFor(f.name, v) != null) Assert.IsTrue(KayKitWeapons.Lengths.ContainsKey(f.name), f.name + " has its drawn length.");
                }
            foreach (var key in KayKitWeapons.Map.Keys) Assert.IsTrue(keys.Contains(key), key + " is a drawn hand variant (no stale entries).");
            // The PBR steel (play note 249): only for variants that wear a KayKit model below rare, and only at rare and epic.
            foreach (var e in KayKitWeapons.Pbr)
            {
                var fv = e.Key.Split(':');
                Assert.IsNotNull(KayKitWeapons.ModelFor(fv[0], fv[1]), e.Key + " wears a KayKit model below rare.");
                Assert.AreEqual(e.Value, KayKitWeapons.ModelFor(fv[0], fv[1], KayKitWeapons.PbrQuality), e.Key + " wears " + e.Value + " at rare.");
                Assert.AreEqual(e.Value, KayKitWeapons.ModelFor(fv[0], fv[1], 4), e.Key + " and at epic.");
                Assert.AreNotEqual(e.Value, KayKitWeapons.ModelFor(fv[0], fv[1], 2), e.Key + ": not at uncommon.");
                if (KayKitWeapons.KindOf(fv[0]) == KayKitWeapons.Kind.Weapon) Assert.IsTrue(KayKitWeapons.GripAlong(e.Value).HasValue, e.Value + " has its measured grip.");
            }
            // The Sky Den axes (play note 250): every axe family's variant wears one at every quality but rare (the PBR war axe for
            // bearded and crescent), the level band and the zone's palette choosing which; all twenty are used, the cleaver stays KayKit's.
            var axes = new HashSet<string>();
            foreach (var f in new[] { "axe.hand", "axe.bearded", "axe.crescent" })
                foreach (var v in GearLooks.Family(f).variants)
                    for (int q = 0; q <= 4; q++) for (int t = 0; t <= 5; t++) foreach (var p in new[] { "oakhaven", "khaven" })
                    {
                        var m = KayKitWeapons.ModelFor(f, v, q, t, p); string what = f + ":" + v + " q" + q + " band " + t + " " + p;
                        if (q == 3 && f != "axe.hand") { Assert.AreEqual("pbr/Axe", m, what + ": the PBR war axe"); continue; }
                        Assert.IsTrue(KayKitWeapons.IsAxes(m), what + " is a Sky Den axe (" + m + ")"); axes.Add(m);
                        Assert.AreEqual(KayKitWeapons.AxeGrip, KayKitWeapons.GripAlong(m), what + ": its grip measured up the haft");
                    }
            Assert.AreEqual(20, axes.Count, "all twenty axes are worn by something: " + string.Join(", ", axes.OrderBy(a => a)));
            Assert.AreEqual("axe_C", KayKitWeapons.ModelFor("cleaver", "slab", 4, 5, "khaven"), "the cleaver keeps KayKit's broad blade");
            int mapped = KayKitWeapons.Map.Values.Count(m => m.Length > 0);
            TestContext.WriteLine(mapped + " of " + keys.Count + " variants wear a KayKit model.");
            Assert.GreaterOrEqual(mapped, 40, "Most weapons and shields wear a model; the drawings are kept only where the look is the point.");
        }

        [Test] public void Every_mapped_model_loads_with_a_readable_mesh_and_the_atlas_reads()
        {
            foreach (var m in KayKitWeapons.Models)
            {
                var src = ActorVisual.LoadWeaponModel(m);
                Assert.NotNull(src, m + " is in Resources/" + KayKitWeapons.PathOf(m) + (KayKitWeapons.NodeOf(m) != null ? " (node " + KayKitWeapons.NodeOf(m) + ")" : ""));
                if (KayKitWeapons.IsAxes(m)) foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true)) foreach (var mat in r.sharedMaterials)
                {
                    Assert.NotNull(mat, m + ": the FBX's own materials are remapped to the pack's"); Assert.AreEqual("Standard", mat.shader.name, m + ": on the built-in Standard shader");
                    StringAssert.StartsWith("Axe-P", mat.name, m + ": the pack's Axe-P1 or Axe-P2 material");
                }
                if (KayKitWeapons.IsPbr(m))
                {
                    var mat = Resources.Load<Material>(KayKitWeapons.MaterialOf(m)); Assert.NotNull(mat, m + "'s own material");
                    Assert.AreEqual("Standard", mat.shader.name, m + ": rebuilt on the built-in Standard shader (the pack's were URP Lit)");
                    Assert.NotNull(mat.mainTexture, m + ": its albedo"); Assert.NotNull(mat.GetTexture("_BumpMap"), m + ": its normal map"); Assert.NotNull(mat.GetTexture("_MetallicGlossMap"), m + ": its metal and smoothness");
                }
                var filters = src.GetComponentsInChildren<MeshFilter>(true);
                Assert.AreEqual(1, filters.Length, m + " is one mesh, as the pack builds them.");
                Assert.NotNull(filters[0].sharedMesh, m + " has its mesh"); Assert.IsTrue(filters[0].sharedMesh.isReadable, m + " is readable (the bow's string is cut from it, the tests measure it).");
                Assert.IsTrue(ActorVisual.KayKitBounds(src, out var b) && b.size.magnitude > .1f, m + " has a size.");
            }
            var atlas = Resources.Load<Material>(KayKitWeapons.Atlas);
            Assert.NotNull(atlas, "the weapons' atlas material"); var tex = atlas.mainTexture as Texture2D;
            Assert.NotNull(tex, "its texture"); Assert.IsTrue(tex.isReadable, "readable, to tint by palette (ThirdPartyImport)");
        }

        /// <summary>Every vertex of the placed model, in the frame's space.</summary>
        static List<Vector3> FrameVertices(Transform frame, Transform model)
        {
            var list = new List<Vector3>();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = frame.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices) list.Add(m.MultiplyPoint3x4(v));
            }
            return list;
        }
        /// <summary>Where the mesh's triangle edges cross the plane y = 0 of the frame: its cross-section there.</summary>
        static List<Vector3> Section(Transform frame, Transform model)
        {
            var pts = new List<Vector3>();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = frame.worldToLocalMatrix * mf.transform.localToWorldMatrix; var mesh = mf.sharedMesh;
                var v = mesh.vertices.Select(p => m.MultiplyPoint3x4(p)).ToArray(); var tri = mesh.triangles;
                for (int i = 0; i + 2 < tri.Length; i += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        Vector3 a = v[tri[i + e]], b = v[tri[i + (e + 1) % 3]];
                        if ((a.y < 0) == (b.y < 0)) continue;
                        pts.Add(Vector3.Lerp(a, b, a.y / (a.y - b.y)));
                    }
            }
            return pts;
        }
        static float Radius(Vector3 p) { return new Vector2(p.x, p.z).magnitude; }

        [Test] public void Each_model_is_held_at_its_familys_length_with_the_fist_round_its_grip()
        {
            // Every model every variant can wear (KayKit's, the PBR steel at rare and epic, the Sky Den axes by band and palette).
            var checks = KayKitWeapons.Wearable.Select(w => (w.family + ":" + w.variant + " q" + w.quality + " band " + w.tier + " " + w.palette, w.model, KayKitWeapons.KindOf(w.family), KayKitWeapons.LengthOf(w.family))).ToList();
            checks.Add(("the Ranger's bow", KayKitWeapons.ClassBow, KayKitWeapons.Kind.Bow, KayKitWeapons.BowLength));
            foreach (var (what, model, kind, length) in checks)
            {
                var frame = new GameObject("Frame " + what).transform; made.Add(frame.gameObject);
                var t = ActorVisual.PlaceKayKit(frame, model, kind, length); Assert.NotNull(t, what + ": " + model + " is placed.");
                var verts = FrameVertices(frame, t); var b = new Bounds(verts[0], Vector3.zero); foreach (var p in verts) b.Encapsulate(p);
                string say = what + " (" + model + ", bounds " + b.min.ToString("0.00") + " to " + b.max.ToString("0.00") + ")";
                if (!KayKitWeapons.GripAlong(model).HasValue)   // a PBR or Sky Den weapon's grip is measured (its pivot is elsewhere): the cross-section below is its check
                    Assert.Less(frame.InverseTransformPoint(t.position).magnitude, .03f, say + ": its pivot, the grip, is at the frame's origin.");
                if (kind == KayKitWeapons.Kind.Shield)
                {
                    Assert.Less(b.size.y, Mathf.Min(b.size.x, b.size.z), say + ": lies flat on the forearm, its face toward +Y (outward).");
                    Assert.That(b.size.x, Is.InRange(length * .85f, length * 1.15f), say + ": its height (up the forearm, +X) is the family's " + length + " m.");
                    Assert.Less(new Vector2(b.center.x, b.center.z).magnitude, .15f, say + ": the handle is behind the middle of the shield.");
                    continue;
                }
                Assert.Greater(b.size.y, Mathf.Max(b.size.x, b.size.z), say + ": lies along the frame's +Y (the forearm's pointing line).");
                Assert.That(b.size.y, Is.InRange(length * .85f, length * 1.15f), say + ": as long as the family's drawing, " + length + " m.");
                Assert.GreaterOrEqual(b.max.y, -b.min.y - .01f, say + ": the head or blade is up, the pommel below the fist (a plain staff is even).");
                var section = Section(frame, t); var near = section.Where(p => Radius(p) < .1f).ToList();
                Assert.Greater(near.Count, 2, say + ": the mesh passes through the fist.");
                var centre = near.Aggregate(Vector3.zero, (s, p) => s + p) / near.Count;
                Assert.Less(Radius(centre), .03f, say + ": the handle is centred on the hand's axis (within 3 cm).");
                if (kind == KayKitWeapons.Kind.Bow) { Assert.Greater(b.max.x, -b.min.x, say + ": the string is toward +X, the archer's body."); continue; }
                if (KayKitWeapons.KnuckleBow.Contains(model))
                {
                    var outer = section.Where(p => Radius(p) >= .06f).ToList();   // the knuckle bow: one bar, on one side of the fist
                    Assert.IsTrue(outer.All(p => p.z > 0) || outer.All(p => p.z < 0) || outer.All(p => p.x > 0) || outer.All(p => p.x < 0), say + ": past the grip only the knuckle bow, on one side of the fist");
                }
                else Assert.Less(section.Max(Radius), .06f, say + ": nothing but the handle at the fist, thin enough to close round.");
                Assert.GreaterOrEqual(b.max.z, -b.min.z - .005f, say + ": the edge or head is toward +Z.");
            }
        }

        [Test] public void On_a_figure_the_held_and_put_away_pieces_are_the_models_and_a_fallback_stays_drawn()
        {
            var looks = GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text); var db = new ItemDatabase();
            string Item(string look, string slot) { string id = "test.kaykit." + db.Items.Count; looks.Register(id, look); db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = slot, quality = 3, level = 8 }; return id; }
            var go = new GameObject("KayKit figure"); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
            var v = ActorVisual.Attach(go, ActorLook.Warrior);
            v.ApplyGearIds(new[] { Item("sword.arming:straight/tollroad", "mainhand"), Item("shield.heater:plain/tollroad", "offhand") }, db, looks);
            // Rare (for the accent): the arming sword is the PBR steel at rare (play note 249), the heater keeps KayKit's heater.
            Assert.AreEqual("pbr/Sword", KayKitWeapons.ModelFor("sword.arming", "straight", 3)); Assert.AreEqual("shield_B", KayKitWeapons.ModelFor("shield.heater", "plain", 3));
            foreach (var (slot, model) in new[] { (EquipSlot.MainHand, "pbr/Sword"), (EquipSlot.OffHand, "shield_B") })
                foreach (bool slung in new[] { false, true })
                {
                    var root = v.GearRoot(slot, slung); Assert.NotNull(root, slot + (slung ? " put away" : " in hand"));
                    Assert.NotNull(root.Find(KayKitWeapons.NodeName(model)), slot + (slung ? " put away" : " in hand") + " is the model " + model + ".");
                    Assert.Greater(root.GetComponentsInChildren<Renderer>(true).Count(r => r.name == ActorVisual.AccentName), 0, "a rare piece keeps its glowing accent on the weapon root");
                }
            v.ApplyGearIds(new[] { Item("knife:glass/pale+glow", "mainhand") }, db, looks);
            var knife = v.GearRoot(EquipSlot.MainHand);
            Assert.IsFalse(knife.GetComponentsInChildren<Transform>(true).Any(c => c.name.StartsWith("KayKit")), "the glass knife keeps its drawing");
            Assert.Greater(knife.GetComponentsInChildren<Renderer>(true).Length, 2, "drawn parts");

            var r = new GameObject("KayKit ranger"); new GameObject("Body").transform.SetParent(r.transform, false); made.Add(r);
            ActorVisual.Attach(r, ActorLook.Ranger);
            Assert.NotNull(r.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == "KayKit " + KayKitWeapons.ClassBow), "the Ranger holds the strung KayKit bow");
        }

        [Test] public void The_tint_moves_the_steel_and_wood_swatches_by_palette_and_nothing_else()
        {
            var tex = Resources.Load<Material>(KayKitWeapons.Atlas).mainTexture as Texture2D;
            var bronze = ActorVisual.TintedAtlas(tex, new Color(.6f, .35f, .2f), new Color(.43f, .27f, .13f));
            var iron = ActorVisual.TintedAtlas(tex, new Color(.75f, .75f, .77f), new Color(.2f, .16f, .16f));
            Assert.NotNull(bronze); Assert.NotNull(iron);
            Color At(Texture2D t, int row, int col) { return t.GetPixelBilinear((col + .5f) / 8, 1 - (row + .5f) / 4); }   // a swatch's middle, from the top-left
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 8; col++)
                {
                    float d = Vector4.Distance(At(bronze, row, col), At(iron, row, col));
                    if (KayKitWeapons.MetalCell(row, col) || KayKitWeapons.WoodCell(row, col)) Assert.Greater(d, .05f, "swatch " + row + "," + col + " follows the palette");
                    else Assert.Less(d, .01f, "swatch " + row + "," + col + " (a wrap, a gem, a painted face) keeps the pack's colour");
                }
            var c = At(bronze, 1, 6); Assert.Greater(c.r, c.b, "a bronze palette's blade is warm, not the pack's blue steel");
            Object.DestroyImmediate(bronze); Object.DestroyImmediate(iron);
        }

        [Test] public void The_wardrobe_line_up_shows_every_mapped_weapon_in_hand_and_put_away()
        {
            var shown = new HashSet<string>(WardrobeCapture.KayKitLooks.Select(l => { GearLooks.TryParseLook(l, out var f, out var v, out _, out _); return f + ":" + v; }));
            foreach (var e in KayKitWeapons.Map) if (e.Value.Length > 0) Assert.IsTrue(shown.Contains(e.Key), e.Key + " is in the KayKit line-up (19-kaykit).");
            var pbr = new HashSet<string>(WardrobeCapture.PbrLooks.Select(l => { GearLooks.TryParseLook(l, out var f, out var v, out _, out _); return f + ":" + v; }));
            foreach (var key in KayKitWeapons.Pbr.Keys) Assert.IsTrue(pbr.Contains(key), key + " is in the PBR line-up (19-kaykit-pbr).");
            Assert.AreEqual(4, WardrobeCapture.KayKitViews.Length, "in hand and put away, each from the front and the side");
        }
    }
}
