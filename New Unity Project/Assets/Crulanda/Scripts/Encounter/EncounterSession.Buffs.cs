using System.Collections.Generic;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Buffing up before the bosses (Chris, 2026-10-15: "we should use food buffs, alchemy buffs, class buffs,etc to beat the bosses";
    /// BossBuffs). The party's leader stops short of a boss or an elite camp (SimCompanion: a stop, numbered here) and waits until every
    /// living member has buffed or <see cref="BossBuffs.StopWait"/> seconds pass. You buff with /buff (the run bot does it for you):
    /// a feast from your bags for the party, else a dish; the elixir for your part; your class's draught when your pool is low; and your
    /// class's standing buff on everyone near (ClassKit.PartyBuff). None of it is saved: the buffs ride the status effects.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The buff-up stops so far (each leader's stop counts one up); the one going on, while <see cref="BuffingUp"/>.</summary>
        public int BuffStop { get; private set; }
        /// <summary>The leader stopped to buff up, or null.</summary>
        public SimCompanion BuffLeader { get; private set; }
        public float BuffStopSince { get; private set; }
        /// <summary>A leader is stopped short of a boss or an elite camp for the party to buff up.</summary>
        public bool BuffingUp { get { return BuffLeader != null && BuffLeader.BuffingUp; } }
        /// <summary>The stop at which you last buffed (/buff), -1 for none.</summary>
        public int PlayerBuffedStop { get; private set; } = -1;
        /// <summary>What your last buff-up used, for the run's log ("Pass-fire feast (5 fed), Scree salve, Sworn to the Colours on 5").</summary>
        public string PlayerBuffNote { get; private set; }

        /// <summary>The party's wipes so far this session (Recover with no fight left going on). Dying clears the buffs, so a leader whose boss
        /// was buffed for before a wipe stops to buff up again before the next pull (2026-10-18; SimCompanion.BuffPending).</summary>
        public int Wipes { get; private set; }
        /// <summary>Counts a wipe (Recover; a test's).</summary>
        public void CountWipe() { Wipes++; }
        /// <summary>
        /// Whether a buff-up would give the party anything (a stop again after a wipe is made only then, 2026-10-18): a living member lacks
        /// the standing buff of a class in the party (yours or a sim's), or a sim lacks its meal or its elixir and carries one (or a feast
        /// is carried), or you lack yours and carry one.
        /// </summary>
        public bool PartyWantsBuffs()
        {
            float now = Time.time;
            if (Player != null && Player.IsAlive)
            {
                var mine = Kit != null ? Kit.PartyBuff : null; if (mine != null && WantsClassBuff(mine, Player)) return true;
                var st = Player.GetComponent<Combatant>()?.Statuses; var carried = Carried();
                if (st != null && BossBuffs.Wants(st, Consumables.FedId, now) && (BossBuffs.PickFeast(carried) ?? BossBuffs.PickDish(carried)) != null) return true;
                var elixir = BossBuffs.PickElixir(PlayerRole, BossBuffs.MainStat(ClassDef != null ? ClassDef.id : null), carried);
                if (st != null && elixir != null && BossBuffs.Wants(st, Consumables.StatusId(elixir.buff), now)) return true;
            }
            foreach (var c in PartySims)
            {
                if (c == null || c.actor == null || !c.actor.IsAlive) continue;
                var cb = ClassBuffs.For(c.sim.classId); if (cb != null && WantsClassBuff(cb, c.actor)) return true;
                var st = c.GetComponent<Combatant>()?.Statuses; if (st == null) continue; var carried = c.Carried();
                if (BossBuffs.Wants(st, Consumables.FedId, now) && (BossBuffs.PickFeast(carried) ?? BossBuffs.PickDish(carried)) != null) return true;
                var elixir = BossBuffs.PickElixir(c.Role, BossBuffs.MainStat(c.sim.classId), carried);
                if (elixir != null && BossBuffs.Wants(st, Consumables.StatusId(elixir.buff), now)) return true;
            }
            return false;
        }
        /// <summary>A leader stops to buff up: the stop's number.</summary>
        public int BeginBuffStop(SimCompanion leader) { BuffStop++; BuffLeader = leader; BuffStopSince = Time.time; return BuffStop; }
        public void EndBuffStop(SimCompanion leader) { if (BuffLeader == leader) BuffLeader = null; }
        /// <summary>Every living member has buffed at this stop: each sim has taken its turn, and you have (/buff) or need nothing (fed, and an
        /// elixir or a salve on, each with more than BossBuffs.Refresh left).</summary>
        public bool PartyBuffed(int stop)
        {
            foreach (var c in PartySims) if (c != null && c.actor != null && c.actor.IsAlive && c.BuffedStop != stop) return false;
            if (Player == null || !Player.IsAlive || PlayerBuffedStop == stop) return true;
            var st = Player.GetComponent<Combatant>()?.Statuses; float now = Time.time;
            return st != null && !BossBuffs.Wants(st, Consumables.FedId, now) && (!BossBuffs.Wants(st, Consumables.ElixirId, now) || !BossBuffs.Wants(st, Consumables.SalveId, now));
        }

        /// <summary>The party near a point, alive: you, Mira (recruited), the sims; <paramref name="first"/> first when it is one of them.</summary>
        public List<Actor> PartyAround(Vector3 at, float reach, Actor first = null)
        {
            var list = new List<Actor>();
            void Add(Actor a) { if (a != null && a.IsAlive && !list.Contains(a) && Vector3.Distance(a.transform.position, at) <= reach) list.Add(a); }
            Add(first);
            Add(Player);
            if (Companion != null && Progress.recruited && Companion.gameObject.activeInHierarchy) Add(Companion.actor);
            foreach (var s in PartySims) if (s != null) Add(s.actor);
            return list;
        }
        /// <summary>Who shares a feast another eats (a party sim at a stop): the eater first, then the party within Consumables.FeastRange of it.</summary>
        public List<StatusEffectRuntime> FeastCircle(Actor eater)
        {
            var list = new List<StatusEffectRuntime>(); if (eater == null) return list;
            foreach (var a in PartyAround(eater.transform.position, Consumables.FeastRange, eater)) { var c = a.GetComponent<Combatant>(); if (c != null && c.Statuses != null) list.Add(c.Statuses); }
            return list;
        }
        /// <summary>Lays a class's standing buff on the party within BossBuffs.BuffReach of <paramref name="by"/> (each one of it at a time: a
        /// second of the same class's refreshes it). Its name rises over the one who laid it. How many it went on.</summary>
        public int LayClassBuff(ClassBuff buff, Actor by)
        {
            if (buff == null || by == null || !by.IsAlive) return 0;
            int n = 0; var def = buff.Definition();
            foreach (var a in PartyAround(by.transform.position, BossBuffs.BuffReach, by))
            {
                var c = a.GetComponent<Combatant>(); if (c == null || c.Statuses == null) continue;
                c.Statuses.Remove(def.id); if (c.Apply(def)) n++;
            }
            if (n > 0) FloatText(by.transform.position + Vector3.up * 2.2f, buff.name, new Color(1, .9f, .55f));
            return n;
        }
        /// <summary>Whether anyone near <paramref name="by"/> would take a class buff now (none on, or under BossBuffs.Refresh left).</summary>
        public bool WantsClassBuff(ClassBuff buff, Actor by)
        {
            if (buff == null || by == null) return false; float now = Time.time;
            foreach (var a in PartyAround(by.transform.position, BossBuffs.BuffReach, by)) { var c = a.GetComponent<Combatant>(); if (c != null && BossBuffs.Wants(c.Statuses, buff.id, now)) return true; }
            return false;
        }

        /// <summary>Your part at a stop: the one your class plays (BossBuffs.NominalRole), a Paladin with a Warrior sim along deals damage.</summary>
        public SimCompanion.PartyRole PlayerRole
        {
            get
            {
                string id = ClassDef != null ? ClassDef.id : null; var role = BossBuffs.NominalRole(id);
                if (role == SimCompanion.PartyRole.Tank && PartySims.Exists(c => c != null && c.Role == SimCompanion.PartyRole.Tank)) role = SimCompanion.PartyRole.Damage;
                return role;
            }
        }
        int BagIndexOf(System.Func<ItemDef, bool> pick)
        {
            if (Items == null) return -1;
            for (int i = 0; i < Progress.bag.Count; i++) { var st = Progress.bag[i]; if (!st.Empty && pick(Items.Get(st.item))) return i; }
            return -1;
        }
        List<ItemDef> Carried()
        {
            var l = new List<ItemDef>(); if (Items == null) return l;
            foreach (var st in Progress.bag) { if (st.Empty) continue; var d = Items.Get(st.item); if (d != null && d.kind == "consumable" && !l.Contains(d)) l.Add(d); }
            return l;
        }
        /// <summary>
        /// /buff (and the run bot at a stop): out of a fight, from your bags, what you lack (BossBuffs.Wants): a feast for the party when you
        /// carry one, else your best dish; the elixir for your part (BossBuffs.PickElixir); your class's draught under 60% of your pool; and
        /// your class's standing buff on everyone near; Mira's too, if she has one. What was used, in a list (empty: nothing to do).
        /// </summary>
        public List<string> BuffUp()
        {
            var used = new List<string>();
            if (Player == null || !Player.IsAlive) return used;
            if (InCombat) { Message("Not while you are fighting."); return used; }
            var st = Player.GetComponent<Combatant>()?.Statuses; float now = Time.time; var carried = Carried();
            if (st != null && BossBuffs.Wants(st, Consumables.FedId, now))
            {
                var feast = BossBuffs.PickFeast(carried); var food = feast ?? BossBuffs.PickDish(carried);
                int i = food != null ? BagIndexOf(d => d == food) : -1;
                if (i >= 0 && UseItem(i))
                {
                    int fed = 0; if (feast != null) foreach (var a in PartyAround(Player.transform.position, Consumables.FeastRange)) { var c = a.GetComponent<Combatant>(); if (c != null && c.Statuses != null && c.Statuses.Has(Consumables.FedId)) fed++; }
                    used.Add(food.name + (feast != null ? " (" + Mathf.Min(fed, Consumables.PartySize) + " fed)" : ""));
                }
            }
            var elixir = BossBuffs.PickElixir(PlayerRole, BossBuffs.MainStat(ClassDef != null ? ClassDef.id : null), carried);
            if (elixir != null && st != null && BossBuffs.Wants(st, Consumables.StatusId(elixir.buff), now)) { int i = BagIndexOf(d => d == elixir); if (i >= 0 && UseItem(i)) used.Add(elixir.name); }
            var pool = Consumables.PoolOf(ClassDef);
            if (Player.Resource != null && Player.Resource.Pool.Max > 0 && Player.Resource.Pool.Ratio < BossBuffs.DraughtBelow && PotionCooldown <= 0)
            {
                var draught = BossBuffs.PickDraught(pool, carried); int i = draught != null ? BagIndexOf(d => d == draught) : -1;
                if (i >= 0 && UseItem(i)) used.Add(draught.name);
            }
            var mine = Kit != null ? Kit.PartyBuff : null;
            if (mine != null && WantsClassBuff(mine, Player)) { int n = LayClassBuff(mine, Player); if (n > 0) used.Add(mine.name + " on " + n); }
            if (Companion != null && Progress.recruited && Companion.actor != null && Companion.actor.IsAlive)
            {
                var hers = Companion.PartyBuff;   // none of her own yet
                if (hers != null && WantsClassBuff(hers, Companion.actor)) { int n = LayClassBuff(hers, Companion.actor); if (n > 0) used.Add("Mira's " + hers.name + " on " + n); }
            }
            PlayerBuffedStop = BuffStop; PlayerBuffNote = string.Join(", ", used);
            Message(used.Count > 0 ? "Buffed: " + PlayerBuffNote + "." : "Nothing to buff with, or you need nothing.");
            return used;
        }
    }
}
