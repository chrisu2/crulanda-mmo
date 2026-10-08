using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Archivist (Docs/CC_DESIGN.md section 0, 2026-10-08; the Silent Pilgrims, the Original Song and the echo-jars are CANON roots, the
    /// kit GAME-ONLY until checked against the books): the second crowd-control class, a song-caster on Mana with little damage.
    /// Shard Note is its one damage cast. Lull sleeps the target and up to two more within 6 m for 20 s; Echo Bind holds one mob in its own
    /// echo 30 s (one at a time); both break on damage (the CC engine, EncounterEnemy.Control). Hush interrupts and silences 5 s. One song
    /// at a time: Cadence (you move faster and Mira casts faster) or Dirge (fighting mobs within 10 m are slowed). Echo-jar replays a
    /// stored death-cry: up to three mobs within 8 m of the target flee. Bar: 1 Shard Note, 2 Lull, 3 Hush, 4 Echo Bind, 5 Cadence,
    /// 6 Dirge, 7 Echo-jar. Talents: Talents/archivist.json, all 21 implemented (2026-10-08).
    /// </summary>
    public sealed class ArchivistKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "sv-quick-hush", "sv-long-hush", "sv-still-air", "sv-vow", "sv-quiet-step", "sv-no-word", "sv-silence",
            "lu-deep-sleep", "lu-unsong", "lu-soft", "lu-wide", "lu-fourth", "lu-dreams", "lu-mesmer",
            "ar-resonance", "ar-full-jar", "ar-chorus", "ar-low-dirge", "ar-second-jar", "ar-catalogue", "ar-original" };
        public static readonly string[] BarIds = { "archivist.shard_note", "archivist.lull", "archivist.hush", "archivist.echo_bind", "archivist.cadence", "archivist.dirge", "archivist.echo_jar" };
        public const float LullSeconds = 20, LullReach = 6, BindSeconds = 30, HushSilence = 5, DirgeReach = 10, JarReach = 8, JarSeconds = 6, CadenceSpeed = .1f;
        public const int LullMost = 3, JarMost = 3;
        public enum Song { None, Cadence, Dirge }

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly List<(EncounterEnemy e, float until)> sleepers = new List<(EncounterEnemy, float)>();
        float dirgeNext; bool originalSung;
        /// <summary>The song being sung (one at a time).</summary>
        public Song Singing { get; private set; }
        /// <summary>The mob held in Echo Bind (one at a time).</summary>
        public EncounterEnemy Bound { get; private set; }

        public ArchivistKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Archivist content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Archivist unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Archivist ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        public override bool MeleeAutoAttacks { get { return false; } }   // a singer: no swings
        int R(string id) { return s.TalentRank(id); }
        int Level { get { return s.Player.Level; } }
        public override float MoveSpeedMultiplier { get { return Singing == Song.Cadence ? 1 + CadenceSpeed : 1; } }
        public override float CompanionHaste { get { return Singing == Song.Cadence ? CadenceSpeed : 0; } }
        /// <summary>Chorus (Cadence: the party hits harder) and No Word (a hushed mob takes more).</summary>
        public override float PartyDamageMultiplier(EncounterEnemy enemy)
        {
            float m = Singing == Song.Cadence ? 1 + .02f * R("ar-chorus") : 1;
            if (enemy != null && enemy.Silenced) m *= 1 + .05f * R("sv-no-word");
            return m;
        }
        /// <summary>Quiet Step: mobs notice you a metre closer per rank.</summary>
        public override float NoticeShrink { get { return R("sv-quiet-step"); } }
        public override void ResetState() { Singing = Song.None; Bound = null; sleepers.Clear(); dirgeNext = 0; originalSung = false; }
        public override void ClearTransient() { Bound = null; sleepers.Clear(); }
        public override string StatusLine { get { return Singing == Song.None ? null : "Singing " + Singing; } }
        public override string TargetStatus(EncounterEnemy enemy) { return enemy != null ? enemy.ControlLabel : null; }

        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "archivist.shard_note": return ShardNote(a);
                case "archivist.lull": return Lull(a);
                case "archivist.hush": return Hush(a);
                case "archivist.echo_bind": return EchoBind(a);
                case "archivist.cadence": return Sing(a, Song.Cadence);
                case "archivist.dirge": return Sing(a, Song.Dirge);
                case "archivist.echo_jar": return EchoJar(a);
            }
            return false;
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        bool Took(EncounterEnemy t, string why) { if (why == null) return true; s.Message(t.actor.DisplayName + ": " + why + "."); return false; }
        static readonly Color SongBlue = new Color(.6f, .8f, 1);

        bool ShardNote(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, SongBlue, .16f);
                float catalogue = R("ar-catalogue") > 0 && (t.Silenced || t.Feared) ? 1.25f : 1;   // The Catalogue
                t.Receive(Dmg((a.power + 2 * Level) * (1 + .04f * R("ar-resonance")) * catalogue), s.Player);
            });
        }
        /// <summary>Lull: the target and up to two more within 6 m of it sleep (broken by damage). A mob already fighting can be lulled too.
        /// The Original Song: the first Lull of a fight takes every mob within 10 m of the target.</summary>
        bool Lull(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost - 5 * R("lu-dreams"), a.cooldown - 5 * R("lu-soft")), () => {
                if (!t.actor.IsAlive) return;
                float seconds = LullSeconds + 2 * R("lu-deep-sleep");
                bool original = R("ar-original") > 0 && !originalSung;
                float reach = original ? 10 : LullReach + R("lu-wide"); int most = original ? 99 : LullMost + R("lu-fourth");
                if (original) originalSung = true;
                var near = s.Enemies.Where(e => e != null && e != t && e.actor.IsAlive && !e.Game && Vector3.Distance(e.transform.position, t.transform.position) <= reach)
                    .OrderBy(e => Vector3.Distance(e.transform.position, t.transform.position)).Take(most - 1).ToList();
                near.Insert(0, t); int n = 0;
                foreach (var e in near) if (e.Apply("incap", s.Player, seconds) == null) { sleepers.Add((e, Time.time + seconds)); n++; }
                s.Message(n == 0 ? "The Lull finds no one to take." : (original ? "The Original Song: " : "Lull: ") + n + (n == 1 ? " sleeps." : " sleep."));
            });
        }
        bool Hush(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - 2 * R("sv-quick-hush")), () => {
                float secs = HushSilence + R("sv-long-hush");
                var hushed = R("sv-silence") > 0 ? s.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Game && Vector3.Distance(e.transform.position, t.transform.position) <= 6).ToList() : new List<EncounterEnemy> { t };
                if (!hushed.Contains(t)) hushed.Insert(0, t);
                foreach (var e in hushed)
                {
                    string why = e.Apply("silence", s.Player, secs); if (e == t) Took(t, why);
                    if (why == null && R("sv-still-air") > 0) e.Slow(.2f * R("sv-still-air"), 5);   // Still Air
                    if (why == null && R("sv-vow") > 0) e.Apply("stun", s.Player, 1);   // The Vow
                }
            });
        }
        /// <summary>Echo Bind: one mob held 30 s; binding another lets the first go. Mesmer: a boss is stunned 4 s instead.</summary>
        bool EchoBind(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (Bound != null && Bound != t && Bound.Incapacitated) Bound.Release();
                string why = t.Apply("incap", s.Player, BindSeconds);
                if (why == "Immune" && R("lu-mesmer") > 0) { if (t.Apply("stun", s.Player, 4) == null) s.Message("Mesmer: " + t.actor.DisplayName + " is caught in its own echo for a moment."); return; }
                if (Took(t, why)) { Bound = t; sleepers.Add((t, Time.time + BindSeconds)); }
            });
        }
        bool Sing(AbilityDefinition a, Song song)
        {
            if (Singing == song) { Singing = Song.None; s.Message("You let the " + song + " fall silent."); return true; }
            return Start(a, () => { Singing = song; s.FloatText(s.Player.transform.position, song.ToString(), SongBlue); });
        }
        /// <summary>Echo-jar: a stored death-cry let out over the target: it and up to two more within 8 m flee.</summary>
        bool EchoJar(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - 30 * R("ar-second-jar")), () => {
                float seconds = JarSeconds + 2 * R("ar-full-jar"); int n = 0;
                foreach (var e in s.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Game && Vector3.Distance(e.transform.position, t.transform.position) <= JarReach)
                    .OrderBy(e => e == t ? 0 : 1).Take(JarMost).ToList())
                    if (e.Apply("fear", s.Player, seconds) == null) n++;
                s.Message(n == 0 ? "The jar's cry frightens no one." : "The death-cry rings out: " + n + (n == 1 ? " flees." : " flee."));
            });
        }
        public override void Tick(bool inCombat)
        {
            if (!inCombat) originalSung = false;
            // Unsong: a sleeper woken early (by damage) is slowed by half for 6 s.
            for (int i = sleepers.Count - 1; i >= 0; i--)
            {
                var (e, until) = sleepers[i];
                if (e == null || !e.actor.IsAlive || Time.time >= until) { sleepers.RemoveAt(i); if (e == Bound) Bound = null; continue; }
                if (!e.Incapacitated) { sleepers.RemoveAt(i); if (e == Bound) Bound = null; if (R("lu-unsong") > 0) e.Slow(.5f, 6); }
            }
            if (Singing == Song.Dirge && Time.time >= dirgeNext)
            {
                dirgeNext = Time.time + 1; float slow = .3f + .1f * R("ar-low-dirge");
                foreach (var e in s.Enemies) if (e != null && e.actor.IsAlive && e.Engaged && Vector3.Distance(e.transform.position, s.Player.transform.position) <= DirgeReach) e.Slow(slow, 1.5f);
            }
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, cost),
                cooldown = Mathf.Max(a.cooldown > 0 ? 1 : 0, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
    }
}
