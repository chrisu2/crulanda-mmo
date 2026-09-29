using System;
using System.Collections.Generic;
using Crulanda.Core;

namespace Crulanda.Data
{
    /// <summary>
    /// Runtime lookup of definitions by <see cref="ContentId"/>. Built once at startup from a
    /// <see cref="ContentDatabase"/> and registered in <see cref="Services"/>.
    /// </summary>
    public sealed class ContentRegistry
    {
        readonly Dictionary<ContentId, DefinitionBase> _byId = new Dictionary<ContentId, DefinitionBase>();

        public int Count { get { return _byId.Count; } }

        /// <summary>Adds a definition. Returns false (and logs) for null, invalid id, or duplicate id.</summary>
        public bool TryAdd(DefinitionBase definition)
        {
            if (definition == null)
            {
                CrulandaLog.Error(LogCategory.Data, "ContentRegistry: null definition skipped.");
                return false;
            }

            if (!definition.Id.IsValid)
            {
                CrulandaLog.Error(LogCategory.Data,
                    "ContentRegistry: '" + definition.name + "' has invalid ContentId '" + definition.Id + "'.", definition);
                return false;
            }

            DefinitionBase existing;
            if (_byId.TryGetValue(definition.Id, out existing))
            {
                CrulandaLog.Error(LogCategory.Data,
                    "ContentRegistry: duplicate id '" + definition.Id + "' ('" + definition.name +
                    "' vs '" + existing.name + "').", definition);
                return false;
            }

            _byId.Add(definition.Id, definition);
            return true;
        }

        public bool TryGet<T>(ContentId id, out T definition) where T : DefinitionBase
        {
            DefinitionBase found;
            if (_byId.TryGetValue(id, out found))
            {
                definition = found as T;
                return definition != null;
            }
            definition = null;
            return false;
        }

        public T Get<T>(ContentId id) where T : DefinitionBase
        {
            T definition;
            if (!TryGet(id, out definition))
                throw new KeyNotFoundException("No " + typeof(T).Name + " with id '" + id + "'.");
            return definition;
        }

        public IEnumerable<T> All<T>() where T : DefinitionBase
        {
            foreach (var d in _byId.Values)
            {
                var typed = d as T;
                if (typed != null) yield return typed;
            }
        }
    }
}
