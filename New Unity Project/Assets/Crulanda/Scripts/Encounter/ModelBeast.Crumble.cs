using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A death that falls apart (2026-10-04, Chris on the drowned dead: "instead of falling face first, maybe crumble into a pile
    /// of bones?"): the clips stop, and every bone of the rig drops where it is, the lower ones a moment before the upper, as if
    /// what held them up had let go all at once. Each falls under its own weight, drifts in toward the middle so they land in a
    /// heap, and turns as it falls to lie flat on the ground (the skull on its side). The pose is a function of the time since
    /// death alone, so edit mode (<see cref="Sample"/>) and play draw the same heap; <see cref="Revive"/> stands it up again.
    /// </summary>
    public sealed partial class ModelBeast
    {
        /// <summary>Dies by falling apart into a heap rather than playing its death (the skeletons).</summary>
        public bool Crumbles;
        float groundLocal; bool crumbling; float crumbleTime;
        struct Crumb { public Transform t; public Vector3 from, to; public Quaternion rotFrom, rotTo; public float delay, fall; }
        readonly List<Crumb> crumbs = new List<Crumb>();

        /// <summary>Lets go: the bones as they stand now, and where each will come to rest.</summary>
        void StartCrumble()
        {
            crumbling = true; crumbleTime = 0; crumbs.Clear();
            if (graph.IsValid()) graph.Stop();
            if (Animator != null) Animator.enabled = false;
            var bones = new List<Transform>();
            foreach (var r in Renderers) if (r is SkinnedMeshRenderer sk) foreach (var b in sk.bones) if (b != null && !bones.Contains(b)) bones.Add(b);
            // Parents before children: each bone's place is set in the world, so a child set after its parent stays where it is put.
            int Depth(Transform t) { int d = 0; for (var p = t; p != null && p != Model; p = p.parent) d++; return d; }
            bones.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            float ground = Model.parent != null ? Model.parent.TransformPoint(new Vector3(0, groundLocal, 0)).y : Model.position.y;
            var centre = Model.position; centre.y = 0;
            for (int i = 0; i < bones.Count; i++)
            {
                var t = bones[i]; var from = t.position;
                float h1 = Hash(i, 1), h2 = Hash(i, 2), h3 = Hash(i, 3);
                // Into a heap: halfway in toward the middle, a little scattered, a little stacked.
                var flat = new Vector3(from.x, 0, from.z); var inward = centre + (flat - centre) * .45f;
                float a = h1 * Mathf.PI * 2, spread = .08f + h2 * .3f;
                var to = inward + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * spread; to.y = ground + .03f + h3 * .09f;
                // Lying flat: the bone's length (toward its first child; the skull its up) turned to the horizontal, any way round.
                var along = t.childCount > 0 ? (t.GetChild(0).position - from) : t.up; if (along.sqrMagnitude < 1e-6f) along = t.up;
                float yaw = h3 * 360; var lie = Quaternion.Euler(0, yaw, 0) * Vector3.forward; lie.y = -.08f;
                var rotTo = Quaternion.FromToRotation(along.normalized, lie.normalized) * t.rotation;
                float drop = Mathf.Max(.02f, from.y - to.y);
                crumbs.Add(new Crumb { t = t, from = from, to = to, rotFrom = t.rotation, rotTo = rotTo,
                    delay = Mathf.Clamp01((from.y - ground) / 2f) * .18f + h2 * .05f, fall = Mathf.Sqrt(2 * drop / 9.8f) });
            }
        }
        /// <summary>The heap <paramref name="time"/> seconds after it let go.</summary>
        void CrumblePose(float time)
        {
            foreach (var c in crumbs)
            {
                if (c.t == null) continue;
                float t = Mathf.Max(0, time - c.delay), k = Mathf.Clamp01(t / Mathf.Max(.05f, c.fall));
                float y = Mathf.Max(c.to.y, c.from.y - 4.9f * t * t);                     // down under its own weight
                var p = Vector3.Lerp(c.from, c.to, Mathf.SmoothStep(0, 1, k)); p.y = y;  // drifting in to the heap as it goes
                c.t.SetPositionAndRotation(p, Quaternion.Slerp(c.rotFrom, c.rotTo, Mathf.SmoothStep(0, 1, k)));
            }
        }
        void CrumbleTick(float dt) { if (crumbleTime > 3) return; crumbleTime += dt; CrumblePose(crumbleTime); }
        /// <summary>Stood up again: the clips take the bones back.</summary>
        void EndCrumble()
        {
            crumbling = false; crumbs.Clear();
            if (Animator != null) Animator.enabled = true;
            if (graph.IsValid()) graph.Play();
        }
        // ------------------------------------------------------------------------------------------------- tipping over
        /// <summary>Dies by keeling over onto its side, still, rather than playing its death (the crows: the raven's own death
        /// turned it on its back; Chris: "bird should just fall over on the side. not upside down").</summary>
        public bool TipsOver;
        bool tipping; float tipTime;
        void StartTip() { tipping = true; tipTime = 0; shot = null; pace = 0; rest = "idle"; }
        /// <summary><paramref name="time"/> seconds after it died: a moment on its feet, then over onto its right side in a third of a
        /// second, its flank on the ground, and it lies still (its clips stopped).</summary>
        void TipPose(float time)
        {
            float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - .08f) / .35f)), roll = -90 * k;
            var turn = Quaternion.AngleAxis(roll, Vector3.forward); var pivot = new Vector3(0, groundLocal, 0);
            Model.localRotation = turn * baseRot;
            Model.localPosition = pivot + turn * (basePos - pivot) + new Vector3(0, Height * .28f * k, 0);   // its flank, not its feet, on the ground
        }
        void TipTick(float dt)
        {
            if (tipTime > 1) return;
            tipTime += dt;
            if (tipTime > .1f && graph.IsValid() && graph.IsPlaying()) graph.Stop();   // still: no breathing, no blinking
            TipPose(tipTime);
        }
        void EndTip()
        {
            tipping = false; Model.localRotation = baseRot; Model.localPosition = basePos;
            if (graph.IsValid() && !graph.IsPlaying()) graph.Play();
        }
        static float Hash(int i, int salt)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(salt * 19349663); h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return (h & 0xffff) / 65536f;
        }
    }
}
