using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Rusty Spoon as Book 1 chapter 1 tells it (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 5, 2026-10-18; Chris: "keep all"; the
    /// session plays it in EncounterSession.Spoon). Pure rules and lines here; every beat cites the book (`b1 ch.N:L` is
    /// D:\code\crulanda\book1\chapter_N.md at line L):
    /// - the smell, said once on coming in: "wet dog and stale beer" (CANON, b1 ch.1:7);
    /// - Triad at the back room's card table: "Three cards. High card takes it." (CANON, ch.1:45); the suits Stones, Cups, Swords, Wyrms and
    ///   Coins (CANON, ch.1:47, 71); "Three Kings. The Crown. Unbeatable." (CANON, ch.1:55-57). The stake, the deck of Ace to Ten and the King
    ///   and the order of the hands past those are GAME-ONLY. Now and then the dealer lays four Aces, the "Grand Emperor's Flush", "in the
    ///   appendix. Page forty-two." (CANON joke, ch.1:87), the table goes over (ch.1:89) and the brawl begins;
    /// - the drunk in the corner "butchering The Ballad of the One-Legged Goat" (CANON, ch.1:59): a short line of it now and then, and the
    ///   room goes quiet for a Crown (ch.1:59: "The table went quiet. Even the drunk in the corner stopped"). No verses are written: the book
    ///   gives only the title;
    /// - the brawl that "engulfed the tavern instantly, as decades of simmering Lowtown resentments predictably boiled over into flying fists
    ///   and shattered wood" (CANON, ch.1:107): every so often while you are near, the drinkers fight each other, fists and bottles only,
    ///   knocked down, never killed (GAME-ONLY rule); wade in and a few turn on you (GAME-ONLY); it ends with one thrown "through the shattered
    ///   front window" into "the cold mud of the alleyway" (CANON, ch.1:119-121), by Gorm when he is in (ch.1:119: Gorm throws), and the window
    ///   stays boarded a while after (GAME-ONLY).
    /// No machines.
    /// </summary>
    public static class SpoonRules
    {
        // ---------- the room ----------
        /// <summary>Said on coming in (b1 ch.1:7), again only after <see cref="SmellAgainSeconds"/> away.</summary>
        public const string SmellLine = "The Rusty Spoon smells like wet dog and stale beer, a communal despair long since settled into the floorboards.";
        public const float SmellAgainSeconds = 300;
        /// <summary>The Spoon's people (names as the zone's residents have them): the keeper, the bartender (b1 ch.1:105), Gorm (ch.1:21) and the
        /// drunk in the corner (ch.1:59, named in the game only).</summary>
        public const string Keeper = "Mother Sall", Bartender = "Snivels Gear-Grinder", Gorm = "Gorm Rock-Wall", Drunk = "Ottie Swale";
        /// <summary>The corner drunk's singing: short lines about the song, never its verses (the book gives only its title, ch.1:59).</summary>
        public static readonly string[] BalladLines = {
            "(singing, badly) The Ballad of the One-Legged Goat... the Goaaat...",
            "(singing) ...and the goat went HOP... no. Wait. From the top.",
            "(singing) The One-Legged Goat! The One-Legged... how does the middle go?",
        };
        public const float SingEveryMin = 25, SingEveryMax = 45;

        // ---------- Triad (b1 ch.1:45-57) ----------
        public static readonly string[] Suits = { "Stones", "Cups", "Swords", "Wyrms", "Coins" };
        /// <summary>Ranks 1 (the Ace, low) to 10, and 11 the King, the highest (three of them the Crown).</summary>
        public const int Ace = 1, King = 11, Ranks = 11;
        public struct Card
        {
            public int rank, suit;
            public Card(int rank, int suit) { this.rank = rank; this.suit = suit; }
            public override string ToString() { return RankName(rank) + " of " + Suits[suit]; }
        }
        public static string RankName(int r)
        {
            switch (r) { case Ace: return "Ace"; case King: return "King"; case 2: return "Two"; case 3: return "Three"; case 4: return "Four"; case 5: return "Five";
                case 6: return "Six"; case 7: return "Seven"; case 8: return "Eight"; case 9: return "Nine"; default: return "Ten"; }
        }
        /// <summary>The stake to sit in: a crown a level, never under five.</summary>
        public static int Stake(int level) { return Mathf.Max(5, level); }
        /// <summary>The share of hands the dealer "finds" the Grand Emperor's Flush (ch.1:87) and the table goes over.</summary>
        public const float CheatChance = .05f;
        /// <summary>Three Kings: the Crown, unbeatable (ch.1:55-57).</summary>
        public static bool Crown(IList<Card> hand) { return hand.Count == 3 && hand[0].rank == King && hand[1].rank == King && hand[2].rank == King; }
        /// <summary>
        /// Whose hand takes it: 1 the first, -1 the second, 0 a push. The Crown beats all; else high card takes it (ch.1:45), the next card
        /// breaking a tie, and the third (GAME-ONLY).
        /// </summary>
        public static int Compare(IList<Card> a, IList<Card> b)
        {
            bool ca = Crown(a), cb = Crown(b);
            if (ca != cb) return ca ? 1 : -1;
            var ra = Sorted(a); var rb = Sorted(b);
            for (int i = 0; i < ra.Length && i < rb.Length; i++) if (ra[i] != rb[i]) return ra[i] > rb[i] ? 1 : -1;
            return 0;
        }
        static int[] Sorted(IList<Card> h) { var r = new int[h.Count]; for (int i = 0; i < h.Count; i++) r[i] = h[i].rank; Array.Sort(r); Array.Reverse(r); return r; }
        /// <summary>Deals two hands of three from one shuffled deck (5 suits of 11).</summary>
        public static (Card[] mine, Card[] theirs) Deal(System.Random rng)
        {
            var deck = new List<Card>();
            for (int s = 0; s < Suits.Length; s++) for (int r = 1; r <= Ranks; r++) deck.Add(new Card(r, s));
            var mine = new Card[3]; var theirs = new Card[3];
            for (int i = 0; i < 6; i++) { int k = rng.Next(deck.Count); var c = deck[k]; deck.RemoveAt(k); if (i % 2 == 0) mine[i / 2] = c; else theirs[i / 2] = c; }
            return (mine, theirs);
        }
        /// <summary>What a hand pays back on the stake: the stake and as much again for a win, three times over for the Crown; the stake back on a
        /// push; nothing on a loss.</summary>
        public static int Payout(int stake, int result, bool crown) { return result > 0 ? stake * (crown ? 4 : 2) : result == 0 ? stake : 0; }
        public static string Hand(IList<Card> h) { return h[0] + ", " + h[1] + ", " + h[2]; }
        public const string CheatLine = "The dealer lays down four cards: the Ace of Wyrms, of Swords, of Cups and of Coins. \"Grand Emperor's Flush. Very rare. It's in the appendix. Page forty-two.\" The big man beside you surges up and flips the table.";
        public const string CrownLine = "Three Kings: the Crown. The table goes quiet. Even the drunk in the corner stops butchering The Ballad of the One-Legged Goat.";

        // ---------- the brawl (b1 ch.1:105-121) ----------
        /// <summary>How long a brawl runs; how long after you come near the first starts; how long between brawls; how near you must be.</summary>
        public const float BrawlSeconds = 20, FirstBrawlMin = 60, FirstBrawlMax = 150, BrawlEveryMin = 420, BrawlEveryMax = 660, BrawlNear = 24;
        /// <summary>The fewest drinkers in the room for a brawl (fewer: it waits).</summary>
        public const int MinBrawlers = 2;
        /// <summary>Every this often in a brawl one more of them goes down, for <see cref="DownMin"/> to <see cref="DownMax"/> s.</summary>
        public const float DownEvery = 3.5f, DownMin = 6, DownMax = 11;
        /// <summary>Thrown out of the window: down in the lane this long. The window boarded this long before it is glazed again.</summary>
        public const float ThrownSeconds = 14, WindowRepairSeconds = 480;
        /// <summary>Wading in: how long, how many turn on you, how often one lands a blow, its share of your health, and the share you never go
        /// under (fists and bottles: a brawl hurts, it never kills).</summary>
        public const float JoinSeconds = 9, JoinBlowEvery = 1.4f, JoinBlowShare = .05f, JoinFloor = .35f; public const int JoinTurn = 3;
        /// <summary>A blow in the brawl on someone with <paramref name="health"/> of <paramref name="max"/>: a share of their health, never taking them
        /// under <see cref="JoinFloor"/> of it (0 once there).</summary>
        public static int JoinBlow(int health, int max)
        {
            int floor = Mathf.CeilToInt(max * JoinFloor), blow = Mathf.Max(1, Mathf.CeilToInt(max * JoinBlowShare));
            return Mathf.Clamp(Mathf.Min(blow, health - floor), 0, blow);
        }
        /// <summary>What you pick off the floor after a brawl you waded into (coin from the flipped tables).</summary>
        public static int FloorCoin(int level) { return Mathf.Max(3, level * 2); }
        public static readonly string[] StartLines = {
            "A loser at the cards flips his table. Decades of simmering Lowtown resentments boil over into flying fists and shattered wood.",
            "Somebody's elbow finds somebody's ale. The Rusty Spoon erupts: the brawl engulfs the tavern in an instant.",
            "\"Cheat!\" A chair goes over, then a table, then everyone. The brawl takes the room at once.",
        };
        public static readonly string[] BrawlShouts = { "Oi!", "That was MY drink!", "Cheat!", "Not the face!", "Hold him!", "Who threw that?", "Outside! No, inside!" };
        public const string SnivelsAtStart = "Not the vodka shelf! Not again!";
        public const string SnivelsAtEnd = "And who's paying for that window? Hm? HM?";
        public const string JoinLine = "You wade into the brawl. A few of them forget each other and turn on you.";
        public const string JoinPrompt = "Wade into the brawl";
        public static string ThrowLine(string thrown, bool gorm)
        {
            return gorm ? "Gorm Rock-Wall sets his ale aside with a long, weary sigh, takes " + thrown + " by the collar and throws him out through the front window into the mud of the lane."
                : thrown + " goes out through the front window in a shower of glass and lands in the mud of the lane.";
        }
        public const string FloorCoinLine = "When the dust settles you sweep up what the tables spilled: ";
        public const string WindowFixedLine = "Somebody has glazed the Spoon's front window again. It won't last.";
    }
}
