using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Worn gear on the figure, the hand-held half (loot step A1; step A2 adds the armour): a bare Warrior holds nothing and has
    /// no pads; a weapon goes on the right hand and a shield or lantern on the left; swapping gear leaves nothing behind and makes
    /// no new materials or meshes; gear put on while swimming is slung on the back; every main-hand and off-hand family and
    /// variant builds at every quality within the part budget; arcs curve and rods are capped; a session with no items keeps the class kit; enemies' own weapons
    /// and Caddock's crown are untouched. Built in edit mode on bare figures (no scene, no save).
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
            Assert.IsNull(v.GearRoot(EquipSlot.MainHand)); Assert.IsNull(v.GearRoot(EquipSlot.OffHand));
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
            // Armour shows from step A2; until then those slots add nothing.
            v.ApplyGearIds(new[] { blade, ItemDatabase.GearId("chest", 5, 2, 1) }, db, looks);
            Assert.IsNull(v.GearRoot(EquipSlot.Chest)); Assert.IsNull(v.GearRoot(EquipSlot.OffHand), "Taking the lantern off empties the hand.");
        }

        [Test] public void Swapping_gear_destroys_the_old_parts_and_reuses_materials()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            var a = new[] { GeneratedPiece(db, "mainhand", 7, 3, "Hatchet"), GeneratedPiece(db, "offhand", 7, 3, "Shield") };
            var b = new[] { GeneratedPiece(db, "mainhand", 11, 4, "Cudgel"), GeneratedPiece(db, "offhand", 11, 1, "Lantern") };
            v.ApplyGearIds(a, db, looks); int partsA = v.GearPartCount, allA = v.GetComponentsInChildren<Transform>(true).Length;
            v.ApplyGearIds(b, db, looks); int partsB = v.GearPartCount, allB = v.GetComponentsInChildren<Transform>(true).Length;
            int mats = GearMats.Count, meshes = GearMeshes.Count;
            for (int i = 0; i < 50; i++)
            {
                v.ApplyGearIds(i % 2 == 0 ? a : b, db, looks);
                Assert.AreEqual(i % 2 == 0 ? partsA : partsB, v.GearPartCount, "Swap " + i + ": only the new piece's parts.");
                Assert.AreEqual(i % 2 == 0 ? allA : allB, v.GetComponentsInChildren<Transform>(true).Length, "Swap " + i + ": nothing left behind.");
            }
            Assert.AreEqual(mats, GearMats.Count, "No new materials after the first wear of each piece.");
            Assert.AreEqual(meshes, GearMeshes.Count, "No new meshes either.");
            // Applying the same gear again rebuilds nothing.
            var root = v.GearRoot(EquipSlot.MainHand); v.ApplyGearIds(b, db, looks);
            Assert.AreSame(root, v.GearRoot(EquipSlot.MainHand), "Unchanged slots are kept.");
        }

        [Test] public void Gear_built_while_swimming_is_stowed_not_held()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure();
            v.Pose = ActorPose.Swim;
            v.ApplyGearIds(new[] { GeneratedPiece(db, "mainhand", 4, 2, "Blade"), GeneratedPiece(db, "offhand", 4, 2, "Shield") }, db, looks);
            foreach (var s in new[] { EquipSlot.MainHand, EquipSlot.OffHand })
            {
                Assert.IsFalse(v.GearRoot(s).gameObject.activeSelf, s + ": nothing in the hand while swimming.");
                Assert.IsTrue(v.GearRoot(s, true).gameObject.activeSelf, s + ": slung on the back.");
            }
            v.Pose = ActorPose.None;
            v.ApplyGearIds(new[] { GeneratedPiece(db, "mainhand", 4, 2, "Blade", 1) }, db, looks);
            Assert.IsTrue(v.GearRoot(EquipSlot.MainHand).gameObject.activeSelf, "Out of the water: in the hand.");
            Assert.IsFalse(v.GearRoot(EquipSlot.MainHand, true).gameObject.activeSelf);
        }

        [Test] public void Every_family_and_variant_builds_at_every_quality()
        {
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(); var palettes = new List<string>(looks.PaletteIds); int built = 0;
            foreach (var family in GearLooks.Families)
            {
                var slot = family.slot == "mainhand" ? EquipSlot.MainHand : family.slot == "offhand" ? EquipSlot.OffHand : (EquipSlot)(-1);
                if ((int)slot < 0) continue;   // armour: step A2
                foreach (var variant in family.variants)
                    for (int q = 0; q <= 4; q++)
                    {
                        string id = "test." + family.name + "." + variant + "." + q, look = family.name + ":" + variant + "/" + palettes[built % palettes.Count];
                        looks.Register(id, look); db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = family.slot, quality = q };
                        v.ApplyGearIds(new[] { id }, db, looks); built++;
                        var root = v.GearRoot(slot);
                        Assert.NotNull(root, look + " at quality " + q + " shows.");
                        Assert.That(v.GearParts(slot), Is.InRange(2, 12), look + " at quality " + q + ": a few parts, at most 12.");
                        Assert.AreEqual(v.GearParts(slot), Renderers(v.GearRoot(slot, true)), look + ": the slung copy matches.");
                        int lit = 0; foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) if (r.sharedMaterial != null && r.sharedMaterial.IsKeywordEnabled("_EMISSION")) lit++;
                        if (q >= 3) Assert.GreaterOrEqual(lit, q - 2, look + ": rare and epic have glowing accents.");
                        Assert.AreEqual(q == 4, root.GetComponent<GearGlow>() != null, look + ": only epic pulses.");
                        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) Assert.NotNull(r.GetComponent<MeshFilter>().sharedMesh, look + " has every mesh.");
                    }
            }
            Assert.AreEqual((47 + 19) * 5, built, "47 main-hand and 19 off-hand variants, five qualities each.");
        }

        [Test] public void Arcs_curve_and_rods_are_closed()
        {
            var arc = GearMeshes.Arc("test.crescent", .1f, .19f, 20, 160, .036f);
            Assert.Greater(arc.bounds.max.z, .18f, "A crescent reaches out to its full radius at 90 degrees, not a flat slab between its ends.");
            Assert.GreaterOrEqual(arc.vertexCount, 4 * 4 * 14, "Fourteen steps of 10 degrees, four faces each.");
            Assert.AreEqual(-.018f, arc.bounds.min.y, 1e-4f, "Centred on y 0."); Assert.AreEqual(.018f, arc.bounds.max.y, 1e-4f);
            var rod = GearMeshes.Rod("test.rod", new[] { new Vector3(0, 0, 0), new Vector3(0, .2f, .1f), new Vector3(0, .3f, 0) }, .02f, .015f, 6, 9);
            Assert.AreEqual(8 * 6 * 6 + 2 * 6 * 3, rod.triangles.Length, "The tube's sides and a fan over each end.");
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
            Assert.AreEqual(1, v.ClassKitParts, "A main-hand item replaces the staff; the hood stays until a helm (step A2).");
            v.ApplyGearIds(new string[0], db, looks);
            Assert.AreEqual(3, v.ClassKitParts, "Unequipped, the staff comes back.");
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
        }
    }
}
