using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// How a camp's mobs answer when one of them joins a fight (playtest note 3).
    /// - Solitary: nobody comes (boar, stags).
    /// - Pack: packmates near it come at once, without a sound (wolves, hounds, Weave-Eaters, spiders, briars).
    /// - Call: it calls out, and those in earshot come after a short beat; a neighbouring camp of the same people hears it too
    ///   (deserters, Sandthrone riders, the Concord, cultists, Hollow Men, the pale, withered Keepers).
    /// </summary>
    public enum SocialKind { Solitary, Pack, Call }

    /// <summary>
    /// The rules of social aggro, apart from the scene: a camp's kind and kin, how far each kind reaches, what a caller says, and
    /// which camps guard an elite. All of it is fixed by the zone data and this table; none of it draws from a zone's random stream.
    /// </summary>
    public static class SocialAggro
    {
        /// <summary>Metres from the mob that joined the fight: a pack's reach, a call's earshot, and both when a sneaking player was merely noticed.</summary>
        public const float PackReach = 9, CallReach = 12, SneakReach = 3.5f;
        /// <summary>How much further a call carries against a party (playtest note 91).</summary>
        public const float PartyReach = 1.5f;
        /// <summary>An elite's guards come from this far, however it was pulled; a non-elite camp guards an elite when its edge is within GuardPairing of the elite's centre.</summary>
        public const float GuardReach = 16, GuardPairing = 8;
        /// <summary>A guard's or a kinsman's alarm brings a camp's elite only from this near: beside it. Further off the elite stays where it is, so its guards can be cleared first.</summary>
        public const float LordReach = 3.5f;
        /// <summary>Seconds between a call and the first to answer it, and between each who answers and the next.</summary>
        public const float CallBeat = 1.2f, CallStagger = .15f;
        /// <summary>Nobody answers across more than this much height (a camp on the hill over a cave; half the reach when that is more), or when the walk round is more than twice the reach and 4 m.</summary>
        public const float MaxClimb = 4;

        /// <summary>A camp's kind: what its data says (social: "pack", "call" or "solitary"), else by its look.</summary>
        public static SocialKind KindFor(ZoneCamp camp) { return camp == null ? SocialKind.Solitary : KindFor(camp.social, camp.look); }
        public static SocialKind KindFor(string social, string look)
        {
            switch ((social ?? "").Trim().ToLowerInvariant())
            {
                case "pack": return SocialKind.Pack;
                case "call": return SocialKind.Call;
                case "solitary": return SocialKind.Solitary;
            }
            switch ((look ?? "").ToLowerInvariant())
            {
                case "wolf": case "weaveeater": case "spider": case "bramble": return SocialKind.Pack;
                case "boar": case "stag": case "bear": return SocialKind.Solitary;
                default: return SocialKind.Call;   // people, and what was people: they have voices
            }
        }
        /// <summary>True for the social values the data may hold (empty = by look).</summary>
        public static bool ValidSocial(string social)
        {
            string s = (social ?? "").Trim().ToLowerInvariant();
            return s == "" || s == "pack" || s == "call" || s == "solitary";
        }
        /// <summary>
        /// Who counts as the same people (or the same kind of beast) across camps: a call carries to kin in another camp, and an
        /// elite's call brings its kin. Deserters and their king are one band; toll-guards, pickets and outriders are all Sandthrone.
        /// </summary>
        public static string Kin(string look)
        {
            switch ((look ?? "").ToLowerInvariant())
            {
                case "deserter": case "banditking": return "deserters";
                case "outrider": return "sandthrone";
                case "collector": case "warden": case "": return "concord";
                case "cultist": return "cult";
                case "keeper": return "keepers";
                default: return look.ToLowerInvariant();
            }
        }
        /// <summary>How far a mob's joining a fight carries. Sneaking counts only when the mob noticed the player by itself (it was not hit).</summary>
        public static float Reach(SocialKind kind, bool sneakNoticed)
        {
            if (kind == SocialKind.Solitary) return 0;
            return sneakNoticed ? SneakReach : kind == SocialKind.Pack ? PackReach : CallReach;
        }
        // What a caller says, by kin: the chat line (after its name) and the word over its head. GAME-ONLY.
        static readonly Dictionary<string, (string chat, string over)[]> Shouts = new Dictionary<string, (string, string)[]> {
            { "deserters", new[] { (" shouts: \"Blades out! We're found!\"", "Blades out!"), (" shouts: \"To arms! To arms!\"", "To arms!"), (" shouts: \"Up! Up! Someone's in the camp!\"", "Up! Up!") } },
            { "sandthrone", new[] { (" shouts: \"Sandthrone! To me!\"", "To me!"), (" shouts: \"Riders! Here, on me!\"", "On me!") } },
            { "concord", new[] { (" calls out: \"In the Concord's name, hold them!\"", "Hold them!"), (" calls out: \"Wardens! Here!\"", "Wardens!") } },
            { "cult", new[] { (" shrieks: \"The ash sees you! Brothers!\"", "Brothers!"), (" shrieks: \"Unbeliever! Unbeliever!\"", "Unbeliever!") } },
            { "hollow", new[] { (" lets out a dry, rattling moan.", "A rattling moan"), (" moans, long and empty, and the others lift their heads.", "A long moan") } },
            { "pale", new[] { (" turns, and the other pale things turn with it.", "It turns") } },
            { "keepers", new[] { (" creaks a long warning through the trees.", "A long creak") } },
            { "skeleton", new[] { (" clatters, and the old bones around it turn their skulls.", "A dry clatter"), (" rattles its jaw, and the drowned dead come.", "Rattling") } },
        };
        static readonly (string chat, string over)[] PlainShout = { (" cries out for help.", "Help!") };
        /// <summary>One of the kin's shouts (pick chooses among them; any number will do).</summary>
        public static (string chat, string over) Shout(string kin, int pick)
        {
            var lines = kin != null && Shouts.TryGetValue(kin, out var found) ? found : PlainShout;
            return lines[Mathf.Abs(pick) % lines.Length];
        }
        /// <summary>
        /// For each camp, the camps that guard it (null when it is no elite's camp, or has none). The data may pair them
        /// (ZoneCamp.guards on the elite's camp: guard camp names, comma-separated, or "none"); otherwise every non-elite camp whose
        /// edge lies within <see cref="GuardPairing"/> of the elite's centre guards it (a king's hall and the guard before it).
        /// </summary>
        public static List<int>[] GuardCamps(ZoneCamp[] camps)
        {
            var result = new List<int>[camps == null ? 0 : camps.Length];
            for (int e = 0; e < result.Length; e++)
            {
                var lord = camps[e]; if (lord == null || !lord.elite) continue;
                string named = (lord.guards ?? "").Trim();
                if (named.Equals("none", StringComparison.OrdinalIgnoreCase)) continue;
                var names = named.Length == 0 ? null : Array.ConvertAll(named.Split(','), n => n.Trim());
                for (int c = 0; c < camps.Length; c++)
                {
                    var camp = camps[c]; if (c == e || camp == null || camp.elite) continue;
                    bool guards = names != null ? Array.IndexOf(names, camp.name) >= 0 : Vector2.Distance(camp.center, lord.center) - camp.radius <= GuardPairing;
                    if (!guards) continue;
                    if (result[e] == null) result[e] = new List<int>();
                    result[e].Add(c);
                }
            }
            return result;
        }
    }
}
