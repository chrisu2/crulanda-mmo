using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Dishes you can see (playtest note 235, 2026-10-18: "FBX ... for foods and buff items"): a dish you cook is set down by the fire you
    // cooked it at, and food bought from an innkeeper is set down on the nearest of the inn's tables (ZoneBuilder.TableTops), each in its
    // look from FoodModels.Dishes, for a while and then cleared away. For the eye only: nothing to pick up, no collider. GAME-ONLY.
    // ============================================================================================
    public partial class EncounterSession
    {
        /// <summary>How long a served or cooked dish stays out (seconds).</summary>
        public const float DishSeconds = 90;
        /// <summary>No more than this many out at once: the oldest is cleared first.</summary>
        const int DishesOut = 6;
        readonly List<GameObject> dishes = new List<GameObject>();
        /// <summary>The role of the villager whose wares are open (OpenVendor): an innkeeper's food goes on a table.</summary>
        string vendorRole;
        /// <summary>Puts a dish's look down at a world point (its foot), turned to face <paramref name="toward"/>; null for an item with none.</summary>
        GameObject SetDish(string item, Vector3 foot, Vector3 toward)
        {
            if (Zone == null || !FoodModels.Dishes.ContainsKey(item)) return null;
            var face = toward - foot; face.y = 0; float yaw = face.sqrMagnitude > .01f ? Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg : 0;
            var go = FoodModels.Serve(Zone.transform, Zone.transform.InverseTransformPoint(foot), yaw - Zone.transform.eulerAngles.y, item);
            if (go == null) return null;
            dishes.RemoveAll(d => d == null);
            while (dishes.Count >= DishesOut) { Destroy(dishes[0]); dishes.RemoveAt(0); }
            dishes.Add(go); Destroy(go, DishSeconds);
            return go;
        }
        /// <summary>A dish just cooked, on the ground by the fire it was made at, on the side the cook stands.</summary>
        void ShowCooked(RecipeDef r)
        {
            if (r == null || Player == null) return;
            var station = StationFor(r); if (station == null || station.kind != "fire") return;
            var fire = station.position; var me = Player.transform.position; var off = me - fire; off.y = 0;
            off = off.sqrMagnitude > .01f ? off.normalized : Vector3.forward;
            var foot = fire + off * .8f; foot.y = Mathf.Max(fire.y, me.y - .05f);
            SetDish(r.output, foot, me);
        }
        /// <summary>Food bought from an innkeeper, on the nearest of the inn's tables within reach of the counter (none: in the bags only).</summary>
        void ServeBought(ItemDef d)
        {
            if (VendorNpc == null || VendorSim != null || d == null || !d.food || Zone == null || vendorRole != "innkeeper") return;
            Vector3 best = Vector3.zero; float bestD = 14;
            foreach (var top in Zone.TableTops) { float dd = Vector3.Distance(top, vendorAt); if (dd < bestD) { bestD = dd; best = top; } }
            if (bestD >= 14) return;
            SetDish(d.id, best + Vector3.up * .005f, Player != null ? Player.transform.position : best + Vector3.back);
        }
    }
}
