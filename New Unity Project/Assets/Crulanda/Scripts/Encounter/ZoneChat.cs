using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>The chat's channels (playtest note 62; Docs/CHAT_RESEARCH.md): the game's own messages, and what people say.</summary>
    public enum ChatChannel { System, Say, Zone, Trade, LFG, Party, Whisper, Guild }

    /// <summary>One line in the chat: who said it, where, and when.</summary>
    public sealed class ChatLine
    {
        public ChatChannel channel; public string speaker, text; public float time;
        /// <summary>A whisper's other end: whom you told, or null for one sent to you.</summary>
        public string to;
        public string Shown
        {
            get
            {
                if (channel == ChatChannel.System) return text;
                if (channel == ChatChannel.Guild && string.IsNullOrEmpty(speaker)) return "[Guild] " + text;
                if (channel == ChatChannel.Whisper) return to != null ? "To " + to + ": " + text : speaker + " whispers: " + text;
                return "[" + ZoneChat.Label(channel) + "] " + speaker + ": " + text;
            }
        }
    }

    /// <summary>
    /// The zone's chat (2026-10-06, playtest note 62): a log of lines by channel (System for the game's own messages, Say near you,
    /// Zone for everyone here, Trade, LFG and Party), what you type (Enter, with /s /z /t /lfg /p), and the sims' side of it
    /// (SimChatter). Kept by the session; drawn by EncounterHud.DrawChat.
    /// </summary>
    public static class ZoneChat
    {
        public const int Kept = 120;
        public static string Label(ChatChannel c) { return c == ChatChannel.LFG ? "LFG" : c.ToString(); }
        public static Color Colour(ChatChannel c)
        {
            switch (c)
            {
                case ChatChannel.Say: return new Color(1, 1, 1);
                case ChatChannel.Zone: return new Color(1, .82f, .62f);
                case ChatChannel.Trade: return new Color(1, .62f, .78f);
                case ChatChannel.LFG: return new Color(.55f, .85f, 1);
                case ChatChannel.Party: return new Color(.6f, .7f, 1);
                case ChatChannel.Whisper: return new Color(1, .55f, 1);
                case ChatChannel.Guild: return new Color(.4f, 1, .45f);
                default: return new Color(1, .96f, .86f);
            }
        }
        /// <summary>What you typed, split into its channel and words: "/t wts ore" is Trade "wts ore"; no prefix is the sticky channel
        /// (<paramref name="current"/>, Zone at first). A prefix on its own ("/lfg") gives that channel with no words: a switch.</summary>
        public static (ChatChannel channel, string text) Parse(string raw, ChatChannel current = ChatChannel.Zone)
        {
            raw = (raw ?? "").Trim(); if (raw.Length == 0) return (current, "");
            if (!raw.StartsWith("/")) return (current, raw);
            int sp = raw.IndexOf(' '); string cmd = (sp < 0 ? raw : raw.Substring(0, sp)).ToLowerInvariant(), rest = sp < 0 ? "" : raw.Substring(sp + 1).Trim();
            switch (cmd)
            {
                case "/s": case "/say": return (ChatChannel.Say, rest);
                case "/t": case "/trade": return (ChatChannel.Trade, rest);
                case "/lfg": case "/l": return (ChatChannel.LFG, rest);
                case "/p": case "/party": return (ChatChannel.Party, rest);
                case "/g": case "/gu": case "/guild": return (ChatChannel.Guild, rest);   // round 27: /g is the guild's, as everywhere
                default: return (ChatChannel.Zone, cmd == "/z" || cmd == "/zone" || cmd == "/ooc" ? rest : raw);
            }
        }
        /// <summary>The prefix that opens a channel ("/p" for Party).</summary>
        public static string Prefix(ChatChannel c)
        {
            switch (c)
            {
                case ChatChannel.Say: return "/s"; case ChatChannel.Trade: return "/t"; case ChatChannel.LFG: return "/lfg"; case ChatChannel.Party: return "/p"; case ChatChannel.Guild: return "/g";
                default: return "/z";
            }
        }
    }

    public sealed partial class EncounterSession
    {
        /// <summary>The chat: every line by channel, oldest first (ZoneChat).</summary>
        public readonly List<ChatLine> Chat = new List<ChatLine>();
        /// <summary>Where plain words go (Round 24, playtest note 71: "I can't talk in party or lfg"): the last channel you used by its
        /// prefix, Zone at first. "/lfg" alone switches it.</summary>
        public ChatChannel ChatDefault { get; private set; } = ChatChannel.Zone;
        public void ChatSay(ChatChannel channel, string speaker, string text, string to = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            Chat.Add(new ChatLine { channel = channel, speaker = speaker, text = text, time = Time.time, to = to });
            if (Chat.Count > ZoneChat.Kept) Chat.RemoveAt(0);
            if (channel == ChatChannel.Whisper && to == null) LastWhisperer = speaker;
        }
        /// <summary>Who last whispered you (/r answers them).</summary>
        public string LastWhisperer { get; private set; }
        /// <summary>A sim by its name as typed, else by the start of its name, those online and here first (first names repeat).</summary>
        public SimAdventurer SimByName(string name)
        {
            var pop = SimPopulation.Active; if (pop == null || string.IsNullOrEmpty(name)) return null;
            var s = pop.World.sims.Find(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
            if (s != null) return s;
            var hour = Crulanda.World.WorldClock.Hour;
            var like = pop.World.sims.FindAll(x => x.name.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (x.IsOnlineAt(hour) || InParty(x.id)));
            return like.Find(x => x.zone == ZoneId && pop.Find(x.id) != null) ?? (like.Count > 0 ? like[0] : pop.World.sims.Find(x => x.name.StartsWith(name, StringComparison.OrdinalIgnoreCase)));
        }
        /// <summary>/w name words (5.5): a tell to one adventurer online anywhere; a friend or an acquaintance answers, a stranger
        /// mostly, a rival never (SimChatter.Whispered).</summary>
        public bool Whisper(string nameAndText)
        {
            nameAndText = (nameAndText ?? "").Trim(); int sp = nameAndText.IndexOf(' ');
            if (sp < 0) { Message("Whisper whom, and what? /w <name> <words>."); return false; }
            // The name may be two words ("Wren Wick hello"): try the longest match first.
            string text = null; SimAdventurer s = null;
            var words = nameAndText.Split(' ');
            for (int n = Math.Min(2, words.Length - 1); n >= 1 && s == null; n--) { s = SimByName(string.Join(" ", words, 0, n)); if (s != null) text = string.Join(" ", words, n, words.Length - n).Trim(); }
            if (s == null) { Message("Nobody called " + words[0] + "."); return false; }
            if (text.Length == 0) { Message("Say what to " + s.name + "?"); return false; }
            if (!s.IsOnlineAt(Crulanda.World.WorldClock.Hour) && !InParty(s.id)) { Message(s.name + " is not online."); return false; }
            ChatSay(ChatChannel.Whisper, "You", text, s.name);
            SimChatter.Active?.Whispered(s, text);
            return true;
        }
        /// <summary>/friend name (5.5): on or off your friends list; "/friends" lists them with who is online.</summary>
        public void ToggleFriend(string name)
        {
            var s = SimByName(name); if (s == null) { Message(string.IsNullOrEmpty(name) ? "Befriend whom? /friend <name>." : "Nobody called " + name + "."); return; }
            s.friend = !s.friend;
            Message(s.friend ? s.name + " is on your friends list." : s.name + " is off your friends list.");
            if (s.friend) SimMemory.Note(s, SimMemory.Deed.Befriended);
            SimPopulation.Active?.Persist();
        }
        public void ListFriends()
        {
            var pop = SimPopulation.Active; if (pop == null) return;
            var hour = Crulanda.World.WorldClock.Hour; var friends = pop.World.sims.FindAll(x => x.friend);
            if (friends.Count == 0) { Message("No friends yet: /friend <name> adds one (the who list, O, shows who is about)."); return; }
            friends.Sort((a, b) => (a.IsOnlineAt(hour) ? 0 : 1).CompareTo(b.IsOnlineAt(hour) ? 0 : 1));
            var sb = new System.Text.StringBuilder("Friends: ");
            for (int i = 0; i < friends.Count; i++) { var f = friends[i]; sb.Append(i > 0 ? ", " : "").Append(f.name).Append(f.IsOnlineAt(hour) || InParty(f.id) ? " (" + SimRoster.ClassName(f.classId) + " " + f.level + ", " + ZoneName(f.zone) + ")" : " (offline)"); }
            Message(sb.ToString());
        }
        /// <summary>You typed a line (Enter): a command (/invite, /inv, /leave, /who, /help), or words into their channel, which the
        /// sims who hear may answer (SimChatter.Heard).</summary>
        public void PlayerChat(string raw)
        {
            raw = (raw ?? "").Trim(); if (raw.Length == 0) return;
            if (raw.StartsWith("/"))
            {
                int sp = raw.IndexOf(' '); string cmd = (sp < 0 ? raw : raw.Substring(0, sp)).ToLowerInvariant(), arg = sp < 0 ? "" : raw.Substring(sp + 1).Trim();
                switch (cmd)
                {
                    case "/invite": case "/inv": InviteByName(arg); return;
                    case "/leave": case "/kick": if (arg.Length == 0) { foreach (var c in PartySims.ToArray()) if (c != null) LeaveParty(c.sim.id); } else { var c = PartySims.Find(x => x != null && x.sim.name.StartsWith(arg, StringComparison.OrdinalIgnoreCase)); if (c != null) LeaveParty(c.sim.id, cmd == "/kick"); else Message("Nobody called " + arg + " is in your party."); } return;
                    case "/who": WhoOpen = !WhoOpen; return;
                    case "/w": case "/tell": case "/whisper": Whisper(arg); return;
                    case "/r": case "/reply": if (LastWhisperer == null) Message("Nobody has whispered you yet."); else Whisper(LastWhisperer + " " + arg); return;
                    case "/friend": ToggleFriend(arg); return;
                    case "/friends": ListFriends(); return;
                    case "/assist": case "/a": Assist(arg); return;
                    case "/lead": case "/run": Lead(arg); return;
                    case "/dungeon": LeadDungeon(); return;
                    case "/guild": if (arg.Length == 0) { GuildRoster(); return; } break;
                    case "/groster": case "/ginfo": GuildRoster(); return;
                    case "/gquit": LeaveGuild(); return;
                    case "/invis": Unseen = !Unseen; Message(Unseen ? "Unseen: mobs will not notice you (until you hit one). /invis again to be seen." : "Seen again."); return;
                    case "/adit":   // a tester's way in (Chris, 2026-10-08): level 11 and straight to the Sealed Adit's yard
                        {
                            var adit = Zone != null ? Zone.FindZone("zone.adit") : null; if (adit == null) { Message("The Sealed Adit is not in this build."); return; }
                            if (Progress.Level < 11) { Progress.experience = EncounterProgress.XpForLevel(11); ApplyLevel(); Player.Health.ApplyHealing(Player.Health.Pool.Max); }
                            int brought = 0; foreach (var c in PartySims) if (c != null && c.sim.level < 11) { c.MatchLevel(11); brought++; }   // and the party with you (Chris, 2026-10-09)
                            PlayerPrefs.SetInt("test.adit", 1);   // and again on arrival (EncounterSession's load)
                            Message("Into the Sealed Adit at level 11" + (brought > 0 ? ", the party brought up to 11 with you" : "") + "...");
                            TravelTo(new Crulanda.World.ZoneExit { to = "zone.adit", name = "/adit", arrive = adit.spawns.player });
                            return;
                        }
                    case "/help": Message("Chat: /s say, /z zone, /t trade, /lfg, /p party, /w <name> <words> whisper, /r reply; /invite <name> (anyone online, anywhere), /leave [name], /who, /friend <name>, /friends, /assist [name], /lead [name] (a sim leads a run to a camp), /dungeon (a sim leads you through the dungeon); /g guild chat, /guild (who is in it), /gquit."); return;
                    case "/s": case "/say": case "/z": case "/zone": case "/ooc": case "/t": case "/trade": case "/lfg": case "/l": case "/p": case "/party": case "/g": case "/gu": break;
                    default: Message("Unknown command " + cmd + ". /help lists them."); return;
                }
            }
            var (channel, text) = ZoneChat.Parse(raw, ChatDefault);
            if (channel == ChatChannel.Guild && !InGuild) { Message("You are not in a guild. Say \"lf guild\" in /z or /lfg and someone may ask you in."); return; }
            if (channel == ChatChannel.Party && PartySims.Count == 0) { Message("You are not in a party: /invite someone first (the who list, O, shows who is online)."); return; }
            if (raw.StartsWith("/") && channel != ChatDefault) { ChatDefault = channel; if (text.Length == 0) { Message("Talking in " + ZoneChat.Label(channel) + " now (" + ZoneChat.Prefix(ChatChannel.Zone) + " for Zone)."); return; } }
            if (text.Length == 0) return;
            ChatSay(channel, "You", text);
            SimChatter.Active?.Heard(channel, text);
        }
    }
}
