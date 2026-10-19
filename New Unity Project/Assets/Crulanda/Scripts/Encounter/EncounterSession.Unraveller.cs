using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The world boss in play (Docs/WORLD_BOSS_1.md 1.3-1.4, build step 3; the clock is WorldBossClock, her rank EncounterEnemy.Raid, the
    /// reach ZoneBuilder.DrainReach):
    /// - The clock runs on play time in every zone (not while paused) and is saved in the world slot (WorldSave.unraveller, format 3).
    /// - Five minutes before she rises, in the Lowtowns: the System line, the Drain Reach's water stilled and grey, the Drain Courts' lamps out,
    ///   Tibb Marrow and Ketta Rune calling in Zone chat and standing by their salt barrel and harpoon rack on the apron (talk to them to buy),
    ///   the villagers within 60 m gone indoors, and the sims' chatter.
    /// - She rises out of the river at the silt bar (a placeholder: the Weave-Eater's look scaled up, the Weave-Eaters' book rules, no phases
    ///   yet; steps 4-5), the System line in every zone. She stays 45 minutes, or until killed, or until she has wiped the raid twice; then she
    ///   sinks, and the reach is as it was. A kill or her sinking starts the next 3 hours.
    /// Coming into the Lowtowns while she is warned or up finds the reach as the clock says (she is there, risen).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The Unraveller in this zone now (null when she is not), alive, rising, sinking or dead.</summary>
        public EncounterEnemy UnravellerFoe { get; private set; }
        /// <summary>The world's timer for her (null with no world loaded).</summary>
        public WorldBossState WorldBossNow { get { return SimPopulation.Active != null && SimPopulation.Active.World != null ? SimPopulation.Active.World.unraveller : null; } }
        int WorldSeed { get { return SimPopulation.Active != null && SimPopulation.Active.World != null ? SimPopulation.Active.World.seed : 0; } }
        bool InDrainReachZone { get { return ZoneId == Unraveller.Zone && Zone != null && Zone.SiltBar != null; } }
        /// <summary>Rising out of the river (true) or sinking into it (false), from this time; -1 when neither.</summary>
        float surfaceFrom = -1; bool surfacingUp; bool unravellerOnParty; bool reachWarnedHere;
        readonly List<(Villager v, Vector3 at)> sentIndoors = new List<(Villager, Vector3)>();
        readonly List<(Villager v, Vector3 at)> stockKeepers = new List<(Villager, Vector3)>();
        /// <summary>She is rising out of the river now.</summary>
        public bool UnravellerRising { get { return UnravellerFoe != null && surfaceFrom >= 0 && surfacingUp; } }
        /// <summary>She is sinking back into the river now.</summary>
        public bool UnravellerSinking { get { return UnravellerFoe != null && surfaceFrom >= 0 && !surfacingUp; } }

        /// <summary>Each frame of play (not paused): the clock, what it brings, and the zone made to match it.</summary>
        /// <summary>--crulanda-unraveller-now (the group run at 10, and tests of her in a build): in the Lowtowns, the clock is set so the warning
        /// comes at once and she rises 30 s after. Once a run.</summary>
        public const string NowFlag = "--crulanda-unraveller-now";
        static bool nowDone;
        public static bool NowRequested { get { return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), NowFlag) >= 0; } }
        /// <summary>Brings her rising to within <paramref name="seconds"/> (the warning at once), if she is not warned or up already.</summary>
        public void UnravellerSoon(float seconds)
        {
            var s = WorldBossNow; if (s == null || s.state != WorldBossState.Waiting) return;
            if (s.riseAt < 0) s.riseAt = WorldBossClock.RollRise(WorldSeed, s.cycles);
            s.played = Mathf.Max(s.played, s.riseAt - Mathf.Max(.1f, seconds));
        }
        void TickUnraveller()
        {
            TickRaidFeast();   // the raid's stewpot (EncounterSession.UnravellerFight)
            var s = WorldBossNow; if (s == null) return;
            if (!nowDone && ZoneId == Unraveller.Zone && NowRequested) { nowDone = true; UnravellerSoon(30); }
            var beat = WorldBossClock.Tick(s, Mathf.Min(Time.deltaTime, .5f), WorldSeed);
            if (beat == WorldBossClock.Beat.Warned) UnravellerWarned();
            else if (beat == WorldBossClock.Beat.Rose) UnravellerRose();
            else if (beat == WorldBossClock.Beat.Sank) UnravellerEnded(Unraveller.SunkSystem);
            MatchReach(s);
            TickUnravellerFoe(s);
        }
        /// <summary>The warning (five minutes before), said in the Lowtowns only.</summary>
        void UnravellerWarned()
        {
            if (ZoneId != Unraveller.Zone) return;
            Message(Unraveller.WarningSystem);
            ChatSay(ChatChannel.Zone, Unraveller.Tibb, Unraveller.TibbCall);
            ChatSay(ChatChannel.Zone, Unraveller.Ketta, Unraveller.KettaCall);
            SimChatter.Active?.UnravellerComing();
        }
        /// <summary>She rises: the line in every zone; in the Lowtowns, out of the river at the bar.</summary>
        void UnravellerRose()
        {
            Message(Unraveller.RisenSystem);
            if (InDrainReachZone) SpawnUnraveller(true);
        }
        /// <summary>Her rising over unbeaten (the 45 minutes, or two wipes): the line in every zone, and she sinks.</summary>
        void UnravellerEnded(string line)
        {
            Message(line);
            if (UnravellerFoe != null && UnravellerFoe.actor.IsAlive) SinkUnraveller();
        }

        /// <summary>The zone as the clock says: the reach warned (still water, lamps out, the stock and its keepers out, the near villagers in)
        /// while she is warned or up, and her here while she is up; all as it was otherwise.</summary>
        void MatchReach(WorldBossState s)
        {
            if (!InDrainReachZone) return;
            bool warned = s.state == WorldBossState.Warned || s.state == WorldBossState.Up;
            if (warned != reachWarnedHere) SetReach(warned);
            if (s.state == WorldBossState.Up && (UnravellerFoe == null || (!UnravellerFoe.actor.IsAlive && !UnravellerSinking))) SpawnUnraveller(false);
            if (s.state != WorldBossState.Up && UnravellerFoe != null && UnravellerFoe.actor.IsAlive && !UnravellerSinking) SinkUnraveller();
        }
        void SetReach(bool warned)
        {
            Zone.SetReachWarned(warned); Zone.SetReachStock(warned);
            var life = VillageLife.Active;
            if (warned && life == null) return;   // the village not up yet (the zone's first frames): again next frame
            reachWarnedHere = warned;
            if (warned)
            {
                var bar = Zone.SiltBar.center;
                // Tibb and Ketta bring their stock to the apron (talk to them there: their own shops).
                foreach (var (name, at, toward) in new[] { (Unraveller.Tibb, Zone.ReachTibbAt, Zone.ReachBarrelAt), (Unraveller.Ketta, Zone.ReachKettaAt, Zone.ReachRackAt) })
                {
                    var v = life.Find(name); if (v == null || at == Vector3.zero) continue;
                    stockKeepers.Add((v, v.transform.position));
                    var face = toward - at; face.y = 0;
                    v.StandAt(NavMesh.SamplePosition(at, out var hit, 2, NavMesh.AllAreas) ? hit.position : at, face.sqrMagnitude > .01f ? Quaternion.LookRotation(face).eulerAngles.y : 0);
                }
                // The villagers near the river go indoors.
                foreach (var v in life.Villagers)
                {
                    if (v == null || !v.Visible || stockKeepers.Exists(k => k.v == v)) continue;
                    var p = v.transform.position; if (Vector2.Distance(new Vector2(p.x, p.z), bar) > Unraveller.IndoorsRange) continue;
                    if (v.GoIndoors()) sentIndoors.Add((v, p));
                }
            }
            else
            {
                foreach (var (v, at) in stockKeepers) if (v != null) v.Release(at);
                foreach (var (v, at) in sentIndoors) if (v != null) v.Release(v.transform.position);
                stockKeepers.Clear(); sentIndoors.Clear();
            }
        }

        /// <summary>Her, at the silt bar: the Weave-Eater's look three times a man's height, level 30, a raid boss for ten (no group scaling,
        /// the +5 clamp), her health and swing the placeholder numbers (Unraveller). <paramref name="rising"/>: up out of the river over
        /// Unraveller.RiseSeconds; else standing there already (you came into the zone while she was up).</summary>
        public EncounterEnemy SpawnUnraveller(bool rising)
        {
            var bar = Zone != null ? Zone.SiltBar : null; if (bar == null) return null;
            if (UnravellerFoe != null) { RemoveUnraveller(); }
            var want = Zone.Ground(bar.center);
            if (!NavMesh.SamplePosition(want, out var hit, 4, NavMesh.AllAreas)) { Debug.LogWarning("The Unraveller: no navmesh on the silt bar at " + want); return null; }
            var a = SpawnActor(Unraveller.Name, content.enemy, hit.position + Vector3.up, Color.grey, Unraveller.Id, ActorLook.WeaveEater, Unraveller.Level);
            AddAgent(a.gameObject, 3.4f, .45f);
            var e = a.gameObject.AddComponent<EncounterEnemy>(); e.actor = a; e.persistentId = Unraveller.Id; e.session = this;
            e.Camp = true; e.Elite = true; e.RespawnSeconds = 1e9f; e.CampCenter = bar.center; e.CampRadius = 2; e.Gap = 3; e.MobName = Unraveller.Name;
            a.gameObject.SetActive(true); e.Initialize(); Enemies.Add(e);
            e.MakeRaidBoss(Unraveller.RaidSize);
            a.transform.localScale = Vector3.one * Unraveller.Scale;
            UnravellerBody.Dress(e); e.BodyRadius = UnravellerBody.BodyRadius;   // the spider, the purple crown, the void wrap (Chris, play note 256)
            a.Stats.SetBase(StatType.MaxHealth, Unraveller.HealthPool);
            a.Health.ApplyHealing(a.Health.Pool.Max);
            e.HitBase = UnravellerTuning.Current.hitBase;   // the data file's (Unraveller.HitBase its default)
            e.Cast = EncounterEnemy.FeedingCast(UnravellerTuning.Current);   // the Feeding: a cast bar to interrupt (EncounterEnemy.Unraveller)
            if (InRaid) RaidBoss = e;
            UnravellerFoe = e; unravellerOnParty = false;
            if (rising)
            {
                surfacingUp = true; surfaceFrom = Time.time; e.enabled = false; Surface(0);
                ChatSay(ChatChannel.Say, Unraveller.Name, Unraveller.RisingLine);
            }
            else { surfaceFrom = -1; UnravellerDepth = 0; }
            return e;
        }
        /// <summary>She sinks back into the river (out of any fight), and is gone when she is under.</summary>
        void SinkUnraveller()
        {
            var e = UnravellerFoe; if (e == null) return;
            if (e.actor.IsAlive) e.ResetFight();
            e.enabled = false; surfacingUp = false; surfaceFrom = Time.time;
            if (Target == e) { Target = null; AutoAttack = false; }
        }
        void RemoveUnraveller()
        {
            var e = UnravellerFoe; UnravellerFoe = null; surfaceFrom = -1; unravellerOnParty = false;
            if (e == null) return;
            EndUnravellerFight(e); if (RaidBoss == e) RaidBoss = null;
            if (Target == e) { Target = null; AutoAttack = false; }
            Enemies.Remove(e); Destroy(e.gameObject);
        }
        /// <summary>How far under the water her body is now, in metres (0 risen); and how deep she rises from.</summary>
        public float UnravellerDepth { get; private set; }
        public const float UnravellerSurfaceDepth = 7;
        /// <summary>Her body set <paramref name="t"/> of the way up out of the river (0: under, 1: risen).</summary>
        void Surface(float t)
        {
            UnravellerDepth = -UnravellerSurfaceDepth * (1 - Mathf.SmoothStep(0, 1, t));   // her body (UnravellerBody) from under the water
            var body = UnravellerFoe != null ? UnravellerFoe.transform.Find("Body") : null; if (body == null) return;
            body.localPosition = new Vector3(0, -2.4f * (1 - Mathf.SmoothStep(0, 1, t)), 0);
        }
        /// <summary>Her rising and sinking, and a wipe counted when the fight with your party ends with you dead.</summary>
        void TickUnravellerFoe(WorldBossState s)
        {
            var e = UnravellerFoe; if (e == null) return;
            if (surfaceFrom >= 0)
            {
                float t = (Time.time - surfaceFrom) / (surfacingUp ? Unraveller.RiseSeconds : Unraveller.SinkSeconds);
                Surface(surfacingUp ? t : 1 - t);
                if (t < 1) return;
                if (surfacingUp) { surfaceFrom = -1; e.enabled = true; }
                else { RemoveUnraveller(); return; }
            }
            if (!e.actor.IsAlive) return;
            if (e.FightingParty) { unravellerOnParty = true; TickUnravellerFight(e); return; }   // her fight (EncounterSession.UnravellerFight)
            if (!unravellerOnParty) return;
            unravellerOnParty = false;
            if (Player != null && Player.IsAlive) return;   // the fight broke off another way (out of reach, the leash)
            ChatSay(ChatChannel.Say, Unraveller.Name, Unraveller.WipeLine);
            if (WorldBossClock.Wiped(s, WorldSeed)) UnravellerEnded(Unraveller.SunkSystem);
        }
        /// <summary>She is dead (EnemyDied): the line in every zone and the next 3 hours begun; the reach goes back to as it was.</summary>
        void UnravellerKilled(EncounterEnemy e)
        {
            if (e == null || e != UnravellerFoe) return;
            var s = WorldBossNow; if (s == null || s.state != WorldBossState.Up) return;
            WorldBossClock.Killed(s, WorldSeed);
            bool ours = e.TappedBy == null || (Player != null && e.TappedBy == Player.EntityId.Value) || PartyActor(e.TappedBy) != null;
            Message(ours ? Unraveller.KilledSystem("You", Guild) : Unraveller.OthersKilledSystem);
        }
    }
}
