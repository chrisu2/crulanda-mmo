#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Archivist in play (Docs/CC_DESIGN.md section 0): Lull sleeps a group and damage wakes one; Echo Bind holds one at a
    /// time; Hush silences; a song at a time; Echo-jar sends them running.</summary>
    public class ArchivistLoopTests
    {
        string root; EncounterSession session;
        ArchivistKit Arch { get { return session.Kit as ArchivistKit; } }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-arch-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.archivist";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(Arch);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null; SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        void Level10() { session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10); }
        EncounterEnemy Foe(EncounterEnemy not = null) { var e = session.Enemies.Find(x => x != null && x != not && x.actor.IsAlive && !x.Elite && !x.Game && !x.Engaged); Assert.NotNull(e, "A living mob."); return e; }
        void Near(EncounterEnemy e, float d) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * d); session.Select(e); }
        const int Note = 0, Lull = 1, Hush = 2, Bind = 3, Cadence = 4, Dirge = 5, Jar = 6;
        IEnumerator Cast() { yield return new WaitForSeconds(1.7f); }

        [UnityTest] public IEnumerator The_Archivist_is_its_own_character()
        {
            Assert.AreEqual("class.archivist", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(7, session.Kit.ActionCount); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Lull));
            Assert.IsFalse(session.Kit.MeleeAutoAttacks); Assert.AreEqual(ActorLook.Mage, EncounterSession.LookForClass("class.archivist"));
            yield return null;
        }
        [UnityTest] public IEnumerator Lull_sleeps_and_damage_wakes()
        {
            Level10(); var e = Foe(); Near(e, 12);
            Assert.IsTrue(session.UseAbility(Lull), "Lull"); yield return Cast();
            Assert.IsTrue(e.Incapacitated, "asleep"); StringAssert.StartsWith("Held", e.ControlLabel);
            Assert.IsTrue(session.UseAbility(Note), "Shard Note"); yield return Cast();
            Assert.IsFalse(e.Incapacitated, "the note wakes it");
        }
        [UnityTest] public IEnumerator Echo_Bind_holds_one_at_a_time_and_Hush_silences()
        {
            Level10(); var a = Foe(); var b = Foe(a); Near(a, 12);
            Assert.IsTrue(session.UseAbility(Bind), "Bind a"); yield return Cast();
            Assert.IsTrue(a.Incapacitated); Assert.AreEqual(a, Arch.Bound);
            Near(b, 12);
            Assert.IsTrue(session.UseAbility(Bind), "Bind b"); yield return Cast();
            Assert.IsTrue(b.Incapacitated); Assert.IsFalse(a.Incapacitated, "the first is let go"); Assert.AreEqual(b, Arch.Bound);
            Assert.IsTrue(session.UseAbility(Hush), "Hush"); yield return null;
            Assert.IsTrue(b.Silenced);
        }
        [UnityTest] public IEnumerator One_song_at_a_time_and_the_jar_sends_them_running()
        {
            Level10(); float walk = session.Kit.MoveSpeedMultiplier;
            Assert.IsTrue(session.UseAbility(Cadence)); yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(ArchivistKit.Song.Cadence, Arch.Singing); Assert.Greater(session.Kit.MoveSpeedMultiplier, walk);
            Assert.IsTrue(session.UseAbility(Dirge)); yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(ArchivistKit.Song.Dirge, Arch.Singing, "the Dirge replaces the Cadence"); Assert.AreEqual(walk, session.Kit.MoveSpeedMultiplier);
            var e = Foe(); Near(e, 12);
            Assert.IsTrue(session.UseAbility(Jar), "Echo-jar"); yield return null;
            Assert.IsTrue(e.Feared);
        }
    }
}
#endif
