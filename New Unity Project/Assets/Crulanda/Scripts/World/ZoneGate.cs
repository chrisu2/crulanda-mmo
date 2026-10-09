using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.World
{
    /// <summary>
    /// A gate across a cave's way (dungeon step D3: the Sealed Adit's cage-lift gate and the gate onto the Rail Hall's platform): an
    /// iron grille in a frame. Shut, it stops the player (a collider) and every agent, mobs, Mira and sims alike (a navmesh obstacle
    /// that carves the floor); open, both are gone and the grille climbs into its frame. The zone builder registers each in
    /// ZoneBuilder.Gates; what opens it is the session's business (EncounterSession.Adit).
    /// </summary>
    public sealed class ZoneGate : MonoBehaviour
    {
        /// <summary>What it is called in words ("the cage-lift gate"), the cave it stands in and how far in.</summary>
        public string Title, Cave; public float Along;
        /// <summary>The elite whose hall lies behind it (the loud way in brings his people), or null.</summary>
        public string Lord;
        /// <summary>The bars, moved up by <see cref="Rise"/> metres as it opens.</summary>
        public Transform Grille; public float Rise = 2.5f;
        /// <summary>Shown once it is open (the lift's frame: the three sigils set in their sockets).</summary>
        public GameObject[] Lights = new GameObject[0];
        BoxCollider wall; NavMeshObstacle cut; float lift, target;
        public bool Open { get; private set; }

        /// <summary>Makes the blocking box, <paramref name="size"/> wide, high and deep, standing on the gate's foot.</summary>
        public void Init(Vector3 size)
        {
            wall = gameObject.AddComponent<BoxCollider>(); wall.center = new Vector3(0, size.y / 2, 0); wall.size = size;
            cut = gameObject.AddComponent<NavMeshObstacle>(); cut.shape = NavMeshObstacleShape.Box; cut.center = wall.center; cut.size = size; cut.carving = true;
        }
        public void SetOpen(bool open, bool instant = false)
        {
            Open = open;
            if (wall != null) wall.enabled = !open;
            if (cut != null) cut.enabled = !open;
            foreach (var l in Lights) if (l != null) l.SetActive(open);
            target = open ? Rise : 0;
            if (instant) { lift = target; Place(); }
        }
        /// <summary>Cuts a shut gate into navmesh built after it (the zone's is built once its props stand: EncounterNavigation).</summary>
        public void Recarve() { if (cut != null && cut.enabled) { cut.enabled = false; cut.enabled = true; } }
        void Update()
        {
            if (Mathf.Approximately(lift, target)) return;
            lift = Mathf.MoveTowards(lift, target, Time.deltaTime * Rise / 2.5f);   // two and a half seconds up into the frame
            Place();
        }
        void Place() { if (Grille != null) Grille.localPosition = new Vector3(0, lift, 0); }
    }
}
