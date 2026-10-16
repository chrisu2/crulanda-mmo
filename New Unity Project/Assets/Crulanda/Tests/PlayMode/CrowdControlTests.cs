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
        /// <summary>The Jailer (TALENT_DEPTH.md, 2026-10-10): Hock Cut slows, the Net holds where it stands, the Shackle stuns and (Drag Chain)
        /// drags the foe to your feet; a held foe takes harder blows (Turnkey) and one netted or shackled deals less (Warden).</summary>
        [UnityTest] public IEnumerator The_Jailer_cuts_nets_and_shackles()
        {
            Assert.AreEqual("class.warrior", session.ClassDef.id);
            session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.SetCurrent(100);
            int cut = -1, net = -1, shackle = -1;
            for (int i = 0; i < session.Kit.ActionCount; i++) { var x = session.Kit.ActionAt(i); if (x.id == "warrior.hock_cut") cut = i; if (x.id == "warrior.net") net = i; if (x.id == "warrior.shackle") shackle = i; }
            Assert.AreEqual(11, session.Kit.ActionCount, "eight, and the Jailer's three");
            Assert.IsTrue(cut >= 0 && net >= 0 && shackle >= 0, "all three on the bar");
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(net), "the Net waits on its talent");
            foreach (var id in new[] { "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook", "jl-weighted-net",
                "jl-tight-weave", "jl-tight-weave", "jl-heavy-iron", "jl-heavy-iron", "jl-drag-chain", "jl-wide-mesh", "jl-keepers-due", "jl-keepers-due", "jl-turnkey", "jl-warden" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats();
            Assert.IsNull(session.Kit.ActionLockLabel(net), "the Net learnt");
            var jailer = (WarriorKit)session.Kit; var motor = session.Player.GetComponent<AdventurerMotor>();
            var e = Mob(); motor.Teleport(e.transform.position + Vector3.back * 2); session.Select(e); yield return null;
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f, "nothing held yet");
            Assert.IsTrue(session.UseAbility(cut), "Hock Cut"); yield return null;
            Assert.IsTrue(e.Slowed, "slowed"); Assert.IsTrue(jailer.Holds(e)); Assert.IsFalse(jailer.Binds(e), "a slow is a hold, not a binding");
            Assert.AreEqual(1.03f, session.Kit.PartyDamageMultiplier(e), 1e-3f, "Turnkey: a held foe takes 3% more");
            yield return new WaitForSeconds(1.6f);
            session.Player.Resource.Pool.SetCurrent(50);
            Assert.IsTrue(session.UseAbility(net), "Weighted Net"); yield return null;
            Assert.IsTrue(e.Rooted, "held where it stands"); Assert.IsTrue(jailer.Binds(e));
            Assert.That(e.RootRemaining, Is.InRange(6.5f, 7.01f), "5 s and Tight Weave's two");
            Assert.AreEqual(50 - 15 + 8, session.Player.Resource.Pool.Current, "the Net's cost, and Keeper's Due's 8 Vigor back");
            int plain = 100; var combat = session.Player.GetComponent<Crulanda.Combat.Combatant>();
            int taken = session.Kit.ResolveEnemyHit(e, session.Player, plain);
            Assert.LessOrEqual(taken, Mathf.CeilToInt(plain * (1 - WarriorKit.WardenCut)), "Warden: a netted foe deals less");
            session.Player.Health.ApplyHealing(9999);
            // The Shackle at range: the foe stunned and dragged to your feet.
            var f = session.Enemies.First(x => x != null && x != e && x.actor.IsAlive && x.Camp && !x.Elite && !x.Game && !x.Hidden);
            motor.Teleport(f.transform.position + Vector3.back * 12); session.Select(f); yield return null; yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(shackle), "Shackle Cast"); yield return null;
            Assert.IsTrue(f.Stunned, "stunned"); Assert.IsTrue(jailer.Binds(f));
            Assert.Less(Vector3.Distance(f.transform.position, session.Player.transform.position), 4f, "Drag Chain: dragged to your feet");
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
        /// <summary>Chris's rule (2026-10-15, CC_DESIGN.md): on an elite or a boss each kind of hold lands once a fight, for half its time;
        /// an interrupt is not a hold; a reset fight forgets.</summary>
        [UnityTest] public IEnumerator A_boss_takes_each_hold_once_for_half()
        {
            yield return null;
            var king = session.Enemies.FirstOrDefault(x => x != null && x.Elite && x.actor.DisplayName.Contains("Caddock"));
            Assert.NotNull(king, "Caddock is in Crowsfoot Hollow");
            Assert.IsNull(king.Apply("incap", session.Player, 20), "held once"); Assert.That(king.ControlSecondsLeft, Is.InRange(9f, 10.01f), "for half the time");
            king.Release();
            Assert.AreEqual("Immune (once a fight)", king.Apply("incap", session.Player, 20), "never a second time");
            Assert.IsNull(king.Apply("stun", session.Player, 4), "another kind still lands once");
            Assert.AreEqual("Immune (once a fight)", king.Apply("stun", session.Player, 4));
            Assert.IsNull(king.Apply("silence", session.Player, 3), "an interrupt is not a hold");
            Assert.IsNull(king.Apply("silence", session.Player, 3), "and works again");
            king.ResetFight(); yield return null;
            Assert.IsNull(king.Apply("stun", session.Player, 4), "a new fight, a new stun");
        }
    }
}
#endif
