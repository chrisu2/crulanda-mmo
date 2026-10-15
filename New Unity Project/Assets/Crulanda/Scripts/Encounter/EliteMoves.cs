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
        // A hold on the party (Chris, 2026-10-11: "needs some of the bosses to stun or cc members of the party? maybe freeze? stun? fear?
        // disorient?"): cc "stun", "root", "fear" or "disorient", every ccEvery seconds for ccSeconds, on everyone within ccReach of it
        // (0: on whoever it fights). EncounterEnemy.Boss.HoldParty; EncounterSession.HoldPlayer and SimCompanion.HoldUntil take it.
        public string cc, ccName, ccText; public float ccEvery, ccSeconds = 3, ccReach;
        /// <summary>Held by the party's crowd control for the full time, no immunity and no diminishing (the duo's Nix).</summary>
        public bool holdable;
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
            new EliteMove { mob = "Gang-Boss Haddo Lusk", name = "Tally-Stick", windup = 2.2f, blow = 3.6f, reach = 3.8f, every = 12, callMost = 2, callReach = 66,
                cc = "stun", ccName = "Floored", ccSeconds = 2, ccEvery = 14, ccText = "Haddo's tally-stick takes the legs from under you.",   // (the default 22 in the first Adit; drawn out three times)
                deathCall = 3, deathText = "Boots in the drift behind: Lusk's relief shift walks in, three of them.",   // the first boss: a lesson, not a wall (the paper fight: his two guards answer, nobody else)
                call = "Lusk roars: \"Overseers! Earn your cut!\"", callShort = "Earn your cut!",
                enrage = "Lusk throws the tally book down and comes round the table." },
            // Nix's two-part fight (D5): first his engine, then him on foot. Together about one and three-quarter elites of health.
            new EliteMove { mob = "The Rock-Eater", name = "Rock-Crush", windup = 2, blow = 3.8f, reach = 3.6f, every = 10, knock = 4, health = 1.3f, callReach = 42, callMost = 2,   // health 1.3 (was .95): the duo (2026-10-11, Chris: "the duo boss needs to be tougher"; 1.6 with the mend was a stalemate)
                cc = "disorient", ccName = "Shaken", ccSeconds = 3, ccEvery = 20, ccReach = 5, ccText = "The floor heaves under the Rock-Eater: the cave swims round you.",
                call = "The Rock-Eater bellows across the workshop. The guards come running.", callShort = "Bellow",
                enrage = "The crystals on the Rock-Eater's back blaze violet and it tears at the floor." },
            new EliteMove { mob = "Nix, the turncoat", name = "Goad-Hook", windup = 1.6f, blow = 3.6f, reach = 3.4f, every = 9, disarm = 4, health = .8f, callReach = 66, holdable = true,   // (22 before the Adit was drawn out); holdable: the duo's play is to hold him
                cc = "fear", ccName = "Shriek", ccSeconds = 3, ccEvery = 25, ccReach = 4, ccText = "Nix shrieks in your face and you run before you know it.",
                deathText = "Boots on the far ramp behind you: the Berth's night crew comes down it, three cutters.",   // his death releases the Berth night crew (ADIT_REWORK 5.3; 2026-10-15)
                call = "Nix shrieks: \"Get them off me!\"", callShort = "Get them off me!",
                enrage = "Nix stops dodging and comes at you with the goad in both hands." },
            new EliteMove { mob = "Cinder-Warden Ysolt", name = "Ember Blow", windup = 2, blow = 4.2f, reach = 3.8f, every = 10, callReach = 42, callMost = 2,
                cc = "fear", ccName = "The fire sees you", ccSeconds = 4, ccEvery = 16, ccReach = 6, ccText = "Ysolt lifts the brand and the fire looks back at you: you run from it.",
                knock = 5, ringEvery = 15, ringReach = 4.5f, ringBlow = 1.6f, ringName = "Flame Ring",
                deathText = "A bell rings in the cloister. Three zealots come in at the chapel's other door, behind you.",   // her death releases The cloister bell (ADIT_REWORK 5.6; 2026-10-15)
                call = "Ysolt lifts the brand: \"The fire sees you.\"", callShort = "The fire sees you",
                enrage = "Ysolt steps into the heat and comes out burning." },
            new EliteMove { mob = "The Foreman Who Forgot", name = "What the Grey Takes", windup = 2.4f, blow = 4.4f, reach = 3.6f, every = 12, callReach = 120, callMost = 2,
                blinkEvery = 14, phases = new[] {
                    new BossPhase { at = .5f, kind = "reset", text = "The Foreman stops. He looks at his hands, then at you, as if for the first time." },
                    new BossPhase { at = .25f, kind = "terrify", seconds = 3, text = "The Foreman opens his mouth and the grey comes out of it. You cannot move." } },   // the Hollow drift, back up the breach
                deathText = "Something in the walls lets go. Two Hollow Men come out of the grey behind you: what the grey kept.",   // his death releases What the grey kept (ADIT_REWORK 5.8; 2026-10-15)
                call = "The Foreman calls a shift-name nobody has answered to in years. Something in the grey answers.", callShort = "Shift!",
                enrage = "The Foreman flickers, and for a moment there is less of him." },
            // The rare (D3, one visit in five): a slow, heavy swing of a pick that has worked this drift since before the seal; alone, he calls nobody.
            new EliteMove { mob = "The Quiet Miner", name = "The Last Swing of the Shift", windup = 2.3f, blow = 4.6f, reach = 3.8f, every = 11,
                enrage = "The Quiet Miner stops working. For the first time, he looks at you." },
            new EliteMove { mob = "The Vent-Hound", name = "Ember Lunge", windup = 1.5f, blow = 3.6f, reach = 4.4f, every = 9,
                enrage = "The Vent-Hound's coat glows along the spine." },
            new EliteMove { mob = "Quartermaster Brannigan Sorrel", name = "Boot-Heel Stamp", windup = 2, blow = 4.2f, reach = 3.6f, every = 10, callReach = 30, callMost = 2,   // (14 in the first Adit; 48 brought the gunners until the carriage moved to Danner, who stands 32 m off now, 2026-10-11: his guard answers, never he)
                phases = new[] {
                    new BossPhase { at = .667f, kind = "stamp", seconds = 2, text = "Sorrel stamps: the platform jumps under you. He goes to the rack and comes back with a maul." },
                    new BossPhase { at = .333f, kind = "stamp", seconds = 2, text = "Sorrel stamps again, and this time comes back with the poleaxe." } },   // his guards, not the captain down the hall
                deathText = "Boots at the line's far end: Sorrel's dockers come up the platform, three of them.",   // his death releases Sorrel's dockers (ADIT_REWORK 5.10; 2026-10-15)
                call = "Sorrel bellows: \"Platform! Hold the ramp!\"", callShort = "Hold the ramp!",
                enrage = "Sorrel goes to the rack and comes back with something heavier." },
            new EliteMove { mob = "Rail-Captain Orsk Danner", name = "The Dead Line", boss = true, callOnPull = true, cc = "root", ccName = "Chained", ccSeconds = 4, ccEvery = 14, ccText = "Danner's coupling chain wraps your legs: you stand where you are.", phases = new[] { new BossPhase { at = .5f, kind = "rally", text = "Danner, without turning: \"More.\"" } }, health = BossHealth, hit = BossHit, windup = 2.2f, blow = 3.2f, reach = 4, every = 12, callReach = 10,   // sturdy, not savage: his guards and the shadows are the fight (the paper fight at level 12)
                call = "Danner says, quietly: \"Guards.\" They step out of the dark.", callShort = "Guards.", enrageAt = .35f, enrageHaste = .6f,
                enrage = "Danner draws a second blade. The letter stays buttoned in his coat." },
            // The reworked Adit's elites (Docs/ADIT_REWORK.md section 5; 2026-10-14), with the moves the engine has today. What section 5
            // marks NEW (a cast to interrupt, a phase that looses more, a boss raised by her sister, a death that releases a shift) waits
            // for build step S4. GAME-ONLY.
            new EliteMove { mob = "Overseer Grael Vosk", name = "Lash", windup = 1.8f, blow = 3.8f, reach = 4.2f, every = 10, callAt = .5f, callReach = 50, callMost = 3,
                cc = "root", ccName = "Lashed", ccSeconds = 3, ccEvery = 12, ccText = "Vosk's whip wraps your shins: you stand where you are.",
                call = "Vosk cracks the whip at the catwalk: \"Drive them!\"", callShort = "Drive them!", enrage = "Vosk throws the whip down and takes up the pick." },
            new EliteMove { mob = "Urn-Keeper Sabel", name = "Cinerary Blow", windup = 2, blow = 4, reach = 3.8f, every = 11, callReach = 30, callMost = 2,
                cc = "disorient", ccName = "Ash in the Eyes", ccSeconds = 3, ccEvery = 18, ccReach = 6, ccText = "Sabel tips an urn and the ash fills your eyes.",
                phases = new[] {   // the urn-risen climb out at two thirds and a third (ADIT_REWORK.md section 5 item 4; the camps are "loose:Urn-Keeper Sabel"; 2026-10-15)
                    new BossPhase { at = .66f, kind = "loose", camp = "Urn-risen (first)", text = "Sabel raps on the nearest urn and its lid lifts from inside: the Ash-bound dead climb out, grey to the eyes." },
                    new BossPhase { at = .33f, kind = "loose", camp = "Urn-risen (second)", text = "Sabel kicks over the second urn. More of the dead climb out of the ash." } },
                call = "Sabel knocks on an urn: \"Wake, and serve.\"", callShort = "Wake, and serve", enrage = "Sabel breaks the last urn over her own head and laughs." },
            new EliteMove { mob = "Loremaster Ivo Senn", name = "Iron-Bound Spine", windup = 2.2f, blow = 4, reach = 3.6f, every = 11, callOnPull = true, callReach = 30, callMost = 2,
                cc = "stun", ccName = "Lullaby of Ash", ccSeconds = 4, ccEvery = 18, ccReach = 6, ccText = "Senn reads a line of the old hymn and your eyes close.",
                call = "Senn snaps the book shut: \"Copyists. To me.\"", callShort = "Copyists, to me", enrage = "Senn tears the page out and reads it aloud." },
            new EliteMove { mob = "Master-at-Arms Corvane", name = "Shield-Bash", windup = 1.6f, blow = 3.8f, reach = 3.4f, every = 10, callOnPull = true, callReach = 30, callMost = 3, knock = 3,
                cc = "stun", ccName = "Shield-bashed", ccSeconds = 2, ccEvery = 12, ccText = "Corvane's shield-rim takes you on the jaw.",
                phases = new[] { new BossPhase { at = .5f, kind = "loose", camp = "The Vent-Hound", text = "Corvane bellows down the yard: \"Loose the hound!\"" } },   // if it still lives it leaves the kennel and joins (section 5 item 5; 2026-10-15)
                call = "Corvane raps his shield: \"Form up!\"", callShort = "Form up!", enrage = "Corvane sets his feet and stops giving ground." },
            new EliteMove { mob = "Sister Ashe Varrow", name = "Brand-Strike", windup = 2, blow = 3.6f, reach = 3.6f, every = 11, callReach = 8,
                ringEvery = 14, ringReach = 4.5f, ringName = "Kindling",
                call = "Varrow sets her hand on the brazier: \"Sister, I am here.\"", callShort = "Sister, I am here", enrage = "Varrow's robe catches and she does not put it out." },
            new EliteMove { mob = "The Grey Whisperer", name = "Hush", windup = 2, blow = 3.8f, reach = 3.6f, every = 11, callAt = .4f, callReach = 40, callMost = 3,
                cc = "stun", ccName = "The Long Lull", ccSeconds = 4, ccEvery = 14, ccReach = 8, ccText = "The whisper reaches you and you forget to move.",
                call = "The Whisperer whispers, and the crawlers who fled come back out of the grey.", callShort = "Come back", enrage = "The Whisperer stops whispering." },
            new EliteMove { mob = "Brother Hollowsong", name = "The Unsung", boss = true, health = BossHealth, hit = BossHit, windup = 2.2f, blow = 4.2f, reach = 3.8f, every = 11, callAt = .5f, callReach = 40, callMost = 3,
                cc = "stun", ccName = "The Unsung", ccSeconds = 3, ccEvery = 18, ccReach = 8, ccText = "Hollowsong sings the note nobody sings and you cannot move for hearing it.",
                call = "Hollowsong's choir comes up the shaft stair, singing nothing.", callShort = "The choir", enrage = "Hollowsong's song goes on without him." },
            new EliteMove { mob = "Brack the Bailer", name = "Bucket-Swing", windup = 1.8f, blow = 3.8f, reach = 3.8f, every = 10, knock = 4, callReach = 26, callMost = 2,
                cc = "disorient", ccName = "Bucket Douse", ccSeconds = 3, ccEvery = 16, ccReach = 4, ccText = "Brack empties the bucket over you, sump-water and all.",
                call = "Brack bangs the bucket on the chain: \"Bail!\"", callShort = "Bail!", enrage = "Brack wades in up to the waist and keeps coming." },
            new EliteMove { mob = "Old Bristle", name = "Charge", windup = 1.5f, blow = 3.8f, reach = 4.6f, every = 9, knock = 5,
                cc = "disorient", ccName = "Trample", ccSeconds = 3, ccEvery = 18, ccReach = 5, ccText = "Old Bristle's trotters go over you.", enrage = "Old Bristle's bristles stand straight up." },
            // (S7 tuning, 2026-10-15) The Old Burrower is level 13 in a level-11 wing and optional: its throw 6 to 4 m and its Burrow's reach 6 to
            // 5 m, so the party is not thrown out of its own healer's reach round the Lurker's Pool; Old Kettle's pitch holds 2 s, not 3, every 20
            // s, not 18, so it seldom holds you in his Hot Pitch ring (both on a 14 s clock). Worth the side trip, not a wall on it.
            new EliteMove { mob = "The Old Burrower", name = "Earth-Heave", windup = 2, blow = 4, reach = 3.8f, every = 10, knock = 4,
                cc = "disorient", ccName = "Burrow", ccSeconds = 3, ccEvery = 16, ccReach = 5, ccText = "The Old Burrower goes under and comes up beneath you.", enrage = "The Old Burrower stops circling." },
            new EliteMove { mob = "Powder-Sergeant Hobb", name = "Fuse-Pick", windup = 1.8f, blow = 3.8f, reach = 3.6f, every = 10, callReach = 30, callMost = 2,
                ringEvery = 12, ringReach = 4.5f, ringName = "Short Fuse",
                call = "Hobb whistles for the sappers.", callShort = "Sappers!", enrage = "Hobb lights the whole bandolier." },
            new EliteMove { mob = "Old Kettle, the pitch-boiler", name = "Ladle", windup = 2, blow = 4, reach = 3.6f, every = 11, ringEvery = 14, ringReach = 4.5f, ringName = "Hot Pitch",
                cc = "root", ccName = "Stuck in the pitch", ccSeconds = 2, ccEvery = 20, ccReach = 4.5f, ccText = "The pitch takes your boots and holds them.",
                callReach = 26, callMost = 2, call = "Old Kettle bangs the ladle on the pot: \"Who's hungry?\"", callShort = "Who's hungry?", enrage = "Old Kettle tips the pot over." },
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
