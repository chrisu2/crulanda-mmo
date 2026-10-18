using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A slab of the broken bridge turning slowly where the blast froze it ("Some were spinning slowly.", book1 ch.17:201): dressing
    /// beside the walk, never stood on (no collider).
    /// </summary>
    public sealed class SlowSpin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 8;
        void Update() { transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
    }

    /// <summary>
    /// The slab that tips (book1 ch.17:243, "The stone ahead of them tilted. forty-five degrees."; REWORK_LOWTOWNS_HALLOWS pick 10),
    /// made safe for the navmesh and the party: the deck's walk keeps its collider and its navmesh flat and solid, and what you see is a
    /// twin of it that moves. A foot on it and it dips a few degrees and rocks, groaning (the session says so); you keep walking, nothing
    /// falls. Once everyone who stepped on it is well past, it tips to forty-five degrees over the fog; anyone coming near again and it
    /// rights itself, level before they reach it. The parapets stay where they are.
    /// </summary>
    public sealed class TippingSlab : MonoBehaviour
    {
        /// <summary>The dip under a foot (degrees), the rock either side of it, and the tip once the party is past.</summary>
        public const float Dip = 4, Rock = 1.2f, Tipped = 45;
        /// <summary>How near (flat metres from the slab's middle) anyone keeps it level once tipped.</summary>
        public const float Clear = 12;
        Transform pivot, walk; Quaternion level; float angle, target; bool someoneOn, stepped;
        /// <summary>The angle the slab shows now (degrees; tests read it).</summary>
        public float Angle { get { return angle; } }
        /// <summary>Whether it stands tipped (or tipping) now.</summary>
        public bool IsTipped { get { return target >= Tipped - .01f; } }
        /// <summary>Whether a foot has been on it since it last tipped.</summary>
        public bool Stepped { get { return stepped; } }
        /// <summary>Its middle, on its top.</summary>
        public Vector3 Middle { get; private set; }

        /// <summary>Puts the moving twin on a deck's walk (its "Walk" child): the walk's own renderer is hidden, its collider kept.</summary>
        public static TippingSlab On(Transform deck)
        {
            if (deck == null) return null;
            var walk = deck.Find("Walk"); if (walk == null) return null;
            var r = walk.GetComponent<MeshRenderer>(); if (r == null) return null;
            var slab = deck.gameObject.AddComponent<TippingSlab>();
            // The pivot along the walk's long middle line, on its top: it tips about its length, its low side toward the fog.
            var top = walk.position + walk.rotation * new Vector3(0, walk.localScale.y / 2, 0);
            slab.Middle = top; slab.walk = walk;
            slab.pivot = new GameObject("The tipping slab").transform; slab.pivot.SetParent(deck, false); slab.pivot.SetPositionAndRotation(top, walk.rotation); slab.level = walk.rotation;
            var twin = GameObject.CreatePrimitive(PrimitiveType.Cube); twin.name = "Its stone"; Object.DestroyImmediate(twin.GetComponent<Collider>());
            twin.transform.SetParent(slab.pivot, false); twin.transform.localPosition = new Vector3(0, -walk.localScale.y / 2, 0); twin.transform.localScale = walk.localScale;
            twin.GetComponent<MeshRenderer>().sharedMaterial = r.sharedMaterial;
            r.enabled = false;
            return slab;
        }

        /// <summary>Whether someone whose feet (or middle) are at <paramref name="p"/> stands on the slab's walk.</summary>
        public bool Holds(Vector3 p)
        {
            if (walk == null) return false;
            var l = walk.InverseTransformPoint(p);
            float overTop = (l.y - .5f) * walk.localScale.y;
            return Mathf.Abs(l.x) <= .5f && Mathf.Abs(l.z) <= .5f && overTop > -.6f && overTop < 2.6f;
        }
        /// <summary>
        /// Told each frame (EncounterSession.AshRoad): how many stand on it and whether anyone is within <see cref="Clear"/> metres. Returns
        /// 1 the moment a foot first comes onto it (groan), 2 the moment it tips (the party past), 0 otherwise.
        /// </summary>
        public int Feel(int onIt, bool anyoneNear)
        {
            int said = 0;
            if (onIt > 0)
            {
                if (!someoneOn) said = 1;
                someoneOn = true; stepped = true; target = Dip;
            }
            else
            {
                someoneOn = false;
                if (anyoneNear) target = 0;
                else if (stepped && target < Tipped) { target = Tipped; stepped = false; said = 2; }
            }
            return said;
        }

        void Update()
        {
            if (pivot == null) return;
            // Tipping is slow and heavy (about two seconds to forty-five), righting quicker (a second and a half), the dip a moment.
            float speed = target >= Tipped ? 24 : angle > Dip + 1 ? 32 : 10;
            angle = Mathf.MoveTowards(angle, target, speed * Time.deltaTime);
            float rock = someoneOn ? Mathf.Sin(Time.time * 2.3f) * Rock : 0;
            pivot.rotation = level * Quaternion.Euler(0, 0, angle + rock);
        }
    }
}
