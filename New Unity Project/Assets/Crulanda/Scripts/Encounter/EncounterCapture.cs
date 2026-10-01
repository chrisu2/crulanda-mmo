using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>Opt-in rendered smoke run for automated validation of a standalone player.</summary>
    public sealed class EncounterCapture : MonoBehaviour
    {
        public EncounterSession session;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ConfigureCapture()
        {
            if (!Requested) return;
            var zoneArgs = Environment.GetCommandLineArgs(); int zi = Array.IndexOf(zoneArgs, "--crulanda-zone");
            if (zi >= 0 && zi + 1 < zoneArgs.Length) Crulanda.World.ZoneBuilder.RequestedZoneId = zoneArgs[zi + 1];
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
        public static bool Requested
        {
            get { var a = Environment.GetCommandLineArgs(); return Array.IndexOf(a, "--crulanda-capture") >= 0 || Array.IndexOf(a, "--crulanda-ui-capture") >= 0 || Array.IndexOf(a, "--crulanda-world-capture") >= 0; }
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int ui = Array.IndexOf(args, "--crulanda-ui-capture");
            if (ui >= 0 && ui + 1 < args.Length) { yield return CaptureInterface(args[ui + 1]); yield break; }
            int world = Array.IndexOf(args, "--crulanda-world-capture");
            if (world >= 0 && world + 1 < args.Length) { yield return CaptureWorldTour(args[world + 1]); yield break; }
            int i = Array.IndexOf(args, "--crulanda-capture");
            if (i < 0 || i + 1 >= args.Length) yield break;
            string directory = args[i + 1]; Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            CaptureWorld(Path.Combine(directory, "01-camp.png"));
            session.Interact();
            yield return new WaitForSeconds(1);
            var target = session.Enemies[0];
            session.Player.GetComponent<AdventurerMotor>().Teleport(target.transform.position + Vector3.back * 2.7f);
            session.Select(target); session.UseAbility(0);
            yield return new WaitForSeconds(3);
            CaptureWorld(Path.Combine(directory, "02-combat.png"));
            float deadline = Time.time + 40;
            while (target.actor.IsAlive && Time.time < deadline)
            { session.UseAbility(0); session.UseAbility(2); yield return new WaitForSeconds(.2f); }
            session.Player.GetComponent<AdventurerMotor>().Teleport(target.transform.position + Vector3.back * 2);
            session.Interact(); session.Equip();
            yield return new WaitForSeconds(1);
            CaptureWorld(Path.Combine(directory, "03-reward.png"));
            yield return new WaitForSeconds(2);
            if (target.actor.IsAlive || session.Progress.equippedItem != session.content.itemId)
            { Debug.LogError("Rendered encounter smoke run failed."); Application.Quit(1); }
            else { Debug.Log("RENDERED_ENCOUNTER_SMOKE_PASSED"); Application.Quit(0); }
        }
        /// <summary>
        /// Visible-window HUD capture (IMGUI included) on an isolated temp save: a level-10 Tank/DPS hybrid,
        /// the open talent panel, then the combat HUD. Must run windowed, not -batchmode.
        /// </summary>
        IEnumerator CaptureInterface(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(1.5f);
            session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10); session.Interact();
            bool druid = session.Druid != null; string prefix = druid ? "druid-" : "warrior-";
            var build = druid
                ? new[] { "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-heartwood-brace",
                    "ts-green-voice", "ts-green-voice", "ts-green-voice", "ts-green-voice", "ts-green-voice" }
                : new[] { "tk-hardened-grip", "tk-hardened-grip", "tk-hardened-grip", "tk-tempered-armor", "tk-tempered-armor",
                    "tk-intercept", "dp-weapon-pressure", "dp-weapon-pressure", "dp-read-the-opening", "dp-read-the-opening", "dp-read-the-opening" };
            foreach (var id in build) if (!session.ChangeTalent(id, 1)) Debug.LogError("UI capture could not buy " + id);
            session.BuildOpen = true;
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "01-talents.png"));
            yield return new WaitForSeconds(1);
            session.BuildOpen = false;
            var target = session.Enemies[0];
            session.Player.GetComponent<AdventurerMotor>().Teleport(target.transform.position + Vector3.back * (druid ? 8 : 2.5f));
            session.Select(target);
            if (druid)
            {
                session.UseAbility(4); yield return new WaitForSeconds(1.6f);   // Seedshot
                session.UseAbility(5); yield return new WaitForSeconds(1.0f);   // Thornbolt, mid-cast
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "02-thornsong.png"));
                yield return new WaitForSeconds(2f);
                session.UseAbility(0); yield return new WaitForSeconds(1.6f);   // Barkhide
                session.Player.GetComponent<AdventurerMotor>().Teleport(target.transform.position + Vector3.back * 2.5f);
                session.UseAbility(4); yield return new WaitForSeconds(3.5f);   // Bough Strike
            }
            else
            {
                session.UseAbility(0); yield return new WaitForSeconds(3.2f);
                session.UseAbility(2); yield return new WaitForSeconds(2.4f);
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "03-combat.png"));
            yield return new WaitForSeconds(.6f);
            session.MapOpen = true; yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "04-zone-map.png")); yield return new WaitForSeconds(.5f);
            session.MapWorld = true; yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "05-world-map.png")); yield return new WaitForSeconds(.5f);
            session.MapOpen = false;
            // The walk-in inn: open its door, look in from the doorstep, then from inside.
            var zone = Crulanda.World.ZoneBuilder.Active;
            var door = zone == null ? null : zone.Doors.Find(d => d.openable);
            if (door != null)
            {
                foreach (var e in session.Enemies) e.ResetFight();
                door.SetOpen(true);
                var motor = session.Player.GetComponent<AdventurerMotor>();
                var outward = (door.position - door.hinge.parent.position); outward.y = 0; outward.Normalize();
                motor.Teleport(door.position + outward * 4 + Vector3.up * .2f);
                float yaw = Quaternion.LookRotation(-outward).eulerAngles.y;
                motor.SetView(yaw, 12, 5); yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "06-inn-door.png")); yield return new WaitForSeconds(.5f);
                motor.Teleport(door.hinge.parent.position + Vector3.up * 1.1f - outward * .5f);
                motor.SetView(yaw + 30, 18, 4); yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "07-inn-inside.png")); yield return new WaitForSeconds(.5f);
            }
            if (session.Quests != null && zone != null && VillageLife.Active != null) { var q = CaptureQuests(directory, prefix, zone); while (q.MoveNext()) yield return q.Current; }
            if (session.Professions != null) { var t = CaptureTrades(directory, prefix); while (t.MoveNext()) yield return t.Current; }
            yield return new WaitForSeconds(1);
            Debug.Log("UI_CAPTURE_DONE"); Application.Quit(0);
        }
        /// <summary>The Trades window (K) beside the bags: a skill whose tool hangs at the belt, then one still wanting its tool.</summary>
        IEnumerator CaptureTrades(string directory, string prefix)
        {
            session.Conversation = null; session.QuestBookOpen = false; session.ReadingDocument = null;
            foreach (var (item, count) in new[] { ("tool.pick", 1), ("tool.hatchet", 1), ("mat.copper_ore", 7), ("mat.yarrow", 3), ("mat.flour", 2) }) Inventory.Add(session.Progress, session.Items, item, count);
            int pick = session.Progress.bag.FindIndex(s => s.item == "tool.pick"); if (pick >= 0) session.EquipFromBag(pick);
            EncounterHud.TradesPage = "mining"; session.ShowTrades(true); yield return new WaitForSeconds(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "17-trades.png")); yield return new WaitForSeconds(.4f);
            EncounterHud.TradesPage = "woodcutting"; yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "18-trades-no-tool.png")); yield return new WaitForSeconds(.4f);
            EncounterHud.TradesPage = null; session.ShowTrades(false); session.InventoryOpen = false;
        }
        /// <summary>The quest interface: a giver's !, the offer, the quest book, the ledger in the Chronicle, a hand-in list, standing, the tracker.</summary>
        IEnumerator CaptureQuests(string directory, string prefix, Crulanda.World.ZoneBuilder zone)
        {
            var motor = session.Player.GetComponent<AdventurerMotor>(); var life = VillageLife.Active; var log = session.Quests;
            Crulanda.World.WorldClock.Hour = 10;
            foreach (var e in session.Enemies) e.ResetFight();
            IEnumerator Face(Villager v, float distance)
            {
                v.StandAt(v.transform.position, 0);
                var at = v.transform.position; var from = at + Vector3.back * distance; from.y = zone.HeightAt(from.x, from.z) + 1.1f;
                motor.Teleport(from); motor.SetView(0, 8, 3.5f);
                v.transform.rotation = Quaternion.Euler(0, 180, 0);
                yield return new WaitForSeconds(.8f);
            }
            var goody = life.Find("Goody Marl");
            if (goody != null)
            {
                var f = Face(goody, 5f); while (f.MoveNext()) yield return f.Current;
                var body = Array.FindAll(session.Player.GetComponentsInChildren<Renderer>(), r => r.enabled);
                foreach (var r in body) r.enabled = false;   // look past ourselves at the quest giver
                yield return new WaitForSeconds(.2f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "08-quest-giver.png")); yield return new WaitForSeconds(.4f);
                foreach (var r in body) r.enabled = true;
                session.QuestTalk(goody.Name, goody.transform.position); yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "09-quest-offer.png")); yield return new WaitForSeconds(.4f);
                session.AcceptQuest(log.Def("npc.goody.eggs")); session.Conversation = null;
            }
            // Chapter one up to the wagon, then the ledger opens in the Chronicle.
            session.Progress.recruited = true;
            foreach (var e in session.Enemies) if (e.actor.IsAlive) e.actor.Health.ApplyDamage(100000);
            yield return null;
            session.Progress.Loot(session.Enemies[0].persistentId); Inventory.Add(session.Progress, session.Items, session.content.itemId, 1); session.Equip(); session.ReconcileQuests();
            EncounterHud.BookTab = "quests"; session.QuestBookOpen = true; yield return new WaitForSeconds(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "10-quest-book.png")); yield return new WaitForSeconds(.4f);
            session.QuestBookOpen = false;
            var wagon = zone.Interactables.Find(i => i.name == "Bureau wagon");
            if (wagon != null)
            {
                motor.Teleport(zone.Ground(new Vector2(wagon.position.x - 1.5f, wagon.position.z - 3.5f), 1.1f)); motor.SetView(20, 14, 5);
                yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "11-wagon.png")); yield return new WaitForSeconds(.4f);
                session.UseInteractable(wagon); yield return new WaitForSeconds(.6f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "12-chronicle-ledger.png")); yield return new WaitForSeconds(.4f);
                session.QuestBookOpen = false; session.ReadingDocument = null;
            }
            var corwin = life.Find("Corwin Ashby");
            if (corwin != null)
            {
                var f = Face(corwin, 4f); while (f.MoveNext()) yield return f.Current;
                session.QuestTalk(corwin.Name, corwin.transform.position); yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "13-quest-complete.png")); yield return new WaitForSeconds(.4f);
                session.CompleteQuest(log.Def("main.oakhaven.1")); yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "14-quest-list.png")); yield return new WaitForSeconds(.4f);
                session.AcceptQuest(log.Def("main.oakhaven.2")); session.Conversation = null;
            }
            EncounterHud.BookTab = "standing"; session.QuestBookOpen = true; yield return new WaitForSeconds(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "15-standing.png")); yield return new WaitForSeconds(.4f);
            session.QuestBookOpen = false; EncounterHud.BookTab = "quests";
            var sel = life.Find("Sel Harrow");
            if (sel != null) { var f = Face(sel, 7f); while (f.MoveNext()) yield return f.Current; }
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "16-tracker.png")); yield return new WaitForSeconds(.4f);
        }
        /// <summary>Scenic tour of a generated zone with the HUD hidden: one shot per (place, yaw, pitch, zoom).</summary>
        IEnumerator CaptureWorldTour(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            var zone = Crulanda.World.ZoneBuilder.Active;
            var motor = session.Player.GetComponent<AdventurerMotor>();
            // Generic tour: the entrance, then each landmark, then the first exit. A landmark with an authored view is shot from
            // it as authored (facing the landmark, its own pitch and zoom). Any other landmark (and the exit) is shot from a
            // searched viewpoint with the player out of frame (LandmarkView); when none is clear, the old orbit framing from
            // its south-west (the exit's from the north-east) stands in.
            var shots = new List<(string name, Vector2 at, float yaw, float pitch, float zoom, Vector2 mark, float radius, float bearing, Transform root)>();
            if (zone != null)
            {
                var z = zone.Zone; shots.Add(("01-entrance", z.spawns.player, z.spawns.playerFacing, 16, 12, Vector2.zero, 0, 0, null));
                int n = 2;
                foreach (var l in z.landmarks)
                {
                    var from = l.at + new Vector2(-.6f, -.8f) * Mathf.Min(l.radius + 4, 14);
                    float lim = z.size / 2 - 10; from = new Vector2(Mathf.Clamp(from.x, -lim, lim), Mathf.Clamp(from.y, -lim, lim));
                    bool authored = l.view != Vector2.zero; if (authored) from = l.view;   // the side that reads: a door, the grey, a view across the water
                    var dir = l.at - from; float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                    // A prop carrying the landmark's name is framed on its own extent, and a building from its front (-Z), turned
                    // a little for a three-quarter view; anything else from the south-west. Authored views skip the search (radius 0).
                    var prop = Array.Find(z.props, p => p != null && p.name == l.name);
                    Transform root = null;
                    if (prop != null) { root = zone.transform.Find("Zone props/" + prop.name); if (root == null) root = zone.transform.Find("Zone static scenery/" + prop.name); }
                    float bearing = prop != null && Array.IndexOf(Fronted, prop.kind) >= 0 ? prop.rotation + 205 : Mathf.Atan2(-.6f, -.8f) * Mathf.Rad2Deg;
                    shots.Add(((n++).ToString("00") + "-" + l.name.ToLowerInvariant().Replace(' ', '-').Replace("'", ""), from, yaw, l.viewPitch > 0 ? l.viewPitch : 17, l.viewZoom > 0 ? l.viewZoom : 11,
                        l.at, authored ? 0 : l.radius, bearing, root));
                }
                if (z.exits.Length > 0)
                {
                    var x = z.exits[0];   // its waystone, seen from the zone's side looking out along the road
                    shots.Add(((n++).ToString("00") + "-exit", x.at + new Vector2(4, 4), 225, 15, 10, x.at, Mathf.Max(x.radius, 4), Mathf.Atan2(-x.at.x, -x.at.y) * Mathf.Rad2Deg,
                        zone.transform.Find("Zone static scenery/Exit: " + x.name)));
                }
            }
            // Every tree's crown (its renderers' bounds): the camera keeps out of them, and they are counted in a sight line.
            var crowns = new List<Bounds>();
            foreach (var tf in Crulanda.World.TreeFade.All)
            {
                var parts = tf == null ? null : tf.GetComponentsInChildren<MeshRenderer>(); if (parts == null || parts.Length == 0) continue;
                var cb = parts[0].bounds; foreach (var r in parts) cb.Encapsulate(r.bounds); crowns.Add(cb);
            }
            EncounterHud.Hidden = true;
            foreach (var s in shots)
            {
                string file = Path.Combine(directory, (zone != null ? zone.Zone.id.Replace("zone.", "") : "world") + "-" + s.name + ".png");
                if (zone != null && s.radius > 0 && LandmarkView(zone, s.mark, s.radius, s.bearing, s.root, crowns, out var cam, out var focus, out var near))
                {
                    // The camera goes straight to the viewpoint (the orbit rests); the player waits hidden just behind it, and
                    // trees short of the landmark fade as they would between the camera and the player.
                    var back = new Vector2(cam.x - focus.x, cam.z - focus.z).normalized;
                    motor.Teleport(zone.Ground(new Vector2(cam.x, cam.z) + back * 2, 1.1f));
                    var body = Array.FindAll(session.Player.GetComponentsInChildren<Renderer>(), r => r.enabled);
                    foreach (var r in body) r.enabled = false;
                    motor.enabled = false; var view = session.View.transform; var look = Quaternion.LookRotation(focus - cam);
                    for (float w = 0; w < .6f; w += Time.deltaTime) { view.SetPositionAndRotation(cam, look); Crulanda.World.TreeFade.UpdateAll(cam, near, near); yield return null; }
                    view.SetPositionAndRotation(cam, look); ScreenCapture.CaptureScreenshot(file);
                    yield return new WaitForSeconds(.4f);
                    motor.enabled = true; foreach (var r in body) r.enabled = true;
                    continue;
                }
                if (s.radius > 0) Debug.Log("World tour " + s.name + ": no clear viewpoint, orbit framing");
                motor.Teleport(zone != null ? zone.Ground(s.at, 1.1f) : new Vector3(s.at.x, 1.1f, s.at.y));
                motor.SetView(s.yaw, s.pitch, s.zoom);
                yield return new WaitForSeconds(.6f);
                ScreenCapture.CaptureScreenshot(file);
                yield return new WaitForSeconds(.4f);
            }
            // Day and night at the first hen coop: the hen-wife's dusk round-up, then the village after dark.
            if (zone != null && zone.Coops.Count > 0)
            {
                string prefix = zone.Zone.id.Replace("zone.", "") + "-";
                var coop = zone.Coops[0]; var yard = new Vector2(coop.yard.x, coop.yard.z);
                // Stand somewhere clear with a view of the ramp (not inside the farmhouse).
                var from = yard + new Vector2(0, -8);
                foreach (var offset in new[] { new Vector2(3.5f, -2.5f), new Vector2(-3.5f, -2.5f), new Vector2(0, -4), new Vector2(4, -8), new Vector2(-4, -8), new Vector2(0, -10) })
                {
                    var at = zone.Ground(yard + offset, 1.7f);
                    if (!Physics.CheckSphere(at, 1.2f) &&!Physics.Linecast(at + Vector3.up, coop.door + Vector3.up)) { from = yard + offset; break; }
                }
                var dir = new Vector2(coop.door.x, coop.door.z) - from;
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                var hours = new[] { (8.6f, "90-coop-morning", 2f), (18.95f, "91-coop-dusk", 7f), (19.9f, "92-coop-roundup", 9f), (20.6f, "93-coop-shut", 12f), (23f, "94-coop-night", 2f) };
                foreach (var h in hours)
                {
                    Crulanda.World.WorldClock.Hour = h.Item1;
                    motor.Teleport(zone.Ground(from, 1.1f)); motor.SetView(yaw, 22, 7);
                    yield return new WaitForSeconds(h.Item3);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + h.Item2 + ".png"));
                    yield return new WaitForSeconds(.4f);
                }
                Crulanda.World.WorldClock.Hour = 22.5f;
                var green = zone.Zone.spawns.recovery;
                motor.Teleport(zone.Ground(green + new Vector2(-10, -16), 1.1f)); motor.SetView(30, 14, 12);
                yield return new WaitForSeconds(1.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "95-village-night.png"));
                yield return new WaitForSeconds(.4f);
                Crulanda.World.WorldClock.Hour = 6.4f;
                yield return new WaitForSeconds(1f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "96-village-dawn.png"));
                yield return new WaitForSeconds(.4f);
            }
            // A zone with no coop (the Verdant Shore): its heart after dark anyway, lamps lit (Rootfast's treehouse lanterns, the
            // glade's glowing caps and flowers), from the recovery point, then dawn.
            if (zone != null && zone.Coops.Count == 0)
            {
                string prefix = zone.Zone.id.Replace("zone.", "") + "-";
                EncounterHud.Hidden = true;
                foreach (var (name, hour, shot) in new[] { ("Rootfast", 21.5f, "95-village-night"), ("The Whispering Glade", 22.5f, "94-glade-night"), ("Rootfast", 6.4f, "96-village-dawn") })
                {
                    var l = Array.Find(zone.Zone.landmarks, x => x != null && x.name == name); if (l == null) continue;
                    Crulanda.World.WorldClock.Hour = hour;
                    var at = l.view != Vector2.zero ? l.view : l.at + new Vector2(0, -(l.radius + 10));
                    motor.Teleport(zone.Ground(at, 1.1f)); var look = l.at - at; motor.SetView(Mathf.Atan2(look.x, look.y) * Mathf.Rad2Deg, l.viewPitch > 0 ? l.viewPitch : 14, l.viewZoom > 0 ? l.viewZoom : 11);
                    yield return new WaitForSeconds(1.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + shot + ".png")); yield return new WaitForSeconds(.4f);
                }
                Crulanda.World.WorldClock.Hour = 11; EncounterHud.Hidden = false;
            }
            // The trades: each at their workplace, then one of every trade lined up on the green with nameplates showing.
            var life = VillageLife.Active;
            if (zone != null && life != null)
            {
                string prefix = zone.Zone.id.Replace("zone.", "") + "-";
                Crulanda.World.WorldClock.Hour = 11;
                EncounterHud.Hidden = false;
                var playerRenderers = Array.FindAll(session.Player.GetComponentsInChildren<Renderer>(), r => r.enabled);
                foreach (var r in playerRenderers) r.enabled = false;   // the camera looks past where we stand
                var jobs = new[] { ("blacksmith", "forge"), ("merchant", "stall"), ("baker", "oven"), ("leatherworker", "tannery"), ("lumberjack", "woodpile") };
                foreach (var job in jobs)
                {
                    Villager worker = null;
                    foreach (var v in life.Villagers) if (v.Role == job.Item1) { worker = v; break; }
                    if (worker == null || !worker.WorkAt(job.Item2)) continue;
                    var at = worker.transform.position; var facing = worker.transform.forward; facing.y = 0; facing.Normalize();
                    var side = Vector3.Cross(Vector3.up, facing);
                    var stand = at + side * 3.6f + facing * .6f;   // from the side, so both the worker and the work show
                    var d = at - stand;
                    motor.Teleport(zone.Ground(new Vector2(stand.x, stand.z), 1.1f)); motor.SetView(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 12, 1.5f);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "97-trade-" + job.Item1 + ".png"));
                    yield return new WaitForSeconds(.4f);
                }
                var seen = new System.Collections.Generic.HashSet<string>(); var line = new System.Collections.Generic.List<Villager>();
                foreach (var v in life.Villagers) if (v.Title != null && seen.Add(v.Role)) line.Add(v);
                var before = line.ConvertAll(v => v.transform.position);
                // Everyone else steps out of sight for the line-ups (a drinker wandered into the errands shot), back after.
                var bystanders = life.Villagers.FindAll(v => !v.Resident && !line.Contains(v)); var bystanderAt = bystanders.ConvertAll(v => v.transform.position);
                foreach (var v in bystanders) v.Park();
                // Groups of four in a row facing the camera, so nameplates don't overlap.
                for (int g = 0; g * 4 < line.Count; g++)
                {
                    for (int i = g * 4; i < Mathf.Min(line.Count, g * 4 + 4); i++)
                        line[i].StandAt(zone.Ground(new Vector2((i - g * 4 - 1.5f) * 2.6f, -8)), 180);
                    // The others step out of sight (a parking row at -40,-40 turned up in the water shots, heaped on one bank).
                    for (int i = 0; i < line.Count; i++) if (i < g * 4 || i >= g * 4 + 4) line[i].Park();
                    motor.Teleport(zone.Ground(new Vector2(0, -15), 1.1f)); motor.SetView(0, 6, 3.2f);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "98-trades-lineup-" + (g + 1) + ".png"));
                    yield return new WaitForSeconds(.4f);
                }
                for (int i = 0; i < line.Count; i++) line[i].Release(before[i]);   // back to their day, from where they were
                // The errands: one of each trade with what they carry between the trades (eggs, water, grain, flour, bread, logs, a hide...).
                var loads = new[] { ("henwife", Load.Eggs, 5), ("farmer", Load.Grain, 1), ("miller", Load.Flour, 1), ("baker", Load.Bread, 3), ("lumberjack", Load.Logs, 3), ("hunter", Load.Game, 1), ("herbalist", Load.Herbs, 1), ("gossip", Load.Bucket, 1), ("skinner", Load.Hide, 1), ("blacksmith", Load.Goods, 1), ("child", Load.Bucket, 1), ("leatherworker", Load.Goods, 1) };
                var carriers = new System.Collections.Generic.List<(Villager, Load, int)>();
                foreach (var (role, what, count) in loads) { var v = life.Villagers.Find(x => x.Role == role && !x.Resident); if (v != null) carriers.Add((v, what, count)); }
                var wasAt = carriers.ConvertAll(c => c.Item1.transform.position);
                for (int g = 0; g * 4 < carriers.Count; g++)
                {
                    for (int i = 0; i < carriers.Count; i++)
                    {
                        if (i < g * 4 || i >= g * 4 + 4) { carriers[i].Item1.Park(); continue; }
                        carriers[i].Item1.StandAt(zone.Ground(new Vector2((i - g * 4 - 1.5f) * 2.6f, -8)), 180); carriers[i].Item1.ShowLoad(carriers[i].Item2, carriers[i].Item3);
                    }
                    motor.Teleport(zone.Ground(new Vector2(0, -15), 1.1f)); motor.SetView(0, 6, 3.2f);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-errands-lineup-" + (g + 1) + ".png"));
                    yield return new WaitForSeconds(.4f);
                }
                for (int i = 0; i < carriers.Count; i++) carriers[i].Item1.Release(wasAt[i]);
                // The out of work at the inn: one with a tankard, the next slumped over the table, seen from the table by the door.
                if (life.Places.TryGetValue("inn", out var seats) && seats.Count >= 8)
                {
                    var drinkers = life.Villagers.FindAll(v => v.Role == "drinker" && !v.Resident);
                    if (drinkers.Count > 0)
                    {
                        var drinkerAt = drinkers.ConvertAll(v => v.transform.position);
                        for (int i = 0; i < drinkers.Count; i++) drinkers[i].PoseAtInn(i, i % 2 == 1);
                        // The camera is set by hand (the orbit camera backs out through the wall from a seat): at head height over the
                        // far table, looking at the drinkers' table.
                        var innDoor = zone.Doors.Find(d => d.openable && d.hinge != null); var innRoom = innDoor != null ? innDoor.hinge.parent : null;
                        var table = (drinkers[0].transform.position + drinkers[Mathf.Min(1, drinkers.Count - 1)].transform.position) / 2 + Vector3.up * .2f;
                        var eye = innRoom != null ? innRoom.TransformPoint(new Vector3(-3.6f, 1.75f, 1.4f)) : seats[7] + Vector3.up * 1.7f;
                        motor.Teleport(seats[7] + Vector3.up * 1.1f); motor.enabled = false;
                        var lens = Camera.main.transform;
                        for (float t = 0; t < 1.5f; t += Time.deltaTime) { lens.position = eye; lens.rotation = Quaternion.LookRotation(table - eye); yield return null; }
                        ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-inn-drinkers.png"));
                        for (float t = 0; t < .4f; t += Time.deltaTime) { lens.position = eye; lens.rotation = Quaternion.LookRotation(table - eye); yield return null; }
                        motor.enabled = true;
                        for (int i = 0; i < drinkers.Count; i++) drinkers[i].Release(drinkerAt[i]);
                    }
                }
                for (int i = 0; i < bystanders.Count; i++) bystanders[i].Release(bystanderAt[i]);
                foreach (var r in playerRenderers) r.enabled = true;
            }
            // Nature: swimming in the first lake, then walking up on an ambush camp until something leaps out of the grass.
            if (zone != null)
            {
                string prefix = zone.Zone.id.Replace("zone.", "") + "-";
                Crulanda.World.WorldClock.Hour = 15;
                // Water close-ups: a creek from its bank at eye level, looking along it; the first bridge; the pond by day,
                // swimming, and at night.
                if (zone.Water.Creeks.Count > 0)
                {
                    var c = zone.Water.Creeks[0]; int i = c.pts.Length / 3; var dir = (c.pts[i + 1] - c.pts[i]).normalized; var side = new Vector2(-dir.y, dir.x);
                    var bank = c.pts[i] + side * (c.width + 1.5f);
                    motor.Teleport(zone.Ground(bank, 1.1f)); var look = c.pts[i + 4] - bank; motor.SetView(Mathf.Atan2(look.x, look.y) * Mathf.Rad2Deg, 10, 4);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-creek.png")); yield return new WaitForSeconds(.4f);
                    motor.Teleport(zone.Ground(c.pts[i], 1.1f)); motor.SetView(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 22, 6);
                    yield return new WaitForSeconds(1.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-wading.png")); yield return new WaitForSeconds(.4f);
                }
                var bridgeProp = System.Array.Find(zone.Zone.props, pr => pr != null && pr.kind == "bridge");
                if (bridgeProp != null)
                {
                    var from = bridgeProp.at + new Vector2(8, -6);
                    motor.Teleport(zone.Ground(from, 1.1f)); var look = bridgeProp.at - from; motor.SetView(Mathf.Atan2(look.x, look.y) * Mathf.Rad2Deg, 14, 5);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-bridge.png")); yield return new WaitForSeconds(.4f);
                }
                if (zone.Water.Lakes.Count > 0)
                {
                    var l = zone.Water.Lakes[0];
                    var shore = l.def.center + new Vector2(0, -(l.radius + 5));
                    motor.Teleport(zone.Ground(shore, 1.1f)); motor.SetView(0, 18, 7);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-pond.png")); yield return new WaitForSeconds(.4f);
                    motor.Teleport(new Vector3(l.def.center.x + l.radius * .3f, l.level - .3f, l.def.center.y));
                    motor.SetView(200, 16, 6);
                    yield return new WaitForSeconds(2.5f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-swimming.png")); yield return new WaitForSeconds(.4f);
                    Crulanda.World.WorldClock.Hour = 22.5f;
                    motor.Teleport(zone.Ground(shore, 1.1f)); motor.SetView(0, 18, 7);
                    yield return new WaitForSeconds(3f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-pond-night.png")); yield return new WaitForSeconds(.4f);
                    Crulanda.World.WorldClock.Hour = 15;
                }
                var camp = zone.Zone.camps == null ? null : System.Array.Find(zone.Zone.camps, c => c != null && c.ambush);
                if (camp != null)
                {
                    var from = camp.center + new Vector2(0, -(camp.radius + 12));
                    motor.Teleport(zone.Ground(from, 1.1f)); motor.SetView(0, 14, 7);
                    yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-ambush-before.png")); yield return new WaitForSeconds(.4f);
                    motor.Teleport(zone.Ground(camp.center + new Vector2(0, -camp.radius * .6f), 1.1f));
                    yield return new WaitForSeconds(.9f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-ambush-sprung.png")); yield return new WaitForSeconds(.4f);
                }
                // Targeting: a camp enemy selected, with the ring under it and its nameplate boxed.
                var mob = session.Enemies.Find(e => e != null && e.actor.IsAlive && !e.Hidden && e.persistentId != null && e.persistentId.StartsWith("mob."));
                if (mob != null)
                {
                    var mp = mob.transform.position;
                    motor.Teleport(zone.Ground(new Vector2(mp.x, mp.z - 5), 1.1f)); motor.SetView(0, 34, 7); session.Select(mob);
                    yield return new WaitForSeconds(.6f);   // before it wanders or closes in
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-target-ring.png")); yield return new WaitForSeconds(.4f);
                    session.Select(null);
                }
                // The camera among trees: stand just past a tree with the camera behind it; the tree fades instead of hiding you.
                var tree = Crulanda.World.TreeFade.All.Find(t => t != null && new Vector2(t.transform.position.x, t.transform.position.z).magnitude < 90);
                if (tree != null)
                {
                    var tp = tree.transform.position;
                    motor.Teleport(zone.Ground(new Vector2(tp.x, tp.z + 3.5f), 1.1f)); motor.SetView(0, 12, 7);
                    yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "99-tree-fade.png")); yield return new WaitForSeconds(.4f);
                }
                // Weather: each of the zone's unsettled kinds at midday from the entrance (the land, the sky and the air), rain on
                // the pond, and the calm tour weather again after. Every other shot holds the calm weather (WorldWeather.TourKind).
                var weather = Crulanda.World.WorldWeather.Active;
                if (weather != null)
                {
                    EncounterHud.Hidden = true; Crulanda.World.WorldClock.Hour = 11.5f;
                    var z = zone.Zone;
                    foreach (var kind in weather.Showcase())
                    {
                        weather.Force(kind, true);
                        string name = Crulanda.World.WeatherSchedule.Name(kind).ToLowerInvariant().Replace(' ', '-');
                        motor.Teleport(zone.Ground(z.spawns.player, 1.1f)); motor.SetView(z.spawns.playerFacing, 9, 12);
                        yield return new WaitForSeconds(4);
                        while (Crulanda.World.WorldWeather.Flash > .01f) yield return null;   // not mid-lightning
                        ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "80-weather-" + name + ".png")); yield return new WaitForSeconds(.4f);
                        if ((kind == Crulanda.World.WeatherKind.Rain || kind == Crulanda.World.WeatherKind.Storm) && zone.Water.Lakes.Count > 0)
                        {
                            var l = zone.Water.Lakes[0];
                            motor.Teleport(zone.Ground(l.def.center + new Vector2(0, -(l.radius + 4)), 1.1f)); motor.SetView(0, 24, 6);
                            yield return new WaitForSeconds(2);
                            ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "81-weather-" + name + "-pond.png")); yield return new WaitForSeconds(.4f);
                        }
                    }
                    weather.Force(weather.TourKind(), true);
                }
                // Crowsfoot Hollow, the first dungeon: the camp chamber from its way in, the head of the Drop looking down it, the Store
                // Caves from the foot of the Drop, and the Echoing Hall from the Deep Stair.
                foreach (var hollow in Crulanda.World.Hollow.All)
                {
                    string hs = hollow == Crulanda.World.Hollow.All[0] ? "" : "-" + hollow.Name.ToLowerInvariant().Replace("'", "").Replace(" ", "-");   // a second cave in a zone keeps its name in the shot
                    EncounterHud.Hidden = true; Crulanda.World.WorldClock.Hour = 15;
                    // The band holds still for the pictures (they would come for you, a level-1 tourist, all the way down), and you
                    // start whole.
                    foreach (var e in session.Enemies) if (e != null) e.enabled = false;
                    session.Player.Health.ApplyHealing(session.Player.Health.Pool.Max);
                    float drop = -1, stores = -1, widest = 0;
                    for (int i = 0; i + 3 < hollow.Centre.Count && drop < 0; i++) if (hollow.Centre[i].y - hollow.Centre[i + 3].y > .7f) drop = hollow.Along[i];   // where the floor first falls away
                    for (int i = 0; i < hollow.Centre.Count; i++)
                        if (hollow.Along[i] > hollow.Length * .45f && hollow.Along[i] < hollow.Length * .7f && hollow.Half[i] > widest) { widest = hollow.Half[i]; stores = hollow.Along[i]; }
                    // A cave whose floor falls away from the mouth itself (the Root-Mother's Deep, down its root-stair) has no camp by
                    // the way in, and the head of that stair is out in the daylight: both views stood at the mouth, the camera outside
                    // it. Its first picture is its first chamber (the widest ring of the first two fifths: the Root Gallery) from just
                    // inside it, and its drop the steepest stretch past that chamber (the Cold Stair), from its head.
                    float campAt = 6.5f, campTo = 12.5f, campPitch = 10, dropPitch = 24;
                    if (drop >= 0 && drop < 12.5f)
                    {
                        float first = -1, steep = .01f; widest = 0; drop = -1;
                        for (int i = 0; i < hollow.Centre.Count; i++)
                            if (hollow.Along[i] > hollow.Length * .12f && hollow.Along[i] < hollow.Length * .4f && hollow.Half[i] > widest) { widest = hollow.Half[i]; first = hollow.Along[i]; }
                        for (int i = 0; first > 0 && i + 3 < hollow.Centre.Count; i++)
                            if (hollow.Along[i] > first + 8 && hollow.Centre[i].y - hollow.Centre[i + 3].y > steep + .01f) { steep = hollow.Centre[i].y - hollow.Centre[i + 3].y; drop = hollow.Along[i]; }
                        if (first > 0) { campAt = first - 4; campTo = first + 4; campPitch = 16; }   // from the stair's foot, over the stair behind
                        dropPitch = 18;   // under the Sap Well's roof
                    }
                    var views = new List<(float from, float toward, float pitch, string shot)> { (campAt, campTo, campPitch, "85-hollow-camp" + hs) };
                    if (drop > 0) views.Add((drop - 2.5f, drop + 7, dropPitch, "88-hollow-drop" + hs));
                    if (stores > 0) views.Add((stores - 8, stores + 1, 10, "89-hollow-stores" + hs));
                    views.Add((hollow.Length - 13, hollow.Length - 5, 10, "86-hollow-hall" + hs));
                    foreach (var (from, toward, pitch, shot) in views)
                    {
                        var stand = hollow.At(from); var ahead = hollow.At(toward) - stand;
                        motor.Teleport(stand + Vector3.up * 1.1f); motor.SetView(Mathf.Atan2(ahead.x, ahead.z) * Mathf.Rad2Deg, pitch, 5);
                        yield return new WaitForSeconds(1.2f);
                        ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + shot + ".png")); yield return new WaitForSeconds(.4f);
                    }
                }
                // A hidden find up close, one a zone (its first chest, cache, herb, note or key): from the side you come to it (its
                // frame's -Z), a little above, the player out of frame and the HUD hidden. Secrets are on no map: this is their picture.
                Crulanda.World.ZoneSecretSpot find = null;
                foreach (var kind in new[] { "chest", "cache", "herb", "note", "key" })
                    if (find == null) find = zone.Secrets.Find(sp => sp != null && sp.def != null && sp.def.kind == kind && sp.root != null);
                if (find != null)
                {
                    EncounterHud.Hidden = true; Crulanda.World.WorldClock.Hour = 15;
                    var focus = find.position + Vector3.up * .15f; var back = -find.root.forward; back.y = 0; back.Normalize();
                    Vector3 From(float turn)
                    {
                        var c = focus + Quaternion.Euler(0, turn, 0) * back * 2.4f + Vector3.up * 1.1f;
                        c.y = Mathf.Max(c.y, zone.StandAt(new Vector2(c.x, c.z), focus.y).y + 1.2f); return c;
                    }
                    var shotFrom = From(0);
                    for (int k = 1; k < 8 && Physics.CheckSphere(shotFrom, .35f, ~0, QueryTriggerInteraction.Ignore); k++) shotFrom = From((k + 1) / 2 * (k % 2 == 1 ? 35 : -35));   // in the open: turn round it
                    var body = Array.FindAll(session.Player.GetComponentsInChildren<Renderer>(), r => r.enabled);
                    motor.Teleport(zone.StandAt(new Vector2(shotFrom.x + back.x * 2, shotFrom.z + back.z * 2), focus.y, 1.1f));
                    foreach (var r in body) r.enabled = false;
                    motor.enabled = false; var view = session.View.transform; var look = Quaternion.LookRotation(focus - shotFrom);
                    for (float w = 0; w < .8f; w += Time.deltaTime) { view.SetPositionAndRotation(shotFrom, look); Crulanda.World.TreeFade.UpdateAll(shotFrom, focus, focus); yield return null; }
                    view.SetPositionAndRotation(shotFrom, look);
                    string slug = find.def.id.Substring(find.def.id.LastIndexOf('.') + 1);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "87-secret-" + slug + ".png")); yield return new WaitForSeconds(.4f);
                    motor.enabled = true; foreach (var r in body) r.enabled = true;
                }
            }
            EncounterHud.Hidden = false;
            Debug.Log("WORLD_CAPTURE_DONE"); Application.Quit(0);
        }
        /// <summary>Prop kinds whose front (door or open side) faces local -Z: their landmark shots look at that side.</summary>
        static readonly string[] Fronted = { "house", "inn", "barn", "mill", "coop", "forge", "stall", "oven", "tannery", "woodpile", "wagon", "crypt" };
        /// <summary>
        /// A viewpoint for a landmark shot. Sizes the landmark (its named prop's renderers, else the scenery colliders and tree
        /// crowns in its circle), then tries 16 bearings round it, the preferred one first and turning away both ways, at three
        /// distances that fit its footprint and height into the middle of the frame. A spot counts when it is inside the zone
        /// and in open air (not in scenery or a crown, above any water) and sees the landmark's middle with nothing solid short
        /// of the landmark itself. The best wins: nearest the preferred bearing and distance, fewest trees in the sight line
        /// (they fade), the landmark's foot in view, no roof under the camera. <paramref name="near"/> is where the sight line
        /// reaches the landmark (a prop's own bounds, else its circle or the first of its trees), so every tree short of it
        /// fades and the landmark's own trees (the Great Oak, a wood) never do. False when nothing is clear.
        /// </summary>
        bool LandmarkView(Crulanda.World.ZoneBuilder zone, Vector2 at, float radius, float bearing, Transform root, List<Bounds> crowns, out Vector3 camera, out Vector3 focus, out Vector3 near)
        {
            camera = near = Vector3.zero;
            float reach = Mathf.Clamp(radius, 4, 18), top = float.MinValue;
            var own = root != null ? root.GetComponentsInChildren<MeshRenderer>() : new MeshRenderer[0];
            var ob = new Bounds(); if (own.Length == 0) root = null;   // nothing drawn under that name: its circle stands in
            if (root != null)
            {
                ob = own[0].bounds; foreach (var r in own) ob.Encapsulate(r.bounds);
                at = new Vector2(ob.center.x, ob.center.z); top = ob.max.y; reach = Mathf.Clamp(Mathf.Max(ob.extents.x, ob.extents.z) + 1, 4, 22);
                ob.Expand(.3f);   // padded as TreeFade pads a crown, so a landmark that is a tree stays solid
            }
            float ground = zone.HeightAt(at.x, at.y);
            if (zone.WaterAt(at, out float surface, out _)) ground = Mathf.Max(ground, surface);   // a pond: its surface, not its bed
            if (root == null)
            {
                foreach (var c in Physics.OverlapSphere(new Vector3(at.x, ground, at.y), reach, ~0, QueryTriggerInteraction.Ignore))
                    if (c.bounds.size.x < 60 && c.bounds.size.z < 60 && !Ignored(c)) top = Mathf.Max(top, c.bounds.max.y);
                foreach (var cb in crowns) if (Flat(cb.center, at) < reach) top = Mathf.Max(top, cb.max.y);
            }
            float height = Mathf.Clamp(top - ground, 2, 24);
            focus = new Vector3(at.x, ground + Mathf.Clamp(height * .5f, 1.2f, 10), at.y);
            // Distance: the footprint across three quarters of the frame's width, the height within 70% of it. Tall landmarks
            // are seen from lower down, so their tops stay in frame.
            float tanV = Mathf.Tan(session.View.fieldOfView * .5f * Mathf.Deg2Rad), tanH = tanV * session.View.aspect;
            float fit = Mathf.Clamp(Mathf.Max(reach / (.75f * tanH), height * .5f / (.7f * tanV), reach + 3, 10), 10, 38);
            var distances = new[] { fit, Mathf.Min(fit * 1.3f, 46), Mathf.Max(fit * .8f, reach + 3) };
            float rise = Mathf.Tan(Mathf.Clamp(22 - height * .6f, 8, 20) * Mathf.Deg2Rad), lim = zone.Zone.size / 2 - 12, best = float.MaxValue;
            var mid = at; var nearby = crowns.FindAll(q => Flat(q.center, mid) < distances[1] + 12);
            for (int k = 0; k < 16; k++)
            {
                float turn = (k + 1) / 2 * (k % 2 == 1 ? -22.5f : 22.5f), a = (bearing + turn) * Mathf.Deg2Rad;
                var outward = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                for (int j = 0; j < distances.Length; j++)
                {
                    float score = Mathf.Abs(turn) / 45 + j * .6f; if (score >= best) continue;
                    var p = at + outward * distances[j];
                    if (Mathf.Abs(p.x) > lim || Mathf.Abs(p.y) > lim) continue;
                    float floor = zone.HeightAt(p.x, p.y) + 2.2f;
                    if (zone.WaterAt(p, out float s, out _)) floor = Mathf.Max(floor, s + 2);
                    var cam = new Vector3(p.x, Mathf.Max(focus.y + distances[j] * rise, floor), p.y);
                    var sight = focus - cam; float length = sight.magnitude; sight /= length;
                    if (sight.y < -.64f) continue;   // steeper than 40 degrees down: perched on a rise over it
                    // Open air (not in scenery or within half a metre of a crown), and the landmark's middle in clear view.
                    bool shut = false;
                    foreach (var c in Physics.OverlapSphere(cam, .8f, ~0, QueryTriggerInteraction.Ignore)) if (!Ignored(c)) { shut = true; break; }
                    foreach (var cb in nearby) if (cb.SqrDistance(cam) < .25f) { shut = true; break; }
                    if (shut || Blocked(cam, focus, at, reach, root, .4f) || UnderWater(zone, cam, focus)) continue;
                    // The rest costs: the landmark's foot hidden, a roof or rock under the camera, each tree short of the landmark.
                    if (Blocked(cam, new Vector3(at.x, ground + .6f, at.y), at, reach, root, .25f)) score += 1.5f;
                    if (Physics.Raycast(cam, Vector3.down, out var under, 60, ~0, QueryTriggerInteraction.Ignore) && under.collider.bounds.size.x < 60 && !Ignored(under.collider)) score += 1;
                    var ray = new Ray(cam, sight); float gap = length * (1 - reach / distances[j]);
                    if (root != null) { if (ob.IntersectRay(ray, out float enter)) gap = enter; }
                    else foreach (var cb in nearby) if (Flat(cb.center, at) < reach) { var e = cb; e.Expand(.3f); if (e.IntersectRay(ray, out float hit)) gap = Mathf.Min(gap, hit); }
                    foreach (var cb in nearby) if (root != null || Flat(cb.center, at) >= reach) { var e = cb; e.Expand(.3f); if (e.IntersectRay(ray, out float hit) && hit < gap - .5f) score += 1; }
                    if (score < best) { best = score; camera = cam; near = cam + sight * gap; }
                }
            }
            return best < float.MaxValue;
        }
        /// <summary>
        /// True when scenery stops a sphere swept from a to b short of the landmark. Characters and trees never count (trees
        /// fade). For a prop only the prop itself and the ground in its circle may be in the way; otherwise anything in its circle.
        /// </summary>
        static bool Blocked(Vector3 a, Vector3 b, Vector2 at, float reach, Transform root, float radius)
        {
            var d = b - a; float length = d.magnitude;
            foreach (var hit in Physics.SphereCastAll(a, radius, d / length, length, ~0, QueryTriggerInteraction.Ignore))
            {
                var c = hit.collider; if (Ignored(c) || root != null && c.transform.IsChildOf(root)) continue;
                if (hit.distance <= 0 || Flat(hit.point, at) > reach || root != null && c.bounds.size.x < 60 && c.bounds.size.z < 60) return true;
            }
            return false;
        }
        static bool Ignored(Collider c) { return c.GetComponentInParent<Crulanda.Gameplay.Actor>() != null || c.GetComponentInParent<Crulanda.World.TreeFade>() != null; }
        static float Flat(Vector3 p, Vector2 at) { return Vector2.Distance(new Vector2(p.x, p.z), at); }
        /// <summary>True when the line from a to b dips under a water surface anywhere along it.</summary>
        static bool UnderWater(Crulanda.World.ZoneBuilder zone, Vector3 a, Vector3 b)
        {
            for (int i = 0; i <= 8; i++) { var p = Vector3.Lerp(a, b, i / 8f); if (zone.WaterAt(new Vector2(p.x, p.z), out float s, out _) && p.y < s + .1f) return true; }
            return false;
        }
        void CaptureWorld(string path)
        {
            // Hidden Windows players may have no back buffer. Explicit rendering verifies the world
            // without exposing an automated window; IMGUI overlays are not included in this capture.
            var camera = session.View;
            var target = new RenderTexture(1440, 900, 24);
            var previous = RenderTexture.active;
            var oldTarget = camera.targetTexture;
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = previous;
                target.Release(); Destroy(target); Destroy(image);
            }
        }
    }
}
