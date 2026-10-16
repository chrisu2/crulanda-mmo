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
            new EliteMove { mob = "The Rock-Eater", name = "Rock-Crush", windup = 2, blow = 3.0f, reach = 3.6f, every = 15.54f, knock = 3, health = .9f, hit = .5f, callReach = 42, callMost = 1,   // health .9 (was 1.15) and the Shaken on whom it fights only (was all within 5 m), 2026-10-15 17:10: adit-final4 still wiped (the tank died at 66%; every swing is capped at 60% of the victim's health, so blow and goad cuts never reached the tank); eased 2026-10-15 16:50 (blow 3.8, every 10, knock 4, health 1.3, two called): three sim parties wiped at the duo, the tank knocked down every 14 s   // health 1.3 (was .95): the duo (2026-10-11, Chris: "the duo boss needs to be tougher"; 1.6 with the mend was a stalemate)
                cc = "disorient", ccName = "Shaken", ccSeconds = 3, ccEvery = 22.2f, ccReach = 0, ccText = "The floor heaves under the Rock-Eater: the cave swims round you.",
                call = "The Rock-Eater bellows across the workshop. The guards come running.", callShort = "Bellow",
                enrage = "The crystals on the Rock-Eater's back blaze violet and it tears at the floor." },
            new EliteMove { mob = "Nix, the turncoat", name = "Goad-Hook", windup = 1.6f, blow = 3.6f, reach = 3.4f, every = 9.99f, disarm = 4, health = .8f, callReach = 66, holdable = true,   // (22 before the Adit was drawn out); holdable: the duo's play is to hold him
                cc = "fear", ccName = "Shriek", ccSeconds = 3, ccEvery = 27.75f, ccReach = 4,   // the duo's moves 11% further apart (Chris, 2026-10-15 17:40: "up these number 12%" / "no i meant time to cast..should be longer"; 18:00: 12% won with the tank never low, "move timer to 11%")
                ccText = "Nix shrieks in your face and you run before you know it.",
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
            // ---------- The Lowtowns (Phase 9.1, Docs/ZONE_LOWTOWNS.md section 4; GAME-ONLY) ----------
            // The Mother of Drains: a great Weave-Eater in the deepest court, at night only; salt calcifies her once a fight and a harpoon pins her.
            new EliteMove { mob = "Mother of Drains", name = "Drowning Coil", windup = 2.1f, blow = 4.1f, reach = 4.4f, every = 11, callReach = 30, callMost = 3,
                cc = "root", ccName = "Coiled", ccSeconds = 3, ccEvery = 16, ccReach = 5, ccText = "The Mother of Drains wraps the wet dark round your legs.",
                call = "The Mother of Drains lets out a sound like water going down a drain, and the courts answer.", callShort = "The drains answer",
                enrage = "The Mother of Drains swells until the court's lamps gutter." },
            // Knuckle Brannoc: the Iron Teeth's yard-boss by the Outfall, with his two sifters.
            new EliteMove { mob = "Knuckle Brannoc", name = "Iron Knuckles", windup = 1.7f, blow = 3.9f, reach = 3.5f, every = 10, callReach = 34, callMost = 2, knock = 3,
                call = "Brannoc bangs his iron knuckles together: \"Teeth! Yard!\"", callShort = "Teeth! Yard!",
                enrage = "Brannoc spits out a tooth and grins with the rest." },
            // The rare (one visit in four): a Weave-Eater that drinks the lamps of one lane dark as it goes.
            new EliteMove { mob = "The Lantern-Eater", name = "Lamp-Drinker", windup = 2, blow = 3.8f, reach = 4, every = 11,
                cc = "disorient", ccName = "Snuffed", ccSeconds = 3, ccEvery = 17, ccReach = 6, ccText = "The Lantern-Eater drinks the light out of your eyes.",
                enrage = "Every lamp in the lane goes out at once." },
            new EliteMove { mob = "Old Kettle, the pitch-boiler", name = "Ladle", windup = 2, blow = 4, reach = 3.6f, every = 11, ringEvery = 14, ringReach = 4.5f, ringName = "Hot Pitch",
                cc = "root", ccName = "Stuck in the pitch", ccSeconds = 2, ccEvery = 20, ccReach = 4.5f, ccText = "The pitch takes your boots and holds them.",
                callReach = 26, callMost = 2, call = "Old Kettle bangs the ladle on the pot: \"Who's hungry?\"", callShort = "Who's hungry?", enrage = "Old Kettle tips the pot over." },
            // ---------- Hallow's Creek (Docs/DUNGEON2_DESIGN.md section 2; levels 15-20; GAME-ONLY people over the CANON Bureau, Iron Pact
            // and Hollow). Every hold reaches past the tank; holds on them follow Chris's rule (half the time, each kind once a fight). ----------
            new EliteMove { mob = "Road-Captain Everard Hurst", name = "Carter's Shove", windup = 1.8f, blow = 3.8f, reach = 3.8f, every = 11, callReach = 20, callMost = 2,
                cc = "stun", ccName = "Down in the ash!", ccSeconds = 2, ccEvery = 14, ccReach = 5, ccText = "Hurst bawls \"Down in the ash!\" and his shoulder puts you on your back.",
                call = "Hurst roars down the road: \"Carters! Leave the sacks!\"", callShort = "Leave the sacks!", enrage = "Hurst spits ash and comes on with both fists." },
            new EliteMove { mob = "The Old Ram", name = "Paper Horns", windup = 1.6f, blow = 3.8f, reach = 4.2f, every = 10, knock = 3,
                cc = "disorient", ccName = "Butted", ccSeconds = 3, ccEvery = 18, ccReach = 0, ccText = "The old ram's grey horns take you in the ribs, light as paper and hard as stone.",
                enrage = "The Old Ram lowers its head and paws the ash." },
            new EliteMove { mob = "The Grey Carter", name = "Whip of Dust", windup = 1.8f, blow = 3.8f, reach = 4.4f, every = 11,
                cc = "root", ccName = "Wound in the reins", ccSeconds = 3, ccEvery = 16, ccReach = 0, ccText = "The Grey Carter's reins of dust wind round your legs.",
                enrage = "The Grey Carter cracks a whip that is not there, and something in the road answers." },
            new EliteMove { mob = "Sergeant Wymond Tredway", name = "Shield-Edge", windup = 1.7f, blow = 3.8f, reach = 3.6f, every = 10, callReach = 20, callMost = 2,
                cc = "root", ccName = "Hold the span!", ccSeconds = 3, ccEvery = 12, ccReach = 0, ccText = "\"Hold the span!\" Tredway's shield pins you where you stand.",
                phases = new[] { new BossPhase { at = .5f, kind = "rally", text = "Tredway locks his shield with his men's: \"Shield-wall! Hold the span!\"" } },
                call = "Tredway bellows: \"Line! On me!\"", callShort = "On me!", enrage = "Tredway drops the shield and draws the short sword." },
            new EliteMove { mob = "The Fallen Mason", name = "Trowel-Edge", windup = 2, blow = 4, reach = 3.8f, every = 11, ringEvery = 15, ringReach = 4.5f, ringBlow = 1.4f, ringName = "Wet Mortar",
                cc = "root", ccName = "Set in the mortar", ccSeconds = 3, ccEvery = 16, ccReach = 4, ccText = "The grey mortar round your boots sets hard.",
                enrage = "The Fallen Mason lays a course of nothing between you and the edge." },
            new EliteMove { mob = "Assessor Piers Upcott", name = "Tally-Rod", windup = 1.8f, blow = 3.8f, reach = 3.6f, every = 11, callReach = 20, callMost = 2,
                cc = "stun", ccName = "Marked for the cut", ccSeconds = 3, ccEvery = 15, ccReach = 0, ccText = "Upcott chalks a cross on you, and you go to stone where you stand.",
                call = "Upcott taps the chalk on his board: \"More hands. This one's for the cut.\"", callShort = "More hands", enrage = "Upcott throws the chalk away and takes up the cutter's mallet." },
            new EliteMove { mob = "The Lamplighter", name = "Pole-Hook", windup = 1.9f, blow = 3.8f, reach = 4.2f, every = 11,
                cc = "fear", ccName = "Lights out", ccSeconds = 4, ccEvery = 18, ccReach = 6, ccText = "The Lamplighter puts out a lamp that was never lit, and the dark comes after you.",
                enrage = "The Lamplighter lights every dead lamp on the street at once, and none of them gives light." },
            new EliteMove { mob = "Picker-Master Fulke Yelland", name = "Sorting Hook", windup = 1.8f, blow = 3.8f, reach = 3.8f, every = 11, callReach = 20, callMost = 2,
                cc = "disorient", ccName = "Bag him", ccSeconds = 3, ccEvery = 14, ccReach = 0, ccText = "\"Bag him!\" A sack comes down over your head.",
                phases = new[] { new BossPhase { at = .5f, kind = "rally", text = "Yelland bangs the hook on a crate: \"Sort them! Sort them!\"" } },
                call = "Yelland shouts down the loft: \"Pickers! To the stair!\"", callShort = "To the stair!", enrage = "Yelland kicks the tally off the loft and comes on with the hook." },
            new EliteMove { mob = "Gil Coldwell, the Loft Watch", name = "Net-Bow Butt", windup = 1.7f, blow = 3.6f, reach = 3.6f, every = 10, callReach = 18, callMost = 2,
                cc = "root", ccName = "Netted", ccSeconds = 3, ccEvery = 13, ccReach = 0, ccText = "Coldwell's net-bow throws a weighted net over you.",
                call = "Coldwell whistles down from the high loft.", callShort = "A whistle", enrage = "Coldwell drops the net-bow and draws a long knife." },
            new EliteMove { mob = "First-Cut", name = "Broken Shell", windup = 2, blow = 4, reach = 3.8f, every = 11, callAt = .5f, callReach = 18, callMost = 3,
                cc = "fear", ccName = "The shriek", ccSeconds = 3, ccEvery = 20, ccReach = 6, ccText = "First-Cut opens its mouth and the cellar fills with a sound like tearing metal: you run.",
                call = "First-Cut shrieks, and the niches in the walls answer.", callShort = "The shriek", enrage = "First-Cut's grey splits along the old chisel-line, and it does not slow." },
            new EliteMove { mob = "The Long Shadow", name = "Cold Hand", windup = 2.2f, blow = 4, reach = 3.8f, every = 12,
                cc = "stun", ccName = "A minute taken", ccSeconds = 4, ccEvery = 18, ccReach = 4, ccText = "The Long Shadow passes over you and takes a minute: you stand in it, unable to move.",
                enrage = "The Long Shadow stretches across the whole cellar floor." },
            new EliteMove { mob = "Registrar Dunstan Pardew", name = "Ledger-Spine", windup = 1.9f, blow = 3.8f, reach = 3.6f, every = 11, callReach = 20, callMost = 2,
                cc = "stun", ccName = "Hush", ccSeconds = 2, ccEvery = 16, ccReach = 5, ccText = "\"Hush.\" Pardew's voice is very quiet, and you cannot move for it.",
                phases = new[] { new BossPhase { at = .5f, kind = "loose", camp = "Clerks from the cells", text = "Pardew, without looking up: \"Copy that.\" Two clerks come out of the copying cells with their pens still wet." } },
                call = "Pardew rings a small brass bell on the desk.", callShort = "The desk bell", enrage = "Pardew closes the register, and opens it again at the back." },
            new EliteMove { mob = "The Reader at the Desk", name = "Turned Page", windup = 1.8f, blow = 3.6f, reach = 3.6f, every = 11,
                cc = "disorient", ccName = "Read to me", ccSeconds = 3, ccEvery = 15, ccReach = 0, ccText = "\"Read to me,\" the Reader says, and the words on the page swim in your eyes.",
                enrage = "The Reader closes the book. The stacks lean in to listen." },
            new EliteMove { mob = "Pact-Captain Rowena Kersey", name = "Pact Halberd", windup = 2, blow = 3.6f, reach = 3.8f, every = 12, callReach = 16, callMost = 2,
                cc = "root", ccName = "Form the line", ccSeconds = 3, ccEvery = 15, ccReach = 4, ccText = "\"Form the line!\" Kersey's halberd hooks your legs from under you.",
                call = "Kersey calls up the tower: \"Line! Down the stair!\"", callShort = "Line!", enrage = "Kersey plants the halberd and draws her sword." },
            new EliteMove { mob = "Bureau Examiner Thurstan Vowell", name = "The Seal", boss = true, health = BossHealth, hit = BossHit, windup = 2.2f, blow = 4, reach = 3.8f, every = 12, callReach = 12, callMost = 2,
                cc = "stun", ccName = "The hour", ccSeconds = 3, ccEvery = 18, ccReach = 8, ccText = "The bell hums under the Examiner's hand and the hour comes for you: stone, where you stand.",
                phases = new[] { new BossPhase { at = .5f, kind = "event", camp = "waking", text = "Vowell takes hold of the bell rope. \"It is nearly noon,\" he says. \"It is always nearly noon.\"" } },
                call = "Vowell: \"Clerks. Take their names.\"", callShort = "Take their names", enrageAt = .35f, enrageHaste = .6f, enrage = "Vowell lets go of the rope. The bell goes on ringing without him." },
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
