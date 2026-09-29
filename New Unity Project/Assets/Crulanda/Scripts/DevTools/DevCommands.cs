using System.Globalization;
using System.Text;
using Crulanda.Core;
using Crulanda.Data;
using Crulanda.Gameplay;

namespace Crulanda.DevTools
{
    /// <summary>Phase 0 built-in console commands. Later phases add their own (Sim.*, Combat.*, Quest.*...).</summary>
    public static class DevCommands
    {
        public static void RegisterAll(DebugCommandRegistry registry)
        {
            registry.Register("log.level", "Shows or sets minimum log levels.", LogLevelCommand, "log.level [category|all] [level]");
            registry.Register("actor.list", "Lists physically present actors.", ActorList, "actor.list");
            registry.Register("actor.info", "Shows details for one actor.", ActorInfo, "actor.info <name>");
            registry.Register("actor.damage", "Applies raw damage to an actor.", ActorDamage, "actor.damage <name> <amount>");
            registry.Register("actor.heal", "Applies raw healing to an actor.", ActorHeal, "actor.heal <name> <amount>");
            registry.Register("actor.revive", "Revives a dead actor.", ActorRevive, "actor.revive <name> [health]");
            registry.Register("content.list", "Lists registered content definitions.", ContentList, "content.list");
        }

        static string LogLevelCommand(string[] args)
        {
            if (args.Length == 0)
            {
                var sb = new StringBuilder("Log levels:");
                foreach (LogCategory c in System.Enum.GetValues(typeof(LogCategory)))
                    sb.Append("\n  ").Append(c).Append(" = ").Append(CrulandaLog.GetLevel(c));
                return sb.ToString();
            }

            if (args.Length != 2) return "Usage: log.level [category|all] [level]";

            LogLevel level;
            if (!CrulandaLog.TryParseLevel(args[1], out level))
                return "Unknown level '" + args[1] + "'. Use Verbose, Info, Warning, Error or Off.";

            if (string.Equals(args[0], "all", System.StringComparison.OrdinalIgnoreCase))
            {
                CrulandaLog.SetAllLevels(level);
                return "All categories set to " + level + ".";
            }

            LogCategory category;
            if (!CrulandaLog.TryParseCategory(args[0], out category))
                return "Unknown category '" + args[0] + "'.";

            CrulandaLog.SetLevel(category, level);
            return category + " set to " + level + ".";
        }

        static string ActorList(string[] args)
        {
            if (ActorRegistry.Count == 0) return "No actors.";

            var sb = new StringBuilder();
            for (int i = 0; i < ActorRegistry.Actors.Count; i++)
            {
                var a = ActorRegistry.Actors[i];
                if (i > 0) sb.Append('\n');
                sb.Append(Describe(a));
            }
            return sb.ToString();
        }

        static string ActorInfo(string[] args)
        {
            Actor a;
            string error = Resolve(args, 1, out a);
            if (error != null) return error;

            var sb = new StringBuilder(Describe(a));
            sb.Append("\n  id: ").Append(a.EntityId);
            sb.Append("\n  archetype: ").Append(a.Archetype != null ? a.Archetype.Id.ToString() : "(none)");
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                float v = a.Stats.Get(stat);
                if (v != 0f) sb.Append("\n  ").Append(stat).Append(": ").Append(v.ToString("0.##", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        static string ActorDamage(string[] args)
        {
            Actor a; int amount;
            string error = ResolveWithAmount(args, out a, out amount);
            if (error != null) return error;
            int applied = a.Health.ApplyDamage(amount);
            return a.DisplayName + " takes " + applied + " damage. " + Describe(a);
        }

        static string ActorHeal(string[] args)
        {
            Actor a; int amount;
            string error = ResolveWithAmount(args, out a, out amount);
            if (error != null) return error;
            int applied = a.Health.ApplyHealing(amount);
            return a.DisplayName + " heals " + applied + ". " + Describe(a);
        }

        static string ActorRevive(string[] args)
        {
            Actor a;
            string error = Resolve(args, 1, out a);
            if (error != null) return error;

            int health = a.Health.Pool.Max;
            if (args.Length > 1 && !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out health))
                return "Health must be a whole number.";

            a.Health.Revive(health);
            return Describe(a);
        }

        static string ContentList(string[] args)
        {
            ContentRegistry registry;
            if (!Services.TryGet(out registry)) return "No content registry (is GameBootstrap in the scene?).";

            var sb = new StringBuilder("Content (" + registry.Count + "):");
            foreach (var d in registry.All<DefinitionBase>())
                sb.Append("\n  ").Append(d.Id).Append("  [").Append(d.GetType().Name).Append(", ").Append(d.Canon).Append(']');
            return sb.ToString();
        }

        // ---- helpers ----

        static string Resolve(string[] args, int minArgs, out Actor actor)
        {
            actor = null;
            if (args.Length < minArgs) return "Missing arguments. Try 'help'.";
            actor = ActorRegistry.FindByName(args[0]);
            return actor == null ? "No unique actor matches '" + args[0] + "'. Try actor.list." : null;
        }

        static string ResolveWithAmount(string[] args, out Actor actor, out int amount)
        {
            amount = 0;
            string error = Resolve(args, 2, out actor);
            if (error != null) return error;
            if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out amount) || amount < 0)
                return "Amount must be a non-negative whole number.";
            return null;
        }

        static string Describe(Actor a)
        {
            return a.DisplayName + " L" + a.Level + " " + a.Disposition + " " + a.Classification +
                   "  HP " + a.Health.Pool.Current + "/" + a.Health.Pool.Max +
                   (a.Resource.Kind != ResourceKind.None
                        ? "  " + a.Resource.Kind + " " + a.Resource.Pool.Current + "/" + a.Resource.Pool.Max
                        : string.Empty) +
                   (a.IsAlive ? string.Empty : "  [DEAD]");
        }
    }
}
