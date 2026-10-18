using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Chemical River in play (Lowtowns pick 6; <see cref="ChemicalRiver"/> says the rules): twice a second, everyone of the party in its
    /// black water is bitten by it, and what was tracking them by magic loses them. Nothing happens in a zone without a chemical river.
    /// </summary>
    public sealed partial class EncounterSession
    {
        float riverTickAt, riverLostSaidAt; bool playerInRiver;
        void TickRiver()
        {
            if (Zone == null || Player == null || Time.time < riverTickAt || !Zone.HasChemical) return;
            riverTickAt = Time.time + ChemicalRiver.TickSeconds;
            // A copy: a mob giving up below resets its fight, and that may ask for the party again.
            foreach (var a in PartyActors().ToArray())
            {
                if (a == null || !a.IsAlive) continue;
                bool wet = Zone.ChemicalAt(a.transform.position);
                if (a == Player) { if (wet && !playerInRiver) Message(ChemicalRiver.EnterLine); playerInRiver = wet; }
                if (!wet) continue;
                // The poison: a bite each tick, never the last of anyone's health.
                var hp = a.Health; int bite = ChemicalRiver.BiteFrom(hp.Pool.Current, hp.Pool.Max);
                if (bite > 0) { hp.ApplyDamage(bite); if (a == Player) FloatText(a.transform.position, "-" + bite, new Color(.55f, .85f, .3f)); }
                // Hidden from what tracks by magic: a Weave-Eater beyond touching, a far-reaching caster beyond a few strides.
                for (int i = 0; i < Enemies.Count; i++)
                {
                    var e = Enemies[i];
                    if (e == null || e.actor == null || !e.actor.IsAlive || e.Game || e.Victim != a) continue;
                    float distance = Vector3.Distance(e.transform.position, a.transform.position);
                    if (!ChemicalRiver.LosesTrack(WeaveEaterRules.IsWeaveEater(e), e.Cast != null ? e.Cast.reach : 0, distance)) continue;
                    e.LoseSight(a, ChemicalRiver.LostSeconds);
                    FloatText(e.transform.position, "Lost", new Color(.7f, .7f, .75f));
                    if (Time.time >= riverLostSaidAt) { riverLostSaidAt = Time.time + 8; Message(ChemicalRiver.LostLine(e.actor.DisplayName)); }
                }
            }
        }
    }
}
