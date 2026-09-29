using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Combat
{
    [RequireComponent(typeof(Actor))]
    [DisallowMultipleComponent]
    public sealed class DerivedStatsController : MonoBehaviour
    {
        Actor actor;
        StatBlock bound;
        DerivedStatRules rules;
        int weaponBonus;
        public void Configure(DerivedStatRules definition, int equipmentBonus)
        {
            rules = definition; weaponBonus = equipmentBonus;
            actor = GetComponent<Actor>();
            Attach();
        }
        public void SetEquipmentBonus(int bonus) { weaponBonus = bonus; Recalculate(); }
        void OnEnable() { if (rules != null) Attach(); }
        void Attach()
        {
            Detach();
            actor.Rebuilt += Rebind; actor.LevelChanged += LevelChanged;
            bound = actor.Stats; bound.Changed += OnStatChanged;
            Recalculate();
        }
        void Rebind(Actor owner) { Attach(); }
        void LevelChanged(int level) { Recalculate(); }
        void OnStatChanged(StatType stat)
        {
            if (stat == StatType.Stamina || stat == StatType.Strength || stat == StatType.Intellect || stat == StatType.Agility) Recalculate();
        }
        void Recalculate()
        {
            if (bound == null || rules == null) return;
            var values = DerivedStatCalculator.Calculate(rules, actor.Level, bound.Get(StatType.Stamina),
                bound.Get(StatType.Strength), bound.Get(StatType.Intellect), bound.Get(StatType.Agility), weaponBonus);
            Set(StatType.MaxHealth, values.health); Set(StatType.AttackPower, values.attackPower);
            Set(StatType.SpellPower, values.spellPower); Set(StatType.Armor, values.armor);
        }
        void Set(StatType stat, int value) { if (bound.GetBase(stat) != value) bound.SetBase(stat, value); }
        void OnDisable() { Detach(); }
        void Detach()
        {
            if (actor != null) { actor.Rebuilt -= Rebind; actor.LevelChanged -= LevelChanged; }
            if (bound != null) bound.Changed -= OnStatChanged;
            bound = null;
        }
    }
}
