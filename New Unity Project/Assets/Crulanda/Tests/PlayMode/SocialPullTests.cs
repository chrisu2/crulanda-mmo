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
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// What the social and elite tests share: Oakhaven loaded with its own save folder, and a stage. The stage is the middle of
    /// an open wolf camp; the mobs a test needs are stood there at measured distances (EncounterEnemy.Rehome) and every other
    /// enemy is stood down, so a test says exactly who is near whom whatever the zone's layout becomes.
    /// </summary>
    public abstract class PullStage
    {
        protected string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindAnyObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; GameAnimal.SneakingOverride = null; ZoneBuilder.RequestedZoneId = null;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-pull-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(root, Session().SaveDirectoryOverride, "This test saves to its own folder, never the real one.");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; GameAnimal.SneakingOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        protected static EncounterSession Session() { return UnityEngine.Object.FindAnyObjectByType<EncounterSession>(); }
        protected static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }
        protected static Vector2 Xz(Vector3 p) { return new Vector2(p.x, p.z); }
        protected static void AtLevel(EncounterSession s, int level) { s.Progress.experience = EncounterProgress.XpForLevel(level); s.Player.SetLevel(level); }
        /// <summary>The player cannot die in the few seconds a test runs: these tests are about who comes and what lands, not who wins.</summary>
        protected static void Sturdy(EncounterSession s) { s.Player.Stats.SetBase(StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000); }
        /// <summary>The middle of the first open wolf camp (no ambush, no elite, three wolves or more): walkable wood, far from the village.</summary>
        protected static Vector2 Stage(EncounterSession s)
        {
            foreach (var camp in s.Zone.Zone.camps)
                if (camp != null && !camp.elite && !camp.ambush && camp.look == "wolf" && camp.count >= 3 && !Hollow.InsideAny(s.Zone.Ground(camp.center, 1), 0)) return camp.center;
            Assert.Fail("Oakhaven has an open wolf camp to stage the tests in."); return default;
        }
        /// <summary>The living, standing, ordinary mobs of the first camp that fits and holds at least <paramref name="need"/> of them.</summary>
        protected static List<EncounterEnemy> CampMobs(EncounterSession s, Func<ZoneCamp, bool> fits, int need, int skipCamp = -1)
        {
            var camps = s.Zone.Zone.camps;
            for (int c = 0; c < camps.Length; c++)
            {
                if (c == skipCamp || camps[c] == null || !fits(camps[c])) continue;
                var mobs = s.Enemies.FindAll(e => e != null && e.CampIndex == c && e.actor.IsAlive && !e.Hidden && !e.Elite);
                if (mobs.Count >= need) return mobs;
            }
            Assert.Fail("Oakhaven has a camp of the kind this test needs, with " + need + " mobs."); return null;
        }
        protected static EncounterEnemy Named(EncounterSession s, string mob)
        {
            var e = s.Enemies.Find(x => x != null && x.Elite && x.MobName == mob && x.actor.IsAlive);
            Assert.NotNull(e, mob + " is in Oakhaven."); Assert.NotNull(e.Move, mob + " has its move."); return e;
        }
        /// <summary>Everyone but the cast stands down (their scripts off): nothing else notices, answers or joins.</summary>
        protected static void StandDown(EncounterSession s, params EncounterEnemy[] cast) { foreach (var e in s.Enemies) if (e != null && Array.IndexOf(cast, e) < 0) e.enabled = false; }
        /// <summary>Stands a mob in the middle of the stage and makes that its home.</summary>
        protected static void Place(EncounterSession s, EncounterEnemy e, Vector2 stage)
        {
            var want = s.Zone.Ground(stage, 0); e.Rehome(want);
            Assert.Less(Flat(e.transform.position, want), 3.1f, e.Name + " could be stood on the stage.");
            Assert.Less(Flat(e.Home, e.transform.position), .1f, "Where it stands is its home now.");
        }
        /// <summary>
        /// Stands a mob <paramref name="distance"/> metres from a point and makes that its home, trying the bearings in order
        /// (0 north, 90 east; all eight when none are given) until the ground lets it stand within half a metre of that distance.
        /// </summary>
        protected static void PlaceNear(EncounterSession s, EncounterEnemy e, Vector3 from, float distance, params float[] bearings)
        {
            foreach (float bearing in bearings.Concat(new[] { 0f, 90, 180, 270, 45, 135, 225, 315 }))
            {
                var dir = Quaternion.Euler(0, bearing, 0) * Vector3.forward;
                e.Rehome(s.Zone.Ground(Xz(from) + new Vector2(dir.x, dir.z) * distance, 0));
                if (Mathf.Abs(Flat(e.transform.position, from) - distance) < .5f) return;
            }
            Assert.Fail(e.Name + " could not be stood " + distance + " m from the middle of the stage on any bearing.");
        }
        /// <summary>The player, <paramref name="distance"/> metres from a point on a bearing (0 north, 90 east).</summary>
        protected static void PutPlayer(EncounterSession s, Vector3 from, float distance, float bearing)
        {
            var dir = Quaternion.Euler(0, bearing, 0) * Vector3.forward;
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(Xz(from) + new Vector2(dir.x, dir.z) * distance, 1.1f)); Physics.SyncTransforms();
        }
        protected static IEnumerator Until(Func<bool> done, float seconds) { for (float t = 0; t < seconds && !done(); t += Time.deltaTime) yield return null; }
        protected static IEnumerator Wait(float seconds) { for (float t = 0; t < seconds; t += Time.deltaTime) yield return null; }
        protected static bool Said(EncounterSession s, string part) { return s.Messages.Exists(m => m.Contains(part)); }
        protected static string Chat(EncounterSession s) { return " The chat holds: " + string.Join(" / ", s.Messages); }
        /// <summary>Nothing but actors between two points (as EncounterEnemy.Sees looks).</summary>
        protected static bool Open(Vector3 from, Vector3 to)
        {
            foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null) return false;
            return true;
        }
    }

    /// <summary>
    /// Social aggro in the running game (playtest note 3, the owner's words: "some mobs need to be more social. i can pull them
    /// easy 1 at a time even when they stand next to each other"):
    /// - a wolf pack comes together, at once and without a word, and a wolf out of reach stays;
    /// - a boar stays single with another boar beside it;
    /// - a deserter's shout is read in the chat and over his head, and brings his camp and kin from the next camp after a beat;
    /// - sneaking peels one wolf from another that would have come;
    /// - an elite's guards come from further than a call carries, and a guard's shout brings the elite only from beside it;
    /// - Mira's heals do not turn the mobs that joined a pull on her (their threat is shared out among the mobs in the fight);
    /// - a linked group whose leash breaks heals and goes home together, and stays home.
    /// The pulls are made with a blow from 7 m (outside every mob's 5 m notice) unless the test is about being noticed.
    /// </summary>
    public class SocialPullTests : PullStage
    {
        [UnityTest] public IEnumerator A_wolf_pack_comes_together_and_a_wolf_out_of_reach_stays()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var wolves = CampMobs(s, c => c.look == "wolf" && !c.ambush, 3);
            EncounterEnemy a = wolves[0], b = wolves[1], far = wolves[2], c = wolves.Count > 3 ? wolves[3] : null;
            Assert.AreEqual(SocialKind.Pack, a.Social, "Wolves pack.");
            StandDown(s, c != null ? new[] { a, b, far, c } : new[] { a, b, far });
            Place(s, a, stage); var mid = a.transform.position;
            PlaceNear(s, b, mid, 3, 90); PlaceNear(s, far, mid, 14, 90, 0, 180); if (c != null) PlaceNear(s, c, mid, 3, 0, 180);
            Assert.Less(Vector3.Distance(mid, b.transform.position), SocialAggro.PackReach - 2, "The second wolf stands next to the first.");
            Assert.Greater(Vector3.Distance(mid, far.transform.position), SocialAggro.PackReach + 2, "The far wolf is out of a pack's reach.");
            PutPlayer(s, mid, 7, 270);   // out of every wolf's notice (5 m): the pull is the blow
            yield return null;
            Assert.IsFalse(a.Engaged || b.Engaged || far.Engaged, "Nobody has noticed you at 7 m.");
            a.Receive(1, s.Player);
            yield return Until(() => a.Engaged && b.Engaged && (c == null || c.Engaged), 1);
            Assert.IsTrue(a.Engaged, "The wolf you hit comes for you.");
            Assert.IsTrue(b.Engaged, "The wolf beside it comes too: you cannot pull them one at a time.");
            if (c != null) Assert.IsTrue(c.Engaged, "And the third.");
            Assert.AreSame(s.Player, b.Victim, "It comes for whoever pulled.");
            Assert.NotNull(a.Group); Assert.AreSame(a.Group, b.Group, "They fight as one group.");
            Assert.IsFalse(Said(s, "shouts"), "A pack says nothing.");
            yield return Wait(1.5f);
            Assert.IsFalse(far.Engaged, "The wolf 14 m off was out of reach and stays where it is."); Assert.IsNull(far.Group); Assert.IsFalse(far.Answering);
            // Mira heals twice. Her threat is shared out among the wolves in the fight, so the one that joined (and has not been
            // touched) stays on whoever pulled. Given to each in full, her second heal turned it.
            // Recruited, she follows you: she stands beside you, not back in the Golden Cask (a wolf turned on her there is past its
            // leash at once, and the whole pack goes home).
            s.Progress.recruited = true; var mira = s.Companion.actor;
            s.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(s.Player.transform.position + s.Player.transform.right * 2); yield return null;
            Assert.AreSame(mira, s.PartyActor(mira.EntityId.Value), "Mira is in the party: her threat counts.");
            int heal = HealerCompanion.HealFor(s.content.healingAbility.power, s.Player.Level);
            Assert.Less(heal, s.JoinThreat, "Two heals' threat, unshared, is less than what a joiner holds on the puller.");
            s.HealThreat(mira, heal); s.HealThreat(mira, heal);
            yield return null; yield return null;
            Assert.IsTrue(b.Engaged, "The wolf that joined is still in the fight (alive " + b.actor.IsAlive + ", health " + b.actor.Health.Pool.Ratio.ToString("0.00") + ", group " + (b.Group != null) + ", " + Vector3.Distance(b.transform.position, s.Player.transform.position).ToString("0.0") + " m from you, the first wolf engaged " + a.Engaged + ", you alive " + s.Player.IsAlive + ")."); Assert.AreSame(s.Player, b.Victim, "Two of Mira's heals later the wolf that joined is still on whoever pulled, not on her.");
        }

        [UnityTest] public IEnumerator A_boar_stays_single_with_another_beside_it()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var boars = CampMobs(s, c => c.look == "boar" && !c.ambush, 2);
            EncounterEnemy a = boars[0], b = boars[1];
            Assert.AreEqual(SocialKind.Solitary, a.Social, "Boar are solitary.");
            StandDown(s, a, b);
            Place(s, a, stage); var mid = a.transform.position; PlaceNear(s, b, mid, 3, 90);
            PutPlayer(s, mid, 7, 270);
            yield return null;
            a.Receive(1, s.Player);
            yield return Until(() => a.Engaged, 1);
            Assert.IsTrue(a.Engaged, "The boar you hit comes for you.");
            yield return Wait(2);
            Assert.IsFalse(b.Engaged, "The boar beside it goes on rooting."); Assert.IsFalse(b.Answering); Assert.IsNull(a.Group, "A solitary beast fights alone.");
        }

        [UnityTest] public IEnumerator A_deserters_shout_is_read_in_the_chat_and_brings_the_camp_after_a_beat()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var camp = CampMobs(s, c => c.tag == "deserter", 2);
            var next = CampMobs(s, c => c.tag == "deserter", 2, camp[0].CampIndex);   // another camp of the same people
            EncounterEnemy a = camp[0], b = camp[1], kin = next[0], far = next[1];
            Assert.AreEqual(SocialKind.Call, a.Social, "Deserters call out."); Assert.AreEqual(a.Kin, kin.Kin); Assert.AreNotEqual(a.CampIndex, kin.CampIndex);
            StandDown(s, a, b, kin, far);
            Place(s, a, stage); var mid = a.transform.position;
            PlaceNear(s, b, mid, 4, 90); PlaceNear(s, kin, mid, 8, 0, 180); PlaceNear(s, far, mid, 17, 90, 0, 180);
            Assert.Less(Vector3.Distance(mid, kin.transform.position), SocialAggro.CallReach - 1, "The next camp's man is in earshot.");
            Assert.Greater(Vector3.Distance(mid, far.transform.position), SocialAggro.CallReach + 2, "The far one is out of earshot.");
            PutPlayer(s, mid, 7, 270);
            yield return null;
            s.Messages.Clear();
            a.Receive(1, s.Player);
            yield return Until(() => b.Answering, 1);
            // The shout: a line in the chat with his name, and a word over his head.
            Assert.IsTrue(a.Engaged, "The deserter you hit comes for you.");
            var line = s.Messages.Find(m => m.StartsWith(a.Name) && (m.Contains("shouts") || m.Contains("calls")));
            Assert.NotNull(line, "His shout is in the chat." + Chat(s));
            var words = Enumerable.Range(0, 6).Select(i => SocialAggro.Shout(a.Kin, i).over).Distinct().ToList();
            Assert.IsTrue(s.Floating.Exists(f => words.Contains(f.text)), "And over his head (one of: " + string.Join(", ", words) + ").");
            // The beat: the others have heard, and are not yet coming.
            Assert.IsTrue(b.Answering, "His campmate has heard."); Assert.IsFalse(b.Engaged, "But there is a beat before he comes."); Assert.IsTrue(kin.Answering, "The next camp's man heard it too.");
            yield return Until(() => b.Engaged && kin.Engaged, SocialAggro.CallBeat + 1.5f);
            Assert.IsTrue(b.Engaged, "After the beat his campmate comes."); Assert.IsTrue(kin.Engaged, "And the man from the next camp.");
            Assert.AreSame(s.Player, b.Victim); Assert.AreSame(a.Group, b.Group); Assert.AreSame(a.Group, kin.Group);
            yield return Wait(1);
            Assert.IsFalse(far.Engaged, "The deserter out of earshot never heard a thing."); Assert.IsFalse(far.Answering);
        }

        [UnityTest] public IEnumerator Sneaking_peels_one_wolf_from_another_that_would_have_come()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var wolves = CampMobs(s, c => c.look == "wolf" && !c.ambush, 2);
            EncounterEnemy a = wolves[0], b = wolves[1];
            StandDown(s, a, b);
            Place(s, a, stage); var mid = a.transform.position; PlaceNear(s, b, mid, 5, 90);
            float apart = Vector3.Distance(mid, b.transform.position);
            Assert.Greater(apart, SocialAggro.SneakReach + .5f, "Further apart than a sneaking pull carries."); Assert.Less(apart, SocialAggro.PackReach - 2.5f, "Near enough that a plain pull brings both.");
            // Stand 3.5 m from the first wolf on the side away from the second, where it has a clear line to you.
            var away = mid - b.transform.position; away.y = 0; away.Normalize();
            Vector3? spot = null;
            foreach (float turn in new[] { 0f, 30, -30, 60, -60 })
            {
                var dir = Quaternion.Euler(0, turn, 0) * away; var p = s.Zone.Ground(Xz(mid) + new Vector2(dir.x, dir.z) * 3.5f, 1.1f);
                if (!NavMesh.SamplePosition(p, out _, 1.6f, NavMesh.AllAreas)) continue;
                Vector3 eye = mid + Vector3.up * .6f, chest = p + Vector3.up * .4f;
                if (Open(eye, chest) && Open(chest, eye)) { spot = p; break; }
            }
            Assert.IsTrue(spot.HasValue, "A place 3.5 m from the first wolf with a clear line, away from the second.");
            Assert.Greater(Flat(spot.Value, b.transform.position), 6.5f, "The second wolf cannot notice you itself from there.");
            GameAnimal.SneakingOverride = true;   // the tests' Ctrl
            s.Player.GetComponent<AdventurerMotor>().Teleport(spot.Value); Physics.SyncTransforms();
            yield return Until(() => a.Engaged, 2);
            Assert.IsTrue(a.Engaged, "The wolf at the edge notices you at 3.5 m, sneaking or not.");
            yield return Wait(1.2f);
            Assert.IsFalse(b.Engaged, "Sneaking, you peeled it: the wolf 5 m behind it never stirred."); Assert.IsFalse(b.Answering); Assert.IsNull(b.Group);
            // The same pull standing up brings both.
            GameAnimal.SneakingOverride = false;
            a.ResetFight();
            yield return Until(() => a.Engaged && b.Engaged, 2.5f);
            Assert.IsTrue(a.Engaged, "It notices you again."); Assert.IsTrue(b.Engaged, "And this time, not sneaking, the second wolf comes with it.");
        }

        [UnityTest] public IEnumerator An_elites_guards_come_from_further_than_a_call_carries()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var king = Named(s, "Caddock, the Bandit King");
            var guards = s.Enemies.FindAll(e => e != king && e.actor.IsAlive && s.GuardOf(e, king));
            Assert.GreaterOrEqual(guards.Count, 2, "The king's guard stands with him.");
            var other = s.Enemies.Find(e => e.actor.IsAlive && !e.Elite && e.Kin == king.Kin && !s.GuardOf(e, king));
            Assert.NotNull(other, "A deserter who is not of the king's guard.");
            StandDown(s, king, guards[0], guards[1], other);
            Place(s, king, stage); var mid = king.transform.position;
            PlaceNear(s, guards[0], mid, 14, 90, 45); PlaceNear(s, guards[1], mid, 14, 0, 315); PlaceNear(s, other, mid, 14, 270, 180);
            foreach (var g in new[] { guards[0], guards[1], other })
            {
                float d = Vector3.Distance(mid, g.transform.position);
                Assert.Greater(d, SocialAggro.CallReach + .5f, g.Name + " is beyond a call."); Assert.Less(d, SocialAggro.GuardReach - .5f, g.Name + " is within a guard's reach.");
            }
            PutPlayer(s, mid, 7, 225);
            yield return null;
            Assert.IsFalse(king.Engaged, "Nobody has noticed you at 7 m.");
            king.Receive(1, s.Player);
            yield return Until(() => guards[0].Engaged && guards[1].Engaged, SocialAggro.CallBeat + 2);
            Assert.IsTrue(king.Engaged); Assert.IsTrue(guards[0].Engaged && guards[1].Engaged, "A boss always fights with its guards.");
            Assert.AreSame(king.Group, guards[0].Group);
            yield return Wait(1);
            Assert.IsFalse(other.Engaged, "The deserter who is no guard was out of earshot and stays."); Assert.IsFalse(other.Answering);
        }

        /// <summary>The king on the stage, a guard <paramref name="near"/> metres east of him, a second guard 3 m to that guard's side, and the player 7 m beyond the first guard, out of everyone's notice.</summary>
        static void KingAndGuards(EncounterSession s, float near, out EncounterEnemy king, out EncounterEnemy first, out EncounterEnemy second)
        {
            var stage = Stage(s); var lord = Named(s, "Caddock, the Bandit King");
            var guards = s.Enemies.FindAll(e => e != lord && e.actor.IsAlive && s.GuardOf(e, lord));
            Assert.GreaterOrEqual(guards.Count, 2, "The king's guard stands with him.");
            king = lord; first = guards[0]; second = guards[1];
            StandDown(s, king, first, second);
            Place(s, king, stage); var mid = king.transform.position;
            PlaceNear(s, first, mid, near, 90, 45, 135);
            var on = first.transform.position - mid; on.y = 0; float bearing = Mathf.Atan2(on.x, on.z) * Mathf.Rad2Deg;
            PlaceNear(s, second, first.transform.position, 3, bearing + 90, bearing - 90);
            PutPlayer(s, first.transform.position, 7, bearing);
            Assert.Greater(Flat(s.Player.transform.position, king.transform.position), 6.5f, "The king cannot notice you himself from there.");
            Assert.Greater(Flat(s.Player.transform.position, second.transform.position), 5.5f, "Nor can the second guard.");
            Assert.Less(Vector3.Distance(first.transform.position, second.transform.position), SocialAggro.CallReach - 2, "The second guard is in the first's earshot.");
        }

        [UnityTest] public IEnumerator A_guards_shout_brings_his_fellows_and_not_the_king_across_the_hall()
        {
            var s = Session(); Sturdy(s);
            KingAndGuards(s, 8, out var king, out var first, out var second);
            float apart = Vector3.Distance(king.transform.position, first.transform.position);
            Assert.Greater(apart, SocialAggro.LordReach + 1, "The guard does not stand beside the king."); Assert.Less(apart, SocialAggro.CallReach - 1, "But well within earshot of him.");
            yield return null;
            Assert.IsFalse(king.Engaged || first.Engaged || second.Engaged, "Nobody has noticed you.");
            s.Messages.Clear();
            first.Receive(1, s.Player);
            yield return Until(() => first.Engaged && second.Engaged, SocialAggro.CallBeat + 1.5f);
            Assert.IsTrue(first.Engaged, "The guard you hit comes for you."); Assert.IsTrue(second.Engaged, "And his fellow, at his shout.");
            Assert.IsTrue(s.Messages.Exists(m => m.StartsWith(first.Name)), "The shout is in the chat." + Chat(s));
            yield return Wait(1.5f);
            Assert.IsFalse(king.Engaged, "The king stays before his throne: his guard can be cleared first."); Assert.IsFalse(king.Answering); Assert.IsNull(king.Group);
            Assert.AreEqual(king.actor.Health.Pool.Max, king.actor.Health.Pool.Current);
        }

        [UnityTest] public IEnumerator A_guard_beside_the_king_brings_him_and_he_brings_the_rest()
        {
            var s = Session(); Sturdy(s);
            KingAndGuards(s, 2.5f, out var king, out var first, out var second);
            Assert.Less(Vector3.Distance(king.transform.position, first.transform.position), SocialAggro.LordReach - .3f, "The guard stands beside the king.");
            // A third of the guard far across the hall: out of the first guard's earshot, within the king's reach for his guards.
            var third = s.Enemies.Find(e => e != king && e != first && e != second && e.actor.IsAlive && s.GuardOf(e, king));
            if (third != null)
            {
                third.enabled = true; PlaceNear(s, third, king.transform.position, 12, 270, 225, 315);
                Assert.Greater(Vector3.Distance(first.transform.position, third.transform.position), SocialAggro.CallReach + .5f, "Out of the guard's earshot.");
                Assert.Less(Vector3.Distance(king.transform.position, third.transform.position), SocialAggro.GuardReach - .5f, "Within the king's reach for his guards.");
            }
            yield return null;
            Assert.IsFalse(king.Engaged || first.Engaged, "Nobody has noticed you.");
            first.Receive(1, s.Player);
            yield return Until(() => king.Engaged && second.Engaged, SocialAggro.CallBeat + 2);
            Assert.IsTrue(first.Engaged); Assert.IsTrue(second.Engaged, "His fellow comes at the shout.");
            Assert.IsTrue(king.Engaged, "The king, a stride from the guard you hit, comes too."); Assert.AreSame(s.Player, king.Victim);
            if (third != null)
            {
                yield return Until(() => third.Engaged, SocialAggro.CallBeat + 2);
                Assert.IsTrue(third.Engaged, "And the king, drawn in, brings the guard the first shout did not reach.");
            }
        }

        [UnityTest] public IEnumerator A_linked_group_resets_together_and_stays_home()
        {
            var s = Session(); Sturdy(s); var stage = Stage(s);
            var wolves = CampMobs(s, c => c.look == "wolf" && !c.ambush, 2);
            EncounterEnemy a = wolves[0], b = wolves[1];
            StandDown(s, a, b);
            Place(s, a, stage); var mid = a.transform.position; PlaceNear(s, b, mid, 3, 90);
            PutPlayer(s, mid, 7, 270);
            yield return null;
            a.Receive(1, s.Player);
            yield return Until(() => a.Engaged && b.Engaged, 1);
            Assert.IsTrue(a.Engaged && b.Engaged, "Both wolves are in the fight.");
            b.Receive(15, s.Player); a.Receive(15, s.Player);
            Assert.Less(b.actor.Health.Pool.Current, b.actor.Health.Pool.Max);
            yield return Wait(.5f);
            // Out past the leash (it is measured from where the pull began): the whole group gives up at once.
            s.Messages.Clear();
            PutPlayer(s, mid, s.Leash + 30, 270);
            yield return Until(() => !a.Engaged && !b.Engaged, 1);
            Assert.IsFalse(a.Engaged || b.Engaged, "Both give up together.");
            Assert.AreEqual(a.actor.Health.Pool.Max, a.actor.Health.Pool.Current, "Whole again."); Assert.AreEqual(b.actor.Health.Pool.Max, b.actor.Health.Pool.Current, "Both of them.");
            Assert.IsNull(a.Group); Assert.IsNull(b.Group); Assert.IsTrue(a.Evading && b.Evading, "On their way home they notice nobody.");
            Assert.IsTrue(Said(s, "break off"), "The chat says they went home." + Chat(s));
            // Home is as near as its agent stops (a beast halts 2.1 m short of where it is sent).
            float nearA = a.GetComponent<NavMeshAgent>().stoppingDistance + .6f, nearB = b.GetComponent<NavMeshAgent>().stoppingDistance + .6f;
            yield return Until(() => Flat(a.transform.position, a.Home) < nearA && Flat(b.transform.position, b.Home) < nearB, 8);
            Assert.Less(Flat(a.transform.position, a.Home), nearA, "The first is home."); Assert.Less(Flat(b.transform.position, b.Home), nearB, "The second is home.");
            yield return Wait(EncounterEnemy.EvadeSeconds + 1);
            Assert.IsFalse(a.Engaged || b.Engaged, "And they stay there: no coming and going.");
            Assert.IsFalse(s.InCombat, "The fight is over.");
        }
    }
}
#endif
