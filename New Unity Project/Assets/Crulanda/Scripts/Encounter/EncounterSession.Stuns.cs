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
        // Holds a boss puts on the party (2026-10-11; EliteMove.cc): rooted (no moving), feared (you run from it; no abilities), disoriented
        // (your steps go the wrong way; no abilities). Stunned is above. SimCompanion.HoldUntil holds a sim the same way.
        public float PlayerRootedUntil { get; private set; } public float PlayerFearedUntil { get; private set; } public float PlayerDisorientedUntil { get; private set; }
        public Vector3 PlayerFearFrom { get; private set; }
        public bool PlayerRooted { get { return Time.time < PlayerRootedUntil; } }
        public bool PlayerFeared { get { return Time.time < PlayerFearedUntil; } }
        public bool PlayerDisoriented { get { return Time.time < PlayerDisorientedUntil; } }
        /// <summary>Held so that no ability can be used: stunned, feared or disoriented.</summary>
        public bool PlayerHeld { get { return PlayerStunned || PlayerFeared || PlayerDisoriented; } }
        /// <summary>What holds you, for the frame ("STUNNED 1.2s"), or null.</summary>
        public string PlayerHoldLabel
        {
            get
            {
                float now = Time.time;
                if (PlayerStunned) return "STUNNED " + (PlayerStunnedUntil - now).ToString("0.0") + "s"; if (PlayerFeared) return "FLEEING " + (PlayerFearedUntil - now).ToString("0.0") + "s";
                if (PlayerRooted) return "ROOTED " + (PlayerRootedUntil - now).ToString("0.0") + "s"; if (PlayerDisoriented) return "DISORIENTED " + (PlayerDisorientedUntil - now).ToString("0.0") + "s";
                return null;
            }
        }
        public void HoldPlayer(string kind, float seconds, Vector3 from, string why)
        {
            if (Player == null || !Player.IsAlive || seconds <= 0) return;
            float until = Time.time + seconds; string word;
            switch (kind)
            {
                case "stun": StunPlayer(seconds, why); return;
                case "root": PlayerRootedUntil = Mathf.Max(PlayerRootedUntil, until); word = "Rooted"; break;
                case "fear": PlayerFearedUntil = Mathf.Max(PlayerFearedUntil, until); PlayerFearFrom = from; word = "Fleeing"; break;
                case "disorient": PlayerDisorientedUntil = Mathf.Max(PlayerDisorientedUntil, until); word = "Disoriented"; break;
                default: return;
            }
            FloatText(Player.transform.position + Vector3.up * .4f, word, new Color(1, .85f, .3f));
            Message((string.IsNullOrEmpty(why) ? word : why) + ": " + (kind == "root" ? "you can't move" : kind == "fear" ? "you run" : "your steps go wrong") + " for " + seconds.ToString("0.#") + " seconds.");
        }
        /// <summary>A weapon move (melee or shot) can't be made while disarmed; spells can.</summary>
        bool WeaponMove(int index) { var a = ActionAt(index); return a != null && a.castTime <= 0 && a.range <= 6 && a.power > 0; }
    }
}
