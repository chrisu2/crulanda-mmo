using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>The chat's channels (playtest note 62; Docs/CHAT_RESEARCH.md): the game's own messages, and what people say.</summary>
    public enum ChatChannel { System, Say, Zone, Trade, LFG, Party }

    /// <summary>One line in the chat: who said it, where, and when.</summary>
    public sealed class ChatLine
    {
        public ChatChannel channel; public string speaker, text; public float time;
        public string Shown { get { return channel == ChatChannel.System ? text : "[" + ZoneChat.Label(channel) + "] " + speaker + ": " + text; } }
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
                case "/p": case "/party": case "/g": return (ChatChannel.Party, rest);
                default: return (ChatChannel.Zone, cmd == "/z" || cmd == "/zone" || cmd == "/ooc" ? rest : raw);
            }
        }
        /// <summary>The prefix that opens a channel ("/p" for Party).</summary>
        public static string Prefix(ChatChannel c)
        {
            switch (c)
            {
                case ChatChannel.Say: return "/s"; case ChatChannel.Trade: return "/t"; case ChatChannel.LFG: return "/lfg"; case ChatChannel.Party: return "/p";
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
        public void ChatSay(ChatChannel channel, string speaker, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Chat.Add(new ChatLine { channel = channel, speaker = speaker, text = text, time = Time.time });
            if (Chat.Count > ZoneChat.Kept) Chat.RemoveAt(0);
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
                    case "/leave": case "/kick": if (arg.Length == 0) { foreach (var c in PartySims.ToArray()) if (c != null) LeaveParty(c.sim.id); } else { var c = PartySims.Find(x => x != null && x.sim.name.StartsWith(arg, StringComparison.OrdinalIgnoreCase)); if (c != null) LeaveParty(c.sim.id); else Message("Nobody called " + arg + " is in your party."); } return;
                    case "/who": WhoOpen = !WhoOpen; return;
                    case "/help": Message("Chat: /s say, /z zone, /t trade, /lfg, /p party; /invite <name> (anyone online, anywhere), /leave [name], /who."); return;
                    case "/s": case "/say": case "/z": case "/zone": case "/ooc": case "/t": case "/trade": case "/lfg": case "/l": case "/p": case "/party": case "/g": break;
                    default: Message("Unknown command " + cmd + ". /help lists them."); return;
                }
            }
            var (channel, text) = ZoneChat.Parse(raw, ChatDefault);
            if (channel == ChatChannel.Party && PartySims.Count == 0) { Message("You are not in a party: /invite someone first (the who list, O, shows who is online)."); return; }
            if (raw.StartsWith("/") && channel != ChatDefault) { ChatDefault = channel; if (text.Length == 0) { Message("Talking in " + ZoneChat.Label(channel) + " now (" + ZoneChat.Prefix(ChatChannel.Zone) + " for Zone)."); return; } }
            if (text.Length == 0) return;
            ChatSay(channel, "You", text);
            SimChatter.Active?.Heard(channel, text);
        }
    }
}
