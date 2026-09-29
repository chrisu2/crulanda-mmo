using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Combat
{
    [RequireComponent(typeof(Actor))]
    [DisallowMultipleComponent]
    public sealed class Combatant : MonoBehaviour
    {
        Actor actor;
        float barrierUntil;
        public StatusEffectRuntime Statuses { get; private set; }
        /// <summary>Remaining absorb shield. A new barrier replaces a weaker one; it never stacks.</summary>
        public int Barrier { get; private set; }
        void Awake() { actor = GetComponent<Actor>(); }
        void OnEnable()
        {
            actor.Rebuilt += OnRebuilt;
            actor.Health.Died += OnDeath;
            OnRebuilt(actor);
        }
        void OnRebuilt(Actor owner)
        {
            Statuses?.Clear(); Statuses = new StatusEffectRuntime(owner.Stats); ClearBarrier();
        }
        void Update() { Statuses?.Tick(Time.time); if (Barrier > 0 && Time.time >= barrierUntil) ClearBarrier(); }
        void OnDeath(Health health) { Statuses?.Clear(); ClearBarrier(); }
        void OnDisable()
        {
            if (actor != null) { actor.Rebuilt -= OnRebuilt; actor.Health.Died -= OnDeath; }
            Statuses?.Clear(); ClearBarrier();
        }
        public void ClearBarrier() { Barrier = 0; barrierUntil = 0; }
        public bool AddBarrier(int amount, float duration)
        {
            if (actor == null || !actor.IsAlive || amount <= 0 || !(duration > 0) || float.IsInfinity(duration)) return false;
            if (Time.time < barrierUntil && Barrier >= amount) { barrierUntil = Mathf.Max(barrierUntil, Time.time + duration); return true; }
            Barrier = amount; barrierUntil = Time.time + duration; return true;
        }
        /// <summary>Applies armor and statuses, then drains any barrier. Returns health actually lost.</summary>
        public int Damage(int amount)
        {
            Statuses.Tick(Time.time);
            int mitigated = CombatMath.Damage(amount, actor.Stats.Get(StatType.Armor), Statuses.IncomingDamageMultiplier);
            if (Barrier > 0 && Time.time < barrierUntil)
            {
                int absorbed = Mathf.Min(Barrier, mitigated);
                Barrier -= absorbed; mitigated -= absorbed;
                if (Barrier == 0) barrierUntil = 0;
            }
            return mitigated > 0 ? actor.Health.ApplyDamage(mitigated) : 0;
        }
        public int Heal(int amount) { return actor.Health.ApplyHealing(amount); }
        public bool Apply(StatusEffectDefinition definition)
        {
            Statuses.Tick(Time.time);
            return actor.IsAlive && Statuses.Apply(definition, Time.time);
        }
    }
}
