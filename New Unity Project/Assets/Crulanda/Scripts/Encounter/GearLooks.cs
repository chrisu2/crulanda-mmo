using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Gear looks: what an item looks like when worn. A look is "family:variant/palette" plus quality trim, worked out from the
    // item id and its ItemDef every time (never saved). Named and crafted items name their look; generated gear gets one from
    // the two words of its name (the material picks the palette, the piece the family); anything else wears its slot's
    // fallback. The data is Resources/Gear/looks.json.
    // ============================================================================================
    [Serializable] public sealed class GearLookFile
    {
        public GearPalette[] palettes = new GearPalette[0];
        public GearMaterialWord[] materials = new GearMaterialWord[0];
        public GearPieceWord[] words = new GearPieceWord[0];
        public GearFallback[] fallbacks = new GearFallback[0];
        public GearTint[] tints = new GearTint[0];
        public GearLookDef[] looks = new GearLookDef[0];
    }
    /// <summary>A region's colours, as "#RRGGBB": two cloths, leather, metal, trim, wood and the glow of its rare and epic accents.</summary>
    [Serializable] public sealed class GearPalette { public string id, cloth, cloth2, leather, metal, trim, wood, glow; }
    /// <summary>A generated name's material word: its palette, a shade (0 as is, 1 darker and cooler, 2 lighter and warmer) and a small detail part.</summary>
    [Serializable] public sealed class GearMaterialWord { public string word, palette, detail; public int shade; }
    /// <summary>A generated name's piece word in a slot: one family for every level, or five by level band (1-2, 3-5, 6-8, 9-10, 11-13).</summary>
    [Serializable] public sealed class GearPieceWord { public string slot, word; public string[] families = new string[0]; }
    [Serializable] public sealed class GearFallback { public string slot, family; }
    /// <summary>Items whose id starts with <c>prefix</c> have their metal replaced by this colour (a crafted metal tier).</summary>
    [Serializable] public sealed class GearTint { public string prefix, metal; }
    [Serializable] public sealed class GearLookDef { public string item, look; }

    /// <summary>A shape family: its slot, its variants (the first <see cref="gen"/> may be given to generated gear; the rest are for named items).</summary>
    public sealed class GearFamily
    {
        public readonly string name, slot; public readonly string[] variants; public readonly int gen;
        public GearFamily(string name, string slot, int gen, params string[] variants) { this.name = name; this.slot = slot; this.gen = gen; this.variants = variants; }
    }

    /// <summary>
    /// A resolved look. Colours are final (palette, shade, metal tint and quality applied). Quality: poor is faded with one wear
    /// detail and no trim; common has plain trim; uncommon has the palette's trim and one extra part; rare polished trim and one
    /// glowing accent; epic gold-toned trim and two pulsing accents. <c>+glow</c> in a look string forces at least one accent.
    /// </summary>
    public struct GearLook
    {
        public string family, variant, palette, detail; public int quality;
        public Color cloth, cloth2, leather, metal, trim, wood, glow, bone;
        /// <summary>The look string asked for a glow.</summary>
        public bool forceGlow;
        /// <summary>Nothing matched: the slot's fallback family in the oakhaven palette.</summary>
        public bool fallback;
        /// <summary>Trim: whether there is any (not on poor), its colour, smoothness and metalness.</summary>
        public bool hasTrim; public Color trimColor; public float trimSmooth, trimMetal;
        /// <summary>Glowing accents (0, rare 1, epic 2), their emission strength and whether they pulse (epic).</summary>
        public int accents; public float glowPower; public bool pulse;
        /// <summary>Poor gear carries a wear mark; uncommon and better one extra detail part.</summary>
        public bool worn, extra;
        /// <summary>How worked the piece is, 0 to 4, from its level band (1-2 ... 11-13): armour grows more plates, rims and crests as it climbs.</summary>
        public int tier;
    }

    public sealed class GearLooks
    {
        /// <summary>Every family and variant (DESIGN.md section 2.3). Variants after the first <c>gen</c> are named-item only.</summary>
        public static readonly GearFamily[] Families = {
            // Main hand.
            new GearFamily("sword.short", "mainhand", 2, "plain", "notched"),
            new GearFamily("sword.arming", "mainhand", 3, "straight", "curved", "disc"),
            new GearFamily("sword.falchion", "mainhand", 2, "clipped", "heavy"),
            new GearFamily("sword.sabre", "mainhand", 0, "officer", "broken"),
            new GearFamily("sword.leaf", "mainhand", 2, "bronze", "root"),
            new GearFamily("sword.great", "mainhand", 0, "steel", "wood", "living"),
            new GearFamily("knife", "mainhand", 2, "hooked", "needle", "glass", "sickle"),
            new GearFamily("axe.hand", "mainhand", 2, "wedge", "hatchet"),
            new GearFamily("axe.bearded", "mainhand", 2, "plain", "hooked"),
            new GearFamily("axe.crescent", "mainhand", 2, "plain", "spiked"),
            new GearFamily("cleaver", "mainhand", 0, "slab", "notched"),
            new GearFamily("club", "mainhand", 3, "plain", "studded", "bound", "tusk", "tankard"),
            new GearFamily("mace.flanged", "mainhand", 1, "six", "fist"),
            new GearFamily("hammer.war", "mainhand", 2, "pick", "maul"),
            new GearFamily("mace.root", "mainhand", 1, "burl", "antler", "briar"),
            new GearFamily("polearm", "mainhand", 0, "spear", "harpoon", "fork", "billhook", "spade"),
            new GearFamily("staff", "mainhand", 1, "knob", "crook", "forked", "skull"),
            // Off hand.
            new GearFamily("shield.buckler", "offhand", 1, "plain", "tusk", "chitin"),
            new GearFamily("shield.round", "offhand", 2, "boards", "hide", "cask", "lid"),
            new GearFamily("shield.heater", "offhand", 2, "plain", "striped", "glass"),
            new GearFamily("shield.kite", "offhand", 1, "plain", "slab"),
            new GearFamily("shield.leaf", "offhand", 1, "bronze", "bark"),
            new GearFamily("offhand.hung", "offhand", 1, "lantern", "shuttered", "moss", "censer", "scale"),
            // Head.
            new GearFamily("head.cap", "head", 2, "plain", "flaps", "leaf"),
            new GearFamily("head.hood", "head", 1, "cloth", "oilskin"),
            new GearFamily("head.coif", "head", 1, "mail"),
            new GearFamily("head.wrap", "head", 0, "scarf"),
            new GearFamily("head.kettle", "head", 1, "plain", "half"),
            new GearFamily("head.barbute", "head", 1, "plain", "rimed"),
            new GearFamily("head.mask", "head", 0, "bone", "tear"),
            new GearFamily("head.circlet", "head", 0, "band", "briar"),
            new GearFamily("head.crown", "head", 0, "tin", "root", "antler"),
            // Shoulders, chest, hands, legs, feet, neck.
            new GearFamily("shoulder.mantle", "shoulders", 2, "cloth", "fur", "hide", "frayed", "shawl"),
            new GearFamily("shoulder.pauldron", "shoulders", 1, "dome", "bone"),
            new GearFamily("shoulder.spaulder", "shoulders", 1, "lames", "glass", "bark"),
            new GearFamily("chest.tunic", "chest", 1, "plain"),
            new GearFamily("chest.jerkin", "chest", 2, "leather", "hide", "scale"),
            new GearFamily("chest.hauberk", "chest", 1, "mail"),
            new GearFamily("chest.coat", "chest", 0, "grey", "skirted"),
            new GearFamily("chest.cuirass", "chest", 0, "plate", "bark"),
            new GearFamily("chest.robe", "chest", 0, "vestment", "cassock", "shroud"),
            new GearFamily("hands.gloves", "hands", 1, "plain", "fingerless", "mitts"),
            new GearFamily("hands.wraps", "hands", 1, "cloth", "fur", "silk"),
            new GearFamily("hands.gauntlets", "hands", 1, "plate", "bone", "glass", "root"),
            new GearFamily("legs.breeches", "legs", 2, "plain", "patched"),
            new GearFamily("legs.leggings", "legs", 1, "garter", "hide"),
            new GearFamily("legs.greaves", "legs", 1, "plate", "full"),
            new GearFamily("legs.kilt", "legs", 0, "bone"),
            new GearFamily("feet.shoes", "feet", 1, "plain"),
            new GearFamily("feet.boots", "feet", 1, "plain", "waders", "hobnail", "root"),
            new GearFamily("feet.sabatons", "feet", 1, "plate"),
            new GearFamily("neck.pendant", "neck", 1, "drop", "glass", "seal", "signet", "ember", "leaf", "vial", "jar"),
            new GearFamily("neck.cord", "neck", 1, "beads", "knot", "fang", "tooth", "tine"),
            new GearFamily("neck.torc", "neck", 1, "metal", "band", "wood", "charm")
        };
        static Dictionary<string, GearFamily> byName;
        public static GearFamily Family(string name)
        {
            if (byName == null) { byName = new Dictionary<string, GearFamily>(StringComparer.Ordinal); foreach (var f in Families) byName[f.name] = f; }
            return name != null && byName.TryGetValue(name, out var g) ? g : null;
        }
        /// <summary>Bone and horn (tusks, skulls, harpoon heads): the same in every palette.</summary>
        public static readonly Color Bone = new Color(.86f, .82f, .72f);
        /// <summary>Epic trim leans toward this gold.</summary>
        public static readonly Color Gold = new Color(.85f, .66f, .2f);

        readonly Dictionary<string, GearPalette> palettes = new Dictionary<string, GearPalette>(StringComparer.Ordinal);
        readonly Dictionary<string, GearMaterialWord> materials = new Dictionary<string, GearMaterialWord>(StringComparer.Ordinal);
        readonly Dictionary<string, GearPieceWord> words = new Dictionary<string, GearPieceWord>(StringComparer.Ordinal);
        readonly Dictionary<string, string> fallbacks = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly List<(string prefix, Color metal)> tints = new List<(string, Color)>();
        readonly Dictionary<string, string> explicitLooks = new Dictionary<string, string>(StringComparer.Ordinal);
        public IEnumerable<string> PaletteIds { get { return palettes.Keys; } }
        /// <summary>Items with a look of their own (the looks table and everything registered since), by id.</summary>
        public IReadOnlyDictionary<string, string> Explicit { get { return explicitLooks; } }

        static GearLooks loaded; static bool failed;
        /// <summary>The game's looks (Resources/Gear/looks.json), parsed once. Null, with one error logged, when the file is missing or invalid.</summary>
        public static GearLooks Load()
        {
            if (loaded != null || failed) return loaded;
            var text = Resources.Load<TextAsset>("Gear/looks");
            if (text == null) { failed = true; Debug.LogError("Gear looks missing: Resources/Gear/looks.json."); return null; }
            try { loaded = Parse(text.text); }
            catch (ArgumentException e) { failed = true; Debug.LogError("Gear looks invalid:\n" + e.Message); return null; }
            return loaded;
        }

        /// <summary>Reads a looks file. Every problem is collected and thrown together as one ArgumentException.</summary>
        public static GearLooks Parse(string json)
        {
            var g = new GearLooks(); var errors = new List<string>();
            GearLookFile f = null;
            try { f = JsonUtility.FromJson<GearLookFile>(json); } catch (Exception e) { errors.Add("Unreadable looks file: " + e.Message); }
            if (f == null) { errors.Add("The looks file is empty."); throw new ArgumentException(string.Join("\n", errors)); }
            foreach (var p in f.palettes ?? new GearPalette[0])
            {
                if (p == null || string.IsNullOrEmpty(p.id)) { errors.Add("A palette has no id."); continue; }
                if (g.palettes.ContainsKey(p.id)) { errors.Add("Duplicate palette '" + p.id + "'."); continue; }
                foreach (var (field, hex) in new[] { ("cloth", p.cloth), ("cloth2", p.cloth2), ("leather", p.leather), ("metal", p.metal), ("trim", p.trim), ("wood", p.wood), ("glow", p.glow) })
                    if (!ColorUtility.TryParseHtmlString(hex ?? "", out _)) errors.Add("Palette '" + p.id + "' has a bad " + field + " colour '" + hex + "'.");
                g.palettes[p.id] = p;
            }
            foreach (var m in f.materials ?? new GearMaterialWord[0])
            {
                if (m == null || string.IsNullOrEmpty(m.word)) { errors.Add("A material has no word."); continue; }
                if (!g.palettes.ContainsKey(m.palette ?? "")) errors.Add("Material '" + m.word + "' names unknown palette '" + m.palette + "'.");
                if (m.shade < 0 || m.shade > 2) errors.Add("Material '" + m.word + "' has shade " + m.shade + " (0, 1 or 2).");
                if (g.materials.ContainsKey(m.word)) errors.Add("Duplicate material '" + m.word + "'.");
                g.materials[m.word] = m;
            }
            foreach (var w in f.words ?? new GearPieceWord[0])
            {
                if (w == null || string.IsNullOrEmpty(w.word) || ItemDatabase.SlotIndex(w.slot) < 0) { errors.Add("A piece word has no word or no valid slot."); continue; }
                var fams = w.families ?? new string[0];
                if (fams.Length != 1 && fams.Length != 5) errors.Add("Piece '" + w.slot + " " + w.word + "' needs 1 or 5 families, not " + fams.Length + ".");
                foreach (var name in fams)
                {
                    var fam = Family(name);
                    if (fam == null) errors.Add("Piece '" + w.slot + " " + w.word + "' names unknown family '" + name + "'.");
                    else if (fam.slot != w.slot) errors.Add("Piece '" + w.slot + " " + w.word + "' names " + name + ", a " + fam.slot + " family.");
                    else if (fam.gen < 1) errors.Add("Piece '" + w.slot + " " + w.word + "' names " + name + ", which generated gear may not use.");
                }
                string key = w.slot + "|" + w.word;
                if (g.words.ContainsKey(key)) errors.Add("Duplicate piece '" + w.slot + " " + w.word + "'.");
                g.words[key] = w;
            }
            foreach (var fb in f.fallbacks ?? new GearFallback[0])
            {
                var fam = fb == null ? null : Family(fb.family);
                if (fb == null || ItemDatabase.SlotIndex(fb.slot) < 0) { errors.Add("A fallback has no valid slot."); continue; }
                if (fam == null || fam.slot != fb.slot) { errors.Add("The " + fb.slot + " fallback '" + fb.family + "' is not a " + fb.slot + " family."); continue; }
                g.fallbacks[fb.slot] = fb.family;
            }
            foreach (var slot in ItemDatabase.SlotIds) if (!g.fallbacks.ContainsKey(slot)) errors.Add("No fallback family for slot '" + slot + "'.");
            if (!g.palettes.ContainsKey("oakhaven")) errors.Add("The fallback palette 'oakhaven' is missing.");
            foreach (var t in f.tints ?? new GearTint[0])
            {
                if (t == null || string.IsNullOrEmpty(t.prefix) || !ColorUtility.TryParseHtmlString(t.metal ?? "", out var c)) { errors.Add("A tint has no prefix or a bad colour."); continue; }
                g.tints.Add((t.prefix, c));
            }
            foreach (var l in f.looks ?? new GearLookDef[0])
            {
                if (l == null || string.IsNullOrEmpty(l.item)) { errors.Add("A look has no item."); continue; }
                if (!g.IsValid(l.look, out var why)) { errors.Add("Item '" + l.item + "': " + why); continue; }
                if (g.explicitLooks.ContainsKey(l.item)) errors.Add("Duplicate look for '" + l.item + "'.");
                g.explicitLooks[l.item] = l.look;
            }
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            return g;
        }

        /// <summary>Gives an item its own look (named loot when the loot database loads). Throws ArgumentException for a bad look string.</summary>
        public void Register(string itemId, string look)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("A look needs an item id.");
            if (!IsValid(look, out var why)) throw new ArgumentException("Item '" + itemId + "': " + why);
            explicitLooks[itemId] = look;
        }
        /// <summary>True when the look string parses and names a real family, one of its variants and a palette in this file.</summary>
        public bool IsValid(string look, out string why)
        {
            why = null;
            if (!TryParseLook(look, out var family, out _, out var palette, out _)) { why = "bad look '" + look + "' (family:variant/palette[+glow], with a known family and variant)."; return false; }
            if (!palettes.ContainsKey(palette)) { why = "look '" + look + "' names unknown palette '" + palette + "'."; return false; }
            return Family(family) != null;
        }

        /// <summary>
        /// Splits "family[:variant]/palette[+glow]". False when it does not parse, the family is unknown or the variant is not one
        /// of the family's. A missing variant means the family's first. The palette is not checked here (see <see cref="IsValid"/>).
        /// </summary>
        public static bool TryParseLook(string s, out string family, out string variant, out string palette, out bool glow)
        {
            family = variant = palette = null; glow = false;
            if (string.IsNullOrEmpty(s)) return false;
            if (s.EndsWith("+glow", StringComparison.Ordinal)) { glow = true; s = s.Substring(0, s.Length - 5); }
            int slash = s.IndexOf('/');
            if (slash <= 0 || slash != s.LastIndexOf('/') || slash == s.Length - 1) return false;
            palette = s.Substring(slash + 1); string left = s.Substring(0, slash);
            if (palette.IndexOf('+') >= 0 || palette.IndexOf(':') >= 0) return false;
            int colon = left.IndexOf(':');
            if (colon >= 0 && (colon != left.LastIndexOf(':') || colon == 0 || colon == left.Length - 1)) return false;
            family = colon < 0 ? left : left.Substring(0, colon);
            var fam = Family(family); if (fam == null) return false;
            variant = colon < 0 ? fam.variants[0] : left.Substring(colon + 1);
            return Array.IndexOf(fam.variants, variant) >= 0;
        }

        /// <summary>
        /// A generated item's name words and id numbers: "Worn " is dropped and everything from " of " on; the last word left is
        /// the piece (Blade, Hauberk) and the rest the material (Ridge-forged). False for anything not generated.
        /// </summary>
        public static bool TrySplitGenerated(ItemDef d, out string material, out string piece, out int level, out int seed)
        {
            material = piece = null; level = seed = 0;
            if (d == null || d.id == null || !d.id.StartsWith("gen.", StringComparison.Ordinal) || string.IsNullOrEmpty(d.name)) return false;
            var parts = d.id.Split('.');
            if (parts.Length != 5 || !int.TryParse(parts[2], out level) || !int.TryParse(parts[4], out seed)) return false;
            string n = d.name;
            if (n.StartsWith("Worn ", StringComparison.Ordinal)) n = n.Substring(5);
            int of = n.IndexOf(" of ", StringComparison.Ordinal); if (of >= 0) n = n.Substring(0, of);
            int space = n.LastIndexOf(' '); if (space <= 0) return false;
            material = n.Substring(0, space); piece = n.Substring(space + 1);
            return true;
        }
        /// <summary>The resolver's own copy of the level bands (ItemDatabase's is private): 1-2, 3-5, 6-8, 9-10, 11-13.</summary>
        public static int BandOf(int level) { return level <= 2 ? 0 : level <= 5 ? 1 : level <= 8 ? 2 : level <= 10 ? 3 : 4; }
        /// <summary>A short key for a resolved look (family, variant, palette, quality, glow, metal, detail and tier), for caching rendered icons.</summary>
        public static string LookKey(GearLook l)
        {
            return l.family + ":" + l.variant + "/" + l.palette + "#" + l.quality + (l.forceGlow ? "+glow" : "") + "@" + ColorUtility.ToHtmlStringRGB(l.metal) + "~" + l.detail + "^" + l.tier;
        }

        /// <summary>The look of an item: its own look, else its generated name's, else its slot's fallback. Never fails.</summary>
        public GearLook Resolve(ItemDef d)
        {
            string slot = d != null && ItemDatabase.SlotIndex(d.slot) >= 0 ? d.slot : "mainhand";
            int q = d == null ? 1 : Mathf.Clamp(d.quality, 0, 4);
            string id = d != null ? d.id ?? "" : "";
            int tier = BandOf(Mathf.Clamp(d == null ? 1 : d.level + 1, 1, EncounterProgress.LevelCap));   // the curve level: required level + 1 (DESIGN.md 3.2)
            // Its own look, if it names a family of the item's slot.
            if (explicitLooks.TryGetValue(id, out var own) && TryParseLook(own, out var fam, out var variant, out var pal, out bool glow) && Family(fam).slot == slot && palettes.ContainsKey(pal))
                return Tint(Make(fam, variant, pal, 0, "none", q, glow, false, tier), id);
            // Generated: the material word picks the palette, the piece word (by level band) the family, the seed the variant.
            if (TrySplitGenerated(d, out var material, out var piece, out int level, out int seed)
                && materials.TryGetValue(material, out var m) && words.TryGetValue(slot + "|" + piece, out var w) && w.families.Length > 0)
            {
                var family = Family(w.families.Length == 1 ? w.families[0] : w.families[Mathf.Clamp(BandOf(level), 0, w.families.Length - 1)]);
                if (family != null && family.gen > 0)
                    return Tint(Make(family.name, family.variants[Mathf.Abs(seed / 7) % family.gen], m.palette, m.shade, m.detail, q, false, false, BandOf(Mathf.Clamp(level, 1, EncounterProgress.LevelCap))), id);
            }
            var fb = Family(fallbacks.TryGetValue(slot, out var f) ? f : "sword.arming");
            return Tint(Make(fb.name, fb.variants[0], "oakhaven", 0, "none", q, false, true, tier), id);
        }

        GearLook Tint(GearLook l, string id)
        {
            foreach (var (prefix, metal) in tints) if (id.StartsWith(prefix, StringComparison.Ordinal)) { l.metal = l.quality == 0 ? Faded(metal) : metal; if (l.quality == 1) l.trimColor = l.metal; break; }
            return l;
        }
        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s ?? "", out var c); c.a = 1; return c; }
        /// <summary>Shade 1: 12% darker and a little cooler. Shade 2: 12% lighter and a little warmer.</summary>
        static Color Shade(Color c, int shade)
        {
            if (shade == 1) return new Color(c.r * .88f * .97f, c.g * .88f, Mathf.Min(1, c.b * .88f * 1.04f));
            if (shade == 2) { var l = Color.Lerp(c, Color.white, .12f); return new Color(Mathf.Min(1, l.r * 1.03f), l.g, l.b * .96f); }
            return c;
        }
        /// <summary>Poor gear: 35% toward grey and 15% darker.</summary>
        static Color Faded(Color c) { float g = c.r * .3f + c.g * .59f + c.b * .11f; return Color.Lerp(c, new Color(g, g, g), .35f) * .85f; }

        GearLook Make(string family, string variant, string paletteId, int shade, string detail, int q, bool glow, bool fallback, int tier)
        {
            var p = palettes[paletteId];
            Color C(string hex) { var c = Shade(Hex(hex), shade); return q == 0 ? Faded(c) : c; }
            var l = new GearLook {
                family = family, variant = variant, palette = paletteId, detail = string.IsNullOrEmpty(detail) ? "none" : detail, quality = q, forceGlow = glow, fallback = fallback, tier = tier,
                cloth = C(p.cloth), cloth2 = C(p.cloth2), leather = C(p.leather), metal = C(p.metal), trim = C(p.trim), wood = C(p.wood), glow = Hex(p.glow), bone = q == 0 ? Faded(Bone) : Bone
            };
            Quality(ref l, q);
            if (glow) { l.accents = Mathf.Max(l.accents, 1); l.glowPower = Mathf.Max(l.glowPower, 1.5f); }
            return l;
        }
        /// <summary>The quality treatment (DESIGN.md section 2.4) on a look whose colours are set.</summary>
        static void Quality(ref GearLook l, int q)
        {
            l.worn = q == 0; l.extra = q >= 2; l.hasTrim = q >= 1;
            switch (q)
            {
                case 0: l.trimColor = l.metal; l.trimSmooth = .15f; l.trimMetal = .2f; break;
                case 1: l.trimColor = l.metal; l.trimSmooth = .35f; l.trimMetal = .4f; break;
                case 2: l.trimColor = l.trim; l.trimSmooth = .5f; l.trimMetal = .6f; break;
                case 3: l.trimColor = l.trim; l.trimSmooth = .7f; l.trimMetal = .8f; l.accents = 1; l.glowPower = 1.5f; break;
                default: l.trimColor = Color.Lerp(l.trim, GearLooks.Gold, .6f); l.trimSmooth = .78f; l.trimMetal = .9f; l.accents = 2; l.glowPower = 2.5f; l.pulse = true; break;
            }
        }
    }
}
