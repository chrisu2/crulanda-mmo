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
        GUIStyle heading, text, small, tiny, button, number, compact, tile, denseAction, abbrev, keybind, frameName, barText, micro;
        readonly Color ink = new Color(.045f, .065f, .075f, .94f);
        readonly Color gold = new Color(.91f, .76f, .43f);
        static readonly Color HealthGreen = new Color(.12f, .72f, .16f);
        Texture2D disc;
        readonly HudMaps maps = new HudMaps();
        public static bool BlocksPointer(Vector2 point)
        {
            var p = new Vector2(point.x * 1440 / Screen.width, (Screen.height - point.y) * 900 / Screen.height);
            return paused || buildVisible || mapVisible || QuestUiBlocks(p) || ItemUiBlocks(p) || p.y > 795 || new Rect(10, 10, 350, 190).Contains(p) ||
                (targetVisible && new Rect(365, 10, 350, 130).Contains(p)) || new Rect(1215, 0, 225, 240).Contains(p) ||
                new Rect(1110, 236, 330, 200).Contains(p) || false;
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
            tiny = new GUIStyle(small) { fontSize = 13 };
            compact = new GUIStyle(button) { fontSize = 13, padding = new RectOffset(4, 4, 3, 3) };
            denseAction = new GUIStyle(button) { fontSize = 11, padding = new RectOffset(2, 2, 2, 2), wordWrap = true };
            tile = new GUIStyle(GUI.skin.button) { fontSize = 12, wordWrap = true, alignment = TextAnchor.UpperCenter, padding = new RectOffset(4, 4, 6, 4) };
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

        void OnGUI()
        {
            if (Hidden || session == null || session.Player == null) return;
            Styles();
            inventoryVisible = session.InventoryOpen; paused = session.Paused; buildVisible = session.BuildOpen;
            mapVisible = session.MapOpen; targetVisible = session.Target != null || session.HasFriendlyFocus; bookVisible = session.QuestBookOpen; talkVisible = session.Conversation != null;
            bagsVisible = session.InventoryOpen; charVisible = session.CharacterOpen; vendorVisible = session.VendorNpc != null;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1440f, Screen.height / 900f, 1));
            GUI.color = Color.white;
            DrawWorldLabels();
            DrawPlayerFrame(); DrawPartyFrame(); if (session.Target != null) DrawTargetFrame(); else if (session.HasFriendlyFocus) DrawFriendFrame();
            maps.DrawMinimap(session, gold);
            DrawQuestTracker(); DrawChat(); DrawCenter(); DrawActionBar(); DrawMicroMenu(); DrawXpBar();
            if (session.CharacterOpen) DrawCharacter();
            if (session.VendorNpc != null) DrawVendor();
            if (session.InventoryOpen) DrawBags();
            if (session.BuildOpen) DrawBuild();
            if (session.MapOpen) maps.DrawWindow(session, gold, ink);
            if (session.QuestBookOpen) DrawQuestBook();
            if (session.Conversation != null) DrawConversation();
            if (!session.Player.IsAlive)
            {
                Frame(new Rect(490, 355, 460, 145));
                GUI.Label(new Rect(530, 375, 400, 40), "YOU HAVE FALLEN", heading);
                if (GUI.Button(new Rect(535, 433, 370, 44), "Recover [R]", button)) session.Recover();
            }
            if (session.InventoryOpen || session.CharacterOpen || session.VendorNpc != null) DrawDragAndConfirm();
            if (session.Paused) DrawPause();
        }

        // ---------- helpers ----------
        void Shadow(Rect r, string s, GUIStyle style, Color c)
        {
            GUI.contentColor = new Color(0, 0, 0, .9f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), s, style);
            GUI.contentColor = c; GUI.Label(r, s, style); GUI.contentColor = Color.white;
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
            if (!string.IsNullOrEmpty(label)) Shadow(r, label, barText, Color.white);
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
            switch (kind) { case Crulanda.Core.ResourceKind.Mana: return new Color(.18f, .38f, .9f); case Crulanda.Core.ResourceKind.Breath: return new Color(.2f, .66f, .62f); default: return new Color(.86f, .62f, .16f); }
        }

        // ---------- frames ----------
        void DrawPlayerFrame()
        {
            var p = session.Player; var cls = session.ClassDef;
            Fill(new Rect(58, 22, 290, 64), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(52, 54), 74, cls.id == "class.druid" ? new Color(.95f, .5f, .15f) : new Color(.78f, .61f, .43f), cls.displayName.Substring(0, 1), session.Progress.Level.ToString());
            Shadow(new Rect(96, 24, 240, 20), "You · " + cls.displayName, frameName, Color.white);
            UnitBar(new Rect(96, 45, 244, 18), p.Health.Pool.Ratio, HealthGreen, p.Health.Pool.Current + " / " + p.Health.Pool.Max);
            UnitBar(new Rect(96, 66, 244, 13), p.Resource.Pool.Ratio, ResourceColor(cls.resource), p.Resource.Pool.Current + " " + ResourceShort(cls.resource));
            var status = session.Kit.StatusLine;
            if (!string.IsNullOrEmpty(status)) Shadow(new Rect(96, 84, 300, 20), status, tiny, new Color(1, .92f, .75f));
        }
        void DrawPartyFrame()
        {
            var m = session.Companion; if (m == null) return;
            var a = m.actor;
            Fill(new Rect(48, 118, 214, 44), new Color(0, 0, 0, .5f));
            Portrait(new Vector2(40, 140), 46, new Color(.45f, .8f, .55f), "M", null);
            Shadow(new Rect(70, 116, 190, 18), "Mira" + (session.Progress.recruited ? "" : "  (not in party)"), tiny, session.Progress.recruited ? Color.white : new Color(.8f, .8f, .8f));
            UnitBar(new Rect(70, 135, 184, 12), a.Health.Pool.Ratio, HealthGreen, "");
            UnitBar(new Rect(70, 149, 184, 8), a.Resource.Pool.Ratio, ResourceColor(Crulanda.Core.ResourceKind.Mana), "");
            Shadow(new Rect(20, 166, 320, 20), m.Activity, tiny, new Color(.85f, .9f, .85f));
            if (m.CastProgress > 0) UnitBar(new Rect(70, 186, 184, 7), m.CastProgress, new Color(.3f, .85f, .65f), "");
        }
        void DrawTargetFrame()
        {
            var t = session.Target; var a = t.actor;
            Fill(new Rect(372, 22, 290, 64), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(668, 54), 74, a.IsAlive ? new Color(.85f, .2f, .15f) : new Color(.4f, .4f, .4f), a.DisplayName.Substring(0, 1), a.IsAlive ? a.Level.ToString() : null);
            Shadow(new Rect(380, 24, 240, 20), a.DisplayName, frameName, a.IsAlive ? ConColor(a.Level) : new Color(.7f, .7f, .7f));
            UnitBar(new Rect(378, 45, 244, 18), a.Health.Pool.Ratio, HealthGreen, a.IsAlive ? a.Health.Pool.Current + " / " + a.Health.Pool.Max : "Dead · press E at the body");
            if (a.IsAlive)
            {
                float next = t.NextSwingIn, interval = session.content.enemySwingInterval;
                UnitBar(new Rect(378, 67, 244, 7), 1 - next / Mathf.Max(.01f, interval), new Color(.85f, .45f, .3f), "");
                var extra = session.Kit.TargetStatus(t);
                Shadow(new Rect(378, 78, 330, 20), (next > 0 ? "Swing in " + next.ToString("0.0") + "s" : "Swing ready") + (string.IsNullOrEmpty(extra) ? "" : "  ·  " + extra), tiny, Color.white);
                if (t.Victim != null) Shadow(new Rect(378, 96, 330, 20), "Target of target: " + t.Victim.DisplayName, tiny, new Color(.85f, .85f, .85f));
            }
        }

        // ---------- right side ----------
        void DrawQuestTracker()
        {
            if (session.Quests != null && session.Zone != null) { DrawQuests(); return; }
            var r = new Rect(1120, 240, 310, 200);
            Shadow(new Rect(r.x, r.y, r.width, 20), session.ZoneTitle, frameName, gold);
            int total = session.Enemies.Count, dead = session.Enemies.FindAll(e => !e.actor.IsAlive).Count;
            string miraDistance = session.Progress.recruited || session.Companion == null ? "" :
                "  (" + Mathf.RoundToInt(Vector3.Distance(session.Player.transform.position, session.Companion.transform.position)) + "m)";
            var lines = new[] {
                (session.Progress.recruited, session.Objective(0, "Recruit the healer [E]") + miraDistance),
                (dead == total, session.Objective(1, "Secure the trail") + "  " + dead + "/" + total),
                (session.Progress.equippedItem == session.content.itemId, session.Objective(2, "Equip a recovered blade [I]"))
            };
            float y = r.y + 24;
            foreach (var (done, line) in lines)
            { Shadow(new Rect(r.x, y, r.width, 36), (done ? "✓  " : "–  ") + line, tiny, done ? new Color(.6f, .6f, .6f) : new Color(1, .95f, .85f)); y += 22; }
            if (session.Zone != null) Shadow(new Rect(r.x, y + 4, r.width, 36), session.Zone.Zone.subtitle, tiny, new Color(.75f, .75f, .72f));
            if (total > 0 && dead == total && GUI.Button(new Rect(r.x, y + 44, 250, 28), "Patrol returns · keep gear and XP", micro)) session.RepeatTrail();
        }

        // ---------- bottom ----------
        void DrawChat()
        {
            Fill(new Rect(10, 620, 470, 150), new Color(0, 0, 0, .32f));
            float y = 770 - 6;
            for (int i = session.Messages.Count - 1; i >= 0 && y > 626; i--)
            {
                var h = tiny.CalcHeight(new GUIContent(session.Messages[i]), 455); y -= h;
                Shadow(new Rect(18, y, 455, h), session.Messages[i], tiny, new Color(1, .96f, .86f));
            }
            Shadow(new Rect(12, 774, 700, 20), "WASD move · Space jump · Right-drag look · Wheel zoom · Tab target · E interact · L quests · M map · B talents · I bags · C character", tiny, new Color(.8f, .8f, .78f));
        }
        void DrawCenter()
        {
            var prompt = session.InteractPrompt;
            if (prompt != null)
            {
                Fill(new Rect(520, 692, 400, 34), new Color(0, 0, 0, .55f));
                Shadow(new Rect(530, 697, 390, 26), "[E]  " + prompt, text, gold);
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
        void DrawActionBar()
        {
            int n = session.ActionCount; const float size = 50, gap = 6;
            float total = n * (size + gap) - gap, x0 = 720 - total / 2, y = 808;
            Fill(new Rect(x0 - 12, y - 10, total + 24, 82), new Color(.07f, .06f, .05f, .9f));
            Fill(new Rect(x0 - 12, y - 10, total + 24, 2), new Color(.62f, .52f, .33f));
            string hover = null; var mouse = Event.current.mousePosition;
            for (int i = 0; i < n; i++)
            {
                var a = session.ActionAt(i); var r = new Rect(x0 + i * (size + gap), y, size, size);
                float remaining = session.CooldownRemaining(i); string locked = session.ActionLockLabel(i);
                bool usable = session.Player.IsAlive && !session.Paused && locked == null;
                Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), locked == null ? new Color(.62f, .52f, .33f) : new Color(.25f, .25f, .25f));
                Fill(r, locked == null ? new Color(.16f, .2f, .24f) : new Color(.1f, .1f, .1f));
                Fill(new Rect(r.x, r.y, r.width, r.height / 2), new Color(1, 1, 1, .05f));
                bool cooling = remaining > 0 && a.cooldown > 0;
                if (!cooling) Shadow(r, Abbreviation(a.name), abbrev, locked == null ? Color.white : new Color(.45f, .45f, .45f));
                if (cooling)
                {
                    float frac = Mathf.Clamp01(remaining / Mathf.Max(a.cooldown, .01f));
                    Fill(new Rect(r.x, r.y, r.width, r.height * frac), new Color(0, 0, 0, .62f));
                    Shadow(r, remaining < 10 ? remaining.ToString("0.0") : Mathf.CeilToInt(remaining).ToString(), abbrev, new Color(1, .95f, .6f));
                }
                else if (remaining > 0) Fill(r, new Color(0, 0, 0, .35f));
                Shadow(r, EncounterInput.SlotLabel(i), keybind, new Color(.9f, .9f, .9f));
                string sub = locked ?? (a.cost > 0 ? a.cost.ToString() : "");
                if (sub.Length > 0) Shadow(new Rect(r.x, r.yMax - 16, r.width - 3, 16), sub, new GUIStyle(keybind) { alignment = TextAnchor.LowerRight, fontSize = 10 }, locked == null ? new Color(.7f, .85f, 1) : new Color(.8f, .6f, .5f));
                Shadow(new Rect(r.x - 6, r.yMax + 2, r.width + 12, 16), a.name.Length > 10 ? a.name.Substring(0, 9) + "…" : a.name, new GUIStyle(barText) { fontSize = 10, wordWrap = false, clipping = TextClipping.Clip }, new Color(.85f, .82f, .75f));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && usable) session.UseAbility(i);
                if (r.Contains(mouse))
                    hover = a.name + (locked != null ? "  (" + locked + ")" : "") + "\n" + a.description +
                        (a.cost > 0 ? "\nCost: " + a.cost + " " + ResourceName(session.ClassDef.resource) : "") + (a.cooldown > 0 ? "   Cooldown: " + a.cooldown + "s" : "");
            }
            if (hover != null)
            {
                var h = small.CalcHeight(new GUIContent(hover), 380) + 16; var tr = new Rect(x0 + total - 400, y - 20 - h, 400, h);
                Fill(tr, new Color(.04f, .05f, .07f, .95f)); Fill(new Rect(tr.x, tr.y, tr.width, 2), gold);
                GUI.Label(new Rect(tr.x + 10, tr.y + 8, 380, h), hover, small);
            }
        }
        void DrawMicroMenu()
        {
            float x = 1110, y = 842; int points = session.Talents.Available(session.Progress);
            if (GUI.Button(new Rect(x - 80, y, 76, 30), "Character", micro)) session.CharacterOpen = !session.CharacterOpen;
            if (GUI.Button(new Rect(x, y, 70, 30), points > 0 ? "Talents " + points : "Talents", micro)) session.BuildOpen = !session.BuildOpen;
            if (GUI.Button(new Rect(x + 74, y, 56, 30), "Bags", micro)) session.InventoryOpen = !session.InventoryOpen;
            if (GUI.Button(new Rect(x + 134, y, 56, 30), "Map", micro)) session.MapOpen = !session.MapOpen;
            if (GUI.Button(new Rect(x + 194, y, 56, 30), "Save", micro)) session.Save();
            if (GUI.Button(new Rect(x + 254, y, 56, 30), "Load", micro)) session.Load();
            if (points > 0) Disc(new Rect(x + 60, y - 6, 14, 14), new Color(1, .82f, .2f));
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
        void DrawPause()
        {
            Frame(new Rect(475, 325, 490, 250));
            GUI.Label(new Rect(525, 351, 400, 35), "EXPEDITION PAUSED", heading);
            GUI.Label(new Rect(525, 398, 400, 35), "Progress autosaves out of combat.", text);
            if (GUI.Button(new Rect(525, 448, 390, 44), "Resume [Esc]", button)) session.Resume();
            // Each class is a separate character with its own save; switching saves this one first.
            string other = session.ClassDef.id == "class.druid" ? "class.warrior" : "class.druid";
            var otherClass = session.content.FindClass(other);
            GUI.enabled = otherClass != null && !session.InCombat;
            if (otherClass != null && GUI.Button(new Rect(525, 504, 390, 44), "Play your " + otherClass.definition.displayName + " (separate character)", button))
                session.SwitchCharacter(other);
            GUI.enabled = true;
        }
        public static string ResourceName(Crulanda.Core.ResourceKind kind) { return kind == Crulanda.Core.ResourceKind.Breath ? "Shift Breath" : kind.ToString(); }
        static string ResourceShort(Crulanda.Core.ResourceKind kind) { return kind == Crulanda.Core.ResourceKind.Breath ? "Breath" : kind.ToString(); }
        public static string Pips(int value, int max) { return new string('●', Mathf.Clamp(value, 0, max)) + new string('○', Mathf.Max(0, max - value)); }
        // Panel geometry follows the branch count: 3 trees fit the original panel; 4 (Druid) use the full width.
        float PanelX, PanelW, TreeLeft, TreeWidth, CellWidth, TileWidth;
        const float TileHeight = 96, TierTop = 272, TierHeight = 128;
        void LayoutTrees(int branches)
        {
            PanelX = branches > 3 ? 20 : 155; PanelW = branches > 3 ? 1400 : 1130; TreeLeft = PanelX + 25;
            TreeWidth = (PanelW - 50 - 12 * (branches - 1)) / branches; CellWidth = TreeWidth / 4f; TileWidth = CellWidth - 6;
        }
        Rect TileRect(int branch, TalentNodeData n)
        { return new Rect(TreeLeft + branch * (TreeWidth + 12) + n.col * CellWidth + 2, TierTop + n.tier * TierHeight, TileWidth, TileHeight); }
        void DrawBuild()
        {
            var tree = session.Talents; var p = session.Progress; LayoutTrees(tree.Branches.Count);
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
                    if (GUI.Button(r, n.name + "\n" + (kind.Length > 0 ? kind + "\n" : "") + rank + "/" + n.max, tile) && canAdd) session.ChangeTalent(n.id, 1);
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
        GUIStyle centered, marker;
        bool ToCanvas(Vector3 world, out Vector2 p)
        {
            var s = session.View.WorldToScreenPoint(world); p = new Vector2(s.x * 1440 / Screen.width, (Screen.height - s.y) * 900 / Screen.height);
            return s.z > 0 && p.x > -100 && p.x < 1540 && p.y > 0 && p.y < 900;
        }
        void DrawPlaceAndPeopleLabels()
        {
            if (centered == null)
            {
                centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                marker = new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter, fontSize = 34 };
            }
            var player = session.Player.transform.position;
            // Landmark names float over their places when you are near enough to see them.
            if (session.Zone != null)
                foreach (var l in session.Zone.Zone.landmarks)
                {
                    var at = session.Zone.Ground(l.at, 7.2f);
                    float d = Vector3.Distance(player, at);
                    if (d > 38 || !ToCanvas(at, out var p)) continue;
                    GUI.color = new Color(1, 1, 1, Mathf.Clamp01((38 - d) / 10));
                    GUI.contentColor = new Color(.1f, .08f, .06f); GUI.Label(new Rect(p.x - 159, p.y - 13, 320, 28), l.name, centered);
                    GUI.contentColor = gold; GUI.Label(new Rect(p.x - 160, p.y - 14, 320, 28), l.name, centered);
                    GUI.color = Color.white;
                }
            // Villagers: soft nameplates when close, and speech bubbles when they talk.
            var life = VillageLife.Active;
            if (life != null)
                foreach (var v in life.Villagers)
                {
                    if (!v.Visible) continue;
                    var top = v.transform.position + Vector3.up * 1.3f; float vd = Vector3.Distance(player, top);
                    if (vd > 28 || !ToCanvas(top, out var vp)) continue;
                    if (vd < 18)
                    {
                        // WoW-style: name, and the trade in angle brackets beneath it.
                        float a = Mathf.Clamp01((18 - vd) / 4); var style = small != null ? new GUIStyle(small) { alignment = TextAnchor.MiddleCenter } : centered;
                        bool titled = v.Title != null;
                        void Plate(Rect r, string text, Color c)
                        {
                            GUI.contentColor = new Color(0, 0, 0, a * .85f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
                            GUI.contentColor = c; GUI.Label(r, text, style);
                        }
                        Plate(new Rect(vp.x - 100, vp.y - (titled ? 42 : 26), 200, 24), v.Name, session.FocusVillager == v ? new Color(.55f, 1, .55f, a) : new Color(.85f, .9f, 1, a));
                        if (titled) Plate(new Rect(vp.x - 100, vp.y - 26, 200, 22), "<" + v.Title + ">", new Color(1, .84f, .45f, a));
                    }
                    DrawHeadMarker(v.Name, vp, vd, v.Title != null && vd < 18 ? 44 : 28);
                    if (v.Bubble != null && Time.time < v.BubbleUntil)
                    {
                        var content = new GUIContent(v.Bubble); float bw = Mathf.Min(260, small.CalcSize(content).x + 20), bh = small.CalcHeight(content, bw - 16) + 10;
                        var br = new Rect(vp.x - bw / 2, vp.y - (v.Title != null && vd < 18 ? 50 : 34) - bh, bw, bh);
                        GUI.color = new Color(.96f, .93f, .84f, .92f); GUI.DrawTexture(br, Texture2D.whiteTexture); GUI.color = Color.white;
                        GUI.contentColor = new Color(.12f, .1f, .08f); GUI.Label(new Rect(br.x + 8, br.y + 4, bw - 16, bh), v.Bubble, small);
                    }
                    GUI.contentColor = Color.white;
                }
            // Mira: nameplate, plus a gold marker until she has joined you.
            var mira = session.Companion; if (mira == null) return;
            var head = mira.transform.position + Vector3.up * 1.4f;
            if (Vector3.Distance(player, head) < 45 && ToCanvas(head, out var mp))
            {
                GUI.contentColor = new Color(.55f, 1, .7f); GUI.Label(new Rect(mp.x - 100, mp.y - 30, 200, 26), "Mira", centered);
                if (!session.Progress.recruited) { GUI.contentColor = gold; GUI.Label(new Rect(mp.x - 30, mp.y - 72, 60, 44), "!", marker); }
                else DrawHeadMarker("Mira", mp, Vector3.Distance(player, head), 30);
            }
            GUI.contentColor = Color.white;
        }
        GUIStyle plate;
        void DrawWorldLabels()
        {
            DrawPlaceAndPeopleLabels();
            foreach (var enemy in session.Enemies)
            {
                if (enemy.Hidden) continue;   // lying in wait: no nameplate
                var screen = session.View.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.7f);
                if (screen.z <= 0 || session.Distance(enemy) > 25) continue;
                float x = screen.x * 1440 / Screen.width, y = (Screen.height - screen.y) * 900 / Screen.height;
                if (y < 140 || y > 560) continue;
                // Names are centred over the enemy, like the bar. Your target: a dark backing with a gold edge sized to its
                // name (the ring on the ground marks it too).
                if (plate == null) plate = new GUIStyle(small) { alignment = TextAnchor.UpperCenter, wordWrap = false };
                string label = enemy.actor.Level + "  " + enemy.actor.DisplayName;
                if (session.Target == enemy)
                {
                    float bw = Mathf.Max(146, plate.CalcSize(new GUIContent(label)).x + 16);
                    var box = new Rect(x - bw / 2, y - 25, bw, 38);
                    GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(box, Texture2D.whiteTexture);
                    GUI.color = gold; foreach (var edge in new[] { new Rect(box.x, box.y, box.width, 1.5f), new Rect(box.x, box.yMax - 1.5f, box.width, 1.5f), new Rect(box.x, box.y, 1.5f, box.height), new Rect(box.xMax - 1.5f, box.y, 1.5f, box.height) })
                        GUI.DrawTexture(edge, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                // Level-coloured names (grey, green, yellow, orange, red by how the enemy compares to you).
                GUI.contentColor = session.Target == enemy ? gold : ConColor(enemy.actor.Level);
                GUI.Label(new Rect(x - 110, y - 25, 220, 27), label, plate);
                Bar(new Rect(x - 65, y, 130, 6), enemy.actor.Health.Pool.Ratio, new Color(.8f,.3f,.25f), "");
            }
            GUI.contentColor = Color.white;
            foreach (var f in session.Floating)
            {
                var p = session.View.WorldToScreenPoint(f.position + Vector3.up * (1.3f - (f.expires - Time.time)));
                if (p.z <= 0) continue;
                GUI.contentColor = f.color;
                GUI.Label(new Rect(p.x * 1440 / Screen.width - 45, (Screen.height - p.y) * 900 / Screen.height, 90, 38), f.text, number);
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
        void Bar(Rect r, float fill, Color color, string label)
        {
            GUI.color = new Color(.12f,.15f,.17f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(r.x,r.y,r.width * Mathf.Clamp01(fill),r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label.Length > 0) GUI.Label(new Rect(r.x+5,r.y-1,r.width,r.height+8), label, small);
        }
    }
}




