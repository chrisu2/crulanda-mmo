using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The alchemy trade's look (play note 253, 2026-10-19, Chris: "assets for alchemy - myassets Free Alchemy and Magic Pack"):
    /// StormBringer Studios' Free Alchemy and Magic Pack (Asset Store EULA; Resources/Props/Alchemy, the pack's own files and GUIDs),
    /// three models: a stone mortar with its pestle, a round glass flask with its stopper, and a crystal flask on a brass stand. Their
    /// materials are the pack's, Built-in Standard and Standard (Specular setup); the glass is the pack's own clear glass, too clear to read
    /// as a potion, so a placed flask's glass takes PotionGlass (the same shader and normal map, a pale green tint at half alpha).
    /// Where they stand: each zone's herbalist's bench (<see cref="HerbBench"/>, its own arrangement by zone: <see cref="BenchSets"/>),
    /// Lisbet's drying hut in Oakhaven, the Crown's Court Still-room (its still-table and its counter), and as the look of the
    /// cordials and the draughts (FoodModels.Dishes). GAME-ONLY: where they stand is the game's.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        public const string AlchemyMortar = "Alchemy/Prefabs/Mortar", AlchemyFlask = "Alchemy/Prefabs/Flask", AlchemyCrystal = "Alchemy/Prefabs/Cristal_Flask";
        /// <summary>Their heights at life size (the files are in centimetres): the mortar to the pestle's top, the flask to its stopper,
        /// the crystal flask on its stand.</summary>
        public const float MortarHeight = .2f, FlaskHeight = .16f, CrystalHeight = .29f;
        static Material potionGlass;
        public static Material PotionGlass { get { if (potionGlass == null) potionGlass = Resources.Load<Material>("Props/Alchemy/PotionGlass"); return potionGlass; } }
        /// <summary>The pack's glass on a placed model swapped for <see cref="PotionGlass"/>; its stone, brass and crystal kept.</summary>
        static void DressAlchemy(GameObject go)
        {
            if (PotionGlass == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Glass")) mats[i] = PotionGlass;
                r.sharedMaterials = mats;
            }
        }
        /// <summary>One piece on a bench: which model, where on the bench top (x along it, z across: - the front), its turn.</summary>
        public struct BenchPiece
        {
            public string path; public float x, z, yaw;
            public BenchPiece(string path, float x, float z, float yaw = 0) { this.path = path; this.x = x; this.z = z; this.yaw = yaw; }
            public float Height { get { return path == AlchemyMortar ? MortarHeight : path == AlchemyCrystal ? CrystalHeight : FlaskHeight; } }
        }
        static BenchPiece Mo(float x, float z, float yaw = 0) { return new BenchPiece(AlchemyMortar, x, z, yaw); }
        static BenchPiece Fl(float x, float z, float yaw = 0) { return new BenchPiece(AlchemyFlask, x, z, yaw); }
        static BenchPiece Cr(float x, float z, float yaw = 0) { return new BenchPiece(AlchemyCrystal, x, z, yaw); }
        /// <summary>
        /// Each zone's herbalist's bench, its own way (Chris: every zone unique). The bench is 1.6 by .55; its jars stand at x -.2, .02, .24
        /// toward the back, the cut bunch at the front left of the middle, the Ash Rim's salt blocks at x .52 and .66 at the back.
        /// Mother Vane's salt-bench (the Ash Rim): the mortar for the salt, two flasks of brine at the front right. Wenna Coyle's (Khaven):
        /// the mortar at the back right, three flasks along the front. The Moss-Lantern's (the Verdant): the crystal flask on its stand at
        /// the right, the mortar at the left. The dyer's vats (the Lowtowns): the mortar for the madder, one flask of mordant. Any other: a
        /// mortar and a flask.
        /// </summary>
        public static readonly Dictionary<string, BenchPiece[]> BenchSets = new Dictionary<string, BenchPiece[]> {
            { "zone.ashrim", new[] { Mo(-.55f, .02f, 30), Fl(.4f, -.15f), Fl(.6f, -.16f, 50) } },
            { "zone.khaven", new[] { Mo(.6f, .1f, -20), Fl(-.55f, -.02f), Fl(.42f, -.15f, 30), Fl(.62f, -.14f, 90) } },
            { "zone.verdant", new[] { Mo(-.55f, .02f), Cr(.55f, .02f, 15) } },
            { "zone.lowtowns", new[] { Mo(-.5f, .05f, 60), Fl(.55f, -.12f) } },
            { "", new[] { Mo(-.55f, .02f), Fl(.55f, -.14f) } },
        };
        public static BenchPiece[] BenchSetOf(string zone) { return zone != null && BenchSets.TryGetValue(zone, out var s) ? s : BenchSets[""]; }
        /// <summary>A bench's set on its top (<paramref name="top"/>: the top's middle in <paramref name="t"/>'s frame). False when the
        /// pack is not there (the bare test scenes: the bench draws its painted mortar and vials).</summary>
        bool AlchemyOnBench(Transform t, Vector3 top, BenchPiece[] set)
        {
            if (!HasProp(AlchemyMortar)) return false;
            foreach (var p in set) ModelProp(t, p.path, top + new Vector3(p.x, 0, p.z), p.yaw, p.Height);
            return true;
        }
    }
}
