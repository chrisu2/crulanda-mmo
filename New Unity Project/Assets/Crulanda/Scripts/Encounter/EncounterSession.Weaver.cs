using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Weaver's unlocking (dungeon step D4; Docs/DUNGEON_DESIGN.md section 4, the Rail Hall), the session's side of it (AditWeaver walks
    /// Mother Quillet and keeps the time):
    /// - she is the freed goblin who hums the platform gate open on the quiet way ("The Pressed" done), and she stays by the gate;
    /// - spoken to, she sets off down the platform; she is one of your party for every mob, for Mira and for the sims (PartyActor);
    /// - two waves come for her: platform guards from behind the carriage a third of the way down, dockers from the far end at two thirds;
    /// - Rail-Captain Danner lies in the dark while the escort can still happen and stands up when the lock is half matched (he still
    ///   springs out at anyone who walks up to him);
    /// - matched, the carriage's geodes go dark (ZoneBuilder.QuietCarriage) and "adit.lock" is kept for good: Pib's "The Weaver's Lock"
    ///   (a flag objective, "key:adit.lock") can be handed in;
    /// - if she falls, whatever was on her lets go, the waves go back into the dark, and a little later she is by the gate again.
    /// All GAME-ONLY (the goblins are CANON people; Quillet, the lock and the waves are ours).
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string WeaverLock = "adit.lock", WeaverQuest = "side.adit.weaver", DannerName = "Rail-Captain Orsk Danner";
        /// <summary>How many come in each of the two waves.</summary>
        public const int WaveSize = 3;
        /// <summary>Mother Quillet while she is in the hall (null before the quiet gate, and once the lock was matched on an earlier visit).</summary>
        public AditWeaver Weaver { get; private set; }
        readonly List<EncounterEnemy> weaverWaves = new List<EncounterEnemy>();
        /// <summary>The waves' people still standing or lying where they fell (tests read it).</summary>
        public IReadOnlyList<EncounterEnemy> WeaverWaves { get { return weaverWaves; } }

        /// <summary>The escort can still happen here: the Rail Hall is built, a freed goblin will open the gate, and the lock is not matched yet.</summary>
        bool EscortPossible { get { return Zone != null && Zone.RailWaves != null && Quests != null && Quests.IsDone(PressedQuest) && !Progress.keys.Contains(WeaverLock); } }
        EncounterEnemy Danner() { return Enemies.Find(e => e != null && e.Camp && e.Elite && e.actor.IsAlive && e.MobName == DannerName); }

        /// <summary>On arrival: a lock matched on an earlier visit leaves the carriage dark; while the escort can still happen, Danner lies in the dark.</summary>
        void StartWeaver()
        {
            if (Zone == null || Zone.RailWaves == null) return;
            if (Progress.keys.Contains(WeaverLock)) { Zone.QuietCarriage(); return; }
            if (!EscortPossible) return;
            var danner = Danner(); if (danner != null) danner.Hide();
        }
        /// <summary>The quiet way in: the goblin who hummed the gate open is Mother Quillet, and she waits on the hall's side of it.</summary>
        void SpawnWeaver(ZoneGate g)
        {
            if (Weaver != null || g == null || !EscortPossible) return;
            var at = g.transform.position + g.transform.forward * 2.4f + g.transform.right * 1.2f;
            if (!NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) return;
            var a = SpawnActor(AditWeaver.Name, content.healer, hit.position + Vector3.up, new Color(.46f, .56f, .4f), "npc.adit.quillet", ActorLook.Villager, 12);
            AddAgent(a.gameObject, 2.2f, .35f);
            a.transform.localScale = Vector3.one * .78f;   // a goblin, and an old one (a villager's figure until there is a goblin's)
            var w = a.gameObject.AddComponent<AditWeaver>();
            a.gameObject.SetActive(true);
            a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(12, false, true, false)); a.Health.ApplyHealing(a.Health.Pool.Max);   // an elite's health: she can take a beating, not a long one
            var lockAt = NavMesh.SamplePosition(Zone.RailLock, out var l, 3, NavMesh.AllAreas) ? l.position : Zone.RailLock;
            w.Init(a, this, lockAt);
            w.talk = new ZoneInteractable { name = AditWeaver.Name, prompt = "Talk to " + AditWeaver.Name, kind = "weaver", position = a.transform.position };
            Zone.Interactables.Add(w.talk);
            Weaver = w;
            w.Say("That carriage is full of singing stones, and every one of them was somebody's memory once. There's a lock on it I can match. Walk me down there, and keep the Company off me.");
        }
        void TalkToWeaver()
        {
            var w = Weaver; if (w == null) return;
            if (w.Now == AditWeaver.Stage.Done) { w.Say("Hear that? Nothing. Go and tell Pib his old auntie did it. He'll not believe you."); return; }
            if (!w.Begin()) w.Say("Not now. Give me a moment.");
        }
        /// <summary>A wave comes for her: 1, platform guards dropping off the back of the carriage; 2, dockers from the far end.</summary>
        public void WeaverWave(AditWeaver w, int wave)
        {
            if (Zone == null || Zone.RailWaves == null || wave < 1 || wave > Zone.RailWaves.Length || w == null || !w.actor.IsAlive) return;
            int c = CampByTag(wave == 1 ? "platformguard" : "docker"); if (c < 0) return;
            var camp = Zone.Zone.camps[c]; var look = LookFor(camp.look, false); int level = camp.levelMin;
            for (int k = 0; k < WaveSize; k++)
            {
                var spot = Zone.RailWaves[wave - 1] + new Vector3((k - 1) * 1.5f, 0, (k % 2) * 1.2f);
                if (!NavMesh.SamplePosition(spot, out var hit, 3, NavMesh.AllAreas)) continue;
                string id = "mob." + LootContext.CampTag(camp) + "." + Zone.Zone.id.Replace("zone.", "") + "." + c + ".e" + wave + k;   // the camp's loot (LootContext reads the camp from the id)
                var a = SpawnActor(camp.mob, content.enemy, hit.position + Vector3.up, Color.grey, id, look, level);
                AddAgent(a.gameObject, 2.8f);
                var e = a.gameObject.AddComponent<EncounterEnemy>(); e.actor = a; e.persistentId = id; e.session = this;
                e.Camp = true; e.RespawnSeconds = 1e6f; e.CampCenter = new Vector2(hit.position.x, hit.position.z); e.CampRadius = 2;   // never back: one wave
                a.gameObject.SetActive(true); e.Initialize(); Enemies.Add(e);
                e.Tough = Zone.Zone.groupZone;
                a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(level, false, e.Tough, false)); a.Health.ApplyHealing(a.Health.Pool.Max);
                e.HitBase = EncounterEnemy.MobHit(level, false, e.Tough);
                e.MobName = camp.mob; e.Cast = MobCasts.For(camp.mob);   // no camp of its own (CampIndex -1): it calls nobody and answers nobody
                e.threat.Add(w.actor.EntityId.Value, EncounterEnemy.JoinThreat);
                weaverWaves.Add(e);
            }
            Message(wave == 1 ? "Boots on the carriage's bed: platform guards drop off the back of it and run at the goblin." : "Out of the dark at the far end, where the line runs on: dockers with hooks and bars. \"Get that goblin off the carriage!\"");
        }
        int CampByTag(string tag)
        {
            var camps = Zone.Zone.camps;
            for (int c = 0; c < camps.Length; c++) if (camps[c] != null && !camps[c].elite && LootContext.CampTag(camps[c]) == tag) return c;
            return -1;
        }
        /// <summary>The lock half matched: Danner stands up out of the dark and comes for her.</summary>
        public void WeaverHalfway(AditWeaver w)
        {
            var d = Danner();
            if (d == null) { w.Say("Half of it. Don't you talk to me, I'll lose the note."); return; }
            bool hid = d.Hidden; d.Rise(w.actor);
            Message(hid ? DannerName + " stands up out of the dark at the end of the platform. \"Who let a goblin at my carriage?\"" : DannerName + " roars down the platform: \"Off my carriage!\"");
        }
        /// <summary>Matched: the geodes go dark, the carriage will not run, and "adit.lock" is kept for good.</summary>
        public void WeaverDone(AditWeaver w)
        {
            if (!Progress.keys.Contains(WeaverLock)) Progress.keys.Add(WeaverLock);
            if (Zone != null) Zone.QuietCarriage();
            FloatText(w.LockAt + Vector3.up * 2, "The lock is matched", new Color(.8f, .65f, 1));
            Message("The lock's note slides into the right one. One by one the geodes in the carriage gutter and go dark, and the whole hall is quieter for it. The carriage will not run again.");
            w.Say("There. Let go, all of them. Whatever they held is back in the stone where it belongs.");
            ReconcileQuests(); Save(false);
        }
        /// <summary>She fell: whatever was on her lets go, the waves go back into the dark, and she is carried back to the gate.</summary>
        public void WeaverFell(AditWeaver w)
        {
            var body = w.transform.Find("Body"); if (body != null) { body.localRotation = Quaternion.Euler(0, 0, 90); body.localPosition = new Vector3(0, -.6f, 0); }
            foreach (var e in Enemies) if (e != null && e.actor.IsAlive && e.Victim == w.actor) e.ResetFight();
            foreach (var e in weaverWaves.ToArray())
            {
                if (e == null) { weaverWaves.Remove(e); continue; }
                if (!e.actor.IsAlive) continue;   // the fallen stay where they fell, to be searched
                if (Target == e) { Target = null; AutoAttack = false; }
                Enemies.Remove(e); weaverWaves.Remove(e); Destroy(e.gameObject);
            }
            Message(AditWeaver.Name + " goes down. The Company men melt back into the dark; the goblins will drag her back to the gate. Give her a little while.");
        }
        /// <summary>Back by the gate, whole, to try again when spoken to.</summary>
        public void WeaverBack(AditWeaver w)
        {
            var body = w.transform.Find("Body"); if (body != null) { body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
            w.actor.Health.Revive(w.actor.Health.Pool.Max); w.actor.Health.ApplyHealing(w.actor.Health.Pool.Max);
            w.Restart();
            Message(AditWeaver.Name + " is back by the platform gate, bandaged and cross about it. Talk to her when you are ready to go again.");
        }
    }
}
