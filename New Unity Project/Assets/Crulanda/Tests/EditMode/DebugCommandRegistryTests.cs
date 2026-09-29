using NUnit.Framework;
using Crulanda.DevTools;

namespace Crulanda.Tests
{
    public class DebugCommandRegistryTests
    {
        [Test]
        public void Tokenize_splits_on_whitespace_and_respects_quotes()
        {
            CollectionAssert.AreEqual(new[] { "actor.damage", "Test Wolf", "50" },
                DebugCommandRegistry.Tokenize("actor.damage \"Test Wolf\" 50"));
            CollectionAssert.AreEqual(new[] { "a", "b" }, DebugCommandRegistry.Tokenize("  a    b  "));
            Assert.AreEqual(0, DebugCommandRegistry.Tokenize("   ").Length);
            Assert.AreEqual(0, DebugCommandRegistry.Tokenize(null).Length);
        }

        [Test]
        public void Execute_passes_arguments_without_the_command_name()
        {
            var r = new DebugCommandRegistry();
            string[] seen = null;
            r.Register("echo", "echoes", args => { seen = args; return "ok"; });

            Assert.AreEqual("ok", r.Execute("echo one \"two words\""));
            CollectionAssert.AreEqual(new[] { "one", "two words" }, seen);
        }

        [Test]
        public void Command_names_are_case_insensitive()
        {
            var r = new DebugCommandRegistry();
            r.Register("Actor.List", "x", args => "listed");
            Assert.AreEqual("listed", r.Execute("actor.list"));
        }

        [Test]
        public void Unknown_command_returns_a_hint()
        {
            StringAssert.Contains("Unknown command", new DebugCommandRegistry().Execute("bogus"));
        }

        [Test]
        public void Handler_exceptions_become_error_text()
        {
            var r = new DebugCommandRegistry();
            r.Register("boom", "x", args => { throw new System.InvalidOperationException("bad"); });
            StringAssert.Contains("bad", r.Execute("boom"));
        }

        [Test]
        public void Blank_input_returns_empty()
        {
            Assert.AreEqual(string.Empty, new DebugCommandRegistry().Execute("   "));
        }

        [Test]
        public void Help_lists_commands_and_shows_usage()
        {
            var r = new DebugCommandRegistry();
            r.Register("foo.bar", "does a thing", args => null, "foo.bar <x>");
            StringAssert.Contains("foo.bar", r.Execute("help"));
            StringAssert.Contains("foo.bar <x>", r.Execute("help foo.bar"));
        }
    }
}
