using System;
using System.Collections;
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
            yield return new WaitForSeconds(1);
            Debug.Log("UI_CAPTURE_DONE"); Application.Quit(0);
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
            // Generic tour: the entrance, then each landmark framed from its south-west, then the first exit.
            var shots = new System.Collections.Generic.List<(string name, Vector2 at, float yaw, float pitch, float zoom)>();
            if (zone != null)
            {
                var z = zone.Zone; shots.Add(("01-entrance", z.spawns.player, z.spawns.playerFacing, 16, 12));
                int n = 2;
                foreach (var l in z.landmarks)
                {
                    var from = l.at + new Vector2(-.6f, -.8f) * Mathf.Min(l.radius + 4, 14);
                    float lim = z.size / 2 - 10; from = new Vector2(Mathf.Clamp(from.x, -lim, lim), Mathf.Clamp(from.y, -lim, lim));
                    var dir = l.at - from; float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                    shots.Add(((n++).ToString("00") + "-" + l.name.ToLowerInvariant().Replace(' ', '-').Replace("'", ""), from, yaw, 17, 11));
                }
                if (z.exits.Length > 0) shots.Add(((n++).ToString("00") + "-exit", z.exits[0].at + new Vector2(4, 4), 225, 15, 10));
            }
            EncounterHud.Hidden = true;
            foreach (var s in shots)
            {
                motor.Teleport(zone != null ? zone.Ground(s.at, 1.1f) : new Vector3(s.at.x, 1.1f, s.at.y));
                motor.SetView(s.yaw, s.pitch, s.zoom);
                yield return new WaitForSeconds(.6f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, (zone != null ? zone.Zone.id.Replace("zone.", "") : "world") + "-" + s.name + ".png"));
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
                // Groups of four in a row facing the camera, so nameplates don't overlap.
                for (int g = 0; g * 4 < line.Count; g++)
                {
                    for (int i = g * 4; i < Mathf.Min(line.Count, g * 4 + 4); i++)
                        line[i].StandAt(zone.Ground(new Vector2((i - g * 4 - 1.5f) * 2.6f, -8)), 180);
                    for (int i = 0; i < line.Count; i++) if (i < g * 4 || i >= g * 4 + 4) line[i].StandAt(zone.Ground(new Vector2(-40 + i, -40)), 0);
                    motor.Teleport(zone.Ground(new Vector2(0, -15), 1.1f)); motor.SetView(0, 6, 3.2f);
                    yield return new WaitForSeconds(1.2f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, prefix + "98-trades-lineup-" + (g + 1) + ".png"));
                    yield return new WaitForSeconds(.4f);
                }
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
            }
            EncounterHud.Hidden = false;
            Debug.Log("WORLD_CAPTURE_DONE"); Application.Quit(0);
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
