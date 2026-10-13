using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Area damage by specialization (Chris, 2026-10-13: "i do want some classes to specialize in AOE (like a cleave paladin, or cleave
    /// warrior) or a ranger with scatter arrows or volley but i dont want dps vs 1 and dps vs 3 to be the same for every class ..this should
    /// be based on specializations"). GAME-ONLY. While the build you have spent most in is one of these, each of your own blows on a foe
    /// splashes a share of its damage onto up to two more foes near it. A held foe (incapacitated or feared) is never splashed: the party
    /// leaves held mobs alone. Hidden foes, riders and game are never splashed. Your pet's and your spirit's blows do not splash.
    /// Every other damage build is a single-target specialist: about the same against three as against one, and the better against one.
    /// </summary>
    public partial class EncounterSession
    {
        public readonly struct Spread
        {
            public readonly string name; public readonly float share, reach; public readonly int extra;
            public Spread(string name, float share, float reach, int extra) { this.name = name; this.share = share; this.reach = reach; this.extra = extra; }
        }
        public static readonly Dictionary<string, Spread> BuildSpread = new Dictionary<string, Spread>
        {
            { "class.warrior/dps", new Spread("Sweeping blows", .55f, 5, 2) },        // the cleave warrior
            { "class.paladin/judicator", new Spread("Wide judgement", .55f, 5, 2) },  // the cleave paladin
            { "class.ranger/skirmisher", new Spread("Scatter shot", .5f, 8, 2) },     // scatter arrows, the volley
            { "class.mage/combustion", new Spread("Spreading flame", .5f, 6, 2) },
            { "class.shaman/stormcaller", new Spread("Forked storm", .45f, 6, 2) },
        };
        bool spreading; string spreadFor; int spreadCount = -1; Spread? spreadNow;
        /// <summary>The lead build's spread, or null for a single-target build (cached until your ranks change).</summary>
        public Spread? PlayerSpread
        {
            get
            {
                var list = Progress != null ? Progress.talents : null; int count = 0; if (list != null) foreach (var t in list) if (t != null) count += t.rank;
                if (count != spreadCount || spreadFor == null) { spreadCount = count; spreadFor = LeadBuild ?? ""; spreadNow = BuildSpread.TryGetValue(spreadFor, out var v) ? v : (Spread?)null; }
                return spreadNow;
            }
        }
        /// <summary>Called by EncounterEnemy.Receive after a blow of yours landed for <paramref name="dealt"/>.</summary>
        public void SpreadBlow(EncounterEnemy struck, Actor source, int dealt)
        {
            if (spreading || dealt <= 0 || struck == null || source == null || source != Player || Meter.Redirect != null) return;
            var sp = PlayerSpread; if (!sp.HasValue) return;
            var spread = sp.Value; float scale = Mathf.Max(.01f, PlayerDamageScale);
            int splash = Mathf.RoundToInt(dealt * spread.share / scale); if (splash <= 0) return;   // Receive applies your scale again
            var near = new List<(EncounterEnemy e, float d)>();
            foreach (var e in Enemies)
            {
                if (e == null || e == struck || e.actor == null || !e.actor.IsAlive || e.Game || e.Hidden || e.Inside != null || SimCompanion.Held(e)) continue;
                float d = Vector3.Distance(e.transform.position, struck.transform.position); if (d <= spread.reach) near.Add((e, d));
            }
            near.Sort((a, b) => a.d.CompareTo(b.d));
            spreading = true;
            try { for (int i = 0; i < near.Count && i < spread.extra; i++) near[i].e.Receive(splash, Player); }
            finally { spreading = false; }
        }
    }
}
