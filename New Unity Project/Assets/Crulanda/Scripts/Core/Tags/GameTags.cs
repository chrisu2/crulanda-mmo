using System;
using System.Collections.Generic;
using System.Reflection;

namespace Crulanda.Core
{
    /// <summary>
    /// The canonical list of gameplay tags. Add new tags here (and in Docs/GAMEPLAY_TAGS.md).
    /// Nested static classes give IntelliSense-friendly access: GameTags.State.Dead.
    /// Never rename a released tag: saves and data assets store the path string.
    /// </summary>
    public static class GameTags
    {
        public static class Ability
        {
            public static readonly GameTag Attack = new GameTag("Ability.Attack");
            public static readonly GameTag Spell = new GameTag("Ability.Spell");
            public static readonly GameTag Heal = new GameTag("Ability.Heal");
            public static readonly GameTag Utility = new GameTag("Ability.Utility");
            public static readonly GameTag CrowdControl = new GameTag("Ability.CrowdControl");
        }

        public static class State
        {
            public static readonly GameTag Combat = new GameTag("State.Combat");
            public static readonly GameTag Dead = new GameTag("State.Dead");
            public static readonly GameTag Stunned = new GameTag("State.Stunned");
            public static readonly GameTag Silenced = new GameTag("State.Silenced");
            public static readonly GameTag Rooted = new GameTag("State.Rooted");
            public static readonly GameTag Casting = new GameTag("State.Casting");
        }

        public static class Damage
        {
            public static readonly GameTag Physical = new GameTag("Damage.Physical");
            public static readonly GameTag Fire = new GameTag("Damage.Fire");
            public static readonly GameTag Frost = new GameTag("Damage.Frost");
            public static readonly GameTag Nature = new GameTag("Damage.Nature");
            public static readonly GameTag Shadow = new GameTag("Damage.Shadow");
            public static readonly GameTag Holy = new GameTag("Damage.Holy");
        }

        // Prototype classes only. PROVISIONAL: the final roster comes from Crulanda canon.
        public static class Class
        {
            public static readonly GameTag Warrior = new GameTag("Class.Warrior");
            public static readonly GameTag Cleric = new GameTag("Class.Cleric");
            public static readonly GameTag Ranger = new GameTag("Class.Ranger");
            public static readonly GameTag Mage = new GameTag("Class.Mage");
        }

        public static class Role
        {
            public static readonly GameTag Tank = new GameTag("Role.Tank");
            public static readonly GameTag Healer = new GameTag("Role.Healer");
            public static readonly GameTag Damage = new GameTag("Role.Damage");
            public static readonly GameTag Support = new GameTag("Role.Support");
        }

        static List<GameTag> _all;
        static HashSet<GameTag> _allSet;

        /// <summary>Every tag declared above (found by reflection; intended for validation/tools).</summary>
        public static IReadOnlyList<GameTag> All
        {
            get
            {
                EnsureBuilt();
                return _all;
            }
        }

        public static bool IsKnown(GameTag tag)
        {
            EnsureBuilt();
            return _allSet.Contains(tag);
        }

        static void EnsureBuilt()
        {
            if (_all != null) return;

            var list = new List<GameTag>();
            foreach (var nested in typeof(GameTags).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
            {
                foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(GameTag))
                        list.Add((GameTag)field.GetValue(null));
                }
            }
            _all = list;
            _allSet = new HashSet<GameTag>(list);
        }
    }
}
