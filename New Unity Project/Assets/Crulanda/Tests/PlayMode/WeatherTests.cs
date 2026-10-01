#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Weather in play (Oakhaven):
    /// - rain falls, wets and darkens the ground, rings the water, and stops under a roof; clearing dries the ground;
    /// - every kind keeps the fog in its band and the light readable, by day and by night;
    /// - the zone's own weather follows its schedule, and a turn is announced in the chat;
    /// - the wind reaches the shaders, and the cloud layer is up.
    /// </summary>
    public class WeatherTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-weather-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
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

        [UnityTest] public IEnumerator Rain_falls_wets_the_ground_and_clearing_dries_it()
        {
            var weather = WorldWeather.Active; var zone = ZoneBuilder.Active;
            Assert.IsNotNull(weather, "The zone starts its weather."); Assert.IsNotNull(zone.GroundMaterial);
            weather.Force(WeatherKind.Clear, true); yield return null;
            float dry = zone.GroundMaterial.color.grayscale;
            weather.Force(WeatherKind.Rain, true);
            for (int i = 0; i < 10; i++) yield return null;
            var rain = weather.transform.Find("Rain").GetComponent<ParticleSystem>();
            Assert.Greater(rain.emission.rateOverTime.constant, 1000, "Rain falls.");
            Assert.IsTrue(rain.isPlaying);
            Assert.AreEqual(1, weather.Wet, 1e-3f); Assert.Less(zone.GroundMaterial.color.grayscale, dry - .02f, "The wet ground is darker.");
            Assert.Greater(WorldWeather.Now.dim, .5f, "Rain comes under cloud.");
            Assert.Less(RenderSettings.fogEndDistance, zone.Zone.lighting.fogEnd, "The fog closes in.");
            Assert.AreEqual("Rain", WeatherSchedule.Name(weather.Kind));
            weather.Force(WeatherKind.Clear, true); yield return null;
            Assert.AreEqual(0, rain.emission.rateOverTime.constant, 1e-3f, "Clear skies: no rain.");
            Assert.AreEqual(dry, zone.GroundMaterial.color.grayscale, .005f, "Cleared at once (forced), the ground is dry again.");
        }

        [UnityTest] public IEnumerator No_rain_falls_on_a_camera_under_a_roof()
        {
            var weather = WorldWeather.Active; var zone = ZoneBuilder.Active;
            var inn = Array.Find(zone.Zone.props, p => p != null && p.kind == "inn");
            Assert.IsNotNull(inn, "Oakhaven has its inn.");
            Assert.IsTrue(zone.UnderRoof(inn.at), "The inn's floor is under its roof.");
            Assert.IsFalse(zone.UnderRoof(zone.Zone.spawns.player), "The road in is open to the sky.");
            weather.Force(WeatherKind.Rain, true);
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.enabled = false;
            var cam = Camera.main.transform;
            for (int i = 0; i < 5; i++) { cam.position = zone.Ground(inn.at, 1.7f); yield return null; }
            var rain = weather.transform.Find("Rain").GetComponent<ParticleSystem>();
            Assert.AreEqual(0, rain.emission.rateOverTime.constant, 1e-3f, "Indoors, nothing falls.");
            // Under the porch, by the lantern (local z = -(d/2 + 1.2); a local offset z is (z sin, z cos) in the world, as InBuilding turns it back).
            float yr = inn.rotation * Mathf.Deg2Rad, d = inn.size.y > 0 ? inn.size.y : 8;
            var porch = inn.at + new Vector2(Mathf.Sin(yr), Mathf.Cos(yr)) * -(d / 2 + 1.2f);
            Assert.IsTrue(zone.UnderRoof(porch), "Under the inn's porch, no rain falls.");
            for (int i = 0; i < 5; i++) { cam.position = zone.Ground(porch, 1.7f); yield return null; }
            Assert.AreEqual(0, rain.emission.rateOverTime.constant, 1e-3f, "Under the porch, nothing falls.");
            motor.enabled = true;
        }

        [UnityTest] public IEnumerator Every_kind_keeps_the_fog_in_its_band_and_the_light_readable()
        {
            var weather = WorldWeather.Active; var lighting = ZoneBuilder.Active.Zone.lighting;
            var sun = RenderSettings.sun;
            foreach (WeatherKind kind in Enum.GetValues(typeof(WeatherKind)))
            {
                weather.Force(kind, true);
                foreach (float hour in new[] { 11f, 23f })
                {
                    WorldClock.Hour = hour; yield return null; yield return null;
                    string at = kind + " at " + hour;
                    Assert.That(RenderSettings.fogEndDistance, Is.InRange(Mathf.Min(lighting.fogEnd, 55) - .01f, lighting.fogEnd + .01f), at + ": fog end");
                    Assert.Less(RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, at);
                    Assert.Greater(RenderSettings.ambientSkyColor.grayscale, hour > 20 ? .15f : .3f, at + ": skylight keeps faces readable");
                    if (sun != null) Assert.Greater(sun.intensity, hour > 20 ? .05f : .2f, at + ": the sun or moon still lights the land");
                }
            }
        }

        [UnityTest] public IEnumerator The_zone_weather_follows_its_schedule_and_a_turn_is_announced()
        {
            var weather = WorldWeather.Active; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            weather.Release(true);
            // The first spell change after spell 5 that has something to say.
            int i = 6; while (weather.Schedule.Spell(i) == weather.Schedule.Spell(i - 1) || WeatherSchedule.Herald(weather.Schedule.Spell(i - 1), weather.Schedule.Spell(i)) == null) i++;
            WorldWeather.Clock = i * (double)WeatherSchedule.SpellSeconds - 5;   // settle into the spell before
            yield return null; yield return null;
            Assert.AreEqual(weather.Schedule.Spell(i - 1), weather.Kind);
            WorldWeather.Clock = i * (double)WeatherSchedule.SpellSeconds - 1e-3;
            var heard = new List<WeatherKind>(); Action<WeatherKind, WeatherKind> ear = (from, to) => heard.Add(to);
            WorldWeather.Turned += ear;
            for (int f = 0; f < 5; f++) yield return null;
            WorldWeather.Turned -= ear;
            Assert.AreEqual(weather.Schedule.Spell(i), weather.Kind, "The weather is the schedule's.");
            Assert.Contains(weather.Schedule.Spell(i), heard, "The turn is raised.");
            Assert.Contains(WeatherSchedule.Herald(weather.Schedule.Spell(i - 1), weather.Schedule.Spell(i)), session.Messages, "And said in the chat.");
        }

        [UnityTest] public IEnumerator The_wind_reaches_the_shaders_and_the_clouds_are_up()
        {
            var weather = WorldWeather.Active;
            weather.Force(WeatherKind.Windy, true); yield return null;
            var wind = Shader.GetGlobalVector("_WeatherWind");
            Assert.AreEqual(WeatherLook.Of(WeatherKind.Windy).wind, wind.z, 1e-3f, "Gust strength");
            Assert.AreEqual(1, new Vector2(wind.x, wind.y).magnitude, 1e-3f, "A direction on the ground");
            var layer = weather.transform.Find("Cloud layer");
            Assert.IsNotNull(layer, "The cloud layer is up."); Assert.IsNotNull(WorldWeather.CloudNoise);
            var mat = layer.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreEqual("Crulanda/Clouds", mat.shader.name);
            Assert.AreEqual(WeatherLook.Of(WeatherKind.Windy).clouds, mat.GetFloat("_Coverage"), 1e-3f);
        }
    }
}
#endif
