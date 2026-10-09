using System;

namespace Crulanda.Encounter
{
    /// <summary>
    /// What a camp elite does beyond its swings (playtest note 2). All GAME-ONLY.
    /// - The heavy blow: every <see cref="every"/> seconds it stops, draws back for <see cref="windup"/> seconds (a cast bar under the
    ///   target frame, a mark on the ground out to <see cref="reach"/> metres) and then hits whoever it is fighting for
    ///   <see cref="blow"/> times its swing, if they are still inside the mark. Stepping out avoids it; Guard blunts it.
    /// - The enrage: at <see cref="enrageAt"/> of its health it swings <see cref="enrageHaste"/> times as far apart (faster) until it dies or resets.
    /// - The call: at <see cref="callAt"/> of its health it calls once, and its camp, its guards and its kin within
    ///   <see cref="callReach"/> metres come (nothing is said when nobody can answer). <see cref="callMost"/> is the most of its
    ///   kin, beyond its guards, who answer (the nearest; 0 = all who hear): a call across a hillside reaches a whole camp.
    /// <see cref="health"/> and <see cref="hit"/> multiply the elite's health and swing: a dungeon's end boss is a step harder.
    /// </summary>
    public sealed class EliteMove
    {
        public string mob, name, call, callShort, enrage;
        public bool boss;
        public float windup = 2, blow = 4, reach = 3.8f, every = 11, first = 4;
        public float enrageAt = .3f, enrageHaste = .65f, callAt = .6f, callReach = 22;
        public float health = 1, hit = 1;
        public int callMost;
        // Boss mechanics (D2, 2026-10-08; EncounterEnemy.Boss):
        public BossPhase[] phases;
        public bool callOnPull;
        public int deathCall; public float deathReach = 45; public string deathText;
        public float knock, disarm, blinkEvery;
        public float ringEvery, ringReach = 4.5f, ringBlow = 1.6f; public string ringName;
    }

