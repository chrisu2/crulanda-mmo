using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Nix's two-part fight (dungeon step D5; DUNGEON_DESIGN.md 3a, "first the Rock-Eater, then Nix"): the Geode Floor's workshop camp
    /// spawns the Rock-Eater, a great burrowing beast that eats rock and geodes (ActorLook.RockEater; Chris, 2026-10-11: "NO Machines", it had
    /// been a boring-engine), as a second elite of Nix's camp, and Nix rides it in the saddle: seen, but out of reach and deaf to calls
    /// (EncounterEnemy.HideInside). The beast fights with its charge (EliteMoves "Rock-Crush", a knockback); its guards defend it as they
    /// would Nix. When it falls, Nix leaps down beside it and comes for whoever brought it down, with his Goad-Hook. The Amber Sigil and the
    /// camp's deed come from Nix alone (the beast's body rolls plain loot: its id names no camp). Both come back with the camp, Nix in the
    /// saddle again. All GAME-ONLY (the goblins are CANON).
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string RockEaterName = "The Rock-Eater", NixName = "Nix, the turncoat";

        /// <summary>The named elite of its camp (not a machine it rides, not a wave's): the one the key and the deed come from.</summary>
        bool NamedElite(EncounterEnemy e) { var camp = CampOf(e); return e != null && e.Camp && e.Elite && camp != null && camp.mob == e.MobName; }
        EncounterEnemy Machine(EncounterEnemy rider) { return Enemies.Find(e => e != null && e.Camp && e.MobName == RockEaterName && e.CampIndex == rider.CampIndex); }
        EncounterEnemy Rider(EncounterEnemy machine) { return Enemies.Find(e => e != null && e.Camp && e.MobName == NixName && e.CampIndex == machine.CampIndex); }

        /// <summary>Beside the camp's Nix as he spawns: his beast, and him into its saddle.</summary>
        void SpawnRockEater(EncounterEnemy nix, ZoneCamp camp, int campIndex, int level)
        {
            var at = nix.transform.position + nix.transform.right * 2.2f;
            if (!NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) return;
            string id = "mob.rockeater." + Zone.Zone.id.Replace("zone.", "") + ".m" + campIndex + ".0";   // LootContext reads no camp from "m21": plain loot, not Nix's pieces
            var a = SpawnActor(RockEaterName + " (elite)", content.enemy, hit.position + Vector3.up, Color.grey, id, ActorLook.RockEater, level);
            AddAgent(a.gameObject, 2.4f, .95f);
            var e = a.gameObject.AddComponent<EncounterEnemy>(); e.actor = a; e.persistentId = id; e.session = this;
            e.Camp = true; e.Elite = true; e.RespawnSeconds = nix.RespawnSeconds; e.CampCenter = camp.center; e.CampRadius = camp.radius;
            a.gameObject.SetActive(true); e.Initialize(); Enemies.Add(e);
            ConfigureSocial(e, camp, campIndex, level, false);   // Nix's camp: its guards answer it, it links with them
            e.MobName = RockEaterName; e.Cast = null;
            var move = EliteMoves.For(RockEaterName, false); e.ArmElite(move);
            a.Stats.SetBase(StatType.MaxHealth, Mathf.RoundToInt(EncounterEnemy.MobHealth(level, false, true, false) * move.health));
            a.Health.ApplyHealing(a.Health.Pool.Max);
            e.HitBase = EncounterEnemy.MobHit(level, false, true) * move.hit;
            nix.HideInside(e);
        }
        /// <summary>The duo (Chris, 2026-10-11: "the duo boss needs to be tougher. maybe perm CC one while killing one?"): the Rock-Eater
        /// engaged, Nix leaps down at once, marked for the party to hold. Free, he goads the beast: its blows half again and it mends 2% a
        /// second. Held (he can be, for the full time, as long as the party keeps him held), it is calm. Nix killed first, the beast rampages
        /// for the rest of the fight. So: hold Nix, kill the beast, then Nix.</summary>
        public const float GoadedBlows = 1.5f, GoadedMend = .005f;   // the mend .5% a second (2% made the 18:23 run a stalemate: 23 minutes on the beast, no kill)
        /// <summary>The duo's state for the run log: "Nix held 12s", "Nix free", "Nix down", "Nix in the saddle"; null when the beast is not up.</summary>
        public string DuoState
        {
            get
            {
                var m = Enemies.Find(e => e != null && e.Camp && e.MobName == RockEaterName && e.actor.IsAlive); if (m == null) return null;
                var r = Rider(m); if (r == null || !r.actor.IsAlive) return "Nix down, the beast " + (m.BlowScale > 1 ? "rampaging" : "calm");
                if (r.Inside == m) return "Nix in the saddle";
                return (SimCompanion.Held(r) ? "Nix held " + r.ControlSecondsLeft.ToString("0") + "s" : "Nix free") + ", the beast " + Mathf.RoundToInt(m.actor.Health.Pool.Ratio * 100) + "%" + (m.BlowScale > 1 ? " goaded" : "");
            }
        }
        float duoMendAt; bool duoRampaging;
        void TickDuo()
        {
            if (Zone == null || Zone.Zone.id != "zone.adit") return;
            var machine = Enemies.Find(e => e != null && e.Camp && e.MobName == RockEaterName && e.actor.IsAlive);
            if (machine == null) { duoRampaging = false; return; }
            var rider = Rider(machine);
            if (!machine.Engaged) { machine.BlowScale = 1; duoRampaging = false; return; }
            if (rider != null && rider.actor.IsAlive && rider.Inside == machine)
            {
                // The fight is on: Nix leaps down beside it, goad in hand, and the party is told whom to hold.
                var foe = machine.Victim != null ? machine.Victim.transform.position : Player.transform.position; var towards = foe - machine.transform.position; towards.y = 0;
                rider.Rehome(machine.transform.position + (towards.sqrMagnitude > .01f ? towards.normalized : Vector3.forward) * 2.2f);
                rider.Rise(machine.Victim ?? Player); rider.RaidMarked = RaidMark.Moon;
                Message(NixName + " vaults out of the saddle, goad in hand, and drives the Rock-Eater at you: \"Dig, girl! DIG!\" Hold him and the beast goes quiet.");
            }
            bool nixFree = rider != null && rider.actor.IsAlive && rider.Inside == null && !SimCompanion.Held(rider);
            bool nixDead = rider == null || !rider.actor.IsAlive;
            if (nixDead && !duoRampaging) { duoRampaging = true; Message("With Nix down nothing holds the Rock-Eater back: it rampages."); FloatText(machine.transform.position + Vector3.up, "Rampage!", new Color(1, .4f, .3f)); }
            machine.BlowScale = nixFree || nixDead ? GoadedBlows : 1;
            if (nixFree && Time.time >= duoMendAt) { duoMendAt = Time.time + 1; machine.actor.Health.ApplyHealing(Mathf.Max(1, Mathf.RoundToInt(machine.actor.Health.Pool.Max * GoadedMend))); }
        }
        /// <summary>Whether the duo's beast is goaded or rampaging now (its blows half again).</summary>
        public bool RockEaterDriven { get { var m = Enemies.Find(e => e != null && e.Camp && e.MobName == RockEaterName && e.actor.IsAlive); return m != null && m.BlowScale > 1; } }
        /// <summary>The Rock-Eater fell with its rider still up: the rider leaps down beside it and comes for whoever brought it down.</summary>
        void BreakOut(EncounterEnemy machine)
        {
            if (machine == null || machine.MobName != RockEaterName) return;
            var rider = Rider(machine); if (rider == null || !rider.actor.IsAlive || rider.Inside != machine) return;
            var killer = CombatActor(machine.TappedBy) ?? Player;
            var towards = killer != null ? killer.transform.position - machine.transform.position : machine.transform.forward; towards.y = 0;
            rider.Rehome(machine.transform.position + (towards.sqrMagnitude > .01f ? towards.normalized : Vector3.forward) * 1.8f);
            rider.Rise(killer);
            Message("The Rock-Eater bellows, staggers and comes down like a rockfall. " + NixName + " vaults out of the saddle with his goad in both hands: \"Do you know how long she took to RAISE?\"");
        }
        /// <summary>A camp mob came back: a rider finds its beast standing, or a beast its rider, and they pair up again.</summary>
        public void EnemyRespawned(EncounterEnemy e)
        {
            if (e == null || restoring) return;
            if (e.MobName == NixName) { var m = Machine(e); if (m != null && m.actor.IsAlive && !m.Engaged) e.HideInside(m); }
            else if (e.MobName == RockEaterName) { var r = Rider(e); if (r != null && r.actor.IsAlive && !r.Engaged && r.Inside == null) r.HideInside(e); }
        }
    }
}
