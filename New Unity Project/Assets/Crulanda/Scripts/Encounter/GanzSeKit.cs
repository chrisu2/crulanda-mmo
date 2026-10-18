using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Phase 8.5, step 1 (ROADMAP "Phase 8.5 plan"; Chris chose route (a), rebuilding people on a modular pack's body, and the pack,
    /// verbatim "GanzSe (Recommended)", 2026-10-18): GanzSe's FREE Modular Character - Fantasy Low Poly Pack (Asset Store; its
    /// Built-In model, material and palette in Resources/Characters/GanzSe). One male body on one Humanoid skeleton with every
    /// piece skinned to it: armour for six slots (Head, Chest, Arm, Belt, Legs, Feet) in six types and three colourways each, five
    /// hairstyles, beards, brows and eyes in five colours each, five noses, two pairs of ears. All in one palette texture (a grid
    /// of flat colour swatches), so a piece's colour is its own; skin is the one tint.
    ///
    /// This is the one table that says which piece a worn item shows (<see cref="Map"/>), what a person wears with nothing in a
    /// slot (<see cref="Outfit"/>) and which hair, face and colours a figure gets. The figure itself is ModelFigure.GanzSe.cs;
    /// ActorVisual.GanzSe.cs skips the drawn armour for a slot a piece covers. Behind <see cref="On"/> (off by default: the game is
    /// unchanged until the lead turns it on after a wardrobe capture; <c>--crulanda-ganzse</c> on the command line turns it on).
    ///
    /// The types as they read (a contact sheet drawn from the file, 2026-10-18; the README names none): 1 a steel breastplate on
    /// mail with shoulder pads (mail); 2 black spiked plate (plate); 3 bone and hide, a crop top and a skirt of strips, a horned hood
    /// (hide); 4 a sleeved coat with a medallion and a skirt, a skull helm (the caster's: cloth); 5 a jerkin with a scarf, a belt
    /// with a buckle, a brimmed hat (leather); 6 fur and bone with horns (fur). There is no robe, hood or plain shirt in the pack.
    /// </summary>
    public static class GanzSeKit
    {
        // ------------------------------------------------------------------------------------------------- the switch
        /// <summary>The switch's default: off, so nothing changes until a wardrobe capture with it on has been looked at.</summary>
        public const bool DefaultOn = false;
        /// <summary>The command-line switch that turns the kit on (a capture, a preview build).</summary>
        public const string Flag = "--crulanda-ganzse";
        static bool? on;
        /// <summary>People are built on GanzSe's body (read when a figure is built). Default <see cref="DefaultOn"/>, or on with <see cref="Flag"/>.</summary>
        public static bool On
        {
            get { if (on == null) on = DefaultOn || Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; return on.Value; }
            set { on = value; }
        }
        /// <summary>Women on the pack's body too. Off: the pack has only a man's body, so women keep the Quaternius figure.</summary>
        public static bool Women = false;

        // ------------------------------------------------------------------------------------------------- the file
        /// <summary>The model (Resources path) and its material (the Built-In one: Standard on the palette texture).</summary>
        public const string ModelPath = "Characters/GanzSe/GanzSe Free Modular Character 1_1", MaterialPath = "Characters/GanzSe/Base Palette Material";
        /// <summary>The skeleton's root under the model, and the body.</summary>
        public const string SkeletonRoot = "Base Character Root", Body = "Base Character Mesh";
        public enum Slot { Head, Chest, Arm, Belt, Legs, Feet }
        public const int SlotCount = 6, Types = 6, Colourways = 3, FaceTypes = 5, FaceColours = 5, Noses = 5, Ears = 2;
        static readonly string[] SlotWords = { "Head", "Chest", "Arm", "Belt", "Legs", "Feet" };
        public static string Armour(Slot slot, int type, int colour) { return SlotWords[(int)slot] + " Armor Type " + type + " Color " + colour; }
        public static string Hair(int type, int colour) { return "Hair Type " + type + " Color " + colour; }
        public static string Beard(int type, int colour) { return "Face Hair Type " + type + " Color " + colour; }
        public static string Brows(int type, int colour) { return "Eyebrow Type " + type + " Color " + colour; }
        public static string Eyes(int type, int colour) { return "Eyes Type " + type + " Color " + colour; }
        public static string Nose(int type) { return "Nose Type " + type; }
        public static string EarsOf(int type) { return "Ears Type " + type; }
        /// <summary>The slot a piece name is for (armour only), or null.</summary>
        public static Slot? SlotOf(string piece)
        {
            if (piece == null) return null;
            for (int i = 0; i < SlotCount; i++) if (piece.StartsWith(SlotWords[i] + " Armor Type ", StringComparison.Ordinal)) return (Slot)i;
            return null;
        }
        /// <summary>A piece's type (1-6), or 0.</summary>
        public static int TypeOf(string piece)
        {
            if (piece == null) return 0; int i = piece.IndexOf(" Type ", StringComparison.Ordinal); if (i < 0) return 0;
            int n = 0; for (int k = i + 6; k < piece.Length && char.IsDigit(piece[k]); k++) n = n * 10 + (piece[k] - '0'); return n;
        }
        /// <summary>Is the scalp's hair (not the beard or the brows): hidden under a helm, measured for a hat.</summary>
        public static bool IsScalpHair(string name) { return name != null && name.StartsWith("Hair Type ", StringComparison.Ordinal); }

        static string[] all;
        /// <summary>Every skinned piece in the model file (its node names, checked against the file by GanzSeKitTests).</summary>
        public static string[] AllPieces
        {
            get
            {
                if (all != null) return all;
                var list = new List<string> { Body };
                for (int s = 0; s < SlotCount; s++) for (int t = 1; t <= Types; t++) for (int c = 1; c <= Colourways; c++) list.Add(Armour((Slot)s, t, c));
                for (int t = 1; t <= FaceTypes; t++) for (int c = 1; c <= FaceColours; c++) { list.Add(Hair(t, c)); list.Add(Beard(t, c)); list.Add(Brows(t, c)); list.Add(Eyes(t, c)); }
                for (int t = 1; t <= Noses; t++) list.Add(Nose(t));
                for (int t = 1; t <= Ears; t++) list.Add(EarsOf(t));
                return all = list.ToArray();
            }
        }

        // ------------------------------------------------------------------------------------------------- what a worn item shows
        /// <summary>How heavy a piece reads, which picks its type.</summary>
        public enum Weight { None, Cloth, Leather, Hide, Fur, Mail, Plate }
        /// <summary>The type each weight wears (see the class summary for how each reads).</summary>
        public static int TypeFor(Weight w)
        {
            switch (w) { case Weight.Mail: return 1; case Weight.Plate: return 2; case Weight.Hide: return 3; case Weight.Cloth: return 4; case Weight.Leather: return 5; case Weight.Fur: return 6; default: return 0; }
        }
        /// <summary>
        /// Every armour family's weight (GearLooks.Families; "family/variant" overrides the family). A family not here, or
        /// <see cref="Weight.None"/>, keeps its drawn piece (the pack has nothing like it): the cloth hood, the scarf,
        /// circlets and crowns on the head; the shoulders and the neck (the pack has no such slots; under any of its chest pieces the
        /// drawn shoulders are not drawn).
        /// </summary>
        static readonly Dictionary<string, Weight> Weights = new Dictionary<string, Weight>(StringComparer.Ordinal) {
            // Head (Chris, play note 239, the 04-sets-front line-up: "helms and gauntlet still tooo round"): every head piece the pack
            // has a match for shows the pack's angular piece, not our drawn dome. A leather cap the rogue's brimmed hat, a flapped or
            // leaf cap, an oilskin hood and a bone mask the hide hood with its ears and horns, the cult's mask the caster's skull helm,
            // mail the breastplate's helm, plate the black helm. Drawn still, for what the pack truly lacks: the cloth hood, the
            // scarf, the circlet and the crown (no domes among them).
            { "head.coif", Weight.Mail }, { "head.kettle", Weight.Mail }, { "head.barbute", Weight.Plate }, { "model.helm", Weight.Plate },
            { "head.cap", Weight.Leather }, { "head.cap/flaps", Weight.Hide }, { "head.cap/leaf", Weight.Hide },
            { "head.hood", Weight.None }, { "head.hood/oilskin", Weight.Hide }, { "head.mask", Weight.Hide }, { "head.mask/tear", Weight.Cloth },
            { "head.wrap", Weight.None }, { "head.circlet", Weight.None }, { "head.crown", Weight.None },
            // Chest.
            { "chest.tunic", Weight.Cloth }, { "chest.robe", Weight.Cloth }, { "chest.jerkin", Weight.Leather }, { "chest.jerkin/hide", Weight.Hide }, { "chest.jerkin/scale", Weight.Mail },
            { "chest.coat", Weight.Mail }, { "chest.hauberk", Weight.Mail }, { "chest.cuirass", Weight.Plate }, { "chest.cuirass/bark", Weight.Hide },
            // Hands (the pack's Arm pieces: bracers and gauntlets).
            { "hands.wraps", Weight.Cloth }, { "hands.wraps/fur", Weight.Fur }, { "hands.gloves", Weight.Leather }, { "hands.gauntlets", Weight.Plate },
            { "hands.gauntlets/bone", Weight.Hide }, { "hands.gauntlets/root", Weight.Hide },
            // Legs.
            { "legs.breeches", Weight.Cloth }, { "legs.leggings", Weight.Leather }, { "legs.leggings/hide", Weight.Hide }, { "legs.kilt", Weight.Hide }, { "legs.greaves", Weight.Plate },
            // Feet.
            { "feet.shoes", Weight.Cloth }, { "feet.boots", Weight.Leather }, { "feet.boots/root", Weight.Hide }, { "feet.sabatons", Weight.Plate },
        };
        /// <summary>The weight of a family and variant (<see cref="Weight.None"/>: drawn as before).</summary>
        public static Weight WeightOf(string family, string variant)
        {
            if (family == null) return Weight.None;
            if (variant != null && Weights.TryGetValue(family + "/" + variant, out var w)) return w;
            return Weights.TryGetValue(family, out w) ? w : Weight.None;
        }
        /// <summary>True when the family is in the table (mapped to a piece or kept drawn on purpose).</summary>
        public static bool Listed(string family) { return family != null && Weights.ContainsKey(family); }
        /// <summary>The pack's slot for an equipment slot (null: none; the shoulders, the neck and the hands' weapons).</summary>
        public static Slot? SlotFor(EquipSlot slot)
        {
            switch (slot) { case EquipSlot.Head: return Slot.Head; case EquipSlot.Chest: return Slot.Chest; case EquipSlot.Hands: return Slot.Arm; case EquipSlot.Legs: return Slot.Legs; case EquipSlot.Feet: return Slot.Feet; default: return null; }
        }

        /// <summary>
        /// What each type's three colourways look like (read off the contact sheet; the colour a look is matched against):
        /// 1 grey-green steel, red, gold and blue; 2 black, brown and bone, gold; 3 tan, grey-green, rust; 4 purple and gold, red,
        /// teal; 5 blue, yellow, green; 6 tan, dark, red.
        /// </summary>
        static readonly Color[,] Ways = {
            { new Color(.42f, .47f, .42f), new Color(.6f, .15f, .15f), new Color(.8f, .62f, .2f) },
            { new Color(.22f, .22f, .24f), new Color(.5f, .36f, .26f), new Color(.78f, .58f, .12f) },
            { new Color(.62f, .48f, .32f), new Color(.42f, .46f, .42f), new Color(.62f, .32f, .26f) },
            { new Color(.45f, .15f, .55f), new Color(.62f, .22f, .16f), new Color(.2f, .5f, .5f) },
            { new Color(.3f, .55f, .85f), new Color(.85f, .7f, .12f), new Color(.3f, .62f, .52f) },
            { new Color(.62f, .52f, .4f), new Color(.32f, .32f, .36f), new Color(.55f, .15f, .15f) } };
        /// <summary>The colourway (1-3) of a type nearest a colour: by hue among coloured ones, a grey to the plainest.</summary>
        public static int Colourway(int type, Color key)
        {
            if (type < 1 || type > Types) return 1;
            Color.RGBToHSV(key, out float h, out float s, out float v);
            int best = 1; float bestD = float.MaxValue;
            for (int c = 0; c < Colourways; c++)
            {
                Color.RGBToHSV(Ways[type - 1, c], out float hc, out float sc, out float vc);
                float dh = Mathf.Abs(h - hc); dh = Mathf.Min(dh, 1 - dh) * 2;
                float d = Mathf.Min(s, sc) * dh * 3 + Mathf.Abs(s - sc) * 1.5f + Mathf.Abs(v - vc) * .5f;
                if (d < bestD) { bestD = d; best = c + 1; }
            }
            return best;
        }
        /// <summary>The colour a look is matched by: the cloth for cloth, leather, hide and fur (their dyed parts); for mail and
        /// plate the metal, or the trim on rare and epic pieces (an epic's gold gives the gold colourway).</summary>
        public static Color KeyColour(Weight w, GearLook l)
        {
            if (w == Weight.Mail || w == Weight.Plate) return l.quality >= 3 && l.hasTrim ? l.trimColor : l.metal;
            return l.cloth;
        }
        /// <summary>
        /// The piece a worn item shows in its slot, and for a chest piece the belt that goes with it (<paramref name="belt"/>);
        /// false when the slot has no piece for it (the drawn piece stays).
        /// </summary>
        public static bool Map(EquipSlot slot, GearLook look, out string piece, out string belt)
        {
            piece = belt = null;
            var s = SlotFor(slot); if (s == null) return false;
            var w = WeightOf(look.family, look.variant); int type = TypeFor(w); if (type == 0) return false;
            int colour = Colourway(type, KeyColour(w, look));
            piece = Armour(s.Value, type, colour);
            if (s == Slot.Chest) belt = Armour(Slot.Belt, type, colour);
            return true;
        }
        /// <summary>Any of the pack's chest pieces: each has something on its shoulders (pads, plates, rings, bone, a pauldron, fur), so
        /// the drawn shoulder piece, a ball over the pack's shoulder (play note 239), is not drawn over it.</summary>
        public static bool CoversShoulders(string chestPiece) { return SlotOf(chestPiece) == Slot.Chest; }

        // ------------------------------------------------------------------------------------------------- what people wear bare
        /// <summary>A set's pieces: slot, type, colourway.</summary>
        public struct Wear { public Slot slot; public int type, colour; public Wear(Slot s, int t, int c) { slot = s; type = t; colour = c; } public string Piece { get { return Armour(slot, type, colour); } } }
        static Wear[] Set(int type, int colour, params Slot[] slots) { var w = new Wear[slots.Length]; for (int i = 0; i < slots.Length; i++) w[i] = new Wear(slots[i], type, colour); return w; }
        static readonly Slot[] LegsFeet = { Slot.Legs, Slot.Feet }, Clothed = { Slot.Chest, Slot.Belt, Slot.Legs, Slot.Feet }, Armoured = { Slot.Head, Slot.Chest, Slot.Arm, Slot.Belt, Slot.Legs, Slot.Feet };
        /// <summary>
        /// What a person wears where the slot is empty (their own clothes, under any gear), by look and, for villagers, by trade.
        /// Everyone is clothed (a wardrobe line-up of bare chests and shorts, wardrobe-ganzse-1): the player's classes in a top, breeches
        /// and boots of their colours, the world's people in their own. The pack has no plain shirt: the least warlike pieces (the jerkin, the hide top, the caster's coat).
        /// </summary>
        public static Wear[] Outfit(string look, string role, int variant)
        {
            int v = Mathf.Abs(variant);
            switch (look)
            {
                case "Warrior": return Concat(Set(5, 1, Slot.Chest), Set(1, 1, LegsFeet));    // the jerkin in his blue, mail breeches
                case "Paladin": return Concat(Set(5, 2, Slot.Chest), Set(1, 3, LegsFeet));
                case "Ranger": return Set(5, 3, Slot.Chest, Slot.Legs, Slot.Feet);
                case "Mage": return Set(4, 1, Slot.Chest, Slot.Legs, Slot.Feet);
                case "Druid": return Set(3, 2, Slot.Chest, Slot.Legs, Slot.Feet);
                case "Healer": return Set(4, 3, Slot.Chest, Slot.Legs, Slot.Feet);
                case "Collector": case "Warden": return Set(1, 1, Clothed);
                case "Sentry": return Set(1, 1, Armoured);
                case "Outrider": return Set(5, 1, Clothed);
                case "Cultist": return Set(4, 1, Clothed);
                case "Deserter": return Set(6, 1 + v % 2, Clothed);
                case "BanditKing": return Set(6, 3, Armoured);
                case "SaltMender": return Set(3, 1, Clothed);
                case "Hollow": return Set(3, 2, LegsFeet);
                case "Villager": return Trade(role, v);
                default: return Set(5, 2, Clothed);
            }
        }
        /// <summary>The villagers' clothes by trade (TradeSpec's trades); plain folk in the jerkin, its colour by the variant.</summary>
        static Wear[] Trade(string role, int v)
        {
            switch (role)
            {
                case "blacksmith": return Set(6, 2, LegsFeet);                    // bare-armed at the forge, in dark breeches
                case "merchant": return Set(5, 1, Clothed);
                case "baker": case "miller": case "innkeeper": return Set(5, 2, Clothed);
                case "henwife": return Set(3, 3, Clothed);
                case "farmer": return Concat(Set(5, 2, Slot.Chest), Set(3, 1, LegsFeet));
                case "hunter": case "warden": return Set(5, 3, Clothed);
                case "leatherworker": case "skinner": return Set(3, 1, Clothed);
                case "lumberjack": return Concat(Set(5, 2, Slot.Chest), Set(6, 1, LegsFeet));
                case "herbalist": return Set(3, 2, Clothed);
                case "elder": return Set(4, 2, Clothed);
                case "stranger": return Set(5, 1, Armoured);                       // the hat and the scarf
                case "pilgrim": return Set(3, 3, Clothed);
                case "councilor": case "scholar": return Set(4, 3, Clothed);
                case "child": return Set(5, 2, Slot.Chest, Slot.Legs, Slot.Feet);
                default: return Set(5, 1 + v % 3, Clothed);
            }
        }
        static Wear[] Concat(Wear[] a, Wear[] b) { var w = new Wear[a.Length + b.Length]; a.CopyTo(w, 0); b.CopyTo(w, a.Length); return w; }
        /// <summary>The trades <see cref="Outfit"/> clothes by name (the rest wear the plain folk's jerkin).</summary>
        public static readonly string[] Trades = { "blacksmith", "merchant", "baker", "miller", "innkeeper", "henwife", "farmer", "hunter", "warden", "leatherworker", "skinner",
            "lumberjack", "herbalist", "elder", "stranger", "pilgrim", "councilor", "scholar", "child", "drinker" };

        // ------------------------------------------------------------------------------------------------- the face
        /// <summary>The pack's skin swatch, and the Quaternius kit's skin (the mean of T_Superhero_Male_Light_BaseColor's skin): the
        /// pack's skin is tinted by their ratio times the figure's tone, so a figure is the same tone on either kit.</summary>
        public static readonly Color SkinSwatch = new Color(.92f, .73f, .55f), KitSkin = new Color(.655f, .454f, .316f);
        public static Color Skin(Color tone) { return new Color(tone.r * KitSkin.r / SkinSwatch.r, tone.g * KitSkin.g / SkinSwatch.g, tone.b * KitSkin.b / SkinSwatch.b); }
        /// <summary>The pack's material made matte (the Quaternius kit's gloss).</summary>
        public const float Gloss = .25f;
        /// <summary>The pack's five hair colours (black, dark brown, brown, blond, grey), as the swatches read.</summary>
        static readonly Color[] HairColours = { new Color(.03f, .03f, .03f), new Color(.14f, .08f, .04f), new Color(.37f, .23f, .12f), new Color(.72f, .49f, .1f), new Color(.57f, .57f, .57f) };
        /// <summary>ActorVisual lifts its hair colours by this for the Quaternius kit's grey hair (SpecFor); taken off again here.</summary>
        public const float HairLift = 1.7f;
        /// <summary>The hair colour (1-5) nearest an old hair colour (as ActorVisual gives it, lifted).</summary>
        public static int HairColour(Color lifted)
        {
            var c = new Color(lifted.r / HairLift, lifted.g / HairLift, lifted.b / HairLift);
            int best = 1; float bestD = float.MaxValue;
            for (int i = 0; i < HairColours.Length; i++)
            {
                var h = HairColours[i]; Color.RGBToHSV(c, out _, out float s, out _); Color.RGBToHSV(h, out _, out float sh, out _);
                float d = Mathf.Abs(c.r - h.r) + Mathf.Abs(c.g - h.g) + Mathf.Abs(c.b - h.b) + Mathf.Abs(s - sh) * .3f;
                if (d < bestD) { bestD = d; best = i + 1; }
            }
            return best;
        }
        /// <summary>The hairstyle (1-5) for ModelFigure's style (0 short, 1 long, 2 tied back, 3 cropped, 4 bald: 0, no hair):
        /// 1 parted, 2 shaggy, 5 swept back, 4 cropped. (The pack has no long hair.)</summary>
        public static int HairStyle(int style) { switch (style) { case 0: return 1; case 1: return 2; case 2: return 5; case 3: return 4; default: return 0; } }
        /// <summary>A beard's style (1 moustache, 2 thin moustache, 3 full beard, 4 short moustache, 5 goatee) by the variant: mostly the full beard.</summary>
        public static int BeardStyle(int variant) { int v = Mathf.Abs(variant); return new[] { 3, 3, 1, 5, 3, 2 }[v % 6]; }
        /// <summary>Eyes: the colour by the variant, dark only (1 black, 2 dark brown, 3 brown). The pack's blue and green swatches are
        /// bright enough to read as glowing on a low-poly face at any distance (wardrobe-ganzse-1, where every man had them: the old
        /// formula took every variant to the blue).</summary>
        public static int EyeColour(int variant) { int v = Mathf.Abs(variant); return new[] { 2, 3, 2, 1, 3 }[v % 5]; }
    }
}
