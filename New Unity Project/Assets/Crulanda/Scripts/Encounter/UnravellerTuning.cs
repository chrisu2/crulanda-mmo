using System;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's numbers (Docs/WORLD_BOSS_1.md 3.2-3.5; build steps 4 and 5): read from EncounterContent/Raids/unraveller.json (the
    /// content's raid files, EncounterContent.raidFiles), so that the balancing of step 6 edits only that file. The defaults here are the
    /// file's, for a build or a test that has not registered it. All GAME-ONLY, PROVISIONAL until the group run tool at 10 has tuned them.
    /// Seconds, metres, and shares of health (.6 = 60%).
    /// </summary>
    [Serializable]
    public sealed class UnravellerTuning
    {
        // ---------- her, and the clock ----------
        public int health = 195000; public float hitBase = 190, swingEvery = 2.2f, enrageSeconds = 540, phase2At = .7f, phase3At = .4f;
        // ---------- the rules that hold all fight (3.3): the Weave-Eaters' own, scaled ----------
        public float lighthouseShare = .1f, lighthouseBeat = 2, lighthouseReach = 30;
        public float blinkEvery = 9, blinkReach = 12, touchShare = .6f, touchSilence = 3, wardReach = 10;
        public float pinFirst = 12, pinStep = 1, pinLeast = 7; public int haulPins = 2; public float haulPace = 1, haulTaken = 1.1f;
        public int steelStacks = 3;
        /// <summary>Attack power lost a stack of her blackened steel (the Weave-Eaters' own is 10%, one stack: WeaveEaterRules).</summary>
        public float steelShare = .05f;
        // ---------- phase 1 (3.4) ----------
        public float unravelSeconds = 10, unravelArmour = .06f, unravelTaken = .05f; public int swapAt = 4, erasedAt = 7;
        public float feedCast = 3, feedFirst = 8, feedEvery1 = 15, feedEvery2 = 20, feedEvery3 = 20, feedReach = 20, feedHeal = .003f, feedGrow = .02f, interruptImmune = 10;
        // ---------- phase 2 ----------
        /// <summary>The brood's wait in each phase; in phase 1 none (0) as the doc has it: the fight's first two minutes are the pins, the
        /// Feeding and the swap. A step-6 knob if phase 1 wants adds (play note 260, "she needs adds").</summary>
        public float broodEvery1 = 0, broodEvery2 = 30, broodEvery3 = 40; public int broodCount = 5, broodLevel = 22;
        /// <summary>A control class's hold on a brood (raid 4): its seconds and its wait before the next.</summary>
        public float broodHoldSeconds = 18, broodHoldEvery = 15;
        public float broodHealthShare = .12f, broodSalt = 4, shatter = 3, broodEatenReach = 2.5f, broodEatenHeal = .01f, broodEatenGrow = .05f;
        public float unmadeEvery = 20, unmadeRadius = 2.5f, unmadeWarn = 3, unmadeShare = .15f, unmadeRamp = .05f;
        public float swallowEvery2 = 35, swallowEvery3 = 40, swallowShare = .04f, swallowSeconds = 10; public int saltsToFree = 3;
        public float saltWindow = 12, calcifySeconds = 4, calcifyTaken = 1.5f;
        // ---------- phase 3 ----------
        public float pulseEvery = 3, pulseStart = .02f, pulseGrow = .0012f;
        public float huntEvery = 20, huntWarn = 3, huntRadius = 5, huntShare = .8f, missStun = 2;
        // ---------- the sims (3.5) ----------
        public int harpooners = 3, interrupters = 3; public float harpoonLead = 2;
        /// <summary>The chance a sim misses its part of a mechanic now and then: at 18, at 20, and what a careless one (rash: nerve over .75) adds.</summary>
        public float simMissAt18 = .12f, simMissAt20 = .04f, simMissCareless = .06f;

        /// <summary>The numbers in play (the content's file once a session has read it; the defaults before).</summary>
        public static UnravellerTuning Current { get { return current ?? (current = new UnravellerTuning()); } set { current = value; } }
        static UnravellerTuning current;
        /// <summary>The file's name among the content's raid files.</summary>
        public const string FileName = "unraveller";
        /// <summary>The numbers from the file's text; the defaults for anything it leaves out, or for text that will not parse (with a warning).</summary>
        public static UnravellerTuning Parse(string json)
        {
            var t = new UnravellerTuning();
            if (string.IsNullOrEmpty(json)) return t;
            try { JsonUtility.FromJsonOverwrite(json, t); }
            catch (Exception ex) { Debug.LogWarning("The Unraveller's numbers (" + FileName + ".json) did not parse: " + ex.Message + ". Using the defaults."); return new UnravellerTuning(); }
            return t;
        }
        /// <summary>Reads the content's raid file into <see cref="Current"/> (the session at its start); the defaults with none registered.</summary>
        public static void LoadFrom(TextAsset[] files)
        {
            if (files != null) foreach (var f in files) if (f != null && f.name == FileName) { Current = Parse(f.text); return; }
            Current = new UnravellerTuning();
        }
        /// <summary>The Feeding's wait in <paramref name="phase"/> (1-3).</summary>
        public float FeedEvery(int phase) { return phase >= 3 ? feedEvery3 : phase == 2 ? feedEvery2 : feedEvery1; }
        /// <summary>The brood's wait in a phase (none in phase 1: 0).</summary>
        public float BroodEvery(int phase) { return phase >= 3 ? broodEvery3 : phase == 2 ? broodEvery2 : broodEvery1; }
        /// <summary>The swallowing's wait in a phase (none in phase 1: 0).</summary>
        public float SwallowEvery(int phase) { return phase >= 3 ? swallowEvery3 : phase == 2 ? swallowEvery2 : 0; }
    }
}
