using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The open ground dressed (playtest note 84, 2026-10-07: "still lots of empty space", a screenshot of the meadow between the
    /// village and the woods): the nature kit scattered over the green zones' open grass in clusters on an 18 m grid, each cell
    /// jittered: thickets (two to four bushes, often round a young tree), lone trees, mossy rocks with pebbles, flower patches and ferns,
    /// stumps and fallen logs. Only where the grass grows open (Openness: no road, water, field, clearing, building or grey there),
    /// out of the village's heart, clear of camps, nodes, secrets, props and trunks, on gentle ground. From its own stream, after the
    /// forest edge: nothing else in the zone moves. Trees and rocks are solid; bushes and flowers are walked through.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        void BuildMeadow()
        {
            if (Gloom || Zone.biome == "ash" || !KitTrees) return;
            bool mountain = Zone.biome == "mountain";
            var mr = new System.Random(Zone.seed * 31 + 8423); float M() { return (float)mr.NextDouble(); }
            var root = new GameObject("Meadow").transform; root.SetParent(statics, false);
            const float cell = 18; int placed = 0;
            for (float cx = -Half + 14; cx < Half - 14; cx += cell)
                for (float cz = -Half + 14; cz < Half - 14; cz += cell)
                {
                    var at = new Vector2(cx + (M() - .5f) * cell * .8f, cz + (M() - .5f) * cell * .8f); float roll = M(), turn = M() * 360, pick = M();
                    if (at.magnitude < Zone.flatRadius * .5f) continue;   // the village's heart stays as it is laid out
                    if (Openness(at) < .6f || NearRoad(at, 4) || NearProp(at, 4) || !TrunkClear(at, 4) || InBuilding(at) || Water.NearWater(at, 3)) continue;
                    if (mountain ? Steep(at, 2) > .5f : Steep(at, 2) > .35f) continue;
                    if (Busy(at)) continue;
                    if (mountain)
                    {
                        // The heights: a stone and its pebbles now and then, a lone pine more rarely.
                        if (roll < .16f) { MeadowRocks(root, at, turn, pick); placed++; }
                        else if (roll < .24f) { MeadowTree(root, at, turn, pick, true); placed++; }
                        continue;
                    }
                    if (roll < .3f) { MeadowThicket(root, at, turn, pick, M); placed++; }
                    else if (roll < .42f) { MeadowTree(root, at, turn, pick, Zone.biome != "verdant" && pick < .3f); placed++; }
                    else if (roll < .52f) { MeadowRocks(root, at, turn, pick); placed++; }
                    else if (roll < .72f) { MeadowFlowers(root, at, turn, M); placed++; }
                    else if (roll < .78f) { MeadowStump(root, at, turn); placed++; }
                    else if (roll < .83f) { MeadowLog(root, at, turn, pick); placed++; }
                }
            Debug.Log("Meadow: " + placed + " clusters in " + Zone.id + ".");
        }
        /// <summary>Every tilled field's crop (playtest note 76): CropField, growing through the days. Not static: it grows.</summary>
        void BuildCrops()
        {
            if (Zone.fields == null) return; var root = new GameObject("Crops").transform; root.SetParent(transform, false); int n = 0;
            for (int i = 0; i < Zone.fields.Length; i++) { var f = Zone.fields[i]; if (f != null && f.crop == "soil" && CropField.Build(this, f, i, root) != null) n++; }
            if (n > 0) Debug.Log("Crops: " + n + " fields in " + Zone.id + ".");
        }
        /// <summary>A camp, a node, a secret or the spawn too near to dress over.</summary>
        bool Busy(Vector2 at)
        {
            foreach (var c in Zone.camps) if (c != null && Vector2.Distance(at, c.center) < c.radius + 8) return true;
            // (Not the gathering nodes: they are built after and must never move scenery, NodeStreamTests. They stand among the meadow.)
            foreach (var s in Zone.secrets) if (s != null && Vector2.Distance(at, s.at) < s.radius + 5) return true;
            foreach (var e in Zone.exits) if (e != null && Vector2.Distance(at, e.at) < 12) return true;
            foreach (var c in Zone.clearings) if (c != null && Vector2.Distance(at, c.center) < c.radius + 4) return true;
            foreach (var l in Zone.landmarks) if (l != null && Vector2.Distance(at, l.at) < 6) return true;
            return false;
        }
        Transform MeadowRoot(Transform parent, Vector2 at, float turn, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.position = Ground(at); t.rotation = Quaternion.Euler(0, turn, 0); return t;
        }
        void MeadowThicket(Transform parent, Vector2 at, float turn, float pick, System.Func<float> M)
        {
            var t = MeadowRoot(parent, at, turn, "Thicket");
            int bushes = 2 + (int)(M() * 3);
            for (int i = 0; i < bushes; i++)
            {
                float a = i * 2.4f + M(), r = 1.2f + M() * 1.6f; var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                var b = GroundProp(t, M() < .3f ? "Nature/Bush_Common_Flowers" : "Nature/Bush_Common", p, M() * 360, 1 + M() * .7f);
                if (b != null) DressNature(b, Wither(KitBush * (.88f + M() * .24f)), "*", LeafMask);
            }
            if (pick < .45f && TrunkClear(at, 6))
            {
                var tree = new GameObject("Thicket tree").transform; tree.SetParent(t, false); tree.position = Ground(at); tree.rotation = Quaternion.Euler(0, turn, 0);
                var kit = KitTree(tree, "Nature/CommonTree_" + (1 + (int)(pick * 11) % 5), 4.5f + M() * 1.5f, .22f, M() * 360);
                if (kit != null) { DressNature(kit, Wither(KitLeaf[(int)(pick * 7) % 2]), "*", LeafMask); trunks.Add(at); }
            }
        }
        void MeadowTree(Transform parent, Vector2 at, float turn, float pick, bool pine)
        {
            if (!TrunkClear(at, 7)) return;
            var t = MeadowRoot(parent, at, turn, pine ? "Lone pine" : "Lone tree");
            GameObject kit;
            if (pine) kit = KitTree(t, "Nature/Pine_" + (1 + (int)(pick * 13) % 5), 8 + pick * 4, .3f, turn);
            else if (pick > .8f && HasProp("Nature/TwistedTree_1")) { kit = KitTree(t, "Nature/TwistedTree_" + (1 + (int)(pick * 17) % 5), 7 + pick, .3f, turn); if (kit != null) DressNature(kit, Autumn[(int)(pick * 19) % Autumn.Length], "Leaves_TwistedTree", AutumnMask); }
            else { kit = KitTree(t, "Nature/CommonTree_" + (1 + (int)(pick * 13) % 5), 6 + pick * 2.5f, .3f, turn); if (kit != null) DressNature(kit, Wither(KitLeaf[new[] { 0, 1, 3 }[(int)(pick * 9) % 3]]), "*", LeafMask); }
            if (kit != null) { LeafTrees.Add(t.position); trunks.Add(at); }
        }
        void MeadowRocks(Transform parent, Vector2 at, float turn, float pick)
        {
            var t = MeadowRoot(parent, at, turn, "Meadow rocks");
            float s = .7f + pick * .9f;
            var rock = KitCrags ? KitCrag(t, (int)(pick * 7), 1.4f * s, turn, .12f * s) : KitRock(t, (int)(pick * 7), s, 1.9f * s, turn);
            if (rock != null) Solid(t, new Vector3(0, .45f * s, 0), new Vector3(1.4f * s, .9f * s, 1.2f * s));
            for (int i = 0; i < 3; i++)
            {
                float a = turn * Mathf.Deg2Rad + i * 2.1f, r = 1.4f * s + i * .3f;
                GroundProp(t, "Nature/Pebble_Round_" + (1 + (i + (int)(pick * 5)) % 5), new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), i * 77, .18f + i * .05f);
            }
        }
        void MeadowFlowers(Transform parent, Vector2 at, float turn, System.Func<float> M)
        {
            var t = MeadowRoot(parent, at, turn, "Flowers");
            string[] kinds = { "Nature/Flower_3_Group", "Nature/Flower_4_Group", "Nature/Fern_1", "Nature/Plant_1", "Nature/Clover_1", "Nature/Plant_7" };
            int n = 4 + (int)(M() * 5);
            for (int i = 0; i < n; i++)
            {
                float a = M() * 6.28f, r = M() * 3.2f; var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                var k = kinds[(int)(M() * kinds.Length) % kinds.Length]; var f = GroundProp(t, k, p, M() * 360, k.Contains("Fern") || k.Contains("Plant") ? .6f + M() * .4f : .35f + M() * .2f);
                if (f != null) DressNature(f);
            }
        }
        void MeadowStump(Transform parent, Vector2 at, float turn)
        {
            var t = MeadowRoot(parent, at, turn, "Stump");
            if (GroundProp(t, "Megapack/Models/Plants/TreeTrunk_1", Vector3.down * .05f, 0, .7f) != null) Solid(t, new Vector3(0, .35f, 0), new Vector3(.8f, .7f, .8f));
        }
        void MeadowLog(Transform parent, Vector2 at, float turn, float pick)
        {
            var t = MeadowRoot(parent, at, turn, "Fallen log");
            bool big = pick > .5f;
            var log = big ? GroundProp(t, "Megapack/Models/Tree_Broken_1", Vector3.down * .15f, 0, 1.2f) : GroundProp(t, "Megapack/Models/WodenLog_Cuted", Vector3.down * .05f, 0, .55f);   // the root carries the turn, so the collider lies along the log
            if (log != null) Solid(t, new Vector3(0, big ? .5f : .3f, 0), big ? new Vector3(4.6f, 1, 1.4f) : new Vector3(1.8f, .6f, .6f));
        }
    }
}
