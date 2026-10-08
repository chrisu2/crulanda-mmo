using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The guilds (round 27, sims item 5; ROADMAP 5.7): four GAME-ONLY guilds the sims belong to, each with its own character. About
    /// two in three sims are in one, chosen by what they are like (the bold run with the Scree Hounds, the high levels with the Long
    /// Road, the friendly with the Lantern Watch, the rest with Oak and Ember). Assigned once per sim, in the order the sims were made,
    /// so an old world gains them and a grown world's new sims get theirs (WorldSave.guildedUpTo).
    /// </summary>
    public static class SimGuilds
    {
        public static readonly (string name, string motto)[] All = {
            ("The Lantern Watch", "all levels, we help the new ones"),
            ("Oak and Ember", "crafters, gatherers and good company"),
            ("The Long Road", "levelling hard, dungeon runs every night"),
            ("Scree Hounds", "the hard camps and the elites first"),
        };
        public static bool Exists(string guild) { return Array.FindIndex(All, g => g.name == guild) >= 0; }
        public static string Motto(string guild) { int i = Array.FindIndex(All, g => g.name == guild); return i < 0 ? "" : All[i].motto; }
        /// <summary>Gives each sim not yet assigned its guild (or none). True when any was assigned.</summary>
        public static bool Assign(WorldSave w)
        {
            if (w == null || w.sims == null || w.guildedUpTo >= w.sims.Count) return false;
            for (int i = Math.Max(0, w.guildedUpTo); i < w.sims.Count; i++)
            {
                var s = w.sims[i]; var r = new System.Random(w.seed * 31 + i * 7919 + 5);
                double roll = r.NextDouble(), pick = r.NextDouble();
                if (roll < .32) { s.guild = ""; continue; }
                if (s.bold > .65f && pick < .6) s.guild = All[3].name;
                else if (s.level >= 7 && pick < .65) s.guild = All[2].name;
                else if (s.friendly > .55f) s.guild = All[0].name;
                else s.guild = All[1].name;
            }
            w.guildedUpTo = w.sims.Count; return true;
        }
        public static List<SimAdventurer> Members(WorldSave w, string guild) { return w == null || string.IsNullOrEmpty(guild) ? new List<SimAdventurer>() : w.sims.FindAll(s => s.guild == guild); }
        /// <summary>The guild master: its highest level, the first made on a tie.</summary>
        public static SimAdventurer Leader(WorldSave w, string guild)
        {
            SimAdventurer best = null; foreach (var s in Members(w, guild)) if (best == null || s.level > best.level) best = s; return best;
        }
    }

    public sealed partial class EncounterSession
    {
        /// <summary>The guild you are in ("" none). Kept on the character (EncounterProgress.guild).</summary>
        public string Guild { get { return Progress?.guild ?? ""; } }
        public bool InGuild { get { return !string.IsNullOrEmpty(Guild); } }
        public bool SameGuild(SimAdventurer s) { return s != null && InGuild && s.guild == Guild; }
        /// <summary>The guild a pending invitation is to (null: the invitation is to a group).</summary>
        public string PendingGuild { get; private set; }
        /// <summary>A sim asks you into its guild (the same Accept / Decline panel as a group invitation).</summary>
        public void GuildInvitedBy(SimAdventurer s)
        {
            if (s == null || string.IsNullOrEmpty(s.guild) || InGuild || PendingInvite != null) return;
            PendingInvite = s; PendingGuild = s.guild; pendingUntil = Time.time + 30;
            Message(s.name + " invites you to join <" + s.guild + ">. (Accept or decline above the bars.)");
        }
        public void JoinGuild(string guild, SimAdventurer by = null)
        {
            if (!SimGuilds.Exists(guild)) { Message("There is no guild called " + guild + "."); return; }
            if (Guild == guild) { Message("You are already in <" + guild + ">."); return; }
            Progress.guild = guild;
            Message("You have joined <" + guild + ">. /g talks in guild chat; /guild shows who is in it; /gquit leaves.");
            ChatSay(ChatChannel.Guild, "", (by != null ? by.name + " has invited you" : "You have joined") + ". Welcome to <" + guild + ">.");
            if (by != null) SimMemory.Note(by, SimMemory.Deed.Befriended);
            SimChatter.Active?.Welcomed(by);
            SimPopulation.Active?.Persist(); Save(false);
        }
        public void LeaveGuild()
        {
            if (!InGuild) { Message("You are not in a guild."); return; }
            string was = Guild; Progress.guild = "";
            Message("You have left <" + was + ">."); SimChatter.Active?.Farewell(was); Save(false);
        }
        /// <summary>/guild: the roster, those online first, the guild master marked.</summary>
        public void GuildRoster()
        {
            if (!InGuild) { Message("You are not in a guild. Guild folk ask the ones they get on with; say \"lf guild\" in /z or /lfg."); return; }
            var pop = SimPopulation.Active; if (pop == null) return;
            var hour = WorldClock.Hour; var mates = SimGuilds.Members(pop.World, Guild); var lead = SimGuilds.Leader(pop.World, Guild);
            mates.Sort((a, b) => (a.IsOnlineAt(hour) ? 0 : 1) != (b.IsOnlineAt(hour) ? 0 : 1) ? (a.IsOnlineAt(hour) ? 0 : 1).CompareTo(b.IsOnlineAt(hour) ? 0 : 1) : b.level.CompareTo(a.level));
            int online = mates.FindAll(s => s.IsOnlineAt(hour) || InParty(s.id)).Count;
            var sb = new System.Text.StringBuilder("<" + Guild + ">, " + SimGuilds.Motto(Guild) + ". " + (mates.Count + 1) + " members, " + online + " online: ");
            for (int i = 0; i < mates.Count; i++)
            {
                var s = mates[i]; bool on = s.IsOnlineAt(hour) || InParty(s.id);
                sb.Append(i > 0 ? ", " : "").Append(s.name).Append(s == lead ? " (guild master)" : "").Append(on ? " (" + SimRoster.ClassName(s.classId) + " " + s.level + ", " + ZoneName(s.zone) + ")" : " (offline)");
            }
            Message(sb.ToString());
        }
    }

    public sealed partial class SimChatter
    {
        float nextGuild, nextRecruit;
        List<SimAdventurer> GuildOnline(string guild = null)
        {
            guild = guild ?? S.Guild; var list = new List<SimAdventurer>(); if (string.IsNullOrEmpty(guild)) return list;
            foreach (var s in Population.World.sims) if (s.guild == guild && (s.IsOnlineAt(WorldClock.Hour) || S.InParty(s.id))) list.Add(s);
            return list;
        }
        /// <summary>Every minute or two one of your guild online anywhere says something in Guild; now and then a guild sim who likes
        /// you asks you in (whisper, then the invitation), if you are in none.</summary>
        void GuildTick()
        {
            if (Time.time >= nextGuild && S.InGuild)
            {
                nextGuild = Time.time + Mathf.Lerp(45, 110, R);
                var on = GuildOnline().FindAll(s => s.chatty > .2f); if (on.Count > 0) GuildTalk(on[rng.Next(on.Count)], on);
            }
            if (Time.time >= nextRecruit)
            {
                nextRecruit = Time.time + Mathf.Lerp(240, 480, R);
                if (S.InGuild || S.InCombat || S.PendingInvite != null || S.Progress.Level < 2) return;
                var keen = Here().FindAll(s => !string.IsNullOrEmpty(s.guild) && SimMemory.Of(s) >= SimMemory.Standing.Acquaintance);
                if (keen.Count == 0 || R > .7f) return;
                var s2 = keen[rng.Next(keen.Count)]; AskToJoin(s2);
            }
        }
        void AskToJoin(SimAdventurer s)
        {
            Later(ChatChannel.Whisper, s.name, s.chatty > .65f ? "hey, " + s.guild + " is recruiting. want a ginv?" : "You'd fit in with us, " + s.guild + ". " + Cap(SimGuilds.Motto(s.guild)) + ". Shall I send an invitation?");
            StartCoroutine(GuildInviteLater(s, 6));
        }
        System.Collections.IEnumerator GuildInviteLater(SimAdventurer s, float after) { yield return new WaitForSeconds(after + R * 3); if (s != null && (s.IsOnlineAt(WorldClock.Hour) || S.InParty(s.id))) S.GuildInvitedBy(s); }
        static string Cap(string t) { return string.IsNullOrEmpty(t) ? t : char.ToUpperInvariant(t[0]) + t.Substring(1); }
        void GuildTalk(SimAdventurer s, List<SimAdventurer> on)
        {
            bool lingo = s.chatty > .65f; var options = new List<string>();
            options.Add(lingo ? "evening all" : "Evening, all."); options.Add("who's on?"); options.Add(S.ZoneName(s.zone) + (lingo ? " is dead tonight" : " is quiet tonight."));
            if (s.goodIds.Count > 0) options.Add((lingo ? "got " : "I have ") + S.ItemName(s.goodIds[rng.Next(s.goodIds.Count)]) + (lingo ? " if any guildie needs it, cheap" : " if anyone in the guild needs it. Cheap to you."));
            if (s.bold > .5f) options.Add(lingo ? "anyone up for a dungeon run? /dungeon" : "Anyone for a dungeon run tonight?");
            if (s.level < EncounterProgress.LevelCap) options.Add(lingo ? "so close to " + (s.level + 1) : "Nearly level " + (s.level + 1) + ".");
            if (on.Count > 1) { var o = on.Find(x => x != s); if (o != null) options.Add((lingo ? "o/ " : "Hello, ") + o.name.Split(' ')[0]); }
            S.ChatSay(ChatChannel.Guild, s.name, options[rng.Next(options.Count)]);
        }
        /// <summary>You said something in Guild: one of them answers (an offer to group goes as far as an invitation).</summary>
        void GuildHeard(string t)
        {
            var on = GuildOnline().FindAll(s => !S.InParty(s.id)); if (on.Count == 0) return;
            var s = on[rng.Next(on.Count)];
            if (t.Contains("group") || t.Contains("lfg") || t.Contains("run") || t.Contains("dungeon") || t.Contains("help"))
            {
                var fit = on.FindAll(x => Mathf.Abs(x.level - S.Progress.Level) <= EncounterSession.InviteLevelGap);
                if (fit.Count > 0) { s = fit[rng.Next(fit.Count)]; Later(ChatChannel.Guild, s.name, s.chatty > .65f ? "i'm in, sending inv" : "I'll come. Sending you an invite."); StartCoroutine(InviteLater(s, 5)); return; }
                Later(ChatChannel.Guild, s.name, "nobody near your level on, sorry"); return;
            }
            if (t == "o/" || t.StartsWith("hi") || t.StartsWith("hey") || t.StartsWith("hello") || t.Contains("evening") || t.Contains("morning"))
            {
                int n = Mathf.Min(on.Count, 1 + (R < .6f ? 1 : 0));
                for (int i = 0; i < n; i++) { var o = on[rng.Next(on.Count)]; on.Remove(o); Later(ChatChannel.Guild, o.name, Pick(new[] { "o/", "hey!", "evening", "hi :)", "welcome back" }), i * 2.5f); }
                return;
            }
            if (t.Contains("?")) { Later(ChatChannel.Guild, s.name, Pick(new[] { "not sure, ask " + (SimGuilds.Leader(Population.World, S.Guild)?.name.Split(' ')[0] ?? "around"), "no idea sorry", "try the notice board" })); return; }
            if (R < .6f) Later(ChatChannel.Guild, s.name, Pick(new[] { "lol", "ha", "true", "nice", ":)" }));
        }
        /// <summary>Asking about guilds in Zone or LFG ("lf guild"): someone guilded and not against you asks you in.</summary>
        bool AskedForGuild(string t)
        {
            if (!t.Contains("guild") || S.InGuild) return false;
            var keen = Here().FindAll(s => !string.IsNullOrEmpty(s.guild) && !SimMemory.Refuses(s) && s.friendly > .3f);
            if (keen.Count == 0) return false;
            var s = keen[rng.Next(keen.Count)];
            Later(ChatChannel.Zone, s.name, s.chatty > .65f ? "<" + s.guild + "> recruiting, sending ginv" : "<" + s.guild + "> will have you. I'll send an invitation.");
            StartCoroutine(GuildInviteLater(s, 5)); return true;
        }
        /// <summary>Now and then a recruiting call in Zone (for the talk options).</summary>
        string Recruiting(SimAdventurer s)
        {
            if (string.IsNullOrEmpty(s.guild) || s.chatty < .4f || S.Guild == s.guild) return null;
            return s.chatty > .65f ? "<" + s.guild + "> recruiting, " + SimGuilds.Motto(s.guild) + ", pst" : "<" + s.guild + "> is looking for members: " + SimGuilds.Motto(s.guild) + ". Whisper me.";
        }
        public void Welcomed(SimAdventurer by)
        {
            var on = GuildOnline().FindAll(s => s != by); int n = Mathf.Min(on.Count, 2);
            if (by != null) Later(ChatChannel.Guild, by.name, by.chatty > .65f ? "welcome!! :)" : "Welcome aboard.", 0);
            for (int i = 0; i < n; i++) { var o = on[rng.Next(on.Count)]; on.Remove(o); Later(ChatChannel.Guild, o.name, Pick(new[] { "welcome!", "welcome :)", "o/ welcome", "welcome in" }), 2 + i * 2); }
        }
        public void Farewell(string guild)
        {
            var on = GuildOnline(guild); if (on.Count == 0) return;
            var o = on[rng.Next(on.Count)]; Later(ChatChannel.Whisper, o.name, o.chatty > .65f ? "aw, you left? np, good luck out there" : "Sorry to see you go. Good luck out there.", 1);
        }
        /// <summary>You gained a level: your guild says grats.</summary>
        public void PlayerDinged(int level)
        {
            var on = GuildOnline(); int n = Mathf.Min(on.Count, 1 + (R < .5f ? 1 : 0) + (R < .3f ? 1 : 0));
            for (int i = 0; i < n; i++) { var o = on[rng.Next(on.Count)]; on.Remove(o); Later(ChatChannel.Guild, o.name, Pick(new[] { "gz!", "grats on " + level + "!", "grats", "nice, gz", "ding! gz" }), i * 2); }
        }
    }
}
