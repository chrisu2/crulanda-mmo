using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The training field's two dummies (GAME-ONLY; Chris, 2026-10-13: "put a few test dummies in town ... one for dps and one for heals",
    /// "put out in a field near town"). Only the look is built here: the session (EncounterSession.Training) stands the foe and the friend
    /// at their feet. training_dummy: the asset pack's straw dummy on its post. mending_dummy: the same, bound in pale bandages with a green
    /// sash, so the two read apart from across the field.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        void TrainingDummyProp(Transform t, bool mending)
        {
            var straw = Tint(art.hay, new Color(.78f, .66f, .4f)); var wood = Tint(art.timber, new Color(.38f, .27f, .17f));
            if (GroundProp(t, "Megapack/Models/Outpost/Dummy", Vector3.zero, 0, 1.9f) == null)
            {
                // Without the pack: a post, a straw body and a cross-arm.
                Part(PrimitiveType.Cylinder, t, new Vector3(0, .95f, 0), new Vector3(.12f, .95f, .12f), wood);
                Part(PrimitiveType.Capsule, t, new Vector3(0, 1.25f, 0), new Vector3(.5f, .45f, .4f), straw);
                Part(PrimitiveType.Sphere, t, new Vector3(0, 1.78f, 0), new Vector3(.32f, .32f, .3f), straw);
                Part(PrimitiveType.Cube, t, new Vector3(0, 1.45f, 0), new Vector3(1.2f, .09f, .09f), wood);
            }
            if (!mending) return;
            var bandage = Tint(art.cloth, new Color(.9f, .88f, .8f)); var sash = Tint(art.cloth, new Color(.25f, .55f, .3f));
            foreach (float y in new[] { 1.05f, 1.3f, 1.55f }) Part(PrimitiveType.Cylinder, t, new Vector3(0, y, 0), new Vector3(.56f, .04f, .48f), bandage);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.3f, 0), new Vector3(.6f, .7f, .06f), sash, Quaternion.Euler(0, 0, 38));
        }
    }
}
