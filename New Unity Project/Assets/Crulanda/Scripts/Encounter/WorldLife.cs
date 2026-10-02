using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Ambient life for a generated zone: named villagers with homes, roles and daily activities, and critters that
    /// never fight. Places come from the zone itself (fields, wells, the green, inside the inn, the mill, houses), so
    /// any zone gets village life from a count in its JSON (<see cref="ZoneLife"/>). All names and lines are GAME-ONLY.
    /// </summary>
    public sealed class VillageLife : MonoBehaviour
    {
        public static VillageLife Active { get; private set; }
        public EncounterSession Session { get; private set; }
        public ZoneBuilder Zone { get; private set; }
        public readonly List<Villager> Villagers = new List<Villager>();
        public readonly List<Critter> Critters = new List<Critter>();
        public readonly List<ZoneDoor> Homes = new List<ZoneDoor>();
        public readonly Dictionary<string, List<Vector3>> Places = new Dictionary<string, List<Vector3>>();
        System.Random rng;
        public float R01 { get { return (float)rng.NextDouble(); } }
        public int Next(int max) { return rng.Next(max); }

        static readonly string[] Names = { "Brannoc Vell", "Wil Carder", "Ama Rusk", "Hedda Thorne", "Sel Harrow", "Garet Moss", "Old Tobin", "Pim",
            "Osk Farrow", "Lisbet Crane", "Ilse Brandt", "Corwin Ashby", "Aldo Crisp", "Maud Tanner", "Fen Walker", "Edda Pell", "Nettie", "Tamsin Reed",
            "Jory", "Grete Lowe", "Hob Linden", "Wenna Scrope" };
        // The trades come first so a small village still gets its smith and baker; a trade whose workplace the zone
        // lacks falls back to farming or gossip (see Init).
        static readonly string[] Roles = { "blacksmith", "farmer", "merchant", "baker", "gossip", "hunter", "drinker", "child", "lumberjack", "herbalist",
            "farmer", "elder", "miller", "leatherworker", "skinner", "gossip", "child", "merchant", "drinker", "farmer", "gossip", "child" };
        /// <summary>Place a trade needs; without one in the zone the villager takes a plainer role.</summary>
        static string Needs(string role)
        {
            switch (role)
            {
                case "blacksmith": return "forge"; case "merchant": return "stall"; case "baker": return "oven";
                case "leatherworker": return "leathershop"; case "skinner": return "tannery"; case "lumberjack": case "hunter": return "woods";
                case "herbalist": return "meadow"; case "miller": return "mill"; case "farmer": return "field"; case "innkeeper": return "inn"; default: return null;
            }
        }
        /// <summary>WoW-style subtitle under a villager's name, e.g. &lt;Blacksmith&gt;. Null for folk without a trade.</summary>
        public static string TitleFor(string role)
        {
            switch (role)
            {
                case "blacksmith": return "Blacksmith"; case "merchant": return "Merchant"; case "baker": return "Baker"; case "henwife": return "Hen-wife";
                case "farmer": return "Farmer"; case "hunter": return "Hunter"; case "leatherworker": return "Leatherworker"; case "skinner": return "Skinner";
                case "lumberjack": return "Woodcutter"; case "herbalist": return "Herbalist"; case "miller": return "Miller"; case "elder": return "Village Elder";
                case "innkeeper": return "Innkeeper";
                default: return null;
            }
        }
        readonly Dictionary<Vector3, Vector3> faceAt = new Dictionary<Vector3, Vector3>();
        readonly Dictionary<Vector3, string> placeName = new Dictionary<Vector3, string>();
        /// <summary>Where someone working at a place should look (the anvil, the counter's customers), if it is a workplace. Of
        /// stand points close together (the kitchen's range and table), the nearest one's.</summary>
        public Vector3? LookFor(Vector3 at)
        {
            Vector3? best = null; float bd = 2.5f;
            foreach (var kv in faceAt) { float d = (kv.Key - at).sqrMagnitude; if (d < bd) { bd = d; best = kv.Value; } }
            return best;
        }
        /// <summary>The name of the workplace (the prop's name: "Tanner's leather shop") that the nearest stand point belongs to, or null off any.</summary>
        public string WorkplaceAt(Vector3 at)
        {
            string best = null; float bd = 2.5f;
            foreach (var kv in placeName) { float d = (kv.Key - at).sqrMagnitude; if (d < bd) { bd = d; best = kv.Value; } }
            return best;
        }
        // Whose workshop a prop is (life.workshops): its owner's props by name, and each prop's owner.
        readonly Dictionary<string, List<string>> workshopsOf = new Dictionary<string, List<string>>();
        readonly Dictionary<string, string> ownerOf = new Dictionary<string, string>();
        /// <summary>The props this villager works at by the zone's <c>life.workshops</c> ("Produce stall"), empty for none.</summary>
        public List<string> WorkshopsOf(string name) { return name != null && workshopsOf.TryGetValue(name, out var l) ? l : new List<string>(); }
        /// <summary>A villager's own places of a kind, or null when they have none: the stand points of their own workshops (the baker's
        /// stall, Hob Linden's kitchen), and for a farmer the fields within 60 m of home.</summary>
        public List<Vector3> OwnPlaces(Villager v, string kind)
        {
            if (v == null || !Places.TryGetValue(kind, out var all) || all.Count == 0) return null;
            if (workshopsOf.TryGetValue(v.Name, out var props))
            {
                var mine = all.FindAll(p => placeName.TryGetValue(p, out var n) && props.Contains(n));
                if (mine.Count > 0) return mine;
            }
            if (kind == "field" && v.Role == "farmer" && v.Home != null)
            {
                var near = all.FindAll(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(v.Home.position.x, v.Home.position.z)) < 60);
                if (near.Count > 0) return near;
            }
            return null;
        }
        /// <summary>Where a villager works at a kind of place: their own (see <see cref="OwnPlaces"/>), else any of the kind.</summary>
        public List<Vector3> PlaceFor(Villager v, string kind) { return OwnPlaces(v, kind) ?? (Places.TryGetValue(kind, out var all) ? all : new List<Vector3>()); }
        /// <summary>Goods handed over at the end of an errand: who carried them, the errand, and who answered (null: nobody did).</summary>
        public event System.Action<Villager, Errand, Villager> HandedOver;
        internal void HandOver(Villager carrier, Errand e, Villager taker) { HandedOver?.Invoke(carrier, e, taker); }

        public void Init(EncounterSession session)
        {
            Session = session; Zone = session.Zone; Active = this;
            var z = Zone.Zone; rng = new System.Random(z.seed + 7);
            FindPlaces(); MorningCask(); NameHouseholds();
            // Without households in the zone's data, villager i takes house i while houses last and the rest lodge at the inn.
            bool named = Households.Count > 0; var lodging = Zone.Doors.Find(d => d.kind == "rooms");
            int count = z.life.villagers;
            for (int i = 0; i < count; i++)
            {
                string role = Roles[i % Roles.Length], need = Needs(role);
                // Out of work: a trade whose workplace this village lacks. Where there is an inn they drink; elsewhere they gossip.
                if (need != null && Places[need].Count == 0) role = Places["inn"].Count > 0 ? "drinker" : "gossip";
                var start = RandomPlace(role == "drinker" ? "inn" : "green") ?? Zone.Ground(z.spawns.recovery);
                var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : Names;
                string name = names[i % names.Length];
                var household = named ? HouseholdOf(name) : i < Homes.Count ? Derived(Homes[i]) : null;
                Settle(Villager.Spawn(this, name, role, i, household?.house ?? lodging, start), household);
            }
            // Residents: named people with a fixed place (quest givers), e.g. a hooded stranger who never leaves the inn. One who
            // works (an innkeeper) has the role's day and a home instead. A post's keeper may be named in a household (so a knock at
            // the house names them) and still keeps the post day and night.
            if (z.life.residents != null)
                for (int i = 0; i < z.life.residents.Length; i++)
                {
                    var r = z.life.residents[i]; if (r == null || string.IsNullOrEmpty(r.name)) continue;
                    var household = HouseholdOf(r.name);
                    if (r.works)
                    {
                        var at = RandomPlace(r.place ?? "green") ?? Zone.Ground(z.spawns.recovery);
                        Settle(Villager.Spawn(this, r.name, r.role ?? "stranger", 60 + i, household?.house ?? lodging, at, null, null, r.title, r.look), household);
                        continue;
                    }
                    string place = r.place ?? "green";
                    if (r.at != Vector2.zero) { place = "post:" + r.name; Places[place] = new List<Vector3>(); AddPlace(place, r.at); }   // a fixed post
                    var start = RandomPlace(place) ?? Zone.Ground(r.at != Vector2.zero ? r.at : z.spawns.recovery);
                    Settle(Villager.Spawn(this, r.name, r.role ?? "stranger", 60 + i, null, start, null, place, r.title, r.look), household);
                }
            // One hen-wife per coop, living in her household's house (without households, the house nearest her coop). The coop
            // starts open or shut for the hour.
            for (int i = 0; i < Zone.Coops.Count; i++)
            {
                var coop = Zone.Coops[i]; string name = KeeperNames[i % KeeperNames.Length];
                coop.SetOpen(WorldClock.Between(6, 20.4f));
                Household household;
                if (named) household = HouseholdOf(name);
                else
                {
                    ZoneDoor near = null; float best = float.MaxValue;
                    foreach (var h in Homes) { float d = (h.position - coop.door).sqrMagnitude; if (d < best) { best = d; near = h; } }
                    household = near != null ? Households.Find(h => h.house == near) ?? Derived(near) : null;
                }
                var start = NavMesh.SamplePosition(coop.yard, out var hit, 4, NavMesh.AllAreas) ? hit.position : coop.yard;
                Settle(Villager.Spawn(this, name, "henwife", 40 + i, household?.house ?? lodging, start, coop), household);
            }
            foreach (var group in z.life.critters)
                for (int i = 0; i < group.count; i++)
                    Critters.Add(Critter.Spawn(this, group.kind, group.center, group.radius));
            StartEconomy();
        }
        static readonly string[] KeeperNames = { "Goody Marl", "Hettie Brook", "Nan Pennock", "Old Sorrel" };
        public static string[] DefaultNames { get { return Names; } }
        /// <summary>The trades of the default villagers, by index (before a trade this village lacks a place for falls back).</summary>
        public static string[] DefaultRoles { get { return Roles; } }
        public static string[] KeeperNamesList { get { return KeeperNames; } }

        // ---------- households: who lives behind which door ----------
        /// <summary>The village's households: from the zone's <c>life.households</c>, or one per house dealt out in order.</summary>
        public readonly List<Household> Households = new List<Household>();
        /// <summary>The household a villager, hen-wife or resident of this name belongs to, or null.</summary>
        public Household HouseholdOf(string name)
        {
            foreach (var h in Households) if (h.def != null && System.Array.Exists(h.def.members, m => m != null && m.name == name)) return h;
            foreach (var h in Households) if (h.members.Exists(v => v.Name == name)) return h;
            return null;
        }
        /// <summary>The household whose house this door is, or null (a door nobody lives behind).</summary>
        public Household HouseholdAt(ZoneDoor door) { return door == null ? null : Households.Find(h => h.house == door); }
        /// <summary>The households the zone names, each with the door of its house: a house or mill's own door, a barn's added home
        /// door, an inn's door to its rooms. A house that is not built leaves its household without a door (they lodge at the inn).</summary>
        void NameHouseholds()
        {
            var defs = Zone.Zone.life.households; if (defs == null) return;
            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.name)) continue;
                var door = string.IsNullOrEmpty(def.house) ? null : Zone.Doors.Find(d => !d.openable && d.kind != "rooms" && d.name == def.house) ?? Zone.Doors.Find(d => d.kind == "rooms" && d.name == def.house + ", upstairs");
                if (door == null) Debug.LogWarning("Household '" + def.name + "': no door for the house '" + def.house + "'; they lodge at the inn.");
                Households.Add(new Household { name = def.name, def = def, house = door });
            }
        }
        /// <summary>A household made for a house when the zone names none (called after the house).</summary>
        Household Derived(ZoneDoor house) { var h = new Household { name = house.name, house = house }; Households.Add(h); return h; }
        void Settle(Villager v, Household household) { Villagers.Add(v); v.Household = household; if (household != null) household.members.Add(v); }
        /// <summary>Who is indoors behind this door now (hidden at home: abed, at dinner, or fled in).</summary>
        public List<Villager> AtHome(ZoneDoor door) { return Villagers.FindAll(v => v.Home == door && v.Indoors); }
        /// <summary>
        /// The player paid this villager this much coin (a purchase, or a bag made for a quest's hides): it goes into their household's
        /// purse. Before the shops shut the household plans again and sets out for what it can now afford ("That's the fire lit
        /// tonight."); after, the coin waits for the morning, and a house that went without firewood today says so once ("That's
        /// tomorrow's fire."). Past the cap it goes to the tithe-man.
        /// </summary>
        public void Paid(string npc, int coin)
        {
            var v = Find(npc); var h = v != null ? v.Household : HouseholdOf(npc); var p = PurseOf(h);
            if (p == null || coin <= 0) return;
            bool late = WorldClock.Hour >= ShopsShut;
            int over = Economy.Earn(p, coin, !late, out var claimed);
            string line = over > 0 ? (p.said.Add("tithe") ? TitheLine : null)
                : late ? (Economy.Live(p, "firewood") && (p.cold || !p.met.Contains("firewood") && !p.claimed.Contains("firewood")) && p.said.Add("tomorrow") ? TomorrowLine : null)
                : claimed != null && claimed.Contains("firewood") ? TonightLine
                : claimed != null && claimed.Contains("bread") ? "That's bread on the board tonight. Bless you." : null;
            if (line == null || v == null) return;
            Session.Message(v.Name + ": " + line);
            if (Time.time >= v.BubbleUntil) v.Say(line, 6);   // a quest's own last words keep the bubble
        }
        public const string TonightLine = "That's the fire lit tonight. Bless you.", TomorrowLine = "That's tomorrow's fire. Bless you.";
        const string TitheLine = "More than we'll spend. The tithe-man will have the rest.";

        // ---------- purses: what each household buys, and who goes without (ADDENDUM C; VillageEconomy) ----------
        /// <summary>The village's purses for this character (see <see cref="VillageEconomy"/>).</summary>
        public VillageEconomy Economy { get; private set; }
        /// <summary>From this hour the coin the player pays waits for the next morning.</summary>
        public const float ShopsShut = 18;
        /// <summary>From this hour a house that went without firewood today lets its fire die, and its chimney stops smoking until the next.</summary>
        public const float ColdFrom = 17;
        /// <summary>A household with nobody to send further than this from the green buys on the books.</summary>
        const float BooksReach = 100;
        /// <summary>A household's purse (null: no household, or no economy).</summary>
        public Purse PurseOf(Household h) { return h != null && Economy != null ? Economy.PurseOf(h.name) : null; }
        void StartEconomy()
        {
            string root = Session.SaveDirectoryOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "CrulandaEncounter");
            Economy = VillageEconomy.For(root + "|" + EncounterSave.SlotFor(Session.ClassDef != null ? Session.ClassDef.id : null), Zone.Zone.id);
            foreach (var h in Households) Economy.Open(h.name, h.def != null ? h.def.stipend : 6, h.def?.needs, h.members.ConvertAll(m => m.Role), BagsFor(h));
            if (Economy.Turned(WorldClock.Hour)) Economy.NewDay(); else Economy.PlanAll();
            // The part of today before the session began went by off-screen: what was claimed for errands already over is bought.
            SettleDue(true);
            // Wood bought before this scene (a bundle still being carried when the player left) is home by now.
            foreach (var p in Economy.Purses) if (p.met.Contains("firewood")) p.cold = false;
        }
        /// <summary>Trade bags the player wears, counted for the leatherworker's household (0 for every other).</summary>
        int BagsFor(Household h)
        {
            if (h == null || !h.members.Exists(m => m.Role == "leatherworker") || Session.Progress == null || Session.Items == null) return 0;
            return Inventory.Pouches(Session.Progress, Session.Items).Count;
        }
        /// <summary>The purses' part of the village's tick: a new day's stipends and plans, what is bought on the books, and the hearths.</summary>
        void Purses()
        {
            if (Economy == null) return;
            if (Economy.Turned(WorldClock.Hour)) { foreach (var h in Households) Economy.SetBags(h.name, BagsFor(h)); Economy.NewDay(); }
            SettleDue(false);
            float hour = WorldClock.Hour, woodUntil = VillageWork.ShopFor("firewood").until;
            foreach (var h in Households)
            {
                var p = PurseOf(h); if (p == null) continue;
                bool wood = Economy.Live(p, "firewood");
                if (!wood) p.cold = false;
                else if (hour >= ColdFrom && !p.met.Contains("firewood") && (!p.claimed.Contains("firewood") || hour >= woodUntil)) p.cold = true;
                h.ApplyHearth(p.cold);
            }
        }
        /// <summary>Buy what is claimed and due: on the books for a household with nobody to send, once the need's window has opened;
        /// with <paramref name="closedToo"/> (a session starting) also what a household's errand window has already closed on.</summary>
        void SettleDue(bool closedToo)
        {
            float hour = WorldClock.Hour;
            foreach (var h in Households)
            {
                var p = PurseOf(h); if (p == null) continue;
                foreach (var e in VillageWork.Shopping)
                    if (p.claimed.Contains(e.need) && hour >= e.at && (OnTheBooks(h, e.need) || closedToo && hour >= e.until) && Economy.Settle(p, e.need) && e.need == "firewood") p.cold = false;
            }
        }
        /// <summary>Who in a household goes for a need: a member other than the head, not a drinker, a hen-wife, a post-keeper or the
        /// innkeeper. Bread goes to a child first, firewood only to a grown-up, eggs to a grown-up first. Null: nobody to send.</summary>
        public Villager Runner(Household h, string need)
        {
            if (h == null) return null;
            Villager head = h.Head, adult = null, child = null;
            foreach (var m in h.members)
            {
                if (m == head || m.Resident || m.Coop != null || m.Role == "drinker" || m.Role == "innkeeper") continue;
                if (m.Role == "child") { if (child == null) child = m; } else if (adult == null) adult = m;
            }
            return need == "bread" ? child ?? adult : need == "firewood" ? adult : adult ?? child;
        }
        /// <summary>A need bought on the books, with no walk: a household of one, one with nobody to send for it, or one living out
        /// past the village (more than <see cref="BooksReach"/> from the green), so the smiths and merchants stay at their shops.</summary>
        public bool OnTheBooks(Household h, string need)
        {
            if (h.members.Count < 2 || Runner(h, need) == null || h.house == null) return true;
            var green = Zone.Zone.clearings.Length > 0 ? Zone.Zone.clearings[0].center : Vector2.zero;
            return Vector2.Distance(new Vector2(h.house.position.x, h.house.position.z), green) > BooksReach;
        }
        /// <summary>The household's shopping this villager should go for now: an errand in its window, the coin claimed, and this
        /// villager the one to send (a child's own midday loaf comes first). One the household is short of is said once instead.</summary>
        public List<Errand> ShoppingFor(Villager v)
        {
            var list = new List<Errand>(); var h = v.Household; var p = PurseOf(h); if (p == null) return list;
            float hour = WorldClock.Hour; var own = VillageWork.DayFor(v.Role);
            foreach (var e in VillageWork.Shopping)
            {
                if (hour < e.at || hour >= e.until || Runner(h, e.need) != v || OnTheBooks(h, e.need)) continue;
                if (own != null && System.Array.Exists(own.errands, x => x.need == e.need && hour < x.until && !v.Done(x.id))) continue;
                if (p.claimed.Contains(e.need)) list.Add(e);
                else SayShort(v, p, e.need);
            }
            return list;
        }
        /// <summary>Whether a villager's own errand that buys a need (the child's loaf) may run: yes while its coin is claimed, and
        /// always with no purse (as before the purses). Short of it, they say so once.</summary>
        public bool MayFetch(Villager v, Errand e)
        {
            var p = PurseOf(v.Household); if (p == null || e.need == null || p.claimed.Contains(e.need)) return true;
            SayShort(v, p, e.need); return false;
        }
        void SayShort(Villager v, Purse p, string need)
        {
            if (p.met.Contains(need) || Economy.ShortOf(p) != need || !p.said.Add(need)) return;
            var line = VillageWork.ShortLine(need, v.Role == "child"); if (line != null) v.Say(line, 4);
        }
        /// <summary>Goods picked up on an errand that buys a need: paid for now. False when the coin is no longer there for it.</summary>
        public bool Fetched(Villager v, Errand e)
        {
            var p = PurseOf(v.Household); return p == null || e.need == null || Economy.Settle(p, e.need);
        }
        /// <summary>An errand that buys a need ended. Handed over at home, or given up once paid at pick-up (the goods go home with
        /// them): firewood lights the hearth. Given up before pick-up: with <paramref name="again"/> the claim is kept and true is
        /// returned, so the runner sets out again while its window is open (a flight, bedtime, a capture); without, the claim is let
        /// go (the place could not be reached).</summary>
        public bool ErrandEnded(Villager v, Errand e, bool carried, bool again = false)
        {
            var h = v.Household; var p = PurseOf(h); if (p == null || e.need == null) return false;
            if (!carried && !p.met.Contains(e.need)) { if (again && p.claimed.Contains(e.need)) return true; Economy.Release(p, e.need); return false; }
            if (e.need == "firewood") { p.cold = false; h.ApplyHearth(false); }
            return false;
        }
        /// <summary>Whoever keeps the shop for a need (the head of the selling household: Hedda Thorne for bread, Ama Rusk for eggs),
        /// or null when the zone has none.</summary>
        public Villager Seller(string need)
        {
            var seller = Economy != null ? Economy.SellerOf(need) : null; var h = seller != null ? Households.Find(x => x.name == seller.household) : null;
            return h != null ? h.Head : null;
        }
        /// <summary>The first name of whoever keeps the shop for a need (the baker for bread), for the shopper's pick-up line.</summary>
        public string SellerName(string need) { var s = Seller(need); return s != null ? FirstName(s.Name) : null; }
        /// <summary>What a household short of firewood says to the player (one talk in three; see <see cref="LineFor"/>), only where the
        /// player can pay them (a member has wares or quest business). Null for a warm house, and before dusk for one that went cold
        /// last night but has its wood bought or on the way today.</summary>
        public string PurseLine(Villager v)
        {
            var h = v.Household; var p = PurseOf(h); if (p == null || !Economy.Live(p, "firewood")) return null;
            bool dusk = WorldClock.Hour >= ColdFrom, without = !p.met.Contains("firewood") && !p.claimed.Contains("firewood");
            bool cold = dusk && p.cold || without && (p.cold || dusk);
            if (!cold || !h.members.Exists(m => Session.IsVendor(m) || Session.Quests != null && Session.Progress != null && Session.Quests.For(m.Name, Session.ZoneId, Session.Progress.Level).Count > 0)) return null;
            if (Economy.Live(p, "bread") && !p.met.Contains("bread") && !p.claimed.Contains("bread")) return "Cold hearth again tonight. Wood's three coppers we haven't got.";
            var child = h.members.Find(m => m.Role == "child");
            if (child == v) return "No fire again. I sleep in my coat.";
            return "We've bread. No fire; " + (child != null ? FirstName(child.Name) + " sleeps in " + Possessive(h, child) + " coat." : "we sleep in our coats.");
        }

        /// <summary>
        /// What a knock at a household's door gets (GAME-ONLY): who is abed, where the head of the house is if they are out, or
        /// that nobody is home. Null for a door no household lives behind (the old barred-door lines answer there).
        /// </summary>
        public string KnockLine(ZoneDoor door)
        {
            var h = HouseholdAt(door); if (h == null || h.members.Count == 0) return null;
            var inside = AtHome(door); var head = h.Head;
            if (inside.Count == 0)
            {
                if (head.Resident) return "No answer. " + HouseName(h) + " stands empty; " + FirstName(head.Name) + " keeps watch elsewhere, day and night.";
                return "No answer. " + HouseName(h) + (WorldClock.Between(5, 18) ? " is empty till supper." : " is dark, and nobody's home.");
            }
            if (inside.Count == h.members.Count && inside.TrueForAll(v => v.Activity == "sleep" || v.Activity == "sleepitoff"))
            {
                var child = h.members.Find(v => v.Role == "child");
                string after = child != null ? " A child coughs, and somebody hushes " + Pronoun(h, child) + "." : h.members.Count > 1 ? " Somebody turns over and snores." : " The bar is down and the fire banked.";
                return Folk(h) + (h.members.Count > 1 ? " are abed." : " is abed.") + after;
            }
            if (!inside.Contains(head))
            {
                var where = Whereabouts(head);
                return "A voice through the planks: \"" + FirstName(head.Name) + "'s " + (where != null ? where + ". Try there.\"" : "out. " + (WorldClock.Between(5, 18) ? "Back by supper, likely.\"" : "Back before long, likely.\""));
            }
            return FirstName(head.Name) + "'s voice, through the planks: \"" + (WorldClock.IsNight ? "Not at this hour." : "Not now.") + " Since the collectors came, this door stays barred.\"";
        }
        /// <summary>Where someone is, as the folk at home would say it ("at the shop by the South road"), or null when it is no one place.</summary>
        string Whereabouts(Villager v)
        {
            if (!v.Visible || v.Errand != null) return null;
            switch (v.Activity)
            {
                case "leathershop": { var road = RoadNear(v.transform.position, 12); return "at the shop" + (road != null ? " by the " + road : ""); }
                case "forge": return "at the forge"; case "stall": return "at the stall"; case "oven": return "at the bakehouse";
                case "tannery": return "at the tannery yard"; case "woodpile": return "at the woodyard"; case "mill": return "at the mill";
                case "dryhut": return "at the drying hut"; case "lodge": return "at the lodge"; case "field": return "out in the fields";
                case "woods": return "in the woods"; case "meadow": return "out on the hill gathering"; case "green": return "on the green";
                case "well": return "at the well"; case "inn": case "bar": case "kitchen": case "kitchendoor": case "passedout": return "at the inn";
                case "yard": case "herd": return "with the hens";
                default: return null;
            }
        }
        /// <summary>The name of the road passing within this distance of a point, or null.</summary>
        string RoadNear(Vector3 at, float within)
        {
            string best = null; float bd = within; var p = new Vector2(at.x, at.z);
            foreach (var r in Zone.Zone.roads)
            {
                if (r == null || string.IsNullOrEmpty(r.name)) continue;
                for (int i = 0; i + 1 < r.points.Length; i++)
                {
                    Vector2 a = r.points[i], ab = r.points[i + 1] - a;
                    float d = Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude))) - r.width / 2;
                    if (d < bd) { bd = d; best = r.name; }
                }
            }
            return best;
        }
        /// <summary>How a house is named at the start of a sentence: "The Tanner house", "Jory's house", "Moss's lodge". A possessive
        /// stays bare only when the owner is the household or one of its members ("The Crypt-Keeper's Hovel" takes the article).</summary>
        static string HouseName(Household h)
        {
            string n = h.def != null && !string.IsNullOrEmpty(h.def.house) ? h.def.house : h.house != null ? h.house.name : h.name;
            int s = n.IndexOf("'s "); string owner = s > 0 ? n.Substring(0, s) : null;
            return n.StartsWith("The ") || owner != null && (owner == h.name || h.members.Exists(v => FirstName(v.Name) == owner)) ? n : "The " + n;
        }
        /// <summary>A household as its neighbours say it: one person by name, a family by its name ("The Tanners"); a farm's folk go by
        /// the head's family name (the Harrow farm's are "The Harrows", the Brook farm's "The Lowes").</summary>
        static string Folk(Household h)
        {
            if (h.members.Count == 1) return FirstName(h.members[0].Name);
            string n = h.name.EndsWith(" farm") ? h.name.Substring(0, h.name.Length - 5) : h.name;
            if (h.name.EndsWith(" farm") && h.Head != null && h.Head.Name.IndexOf(' ') > 0) n = h.Head.Name.Substring(h.Head.Name.LastIndexOf(' ') + 1);
            return n.Contains(" ") ? "Everyone in " + HouseName(h).Replace("The ", "the ") : "The " + n + "s";
        }
        static string Possessive(Household h, Villager v) { var p = Pronoun(h, v); return p == "her" ? "her" : p == "him" ? "his" : "their"; }
        static string Pronoun(Household h, Villager v)
        {
            string kin = h.KinOf(v);
            return kin == "daughter" || kin == "wife" || kin == "aunt" ? "her" : kin == "son" || kin == "husband" || kin == "father" ? "him" : "them";
        }
        /// <summary>What neighbours call someone: the first name ("Maud"), or the whole of a name like "Old Tobin" or "Goody Marl".</summary>
        public static string FirstName(string name)
        {
            int space = name.IndexOf(' ');
            if (space < 0) return name;
            string first = name.Substring(0, space);
            return first == "Old" || first == "Goody" || first == "Warden" || first == "Sister" ? name : first;
        }
        /// <summary>Hens lay through the working day, one egg each at most; the count resets before dawn. The water pan dries out. The
        /// purses keep their day (<see cref="Purses"/>).</summary>
        void Update()
        {
            foreach (var coop in Zone.Coops) coop.Dry(Time.deltaTime / 420);   // a pan lasts about four hours
            if (Time.time < nextLay) return;
            nextLay = Time.time + 20;
            if (WorldClock.Between(4, 5)) { Stock.Clear(); MorningCask(); }   // the stock is the day's deliveries: yesterday's are eaten, sold or burnt
            foreach (var coop in Zone.Coops)
            {
                if (WorldClock.Between(4, 5)) coop.LaidToday = 0;
                if (!WorldClock.Between(7, 16.5f)) continue;
                int hens = 0; foreach (var c in Critters) if (c.Coop == coop) hens++;
                if (coop.LaidToday < hens && R01 < .35f) { coop.Eggs++; coop.LaidToday++; }
            }
            Purses();
        }
        float nextLay;
        public IEnumerable<Critter> HensOf(ZoneCoop coop) { foreach (var c in Critters) if (c.Coop == coop) yield return c; }
        void OnDestroy() { if (Active == this) Active = null; }
        public Villager Find(string name) { return Villagers.Find(v => v.Name == name); }

        void FindPlaces()
        {
            var z = Zone.Zone;
            foreach (var key in new[] { "field", "well", "green", "inn", "mill", "wander", "forge", "stall", "oven", "tannery", "woodpile", "woods", "meadow", "leathershop", "dryhut", "kitchen", "kitchendoor", "bar", "lodge" }) Places[key] = new List<Vector3>();
            // Trade workplaces (exact stand points, with where to look while working, and whose place it is).
            foreach (var w in Zone.Workplaces)
                if (NavMesh.SamplePosition(w.stand, out var wh, 1.5f, NavMesh.AllAreas)) { Places[w.kind].Add(wh.position); faceAt[wh.position] = w.look; placeName[wh.position] = w.name; }
            if (Places["leathershop"].Count == 0) Places["leathershop"] = Places["tannery"];   // a village with a tannery yard but no shop: the leatherworker works in the yard
            foreach (var w in z.life.workshops)
            {
                if (w == null || string.IsNullOrEmpty(w.who) || string.IsNullOrEmpty(w.prop)) continue;
                if (!workshopsOf.TryGetValue(w.who, out var props)) workshopsOf[w.who] = props = new List<string>();
                props.Add(w.prop); ownerOf[w.prop] = w.who;
            }
            // Woods: inside living groves (not orchards). Meadow: open hillside between the village and the forest edge.
            foreach (var g in z.groves)
            {
                if (g.kind == "orchard") continue;
                for (int i = 0; i < 6; i++) AddPlace("woods", g.center + new Vector2((R01 - .5f) * g.size.x * .8f, (R01 - .5f) * g.size.y * .8f));
            }
            for (int i = 0; i < 40 && Places["meadow"].Count < 14; i++)
            {
                float a = R01 * Mathf.PI * 2, r = Mathf.Lerp(z.flatRadius + 6, z.size / 2 - 18, R01);
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (z.wasting != null && p.x > z.wasting.x - 20) continue;
                AddPlace("meadow", p);
            }
            // Houses: every barred door but the inn's rooms (house, mill and a barn's home door), in the order they were built.
            foreach (var d in Zone.Doors) if (!d.openable && d.kind != "rooms") Homes.Add(d);
            foreach (var f in z.fields)
                for (int i = 0; i < 4; i++)
                {
                    var local = new Vector2((R01 - .5f) * f.size.x * .8f, (R01 - .5f) * f.size.y * .8f);
                    float a = f.rotation * Mathf.Deg2Rad;
                    var p = f.center + new Vector2(local.x * Mathf.Cos(a) - local.y * Mathf.Sin(a), local.x * Mathf.Sin(a) + local.y * Mathf.Cos(a));
                    AddPlace("field", p);
                }
            foreach (var prop in z.props)
            {
                if (prop == null) continue;
                if (prop.kind == "well") for (int i = 0; i < 3; i++) { float a = i * 2.1f; AddPlace("well", prop.at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.3f); }
                if (prop.kind == "mill") AddPlace("mill", prop.at + new Vector2(0, -(prop.size.y > 0 ? prop.size.y : 6) / 2 - 2));
            }
            if (z.clearings.Length > 0)
            {
                var green = z.clearings[0];
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; AddPlace("green", green.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Max(3, green.radius - 3)); }
            }
            // Inside the inn: seats around the tables and a spot at the bar (same layout as ZoneBuilder.Inn).
            foreach (var door in Zone.Doors)
            {
                if (!door.openable || door.hinge == null) continue;
                var inn = door.hinge.parent;
                foreach (var table in new[] { new Vector2(2.2f, -1.6f), new Vector2(2.8f, 1.3f), new Vector2(-2.8f, -1.9f) })
                    for (int k = 0; k < 3; k++) { float a = k * 120 * Mathf.Deg2Rad; AddPlace("inn", inn.TransformPoint(new Vector3(table.x + Mathf.Cos(a) * .95f, 0, table.y + Mathf.Sin(a) * .95f)), true); }
            }
            for (int i = 0; i < 24; i++) AddPlace("wander", new Vector2((R01 - .5f) * z.flatRadius * 1.6f, (R01 - .5f) * z.flatRadius * 1.6f));
        }
        /// <summary>How far from the village's middle its people go to work the fields, cut wood or walk the meadows: the farms and
        /// woods past it (a grown zone's outer ring) are the wild's, not a day's errand.</summary>
        const float VillageReach = 125;
        void AddPlace(string key, Vector2 at)
        {
            if ((key == "field" || key == "woods" || key == "meadow") && at.magnitude > VillageReach) return;
            AddPlace(key, Zone.Ground(at), false);
        }
        void AddPlace(string key, Vector3 world, bool exact)
        {
            if (NavMesh.SamplePosition(world, out var hit, exact ? 1.2f : 3f, NavMesh.AllAreas)) Places[key].Add(hit.position);
        }
        /// <summary>A random place of a kind, avoiding spots near living enemies (villagers steer clear of the collectors, though
        /// they will draw water with one across the square: <see cref="KeepClear"/>). With <paramref name="who"/>, one of their own
        /// places of the kind when they have any (<see cref="PlaceFor"/>).</summary>
        public Vector3? RandomPlace(string key, Villager who = null)
        {
            var list = who != null ? PlaceFor(who, key) : Places.TryGetValue(key, out var all) ? all : null;
            if (list == null || list.Count == 0) return null;
            for (int tries = 0; tries < 8; tries++)
            {
                var p = list[rng.Next(list.Count)];
                if (!EnemyNear(p, KeepClear)) return p;
            }
            return null;
        }
        /// <summary>How close to a living enemy nobody will stand to work or chat. (12 m shut the well while a collector stood seven metres off.)</summary>
        public const float KeepClear = 7;
        public bool EnemyNear(Vector3 at, float radius)
        {
            foreach (var e in Session.Enemies) if (e.actor.IsAlive && (e.transform.position - at).sqrMagnitude < radius * radius) return true;
            return false;
        }
        /// <summary>Fighting nearby: an engaged enemy within 26 m, or the player fighting within 18 m.</summary>
        public bool Danger(Vector3 at)
        {
            foreach (var e in Session.Enemies) if (e.Engaged && (e.transform.position - at).sqrMagnitude < 26 * 26) return true;
            return Session.InCombat && (Session.Player.transform.position - at).sqrMagnitude < 18 * 18;
        }
        public bool EnemiesCleared { get { return Session.Enemies.TrueForAll(e => !e.actor.IsAlive); } }
        public Villager NearestVillager(Vector3 p, float range, Villager except = null)
        {
            Villager best = null; float bestD = range * range;
            foreach (var v in Villagers) { if (!v.Visible || v == except) continue; float d = (v.transform.position - p).sqrMagnitude; if (d < bestD) { best = v; bestD = d; } }
            return best;
        }
        /// <summary>A place of this kind where someone is at work right now (the stall with the merchant behind it), so an errand goes to
        /// them. With a <paramref name="role"/>, only someone of that trade, and when none is at work there, one of that trade's own
        /// places all the same (one of the merchants' stalls, at random, while none is behind one).</summary>
        public Vector3? WorkedPlace(string kind, Villager except, string role = null)
        {
            if (!Places.TryGetValue(kind, out var list) || list.Count == 0) return null;
            foreach (var v in Villagers)
            {
                if (v == except || !v.Visible || v.Activity != kind || role != null && v.Role != role) continue;
                Vector3? best = null; float bestD = 2.5f * 2.5f;
                foreach (var p in list) { float d = (p - v.transform.position).sqrMagnitude; if (d < bestD) { bestD = d; best = p; } }
                if (best.HasValue) return best;
            }
            if (role == null) return null;
            var theirs = list.FindAll(p => placeName.TryGetValue(p, out var n) && ownerOf.TryGetValue(n, out var who) && Find(who) is Villager o && o.Role == role);
            return theirs.Count > 0 ? theirs[rng.Next(theirs.Count)] : (Vector3?)null;
        }

        // ---------- the village's stock: goods carried between the trades ----------
        /// <summary>What is left in yesterday's cask at the inn each morning, in tankards (the merchant brings a fresh one: "a cask for the inn").</summary>
        public const int MorningAle = 10;
        void MorningCask() { if (Places.TryGetValue("inn", out var seats) && seats.Count > 0) Stock["inn.ale"] = MorningAle; }
        /// <summary>What has been delivered where today, by "place.good" (inn.eggs, stall.flour, forge.wood...); see <see cref="VillageWork"/>.</summary>
        public readonly Dictionary<string, int> Stock = new Dictionary<string, int>();
        public int Count(string key) { return Stock.TryGetValue(key, out var n) ? n : 0; }
        public void Deliver(string key, int n) { Stock[key] = Count(key) + n; }
        /// <summary>Take some (what the stall sells on, what the kitchen uses); false when there is none.</summary>
        public bool Take(string key, int n = 1) { int have = Count(key); if (have < n) return false; Stock[key] = have - n; return true; }

        // ---------- what people say ----------
        static readonly string[] Wary = {
            "Keep your voice down. The grey-coats are still about.",
            "They took the Harrow girl last week. Said the Council needed her 'resonance'.",
            "Don't stare at the collectors. They remember faces.",
            "The well's gone bitter. Iron, my gran says. Or worse.",
            "Every morning the grey is a little closer. You can hear it, if the wind stops.",
            "Mira's at the Cask. She mends what the collectors break." };
        /// <summary>
        /// Crowsfoot Hollow (GAME-ONLY): Sandthrone deserters raid the north farms until the player clears the hollow (this
        /// quest), and then the talk turns. Oakhaven's wary pool carries the lines, so the draw from the shared rng is unchanged.
        /// </summary>
        public const string HollowQuest = "side.oakhaven.crowsfoot";
        static readonly string[] WaryRaided = new List<string>(Wary) {
            "Deserters came down the North road again last night. Sandthrone, or they were once.",
            "Bar your door after dark. The Crowsfoot lot don't knock.",
            "They've a king up in Crowsfoot Hollow, if you'll believe it. Tin crown and all." }.ToArray();
        static readonly string[] WaryAfterHollow = new List<string>(Wary) {
            "Not a door kicked in on the North road since you went up to Crowsfoot.",
            "Wil Carder's got his seed-corn back. First time I've seen him smile since harvest." }.ToArray();
        static readonly string[] Relieved = {
            "You drove them off? Then we've a few quiet nights, at least.",
            "I'll pour you a cup at the Cask. On me.",
            "They'll send more. They always send more. But thank you.",
            "The children can play on the green again." };
        static readonly string[] Afraid = {
            "Don't linger by the gallows. It listens.",
            "The Sandthrone take what they like and laugh while they do it.",
            "Something white walks by the crypt at dusk. It leaves no footprints." };
        /// <summary>The Verdant Shore (GAME-ONLY lines; the Keepers are CANON-EXPANDED): what the wood-folk say to you, what they murmur
        /// among themselves, and each resident's own two lines.</summary>
        static readonly string[] Keepers = {
            "The wood knows you now. Mostly.", "Walk soft on the roots. They are older than your grandmother's grandmother, and they remember her too.",
            "Iron rusts here. Words last longer, if you choose them well.", "Hoard nothing. The Shore takes back what is held too tight.",
            "Listen. The trees are passing word of you from root to root.", "The lanterns wake at dusk. Stay and see them.",
            "There is a cold in the east that was not there when I was a sapling.", "You flesh-folk are so loud. It is not your fault. You are very short.",
            "Mind the mist below the ridge. It does not breathe.", "Willow says you asked first. That is rare in your kind." };
        static readonly string[] KeeperChatter = { "...root to root, and the north wood answers.", "The sap runs slow this season.", "Something grey in the east. Still.", "Hush. The mere is dreaming.", "Mm. The flesh-folk again." };
        static readonly Dictionary<string, string[]> OwnLines = new Dictionary<string, string[]> {
            { "Willow-Whisper", new[] { "Stand still a moment. There. Now the wood can hear you.", "The Root-Mother sleeps. We sing so that she dreams well." } },
            { "Oak-Bane", new[] { "Flesh-folk. You are still here.", "The deep wood does not need you. One day it may put up with you." } },
            { "Moss-Lantern", new[] { "Sap-cakes, warm from the stone. One each. Well. Two.", "A lantern is only a little of the day, kept." } },
            { "Alder-Knot", new[] { "Wood, bone and patience. That is all a tool needs.", "Your sword will rust. Bring it to me when it does." } },
            { "Reed-Song", new[] { "Hush. The frogs are counting.", "The mere keeps its mist till noon. It is shy." } },
            { "Sister Iselle", new[] { "I am listening. You may listen too, if you are quiet.", "The Archive has words for this glade. None of them are right." } },
            { "Ondine Varro", new[] { "Charts, salt, and anything that glows. That's what House Glass pays for.", "There's a cove under that ridge with deep water close in. Somebody ought to be using it." } },
        };
        static readonly string[] ChildLines = { "Are you a real adventurer?", "Mum says not to go near the grey.", "I saw a crow as big as a dog!" };
        static readonly string[] Chatter = {
            "...and the miller swore it was a wolf.", "Two coppers for a loaf now!", "Did you see the sky over the east fields?",
            "Hush. Grey-coat, by the well.", "Harvest's thin this year.", "My roof leaks again.", "Old Tobin's been drinking since noon.",
            "They say the Wasting sings at night.", "Aye.", "Mm. Could be." };
        static readonly string[] HenwifeLines = {
            "Mind your feet. The speckled one's gone broody, and she bites.",
            "Foxes won't come near the grey. Nothing will. But collectors eat eggs same as anyone.",
            "Hens know when weather's turning. Lately they know when the grey's moving, too.",
            "Scatter the grain wide or the bossy ones get it all." };
        static readonly Dictionary<string, string[]> TradeLines = new Dictionary<string, string[]> {
            { "blacksmith", new[] { "The collectors took my best iron. 'Tithe,' they called it.", "Mind the sparks. Nails and hinges, mostly. Nobody orders blades any more.", "Bring me good ore and I'll make you something worth the carrying." } },
            { "merchant", new[] { "Prices are up. Nothing comes from the east now, and precious little from the west.", "Cloth, pots, a good knife. Coin or trade, I'm not proud.", "The Concord taxes the carts twice: once on the road, once at the stall." } },
            { "baker", new[] { "Bread's thin this week. The flour tastes faintly of ash.", "First loaves come out at dawn. Last ones go to whoever's still standing.", "Fire's never gone out in this oven. Not once, in forty years." } },
            { "hunter", new[] { "Game's gone west. Even the deer know better than to stay.", "Tracks stop dead at the grey. Like the animal just... wasn't.", "Stay out of the woods after dark. Not everything in there is hunting deer." } },
            { "lumberjack", new[] { "I won't cut east of the Harrow wood. The trees go grey from the heart out.", "Good oak splits clean. Grey oak crumbles like old bread.", "Winter's coming whether the Council likes it or not. Need wood stacked." } },
            { "herbalist", new[] { "Feverfew, yarrow, marigold... the grey leaves nothing that heals.", "If Mira's short of anything, tell her I've fresh comfrey.", "Some herbs only grow where the ground remembers being alive." } },
            { "leatherworker", new[] { "Good hides are scarce. Half of what comes in is grey at the edges.", "A proper belt outlasts a man. Mine outlasted two." } },
            { "skinner", new[] { "Stinks, I know. So does hunger.", "Bring me pelts and I'll pay fair. Rabbit, deer, anything that isn't grey." } },
            { "miller", new[] { "The wheel still turns. There's less to put under the stone every year.", "Flour's flour. Don't ask where the grain's been." } },
            { "elder", new[] { "I remember when the east was green to the horizon.", "The oak on the green was old when my grandmother was young. It'll see the lot of us out." } },
            { "innkeeper", new[] { "Ale's thin and the stew's thinner. Sit where you like.", "The Cask stood before the Concord and it'll stand after." } } };
        /// <summary>The innkeeper on Mira, while she still sits in the corner of his taproom (before she joins you).</summary>
        const string InnkeeperOnMira = "Mira's in the corner. Don't crowd her; she's the only mender we've got.";
        public string LineFor(Villager v, bool toPlayer)
        {
            if (Zone.Zone.life.mood == "keepers")
            {
                // The Verdant Shore: the Keepers and the two travellers among them have their own lines, and none of Oakhaven's talk.
                if (!toPlayer) return KeeperChatter[rng.Next(KeeperChatter.Length)];
                if (OwnLines.TryGetValue(v.Name, out var own) && rng.Next(2) == 0) return own[rng.Next(own.Length)];
                return v.Keeper ? Keepers[rng.Next(Keepers.Length)] : OwnLines.TryGetValue(v.Name, out var mine) ? mine[rng.Next(mine.Length)] : Keepers[rng.Next(Keepers.Length)];
            }
            if (v.PassedOut) return "Zzz...";
            if (!toPlayer) return Chatter[rng.Next(Chatter.Length)];
            if (rng.Next(3) == 0) { var day = StockLine(v); if (day != null) return day; }
            var purse = PurseLine(v); if (purse != null && rng.Next(3) == 0) return purse;
            if (v.Role == "innkeeper" && Session.Progress != null && !Session.Progress.recruited && rng.Next(3) == 0) return InnkeeperOnMira;
            if (!v.Keeper && TradeLines.TryGetValue(v.Role, out var trade) && rng.Next(3) > 0) return trade[rng.Next(trade.Length)];
            if (v.Role == "henwife" && v.Coop != null)
            {
                if (WorldClock.Between(19.3f, 21)) return "Can't stop, love. Hens to put away before dark.";
                if (v.Coop.LaidToday > 0 && rng.Next(3) == 0)
                    return v.Coop.LaidToday + (v.Coop.LaidToday == 1 ? " egg" : " eggs") + " so far today. Used to be twice that, before the grey crept closer.";
                return HenwifeLines[rng.Next(HenwifeLines.Length)];
            }
            if (v.Role == "child") return ChildLines[rng.Next(ChildLines.Length)];
            var mood = Zone.Zone.life.mood;
            var pool = mood == "afraid" ? (EnemiesCleared ? Relieved : Afraid) : EnemiesCleared ? Relieved : Wary;
            if (pool == Wary && Zone.Zone.id == "zone.oakhaven") pool = Session.Quests != null && Session.Quests.IsDone(HollowQuest) ? WaryAfterHollow : WaryRaided;
            return pool[rng.Next(pool.Length)];
        }
        /// <summary>The innkeeper, and the village, on meat the player sold today (a sale of anything whose trade is "inn.meat").</summary>
        const string MeatSoldInnkeeper = "Somebody's been selling meat in the village. It's all in the pot; the stew's not thin tonight.";
        string MeatSoldLine { get { return Zone.Zone.id == "zone.oakhaven" ? "Boar in the Cask's pot tonight. Somebody's been hunting." : "Meat in the pot at the inn tonight. Somebody's been hunting."; } }
        /// <summary>What the day's deliveries give people to say: the trades talk of each other's goods (see <see cref="Stock"/>),
        /// the player's sales among them (ore and wood to the forge, herbs to the stall, meat to the inn's pot: EncounterSession.SellBag,
        /// which marks its own as "sold." + the trade, so the merchant's fresh herbs are the player's and not the herbalist's daily
        /// errand, and meat sold in the village is somebody's hunting and not the hunter's hares).</summary>
        string StockLine(Villager v)
        {
            switch (v.Role)
            {
                case "merchant": return Count("stall.eggs") > 0 ? "Eggs in from the hen-wife, if you want them. Fresh today." : Count("sold.stall.herbs") > 0 ? "Fresh-cut herbs on the stall. Somebody's been in the meadow." : Count("stall.flour") > 0 ? "Flour from the mill, ground this afternoon. Dear, mind." : Count("stall.goods") > 0 ? "Belts and nails and hinges, all village-made. Nothing from the east." : null;
                case "baker": return Count("oven.flour") > 0 ? "The miller's flour came in. Thin stuff, but it rises." : WorldClock.Between(10, 19) ? "No flour from the mill yet today. The loaves'll be late." : null;
                case "blacksmith":
                    if (Count("forge.ore") > 0) return Zone.Zone.id == "zone.oakhaven" ? "Someone's been up the Crowsfoot with a pick. First ore I've not had to beg for." : "Someone's been out with a pick. First ore I've not had to beg for.";
                    return Count("forge.wood") > 0 ? "The woodcutter brought oak this morning. Hearth's drawing well." : null;
                case "miller": return Count("mill.grain") > 0 ? "Barley's in from the fields. The stone's turning on something, at least." : null;
                case "leatherworker": case "skinner": return Count("sold.tannery.hides") > 0 ? "Somebody's been selling pelts in the village. Clean ones, too; not a grey edge among them." : Count("tannery.hides") > 0 ? "The hunter's been by with a hide. Grey at one edge; the rest'll do." : null;
                case "innkeeper": return Places["inn"].Count > 0 && Count("inn.ale") == 0 ? "Dry. Ama's cask never sees the night out." : Count("sold.inn.meat") > 0 ? MeatSoldInnkeeper : Count("inn.meat") > 0 ? "Garet's hares are in the pot. Don't tell the out-of-work." : null;
                case "henwife": return Count("stall.eggs") > 0 ? "Eggs are at the produce stall if you're wanting any. I don't sell from the yard." : Count("inn.eggs") > 0 ? "Took the Cask its eggs this morning. The rest go to the stall after dinner." : null;
                case "drinker": case "elder": case "gossip": case "farmer":
                    return Places["inn"].Count > 0 && Count("inn.ale") == 0 ? "The cask's run dry at the inn. The out-of-work drank it by supper." : Count("sold.inn.meat") > 0 ? MeatSoldLine : Count("inn.meat") > 0 ? "Hare in the Cask's pot tonight. The hunter's doing." : Count("inn.bread") > 0 && Count("inn.eggs") > 0 ? "Bread and eggs at the Cask today. Like old times, nearly." : Count("inn.wood") > 0 ? "The Cask's got a fire going. Dry oak, for once." : null;
                default: return null;
            }
        }
        public void Talk(Villager v)
        {
            var line = LineFor(v, true); v.Say(line, 6); if (!v.PassedOut) v.FacePlayer();
            Session.Message(v.Name + ": " + line);
        }
    }

    /// <summary>
    /// A household (GAME-ONLY): the folk who share one house, from the zone's data (<see cref="ZoneHousehold"/>) or, in a zone
    /// that names none, one per house. house is the door they go in by (null when the house is not built; they lodge at the inn).
    /// </summary>
    public sealed class Household
    {
        public string name; public ZoneHousehold def; public ZoneDoor house;
        public readonly List<Villager> members = new List<Villager>();
        /// <summary>The member the data calls the head, else the first.</summary>
        public Villager Head
        {
            get
            {
                if (def != null) foreach (var m in def.members) if (m != null && m.kin == "head") { var v = members.Find(x => x.Name == m.name); if (v != null) return v; }
                return members.Count > 0 ? members[0] : null;
            }
        }
        /// <summary>Light or put out the chimney of their house (its smoke's emission; a door with no chimney is passed over).</summary>
        public void ApplyHearth(bool cold)
        {
            if (house == null || house.smoke == null) return;
            var em = house.smoke.emission; if (em.enabled == !cold) return;
            em.enabled = !cold;
        }
        /// <summary>How a member is kin ("head", "daughter"), or null.</summary>
        public string KinOf(Villager v)
        {
            if (def == null || v == null) return null;
            foreach (var m in def.members) if (m != null && m.name == v.Name) return m.kin;
            return null;
        }
    }

    /// <summary>
    /// A villager: picks an activity for their role, walks there, does it (working, chatting, sitting), and repeats.
    /// Flees home and hides when fighting breaks out nearby; comes back out once it has been calm for a while.
    /// </summary>
    public sealed class Villager : MonoBehaviour
    {
        enum State { Travel, Activity, Flee, Hidden }
        public string Name { get; private set; }
        public string Role { get; private set; }
        /// <summary>A resident keeps one post day and night (quest givers such as the Salt-Mender at the inn).</summary>
        public bool Resident { get { return fixedPlace != null; } }
        /// <summary>A Veridian Keeper (the Verdant Shore's wood-folk): their own body and their own lines.</summary>
        public bool Keeper { get; private set; }
        /// <summary>Trade shown under the name (&lt;Blacksmith&gt;), or null.</summary>
        public string Title { get; private set; }
        public string Bubble { get; private set; }
        public float BubbleUntil { get; private set; }
        public bool Visible { get { return state != State.Hidden; } }
        /// <summary>The coop a hen-wife keeps (null for everyone else).</summary>
        public ZoneCoop Coop { get; private set; }
        public string Activity { get { return activity; } }
        /// <summary>Goods in hand (see <see cref="Load"/>), carried on an errand between trades.</summary>
        public bool Carrying { get { return load != null; } }
        /// <summary>How much a drinker has had (0 sober; past <see cref="Tolerance"/> they fold or go home), how much they can hold, and
        /// whether they are done for the day (the ale ran out, or they did).</summary>
        public float Drunk { get { return drunk; } }
        public float Tolerance { get { return tolerance; } set { tolerance = value; } }
        public bool Spent { get { return spent; } }
        /// <summary>Slumped over the inn's table, dead to the world.</summary>
        public bool PassedOut { get { return activity == "passedout"; } }
        int index; float drunk, tolerance = 1; bool spent; GameObject tankard;
        public Load Carried { get { return loadKind; } }
        /// <summary>The errand under way (walking to pick up, or carrying), or null.</summary>
        public Errand Errand { get { return errand; } }
        /// <summary>Errands finished so far today, by id.</summary>
        public bool Done(string errandId) { return done.Contains(errandId); }
        /// <summary>The door of the house they live in (null: no house; they sleep on a bench at the inn).</summary>
        public ZoneDoor Home { get { return home; } }
        /// <summary>The household they belong to (see <see cref="VillageLife.Households"/>), or null.</summary>
        public Household Household { get; internal set; }
        /// <summary>Indoors at home: gone in to bed, to dinner, at the end of an errand home, or fled in.</summary>
        public bool Indoors { get { return state == State.Hidden && indoors; } }
        bool indoors;
        VillageLife life; NavMeshAgent agent; ActorVisual visual; ZoneDoor home; Renderer[] renderers;
        State state; string activity; float until, calmSince, nextBark, nextChatter, bedAt, wakeAt, nextHerd;
        Villager partner; string fixedPlace;
        GameObject load; Load loadKind; int loadCount; Errand errand; int leg; Vector3 dropAt; bool goingIn; readonly HashSet<string> done = new HashSet<string>(); float lastHour;
        /// <summary>Night: everyone goes home to bed (staggered; drinkers stay at the inn late, children go early).</summary>
        public bool Bedtime { get { return WorldClock.Between(bedAt, wakeAt); } }
        /// <summary>When a hunter who lives at a lodge sets off for bed: Oakhaven's is about a game hour's walk from the green.</summary>
        public const float HunterBed = 19.8f;

        public static Villager Spawn(VillageLife life, string name, string role, int index, ZoneDoor home, Vector3 at, ZoneCoop coop = null, string fixedPlace = null, string title = null, string look = null)
        {
            var go = new GameObject(name); go.SetActive(false); go.transform.position = at + Vector3.up;
            go.transform.SetParent(life.transform, false); go.transform.position = at + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            var v = go.AddComponent<Villager>(); v.life = life; v.Name = name; v.Role = role; v.home = home;
            v.agent = go.AddComponent<NavMeshAgent>(); v.agent.speed = role == "child" ? 2.4f : role == "elder" ? 1.1f : 1.6f;
            v.agent.angularSpeed = 360; v.agent.acceleration = 8; v.agent.stoppingDistance = .4f; v.agent.radius = .3f; v.agent.height = 2;
            v.agent.baseOffset = role == "child" ? .66f : 1; v.agent.avoidancePriority = 60;
            // A resident's own look (a Veridian Keeper): the body of the look, calm (an even variant), and no villager's outfit.
            v.Keeper = look == "keeper";
            v.visual = ActorVisual.Attach(go, v.Keeper ? ActorLook.Keeper : ActorLook.Villager, v.Keeper ? (index * 2) : index * 5 + 3, role == "child", v.Keeper ? null : role);
            if (v.Keeper) { v.agent.height = 2.6f; v.agent.baseOffset = 1; v.agent.speed = 1.3f; }
            v.Title = title ?? VillageLife.TitleFor(role); v.fixedPlace = fixedPlace;
            v.Coop = coop;
            // Bed and rising: the baker is up before dawn for the first loaves, the drinkers last to bed and last up, children early to bed;
            // the hunter early too where he has a lodge, for the long walk out to it; the innkeeper up at first light and last to bed
            // but the drinkers.
            v.bedAt = role == "drinker" ? 23 + (index % 3) * .3f : role == "child" ? 19.8f + (index % 3) * .2f : role == "henwife" ? HerdHour : role == "baker" ? 19.6f : role == "hunter" && life.Places["lodge"].Count > 0 ? HunterBed : role == "innkeeper" ? VillageWork.InnkeeperBed : 20.2f + (index % 5) * .25f;
            v.wakeAt = role == "drinker" ? 7.5f : role == "henwife" ? 5.7f : role == "baker" ? 4.6f : role == "innkeeper" ? VillageWork.InnkeeperUp : 5.8f + (index % 5) * .3f;
            if (fixedPlace != null) v.bedAt = v.wakeAt = 0;   // residents keep their post day and night
            v.lastHour = WorldClock.Hour; v.index = index; v.tolerance = .85f + (index % 5) * .18f;
            go.SetActive(true);
            // Only what's showing now: the placeholder capsule ActorVisual hides must stay hidden when they come back out.
            v.renderers = System.Array.FindAll(go.GetComponentsInChildren<Renderer>(), r => r.enabled);
            v.nextBark = Time.time + life.R01 * 10;
            if (v.Bedtime && home != null && (coop == null || !coop.Open)) { v.activity = "sleep"; v.StartActivity(); }   // spawned at night: already abed
            else v.ChooseNext();
            return v;
        }
        public void Say(string line, float seconds) { Bubble = line; BubbleUntil = Time.time + seconds; }
        public void FacePlayer()
        {
            var to = life.Session.Player.transform.position - transform.position; to.y = 0;
            if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(to);
            until = Mathf.Max(until, Time.time + 4);
            Hold(6);
        }
        /// <summary>Stop and attend to the player for a while (small talk). A quest conversation holds them for as long as it is open.</summary>
        public void Hold(float seconds) { heldUntil = Mathf.Max(heldUntil, Time.time + seconds); }
        float heldUntil; bool held; ActorPose heldPose; Vector3? heldDestination;
        bool Attending
        {
            get { var c = life.Session.Conversation; return (c != null && c.npc == Name) || Time.time < heldUntil; }
        }
        void Go(Vector3 target, float speedScale = 1)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            float reel = Reeling; visual.Stagger = reel; ShowTankard(false);
            agent.speed = (Role == "child" ? 2.4f : Role == "elder" ? 1.1f : 1.6f) * speedScale * (1 - .35f * reel);
            agent.isStopped = false; agent.SetDestination(target); state = State.Travel; visual.Pose = ActorPose.None;
        }
        /// <summary>
        /// What next: bed when it is time; a resident's post; the errand that is due (see <see cref="VillageWork"/>); otherwise one of
        /// the places the trade's shift says to be at this hour (a repeat weights the choice). Off the schedule they potter.
        /// </summary>
        void ChooseNext()
        {
            CancelErrand();
            if (Coop != null) { KeeperNext(); return; }
            if (Bedtime)
            {
                activity = "sleep";
                if (home != null) { Go(home.position); return; }
                var bed = life.RandomPlace("inn");   // no house: a bench at the inn
                if (bed.HasValue) Go(bed.Value); else { state = State.Activity; until = Time.time + 30; }
                return;
            }
            if (fixedPlace != null) { activity = fixedPlace; var spot = life.RandomPlace(fixedPlace); if (spot.HasValue) Go(spot.Value); else { state = State.Activity; until = Time.time + 20; } return; }
            if (Role == "drinker" && DrinkerNext()) return;
            if (StartErrand()) return;
            var shift = VillageWork.ShiftFor(Role, WorldClock.Hour);
            GoTo(shift != null ? VillageWork.PlacesFor(shift, Role, life.Places["lodge"].Count > 0) : Role == "child" ? new[] { "green", "green", "wander", "wander", "well" } : new[] { "wander", "green" });
        }
        /// <summary>Walk to one of these kinds of place (a repeat weights the choice; a kind this village lacks is passed over).</summary>
        void GoTo(string[] options)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                activity = options[life.Next(options.Length)];
                var target = activity == "home" || activity == "yard" ? Resolve(activity) : life.RandomPlace(activity, this);   // their own stall, shop or fields first
                if (target.HasValue) { Go(target.Value); return; }
            }
            activity = "wander"; var alt = life.RandomPlace("wander") ?? life.RandomPlace("green");
            if (alt.HasValue) Go(alt.Value); else { state = State.Activity; until = Time.time + 5; }
        }
        void StartActivity()
        {
            if (errand != null) { ErrandArrive(); return; }
            if (Role == "drinker" && activity == "inn") { DrinkRound(); return; }
            if (activity == "sleepitoff") { state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true; until = Time.time + 30; if (home != null) Hide(); return; }
            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            until = Time.time + (activity == "home" ? HomeStay : 14 + life.R01 * 30);
            visual.Pose = PoseFor(activity);
            var look = life.LookFor(transform.position); if (look.HasValue) Face(look.Value);
            if (Coop != null) KeeperStart();
            if ((activity == "home" || activity == "sleep") && home != null) Hide();
        }
        /// <summary>Indoors for a short call, or for the rest of a shift spent at home (the children's dinner, the hen-wife's).</summary>
        float HomeStay
        {
            get
            {
                var s = VillageWork.ShiftFor(Role, WorldClock.Hour);
                return s != null && s.places.Length == 1 && s.places[0] == "home" ? Mathf.Clamp((s.to - WorldClock.Hour) * WorldClock.RealMinutesPerDay * 60 / 24, 20, 240) : 20;
            }
        }

        // ---------- errands: goods carried between the trades (see VillageWork) ----------
        /// <summary>Start the first errand of the day that is due and not yet done; false when there is none, or its places are not in this village.</summary>
        bool StartErrand()
        {
            float hour = WorldClock.Hour;
            if (hour < lastHour - 6) done.Clear();   // a new day (or the clock jumped back to the morning)
            lastHour = hour;
            // The household's shopping first, when its purse has the coin claimed (VillageLife.ShoppingFor): the family sets out when paid.
            foreach (var e in life.ShoppingFor(this)) if (!done.Contains(e.id)) { if (Begin(e)) return true; life.ErrandEnded(this, e, false); }
            var day = VillageWork.DayFor(Role); if (day == null) return false;
            foreach (var e in day.errands)
            {
                if (done.Contains(e.id) || hour < e.at || hour >= e.until) continue;
                if (e.need != null && !life.MayFetch(this, e)) { done.Add(e.id); continue; }   // the child's loaf, with no coin for bread
                if (Begin(e)) return true;
            }
            return false;
        }
        /// <summary>Set off on an errand (marked done for today either way); false when its places are not in this village or are blocked.</summary>
        bool Begin(Errand e)
        {
            done.Add(e.id);
            // Buying for the household: from the seller's own place (Ama Rusk's Produce stall for eggs), where the coin goes.
            var keeper = e.need != null ? life.Seller(e.need) : null;
            var from = keeper != null && life.OwnPlaces(keeper, e.from) != null ? life.RandomPlace(e.from, keeper) : Resolve(e.from, e.fromRole); if (!from.HasValue) { Blocked(e.from); return false; }
            Vector3? to = null; if (e.to != null) { to = HandOverAt(e.door) ?? Resolve(e.to, e.toRole); if (!to.HasValue) { Blocked(e.to); return false; } }
            errand = e; leg = 0; dropAt = to ?? from.Value; activity = e.id; Go(from.Value); return true;
        }
        /// <summary>Where goods are handed over at a door (<see cref="Errand.door"/>: the kitchen's back door, the bar), or null when
        /// the village has no such place. Hen-wives come to the kitchen together, so each takes a spot nobody else is bound for.</summary>
        Vector3? HandOverAt(string door)
        {
            if (door == null || !life.Places.TryGetValue(door, out var spots) || spots.Count == 0) return null;
            foreach (var p in spots) if (!life.Villagers.Exists(v => v != this && v.errand != null && (v.dropAt - p).sqrMagnitude < .25f)) return p;
            return spots[life.Next(spots.Count)];
        }
        /// <summary>Who answers a hand-over. At a door (the kitchen's back door, the bar): only the innkeeper (where there is none,
        /// whoever keeps the kitchen or the bar), at work or passing by within reach of it, whatever he is about (the pot on, the
        /// dinner); else nobody, never another carrier. The reach (4 m at the kitchen's door, 3 m at the bar) takes in the range and
        /// the table but not the taproom through the wall. Otherwise whoever is nearest within 6 m, stood at their work and not
        /// carrying goods of their own.</summary>
        Villager Taker(Errand e)
        {
            Villager best = null; float bestD;
            if (e.door != null && life.Places.TryGetValue(e.door, out var spots) && spots.Count > 0)
            {
                bestD = e.door == VillageWork.Bar ? 3 * 3 : 4 * 4;
                foreach (var v in life.Villagers)
                {
                    if (v == this || !v.Visible || v.state == State.Flee || v.Role != "innkeeper" && (v.errand != null || v.activity != "kitchen" && v.activity != "bar")) continue;
                    float d = (v.transform.position - transform.position).sqrMagnitude; if (d < bestD) { bestD = d; best = v; }
                }
                return best;
            }
            bestD = 6 * 6;
            foreach (var v in life.Villagers)
            {
                if (v == this || !v.Visible || v.state != State.Activity || v.errand != null) continue;
                float d = (v.transform.position - transform.position).sqrMagnitude; if (d < bestD) { bestD = d; best = v; }
            }
            return best;
        }
        /// <summary>Give up the errand (goods posed for a capture are dropped too). A purchase not yet picked up keeps its coin set
        /// aside and is tried again while its window is open; with <paramref name="again"/> false (the place could not be reached)
        /// its coin is let go.</summary>
        void CancelErrand(bool again = true)
        {
            if (errand != null && errand.need != null && life.ErrandEnded(this, errand, leg > 0, again)) done.Remove(errand.id);
            errand = null; goingIn = false; DropLoad();
        }
        /// <summary>An errand's place is there but nobody can get near it for the grey-coats: say so, and let it go for today.</summary>
        void Blocked(string place)
        {
            if (!life.Places.TryGetValue(place, out var l) || l.Count == 0 || life.R01 > .7f) return;
            Say(place == "well" ? "Can't get to the well with that grey-coat stood by it." : "Not with the grey-coats about. It'll keep.", 4);
        }
        /// <summary>Where an errand's place is: home, the coop's spots, one of their own places of the kind (the baker's loaves to her
        /// own stall), or a kind of place (where one of that trade, or of <paramref name="role"/>, is working now, if any).</summary>
        Vector3? Resolve(string place, string role = null)
        {
            switch (place)
            {
                case "home": return home != null ? home.position : (Vector3?)null;
                case "nest": return Coop != null ? Coop.nest : (Vector3?)null;
                case "trough": return Coop != null ? Coop.trough : (Vector3?)null;
                case "pan": return Coop != null ? Coop.pan : (Vector3?)null;
                case "yard": return Coop != null ? Around(Coop.yard, 3) : null;
            }
            if (life.OwnPlaces(this, place) != null) return life.RandomPlace(place, this);
            return life.WorkedPlace(place, this, role) ?? life.RandomPlace(place);
        }
        /// <summary>At the end of an errand's leg: pick up (what the place yields shows in the hands), or hand over and say so.</summary>
        void ErrandArrive()
        {
            var e = errand; state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            var look = life.LookFor(transform.position); if (look.HasValue) Face(look.Value);
            if (leg == 0)
            {
                // Buying for the household: paid at pick-up; with the coin gone (a claim let go), they come away with nothing.
                if (e.need != null && !life.Fetched(this, e)) { errand = null; visual.Pose = ActorPose.None; until = Time.time + 3; Say(VillageWork.ShortLine(e.need, Role == "child"), 4); return; }
                int n = e.amount;
                if (e.from == "nest") { n = Coop.Eggs; Coop.Eggs = 0; Face(Coop.door + (Coop.nest - Coop.door) * 2); }
                if (e.from == "trough") { Coop.FedAt = Time.time; Coop.FeedSpot = Coop.trough; Face(Coop.trough + (Coop.trough - Coop.yard)); }
                visual.Pose = e.pose; until = Time.time + e.work; loadCount = n;
                string pick = e.from == "nest" ? EggLine(n) : e.pickupLine ?? (e.to == null ? e.line : null);
                if (pick != null && pick.Contains("{seller}")) { var seller = life.SellerName(e.need); pick = seller != null ? pick.Replace("{seller}", seller) : pick.Replace(", {seller}", ""); }
                if (pick != null) Say(pick, 4);
                if (e.to == null || n == 0) { errand = null; return; }   // a task done on the spot, or nothing to carry
                if (e.load != Load.None) Carry(e.load, n);
            }
            else
            {
                visual.Pose = e.dropPose; until = Time.time + e.work;
                if (e.good != null) life.Deliver(e.to + "." + e.good, Mathf.Max(1, loadCount));
                if (e.to == "pan") { Coop.Water(); Face(Coop.pan + (Coop.pan - Coop.yard)); }
                if (e.line != null) Say(e.line.Replace("{n}", loadCount.ToString()), 5);
                var taker = Taker(e);
                if (taker != null) { var reply = VillageWork.Reply(e.good, life.R01); if (reply != null) taker.Say(reply, 4); taker.Face(transform.position); }
                life.HandOver(this, e, taker);
                if (e.need != null) life.ErrandEnded(this, e, true);   // bought and brought home: firewood lights the hearth
                DropLoad();
                if (e.to == "home" && home != null) { activity = "home"; goingIn = true; until = Time.time + 4; }
                errand = null;
            }
        }
        /// <summary>Capture/debug: hold these goods (as on an errand) while posed by <see cref="StandAt"/>.</summary>
        public void ShowLoad(Load what, int count) { Carry(what, count); }
        /// <summary>Capture/debug: sat at an inn seat with a tankard, or slumped over the table, until <see cref="Release"/>.</summary>
        public bool PoseAtInn(int seat, bool slumped)
        {
            if (!life.Places.TryGetValue("inn", out var seats) || seats.Count == 0 || !agent.isOnNavMesh) return false;
            foreach (var r in renderers) r.enabled = true; CancelErrand();
            agent.Warp(seats[seat % seats.Count]); agent.isStopped = true; state = State.Activity; until = Time.time + 60; enabled = false;
            activity = slumped ? "passedout" : "inn"; visual.Pose = slumped ? ActorPose.Slump : ActorPose.Drink; ShowTankard(!slumped);
            return true;
        }
        static string EggLine(int n) { return n >= 5 ? n + " eggs. Good girls." : n >= 2 ? n + " eggs. They're off-lay; it's the grey, I'd wager." : n == 1 ? "Just the one. Well. It's something." : "Nothing. Off-lay, the lot of you."; }
        void Carry(Load what, int count) { DropLoad(); loadKind = what; loadCount = count; load = LoadProps.Build(transform, what, count); }
        void DropLoad() { if (load != null) Destroy(load); load = null; loadKind = Load.None; }
        /// <summary>Capture/debug: go straight to a place of this kind and work there for a minute.</summary>
        public bool WorkAt(string place)
        {
            if (!life.Places.TryGetValue(place, out var list) || list.Count == 0 || !agent.isOnNavMesh) return false;
            foreach (var r in renderers) r.enabled = true; CancelErrand();
            agent.Warp(list[0]); activity = place; StartActivity(); until = Time.time + 60; return true;
        }
        /// <summary>Capture/debug: stop and stand still at a spot, facing a direction (for line-up shots).</summary>
        public void StandAt(Vector3 at, float yaw)
        {
            foreach (var r in renderers) r.enabled = true; CancelErrand();
            if (agent.isOnNavMesh) { agent.Warp(at); agent.isStopped = true; }
            transform.rotation = Quaternion.Euler(0, yaw, 0); visual.Pose = ActorPose.None; state = State.Activity; activity = "posed"; until = Time.time + 60; enabled = false;
        }
        /// <summary>Capture/debug: out of sight and out of the way (no body, no nameplate) until <see cref="Release"/>.</summary>
        public void Park()
        {
            state = State.Hidden; indoors = false; Bubble = null; foreach (var r in renderers) r.enabled = false;
            CancelErrand(); ShowTankard(false);   // not left hanging in the air where they stood
            if (agent.isOnNavMesh) agent.isStopped = true; enabled = false;
        }
        /// <summary>Capture/debug: indoors at home now, as at the end of an errand home, until <see cref="Release"/>. False with no home.</summary>
        public bool GoIndoors()
        {
            if (home == null || !agent.isOnNavMesh) return false;
            CancelErrand(); if (NavMesh.SamplePosition(home.position, out var hit, 2.5f, NavMesh.AllAreas)) agent.Warp(hit.position);
            activity = "home"; until = Time.time + 600; Hide(); enabled = false; return true;
        }
        /// <summary>Capture/debug: back to the day's routine after <see cref="StandAt"/>, <see cref="Park"/> or <see cref="GoIndoors"/>, starting from where they were.</summary>
        public void Release(Vector3 at)
        {
            foreach (var r in renderers) r.enabled = true;
            if (agent.isOnNavMesh && NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas)) agent.Warp(hit.position);
            enabled = true; state = State.Activity; until = 0; visual.Pose = ActorPose.None; ChooseNext();
        }
        /// <summary>What working looks like for this trade at this place.</summary>
        ActorPose PoseFor(string place)
        {
            switch (place)
            {
                case "field": return Role == "farmer" ? ActorPose.Work : ActorPose.Gather;
                case "mill": return ActorPose.Work;
                case "well": return life.R01 < .5f ? ActorPose.Work : ActorPose.Talk;
                case "inn": case "sleep": return ActorPose.Sit;
                case "forge": return ActorPose.Hammer;
                case "stall": return Role == "merchant" || Role == "baker" && life.R01 < .5f ? ActorPose.Talk : ActorPose.Knead;
                case "oven": case "tannery": case "leathershop": case "dryhut": case "kitchen": return ActorPose.Knead;
                case "bar": return ActorPose.Talk;
                case "lodge": return ActorPose.Work;
                case "woodpile": return ActorPose.Chop;
                case "woods": return Role == "lumberjack" ? ActorPose.Chop : Role == "hunter" || Role == "warden" ? ActorPose.None : ActorPose.Gather;
                case "meadow": return Role == "hunter" ? ActorPose.None : ActorPose.Gather;
                default: return ActorPose.None;
            }
        }
        /// <summary>Out of sight and counted indoors at home. Every caller is at their door, but for a flight stranded short of it, which
        /// hides where it stopped and is not counted at home (see the flee branch of Update).</summary>
        void Hide()
        {
            state = State.Hidden; indoors = true; calmSince = Time.time; foreach (var r in renderers) r.enabled = false;
            DropLoad(); ShowTankard(false);   // the goods put away in the pantry
            if (agent.isOnNavMesh) agent.isStopped = true; Bubble = null;
        }
        void Emerge()
        {
            foreach (var r in renderers) r.enabled = true;
            if (home != null && agent.isOnNavMesh && NavMesh.SamplePosition(home.position, out var hit, 2, NavMesh.AllAreas)) agent.Warp(hit.position);
            if (activity == "fled") Say(life.EnemiesCleared ? "Is it over? ...Gods be thanked." : "Is it quiet? Is it safe?", 4);
            else if (activity == "sleep" && life.R01 < .3f) Say(life.R01 < .5f ? "Another grey morning." : "Morning. Still here, then.", 4);
            else if (activity == "sleepitoff") Say(life.R01 < .5f ? "Never again. ...Is the Cask open yet?" : "My head. Who put the sun there?", 5);
            if (activity == "sleep" || activity == "sleepitoff") { drunk = 0; spent = false; visual.Stagger = 0; }   // a new day, a clear head
            ChooseNext();
        }

        // ---------- the out of work at the inn ----------
        static readonly string[] SoberLines = { "Same again.", "To absent friends.", "First of the day. Well. Of the afternoon.", "No work, no worry. That's what I tell the wife." };
        static readonly string[] MerryLines = { "I'm not shaying the Council's wrong. I'm shaying... what was I shaying?", "Lissen. Lissen. The grey's jusht weather.", "Another! For the Harrow girl.", "They took my trade. They can't take my thirsht." };
        static readonly string[] FarGoneLines = { "Thersh two of you. Both ugly.", "I can shee the Washting from here. 'S pretty.", "Hic.", "'M fine. 'M fine. The floor's drunk." };
        static readonly string[] LeavingLines = { "Thass me done. G'night, all.", "Home. Before she locks the door.", "One more and I'd be under the table. G'night." };
        static readonly string[] DryLines = { "Dry? The Cask's dry? Then I'm for home.", "No ale. No work and no ale. What a village.", "Empty. Somebody tell the merchant." };
        /// <summary>0 steady to 1 reeling, from how near a drinker is to their limit.</summary>
        float Reeling { get { return Role == "drinker" && tolerance > 0 ? Mathf.Clamp01((drunk / tolerance - .45f) / .55f) : 0; } }
        void ShowTankard(bool on)
        {
            if (on && tankard == null && visual != null && visual.RightArm != null) tankard = LoadProps.Tankard(visual.RightArm);
            if (tankard != null) tankard.SetActive(on);
        }
        /// <summary>
        /// The out of work (the drinkers, and anyone whose trade this village has no place for) drink at the inn until the ale is gone or
        /// they are. Past their limit some fold over the table where they sit and the rest say goodnight while they can; spent, they go
        /// home to sleep it off until morning (with no home, they stay slumped at the table). False: the day's shift decides.
        /// </summary>
        bool DrinkerNext()
        {
            if (activity == "passedout" && !spent) { spent = true; Say("Ugh. My head. Who moved the floor?", 5); }   // come round, hours later
            if (!spent && drunk >= tolerance)
            {
                if (activity == "inn" && (index % 5 < 2 || home == null)) { PassOut(); return true; }
                spent = true; Say(LeavingLines[life.Next(LeavingLines.Length)], 5);
            }
            if (!spent) return false;
            if (home == null) { if (activity == "inn" || activity == "passedout") { PassOut(); return true; } return false; }
            activity = "sleepitoff"; Go(home.position); return true;
        }
        void PassOut()
        {
            activity = "passedout"; state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            ShowTankard(false); visual.Pose = ActorPose.Slump; until = Time.time + 280 + life.R01 * 220; Say("Zzz...", 6); nextBark = Time.time + 20;
        }
        /// <summary>A round at the inn: a tankard off the day's cask (the village's stock, "inn.ale") and a little further gone; when the
        /// cask is dry they grumble and call it a day.</summary>
        void DrinkRound()
        {
            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            var look = life.LookFor(transform.position); if (look.HasValue) Face(look.Value);
            until = Time.time + 16 + life.R01 * 22;
            if (spent || !life.Take("inn.ale"))
            {
                visual.Pose = ActorPose.Sit; ShowTankard(false);
                if (!spent) { spent = true; Say(DryLines[life.Next(DryLines.Length)], 5); until = Time.time + 8; }
                return;
            }
            drunk += .07f + life.R01 * .05f; visual.Pose = ActorPose.Drink; ShowTankard(true);
            float gone = drunk / Mathf.Max(.01f, tolerance); var lines = gone < .4f ? SoberLines : gone < .75f ? MerryLines : FarGoneLines;
            if (life.R01 < .55f) Say(lines[life.Next(lines.Length)], 4);
        }

        // ---------- the hen-wife ----------
        /// <summary>Hens start heading in on their own from 18:36; she sweeps up the stragglers from 19:18.</summary>
        const float HerdHour = 19.3f;
        static readonly string[] HerdLines ={ "Come on, you daft bird!", "In you go. In!", "Shoo, shoo! Bedtime.", "Not you again, Speckle." };
        /// <summary>
        /// Her day (the schedule in <see cref="VillageWork"/>): open the coop at first light and scatter the morning feed; eggs to the
        /// inn's kitchen; water from the well for the hens, morning and afternoon; dinner at home; the afternoon feed; eggs to the
        /// produce stall; the last eggs home for the pot; and pottering in the yard between. Towards dusk she stays by the coop and
        /// watches them go up the ramp; from <see cref="HerdHour"/> she herds stragglers in, shuts the door, and goes to bed.
        /// </summary>
        void KeeperNext()
        {
            if (Coop.Open && WorldClock.Between(HerdHour, 5.5f)) { activity = "herd"; Go(Coop.yard, 1.2f); return; }
            if (Bedtime) { activity = "sleep"; if (home != null) Go(home.position); else { state = State.Activity; until = Time.time + 30; } return; }
            if (!Coop.Open) { activity = "open"; Go(Coop.door); return; }
            if (StartErrand()) return;
            var shift = VillageWork.ShiftFor(Role, WorldClock.Hour);
            GoTo(shift != null ? shift.places : new[] { "yard", "yard", "yard", "well" });
        }
        Vector3? Around(Vector3 p, float r)
        {
            var q = p + new Vector3(life.R01 - .5f, 0, life.R01 - .5f) * r * 2;
            return NavMesh.SamplePosition(q, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : (Vector3?)null;
        }
        void Face(Vector3 p) { var to = p - transform.position; to.y = 0; if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(to); }
        void KeeperStart()
        {
            switch (activity)
            {
                case "open":
                    Coop.SetOpen(true); visual.Pose = ActorPose.Work; until = Time.time + 5; Face(Coop.door + (Coop.door - Coop.yard) * .3f);
                    Say(life.R01 < .5f ? "Up you get, girls. Out you come." : "Morning, ladies. Mind the step.", 4); break;
                case "yard": visual.Pose = ActorPose.None; until = Time.time + 12 + life.R01 * 18; if (life.R01 < .35f) Say(life.R01 < .5f ? "There's my speckled lady." : "Who's been scratching up my beans?", 4); break;
                case "herd": visual.Pose = ActorPose.None; until = float.MaxValue; break;
                case "close":
                    Coop.SetOpen(false); visual.Pose = ActorPose.Work; until = Time.time + 4;
                    Say("That's all of you. Night, girls.", 4); break;
            }
        }
        /// <summary>Dusk: walk round behind the farthest straggler so it runs for the coop; shut the door once all are in.</summary>
        void Herd()
        {
            if (Time.time < nextHerd) return;
            nextHerd = Time.time + .6f;
            Critter straggler = null; float far = -1; bool anyOut = false;
            foreach (var hen in life.HensOf(Coop))
            {
                if (hen.Roosting) continue;
                anyOut = true;
                if ((hen.transform.position - transform.position).sqrMagnitude < 12) hen.Shoo();
                float d = (hen.transform.position - Coop.door).sqrMagnitude;
                if (!hen.Returning && d > far) { far = d; straggler = hen; }
            }
            if (!anyOut || WorldClock.Between(22, 5.5f)) { activity = "close"; Go(Coop.door); return; }
            if (!agent.isOnNavMesh) return;
            agent.isStopped = false; agent.speed = 2.2f; visual.Pose = ActorPose.None;
            if (straggler != null)
            {
                var from = straggler.transform.position - Coop.door; from.y = 0;
                var behind = straggler.transform.position + (from.sqrMagnitude > .01f ? from.normalized : Vector3.forward) * 1.6f;
                if (NavMesh.SamplePosition(behind, out var hit, 2, NavMesh.AllAreas)) agent.SetDestination(hit.position);
                if (life.R01 < .06f) Say(HerdLines[life.Next(HerdLines.Length)], 3);
            }
            else agent.SetDestination(Coop.yard);   // all heading in: wait by the ramp
        }
        void Flee()
        {
            state = State.Flee; visual.Pose = ActorPose.None; Say(life.R01 < .5f ? "Run! Get inside!" : "Collectors! Hide!", 3);
            if (home != null && agent.isOnNavMesh) { agent.speed = 4; agent.isStopped = false; agent.SetDestination(home.position); }
            else { state = State.Activity; visual.Pose = ActorPose.Cower; until = Time.time + 8; }
        }
        bool Arrived { get { return agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + .25f; } }
        /// <summary>Came to the end of a path that could not reach the place (e.g. across deep water).</summary>
        bool Stranded { get { return agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + .25f; } }
        void Update()
        {
            if (life == null || !agent.isOnNavMesh) return;
            bool danger = life.Danger(transform.position);
            if (state == State.Hidden)
            {
                if (danger) calmSince = Time.time;
                else if (activity == "sleep") { if (!Bedtime || Interrupted) Emerge(); }
                else if (activity == "sleepitoff") { if (WorldClock.Between(wakeAt, 10.5f)) Emerge(); }   // not before morning
                else if (Coop != null && Interrupted) Emerge();   // hens before anything else at dusk
                else if (Time.time - calmSince > (activity == "home" ? until - calmSince : 12)) Emerge();
                return;
            }
            if (state == State.Flee)
            {
                // Only a flight that reached the door counts as indoors; one stranded short of it hides where it stopped.
                if (Arrived || Stranded) { activity = "fled"; Hide(); var d = transform.position - home.position; d.y = 0; indoors = d.magnitude < 3.5f; }
                return;
            }
            if (danger && Role != "drinker" || danger && life.EnemyNear(transform.position, 10)) { Flee(); return; }
            // Talking with the player: stand still and face them until the conversation ends, then carry on.
            if (Attending && !PassedOut)
            {
                var to = life.Session.Player.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 6);
                if (!held)
                {
                    // Stop dead: drop the path (kept to resume) so the agent doesn't coast on.
                    held = true; heldPose = visual.Pose; visual.Pose = ActorPose.Talk;
                    heldDestination = agent.isOnNavMesh && agent.hasPath ? agent.destination : (Vector3?)null;
                    if (agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); agent.velocity = Vector3.zero; }
                }
                return;
            }
            if (held)
            {
                held = false; visual.Pose = heldPose;
                if (state == State.Travel && agent.isOnNavMesh && heldDestination.HasValue) { agent.isStopped = false; agent.SetDestination(heldDestination.Value); }
                until = Mathf.Max(until, Time.time + 3);
            }
            if (Interrupted) { partner = null; ChooseNext(); return; }
            if (state == State.Travel && Stranded) { partner = null; CancelErrand(false); activity = "wander"; var alt = life.RandomPlace("wander"); if (alt.HasValue) Go(alt.Value); else { state = State.Activity; until = Time.time + 5; } return; }
            if (state == State.Travel && Arrived) StartActivity();
            if (state == State.Activity)
            {
                if (activity == "green" || activity == "well") Chat();
                if (activity == "herd") Herd();
                else if (Time.time > until)
                {
                    partner = null;
                    if (errand != null && leg == 0) { leg = 1; Go(dropAt); }   // picked up: carry it over
                    else if (goingIn) { goingIn = false; until = Time.time + 20; Hide(); }   // said at the door; now indoors a while
                    else ChooseNext();
                }
            }
            BarkAtPlayer();
        }
        /// <summary>The hour calls them elsewhere: bedtime for most; for the hen-wife, dusk means the hens come first.</summary>
        bool Interrupted
        {
            get
            {
                if (Coop != null) return Coop.Open && activity != "herd" && activity != "close" && WorldClock.Between(HerdHour, 5.5f);
                return activity != "sleep" && Bedtime;
            }
        }
        void Chat()
        {
            if (partner == null || !partner.Visible || (partner.transform.position - transform.position).sqrMagnitude > 16)
            {
                partner = null;
                foreach (var other in life.Villagers)
                    if (other != this && other.Visible && other.state == State.Activity && (other.transform.position - transform.position).sqrMagnitude < 16) { partner = other; break; }
            }
            if (partner == null) return;
            var to = partner.transform.position - transform.position; to.y = 0;
            if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3);
            visual.Pose = ActorPose.Talk;
            if (Time.time > nextChatter) { nextChatter = Time.time + 7 + life.R01 * 9; if (life.R01 < .6f) Say(life.LineFor(this, false), 4); }
        }
        void BarkAtPlayer()
        {
            if (PassedOut) { if (Time.time >= nextBark) { nextBark = Time.time + 18 + life.R01 * 14; Say(life.R01 < .7f ? "Zzz..." : "...'nother one...", 4); } return; }
            var player = life.Session.Player; if (player == null || !player.IsAlive || Time.time < nextBark) return;
            if ((player.transform.position - transform.position).sqrMagnitude < 16 && !life.Session.InCombat)
            {
                nextBark = Time.time + 40 + life.R01 * 30;
                if (life.R01 < .55f) { Say(life.LineFor(this, true), 5); FacePlayer(); }
            }
        }
    }

    /// <summary>
    /// A critter: wanders and grazes near home, never fights, flees from the player. Crows take wing. Built from
    /// primitives standing on legs that step in a gait, with small idle motions (pecking, grazing, hopping, flapping).
    /// No colliders, not targetable.
    /// </summary>
    public sealed class Critter : MonoBehaviour
    {
        enum State { Idle, Walk, Flee, Fly, Feed, Return, Roost }
        public string Kind { get; private set; }
        /// <summary>A hen's coop: she roosts inside at night, rushes the feed, and heads in at dusk.</summary>
        public ZoneCoop Coop { get; private set; }
        public bool Roosting { get { return state == State.Roost; } }
        public bool Returning { get { return state == State.Return; } }
        VillageLife life; Vector2 home; float radius; State state; Vector3 target, flyFrom; float until, speed, fleeSpeed, fleeRadius, flyT, fedSeen = -999;
        Transform body, head, wingL, wingR; float phase, seed; Renderer[] rends; bool wingsSpread;
        // Legs on hip pivots (see Leg): lag is the leg's place in the stride in radians, or -1 to swing with a rabbit's hop;
        // amp its swing in degrees (negative reaches forward). stepRate: phase per m/s that keeps a planted foot from sliding.
        readonly List<(Transform hip, float lag, float amp)> legs = new List<(Transform, float, float)>();
        float stepRate = 12, legLen, stride, tuck; bool atRest;
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        static Material Mat(Color c)
        {
            if (!mats.TryGetValue(c, out var m) || m == null) { m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", .1f); mats[c] = m; }
            return m;
        }
        Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3? euler = null)
        {
            var o = GameObject.CreatePrimitive(type); Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            if (euler.HasValue) o.transform.localEulerAngles = euler.Value;
            o.GetComponent<Renderer>().sharedMaterial = Mat(c); o.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return o.transform;
        }
        /// <summary>A leg on a hip pivot in body space, hip.y above the ground: a slim cylinder (thick &gt; 0) down to the
        /// ground, swung by <see cref="Legs"/>. lag: its place in the stride (0-1), or -1 to swing with the hop; amp: degrees.</summary>
        Transform Leg(Vector3 hip, float thick, Color c, float lag, float amp)
        {
            var t = new GameObject("Leg").transform; t.SetParent(body, false); t.localPosition = hip;
            if (thick > 0) Part(PrimitiveType.Cylinder, t, new Vector3(0, -hip.y / 2, 0), new Vector3(thick, hip.y / 2, thick), c);
            legs.Add((t, lag < 0 ? -1 : lag * Mathf.PI * 2, amp)); legLen = hip.y;
            stepRate = Mathf.PI / (2 * hip.y * Mathf.Sin(Mathf.Abs(amp) * Mathf.Deg2Rad));   // a half-stride carries it 2 * len * sin(amp)
            return t;
        }
        /// <summary>A bird's foot at the bottom of a leg: one toe fore and aft (the hind toe behind) and two splayed forward.</summary>
        void Toes(Transform leg, float toe, Color c)
        {
            var ankle = new Vector3(0, .006f - leg.localPosition.y, 0); var size = new Vector3(toe * .28f, .012f, toe);
            Part(PrimitiveType.Cube, leg, ankle + new Vector3(0, 0, toe * .3f), new Vector3(size.x, size.y, toe * 1.6f), c);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, leg, ankle + Quaternion.Euler(0, s * 38, 0) * new Vector3(0, 0, toe * .5f), size, c, new Vector3(0, s * 38, 0));
        }
        public static Critter Spawn(VillageLife life, string kind, Vector2 center, float radius)
        {
            var go = new GameObject(kind); go.transform.SetParent(life.transform, false);
            var c = go.AddComponent<Critter>(); c.life = life; c.Kind = kind; c.home = center; c.radius = radius;
            // Start on dry, walkable ground (never on a creek or pond bed).
            var start = center + new Vector2(life.R01 - .5f, life.R01 - .5f) * radius * 1.6f;
            for (int tries = 0; tries < 12 && life.Zone.WaterAt(start, out _, out _); tries++) start = center + new Vector2(life.R01 - .5f, life.R01 - .5f) * radius * 1.6f;
            var spawnAt = life.Zone.Ground(start);
            if (NavMesh.SamplePosition(spawnAt, out var spawnHit, 3, NavMesh.AllAreas) && !life.Zone.WaterAt(new Vector2(spawnHit.position.x, spawnHit.position.z), out _, out _)) spawnAt = spawnHit.position;
            go.transform.position = spawnAt; go.transform.rotation = Quaternion.Euler(0, life.R01 * 360, 0);
            c.seed = life.R01 * 100; c.Build(kind); c.until = Time.time + life.R01 * 4;
            c.rends = go.GetComponentsInChildren<Renderer>();
            if (kind == "chicken")
            {
                float best = (radius + 25) * (radius + 25);
                foreach (var coop in life.Zone.Coops)
                {
                    float d = (new Vector2(coop.door.x, coop.door.z) - center).sqrMagnitude;
                    if (d < best) { best = d; c.Coop = coop; }
                }
                if (c.Coop != null) { c.fedSeen = c.Coop.FedAt; if (!c.Coop.Open || WorldClock.Between(c.RoostHour, 6)) c.Roost(); }
            }
            return c;
        }
        /// <summary>Each hen heads in on her own around dusk (staggered over most of an hour).</summary>
        float RoostHour { get { return 18.6f + (seed % 10) * .09f; } }
        public void Shoo() { if (Coop == null || state == State.Roost || state == State.Return) return; state = State.Return; target = Coop.door; }
        void Roost()
        {
            state = State.Roost; foreach (var r in rends) r.enabled = false;
            if (Coop != null) transform.position = Coop.door;
            until = Time.time + 1 + (seed % 10) * .8f;   // hens come out one by one in the morning
        }
        /// <summary>Coop routine; returns true when it has handled this frame.</summary>
        bool HenRoutine()
        {
            switch (state)
            {
                case State.Roost:
                    if (Coop.Open && WorldClock.Between(6, RoostHour) && Time.time > until)
                    {
                        foreach (var r in rends) r.enabled = true;
                        transform.position = Coop.door; transform.rotation = Quaternion.LookRotation(Coop.yard - Coop.door);
                        var p = PickPoint(new Vector2(Coop.yard.x, Coop.yard.z), 2.5f);
                        if (p.HasValue) { target = p.Value; state = State.Walk; } else { state = State.Idle; until = Time.time + 2; }
                    }
                    return true;
                case State.Return:
                    if (Step(1.6f))
                    {
                        if (Coop.Open) Roost();
                        else { state = State.Idle; until = Time.time + 4; }   // shut out: huddle by the door
                    }
                    return true;
            }
            if (state != State.Flee && state != State.Fly && WorldClock.Between(RoostHour, 6))
            {
                if (!Coop.Open && (transform.position - Coop.door).sqrMagnitude < 2) { Idle(); return true; }
                Shoo(); return true;
            }
            // Feed scattered: every hen in the yard comes running.
            if (Coop.Feeding && fedSeen != Coop.FedAt && state != State.Flee)
            {
                fedSeen = Coop.FedAt;
                var p = PickPoint(new Vector2(Coop.FeedSpot.x, Coop.FeedSpot.z), 1.3f);
                if (p.HasValue) { target = p.Value; state = State.Feed; }
            }
            return false;
        }
        void Build(string kind)
        {
            body = new GameObject("Body").transform; body.SetParent(transform, false);
            var r = life.R01;
            switch (kind)
            {
                case "chicken":
                    speed = .7f; fleeSpeed = 2.8f; fleeRadius = 3;
                    var plumage = r < .5f ? new Color(.92f, .9f, .84f) : new Color(.55f, .36f, .2f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .25f, 0), new Vector3(.26f, .24f, .34f), plumage);
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, .43f, .14f), Vector3.one * .13f, plumage);
                    Part(PrimitiveType.Cube, head, new Vector3(0, .6f, .1f), new Vector3(.25f, .5f, .4f), new Color(.8f, .15f, .12f));
                    Part(PrimitiveType.Cube, head, new Vector3(0, -.05f, .6f), new Vector3(.25f, .2f, .5f), new Color(.9f, .7f, .2f));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .33f, -.18f), new Vector3(.12f, .18f, .12f), plumage);
                    // Two thin yellow legs stepping in turn, each on three splayed toes.
                    var shank = new Color(.86f, .66f, .22f);
                    foreach (int s in new[] { -1, 1 }) Toes(Leg(new Vector3(s * .055f, .18f, .02f), .03f, shank, s < 0 ? 0 : .5f, 32), .05f, shank);
                    break;
                case "rabbit":
                    speed = 1.1f; fleeSpeed = 6; fleeRadius = 7;
                    var fur = Color.Lerp(new Color(.52f, .44f, .34f), new Color(.62f, .6f, .56f), r);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .18f, 0), new Vector3(.2f, .2f, .32f), fur);
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, .29f, .14f), Vector3.one * .15f, fur);
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, head, new Vector3(s * .25f, .9f, -.1f), new Vector3(.25f, .6f, .15f), fur, new Vector3(-15, 0, s * 10));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .19f, -.17f), Vector3.one * .08f, new Color(.92f, .92f, .9f));
                    // Haunches: big bent back legs bulging from the flanks, each on a long hind foot flat on the ground; small
                    // forepaws under the chest. Mid-hop the haunches kick back and the forepaws reach forward (see Legs).
                    foreach (int s in new[] { -1, 1 })
                    {
                        var haunch = Leg(new Vector3(s * .07f, .1f, -.07f), 0, fur, -1, 45);
                        Part(PrimitiveType.Sphere, haunch, new Vector3(s * .005f, .005f, -.005f), new Vector3(.075f, .13f, .15f), fur);
                        Part(PrimitiveType.Sphere, haunch, new Vector3(0, -.085f, .03f), new Vector3(.045f, .03f, .15f), fur);
                        Part(PrimitiveType.Sphere, Leg(new Vector3(s * .045f, .13f, .1f), .03f, fur, -1, -35), new Vector3(0, -.116f, .012f), new Vector3(.036f, .028f, .05f), fur);
                    }
                    break;
                case "crow":
                    speed = .5f; fleeSpeed = 7; fleeRadius = 7;
                    var black = new Color(.07f, .07f, .09f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .18f, 0), new Vector3(.16f, .15f, .3f), black);
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, .29f, .13f), Vector3.one * .11f, black);
                    Part(PrimitiveType.Cube, head, new Vector3(0, -.1f, .7f), new Vector3(.2f, .2f, .7f), new Color(.15f, .15f, .15f));
                    // Wings on shoulder pivots: folded down over the flanks on the ground (FoldWings), spread wide and beating
                    // in flight (thin slivers flapping about the body's own axis made a flying crow look perched on thin air).
                    foreach (int s in new[] { -1, 1 })
                    {
                        var wing = new GameObject("Wing").transform; wing.SetParent(body, false); wing.localPosition = new Vector3(s * .07f, .23f, .03f);
                        Part(PrimitiveType.Cube, wing, new Vector3(s * .17f, 0, -.02f), new Vector3(.34f, .02f, .15f), black);
                        if (s < 0) wingL = wing; else wingR = wing;
                    }
                    FoldWings();
                    Part(PrimitiveType.Cube, body, new Vector3(0, .19f, -.2f), new Vector3(.1f, .02f, .16f), black);
                    // Two thin dark legs on splayed toes; tucked back under the tail in flight (see Fly).
                    var claw = new Color(.13f, .13f, .14f);
                    foreach (int s in new[] { -1, 1 }) Toes(Leg(new Vector3(s * .035f, .13f, .01f), .02f, claw, s < 0 ? 0 : .5f, 30), .036f, claw);
                    break;
                case "deer":
                    speed = 1.2f; fleeSpeed = 7.5f; fleeRadius = 16;
                    var hide = new Color(.5f, .34f, .2f);
                    Part(PrimitiveType.Capsule, body, new Vector3(0, 1f, 0), new Vector3(.45f, .6f, .45f), hide, new Vector3(90, 0, 0));
                    // Hips tucked up inside the body so the leg tops stay hidden as they swing; diagonal pairs step together.
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Leg(new Vector3(sx * .15f, .92f, sz * .42f), .08f, hide * .8f, sx == sz ? 0 : .5f, 22);
                    var neck = Part(PrimitiveType.Capsule, body, new Vector3(0, 1.35f, .55f), new Vector3(.18f, .32f, .18f), hide, new Vector3(35, 0, 0));
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, 1.62f, .78f), new Vector3(.2f, .2f, .32f), hide);
                    if (r < .5f) foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, head, new Vector3(s * .5f, 1.4f, -.3f), new Vector3(.12f, .9f, .12f), new Color(.7f, .62f, .5f), new Vector3(-10, 0, s * 25));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, 1.05f, -.62f), Vector3.one * .14f, new Color(.9f, .88f, .82f));
                    break;
                case "sheep":
                    speed = .5f; fleeSpeed = 2.4f; fleeRadius = 3;
                    var wool = Color.Lerp(new Color(.9f, .88f, .82f), new Color(.8f, .77f, .7f), r);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .62f, 0), new Vector3(.75f, .6f, 1f), wool);
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, .7f, .55f), new Vector3(.25f, .28f, .32f), new Color(.12f, .11f, .1f));
                    // Hips up inside the fleece (the old legs stopped just short of it at the corners).
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Leg(new Vector3(sx * .2f, .47f, sz * .28f), .08f, new Color(.12f, .11f, .1f), sx == sz ? 0 : .5f, 20);
                    break;
                default: // cat
                    speed = .9f; fleeSpeed = 4.5f; fleeRadius = 4;
                    var coat = r < .33f ? new Color(.15f, .14f, .13f) : r < .66f ? new Color(.75f, .45f, .2f) : new Color(.55f, .55f, .55f);
                    Part(PrimitiveType.Capsule, body, new Vector3(0, .26f, 0), new Vector3(.18f, .22f, .18f), coat, new Vector3(90, 0, 0));
                    head = Part(PrimitiveType.Sphere, body, new Vector3(0, .38f, .22f), Vector3.one * .15f, coat);
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, head, new Vector3(s * .28f, .45f, 0), new Vector3(.2f, .3f, .1f), coat, new Vector3(0, 0, s * 15));
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .38f, -.28f), new Vector3(.04f, .2f, .04f), coat, new Vector3(-50, 0, 0));
                    // Four slim legs (diagonal pairs step together) on small paws; some cats wear white socks.
                    var paw = seed % 10 < 3 ? new Color(.9f, .88f, .82f) : Color.Lerp(coat, Color.white, .12f);
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                        Part(PrimitiveType.Sphere, Leg(new Vector3(sx * .055f, .22f, sz * .14f), .05f, coat, sx == sz ? 0 : .5f, 28), new Vector3(0, -.202f, .015f), new Vector3(.065f, .036f, .085f), paw);
                    break;
            }
        }
        Vector3? PickPoint(Vector2 around, float range)
        {
            for (int i = 0; i < 6; i++)
            {
                var p = life.Zone.Ground(around + new Vector2(life.R01 - .5f, life.R01 - .5f) * range * 2);
                if (NavMesh.SamplePosition(p, out var hit, 2, NavMesh.AllAreas) && !NavMesh.Raycast(transform.position, hit.position, out _, NavMesh.AllAreas)
                    && !life.Zone.WaterAt(new Vector2(hit.position.x, hit.position.z), out _, out _) && !CrossesWater(transform.position, hit.position)) return hit.position;
            }
            return null;
        }
        void Update()
        {
            if (life == null || life.Session.Player == null) return;
            if (Coop != null && HenRoutine()) return;
            var me = transform.position; var player = life.Session.Player.transform.position;
            float playerDist = Vector3.Distance(new Vector3(me.x, 0, me.z), new Vector3(player.x, 0, player.z));
            if (state != State.Flee && state != State.Fly && playerDist < fleeRadius)
            {
                var away = me - player; away.y = 0; away = away.sqrMagnitude > .01f ? away.normalized : transform.forward;
                if (Kind == "crow")
                {
                    // Fly off and land somewhere dry (try a few distances and headings; failing that, come back down here).
                    state = State.Fly; flyFrom = me; flyT = 0; target = me;
                    for (int tries = 0; tries < 8; tries++)
                    {
                        var dir2 = Quaternion.Euler(0, (tries % 2 == 0 ? 1 : -1) * tries * 20, 0) * away;
                        var land = new Vector2(me.x, me.z) + new Vector2(dir2.x, dir2.z) * (18 + life.R01 * 12);
                        if (!life.Zone.WaterAt(land, out _, out _) && Mathf.Abs(land.x) < life.Zone.Half - 4 && Mathf.Abs(land.y) < life.Zone.Half - 4) { target = life.Zone.Ground(land); break; }
                    }
                }
                else { var p = PickPoint(new Vector2(me.x + away.x * 10, me.z + away.z * 10), 4); if (p.HasValue) { target = p.Value; state = State.Flee; } }
            }
            switch (state)
            {
                case State.Idle:
                    Idle();
                    if (Time.time > until)
                    {
                        // While there's feed down, hens stay pecking round the trough.
                        bool feeding = Coop != null && Coop.Feeding;
                        var p = feeding ? PickPoint(new Vector2(Coop.FeedSpot.x, Coop.FeedSpot.z), 1.5f) : PickPoint(home, radius);
                        if (p.HasValue) { target = p.Value; state = State.Walk; } else until = Time.time + 2;
                    }
                    break;
                case State.Walk: case State.Flee: case State.Feed:
                    if (Step(state == State.Flee ? fleeSpeed : state == State.Feed ? 2.3f : speed)) { state = State.Idle; until = Time.time + 1.5f + life.R01 * 6; }
                    break;
                case State.Fly: Fly(); break;
            }
            if (state != State.Fly && wingsSpread) FoldWings();   // any way down that skips Fly's landing still folds them
        }
        /// <summary>Does the straight walk from a to b dip into water anywhere (checked every metre)?</summary>
        bool CrossesWater(Vector3 a, Vector3 b)
        {
            int n = Mathf.CeilToInt(Vector3.Distance(a, b));
            for (int i = 1; i <= n; i++) { var p = Vector3.Lerp(a, b, i / (float)n); if (life.Zone.WaterAt(new Vector2(p.x, p.z), out _, out _)) return true; }
            return false;
        }
        /// <summary>Moves along the ground toward the target; returns true on arrival.</summary>
        bool Step(float v)
        {
            var me = transform.position; var to = target - me; to.y = 0;
            if (to.magnitude < .3f) return true;
            var dir = to.normalized; var next = me + dir * v * Time.deltaTime;
            if (life.Zone.WaterAt(new Vector2(next.x, next.z), out _, out _)) return true;   // stop at the water's edge
            next.y = life.Zone.HeightAt(next.x, next.z);
            transform.position = next; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8);
            // Stride matched to the ground speed (capped, so a bolt is quick legs rather than a blur); a rabbit hops,
            // one hop each half-turn, longer and higher as it speeds up.
            phase += Time.deltaTime * (Kind == "rabbit" ? Mathf.Min(v, 2.2f) * 5.5f : Mathf.Min(v * stepRate, 24));
            if (Kind == "rabbit") body.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(.16f, .28f, (v - 1) / 5), 0);
            Legs(1);
            if (head != null && Kind != "deer") head.localEulerAngles = Vector3.zero;
            return false;
        }
        /// <summary>
        /// Legs: swung in the gait while stepping (easing in and out), tucked back under the tail in flight. Walkers dip as
        /// their legs spread so the planted feet stay on the ground; a rabbit stretches out mid-hop and pitches nose up, then down.
        /// </summary>
        void Legs(float moving, bool flying = false)
        {
            stride = Mathf.MoveTowards(stride, moving, Time.deltaTime * 5); tuck = Mathf.MoveTowards(tuck, flying ? 1 : 0, Time.deltaTime * 4);
            if (stride == 0 && tuck == 0) { if (atRest) return; atRest = true; } else atRest = false;
            foreach (var l in legs)
                l.hip.localEulerAngles = new Vector3((l.lag < 0 ? Mathf.Abs(Mathf.Sin(phase)) : Mathf.Sin(phase + l.lag)) * l.amp * stride + tuck * 75, 0, 0);
            if (Kind == "rabbit") body.localEulerAngles = new Vector3(-Mathf.Sin(phase * 2) * 9 * stride, 0, 0);
            else if (legs.Count > 0) body.localPosition = new Vector3(0, -legLen * (1 - Mathf.Cos(Mathf.Sin(phase) * legs[0].amp * stride * Mathf.Deg2Rad)), 0);
        }
        void Idle()
        {
            body.localPosition = Vector3.zero; Legs(0);
            if (head == null) return;
            float t = Time.time + seed;
            // Chickens peck, sheep and deer graze (head down), rabbits and cats look around.
            if (Kind == "chicken" || Kind == "crow") head.localEulerAngles = new Vector3(Mathf.Max(0, Mathf.Sin(t * 5)) * 55, 0, 0);
            else if (Kind == "sheep" || Kind == "deer") head.localEulerAngles = new Vector3(Mathf.Sin(t * .4f) > 0 ? 40 : 0, 0, 0);
            else head.localEulerAngles = new Vector3(0, Mathf.Sin(t * .8f) * 35, 0);
        }
        void Fly()
        {
            flyT += Time.deltaTime / 3.2f;
            var flat = Vector3.Lerp(flyFrom, target, Mathf.SmoothStep(0, 1, flyT));
            float height = Mathf.Sin(Mathf.Clamp01(flyT) * Mathf.PI) * 9;
            transform.position = flat + Vector3.up * height;
            var dir = target - flyFrom; dir.y = 0; if (dir.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(dir);
            // Beat up to height, glide the middle stretch on spread wings (a shallow V), beat again to land.
            float flap = flyT > .35f && flyT < .7f ? Mathf.Sin(Time.time * 5) * 8 - 6 : Mathf.Sin(Time.time * 22) * 60;
            if (wingL != null) { wingsSpread = true; wingL.localEulerAngles = new Vector3(0, 0, flap); wingR.localEulerAngles = new Vector3(0, 0, -flap); }
            Legs(0, flyT < .85f);   // tucked from take-off; let down again for the landing
            if (flyT >= 1)
            {
                state = State.Idle; until = Time.time + 3 + life.R01 * 5;
                transform.position = life.Zone.Ground(new Vector2(target.x, target.z));
                FoldWings();
            }
        }
        /// <summary>
        /// Wings folded (on the ground, from spawn and every landing): each swept back along the body and hung down over its
        /// flank, chord near vertical with the top edge tucked in, tip a touch up over the tail. A yaw alone left them lying
        /// flat, as wide as the chord: a square with a head. <see cref="Fly"/> spreads them.
        /// </summary>
        void FoldWings()
        {
            if (wingL == null) return; wingsSpread = false;
            foreach (int s in new[] { -1, 1 })
                (s < 0 ? wingL : wingR).localRotation = Quaternion.AngleAxis(s * 80, Vector3.up) * Quaternion.AngleAxis(s * 8, Vector3.forward) * Quaternion.AngleAxis(-100, Vector3.right);
        }
    }
}
