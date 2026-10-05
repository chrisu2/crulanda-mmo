using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The painted HUD icons (Assets/Crulanda/Resources/Icons, made by tools/art/make_icons.py). Resolves an item to its picture:
    /// its own (Icons/item/&lt;id&gt;), else for gear the look's family in its palette (Icons/gear/&lt;family&gt;__&lt;palette&gt;, how
    /// generated gear is drawn), then a slot silhouette (Icons/slot/&lt;slot&gt;), else the item's kind and trade (Icons/kind/&lt;kind&gt;:
    /// ore, timber, herb, larder, hide, food, potion, tool, bag, junk, material); abilities, talents, quest items and trades by id.
    /// Every lookup is cached, hits and misses alike, so OnGUI never loads or allocates; a missing icon returns null and the
    /// HUD keeps its letters.
    /// </summary>
    public static class IconDb
    {
        /// <summary>Resources path to texture (null when there is no such file). Shared by every resolver.</summary>
        static readonly Dictionary<string, Texture2D> byPath = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        /// <summary>Item id to the texture chosen for it, after the fallbacks (null cached too).</summary>
        static readonly Dictionary<string, Texture2D> byItem = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        static GearLooks looks;

        /// <summary>A Resources path under Icons/ (an id's dots are written as '-', as the generator writes them).</summary>
        public static string PathFor(string group, string id) { return "Icons/" + group + "/" + id.Replace('.', '-'); }
        public static Texture2D Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (byPath.TryGetValue(path, out var t)) return t;
            t = Resources.Load<Texture2D>(path);
            byPath[path] = t;
            return t;
        }
        public static Texture2D Ability(string abilityId) { return string.IsNullOrEmpty(abilityId) ? null : Load(PathFor("ability", abilityId)); }
        public static Texture2D Talent(string talentId) { return string.IsNullOrEmpty(talentId) ? null : Load(PathFor("talent", talentId)); }
        public static Texture2D QuestItem(string questItemId) { return string.IsNullOrEmpty(questItemId) ? null : Load(PathFor("quest", questItemId)); }
        public static Texture2D Trade(string professionId) { return string.IsNullOrEmpty(professionId) ? null : Load(PathFor("trade", professionId)); }
        /// <summary>The grey silhouette for an unknown piece of a slot (the Armoury's "unknown" rows).</summary>
        public static Texture2D Slot(string slot) { return string.IsNullOrEmpty(slot) ? null : Load("Icons/slot/" + slot); }

        /// <summary>An item's icon by its definition: own, then gear family and palette, then slot, then kind and trade; null if none.</summary>
        public static Texture2D Item(ItemDef d)
        {
            if (d == null || string.IsNullOrEmpty(d.id)) return null;
            if (byItem.TryGetValue(d.id, out var t)) return t;
            t = Load(PathFor("item", d.id));
            if (t == null && d.kind == "gear")
            {
                looks = looks ?? GearLooks.Load();
                if (looks != null) { var l = looks.Resolve(d); t = l.family.StartsWith("model.") ? Load("Icons/model/" + l.variant) : Load("Icons/gear/" + l.family.Replace('.', '-') + "__" + l.palette); }   // a model's own (Editor/ModelIcons)
                if (t == null) t = Slot(d.slot);
            }
            if (t == null) t = Load("Icons/kind/" + KindKey(d));
            byItem[d.id] = t;
            return t;
        }
        /// <summary>The kind-and-trade key the generator draws a generic picture for.</summary>
        public static string KindKey(ItemDef d)
        {
            if (d == null) return "junk";
            if (Inventory.IsHide(d)) return "hide";
            if (d.kind == "material") return d.pouch == "ore" || d.pouch == "timber" || d.pouch == "herb" || d.pouch == "larder" ? d.pouch : "material";
            if (d.kind == "consumable") return d.food ? "food" : "potion";
            if (d.kind == "tool" || d.kind == "bag" || d.kind == "junk") return d.kind;
            return "junk";
        }
        /// <summary>Drops every cached texture reference (tests, or after a Resources.UnloadUnusedAssets).</summary>
        public static void Clear() { byPath.Clear(); byItem.Clear(); looks = null; }
    }
}
