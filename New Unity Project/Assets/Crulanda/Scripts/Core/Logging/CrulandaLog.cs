using System;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>Subsystem a log message belongs to. Append only; do not reorder.</summary>
    public enum LogCategory
    {
        Core = 0, Data = 1, Actors = 2, Stats = 3, Combat = 4, Abilities = 5, Threat = 6, AI = 7,
        SimPlayers = 8, Items = 9, Inventory = 10, Quests = 11, Factions = 12, Groups = 13,
        World = 14, Persistence = 15, UI = 16, Economy = 17, DevTools = 18
    }

    public enum LogLevel { Verbose = 0, Info = 1, Warning = 2, Error = 3, Off = 4 }

    public struct LogEntry
    {
        public readonly DateTime TimeUtc;
        public readonly LogLevel Level;
        public readonly LogCategory Category;
        public readonly string Message;

        public LogEntry(DateTime timeUtc, LogLevel level, LogCategory category, string message)
        {
            TimeUtc = timeUtc; Level = level; Category = category; Message = message;
        }

        public override string ToString()
        {
            return "[" + Category + "] " + Message;
        }
    }

    /// <summary>
    /// Categorised logging with per-category minimum levels and a small in-memory ring buffer
    /// (used by the dev console). Main-thread only.
    /// </summary>
    public static class CrulandaLog
    {
        public const int RecentCapacity = 200;

        static readonly int CategoryCount = Enum.GetValues(typeof(LogCategory)).Length;
        static readonly LogLevel[] _minLevels = new LogLevel[CategoryCount];
        static readonly LogEntry[] _ring = new LogEntry[RecentCapacity];
        static int _ringStart;
        static int _ringCount;

        /// <summary>Replace to redirect output (tests). When null, output goes to UnityEngine.Debug.</summary>
        public static Action<LogEntry, UnityEngine.Object> Sink;

        /// <summary>Raised for every entry that passes the level filter.</summary>
        public static event Action<LogEntry> EntryLogged;

        static CrulandaLog()
        {
            ApplyDefaultLevels();
        }

        public static bool IsEnabled(LogCategory category, LogLevel level)
        {
            return level >= _minLevels[(int)category];
        }

        public static LogLevel GetLevel(LogCategory category)
        {
            return _minLevels[(int)category];
        }

        public static void SetLevel(LogCategory category, LogLevel level)
        {
            _minLevels[(int)category] = level;
        }

        public static void SetAllLevels(LogLevel level)
        {
            for (int i = 0; i < _minLevels.Length; i++) _minLevels[i] = level;
        }

        public static void Verbose(LogCategory category, string message, UnityEngine.Object context = null)
        {
            Write(LogLevel.Verbose, category, message, context);
        }

        public static void Info(LogCategory category, string message, UnityEngine.Object context = null)
        {
            Write(LogLevel.Info, category, message, context);
        }

        public static void Warn(LogCategory category, string message, UnityEngine.Object context = null)
        {
            Write(LogLevel.Warning, category, message, context);
        }

        public static void Error(LogCategory category, string message, UnityEngine.Object context = null)
        {
            Write(LogLevel.Error, category, message, context);
        }

        static void Write(LogLevel level, LogCategory category, string message, UnityEngine.Object context)
        {
            if (!IsEnabled(category, level)) return;

            var entry = new LogEntry(DateTime.UtcNow, level, category, message ?? string.Empty);
            Push(entry);

            if (Sink != null)
            {
                Sink(entry, context);
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Error: UnityEngine.Debug.LogError(entry.ToString(), context); break;
                    case LogLevel.Warning: UnityEngine.Debug.LogWarning(entry.ToString(), context); break;
                    default: UnityEngine.Debug.Log(entry.ToString(), context); break;
                }
            }

            var handler = EntryLogged;
            if (handler != null) handler(entry);
        }

        static void Push(LogEntry entry)
        {
            int index = (_ringStart + _ringCount) % RecentCapacity;
            if (_ringCount == RecentCapacity)
            {
                _ring[_ringStart] = entry;
                _ringStart = (_ringStart + 1) % RecentCapacity;
            }
            else
            {
                _ring[index] = entry;
                _ringCount++;
            }
        }

        /// <summary>Copies up to <paramref name="max"/> most recent entries (oldest first) into <paramref name="buffer"/>.</summary>
        public static void GetRecent(System.Collections.Generic.List<LogEntry> buffer, int max)
        {
            buffer.Clear();
            int take = Math.Min(max, _ringCount);
            int first = _ringCount - take;
            for (int i = 0; i < take; i++)
            {
                buffer.Add(_ring[(_ringStart + first + i) % RecentCapacity]);
            }
        }

        public static bool TryParseLevel(string text, out LogLevel level)
        {
            return Enum.TryParse(text, true, out level) && Enum.IsDefined(typeof(LogLevel), level);
        }

        public static bool TryParseCategory(string text, out LogCategory category)
        {
            return Enum.TryParse(text, true, out category) && Enum.IsDefined(typeof(LogCategory), category);
        }

        public static void ResetToDefaults()
        {
            ApplyDefaultLevels();
            _ringStart = 0;
            _ringCount = 0;
            Sink = null;
            EntryLogged = null;
        }

        static void ApplyDefaultLevels()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetAllLevels(LogLevel.Info);
#else
            SetAllLevels(LogLevel.Warning);
#endif
        }

        // Supports "Enter Play Mode without domain reload": statics must not leak between sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ResetToDefaults();
        }
    }
}
