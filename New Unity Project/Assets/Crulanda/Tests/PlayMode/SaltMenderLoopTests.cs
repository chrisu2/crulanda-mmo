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
using Crulanda.Combat;

namespace Crulanda.Tests
{
    /// <summary>The Salt-Mender in play (Phase 9.2, 2026-10-11): its own character on Salt; Salt Shard at range; Binding Salt holds and
    /// White Crust opens the held foe to the party; Seal the Wound silences; the Salt-Ward grounds a blow into Salt; Draw the Line taunts;
    /// Sung Wall stands the party behind salt; the Song of Sifting burns a pack; Calcify (a talent) stuns.</summary>
    public class SaltMenderLoopTests
    {
        string root; EncounterSession session;
        SaltMenderKit Salt { get { return session.Kit as SaltMenderKit; } }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-salt-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.saltmender";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(Salt, "the Salt-Mender's kit");
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null; SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        void Level(int n) { session.Progress.experience = EncounterProgress.XpForLevel(n); session.Player.SetLevel(n); session.Player.Resource.Pool.SetCurrent(session.Player.Resource.Pool.Max); }
        EncounterEnemy Foe() { var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Elite && !x.Game && !x.Engaged); Assert.NotNull(e, "a living mob"); return e; }
        void Near(EncounterEnemy e, float d) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * d); session.Select(e); }
        const int Shard = 0, Binding = 1, Ward = 2, Seal = 3, Line = 4, Wall = 5, Sifting = 6, Calcify = 7;

        [UnityTest] public IEnumerator The_Salt_Mender_is_its_own_character_on_Salt()
        {
            Assert.AreEqual("class.saltmender", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Salt, session.ClassDef.resource);
            Assert.AreEqual(8, session.Kit.ActionCount); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Binding));
            Level(10); Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Calcify));
            Assert.AreEqual(ActorLook.Mage, EncounterSession.LookForClass("class.saltmender"));
            yield return null;
        }
        [UnityTest] public IEnumerator Salt_Shard_hits_at_range_and_spends_Salt()
        {
            Level(10); var e = Foe(); Near(e, 15); yield return null;
            int before = e.actor.Health.Pool.Current, salt = session.Player.Resource.Pool.Current;
            Assert.IsTrue(session.UseAbility(Shard), "Salt Shard at 15 m");
            Assert.Less(e.actor.Health.Pool.Current, before, "the shard lands"); Assert.Less(session.Player.Resource.Pool.Current, salt, "it costs Salt");
        }
        [UnityTest] public IEnumerator Binding_Salt_holds_and_White_Crust_opens_it_to_the_party()
        {
            Level(10);
            foreach (var id in new[] { "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-white-crust", "ss-white-crust", "ss-white-crust" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats();
            var e = Foe(); Near(e, 8); yield return null;
            Assert.IsTrue(session.UseAbility(Binding), "Binding Salt"); yield return null;
            Assert.IsTrue(e.Incapacitated, "held in white crystal"); Assert.IsTrue(Salt.Holds(e));
            Assert.AreEqual(1.09f, session.Kit.PartyDamageMultiplier(e), 1e-3f, "White Crust: 9% more from the party on a held foe");
        }
        [UnityTest] public IEnumerator Seal_the_Wound_silences()
        {
            Level(10); var e = Foe(); Near(e, 10); yield return null;
            Assert.IsTrue(session.UseAbility(Seal), "Seal the Wound"); yield return null;
            Assert.IsTrue(e.Silenced, "silenced");
        }
        [UnityTest] public IEnumerator The_Salt_Ward_grounds_a_blow_into_Salt_and_the_line_draws_the_foe()
        {
            Level(10); var e = Foe(); Near(e, 3); yield return null;
            Assert.IsTrue(session.UseAbility(Ward), "Salt-Ward"); Assert.IsTrue(Salt.Warded); Assert.Greater(session.Player.GetComponent<Combatant>().Barrier, 0, "a ward on you");
            StringAssert.Contains("Salt-Ward", session.Kit.StatusLine);
            session.Player.Resource.Pool.SetCurrent(50);
            session.Kit.ResolveEnemyHit(e, session.Player, 10);
            Assert.AreEqual(50 + (int)SaltMenderKit.WardGround, session.Player.Resource.Pool.Current, "the blow grounded into Salt");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Line), "Draw the Line"); yield return null;
            Assert.AreSame(session.Player, e.Victim, "it comes for you");
        }
        [UnityTest] public IEnumerator Sung_Wall_stands_the_party_behind_salt()
        {
            Level(10); yield return null;
            var combat = session.Player.GetComponent<Combatant>();
            Assert.IsTrue(session.UseAbility(Wall), "Sung Wall"); Assert.IsTrue(Salt.WallSung);
            var e = Foe();
            int hp = session.Player.Health.Pool.Current; session.Kit.ResolveEnemyHit(e, session.Player, 100);
            Assert.AreEqual(hp - 85, session.Player.Health.Pool.Current, 1, "15% less behind the wall");
        }
        [UnityTest] public IEnumerator The_Song_of_Sifting_burns_and_Calcify_stuns()
        {
            Level(15);
            foreach (var id in new[] { "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-calcify" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats(); Assert.IsNull(session.Kit.ActionLockLabel(Calcify), "Calcify learnt");
            var e = Foe(); Near(e, 12); yield return null;
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Sifting), "Song of Sifting");
            yield return new WaitForSeconds(2.2f);
            Assert.Less(e.actor.Health.Pool.Current, before, "it burns"); StringAssert.Contains("Sifted", session.Kit.TargetStatus(e));
            if (!e.actor.IsAlive) yield break;
            Assert.IsTrue(session.UseAbility(Calcify), "Calcify"); yield return null;
            Assert.IsTrue(e.Stunned, "calcified");
        }
    }
}
#endif
