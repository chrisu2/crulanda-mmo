using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Data
{
    /// <summary>
    /// Base class for every static content definition (archetypes now; items, abilities, quests later).
    /// Definitions are immutable at runtime: mutable state belongs in plain C# runtime objects or components.
    /// </summary>
    public abstract class DefinitionBase : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private ContentId _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 6)] private string _description;

        [Header("Classification")]
        [SerializeField] private CanonStatus _canonStatus = CanonStatus.Provisional;
        [SerializeField] private TagSet _tags = new TagSet();

        public ContentId Id { get { return _id; } }
        public string DisplayName { get { return string.IsNullOrEmpty(_displayName) ? name : _displayName; } }
        public string Description { get { return _description; } }
        public CanonStatus Canon { get { return _canonStatus; } }
        public TagSet Tags { get { return _tags; } }

        protected virtual void OnValidate()
        {
            if (!_id.IsValid)
            {
                CrulandaLog.Warn(LogCategory.Data,
                    "Definition '" + name + "' has no valid ContentId (lowercase a-z, 0-9, '_', '-', '.'; start with a letter).",
                    this);
            }

            if (_tags != null)
            {
                foreach (var tag in _tags.Tags)
                {
                    if (!GameTags.IsKnown(tag))
                        CrulandaLog.Warn(LogCategory.Data,
                            "Definition '" + name + "' uses unknown tag '" + tag + "'. Add it to GameTags.", this);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor tooling and tests only; compiled out of player builds.</summary>
        public void EditorSetIdentity(ContentId id, string displayName, CanonStatus canon)
        {
            _id = id;
            _displayName = displayName;
            _canonStatus = canon;
        }
#endif
    }
}
