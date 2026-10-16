using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The capital's halls in play (CAPITAL_HUB.md step 4; CapitalHalls.cs holds the rules): speaking (E) to a hall person who has no
    /// quest business with you opens their window (<see cref="Hall"/>, drawn by EncounterHud.Halls):
    /// - High Councilor Aethelgard: his audience, a line for each topic as he judges you (CouncilAudience); "Has the Council work for
    ///   me?" opens his clerk's errands; he is never a target and nothing in the game can strike him (HallRoles.Untouchable);
    /// - the Council's clerk: the errands, as any quest giver's (QuestTalk); one handed in today is off the list until tomorrow, one
    ///   refused costs the Council's standing (DeclineQuest);
    /// - the archivist: the Archive's reading room (ArchiveRoom) and the copying trade; /archive opens the room anywhere (a tester's);
    /// - a guild's master: the guild's window (its motto, its roster, its notice board of asks; Join for one in no guild);
    /// - a trainer: the paid talent reset and the class's abilities by level;
    /// - the dresser: the quest book's Armoury tab (the looks seen);
    /// - a trade's trainer: that trade taken up, put aside, or its tool bought (CanTakeUp counts the trainer as the trade's own person).
    /// The window shuts when you walk away (as a conversation does), on Esc, or with its Close.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>An open hall window: whose, what kind, and its state.</summary>
        public sealed class HallWindow
        {
            public string kind, arg, npc; public Vector3 where; public bool anywhere;
            /// <summary>The audience: the topic last asked and his answer.</summary>
            public string topic; public CouncilAudience.Line line;
            /// <summary>The Archive: the shelf (zone id) chosen and the page being read; the archivist's last word.</summary>
            public string shelf, reading, said;
            /// <summary>Forget, asked once: the second press puts the trade aside.</summary>
            public bool confirm;
        }
        /// <summary>The hall window open (null: none).</summary>
        public HallWindow Hall;
        public const float HallReach = 6;

        /// <summary>A hall person spoken to with no quest business: their window opens. False for anyone else.</summary>
        public bool HallTalk(Villager v)
        {
            if (v == null || !HallRoles.Parse(v.Role, out var kind, out var arg)) return false;
            var w = new HallWindow { kind = kind, arg = arg, npc = v.Name, where = v.transform.position };
            switch (kind)
            {
                case HallRoles.Councilor:
                    w.topic = CouncilAudience.Greeting; w.line = CouncilAudience.Say(w.topic, AudienceContext());
                    v.Say(w.line.text, 7); Message(v.Name + ": " + w.line.text);
                    break;
                case HallRoles.CouncilClerk:
                    { string line = Quests == null ? "\"The Council has nothing for you.\"" : "\"The Council's errands for today are done. Come back tomorrow.\""; v.Say(line, 6); Message(v.Name + ": " + line); }
                    return true;
                case HallRoles.Archivist:
                    w.shelf = ZoneId; w.said = "\"Mind the candles. Every page here is the only one of its kind, or was.\"";
                    break;
                case HallRoles.Guildmaster:
                    if (GuildHalls.FromSlug(arg) == null) return false;
                    break;
                case HallRoles.Trainer:
                    if (string.IsNullOrEmpty(arg)) w.arg = ClassDef != null ? ClassDef.id : "";
                    break;
                case HallRoles.Dresser:
                    {
                        string line = "\"Everything you have ever worn is in the book. Choose a look and I will see it right.\"";
                        v.Say(line, 6); Message(v.Name + ": " + line);
                        Conversation = null; Hall = null; EncounterHud.BookTab = "armoury"; ReadingDocument = null; QuestBookOpen = true;
                    }
                    return true;
                case HallRoles.ProfessionTrainer:
                    if (Professions == null || Professions.Db.Profession(arg) == null) return false;
                    break;
            }
            Conversation = null; Hall = w;
            return true;
        }
        /// <summary>What a hall person answers a hail with (null: not a hall person).</summary>
        string HallHail(Villager v)
        {
            if (v == null || !HallRoles.Parse(v.Role, out var kind, out var arg)) return null;
            switch (kind)
            {
                case HallRoles.Councilor: return Pick(new[] { "You may approach.", "Yes?", "The Council hears you. Briefly." });
                case HallRoles.CouncilClerk: return Pick(new[] { "Errands for the Council, this side of the desk.", "Name and business. Quickly." });
                case HallRoles.Archivist: return Pick(new[] { "Hush. Yes, hello.", "Well met. Wipe your boots." });
                case HallRoles.Guildmaster: { var g = GuildHalls.FromSlug(arg); return g == Guild && g != null ? "Welcome home." : g != null ? "Hail. " + g + " is always looking." : "Hail."; }
                case HallRoles.Trainer: return Pick(new[] { "Hail. Your stance is wrong, but we can mend that.", "Well met. Training, or talk?" });
                case HallRoles.Dresser: return Pick(new[] { "Hail! Oh, those boots. Come here.", "Good day. Something less muddy, perhaps?" });
                case HallRoles.ProfessionTrainer: return Pick(new[] { "Hail. Here to learn the trade?", "Well met. Mind the tools." });
                default: return null;
            }
        }

        // ---------- the audience ----------
        public CouncilAudience.Context AudienceContext() { return CouncilAudience.Of(Progress, Quests, ZoneId); }
        /// <summary>A topic put to Aethelgard: his answer in the window and in chat. "Has the Council work for me?" opens his clerk's
        /// errands when there are any; "Take your leave" shuts the window.</summary>
        public void AudienceAsk(string topic)
        {
            var w = Hall; if (w == null || w.kind != HallRoles.Councilor) return;
            w.topic = topic; w.line = CouncilAudience.Say(topic, AudienceContext());
            Message(w.npc + ": " + w.line.text); VillageLife.Active?.Find(w.npc)?.Say(w.line.text, 7);
            if (topic == CouncilAudience.Farewell) { Hall = null; return; }
            if (topic == CouncilAudience.Errands && QuestTalk(CouncilAudience.Clerk, w.where)) Hall = null;
        }

        // ---------- the Archive ----------
        /// <summary>The zones the Archive shelves, this one first, then by level (as the book of lore's).</summary>
        public List<ZoneDefinition> ArchiveZones()
        {
            var zones = new List<ZoneDefinition>(); if (Zone == null) return zones;
            zones.Add(Zone.Zone);
            var others = Zone.AllZones().FindAll(z => z != null && z.id != Zone.Zone.id);
            others.Sort((a, b) => a.levelMin != b.levelMin ? a.levelMin.CompareTo(b.levelMin) : string.CompareOrdinal(a.id, b.id));
            zones.AddRange(others);
            return zones;
        }
        public List<ArchiveRoom.Shelf> ArchiveShelves() { return Quests == null ? new List<ArchiveRoom.Shelf>() : ArchiveRoom.Shelves(Quests.Db, ArchiveZones(), Progress, Discoveries); }
        /// <summary>A found page lent to the archivist to copy: a word of a find not yet found, said in the window and in chat.</summary>
        public bool ArchiveCopy(string doc)
        {
            var w = Hall; if (w == null || w.kind != HallRoles.Archivist) return false;
            var word = ArchiveRoom.Copy(Progress, doc, ArchiveZones(), Discoveries, out var why);
            string who = w.anywhere ? "The archivist" : w.npc;
            if (word == null) { w.said = why; Message(who + ": " + why); return false; }
            w.said = "\"A fair trade. Here is something for it.\" " + word;
            Message(who + " copies your page and tells you: " + word);
            Save(false);
            return true;
        }
        /// <summary>/archive: the reading room opens where you stand (a tester's; in play it is the archivist's).</summary>
        void TesterArchive()
        {
            if (Quests == null) { Message("No pages in this build."); return; }
            if (Hall != null && Hall.kind == HallRoles.Archivist) { Hall = null; return; }
            Conversation = null;
            Hall = new HallWindow { kind = HallRoles.Archivist, npc = "The Archive", where = Player != null ? Player.transform.position : Vector3.zero, anywhere = true, shelf = ZoneId,
                said = "Testing: the Archive's reading room, opened by /archive. In play the archivist in the Crown opens it." };
        }

        // ---------- guild halls ----------
        /// <summary>Join the guild of the hall you stand in (the master's invitation; one in no guild only).</summary>
        public bool JoinHallGuild(string guild)
        {
            if (!SimGuilds.Exists(guild)) return false;
            if (InGuild && Guild != guild) { Message("You are of <" + Guild + "> already. Leave it first (/gquit)."); return false; }
            if (Guild == guild) return false;
            JoinGuild(guild);
            return Guild == guild;
        }
        /// <summary>The guild's notice board of asks (members only): the conversation window, as any giver's.</summary>
        public bool OpenGuildBoard(string guild)
        {
            if (Guild != guild) { Message("The board is for members of <" + guild + ">."); return false; }
            var at = Hall != null ? Hall.where : Player.transform.position;
            if (!QuestTalk(GuildHalls.BoardName(guild), at)) { Message(guild + "'s board: nothing pinned up for you today."); return false; }
            Hall = null; return true;
        }
        readonly Dictionary<string, List<Vector3>> hallSpots = new Dictionary<string, List<Vector3>>();
        ZoneBuilder hallSpotsZone;
        /// <summary>
        /// Where a guild's members idle in its hall (SimPopulation.Spot): the zone's "hall.&lt;slug&gt;" place where the village has one, else
        /// its props of that name, else a ring round the guild master's post. Empty in a zone with no hall of that guild.
        /// </summary>
        public List<Vector3> HallSpots(string guild)
        {
            if (hallSpotsZone != Zone) { hallSpots.Clear(); hallSpotsZone = Zone; }
            if (string.IsNullOrEmpty(guild) || Zone == null) return new List<Vector3>();
            if (hallSpots.TryGetValue(guild, out var have)) return have;
            var list = new List<Vector3>(); string place = GuildHalls.HallPlace(guild);
            void Add(Vector3 p) { if (NavMesh.SamplePosition(p, out var hit, 3, NavMesh.AllAreas)) list.Add(hit.position); }
            var life = VillageLife.Active;
            if (life != null && life.Places.TryGetValue(place, out var placed)) list.AddRange(placed);
            if (list.Count == 0 && Zone.Zone.props != null)
                foreach (var p in Zone.Zone.props) if (p != null && p.name == place) Add(Zone.Ground(p.at));
            if (list.Count == 0 && Zone.Zone.life != null && Zone.Zone.life.residents != null)
                foreach (var r in Zone.Zone.life.residents)
                {
                    if (r == null || r.at == Vector2.zero || !HallRoles.Parse(r.role, out var kind, out var arg) || kind != HallRoles.Guildmaster || GuildHalls.FromSlug(arg) != guild) continue;
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3; Add(Zone.Ground(r.at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.5f)); }
                }
            hallSpots[guild] = list;
            return list;
        }

        // ---------- trainers ----------
        /// <summary>The trainer's class: the window's (a trainer of a class) or yours (a bare "trainer").</summary>
        public string TrainerClass { get { return Hall != null && Hall.kind == HallRoles.Trainer && !string.IsNullOrEmpty(Hall.arg) ? Hall.arg : ClassDef != null ? ClassDef.id : null; } }
        public List<(int level, string name, string talent)> TrainerAbilities(string classId)
        {
            var c = content != null ? content.FindClass(classId) : null;
            return TrainerRules.Abilities(c != null ? c.definition : null, content != null ? content.abilities : null);
        }
        /// <summary>A talent reset bought at a trainer of your class: every point back, the price taken (TrainerRules.PaidReset). Not in a fight.</summary>
        public bool PaidTalentReset()
        {
            if (!CanEditBuild) { Message("Not now: you can't change your talents in a fight."); return false; }
            if (ClassDef != null && TrainerClass != ClassDef.id) { Message("\"I train " + SimRoster.ClassName(TrainerClass) + "s. Find your own.\""); return false; }
            if (!TrainerRules.PaidReset(Progress, out int price, out var why)) { Message(why); return false; }
            Kit.ClearTransient(); Kit.ApplyStats(); Save(false);
            Message("Talent points refunded for " + price + " crowns. Spend them again with B.");
            return true;
        }

        // ---------- trade trainers ----------
        /// <summary>A castle trainer of this trade awake within TrainerRange (a resident "profession-trainer:&lt;id&gt;"), or null.</summary>
        public Villager HallTradeTrainerNear(string trade)
        {
            if (string.IsNullOrEmpty(trade) || Player == null || VillageLife.Active == null) return null;
            Villager best = null; float bestD = TrainerRange;
            foreach (var v in VillageLife.Active.Villagers)
            {
                if (v == null || !v.Visible || !HallRoles.Parse(v.Role, out var kind, out var arg) || kind != HallRoles.ProfessionTrainer || arg != trade) continue;
                float d = GroundDistance(v.transform.position); if (d <= bestD) { best = v; bestD = d; }
            }
            return best;
        }
        /// <summary>A gathering trade's tool bought from its trainer (its merchant's price): the trade comes with it.</summary>
        public bool BuyTradeTool(string trade)
        {
            var d = Professions != null ? Professions.Db.Profession(trade) : null; if (d == null || string.IsNullOrEmpty(d.tool) || Items == null) return false;
            if (Inventory.Owns(Progress, d.tool) || Inventory.Has(Progress, d.tool)) { Message("You have a " + ItemName(d.tool) + " already."); return false; }
            if (!Inventory.Buy(Progress, Items, d.tool, out var why)) { Message(why); return false; }
            Message("You buy a " + ItemName(d.tool) + ". " + d.name + " is yours to work.");
            Save(false);
            return true;
        }
        /// <summary>A trade's page in the Trades book (K), from its trainer.</summary>
        public void OpenTradePage(string trade)
        {
            EncounterHud.TradesPage = trade; EncounterHud.TradesRecipe = null; EncounterHud.TradesRecipes = true;
            Hall = null; ShowTrades(true);
        }

        // ---------- the Council's errands and the guilds' asks ----------
        /// <summary>An errand of the Council or a guild's ask: a quest of the halls, once a day, not a notice-board bounty.</summary>
        public static bool HallQuest(QuestDef q) { return CouncilErrands.IsErrand(q) || GuildHalls.IsAsk(q); }
        /// <summary>Declined in the conversation: an errand of the Council costs its standing (once a day each).</summary>
        public void DeclineQuest(QuestDef q)
        {
            if (Quests != null && CouncilErrands.Declined(Quests, q)) Save(false);
        }
        /// <summary>Each frame with a hall window open: it shuts when you walk away from whoever opened it.</summary>
        public void TendHall()
        {
            if (Hall == null || Hall.anywhere || Player == null) return;
            if (Vector3.Distance(Player.transform.position, Hall.where) > HallReach) Hall = null;
        }
    }
}
