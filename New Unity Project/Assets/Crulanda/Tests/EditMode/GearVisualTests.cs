using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Worn gear on the figure (loot steps A1 and A2): a bare Warrior holds nothing, wears nothing and has no pads; a weapon goes
    /// on the right hand and a shield or lantern on the left; armour hangs from the body and the limbs it follows; taking it off
    /// gives the bare body back exactly; swapping gear leaves nothing behind and makes no new materials or meshes; gear put on
    /// while swimming is slung on the back; barbutes and hoods hide the hair, open helms do not; a full kit stays inside the part
    /// budget; every family and variant builds at every quality and level band and sits on the figure; arcs curve and rods are
    /// capped; a pendant hangs in front of a mantle and boots go under greaves; a session with no items keeps the class kit;
    /// the Druid keeps her staff and hood until a piece replaces them; enemies' own weapons and Caddock's crown are untouched.
    /// Built in edit mode on bare figures (no scene, no save).
    /// </summary>
    public class GearVisualTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); }

        ActorVisual Figure(ActorLook look = ActorLook.Warrior, string name = "Gear test figure")
        {
            var go = new GameObject(name); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
            return ActorVisual.Attach(go, look);
        }
        static GearLooks Looks() { return GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text); }
        static int Renderers(Transform t) { return t.GetComponentsInChildren<MeshRenderer>(true).Length; }
        static Transform LeftArm(ActorVisual v) { return v.transform.Find("Body/Arm L"); }
        static readonly EquipSlot[] Armour = { EquipSlot.Head, EquipSlot.Neck, EquipSlot.Shoulders, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Legs, EquipSlot.Feet };
        /// <summary>A generated item whose name has this piece word ("Blade", "Shield", "Lantern").</summary>
        static string GeneratedPiece(ItemDatabase db, string slot, int level, int quality, string piece, int skip = 0)
        {
            for (int seed = 0; seed < 5000; seed++)
            {
                string id = ItemDatabase.GearId(slot, level, quality, seed);
                if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == piece && skip-- <= 0) return id;
            }
            Assert.Fail("No generated " + piece + " found."); return null;
        }
        /// <summary>A whole generated kit, one piece word per slot (head, neck, shoulders, chest, hands, legs, feet, main hand, off hand).</summary>
        static string[] Kit(ItemDatabase db, int level, int quality, params string[] pieces)
        {
            var ids = new string[pieces.Length];
            for (int s = 0; s < pieces.Length; s++) ids[s] = GeneratedPiece(db, ItemDatabase.SlotIds[s], level, quality, pieces[s]);
            return ids;
        }
        static readonly string[] Martial = { "Cap", "Torc", "Pauldrons", "Hauberk", "Gauntlets", "Greaves", "Sabatons", "Blade", "Shield" };
        static readonly string[] Cloth = { "Hood", "Pendant", "Mantle", "Tunic", "Wraps", "Breeches", "Shoes", "Cudgel", "Lantern" };
        /// <summary>Every renderer of the figure that is not gear, with its material, whether it draws, and every transform's place.</summary>
        static (Dictionary<Renderer, (Material, bool, bool)> parts, Dictionary<Transform, (Vector3, Quaternion, Vector3)> places) Bare(ActorVisual v)
        {
            var parts = new Dictionary<Renderer, (Material, bool, bool)>(); var places = new Dictionary<Transform, (Vector3, Quaternion, Vector3)>();
            foreach (var r in v.GetComponentsInChildren<Renderer>(true)) if (!InGear(r.transform)) parts[r] = (r.sharedMaterial, r.enabled, r.gameObject.activeSelf);
            foreach (var t in v.GetComponentsInChildren<Transform>(true)) if (!InGear(t)) places[t] = (t.localPosition, t.localRotation, t.localScale);
            return (parts, places);
        }
        static bool InGear(Transform t) { for (; t != null; t = t.parent) if (t.name.StartsWith("Gear ")) return true; return false; }
        /// <summary>All of a slot's parts: its root on the body and its roots on the limbs.</summary>
        static List<MeshRenderer> SlotParts(ActorVisual v, EquipSlot s)
        {
            var list = new List<MeshRenderer>();
            if (v.GearRoot(s) != null) list.AddRange(v.GearRoot(s).GetComponentsInChildren<MeshRenderer>(true));
            foreach (var t in v.GearLimbRoots(s)) list.AddRange(t.GetComponentsInChildren<MeshRenderer>(true));
            return list;
        }

        [Test] public void Bare_warrior_holds_nothing_and_has_no_pads()
        {
            var v = Figure();
            Assert.AreEqual(6, v.ClassKitParts, "Before gear drives it: two pads, a sword and a shield (two parts each).");
            v.ApplyGearIds(new string[0], new ItemDatabase(), Looks());
            Assert.IsTrue(v.GearDriven);
            Assert.AreEqual(0, v.ClassKitParts, "No pads, no class sword or shield.");
            Assert.AreEqual(0, v.GearPartCount);
            Assert.AreEqual(2, Renderers(v.RightArm), "The right arm is a sleeve and a hand.");
            Assert.AreEqual(2, Renderers(LeftArm(v)), "So is the left.");
            foreach (EquipSlot s in System.Enum.GetValues(typeof(EquipSlot))) { Assert.IsNull(v.GearRoot(s), s + " shows nothing."); Assert.AreEqual(0, v.GearLimbRoots(s).Count); }
            Assert.IsTrue(v.HairShowing, "The hair shows.");
        }

        [Test] public void Equipping_each_slot_adds_parts_under_its_gear_root()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            string blade = GeneratedPiece(db, "mainhand", 5, 2, "Blade"), shield = GeneratedPiece(db, "offhand", 5, 2, "Shield"), lantern = GeneratedPiece(db, "offhand", 5, 2, "Lantern");
            v.ApplyGearIds(new[] { blade, shield }, db, looks);
            Assert.That(v.GearParts(EquipSlot.MainHand), Is.InRange(1, 12), "A blade in the right hand.");
            Assert.AreSame(v.RightArm, v.GearRoot(EquipSlot.MainHand).parent, "It follows the right arm.");
            Assert.That(v.GearParts(EquipSlot.OffHand), Is.InRange(1, 12), "A shield on the left arm.");
            Assert.AreSame(LeftArm(v), v.GearRoot(EquipSlot.OffHand).parent);
            foreach (var s in new[] { EquipSlot.MainHand, EquipSlot.OffHand })
            {
                var back = v.GearRoot(s, true);
                Assert.AreEqual("Body", back.parent.name, s + " has a slung copy on the back.");
                Assert.IsFalse(back.gameObject.activeSelf, "Hidden until you swim."); Assert.IsTrue(v.GearRoot(s).gameObject.activeSelf);
                Assert.AreEqual(v.GearParts(s), Renderers(back), "The slung copy is the same piece.");
            }
            v.ApplyGearIds(new[] { blade, lantern }, db, looks);
            Assert.AreSame(LeftArm(v), v.GearRoot(EquipSlot.OffHand).parent, "A lantern hangs from the left hand.");
            Assert.NotNull(v.GearRoot(EquipSlot.OffHand).GetComponent<GearHang>(), "And keeps hanging straight.");
            Assert.AreEqual("offhand.hung", looks.Resolve(db.Get(lantern)).family);
            // Armour: every slot shows, on the body and, for hands, legs and feet, on both limbs.
            var kit = Kit(db, 5, 2, "Cap", "Pendant", "Mantle", "Tunic", "Gloves", "Breeches", "Boots");
            v.ApplyGearIds(kit, db, looks);
            foreach (var s in Armour)
            {
                Assert.NotNull(v.GearRoot(s), s + " shows."); Assert.AreEqual("Body", v.GearRoot(s).parent.name, s + " has its root on the body.");
                Assert.That(v.GearParts(s), Is.InRange(1, 12), s + ": a few parts, at most 12.");
                Assert.IsNull(v.GearRoot(s, true), s + " is not slung for swimming: armour stays on.");
            }
            var hands = v.GearLimbRoots(EquipSlot.Hands); Assert.AreEqual(2, hands.Count, "A glove on each hand.");
            CollectionAssert.AreEquivalent(new[] { "Arm L", "Arm R" }, new[] { hands[0].parent.name, hands[1].parent.name });
            foreach (var s in new[] { EquipSlot.Legs, EquipSlot.Feet })
            {
                var legs = v.GearLimbRoots(s); Assert.AreEqual(2, legs.Count, s + " on both legs.");
                CollectionAssert.AreEquivalent(new[] { "Leg L", "Leg R" }, new[] { legs[0].parent.name, legs[1].parent.name });
            }
            Assert.That(Renderers(v.RightArm), Is.GreaterThan(2), "The glove is on the right arm, so it swings with it.");
            // Taking the lantern off empties the hand; the chest piece goes too.
            v.ApplyGearIds(new[] { blade }, db, looks);
            Assert.IsNull(v.GearRoot(EquipSlot.Chest)); Assert.IsNull(v.GearRoot(EquipSlot.OffHand), "Taking the lantern off empties the hand.");
            Assert.AreEqual(0, v.GearLimbRoots(EquipSlot.Hands).Count, "No glove roots left behind.");
        }

        [Test] public void Unequipping_restores_the_base_body_materials()
        {
            var db = new ItemDatabase(); var looks = Looks();
            foreach (var look in new[] { ActorLook.Warrior, ActorLook.Druid })
            {
                var v = Figure(look); v.ApplyGearIds(new string[0], db, looks);
                var bare = Bare(v);
                foreach (var pieces in new[] { Martial, Cloth, new[] { "Coif", "Cord", "Spaulders", "Jerkin", "Wraps", "Leggings", "Boots", "Hatchet", "Buckler" } })
                {
                    v.ApplyGearIds(Kit(db, 9, 3, pieces), db, looks);
                    int changed = 0; foreach (var kv in bare.parts) if (kv.Key.sharedMaterial != kv.Value.Item1) changed++;
                    Assert.Greater(changed, 4, look + " in " + string.Join(", ", pieces) + ": the armour recolours what it covers.");
                    v.ApplyGearIds(new string[0], db, looks);
                    foreach (var kv in bare.parts)
                    {
                        Assert.NotNull(kv.Key, "No bare part was destroyed.");
                        Assert.AreSame(kv.Value.Item1, kv.Key.sharedMaterial, look + ": " + kv.Key.name + " has its own material back.");
                        Assert.AreEqual(kv.Value.Item2, kv.Key.enabled, look + ": " + kv.Key.name + " draws as before (the belt comes back).");
                        Assert.AreEqual(kv.Value.Item3, kv.Key.gameObject.activeSelf, look + ": " + kv.Key.name + " shows as before (hair, the Druid's hood).");
                    }
                    foreach (var kv in bare.places)
                    {
                        Assert.That(Vector3.Distance(kv.Value.Item1, kv.Key.localPosition), Is.LessThan(1e-5f), look + ": " + kv.Key.name + " is back in place (tucked hair, the Druid's cloak).");
                        Assert.That(Quaternion.Angle(kv.Value.Item2, kv.Key.localRotation), Is.LessThan(.01f), look + ": " + kv.Key.name + " is turned as before.");
                        Assert.That(Vector3.Distance(kv.Value.Item3, kv.Key.localScale), Is.LessThan(1e-5f), look + ": " + kv.Key.name + " is its own size again.");
                    }
                    Assert.AreEqual(0, v.GearPartCount);
                }
            }
        }

        [Test] public void Swapping_gear_destroys_the_old_parts_and_reuses_materials()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            var a = Kit(db, 7, 3, Martial); var b = Kit(db, 11, 4, Cloth); b[7] = GeneratedPiece(db, "mainhand", 11, 4, "Cudgel"); b[8] = GeneratedPiece(db, "offhand", 11, 1, "Lantern");
            v.ApplyGearIds(a, db, looks); int partsA = v.GearPartCount, allA = v.GetComponentsInChildren<Transform>(true).Length;
            v.ApplyGearIds(b, db, looks); int partsB = v.GearPartCount, allB = v.GetComponentsInChildren<Transform>(true).Length;
            int mats = GearMats.Count, meshes = GearMeshes.Count;
            for (int i = 0; i < 50; i++)
            {
                v.ApplyGearIds(i % 2 == 0 ? a : b, db, looks);
                Assert.AreEqual(i % 2 == 0 ? partsA : partsB, v.GearPartCount, "Swap " + i + ": only the new pieces' parts.");
                Assert.AreEqual(i % 2 == 0 ? allA : allB, v.GetComponentsInChildren<Transform>(true).Length, "Swap " + i + ": nothing left behind.");
            }
            Assert.AreEqual(mats, GearMats.Count, "No new materials after the first wear of each piece.");
            Assert.AreEqual(meshes, GearMeshes.Count, "No new meshes either.");
            // Applying the same gear again rebuilds nothing.
            var root = v.GearRoot(EquipSlot.MainHand); var chest = v.GearRoot(EquipSlot.Chest); v.ApplyGearIds(b, db, looks);
            Assert.AreSame(root, v.GearRoot(EquipSlot.MainHand), "Unchanged slots are kept.");
            Assert.AreSame(chest, v.GearRoot(EquipSlot.Chest), "Armour too.");
            // A neck piece rests on the chest piece: changing the chest rebuilds the neck, changing the boots does not.
            var neck = v.GearRoot(EquipSlot.Neck); var swapFeet = (string[])b.Clone(); swapFeet[6] = GeneratedPiece(db, "feet", 11, 4, "Boots");
            v.ApplyGearIds(swapFeet, db, looks); Assert.AreSame(neck, v.GearRoot(EquipSlot.Neck), "New boots leave the pendant alone.");
            var swapChest = (string[])b.Clone(); swapChest[3] = GeneratedPiece(db, "chest", 11, 4, "Hauberk");
            v.ApplyGearIds(swapChest, db, looks); Assert.AreNotSame(neck, v.GearRoot(EquipSlot.Neck), "A hauberk lifts the pendant onto its collar.");
        }

        [Test] public void Pendant_hangs_in_front_of_a_mantle_and_boots_go_under_greaves()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            float Front(EquipSlot s) { float z = float.MinValue; foreach (var r in SlotParts(v, s)) z = Mathf.Max(z, r.bounds.max.z); return z; }
            float Top(EquipSlot s) { float y = float.MinValue; foreach (var r in SlotParts(v, s)) y = Mathf.Max(y, r.bounds.max.y); return y; }
            // The wardrobe's cloth kit: hood, pendant, mantle and tunic. The drop hangs in front of the mantle, not inside it.
            var kit = Kit(db, 5, 2, "Hood", "Pendant", "Mantle", "Tunic");
            v.ApplyGearIds(kit, db, looks);
            Assert.Greater(Front(EquipSlot.Neck), Front(EquipSlot.Shoulders), "The pendant's drop hangs in front of the mantle.");
            // Taking the mantle off rebuilds the neck piece (it now lies over the hood's cape), and putting it back does too.
            var neck = v.GearRoot(EquipSlot.Neck); var bare = new[] { kit[0], kit[1], kit[3] };
            v.ApplyGearIds(bare, db, looks); Assert.AreNotSame(neck, v.GearRoot(EquipSlot.Neck), "Off with the mantle: the pendant is laid again.");
            neck = v.GearRoot(EquipSlot.Neck); v.ApplyGearIds(kit, db, looks); Assert.AreNotSame(neck, v.GearRoot(EquipSlot.Neck), "And again when it goes back on.");
            // Boots under greaves are rebuilt without their folded top, which would poke through the shin plates.
            var boots = GeneratedPiece(db, "feet", 9, 3, "Boots");
            v.ApplyGearIds(new[] { boots }, db, looks); float alone = Top(EquipSlot.Feet); var feet = v.GearRoot(EquipSlot.Feet);
            v.ApplyGearIds(new[] { GeneratedPiece(db, "legs", 9, 3, "Greaves"), boots }, db, looks);
            Assert.AreNotSame(feet, v.GearRoot(EquipSlot.Feet), "Putting greaves on rebuilds the boots.");
            Assert.Less(Top(EquipSlot.Feet), alone - .02f, "Under greaves the boots lose their folded top (it stood .04 above the shaft).");
        }

        [Test] public void Gear_built_while_swimming_is_stowed_not_held()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            v.Pose = ActorPose.Swim;
            v.ApplyGearIds(new[] { GeneratedPiece(db, "mainhand", 4, 2, "Blade"), GeneratedPiece(db, "offhand", 4, 2, "Shield"), GeneratedPiece(db, "chest", 4, 2, "Hauberk") }, db, looks);
            foreach (var s in new[] { EquipSlot.MainHand, EquipSlot.OffHand })
            {
                Assert.IsFalse(v.GearRoot(s).gameObject.activeSelf, s + ": nothing in the hand while swimming.");
                Assert.IsTrue(v.GearRoot(s, true).gameObject.activeSelf, s + ": slung on the back.");
            }
            Assert.IsTrue(v.GearRoot(EquipSlot.Chest).gameObject.activeSelf, "Armour stays on in the water.");
            v.Pose = ActorPose.None;
            v.ApplyGearIds(new[] { GeneratedPiece(db, "mainhand", 4, 2, "Blade", 1) }, db, looks);
            Assert.IsTrue(v.GearRoot(EquipSlot.MainHand).gameObject.activeSelf, "Out of the water: in the hand.");
            Assert.IsFalse(v.GearRoot(EquipSlot.MainHand, true).gameObject.activeSelf);
        }

        [Test] public void Barbutes_hide_the_hair_and_open_helms_do_not()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();   // variant 0: short hair
            v.ApplyGearIds(new string[0], db, looks); Assert.IsTrue(v.HairShowing);
            var barbute = GeneratedPiece(db, "head", 12, 2, "Cap"); Assert.AreEqual("head.barbute", looks.Resolve(db.Get(barbute)).family, "A level-12 cap is a barbute.");
            v.ApplyGearIds(new[] { barbute }, db, looks); Assert.IsFalse(v.HairShowing, "A barbute hides the hair.");
            foreach (var (piece, level, family) in new[] { ("Hood", 4, "head.hood"), ("Coif", 4, "head.coif") })
            {
                var id = GeneratedPiece(db, "head", level, 2, piece); Assert.AreEqual(family, looks.Resolve(db.Get(id)).family);
                v.ApplyGearIds(new[] { id }, db, looks); Assert.IsFalse(v.HairShowing, family + " covers the hair.");
            }
            foreach (var (level, family) in new[] { (2, "head.cap"), (7, "head.kettle") })
            {
                var id = GeneratedPiece(db, "head", level, 2, "Cap"); Assert.AreEqual(family, looks.Resolve(db.Get(id)).family);
                v.ApplyGearIds(new[] { id }, db, looks); Assert.IsTrue(v.HairShowing, family + " leaves the hair showing below it.");
            }
            foreach (var look in new[] { "head.crown:tin/sandthrone", "head.circlet:band/concord" })
            {
                string id = "test." + look; looks.Register(id, look); db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = "head", quality = 3 };
                v.ApplyGearIds(new[] { id }, db, looks); Assert.IsTrue(v.HairShowing, look + " sits on the hair.");
            }
            v.ApplyGearIds(new string[0], db, looks); Assert.IsTrue(v.HairShowing, "Bare-headed again.");
        }

        [Test] public void Full_kit_stays_under_the_part_budget()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(); var rng = new System.Random(5); int worst = 0; string worstKit = "";
            for (int level = 1; level <= EncounterProgress.LevelCap; level++)
                for (int q = 0; q <= 4; q++)
                    for (int k = 0; k < 4; k++)
                    {
                        var ids = new string[ItemDatabase.SlotIds.Length];
                        for (int s = 0; s < ids.Length; s++) ids[s] = ItemDatabase.GearId(ItemDatabase.SlotIds[s], level, q, rng.Next(10000));
                        v.ApplyGearIds(ids, db, looks);
                        foreach (EquipSlot s in System.Enum.GetValues(typeof(EquipSlot))) Assert.That(v.GearParts(s), Is.InRange(1, 12), ids[(int)s] + " (" + db.Get(ids[(int)s]).name + "): at most 12 parts a slot.");
                        if (v.GearPartCount > worst) { worst = v.GearPartCount; worstKit = string.Join(", ", System.Array.ConvertAll(ids, id => db.Get(id).name)); }
                    }
            foreach (var pieces in new[] { Martial, Cloth })
                for (int q = 0; q <= 4; q++) { v.ApplyGearIds(Kit(db, 13, q, pieces), db, looks); if (v.GearPartCount > worst) { worst = v.GearPartCount; worstKit = string.Join(", ", pieces) + " q" + q; } }
            Assert.LessOrEqual(worst, 70, "A fully dressed figure stays inside 70 gear parts (worst: " + worstKit + ").");
            TestContext.WriteLine("Most gear parts on one figure: " + worst + " (" + worstKit + ").");
        }

        [Test] public void Every_family_and_variant_builds_at_every_quality()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(); var palettes = new List<string>(looks.PaletteIds); int built = 0;
            // Each variant at every quality (its level band rising with it), and at the two extremes: a level-1 epic and a level-13 common.
            var combos = new[] { (0, 1), (1, 3), (2, 6), (3, 9), (4, 12), (4, 1), (1, 12) };
            foreach (var family in GearLooks.Families)
            {
                var slot = (EquipSlot)ItemDatabase.SlotIndex(family.slot); bool hand = slot == EquipSlot.MainHand || slot == EquipSlot.OffHand;
                foreach (var variant in family.variants)
                    foreach (var (q, level) in combos)
                    {
                        string id = "test." + family.name + "." + variant + "." + q + "." + level, look = family.name + ":" + variant + "/" + palettes[built % palettes.Count];
                        looks.Register(id, look); db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = family.slot, quality = q, level = level - 1 };
                        v.ApplyGearIds(new[] { id }, db, looks); built++;
                        var root = v.GearRoot(slot); string what = look + " at quality " + q + ", level " + level;
                        Assert.NotNull(root, what + " shows.");
                        Assert.That(v.GearParts(slot), Is.InRange(hand ? 2 : 1, 12), what + ": a few parts, at most 12.");
                        if (hand) Assert.AreEqual(v.GearParts(slot), Renderers(v.GearRoot(slot, true)), what + ": the slung copy matches.");
                        var parts = SlotParts(v, slot);
                        int lit = 0; foreach (var r in parts) if (r.sharedMaterial != null && r.sharedMaterial.IsKeywordEnabled("_EMISSION")) lit++;
                        if (q >= 3) Assert.GreaterOrEqual(lit, hand ? q - 2 : 1, what + ": rare and epic have glowing accents.");
                        Assert.AreEqual(q == 4, root.GetComponent<GearGlow>() != null, what + ": only epic pulses.");
                        foreach (var r in parts) { var mesh = r.GetComponent<MeshFilter>().sharedMesh; Assert.NotNull(mesh, what + " has every mesh."); Assert.Greater(mesh.vertexCount, 0, what + ": no empty part."); Assert.NotNull(r.sharedMaterial, what + ": every part has a material."); }
                    }
            }
            Assert.AreEqual(146 * combos.Length, built, "146 variants (47 main-hand, 19 off-hand, 80 armour), seven quality and level pairs each.");
        }

        [Test] public void Armour_sits_on_the_figure()
        {
            // Nothing floats: every armour part touches the bare body, or touches a part of the same piece that does.
            var db = new ItemDatabase(); var looks = Looks(); var palettes = new List<string>(looks.PaletteIds); int n = 0;
            foreach (var hair in new[] { 0, 4 })   // short hair, and bald (circlets and crowns sit lower)
            {
                var go = new GameObject("Fit test figure"); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
                var v = ActorVisual.Attach(go, ActorLook.Warrior, hair);
                v.ApplyGearIds(new string[0], db, looks);
                var body = new List<Bounds>(); foreach (var r in v.GetComponentsInChildren<MeshRenderer>(false)) if (!InGear(r.transform)) body.Add(r.bounds);
                foreach (var family in GearLooks.Families)
                {
                    var slot = (EquipSlot)ItemDatabase.SlotIndex(family.slot); if (slot == EquipSlot.MainHand || slot == EquipSlot.OffHand) continue;
                    foreach (var variant in family.variants)
                        foreach (var (q, level) in new[] { (1, 1), (3, 9), (4, 13) })
                        {
                            string id = "fit." + family.name + "." + variant + "." + q + "." + level, look = family.name + ":" + variant + "/" + palettes[n++ % palettes.Count];
                            looks.Register(id, look); db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = family.slot, quality = q, level = level - 1 };
                            v.ApplyGearIds(new[] { id }, db, looks);
                            var parts = SlotParts(v, slot); var anchored = new HashSet<MeshRenderer>();
                            foreach (var p in parts) foreach (var b in body) { var e = b; e.Expand(.08f); if (e.Intersects(p.bounds)) { anchored.Add(p); break; } }
                            for (bool grew = true; grew;)
                            {
                                grew = false;
                                foreach (var p in parts) if (!anchored.Contains(p)) foreach (var a in anchored) { var e = a.bounds; e.Expand(.04f); if (e.Intersects(p.bounds)) { anchored.Add(p); grew = true; break; } }
                            }
                            foreach (var p in parts) Assert.IsTrue(anchored.Contains(p), look + " (quality " + q + ", level " + level + (hair == 4 ? ", bald" : "") + "): " + p.name + " under " + p.transform.parent.name + " floats off the figure at " + p.bounds.center);
                        }
                }
            }
        }

        [Test] public void Arcs_curve_and_rods_are_closed()
        {
            var arc = GearMeshes.Arc("test.crescent", .1f, .19f, 20, 160, .036f);
            Assert.Greater(arc.bounds.max.z, .18f, "A crescent reaches out to its full radius at 90 degrees, not a flat slab between its ends.");
            Assert.GreaterOrEqual(arc.vertexCount, 4 * 4 * 14, "Fourteen steps of 10 degrees, four faces each.");
            Assert.AreEqual(-.018f, arc.bounds.min.y, 1e-4f, "Centred on y 0."); Assert.AreEqual(.018f, arc.bounds.max.y, 1e-4f);
            var rod = GearMeshes.Rod("test.rod", new[] { new Vector3(0, 0, 0), new Vector3(0, .2f, .1f), new Vector3(0, .3f, 0) }, .02f, .015f, 6, 9);
            Assert.AreEqual(8 * 6 * 6 + 2 * 6 * 3, rod.triangles.Length, "The tube's sides and a fan over each end.");
            // A shell's faces all face out of its wall, and a gap leaves the front open.
            var shell = GearMeshes.Shell("test.shell", new[] { new Vector4(.2f, 0, .2f, 0), new Vector4(.2f, .3f, .2f, 0) }, .02f, 2, new[] { 90f }, 90, 16);
            Assert.AreEqual(.2f, shell.bounds.max.x, .002f); Assert.AreEqual(-.2f, shell.bounds.min.z, .002f, "The back is closed.");
            Assert.Less(shell.bounds.max.z, .2f * Mathf.Sin(45 * Mathf.Deg2Rad) + .002f, "The front 90 degrees are open.");
            var normals = shell.normals; var verts = shell.vertices; int outward = 0, inward = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                var flat = new Vector2(verts[i].x, verts[i].z); float r = flat.magnitude; if (Mathf.Abs(normals[i].y) > .3f || r < .001f) continue;   // the walls, not the hem and top bands
                float d = Vector2.Dot(flat / r, new Vector2(normals[i].x, normals[i].z));
                if (r > .195f && d > .9f) outward++; if (r < .185f && d < -.9f) inward++;
            }
            Assert.Greater(outward, 16, "The outside of the wall faces out."); Assert.Greater(inward, 16, "The inside faces in.");
        }

        [Test] public void Legacy_session_without_items_keeps_the_class_kit()
        {
            var v = Figure(); var worn = EncounterSession.FreshProgress().equipment;
            Assert.IsFalse(GearBinder.Dress(v, worn, null, Looks()), "No item database: the binder leaves the figure alone.");
            Assert.IsFalse(v.GearDriven); Assert.AreEqual(6, v.ClassKitParts, "The Warrior keeps the sword, shield and pads.");
            Assert.AreEqual(4, Renderers(v.RightArm), "Sleeve, hand, sword and guard.");
        }

        [Test] public void Druid_staff_yields_to_a_main_hand_item()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(ActorLook.Druid);
            v.ApplyGearIds(new string[0], db, looks);
            Assert.AreEqual(3, v.ClassKitParts, "Empty-handed, the Druid keeps her hood and her staff (two parts).");
            v.ApplyGearIds(new[] { GeneratedPiece(db, "mainhand", 3, 1, "Cudgel") }, db, looks);
            Assert.AreEqual(1, v.ClassKitParts, "A main-hand item replaces the staff; the hood stays.");
            v.ApplyGearIds(new string[0], db, looks);
            Assert.AreEqual(3, v.ClassKitParts, "Unequipped, the staff comes back.");
        }

        [Test] public void Druid_hood_yields_to_a_head_piece_and_her_forms_still_tint_what_shows()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(ActorLook.Druid);
            v.ApplyGearIds(new string[0], db, looks);
            var cloak = v.transform.Find("Body").GetComponentsInChildren<Transform>(true); Vector3 hung = Vector3.zero; Transform cape = null;
            foreach (var t in cloak) if (t.localScale == new Vector3(.52f, .95f, .05f)) { cape = t; hung = t.localPosition; }
            Assert.NotNull(cape, "The Druid's cloak.");
            v.ApplyGearIds(new[] { GeneratedPiece(db, "head", 3, 2, "Cap") }, db, looks);
            Assert.AreEqual(2, v.ClassKitParts, "A cap replaces the hood; the staff stays.");
            v.ApplyGearIds(new[] { GeneratedPiece(db, "chest", 6, 2, "Hauberk") }, db, looks);
            Assert.AreEqual(3, v.ClassKitParts, "Cap off: the hood is back.");
            Assert.Less(cape.localPosition.z, hung.z - .05f, "Over a hauberk the cloak hangs further back, clear of the mail.");
            // A form recolours the Druid's own cloth: the sleeves under a tunic do not change, the bare ones do.
            var sleeve = v.transform.Find("Body/Arm R").GetChild(0).GetComponent<Renderer>();
            v.ApplyGearIds(new[] { GeneratedPiece(db, "chest", 6, 2, "Tunic") }, db, looks);
            var tunic = sleeve.sharedMaterial; var before = tunic.color;
            v.SetClothColor(new Color(.8f, .1f, .1f));
            Assert.AreEqual(before, tunic.color, "The tunic's material is shared gear material and never tinted.");
            v.ApplyGearIds(new string[0], db, looks);
            Assert.AreEqual(new Color(.8f, .1f, .1f), sleeve.sharedMaterial.color, "Bare again, the sleeve shows the form's tint.");
            Assert.That(Vector3.Distance(hung, cape.localPosition), Is.LessThan(1e-5f), "The cloak hangs as it did.");
        }

        [Test] public void Enemy_weapon_and_crown_parts_are_unchanged()
        {
            for (int i = 0; i < 6; i++)
            {
                var deserter = Figure(ActorLook.Deserter, "Deserter " + i);
                Assert.IsTrue(deserter.transform.Find("Body/Arm R/Falchion") != null || deserter.transform.Find("Body/Arm R/Club") != null, "A deserter's falchion or club.");
            }
            var king = Figure(ActorLook.BanditKing, "Caddock");
            Assert.NotNull(king.transform.Find("Body/Tin crown")); Assert.AreEqual(12 + 7 * 2 + 1, king.transform.Find("Body/Tin crown").childCount, "Twelve plates, seven prongs with points, the red glass.");
            Assert.NotNull(king.transform.Find("Body/Arm R/Cleaver")); Assert.AreEqual(8, king.transform.Find("Body/Arm R/Cleaver").childCount);
            foreach (var t in king.GetComponentsInChildren<Transform>(true)) Assert.IsFalse(t.name.StartsWith("Gear "), "No gear roots on enemies.");
            Assert.IsFalse(king.GearDriven);
            // Villagers are not gear-driven either: their bodies are built as before.
            var villager = Figure(ActorLook.Villager, "Villager"); Assert.IsFalse(villager.GearDriven);
            Assert.AreEqual(2, Renderers(villager.RightArm), "A villager's arm is a sleeve and a hand.");
        }
    }
}
