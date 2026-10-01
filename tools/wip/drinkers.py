"""The out-of-work drink at the inn until the ale is gone or they are (Chris, 2026-10-01: "please have unemployed npc show up at
   the inn and drink till gone or passed out")."""
import io
A = 'D:/code/mmo/New Unity Project/Assets/Crulanda/Scripts/Encounter/'
def patch(name, reps):
    p = A + name; s = io.open(p, encoding='utf-8', newline='').read()
    for a, b in reps:
        assert s.count(a) == 1, (name, a[:80], s.count(a))
    for a, b in reps: s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8', newline='').write(s); print('patched', name)

patch('ActorVisual.cs', [
 ("public enum ActorPose { None, Work, Talk, Sit, Cower, Hammer, Chop, Gather, Knead, Swim, Sneak }",
  "public enum ActorPose { None, Work, Talk, Sit, Cower, Hammer, Chop, Gather, Knead, Swim, Sneak, Drink, Slump }"),
 ("        public void SetClothColor(Color c) { if (cloth != null) cloth.color = c; }",
  "        public void SetClothColor(Color c) { if (cloth != null) cloth.color = c; }\n"
  "        /// <summary>The right arm's shoulder pivot (the hand is .62 down it): what a villager's tankard hangs from.</summary>\n"
  "        public Transform RightArm { get { return armR; } }\n"
  "        /// <summary>0 sober to 1 reeling: the body rolls and pitches with the stride (a drinker walking home).</summary>\n"
  "        public float Stagger;\n"
  "        bool staggering;"),
 ("                if (posed) { posed = false; swimLean = 0; body.localPosition = Vector3.zero; body.localEulerAngles = new Vector3(stoop, 0, 0); }\n                return;",
  "                if (posed) { posed = false; swimLean = 0; body.localPosition = Vector3.zero; body.localEulerAngles = new Vector3(stoop, 0, 0); }\n"
  "                if (Stagger > 0) { staggering = true; body.localEulerAngles = new Vector3(stoop + Mathf.Sin(phase * .5f) * 6 * Stagger, 0, Mathf.Sin(phase * .5f + 1.1f) * 14 * Stagger); }\n"
  "                else if (staggering) { staggering = false; body.localEulerAngles = new Vector3(stoop, 0, 0); }\n"
  "                return;"),
 ("                case ActorPose.Cower:\n                    armL.localEulerAngles = new Vector3(-150, 0, 25); armR.localEulerAngles = new Vector3(-150, 0, -25); break;",
  "                case ActorPose.Cower:\n                    armL.localEulerAngles = new Vector3(-150, 0, 25); armR.localEulerAngles = new Vector3(-150, 0, -25); break;\n"
  "                case ActorPose.Drink:    // seated with a tankard: the right arm lifts it to the mouth every few seconds, the head tipping back\n"
  "                {\n"
  "                    legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0);\n"
  "                    float k = Mathf.Repeat(t * .28f, 1), lift = k < .18f ? Mathf.SmoothStep(0, 1, k / .18f) : k < .45f ? 1 : k < .6f ? 1 - Mathf.SmoothStep(0, 1, (k - .45f) / .15f) : 0;\n"
  "                    armL.localEulerAngles = new Vector3(-35, 0, 8); armR.localEulerAngles = new Vector3(-40 - lift * 85, 0, -8 + lift * 18);\n"
  "                    lean -= lift * 8; break;\n"
  "                }\n"
  "                case ActorPose.Slump:    // passed out over the table: folded forward, arms out, breathing slow\n"
  "                    legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0);\n"
  "                    armL.localEulerAngles = new Vector3(-110, 0, 22); armR.localEulerAngles = new Vector3(-105, 0, -26);\n"
  "                    lean += 62 + Mathf.Sin(t * .6f) * 1.5f; break;"),
 ("body.localPosition = new Vector3(0, Pose == ActorPose.Sit ? -.45f * (child ? .66f : 1)",
  "body.localPosition = new Vector3(0, Pose == ActorPose.Sit || Pose == ActorPose.Drink || Pose == ActorPose.Slump ? -.45f * (child ? .66f : 1)"),
])

