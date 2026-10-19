using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Play note 247 (2026-10-19, Chris: "replace all wooden barrels with this in myassets . simple wood barrel"): every wooden barrel in
    /// the game is the Simple Wood Barrel (Resources/Props/WoodBarrel). No old barrel model is named in the code; the new one loads, stands
    /// at the height asked with its foot on the spot, and wears a Built-in Standard material with the pack's three maps (capped at 1024);
    /// the Spoon's dice-pit kegs, which are seats, keep their seat height.
    /// </summary>
    public class WoodBarrelTests
    {
        static string Crulanda { get { return Path.Combine(Application.dataPath, "Crulanda"); } }

        /// <summary>The old barrels, kegs and casks the builders used to stand: the Fantasy kit's (plain, with apples, on a holder), the
        /// market pack's, the Megapack's, KayKit's barrels and keg, the Medieval beer barrel, the Dungeon kit's (the Adit's Piece).</summary>
        static readonly string[] OldBarrels = {
            "\"Fantasy/Barrel\"", "Fantasy/Barrel_Apples", "Barrel_Apples\"", "Barrel_Holder\"", "Fantasy2/barrel_001", "Megapack/Models/Barrel_1", "Megapack/Models/Barrel_Open",
            "KayKit/barrel_large", "KayKit/barrel_small", "KayKit/keg", "Medieval/Prefabs/BeerBarrelV1", "Dungeon/Barrel", "Piece(\"Barrel\"",
        };

        [Test] public void No_old_barrel_model_is_named_in_the_code()
        {
            var found = new StringBuilder(); int files = 0;
            foreach (var dir in new[] { "Scripts", "Editor" })
                foreach (var f in Directory.GetFiles(Path.Combine(Crulanda, dir), "*.cs", SearchOption.AllDirectories))
                {
                    files++; var lines = File.ReadAllLines(f);
                    for (int i = 0; i < lines.Length; i++)
                        foreach (var old in OldBarrels)
                            if (lines[i].Contains(old)) found.Append(Path.GetFileName(f) + ":" + (i + 1) + " " + old + "; ");
                }
            Assert.Greater(files, 100, "the code was read");
            Assert.AreEqual("", found.ToString(), "an old barrel model still named: ");
            // The builders' barrel helper stands the wood barrel.
            var main = File.ReadAllText(Path.Combine(Crulanda, "Scripts", "World", "ZoneBuilder.cs"));
            Assert.IsTrue(Regex.IsMatch(main, @"GameObject Barrel\(Transform t[^\n]*\n\s*\{\s*\n\s*if \(!painted\) \{ var model = ModelProp\(t, WoodBarrelPath,"), "ZoneBuilder.Barrel stands the wood barrel first");
        }

        [Test] public void The_wood_barrel_loads_and_stands_on_its_foot()
        {
            Assert.IsTrue(File.Exists(Path.Combine(Crulanda, "Resources", "Props", "WoodBarrel", "BARREL_1.fbx")), "the model is in the project");
            Assert.IsTrue(ZoneBuilder.HasProp(ZoneBuilder.WoodBarrelPath), "it loads from Resources");
            Assert.IsTrue(ZoneBuilder.KitProps.Any(k => k.path == ZoneBuilder.WoodBarrelPath), "it is in the kit row (PropCapture)");
            var root = new GameObject("barrel").transform;
            try
            {
                var go = ZoneBuilder.ModelProp(root, ZoneBuilder.WoodBarrelPath, new Vector3(1, 0, 2), 30, .95f, true);
                Assert.NotNull(go, "placed");
                var b = ZoneBuilder.PropBounds(root);
                Assert.AreEqual(.95f, b.size.y, .02f, "stands .95 m, as the Fantasy kit's barrel did");
                Assert.AreEqual(0, b.min.y, .02f, "its foot on the spot");
                Assert.That(Mathf.Max(b.size.x, b.size.z), Is.InRange(.55f, .8f), "upright, about two thirds as wide as tall (not on its side)");
                Assert.NotNull(go.GetComponent<BoxCollider>(), "solid when asked");
                var mesh = ZoneBuilder.WoodBarrelMesh; Assert.NotNull(mesh, "its mesh");
                Assert.IsTrue(mesh.isReadable, "readable, for the joined meshes (the Lowtowns' clutter, the Spoon)");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test] public void Its_material_is_Built_in_Standard_with_the_packs_maps()
        {
            var m = ZoneBuilder.WoodBarrelMaterial; Assert.NotNull(m, "Resources/Props/WoodBarrel/WoodBarrel.mat");
            Assert.AreEqual("Standard", m.shader.name, "the Built-in Standard shader");
            Assert.IsTrue(m.shader.isSupported, "the shader runs here");
            foreach (var (prop, file) in new[] { ("_MainTex", "BARREL_None_AlbedoTransparency"), ("_BumpMap", "BARREL_None_Normal"), ("_MetallicGlossMap", "BARREL_None_MetallicSmoothness") })
            {
                var tex = m.GetTexture(prop); Assert.NotNull(tex, prop);
                Assert.AreEqual(file, tex.name, prop);
                var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter; Assert.NotNull(imp, file + " importer");
                Assert.LessOrEqual(imp.maxTextureSize, 1024, file + " capped at 1024 (ThirdPartyImport)");
            }
            var root = new GameObject("barrel").transform;
            try
            {
                var go = ZoneBuilder.ModelProp(root, ZoneBuilder.WoodBarrelPath, Vector3.zero, 0, 1);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) foreach (var mm in r.sharedMaterials) Assert.AreSame(m, mm, "a placed barrel wears WoodBarrel.mat");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test] public void The_Spoons_pit_kegs_keep_their_seat_height()
        {
            Assert.AreEqual(.44f, ZoneBuilder.PitKegTop, 1e-4f, "the dice pit's kegs stand .44 m, as the painted ones did");
            // The barrel fitted as a pit keg: its top at the seat's height, its foot on the floor, .42 across.
            var mesh = ZoneBuilder.WoodBarrelMesh; Assert.NotNull(mesh);
            var fit = ZoneBuilder.WoodBarrelFit(mesh, new Vector3(3, 0, -1), Quaternion.Euler(0, 40, 0), ZoneBuilder.PitKegTop, .42f);
            var mb = mesh.bounds; float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = fit.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y);
            }
            Assert.AreEqual(0, lo, .005f, "the keg's foot on the floor");
            Assert.AreEqual(ZoneBuilder.PitKegTop, hi, .005f, "the keg's top at the seat's height");
            // The Spoon stands the kegs at that height and marks the seats on their tops.
            var spoon = File.ReadAllText(Path.Combine(Crulanda, "Scripts", "World", "ZoneBuilder.Spoon.cs"));
            StringAssert.Contains("Keg(new Vector3(s.x, 0, s.y), PitKegTop, .42f", spoon, "the pit's kegs are the wood barrel at PitKegTop");
            StringAssert.Contains("SeatMark(t, s, PitKegTop);", spoon, "the seat marks on the kegs' tops");
        }
    }
}
