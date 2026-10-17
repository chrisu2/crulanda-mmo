using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>A dungeon checkpoint as built: its data, where it stands (on the floor at its foot) and its marker.</summary>
    public sealed class ZoneCheckpointSpot { public ZoneCheckpoint def; public int index; public Vector3 position; public Transform root; }

    /// <summary>
    /// A dungeon's checkpoints (GAME-ONLY; Chris, 2026-10-16: "one thing to help respawns is to have checkpoints in the dungeon so they don't
    /// have to run all the back from the original spawn area. maybe 3 checkpoints in dungeons"): ZoneDefinition.checkpoints, each a marker on
    /// the floor beside the way, built after the secrets (off the maps, as they are). EncounterSession.Checkpoints marks one reached when a
    /// party member comes near it and wakes the fallen at the latest reached. No collider and no navmesh cut: nothing walks round it.
    /// - "cairn" (the Sealed Adit): a miners' waycairn, flat stones stacked chest-high where an old shift could rest and be found, a chalk rune
    ///   on its top stone that glows pale in the dark with a soft light. Not the spirit stone (a tall standing stone you touch).
    /// - "lamp" (Hallow's Creek): a still lamp, an iron post and a lantern whose flame stopped with the town in the pause and has not moved
    ///   since: a warm unmoving light. No machines (Chris, 2026-10-11): nothing turns, nothing works itself.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The zone's checkpoints in the run's order (empty outside a dungeon with them).</summary>
        public readonly List<ZoneCheckpointSpot> Checkpoints = new List<ZoneCheckpointSpot>();

        /// <summary>Where a checkpoint's data puts it: in its cave (along, aside, rise over the floor), else on open ground at <c>at</c>.</summary>
        public Vector3 CheckpointPoint(ZoneCheckpoint c)
        {
            if (c == null) return Vector3.zero;
            var h = CaveNamed(c.cave);
            if (h != null) return CavePoint(h, c.along, c.aside, c.rise);
            return OpenPoint(c.at, c.rise);
        }

        void BuildCheckpoints()
        {
            Checkpoints.Clear();
            if (Zone.checkpoints == null) return;
            for (int k = 0; k < Zone.checkpoints.Length; k++)
            {
                var c = Zone.checkpoints[k]; if (c == null || string.IsNullOrEmpty(c.name)) continue;
                var foot = CheckpointPoint(c);
                // Its face toward the way: in a cave toward the passage's middle, on open ground toward where the zone is entered.
                var h = CaveNamed(c.cave);
                var toward = h != null ? h.At(c.along, 0) : new Vector3(Zone.spawns.player.x, foot.y, Zone.spawns.player.y);
                var face = toward - foot; face.y = 0;
                var marker = new GameObject("Checkpoint: " + c.name).transform; marker.SetParent(transform, false);
                marker.position = foot; marker.rotation = face.sqrMagnitude > .01f ? Quaternion.LookRotation(face) : Quaternion.identity;
                if (c.look == "lamp") StillLamp(marker); else Waycairn(marker, k);
                Checkpoints.Add(new ZoneCheckpointSpot { def = c, index = k, position = foot, root = marker });
            }
        }

        /// <summary>A miners' waycairn: five flat stones stacked to about 1.2 m, a chalk rune on the top one, a soft pale light.</summary>
        void Waycairn(Transform t, int seed)
        {
            var stone = Tint(art.stone, new Color(.5f, .5f, .53f)); var darker = Tint(art.stone, new Color(.38f, .38f, .41f));
            float y = 0;
            float[] w = { 1.25f, 1.05f, .9f, .72f, .55f }, hgt = { .3f, .26f, .24f, .22f, .2f };
            for (int i = 0; i < w.Length; i++)
            {
                float turn = (seed * 37 + i * 53) % 360;
                Part(PrimitiveType.Cube, t, new Vector3(((i * 7 + seed) % 3 - 1) * .04f, y + hgt[i] / 2, 0), new Vector3(w[i], hgt[i], w[i] * .8f), i % 2 == 0 ? stone : darker, Quaternion.Euler(0, turn, i % 2 == 0 ? 2 : -3));
                y += hgt[i];
            }
            // The chalk rune on the top stone's face toward the way, and a little light (a lamp's worth in the dark, nothing like a camp fire).
            var pale = new Color(.75f, .9f, 1f);
            Part(PrimitiveType.Cube, t, new Vector3(0, y - .1f, .29f), new Vector3(.26f, .14f, .02f), Glowing(pale, 1.8f));
            Part(PrimitiveType.Sphere, t, new Vector3(0, y + .05f, 0), new Vector3(.16f, .1f, .16f), Glowing(pale, 1.4f));
            Glow(t, new Vector3(0, y + .6f, .5f), 7, .8f, pale, .8f);
        }

        /// <summary>A still lamp: an iron post, an arm, a lantern with a flame that does not move, a warm light.</summary>
        void StillLamp(Transform t)
        {
            var iron = Tint(art.metal, new Color(.2f, .19f, .18f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .1f, 0), new Vector3(.42f, .2f, .42f), Tint(art.stone, new Color(.46f, .45f, .43f)));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.2f, 0), new Vector3(.12f, 1.1f, .12f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.25f, .2f), new Vector3(.06f, .06f, .5f), iron);
            var lantern = new Vector3(0, 2.0f, .42f);
            Part(PrimitiveType.Cube, t, lantern + new Vector3(0, .24f, 0), new Vector3(.3f, .05f, .3f), iron);
            Part(PrimitiveType.Cube, t, lantern - new Vector3(0, .2f, 0), new Vector3(.26f, .04f, .26f), iron);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -.13f : .13f, z = i < 2 ? -.13f : .13f;
                Part(PrimitiveType.Cube, t, lantern + new Vector3(x, 0, z), new Vector3(.03f, .44f, .03f), iron);
            }
            var flame = new Color(1f, .74f, .38f);
            Part(PrimitiveType.Cube, t, lantern, new Vector3(.24f, .38f, .24f), Glowing(new Color(1f, .86f, .6f), .9f));
            Part(PrimitiveType.Capsule, t, lantern, new Vector3(.07f, .09f, .07f), Glowing(flame, 3));
            Glow(t, lantern, 9, 1.1f, flame, 1.3f);
        }
    }
}