    /// <summary>The twelve camp elites' moves, by the camp's mob name, and a plain one for any elite not listed.</summary>
    public static class EliteMoves
    {
        /// <summary>How much harder a dungeon's end boss is than an outdoor named elite of its level.</summary>
        public const float BossHealth = 1.25f, BossHit = 1.1f;
        public static readonly EliteMove[] All = {
            // ---------- Oakhaven ----------
            new EliteMove { mob = "Caddock, the Bandit King", name = "The King's Due", boss = true, health = BossHealth, hit = BossHit, windup = 2, blow = 3.1f, reach = 3.8f, every = 12,
                call = "Caddock bellows: \"To me, you dogs! Earn your keep!\"", callShort = "To me!", enrageAt = .35f, enrageHaste = .6f,
                enrage = "Caddock shoves the tin crown straight and comes on like a cornered man." },
            new EliteMove { mob = "Quartermaster Hesk", name = "Short Weight", windup = 1.8f, blow = 3.8f, reach = 3.4f, every = 10,
                call = "Hesk shouts: \"Thieves in the stores! Up, the lot of you!\"", callShort = "Thieves!",
                enrage = "Hesk kicks the desk aside. The ledger can wait." },
            new EliteMove { mob = "Old Whitefoot", name = "Throat-Lunge", windup = 1.5f, blow = 3.5f, reach = 4.2f, every = 9, callReach = 32,
                call = "Old Whitefoot throws back its head and howls. The wood answers.", callShort = "Howl",
                enrage = "Old Whitefoot's hackles rise. It stops circling." },
            // ---------- Khaven ----------
            new EliteMove { mob = "The Grey Sexton", name = "Gravedigger's Swing", windup = 2.2f, blow = 4.3f, reach = 3.8f, every = 11, callReach = 40, callMost = 2,
                call = "The Grey Sexton beats its spade on a stone, and the graves listen.", callShort = "Knell",
                enrage = "The Grey Sexton digs faster, as if the grave were late." },
            // No pale camp stands in Khaven today: the Reckoner's call is ready for one, and until then nobody answers it.
            new EliteMove { mob = "The Pale Reckoner", name = "The Reckoning", windup = 2.4f, blow = 4.6f, reach = 3.6f, every = 12,
                call = "The Pale Reckoner raises one hand, and the pale things come to be counted.", callShort = "Tally",
                enrage = "The Pale Reckoner stops counting and starts collecting." },
            // ---------- The Peaks ----------
            new EliteMove { mob = "Sandthrone captain", name = "Eyrie Cleave", windup = 1.7f, blow = 3.8f, reach = 3.6f, every = 10, callReach = 50, callMost = 2,
                call = "The captain roars: \"Toll-guard! To the eyrie!\"", callShort = "To the eyrie!",
                enrage = "The captain throws caution after his shield." },
            new EliteMove { mob = "Old Scree-Tusk", name = "Tusk-Heave", windup = 1.8f, blow = 4, reach = 4.4f, every = 10,
                enrage = "Old Scree-Tusk squeals and goes red in the eye." },
            // ---------- The Ash Rim ----------
            new EliteMove { mob = "Brood Weave-Eater", name = "Fraying Lash", windup = 2, blow = 3.8f, reach = 4, every = 10,
                call = "The Brood Weave-Eater shrills, and the brood stirs.", callShort = "Shrill",
                enrage = "The Brood Weave-Eater's lashes blur." },
            new EliteMove { mob = "The Ash-Deacon", name = "Cinder Benediction", windup = 2.2f, blow = 4, reach = 3.8f, every = 11,
                call = "The Ash-Deacon cries: \"Witness! Come and witness!\"", callShort = "Witness!",
                enrage = "The Ash-Deacon laughs through the mask and swings the censer wide." },
            // ---------- The Verdant Shore ----------
            new EliteMove { mob = "Greyheart", name = "Felling Stroke", windup = 2.3f, blow = 4.3f, reach = 4, every = 11,
                call = "Greyheart groans like a tree in a gale, and the grey ones turn.", callShort = "Groan",
                enrage = "Greyheart's bark splits. What is under it is angrier." },
            new EliteMove { mob = "Old Ninebranch", name = "Nine-Tine Toss", windup = 1.6f, blow = 3.8f, reach = 4.4f, every = 9,
                enrage = "Old Ninebranch lowers all nine tines and paws the moss." },
            new EliteMove { mob = "The Hollow Root-Warden", name = "The Root's Weight", boss = true, health = BossHealth, hit = BossHit, windup = 2.2f, blow = 3.6f, reach = 4.2f, every = 11,
                call = "The Hollow Root-Warden strikes the floor, and the Heart answers.", callShort = "The Heart answers", enrageAt = .35f, enrageHaste = .6f,
                enrage = "The Hollow Root-Warden creaks, and every root in the Heart pulls tight." },
            // ---------- The Sealed Adit (dungeon D1; PROVISIONAL moves until D2 gives each boss its mechanics, DUNGEON_DESIGN.md) ----------
            new EliteMove { mob = "Gang-Boss Haddo Lusk", name = "Tally-Stick", windup = 2.2f, blow = 3.6f, reach = 3.8f, every = 12, callMost = 2,
                deathCall = 3, deathText = "Boots in the drift behind: Lusk's relief shift walks in, three of them.",   // the first boss: a lesson, not a wall (the paper fight: his two guards answer, nobody else)
                call = "Lusk roars: \"Overseers! Earn your cut!\"", callShort = "Earn your cut!",
                enrage = "Lusk throws the tally book down and comes round the table." },
            new EliteMove { mob = "Nix, the turncoat", name = "Spanner-Lock", windup = 1.6f, blow = 3.6f, reach = 3.4f, every = 9, disarm = 4,
                call = "Nix shrieks: \"Get them off me!\"", callShort = "Get them off me!",
                enrage = "Nix's hands blur over the engine's controls." },
            new EliteMove { mob = "Cinder-Warden Ysolt", name = "Ember Blow", windup = 2, blow = 4.2f, reach = 3.8f, every = 10, callReach = 14, callMost = 2,
                knock = 5, ringEvery = 15, ringReach = 4.5f, ringBlow = 1.6f, ringName = "Flame Ring",
                call = "Ysolt lifts the brand: \"The fire sees you.\"", callShort = "The fire sees you",
                enrage = "Ysolt steps into the heat and comes out burning." },
            new EliteMove { mob = "The Foreman Who Forgot", name = "What the Grey Takes", windup = 2.4f, blow = 4.4f, reach = 3.6f, every = 12, callReach = 40, callMost = 2,
                blinkEvery = 14, phases = new[] {
                    new BossPhase { at = .5f, kind = "reset", text = "The Foreman stops. He looks at his hands, then at you, as if for the first time." },
                    new BossPhase { at = .25f, kind = "terrify", seconds = 3, text = "The Foreman opens his mouth and the grey comes out of it. You cannot move." } },   // the Hollow drift, back up the breach
                call = "The Foreman calls a shift-name nobody has answered to in years. Something in the grey answers.", callShort = "Shift!",
                enrage = "The Foreman flickers, and for a moment there is less of him." },
            // The rare (D3, one visit in five): a slow, heavy swing of a pick that has worked this drift since before the seal; alone, he calls nobody.
            new EliteMove { mob = "The Quiet Miner", name = "The Last Swing of the Shift", windup = 2.3f, blow = 4.6f, reach = 3.8f, every = 11,
                enrage = "The Quiet Miner stops working. For the first time, he looks at you." },
            new EliteMove { mob = "The Vent-Hound", name = "Ember Lunge", windup = 1.5f, blow = 3.6f, reach = 4.4f, every = 9,
                enrage = "The Vent-Hound's coat glows along the spine." },
            new EliteMove { mob = "Quartermaster Brannigan Sorrel", name = "Boot-Heel Stamp", windup = 2, blow = 4.2f, reach = 3.6f, every = 10, callReach = 14, callMost = 2,
                phases = new[] {
                    new BossPhase { at = .667f, kind = "stamp", seconds = 2, text = "Sorrel stamps: the platform jumps under you. He goes to the rack and comes back with a maul." },
                    new BossPhase { at = .333f, kind = "stamp", seconds = 2, text = "Sorrel stamps again, and this time comes back with the poleaxe." } },   // his guards, not the captain down the hall
                call = "Sorrel bellows: \"Platform! Hold the ramp!\"", callShort = "Hold the ramp!",
                enrage = "Sorrel goes to the rack and comes back with something heavier." },
            new EliteMove { mob = "Rail-Captain Orsk Danner", name = "The Dead Line", boss = true, callOnPull = true, phases = new[] { new BossPhase { at = .5f, kind = "rally", text = "Danner, without turning: \"More.\"" } }, health = BossHealth, hit = BossHit, windup = 2.2f, blow = 3.2f, reach = 4, every = 12, callReach = 10,   // sturdy, not savage: his guards and the shadows are the fight (the paper fight at level 12)
                call = "Danner says, quietly: \"Guards.\" They step out of the dark.", callShort = "Guards.", enrageAt = .35f, enrageHaste = .6f,
                enrage = "Danner draws a second blade. The letter stays buttoned in his coat." },
        };
        static readonly EliteMove Blow = new EliteMove { name = "Heavy Blow" }, Lunge = new EliteMove { name = "Savage Lunge", windup = 1.7f, blow = 3.8f, reach = 4.2f, every = 10 };
        /// <summary>The move of the elite a camp names; a plain heavy blow (a lunge, for a beast) when it is not one of the twelve.</summary>
        public static EliteMove For(string mob, bool beast)
        {
            var found = Array.Find(All, m => m.mob == mob);
            return found ?? (beast ? Lunge : Blow);
        }
    }
}
