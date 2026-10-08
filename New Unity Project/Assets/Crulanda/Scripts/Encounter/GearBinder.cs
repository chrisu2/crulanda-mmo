using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Shows what the player wears. Made once when play starts and kept across scene loads, it watches the session's player and
    /// equipment every frame and redresses the figure (ActorVisual.ApplyGear) when either changes: equipping, unequipping, a
    /// load (F9 makes a new player), a new zone. It needs no hook in the session; once the professions work is in, one line at
    /// the end of EncounterSession.ApplyEquipment can replace it. A session with no item database (the legacy Quiet Trail)
    /// keeps the class kit.
    /// </summary>
    public sealed class GearBinder : MonoBehaviour
    {
        static GearBinder instance; static bool hooked;
        /// <summary>Dress the player again on the next check (the Show helms switch).</summary>
        public static void Refresh() { if (instance != null) instance.worn = new string[0]; }
        EncounterSession session; Crulanda.Gameplay.Actor dressed; string[] worn = new string[0]; float nextSearch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            Ensure();
            if (hooked) return;
            hooked = true;   // every scene load: look for the new session at once (and come back if something removed the binder)
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => { Ensure(); instance.session = null; instance.nextSearch = 0; };
        }
        static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("Gear binder"); DontDestroyOnLoad(go); instance = go.AddComponent<GearBinder>();
        }

        void LateUpdate()
        {
            if (session == null)
            {
                if (Time.unscaledTime < nextSearch) return;
                nextSearch = Time.unscaledTime + .5f;
                session = FindFirstObjectByType<EncounterSession>(); dressed = null;
                if (session == null) return;
            }
            var player = session.Player; var progress = session.Progress;
            if (player == null || progress == null || progress.equipment == null || session.Items == null) return;
            if (player == dressed && Same(progress.equipment)) return;
            var looks = GearLooks.Load();
            if (!Dress(player.GetComponent<ActorVisual>(), progress.equipment, session.Items, looks)) return;
            dressed = player; Remember(progress.equipment);
        }
        bool Same(List<ItemStack> equipment)
        {
            if (equipment.Count != worn.Length) return false;
            for (int i = 0; i < worn.Length; i++) if (worn[i] != (equipment[i].Empty ? "" : equipment[i].item)) return false;
            return true;
        }
        void Remember(List<ItemStack> equipment)
        {
            worn = new string[equipment.Count];
            for (int i = 0; i < worn.Length; i++) worn[i] = equipment[i].Empty ? "" : equipment[i].item;
        }
        /// <summary>Dresses a figure in this equipment. False, leaving the class kit alone, when there is no figure, item database or looks.</summary>
        public static bool Dress(ActorVisual visual, IList<ItemStack> equipment, ItemDatabase items, GearLooks looks)
        {
            if (visual == null || equipment == null || items == null || looks == null) return false;
            visual.ApplyGear(equipment, items, looks); return true;
        }
    }
}
