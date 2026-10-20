using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's Hollow as Book 1 chapter 4 shows them in Oakhaven (Docs/REWORK_LOWTOWNS_HALLOWS.md, Hallow's Creek pick 6; Chris
    /// 2026-10-18: "keep all"). The creature and each behaviour are CANON (b1 ch.N:L is D:\code\crulanda\book1\chapter_N.md at line L); the
    /// numbers, the cellar places and the well's place under the well-house are GAME-ONLY / CANON-EXPANDED:
    /// - out of the cellar doors: they come "from every single shadowed alleyway and dark cellar door" (ch.4:161): pulling a Hollow camp in the
    ///   Cellars brings one more up a cellar door behind the party (a camp released on "pull:&lt;camp&gt;", EncounterSession.HollowCellars);
    /// - the shift: "One second it was standing motionless by the desk. The very next second, it was suddenly two full feet closer ... zero
    ///   visible striding steps" (ch.4:109): a slow, silent glide of up to <see cref="GlideReach"/> m toward its foe, no walking, every
    ///   <see cref="GlideEvery"/> s while the foe is more than <see cref="GlideMin"/> m off (not the Weave-Eaters' jerking blink);
    /// - the dust: "Every single successful hit immediately puffed a thick, choking cloud of highly toxic, grey dust ... stinging her watering
    ///   eyes" (ch.4:177): a puff warned on the ground for <see cref="DustWindup"/> s; whoever of the party stands within <see cref="DustReach"/> m
    ///   when it goes off is blinded for <see cref="BlindSeconds"/> s, each of their blows missing <see cref="BlindMiss"/> of the time;
    /// - the halves: "I cleanly cut them completely in half, and the sundered, broken pieces simply blindly drag themselves ... to keep coming"
    ///   (ch.4:181): a Hollow cut down splits <see cref="SplitChance"/> of the time into a crawling half (<see cref="HalfHealth"/> of its health,
    ///   <see cref="HalfHit"/> of its blow, <see cref="HalfPace"/> of its pace) that keeps coming; the half does not split again;
    /// - the well: the Hollow "pouring endlessly out of it, or desperately, mindlessly protecting it", "a sickening, bright purple light ...
    ///   pulsing rhythmically from the deep depths" with a crystal "pulsing exactly like a diseased, glowing tumor" (ch.4:191-197, 222); broken,
    ///   every Hollow near it froze and crumbled (ch.4:240-245): an anchor-well in the Cellars, under the well-house; a Hollow that falls within
    ///   <see cref="WellReach"/> m of it while it pulses stands up again after <see cref="WellRiseSeconds"/> s with <see cref="WellRiseHealth"/> of its
    ///   health (once a visit each), until someone breaks it (E, <see cref="WellBreakSeconds"/> s of work, a fight allowed, a blow taken stops it);
    ///   then every Hollow within reach takes <see cref="WellCrumble"/> of its health.
    /// Only the ordinary Hollow of Hallow's Creek (<see cref="Mob"/> in <see cref="Zone"/>); the Hollow Men elsewhere keep the fights Chris
    /// settled. Pure rules here; EncounterEnemy.Hollow and EncounterSession.HollowCellars apply them. No machines: a stone well, a crystal.
    /// </summary>
    public static class HollowRules
    {
        public const string Zone = "zone.hallows", Mob = "Hallow's Creek Hollow";
        /// <summary>The release key prefix for a camp brought by another camp's pull (ZoneCamp.after "pull:&lt;camp name&gt;").</summary>
        public const string PullKey = "pull:";
        /// <summary>The shift (ch.4:109): the gap under which it walks, how far a glide goes, how often, and how long it takes (slow, silent).</summary>
        public const float GlideMin = 4, GlideReach = 6, GlideEvery = 7, GlideSeconds = .9f, GlideStop = 1.8f;
        /// <summary>The dust (ch.4:177): the warning, the puff's reach round it, how often, the first one into a fight, the blinding and its miss.</summary>
        public const float DustWindup = 1.5f, DustReach = 3.5f, DustEvery = 16, DustFirst = 6, BlindSeconds = 4, BlindMiss = .25f;
        /// <summary>The halves (ch.4:181).</summary>
        public const float SplitChance = .3f, HalfHealth = .25f, HalfHit = .5f, HalfPace = .55f;
        /// <summary>The well (ch.4:191-245).</summary>
        public const float WellReach = 14, WellRiseSeconds = 4, WellRiseHealth = .4f, WellBreakSeconds = 4, WellCrumble = .5f;
        /// <summary>The anchor (ZoneAnchor) the well stands at, and the interactable's kind.</summary>
        public const string WellAnchor = "the anchor-well", WellKind = "anchorwell", WellName = "The anchor-well";

        /// <summary>The dice for a split and a blinded blow's miss, in [0, 1). Tests set it.</summary>
        public static System.Func<float> Dice = () => Random.value;
        public static bool BookHollow(string zoneId, string mobName) { return zoneId == Zone && mobName == Mob; }
        /// <summary>Whether it glides toward a foe <paramref name="gap"/> m off now: not held, not drawing up dust, its glide ready.</summary>
        public static bool Glides(float gap, bool rooted, bool busy, float now, float readyAt) { return !rooted && !busy && gap > GlideMin && now >= readyAt; }
        /// <summary>How far a glide goes toward a foe <paramref name="gap"/> m off: up to <see cref="GlideReach"/>, stopping <see cref="GlideStop"/> short.</summary>
        public static float GlideStep(float gap) { return Mathf.Clamp(gap - GlideStop, 0, GlideReach); }
        /// <summary>Whether it puffs dust now: its foe within the puff's reach, not held or busy, the dust ready.</summary>
        public static bool Puffs(float gap, bool rooted, bool busy, float now, float readyAt) { return !rooted && !busy && gap <= DustReach && now >= readyAt; }
        /// <summary>Whether a blinded blow misses, for a roll in [0, 1).</summary>
        public static bool Misses(bool blinded, float roll) { return blinded && roll < BlindMiss; }
        /// <summary>Whether a Hollow cut down splits, for a roll in [0, 1): never a half, never one the well will raise.</summary>
        public static bool Splits(bool half, bool wellRaises, float roll) { return !half && !wellRaises && roll < SplitChance; }
        /// <summary>Whether the well raises a Hollow that fell <paramref name="distance"/> m from it: the well whole, and the Hollow not raised before.</summary>
        public static bool WellRaises(bool wellWhole, float distance, bool risenBefore, bool half) { return wellWhole && !risenBefore && !half && distance <= WellReach; }
        /// <summary>A half's or a risen Hollow's health out of the full.</summary>
        public static int Share(int max, float share) { return Mathf.Max(1, Mathf.RoundToInt(max * share)); }
    }
}
