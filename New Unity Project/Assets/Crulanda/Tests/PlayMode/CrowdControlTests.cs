#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Crowd control, the engine (Docs/CC_DESIGN.md step C1): a hold stops a mob and breaks on damage; a stun does not break;
    /// the same hold again within 18 s is half as long and a third time does not take; a boss takes no hold; a held mob answers no call.</summary>
    public class CrowdControlTests
    {
        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-cc-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        EncounterEnemy Mob() { return session.Enemies.First(e => e != null && e.actor.IsAlive && e.Camp && !e.Elite && !e.Game && !e.Hidden); }

        [UnityTest] public IEnumerator A_hold_stops_a_mob_and_breaks_on_damage()
        {
            var e = Mob(); yield return null;
            Assert.IsNull(e.Apply("incap", session.Player, 20), "it takes");
            Assert.IsTrue(e.Incapacitated); Assert.IsTrue(e.Controlled); Assert.IsFalse(e.CanAnswer, "a held mob answers no call");
            yield return null; var rings = session.GetComponent<ControlRings>(); Assert.NotNull(rings); Assert.GreaterOrEqual(rings.Showing, 1, "a ring under it");
            StringAssert.StartsWith("Held", e.ControlLabel);
            yield return null; e.Receive(1, session.Player);
            Assert.IsFalse(e.Incapacitated, "damage breaks it");
            Assert.IsNull(e.Apply("stun", session.Player, 3)); e.Receive(1, session.Player);
            Assert.IsTrue(e.Stunned, "a stun does not break");
        }
        [UnityTest] public IEnumerator The_same_hold_again_is_shorter_then_does_not_take()
        {
            var e = Mob(); yield return null;
            Assert.IsNull(e.Apply("incap", session.Player, 20)); e.Receive(1, session.Player);
            Assert.IsNull(e.Apply("incap", session.Player, 20), "again: it takes");
            StringAssert.Contains("10 s", e.ControlLabel, "half as long");
            e.Receive(1, session.Player);
            Assert.AreEqual("Immune (too soon)", e.Apply("incap", session.Player, 20), "a third time: no");
        }
        [UnityTest] public IEnumerator Marks_are_one_a_mob_and_the_party_leaves_moon_and_held_alone()
        {
            var a = Mob(); var b = session.Enemies.First(e => e != null && e != a && e.actor.IsAlive && e.Camp && !e.Elite && !e.Game && !e.Hidden); yield return null;
            session.Select(a); session.MarkTarget(RaidMark.Skull); Assert.AreEqual(RaidMark.Skull, a.RaidMarked); Assert.IsFalse(a.LeaveAlone);
            session.Select(b); session.MarkTarget(RaidMark.Skull); Assert.AreEqual(RaidMark.None, a.RaidMarked, "one skull"); Assert.AreEqual(RaidMark.Skull, b.RaidMarked);
            session.Select(a); session.MarkTarget(RaidMark.Moon); Assert.IsTrue(a.LeaveAlone, "moon: leave it be");
            session.MarkTarget(RaidMark.None); Assert.IsFalse(a.LeaveAlone);
            Assert.IsNull(a.Apply("incap", session.Player, 10)); Assert.IsTrue(a.LeaveAlone, "held: leave it be");
        }
        [UnityTest] public IEnumerator The_Warrior_bashes_and_shouts()
        {
            Assert.AreEqual("class.warrior", session.ClassDef.id, "a fresh character is a Warrior");
            session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10); session.Player.Resource.Pool.SetCurrent(100);
            var e = Mob(); session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 2); session.Select(e);
            int bash = -1, shout = -1;
            for (int i = 0; i < session.Kit.ActionCount; i++) { var x = session.Kit.ActionAt(i); if (x.id == "ability.shield_bash") bash = i; if (x.id == "ability.shout") shout = i; }
            Assert.IsTrue(bash >= 0 && shout >= 0, "both on the bar");
            Assert.IsTrue(session.UseAbility(bash), "Shield Bash"); yield return null; Assert.IsTrue(e.Silenced);
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(shout), "Shout"); yield return null; Assert.IsTrue(e.Feared);
        }
        [UnityTest] public IEnumerator A_caster_casts_and_a_silence_interrupts_it()
        {
            var e = Mob(); e.Cast = MobCasts.For("Ash initiate"); Assert.NotNull(e.Cast);
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 6); session.Select(e);
            string me = session.Player.EntityId.Value; float t = 0;
            while (t < 8 && !e.Casting) { e.threat.Add(me, 1); t += Time.deltaTime; yield return null; }
            Assert.IsTrue(e.Casting, "it begins Cinder Bolt"); Assert.AreEqual(0, e.Interrupts);
            Assert.IsNull(e.Apply("silence", session.Player, 3)); Assert.IsFalse(e.Casting, "interrupted"); Assert.AreEqual(1, e.Interrupts);
        }
        [UnityTest] public IEnumerator A_mender_heals_a_hurt_ally()
        {
            var m = Mob(); m.Cast = MobCasts.For("Ash mender");
            var o = session.Enemies.Where(x => x != null && x != m && x.actor.IsAlive && x.Camp && !x.Elite && !x.Game && !x.Hidden).OrderBy(x => Vector3.Distance(x.transform.position, m.transform.position)).First();
            session.Player.GetComponent<AdventurerMotor>().Teleport(m.transform.position + Vector3.back * 6); session.Select(m);
            string me = session.Player.EntityId.Value; m.threat.Add(me, 50); o.threat.Add(me, 50); yield return null; yield return null;
            o.actor.Health.ApplyDamage(o.actor.Health.Pool.Max / 2); int hurt = o.actor.Health.Pool.Current; float t = 0;
            while (t < 9 && o.actor.Health.Pool.Current <= hurt) { m.threat.Add(me, 1); o.threat.Add(me, 1); session.Player.Health.ApplyHealing(999); t += Time.deltaTime; yield return null; }
            Assert.Greater(o.actor.Health.Pool.Current, hurt, "Ember Mend lands on the hurt one");
        }
        [UnityTest] public IEnumerator A_boss_takes_no_hold()
        {
            yield return null;
            var king = session.Enemies.FirstOrDefault(x => x != null && x.Elite && x.actor.DisplayName.Contains("Caddock"));
            Assert.NotNull(king, "Caddock is in Crowsfoot Hollow");
            Assert.AreEqual("Immune", king.Apply("incap", session.Player, 20)); Assert.AreEqual("Immune", king.Apply("fear", session.Player, 8));
            Assert.IsNull(king.Apply("stun", session.Player, 3), "a stun works on a boss");
        }
    }
}
#endif
