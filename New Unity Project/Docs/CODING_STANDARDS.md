# Coding Standards

- C# for Unity 6. Namespaces match assemblies (`Crulanda.Gameplay`). One top-level type per file, file = type name
  (mandatory for MonoBehaviour/ScriptableObject).
- Private fields `_camelCase`; serialized private fields use `[SerializeField] private`. Public API is PascalCase.
- No magic numbers: name constants or expose config. No `Update` unless truly per-frame; prefer events/timers.
- MonoBehaviours are thin. Logic that can be plain C# goes in plain classes with EditMode tests.
- Never `Debug.Log` directly in game code: use `CrulandaLog.X(LogCategory.Y, ...)`.
- Cross-system notifications: `EventBus` structs. Owner-to-listener: C# events. Unsubscribe in `OnDisable`/`OnDestroy`.
- Any new static state must reset in a `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` method.
- Editor/test-only mutators on runtime classes are wrapped in `#if UNITY_EDITOR` and prefixed `Editor`.
- Content and rules live in data assets/tags, not scene hand-wiring.
- Definition of done = brief section 48 (compiles, testable in game, edge cases, save/load impact, debug info, docs, steps).
