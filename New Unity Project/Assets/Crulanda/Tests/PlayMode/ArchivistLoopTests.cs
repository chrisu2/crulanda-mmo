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
        const int Note = 0, Lull = 1, Hush = 2, Bind = 3, Cadence = 4, Dirge = 5, Jar = 6, Phrase = 7, Mending = 8, Chorus = 9;
        IEnumerator Cast() { yield return new WaitForSeconds(1.7f); }

        [UnityTest] public IEnumerator The_Archivist_is_its_own_character()
        {
            Assert.AreEqual("class.archivist", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(14, session.Kit.ActionCount, "seven, the Cantor's three, Unwrite, Erasure, Stillness, and the Mending Verse (2026-10-13)"); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Lull));
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Chorus), "the Chorus waits on its talent");
            Assert.IsFalse(session.Kit.MeleeAutoAttacks); Assert.AreEqual(ActorLook.Mage, EncounterSession.LookForClass("class.archivist"));
            yield return null;
        }
        /// <summary>The Cantor (TALENT_DEPTH.md, 2026-10-10): a Mending Phrase heals the most hurt, the Song of Mending mends the party
        /// each second (and is one of the songs), the Chorus puts a barrier on everyone near.</summary>
        [UnityTest] public IEnumerator The_Cantor_mends_sings_and_shields()
        {
            Level10(); session.Player.Resource.Pool.SetCurrent(session.Player.Resource.Pool.Max);
            foreach (var id in new[] { "ca-clear-voice", "ca-clear-voice", "ca-clear-voice", "ca-clear-voice", "ca-clear-voice", "ca-chorus" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            Assert.IsNull(session.Kit.ActionLockLabel(Chorus), "the Chorus learnt");
            var hp = session.Player.Health; hp.ApplyDamage(hp.Pool.Max / 2); int hurt = hp.Pool.Current;
            Assert.IsTrue(session.UseAbility(Phrase), "Mending Phrase"); yield return Cast();
            Assert.Greater(hp.Pool.Current, hurt, "the phrase heals you"); hurt = hp.Pool.Current;
            Assert.IsTrue(session.UseAbility(Mending), "the Song of Mending"); yield return null;
            Assert.AreEqual(ArchivistKit.Song.Mending, Arch.Singing);
            yield return new WaitForSeconds(2.2f);
            Assert.Greater(hp.Pool.Current, hurt, "the song mends you each second");
            Assert.IsTrue(session.UseAbility(Cadence), "Cadence"); yield return null; Assert.AreEqual(ArchivistKit.Song.Cadence, Arch.Singing, "one song at a time");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Chorus), "the Chorus"); yield return null;
            Assert.Greater(session.Player.GetComponent<Crulanda.Combat.Combatant>().Barrier, 0, "a barrier on you");
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
