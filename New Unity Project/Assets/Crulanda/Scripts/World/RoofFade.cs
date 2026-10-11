using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A building that comes between the camera and the player turns see-through where it does, as a tree does (TreeFade, whose fade
    /// materials it shares): a part (a roof, a wall, a chimney) that the line from the camera to the player's head or body passes
    /// through fades, with the small pieces on it (a window frame, a beam); the rest stays solid, so the far walls of a room you stand
    /// in still make a room (playtest note 26 for the roofs; note 120, Chris, 2026-10-11: "GOING IN A BUILDING MAKES IT DISAPPEAR").
    /// The parts are boxed in the building's own frame: in the world's frame a wall of a building set at an angle (the Golden Cask, 41
    /// degrees off) boxed most of the room, so a camera anywhere inside counted as inside a wall and the whole building faded.
    /// Only buildings carry it (ZoneBuilder.BuildProps: anything with a footprint); trees keep TreeFade.
    /// </summary>
    public sealed class RoofFade : MonoBehaviour
    {
        public static readonly List<RoofFade> All = new List<RoofFade>();
        /// <summary>How much of a faded part still shows.</summary>
        public const float FadedAlpha = .18f;
        /// <summary>Half the building across its walls, along its own x and z (ZoneBuilder: the footprint less the eaves); zero if not known.</summary>
        public Vector2 Half;
        /// <summary>Walled all round, so the camera stays within the walls while you are inside (not an open forge or shelter).</summary>
        public bool Walled = true;
        static MaterialPropertyBlock block;

        Renderer[] parts; Material[][] solid, fadedMats; Bounds bounds; Bounds[] partBounds; bool[] big; int[] owner; float[] alpha, target; bool ready;
        /// <summary>Any part of it is faded now.</summary>
        public bool Faded { get { if (alpha != null) foreach (var a in alpha) if (a < .999f) return true; return false; } }
        /// <summary>The big parts (walls, roofs, chimneys), and how many of them are faded now (for the tests).</summary>
        public int BigParts { get { if (!ready) Prepare(); int n = 0; foreach (var b in big) if (b) n++; return n; } }
        public int FadedBigParts { get { if (!ready) Prepare(); int n = 0; for (int i = 0; i < parts.Length; i++) if (big[i] && alpha[i] < .999f) n++; return n; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); Restore(); }

        void Prepare()
        {
            ready = true;
            var list = new List<Renderer>(); foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer)) list.Add(r);   // the chimney's smoke neither hides nor fades
            parts = list.ToArray(); int n = parts.Length;
            solid = new Material[n][]; fadedMats = new Material[n][]; partBounds = new Bounds[n]; big = new bool[n]; owner = new int[n]; alpha = new float[n]; target = new float[n];
            bounds = new Bounds(Vector3.zero, Vector3.zero); bool any = false;
            for (int i = 0; i < n; i++)
            {
                solid[i] = parts[i].sharedMaterials; alpha[i] = target[i] = 1; owner[i] = -1;
                partBounds[i] = LocalBounds(parts[i]);
                if (!any) { bounds = partBounds[i]; any = true; } else bounds.Encapsulate(partBounds[i]);
                // Only the big parts (roofs, walls, chimneys) count as blocking; a table or a lamp between the camera and the player does not.
                var s = partBounds[i].size; big[i] = Mathf.Max(s.x, Mathf.Max(s.y, s.z)) >= 1.8f;
            }
            // A small piece fades with the big part it sits on (a window frame with its wall); one on none (a table) never fades.
            for (int i = 0; i < n; i++)
            {
                if (big[i]) continue; float best = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    if (!big[j]) continue; var box = partBounds[j]; box.Expand(.3f);
                    if (!box.Contains(partBounds[i].center)) continue;
                    float v = box.size.x * box.size.y * box.size.z; if (v < best) { best = v; owner[i] = j; }
                }
            }
        }
        /// <summary>A part's box in the building's own frame (walls and floors square to it), from its mesh where it has one.</summary>
        Bounds LocalBounds(Renderer r)
        {
            var mf = r.GetComponent<MeshFilter>(); Bounds src; Matrix4x4 m;
            if (mf != null && mf.sharedMesh != null) { src = mf.sharedMesh.bounds; m = transform.worldToLocalMatrix * r.transform.localToWorldMatrix; }
            else { src = r.bounds; m = transform.worldToLocalMatrix; }
            Vector3 c = src.center, e = src.extents; var lb = new Bounds(m.MultiplyPoint3x4(c), Vector3.zero);
            for (int k = 0; k < 8; k++) lb.Encapsulate(m.MultiplyPoint3x4(c + Vector3.Scale(e, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1))));
            return lb;
        }
        /// <summary>Whether a point stands within the walls (its footprint less the eaves, below the roof's top).</summary>
        bool Holds(Vector3 world)
        {
            var p = transform.InverseTransformPoint(world);
            if (Half.x > 0) return Mathf.Abs(p.x) <= Half.x && Mathf.Abs(p.z) <= Half.y && p.y >= bounds.min.y - .5f && p.y <= bounds.max.y;
            return bounds.Contains(p);
        }
        /// <summary>Whether the player stands inside a walled building (AdventurerMotor lets the camera come closer there).</summary>
        public static bool Inside(Vector3 player)
        {
            foreach (var r in All) { if (!r.ready) r.Prepare(); if (r.Walled && r.Holds(player)) return true; }
            return false;
        }
        /// <summary>The player is inside a walled building: the camera stays within its walls, along the line from the player out to it,
        /// a hand's breadth short of them (AdventurerMotor; Round 24, note 65; the tester's note 117, "the camera goes inside walls").</summary>
        public static bool ClampInside(Vector3 player, ref Vector3 camera)
        {
            foreach (var r in All)
            {
                if (!r.ready) r.Prepare();
                if (!r.Walled || !r.Holds(player)) continue;
                var lp = r.transform.InverseTransformPoint(player); var lc = r.transform.InverseTransformPoint(camera); var d = lc - lp;
                if (r.Half.x <= 0) { if (!r.bounds.Contains(lc) && r.bounds.IntersectRay(new Ray(lc, -d.normalized), out float hit)) camera = r.transform.TransformPoint(lc - d.normalized * (hit + .35f)); return true; }
                float hx = r.Half.x - .35f, hz = r.Half.y - .35f, t = 1;
                if (Mathf.Abs(d.x) > 1e-4f) t = Mathf.Min(t, (Mathf.Sign(d.x) * hx - lp.x) / d.x);
                if (Mathf.Abs(d.z) > 1e-4f) t = Mathf.Min(t, (Mathf.Sign(d.z) * hz - lp.z) / d.z);
                float length = d.magnitude; if (length > .01f) t = Mathf.Max(t, Mathf.Min(1, .4f / length));   // never into the player's own head
                camera = r.transform.TransformPoint(lp + d * Mathf.Clamp01(t));
                return true;
            }
            return false;
        }
        /// <summary>Fades the parts that block the camera's view of the player and brings back the rest. Once a frame, after the camera
        /// has moved (AdventurerMotor, with TreeFade.UpdateAll).</summary>
        public static void UpdateAll(Vector3 camera, Vector3 head, Vector3 body)
        {
            if (TreeFade.FadeTemplate == null) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            foreach (var r in All)
            {
                if (!r.ready) r.Prepare();
                r.Aim(camera, head, body);
                bool changed = false;
                for (int i = 0; i < r.parts.Length; i++)
                {
                    if (Mathf.Approximately(r.alpha[i], r.target[i])) continue;
                    r.alpha[i] = Mathf.MoveTowards(r.alpha[i], r.target[i], dt * 5); changed = true;
                }
                if (changed) r.Apply();
            }
        }
        /// <summary>Each part's aim: faded if the line from the camera to the player's head or body passes through it, or the camera is in
        /// it; a small piece as its big part; solid otherwise.</summary>
        void Aim(Vector3 camera, Vector3 head, Vector3 body)
        {
            Vector3 c = transform.InverseTransformPoint(camera), h = transform.InverseTransformPoint(head), b = transform.InverseTransformPoint(body);
            var whole = bounds; whole.Expand(.3f);
            bool near = whole.Contains(c) || Entry(whole, c, h) >= 0 || Entry(whole, c, b) >= 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!big[i]) continue; var pb = partBounds[i];
                bool hides = near && (pb.Contains(c) || (!pb.Contains(h) && Entry(pb, c, h) >= 0) || (!pb.Contains(b) && Entry(pb, c, b) >= 0));
                target[i] = hides ? FadedAlpha : 1;
            }
            for (int i = 0; i < parts.Length; i++) if (!big[i]) target[i] = owner[i] >= 0 ? target[owner[i]] : 1;
        }
        static float Entry(Bounds box, Vector3 from, Vector3 to)
        {
            var d = to - from; float length = d.magnitude; if (length < .01f) return -1;
            return box.IntersectRay(new Ray(from, d / length), out float hit) && hit < length - .4f ? hit : -1;
        }
        void Apply()
        {
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < parts.Length; i++)
            {
                var r = parts[i]; if (r == null) continue;
                var mats = solid[i];
                if (alpha[i] >= .999f) { if (fadedMats[i] != null && r.sharedMaterial != (mats.Length > 0 ? mats[0] : null)) { r.sharedMaterials = mats; r.SetPropertyBlock(null); } continue; }
                if (fadedMats[i] == null) { fadedMats[i] = new Material[mats.Length]; for (int m = 0; m < mats.Length; m++) fadedMats[i][m] = TreeFade.FadeOf(mats[m]); }
                r.sharedMaterials = fadedMats[i];
                var tint = mats.Length > 0 && mats[0] != null && mats[0].HasProperty("_Color") ? mats[0].color : Color.white;
                r.GetPropertyBlock(block); tint.a = alpha[i]; block.SetColor("_Color", tint); r.SetPropertyBlock(block);
            }
        }
        void Restore()
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                parts[i].sharedMaterials = solid[i]; parts[i].SetPropertyBlock(null); alpha[i] = target[i] = 1;
            }
        }
    }
}
