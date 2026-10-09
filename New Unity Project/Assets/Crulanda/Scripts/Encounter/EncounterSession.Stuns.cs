using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// You can be stunned (no moving, no abilities, no swings) and disarmed (no swings, no weapon abilities) by a boss (D2,
    /// EncounterEnemy.Boss: Sorrel's stamp, the Foreman's terrify, Nix's Spanner-Lock). The target frame and a float say so.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public float PlayerStunnedUntil { get; private set; }
        public float PlayerDisarmedUntil { get; private set; }
        public bool PlayerStunned { get { return Time.time < PlayerStunnedUntil; } }
        public bool PlayerDisarmed { get { return Time.time < PlayerDisarmedUntil; } }
        public void StunPlayer(float seconds, string why)
        {
            if (Player == null || !Player.IsAlive || seconds <= 0) return;
            PlayerStunnedUntil = Mathf.Max(PlayerStunnedUntil, Time.time + seconds);
            FloatText(Player.transform.position + Vector3.up * .4f, "Stunned", new Color(1, .85f, .3f));
            Message((string.IsNullOrEmpty(why) ? "Stunned" : why) + ": you can't act for " + seconds.ToString("0.#") + " seconds.");
        }
        public void DisarmPlayer(float seconds, string why)
        {
            if (Player == null || !Player.IsAlive || seconds <= 0) return;
            PlayerDisarmedUntil = Mathf.Max(PlayerDisarmedUntil, Time.time + seconds);
            FloatText(Player.transform.position + Vector3.up * .4f, "Disarmed", new Color(1, .7f, .4f));
            Message((string.IsNullOrEmpty(why) ? "Disarmed" : why) + ": your weapon is locked for " + seconds.ToString("0.#") + " seconds.");
        }
        /// <summary>A weapon move (melee or shot) can't be made while disarmed; spells can.</summary>
        bool WeaponMove(int index) { var a = ActionAt(index); return a != null && a.castTime <= 0 && a.range <= 6 && a.power > 0; }
    }
}
