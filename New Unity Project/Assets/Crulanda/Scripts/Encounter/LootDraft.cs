using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The drafted loot files (EncounterContent/Loot/loot.*.json) for the wardrobe capture only, until step L2 moves them into
    /// EncounterContent/Items and the game loads them. Crulanda > World > Build Oakhaven (and so every player build) writes this
    /// asset to Resources/Gear/LootDraft.asset, which puts the files in a player without registering them on Encounter.asset.
    /// </summary>
    public sealed class LootDraft : ScriptableObject
    {
        public TextAsset[] files = new TextAsset[0];
        public const string ResourcePath = "Gear/LootDraft";
        public const string Folder = "Assets/Crulanda/EncounterContent/Loot";

        /// <summary>
        /// The drafted loot files' text: from the Resources asset, or in the editor straight from the folder when the asset has
        /// not been written yet. Empty when there are none.
        /// </summary>
        public static List<string> Texts()
        {
            var texts = new List<string>();
            var draft = Resources.Load<LootDraft>(ResourcePath);
            if (draft != null && draft.files != null) foreach (var f in draft.files) if (f != null) texts.Add(f.text);
            if (texts.Count == 0 && Application.isEditor)
            {
                var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Loot");
                if (Directory.Exists(dir)) { var files = Directory.GetFiles(dir, "loot.*.json"); Array.Sort(files, StringComparer.Ordinal); foreach (var f in files) texts.Add(File.ReadAllText(f)); }
            }
            return texts;
        }
    }
}
