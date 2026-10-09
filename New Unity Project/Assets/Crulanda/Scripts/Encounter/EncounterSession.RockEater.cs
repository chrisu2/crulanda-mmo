using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Nix's two-part fight (dungeon step D5; DUNGEON_DESIGN.md 3a, "first the Rock-Eater, then Nix"): the Geode Floor's workshop camp
    /// spawns the Rock-Eater, a goblin boring-engine (ActorLook.RockEater), as a second elite of Nix's camp, and Nix rides inside it: hidden,
    /// unseen, out of reach and deaf to calls (EncounterEnemy.HideInside). The machine fights with its drill (EliteMoves "Bore", a
    /// knockback); its guards defend it as they would Nix. When it breaks, Nix jumps out at the wreck and comes for whoever broke it, with
    /// his Spanner-Lock. The Amber Sigil and the camp's deed come from Nix alone (the machine's corpse rolls plain loot: its id names no
    /// camp). Both come back with the camp, Nix inside again. All GAME-ONLY (the goblins are CANON).
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string RockEaterName = "The Rock-Eater", NixName = "Nix, the turncoat";

        /// <summary>The named elite of its camp (not a machine it rides, not a wave's): the one the key and the deed come from.</summary>
        bool NamedElite(EncounterEnemy e) { var camp = CampOf(e); return e != null && e.Camp && e.Elite && camp != null && camp.mob == e.MobName; }
        EncounterEnemy Machine(EncounterEnemy rider) { return Enemies.Find(e => e != null && e.Camp && e.MobName == RockEaterName && e.CampIndex == rider.CampIndex); }
        EncounterEnemy Rider(EncounterEnemy machine) { return Enemies.Find(e => e != null && e.Camp && e.MobName == NixName && e.CampIndex == machine.CampIndex); }

        /// <summary>Beside the camp's Nix as he spawns: his engine, and him into its seat.</summary>
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
        /// <summary>The Rock-Eater broke: its rider jumps out at the wreck and comes for whoever broke it.</summary>
        void BreakOut(EncounterEnemy machine)
        {
            if (machine == null || machine.MobName != RockEaterName) return;
            var rider = Rider(machine); if (rider == null || !rider.actor.IsAlive || rider.Inside != machine) return;
            var killer = CombatActor(machine.TappedBy) ?? Player;
            var towards = killer != null ? killer.transform.position - machine.transform.position : machine.transform.forward; towards.y = 0;
            rider.Rehome(machine.transform.position + (towards.sqrMagnitude > .01f ? towards.normalized : Vector3.forward) * 1.8f);
            rider.Rise(killer);
            Message("The Rock-Eater shudders, screams and stops. The canopy bangs open and " + NixName + " drops out of the seat with a spanner in both hands: \"Do you know what that COST?\"");
        }
        /// <summary>A camp mob came back: a rider finds its machine standing, or a machine its rider, and they pair up again.</summary>
        public void EnemyRespawned(EncounterEnemy e)
        {
            if (e == null || restoring) return;
            if (e.MobName == NixName) { var m = Machine(e); if (m != null && m.actor.IsAlive && !m.Engaged) e.HideInside(m); }
            else if (e.MobName == RockEaterName) { var r = Rider(e); if (r != null && r.actor.IsAlive && !r.Engaged && r.Inside == null) r.HideInside(e); }
        }
    }
}
