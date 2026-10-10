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
    /// The Cantor (its fourth path, a healer; 2026-10-10, TALENT_DEPTH.md; CANON-EXPANDED: the books' Song of Mending and Symphony of
    /// Mending, the Silent Pilgrims' hope): 8 Mending Phrase (a sung heal on the most hurt), 9 the Song of Mending (a third song: the party
    /// near you mends each second), 0 Chorus of the Weave (a talent: a barrier on every party member near you).
    /// </summary>
    public sealed class ArchivistKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "sv-quick-hush", "sv-long-hush", "sv-still-air", "sv-vow", "sv-quiet-step", "sv-no-word", "sv-silence",
            "lu-deep-sleep", "lu-unsong", "lu-soft", "lu-wide", "lu-fourth", "lu-dreams", "lu-mesmer",
            "ar-resonance", "ar-full-jar", "ar-chorus", "ar-low-dirge", "ar-second-jar", "ar-catalogue", "ar-original",
            "ca-clear-voice", "ca-breath", "ca-held-note", "ca-chorus", "ca-long-song", "ca-echoing-mend", "ca-resonant-ward", "ca-quiet-mind", "ca-weave", "ca-symphony" };   // the Cantor, tiers 0-3
        public static readonly string[] BarIds = { "archivist.shard_note", "archivist.lull", "archivist.hush", "archivist.echo_bind", "archivist.cadence", "archivist.dirge", "archivist.echo_jar",
            "archivist.mending_phrase", "archivist.song_of_mending", "archivist.chorus" };
        public const float MendingReach = 10, MendingShare = .01f, HeldNoteShare = .01f, HeldNoteSeconds = 5, LowMend = .35f;
        public const float LullSeconds = 20, LullReach = 6, BindSeconds = 30, HushSilence = 5, DirgeReach = 10, JarReach = 8, JarSeconds = 6, CadenceSpeed = .1f;
        public const int LullMost = 3, JarMost = 3;
        public enum Song { None, Cadence, Dirge, Mending }

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly List<(EncounterEnemy e, float until)> sleepers = new List<(EncounterEnemy, float)>();
        float dirgeNext, mendNext; bool originalSung;
        readonly List<(Actor who, float until, int each)> heldNotes = new List<(Actor, float, int)>(); float heldNext;
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
        public override void ResetState() { Singing = Song.None; Bound = null; sleepers.Clear(); dirgeNext = mendNext = 0; originalSung = false; heldNotes.Clear(); }
        public override void ClearTransient() { Bound = null; sleepers.Clear(); }
        public override string StatusLine { get { return Singing == Song.None ? null : "Singing " + SongName(Singing); } }
        static string SongName(Song song) { return song == Song.Mending ? "the Song of Mending" : song.ToString(); }
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
                case "archivist.mending_phrase": return MendingPhrase(a);
                case "archivist.song_of_mending": return Sing(a, Song.Mending);
                case "archivist.chorus": return Chorus(a);
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
            if (Singing == song) { Singing = Song.None; s.Message("You let " + (song == Song.Mending ? "" : "the ") + SongName(song) + " fall silent."); return true; }
            return Start(a, () => { Singing = song; s.FloatText(s.Player.transform.position, song == Song.Mending ? "Song of Mending" : song.ToString(), SongBlue); });
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
        // ---------- the Cantor ----------
        /// <summary>You, Mira when she is with you, and the sims in your party.</summary>
        IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null && c.actor != null) yield return c.actor;
        }
        /// <summary>The most hurt living party member within <paramref name="reach"/> of you (you, if nobody is hurt).</summary>
        public Actor MostHurt(float reach)
        {
            Actor best = s.Player; float low = s.Player.Health.Pool.Ratio;
            foreach (var who in Party())
                if (who != null && who.IsAlive && who.Health.Pool.Ratio < low && Vector3.Distance(who.transform.position, s.Player.transform.position) <= reach) { best = who; low = who.Health.Pool.Ratio; }
            return best;
        }
        int HealActor(Actor target, int amount)
        {
            if (target == null || !target.IsAlive || amount <= 0) return 0;
            int healed = target.GetComponent<Combatant>().Heal(amount); if (healed > 0) HealFx.Show(target.transform);
            if (healed > 0) { s.AddHealingThreat(healed); s.FloatText(target.transform.position, "+" + healed, new Color(.75f, .9f, 1)); }
            return healed;
        }
        public int PhraseAmount(AbilityDefinition a, Actor target)
        {
            float heal = (a.power + 3 * Level) * (1 + .04f * R("ca-clear-voice"));
            if (R("ca-echoing-mend") > 0 && target != null && target.Health.Pool.Ratio < LowMend) heal *= 1.3f;   // Echoing Mend
            return Mathf.RoundToInt(heal);
        }
        /// <summary>Mending Phrase: sung 1.5 s (Breath: quicker), then heals whoever is most hurt by then; Held Note leaves a mend behind.</summary>
        bool MendingPhrase(AbilityDefinition a)
        {
            var sung = new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, a.cost - 3 * R("ca-quiet-mind")),
                cooldown = a.cooldown, range = a.range, castTime = Mathf.Max(.5f, a.castTime - .15f * R("ca-breath")), globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
            return Start(sung, () => {
                var target = MostHurt(a.range);
                HealActor(target, PhraseAmount(a, target));
                int held = R("ca-held-note");
                if (held > 0 && target != null && target.IsAlive) heldNotes.Add((target, Time.time + HeldNoteSeconds, Mathf.Max(1, Mathf.RoundToInt(target.Health.Pool.Max * HeldNoteShare * held))));
            });
        }
        /// <summary>Chorus of the Weave: a barrier on every party member within 15 m (Resonant Ward: larger; Symphony of Mending: a heal too).</summary>
        bool Chorus(AbilityDefinition a)
        {
            return Start(a, () => {
                int n = 0, size = Mathf.Max(1, Mathf.RoundToInt((a.power + 2 * Level) * (1 + .25f * R("ca-resonant-ward"))));
                foreach (var who in Party())
                {
                    if (who == null || !who.IsAlive || Vector3.Distance(who.transform.position, s.Player.transform.position) > a.range) continue;
                    who.GetComponent<Combatant>().AddBarrier(size, a.duration); n++;
                    if (R("ca-symphony") > 0) HealActor(who, Mathf.Max(1, Mathf.RoundToInt(who.Health.Pool.Max * .08f)));
                }
                s.FloatText(s.Player.transform.position + Vector3.up * 1.5f, "Chorus", SongBlue);
                s.Message("Chorus of the Weave: " + n + (n == 1 ? " shielded." : " shielded."));
            });
        }
        /// <summary>The Song of Mending's reach (The Weave: further).</summary>
        public float MendingSongReach { get { return MendingReach + 2 * R("ca-weave"); } }

        public override void Tick(bool inCombat)
        {
            if (Singing == Song.Mending && Time.time >= mendNext)
            {
                mendNext = Time.time + 1; float share = MendingShare + .005f * R("ca-long-song"), reach = MendingSongReach;
                foreach (var who in Party())
                    if (who != null && who.IsAlive && who.Health.Pool.Ratio < 1 && Vector3.Distance(who.transform.position, s.Player.transform.position) <= reach)
                        HealActor(who, Mathf.Max(1, Mathf.RoundToInt(who.Health.Pool.Max * share)));
            }
            if (heldNotes.Count > 0 && Time.time >= heldNext)
            {
                heldNext = Time.time + 1;
                for (int i = heldNotes.Count - 1; i >= 0; i--)
                {
                    var (who, until, each) = heldNotes[i];
                    if (who == null || !who.IsAlive || Time.time > until) { heldNotes.RemoveAt(i); continue; }
                    HealActor(who, each);
                }
            }
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
