using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Minimap (round, north-up, rotating player arrow), zone map (M) and world map (scroll out on the zone map).
    /// Draws in EncounterHud's 1440x900 canvas. The zone picture is ZoneBuilder.MapTexture; the world map is the
    /// author's overworld image (ZoneArt.worldMap) with a pin per zone. What the two zone maps mark comes from
    /// <see cref="Marks"/> alone.
    /// </summary>
    public sealed class HudMaps
    {
        /// <summary>What a map mark is (see <see cref="Marks"/>).</summary>
        public enum MarkKind { Landmark, Camp, Exit, Enemy, QuestPlace, QuestRoad, QuestPerson }
        /// <summary>
        /// One thing the maps mark: what it is, where it is in the world, and its words (a name, a mob, a quest's title, or the ! or ?
        /// over someone). A camp carries its data (ring and levels); a road out the zone it leads to (null if unknown); a grey ! or ? is grey.
        /// </summary>
        public struct MapMark { public MarkKind kind; public Vector3 world; public string text; public bool grey; public ZoneDefinition to; public ZoneCamp camp; }
        /// <summary>
        /// Everything the minimap and the zone map mark besides you and Mira, in drawing order: the zone's landmarks, camps and roads
        /// out, enemies in sight, the places quests send you, the roads they lead down, and the ! and ? over people. The maps draw
        /// from this list and nothing else. Hidden finds (ZoneBuilder.Secrets) are never read here: a secret is on no map.
        /// </summary>
        static bool InCave(Vector2 at) { float y = 0; return Crulanda.World.Hollow.FloorUnder(at, ref y); }
        /// <summary>"  3-5" after a landmark that names a cave with camps in it (the levels of its camps), else nothing.</summary>
        static string CaveBand(Crulanda.World.ZoneDefinition z, string name)
        {
            var h = Crulanda.World.Hollow.All.Find(x => x.Name == name); if (h == null || z.camps == null) return "";
            int lo = int.MaxValue, hi = int.MinValue;
            foreach (var c in z.camps) if (c != null && h.FloorAt(c.center, out _)) { lo = Mathf.Min(lo, c.levelMin); hi = Mathf.Max(hi, c.levelMax); }
            return lo > hi ? "" : "  " + (lo == hi ? lo.ToString() : lo + "-" + hi);
        }
        public static void Marks(EncounterSession s, List<MapMark> into)
        {
            into.Clear(); var zone = s.Zone;
            if (zone != null)
            {
                var z = zone.Zone;
                // A cave's camps are not drawn over the land above them: the cave's own landmark carries their levels ("3-5").
                // A place not yet explored is a "?" (Achievements: walk there and it is named).
                foreach (var l in z.landmarks) into.Add(new MapMark { kind = MarkKind.Landmark, world = zone.Ground(l.at), text = s.Feats == null || s.Feats.Explored(z, l) ? l.name + CaveBand(z, l.name) : "?" });
                if (z.camps != null)
                    foreach (var c in z.camps) if (c != null && !InCave(c.center)) into.Add(new MapMark { kind = MarkKind.Camp, world = zone.Ground(c.center), text = c.mob, camp = c });
                foreach (var e in z.exits) into.Add(new MapMark { kind = MarkKind.Exit, world = zone.Ground(e.at), text = e.name, to = ZoneById(zone, e.to) });
            }
            foreach (var enemy in s.Enemies) if (enemy != null && enemy.actor.IsAlive && !enemy.Hidden) into.Add(new MapMark { kind = MarkKind.Enemy, world = enemy.transform.position, text = enemy.actor.DisplayName });
            if (s.Quests == null || zone == null) return;
            foreach (var (q, o, done) in s.Quests.Places())
                if (!done && (string.IsNullOrEmpty(o.zone) || o.zone == s.ZoneId)) into.Add(new MapMark { kind = MarkKind.QuestPlace, world = zone.Ground(o.at), text = q.title });
            // Breadcrumbs: a quest step waiting in another zone rings the exit on the road there.
            foreach (var (q, st) in s.Quests.Active())
            {
                var elsewhere = s.QuestZoneElsewhere(q, st); if (elsewhere == null) continue;
                var exit = s.ExitToward(elsewhere); if (exit == null) continue;
                into.Add(new MapMark { kind = MarkKind.QuestRoad, world = zone.Ground(exit.at), text = q.title });
            }
            var life = VillageLife.Active; if (life == null) return;
            foreach (var v in life.Villagers)
            {
                if (!v.Visible) continue;
                char m = s.Quests.Marker(v.Name, s.ZoneId, s.Progress.Level, out bool grey); if (m == ' ') continue;
                into.Add(new MapMark { kind = MarkKind.QuestPerson, world = v.transform.position, text = m.ToString(), grey = grey });
            }
        }
        /// <summary>A zone's data by id, from the builder's parsed list (no JSON parsing per frame).</summary>
        static ZoneDefinition ZoneById(ZoneBuilder zone, string id) { foreach (var d in zone.AllZones()) if (d.id == id) return d; return null; }
        readonly List<MapMark> marks = new List<MapMark>();
        public static readonly Rect MinimapRect = new Rect(1238, 30, 184, 184);
        public static readonly Rect WindowRect = new Rect(230, 50, 980, 800);
        static readonly float[] MinimapRadii = { 28, 45, 70 };
        int zoom = 1;
        Texture2D mask, dot, arrow, pin;
        GUIStyle label, title, note, centered, placeSmall;

        void Init()
        {
            if (mask != null) return;
            mask = new Texture2D(256, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(127.5f, 127.5f)) / 128f;
                Color c = r < .9f ? Color.clear : r < .97f ? new Color(.62f, .52f, .33f, 1) : r < 1 ? new Color(.2f, .16f, .1f, 1) : Color.clear;
                if (r >= .88f && r < .9f) c = new Color(.1f, .08f, .06f, .8f);
                mask.SetPixel(x, y, c);
            }
            mask.Apply();
            dot = Circle(24); pin = Circle(40, true);
            arrow = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                // Pointing up in GUI space (texture row 31 is the top).
                float half = (y / 31f) * 13f; bool inside = Mathf.Abs(x - 15.5f) < (15 - half) && y > 2 && y < 30;
                bool notch = y < 9 && Mathf.Abs(x - 15.5f) < (9 - y) * .8f;
                arrow.SetPixel(x, y, inside && !notch ? Color.white : Color.clear);
            }
            arrow.Apply();
            label = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            note = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            placeSmall = new GUIStyle(note) { wordWrap = false, clipping = TextClipping.Clip };   // a place name in a two-column list: one line
        }
        static Texture2D Circle(int size, bool ring = false)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2((size - 1) / 2f, (size - 1) / 2f)) / (size / 2f);
                Color c = r < 1 ? Color.white : Color.clear;
                if (ring && r < 1 && r > .72f) c = new Color(.1f, .08f, .06f, 1);
                t.SetPixel(x, y, c);
            }
            t.Apply(); return t;
        }
        void Shadowed(Rect r, string s, GUIStyle style, Color color)
        {
            GUI.contentColor = new Color(0, 0, 0, .85f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, style);
            GUI.contentColor = color; GUI.Label(r, s, style); GUI.contentColor = Color.white;
        }
        void Dot(Vector2 at, float size, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(at.x - size / 2, at.y - size / 2, size, size), dot); GUI.color = Color.white; }
        void Arrow(Vector2 at, float size, float yaw)
        {
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(yaw, at);
            GUI.color = new Color(1, .92f, .55f); GUI.DrawTexture(new Rect(at.x - size / 2, at.y - size / 2, size, size), arrow); GUI.color = Color.white;
            GUI.matrix = m;
        }

        GUIStyle mark;
        /// <summary>
        /// A quest pin: a gold ring on a place a quest asks you to go (a larger one on the road out toward a step in another zone),
        /// or the ! or ? over a quest giver (the same rules as the head markers). People off the edge of the minimap are not shown;
        /// places are clamped to its rim.
        /// </summary>
        void QuestMark(MapMark m, System.Func<Vector3, Vector2?> person, System.Func<Vector3, Vector2?> place, int size)
        {
            if (mark == null) mark = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mark.fontSize = size;
            if (m.kind == MarkKind.QuestPerson)
            {
                var at = person(m.world); if (!at.HasValue) return;
                Shadowed(new Rect(at.Value.x - 12, at.Value.y - 14, 24, 26), m.text, mark, m.grey ? new Color(.7f, .7f, .7f) : new Color(1, .82f, .15f));
            }
            else if (m.kind == MarkKind.QuestPlace || m.kind == MarkKind.QuestRoad)
            {
                var at = place(m.world); if (!at.HasValue) return;
                bool road = m.kind == MarkKind.QuestRoad; float half = size * (road ? .75f : .6f);
                GUI.color = new Color(1, .82f, .25f, road ? .95f : .85f); GUI.DrawTexture(new Rect(at.Value.x - half, at.Value.y - half, half * 2, half * 2), pin); GUI.color = Color.white;
            }
        }

        public static string Band(ZoneDefinition z) { return z.levelMin == z.levelMax ? "(" + z.levelMin + ")" : "(" + z.levelMin + "-" + z.levelMax + ")"; }
        /// <summary>Colour of a level band for you: grey = beneath you, green = right for you, yellow = a stretch, red = too dangerous.</summary>
        public static Color BandColor(ZoneDefinition z, int level)
        {
            if (level > z.levelMax + 2) return new Color(.65f, .65f, .62f);
            if (level >= z.levelMin) return new Color(.5f, 1, .45f);
            if (level >= z.levelMin - 2) return new Color(1, .85f, .3f);
            return new Color(1, .35f, .28f);
        }
        static Color LevelColor(int mobLevel, int level)
        {
            int d = mobLevel - level;
            return d <= -4 ? new Color(.65f, .65f, .62f) : d <= -2 ? new Color(.5f, 1, .45f) : d <= 1 ? new Color(1, .88f, .35f) : d <= 3 ? new Color(1, .55f, .25f) : new Color(1, .3f, .25f);
        }

        // ---------- minimap ----------
        public void DrawMinimap(EncounterSession s, Color gold)
        {
            Init(); var zone = s.Zone; var r = MinimapRect;
            Shadowed(new Rect(r.x - 40, r.y - 26, r.width + 80, 22), zone != null ? zone.Zone.displayName : s.ZoneTitle, centered, gold);
            GUI.color = new Color(.08f, .09f, .08f, 1); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            var p = s.Player.transform.position; float radius = MinimapRadii[zoom];
            if (zone != null && zone.MapTexture != null)
            {
                var uv = zone.MapUV(p); float w = radius * 2 / zone.Zone.size;
                GUI.DrawTextureWithTexCoords(r, zone.MapTexture, new Rect(uv.x - w / 2, uv.y - w / 2, w, w));
            }
            var center = r.center; float px = r.width / 2 / radius;
            System.Func<Vector3, bool, Vector2?> toMini = (world, clampToEdge) => {
                var d = new Vector2(world.x - p.x, world.z - p.z) * px; d.y = -d.y;
                float max = r.width / 2 - 9;
                if (d.magnitude > max) { if (!clampToEdge) return null; d = d.normalized * max; }
                return center + d;
            };
            // Roads out, enemies and quest pins (the minimap leaves landmarks and camps to the zone map).
            System.Func<Vector3, Vector2?> person = w => toMini(w, false), place = w => toMini(w, true);
            Marks(s, marks);
            foreach (var m in marks)
            {
                if (m.kind == MarkKind.Exit)
                {
                    var at = toMini(m.world, true); if (!at.HasValue) continue;
                    Dot(at.Value, 11, new Color(1, .82f, .3f));
                    // Where the road goes, and its level band, tucked inside the rim.
                    if (m.to != null) { var inward = (center - at.Value).normalized * 16; Shadowed(new Rect(at.Value.x + inward.x - 40, at.Value.y + inward.y - 9, 80, 18), Band(m.to), centered, BandColor(m.to, s.Progress.Level)); }
                }
                else if (m.kind == MarkKind.Enemy) { var at = toMini(m.world, false); if (at.HasValue) Dot(at.Value, 9, new Color(.95f, .25f, .2f)); }
                else QuestMark(m, person, place, 15);
            }
            if (s.Companion != null)
            { var at = toMini(s.Companion.transform.position, !s.Progress.recruited); if (at.HasValue) Dot(at.Value, s.Progress.recruited ? 9 : 12, s.Progress.recruited ? new Color(.4f, 1, .55f) : gold); }
            Arrow(center, 20, s.Player.transform.eulerAngles.y);
            GUI.DrawTexture(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), mask);
            // Time of day on the rim: a sun or moon disc and the hour.
            bool night = WorldClock.Darkness > .5f;
            Dot(new Vector2(r.xMax - 4, r.y + 10), 16, night ? new Color(.72f, .8f, 1) : new Color(1, .82f, .35f));
            Shadowed(new Rect(r.xMax - 50, r.y + 20, 60, 20), WorldClock.Text, centered, night ? new Color(.8f, .86f, 1) : gold);
            // The weather under the hour, when there is any to speak of.
            var weather = WorldWeather.Active;
            if (weather != null && weather.Kind != WeatherKind.Clear && weather.Kind != WeatherKind.Fair)
                Shadowed(new Rect(r.xMax - 70, r.y + 38, 100, 18), WeatherSchedule.Name(weather.Kind), centered, night ? new Color(.72f, .78f, .92f) : new Color(.86f, .84f, .74f));
            // Corner mask: square HUD corners hidden under the round frame.
            // The buttons stand down under the bags window (three or four trade bags worn), so a press meant for a slot reaches it.
            Rect plus = new Rect(r.xMax - 18, r.yMax - 30, 24, 24), minus = new Rect(r.xMax - 18, r.yMax - 4, 24, 24), map = new Rect(r.x - 10, r.yMax - 16, 28, 24);
            if (!EncounterHud.BagsCover(plus) && GUI.Button(plus, "+")) zoom = Mathf.Max(0, zoom - 1);
            if (!EncounterHud.BagsCover(minus) && GUI.Button(minus, "−")) zoom = Mathf.Min(MinimapRadii.Length - 1, zoom + 1);
            if (!EncounterHud.BagsCover(map) && GUI.Button(map, "M")) s.MapOpen = !s.MapOpen;
        }

        // ---------- zone / world map window ----------
        public void DrawWindow(EncounterSession s, Color gold, Color ink)
        {
            Init(); var zone = s.Zone; var w = WindowRect;
            if (Event.current.type == EventType.ScrollWheel && w.Contains(Event.current.mousePosition))
            { s.MapWorld = Event.current.delta.y > 0 || (s.MapWorld && Event.current.delta.y >= 0); Event.current.Use(); }
            GUI.color = new Color(ink.r, ink.g, ink.b, .97f); GUI.DrawTexture(w, Texture2D.whiteTexture); GUI.color = Color.white;
            var map = new Rect(w.x + 20, w.y + 64, 700, 700);
            if (!s.MapWorld && zone != null) DrawZoneMap(s, zone, map, gold);
            else DrawWorldMap(s, zone, map, gold);
            if (GUI.Button(new Rect(w.xMax - 250, w.y + 14, 110, 32), s.MapWorld ? "Zone map" : "World map")) s.MapWorld = !s.MapWorld;
            if (GUI.Button(new Rect(w.xMax - 130, w.y + 14, 110, 32), "Close [M]")) s.MapOpen = false;
            GUI.Label(new Rect(w.x + 20, w.yMax - 30, 700, 24), s.MapWorld ? "Scroll in (or press Zone map) to return to the zone." : "Scroll out (or press World map) for the world map.", note);
        }
        Vector2 OnMap(Rect map, Vector2 uv) { return new Vector2(map.x + uv.x * map.width, map.y + (1 - uv.y) * map.height); }
        void DrawZoneMap(EncounterSession s, ZoneBuilder zone, Rect map, Color gold)
        {
            var z = zone.Zone; var side = new Rect(map.xMax + 20, map.y, WindowRect.xMax - map.xMax - 40, map.height);
            Shadowed(new Rect(WindowRect.x + 20, WindowRect.y + 14, 600, 34), z.displayName + "   " + Band(z), title, gold);
            GUI.Label(new Rect(WindowRect.x + 20, WindowRect.y + 44, 700, 20), z.subtitle, note);
            if (zone.MapTexture != null) GUI.DrawTexture(map, zone.MapTexture);
            GUI.color = new Color(0, 0, 0, .25f); GUI.DrawTexture(new Rect(map.x - 2, map.y - 2, map.width + 4, 2), Texture2D.whiteTexture); GUI.color = Color.white;
            System.Func<Vector3, Vector2?> onMap = w => OnMap(map, zone.MapUV(w));
            Marks(s, marks);
            foreach (var m in marks)
            {
                var at = OnMap(map, zone.MapUV(m.world));
                switch (m.kind)
                {
                    case MarkKind.Landmark:
                        Dot(at, 8, new Color(.95f, .85f, .6f)); Shadowed(new Rect(at.x - 110, at.y + 4, 220, 20), m.text, centered, new Color(1, .93f, .75f));
                        break;
                    case MarkKind.Camp:
                    {
                        // A red ring with the pack and its levels.
                        var c = m.camp; float rr = Mathf.Max(10, c.radius / z.size * map.width);
                        GUI.color = new Color(.9f, .25f, .2f, .5f); GUI.DrawTexture(new Rect(at.x - rr, at.y - rr, rr * 2, rr * 2), pin); GUI.color = Color.white;
                        Shadowed(new Rect(at.x - 110, at.y + rr - 2, 220, 20), c.mob + "  " + (c.levelMin == c.levelMax ? c.levelMin.ToString() : c.levelMin + "-" + c.levelMax) + (c.elite ? "  elite" : ""), centered, LevelColor(c.levelMax, s.Progress.Level));
                        break;
                    }
                    case MarkKind.Exit:
                    {
                        Dot(at, 14, new Color(1, .82f, .3f));
                        string text = "→ " + (m.to != null ? m.to.displayName + "  (" + Band(m.to) + ")" : m.text);
                        float tw = label.CalcSize(new GUIContent(text)).x;
                        float lx = Mathf.Clamp(at.x - tw / 2, map.x + 4, map.xMax - tw - 4), ly = Mathf.Clamp(at.y - 26, map.y + 4, map.yMax - 24);
                        Shadowed(new Rect(lx, ly, tw + 4, 20), text, label, m.to != null ? BandColor(m.to, s.Progress.Level) : gold);
                        break;
                    }
                    case MarkKind.Enemy: Dot(at, 10, new Color(.95f, .25f, .2f)); break;
                    default: QuestMark(m, onMap, onMap, 20); break;
                }
            }
            if (s.Companion != null)
            {
                var at = OnMap(map, zone.MapUV(s.Companion.transform.position));
                Dot(at, 12, s.Progress.recruited ? new Color(.4f, 1, .55f) : gold); Shadowed(new Rect(at.x + 8, at.y - 10, 120, 20), "Mira", label, new Color(.6f, 1, .7f));
            }
            Arrow(OnMap(map, zone.MapUV(s.Player.transform.position)), 26, s.Player.transform.eulerAngles.y);
            // Side panel: legend and places.
            float y = side.y;
            GUI.Label(new Rect(side.x, y, side.width, 22), "LEGEND", label); y += 26;
            foreach (var (text, c) in new[] { ("You", new Color(1, .92f, .55f)), ("Mira", new Color(.4f, 1, .55f)), ("Enemy / camp", new Color(.95f, .25f, .2f)), ("Road out (levels)", new Color(1, .82f, .3f)), ("Quest place", new Color(1, .82f, .25f)), ("Landmark", new Color(.95f, .85f, .6f)) })
            { Dot(new Vector2(side.x + 8, y + 10), 11, c); GUI.Label(new Rect(side.x + 22, y, side.width - 22, 22), text, note); y += 22; }
            y += 12; GUI.Label(new Rect(side.x, y, side.width, 22), "ROADS OUT", label); y += 24;
            foreach (var e in z.exits)
            {
                var to = zone.FindZone(e.to); if (to == null) continue;
                Shadowed(new Rect(side.x, y, side.width, 20), to.displayName + "  " + Band(to), label, BandColor(to, s.Progress.Level)); y += 20;
                GUI.Label(new Rect(side.x + 10, y, side.width - 10, 20), e.name, note); y += 22;
            }
            int known = s.Feats != null ? s.Feats.ExploredIn(z) : z.landmarks.Length;
            y += 10; GUI.Label(new Rect(side.x, y, side.width, 22), "PLACES  (" + known + " / " + (s.Feats != null ? s.Feats.PoisIn(z) : z.landmarks.Length) + ")", label); y += 24;
            // One column while they fit; a bigger zone's places go in two narrower columns in the smaller hand.
            float room = side.yMax - 60 - y; bool two = z.landmarks.Length > Mathf.FloorToInt(room / 19);
            float step = two ? 17 : 19, colW = two ? side.width / 2 - 4 : side.width; int perCol = Mathf.Max(1, Mathf.FloorToInt(room / step));
            for (int i = 0; i < z.landmarks.Length && i < perCol * (two ? 2 : 1); i++)
            {
                bool seen = s.Feats == null || s.Feats.Explored(z, z.landmarks[i]);
                Shadowed(new Rect(side.x + (i / perCol) * (colW + 8), y + (i % perCol) * step, colW, 20), seen ? z.landmarks[i].name : "? ? ?", two ? placeSmall : label, seen ? gold : new Color(.6f, .58f, .52f));
            }
            if (!string.IsNullOrEmpty(z.canonStatus)) GUI.Label(new Rect(side.x, side.yMax - 40, side.width, 40), "Lore status: " + z.canonStatus, note);
        }
        void DrawWorldMap(EncounterSession s, ZoneBuilder zone, Rect map, Color gold)
        {
            Shadowed(new Rect(WindowRect.x + 20, WindowRect.y + 14, 600, 34), "The Land of Crulanda", title, gold);
            GUI.Label(new Rect(WindowRect.x + 20, WindowRect.y + 44, 700, 20), "World map", note);
            var art = zone != null ? zone.art : null;
            if (art != null && art.worldMap != null) GUI.DrawTexture(map, art.worldMap, ScaleMode.StretchToFill);
            else GUI.Label(map, "World map image not imported.", note);
            if (zone == null) return;
            var side = new Rect(map.xMax + 20, map.y, WindowRect.xMax - map.xMax - 40, map.height); float y = side.y;
            GUI.Label(new Rect(side.x, y, side.width, 22), "ZONES", label); y += 28;
            var all = zone.AllZones();
            System.Func<ZoneDefinition, Vector2> pos = zz => new Vector2(map.x + zz.worldMapPosition.x * map.width, map.y + zz.worldMapPosition.y * map.height);
            // Roads between zones: a dotted line for each pair joined by an exit.
            foreach (var a in all)
                foreach (var e in a.exits)
                {
                    var b = all.Find(zz => zz.id == e.to); if (b == null || a.worldMapPosition.x < 0 || b.worldMapPosition.x < 0 || string.CompareOrdinal(a.id, b.id) > 0) continue;
                    Vector2 p0 = pos(a), p1 = pos(b); int dots = Mathf.Max(3, Mathf.RoundToInt(Vector2.Distance(p0, p1) / 9));
                    for (int i = 1; i < dots; i++) Dot(Vector2.Lerp(p0, p1, i / (float)dots), 5, new Color(.3f, .2f, .1f, .85f));
                }
            foreach (var z in all)
            {
                bool here = z.id == zone.Zone.id;
                if (z.worldMapPosition.x >= 0)
                {
                    var at = pos(z);
                    GUI.color = here ? new Color(1, .85f, .35f) : BandColor(z, s.Progress.Level);
                    GUI.DrawTexture(new Rect(at.x - (here ? 11 : 8), at.y - (here ? 11 : 8), here ? 22 : 16, here ? 22 : 16), pin); GUI.color = Color.white;
                    Shadowed(new Rect(at.x + 12, at.y - 11, 260, 22), z.displayName + "  " + Band(z) + (here ? "  (you are here)" : ""), label, here ? gold : BandColor(z, s.Progress.Level));
                }
                Shadowed(new Rect(side.x, y, side.width, 20), z.displayName + "  " + Band(z), label, here ? gold : BandColor(z, s.Progress.Level)); y += 20;
                string info = (z.subtitle ?? "") + (string.IsNullOrEmpty(z.worldMapNote) ? "" : "\n" + z.worldMapNote);
                float h = note.CalcHeight(new GUIContent(info), side.width);
                GUI.Label(new Rect(side.x, y, side.width, h), info, note); y += h + 14;
            }
        }
    }
}
