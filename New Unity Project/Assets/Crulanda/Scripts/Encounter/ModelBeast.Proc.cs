using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// An animal whose file has a skeleton and no clips (2026-10-03: CraftPix's free wild animals, Chris's find; the boar first):
    /// the code moves it. The legs are found from the spine (each limb hanging from the chest or the hips), and every frame the
    /// skeleton is set back to its rest pose and posed: a trot matched to the ground speed (diagonal pairs, the knees folding as
    /// each foot comes forward, the body bobbing and the head nodding with it); standing, it looks about, or roots and grazes with
    /// its head down; lying in wait, it crouches with its legs folded and its head low; a blow is a charge (head down, a lunge, a
    /// toss of the tusks); struck, it flinches; killed, it rolls onto its side where it stood. The same poses serve edit mode
    /// (<see cref="Sample"/>) and play.
    /// </summary>
    public sealed partial class ModelBeast
    {
        bool proc;
        /// <summary>True when the code moves this animal (its file has no clips).</summary>
        public bool Procedural { get { return proc; } }
        struct Leg { public Transform upper, lower; public bool front; public int side; }
        Transform[] pBones; Quaternion[] pRest; readonly List<Leg> pLegs = new List<Leg>();
        Transform pChest, pRear, pNeck, pHead, pTail;
        Vector3 basePos; Quaternion baseRot; float halfWidth, legLen = .4f;
        float pPhase, pSpeed, pWalk, pGraze, pLow, pDeath, pTime, pSeed;
        static readonly Dictionary<string, float> procLengths = new Dictionary<string, float> {
            { "attack", .7f }, { "kick", .7f }, { "hitL", .35f }, { "hitR", .35f }, { "death", .8f }, { "jump", .6f }, { "walk", 1 }, { "run", .6f },
            { "idle", 4 }, { "idle2", 4 }, { "eat", 4 }, { "headlow", 4 } };

        /// <summary>Finds the skeleton the code will move: the chest and the hips (the spine bones the limbs hang from), the neck,
        /// the head, the tail's first bone, and each leg's upper bone and its middle joint, which side and which end.</summary>
        void MakeRig()
        {
            proc = true; pSeed = Mathf.Repeat(Model.position.x * 7.3f + Model.position.z * 3.1f, 6.283f);   // no two of a sounder look about in step
            var skin = Model.GetComponentInChildren<SkinnedMeshRenderer>();
            pBones = skin != null && skin.bones != null && skin.bones.Length > 0 ? skin.bones : Model.GetComponentsInChildren<Transform>();
            pRest = new Quaternion[pBones.Length];
            for (int i = 0; i < pBones.Length; i++) pRest[i] = pBones[i] != null ? pBones[i].localRotation : Quaternion.identity;
            pNeck = Bone("Neck"); pHead = Bone("Head"); pTail = Bone("Tail_1");
            pChest = pNeck != null ? pNeck.parent : Bone("Spine_1");
            pRear = pTail != null ? pTail.parent : Bone("Spine_2");
            // In the animal's own frame (its parent: +Z ahead, +X to its right, metres), the model already turned and scaled.
            var frame = Model.parent;
            Vector3 Local(Vector3 world) { return frame != null ? frame.InverseTransformPoint(world) : world; }
            var centre = Local((pChest != null ? pChest.position : Model.position + Vector3.up) * .5f + (pRear != null ? pRear.position : Model.position + Vector3.up) * .5f);
            float lowest = float.MaxValue, highest = float.MinValue;
            foreach (var spine in new[] { pChest, pRear })
            {
                if (spine == null) continue;
                foreach (Transform c in spine)
                {
                    if (c == pNeck || c == pTail || c.name.StartsWith("Spine")) continue;
                    // A limb: a chain of at least three bones going down from the spine.
                    var chain = new List<Transform> { c }; var t = c;
                    while (t.childCount > 0) { t = t.GetChild(0); chain.Add(t); }
                    if (chain.Count < 3) continue;
                    var top = Local(c.position); var end = Local(chain[chain.Count - 1].position);
                    if (end.y > top.y - .03f) continue;   // not hanging down: an ear, a horn, a wing
                    lowest = Mathf.Min(lowest, end.y); highest = Mathf.Max(highest, top.y);
                    pLegs.Add(new Leg { upper = c, lower = chain[chain.Count / 2], front = spine == pChest, side = top.x - centre.x < 0 ? -1 : 1 });
                }
            }
            if (pLegs.Count > 0) legLen = Mathf.Max(.05f, highest - lowest);
        }
        /// <summary>Back to the rest pose, then this pose. <paramref name="walk"/> 0 standing to 1 striding at <paramref name="phase"/>
        /// (radians); the rest by name: degrees for the head, neck and tail, fractions of its height for the body's moves.</summary>
        void Pose(float walk, float phase, float swingDeg, float headPitch, float headYaw, float neckPitch, float tailYaw, float drop, float lunge, float pitch, float roll, float fold, float death)
        {
            for (int i = 0; i < pBones.Length; i++) if (pBones[i] != null) pBones[i].localRotation = pRest[i];
            var parent = Model.parent; Vector3 right = parent != null ? parent.right : Vector3.right, up = parent != null ? parent.up : Vector3.up;
            // The body: dropped, lunged, pitched; killed, rolled onto its side about its middle and down onto the ground.
            float h = Height, mid = h * .45f;
            var turn = Quaternion.Euler(pitch, 0, roll);
            var pivot = new Vector3(0, basePos.y + mid, 0);
            Model.localRotation = turn * baseRot;
            Model.localPosition = pivot + turn * (basePos - pivot) + new Vector3(0, -drop * h - death * Mathf.Max(0, mid - halfWidth), lunge * h);
            // Legs: diagonal pairs swing together; the middle joint folds as the foot comes forward (and all of them fold to lie low).
            foreach (var l in pLegs)
            {
                float off = (l.front ? 0 : Mathf.PI) + (l.side < 0 ? 0 : Mathf.PI);
                float s = Mathf.Sin(phase + off), lift = Mathf.Max(0, Mathf.Cos(phase + off));
                float swing = -swingDeg * s * walk, bend = 38 * lift * walk + fold * (l.front ? 70 : -70);
                if (death > 0) { swing = Mathf.Lerp(swing, l.front ? -18 : 18, death); bend *= 1 - death; }
                l.upper.rotation = Quaternion.AngleAxis(swing, right) * l.upper.rotation;
                l.lower.rotation = Quaternion.AngleAxis(bend * (l.front ? 1 : -1), right) * l.lower.rotation;
            }
            if (pNeck != null) pNeck.rotation = Quaternion.AngleAxis(neckPitch, right) * pNeck.rotation;
            if (pHead != null) { pHead.rotation = Quaternion.AngleAxis(headYaw, up) * Quaternion.AngleAxis(headPitch, right) * pHead.rotation; }
            if (pTail != null) pTail.rotation = Quaternion.AngleAxis(tailYaw, up) * pTail.rotation;
        }
        /// <summary>Strides per second at a pace (m/s): the trot of a leg this long, quickening, never a blur.</summary>
        float Cadence(float speed) { return Mathf.Min(3.2f, speed / Mathf.Max(.3f, legLen * 2.4f)); }
        /// <summary>A one-shot's pose at <paramref name="t"/> seconds in: head (down +), lunge (of its height), roll, legs' extra swing.</summary>
        static void Shot(string slot, float t, int side, out float head, out float lunge, out float roll, out float yaw)
        {
            head = lunge = roll = yaw = 0;
            switch (slot)
            {
                case "attack": case "kick":
                    // A charge: head down and weight back, a lunge with a toss of the tusks, then back.
                    if (t < .18f) { float k = t / .18f; head = 26 * k; lunge = -.06f * k; }
                    else if (t < .32f) { float k = (t - .18f) / .14f; head = Mathf.Lerp(26, -30, k); lunge = Mathf.Lerp(-.06f, .24f, k); }
                    else { float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .32f) / .38f)); head = -30 * (1 - k); lunge = .24f * (1 - k); }
                    break;
                case "hitL": case "hitR":
                    { float k = Mathf.Sin(Mathf.Clamp01(t / .35f) * Mathf.PI); roll = 9 * k * side; yaw = -16 * k * side; head = -6 * k; }
                    break;
                case "jump":
                    { float k = Mathf.Sin(Mathf.Clamp01(t / .6f) * Mathf.PI); lunge = .3f * k; head = -10 * k; }
                    break;
            }
        }
        /// <summary>Play mode, one frame: eases its gait, its standing pose and any one-shot, and poses it.</summary>
        void ProcTick(float dt)
        {
            pTime += dt;
            pSpeed = Mathf.Lerp(pSpeed, pace, dt * 6);
            pWalk = Mathf.MoveTowards(pWalk, Mathf.InverseLerp(.1f, Mathf.Max(.2f, WalkPace * .5f), pSpeed), dt * 4);
            pPhase += dt * Cadence(pSpeed) * Mathf.PI * 2;
            pGraze = Mathf.MoveTowards(pGraze, rest == "eat" ? 1 : 0, dt * 1.5f);
            pLow = Mathf.MoveTowards(pLow, rest == "headlow" ? 1 : 0, dt * 2);
            float head = 0, lunge = 0, roll = 0, yaw = 0;
            if (dead)
            {
                shotTime += dt; pDeath = Mathf.SmoothStep(0, 1, Mathf.Clamp01(shotTime / .6f));
            }
            else
            {
                pDeath = 0;
                if (shot != null) { shotTime += dt; Shot(shot, shotTime, shot == "hitR" ? -1 : 1, out head, out lunge, out roll, out yaw); if (shotTime >= ProcLength(shot)) shot = null; }
            }
            float run = Mathf.InverseLerp(WalkPace, RunPace, pSpeed);
            float look = Mathf.Sin(pTime * .37f + pSeed) * 24 * (1 - pWalk) * (1 - pGraze) * (1 - pLow);
            float root = pGraze * Mathf.Sin(pTime * 5) * 5;
            Pose(pWalk, pPhase, Mathf.Lerp(24, 34, run), head + pGraze * 18 + root + Mathf.Sin(pPhase * 2) * 4 * pWalk + pLow * 10 + pDeath * 10, look + yaw,
                pGraze * 34 + pLow * 18, Mathf.Sin(pPhase) * 18 * pWalk + Mathf.Sin(pTime * 1.3f) * 8 * (1 - pWalk),
                Mathf.Abs(Mathf.Cos(pPhase * 2)) * .025f * pWalk + pLow * .2f, lunge, run * 4 * pWalk, roll + pDeath * 88, pLow, pDeath);
        }
        float ProcLength(string slot) { return procLengths.TryGetValue(slot, out var l) ? l : 1; }
        /// <summary>Edit mode: the pose <paramref name="time"/> seconds into a motion (walk and run at their paces).</summary>
        void ProcSample(string slot, float time)
        {
            float head = 0, lunge = 0, roll = 0, yaw = 0, walk = 0, phase = 0, graze = 0, low = 0, death = 0, swing = 24;
            switch (slot)
            {
                case "walk": walk = 1; phase = time * Cadence(WalkPace) * Mathf.PI * 2; break;
                case "run": walk = 1; swing = 34; phase = time * Cadence(RunPace) * Mathf.PI * 2; break;
                case "eat": graze = 1; break;
                case "headlow": low = 1; break;
                case "death": death = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .6f)); break;
                default: Shot(slot, time, slot == "hitR" ? -1 : 1, out head, out lunge, out roll, out yaw); break;
            }
            Pose(walk, phase, swing, head + graze * 18 + low * 10 + death * 10, yaw, graze * 34 + low * 18, 0, low * .2f, lunge, 0, roll + death * 88, low, death);
        }
    }
}
