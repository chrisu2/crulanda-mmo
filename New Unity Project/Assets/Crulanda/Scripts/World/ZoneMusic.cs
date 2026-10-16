using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The zone's music (2026-10-15, Chris 01:20: "I have some music: dark realm, eastern life, last zone"): thirteen tracks of
    /// ithappy's three packs under Resources/Music (Dark, Eastern, Last; the terms in Resources/Music/LICENSES.md). Sparse, as a
    /// field should be: a track plays through, then two to four minutes of quiet, then another of the set; the first a few seconds
    /// after the zone loads. The set follows where you stand (<see cref="TracksFor"/>): the zone's by day, its night set after dusk
    /// (WorldClock.Darkness), its grey set near the Wasting's curtain, and the dungeon set in the Sealed Adit or inside any cave
    /// (Hollow.InsideAny: Crowsfoot Hollow, the Root-Mother's Deep). When the set changes under a playing track that is not in the
    /// new one, six seconds of crossfade carry it over; in a gap, a cave's mouth cuts the wait short. A fight track (the eastern
    /// pack's two) comes up over the ambient while the party fights (EncounterSession.InCombat), ducking it to a quarter, and fades
    /// out eight seconds after the last blow. Night takes the level to .6. The level is PlayerPrefs "music" (0-1, .35 at first):
    /// /music &lt;0-1&gt; and /music off in the chat (EncounterSession.PlayerChat). The session makes one per zone load
    /// (<see cref="Begin"/>); the camera's AudioListener hears it; everything is 2D. A zone's own folder, Resources/Music/&lt;short&gt;
    /// (oakhaven, khaven, peaks, ashrim, verdant, adit), adds any track dropped into it to that zone's day set, no code needed.
    /// </summary>
    public sealed class ZoneMusic : MonoBehaviour
    {
        public const string VolumeKey = "music"; public const float DefaultVolume = .35f;
        /// <summary>Seconds: the crossfade (and a fresh track's rise), the fight track's rise and its fall, the quiet between tracks
        /// (least and most), the wait after a load. Then the level at full night, the ambient's share under a fight, and the metres
        /// before the Wasting's curtain where the grey set begins.</summary>
        public const float Crossfade = 6, FightIn = 1.5f, FightOut = 8, GapMin = 120, GapMax = 240, FirstWait = 12, NightLevel = .6f, Duck = .25f, GreyReach = 70;
        /// <summary>A zone's tracks, as Resources paths without the extension: by day; by night (the day's when empty); near the grey
        /// (zones with a wasting block; the day's when empty); and its fight track (the field one when null).</summary>
        public sealed class Set { public string[] day = new string[0], night = new string[0], grey = new string[0]; public string combat; }
        public const string FieldFight = "Music/Eastern/MeasuredFight", DungeonFight = "Music/Eastern/AggressiveFight";
        /// <summary>The Sealed Adit's and every cave's: the dark pack's two crypts.</summary>
        public static readonly string[] DungeonTracks = { "Music/Dark/ScaryCrypt", "Music/Dark/Crypt" };
        /// <summary>
        /// By zone id, chosen by the packs' moods against WORLD_ZONES: the quiet eastern pieces for the Peaks (a pass with pilgrims)
        /// and the Rim's dusk, the last-zone pieces for the Rim's waste and the grey's edge, the dark pack for Khaven's dusk, the
        /// villages' nights and the dungeons. Nothing in the packs is pastoral, so Oakhaven and the Shore take the two softest (Last
        /// Hope, Oasis). Two to four a zone; a track two zones share is stored once, under its pack.
        /// </summary>
        public static readonly Dictionary<string, Set> Sets = new Dictionary<string, Set> {
            { "zone.oakhaven", new Set { day = new[] { "Music/Last/LastHope", "Music/Eastern/Oasis" }, night = new[] { "Music/Dark/Cemetery" }, grey = new[] { "Music/Last/EchoesAfterFire" } } },
            { "zone.khaven", new Set { day = new[] { "Music/Dark/Cemetery", "Music/Last/RustAndSilence" } } },   // dusk all day: the same by night
            { "zone.peaks", new Set { day = new[] { "Music/Eastern/MysteriousCity", "Music/Eastern/RestlessNight" }, night = new[] { "Music/Eastern/RestlessNight" } } },
            { "zone.ashrim", new Set { day = new[] { "Music/Last/EchoesAfterFire", "Music/Last/ForgottenSouls", "Music/Eastern/DesertSunset" }, night = new[] { "Music/Last/ForgottenSouls" }, grey = new[] { "Music/Last/EchoesAfterFire" } } },
            { "zone.verdant", new Set { day = new[] { "Music/Eastern/Oasis", "Music/Last/LastHope" }, night = new[] { "Music/Eastern/RestlessNight" } } },
            { "zone.adit", new Set { day = DungeonTracks, combat = DungeonFight } },
            { "zone.crown", new Set { day = new[] { "Music/Eastern/MysteriousCity", "Music/Last/LastHope" }, night = new[] { "Music/Eastern/RestlessNight" } } },   // the Crown of Argentis (a city)
        };
        /// <summary>A zone's set; a zone not in <see cref="Sets"/> gets Oakhaven's.</summary>
        public static Set SetFor(string zoneId) { Set s; return zoneId != null && Sets.TryGetValue(zoneId, out s) ? s : Sets["zone.oakhaven"]; }
        /// <summary>The tracks for where <paramref name="at"/> stands in <paramref name="zone"/> at <paramref name="darkness"/>
        /// (WorldClock's, 0..1), and the fight track that goes with them: the dungeon set inside a cave or in the Adit, else the zone's
        /// grey, night or day set.</summary>
        public static string[] TracksFor(ZoneDefinition zone, Vector3 at, float darkness, out string fight)
        {
            var set = SetFor(zone != null ? zone.id : null);
            if (set.day == DungeonTracks || Hollow.InsideAny(at, 1)) { fight = DungeonFight; return DungeonTracks; }
            fight = set.combat ?? FieldFight;
            if (zone != null && zone.wasting != null && set.grey.Length > 0 && at.x > zone.wasting.x - GreyReach) return set.grey;
            if (darkness > .5f && set.night.Length > 0) return set.night;
            return set.day;
        }
        /// <summary>The level, 0..1, kept in PlayerPrefs "music"; 0 is off (no track is even loaded).</summary>
        public static float Volume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume)); }
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); if (value > 0) PlayerPrefs.SetInt(VolumeKey + ".mute", 0); PlayerPrefs.Save(); }   // /music on unmutes too
        }
        public static ZoneMusic Active { get; private set; }
        /// <summary>One for the zone just loaded, in place of any older one: the zone, whether the party is fighting, and where the
        /// player stands (both asked each frame).</summary>
        public static ZoneMusic Begin(ZoneDefinition zone, Func<bool> fighting, Func<Vector3> playerAt)
        {
            if (Active != null) Destroy(Active.gameObject);
            var m = new GameObject("Zone music").AddComponent<ZoneMusic>(); m.Init(zone, fighting, playerAt); return m;
        }
        public void Init(ZoneDefinition zone, Func<bool> fighting, Func<Vector3> playerAt)
        {
            this.zone = zone; this.fighting = fighting; this.playerAt = playerAt;
            if (zone != null && !string.IsNullOrEmpty(zone.id))
            {
                var folder = "Music/" + zone.id.Replace("zone.", "");
                extras = Array.ConvertAll(Resources.LoadAll<AudioClip>(folder), c => folder + "/" + c.name);
            }
        }
        /// <summary>What plays now (tests, a HUD line): the ambient's path, or null in a gap; and whether the fight track is up.</summary>
        public string Playing { get { return a != null && a.Playing ? a.Path : null; } }
        public bool FightUp { get { return fightLevel > 0; } }

        /// <summary>One source and what it is to play: cued by path, started once the clip's data is in (the clips load in the
        /// background), stopped; its level is the fade, 0..1, the owner mixes in.</summary>
        sealed class Deck
        {
            public readonly AudioSource Src; public string Path; public float Level; AudioClip pending; float startedAt = -1;
            public Deck(Transform under, string name, bool loop)
            {
                Src = new GameObject(name).AddComponent<AudioSource>(); Src.transform.SetParent(under, false);
                Src.playOnAwake = false; Src.loop = loop; Src.spatialBlend = 0; Src.volume = 0; Src.priority = 0;
            }
            public bool Busy { get { return pending != null || Src.clip != null; } }
            public bool Playing { get { return Src.clip != null && Src.isPlaying; } }
            /// <summary>A track that ran to its end (not one still loading, not a stopped deck).</summary>
            public bool Ended(float now) { return pending == null && Src.clip != null && !Src.isPlaying && now > startedAt + 1; }
            public void Cue(string path, float now)
            {
                Stop(); if (string.IsNullOrEmpty(path)) return;
                pending = Resources.Load<AudioClip>(path);
                if (pending == null) { Debug.LogWarning("Zone music: no clip at Resources/" + path + "."); return; }
                Path = path; if (pending.loadState == AudioDataLoadState.Unloaded) pending.LoadAudioData(); startedAt = now;
            }
            public void Tick(float now)
            {
                if (pending == null) return;
                if (pending.loadState == AudioDataLoadState.Loaded) { Src.clip = pending; Src.Play(); pending = null; startedAt = now; }
                else if (pending.loadState == AudioDataLoadState.Failed || now > startedAt + 30) { Debug.LogWarning("Zone music: " + Path + " did not load."); pending = null; Path = null; }
            }
            public void Stop() { Src.Stop(); Src.clip = null; Path = null; pending = null; Level = 0; }
        }
        ZoneDefinition zone; Func<bool> fighting; Func<Vector3> playerAt; string[] extras = new string[0];
        Deck a, b, fight;   // a carries the ambient, b the one it crosses to, fight the fight track over both
        float fade = -1, fightLevel, nextAt; bool fightWanted; string[] shown; string lastPath;
        void Awake() { Active = this; a = new Deck(transform, "Ambient", false); b = new Deck(transform, "Ambient (next)", false); fight = new Deck(transform, "Fight", true); nextAt = Time.unscaledTime + FirstWait; }
        void OnDestroy() { if (Active == this) Active = null; }
        void Update()
        {
            if (AudioListener.pause) return;   // the application paused: nothing moves on
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime, level = AudioMix.Effective(AudioMix.Channel.Music), fightSet = AudioMix.Effective(AudioMix.Channel.CombatMusic), dark = WorldClock.Darkness;
            var want = TracksFor(zone, playerAt != null ? playerAt() : Vector3.zero, dark, out string fightPath);
            a.Tick(now); b.Tick(now); fight.Tick(now);
            if (a.Ended(now)) { a.Stop(); nextAt = now + UnityEngine.Random.Range(GapMin, GapMax); }
            // The set changed under the track: cross to one of the new set. In a gap, a cave's mouth (or the Adit) shortens the wait.
            if (want != shown)
            {
                shown = want;
                if (a.Busy && fade < 0 && Array.IndexOf(want, a.Path) < 0) { b.Cue(Pick(want), now); fade = 0; }
                else if (!a.Busy && want == DungeonTracks) nextAt = Mathf.Min(nextAt, now + 4);
            }
            if (fade >= 0)
            {
                fade += dt; float k = Mathf.Clamp01(fade / Crossfade); a.Level = 1 - k; b.Level = k;
                if (k >= 1) { a.Stop(); var s = a; a = b; b = s; a.Level = 1; fade = -1; shown = null; }   // looked at again next frame: the set may have moved on meanwhile
            }
            else if (!a.Busy) { if (now >= nextAt && level > 0) { a.Cue(Pick(want), now); a.Level = 0; if (!a.Busy) nextAt = now + 30; } }   // a clip that is not there: try again later, not every frame
            else a.Level = Mathf.MoveTowards(a.Level, 1, dt / Crossfade);   // a fresh track rises from nothing
            // The fight track: up in a second and a half, down over eight; gone when down. A fight that starts again inside those
            // eight seconds keeps the track it had.
            bool fightingNow = fighting != null && fighting() && fightSet > 0;
            if (fightingNow && !fightWanted) { fight.Cue(fightPath, now); fightWanted = true; }
            fightLevel = Mathf.MoveTowards(fightLevel, fightingNow ? 1 : 0, dt / (fightingNow ? FightIn : FightOut));
            if (!fightingNow && fightLevel <= 0 && fightWanted) { fight.Stop(); fightWanted = false; }
            float night = Mathf.Lerp(1, NightLevel, dark), duck = Mathf.Lerp(1, Duck, fightLevel);
            a.Src.volume = level * night * duck * a.Level; b.Src.volume = level * night * duck * b.Level; fight.Src.volume = fightSet * Mathf.Lerp(1, .85f, dark) * fightLevel;
        }
        /// <summary>One of the set, not the last played when there is a choice; the zone's own folder's tracks count with its day set.</summary>
        string Pick(string[] set)
        {
            var pool = new List<string>(set); if (set == SetFor(zone != null ? zone.id : null).day) pool.AddRange(extras);
            if (pool.Count > 1) pool.Remove(lastPath);
            if (pool.Count == 0) return null;
            lastPath = pool[UnityEngine.Random.Range(0, pool.Count)]; return lastPath;
        }
    }
}
