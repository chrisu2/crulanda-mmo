using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The Fantasy pack's market pieces (Resources/Props/Fantasy2, 2026-10-15): each loads and stands upright at its metre
    /// height with its foot on its root, and Oakhaven's "market" props name pieces of the table (ZoneBuilder.Market).</summary>
    public class MarketPropTests
    {
        [Test] public void Every_market_piece_loads_and_stands_on_its_foot()
        {
            var root = new GameObject("market").transform; var wrong = new StringBuilder(); int n = 0;
            foreach (var (name, path, height, solid) in ZoneBuilder.MarketPieces)
            {
                var holder = new GameObject(name).transform; holder.SetParent(root, false);
                var go = ZoneBuilder.ModelProp(holder, path, Vector3.zero, 0, height, solid);
                if (go == null) { wrong.Append(path + " missing; "); continue; }
                var b = ZoneBuilder.PropBounds(holder); n++;
                if (Mathf.Abs(b.size.y - height) > height * .05f + .02f) wrong.Append(path + " stands " + b.size.y.ToString("0.00") + " m, not " + height + "; ");
                if (Mathf.Abs(b.min.y) > .03f) wrong.Append(path + " foot at " + b.min.y.ToString("0.00") + "; ");
                if (b.size.x > 4 || b.size.z > 4) wrong.Append(path + " is " + b.size.x.ToString("0.0") + " x " + b.size.z.ToString("0.0") + " across: on its side?; ");
                if (solid != (go.GetComponent<BoxCollider>() != null)) wrong.Append(path + (solid ? " has no collider; " : " has a collider; "));
            }
            Object.DestroyImmediate(root.gameObject);
            Assert.AreEqual(ZoneBuilder.MarketPieces.Length, n, "pieces loaded");
            Assert.AreEqual("", wrong.ToString(), "wrong: ");
        }

        [Test] public void Oakhavens_market_props_name_pieces_of_the_table()
        {
            var zone = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/oakhaven.json")));
            int n = 0;
            foreach (var p in zone.props)
            {
                if (p == null || p.kind != "market") continue; n++;
                Assert.That(p.variant, Is.InRange(0, ZoneBuilder.MarketPieces.Length - 1), p.name + " names a piece");
                Assert.That(p.lift, Is.InRange(0f, 1.2f), p.name + " is lifted a table's height at most");
                Assert.That(p.scale, Is.InRange(.5f, 2f), p.name + " is near the pack's size");
            }
            Assert.That(n, Is.InRange(10, 16), "market pieces in Oakhaven");
        }
    }
}
