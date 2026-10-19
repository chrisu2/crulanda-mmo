using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Which KayKit model a drawn weapon or shield wears (Chris, play note 245: "weapons . KayKit : Fantasy Weapons Bits"; Kay
    /// Lousberg, CC0, Resources/Props/KayKitWeapons). Every main-hand and off-hand family and variant of the drawn families
    /// (GearLooks.Families, not the "model." ones, which are the Asset Store packs' own prefabs) is listed: a KayKit model, or ""
    /// for an explicit fallback to the old drawing, where the variant's look is the point (a glowing glass knife, a pot lid, a
    /// tankard, a skull staff, the root-wood weapons, the lanterns) and no model in the pack is that thing. Shapes are picked by
    /// variant (the pack's five swords are five different swords, not five qualities: sword_A is a wooden one). The lengths are
    /// the old drawings', butt to tip in the weapon frame (before ActorVisual.HeldScale), so a model is held at the size its
    /// family always was. GAME-ONLY choices.
    /// </summary>
    public static class KayKitWeapons
    {
        public const string Folder = "Props/KayKitWeapons/";
        public const string Atlas = Folder + "KayKitWeaponsAtlas";
        /// <summary>How a model is laid in its frame (ActorVisual.KayKitFit).</summary>
        public enum Kind { Weapon, Shield, Bow }
        /// <summary>The Ranger's own bow (the class kit, held in the left hand): the strung one, whose string the bow shot cuts out and draws.</summary>
        public const string ClassBow = "bow_A_withString";
        public const float BowLength = 1.2f;   // as the pack bow it replaces (ActorVisual.ModelLength "Bow")

        /// <summary>family:variant to model ("" = the old drawing). Every drawn main-hand and off-hand variant is here (KayKitWeaponTests).</summary>
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            // Swords: sword_B the plain steel one-hander, sword_C the slim single-edged blade, sword_D the cup-hilted one, sword_E the
            // two-hander, sword_A the wooden one.
            { "sword.short:plain", "sword_B" }, { "sword.short:notched", "sword_B" },
            { "sword.arming:straight", "sword_B" }, { "sword.arming:curved", "sword_C" }, { "sword.arming:disc", "sword_D" },
            { "sword.falchion:clipped", "sword_C" }, { "sword.falchion:heavy", "sword_C" },
            { "sword.sabre:officer", "sword_D" }, { "sword.sabre:broken", "" },           // the glowing seam where it was forged whole again
            { "sword.leaf:bronze", "sword_B" }, { "sword.leaf:root", "" },                 // the root-wood hilt
            { "sword.great:steel", "sword_E" }, { "sword.great:wood", "sword_A" }, { "sword.great:living", "" },   // sprouting, its buds lit
            // Knives: dagger_A the plain needle-pointed one, dagger_B the one with the swept guard.
            { "knife:hooked", "dagger_B" }, { "knife:needle", "dagger_A" }, { "knife:glass", "" }, { "knife:sickle", "" },
            { "knife.off:hooked", "dagger_B" }, { "knife.off:needle", "dagger_A" }, { "knife.off:glass", "" }, { "knife.off:sickle", "" },
            // Axes: axe_A the small double-bit, axe_B the great crescent, axe_C the broad one-sided blade (also the cleaver).
            { "axe.hand:wedge", "axe_A" }, { "axe.hand:hatchet", "axe_A" },
            { "axe.bearded:plain", "axe_C" }, { "axe.bearded:hooked", "axe_C" },
            { "axe.crescent:plain", "axe_B" }, { "axe.crescent:spiked", "axe_B" },
            { "cleaver:slab", "axe_C" }, { "cleaver:notched", "axe_C" },
            // Clubs, maces, hammers: hammer_C the spiked club, hammer_B the spiked ball, hammer_A the war hammer.
            { "club:plain", "hammer_C" }, { "club:studded", "hammer_C" }, { "club:bound", "hammer_C" }, { "club:tusk", "" }, { "club:tankard", "" },
            { "mace.flanged:six", "hammer_B" }, { "mace.flanged:fist", "" },              // a hand of stone
            { "hammer.war:pick", "hammer_A" }, { "hammer.war:maul", "hammer_A" },
            { "mace.root:burl", "" }, { "mace.root:antler", "" }, { "mace.root:briar", "" },   // the Veridian root-wood maces
            // Polearms and staves.
            { "polearm:spear", "spear_A" }, { "polearm:harpoon", "spear_A" }, { "polearm:billhook", "halberd" }, { "polearm:fork", "" }, { "polearm:spade", "" },
            { "staff:knob", "staff_A" }, { "staff:crook", "" }, { "staff:forked", "staff_B" }, { "staff:skull", "" },   // forked: the crystal-topped staff
            // Shields: shield_A the small board shield, shield_B the heater with its painted face, shield_C the planked one with spikes.
            { "shield.buckler:plain", "shield_A" }, { "shield.buckler:tusk", "" }, { "shield.buckler:chitin", "" },
            { "shield.round:boards", "shield_C" }, { "shield.round:hide", "shield_C" }, { "shield.round:cask", "" }, { "shield.round:lid", "" },
            { "shield.heater:plain", "shield_B" }, { "shield.heater:striped", "shield_B" }, { "shield.heater:glass", "" },
            { "shield.kite:plain", "shield_B" }, { "shield.kite:slab", "shield_B" },
            { "shield.leaf:bronze", "" }, { "shield.leaf:bark", "" },
            // Not weapons: lanterns, a censer, a balance.
            { "offhand.hung:lantern", "" }, { "offhand.hung:shuttered", "" }, { "offhand.hung:moss", "" }, { "offhand.hung:censer", "" }, { "offhand.hung:scale", "" },
        };

        /// <summary>The old drawing's length, butt to tip (a shield: its height), in the weapon or shield frame.</summary>
        public static readonly Dictionary<string, float> Lengths = new Dictionary<string, float>
        {
            { "sword.short", .85f }, { "sword.arming", 1.15f }, { "sword.falchion", 1.0f }, { "sword.sabre", 1.12f }, { "sword.leaf", 1.02f }, { "sword.great", 1.45f },
            { "knife", .56f }, { "knife.off", .56f }, { "axe.hand", .75f }, { "axe.bearded", .87f }, { "axe.crescent", .83f }, { "cleaver", .68f },
            { "club", .7f }, { "mace.flanged", .79f }, { "hammer.war", .8f }, { "mace.root", .8f }, { "polearm", 1.92f }, { "staff", 1.6f },
            { "shield.buckler", .38f }, { "shield.round", .58f }, { "shield.heater", .53f }, { "shield.kite", .66f }, { "shield.leaf", .6f },
        };

        // ---------- the PBR weapons (Cody Dream Studios, "PBR medieval Weapons", Asset Store EULA; play note 249) ----------
        // Five realistic steel pieces with their own PBR materials (Resources/Props/PBRWeapons): a long cross-hilted sword, a
        // bearded war axe, a flanged mace, a war spear and a domed round steel shield. They are the heavier steel tier: a rare or
        // epic piece of these variants wears one in place of its KayKit model (the cartoon look stays for common and uncommon).
        // Their pivots are not at the grip (the pack's own placing): the grip is measured along the length from the butt, on the
        // line of the butt's own middle (ActorVisual.ModelGrip), from their meshes (2026-10-19): the sword's wrapped grip between
        // the pommel and the guard 15% up, the axe's and the mace's hand 20% up the haft, the spear's 31% up as the old polearm's.
        public const string PbrPrefix = "pbr/";
        public const string PbrModels = "Props/PBRWeapons/Models/", PbrMaterials = "Props/PBRWeapons/Matereal/";
        public const int PbrQuality = 3;   // rare and epic
        public static readonly Dictionary<string, string> Pbr = new Dictionary<string, string>
        {
            { "sword.arming:straight", "pbr/Sword" }, { "sword.arming:disc", "pbr/Sword" }, { "sword.great:steel", "pbr/Sword" },
            { "mace.flanged:six", "pbr/Mace" },
            { "polearm:spear", "pbr/Spear" },
            { "shield.round:boards", "pbr/shield " }, { "shield.round:hide", "pbr/shield " },   // the pack's shield is round (a heater keeps KayKit's heater)
            // The PBR war axe is the rare bearded and crescent axe; at epic the ornate Sky Den axes take over (AxeFor).
        };
        public static bool IsPbr(string model) { return model != null && model.StartsWith(PbrPrefix); }

        // ---------- the Mid poly Axes Collection (Sky Den Games, Asset Store EULA; play note 250) ----------
        // Twenty axes in one FBX (Resources/Props/MidPolyAxes/Axes.fbx), one node each, Axe01 to Axe20, every one with its pivot at
        // the butt and its head up +Y (measured from the FBX, 2026-10-19), on the pack's two Standard materials. Picked by family,
        // variant, quality, level band and zone palette (AxeFor): plain wooden-hafted axes and hatchets for the low bands, steel
        // bearded and crescent axes higher up, the dark ones for the dark palettes (Khaven, the Ashwalkers, the Cult), the ornate
        // ones at epic. The grip is 20% up the haft from the butt, as the drawn axes held it.
        public const string AxesPrefix = "axes/", AxesSet = "Props/MidPolyAxes/Axes";
        public const float AxeGrip = .2f;
        public static bool IsAxes(string model) { return model != null && model.StartsWith(AxesPrefix); }
        static bool Dark(string palette) { return palette == "khaven" || palette == "ashwalker" || palette == "cult"; }
        /// <summary>
        /// The axe for an axe family's piece (null: not one of the axe families; the cleaver stays KayKit's). Hand axes by level band:
        /// wedge Axe03, Axe04, then Axe05, the steel-bitted Axe18 at rare and better; hatchet Axe07, Axe10, Axe08, then Axe09 (the top band). Bearded: Axe06
        /// in the first two bands, Axe16 after, the hooked one Axe17; crescent: Axe11, then Axe14 from the fourth band, the dark Axe20
        /// in a dark palette; spiked: Axe12, the dark spiked Axe01. Rare bearded and crescent axes are the PBR war axe; epic ones the
        /// ornate: the bearded Axe13, the gold double-bit Axe02 (the bone Axe19 in a dark palette), the spiked round Axe15.
        /// </summary>
        public static string AxeFor(string family, string variant, int quality, int tier, string palette)
        {
            bool dark = Dark(palette);
            switch (family)
            {
                case "axe.hand":
                    if (variant == "hatchet") return AxesPrefix + (tier <= 1 ? "Axe07" : tier <= 3 ? "Axe10" : tier == 4 ? "Axe08" : "Axe09");
                    return AxesPrefix + (quality >= 3 ? "Axe18" : tier <= 1 ? "Axe03" : tier <= 3 ? "Axe04" : "Axe05");
                case "axe.bearded":
                    if (quality == 3) return "pbr/Axe";
                    if (quality >= 4) return AxesPrefix + "Axe13";
                    return AxesPrefix + (variant == "hooked" ? "Axe17" : tier <= 1 ? "Axe06" : "Axe16");
                case "axe.crescent":
                    if (quality == 3) return "pbr/Axe";
                    if (quality >= 4) return AxesPrefix + (variant == "spiked" ? "Axe15" : dark ? "Axe19" : "Axe02");
                    if (variant == "spiked") return AxesPrefix + (dark ? "Axe01" : "Axe12");
                    return AxesPrefix + (dark ? "Axe20" : tier <= 2 ? "Axe11" : "Axe14");
            }
            return null;
        }

        /// <summary>A model's grip as a fraction of its length from the butt, where its pivot is not the grip (the PBR and Sky Den weapons); null for KayKit's.</summary>
        public static float? GripAlong(string model)
        {
            if (IsAxes(model)) return AxeGrip;
            switch (model) { case "pbr/Sword": return .15f; case "pbr/Axe": return .2f; case "pbr/Mace": return .2f; case "pbr/Spear": return .31f; }
            return null;
        }
        /// <summary>The Resources path of a model id (a KayKit name, "pbr/" and the PBR model's name, or "axes/" and the axe's node).</summary>
        public static string PathOf(string model) { return IsPbr(model) ? PbrModels + model.Substring(PbrPrefix.Length) : IsAxes(model) ? AxesSet : Folder + model; }
        /// <summary>The node inside the loaded model to use (an axe of the Sky Den set); null for the whole model.</summary>
        public static string NodeOf(string model) { return IsAxes(model) ? model.Substring(AxesPrefix.Length) : null; }
        /// <summary>A PBR model's own material (built-in Standard, rebuilt from the pack's URP ones); null for KayKit's and the axes' (they keep the FBX's).</summary>
        public static string MaterialOf(string model)
        {
            if (!IsPbr(model)) return null; string n = model.Substring(PbrPrefix.Length).Trim();
            return PbrMaterials + (n == "shield" ? "Shield" : n);
        }

        /// <summary>
        /// The model for this look (an axe family's from AxeFor; a rare or epic steel piece's PBR one; else KayKit's), or null for the
        /// old drawing. <paramref name="tier"/> is the level band (GearLook.tier), <paramref name="palette"/> the zone's palette.
        /// </summary>
        public static string ModelFor(string family, string variant, int quality = 0, int tier = 0, string palette = null)
        {
            if (family == null) return null; string key = family + ":" + variant;
            if (!Map.TryGetValue(key, out var m) || m.Length == 0) return null;
            var axe = AxeFor(family, variant, quality, tier, palette); if (axe != null) return axe;
            if (quality >= PbrQuality && Pbr.TryGetValue(key, out var p)) return p;
            return m;
        }
        public static bool IsShield(string family) { return family != null && family.StartsWith("shield."); }
        public static Kind KindOf(string family) { return IsShield(family) ? Kind.Shield : Kind.Weapon; }
        public static float LengthOf(string family) { return family != null && Lengths.TryGetValue(family, out var l) ? l : .95f; }
        static readonly string[] SamplePalettes = { "oakhaven", "khaven" };
        /// <summary>
        /// Every model a mapped variant can wear, with the first quality, level band and palette that gives it (for the tests and the
        /// wardrobe): (family, variant, quality, tier, palette, model).
        /// </summary>
        public static List<(string family, string variant, int quality, int tier, string palette, string model)> Wearable
        {
            get
            {
                var list = new List<(string, string, int, int, string, string)>(); var seen = new HashSet<string>();
                foreach (var key in Map.Keys)
                {
                    var fv = key.Split(':');
                    for (int q = 0; q <= 4; q++) for (int t = 0; t <= 5; t++) foreach (var p in SamplePalettes)
                    {
                        var m = ModelFor(fv[0], fv[1], q, t, p); if (m != null && seen.Add(key + "=" + m)) list.Add((fv[0], fv[1], q, t, p, m));
                    }
                }
                return list;
            }
        }
        /// <summary>Every model any variant wears, and the Ranger's bow.</summary>
        public static IEnumerable<string> Models
        {
            get
            {
                var seen = new HashSet<string>(); foreach (var w in Wearable) if (seen.Add(w.model)) yield return w.model;
                if (seen.Add(ClassBow)) yield return ClassBow;
            }
        }

        // ---------- the atlas's swatches (weapons_bits_texture.png: 8 columns by 4 rows of vertical gradients) ----------
        // Measured from the pack's glTF UVs (2026-10-19): the blades, heads and rims are row 2 column 7 and row 2 column 6 (a blue
        // steel and a pale steel, counted from 1 at the top left); the hafts and grips row 3 column 5 (a brown wood). The wraps,
        // the gems and the painted faces are left as they are.
        /// <summary>True for the steel swatches, counted from the top-left (row 0 the top).</summary>
        public static bool MetalCell(int row, int col) { return row == 1 && (col == 5 || col == 6); }
        public static bool WoodCell(int row, int col) { return row == 2 && col == 4; }
        /// <summary>Whether a weapon's steel and wood take its palette's colours (on), or the pack's own (off).</summary>
        public static bool Tint = true;
        public const float MetalTint = .6f, WoodTint = .45f;
    }
}
