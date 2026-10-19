using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Fantasy Props MegaKit's pieces and the food models laid out (playtest notes 234 and 235, 2026-10-18): the dungeons and rooms from
    /// <see cref="PropDressing"/>, each zone's fare on its inn tables, bars, kitchens and stalls from <see cref="FoodModels.Fare"/>. None of
    /// it draws from the zone's stream (where a choice is made, it is by the table's number), so nothing placed after it moves; the small
    /// pieces cast no shadow and are not drawn far off (DetailCull); the big ones are solid where they stand, by the wall, and the nodes keep
    /// clear of every floor piece (KeepClear). GAME-ONLY: the arrangement is the game's.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The inns' table tops (world), where a dish bought from the innkeeper is set down (EncounterSession.ServeDish).</summary>
        public readonly List<Vector3> TableTops = new List<Vector3>();
        /// <summary>A small piece: no shadow, and drawn only near.</summary>
        void SmallPiece(GameObject go)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; CullDetail(r, 0); }
        }
        /// <summary>
        /// A piece hung on a wall (a wall lantern, a peg rack): placed, then turned so the side its pivot is on (the kit's wall pieces have
        /// their pivot at the plate that meets the wall) faces the wall, the pivot on <paramref name="wall"/>, its foot <paramref name="up"/>
        /// over it; <paramref name="inward"/> points from the wall into the room. A piece pivoted in its middle faces along inward.
        /// </summary>
        GameObject WallHang(Transform parent, string path, Vector3 wall, Vector3 inward, float height, float up)
        {
            var hang = new GameObject("Hung " + path.Substring(path.LastIndexOf('/') + 1)).transform; hang.SetParent(parent, false);
            var go = ModelProp(hang, path, Vector3.zero, 0, height); if (go == null) { DestroyImmediate(hang.gameObject); return null; }
            var lp = go.transform.localPosition; var pivot = new Vector3(lp.x, 0, lp.z);
            go.transform.localPosition = new Vector3(0, lp.y, 0);   // its pivot over the holder's foot
            inward.y = 0; if (inward.sqrMagnitude < 1e-4f) inward = Vector3.forward; inward.Normalize();
            float want = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            float body = pivot.sqrMagnitude > 1e-4f ? Mathf.Atan2(-pivot.x, -pivot.z) * Mathf.Rad2Deg : 0;   // from the pivot to the body
            hang.localPosition = wall + Vector3.up * up; hang.localRotation = Quaternion.Euler(0, want - body, 0);
            return hang.gameObject;
        }
        /// <summary>The bounds of a placed piece in <paramref name="t"/>'s space.</summary>
        static Bounds BoundsIn(Transform t, GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); var b = new Bounds(); bool any = false;
            foreach (var r in rs)
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = t.InverseTransformPoint(wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>
        /// A cave's kit dressing (<see cref="PropDressing.Caves"/> under <paramref name="key"/>), laid along its passage: floor pieces at
        /// their share of the half-width off the middle, wall pieces on the rock at their height, the big ones solid (Block). anchor gives
        /// a named chamber's place along the passage (NaN for one this cave has not); free, when given, says a spot may take a piece (the
        /// Adit's: not in another cave's air, a deck's way or the water).
        /// </summary>
        void DressCave(Transform t, Hollow h, string key, Vector3[] c, Vector3[,] ring, int n, int P, Func<float, int> RingAt, Func<float, float, Vector3> On,
            Func<float, Quaternion> Along, Func<string, float> anchor, Func<Vector3, bool> free, Action<Vector3, Vector3, Quaternion> block)
        {
            if (string.IsNullOrEmpty(key) || !PropDressing.Caves.TryGetValue(key, out var set) || h.Length < 4) return;
            var root = new GameObject("Kit dressing").transform; root.SetParent(t, false);
            foreach (var p in set)
            {
                float s = string.IsNullOrEmpty(p.anchor) ? p.z * h.Length : (anchor != null ? anchor(p.anchor) : float.NaN) + p.z;
                if (float.IsNaN(s)) continue;
                s = Mathf.Clamp(s, 1.5f, h.Length - 1.5f);
                int i = RingAt(s); var q = Along(s);
                if (p.place == PropDressing.Place.Wall)
                {
                    int side = p.x >= 0 ? 1 : -1, best = side > 0 ? 1 : P - 2;
                    for (int k = 1; k < P - 1; k++)
                        if ((side > 0 ? k <= P / 2 : k >= P / 2) && Mathf.Abs(ring[i, k].y - c[i].y - p.y) < Mathf.Abs(ring[i, best].y - c[i].y - p.y)) best = k;
                    // Hung flush on the rock the passage actually has there: a ray from the middle of the passage at the piece's height out to the
                    // wall (the shell's collider, this cave's own), the piece's back on the hit, facing back along the ray. No hit near the wall
                    // (a mouth into another cave, a cut face): nothing hung (frames 2026-10-18: nothing left floating off the rock).
                    var across = ring[i, best] - c[i]; across.y = 0; if (across.sqrMagnitude < .01f) continue; across.Normalize();
                    var from = t.TransformPoint(c[i] + Vector3.up * p.y); var dir = t.TransformDirection(across).normalized;
                    Physics.SyncTransforms();
                    RaycastHit wallHit = default; bool hit = false; float reach = h.Half[i] * 1.35f * Mathf.Max(.01f, t.lossyScale.x);
                    foreach (var rh in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
                        if (rh.collider is MeshCollider && rh.collider.transform.IsChildOf(t) && rh.collider.transform.parent == t && (!hit || rh.distance < wallHit.distance)) { wallHit = rh; hit = true; }
                    if (!hit || wallHit.distance < h.Half[i] * .6f) continue;
                    var inward = t.InverseTransformDirection(-dir); inward.y = 0; inward.Normalize();
                    var foot = t.InverseTransformPoint(wallHit.point) + inward * .04f; foot.y = c[i].y;
                    if (free != null && !free(foot + inward * .4f)) continue;
                    SmallPiece(WallHang(root, p.path, foot, inward, p.height, p.y));
                    continue;
                }
                var at = On(s, p.x * h.Half[i]) + Vector3.up * p.y;
                if (free != null && !free(at)) continue;
                if (p.solid && h.Half[i] < 2.2f) continue;   // a narrow drift takes nothing solid
                var go = ModelProp(root, p.path, at, q.eulerAngles.y + p.yaw, p.height);
                if (go == null) continue;
                var b = BoundsIn(t, go);
                // Its inner edge leaves the middle of the floor clear (a metre either side of the line, or most of a narrow way): else not placed.
                if (Mathf.Abs(p.x * h.Half[i]) - Mathf.Max(b.size.x, b.size.z) / 2 < Mathf.Min(.9f, h.Half[i] * .45f)) { DestroyImmediate(go); continue; }
                KeepClear(t, at, Mathf.Max(.35f, Mathf.Max(b.size.x, b.size.z) * .6f));
                if (p.solid && block != null)
                {   // its own box, turned with it (not the box round it, which reaches further into the way)
                    var own = PropBounds(go.transform); var k = go.transform.localScale;
                    block(at, new Vector3(own.size.x * k.x * .9f, own.size.y * k.y, own.size.z * k.z * .9f), go.transform.localRotation);
                }
                if (b.size.y < .5f) SmallPiece(go);
            }
        }

        /// <summary>A room's kit dressing (<see cref="PropDressing.Rooms"/>), in <paramref name="t"/>'s frame; top pieces on what is there.</summary>
        void DressRoom(Transform t, string key, float floor = 0)
        {
            if (!PropDressing.Rooms.TryGetValue(key, out var room)) return;
            var root = new GameObject("Kit dressing").transform; root.SetParent(t, false);
            foreach (var p in room.pieces)
            {
                var at = new Vector3(p.x, floor + p.y, p.z);
                var go = ModelProp(root, p.path, at, p.yaw, p.height, p.solid && p.place == PropDressing.Place.Floor);
                if (go == null) continue;
                if (p.place == PropDressing.Place.Floor) KeepClear(t, at, .5f);
                if (p.height < .5f) SmallPiece(go);
            }
        }

        /// <summary>
        /// A table laid with the zone's fare (<see cref="FoodModels.Fare"/>): a plate with one of its foods on it, and on every other table
        /// a second thing beside (a loaf, a pot, a bottle), picked by the table's number, not the zone's stream. <paramref name="top"/> is the
        /// table top's middle in <paramref name="t"/>'s frame; the top goes on the list a bought dish is set down on.
        /// </summary>
        void TableFare(Transform t, Vector3 top, float yaw, int n, float reach = .5f)
        {
            TableTops.Add(t.TransformPoint(top));
            var fare = FoodModels.FareOf(Zone != null ? Zone.id : null); if (fare.Length == 0) return;
            var q = Quaternion.Euler(0, yaw, 0);
            var dish = fare[n % fare.Length]; var plate = new FoodModels.Bit(Zone.id == "zone.crown" ? "Food/Plate2" : n % 2 == 0 ? "Food/Plate" : "Food/Plate_Square", n % 2 == 0 ? .035f : .021f);
            bool big = dish.height > .14f;   // a pot or a bottle stands on the board, not on a plate
            var spot = top + q * new Vector3(-reach * .4f, 0, -.05f);
            var laid = FoodModels.Lay(t, spot, yaw + n * 37, big ? new[] { dish } : new[] { plate, new FoodModels.Bit(dish.path, dish.height, 0, 0, 20, .018f, dish.width) }, "fare");
            SmallPiece(laid);
            if (n % 2 == 1)
            {
                var side = fare[(n * 3 + 1) % fare.Length];
                SmallPiece(FoodModels.Lay(t, top + q * new Vector3(reach * .55f, 0, .12f), yaw - n * 23, new[] { side }, "fare"));
            }
        }
        /// <summary>A bar or a work table laid along its length with the zone's fare: <paramref name="count"/> things from
        /// <paramref name="from"/> to <paramref name="to"/> (the top's height in both), each its own.</summary>
        void CounterFare(Transform t, Vector3 from, Vector3 to, int count, int offset = 0)
        {
            var fare = FoodModels.FareOf(Zone != null ? Zone.id : null); if (fare.Length == 0 || count < 1) return;
            var run = to - from; float yaw = Mathf.Atan2(run.x, run.z) * Mathf.Rad2Deg;
            for (int k = 0; k < count; k++)
            {
                var bit = fare[(k + offset) % fare.Length];
                SmallPiece(FoodModels.Lay(t, Vector3.Lerp(from, to, count == 1 ? .5f : k / (float)(count - 1)), yaw + k * 53, new[] { bit }, "fare"));
            }
        }
        /// <summary>A stall's basket heaped with the zone's produce, or its counter with its bread (FoodModels.Produce, Baked); false
        /// when the zone has none of its own, and the stall keeps its painted goods.</summary>
        bool StallFood(Transform t, Vector3 at, int i, bool baked)
        {
            var table = baked ? FoodModels.Baked : FoodModels.Produce;
            if (Zone == null || !table.TryGetValue(Zone.id, out var names) || names.Length == 0) return false;
            string name = names[i % names.Length]; float h = FoodModels.LifeHeight(name); if (h <= 0 || !HasProp("Food/" + name)) return false;
            if (baked) { SmallPiece(ModelProp(t, "Food/" + name, at, i * 41, h)); return true; }
            for (int k = 0; k < 3; k++) SmallPiece(ModelProp(t, "Food/" + name, at + new Vector3((k - 1) * .08f, .1f + (k % 2) * .03f, (k % 2) * .07f - .035f), k * 70 + i * 13, h));
            return true;
        }
    }
}
