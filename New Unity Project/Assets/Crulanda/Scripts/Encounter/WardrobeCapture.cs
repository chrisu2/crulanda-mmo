using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The wardrobe line-up (loot DESIGN.md section 8), run with <c>--crulanda-wardrobe-capture &lt;dir&gt;</c> from a windowed
    /// player (the labels are IMGUI): bare mannequins in a quiet open spot near the zone's green, one row at a time, each
    /// labelled. Noon in calm weather; the HUD, the player, Mira, the enemies and the village are hidden. Rows are a few figures
    /// wide so each piece is big enough to judge, which takes several shots per set:
    /// 01-weapon-rack-a..g, 02-shield-wall-a..c (every main-hand and off-hand family and variant, held);
    /// 03-quality-ladder-a (blades), -b (shields), -c (lanterns): poor to epic at dusk so the glow reads;
    /// 04-sets-front-a..d and 05-sets-back-a..d: full generated kits at levels 2, 5, 8, 10 and 13 (a common, b uncommon, c rare
    /// in mail and plate; d rare in cloth and leather), front and back;
    /// 06-helms-*, 07-shoulders-chests-*, 08-hands-legs-feet-neck-*: every armour family and variant up close;
    /// 09-palettes-a, -b: the same kit in all ten palettes; 10-crafted: the Blacksmith's 21 pieces by metal tier;
    /// 11-in-motion-front, -back: a rare level-13 kit standing, walking, sneaking, sitting and swimming (weapons slung);
    /// 12-druid-forms: the Druid in that kit in each of her four forms, and bare-headed in her hood;
    /// 13-named-oakhaven-*, 14-named-khaven-*, 15-named-peaks-*, 16-named-ashrim-*, 17-named-verdant-*, 18-named-world-*: every
    /// named item of the loot database (step A3) on its own mannequin at its own level and quality, labelled with its name, quality
    /// and source; each boss's list together, then each set (its other pieces and one figure wearing the whole set), then the
    /// zone's drop lists, quest rewards and merchants' pieces; the world's drops and the named items already in the game last.
    /// The session is on the capture's throwaway save (EncounterCapture), so nothing here touches a real character.
    /// </summary>
    public sealed class WardrobeCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-wardrobe-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }

        /// <summary>Every main-hand family and variant with the palette its named piece wears (glowing where the named piece does).</summary>
        public static readonly string[] MainHandLooks = {
            "sword.short:plain/oakhaven", "sword.short:notched/oakhaven", "sword.arming:straight/tollroad", "sword.arming:curved/concord", "sword.arming:disc/tollroad",
            "sword.falchion:clipped/sandthrone", "sword.falchion:heavy/sandthrone", "sword.sabre:officer/sandthrone", "sword.sabre:broken/sandthrone+glow",
            "sword.leaf:bronze/veridian", "sword.leaf:root/veridian", "sword.great:steel/veridian", "sword.great:wood/veridian+glow", "sword.great:living/veridian+glow",
            "knife:hooked/sandthrone", "knife:needle/tollroad", "knife:glass/pale+glow", "knife:sickle/ashwalker+glow", "cleaver:slab/ashwalker", "cleaver:notched/sandthrone",
            "axe.hand:wedge/oakhaven", "axe.hand:hatchet/khaven", "axe.bearded:plain/sandthrone", "axe.bearded:hooked/khaven", "axe.crescent:plain/tollroad", "axe.crescent:spiked/ashwalker",
            "club:plain/oakhaven", "club:studded/oakhaven", "club:bound/pilgrim", "club:tusk/tollroad", "club:tankard/oakhaven",
            "mace.flanged:six/tollroad", "mace.flanged:fist/khaven", "hammer.war:pick/khaven", "hammer.war:maul/khaven",
            "mace.root:burl/veridian", "mace.root:antler/veridian", "mace.root:briar/veridian",
            "polearm:spear/tollroad", "polearm:harpoon/ashwalker", "polearm:fork/oakhaven", "polearm:billhook/sandthrone", "polearm:spade/khaven",
            "staff:knob/oakhaven", "staff:crook/pilgrim", "staff:forked/pale+glow", "staff:skull/cult"
        };
        /// <summary>Every off-hand family and variant.</summary>
        public static readonly string[] OffHandLooks = {
            "shield.buckler:plain/oakhaven", "shield.buckler:tusk/oakhaven", "shield.buckler:chitin/veridian",
            "shield.round:boards/oakhaven", "shield.round:hide/sandthrone", "shield.round:cask/ashwalker", "shield.round:lid/sandthrone",
            "shield.heater:plain/tollroad", "shield.heater:striped/concord", "shield.heater:glass/pale+glow",
            "shield.kite:plain/veridian", "shield.kite:slab/tollroad+glow", "shield.leaf:bronze/veridian", "shield.leaf:bark/veridian",
            "offhand.hung:lantern/oakhaven", "offhand.hung:shuttered/sandthrone+glow", "offhand.hung:moss/veridian+glow", "offhand.hung:censer/cult+glow", "offhand.hung:scale/concord"
        };
        /// <summary>Every armour family and variant, by slot, in the palette its named pieces mostly wear.</summary>
        public static readonly string[] HeadLooks = {
            "head.cap:plain/oakhaven", "head.cap:flaps/khaven", "head.cap:leaf/veridian", "head.hood:cloth/pilgrim", "head.hood:oilskin/oakhaven", "head.coif:mail/tollroad",
            "head.wrap:scarf/sandthrone", "head.kettle:plain/tollroad", "head.kettle:half/sandthrone", "head.barbute:plain/tollroad", "head.barbute:rimed/pale+glow",
            "head.mask:bone/ashwalker", "head.mask:tear/cult+glow", "head.circlet:band/concord", "head.circlet:briar/veridian", "head.crown:tin/sandthrone", "head.crown:root/veridian+glow", "head.crown:antler/veridian"
        };
        public static readonly string[] ShoulderChestLooks = {
            "shoulder.mantle:cloth/oakhaven", "shoulder.mantle:fur/pilgrim", "shoulder.mantle:hide/ashwalker", "shoulder.mantle:frayed/khaven", "shoulder.mantle:shawl/pale",
            "shoulder.pauldron:dome/tollroad", "shoulder.pauldron:bone/ashwalker", "shoulder.spaulder:lames/tollroad", "shoulder.spaulder:glass/pale+glow", "shoulder.spaulder:bark/veridian",
            "chest.tunic:plain/oakhaven", "chest.jerkin:leather/khaven", "chest.jerkin:hide/ashwalker", "chest.jerkin:scale/sandthrone", "chest.hauberk:mail/tollroad",
            "chest.coat:grey/concord", "chest.coat:skirted/sandthrone", "chest.cuirass:plate/tollroad", "chest.cuirass:bark/veridian",
            "chest.robe:vestment/pilgrim", "chest.robe:cassock/cult", "chest.robe:shroud/pale"
        };
        public static readonly string[] HandLooks = {
            "hands.gloves:plain/oakhaven", "hands.gloves:fingerless/sandthrone", "hands.gloves:mitts/pilgrim", "hands.wraps:cloth/oakhaven", "hands.wraps:fur/oakhaven",
            "hands.wraps:silk/concord", "hands.gauntlets:plate/tollroad", "hands.gauntlets:bone/ashwalker", "hands.gauntlets:glass/pale+glow", "hands.gauntlets:root/veridian+glow"
        };
        public static readonly string[] LegLooks = {
            "legs.breeches:plain/oakhaven", "legs.breeches:patched/sandthrone", "legs.leggings:garter/pilgrim", "legs.leggings:hide/ashwalker", "legs.greaves:plate/tollroad", "legs.greaves:full/veridian", "legs.kilt:bone/ashwalker"
        };
        public static readonly string[] FeetLooks = {
            "feet.shoes:plain/oakhaven", "feet.boots:plain/sandthrone", "feet.boots:waders/oakhaven", "feet.boots:hobnail/khaven", "feet.boots:root/veridian+glow", "feet.sabatons:plate/ashwalker"
        };
        public static readonly string[] NeckLooks = {
            "neck.pendant:drop/concord", "neck.pendant:glass/pale+glow", "neck.pendant:seal/concord", "neck.pendant:signet/tollroad", "neck.pendant:ember/ashwalker", "neck.pendant:leaf/veridian",
            "neck.pendant:vial/pale+glow", "neck.pendant:jar/pilgrim+glow", "neck.cord:beads/oakhaven", "neck.cord:knot/pilgrim", "neck.cord:fang/oakhaven", "neck.cord:tooth/ashwalker",
            "neck.cord:tine/veridian", "neck.torc:metal/tollroad", "neck.torc:band/sandthrone", "neck.torc:wood/veridian", "neck.torc:charm/oakhaven"
        };
        /// <summary>The Blacksmith's 21 pieces (professions DESIGN.md 5.2) by metal tier, with the level each tier is made at.</summary>
        public static readonly (string tier, int level, string[] items)[] Crafted = {
            ("Copper", 2, new[] { "craft.copper_cudgel", "craft.copper_buckler", "craft.copper_gauntlets", "craft.copper_jerkin" }),
            ("Bog-iron", 4, new[] { "craft.bogiron_hatchet", "craft.bogiron_helm", "craft.bogiron_greaves", "craft.bogiron_hauberk" }),
            ("Ridge-steel", 7, new[] { "craft.ridgesteel_blade", "craft.ridgesteel_shield", "craft.ridgesteel_pauldrons", "craft.ridgesteel_cuirass" }),
            ("Ash-steel", 9, new[] { "craft.ashsteel_cleaver", "craft.ashsteel_helm", "craft.ashsteel_sabatons", "craft.ashsteel_hauberk" }),
            ("Veridian", 11, new[] { "craft.veridian_warblade", "craft.veridian_shield", "craft.veridian_legplates", "craft.veridian_breastplate" }),
            ("Heartwood", 12, new[] { "craft.heartwood_greatblade" })
        };
        /// <summary>Generated kits by piece word, head to off hand: mail and plate, and cloth and leather.</summary>
        static readonly string[] Martial = { "Cap", "Torc", "Pauldrons", "Hauberk", "Gauntlets", "Greaves", "Sabatons", "Blade", "Shield" };
        static readonly string[] Cloth = { "Hood", "Pendant", "Mantle", "Jerkin", "Gloves", "Leggings", "Boots", "Cudgel", "Lantern" };
        static readonly int[] SetLevels = { 2, 5, 8, 10, 13 };
        const int PerRow = 5; const float Spacing = 1.7f;   // five to a shot, framed close enough to judge a guard or a rim

        /// <summary>How a shot frames its row: figures per row and apart, the height looked at, the camera's pitch and margin, where the labels hang (above the figure's root, negative is below), and which way the figures face.</summary>
        sealed class Framing
        {
            public int perRow = PerRow; public float spacing = Spacing, focus = .95f, pitch = 12, margin = 1.2f, label = -1.05f, yaw = 180;
        }
        sealed class Entry { public string[] ids; public string title, sub; public ActorLook look = ActorLook.Warrior; public ActorPose pose; public bool walk; public int form = -1, hair = -1; public float yaw = float.NaN; }
        EncounterSession session; ItemDatabase db; GearLooks looks;
        readonly List<GameObject> figures = new List<GameObject>();
        readonly List<(Transform at, string title, string sub)> labels = new List<(Transform, string, string)>();
        readonly List<(Transform t, Vector3 home, Vector3 along)> walkers = new List<(Transform, Vector3, Vector3)>();
        GUIStyle titleStyle, subStyle; float labelAt = -1.05f;
        Vector2 spot; float ground;

        public IEnumerator Run(EncounterSession s, string directory)
        {
            session = s; Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            var text = Resources.Load<TextAsset>("Gear/looks");
            if (text == null) { Debug.LogError("Wardrobe capture: Resources/Gear/looks.json is missing."); Application.Quit(1); yield break; }
            looks = GearLooks.Parse(text.text); db = new ItemDatabase();
            Quiet();
            var zone = Crulanda.World.ZoneBuilder.Active;
            spot = FindSpot(zone, out ground);
            Debug.Log("Wardrobe capture at " + spot + " (ground " + ground.ToString("0.0") + ").");

            var weapons = new Framing { yaw = 115 }; var shields = new Framing { yaw = -115 };
            var main = Entries(MainHandLooks, "mainhand"); var off = Entries(OffHandLooks, "offhand");
            yield return Shots(directory, "01-weapon-rack", main, weapons, 12);
            yield return Shots(directory, "02-shield-wall", off, shields, 12);
            // The quality ladder: the same generated piece (level 8, Ridge-forged) from poor to epic, at dusk.
            yield return Shot(Path.Combine(directory, "03-quality-ladder-a.png"), Ladder("mainhand", "Blade"), 19.3f, weapons);
            yield return Shot(Path.Combine(directory, "03-quality-ladder-b.png"), Ladder("offhand", "Shield"), 19.3f, shields);
            yield return Shot(Path.Combine(directory, "03-quality-ladder-c.png"), Ladder("offhand", "Lantern"), 19.3f, new Framing { yaw = -150 });

            // Full generated kits by level (columns) and quality (one shot each), front and back.
            var sets = new Framing { perRow = 5, spacing = 1.5f, pitch = 8, margin = .9f };
            var kits = new[] { (Martial, 1), (Martial, 2), (Martial, 3), (Cloth, 3) };
            for (int k = 0; k < kits.Length; k++)
            {
                var row = new List<Entry>(); foreach (int level in SetLevels) row.Add(KitEntry(kits[k].Item1, level, kits[k].Item2));
                yield return Shot(Path.Combine(directory, "04-sets-front-" + (char)('a' + k) + ".png"), row, 12, sets);
                yield return Shot(Path.Combine(directory, "05-sets-back-" + (char)('a' + k) + ".png"), row, 12, new Framing { perRow = 5, spacing = 1.5f, pitch = 8, margin = .9f, yaw = 0 });
            }
            // Every armour family and variant up close: uncommon, at the middle level band, so rims and trim show but not glow.
            yield return Shots(directory, "06-helms", Entries(HeadLooks, "head", 7), new Framing { perRow = 4, spacing = .9f, focus = 1.62f, pitch = 6, margin = .35f, label = 1.32f, yaw = 165 }, 12);
            var chests = Entries(ShoulderChestLooks, null, 7);
            chests.Add(MaterialEntry("chest", 4, 2, "Riveted", "Jerkin", "brigandine"));   // the studded look comes from the generated material, not a named look
            yield return Shots(directory, "07-shoulders-chests", chests, new Framing { perRow = 4, spacing = 1.05f, focus = 1.3f, pitch = 8, margin = .45f, label = -.62f, yaw = 160 }, 12);
            var limbs = new List<Entry>(); var limbFrames = new List<Framing>();
            void Close(string[] list, string slot, Framing frame) { var e = Entries(list, slot, 7); for (int i = 0; i < e.Count; i += frame.perRow) { limbs.AddRange(e.GetRange(i, Mathf.Min(frame.perRow, e.Count - i))); limbFrames.Add(frame); } }
            Close(HandLooks, "hands", new Framing { perRow = 4, spacing = 1.05f, focus = 1.0f, pitch = 8, margin = .45f, label = -.75f, yaw = 135 });
            Close(LegLooks, "legs", new Framing { perRow = 4, spacing = 1.0f, focus = .55f, pitch = 12, margin = .45f, label = .45f, yaw = 150 });
            Close(FeetLooks, "feet", new Framing { perRow = 4, spacing = 1.0f, focus = .4f, pitch = 16, margin = .45f, label = .45f, yaw = 150 });
            Close(NeckLooks, "neck", new Framing { perRow = 4, spacing = .9f, focus = 1.48f, pitch = 6, margin = .35f, label = -.3f, yaw = 175 });
            for (int i = 0, used = 0; i < limbFrames.Count; i++)
            {
                var frame = limbFrames[i]; int n = Mathf.Min(frame.perRow, limbs.Count - used);
                yield return Shot(Path.Combine(directory, "08-hands-legs-feet-neck-" + (char)('a' + i) + ".png"), limbs.GetRange(used, n), 12, frame); used += n;
            }
            // The same kit in every palette; the crafted pieces by metal tier.
            var palettes = new List<Entry>(); foreach (var p in looks.PaletteIds) palettes.Add(PaletteEntry(p));
            yield return Shots(directory, "09-palettes", palettes, new Framing { perRow = 5, spacing = 1.5f, pitch = 8, margin = .9f }, 12);
            var crafted = new List<Entry>(); foreach (var c in Crafted) crafted.Add(CraftedEntry(c.tier, c.level, c.items));
            yield return Shot(Path.Combine(directory, "10-crafted.png"), crafted, 12, new Framing { perRow = 6, spacing = 1.5f, pitch = 8, margin = .9f });
            // In motion: a rare level-13 kit in each pose (swimming slings the weapons), then the Druid in it in each form.
            foreach (var (name, yaw) in new[] { ("front", 180f), ("back", 0f) })
            {
                var poses = new List<Entry>();
                foreach (var (title, pose, walk) in new[] { ("Standing", ActorPose.None, false), ("Walking", ActorPose.None, true), ("Sneaking", ActorPose.Sneak, false), ("Sitting", ActorPose.Sit, false), ("Swimming", ActorPose.Swim, false) })
                { var e = KitEntry(Martial, 13, 3); e.title = title; e.pose = pose; e.walk = walk; poses.Add(e); }
                yield return Shot(Path.Combine(directory, "11-in-motion-" + name + ".png"), poses, 12, new Framing { perRow = 5, spacing = 1.6f, pitch = 8, margin = .9f, yaw = yaw });
            }
            var forms = new List<Entry>();
            foreach (DruidForm form in Enum.GetValues(typeof(DruidForm))) { var e = KitEntry(Martial, 13, 3); e.look = ActorLook.Druid; e.form = (int)form; e.title = form.ToString(); forms.Add(e); }
            var hooded = KitEntry(Martial, 13, 3); hooded.look = ActorLook.Druid; hooded.ids[0] = null; hooded.title = "No helm: her hood"; forms.Add(hooded);
            yield return Shot(Path.Combine(directory, "12-druid-forms.png"), forms, 12, new Framing { perRow = 5, spacing = 1.6f, pitch = 8, margin = .9f, yaw = 160 });
            yield return NamedShots(directory);
            Clear(); EncounterHud.Hidden = false;
            Debug.Log("WARDROBE_CAPTURE_DONE"); Application.Quit(0);
        }

        /// <summary>A list in rows of the framing's width, one shot each: name-a, name-b...</summary>
        IEnumerator Shots(string directory, string name, List<Entry> list, Framing frame, float hour)
        {
            for (int i = 0, n = 0; i < list.Count; i += frame.perRow, n++)
                yield return Shot(Path.Combine(directory, name + "-" + (char)('a' + n) + ".png"), list.GetRange(i, Mathf.Min(frame.perRow, list.Count - i)), hour, frame);
        }
        /// <summary>
        /// One look per entry, as a named test item (wardrobe.N) registered with this run's own copy of the looks: uncommon, and at
        /// <paramref name="level"/> when given (armour shows its level band). The slot is the look's own when none is given.
        /// </summary>
        List<Entry> Entries(string[] list, string slot, int level = 0)
        {
            var e = new List<Entry>();
            foreach (var look in list)
            {
                string id = "wardrobe." + db.Items.Count;
                looks.Register(id, look);
                GearLooks.TryParseLook(look, out var family, out var variant, out var palette, out bool glow);
                db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = slot ?? GearLooks.Family(family).slot, quality = 2, level = Mathf.Max(0, level - 1), canonStatus = "GAME-ONLY" };
                e.Add(new Entry { ids = new[] { id }, title = family + " : " + variant, sub = palette + (glow ? " + glow" : "") });
            }
            return e;
        }
        /// <summary>The first generated item (seeds from 0) of this slot, level and quality whose name has this piece word: the same every run.</summary>
        string Generated(string slot, int level, int quality, string piece)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                string id = ItemDatabase.GearId(slot, level, quality, seed);
                if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == piece) return id;
            }
            Debug.LogError("Wardrobe capture: no " + ItemDatabase.QualityNames[quality] + " level-" + level + " " + piece + " among the first 10000 seeds."); return null;
        }
        /// <summary>A generated piece made of this material word (the first seed from 0 that gives it), labelled with what it shows.</summary>
        Entry MaterialEntry(string slot, int level, int quality, string material, string piece, string shows)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                string id = ItemDatabase.GearId(slot, level, quality, seed);
                if (GearLooks.TrySplitGenerated(db.Get(id), out var m, out var p, out _, out _) && m == material && p == piece) return new Entry { ids = new[] { id }, title = db.Get(id).name, sub = shows };
            }
            Debug.LogError("Wardrobe capture: no " + material + " " + piece + " at level " + level + "."); return new Entry { ids = new string[0], title = material + " " + piece, sub = "not found" };
        }
        /// <summary>A whole generated kit: one piece per slot by word, at this level and quality.</summary>
        Entry KitEntry(string[] pieces, int level, int quality)
        {
            var ids = new string[pieces.Length]; for (int s = 0; s < pieces.Length; s++) ids[s] = Generated(ItemDatabase.SlotIds[s], level, quality, pieces[s]);
            var chest = ids[3] == null ? null : db.Get(ids[3]);
            return new Entry { ids = ids, title = "Level " + level + " " + ItemDatabase.QualityNames[quality], sub = chest == null ? "" : chest.name };
        }
        /// <summary>One kit (kettle hat, torc, pauldrons, hauberk, gauntlets, greaves, boots, sword and heater) in one palette, rare at level 9.</summary>
        Entry PaletteEntry(string palette)
        {
            string[] kit = { "head.kettle:plain", "neck.torc:metal", "shoulder.pauldron:dome", "chest.hauberk:mail", "hands.gauntlets:plate", "legs.greaves:plate", "feet.boots:plain", "sword.arming:straight", "shield.heater:plain" };
            var ids = new string[kit.Length];
            for (int s = 0; s < kit.Length; s++)
            {
                string id = "wardrobe." + db.Items.Count, look = kit[s] + "/" + palette; looks.Register(id, look);
                db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = ItemDatabase.SlotIds[s], quality = 3, level = 8, canonStatus = "GAME-ONLY" }; ids[s] = id;
            }
            return new Entry { ids = ids, title = palette, sub = "rare, level 9" };
        }
        /// <summary>A metal tier's crafted pieces on one figure (their looks and metal tints are in looks.json; the items themselves are the professions work's).</summary>
        Entry CraftedEntry(string tier, int level, string[] items)
        {
            foreach (var id in items)
            {
                if (!looks.Explicit.TryGetValue(id, out var look) || !GearLooks.TryParseLook(look, out var family, out _, out _, out _)) { Debug.LogError("Wardrobe capture: no look for " + id + "."); continue; }
                if (db.Get(id) == null) db.Items[id] = new ItemDef { id = id, name = id, kind = "gear", slot = GearLooks.Family(family).slot, quality = id.Contains("heartwood") ? 3 : 2, level = level, canonStatus = "GAME-ONLY" };
            }
            return new Entry { ids = items, title = tier, sub = items.Length + " piece" + (items.Length == 1 ? "" : "s") + ", level " + level };
        }
        /// <summary>Five generated pieces, poor to epic, all "Ridge-forged &lt;piece&gt;" at level 8 with the family's first variant.</summary>
        List<Entry> Ladder(string slot, string piece)
        {
            var e = new List<Entry>();
            for (int q = 0; q <= 4; q++)
            {
                string found = null;
                for (int seed = 0; seed < 10000 && found == null; seed++)
                {
                    string id = ItemDatabase.GearId(slot, 8, q, seed); var d = db.Get(id);
                    if (GearLooks.TrySplitGenerated(d, out var material, out var p, out _, out _) && material == "Ridge-forged" && p == piece
                        && (looks.Resolve(d).family.StartsWith("model.") || looks.Resolve(d).variant == GearLooks.Family(looks.Resolve(d).family).variants[0])) found = id;   // uncommon and better blades and shields wear models (2026-10-05)
                }
                if (found == null) { Debug.LogError("Wardrobe capture: no " + ItemDatabase.QualityNames[q] + " Ridge-forged " + piece + " among the first 10000 seeds."); continue; }
                var l = looks.Resolve(db.Get(found));
                e.Add(new Entry { ids = new[] { found }, title = ItemDatabase.QualityNames[q], sub = db.Get(found).name + "  (" + l.family + " : " + l.variant + ")" });
            }
            return e;
        }

        /// <summary>The named-item shots: number, the id prefix of the zone's items and the zone (the world's file last).</summary>
        static readonly (string shot, string prefix, string zone)[] NamedZones = {
            ("13", "oak", "oakhaven"), ("14", "kha", "khaven"), ("15", "pea", "peaks"), ("16", "ash", "ashrim"), ("17", "ver", "verdant"), ("18", "world", "world")
        };
        /// <summary>
        /// Shots 13-18: the game's item files (the loot files among them since step L2) parsed afresh, every named item on a
        /// mannequin, grouped (see the class summary). Skipped with an error when there are no loot files or they do not parse.
        /// </summary>
        IEnumerator NamedShots(string directory)
        {
            var texts = new List<string>();
            if (session.content != null) foreach (var f in session.content.itemFiles ?? new TextAsset[0]) if (f != null) texts.Add(f.text);
            ItemDatabase named; LootDatabase loot;
            try { named = ItemDatabase.Parse(texts); loot = LootDatabase.Parse(texts, named, looks); }
            catch (ArgumentException e) { Debug.LogError("Wardrobe capture: the loot files do not parse, so shots 13-18 are skipped:\n" + e.Message); yield break; }
            if (loot.Gear.Count == 0) { Debug.LogError("Wardrobe capture: no loot files among the item files (Crulanda > World > Build Oakhaven registers EncounterContent/Items), so shots 13-18 are skipped."); yield break; }
            db = named;
            var frame = new Framing { perRow = 4, spacing = 1.7f, pitch = 8, margin = .7f, yaw = 160 };
            foreach (var (shot, prefix, zone) in NamedZones) yield return Rows(directory, shot + "-named-" + zone, NamedGroups(loot, prefix, zone), frame);
        }
        /// <summary>Groups packed into rows of the framing's width, a group never split unless it is wider than a row; one shot a row.</summary>
        IEnumerator Rows(string directory, string name, List<List<Entry>> groups, Framing frame)
        {
            var rows = new List<List<Entry>>(); var row = new List<Entry>();
            foreach (var g in groups)
            {
                if (g.Count == 0) continue;
                if (row.Count > 0 && row.Count + g.Count > frame.perRow) { rows.Add(row); row = new List<Entry>(); }
                foreach (var e in g) { if (row.Count == frame.perRow) { rows.Add(row); row = new List<Entry>(); } row.Add(e); }
            }
            if (row.Count > 0) rows.Add(row);
            for (int i = 0; i < rows.Count; i++) yield return Shot(Path.Combine(directory, name + "-" + (char)('a' + i) + ".png"), rows[i], 12, frame);
        }
        /// <summary>
        /// One zone's named items, grouped: each boss's list (signature pieces, rare table, epic), each set the zone's items belong
        /// to (its pieces not already shown, then the whole set on one figure), each drop list of ordinary mobs, the quest rewards,
        /// the merchants' pieces. The world: its drops, then the named items already in the game that no list drops.
        /// </summary>
        List<List<Entry>> NamedGroups(LootDatabase loot, string prefix, string zone)
        {
            var groups = new List<List<Entry>>(); var shown = new HashSet<string>(); bool world = zone == "world";
            string Pct(float chance) { return (chance * 100).ToString("0.##") + "%"; }
            bool Mine(string id) { return id.StartsWith("loot." + prefix + ".", StringComparison.Ordinal); }
            Entry One(string id, string from)
            {
                shown.Add(id); var d = db.Get(id);
                // Weapons and shields turn side-on as in shots 01 and 02, so a blade is not pointed at the camera and a shield shows its face.
                float yaw = d == null ? float.NaN : d.slot == "mainhand" ? 115 : d.slot == "offhand" && looks.Resolve(d).family != "offhand.hung" ? -115 : float.NaN;
                return new Entry { ids = new[] { id }, title = d != null ? d.name : id, sub = (d != null ? ItemDatabase.QualityNames[d.quality] : "?") + ", " + from, yaw = yaw };
            }
            string From(string id)
            {
                var g = loot.Meta(id); string kind = g == null ? null : LootDatabase.SourceKind(g.source); if (kind == null) return "no source";
                string where = g.source.Substring(kind.Length + 1);
                foreach (var d in loot.Drops)
                    foreach (var grp in d.groups)
                        if (Array.Exists(grp.pick, k => k.item == id))
                            return kind == "world" ? "world drop, levels " + where + ", " + Pct(grp.chance) + (string.IsNullOrEmpty(d.rank) ? "" : " (" + d.rank + ")")
                                : (string.IsNullOrEmpty(d.tag) ? "any camp" : d.tag) + (d.levelMin > 1 ? " level " + d.levelMin + "+" : "") + ", " + Pct(grp.chance);
                return kind == "quest" ? "quest " + where : kind == "vendor" ? "sold by " + where : kind == "secret" ? "a hidden find" : kind + " " + where;
            }
            if (!world)
            {
                foreach (var d in loot.Drops)
                {
                    if (d.zone != zone || string.IsNullOrEmpty(d.mob)) continue;
                    var g = new List<Entry>();
                    foreach (var grp in d.groups)
                        foreach (var k in grp.pick)
                            if (!shown.Contains(k.item)) g.Add(One(k.item, d.mob + ", " + (grp.signature ? "signature" : db.Get(k.item)?.quality == 4 ? "epic " + Pct(grp.chance) : "rare table " + Pct(grp.chance))));
                    groups.Add(g);
                }
                foreach (var s in loot.SetOrder)
                {
                    if (!Array.Exists(s.pieces, Mine)) continue;
                    var g = new List<Entry>();
                    foreach (var piece in s.pieces) if (!shown.Contains(piece)) g.Add(One(piece, From(piece)));
                    g.Add(new Entry { ids = s.pieces, title = s.name, sub = "the whole set, " + s.pieces.Length + " pieces" });
                    groups.Add(g);
                }
            }
            foreach (var d in loot.Drops)
            {
                if (world ? !string.IsNullOrEmpty(d.zone) : d.zone != zone || !string.IsNullOrEmpty(d.mob)) continue;
                var g = new List<Entry>();
                foreach (var grp in d.groups) foreach (var k in grp.pick) if (Mine(k.item) && !shown.Contains(k.item)) g.Add(One(k.item, From(k.item)));
                groups.Add(g);
            }
            foreach (var kind in new[] { "quest", "vendor" })
            {
                var g = new List<Entry>();
                foreach (var m in loot.GearOrder) if (Mine(m.id) && !shown.Contains(m.id) && LootDatabase.SourceKind(m.source) == kind) g.Add(One(m.id, From(m.id)));
                groups.Add(g);
            }
            var rest = new List<Entry>();
            foreach (var m in loot.GearOrder)
            {
                bool listed = loot.Drops.Exists(d => Array.Exists(d.groups, grp => Array.Exists(grp.pick, k => k.item == m.id)));
                if (shown.Contains(m.id) || !(Mine(m.id) || (world && m.legacy && !listed))) continue;
                rest.Add(One(m.id, m.legacy ? "already in the game, " + From(m.id) : From(m.id)));
            }
            groups.Add(rest);
            return groups;
        }

        /// <summary>Hides everything that is not the line-up: the HUD, the player and Mira, the enemies, the village; noon, calm weather.</summary>
        void Quiet()
        {
            EncounterHud.Hidden = true; Crulanda.World.WorldClock.Hour = 12;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            foreach (var e in session.Enemies) if (e != null) { e.enabled = false; foreach (var r in e.GetComponentsInChildren<Renderer>(true)) r.enabled = false; }
            if (session.Companion != null) foreach (var r in session.Companion.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var r in session.Player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var motor = session.Player.GetComponent<AdventurerMotor>(); if (motor != null) motor.enabled = false;
            if (VillageLife.Active != null) VillageLife.Active.gameObject.SetActive(false);
        }

        /// <summary>
        /// Open, dry, fairly level ground near the green with nothing standing on it or in front of it (where the camera stands,
        /// to the south): rings of candidates round the recovery point, the first clear one wins.
        /// </summary>
        internal static Vector2 FindSpot(Crulanda.World.ZoneBuilder zone, out float ground)
        {
            ground = 0; if (zone == null) return Vector2.zero;
            var home = zone.Zone.spawns.recovery; float half = zone.Zone.size / 2 - 20, width = (PerRow - 1) * Spacing + 3;
            for (int ring = 0; ring <= 8; ring++)
                for (int k = 0; k < (ring == 0 ? 1 : 12); k++)
                {
                    float a = k * 30 * Mathf.Deg2Rad; var c = home + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ring * 7;
                    if (Mathf.Abs(c.x) > half || Mathf.Abs(c.y) > half) continue;
                    if (Clear(zone, c, width, out ground)) return c;
                }
            ground = zone.HeightAt(home.x, home.y); return home;
        }
        static bool Clear(Crulanda.World.ZoneBuilder zone, Vector2 c, float width, out float ground)
        {
            float lo = float.MaxValue, hi = float.MinValue; ground = 0;
            for (int i = 0; i <= 6; i++)
                for (int j = 0; j <= 4; j++)
                {
                    var p = c + new Vector2(-width / 2 + width * i / 6, -9 + 10.5f * j / 4);   // the row (z 0) and the ground in front of it to the camera (z -9)
                    if (zone.WaterAt(p, out _, out _)) return false;
                    float h = zone.HeightAt(p.x, p.y); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
            if (hi - lo > 1.4f) return false;
            ground = zone.HeightAt(c.x, c.y);
            var box = new Vector3(c.x, hi + 1.8f, c.y - 3.5f); var halfSize = new Vector3(width / 2 + .5f, 1.4f, 5.5f);
            foreach (var col in Physics.OverlapBox(box, halfSize, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (col.bounds.size.x < 60 && col.bounds.size.z < 60 && col.GetComponentInParent<Crulanda.Gameplay.Actor>() == null) return false;
            return true;
        }

        /// <summary>One row of mannequins, each dressed in its entry (posed, walking or in a Druid form if it says so), shot from the south.</summary>
        IEnumerator Shot(string file, List<Entry> row, float hour, Framing frame)
        {
            Clear(); Crulanda.World.WorldClock.Hour = hour; labelAt = frame.label;
            var zone = Crulanda.World.ZoneBuilder.Active;
            int n = row.Count; float width = (frame.perRow - 1) * frame.spacing;
            for (int i = 0; i < n; i++)
            {
                var e = row[i]; var p = spot + new Vector2((i - (n - 1) / 2f) * frame.spacing, 0);
                var at = zone != null ? zone.Ground(p, 1) : new Vector3(p.x, 1, p.y);
                var go = new GameObject("Wardrobe mannequin " + e.title); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, float.IsNaN(e.yaw) ? frame.yaw : e.yaw, 0));
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; body.transform.SetParent(go.transform, false); Destroy(body.GetComponent<Collider>());
                var look = ActorVisual.Attach(go, e.look, e.hair >= 0 ? e.hair : i);
                look.ApplyGearIds(Array.FindAll(e.ids, id => id != null), db, looks);
                look.Pose = e.pose;
                if (e.form >= 0) { body.transform.localScale = DruidKit.FormScale((DruidForm)e.form); look.SetClothColor(DruidKit.FormColor((DruidForm)e.form)); }
                if (e.walk) walkers.Add((go.transform, at, Vector3.right));
                figures.Add(go); labels.Add((go.transform, e.title, e.sub));
            }
            var view = session.View; var focus = new Vector3(spot.x, ground + frame.focus, spot.y);
            float hHalf = Mathf.Atan(Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * view.aspect);
            float dist = (width / 2 + frame.margin) / Mathf.Tan(hHalf);
            var cam = focus - Quaternion.Euler(frame.pitch, 0, 0) * Vector3.forward * dist;
            if (zone != null) cam.y = Mathf.Max(cam.y, zone.HeightAt(cam.x, cam.z) + .6f);
            var turn = Quaternion.LookRotation(focus - cam);
            for (float w = 0; w < .9f; w += Time.deltaTime) { view.transform.SetPositionAndRotation(cam, turn); Crulanda.World.TreeFade.UpdateAll(cam, focus, focus); yield return null; }
            view.transform.SetPositionAndRotation(cam, turn);
            ScreenCapture.CaptureScreenshot(file);
            yield return new WaitForSeconds(.5f);
        }
        /// <summary>Walkers pace back and forth along the row at a run's pace, so the stride shows.</summary>
        void Update()
        {
            foreach (var (t, home, along) in walkers) if (t != null) t.position = home + along * (Mathf.PingPong(Time.time * 2.6f, .7f) - .35f);
        }
        void Clear()
        {
            foreach (var f in figures) if (f != null) Destroy(f);
            figures.Clear(); labels.Clear(); walkers.Clear();
        }

        void OnGUI()
        {
            if (labels.Count == 0 || session == null || session.View == null) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
                titleStyle.normal.textColor = Color.white;
                subStyle = new GUIStyle(titleStyle) { fontSize = 11, fontStyle = FontStyle.Normal }; subStyle.normal.textColor = new Color(.85f, .85f, .8f);
            }
            foreach (var (at, title, sub) in labels)
            {
                if (at == null) continue;
                var sp = session.View.WorldToScreenPoint(at.position + Vector3.up * labelAt); if (sp.z < 0) continue;
                float titleHeight = Mathf.Max(20, titleStyle.CalcHeight(new GUIContent(title), 190));   // a long name wraps to a second line
                float subHeight = Mathf.Max(20, subStyle.CalcHeight(new GUIContent(sub), 190));   // a long source wraps to a second line
                var r = new Rect(sp.x - 95, Screen.height - sp.y + 4, 190, titleHeight + subHeight);
                var old = GUI.color; GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
                GUI.Label(new Rect(r.x, r.y + 2, r.width, titleHeight), title, titleStyle);
                GUI.Label(new Rect(r.x, r.y + titleHeight, r.width, subHeight), sub, subStyle);
            }
        }
    }
}
