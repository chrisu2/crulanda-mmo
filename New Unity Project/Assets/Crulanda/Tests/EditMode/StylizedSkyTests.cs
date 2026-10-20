using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Play note 254 (Chris, 2026-10-19: "some better sky boxes in my assets Stylized Skyboxes | FREE"): the stylized skies by the hour and
    /// the weather (StylizedSky, WorldClock). Data and files only: no zone is built.
    /// </summary>
    public class StylizedSkyTests
    {
        static string Root { get { return Path.Combine(Application.dataPath, "Crulanda"); } }
        static string SkyDir { get { return Path.Combine(Root, "Resources", "Sky", "Stylized"); } }
        /// <summary>The pack's own GUIDs for its pictures (Stylized Skyboxes FREE.unitypackage).</summary>
        static readonly Dictionary<string, string> PackGuids = new Dictionary<string, string> {
            { "Noon", "c7024ed95b4f3ac4c8212989e1db998f" }, { "Noon (Cloudy)", "9187479000d82bf4f941adc02b9ba286" }, { "Noon (Overcast)", "bbaf4ac71210ce446b22becb60e739d1" },
            { "Sunrise", "d0d59302157ea1244817b96cdbb7f53b" }, { "Sunset", "4b9d96d54fb751d4d92da9e7ae4f2774" }, { "Dusk", "42bb90c862b40c545819434422d7c5ea" },
            { "Night", "ee1ef284a95b0e24582670375fbb1d35" }, { "NightGalaxy", "6bd15abdb69997640ad668fffa5206c6" }, { "Aurora", "0bdf7878b1cd15d478b4b74283b72251" } };
        static string Guid(string meta) { var m = Regex.Match(File.ReadAllText(meta), @"^guid: ([0-9a-f]{32})", RegexOptions.Multiline); return m.Success ? m.Groups[1].Value : null; }
        static ZoneDefinition Zone(string id) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Root, "EncounterContent", "Zones", id + ".json"))); }
        static IEnumerable<string> ZoneIds() { return Directory.GetFiles(Path.Combine(Root, "EncounterContent", "Zones"), "*.json").Select(Path.GetFileNameWithoutExtension); }
        static float W(List<(int sky, float weight)> mix, int sky) { return mix.Where(m => m.sky == sky).Sum(m => m.weight); }

        [Test] public void Every_sky_referenced_exists()
        {
            Assert.AreEqual(9, StylizedSky.Names.Length);
            foreach (var name in StylizedSky.Names)
            {
                var exr = Path.Combine(SkyDir, "HDRI", name + ".exr");
                Assert.IsTrue(File.Exists(exr), name + ".exr"); Assert.IsTrue(File.Exists(exr + ".meta"), name + ".exr.meta");
                Assert.AreEqual(PackGuids[name], Guid(exr + ".meta"), name + " keeps the pack's GUID");
                Assert.Greater(File.ReadAllLines(exr + ".meta").Length, 20, name + ".exr.meta is the pack's full meta");
            }
            Assert.IsTrue(File.Exists(Path.Combine(SkyDir, "Sky_Noon.mat")), "Sky_Noon.mat: the runtime material's source");
            // Each pack material shows one of the pictures.
            var known = new HashSet<string>(PackGuids.Values);
            foreach (var mat in Directory.GetFiles(SkyDir, "*.mat"))
            {
                var m = Regex.Match(File.ReadAllText(mat), @"_TexA:\s+m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})");
                Assert.IsTrue(m.Success && known.Contains(m.Groups[1].Value), Path.GetFileName(mat) + " shows a picture of the pack");
            }
            // A zone's own day sky is one of them.
            foreach (var id in ZoneIds())
            {
                var l = Zone(id).lighting; if (l == null || string.IsNullOrEmpty(l.skyDay)) continue;
                Assert.GreaterOrEqual(StylizedSky.IndexOf(l.skyDay), 0, id + "'s skyDay " + l.skyDay);
            }
        }

        [Test] public void The_skies_are_Built_in()
        {
            string shaderPath = Path.Combine(Root, "World", "Shaders", "StylizedSky.shader"), shader = File.ReadAllText(shaderPath), shaderGuid = Guid(shaderPath + ".meta");
            StringAssert.Contains("Shader \"Crulanda/StylizedSky\"", shader); StringAssert.Contains("CGPROGRAM", shader);
            foreach (var srp in new[] { "UniversalPipeline", "HDRenderPipeline", "HLSLPROGRAM" }) StringAssert.DoesNotContain(srp, shader, "Built-in only");
            foreach (var mat in Directory.GetFiles(Path.Combine(Root, "Resources", "Sky"), "*.mat", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(mat);
                StringAssert.Contains("m_Shader: {fileID: 4800000, guid: " + shaderGuid + ", type: 3}", text, Path.GetFileName(mat) + " on Crulanda/StylizedSky");
                StringAssert.DoesNotContain("MonoBehaviour", text, Path.GetFileName(mat) + ": the HDRP material-version block is gone");
            }
            foreach (var ext in new[] { "*.shadergraph", "*.unity", "*.prefab", "*.cs", "*.asmdef", "*.obj", "*.fbx" })
                Assert.IsEmpty(Directory.GetFiles(Path.Combine(Root, "Resources", "Sky"), ext, SearchOption.AllDirectories), "no demo or Fog Ring files: " + ext);
            foreach (var meta in Directory.GetFiles(Path.Combine(SkyDir, "HDRI"), "*.exr.meta"))
            {
                string text = File.ReadAllText(meta);
                StringAssert.Contains("textureShape: 1", text, Path.GetFileName(meta) + " a flat lat-long picture");
                StringAssert.Contains("enableMipMap: 0", text, Path.GetFileName(meta) + " no mips");
                foreach (Match m in Regex.Matches(text, @"maxTextureSize: (\d+)")) Assert.LessOrEqual(int.Parse(m.Groups[1].Value), 2048, Path.GetFileName(meta));
            }
            StringAssert.Contains("Assets/Crulanda/Resources/Sky/", File.ReadAllText(Path.Combine(Root, "Editor", "ThirdPartyImport.cs")), "the importer holds the skies' settings");
        }

        [Test] public void Every_hour_maps_to_a_sky()
        {
            foreach (var (cloudy, overcast) in new[] { (0f, 0f), (.6f, 0f), (1f, .4f), (1f, 1f) })
                foreach (bool aurora in new[] { false, true })
                    foreach (int day in new[] { -1, StylizedSky.Overcast, StylizedSky.Sunset })
                        for (float h = 0; h <= 24.001f; h += .05f)
                        {
                            var mix = StylizedSky.Weights(h, cloudy, overcast, aurora, day); string at = "hour " + h.ToString("0.00") + ", cloud " + cloudy + "/" + overcast;
                            Assert.That(mix.Count, Is.InRange(1, 3), at);
                            Assert.AreEqual(1, mix.Sum(m => m.weight), 1e-4f, at);
                            foreach (var (sky, weight) in mix) { Assert.That(sky, Is.InRange(0, StylizedSky.Names.Length - 1), at); Assert.Greater(weight, 0, at); }
                        }
            Assert.AreEqual(1, W(StylizedSky.Weights(12, 0, 0, false), StylizedSky.Noon), 1e-4f, "a clear noon");
            Assert.AreEqual(1, W(StylizedSky.Weights(13, 1, 0, false), StylizedSky.Cloudy), 1e-4f, "a cloudy afternoon");
            Assert.AreEqual(1, W(StylizedSky.Weights(11, 1, 1, false), StylizedSky.Overcast), 1e-4f, "an overcast morning");
            Assert.AreEqual(1, W(StylizedSky.Weights(6.75f, 0, 0, false), StylizedSky.Sunrise), 1e-4f, "sunrise");
            Assert.Greater(W(StylizedSky.Weights(18.5f, 0, 0, false), StylizedSky.Sunset), .5f, "sunset");
            Assert.AreEqual(1, W(StylizedSky.Weights(19.75f, 0, 0, false), StylizedSky.Dusk), 1e-4f, "dusk");
            Assert.AreEqual(1, W(StylizedSky.Weights(2, 0, 0, false), StylizedSky.Galaxy), 1e-4f, "a clear night: the galaxy");
            Assert.AreEqual(1, W(StylizedSky.Weights(2, 0, 0, true), StylizedSky.Aurora), 1e-4f, "an aurora night");
            Assert.AreEqual(1, W(StylizedSky.Weights(23, 1, 1, true), StylizedSky.Night), 1e-4f, "a clouded night: no stars, no aurora");
            // The painted moon is painted over (the moons are TwinMoons'); only the night with a moon has one.
            Assert.Greater(StylizedSky.Hide(StylizedSky.Night).z, 0);
            foreach (int s in Enumerable.Range(0, 9).Where(s => s != StylizedSky.Night)) Assert.AreEqual(0, StylizedSky.Hide(s).z, StylizedSky.Names[s]);
            // Aurora nights are rare.
            int n = Enumerable.Range(0, 600).Count(StylizedSky.AuroraNight); Assert.That(n, Is.InRange(60, 140), "about one night in six");
        }

        [Test] public void Each_zone_keeps_its_own_sky()
        {
            var hallows = Zone("hallows").lighting; Assert.AreEqual("Noon (Overcast)", hallows.skyDay, "Hallow's Creek: a grey sky (b1 ch.21:19)");
            Assert.AreEqual(1, W(StylizedSky.Weights(12, 0, 0, false, StylizedSky.IndexOf(hallows.skyDay)), StylizedSky.Overcast), 1e-4f, "grey on a clear day too");
            Assert.IsFalse(hallows.aurora, "no aurora over the still town");
            // Khaven follows the weather and the clock like the others (Chris 2026-10-19, "CHECK LORE CANON": its only canon is its map,
            // with no sky or hour; the held dusk was GAME-ONLY, dropped). Its weather never clears, so its day sky stays grey.
            Assert.IsTrue(string.IsNullOrEmpty(Zone("khaven").lighting.skyDay), "Khaven has no forced day sky");
            Assert.AreEqual(0, Zone("khaven").lighting.sunHigh, "and no held-low sun: its light climbs with the clock");
            // The Lowtowns' yellow smog: a thick haze of its own fog colour, a yellow tint.
            var low = Zone("lowtowns").lighting; float haze = StylizedSky.ZoneHaze(low.skyHaze); var tint = StylizedSky.ZoneTint(low.skyTint);
            Assert.Greater(haze, .8f, "the smog");
            foreach (var id in ZoneIds().Where(z => z != "lowtowns")) { var l = Zone(id).lighting; if (l != null) Assert.Less(StylizedSky.ZoneHaze(l.skyHaze), haze, id + " hazes less than the Lowtowns"); }
            Assert.Greater(tint.r, tint.b, "the Lowtowns' sky leans yellow");
            var fog = ZoneColors.Parse(low.fogColor, Color.grey); Assert.Greater(fog.r, fog.b, "and its haze is its yellow-brown fog");
            // The aurora only on the Peaks and the Ash Rim.
            foreach (var id in ZoneIds()) { var l = Zone(id).lighting; if (l != null) Assert.AreEqual(id == "peaks" || id == "ashrim", l.aurora, id + " aurora"); }
            // Every zone takes the stylized sky (none falls back to the procedural one), each with its own tint where it gave one.
            foreach (var id in ZoneIds()) { var l = Zone(id).lighting; if (l != null) Assert.AreNotEqual("procedural", l.skyStyle, id); }
            Assert.AreNotEqual(StylizedSky.ZoneTint(Zone("khaven").lighting.skyTint), StylizedSky.ZoneTint(Zone("oakhaven").lighting.skyTint), "Khaven's sky is not Oakhaven's");
        }
    }
}
