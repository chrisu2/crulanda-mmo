using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Classic-MMO style HUD on a 1440x900 canvas: portrait unit frames (you, target, party), round minimap with the
    /// quest tracker under it, square action buttons with keybinds and cooldown shading, micro-menu, full-width XP bar,
    /// a chat-style log, and the M zone/world map.
    /// </summary>
    public sealed partial class EncounterHud : MonoBehaviour
    {
        public EncounterSession session;
        static bool inventoryVisible, paused, buildVisible, mapVisible, targetVisible;
        GUIStyle heading, text, small, tiny, button, slim, number, compact, tile, tileIcon, denseAction, abbrev, keybind, frameName, barText, micro;
        readonly Color ink = new Color(.045f, .065f, .075f, .94f);
        readonly Color gold = new Color(.91f, .76f, .43f);
        static readonly Color HealthGreen = new Color(.12f, .72f, .16f);
        Texture2D disc;
        readonly HudMaps maps = new HudMaps();
        public static bool BlocksPointer(Vector2 point)
        {
            var p = ScreenToCanvas(point.x, point.y);
            Vector2 fr = p - PanelOffset("frames"), mm = p - PanelOffset("minimap"), tr = p - PanelOffset("tracker"), br = p - PanelOffset("bar");   // each panel where it was put
            return paused || buildVisible || mapVisible || uiUnlocked || QuestUiBlocks(p) || WhoUiBlocks(p) || HallUiBlocks(p) || ItemUiBlocks(p) || TradesUiBlocks(p) || LootUiBlocks(p) || br.y > 795 || new Rect(10, 10, 350, 190).Contains(fr) ||
                (targetVisible && new Rect(365, 10, 350, 130).Contains(fr)) || new Rect(1215, 0, 225, 240).Contains(mm) ||
                (partySims > 0 && new Rect(10, 196, 400, 46 * partySims).Contains(fr)) || chatRect.Contains(p) ||   // the chat
                new Rect(1110, 236, 330, 200).Contains(tr) || false;
        }
        /// <summary>Capture tools hide the HUD to photograph the world.</summary>
        public static bool Hidden;

        void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            number = new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            slim = new GUIStyle(GUI.skin.button) { fontSize = 14, padding = new RectOffset(6, 6, 2, 2), alignment = TextAnchor.MiddleCenter };   // small buttons in frames (Invite, Leave)
            tiny = new GUIStyle(small) { fontSize = 13 };
            compact = new GUIStyle(button) { fontSize = 13, padding = new RectOffset(4, 4, 3, 3) };
            denseAction = new GUIStyle(button) { fontSize = 11, padding = new RectOffset(2, 2, 2, 2), wordWrap = true };
            tile = new GUIStyle(GUI.skin.button) { fontSize = 12, wordWrap = true, alignment = TextAnchor.UpperCenter, padding = new RectOffset(4, 4, 6, 4) };
            tileIcon = new GUIStyle(tile) { fontSize = 11, padding = new RectOffset(3, 3, 44, 2) };   // under the talent's icon
            abbrev = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            keybind = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft, padding = new RectOffset(3, 0, 1, 0) };
            frameName = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            barText = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            micro = new GUIStyle(GUI.skin.button) { fontSize = 12, padding = new RectOffset(2, 2, 2, 2) };
            disc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                disc.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - r) * 20)));
            }
            disc.Apply();
        }

        // Playtest note 23 (fps): the HUD drew a whole layout pass before every repaint though it uses no GUILayout; switched off.
        // Each part is timed (the capture tour's performance probe reads these markers).
        static readonly Unity.Profiling.ProfilerMarker mWorld = new Unity.Profiling.ProfilerMarker("HUD.WorldLabels"), mFrames = new Unity.Profiling.ProfilerMarker("HUD.Frames"),
            mMap = new Unity.Profiling.ProfilerMarker("HUD.Minimap"), mPanels = new Unity.Profiling.ProfilerMarker("HUD.Panels"), mWindows = new Unity.Profiling.ProfilerMarker("HUD.Windows");
        static readonly Unity.Profiling.ProfilerMarker mGather = new Unity.Profiling.ProfilerMarker("HUD.Gather"), mLayout = new Unity.Profiling.ProfilerMarker("HUD.Layout"),
            mDraw = new Unity.Profiling.ProfilerMarker("HUD.DrawPlates"), mPlaces = new Unity.Profiling.ProfilerMarker("HUD.PlaceNames");
        void Awake() { useGUILayout = false; CheckDisplay(); }
        /// <summary>Back from another window (alt-tab): an open chat line takes the keys again, and the keyboard is switched back on if
        /// the input system left it off (the tester's note 115, 2026-10-10: "After alt-tabbing or using chat, the game ignored keys until
        /// I clicked inside the window").</summary>
        void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            if (chatTyping) chatFocus = true;
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current; if (k != null && !k.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(k);
#endif
        }
        void LateUpdate() { TickPaperDoll(); }
        void OnGUI()
        {
            if (Hidden || session == null || session.Player == null) return;
            Styles();
            inventoryVisible = session.InventoryOpen; paused = session.Paused; buildVisible = session.BuildOpen;
            mapVisible = session.MapOpen; targetVisible = session.Target != null || session.HasFriendlyFocus; partySims = session.PartySims.Count; whoVisible = session.WhoOpen; bookVisible = session.QuestBookOpen; talkVisible = session.Conversation != null; hallVisible = session.Hall != null;
            bagsVisible = session.InventoryOpen; charVisible = session.CharacterOpen; vendorVisible = session.VendorNpc != null; tradesVisible = session.TradesOpen;
            bankVisible = session.BankOpen; exchangeVisible = session.ExchangeOpen;
            GUI.matrix = CanvasMatrix;   // even and centred (playtest note 94; EncounterHud.Display)
            GUI.color = Color.white;
            using (mWorld.Auto()) DrawWorldLabels();
            // The movable panels (playtest note 97): each drawn where the player put it (EncounterHud.Display, PanelOffset).
            using (mFrames.Auto()) { At("frames"); DrawPlayerFrame(); DrawPartyFrame(); DrawBuffs(); if (session.Target != null) DrawTargetFrame(); else if (session.HasFriendlyFocus) DrawFriendFrame(); }
            using (mMap.Auto()) { At("minimap"); maps.DrawMinimap(session, gold); }
            using (mPanels.Auto()) { At("tracker"); DrawQuestTracker(); At(null); DrawChat(); DrawCenter(); At("bar"); DrawActionBar(); At(null); if (session.SpellbookOpen) DrawSpellbook(); At("pet"); DrawPetBar(); At("nerve"); DrawNervePanel(); At("bar"); DrawMicroMenu(); DrawXpBar(); At(null); }
            var windows = mWindows.Auto();
            if (session.CharacterOpen) DrawCharacter();
            if (session.VendorNpc != null) DrawVendor();
            if (session.BankOpen) DrawBank();   // the capital's counters (EncounterHud.Capital.cs)
            if (session.ExchangeOpen) DrawExchange();
            if (session.InventoryOpen) DrawBags();
            if (session.TradesOpen) DrawTrades();
            if (session.BuildOpen) DrawBuild();
            if (session.MapOpen) maps.DrawWindow(session, gold, ink);
            if (session.QuestBookOpen) DrawQuestBook();
            if (session.WhoOpen) DrawWho();
            if (session.MeterOpen && !BigWindowOpen) { At("meter"); DrawMeter(); At(null); }   // not over a big window (Chris, 2026-10-13: it lay over the talents)
            if (session.ScorecardOpen) DrawScorecard();
            if (session.Conversation != null) DrawConversation();
            if (session.Hall != null) DrawHall();   // the capital's halls (EncounterHud.Halls)
            DrawLoot();
            if (uiUnlocked) DrawPanelHandles();
            DrawDiscoveryToast();   // over the windows: it lasts a few seconds
            if (!session.Player.IsAlive)
            {
                Frame(new Rect(490, 355, 460, 145));
                GUI.Label(new Rect(530, 375, 400, 40), "YOU HAVE FALLEN", heading);
                if (GUI.Button(new Rect(535, 433, 370, 44), "Recover [R]", button)) session.Recover();
            }
            if (session.InventoryOpen || session.CharacterOpen || session.VendorNpc != null || session.BankOpen || session.ExchangeOpen) DrawDragAndConfirm();
            if (session.Paused) DrawPause();
            windows.Dispose();
        }

        // ---------- helpers ----------
        void Shadow(Rect r, string s, GUIStyle style, Color c)
        {
            GUI.contentColor = new Color(0, 0, 0, .9f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, style);
            GUI.contentColor = c; GUI.Label(r, s, style); GUI.contentColor = Color.white;
        }
        readonly GUIContent measureContent = new GUIContent();   // reused for CalcSize/CalcHeight so measuring lines allocates nothing
        GUIStyle enemyPlateStyle;
        /// <summary>Text with a 1px dark outline (four diagonal copies, painted on Repaint only): readable over bright grass, sky and walls.</summary>
        void Outlined(Rect r, string s, GUIStyle style, Color c)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.contentColor = new Color(0, 0, 0, .8f * c.a);
                GUI.Label(new Rect(r.x - 1, r.y - 1, r.width, r.height), s, style); GUI.Label(new Rect(r.x + 1, r.y - 1, r.width, r.height), s, style);
                GUI.Label(new Rect(r.x - 1, r.y + 1, r.width, r.height), s, style); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, style);
            }
            GUI.contentColor = c; GUI.Label(r, s, style); GUI.contentColor = Color.white;
        }
        /// <summary>Subtle ink plate behind free-floating HUD text (quest tracker, message log) with a faint gold rule on top; alpha fades it out.</summary>
        void HudBacking(Rect r, float alpha)
        {
            if (alpha <= .01f) return;
            Fill(r, new Color(ink.r, ink.g, ink.b, .5f * alpha)); Fill(new Rect(r.x, r.y, r.width, 1), new Color(gold.r, gold.g, gold.b, .3f * alpha));
        }
        void Disc(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, disc); GUI.color = Color.white; }
        void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        /// <summary>A bar with a dark edge and centred shadowed text, like classic unit frames.</summary>
        void UnitBar(Rect r, float fill, Color c, string label)
        {
            Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0, 0, 0, .85f));
            Fill(r, new Color(c.r * .25f, c.g * .25f, c.b * .25f, 1));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), c);
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height * .35f), new Color(1, 1, 1, .12f));
            // Thin bars (the 13 px resource bar): the label gets a 20 px rect centred on the bar (the label style clips at its padded rect), so it isn't cut top and bottom.
            if (!string.IsNullOrEmpty(label)) Shadow(r.height >= 20 ? r : new Rect(r.x, r.center.y - 10, r.width, 20), label, barText, Color.white);
        }
        /// <summary>Round portrait: coloured ring, dark face with an initial, and a level badge.</summary>
        void Portrait(Vector2 c, float size, Color ring, string letter, string level)
        {
            Disc(new Rect(c.x - size / 2 - 3, c.y - size / 2 - 3, size + 6, size + 6), new Color(.12f, .1f, .07f));
            Disc(new Rect(c.x - size / 2, c.y - size / 2, size, size), ring);
            Disc(new Rect(c.x - size / 2 + 5, c.y - size / 2 + 5, size - 10, size - 10), new Color(.1f, .11f, .12f));
            Shadow(new Rect(c.x - size / 2, c.y - size / 2, size, size), letter, abbrev, ring);
            if (level != null)
            {
                var b = new Rect(c.x - size / 2 - 4, c.y + size / 2 - 22, 26, 26);
                Disc(new Rect(b.x - 2, b.y - 2, 30, 30), new Color(.12f, .1f, .07f)); Disc(b, new Color(.55f, .45f, .25f));
                Shadow(b, level, barText, Color.white);
            }
        }
        static Color ResourceColor(Crulanda.Core.ResourceKind kind)
        {
            switch (kind) { case Crulanda.Core.ResourceKind.Mana: return new Color(.18f, .38f, .9f); case Crulanda.Core.ResourceKind.Breath: return new Color(.2f, .66f, .62f); case Crulanda.Core.ResourceKind.Salt: return new Color(.86f, .86f, .8f); default: return new Color(.86f, .62f, .16f); }
        }

        // ---------- frames ----------
        void DrawPlayerFrame()
        {
            var p = session.Player; var cls = session.ClassDef;
            Fill(new Rect(58, 22, 290, 64), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(52, 54), 74, ClassColour(cls.id), cls.displayName.Substring(0, 1), session.Progress.Level.ToString());
            Shadow(new Rect(96, 24, 240, 20), "You · " + cls.displayName, frameName, Color.white);
            if (!string.IsNullOrEmpty(session.Progress.title)) Shadow(new Rect(96, 4, 300, 18), "\u201C" + session.Progress.title + "\u201D", tiny, gold);   // the title worn (Achievements)
            UnitBar(new Rect(96, 45, 244, 18), p.Health.Pool.Ratio, HealthGreen, p.Health.Pool.Current + " / " + p.Health.Pool.Max);
            UnitBar(new Rect(96, 66, 244, 13), p.Resource.Pool.Ratio, ResourceColor(cls.resource), p.Resource.Pool.Current + " " + ResourceShort(cls));
            var status = session.Kit.StatusLine;
            if (!string.IsNullOrEmpty(status)) Shadow(new Rect(96, 84, 300, 20), status, tiny, new Color(1, .92f, .75f));
        }
        void DrawPartyFrame()
        {
            var m = session.Companion; if (m == null) return;
            var a = m.actor;
            Fill(new Rect(48, 118, 214, 44), new Color(0, 0, 0, .5f));
            Portrait(new Vector2(40, 140), 46, new Color(.45f, .8f, .55f), "M", null);
            Shadow(new Rect(70, 114, 190, 22), "Mira · " + session.Progress.Level + (session.Progress.recruited ? "" : "  (not in party)"), tiny, session.Progress.recruited ? Color.white : new Color(.8f, .8f, .8f));   // 22 px: an 18 px rect clipped the p and y
            if (!session.Progress.recruited && GUI.Button(new Rect(268, 122, 60, 26), "Invite", slim)) session.InviteMira();   // 2026-10-10
            UnitBar(new Rect(70, 135, 184, 12), a.Health.Pool.Ratio, HealthGreen, "");
            UnitBar(new Rect(70, 149, 184, 8), a.Resource.Pool.Ratio, ResourceColor(Crulanda.Core.ResourceKind.Mana), "");
            Shadow(new Rect(20, 166, 320, 20), m.Activity, tiny, new Color(.85f, .9f, .85f));
            if (m.CastProgress > 0) UnitBar(new Rect(70, 186, 184, 7), m.CastProgress, new Color(.3f, .85f, .65f), "");
            // The sims in your party (Phase 5.2b): one row each under Mira, health and what they are doing, and a button to part ways.
            for (int i = 0; i < session.PartySims.Count; i++)
            {
                var c = session.PartySims[i]; if (c == null) continue;
                float y = 200 + 46 * i; var col = ClassColour(c.sim.classId);
                Fill(new Rect(48, y, 214, 40), new Color(0, 0, 0, .5f));
                Portrait(new Vector2(40, y + 20), 40, c.actor.IsAlive ? col : new Color(.4f, .4f, .4f), c.sim.name.Substring(0, 1), c.sim.level.ToString());
                Shadow(new Rect(70, y - 3, 150, 20), c.sim.name, tiny, Color.white);
                var role = c.Role; Shadow(new Rect(196, y - 3, 70, 20), role.ToString(), tiny, role == SimCompanion.PartyRole.Tank ? new Color(.8f, .85f, 1) : role == SimCompanion.PartyRole.Healer ? new Color(.6f, 1, .7f) : new Color(1, .8f, .6f));   // 5.6
                UnitBar(new Rect(70, y + 16, 184, 10), c.actor.Health.Pool.Ratio, HealthGreen, "");
                Shadow(new Rect(70, y + 26, 200, 18), c.Activity, tiny, new Color(.85f, .9f, .85f));
                if (c.Leading.HasValue) { if (GUI.Button(new Rect(268, y + 4, 50, 26), "Stop", slim)) c.StopLeading(); }
                else if (GUI.Button(new Rect(268, y + 4, 50, 26), "Lead", slim)) session.Lead(c.sim.name);
                if (GUI.Button(new Rect(322, y + 4, 60, 26), "Leave", slim)) session.LeaveParty(c.sim.id);
            }
            // Mother Quillet while she is on her way to the lock (D4): her health, how the walk or the lock goes, and the lock as a bar.
            var w = session.Weaver;
            if (w != null && w.actor != null && (w.Now == AditWeaver.Stage.Walking || w.Now == AditWeaver.Stage.Matching || w.Now == AditWeaver.Stage.Fallen))
            {
                float y = 200 + 46 * session.PartySims.Count;
                Fill(new Rect(48, y, 214, 48), new Color(0, 0, 0, .5f));
                Portrait(new Vector2(40, y + 20), 40, w.actor.IsAlive ? WeaverViolet : new Color(.4f, .4f, .4f), "Q", w.actor.Level.ToString());
                Shadow(new Rect(70, y - 3, 190, 20), AditWeaver.Name, tiny, Color.white);
                UnitBar(new Rect(70, y + 16, 184, 10), w.actor.Health.Pool.Ratio, HealthGreen, "");
                Shadow(new Rect(70, y + 26, 280, 18), w.Status, tiny, new Color(.88f, .82f, 1));
                UnitBar(new Rect(70, y + 42, 184, 4), w.Now == AditWeaver.Stage.Matching ? w.Matched : w.Walked, WeaverViolet, "");
            }
        }
        static readonly Color WeaverViolet = new Color(.68f, .5f, 1);
        /// <summary>The buffs and songs on you and on each of the party (Chris, 2026-10-14: "we need a buff ui so i know what buff/songs
        /// are on the party"): a row of chips beside each frame, each a status effect on that one (its name and seconds left) or the
        /// kit's own buff on the whole party (a verse, a rally).</summary>
        void DrawBuffs()
        {
            var kit = session.Kit; if (kit == null || session.Player == null) return;
            var party = new List<(string, float)>(kit.PartyBuffs());
            Chips(new Rect(352, 88, 420, 18), Buffs(session.Player, party));
            var m = session.Companion;
            if (m != null && m.actor != null && session.Progress.recruited) Chips(new Rect(334, 142, 460, 18), Buffs(m.actor, party));
            for (int i = 0; i < session.PartySims.Count; i++)
            {
                var c = session.PartySims[i]; if (c == null || c.actor == null) continue;
                Chips(new Rect(388, 200 + 46 * i + 16, 420, 18), Buffs(c.actor, party));
            }
        }
        List<(string, float)> Buffs(Crulanda.Gameplay.Actor a, List<(string, float)> party)
        {
            var list = new List<(string, float)>();
            var cb = a != null ? a.GetComponent<Crulanda.Combat.Combatant>() : null;
            if (cb != null && cb.Statuses != null) cb.Statuses.Each(Time.time, (id, rem) => list.Add((session.StatusName(id), rem)));
            list.AddRange(party);
            return list;
        }
        void Chips(Rect row, List<(string name, float remaining)> buffs)
        {
            float x = row.x;
            foreach (var (name, remaining) in buffs)
            {
                string text = remaining > 0 ? name + " " + Mathf.CeilToInt(remaining) + "s" : name;
                float w = tiny.CalcSize(new GUIContent(text)).x + 10; if (x + w > row.xMax) break;
                Fill(new Rect(x, row.y, w, row.height), new Color(.08f, .1f, .06f, .72f));
                Shadow(new Rect(x + 5, row.y - 1, w, row.height + 2), text, tiny, remaining > 0 && remaining < 5 ? new Color(1, .8f, .6f) : new Color(.8f, 1, .8f));
                x += w + 4;
            }
        }
        /// <summary>The target frame's line under a game animal's health (GAME-ONLY).</summary>
        public const string GameLine = "Game: it won't fight, but it will run. Skin it for its hide.";
        void DrawTargetFrame()
        {
            var t = session.Target; var a = t.actor;
            Fill(new Rect(372, 22, 290, 64), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(668, 54), 74, a.IsAlive ? new Color(.85f, .2f, .15f) : new Color(.4f, .4f, .4f), a.DisplayName.Substring(0, 1), a.IsAlive ? a.Level.ToString() : null);
            Shadow(new Rect(380, 24, 240, 20), a.DisplayName, frameName, a.IsAlive ? ConColor(a.Level) : new Color(.7f, .7f, .7f));
            UnitBar(new Rect(378, 45, 244, 18), a.Health.Pool.Ratio, HealthGreen, a.IsAlive ? a.Health.Pool.Current + " / " + a.Health.Pool.Max : "Dead · press E at the body");
            if (a.IsAlive && t.Game) Shadow(new Rect(378, 67, 330, 20), GameLine, tiny, Color.white);
            else if (a.IsAlive)
            {
                float next = t.NextSwingIn, interval = t.SwingInterval;
                var extra = WithEnrage(t, session.Kit.TargetStatus(t));
                if (!DrawBlowBar(t))   // an elite drawing back shows its heavy blow as a cast bar instead (EncounterHud.Elite)
                {
                    UnitBar(new Rect(378, 67, 244, 7), 1 - next / Mathf.Max(.01f, interval), new Color(.85f, .45f, .3f), "");
                    Shadow(new Rect(378, 78, 330, 20), (next > 0 ? "Swing in " + next.ToString("0.0") + "s" : "Swing ready") + (string.IsNullOrEmpty(extra) ? "" : "  ·  " + extra), tiny, Color.white);
                }
                if (t.Victim != null) Shadow(new Rect(378, 96, 330, 20), "Target of target: " + t.Victim.DisplayName, tiny, new Color(.85f, .85f, .85f));
            }
        }

        // ---------- right side ----------
        void DrawQuestTracker()
        {
            if (session.Quests != null && session.Zone != null) { DrawQuests(); return; }
            var r = new Rect(1120, 240, 310, 200);
            TrackerBegin(r, 1); TrackLine(new Rect(r.x, r.y, r.width, 20), session.ZoneTitle, frameName, gold);
            int total = session.Enemies.Count, dead = session.Enemies.FindAll(e => !e.actor.IsAlive).Count;
            string miraDistance = session.Progress.recruited || session.Companion == null ? "" :
                "  (" + Mathf.RoundToInt(Vector3.Distance(session.Player.transform.position, session.Companion.transform.position)) + "m)";
            var lines = new[] {
                (session.Progress.recruited, session.Objective(0, "Recruit the healer [" + KeyBindings.InteractLabel + "]") + miraDistance),
                (dead == total, session.Objective(1, "Secure the trail") + "  " + dead + "/" + total),
                (session.Progress.equippedItem == session.content.itemId, session.Objective(2, "Equip a recovered blade [I]"))
            };
            float y = r.y + 24;
            foreach (var (done, line) in lines)
            { TrackLine(new Rect(r.x, y, r.width, 36), (done ? "✓  " : "–  ") + line, tiny, done ? new Color(.6f, .6f, .6f) : new Color(1, .95f, .85f)); y += 22; }
            if (session.Zone != null) TrackLine(new Rect(r.x, y + 4, r.width, 36), session.Zone.Zone.subtitle, tiny, new Color(.75f, .75f, .72f));
            trackerH = y - r.y + (session.Zone != null ? 24 : 0);
            if (total > 0 && dead == total && GUI.Button(new Rect(r.x, y + 44, 250, 28), "Patrol returns · keep gear and XP", micro)) session.RepeatTrail();
        }

        // ---------- bottom ----------
        float chatTop = 770, chatW, chatAt; int chatCount; string chatFirst, chatLast;
        static readonly Rect ChatArea = new Rect(10, 620, 470, 150);
        const float ChatPlateSeconds = 8;
        /// <summary>The message log. Its plate hugs the lines (last pass's extent, since IMGUI paints in call order) and fades out
        /// a few seconds after the last message; hovering the log brings it back. The text itself stays, outlined.</summary>
        /// <summary>The chat (2026-10-06, playtest note 62; ZoneChat): tabs for All, Zone, Trade, LFG, Party and the game's own
        /// messages; Enter to type (/s say, /z zone, /t trade, /lfg, /p party), Enter to send, Esc to stop.</summary>
        /// <summary>One window (Round 24, playtest note 66: "one window, but colorized"): every channel together, each in its colour,
        /// with filter chips to hide one; dragged by its top edge and resized by its top-right corner (kept in PlayerPrefs).</summary>
        static readonly (string chip, ChatChannel ch)[] ChatChips = { ("Zone", ChatChannel.Zone), ("Trade", ChatChannel.Trade), ("LFG", ChatChannel.LFG), ("Party", ChatChannel.Party), ("Whisper", ChatChannel.Whisper), ("Guild", ChatChannel.Guild), ("Sys", ChatChannel.System) };
        static bool chatTyping, chatFocus, chatDrag, chatSize; static string chatTyped = ""; static Vector2 chatGrab;
        /// <summary>Opens the chat line with "/w name " ready (the sim frame's Whisper button, 5.5).</summary>
        void BeginWhisper(string name) { chatTyped = "/w " + name + " "; chatTyping = true; chatFocus = true; EncounterInput.Typing = true; }
        static Rect chatRect = new Rect(10, 592, 472, 182); static int chatHidden; static bool chatLoaded;
        static bool Shown(ChatLine l) { int bit = l.channel == ChatChannel.Say ? 1 << (int)ChatChannel.Zone : 1 << (int)l.channel; return (chatHidden & bit) == 0; }
        static bool ChipAt(Vector2 p) { for (int i = 0; i < ChatChips.Length; i++) if (new Rect(chatRect.x + 4 + i * 58, chatRect.y + 2, 56, 18).Contains(p)) return true; return false; }
        void DrawChat()
        {
            var e = Event.current;
            if (!chatLoaded)
            {
                chatLoaded = true;
                try { chatRect = new Rect(PlayerPrefs.GetFloat("chat.x", 10), PlayerPrefs.GetFloat("chat.y", 592), Mathf.Max(470, PlayerPrefs.GetFloat("chat.w", 472)), PlayerPrefs.GetFloat("chat.h", 182)); chatHidden = PlayerPrefs.GetInt("chat.hidden", 0); } catch { }
            }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                if (chatTyping) { session.PlayerChat(chatTyped); chatTyped = ""; chatTyping = false; GUI.FocusControl(null); e.Use(); }
                else if (!session.Paused) { chatTyping = true; chatTyped = ""; chatFocus = true; e.Use(); }
            }
            else if (chatTyping && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { chatTyping = false; chatTyped = ""; GUI.FocusControl(null); e.Use(); }
            EncounterInput.Typing = chatTyping;
            // The top edge drags the window; the top-right corner resizes it.
            var bar = new Rect(chatRect.x, chatRect.y, chatRect.width - 22, 22); var corner = new Rect(chatRect.xMax - 22, chatRect.y, 22, 22);
            if (e.type == EventType.MouseDown && e.button == 0) { if (corner.Contains(e.mousePosition)) { chatSize = true; e.Use(); } else if (bar.Contains(e.mousePosition) && !ChipAt(e.mousePosition)) { chatDrag = true; chatGrab = e.mousePosition - chatRect.position; e.Use(); } }
            if (e.type == EventType.MouseDrag && chatDrag) { chatRect.position = new Vector2(Mathf.Clamp(e.mousePosition.x - chatGrab.x, 0, 1440 - chatRect.width), Mathf.Clamp(e.mousePosition.y - chatGrab.y, 0, 790 - chatRect.height)); e.Use(); }
            if (e.type == EventType.MouseDrag && chatSize) { float w = Mathf.Clamp(e.mousePosition.x - chatRect.x, 470, 900), h = Mathf.Clamp(chatRect.yMax - e.mousePosition.y, 90, 500); chatRect = new Rect(chatRect.x, chatRect.yMax - h, w, h); e.Use(); }
            if (e.type == EventType.MouseUp && (chatDrag || chatSize)) { chatDrag = chatSize = false; try { PlayerPrefs.SetFloat("chat.x", chatRect.x); PlayerPrefs.SetFloat("chat.y", chatRect.y); PlayerPrefs.SetFloat("chat.w", chatRect.width); PlayerPrefs.SetFloat("chat.h", chatRect.height); } catch { } e.Use(); }
            var box = chatRect;
            Fill(box, new Color(0, 0, 0, chatTyping || box.Contains(e.mousePosition) ? .5f : .3f));
            Fill(bar, new Color(1, 1, 1, .05f)); Shadow(new Rect(corner.x + 4, corner.y + 1, 18, 20), "⤢", tiny, new Color(.8f, .8f, .78f));
            for (int i = 0; i < ChatChips.Length; i++)
            {
                var r = new Rect(box.x + 4 + i * 58, box.y + 2, 56, 18); bool on = (chatHidden & (1 << (int)ChatChips[i].ch)) == 0;
                var c = ZoneChat.Colour(ChatChips[i].ch); Fill(r, on ? new Color(c.r, c.g, c.b, .22f) : new Color(0, 0, 0, .25f));
                if (GUI.Button(r, ChatChips[i].chip, slim)) { chatHidden ^= 1 << (int)ChatChips[i].ch; try { PlayerPrefs.SetInt("chat.hidden", chatHidden); } catch { } }
            }
            float bottom = chatTyping ? box.yMax - 28 : box.yMax - 4, top = box.y + 24, y = bottom; float textW = box.width - 18;
            var lines = session.Chat;
            for (int i = lines.Count - 1; i >= 0 && y > top; i--)
            {
                var l = lines[i]; if (!Shown(l)) continue;
                string shown = l.Shown; measureContent.text = shown; var h = tiny.CalcHeight(measureContent, textW); if (y - h < top) break; y -= h;
                var colour = l.channel == ChatChannel.System ? session.LineColour(l.text, ZoneChat.Colour(ChatChannel.System)) : ZoneChat.Colour(l.channel);
                Outlined(new Rect(box.x + 8, y, textW, h), shown, tiny, colour);   // loot lines in their quality's colour
            }
            if (chatTyping)
            {
                var cc = ZoneChat.Colour(session.ChatDefault); string tag = "[" + ZoneChat.Label(session.ChatDefault) + "]"; float tw = TextWidth(tiny, tag) + 10;
                Fill(new Rect(box.x + 4, box.yMax - 26, tw, 22), new Color(cc.r, cc.g, cc.b, .25f)); Shadow(new Rect(box.x + 8, box.yMax - 25, tw, 20), tag, tiny, cc);
                GUI.SetNextControlName("chat");
                chatTyped = GUI.TextField(new Rect(box.x + 4 + tw, box.yMax - 26, box.width - 8 - tw, 22), chatTyped, 200);
                // Focus the box until it really has the keys (playtest note 89, "when I hit enter ... I should be able to type without
                // having to click on chat box"): the control only exists from this pass, so focusing it once could miss. Then the cursor
                // goes to the end (a whisper's "/w name " stays, not selected to be typed over).
                if (chatFocus)
                {
                    GUI.FocusControl("chat");
                    if (GUI.GetNameOfFocusedControl() == "chat" && GUIUtility.keyboardControl != 0)
                    {
                        var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        if (te != null) { te.text = chatTyped; te.MoveTextEnd(); }
                        if (e.type == EventType.Repaint) chatFocus = false;
                    }
                }
            }
            // (The key hints that ran along the foot of the screen live in Esc > Controls now: Chris, 2026-10-07, "should be removed and put in an esc menu".)
        }
        void DrawCenter()
        {
            var prompt = session.InteractPrompt;
            if (prompt != null)
            {
                Fill(new Rect(520, 692, 400, 34), new Color(0, 0, 0, .55f));
                Shadow(new Rect(530, 697, 390, 26), "[" + KeyBindings.InteractLabel + "]  " + prompt, text, gold);
            }
            var bar = new Rect(560, 744, 320, 12);
            if (session.PlayerCasting)
            {
                UnitBar(bar, session.PlayerCastProgress, new Color(.95f, .75f, .2f), session.PlayerCastName);
                Shadow(new Rect(bar.x, bar.yMax + 2, bar.width, 18), "moving interrupts", tiny, new Color(.8f, .8f, .8f));
            }
            else if (session.AutoAttack || session.Target != null)
            {
                UnitBar(new Rect(bar.x, bar.y + 4, bar.width, 5), session.AutoAttack ? 1 - session.SwingRemaining / Mathf.Max(.01f, session.content.playerSwingInterval) : 0, gold, "");
                Shadow(new Rect(bar.x, bar.y + 10, bar.width, 18), session.AutoAttackStatus, tiny, new Color(.9f, .9f, .85f));
            }
        }
        static string Abbreviation(string name)
        {
            var words = name.Split(' ');
            if (words.Length >= 2) return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
            return name.Length <= 3 ? name.ToUpperInvariant() : name.Substring(0, 3).ToUpperInvariant();
        }
        /// <summary>
        /// The companion bar (playtest note 93, 2026-10-07: "a companion UI element that I can have him attack, assist, stay"): while the
        /// Ranger's wolf is out, left of the action bar, its health and three orders, the standing one lit.
        /// </summary>
        /// <summary>The pet frame's name: the Bonecaller's thrall is not a wolf (step-4 frames).</summary>
        public static string PetLabel(string kind) { return kind == "thrall" ? "Thrall" : "Wolf"; }
        /// <summary>The Scoundrel's Nerve, its own small panel to move where you like (Chris, 2026-10-14: "need to see how much nerve the
        /// scoundrel has built up", "a seperate small ui element i can move around"): the number, and ten pips, one lit for each tenth
        /// built; lit gold when there is enough for Settle the Score to be worth it (half or more).</summary>
        void DrawNervePanel()
        {
            var cls = session.ClassDef; if (cls == null || cls.id != "class.scoundrel" || session.Player == null) return;
            var pool = session.Player.Resource.Pool; var box = new Rect(380, 96, 250, 44);
            Fill(box, new Color(.07f, .06f, .05f, .9f)); Fill(new Rect(box.x, box.y, box.width, 2), new Color(.9f, .72f, .3f));
            Shadow(new Rect(box.x + 10, box.y + 4, 120, 20), "Nerve", tiny, new Color(1, .92f, .75f));
            Shadow(new Rect(box.x + 150, box.y + 4, 90, 20), pool.Current + " / " + pool.Max, tiny, Color.white);
            int lit = Mathf.FloorToInt(pool.Ratio * 10 + .001f); var gold = pool.Ratio >= .5f ? new Color(1, .82f, .3f) : new Color(.85f, .6f, .25f);
            for (int i = 0; i < 10; i++) Fill(new Rect(box.x + 10 + i * 23.2f, box.y + 26, 20, 10), i < lit ? gold : new Color(.25f, .22f, .16f, .9f));
        }
        void DrawPetBar()
        {
            var pet = session.Pet; if (pet == null || pet.actor == null || !pet.actor.IsAlive) return;
            float total = session.ActionCount * 56 - 6, x0 = 720 - total / 2 - 12 - 262, y = 800;
            var box = new Rect(x0, y, 250, 76);
            Fill(box, new Color(.07f, .06f, .05f, .9f)); Fill(new Rect(box.x, box.y, box.width, 2), new Color(.62f, .52f, .33f));
            var hp = pet.actor.Health.Pool;
            Shadow(new Rect(box.x + 8, box.y + 4, 120, 20), PetLabel(pet.Kind), tiny, Color.white);
            var bar = new Rect(box.x + 60, box.y + 9, 182, 10); Fill(bar, new Color(0, 0, 0, .6f));
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hp.Ratio), bar.height), new Color(.3f, .75f, .3f));
            string[] labels = { "Attack", "Assist", "Stay" };
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(box.x + 8 + i * 80, box.y + 30, 74, 36);
                bool lit = i == 1 && pet.Mode == RangerPet.Order.Assist || i == 2 && pet.Mode == RangerPet.Order.Stay || i == 0 && pet.Quarry != null;
                if (lit) Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(.95f, .78f, .35f, .9f));
                if (GUI.Button(r, labels[i], slim))
                {
                    if (i == 0) session.PetAttack(); else session.PetOrder(i == 1 ? RangerPet.Order.Assist : RangerPet.Order.Stay);
                }
            }
        }
        void DrawActionBar()
        {
            int n = session.BarSlots; const float size = 50, gap = 6;   // the chosen bar (EncounterSession.Bar, Docs/ACTION_BAR.md)
            float total = n * (size + gap) - gap, x0 = 720 - total / 2, y = 808;
            Fill(new Rect(x0 - 12, y - 10, total + 24, 82), new Color(.07f, .06f, .05f, .9f));
            Fill(new Rect(x0 - 12, y - 10, total + 24, 2), new Color(.62f, .52f, .33f));
            string hover = null; var mouse = Event.current.mousePosition;
            for (int b = 0; b < n; b++)
            {
                int i = session.BarKitSlot(b); var r = new Rect(x0 + b * (size + gap), y, size, size);
                bool placing = session.SpellbookOpen && session.BookPick != null;
                if (i < 0)   // an empty slot: the spellbook fills it
                {
                    Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), placing ? gold : new Color(.25f, .25f, .25f)); Fill(r, new Color(.1f, .09f, .08f));
                    Shadow(r, EncounterInput.SlotLabel(b), keybind, new Color(.6f, .6f, .6f));
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { if (placing) { session.PlaceOnBar(b, session.BookPick); session.BookPick = null; } else session.SpellbookOpen = true; }
                    if (r.Contains(mouse)) hover = "An empty slot: choose what goes here in the spellbook (" + "P)";
                    continue;
                }
                var a = session.ActionAt(i);
                float remaining = session.CooldownRemaining(i); string locked = session.ActionLockLabel(i);
                bool usable = session.Player.IsAlive && !session.Paused && locked == null;
                Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), locked == null ? new Color(.62f, .52f, .33f) : new Color(.25f, .25f, .25f));
                Fill(r, locked == null ? new Color(.16f, .2f, .24f) : new Color(.1f, .1f, .1f));
                Fill(new Rect(r.x, r.y, r.width, r.height / 2), new Color(1, 1, 1, .05f));
                bool cooling = remaining > 0 && a.cooldown > 0;
                // Its painted icon (playtest note 11): an ability's own, or the talent that gives it; greyed while locked.
                var icon = IconDb.Ability(a.id) ?? IconDb.Talent(a.id);
                if (icon != null)
                {
                    GUI.color = locked == null ? Color.white : new Color(.42f, .42f, .42f);
                    GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), icon, ScaleMode.ScaleToFit); GUI.color = Color.white;
                }
                else if (!cooling) Shadow(r, Abbreviation(a.name), abbrev, locked == null ? Color.white : new Color(.45f, .45f, .45f));
                if (cooling)
                {
                    float frac = Mathf.Clamp01(remaining / Mathf.Max(a.cooldown, .01f));
                    Fill(new Rect(r.x, r.y, r.width, r.height * frac), new Color(0, 0, 0, .62f));
                    Shadow(r, remaining < 10 ? remaining.ToString("0.0") : Mathf.CeilToInt(remaining).ToString(), abbrev, new Color(1, .95f, .6f));
                }
                else if (remaining > 0) Fill(r, new Color(0, 0, 0, .35f));
                Shadow(r, EncounterInput.SlotLabel(b), keybind, new Color(.9f, .9f, .9f));
                string sub = locked ?? (a.cost > 0 ? a.cost.ToString() : "");
                if (sub.Length > 0) Shadow(new Rect(r.x, r.yMax - 16, r.width - 3, 16), sub, new GUIStyle(keybind) { alignment = TextAnchor.LowerRight, fontSize = 10 }, locked == null ? new Color(.7f, .85f, 1) : new Color(.8f, .6f, .5f));
                Shadow(new Rect(r.x - 6, r.yMax + 2, r.width + 12, 16), a.name.Length > 10 ? a.name.Substring(0, 9) + "…" : a.name, new GUIStyle(barText) { fontSize = 10, wordWrap = false, clipping = TextClipping.Clip }, new Color(.85f, .82f, .75f));
                if (placing) Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, 2), gold);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { if (placing) { session.PlaceOnBar(b, session.BookPick); session.BookPick = null; } else if (session.SpellbookOpen) session.BookPick = session.Kit.BookKey(i); else if (usable) session.UseAbility(i); }
                if (r.Contains(mouse))
                    hover = a.name + (locked != null ? "  (" + locked + ")" : "") + "\n" + a.description +
                        (a.cost > 0 ? "\nCost: " + a.cost + " " + ResourceName(session.ClassDef) : "") + (a.cooldown > 0 ? "   Cooldown: " + a.cooldown + "s" : "");
            }
            // The stance bar (the Druid's forms, Shift+1-4), small, above the left end of the chosen bar.
            var stances = session.Kit.StanceSlots;
            for (int s2 = 0; s2 < stances.Length; s2++)
            {
                int i = stances[s2]; var a = session.ActionAt(i); if (a == null) continue;
                var r = new Rect(x0 + s2 * 40, y - 52, 36, 36); string locked = session.ActionLockLabel(i); bool active = locked == "Active";
                Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), active ? gold : new Color(.35f, .3f, .22f));
                var icon = IconDb.Ability(a.id) ?? IconDb.Talent(a.id);
                if (icon != null) GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), icon, ScaleMode.ScaleToFit); else Shadow(r, Abbreviation(a.name), abbrev, Color.white);
                Shadow(new Rect(r.x + 2, r.y, r.width, 14), "⇧" + (s2 + 1), new GUIStyle(keybind) { fontSize = 9 }, new Color(.9f, .9f, .9f));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && session.Player.IsAlive && !session.Paused) session.UseAbility(i);
                if (r.Contains(mouse)) hover = a.name + (locked != null && !active ? "  (" + locked + ")" : active ? "  (your form)" : "") + "\n" + a.description + "\nShift+" + (s2 + 1);
            }
            if (hover != null)
            {
                var h = small.CalcHeight(new GUIContent(hover), 380) + 16; var tr = new Rect(x0 + total - 400, y - 20 - h, 400, h);
                Fill(tr, new Color(.04f, .05f, .07f, .95f)); Fill(new Rect(tr.x, tr.y, tr.width, 2), gold);
                GUI.Label(new Rect(tr.x + 10, tr.y + 8, 380, h), hover, small);
            }
        }
        /// <summary>The spellbook (P; Docs/ACTION_BAR.md): every ability of yours and to come; click one, then a bar slot, to put it there.
        /// Alternate advancement at the foot: the points past the level cap and the next slot to buy.</summary>
        /// <summary>A window that fills much of the screen: the meter keeps out of its way.</summary>
        bool BigWindowOpen { get { return session.BuildOpen || session.CharacterOpen || session.InventoryOpen || session.QuestBookOpen || session.MapOpen || session.TradesOpen || session.SpellbookOpen; } }
        void DrawSpellbook()
        {
            var book = session.Book(); const float w = 560; int rows = (book.Count + 1) / 2;
            float h = 128 + rows * 46; var box = new Rect(720 - w / 2, Mathf.Max(40, 780 - h - 40), w, h);
            Fill(box, new Color(.06f, .05f, .04f, .96f)); Fill(new Rect(box.x, box.y, box.width, 2), gold);
            Shadow(new Rect(box.x + 14, box.y + 8, 300, 24), "SPELLBOOK", frameName, gold);
            Shadow(new Rect(box.x + 14, box.y + 32, w - 60, 18), "Your bar has " + session.BarSlots + " slots. Click an ability, then a slot on the bar. P or Esc closes.", tiny, Color.white);
            if (GUI.Button(new Rect(box.xMax - 32, box.y + 6, 24, 22), "x", slim)) { session.SpellbookOpen = false; session.BookPick = null; }
            var mouse = Event.current.mousePosition; string tip = null;
            for (int k = 0; k < book.Count; k++)
            {
                var e = book[k]; var a = session.ActionAt(e.slot); if (a == null) continue;
                var r = new Rect(box.x + 14 + (k % 2) * (w / 2 - 6), box.y + 58 + (k / 2) * 46, w / 2 - 22, 40);
                bool picked = session.BookPick == e.key; int at = session.Progress.bar.IndexOf(e.key);
                Fill(r, picked ? new Color(.38f, .3f, .12f) : new Color(.12f, .11f, .09f));
                if (picked) Fill(new Rect(r.x, r.y, r.width, 2), gold);
                var icon = IconDb.Ability(a.id) ?? IconDb.Talent(a.id);
                if (icon != null) { GUI.color = e.yours ? Color.white : new Color(.45f, .45f, .45f); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, 34, 34), icon, ScaleMode.ScaleToFit); GUI.color = Color.white; }
                Shadow(new Rect(r.x + 42, r.y + 2, r.width - 46, 18), a.name, small, e.yours ? Color.white : new Color(.55f, .55f, .55f));
                string note = !e.yours ? (session.ActionLockLabel(e.slot) ?? "to come") : at >= 0 ? "on the bar: " + EncounterInput.SlotLabel(at) : "not on the bar";
                Shadow(new Rect(r.x + 42, r.y + 20, r.width - 46, 16), note, tiny, at >= 0 ? new Color(.7f, .85f, 1) : new Color(.75f, .7f, .6f));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) session.BookPick = picked ? null : e.key;
                if (r.Contains(mouse)) tip = a.name + "\n" + a.description;
            }
            float ay = box.yMax - 58; Fill(new Rect(box.x + 10, ay - 6, w - 20, 1), new Color(.62f, .52f, .33f, .6f));
            bool capped = session.Progress.Level >= EncounterProgress.LevelCap; int extra = session.Progress.aaSlots, have = session.AaAvailable;
            string aa = !capped ? "Alternate advancement opens at level " + EncounterProgress.LevelCap + ": experience past it earns points that buy up to " + ActionBarRules.MaxExtraSlots + " more bar slots."
                : extra >= ActionBarRules.MaxExtraSlots ? "Alternate advancement: all " + ActionBarRules.MaxExtraSlots + " extra slots bought (" + have + " points to spare)."
                : "Alternate advancement: " + have + " point" + (have == 1 ? "" : "s") + " (" + ActionBarRules.AaProgress(session.Progress.experience) + " of " + ActionBarRules.AaPointXp + " experience to the next). The next slot costs " + session.NextSlotCost + ".";
            GUI.Label(new Rect(box.x + 14, ay, w - 170, 48), aa, tiny);
            GUI.enabled = capped && extra < ActionBarRules.MaxExtraSlots && have >= session.NextSlotCost;
            if (GUI.Button(new Rect(box.xMax - 146, ay + 4, 132, 28), "Buy a bar slot", slim)) session.BuyBarSlot();
            GUI.enabled = true;
            if (tip != null)
            {
                var th = small.CalcHeight(new GUIContent(tip), 360) + 14; var tr = new Rect(box.xMax + 8, box.y, 380, th);
                Fill(tr, new Color(.04f, .05f, .07f, .95f)); Fill(new Rect(tr.x, tr.y, tr.width, 2), gold); GUI.Label(new Rect(tr.x + 10, tr.y + 7, 360, th), tip, small);
            }
        }
        /// <summary>
        /// One Menu button at the bottom right (Chris, 2026-10-05: the row of Trades, Character, Talents, Bags, Map, Save and Load
        /// "should be condensed into one menu button"): it opens a short list over itself, each line with its key, and a choice
        /// shuts the list. A gold dot on the button (and on Talents) while talent points wait.
        /// </summary>
        bool menuOpen;
        void DrawMicroMenu()
        {
            int points = session.Talents.Available(session.Progress);
            var button = new Rect(1330, 842, 96, 30);
            if (GUI.Button(button, menuOpen ? "Menu  ▼" : "Menu  ▲", micro)) menuOpen = !menuOpen;
            if (points > 0) Disc(new Rect(button.xMax - 10, button.y - 6, 14, 14), new Color(1, .82f, .2f));
            if (!menuOpen) return;
            var items = new List<(string label, System.Action act)> {
                ("Character   [C]", () => { session.CharacterOpen = !session.CharacterOpen; if (session.CharacterOpen) session.ShowTrades(false); }),
                (points > 0 ? "Talents " + points + "   [B]" : "Talents   [B]", () => session.BuildOpen = !session.BuildOpen),
                ("Bags   [I]", () => session.InventoryOpen = !session.InventoryOpen),
            };
            if (session.Professions != null) items.Add(("Trades   [K]", () => session.ShowTrades(!session.TradesOpen)));
            items.Add(("Map   [M]", () => session.MapOpen = !session.MapOpen));
            if (SimPopulation.Active != null) items.Add(("Who   [" + KeyBindings.Label(GameKey.Who) + "]", () => session.WhoOpen = !session.WhoOpen));   // 2026-10-10: Chris found no way to it
            items.Add(("Save", () => session.Save()));
            items.Add(("Load", () => session.Load()));
            float w = 170, h = 32, top = button.y - 6 - items.Count * (h + 2);
            var panel = new Rect(button.xMax - w - 6, top - 6, w + 12, items.Count * (h + 2) + 8);
            Fill(panel, new Color(.04f, .05f, .07f, .94f)); Fill(new Rect(panel.x, panel.y, panel.width, 2), gold);
            for (int k = 0; k < items.Count; k++)
            {
                var r = new Rect(panel.x + 6, top + k * (h + 2), w, h);
                if (GUI.Button(r, items[k].label, micro)) { menuOpen = false; items[k].act(); }
                if (points > 0 && items[k].label.StartsWith("Talents")) Disc(new Rect(r.xMax - 12, r.y - 4, 12, 12), new Color(1, .82f, .2f));
            }
            // A click anywhere else shuts it.
            if (Event.current.type == EventType.MouseDown && !panel.Contains(Event.current.mousePosition) && !button.Contains(Event.current.mousePosition)) menuOpen = false;
        }
        void DrawXpBar()
        {
            var pr = session.Progress; float xp = pr.XpIntoLevel / (float)pr.XpLevelSize; bool capped = pr.Level >= EncounterProgress.LevelCap;
            var r = new Rect(0, 888, 1440, 12);
            Fill(r, new Color(.05f, .03f, .08f, .95f));
            Fill(new Rect(0, r.y + 1, 1440 * (capped ? 1 : xp), r.height - 2), capped ? new Color(.45f, .38f, .2f) : new Color(.55f, .22f, .75f));
            for (int i = 1; i < 20; i++) Fill(new Rect(i * 72, r.y, 1, r.height), new Color(0, 0, 0, .6f));
            Shadow(new Rect(0, r.y - 3, 1440, 18), capped ? "Level 10 · prototype cap" : "Level " + pr.Level + "  ·  XP " + pr.XpIntoLevel + " / " + pr.XpLevelSize, new GUIStyle(barText) { fontSize = 10 }, Color.white);
        }
        /// <summary>A class's colour on its portrait and in the class list.</summary>
        public static Color ClassColour(string classId)
        {
            switch (classId)
            {
                case "class.druid": return new Color(.95f, .5f, .15f);
                case "class.paladin": return new Color(.95f, .82f, .4f);
                case "class.ranger": return new Color(.45f, .75f, .3f);
                case "class.mage": return new Color(.6f, .45f, .95f);
                case "class.rogue": return new Color(.95f, .85f, .35f);
                case "class.archivist": return new Color(.55f, .8f, 1f);
                case "class.saltmender": return new Color(.92f, .92f, .86f);
                case "class.cleric": return new Color(1f, .95f, .72f);
                case "class.shaman": return new Color(.4f, .6f, 1f);
                case "class.scoundrel": return new Color(.86f, .36f, .44f);   // claret and rose (2026-10-14)
                default: return new Color(.78f, .61f, .43f);
            }
        }
        void DrawPause()
        {
            // Each class is a separate character with its own save; switching saves this one first. One button per other class.
            var others = new List<PlayableClass>();
            foreach (var c in session.content.AllClasses()) if (c.definition.id != session.ClassDef.id) others.Add(c);
            // (2026-10-13, Chris: "cant see all classes. need another mechanism to be able to choose": one button a class ran off the screen
            // at ten classes; now a grid of three across.)
            int rows = (others.Count + 2) / 3;
            Frame(new Rect(475, 325, 490, 296 + 48 * rows));
            GUI.Label(new Rect(525, 351, 400, 35), "EXPEDITION PAUSED", heading);
            GUI.Label(new Rect(525, 398, 400, 35), "Progress autosaves out of combat.", text);
            if (controlsOpen) { DrawControls(); return; }
            if (displayOpen) { DrawDisplay(); return; }
            if (soundOpen) { DrawSound(); return; }
            if (GUI.Button(new Rect(525, 448, 126, 44), "Resume [Esc]", button)) session.Resume();
            if (GUI.Button(new Rect(657, 448, 126, 44), "Controls", button)) controlsOpen = true;
            if (GUI.Button(new Rect(789, 448, 126, 44), "Display", button)) displayOpen = true;   // resolution, mode, UI scale (playtest note 94)   // key bindings (playtest note 75)
            GUI.enabled = !session.InCombat;
            if (others.Count > 0) GUI.Label(new Rect(525, 500, 400, 30), "Play another character (each its own save):", text);
            for (int i = 0; i < others.Count; i++)
                if (GUI.Button(new Rect(525 + (i % 3) * 132, 534 + 48 * (i / 3), 126, 44), others[i].definition.displayName, button)) { session.SwitchCharacter(others[i].definition.id); break; }
            GUI.enabled = true;
            // Chris 2026-10-14: "all need an exit command to close the .exe" (also /exit and /quit in the chat): saved first, out of combat.
            if (GUI.Button(new Rect(525, 540 + 48 * rows + 8, 126, 44), "Quit game", button)) session.Quit();
            if (GUI.Button(new Rect(657, 540 + 48 * rows + 8, 126, 44), "Sound", button)) soundOpen = true;   // Chris 2026-10-15: sliders and mutes
        }
        public static string ResourceName(Crulanda.Core.ResourceKind kind) { return kind == Crulanda.Core.ResourceKind.Breath ? "Shift Breath" : kind.ToString(); }
        static string ResourceShort(Crulanda.Core.ResourceKind kind) { return kind == Crulanda.Core.ResourceKind.Breath ? "Breath" : kind.ToString(); }
        /// <summary>The class's own name for its pool when it has one (the Scoundrel's Nerve), else the kind's.</summary>
        public static string ResourceName(ClassDefinition cls) { return cls != null && !string.IsNullOrEmpty(cls.resourceName) ? cls.resourceName : ResourceName(cls != null ? cls.resource : Crulanda.Core.ResourceKind.None); }
        static string ResourceShort(ClassDefinition cls) { return cls != null && !string.IsNullOrEmpty(cls.resourceName) ? cls.resourceName : ResourceShort(cls != null ? cls.resource : Crulanda.Core.ResourceKind.None); }
        public static string Pips(int value, int max) { return new string('●', Mathf.Clamp(value, 0, max)) + new string('○', Mathf.Max(0, max - value)); }
        // Panel geometry follows the branch count: 3 trees fit the original panel; 4 (Druid) use the full width.
        float PanelX, PanelW, TreeLeft, TreeWidth, CellWidth, TileWidth, TileHeight = 96, TierHeight = 128;
        const float TierTop = 272, TreesBottom = 690;
        /// <summary>The rows close up when a tree is four deep (Chris, 2026-10-13: the Mender's fourth row ran over Refund all and Close):
        /// every row fits between the trees' top and the hint line at 700; three rows keep the old 128 spacing.</summary>
        void LayoutTrees(int branches, int rows = 3)
        {
            TierHeight = Mathf.Min(128, (TreesBottom - TierTop) / Mathf.Max(1, rows)); TileHeight = Mathf.Min(96, TierHeight - 8);
            PanelX = branches > 3 ? 20 : 155; PanelW = branches > 3 ? 1400 : 1130; TreeLeft = PanelX + 25;
            TreeWidth = (PanelW - 50 - 12 * (branches - 1)) / branches; CellWidth = TreeWidth / 4f; TileWidth = CellWidth - 6;
        }
        Rect TileRect(int branch, TalentNodeData n)
        { return new Rect(TreeLeft + branch * (TreeWidth + 12) + n.col * CellWidth + 2, TierTop + n.tier * TierHeight, TileWidth, TileHeight); }
        void DrawBuild()
        {
            var tree = session.Talents; var p = session.Progress; int deepest = 0; foreach (var br in tree.Branches) foreach (var nd in br.nodes) deepest = Mathf.Max(deepest, nd.tier);
            LayoutTrees(tree.Branches.Count, deepest + 1);
            GUI.color = new Color(ink.r, ink.g, ink.b, 1); GUI.DrawTexture(new Rect(PanelX, 95, PanelW, 685), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(TreeLeft, 108, 850, 32), session.ClassDef.displayName.ToUpperInvariant() + " TALENTS", heading);
            GUI.Label(new Rect(TreeLeft, 142, PanelW - 50, 40), "Level " + p.Level + "  ·  " + tree.Available(p) + " of " + tree.Budget(p.Level) +
                " points available  ·  5 points in a tree unlock its next row  ·  Left-click invest, right-click refund" +
                (Debug.isDebugBuild && p.Level < 10 ? "  ·  F10: jump to level 10 (dev)" : ""), small);
            string hover = null; var mouse = Event.current.mousePosition;
            for (int b = 0; b < tree.Branches.Count; b++)
            {
                var branch = tree.Branches[b]; float x = TreeLeft + b * (TreeWidth + 12);
                GUI.color = new Color(1, 1, 1, .05f); GUI.DrawTexture(new Rect(x - 6, 180, TreeWidth, 520), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.contentColor = gold;
                GUI.Label(new Rect(x, 184, 240, 26), branch.name.ToUpperInvariant() + "   " + tree.SpentIn(p, branch), text);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(x, 208, TreeWidth - 12, 60), branch.loop, tiny);
                foreach (var n in branch.nodes)
                {
                    if (string.IsNullOrEmpty(n.req)) continue;
                    var parent = tree.Find(n.req); if (parent == null) continue;
                    Rect from = TileRect(b, parent), to = TileRect(b, n);
                    GUI.color = TalentTree.Rank(p, parent.id) >= parent.max ? gold : new Color(.4f, .45f, .47f);
                    GUI.DrawTexture(new Rect(from.center.x - 2, from.yMax, 4, to.y - from.yMax), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                foreach (var n in branch.nodes)
                {
                    var r = TileRect(b, n); int rank = TalentTree.Rank(p, n.id);
                    bool canAdd = session.CanEditBuild && tree.Propose(p, n.id, 1, out _, out _);
                    bool canRemove = session.CanEditBuild && rank > 0 && tree.Propose(p, n.id, -1, out _, out _);
                    // State colors mirror the calculator: gold = maxed, green = invested or available, dim = locked.
                    Color edge = rank >= n.max ? gold : rank > 0 || canAdd ? new Color(.42f, .76f, .35f) : new Color(.3f, .33f, .35f);
                    GUI.color = edge; GUI.DrawTexture(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), Texture2D.whiteTexture); GUI.color = Color.white;
                    GUI.contentColor = rank > 0 || canAdd ? Color.white : new Color(.6f, .62f, .63f);
                    string kind = n.kind == "active" ? "ACTION" : n.kind == "modifier" ? "MODIFIES" : "";
                    // Its painted icon at the top (playtest note 11), the name and rank under it; the old text tile without one.
                    var icon = IconDb.Talent(n.id);
                    if (icon == null) { if (GUI.Button(r, n.name + "\n" + (kind.Length > 0 ? kind + "\n" : "") + rank + "/" + n.max, tile) && canAdd) session.ChangeTalent(n.id, 1); }
                    else
                    {
                        if (GUI.Button(r, n.name + "\n" + (kind.Length > 0 ? kind + "  " : "") + rank + "/" + n.max, tileIcon) && canAdd) session.ChangeTalent(n.id, 1);
                        GUI.color = rank > 0 || canAdd ? Color.white : new Color(.5f, .5f, .5f);
                        GUI.DrawTexture(new Rect(r.center.x - 18, r.y + 5, 36, 36), icon, ScaleMode.ScaleToFit); GUI.color = Color.white;
                    }
                    GUI.contentColor = Color.white;
                    if (r.Contains(mouse))
                    {
                        hover = n.name + "  (" + rank + "/" + n.max + ")  —  " + n.desc + "\n" + tree.Requirement(p, n.id);
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        { if (canRemove) session.ChangeTalent(n.id, -1); Event.current.Use(); }
                    }
                }
            }
            if (hover == null) hover = session.CanEditBuild ? "Hover a talent for details. Changes save immediately; full respec is free during the prototype." :
                "Talents are read-only while fighting, dead or paused.";
            GUI.Label(new Rect(TreeLeft, 700, PanelW - 440, 72), hover, small);
            GUI.enabled = session.CanEditBuild;
            if (GUI.Button(new Rect(PanelX + PanelW - 400, 715, 180, 40), "Refund all", button)) session.ResetTalents();
            GUI.enabled = true;
            if (GUI.Button(new Rect(PanelX + PanelW - 210, 715, 180, 40), "Close [B]", button)) session.BuildOpen = false;
        }
        // ---------- world labels ----------
        // Nameplates, ! and ? markers, speech bubbles and place names float over the world. They keep off the HUD panels and
        // inside the screen, hide behind solid scenery (one ray each), and stack instead of overprinting: the nearest keeps its
        // place and farther ones move up out of its way. Drawn on Repaint only (labels take no input), so the rays run once a frame.
        struct Plate { public float dist, fade, top; public Vector2 at; public Rect box; public Villager v; public EncounterEnemy e; public SimFigure sim; public SimCompanion simParty; public bool mira, named, shown, grey; public char mark; public string name, title; }
        static int partySims;
        readonly System.Collections.Generic.List<Plate> plates = new System.Collections.Generic.List<Plate>(48);
        readonly System.Collections.Generic.List<Rect> taken = new System.Collections.Generic.List<Rect>(64);
        readonly System.Collections.Generic.Dictionary<EncounterEnemy, (float height, int level, string label)> enemyPlates = new System.Collections.Generic.Dictionary<EncounterEnemy, (float height, int level, string label)>();
        readonly System.Collections.Generic.Dictionary<string, string> bracketed = new System.Collections.Generic.Dictionary<string, string>();
        readonly Rect[] hud = new Rect[10]; int hudCount;
        static readonly RaycastHit[] sightHits = new RaycastHit[12];
        static readonly GUIContent measure = new GUIContent();
        /// <summary>The quest tracker's area as last drawn (world labels keep out of it). The old test map's tracker is fixed.</summary>
        Rect trackerArea = new Rect(1110, 236, 330, 210);
        GUIStyle centered, plateText;
        bool ToCanvas(Vector3 world, out Vector2 p)
        {
            var s = session.View.WorldToScreenPoint(world); p = ScreenToCanvas(s.x, s.y);
            return s.z > 0 && p.x > -100 && p.x < 1540 && p.y > 0 && p.y < 900;
        }
        // Text widths and sight lines are kept (playtest note 23, fps: the nameplates and place names cost 2.2 ms a frame, measuring
        // every label and casting a ray for each, every frame). A width never changes; a sight line is looked at again every 0.2 s.
        readonly System.Collections.Generic.Dictionary<GUIStyle, System.Collections.Generic.Dictionary<string, float>> widths = new System.Collections.Generic.Dictionary<GUIStyle, System.Collections.Generic.Dictionary<string, float>>();
        float TextWidth(GUIStyle style, string s)
        {
            if (!widths.TryGetValue(style, out var known)) widths[style] = known = new System.Collections.Generic.Dictionary<string, float>();
            if (!known.TryGetValue(s, out var w)) { if (known.Count > 2000) known.Clear(); measure.text = s; known[s] = w = style.CalcSize(measure).x; }
            return w;
        }
        readonly System.Collections.Generic.Dictionary<object, (float until, bool hidden)> sight = new System.Collections.Generic.Dictionary<object, (float, bool)>();
        bool Occluded(object key, Vector3 point, float slack = .3f)
        {
            if (sight.TryGetValue(key, out var known) && Time.unscaledTime < known.until) return known.hidden;
            if (sight.Count > 1000) sight.Clear();
            bool hidden = Occluded(point, slack); sight[key] = (Time.unscaledTime + .15f + (key.GetHashCode() & 7) * .01f, hidden);
            return hidden;
        }
        string Bracketed(string title) { if (!bracketed.TryGetValue(title, out var s)) bracketed[title] = s = "<" + title + ">"; return s; }
        /// <summary>The HUD panels showing this frame: unit frames, minimap, quest tracker, chat, the centre prompt and bars, and the bars along the bottom.</summary>
        void CollectHudRects()
        {
            hudCount = 0; var mini = HudMaps.MinimapRect;
            hud[hudCount++] = new Rect(6, 6, 356, 104);                                                          // your frame and its status line
            if (session.Companion != null) hud[hudCount++] = new Rect(6, 112, 264, 84);                           // Mira's frame
            if (targetVisible) hud[hudCount++] = new Rect(362, 8, 380, 114);                                     // target or friend frame
            hud[hudCount++] = new Rect(mini.x - 42, 0, 1440 - mini.x + 42, mini.yMax + 28);                      // minimap, its name and buttons
            if (trackerArea.height > 0) hud[hudCount++] = trackerArea;
            hud[hudCount++] = new Rect(6, 614, 480, 160);                                                        // chat
            hud[hudCount++] = new Rect(516, 688, 408, 90);                                                       // [E] prompt, cast and swing bars
            hud[hudCount++] = new Rect(0, 772, 1440, 128);                                                       // key help, action bar, micro menu, XP bar
        }
        bool OverHud(Rect r) { for (int i = 0; i < hudCount; i++) if (hud[i].Overlaps(r)) return true; return false; }
        /// <summary>
        /// Finds room for a world label: nudged inside the screen's sides, lifted past the labels already placed this frame by at
        /// most <paramref name="maxLift"/>, never over a HUD panel or off the top. Records it and returns true if it may show.
        /// </summary>
        bool Place(ref Rect r, float maxLift)
        {
            r.x = Mathf.Clamp(r.x, 4, 1436 - r.width); float start = r.y;
            for (int pass = 0; ; pass++)
            {
                bool moved = false;
                for (int i = 0; i < taken.Count; i++) if (taken[i].Overlaps(r)) { r.y = taken[i].y - r.height - 1; moved = true; }
                if (!moved) break; if (pass == 7) return false;
            }
            if (start - r.y > maxLift || r.y < 2 || OverHud(r)) return false;
            taken.Add(r); return true;
        }
        /// <summary>A bubble a HUD panel blocks slides sideways off it, staying over its speaker's side, if there is room there.</summary>
        bool SlideOffHud(ref Rect r, float speakerX)
        {
            r.x = Mathf.Clamp(r.x, 4, 1436 - r.width);
            for (int i = 0; i < hudCount; i++)
            {
                if (!hud[i].Overlaps(r)) continue;
                var s = r; s.x = speakerX > hud[i].center.x ? hud[i].xMax + 4 : hud[i].x - s.width - 4;
                if (Mathf.Abs(s.center.x - speakerX) < s.width / 2 + 60 && Place(ref s, 120)) { r = s; return true; }
                return false;
            }
            return false;
        }
        /// <summary>
        /// True when solid scenery (walls, roofs, ground, rocks) hides <paramref name="point"/> from the camera. People and trees
        /// never hide a label: trees turn see-through, and people stand in front of each other. Anything within
        /// <paramref name="slack"/> in front of the point doesn't count (a place's own building).
        /// </summary>
        bool Occluded(Vector3 point, float slack = .3f)
        {
            var from = session.View.transform.position; var d = point - from; float length = d.magnitude - slack;
            if (length <= .1f) return false;
            int n = Physics.RaycastNonAlloc(from, d.normalized, sightHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = sightHits[i].collider;
                if (c.GetComponentInParent<Crulanda.Gameplay.Actor>() == null && c.GetComponentInParent<Crulanda.World.TreeFade>() == null) return true;
            }
            return false;
        }
        /// <summary>
        /// An enemy's plate height over its root: just above the top of its model, so wolves and boars get low plates and people
        /// keep theirs over the head (measured once from its renderers while it stands, elite scale included). Also its
        /// "level  name" label, rebuilt only when the level changes. A game animal's root rides 1 m over its feet, so its height is
        /// kept from the feet (always above zero) and the 1 m taken off again: a rabbit's plate sits just over its ears.
        /// </summary>
        (float height, string label) EnemyPlate(EncounterEnemy e)
        {
            var a = e.actor; bool known = enemyPlates.TryGetValue(e, out var c), dirty = !known || c.level != a.Level;
            float lift = e.Game ? 1 : 0;
            if (dirty) { c.level = a.Level; c.label = a.Level + "  " + a.DisplayName; }
            if (c.height <= 0 && !e.Hidden && a.IsAlive)
            {
                float top = float.MinValue;
                foreach (var r in e.GetComponentsInChildren<Renderer>()) if (r.enabled) top = Mathf.Max(top, r.bounds.max.y);
                c.height = top > float.MinValue ? Mathf.Clamp(top - e.transform.position.y + lift + .45f, .3f, 1.7f * e.transform.lossyScale.y + lift) : 1.7f; dirty = true;
            }
            if (dirty) { if (!known && enemyPlates.Count > 400) enemyPlates.Clear(); enemyPlates[e] = c; }
            return ((c.height > 0 ? c.height : 1.7f) - lift, c.label);
        }
        /// <summary>
        /// A plate's box: its name (and trade) rows above the anchor, with the ! or ? above those. <paramref name="p"/>.top is
        /// where the rows start, relative to the anchor.
        /// </summary>
        void AddPlate(Plate p, float width)
        {
            float top = p.at.y + p.top - (p.mark != ' ' ? 36 : 0), bottom = p.named ? p.at.y - 3 : p.at.y + p.top;
            p.box = new Rect(p.at.x - width / 2, top, width, bottom - top); p.shown = true; plates.Add(p);
        }
        /// <summary>Villagers, enemies and Mira that show a plate this frame: in range, on screen and in plain sight.</summary>
        void GatherPlates(Vector3 player)
        {
            var life = VillageLife.Active;
            if (life != null)
                foreach (var v in life.Villagers)
                {
                    if (!v.Visible) continue;
                    var root = v.transform.position; float vd = Vector3.Distance(player, root + Vector3.up * 1.3f);
                    if (vd > 28 || !ToCanvas(root + Vector3.up * 1.3f, out var vp) || !OnCanvasX(vp.x)) continue;
                    // Soft nameplates when close (name, and the trade in angle brackets beneath it), the quest marker, and speech.
                    bool named = vd < 18; char m = HeadMarker(v.Name, vd, out bool grey);
                    if (!named && m == ' ' && (v.Bubble == null || Time.time >= v.BubbleUntil)) continue;
                    if (Occluded(v, root + Vector3.up * .85f)) continue;
                    // On an errand with goods in hand, the trade line says where they are bound: <Hen-wife · eggs to the inn>.
                    string title = named && v.Title != null ? Bracketed(v.Carrying && v.Errand != null ? v.Title + " · " + v.Errand.id : v.Title) : null;
                    float w = Mathf.Max(Mathf.Max(named ? TextWidth(plateText, v.Name) : 0, title != null ? TextWidth(plateText, title) : 0), m != ' ' ? 28 : 0) + 8;
                    AddPlate(new Plate { dist = vd, fade = named ? Mathf.Clamp01((18 - vd) / 4) : 0, top = named ? (title != null ? -41 : -25) : -20, at = vp, v = v, named = named, name = v.Name, title = title, mark = m, grey = grey }, w);
                }
            // The other adventurers (Phase 5.2): name in their class's colour, the class and level beneath, like a player's plate.
            var pop = SimPopulation.Active;
            if (pop != null)
                foreach (var f in pop.Figures)
                {
                    if (f == null || f.Hidden) continue;   // inside the inn
                    var root = f.transform.position; float fd = Vector3.Distance(player, root + Vector3.up * 1.3f);
                    if (fd > 24 || !ToCanvas(root + Vector3.up * 1.3f, out var fp) || !OnCanvasX(fp.x) || Occluded(f, root + Vector3.up * .85f)) continue;
                    string title = Bracketed(SimRoster.ClassName(f.sim.classId) + " " + f.sim.level + (f.Activity == SimFigure.Doing.Loiter ? "" : " · " + f.Doings));
                    if (!string.IsNullOrEmpty(f.sim.guild)) title = Bracketed(f.sim.guild) + " " + SimRoster.ClassName(f.sim.classId) + " " + f.sim.level;   // round 27: the guild tag
                    float w = Mathf.Max(TextWidth(plateText, f.sim.name), TextWidth(plateText, title)) + 8;
                    AddPlate(new Plate { dist = fd, fade = Mathf.Clamp01((24 - fd) / 4), top = -41, at = fp, sim = f, named = true, name = f.sim.name, title = title, mark = ' ' }, w);
                }
            foreach (var c in session.PartySims)
            {
                if (c == null) continue;
                var root = c.transform.position; float cd = Vector3.Distance(player, root + Vector3.up * 1.3f);
                if (cd > 30 || !ToCanvas(root + Vector3.up * 1.3f, out var cp) || !OnCanvasX(cp.x) || Occluded(c, root + Vector3.up * .85f)) continue;
                string ctitle = string.IsNullOrEmpty(c.sim.guild) ? Bracketed(SimRoster.ClassName(c.sim.classId) + " " + c.sim.level + " · party") : Bracketed(c.sim.guild) + " " + SimRoster.ClassName(c.sim.classId) + " " + c.sim.level;
                AddPlate(new Plate { dist = cd, fade = 1, top = -41, at = cp, simParty = c, named = true, name = c.sim.name, title = ctitle, mark = ' ' }, Mathf.Max(TextWidth(plateText, c.sim.name), TextWidth(plateText, ctitle)) + 8);
            }
            var weaver = session.Weaver;   // Mother Quillet (D4): her name and what she is
            if (weaver != null && weaver.actor != null)
            {
                var root = weaver.transform.position; float wd = Vector3.Distance(player, root + Vector3.up * 1.1f);
                if (wd <= 30 && ToCanvas(root + Vector3.up * 1.1f, out var wp) && OnCanvasX(wp.x) && !Occluded(weaver, root + Vector3.up * .7f))
                {
                    string wtitle = Bracketed("Weaver of the Warrens");
                    AddPlate(new Plate { dist = wd, fade = 1, top = -41, at = wp, named = true, name = AditWeaver.Name, title = wtitle, mark = ' ' }, Mathf.Max(TextWidth(plateText, AditWeaver.Name), TextWidth(plateText, wtitle)) + 8);
                }
            }
            foreach (var e in session.Enemies) EnemyPlateAt(e, 25);
            // Game animals (deer, rabbits): a plate when close or targeted, so the fields are not a sea of names.
            foreach (var e in session.Game) EnemyPlateAt(e, session.Target == e ? 25 : 12);
            // The training yard's mending dummy: a friend's name over it, so it is found (Chris, 15:20: "i dont see a healing dummy").
            var mend = session.MendingDummy;
            if (mend != null)
            {
                var top = mend.transform.position + Vector3.up * 1.2f; float dd = Vector3.Distance(player, top);
                if (dd < 40 && ToCanvas(top, out var dp) && OnCanvasX(dp.x)) AddPlate(new Plate { dist = dd, fade = 1, top = -41, at = dp, named = true, name = "Mending dummy", title = Bracketed("Heal me"), mark = ' ' }, Mathf.Max(TextWidth(plateText, "Mending dummy"), 30) + 12);
            }
            // Mira: nameplate, plus a gold ! until she has joined you (after that, whatever the quests say).
            var mira = session.Companion; if (mira == null) return;
            var head = mira.transform.position + Vector3.up * 1.4f; float md = Vector3.Distance(player, head);
            if (md >= 45 || !ToCanvas(head, out var mp) || !OnCanvasX(mp.x) || Occluded(mira, mira.transform.position + Vector3.up * .9f)) return;
            bool mgrey = false; char mm = session.Progress.recruited ? HeadMarker("Mira", md, out mgrey) : '!';
            AddPlate(new Plate { dist = md, fade = 1, top = -29, at = mp, mira = true, named = true, name = "Mira", mark = mm, grey = mgrey }, Mathf.Max(TextWidth(centered, "Mira"), 28) + 8);
        }
        void EnemyPlateAt(EncounterEnemy e, float range)
        {
            if (e == null || e.Hidden) return;   // lying in wait: no nameplate
            float d = session.Distance(e); if (d > range) return;
            var (h, label) = EnemyPlate(e); var root = e.transform.position;
            if (!ToCanvas(root + Vector3.up * h, out var ep) || !OnCanvasX(ep.x) || Occluded(e, root + Vector3.up * (h - .45f))) return;
            float w = session.Target == e ? Mathf.Max(146, TextWidth(plateText, label) + 16) : Mathf.Max(130, TextWidth(plateText, label)) + 8;
            plates.Add(new Plate { dist = d, fade = 1, at = ep, e = e, name = label, shown = true, box = new Rect(ep.x - w / 2, ep.y - 25, w, 38) });
        }
        void DrawPlate(Plate p)
        {
            var a = p.at;
            if (p.e != null)
            {
                bool target = session.Target == p.e;
                // Your target: a dark backing with a gold edge behind its nameplate (the ring on the ground marks it too).
                if (target)
                {
                    var b = p.box; Fill(b, new Color(0, 0, 0, .5f));
                    Fill(new Rect(b.x, b.y, b.width, 1.5f), gold); Fill(new Rect(b.x, b.yMax - 1.5f, b.width, 1.5f), gold);
                    Fill(new Rect(b.x, b.y, 1.5f, b.height), gold); Fill(new Rect(b.xMax - 1.5f, b.y, 1.5f, b.height), gold);
                }
                // Level-coloured names (grey, green, yellow, orange, red by how the enemy compares to you).
                Outlined(new Rect(a.x - 150, a.y - 25, 300, 22), p.name, plateText, target ? gold : ConColor(p.e.actor.Level));
                if (p.e.RaidMarked != RaidMark.None && p.e.actor.IsAlive)   // the raid mark above the name (CC step C3): an icon (Chris, 2026-10-11: "you say Moon it really need to show an ICon"); gone with the mob ("once the mob that was cc dies. take off the icon")
                {
                    var icon = Mark(p.e.RaidMarked == RaidMark.Skull ? "skull" : p.e.RaidMarked == RaidMark.Moon ? "moon" : "cross");
                    if (icon != null) { Fill(new Rect(a.x - 18, a.y - 63, 36, 36), new Color(0, 0, 0, .55f)); GUI.DrawTexture(new Rect(a.x - 16, a.y - 61, 32, 32), icon); }
                    else Outlined(new Rect(a.x - 150, a.y - 45, 300, 22), p.e.RaidMarked == RaidMark.Skull ? "[ SKULL ]" : p.e.RaidMarked == RaidMark.Moon ? "( MOON )" : "< CROSS >", plateText,
                        p.e.RaidMarked == RaidMark.Skull ? Color.white : p.e.RaidMarked == RaidMark.Moon ? new Color(.6f, .8f, 1) : new Color(1, .4f, .35f));
                }
                Fill(new Rect(a.x - 66, a.y - 1, 132, 8), new Color(0, 0, 0, .75f));
                Bar(new Rect(a.x - 65, a.y, 130, 6), p.e.actor.Health.Pool.Ratio, new Color(.8f, .3f, .25f), "");
                var held = p.e.ControlLabel;   // crowd control on any mob, not just your target (CC step C4)
                if (held != null)
                {
                    // Who is held, seen at a glance (Chris, 2026-10-11: "show who is CCd"): the hold's icon beside its name and the plate tinted.
                    var hc = ControlRings.ColourFor(p.e); var hi = Mark(HoldIcon(held));
                    Fill(new Rect(a.x - 70, a.y - 27, 140, 26), new Color(hc.r, hc.g, hc.b, .3f));
                    if (hi != null) { Fill(new Rect(a.x - 100, a.y - 30, 30, 30), new Color(0, 0, 0, .6f)); GUI.DrawTexture(new Rect(a.x - 98, a.y - 28, 26, 26), hi); }
                    Outlined(new Rect(a.x - 150, a.y + 6, 300, 20), held, plateText, hc);
                }
                else if (p.e.Casting && p.e.Cast != null)
                {
                    Fill(new Rect(a.x - 66, a.y + 9, 132, 6), new Color(0, 0, 0, .75f));
                    Fill(new Rect(a.x - 65, a.y + 10, 130 * p.e.CastProgress, 4), new Color(.85f, .45f, 1));
                    Outlined(new Rect(a.x - 150, a.y + 14, 300, 20), p.e.Cast.name, plateText, new Color(.9f, .7f, 1));
                }
                return;
            }
            // Rows are 22 px (24 for Mira's larger font): the label style clips at its padded rect, so tighter rows cut off g, p and y.
            float rows = a.y + p.top;
            if (p.named)
            {
                var style = p.mira ? centered : plateText;
                var simColour = p.sim != null ? ClassColour(p.sim.sim.classId) : p.simParty != null ? ClassColour(p.simParty.sim.classId) : Color.white;
                Outlined(new Rect(a.x - 150, rows, 300, p.mira ? 24 : 22), p.name, style, p.mira ? new Color(.55f, 1, .7f) :
                    p.sim != null || p.simParty != null ? new Color(simColour.r, simColour.g, simColour.b, p.fade) :
                    session.FocusVillager == p.v ? new Color(.55f, 1, .55f, p.fade) : new Color(.85f, .9f, 1, p.fade));
                if (p.title != null) Outlined(new Rect(a.x - 150, rows + 15, 300, 22), p.title, style, new Color(1, .84f, .45f, p.fade));
            }
            if (p.mark != ' ') DrawHeadMarker(p.mark, p.grey, new Vector2(a.x, rows - 20));
        }
        /// <summary>A speech bubble above its speaker's plate and marker (never over them), off the HUD, fading as it ends.</summary>
        void DrawBubble(Plate p)
        {
            var v = p.v; if (v.Bubble == null || Time.time >= v.BubbleUntil) return;
            measure.text = v.Bubble; float bw = Mathf.Min(260, small.CalcSize(measure).x + 20), bh = small.CalcHeight(measure, bw - 16) + 10;
            var natural = new Rect(p.at.x - bw / 2, (p.box.height > 0 ? p.box.y : p.at.y + p.top) - 7 - bh, bw, bh); var br = natural;
            if (!Place(ref br, 120)) { br = natural; if (!SlideOffHud(ref br, p.at.x)) return; }
            float fade = Mathf.Clamp01((v.BubbleUntil - Time.time) * 2); var paper = new Color(.96f, .93f, .84f, .92f * fade);
            Fill(br, paper);
            float tail = Mathf.Clamp(p.at.x, br.x + 10, br.xMax - 10);   // a small tail pointing down at the speaker
            Fill(new Rect(tail - 5, br.yMax, 10, 3), paper); Fill(new Rect(tail - 2, br.yMax + 3, 4, 3), paper);
            GUI.contentColor = new Color(.12f, .1f, .08f, fade); GUI.Label(new Rect(br.x + 8, br.y + 4, bw - 16, bh), v.Bubble, small); GUI.contentColor = Color.white;
        }
        /// <summary>
        /// Landmark names over their places when you are near enough to see them, and the roads out ("Road to Khaven Village
        /// (3-5)") from further off, so an exit is never a mystery. They give way to people's plates.
        /// </summary>
        void DrawPlaceNames(Vector3 player)
        {
            if (session.Zone == null) return;
            foreach (var l in session.Zone.Zone.landmarks) PlaceName(player, session.Zone.GroundFixed(l.at, 7.2f), l.name, 38, gold);
            foreach (var e in session.Zone.Zone.exits)
            {
                var to = session.Zone.FindZone(e.to); if (to == null) continue;
                string rk = e.to + "|" + e.name; if (!roads.TryGetValue(rk, out var road)) roads[rk] = road = "Road to " + to.displayName + "  " + HudMaps.Band(to);
                PlaceName(player, session.Zone.GroundFixed(e.at, 6.5f), road, 60, HudMaps.BandColor(to, session.Progress.Level));
            }
        }
        readonly System.Collections.Generic.Dictionary<string, string> roads = new System.Collections.Generic.Dictionary<string, string>();
        void PlaceName(Vector3 player, Vector3 at, string name, float reach, Color colour)
        {
            float d = Vector3.Distance(player, at);
            if (d > reach || !ToCanvas(at, out var p) || !OnCanvasX(p.x) || Occluded(name, at, 8)) return;
            float w = TextWidth(centered, name) + 10; var r = new Rect(p.x - w / 2, Mathf.Max(4, p.y - 14), w, 28);
            if (!Place(ref r, 40)) return;
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01((reach - d) / 10));
            GUI.contentColor = new Color(.1f, .08f, .06f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), name, centered);
            GUI.contentColor = colour; GUI.Label(r, name, centered);
            GUI.color = Color.white; GUI.contentColor = Color.white;
        }
        void DrawWorldLabels()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (centered == null)
            {
                centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                plateText = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            }
            var g = mGather.Auto();
            CollectHudRects(); taken.Clear(); plates.Clear();
            var player = session.Player.transform.position;
            GatherPlates(player);
            g.Dispose(); var lay = mLayout.Auto();
            // Nearest first: each keeps its natural place if it can, and farther ones stack above it.
            for (int i = 1; i < plates.Count; i++) { var p = plates[i]; int j = i - 1; while (j >= 0 && plates[j].dist > p.dist) { plates[j + 1] = plates[j]; j--; } plates[j + 1] = p; }
            for (int i = 0; i < plates.Count; i++)
            {
                var p = plates[i]; if (p.box.height <= 0) continue;   // only speaking: the bubble finds its own room
                var r = p.box; p.shown = Place(ref r, 200); p.at += r.position - p.box.position; p.box = r; plates[i] = p;
            }
            lay.Dispose(); var dr = mDraw.Auto();
            for (int i = plates.Count - 1; i >= 0; i--) if (plates[i].shown) DrawPlate(plates[i]);   // nearer plates on top
            for (int i = 0; i < plates.Count; i++) if (plates[i].shown && plates[i].v != null) DrawBubble(plates[i]);
            dr.Dispose();
            using (mPlaces.Auto()) DrawPlaceNames(player);
            foreach (var f in session.Floating)
            {
                var p = session.View.WorldToScreenPoint(f.position + Vector3.up * (1.3f - (f.expires - Time.time)));
                if (p.z <= 0) continue;
                GUI.contentColor = f.color;
                { var cp = ScreenToCanvas(p.x, p.y); GUI.Label(new Rect(cp.x - 45, cp.y, 90, 38), f.text, number); }
            }
            GUI.contentColor = Color.white;
        }
        /// <summary>Classic "con" colours from LevelCon: trivial grey, easy green, even yellow, tough orange, dangerous red, deadly purple-red.</summary>
        Color ConColor(int level)
        {
            switch (LevelCon.Evaluate(session.Progress.Level, level))
            {
                case Crulanda.Core.ConDifficulty.Trivial: return new Color(.6f, .6f, .6f);
                case Crulanda.Core.ConDifficulty.Easy: return new Color(.35f, .85f, .35f);
                case Crulanda.Core.ConDifficulty.Even: return new Color(1, .88f, .3f);
                case Crulanda.Core.ConDifficulty.Tough: return new Color(1, .55f, .2f);
                case Crulanda.Core.ConDifficulty.Dangerous: return new Color(1, .25f, .2f);
                default: return new Color(.85f, .2f, .55f);
            }
        }
        void Frame(Rect r) { GUI.color = ink; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        /// <summary>The party's damage and healing meter (/meter): a row a member, damage and healing done, and each a second over the
        /// party's fighting time since the reset. Under the quest tracker on the right.</summary>
        /// <summary>Where the meter sits on the 1440x900 canvas for a party of <paramref name="rows"/>.</summary>
        public static Rect MeterRect(int rows) { return new Rect(1100, 456, 340, 60 + 22 * Mathf.Max(1, rows)); }
        void DrawMeter()
        {
            var m = session.Meter; var rows = new List<CombatMeter.Row>(m.Rows); float t = m.CombatSeconds;
            var r = MeterRect(rows.Count);
            Fill(r, new Color(0, 0, 0, .55f));
            GUI.contentColor = gold; GUI.Label(new Rect(r.x + 10, r.y + 6, 200, 20), "DAMAGE AND HEALING", small); GUI.contentColor = Color.white;
            GUI.Label(new Rect(r.x + 10, r.y + 22, 420, 18), (session.Meter.RunName != null ? session.Meter.RunName + "  ·  " : "") + "over " + Mathf.RoundToInt(t) + " s of fighting  ·  /meter reset", tiny);   // the run's name when one is set (the balance harness names each fight)
            GUI.contentColor = new Color(1, .75f, .5f); GUI.Label(new Rect(r.x + 178, r.y + 40, 80, 18), "damage", tiny);
            GUI.contentColor = new Color(.5f, 1, .7f); GUI.Label(new Rect(r.x + 256, r.y + 40, 80, 18), "healing", tiny); GUI.contentColor = Color.white;
            float y = r.y + 58;
            foreach (var row in rows)
            {
                GUI.Label(new Rect(r.x + 10, y, 170, 20), row.name, small);
                GUI.contentColor = new Color(1, .75f, .5f); GUI.Label(new Rect(r.x + 178, y, 80, 20), row.damage.ToString("N0") + " " + row.Dps(t).ToString("0") + "/s", small);
                GUI.contentColor = new Color(.5f, 1, .7f); GUI.Label(new Rect(r.x + 256, y, 74, 20), row.healing.ToString("N0") + " " + row.Hps(t).ToString("0") + "/s", small);
                GUI.contentColor = Color.white; y += 22;
            }
            if (rows.Count == 0) GUI.Label(new Rect(r.x + 10, y, 300, 20), "Nothing yet.", small);
        }
        /// <summary>The run's scorecard (Chris, 2026-10-11): every member with its class, damage and healing a second, deaths; the MVP and the
        /// healer of the run; what dropped, off whom, to whom.</summary>
        void DrawScorecard()
        {
            var m = session.Meter; var rows = new List<CombatMeter.Row>(m.Rows); float t = m.CombatSeconds;
            int lootLines = Mathf.Min(10, m.Drops.Count);
            var r = new Rect(290, 110, 860, 190 + 26 * Mathf.Max(1, rows.Count) + 22 * Mathf.Max(1, lootLines));
            Frame(r);
            GUI.Label(new Rect(r.x + 30, r.y + 18, 800, 36), (session.ScorecardTitle ?? "THE RUN").ToUpperInvariant(), heading);
            GUI.Label(new Rect(r.x + 30, r.y + 56, 800, 22), Mathf.FloorToInt(m.RunSeconds / 60) + " min " + (Mathf.FloorToInt(m.RunSeconds) % 60).ToString("00") + " s  ·  " + Mathf.RoundToInt(t) + " s of it fighting", small);
            float y = r.y + 88;
            GUI.contentColor = gold;
            GUI.Label(new Rect(r.x + 30, y, 260, 22), "MEMBER", small); GUI.Label(new Rect(r.x + 300, y, 160, 22), "DAMAGE", small); GUI.Label(new Rect(r.x + 470, y, 160, 22), "HEALING", small); GUI.Label(new Rect(r.x + 640, y, 90, 22), "DEATHS", small);
            GUI.contentColor = Color.white; y += 26;
            var mvp = m.Mvp(); var healer = m.TopHealer();
            foreach (var row in rows)
            {
                GUI.Label(new Rect(r.x + 30, y, 260, 22), row.name + (row == mvp ? "  ★" : ""), small);
                GUI.contentColor = new Color(1, .75f, .5f); GUI.Label(new Rect(r.x + 300, y, 160, 22), row.damage.ToString("N0") + "  " + row.Dps(t).ToString("0") + "/s", small);
                GUI.contentColor = new Color(.5f, 1, .7f); GUI.Label(new Rect(r.x + 470, y, 160, 22), row.healing.ToString("N0") + "  " + row.Hps(t).ToString("0") + "/s", small);
                GUI.contentColor = row.deaths > 0 ? new Color(1, .5f, .45f) : Color.white; GUI.Label(new Rect(r.x + 640, y, 90, 22), row.deaths.ToString(), small);
                GUI.contentColor = Color.white; y += 26;
            }
            if (rows.Count == 0) { GUI.Label(new Rect(r.x + 30, y, 400, 22), "Nothing fought yet.", small); y += 26; }
            y += 6; GUI.contentColor = gold;
            GUI.Label(new Rect(r.x + 30, y, 800, 22), (mvp != null ? "MVP: " + mvp.name : "") + (healer != null ? "     Healer of the run: " + healer.name : ""), small);
            GUI.contentColor = Color.white; y += 30;
            GUI.Label(new Rect(r.x + 30, y, 400, 22), "LOOT" + (m.Coin > 0 ? "  ·  " + m.Coin + " coin to you" : ""), small); y += 24;
            if (m.Drops.Count == 0) GUI.Label(new Rect(r.x + 30, y, 800, 22), "Nothing dropped.", tiny);
            for (int i = Mathf.Max(0, m.Drops.Count - lootLines); i < m.Drops.Count; i++) { var d = m.Drops[i]; GUI.Label(new Rect(r.x + 30, y, 800, 22), d.item + "  off " + d.from + "  to " + d.to, tiny); y += 22; }
            if (GUI.Button(new Rect(r.xMax - 150, r.yMax - 46, 120, 32), "Close [Esc]", button)) session.ScorecardOpen = false;
        }
        static Texture2D Mark(string id) { return IconDb.Load(IconDb.PathFor("mark", id)); }
        /// <summary>Which hold icon a mob's control label calls for (Icons/mark): held, stunned, feared, silenced; null for none.</summary>
        static string HoldIcon(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            if (label.StartsWith("Held")) return "held"; if (label.StartsWith("Stunned")) return "stunned"; if (label.StartsWith("Fleeing")) return "feared"; if (label.StartsWith("Silenced")) return "silenced";
            return null;
        }
        void Bar(Rect r, float fill, Color color, string label)
        {
            GUI.color = new Color(.12f,.15f,.17f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(r.x,r.y,r.width * Mathf.Clamp01(fill),r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label.Length > 0) GUI.Label(new Rect(r.x+5,r.y-1,r.width,r.height+8), label, small);
        }
    }
}




