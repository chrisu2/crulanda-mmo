using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Trades: gathering skills (mining, woodcutting, herbalism), Cooking for everyone, and the crafts a character takes up.
    // The content lives in EncounterContent/Professions/*.json: the trades themselves, the kinds of node that can be worked
    // in the world, and the recipes. What a character has learned is saved as EncounterProgress.professions (save format 8).
    // ============================================================================================
    [Serializable] public sealed class ProfessionFile
    {
        /// <summary>How many crafts a character may hold at once (0 = not said in this file; the default is 2).</summary>
        public int craftSlots;
        public ProfessionDef[] professions = new ProfessionDef[0];
        public NodeDef[] nodes = new NodeDef[0];
        public RecipeDef[] recipes = new RecipeDef[0];
    }
    /// <summary>
    /// kind: gather (free for everyone, no limit) | free (everyone has it from the start and cannot forget it) | craft (one of the
    /// few a character may hold). tool: the item that teaches it (kind tool, with teaches = this id); a gathering skill with no
    /// tool is known from the start. station: where its recipes are made (forge, bench or fire; alternatives joined with |).
    /// taught: the line said when its tool is first used.
    /// </summary>
    [Serializable] public sealed class ProfessionDef { public string id, name, kind, tool, verb, station, trainerRole, description, taught, canonStatus; }
    /// <summary>
    /// A kind of thing that can be worked in the world (an ore seam, a windfall, a herb). skill: the skill at which it comes
    /// easily. min-max: how many of item one working gives. respawn: seconds until it can be worked again. seconds: how long
    /// the work takes. look and variant choose the prop the zone builds; prompt is what E offers.
    /// </summary>
    [Serializable] public sealed class NodeDef
    {
        public string id, name, profession, item, look, prompt;
        public int skill = 1, min = 1, max = 1, variant;
        public float respawn = 180, seconds = 2;
    }
    /// <summary>Something made at a station: inputs from the bags become count of output. skill: the trade skill it needs.</summary>
    [Serializable] public sealed class RecipeDef
    {
        public string id, name, profession, station, output;
        public int skill = 1, count = 1;
        public RecipeInput[] inputs = new RecipeInput[0];
    }
    [Serializable] public sealed class RecipeInput { public string item; public int count = 1; }
    /// <summary>One learned trade in the save: its id and the skill reached (1-100).</summary>
    [Serializable] public sealed class ProfessionSkill { public string id; public int skill; }

    public sealed class ProfessionDatabase
    {
        public const int MaxSkill = 100;
        public static readonly string[] Kinds = { "gather", "free", "craft" };
        public static readonly string[] StationKinds = { "forge", "bench", "fire" };
        /// <summary>The trades in the order the files list them (the order the Trades window shows them).</summary>
        public readonly List<ProfessionDef> Order = new List<ProfessionDef>();
        public readonly List<NodeDef> Nodes = new List<NodeDef>();
        public readonly List<RecipeDef> Recipes = new List<RecipeDef>();
        public int CraftSlots { get; private set; } = 2;
        readonly Dictionary<string, ProfessionDef> professions = new Dictionary<string, ProfessionDef>(StringComparer.Ordinal);
        readonly Dictionary<string, NodeDef> nodes = new Dictionary<string, NodeDef>(StringComparer.Ordinal);
        readonly Dictionary<string, RecipeDef> recipes = new Dictionary<string, RecipeDef>(StringComparer.Ordinal);

        /// <summary>
        /// Reads the profession files against the item database. Every problem is collected and thrown as one ArgumentException:
        /// - a trade, node or recipe with no id, or an id used twice; a trade with no name or an unknown kind;
        /// - a tool that is not an item of kind tool, or that teaches another trade; a tool item that teaches no known trade;
        /// - a station that is not forge, bench or fire;
        /// - a node whose trade is not a gathering skill, whose item is unknown, whose skill is outside 1-100, whose yield is
        ///   below 1 or has min over max, or whose respawn or working time is not above zero;
        /// - a recipe with an unknown trade, output or input, no inputs, a count below 1, a skill outside 1-100 or no station.
        /// </summary>
        public static ProfessionDatabase Parse(IEnumerable<string> jsonFiles, ItemDatabase items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var db = new ProfessionDatabase(); var errors = new List<string>(); var files = new List<ProfessionFile>();
            foreach (var json in jsonFiles ?? new string[0])
            {
                if (string.IsNullOrWhiteSpace(json)) continue;
                ProfessionFile f; try { f = JsonUtility.FromJson<ProfessionFile>(json); } catch (Exception e) { errors.Add("Unreadable profession file: " + e.Message); continue; }
                if (f != null) files.Add(f);
            }
            // Trades first, across every file, so a node or recipe may name a trade from another file.
            foreach (var f in files)
            {
                if (f.craftSlots > 0) db.CraftSlots = f.craftSlots;
                foreach (var d in f.professions ?? new ProfessionDef[0])
                {
                    if (d == null || string.IsNullOrEmpty(d.id)) { errors.Add("A profession has no id."); continue; }
                    if (db.professions.ContainsKey(d.id)) { errors.Add("Duplicate profession '" + d.id + "'."); continue; }
                    string p = "Profession '" + d.id + "': ";
                    if (string.IsNullOrEmpty(d.name)) errors.Add(p + "no name.");
                    if (Array.IndexOf(Kinds, d.kind) < 0) errors.Add(p + "unknown kind '" + d.kind + "'.");
                    if (!string.IsNullOrEmpty(d.tool))
                    {
                        var tool = items.Get(d.tool);
                        if (tool == null) errors.Add(p + "unknown tool '" + d.tool + "'.");
                        else if (tool.kind != "tool") errors.Add(p + "its tool '" + d.tool + "' is not an item of kind tool.");
                        else if (tool.teaches != d.id) errors.Add(p + "its tool '" + d.tool + "' teaches '" + tool.teaches + "'.");
                    }
                    if (!string.IsNullOrEmpty(d.station)) foreach (var s in Stations(d.station)) if (Array.IndexOf(StationKinds, s) < 0) errors.Add(p + "unknown station '" + s + "'.");
                    db.professions[d.id] = d; db.Order.Add(d);
                }
            }
            foreach (var i in items.Items.Values)
                if (!string.IsNullOrEmpty(i.teaches) && !db.professions.ContainsKey(i.teaches)) errors.Add("Item '" + i.id + "' teaches unknown profession '" + i.teaches + "'.");
            foreach (var f in files)
            {
                foreach (var n in f.nodes ?? new NodeDef[0])
                {
                    if (n == null || string.IsNullOrEmpty(n.id)) { errors.Add("A node has no id."); continue; }
                    if (db.nodes.ContainsKey(n.id)) { errors.Add("Duplicate node '" + n.id + "'."); continue; }
                    string p = "Node '" + n.id + "': ";
                    if (!db.professions.TryGetValue(n.profession ?? "", out var trade)) errors.Add(p + "unknown profession '" + n.profession + "'.");
                    else if (trade.kind != "gather") errors.Add(p + "'" + n.profession + "' is not a gathering skill.");
                    if (items.Get(n.item) == null) errors.Add(p + "unknown item '" + n.item + "'.");
                    if (n.skill < 1 || n.skill > MaxSkill) errors.Add(p + "skill " + n.skill + " is outside 1-" + MaxSkill + ".");
                    if (n.min < 1 || n.min > n.max) errors.Add(p + "yield " + n.min + "-" + n.max + " is not a range.");
                    if (!(n.respawn > 0) || !(n.seconds > 0)) errors.Add(p + "respawn and seconds must be above zero.");
                    db.nodes[n.id] = n; db.Nodes.Add(n);
                }
                foreach (var r in f.recipes ?? new RecipeDef[0])
                {
                    if (r == null || string.IsNullOrEmpty(r.id)) { errors.Add("A recipe has no id."); continue; }
                    if (db.recipes.ContainsKey(r.id)) { errors.Add("Duplicate recipe '" + r.id + "'."); continue; }
                    string p = "Recipe '" + r.id + "': ";
                    if (!db.professions.ContainsKey(r.profession ?? "")) errors.Add(p + "unknown profession '" + r.profession + "'.");
                    if (r.skill < 1 || r.skill > MaxSkill) errors.Add(p + "skill " + r.skill + " is outside 1-" + MaxSkill + ".");
                    var stations = Stations(r.station);
                    if (stations.Length == 0) errors.Add(p + "no station.");
                    foreach (var s in stations) if (Array.IndexOf(StationKinds, s) < 0) errors.Add(p + "unknown station '" + s + "'.");
                    if (items.Get(r.output) == null) errors.Add(p + "unknown output '" + r.output + "'.");
                    if (r.count < 1) errors.Add(p + "makes " + r.count + ".");
                    if (r.inputs == null || r.inputs.Length == 0) errors.Add(p + "no inputs.");
                    foreach (var i in r.inputs ?? new RecipeInput[0])
                    {
                        if (i == null || items.Get(i.item) == null) errors.Add(p + "unknown input '" + (i != null ? i.item : null) + "'.");
                        else if (i.count < 1) errors.Add(p + "takes " + i.count + " of '" + i.item + "'.");
                    }
                    db.recipes[r.id] = r; db.Recipes.Add(r);
                }
            }
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            return db;
        }
        /// <summary>The station kinds in a station field ("forge|fire" is either); none for an empty field.</summary>
        public static string[] Stations(string station) { return string.IsNullOrEmpty(station) ? new string[0] : station.Split('|'); }
        public ProfessionDef Profession(string id) { return id != null && professions.TryGetValue(id, out var d) ? d : null; }
        public NodeDef Node(string id) { return id != null && nodes.TryGetValue(id, out var n) ? n : null; }
        public RecipeDef Recipe(string id) { return id != null && recipes.TryGetValue(id, out var r) ? r : null; }
        /// <summary>A trade's node kinds, easiest first.</summary>
        public List<NodeDef> NodesFor(string profession)
        {
            var list = Nodes.FindAll(n => n.profession == profession); list.Sort((a, b) => a.skill != b.skill ? a.skill.CompareTo(b.skill) : string.CompareOrdinal(a.id, b.id));
            return list;
        }
        /// <summary>A trade's recipes, easiest first.</summary>
        public List<RecipeDef> RecipesFor(string profession)
        {
            var list = Recipes.FindAll(r => r.profession == profession); list.Sort((a, b) => a.skill != b.skill ? a.skill.CompareTo(b.skill) : string.CompareOrdinal(a.id, b.id));
            return list;
        }
        /// <summary>A trade every character has from the start: Cooking, and a gathering skill that needs no tool (Herbalism).</summary>
        public static bool FromTheStart(ProfessionDef d) { return d != null && (d.kind == "free" || (d.kind == "gather" && string.IsNullOrEmpty(d.tool))); }
    }

    /// <summary>
    /// What a character knows of the trades: which skills are learned and how far each has come (EncounterProgress.professions,
    /// save format 8). A gathering tool (a pick, a hatchet) is used once from the bags: it teaches its skill at 1 and hangs at the
    /// belt from then on, taking no bag slot; a second one is refused and kept. Gathering: whether a node can be worked, how long
    /// it takes, what it yields and the skill roll (the session runs the work bar and rests the node). Pure logic over the
    /// progress, like QuestLog and DiscoveryLog, so it is tested without a scene.
    /// </summary>
    public sealed class ProfessionLog
    {
        public const string AlreadyCarriedLine = "You already carry one.";
        public const string NotAToolLine = "That can't be used.";
        public EncounterProgress Progress { get; private set; }
        public readonly ProfessionDatabase Db;
        /// <summary>Items, for tools used from a bag slot.</summary>
        public readonly ItemDatabase Items;
        /// <summary>Chat lines.</summary>
        public Action<string> Say = t => { };
        /// <summary>A skill just rose: its trade and the new skill (the session prints "Mining 12." and raises the tier toasts).</summary>
        public Action<string, int> SkillUp = (id, skill) => { };

        public ProfessionLog(ProfessionDatabase db, ItemDatabase items, EncounterProgress progress)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db)); Items = items; Bind(progress);
        }
        /// <summary>
        /// Takes over a progress (a new character, or one just loaded). The trades everyone has from the start are added at 1
        /// when missing, and a known trade's skill is brought inside 1-100. Entries for trades this content does not have are
        /// kept as they are and ignored, so content can change without losing them.
        /// </summary>
        public void Bind(EncounterProgress progress)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (Progress.professions == null) Progress.professions = new List<ProfessionSkill>();
            Progress.professions.RemoveAll(s => s == null || string.IsNullOrEmpty(s.id));
            foreach (var s in Progress.professions) if (Db.Profession(s.id) != null) s.skill = Math.Max(1, Math.Min(ProfessionDatabase.MaxSkill, s.skill));
            foreach (var d in Db.Order) if (ProfessionDatabase.FromTheStart(d) && Entry(d.id) == null) Progress.professions.Add(new ProfessionSkill { id = d.id, skill = 1 });
        }
        ProfessionSkill Entry(string id) { return Progress.professions.Find(s => s.id == id); }

        // ---------- queries ----------
        /// <summary>Whether the character has this trade (a known trade with a saved entry).</summary>
        public bool Has(string id) { return Db.Profession(id) != null && Entry(id) != null; }
        /// <summary>The skill in a trade, 1-100, or 0 when the character does not have it.</summary>
        public int Skill(string id) { return Has(id) ? Entry(id).skill : 0; }
        /// <summary>How many crafts the character holds, of Db.CraftSlots.</summary>
        public int CraftSlotsUsed { get { int n = 0; foreach (var d in Db.Order) if (d.kind == "craft" && Has(d.id)) n++; return n; } }

        // ---------- tools ----------
        /// <summary>
        /// Learns the skill a tool teaches, at 1, and says its line. False, with why, when the item is not a tool of a known
        /// trade or its skill is already learned ("You already carry one."). The caller removes the item on success.
        /// </summary>
        public bool UseTool(ItemDef tool, out string why)
        {
            why = null;
            var d = tool != null && tool.kind == "tool" ? Db.Profession(tool.teaches) : null;
            if (d == null) { why = NotAToolLine; return false; }
            if (Has(d.id)) { why = AlreadyCarriedLine; return false; }
            Progress.professions.Add(new ProfessionSkill { id = d.id, skill = 1 });
            Say(string.IsNullOrEmpty(d.taught) ? "You hang it at your belt. " + d.name + " is yours to learn." : d.taught);
            return true;
        }
        /// <summary>
        /// Uses the tool in a bag slot: on success one is taken from that slot (it hangs at the belt and needs no slot from then
        /// on). A refused tool stays where it is.
        /// </summary>
        public bool UseTool(int bagIndex, out string why)
        {
            why = NotAToolLine;
            if (Items == null || Progress.bag == null || bagIndex < 0 || bagIndex >= Progress.bag.Count || Progress.bag[bagIndex].Empty) return false;
            var s = Progress.bag[bagIndex];
            if (!UseTool(Items.Get(s.item), out why)) return false;
            s.count--; if (s.count <= 0) { s.item = ""; s.count = 0; }
            return true;
        }

        // ---------- gathering ----------
        public const string BagsFullLine = "Your bags are full.";
        /// <summary>
        /// Whether the character can work a node: they need its trade (a gathering skill with a tool is learned by hanging the tool
        /// at the belt; why then names it: "You need a miner's pick. Merchants sell them."). Skill never refuses a node: under the
        /// node's skill the work is only hard going (<paramref name="hard"/>): slower, a yield of one, a skill point every time.
        /// </summary>
        public bool CanGather(NodeDef n, out bool hard, out string why)
        {
            hard = false; why = null;
            var trade = n == null ? null : Db.Profession(n.profession);
            if (trade == null) { why = "You can't work that."; return false; }
            if (!Has(trade.id))
            {
                var tool = Items?.Get(trade.tool);
                why = tool != null ? "You need a " + char.ToLowerInvariant(tool.name[0]) + tool.name.Substring(1) + ". Merchants sell them." : "You don't know how to work that.";
                return false;
            }
            hard = Skill(trade.id) < n.skill;
            return true;
        }
        /// <summary>How long working a node takes: its seconds, twice that when it is hard going.</summary>
        public float WorkSeconds(NodeDef n, bool hard) { return hard ? n.seconds * 2 : n.seconds; }
        /// <summary>What working a node yields (the yield only; no skill roll): exactly one when the skill is under the node's; else
        /// min-max, and a one-in-four chance of one more from 20 points over it.</summary>
        public (string item, int count) RollGather(NodeDef n, System.Random rng)
        {
            int skill = Skill(n.profession);
            if (skill < n.skill) return (n.item, 1);
            int count = n.min + rng.Next(Math.Max(1, n.max - n.min + 1));
            if (skill >= n.skill + 20 && rng.NextDouble() < .25) count++;
            return (n.item, count);
        }
        /// <summary>The chance that working a node of skill <paramref name="nodeSkill"/> raises a skill: certain under 20 points over
        /// it, even under 40, never from 40 over.</summary>
        public static float GatherUpChance(int skill, int nodeSkill) { return skill < nodeSkill + 20 ? 1 : skill < nodeSkill + 40 ? .5f : 0; }
        /// <summary>The skill roll after working a node (see <see cref="GatherUpChance"/>); a skill never passes 100. True when it rose
        /// (SkillUp is told).</summary>
        public bool Gathered(NodeDef n, System.Random rng)
        {
            var e = n == null || Db.Profession(n.profession) == null ? null : Entry(n.profession);
            if (e == null || e.skill >= ProfessionDatabase.MaxSkill) return false;
            float chance = GatherUpChance(e.skill, n.skill);
            if (chance <= 0 || (chance < 1 && rng.NextDouble() >= chance)) return false;
            e.skill++; SkillUp(n.profession, e.skill);
            return true;
        }
        /// <summary>
        /// A node worked to the end: the yield goes into the bags and, when any of it went in, the skill is rolled. Returns how many
        /// went in; 0 with why when the node can't be worked or nothing fits ("Your bags are full."), and then nothing changes.
        /// </summary>
        public int Gather(NodeDef n, System.Random rng, out string why)
        {
            if (!CanGather(n, out _, out why)) return 0;
            if (Items == null || Inventory.Room(Progress, Items, n.item) == 0) { why = BagsFullLine; return 0; }
            var (item, count) = RollGather(n, rng);
            int got = count - Inventory.Add(Progress, Items, item, count);
            if (got > 0) Gathered(n, rng);
            return got;
        }
    }
}
