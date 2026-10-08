using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The sims talking (2026-10-06, playtest note 62; Docs/CHAT_RESEARCH.md): every few seconds (sooner the more of them are here)
    /// one of this zone's sims says something, the chatty ones far more often, a few never. What they say is true of the zone and of
    /// themselves: a group wanted for a real camp or elite at their level, ore and herbs they gather for sale, where a place is (and
    /// another sim answers with its real direction), an elite that is up, the weather and the dark, a grumble about their hunt, a
    /// goodnight as they log off. In your party they call out in Party ("inc", "need a heal"). They answer you: a greeting, "where
    /// is ...", a group wanted, something for sale, thanks. Names of places, mobs and items come from the zone (no other game's).
    /// </summary>
    public sealed class SimChatter : MonoBehaviour
    {
        public static SimChatter Active { get; private set; }
        public SimPopulation Population;
        EncounterSession S { get { return Population.Session; } }
        float next, nextParty;
        readonly List<(float at, ChatChannel ch, string who, string text)> pending = new List<(float, ChatChannel, string, string)>();
        readonly Dictionary<string, float> said = new Dictionary<string, float>();
        System.Random rng;

        public void Init(SimPopulation pop) { Population = pop; Active = this; rng = new System.Random(pop.World.seed + 77); next = Time.time + 6; nextParty = Time.time + 10; nextAsk = Time.time + 60; }
        void OnDestroy() { if (Active == this) Active = null; }
        float R { get { return (float)rng.NextDouble(); } }
        T Pick<T>(IList<T> list) { return list[rng.Next(list.Count)]; }

        void Update()
        {
            if (Population == null || S == null || S.Paused) return;
            for (int i = pending.Count - 1; i >= 0; i--)
                if (Time.time >= pending[i].at) { var p = pending[i]; pending.RemoveAt(i); S.ChatSay(p.ch, p.who, p.text); }
            if (!Population.Lively) return;   // in tests and captures the zone is quiet unless they live (SimPopulation.Lively)
            var here = Here();
            if (Time.time >= next && here.Count > 0)
            {
                next = Time.time + Mathf.Lerp(4, 12, R) * Mathf.Clamp(5f / Mathf.Sqrt(here.Count + 1), .8f, 2.5f);
                var who = PickTalker(here); if (who != null) Talk(who);
            }
            if (Time.time >= nextParty && S.PartySims.Count > 0) { nextParty = Time.time + Mathf.Lerp(6, 14, R); PartyTalk(); }
            FriendsAsk();
        }
        /// <summary>Sims online in this zone (standing about or in your party).</summary>
        List<SimAdventurer> Here()
        {
            var list = new List<SimAdventurer>();
            foreach (var s in Population.World.sims) if (s.zone == S.ZoneId && s.IsOnlineAt(WorldClock.Hour)) list.Add(s);
            return list;
        }
        SimAdventurer PickTalker(List<SimAdventurer> here)
        {
            float total = 0; foreach (var s in here) total += Weight(s);
            float r = R * total; foreach (var s in here) { r -= Weight(s); if (r <= 0) return Weight(s) > 0 ? s : null; }
            return null;
        }
        static float Weight(SimAdventurer s) { return s.chatty < .15f ? 0 : s.chatty * s.chatty; }   // the quiet ones lurk
        SimFigure Fig(SimAdventurer s) { return Population.Find(s.id); }

        // ---------- what they say ----------
        void Talk(SimAdventurer s)
        {
            var f = Fig(s); bool lingo = s.chatty > .65f;
            var options = new List<Func<(ChatChannel, string)>>();
            var camp = CampFor(s); var elite = EliteUp(); var item = GatheredItem(); var place = SomePlace();
            if (camp != null && s.bold > .35f) options.Add(() => (ChatChannel.LFG, lingo ? "LF1M " + camp + ", " + Short(s) + " here" : "Looking for one more for the " + camp + ". I'm a " + Long(s) + "."));
            if (elite != null && s.bold > .5f && s.level >= elite.actor.Level - 2) options.Add(() => (ChatChannel.LFG, lingo ? "LF2M " + elite.Name + ", have " + SimRoster.ClassName(s.classId).ToLower() : "Anyone up for " + elite.Name + "? Need two more."));
            if (elite != null) options.Add(() => (ChatChannel.Zone, elite.Name + (lingo ? " is up by " : " is up near ") + Near(elite.transform.position) + (lingo ? ", careful" : ". Careful out there.")));
            if (s.goodIds.Count > 0) { int gi = rng.Next(s.goodIds.Count); string gname = S.ItemName(s.goodIds[gi]); int gn = s.goodCounts[gi]; options.Add(() => (ChatChannel.Trade, lingo ? "WTS " + gname + " x" + gn + ", pst" : "Selling " + gname + ", " + gn + " of them. Send me a tell.")); }
            else if (item != null && (f == null || f.Activity == SimFigure.Doing.Gather || s.bold < .5f)) options.Add(() => (ChatChannel.Trade, lingo ? "WTB " + item + ", any amount" : "Buying " + item + " if anyone has some."));
            if (item != null) options.Add(() => (ChatChannel.Trade, lingo ? "WTB " + item + " x10" : "Buying " + item + " if anyone has some."));
            if (place != null && s.level <= 4) options.Add(() => { Answer(place, s); return (ChatChannel.Zone, lingo ? "where's " + place.name + "?" : "Can anyone tell me where " + place.name + " is?"); });
            var exit = SomeExit();
            if (exit != null && s.level <= 6) options.Add(() => { AnswerRoad(exit, s); return (ChatChannel.Zone, lingo ? "how do i get to " + S.ZoneName(exit.to) + "?" : "How do I get to " + S.ZoneName(exit.to) + " from here?"); });
            if (f != null && f.Activity == SimFigure.Doing.Hunt && f.Quarry != null) options.Add(() => (ChatChannel.Zone, Pick(new[] { "these " + Plural(f.Quarry.Name) + " hit hard", "anyone else on " + Plural(f.Quarry.Name) + "?", f.Quarry.Name + " number " + (2 + rng.Next(6)) + ", still nothing", "so many " + Plural(f.Quarry.Name) + " by " + Near(f.Quarry.transform.position) })));
            if (f != null && f.Activity == SimFigure.Doing.Inn) options.Add(() => (ChatChannel.Zone, Pick(new[] { "anyone at the inn?", "inn stew is decent tonight", "brb, food", "resting up, then back out" })));
            if (f != null && f.Activity == SimFigure.Doing.Gather && item != null) options.Add(() => (ChatChannel.Zone, Pick(new[] { item + " everywhere today", "who keeps taking all the " + item + "?", "skilling up, slow going" })));
            if (WorldClock.IsNight) options.Add(() => (ChatChannel.Zone, Pick(new[] { "dark already?", "can't see a thing out here", "night mobs are no joke", "need a torch lol" })));
            var w = WorldWeather.Active;
            if (w != null && (w.Kind == WeatherKind.Rain || w.Kind == WeatherKind.Storm)) options.Add(() => (ChatChannel.Zone, Pick(new[] { "rain again", "soaked", "this weather..." })));
            options.Add(() => (ChatChannel.Zone, Pick(new[] { "anyone know a good spot for xp at " + s.level + "?", "what's a good weapon for a " + SimRoster.ClassName(s.classId).ToLower() + "?", "o/", "quiet tonight", "lol", "anyone around?", "this zone is so pretty" })));
            for (int tries = 0; tries < 4; tries++)
            {
                var (ch, text) = Pick(options)();
                string key = s.id + "|" + text;
                if (said.TryGetValue(key, out var t) && Time.time - t < 180) continue;   // not the same line twice in three minutes
                said[key] = Time.time; S.ChatSay(ch, s.name, text); return;
            }
        }
        void PartyTalk()
        {
            var c = S.PartySims[rng.Next(S.PartySims.Count)]; if (c == null || !c.actor.IsAlive) return;
            string line = null;
            if (c.actor.Health.Pool.Ratio < .4f) line = Pick(new[] { "need a heal", "low!", "help, low hp" });
            else if (S.InCombat) line = Pick(new[] { "inc", "got this one", "on it", "careful, adds", "nice" });
            else if (R < .35f) line = Pick(new[] { "where to next?", "ty for the invite", "good group", "brb 1 min", "lead on" });
            if (line != null) S.ChatSay(ChatChannel.Party, c.sim.name, line);
        }
        /// <summary>A level gained (5.3b): "ding N!" and a grats or two from the others.</summary>
        public void Ding(SimAdventurer s, bool inParty = false)
        {
            if (!Population.Lively && !inParty) return;
            var ch = inParty ? ChatChannel.Party : ChatChannel.Zone;
            S.ChatSay(ch, s.name, Pick(new[] { "ding " + s.level + "!", "ding!", "ding " + s.level + " :)", "ding " + s.level }));
            var others = Here().FindAll(o => o.id != s.id && o.chatty > .3f); int n = Mathf.Min(others.Count, 1 + (R < .5f ? 1 : 0));
            if (inParty) { S.ChatSay(ChatChannel.Party, "Mira", "Grats!"); return; }
            for (int i = 0; i < n; i++) { var o = others[rng.Next(others.Count)]; others.Remove(o); Later(ch, o.name, Pick(new[] { "grats", "gz", "grats!", "nice, grats", "gratz" }), i * 2.5f); }
        }
        public void Died(SimAdventurer s, Vector3 at) { if (Population.Lively && s.chatty > .35f && R < .7f) Later(ChatChannel.Zone, s.name, Pick(new[] { "died at " + Near(at) + ", running back", "corpse run time...", "ugh, dead. cr", "anyone near " + Near(at) + "? died there" }), 4); }
        public void Recovered(SimAdventurer s) { if (Population.Lively && s.chatty > .5f && R < .5f) Later(ChatChannel.Zone, s.name, Pick(new[] { "got my corpse back", "back in it", "ok, alive again" }), 1); }
        public void Leaving(SimAdventurer s, ZoneExit exit) { if (Population.Lively && s.chatty > .4f && R < .7f) Later(ChatChannel.Zone, s.name, Pick(new[] { "heading to " + S.ZoneName(exit.to) + ", cya", "off to " + S.ZoneName(exit.to), "outgrown this place, " + S.ZoneName(exit.to) + " next" }), 1); }
        public void Sold(SimAdventurer s, int coin) { if (Population.Lively && s.chatty > .5f && R < .4f) Later(ChatChannel.Trade, s.name, Pick(new[] { "sold my ore, " + coin + " coin, not bad", "stall took the lot for " + coin, coin + " coin for a morning's gathering" }), 1); }
        public void Upgraded(SimAdventurer s) { if (Population.Lively && s.chatty > .3f) Later(ChatChannel.Zone, s.name, Pick(new[] { "finally afforded better gear", "new " + (SimGear.Weight(s.classId) == "heavy" ? "mail" : SimGear.Weight(s.classId) == "leather" ? "leathers" : "robes") + ", look at me", "upgraded, feeling strong" }), 1); }
        public void Crafted(SimAdventurer s, string name, bool worn) { if (Population.Lively && s.chatty > .3f && R < .8f) Later(worn ? ChatChannel.Zone : ChatChannel.Trade, s.name, worn ? Pick(new[] { "just forged a " + name.ToLower() + ", wearing it", "made my own " + name.ToLower(), name + " done, my own make" }) : Pick(new[] { "WTS " + name + ", fresh made", "brewed a " + name.ToLower() + ", selling", "selling " + name.ToLower() + "s, pst" }), 1); }
        /// <summary>A sim logs off: a chatty one says so.</summary>
        public void LoggedOff(SimAdventurer s) { if (s.chatty > .45f && R < .6f && Population.Lively) S.ChatSay(ChatChannel.Zone, s.name, Pick(new[] { "gn all", "night all", "off to bed, gn", "logging, cya" })); }

        // ---------- answering ----------
        /// <summary>You said something: the sims who heard it may answer.</summary>
        public void Heard(ChatChannel ch, string text)
        {
            var here = Here(); if (here.Count == 0) return;
            string t = text.ToLowerInvariant();
            SimAdventurer Someone(Func<SimAdventurer, bool> ok = null) { var l = here.FindAll(s => !S.InParty(s.id) && (ok == null || ok(s))); return l.Count == 0 ? null : l[rng.Next(l.Count)]; }
            if (ch == ChatChannel.Party)
            {
                if (S.PartySims.Count > 0 && R < .7f) { var c = S.PartySims[rng.Next(S.PartySims.Count)]; Later(ChatChannel.Party, c.sim.name, t.Contains("?") ? Pick(new[] { "sure", "np", "your call", "ok" }) : Pick(new[] { "ok", "k", "sounds good", "on it" })); }
                return;
            }
            var place = PlaceIn(t);
            if (place != null && (t.Contains("where") || t.Contains("?"))) { var s = Someone(x => x.friendly > .3f); if (s != null) Later(ChatChannel.Zone, s.name, Directions(place, s)); return; }
            if (t.Contains("lfg") || t.Contains("lfm") || t.Contains("group") || ch == ChatChannel.LFG)
            {
                int lv = S.Progress.Level; var s = Someone(x => Mathf.Abs(x.level - lv) <= 3 && x.friendly > .3f);
                if (s != null) Later(ChatChannel.Zone, s.name, s.chatty > .65f ? Short(s) + ", inv me" : "I'm a " + Long(s) + ", happy to join you.");
                return;
            }
            if (t.Contains("wts") || t.Contains("selling") || ch == ChatChannel.Trade) { var s = Someone(); if (s != null && R < .6f) Later(ChatChannel.Trade, s.name, Pick(new[] { "how much?", "pst", "price?", "what's it worth to you" })); return; }
            if (t.Contains("ty") || t.Contains("thank")) { var s = Someone(x => x.friendly > .5f); if (s != null) Later(ChatChannel.Zone, s.name, Pick(new[] { "np", "anytime", "np :)" })); return; }
            if (t == "o/" || t.StartsWith("hi") || t.StartsWith("hey") || t.StartsWith("hello") || t.Contains("evening") || t.Contains("morning"))
            {
                int n = 1 + (R < .5f ? 1 : 0);
                for (int i = 0; i < n; i++) { var s = Someone(x => x.friendly > .35f); if (s != null) Later(ChatChannel.Zone, s.name, Pick(new[] { "hi", "o/", "hey", "evening", "hello there" }), 2 + i * 3); }
                return;
            }
            if (t.Contains("?") && R < .5f) { var s = Someone(x => x.friendly > .5f); if (s != null) Later(ChatChannel.Zone, s.name, Pick(new[] { "not sure, sorry", "ask at the inn?", "no idea", "try the notice board" })); }
        }
        void Later(ChatChannel ch, string who, string text, float extra = 0) { pending.Add((Time.time + 2.5f + R * 4 + extra, ch, who, text)); }

        // ---------- whispers, friends and memory (5.5, 2026-10-07) ----------
        readonly Dictionary<string, float> answered = new Dictionary<string, float>();
        float nextAsk;
        /// <summary>You whispered a sim: a friend or an acquaintance answers, a stranger when it is friendly, a rival never. An
        /// answer counts for you a little (once a minute each).</summary>
        public void Whispered(SimAdventurer s, string text)
        {
            if (s == null) return;
            var st = SimMemory.Of(s); string t = text.ToLowerInvariant(); bool lingo = s.chatty > .65f;
            if (st == SimMemory.Standing.Rival) return;
            if (st == SimMemory.Standing.Stranger && s.friendly < .3f && R < .6f) return;
            string line;
            var place = PlaceIn(t);
            if (place != null) line = Directions(place, s);
            else if (t.Contains("inv") || t.Contains("group") || t.Contains("lfg") || t.Contains("party"))
                line = S.InviteRefusal(s) == null ? (lingo ? "sure, inv" : "Gladly. Send the invite.") : (lingo ? "can't rn, sry" : "I can't just now, sorry.");
            else if (t.Contains("ty") || t.Contains("thank")) line = Pick(new[] { "np", "anytime", "np :)" });
            else if (t.Contains("?")) line = Pick(st == SimMemory.Standing.Stranger ? new[] { "not sure", "no idea, sorry", "dunno" } : new[] { "hmm, not sure", "let me think... no idea", "ask at the inn maybe?" });
            else if (t == "o/" || t.StartsWith("hi") || t.StartsWith("hey") || t.StartsWith("hello") || t.Contains("evening") || t.Contains("morning"))
                line = st == SimMemory.Standing.Friend ? Pick(new[] { "hey you :)", "o/ good to see you", "hey! how's it going" }) : st == SimMemory.Standing.Acquaintance ? Pick(new[] { "hey", "o/", "hi again" }) : Pick(new[] { "hi", "hello", "o/" });
            else if (t.Contains("wts") || t.Contains("wtb") || t.Contains("sell") || t.Contains("buy")) line = Pick(new[] { "how much?", "what's your price", "maybe, what for?" });
            else line = st == SimMemory.Standing.Friend ? Pick(new[] { "ha, yeah", ":)", "true", "same" }) : st == SimMemory.Standing.Acquaintance ? Pick(new[] { "yeah", "ok", "heh" }) : Pick(new[] { "ok", "sure", "k" });
            Later(ChatChannel.Whisper, s.name, line, -1);
            if (!answered.TryGetValue(s.id, out var at) || Time.time - at > 60) { answered[s.id] = Time.time; SimMemory.Note(s, SimMemory.Deed.WhisperAnswered); }
        }
        /// <summary>You hailed a sim (H, playtest note 83): it answers in Say by what it makes of you; a rival only glares. It counts
        /// a little, like an answered whisper (once a minute).</summary>
        public void Hailed(SimAdventurer s)
        {
            if (s == null) return; var st = SimMemory.Of(s); bool lingo = s.chatty > .65f;
            if (st == SimMemory.Standing.Rival) { S.Message(s.name + " looks straight through you."); return; }
            string line = st == SimMemory.Standing.Friend ? Pick(lingo ? new[] { "o/ hey you!", "heyyy", "yo! what's up" } : new[] { "Hail, friend! Good to see you.", "Well met again!", "There you are." })
                : st == SimMemory.Standing.Acquaintance ? Pick(lingo ? new[] { "o/", "hey again", "sup" } : new[] { "Hail again.", "Well met.", "Hello there." })
                : Pick(lingo ? new[] { "o/", "hi", "hey" } : new[] { "Hail, stranger.", "Well met.", "Good day." });
            if (s.friendly < .2f && st == SimMemory.Standing.Stranger && R < .5f) line = Pick(new[] { "...", "hm.", "busy." });
            Later(ChatChannel.Say, s.name, line, -2);
            if (!answered.TryGetValue(s.id, out var at) || Time.time - at > 60) { answered[s.id] = Time.time; SimMemory.Note(s, SimMemory.Deed.WhisperAnswered); }
        }
        /// <summary>A sim came into your zone or online (SimPopulation.Refresh): a friend says hello, once an hour of the clock.</summary>
        public void Arrived(SimAdventurer s)
        {
            if (!Population.Lively || SimMemory.Of(s) != SimMemory.Standing.Friend) return;
            float hour = WorldClock.Hour; if (s.greetedHour >= 0 && Mathf.Abs(Mathf.DeltaAngle(s.greetedHour * 15, hour * 15)) < 15) return;
            s.greetedHour = hour; bool lingo = s.chatty > .65f;
            Later(ChatChannel.Whisper, s.name, Pick(lingo ? new[] { "hey, you on? o/", "yo, you about?", "o/ what are you up to" } : new[] { "Hello again. Good to see you about.", "Evening, friend. How goes it?", "Hello! Are you out hunting today?" }), 2 + R * 4);
        }
        /// <summary>A standing crossed: a friend made or lost says so.</summary>
        public void StandingChanged(SimAdventurer s, SimMemory.Standing was, SimMemory.Standing now)
        {
            if (now == SimMemory.Standing.Friend) { S.Message(s.name + " counts you a friend now."); Later(ChatChannel.Whisper, s.name, s.chatty > .65f ? "you're alright, you know. add me" : "You're good company. Let's group again some time.", 1); }
            else if (now == SimMemory.Standing.Rival) { S.Message(s.name + " has had enough of you."); if (s.chatty > .4f) Later(ChatChannel.Zone, s.name, Pick(new[] { "some people...", "ninja looters everywhere", "won't group with that one again" }), 1); }
            else if (was == SimMemory.Standing.Friend) S.Message("Things have cooled between you and " + s.name + ".");
        }
        /// <summary>Now and then a friend not in your party asks you to group (5.5), by whisper; "/invite" accepts.</summary>
        void FriendsAsk()
        {
            if (Time.time < nextAsk) return; nextAsk = Time.time + Mathf.Lerp(150, 300, R);
            if (S.PartySims.Count >= EncounterSession.MaxPartySims || S.InCombat) return;
            var friends = Here().FindAll(s => !S.InParty(s.id) && SimMemory.Of(s) == SimMemory.Standing.Friend && s.bold > .3f && Mathf.Abs(s.level - S.Progress.Level) <= EncounterSession.InviteLevelGap);
            if (friends.Count == 0 || R > .6f) return;
            var s = friends[rng.Next(friends.Count)]; var camp = CampFor(s); bool lingo = s.chatty > .65f;
            Later(ChatChannel.Whisper, s.name, camp != null ? (lingo ? "want to do " + camp + "? inv me" : "Fancy the " + camp + "? Invite me if so.") : (lingo ? "grouping? inv me" : "Want to group for a bit? Invite me if so."));
        }
        void Answer(ZoneLabel place, SimAdventurer asker)
        {
            var here = Here().FindAll(s => s.id != asker.id && s.friendly > .3f && s.level >= asker.level);
            if (here.Count == 0 || R > .75f) return;
            var s = here[rng.Next(here.Count)]; Later(ChatChannel.Zone, s.name, Directions(place, s), 1);
        }
        void AnswerRoad(ZoneExit exit, SimAdventurer asker)
        {
            var here = Here().FindAll(s => s.id != asker.id && s.friendly > .3f);
            if (here.Count == 0 || R > .75f) return;
            var s = here[rng.Next(here.Count)]; string road = exit.name; int to = road.IndexOf(" to ", StringComparison.Ordinal); if (to > 0) road = road.Substring(0, to);
            Later(ChatChannel.Zone, s.name, (s.chatty > .65f ? "take the " : "Take the ") + road.ToLowerInvariant() + " out of town, " + Compass(new Vector2(exit.at.x, exit.at.y)) + (s.chatty > .65f ? "" : "."), 1);
        }
        /// <summary>"The Old Barrow's south of the village, past Brook pond" from the zone's own places.</summary>
        string Directions(ZoneLabel place, SimAdventurer s)
        {
            string near = NearestOther(place);
            string a = place.name + (s.chatty > .65f ? "'s " : " is ") + Compass(place.at) + (near != null ? ", past " + near : "");
            return s.chatty > .65f ? a : a + ".";
        }
        static string Compass(Vector2 at)
        {
            if (at.magnitude < 25) return "right in the village";
            float deg = Mathf.Atan2(at.x, at.y) * Mathf.Rad2Deg; string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            string dir = names[Mathf.RoundToInt(Mathf.Repeat(deg, 360) / 45f) % 8];
            return (at.magnitude > 150 ? "way " : "") + dir + " of the village";
        }
        string NearestOther(ZoneLabel place)
        {
            var marks = S.Zone?.Zone.landmarks; if (marks == null) return null;
            string best = null; float bd = float.MaxValue;
            foreach (var l in marks) { if (l == place) continue; float d = Vector2.Distance(l.at, place.at); float toVillage = place.at.magnitude, lToVillage = l.at.magnitude; if (d < bd && lToVillage < toVillage && d < 90) { bd = d; best = l.name; } }
            return best;
        }

        // ---------- the zone's facts ----------
        /// <summary>A camp near the sim's level, by its mobs' name: "Grey wolves at the Old Fold".</summary>
        string CampFor(SimAdventurer s)
        {
            var c = new List<EncounterEnemy>();
            foreach (var e in S.Enemies) if (e != null && e.Camp && !e.Game && !e.Elite && e.actor.IsAlive && Mathf.Abs(e.actor.Level - s.level) <= 1) c.Add(e);
            if (c.Count == 0) return null;
            var m = c[rng.Next(c.Count)]; return Plural(m.Name) + " at " + Near(m.transform.position);
        }
        EncounterEnemy EliteUp()
        {
            var c = S.Enemies.FindAll(e => e != null && e.Camp && e.Elite && e.actor.IsAlive);
            return c.Count == 0 ? null : c[rng.Next(c.Count)];
        }
        string GatheredItem()
        {
            var zb = S.Zone; if (zb == null || S.Professions == null) return null;
            var names = new List<string>();
            foreach (var i in zb.Interactables) if (i != null && i.node != null) { var d = S.Professions.Db.Node(i.node); if (d != null && !string.IsNullOrEmpty(d.item)) { string n = S.ItemName(d.item); if (!names.Contains(n)) names.Add(n); } }
            return names.Count == 0 ? null : Pick(names);
        }
        ZoneLabel SomePlace() { var m = S.Zone?.Zone.landmarks; return m == null || m.Length == 0 ? null : m[rng.Next(m.Length)]; }
        ZoneExit SomeExit() { var e = S.Zone?.Zone.exits; return e == null || e.Length == 0 ? null : e[rng.Next(e.Length)]; }
        ZoneLabel PlaceIn(string lower)
        {
            var m = S.Zone?.Zone.landmarks; if (m == null) return null;
            ZoneLabel best = null; int bestLen = 0;
            foreach (var l in m)
            {
                if (string.IsNullOrEmpty(l.name)) continue; string n = l.name.ToLowerInvariant();
                if (lower.Contains(n) && n.Length > bestLen) { best = l; bestLen = n.Length; continue; }
                foreach (var word in n.Replace("the ", "").Split(' ')) if (word.Length >= 5 && lower.Contains(word) && word.Length > bestLen) { best = l; bestLen = word.Length; }
            }
            return best;
        }
        string Near(Vector3 p)
        {
            var m = S.Zone?.Zone.landmarks; if (m == null || m.Length == 0) return "the village";
            string best = m[0].name; float bd = float.MaxValue;
            foreach (var l in m) { float d = Vector2.Distance(l.at, new Vector2(p.x, p.z)); if (d < bd) { bd = d; best = l.name; } }
            return best;
        }
        public static string Plural(string name)
        {
            if (string.IsNullOrEmpty(name)) return name; var n = name.ToLowerInvariant();
            if (n.EndsWith("wolf")) return n.Substring(0, n.Length - 1) + "ves";
            if (n.EndsWith("s") || n.EndsWith("x")) return n + "es";
            if (n.EndsWith("y") && !n.EndsWith("ey")) return n.Substring(0, n.Length - 1) + "ies";
            return n + "s";
        }
        static string Short(SimAdventurer s) { return SimRoster.ClassName(s.classId).ToLower() + " " + s.level; }
        static string Long(SimAdventurer s) { return "level " + s.level + " " + SimRoster.ClassName(s.classId).ToLower(); }
    }
}
