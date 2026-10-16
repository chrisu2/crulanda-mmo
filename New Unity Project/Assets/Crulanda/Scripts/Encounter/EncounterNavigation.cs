using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Builds the runtime navigation mesh. In a generated zone (ZoneBuilder present) it walks the sculpted ground and
    /// bridge decks, cuts out every NavBlocker's colliders and blocks water so agents use the bridges. The legacy
    /// Quiet Trail scene keeps its name-based props path.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class EncounterNavigation : MonoBehaviour
    {
        NavMeshData data;
        NavMeshDataInstance instance;
        readonly List<NavMeshLinkInstance> links = new List<NavMeshLinkInstance>();
        void Awake()
        {
            var zone = ZoneBuilder.Active;
            var sources = zone != null ? ZoneSources(zone) : LegacySources();
            float size = zone != null ? zone.Zone.size : 64;
            Physics.SyncTransforms();
            var reach = new Bounds(Vector3.zero, new Vector3(size, 40, size));
            if (zone != null) reach = Crulanda.World.Hollow.Reach(reach);   // cave passages may run on under the hills past the edge
            data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources, reach, Vector3.zero, Quaternion.identity);
            if (data != null) instance = NavMesh.AddNavMeshData(data);
            else Debug.LogError("Could not build encounter navigation.");
            // (The Adit rework) The caves' one-way drops (ZoneDrop): a link down off each lip, never back up. A camp's mobs keep off them
            // (their agents leave DropArea out of their mask).
            if (zone != null && data != null)
                foreach (var d in zone.Drops)
                    links.Add(NavMesh.AddLink(new NavMeshLinkData { startPosition = d.from, endPosition = d.to, width = d.width, bidirectional = false, area = DropArea, agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID }));
        }
        static List<NavMeshBuildSource> ZoneSources(ZoneBuilder zone)
        {
            var sources = new List<NavMeshBuildSource> {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = zone.GroundMesh.sharedMesh,
                    transform = zone.GroundMesh.transform.localToWorldMatrix, area = 0 }
            };
            foreach (var deck in FindObjectsByType<NavWalkable>(FindObjectsSortMode.None))
            {
                var mf = deck.GetComponent<MeshFilter>(); if (mf == null) continue;
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = mf.sharedMesh, transform = deck.transform.localToWorldMatrix, area = 0 });
            }
            foreach (var blocker in FindObjectsByType<NavBlocker>(FindObjectsSortMode.None))
                foreach (var col in blocker.GetComponents<Collider>())
                {
                    if (col is BoxCollider box)
                    {
                        // Oriented box: a rotated wall must not blank out the axis-aligned area around it (e.g. an inn interior).
                        var tr = box.transform; var scale = tr.lossyScale;
                        var size = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) + new Vector3(2 * ZoneBuilder.NavPad, 0, 2 * ZoneBuilder.NavPad);   // the pad the doorways are measured with (ZoneBuilder.NavGap)
                        var at = Matrix4x4.TRS(tr.TransformPoint(box.center), tr.rotation, Vector3.one);
                        sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = size, transform = at, area = 1 });
                        // And the box's inside: a Box source is its faces, and a floor under a block with head-room between them stayed
                        // walkable (the island under Lusk's tally stage, the Adit rework, 2026-10-14); the modifier marks the volume.
                        sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.ModifierBox, size = size, transform = at, area = 1 });
                        continue;
                    }
                    var b = col.bounds;
                    sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = b.size + new Vector3(2 * ZoneBuilder.NavPad, 0, 2 * ZoneBuilder.NavPad),
                        transform = Matrix4x4.TRS(b.center, Quaternion.identity, Vector3.one), area = 1 });
                }
            // Water: shallow (wading) water is walkable but costly (area 3), so agents prefer bridges yet can follow you in.
            // Swim-depth water is not walkable (area 1). Modifier volumes (not solid boxes) follow each 3 m of creek at its
            // own level and width, and cover lakes in radial wedges out to their wandering shore, so they never reach up into
            // bridge decks or out over dry shore.
            NavMesh.SetAreaCost(ShallowsArea, 6);
            var water = zone.Water;
            if (water != null)
            {
                foreach (var c in water.Creeks)
                    for (int i = 0; i + 1 < c.pts.Length; i++)
                    {
                        Vector2 a = c.pts[i], b = c.pts[i + 1], mid = (a + b) / 2, dir = b - a; if (dir.sqrMagnitude < .001f) continue;
                        float level = (c.level[i] + c.level[i + 1]) / 2, bottom = level + ZoneWater.BankDrop - c.depth - 1, wide = (c.wide[i] + c.wide[i + 1]) / 2;
                        var rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
                        if (c.waterHalf > .2f) sources.Add(Modifier(new Vector3(mid.x, (bottom + level + .05f) / 2, mid.y), rot, new Vector3(c.waterHalf * wide * 2, level + .05f - bottom, dir.magnitude + .6f), ShallowsArea));
                        if (c.swimHalf > .3f) sources.Add(Modifier(new Vector3(mid.x, (bottom + level + .05f) / 2, mid.y), rot, new Vector3(c.swimHalf * wide * 2, level + .05f - bottom, dir.magnitude + .6f), 1));
                    }
                foreach (var k in water.Lakes)
                {
                    // Radial wedges out to the irregular waterline, and in from it to where the water gets swim-deep.
                    float bottom = k.bottom - 1; const int Seg = 48;
                    for (int s = 0; s < Seg; s++)
                    {
                        float a = (s + .5f) * Mathf.PI * 2 / Seg; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
                        float wet = k.RadiusAt(dir), swim = k.swimInset > 0 ? wet - k.swimInset : 0;
                        foreach (var (reach, area) in new[] { (wet, ShallowsArea), (swim, 1) })
                        {
                            if (reach < .3f) continue;
                            var mid = k.def.center + dir * reach / 2;
                            sources.Add(Modifier(new Vector3(mid.x, (bottom + k.level + .05f) / 2, mid.y), rot, new Vector3(reach * Mathf.PI * 2 / Seg + .3f, k.level + .05f - bottom, reach), area));
                        }
                    }
                }
            }
            // (The Adit rework) The caves' pools, streams and flooded stretches (ZoneCaveWater), as the land's water: wading costs, swim-deep is not walked.
            foreach (var (centre, rot, size, swim) in zone.CaveWaterAreas) sources.Add(Modifier(centre, rot, size, swim ? 1 : ShallowsArea));
            return sources;
        }
        /// <summary>Navigation area for wadeable water: walkable, but agents avoid it when a dry way exists.</summary>
        public const int ShallowsArea = 3;
        /// <summary>Navigation area of the caves' one-way drops (the Adit rework): the party may take them down; a camp's mobs keep off them.</summary>
        public const int DropArea = 4;
        static NavMeshBuildSource Modifier(Vector3 centre, Quaternion rot, Vector3 size, int area)
        { return new NavMeshBuildSource { shape = NavMeshBuildSourceShape.ModifierBox, size = size, transform = Matrix4x4.TRS(centre, rot, Vector3.one), area = area }; }
        static List<NavMeshBuildSource> LegacySources()
        {
            var sources = new List<NavMeshBuildSource> {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(60,.5f,60),
                    transform = Matrix4x4.TRS(new Vector3(0,-.25f,0),Quaternion.identity,Vector3.one), area = 0 }
            };
            // Upgrade existing prototype scenes as well as newly built scenes.
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string label = renderer.gameObject.name;
                if (label != "Tree trunk" && label != "Ridge stone" && label != "Supply tent" &&
                    label != "Tent roof" && label != "Camp crate" && label != "Ruin pillar" &&
                    label != "Ruin lintel" && label != "Boundary") continue;
                var collider = renderer.GetComponent<Collider>();
                if (collider == null)
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    if (label == "Tree trunk" || label == "Ruin pillar")
                    {
                        var capsule = renderer.gameObject.AddComponent<CapsuleCollider>();
                        capsule.center = mesh.bounds.center; capsule.height = mesh.bounds.size.y;
                        capsule.radius = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
                    }
                    else
                    {
                        var box = renderer.gameObject.AddComponent<BoxCollider>();
                        box.center = mesh.bounds.center; box.size = mesh.bounds.size;
                    }
                }
                // Block the footprint, including the top, so agents cannot route through or onto props.
                var bounds = renderer.bounds;
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    size = bounds.size, transform = Matrix4x4.TRS(bounds.center, Quaternion.identity, Vector3.one), area = 1 });
            }
            return sources;
        }
        void OnDestroy()
        {
            foreach (var l in links) NavMesh.RemoveLink(l);
            links.Clear();
            if (instance.valid) instance.Remove(); if (data != null) Destroy(data);
        }
    }
}
