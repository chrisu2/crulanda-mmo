using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A house assembled from the Medieval Village MegaKit (Quaternius, CC0; Resources/Props/Village), 2026-10-07. The kit's walls are
    /// 2 m wide and 3 m high on a 2 m grid, its floors 2 x 2 m, its roofs named by their span in metres (Roof_RoundTiles_6x8).
    /// <see cref="House"/> lays out a footprint of whole 2 m bays, its front (the door) toward -Z like every builder here, one or two
    /// storeys: brick or plaster below, timber-framed plaster above, corner posts, a dark wood floor, a tiled gable roof and a chimney.
    /// For now it is used by Editor/VillageCapture to show Chris a kit house beside the painted ones; nothing in a zone uses it yet.
    /// </summary>
    public static class VillageKit
    {
        public const float Bay = 2, Storey = 3;
        public struct Style
        {
            public string lower, upper, door, window, corner, floor, roof;
            public static Style Oakhaven => new Style { lower = "Wall_UnevenBrick", upper = "Wall_Plaster", door = "Door_Round", window = "Window_Wide_Round", corner = "Corner_Exterior_Wood", floor = "Floor_WoodDark", roof = "Roof_RoundTiles" };
        }
        /// <summary>The kit's FBX are Z-up (they came in lying on their backs, 2026-10-07): each piece is stood up inside a holder at its
        /// place and turn.</summary>
        public static Vector3 Upright = new Vector3(-90, 0, 0);
        static GameObject Piece(Transform parent, string name, Vector3 at, float yaw)
        {
            var src = ZoneBuilder.PropSource("Village/" + name); if (src == null) { Debug.LogWarning("VILLAGE_KIT missing " + name); return null; }
            var holder = new GameObject(name).transform; holder.SetParent(parent, false); holder.localPosition = at; holder.localRotation = Quaternion.Euler(0, yaw, 0);
            var go = Object.Instantiate(src, holder, false); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.Euler(Upright);
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c); }
            return holder.gameObject;
        }
        /// <summary>A piece's bounds in its holder's space, for checking the kit's axes (VillageCapture logs them).</summary>
        public static Bounds PieceBounds(Transform parent, string name)
        {
            var p = Piece(parent, name, Vector3.zero, 0); if (p == null) return new Bounds();
            var b = new Bounds(); bool any = false;
            foreach (var r in p.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            Object.DestroyImmediate(p); return b;
        }
        /// <summary>
        /// A house <paramref name="bays"/> x <paramref name="depth"/> bays (2 m each), <paramref name="storeys"/> high, its door in bay
        /// <paramref name="doorBay"/> of the front (-Z). <paramref name="faceOut"/> turns the walls round if the kit's outer face is the
        /// other side (the capture shows which). Returns the house's root.
        /// </summary>
        public static Transform House(Transform parent, int bays, int depth, int storeys, int doorBay, Style style, bool faceOut = true)
        {
            var root = new GameObject("Village house").transform; root.SetParent(parent, false);
            float w = bays * Bay, d = depth * Bay; float flip = faceOut ? 0 : 180;
            for (int s = 0; s < storeys; s++)
            {
                float y = s * Storey; string wall = s == 0 ? style.lower : style.upper;
                // Front (-Z) and back (+Z): a door in the front's door bay on the ground, windows in every other bay but the ends.
                for (int i = 0; i < bays; i++)
                {
                    float x = -w / 2 + Bay * (i + .5f);
                    string front = s == 0 && i == doorBay ? wall + "_" + style.door : (i > 0 && i < bays - 1) || s > 0 ? wall + "_" + style.window : wall + "_Straight";
                    if (s > 0 && style.upper == "Wall_Plaster" && (i == 0 || i == bays - 1)) front = "Wall_Plaster_WoodGrid";
                    Piece(root, front, new Vector3(x, y, -d / 2), 180 + flip);
                    Piece(root, i % 2 == 1 && s > 0 ? wall + "_" + style.window : (s > 0 && style.upper == "Wall_Plaster" ? "Wall_Plaster_WoodGrid" : wall + "_Straight"), new Vector3(x, y, d / 2), 0 + flip);
                }
                // The ends (-X and +X): a window in the middle bay upstairs.
                for (int j = 0; j < depth; j++)
                {
                    float z = -d / 2 + Bay * (j + .5f); string end = s > 0 && j == depth / 2 ? wall + "_" + style.window : wall + "_Straight";
                    Piece(root, end, new Vector3(-w / 2, y, z), -90 + flip);
                    Piece(root, end, new Vector3(w / 2, y, z), 90 + flip);
                }
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Piece(root, style.corner, new Vector3(sx * w / 2, y, sz * d / 2), 0);
                for (int i = 0; i < bays; i++) for (int j = 0; j < depth; j++) Piece(root, style.floor, new Vector3(-w / 2 + Bay * (i + .5f), y + .01f, -d / 2 + Bay * (j + .5f)), 0);
            }
            // The roof: the kit's tiled gable for the nearest span (its ridge along the house's length), over the top storey.
            int spanW = Mathf.Clamp(depth * 2, 4, 8), spanL = Mathf.Clamp(bays * 2, 4, 14); if (spanL % 2 == 1) spanL++;
            var roof = Piece(root, style.roof + "_" + spanW + "x" + spanL, new Vector3(0, storeys * Storey, 0), 90);
            if (roof == null) Piece(root, style.roof + "_6x8", new Vector3(0, storeys * Storey, 0), 90);
            // The gable ends: the kit's brick gable for the span, filling the triangle under the roof at each end.
            foreach (int sx in new[] { -1, 1 }) Piece(root, "Roof_Front_Brick" + spanW, new Vector3(sx * w / 2, storeys * Storey, 0), sx < 0 ? -90 : 90);
            Piece(root, "Prop_Chimney", new Vector3(w / 2 - 1, storeys * Storey + 1.4f, d / 4), 0);
            return root;
        }
    }
}
