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
    /// <summary>
    /// Nix's two-part fight (DUNGEON_DESIGN.md 3a; dungeon step D5): the Geode Floor's workshop has the Rock-Eater, a great burrowing beast
    /// Nix rides (Chris, 2026-10-11, no machines), in its saddle (seen, out of reach, not drawn out by walking up to it); when it falls he leaps down and
    /// comes for whoever broke it; the Amber Sigil and the camp's deed come from him, not the machine; both come back with the camp,
    /// Nix inside again.
    /// </summary>
    public class RockEaterTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-rockeater-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit"; PlayerPrefs.SetInt("test.adit", 1);   // level 11 on arrival (the /adit hook): a level-1 character dies to one swing of a level-11 elite
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        EncounterEnemy Find(string mobName) { var e = session.Enemies.Find(x => x != null && x.Camp && x.Elite && x.MobName == mobName); Assert.NotNull(e, mobName); return e; }
        static bool Visible(EncounterEnemy e) { return e.transform.Find("Body").GetComponentsInChildren<Renderer>(true).Any(r => r.enabled); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        void Kill(EncounterEnemy e) { e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3); }

        [UnityTest] public IEnumerator Nix_rides_the_Rock_Eater_and_jumps_out_when_it_breaks()
        {
            var machine = Find(EncounterSession.RockEaterName); var nix = Find(EncounterSession.NixName);
            Assert.AreEqual(nix.CampIndex, machine.CampIndex, "the machine is his camp's");
            Assert.AreSame(machine, nix.Inside); Assert.IsTrue(nix.Hidden, "in the saddle, out of reach"); Assert.IsTrue(Visible(nix), "and seen riding it");
            var seat = machine.GetComponent<ActorVisual>().RiderSeat; Assert.NotNull(seat, "the beast has a saddle");
            Assert.Less(Vector3.Distance(nix.transform.position, seat.position + Vector3.up * EncounterEnemy.RiderLift), .3f, "Nix sits in it");
            Assert.Greater(seat.position.y, machine.transform.position.y, "up on its back");
            Assert.IsTrue(Visible(machine)); Assert.IsFalse(nix.CanAnswer, "no call brings him out");
            Assert.AreEqual(ActorLook.RockEater, machine.GetComponent<ActorVisual>().Look);
            Assert.IsNull(LootContext.From(machine.persistentId, session.Zone.Zone.camps, 11, true).mob, "the machine's corpse rolls no named piece of Nix's");
            // Walking up to the engine (well inside the 8 m a hidden mob pounces from) does not bring him out, and nothing reaches him inside it.
            Assert.AreEqual(11, session.Progress.Level);
            session.Player.GetComponent<AdventurerMotor>().Teleport(machine.transform.position + machine.transform.forward * 4f);
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(nix.Hidden, "still inside"); Assert.AreSame(machine, nix.Inside);
            int before = nix.actor.Health.Pool.Current; nix.Receive(50, session.Player); Assert.AreEqual(before, nix.actor.Health.Pool.Current, "beyond harm");
            // The engine breaks: no sigil from it; Nix is out at the wreck, fighting whoever broke it.
            machine.Receive(1, session.Player); Kill(machine); yield return Frames(2);
            Assert.IsFalse(machine.actor.IsAlive); Assert.IsFalse(session.Progress.keys.Contains("sigil.amber"), "the sigil is Nix's, not the machine's");
            Assert.IsFalse(session.Progress.elitesSlain.Any(k => k.Contains("rockeater")), "no deed for the machine");
            Assert.IsNull(nix.Inside); Assert.IsFalse(nix.Hidden, "out"); Assert.IsTrue(Visible(nix), "and seen");
            Assert.Less(Vector3.Distance(nix.transform.position, machine.transform.position), 4, "at the wreck");
            float t = 0; while (t < 3 && nix.Victim != session.Player) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(session.Player, nix.Victim, "he comes for you");
            Kill(nix); yield return Frames(2);
            Assert.Contains("sigil.amber", session.Progress.keys, "the Amber Sigil from Nix");
            Assert.IsTrue(session.Progress.elitesSlain.Any(k => k.EndsWith("." + nix.CampIndex) && k.Contains("nix")), "and the camp's deed");
        }
        /// <summary>The saddle and the crystals measured on its hide (Chris's captures, 2026-10-11: the saddle at its mouth, a strap standing up
        /// off its back like a pole, the crystals at its tail). After it has stood a while, its idle moving its back: the saddle in the middle
        /// of its length on top of its back with Nix in it, every crystal's foot in its hide and the ridge behind the saddle, and the girth
        /// lying down its flanks, none of it above the saddle.</summary>
        [UnityTest] public IEnumerator Its_saddle_and_crystals_sit_on_its_back()
        {
            var beast = Find(EncounterSession.RockEaterName); var nix = Find(EncounterSession.NixName); var look = beast.GetComponent<ActorVisual>();
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(look.GearFitted, "its gear fitted on its idle, two frames after it was built");
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, look.BeastModel.Animator.cullingMode, "its bones posed even out of sight (in the game it stands unseen when built)");
            var surface = new HideSurface(look.BeastModel.Renderers, look.BeastModel.Height); var hide = surface.Points;
            Assert.Greater(hide.Length, 100, "its hide measured");
            var body = beast.transform.Find("Body"); Vector3 ahead = body.forward, o = body.position;
            float back = hide.Min(v => Vector3.Dot(v - o, ahead)), front = hide.Max(v => Vector3.Dot(v - o, ahead));
            float Frac(Vector3 p) { return (Vector3.Dot(p - o, ahead) - back) / (front - back); }
            // Measured on the hide's surface (rays on its triangles; its vertices lie too far apart on a low-poly hide grown two and a half times).
            float Top(Vector3 p) { return surface.TopAt(p) ?? float.NaN; }
            float Gap(Vector3 p) { float top = Top(p); return float.IsNaN(top) ? 99 : Mathf.Abs(p.y - top); }   // how far over or into its back
            Vector3 aside = body.right;
            float Flank(Vector3 p)   // how far off its side, at its own height and place along it
            {
                float x = Vector3.Dot(p - o, aside), sign = Mathf.Sign(x);
                var met = surface.Cast(p + aside * sign * 3, -aside * sign);
                return met.HasValue ? Mathf.Abs(Vector3.Dot(met.Value - p, aside)) : 99;
            }
            var gear = body.GetComponentsInChildren<Transform>(true);
            var saddle = gear.FirstOrDefault(t => t.name == "Saddle"); Assert.NotNull(saddle, "a saddle");
            string at = " (saddle at " + Frac(saddle.position).ToString("0.00") + " of its length, " + saddle.position.y.ToString("0.00") + " m up, its back there " + Top(saddle.position).ToString("0.00") + ", on " + saddle.parent.name + ")";
            Assert.That(Frac(saddle.position), Is.InRange(.3f, .65f), "the saddle in the middle of its length, not at its head" + at);
            Assert.Less(Mathf.Abs(saddle.position.y - .1f - Top(saddle.position)), .3f, "on top of its back" + at);
            Assert.Less(Vector3.Distance(nix.transform.position, look.RiderSeat.position + Vector3.up * EncounterEnemy.RiderLift), .3f, "Nix in it");
            var crystals = gear.Where(t => t.name == "Geode crystal").ToList(); Assert.GreaterOrEqual(crystals.Count, 7, "a ridge of crystal");
            foreach (var c in crystals)
            {
                var foot = c.position - c.up * c.lossyScale.y / 2;
                Assert.Less(Gap(foot), .3f, "a crystal's foot on its back, not in the air: " + Gap(foot).ToString("0.00") + " m off at " + Frac(foot).ToString("0.00") + at);
            }
            Assert.GreaterOrEqual(crystals.Count(c => Frac(c.position) < Frac(saddle.position)), 5, "the ridge behind the saddle" + at);
            var girth = gear.Where(t => t.name == "Girth").ToList(); Assert.GreaterOrEqual(girth.Count, 2, "a girth");
            foreach (var g in girth)
            {
                Assert.Less(Flank(g.position), .25f, "the girth lies on its flank: " + Flank(g.position).ToString("0.00") + " m off" + at);
                Assert.Less(g.position.y + g.lossyScale.y / 2 * Mathf.Abs(g.up.y), saddle.position.y + .15f, "and stands up above nothing" + at);
            }
        }
        [UnityTest] public IEnumerator Both_come_back_with_the_camp_and_Nix_is_inside_again()
        {
            var machine = Find(EncounterSession.RockEaterName); var nix = Find(EncounterSession.NixName);
            machine.RespawnSeconds = nix.RespawnSeconds = 1;
            Kill(machine); yield return Frames(2); Kill(nix); yield return Frames(2);
            Assert.IsFalse(machine.actor.IsAlive); Assert.IsFalse(nix.actor.IsAlive);
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(machine.actor.IsAlive, "the engine is back"); Assert.IsTrue(nix.actor.IsAlive, "and so is Nix");
            Assert.AreSame(machine, nix.Inside, "in the saddle again"); Assert.IsTrue(nix.Hidden); Assert.IsTrue(Visible(nix));
            Assert.IsTrue(Visible(machine));
            Assert.IsFalse(machine.transform.Find("Body").GetComponent<MeshRenderer>().enabled, "the beast's placeholder capsule stays off");
            Assert.IsFalse(nix.transform.Find("Body").GetComponent<MeshRenderer>().enabled, "and Nix's");
        }
    }
}
#endif
