using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's fight as pure rules (Docs/WORLD_BOSS_1.md 3.3-3.4; build steps 4 and 5), so each is tested without a scene
    /// (UnravellerRulesTests); EncounterEnemy.Unraveller and EncounterSession.UnravellerFight apply them, with the numbers of
    /// <see cref="UnravellerTuning"/> (EncounterContent/Raids/unraveller.json). The behaviour is CANON (book1 ch.20:111-207: the blink, the
    /// lighthouse, the eaten spell, the erasing touch, steel gone black, the harpoons that pin and haul, salt that calcifies, the shattering;
    /// the river that hides from magic, b1 ch.16:75; the mothers' tale of the unravelling, B1WB:121); every number is GAME-ONLY.
    /// </summary>
    public static class UnravellerRules
    {
        /// <summary>Her phase at a share of her health: 1 "The Hunger" (to 70%), 2 "The Brood" (to 40%), 3 "The Unravelling".</summary>
        public static int Phase(float ratio, UnravellerTuning t) { return ratio > t.phase2At ? 1 : ratio > t.phase3At ? 2 : 3; }
        /// <summary>Her phase now, once reached never lost (run 1: the brood she ate healed her back over 70% and the fight went back to phase 1).</summary>
        public static int PhaseNow(int was, float ratio, UnravellerTuning t) { return Mathf.Max(was, Phase(ratio, t)); }
        public static readonly string[] PhaseNames = { "", "The Hunger", "The Brood", "The Unravelling" };
        /// <summary>How long the next harpoon pins her, <paramref name="pinsSoFar"/> having landed this fight: 12 s, then 11, 10 ... never under 7.</summary>
        public static float PinSeconds(int pinsSoFar, UnravellerTuning t) { return Mathf.Max(t.pinLeast, t.pinFirst - t.pinStep * Mathf.Max(0, pinsSoFar)); }
        /// <summary>Two or more harpoons in her at once: she is hauled toward the river's middle and takes more.</summary>
        public static bool Hauled(int harpoonsIn, UnravellerTuning t) { return harpoonsIn >= t.haulPins; }
        /// <summary>What a blow on her is multiplied by: hauled (x1.1), calcified with someone spat out (x1.5).</summary>
        public static float TakenScale(bool hauled, bool calcified, UnravellerTuning t) { return (hauled ? t.haulTaken : 1) * (calcified ? t.calcifyTaken : 1); }
        /// <summary>Her touch where a blink lands (ch.20:133): 60% of the health of anyone not a tank (a tank takes her as a tank does: nothing more).</summary>
        public static int TouchDamage(int maxHealth, bool tank, UnravellerTuning t) { return tank ? 0 : Mathf.Max(1, Mathf.RoundToInt(maxHealth * t.touchShare)); }
        /// <summary>Unravel's stacks on her target: the armour lost (a share, 6% a stack) and what it takes more (x1.05 a stack).</summary>
        public static float UnravelArmour(int stacks, UnravellerTuning t) { return -t.unravelArmour * Mathf.Max(0, stacks); }
        public static float UnravelTaken(int stacks, UnravellerTuning t) { return 1 + t.unravelTaken * Mathf.Max(0, stacks); }
        /// <summary>No swap: at seven stacks the tank is erased.</summary>
        public static bool Erased(int stacks, UnravellerTuning t) { return stacks >= t.erasedAt; }
        /// <summary>Blackened steel's stacks after one more blow from steel: one more, never over the most (three on her, one on a Weave-Eater).</summary>
        public static int SteelNext(int stacks, int most) { return Mathf.Clamp(stacks + 1, 1, Mathf.Max(1, most)); }
        /// <summary>The Feeding unstopped: what she heals (a share of her pool) and how much heavier her blows grow, for <paramref name="eaten"/> good effects eaten.</summary>
        public static float FeedHeal(int eaten, UnravellerTuning t) { return t.feedHeal * Mathf.Max(0, eaten); }
        public static float FeedGrow(int eaten, UnravellerTuning t) { return t.feedGrow * Mathf.Max(0, eaten); }
        /// <summary>An interrupt takes unless the last one did within <see cref="UnravellerTuning.interruptImmune"/> seconds.</summary>
        public static bool InterruptTakes(float now, float lastInterruptAt, UnravellerTuning t) { return lastInterruptAt < 0 || now - lastInterruptAt >= t.interruptImmune; }
        /// <summary>The Unravelling's pulse, the <paramref name="pulse"/>th (from 0): 1.5% of everyone's health, 0.1% more each pulse.</summary>
        public static float PulseShare(int pulse, UnravellerTuning t) { return t.pulseStart + t.pulseGrow * Mathf.Max(0, pulse); }
        /// <summary>Seconds left against the enrage (the feast at 9:00 from the pull).</summary>
        public static float EnrageLeft(float sincePull, UnravellerTuning t) { return Mathf.Max(0, t.enrageSeconds - sincePull); }
        public static bool Feast(float sincePull, UnravellerTuning t) { return sincePull >= t.enrageSeconds; }
        /// <summary>Whether the ground spikes may burst at <paramref name="spot"/>: never within reach of a held brood (the spikes hurt only the
        /// raid, and a hold is the raid's to keep; raid 7).</summary>
        public static bool SpikeSpotClear(Vector2 spot, IList<Vector2> heldBrood, UnravellerTuning t)
        {
            if (heldBrood != null) foreach (var h in heldBrood) if (Vector2.Distance(spot, h) < t.spikeRadius + 1.5f) return false;
            return true;
        }
        /// <summary>Unmade ground's bite in a second, for one who has stood in it <paramref name="stoodSeconds"/> whole seconds before this
        /// one: 15% of health, 5% more each second stood (raid 4). Under the 60% cap.</summary>
        public static float GroundBite(int stoodSeconds, UnravellerTuning t) { return Mathf.Min(EncounterEnemy.MaxBlowShare, t.unmadeShare + t.unmadeRamp * Mathf.Max(0, stoodSeconds)); }
        /// <summary>A blow on one of her brood: three times as much while it stands calcified by salt ("shattered ... into a million harmless shards").</summary>
        public static int ShatterDamage(int damage, bool calcified, UnravellerTuning t) { return calcified ? Mathf.RoundToInt(damage * t.shatter) : damage; }
        /// <summary>A brood that reaches her is eaten.</summary>
        public static bool BroodEaten(float distanceToHer, UnravellerTuning t) { return distanceToHer <= t.broodEatenReach; }

        /// <summary>Who her blink goes to (ch.20:131): whoever is top of her threat, the one she can see; someone in the river beyond touching is
        /// hidden from her (b1 ch.16:75). Null for nobody.</summary>
        public static string BlinkTarget(IList<UnravellerCandidate> all)
        {
            UnravellerCandidate best = null;
            if (all != null) foreach (var c in all) if (c != null && c.alive && !Hidden(c) && (best == null || c.threat > best.threat)) best = c;
            return best?.id;
        }
        static bool Hidden(UnravellerCandidate c) { return c.inRiver && c.distance > ChemicalRiver.MeleeReach; }
        /// <summary>Who she hunts in phase 3: the one working the most magic (of those who draw her, the most threat), never her target, the
        /// swallowed or the dead. Null for nobody.</summary>
        public static string HuntPick(IList<UnravellerCandidate> all)
        {
            UnravellerCandidate best = null;
            if (all != null) foreach (var c in all) if (c != null && c.alive && c.magic && !c.victim && !c.swallowed && c.distance <= HuntReach && (best == null || c.threat > best.threat)) best = c;
            return best?.id;
        }
        /// <summary>How far from her she looks for whom to hunt (those in her fight, not one standing off on the apron).</summary>
        public const float HuntReach = 25;
        /// <summary>Who she drifts over and swallows: the nearest living member who is not her target (nor the hunted, nor one swallowed).</summary>
        public static string SwallowPick(IList<UnravellerCandidate> all)
        {
            UnravellerCandidate best = null;
            if (all != null) foreach (var c in all) if (c != null && c.alive && !c.victim && !c.swallowed && !c.hunted && (best == null || c.distance < best.distance)) best = c;
            return best?.id;
        }
        /// <summary>Where the hunt ends: the hunted in the river, she lands on empty water and is stunned ("missed her meal"); else the hunted
        /// is struck with everyone within the 5 m.</summary>
        public static bool HuntMissed(bool huntedInRiver) { return huntedInRiver; }
        /// <summary>The hunt's blow on one at <paramref name="distance"/> from where she lands (80%, passing the cap), or 0 outside the 5 m. The
        /// hunted escaping into the river is not struck.</summary>
        public static int HuntDamage(int maxHealth, float distance, bool isHunted, bool huntedInRiver, UnravellerTuning t)
        {
            if (isHunted && huntedInRiver) return 0;
            return distance <= t.huntRadius ? Mathf.RoundToInt(maxHealth * t.huntShare) : 0;
        }
    }

    /// <summary>One of the raid as her rules see it (UnravellerRules' choices).</summary>
    public sealed class UnravellerCandidate
    {
        public string id; public float threat, distance; public bool alive = true, tank, inRiver, magic, victim, swallowed, hunted;
    }

    /// <summary>
    /// The harpoons in her (ch.20:181-191): each that lands pins her (no blink, no move) for <see cref="UnravellerRules.PinSeconds"/> from that
    /// throw, a second shorter each time; each stays in her as long as its pin; two in at once and she is hauled. Pure (times given).
    /// </summary>
    public sealed class PinTrack
    {
        readonly List<float> inHer = new List<float>();
        public int Landed { get; private set; }
        public float PinnedUntil { get; private set; }
        /// <summary>A harpoon lands at <paramref name="now"/>: the seconds it pins her.</summary>
        public float Harpoon(float now, UnravellerTuning t)
        {
            float s = UnravellerRules.PinSeconds(Landed, t); Landed++;
            inHer.RemoveAll(x => x <= now); inHer.Add(now + s);
            PinnedUntil = Mathf.Max(PinnedUntil, now + s);
            return s;
        }
        public int In(float now) { int n = 0; foreach (var x in inHer) if (x > now) n++; return n; }
        public float Left(float now) { return Mathf.Max(0, PinnedUntil - now); }
        public bool Pinned(float now) { return now < PinnedUntil; }
        public void Clear() { inHer.Clear(); Landed = 0; PinnedUntil = 0; }
    }

    /// <summary>Unravel's stacks on each one her blows land on (3.4): a stack a blow, each for unravelSeconds. Pure.</summary>
    public sealed class UnravelStacks
    {
        readonly Dictionary<string, List<float>> on = new Dictionary<string, List<float>>();
        /// <summary>One more on <paramref name="id"/> at <paramref name="now"/>: how many it has now.</summary>
        public int Add(string id, float now, UnravellerTuning t)
        {
            if (!on.TryGetValue(id, out var l)) on[id] = l = new List<float>();
            l.RemoveAll(x => x <= now); l.Add(now + t.unravelSeconds); return l.Count;
        }
        public int Count(string id, float now) { if (id == null || !on.TryGetValue(id, out var l)) return 0; int n = 0; foreach (var x in l) if (x > now) n++; return n; }
        /// <summary>Seconds until the first of its stacks falls off (0 with none).</summary>
        public float NextOff(string id, float now) { if (id == null || !on.TryGetValue(id, out var l)) return 0; float m = float.MaxValue; foreach (var x in l) if (x > now) m = Mathf.Min(m, x - now); return m == float.MaxValue ? 0 : m; }
        public void Clear(string id) { if (id != null) on.Remove(id); }
        public void Clear() { on.Clear(); }
    }

    /// <summary>
    /// Swallowed (3.4; CANON-EXPANDED: her touch erases, ch.20:133; salt forces structure on her, ch.20:203): three salt throws from three
    /// different members within saltWindow seconds calcify her and she spits the swallowed out whole; not freed within swallowSeconds, they
    /// are erased. Salt on her when nobody is swallowed does nothing. Pure.
    /// </summary>
    public sealed class SwallowSalts
    {
        readonly Dictionary<string, float> salted = new Dictionary<string, float>();
        public string Victim { get; private set; }
        public float Since { get; private set; } = -1;
        public bool Active { get { return Victim != null; } }
        public void Begin(string victim, float now) { Victim = victim; Since = now; salted.Clear(); }
        public void End() { Victim = null; Since = -1; salted.Clear(); }
        /// <summary>How many different members have salted her within the window, as of <paramref name="now"/>.</summary>
        public int Salts(float now, UnravellerTuning t) { int n = 0; foreach (var kv in salted) if (now - kv.Value <= t.saltWindow) n++; return n; }
        /// <summary>A salt from <paramref name="thrower"/>: true when it is the one that frees the swallowed (the third of three).</summary>
        public bool Salt(string thrower, float now, UnravellerTuning t)
        {
            if (!Active || thrower == null || thrower == Victim) return false;
            salted[thrower] = now;
            return Salts(now, t) >= t.saltsToFree;
        }
        /// <summary>Held too long: the swallowed is erased.</summary>
        public bool Erased(float now, UnravellerTuning t) { return Active && now - Since >= t.swallowSeconds; }
        public bool SaltedBy(string thrower) { return thrower != null && salted.ContainsKey(thrower); }
    }

    /// <summary>A once-a-fight charge (the Shaman's Barrow-wake, play note 272): ready, spent, and ready again when the fight is over. Pure.</summary>
    public sealed class RezCharge
    {
        public bool Ready { get; private set; } = true;
        /// <summary>Spends it: true when it was ready.</summary>
        public bool Spend() { if (!Ready) return false; Ready = false; return true; }
        /// <summary>The fight over (or wiped): ready again.</summary>
        public void Reset() { Ready = true; }
    }

    /// <summary>A rotation the raid's leader sets (3.5: the harpoons, the interrupts): members in turn; one who cannot (down, swallowed, out of
    /// harpoons) is passed over. Pure.</summary>
    public sealed class RaidRotation
    {
        public readonly List<string> Order = new List<string>();
        int at;
        public RaidRotation(IEnumerable<string> order) { if (order != null) Order.AddRange(order); }
        public bool Contains(string id) { return Order.Contains(id); }
        /// <summary>Whose turn it is now: the one at the turn, or the next after it who is able; null when nobody is.</summary>
        public string Current(Func<string, bool> able)
        {
            for (int k = 0; k < Order.Count; k++) { var id = Order[(at + k) % Order.Count]; if (able == null || able(id)) return id; }
            return null;
        }
        /// <summary><paramref name="id"/> took its turn: the next one is after it.</summary>
        public void Took(string id) { int i = Order.IndexOf(id); if (i >= 0 && Order.Count > 0) at = (i + 1) % Order.Count; }
        public void Skip() { if (Order.Count > 0) at = (at + 1) % Order.Count; }
    }

    /// <summary>
    /// The sims' choices in her fight (3.5), pure so each is tested (RaidJobsTests): the harpoon rotation, the answer to the Feeding, the tank
    /// swap, the three salts on the swallowed, the brood's takers, the hunted's escape, stepping out of marked ground (any ground mark the game
    /// draws: GroundMarks), and the mistakes on purpose.
    /// </summary>
    public static class RaidJobs
    {
        /// <summary>Whom a Shaman raises (play note 272): the most needed of the fallen within reach, a tank, then a healer, then a
        /// damage-dealer, the nearest of a kind first; never one lying in marked ground (the danger that killed them). Null for nobody.</summary>
        public static string RezPick(IList<(string id, SimCompanion.PartyRole role, bool dead, bool inDanger, float distance)> all, float reach)
        {
            string best = null; int bestRank = 9; float bestD = float.MaxValue;
            if (all != null) foreach (var c in all)
            {
                if (!c.dead || c.inDanger || c.distance > reach) continue;
                int rank = c.role == SimCompanion.PartyRole.Tank ? 0 : c.role == SimCompanion.PartyRole.Healer ? 1 : 2;
                if (rank < bestRank || rank == bestRank && c.distance < bestD) { best = c.id; bestRank = rank; bestD = c.distance; }
            }
            return best;
        }
        /// <summary>A harpooner throws now: its turn, a harpoon in its bags, her within throwing reach, and her pin running out (2 s left or free).</summary>
        public static bool HarpoonNow(bool myTurn, float pinLeft, bool hasHarpoon, bool inReach, float lead) { return myTurn && hasHarpoon && inReach && pinLeft <= lead; }
        public enum Answer { None, Interrupt, Harpoon }
        /// <summary>The answer to the Feeding: the interrupter on turn if its kick is ready and she is not immune; else a fresh harpoon (it breaks
        /// the channel: "Bone holds"); else nothing.</summary>
        public static Answer FeedingAnswer(bool feeding, bool interrupterReady, bool immune, bool harpoonerReady)
        {
            if (!feeding) return Answer.None;
            if (interrupterReady && !immune) return Answer.Interrupt;
            return harpoonerReady ? Answer.Harpoon : Answer.None;
        }
        /// <summary>
        /// Whether a tank taunts her now (3.4, the swap at four Unravel stacks): never when she is on it; when she is on the other tank, only once
        /// that tank has <paramref name="swapAt"/> or more and this one has fewer, or that tank is one blow from erased; when she is on someone
        /// not a tank (a blink, the lighthouse), the tank meant to hold her (the one with fewer stacks; <paramref name="preferred"/>).
        /// </summary>
        public static bool TauntHer(int myStacks, int otherStacks, bool onMe, bool onOtherTank, bool preferred, int swapAt, int erasedAt)
        {
            if (onMe) return false;
            if (onOtherTank) return otherStacks >= erasedAt - 1 && myStacks < erasedAt - 1 || otherStacks >= swapAt && myStacks < otherStacks;
            return preferred;
        }
        /// <summary>
        /// The salts on the swallowed (3.5): the three nearest who carry salt throw it. When you are one of the three nearest, two sims throw and
        /// the raid warning tells you to. <paramref name="near"/>: (id, distance to her, carries salt, is you), the swallowed left out.
        /// </summary>
        public static (List<string> sims, bool you) SaltThrowers(IList<(string id, float distance, bool salt, bool you)> near, int need)
        {
            var l = new List<(string id, float distance, bool salt, bool you)>();
            if (near != null) foreach (var n in near) if (n.salt) l.Add(n);
            l.Sort((a, b) => a.distance.CompareTo(b.distance));
            var sims = new List<string>(); bool you = false;
            for (int i = 0; i < l.Count && sims.Count + (you ? 1 : 0) < need; i++) { if (l[i].you) you = true; else sims.Add(l[i].id); }
            return (sims, you);
        }
        /// <summary>
        /// Who holds the brood (raid 4, Chris: "more brood per wave..need CC baby"): the raid's control, in order: the Archivist first (that is
        /// its raid part), then the Rogue, the Scoundrel, the Mage, a Druid not healing; each holds one brood out of the fight in turn (a
        /// sap, a lull, a hex, a sleep). Its place in that order, or -1 for one who holds none (tanks, healers, the rest).
        /// </summary>
        public static int BroodHolder(string classId, SimCompanion.PartyRole role)
        {
            if (role != SimCompanion.PartyRole.Damage) return -1;
            switch (classId) { case "class.archivist": return 0; case "class.rogue": return 1; case "class.scoundrel": return 2; case "class.mage": return 3; case "class.druid": return 4; default: return -1; }
        }
        /// <summary>Who takes the brood while a wave is up (raid 5, Chris: "off tank and dps kill adds while main tank stay on main spider"):
        /// the tank not holding her (it taunts them off the healers), and every damage-dealer, in the kill order, until the wave is dead; then
        /// back to her. The tank holding her stays on her; the healers heal. (A swap mid-wave: the tank who takes her stays, the one she leaves
        /// goes to the brood.)</summary>
        public static bool TakesBrood(SimCompanion.PartyRole role, int party, bool holdsHer, bool melee) { return role == SimCompanion.PartyRole.Tank ? !holdsHer : role == SimCompanion.PartyRole.Damage; }
        /// <summary>
        /// The brood's kill order (raid 5, Chris: "once they are cc they need to be killed systematically or they will break and eventually kill
        /// the raid"): a free one first (it is on the healers; the nearest to breaking first among the free); then the held, Cross before Moon
        /// before unmarked, the one whose hold runs out soonest first. The first is the Skull: the brood-takers kill it, and only it (a blow on
        /// another held one would break it). With every one held, the off-tank grabs the Skull (its blow breaks that hold, its taunt takes it)
        /// and the damage-dealers assist it, waiting on her until it is there; no area blow near a held one. (id, held, its mark, seconds left
        /// on its hold).
        /// </summary>
        public static List<string> KillOrder(IList<(string id, bool held, RaidMark mark, float holdLeft)> brood)
        {
            var l = new List<(string id, bool held, RaidMark mark, float holdLeft)>(); if (brood != null) l.AddRange(brood);
            int Rank(RaidMark m) { return m == RaidMark.Skull ? 0 : m == RaidMark.Cross ? 1 : m == RaidMark.Moon ? 2 : 3; }
            l.Sort((a, b) => a.held != b.held ? (a.held ? 1 : -1) : a.held && Rank(a.mark) != Rank(b.mark) ? Rank(a.mark).CompareTo(Rank(b.mark)) : a.holdLeft.CompareTo(b.holdLeft));
            var ids = new List<string>(); foreach (var x in l) ids.Add(x.id); return ids;
        }
        /// <summary>A sim misses its part of a mechanic now and then (3.5, "mistakes on purpose"): more at 18 than at 20, more when careless (rash:
        /// nerve over .75). <paramref name="roll"/> in 0..1.</summary>
        public static bool Misses(int level, float bold, float roll, UnravellerTuning t)
        {
            float k = Mathf.InverseLerp(18, 20, level);
            float p = Mathf.Lerp(t.simMissAt18, t.simMissAt20, k) + (bold > .75f ? t.simMissCareless : 0);
            return roll < p;
        }
    }
}
