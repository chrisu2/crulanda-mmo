using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;

namespace Crulanda.Game
{
    /// <summary>
    /// Composition root. Put one in the first scene loaded. It builds the content registry, registers
    /// long-lived services, and applies the default log level. Runs before other scripts.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private ContentDatabase _contentDatabase;
        [SerializeField] private LogLevel _defaultLogLevel = LogLevel.Info;
        [SerializeField] private bool _persistAcrossScenes = true;

        ContentRegistry _registry;
        bool _ownsServices;

        void Awake()
        {
            GameBootstrap existing;
            if (Services.TryGet(out existing) && existing != this)
            {
                CrulandaLog.Warn(LogCategory.Core, "A GameBootstrap already exists; destroying the duplicate.", this);
                Destroy(gameObject);
                return;
            }

            CrulandaLog.SetAllLevels(_defaultLogLevel);

            Services.Register(this);
            _ownsServices = true;

            if (_contentDatabase != null)
            {
                _registry = _contentDatabase.BuildRegistry();
            }
            else
            {
                _registry = new ContentRegistry();
                CrulandaLog.Warn(LogCategory.Core, "GameBootstrap has no ContentDatabase assigned; registry is empty.", this);
            }
            Services.Register(_registry);

            if (_persistAcrossScenes) DontDestroyOnLoad(gameObject);
            CrulandaLog.Info(LogCategory.Core, "Bootstrap complete.");
        }

        void OnDestroy()
        {
            if (!_ownsServices) return;
            Services.Unregister(_registry);
            Services.Unregister(this);
        }

#if UNITY_EDITOR
        /// <summary>Editor tooling only.</summary>
        public void EditorSetContentDatabase(ContentDatabase database)
        {
            _contentDatabase = database;
        }
#endif
    }
}
