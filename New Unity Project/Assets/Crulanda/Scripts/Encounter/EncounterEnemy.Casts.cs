using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>A mob's spell (Docs/CC_DESIGN.md section 5, step C5, 2026-10-08; GAME-ONLY): something worth interrupting.</summary>
    public sealed class MobCast
    {
        public string mob, name, kind;   // kind: "heal" (an ally under 70%), "bolt" (its foe), "drain" (its foe, healing itself), "blast" (all near it), "net" (a hold on the party round its foe)
        public float cast = 2.5f, every = 10, first = 4, power = 2.5f, reach = 25;
        /// <summary>(S6) A "net": the hold it lands (EliteMove.cc's kinds: "root", "stun", "disorient", "fear"), for how long, on everyone of the
        /// party within <see cref="area"/> metres of the foe it is thrown at.</summary>
        public string cc; public float seconds, area;
        /// <summary>(2026-10-15) A "bolt" or a "blast" that throws whoever it hits this many metres back from it, only ever onto walkable
        /// ground (EncounterEnemy.Knock, the elites' knockback). 0: none.</summary>
        public float knock;
        /// <summary>What the float text says as a "blast" goes off; empty: its name.</summary>
        public string landText;
    }
    /// <summary>
    /// The casts, by camp mob name: the Sealed Adit's casters first, then (2026-10-15, Chris: "also most mobs dont seem to be cc or aoe
    /// still a thing?") one signature move for every ordinary camp mob in every zone, so trash brings holds and area damage too, not only
    /// bosses. Docs/CC_DESIGN.md, "Signature moves for every mob". All GAME-ONLY. A beast's move is a short wind-up (under a second),
    /// not a spell, but it reads and is stopped the same way.
    /// </summary>
    public static class MobCasts
    {
        /// <summary>The kinds <see cref="EncounterEnemy"/> lands (its Land switch), and a net's holds (EncounterSession.HoldPlayer, SimCompanion.HoldUntil).</summary>
        public static readonly string[] Kinds = { "heal", "bolt", "drain", "blast", "net" }, Holds = { "root", "stun", "disorient", "fear" };
        public static readonly MobCast[] All = {
            new MobCast { mob = "Ash mender", name = "Ember Mend", kind = "heal", cast = 2.5f, every = 9, first = 3, power = .35f },
            new MobCast { mob = "Ash initiate", name = "Cinder Bolt", kind = "bolt", cast = 2f, every = 8, first = 4, power = 2.5f },
            new MobCast { mob = "Sandthrone sapper", name = "Short Fuse", kind = "blast", cast = 3f, every = 14, first = 5, power = 3f, reach = 4.5f, landText = "BOOM" },
            new MobCast { mob = "Hollow Man", name = "Grey Drain", kind = "drain", cast = 2.5f, every = 10, first = 4, power = 2f },
            // The Adit's shooters (D5, DUNGEON_DESIGN.md section 5): the yard's pickets open at range, the carriage gunners fire from the platform.
            new MobCast { mob = "Sandthrone picket", name = "Crossbow Bolt", kind = "bolt", cast = 1.5f, every = 9, first = 1, power = 2f, reach = 20 },
            new MobCast { mob = "Sandthrone carriage gunner", name = "Rail-shot", kind = "bolt", cast = 1.6f, every = 7, first = 2, power = 2.4f, reach = 25 },
            // (S6, the Black Cove; GAME-ONLY) The harbour netters on the scaffold throw a weighted net: rooted, everyone within 4 m of whom it is
            // thrown at, for 3 s. Interrupt the throw (it is a cast) and nobody is held.
            new MobCast { mob = "Sandthrone harbour netter", name = "Weighted Net", kind = "net", cc = "root", seconds = 3, area = 4, cast = 1.8f, every = 14, first = 3, power = 0, reach = 18 },

            // ---------- Signature moves for every ordinary mob (2026-10-15; CC_DESIGN.md; GAME-ONLY) ----------
            // People with weapons: the Sandthrone company at work in the Adit, its deserters and riders outside, the Concord's wardens.
            new MobCast { mob = "Sandthrone overseer", name = "Whip-Crack", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = 1.2f, every = 14, first = 5, power = 0, reach = 6 },
            new MobCast { mob = "Sandthrone digger", name = "Pick Sweep", kind = "blast", cast = 1.5f, every = 14, first = 6, power = 2f, reach = 3 },
            new MobCast { mob = "Sandthrone geode-carrier", name = "Geode Toss", kind = "bolt", cast = 1.4f, every = 15, first = 5, power = 2f, reach = 12, knock = 2.5f },
            new MobCast { mob = "Sandthrone watchman", name = "Lamp-Flash", kind = "net", cc = "disorient", seconds = 2, area = 0, cast = 1.4f, every = 15, first = 5, power = 0, reach = 10 },
            new MobCast { mob = "Sandthrone platform guard", name = "Haft-Shove", kind = "bolt", cast = 1.2f, every = 13, first = 5, power = 1.6f, reach = 4, knock = 3 },
            new MobCast { mob = "Sandthrone docker", name = "Bale-Hook", kind = "net", cc = "root", seconds = 2, area = 0, cast = 1.3f, every = 14, first = 5, power = 0, reach = 6 },
            new MobCast { mob = "Sandthrone geode-cutter", name = "Shard Spray", kind = "blast", cast = 1.5f, every = 15, first = 6, power = 2f, reach = 3.5f },
            new MobCast { mob = "Sandthrone sorter", name = "Cull-Stone", kind = "bolt", cast = 1.5f, every = 12, first = 4, power = 1.8f, reach = 18 },   // down off the catwalk
            new MobCast { mob = "Sandthrone smelter", name = "Slag Splash", kind = "blast", cast = 2f, every = 15, first = 5, power = 2.5f, reach = 4 },   // ADIT_REWORK.md section 5's new trash kits
            new MobCast { mob = "Sandthrone bargeman", name = "Barge-Pole Sweep", kind = "blast", cast = 1.6f, every = 16, first = 6, power = 1.6f, reach = 3.5f, knock = 2.5f },
            new MobCast { mob = "Sandthrone outrider", name = "Pommel Strike", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = 1.2f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Sandthrone deserter", name = "Dirty Kick", kind = "net", cc = "disorient", seconds = 1.5f, area = 0, cast = 1f, every = 15, first = 6, power = 0, reach = 4 },
            new MobCast { mob = "Sandthrone salt-raider", name = "Fistful of Salt", kind = "net", cc = "disorient", seconds = 2, area = 2.5f, cast = 1.3f, every = 15, first = 5, power = 0, reach = 6 },
            new MobCast { mob = "Sandthrone toll-guard", name = "Halberd Sweep", kind = "blast", cast = 1.5f, every = 15, first = 6, power = 1.8f, reach = 3.5f, knock = 2 },
            new MobCast { mob = "Hollow forager", name = "Sling Stone", kind = "bolt", cast = 1.5f, every = 14, first = 4, power = 1.6f, reach = 18 },
            new MobCast { mob = "Concord warden", name = "Warden's Manacle", kind = "net", cc = "root", seconds = 2, area = 0, cast = 1.5f, every = 15, first = 5, power = 0, reach = 5 },
            new MobCast { mob = "Grey-robed false pilgrim", name = "Sleeve-Knife", kind = "bolt", cast = 1f, every = 14, first = 4, power = 2.2f, reach = 4 },
            // The Ash cult.
            new MobCast { mob = "Ash zealot", name = "Fervent Cry", kind = "net", cc = "fear", seconds = 2, area = 5, cast = 1.8f, every = 16, first = 6, power = 0, reach = 8 },
            new MobCast { mob = "Ash scribe", name = "Cinder Bolt", kind = "bolt", cast = 2f, every = 12, first = 4, power = 2.2f, reach = 25 },   // ADIT_REWORK.md section 5's new trash kits
            new MobCast { mob = "Bone-masked cultist", name = "Ash Cloud", kind = "net", cc = "disorient", seconds = 2, area = 4, cast = 1.8f, every = 16, first = 5, power = 0, reach = 10 },
            // The risen dead.
            new MobCast { mob = "Ash-bound dead", name = "Grave-Grip", kind = "net", cc = "root", seconds = 2, area = 0, cast = 1f, every = 14, first = 4, power = 0, reach = 3.5f },
            new MobCast { mob = "Drowned dead", name = "Black Water", kind = "blast", cast = 1.5f, every = 15, first = 5, power = 1.8f, reach = 3.5f },
            // The grey, the Hollow, the Pale and the Wasting's other things.
            new MobCast { mob = "Greyed crawler", name = "Grey Clutch", kind = "net", cc = "root", seconds = 2, area = 0, cast = .8f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Grey whisperer", name = "Grey Lull", kind = "net", cc = "stun", seconds = 3, area = 0, cast = 2f, every = 16, first = 6, power = 0, reach = 15 },   // ADIT_REWORK.md's 4 s held to trash's 3
            new MobCast { mob = "Pale shadow", name = "Pale Dread", kind = "net", cc = "fear", seconds = 2, area = 3, cast = 1.6f, every = 16, first = 5, power = 0, reach = 10 },
            new MobCast { mob = "Pale watcher", name = "Unblinking Gaze", kind = "net", cc = "stun", seconds = 2, area = 0, cast = 1.8f, every = 16, first = 5, power = 0, reach = 15 },
            new MobCast { mob = "The Umbra Watcher", name = "Umbra-Fall", kind = "net", cc = "disorient", seconds = 2.5f, area = 4, cast = 2f, every = 15, first = 5, power = 0, reach = 12 },
            new MobCast { mob = "Weave-Eater", name = "Thread-Snare", kind = "net", cc = "root", seconds = 2, area = 3, cast = 1.2f, every = 15, first = 5, power = 0, reach = 8 },
            // The Lowtowns (Phase 9.1, Docs/ZONE_LOWTOWNS.md section 4): the back yards' hounds, the river thieves, the Iron Teeth, the Council's
            // hired warders and what the burners' cellar keeps.
            new MobCast { mob = "Gutter-hound", name = "Hamstring", kind = "net", cc = "root", seconds = 1.5f, area = 0, cast = .8f, every = 15, first = 5, power = 0, reach = 3.5f },
            new MobCast { mob = "Dock-rat cutter", name = "Thrown Knife", kind = "bolt", cast = 1.2f, every = 14, first = 4, power = 2f, reach = 10 },
            new MobCast { mob = "Dock-rat poler", name = "Pole Sweep", kind = "blast", cast = 1.5f, every = 15, first = 6, power = 2f, reach = 3.5f, knock = 2.5f },
            new MobCast { mob = "Iron Teeth runner", name = "Grit in the Eyes", kind = "net", cc = "disorient", seconds = 2, area = 0, cast = 1.1f, every = 15, first = 5, power = 0, reach = 5 },
            new MobCast { mob = "Iron Teeth sifter", name = "Slag Toss", kind = "bolt", cast = 1.4f, every = 14, first = 4, power = 2.2f, reach = 12 },
            new MobCast { mob = "Hired warder", name = "Cudgel to the Temple", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = 1.3f, every = 16, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Grey-touched", name = "The Forgetting", kind = "drain", cast = 2.2f, every = 15, first = 4, power = 1.8f, reach = 12 },
            // The Chemical River's (Lowtowns pick 6; CANON creatures, book1 ch.15:391): the sludge-eels' clinging bite, the mutated crocodile's roll.
            new MobCast { mob = "Sludge-eel", name = "Clinging Mouth", kind = "net", cc = "root", seconds = 1.5f, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 3.5f },
            // The Bestiary kit's two (Chris, 2026-10-18; Docs/BESTIARY_MONSTERS.md; GAME-ONLY): the Ember Vent's ember-bound lash the chain the cult
            // binds them with round whoever they fight (the glowing tethers, CANON, book1 ch.12:19); the bank-snouts butt with their tusks.
            new MobCast { mob = "Ember-bound", name = "Chain-Lash", kind = "net", cc = "root", seconds = 2, area = 0, cast = 1.2f, every = 15, first = 5, power = 0, reach = 6 },
            new MobCast { mob = "Bank-snout", name = "Tusk Butt", kind = "bolt", cast = .9f, every = 14, first = 4, power = 1.6f, reach = 3.5f, knock = 2 },
            new MobCast { mob = "The Old Croc", name = "Death Roll", kind = "net", cc = "stun", seconds = 2, area = 0, cast = 1f, every = 14, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Mist-walker", name = "Palemist Breath", kind = "net", cc = "disorient", seconds = 2, area = 4, cast = 1.6f, every = 16, first = 5, power = 0, reach = 8 },
            new MobCast { mob = "Void-Toucher", name = "Void Pulse", kind = "blast", cast = 1.8f, every = 15, first = 5, power = 2.2f, reach = 4 },
            new MobCast { mob = "Withered Keeper", name = "Grey Roots", kind = "net", cc = "root", seconds = 2.5f, area = 3, cast = 1.6f, every = 16, first = 5, power = 0, reach = 10 },
            new MobCast { mob = "Creeping briar", name = "Thorn Burst", kind = "blast", cast = .8f, every = 14, first = 4, power = 1.8f, reach = 3.5f },
            // The Forge District (Docs/ZONE_FORGE_DISTRICT.md, 2026-10-19; GAME-ONLY): the slag-hounds' heel-bite, the Blacklegs' thrown slag, the
            // Council's foundry overseers' cudgel; the rare Old Clinker's worry and the Spoke-Leech's thread-snare. No machines: thrown, swung, bitten.
            new MobCast { mob = "Slag-hound", name = "Heel-Bite", kind = "net", cc = "root", seconds = 1.5f, area = 0, cast = .8f, every = 15, first = 5, power = 0, reach = 3.5f },
            new MobCast { mob = "Blackleg", name = "Hot Slag", kind = "bolt", cast = 1.4f, every = 14, first = 4, power = 2.2f, reach = 12 },
            new MobCast { mob = "Foundry overseer", name = "Tally-Stick Cudgel", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = 1.3f, every = 16, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Old Clinker", name = "Worry", kind = "net", cc = "stun", seconds = 2, area = 0, cast = 1f, every = 14, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "The Spoke-Leech", name = "Rune-Snare", kind = "net", cc = "root", seconds = 2, area = 3, cast = 1.2f, every = 15, first = 5, power = 0, reach = 8 },
            // Beasts: a wind-up of under a second, not a spell.
            new MobCast { mob = "Grey wolf", name = "Heel-Snap", kind = "net", cc = "root", seconds = 1.5f, area = 0, cast = .7f, every = 15, first = 6, power = 0, reach = 3.5f },
            new MobCast { mob = "Mountain wolf", name = "Heel-Snap", kind = "net", cc = "root", seconds = 2, area = 0, cast = .7f, every = 15, first = 5, power = 0, reach = 3.5f },
            new MobCast { mob = "Ash hound", name = "Ash-Shake", kind = "net", cc = "disorient", seconds = 1.5f, area = 3, cast = .8f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Company pit-hound", name = "Hold Fast", kind = "net", cc = "root", seconds = 2, area = 0, cast = .7f, every = 14, first = 4, power = 0, reach = 3.5f },
            new MobCast { mob = "Old Cinder-Jaw", name = "Jaw-Lock", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = .9f, every = 14, first = 5, power = 0, reach = 3.5f },
            new MobCast { mob = "The Hush-Mother", name = "Mother's Howl", kind = "net", cc = "fear", seconds = 2, area = 4, cast = 1f, every = 16, first = 5, power = 0, reach = 6 },
            new MobCast { mob = "Wild boar", name = "Tusk-Toss", kind = "bolt", cast = .8f, every = 14, first = 6, power = 1.5f, reach = 3.5f, knock = 2.5f },
            new MobCast { mob = "Carrion boar", name = "Tusk-Toss", kind = "bolt", cast = .8f, every = 14, first = 5, power = 1.8f, reach = 3.5f, knock = 2.5f },
            new MobCast { mob = "Mire boar", name = "Mud-Splash", kind = "net", cc = "disorient", seconds = 1.5f, area = 3, cast = .8f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Rockhide boar", name = "Rockhide Ram", kind = "bolt", cast = .9f, every = 14, first = 5, power = 2f, reach = 4, knock = 3.5f },
            new MobCast { mob = "Moss-backed boar", name = "Tusk-Toss", kind = "bolt", cast = .8f, every = 14, first = 5, power = 2f, reach = 3.5f, knock = 3 },
            new MobCast { mob = "Old Brine-Tusk", name = "Brine Charge", kind = "bolt", cast = .9f, every = 13, first = 5, power = 2.4f, reach = 4, knock = 3.5f },
            new MobCast { mob = "Brown bear", name = "Paw-Cuff", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = .9f, every = 15, first = 6, power = 0, reach = 4 },
            new MobCast { mob = "Old Hazelmaw", name = "Paw-Cuff", kind = "net", cc = "stun", seconds = 2, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Mountain bear", name = "Paw-Cuff", kind = "net", cc = "stun", seconds = 2, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 4 },
            new MobCast { mob = "Greyed bear", name = "Grey Maul", kind = "blast", cast = 1f, every = 15, first = 5, power = 2.2f, reach = 3.5f, knock = 2.5f },
            new MobCast { mob = "Red hart", name = "Antler Toss", kind = "bolt", cast = .8f, every = 15, first = 5, power = 1.6f, reach = 3.5f, knock = 3 },
            new MobCast { mob = "Moss-antler stag", name = "Antler Sweep", kind = "blast", cast = 1f, every = 15, first = 5, power = 1.8f, reach = 3, knock = 2.5f },
            new MobCast { mob = "Carr spider", name = "Web-Spit", kind = "net", cc = "root", seconds = 2, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 12 },
            new MobCast { mob = "Charnel spider", name = "Web-Spit", kind = "net", cc = "root", seconds = 2, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 12 },
            new MobCast { mob = "Canopy spider", name = "Web-Drop", kind = "net", cc = "root", seconds = 2, area = 3, cast = 1f, every = 16, first = 5, power = 0, reach = 12 },
            new MobCast { mob = "Basalt spider", name = "Grit-Spit", kind = "net", cc = "disorient", seconds = 1.5f, area = 0, cast = .9f, every = 15, first = 5, power = 0, reach = 10 },
            // ---------- Hallow's Creek (Docs/DUNGEON2_DESIGN.md; GAME-ONLY): the Bureau's people, the Iron Pact, the Hollow, the ringers ----------
            new MobCast { mob = "Bureau grey-picker", name = "Grit in the Eyes", kind = "net", cc = "disorient", seconds = 1.5f, area = 0, cast = 1.2f, every = 15, first = 5, power = 0, reach = 5 },
            new MobCast { mob = "Iron Pact soldier", name = "Pact Shield-Bash", kind = "bolt", cast = 1.2f, every = 14, first = 5, power = 1.6f, reach = 4, knock = 2.5f },
            new MobCast { mob = "Iron Pact crossbowman", name = "Pact Quarrel", kind = "bolt", cast = 1.5f, every = 13, first = 2, power = 2f, reach = 22 },
            new MobCast { mob = "Bureau cutter", name = "Chisel Strike", kind = "blast", cast = 1.5f, every = 15, first = 6, power = 2f, reach = 3 },
            new MobCast { mob = "Bureau clerk", name = "Mend the Copy", kind = "heal", cast = 2f, every = 14, first = 4, power = .3f, reach = 15 },   // the clerks ward and mend each other (the zealots' lesson)
            new MobCast { mob = "Bureau teamster", name = "Cart-Whip", kind = "net", cc = "root", seconds = 2, area = 0, cast = 1.2f, every = 15, first = 5, power = 0, reach = 7 },
            new MobCast { mob = "Hallow's Creek Hollow", name = "Grey Clutch of the Cut", kind = "drain", cast = 2f, every = 14, first = 4, power = 1.8f, reach = 6 },
            new MobCast { mob = "Paused ringer", name = "The Ringer's Hand", kind = "net", cc = "stun", seconds = 1.5f, area = 0, cast = 1.4f, every = 15, first = 4, power = 0, reach = 4 },
        };
        public static MobCast For(string mob) { return string.IsNullOrEmpty(mob) ? null : All.FirstOrDefault(c => c.mob == mob); }

        /// <summary>
        /// The party's diminishing returns on a mob's hold (CC_DESIGN.md section 1's rule for holds on mobs, turned round; 2026-10-15): the same
        /// kind on the same one of the party within <see cref="EncounterEnemy.DrWindow"/> seconds of the last that took lasts half as long, a
        /// third time not at all. So a pull of three overseers is one stun, a short one and an "Immune", not a stun-lock. A boss's hold
        /// (EliteMove.cc) is not counted here. Returns the seconds the hold gets (0: it does not take).
        /// </summary>
        public static float Diminished(string who, string kind, float seconds, float now)
        {
            string key = who + ":" + kind;
            int n = held.TryGetValue(key, out var was) && now - was.at < EncounterEnemy.DrWindow ? was.n + 1 : 0;
            if (n >= 2) return 0;   // immune; the window still runs from the last one that took
            if (held.Count > 256) held.Clear();   // (a scene's worth of actors at most; a cleared entry only forgives a hold)
            held[key] = (now, n);
            return n == 1 ? seconds * .5f : seconds;
        }
        static readonly Dictionary<string, (float at, int n)> held = new Dictionary<string, (float, int)>();
    }

    /// <summary>
    /// Casting (step C5): a caster in a fight stops, casts for a couple of seconds (a cast bar on its nameplate and your target frame),
    /// then its spell lands. Any silence (Kick, Hush, Quench, Censure, Shield Bash, Pin), stun, hold or fear interrupts it, and a
    /// silenced mob can't start one. Mira hushes a cast she sees coming; party sims with an interrupt use it.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public MobCast Cast { get; set; }
        float castStart = -1, castEnd = -1, nextCastAt; EncounterEnemy castOn;
        public bool Casting { get { return castEnd >= 0; } }
        public float CastProgress { get { return Casting ? Mathf.Clamp01((Time.time - castStart) / Mathf.Max(.01f, castEnd - castStart)) : 0; } }
        public float CastRemaining { get { return Casting ? Mathf.Max(0, castEnd - Time.time) : 0; } }
        /// <summary>How many of its casts were interrupted (for tests and the log).</summary>
        public int Interrupts { get; private set; }

        void CancelCast(bool interrupted)
        {
            if (!Casting) return;
            castStart = castEnd = -1; castOn = null; nextCastAt = Time.time + (Cast != null ? Cast.every * .6f : 5);
            if (interrupted) { Interrupts++; session.FloatText(transform.position + Vector3.up * .6f, "Interrupted!", new Color(1, .9f, .5f)); }
        }
        void ResetCasting() { CancelCast(false); nextCastAt = 0; }
        /// <summary>Each frame in a fight: true while it casts (it stands still and doesn't swing).</summary>
        bool TickCast()
        {
            if (Cast == null || !actor.IsAlive) return false;
            if (Casting)
            {
                if (Controlled || Silenced) { CancelCast(true); return false; }
                if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
                if (Time.time < castEnd) return true;
                castStart = castEnd = -1; nextCastAt = Time.time + Cast.every; Land(); return true;
            }
            if (nextCastAt <= 0) { nextCastAt = Time.time + Cast.first; return false; }
            if (Time.time < nextCastAt || Silenced || Victim == null) return false;
            if (Cast.kind == "heal")
            {
                castOn = session.Enemies.Where(e => e != null && e.actor.IsAlive && e.Engaged && e.actor.Health.Pool.Ratio < .7f && Vector3.Distance(e.transform.position, transform.position) <= 20)
                    .OrderBy(e => e.actor.Health.Pool.Ratio).FirstOrDefault();
                if (castOn == null) { nextCastAt = Time.time + 1; return false; }
            }
            // Its foe within its reach (a blast's too, 2026-10-15: a pick sweep or a fuse waits until its foe is in it, not lit at someone 20 m off).
            else if (Vector3.Distance(Victim.transform.position, transform.position) > Cast.reach) return false;
            castStart = Time.time; castEnd = Time.time + Cast.cast;
            session.FloatText(transform.position + Vector3.up * .9f, Cast.name, new Color(1, .62f, .2f));
            return true;
        }
        void Land()
        {
            switch (Cast.kind)
            {
                case "heal":
                    if (castOn != null && castOn.actor.IsAlive)
                    {
                        int healed = castOn.actor.Health.ApplyHealing(Mathf.RoundToInt(castOn.actor.Health.Pool.Max * Cast.power));
                        session.FloatText(castOn.transform.position, "+" + healed, new Color(.5f, 1, .5f));
                    }
                    break;
                case "bolt":
                case "drain":
                    if (Victim != null && Victim.IsAlive && Vector3.Distance(Victim.transform.position, transform.position) <= Cast.reach + 2)
                    {
                        var struck = Victim;
                        int dealt = session.Kit.ResolveEnemyHit(this, struck, Mathf.RoundToInt(HitBase * Cast.power * GroupDamageScale));
                        session.FloatText(struck.transform.position, Cast.name + " " + dealt, new Color(1, .5f, .3f));
                        if (Cast.kind == "drain" && dealt > 0) actor.Health.ApplyHealing(dealt);
                        if (Cast.knock > 0 && struck.IsAlive) Knock(struck, Cast.knock);   // a boar's toss, a shove (2026-10-15)
                    }
                    break;
                case "net":   // (S6) a hold on everyone of the party round its foe (EncounterSession.HoldPlayer, SimCompanion.HoldUntil, as a boss's hold)
                    if (Victim == null || !Victim.IsAlive || Vector3.Distance(Victim.transform.position, transform.position) > Cast.reach + 2) break;
                    var netAt = Victim.transform.position; int held = 0;
                    foreach (var who in new[] { session.Player }.Concat(session.PartySims.Where(c => c != null).Select(c => c.actor)))
                    {
                        if (who == null || !who.IsAlive || Vector3.Distance(who.transform.position, netAt) > Mathf.Max(.5f, Cast.area)) continue;
                        var sim = who == session.Player ? null : who.GetComponent<SimCompanion>(); if (who != session.Player && sim == null) continue;
                        // The same hold again soon is shorter, a third time nothing (MobCasts.Diminished, 2026-10-15): no stun-lock from a pull.
                        float seconds = MobCasts.Diminished(who.EntityId.Value, Cast.cc, Cast.seconds, Time.time);
                        if (seconds <= 0) { session.FloatText(who.transform.position + Vector3.up * .4f, "Immune", Color.white); continue; }
                        if (sim == null) session.HoldPlayer(Cast.cc, seconds, transform.position, Name + "'s " + Cast.name);
                        else sim.HoldUntil(Cast.cc, seconds, transform.position);
                        held++;
                    }
                    session.FloatText(netAt + Vector3.up * .6f, Cast.name + (held > 0 ? "!" : " misses"), new Color(1, .75f, .35f));
                    break;
                case "blast":
                    var hit = new HashSet<Actor>();
                    foreach (var who in new[] { session.Player, session.Companion != null ? session.Companion.actor : null }.Concat(session.PartySims.Where(c => c != null).Select(c => c.actor)))
                        if (who != null && who.IsAlive && hit.Add(who) && Vector3.Distance(who.transform.position, transform.position) <= Cast.reach)
                        {
                            session.Kit.ResolveEnemyHit(this, who, Mathf.RoundToInt(HitBase * Cast.power * GroupDamageScale));
                            if (Cast.knock > 0 && who.IsAlive) Knock(who, Cast.knock);   // a pole's or a halberd's sweep throws them back (2026-10-15)
                        }
                    session.FloatText(transform.position + Vector3.up, string.IsNullOrEmpty(Cast.landText) ? Cast.name + "!" : Cast.landText, new Color(1, .45f, .2f));
                    break;
                default:
                    Debug.LogWarning("[MobCast] " + Cast.mob + "'s " + Cast.name + ": no such kind \"" + Cast.kind + "\" (MobCasts.Kinds).");
                    break;
            }
        }
    }
}
