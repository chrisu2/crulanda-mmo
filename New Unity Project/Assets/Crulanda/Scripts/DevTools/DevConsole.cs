using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.DevTools
{
    /// <summary>
    /// In-game developer console and HUD line, drawn with IMGUI (fast to build, needs no assets, and is
    /// deliberately throwaway: the real UI is separate). Toggle with the backquote key or F1.
    /// Uses IMGUI events for the toggle key so it does not depend on the legacy Input Manager.
    /// Disables itself in non-development player builds (Debug.isDebugBuild is false there).
    /// </summary>
    public sealed class DevConsole : MonoBehaviour
    {
        const string InputControl = "DevConsoleInput";
        const int MaxLines = 200;
        const int VisibleLines = 14;

        readonly DebugCommandRegistry _commands = new DebugCommandRegistry();
        readonly List<string> _lines = new List<string>();
        readonly List<string> _history = new List<string>();

        bool _open;
        string _input = string.Empty;
        int _historyIndex = -1;
        bool _focusPending;
        float _smoothedDelta = 1f / 60f;
        GUIStyle _monoStyle;

        void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            DevCommands.RegisterAll(_commands);
            AddLine("Dev console ready. Press ` (backquote) or F1. Type 'help'.");
        }

        void OnEnable()
        {
            CrulandaLog.EntryLogged += OnLogEntry;
        }

        void OnDisable()
        {
            CrulandaLog.EntryLogged -= OnLogEntry;
        }

        void Update()
        {
            _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.05f);
        }

        void OnLogEntry(LogEntry entry)
        {
            AddLine("[" + entry.Level + "] " + entry.ToString());
        }

        void AddLine(string text)
        {
            foreach (var line in text.Split('\n')) _lines.Add(line);
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
        }

        void OnGUI()
        {
            var e = Event.current;

            // Swallow the toggle character so it never lands in the text field.
            if (e.type == EventType.KeyDown && e.character == '`')
            {
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.BackQuote || e.keyCode == KeyCode.F1))
            {
                _open = !_open;
                _focusPending = _open;
                e.Use();
                return;
            }

            if (_monoStyle == null)
            {
                _monoStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
            }

            DrawHudLine();
            if (_open) DrawConsole(e);
        }

        void DrawHudLine()
        {
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            GUI.Label(new Rect(8f, Screen.height - 24f, 600f, 20f),
                "FPS " + fps.ToString("0") + "  |  Actors " + ActorRegistry.Count + "  |  ` = console", _monoStyle);
        }

        void DrawConsole(Event e)
        {
            float height = 24f + VisibleLines * 16f + 30f;
            GUI.Box(new Rect(0f, 0f, Screen.width, height), GUIContent.none);

            int start = Mathf.Max(0, _lines.Count - VisibleLines);
            for (int i = start; i < _lines.Count; i++)
                GUI.Label(new Rect(8f, 6f + (i - start) * 16f, Screen.width - 16f, 18f), _lines[i], _monoStyle);

            float inputY = height - 28f;

            if (e.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == InputControl)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    Submit();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow) { RecallHistory(-1); e.Use(); }
                else if (e.keyCode == KeyCode.DownArrow) { RecallHistory(1); e.Use(); }
            }

            GUI.SetNextControlName(InputControl);
            _input = GUI.TextField(new Rect(8f, inputY, Screen.width - 16f, 22f), _input);

            if (_focusPending && e.type == EventType.Repaint)
            {
                GUI.FocusControl(InputControl);
                _focusPending = false;
            }
        }

        void Submit()
        {
            string line = _input.Trim();
            _input = string.Empty;
            _historyIndex = -1;
            if (line.Length == 0) return;

            _history.Add(line);
            AddLine("> " + line);
            string result = _commands.Execute(line);
            if (result.Length > 0) AddLine(result);
        }

        void RecallHistory(int direction)
        {
            if (_history.Count == 0) return;
            if (_historyIndex < 0) _historyIndex = _history.Count;
            _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, _history.Count);
            _input = _historyIndex >= _history.Count ? string.Empty : _history[_historyIndex];
        }
    }
}
