using System.Collections.Generic;
using Crulanda.Combat;
using Crulanda.Gameplay;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>What a buff on the buff row is, for its picture when it has no icon of its own.</summary>
    public enum BuffKind { Food, Elixir, Salve, ClassBuff, Song, Other }

    /// <summary>One buff or debuff on someone, as the buff row draws it.</summary>
    public struct BuffEntry
    {
        public string id, name, effect, classId;
        public float remaining;
        public bool harmful;
        public BuffKind kind;
    }

    /// <summary>
    /// The buff row's small square icons (Chris, 2026-10-18: "for buffs need icons or something bc this is messy and hard to read
    /// screen"; Docs/PLAYTEST_NOTES.md note 231). In place of the text tags ("Well fed 741s"): a 22 px square per buff with its
    /// picture (an item's or an ability's icon, else a drawn glyph by kind), its time left in the corner ("12m", or seconds under a
    /// minute), a green border for a help and a red one for a harm, and the name, time and effect in a tooltip. Pure layout and words
    /// here so they are tested without a scene (BuffIconTests); EncounterHud.DrawBuffs paints them.
    /// </summary>
    public static class BuffIcons
    {
        /// <summary>An icon's side and the gap between two (canvas pixels at UiScale 1).</summary>
        public const float Size = 22, Gap = 2;
        /// <summary>The party frames' plate width (EncounterHud.DrawPartyFrame): a party member's row is no wider.</summary>
        public const float PartyFrameWidth = 214;
        /// <summary>The player's own row: twelve icons.</summary>
        public const float PlayerRowWidth = 12 * Size + 11 * Gap;
        public static readonly Color Helpful = new Color(.35f, .85f, .4f), Harmful = new Color(.95f, .2f, .15f);

        /// <summary>How wide a row of this many icons is.</summary>
        public static float RowWidth(int count) { return count <= 0 ? 0 : count * Size + (count - 1) * Gap; }
        /// <summary>How many icons fit in this width.</summary>
        public static int Fits(float width) { return width < Size ? 0 : 1 + Mathf.FloorToInt((width - Size) / (Size + Gap) + 1e-4f); }
        /// <summary>The i-th icon's square in a row.</summary>
        public static Rect IconRect(Rect row, int i) { return new Rect(row.x + i * (Size + Gap), row.y, Size, Size); }
        /// <summary>How many icons are drawn for this many buffs in this width; when they do not all fit, the last slot says "+N" instead.</summary>
        public static int Shown(int count, float width) { int fit = Fits(width); return count <= fit ? count : Mathf.Max(0, fit - 1); }

        /// <summary>The corner number: whole minutes from a minute up ("12m"), seconds under it ("45"), nothing for a buff without an end.</summary>
        public static string Countdown(float seconds)
        {
            if (seconds <= 0) return "";
            if (seconds >= 6000) return Mathf.FloorToInt(seconds / 3600) + "h";
            if (seconds >= 60) return Mathf.FloorToInt(seconds / 60) + "m";
            return Mathf.CeilToInt(seconds).ToString();
        }
        /// <summary>The tooltip's time: "12m 21s left", "45s left", or "Until it ends" for one without an end.</summary>
        public static string TimeLeft(float seconds)
        {
            if (seconds <= 0) return "Until it ends";
            int s = Mathf.CeilToInt(seconds);
            return s >= 60 ? s / 60 + "m " + s % 60 + "s left" : s + "s left";
        }
        /// <summary>A status's kind by its id: a consumable's, a class buff, or anything else.</summary>
        public static BuffKind KindOf(string id)
        {
            if (id == Consumables.FedId) return BuffKind.Food;
            if (id == Consumables.ElixirId) return BuffKind.Elixir;
            if (id == Consumables.SalveId) return BuffKind.Salve;
            if (id != null && id.StartsWith("buff.class.")) return BuffKind.ClassBuff;
            return BuffKind.Other;
        }
        /// <summary>The class a class buff's status belongs to ("buff.class.paladin" to "class.paladin"), else null.</summary>
        public static string ClassOf(string id) { return id != null && id.StartsWith("buff.class.") ? "class." + id.Substring("buff.class.".Length) : null; }
        /// <summary>The words for what a status does: its stats ("+6 Stamina, +4 Agility", "+3% Strength") and the harm it lets through.</summary>
        public static string Effect(StatusEffectDefinition d)
        {
            if (d == null) return null;
            var parts = new List<string>();
            if (d.modifiers != null)
                foreach (var m in d.modifiers)
                {
                    if (m == null || m.value == 0) continue;
                    string sign = m.value > 0 ? "+" : "-"; float v = Mathf.Abs(m.value);
                    parts.Add(m.operation == ModifierOp.Flat ? sign + Mathf.RoundToInt(v) + " " + m.stat : sign + Mathf.RoundToInt(v * 100) + "% " + m.stat);
                }
            if (d.incomingDamageMultiplier > 1.001f) parts.Add("Takes " + Mathf.RoundToInt((d.incomingDamageMultiplier - 1) * 100) + "% more damage");
            else if (d.incomingDamageMultiplier < .999f) parts.Add("Takes " + Mathf.RoundToInt((1 - d.incomingDamageMultiplier) * 100) + "% less damage");
            return parts.Count == 0 ? null : string.Join(", ", parts);
        }
        /// <summary>A status's own picture: a class buff's (Icons/ability/buff-class-*), a dish's plate, a tincture's or salve's flask, an
        /// ability's of the same id; null when there is none (the HUD draws a glyph).</summary>
        public static Texture2D Icon(string id, BuffKind kind)
        {
            switch (kind)
            {
                case BuffKind.Food: return IconDb.Load("Icons/kind/food");
                case BuffKind.Elixir: case BuffKind.Salve: return IconDb.Load("Icons/kind/potion");
                case BuffKind.Song: return null;
                default: return IconDb.Ability(id);
            }
        }
        /// <summary>The tooltip's text: the name (red for a harm), the time left and what it does.</summary>
        public static string Tooltip(BuffEntry b)
        {
            string head = "<b><color=#" + (b.harmful ? "ff6655" : "88ee88") + ">" + b.name + "</color></b>";
            string kind = b.harmful ? "Harmful" : b.kind == BuffKind.Song ? "The party's song" : "Helpful";
            return head + "\n" + kind + "  ·  " + TimeLeft(b.remaining) + (string.IsNullOrEmpty(b.effect) ? "" : "\n" + b.effect);
        }
    }
}
