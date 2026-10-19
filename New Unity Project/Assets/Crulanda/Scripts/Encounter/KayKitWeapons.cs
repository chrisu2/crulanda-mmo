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

        /// <summary>The KayKit model for this look, or null for the old drawing (a fallback, a family that is not a weapon, an unknown one).</summary>
        public static string ModelFor(string family, string variant)
        {
            return family != null && Map.TryGetValue(family + ":" + variant, out var m) && m.Length > 0 ? m : null;
        }
        public static bool IsShield(string family) { return family != null && family.StartsWith("shield."); }
        public static Kind KindOf(string family) { return IsShield(family) ? Kind.Shield : Kind.Weapon; }
        public static float LengthOf(string family) { return family != null && Lengths.TryGetValue(family, out var l) ? l : .95f; }
        /// <summary>Every model the map uses, and the Ranger's bow.</summary>
        public static IEnumerable<string> Models
        {
            get { var seen = new HashSet<string>(); foreach (var m in Map.Values) if (m.Length > 0 && seen.Add(m)) yield return m; if (seen.Add(ClassBow)) yield return ClassBow; }
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
