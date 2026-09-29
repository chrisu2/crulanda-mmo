using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Scans every DefinitionBase asset for invalid or duplicate ids and for definitions missing from all
    /// ContentDatabases. Menu: Crulanda > Validate Content. Run before commits and builds.
    /// </summary>
    public static class ContentValidator
    {
        [MenuItem("Crulanda/Validate Content")]
        public static void Validate()
        {
            int problems = 0;
            var idToPath = new Dictionary<ContentId, string>();
            var allDefinitions = new List<DefinitionBase>();

            foreach (string guid in AssetDatabase.FindAssets("t:DefinitionBase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<DefinitionBase>(path);
                if (def == null) continue;
                allDefinitions.Add(def);

                if (!def.Id.IsValid)
                {
                    Debug.LogError("[Crulanda] Invalid ContentId '" + def.Id + "' in " + path, def);
                    problems++;
                    continue;
                }

                string other;
                if (idToPath.TryGetValue(def.Id, out other))
                {
                    Debug.LogError("[Crulanda] Duplicate ContentId '" + def.Id + "' in " + path + " and " + other, def);
                    problems++;
                }
                else
                {
                    idToPath.Add(def.Id, path);
                }

                foreach (var tag in def.Tags.Tags)
                {
                    if (!GameTags.IsKnown(tag))
                    {
                        Debug.LogWarning("[Crulanda] Unknown tag '" + tag + "' in " + path, def);
                        problems++;
                    }
                }
            }

            var listed = new HashSet<DefinitionBase>();
            foreach (string guid in AssetDatabase.FindAssets("t:ContentDatabase"))
            {
                var db = AssetDatabase.LoadAssetAtPath<ContentDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if (db == null) continue;
                foreach (var d in db.Definitions) if (d != null) listed.Add(d);
            }

            foreach (var def in allDefinitions)
            {
                if (!listed.Contains(def))
                {
                    Debug.LogWarning("[Crulanda] '" + def.name + "' (" + def.Id + ") is not in any ContentDatabase.", def);
                    problems++;
                }
            }

            Debug.Log("[Crulanda] Content validation finished: " + allDefinitions.Count + " definitions, " +
                      problems + " problem(s).");
        }
    }
}
