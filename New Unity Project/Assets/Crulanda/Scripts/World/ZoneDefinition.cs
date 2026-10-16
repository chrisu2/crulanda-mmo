using System;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A playable zone described as data (EncounterContent/Zones/*.json) and generated at runtime by ZoneBuilder.
    /// Coordinates are metres on the ground plane: x = east, z = north, origin at the zone centre.
    /// Lore status of every zone is recorded in <see cref="canonStatus"/>; game-only additions must say so.
    /// </summary>
    [Serializable] public sealed class ZoneDefinition
    {
        public string id, displayName, subtitle, canonStatus, canonNote;
        /// <summary>A dungeon (the Sealed Adit): a zone of caves you go into from another zone; the sims never wander into it on their own.</summary>
        public bool dungeon;
        /// <summary>A raid (none built yet; Chris, 2026-10-15: "raids once per week"): its bosses and elites lock for a week (the lockout
        /// table, Encounter.Lockouts), not a dungeon's two days.</summary>
        public bool raid;
        /// <summary>A group zone (the Verdant Shore, round 29; Chris: "all epic mobs, group required"): every camp mob has an elite's
        /// health and blows (EncounterEnemy.Tough), though only the named ones are elites.</summary>
        public bool groupZone;
        public float size = 140;            // square side length
        public float flatRadius = 50;       // village ground stays flat inside this radius
        public float hillHeight = 3;        // rolling ground outside it
        public int seed = 1;
        /// <summary>
        /// A big zone's outer country: from wildFrom metres out from the middle (the larger of |x| and |z|; 0: nowhere) the open
        /// ground's grass thins, down to wildDensity of the full meadow at the edge, so the tufts to sow (load time, memory) grow
        /// less than the zone's area. Tall grass patches keep their density (an ambusher lies in them).
        /// </summary>
        public float wildFrom, wildDensity = 1;
        public ZoneLighting lighting = new ZoneLighting();
        public ZoneSpawns spawns = new ZoneSpawns();
        public ZonePath[] roads = new ZonePath[0];
        public ZonePath[] water = new ZonePath[0];
        public ZoneRect[] fields = new ZoneRect[0];
        public ZoneCircle[] clearings = new ZoneCircle[0];
        public ZoneProp[] props = new ZoneProp[0];
        public ZoneWasting wasting;         // only when the JSON has a "wasting" block (ZoneBuilder.ParseZone)
        public ZoneLabel[] landmarks = new ZoneLabel[0];
        public string[] objectives = new string[0];
        public ZoneExit[] exits = new ZoneExit[0];
        public ZoneGrove[] groves = new ZoneGrove[0];
        /// <summary>The wild woods round the village (ZoneBuilder.Woods.cs); cover 0 (the default): none.</summary>
        public ZoneWoods woods = new ZoneWoods();
        public string waterTint;   // optional hex, e.g. dark Gloom Creek
        public float waterReflect; // optional sky reflection face-on 0..1 (0 = the water material's own); murky water ~.3
        /// <summary>
        /// The zone's weather: kinds with weights, calmest first (clear, fair, windy, overcast, mist, rain, flurries, ashsquall,
        /// storm). Empty: the biome's default. Spells of weather follow each other one step of severity at a time (WorldWeather).
        /// </summary>
        public ZoneWeather[] weather = new ZoneWeather[0];
        /// <summary>Hidden finds (ZoneSecret): on no map; found by walking onto a lookout or searching a hidden spot.</summary>
        public ZoneSecret[] secrets = new ZoneSecret[0];
        /// <summary>Things to gather that are not props (ore seams, windfalls, herbs; see <see cref="ZoneNode"/>). Herb props carry their own <see cref="ZoneProp.node"/>.</summary>
        public ZoneNode[] nodes = new ZoneNode[0];
        /// <summary>Where the trades' recipes are made that is no villager's workplace (a field anvil, a bench, a cookfire; see
        /// <see cref="ZoneStation"/>). A smithy, a bake oven, the herbalist's drying hut, an inn's kitchen and its hearth are stations already.</summary>
        public ZoneStation[] stations = new ZoneStation[0];
        public ZoneLife life;
        /// <summary>Level band shown on maps and exits (e.g. 1-2); camps spawn inside it.</summary>
        public int levelMin = 1, levelMax = 2;
        /// <summary>
        /// Ground palette: meadow (green farmland), mountain (rock, scree, sparse grass), ash (grey cracked ash), gloom (Khaven:
        /// hard grey-brown earth, thin dry grass and no flowers, grey dead woods, withered pines and brush, a drained dusk grade).
        /// </summary>
        public string biome = "meadow";
        /// <summary>Mob camps for levelling: packs that respawn after they are killed. Not saved; they always return.</summary>
        public ZoneCamp[] camps = new ZoneCamp[0];
        public ZoneLake[] lakes = new ZoneLake[0];
        /// <summary>Patches of tall, dense grass (ambush camps get their own automatically).</summary>
        public ZoneCircle[] tallGrass = new ZoneCircle[0];
        /// <summary>Landmark ground: level pads, raised perches and sunk basins (see <see cref="ZoneShape"/>).</summary>
        public ZoneShape[] shapes = new ZoneShape[0];
        /// <summary>Pin on the overworld map, normalised 0..1 from the image's top-left corner.</summary>
        public Vector2 worldMapPosition = new Vector2(-1, -1);
        public string worldMapNote;
        // ---- the works in a dungeon's caves (the Adit rework, Docs/ADIT_REWORK.md; all GAME-ONLY, all optional: a zone without them builds as it did) ----
        /// <summary>Walkable decks in caves: trestles, catwalks, rope bridges, platforms, islands, scree ramps (<see cref="ZoneDeck"/>; ZoneBuilder.AditDecks).</summary>
        public ZoneDeck[] decks = new ZoneDeck[0];
        /// <summary>One-way drops off a ledge or a deck into the cave below (<see cref="ZoneDrop"/>).</summary>
        public ZoneDrop[] drops = new ZoneDrop[0];
        /// <summary>Pools, streams and flooded stretches in caves (<see cref="ZoneCaveWater"/>; ZoneBuilder.AditWater).</summary>
        public ZoneCaveWater[] caveWater = new ZoneCaveWater[0];
        /// <summary>Gates across caves' ways (<see cref="ZoneGateDef"/>; built as ZoneGate by ZoneBuilder.Adit).</summary>
        public ZoneGateDef[] gates = new ZoneGateDef[0];
        /// <summary>Named places in caves (<see cref="ZoneAnchor"/>): where a run regroups, where a wave comes from, a chamber's middle.</summary>
        public ZoneAnchor[] anchors = new ZoneAnchor[0];
        /// <summary>A dungeon's wings, in the order they open (<see cref="ZoneWing"/>; the run a sim leads).</summary>
        public ZoneWing[] wings = new ZoneWing[0];
        /// <summary>A dungeon's events (<see cref="ZoneEvent"/>: the Wake).</summary>
        public ZoneEvent[] events = new ZoneEvent[0];
        /// <summary>Trails of living lichen along the run's way (<see cref="ZoneCrumb"/>).</summary>
        public ZoneCrumb[] crumbs = new ZoneCrumb[0];
    }
    /// <summary>
    /// Ambient life. villagers: how many named villagers live here (homes are the zone's barred houses, given by households).
    /// mood: "wary" (collectors about; brightens once they are gone) or "afraid". Critters never fight; they flee.
    /// </summary>
    [Serializable] public sealed class ZoneLife
    {
        public int villagers; public string mood = "wary";
        public ZoneCritters[] critters = new ZoneCritters[0];
        /// <summary>Villager names for this zone, in order. Omitted: the default (Oakhaven) list.</summary>
        public string[] names = new string[0];
        /// <summary>Named extra people with a fixed role and place (quest givers such as a stranger at the inn).</summary>
        public ZoneResident[] residents = new ZoneResident[0];
        /// <summary>Who lives behind which door: one named house per household, families sharing. Empty: villager i takes the
        /// zone's house i while houses last, the rest lodge at the inn, and a hen-wife joins the house nearest her coop.</summary>
        public ZoneHousehold[] households = new ZoneHousehold[0];
        /// <summary>Whose workshop a prop is (a trade then works its own rather than any of the kind).</summary>
        public ZoneWorkshop[] workshops = new ZoneWorkshop[0];
        /// <summary>Stories of the villagers who are not residents (the named folk of names), by name; a resident's go on the resident.
        /// Said one time in three when spoken to (ZoneLore.Story; 2026-10-15, Chris: "stories from npcs").</summary>
        public ZoneStory[] stories = new ZoneStory[0];
    }
    /// <summary>
    /// role: any villager role, or stranger (keeps to one place). place: a place kind (inn, green, woods...).
    /// title: the nameplate subtitle. works: an ordinary villager with the role's working day and a home (an innkeeper), not
    /// one who keeps a post day and night. stories: two or three lines of their own, memories, grudges and hopes tied to the
    /// canon (GAME-ONLY lines about CANON things), said one time in three when spoken to (ZoneLore.Story).
    /// </summary>
    [Serializable] public sealed class ZoneResident { public string name, role, title, place, look; public Vector2 at; public bool works; public string[] stories = new string[0]; }
    /// <summary>A named person's stories (<see cref="ZoneLife.stories"/>): name as the villager is named, and their lines.</summary>
    [Serializable] public sealed class ZoneStory { public string name; public string[] lines = new string[0]; }
    /// <summary>
    /// A household (GAME-ONLY): the folk who share one house. house names a prop of kind house, mill or barn (a barn gets a door
    /// of its own) or inn (its rooms upstairs). members name spawned villagers, hen-wives and residents; kin is free text for
    /// lines ("head", "husband", "daughter"). stipend and needs are the household's purse (coin a day, what it buys in order).
    /// </summary>
    [Serializable] public sealed class ZoneHousehold
    {
        public string name, house, canonStatus; public int stipend = 6;
        public string[] needs = { "bread", "firewood", "eggs" };
        public ZoneMember[] members = new ZoneMember[0];
    }
    /// <summary>A member of a household: a villager's name and how they are kin ("head", "wife", "son", "lodger").</summary>
    [Serializable] public sealed class ZoneMember { public string name, kin; }
    /// <summary>who works at the prop named prop (its workplaces' stand points are theirs).</summary>
    [Serializable] public sealed class ZoneWorkshop { public string who, prop; }
    /// <summary>kind: chicken, rabbit, crow, deer, sheep, cat.</summary>
    [Serializable] public sealed class ZoneCritters { public string kind; public Vector2 center; public float radius = 10; public int count = 4; }
    /// <summary>
    /// A levelling camp. Holds count mobs of one kind within radius of center, each at a level between levelMin and
    /// levelMax, respawning respawn seconds after death. Mob ids are "mob.&lt;tag&gt;.&lt;zone&gt;.&lt;camp index&gt;.&lt;n&gt;", so a
    /// quest can target a kind with "mob.wolf.*".
    /// - look: collector, warden, outrider, pale, hollow, cultist, wolf, boar, weaveeater, deserter, banditking.
    /// - elite: tougher, and marked on the map.
    /// </summary>
    [Serializable] public sealed class ZoneCamp
    {
        public string name, mob, tag, look, canonStatus;
        public Vector2 center; public float radius = 10;
        public int count = 4, levelMin = 1, levelMax = 1;
        public float respawn = 75; public bool elite;
        /// <summary>Harder on purpose (a cave behind a quest, such as Crowsfoot Hollow): its levels may run up to three past the zone's
        /// band. The maps show every camp's levels in their level colour, so the player sees it coming.</summary>
        public bool harder;
        /// <summary>Lie hidden in tall grass until you come close (much closer if you sneak, holding Ctrl), then leap out.</summary>
        public bool ambush;
        /// <summary>How its mobs answer when one of them joins a fight: "pack" (those near come at once), "call" (it shouts and those
        /// in earshot come after a beat), "solitary" (nobody comes). Empty = by its look: wolves, Weave-Eaters, spiders and briars
        /// pack, boar and stags are solitary, people call (SocialAggro.KindFor in the game's code).</summary>
        public string social;
        /// <summary>On an elite's camp: the camps whose mobs are its guards and always fight beside it, by name, comma-separated
        /// ("none" for no guards). Empty = every non-elite camp whose edge is within 8 m of this camp's centre.</summary>
        public string guards;
        /// <summary>In a cave (the Sealed Adit): which cave, how far in and how far to the right of its middle the camp stands. The
        /// centre is written from these by tools/wip/dungeon/adit_layout.py; the run a sim leads goes cave by cave in this order.</summary>
        public string cave; public float along, aside;
        /// <summary>A key its elite carries (dungeon step D3: the Sealed Adit's rail sigils), taken from the body at the first kill and
        /// kept for good (EncounterProgress.keys), and its name in words ("the Amber Sigil"). Empty = none.</summary>
        public string key, keyName;
        /// <summary>A rare camp's chance to be there on a visit (0..1; the Quiet Miner, one run in five). 0 = always. A rare camp comes
        /// last in the list, so a visit without it moves no other camp's mobs, and no deed waits on it.</summary>
        public float rare;
        /// <summary>Its mobs bolt at this fraction of their health (dungeon step D5: the Grey Breach's crawlers at a quarter): a run from
        /// whoever they fight for a few seconds, then back. 0 = they fight to the end. Not for elites.</summary>
        public float flee;
        /// <summary>A patrol (playtest note 131, Chris, 2026-10-11: "should all patrol"): the points of its road, walked back and forth side
        /// by side while it has nobody to fight. Empty = its mobs stroll about the camp. The Sealed Adit's are written by adit_layout.py.</summary>
        public Vector2[] route;
        /// <summary>The Adit rework (Docs/ADIT_REWORK.md; GAME-ONLY): the wing it belongs to (<see cref="ZoneWing.id"/>) and its place in that
        /// wing's run (1, 2...; 0 for a patrol, a camp off the route or one released later).</summary>
        public string wing; public int step;
        /// <summary>What brings it (empty: it is there from the start, as every camp was): "kill:&lt;mob&gt;" (that mob's death), "event:&lt;id&gt;" (a
        /// wave of an event), "loose:&lt;mob&gt;" (a boss's phase). It is not spawned with the others, but when that happens.</summary>
        public string after;
        /// <summary>The gate (by title) the run waits at before it, "none" for no gate; empty: the last gate before it in its cave.</summary>
        public string gate;
        /// <summary>The run may leave it (a side room's camp); passive: it ignores you until struck (the Old Burrower).</summary>
        public bool optional, passive;
        /// <summary>How far above its cave's floor it stands (on a deck: a trestle's landing, a catwalk); 0 on the floor.</summary>
        public float rise;
    }
    /// <summary>Walk into the radius and press E to travel; you arrive at <see cref="arrive"/> in the other zone.</summary>
    [Serializable] public sealed class ZoneExit { public string to, name; public Vector2 at, arrive; public float radius = 5; }
    /// <summary>
    /// A hidden find: on no map, rewarded when found (a "Discovered" toast, XP, and most hold a cache). Its id is unique across
    /// zones ("secret.&lt;zone&gt;.&lt;slug&gt;") and is what the save keeps. kind is what stands there (ZoneBuilder builds it):
    /// - vista: a lookout, found by walking within radius (a cairn or nothing at all);
    /// - cache: a hidden box or bundle; note: a page or scrap tucked away; herb: a rare plant; chest: a locked chest;
    ///   key: a key hidden somewhere else, for a chest that names it in needs. These are found by searching (E, prompt).
    /// - (Phase 8.6) carving: marks cut in a stone, a post or a cave wall (a line in the book of lore); read: a book, a cairn's book
    ///   or slates (a page); grave: a named stone (a line in the book); remains: a dead adventurer (gear and a page); shrine: a
    ///   wayshrine (its blessing; after the find, for a crown). A found herb grows back a game day after it is picked.
    /// Rewards: xp, gold, an item id, a document id (a readable page, as quests give). height lifts the prop off the ground
    /// (something tucked up under a bridge or in a tree); rotation turns it. canonStatus as everywhere.
    /// clue: the Discoveries tab's line for it while it is not found, a riddle in the book's voice that points at the region and
    /// the hiding place; hint: what a villager says about it, in theirs (2026-10-15, Chris: "still havent found many secrets";
    /// Phase 8.6 3e). Neither names a landmark on the map (DiscoveryLog.Validate); either stands in for the other when missing.
    /// </summary>
    [Serializable] public sealed class ZoneSecret
    {
        public string id, name, kind = "cache", prompt, text, item, document, needs, canonStatus, clue, hint;
        public Vector2 at; public float radius = 2.5f, rotation, height;
        public int xp, gold;
        /// <summary>In a cave (the Adit rework): which cave, how far in, how far to the right of its middle and how far above its floor it
        /// stands (a deck, a niche over a landing). <see cref="at"/> is then written from these when the zone is built. Empty cave: at, as
        /// every secret was.</summary>
        public string cave; public float along, aside, rise;
    }
    /// <summary>A secret as built in the world (ZoneBuilder.Secrets): its data, where it stands, and its visible parts.</summary>
    public sealed class ZoneSecretSpot { public ZoneSecret def; public Vector3 position; public Transform root; }
    /// <summary>One kind of weather a zone can have and how often (a weight, relative to the zone's other kinds).</summary>
    [Serializable] public sealed class ZoneWeather { public string kind; public float weight = 1; }
    /// <summary>An area filled with trees. kind: dead (grey, leafless, darkened ground), pine, broadleaf.</summary>
    /// <summary>
    /// The wild woods (Chris, 2026-10-14, "Filling the zones in the Elwynn style"; ZoneBuilder.Woods.cs). cover: the share of the open
    /// wild that is wood (0: none). from: no woods nearer the middle than this; ramp: over the next ramp metres they thin in. spacing:
    /// one tree a cell this wide. edge: kept back from the zone's edge (its own forest ring stands there). scale: the noise's feature
    /// size in metres (glades and stands). deepAbove: the old forest (always wood, its great oaks a deepOaks share of its trees)
    /// where a slow noise is above this (1: none); oaks: the great oaks' share elsewhere (0: none); snags: dead snags' share; pineAbove:
    /// pine stands (never in the old forest) where a third noise is above this (0: all pine). oakScale: the great oaks' size (.5 about
    /// 14 m; 1 a giant of the Verdant Shore). floor: the forest floor's density. seed: varies them.
    /// </summary>
    [Serializable] public sealed class ZoneWoods
    {
        public float cover, from = 80, ramp = 60, spacing = 8, edge = 12, scale = 90, deepAbove = 1, deepOaks = .55f, oaks, oakScale = .5f, snags = .04f, pineAbove = .62f, floor = 1;
        public int seed;
    }
    [Serializable] public sealed class ZoneGrove { public string name, kind = "dead"; public Vector2 center, size; public int count = 30; }
    [Serializable] public sealed class ZoneLighting
    {
        public float sunPitch = 32, sunYaw = -40, sunIntensity = 1.1f;
        public string sunColor = "#FFDDB0", ambientSky = "#8A93A0", ambientEquator = "#6E6A5E", ambientGround = "#3C382F";
        public string fogColor = "#9A9888"; public float fogStart = 45, fogEnd = 120;
        /// <summary>
        /// Optional (0 or empty = the shared default), read by WorldClock. sunHigh: the sun's noon elevation in degrees (the
        /// default arc climbs to about 45; Khaven's stays low, so its day holds the dusk). skyTint (hex), skyExposure (default
        /// 1.05) and skyHaze (atmosphere thickness): the procedural sky's daytime look. Night falls the same everywhere.
        /// </summary>
        public float sunHigh, skyExposure, skyHaze; public string skyTint;
    }
    [Serializable] public sealed class ZoneSpawns
    {
        public Vector2 player, companion, recovery; public float playerFacing;
        public ZoneEnemy[] enemies = new ZoneEnemy[0];
        public float leash = 17;
    }
    /// <summary>look: collector, warden, outrider, pale (default: collector, or warden when veteran).</summary>
    [Serializable] public sealed class ZoneEnemy { public string id, name, look; public Vector2 at; public bool veteran; public int level; }
    /// <summary>
    /// A road or creek. depth (creeks only): the channel's depth below its banks. The water surface sits 0.42 m under the
    /// bank, so the water in the middle is depth - 0.42 deep. The default 1.1 is a wading creek (0.68 m); swimming needs
    /// about 1.9 or more. See ZoneWater.
    /// swingFrom, swingAlong (creeks only): the point its side-to-side swing is counted from, and the metres already run
    /// there. A creek lengthened upstream names its old first point, so its line downstream stays where it was
    /// (ZoneWater.Meander). 0 and 0: from the first point, as every creek was.
    /// </summary>
    [Serializable] public sealed class ZonePath { public string name; public float width = 4; public Vector2[] points = new Vector2[0]; public float depth = 1.1f; public int swingFrom; public float swingAlong; }
    /// <summary>
    /// A lake or pond. radius is the mean waterline (the shore wanders up to a fifth in and out around it); depth is the
    /// water's depth in the middle (a flat bottom, a bank under about 38 degrees, a gentle shore). Deeper than about
    /// 1.45 m, you swim. See ZoneWater.
    /// </summary>
    [Serializable] public sealed class ZoneLake { public string name; public Vector2 center; public float radius = 10, depth = 2.6f; }
    [Serializable] public sealed class ZoneRect { public string name; public Vector2 center, size; public float rotation; public string crop = "soil"; }
    [Serializable] public sealed class ZoneCircle { public string name; public Vector2 center; public float radius = 8; }
    /// <summary>
    /// Landmark ground, applied over the land (relief and cliff lifts included) before water: a level pad of radius easing
    /// back to the land over blend metres. It sits at the mean ground round its edge plus height (0: level; negative: a sunk
    /// basin); with a positive height it is a perch, that high above the lowest ground round its foot (radius + blend).
    /// paint (optional): mud (a wallow's wet ground) or unmade (the Wasting's grey, thread-veined ground); painted ground
    /// grows no grass.
    /// </summary>
    [Serializable] public sealed class ZoneShape
    {
        public string name, paint; public Vector2 center; public float radius = 8, height, blend = 6;
        [NonSerialized] public float level;   // set by ZoneBuilder.PrepareShapes
    }
    /// <summary>
    /// kind: house, inn, barn, well, dead_oak, great_oak (the living Great Oak with its bench ring), tree, pine, fence, haystack, cart, barrels, crates, lamp, grave, rock, bridge,
    /// hedge, signpost, ruin, ruined_house, wall (uses points), tower, gallows, crypt, cliff, wayshrine (a wayside shrine).
    /// Landmarks: gate (size.x the gap between two towers), keep, perch (rocks round a raised ZoneShape; size.x its radius),
    /// wallow, cave (size.x the face), shelter, brazier (variant 1: bone legs), brood, rib (size: reach to the spine, height),
    /// spine (size: length, height), shrine (the Cult of Ash's), idol, cavern (a walk-in cave in a rocky knoll, its mouth at
    /// the prop facing -Z; variant 0 is Crowsfoot Hollow's plan; give it a level ZoneShape pad under its floor, see Hollow).
    /// rotation in degrees (0 = door faces south). variant picks colour/size variations; some are looks of their own:
    /// well 1 blood-stone, inn 1 the Cracked Hearth (split, glowing chimney), inn 2 a waystation (one storey, a long table and bunks),
    /// crypt 1 a turfed barrow, grave 1 heaved over.
    /// </summary>
    [Serializable] public sealed class ZoneProp
    {
        public string kind, name; public Vector2 at; public float rotation, scale = 1; public int variant;
        public Vector2 size;   // footprint for sized props (house, barn, fence length in x)
        /// <summary>Cliff: raise the ground behind the crag (its +z side; negative, its -z side) this many metres into a shelf.</summary>
        public float lift;
        public Vector2[] points = new Vector2[0];   // polyline props (wall)
        /// <summary>Optional: makes the prop usable with E. This is the prompt, e.g. "Search the black-iron wagon".</summary>
        public string interact;
        /// <summary>
        /// Optional quest item the interaction gives, only while a quest wants it. once = true: it is emptied for good
        /// (a crate); otherwise it grows back after a while (herbs).
        /// </summary>
        public string item; public bool once;
        /// <summary>Optional: the kind of node it is to gather (a herb prop: "node.yarrow"), from the trades' content. It is then
        /// worked with the trade's skill, and still gives item while a quest wants it.</summary>
        public string node;
        /// <summary>A cavern's own passage (rows of five: x, z, half-width, height, floor drop below the mouth), instead of its variant's.</summary>
        public float[] plan;
        /// <summary>A cavern opening out of another (by name, listed before it): its mouth stands on that cave's floor, and each cave's
        /// walls are open where the other runs through them (the Sealed Adit's branches off the Gallery).</summary>
        public string within;
        /// <summary>A cavern within another: its mouth this many metres up the other's wall (0: on its floor, as every branch was). A
        /// doorway high in a hall's wall, reached by a deck or left by a drop (the Adit rework: the Foundry's doors off the Gallery).</summary>
        public float rise;
        /// <summary>A cavern whose far end opens into another cave (by name) instead of closing: its last rows stay full width, out in the
        /// other's air at the other's floor plus the lip its plan gives (a level way back round, or a ledge over a hall), and the two
        /// caves' walls are open into each other there. Empty: it closes, as every cave did.</summary>
        public string joins;
        /// <summary>How the Sealed Adit dresses a cavern of variant 2 (ZoneBuilder.Adit): "main" (the Workings, the Gallery, the stair and
        /// the Rail Hall), "geode", "vent", "breach", or any other word for a plain cave of lamps and rubble. Empty: by its name, as the
        /// first Adit's caves were. A treehouse of style "inn" is walked into: its round house has a double door and seats (the Shore's
        /// Hearth-Tree, Phase 8.6, 2026-10-15).</summary>
        public string style;
        /// <summary>The kind of prop that stood here before it was raised again (2026-10-15: "ruined_house", Wain's Rest as a waystation):
        /// it takes that kind's draws from the zone's stream and counts as that kind did for the camps' ground (ZoneBuilder.MobGround),
        /// so nothing else in the zone moves.</summary>
        public string was;
    }
    /// <summary>
    /// A deck in a cave (GAME-ONLY; the Adit rework, ZoneBuilder.AditDecks): a walkable way of planks, cut stone, rope or scree, running
    /// from <see cref="from"/> to <see cref="to"/> metres along its cave, its middle <see cref="aside"/> then <see cref="aside2"/> metres to the
    /// right of the cave's middle, its top <see cref="rise"/> metres over the cave's floor at its start and <see cref="rise2"/> at its end
    /// (straight between). One that runs across a hall keeps from and to alike and goes from aside to aside2. Everything on it stands in
    /// the navmesh; the way under it stays walkable where there is room.
    /// - style: timber (planks on posts), stone (cut stone on piers), rope (a rope bridge: planks on ropes, posts at its ends), scree
    ///   (a rubble ramp, always built up from the floor).
    /// - rails: both, left, right (as you walk it from its start), none, or empty: on every open edge (not along the wall it hugs, not
    ///   where a floor, another deck or a drop carries on). A rail is a solid 1.6 m: nobody falls off by walking.
    /// - wall: -1 or 1: it hugs the cave's left or right wall at its height (aside and aside2 are not used); 0: it stands free.
    /// - width across; solid: built up from the floor (a plinth). Any stretch under 2.5 m over the floor is built up anyway.
    /// - radius: a round deck (an island, a platform) of that radius at from and aside, its top rise over the floor.
    /// </summary>
    [Serializable] public sealed class ZoneDeck
    {
        public string name, cave, style = "timber", rails, canonStatus;
        public int wall;
        public float from, to, aside, aside2, rise, rise2, width = 3, radius;
        public bool solid;
    }
    /// <summary>
    /// A one-way drop (GAME-ONLY): off a ledge or a deck's open end in one cave (from metres along fromCave, fromAside to the right of its
    /// middle, fromRise over its floor: where you step off, a metre or so in from the edge, on the floor or deck there) down to where you
    /// land (to, toAside, toRise in toCave; empty toCave: the same cave; in a pool, its bed). Agents may take it down, never up
    /// (EncounterNavigation: a one-way link; a camp's mobs not at all). No deck rail stands across its start. falls: a stream falls down
    /// it too (a sheet of water from the lip, mist and foam at its foot). scenery: only that look, no way down (a fall beside a stair).
    /// </summary>
    [Serializable] public sealed class ZoneDrop
    {
        public string name, fromCave, toCave;
        public float from, fromAside, fromRise, to, toAside, toRise, width = 2;
        public bool falls, scenery;
    }
    /// <summary>
    /// Water in a cave (GAME-ONLY; ZoneBuilder.AditWater). depth: how deep in the middle (over about 1.45 m you swim).
    /// - a pool: radius over 0, round at from and aside, a basin sunk into the floor (its surface a hand under the floor, its banks
    ///   easing down over about twice its depth);
    /// - a stream: from..to along the cave, half metres either side of aside, sunk the same way, its surface following the floor;
    /// - flood: the water stands depth over the cave's lowest floor between from and to, from wall to wall, and on until the floor
    ///   climbs out of it (a drowned working, a flooded drift; the layout lets the floor rise out of it at both ends).
    /// A sunk basin is kept a metre deep at most where it meets a wall (the rock's foot is 1.2 m under the floor): deep water is a pool
    /// in the middle of a chamber, or a flood. Nothing of a cave's furnishing is set down in water.
    /// </summary>
    [Serializable] public sealed class ZoneCaveWater
    {
        public string name, cave;
        public float from, to, aside, half = 2.5f, radius, depth = 1;
        public bool flood;
    }
    /// <summary>
    /// A gate across a cave's way (GAME-ONLY; built as a ZoneGate by ZoneBuilder.Adit): its title, its cave and how far in; kind (liftgate,
    /// platformgate, bossdoor, bar, pen, hiddendoor); the prompt E shows at it (empty: there is nothing to use, it opens of itself); lord:
    /// the elite whose people answer the loud way; opensOn: the mob whose death opens it; look (grille, door, boards, bar, stockade,
    /// bookcase, song; empty: by its kind); side: "far" when it is worked from beyond it (a bar lifted from the hub's side only).
    /// </summary>
    [Serializable] public sealed class ZoneGateDef { public string title, cave, kind, prompt, lord, opensOn, look, side; public float along; }
    /// <summary>A named place in a cave (GAME-ONLY): along, aside and rise over its floor (a deck). ZoneBuilder.Anchors holds where each stands.</summary>
    [Serializable] public sealed class ZoneAnchor { public string name, cave; public float along, aside, rise; }
    /// <summary>
    /// A wing of a dungeon (GAME-ONLY; the run a sim leads): id and name; after: the wings or groups done before it opens (comma-separated);
    /// group: wings of which one is enough (the Scriptorium or the Muster Yard); exit: "drop" or "walk", the way home from its end; home:
    /// the anchor the run regroups at after it; optional: the run leaves it to you.
    /// </summary>
    [Serializable] public sealed class ZoneWing { public string id, name, after, group, exit, home; public bool optional; }
    /// <summary>An event of a dungeon (GAME-ONLY: the Wake): its id and kind, the wing it belongs to and its place in that wing's run, the anchor
    /// it centres on, and its waves.</summary>
    [Serializable] public sealed class ZoneEvent { public string id, kind, wing, anchor; public int step; public ZoneWave[] waves = new ZoneWave[0]; }
    /// <summary>A wave of an event: at seconds in, the camp (by name) it brings, and the line said as it comes (empty: the code's own line for that
    /// camp, else a plain one; EncounterSession.Release.cs, 2026-10-15).</summary>
    [Serializable] public sealed class ZoneWave { public float at; public string camp, text; }
    /// <summary>A trail of living lichen along a cave (GAME-ONLY): from..to along it, aside of its middle (the way the run goes).</summary>
    [Serializable] public sealed class ZoneCrumb { public string cave; public float from, to, aside; }
    /// <summary>
    /// A thing to gather that is not a prop: an ore seam, a windfall, a herb. node: its kind ("node.copper") in the trades'
    /// content, which says how it looks, what E offers and what it gives. rotation: its yaw (a seam's rock lies on its +Z side,
    /// its ore faces -Z; a windfall's stump is at its -X end). under: on a cave's floor (Hollow), not on the land over it.
    /// item: optional quest item it also gives while a quest wants it (as <see cref="ZoneProp.item"/>). Built after the map, from
    /// streams of their own, with no colliders: the zone's layout and navmesh are as they were.
    /// </summary>
    [Serializable] public sealed class ZoneNode { public string node, item; public Vector2 at; public float rotation; public bool under; }
    /// <summary>
    /// A station of the trades that is no villager's workplace (GAME-ONLY): kind forge (a field anvil and its coal pan; variant 1
    /// on a bone-bound block, 2 on a mossed stone with its embers in a stone basin), bench (a herbalist's bench with a drying rail)
    /// or fire (a cookfire in a ring of stones, a pot on a tripod). name is what the Trades window calls it ("Forge: Oska's
    /// bone-anvil"). rotation: its yaw (its front, where you stand, faces -Z). Built after the nodes, each from a stream of its own,
    /// with no colliders: the zone's layout and navmesh are as they were.
    /// </summary>
    [Serializable] public sealed class ZoneStation { public string kind, name, canonStatus; public Vector2 at; public float rotation; public int variant; }
    /// <summary>A station as built (ZoneBuilder.Stations): its kind (forge, bench, fire), its name, where the thing worked stands, and
    /// the prop it belongs to (a workplace's building, an inn, or a station of its own).</summary>
    public sealed class ZoneStationSpot
    {
        public string kind, name; public Vector3 position; public Transform root;
        /// <summary>For a station behind walls, the floor it is worked from, in its root's local x/z (an inn's taproom, the drying
        /// hut; the smithy and the kitchen on their open sides); zero size for one in the open.</summary>
        public Rect room;
        /// <summary>Whether a spot is on the station's side of its walls: anywhere for one in the open, else inside its room. The
        /// inn's hearth is not worked from the street through the wall.</summary>
        public bool Reaches(Vector3 at)
        {
            if (room.width <= 0 || room.height <= 0 || root == null) return true;
            var l = root.InverseTransformPoint(at); return room.Contains(new Vector2(l.x, l.z));
        }
    }
    /// <summary>A usable prop registered by ZoneBuilder (see ZoneProp.interact).</summary>
    public sealed class ZoneInteractable
    {
        public string name, prompt, item, kind; public bool once;
        /// <summary>The kind of node it is to gather (ZoneProp.node, ZoneNode.node), or null.</summary>
        public string node;
        /// <summary>Picked or emptied things vanish (herbs regrow, crates stay empty, worked nodes come back); wagons and trees stay put.</summary>
        public bool Vanishes { get { return kind == "herb" || kind == "crates" || kind == "barrels" || node != null; } }
        /// <summary>What vanishes while a worked node rests (a seam's ore, a windfall's trunk); null: the whole of it (a herb).</summary>
        public Transform part;
        public Vector3 position; public Transform root;
        public float hiddenUntil;
        /// <summary>The gate it works (kinds "liftgate" and "platformgate"); once the gate is open there is nothing left to use.</summary>
        public ZoneGate gate;
        public string Key(string zoneId) { return zoneId + "|" + name + "|" + Mathf.RoundToInt(position.x) + "|" + Mathf.RoundToInt(position.z); }
    }
    [Serializable] public sealed class ZoneWasting
    {
        /// <summary>The unmade edge: everything east of <see cref="x"/> greys out; a static curtain stands at x.</summary>
        public float x = 58, curtainHeight = 26, fade = 14; public string note;
    }
    /// <summary>
    /// A named place, labelled over <see cref="at"/>. view (optional): where the capture tour stands to frame it, facing at
    /// (unset: from its south-west); viewPitch and viewZoom set that shot's camera (0: 17 degrees, 11 m). ground (optional):
    /// "licked" paints the place's ground smooth and uncracked in an ash zone (the Unwoven Flats); paint only.
    /// </summary>
    [Serializable] public sealed class ZoneLabel { public string name, text, canonStatus; public Vector2 at; public float radius = 10; public Vector2 view; public float viewPitch, viewZoom; public string ground; }   // canonStatus: CANON / CANON-EXPANDED / GAME-ONLY / PROVISIONAL

    public static class ZoneColors
    {
        public static Color Parse(string hex, Color fallback)
        { return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback; }
    }
}
