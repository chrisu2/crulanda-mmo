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
        /// <summary>What you typed, split into its channel and words: "/t wts ore" is Trade "wts ore"; no prefix is Zone.</summary>
        public static (ChatChannel channel, string text) Parse(string raw)
        {
            raw = (raw ?? "").Trim(); if (raw.Length == 0) return (ChatChannel.Zone, "");
            if (!raw.StartsWith("/")) return (ChatChannel.Zone, raw);
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
    }

    public sealed partial class EncounterSession
    {
        /// <summary>The chat: every line by channel, oldest first (ZoneChat).</summary>
        public readonly List<ChatLine> Chat = new List<ChatLine>();
        public void ChatSay(ChatChannel channel, string speaker, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Chat.Add(new ChatLine { channel = channel, speaker = speaker, text = text, time = Time.time });
            if (Chat.Count > ZoneChat.Kept) Chat.RemoveAt(0);
        }
        /// <summary>You typed a line (Enter): into its channel, and the sims who hear it may answer (SimChatter.Heard).</summary>
        public void PlayerChat(string raw)
        {
            var (channel, text) = ZoneChat.Parse(raw); if (text.Length == 0) return;
            if (channel == ChatChannel.Party && PartySims.Count == 0) { Message("You are not in a party."); return; }
            ChatSay(channel, "You", text);
            SimChatter.Active?.Heard(channel, text);
        }
    }
}
