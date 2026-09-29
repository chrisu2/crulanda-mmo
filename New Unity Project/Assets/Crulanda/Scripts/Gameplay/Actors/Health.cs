using System;
using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// Holds an actor's health pool and dead/alive state. It applies raw amounts only; damage
    /// calculation (mitigation, crits, threat) belongs to combat systems added in Phase 1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        public VitalPool Pool { get; private set; }
        public Actor Owner { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>Raised once when health first reaches zero.</summary>
        public event Action<Health> Died;

        void Awake()
        {
            if (Pool == null) Pool = new VitalPool(1, true);
        }

        internal void Bind(Actor owner, int maxHealth)
        {
            if (Pool == null) Pool = new VitalPool(1, true);
            Owner = owner;
            IsDead = false;
            Pool.SetMax(maxHealth, false);
            Pool.Fill();
        }

        /// <summary>Removes health. Returns the amount actually removed (0 if already dead).</summary>
        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount", "Use ApplyHealing for negative values.");
            if (IsDead || amount == 0) return 0;

            int applied = -Pool.Change(-amount);
            if (Pool.IsEmpty) Die();
            return applied;
        }

        /// <summary>Adds health. Returns the amount actually restored (0 if dead; use Revive first).</summary>
        public int ApplyHealing(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount", "Use ApplyDamage for negative values.");
            if (IsDead || amount == 0) return 0;
            return Pool.Change(amount);
        }

        public void SetMaximum(int maximum)
        {
            Pool.SetMax(Math.Max(1, maximum), true);
            if (!IsDead && Pool.IsEmpty) Die();
        }

        public void Revive(int health)
        {
            if (!IsDead) return;
            IsDead = false;
            Pool.SetCurrent(Math.Max(1, health));
            CrulandaLog.Verbose(LogCategory.Actors, (Owner != null ? Owner.DisplayName : name) + " revived.", this);
        }

        void Die()
        {
            IsDead = true;
            CrulandaLog.Info(LogCategory.Actors, (Owner != null ? Owner.DisplayName : name) + " died.", this);

            var handler = Died;
            if (handler != null) handler(this);
            if (Owner != null) EventBus.Publish(new ActorDiedEvent(Owner));
        }
    }
}
