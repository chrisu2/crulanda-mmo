using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A camp's way of fighting beyond its one signature cast (Chris, 2026-10-18: the fights "feel samey"; Docs/REWORK_LOWTOWNS_HALLOWS.md,
    /// Lowtowns pick 7 "New tricks for the other camps" and Hallow's Creek pick 8 "New tricks for the Bureau and the Pact"). Each is
    /// telegraphed (a line, a float over the mob, a ring on the ground) and each has an answer. EncounterEnemy.Tricks lands them.
    /// </summary>
    public sealed class MobTrick
    {
        /// <summary>
        /// The camp mob it belongs to, the trick's kind (<see cref="MobTricks.Kinds"/>), its name, and the line the player reads the first
        /// time it shows in a fight (what it is and its answer).
        /// </summary>
        public string mob, kind, name, line;
        /// <summary>CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL, and where the books show it.</summary>
        public string canonStatus, cite;
        /// <summary>
        /// Seconds into a fight before the first, between one and the next, and the wind-up (a hold or a silence in it stops it);
        /// for "clock" <see cref="seconds"/> is the clock itself.
        /// </summary>
        public float first = 5, every = 14, windup = 1.2f;
        /// <summary>
        /// How far it reaches (metres); its power in swings; seconds an effect lasts; metres it moves you (a knock, a pull) or adds to a
        /// spear's reach; share: the part a shield turns, the miss chance of a dazzle, the extra a broken shell gives.
        /// </summary>
        public float reach = 4, power = 1, seconds, metres, share, area = 2;
    }

    /// <summary>The tricks by camp mob (one a mob at most), and the pure rules they are measured by (MobTrickTests).</summary>
    public static class MobTricks
    {
        /// <summary>
        /// shieldline: two or more lock shields (frontal damage turned, they march together; a spear's reach on the Pact's);
        /// slate: a thrown slate at whoever stands back, a ring on the ground, a step's knock; circle: round behind its foe, then a
        /// flank bite; hook: a boat-hook pull toward it; flash: a blinding flash (a miss chance) unless you look away; clock: cutting
        /// against the clock; echo: a grey after-image repeats its blow on the same spot 2 s on; roof: holds a height and shoots while
        /// it can see you; strike: "Struck from the record", one of your helpful workings taken off.
        /// </summary>
        public static readonly string[] Kinds = { "shieldline", "slate", "circle", "hook", "flash", "clock", "echo", "roof", "strike" };
        public static readonly string[] Statuses = { "CANON", "CANON-EXPANDED", "GAME-ONLY", "PROVISIONAL" };
        /// <summary>The shield-line's gap: two of a line within this many metres of each other have their shields locked.</summary>
        public const float LockGap = 3.5f;
        /// <summary>A locked line marches at this share of its pace (they advance together).</summary>
        public const float MarchPace = .8f;

        public static readonly MobTrick[] All = {
            // ---------- The Lowtowns (pick 7) ----------
            new MobTrick { mob = "Hired warder", kind = "shieldline", name = "Shield-Line", share = .5f, first = 0, every = 0, windup = 0, reach = LockGap,
                line = "The warders lock shields across the lane and come on together. Their shields turn half of what hits them from the front: get round their flank, or break the line.",
                canonStatus = "CANON-EXPANDED", cite = "The Council's enforcers move 'with a rigid, terrifying precision' (book 1 ch.1:241) and are sent 'into the slums' (ch.2:155); a shield-line in a lane (ch.20:125-127). REWORK 5A, 'Fights feel samey' 4." },
            new MobTrick { mob = "Iron Teeth sifter", kind = "slate", name = "Thrown Slate", first = 4, every = 13, windup = 1.4f, reach = 18, power = .9f, metres = 1.5f, area = 2,
                line = "A slate comes spinning down at whoever hangs back: step out of the ring before it lands.",
                canonStatus = "GAME-ONLY", cite = "Over the rooftop edge, the Lowtowns' way out (book 1 ch.1:297). REWORK 5A, 'Fights feel samey' 2." },
            new MobTrick { mob = "Gutter-hound", kind = "circle", name = "Flank Bite", first = 4, every = 10, windup = 2.5f, reach = 6, power = 1.6f,
                line = "The hounds circle to your back before they bite: keep them in front of you, or put a wall behind you.",
                canonStatus = "GAME-ONLY", cite = "A massive, aggressive stray mastiff (book 1 ch.3:61; ch.7:73). REWORK 5A, 'Fights feel samey' 3." },
            new MobTrick { mob = "Dock-rat cutter", kind = "hook", name = "Boat-Hook", first = 5, every = 15, windup = 1.3f, reach = 9, power = .5f, metres = 3,
                line = "The dock-rats fight from their barges with a boat-hook: stop the swing, or stand off past its reach, or it drags you to the edge.",
                canonStatus = "GAME-ONLY", cite = "The dock-rats' moored barges on the poisoned river (book 1 ch.15:391; ch.16:75). REWORK 5A, 'Fights feel samey' 5." },
            new MobTrick { mob = "Grey-touched", kind = "flash", name = "Someone Else's Memory", first = 6, every = 18, windup = 1.6f, reach = 7, seconds = 3.5f, share = .3f,
                line = "Light gathers in the Grey-touched's frozen face: look away before it flashes, or stop it, or your blows go wide for a few seconds.",
                canonStatus = "CANON-EXPANDED", cite = "The geodes' frozen faces, souls in the stone (book 1 ch.3:133). REWORK 5A, 'Fights feel samey' 6." },
            // ---------- Hallow's Creek (pick 8) ----------
            new MobTrick { mob = "Bureau cutter", kind = "clock", name = "Cutting the Shell", seconds = 18, share = .3f, first = 0, every = 0, windup = 0, reach = 0,
                line = "The cutters are at a townsman's shell with their chisels. Stop them before it breaks: kill them, or hold them (the chisel stops while every cutter is held).",
                canonStatus = "CANON-EXPANDED", cite = "'If you break the shell, you kill the soul' (book 1 ch.17:67); the geodes as souls (ch.3:133). REWORK 5B, 'Fights feel samey' 2." },
            new MobTrick { mob = "Paused ringer", kind = "echo", name = "Time-Lost Echo", first = 3, every = 6, windup = 2, reach = 0, power = 1, area = 2,
                line = "Each blow the ringer lands comes again two seconds on, a grey echo on the same spot: step out of the grey ring.",
                canonStatus = "CANON-EXPANDED", cite = "The paused as 'a Time-Lost Echo' (book 1 ch.17:55); the town stuck in its moment (ch.21:11-25). REWORK 5B, 'Fights feel samey' 3." },
            new MobTrick { mob = "Iron Pact crossbowman", kind = "roof", name = "Quarrel from the Wall", first = 2, every = 4, windup = 1, reach = 30, power = .8f, metres = 1.5f,
                line = "Pact crossbows hold the height and shoot down while they can see you: climb to them, or get out of their sight and make them come down.",
                canonStatus = "GAME-ONLY", cite = "The Iron Pact holds Hallow's Creek's ground (book 1 ch.19:33). REWORK 5B, 'Fights feel samey' 5." },
            new MobTrick { mob = "Iron Pact soldier", kind = "shieldline", name = "Pact Shield-Wall", share = .5f, metres = 1.2f, first = 0, every = 0, windup = 0, reach = LockGap,
                line = "The Pact lock shields with spears out between them: they reach further, and their shields turn half from the front. Flank them, or break the wall.",
                canonStatus = "GAME-ONLY", cite = "'Formation! Shields up!' and a line of bucklers and spears (book 1 ch.20:125-127). REWORK 5B, 'Fights feel samey' 5." },
            new MobTrick { mob = "Bureau clerk", kind = "strike", name = "Struck from the Record", first = 6, every = 18, windup = 2.2f, reach = 20,
                line = "A clerk reads you out of the record: stop the reading, or one of your blessings, buffs or meals is struck off.",
                canonStatus = "GAME-ONLY", cite = "The High Council Investigation Bureau's black sunburst and its ledger of names (book 1 ch.3:145, 253). REWORK 5B, 'Fights feel samey' 6." },
        };

        public static MobTrick For(string mob) { return string.IsNullOrEmpty(mob) ? null : All.FirstOrDefault(t => t.mob == mob); }

        /// <summary>A blow from <paramref name="toSource"/> (from the shield-bearer to whoever struck) is in front of its shield.</summary>
        public static bool Frontal(Vector3 forward, Vector3 toSource)
        {
            forward.y = 0; toSource.y = 0;
            if (forward.sqrMagnitude < 1e-4f || toSource.sqrMagnitude < 1e-4f) return true;
            return Vector3.Dot(forward.normalized, toSource.normalized) > .35f;
        }
        /// <summary>What a shield-line member takes: a frontal blow on a locked line loses <paramref name="share"/> of it.</summary>
        public static int ShieldTaken(int damage, bool locked, bool frontal, float share)
        {
            if (damage <= 0 || !locked || !frontal) return damage;
            return Mathf.Max(1, Mathf.RoundToInt(damage * (1 - Mathf.Clamp01(share))));
        }
        /// <summary>A hound at <paramref name="toHound"/> (from its foe to it) is at the back of a foe facing <paramref name="victimForward"/>.</summary>
        public static bool Behind(Vector3 victimForward, Vector3 toHound)
        {
            victimForward.y = 0; toHound.y = 0;
            if (victimForward.sqrMagnitude < 1e-4f || toHound.sqrMagnitude < 1e-4f) return false;
            return Vector3.Dot(victimForward.normalized, toHound.normalized) < -.2f;
        }
        /// <summary>Someone facing <paramref name="forward"/> with the flash at <paramref name="toFlash"/> (from them) has looked away.</summary>
        public static bool LookedAway(Vector3 forward, Vector3 toFlash)
        {
            forward.y = 0; toFlash.y = 0;
            if (forward.sqrMagnitude < 1e-4f || toFlash.sqrMagnitude < 1e-4f) return false;
            return Vector3.Dot(forward.normalized, toFlash.normalized) < 0;
        }
        /// <summary>
        /// The cutters' clock after <paramref name="dt"/> seconds: it runs only while one of the cutters is free (not held) and alive.
        /// </summary>
        public static float ClockLeft(float left, float dt, int free) { return free > 0 ? Mathf.Max(0, left - dt) : left; }
        /// <summary>The ids of harmful workings "Struck from the record" never takes (it only takes what helps you).</summary>
        public static bool Harmful(string id, float takenMultiplier)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return takenMultiplier > 1 || id.Contains("grey") || id.Contains("wasting") || id.Contains("poison") || id.Contains("bleed") || id.Contains("sunder") || id.Contains("expose");
        }
    }
}
