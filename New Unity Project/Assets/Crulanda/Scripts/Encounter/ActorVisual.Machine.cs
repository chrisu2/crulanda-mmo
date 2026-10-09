using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Rock-Eater (GAME-ONLY; DUNGEON_DESIGN.md 3a): the goblin boring-engine Nix rides in the Geode Floor's workshop, the first
    /// half of his two-part fight. A rust-plated hull on two tracks, the pilot's seat on top, a stack, and the drill out front, which
    /// turns while it lives (faster as it moves). No legs and no arms: the walk cycle leaves it alone, it neither strikes nor flinches,
    /// and it does not sink into the grass (it never lies in wait). Dead, EncounterEnemy leaves it where it stopped, tipped a little.
    /// </summary>
    public sealed partial class ActorVisual
    {
        Transform drill; Material stackSmoke;
        void BuildRockEater()
        {
            var rust = Mat(new Color(.36f, .23f, .15f), .2f, .3f); var iron = Mat(new Color(.22f, .21f, .2f), .3f, .5f); var dark = Mat(new Color(.2f, .14f, .09f));
            var brass = Mat(new Color(.7f, .55f, .25f), .6f, .7f); var glass = Mat(new Color(.8f, .6f, 1), .9f); glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor", new Color(.6f, .3f, 1) * 1.6f);
            cloth = rust; LieDepth = 0;
            // The feet are at -1 (the actor's centre is a metre up): everything sits at its true height.
            Part(PrimitiveType.Cube, body, new Vector3(0, -.2f, 0), new Vector3(2.2f, 1.6f, 2.8f), rust);                              // the hull
            Part(PrimitiveType.Cube, body, new Vector3(0, .62f, -.4f), new Vector3(1.2f, .6f, 1.2f), iron);                               // the pilot's seat
            Part(PrimitiveType.Cube, body, new Vector3(0, 1.0f, -.4f), new Vector3(.9f, .18f, .9f), dark);                                // its canopy
            foreach (int side in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, body, new Vector3(side * 1.2f, -.6f, 0), new Vector3(.4f, .8f, 3), dark);                       // the tracks
                for (int k = 0; k < 3; k++) Part(PrimitiveType.Cylinder, body, new Vector3(side * 1.42f, -.6f, -1 + k), new Vector3(.5f, .05f, .5f), iron, new Vector3(0, 0, 90));   // their wheels
                Part(PrimitiveType.Sphere, body, new Vector3(side * 1.12f, .3f, 1.0f), Vector3.one * .18f, glass);                      // the lamps, geode-lit
            }
            Part(PrimitiveType.Cylinder, body, new Vector3(.6f, 1.25f, -1), new Vector3(.25f, .45f, .25f), iron);                         // the stack
            for (int k = 0; k < 5; k++) Part(PrimitiveType.Sphere, body, new Vector3(-.7f + k * .35f, .65f, 1.35f), Vector3.one * .1f, brass);   // rivets along the drill's collar
            Part(PrimitiveType.Cylinder, body, new Vector3(0, .1f, 1.5f), new Vector3(1.1f, .12f, 1.1f), iron, new Vector3(90, 0, 0));    // the collar
            // The drill: a cone out front, turning on its own pivot.
            drill = new GameObject("Drill").transform; drill.SetParent(body, false); drill.localPosition = new Vector3(0, .1f, 1.6f);
            var cone = new GameObject("Bit", typeof(MeshFilter), typeof(MeshRenderer)).transform; cone.SetParent(drill, false);
            cone.GetComponent<MeshFilter>().sharedMesh = Crulanda.World.ZoneMeshes.Cone(1, 1); cone.GetComponent<MeshRenderer>().sharedMaterial = iron;
            cone.localRotation = Quaternion.Euler(90, 0, 0); cone.localScale = new Vector3(.9f, 1.6f, .9f);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, drill, Quaternion.Euler(0, 0, k * 120) * new Vector3(.3f, 0, .5f), new Vector3(.08f, .08f, .8f), brass, new Vector3(0, 0, k * 120));   // the cutting ridges
        }
        void MachineLate()
        {
            var delta = transform.position - lastPosition; delta.y = 0; lastPosition = transform.position;
            float target = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            speed = Mathf.Lerp(speed, target, Time.deltaTime * 8);
            var a = GetComponent<Crulanda.Gameplay.Actor>(); bool alive = a == null || a.IsAlive;
            if (alive) drill.Rotate(0, 0, (140 + speed * 90) * Time.deltaTime, Space.Self);
        }
    }
}
