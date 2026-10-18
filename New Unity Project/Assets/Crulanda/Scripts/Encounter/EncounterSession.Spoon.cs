using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Rusty Spoon as Book 1 chapter 1 tells it, in play (the Lowtowns' rework, pick 5, 2026-10-18; SpoonRules has the rules, the lines and
    /// the citations): the smell on coming in, the corner drunk's ballad, Triad at the back room's card table, and the brawl that takes the
    /// room every so often while you are near, which you may wade into, and which ends with one of them out through the front window (boarded
    /// a while after). The Spoon is the inn whose frame holds ZoneBuilder.SpoonWindowName; any zone without it has none of this. Nothing here
    /// is saved: a visit starts with the window glazed and the next brawl a while off.
    /// </summary>
    public sealed partial class EncounterSession
    {
        ZoneInn spoon; Transform spoonPane, spoonBoards, spoonLane; ZoneInteractable triadTable;
        float smellAgainAt, nextSingAt, nextBrawlAt, brawlEndsAt, nextDownAt, windowFixAt, joinUntil, nextJoinBlow;
        bool spoonInside, joinedBrawl; int joinBlows;
        readonly List<Villager> brawlers = new List<Villager>(), turnedOnYou = new List<Villager>();
        System.Random spoonRng;
        /// <summary>A brawl under way in the Spoon now.</summary>
        public bool SpoonBrawling { get { return brawlEndsAt > 0; } }
        /// <summary>Whether the Spoon's front window stands boarded (a brawl ended through it and it is not glazed yet).</summary>
        public bool SpoonWindowBroken { get { return spoonBoards != null && spoonBoards.gameObject.activeSelf; } }
        /// <summary>The drinkers in the brawl now (tests read it).</summary>
        public IReadOnlyList<Villager> SpoonBrawlers { get { return brawlers; } }
        /// <summary>Seconds until the next brawl may start (tests set it to 0 to start one now).</summary>
        public float SpoonBrawlIn { get { return nextBrawlAt - Time.time; } set { nextBrawlAt = Time.time + value; } }

        /// <summary>On arrival (StartAdit): finds the Spoon, its window and its card table, and puts the Triad table in reach.</summary>
        void StartSpoon()
        {
            spoon = null; spoonPane = spoonBoards = spoonLane = null; triadTable = null; brawlers.Clear(); turnedOnYou.Clear();
            brawlEndsAt = windowFixAt = joinUntil = 0; joinedBrawl = false; nextBrawlAt = 0; spoonInside = false;
            if (Zone == null) return;
            foreach (var inn in Zone.Inns) if (inn != null && inn.frame != null && inn.frame.Find(ZoneBuilder.SpoonWindowName) != null) { spoon = inn; break; }
            if (spoon == null) return;
            spoonRng = new System.Random(Zone.Zone.seed + 105);
            spoonPane = spoon.frame.Find(ZoneBuilder.SpoonWindowName); spoonBoards = spoon.frame.Find(ZoneBuilder.SpoonBoardsName); spoonLane = spoon.frame.Find(ZoneBuilder.SpoonLaneName);
            var cards = spoon.frame.Find(ZoneBuilder.SpoonCardsName);
            if (cards != null)
            {
                triadTable = new ZoneInteractable { name = "The Triad table", kind = "triad", position = cards.position, root = cards };
                Zone.Interactables.Add(triadTable);
            }
        }

        /// <summary>A frame of the Spoon: the smell, the singing, the brawl's clock and its blows, the window's glazing.</summary>
        void TickSpoon()
        {
            if (spoon == null || Player == null || VillageLife.Active == null) return;
            float now = Time.time; var at = Player.transform.position;
            bool inside = spoon.Contains(at), near = Vector3.Distance(at, spoon.frame.position) < SpoonRules.BrawlNear;
            if (inside && !spoonInside && now >= smellAgainAt) Message(SpoonRules.SmellLine);
            if (inside) smellAgainAt = now + SpoonRules.SmellAgainSeconds;
            spoonInside = inside;
            if (windowFixAt > 0 && now >= windowFixAt) { windowFixAt = 0; SetSpoonWindow(false); if (near) Message(SpoonRules.WindowFixedLine); }
            if (!SpoonBrawling)
            {
                if (inside && now >= nextSingAt)
                {
                    nextSingAt = now + Mathf.Lerp(SpoonRules.SingEveryMin, SpoonRules.SingEveryMax, (float)spoonRng.NextDouble());
                    var drunk = VillageLife.Active.Find(SpoonRules.Drunk);
                    if (drunk != null && drunk.Visible && !drunk.Brawling) drunk.Say(SpoonRules.BalladLines[spoonRng.Next(SpoonRules.BalladLines.Length)], 5);
                }
                if (nextBrawlAt <= 0) nextBrawlAt = now + Mathf.Lerp(SpoonRules.FirstBrawlMin, SpoonRules.FirstBrawlMax, (float)spoonRng.NextDouble());
                else if (now >= nextBrawlAt) { if (!near || InCombat || !StartSpoonBrawl(null)) nextBrawlAt = now + 30; }
                return;
            }
            // The brawl: one more goes down every few seconds; wading in, a few take swings at you.
            if (now >= nextDownAt)
            {
                nextDownAt = now + SpoonRules.DownEvery;
                var up = brawlers.FindAll(v => v != null && v.Brawling && !v.KnockedDown && v.Name != SpoonRules.Gorm && !turnedOnYou.Contains(v));
                if (up.Count > 1)
                {
                    var v = up[spoonRng.Next(up.Count)];
                    v.KnockDown(Mathf.Lerp(SpoonRules.DownMin, SpoonRules.DownMax, (float)spoonRng.NextDouble()));
                    FloatText(v.transform.position, "Thud!", new Color(1, .8f, .5f));
                }
                var shouter = brawlers.Find(b => b != null && b.Brawling && !b.KnockedDown && spoonRng.Next(2) == 0);
                if (shouter != null) shouter.Say(SpoonRules.BrawlShouts[spoonRng.Next(SpoonRules.BrawlShouts.Length)], 2.5f);
            }
            if (joinedBrawl && now >= nextJoinBlow && Player.IsAlive)
            {
                nextJoinBlow = now + SpoonRules.JoinBlowEvery;
                var swing = turnedOnYou.Find(v => v != null && v.Brawling && !v.KnockedDown && Vector3.Distance(v.transform.position, at) < 4);
                if (swing != null)
                {
                    swing.Brawl(at, joinUntil - now + 1);
                    int blow = SpoonRules.JoinBlow(Player.Health.Pool.Current, Player.Health.Pool.Max);
                    if (blow > 0) { Player.Health.ApplyDamage(blow); FloatText(at, "−" + blow, new Color(1, .45f, .35f)); }
                }
                // Every other beat you land one back: one of those on you goes down.
                if (++joinBlows % 2 == 0)
                {
                    var hit = turnedOnYou.Find(v => v != null && v.Brawling && !v.KnockedDown);
                    if (hit != null) { hit.KnockDown(Mathf.Lerp(SpoonRules.DownMin, SpoonRules.DownMax, (float)spoonRng.NextDouble())); FloatText(hit.transform.position, "Down!", new Color(1, .85f, .4f)); Message("You put " + hit.Name + " on the boards."); }
                }
                if (turnedOnYou.TrueForAll(v => v == null || !v.Brawling || v.KnockedDown)) joinUntil = Mathf.Min(joinUntil, now);
            }
            if (joinedBrawl && now >= joinUntil) brawlEndsAt = Mathf.Min(brawlEndsAt, now);
            if (now >= brawlEndsAt) EndSpoonBrawl();
        }

        /// <summary>
        /// Starts a brawl among the drinkers in the Spoon (not the keeper or the bartender; Gorm only throws), with <paramref name="cause"/> as its
        /// line (a random start line if null). False when fewer than SpoonRules.MinBrawlers are in the room.
        /// </summary>
        public bool StartSpoonBrawl(string cause)
        {
            if (spoon == null || SpoonBrawling || VillageLife.Active == null) return false;
            brawlers.Clear(); turnedOnYou.Clear();
            foreach (var v in VillageLife.Active.Villagers)
                if (v != null && v.Visible && !v.Brawling && v.Name != SpoonRules.Keeper && v.Name != SpoonRules.Bartender && v.Name != SpoonRules.Gorm && spoon.Contains(v.transform.position)) brawlers.Add(v);
            if (brawlers.Count < SpoonRules.MinBrawlers) { brawlers.Clear(); return false; }
            float now = Time.time; brawlEndsAt = now + SpoonRules.BrawlSeconds; nextDownAt = now + SpoonRules.DownEvery; joinedBrawl = false; joinBlows = 0;
            // Each squares up to the next round the room (the last to the first).
            for (int i = 0; i < brawlers.Count; i++) brawlers[i].Brawl(brawlers[(i + 1) % brawlers.Count].transform.position, SpoonRules.BrawlSeconds + 2);
            Message(cause ?? SpoonRules.StartLines[spoonRng.Next(SpoonRules.StartLines.Length)]);
            var snivels = VillageLife.Active.Find(SpoonRules.Bartender); if (snivels != null && snivels.Visible) snivels.Say(SpoonRules.SnivelsAtStart, 4);
            return true;
        }

        /// <summary>The brawl over: the worst of them out through the front window (by Gorm when he is in), the window boarded, the rest back to
        /// their ale; if you waded in, what the tables spilled.</summary>
        void EndSpoonBrawl()
        {
            brawlEndsAt = 0; nextBrawlAt = Time.time + Mathf.Lerp(SpoonRules.BrawlEveryMin, SpoonRules.BrawlEveryMax, (float)spoonRng.NextDouble());
            var standing = brawlers.FindAll(v => v != null && v.Brawling && !v.KnockedDown);
            var thrown = standing.Count > 0 ? standing[spoonRng.Next(standing.Count)] : brawlers.Find(v => v != null);
            var gorm = VillageLife.Active.Find(SpoonRules.Gorm);
            bool byGorm = gorm != null && gorm.Visible && spoon.Contains(gorm.transform.position);
            foreach (var v in brawlers) if (v != null && v != thrown && !v.KnockedDown) v.EndBrawl();
            if (thrown != null && spoonLane != null)
            {
                thrown.ThrowOut(spoonLane.position, SpoonRules.ThrownSeconds);
                Message(SpoonRules.ThrowLine(thrown.Name, byGorm));
                if (spoonPane != null) FloatText(spoonPane.position, "Crash!", new Color(1, .75f, .45f));
                SetSpoonWindow(true); windowFixAt = Time.time + SpoonRules.WindowRepairSeconds;
            }
            var snivels = VillageLife.Active.Find(SpoonRules.Bartender); if (snivels != null && snivels.Visible) snivels.Say(SpoonRules.SnivelsAtEnd, 5);
            if (joinedBrawl && Player != null && Player.IsAlive)
            {
                int coin = SpoonRules.FloorCoin(Player.Level); Progress.gold += coin;
                Message(SpoonRules.FloorCoinLine + coin + " crowns.");
            }
            joinedBrawl = false; brawlers.Clear(); turnedOnYou.Clear();
        }
        void SetSpoonWindow(bool broken)
        {
            if (spoonPane != null) spoonPane.gameObject.SetActive(!broken);
            if (spoonBoards != null) spoonBoards.gameObject.SetActive(broken);
        }

        /// <summary>Talking to a brawler while the brawl is on wades you in instead (SpoonRules.JoinPrompt).</summary>
        string SpoonTalkPrompt(Villager v) { return SpoonBrawling && !joinedBrawl && v != null && brawlers.Contains(v) && !v.KnockedDown ? SpoonRules.JoinPrompt : null; }
        /// <summary>Wades into the brawl: a few of them turn on you for a while. True when it took the interact.</summary>
        bool SpoonJoin(Villager v)
        {
            if (SpoonTalkPrompt(v) == null) return false;
            JoinSpoonBrawl(); return true;
        }
        /// <summary>Into the brawl (tests call it): the nearest few still standing turn on you.</summary>
        public void JoinSpoonBrawl()
        {
            if (!SpoonBrawling || joinedBrawl || Player == null) return;
            float now = Time.time; joinedBrawl = true; joinUntil = now + SpoonRules.JoinSeconds; nextJoinBlow = now + .8f; joinBlows = 0;
            brawlEndsAt = Mathf.Max(brawlEndsAt, joinUntil + 1);
            var at = Player.transform.position;
            var up = brawlers.FindAll(b => b != null && b.Brawling && !b.KnockedDown);
            up.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));
            turnedOnYou.Clear();
            for (int i = 0; i < up.Count && i < SpoonRules.JoinTurn; i++) { turnedOnYou.Add(up[i]); up[i].Brawl(at, SpoonRules.JoinSeconds + 1); }
            Message(SpoonRules.JoinLine);
        }

        /// <summary>The Triad table when you stand at it and have nobody picked out to talk to (the back room's drinkers sit round it).</summary>
        ZoneInteractable SpoonTableAtHand()
        {
            if (triadTable == null || Player == null || FocusVillager != null) return null;
            return GroundDistance(triadTable.position) < 1.8f ? triadTable : null;
        }
        string SpoonPrompt(ZoneInteractable i)
        {
            if (i == null || i.kind != "triad") return null;
            return SpoonBrawling ? "The table's on its side" : "Sit in at Triad (" + SpoonRules.Stake(Player.Level) + " crowns)";
        }
        /// <summary>A hand of Triad (SpoonRules): the stake down, three cards each, high card takes it; now and then the dealer's Grand Emperor's
        /// Flush and the table over. True when it was the Triad table.</summary>
        bool UseSpoonThing(ZoneInteractable i)
        {
            if (i == null || i.kind != "triad") return false;
            if (SpoonBrawling) { Message("Not now: the cards are all over the floor."); return true; }
            int stake = SpoonRules.Stake(Player.Level);
            if (Progress.gold < stake) { Message("The stake is " + stake + " crowns. The dealer looks at your purse and shakes his head."); return true; }
            Progress.gold -= stake;
            if (spoonRng.NextDouble() < SpoonRules.CheatChance && StartSpoonBrawl(SpoonRules.CheatLine)) return true;
            var (mine, theirs) = SpoonRules.Deal(spoonRng);
            int result = SpoonRules.Compare(mine, theirs); bool crown = SpoonRules.Crown(mine);
            int back = SpoonRules.Payout(stake, result, crown); Progress.gold += back;
            Message("Triad. Three cards; high card takes it. You hold " + SpoonRules.Hand(mine) + "; the table shows " + SpoonRules.Hand(theirs) + ". "
                + (result > 0 ? "Yours: " + back + " crowns back." : result == 0 ? "A push: your stake back." : "The table takes your " + stake + " crowns."));
            if (crown) Message(SpoonRules.CrownLine);
            return true;
        }
    }
}
