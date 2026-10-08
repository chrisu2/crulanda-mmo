using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Crulanda.Encounter
{
    /// <summary>The game's rebindable keys (playtest note 75, 2026-10-07).</summary>
    public enum GameKey { Forward, Back, Left, Right, Interact, Jump, Run, Target, Map, Quests, Talents, Bags, Character, Trades, Who, Recover, Hail }

    /// <summary>
    /// Key bindings (playtest note 75: "I hate WASD for movement, I prefer ESDF and G for interact"): every game key bound to a keyboard
    /// key, saved in PlayerPrefs (keys.&lt;GameKey&gt;), with the WASD and ESDF presets and a rebind from the pause menu's Controls
    /// window (EncounterHud.Controls). The code still asks EncounterInput.Press(KeyCode.E) and the like: each of those KeyCodes is
    /// the default of one game key, and is answered by whatever key that game key is bound to now.
    /// </summary>
    public static class KeyBindings
    {
        public static readonly (GameKey key, KeyCode code, string label)[] All = {
            (GameKey.Forward, KeyCode.W, "Move forward"), (GameKey.Back, KeyCode.S, "Move back"), (GameKey.Left, KeyCode.A, "Move left"), (GameKey.Right, KeyCode.D, "Move right"),
            (GameKey.Interact, KeyCode.E, "Interact, talk, loot, travel"), (GameKey.Jump, KeyCode.Space, "Jump"), (GameKey.Run, KeyCode.Slash, "Walk / run"),
            (GameKey.Target, KeyCode.Tab, "Next target"), (GameKey.Map, KeyCode.M, "Map"), (GameKey.Quests, KeyCode.L, "Quest book"), (GameKey.Talents, KeyCode.B, "Talents"),
            (GameKey.Bags, KeyCode.I, "Bags"), (GameKey.Character, KeyCode.C, "Character"), (GameKey.Trades, KeyCode.K, "Trades"), (GameKey.Who, KeyCode.O, "Who list"),
            (GameKey.Recover, KeyCode.R, "Recover (when beaten)"), (GameKey.Hail, KeyCode.H, "Hail (greet who you face)"),
        };
#if ENABLE_INPUT_SYSTEM
        static readonly Dictionary<GameKey, Key> Defaults = new Dictionary<GameKey, Key> {
            { GameKey.Forward, Key.W }, { GameKey.Back, Key.S }, { GameKey.Left, Key.A }, { GameKey.Right, Key.D }, { GameKey.Interact, Key.E }, { GameKey.Jump, Key.Space },
            { GameKey.Run, Key.Slash }, { GameKey.Target, Key.Tab }, { GameKey.Map, Key.M }, { GameKey.Quests, Key.L }, { GameKey.Talents, Key.B }, { GameKey.Bags, Key.I },
            { GameKey.Character, Key.C }, { GameKey.Trades, Key.K }, { GameKey.Who, Key.O }, { GameKey.Recover, Key.R }, { GameKey.Hail, Key.H },
        };
        static Dictionary<GameKey, Key> bound;
        static Dictionary<KeyCode, GameKey> byCode;
        static void Load()
        {
            if (bound != null) return;
            bound = new Dictionary<GameKey, Key>(Defaults); byCode = new Dictionary<KeyCode, GameKey>();
            foreach (var a in All) byCode[a.code] = a.key;
            foreach (var a in All)
            {
                string saved = null; try { saved = PlayerPrefs.GetString("keys." + a.key, null); } catch { }
                if (!string.IsNullOrEmpty(saved) && System.Enum.TryParse<Key>(saved, out var k) && k != Key.None) bound[a.key] = k;
            }
        }
        public static Key KeyOf(GameKey g) { Load(); return bound[g]; }
        /// <summary>The game key a default KeyCode stands for (KeyCode.E is Interact), if any.</summary>
        public static bool Stands(KeyCode code, out GameKey g) { Load(); return byCode.TryGetValue(code, out g); }
        /// <summary>Binds a key; the game key that had it takes the old one (no key does two things).</summary>
        public static void Bind(GameKey g, Key k)
        {
            Load(); if (k == Key.None || k == Key.Escape) return;
            var old = bound[g];
            foreach (var a in All) if (a.key != g && bound[a.key] == k) { bound[a.key] = old; Save(a.key); }
            bound[g] = k; Save(g);
        }
        static void Save(GameKey g) { try { PlayerPrefs.SetString("keys." + g, bound[g].ToString()); PlayerPrefs.Save(); } catch { } }
        /// <summary>The presets: classic WASD (E interacts), or ESDF with G to interact (note 75); everything else at its default.</summary>
        public static void Preset(bool esdf)
        {
            Load(); foreach (var d in Defaults) bound[d.Key] = d.Value;
            if (esdf) { bound[GameKey.Forward] = Key.E; bound[GameKey.Left] = Key.S; bound[GameKey.Back] = Key.D; bound[GameKey.Right] = Key.F; bound[GameKey.Interact] = Key.G; }
            foreach (var a in All) Save(a.key);
        }
        public static bool Held(GameKey g) { var kb = Keyboard.current; return kb != null && kb[KeyOf(g)].isPressed; }
        public static bool Pressed(GameKey g)
        {
            var kb = Keyboard.current; if (kb == null) return false; var k = KeyOf(g);
            return kb[k].wasPressedThisFrame || (k == Key.Slash && kb.numpadDivideKey.wasPressedThisFrame);
        }
        /// <summary>How a key reads on screen: "E", "Space", "/".</summary>
        public static string Label(GameKey g)
        {
            var k = KeyOf(g);
            switch (k) { case Key.Slash: return "/"; case Key.Space: return "Space"; case Key.Tab: return "Tab"; case Key.Backquote: return "`"; }
            var s = k.ToString(); if (s.StartsWith("Digit")) s = s.Substring(5); if (s.StartsWith("Numpad")) s = "Num " + s.Substring(6); return s;
        }
        /// <summary>The key pressed this frame, if one (for rebinding), Escape included so it can cancel.</summary>
        public static Key PressedNow()
        {
            var kb = Keyboard.current; if (kb == null) return Key.None;
            foreach (var c in kb.allKeys) if (c != null && c.wasPressedThisFrame) return c.keyCode;
            return Key.None;
        }
#else
        public static bool Stands(KeyCode code, out GameKey g) { g = GameKey.Interact; return false; }
        public static string Label(GameKey g) { foreach (var a in All) if (a.key == g) return a.code.ToString(); return "?"; }
        public static void Preset(bool esdf) { }
#endif
        /// <summary>The four movement keys as one word ("WASD", "ESDF"), for the help line.</summary>
        public static string MoveLabel { get { return Label(GameKey.Forward) + Label(GameKey.Left) + Label(GameKey.Back) + Label(GameKey.Right); } }
        public static string InteractLabel { get { return Label(GameKey.Interact); } }
    }
}
