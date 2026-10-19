using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    public enum RollChoice { Undecided, Need, Greed, Pass }

    /// <summary>
    /// A roll for a piece of loot (Round 25, 2026-10-07; playtest note 72: "allow all party members to get loot, and maybe need/greed
    /// uncommon, rare, epic, legendary items"): the thing off the body, each of the party's choice (you by the buttons, the sims by
    /// what they can use), Need over Greed, the highest roll of the top tier wins, all passing leaves it to you. Twenty seconds.
    /// </summary>
    public sealed class LootRoll
    {
        public string item; public int count; public string from;
        public readonly List<(string id, string name, RollChoice choice, int roll)> members = new List<(string, string, RollChoice, int)>();
        public float started, deadline;
        public string Winner { get; private set; }
        public bool Done { get; private set; }
        public RollChoice Of(string id) { foreach (var m in members) if (m.id == id) return m.choice; return RollChoice.Undecided; }
        public void Choose(string id, RollChoice choice, int roll)
        {
            for (int i = 0; i < members.Count; i++) if (members[i].id == id && members[i].choice == RollChoice.Undecided) members[i] = (id, members[i].name, choice, roll);
        }
        public bool AllChosen { get { foreach (var m in members) if (m.choice == RollChoice.Undecided) return false; return true; } }
        /// <summary>Need beats Greed; the highest roll of that tier; nobody wanting it, the looter ("you").</summary>
        public string Resolve()
        {
            Done = true; string best = null; int bestRoll = -1; var tier = RollChoice.Pass;
            foreach (var m in members)
            {
                var c = m.choice == RollChoice.Undecided ? RollChoice.Pass : m.choice;
                if (c == RollChoice.Pass) continue;
                if (c == RollChoice.Need && tier != RollChoice.Need) { tier = RollChoice.Need; best = m.id; bestRoll = m.roll; continue; }
                if (c == tier && m.roll > bestRoll) { best = m.id; bestRoll = m.roll; }
                if (tier == RollChoice.Pass) { tier = c; best = m.id; bestRoll = m.roll; }
            }
            Winner = best ?? "you"; return Winner;
        }
    }

    public sealed partial class EncounterSession
    {
        public const float RollSeconds = 20;
        /// <summary>The roll going on, if one (EncounterHud.DrawRoll).</summary>
        public LootRoll Roll { get; private set; }
        readonly Queue<(string item, int count, string from)> rollQueue = new Queue<(string, int, string)>();
        readonly System.Random rollRng = new System.Random();
        /// <summary>Whether this drop is rolled for rather than taken: in a party, and uncommon or better, or a piece of gear (of any
        /// quality) that is an upgrade for one of the party's sims (Chris, 2026-10-18: "group is not rolling on loot..if upgrade they
        /// need to get it": a common piece went to your bags though the Warrior needed it). Common gear no sim needs is simply taken.</summary>
        public bool Rolled(string item)
        {
            var d = Items?.Get(item); if (PartySims.Count == 0 || d == null) return false;
            if (ComponentBag.IsComponent(d)) return false;   // a legendary piece is each member's own roll (WORLD_BOSS_1.md 2.5)
            // Uncommon and better only (Chris, 2026-10-18: "dont roll on common gear. i just didnt see people win rolls"): commons are taken.
            return d.quality >= 2;
        }
        /// <summary>The trades (for a made piece's weight: SimGear.Fits), or null.</summary>
        public ProfessionDatabase LootTrades { get { return ProfessionContent(); } }
        /// <summary>Your choice by your own upgrade check (the group run's bot, RunCapture): Need for gear you can wear that beats what you
        /// wear in its slot (LootJudge.IsUpgrade), else Pass (all passing, it is yours anyway).</summary>
        public RollChoice YourRollFor(ItemDef d)
        {
            if (d == null || d.kind != "gear" || Items == null) return RollChoice.Pass;
            int slot = ItemDatabase.SlotIndex(d.slot); var eq = Progress.equipment;
            var worn = slot < 0 || slot >= eq.Count || eq[slot].Empty ? null : Items.Get(eq[slot].item);
            return LootJudge.IsUpgrade(d, worn, Progress.Level, Progress.classId) ? RollChoice.Need : RollChoice.Pass;
        }
        void QueueRoll(string item, int count, string from)
        {
            if (Roll != null && !Roll.Done) { rollQueue.Enqueue((item, count, from)); Message(ItemName(item) + " waits its turn to be rolled for."); return; }
            StartRoll(item, count, from);
        }
        void StartRoll(string item, int count, string from)
        {
            var r = new LootRoll { item = item, count = count, from = from, started = Time.time, deadline = Time.time + RollSeconds };
            r.members.Add(("you", "You", RollChoice.Undecided, 0));
            foreach (var c in PartySims) if (c != null) r.members.Add((c.sim.id, c.sim.name, RollChoice.Undecided, 0));
            Roll = r;
            ChatSay(ChatChannel.Party, "You", "rolling for " + ItemName(item) + ": need, greed or pass");
            // The sims decide in a moment or two, each by what it can use.
            foreach (var c in PartySims) if (c != null) StartCoroutine(SimDecides(r, c, 1f + (float)rollRng.NextDouble() * 2.5f));
        }
        System.Collections.IEnumerator SimDecides(LootRoll r, SimCompanion c, float after)
        {
            yield return new WaitForSeconds(after);
            if (r != Roll || r.Done || c == null) yield break;
            ChooseRoll(c.sim.id, c.RollFor(Items?.Get(r.item)));
        }
        /// <summary>A choice made (yours from the buttons, a sim's from its judgement): its roll is thrown with it.</summary>
        public void ChooseRoll(string memberId, RollChoice choice)
        {
            var r = Roll; if (r == null || r.Done || choice == RollChoice.Undecided) return;
            int roll = choice == RollChoice.Pass ? 0 : 1 + rollRng.Next(100);
            r.Choose(memberId, choice, roll);
            string who = memberId == "you" ? "You" : PartySim(memberId)?.sim.name ?? memberId;
            // Said as Chris put it (2026-10-18): "Isolde rolls Need (87)".
            ChatSay(ChatChannel.Party, who, choice == RollChoice.Pass ? "passes on " + ItemName(r.item) : "rolls " + (choice == RollChoice.Need ? "Need" : "Greed") + " (" + roll + ") for " + ItemName(r.item));
            if (r.AllChosen) ResolveRoll();
        }
        void TickRoll()
        {
            var r = Roll; if (r == null) return;
            if (!r.Done && Time.time >= r.deadline) ResolveRoll();
            if (r.Done && Time.time >= r.deadline + 4) { Roll = null; if (rollQueue.Count > 0) { var n = rollQueue.Dequeue(); StartRoll(n.item, n.count, n.from); } }
        }
        void ResolveRoll()
        {
            var r = Roll; if (r == null || r.Done) return;
            string winner = r.Resolve(); r.deadline = Time.time; string name = ItemName(r.item);
            Meter.Loot(name + (r.count > 1 ? " x" + r.count : ""), r.from, winner == "you" ? "You" : (PartySim(winner)?.sim.name ?? winner));   // the scorecard's loot
            // Remembered (5.5): needing over a sim's need counts against you; passing to one that needed counts for you.
            foreach (var m in r.members)
            {
                if (m.id == "you" || m.choice != RollChoice.Need) continue;
                var c = PartySim(m.id); if (c == null) continue;
                if (winner == "you" && r.Of("you") == RollChoice.Need) SimMemory.Note(c.sim, SimMemory.Deed.NeededOverIt);
                else if (winner == m.id && r.Of("you") == RollChoice.Pass) SimMemory.Note(c.sim, SimMemory.Deed.PassedToIt);
            }
            if (winner == "you")
            {
                int left = Items != null ? Inventory.Add(Progress, Items, r.item, r.count) : r.count;
                if (r.count - left > 0) Received(r.item, r.count - left);
                if (left > 0) { Message(name + " would not fit: it lies where " + r.from + " fell."); PutBack(r.from, r.item, left); }
                ChatSay(ChatChannel.Party, "You", r.Of("you") == RollChoice.Pass || r.Of("you") == RollChoice.Undecided ? "nobody wanted " + name + ", it's mine then" : "won " + name);
            }
            else
            {
                var c = PartySim(winner); if (c == null) { PutBack(r.from, r.item, r.count); return; }
                var d = Items?.Get(r.item);
                if (d != null && d.kind == "gear" && r.Of(winner) == RollChoice.Need) { SimEconomy.PutOn(c.sim, d, Items); SimGear.Dress(c.GetComponent<ActorVisual>(), c.sim, Items); ChatSay(ChatChannel.Party, c.sim.name, "won " + name + ", equipping it, ty"); }
                else if (SimMemory.Of(c.sim) == SimMemory.Standing.Friend && r.Of("you") != RollChoice.Pass && Items != null && Inventory.Add(Progress, Items, r.item, r.count) == 0)
                { Received(r.item, r.count); ChatSay(ChatChannel.Party, c.sim.name, "won " + name + ", but you have it, I was only greeding"); Message(c.sim.name + " wins " + name + " and gives it to you."); return; }   // a friend (5.5)
                else { SimEconomy.Add(c.sim, r.item, r.count); ChatSay(ChatChannel.Party, c.sim.name, "won " + name + ", ty"); }
                Message(c.sim.name + " wins " + name + ".");
            }
        }
        void PutBack(string from, string item, int count)
        {
            var body = Enemies.Find(e => e != null && e.actor != null && e.actor.DisplayName == from && !e.actor.IsAlive);
            if (body == null) return; if (body.Drops == null) body.Drops = new List<LootDrop>(); body.Drops.Add(new LootDrop(item, count)); ShowBeacon(body);
        }
        /// <summary>The party's share of a body's coins: split evenly, the sims' into their purses (SimEconomy); yours is what is left over.</summary>
        int SplitCoins(int coins)
        {
            int n = 1; foreach (var c in PartySims) if (c != null) n++;
            if (n == 1) return coins;
            int share = coins / n;
            foreach (var c in PartySims) if (c != null) c.sim.coin += share;
            return coins - share * (n - 1);
        }
    }
}
