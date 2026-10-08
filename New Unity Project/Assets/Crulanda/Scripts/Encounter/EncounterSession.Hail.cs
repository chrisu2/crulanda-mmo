using System.Collections;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hailing (playtest note 83, 2026-10-07: "'H' to hail other people, NPCs and sims"): H greets whoever you have selected (a
    /// villager, a sim, Mira), or else the nearest person within 15 m in front of you. You say it in Say; they turn and answer a moment
    /// later: a villager by their role, a sim by what it makes of you (SimChatter.Hailed), Mira as Mira. A mob is not hailed.
    /// </summary>
    public sealed partial class EncounterSession
    {
        float hailAgain;
        public void Hail()
        {
            if (Player == null || !Player.IsAlive || Paused || Time.time < hailAgain) return;
            hailAgain = Time.time + 1.2f;
            var me = Player.transform.position; var ahead = Player.transform.forward;
            // Whom: the selected friend first, then the nearest person in front.
            Villager v = FocusVillager; SimFigure sf = FocusSimId != null ? SimPopulation.Active?.Find(FocusSimId) : null; bool mira = FocusMira && Companion != null;
            SimCompanion pc = FocusSimId != null ? PartySim(FocusSimId) : null;
            if (v == null && sf == null && !mira && pc == null)
            {
                float best = 15;
                foreach (var x in Object.FindObjectsByType<Villager>(FindObjectsInactive.Exclude))
                {
                    if (x == null || !x.Visible) continue; var d = x.transform.position - me; d.y = 0; float dist = d.magnitude;
                    if (dist < best && Vector3.Dot(d.normalized, ahead) > .3f) { best = dist; v = x; }
                }
                if (SimPopulation.Active != null)
                    foreach (var f in SimPopulation.Active.Figures)
                    {
                        if (f == null || f.Hidden) continue; var d = f.transform.position - me; d.y = 0; float dist = d.magnitude;
                        if (dist < best && Vector3.Dot(d.normalized, ahead) > .3f) { best = dist; sf = f; v = null; }
                    }
                if (Companion != null && v == null && sf == null) { var d = Companion.transform.position - me; d.y = 0; if (d.magnitude < 15) mira = true; }
            }
            string who = v != null ? v.Name : sf != null ? sf.sim.name : pc != null ? pc.sim.name : mira ? "Mira" : null;
            if (who == null) { ChatSay(ChatChannel.Say, "You", "Hail!"); return; }
            string first = who.Split(' ')[0];
            ChatSay(ChatChannel.Say, "You", Pick(new[] { "Hail, " + first + "!", "Well met, " + first + ".", "Good day, " + first + "." }));
            if (v != null) StartCoroutine(Answer(v.Name, v.transform, VillagerHail(v)));
            else if (sf != null) { SimChatter.Active?.Hailed(sf.sim); Face(sf.transform); }
            else if (pc != null) StartCoroutine(Answer(pc.sim.name, pc.transform, Pick(new[] { "o/", "hey", "yo" })));
            else if (mira) StartCoroutine(Answer("Mira", Companion.transform, Pick(new[] { "I'm right here, you know.", "Hail yourself. Shall we go?", "Yes, yes. Hello." })));
        }
        static string Pick(string[] lines) { return lines[Random.Range(0, lines.Length)]; }
        IEnumerator Answer(string name, Transform body, string line)
        {
            Face(body);
            yield return new WaitForSeconds(.8f + Random.value * .8f);
            if (body != null) ChatSay(ChatChannel.Say, name, line);
        }
        /// <summary>Turns someone to look at you (a villager or a sim stops what it does for a moment by its own rules; this is only the turn).</summary>
        void Face(Transform body)
        {
            if (body == null || Player == null) return; var d = Player.transform.position - body.position; d.y = 0;
            if (d.sqrMagnitude > .01f) body.rotation = Quaternion.LookRotation(d);
        }
        string VillagerHail(Villager v)
        {
            switch (v.Role)
            {
                case "farmer": return Pick(new[] { "Morning. Mind the furrows.", "Hail. Fine weather for the barley.", "Aye, hello. Can't stop, the field won't hoe itself." });
                case "blacksmith": return Pick(new[] { "Hail. Need an edge put on that?", "Well met. Forge is hot if you need it." });
                case "child": return Pick(new[] { "Hello! Are you an adventurer?", "Hi! Have you seen a wolf? A real one?" });
                case "drinker": return Pick(new[] { "Hail, friend! Join me for one?", "Ah, a fresh face. Cask's open." });
                case "elder": return Pick(new[] { "Well met, traveller. Oakhaven welcomes you.", "Hail. Walk carefully; the grey is close." });
                case "merchant": return Pick(new[] { "Hail! Wares to sell, coin to spend?", "Good day. Best prices on the green." });
                case "gossip": return Pick(new[] { "Oh, hello! Have you heard about the collectors?", "Hail! You'll never guess what I saw." });
                default: return Pick(new[] { "Hail.", "Good day to you.", "Well met.", "Hello there." });
            }
        }
    }
}
