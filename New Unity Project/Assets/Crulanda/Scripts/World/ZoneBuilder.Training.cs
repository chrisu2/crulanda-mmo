using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The training field's two dummies (GAME-ONLY; Chris, 2026-10-13: "put a few test dummies in town ... one for dps and one for heals",
    /// "put out in a field near town"). The look is built onto the session's own foe and friend (EncounterSession.Training), so the model,
    /// its nameplate and its click box are always in one place (the 13:27 frames: the foe's agent had snapped it metres from a look built
    /// here at the prop). training_dummy: the asset pack's straw dummy on its post. mending_dummy: the same, bound in pale bandages with a
    /// green sash, so the two read apart from across the field.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>
        /// The sparring yard (Chris, 2026-10-13 ~13:40: "sparring area needs to resemble something like this", with pictures of a fenced
        /// yard of tents, banners, dummies, weapon racks and targets; "try the small props pack in downloads"). GAME-ONLY. A fenced ground
        /// <paramref name="size"/> across (local x) and deep (local z), its gate in the front fence (+z, toward the town): two tents at the
        /// back, banners at the gate and the corners, dummies and mannequins down the left, archery butts down the right, weapon stands and a
        /// peg rack, crates and a barrel by the right tent, and a healer's corner (a stool, a bucket, a basin) in front of it. The session's
        /// own training and mending dummies stand inside it (their own zone props). The Small Props Pack (RG Poly, CC0) gives the butts,
        /// the mannequins and the healer's things.
        /// </summary>
        void TrainingYard(Transform t, Vector2 size)
        {
            float hx = Mathf.Max(8, size.x) / 2, hz = Mathf.Max(6, size.y) / 2, gate = 2.6f;
            float G(float x, float z) { return LocalGround(t, x, z); }
            Transform Side(string name, float x, float z, float yaw)
            {
                var side = new GameObject(name).transform; side.SetParent(t, false); side.localPosition = new Vector3(x, G(x, z), z); side.localRotation = Quaternion.Euler(0, yaw, 0);
                return side;
            }
            // The fence, with the gate in the front.
            Fence(Side("Yard fence, back", 0, -hz, 0), 2 * hx);
            Fence(Side("Yard fence, left", -hx, 0, 90), 2 * hz);
            Fence(Side("Yard fence, right", hx, 0, 90), 2 * hz);
            float front = hx - gate;
            Fence(Side("Yard fence, front left", -(gate + front / 2), hz, 0), front);
            Fence(Side("Yard fence, front right", gate + front / 2, hz, 0), front);
            // Two tents at the back, their open sides to the yard.
            GroundProp(t, "Megapack/Models/Tent_1", new Vector3(-hx + 3.4f, 0, -hz + 2.8f), 180, 3.0f, true);
            GroundProp(t, "Megapack/Models/Tent_1", new Vector3(hx - 3.4f, 0, -hz + 2.8f), 180, 3.0f, true);
            // Banners: either side of the gate and at the four corners.
            foreach (float x in new[] { -gate - .4f, gate + .4f }) GroundProp(t, "Fantasy/Banner_1", new Vector3(x, 0, hz + .3f), 0, 2.8f);
            foreach (var (x, z) in new[] { (-hx + .5f, -hz + .5f), (hx - .5f, -hz + .5f), (-hx + .5f, hz - .5f), (hx - .5f, hz - .5f) }) GroundProp(t, "Fantasy/Banner_2", new Vector3(x, 0, z), 0, 3.2f);
            // Dummies and mannequins down the left, facing in.
            string[] dummies = { "Megapack/Models/Outpost/Dummy", "SmallProps/Manequin_1_A_001", "Fantasy/Dummy", "SmallProps/Manequin_2_A_002" };
            for (int i = 0; i < dummies.Length; i++) GroundProp(t, dummies[i], new Vector3(-hx + 1.6f, 0, -1.6f + i * 2.2f), 90, dummies[i].StartsWith("SmallProps") ? 1.8f : 1.9f, true);
            // Archery butts down the right, facing in.
            string[] butts = { "SmallProps/PracticeTarget_1_A", "SmallProps/PracticeTarget_2_A", "Megapack/Models/Arrow Target", "SmallProps/PracticeTarget_1_B" };
            for (int i = 0; i < butts.Length; i++) GroundProp(t, butts[i], new Vector3(hx - 1.4f, 0, 1.0f + i * 1.9f), -90, 1.7f, true);   // their lanes clear of the healer's corner
            // Weapon stands by the left tent, a peg rack against the back fence.
            GroundProp(t, "Fantasy/WeaponStand", new Vector3(-hx + 1.3f, 0, -hz + 5.6f), 90, 1.6f, true);
            GroundProp(t, "Fantasy/WeaponStand", new Vector3(-hx + 6.4f, 0, -hz + 1.0f), 0, 1.6f, true);
            GroundProp(t, "Fantasy/Peg_Rack", new Vector3(0, 0, -hz + .7f), 0, 1.7f, true);
            // Stores by the right tent.
            Crate(t, new Vector3(hx - 1.1f, G(hx - 1.1f, -hz + 1.1f) + .41f, -hz + 1.1f), .82f, 20);
            Crate(t, new Vector3(hx - 2.0f, G(hx - 2.0f, -hz + 1.0f) + .41f, -hz + 1.0f), .82f, -12);
            Barrel(t, new Vector3(hx - 1.0f, G(hx - 1.0f, -hz + 2.2f), -hz + 2.2f), 1, 30);
            Solid(t, new Vector3(hx - 1.5f, .6f, -hz + 1.5f), new Vector3(2.2f, 1.2f, 2.2f));
            // The healer's corner in front of the right tent.
            GroundProp(t, "SmallProps/Stool_A", new Vector3(hx - 5.4f, 0, -hz + 5.6f), 20, .45f);
            GroundProp(t, "SmallProps/Bucket_1_A", new Vector3(hx - 4.6f, 0, -hz + 5.2f), 0, .35f);
            GroundProp(t, "SmallProps/Basin_A", new Vector3(hx - 6.4f, 0, -hz + 5.8f), 0, .3f);
        }
        /// <summary>The dummy's look under <paramref name="t"/>, its feet at <paramref name="foot"/> (local).</summary>
        public void TrainingDummyLook(Transform t, Vector3 foot, bool mending)
        {
            var straw = Tint(art.hay, new Color(.78f, .66f, .4f)); var wood = Tint(art.timber, new Color(.38f, .27f, .17f));
            if (ModelProp(t, "Megapack/Models/Outpost/Dummy", foot, 0, 1.9f) == null)
            {
                // Without the pack: a post, a straw body and a cross-arm.
                Part(PrimitiveType.Cylinder, t, foot + new Vector3(0, .95f, 0), new Vector3(.12f, .95f, .12f), wood);
                Part(PrimitiveType.Capsule, t, foot + new Vector3(0, 1.25f, 0), new Vector3(.5f, .45f, .4f), straw);
                Part(PrimitiveType.Sphere, t, foot + new Vector3(0, 1.78f, 0), new Vector3(.32f, .32f, .3f), straw);
                Part(PrimitiveType.Cube, t, foot + new Vector3(0, 1.45f, 0), new Vector3(1.2f, .09f, .09f), wood);
            }
            if (!mending) return;
            var bandage = Tint(art.cloth, new Color(.9f, .88f, .8f)); var sash = Tint(art.cloth, new Color(.25f, .55f, .3f));
            foreach (float y in new[] { 1.05f, 1.3f, 1.55f }) Part(PrimitiveType.Cylinder, t, foot + new Vector3(0, y, 0), new Vector3(.56f, .04f, .48f), bandage);
            Part(PrimitiveType.Cube, t, foot + new Vector3(0, 1.3f, 0), new Vector3(.6f, .7f, .06f), sash, Quaternion.Euler(0, 0, 38));
        }
    }
}
