#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Hidden finds in play (Oakhaven). Each test puts its own secrets into the running zone, in quiet places away from everything
    /// else (the zone's own secrets included), so it doesn't depend on the zone's data and passes alongside it:
    /// - a lookout is found by walking onto it: the toast, a chat line and the rewards once, and nothing on a second visit;
    /// - searching a cache with E pays XP (levelling up as a quest reward does), gold, its item into the bags and its Chronicle page;
    /// - a chest is locked until its key is found, then opens; a key you take vanishes;
    /// - finds survive a save and load (and a load of an older save gives back what it hadn't found) and a whole reload of the zone;
    /// - the Discoveries tab counts found / total per zone, this one first, naming only the found; no map mark is a secret.
    /// </summary>
    public class DiscoveryTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-discovery-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        // ---------- helpers ----------
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static Vector2 Flat(Vector3 v) { return new Vector2(v.x, v.z); }
        static ZoneSecret Test(string slug, string kind, string name, int xp = 0, int gold = 0)
        {
            return new ZoneSecret { id = "secret.oakhaven.zz-test-" + slug, kind = kind, name = name, prompt = "Search the " + slug, text = "Left here for the test.",
                xp = xp, gold = gold, radius = 3, canonStatus = "GAME-ONLY (test)" };
        }
        /// <summary>Puts a test secret into the running zone: into its data (the book counts it) and as a built spot, with a small marker unless it is a lookout.</summary>
        static ZoneSecretSpot Hide(EncounterSession s, ZoneSecret def, Vector2 at)
        {
            var zone = s.Zone; def.at = at;
            zone.Zone.secrets = (zone.Zone.secrets ?? new ZoneSecret[0]).Concat(new[] { def }).ToArray();
            Transform marker = null;
            if (!DiscoveryLog.IsVista(def))
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Test secret " + def.id;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());   // nothing to trip on
                go.transform.position = zone.Ground(at, .2f); go.transform.localScale = Vector3.one * .4f; marker = go.transform;
            }
            var spot = new ZoneSecretSpot { def = def, position = zone.Ground(at), root = marker };
            zone.Secrets.Add(spot);
            return spot;
        }
        /// <summary>
        /// Open, dry, walkable ground well away from anything that would take E first or start a fight (roads out, camps and enemies,
        /// which notice you at 5-8 m; villagers and Mira, whom E talks to within 3.5 m; usable props, doors, quest places) and from the
        /// zone's own secrets: the test's finds are the only ones in play.
        /// </summary>
        static List<Vector2> QuietSpots(EncounterSession s, int count, float apart = 16)
        {
            var zone = s.Zone; var z = zone.Zone; float half = z.size / 2 - 22;
            var busy = new List<(Vector2 at, float clear)>();
            foreach (var e in z.exits) busy.Add((e.at, e.radius + 10));
            foreach (var c in z.camps ?? new ZoneCamp[0]) if (c != null) busy.Add((c.center, c.radius + 16));
            foreach (var e in s.Enemies) if (e != null) busy.Add((Flat(e.transform.position), 16));
            if (VillageLife.Active != null) foreach (var v in VillageLife.Active.Villagers) busy.Add((Flat(v.transform.position), 20));
            if (s.Companion != null) busy.Add((Flat(s.Companion.transform.position), 10));
            foreach (var i in zone.Interactables) busy.Add((Flat(i.position), 6));
            foreach (var d in zone.Doors) busy.Add((Flat(d.position), 6));
            foreach (var spot in s.SecretSpots) busy.Add((Flat(spot.position), Mathf.Max(spot.def.radius, 3) + 10));
            if (s.Quests != null) foreach (var (q, o, done) in s.Quests.Places()) busy.Add((o.at, o.radius + 3));
            var found = new List<Vector2>();
            for (float x = -half; x <= half && found.Count < count; x += 6)
                for (float y = -half; y <= half && found.Count < count; y += 6)
                {
                    var p = new Vector2(x, y);
                    if (busy.Exists(b => Vector2.Distance(b.at, p) < b.clear) || found.Exists(f => Vector2.Distance(f, p) < apart) || !Open(zone, p)) continue;
                    found.Add(p);
                }
            Assert.AreEqual(count, found.Count, "Quiet places in " + z.displayName + " for the test's secrets.");
            return found;
        }
        /// <summary>Dry ground on the navmesh.</summary>
        static bool Open(ZoneBuilder zone, Vector2 p)
        {
            if (zone.WaterAt(p, out _, out _)) return false;
            return NavMesh.SamplePosition(zone.Ground(p), out var hit, 1, NavMesh.AllAreas) && Vector2.Distance(Flat(hit.position), p) < .5f;
        }
        /// <summary>A start about 9 m from a lookout with a straight, dry, unobstructed walk to it that crosses no other lookout.</summary>
        static bool Approach(EncounterSession s, Vector2 to, out Vector2 from)
        {
            var zone = s.Zone;
            for (int k = 0; k < 8; k++)
            {
                from = to + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * 9;
                if (!Open(zone, from)) continue;
                bool clear = true;
                for (float t = 0; t <= 1.001f && clear; t += .1f)
                {
                    var at = Vector2.Lerp(from, to, t);
                    if (zone.WaterAt(at, out _, out _)) clear = false;
                    foreach (var spot in s.SecretSpots) if (DiscoveryLog.IsVista(spot.def) && Vector2.Distance(at, Flat(spot.position)) < spot.def.radius + 2) clear = false;
                }
                if (!clear) continue;
                NavMesh.SamplePosition(zone.Ground(from), out var a, 1, NavMesh.AllAreas); NavMesh.SamplePosition(zone.Ground(to), out var b, 1, NavMesh.AllAreas);
                if (!NavMesh.Raycast(a.position, b.position, out _, NavMesh.AllAreas)) return true;
            }
            from = default; return false;
        }
        /// <summary>Walks the player with the character controller (motor off) to a point, as far as the ground allows.</summary>
        static IEnumerator Walk(EncounterSession s, Vector2 to)
        {
            var body = s.Player.GetComponent<CharacterController>();
            for (int step = 0; step < 300; step++)
            {
                var p = body.transform.position; var d = new Vector3(to.x - p.x, 0, to.y - p.z);
                if (d.magnitude < .15f) break;
                body.Move(Vector3.ClampMagnitude(d, .2f) + Vector3.down * .15f);   // 4 m/s at a 0.05 s step, pressed to the ground
                if (step % 4 == 0) yield return null;
            }
        }
        /// <summary>Stands the player just beside a place (within E's reach).</summary>
        static IEnumerator StandBy(EncounterSession s, Vector2 at)
        {
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(at + new Vector2(1.2f, 0), 1.1f));
            yield return null;
        }
        /// <summary>Lets any toast from the zone's own secrets (a lookout at the spawn point, say) run out before a test looks at its own.</summary>
        static IEnumerator ToastsClear(EncounterSession s)
        {
            float until = Time.time + 2 * EncounterSession.ToastSeconds + 1;
            while (s.ToastName != null && Time.time < until) yield return null;
            Assert.IsNull(s.ToastName, "The toast queue runs out.");
        }

        // ---------- tests ----------
        [UnityTest] public IEnumerator A_lookout_is_found_by_walking_onto_it_and_pays_once()
        {
            var s = Session(); var zone = s.Zone;
            Vector2 at = default, from = default; bool placed = false;
            foreach (var p in QuietSpots(s, 8)) if (Approach(s, p, out from)) { at = p; placed = true; break; }
            Assert.IsTrue(placed, "A quiet place in Oakhaven with a clear walk up to it.");
            var vista = Test("lookout", "vista", "Test Lookout", xp: 40, gold: 12);
            Hide(s, vista, at);
            var motor = s.Player.GetComponent<AdventurerMotor>(); motor.Teleport(zone.Ground(from, 1.1f)); motor.enabled = false;
            yield return ToastsClear(s);
            yield return new WaitForSeconds(.7f);
            Assert.IsFalse(s.Discoveries.IsFound(vista), "Not found from 9 m off.");
            int xp = s.Progress.experience, gold = s.Progress.gold;

            yield return Walk(s, at);
            yield return new WaitForSeconds(1.2f);   // lookouts are checked every 0.5 s
            Assert.Less(Vector2.Distance(Flat(s.Player.transform.position), at), vista.radius, "The walk reached the lookout.");
            Assert.IsTrue(s.Discoveries.IsFound(vista), "Found by standing on it.");
            Assert.Contains(vista.id, s.Progress.discoveries);
            Assert.AreEqual(xp + 40, s.Progress.experience); Assert.AreEqual(gold + 12, s.Progress.gold);
            Assert.AreEqual("Test Lookout", s.ToastName, "The Discovered toast names it.");
            Assert.IsTrue(s.Messages.Exists(m => m.Contains("Test Lookout") && m.Contains("Left here for the test.") && m.Contains("+40 XP") && m.Contains("+12 gold")),
                "A chat line with its text and rewards: " + string.Join(" | ", s.Messages));
            yield return new WaitForSeconds(EncounterSession.ToastSeconds);
            Assert.IsNull(s.ToastName, "The toast fades after about 3 s.");

            // Away and back again: nothing more.
            yield return Walk(s, from);
            yield return new WaitForSeconds(.7f);
            yield return Walk(s, at);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(xp + 40, s.Progress.experience, "No experience the second time."); Assert.AreEqual(gold + 12, s.Progress.gold);
            Assert.AreEqual(1, s.Progress.discoveries.Count(id => id == vista.id));
            Assert.IsNull(s.ToastName, "No toast the second time.");
            motor.enabled = true;
        }

        [UnityTest] public IEnumerator Searching_a_cache_pays_xp_gold_its_item_and_its_page()
        {
            var s = Session(); var at = QuietSpots(s, 1)[0];
            string page = s.Quests.Db.Documents.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
            Assert.IsFalse(s.Progress.documents.Contains(page), "A fresh character hasn't read it.");
            var cache = Test("cache", "cache", "Test Cache", xp: 250, gold: 15);
            cache.prompt = "Search under the loose stones"; cache.item = "potion.minor"; cache.document = page;
            var spot = Hide(s, cache, at);
            yield return ToastsClear(s);
            s.Progress.experience = 0; s.Player.SetLevel(1);   // level 1 whatever the zone's own finds paid on the way in
            yield return StandBy(s, at);
            Assert.AreSame(spot, s.NearbySecret, "In reach, the cache is what E searches.");
            Assert.AreEqual("Search under the loose stones", s.InteractPrompt, "The E prompt is the cache's own.");
            int gold = s.Progress.gold, potions = Inventory.Count(s.Progress, "potion.minor");

            s.Interact();   // E
            Assert.IsTrue(s.Discoveries.IsFound(cache));
            Assert.AreEqual(250, s.Progress.experience); Assert.AreEqual(gold + 15, s.Progress.gold);
            Assert.AreEqual(potions + 1, Inventory.Count(s.Progress, "potion.minor"), "The item is in the bags.");
            Assert.Contains(page, s.Progress.documents, "The page is in the Chronicle.");
            Assert.IsTrue(s.Messages.Exists(m => m.StartsWith("New page in your Chronicle")), "And said so.");
            Assert.AreEqual(2, s.Progress.Level, "250 XP from level 1 is a level, taken the way quest rewards are.");
            Assert.AreEqual(2, s.Player.Level, "The character levels up at once.");
            Assert.IsTrue(s.Messages.Exists(m => m.StartsWith("Level 2!")));
            Assert.AreEqual("Test Cache", s.ToastName);
            Assert.IsNull(s.NearbySecret, "Found, it isn't offered again.");
            Assert.AreNotEqual("Search under the loose stones", s.InteractPrompt);
            s.Interact();
            Assert.AreEqual(250, s.Progress.experience, "E again gives nothing."); Assert.AreEqual(potions + 1, Inventory.Count(s.Progress, "potion.minor"));
            Assert.IsTrue(spot.root.GetComponent<Renderer>().enabled, "A cache stays where it was; only what you pocket vanishes.");
        }

        [UnityTest] public IEnumerator A_chest_stays_locked_until_its_key_is_found()
        {
            var s = Session(); var spots = QuietSpots(s, 2);
            var key = Test("key", "key", "Test Key", xp: 10);
            var chest = Test("chest", "chest", "Test Chest", xp: 30, gold: 25); chest.needs = key.id; chest.prompt = "Open the iron-bound chest";
            var keySpot = Hide(s, key, spots[0]); var chestSpot = Hide(s, chest, spots[1]);
            yield return ToastsClear(s);
            // The chest first: locked. Nothing given, nothing recorded, no toast.
            yield return StandBy(s, spots[1]);
            Assert.AreEqual("Open the iron-bound chest", s.InteractPrompt, "A locked chest still shows its prompt.");
            int xp = s.Progress.experience, gold = s.Progress.gold;
            s.Interact();
            Assert.AreEqual("Locked. The key must be somewhere near.", s.Messages.Last());
            Assert.IsFalse(s.Discoveries.IsFound(chest)); Assert.AreEqual(xp, s.Progress.experience); Assert.AreEqual(gold, s.Progress.gold);
            Assert.IsNull(s.ToastName, "A locked chest is no discovery.");
            Assert.AreSame(chestSpot, s.NearbySecret, "It waits to be opened.");
            // The key: a find of its own, and it is taken.
            yield return StandBy(s, spots[0]);
            Assert.AreSame(keySpot, s.NearbySecret);
            s.Interact();
            Assert.IsTrue(s.Discoveries.IsFound(key)); Assert.AreEqual(xp + 10, s.Progress.experience); Assert.AreEqual("Test Key", s.ToastName);
            Assert.IsFalse(keySpot.root.GetComponent<Renderer>().enabled, "Taken: the key is gone from its hiding place.");
            // Now the chest opens.
            yield return StandBy(s, spots[1]);
            Assert.AreEqual(DiscoveryLog.Result.Found, s.Search(s.NearbySecret));
            Assert.IsTrue(s.Discoveries.IsFound(chest));
            Assert.AreEqual(xp + 40, s.Progress.experience); Assert.AreEqual(gold + 25, s.Progress.gold);
            Assert.IsTrue(chestSpot.root.GetComponent<Renderer>().enabled, "The chest stays, open.");
        }

        [UnityTest] public IEnumerator Finds_survive_save_and_load_and_a_reload_and_never_pay_twice()
        {
            var s = Session(); var spots = QuietSpots(s, 2);
            var cache = Test("stash", "cache", "Test Stash", xp: 35, gold: 9);
            var note = Test("note", "note", "Test Note", xp: 5);
            var cacheSpot = Hide(s, cache, spots[0]); var noteSpot = Hide(s, note, spots[1]);
            string file = Path.Combine(root, EncounterSave.SlotFor(s.ClassDef.id) + ".save.json"), older = file + ".test-older";
            // The stash: found and saved (a find saves at once). Keep that save aside.
            yield return StandBy(s, spots[0]);
            s.Interact();
            Assert.IsTrue(s.Discoveries.IsFound(cache));
            int xp = s.Progress.experience, gold = s.Progress.gold;
            s.Save(false);
            Assert.IsTrue(File.Exists(file), "Saved to the test's folder."); StringAssert.Contains(cache.id, File.ReadAllText(file));
            File.Copy(file, older, true);
            // Then the note, which you take with you: it vanishes.
            yield return StandBy(s, spots[1]);
            s.Interact();
            Assert.IsTrue(s.Discoveries.IsFound(note)); Assert.IsFalse(noteSpot.root.GetComponent<Renderer>().enabled);
            Assert.AreEqual(xp + 5, s.Progress.experience);

            // Load (F9): both finds come back from the save, and neither is offered or pays again.
            s.Load();
            yield return new WaitForSeconds(.7f);
            Assert.Contains(cache.id, s.Progress.discoveries); Assert.Contains(note.id, s.Progress.discoveries);
            Assert.AreEqual(xp + 5, s.Progress.experience); Assert.AreEqual(gold, s.Progress.gold);
            Assert.IsFalse(noteSpot.root.GetComponent<Renderer>().enabled, "The note stays taken.");
            yield return StandBy(s, spots[0]);
            Assert.IsNull(s.NearbySecret, "The stash isn't offered again.");
            Assert.AreEqual(DiscoveryLog.Result.AlreadyFound, s.Search(cacheSpot));
            Assert.AreEqual(xp + 5, s.Progress.experience); Assert.AreEqual(gold, s.Progress.gold);

            // Load the older save: it has the stash but not the note, so the note is back where it was, to be found again.
            File.Copy(older, file, true);
            s.Load();
            yield return new WaitForSeconds(.7f);
            Assert.Contains(cache.id, s.Progress.discoveries); Assert.IsFalse(s.Progress.discoveries.Contains(note.id));
            Assert.AreEqual(xp, s.Progress.experience);
            Assert.IsTrue(noteSpot.root.GetComponent<Renderer>().enabled, "Unfound in that save, the note shows again.");
            yield return StandBy(s, spots[1]);
            Assert.AreSame(noteSpot, s.NearbySecret, "And can be found again.");

            // A whole reload of the zone: a new session reads the save from disk.
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            var t = Session(); Assert.AreNotSame(s, t);
            Assert.Contains(cache.id, t.Progress.discoveries); Assert.AreEqual(xp, t.Progress.experience); Assert.AreEqual(gold, t.Progress.gold);
            Hide(t, cache, spots[0]);   // the new zone has none of the test's secrets until they are put back
            yield return StandBy(t, spots[0]);
            Assert.IsNull(t.NearbySecret, "Still found after a reload.");
            t.Interact();
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(xp, t.Progress.experience, "Never pays twice."); Assert.AreEqual(gold, t.Progress.gold);
            Assert.IsTrue(t.DiscoveryTallies()[0].found.Exists(f => f.id == cache.id), "The book shows it found.");
        }

        [UnityTest] public IEnumerator The_book_counts_found_of_total_per_zone_and_no_map_marks_a_secret()
        {
            var s = Session(); var zone = s.Zone; var spots = QuietSpots(s, 3);
            int own = s.SecretSpots.Count;   // the zone's own secrets (none until its data has some)
            var defs = new[] { Test("tor", "vista", "Test Tor", xp: 5), Test("cairn", "cache", "Test Cairn", xp: 5), Test("herb", "herb", "Test Herb", xp: 5) };
            for (int i = 0; i < defs.Length; i++) Hide(s, defs[i], spots[i]);
            yield return StandBy(s, spots[1]);
            s.Interact();
            Assert.IsTrue(s.Discoveries.IsFound(defs[1]));

            var tallies = s.DiscoveryTallies(); var zones = zone.AllZones();
            Assert.AreEqual(zones.Count, tallies.Count, "Every zone is listed.");
            Assert.AreEqual(tallies.Count, tallies.Select(x => x.zoneId).Distinct().Count(), "Each once.");
            var here = tallies[0];
            Assert.AreEqual(zone.Zone.id, here.zoneId, "This zone first."); Assert.IsTrue(here.here);
            Assert.AreEqual(own + 3, here.total, "Its own secrets and the three put here.");
            Assert.AreEqual(1, here.found.Count); Assert.AreSame(defs[1], here.found[0], "Only the found one is named.");
            Assert.AreEqual(own + 2, here.Hidden, "The rest are a count.");
            foreach (var t in tallies.Skip(1))
            {
                var def = zones.Find(z => z.id == t.zoneId); Assert.NotNull(def, t.zoneId);
                Assert.IsFalse(t.here);
                Assert.AreEqual((def.secrets ?? new ZoneSecret[0]).Where(x => x != null && !string.IsNullOrEmpty(x.id)).Select(x => x.id).Distinct().Count(), t.total, t.zoneName + ": its total is its secrets.");
                Assert.IsEmpty(t.found, t.zoneName + ": nothing found there yet.");
            }

            // The maps: no mark stands on a secret or carries a secret's name (found or not, the zone's own or the test's).
            var marks = new List<HudMaps.MapMark>(); HudMaps.Marks(s, marks);
            Assert.IsTrue(marks.Exists(m => m.kind == HudMaps.MarkKind.Exit), "The maps mark the roads out.");
            Assert.IsTrue(marks.Exists(m => m.kind == HudMaps.MarkKind.Landmark), "And the landmarks.");
            foreach (var spot in s.SecretSpots)
                foreach (var m in marks)
                {
                    if (!string.IsNullOrEmpty(spot.def.name)) Assert.AreNotEqual(spot.def.name, m.text, m.kind + " mark named like the secret " + spot.def.id + ".");
                    if (m.kind == HudMaps.MarkKind.Enemy || m.kind == HudMaps.MarkKind.QuestPerson) continue;   // people and mobs move about
                    Assert.Greater(Vector2.Distance(Flat(m.world), Flat(spot.position)), .5f, m.kind + " mark '" + m.text + "' stands on the secret " + spot.def.id + ".");
                }
            foreach (var l in zone.Zone.landmarks) Assert.IsFalse(s.SecretSpots.Exists(sp => sp.def.name == l.name), "The landmark '" + l.name + "' names a secret.");
        }
    }
}
#endif
