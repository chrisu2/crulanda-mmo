using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Data
{
    /// <summary>
    /// Explicit list of all definitions shipped with the game. Explicit (rather than Resources.LoadAll)
    /// so loading is deterministic and Addressables can replace it later without changing callers.
    /// Populate via Crulanda menu tools; run "Validate Content" to find definitions missing from it.
    /// </summary>
    [CreateAssetMenu(menuName = "Crulanda/Content Database", fileName = "ContentDatabase")]
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] private List<DefinitionBase> _definitions = new List<DefinitionBase>();

        public IReadOnlyList<DefinitionBase> Definitions { get { return _definitions; } }

        public ContentRegistry BuildRegistry()
        {
            var registry = new ContentRegistry();
            for (int i = 0; i < _definitions.Count; i++)
                registry.TryAdd(_definitions[i]);

            CrulandaLog.Info(LogCategory.Data,
                "Content registry built: " + registry.Count + " of " + _definitions.Count + " definitions accepted.");
            return registry;
        }

#if UNITY_EDITOR
        /// <summary>Editor tooling only.</summary>
        public void EditorSetDefinitions(IEnumerable<DefinitionBase> definitions)
        {
            _definitions = new List<DefinitionBase>(definitions);
        }
#endif
    }
}
