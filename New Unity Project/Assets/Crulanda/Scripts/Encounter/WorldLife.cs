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
                case "leatherworker": case "skinner": return "tannery"; case "lumberjack": case "hunter": return "woods";
                case "herbalist": return "meadow"; case "miller": return "mill"; default: return null;
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
                default: return null;
            }
        }
        readonly Dictionary<Vector3, Vector3> faceAt = new Dictionary<Vector3, Vector3>();
        /// <summary>Where someone working at a place should look (the anvil, the counter's customers), if it is a workplace.</summary>
        public Vector3? LookFor(Vector3 at)
        {
            foreach (var kv in faceAt) if ((kv.Key - at).sqrMagnitude < 2.5f) return kv.Value;
            return null;
        }

        public void Init(EncounterSession session)
        {
            Session = session; Zone = session.Zone; Active = this;
            var z = Zone.Zone; rng = new System.Random(z.seed + 7);
            FindPlaces();
            int count = z.life.villagers;
            for (int i = 0; i < count; i++)
            {
                string role = Roles[i % Roles.Length], need = Needs(role);
                if (need != null && Places[need].Count == 0) role = i % 2 == 0 ? "farmer" : "gossip";
                var home = Homes.Count > 0 ? Homes[i % Homes.Count] : null;
                var start = RandomPlace(role == "drinker" ? "inn" : "green") ?? Zone.Ground(z.spawns.recovery);
                var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : Names;
                Villagers.Add(Villager.Spawn(this, names[i % names.Length], role, i, home, start));
            }
            // Residents: named people with a fixed place (quest givers), e.g. a hooded stranger who never leaves the inn.
            if (z.life.residents != null)
                for (int i = 0; i < z.life.residents.Length; i++)
                {
                    var r = z.life.residents[i]; if (r == null || string.IsNullOrEmpty(r.name)) continue;
                    string place = r.place ?? "green";
                    if (r.at != Vector2.zero) { place = "post:" + r.name; Places[place] = new List<Vector3>(); AddPlace(place, r.at); }   // a fixed post
                    var start = RandomPlace(place) ?? Zone.Ground(r.at != Vector2.zero ? r.at : z.spawns.recovery);
                    var v = Villager.Spawn(this, r.name, r.role ?? "stranger", 60 + i, null, start, null, place, r.title);
                    Villagers.Add(v);
                }
            // One hen-wife per coop, living in the nearest house. The coop starts open or shut for the hour.
            for (int i = 0; i < Zone.Coops.Count; i++)
            {
                var coop = Zone.Coops[i];
                coop.SetOpen(WorldClock.Between(6, 20.4f));
                ZoneDoor home = null; float best = float.MaxValue;
                foreach (var h in Homes) { float d = (h.position - coop.door).sqrMagnitude; if (d < best) { best = d; home = h; } }
                var start = NavMesh.SamplePosition(coop.yard, out var hit, 4, NavMesh.AllAreas) ? hit.position : coop.yard;
                var keeper = Villager.Spawn(this, KeeperNames[i % KeeperNames.Length], "henwife", 40 + i, home, start, coop);
                Villagers.Add(keeper);
            }
            foreach (var group in z.life.critters)
                for (int i = 0; i < group.count; i++)
                    Critters.Add(Critter.Spawn(this, group.kind, group.center, group.radius));
        }
        static readonly string[] KeeperNames = { "Goody Marl", "Hettie Brook", "Nan Pennock", "Old Sorrel" };
        public static string[] DefaultNames { get { return Names; } }
        public static string[] KeeperNamesList { get { return KeeperNames; } }
        /// <summary>Hens lay through the working day, one egg each at most; the count resets before dawn.</summary>
        void Update()
        {
            if (Time.time < nextLay) return;
            nextLay = Time.time + 20;
            foreach (var coop in Zone.Coops)
            {
                if (WorldClock.Between(4, 5)) coop.LaidToday = 0;
                if (!WorldClock.Between(7, 16.5f)) continue;
                int hens = 0; foreach (var c in Critters) if (c.Coop == coop) hens++;
                if (coop.LaidToday < hens && R01 < .35f) { coop.Eggs++; coop.LaidToday++; }
            }
        }
        float nextLay;
        public IEnumerable<Critter> HensOf(ZoneCoop coop) { foreach (var c in Critters) if (c.Coop == coop) yield return c; }
        void OnDestroy() { if (Active == this) Active = null; }
        public Villager Find(string name) { return Villagers.Find(v => v.Name == name); }

        void FindPlaces()
        {
            var z = Zone.Zone;
            foreach (var key in new[] { "field", "well", "green", "inn", "mill", "wander", "forge", "stall", "oven", "tannery", "woodpile", "woods", "meadow" }) Places[key] = new List<Vector3>();
            // Trade workplaces (exact stand points, with where to look while working).
            foreach (var w in Zone.Workplaces)
                if (NavMesh.SamplePosition(w.stand, out var wh, 1.5f, NavMesh.AllAreas)) { Places[w.kind].Add(wh.position); faceAt[wh.position] = w.look; }
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
            foreach (var d in Zone.Doors) if (!d.openable) Homes.Add(d);
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
        void AddPlace(string key, Vector2 at) { AddPlace(key, Zone.Ground(at), false); }
        void AddPlace(string key, Vector3 world, bool exact)
        {
            if (NavMesh.SamplePosition(world, out var hit, exact ? 1.2f : 3f, NavMesh.AllAreas)) Places[key].Add(hit.position);
        }
        /// <summary>A random place of a kind, avoiding spots near living enemies (villagers steer clear of the collectors).</summary>
        public Vector3? RandomPlace(string key)
        {
            if (!Places.TryGetValue(key, out var list) || list.Count == 0) return null;
            for (int tries = 0; tries < 6; tries++)
            {
                var p = list[rng.Next(list.Count)];
                if (!EnemyNear(p, 12)) return p;
            }
            return null;
        }
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
        public Villager NearestVillager(Vector3 p, float range)
        {
            Villager best = null; float bestD = range * range;
            foreach (var v in Villagers) { if (!v.Visible) continue; float d = (v.transform.position - p).sqrMagnitude; if (d < bestD) { best = v; bestD = d; } }
            return best;
        }

        // ---------- what people say ----------
        static readonly string[] Wary = {
            "Keep your voice down. The grey-coats are still about.",
            "They took the Harrow girl last week. Said the Council needed her 'resonance'.",
            "Don't stare at the collectors. They remember faces.",
            "The well's gone bitter. Iron, my gran says. Or worse.",
            "Every morning the grey is a little closer. You can hear it, if the wind stops.",
            "Mira's at the Cask. She mends what the collectors break." };
        static readonly string[] Relieved = {
            "You drove them off? Then we've a few quiet nights, at least.",
            "I'll pour you a cup at the Cask. On me.",
            "They'll send more. They always send more. But thank you.",
            "The children can play on the green again." };
        static readonly string[] Afraid = {
            "Don't linger by the gallows. It listens.",
            "The Sandthrone take what they like and laugh while they do it.",
            "Something white walks by the crypt at dusk. It leaves no footprints." };
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
            { "elder", new[] { "I remember when the east was green to the horizon.", "The oak on the green was old when my grandmother was young. It'll see the lot of us out." } } };
        public string LineFor(Villager v, bool toPlayer)
        {
            if (!toPlayer) return Chatter[rng.Next(Chatter.Length)];
            if (TradeLines.TryGetValue(v.Role, out var trade) && rng.Next(3) > 0) return trade[rng.Next(trade.Length)];
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
            return pool[rng.Next(pool.Length)];
        }
        public void Talk(Villager v)
        {
            var line = LineFor(v, true); v.Say(line, 6); v.FacePlayer();
            Session.Message(v.Name + ": " + line);
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
        /// <summary>Trade shown under the name (&lt;Blacksmith&gt;), or null.</summary>
        public string Title { get; private set; }
        public string Bubble { get; private set; }
        public float BubbleUntil { get; private set; }
        public bool Visible { get { return state != State.Hidden; } }
        /// <summary>The coop a hen-wife keeps (null for everyone else).</summary>
        public ZoneCoop Coop { get; private set; }
        public string Activity { get { return activity; } }
        public bool Carrying { get { return basket != null && basket.activeSelf; } }
        VillageLife life; NavMeshAgent agent; ActorVisual visual; ZoneDoor home; Renderer[] renderers;
        State state; string activity; float until, calmSince, nextBark, nextChatter, bedAt, wakeAt, nextHerd;
        Villager partner; GameObject basket; string fixedPlace;
        /// <summary>Night: everyone goes home to bed (staggered; drinkers stay at the inn late, children go early).</summary>
        public bool Bedtime { get { return WorldClock.Between(bedAt, wakeAt); } }

        public static Villager Spawn(VillageLife life, string name, string role, int index, ZoneDoor home, Vector3 at, ZoneCoop coop = null, string fixedPlace = null, string title = null)
        {
            var go = new GameObject(name); go.SetActive(false); go.transform.position = at + Vector3.up;
            go.transform.SetParent(life.transform, false); go.transform.position = at + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            var v = go.AddComponent<Villager>(); v.life = life; v.Name = name; v.Role = role; v.home = home;
            v.agent = go.AddComponent<NavMeshAgent>(); v.agent.speed = role == "child" ? 2.4f : role == "elder" ? 1.1f : 1.6f;
            v.agent.angularSpeed = 360; v.agent.acceleration = 8; v.agent.stoppingDistance = .4f; v.agent.radius = .3f; v.agent.height = 2;
            v.agent.baseOffset = role == "child" ? .66f : 1; v.agent.avoidancePriority = 60;
            v.visual = ActorVisual.Attach(go, ActorLook.Villager, index * 5 + 3, role == "child", role);
            v.Title = title ?? VillageLife.TitleFor(role); v.fixedPlace = fixedPlace;
            v.Coop = coop;
            v.bedAt = role == "drinker" ? 23 + (index % 3) * .3f : role == "child" ? 19.8f + (index % 3) * .2f : role == "henwife" ? HerdHour : 20.2f + (index % 5) * .25f;
            v.wakeAt = role == "drinker" ? 7.5f : role == "henwife" ? 5.7f : 5.8f + (index % 5) * .3f;
            if (fixedPlace != null) v.bedAt = v.wakeAt = 0;   // residents keep their post day and night
            if (coop != null)
            {
                // Egg basket, carried on the arm after collecting from the nest boxes.
                v.basket = new GameObject("Egg basket"); v.basket.transform.SetParent(go.transform, false); v.basket.transform.localPosition = new Vector3(.42f, -.1f, .15f);
                var weave = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(weave.GetComponent<Collider>());
                weave.transform.SetParent(v.basket.transform, false); weave.transform.localScale = new Vector3(.34f, .1f, .26f);
                weave.GetComponent<Renderer>().material.color = new Color(.55f, .4f, .22f);
                for (int i = 0; i < 3; i++)
                {
                    var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(egg.GetComponent<Collider>());
                    egg.transform.SetParent(v.basket.transform, false); egg.transform.localPosition = new Vector3((i - 1) * .08f, .1f, (i % 2) * .05f); egg.transform.localScale = new Vector3(.07f, .09f, .07f);
                    egg.GetComponent<Renderer>().material.color = i == 1 ? new Color(.78f, .6f, .42f) : new Color(.95f, .92f, .85f);
                }
                v.basket.SetActive(false);
            }
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
            agent.speed = (Role == "child" ? 2.4f : Role == "elder" ? 1.1f : 1.6f) * speedScale;
            agent.isStopped = false; agent.SetDestination(target); state = State.Travel; visual.Pose = ActorPose.None;
        }
        void ChooseNext()
        {
            if (Coop != null) { KeeperNext(); return; }
            if (Bedtime)
            {
                activity = "sleep";
                if (home != null) { Go(home.position); return; }
                var bed = life.RandomPlace("inn");   // no house: a bench at the inn
                if (bed.HasValue) Go(bed.Value); else { state = State.Activity; until = Time.time + 30; }
                return;
            }
            // Weighted by role; each option is a kind of place.
            string[] options;
            if (fixedPlace != null) { activity = fixedPlace; var spot = life.RandomPlace(fixedPlace); if (spot.HasValue) Go(spot.Value); else { state = State.Activity; until = Time.time + 20; } return; }
            switch (Role)
            {
                case "farmer": options = new[] { "field", "field", "field", "field", "well", "green", "green", "inn", "home" }; break;
                case "gossip": options = new[] { "green", "green", "green", "well", "well", "inn", "wander", "home" }; break;
                case "drinker": options = new[] { "inn", "inn", "inn", "inn", "green", "wander" }; break;
                case "child": options = new[] { "green", "green", "wander", "wander", "well" }; break;
                case "elder": options = new[] { "inn", "inn", "green", "green", "home" }; break;
                case "miller": options = new[] { "mill", "mill", "mill", "inn", "green" }; break;
                case "blacksmith": options = new[] { "forge", "forge", "forge", "forge", "forge", "well", "inn" }; break;
                case "merchant": options = new[] { "stall", "stall", "stall", "stall", "green", "inn" }; break;
                case "baker": options = new[] { "oven", "oven", "oven", "oven", "stall", "well" }; break;
                case "leatherworker": options = new[] { "tannery", "tannery", "tannery", "stall", "green" }; break;
                case "skinner": options = new[] { "tannery", "tannery", "tannery", "woods", "well" }; break;
                case "lumberjack": options = new[] { "woods", "woods", "woods", "woodpile", "woodpile", "inn" }; break;
                case "hunter": options = new[] { "woods", "woods", "woods", "meadow", "tannery", "inn" }; break;
                case "herbalist": options = new[] { "meadow", "meadow", "meadow", "woods", "stall", "green" }; break;
                default: options = new[] { "wander", "green" }; break;
            }
            activity = options[life.Next(options.Length)];
            Vector3? target = activity == "home" && home != null ? home.position : life.RandomPlace(activity);
            if (!target.HasValue) { activity = "wander"; target = life.RandomPlace("wander"); }
            if (target.HasValue) Go(target.Value); else { state = State.Activity; until = Time.time + 5; }
        }
        void StartActivity()
        {
            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            until = Time.time + (activity == "home" ? 20 : 14 + life.R01 * 30);
            visual.Pose = PoseFor(activity);
            var look = life.LookFor(transform.position); if (look.HasValue) Face(look.Value);
            if (Coop != null) KeeperStart();
            if ((activity == "home" || activity == "sleep") && home != null) Hide();
        }
        /// <summary>Capture/debug: go straight to a place of this kind and work there for a minute.</summary>
        public bool WorkAt(string place)
        {
            if (!life.Places.TryGetValue(place, out var list) || list.Count == 0 || !agent.isOnNavMesh) return false;
            foreach (var r in renderers) r.enabled = true;
            agent.Warp(list[0]); activity = place; StartActivity(); until = Time.time + 60; return true;
        }
        /// <summary>Capture/debug: stop and stand still at a spot, facing a direction (for line-up shots).</summary>
        public void StandAt(Vector3 at, float yaw)
        {
            foreach (var r in renderers) r.enabled = true;
            if (agent.isOnNavMesh) { agent.Warp(at); agent.isStopped = true; }
            transform.rotation = Quaternion.Euler(0, yaw, 0); visual.Pose = ActorPose.None; state = State.Activity; activity = "posed"; until = Time.time + 60; enabled = false;
        }
        /// <summary>Capture/debug: out of sight and out of the way (no body, no nameplate) until <see cref="Release"/>.</summary>
        public void Park()
        {
            state = State.Hidden; Bubble = null; foreach (var r in renderers) r.enabled = false;
            if (basket != null) basket.SetActive(false);   // not left hanging in the air where she stood
            if (agent.isOnNavMesh) agent.isStopped = true; enabled = false;
        }
        /// <summary>Capture/debug: back to the day's routine after <see cref="StandAt"/> or <see cref="Park"/>, starting from where they were.</summary>
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
                case "oven": case "tannery": return ActorPose.Knead;
                case "woodpile": return ActorPose.Chop;
                case "woods": return Role == "lumberjack" ? ActorPose.Chop : Role == "hunter" || Role == "warden" ? ActorPose.None : ActorPose.Gather;
                case "meadow": return Role == "hunter" ? ActorPose.None : ActorPose.Gather;
                default: return ActorPose.None;
            }
        }
        void Hide()
        {
            state = State.Hidden; calmSince = Time.time; foreach (var r in renderers) r.enabled = false;
            if (basket != null) basket.SetActive(false);   // eggs put away in the pantry
            if (agent.isOnNavMesh) agent.isStopped = true; Bubble = null;
        }
        void Emerge()
        {
            foreach (var r in renderers) r.enabled = true;
            if (home != null && agent.isOnNavMesh && NavMesh.SamplePosition(home.position, out var hit, 2, NavMesh.AllAreas)) agent.Warp(hit.position);
            if (activity == "fled") Say(life.EnemiesCleared ? "Is it over? ...Gods be thanked." : "Is it quiet? Is it safe?", 4);
            else if (activity == "sleep" && life.R01 < .3f) Say(life.R01 < .5f ? "Another grey morning." : "Morning. Still here, then.", 4);
            ChooseNext();
        }

        // ---------- the hen-wife ----------
        /// <summary>Hens start heading in on their own from 18:36; she sweeps up the stragglers from 19:18.</summary>
        const float HerdHour = 19.3f;
        static readonly string[] HerdLines ={ "Come on, you daft bird!", "In you go. In!", "Shoo, shoo! Bedtime.", "Not you again, Speckle." };
        /// <summary>
        /// Her day: open the coop at first light; scatter feed morning and afternoon; collect eggs from the nest
        /// boxes and carry them home; potter in the yard. From dusk she herds stragglers in, shuts the door, and goes to bed.
        /// </summary>
        void KeeperNext()
        {
            if (Coop.Open && WorldClock.Between(HerdHour, 5.5f)) { activity = "herd"; Go(Coop.yard, 1.2f); return; }
            if (Bedtime) { activity = "sleep"; if (home != null) Go(home.position); else { state = State.Activity; until = Time.time + 30; } return; }
            if (!Coop.Open) { activity = "open"; Go(Coop.door); return; }
            if (Carrying && home != null) { activity = "home"; Go(home.position); return; }
            if (Time.time - Coop.FedAt > 300 && WorldClock.Between(6, 17.5f)) { activity = "feed"; Go(Coop.trough); return; }
            if (Coop.Eggs >= 3 || (Coop.Eggs > 0 && WorldClock.Between(15, 18.6f))) { activity = "eggs"; Go(Coop.nest); return; }
            // Towards dusk she stays by the coop and watches them go up the ramp.
            var options = WorldClock.Between(18, HerdHour) ? new[] { "yard" } : new[] { "yard", "yard", "yard", "well", "green", "home" };
            activity = options[life.Next(options.Length)];
            Vector3? target = activity == "yard" ? Around(Coop.yard, 3) : activity == "home" && home != null ? home.position : life.RandomPlace(activity);
            if (target.HasValue) Go(target.Value); else { activity = "yard"; state = State.Activity; until = Time.time + 8; }
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
                case "feed":
                    Coop.FedAt = Time.time; Coop.FeedSpot = Coop.trough; visual.Pose = ActorPose.Work; until = Time.time + 16;
                    Say("Chook-chook-chook-chook!", 4); break;
                case "eggs":
                {
                    int n = Coop.Eggs; Coop.Eggs = 0; visual.Pose = ActorPose.Work; until = Time.time + 8; Face(Coop.door + (Coop.nest - Coop.door) * 2);
                    if (basket != null) basket.SetActive(n > 0);
                    Say(n >= 5 ? n + " eggs. Good girls." : n >= 2 ? n + " eggs. They're off-lay; it's the grey, I'd wager." : "Just the one. Well. It's something.", 5);
                    break;
                }
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
                else if (Coop != null && Interrupted) Emerge();   // hens before anything else at dusk
                else if (Time.time - calmSince > (activity == "home" ? until - calmSince : 12)) Emerge();
                return;
            }
            if (state == State.Flee) { if (Arrived || Stranded) { activity = "fled"; Hide(); } return; }
            if (danger && Role != "drinker" || danger && life.EnemyNear(transform.position, 10)) { Flee(); return; }
            // Talking with the player: stand still and face them until the conversation ends, then carry on.
            if (Attending)
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
            if (state == State.Travel && Stranded) { partner = null; activity = "wander"; var alt = life.RandomPlace("wander"); if (alt.HasValue) Go(alt.Value); else { state = State.Activity; until = Time.time + 5; } return; }
            if (state == State.Travel && Arrived) StartActivity();
            if (state == State.Activity)
            {
                if (activity == "green" || activity == "well") Chat();
                if (activity == "herd") Herd();
                else if (Time.time > until) { partner = null; ChooseNext(); }
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
        Transform body, head, wingL, wingR; float phase, seed; Renderer[] rends;
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
                    // Wings on shoulder pivots: folded back along the body on the ground, spread wide and beating in flight
                    // (thin slivers flapping about the body's own axis made a flying crow look perched on thin air).
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
            if (wingL != null) { wingL.localEulerAngles = new Vector3(0, 0, flap); wingR.localEulerAngles = new Vector3(0, 0, -flap); }
            Legs(0, flyT < .85f);   // tucked from take-off; let down again for the landing
            if (flyT >= 1)
            {
                state = State.Idle; until = Time.time + 3 + life.R01 * 5;
                transform.position = life.Zone.Ground(new Vector2(target.x, target.z));
                FoldWings();
            }
        }
        /// <summary>Wings folded back along the body (on the ground); <see cref="Fly"/> spreads them.</summary>
        void FoldWings() { if (wingL == null) return; wingL.localEulerAngles = new Vector3(0, -80, 0); wingR.localEulerAngles = new Vector3(0, 80, 0); }
    }
}