patch('VillageWork.cs', [
 ('{ "drinker", new WorkDay(new[] { new Shift(7.5f, 10, "wander", "green", "well"), new Shift(10, 15, "inn", "inn", "inn", "inn", "green"), new Shift(15, 16, "wander", "well"), new Shift(16, 23.5f, "inn", "inn", "inn", "inn", "inn", "green") }) },',
  '// The out of work (the drinkers, and any trade whose workplace the village lacks): loiter the morning away, then the inn, where\n'
  '            // they drink until the ale is gone or they are (Villager.DrinkRound).\n'
  '            { "drinker", new WorkDay(new[] { new Shift(7.5f, 11, "wander", "green", "well"), new Shift(11, 23.6f, "inn") }) },'),
 ('                new Errand("shutters up", 17.8f, 19.5f, "stall", "home", Load.Goods, "goods", "Shutters up. What didn\'t sell comes home.")) },',
  '                new Errand("a cask for the inn", 8.6f, 11, "stall", "inn", Load.Goods, "ale", "Ale for the Cask. Make it last the night, for once.") { amount = 12 },\n'
  '                new Errand("shutters up", 17.8f, 19.5f, "stall", "home", Load.Goods, "goods", "Shutters up. What didn\'t sell comes home.")) },'),
 ('                case "goods": return r < .5f ? "Leave it on the counter." : "I\'ll see what sells.";',
  '                case "goods": return r < .5f ? "Leave it on the counter." : "I\'ll see what sells.";\n'
  '                case "ale": return r < .5f ? "About time. They\'re through the last one." : "Roll it behind the bar.";'),
 ("        public static GameObject Build(Transform owner, Load load, int count)",
  "        /// <summary>A drinker's tankard, hung from the right arm's pivot at the hand and tilted so that lifting the arm tips it to the mouth.</summary>\n"
  "        public static GameObject Tankard(Transform arm)\n"
  "        {\n"
  "            var go = new GameObject(\"Tankard\"); var t = go.transform; t.SetParent(arm, false); t.localPosition = new Vector3(0, -.64f, .1f); t.localEulerAngles = new Vector3(55, 0, 0);\n"
  "            Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(.11f, .075f, .11f), new Color(.46f, .33f, .19f));\n"
  "            Part(PrimitiveType.Cylinder, t, new Vector3(0, .078f, 0), new Vector3(.118f, .006f, .118f), new Color(.62f, .6f, .56f));   // the pewter rim\n"
  "            Part(PrimitiveType.Cylinder, t, new Vector3(0, .07f, 0), new Vector3(.095f, .004f, .095f), new Color(.86f, .8f, .62f));    // the head on the ale\n"
  "            Part(PrimitiveType.Cube, t, new Vector3(.075f, 0, 0), new Vector3(.03f, .09f, .02f), new Color(.36f, .26f, .16f));         // the handle\n"
  "            return go;\n"
  "        }\n"
  "        public static GameObject Build(Transform owner, Load load, int count)"),
])

