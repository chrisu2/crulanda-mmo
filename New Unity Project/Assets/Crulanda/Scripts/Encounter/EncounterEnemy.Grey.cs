using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Wasting's grey (handoff item 8, 2026-10-13). GAME-ONLY mechanic on a CANON creature (the Hollow Men of the tunnels under the
    /// Peaks, book 2 ch. 19; the Wasting is CANON): a Hollow Man's blow greys whoever it lands on, who then takes a tenth more from every
    /// blow for twelve seconds. Another blow refreshes it; it never stacks. The cleanses lift it (StatusEffectRuntime.RemoveHarmful knows
    /// "grey"): the Cleric's Absolve, the Salt Cairn's sifting and a sim Salt-Mender's sifting. Before this nothing applied the grey, so
    /// those cleanses had nothing to lift.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public const string GreyId = "status.wasting_grey";
        public const float GreyTaken = 1.1f, GreySeconds = 12;
        public static readonly StatusEffectDefinition GreyDefinition = new StatusEffectDefinition { id = GreyId, name = "The grey", duration = GreySeconds, incomingDamageMultiplier = GreyTaken };
        /// <summary>This mob's blows grey: the Hollow Men.</summary>
        public bool Greys { get { return Figure != null && Figure.Look == ActorLook.Hollow; } }
        void Grey(Actor victim)
        {
            if (!Greys || victim == null || !victim.IsAlive) return;
            var c = victim.GetComponent<Combatant>(); if (c == null) return;
            bool fresh = !c.Statuses.Has(GreyId);
            if (c.Apply(GreyDefinition) && fresh && victim == session.Player) session.Message("The grey settles on you: you take a tenth more from every blow until it fades or is lifted.");
        }
    }
}
