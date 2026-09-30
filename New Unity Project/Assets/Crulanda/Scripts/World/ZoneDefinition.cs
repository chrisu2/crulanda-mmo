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
    /// Ambient life. villagers: how many named villagers live here (homes are the zone's barred houses).
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
    }
    /// <summary>
    /// role: any villager role, or stranger (keeps to one place). place: a place kind (inn, green, woods...).
    /// title: the nameplate subtitle.
    /// </summary>
    [Serializable] public sealed class ZoneResident { public string name, role, title, place, look; public Vector2 at; }
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
    /// </summary>
    [Serializable] public sealed class ZonePath { public string name; public float width = 4; public Vector2[] points = new Vector2[0]; public float depth = 1.1f; }
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
    }
    /// <summary>A usable prop registered by ZoneBuilder (see ZoneProp.interact).</summary>
    public sealed class ZoneInteractable
    {
        public string name, prompt, item, kind; public bool once;
        /// <summary>Picked or emptied things vanish (herbs regrow, crates stay empty); wagons and trees stay put.</summary>
        public bool Vanishes { get { return kind == "herb" || kind == "crates" || kind == "barrels"; } }
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
    /// (unset: from its south-west); viewPitch and viewZoom set that shot's camera (0: 17 degrees, 11 m).
    /// </summary>
    [Serializable] public sealed class ZoneLabel { public string name, text; public Vector2 at; public float radius = 10; public Vector2 view; public float viewPitch, viewZoom; }

    public static class ZoneColors
    {
        public static Color Parse(string hex, Color fallback)
        { return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback; }
    }
}