patch('WorldLife.cs', [
 # The out of work.
 ('                case "herbalist": return "meadow"; case "miller": return "mill"; default: return null;',
  '                case "herbalist": return "meadow"; case "miller": return "mill"; case "farmer": return "field"; default: return null;'),
 ('                if (need != null && Places[need].Count == 0) role = i % 2 == 0 ? "farmer" : "gossip";',
  '                // Out of work: a trade whose workplace this village lacks. Where there is an inn they drink; elsewhere they gossip.\n'
  '                if (need != null && Places[need].Count == 0) role = Places["inn"].Count > 0 ? "drinker" : "gossip";'),
 ('            if (WorldClock.Between(4, 5)) Stock.Clear();   // the stock is the day\'s deliveries: yesterday\'s are eaten, sold or burnt',
  '            if (WorldClock.Between(4, 5)) { Stock.Clear(); MorningCask(); }   // the stock is the day\'s deliveries: yesterday\'s are eaten, sold or burnt'),
 ('        /// <summary>What has been delivered where today, by "place.good"',
  '        /// <summary>What is left in yesterday\'s cask at the inn each morning, in tankards (the merchant brings a fresh one: "a cask for the inn").</summary>\n'
  '        public const int MorningAle = 10;\n'
  '        void MorningCask() { if (Places.TryGetValue("inn", out var seats) && seats.Count > 0) Stock["inn.ale"] = MorningAle; }\n'
  '        /// <summary>What has been delivered where today, by "place.good"'),
 ('            FindPlaces();\n            int count = z.life.villagers;', '            FindPlaces(); MorningCask();\n            int count = z.life.villagers;'),
 ('            if (!toPlayer) return Chatter[rng.Next(Chatter.Length)];\n            if (rng.Next(3) == 0)',
  '            if (v.PassedOut) return "Zzz...";\n            if (!toPlayer) return Chatter[rng.Next(Chatter.Length)];\n            if (rng.Next(3) == 0)'),
 ('            var line = LineFor(v, true); v.Say(line, 6); v.FacePlayer();', '            var line = LineFor(v, true); v.Say(line, 6); if (!v.PassedOut) v.FacePlayer();'),
 ('                case "drinker": case "elder": case "gossip": case "farmer":\n                    return Count("inn.meat") > 0',
  '                case "drinker": case "elder": case "gossip": case "farmer":\n                    return Places["inn"].Count > 0 && Count("inn.ale") == 0 ? "The cask\'s run dry at the inn. The out-of-work drank it by supper." : Count("inn.meat") > 0'),
 # Villager state.
 ('        public bool Carrying { get { return load != null; } }',
  '        public bool Carrying { get { return load != null; } }\n'
  '        /// <summary>How much a drinker has had (0 sober; past <see cref="Tolerance"/> they fold or go home), how much they can hold, and\n'
  '        /// whether they are done for the day (the ale ran out, or they did).</summary>\n'
  '        public float Drunk { get { return drunk; } }\n'
  '        public float Tolerance { get { return tolerance; } set { tolerance = value; } }\n'
  '        public bool Spent { get { return spent; } }\n'
  '        /// <summary>Slumped over the inn\'s table, dead to the world.</summary>\n'
  '        public bool PassedOut { get { return activity == "passedout"; } }\n'
  '        int index; float drunk, tolerance = 1; bool spent; GameObject tankard;'),
 ('            v.lastHour = WorldClock.Hour;', '            v.lastHour = WorldClock.Hour; v.index = index; v.tolerance = .85f + (index % 5) * .18f;'),
 ('            agent.speed = (Role == "child" ? 2.4f : Role == "elder" ? 1.1f : 1.6f) * speedScale;\n            agent.isStopped = false; agent.SetDestination(target); state = State.Travel; visual.Pose = ActorPose.None;',
  '            float reel = Reeling; visual.Stagger = reel; ShowTankard(false);\n'
  '            agent.speed = (Role == "child" ? 2.4f : Role == "elder" ? 1.1f : 1.6f) * speedScale * (1 - .35f * reel);\n'
  '            agent.isStopped = false; agent.SetDestination(target); state = State.Travel; visual.Pose = ActorPose.None;'),
 ('            if (StartErrand()) return;\n            var shift = VillageWork.ShiftFor(Role, WorldClock.Hour);\n            GoTo(shift != null ? shift.places : Role == "child"',
  '            if (Role == "drinker" && DrinkerNext()) return;\n            if (StartErrand()) return;\n            var shift = VillageWork.ShiftFor(Role, WorldClock.Hour);\n            GoTo(shift != null ? shift.places : Role == "child"'),
 ('            if (errand != null) { ErrandArrive(); return; }\n            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;\n            until = Time.time + (activity == "home" ? HomeStay',
  '            if (errand != null) { ErrandArrive(); return; }\n'
  '            if (Role == "drinker" && activity == "inn") { DrinkRound(); return; }\n'
  '            if (activity == "sleepitoff") { state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true; until = Time.time + 30; if (home != null) Hide(); return; }\n'
  '            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;\n            until = Time.time + (activity == "home" ? HomeStay'),
 ('            state = State.Hidden; calmSince = Time.time; foreach (var r in renderers) r.enabled = false;\n            DropLoad();   // the goods put away in the pantry',
  '            state = State.Hidden; calmSince = Time.time; foreach (var r in renderers) r.enabled = false;\n            DropLoad(); ShowTankard(false);   // the goods put away in the pantry'),
 ('            CancelErrand();   // not left hanging in the air where they stood', '            CancelErrand(); ShowTankard(false);   // not left hanging in the air where they stood'),
 ('            else if (activity == "sleep" && life.R01 < .3f) Say(life.R01 < .5f ? "Another grey morning." : "Morning. Still here, then.", 4);\n            ChooseNext();',
  '            else if (activity == "sleep" && life.R01 < .3f) Say(life.R01 < .5f ? "Another grey morning." : "Morning. Still here, then.", 4);\n'
  '            else if (activity == "sleepitoff") Say(life.R01 < .5f ? "Never again. ...Is the Cask open yet?" : "My head. Who put the sun there?", 5);\n'
  '            if (activity == "sleep" || activity == "sleepitoff") { drunk = 0; spent = false; visual.Stagger = 0; }   // a new day, a clear head\n'
  '            ChooseNext();'),
 ('                else if (activity == "sleep") { if (!Bedtime || Interrupted) Emerge(); }',
  '                else if (activity == "sleep") { if (!Bedtime || Interrupted) Emerge(); }\n'
  '                else if (activity == "sleepitoff") { if (WorldClock.Between(wakeAt, 10.5f)) Emerge(); }   // not before morning'),
 ('            if (Attending)\n            {', '            if (Attending && !PassedOut)\n            {'),
 ('            var player = life.Session.Player; if (player == null || !player.IsAlive || Time.time < nextBark) return;',
  '            if (PassedOut) { if (Time.time >= nextBark) { nextBark = Time.time + 18 + life.R01 * 14; Say(life.R01 < .7f ? "Zzz..." : "...\'nother one...", 4); } return; }\n'
  '            var player = life.Session.Player; if (player == null || !player.IsAlive || Time.time < nextBark) return;'),
 # The drinker's own logic, before the hen-wife's.
 ('        // ---------- the hen-wife ----------',
  '''        // ---------- the out of work at the inn ----------
        static readonly string[] SoberLines = { "Same again.", "To absent friends.", "First of the day. Well. Of the afternoon.", "No work, no worry. That's what I tell the wife." };
        static readonly string[] MerryLines = { "I'm not shaying the Council's wrong. I'm shaying... what was I shaying?", "Lissen. Lissen. The grey's jusht weather.", "Another! For the Harrow girl.", "They took my trade. They can't take my thirsht." };
        static readonly string[] FarGoneLines = { "Thersh two of you. Both ugly.", "I can shee the Washting from here. 'S pretty.", "Hic.", "'M fine. 'M fine. The floor's drunk." };
        static readonly string[] LeavingLines = { "Thass me done. G'night, all.", "Home. Before she locks the door.", "One more and I'd be under the table. G'night." };
        static readonly string[] DryLines = { "Dry? The Cask's dry? Then I'm for home.", "No ale. No work and no ale. What a village.", "Empty. Somebody tell the merchant." };
        /// <summary>0 steady to 1 reeling, from how near a drinker is to their limit.</summary>
        float Reeling { get { return Role == "drinker" && tolerance > 0 ? Mathf.Clamp01((drunk / tolerance - .45f) / .55f) : 0; } }
        void ShowTankard(bool on)
        {
            if (on && tankard == null && visual != null && visual.RightArm != null) tankard = LoadProps.Tankard(visual.RightArm);
            if (tankard != null) tankard.SetActive(on);
        }
        /// <summary>
        /// The out of work (the drinkers, and anyone whose trade this village has no place for) drink at the inn until the ale is gone or
        /// they are. Past their limit some fold over the table where they sit and the rest say goodnight while they can; spent, they go
        /// home to sleep it off until morning (with no home, they stay slumped at the table). False: the day's shift decides.
        /// </summary>
        bool DrinkerNext()
        {
            if (activity == "passedout" && !spent) { spent = true; Say("Ugh. My head. Who moved the floor?", 5); }   // come round, hours later
            if (!spent && drunk >= tolerance)
            {
                if (activity == "inn" && (index % 5 < 2 || home == null)) { PassOut(); return true; }
                spent = true; Say(LeavingLines[life.Next(LeavingLines.Length)], 5);
            }
            if (!spent) return false;
            if (home == null) { if (activity == "inn" || activity == "passedout") { PassOut(); return true; } return false; }
            activity = "sleepitoff"; Go(home.position); return true;
        }
        void PassOut()
        {
            activity = "passedout"; state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            ShowTankard(false); visual.Pose = ActorPose.Slump; until = Time.time + 280 + life.R01 * 220; Say("Zzz...", 6); nextBark = Time.time + 20;
        }
        /// <summary>A round at the inn: a tankard off the day's cask (the village's stock, "inn.ale") and a little further gone; when the
        /// cask is dry they grumble and call it a day.</summary>
        void DrinkRound()
        {
            state = State.Activity; if (agent.isOnNavMesh) agent.isStopped = true;
            var look = life.LookFor(transform.position); if (look.HasValue) Face(look.Value);
            until = Time.time + 16 + life.R01 * 22;
            if (spent || !life.Take("inn.ale"))
            {
                visual.Pose = ActorPose.Sit; ShowTankard(false);
                if (!spent) { spent = true; Say(DryLines[life.Next(DryLines.Length)], 5); until = Time.time + 8; }
                return;
            }
            drunk += .07f + life.R01 * .05f; visual.Pose = ActorPose.Drink; ShowTankard(true);
            float gone = drunk / Mathf.Max(.01f, tolerance); var lines = gone < .4f ? SoberLines : gone < .75f ? MerryLines : FarGoneLines;
            if (life.R01 < .55f) Say(lines[life.Next(lines.Length)], 4);
        }

        // ---------- the hen-wife ----------'''),
])
print('DRINKERS OK')
