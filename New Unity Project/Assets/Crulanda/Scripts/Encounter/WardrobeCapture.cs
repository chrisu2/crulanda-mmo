using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The wardrobe line-up (loot DESIGN.md section 8), run with <c>--crulanda-wardrobe-capture &lt;dir&gt;</c> from a windowed
    /// player (the labels are IMGUI): bare mannequins in a quiet open spot near the zone's green, one row at a time, every
    /// main-hand and off-hand family and variant held, each labelled with its look, then a quality ladder (poor to epic) at
    /// dusk so the glow reads. Noon in calm weather; the HUD, the player, Mira, the enemies and the village are hidden. Rows are
    /// seven figures wide so each weapon is big enough to judge, which takes several shots per set:
    /// 01-weapon-rack-a..g, 02-shield-wall-a..c, 03-quality-ladder-a (blades), -b (shields), -c (lanterns).
    /// The session is on the capture's throwaway save (EncounterCapture), so nothing here touches a real character.
    /// </summary>
    public sealed class WardrobeCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-wardrobe-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }

        /// <summary>Every main-hand family and variant with the palette its named piece wears (glowing where the named piece does).</summary>
        public static readonly string[] MainHandLooks = {
            "sword.short:plain/oakhaven", "sword.short:notched/oakhaven", "sword.arming:straight/tollroad", "sword.arming:curved/concord", "sword.arming:disc/tollroad",
            "sword.falchion:clipped/sandthrone", "sword.falchion:heavy/sandthrone", "sword.sabre:officer/sandthrone", "sword.sabre:broken/sandthrone+glow",
            "sword.leaf:bronze/veridian", "sword.leaf:root/veridian", "sword.great:steel/veridian", "sword.great:wood/veridian+glow", "sword.great:living/veridian+glow",
            "knife:hooked/sandthrone", "knife:needle/tollroad", "knife:glass/pale+glow", "knife:sickle/ashwalker+glow", "cleaver:slab/ashwalker", "cleaver:notched/sandthrone",
            "axe.hand:wedge/oakhaven", "axe.hand:hatchet/khaven", "axe.bearded:plain/sandthrone", "axe.bearded:hooked/khaven", "axe.crescent:plain/tollroad", "axe.crescent:spiked/ashwalker",
            "club:plain/oakhaven", "club:studded/oakhaven", "club:bound/pilgrim", "club:tusk/tollroad", "club:tankard/oakhaven",
            "mace.flanged:six/tollroad", "mace.flanged:fist/khaven", "hammer.war:pick/khaven", "hammer.war:maul/khaven",
            "mace.root:burl/veridian", "mace.root:antler/veridian", "mace.root:briar/veridian",
            "polearm:spear/tollroad", "polearm:harpoon/ashwalker", "polearm:fork/oakhaven", "polearm:billhook/sandthrone", "polearm:spade/khaven",
            "staff:knob/oakhaven", "staff:crook/pilgrim", "staff:forked/pale+glow", "staff:skull/cult"
        };
        /// <summary>Every off-hand family and variant.</summary>
        public static readonly string[] OffHandLooks = {
            "shield.buckler:plain/oakhaven", "shield.buckler:tusk/oakhaven", "shield.buckler:chitin/veridian",
            "shield.round:boards/oakhaven", "shield.round:hide/sandthrone", "shield.round:cask/ashwalker", "shield.round:lid/sandthrone",
            "shield.heater:plain/tollroad", "shield.heater:striped/concord", "shield.heater:glass/pale+glow",
            "shield.kite:plain/veridian", "shield.kite:slab/tollroad+glow", "shield.leaf:bronze/veridian", "shield.leaf:bark/veridian",
            "offhand.hung:lantern/oakhaven", "offhand.hung:shuttered/sandthrone+glow", "offhand.hung:moss/veridian+glow", "offhand.hung:censer/cult+glow", "offhand.hung:scale/concord"
        };
        const int PerRow = 5; const float Spacing = 1.7f;   // five to a shot, framed close enough to judge a guard or a rim

        sealed class Entry { public string[] ids; public string title, sub; }
        EncounterSession session; ItemDatabase db; GearLooks looks;
        readonly List<GameObject> figures = new List<GameObject>();
        readonly List<(Transform at, string title, string sub)> labels = new List<(Transform, string, string)>();
        GUIStyle titleStyle, subStyle;
        Vector2 spot; float ground;

        public IEnumerator Run(EncounterSession s, string directory)
        {
            session = s; Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            var text = Resources.Load<TextAsset>("Gear/looks");
            if (text == null) { Debug.LogError("Wardrobe capture: Resources/Gear/looks.json is missing."); Application.Quit(1); yield break; }
            looks = GearLooks.Parse(text.text); db = new ItemDatabase();
            Quiet();
            var zone = Crulanda.World.ZoneBuilder.Active;
            spot = FindSpot(zone, out ground);
            Debug.Log("Wardrobe capture at " + spot + " (ground " + ground.ToString("0.0") + ").");

            var main = Entries(MainHandLooks, "mainhand"); var off = Entries(OffHandLooks, "offhand");
            for (int i = 0, n = 0; i < main.Count; i += PerRow, n++) yield return Shot(Path.Combine(directory, "01-weapon-rack-" + (char)('a' + n) + ".png"), main.GetRange(i, Mathf.Min(PerRow, main.Count - i)), 115, 12);
            for (int i = 0, n = 0; i < off.Count; i += PerRow, n++) yield return Shot(Path.Combine(directory, "02-shield-wall-" + (char)('a' + n) + ".png"), off.GetRange(i, Mathf.Min(PerRow, off.Count - i)), -115, 12);
            // The quality ladder: the same generated piece (level 8, Ridge-forged) from poor to epic, at dusk.
            yield return Shot(Path.Combine(directory, "03-quality-ladder-a.png"), Ladder("mainhand", "Blade"), 115, 19.3f);
            yield return Shot(Path.Combine(directory, "03-quality-ladder-b.png"), Ladder("offhand", "Shield"), -115, 19.3f);
            yield return Shot(Path.Combine(directory, "03-quality-ladder-c.png"), Ladder("offhand", "Lantern"), -150, 19.3f);
            Clear(); EncounterHud.Hidden = false;
            Debug.Log("WARDROBE_CAPTURE_DONE"); Application.Quit(0);
        }

        /// <summary>One look per entry, as a named test item (wardrobe.N) registered with this run's own copy of the looks.</summary>
        List<Entry> Entries(string[] list, string slot)
        {
            var e = new List<Entry>();
            foreach (var look in list)
            {
                string id = "wardrobe." + db.Items.Count;
                looks.Register(id, look);
                GearLooks.TryParseLook(look, out var family, out var variant, out var palette, out bool glow);
                db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = slot, quality = 2, canonStatus = "GAME-ONLY" };
                e.Add(new Entry { ids = new[] { id }, title = family + " : " + variant, sub = palette + (glow ? " + glow" : "") });
            }
            return e;
        }
        /// <summary>Five generated pieces, poor to epic, all "Ridge-forged &lt;piece&gt;" at level 8 with the family's first variant.</summary>
        List<Entry> Ladder(string slot, string piece)
        {
            var e = new List<Entry>();
            for (int q = 0; q <= 4; q++)
            {
                string found = null;
                for (int seed = 0; seed < 10000 && found == null; seed++)
                {
                    string id = ItemDatabase.GearId(slot, 8, q, seed); var d = db.Get(id);
                    if (GearLooks.TrySplitGenerated(d, out var material, out var p, out _, out _) && material == "Ridge-forged" && p == piece && looks.Resolve(d).variant == GearLooks.Family(looks.Resolve(d).family).variants[0]) found = id;
                }
                if (found == null) { Debug.LogError("Wardrobe capture: no " + ItemDatabase.QualityNames[q] + " Ridge-forged " + piece + " among the first 10000 seeds."); continue; }
                var l = looks.Resolve(db.Get(found));
                e.Add(new Entry { ids = new[] { found }, title = ItemDatabase.QualityNames[q], sub = db.Get(found).name + "  (" + l.family + " : " + l.variant + ")" });
            }
            return e;
        }

        /// <summary>Hides everything that is not the line-up: the HUD, the player and Mira, the enemies, the village; noon, calm weather.</summary>
        void Quiet()
        {
            EncounterHud.Hidden = true; Crulanda.World.WorldClock.Hour = 12;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            foreach (var e in session.Enemies) if (e != null) { e.enabled = false; foreach (var r in e.GetComponentsInChildren<Renderer>(true)) r.enabled = false; }
            if (session.Companion != null) foreach (var r in session.Companion.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var r in session.Player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var motor = session.Player.GetComponent<AdventurerMotor>(); if (motor != null) motor.enabled = false;
            if (VillageLife.Active != null) VillageLife.Active.gameObject.SetActive(false);
        }

        /// <summary>
        /// Open, dry, fairly level ground near the green with nothing standing on it or in front of it (where the camera stands,
        /// to the south): rings of candidates round the recovery point, the first clear one wins.
        /// </summary>
        static Vector2 FindSpot(Crulanda.World.ZoneBuilder zone, out float ground)
        {
            ground = 0; if (zone == null) return Vector2.zero;
            var home = zone.Zone.spawns.recovery; float half = zone.Zone.size / 2 - 20, width = (PerRow - 1) * Spacing + 3;
            for (int ring = 0; ring <= 8; ring++)
                for (int k = 0; k < (ring == 0 ? 1 : 12); k++)
                {
                    float a = k * 30 * Mathf.Deg2Rad; var c = home + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ring * 7;
                    if (Mathf.Abs(c.x) > half || Mathf.Abs(c.y) > half) continue;
                    if (Clear(zone, c, width, out ground)) return c;
                }
            ground = zone.HeightAt(home.x, home.y); return home;
        }
        static bool Clear(Crulanda.World.ZoneBuilder zone, Vector2 c, float width, out float ground)
        {
            float lo = float.MaxValue, hi = float.MinValue; ground = 0;
            for (int i = 0; i <= 6; i++)
                for (int j = 0; j <= 4; j++)
                {
                    var p = c + new Vector2(-width / 2 + width * i / 6, -9 + 10.5f * j / 4);   // the row (z 0) and the ground in front of it to the camera (z -9)
                    if (zone.WaterAt(p, out _, out _)) return false;
                    float h = zone.HeightAt(p.x, p.y); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
            if (hi - lo > 1.4f) return false;
            ground = zone.HeightAt(c.x, c.y);
            var box = new Vector3(c.x, hi + 1.8f, c.y - 3.5f); var halfSize = new Vector3(width / 2 + .5f, 1.4f, 5.5f);
            foreach (var col in Physics.OverlapBox(box, halfSize, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (col.bounds.size.x < 60 && col.bounds.size.z < 60 && col.GetComponentInParent<Crulanda.Gameplay.Actor>() == null) return false;
            return true;
        }

        /// <summary>One row of mannequins, each dressed in its entry, shot from the south.</summary>
        IEnumerator Shot(string file, List<Entry> row, float yaw, float hour)
        {
            Clear(); Crulanda.World.WorldClock.Hour = hour;
            var zone = Crulanda.World.ZoneBuilder.Active;
            int n = row.Count; float width = (PerRow - 1) * Spacing;
            for (int i = 0; i < n; i++)
            {
                var p = spot + new Vector2((i - (n - 1) / 2f) * Spacing, 0);
                var at = zone != null ? zone.Ground(p, 1) : new Vector3(p.x, 1, p.y);
                var go = new GameObject("Wardrobe mannequin " + row[i].title); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; body.transform.SetParent(go.transform, false); Destroy(body.GetComponent<Collider>());
                var look = ActorVisual.Attach(go, ActorLook.Warrior, i);
                look.ApplyGearIds(row[i].ids, db, looks);
                figures.Add(go); labels.Add((go.transform, row[i].title, row[i].sub));
            }
            var view = session.View; var focus = new Vector3(spot.x, ground + .95f, spot.y);
            float hHalf = Mathf.Atan(Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * view.aspect);
            float dist = (width / 2 + 1.2f) / Mathf.Tan(hHalf), pitch = 12;
            var cam = focus - Quaternion.Euler(pitch, 0, 0) * Vector3.forward * dist;
            if (zone != null) cam.y = Mathf.Max(cam.y, zone.HeightAt(cam.x, cam.z) + 1.3f);
            var turn = Quaternion.LookRotation(focus - cam);
            for (float w = 0; w < .9f; w += Time.deltaTime) { view.transform.SetPositionAndRotation(cam, turn); Crulanda.World.TreeFade.UpdateAll(cam, focus, focus); yield return null; }
            view.transform.SetPositionAndRotation(cam, turn);
            ScreenCapture.CaptureScreenshot(file);
            yield return new WaitForSeconds(.5f);
        }
        void Clear()
        {
            foreach (var f in figures) if (f != null) Destroy(f);
            figures.Clear(); labels.Clear();
        }

        void OnGUI()
        {
            if (labels.Count == 0 || session == null || session.View == null) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
                titleStyle.normal.textColor = Color.white;
                subStyle = new GUIStyle(titleStyle) { fontSize = 11, fontStyle = FontStyle.Normal }; subStyle.normal.textColor = new Color(.85f, .85f, .8f);
            }
            foreach (var (at, title, sub) in labels)
            {
                if (at == null) continue;
                var sp = session.View.WorldToScreenPoint(at.position + Vector3.down * 1.05f); if (sp.z < 0) continue;
                var r = new Rect(sp.x - 95, Screen.height - sp.y + 4, 190, 40);
                var old = GUI.color; GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
                GUI.Label(new Rect(r.x, r.y + 2, r.width, 20), title, titleStyle);
                GUI.Label(new Rect(r.x, r.y + 20, r.width, 20), sub, subStyle);
            }
        }
    }
}
