using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// People on GanzSe's body (Phase 8.5, step 1; <see cref="GanzSeKit"/>, ModelFigure.GanzSe.cs), behind <see cref="GanzSeKit.On"/>.
    /// A man (the pack has no woman's body; women keep the Quaternius figure unless <see cref="GanzSeKit.Women"/>), not an orc (the
    /// orc's head is hung on the Quaternius head), is built on the pack's body in the clothes of his look or trade. Worn armour in a
    /// slot the pack covers (head helms, chest, hands, legs, feet) shows the pack's piece instead of the drawn one, so nothing
    /// doubles up; any pack chest piece hides the drawn shoulder piece; the rest (the cloth hood, the scarf, circlets, crowns,
    /// the neck, every weapon and shield) is drawn and carried as before, on the same frames.
    /// </summary>
    public sealed partial class ActorVisual
    {
        bool OnGanzSe { get { return model != null && model.GanzSe; } }
        /// <summary>The chest piece the pack shows once the slots are dressed (gear's, or the figure's own), for the shoulders.</summary>
        string gzChest;
        bool UseGanzSe(ModelFigure.Spec s) { return GanzSeKit.On && !orc && (!s.female || GanzSeKit.Women) && ModelFigure.GanzSeAvailable; }
        /// <summary>The spec on the pack's body: the face's seed, the clothes of the look (or the villager's trade), no hood.</summary>
        ModelFigure.Spec GanzSeSpec(ModelFigure.Spec s, ActorLook look)
        {
            s.ganzse = true; s.seed = variant; s.hood = false;
            s.outfit = GanzSeKit.Outfit(look.ToString(), role, variant);
            return s;
        }
        /// <summary>Before the slots are dressed (ApplyGear): the chest piece that will show, into the shoulders' signature so they
        /// are dressed again when it changes.</summary>
        void GanzSeChest(bool[] shows, GearLook[] lks, string[] sigs)
        {
            if (!OnGanzSe) return;
            int c = (int)EquipSlot.Chest, sh = (int)EquipSlot.Shoulders;
            gzChest = shows[c] && GanzSeKit.Map(EquipSlot.Chest, lks[c], out var piece, out _) ? piece : model.BaseOf(GanzSeKit.Slot.Chest);
            if (shows[sh]) sigs[sh] += "|gz:" + gzChest;
        }
        /// <summary>A slot dressed by the pack (true: the drawn armour is skipped): its piece (and a chest piece's belt) on the figure;
        /// a helm hides the hair and the class's hood. The shoulders: true (nothing drawn) under a chest piece with armoured shoulders.</summary>
        bool GanzSeBuild(EquipSlot slot, GearLook look)
        {
            if (!OnGanzSe) return false;
            if (slot == EquipSlot.Shoulders) return GanzSeKit.CoversShoulders(gzChest);
            var gs = GanzSeKit.SlotFor(slot);
            if (gs == null || !GanzSeKit.Map(slot, look, out var piece, out var belt)) return false;
            model.Wear(gs.Value, piece); if (belt != null) model.Wear(GanzSeKit.Slot.Belt, belt);
            if (slot == EquipSlot.Head) { ShowHair(false); DruidHood(false); }
            return true;
        }
        /// <summary>A slot cleared: gear's piece comes off (the figure's own clothes show again).</summary>
        void GanzSeClear(int s)
        {
            if (!OnGanzSe) return;
            var gs = GanzSeKit.SlotFor((EquipSlot)s); if (gs == null) return;
            model.Wear(gs.Value, null); if (gs == GanzSeKit.Slot.Chest) model.Wear(GanzSeKit.Slot.Belt, null);
        }
    }
}
