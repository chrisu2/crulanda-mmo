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
    }
    /// <summary>
    /// role: any villager role, or stranger (keeps to one place). place: a place kind (inn, green, woods...).
    /// title: the nameplate subtitle. works: an ordinary villager with the role's working day and a home (an innkeeper), not
    /// one who keeps a post day and night.
    /// </summary>
    [Serializable] public sealed class ZoneResident { public string name, role, title, place, look; public Vector2 at; public bool works; }
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
    }
    /// <summary>Walk into the radius and press E to travel; you arrive at <see cref="arrive"/> in the other zone.</summary>
    [Serializable] public sealed class ZoneExit { public string to, name; public Vector2 at, arrive; public float radius = 5; }
    /// <summary>
    /// A hidden find: on no map, rewarded when found (a "Discovered" toast, XP, and most hold a cache). Its id is unique across
    /// zones ("secret.&lt;zone&gt;.&lt;slug&gt;") and is what the save keeps. kind is what stands there (ZoneBuilder builds it):
    /// - vista: a lookout, found by walking within radius (a cairn or nothing at all);
    /// - cache: a hidden box or bundle; note: a page or scrap tucked away; herb: a rare plant; chest: a locked chest;
    ///   key: a key hidden somewhere else, for a chest that names it in needs. These are found by searching (E, prompt).
    /// Rewards: xp, gold, an item id, a document id (a readable page, as quests give). height lifts the prop off the ground
    /// (something tucked up under a bridge or in a tree); rotation turns it. canonStatus as everywhere.
    /// </summary>
    [Serializable] public sealed class ZoneSecret
    {
        public string id, name, kind = "cache", prompt, text, item, document, needs, canonStatus;
        public Vector2 at; public float radius = 2.5f, rotation, height;
        public int xp, gold;
    }
    /// <summary>A secret as built in the world (ZoneBuilder.Secrets): its data, where it stands, and its visible parts.</summary>
    public sealed class ZoneSecretSpot { public ZoneSecret def; public Vector3 position; public Transform root; }
    /// <summary>One kind of weather a zone can have and how often (a weight, relative to the zone's other kinds).</summary>
    [Serializable] public sealed class ZoneWeather { public string kind; public float weight = 1; }
    /// <summary>An area filled with trees. kind: dead (grey, leafless, darkened ground), pine, broadleaf.</summary>
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
    /// well 1 blood-stone, inn 1 the Cracked Hearth (split, glowing chimney), crypt 1 a turfed barrow, grave 1 heaved over.
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
    }
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
