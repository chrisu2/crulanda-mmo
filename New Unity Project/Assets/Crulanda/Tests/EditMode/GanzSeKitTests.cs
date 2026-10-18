using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Phase 8.5, step 1 (ROADMAP "Phase 8.5 plan": people on GanzSe's FREE Modular Character, Chris's pick "GanzSe (Recommended)"):
    /// the switch is off by default; every armour family is either mapped to a piece the file holds or kept drawn on purpose; cloth,
    /// leather, mail and plate read as different pieces; every look's and trade's own clothes are pieces the file holds; the file as
    /// imported holds exactly the pieces the table names, on a Humanoid avatar whose hips are spine_01; the import rule's palette
    /// settings; and, with the switch on, a man is built on the pack's body, wears its pieces for his gear (no drawn armour doubling
    /// them) and takes them off again, while a woman and an orc keep the Quaternius figure.
    /// </summary>
    public class GanzSeKitTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        bool models, ganz;
        [SetUp] public void Save() { models = ActorVisual.Models; ganz = GanzSeKit.On; }
        [TearDown] public void Restore() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; GanzSeKit.On = ganz; }

        static readonly HashSet<string> Pieces = new HashSet<string>(GanzSeKit.AllPieces);
        static readonly string[] ArmourSlots = { "head", "chest", "hands", "legs", "feet" };
        static GearLook Look(string family, string variant, Color cloth, Color metal, int quality = 1)
        {
            return new GearLook { family = family, variant = variant, cloth = cloth, cloth2 = cloth, leather = new Color(.4f, .27f, .16f), metal = metal, quality = quality, hasTrim = quality >= 1, trimColor = quality >= 4 ? GearLooks.Gold : metal };
        }
        static EquipSlot SlotOf(string slot) { return (EquipSlot)ItemDatabase.SlotIndex(slot); }

        [Test] public void The_switch_is_off_by_default()
        {
            Assert.IsFalse(GanzSeKit.DefaultOn, "The game is unchanged until the lead turns the kit on after a wardrobe capture.");
            if (!System.Environment.GetCommandLineArgs().Contains(GanzSeKit.Flag)) Assert.IsFalse(ganz, "Off unless " + GanzSeKit.Flag + " is given.");
            Assert.IsFalse(GanzSeKit.Women, "Women keep the Quaternius figure (the pack has only a man's body).");
        }

        [Test] public void Every_armour_family_is_a_piece_in_the_file_or_kept_drawn()
        {
            var steel = new Color(.62f, .63f, .66f);
            foreach (var f in GearLooks.Families.Where(x => ArmourSlots.Contains(x.slot)))
            {
                Assert.IsTrue(GanzSeKit.Listed(f.name), f.name + " is in the table (mapped, or kept drawn on purpose).");
                foreach (var v in f.variants)
                {
                    var w = GanzSeKit.WeightOf(f.name, v); bool mapped = GanzSeKit.Map(SlotOf(f.slot), Look(f.name, v, new Color(.5f, .2f, .6f), steel), out var piece, out var belt);
                    Assert.AreEqual(w != GanzSeKit.Weight.None, mapped, f.name + ":" + v + " maps when it has a weight");
                    if (!mapped) continue;
                    Assert.IsTrue(Pieces.Contains(piece), f.name + ":" + v + " -> " + piece + " is a piece in the file.");
                    Assert.AreEqual(GanzSeKit.SlotFor(SlotOf(f.slot)), GanzSeKit.SlotOf(piece), f.name + ": the piece is for its slot.");
                    if (f.slot == "chest") Assert.IsTrue(Pieces.Contains(belt), f.name + ":" + v + ": its belt " + belt);
                    else Assert.IsNull(belt);
                }
            }
            // The shoulders and the neck have no slot in the pack: drawn as before.
            foreach (var f in GearLooks.Families.Where(x => x.slot == "shoulders" || x.slot == "neck"))
                Assert.IsFalse(GanzSeKit.Map(SlotOf(f.slot), Look(f.name, f.variants[0], Color.gray, steel), out _, out _), f.name + " stays drawn.");
        }

        [Test] public void Cloth_leather_mail_and_plate_read_differently()
        {
            var steel = new Color(.62f, .63f, .66f); var purple = new Color(.42f, .12f, .5f);
            string Chest(string family, string variant, Color cloth, Color metal, int q = 1) { Assert.IsTrue(GanzSeKit.Map(EquipSlot.Chest, Look(family, variant, cloth, metal, q), out var p, out _)); return p; }
            var cloth = Chest("chest.robe", "vestment", purple, steel); var leather = Chest("chest.jerkin", "leather", new Color(.2f, .35f, .7f), steel);
            var mail = Chest("chest.hauberk", "mail", purple, steel); var plate = Chest("chest.cuirass", "plate", purple, steel);
            Assert.AreEqual(4, GanzSeKit.TypeOf(cloth), "A cloth caster wears the sleeved coat (" + cloth + ").");
            Assert.AreEqual(5, GanzSeKit.TypeOf(leather), "A leather rogue the jerkin (" + leather + ").");
            Assert.AreEqual(1, GanzSeKit.TypeOf(mail), "Mail the breastplate on mail (" + mail + ").");
            Assert.AreEqual(2, GanzSeKit.TypeOf(plate), "Plate the black plate (" + plate + ").");
            Assert.AreEqual(3, GanzSeKit.TypeOf(Chest("chest.jerkin", "hide", purple, steel)), "Hide the bone and hide.");
            // Colourways: the purple robe purple, the blue jerkin blue, plain steel the plainest plate, an epic's gold trim gold.
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 4, 1), cloth); Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 5, 1), leather);
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 2, 1), plate);
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 2, 3), Chest("chest.cuirass", "plate", purple, steel, 4), "Epic plate in gold.");
            // A breastplate on mail with pads, the caster's coat with its rings and fur cover the shoulders; the jerkin does not.
            Assert.IsTrue(GanzSeKit.CoversShoulders(mail)); Assert.IsTrue(GanzSeKit.CoversShoulders(cloth)); Assert.IsFalse(GanzSeKit.CoversShoulders(leather)); Assert.IsFalse(GanzSeKit.CoversShoulders(null));
        }

        [Test] public void Every_look_and_trade_wears_pieces_the_file_holds()
        {
            var looks = new[] { "Warrior", "Paladin", "Ranger", "Mage", "Druid", "Healer", "Collector", "Warden", "Sentry", "Outrider", "Cultist", "Deserter", "BanditKing", "SaltMender", "Hollow" };
            foreach (var look in looks)
                foreach (var w in GanzSeKit.Outfit(look, null, 3)) Assert.IsTrue(Pieces.Contains(w.Piece), look + ": " + w.Piece);
            foreach (var trade in GanzSeKit.Trades.Concat(new[] { "someone", null }))
            {
                var set = GanzSeKit.Outfit("Villager", trade, 5);
                foreach (var w in set) Assert.IsTrue(Pieces.Contains(w.Piece), trade + ": " + w.Piece);
                Assert.IsTrue(set.Any(w => w.slot == GanzSeKit.Slot.Legs), (trade ?? "plain folk") + " wears breeches.");
                if (trade != "blacksmith") Assert.IsTrue(set.Any(w => w.slot == GanzSeKit.Slot.Chest), (trade ?? "plain folk") + " wears a top.");
                Assert.AreEqual(set.Length, set.Select(w => w.slot).Distinct().Count(), "one piece a slot");
            }
            Assert.AreNotEqual(GanzSeKit.Outfit("Villager", "farmer", 1)[0].Piece, GanzSeKit.Outfit("Villager", "elder", 1)[0].Piece, "Trades dress differently.");
            // The face: the old hair colours to the pack's (black, dark brown, brown, blond, grey).
            Assert.AreEqual(1, GanzSeKit.HairColour(new Color(.08f, .07f, .06f) * GanzSeKit.HairLift));
            Assert.AreEqual(4, GanzSeKit.HairColour(new Color(.62f, .48f, .26f) * GanzSeKit.HairLift));
            Assert.AreEqual(5, GanzSeKit.HairColour(new Color(.7f, .68f, .64f) * GanzSeKit.HairLift));
            Assert.AreEqual(0, GanzSeKit.HairStyle(4), "bald");
            for (int v = -20; v <= 40; v++) Assert.That(GanzSeKit.EyeColour(v), Is.InRange(1, 3), "dark eyes for variant " + v);
            Assert.Greater(Enumerable.Range(0, 10).Select(GanzSeKit.EyeColour).Distinct().Count(), 1, "eyes vary");
            foreach (var look in new[] { "Warrior", "Paladin", "Ranger", "Mage", "Druid", "Healer" })
                Assert.IsTrue(GanzSeKit.Outfit(look, null, 0).Any(w => w.slot == GanzSeKit.Slot.Chest), look + " starts in a top, not bare-chested.");
            var tone = GanzSeKit.Skin(Color.white); Assert.That(GanzSeKit.SkinSwatch.r * tone.r, Is.EqualTo(GanzSeKit.KitSkin.r).Within(.001f), "the swatch tinted to the Quaternius skin");
        }

        [Test] public void The_file_holds_exactly_the_tables_pieces_on_a_humanoid()
        {
            Assume.That(ModelFigure.GanzSeAvailable, "The pack is in the project (Resources/Characters/GanzSe).");
            CollectionAssert.AreEquivalent(GanzSeKit.AllPieces, ModelFigure.GanzSePieces, "The pieces the table names are the file's skinned pieces.");
            var model = Resources.Load<GameObject>(GanzSeKit.ModelPath); var avatar = model.GetComponent<Animator>().avatar;
            Assert.IsTrue(avatar != null && avatar.isValid && avatar.isHuman, "A Humanoid avatar for our clips.");
            var map = avatar.humanDescription.human.ToDictionary(h => h.humanName, h => h.boneName);
            Assert.AreEqual("spine_01", map["Hips"], "Its hips are spine_01 (the legs hang from it).");
            Assert.AreEqual("head", map["Head"]); Assert.AreEqual("forearm_r", map["RightLowerArm"]); Assert.AreEqual("shin_l", map["LeftLowerLeg"]);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == GanzSeKit.Body);
            Assert.IsTrue(body.sharedMesh.isReadable, "Readable: the head is cut from the body for measuring hats.");
            var headBone = model.GetComponentsInChildren<Transform>(true).First(t => t.name == "head");
            Assert.That(headBone.position.y - model.transform.position.y, Is.InRange(1.4f, 1.8f), "Y up, in metres: the head bone about 1.55 m over the soles (no scale or turn to fix).");
            Assert.Greater(model.GetComponentsInChildren<Transform>(true).First(t => t.name == "toes_l").position.z, model.GetComponentsInChildren<Transform>(true).First(t => t.name == "foot_l").position.z, "It faces +Z (toes before the ankle).");
        }

        [Test] public void The_import_rule_keeps_the_palette_sharp()
        {
            var m = AssetImporter.GetAtPath("Assets/Crulanda/Resources/Characters/GanzSe/GanzSe Free Modular Character 1_1.fbx") as ModelImporter;
            var t = AssetImporter.GetAtPath("Assets/Crulanda/Resources/Characters/GanzSe/Base Palette Texture.png") as TextureImporter;
            Assume.That(m != null && t != null, "The pack is in the project.");
            Assert.AreEqual(ModelImporterAnimationType.Human, m.animationType); Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, m.avatarSetup);
            Assert.IsFalse(m.importAnimation, "No clips of its own."); Assert.IsTrue(m.isReadable);
            Assert.AreEqual(FilterMode.Point, t.filterMode, "Point-filtered swatches."); Assert.IsFalse(t.mipmapEnabled, "No mipmaps to blend the swatches.");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, t.textureCompression);
        }

        ActorVisual Figure(ActorLook look, int variant = 0, string role = null, bool orc = false, string name = "GanzSe test figure")
        {
            var go = new GameObject(name); new GameObject("Body").transform.SetParent(go.transform, false); go.transform.position = new Vector3(made.Count * 3, 1, 20); made.Add(go);
            return ActorVisual.Attach(go, look, variant, false, role, orc);
        }
        static string Generated(ItemDatabase db, string slot, int level, int quality, string piece)
        {
            for (int seed = 0; seed < 5000; seed++)
            {
                string id = ItemDatabase.GearId(slot, level, quality, seed);
                if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == piece) return id;
            }
            Assert.Fail("No generated " + piece + "."); return null;
        }

        [Test] public void A_man_is_built_on_the_pack_and_wears_its_pieces()
        {
            Assume.That(ModelFigure.Available && ModelFigure.GanzSeAvailable, "Both kits are in the project.");
            ActorVisual.Models = true;
            GanzSeKit.On = false; Assert.IsFalse(Figure(ActorLook.Warrior, 3).Model.GanzSe, "Off: the Quaternius figure, as now.");
            GanzSeKit.On = true;
            var v = Figure(ActorLook.Warrior, 3); var m = v.Model;
            Assert.IsTrue(m.GanzSe, "On: the pack's body."); Assert.IsTrue(m.Complete, "Every bone the frames hang on.");
            Assert.IsTrue(m.Animator.avatar != null && m.Animator.avatar.isHuman, "Its Humanoid avatar.");
            foreach (var f in new[] { "Arm L", "Arm R/Forearm R/Hand R", "Leg L/Shin L/Foot L", "Head frame", "Chest frame", "Hips frame" }) Assert.NotNull(v.transform.Find("Body/" + f), "the frame " + f);
            Assert.AreEqual("spine_01", m.Hips.name); Assert.AreEqual("head", m.HeadBone.name); Assert.AreEqual("forearm_r", m.LowerArmR.name);
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Legs, 1, 1), m.Worn(GanzSeKit.Slot.Legs), "The Warrior's own breeches.");
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 5, 1), m.Worn(GanzSeKit.Slot.Chest), "His own top: nobody in underwear (wardrobe-ganzse-1).");
            var skin = m.GzBody.sharedMaterials[0].color; Assert.Less(skin.b, .75f, "The skin tinted to his tone, not the pale swatch (" + skin + ")");
            Assert.AreEqual(GanzSeKit.Gloss, m.GzBody.sharedMaterials[0].GetFloat("_Glossiness"), .001f, "Matte.");
            foreach (var r in m.Renderers) if (r.name.StartsWith("Eyes Type")) StringAssert.DoesNotMatch("Color [45]$", r.name, "Dark eyes, not the bright blue or green.");
            Assert.IsTrue(m.Head != null && m.Head.forceRenderingOff, "The head cut for measuring, never drawn.");
            foreach (var r in m.Renderers) Assert.IsTrue(r.sharedMaterials.Length > 0 && r.sharedMaterials.All(x => x != null), r.name + " has its material.");
            // Gear: the pack's pieces, nothing drawn over them.
            var db = new ItemDatabase(); var looks = GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text);
            var ids = new[] { Generated(db, "chest", 7, 3, "Hauberk"), Generated(db, "hands", 7, 3, "Gauntlets"), Generated(db, "legs", 7, 3, "Greaves"), Generated(db, "feet", 7, 3, "Sabatons") };
            v.ApplyGearIds(ids, db, looks);
            foreach (var id in ids)
            {
                var d = db.Get(id); var slot = SlotOf(d.slot); var gs = GanzSeKit.SlotFor(slot).Value;
                Assert.IsTrue(GanzSeKit.Map(slot, looks.Resolve(d), out var piece, out _), id + " has a piece");
                Assert.AreEqual(piece, m.Worn(gs), id + " shows its piece.");
                Assert.AreEqual(0, v.GearParts(slot), id + ": no drawn armour doubling it.");
            }
            Assert.IsNotNull(m.Worn(GanzSeKit.Slot.Belt), "The chest piece's belt.");
            // Off again: his own clothes.
            v.ApplyGearIds(new string[0], db, looks);
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Chest, 5, 1), m.Worn(GanzSeKit.Slot.Chest), "His own top back."); Assert.IsNull(m.Worn(GanzSeKit.Slot.Belt));
            Assert.AreEqual(GanzSeKit.Armour(GanzSeKit.Slot.Legs, 1, 1), m.Worn(GanzSeKit.Slot.Legs), "His breeches back.");
            Assert.AreEqual(1, m.Renderers.Count(r => r != null && r.name == m.Worn(GanzSeKit.Slot.Legs)), "One piece a slot, the old one gone.");
            // A hood stays drawn (the pack has none) and hides his hair.
            v.ApplyGearIds(new[] { Generated(db, "head", 5, 2, "Hood") }, db, looks);
            Assert.Greater(v.GearParts(EquipSlot.Head), 0, "The hood is drawn."); Assert.IsFalse(v.HairShowing, "It hides the hair.");
            // A villager in his trade's clothes; a woman and an orc keep the Quaternius figure.
            var smith = Figure(ActorLook.Villager, 3, "blacksmith", false, "Brannoc Vell"); Assert.IsTrue(smith.Model.GanzSe);
            Assert.AreEqual(GanzSeKit.Outfit("Villager", "blacksmith", 3).First(w => w.slot == GanzSeKit.Slot.Legs).Piece, smith.Model.Worn(GanzSeKit.Slot.Legs));
            Assert.IsFalse(Figure(ActorLook.Villager, 40, "henwife", false, "Hedda Thorne").Model.GanzSe, "A woman: the Quaternius figure.");
            var orc = Figure(ActorLook.Paladin, 3, null, true); Assert.IsTrue(orc.Model == null || !orc.Model.GanzSe, "An orc: not the pack's body.");
        }
    }
}
