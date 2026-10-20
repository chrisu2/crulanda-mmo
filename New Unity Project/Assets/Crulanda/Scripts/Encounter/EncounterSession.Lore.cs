using System.Collections.Generic;
using UnityEngine;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The book of lore in play (Phase 8.6 step 7; LoreBook) and the finds that stay of use once found (step 8; DiscoveryLog.Lingers):
    /// - the book: the lore files (EncounterContent/Lore, content.loreFiles) bound to the progress and the content; doing fills it:
    ///   speaking to someone (Interact, Hail), killing a camp's mob (EnemyDied), working a node or picking a rare herb (GatherNow,
    ///   PickHerb), each once, with a line in chat the first time; places come from exploring, pages and finds from finding them;
    /// - a rare herb found grows back a game day after it is picked (DiscoveryLog.Regrown): it shows again and E picks it with
    ///   Herbalism, on the work bar, as a node is worked;
    /// - a wayshrine found blesses you (DiscoveryLog.Blessing, a status on the buff row and a ring of motes, the auras' own), and
    ///   after that a crown left in its dish blesses you again.
    /// </summary>
    public sealed partial class EncounterSession
    {
        LoreBook lore; Crulanda.World.ZoneBuilder loreZone;
        /// <summary>The book of lore, bound to this character and this content; null when the content has no lore files.</summary>
        public LoreBook Lore
        {
            get
            {
                if (lore == null)
                {
                    if (content == null || content.loreFiles == null || content.loreFiles.Length == 0) return null;
                    var texts = new List<string>(); foreach (var f in content.loreFiles) if (f != null) texts.Add(f.text);
                    lore = LoreBook.Parse(texts);
                    lore.TitleOf = name => { var v = VillageLife.Active != null ? VillageLife.Active.Find(name) : null; return v != null ? VillageLife.TitleFor(v.Role) : null; };
                }
                if (lore.Progress != Progress) lore.Bind(Progress);
                if (loreZone != Zone && Zone != null) { loreZone = Zone; lore.Zones = Zone.AllZones(); }
                lore.Quests = Quests != null ? Quests.Db : null; lore.Trades = Professions != null ? Professions.Db : null; lore.Loot = Loot; lore.Items = Items;
                return lore;
            }
        }
        /// <summary>
        /// Found recipes for the book's Leaves chapter. HOOK (Phase 8.6 step 5, another hand's work): set it to list the leaves the
        /// character has learned; until then the chapter is empty.
        /// </summary>
        public System.Func<List<LoreBook.Line>> LoreLeaves;

        // ---------- what doing writes in the book ----------
        void LoreSays(string what) { Message("Your book of lore has a line on " + what + " now [L]."); }
        /// <summary>Spoken to someone of this zone (a villager, a resident, Mira's not one).</summary>
        void NoteMet(string name) { var l = Lore; if (l != null && Zone != null && l.Meet(Zone.Zone, name)) LoreSays(name); }
        /// <summary>Spoken to an adventurer (hailed): the People chapter's adventurers met.</summary>
        void NoteMetSim(SimAdventurer sim) { var l = Lore; if (l != null && sim != null) l.MeetSim(sim.id); }
        /// <summary>A camp's mob killed: its kind in the Creatures chapter (its camp's mob name).</summary>
        void NoteSlain(EncounterEnemy e)
        {
            var l = Lore; if (l == null || Zone == null || e == null || !e.Camp) return;
            var mob = LootContext.From(e.persistentId, Zone.Zone.camps, 0, false).mob;
            if (string.IsNullOrEmpty(mob) || !l.Slay(Zone.Zone, mob)) return;
            bool named = mob.StartsWith("The ") || System.Array.Exists(Zone.Zone.camps, c => c != null && c.mob == mob && c.elite);   // Old Whitefoot, not "the Old Whitefoot"
            LoreSays(named ? mob : "the " + mob);
        }
        /// <summary>A node worked (or a rare herb picked, by its item): the Herbs and Ores chapter.</summary>
        void NoteWorked(string subject, string name)
        {
            var l = Lore; if (l != null && Zone != null && l.Work(Zone.Zone, subject)) LoreSays(string.IsNullOrEmpty(name) ? subject : name);
        }

        // ---------- finds of use again: a herb grown back, a wayshrine ----------
        /// <summary>A found secret that E can use again now: a rare herb grown back, or a wayshrine.</summary>
        public bool UsableAgain(Crulanda.World.ZoneSecretSpot spot)
        {
            return spot != null && spot.def != null && Discoveries != null && Discoveries.IsFound(spot.def) && DiscoveryLog.Lingers(spot.def) &&
                (spot.def.kind == "shrine" || Discoveries.Regrown(spot.def));
        }
        /// <summary>What E says at a secret: its search prompt, or for one found and of use again, picking the herb or the crown.</summary>
        public string SecretPrompt(Crulanda.World.ZoneSecretSpot spot)
        {
            if (spot == null) return null;
            if (UsableAgain(spot)) return spot.def.kind == "shrine" ? "Leave a crown at the shrine" : SearchPrompt(spot.def);
            return SearchPrompt(spot.def);
        }
        /// <summary>E at a found secret of use again: the herb is picked, the crown left.</summary>
        void UseAgain(Crulanda.World.ZoneSecretSpot spot)
        {
            if (spot.def.kind == "shrine")
            {
                if (Player.GetComponent<Combatant>().Statuses.Remaining(DiscoveryLog.BlessingId, Time.time) > 0) { Message(DiscoveryLog.StillBlessedLine); return; }
                if (!Discoveries.Offer(spot.def, out var why)) { Message(why); return; }
                Bless(spot); Save(false);
                return;
            }
            PickHerb(spot);
        }
        /// <summary>The wayshrine's blessing on you: the status (the buff row shows it) and a ring of pale motes round you for a moment.</summary>
        void Bless(Crulanda.World.ZoneSecretSpot spot)
        {
            if (Player == null || !Player.IsAlive) return;
            Player.GetComponent<Combatant>().Apply(DiscoveryLog.Blessing());
            ArchivistKit.Aura(Player.transform, DiscoveryLog.BlessingName, new Color(1, .9f, .62f), 4, .55f, 1.4f, 8);
            Message("The shrine's blessing is on you: a little less from every blow for " + Mathf.RoundToInt(DiscoveryLog.BlessingSeconds / 60) + " minutes.");
        }
        /// <summary>A grown-back herb picked with Herbalism: the work bar, then one into the bags, a skill roll, and it is gone till tomorrow.</summary>
        void PickHerb(Crulanda.World.ZoneSecretSpot spot)
        {
            if (Working || abilities.IsCasting || Professions == null) return;
            var def = DiscoveryLog.HerbNode(spot.def, Professions.Db, Items); if (def == null) return;
            if (InCombat) { Message(FightingLine); return; }
            if (!Professions.CanGather(def, out _, out var why)) { Message(why); return; }
            if (Inventory.Room(Progress, Items, def.item) == 0) { Message(ProfessionLog.BagsFullLine); return; }
            StartWork(WorkLabel("Gather"), def.seconds, () => PickedHerb(spot, def));
            workPose = ActorPose.Gather;
        }
        /// <summary>The herb picked (the work bar's end; tests call it straight). True when it went into the bags.</summary>
        public bool PickedHerb(Crulanda.World.ZoneSecretSpot spot, NodeDef def)
        {
            if (spot == null || def == null || Professions == null || !UsableAgain(spot)) return false;
            int got = Professions.Gather(def, gatherRng, out var why);
            if (got <= 0) { if (why != null) Message(why); return false; }
            FloatText(spot.position, "+" + got + " " + ItemName(def.item), new Color(.86f, .95f, .66f));
            Discoveries.Picked(spot.def); Pocket(spot);
            NoteWorked(spot.def.item, ItemName(def.item));
            Save(false);
            return true;
        }
        /// <summary>Every half second: a picked herb that has grown back shows again.</summary>
        void TickRegrowth()
        {
            if (Discoveries == null) return;
            foreach (var spot in SecretSpots)
                if (spot.root != null && spot.def.kind == "herb" && pocketed.TryGetValue(spot.root, out var shown) && Discoveries.Regrown(spot.def))
                { foreach (var r in shown) if (r != null) r.enabled = true; pocketed.Remove(spot.root); }
        }
    }
}
