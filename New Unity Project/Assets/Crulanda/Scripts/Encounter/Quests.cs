using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Quest content (EncounterContent/Quests/*.json). Any file may hold factions, items, documents and quests;
    // all files are merged and validated together. Design notes: Docs/QUEST_DESIGN.md.
    // ============================================================================================
    [Serializable] public sealed class QuestFile
    {
        public FactionDef[] factions = new FactionDef[0];
        public QuestItemDef[] items = new QuestItemDef[0];
        public DocumentDef[] documents = new DocumentDef[0];
        public QuestDef[] quests = new QuestDef[0];
    }
    /// <summary>A faction you can stand well or badly with. fixedStanding: can't change (e.g. the Concord hunts you regardless).</summary>
    [Serializable] public sealed class FactionDef { public string id, name, description, canonStatus; public int start; public bool fixedStanding; }
    /// <summary>A quest item: carried in the quest bag, not equipment.</summary>
    [Serializable] public sealed class QuestItemDef { public string id, name, description; }
    /// <summary>A readable page kept in the Chronicle (the ledger, a lullaby, a letter).</summary>
    [Serializable] public sealed class DocumentDef { public string id, title, text, canonStatus; }
    /// <summary>
    /// kind: main | side | npc | faction. giver/turnIn: a villager's name, "Mira", or "auto" (starts itself in its zone).
    /// Steps run in order; a step is done when all its objectives are.
    /// level: offered up to 3 levels early, with a grey ! until then. minLevel (optional, 0 = none): below it the quest is
    /// not offered, shows no ! at all, and can't be accepted (the Crowsfoot Hollow quest waits for level 3).
    /// </summary>
    [Serializable] public sealed class QuestDef
    {
        public string id, title, kind = "side", giver, turnIn, zone, canonStatus;
        public int level = 1, minLevel;
        public string[] requires = new string[0];
        public string requiresFaction; public int requiresStanding;
        public string summary, offer, progress, complete;
        public string[] giveOnAccept = new string[0];
        public QuestStepDef[] steps = new QuestStepDef[0];
        public QuestRewardDef rewards = new QuestRewardDef();
        public bool IsMain { get { return kind == "main"; } }
    }
    /// <summary>text: what the tracker says to do; say: the line shown (chat and bubble) when the step completes.</summary>
    [Serializable] public sealed class QuestStepDef { public string text, say, sayBy; public QuestObjectiveDef[] objectives = new QuestObjectiveDef[0]; }
    /// <summary>
    /// type:
    /// - flag: a game state (recruited, equipped:&lt;item&gt;).
    /// - kill: an enemy id; a trailing * matches as a prefix.
    /// - talk: speak to an NPC.
    /// - deliver: bring item to target; it is consumed.
    /// - collect: have count of item; target is where it comes from, an interactable name or "kill:&lt;enemy&gt;".
    /// - interact: use a named interactable.
    /// - visit: stand within radius of at, optionally at night.
    /// For talk and deliver, say is the NPC's line. document: a page revealed when the objective completes.
    /// </summary>
    [Serializable] public sealed class QuestObjectiveDef
    {
        /// <summary>Zone the objective happens in (visit/interact/kill there). Empty = anywhere. Maps point across zones via exits.</summary>
        public string zone;
        public string type, target, item, text, say, document;
        public int count = 1;
        public Vector2 at; public float radius = 6; public bool night;
    }
    [Serializable] public sealed class QuestRewardDef
    {
        public int xp, gold;
        public string[] items = new string[0], documents = new string[0];
        public StandingChange[] reputation = new StandingChange[0];
    }
    [Serializable] public sealed class StandingChange { public string faction; public int amount; }

    // ---------- save state (lives in EncounterProgress, format 4) ----------
    [Serializable] public sealed class QuestState { public string id; public int step; public List<int> counts = new List<int>(); public bool tracked = true; }
    [Serializable] public sealed class FactionStanding { public string faction; public int value; }

    public enum QuestStatus { Unavailable, Available, Active, ReadyToTurnIn, Done }

    /// <summary>All quest content, merged and validated. Throws ArgumentException listing every problem.</summary>
    public sealed class QuestDatabase
    {
        public readonly Dictionary<string, QuestDef> Quests = new Dictionary<string, QuestDef>(StringComparer.Ordinal);
        public readonly List<QuestDef> Ordered = new List<QuestDef>();
        public readonly Dictionary<string, FactionDef> Factions = new Dictionary<string, FactionDef>(StringComparer.Ordinal);
        public readonly List<FactionDef> FactionOrder = new List<FactionDef>();
        public readonly Dictionary<string, QuestItemDef> Items = new Dictionary<string, QuestItemDef>(StringComparer.Ordinal);
        public readonly Dictionary<string, DocumentDef> Documents = new Dictionary<string, DocumentDef>(StringComparer.Ordinal);
        static readonly HashSet<string> Types = new HashSet<string> { "flag", "kill", "talk", "deliver", "collect", "interact", "visit" };
        static readonly HashSet<string> Kinds = new HashSet<string> { "main", "side", "npc", "faction" };

        public static QuestDatabase Parse(IEnumerable<string> jsonFiles)
        {
            var db = new QuestDatabase(); var errors = new List<string>();
            foreach (var json in jsonFiles)
            {
                if (string.IsNullOrWhiteSpace(json)) continue;
                QuestFile f;
                try { f = JsonUtility.FromJson<QuestFile>(json); } catch (Exception e) { errors.Add("Unreadable quest file: " + e.Message); continue; }
                if (f == null) continue;
                foreach (var x in f.factions ?? new FactionDef[0]) if (x != null) { if (!Add(db.Factions, x.id, x, errors, "faction")) continue; db.FactionOrder.Add(x); }
                foreach (var x in f.items ?? new QuestItemDef[0]) if (x != null) Add(db.Items, x.id, x, errors, "item");
                foreach (var x in f.documents ?? new DocumentDef[0]) if (x != null) Add(db.Documents, x.id, x, errors, "document");
                foreach (var x in f.quests ?? new QuestDef[0]) if (x != null && Add(db.Quests, x.id, x, errors, "quest")) db.Ordered.Add(x);
            }
            foreach (var q in db.Ordered) db.Check(q, errors);
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            return db;
        }
        static bool Add<T>(Dictionary<string, T> map, string id, T value, List<string> errors, string what)
        {
            if (string.IsNullOrEmpty(id)) { errors.Add("A " + what + " has no id."); return false; }
            if (map.ContainsKey(id)) { errors.Add("Duplicate " + what + " id '" + id + "'."); return false; }
            map[id] = value; return true;
        }
        void Check(QuestDef q, List<string> errors)
        {
            string p = "Quest '" + q.id + "': ";
            if (string.IsNullOrEmpty(q.title)) errors.Add(p + "no title.");
            if (!Kinds.Contains(q.kind ?? "")) errors.Add(p + "unknown kind '" + q.kind + "'.");
            if (string.IsNullOrEmpty(q.giver)) errors.Add(p + "no giver.");
            if (string.IsNullOrEmpty(q.turnIn)) errors.Add(p + "no turnIn.");
            if (q.steps == null || q.steps.Length == 0) errors.Add(p + "no steps.");
            if (q.minLevel < 0 || q.minLevel > EncounterProgress.LevelCap) errors.Add(p + "minLevel " + q.minLevel + " is outside 0-" + EncounterProgress.LevelCap + ".");
            foreach (var r in q.requires ?? new string[0]) if (!Quests.ContainsKey(r)) errors.Add(p + "requires unknown quest '" + r + "'.");
            if (!string.IsNullOrEmpty(q.requiresFaction) && !Factions.ContainsKey(q.requiresFaction)) errors.Add(p + "unknown faction '" + q.requiresFaction + "'.");
            foreach (var i in q.giveOnAccept ?? new string[0]) if (!Items.ContainsKey(i)) errors.Add(p + "gives unknown item '" + i + "'.");
            if (q.steps != null)
                for (int s = 0; s < q.steps.Length; s++)
                {
                    var step = q.steps[s];
                    if (step == null || step.objectives == null || step.objectives.Length == 0) { errors.Add(p + "step " + s + " has no objectives."); continue; }
                    foreach (var o in step.objectives)
                    {
                        if (o == null || !Types.Contains(o.type ?? "")) { errors.Add(p + "step " + s + " has an unknown objective type '" + o?.type + "'."); continue; }
                        if (o.count < 1) errors.Add(p + "step " + s + " objective count must be at least 1.");
                        if ((o.type == "deliver" || o.type == "collect") && (string.IsNullOrEmpty(o.item) || !Items.ContainsKey(o.item))) errors.Add(p + "step " + s + " needs a known item.");
                        if (o.type != "visit" && string.IsNullOrEmpty(o.target)) errors.Add(p + "step " + s + " " + o.type + " needs a target.");
                        if (!string.IsNullOrEmpty(o.document) && !Documents.ContainsKey(o.document)) errors.Add(p + "unknown document '" + o.document + "'.");
                    }
                }
            var rw = q.rewards ?? new QuestRewardDef();
            foreach (var i in rw.items ?? new string[0]) if (!Items.ContainsKey(i)) errors.Add(p + "rewards unknown item '" + i + "'.");
            foreach (var d in rw.documents ?? new string[0]) if (!Documents.ContainsKey(d)) errors.Add(p + "rewards unknown document '" + d + "'.");
            foreach (var r in rw.reputation ?? new StandingChange[0]) if (r == null || !Factions.ContainsKey(r.faction ?? "")) errors.Add(p + "reputation for unknown faction '" + r?.faction + "'.");
        }
        public string ItemName(string id) { return id != null && Items.TryGetValue(id, out var i) ? i.name : id; }
    }

    /// <summary>
    /// The player's quests: offering, accepting, advancing on game events, turning in, rewards and faction standing.
    /// Pure logic over <see cref="EncounterProgress"/> so it can be tested without a scene. Messages go to <see cref="Say"/>.
    /// </summary>
    public sealed class QuestLog
    {
        public readonly QuestDatabase Db;
        public EncounterProgress Progress { get; private set; }
        /// <summary>(text, speaker or null) for the chat log and speech bubbles.</summary>
        public Action<string, string> Say = (t, s) => { };
        /// <summary>A document was revealed (the HUD opens it to read).</summary>
        public Action<string> Revealed = id => { };
        public QuestLog(QuestDatabase db, EncounterProgress progress) { Db = db; Bind(progress); }
        public void Bind(EncounterProgress progress)
        {
            Progress = progress;
            if (Progress.quests == null) Progress.quests = new List<QuestState>();
            if (Progress.questsDone == null) Progress.questsDone = new List<string>();
            if (Progress.reputation == null) Progress.reputation = new List<FactionStanding>();
            if (Progress.documents == null) Progress.documents = new List<string>();
            if (Progress.questItems == null) Progress.questItems = new List<string>();
            if (Progress.usedInteractables == null) Progress.usedInteractables = new List<string>();
        }

        // ---------- queries ----------
        public QuestDef Def(string id) { return id != null && Db.Quests.TryGetValue(id, out var q) ? q : null; }
        public QuestState State(string id) { return Progress.quests.Find(s => s.id == id); }
        public bool IsDone(string id) { return Progress.questsDone.Contains(id); }
        public QuestStatus Status(QuestDef q, string zoneId)
        {
            if (IsDone(q.id)) return QuestStatus.Done;
            var s = State(q.id);
            if (s != null) return s.step >= q.steps.Length ? QuestStatus.ReadyToTurnIn : QuestStatus.Active;
            if (!string.IsNullOrEmpty(q.zone) && q.zone != zoneId) return QuestStatus.Unavailable;
            foreach (var r in q.requires) if (!IsDone(r)) return QuestStatus.Unavailable;
            if (!string.IsNullOrEmpty(q.requiresFaction) && Standing(q.requiresFaction) < q.requiresStanding) return QuestStatus.Unavailable;
            return QuestStatus.Available;
        }
        public QuestStepDef CurrentStep(QuestState s) { var q = Def(s.id); return q == null || s.step >= q.steps.Length ? null : q.steps[s.step]; }
        public IEnumerable<(QuestDef quest, QuestState state)> Active()
        {
            foreach (var s in Progress.quests) { var q = Def(s.id); if (q != null) yield return (q, s); }
        }
        public int ItemCount(string item) { int n = 0; foreach (var i in Progress.questItems) if (i == item) n++; return n; }
        /// <summary>Progress on an objective of the current step (collect counts come from the quest bag).</summary>
        public int Count(QuestState s, int objective)
        {
            var step = CurrentStep(s); if (step == null) return 0;
            var o = step.objectives[objective];
            if (o.type == "collect") return Math.Min(o.count, ItemCount(o.item));
            return objective < s.counts.Count ? s.counts[objective] : 0;
        }
        public bool ObjectiveDone(QuestState s, int objective) { var step = CurrentStep(s); return step != null && Count(s, objective) >= step.objectives[objective].count; }
        public string ObjectiveLine(QuestState s, int objective)
        {
            var o = CurrentStep(s).objectives[objective];
            string text = !string.IsNullOrEmpty(o.text) ? o.text : o.type == "kill" ? "Defeat " + o.target : o.type == "collect" ? Db.ItemName(o.item) :
                o.type == "deliver" ? "Bring " + Db.ItemName(o.item) + " to " + o.target : o.type == "talk" ? "Speak with " + o.target : o.type == "interact" ? o.target : "Go there";
            return o.count > 1 ? text + ": " + Count(s, objective) + "/" + o.count : text;
        }

        // ---------- faction standing ----------
        public int Standing(string faction)
        {
            var s = Progress.reputation.Find(r => r.faction == faction);
            if (s != null) return s.value;
            return Db.Factions.TryGetValue(faction ?? "", out var f) ? f.start : 0;
        }
        public static readonly string[] TierNames = { "Hostile", "Distrusted", "Neutral", "Trusted", "Honoured", "Sworn" };
        static readonly int[] TierFloors = { int.MinValue, -3000, 0, 1000, 3000, 6000 };
        public static int Tier(int value) { int t = 0; for (int i = 0; i < TierFloors.Length; i++) if (value >= TierFloors[i]) t = i; return t; }
        /// <summary>Progress through the current tier, 0..1 (Sworn is always full).</summary>
        public static float TierProgress(int value)
        {
            int t = Tier(value); if (t >= TierFloors.Length - 1) return 1;
            int lo = t == 0 ? -6000 : TierFloors[t], hi = TierFloors[t + 1];
            return Mathf.Clamp01((value - lo) / (float)(hi - lo));
        }
        public void ChangeStanding(string faction, int amount)
        {
            if (!Db.Factions.TryGetValue(faction ?? "", out var f) || f.fixedStanding || amount == 0) return;
            var s = Progress.reputation.Find(r => r.faction == faction);
            if (s == null) { s = new FactionStanding { faction = faction, value = f.start }; Progress.reputation.Add(s); }
            int before = Tier(s.value); s.value = Mathf.Clamp(s.value + amount, -6000, 12000);
            Say((amount > 0 ? "Standing with " : "Standing lost with ") + f.name + " " + (amount > 0 ? "+" : "") + amount + ".", null);
            int after = Tier(s.value);
            if (after != before) Say("You are now " + TierNames[after] + " with " + f.name + ".", null);
        }

        // ---------- accept / abandon / turn in ----------
        /// <summary>Takes on a quest that is available here. Refuses one the character is below <see cref="QuestDef.minLevel"/> for.</summary>
        public bool Accept(QuestDef q, string zoneId)
        {
            if (q == null || Status(q, zoneId) != QuestStatus.Available || Progress.Level < q.minLevel) return false;
            var s = new QuestState { id = q.id }; Progress.quests.Add(s);
            foreach (var item in q.giveOnAccept) Progress.questItems.Add(item);
            Say((q.IsMain ? "Chronicle begun: " : "Quest accepted: ") + q.title, null);
            PrepareStep(q, s);
            return true;
        }
        public bool Abandon(string id)
        {
            var q = Def(id); var s = State(id);
            if (q == null || s == null || q.IsMain) return false;
            Progress.quests.Remove(s);
            foreach (var item in q.giveOnAccept) Progress.questItems.Remove(item);
            Say("Quest abandoned: " + q.title, null);
            return true;
        }
        /// <summary>Hands in a finished quest and pays out. Returns false if it isn't ready.</summary>
        public bool TurnIn(QuestDef q, out int xp)
        {
            xp = 0; var s = q == null ? null : State(q.id);
            if (s == null || s.step < q.steps.Length) return false;
            Progress.quests.Remove(s); Progress.questsDone.Add(q.id);
            var r = q.rewards ?? new QuestRewardDef();
            xp = r.xp; Progress.experience += r.xp; Progress.gold += r.gold;
            foreach (var i in r.items) Progress.questItems.Add(i);
            foreach (var d in r.documents) Reveal(d);
            Say((q.IsMain ? "Chronicle complete: " : "Quest complete: ") + q.title + (r.xp > 0 ? "  +" + r.xp + " XP" : "") + (r.gold > 0 ? "  +" + r.gold + " gold" : ""), null);
            foreach (var c in r.reputation) ChangeStanding(c.faction, c.amount);
            return true;
        }
        void Reveal(string doc)
        {
            if (string.IsNullOrEmpty(doc) || Progress.documents.Contains(doc)) return;
            Progress.documents.Add(doc);
            Say("New page in your Chronicle: " + (Db.Documents.TryGetValue(doc, out var d) ? d.title : doc) + " [L]", null);
            Revealed(doc);
        }

        // ---------- events ----------
        /// <summary>
        /// A game event: kill (enemy id), talk (npc name), interact (interactable name), visit (objective place),
        /// flag (state name), item (bag changed). Advances every matching objective of every active quest's current step.
        /// </summary>
        public void Notify(string type, string target, int amount = 1)
        {
            foreach (var s in Progress.quests.ToArray())
            {
                var q = Def(s.id); var step = CurrentStep(s); if (q == null || step == null) continue;
                bool changed = false;
                for (int i = 0; i < step.objectives.Length; i++)
                {
                    var o = step.objectives[i];
                    if (o.type == "collect") { if (type == "item") changed = true; continue; }
                    if (o.type != type || !Matches(o.target, target)) continue;
                    if (o.type == "deliver") continue;   // handled by Deliver (needs the item in hand)
                    while (s.counts.Count <= i) s.counts.Add(0);
                    if (s.counts[i] >= o.count) continue;
                    s.counts[i] = Math.Min(o.count, s.counts[i] + amount); changed = true;
                    if (o.count > 1) Say(ObjectiveLine(s, i), null);
                    if (s.counts[i] >= o.count) Completed(o);
                }
                if (changed) Advance(q, s);
            }
        }
        /// <summary>Visit objectives: <paramref name="inside"/> says whether the player is there now (radius, and night if required).</summary>
        public void CheckVisits(Func<QuestObjectiveDef, bool> inside)
        {
            foreach (var s in Progress.quests.ToArray())
            {
                var q = Def(s.id); var step = CurrentStep(s); if (q == null || step == null) continue;
                bool changed = false;
                for (int i = 0; i < step.objectives.Length; i++)
                {
                    var o = step.objectives[i]; if (o.type != "visit") continue;
                    while (s.counts.Count <= i) s.counts.Add(0);
                    if (s.counts[i] >= o.count || !inside(o)) continue;
                    s.counts[i] = o.count; changed = true; Completed(o);
                }
                if (changed) Advance(q, s);
            }
        }
        /// <summary>Visit objectives of active quests (for map markers and the position check).</summary>
        public IEnumerable<(QuestDef quest, QuestObjectiveDef objective, bool done)> Places()
        {
            foreach (var (q, s) in Active())
            {
                var step = CurrentStep(s); if (step == null) continue;
                for (int i = 0; i < step.objectives.Length; i++) if (step.objectives[i].type == "visit") yield return (q, step.objectives[i], ObjectiveDone(s, i));
            }
        }
        /// <summary>Talking to someone: completes talk objectives and hands over deliveries. True if any quest cared.</summary>
        public bool TalkTo(string npc)
        {
            bool any = false;
            foreach (var s in Progress.quests.ToArray())
            {
                var q = Def(s.id); var step = CurrentStep(s); if (q == null || step == null) continue;
                for (int i = 0; i < step.objectives.Length; i++)
                {
                    var o = step.objectives[i];
                    if ((o.type != "talk" && o.type != "deliver") || !Matches(o.target, npc)) continue;
                    while (s.counts.Count <= i) s.counts.Add(0);
                    if (s.counts[i] >= o.count) continue;
                    if (o.type == "deliver")
                    {
                        if (ItemCount(o.item) < o.count) continue;
                        for (int k = 0; k < o.count; k++) Progress.questItems.Remove(o.item);
                    }
                    s.counts[i] = o.count; any = true; Completed(o, npc);
                }
                Advance(q, s);
            }
            return any;
        }
        /// <summary>Does any active objective want this item right now (so gathering/looting may give it)?</summary>
        public bool Wants(string item, string source)
        {
            foreach (var (q, s) in Active())
            {
                var step = CurrentStep(s); if (step == null) continue;
                foreach (var o in step.objectives)
                    if (o.type == "collect" && o.item == item && ItemCount(item) < o.count && (string.IsNullOrEmpty(o.target) || Matches(o.target, source))) return true;
            }
            return false;
        }
        /// <summary>Loot for "collect ... from kill:&lt;enemy&gt;" objectives. Returns the item given, or null.</summary>
        public string LootFrom(string enemyId)
        {
            foreach (var (q, s) in Active())
            {
                var step = CurrentStep(s); if (step == null) continue;
                foreach (var o in step.objectives)
                    if (o.type == "collect" && o.target != null && o.target.StartsWith("kill:") && Matches(o.target.Substring(5), enemyId) && ItemCount(o.item) < o.count)
                    { GiveItem(o.item); return o.item; }
            }
            return null;
        }
        public void GiveItem(string item)
        {
            Progress.questItems.Add(item);
            Say("Received: " + Db.ItemName(item) + ".", null);
            Notify("item", item);
        }
        void Completed(QuestObjectiveDef o, string speaker = null)
        {
            // A kill or flag line is narration: its target is an enemy id pattern or a state name, not someone speaking.
            if (!string.IsNullOrEmpty(o.say)) Say(o.say, speaker ?? (o.type == "kill" || o.type == "flag" ? null : o.target));
            if (!string.IsNullOrEmpty(o.document)) Reveal(o.document);
        }
        void Advance(QuestDef q, QuestState s)
        {
            while (s.step < q.steps.Length)
            {
                var step = q.steps[s.step];
                for (int i = 0; i < step.objectives.Length; i++) if (!ObjectiveDone(s, i)) return;
                foreach (var o in step.objectives) if (o.type == "collect") Completed(o);   // collect lines/pages fire when the step is done
                if (!string.IsNullOrEmpty(step.say)) Say(step.say, step.sayBy);
                s.step++; s.counts.Clear();
                if (s.step < q.steps.Length) Say(q.title + ": " + q.steps[s.step].text, null);
                else Say(q.title + ": return to " + q.turnIn + ".", null);
            }
        }
        // A new step may already be satisfied (flags already set, items already carried); the session calls Reconcile after events.
        void PrepareStep(QuestDef q, QuestState s) { }
        /// <summary>
        /// Brings new steps up to date with the world. Set flags, dead enemies and carried items all count.
        /// Called by the session after events and on load.
        /// </summary>
        public void Reconcile(Func<string, bool> flag, Func<string, int> killedMatching)
        {
            for (int guard = 0; guard < 8; guard++)
            {
                bool changed = false;
                foreach (var s in Progress.quests.ToArray())
                {
                    var q = Def(s.id); var step = CurrentStep(s); if (q == null || step == null) continue;
                    for (int i = 0; i < step.objectives.Length; i++)
                    {
                        var o = step.objectives[i];
                        while (s.counts.Count <= i) s.counts.Add(0);
                        int want = s.counts[i];
                        if (o.type == "flag" && flag(o.target)) want = o.count;
                        if (o.type == "kill" && killedMatching != null) want = Math.Max(want, Math.Min(o.count, killedMatching(o.target)));
                        if (want != s.counts[i]) { s.counts[i] = want; changed = true; if (want >= o.count) Completed(o); }
                    }
                    int before = s.step; Advance(q, s); if (s.step != before) changed = true;
                }
                if (!changed) break;
            }
        }
        public static bool Matches(string pattern, string value)
        {
            if (string.IsNullOrEmpty(pattern) || value == null) return false;
            return pattern.EndsWith("*") ? value.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal) : pattern == value;
        }

        // ---------- givers ----------
        /// <summary>
        /// What this NPC offers right now: quests to hand in first, then new ones. New ones are offered from 3 levels below
        /// their level, and never below their <see cref="QuestDef.minLevel"/>.
        /// </summary>
        public List<(QuestDef quest, QuestStatus status)> For(string npc, string zoneId, int level)
        {
            var list = new List<(QuestDef, QuestStatus)>();
            foreach (var q in Db.Ordered)
            {
                var st = Status(q, zoneId);
                if (st == QuestStatus.ReadyToTurnIn && q.turnIn == npc) list.Add((q, st));
            }
            foreach (var q in Db.Ordered)
            {
                var st = Status(q, zoneId);
                if (st == QuestStatus.Available && q.giver == npc && q.level <= level + 3 && level >= q.minLevel) list.Add((q, st));
            }
            return list;
        }
        /// <summary>
        /// Head marker for an NPC: '?' gold (hand in, or someone waiting to hear from you), '!' gold (offer), '?' grey (in progress), or none.
        /// An offer more than 3 levels above you shows a grey '!'; one you are below the minLevel of shows nothing.
        /// </summary>
        public char Marker(string npc, string zoneId, int level, out bool grey)
        {
            grey = false;
            foreach (var (q, s) in Active())
            {
                if (s.step >= q.steps.Length && q.turnIn == npc) return '?';
                var step = CurrentStep(s); if (step == null) continue;
                foreach (var o in step.objectives)
                    if ((o.type == "talk" || (o.type == "deliver" && ItemCount(o.item) >= o.count)) && Matches(o.target, npc) && !ObjectiveDoneFor(s, step, o)) return '?';
            }
            foreach (var q in Db.Ordered)
                if (q.giver == npc && q.giver != "auto" && level >= q.minLevel && Status(q, zoneId) == QuestStatus.Available) { grey = q.level > level + 3; return '!'; }
            foreach (var (q, s) in Active()) if (q.turnIn == npc && s.step < q.steps.Length) { grey = true; return '?'; }
            return ' ';
        }
        bool ObjectiveDoneFor(QuestState s, QuestStepDef step, QuestObjectiveDef o) { int i = Array.IndexOf(step.objectives, o); return i >= 0 && ObjectiveDone(s, i); }
        /// <summary>Starts "auto" quests for this zone (the Chronicle begins by itself).</summary>
        public void StartAutomatic(string zoneId)
        {
            foreach (var q in Db.Ordered) if (q.giver == "auto" && Status(q, zoneId) == QuestStatus.Available) Accept(q, zoneId);
        }
    }
}
