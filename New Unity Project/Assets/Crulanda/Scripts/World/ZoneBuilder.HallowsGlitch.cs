using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// (Hallow's Creek, Docs/REWORK_LOWTOWNS_HALLOWS.md pick 9, "Glitches that change the route": CANON-EXPANDED, the town's frozen minute,
    /// b1 ch.21:9-23; each moment GAME-ONLY) What stands in a way a Glitch opens, built as an open-ground gate's bars (OpenGate): a stone
    /// handcart, one wheel off the ground, across a lane (no machines: a two-wheeled cart pushed by hand); slates hung mid-fall at a ramp's
    /// foot. Grey stone like the paused, until the Glitch rolls the cart aside or lets the slates fall.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        void GlitchGateLook(Transform bars, string look, float w, float high)
        {
            var grey = Tint(art.stone, new Color(.6f, .6f, .62f));
            if (look == "cart")
            {
                // The bed across the lane, tipped a little (its near wheel lifted off the cobbles), its two shafts and its load of sacks.
                var tilt = Quaternion.Euler(0, 0, 6);
                Part(PrimitiveType.Cube, bars, new Vector3(0, .95f, 0), new Vector3(Mathf.Min(w, 3.2f), .14f, 1.5f), grey, tilt);
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, bars, new Vector3(0, 1.2f, side * .72f), new Vector3(Mathf.Min(w, 3.2f), .4f, .08f), grey, tilt);
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, bars, new Vector3(side * .3f, .62f + (side > 0 ? .18f : 0), side * .85f), new Vector3(1.24f, .05f, 1.24f), grey, Quaternion.Euler(90, 0, 0));
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, bars, new Vector3(-Mathf.Min(w, 3.2f) / 2 - .8f, .8f, side * .45f), new Vector3(1.8f, .08f, .08f), grey, Quaternion.Euler(0, 0, -8));
                for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, bars, new Vector3(-.8f + k * .8f, 1.35f, (k % 2) * .2f - .1f), new Vector3(.7f, .45f, .55f), grey);
                // Stone crates stacked either side close the rest of the lane (the cart only rolls; they stay where they stand).
                for (float x = Mathf.Min(w, 3.2f) / 2 + .4f; x < w / 2; x += .9f)
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, bars, new Vector3(side * x, .4f, 0), new Vector3(.8f, .8f, .8f), grey, Quaternion.Euler(0, x * 37, 0));
                return;
            }
            // Slates hung in the air at a ramp's foot, every one stopped mid-turn (the Archive's roof is falling in, b1 ch.18 and the build's
            // own broken roof), thick enough that nobody passes.
            var slate = Tint(art.slate != null ? art.slate : art.stone, new Color(.36f, .37f, .42f)); var rnd = new System.Random(2109);
            for (int k = 0; k < 40; k++)
            {
                float x = ((float)rnd.NextDouble() - .5f) * w, y = .2f + (float)rnd.NextDouble() * (high - .4f), z = ((float)rnd.NextDouble() - .5f) * .5f;
                Part(PrimitiveType.Cube, bars, new Vector3(x, y, z), new Vector3(.5f, .04f, .34f), slate, Quaternion.Euler((float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 360));
            }
        }
    }
}
