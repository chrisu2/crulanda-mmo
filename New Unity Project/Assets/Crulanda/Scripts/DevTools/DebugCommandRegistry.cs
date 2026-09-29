using System;
using System.Collections.Generic;
using System.Text;

namespace Crulanda.DevTools
{
    /// <summary>Receives the arguments (command name excluded) and returns text to show, or null for none.</summary>
    public delegate string CommandHandler(string[] args);

    /// <summary>
    /// Name -> handler table behind the dev console. Pure C# (no Unity types) so it is unit-testable.
    /// Command names are dotted and case-insensitive, e.g. "actor.damage" (see brief section 54).
    /// </summary>
    public sealed class DebugCommandRegistry
    {
        sealed class Command
        {
            public string Name;
            public string Usage;
            public string Help;
            public CommandHandler Handler;
        }

        readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

        public DebugCommandRegistry()
        {
            Register("help", "Lists commands, or shows usage for one.", Help, "help [command]");
        }

        public void Register(string name, string help, CommandHandler handler, string usage = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name is required.");
            if (handler == null) throw new ArgumentNullException("handler");

            _commands[name] = new Command { Name = name, Help = help ?? string.Empty, Usage = usage ?? name, Handler = handler };
        }

        public bool Contains(string name)
        {
            return _commands.ContainsKey(name);
        }

        public string Execute(string line)
        {
            var tokens = Tokenize(line);
            if (tokens.Length == 0) return string.Empty;

            Command command;
            if (!_commands.TryGetValue(tokens[0], out command))
                return "Unknown command '" + tokens[0] + "'. Type 'help'.";

            var args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);

            try
            {
                return command.Handler(args) ?? string.Empty;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        string Help(string[] args)
        {
            if (args.Length > 0)
            {
                Command c;
                if (!_commands.TryGetValue(args[0], out c)) return "Unknown command '" + args[0] + "'.";
                return "Usage: " + c.Usage + "\n" + c.Help;
            }

            var names = new List<string>(_commands.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder("Commands:");
            foreach (var n in names) sb.Append("\n  ").Append(_commands[n].Usage).Append(" - ").Append(_commands[n].Help);
            return sb.ToString();
        }

        /// <summary>Splits on whitespace; double quotes group words ("Test Guard" is one token).</summary>
        public static string[] Tokenize(string line)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(line)) return tokens.ToArray();

            var current = new StringBuilder();
            bool inQuotes = false;
            bool hasToken = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true; // "" yields an empty token
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (hasToken) { tokens.Add(current.ToString()); current.Length = 0; hasToken = false; }
                }
                else
                {
                    current.Append(c);
                    hasToken = true;
                }
            }
            if (hasToken) tokens.Add(current.ToString());
            return tokens.ToArray();
        }
    }
}
